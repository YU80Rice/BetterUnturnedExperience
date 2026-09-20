using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V2-15 → DEV-V6-02B: the official inventory-tidy registration body,
    /// migrated from the host flat-compile into the Lit project (V6-T2 真分工程).
    /// It enters the runtime through the same public host bridge as an external
    /// feature; the BepInEx plugin identity and the LMN hard dependency of the
    /// old standalone plugin are gone by construction. The host reaches this
    /// ONLY through LitFeatureAssembly.CreateRegistration() — the ONE public
    /// assembly type; the host-facing composition wrapper (host bridge
    /// registration + wired-instance bookkeeping surface) stays on the host
    /// side as the assembly-root act. Register constructs and arms the module
    /// (the registration runtime never invokes Start itself, the DEV-V2-06
    /// pattern), and the definition payload digest is computed from the
    /// payload bytes, never transcribed (the DEV-V2-10 lesson).
    /// </summary>
    internal static class LitTidyRegistration
    {
        // DEV-V3-03: the set accessor stays property-scoped (internal) — the
        // lifecycle anchor test resets it (through the host facade) so the
        // re-enable path exercises the factory's fresh-instance arming.
        internal static InventoryTidyModule WiredModule { get; set; }

        internal static IFeatureRegistration CreateRegistration(InventoryTidyModule moduleForFactory = null)
        {
            return new Registration(moduleForFactory);
        }

        private static FeatureDefinitionArtifact CreateDefinition()
        {
            var payload = new byte[] { 66, 85, 69, 45, 76, 73, 84, 45, 86, 49 }; // "BUE-LIT-V1"
            return new FeatureDefinitionArtifact(
                new FeatureId(LitRuntime.FeatureIdValue),
                1,
                "bue-inventory-tidy-v1",
                // Sentinel D-style DefinitionSetDigest (free-form identity
                // marker, same convention as the sibling definitions).
                new Digest256(1UL, 0UL, 0UL, 18UL),
                FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload),
                payload);
        }

        /// <summary>
        /// DEV-V3-06 → DEV-V4-04 → DEV-V4-06: the settings facet is BACK as
        /// global choices (ClientPreference scope). DEV-V5-02 (V5-T3): the
        /// facet is now the module's ONE global choice (inventorytidy.direction
        /// stable-finish preference) — the 同类/空间/大件 mode row retires with
        /// the unified tagged row-band layout (the old stored value keeps
        /// loading harmlessly and never decides the algorithm). The legacy
        /// enabled master switch STAYS retired (spec「成功后旧字段从 schema 与
        /// 面板退役」) — the lifecycle is the only switch and the panel draws
        /// the Choice rows through the DEV-V4-02 row projection (T1 检验点②:
        /// LIT is the official-first real consumer of the choice controls).
        /// </summary>
        private sealed class Registration : IFeatureRegistration, IFeatureSettingsRegistration, IFeaturePresentationRegistration
        {
            private readonly InventoryTidyModule moduleForFactory;
            private readonly FeatureId feature;

            internal Registration(InventoryTidyModule moduleForFactory)
            {
                this.moduleForFactory = moduleForFactory;
                feature = new FeatureId(LitRuntime.FeatureIdValue);
            }

            public FeatureDefinitionArtifact Definition { get { return CreateDefinition(); } }

            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }

            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(moduleForFactory); } }

            public IClientUiSatelliteRegistration ClientUi { get { return null; } }

            // DEV-V4-06: the two global choices. OnSettingsApplied stays null
            // honestly — mode/direction have no working-state to refresh (the
            // tidy click reads the saved snapshot at click time, Q59); the
            // retired enabled toggle was the refresh hook's only caller.
            public IReadOnlyList<SettingDescriptor> SettingDescriptors
            {
                get { return InventoryTidyModule.CreateSettingsDescriptors(feature); }
            }

            public Action OnSettingsApplied { get { return null; } }

            // DEV-V6-05 (V6-T5 Q1): self-reported panel copy — the tidy feature
            // patches the native inventory UI directly and ships no ClientUi
            // satellite, so it declares its own display name and direct
            // presentation (the host's hardcoded official name map retired).
            public string DisplayName { get { return LitRuntime.DisplayName; } }
            public bool DirectPresentation { get { return true; } }
        }

        private sealed class ModuleFactory : IFeatureModuleFactory
        {
            private readonly InventoryTidyModule moduleForFactory;

            internal ModuleFactory(InventoryTidyModule moduleForFactory)
            {
                this.moduleForFactory = moduleForFactory;
            }

            public IFeatureModule Create()
            {
                if (moduleForFactory != null) return moduleForFactory;
                if (LitTidyRegistration.WiredModule != null) return LitTidyRegistration.WiredModule;
                // DEV-V6-11: born inert, and it STAYS inert until the host start
                // path calls IFeatureModule.Start — arming (patches, dispatcher
                // queue) happens inside the lifecycle, where the bootstrap's
                // patch pocket is in hand (the pre-ticket factory path armed
                // patches before Start, i.e. outside the platform account).
                var module = ArmNewModule();
                LitTidyRegistration.WiredModule = module;
                return module;
            }
        }

        /// <summary>Host-face order: the module is BORN inert — it enters the
        /// host registration face first and only arms (patches, dispatcher
        /// queue) once the host has started it through the lifecycle
        /// (DEV-V6-11: Start owns arming; a rejected registration never leaves
        /// armed patches outside the host's control).</summary>
        private static InventoryTidyModule ArmNewModule()
        {
            // DEV-V3-06: born inert without a runtime — the settings come
            // exclusively through the host-injected scoped view at Start.
            var module = new InventoryTidyModule();
            module.BindProductionLog();
            return module;
        }
    }
}
