using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Registration;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-21: the host module start path — the composition the network
    /// injection contract froze (DEV-V2-14) finally drives. When the host
    /// registration barrier completes, each catalog module is created through
    /// its factory and started with a composed <see cref="FeatureBootstrap"/>:
    /// Network is the stable never-null feature facade (DEV-V2-21), Events /
    /// OwnedEvents are the host bus views under the feature's OWN identity
    /// (the only route to a TidyCompleted publish), and the lifecycle
    /// generation is host-allocated monotonic.
    ///
    /// DEV-V3-03: every module now runs through the unified lifecycle state
    /// machine (Core FeatureLifecycleRuntime driving the DEV-V3-01
    /// registration records) — the FeatureState projection is host-owned and
    /// per-feature: a factory/start failure ISOLATES that feature (resources
    /// withdrawn, subscriptions dropped, others keep running, never a
    /// CoreSafeMode escalation). The availability matrix rows this ticket
    /// wires: Dependencies (the frozen-catalog read-only capability lookup)
    /// and Lifetime (TryTrack + the read-only status query) are composed
    /// non-null from here on; Settings/MainThread/Logger stay null (06/04/07).
    ///
    /// The panel enable/disable seam (SetFeatureEnabled) rides the SAME
    /// machine: disable = module.Stop(UserDisabled) → subscriptions dropped →
    /// tracked resources reverse-disposed → Stopped; re-enable = NEW lifecycle
    /// generation through the SAME registration factory. Started modules are
    /// tracked per entry so the frozen stop handoff holds (module.Stop FIRST,
    /// bus UnsubscribeAll after Stop returns — the FeatureEventBus class
    /// contract).
    /// </summary>
    internal static class BueFeatureStartRuntime
    {
        private static readonly object sync = new object();
        private static readonly List<StartedModule> started = new List<StartedModule>();

        private sealed class StartedModule
        {
            internal FeatureId Feature;
            internal FeatureLifecycleRuntime Machine;
            // The live module instance; null while the feature has no running
            // module (isolated at start, or stopped through the seam).
            internal IFeatureModule Module;
            // The lifetime view the module received for its CURRENT generation
            // (kept so the query stays usable after stop/isolation).
            internal IFeatureLifetime LifetimeView;
            internal FeatureScopeIdentity Identity;
            internal IBueNetworkApi Network;
            // True while no module is running (stopped via UserDisabled /
            // PluginStopping, or isolated at start).
            internal bool Stopped;
        }

        /// <summary>
        /// Starts every catalog module once the host barrier has completed.
        /// A factory that throws or yields null, and a module whose Start
        /// throws or reports not-started, ISOLATES that feature through the
        /// state machine (host log, resources withdrawn, never a host crash
        /// and never a CoreSafeMode escalation); the rest of the catalog
        /// starts unaffected.
        /// </summary>
        internal static void StartCatalog(FeatureRegistrationRuntime runtime, IBueNetworkApi featureNetwork)
        {
            if (runtime == null || runtime.Catalog == null || featureNetwork == null) return;
            var machine = new FeatureLifecycleRuntime(runtime, BueHostEventRuntime.Bus, BueRuntimeLog.Runtime);
            var entries = runtime.Catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var feature = entry.Definition.Feature;
                // The machine rejects a second start of an already-started
                // record (invalid transition) with a structured diagnostic.
                if (!machine.BeginStart(feature, out var generation))
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=skipped feature=" + feature.Value + " reason=invalid-state");
                    continue;
                }
                var definition = entry.Definition;
                var identity = new FeatureScopeIdentity(definition.Feature, null, null, definition.DefinitionSetId, definition.DefinitionSetDigest);
                IFeatureModule module = null;
                try { module = entry.ModuleFactory.Create(); }
                catch (Exception error)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=failed stage=factory feature=" + feature.Value + " errorType=" + error.GetType().Name);
                    TrackEntry(feature, machine, null, null, identity, featureNetwork);
                    machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "errorType=" + error.GetType().Name, "factory");
                    continue;
                }
                if (module == null)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=failed stage=factory feature=" + feature.Value + " reason=factory-null");
                    TrackEntry(feature, machine, null, null, identity, featureNetwork);
                    machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "factory-null", "factory");
                    continue;
                }
                var bootstrap = ComposeBootstrap(machine, feature, generation, identity, featureNetwork);
                FeatureStartResult result;
                try { result = module.Start(bootstrap); }
                catch (Exception error)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=failed stage=start feature=" + feature.Value + " errorType=" + error.GetType().Name + " message=" + error.Message);
                    TrackEntry(feature, machine, null, bootstrap.Lifetime, identity, featureNetwork);
                    machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "errorType=" + error.GetType().Name, "start");
                    continue;
                }
                if (!result.Started)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=not-started feature=" + feature.Value + " diagnostic=" + result.DiagnosticId);
                    TrackEntry(feature, machine, null, bootstrap.Lifetime, identity, featureNetwork);
                    machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, result.DiagnosticId, "start-result");
                    continue;
                }
                machine.CompleteStart(feature, result.DiagnosticId);
                TrackEntry(feature, machine, module, bootstrap.Lifetime, identity, featureNetwork);
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=started feature=" + feature.Value + " diagnostic=" + result.DiagnosticId);
            }
        }

        /// <summary>
        /// The frozen stop handoff for every host-tracked module: BeginStop
        /// (Running→Stopping on the machine), module.Stop FIRST (its own
        /// in-Stop publishes still dispatch — a Stop fault never skips the
        /// cleanup), then the machine's CompleteStop drops that feature's bus
        /// subscriptions and disposes its tracked resources in reverse
        /// registration order (other features and HostTick subscriptions are
        /// untouched). Entries stay in the tracked set (marked stopped) so the
        /// panel enable seam can re-arm them with a new generation.
        /// </summary>
        internal static void StopAll(FeatureStopReason reason)
        {
            StartedModule[] snapshot;
            lock (sync)
            {
                snapshot = started.ToArray();
            }
            for (var i = 0; i < snapshot.Length; i++)
            {
                var entry = snapshot[i];
                if (entry.Stopped) continue;
                StopEntry(entry, reason);
            }
        }

        /// <summary>
        /// DEV-V3-03: the panel enable/disable command seam (面板=command
        /// adapter, the machine owns the real transitions). Disable stops the
        /// feature with FeatureStopReason.UserDisabled; enable re-arms it
        /// through the SAME registration factory with a NEW lifecycle
        /// generation. Isolated features do NOT auto-restart — only this
        /// explicit user-driven call moves them back to Starting. Official and
        /// ecosystem features share this one seam (契约面同权).
        /// </summary>
        internal static bool SetFeatureEnabled(FeatureId feature, bool enabled)
        {
            if (string.IsNullOrEmpty(feature.Value))
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=rejected reason=empty-feature");
                return false;
            }
            StartedModule entry;
            lock (sync)
            {
                entry = FindEntryLocked(feature.Value);
            }
            if (!enabled)
            {
                if (entry == null || entry.Stopped || entry.Module == null)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=disable-rejected feature=" + feature.Value + " reason=not-running");
                    return false;
                }
                StopEntry(entry, FeatureStopReason.UserDisabled);
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=user-disabled feature=" + feature.Value);
                return true;
            }
            if (entry == null)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=enable-rejected feature=" + feature.Value + " reason=unknown-feature");
                return false;
            }
            var machine = entry.Machine;
            var state = machine.CurrentStatus(feature).State;
            if (state != FeatureState.Stopped && state != FeatureState.Isolated && state != FeatureState.Disabled)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=enable-rejected feature=" + feature.Value + " reason=invalid-state state=" + state);
                return false;
            }
            if (!machine.BeginStart(feature, out var generation)) return false;
            if (!machine.TryGetModuleFactory(feature, out var factory))
            {
                machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "factory-missing", "factory");
                entry.Stopped = true;
                return false;
            }
            IFeatureModule module = null;
            try { module = factory.Create(); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=enable-failed stage=factory feature=" + feature.Value + " errorType=" + error.GetType().Name);
                machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "errorType=" + error.GetType().Name, "factory");
                entry.Stopped = true;
                return false;
            }
            if (module == null)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=enable-failed stage=factory feature=" + feature.Value + " reason=factory-null");
                machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "factory-null", "factory");
                entry.Stopped = true;
                return false;
            }
            var bootstrap = ComposeBootstrap(machine, feature, generation, entry.Identity, entry.Network);
            FeatureStartResult result;
            try { result = module.Start(bootstrap); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=enable-failed stage=start feature=" + feature.Value + " errorType=" + error.GetType().Name + " message=" + error.Message);
                machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "errorType=" + error.GetType().Name, "start");
                entry.Stopped = true;
                return false;
            }
            if (!result.Started)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=enable-failed stage=start feature=" + feature.Value + " diagnostic=" + result.DiagnosticId);
                machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, result.DiagnosticId, "start-result");
                entry.Stopped = true;
                return false;
            }
            machine.CompleteStart(feature, result.DiagnosticId);
            lock (sync)
            {
                entry.Module = module;
                entry.LifetimeView = bootstrap.Lifetime;
                entry.Stopped = false;
            }
            BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=user-enabled feature=" + feature.Value + " generation=" + generation);
            return true;
        }

        private static StartedModule FindEntryLocked(string featureValue)
        {
            for (var i = 0; i < started.Count; i++)
            {
                if (string.Equals(started[i].Feature.Value, featureValue, StringComparison.Ordinal)) return started[i];
            }
            return null;
        }

        private static void TrackEntry(FeatureId feature, FeatureLifecycleRuntime machine, IFeatureModule module, IFeatureLifetime lifetimeView, FeatureScopeIdentity identity, IBueNetworkApi network)
        {
            lock (sync)
            {
                var existing = FindEntryLocked(feature.Value);
                if (existing != null)
                {
                    existing.Module = module;
                    existing.LifetimeView = lifetimeView;
                    existing.Stopped = module == null;
                    return;
                }
                started.Add(new StartedModule
                {
                    Feature = feature,
                    Machine = machine,
                    Module = module,
                    LifetimeView = lifetimeView,
                    Identity = identity,
                    Network = network,
                    Stopped = module == null
                });
            }
        }

        private static void StopEntry(StartedModule entry, FeatureStopReason reason)
        {
            if (!entry.Machine.BeginStop(entry.Feature, reason))
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-stop result=skipped feature=" + entry.Feature.Value + " reason=invalid-state");
                return;
            }
            if (entry.Module != null)
            {
                try { entry.Module.Stop(reason); }
                catch (Exception error)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-stop result=failed feature=" + entry.Feature.Value + " errorType=" + error.GetType().Name);
                }
            }
            entry.Machine.CompleteStop(entry.Feature);
            lock (sync)
            {
                entry.Module = null;
                entry.Stopped = true;
            }
        }

        /// <summary>
        /// The bootstrap composition for one (feature, generation): Identity
        /// binds the registration's own definition set; Events/OwnedEvents/
        /// EventRegistry are the host bus views; Dependencies and Lifetime are
        /// the machine's wired views (DEV-V3-03 matrix rows); Settings and
        /// Logger stay null until their tickets (06/07).
        /// </summary>
        private static FeatureBootstrap ComposeBootstrap(FeatureLifecycleRuntime machine, FeatureId feature, ulong generation, FeatureScopeIdentity identity, IBueNetworkApi featureNetwork)
        {
            var bus = BueHostEventRuntime.Bus;
            return new FeatureBootstrap(
                identity,
                generation,
                null,
                bus.Subscriber(feature),
                bus.Publisher(feature),
                bus.EventRegistry(feature),
                null,
                machine.CreateDependenciesView(),
                machine.CreateLifetimeView(feature, generation),
                featureNetwork);
        }
    }
}
