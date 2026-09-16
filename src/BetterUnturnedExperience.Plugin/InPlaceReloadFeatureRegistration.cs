using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lir;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-22: official in-place-reload (更好的换弹体验) registration —
    /// the InventoryTidyFeatureRegistration pattern: the module is BORN inert
    /// (registered only; it arms when the host start path drives
    /// IFeatureModule.Start with the composed bootstrap), the BepInEx plugin
    /// identity and the LMN hard dependency of the old standalone plugin are
    /// gone by construction, and the definition payload digest is computed
    /// from the payload bytes, never transcribed.
    /// </summary>
    internal static class InPlaceReloadFeatureRegistration
    {
        internal const string FeatureIdValue = LirRuntime.FeatureIdValue;

        internal static InPlaceReloadModule WiredModule { get; set; }

        /// <summary>DEV-V5-07 两表面至多其一的注册期判定：U 菜单分区（表面 A）
        /// 探测接得上=维持无 facet 的诚实注册（等级不造设置项）；接不上=注册
        /// 带设置面（等级请求行，OnSettingsApplied 走模块 HandleSkillSettingsApplied）。
        /// 探测=纯反射零引擎（Binder 缓存结果），headless 与两表面正交——
        /// U3DS 不画任何面，权威账照常。</summary>
        internal static IFeatureRegistration NewRegistration(InPlaceReloadModule moduleForFactory)
        {
            return ReloadSkillDashboardBinder.ProbeSurfaceA()
                ? (IFeatureRegistration)new Registration(moduleForFactory)
                : new RegistrationWithSkillSettingsPage(moduleForFactory);
        }

        internal static FeatureRegistrationResult Register()
        {
            var module = ArmNewModule();
            var result = BueRuntimeHost.Register(NewRegistration(module));
            if (result.Accepted)
            {
                WiredModule = module;
            }
            else
            {
                BueRuntimeLog.Runtime("[BUE-V2LIR] event=lir-registration result=rejected feature=" + FeatureIdValue
                    + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
            }
            return result;
        }

        private static InPlaceReloadModule ArmNewModule()
        {
            // DEV-V3-06: born inert without a runtime (see LIT note).
            var module = new InPlaceReloadModule();
            module.BindProductionLog();
            return module;
        }

        /// <summary>The official definition, extracted for host tests (the payload digest must validate).</summary>
        internal static IFeatureRegistration CreateRegistration(InPlaceReloadModule moduleForFactory = null)
        {
            return NewRegistration(moduleForFactory);
        }

        private static FeatureDefinitionArtifact CreateDefinition()
        {
            var payload = new byte[] { 66, 85, 69, 45, 76, 73, 82, 45, 86, 49 }; // "BUE-LIR-V1"
            return new FeatureDefinitionArtifact(
                new FeatureId(FeatureIdValue),
                1,
                "bue-in-place-reload-v1",
                // Sentinel D-style DefinitionSetDigest (free-form identity
                // marker, same convention as the sibling definitions).
                new Digest256(1UL, 0UL, 0UL, 19UL),
                NetworkModuleFeatureRegistration.ComputePayloadDigest(payload),
                payload);
        }

        // DEV-V3-06: settings facet — same discipline as the sibling
        // official registrations (schema on the registration, panel routed
        // by the catalog, refresh hook feature-owned).
        private sealed class Registration : IFeatureRegistration
        {
            private readonly InPlaceReloadModule moduleForFactory;

            internal Registration(InPlaceReloadModule moduleForFactory)
            {
                this.moduleForFactory = moduleForFactory;
            }

            public FeatureDefinitionArtifact Definition { get { return CreateDefinition(); } }

            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }

            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(moduleForFactory); } }

            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        /// <summary>DEV-V5-07 表面 B（降级设置页）：仅当 U 菜单分区探测接不上
        /// 时被实例化。schema = 功能自持的一条升级请求 Choice 行（LIT facet
        /// 同律：面板只是编辑适配，等级真相恒在 LIR 主机账/回执镜像——行的
        /// OnSettingsApplied 把档位翻译成主机校验的升级请求，随即复位「维持」）。
        /// 契约仍 2.1：facet 是 DEV-V3-06 既有可选面，零扩面。</summary>
        private sealed class RegistrationWithSkillSettingsPage : IFeatureRegistration, IFeatureSettingsRegistration
        {
            private readonly InPlaceReloadModule moduleForFactory;

            internal RegistrationWithSkillSettingsPage(InPlaceReloadModule moduleForFactory)
            {
                this.moduleForFactory = moduleForFactory;
            }

            public FeatureDefinitionArtifact Definition { get { return CreateDefinition(); } }

            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }

            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(moduleForFactory); } }

            public IClientUiSatelliteRegistration ClientUi { get { return null; } }

            public IReadOnlyList<SettingDescriptor> SettingDescriptors
            {
                get { return ReloadSkillSettingsSurface.CreateDescriptors(new FeatureId(FeatureIdValue)); }
            }

            public Action OnSettingsApplied
            {
                get
                {
                    var module = moduleForFactory ?? WiredModule;
                    if (module == null) return null;
                    return () => module.HandleSkillSettingsApplied();
                }
            }
        }

        private sealed class ModuleFactory : IFeatureModuleFactory
        {
            private readonly InPlaceReloadModule moduleForFactory;

            internal ModuleFactory(InPlaceReloadModule moduleForFactory)
            {
                this.moduleForFactory = moduleForFactory;
            }

            public IFeatureModule Create()
            {
                if (moduleForFactory != null) return moduleForFactory;
                if (InPlaceReloadFeatureRegistration.WiredModule != null) return WiredModule;
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
