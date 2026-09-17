using System;
using System.Collections.Generic;
using System.Reflection;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lir;
using SDG.Unturned;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V5-07（V5-T7 → 换弹技能 0～2 级与技能菜单分区）红测先行组。判据面 =
    /// 票面验收条逐字对位：0 级双击仍可用（合并技能窗 = 技术闸 1.5s + 额外 8s，
    /// 窗内再双击 = 红色剩余秒、主机为准）；1 级额外冷却消失、技术闸仍在
    /// （ReloadRuntimePolicy.CooldownSeconds 不删不改，纯技术拒绝保持现网静默）；
    /// 2 级等待后再压、条件不满足则取消（同枪同匣指纹/等级仍 2/玩家可解析）；
    /// 升级扣原版经验（主机校验余额后扣，经验不足零扣零改）；无三级 UI
    /// （等级模型冻结 0..2，分区投影不含 3 级）。具名常量 = 票面 Comments
    /// 2026-09-16 落名：125 / 150 / 8s / 8s / 2。红测只引用 ReloadSkillPolicy
    /// 常量与票面字面值的一次性钉，不在测试里发明第二套数值。
    /// 真机面（askSpend/characterName/枪械指纹的引擎接触、Glazier 实际注入几何）
    /// 为具名接缝缺口（03/04/05/06 同界），实机随 DEV-V5-08。
    /// </summary>
    internal static class DevV5ReloadSkillTests
    {
        internal static void Run(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            LirRuntime.MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    var savedInstaller = InPlaceReloadModule.SkillPatchInstallerForTests;
                    var savedCoreInstaller = InPlaceReloadModule.CorePatchInstallerForTests;
                    var savedHudInstaller = InPlaceReloadModule.HudPatchInstallerForTests;
                    var savedHeadless = BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision;
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        var reported = error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message;
                        if (error.InnerException != null) reported += " <INNER " + error.InnerException.GetType().Name + ": " + error.Message + ">";
                        reds.Add("[" + name + "] " + reported);
                    }
                    finally
                    {
                        if (InPlaceReloadModule.ActiveModule != null)
                        {
                            try { InPlaceReloadModule.ActiveModule.Stop(FeatureStopReason.PluginStopping); } catch (Exception) { }
                        }
                        InPlaceReloadModule.SkillPatchInstallerForTests = savedInstaller;
                        InPlaceReloadModule.CorePatchInstallerForTests = savedCoreInstaller;
                        InPlaceReloadModule.HudPatchInstallerForTests = savedHudInstaller;
                        BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision = savedHeadless;
                        ReloadSkillLevelMirror.Clear();
                    }
                }

                Group("具名常量与文案单源", () => V57GroupPolicyConstants(Check));
                Group("技能冷却三层（0 可用/1 去额外技术闸在/红色剩余主机为准）", () => V57GroupCooldown(Check));
                Group("升级扣经验与等级读写", () => V57GroupUpgrade(Check));
                Group("2 级自动压弹（等待/重检/取消/不再触发）", () => V57GroupAutoRound(Check));
                Group("官方先行消费与两种表面", () => V57GroupOfficialFirst(Check));
                Group("生命周期登记与四互撤", () => V57GroupLifecycle(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V5-07 reload-skill collection: ALL GREEN (0 failures) — groups: 常量/冷却/升级/自动压弹/官方先行与表面/生命周期");
            Console.WriteLine("DEV-V5-07 reload-skill tests: " + (collectAllFailures && reds.Count > 0 ? "FAIL" : "PASS"));
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V5-07 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // ─────────────────────────────────────────────────────────────────
        // harness —— 模块 Start 装配 + 技能假钩子/假权威/假网络/手拨时钟/
        // 内存持久化（生产文件面与引擎面 = 具名缺口随 08，本文件全部走缝）。
        // ─────────────────────────────────────────────────────────────────

        private static HostTick V57Tick(ulong number, float deltaSeconds)
        {
            return new HostTick(number, deltaSeconds, TickPhase.Update);
        }

        private sealed class V57Clock
        {
            public double Now;
            public double Read() { return Now; }
            public void Advance(double seconds) { Now += seconds; }
        }

        private sealed class V57Persistence : IReloadSkillPersistence
        {
            public List<ReloadSkillRecord> Records = new List<ReloadSkillRecord>();
            public int SaveCalls;
            public int LoadCalls;
            public bool FailSave;

            public bool TryLoad(out List<ReloadSkillRecord> records, out string error)
            {
                LoadCalls++;
                records = new List<ReloadSkillRecord>(Records);
                error = null;
                return true;
            }

            public bool TrySave(IReadOnlyList<ReloadSkillRecord> records, out string error)
            {
                SaveCalls++;
                if (FailSave) { error = "synthetic save failure"; return false; }
                Records = new List<ReloadSkillRecord>(records);
                error = null;
                return true;
            }
        }

        /// <summary>可编程假技能钩子（生产实现=模块自身，本缝替换其全部引擎接触）。</summary>
        /// <summary>可编程假技能钩子（生产实现=LirSkillEngineHooks 引擎面，本缝
        /// 替换其全部引擎接触；调度/升级编排留在模块，钩子只供数据与引擎动作）。</summary>
        private sealed class V57Hooks : ILirSkillHooks
        {
            public int WindowChecks;                  // TryBeginRepackWindow 调用计数（功能 A 不读窗的反证）
            public int GetLevelForCalls;              // 等级读=LIR 账的计数
            public double RemainingOnReject = -1d;    // >=0 时 TryBeginRepackWindow 拒绝（剩余秒）；-1=放行
            public byte Level = 1;                    // GetLevelFor / TryResolveLocalLevel 返回值
            public bool LocalResolvable = true;
            public object Fingerprint;                // CaptureFingerprint 返回值
            public object FreshFingerprint;           // 到点重检用新指纹
            public List<(ulong SteamId, byte Target)> Upgrades = new List<(ulong, byte)>();
            public ReloadSkillUpgradeDecision UpgradeDecision;

            public int ArmCalls;
            public bool TryBeginRepackWindow(ulong steamId, out double remainingSeconds)
            {
                WindowChecks++;
                remainingSeconds = RemainingOnReject < 0d ? 0d : RemainingOnReject;
                return RemainingOnReject < 0d;
            }

            public void ArmRepackWindowAfterCommit(ulong steamId) { ArmCalls++; }

            public ReloadSkillUpgradeDecision ExecuteUpgrade(ulong steamId, byte targetLevel)
            {
                Upgrades.Add((steamId, targetLevel));
                return UpgradeDecision;
            }

            public byte GetLevelFor(ulong steamId) { GetLevelForCalls++; return Level; }

            public bool TryResolveLocalLevel(out byte level)
            {
                level = Level;
                return LocalResolvable;
            }

            // 第一次 Capture（成交排轮时）给 Fingerprint；之后（到点重检）给
            // FreshFingerprint——两值不同=「换枪」，相同=枪匣未动。
            private int captures;
            public object CaptureFingerprint(ulong steamId)
            {
                captures++;
                return captures == 1 ? Fingerprint : FreshFingerprint ?? Fingerprint;
            }

            public bool FingerprintMatches(object captured, object fresh)
            {
                return captured == null ? fresh == null : captured.Equals(fresh);
            }
        }

        private sealed class V57Authority : ILirRepackAuthority
        {
            public LirRepackOutcome Outcome = LirRepackOutcome.NoChange;
            public int Total = 0;
            public int RepackCalls;
            public ulong LocalId;

            public int HostInitiatedCalls;
            public LirRepackExecution ExecuteRepack(ulong senderSteamId, ulong requestId, bool hostInitiated = false)
            {
                RepackCalls++;
                if (hostInitiated) HostInitiatedCalls++;
                return new LirRepackExecution { Outcome = Outcome, TotalTransferred = Total };
            }

            public LirMergeExecution ExecuteMerge(ulong targetSteamId, ulong requestId)
            { return new LirMergeExecution { Outcome = LirMergeOutcome.NoChange, TotalMerged = 0 }; }

            public bool TryResolveLocalPlayerSteamId(out ulong steamId) { steamId = LocalId; return LocalId != 0UL; }
        }

        /// <summary>假网络：捕获全部出站帧 + 可注入会话 + 手动派发入站帧。</summary>
        private sealed class V57Network : IBueNetworkApi
        {
            public List<(ChannelDirection Dir, IConnectionSession Session, byte[] Payload)> Sent = new List<(ChannelDirection, IConnectionSession, byte[])>();
            public List<(ChannelDirection Dir, Action<IConnectionSession, byte[]> Handler)> Subscriptions = new List<(ChannelDirection, Action<IConnectionSession, byte[]>)>();
            public List<IConnectionSession> LiveSessions = new List<IConnectionSession>();
            public NetworkSendResult SendResult = NetworkSendResult.Sent;

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "V57"); }

            public bool UnregisterChannel(FeatureId channel) { return true; }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            {
                Subscriptions.Add((direction, handler));
                return new V57Handle();
            }

            public IReadOnlyList<IConnectionSession> Sessions { get { return LiveSessions; } }

            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable)
            { Sent.Add((ChannelDirection.FromClients, null, payload)); return SendResult; }

            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
            { Sent.Add((ChannelDirection.FromServer, null, payload)); return SendResult; }

            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable)
            { Sent.Add((ChannelDirection.FromServer, session, payload)); return SendResult; }

            public void DispatchInbound(ChannelDirection dir, IConnectionSession session, byte[] payload)
            {
                foreach (var pair in Subscriptions)
                {
                    if (pair.Dir == dir) pair.Handler(session, payload);
                }
            }

            private sealed class V57Handle : IDisposable { public void Dispose() { } }
        }

        private sealed class V57Session : IConnectionSession
        {
            public V57Session(ulong sessionId, ulong peerSteamId) { SessionId = sessionId; PeerSteamId = peerSteamId; }
            public ulong SessionId { get; }
            public ulong PeerSteamId { get; }
            public ContractVersion PeerContract { get { return new ContractVersion(2, 1); } }
            public ushort PeerFeatureVersion { get { return 1; } }
            public IReadOnlyList<ChannelVersionEntry> Channels { get { return new ChannelVersionEntry[0]; } }
            public event System.Action Connected { add { } remove { } }
            public event System.Action Disconnected { add { } remove { } }
            public event System.Action<ulong> GenerationChanged { add { } remove { } }
            public NetworkSendResult Send(byte[] payload, bool reliable) { return NetworkSendResult.Sent; }
        }

        /// <summary>模块装配（缝全开：假钩子/假权威/假网络/手拨时钟/内存持久化/
        /// 假补丁安装器——三面登记走缝，红测不装真补丁）。</summary>
        private static InPlaceReloadModule V57StartModule(
            V57Hooks hooks, V57Authority authority, V57Network network, V57Clock clock,
            V57Persistence persistence, bool isServer, out List<string> toasts,
            System.Action<bool, string> check, bool installSkillSurface = true, bool installHudSurface = true)
        {
            var sink = new List<string>();
            toasts = sink;
            InPlaceReloadModule.SkillPatchInstallerForTests = _ => installSkillSurface;
            InPlaceReloadModule.CorePatchInstallerForTests = _ => true;
            InPlaceReloadModule.HudPatchInstallerForTests = _ => installHudSurface;
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var feature = new FeatureId(LirRuntime.FeatureIdValue);
            var module = new InPlaceReloadModule(feature);
            module.AuthorityFactoryForTests = () => authority;
            module.NetServiceFactoryForTests = (m, net) => new LirRepackNetwork(net, m.Authority, () => isServer);
            module.KeyDownProviderForTests = () => false;
            module.RoleProbeForTests = () => isServer;
            module.SkillHooksForTests = hooks;
            module.SkillClockForTests = clock == null ? (Func<double>)null : clock.Read;
            module.SkillPersistenceForTests = persistence ?? new V57Persistence();
            module.ToastSink = line => { lock (sink) sink.Add(line); }; // 前置绑定，避开 LirToast.Show 引擎面
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, network);
            var result = module.Start(bootstrap);
            check(result.Started, "harness: LIR 模块 Start 失败: " + result.DiagnosticId);
            return module;
        }

        /// <summary>把已启动模块推进 n 拍（每拍 delta 秒）；时钟同步手拨。</summary>
        private static void V57Pump(InPlaceReloadModule module, V57Clock clock, ulong ticks, float deltaSeconds, ulong firstTick = 1UL)
        {
            for (var i = 0UL; i < ticks; i++)
            {
                clock.Advance(deltaSeconds);
                module.OnHostTick(V57Tick(firstTick + i, deltaSeconds));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // 组1 —— 票面具名常量逐字钉 + 文案单源。票面 Comments（2026-09-16）
        // 落名 125/150/8/8/2；本组是全文件唯一允许出现这些字面值的地方，
        // 其余各组行为断言只引用 ReloadSkillPolicy 常量（禁止测试发明数值）。
        // ─────────────────────────────────────────────────────────────────
        private static void V57GroupPolicyConstants(System.Action<bool, string> check)
        {
            check(ReloadSkillPolicy.XpCostLevel0To1 == 125, "0→1 经验花费 = 票面具名 125");
            check(ReloadSkillPolicy.XpCostLevel1To2 == 150, "1→2 经验花费 = 票面具名 150");
            check(ReloadSkillPolicy.Level0ExtraCooldownSeconds == 8f, "0 级额外技能冷却 = 票面具名 8 秒");
            check(ReloadSkillPolicy.AutoRoundDelaySeconds == 8f, "2 级自动压弹等待 = 规格建议 8s（本阶段不可设）");
            check(ReloadSkillPolicy.MaxSkillLevel == 2, "技能只到 2 级：无三级");
            check(ReloadRuntimePolicy.CooldownSeconds == 1.5f && ReloadRuntimePolicy.DoubleClickWindowSeconds == 0.3f,
                "技术闸沿用现网（1.5s 不得删不改），双击窗 0.3s 不动");

            check(ReloadSkillPolicy.CostForUpgrade(0) == ReloadSkillPolicy.XpCostLevel0To1
                && ReloadSkillPolicy.CostForUpgrade(1) == ReloadSkillPolicy.XpCostLevel1To2
                && ReloadSkillPolicy.CostForUpgrade(ReloadSkillPolicy.MaxSkillLevel) < 0,
                "花费表单源：CostForUpgrade 只由票面常量组装，满级无花费（无三级消费）");
            check(ReloadSkillPolicy.ExtraCooldownSeconds(0) == ReloadSkillPolicy.Level0ExtraCooldownSeconds
                && ReloadSkillPolicy.ExtraCooldownSeconds(1) == 0d
                && ReloadSkillPolicy.ExtraCooldownSeconds(2) == 0d,
                "额外冷却只属 0 级；1/2 级 = 0（取消额外段，技术闸另在 LirRepackGate）");

            check(ReloadSkillPolicy.LevelName(0) == "基础" && ReloadSkillPolicy.LevelName(1) == "快速换弹"
                && ReloadSkillPolicy.LevelName(2) == "自动压弹", "等级名冻结（T7 名称意向）");
            check(ReloadSkillPolicy.MakeCooldownToast(9.4d).Contains("换弹技能冷却中：还需 10 秒")
                && ReloadSkillPolicy.MakeCooldownToast(9.4d).Contains("#ff5c5c"),
                "冷却文案正式名「换弹技能冷却中」+ 红色 + 剩余秒向上取整（主机为准，不叫自动压弹冷却）");
            check(ReloadSkillPolicy.UpgradeButtonLabel(1) == "升级到 1 级 · 花费 " + ReloadSkillPolicy.XpCostLevel0To1 + " 经验"
                && ReloadSkillPolicy.UpgradeButtonLabel(2) == "升级到 2 级 · 花费 " + ReloadSkillPolicy.XpCostLevel1To2 + " 经验",
                "升级按钮文案由票面花费常量单源组装");
            var insufficient = ReloadSkillPolicy.MakeUpgradeRejectedToast(ReloadSkillUpgradeReject.InsufficientExperience, 2);
            check(insufficient.Contains(ReloadSkillPolicy.XpCostLevel1To2.ToString()) && insufficient.Contains("#ff5c5c"),
                "经验不足回执引用花费常量且为红色");
            check(ReloadSkillPolicy.MakeUpgradeRejectedToast(ReloadSkillUpgradeReject.AlreadyMax, 2).Contains("满级"),
                "满级回执：已到最高等级，不引导三级");

            var options = ReloadSkillSettingsSurface.ChoiceOptions();
            check(options.Count == 3
                && options[0] == ReloadSkillSettingsSurface.OptionMaintain
                && options[1].Contains("1") && options[1].Contains(ReloadSkillPolicy.XpCostLevel0To1.ToString())
                && options[2].Contains("2") && options[2].Contains(ReloadSkillPolicy.XpCostLevel1To2.ToString()),
                "设置页（表面 B）档位 = 维持/请求升 1/请求升 2 三档，无三级档");
            check(!options[1].Contains("3") && !options[2].Contains("3"), "设置页档位文案不含「3」");

            // 冷却 toast 的正式名反证：文本不得写成「自动压弹冷却」（T7 Q3 名称裁决）。
            check(!ReloadSkillPolicy.MakeCooldownToast(1d).Contains("自动压弹"), "技能冷却正式名不叫自动压弹冷却");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组3 —— 升级主机校验：只升下一级、花费=票面常量、经验不足零扣零改、
        // 无三级目标、按玩家/角色持久化、新档不送等级、持久化失败回滚。
        // ─────────────────────────────────────────────────────────────────
        private static void V57GroupUpgrade(System.Action<bool, string> check)
        {
            var persistence = new V57Persistence();
            var store = new ReloadSkillStore();
            var runtime = new ReloadSkillRuntime(store, new V57Clock { Now = 0d }.Read, persistence);

            check(store.GetLevel(9UL, "New") == 0, "新档不送等级：默认 0 级");

            var d0 = runtime.TryAuthorizeUpgrade(9UL, "New", 1, (uint)ReloadSkillPolicy.XpCostLevel0To1);
            check(d0.Accepted && d0.NewLevel == 1 && d0.Cost == ReloadSkillPolicy.XpCostLevel0To1,
                "0→1 成交：花费 = 票面具名常量（升级扣原版经验，金额单源）");
            check(store.GetLevel(9UL, "New") == 0, "只校验不落库：authorize 不是写路径（校验→扣费→提交序的钉）");
            check(runtime.TryCommitUpgrade(9UL, "New", 1, out _), "提交成功（写存储+持久化）");
            check(store.GetLevel(9UL, "New") == 1, "提交后等级=1");
            check(persistence.SaveCalls == 1 && persistence.Records.Count == 1 && persistence.Records[0].Level == 1,
                "提交即持久化：一条记录、等级随写");

            var d1 = runtime.TryAuthorizeUpgrade(9UL, "New", 2, (uint)(ReloadSkillPolicy.XpCostLevel1To2 - 1));
            check(!d1.Accepted && d1.Reason == ReloadSkillUpgradeReject.InsufficientExperience
                && store.GetLevel(9UL, "New") == 1 && persistence.SaveCalls == 1,
                "经验差 1 不足 = 拒绝且零扣零改（askSpend 无余额门的插件侧自检，主机校验先于扣减）");

            var d2 = runtime.TryAuthorizeUpgrade(9UL, "New", 3, uint.MaxValue);
            check(!d2.Accepted && d2.Reason == ReloadSkillUpgradeReject.TargetBeyondMax,
                "目标 3 级 = 拒绝（没有三级，钱再多也不卖）");
            var d2b = runtime.TryAuthorizeUpgrade(9UL, "New", 1, uint.MaxValue);
            check(!d2b.Accepted && d2b.Reason == ReloadSkillUpgradeReject.LevelDrift && store.GetLevel(9UL, "New") == 1,
                "重复请求（1→1）不是「升下一级」= 漂移拒绝；满级另档（d4）");

            var d3 = runtime.TryAuthorizeUpgrade(9UL, "New", 2, uint.MaxValue);
            check(d3.Accepted && d3.Cost == ReloadSkillPolicy.XpCostLevel1To2, "1→2 成交：花费 = 票面具名 150");
            check(runtime.TryCommitUpgrade(9UL, "New", 2, out _), "1→2 提交");
            check(store.GetLevel(9UL, "New") == 2 && persistence.Records.Count == 1 && persistence.Records[0].Level == 2,
                "同人同角色覆盖单条记录（不追加双记录）");
            var d4 = runtime.TryAuthorizeUpgrade(9UL, "New", 1, uint.MaxValue);
            check(!d4.Accepted && d4.Reason == ReloadSkillUpgradeReject.AlreadyMax, "2 级再请求 1 = 已满级语义");

            // 跨级请求被拒（只升下一级）。
            var d5 = runtime.TryAuthorizeUpgrade(11UL, "Skip", 2, uint.MaxValue);
            check(!d5.Accepted && d5.Reason == ReloadSkillUpgradeReject.LevelDrift, "0→2 跳级 = 等级漂移拒绝（只升下一级）");

            // 角色隔离：同 steamId 不同角色各自记账。
            var d6 = runtime.TryAuthorizeUpgrade(9UL, "Bob", 1, uint.MaxValue);
            check(d6.Accepted && runtime.TryCommitUpgrade(9UL, "Bob", 1, out _), "9 号新角色 Bob 从 0 升 1");
            check(store.GetLevel(9UL, "Alice") == 0 && store.GetLevel(9UL, "New") == 2 && store.GetLevel(9UL, "Bob") == 1,
                "按玩家/角色持久化：(9,New)=2、(9,Bob)=1、(9,Alice)=0 互不串账");

            // 键归一：尾部空白折叠、null 视作空角色键。
            check(store.GetLevel(9UL, "New ") == 2 && store.GetLevel(9UL, null) == store.GetLevel(9UL, ""),
                "characterKey 归一（trim/空串统一），不发明第二键空间");

            // 持久化失败 = 提交整体回滚（内存等级不落账）。
            persistence.FailSave = true;
            check(!runtime.TryCommitUpgrade(9UL, "Bob", 2, out var saveErr) && store.GetLevel(9UL, "Bob") == 1 && saveErr != null,
                "落盘失败：回滚等级 + 显式错误（经验扣了但账没落=拒绝上报，主机以回执为准）");
            persistence.FailSave = false;

            // 代际重建：从持久化恢复 = 等级读写闭环。
            var store2 = new ReloadSkillStore();
            var rej = store2.Restore(persistence.Records, out var rejected);
            check(rejected == 0 && store2.GetLevel(9UL, "New") == 2 && store2.GetLevel(9UL, "Bob") == 1,
                "Start 期恢复：等级从自有文件重建（不写 *.bue-settings、不写原版 Skill[][]）");
            check(store2.Restore(new[] { new ReloadSkillRecord(0UL, "z", 1), new ReloadSkillRecord(5UL, "w", 3) }, out var rejected2) == 0
                && rejected2 == 2, "坏记录 fail-closed：steamId=0 与等级>Max 整条丢弃");
            _ = rej;
        }

        // ─────────────────────────────────────────────────────────────────
        // 组5 —— 官方先行消费：真实双击 R 与等级读写全部走更好的换弹体验。
        // 5a 双击驱动入现网输入器→权威入口→技能窗；5b 红剩余秒（本地 toast/
        // 远程 kind 6）；5c 升级请求→主机校验→回执（kind 3/4）；5d 等级确认
        // 同步（kind 7/5，客机只显示主机确认值）；5e binder 宿主可证；
        // 5f 表面 B 档位映射/复位与分区投影（无三级 UI）。
        // ─────────────────────────────────────────────────────────────────
        private static void V57GroupOfficialFirst(System.Action<bool, string> check)
        {
            // 5a. 真实双击：ReloadInputDriver（现网 0.3s 窗）喂进 OnDoubleTapReload，
            // 单击不动作、双击只触发一次，成交路径过技能窗。
            var clock = new V57Clock();
            var hooks = new V57Hooks { Level = 0 };
            var authority = new V57Authority { Outcome = LirRepackOutcome.NoChange, LocalId = 7UL };
            var net = new V57Network();
            var module = V57StartModule(hooks, authority, net, clock, new V57Persistence(), isServer: true, out var toasts, check);
            var presses = new System.Collections.Generic.Queue<bool>();
            var driver = new ReloadInputDriver(() => presses.Count > 0 && presses.Dequeue(), module.OnDoubleTapReload);
            presses.Enqueue(true); // 第一次按下：0.3s 窗锚定基线，只锚不触发
            driver.Tick(V57Tick(1UL, 0.1f));
            check(authority.RepackCalls == 0 && hooks.WindowChecks == 0, "单击 = 原版换弹直通，LIR 不触发");
            presses.Enqueue(true); presses.Enqueue(false); // 窗内第二下 → 触发一次；第三拍无键
            driver.Tick(V57Tick(2UL, 0.1f));
            check(authority.RepackCalls == 1 && hooks.WindowChecks == 1,
                "双击 = 换弹技能触发（现网入口保持，技能窗在事务前——官方先行消费）");
            driver.Tick(V57Tick(3UL, 0.1f));
            check(authority.RepackCalls == 1, "基线已清：连按不误触发（现网语义保持）");
            module.Stop(FeatureStopReason.PluginStopping);

            // 5b-本地. 窗内拒绝 = 红色剩余秒（本机直接 toast，不走线）。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 0, RemainingOnReject = 9.5d };
            authority = new V57Authority { Outcome = LirRepackOutcome.NoChange, LocalId = 7UL };
            net = new V57Network();
            module = V57StartModule(hooks, authority, net, clock, new V57Persistence(), isServer: true, out toasts, check);
            module.NetService.ExecuteRepackFor(7UL, 1100UL, isAuto: false);
            check(authority.RepackCalls == 0, "技能窗拒绝 = 不进事务（技术闸与事务都不消费）");
            check(toasts.Count == 1 && toasts[0].Contains("换弹技能冷却中") && toasts[0].Contains("还需 10 秒")
                && toasts[0].Contains("#ff5c5c"), "0 级冷却中再双击：红色剩余秒（余量向上取整，主机为准）");
            module.Stop(FeatureStopReason.PluginStopping);

            // 5b-远程. 客机请求：窗内拒绝 = 定向 kind 6 回执（剩余毫秒），零本地 toast。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 0, RemainingOnReject = 8.0d };
            authority = new V57Authority { Outcome = LirRepackOutcome.NoChange, LocalId = 7UL };
            net = new V57Network();
            net.LiveSessions.Add(new V57Session(5UL, 9UL));
            module = V57StartModule(hooks, authority, net, clock, new V57Persistence(), isServer: true, out toasts, check);
            module.OnHostTick(V57Tick(1UL, 0.1f)); // NetService.Tick 记会话
            clock.Advance(0.1d);
            net.DispatchInbound(ChannelDirection.FromClients, new V57Session(5UL, 9UL), LirRepackWireCodec.BuildRequest(1101UL));
            module.OnHostTick(V57Tick(2UL, 0.1f)); // Drain → ExecuteRepackFor(9UL,...)
            int notice = 0;
            for (var i = 0; i < net.Sent.Count; i++)
            {
                if (LirRepackWireCodec.TryReadSkillCooldownNotice(net.Sent[i].Payload, out var ms)
                    && net.Sent[i].Session != null && net.Sent[i].Session.PeerSteamId == 9UL && ms >= 7800 && ms <= 8000) notice++;
            }
            check(notice == 1 && toasts.Count == 0, "冷却回执只发请求者（定向、剩余毫秒=主机时钟口径），不串本地");
            check(authority.RepackCalls == 0, "远程窗内拒绝同样不进事务");
            module.Stop(FeatureStopReason.PluginStopping);

            // 5c. 升级请求（客机 kind 3 → 主机 hooks → kind 4 回执 → 客户端确认显示）。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 0, LocalResolvable = false };
            hooks.UpgradeDecision = new ReloadSkillUpgradeDecision { Accepted = true, NewLevel = 1, Cost = ReloadSkillPolicy.XpCostLevel0To1 };
            authority = new V57Authority { Outcome = LirRepackOutcome.NoChange };
            net = new V57Network();
            module = V57StartModule(hooks, authority, net, clock, new V57Persistence(), isServer: false, out toasts, check);
            module.OnHostTick(V57Tick(1UL, 0.1f)); // 首帧完成延迟网络初始化（现网语义）
            check(module.NetService.RequestUpgradeFromServer(1) == LirRepackRequestResult.Dispatched, "客机升级请求已发出");
            byte[] wire = null;
            for (var i = 0; i < net.Sent.Count; i++)
            {
                if (net.Sent[i].Payload != null && net.Sent[i].Payload.Length == 11 && net.Sent[i].Payload[1] == LirRepackWireCodec.MsgRequestUpgrade) wire = net.Sent[i].Payload;
            }
            check(wire != null && LirRepackWireCodec.TryReadUpgradeRequest(wire, out var upReqId, out var upTarget)
                && upTarget == 1, "kind 3 线格式往返（requestId+目标级）");
            var upSession = new V57Session(6UL, 9UL);
            net.LiveSessions.Add(upSession);
            module.OnHostTick(V57Tick(2UL, 0.1f));
            net.DispatchInbound(ChannelDirection.FromClients, upSession, wire);
            module.OnHostTick(V57Tick(3UL, 0.1f));
            check(hooks.Upgrades.Count == 1 && hooks.Upgrades[0].SteamId == 9UL && hooks.Upgrades[0].Target == 1,
                "主机侧校验经技能钩子（steamId 取自会话身份，非载荷自报）");
            byte[] reply = null;
            for (var i = 0; i < net.Sent.Count; i++)
            {
                if (LirRepackWireCodec.TryReadUpgradeResult(net.Sent[i].Payload, out _, out _, out _, out _)) reply = net.Sent[i].Payload;
            }
            check(reply != null, "升级回执 kind 4 已定向发回");
            net.DispatchInbound(ChannelDirection.FromServer, upSession, reply);
            module.OnHostTick(V57Tick(4UL, 0.1f));
            check(ReloadSkillLevelMirror.HasConfirmed && ReloadSkillLevelMirror.ConfirmedLevel == 1,
                "客户端只显示主机确认的等级（回执采纳前镜像不动）");
            check(toasts.Count == 1 && toasts[0].Contains(ReloadSkillPolicy.XpCostLevel0To1.ToString()),
                "升级成功 toast 引用票面花费常量");
            module.Stop(FeatureStopReason.PluginStopping);

            // 5c-拒. 经验不足：完整环回（客发 kind 3 → 主机校验拒 → kind 4 → 客应用）——
            // 镜像不动 + 红色花费引用 toast。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 0, LocalResolvable = false };
            hooks.UpgradeDecision = new ReloadSkillUpgradeDecision { Accepted = false, Reason = ReloadSkillUpgradeReject.InsufficientExperience };
            net = new V57Network();
            module = V57StartModule(hooks, new V57Authority(), net, clock, new V57Persistence(), isServer: false, out toasts, check);
            var s7 = new V57Session(7UL, 12UL);
            net.LiveSessions.Add(s7);
            module.OnHostTick(V57Tick(1UL, 0.1f));
            check(module.NetService.RequestUpgradeFromServer(2) == LirRepackRequestResult.Dispatched, "拒绝流装配：客请求已发");
            byte[] ask = null;
            for (var i = 0; i < net.Sent.Count; i++)
            {
                if (LirRepackWireCodec.TryReadUpgradeRequest(net.Sent[i].Payload, out var aid, out var atg) && atg == 2) ask = net.Sent[i].Payload;
            }
            check(ask != null, "拒绝流装配：捕获本侧 kind 3");
            net.DispatchInbound(ChannelDirection.FromClients, s7, ask);
            module.OnHostTick(V57Tick(2UL, 0.1f));
            byte[] reject = null;
            for (var i = 0; i < net.Sent.Count; i++)
            {
                if (LirRepackWireCodec.TryReadUpgradeResult(net.Sent[i].Payload, out var rj, out var ack, out var lv, out var rc)
                    && !ack && rc == (byte)ReloadSkillUpgradeReject.InsufficientExperience) reject = net.Sent[i].Payload;
            }
            check(reject != null && hooks.Upgrades.Count == 1, "拒绝回执带原 requestId 与原因码");
            ReloadSkillLevelMirror.Clear();
            net.DispatchInbound(ChannelDirection.FromServer, s7, reject);
            module.OnHostTick(V57Tick(3UL, 0.1f));
            check(!ReloadSkillLevelMirror.HasConfirmed, "拒绝回执不改等级镜像（只显示主机确认值）");
            check(toasts.Count >= 1 && toasts[toasts.Count - 1].Contains("经验不足")
                && toasts[toasts.Count - 1].Contains(ReloadSkillPolicy.XpCostLevel1To2.ToString()),
                "客侧拒绝 toast 引用票面花费（红色文案单源）");
            module.Stop(FeatureStopReason.PluginStopping);

            // 5d. 等级确认同步：会话建立后客机一次性 kind 7；主机 kind 5 回当前账。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 1 };
            net = new V57Network();
            module = V57StartModule(hooks, new V57Authority(), net, clock, new V57Persistence(), isServer: true, out _, check);
            var s8 = new V57Session(8UL, 21UL);
            net.LiveSessions.Add(s8);
            module.OnHostTick(V57Tick(1UL, 0.1f));
            module.NetService.RequestLevelStateFromServer();
            int asked = 0;
            for (var i = 0; i < net.Sent.Count; i++)
            {
                if (net.Sent[i].Payload != null && net.Sent[i].Payload.Length == 2
                    && net.Sent[i].Payload[1] == LirRepackWireCodec.MsgRequestLevelState) asked++;
            }
            check(asked == 1, "kind 7 等级求取请求发出");
            module.OnHostTick(V57Tick(2UL, 0.1f));
            net.DispatchInbound(ChannelDirection.FromClients, s8, new byte[] { LirRepackWireCodec.ProtocolVersion, LirRepackWireCodec.MsgRequestLevelState });
            module.OnHostTick(V57Tick(3UL, 0.1f));
            byte[] state = null;
            for (var i = 0; i < net.Sent.Count; i++)
            {
                if (LirRepackWireCodec.TryReadLevelState(net.Sent[i].Payload, out var lv)) state = net.Sent[i].Payload;
            }
            check(state != null && hooks.GetLevelForCalls >= 1, "主机按账回 kind 5（等级读=LIR 存储，不读原版 Skill[][]）");
            module.Stop(FeatureStopReason.PluginStopping);
            _ = clock;

            // 5e. 表面 A binder 宿主可证：恰一 updateSelection 命中（漂移即抛不静默绑错）。
            var target = ReloadSkillDashboardBinder.ResolveSectionHostTarget();
            check(target != null && target.DeclaringType == typeof(PlayerDashboardSkillsUI)
                && target.Name == "updateSelection" && ((MethodBase)target).GetParameters().Length == 1,
                "换弹技能分区绑定唯一事实源 = PlayerDashboardSkillsUI.updateSelection（显式解析，0/多义即抛）");
            int candidates = 0;
            foreach (var m in typeof(PlayerDashboardSkillsUI).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (m.Name == "updateSelection" && m.GetParameters().Length == 1) candidates++;
            }
            check(candidates == 1, "updateSelection 恰一命中（本票面无重载漂移）");
            check(ReloadSkillDashboardBinder.ProbeSurfaceA(), "注册期探测 = 接得上（真实程序集可证）");
            check(ReloadSkillDashboardBinder.PatchSurface.Count == 1 && ReloadSkillDashboardBinder.PatchSurface[0] == typeof(ReloadSkillDashboardPatch),
                "本票唯一触发面（无夹带面）");
            check(V57NoSwitchMembers(typeof(ReloadSkillDashboardPatch), check)
                && V57NoSwitchMembers(typeof(ReloadSkillDashboardAdapter), check),
                "补丁/适配面不自带功能 bool（登记=唯一开关；生命周期组钉卸载）");

            // 5f. 分区投影（表面 A）= 等级阶梯 0/1/2 + 仅下一级的升级按钮；满级无按钮；
            // 无三级 UI、无占位（任何行不引用 3 级/超限，行数恒不含空串占位）。
            var rows = ReloadSkillSectionModel.BuildRows(0, (uint)ReloadSkillPolicy.XpCostLevel0To1);
            check(rows.Count >= 4 && rows[0].Text == ReloadSkillPolicy.SkillSectionTitle, "分区首行=标题「换弹技能」");
            int ladder = 0, buttons = 0;
            foreach (var r in rows)
            {
                if (r.IsButton) buttons++;
                if (r.Text == ReloadSkillPolicy.LevelRowLabel(0) || r.Text == ReloadSkillPolicy.LevelRowLabel(1) || r.Text == ReloadSkillPolicy.LevelRowLabel(2)) ladder++;
                check(!string.IsNullOrEmpty(r.Text), "投影不画空占位");
                check(!r.Text.Contains("3 级") && !r.Text.Contains("超限"), "无三级 UI（阶梯只 0..2，超限不进本阶段）");
            }
            check(ladder == ReloadSkillPolicy.MaxSkillLevel + 1 && buttons == 1, "同一套等级（0/1/2 三行）+ 至多一颗下一级升级按钮");
            check(rows[rows.Count - 1].IsButton && rows[rows.Count - 1].Enabled &&
                rows[rows.Count - 1].Text == ReloadSkillPolicy.UpgradeButtonLabel(1),
                "0 级且经验刚好 = 升到 1 级按钮可用（余额显示门，扣费仍以主机为准）");
            var poor = ReloadSkillSectionModel.BuildRows(0, (uint)(ReloadSkillPolicy.XpCostLevel0To1 - 1));
            check(!poor[poor.Count - 1].Enabled, "经验不足：按钮画而不可用（不弹假承诺；权威复核在主机）");
            var maxed = ReloadSkillSectionModel.BuildRows(ReloadSkillPolicy.MaxSkillLevel, uint.MaxValue);
            check(maxed[maxed.Count - 1].Text.Contains("已满级") && !maxed[maxed.Count - 1].IsButton,
                "2 级 = 满级行，无按钮无占位（没有三级，不引导）");

            // 5g. 表面 B（设置页降级面）档位映射与复位。
            check(ReloadSkillSettingsSurface.MapOptionToTargetLevel(ReloadSkillSettingsSurface.OptionMaintain) == 0
                && ReloadSkillSettingsSurface.MapOptionToTargetLevel(ReloadSkillSettingsSurface.ChoiceOptions()[1]) == 1
                && ReloadSkillSettingsSurface.MapOptionToTargetLevel(ReloadSkillSettingsSurface.ChoiceOptions()[2]) == 2
                && ReloadSkillSettingsSurface.MapOptionToTargetLevel("乱写的档位") == 0,
                "档位→目标级映射单源；未识别档=0（不发请求）");
            var reset = ReloadSkillSettingsSurface.BuildResetRequest(new FeatureId(LirRuntime.FeatureIdValue), 77UL, 3U);
            check(reset != null && reset.Value.RequestId == 77UL && reset.Value.ExpectedRevision == 3U
                && reset.Value.Mutations.Count == 1
                && reset.Value.Mutations[0].Value.Text == ReloadSkillSettingsSurface.OptionMaintain,
                "请求发完即复位「维持」（行不是第二事实源，等级真相在回执/镜像）");
        }
        private static void V57GroupAutoRound(System.Action<bool, string> check)
        {
            // 4a. 成交→等待→到点再压一轮。
            var clock = new V57Clock();
            var hooks = new V57Hooks { Level = 2, Fingerprint = "gun-A", FreshFingerprint = "gun-A" };
            var authority = new V57Authority { Outcome = LirRepackOutcome.Committed, Total = 3, LocalId = 7UL };
            var net = new V57Network();
            var module = V57StartModule(hooks, authority, net, clock, new V57Persistence(), isServer: true, out var toasts, check);
            module.NetService.ExecuteRepackFor(7UL, 901UL, isAuto: false);
            check(authority.RepackCalls == 1, "手动双击成交走同一权威入口（官方先行）");
            check(module.AutoRounds.PendingCount == 1, "2 级手动成功 = 排一轮自动压弹");
            check(toasts.Count == 1 && toasts[0].Contains("成功压入"), "本地成功 toast 现网语义不变");
            check(hooks.ArmCalls == 1, "权威成交 >0 发才武装技能窗（0 发/闸拒不武装）");

            V57Pump(module, clock, 1UL, (float)(ReloadSkillPolicy.AutoRoundDelaySeconds - 0.5d), firstTick: 1UL);
            check(authority.RepackCalls == 1 && module.AutoRounds.PendingCount == 1,
                "等待未到（差 0.5s）：不提前压（固定等待不可设）");
            clock.Advance(1f);
            module.OnHostTick(V57Tick(2UL, 1f));
            check(authority.RepackCalls == 2 && module.AutoRounds.PendingCount == 0,
                "到点再压一轮，且只一轮（自动成功不再续排）");
            check(hooks.ArmCalls == 2, "自动轮成交同样武装（2 级 extra=0 本层仍调用，生产实现空操作）");
            check(authority.HostInitiatedCalls == 1, "自动轮走主机发起门（跳过客户端 requestId 回放比较）");
            module.Stop(FeatureStopReason.PluginStopping);

            // 4a-gate. U3DS 探针 #5：手动 id 很大之后，自动轮不得被回放闸拒绝。
            LirRepackGate.NowSecondsForTests = () => 1000f;
            LirRepackGate.ResetForGeneration();
            int retry;
            check(LirRepackGate.TryAcquire(77UL, 639252135186329770UL, out retry), "装配：先记一笔客机手动大 id");
            LirRepackGate.NowSecondsForTests = () => 1002f; // 过 1.5s 技术闸
            check(!LirRepackGate.TryAcquire(77UL, 100UL, out retry), "小于最高观察 id 的客户端请求仍拒（回放闸保持）");
            check(LirRepackGate.TryAcquireHostInitiated(77UL, out retry), "主机发起自动轮：跳过回放比较，冷却过后放行");
            LirRepackGate.NowSecondsForTests = () => 1002.1f;
            check(!LirRepackGate.TryAcquireHostInitiated(77UL, out retry), "主机发起仍吃 1.5s 技术闸");
            LirRepackGate.ResetForGeneration();
            LirRepackGate.NowSecondsForTests = null;

            // 4a-P2P. 自动轮回包 id=0：客机 pending 表无此 id，仍要 toast（探针 #3：
            // 主机成交 6 发但客机「未知 requestId」丢包）。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 2, Fingerprint = "gun-A", FreshFingerprint = "gun-A" };
            authority = new V57Authority { Outcome = LirRepackOutcome.Committed, Total = 6, LocalId = 1UL };
            net = new V57Network();
            net.LiveSessions.Add(new V57Session(5UL, 9UL));
            module = V57StartModule(hooks, authority, net, clock, new V57Persistence(), isServer: true, out _, check);
            module.OnHostTick(V57Tick(1UL, 0.1f));
            module.NetService.ExecuteRepackFor(9UL, 961UL, isAuto: false);
            clock.Advance(ReloadSkillPolicy.AutoRoundDelaySeconds + 0.1d);
            module.OnHostTick(V57Tick(2UL, 0.1f));
            var autoFrames = 0;
            ulong autoWireId = 1UL;
            var autoTotal = 0;
            for (var i = 0; i < net.Sent.Count; i++)
            {
                if (!LirRepackWireCodec.TryReadSuccess(net.Sent[i].Payload, out var rid, out var tot)) continue;
                if (rid == 0UL) { autoFrames++; autoWireId = rid; autoTotal = tot; }
            }
            check(autoFrames >= 1 && autoWireId == 0UL && autoTotal == 6,
                "自动轮回包 wireId=0（绕开客机 pending 表）且 total 随成交");
            var clientNet = new V57Network();
            var clientAuth = new V57Authority { LocalId = 9UL };
            var clientToasts = default(List<string>);
            var client = V57StartModule(new V57Hooks { Level = 2 }, clientAuth, clientNet, new V57Clock(), new V57Persistence(), isServer: false, out clientToasts, check);
            client.OnHostTick(V57Tick(1UL, 0.1f));
            clientNet.DispatchInbound(ChannelDirection.FromServer, new V57Session(5UL, 1UL), LirRepackWireCodec.BuildSuccess(0UL, 6));
            client.OnHostTick(V57Tick(2UL, 0.1f));
            var sawAutoToast = false;
            for (var i = 0; i < clientToasts.Count; i++)
                if (clientToasts[i].Contains("成功压入") && clientToasts[i].Contains("6")) sawAutoToast = true;
            check(sawAutoToast, "客机对 wireId=0 的自动轮回包弹成功 toast（不再当未知 requestId 丢掉）");
            client.Stop(FeatureStopReason.PluginStopping);
            module.Stop(FeatureStopReason.PluginStopping);

            // 4b. 到点条件不满足 = 取消，不强制、不重试。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 2, Fingerprint = "gun-A", FreshFingerprint = "gun-B" };
            authority = new V57Authority { Outcome = LirRepackOutcome.Committed, Total = 3, LocalId = 7UL };
            module = V57StartModule(hooks, authority, new V57Network(), clock, new V57Persistence(), isServer: true, out _, check);
            module.NetService.ExecuteRepackFor(7UL, 902UL, isAuto: false);
            clock.Advance(ReloadSkillPolicy.AutoRoundDelaySeconds + 0.1d);
            module.OnHostTick(V57Tick(1UL, 0.1f));
            check(authority.RepackCalls == 1 && module.AutoRounds.PendingCount == 0,
                "切枪（同枪同匣指纹重检失败）= 取消到点轮：零权威调用、条目消耗不重试");
            module.Stop(FeatureStopReason.PluginStopping);

            // 4c. 等级漂移重检（到点前被降/账变了——仍以存储为准）。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 2, Fingerprint = "fp", FreshFingerprint = "fp" };
            authority = new V57Authority { Outcome = LirRepackOutcome.Committed, Total = 3, LocalId = 7UL };
            module = V57StartModule(hooks, authority, new V57Network(), clock, new V57Persistence(), isServer: true, out _, check);
            module.NetService.ExecuteRepackFor(7UL, 903UL, isAuto: false);
            hooks.Level = 1;
            clock.Advance(ReloadSkillPolicy.AutoRoundDelaySeconds + 0.1d);
            module.OnHostTick(V57Tick(1UL, 0.1f));
            check(authority.RepackCalls == 1, "到点等级重检：已非 2 级不压（执行前重检=等级仍允许）");

            // 4d. 窗内手动再双击 = 替换为新一轮（始终至多一条目，手动吃现网闸）。
            hooks.Level = 2;
            module.NetService.ExecuteRepackFor(7UL, 904UL, isAuto: false);
            check(module.AutoRounds.PendingCount == 1 && hooks.WindowChecks == 2,
                "自动轮未到时再来手动：技能窗每次双击都过一遍，待压至多一轮");
            module.Stop(FeatureStopReason.PluginStopping);

            // 4e/4f. 0/1 级与无变化/缺玩家成功语义不排轮。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 1 };
            authority = new V57Authority { Outcome = LirRepackOutcome.Committed, Total = 3, LocalId = 7UL };
            module = V57StartModule(hooks, authority, new V57Network(), clock, new V57Persistence(), isServer: true, out _, check);
            module.NetService.ExecuteRepackFor(7UL, 905UL, isAuto: false);
            check(module.AutoRounds.PendingCount == 0, "1 级成交不排自动轮（自动压弹是 2 级专属）");
            module.NetService.ExecuteRepackFor(7UL, 906UL, isAuto: false);
            var armsBeforeZero = hooks.ArmCalls;
            authority.Outcome = LirRepackOutcome.Committed;
            authority.Total = 0;
            module.NetService.ExecuteRepackFor(7UL, 9061UL, isAuto: false);
            check(hooks.ArmCalls == armsBeforeZero, "Committed total=0 不武装技能窗（P2P 0 级「没压进却进 CD」的权威路径钉）");
            hooks.Level = 2;
            authority.Outcome = LirRepackOutcome.NoChange;
            module.NetService.ExecuteRepackFor(7UL, 907UL, isAuto: false);
            check(module.AutoRounds.PendingCount == 0, "NoChange 不算「双击成功」：不排轮");
            authority.Outcome = LirRepackOutcome.PlayerMissing;
            hooks.Fingerprint = "fp";
            module.NetService.ExecuteRepackFor(8UL, 908UL, isAuto: false);
            check(module.AutoRounds.PendingCount == 0, "PlayerMissing 不排轮");
            module.Stop(FeatureStopReason.PluginStopping);

            // 4g. Stop 即清（运行状态层不过代际；等级是进度层，另在生命周期组）。
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 2, Fingerprint = "fp", FreshFingerprint = "fp" };
            authority = new V57Authority { Outcome = LirRepackOutcome.Committed, Total = 3, LocalId = 7UL };
            module = V57StartModule(hooks, authority, new V57Network(), clock, new V57Persistence(), isServer: true, out _, check);
            module.NetService.ExecuteRepackFor(7UL, 909UL, isAuto: false);
            check(module.AutoRounds.PendingCount == 1, "装配：已排一轮");
            module.Stop(FeatureStopReason.PluginStopping);
            check(module.AutoRounds.PendingCount == 0, "Stop 清自动轮（静态表绑功能代际）");

            // 4h. 调度器本体：仅到点者触发、回调异常隔离不吞他人。
            var sched = new ReloadAutoRoundScheduler();
            var fired = new List<ulong>();
            sched.Schedule(1UL, "a", 10d);
            sched.Schedule(2UL, "b", 10d);
            sched.Tick(9d, (id, fp) => { fired.Add(id); return true; });
            check(fired.Count == 0 && sched.PendingCount == 2, "未到点不触发");
            sched.Tick(10d, (id, fp) => { throw new InvalidOperationException("synthetic fire fault"); });
            check(sched.PendingCount == 0, "触发异常仍消耗条目（一轮为限，无重试风暴）");
            sched.Schedule(3UL, "c", 0d);
            sched.Tick(1d, (id, fp) => { fired.Add(id); return true; });
            check(fired.Count == 1 && fired[0] == 3UL && sched.PendingCount == 0, "同拍后到的条目照常处理");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组2 —— 技能冷却三层：合并窗（技术闸+额外）、0 级可再击、1 级去
        // 额外段、拒绝不推进窗、玩家隔离、窗只由 B 双击成交开启。
        // ─────────────────────────────────────────────────────────────────
        private static void V57GroupCooldown(System.Action<bool, string> check)
        {
            var clock = new V57Clock { Now = 100d };
            var store = new ReloadSkillStore();
            var runtime = new ReloadSkillRuntime(store, clock.Read);

            // 2a. 0 级首双击仍可用（票面：0 级仍能双击 R，只是多一段冷却）。
            check(runtime.TryAdmitDoubleTap(7UL, "Alice", out var rem0), "0 级第一次双击成交放行（技能窗未开）");
            check(rem0 <= 0d, "放行时剩余秒非正（不伪造冷却）");

            // 2a'. DEV-V5-08 P2P：放行 ≠ 武装。0 发/闸拒不得开窗，再击仍放行。
            clock.Advance(1d);
            check(runtime.TryAdmitDoubleTap(7UL, "Alice", out var remIdle) && remIdle <= 0d,
                "未武装：再击仍放行（0 发成交不得进 9.5s CD）");

            // 2b. 权威成交 >0 发后才武装合并窗：再击拒绝、剩余秒如实。
            runtime.ArmWindowAfterCommit(7UL, "Alice");
            check(!runtime.TryAdmitDoubleTap(7UL, "Alice", out var rem1) && rem1 > 0d,
                "0 级窗内再双击 = 技能侧拒绝（带正剩余秒，主机为准）");
            check(Math.Abs(rem1 - (ReloadRuntimePolicy.CooldownSeconds + ReloadSkillPolicy.Level0ExtraCooldownSeconds)) < 0.001d,
                "剩余秒 = 刚武装的合并窗（技术闸 1.5 + 额外 8）——最终可用时间公式冻结");

            // 2c. 拒绝不推进窗（连点不罚更长）。
            var before = rem1;
            clock.Advance(2d);
            check(!runtime.TryAdmitDoubleTap(7UL, "Alice", out var rem2), "窗内再连点仍拒绝");
            check(rem2 < before - 1.99d && rem2 > before - 2.01d, "拒绝不刷新窗：剩余只随时间递减");

            // 2d. 窗尽再击 = 放行；须再次成交才开新窗。
            clock.Advance(rem2);
            check(runtime.TryAdmitDoubleTap(7UL, "Alice", out var rem3), "0 级冷却窗结束后双击恢复可用");
            check(rem3 <= 0d, "新窗放行时剩余非正");
            check(runtime.TryAdmitDoubleTap(7UL, "Alice", out var rem3b) && rem3b <= 0d,
                "窗尽后未再成交：仍不武装（对称 2a'）");

            // 2e. 1 级额外冷却消失：升到 1 级后窗秒秒可击（技术闸另在闸门，不在本层）。
            check(store.TrySetLevel(7UL, "Alice", 1), "测试装配：直接写 1 级（升级路径另组）");
            var clock2 = new V57Clock { Now = 0d };
            var runtime2 = new ReloadSkillRuntime(store, clock2.Read);
            check(runtime2.TryAdmitDoubleTap(7UL, "Alice", out _), "1 级首次成交");
            clock2.Advance(0.05d);
            check(runtime2.TryAdmitDoubleTap(7UL, "Alice", out var remQ),
                "1 级取消额外技能冷却：技能层连击不拒绝（技术闸 LirRepackGate 仍在，另一层）");
            check(remQ <= 0d, "1 级技能窗永不武装（额外段为 0）");
            clock2.Advance(ReloadRuntimePolicy.CooldownSeconds + ReloadSkillPolicy.Level0ExtraCooldownSeconds);
            check(runtime2.TryAdmitDoubleTap(7UL, "Alice", out _), "1 级长时间后同样放行（无残留窗）");

            // 2f. 玩家隔离：7 号已升 1 级（2e 之后额外段=0，本层永不拒——这是
            // 正面断言）；41 号 0 级新键开新窗后在窗中；8 号首击放行不连坐。
            check(runtime.TryAdmitDoubleTap(7UL, "Alice", out _), "1 级玩家即使在 0 级旧窗时段也不拒（额外段取消）");
            check(runtime.TryAdmitDoubleTap(41UL, "Zoe", out _), "41 号 0 级首击放行");
            runtime.ArmWindowAfterCommit(41UL, "Zoe");
            check(!runtime.TryAdmitDoubleTap(41UL, "Zoe", out _), "41 号成交武装后窗内再击拒绝");
            check(runtime.TryAdmitDoubleTap(8UL, "Bob", out _), "冷却窗按玩家隔离：他号不受连坐");

            // 2g. 功能 A 合匣不吃技能窗（窗只由 B 双击成交开启）。
            var hooks = new V57Hooks { Level = 2, Fingerprint = "fp" };
            var authority = new V57Authority();
            var module = V57StartModule(hooks, authority, new V57Network(), new V57Clock(), new V57Persistence(), isServer: true, out _, check);
            authority.LocalId = 7UL;
            var preWindow = hooks.WindowChecks;
            module.ExecuteTidyMerge(7UL, 1UL, 42UL);
            check(hooks.WindowChecks == preWindow && module.AutoRounds.PendingCount == 0,
                "功能 A（整理后合匣）不读不写技能窗、不排自动轮——与二级不是一条");
            module.Stop(FeatureStopReason.PluginStopping);
        }

        // ─────────────────────────────────────────────────────────────────
        // 组6 —— 生命周期：登记=唯一开关（Start 登记/Stop 注销）、三面→四面
        // 同代共存互撤归零、U3DS headless 只砍画面面（技能权威/升级/自动压弹
        // 照常）、运行状态不过代际（冷却窗+自动轮 Stop 即清，等级账持久）。
        // ─────────────────────────────────────────────────────────────────
        private static void V57GroupLifecycle(System.Action<bool, string> check)
        {
            // 6a. Start 登记=唯一开关；headless 只砍画面面。
            var clock = new V57Clock();
            var hooks = new V57Hooks { Level = 2, Fingerprint = "fp", FreshFingerprint = "fp" };
            var authority = new V57Authority { Outcome = LirRepackOutcome.Committed, Total = 3, LocalId = 7UL };
            var net = new V57Network();
            var module = V57StartModule(hooks, authority, net, clock, new V57Persistence(), isServer: true, out _, check);
            check(module.SkillPatchInstalled, "Start 登记：技能分区面已武装（登记=唯一开关）");
            module.NetService.ExecuteRepackFor(7UL, 1300UL, isAuto: false);
            check(module.AutoRounds.PendingCount == 1, "非 headless：2 级成功排轮");
            module.Stop(FeatureStopReason.PluginStopping);
            check(!module.SkillPatchInstalled, "Stop 注销：分区面撤回（原生回退）");

            BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision = true;
            var persistence = new V57Persistence();
            module = V57StartModule(hooks, authority, new V57Network(), clock, persistence, isServer: true, out _, check);
            check(!module.SkillPatchInstalled && module.SkillStartGateDiagnostics.Contains("headless-not-armed"),
                "U3DS headless：分区面不武装（画面裁决），诊断如实");
            authority.RepackCalls = 0;
            hooks.Level = 2;
            module.NetService.ExecuteRepackFor(7UL, 1301UL, isAuto: false);
            check(authority.RepackCalls == 1 && module.AutoRounds.PendingCount == 1,
                "headless 技能权威照常：等级门/自动压弹/升级账不受画面裁决牵连");
            module.Stop(FeatureStopReason.PluginStopping);
            check(persistence.SaveCalls == 0, "Stop 不写持久化（账只在提交时落——生命周期不发明第二写点）");
            BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision = false;

            // 6b. 四面互撤：技能面拒装 = 全零；HUD 面拒装 = 技能面也归零（共存）。
            module = V57StartModule(hooks, authority, new V57Network(), clock, new V57Persistence(), isServer: true, out _, check,
                installSkillSurface: false);
            check(!module.SkillPatchInstalled && !module.PatchesInstalled && !module.HudPatchInstalled
                    && module.StartGateDiagnostics.StartsWith("patch-install-failed"),
                "技能面拒装 = 四面互撤归零（04/05/06 同律，禁半装）");
            module.Stop(FeatureStopReason.PluginStopping);
            module = V57StartModule(hooks, authority, new V57Network(), clock, new V57Persistence(), isServer: true, out _, check,
                installSkillSurface: true, installHudSurface: false);
            check(!module.SkillPatchInstalled && !module.PatchesInstalled && !module.HudPatchInstalled,
                "HUD 面拒装同样连坐技能面（四面同代际共存）");
            module.Stop(FeatureStopReason.PluginStopping);

            // 6c. 运行状态不过代际：窗与轮 Stop 即清，重建模块同账恢复等级。
            clock = new V57Clock();
            var persistence2 = new V57Persistence { Records = new List<ReloadSkillRecord>() };
            var runtimeA = new ReloadSkillRuntime(new ReloadSkillStore(), clock.Read, persistence2);
            check(runtimeA.TryCommitUpgrade(31UL, "G", 1, out _), "装配：落一笔 1 级账");
            hooks = new V57Hooks { Level = 0 };
            module = V57StartModule(hooks, authority, new V57Network(), clock, persistence2, isServer: true, out _, check);
            check(module.SkillRuntime.Store.GetLevel(31UL, "G") == 1, "新代际 Start：等级账从自有文件恢复");
            check(module.AutoRounds.PendingCount == 0, "新代际自动轮表为空（旧代条目不过代际）");
            module.SkillRuntime.TryAdmitDoubleTap(41UL, "Z", out _);
            module.SkillRuntime.ArmWindowAfterCommit(41UL, "Z");
            check(!module.SkillRuntime.TryAdmitDoubleTap(41UL, "Z", out _), "装配：本代 0 级窗已开（成交后武装）");
            module.Stop(FeatureStopReason.PluginStopping);
            var module2 = V57StartModule(hooks, authority, new V57Network(), clock, persistence2, isServer: true, out _, check);
            check(module2.SkillRuntime.TryAdmitDoubleTap(41UL, "Z", out _), "技能冷却窗不过代际：新模块首击放行");
            module2.Stop(FeatureStopReason.PluginStopping);

            // 6d. 表面 B 应用钩子：档位→升级请求→回执→行复位（面板不是第二事实源）。
            var viewB = new V57SettingsViewB { SelectedOption = ReloadSkillSettingsSurface.ChoiceOptions()[2] };
            clock = new V57Clock();
            hooks = new V57Hooks { Level = 1 };
            hooks.UpgradeDecision = new ReloadSkillUpgradeDecision { Accepted = true, NewLevel = 2, Cost = ReloadSkillPolicy.XpCostLevel1To2 };
            authority = new V57Authority { Outcome = LirRepackOutcome.NoChange, LocalId = 7UL };
            module = V57StartModule(hooks, authority, new V57Network(), clock, new V57Persistence(), isServer: true, out var toastsB, check);
            module.AttachSettingsView(viewB);
            module.HandleSkillSettingsApplied();
            check(hooks.Upgrades.Count == 1 && hooks.Upgrades[0].SteamId == 7UL && hooks.Upgrades[0].Target == 2,
                "设置页请求经主机校验路径（与 U 菜单同一权威门）");
            check(viewB.SubmitCalls == 1 && viewB.LastSubmitMutations != null
                && viewB.LastSubmitMutations[0].Value.Text == ReloadSkillSettingsSurface.OptionMaintain,
                "请求后行复位「维持」");
            check(toastsB.Count == 1 && toastsB[0].Contains(ReloadSkillPolicy.XpCostLevel1To2.ToString()), "成功 toast 单源文案");
            viewB.SelectedOption = ReloadSkillSettingsSurface.OptionMaintain;
            module.HandleSkillSettingsApplied();
            check(hooks.Upgrades.Count == 1, "维持档 = 零请求（不误扣）");
            module.Stop(FeatureStopReason.PluginStopping);
            module.HandleSkillSettingsApplied();
            check(hooks.Upgrades.Count == 1 && viewB.SubmitCalls == 1, "停用后钩子即闭（Stop 后不响应设置写）");

            // 5h. 无确认等级不画分区（客机只显示主机确认的等级——未确认=不猜）。
            ReloadSkillLevelMirror.Clear();
            check(!ReloadSkillLevelMirror.HasConfirmed && !ReloadSkillDashboardAdapter.ShouldRenderSection(),
                "等级未经主机确认 = 分区不画（不猜默认级）");
        }

        /// <summary>06 同律反射钉：类型不得自带 Enabled/Disabled/在册语义成员。</summary>
        private static bool V57NoSwitchMembers(Type type, System.Action<bool, string> check)
        {
            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var m in members)
            {
                if (m.MemberType != MemberTypes.Field && m.MemberType != MemberTypes.Property) continue;
                var name = m.Name;
                if (name == null) continue;
                if (name.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Armed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    check(false, "技能面类型含自身开关成员：" + type.Name + "." + name);
                    return false;
                }
                if (name == "Patched" || name == "Installed")
                {
                    check(false, "技能面自持装态成员（在册位归模块）：" + type.Name + "." + name);
                    return false;
                }
            }
            return true;
        }

        /// <summary>表面 B 用设置视图假件：读 enabled+升级档、Submit 记录复位请求。</summary>
        private sealed class V57SettingsViewB : IScopedFeatureSettings
        {
            public string SelectedOption = ReloadSkillSettingsSurface.OptionMaintain;
            public int SubmitCalls;
            public IReadOnlyList<SettingMutation> LastSubmitMutations;

            public FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)
            {
                return new FeatureSettingsSnapshot(new FeatureId(LirRuntime.FeatureIdValue), 1U,
                    revisionScope, 3U, SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new List<SettingEntryView>());
            }

            public bool TryGet(string settingId, out SettingValue value, out uint revision)
            {
                revision = 3U;
                if (settingId == "inplacereload.enabled") { value = SettingValue.Toggle(true); return true; }
                if (settingId == ReloadSkillPolicy.UpgradeSettingId) { value = SettingValue.Choice(SelectedOption); return true; }
                value = default(SettingValue);
                return false;
            }

            public SettingChangeResult Submit(ScopedSettingChangeRequest request)
            {
                SubmitCalls++;
                LastSubmitMutations = request.Mutations;
                SelectedOption = request.Mutations[0].Value.Text;
                return new SettingChangeResult(true, FrameworkErrorCode.None, 4U, GetSnapshot(request.RevisionScope));
            }
        }
    }
}
