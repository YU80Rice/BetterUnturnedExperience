using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22 → DEV-V6-02C: the official in-place-reload (更好的换弹体验)
    /// registration body, migrated from the host flat-compile into the Lir
    /// project (V6-T2 真分工程). It enters the runtime through the same public
    /// host bridge as an external feature; the BepInEx plugin identity and the
    /// LMN hard dependency of the old standalone plugin are gone by
    /// construction. The host reaches this ONLY through
    /// LirFeatureAssembly.CreateRegistration() — the ONE public assembly type;
    /// the host-facing composition wrapper (host bridge registration +
    /// wired-instance projection) stays on the host side as the assembly-root
    /// act. The module is BORN inert (registered only; it arms when the host
    /// start path drives IFeatureModule.Start with the composed bootstrap), the
    /// factory lazy-arming path is verbatim, and the definition payload digest
    /// is computed from the payload bytes, never transcribed.
    /// </summary>
    internal static class LirReloadRegistration
    {
        // DEV-V3-03: the set accessor stays property-scoped (internal) — the
        // lifecycle anchor test resets it (through the host facade) so the
        // re-enable path exercises the factory's fresh-instance arming.
        internal static InPlaceReloadModule WiredModule { get; set; }

        internal static IFeatureRegistration CreateRegistration(InPlaceReloadModule moduleForFactory = null)
        {
            return NewRegistration(moduleForFactory);
        }

        /// <summary>DEV-V5-07 两表面至多其一的注册期判定：U 菜单分区（表面 A）
        /// 探测接得上=维持无 facet 的诚实注册（等级不造设置项）；接不上=注册
        /// 带设置面（等级请求行，OnSettingsApplied 走模块 HandleSkillSettingsApplied）。
        /// 探测=纯反射零引擎（Binder 缓存结果），headless 与两表面正交——
        /// U3DS 不画任何面，权威账照常。</summary>
        private static IFeatureRegistration NewRegistration(InPlaceReloadModule moduleForFactory)
        {
            return ReloadSkillDashboardBinder.ProbeSurfaceA()
                ? (IFeatureRegistration)new Registration(moduleForFactory)
                : new RegistrationWithSkillSettingsPage(moduleForFactory);
        }

        /// <summary>Host-face order: the module is BORN inert — it enters the
        /// host registration face first and only arms (patches, network
        /// service) once the host has accepted it. A rejected registration
        /// must not leave armed patches outside the host's control.</summary>
        private static InPlaceReloadModule ArmNewModule()
        {
            // DEV-V3-06: born inert without a runtime — the settings come
            // exclusively through the host-injected scoped view at Start.
            var module = new InPlaceReloadModule();
            module.BindProductionLog();
            return module;
        }

        private static FeatureDefinitionArtifact CreateDefinition()
        {
            var payload = new byte[] { 66, 85, 69, 45, 76, 73, 82, 45, 86, 49 }; // "BUE-LIR-V1"
            return new FeatureDefinitionArtifact(
                new FeatureId(LirRuntime.FeatureIdValue),
                1,
                "bue-in-place-reload-v1",
                // Sentinel D-style DefinitionSetDigest (free-form identity
                // marker, same convention as the sibling definitions).
                new Digest256(1UL, 0UL, 0UL, 19UL),
                FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload),
                payload);
        }

        // DEV-V3-06: settings facet — same discipline as the sibling
        // official registrations (schema on the registration, panel routed
        // by the catalog, refresh hook feature-owned).
        private sealed class Registration : IFeatureRegistration, IFeaturePresentationRegistration
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

            // DEV-V6-05 (V6-T5 Q1): self-reported panel copy — the reload
            // feature patches the native flow directly and ships no ClientUi
            // satellite (the host's hardcoded official name map retired).
            // BOTH surfaces of this registration self-report: the panel must
            // keep the feature's name whichever settings surface is active.
            public string DisplayName { get { return LirRuntime.DisplayName; } }
            public bool DirectPresentation { get { return true; } }
        }

        /// <summary>DEV-V5-07 表面 B（降级设置页）：仅当 U 菜单分区探测接不上
        /// 时被实例化。schema = 功能自持的一条升级请求 Choice 行（LIT facet
        /// 同律：面板只是编辑适配，等级真相恒在 LIR 主机账/回执镜像——行的
        /// OnSettingsApplied 把档位翻译成主机校验的升级请求，随即复位「维持」）。
        /// 契约仍 2.1：facet 是 DEV-V3-06 既有可选面，零扩面。</summary>
        private sealed class RegistrationWithSkillSettingsPage : IFeatureRegistration, IFeatureSettingsRegistration, IFeaturePresentationRegistration
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
                get { return ReloadSkillSettingsSurface.CreateDescriptors(new FeatureId(LirRuntime.FeatureIdValue)); }
            }

            // DEV-V6-05 (V6-T5 Q1): the second surface reports the SAME panel
            // copy (one feature, one name — whichever surface registered).
            public string DisplayName { get { return LirRuntime.DisplayName; } }
            public bool DirectPresentation { get { return true; } }

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
                if (LirReloadRegistration.WiredModule != null) return WiredModule;
                // DEV-V6-12（V6-T5 Q2 追加裁决）：工厂路径交付**惰性**模块——武装、登记与
                // 开工状态都只由 IFeatureModule.Start 在生命周期内产生（工厂不再是提前
                // 决定武装/开始的地方；生态功能同一形状）。
                var module = ArmNewModule();
                LirReloadRegistration.WiredModule = module;
                return module;
            }
        }
    }
}
