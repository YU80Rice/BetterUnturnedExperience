using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lir;
using Mono.Cecil;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V7-04 red-first coverage: scoped skill progress and the vanilla-row
    /// projection must be independent from the passive scheduler and UI runtime.
    /// </summary>
    internal static class DevV704SkillRowScopeTests
    {
        internal static void Run(bool collectAllFailures = false)
        {
            var failures = new List<string>();
            void Check(bool condition, string message)
            {
                if (condition) return;
                if (collectAllFailures) failures.Add(message);
                else throw new InvalidOperationException(message);
            }

            Group("作用域与旧账隔离", failures, collectAllFailures, () => ScopeAndLegacy(Check));
            Group("原版单行投影", failures, collectAllFailures, () => VanillaRow(Check));
            Group("原版技能行视觉结构", failures, collectAllFailures, () => VisualLayout(Check));
            Group("客户端座位镜像确认", failures, collectAllFailures, () => ClientSeatMirror(Check));
            Group("表面隔离与依赖边界", failures, collectAllFailures, () => SurfaceBoundary(Check));

            Console.WriteLine("DEV-V7-04 skill-row-scope tests: "
                + (failures.Count == 0 ? "PASS" : "FAIL"));
            if (failures.Count != 0)
                throw new InvalidOperationException("DEV-V7-04 failures ("
                    + failures.Count + "): " + string.Join(" || ", failures));
        }

        private static void Group(string name, List<string> failures, bool collect,
            Action body)
        {
            try { body(); }
            catch (Exception error) when (collect)
            {
                failures.Add("[" + name + "] " + error.Message);
            }
        }

        private static void ScopeAndLegacy(Action<bool, string> check)
        {
            var firstMap = new ReloadSkillScopeKey("TestServer", 76561197960265729UL, 0, "PEI");
            var secondMap = new ReloadSkillScopeKey("TestServer", 76561197960265729UL, 0, "Washington");
            var secondSlot = new ReloadSkillScopeKey("TestServer", 76561197960265729UL, 1, "PEI");
            var otherPlayer = new ReloadSkillScopeKey("TestServer", 76561197960265730UL, 0, "PEI");
            var store = new ReloadSkillStore();

            check(store.TrySetLevel(firstMap, 2), "作用域账应允许写入 2 级");
            check(store.GetLevel(firstMap) == 2, "同一世界/槽/玩家应读回等级");
            check(store.GetLevel(secondMap) == 0, "换地图必须从 0 级开始");
            check(store.GetLevel(secondSlot) == 0, "换角色槽不得串账");
            check(store.GetLevel(otherPlayer) == 0, "同服不同玩家不得串账");

            check(typeof(ReloadSkillFilePersistence).GetField("FileName",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) == null,
                "生产持久化不得继续暴露旧全局技能文件");
        }

        private static void VanillaRow(Action<bool, string> check)
        {
            var rows = ReloadSkillSectionModel.BuildRows(0,
                (uint)ReloadSkillPolicy.XpCostLevel0To1);
            check(rows.Count == 1, "技能投影必须是一条完整原版风格行");
            var row = rows[0];
            check(row.Name == ReloadSkillPolicy.SkillSectionTitle,
                "单行必须有换弹技能名称");
            check(row.LevelText.Contains("0") && row.LevelText.Contains("2"),
                "单行必须同时表达当前等级与满级");
            check(row.DescriptionText == ReloadSkillPolicy.LevelDescription(0),
                "0 级描述必须来自单一策略源");
            check(row.CostText.Contains("125"), "0 级单行必须显示 125 经验花费");
            check(row.IsClickable && row.TargetLevel == 1,
                "经验足够时整行必须可点击升级");
            check(row.LockCount == 3 && row.UnlockedCount == 0,
                "单行必须有三格锁条");
            check(row.Height == 80 && row.Step == 90,
                "单行必须使用原版约 80 高/90 步进");

            var poor = ReloadSkillSectionModel.BuildRows(0,
                (uint)ReloadSkillPolicy.XpCostLevel0To1 - 1);
            check(!poor[0].IsClickable, "经验不足时整行必须不可点击");
            var full = ReloadSkillSectionModel.BuildRows(ReloadSkillPolicy.MaxSkillLevel,
                uint.MaxValue)[0];
            check(full.IsFull && full.CostText == "Full" && full.TargetLevel == 0,
                "2 级必须显示 Full 且不得产生三级目标");
            check(!ReloadSkillPolicy.LevelDescription(2).Contains("再等一轮"),
                "2 级描述不得保留旧的再等一轮语义");
        }

        private static void VisualLayout(Action<bool, string> check)
        {
            var layoutType = typeof(ReloadSkillDashboardSurface).Assembly.GetType(
                "BetterUnturnedExperience.Lir.ReloadSkillDashboardLayout");
            check(layoutType != null, "战斗区必须暴露可测试且由真实 Render 消费的布局 seam");
            if (layoutType == null) return;
            var create = layoutType.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
            check(create != null, "布局 seam 必须能按原版行数生成布局");
            if (create == null) return;
            var rows = create.Invoke(null, new object[] { 7, 1 }) as System.Collections.IEnumerable;
            check(rows != null, "布局 seam 必须返回行记录列表");
            if (rows == null) return;
            var twoRows = create.Invoke(null, new object[] { 7, 2 }) as System.Collections.IEnumerable;
            check(twoRows != null, "布局 seam 必须支持多行原版步进");
            if (twoRows != null)
            {
                var second = twoRows.GetEnumerator();
                second.MoveNext();
                second.MoveNext();
                var secondLayout = second.Current;
                object ReadSecond(string property)
                {
                    return secondLayout.GetType().GetProperty(property,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(secondLayout);
                }
                check(Convert.ToInt32(ReadSecond("PositionY")) == 720,
                    "第二行必须按原版 90px 步进，不得重复或插入顶隙");
                check(Convert.ToInt32(ReadSecond("ContentHeightAfterRender")) == 800
                    && Convert.ToInt32(ReadSecond("ContentHeightAfterClear")) == 620,
                    "多行追加后的内容高度必须仍按原版网格计算");
            }

            var row = rows.GetEnumerator();
            if (!row.MoveNext())
            {
                check(false, "新增技能布局必须产生一行");
                return;
            }
            var rowLayout = row.Current;
            object Read(string property)
            {
                return rowLayout.GetType().GetProperty(property,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(rowLayout);
            }
            check(Convert.ToInt32(Read("PositionY")) == 630,
                "技能行必须紧接原版第 7 行，不能额外增加顶隙");
            check(Convert.ToInt32(Read("Height")) == 80 && Convert.ToSingle(Read("WidthScale")) == 1f,
                "行根必须按原版 80 高并横向铺满");
            check(Convert.ToSingle(Read("ButtonWidthScale")) == 1f
                && Convert.ToSingle(Read("ButtonHeightScale")) == 1f,
                "整行按钮必须覆盖 80 高行根");
            check(string.Equals(Read("NameAlignment")?.ToString(), "UpperLeft", StringComparison.Ordinal)
                && string.Equals(Read("DescriptionAlignment")?.ToString(), "LowerLeft", StringComparison.Ordinal)
                && string.Equals(Read("CostAlignment")?.ToString(), "LowerRight", StringComparison.Ordinal),
                "三类文本必须使用原版左上、左下、右下对齐");
            check(Convert.ToBoolean(Read("ChildrenUseRowRoot")),
                "文本与锁条必须挂在各自的 80 高行根，不得挂在额外分区盒");
            check(Convert.ToInt32(Read("LockParentHeight")) == 80
                && Convert.ToInt32(Read("FirstLockX")) == -20
                && Convert.ToInt32(Read("LockY")) == 10
                && Convert.ToSingle(Read("LockHeightScale")) == 0.5f,
                "锁条必须相对 80 高行根使用原版右侧半高定位");
            check(Convert.ToInt32(Read("ContentHeightAfterRender")) == 710
                && Convert.ToInt32(Read("ContentHeightAfterClear")) == 620,
                "追加和清除技能行必须恢复原版滚动内容高度公式");
        }

        private static void SurfaceBoundary(Action<bool, string> check)
        {
            check(typeof(ReloadSkillDashboardAdapter).GetMethod("RenderReal",
                BindingFlags.Static | BindingFlags.NonPublic) != null,
                "战斗区必须保留真实呈现适配器");
            check(typeof(ReloadSkillDashboardSurface).GetMethod("RequestUpgrade",
                BindingFlags.Static | BindingFlags.NonPublic) != null,
                "战斗区整行点击必须共用主机升级入口");
            check(typeof(ReloadSkillSettingsSurface).GetMethod("BuildFallbackProjection",
                BindingFlags.Static | BindingFlags.NonPublic) != null,
                "设置页 fallback 必须消费同一套单行投影");
            var dynamicDescriptors = ReloadSkillSettingsSurface.CreateDynamicDescriptors(
                new FeatureId(LirRuntime.FeatureIdValue));
            ReloadSkillSettingsSurface.CurrentLevelProvider = null;
            ReloadSkillSettingsSurface.CurrentExperienceProvider = null;
            try
            {
                var beforeStart = dynamicDescriptors[0];
                check(beforeStart.DescriptionKey.Contains("等级 0/2"),
                    "惰性设置描述符的注册前快照应从 0 级起步");
                ReloadSkillSettingsSurface.CurrentLevelProvider = () => 1;
                ReloadSkillSettingsSurface.CurrentExperienceProvider = () => uint.MaxValue;
                var afterStart = dynamicDescriptors[0];
                check(afterStart.DescriptionKey.Contains("等级 1/2")
                    && afterStart.AllowedValues.Count == 2
                    && afterStart.AllowedValues[1].Text.Contains("升级到 2"),
                    "模块 Start 绑定 provider 后，已注册描述符必须读取最新 projection");
            }
            finally
            {
                ReloadSkillSettingsSurface.CurrentLevelProvider = null;
                ReloadSkillSettingsSurface.CurrentExperienceProvider = null;
            }

            foreach (var field in typeof(ReloadSkillDashboardSurface).GetFields(
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                check(field.FieldType != typeof(ReloadAutoRoundScheduler),
                    "技能 UI 不得持有被动调度器：" + field.Name);
            }
            var savedHeadless = LirRuntime.HostHeadlessDecision;
            try
            {
                LirRuntime.HostHeadlessDecision = () => true;
                var headlessRegistration = LirFeatureAssembly.CreateRegistration();
                check(!(headlessRegistration is IFeatureSettingsRegistration),
                    "U3DS headless 注册不得转为设置页 fallback");
            }
            finally
            {
                LirRuntime.HostHeadlessDecision = savedHeadless;
            }

            ReloadSkillSettingsSurface.CurrentLevelProvider = () => 1;
            ReloadSkillSettingsSurface.CurrentExperienceProvider = () => uint.MaxValue;
            try
            {
                var descriptor = ReloadSkillSettingsSurface.CreateDescriptors(
                    new FeatureId(LirRuntime.FeatureIdValue))[0];
                check(descriptor.DescriptionKey.Contains("等级 1/2")
                    && descriptor.AllowedValues.Count == 2
                    && descriptor.AllowedValues[1].Text.Contains("升级到 2"),
                    "设置页必须按当前等级投影同一单行并只暴露下一等级入口");
                ReloadSkillSettingsSurface.CurrentExperienceProvider = () => 0u;
                var poor = ReloadSkillSettingsSurface.CreateDescriptors(
                    new FeatureId(LirRuntime.FeatureIdValue))[0];
                check(poor.AllowedValues.Count == 1,
                    "设置页经验不足时不得暴露可升级选项");
            }
            finally
            {
                ReloadSkillSettingsSurface.CurrentLevelProvider = null;
                ReloadSkillSettingsSurface.CurrentExperienceProvider = null;
            }
            check(typeof(ReloadSkillStore).GetMethod("TrySetLevel",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(ReloadSkillScopeKey), typeof(byte) }, null) != null,
                "技能账必须可脱离 UI/被动调度器独立写入");

        }

        // ── 客户端座位镜像确认（U3DS 实机缺陷 2026-10-01：服务端落账成功、
        // 客户端镜像永不确认——TryAcceptSkillLevelConfirmation 依赖本地 scope
        // 解析，而 Provider.clients 在客户端 seat 不含本地玩家）────────────

        private sealed class V74Hooks : ILirSkillHooks, IReloadSkillScopeHooks
        {
            public byte Level;
            public bool LocalScopeResolvable;
            public ReloadSkillScopeKey LocalScope;
            public bool LocalLevelResolvable = true;

            public bool TryBeginRepackWindow(ulong steamId, out double remainingSeconds)
            { remainingSeconds = 0d; return true; }

            public void ArmRepackWindowAfterCommit(ulong steamId) { }

            public readonly List<(ulong SteamId, byte Target)> Upgrades =
                new List<(ulong SteamId, byte Target)>();
            public readonly Queue<ReloadSkillUpgradeDecision> UpgradeDecisions =
                new Queue<ReloadSkillUpgradeDecision>();

            // 真实账本模式（Round 3）：ExecuteUpgrade 走生产 ReloadSkillRuntime 的
            // 校验→扣经验→落账（失败退款）编排，只是把引擎 askSpend 换成账面减法。
            public ReloadSkillRuntime Runtime = null;
            public uint Experience;

            public ReloadSkillUpgradeDecision ExecuteUpgrade(ulong steamId, byte targetLevel)
            {
                Upgrades.Add((steamId, targetLevel));
                var runtime = Runtime;
                if (runtime == null)
                {
                    return UpgradeDecisions.Count > 0 ? UpgradeDecisions.Dequeue()
                        : new ReloadSkillUpgradeDecision { Accepted = false, Reason = ReloadSkillUpgradeReject.LevelDrift };
                }
                var decision = runtime.TryAuthorizeUpgrade(PeerScope, targetLevel, Experience);
                if (!decision.Accepted) return decision;
                Experience -= (uint)decision.Cost; // 扣经验（askSpend 的账面等价）
                if (!runtime.TryCommitUpgrade(PeerScope, decision.NewLevel, out var commitError))
                {
                    Experience += (uint)decision.Cost; // 落账失败=原额退回（不丢经验）
                    return new ReloadSkillUpgradeDecision { Accepted = false, Reason = ReloadSkillUpgradeReject.LevelDrift };
                }
                Level = decision.NewLevel; // 落账成功
                return decision;
            }

            public byte GetLevelFor(ulong steamId) { return Level; }

            public bool IsPlayerAvailable(ulong steamId) { return true; }

            public bool TryResolveLocalLevel(out byte level)
            { level = Level; return LocalLevelResolvable; }

            public bool TryResolveLocalScope(out ReloadSkillScopeKey scope)
            {
                scope = LocalScopeResolvable ? LocalScope : default(ReloadSkillScopeKey);
                return LocalScopeResolvable;
            }

            public bool TryResolveScopeOf(ulong steamId, out ReloadSkillScopeKey scope)
            {
                scope = PeerScopeResolvable ? PeerScope : default(ReloadSkillScopeKey);
                return PeerScopeResolvable;
            }

            public bool PeerScopeResolvable = false;
            public ReloadSkillScopeKey PeerScope = default(ReloadSkillScopeKey);
        }

        private static InPlaceReloadModule NewV74Module(ILirSkillHooks hooks)
        {
            var module = new InPlaceReloadModule(new FeatureId(LirRuntime.FeatureIdValue));
            typeof(InPlaceReloadModule).GetProperty("SkillHooks",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(module, hooks);
            return module;
        }

        private static void ClientSeatMirror(Action<bool, string> check)
        {
            var serverScope = new ReloadSkillScopeKey("U3dsHost", 76561199030780228UL, 0, "PEI");

            // 线格式：服务端权威 scope 随 kind 4 / kind 5 下行（往返 + 截断拒收）。
            const BindingFlags CodecFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var buildResult = typeof(LirRepackWireCodec).GetMethod("BuildUpgradeResult", CodecFlags,
                null, new[] { typeof(ulong), typeof(bool), typeof(byte), typeof(byte),
                        typeof(bool), typeof(ReloadSkillScopeKey) }, null);
            check(buildResult != null, "升级回执构建必须携带服务端权威 scope（hasScope+scope）");
            if (buildResult != null)
            {
                var readResult = typeof(LirRepackWireCodec).GetMethod("TryReadUpgradeResult", CodecFlags,
                    null, new[] { typeof(byte[]), typeof(ulong).MakeByRefType(), typeof(bool).MakeByRefType(),
                            typeof(byte).MakeByRefType(), typeof(byte).MakeByRefType(),
                            typeof(bool).MakeByRefType(), typeof(ReloadSkillScopeKey).MakeByRefType() }, null);
                check(readResult != null, "升级回执解析必须读回服务端权威 scope");
                if (readResult != null)
                {
                    var payload = (byte[])buildResult.Invoke(null,
                        new object[] { 7UL, true, (byte)1, (byte)0, true, serverScope });
                    var args = new object[] { payload, 0UL, false, (byte)0, (byte)0, false,
                        default(ReloadSkillScopeKey) };
                    check((bool)readResult.Invoke(null, args), "带 scope 的 kind 4 帧必须解析成功");
                    check((ulong)args[1] == 7UL && (bool)args[2] && (byte)args[3] == 1
                        && (bool)args[5] && ((ReloadSkillScopeKey)args[6]).Equals(serverScope),
                        "kind 4 往返必须原样读回 requestId/accepted/newLevel/权威 scope");

                    // 写侧不可编码身份=诚实降级为无 scope 帧（Round 3 blocker 钉）。
                    var unencodable = new ReloadSkillScopeKey("\uD800", 76561199030780228UL, 0, "PEI");
                    var degraded = (byte[])buildResult.Invoke(null,
                        new object[] { 9UL, true, (byte)1, (byte)0, true, unencodable });
                    check(degraded.Length == 13, "不可编码身份必须降级为 13 字节无 scope 帧");
                    var degradedArgs = new object[] { degraded, 0UL, false, (byte)0, (byte)0, false,
                        default(ReloadSkillScopeKey) };
                    check((bool)readResult.Invoke(null, degradedArgs) && (ulong)degradedArgs[1] == 9UL
                        && !(bool)degradedArgs[5],
                        "降级帧必须解析成功且 hasScope=false（正常发送，不发明截断身份）");
                }
                var buildState = typeof(LirRepackWireCodec).GetMethod("BuildLevelState", CodecFlags,
                    null, new[] { typeof(byte), typeof(bool), typeof(ReloadSkillScopeKey) }, null);
                var readState = typeof(LirRepackWireCodec).GetMethod("TryReadLevelState", CodecFlags,
                    null, new[] { typeof(byte[]), typeof(byte).MakeByRefType(), typeof(bool).MakeByRefType(),
                            typeof(ReloadSkillScopeKey).MakeByRefType() }, null);
                check(buildState != null && readState != null, "等级状态帧必须携带服务端权威 scope");
                if (buildState != null && readState != null)
                {
                    var state = (byte[])buildState.Invoke(null,
                        new object[] { (byte)1, true, serverScope });
                    var sargs = new object[] { state, (byte)0, false, default(ReloadSkillScopeKey) };
                    check((bool)readState.Invoke(null, sargs) && (byte)sargs[1] == 1
                        && (bool)sargs[2] && ((ReloadSkillScopeKey)sargs[3]).Equals(serverScope),
                        "kind 5 往返必须原样读回等级与权威 scope");
                    var truncated = new byte[state.Length - 1];
                    Array.Copy(state, truncated, truncated.Length);
                    var targs = new object[] { truncated, (byte)0, false, default(ReloadSkillScopeKey) };
                    check(!(bool)readState.Invoke(null, targs), "截断的 scope 帧必须拒收（fail-closed）");

                    // 畸形 scope 身份字段与非法尾块形态=整帧拒收（Round 1 blocker 修复）。
                    var emptyServer = StateFrameWithScope(76561199030780228UL, new byte[0],
                        System.Text.Encoding.UTF8.GetBytes("PEI"));
                    var emptyServerArgs = new object[] { emptyServer, (byte)0, false, default(ReloadSkillScopeKey) };
                    check(!(bool)readState.Invoke(null, emptyServerArgs), "scope 块空 serverId 必须整帧拒收");
                    var zeroSteam = StateFrameWithScope(0UL, System.Text.Encoding.UTF8.GetBytes("U3dsHost"),
                        System.Text.Encoding.UTF8.GetBytes("PEI"));
                    var zeroSteamArgs = new object[] { zeroSteam, (byte)0, false, default(ReloadSkillScopeKey) };
                    check(!(bool)readState.Invoke(null, zeroSteamArgs), "scope 块 steamId=0 必须整帧拒收");
                    var emptyMap = StateFrameWithScope(76561199030780228UL,
                        System.Text.Encoding.UTF8.GetBytes("U3dsHost"), new byte[0]);
                    var emptyMapArgs = new object[] { emptyMap, (byte)0, false, default(ReloadSkillScopeKey) };
                    check(!(bool)readState.Invoke(null, emptyMapArgs), "scope 块空 mapName 必须整帧拒收");
                    var flagZero = new byte[] { LirRepackWireCodec.ProtocolVersion,
                        LirRepackWireCodec.MsgLevelState, 1, 0 };
                    var flagZeroArgs = new object[] { flagZero, (byte)0, false, default(ReloadSkillScopeKey) };
                    check(!(bool)readState.Invoke(null, flagZeroArgs), "flag=0 尾块帧必须整帧拒收（只认完整 scope 块）");
                    var trailingScope = new byte[state.Length + 1];
                    Array.Copy(state, trailingScope, state.Length);
                    var trailingArgs = new object[] { trailingScope, (byte)0, false, default(ReloadSkillScopeKey) };
                    check(!(bool)readState.Invoke(null, trailingArgs), "scope 帧尾随字节必须整帧拒收");
                    var badUtf8 = StateFrameWithScope(76561199030780228UL, new byte[] { 0xFF, 0xFE },
                        System.Text.Encoding.UTF8.GetBytes("PEI"));
                    var badUtf8Args = new object[] { badUtf8, (byte)0, false, default(ReloadSkillScopeKey) };
                    check(!(bool)readState.Invoke(null, badUtf8Args), "scope 块非法 UTF-8 字节必须整帧拒收");
                    var degradedState = (byte[])buildState.Invoke(null,
                        new object[] { (byte)1, true, new ReloadSkillScopeKey("\uD800", 76561199030780228UL, 0, "PEI") });
                    check(degradedState.Length == 3, "kind 5 不可编码身份必须降级为 3 字节无 scope 帧");
                    var degradedStateArgs = new object[] { degradedState, (byte)0, false, default(ReloadSkillScopeKey) };
                    check((bool)readState.Invoke(null, degradedStateArgs) && !(bool)degradedStateArgs[2],
                        "kind 5 降级帧解析成功且 hasScope=false");
                }
            }

            // 模块绑定：客户端 seat 本地解析失败 → 服务端权威 scope 直接绑定。
            var hooks = new V74Hooks { Level = 0, LocalScopeResolvable = false };
            ReloadSkillLevelMirror.Clear();
            var module = NewV74Module(hooks);
            check(module.TryAcceptSkillLevelConfirmation(1, serverScope),
                "客户端 seat 本地解析失败时必须绑定服务端权威 scope 确认等级");
            check(ReloadSkillLevelMirror.HasConfirmedFor(serverScope)
                && ReloadSkillLevelMirror.ConfirmedLevel == 1,
                "镜像必须绑定服务端 scope 并确认等级（不依赖客户端自解析）");

            // 旧图/旧槽回执拒绝保持：本地可解析且与回执 scope 不一致 = stale。
            var localScope = new ReloadSkillScopeKey("U3dsHost", 76561199030780228UL, 0, "Washington");
            hooks.LocalScopeResolvable = true;
            hooks.LocalScope = localScope;
            ReloadSkillLevelMirror.Clear();
            var staleModule = NewV74Module(hooks);
            check(!staleModule.TryAcceptSkillLevelConfirmation(1, serverScope),
                "本地解析与回执 scope 不一致=旧图/旧槽回执，必须拒绝");
            check(!ReloadSkillLevelMirror.HasConfirmedFor(serverScope), "被拒回执不得写入镜像");
            ReloadSkillLevelMirror.Clear();

            // 无权威 scope 且本地解析失败：拒绝（不猜级）。
            hooks.LocalScopeResolvable = false;
            var blindModule = NewV74Module(hooks);
            check(!blindModule.TryAcceptSkillLevelConfirmation(1, null),
                "无权威 scope 且本地解析失败时必须拒绝确认");
            ReloadSkillLevelMirror.Clear();

            // 客机不得用本地账自确认猜级；权威 seat 自确认语义保持（SP/主机回归）。
            hooks.Level = 2;
            hooks.LocalScopeResolvable = true;
            hooks.LocalScope = serverScope;
            hooks.LocalLevelResolvable = true;
            var clientModule = NewV74Module(hooks);
            clientModule.RoleProbeForTests = () => false;
            clientModule.PumpSkillRuntime();
            check(!ReloadSkillLevelMirror.HasConfirmed,
                "客机不得用本地账自确认猜级（显示输入只来自主机回执）");
            var serverModule = NewV74Module(hooks);
            serverModule.RoleProbeForTests = () => true;
            serverModule.PumpSkillRuntime();
            check(ReloadSkillLevelMirror.HasConfirmedFor(serverScope)
                && ReloadSkillLevelMirror.ConfirmedLevel == 2,
                "权威 seat（SP/主机）本地自确认语义保持");
            ReloadSkillLevelMirror.Clear();

            // 修复①锚：引擎绑定必须带客户端 seat 本地玩家解析回退。
            check(BindingHasLocalPlayerFallback(),
                "BueEngineNetBinding 必须含客户端 seat 本地玩家解析回退（Provider.clients 之外）");

            ClientSeatEndToEnd(check);
        }

        // ── 端到端环回（单模块演两席）：服务端扣经验落账→kind 4 权威 scope 回执
        // →镜像确认 1→续升 2——票面承诺的用户链路闭环 ──

        private sealed class V74Network : IBueNetworkApi
        {
            public List<(ChannelDirection Dir, IConnectionSession Session, byte[] Payload)> Sent =
                new List<(ChannelDirection, IConnectionSession, byte[])>();
            public List<(ChannelDirection Dir, Action<IConnectionSession, byte[]> Handler)> Subscriptions =
                new List<(ChannelDirection, Action<IConnectionSession, byte[]>)>();
            public List<IConnectionSession> LiveSessions = new List<IConnectionSession>();

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "V74"); }

            public bool UnregisterChannel(FeatureId channel) { return true; }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            { Subscriptions.Add((direction, handler)); return new V74Handle(); }

            public IReadOnlyList<IConnectionSession> Sessions { get { return LiveSessions; } }

            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable)
            { Sent.Add((ChannelDirection.FromClients, null, payload)); return NetworkSendResult.Sent; }

            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
            { Sent.Add((ChannelDirection.FromServer, null, payload)); return NetworkSendResult.Sent; }

            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable)
            { Sent.Add((ChannelDirection.FromServer, session, payload)); return NetworkSendResult.Sent; }

            public void DispatchInbound(ChannelDirection dir, IConnectionSession session, byte[] payload)
            {
                foreach (var pair in Subscriptions)
                {
                    if (pair.Dir == dir) pair.Handler(session, payload);
                }
            }

            private sealed class V74Handle : IDisposable { public void Dispose() { } }
        }

        private sealed class V74Session : IConnectionSession
        {
            public V74Session(ulong sessionId, ulong peerSteamId) { SessionId = sessionId; PeerSteamId = peerSteamId; }
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

        private sealed class V74Authority : ILirRepackAuthority
        {
            public LirRepackExecution ExecuteRepack(ulong senderSteamId, ulong requestId, bool hostInitiated = false)
            { return new LirRepackExecution { Outcome = LirRepackOutcome.NoChange, TotalTransferred = 0 }; }

            public LirMergeExecution ExecuteMerge(ulong targetSteamId, ulong requestId)
            { return new LirMergeExecution { Outcome = LirMergeOutcome.NoChange, TotalMerged = 0 }; }

            public bool TryResolveLocalPlayerSteamId(out ulong steamId) { steamId = 0UL; return false; }
        }

        private sealed class V74Persistence : IReloadSkillPersistence
        {
            public int SaveCalls;
            public List<ReloadSkillRecord> LastSaved = new List<ReloadSkillRecord>();

            public bool TryLoad(out List<ReloadSkillRecord> records, out string error)
            { records = new List<ReloadSkillRecord>(); error = null; return false; }

            public bool TrySave(IReadOnlyList<ReloadSkillRecord> records, out string error)
            {
                SaveCalls++;
                LastSaved = new List<ReloadSkillRecord>(records);
                error = null;
                return true;
            }
        }

        private static HostTick V74Tick(ulong number, float deltaSeconds)
        {
            return new HostTick(number, deltaSeconds, TickPhase.Update);
        }

        private static InPlaceReloadModule V74StartModule(V74Hooks hooks, V74Authority authority,
            V74Network network, out List<string> toasts, Action<bool, string> check)
        {
            var sink = new List<string>();
            toasts = sink;
            InPlaceReloadModule.SkillPatchInstallerForTests = _ => false;
            InPlaceReloadModule.CorePatchInstallerForTests = _ => true;
            InPlaceReloadModule.HudPatchInstallerForTests = _ => false;
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var feature = new FeatureId(LirRuntime.FeatureIdValue);
            var module = new InPlaceReloadModule(feature);
            module.AuthorityFactoryForTests = () => authority;
            module.NetServiceFactoryForTests = (m, net) => new LirRepackNetwork(net, m.Authority, () => false);
            module.KeyDownProviderForTests = () => false;
            module.RoleProbeForTests = () => false;
            module.SkillHooksForTests = hooks;
            module.SkillClockForTests = null;
            module.SkillPersistenceForTests = new V74Persistence();
            module.ToastSink = line => { lock (sink) sink.Add(line); };
            var pocket = LirTestPocket.Open();
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), pocket.Generation, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, network,
                null, pocket.Patching);
            var result = module.Start(bootstrap);
            check(result.Started, "harness: LIR 模块 Start 失败: " + result.DiagnosticId);
            return module;
        }

        private static void ClientSeatEndToEnd(Action<bool, string> check)
        {
            var serverScope = new ReloadSkillScopeKey("U3dsHost", 76561199030780228UL, 0, "PEI");
            var ledgerPersistence = new V74Persistence();
            var hooks = new V74Hooks
            {
                Level = 0,
                LocalScopeResolvable = false,      // 客户端 seat：本地 scope 解析不出
                LocalLevelResolvable = false,
                PeerScopeResolvable = true,
                PeerScope = serverScope,           // 服务端按请求者解析权威 scope
                // 真实账本运行时：扣经验/落账/持久化走生产 ReloadSkillRuntime 语义。
                Runtime = new ReloadSkillRuntime(new ReloadSkillStore(), () => 0d, ledgerPersistence),
                Experience = (uint)ReloadSkillPolicy.XpCostLevel0To1 + (uint)ReloadSkillPolicy.XpCostLevel1To2,
            };
            var net = new V74Network();
            List<string> toasts;
            var module = V74StartModule(hooks, new V74Authority(), net, out toasts, check);
            var session = new V74Session(9UL, 76561199030780228UL);
            net.LiveSessions.Add(session);
            module.OnHostTick(V74Tick(1UL, 0.1f)); // 首帧延迟网络初始化

            // 第一次升级：客发 kind 3 → 服务端腿执行（扣经验+落账=钩子职责）→ kind 4 → 客户端腿绑定。
            check(module.NetService.RequestUpgradeFromServer(1) == LirRepackRequestResult.Dispatched,
                "端到端：客机升级请求已发出");
            module.OnHostTick(V74Tick(2UL, 0.1f));
            var request = FindSentFrame(net, LirRepackWireCodec.MsgRequestUpgrade, 11);
            check(request != null, "端到端：捕获 kind 3 升级请求帧");
            if (request != null)
            {
                net.DispatchInbound(ChannelDirection.FromClients, session, request); // 服务器腿
                module.OnHostTick(V74Tick(3UL, 0.1f));
            }
            var reply = FindSentFrame(net, LirRepackWireCodec.MsgUpgradeResult, 0,
                p => IsUpgradeResultAccepted(p));
            check(reply != null, "端到端：升级回执 kind 4 已定向发回");
            if (reply != null)
            {
                net.DispatchInbound(ChannelDirection.FromServer, session, reply); // 客户端腿
                module.OnHostTick(V74Tick(4UL, 0.1f));
            }
            check(hooks.Upgrades.Count == 1 && hooks.Upgrades[0].Target == 1,
                "端到端：服务端执行 target=1");
            check(hooks.Experience == (uint)ReloadSkillPolicy.XpCostLevel1To2,
                "端到端：服务端已扣 125 经验（275→150；经验不足不扣、落账失败退款的语义在 ReloadSkillRuntime）");
            check(hooks.Runtime != null && hooks.Runtime.GetLevel(serverScope) == 1
                && ledgerPersistence.SaveCalls >= 1
                && ledgerPersistence.LastSaved.Exists(r => r.Scope.Equals(serverScope) && r.Level == 1),
                "端到端：等级 1 已落账并持久化（已扣经验不丢）");
            check(ReloadSkillLevelMirror.HasConfirmedFor(serverScope)
                && ReloadSkillLevelMirror.ConfirmedLevel == 1,
                "端到端：已落账等级 1 经服务端权威 scope 回执镜像确认（不依赖客户端自解析）");

            // 续升 2：行不再卡 0 级视角，第二次请求 target=2 并确认。
            check(module.NetService.RequestUpgradeFromServer(2) == LirRepackRequestResult.Dispatched,
                "端到端：镜像确认 1 后可发续升请求 target=2");
            module.OnHostTick(V74Tick(5UL, 0.1f));
            var request2 = FindSentFrame(net, LirRepackWireCodec.MsgRequestUpgrade, 11,
                p => p[10] == 2);
            check(request2 != null, "端到端：续升请求帧 target=2");
            if (request2 != null)
            {
                net.DispatchInbound(ChannelDirection.FromClients, session, request2);
                module.OnHostTick(V74Tick(6UL, 0.1f));
            }
            var reply2 = FindSentFrame(net, LirRepackWireCodec.MsgUpgradeResult, 0,
                p => IsUpgradeResultAccepted(p) && p[11] == 2);
            check(reply2 != null, "端到端：续升回执 newLevel=2");
            if (reply2 != null)
            {
                net.DispatchInbound(ChannelDirection.FromServer, session, reply2);
                module.OnHostTick(V74Tick(7UL, 0.1f));
            }
            check(hooks.Upgrades.Count == 2 && hooks.Upgrades[1].Target == 2,
                "端到端：服务端执行续升 target=2（不再重复 target=1）");
            check(hooks.Experience == 0U,
                "端到端：续升再扣 150 经验（125+150 全部消耗，无暗扣）");
            check(hooks.Runtime != null && hooks.Runtime.GetLevel(serverScope) == 2
                && ledgerPersistence.LastSaved.Exists(r => r.Scope.Equals(serverScope) && r.Level == 2),
                "端到端：等级 2 已落账并持久化");
            check(ReloadSkillLevelMirror.HasConfirmedFor(serverScope)
                && ReloadSkillLevelMirror.ConfirmedLevel == 2,
                "端到端：续升 2 经权威 scope 回执确认（用户链路闭环）");
            module.Stop(FeatureStopReason.PluginStopping);
            ReloadSkillLevelMirror.Clear();
        }

        private static byte[] FindSentFrame(V74Network net, byte kind, int exactLength, Func<byte[], bool> predicate = null)
        {
            for (var i = net.Sent.Count - 1; i >= 0; i--)
            {
                var payload = net.Sent[i].Payload;
                if (payload == null || payload.Length < 2 || payload[1] != kind) continue;
                if (exactLength > 0 && payload.Length != exactLength) continue;
                if (predicate != null && !predicate(payload)) continue;
                return payload;
            }
            return null;
        }

        private static bool IsUpgradeResultAccepted(byte[] payload)
        {
            return payload.Length >= 13 && payload[10] == 1;
        }

        /// <summary>手搓 kind 5 带 scope 尾块帧（绕过构建器，直接构造畸形形态）。</summary>
        private static byte[] StateFrameWithScope(ulong steamId, byte[] serverIdBytes, byte[] mapNameBytes)
        {
            var payload = new byte[4 + 1 + serverIdBytes.Length + 8 + 1 + 1 + mapNameBytes.Length];
            payload[0] = LirRepackWireCodec.ProtocolVersion;
            payload[1] = LirRepackWireCodec.MsgLevelState;
            payload[2] = 1; // level
            payload[3] = 1; // hasScope
            payload[4] = (byte)serverIdBytes.Length;
            Array.Copy(serverIdBytes, 0, payload, 5, serverIdBytes.Length);
            var steamOffset = 5 + serverIdBytes.Length;
            for (var i = 0; i < 8; i++) payload[steamOffset + i] = (byte)(steamId >> (8 * i));
            payload[steamOffset + 8] = 0; // characterId
            payload[steamOffset + 9] = (byte)mapNameBytes.Length;
            Array.Copy(mapNameBytes, 0, payload, steamOffset + 10, mapNameBytes.Length);
            return payload;
        }

        private static bool BindingHasLocalPlayerFallback()
        {
            try
            {
                var lirLocation = typeof(InPlaceReloadModule).Assembly.Location;
                if (string.IsNullOrEmpty(lirLocation)) return false;
                var pluginPath = Path.Combine(Path.GetDirectoryName(lirLocation),
                    "BetterUnturnedExperience.dll");
                if (!File.Exists(pluginPath)) return false;
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Path.GetDirectoryName(pluginPath));
                var parameters = new ReaderParameters { AssemblyResolver = resolver, ReadSymbols = false };
                using (var assembly = AssemblyDefinition.ReadAssembly(pluginPath, parameters))
                {
                    foreach (var type in assembly.MainModule.Types)
                    {
                        // FindSteamPlayer 是静态 facade 类 BueEngineNet 的成员（同文件
                        // 另有实例绑定类 BueEngineNetBinding，勿锚错类型）。
                        if (type.Name != "BueEngineNet") continue;
                        foreach (var method in type.Methods)
                        {
                            if (method.Name != "FindLocalSteamPlayerFallback" || !method.HasBody) continue;
                            foreach (var instruction in method.Body.Instructions)
                            {
                                var reference = instruction.Operand as MethodReference;
                                if (reference != null
                                    && reference.DeclaringType != null
                                    && reference.DeclaringType.FullName == "SDG.Unturned.Player"
                                    && reference.Name == "get_LocalPlayer")
                                    return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
