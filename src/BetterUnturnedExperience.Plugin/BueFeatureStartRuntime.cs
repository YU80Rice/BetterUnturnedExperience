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
    /// generation is host-allocated monotonic. Started modules are tracked
    /// per host generation so teardown runs the frozen stop handoff
    /// (FeatureEventBus class contract): module.Stop FIRST, and only after
    /// Stop RETURNS the host drops that feature's bus subscriptions — the
    /// module's final in-Stop publish still dispatches, and nothing of the
    /// feature survives the stop boundary.
    /// </summary>
    internal static class BueFeatureStartRuntime
    {
        private static readonly object sync = new object();
        private static ulong nextGeneration;
        private static readonly List<StartedModule> started = new List<StartedModule>();

        private sealed class StartedModule
        {
            internal FeatureId Feature;
            internal IFeatureModule Module;
        }

        /// <summary>
        /// Starts every catalog module once the host barrier has completed.
        /// A factory that throws or yields null skips that feature (host log,
        /// never a host crash); a module whose Start reports not-started is
        /// not tracked. The bootstrap identity binds the feature's own
        /// registration identity (DEV-V3-01 availability matrix); the
        /// bootstrap carries the composed service seams.
        /// </summary>
        internal static void StartCatalog(FeatureRegistrationRuntime runtime, IBueNetworkApi featureNetwork)
        {
            if (runtime == null || runtime.Catalog == null || featureNetwork == null) return;
            var entries = runtime.Catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var feature = entry.Definition.Feature;
                IFeatureModule module = null;
                try { module = entry.ModuleFactory.Create(); }
                catch (Exception error)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=failed stage=factory feature=" + feature.Value + " errorType=" + error.GetType().Name);
                    continue;
                }
                if (module == null) continue;
                var bus = BueHostEventRuntime.Bus;
                // DEV-V3-01 ten-member availability matrix: Identity,
                // LifecycleGeneration, Events, OwnedEvents and Network are the
                // five frozen never-null members (Identity binds the
                // registration's own definition set). DEV-V3-02 adds the
                // EventRegistry row: the feature's own event-type ownership
                // registration view, composed non-null from this ticket on.
                // The remaining un-wired members stay null — Lifetime/
                // Dependencies wire with DEV-V3-03, MainThread with DEV-V3-04,
                // Settings with DEV-V3-06, Logger with DEV-V3-07.
                var definition = entry.Definition;
                var identity = new FeatureScopeIdentity(definition.Feature, null, null, definition.DefinitionSetId, definition.DefinitionSetDigest);
                var bootstrap = new FeatureBootstrap(
                    identity,
                    NextGeneration(),
                    null,
                    bus.Subscriber(feature),
                    bus.Publisher(feature),
                    bus.EventRegistry(feature),
                    null,
                    null,
                    null,
                    featureNetwork);
                FeatureStartResult result;
                try { result = module.Start(bootstrap); }
                catch (Exception error)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=failed stage=start feature=" + feature.Value + " errorType=" + error.GetType().Name + " message=" + error.Message);
                    continue;
                }
                if (!result.Started)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=not-started feature=" + feature.Value + " diagnostic=" + result.DiagnosticId);
                    continue;
                }
                lock (sync) started.Add(new StartedModule { Feature = feature, Module = module });
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=started feature=" + feature.Value + " diagnostic=" + result.DiagnosticId);
            }
        }

        /// <summary>
        /// The frozen stop handoff for every host-tracked module: Stop first
        /// (its own in-Stop publishes still dispatch), then UnsubscribeAll on
        /// the host bus for exactly that feature (other features and HostTick
        /// subscriptions are untouched).
        /// </summary>
        internal static void StopAll(FeatureStopReason reason)
        {
            StartedModule[] snapshot;
            lock (sync)
            {
                snapshot = started.ToArray();
                started.Clear();
            }
            for (var i = 0; i < snapshot.Length; i++)
            {
                var record = snapshot[i];
                try { record.Module.Stop(reason); }
                catch (Exception error)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-stop result=failed feature=" + record.Feature.Value + " errorType=" + error.GetType().Name);
                }
                try { BueHostEventRuntime.Bus.UnsubscribeAll(record.Feature); }
                catch (Exception) { }
            }
        }

        private static ulong NextGeneration()
        {
            lock (sync)
            {
                nextGeneration++;
                return nextGeneration;
            }
        }
    }
}
