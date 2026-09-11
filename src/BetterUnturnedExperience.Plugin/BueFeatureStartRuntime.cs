using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Diagnostics;
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
    /// non-null from here on; DEV-V3-04 additionally wires MainThread (the
    /// platform dispatcher view — generation-opened per start, owner-
    /// invalidated at every stop/isolation boundary, host-shutdown at
    /// teardown); DEV-V3-06 wires Settings (the scoped settings view — one
    /// host-owned SettingsRuntime per facet-registered feature, generation-
    /// opened per start, owner-invalidated at every stop/isolation boundary;
    /// features WITHOUT a settings facet keep the honest null row: nothing
    /// provided, nothing faked); DEV-V3-07 wires Logger (the feature-bound
    /// diagnostic view — every started feature is wired, the matrix row has
    /// no facet gate; the generation ledger withdraws at the same stop/
    /// isolation boundaries the other written seams use).
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
            // DEV-V3-07 收编双绑：原行路径逐字不变（BueRuntimeLog.Runtime=03
            // 既有锚语义），附加 AggregateHostLine 把带码行进统一摘要——T4 隔离/
            // 状态投影进统一诊断 sink（分 seam 判据可定位，不互相遮蔽）。
            var machine = new FeatureLifecycleRuntime(runtime, BueHostEventRuntime.Bus, line =>
            {
                BueRuntimeLog.Runtime(line);
                BueDiagnosticsRuntime.AggregateHostLine("bue.host", DiagnosticLevel.Info, line);
            });
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
                var bootstrap = ComposeBootstrap(machine, feature, generation, identity, featureNetwork, entry.SettingDescriptors);
                FeatureStartResult result;
                try { result = module.Start(bootstrap); }
                catch (Exception error)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=failed stage=start feature=" + feature.Value + " errorType=" + error.GetType().Name + " message=" + error.Message);
                    TrackEntry(feature, machine, null, bootstrap.Lifetime, identity, featureNetwork);
                    machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "errorType=" + error.GetType().Name, "start");
                    InvalidateMainThread(feature, "start-fault"); // DEV-V3-04: 隔离撤投递账（隔离后不可再投递）
                    BueSettingsRuntime.InvalidateOwner(feature, "start-fault"); // DEV-V3-06: 隔离撤设置代际（写入失效）
                    BueDiagnosticsRuntime.InvalidateOwner(feature, "start-fault"); // DEV-V3-07: 隔离撤诊断代际（捕获 view 再写=边界拒+留痕）
                    continue;
                }
                if (!result.Started)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=not-started feature=" + feature.Value + " diagnostic=" + result.DiagnosticId);
                    TrackEntry(feature, machine, null, bootstrap.Lifetime, identity, featureNetwork);
                    machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, result.DiagnosticId, "start-result");
                    InvalidateMainThread(feature, "start-result");
                    BueSettingsRuntime.InvalidateOwner(feature, "start-result"); // DEV-V3-06: 同上
                    BueDiagnosticsRuntime.InvalidateOwner(feature, "start-result"); // DEV-V3-07: 同上
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
            // DEV-V3-04: the host stop boundary — after every module stopped,
            // the dispatcher refuses all posts and drains un-executed (宿主
            // 停止后投递=显式失败). A later catalog start (same-process
            // reload) reopens a generation through ComposeBootstrap.
            if (reason == FeatureStopReason.PluginStopping)
            {
                BueMainThreadRuntime.Shutdown("plugin-stopping");
                BueDiagnosticsRuntime.ShutdownHost("plugin-stopping"); // DEV-V3-07: 宿主停止后捕获 view 写入=拒+留痕（不静默吞）
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
            // DEV-V3-06: the settings facet comes from the feature's OWN
            // registration record — the new generation is composed against
            // the same schema and the same single source (no restart resets
            // a revision, no stale table feeds a second one).
            machine.TryGetSettingsFacet(feature, out var settingsDescriptors, out _);
            var bootstrap = ComposeBootstrap(machine, feature, generation, entry.Identity, entry.Network, settingsDescriptors);
            FeatureStartResult result;
            try { result = module.Start(bootstrap); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=enable-failed stage=start feature=" + feature.Value + " errorType=" + error.GetType().Name + " message=" + error.Message);
                machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, "errorType=" + error.GetType().Name, "start");
                InvalidateMainThread(feature, "enable-fault");
                BueSettingsRuntime.InvalidateOwner(feature, "enable-fault"); // DEV-V3-06: 隔离撤设置代际
                BueDiagnosticsRuntime.InvalidateOwner(feature, "enable-fault"); // DEV-V3-07: 隔离撤诊断代际
                entry.Stopped = true;
                return false;
            }
            if (!result.Started)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=feature-panel result=enable-failed stage=start feature=" + feature.Value + " diagnostic=" + result.DiagnosticId);
                machine.Isolate(feature, FrameworkErrorCode.ModuleStartFailed, result.DiagnosticId, "start-result");
                InvalidateMainThread(feature, "enable-not-started");
                BueSettingsRuntime.InvalidateOwner(feature, "enable-not-started"); // DEV-V3-06: 同上
                BueDiagnosticsRuntime.InvalidateOwner(feature, "enable-not-started"); // DEV-V3-07: 同上
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

        /// <summary>
        /// DEV-V3-06: the live state projection for panel entries (面板状态侧
        /// 非第二事实源) — entries mirror the host machine's truth, never a
        /// panel-side copy. false for features the start path never tracked
        /// (the display keeps its own default then).
        /// </summary>
        internal static bool TryGetStatus(FeatureId feature, out FeatureStatusView status)
        {
            lock (sync)
            {
                var entry = FindEntryLocked(feature.Value);
                if (entry != null && entry.Machine != null)
                {
                    status = entry.Machine.CurrentStatus(feature);
                    return true;
                }
            }
            status = default(FeatureStatusView);
            return false;
        }

        private static void TrackEntry(FeatureId feature, FeatureLifecycleRuntime machine, IFeatureModule module, IFeatureLifetime lifetimeView, FeatureScopeIdentity identity, IBueNetworkApi network)
        {
            lock (sync)
            {
                var existing = FindEntryLocked(feature.Value);
                if (existing != null)
                {
                    // A re-cataloged start (a fresh runtime composition) re-arms
                    // the entry with the CURRENT generation's assembly: the
                    // machine that drove this module's BeginStart/Start owns its
                    // stop boundary. Keeping a stale machine here made BeginStop
                    // reject with invalid-state and skip UnsubscribeAll — the new
                    // generation's bus subscriptions leaked past the stop edge
                    // (DEV-V3-05 clock-subgroup red: 停止边界自动注销). Identity
                    // and Network move with the machine for the same reason (the
                    // module's bootstrap was composed from them).
                    existing.Machine = machine;
                    existing.Identity = identity;
                    existing.Network = network;
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
            // DEV-V3-04: 停止边界——该功能的待处理主线程任务全部撤账（未执行
            // 任务不再执行），之后的投递=显式失败（矩阵 MainThread 行为面）。
            InvalidateMainThread(entry.Feature, reason.ToString());
            // DEV-V3-06: 停止边界——设置写入代际失效（捕获 view 之后的写入=
            // 显式拒 BUE-SET-001；读取仍如实，矩阵 Settings 行为面）。
            BueSettingsRuntime.InvalidateOwner(entry.Feature, reason.ToString());
            // DEV-V3-07: 停止边界——诊断写入代际失效（捕获 view 之后的写入=
            // 不产模块行+BUE-LOG-004 留痕一条；再启用新代际恢复，矩阵 Logger
            // 行为面）。Stop 返回前不撤账=LIT 停止行仍经 view（Settings 同构）。
            BueDiagnosticsRuntime.InvalidateOwner(entry.Feature, reason.ToString());
            lock (sync)
            {
                entry.Module = null;
                entry.Stopped = true;
            }
        }

        private static void InvalidateMainThread(FeatureId feature, string reason)
        {
            try { BueMainThreadRuntime.Dispatcher.InvalidateOwner(feature, reason); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-V2HOST] event=main-thread result=invalidate-failed feature=" + feature.Value
                    + " reason=" + reason + " errorType=" + error.GetType().Name);
            }
        }

        /// <summary>
        /// The bootstrap composition for one (feature, generation): Identity
        /// binds the registration's own definition set; Events/OwnedEvents/
        /// EventRegistry are the host bus views; Dependencies and Lifetime are
        /// the machine's wired views (DEV-V3-03 matrix rows); MainThread is
        /// the platform dispatcher view for this generation (DEV-V3-04 matrix
        /// row — opening the generation supersedes any older one, so a
        /// re-enabled feature's stale pending work never executes); Settings
        /// is the scoped settings view for this generation (DEV-V3-06 matrix
        /// row — the feature's OWN facet schema composes the one host-owned
        /// SettingsRuntime; no facet = the honest null, nothing faked); Logger
        /// is the feature-bound diagnostic view for this generation
        /// (DEV-V3-07 matrix row — wired for every started feature, the
        /// generation ledger opens here and withdraws at every stop/
        /// isolation boundary).
        /// </summary>
        private static FeatureBootstrap ComposeBootstrap(FeatureLifecycleRuntime machine, FeatureId feature, ulong generation, FeatureScopeIdentity identity, IBueNetworkApi featureNetwork,
            System.Collections.Generic.IReadOnlyList<Contracts.SettingDescriptor> settingsDescriptors)
        {
            var bus = BueHostEventRuntime.Bus;
            var dispatcher = BueMainThreadRuntime.Dispatcher;
            dispatcher.OpenGeneration(feature, generation);
            return new FeatureBootstrap(
                identity,
                generation,
                BueSettingsRuntime.ComposeViewForStart(feature, settingsDescriptors, generation),
                bus.Subscriber(feature),
                bus.Publisher(feature),
                bus.EventRegistry(feature),
                BueDiagnosticsRuntime.ComposeViewForStart(feature, generation), // DEV-V3-07: Logger matrix row — every started feature is wired (no facet gate; unlike Settings there is nothing to withhold)
                machine.CreateDependenciesView(),
                machine.CreateLifetimeView(feature, generation),
                featureNetwork,
                dispatcher.CreateView(feature, generation));
        }
    }
}
