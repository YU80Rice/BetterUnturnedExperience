using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Events;

namespace BetterUnturnedExperience.Core.Registration
{
    /// <summary>
    /// DEV-V3-03: the unified feature lifecycle state machine (V3-T4 plan A).
    /// The HOST owns the FeatureState/FeatureStatusView/StateRevision
    /// projection for every accepted registration; the module cannot mutate
    /// its state (no mutator exists on the contract surface), and one
    /// feature's transition never touches another's. The machine drives the
    /// owner-scoped registration records established in DEV-V3-01 — the
    /// record is the single fact carrier for state, generation, resource
    /// ownership and the stop/isolation outcome, so no FeatureId-keyed
    /// scattered static table grows next to it.
    ///
    /// Frozen semantics this class implements:
    ///   - TryTrack: resources bind to (feature, lifecycle generation);
    ///     rejected for null, stopped/isolated features, stale-generation
    ///     views, duplicate instances and over capacity (64 per feature per
    ///     generation — the bound this ticket fixes); every rejection is an
    ///     explicit false with a structured diagnostic (BUE-LIFE-001..005).
    ///   - Stop boundary: module.Stop FIRST (the caller drives it), then the
    ///     host drops the feature's bus subscriptions and disposes tracked
    ///     resources in REVERSE registration order; a single Dispose fault is
    ///     isolated into a diagnostic (BUE-LIFE-006) and cleanup continues.
    ///   - Re-enable = a NEW lifecycle generation; the old generation's
    ///     views/track handles are invalid (BUE-LIFE-004) and their query
    ///     stays usable, truthfully reporting the feature's CURRENT state.
    ///   - CoreSafeMode is never entered from here: a runtime single-feature
    ///     failure isolates that feature only (the composition-period
    ///     invariant path stays with the registration runtime).
    ///
    /// Public like its siblings (FeatureRegistrationRuntime, FeatureEventBus)
    /// as host-owned composition — public is not SDK contract (V3-T1); the
    /// contract surface modules see is IFeatureLifetime /
    /// IDependencyCapabilityView / FeatureStatusView.
    /// </summary>
    public sealed class FeatureLifecycleRuntime
    {
        /// <summary>The tracked-resource bound per feature per generation (DEV-V3-03's ticket-fixed number; observable and testable).</summary>
        internal const int MaxTrackedResourcesPerGeneration = 64;

        private readonly FeatureRegistrationRuntime registrations;
        private readonly FeatureEventBus bus;
        private readonly Action<string> diagnosticSink;
        private readonly object sync = new object();
        private ulong nextGeneration;

        public FeatureLifecycleRuntime(FeatureRegistrationRuntime registrations, FeatureEventBus bus, Action<string> diagnosticSink)
        {
            this.registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
            this.bus = bus;
            this.diagnosticSink = diagnosticSink;
        }

        /// <summary>
        /// The read-only status view handed to one module for one lifecycle
        /// generation (bootstrap.Lifetime). TryTrack binds to the view's
        /// generation; CurrentStatus answers for the FEATURE (the current
        /// truth), so stale views keep answering truthfully after a re-enable.
        /// </summary>
        public IFeatureLifetime CreateLifetimeView(FeatureId feature, ulong generation)
        {
            return new FeatureLifetimeView(this, feature, generation);
        }

        /// <summary>
        /// The frozen-catalog read-only capability lookup (bootstrap.Dependencies).
        /// Not a solver: the view answers from the catalog that was frozen
        /// before the host start path ran. Today the registration surface has
        /// no capability-declaration source, so the directory's capability
        /// projection is presence-only: a null/empty capabilityId with
        /// minimumVersion 0 asks "is this dependency in the frozen catalog";
        /// any demanded capability or version is honestly unverifiable and
        /// fails closed (false). DEV-V3-08 registers this rule in the SDK
        /// appendix; a future capability-declaration surface can extend the
        /// true-case additively without changing the signature.
        /// </summary>
        public IDependencyCapabilityView CreateDependenciesView()
        {
            return new FrozenCatalogDependencyView(registrations.Catalog);
        }

        /// <summary>The current projection for one feature (never throws, never null; usable at any stage after admission).</summary>
        public FeatureStatusView CurrentStatus(FeatureId feature)
        {
            lock (sync)
            {
                if (registrations.TryGetRecord(feature.Value, out var record))
                {
                    return new FeatureStatusView(feature, record.State, record.StateError, record.StopReason ?? FeatureStopReason.None, record.StopDiagnostic ?? string.Empty, record.StateRevision);
                }
            }
            return new FeatureStatusView(feature, FeatureState.Discovered, FrameworkErrorCode.None, FeatureStopReason.None, string.Empty, 0UL);
        }

        /// <summary>Factory access for the panel enable seam — the host re-arms a feature through the SAME registration factory path.</summary>
        public bool TryGetModuleFactory(FeatureId feature, out IFeatureModuleFactory factory)
        {
            lock (sync)
            {
                if (registrations.TryGetRecord(feature.Value, out var record))
                {
                    factory = record.Registration.ModuleFactory;
                    return true;
                }
            }
            factory = null;
            return false;
        }

        /// <summary>
        /// DEV-V3-06: settings-facet access for the host start path and the
        /// panel re-enable seam — the feature's frozen schema comes from its
        /// OWN registration record (owner-scoped fact carrier, never a
        /// scattered FeatureId table), so every generation of the feature is
        /// composed against the SAME single source. false = no facet (the
        /// Settings matrix row stays null for this feature).
        /// </summary>
        public bool TryGetSettingsFacet(FeatureId feature, out IReadOnlyList<SettingDescriptor> descriptors, out Action onSettingsApplied)
        {
            lock (sync)
            {
                if (registrations.TryGetRecord(feature.Value, out var record) && record.Registration.SettingDescriptors != null)
                {
                    descriptors = record.Registration.SettingDescriptors;
                    onSettingsApplied = record.Registration.OnSettingsApplied;
                    return true;
                }
            }
            descriptors = null;
            onSettingsApplied = null;
            return false;
        }

        /// <summary>
        /// BeginStart: the host start path (catalog start or the panel enable
        /// seam) opens a NEW lifecycle generation. Legal from Discovered (the
        /// catalog start), Disabled, Isolated and Stopped (the user-driven
        /// re-enable paths — Isolated never restarts AUTOMATICALLY, only the
        /// explicit seam call lands here). The per-feature outcome fields
        /// reset; StateRevision never resets.
        /// </summary>
        public bool BeginStart(FeatureId feature, out ulong generation)
        {
            generation = 0;
            string rejectReason = null;
            string stateLine = null;
            lock (sync)
            {
                if (!registrations.TryGetRecord(feature.Value, out var record))
                {
                    rejectReason = "unknown-feature";
                }
                else if (record.State != FeatureState.Discovered && record.State != FeatureState.Disabled
                    && record.State != FeatureState.Isolated && record.State != FeatureState.Stopped)
                {
                    rejectReason = "invalid-transition";
                }
                else
                {
                    nextGeneration++;
                    generation = nextGeneration;
                    record.State = FeatureState.Starting;
                    record.LifecycleGeneration = generation;
                    record.StopReason = null;
                    record.Isolated = false;
                    record.StopDiagnostic = string.Empty;
                    record.StateError = FrameworkErrorCode.None;
                    record.StateRevision++;
                    stateLine = StateLine(feature, FeatureState.Starting, generation, record);
                }
            }
            // Diagnostics never run under the state lock (the frozen discipline).
            if (rejectReason != null)
            {
                Emit("event=feature-lifecycle result=start-rejected feature=" + feature.Value + " reason=" + rejectReason);
                return false;
            }
            Emit(stateLine);
            return true;
        }

        /// <summary>CompleteStart: the module reported a started outcome — Starting → Running.</summary>
        public bool CompleteStart(FeatureId feature, string diagnostic)
        {
            string stateLine = null;
            bool rejected = false;
            lock (sync)
            {
                if (!registrations.TryGetRecord(feature.Value, out var record) || record.State != FeatureState.Starting)
                {
                    rejected = true;
                }
                else
                {
                    record.State = FeatureState.Running;
                    record.StopDiagnostic = diagnostic ?? string.Empty;
                    record.StateRevision++;
                    stateLine = StateLine(feature, FeatureState.Running, record.LifecycleGeneration, record);
                }
            }
            if (rejected)
            {
                Emit("event=feature-lifecycle result=complete-start-rejected feature=" + feature.Value);
                return false;
            }
            Emit(stateLine);
            return true;
        }

        /// <summary>
        /// Isolate: a single-feature runtime failure (Start throw, start
        /// refusal, factory failure — never a CoreSafeMode trigger). Legal
        /// from Starting/Running; withdraws the feature's subscriptions and
        /// tracked resources, then lands Isolated (two transitions, two
        /// revisions). Other features and the host keep running.
        /// </summary>
        public bool Isolate(FeatureId feature, FrameworkErrorCode error, string diagnostic, string stage)
        {
            List<IDisposable> withdrawn = null;
            var isolatingLine = string.Empty;
            var isolatedLine = string.Empty;
            var isolateLine = string.Empty;
            bool rejected = false;
            lock (sync)
            {
                if (!registrations.TryGetRecord(feature.Value, out var record)
                    || (record.State != FeatureState.Starting && record.State != FeatureState.Running))
                {
                    rejected = true;
                }
                else
                {
                    record.State = FeatureState.Isolating;
                    record.StateRevision++;
                    isolatingLine = StateLine(feature, FeatureState.Isolating, record.LifecycleGeneration, record);
                    record.State = FeatureState.Isolated;
                    record.Isolated = true;
                    record.StopReason = FeatureStopReason.RuntimeIsolated;
                    record.StateError = error;
                    record.StopDiagnostic = diagnostic ?? stage ?? string.Empty;
                    record.StateRevision++;
                    withdrawn = SnapshotOwnedResourcesLocked(record);
                    isolatedLine = StateLine(feature, FeatureState.Isolated, record.LifecycleGeneration, record);
                    isolateLine = "event=feature-isolated feature=" + feature.Value + " stage=" + stage
                        + " error=" + error + " diagnosticId=BUE-LIFE-ISOLATE";
                }
            }
            if (rejected)
            {
                Emit("event=feature-lifecycle result=isolate-rejected feature=" + feature.Value + " stage=" + stage);
                return false;
            }
            Emit(isolatingLine);
            Emit(isolatedLine);
            Emit(isolateLine);
            Withdraw(feature, withdrawn);
            return true;
        }

        /// <summary>
        /// BeginStop: the host opens the stop boundary (PluginStopping
        /// teardown or the panel UserDisabled disable). Legal from
        /// Starting/Running/Disabled; the reason is recorded so the Stopping
        /// view already carries it.
        /// </summary>
        public bool BeginStop(FeatureId feature, FeatureStopReason reason)
        {
            string stateLine = null;
            bool rejected = false;
            lock (sync)
            {
                if (!registrations.TryGetRecord(feature.Value, out var record)
                    || (record.State != FeatureState.Running && record.State != FeatureState.Starting && record.State != FeatureState.Disabled))
                {
                    rejected = true;
                }
                else
                {
                    record.State = FeatureState.Stopping;
                    record.StopReason = reason;
                    record.StateRevision++;
                    stateLine = StateLine(feature, FeatureState.Stopping, record.LifecycleGeneration, record);
                }
            }
            if (rejected)
            {
                Emit("event=feature-lifecycle result=stop-rejected feature=" + feature.Value + " reason=not-stoppable");
                return false;
            }
            Emit(stateLine);
            return true;
        }

        /// <summary>
        /// CompleteStop: AFTER module.Stop returned (the caller drives Stop
        /// between BeginStop and CompleteStop — Stop faults never skip the
        /// cleanup), the host drops the feature's bus subscriptions and
        /// disposes the tracked resources in reverse registration order, then
        /// lands Stopped.
        /// </summary>
        public bool CompleteStop(FeatureId feature)
        {
            List<IDisposable> withdrawn = null;
            var stateLine = string.Empty;
            bool rejected = false;
            lock (sync)
            {
                if (!registrations.TryGetRecord(feature.Value, out var record) || record.State != FeatureState.Stopping)
                {
                    rejected = true;
                }
                else
                {
                    record.State = FeatureState.Stopped;
                    record.StateRevision++;
                    withdrawn = SnapshotOwnedResourcesLocked(record);
                    stateLine = StateLine(feature, FeatureState.Stopped, record.LifecycleGeneration, record);
                }
            }
            if (rejected)
            {
                Emit("event=feature-lifecycle result=complete-stop-rejected feature=" + feature.Value);
                return false;
            }
            Emit(stateLine);
            Withdraw(feature, withdrawn);
            return true;
        }

        // Snapshot-and-clear under the lock; the bus drop and the Dispose
        // callbacks run OUTSIDE it (the frozen no-callback-under-lock
        // discipline), in reverse registration order, each fault isolated.
        private static List<IDisposable> SnapshotOwnedResourcesLocked(FeatureRegistrationRecord record)
        {
            var snapshot = new List<IDisposable>(record.OwnedResources);
            record.OwnedResources.Clear();
            return snapshot;
        }

        private void Withdraw(FeatureId feature, List<IDisposable> resources)
        {
            try { bus?.UnsubscribeAll(feature); }
            catch (Exception error)
            {
                Emit("event=feature-resource result=unsubscribe-failed feature=" + feature.Value
                    + " errorType=" + error.GetType().Name);
            }
            for (var i = resources.Count - 1; i >= 0; i--)
            {
                try
                {
                    resources[i].Dispose();
                    Emit("event=feature-resource result=released feature=" + feature.Value
                        + " index=" + (resources.Count - 1 - i) + " diagnosticId=BUE-LIFE-RELEASE");
                }
                catch (Exception error)
                {
                    Emit("event=feature-resource result=release-failed feature=" + feature.Value
                        + " index=" + (resources.Count - 1 - i) + " errorType=" + error.GetType().Name
                        + " message=" + error.Message + " diagnosticId=BUE-LIFE-006");
                }
            }
        }

        private static string StateLine(FeatureId feature, FeatureState to, ulong generation, FeatureRegistrationRecord record)
        {
            return "event=feature-state feature=" + feature.Value + " to=" + to
                + " generation=" + generation + " revision=" + record.StateRevision
                + " reason=" + (record.StopReason.HasValue ? record.StopReason.Value.ToString() : "none")
                + " diagnosticId=BUE-LIFE-STATE";
        }

        /// <summary>
        /// The TryTrack pipeline. Rejection order: null (BUE-LIFE-001) →
        /// state gate — only Starting/Running features may track
        /// (BUE-LIFE-003) → stale-generation view (BUE-LIFE-004) → duplicate
        /// instance (BUE-LIFE-005, no double release) → capacity
        /// (BUE-LIFE-002). Accept = append to the record's owned-resource set.
        /// </summary>
        internal bool TryTrack(FeatureId feature, ulong viewGeneration, IDisposable registration)
        {
            if (registration == null)
            {
                Emit("event=feature-resource result=track-rejected feature=" + feature.Value
                    + " reason=null-registration diagnosticId=BUE-LIFE-001");
                return false;
            }
            string rejectReason = null;
            lock (sync)
            {
                if (!registrations.TryGetRecord(feature.Value, out var record))
                {
                    rejectReason = "unknown-feature";
                }
                else if (record.State != FeatureState.Starting && record.State != FeatureState.Running)
                {
                    rejectReason = record.Isolated ? "feature-isolated" : "feature-not-running";
                }
                else if (record.LifecycleGeneration != viewGeneration)
                {
                    rejectReason = "stale-generation";
                }
                else if (record.OwnedResources.IndexOf(registration) >= 0)
                {
                    rejectReason = "duplicate-registration";
                }
                else if (record.OwnedResources.Count >= MaxTrackedResourcesPerGeneration)
                {
                    rejectReason = "capacity-exceeded";
                }
                else
                {
                    record.OwnedResources.Add(registration);
                }
            }
            if (rejectReason != null)
            {
                var diagnosticId = rejectReason == "capacity-exceeded" ? "BUE-LIFE-002"
                    : rejectReason == "stale-generation" ? "BUE-LIFE-004"
                    : rejectReason == "duplicate-registration" ? "BUE-LIFE-005"
                    : "BUE-LIFE-003";
                Emit("event=feature-resource result=track-rejected feature=" + feature.Value
                    + " generation=" + viewGeneration + " reason=" + rejectReason + " diagnosticId=" + diagnosticId);
                return false;
            }
            Emit("event=feature-resource result=tracked feature=" + feature.Value + " generation=" + viewGeneration
                + " diagnosticId=BUE-LIFE-ACCEPT");
            return true;
        }

        private void Emit(string line)
        {
            var sink = diagnosticSink;
            if (sink == null) return;
            try { sink(line); }
            catch (Exception) { }
        }

        private sealed class FeatureLifetimeView : IFeatureLifetime
        {
            private readonly FeatureLifecycleRuntime owner;
            private readonly FeatureId feature;
            private readonly ulong generation;

            internal FeatureLifetimeView(FeatureLifecycleRuntime owner, FeatureId feature, ulong generation)
            {
                this.owner = owner;
                this.feature = feature;
                this.generation = generation;
            }

            public bool TryTrack(IDisposable registration) { return owner.TryTrack(feature, generation, registration); }

            public FeatureStatusView CurrentStatus { get { return owner.CurrentStatus(feature); } }
        }

        private sealed class FrozenCatalogDependencyView : IDependencyCapabilityView
        {
            private readonly FeatureRegistrationCatalog catalog;

            internal FrozenCatalogDependencyView(FeatureRegistrationCatalog catalog)
            {
                this.catalog = catalog;
            }

            public bool Has(string declaredDependencyId, string capabilityId, ushort minimumVersion)
            {
                if (string.IsNullOrEmpty(declaredDependencyId)) return false;
                // Presence-only capability projection (see the factory comment):
                // an empty capability demand at version 0 is the pure directory
                // lookup; anything more specific cannot be attested today.
                if (!string.IsNullOrEmpty(capabilityId) || minimumVersion != 0) return false;
                return FindEntry(declaredDependencyId) != null;
            }

            public bool TryGet(string declaredDependencyId, out NegotiatedFeatureView feature)
            {
                var entry = string.IsNullOrEmpty(declaredDependencyId) ? null : FindEntry(declaredDependencyId);
                if (entry == null)
                {
                    feature = default(NegotiatedFeatureView);
                    return false;
                }
                feature = new NegotiatedFeatureView(
                    entry.Definition.Feature,
                    default(WireSemanticVersion),
                    default(WireSemanticVersion),
                    NegotiationState.Available,
                    FrameworkErrorCode.None,
                    catalog.CatalogRevision,
                    new CapabilityDescriptor[0]);
                return true;
            }

            private FeatureRegistrationEntry FindEntry(string declaredDependencyId)
            {
                var entries = catalog.Entries;
                for (var i = 0; i < entries.Count; i++)
                {
                    if (string.Equals(entries[i].Definition.Feature.Value, declaredDependencyId, StringComparison.Ordinal)) return entries[i];
                }
                return null;
            }
        }
    }
}
