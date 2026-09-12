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

    /// <summary>
    /// DEV-V3-08 (V3-T9 decision ④): one seam step of the unified ecosystem
    /// contract probe. NotRun is NEVER conflated with Passed — a chain that
    /// died at an earlier seam reports the later seams as NotRun instead of
    /// hiding behind a single whole-chain PASS/FAIL flag (the spec's
    /// per-seam-locatable rule). Sample fixture surface, not SDK contract.
    /// </summary>
    public enum ProbeStepOutcome { NotRun = 0, Passed = 1, Mismatch = 2 }

    /// <summary>
    /// DEV-V3-08: per-seam fault knobs that make one step observe a real
    /// contract behaviour it does NOT expect (the seam is then Mismatch while
    /// every other seam keeps running and keeps passing — the mechanism the
    /// red collection uses to prove per-seam locatability). The real seams
    /// are never faked: the knobs only choose which expectation the probe
    /// mis-states, so the bus/network/settings/diagnostics machinery answers
    /// with its genuine explicit results. Sample fixture surface, not SDK
    /// contract.
    /// </summary>
    public enum NoOpProbeFault
    {
        None = 0,
        // Events seam: publish under a wrong declared EventId (a route the
        // type never registered) while still expecting acceptance — the
        // frozen ownership-routing rejection makes EventsStep Mismatch.
        EventsPublishExpectation = 1,
        // Network seam: send through a channel never registered while still
        // expecting the frozen NoSession answer — the observed
        // ChannelNotRegistered makes NetworkStep Mismatch.
        NetworkSendExpectation = 2,
        // Settings seam: submit a well-formed mutation carrying a stale
        // ExpectedRevision while still expecting acceptance — the frozen
        // optimistic-concurrency rejection makes SettingsStep Mismatch.
        SettingsCommitExpectation = 3,
        // Lifecycle seam: track the first resource while EXPECTING the
        // rejection side — the genuine acceptance makes LifecycleStep
        // Mismatch (an expectation flip, real behaviour untouched).
        LifecycleTrackExpectation = 4,
        // Logger seam: add a null-eventName write (the frozen invalid-id
        // rejection writes BUE-LOG-002 instead of the module line — the
        // Logger seam's own independent criterion is diagnostic-line based,
        // so this knob feeds the host-side line assertion, not a status).
        LoggerInvalidEventWrite = 5,
        // Start seam: the module throws during Start — the host isolates
        // (per-feature isolation, never a host crash) and the chain's
        // remaining steps report NotRun, not Passed.
        StartFault = 6,
    }

    /// <summary>
    /// DEV-V3-08: the probe's own ecosystem event payload type — registered
    /// through the public IFeatureEventRegistry seam (its EventId derived
    /// from the probe's FeatureId) and round-tripped publish→subscribe to
    /// prove the events seam end to end. Sample fixture surface, not SDK
    /// contract.
    /// </summary>
    public struct NoOpProbeEvent
    {
        public ulong Sequence { get; }
        public NoOpProbeEvent(ulong sequence) { Sequence = sequence; }
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
            // DEV-V3-07 Logger 支线（T8 裁决③ probe 链生态侧最小对照，全链
            // probe→08）：探针经注入 view 走三方法窄面——void 面=调用不炸即
            // Written（异常隔离的生态侧观察），行与摘要计数断言归宿主缝
            // （聚合器不随样本走）。NoOp=白名单官方样例身份→BUE-NOOP-* 码
            // 合法（平台侧通过性）；生态前缀拒绝锚以 io.example 身份在宿主
            // 测试「BUE-* 前缀纪律」组另证，两者互不遮蔽。
            public bool LoggerAvailable;
            public bool LoggerInfoWritten;
            public bool LoggerWarningWritten;
            public bool LoggerErrorWritten;
            // ── DEV-V3-08 统一生态契约 probe（V3-T9 裁决④）：冻结链序
            // 注册→Bootstrap→Events→TryTrack/Lifecycle→Network→HostTick→
            // Settings→Logger→停止与隔离 的分 seam 状态。注册与停止/隔离
            // 两端由宿主测试驱动判定；此处七枚缝步各有独立判据，一步
            // Mismatch 不遮蔽其余（NotRun≠Passed=链未跑到≠通过）。
            public ProbeStepOutcome BootstrapStep = ProbeStepOutcome.NotRun;
            public ProbeStepOutcome EventsStep = ProbeStepOutcome.NotRun;
            public ProbeStepOutcome LifecycleStep = ProbeStepOutcome.NotRun;
            public ProbeStepOutcome NetworkStep = ProbeStepOutcome.NotRun;
            public ProbeStepOutcome HostTickStep = ProbeStepOutcome.NotRun;
            public ProbeStepOutcome SettingsStep = ProbeStepOutcome.NotRun;
            public ProbeStepOutcome LoggerStep = ProbeStepOutcome.NotRun;
            // 失败分 seam 可定位的诊断详情（一步一段 token，宿主测试按段核对）。
            public readonly List<string> StepMismatches = new List<string>();
            // Bootstrap 缝步：票后终态矩阵快照（08 附录 A.1 的活出处）。
            public ulong BootstrapGenerationAtStart;
            // Events 缝步：登记→订阅→自发自收→错挂拒 四判据。
            public bool EventsRouteOwned;           // Registered 或跨代幂等（自家重登 003）
            public bool EventsPublishAccepted;      // 正判据：自有 EventId 发布被收
            public bool EventsWrongIdRejected;      // 反判据：错挂声明被拒
            public int EventsSelfReceived;          // 自订阅收到数
            // TryTrack/Lifecycle 缝步：两资源+逆序释放账（DisposalOrder 由
            // 宿主停止边界写入，顺序判据在停止子组）。
            public bool TrackedSecond;
            public readonly List<string> DisposalOrder = new List<string>();
            // Network 缝步：登记/空快照/降级发送/负通道/主线程投递 五判据。
            public bool NetworkChannelOwned;        // Accepted 或跨代幂等（重登重复）
            public int NetworkSessionsAtStart;      // 宿主环境=0（established-only 快照）
            public BetterUnturnedExperience.Contracts.BueNetwork.NetworkSendResult NetworkSendObserved;
            public BetterUnturnedExperience.Contracts.BueNetwork.NetworkSendResult NetworkWrongChannelObserved;
            // Settings 缝步（在 06 三判据上补反判据）：过期 ExpectedRevision 拒。
            public bool SettingsStaleRejected;
            public uint SettingsRevisionAfterCommit;
            // Logger 缝步：null 事件名负写（07 冻结=拒写+BUE-LOG-002 留痕；
            // void 面对探针不可观察，判据=宿主侧诊断行）。
            public bool LoggerInvalidAttempt;
            // 停止与隔离边界=宿主侧判据，但需要缝视图在停止后仍可触达（样本
            // 面：宿主测试用捕获的 view 尝试停止后写入，观察显式拒绝）。
            public IFeatureLifetime LifetimeView;
            public IScopedFeatureSettings SettingsView;
            public IFeatureLogger LoggerView;
            public IFeatureMainThread MainThreadView;
        }

        public static ProbeState LastProbe { get; private set; }

        /// <summary>
        /// DEV-V3-08: the probe's own event identity — derived from the
        /// probe FeatureId per the frozen &lt;owner&gt;/&lt;event-name&gt; rule,
        /// registered through bootstrap.EventRegistry and round-tripped to
        /// prove the events seam (SDK appendix A.2 quotes this constant).
        /// </summary>
        public const string ProbeEventId = "io.github.yu80rice.bue.noop/probe-completed";

        /// <summary>DEV-V3-08: the wrong-declared-id the events seam uses as
        /// its negative leg — an id under the probe's own segment that no
        /// payload type ever registered (publish must be rejected).</summary>
        public const string ProbeWrongEventId = "io.github.yu80rice.bue.noop/probe-not-registered";

        /// <summary>DEV-V3-08: the network channel key the probe registers
        /// (FeatureId IS the channel name, one module = one channel).</summary>
        public const string ProbeChannelId = "io.github.yu80rice.bue.noop";

        /// <summary>DEV-V3-08: negative-leg channel — never registered on
        /// this runtime (the send must answer ChannelNotRegistered).</summary>
        public const string ProbeWrongChannelId = "io.example.noop-negative-channel";

        /// <summary>DEV-V3-08: the seam the NEXT created probe module will
        /// mis-expect (fault knob for the per-seam-locatability red proof;
        /// the host test sets it before StartCatalog and restores None).
        /// Sample fixture surface, not SDK contract.</summary>
        public static NoOpProbeFault NextProbeFault = NoOpProbeFault.None;

        /// <summary>DEV-V3-08: host-test hygiene seam — clear the observability
        /// record so a rejected registration proves the chain never ran
        /// (LastProbe stays null; NotRun is never conflated with Passed).</summary>
        public static void ResetLastProbe() { LastProbe = null; }

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
            // DEV-V3-06 → DEV-V4-07: the settings facet — the sample declares
            // its schema per the Q64 frozen copy: the kept toggle row
            // (probe-toggle, 探针开关) plus the NEW choice row (probe-choice,
            // 探针档位, 甲/乙, default 甲) so ecosystem authors can compare
            // Toggle AND Cycle rendering against the same fixture (T1 检验点③;
            // NoOp 不替代 LIT 对 Choice 的官方先行消费). Both rows are plain
            // ClientPreference settings — NEITHER is a lifecycle proxy and
            // NEITHER registers a legacy alias (04 对拍). 已承诺 scope only,
            // no ServerAuthority write entry offered — nothing is faked.
            public IReadOnlyList<SettingDescriptor> SettingDescriptors { get { return new[] { ProbeToggle, ProbeChoice }; } }
            public Action OnSettingsApplied { get { return null; } }
            private static readonly SettingDescriptor ProbeToggle = new SettingDescriptor(
                new FeatureId("io.github.yu80rice.bue.noop"), "noop.probe-toggle", "探针开关", "样板用的开关，证明生态 Toggle 能出现在面板。",
                SettingKind.Toggle, SettingAuthority.ClientLocal, SettingValue.Toggle(true),
                default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                null, 0, null, 1, 0, null, null);
            // MaximumUtf8Bytes=16 covers 甲/乙 (3 UTF-8 bytes each): the
            // runtime byte gate is exact (0 would reject every Choice submit).
            private static readonly SettingDescriptor ProbeChoice = new SettingDescriptor(
                new FeatureId("io.github.yu80rice.bue.noop"), "noop.probe-choice", "探针档位", "样板用的循环切换，证明生态 Choice 能出现在面板。",
                SettingKind.Choice, SettingAuthority.ClientLocal, SettingValue.Choice(ProbeChoiceDefault),
                default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                new[] { SettingValue.Choice(ProbeChoiceDefault), SettingValue.Choice(ProbeChoiceAlternate) },
                16, null, 1, 1, null, null);
            internal const string ProbeChoiceDefault = "甲";
            internal const string ProbeChoiceAlternate = "乙";
        }

        private sealed class NoOpFactory : IFeatureModuleFactory
        {
            // DEV-V3-08: the module is created against the CURRENT fault knob
            // value — the host test arms one seam, starts the catalog, then
            // restores None.
            public IFeatureModule Create() { return new NoOpModule(NextProbeFault); }
        }

        /// <summary>
        /// DEV-V3-08 (V3-T9 裁决④): the unified ecosystem contract probe.
        /// Start walks the FROZEN chain order — Bootstrap → Events →
        /// TryTrack/Lifecycle → Network → HostTick → Settings → Logger — and
        /// every seam records its own outcome (ProbeStepOutcome) through its
        /// own explicit criteria; a mismatched seam never aborts the chain and
        /// never hides the other seams (the registration and stop/isolation
        /// ends of the chain are host-driven and asserted by the host tests
        /// on this same ProbeState + the structured diagnostic lines). This
        /// is the living counterpart of SDK appendices A/B (bidirectional
        /// anchor): appendix A's example blocks quote this chain's key lines
        /// verbatim line-by-line (the machine anchor is per-line; A.5 rides on
        /// the DEV-V3-05 branch above). The DEV-V3-03 lifecycle consumption, DEV-V3-04 dispatcher
        /// contrast, DEV-V3-05 clock branch, DEV-V3-06 settings branch and
        /// DEV-V3-07 logger branch all remain (their fields keep being
        /// written — the earlier tickets' groups anchor them unchanged).
        /// </summary>
        private sealed class NoOpModule : IFeatureModule
        {
            private readonly NoOpProbeFault fault;
            private ProbeResource first;
            private ProbeResource second;
            private IDisposable hostTickSubscription;

            internal NoOpModule(NoOpProbeFault fault) { this.fault = fault; }

            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                var probe = new ProbeState();
                LastProbe = probe;
                if (fault == NoOpProbeFault.StartFault)
                {
                    // The start seam's own failure mode: a real author-side
                    // crash. The host isolates the feature (per-feature
                    // isolation, never a host crash); the probe exists but
                    // every step stays NotRun — proving NotRun≠Passed.
                    throw new InvalidOperationException("DEV-V3-08 probe knob: simulated Start crash");
                }
                RunBootstrapStep(probe, bootstrap);
                RunEventsStep(probe, bootstrap);
                RunLifecycleStep(probe, bootstrap);
                RunNetworkStep(probe, bootstrap);
                RunHostTickStep(probe, bootstrap);
                RunSettingsStep(probe, bootstrap);
                RunLoggerStep(probe, bootstrap);
                probe.Started = true;
                return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-NOOP-START");
            }

            public void Stop(FeatureStopReason reason) { }

            private void Mismatch(ProbeState probe, string step, string detail)
            {
                probe.StepMismatches.Add(step + ": " + detail);
            }

            // ── Bootstrap seam: the availability matrix, post-ticket final
            // state (spec「成员可用性矩阵」terminal column): every member is
            // composed by the host start path; Settings is non-null because
            // this sample DECLARED its facet (no-facet features keep the
            // honest null — the same rule the 06 matrix anchors). ──
            private void RunBootstrapStep(ProbeState probe, IFeatureBootstrap bootstrap)
            {
                try
                {
                    probe.BootstrapGenerationAtStart = bootstrap.LifecycleGeneration;
                    probe.LifetimeView = bootstrap.Lifetime;
                    probe.SettingsView = bootstrap.Settings;
                    probe.LoggerView = bootstrap.Logger;
                    probe.MainThreadView = bootstrap.MainThread;
                    var allWired = bootstrap.Identity.Id.Value == "io.github.yu80rice.bue.noop"
                        && bootstrap.LifecycleGeneration != 0UL
                        && bootstrap.Events != null && bootstrap.OwnedEvents != null && bootstrap.EventRegistry != null
                        && bootstrap.Network != null && bootstrap.Lifetime != null && bootstrap.Dependencies != null
                        && bootstrap.MainThread != null && bootstrap.Logger != null && bootstrap.Settings != null;
                    probe.BootstrapStep = allWired ? ProbeStepOutcome.Passed : ProbeStepOutcome.Mismatch;
                    if (!allWired) Mismatch(probe, "bootstrap", "matrix-member-missing");
                }
                catch (Exception error)
                {
                    probe.BootstrapStep = ProbeStepOutcome.Mismatch;
                    Mismatch(probe, "bootstrap", error.GetType().Name);
                }
            }

            // ── Events seam: register the probe's own payload type through
            // the public registry seam, subscribe, publish under the owning
            // id (must dispatch) and under a WRONG declared id (must be
            // rejected, zero extra dispatch). Re-registration on a later
            // generation returns the explicit duplicate rejection — the route
            // stays owned, so the step accepts Registered OR
            // EventTypeAlreadyRegistered (idempotent restart, appendix A.2). ──
            private void RunEventsStep(ProbeState probe, IFeatureBootstrap bootstrap)
            {
                try
                {
                    var registration = bootstrap.EventRegistry.Register<NoOpProbeEvent>(ProbeEventId);
                    probe.EventsRouteOwned = registration.Registered
                        || registration.Reason == FeatureEventRegistrationReason.EventTypeAlreadyRegistered;
                    var subscription = bootstrap.Events.Subscribe<NoOpProbeEvent>(e => probe.EventsSelfReceived++);
                    probe.EventsWrongIdRejected = !bootstrap.OwnedEvents.TryPublish(ProbeWrongEventId, new NoOpProbeEvent(0UL));
                    var publishId = fault == NoOpProbeFault.EventsPublishExpectation ? ProbeWrongEventId : ProbeEventId;
                    probe.EventsPublishAccepted = bootstrap.OwnedEvents.TryPublish(publishId, new NoOpProbeEvent(1UL));
                    var ok = probe.EventsRouteOwned && subscription != null
                        && probe.EventsPublishAccepted && probe.EventsWrongIdRejected && probe.EventsSelfReceived >= 1;
                    probe.EventsStep = ok ? ProbeStepOutcome.Passed : ProbeStepOutcome.Mismatch;
                    if (!ok)
                        Mismatch(probe, "events", "owned=" + probe.EventsRouteOwned + " published=" + probe.EventsPublishAccepted
                            + " wrongRejected=" + probe.EventsWrongIdRejected + " received=" + probe.EventsSelfReceived);
                }
                catch (Exception error)
                {
                    probe.EventsStep = ProbeStepOutcome.Mismatch;
                    Mismatch(probe, "events", error.GetType().Name);
                }
            }

            // ── TryTrack/Lifecycle seam: two tracked resources (the pair
            // proves the reverse-order release at the host stop boundary) +
            // the read-only status query mid-Start (Starting — pinned by the
            // host tests). The fault knob flips the FIRST expectation only:
            // the genuine acceptance becomes a recorded mismatch. ──
            private void RunLifecycleStep(ProbeState probe, IFeatureBootstrap bootstrap)
            {
                try
                {
                    first = new ProbeResource(probe, "first");
                    var trackedFirst = bootstrap.Lifetime.TryTrack(first);
                    probe.Tracked = trackedFirst;
                    second = new ProbeResource(probe, "second");
                    probe.TrackedSecond = bootstrap.Lifetime.TryTrack(second);
                    probe.QueriedStateAtStart = bootstrap.Lifetime.CurrentStatus.State;
                    var expectFirstRejected = fault == NoOpProbeFault.LifecycleTrackExpectation;
                    var ok = (trackedFirst != expectFirstRejected) && probe.TrackedSecond;
                    probe.LifecycleStep = ok ? ProbeStepOutcome.Passed : ProbeStepOutcome.Mismatch;
                    if (!ok) Mismatch(probe, "lifecycle", "trackedFirst=" + trackedFirst + " trackedSecond=" + probe.TrackedSecond);
                }
                catch (Exception error)
                {
                    probe.LifecycleStep = ProbeStepOutcome.Mismatch;
                    Mismatch(probe, "lifecycle", error.GetType().Name);
                }
            }

            // ── Network seam (with the dispatcher contrast, appendix A.4):
            // register the probe channel (re-registration on a later
            // generation is the explicit duplicate — the route persists, so
            // Owned covers both), observe the established-only empty snapshot
            // and the frozen explicit degrade results: send on the owned
            // channel with no session = NoSession, send on a never-
            // registered channel = ChannelNotRegistered (negative leg). The
            // fault knob sends through the WRONG channel while still
            // expecting NoSession — the genuine rejection mis-states the
            // expectation and only this seam turns red. ──
            private void RunNetworkStep(ProbeState probe, IFeatureBootstrap bootstrap)
            {
                try
                {
                    var channel = new FeatureId(ProbeChannelId);
                    var registration = bootstrap.Network.RegisterChannel(channel, new ContractVersion(2, 0), 1);
                    probe.NetworkChannelOwned = registration.Accepted
                        || registration.Reason == FeatureRegistrationReason.DuplicateFeature;
                    probe.NetworkSessionsAtStart = bootstrap.Network.Sessions == null ? -1 : bootstrap.Network.Sessions.Count;
                    var sendChannel = fault == NoOpProbeFault.NetworkSendExpectation ? new FeatureId(ProbeWrongChannelId) : channel;
                    probe.NetworkSendObserved = bootstrap.Network.SendToClients(sendChannel, new byte[] { 1 }, false);
                    probe.NetworkWrongChannelObserved = bootstrap.Network.SendToClients(new FeatureId(ProbeWrongChannelId), new byte[] { 2 }, false);
                    if (bootstrap.MainThread != null)
                    {
                        probe.MainThreadAvailable = true;
                        probe.MainThreadPosted = bootstrap.MainThread.Post(() => { }).Posted;
                    }
                    var ok = probe.NetworkChannelOwned && probe.NetworkSessionsAtStart == 0
                        && probe.NetworkSendObserved == BetterUnturnedExperience.Contracts.BueNetwork.NetworkSendResult.NoSession
                        && probe.NetworkWrongChannelObserved == BetterUnturnedExperience.Contracts.BueNetwork.NetworkSendResult.ChannelNotRegistered
                        && probe.MainThreadPosted;
                    probe.NetworkStep = ok ? ProbeStepOutcome.Passed : ProbeStepOutcome.Mismatch;
                    if (!ok)
                        Mismatch(probe, "network", "owned=" + probe.NetworkChannelOwned + " sessions=" + probe.NetworkSessionsAtStart
                            + " send=" + probe.NetworkSendObserved + " wrong=" + probe.NetworkWrongChannelObserved + " posted=" + probe.MainThreadPosted);
                }
                catch (Exception error)
                {
                    probe.NetworkStep = ProbeStepOutcome.Mismatch;
                    Mismatch(probe, "network", error.GetType().Name);
                }
            }

            // ── HostTick seam: the frozen clock subscription (the host drops
            // it at the stop boundary — no self-teardown; receipts/sequence
            // are pinned by the host pumps, DEV-V3-05 semantics). ──
            private void RunHostTickStep(ProbeState probe, IFeatureBootstrap bootstrap)
            {
                try
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
                    probe.HostTickStep = probe.HostTickSubscribed ? ProbeStepOutcome.Passed : ProbeStepOutcome.Mismatch;
                    if (!probe.HostTickSubscribed) Mismatch(probe, "hosttick", "subscribe-null");
                }
                catch (Exception error)
                {
                    probe.HostTickStep = ProbeStepOutcome.Mismatch;
                    Mismatch(probe, "hosttick", error.GetType().Name);
                }
            }

            // ── Settings seam (DEV-V3-06 branch + stale-revision negative
            // leg): read the snapshot, commit a legal change (revision must
            // advance), reject an unknown setting id, reject a replay under
            // the now-stale ExpectedRevision (optimistic concurrency). The
            // fault knob submits a well-formed mutation with a stale
            // revision while still expecting acceptance. ──
            private void RunSettingsStep(ProbeState probe, IFeatureBootstrap bootstrap)
            {
                try
                {
                    var settings = bootstrap.Settings;
                    if (settings == null)
                    {
                        probe.SettingsStep = ProbeStepOutcome.Mismatch;
                        Mismatch(probe, "settings", "not-wired");
                        return;
                    }
                    probe.SettingsAvailable = true;
                    var snapshot = settings.GetSnapshot(SettingRevisionScope.ClientPreference);
                    for (var i = 0; i < snapshot.Entries.Count; i++)
                    {
                        if (snapshot.Entries[i].SettingId == "noop.probe-toggle") probe.SettingsSchemaVisible = true;
                    }
                    // DEV-V4-07: the facet now has TWO rows (probe-toggle +
                    // probe-choice) — locate the toggle row by id instead of
                    // assuming snapshot order, so the chain criteria stay
                    // order-independent while the fixture grows.
                    var current = SettingValue.Toggle(true);
                    for (var i = 0; i < snapshot.Entries.Count; i++)
                    {
                        if (snapshot.Entries[i].SettingId == "noop.probe-toggle") { current = snapshot.Entries[i].EffectiveValue; break; }
                    }
                    var mutation = new[] { new SettingMutation("noop.probe-toggle", SettingValue.Toggle(!current.Boolean)) };
                    var expectedRevision = fault == NoOpProbeFault.SettingsCommitExpectation
                        ? snapshot.Revision + 777u : snapshot.Revision;
                    var commit = settings.Submit(new ScopedSettingChangeRequest(
                        1000UL + snapshot.Revision, SettingRevisionScope.ClientPreference, expectedRevision, mutation));
                    probe.SettingsCommitAccepted = commit.Accepted;
                    probe.SettingsRevisionAdvanced = commit.Accepted && commit.Revision > snapshot.Revision;
                    probe.SettingsRevisionAfterCommit = commit.Revision;
                    var invalid = settings.Submit(new ScopedSettingChangeRequest(
                        2000UL + snapshot.Revision, SettingRevisionScope.ClientPreference, commit.Revision,
                        new[] { new SettingMutation("noop.not-a-setting", SettingValue.Toggle(true)) }));
                    probe.SettingsInvalidRejected = !invalid.Accepted;
                    if (commit.Accepted)
                    {
                        var stale = settings.Submit(new ScopedSettingChangeRequest(
                            3000UL + snapshot.Revision, SettingRevisionScope.ClientPreference, snapshot.Revision, mutation));
                        probe.SettingsStaleRejected = !stale.Accepted;
                    }
                    var ok = probe.SettingsAvailable && probe.SettingsSchemaVisible && probe.SettingsCommitAccepted
                        && probe.SettingsRevisionAdvanced && probe.SettingsInvalidRejected && probe.SettingsStaleRejected;
                    probe.SettingsStep = ok ? ProbeStepOutcome.Passed : ProbeStepOutcome.Mismatch;
                    if (!ok)
                        Mismatch(probe, "settings", "schema=" + probe.SettingsSchemaVisible + " accepted=" + probe.SettingsCommitAccepted
                            + " advanced=" + probe.SettingsRevisionAdvanced + " invalidRejected=" + probe.SettingsInvalidRejected
                            + " staleRejected=" + probe.SettingsStaleRejected);
                }
                catch (Exception error)
                {
                    probe.SettingsStep = ProbeStepOutcome.Mismatch;
                    Mismatch(probe, "settings", error.GetType().Name);
                }
            }

            // ── Logger seam (DEV-V3-07 branch): the three-method narrow
            // surface, one call each; void = surviving the calls is the
            // probe-side criterion, the structured lines/summary are pinned
            // host-side. The knob adds the frozen invalid-identifier write
            // (BUE-LOG-002 replaces the module line — the Logger seam's own
            // line-level negative criterion). ──
            private void RunLoggerStep(ProbeState probe, IFeatureBootstrap bootstrap)
            {
                try
                {
                    var logger = bootstrap.Logger;
                    if (logger == null)
                    {
                        probe.LoggerStep = ProbeStepOutcome.Mismatch;
                        Mismatch(probe, "logger", "not-wired");
                        return;
                    }
                    probe.LoggerAvailable = true;
                    logger.Info("noop-probe-info", "BUE-NOOP-INFO");
                    probe.LoggerInfoWritten = true;
                    logger.Warning("noop-probe-warning", FrameworkErrorCode.SettingRejected, "BUE-NOOP-WARN");
                    probe.LoggerWarningWritten = true;
                    logger.Error("noop-probe-error", FrameworkErrorCode.None, "BUE-NOOP-ERROR", null);
                    probe.LoggerErrorWritten = true;
                    if (fault == NoOpProbeFault.LoggerInvalidEventWrite)
                    {
                        logger.Info(null, "BUE-NOOP-INVALID");
                        probe.LoggerInvalidAttempt = true;
                    }
                    probe.LoggerStep = ProbeStepOutcome.Passed;
                }
                catch (Exception error)
                {
                    probe.LoggerStep = ProbeStepOutcome.Mismatch;
                    Mismatch(probe, "logger", error.GetType().Name);
                }
            }
        }

        private sealed class ProbeResource : IDisposable
        {
            private readonly ProbeState owner;
            private readonly string name;

            internal ProbeResource(ProbeState owner, string name)
            {
                this.owner = owner;
                this.name = name;
            }

            public void Dispose()
            {
                // Reverse-order release at the stop boundary is observable
                // through this record (08 stop seam: [second, first]).
                owner.ResourceDisposed = true;
                owner.DisposalOrder.Add(name);
            }
        }
    }
}
