using System;
using System.IO;
using BetterUnturnedExperience.Contracts;
using HarmonyLib;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-06: official network module registration. It enters the runtime
    /// through the same public host bridge as an external feature (official
    /// features are peers on the registration path, no private privileges).
    /// The adapter is constructed and armed here because the registration
    /// runtime never invokes module factories' Start itself — Register is the
    /// production-only entry (tests construct NetworkModuleAdapter directly
    /// and never call this). The Priority.First patches install only while
    /// the standalone-LMN probe is true: with LMN absent nothing is patched
    /// and nothing reflects (zero false positive).
    /// </summary>
    internal static class NetworkModuleFeatureRegistration
    {
        private const string FeatureId = "io.github.yu80rice.bue.network";
        private const string StandaloneLmnGuid = "com.yu80rice.launchmultiplayernet";
        private const string ModTransportTypeName = "LaunchMultiplayerNet.Routing.ModTransport";
        private const string ModRouterTypeName = "LaunchMultiplayerNet.Routing.ModRouter";

        internal static NetworkModuleAdapter WiredAdapter { get; private set; }

        internal static FeatureRegistrationResult Register()
        {
            var settingsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BetterUnturnedExperience");
            var adapter = new NetworkModuleAdapter(
                settingsRoot,
                isStandaloneLmnLoaded: () => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(StandaloneLmnGuid),
                resolveModTransportType: () => AccessTools.TypeByName(ModTransportTypeName),
                resolveModRouterType: () => AccessTools.TypeByName(ModRouterTypeName),
                refreshPanel: () => { });
            adapter.BindProductionLog();
            adapter.ActivateCore();
            adapter.ApplyNetworkPatches();
            WiredAdapter = adapter;
            return BueRuntimeHost.Register(new Registration());
        }

        private sealed class Registration : IFeatureRegistration
        {
            private static readonly byte[] Payload = { 66, 85, 69, 45, 78, 69, 84, 45, 86, 49 }; // "BUE-NET-V1"

            public FeatureDefinitionArtifact Definition { get; } = new FeatureDefinitionArtifact(
                new FeatureId(FeatureId),
                1,
                "bue-network-v1",
                new Digest256(1UL, 0UL, 0UL, 16UL),
                new Digest256(11400714816950841075UL, 17426045655985939034UL, 13878537219987991745UL, 9961398425803426691UL),
                Payload);

            public ContractVersion MinimumBueContract { get { return new ContractVersion(1, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class ModuleFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return new Module(); }
        }

        private sealed class Module : IFeatureModule
        {
            // The registration runtime stores the factory without invoking
            // Start today; these stay as the defensive lifecycle hooks so the
            // module behaves correctly if that barrier ever starts modules.
            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                var adapter = NetworkModuleFeatureRegistration.WiredAdapter;
                if (adapter != null)
                {
                    adapter.ActivateCore();
                    adapter.ApplyNetworkPatches();
                }
                return default(FeatureStartResult);
            }

            public void Stop(FeatureStopReason reason)
            {
                var adapter = NetworkModuleFeatureRegistration.WiredAdapter;
                if (adapter != null) adapter.IsolateAndDetach();
            }
        }
    }
}
