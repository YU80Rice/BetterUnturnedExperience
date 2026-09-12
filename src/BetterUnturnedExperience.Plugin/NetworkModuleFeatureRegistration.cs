using System;
using System.Collections.Generic;
using System.IO;
using BetterUnturnedExperience.Contracts;

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
    /// is reachable. DEV-V2-18 rewrote the patch-install rule: the
    /// Priority.First patches install whenever the network module is enabled
    /// (BUE frame consumption needs BUE's own patches even with the
    /// standalone LMN absent); with the probe false the LMN seam keeps zero
    /// patch/reflection/mirror actions.
    /// </summary>
    internal static class NetworkModuleFeatureRegistration
    {
        private const string FeatureId = "io.github.yu80rice.bue.network";
        private const string V1CompatFeatureId = "io.github.yu80rice.bue.network.v1compat";
        private const string StandaloneLmnGuid = "com.yu80rice.launchmultiplayernet";

        // DEV-V2-11: the standalone LMN type names are an EXTERNAL contract —
        // the authority is the LMN repository source (LaunchMultiplayerNet/
        // Routing/ModTransport.cs + ModRouter.cs, both `namespace
        // LaunchMultiplayerNet`, no `.Routing` segment in the full names).
        // Both were once transcribed with an extra ".Routing" level and never
        // resolved on the real machine (DEV-V2-10 retest F-C); the red-test
        // anchor pins them to the LMN source names verbatim.
        internal const string ModTransportTypeName = "LaunchMultiplayerNet.ModTransport";
        internal const string ModRouterTypeName = "LaunchMultiplayerNet.ModRouter";

        /// <summary>
        /// DEV-V2-11: resolves a type by full name across the already-loaded
        /// assemblies WITHOUT any logging. AccessTools.TypeByName logs a
        /// HarmonyX Warning on every miss, and the deferred-mirror retry
        /// re-resolves every few seconds while LMN has not loaded yet — that
        /// SpamFlooded real sessions with 63-169 warnings. A silent miss is
        /// the whole point of the deferred retry.
        /// </summary>
        internal static Type TryFindLoadedType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type;
                try { type = assembly.GetType(fullName, false); }
                catch (Exception) { continue; }
                if (type != null) return type;
            }
            return null;
        }

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
                resolveModTransportType: () => TryFindLoadedType(ModTransportTypeName),
                resolveModRouterType: () => TryFindLoadedType(ModRouterTypeName),
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

        // DEV-V3-06 → DEV-V4-04: both network facets declared their switch
        // schema through the registration; the legacy enabled master switches
        // RETIRE with the V4 migration (spec「成功后旧字段从 schema 与面板
        // 退役」) — no facet, no panel row; the durable disable intent lives
        // in the lifecycle intent store and the adapter consults it.
        private sealed class Registration : IFeatureRegistration
        {
            public FeatureDefinitionArtifact Definition { get { return CreateNetworkDefinition(); } }

            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class V1CompatRegistration : IFeatureRegistration
        {
            public FeatureDefinitionArtifact Definition { get { return CreateV1CompatDefinition(); } }

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
