using BepInEx;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin
{
    [BepInPlugin("io.github.yu80rice.betterunturnedexperience", "Better Unturned Experience", "0.0.0")]
    public sealed class BetterUnturnedExperiencePlugin : BaseUnityPlugin
    {
        private const string FeatureId = "io.github.yu80rice.betterunturnedexperience";
        private const string DiagnosticId = "BUE-BOOTSTRAP-001";
        private bool runtimeReadyLogged;

        private void Awake()
        {
            try
            {
                var isBatchMode = Application.isBatchMode;
                var decision = BootstrapGuard.Decide(isBatchMode, isBatchMode, !isBatchMode);
                var runtime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(runtime);
                runtime.OpenRegistration();
                Logger.LogInfo("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapReady decision=" + decision + " diagnosticId=" + DiagnosticId);
            }
            catch (System.Exception error)
            {
                Logger.LogError("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapFailed diagnosticId=" + DiagnosticId + " errorType=" + error.GetType().FullName);
            }
        }

        private void Start()
        {
            TryCompleteRuntime();
        }

        private void Update()
        {
            // Some BepInEx/Unity hosts do not dispatch a plugin Start message
            // before the first frame. Keep the same host-owned barrier as a
            // one-shot next-frame fallback; external features still cannot
            // advance the registration phase.
            TryCompleteRuntime();
        }

        private void TryCompleteRuntime()
        {
            if (runtimeReadyLogged) return;
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Phase != FeatureRegistrationPhase.RegistrationOpen) return;
            if (!runtime.CompleteRuntime()) return;
            runtimeReadyLogged = true;
            Logger.LogInfo("Better Unturned Experience featureId=" + FeatureId + " status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003");
        }
    }
}
