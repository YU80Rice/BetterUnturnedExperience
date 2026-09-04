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
    /// and never call this). DEV-V2-10 F-B: Register admits BOTH facets —
    /// the network feature and the V1 compat feature (spec-V2-phase1 L74) —
    /// so the management panel's network entries exist and the takeover card
    /// is reachable. The Priority.First patches install only while the
    /// standalone-LMN probe is true: with LMN absent nothing is patched and
    /// nothing reflects (zero false positive).
    /// </summary>
    internal static class NetworkModuleFeatureRegistration
    {
        private const string FeatureId = "io.github.yu80rice.bue.network";
        private const string V1CompatFeatureId = "io.github.yu80rice.bue.network.v1compat";
        private const string StandaloneLmnGuid = "com.yu80rice.launchmultiplayernet";
        private const string ModTransportTypeName = "LaunchMultiplayerNet.Routing.ModTransport";
        private const string ModRouterTypeName = "LaunchMultiplayerNet.Routing.ModRouter";

        internal static NetworkModuleAdapter WiredAdapter { get; private set; }

        // DEV-V2-10 seam: the official definitions extracted from the private
        // Registration so tests can drive them through the real registration
        // runtime (the payload digest must validate, never go unchecked).
        internal static FeatureDefinitionArtifact CreateNetworkDefinition()
        {
            var payload = new byte[] { 66, 85, 69, 45, 78, 69, 84, 45, 86, 49 }; // "BUE-NET-V1"
            return new FeatureDefinitionArtifact(
                new FeatureId(FeatureId),
                1,
                "bue-network-v1",
                new Digest256(1UL, 0UL, 0UL, 16UL),
                ComputePayloadDigest(payload),
                payload);
        }

        // DEV-V2-10 F-B: the V1 compat switch is an official feature of its
        // own (spec-V2-phase1 L74) — it registers as a facet so the panel's
        // ToManagementEntry mapping (BUE V1 兼容层) actually receives a
        // catalog entry. Digest sentinel D3=17 is a free-form marker, same
        // style as the sibling definitions (not validated by the runtime).
        internal static FeatureDefinitionArtifact CreateV1CompatDefinition()
        {
            var payload = new byte[] { 66, 85, 69, 45, 78, 69, 84, 45, 86, 49, 67 }; // "BUE-NET-V1C"
            return new FeatureDefinitionArtifact(
                new FeatureId(V1CompatFeatureId),
                1,
                "bue-network-v1compat-v1",
                new Digest256(1UL, 0UL, 0UL, 17UL),
                ComputePayloadDigest(payload),
                payload);
        }

        // The artifact payload digest is COMPUTED from the payload bytes
        // (SHA-256, 4 little-endian uint64 parts — the runtime's Digest256
        // convention), never transcribed by hand: the real machine rejected
        // the network registration (BUE-REG-004) because its baked constants
        // did not match the payload's actual hash.
        internal static Digest256 ComputePayloadDigest(byte[] payload)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(payload);
                return new Digest256(
                    BitConverter.ToUInt64(hash, 0),
                    BitConverter.ToUInt64(hash, 8),
                    BitConverter.ToUInt64(hash, 16),
                    BitConverter.ToUInt64(hash, 24));
            }
        }

        internal static IFeatureRegistration[] CreateOfficialRegistrations()
        {
            return new IFeatureRegistration[] { new Registration(), new V1CompatRegistration() };
        }

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
            // DEV-V2-10 F-B: both facets register through the same public host
            // bridge; the panel's network entries exist only when both are in
            // the catalog. The network feature stays the advertised result.
            var networkResult = new FeatureRegistrationResult(false, new FeatureId(FeatureId), FeatureRegistrationReason.HostUnavailable, "BUE-HOST-001");
            foreach (var registration in CreateOfficialRegistrations())
            {
                var result = BueRuntimeHost.Register(registration);
                if (registration.Definition.Feature.Value == FeatureId) networkResult = result;
                if (!result.Accepted)
                {
                    // A rejected facet leaves its panel entry missing — surface
                    // it on the runtime channel without blocking the other.
                    BueRuntimeLog.Runtime("[BUE-V2NET] event=network-facet-registration result=rejected feature=" + registration.Definition.Feature.Value + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
                }
            }
            return networkResult;
        }

        private sealed class Registration : IFeatureRegistration
        {
            public FeatureDefinitionArtifact Definition { get { return CreateNetworkDefinition(); } }

            public ContractVersion MinimumBueContract { get { return new ContractVersion(1, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class V1CompatRegistration : IFeatureRegistration
        {
            public FeatureDefinitionArtifact Definition { get { return CreateV1CompatDefinition(); } }

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
