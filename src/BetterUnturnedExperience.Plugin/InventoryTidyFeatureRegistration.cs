using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lit;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-15: official inventory-tidy (背包整理) registration. It enters
    /// the runtime through the same public host bridge as an external
    /// feature; the BepInEx plugin identity and the LMN hard dependency of
    /// the old standalone plugin are gone by construction — this module is
    /// source inside the single player DLL. Register constructs and arms the
    /// module (the registration runtime never invokes Start itself, the
    /// DEV-V2-06 pattern), and the definition payload digest is computed
    /// from the payload bytes, never transcribed (the DEV-V2-10 lesson).
    /// </summary>
    internal static class InventoryTidyFeatureRegistration
    {
        internal const string FeatureIdValue = LitRuntime.FeatureIdValue;

        // DEV-V3-03: the set accessor stays property-scoped (internal) — the
        // lifecycle anchor test resets it so the re-enable path exercises the
        // factory's fresh-instance arming.
        internal static InventoryTidyModule WiredModule { get; set; }

        internal static FeatureRegistrationResult Register()
        {
            // Host-face order: the module is BORN inert — it enters the host
            // registration face first and only arms (patches, dispatcher
            // queue) once the host has accepted it. A rejected registration
            // must not leave armed patches outside the host's control.
            var module = ArmNewModule();
            var result = BueRuntimeHost.Register(new Registration(module));
            if (result.Accepted)
            {
                // DEV-V2-21: BORN inert means registered only — the module
                // arms when the host start path drives IFeatureModule.Start
                // with the composed bootstrap (stable network facade + host
                // bus views). Registration no longer starts the module.
                WiredModule = module;
            }
            else
            {
                BueRuntimeLog.Runtime("[BUE-V2LIT] event=lit-registration result=rejected feature=" + FeatureIdValue
                    + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
            }
            return result;
        }

        private static InventoryTidyModule ArmNewModule()
        {
            // DEV-V3-06: born inert without a runtime — the settings come
            // exclusively through the host-injected scoped view at Start.
            var module = new InventoryTidyModule();
            module.BindProductionLog();
            return module;
        }

        /// <summary>
        /// The official definition, extracted for host tests (they drive it
        /// through a real registration runtime — the payload digest must
        /// validate, never go unchecked). moduleForFactory may be null; the
        /// factory then hands out the wired module or lazily arms a new one,
        /// so a host that ever starts modules starts the armed instance.
        /// </summary>
        internal static IFeatureRegistration CreateRegistration(InventoryTidyModule moduleForFactory = null)
        {
            return new Registration(moduleForFactory);
        }

        private static FeatureDefinitionArtifact CreateDefinition()
        {
            var payload = new byte[] { 66, 85, 69, 45, 76, 73, 84, 45, 86, 49 }; // "BUE-LIT-V1"
            return new FeatureDefinitionArtifact(
                new FeatureId(FeatureIdValue),
                1,
                "bue-inventory-tidy-v1",
                // Sentinel D-style DefinitionSetDigest (free-form identity
                // marker, same convention as the sibling definitions).
                new Digest256(1UL, 0UL, 0UL, 18UL),
                NetworkModuleFeatureRegistration.ComputePayloadDigest(payload),
                payload);
        }

        // DEV-V3-06: the settings facet (IFeatureSettingsRegistration) —
        // the module's ONE toggle schema declared through the registration
        // (功能拥有 Schema); the host composes the single runtime from it,
        // routes the panel and injects the scoped view. OnSettingsApplied =
        // the module's own refresh hook (feature-owned reaction, never a
        // second source of truth).
        private sealed class Registration : IFeatureRegistration, IFeatureSettingsRegistration
        {
            private readonly InventoryTidyModule moduleForFactory;

            internal Registration(InventoryTidyModule moduleForFactory)
            {
                this.moduleForFactory = moduleForFactory;
            }

            public FeatureDefinitionArtifact Definition { get { return CreateDefinition(); } }

            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }

            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(moduleForFactory); } }

            public IClientUiSatelliteRegistration ClientUi { get { return null; } }

            public IReadOnlyList<SettingDescriptor> SettingDescriptors { get { return InventoryTidyModule.CreateSettingsDescriptors(new FeatureId(FeatureIdValue)); } }

            public Action OnSettingsApplied
            {
                get
                {
                    var module = WiredModule ?? moduleForFactory;
                    return module == null ? null : new Action(module.RefreshSwitches);
                }
            }
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
                if (InventoryTidyFeatureRegistration.WiredModule != null) return WiredModule;
                // Factory path = the host start path is invoking modules, so
                // the host has already accepted this registration; arming now
                // is inside the host-controlled lifecycle.
                var module = ArmNewModule();
                module.EnsureStarted();
                WiredModule = module;
                return module;
            }
        }
    }
}
