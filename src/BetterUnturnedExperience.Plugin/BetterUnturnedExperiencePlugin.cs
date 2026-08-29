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
        private readonly BueRuntimePumpSlot runtimePumpSlot = new BueRuntimePumpSlot();
        private BueRuntimePump runtimePump;
        private BueRuntimePumpBehaviour runtimePumpBehaviour;
        private BuePluginUpdateDriver pluginUpdateDriver;
        private BueRuntimeCompletionBarrier completionBarrier;

        private void Awake()
        {
            try
            {
                DontDestroyOnLoad(gameObject);
                enabled = true;
                LogAssemblyIdentity();
                var isBatchMode = Application.isBatchMode;
                var decision = BootstrapGuard.Decide(isBatchMode, isBatchMode, !isBatchMode);
                Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-BOOTSTRAP-002 event=runtime-gate decision=" + decision + " batchMode=" + isBatchMode + " headless=" + isBatchMode);
                var runtime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(runtime);
                runtime.OpenRegistration();
                var officialRegistration = BetterItemInteractionFeatureRegistration.Register();
                if (decision == BootstrapDecision.Client)
                {
                    pluginUpdateDriver = new BuePluginUpdateDriver(OnPluginUpdateTick);
                    clientUiComposition = new BueClientUiCompositionRoot();
                    if (!clientUiComposition.Initialize(isBatchMode, isBatchMode, BueNativeManagementPanel.CanBindNativeUi()))
                    {
                        Logger.LogWarning("BUE client UI composition unavailable diagnosticId=BUE-CLIENTUI-001");
                    }
                    else
                    {
                        nativeManagementPanel = new BueNativeManagementPanel(clientUiComposition.ManagementPanel, Logger, null, clientUiComposition.RefreshManagementPanel);
                        nativeManagementPanel.Initialize();
                        AttachRuntimePump();
                        // [DEBUG-drv] coroutine + context pumps ride the client branch so a
                        // dedicated headless server never grows the log unbounded.
                        StartCoroutine(DrvCoroutinePump());
                        DrvStartContextPump();
                        Logger.LogInfo("BUE client UI composition ready featureId=io.github.yu80rice.bue.better-item-interaction diagnosticId=BUE-CLIENTUI-002");
                    }
                }
                SceneManager.sceneLoaded += OnSceneLoaded;
                sceneLoadedSubscribed = true;
                // [DEBUG-drv] host-object state probe; removed after diagnosis.
                Logger.LogInfo("[DEBUG-drv] event=awake-object-state activeInHierarchy=" + gameObject.activeInHierarchy + " activeSelf=" + gameObject.activeSelf + " enabled=" + enabled + " scene=" + gameObject.scene.name);
                Logger.LogInfo("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapReady decision=" + decision + " diagnosticId=" + DiagnosticId);
                Logger.LogInfo("Better Item Interaction featureId=" + officialRegistration.Feature.Value + " accepted=" + officialRegistration.Accepted + " reason=" + officialRegistration.Reason + " diagnosticId=" + officialRegistration.DiagnosticId);
            }
            catch (System.Exception error)
            {
                Logger.LogError("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapFailed diagnosticId=" + DiagnosticId + " errorType=" + error.GetType().FullName);
            }
        }

        public void Start()
        {
            Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=start-entered");
            TryCompleteRuntime();
        }

        // [DEBUG-drv] lifecycle probes distinguish a dead host object from a
        // silent message pump; removed after diagnosis.
        private void OnEnable()
        {
            Logger.LogInfo("[DEBUG-drv] event=on-enabled activeInHierarchy=" + (gameObject != null ? gameObject.activeInHierarchy.ToString() : "n/a"));
        }

        private void OnDisable()
        {
            // [DEBUG-drv] attribution probe; removed after diagnosis. Info
            // level keeps UMM warning summaries clean.
            var components = new System.Text.StringBuilder();
            foreach (var component in gameObject.GetComponents<Component>())
                components.Append(component == null ? "null;" : component.GetType().FullName + (component == this ? "(self)" : "") + ";");
            Logger.LogInfo("[DEBUG-drv] event=on-disabled activeSelf=" + gameObject.activeSelf + " activeInHierarchy=" + gameObject.activeInHierarchy + " enabled=" + enabled + " name=" + gameObject.name + " parent=" + (transform.parent != null ? transform.parent.name : "null") + " components=" + components);
            Logger.LogInfo("[DEBUG-drv] event=on-disabled-stack trace=" + Environment.StackTrace.Replace("\n", " | ").Replace("\r", string.Empty));
            DrvAttemptSelfRevive(logFirstOnly: false);
        }

        // [DEBUG-drv] everything below is diagnostic; removed after diagnosis.
        private int drvContextTicks;
        private bool drvGameDetourChecked;
        private bool drvReviveLogged;

        private void DrvAttemptSelfRevive(bool logFirstOnly)
        {
            try
            {
                var revived = false;
                if (!enabled) { enabled = true; revived = true; }
                if (!gameObject.activeSelf) { gameObject.SetActive(true); revived = true; }
                if (revived && (!logFirstOnly || !drvReviveLogged))
                {
                    drvReviveLogged = true;
                    Logger.LogInfo("[DEBUG-drv] event=self-revive componentReEnabled=" + enabled + " objectActiveSelf=" + gameObject.activeSelf);
                }
            }
            catch (Exception error)
            {
                Logger.LogWarning("[DEBUG-drv] event=self-revive-failed errorType=" + error.GetType().FullName);
            }
        }

        private void DrvStartContextPump()
        {
            var context = System.Threading.SynchronizationContext.Current;
            if (context == null)
            {
                Logger.LogWarning("[DEBUG-drv] event=context-pump-unavailable");
                return;
            }
            Logger.LogInfo("[DEBUG-drv] event=context-pump-started type=" + context.GetType().FullName);
            DrvPostContextTick(context);
        }

        private void DrvPostContextTick(System.Threading.SynchronizationContext context)
        {
            context.Post(_ => DrvContextTick(context), null);
        }

        private void DrvContextTick(System.Threading.SynchronizationContext context)
        {
            drvContextTicks++;
            if (drvContextTicks == 1 || drvContextTicks % 120 == 0)
                Logger.LogInfo("[DEBUG-drv] event=context-tick count=" + drvContextTicks + " enabled=" + enabled + " activeInHierarchy=" + gameObject.activeInHierarchy);
            if (drvContextTicks >= 3600)
            {
                // [DEBUG-drv] exit guard: the probe pump must not outlive the
                // diagnostic session even if the disable source never stops.
                Logger.LogInfo("[DEBUG-drv] event=context-pump-stopped reason=tick-limit");
                return;
            }
            DrvAttemptSelfRevive(logFirstOnly: true);
            DrvCheckGameDetour();
            DrvPostContextTick(context);
        }

        private void DrvCheckGameDetour()
        {
            if (drvGameDetourChecked) return;
            try
            {
                var menuUiType = typeof(SDG.Unturned.MenuUI);
                var instanceField = menuUiType.GetField("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var menuUi = instanceField == null ? null : instanceField.GetValue(null);
                if (menuUi == null) return;
                drvGameDetourChecked = true;
                var before = BueNativeManagementPanel.HostUiTickHits;
                HarmonyLib.AccessTools.Method(typeof(SDG.Unturned.MenuUI), "Update").Invoke(menuUi, null);
                Logger.LogInfo("[DEBUG-drv] event=game-detour-check manualInvoked=true hostUiHitsBefore=" + before + " hostUiHitsAfter=" + BueNativeManagementPanel.HostUiTickHits);
            }
            catch (Exception error)
            {
                drvGameDetourChecked = true;
                Logger.LogWarning("[DEBUG-drv] event=game-detour-check-failed errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
        }

        private System.Collections.IEnumerator DrvCoroutinePump()
        {
            var ticks = 0;
            while (true)
            {
                ticks++;
                if (ticks == 1 || ticks % 120 == 0) Logger.LogInfo("[DEBUG-drv] event=coroutine-tick count=" + ticks);
                yield return null;
            }
        }

        private void AttachRuntimePump()
        {
            if (runtimePumpBehaviour != null && runtimePumpBehaviour.gameObject != null) return;
            DestroyRuntimePump();
            try
            {
                runtimePump = runtimePumpSlot.GetOrCreate(OnRuntimePumpTick);
                runtimePumpBehaviour = BueRuntimePumpBehaviour.Attach(runtimePump);
                Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-created object=BUE.RuntimePump");
            }
            catch (Exception error)
            {
                DestroyRuntimePump();
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-create-failed errorType=" + error.GetType().FullName);
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
                Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-tick count=1");
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
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-failed errorType=" + error.GetType().FullName);
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-003 event=runtime-pump-isolated fallback=NativeUi");
            }
        }

        private void Update()
        {
            if (pluginUpdateDriver != null) pluginUpdateDriver.Update();
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
                Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-001 event=plugin-update count=" + updateTickCount);
            }
            if (nativeManagementPanel != null) nativeManagementPanel.Dispatch(BueNativeManagementPanel.TickSource.Update);
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
            Logger.LogInfo("Better Unturned Experience featureId=" + FeatureId + " status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003");
        }

        private void LogRuntimeCompletionIsolated(Exception error)
        {
            Logger.LogError("Better Unturned Experience featureId=" + FeatureId + " status=RuntimeCompletionIsolated decision=Isolate errorType=" + error.GetType().FullName + " diagnosticId=" + DiagnosticId);
        }

        private bool CompleteRuntimeOnce()
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Phase != FeatureRegistrationPhase.RegistrationOpen) return false;
            if (!runtime.CompleteRuntime()) return false;
            TryRefreshAfterCompletion();
            UnsubscribeSceneLoaded();
            return true;
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
                Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=assembly-identity path=" + location + " sha256=" + hash);
            }
            catch (Exception error)
            {
                Logger.LogWarning("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=assembly-identity-failed errorType=" + error.GetType().FullName);
            }
        }

        private void OnDestroy()
        {
            try
            {
                UnsubscribeSceneLoaded();
                DestroyRuntimePump();
                if (pluginUpdateDriver != null) pluginUpdateDriver.Clear();
                if (nativeManagementPanel != null) nativeManagementPanel.Destroy();
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
