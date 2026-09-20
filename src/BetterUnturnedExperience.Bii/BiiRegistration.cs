using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Bii
{
    /// <summary>DEV-V6-04: the frozen BII identity seam constants (the pre-move
    /// host facade carried them as private consts — the host facade reads these
    /// back through its migration aliases now, 02B/C/D alias precedent).</summary>
    internal static class BiiIdentity
    {
        internal const string FeatureId = "io.github.yu80rice.bue.better-item-interaction";
        /// <summary>DEV-V6-05: the self-reported panel display name (the host's
        /// hardcoded official name map retired with T5 Q1 — the feature owns
        /// its own copy now).</summary>
        internal const string DisplayName = "更好的物品交互";
    }

    /// <summary>
    /// DEV-V6-04: the official Better Item Interaction registration body,
    /// migrated from the host facade (the pre-move private classes lived in
    /// OfficialFeatureRegistration.cs with an EMPTY module — T3 Q2: the empty
    /// shell must not survive). It enters the runtime through the same public
    /// host bridge as an external feature; the host reaches this ONLY through
    /// BiiFeatureAssembly.CreateRegistration() — the ONE public assembly type.
    /// Start creates the interaction rig (component + two adapters) when the
    /// environment gate allows it; Stop tears it down (its own unpatch rides
    /// inside the adapters). Start failure rolls the rig back locally and then
    /// rides the existing isolation path (the host start catalog isolates a
    /// throwing/not-started module — zero changes there).
    /// </summary>
    internal static class BiiRegistration
    {
        /// <summary>The armed rig bookkeeping (the factory is the only
        /// composition path, V6-T2); tests reset it through the public seam.</summary>
        internal static BiiInteractionRig WiredRig { get; set; }

        internal static BiiCompositionMouths HostMouths { get; set; }
        internal static BiiInterfaceComposition InterfaceComposition { get; set; }
        internal static Func<IPlacementCandidateEvaluator> PlacementEvaluatorFactory { get; set; }

        internal static IFeatureRegistration CreateRegistration()
        {
            return new Registration();
        }

        private static FeatureDefinitionArtifact CreateDefinition()
        {
            var payload = new byte[] { 66, 85, 69, 45, 66, 73, 73, 45, 86, 49 }; // "BUE-BII-V1"
            return new FeatureDefinitionArtifact(
                new FeatureId(BiiIdentity.FeatureId),
                1,
                "bue-better-item-interaction-v1",
                // Sentinel D-style DefinitionSetDigest (free-form identity
                // marker, same convention as the sibling definitions; the
                // pre-move host constants carried over verbatim).
                new Digest256(1UL, 0UL, 0UL, 14UL),
                FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload),
                payload);
        }

        private sealed class Registration : IFeatureRegistration, IFeaturePresentationRegistration
        {
            public FeatureDefinitionArtifact Definition { get { return CreateDefinition(); } }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new ModuleFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return new OfficialClientUiSatelliteRegistration(); } }
            // DEV-V6-05 (V6-T5 Q1): the official-first self-report — display
            // name and "I paint directly in the game" (the host's hardcoded
            // official name map retired).
            public string DisplayName { get { return BiiIdentity.DisplayName; } }
            public bool DirectPresentation { get { return true; } }
        }

        private sealed class OfficialClientUiSatelliteRegistration : IClientUiSatelliteRegistration
        {
            public string SatelliteId { get { return "bue-clientui-embedded"; } }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public string RegistrationToken { get { return "bue-official-clientui-v1"; } }
        }

        private sealed class ModuleFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return new BetterItemInteractionModule(); }
        }

        /// <summary>
        /// T3 Q2: the module Start creates and arms the rig; Stop tears it down.
        /// Headless (T3 Q5): Start still runs — no UI component is created, no
        /// inventory patches are installed, nothing throws. The environment gate
        /// is fail-closed on missing wiring (a host path that never bound the
        /// mouths — e.g. a headless server or the host test process — degrades
        /// honestly to the un-armed Started state; the dangerous direction is
        /// arming, so absence must NOT arm — the deliberate opposite of 02C's
        /// clipping-absence reading, 记名 in the audit).
        /// </summary>
        private sealed class BetterItemInteractionModule : IFeatureModule
        {
            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                var mouths = BiiRegistration.HostMouths;
                if (mouths == null) return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-BII-IDLE-UNBOUND");
                // Headless gate (T3 Q5): absent decision reads as headless —
                // fail-closed for the dangerous direction (arming).
                var isHeadless = mouths.HeadlessDecision == null || mouths.HeadlessDecision();
                if (isHeadless) return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-BII-IDLE-HEADLESS");
                // The UI composition seams are bound on the client branch only;
                // without them there is no interface to create (absent = the
                // same honest degradation family).
                if (BiiRegistration.InterfaceComposition == null) return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-BII-IDLE-INTERFACE-ABSENT");

                var rig = new BiiInteractionRig(mouths, BiiRegistration.InterfaceComposition,
                    BiiRegistration.PlacementEvaluatorFactory,
                    // DEV-V6-05 (V6-T5 Q4): the platform patch pocket rides the
                    // module's bootstrap — BII is the official-first consumer
                    // (both Harmony sets register through it in the rig).
                    // Absent pocket = the pre-move self-managed shape.
                    bootstrap != null ? bootstrap.Patching : null);
                try
                {
                    rig.Arm();
                }
                catch (Exception error)
                {
                    // 开工失败=本地回滚（Spec 轴 R3 裁定）：Arm 是分步武装（Harmony 补丁先于
                    // 尾部日志与绑定就位），中途失败若只把异常交给启动器，宿主以 module=null
                    // 记账、无法再调 Stop，已装补丁就会遗留。故此处先拆净自身（Teardown 幂等、
                    // 对未建成的部件为 no-op），再如实返回 not-started 交给既有隔离路径。
                    var rollback = "done";
                    try { rig.Teardown(); }
                    catch (Exception) { rollback = "failed"; }
                    try
                    {
                        EmitRuntime("BUE better item interaction start failed diagnosticId=BUE-BII-START-FAILED"
                            + " errorType=" + error.GetType().Name + " rollback=" + rollback);
                    }
                    catch (Exception) { /* 诊断口自身故障不得遮蔽隔离交接（失败路径只报状态） */ }
                    return new FeatureStartResult(false, FrameworkErrorCode.ModuleStartFailed, "BUE-BII-START-FAILED");
                }
                WiredRig = rig;
                EmitRuntime("BUE better item interaction armed diagnosticId=BUE-BII-001");
                return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-BII-START");
            }

            public void Stop(FeatureStopReason reason)
            {
                var rig = WiredRig;
                if (rig == null) return;
                WiredRig = null;
                EmitRuntime("BUE better item interaction disarming diagnosticId=BUE-BII-002 reason=" + reason);
                rig.Teardown();
            }

            private static void EmitRuntime(string line)
            {
                var mouths = BiiRegistration.HostMouths;
                var sink = mouths != null ? mouths.LogRuntime : null;
                if (sink != null) sink(line);
            }
        }
    }
}
