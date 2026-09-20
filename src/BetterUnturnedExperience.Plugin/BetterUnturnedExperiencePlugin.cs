using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.ClientUi;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Placement;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lit;
using BetterUnturnedExperience.Lir;
using BetterUnturnedExperience.Lht;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterUnturnedExperience.Plugin
{
    [BepInPlugin("io.github.yu80rice.betterunturnedexperience", "Better Unturned Experience", "0.0.0")]
    public sealed class BetterUnturnedExperiencePlugin : BaseUnityPlugin
    {
        private const string FeatureId = "io.github.yu80rice.betterunturnedexperience";
        private const string DiagnosticId = "BUE-BOOTSTRAP-001";
        private int updateTickCount;
        private int runtimePumpTickCount;
        private bool runtimePumpIsolated;
        // DEV-V6-02E: the UI ring lives behind the UI's one public assembly
        // type (ClientUiFeatureAssembly) — the entry no longer holds the
        // composition, native panel or inventory adapters directly.
        private BueDoubleInstallReport doubleInstallReport;
        private readonly BueRuntimePumpSlot runtimePumpSlot = new BueRuntimePumpSlot();
        private BueRuntimePump runtimePump;
        private BueRuntimePumpBehaviour runtimePumpBehaviour;
        private BuePluginUpdateDriver pluginUpdateDriver;
        // F-D: the headless survival pump — an independent DDOL GameObject
        // (ordinary objects survive the host sweep on 3.26.3.9 where
        // HideAndDontSave ones do not) driving the shared tick chain.
        private BueRuntimePump headlessPump;
        private BueRuntimePumpBehaviour headlessPumpBehaviour;

        // DEV-V6-02E: the drag-adapter activation decision moved with the
        // adapters into the UI composition (BueClientUiCompositionRoot
        // .ShouldActivateDragPreview); the entry no longer owns it.

        private void Awake()
        {
            // [R19] Subscribe before anything can fail: the quit flag decides
            // whether OnDestroy preserves or tears down the panel state.
            Application.quitting += OnApplicationQuitting;
            try
            {
                DontDestroyOnLoad(gameObject);
                enabled = true;
                LogAssemblyIdentity();
                BueRuntimeLog.Bind(Logger);
                // F-B1c: bind the listen-host projection reconciler's engine
                // path here (and only here) so the tidy-publish and
                // inventory-open dispatchers stay silent no-ops on the host
                // test path — no test ever JITs an SDG-touching method.
                // DEV-V6-02E: the bind rides the UI's public face (the static
                // seams live in the UI project now); production-only call.
                ClientUiFeatureAssembly.BindEngineDispatcher();
                // DEV-V6-02B (V6-T2 硬项拆法): bind the tidy feature's
                // projection-reconcile relay to the same reconciler — the tidy
                // commit pages travel through this host-owned port (the feature
                // no longer names the UI layer); unbound (test host) the relay
                // is a silent no-op, same family as the engine dispatcher above.
                // DEV-V6-02E: the receive side rides the UI's public seam.
                LitFeatureAssembly.BindProjectionRelay((firstPage, lastPage) =>
                    ClientUiFeatureAssembly.OnTidyPagesCommitted(firstPage, lastPage));
                // DEV-V6-02B: bind the tidyable page range from the tidy
                // feature's public seam — the UI layer's clamps stay
                // single-sourced without naming the feature project.
                ClientUiFeatureAssembly.BindTidyableRange(
                    LitFeatureAssembly.TidyablePageMin, LitFeatureAssembly.TidyablePageMax);
                // DEV-V2-23: the platform double-install self-check — a
                // diagnostic-only BUE-PLATFORM-001 scan of the assemblies already
                // in the AppDomain. It must never block bootstrap and never
                // deletes user files (处置留给用户); panel visibility rides the
                // report below in the client branch.
                RunPlatformDoubleInstallSelfCheck();
                // DEV-V2-19: compose the host event bus + host clock once at
                // bootstrap; the Update chain drives the clock (TickOnce) and
                // feature modules subscribe through the frozen seams.
                BueHostEventRuntime.EnsureCreated();
                var isBatchMode = Application.isBatchMode;
                var decision = BootstrapGuard.Decide(isBatchMode, isBatchMode, !isBatchMode);
                // F-D: the completion chain is static — it survives the plugin
                // host component being destroyed mid-boot (the U3DS sweep).
                BueRuntimeCompletionChain.HeadlessDecision = decision == BootstrapDecision.Headless;
                BueRuntimeCompletionChain.SceneLoadedUnsubscriber = UnsubscribeSceneLoadedStatic;
                BueRuntimeTickChain.FrameProvider = () => Time.frameCount;
                EnsureSceneLoadedSubscribedStatic();
                BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-BOOTSTRAP-002 event=runtime-gate decision=" + decision + " batchMode=" + isBatchMode + " headless=" + isBatchMode);
                // DEV-V3-06: compose the platform settings service BEFORE any
                // registration/adapter path — the facet schemas, the start
                // views and the panel routing all resolve to this ONE default
                // registry (single source of truth, per-feature runtime).
                BueSettingsRuntime.EnsureCreated(BueSettingsRuntime.ProductionSettingsRoot);
                // DEV-V4-04: compose the durable lifecycle intent store on the
                // SAME root — the machine's disable/enable hooks and the legacy
                // enabled migration all resolve this one store (first-wins).
                BueFeatureIntentRuntime.EnsureCreated(BueSettingsRuntime.ProductionSettingsRoot);
                // DEV-V6-03: the runtime comes from the host factory — it is the
                // one site that binds the phase observation mouth (the freeze
                // step inside the production ready entry is observable only
                // through it).
                var runtime = BueRuntimeHost.CreateRuntime();
                BueRuntimeHost.Bind(runtime);
                runtime.OpenRegistration();
                var officialRegistration = BetterItemInteractionFeatureRegistration.Register();
                // DEV-V2-06: the network module (LMN takeover + BueNetworkApi)
                // registers in the global zone — Headless hosts need the
                // network module too; the client-only branch below only adds
                // the panel wiring.
                var networkRegistration = NetworkModuleFeatureRegistration.Register();
                // DEV-V2-15: the official inventory-tidy module registers in
                // the global zone the same way; the tidy button patch and
                // the local single-player transaction arm here.
                var litRegistration = InventoryTidyFeatureRegistration.Register();
                // DEV-V2-22: the official in-place-reload module registers in
                // the global zone too; its frame work rides the host clock
                // and its patches live under its own FeatureId Harmony ID.
                var lirRegistration = InPlaceReloadFeatureRegistration.Register();
                // DEV-V2-20: the official horde-tracker module registers in
                // the global zone the same way — the platform network facade's
                // horde broadcast consumer, riding the host clock.
                var lhtRegistration = HordeTrackerFeatureRegistration.Register();
                if (decision == BootstrapDecision.Headless)
                {
                    // F-D: the survival pump doubles as a completion driver —
                    // on U3DS the plugin host can be destroyed before the
                    // first Start/Update frame, and the pump (independent DDOL
                    // ordinary object) then carries the whole chain. The scene
                    // drive re-attaches it whenever it is found destroyed.
                    EnsureHeadlessSurvivalPump();
                    BueRuntimeCompletionChain.HeadlessPumpHealer = EnsureHeadlessSurvivalPump;
                }
                if (decision == BootstrapDecision.Client)
                {
                    pluginUpdateDriver = new BuePluginUpdateDriver(OnPluginUpdateTick);
                    // DEV-V6-02E: the UI ring assembles through the UI's ONE public
                    // assembly type. BindHostComposition injects every host mouth
                    // and fact (log policy, panel log source, enable/disable ONE
                    // seam, machine facts, display names, direct-presentation set,
                    // facet snapshots, the catalog projection and the settings
                    // routes) — the entry no longer names any UI-internal type.
                    ClientUiFeatureAssembly.BindHostComposition(
                        logRuntime: BueRuntimeLog.Runtime,
                        logWarn: BueRuntimeLog.Warn,
                        logError: BueRuntimeLog.Error,
                        logErrorFriendly: BueRuntimeLog.ErrorFriendly,
                        isCriticalNotReadyReason: BueRuntimeLog.IsCriticalNotReadyReason,
                        hostLog: Logger,
                        featureToggleHandler: BueFeatureStartRuntime.SetFeatureEnabled,
                        tryGetMachineStatus: BueClientUiHostProjection.TryGetMachineStatus,
                        getFacetSnapshot: BueClientUiHostProjection.GetFacetSnapshot,
                        getCatalogEntries: BueClientUiHostProjection.GetCatalogRows,
                        routeGetSnapshot: BueClientUiSettingsRoutes.GetSnapshotRoute(),
                        routeGetDescriptors: BueClientUiSettingsRoutes.GetDescriptorsRoute(),
                        routeApply: BueClientUiSettingsRoutes.GetApplyRoute(),
                        routeApplyBatch: BueClientUiSettingsRoutes.GetApplyBatchRoute(),
                        getNetworkFeature: () => NetworkModuleAdapter.NetworkFeature,
                        getTakeoverStatus: () => NetworkModuleFeatureRegistration.WiredAdapter?.TakeoverStatus,
                        getConfigMigrationStatus: () => NetworkModuleFeatureRegistration.WiredAdapter?.ConfigMigrationStatus,
                        isTakeoverActive: () => NetworkModuleFeatureRegistration.WiredAdapter != null && NetworkModuleFeatureRegistration.WiredAdapter.TakeoverActive,
                        refreshNetworkSwitches: () => NetworkModuleFeatureRegistration.WiredAdapter?.RefreshSwitches(),
                        isRuntimeEvent: BueRuntimeLog.IsRuntimeEvent,
                        // DEV-V6-04: the official component's lifecycle facts ride
                        // the host bridge from the Bii public seam (the component
                        // lives in the Bii project now); absent rig = null = the
                        // pre-move component==null fallback branch.
                        officialComponentState: () => BiiFeatureAssembly.ComponentState?.Invoke(),
                        officialComponentPresentation: () => BiiFeatureAssembly.ComponentPresentation?.Invoke());
                    // The placement evaluator's concrete implementation stays
                    // host-side (Core); it crosses as its contract interface.
                    if (!ClientUiFeatureAssembly.CreateComposition())
                    {
                        Logger.LogWarning("BUE client UI composition unavailable diagnosticId=BUE-CLIENTUI-001");
                    }
                    // DEV-V6-02E: the client-branch orchestration (panel, inventory
                    // adapters, diagnostic-sink tagging, wiring logs) is the UI's
                    // InitializeAndActivate behind the composition gate. The pump
                    // attaches after activation (its first tick is next frame, so
                    // the relative wiring order is unchanged).
                    else if (!ClientUiFeatureAssembly.Initialize(isBatchMode, isBatchMode, ClientUiFeatureAssembly.CanBindNativeUi()))
                    {
                        Logger.LogWarning("BUE client UI composition unavailable diagnosticId=BUE-CLIENTUI-001");
                    }
                    else
                    {
                        AttachRuntimePump();
                        // DEV-V2-23: surface the self-check finding (if any) on the
                        // management panel; the log line was emitted at scan time.
                        if (doubleInstallReport != null && doubleInstallReport.HasConflict)
                            ClientUiFeatureAssembly.SetDoubleInstallNotice(doubleInstallReport.NoticeLine);
                        // DEV-V6-04 (T3 Q2/Q5): bind the BII interface seams now that
                        // the composition is ready — the surface-opened reconcile and
                        // the settings snapshot reader. The BII module arms through
                        // the START path (the official start catalog), which runs at
                        // completion — same door as every other official feature.
                        // Absent on headless (this branch never runs) = no interface
                        // created there, no patches, no throw.
                        BiiFeatureAssembly.BindInterfaceComposition(
                            surfaceOpened: ClientUiFeatureAssembly.BiiSurfaceOpened,
                            surfaceClosed: null,
                            settingsSnapshot: ClientUiFeatureAssembly.BiiSettingsSnapshot);
                    }
                    // F-D: completion refresh rides the static chain hook (the
                    // instance may be gone by the time completion fires).
                    BueRuntimeCompletionChain.CompletionRefreshHook = RefreshManagementPanelHook;
                }
                BueRuntimeLog.Runtime("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapReady decision=" + decision + " diagnosticId=" + DiagnosticId);
                BueRuntimeLog.Runtime("Better Item Interaction featureId=" + officialRegistration.Feature.Value + " accepted=" + officialRegistration.Accepted + " reason=" + officialRegistration.Reason + " diagnosticId=" + officialRegistration.DiagnosticId);
                BueRuntimeLog.Runtime("BUE Network Module featureId=" + networkRegistration.Feature.Value + " accepted=" + networkRegistration.Accepted + " reason=" + networkRegistration.Reason + " diagnosticId=" + networkRegistration.DiagnosticId);
                BueRuntimeLog.Runtime("BUE Inventory Tidy featureId=" + litRegistration.Feature.Value + " accepted=" + litRegistration.Accepted + " reason=" + litRegistration.Reason + " diagnosticId=" + litRegistration.DiagnosticId);
                BueRuntimeLog.Runtime("BUE In-Place Reload featureId=" + lirRegistration.Feature.Value + " accepted=" + lirRegistration.Accepted + " reason=" + lirRegistration.Reason + " diagnosticId=" + lirRegistration.DiagnosticId);
                BueRuntimeLog.Runtime("BUE Horde Tracker featureId=" + lhtRegistration.Feature.Value + " accepted=" + lhtRegistration.Accepted + " reason=" + lhtRegistration.Reason + " diagnosticId=" + lhtRegistration.DiagnosticId);
            }
            catch (System.Exception error)
            {
                // DEV-16G ticket D: user-facing error lines carry the Chinese
                // "BUE 错误：" prefix in front of the structured tokens.
                BueRuntimeLog.ErrorFriendly("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapFailed diagnosticId=" + DiagnosticId + " errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
        }

        private void RunPlatformDoubleInstallSelfCheck()
        {
            try
            {
                doubleInstallReport = BuePlatformDoubleInstallCheck.Run();
            }
            catch (Exception error)
            {
                // The self-check is a diagnostic, never a gate: a scan-side
                // failure isolates under BUE-PLATFORM-002 and bootstrap continues.
                Logger.LogWarning("BUE double-install self-check isolated diagnosticId=BUE-PLATFORM-002 errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
        }

        public void Start()
        {
            BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=start-entered");
            BueRuntimeCompletionChain.TryCompleteRuntimeCore();
        }

        private void AttachRuntimePump()
        {
            if (runtimePumpBehaviour != null && runtimePumpBehaviour.gameObject != null) return;
            DestroyRuntimePump();
            try
            {
                runtimePump = runtimePumpSlot.GetOrCreate(OnRuntimePumpTick);
                runtimePumpBehaviour = BueRuntimePumpBehaviour.Attach(runtimePump);
                BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-created object=BUE.RuntimePump");
            }
            catch (Exception error)
            {
                DestroyRuntimePump();
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-create-failed errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
        }

        private void DestroyRuntimePump()
        {
            runtimePumpSlot.Clear();
            runtimePump = null;
            var behaviour = runtimePumpBehaviour;
            runtimePumpBehaviour = null;
            if (behaviour != null)
            {
                var pumpObject = behaviour.gameObject;
                if (pumpObject != null) UnityEngine.Object.Destroy(pumpObject);
            }
        }

        private void OnRuntimePumpTick()
        {
            if (runtimePumpIsolated) return;
            runtimePumpTickCount++;
            if (runtimePumpTickCount == 1)
            {
                BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-tick count=1");
            }
            try
            {
                if (!ClientUiFeatureAssembly.DispatchPanelTick(true) && ClientUiFeatureAssembly.PanelTickIsolated)
                {
                    runtimePumpIsolated = true;
                    DestroyRuntimePump();
                }
                // F-D: the client pump also carries the shared chain — it
                // survives a host sweep and is frame-deduped against Update.
                BueRuntimeTickChain.Tick();
            }
            catch (Exception error)
            {
                runtimePumpIsolated = true;
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-failed errorType=" + error.GetType().FullName + " message=" + error.Message);
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-isolated fallback=NativeUi");
            }
        }

        private void Update()
        {
            if (pluginUpdateDriver != null) pluginUpdateDriver.Update();
            // F-D: the shared per-frame chain (frame-deduped) — mirror retry,
            // network pump, host clock, tidy dispatcher, completion drive.
            // The headless survival pump carries the chain when this
            // component has been destroyed.
            BueRuntimeTickChain.Tick();
        }

        private void OnPluginUpdateTick()
        {
            updateTickCount++;
            if (updateTickCount == 1 || updateTickCount % 120 == 0)
            {
                BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-001 event=plugin-update count=" + updateTickCount);
            }
            ClientUiFeatureAssembly.DispatchPanelTick(false);
            // GPT watermark: drive DEV-16D from the guaranteed plugin Update;
            // native Harmony callback is supplementary only. DEV-V6-04: the
            // tick enters through the Bii public seam (Plugin→Bii legal edge;
            // the pre-move UI-face forwarder retired with the subsystem).
            BiiFeatureAssembly.TickPreview();
            // DEV-V2-21: the tidy dispatcher pump moved to the host Update
            // chain (headless included) — this client driver no longer owns it.
        }

        private static bool sceneLoadedSubscribedStatic;

        private static void EnsureSceneLoadedSubscribedStatic()
        {
            if (sceneLoadedSubscribedStatic) return;
            SceneManager.sceneLoaded += OnSceneLoadedStaticHandler;
            sceneLoadedSubscribedStatic = true;
        }

        private static void OnSceneLoadedStaticHandler(Scene scene, LoadSceneMode mode)
        {
            BueRuntimeCompletionChain.OnSceneLoadedCore();
        }

        private static void UnsubscribeSceneLoadedStatic()
        {
            if (!sceneLoadedSubscribedStatic) return;
            SceneManager.sceneLoaded -= OnSceneLoadedStaticHandler;
            sceneLoadedSubscribedStatic = false;
        }

        // F-D: the headless survival pump — an independent DDOL GameObject
        // (ordinary objects survive the host sweep on 3.26.3.9 where
        // HideAndDontSave ones do not) driving the shared tick chain. Its
        // callback targets the static chain only, so it keeps ticking after
        // this component has been destroyed; the scene-drive healer
        // re-attaches it if it is ever destroyed too.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureHeadlessSurvivalPump()
        {
            if (headlessPumpBehaviour != null && headlessPumpBehaviour.gameObject != null) return;
            if (headlessPump != null) headlessPump.Clear();
            headlessPump = new BueRuntimePump(BueRuntimeTickChain.Tick);
            try
            {
                headlessPumpBehaviour = BueRuntimePumpBehaviour.Attach(headlessPump);
                BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience event=headless-survival-pump-attached diagnosticId=BUE-BOOTSTRAP-003");
            }
            catch (Exception error)
            {
                headlessPump = null;
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience event=headless-survival-pump-failed errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
        }

        private void DestroyHeadlessPump()
        {
            var behaviour = headlessPumpBehaviour;
            headlessPumpBehaviour = null;
            if (headlessPump != null)
            {
                headlessPump.Clear();
                headlessPump = null;
            }
            if (behaviour != null)
            {
                var pumpObject = behaviour.gameObject;
                if (pumpObject != null) UnityEngine.Object.Destroy(pumpObject);
            }
        }

        private void RefreshManagementPanelHook()
        {
            ClientUiFeatureAssembly.RefreshManagementPanel();
        }

        private void LogAssemblyIdentity()
        {
            try
            {
                var location = typeof(BetterUnturnedExperiencePlugin).Assembly.Location;
                var hash = "unavailable";
                if (!string.IsNullOrEmpty(location) && File.Exists(location))
                {
                    using (var sha = SHA256.Create())
                    using (var stream = File.OpenRead(location))
                    {
                        hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                    }
                }
                // DEV-16G ticket D exception: assembly-identity carries the DLL
                // sha256, the evidence anchor for three-environment verification
                // (logs must embed the deployed hash). One line per session, not
                // noise — stays Info while other load one-shots drop to Debug.
                Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=assembly-identity path=" + location + " sha256=" + hash);
            }
            catch (Exception error)
            {
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=assembly-identity-failed errorType=" + error.GetType().FullName);
            }
        }

        // [R19] The game sweeps the BepInEx_Manager host mid-session (R18 hit
        // map: the vanilla MenuUI.Update postfix keeps ticking every frame
        // through that sweep). A component teardown is therefore NOT an
        // application quit: Harmony patches and the static panel state must
        // survive it, or the driver chain dies with the host object.
        private static bool applicationQuitting;

        private void OnApplicationQuitting()
        {
            applicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (!applicationQuitting)
            {
                // [R19]/F-D: a component teardown is NOT an application quit.
                // The runtime host, the static completion chain and the
                // survival pump are deliberately PRESERVED here — the U3DS
                // host sweep destroyed the plugin before its first Update
                // frame, and the chain must still be able to complete the
                // runtime and tick the modules afterwards. Only the client
                // UI driver (which dies with this component anyway) is cut.
                try
                {
                    if (pluginUpdateDriver != null) pluginUpdateDriver.Clear();
                    BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience event=host-destroyed state=preserved patches-kept=true diagnosticId=BUE-CLIENTUI-005");
                }
                catch (System.Exception error)
                {
                    Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience event=host-destroyed-preserve-failed errorType=" + error.GetType().FullName);
                }
                return;
            }
            try
            {
                DestroyRuntimePump();
                DestroyHeadlessPump();
                if (pluginUpdateDriver != null) pluginUpdateDriver.Clear();
                // DEV-V6-02E: the panel/adapters live behind the UI's public
                // face — destroy order preserved (panel → adapter isolation →
                // module stop → wired adapter → composition).
                ClientUiFeatureAssembly.DestroyNativePanel();
                // DEV-V6-04: adapter isolation enters the Bii public seam
                // (the subsystem owns its own teardown; stop order unchanged).
                BiiFeatureAssembly.IsolateAdapters();
                // DEV-V2-15: the tidy module unloads through its three-phase
                // stop (quiesce → dispatcher drain → full teardown) before
                // the plugin unloads.
                // DEV-V2-21: teardown rides the host start path's tracked
                // modules — Stop first, then the frozen event handoff
                // (UnsubscribeAll per feature AFTER Stop returns).
                BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                // DEV-V2-06: hand the network back (unhook the takeover
                // patches) before the plugin unloads.
                if (NetworkModuleFeatureRegistration.WiredAdapter != null) NetworkModuleFeatureRegistration.WiredAdapter.IsolateAndDetach();
                ClientUiFeatureAssembly.DestroyComposition();
            }
            catch (System.Exception error)
            {
                Logger.LogWarning("BUE client UI teardown isolated diagnosticId=BUE-CLIENTUI-003 errorType=" + error.GetType().FullName);
            }
            // F-D: quit teardown detaches the static chain (scene drive,
            // seams, barrier state) together with the runtime host.
            BueRuntimeCompletionChain.TeardownForQuit();
        }
    }
}
