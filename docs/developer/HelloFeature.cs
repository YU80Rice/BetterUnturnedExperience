using BepInEx;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Plugin;

namespace YourFeature
{
    [BepInPlugin("com.example.your-feature", "Your Feature", "1.0.0")]
    [BepInDependency("io.github.yu80rice.betterunturnedexperience", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class YourFeaturePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var result = BueRuntimeHost.Register(new Registration());
            Logger.LogInfo("Your Feature registered: accepted=" + result.Accepted);
        }

        private sealed class Registration : IFeatureRegistration
        {
            // Replace this payload and the feature identity with your own definition.
            private static readonly byte[] Payload = { 1, 2, 3 };

            public FeatureDefinitionArtifact Definition { get { return new FeatureDefinitionArtifact(
                new FeatureId("com.example.your-feature"), 1, "your-feature",
                new Digest256(1, 2, 3, 4),
                FeatureDefinitionDigest.ComputeArtifactPayloadDigest(Payload), Payload); } }
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
            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                return new FeatureStartResult(true, FrameworkErrorCode.None, "YOUR-FEATURE-START");
            }

            public void Stop(FeatureStopReason reason) { }
        }
    }
}
