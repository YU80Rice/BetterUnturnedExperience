using BepInEx;
using BetterUnturnedExperience.Core.Registration;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin
{
    [BepInPlugin("io.github.yu80rice.betterunturnedexperience", "Better Unturned Experience", "0.0.0")]
    public sealed class BetterUnturnedExperiencePlugin : BaseUnityPlugin
    {
        private const string FeatureId = "io.github.yu80rice.betterunturnedexperience";
        private const string DiagnosticId = "BUE-BOOTSTRAP-001";

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
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || !runtime.CompleteRuntime())
            {
                Logger.LogError("Better Unturned Experience featureId=" + FeatureId + " status=BootstrapFailed diagnosticId=BUE-BOOTSTRAP-002 errorType=RuntimeBarrierRejected");
                return;
            }

            Logger.LogInfo("Better Unturned Experience featureId=" + FeatureId + " status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003");
        }
    }
}
