using BepInEx;
using System;
using System.IO;
using System.Security.Cryptography;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterUnturnedExperience.Plugin
{
    [BepInPlugin("io.github.yu80rice.betterunturnedexperience", "Better Unturned Experience", "0.0.0")]
    public sealed class BetterUnturnedExperiencePlugin : BaseUnityPlugin
    {
        private const string FeatureId = "io.github.yu80rice.betterunturnedexperience";
        private const string DiagnosticId = "BUE-BOOTSTRAP-001";
        private bool runtimeReadyLogged;
        private bool sceneLoadedSubscribed;
        private int updateTickCount;
        private int runtimePumpTickCount;
        private bool runtimePumpIsolated;
        private BueClientUiCompositionRoot clientUiComposition;
        private BueNativeManagementPanel nativeManagementPanel;
        private BueDoubleInstallReport doubleInstallReport;
        private readonly BueRuntimePumpSlot runtimePumpSlot = new BueRuntimePumpSlot();
        private BueRuntimePump runtimePump;
        private BueRuntimePumpBehaviour runtimePumpBehaviour;
        private BuePluginUpdateDriver pluginUpdateDriver;
        private BueRuntimeCompletionBarrier completionBarrier;
        private InventorySurfaceLifecycleAdapter inventoryLifecycleAdapter;
        private InventoryDragPreviewAdapter inventoryDragAdapter;
        private bool isHeadlessDecision;

        // DEV-16D R5: the drag adapter depends on the inventory lifecycle
        // heartbeat.  Keep the activation decision at one host-testable seam so
        // a failed lifecycle hook can never leave a dependent Harmony hook live.
        internal static bool ShouldActivateDragPreview(bool lifecycleHooksInstalled, bool lifecycleIsolated)
        {
            return lifecycleHooksInstalled && !lifecycleIsolated;
        }

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
                isHeadlessDecision = decision == BootstrapDecision.Headless;
                BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-BOOTSTRAP-002 event=runtime-gate decision=" + decision + " batchMode=" + isBatchMode + " headless=" + isBatchMode);
                var runtime = new FeatureRegistrationRuntime();
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
                if (decision == BootstrapDecision.Client)
                {
                    pluginUpdateDriver = new BuePluginUpdateDriver(OnPluginUpdateTick);
                    clientUiComposition = new BueClientUiCompositionRoot(NetworkModuleFeatureRegistration.WiredAdapter, InventoryTidyFeatureRegistration.WiredModule, InPlaceReloadFeatureRegistration.WiredModule, HordeTrackerFeatureRegistration.WiredModule);
                    if (!clientUiComposition.Initialize(isBatchMode, isBatchMode, BueNativeManagementPanel.CanBindNativeUi()))
                    {
                        Logger.LogWarning("BUE client UI composition unavailable diagnosticId=BUE-CLIENTUI-001");
                    }
                    else
                    {
                        nativeManagementPanel = new BueNativeManagementPanel(clientUiComposition.ManagementPanel, Logger, null, clientUiComposition.RefreshManagementPanel);
                        nativeManagementPanel.Initialize();
                        AttachRuntimePump();
                        // [DEV-16C] Inventory lifecycle adapter: probes native
                        // members, fails closed with structured diagnostics and
                        // routes the projected surface into the composition.
                        // Create and register both adapters before either one is
                        // activated.  Cleanup runs in reverse registration order,
                        // so the drag delegate is detached before the lifecycle
                        // heartbeat is removed.
                        inventoryDragAdapter = new InventoryDragPreviewAdapter(Logger, clientUiComposition.OfficialComponent);

                        // DEV-V2-24 F1: level-aware routing (feature lines
                        // choose Debug/Error themselves) and the generic
                        // BUE-CLIENTUI-001 tag is appended ONLY when the line
                        // does not already carry its own diagnosticId=, so
                        // feature-owned ids are never double-tagged.
                        BetterUnturnedExperience.ClientUi.Internal.ClientUiCompositionRoot.DiagnosticSink = (line, level) =>
                        {
                            var tagged = line.Contains("diagnosticId=");
                            var text = "[BUE-CLIENTUI] " + line + (tagged ? "" : " diagnosticId=BUE-CLIENTUI-001");
                            if (level == BetterUnturnedExperience.ClientUi.Internal.ClientUiCompositionRoot.ClientUiDiagnosticLevel.Error)
                                BueRuntimeLog.Error(text);
                            else
                                BueRuntimeLog.Runtime(text);
                        };
                        inventoryLifecycleAdapter = new InventorySurfaceLifecycleAdapter(Logger,
                            surface =>
                            {
                                clientUiComposition.OpenInventory(surface);
                                // [DEV-16D] Rebind the grid's placed-item
                                // delegate on each fresh session dispatch so a
                                // rebuilt UI gets BUE's decision wrapper.
                                inventoryDragAdapter?.AttachGrid(surface);
                            },
                            () =>
                            {
                                inventoryDragAdapter?.DetachGrid();
                                clientUiComposition.CloseInventory();
                            },
                            () => clientUiComposition.OfficialComponent == null
                                || clientUiComposition.OfficialComponent.IsolatePreviewFailureResult(),
                            () =>
                            {
                                clientUiComposition.OfficialComponent?.HidePreview();
                                inventoryDragAdapter?.DetachGrid();
                                clientUiComposition.CloseInventory();
                            },
                            page =>
                            {
                                if (inventoryDragAdapter != null)
                                    inventoryDragAdapter.DetachGridAndDiscardSurface(page);
                                else
                                    clientUiComposition.OfficialComponent?.DiscardInventorySurface(page);
                            });
                        clientUiComposition.OfficialComponent.RegisterCleanupResult(() => inventoryLifecycleAdapter.IsolateAndDetach());
                        clientUiComposition.OfficialComponent.RegisterCleanupResult(() => inventoryDragAdapter.IsolateAndDetach(false));
                        inventoryLifecycleAdapter.Activate();
                        if (inventoryLifecycleAdapter.HooksInstalled)
                            BueRuntimeLog.Runtime("BUE inventory lifecycle wiring enabled diagnosticId=BUE-INVENTORY-001");
                        else
                            Logger.LogWarning("BUE inventory lifecycle wiring disabled diagnosticId=BUE-INVENTORY-003 diagnostics=" + inventoryLifecycleAdapter.GateDiagnostics);
                        // [DEV-16D] Drag preview/commit adapter driven by the same
                        // PlayerUI.Update tick; it is activated only after the
                        // lifecycle heartbeat has installed successfully.
                        if (ShouldActivateDragPreview(inventoryLifecycleAdapter.HooksInstalled, inventoryLifecycleAdapter.Isolated))
                        {
                            inventoryDragAdapter.Activate();
                        }
                        else
                        {
                            inventoryDragAdapter.IsolateAndDetach(false);
                            Logger.LogWarning("BUE drag preview wiring disabled because inventory lifecycle is unavailable diagnosticId=BUE-DRAG-003");
                        }
                        clientUiComposition.OfficialComponent.ProjectionSink = new LoggingInventoryProjectionSink(Logger);
                        // DEV-V2-23: surface the self-check finding (if any) on the
                        // management panel; the log line was emitted at scan time.
                        if (doubleInstallReport != null && doubleInstallReport.HasConflict)
                            clientUiComposition.ManagementPanel.Model.SetDoubleInstallNotice(doubleInstallReport.NoticeLine);
                        if (inventoryDragAdapter.HooksInstalled)
                            BueRuntimeLog.Runtime("BUE drag preview wiring enabled diagnosticId=BUE-DRAG-001");
                        else
                            Logger.LogWarning("BUE drag preview wiring disabled diagnosticId=BUE-DRAG-003 diagnostics=" + inventoryDragAdapter.GateDiagnostics);
                        BueRuntimeLog.Runtime("BUE client UI composition ready featureId=io.github.yu80rice.bue.better-item-interaction diagnosticId=BUE-CLIENTUI-002");
                    }
                }
                SceneManager.sceneLoaded += OnSceneLoaded;
                sceneLoadedSubscribed = true;
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
            TryCompleteRuntime();
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
                if (nativeManagementPanel != null && !nativeManagementPanel.Dispatch(BueNativeManagementPanel.TickSource.RuntimePump) && nativeManagementPanel.TickIsolated)
                {
                    runtimePumpIsolated = true;
                    DestroyRuntimePump();
                }
                TryCompleteRuntime();
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
            // DEV-V2-10 F-A: drives the deferred V1 mirror retry once LMN's
            // assembly loads (BUE bootstraps first under BepInEx name order);
            // throttled and silent inside the adapter, headless included.
            NetworkModuleFeatureRegistration.WiredAdapter?.RetryPendingMirror();
            // DEV-V2-18: the BUE runtime pump — engine peer-state diff,
            // inbound frame dispatch, handshake re-probe; every stage
            // fault-isolated inside the adapter, headless included.
            NetworkModuleFeatureRegistration.WiredAdapter?.TickNetwork();
            // DEV-V2-19: the host clock beat — one HostTick per Update, the
            // single frame-level driver seam for feature modules (never-throw
            // into this chain).
            BueHostEventRuntime.TickOnce();
            // DEV-V2-21: the tidy dispatcher pump rides the host frame chain
            // (headless included; the client-only plugin driver no longer
            // owns it). Never throws into this chain.
            try { InventoryTidyFeatureRegistration.WiredModule?.Tick(); }
            catch (Exception error)
            {
                Logger.LogWarning("[BUE-V2HOST] event=tidy-pump result=failed errorType=" + error.GetType().Name);
            }
            // Some BepInEx/Unity hosts do not dispatch a plugin Start message
            // before the first frame. Keep the same host-owned barrier as a
            // one-shot next-frame fallback; external features still cannot
            // advance the registration phase.
            TryCompleteRuntime();
        }

        private void OnPluginUpdateTick()
        {
            updateTickCount++;
            if (updateTickCount == 1 || updateTickCount % 120 == 0)
            {
                BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-001 event=plugin-update count=" + updateTickCount);
            }
            if (nativeManagementPanel != null) nativeManagementPanel.Dispatch(BueNativeManagementPanel.TickSource.Update);
            // GPT watermark: drive DEV-16D from the guaranteed plugin Update;
            // native Harmony callback is supplementary only.
            inventoryDragAdapter?.Tick();
            // DEV-V2-21: the tidy dispatcher pump moved to the host Update
            // chain (headless included) — this client driver no longer owns it.
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCompleteRuntime();
        }

        private void TryCompleteRuntime()
        {
            if (runtimeReadyLogged) return;
            if (completionBarrier == null) completionBarrier = new BueRuntimeCompletionBarrier(CompleteRuntimeOnce, LogRuntimeCompletionIsolated);
            if (!completionBarrier.TryComplete()) return;
            runtimeReadyLogged = true;
            // DEV-16G ticket D: the ONE aggregate success line. All per-subsystem
            // load Info is demoted to Debug; this is the only load-stage
            // announcement a normal play session sees. Headless announces load
            // without UI.
            BueRuntimeLog.AnnounceReady(isHeadlessDecision);
        }

        private void LogRuntimeCompletionIsolated(Exception error)
        {
            BueRuntimeLog.ErrorFriendly("Better Unturned Experience featureId=" + FeatureId + " status=RuntimeCompletionIsolated decision=Isolate errorType=" + error.GetType().FullName + " diagnosticId=" + DiagnosticId + " message=" + error.Message);
        }

        private bool CompleteRuntimeOnce()
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Phase != FeatureRegistrationPhase.RegistrationOpen) return false;
            if (!runtime.CompleteRuntime()) return false;
            // DEV-V2-21: the host barrier composes the module start path —
            // each catalog module receives its FeatureBootstrap (stable
            // feature network facade + host bus views) and starts here, not
            // at registration time.
            TryStartRegisteredModules();
            TryRefreshAfterCompletion();
            UnsubscribeSceneLoaded();
            return true;
        }

        private void TryStartRegisteredModules()
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
            }
            catch (Exception error)
            {
                Logger.LogWarning("[BUE-V2HOST] event=module-start result=failed errorType=" + error.GetType().Name + " message=" + error.Message);
            }
        }

        private void TryRefreshAfterCompletion()
        {
            try
            {
                if (clientUiComposition != null) clientUiComposition.RefreshManagementPanel();
            }
            catch (Exception error)
            {
                Logger.LogWarning("BUE client UI refresh after completion isolated errorType=" + error.GetType().FullName + " diagnosticId=BUE-CLIENTUI-004");
            }
        }

        private void UnsubscribeSceneLoaded()
        {
            if (!sceneLoadedSubscribed) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            sceneLoadedSubscribed = false;
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
                try
                {
                    UnsubscribeSceneLoaded();
                    if (pluginUpdateDriver != null) pluginUpdateDriver.Clear();
                    BueRuntimeLog.Runtime("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience event=host-destroyed state=preserved patches-kept=true diagnosticId=BUE-CLIENTUI-005");
                }
                catch (System.Exception error)
                {
                    Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience event=host-destroyed-preserve-failed errorType=" + error.GetType().FullName);
                }
                BueRuntimeHost.Clear();
                return;
            }
            try
            {
                UnsubscribeSceneLoaded();
                DestroyRuntimePump();
                if (pluginUpdateDriver != null) pluginUpdateDriver.Clear();
                if (nativeManagementPanel != null) nativeManagementPanel.Destroy();
                if (inventoryDragAdapter != null) inventoryDragAdapter.IsolateAndDetach();
                if (inventoryLifecycleAdapter != null) inventoryLifecycleAdapter.IsolateAndDetach();
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
                if (clientUiComposition != null) clientUiComposition.Destroy();
            }
            catch (System.Exception error)
            {
                Logger.LogWarning("BUE client UI teardown isolated diagnosticId=BUE-CLIENTUI-003 errorType=" + error.GetType().FullName);
            }
            BueRuntimeHost.Clear();
        }
    }
}
