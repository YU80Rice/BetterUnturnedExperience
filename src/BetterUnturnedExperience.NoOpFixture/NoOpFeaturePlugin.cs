using System;
using System.Collections.Generic;
using BepInEx;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Plugin;

namespace BetterUnturnedExperience.NoOpFixture
{
    [BepInPlugin("io.github.yu80rice.bue.noop", "BUE No-op Feature Fixture", "0.0.0")]
    [BepInDependency("io.github.yu80rice.betterunturnedexperience", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class NoOpFeaturePlugin : BaseUnityPlugin
    {
        public static FeatureRegistrationResult RegisterWithBue() { return NoOpFeatureRegistration.Register(); }

        private void Awake()
        {
            var result = new NoOpFeatureBootstrap().Awake();
            Logger.LogInfo("BUE no-op fixture featureId=" + (result.Feature.Value ?? string.Empty) + " accepted=" + result.Accepted + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
        }
    }

    public sealed class NoOpFeatureBootstrap
    {
        public FeatureRegistrationResult Awake() { return NoOpFeatureRegistration.Register(); }
    }

    public static class NoOpFeatureRegistration
    {
        /// <summary>
        /// DEV-V3-03: the ecosystem contract probe's observability seam — the
        /// state of the last probe module the fixture factory created (host
        /// tests read it to verify the ECOSYSTEM-side lifecycle consumption;
        /// the official side is anchored by the real LIT module). Sample
        /// fixture surface, not SDK contract.
        /// </summary>
        public sealed class ProbeState
        {
            public bool Started;
            public bool Tracked;
            public bool ResourceDisposed;
            public FeatureState QueriedStateAtStart;
            // DEV-V3-04 ecosystem-side contrast: the probe observes the
            // wired MainThread dispatcher seam and gets an explicit accepted
            // post through it (the full post-wiring chain probe lands with
            // DEV-V3-08).
            public bool MainThreadAvailable;
            public bool MainThreadPosted;
            // DEV-V3-05 HostTick branch (V3-T6 裁决③): the probe subscribes
            // to the host clock through the Events seam and records what the
            // ticks carried — the host-test side asserts the receipt, the
            // sequence advance, the frozen phase and the stop-boundary
            // auto-unsubscription. Sample fixture surface, not SDK contract.
            public bool HostTickSubscribed;
            public int HostTicksReceived;
            public ulong FirstHostTickNumber;
            public ulong LastHostTickNumber;
            public TickPhase LastHostTickPhase;
            public float LastHostTickDeltaSeconds;
            // DEV-V3-06 Settings 支线（T7 裁决③生态侧最小对照，全链 probe→08）：
            // 探针按生态作者应有姿势消费注入 Settings view——读快照、提交合法
            // 变更观察 revision 推进、提交非法变更观察显式拒。写落到宿主登记
            // 的持久根（与官方同一规则同一 runtime，样本功能无特权）。停止后
            // 视图写入失效=宿主侧锚（不在样本里自证）。
            public bool SettingsAvailable;
            public bool SettingsSchemaVisible;
            public bool SettingsCommitAccepted;
            public bool SettingsRevisionAdvanced;
            public bool SettingsInvalidRejected;
        }

        public static ProbeState LastProbe { get; private set; }

        // The fixture's own registration (whitelisted official sample
        // identity) for host tests that drive the probe through a fresh
        // registration runtime.
        public static IFeatureRegistration ProbeRegistration { get; } = new NoOpRegistration();

        public static FeatureRegistrationResult Register()
        {
            return BueRuntimeHost.Register(ProbeRegistration);
        }

        private sealed class NoOpRegistration : IFeatureRegistration, IFeatureSettingsRegistration
        {
            public FeatureDefinitionArtifact Definition { get; } = new FeatureDefinitionArtifact(new FeatureId("io.github.yu80rice.bue.noop"), 1, "bue-noop", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new NoOpFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
            // DEV-V3-06: the settings facet — the sample declares its schema
            // (one ClientLocal toggle; 已承诺 scope only, no ServerAuthority
            // write entry offered — nothing is faked, per the ticket rule).
            public IReadOnlyList<SettingDescriptor> SettingDescriptors { get { return new[] { ProbeToggle }; } }
            public Action OnSettingsApplied { get { return null; } }
            private static readonly SettingDescriptor ProbeToggle = new SettingDescriptor(
                new FeatureId("io.github.yu80rice.bue.noop"), "noop.probe-toggle", "noop.probe-toggle", "noop.probe-toggle",
                SettingKind.Toggle, SettingAuthority.ClientLocal, SettingValue.Toggle(true),
                default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                null, 0, null, 1, 0, null, null);
        }

        private sealed class NoOpFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return new NoOpModule(); }
        }

        /// <summary>
        /// DEV-V3-03: the probe is no longer a static empty shell — Start
        /// consumes the frozen lifecycle seams exactly as an ecosystem feature
        /// should: one resource through TryTrack, one read-only status query,
        /// an honest Started=true outcome (the R1 inventory's「NoOp 的 Start
        /// 返回 Started=false 不练这些缝」gap closes here). DEV-V3-08 extends
        /// this probe to the full post-wiring chain.
        /// </summary>
        private sealed class NoOpModule : IFeatureModule
        {
            private ProbeResource resource;
            private IDisposable hostTickSubscription;

            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                var probe = new ProbeState();
                LastProbe = probe;
                resource = new ProbeResource(probe);
                probe.Tracked = bootstrap.Lifetime.TryTrack(resource);
                probe.QueriedStateAtStart = bootstrap.Lifetime.CurrentStatus.State;
                // DEV-V3-04: the dispatcher seam's ecosystem-side contrast —
                // observe the wired view and post through it (fire-and-
                // forget, an accepted explicit result; full-chain probe→08).
                if (bootstrap.MainThread != null)
                {
                    probe.MainThreadAvailable = true;
                    probe.MainThreadPosted = bootstrap.MainThread.Post(() => { }).Posted;
                }
                // DEV-V3-05 HostTick branch: subscribe the frozen clock seam
                // exactly as an ecosystem consumer should (the host drops the
                // subscription at the stop boundary — no self-teardown here);
                // record every tick's timing payload for the host-test side.
                if (bootstrap.Events != null)
                {
                    hostTickSubscription = bootstrap.Events.Subscribe<HostTick>(tick =>
                    {
                        probe.HostTicksReceived++;
                        if (probe.HostTicksReceived == 1) probe.FirstHostTickNumber = tick.TickNumber;
                        probe.LastHostTickNumber = tick.TickNumber;
                        probe.LastHostTickPhase = tick.Phase;
                        probe.LastHostTickDeltaSeconds = tick.DeltaTime;
                    });
                    probe.HostTickSubscribed = hostTickSubscription != null;
                }
                // DEV-V3-06 Settings 支线：读→合法提交观察 revision 推进→非法
                // 提交观察显式拒（请求号随 revision 前进取新值：同一 runtime
                // 跨 probe 启动共享，旧号重放会被幂等账本冲突拒——生态姿势
                // 演示：ExpectedRevision+新号）。
                var settings = bootstrap.Settings;
                if (settings != null)
                {
                    probe.SettingsAvailable = true;
                    var snapshot = settings.GetSnapshot(SettingRevisionScope.ClientPreference);
                    for (var i = 0; i < snapshot.Entries.Count; i++)
                    {
                        if (snapshot.Entries[i].SettingId == "noop.probe-toggle") probe.SettingsSchemaVisible = true;
                    }
                    var current = snapshot.Entries.Count == 0 ? SettingValue.Toggle(true) : snapshot.Entries[0].EffectiveValue;
                    var commit = settings.Submit(new ScopedSettingChangeRequest(
                        1000UL + snapshot.Revision, SettingRevisionScope.ClientPreference, snapshot.Revision,
                        new[] { new SettingMutation("noop.probe-toggle", SettingValue.Toggle(!current.Boolean)) }));
                    probe.SettingsCommitAccepted = commit.Accepted;
                    probe.SettingsRevisionAdvanced = commit.Accepted && commit.Revision > snapshot.Revision;
                    var invalid = settings.Submit(new ScopedSettingChangeRequest(
                        2000UL + snapshot.Revision, SettingRevisionScope.ClientPreference, commit.Revision,
                        new[] { new SettingMutation("noop.not-a-setting", SettingValue.Toggle(true)) }));
                    probe.SettingsInvalidRejected = !invalid.Accepted;
                }
                probe.Started = true;
                return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-NOOP-START");
            }

            public void Stop(FeatureStopReason reason) { }
        }

        private sealed class ProbeResource : IDisposable
        {
            private readonly ProbeState owner;

            internal ProbeResource(ProbeState owner) { this.owner = owner; }

            public void Dispose() { owner.ResourceDisposed = true; }
        }
    }
}
