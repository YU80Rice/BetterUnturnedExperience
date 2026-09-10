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
        }

        public FeatureDefinitionArtifact Definition { get; }
        public ContractVersion MinimumBueContract { get; }
        public IFeatureModuleFactory ModuleFactory { get; }
        public IClientUiSatelliteRegistration ClientUi { get; }
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

        public FeatureRegistrationRuntime()
        {
            Phase = FeatureRegistrationPhase.HostStarting;
        }

        public FeatureRegistrationPhase Phase { get; private set; }
        public FeatureRegistrationCatalog Catalog { get; private set; }

        public void OpenRegistration()
        {
            lock (sync)
            {
                if (Phase == FeatureRegistrationPhase.HostStarting) Phase = FeatureRegistrationPhase.RegistrationOpen;
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
                snapshot = new FeatureRegistrationSnapshot(definition, minimumContract, factory, clientUi);
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
                return true;
            }
        }

        /// <summary>
        /// Closes registration and publishes RuntimeReady as one host-owned barrier.
        /// External features cannot call this method; the BUE plugin invokes it after
        /// Unity has completed all dependency-ordered Awake callbacks.
        /// </summary>
        public bool CompleteRuntime()
        {
            lock (sync)
            {
                if (Phase != FeatureRegistrationPhase.RegistrationOpen) return false;
                BuildCatalogLocked();
                Phase = FeatureRegistrationPhase.RuntimeReady;
                return true;
            }
        }

        public bool MarkRuntimeReady()
        {
            lock (sync)
            {
                if (Phase != FeatureRegistrationPhase.CatalogFrozen || Catalog == null) return false;
                Phase = FeatureRegistrationPhase.RuntimeReady;
                return true;
            }
        }

        public void EnterCoreSafeMode()
        {
            lock (sync) Phase = FeatureRegistrationPhase.CoreSafeMode;
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
            byte[] hash;
            using (var sha256 = SHA256.Create()) hash = sha256.ComputeHash(payload.ToArray());
            return ToDigest(hash).Part0 == expected.Part0 && ToDigest(hash).Part1 == expected.Part1 && ToDigest(hash).Part2 == expected.Part2 && ToDigest(hash).Part3 == expected.Part3;
        }

        private static Digest256 ToDigest(byte[] bytes)
        {
            return new Digest256(BitConverter.ToUInt64(bytes, 0), BitConverter.ToUInt64(bytes, 8), BitConverter.ToUInt64(bytes, 16), BitConverter.ToUInt64(bytes, 24));
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
        internal FeatureRegistrationSnapshot(FeatureDefinitionArtifact definition, ContractVersion minimumBueContract, IFeatureModuleFactory moduleFactory, IClientUiSatelliteRegistration clientUi)
        {
            Definition = definition;
            MinimumBueContract = minimumBueContract;
            ModuleFactory = moduleFactory;
            ClientUi = clientUi == null ? null : new ClientUiSatelliteSnapshot(clientUi);
        }

        internal FeatureDefinitionArtifact Definition { get; }
        internal ContractVersion MinimumBueContract { get; }
        internal IFeatureModuleFactory ModuleFactory { get; }
        internal IClientUiSatelliteRegistration ClientUi { get; }
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
    }
}
