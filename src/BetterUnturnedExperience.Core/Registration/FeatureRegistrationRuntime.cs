using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Registration
{
    public sealed class FeatureRegistrationEntry
    {
        internal FeatureRegistrationEntry(FeatureRegistrationSnapshot registration)
        {
            Definition = registration.Definition;
            MinimumBueContract = registration.MinimumBueContract;
            ModuleFactory = registration.ModuleFactory;
            ClientUi = registration.ClientUi;
            // DEV-V3-06: the settings facet projection — the frozen catalog's
            // ONLY basis for the management panel's dynamic routing and for
            // the host start path's Settings wiring (官方硬编码清单退役).
            // Null SettingDescriptors = no facet = no platform-managed
            // settings (the view stays null; the panel never fakes a page).
            SettingDescriptors = registration.SettingDescriptors;
            OnSettingsApplied = registration.OnSettingsApplied;
            // DEV-V6-05 (V6-T5 Q1): the optional presentation-metadata facet —
            // also discovered by type test, also copied at admission (the
            // settings/ClientUi snapshot precedent: the registration object
            // stays caller-mutable, the catalog entry never does). Empty or
            // whitespace-only self-report normalizes to null = "no display
            // name reported", so the panel's fallback is a single honest rule.
            DisplayName = string.IsNullOrWhiteSpace(registration.DisplayName) ? null : registration.DisplayName;
            DirectPresentation = registration.DirectPresentation;
        }

        public FeatureDefinitionArtifact Definition { get; }
        public ContractVersion MinimumBueContract { get; }
        public IFeatureModuleFactory ModuleFactory { get; }
        public IClientUiSatelliteRegistration ClientUi { get; }
        public IReadOnlyList<SettingDescriptor> SettingDescriptors { get; }
        public Action OnSettingsApplied { get; }
        // DEV-V6-05: the self-reported panel facts (null/empty DisplayName =
        // the feature reported nothing; the panel then draws the FeatureId).
        public string DisplayName { get; }
        public bool DirectPresentation { get; }
    }

    public sealed class FeatureRegistrationCatalog
    {
        internal FeatureRegistrationCatalog(IEnumerable<FeatureRegistrationEntry> entries, ulong catalogRevision)
        {
            Entries = new ReadOnlyCollection<FeatureRegistrationEntry>((entries ?? Enumerable.Empty<FeatureRegistrationEntry>()).ToArray());
            CatalogRevision = catalogRevision;
        }

        public IReadOnlyList<FeatureRegistrationEntry> Entries { get; }
        public ulong CatalogRevision { get; }
    }

    public sealed class FeatureRegistrationRuntime : IBueFeatureRegistrationHost
    {
        // DEV-V2-14: the contract Major bump — the frozen surface gained the
        // directional subscribe and the bootstrap network member, so the
        // registration gate supports contract (2,0).
        private const ushort SupportedContractMajor = 2;
        // DEV-V3-01: the Minor 2.1 additive batch (the phase-3 contract
        // surface) opens the gate to (2,1) minimum-contract registrations;
        // (2,0) modules stay registrable unchanged.
        private const ushort SupportedContractMinor = 1;
        private readonly object sync = new object();
        private readonly Dictionary<string, FeatureRegistrationRecord> registrations = new Dictionary<string, FeatureRegistrationRecord>(StringComparer.Ordinal);

        public FeatureRegistrationRuntime(Action<FeatureRegistrationPhase> phaseObserver = null)
        {
            Phase = FeatureRegistrationPhase.HostStarting;
            PhaseObserver = phaseObserver;
        }

        public FeatureRegistrationPhase Phase { get; private set; }
        public FeatureRegistrationCatalog Catalog { get; private set; }

        /// <summary>
        /// DEV-V6-03 (V6-T4 Q1): the phase-transition observation mouth, bound
        /// at construction. The production ready entry freezes the catalog
        /// before publishing ready, but the completion barrier is atomic to
        /// callers — the intermediate CatalogFrozen phase is reachable ONLY
        /// through this observer, which is what makes the freeze step
        /// observable on the production path. The host binds it to the runtime
        /// trace (Debug level); unbound (fixtures, harnesses) every transition
        /// is silently unobserved, and an observer fault never breaks the
        /// phase machine.
        /// </summary>
        public Action<FeatureRegistrationPhase> PhaseObserver { get; }

        public void OpenRegistration()
        {
            lock (sync)
            {
                if (Phase != FeatureRegistrationPhase.HostStarting) return;
                Phase = FeatureRegistrationPhase.RegistrationOpen;
                NotifyPhaseLocked();
            }
        }

        public FeatureRegistrationResult Register(IFeatureRegistration registration)
        {
            lock (sync)
            {
                if (Phase == FeatureRegistrationPhase.HostStarting) return Reject(new FeatureId(string.Empty), FeatureRegistrationReason.HostUnavailable, "BUE-REG-001");
                if (Phase == FeatureRegistrationPhase.CoreSafeMode) return Reject(new FeatureId(string.Empty), FeatureRegistrationReason.CoreUnavailable, "BUE-REG-002");
                if (Phase != FeatureRegistrationPhase.RegistrationOpen) return Reject(new FeatureId(string.Empty), FeatureRegistrationReason.PhaseClosed, "BUE-REG-003");
            }
            FeatureRegistrationSnapshot snapshot;
            FeatureId feature = new FeatureId(string.Empty);
            try
            {
                var definition = registration == null ? null : registration.Definition;
                feature = definition == null ? feature : definition.Feature;
                if (!IsValidDefinition(definition)) return Reject(feature, FeatureRegistrationReason.InvalidDefinitionArtifact, "BUE-REG-004");
                var minimumContract = registration.MinimumBueContract;
                var factory = registration.ModuleFactory;
                var clientUi = registration.ClientUi;
                if (factory == null) return Reject(feature, FeatureRegistrationReason.InvalidModuleFactory, "BUE-REG-005");
                // DEV-V3-01 decision order: artifact validation (basic +
                // format) first, then the reserved segment, then the contract
                // gate, then the duplicate gate. A FeatureId inside the BUE
                // reserved segment registers only when it is exactly one of
                // the enumerated official identities; everything else is the
                // deterministic ReservedFeatureId / BUE-REG-010 rejection.
                if (OfficialFeatureIdentity.IsReservedSegment(feature.Value) && !OfficialFeatureIdentity.IsWhitelisted(feature.Value)) return Reject(feature, FeatureRegistrationReason.ReservedFeatureId, "BUE-REG-010");
                if (!IsSupportedContract(minimumContract)) return Reject(feature, FeatureRegistrationReason.ContractIncompatible, "BUE-REG-006");
                if (clientUi != null && (string.IsNullOrEmpty(clientUi.SatelliteId) || string.IsNullOrEmpty(clientUi.RegistrationToken) || !IsSupportedContract(clientUi.MinimumBueContract))) return Reject(feature, FeatureRegistrationReason.InvalidClientUiRegistration, "BUE-REG-008");
                // DEV-V3-06: the optional settings facet (IFeatureSettingsRegistration,
                // discovered by type test — never demanded on IFeatureRegistration,
                // the implementer-side interface rule). A declared schema must be
                // the feature's OWN, non-empty and within the 64/feature cap
                // (DEV-V3-06's fixed value); rejection is explicit with the new
                // code BUE-REG-011 on the frozen InvalidDefinitionArtifact reason
                // (the enum stays unchanged, Minor 2.1 additive code-table only).
                var settingsFacet = registration as IFeatureSettingsRegistration;
                IReadOnlyList<SettingDescriptor> settingsDescriptors = null;
                Action settingsOnApplied = null;
                if (settingsFacet != null)
                {
                    settingsDescriptors = settingsFacet.SettingDescriptors;
                    // The refresh hook resolves LIVE on the registration object
                    // at fire time (the official registrations bind it to the
                    // module instance wired AFTER admission): the snapshot
                    // stores a resolver, never a possibly-stale delegate.
                    settingsOnApplied = new Action(() =>
                    {
                        var live = settingsFacet.OnSettingsApplied;
                        if (live != null) live();
                    });
                    if (BetterUnturnedExperience.Core.Settings.FeatureSettingsRegistry.ValidateFacetSchema(feature, settingsDescriptors) != null)
                        return Reject(feature, FeatureRegistrationReason.InvalidDefinitionArtifact, "BUE-REG-011");
                }
                // DEV-V6-05 (V6-T5 Q1): the optional presentation-metadata
                // facet (IFeaturePresentationRegistration), discovered by type
                // test exactly like the settings facet — no member is demanded
                // on IFeatureRegistration, so 2.0 ecosystem registration types
                // keep loading unchanged. There is deliberately NO validation
                // rejection here: a display name is panel copy (the panel
                // already draws arbitrary caller-supplied FeatureIds), so an
                // over-long or odd name is not a definition-artifact defect.
                var presentationFacet = registration as IFeaturePresentationRegistration;
                snapshot = new FeatureRegistrationSnapshot(definition, minimumContract, factory, clientUi, settingsDescriptors, settingsOnApplied,
                    // DEV-V6-05: the optional presentation-metadata facet — same
                    // type-discovery rule as the settings facet above (never a
                    // member on IFeatureRegistration; a feature that does not
                    // implement it keeps the honest FeatureId fallback).
                    presentationFacet == null ? null : presentationFacet.DisplayName,
                    presentationFacet != null && presentationFacet.DirectPresentation);
            }
            catch (Exception)
            {
                return Reject(feature, FeatureRegistrationReason.InvalidDefinitionArtifact, "BUE-REG-009");
            }

            lock (sync)
            {
                if (Phase == FeatureRegistrationPhase.HostStarting) return Reject(feature, FeatureRegistrationReason.HostUnavailable, "BUE-REG-001");
                if (Phase == FeatureRegistrationPhase.CoreSafeMode) return Reject(feature, FeatureRegistrationReason.CoreUnavailable, "BUE-REG-002");
                if (Phase != FeatureRegistrationPhase.RegistrationOpen) return Reject(feature, FeatureRegistrationReason.PhaseClosed, "BUE-REG-003");
                if (registrations.ContainsKey(feature.Value)) return Reject(feature, FeatureRegistrationReason.DuplicateFeature, "BUE-REG-007");
                registrations.Add(feature.Value, new FeatureRegistrationRecord(snapshot));
                return new FeatureRegistrationResult(true, feature, FeatureRegistrationReason.None, "BUE-REG-ACCEPT");
            }
        }

        public bool FreezeCatalog()
        {
            lock (sync)
            {
                if (Phase != FeatureRegistrationPhase.RegistrationOpen) return false;
                BuildCatalogLocked();
                Phase = FeatureRegistrationPhase.CatalogFrozen;
                NotifyPhaseLocked();
                return true;
            }
        }

        /// <summary>
        /// Closes registration and publishes RuntimeReady as one host-owned barrier.
        /// External features cannot call this method; the BUE plugin invokes it after
        /// Unity has completed all dependency-ordered Awake callbacks.
        /// DEV-V6-03 (V6-T4 Q1): the internal order is freeze-then-ready — the
        /// production ready path really passes through the freeze phase (the very
        /// two entries above, called on the re-entrant lock, so the two phase
        /// transitions cannot drift apart), while callers still observe exactly
        /// one readiness and the registered observer sees both transitions.
        /// </summary>
        public bool CompleteRuntime()
        {
            lock (sync)
            {
                if (Phase != FeatureRegistrationPhase.RegistrationOpen) return false;
                FreezeCatalog();
                return MarkRuntimeReady();
            }
        }

        public bool MarkRuntimeReady()
        {
            lock (sync)
            {
                if (Phase != FeatureRegistrationPhase.CatalogFrozen || Catalog == null) return false;
                Phase = FeatureRegistrationPhase.RuntimeReady;
                NotifyPhaseLocked();
                return true;
            }
        }

        public void EnterCoreSafeMode()
        {
            lock (sync)
            {
                // DEV-V6-03: 观察口语义=相位「真实迁移」流。原实现对同一相位无条件
                // 重赋值（外显 Phase 值不变）；本票起同相位重复调用既不重赋值也不再
                // 宣告——观察者看到的每一次宣告都对应一次真迁移，不掺重复调用噪声。
                if (Phase == FeatureRegistrationPhase.CoreSafeMode) return;
                Phase = FeatureRegistrationPhase.CoreSafeMode;
                NotifyPhaseLocked();
            }
        }

        /// <summary>
        /// Announces one completed phase transition. Called on the machine's own
        /// lock — same thread as the transition, so an observer always reads the
        /// phase it was handed. Fault-isolated by design: a throwing observer
        /// (diagnostics) never breaks the phase machine, and an unbound observer
        /// is a silent no-op.
        /// </summary>
        private void NotifyPhaseLocked()
        {
            var observer = PhaseObserver;
            if (observer == null) return;
            try { observer(Phase); }
            catch (Exception) { }
        }

        /// <summary>
        /// DEV-V3-03: internal record access for the lifecycle state machine —
        /// the owner-scoped registration record (established in DEV-V3-01) is
        /// the fact carrier the machine drives. Assembly-internal by design:
        /// the public bridge stays the narrow result surface (V3-T2), so the
        /// record never leaks as a registration session.
        /// </summary>
        internal bool TryGetRecord(string featureId, out FeatureRegistrationRecord record)
        {
            lock (sync) return registrations.TryGetValue(featureId, out record);
        }

        private void BuildCatalogLocked()
        {
            var ordered = registrations.Values
                .OrderBy(x => x.Registration.Definition.Feature.Value, StringComparer.Ordinal)
                .ThenBy(x => DigestText(x.Registration.Definition.DefinitionSetDigest), StringComparer.Ordinal)
                .ThenBy(x => DigestText(x.Registration.Definition.ArtifactPayloadDigest), StringComparer.Ordinal)
                .Select(x => new FeatureRegistrationEntry(x.Registration))
                .ToArray();
            Catalog = new FeatureRegistrationCatalog(ordered, ComputeRevision(ordered));
        }

        private static bool IsValidDefinition(FeatureDefinitionArtifact definition)
        {
            return definition != null
                && !string.IsNullOrEmpty(definition.Feature.Value)
                && definition.FormatVersion != 0
                && !string.IsNullOrEmpty(definition.DefinitionSetId)
                && definition.CanonicalPayload != null
                && definition.CanonicalPayload.Count != 0
                && DigestMatches(definition.CanonicalPayload, definition.ArtifactPayloadDigest);
        }

        private static bool IsSupportedContract(ContractVersion version)
        {
            // The host gate is Major 2 exactly (DEV-V3-01): an older Major
            // floor is not a supported contract, and within Major 2 every
            // Minor up to the supported additive batch registers.
            return version.Major == SupportedContractMajor && version.Minor <= SupportedContractMinor;
        }

        private static bool DigestMatches(IReadOnlyList<byte> payload, Digest256 expected)
        {
            // DEV-V6-06 (V6-T6 Q5): the admission gate re-computes through the
            // PUBLIC digest function — the documented generator and the accepted
            // digest share one implementation (no second algorithm to drift).
            var computed = FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload);
            return computed.Part0 == expected.Part0 && computed.Part1 == expected.Part1
                && computed.Part2 == expected.Part2 && computed.Part3 == expected.Part3;
        }

        private static FeatureRegistrationResult Reject(FeatureId feature, FeatureRegistrationReason reason, string diagnosticId)
        {
            return new FeatureRegistrationResult(false, feature, reason, diagnosticId);
        }

        private static ulong ComputeRevision(IEnumerable<FeatureRegistrationEntry> entries)
        {
            var canonical = string.Join("\n", entries.Select(x => string.Join("|", x.Definition.Feature.Value, x.Definition.FormatVersion.ToString("D5"), x.Definition.DefinitionSetId, DigestText(x.Definition.DefinitionSetDigest), DigestText(x.Definition.ArtifactPayloadDigest))));
            byte[] hash;
            using (var sha256 = SHA256.Create()) hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var revision = BitConverter.ToUInt64(hash, 0);
            return revision == 0UL ? 1UL : revision;
        }

        private static string DigestText(Digest256 digest)
        {
            return digest.Part0.ToString("X16") + digest.Part1.ToString("X16") + digest.Part2.ToString("X16") + digest.Part3.ToString("X16");
        }
    }

    internal sealed class FeatureRegistrationSnapshot
    {
        internal FeatureRegistrationSnapshot(FeatureDefinitionArtifact definition, ContractVersion minimumBueContract, IFeatureModuleFactory moduleFactory, IClientUiSatelliteRegistration clientUi,
            IReadOnlyList<SettingDescriptor> settingsDescriptors = null, Action settingsOnApplied = null,
            string displayName = null, bool directPresentation = false)
        {
            Definition = definition;
            MinimumBueContract = minimumBueContract;
            ModuleFactory = moduleFactory;
            ClientUi = clientUi == null ? null : new ClientUiSatelliteSnapshot(clientUi);
            // DEV-V3-06: the facet is COPIED at admission (the ClientUi
            // snapshot precedent) — the registration object stays caller-
            // mutable, the catalog entry never does.
            SettingDescriptors = settingsDescriptors == null
                ? null
                : new System.Collections.ObjectModel.ReadOnlyCollection<SettingDescriptor>(settingsDescriptors.ToArray());
            OnSettingsApplied = settingsOnApplied;
            DisplayName = displayName;
            DirectPresentation = directPresentation;
        }

        internal FeatureDefinitionArtifact Definition { get; }
        internal ContractVersion MinimumBueContract { get; }
        internal IFeatureModuleFactory ModuleFactory { get; }
        internal IClientUiSatelliteRegistration ClientUi { get; }
        internal IReadOnlyList<SettingDescriptor> SettingDescriptors { get; }
        internal Action OnSettingsApplied { get; }
        // DEV-V6-05: the presentation-metadata facet projection (null/empty
        // DisplayName = the feature reported nothing).
        internal string DisplayName { get; }
        internal bool DirectPresentation { get; }
    }

    internal sealed class ClientUiSatelliteSnapshot : IClientUiSatelliteRegistration
    {
        internal ClientUiSatelliteSnapshot(IClientUiSatelliteRegistration source)
        {
            SatelliteId = source.SatelliteId;
            MinimumBueContract = source.MinimumBueContract;
            RegistrationToken = source.RegistrationToken;
        }

        public string SatelliteId { get; }
        public ContractVersion MinimumBueContract { get; }
        public string RegistrationToken { get; }
    }

    /// <summary>
    /// DEV-V3-01: the official identity whitelist. The BUE reserved segment —
    /// the io.github.yu80rice.bue root and everything under it — is admit-
    /// only-if-enumerated: every official FeatureId the host ships is listed
    /// here, and any other reserved-segment registration is deterministically
    /// rejected (ReservedFeatureId / BUE-REG-010). The check is an Ordinal
    /// string comparison: it never reflects on the caller and never reads
    /// paths, and membership grants no privilege beyond the admission rules
    /// every registration already obeys (spec V3-T2).
    /// </summary>
    internal static class OfficialFeatureIdentity
    {
        private const string ReservedSegmentRoot = "io.github.yu80rice.bue";
        // BII (better-item-interaction), LIT (inventory-tidy), LIR
        // (in-place-reload), LHT (horde-tracker), the BUE network module and
        // its V1 compatibility facet, and the shipped NoOp sample fixture.
        private static readonly string[] Whitelist =
        {
            "io.github.yu80rice.bue.better-item-interaction",
            "io.github.yu80rice.bue.inventory-tidy",
            "io.github.yu80rice.bue.in-place-reload",
            "io.github.yu80rice.bue.horde-tracker",
            "io.github.yu80rice.bue.network",
            "io.github.yu80rice.bue.network.v1compat",
            "io.github.yu80rice.bue.noop"
        };

        internal static bool IsReservedSegment(string featureId)
        {
            return featureId != null && (featureId == ReservedSegmentRoot || featureId.StartsWith(ReservedSegmentRoot + ".", StringComparison.Ordinal));
        }

        internal static bool IsWhitelisted(string featureId)
        {
            for (var index = 0; index < Whitelist.Length; index++)
            {
                if (string.Equals(Whitelist[index], featureId, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// DEV-V3-01: the owner-scoped registration record — the host-internal
    /// fact carrier for one accepted registration, keyed by the owner
    /// FeatureId. It consolidates the admission snapshot (the registration
    /// source), the current state, the lifecycle generation, resource
    /// ownership and the stop/isolation outcome in ONE per-feature object, so
    /// the lifecycle wiring (DEV-V3-03) drives this record instead of growing
    /// scattered FeatureId-keyed static tables. Not a public registration
    /// session — the public bridge stays the narrow result surface (V3-T2).
    /// </summary>
    internal sealed class FeatureRegistrationRecord
    {
        internal const string RegistrationSourceBridge = "registration-bridge";

        internal FeatureRegistrationRecord(FeatureRegistrationSnapshot registration)
        {
            Registration = registration;
            RegistrationSource = RegistrationSourceBridge;
            State = FeatureState.Discovered;
            OwnedResources = new List<IDisposable>();
        }

        // 注册来源: the accepted registration artifact snapshot — the only
        // admission path today is the public registration bridge.
        internal FeatureRegistrationSnapshot Registration { get; }
        internal string RegistrationSource { get; }
        // 当前状态: Discovered from admission on; DEV-V3-03's state machine
        // drives every transition, the module itself cannot.
        internal FeatureState State { get; set; }
        // DEV-V3-03: the per-feature monotonic StateRevision — the projection
        // rule frozen in V3-T4 (every legal host-driven transition produces a
        // new FeatureStatusView with a strictly increasing revision; queries
        // between transitions observe a stable value). Never resets across
        // generations.
        internal ulong StateRevision { get; set; }
        // Lifecycle generation: 0 until the host allocates one on module
        // start; DEV-V3-03 takes over allocation and invalidation.
        internal ulong LifecycleGeneration { get; set; }
        // 资源所有权: tracked disposables, released in reverse registration
        // order on stop (DEV-V3-03 wires IFeatureLifetime.TryTrack here).
        internal List<IDisposable> OwnedResources { get; }
        // 停止与隔离结果: null/false until the host stops or isolates the
        // feature (DEV-V3-03 records the outcome here).
        internal FeatureStopReason? StopReason { get; set; }
        internal bool Isolated { get; set; }
        internal string StopDiagnostic { get; set; }
        // DEV-V3-03: the framework error of the last host-driven outcome —
        // None while starting/running, the isolation error (e.g.
        // ModuleStartFailed) once isolated; feeds FeatureStatusView.Error.
        internal FrameworkErrorCode StateError { get; set; }
    }
}
