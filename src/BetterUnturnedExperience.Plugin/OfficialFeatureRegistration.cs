using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// Official Better Item Interaction registration. It deliberately uses the
    /// same public bridge as an external feature; native inventory/UI behavior
    /// is implemented by a later feature slice.
    /// </summary>
    internal static class BetterItemInteractionFeatureRegistration
    {
        private const string FeatureId = "io.github.yu80rice.bue.better-item-interaction";

        internal static FeatureRegistrationResult Register()
        {
            return BueRuntimeHost.Register(new Registration());
        }

        private sealed class Registration : IFeatureRegistration
        {
            private static readonly byte[] Payload = { 66, 85, 69, 45, 66, 73, 73, 45, 86, 49 };

            public FeatureDefinitionArtifact Definition { get; } = new FeatureDefinitionArtifact(
                new FeatureId(FeatureId),
                1,
                "bue-better-item-interaction-v1",
                new Digest256(1UL, 0UL, 0UL, 14UL),
                new Digest256(4239661619294337961UL, 6084291702436199631UL, 12736748317259485579UL, 11302160818330435267UL),
                Payload);

            public ContractVersion MinimumBueContract { get { return new ContractVersion(1, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return new OfficialClientUiSatelliteRegistration(); } }
        }

        private sealed class OfficialClientUiSatelliteRegistration : IClientUiSatelliteRegistration
        {
            public string SatelliteId { get { return "bue-clientui-embedded"; } }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(1, 0); } }
            public string RegistrationToken { get { return "bue-official-clientui-v1"; } }
        }

        private sealed class ModuleFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return new Module(); }
        }

        private sealed class Module : IFeatureModule
        {
            public FeatureStartResult Start(IFeatureBootstrap bootstrap) { return default(FeatureStartResult); }
            public void Stop(FeatureStopReason reason) { }
        }
    }
}
