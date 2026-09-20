using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20 → DEV-V6-02D: the official horde-tracker (更好的尸潮播报) registration
    /// body, migrated from the host flat-compile into the Lht project (V6-T2 真分工程).
    /// It enters the runtime through the same public host bridge as an external feature;
    /// the BepInEx plugin identity and the LMN hard dependency of the old standalone
    /// plugin are gone by construction. The host reaches this ONLY through
    /// LhtFeatureAssembly.CreateRegistration() — the ONE public assembly type; the
    /// host-facing composition wrapper (host bridge registration) stays on the host side
    /// as the assembly-root act. The module is BORN inert (registered only; it arms when
    /// the host start path drives IFeatureModule.Start with the composed bootstrap — the
    /// DEV-V2-06 pattern), and the definition payload digest is computed from the payload
    /// bytes, never transcribed (the DEV-V2-10 lesson).
    /// </summary>
    internal static class HordeTrackerRegistration
    {
        // DEV-V3-03: the wired-instance bookkeeping lives with the registration body in
        // the feature project (the factory is the only composition path, V6-T2); the host
        // sees the CONTRACT-TYPE projection through LhtFeatureAssembly.WiredModule and the
        // lifecycle anchor tests reset it through the host facade (null only).
        internal static HordeTrackerModule WiredModule { get; set; }

        /// <summary>The official definition, extracted for host tests (the payload digest must validate).</summary>
        internal static IFeatureRegistration CreateRegistration(HordeTrackerModule moduleForFactory = null)
        {
            return new Registration(moduleForFactory);
        }

        private static FeatureDefinitionArtifact CreateDefinition()
        {
            var payload = new byte[] { 66, 85, 69, 45, 76, 72, 84, 45, 86, 49 }; // "BUE-LHT-V1"
            return new FeatureDefinitionArtifact(
                new FeatureId(LhtRuntime.FeatureIdValue),
                1,
                "bue-horde-tracker-v1",
                // Sentinel D-style DefinitionSetDigest (free-form identity
                // marker, same convention as the sibling definitions).
                new Digest256(1UL, 0UL, 0UL, 20UL),
                FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload),
                payload);
        }

        private sealed class Registration : IFeatureRegistration, IFeaturePresentationRegistration
        {
            private readonly HordeTrackerModule moduleForFactory;

            internal Registration(HordeTrackerModule moduleForFactory)
            {
                this.moduleForFactory = moduleForFactory;
            }

            public FeatureDefinitionArtifact Definition { get { return CreateDefinition(); } }

            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }

            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(moduleForFactory); } }

            public IClientUiSatelliteRegistration ClientUi { get { return null; } }

            // DEV-V6-05 (V6-T5 Q1): self-reported panel copy. The tracker DOES
            // paint directly into the native HUD (no ClientUi satellite), so
            // the declaration is true — the panel still decides this entry's
            // presentation from its own row override (the module's
            // Available/HeadlessOnly fact, DEV-V2-20), which stays decisive.
            public string DisplayName { get { return LhtRuntime.DisplayName; } }
            public bool DirectPresentation { get { return true; } }
        }

        private sealed class ModuleFactory : IFeatureModuleFactory
        {
            private readonly HordeTrackerModule moduleForFactory;

            internal ModuleFactory(HordeTrackerModule moduleForFactory)
            {
                this.moduleForFactory = moduleForFactory;
            }

            public IFeatureModule Create()
            {
                if (moduleForFactory != null) return moduleForFactory;
                if (HordeTrackerRegistration.WiredModule != null) return WiredModule;
                // Factory path = the host start path is invoking modules, so
                // the host has already accepted this registration; arming now
                // is inside the host-controlled lifecycle.
                var module = ArmNewModule();
                WiredModule = module;
                return module;
            }
        }

        /// <summary>Host-face order: the module is BORN inert — it enters the
        /// host registration face first and only arms (patches, network, HUD)
        /// once the host has accepted it. A rejected registration must not
        /// leave armed patches outside the host's control.</summary>
        private static HordeTrackerModule ArmNewModule()
        {
            // DEV-V3-06: born inert without a runtime — the settings come
            // exclusively through the host-injected scoped view at Start.
            var module = new HordeTrackerModule();
            module.BindProductionLog();
            return module;
        }
    }
}
