using BepInEx;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Plugin;

namespace ExternalCanary
{
    [BepInPlugin("com.bue.canary.plugin", "BUE External Canary", "1.0.0")]
    [BepInDependency("io.github.yu80rice.betterunturnedexperience", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class ExternalCanaryPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var result = BueRuntimeHost.Register(new Registration());
            Logger.LogInfo("BUE external canary featureId=" + result.Feature.Value + " accepted=" + result.Accepted + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
        }

        private sealed class Registration : IFeatureRegistration
        {
            private static readonly byte[] Payload = { 69, 88, 84, 45, 67, 65, 78, 65, 82, 89 };
            public FeatureDefinitionArtifact Definition { get { return new FeatureDefinitionArtifact(new FeatureId("com.bue.canary.hello"), 1, "external-canary", new Digest256(1, 2, 3, 4), FeatureDefinitionDigest.ComputeArtifactPayloadDigest(Payload), Payload); } }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class ModuleFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return new Module(); }
        }

        private sealed class Module : IFeatureModule
        {
            public FeatureStartResult Start(IFeatureBootstrap bootstrap) { return new FeatureStartResult(true, FrameworkErrorCode.None, "EXTERNAL-CANARY-START"); }
            public void Stop(FeatureStopReason reason) { }
        }
    }
}
