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
        private BueClientUiCompositionRoot clientUiComposition;
        private BueNativeManagementPanel nativeManagementPanel;

        private void Awake()
        {
            try
            {
                LogAssemblyIdentity();
                var isBatchMode = Application.isBatchMode;
                var decision = BootstrapGuard.Decide(isBatchMode, isBatchMode, !isBatchMode);
                var runtime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(runtime);
                runtime.OpenRegistration();
                var officialRegistration = BetterItemInteractionFeatureRegistration.Register();
                if (decision == BootstrapDecision.Client)
                {
                    clientUiComposition = new BueClientUiCompositionRoot();
                    if (!clientUiComposition.Initialize(isBatchMode, isBatchMode, true))
                    {
                        Logger.LogWarning("BUE client UI composition unavailable diagnosticId=BUE-CLIENTUI-001");
                    }
                    else
                    {
                        nativeManagementPanel = new BueNativeManagementPanel(clientUiComposition.ManagementPanel, Logger);
                        nativeManagementPanel.Initialize();
                        Logger.LogInfo("BUE client UI composition ready featureId=io.github.yu80rice.bue.better-item-interaction diagnosticId=BUE-CLIENTUI-002");
                    }
                }
                SceneManager.sceneLoaded += OnSceneLoaded;
                sceneLoadedSubscribed = true;
                Logger.LogInfo("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapReady decision=" + decision + " diagnosticId=" + DiagnosticId);
                Logger.LogInfo("Better Item Interaction featureId=" + officialRegistration.Feature.Value + " accepted=" + officialRegistration.Accepted + " reason=" + officialRegistration.Reason + " diagnosticId=" + officialRegistration.DiagnosticId);
            }
            catch (System.Exception error)
            {
                Logger.LogError("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapFailed diagnosticId=" + DiagnosticId + " errorType=" + error.GetType().FullName);
            }
        }

        private void Start()
        {
            Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=start-entered");
            TryCompleteRuntime();
        }

        private void Update()
        {
            updateTickCount++;
            if (updateTickCount == 1 || updateTickCount % 120 == 0)
            {
                Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-001 event=plugin-update count=" + updateTickCount);
            }
            if (nativeManagementPanel != null) nativeManagementPanel.Tick(BueNativeManagementPanel.TickSource.Update);
            // Some BepInEx/Unity hosts do not dispatch a plugin Start message
            // before the first frame. Keep the same host-owned barrier as a
            // one-shot next-frame fallback; external features still cannot
            // advance the registration phase.
            TryCompleteRuntime();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCompleteRuntime();
        }

        private void TryCompleteRuntime()
        {
            if (runtimeReadyLogged) return;
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Phase != FeatureRegistrationPhase.RegistrationOpen) return;
            if (!runtime.CompleteRuntime()) return;
            runtimeReadyLogged = true;
            UnsubscribeSceneLoaded();
            Logger.LogInfo("Better Unturned Experience featureId=" + FeatureId + " status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003");
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
