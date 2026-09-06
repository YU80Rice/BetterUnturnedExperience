using BepInEx;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Plugin;

namespace BetterUnturnedExperience.NoOpFixture
{
    [BepInPlugin("io.github.yu80rice.bue.noop", "BUE No-op Feature Fixture", "0.0.0")]
    [BepInDependency("io.github.yu80rice.betterunturnedexperience", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class NoOpFeaturePlugin : BaseUnityPlugin
    {
        public static FeatureRegistrationResult RegisterWithBue() { return NoOpFeatureRegistration.Register(); }

        private void Awake()
        {
            var result = new NoOpFeatureBootstrap().Awake();
            Logger.LogInfo("BUE no-op fixture featureId=" + (result.Feature.Value ?? string.Empty) + " accepted=" + result.Accepted + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
        }
    }

    public sealed class NoOpFeatureBootstrap
    {
        public FeatureRegistrationResult Awake() { return NoOpFeatureRegistration.Register(); }
    }

    public static class NoOpFeatureRegistration
    {
        public static FeatureRegistrationResult Register()
        {
            return BueRuntimeHost.Register(new NoOpRegistration());
        }

        private sealed class NoOpRegistration : IFeatureRegistration
        {
            public FeatureDefinitionArtifact Definition { get; } = new FeatureDefinitionArtifact(new FeatureId("io.github.yu80rice.bue.noop"), 1, "bue-noop", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
            public ContractVersion MinimumBueContract { get { return new ContractVersion(1, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new NoOpFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class NoOpFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return new NoOpModule(); }
        }

        private sealed class NoOpModule : IFeatureModule
        {
            public FeatureStartResult Start(IFeatureBootstrap bootstrap) { return default(FeatureStartResult); }
            public void Stop(FeatureStopReason reason) { }
        }
    }
}
