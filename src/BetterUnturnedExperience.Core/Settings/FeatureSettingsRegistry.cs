using System;
using System.Collections.Generic;
using System.Linq;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Settings
{
    /// <summary>
    /// DEV-V3-06: the host-owned per-feature settings registry (the
    /// MainThreadDispatcherRuntime composition precedent — public on the
    /// assembly, NOT an SDK contract: features never hold the registry, they
    /// receive the scoped view via IFeatureBootstrap.Settings). It keeps ONE
    /// SettingsRuntime per FeatureId (the single source of truth: a second
    /// schema for the same feature is explicitly kept-out with a structured
    /// line, never silently replaced), tracks each feature's live lifecycle
    /// generation, gates the authority-side ServerAuthority writes, and hands
    /// out the (feature, generation)-bound scoped views.
    ///
    /// Diagnostic code family (registered on the DEV-V3-06 ticket face;
    /// observation lines join the appendix-B observation rows like
    /// BUE-MT-GEN / BUE-CLOCK-001, not the rejection table):
    ///   BUE-SET-001 write rejected — the view's generation is superseded or
    ///           the owner was invalidated (stop/isolation boundary);
    ///   BUE-SET-002 write rejected — ServerAuthority scope on a non-authority
    ///           side (U3DS and the P2P listen host are the SAME authority
    ///           semantics by construction: one gate, one provider);
    ///   BUE-SET-003 a second schema for one feature — KeepExisting (never a
    ///           silent second source of truth);
    ///   BUE-SET-005 an invalid schema reached the registry (the registration
    ///           gate validates facets up front: BUE-REG-011; this is the
    ///           belt for direct construction);
    ///   BUE-SET-CREATED exactly one creation line per feature runtime;
    ///   BUE-SET-GEN  generation-opened / owner-invalidated boundary lines.
    /// </summary>
    public sealed class FeatureSettingsRegistry
    {
        /// <summary>Descriptors per feature, fixed by DEV-V3-06 (observable,
        /// testable — the capacity precedent is DEV-V3-03's TryTrack=64).</summary>
        internal const int MaxDescriptorsPerFeature = 64;

        private sealed class OwnerState
        {
            internal SettingsRuntime Runtime;
            // HasGeneration=false = no live generation (never opened, or
            // invalidated); writes are then refused (reads keep answering).
            internal ulong Generation;
            internal bool HasGeneration;
        }

        private readonly object sync = new object();
        private readonly Dictionary<string, OwnerState> owners = new Dictionary<string, OwnerState>(StringComparer.Ordinal);
        private readonly ISettingsPersistence persistence;
        private readonly Func<bool> authoritySideProvider;
        private readonly Action<string> diagnosticSink;

        public FeatureSettingsRegistry(ISettingsPersistence persistence, Func<bool> authoritySideProvider, Action<string> diagnosticSink)
        {
            // A null provider fails CLOSED: non-authority side (the client
            // role is the conservative default; the host start path always
            // composes the real provider).
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.authoritySideProvider = authoritySideProvider ?? (() => false);
            this.diagnosticSink = diagnosticSink;
        }

        /// <summary>
        /// The single-source factory: composes the feature's SettingsRuntime
        /// from its facet schema on first call; every later call for the same
        /// feature returns the SAME instance (schema equality) or keeps the
        /// existing one with a BUE-SET-003 line (schema conflict). An invalid
        /// schema returns null + BUE-SET-005 (the registration gate makes this
        /// unreachable through the host path — belt for direct construction).
        /// </summary>
        public SettingsRuntime GetOrCreateRuntime(FeatureId feature, IReadOnlyList<SettingDescriptor> descriptors)
        {
            if (string.IsNullOrWhiteSpace(feature.Value)) return null;
            string deferred = null;
            SettingsRuntime result;
            lock (sync)
            {
                if (owners.TryGetValue(feature.Value, out var existing) && existing.Runtime != null)
                {
                    if (SameSchema(existing.Runtime, descriptors)) { result = existing.Runtime; deferred = null; }
                    else
                    {
                        result = existing.Runtime;
                        deferred = "event=settings-schema result=conflict feature=" + feature.Value
                            + " reason=descriptor-mismatch kept=existing diagnosticId=BUE-SET-003";
                    }
                }
                else
                {
                    var errors = ValidateFacetSchema(feature, descriptors);
                    if (errors != null)
                    {
                        result = null;
                        deferred = "event=settings-schema result=rejected feature=" + feature.Value
                            + " errors=" + errors + " diagnosticId=BUE-SET-005";
                    }
                    else
                    {
                        // Composition under the lock is deliberate (named
                        // judgement, R1-Standards #5): the runtime constructor
                        // loads persistence and registers the feature into the
                        // owners map — doing it outside the lock would open a
                        // check-then-act window where two concurrent Ensures
                        // create TWO runtimes for one file, the exact second
                        // source of truth this class exists to forbid. The
                        // lock guards the single-source invariant; every
                        // caller (start path/panel/adapter) rides the host
                        // composition thread, so no IO is ever serialized
                        // against a hot path (the EMIT, the only callback,
                        // stays outside — the 02 R1 discipline).
                        SettingsRuntime runtime = null;
                        try { runtime = new SettingsRuntime(feature, descriptors, persistence); }
                        catch (Exception error)
                        {
                            // The shared validator above must have caught
                            // every construction fault; anything slipping
                            // through stays explicit, never a crash.
                            deferred = "event=settings-schema result=rejected feature=" + feature.Value
                                + " errorType=" + error.GetType().Name + " diagnosticId=BUE-SET-005";
                        }
                        if (runtime == null) result = null;
                        else
                        {
                            ObtainLocked(feature.Value).Runtime = runtime;
                            result = runtime;
                            var schema = descriptors.Max(x => x.SchemaVersion);
                            deferred = "event=settings-runtime result=created feature=" + feature.Value
                                + " descriptors=" + descriptors.Count + " schema=" + schema + " diagnosticId=BUE-SET-CREATED";
                        }
                    }
                }
            }
            Emit(deferred);
            return result;
        }

        /// <summary>The feature's live runtime without creating one (panel
        /// read paths / host internals).</summary>
        public SettingsRuntime TryGetRuntime(FeatureId feature)
        {
            lock (sync)
                return owners.TryGetValue(feature.Value, out var state) ? state.Runtime : null;
        }

        /// <summary>Registers the feature's live lifecycle generation (the
        /// host start path, start and panel re-enable). A superseded
        /// generation's views refuse writes from then on.</summary>
        public void OpenGeneration(FeatureId feature, ulong generation)
        {
            lock (sync)
            {
                var state = ObtainLocked(feature.Value);
                state.HasGeneration = true;
                state.Generation = generation;
            }
            Emit("event=settings-registry result=generation-opened feature=" + feature.Value
                + " generation=" + generation + " diagnosticId=BUE-SET-GEN");
        }

        /// <summary>The stop/isolation boundary: writes from every generation
        /// of the feature are refused until a fresh one opens; reads stay
        /// honest (the feature's own persisted truth, no mutation surface).</summary>
        public void InvalidateOwner(FeatureId feature, string reason)
        {
            lock (sync)
            {
                if (owners.TryGetValue(feature.Value, out var state))
                {
                    state.HasGeneration = false;
                    state.Generation = 0UL;
                }
            }
            Emit("event=settings-registry result=owner-invalidated feature=" + feature.Value
                + " reason=" + (reason ?? string.Empty) + " diagnosticId=BUE-SET-GEN");
        }

        /// <summary>The contract view handed to one module for one lifecycle
        /// generation (bootstrap.Settings). Null when the feature has no
        /// runtime (no settings facet — the matrix row stays null).</summary>
        public IScopedFeatureSettings CreateView(FeatureId feature, ulong generation)
        {
            var runtime = TryGetRuntime(feature);
            return runtime == null ? null : new FeatureScopedSettingsView(this, runtime, feature, generation);
        }

        /// <summary>The view's write pipeline: generation gate → authority
        /// gate → the runtime's own validation/commit (single source). Every
        /// gate decision is taken under the lock; diagnostics emit outside.</summary>
        internal SettingChangeResult Submit(FeatureId owner, ulong viewGeneration, ScopedSettingChangeRequest request)
        {
            SettingsRuntime runtime;
            bool generationOk = false;
            string rejection = null;
            lock (sync)
            {
                if (!owners.TryGetValue(owner.Value, out var state) || state.Runtime == null)
                {
                    runtime = null;
                }
                else
                {
                    runtime = state.Runtime;
                    if (!state.HasGeneration)
                        rejection = "event=settings-view result=rejected feature=" + owner.Value
                            + " stage=submit reason=owner-invalidated scope=" + request.RevisionScope
                            + " view-generation=" + viewGeneration + " live-generation=none diagnosticId=BUE-SET-001";
                    else if (state.Generation != viewGeneration)
                        rejection = "event=settings-view result=rejected feature=" + owner.Value
                            + " stage=submit reason=generation-superseded scope=" + request.RevisionScope
                            + " view-generation=" + viewGeneration + " live-generation=" + state.Generation
                            + " diagnosticId=BUE-SET-001";
                    else
                        generationOk = true;
                }
            }
            if (rejection != null)
            {
                Emit(rejection);
                return Rejected(runtime, owner, request.RevisionScope, FrameworkErrorCode.SettingRejected);
            }
            if (!generationOk) return Rejected(null, owner, request.RevisionScope, FrameworkErrorCode.SettingRejected);
            if (request.RevisionScope == SettingRevisionScope.ServerAuthority && !IsAuthoritySide())
            {
                Emit("event=settings-view result=rejected feature=" + owner.Value
                    + " stage=submit reason=not-authority-side scope=ServerAuthority diagnosticId=BUE-SET-002");
                return Rejected(runtime, owner, request.RevisionScope, FrameworkErrorCode.UnauthorizedSender);
            }
            return runtime.Submit(request);
        }

        private bool IsAuthoritySide()
        {
            try { return authoritySideProvider(); }
            catch { return false; }
        }

        private SettingChangeResult Rejected(SettingsRuntime runtime, FeatureId feature, SettingRevisionScope scope, FrameworkErrorCode error)
        {
            FeatureSettingsSnapshot snapshot;
            if (runtime != null)
            {
                var safeScope = scope == SettingRevisionScope.ServerAuthority ? scope : SettingRevisionScope.ClientPreference;
                try { snapshot = runtime.GetSnapshot(safeScope); }
                catch (ArgumentOutOfRangeException) { snapshot = runtime.GetSnapshot(SettingRevisionScope.ClientPreference); }
                return new SettingChangeResult(false, error, snapshot.Revision, snapshot);
            }
            snapshot = new FeatureSettingsSnapshot(feature, 0, SettingRevisionScope.ClientPreference, 0,
                SettingSyncState.Unavailable, SettingSnapshotSource.SafeDefault, new SettingEntryView[0]);
            return new SettingChangeResult(false, error, 0, snapshot);
        }

        private OwnerState ObtainLocked(string featureValue)
        {
            if (!owners.TryGetValue(featureValue, out var state))
            {
                state = new OwnerState();
                owners.Add(featureValue, state);
            }
            return state;
        }

        private static bool SameSchema(SettingsRuntime runtime, IReadOnlyList<SettingDescriptor> descriptors)
        {
            if (descriptors == null) return false;
            var current = runtime.GetSnapshot(SettingRevisionScope.ClientPreference).Entries;
            if (current.Count != descriptors.Count) return false;
            // ids + authority + value-kind equality is the schema identity
            // here (the runtime's entries are the constructor-cloned, order-
            // normalized form of the same descriptors it was composed from).
            return descriptors.All(d => current.Any(e =>
                e.SettingId == d.SettingId && e.Authority == d.Authority && (byte)e.EffectiveValue.Kind == (byte)d.Kind));
        }

        // internal: shared with the registration bridge (DEV-V3-06 — one
        // validation for the facet gate and the runtime composition, so a
        // registration that passes BUE-REG-011 can never fault later).
        internal static string ValidateFacetSchema(FeatureId feature, IReadOnlyList<SettingDescriptor> descriptors)
        {
            if (descriptors == null || descriptors.Count == 0) return "empty-schema";
            if (descriptors.Count > MaxDescriptorsPerFeature) return "over-capacity-" + MaxDescriptorsPerFeature;
            foreach (var descriptor in descriptors)
            {
                if (!string.Equals(descriptor.Feature.Value, feature.Value, StringComparison.Ordinal)) return "feature-mismatch:" + descriptor.SettingId;
            }
            var errors = SettingsRuntime.ValidateDescriptors(feature, descriptors.ToList());
            return errors.Count == 0 ? null : string.Join(";", errors.Take(4));
        }

        private void Emit(string line)
        {
            if (line == null) return;
            var sink = diagnosticSink;
            if (sink == null) return;
            try { sink(line); }
            catch (Exception) { /* the sink never breaks the settings pipeline */ }
        }
    }

    /// <summary>
    /// DEV-V3-06: the scoped settings view injected as
    /// IFeatureBootstrap.Settings (availability-matrix row: null before this
    /// ticket, the scoped view after it for facet-registered features). It
    /// answers for its OWN feature only; reads are the feature's persisted
    /// truth and always answer (no mutation surface → no cross-generation
    /// pollution), writes ride the registry's generation + authority gates.
    /// The runtime's session-overlay mutators are permanently unreachable
    /// through this view (frozen shape: exactly GetSnapshot/TryGet/Submit).
    /// </summary>
    internal sealed class FeatureScopedSettingsView : IScopedFeatureSettings
    {
        private readonly FeatureSettingsRegistry registry;
        private readonly SettingsRuntime runtime;
        private readonly FeatureId feature;
        private readonly ulong generation;

        internal FeatureScopedSettingsView(FeatureSettingsRegistry registry, SettingsRuntime runtime, FeatureId feature, ulong generation)
        {
            this.registry = registry;
            this.runtime = runtime;
            this.feature = feature;
            this.generation = generation;
        }

        public FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)
        {
            return runtime.GetSnapshot(revisionScope);
        }

        public bool TryGet(string settingId, out SettingValue value, out uint revision)
        {
            return runtime.TryGet(settingId, out value, out revision);
        }

        public SettingChangeResult Submit(ScopedSettingChangeRequest request)
        {
            return registry.Submit(feature, generation, request);
        }
    }
}
