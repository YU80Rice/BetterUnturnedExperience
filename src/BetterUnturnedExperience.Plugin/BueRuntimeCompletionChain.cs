using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// F-D: the runtime completion drive, staticized so it survives the
    /// plugin host component being destroyed. On the U3DS headless boot the
    /// game destroyed BepInEx_Manager between Awake and the first
    /// Start/Update frame; the old instance-bound barrier died with it
    /// (OnDestroy cleared the runtime host and unsubscribed the scene drive)
    /// and the runtime never completed — no ready line, no module starts,
    /// no network arm. The chain lives in static state the destroyer cannot
    /// reach: the scene-loaded core completes an open runtime on any later
    /// drive, the runtime host is preserved across a non-quit sweep (the
    /// same preservation family as the Harmony patches, [R19]), and quit
    /// teardown still detaches everything explicitly.
    ///
    /// Test safety mirrors the reconciler seam discipline: OnSceneLoadedCore
    /// and TryCompleteRuntimeCore are pure (engine-free) and are the only
    /// members tests invoke; every engine-touching member is reached only
    /// through delegates bound in Awake (SceneLoadedUnsubscriber /
    /// HeadlessPumpHealer), so a null binding is a silent no-op and no test
    /// path ever JITs a Unity-touching method.
    /// </summary>
    internal static class BueRuntimeCompletionChain
    {
        internal static bool HeadlessDecision;

        // Bound by the plugin instance in Awake (client branch): refreshes the
        // management panel after completion. Null on headless and in tests.
        internal static Action CompletionRefreshHook = null;

        // Bound in Awake (headless branch): re-attaches the DDOL survival pump
        // when it has been destroyed. Invoked on every scene drive.
        internal static Action HeadlessPumpHealer = null;

        // Bound in Awake: unsubscribes the Unity scene-loaded handler. Invoked
        // ONLY by quit teardown — the completion path intentionally keeps the
        // subscription alive (it is idempotent and doubles as the heal drive).
        internal static Action SceneLoadedUnsubscriber = null;

        internal static BueRuntimeCompletionBarrier CompletionBarrier = null;
        internal static bool RuntimeReadyLogged;

        /// <summary>Test alias of <see cref="Reset"/> — fresh chain per group.</summary>
        internal static void ResetForTests()
        {
            Reset();
        }

        /// <summary>
        /// The scene-drive core (engine-free): heal the survival pump, then
        /// try to complete the runtime. Pure, idempotent, never throws.
        /// </summary>
        internal static void OnSceneLoadedCore()
        {
            var heal = HeadlessPumpHealer;
            if (heal != null)
            {
                try { heal(); }
                catch (Exception error)
                {
                    BueRuntimeLog.Warn("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience event=headless-pump-heal-failed errorType=" + error.GetType().Name + " diagnosticId=BUE-BOOTSTRAP-003");
                }
            }
            TryCompleteRuntimeCore();
        }

        /// <summary>
        /// The one-shot completion drive. Returns true when the runtime is
        /// complete (already or by this call). Announces ready exactly once.
        /// </summary>
        internal static bool TryCompleteRuntimeCore()
        {
            if (RuntimeReadyLogged) return true;
            if (CompletionBarrier == null)
                CompletionBarrier = new BueRuntimeCompletionBarrier(CompleteRuntimeOnceCore, LogRuntimeCompletionIsolated);
            if (!CompletionBarrier.TryComplete()) return false;
            RuntimeReadyLogged = true;
            BueRuntimeLog.AnnounceReady(HeadlessDecision);
            return true;
        }

        private static bool CompleteRuntimeOnceCore()
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Phase != FeatureRegistrationPhase.RegistrationOpen) return false;
            if (!runtime.CompleteRuntime()) return false;
            // DEV-V2-21: the host barrier composes the module start path —
            // each catalog module receives its FeatureBootstrap (stable
            // feature network facade + host bus views) and starts here, not
            // at registration time.
            TryStartRegisteredModulesCore();
            var refresh = CompletionRefreshHook;
            if (refresh != null)
            {
                try { refresh(); }
                catch (Exception error)
                {
                    BueRuntimeLog.Warn("BUE client UI refresh after completion isolated errorType=" + error.GetType().FullName + " diagnosticId=BUE-CLIENTUI-004");
                }
            }
            return true;
        }

        private static void TryStartRegisteredModulesCore()
        {
            try
            {
                var featureNetwork = NetworkModuleFeatureRegistration.WiredAdapter?.FeatureNetworkApi;
                if (featureNetwork == null)
                {
                    BueRuntimeLog.Runtime("[BUE-V2HOST] event=module-start result=skipped reason=feature-network-unavailable");
                    return;
                }
                BueFeatureStartRuntime.StartCatalog(BueRuntimeHost.CurrentRuntime, featureNetwork);
                // DEV-V4-04：目录启动完成后跑官方 legacy enabled 迁移——机解释
                // 停用目标（Running→停 / Isolated→空操作成功），意图事实落盘，
                // 旧键退役。故障自隔离，绝不打穿完成链。
                BueLegacyEnabledMigrationAdapter.RunProduction();
            }
            catch (Exception error)
            {
                BueRuntimeLog.Warn("[BUE-V2HOST] event=module-start result=failed errorType=" + error.GetType().Name + " message=" + error.Message);
            }
        }

        private static void LogRuntimeCompletionIsolated(Exception error)
        {
            BueRuntimeLog.ErrorFriendly("Better Unturned Experience featureId=io.github.yu80rice.betterunturnedexperience status=RuntimeCompletionIsolated decision=Isolate errorType=" + error.GetType().FullName + " diagnosticId=BUE-BOOTSTRAP-001 message=" + error.Message);
        }

        /// <summary>
        /// Quit-path teardown: detach the scene drive, drop every seam
        /// binding, forget the barrier state, and clear the runtime host.
        /// The non-quit host-sweep path must NOT call this — preservation is
        /// the whole point of the chain.
        /// </summary>
        internal static void TeardownForQuit()
        {
            var unsub = SceneLoadedUnsubscriber;
            if (unsub != null)
            {
                try { unsub(); }
                catch (Exception error)
                {
                    BueRuntimeLog.Warn("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience event=teardown-unsubscribe-failed errorType=" + error.GetType().Name + " diagnosticId=BUE-BOOTSTRAP-001");
                }
            }
            Reset();
            BueRuntimeTickChain.Reset();
            BueRuntimeHost.Clear();
        }

        /// <summary>
        /// Drops every seam binding and forgets the barrier state. Used by
        /// quit teardown (production) and by tests (fresh chain per group).
        /// </summary>
        internal static void Reset()
        {
            HeadlessDecision = false;
            CompletionRefreshHook = null;
            HeadlessPumpHealer = null;
            SceneLoadedUnsubscriber = null;
            CompletionBarrier = null;
            RuntimeReadyLogged = false;
        }
    }

    /// <summary>
    /// F-D: the shared per-frame runtime chain (mirror retry → network pump →
    /// host clock → tidy dispatcher → completion drive), frame-deduped so any
    /// number of surviving drivers (plugin Update, client panel pump,
    /// headless survival pump) produce exactly one execution per frame. The
    /// headless survival pump carries the chain when the plugin host
    /// component has been destroyed; the frame guard keeps a healthy host
    /// from double-ticking when both Update and the pump are alive.
    /// </summary>
    internal static class BueRuntimeTickChain
    {
        // Production binds () => Time.frameCount in Awake; tests bind a
        // counter. Null disables deduplication (every Tick runs).
        internal static Func<int> FrameProvider = null;

        // -1 sentinel: frame 0 (a real Unity first frame) must tick.
        private static int lastChainFrame = -1;

        internal static void Tick()
        {
            var provider = FrameProvider;
            if (provider != null)
            {
                var frame = provider();
                if (frame == lastChainFrame) return;
                lastChainFrame = frame;
            }
            // DEV-V2-10 F-A: deferred V1 mirror retry (throttled inside the
            // adapter). DEV-V2-18: engine peer-state diff, inbound frame
            // dispatch, handshake re-probe. DEV-V2-19: the host clock beat.
            // DEV-V2-21: the tidy dispatcher pump. DEV-V3-04: the platform
            // main-thread dispatcher beat (after the clock, same chain). All
            // fault-isolated inside their adapters; never throws into this
            // chain.
            NetworkModuleFeatureRegistration.WiredAdapter?.RetryPendingMirror();
            NetworkModuleFeatureRegistration.WiredAdapter?.TickNetwork();
            BueHostEventRuntime.TickOnce();
            // DEV-V3-04: the platform main-thread dispatcher beat — the ONE
            // sanctioned pump (features post through bootstrap.MainThread and
            // never build pumps of their own). Runs after the host clock so a
            // feature's per-beat posts execute in the same chain beat; fault-
            // isolated inside the composition root, never throws here.
            BueMainThreadRuntime.TickOnce();
            // DEV-V6-11: the tidy tick pump is RETIRED — the tidy feature rides
            // the frozen HostTick seam published by the host clock above (the
            // LIR/LHT shape). The host no longer holds or drives that feature's
            // run instance, so nothing here names it at all.
            BueRuntimeCompletionChain.TryCompleteRuntimeCore();
        }

        /// <summary>Quit teardown companion: drop the frame sentinel and the
        /// provider binding so a later domain reuse starts clean.</summary>
        internal static void Reset()
        {
            FrameProvider = null;
            lastChainFrame = -1;
        }
    }
}
