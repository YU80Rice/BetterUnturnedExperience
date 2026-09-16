using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lit;
using SDG.Unturned;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V5-05（V5-T5 → Ctrl+右键接收侧恢复）红测先行组。判据面 = 票面验收条逐字对位：
    /// 不经 tryAddItem 的路径仍能请求恢复（触发面=onSelectedItem 意图 scope：原版发过
    /// sendDragItem = 零介入；碎洞未发包 = 向主机恰一次请求）；只改接收侧（源侧恒等布局
    /// 逐格对照、非候选页逐格不动、箱子→玩家只排原版固定页序 2..6 里第一个可成立的「那一页」，
    /// 不得升格全身）；失败源和目标都不变（放不下/源件已走/版本过期/租约Busy/提交拒绝 =
    /// 两边逐格零修改）；功能停不执行（登记=唯一开关，无功能 bool、无 Prefix 内死开关）；
    /// 成功不发 TidyCompleted、不触发压弹；两页一笔原子事务（禁「转移失败但目标已被单独整理」）。
    /// 引擎面（真 Harmony 登记、真提交写面、真机意图探针）为具名接缝缺口（03/04 同界），
    /// 实机随 DEV-V5-08；此处全部判据走宿主可跑面：真实 Items/Item 网格 + PreparePage/
    /// PreparePageLeave 真规划（消费 02 唯一出口）+ 同语义假事务钉两页计划消费形状。
    /// </summary>
    internal static class DevV5FastTransferRecoverTests
    {
        internal static void Run(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            LitRuntime.MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
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
                    var savedModule = FastTransferRecoverAdapter.ActiveModule;
                    var savedCommit = FastTransferRecoverAdapter.CommitForTests;
                    var savedProbe = FastTransferRecoverEngine.IntentProbeForTests;
                    var savedInstaller = InventoryTidyModule.FastTransferPatchInstallerForTests;
                    var savedRecoverInstaller = InventoryTidyModule.RecoverPatchInstallerForTests;
                    var savedRole = LitTidyProductionAuthority.ServerRoleProbeForTests;
                    var savedLabel = ItemUseSignalsProvider.ResolveForTests;
                    var savedHeadless = BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision;
                    var savedToast = LitContainerFeedback.ToastSink;
                    FastTransferIntentScope.ResetForTests();
                    InsertRecoverScope.ResetForTests();
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        var reported = error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message;
                        if (error.InnerException != null) reported += " <INNER " + error.InnerException.GetType().Name + ": " + error.InnerException.Message + ">";
                        reds.Add("[" + name + "] " + reported);
                    }
                    finally
                    {
                        FastTransferRecoverAdapter.ActiveModule = savedModule;
                        FastTransferRecoverAdapter.CommitForTests = savedCommit;
                        FastTransferRecoverEngine.IntentProbeForTests = savedProbe;
                        InventoryTidyModule.FastTransferPatchInstallerForTests = savedInstaller;
                        InventoryTidyModule.RecoverPatchInstallerForTests = savedRecoverInstaller;
                        LitTidyProductionAuthority.ServerRoleProbeForTests = savedRole;
                        ItemUseSignalsProvider.ResolveForTests = savedLabel;
                        BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision = savedHeadless;
                        LitContainerFeedback.ToastSink = savedToast;
                        FastTransferIntentScope.ResetForTests();
                        InsertRecoverScope.ResetForTests();
                        MainThreadDispatcher.ResetForTests();
                    }
                }

                Group("触发面与接线范围", () => V55GroupTriggerAndWiring(Check));
                Group("生命周期登记即开关", () => V55GroupLifecycle(Check));
                Group("决策矩阵与只整理接收侧", () => V55GroupDecision(Check));
                Group("统一排版出口消费", () => V55GroupOfficialFirst(Check));
                Group("事务层离场物品语义", () => V55GroupTransaction(Check));
                Group("不发整理完成与线协议全链", () => V55GroupWireAndFullChain(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V5-05 fast-transfer-recover collection: ALL GREEN (0 failures) — groups: 触发面与接线范围/生命周期登记即开关/决策矩阵与只整理接收侧/统一排版出口消费/事务层离场物品语义/不发整理完成与线协议全链");
            Console.WriteLine("DEV-V5-05 fast-transfer-recover tests: " + (collectAllFailures && reds.Count > 0 ? "FAIL" : "PASS"));
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V5-05 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // ─────────────────────────────────────────────────────────────────
        // 宿主假件（03/04 同款构造法：真实 Items/Item/ItemJar 网格，反射直填尺寸绕开
        // loadSize/NetReflection；PlayerInventory 类型绝不触碰——其静态初始化在游戏外必抛。
        // 场景 = 裸 Items[] 页数组 + 容器 Items 直接驱动 engine-free 核心，owner 恒 null
        // = 引擎尾巴（快捷键/投影）不进宿主 JIT，其真机面随 08。）
        // ─────────────────────────────────────────────────────────────────

        internal static Items V55Page(byte page, byte width, byte height)
        {
            var items = new Items(page);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(Items).GetField("_width", flags).SetValue(items, width);
            typeof(Items).GetField("_height", flags).SetValue(items, height);
            typeof(Items).GetField("slots", flags).SetValue(items, new bool[width, height]);
            return items;
        }

        internal static ItemJar V55Jar(Items items, byte x, byte y, byte rot, byte sx, byte sy, ushort id, byte amount)
        {
            var jar = (ItemJar)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ItemJar));
            jar.x = x; jar.y = y; jar.rot = rot; jar.size_x = sx; jar.size_y = sy;
            SetV55ItemField(jar, new Item(id, amount, 100, new byte[0]));
            items.items.Add(jar);
            return jar;
        }

        internal static void SetV55ItemField(ItemJar jar, Item item)
        {
            foreach (var field in typeof(ItemJar).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
            {
                if (field.FieldType == typeof(Item)) { field.SetValue(jar, item); return; }
            }
            throw new InvalidOperationException("no Item field on ItemJar — fixture cannot run");
        }

        internal static FastTransferIntent V55Intent(byte sourcePage, byte x, byte y,
            LitContainerTidyKind kind = LitContainerTidyKind.WorldContainer, ulong fingerprint = 0UL)
        {
            return new FastTransferIntent { SourcePage = sourcePage, SourceX = x, SourceY = y, Kind = kind, Fingerprint = fingerprint };
        }

        // ─────────────────────────────────────────────────────────────────
        // 同语义假事务（04 V54CommitRecorder 的 leave-aware 扩展）：Committed 才动格；
        // Rejected/Critical 保持原网格逐格零修改。leave prep = 恒等布局，提交 = 仅摘走
        // 离场件（生产 = CommitPage remove-all/re-add，其余物品坐标逐格不变）；pending
        // prep = 02 计划落格 + 裸壳 jar 入账（生产 = Items.addItem 新建 jar）。
        // ─────────────────────────────────────────────────────────────────

        internal sealed class V55CommitRecorder
        {
            public int Calls;
            public List<PagePreparation> LastPreps;
            public Dictionary<ItemJar, NewPosition> LastMapping;
            public TidyOperationOutcome Result = TidyOperationOutcome.Committed;

            public TidyOperationOutcome Commit(List<PagePreparation> preps, Dictionary<ItemJar, NewPosition> mapping)
            {
                Calls++;
                LastPreps = preps; LastMapping = mapping;
                if (Result == null || Result.Result != TidyCommitResult.Committed) return Result ?? TidyOperationOutcome.RejectedNoMutation;
                foreach (var prep in preps)
                {
                    if (prep.Leaving != null && prep.Leaving.Tag is ItemJar gone && gone.item != null)
                    {
                        var idx = prep.ItemsInstance.items.IndexOf(gone);
                        if (idx < 0) throw new InvalidOperationException("leave jar not on page — fake commit inconsistent");
                        prep.ItemsInstance.items.RemoveAt(idx);
                    }
                    var pendingTag = prep.Pending == null ? null : prep.Pending.Tag;
                    foreach (var p in prep.Result)
                    {
                        if (p == null || !p.Placed) continue;
                        if (pendingTag != null && ReferenceEquals(p.Tag, pendingTag))
                        {
                            var jar = (ItemJar)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ItemJar));
                            var item = (Item)p.Tag;
                            jar.x = p.ResultX; jar.y = p.ResultY; jar.rot = p.ResultRot;
                            jar.size_x = p.size_x; jar.size_y = p.size_y;
                            SetV55ItemField(jar, item);
                            prep.ItemsInstance.items.Add(jar);
                            continue;
                        }
                        if (p.Tag is ItemJar existing && existing.item != null)
                        {
                            existing.x = p.ResultX; existing.y = p.ResultY; existing.rot = p.ResultRot;
                            mapping[existing] = new NewPosition(prep.Page, p.ResultX, p.ResultY, p.ResultRot);
                        }
                    }
                }
                return TidyOperationOutcome.Committed;
            }
        }

        /// <summary>计数直通策略（04 同款形）：钉「恢复的计划确实经模块注入的策略出口」。</summary>
        internal sealed class V55CountingStrategy : ITidyStrategy
        {
            private readonly TaggedRowBandV1Strategy inner = new TaggedRowBandV1Strategy();
            public int BuildPlanCalls;
            public string StrategyId { get { return inner.StrategyId; } }
            public TidyPlan BuildPlan(TidyInput input) { BuildPlanCalls++; return inner.BuildPlan(input); }
        }

        /// <summary>恒拒策略：证明成败完全由计划出口决定，拒绝 = 两侧零修改。</summary>
        internal sealed class V55RejectStrategy : ITidyStrategy
        {
            public string StrategyId { get { return "v5-05-reject-fixture"; } }
            public TidyPlan BuildPlan(TidyInput input)
            {
                var results = new List<PackableItem>(input.Items.Count);
                foreach (var item in input.Items)
                {
                    item.Placed = false; item.ResultX = 0; item.ResultY = 0; item.ResultRot = 0;
                    results.Add(item);
                }
                return new TidyPlan(StrategyId, results, false);
            }
        }

        private sealed class V55SettingsView : IScopedFeatureSettings
        {
            public readonly List<SettingEntryView> Entries = new List<SettingEntryView>();
            public uint Revision = 7U;

            public FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)
            {
                return new FeatureSettingsSnapshot(new FeatureId(LitRuntime.FeatureIdValue), 1U,
                    revisionScope, Revision, SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, Entries);
            }

            public bool TryGet(string settingId, out SettingValue value, out uint revision)
            {
                value = default(SettingValue);
                revision = Revision;
                for (var index = 0; index < Entries.Count; index++)
                {
                    if (Entries[index].SettingId != settingId) continue;
                    value = Entries[index].EffectiveValue;
                    return true;
                }
                return false;
            }

            public SettingChangeResult Submit(ScopedSettingChangeRequest request)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, Revision, default(FeatureSettingsSnapshot));
            }

            internal void SetDirection(string label)
            {
                Entries.Clear();
                Entries.Add(new SettingEntryView(InventoryTidyModule.DirectionSettingId, SettingAuthority.ClientLocal,
                    new SettingValueOption(true, SettingValue.Choice(label)), false, default(SettingPolicyView),
                    SettingValue.Choice(label), true, true));
            }
        }

        private sealed class V55Lifetime : IFeatureLifetime
        {
            public FeatureState State = FeatureState.Running;
            public readonly List<IDisposable> Tracked = new List<IDisposable>();

            public bool TryTrack(IDisposable registration) { Tracked.Add(registration); return true; }

            public FeatureStatusView CurrentStatus
            {
                get { return new FeatureStatusView(new FeatureId(LitRuntime.FeatureIdValue), State, FrameworkErrorCode.None, FeatureStopReason.None, "v5-05-fake", 1UL); }
            }
        }

        internal static List<(byte x, byte y, byte rot, ushort id, byte amount)> V55Snap(Items items)
        {
            var list = new List<(byte, byte, byte, ushort, byte)>();
            if (items == null) return list;
            for (byte i = 0; i < items.getItemCount(); i++)
            {
                var jar = items.getItem(i);
                list.Add((jar.x, jar.y, jar.rot, jar.item.id, jar.item.amount));
            }
            return list;
        }

        internal static Items[] V55Pages(Items page3 = null, Items page2 = null)
        {
            // 九槽页数组（0/1 恒 null=主副手不入恢复；2..6 玩家页；7 由 containerGrid 参数单传）。
            var pages = new Items[9];
            pages[2] = page2;
            pages[3] = page3;
            return pages;
        }

        internal static LitContainerLiveFacts V55Live(
            LitContainerSessionKind kind = LitContainerSessionKind.WorldContainer,
            bool active = true, ulong fingerprint = 0UL,
            bool openerOk = true, bool accessOk = true, bool driverOk = true)
        {
            return new LitContainerLiveFacts
            {
                KindObserved = kind,
                SessionActive = active,
                WorldOpenerIsRequester = openerOk,
                WorldAccessAllowed = accessOk,
                TrunkDriverAuthorized = driverOk,
                LiveFingerprint = fingerprint,
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // 组1：触发面与接线范围。binder 面恰两条+形状逐参核实（宿主可证=04 R2 同界）；
        // 意图 scope 语义（发包=撤回零介入；未发=恰一次；无 Enter 不泄漏；不挂拖放预览=
        // scope 外的 sendDragItem 结构性无关）；AREA/主副手/未 isStoring 属真机探针面
        // （具名缺口随 08），此处钉探针假缝的调用面。
        // ─────────────────────────────────────────────────────────────────
        private static void V55GroupTriggerAndWiring(System.Action<bool, string> check)
        {
            // 1a. 触发面 = 恰两个 patch 类、固定序、每目标声明类型/名/参数形状逐一核实。
            var surface = FastTransferRecoverBinder.PatchSurface;
            check(surface.Count == 2
                    && surface[0] == typeof(FastTransferIntentPatch)
                    && surface[1] == typeof(FastTransferDragSendPatch),
                "触发面恰两条且序固定（意图开合器、发包观察器），无第三触发点可挂");
            var intentTarget = FastTransferRecoverBinder.ResolveTarget(typeof(FastTransferIntentPatch));
            check(intentTarget != null
                    && intentTarget.DeclaringType != null
                    && intentTarget.DeclaringType.Name == "PlayerDashboardInventoryUI"
                    && intentTarget.Name == "onSelectedItem"
                    && intentTarget.IsStatic
                    && intentTarget.GetParameters().Length == 3,
                "意图面绑定的确是原版 Ctrl+右键唯一入口 onSelectedItem(byte,byte,byte)（私有静态）");
            var dragTarget = FastTransferRecoverBinder.ResolveTarget(typeof(FastTransferDragSendPatch));
            check(dragTarget != null && dragTarget.DeclaringType == typeof(PlayerInventory)
                    && dragTarget.Name == "sendDragItem" && !dragTarget.IsStatic
                    && dragTarget.GetParameters().Length == 7,
                "发包观察面绑定的确是玩家↔容器快速转移唯一发包出口 PlayerInventory.sendDragItem(7×byte)");
            check(FastTransferRecoverBinder.ResolveTarget(typeof(InsertRecoverAutoAddPatch)) == null,
                "未知/非本票类解析为 null = fail-closed（binder 是唯一绑定事实源）");
            check(intentTarget != dragTarget, "两触发点不同方法（无重复安装面）");

            // 1b. 意图 scope 语义（纯 managed，无引擎）。
            FastTransferIntentScope.ResetForTests();
            check(FastTransferIntentScope.ExitEvaluate() == null, "未开 scope 收场 = 无请求（Finalizer 幂等安全）");
            FastTransferIntentScope.NoteDragSent();
            var intent = V55Intent(3, 0, 0);
            FastTransferIntentScope.Enter(intent);
            check(FastTransferIntentScope.IsOpen, "Enter 后 scope 在场");
            check(ReferenceEquals(FastTransferIntentScope.ExitEvaluate(), intent),
                "碎洞未发包（scope 内 sendDragItem 从未被调）= 恰一次恢复请求");
            check(FastTransferIntentScope.ExitEvaluate() == null && !FastTransferIntentScope.IsOpen,
                "请求只此一次，scope 随即闭合（不自递归、不重复发包）");
            var sent = V55Intent(7, 2, 2);
            FastTransferIntentScope.Enter(sent);
            FastTransferIntentScope.NoteDragSent();
            check(FastTransferIntentScope.ExitEvaluate() == null,
                "原版已发 sendDragItem = 撤回请求（成功转移不排：恢复对这次转移零介入）");
            FastTransferIntentScope.Enter(null);
            check(FastTransferIntentScope.ExitEvaluate() == null && !FastTransferIntentScope.IsOpen
                    && FastTransferIntentScope.ExitEvaluate() == null,
                "Enter(null 意图)=不开 scope：探针判否（未Ctrl/未isStoring/AREA/主副手/选中守卫/虚拟箱）时零请求");
            FastTransferIntentScope.NoteDragSent(); // 无窗在开 = 结构性无关（真拖放/菜单存放/ BII）
            FastTransferIntentScope.Enter(sent);
            check(ReferenceEquals(FastTransferIntentScope.ExitEvaluate(), sent),
                "前一次的 NoteDragSent 不泄漏进新 scope（Enter 重置已发标记）");
            check(FastTransferIntentScope.ExitEvaluate() == null, "再收场即空（一次 Enter 至多一个请求）");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组2：生命周期登记即开关。登记 = EnsureStarted 装面时交付 ActiveModule；
        // Stop/隔离注销；U3DS headless 仍登记（权威行为非画面，story 25）；
        // 无独立开关、无功能 bool、无 Prefix 内死开关；熔断共享同一把闸。
        // ─────────────────────────────────────────────────────────────────
        private static void V55GroupLifecycle(System.Action<bool, string> check)
        {
            var installed = new List<Type>();
            var recoverInstalled = new List<Type>();
            InventoryTidyModule.FastTransferPatchInstallerForTests = t => { installed.Add(t); return true; };
            InventoryTidyModule.RecoverPatchInstallerForTests = t => { recoverInstalled.Add(t); return true; };
            LitTidyProductionAuthority.ServerRoleProbeForTests = () => false; // 客机角色：本地权威执行面不进宿主
            var settings = new V55SettingsView();
            settings.SetDirection(InventoryTidyModule.DirectionDescendingLabel);
            var module = V55CreateModule(check, settings, new V55Authority());
            if (module == null) return;
            try
            {
                // 2a. 登记即开关：Start（经假 installer 缝）= 恰两条本票面按 binder 序安装、
                // ActiveModule 代际交付；04 面独立共存（各自登记、互不顶替在册位）。
                check(installed.Count == 2
                        && installed[0] == typeof(FastTransferIntentPatch)
                        && installed[1] == typeof(FastTransferDragSendPatch),
                    "Start 登记的恰是本票两触发面且按 binder 序安装（无第三面）");
                check(recoverInstalled.Count == 3, "04 入包恢复面与 05 快速转移面同代各自登记（两面独立在册）");
                check(ReferenceEquals(FastTransferRecoverAdapter.ActiveModule, module),
                    "ActiveModule = 本代际（登记即交付，交接诚实）");
                check(module.FastTransferPatchesInstalled, "FastTransferPatchesInstalled 随登记为真");

                // 2b. 未登记 = 原版语义：不规划不发送不执行。
                FastTransferRecoverAdapter.ActiveModule = null;
                check(FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 0, 0)) == LitTidyRequestResult.NativeFallback,
                    "ActiveModule=null → NativeFallback（生命周期闸=唯一开关，零介入）");
                check(module.RequestFastTransferRecoverFromIntent(V55Intent(3, 0, 0)) != LitTidyRequestResult.NativeFallback,
                    "模块代际自身在跑时模块入口照走登记后的门序（开关=登记，不是模块私 bool）");
                FastTransferRecoverAdapter.ActiveModule = module;

                // 2c. 熔断共享：同一把 FaultGate 开闸 = 拒绝（与按钮/04/03 同闸，无并行 BUE 锁）。
                module.FaultGate.Open("v5-05 fixture fault", restoreVerified: true);
                check(FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 0, 0)) == LitTidyRequestResult.RejectedFaultCircuit,
                    "熔断开 = 快速转移恢复拒绝（同一熔断）");
                module.FaultGate.Reset();

                // 2d. 方向偏好不可读 = 诚实拒绝（Q59 同规，不发明默认）。
                settings.Entries.Clear();
                check(FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 0, 0)) == LitTidyRequestResult.RejectedPreferenceUnavailable,
                    "已保存方向读不到 = 拒绝（不猜方向）");
                settings.SetDirection(InventoryTidyModule.DirectionDescendingLabel);

                // 2e. 过完全部登记门 = 无会话时显式 RejectedNoSession（真客户端绝不本地改格）。
                check(FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 0, 0)) == LitTidyRequestResult.RejectedNoSession,
                    "门序走尽、会话未就绪 = RejectedNoSession（本机恢复绝不伪成功）");

                // 2f. Stop 注销：登记交还、面撤装（功能停 = 路径不执行，非 if 短路）。
                module.Stop(FeatureStopReason.PluginStopping);
                check(!module.FastTransferPatchesInstalled && FastTransferRecoverAdapter.ActiveModule == null,
                    "Stop 后恢复面注销（补丁没了、登记也交还）");
                check(FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 0, 0)) == LitTidyRequestResult.NativeFallback,
                    "功能停 = 请求路径不执行");
            }
            finally
            {
                try { module.Stop(FeatureStopReason.PluginStopping); } catch (Exception) { }
                FastTransferRecoverAdapter.ActiveModule = null;
            }

            // 2g. U3DS headless：画面闸不挡权威登记（story 25 / T1 Headless 裁决——只砍画面）。
            BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision = true;
            var headlessInstalled = new List<Type>();
            InventoryTidyModule.FastTransferPatchInstallerForTests = t => { headlessInstalled.Add(t); return true; };
            InventoryTidyModule.RecoverPatchInstallerForTests = t => true;
            var headlessSettings = new V55SettingsView();
            headlessSettings.SetDirection(InventoryTidyModule.DirectionDescendingLabel);
            var headless = V55CreateModule(check, headlessSettings, new V55Authority());
            if (headless != null)
            {
                check(headlessInstalled.Count == 2, "U3DS headless 仍登记两触发面（执行面在，画面无）");
                check(headless.FastTransferPatchesInstalled && !headless.PatchesInstalled
                        && headless.StartGateDiagnostics == "headless-ui-not-armed",
                    "headless 只砍 UI 注入闸，恢复登记闸保持（诚实诊断）");
                headless.Stop(FeatureStopReason.PluginStopping);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // 假权威（组2/6 用；线协议全链对位 03 容器全链形状）。
        // ─────────────────────────────────────────────────────────────────
        private sealed class V55Authority : ILitTidyAuthority
        {
            public int FastCount;
            public LitFastTransferRequestContext LastFast;
            public LitContainerTidyReason FastReason = LitContainerTidyReason.None;
            public TidyCommitResult FastCommit = TidyCommitResult.Committed;
            public bool ThrowOnFast;

            public List<HotkeySnapshot> CaptureClientHotkeys() { return new List<HotkeySnapshot>(); }
            public LitAuthorityResult ExecuteServerTidy(LitTidyRequestContext request)
            {
                return LitAuthorityResult.From(TidyOperationOutcome.Committed);
            }
            public LitContainerAuthorityResult ExecuteServerContainerTidy(LitContainerTidyRequestContext request)
            {
                return new LitContainerAuthorityResult { Outcome = TidyOperationOutcome.Committed, Reason = LitContainerTidyReason.None };
            }
            public LitContainerAuthorityResult ExecuteServerFastTransferRecover(LitFastTransferRequestContext request)
            {
                if (ThrowOnFast) throw new InvalidOperationException("fixture authority crash");
                FastCount++;
                LastFast = request;
                return new LitContainerAuthorityResult
                {
                    Outcome = FastCommit == TidyCommitResult.Committed ? TidyOperationOutcome.Committed : TidyOperationOutcome.RejectedNoMutation,
                    Reason = FastReason,
                };
            }
            public LitHotkeyRestoreResult RestoreServerHotkeys(ulong peerSteamId, List<HotkeyRestoreEntry> entries)
            {
                return new LitHotkeyRestoreResult { Restored = 0, Verified = 0, Cleared = 0, FailedIndices = new List<byte>() };
            }
            public bool VerifyClientConvergence(List<LitNewPositionMapping> mappings) { return true; }
        }

        // ─────────────────────────────────────────────────────────────────
        // 组3：决策矩阵与只整理接收侧。两方向成功 = 只动该动的两侧（源恒等布局逐格对照、
        // 非候选页逐格不动）；放不下/源件已走/版本过期/会话关闭/权限变化/提交拒绝/
        // Critical = 两边逐格零修改（假事务只在 Committed 分支落格）。
        // ─────────────────────────────────────────────────────────────────
        private static void V55GroupDecision(System.Action<bool, string> check)
        {
            LitTidyProductionAuthority.ServerRoleProbeForTests = () => false;
            var settings = new V55SettingsView();
            settings.SetDirection(InventoryTidyModule.DirectionDescendingLabel);
            var module = V55CreateModule(check, settings, new V55Authority());
            if (module == null) return;
            var recorder = new V55CommitRecorder();
            FastTransferRecoverAdapter.ActiveModule = module;
            FastTransferRecoverAdapter.CommitForTests = recorder.Commit;
            ItemUseSignalsProvider.ResolveForTests = item => item.id == 5001 ? PlayerUseLabel.Magazine : PlayerUseLabel.Food;
            try
            {
                // 3a. 玩家→箱子（碎洞未发包后的恢复）：整理箱子 + 这一件入箱；源页恒等。
                {
                    var page3 = V55Page(3, 10, 2);
                    V55Jar(page3, 0, 0, 0, 2, 1, 5001, 1); // 待转移的 2x1
                    for (byte i = 2; i < 10; i++) V55Jar(page3, i, 0, 0, 1, 1, 4002, 1);
                    var box = V55FragmentedBox();
                    var pages = V55Pages(page3);
                    var live = V55Live(fingerprint: LitContainerContentFingerprint.FromItems(box));
                    var claim = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = live.LiveFingerprint };
                    var boxBefore = V55Snap(box); var pageBefore = V55Snap(page3);
                    var attempt = FastTransferRecoverAdapter.TryRecover(null, pages, box, live, claim, 3, 0, 0, true);
                    check(attempt.Recovered && attempt.Reason == LitContainerTidyReason.None && attempt.TargetPage == 7,
                        "玩家→箱子：整理受支持容器 + 这一件入箱（恢复成功）");
                    check(recorder.Calls == 1 && recorder.LastPreps.Count == 2
                            && recorder.LastPreps[0].Page == 3 && recorder.LastPreps[1].Page == 7,
                        "一笔两页原子事务：[源页恒等离场, 容器排版+待转移件]（源先目标后=原版 remove-then-add 同序）");
                    check(recorder.LastPreps[0].Leaving != null && recorder.LastPreps[0].Pending == null
                            && recorder.LastPreps[0].Result.Count == 8,
                        "源侧 = Leave 语义（排除这一件，其余 8 件逐格在计划内、不排版）");
                    check(recorder.LastPreps[1].Pending != null
                            && recorder.LastPreps[1].Pending.Tag is Item pendingItem && pendingItem.id == 5001,
                        "容器侧 = 02 计划 + 待转移物品（Pending 复用 04 语义）");
                    var movedItem = (Item)recorder.LastPreps[1].Pending.Tag;
                    check(FoundAt(box, 5001), "提交后这一件在箱子里（禁吞物：不在结果内不算成功）");
                    check(ContainsItem(page3, movedItem) == false, "同一 Item 实例已离源（引用保持=原版同款）");
                    check(SourceIdentityPreserved(pageBefore, page3, 5001),
                        "源侧其余物品逐格坐标/旋转原样（只整理接收侧=源页恒等）");
                    check(box.items.Count == boxBefore.Count + 1 && FoundAt(box, 5001),
                        "接收侧 = 该箱经排版多这一件入账（其余件按 02 计划落位，组4 逐格钉）");
                    check(pages[2] == null, "非候选页根本不入事务（不得升格全身）");
                }

                // 3b. 箱子→玩家：按原版固定页序 2..6 取第一个「整理+放入」成立的页。
                {
                    var box = V55Page(7, 10, 2);
                    var moved = V55Jar(box, 0, 0, 0, 2, 1, 5001, 1);
                    for (byte i = 2; i < 10; i++) V55Jar(box, i, 0, 0, 1, 1, 4001, 1);
                    for (byte i = 0; i < 9; i++) V55Jar(box, i, 1, 0, 1, 1, 4001, 1);
                    // 箱内 18 件 + 待走件 1 件；碎片洞 (1,0)(9,1) 不成 2x1。
                    var page2 = V55FullPage(2, 20); // 20/20 满页：排版也放不下 → 候选不成立
                    var page3 = V55FragmentedPage(3); // 18 件对两个不成 2x1 的碎洞：整理后可放
                    var pages = V55Pages(page3, page2);
                    var live = V55Live(fingerprint: LitContainerContentFingerprint.FromItems(box));
                    var claim = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = live.LiveFingerprint };
                    var boxBefore = V55Snap(box); var page2Before = V55Snap(page2);
                    var attempt = FastTransferRecoverAdapter.TryRecover(null, pages, box, live, claim, 7, 0, 0, true);
                    check(attempt.Recovered && attempt.TargetPage == 3,
                        "箱子→玩家：页2 放不下（跳过不整理）、页3 = 原版页序里第一个可成立候选 = 唯一被排的页");
                    check(recorder.LastPreps.Count == 2 && recorder.LastPreps[0].Page == 7 && recorder.LastPreps[1].Page == 3,
                        "两页事务 = [容器恒等离场, 页3 排版+待转移件]");
                    check(V55Snap(page2).Count == page2Before.Count && SnapEqual(page2Before, V55Snap(page2)),
                        "页2 一格未动（落选候选不得被整理）");
                    check(!ContainsItem(box, moved.item) && FoundAt(page3, 5001), "这一件离开箱子、落进页3 计划位");
                    check(SourceIdentityPreserved(boxBefore, box, 5001), "箱子其余物品恒等逐格（源网格不整理）");
                }

                // 3c. 目标碎洞+整理也放不下 → LayoutFailed，两侧零修改，事务不发起。
                {
                    var p3 = V55Page(3, 10, 2);
                    V55Jar(p3, 0, 0, 0, 2, 1, 5001, 1);
                    for (byte i = 0; i < 18; i++) V55Jar(p3, (byte)(i % 10), (byte)(i / 10), 0, 1, 1, 4201, 1);
                    var box = V55FullPage(7, 20);
                    var pages = V55Pages(p3);
                    var live = V55Live(fingerprint: LitContainerContentFingerprint.FromItems(box));
                    var claim = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = live.LiveFingerprint };
                    var boxBefore = V55Snap(box); var p3Before = V55Snap(p3);
                    var callsBefore = recorder.Calls;
                    var attempt = FastTransferRecoverAdapter.TryRecover(null, pages, box, live, claim, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.LayoutFailed
                            && recorder.Calls == callsBefore
                            && SnapEqual(boxBefore, V55Snap(box)) && SnapEqual(p3Before, V55Snap(p3)),
                        "放不下 = 原版失败 + 两侧逐格零修改（禁「转移失败但目标已被单独整理」——事务根本没发起）");
                }

                // 3d. 恒拒策略 = 计划出口拒绝一切 → 同上零修改。
                {
                    module.Strategy = new V55RejectStrategy();
                    var p3 = V55FragmentedSource(3);
                    var box = V55FragmentedBox();
                    var live = V55Live(fingerprint: LitContainerContentFingerprint.FromItems(box));
                    var claim = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = live.LiveFingerprint };
                    var attempts = recorder.Calls;
                    var attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box, live, claim, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.LayoutFailed && recorder.Calls == attempts,
                        "计划拒绝一切 = 零修改（成败完全由 02 出口决定，恢复不绕行）");
                    module.Strategy = new TaggedRowBandV1Strategy();
                }

                // 3e. 源件已走（权威坐标直读为空）→ ContentChanged，零修改。
                {
                    var p3 = V55FragmentedSource(3);
                    var box = V55FragmentedBox();
                    var live = V55Live(fingerprint: LitContainerContentFingerprint.FromItems(box));
                    var claim = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = live.LiveFingerprint };
                    var before = recorder.Calls;
                    var attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box, live, claim, 5, 7, 7, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.ContentChanged
                            && attempt.Refusal == FastTransferRecoverPlanner.RefusalSourceGone && recorder.Calls == before,
                        "源物已走 = 内容已变（零修改）");
                }

                // 3f. 版本/会话/权限矩阵（03 verifier 复用，权威现读重验）。
                {
                    var p3 = V55FragmentedSource(3);
                    var box = V55FragmentedBox();
                    var fp = LitContainerContentFingerprint.FromItems(box);
                    var stale = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = fp ^ 1UL };
                    var attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box, V55Live(fingerprint: fp), stale, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.ContentChanged,
                        "指纹过期 = ContentChanged（请求版本必重验，客机不能冒充）");
                    attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box,
                        V55Live(active: false, fingerprint: fp),
                        new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = fp }, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.ContainerClosed,
                        "会话已关 = ContainerClosed");
                    attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box,
                        V55Live(openerOk: false, fingerprint: fp),
                        new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = fp }, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.LostAccess,
                        "opener 已换 = LostAccess（世界箱自有权限维度）");
                    attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box,
                        V55Live(kind: LitContainerSessionKind.VirtualContainer, fingerprint: fp),
                        new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = fp }, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.UnsupportedKind,
                        "虚拟箱 = UnsupportedKind（范围与 03 一致，权威 fail-closed）");
                    attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box,
                        V55Live(kind: LitContainerSessionKind.VehicleTrunk, driverOk: true, fingerprint: fp),
                        new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = fp }, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.ContainerClosed,
                        "声明种类≠现读种类（箱≠后备箱互不越界）= 会话已换");
                    // 后备箱正形：kind=2 + driver 在座 → 可恢复。
                    var liveTrunk = V55Live(kind: LitContainerSessionKind.VehicleTrunk, driverOk: true, fingerprint: fp);
                    attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box, liveTrunk,
                        new LitContainerTidyClaim { Kind = LitContainerTidyKind.VehicleTrunk, Fingerprint = fp }, 3, 0, 0, true);
                    check(attempt.Recovered, "已授权后备箱 = 受支持接收侧（03 范围镜像）");
                    var trunkClaimBox = new LitContainerTidyClaim { Kind = LitContainerTidyKind.VehicleTrunk, Fingerprint = fp };
                    attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box,
                        V55Live(kind: LitContainerSessionKind.VehicleTrunk, driverOk: false, fingerprint: fp), trunkClaimBox, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.LostAccess,
                        "离座 = LostAccess（后备箱自有维度，不读世界箱闸）");
                }

                // 3h. 原版满闸（目标页已有 200 件 = ReceiveDragItem 的 >=200 拒）：
                // 箱用 10×21（容量 210）——排版本可容下 2×1，唯一拒绝理由就是原版件数闸。
                {
                    var p3 = V55FragmentedSource(3);
                    var fullBox = V55Page(7, 10, 21);
                    for (int i = 0; i < 200; i++)
                        V55Jar(fullBox, (byte)(i % 10), (byte)(i / 10), 0, 1, 1, (ushort)(3000 + i), 1);
                    var live200 = V55Live(fingerprint: LitContainerContentFingerprint.FromItems(fullBox));
                    var claim200 = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = live200.LiveFingerprint };
                    var calls200 = recorder.Calls;
                    var attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), fullBox, live200, claim200, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.LayoutFailed
                            && recorder.Calls == calls200,
                        "目标 200 件满 = 原版同闸拒绝（恢复不越原版放置条件）");
                }

                // 3g. 提交 Rejected = 事务拒收（内容已变语义）；Critical = 熔断 + InternalFailure。
                {
                    var p3 = V55FragmentedSource(3);
                    var box = V55FragmentedBox();
                    var fp = LitContainerContentFingerprint.FromItems(box);
                    var claim = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = fp };
                    var boxBefore = V55Snap(box);
                    recorder.Result = TidyOperationOutcome.RejectedNoMutation;
                    var attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box, V55Live(fingerprint: fp), claim, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.ContentChanged
                            && attempt.Refusal == FastTransferRecoverAdapter.RefusalCommitRejected
                            && SnapEqual(boxBefore, V55Snap(box)),
                        "提交 Rejected = 两边不动（假事务 Rejected 分支不动格）");
                    recorder.Result = new TidyOperationOutcome { Result = TidyCommitResult.CriticalFailure, RollbackVerified = true };
                    attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box, V55Live(fingerprint: fp), claim, 3, 0, 0, true);
                    check(!attempt.Recovered && attempt.Reason == LitContainerTidyReason.InternalFailure
                            && attempt.Refusal == FastTransferRecoverAdapter.RefusalCommitCritical
                            && !module.FaultGate.Allowed,
                        "Critical = 熔断（同一把闸）+ InternalFailure 结构化原因");
                    module.FaultGate.Reset();
                    recorder.Result = TidyOperationOutcome.Committed;
                }
            }
            finally
            {
                module.Stop(FeatureStopReason.PluginStopping);
                FastTransferRecoverAdapter.ActiveModule = null;
                FastTransferRecoverAdapter.CommitForTests = null;
                ItemUseSignalsProvider.ResolveForTests = null;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // 组4：官方先行消费。真实碎洞场景的接收侧计划逐格等于独立 computed 的
        // tagged-row-band-v1(+pending) 计划；策略出口计数；分类器缝消费待转移件；
        // PreparePageLeave 恒等布局直测。
        // ─────────────────────────────────────────────────────────────────
        private static void V55GroupOfficialFirst(System.Action<bool, string> check)
        {
            LitTidyProductionAuthority.ServerRoleProbeForTests = () => false;
            var settings = new V55SettingsView();
            settings.SetDirection(InventoryTidyModule.DirectionDescendingLabel);
            var module = V55CreateModule(check, settings, new V55Authority());
            if (module == null) return;
            var recorder = new V55CommitRecorder();
            FastTransferRecoverAdapter.ActiveModule = module;
            FastTransferRecoverAdapter.CommitForTests = recorder.Commit;
            ItemUseSignalsProvider.ResolveForTests = item => item.id == 5001 ? PlayerUseLabel.Magazine : PlayerUseLabel.Food;
            try
            {
                var counting = new V55CountingStrategy();
                module.Strategy = counting;

                // 4a. 玩家→箱子：出口恰调一次；容器计划逐格对照独立 computed 的 02 计划
                // （对照计划先算——假事务提交会改箱内坐标，事后重算就不是同一输入了）。
                var p3 = V55FragmentedSource(3);
                var box = V55FragmentedBox();
                var fp = LitContainerContentFingerprint.FromItems(box);
                var claim = new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = fp };
                var expected = ManualTidyService.PreparePage(box, 7, true, TidyMode.SameType, new TaggedRowBandV1Strategy(),
                    new PackableItem { Tag = p3.items[0].item, size_x = 2, size_y = 1, GroupKey = 5001, StableOrder = box.getItemCount(), Label = PlayerUseLabel.Magazine });
                var attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(p3), box, V55Live(fingerprint: fp), claim, 3, 0, 0, true);
                check(attempt.Recovered && counting.BuildPlanCalls == 1
                        && counting.StrategyId == new TaggedRowBandV1Strategy().StrategyId,
                    "接收侧计划恰经模块注入策略出口一次，出口 = tagged-row-band-v1（唯一内置策略）");
                var actual = recorder.LastPreps[1];
                check(expected.Valid && PlanEqual(expected.Result, actual.Result),
                    "容器侧逐格 = 独立 computed 的 02 计划（同输入同计划，恢复不复制算法）");
                check(actual.Pending != null && ((Item)actual.Pending.Tag).id == 5001 && actual.Pending.Label == PlayerUseLabel.Magazine,
                    "待转移物品的标签消费 02 分类器缝（弹匣段参与排版，不单看 id 猜）");

                // 4b. 箱子→玩家：候选页序 = 原版固定序，出口至多逐页尝试（页2 失败、页3 成功）。
                counting.BuildPlanCalls = 0;
                var box2 = V55Page(7, 10, 2);
                var moved = V55Jar(box2, 0, 0, 0, 2, 1, 5001, 1);
                for (byte i = 2; i < 10; i++) V55Jar(box2, i, 0, 0, 1, 1, 4001, 1);
                for (byte i = 0; i < 9; i++) V55Jar(box2, i, 1, 0, 1, 1, 4001, 1);
                var page2 = V55FullPage(2, 20);
                var page3 = V55FragmentedPage(3);
                var fp2 = LitContainerContentFingerprint.FromItems(box2);
                attempt = FastTransferRecoverAdapter.TryRecover(null, V55Pages(page3, page2), box2,
                    V55Live(fingerprint: fp2), new LitContainerTidyClaim { Kind = LitContainerTidyKind.WorldContainer, Fingerprint = fp2 }, 7, 0, 0, true);
                check(attempt.Recovered && attempt.TargetPage == 3 && counting.BuildPlanCalls == 2,
                    "页2 尝试不成、页3 成 = 原版升序逐页试（复现顺序、不升格、不回头整理源）");

                // 4c. PreparePageLeave 直测：恒等布局 = 其余逐格原坐标，离场件恰好缺席。
                var page = V55Page(3, 10, 2);
                var leave = V55Jar(page, 4, 0, 0, 2, 1, 7001, 5);
                V55Jar(page, 0, 0, 0, 1, 1, 7002, 1);
                V55Jar(page, 1, 1, 1, 1, 1, 7003, 1);
                var prep = ManualTidyService.PreparePageLeave(page, 3, leave);
                check(prep.Valid && prep.Result.Count == 2 && prep.Leaving != null && ReferenceEquals(prep.Leaving.Tag, leave)
                        && prep.BeforeJars.Count == 3,
                    "Leave prep：快照含离场件、计划排除离场件（其余 2 件在场）");
                bool identity = true;
                for (int i = 0; i < prep.Result.Count; i++)
                {
                    var e = prep.Result[i];
                    var jar = (ItemJar)e.Tag;
                    if (!e.Placed || e.ResultX != jar.x || e.ResultY != jar.y || e.ResultRot != jar.rot) identity = false;
                }
                check(identity, "恒等布局：其余物品原坐标原旋转逐格相同（源网格不整理的字面落实）");
                var other = V55Page(3, 10, 2);
                V55Jar(other, 0, 0, 0, 1, 1, 7004, 1);
                check(!ManualTidyService.PreparePageLeave(other, 3, leave).Valid,
                    "离场件不在该页 = fail-closed 非法 prep（零修改拒绝，绝不空摘）");
                check(!ManualTidyService.PreparePageLeave(V55Page(3, 0, 0), 3, leave).Valid, "0×0 页 = 非法 prep");
            }
            finally
            {
                module.Stop(FeatureStopReason.PluginStopping);
                FastTransferRecoverAdapter.ActiveModule = null;
                FastTransferRecoverAdapter.CommitForTests = null;
                ItemUseSignalsProvider.ResolveForTests = null;
            }
        }

        // 场景构造器：10×2 网格上 18 个 1×1（id 4001）对碎洞 (3,0)/(7,1)（不成 2×1），
        // 统一排版后成行连续落位、右端留出一块 2×1。
        private static Items V55FragmentedBox()
        {
            var box = V55Page(7, 10, 2);
            for (byte x = 0; x < 10; x++) if (x != 3) V55Jar(box, x, 0, 0, 1, 1, 4001, 1);
            for (byte x = 0; x < 10; x++) if (x != 7) V55Jar(box, x, 1, 0, 1, 1, 4001, 1);
            return box;
        }

        // 源页：10×2 上 2×1 待转移件（id 5001，(0,0)）+ 8 个 1×1（id 4002，(2,0)..(9,0)）。
        private static Items V55FragmentedSource(byte page)
        {
            var p3 = V55Page(page, 10, 2);
            V55Jar(p3, 0, 0, 0, 2, 1, 5001, 1);
            for (byte x = 2; x < 10; x++) V55Jar(p3, x, 0, 0, 1, 1, 4002, 1);
            return p3;
        }

        // 候选页（箱子→玩家）：18 个 1×1 对碎洞（排版后可纳 2×1）。
        private static Items V55FragmentedPage(byte page)
        {
            var p = V55Page(page, 10, 2);
            for (byte x = 0; x < 10; x++) if (x != 3) V55Jar(p, x, 0, 0, 1, 1, 4201, 1);
            for (byte x = 0; x < 10; x++) if (x != 7) V55Jar(p, x, 1, 0, 1, 1, 4201, 1);
            return p;
        }

        private static Items V55FullPage(byte page, int count)
        {
            var p = V55Page(page, 10, 2);
            for (int i = 0; i < count; i++) V55Jar(p, (byte)(i % 10), (byte)(i / 10), 0, 1, 1, (ushort)(8000 + i), 1);
            return p;
        }

        private static Item FindItem(Items items, ushort id)
        {
            for (byte i = 0; i < items.getItemCount(); i++)
                if (items.getItem(i).item.id == id) return items.getItem(i).item;
            return null;
        }

        private static bool ContainsItem(Items items, Item item)
        {
            for (byte i = 0; i < items.getItemCount(); i++)
                if (ReferenceEquals(items.getItem(i).item, item)) return true;
            return false;
        }

        private static bool FoundAt(Items items, ushort id)
        {
            return FindItem(items, id) != null;
        }

        private static bool SnapEqual(List<(byte x, byte y, byte rot, ushort id, byte amount)> a, List<(byte x, byte y, byte rot, ushort id, byte amount)> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i].x != b[i].x || a[i].y != b[i].y || a[i].rot != b[i].rot || a[i].id != b[i].id || a[i].amount != b[i].amount) return false;
            return true;
        }

        /// <summary>除 5001 一件外，其余物品在快照里逐格相同（顺序无关比较）。</summary>
        private static bool SourceIdentityPreserved(List<(byte x, byte y, byte rot, ushort id, byte amount)> before, Items after, ushort movedId)
        {
            var beforeRest = new List<(byte x, byte y, byte rot, ushort id, byte amount)>();
            foreach (var t in before) if (t.id != movedId) beforeRest.Add(t);
            var afterRest = new List<(byte x, byte y, byte rot, ushort id, byte amount)>();
            for (byte i = 0; i < after.getItemCount(); i++)
            {
                var jar = after.getItem(i);
                if (jar.item.id == movedId) continue;
                afterRest.Add((jar.x, jar.y, jar.rot, jar.item.id, jar.item.amount));
            }
            beforeRest.Sort((l, r) => (l.x * 100 + l.y) * 65536 + l.id);
            afterRest.Sort((l, r) => (l.x * 100 + l.y) * 65536 + l.id);
            return SnapEqual(beforeRest, afterRest);
        }

        /// <summary>两计划的逐 Tag 落位一致（含未放置位不出现）。</summary>
        private static bool PlanEqual(IReadOnlyList<PackableItem> a, IReadOnlyList<PackableItem> b)
        {
            int ca = 0, cb = 0;
            foreach (var e in a) if (e != null && e.Placed) ca++;
            foreach (var e in b) if (e != null && e.Placed) cb++;
            if (ca != cb) return false;
            var mapA = new Dictionary<object, (byte x, byte y, byte rot)>();
            foreach (var e in a) if (e != null && e.Placed && e.Tag != null) mapA[e.Tag] = (e.ResultX, e.ResultY, e.ResultRot);
            foreach (var e in b)
            {
                if (e == null || !e.Placed) continue;
                if (e.Tag == null) return false;
                if (!mapA.TryGetValue(e.Tag, out var pos)) return false;
                if (pos.x != e.ResultX || pos.y != e.ResultY || pos.rot != e.ResultRot) return false;
                mapA.Remove(e.Tag);
            }
            return mapA.Count == 0;
        }

        /// <summary>造一个真 Start 的模块（03 harness 的 FeatureBootstrap 形状，role 恒按
        /// ServerRoleProbeForTests 注入；NetService 走真实现+假 authority+无网络拓扑=
        /// 未握手的真实「无会话」形态）。</summary>
        private static InventoryTidyModule V55CreateModule(System.Action<bool, string> check,
            V55SettingsView settings, V55Authority authority)
        {
            var feature = new FeatureId(LitRuntime.FeatureIdValue);
            var module = new InventoryTidyModule(feature);
            module.ScopeDirectoryForTests = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "bue-v5-05-lit-" + Guid.NewGuid().ToString("N"));
            module.FaultContextForTests = () => new LitFaultScopeContext("TestMap", 1);
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var runtime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 1001UL);
            module.NetServiceFactoryForTests = (m, net, book) => new LitTidyNetService(m, net, authority, () => false, book);
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, settings,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, new V55Lifetime(), runtime);
            var started = module.Start(bootstrap);
            if (!started.Started) { check(false, "fixture: module start failed: " + started.DiagnosticId); return null; }
            return module;
        }

        // ─────────────────────────────────────────────────────────────────
        // 组5：事务层离场物品语义（ManualTidyService 手术刀的宿主可跑面）。
        // 守恒 = before-1；错摘/漏摘被抓；页只剩离场件时合法（空页非伪成功）；
        // 快照含离场件 = 回滚重建可完整复原（真回滚写面为具名缺口随 08）。
        // ─────────────────────────────────────────────────────────────────
        private static void V55GroupTransaction(System.Action<bool, string> check)
        {
            // 5a. 正确提交形状（raw 摘走离场件，其余逐格留原格）→ 守恒成立。
            var page = V55Page(3, 10, 2);
            var leave = V55Jar(page, 4, 0, 0, 2, 1, 7001, 5);
            var kept = V55Jar(page, 0, 0, 0, 1, 1, 7002, 1);
            var prep = ManualTidyService.PreparePageLeave(page, 3, leave);
            check(prep.Valid && prep.Leaving != null, "fixture: leave prep 合法");
            page.items.RemoveAt(page.items.IndexOf(leave));
            check(ManualTidyService.VerifyFingerprintConservation(prep),
                "守恒 = before-1（页面 = 原件 - 离场件；Leave 语义与 04 Pending 对称）");
            page.items.Add(leave);

            // 5b. 错摘形状（摘了留下的、留下该摘的）→ 守恒必须抓。
            var page2 = V55Page(3, 10, 2);
            var leave2 = V55Jar(page2, 4, 0, 0, 2, 1, 7101, 1);
            var other2 = V55Jar(page2, 0, 0, 0, 1, 1, 7102, 1);
            var prep2 = ManualTidyService.PreparePageLeave(page2, 3, leave2);
            page2.items.RemoveAt(page2.items.IndexOf(other2));
            check(!ManualTidyService.VerifyFingerprintConservation(prep2),
                "错摘他件 = 守恒失败（假提交形状不放过——与 04 禁吞物同律）");
            page2.items.Add(other2);
            page2.items.RemoveAt(page2.items.IndexOf(leave2));
            check(ManualTidyService.VerifyFingerprintConservation(prep2), "复位后正确形状仍成立");
            kept.x = 0; // 静音未用告警（kept 仅用于形状可读性）

            // 5c. 页上只剩离场件一件 → 合法 prep；提交后空页守恒为真（空非伪成功：
            // 这一件本就该离页，接收侧另有 Pending 守恒钉它入账）。
            var page3 = V55Page(3, 10, 2);
            var only = V55Jar(page3, 0, 0, 0, 2, 1, 7201, 1);
            var prep3 = ManualTidyService.PreparePageLeave(page3, 3, only);
            check(prep3.Valid && prep3.Result.Count == 0, "单件页的离场 prep 合法（空 Result）");
            page3.items.RemoveAt(page3.items.IndexOf(only));
            check(ManualTidyService.VerifyFingerprintConservation(prep3), "提交后空页守恒 = 真（0-1+1 期望配平）");

            // 5d. 两页事务的配对形状：源 prep 只带 Leave、目标 prep 只带 Pending（结构上
            // 不共页——7→7 同页挪位不在恢复域）。
            var src = V55Page(4, 10, 2);
            var leave4 = V55Jar(src, 0, 0, 0, 1, 1, 7301, 1);
            var srcPrep = ManualTidyService.PreparePageLeave(src, 4, leave4);
            check(srcPrep.Valid && srcPrep.Pending == null && srcPrep.Leaving != null,
                "源侧 prep：Leave only（Pending 恒 null）");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组6：不发整理完成 + 线协议 9/10 + 回路双端全链（假权威，零引擎零 Harmony）。
        // 证明：不经 tryAddItem 的路径仍能请求恢复（消息 9 全链到权威恰一次执行）；
        // 恢复与按钮整理共享同一把租约（并发第二笔 Busy）；账本重放回缓存结果帧；
        // 三 kind 结果帧互斥消费（线谎话丢弃）；无 TidyCompleted、无快捷键 ack、
        // 客机失败无提示（原版静默语义）。
        // ─────────────────────────────────────────────────────────────────
        private static void V55GroupWireAndFullChain(System.Action<bool, string> check)
        {
            // 6a. 请求帧逐字段往返 + 形状 fail-closed（sourcePage 域 2..7 之外永不可达）。
            var req = LitTidyWireCodec.BuildFastTransferRequest(42UL, 7u, 3, 2, 5,
                (byte)LitContainerTidyKind.WorldContainer, 0xDEADBEEFCAFEBEEFUL, true);
            check(LitTidyWireCodec.TryReadEnvelope(req, out var msgType, out var body)
                    && msgType == LitTidyWireCodec.MsgRequestFastTransferRecover,
                "线协议：快速转移请求信封可解（新消息号 9）");
            check(LitTidyWireCodec.TryReadFastTransferRequest(body, out var token, out var reqId,
                    out var srcPage, out var srcX, out var srcY, out var kindByte, out var fp, out var desc)
                    && token == 42UL && reqId == 7u && srcPage == 3 && srcX == 2 && srcY == 5
                    && kindByte == (byte)LitContainerTidyKind.WorldContainer && fp == 0xDEADBEEFCAFEBEEFUL && desc,
                "线协议：快速转移请求逐字段往返（源坐标+容器身份声明，无页号身份、无物品伪造面）");
            check(!LitTidyWireCodec.TryReadFastTransferRequest(null, out _, out _, out _, out _, out _, out _, out _, out _),
                "线协议：null body 拒");
            check(!LitTidyWireCodec.TryReadFastTransferRequest(new byte[10], out _, out _, out _, out _, out _, out _, out _, out _),
                "线协议：短包拒");
            var trailing = new byte[body.Length + 1];
            Buffer.BlockCopy(body, 0, trailing, 0, body.Length);
            check(!LitTidyWireCodec.TryReadFastTransferRequest(trailing, out _, out _, out _, out _, out _, out _, out _, out _),
                "线协议：尾随数据拒");
            var badProto = (byte[])body.Clone(); badProto[0] = 2;
            check(!LitTidyWireCodec.TryReadFastTransferRequest(badProto, out _, out _, out _, out _, out _, out _, out _, out _),
                "线协议：协议版本≠3 拒");
            var badToken = (byte[])body.Clone(); Array.Clear(badToken, 1, 8);
            check(!LitTidyWireCodec.TryReadFastTransferRequest(badToken, out _, out _, out _, out _, out _, out _, out _, out _),
                "线协议：token=0 拒");
            var badReqId = (byte[])body.Clone(); Array.Clear(badReqId, 9, 4);
            check(!LitTidyWireCodec.TryReadFastTransferRequest(badReqId, out _, out _, out _, out _, out _, out _, out _, out _),
                "线协议：requestId=0 拒");
            var badKind = (byte[])body.Clone(); badKind[16] = 3;
            check(!LitTidyWireCodec.TryReadFastTransferRequest(badKind, out _, out _, out _, out _, out _, out _, out _, out _),
                "线协议：kind∉{1,2} 拒（与容器请求同一声明域）");
            foreach (byte forbidden in new byte[] { 0, 1, 8, 255 })
            {
                var badPage = (byte[])body.Clone(); badPage[13] = forbidden;
                check(!LitTidyWireCodec.TryReadFastTransferRequest(badPage, out _, out _, out _, out _, out _, out _, out _, out _),
                    "线协议：sourcePage=" + forbidden + " 拒（主副手/AREA/无页永不入框=票面范围的帧级钉）");
            }

            // 6b. 结果帧：恰 [token][reqId][result][reason]，Committed⇔None 互斥律同形 8。
            var resultFrame = LitTidyWireCodec.BuildFastTransferResult(42UL, 7u, TidyCommitResult.Rejected,
                (byte)LitContainerTidyReason.LayoutFailed);
            check(LitTidyWireCodec.TryReadEnvelope(resultFrame, out var rmsg, out var rbody)
                    && rmsg == LitTidyWireCodec.MsgFastTransferRecoverResult,
                "线协议：快速转移结果信封可解（新消息号 10）");
            check(LitTidyWireCodec.TryReadFastTransferResult(rbody, out var rtoken, out var rreq, out var rresult, out var rreason)
                    && rtoken == 42UL && rreq == 7u && rresult == TidyCommitResult.Rejected
                    && rreason == (byte)LitContainerTidyReason.LayoutFailed,
                "线协议：结果帧逐字段往返（复用 03 八类结构化原因，无新枚举）");
            var badResult = (byte[])rbody.Clone(); badResult[12] = 4;
            check(!LitTidyWireCodec.TryReadFastTransferResult(badResult, out _, out _, out _, out _), "线协议：结果字节>3 拒");
            var badReason = (byte[])rbody.Clone(); badReason[13] = 9;
            check(!LitTidyWireCodec.TryReadFastTransferResult(badReason, out _, out _, out _, out _), "线协议：原因码越界拒");
            var lie = LitTidyWireCodec.BuildFastTransferResult(42UL, 9u, TidyCommitResult.Committed, (byte)LitContainerTidyReason.LayoutFailed);
            check(LitTidyWireCodec.TryReadEnvelope(lie, out _, out var lieBody)
                    && !LitTidyWireCodec.TryReadFastTransferResult(lieBody, out _, out _, out _, out _),
                "线协议：Committed+有因 矛盾帧拒收（成功⇔无因）");
            var silent = LitTidyWireCodec.BuildFastTransferResult(42UL, 9u, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.None);
            check(LitTidyWireCodec.TryReadEnvelope(silent, out _, out var silentBody)
                    && !LitTidyWireCodec.TryReadFastTransferResult(silentBody, out _, out _, out _, out _),
                "线协议：拒绝但无因 矛盾帧拒收（无静默失败）");

            // 6c. 客户端 pending 三 kind 互斥（页号不再兼职身份标记）。
            var table = new LitClientPendingTable();
            table.SetPending(1UL, 2UL, 3u, 7, TidyMode.SameType, true, LitClientRequestKind.FastTransfer);
            check(table.TryGetPending(1UL, 2UL, 3u, out var fastEntry)
                    && fastEntry.IsFastTransferRequest && !fastEntry.IsContainerRequest,
                "pending：快速转移页7 请求不被当容器请求（kind 标记优先于挂载位）");
            table.SetPending(1UL, 2UL, 4u, 7, TidyMode.SameType, true, LitClientRequestKind.Container);
            check(table.TryGetPending(1UL, 2UL, 4u, out var contEntry)
                    && contEntry.IsContainerRequest && !contEntry.IsFastTransferRequest,
                "pending：容器请求标记不变（03 行为零变化）");
            table.SetPending(1UL, 2UL, 5u, 3, TidyMode.SameType, true, LitClientRequestKind.Tidy);
            check(table.TryGetPending(1UL, 2UL, 5u, out var tidyEntry)
                    && !tidyEntry.IsContainerRequest && !tidyEntry.IsFastTransferRequest,
                "pending：服装页请求两者皆非（三态互斥闭合）");

            // 6d. 账本 kind 标记 + 租约共享（同一把每玩家租约）。
            var book = new LitServerSessionBook();
            var ledger = new LitRequestLedger();
            var leases = new LitPlayerLeaseGate();
            var gate = new LitAdmissionGate(book, ledger, leases);
            book.TryBeginSession(1001UL, 9UL, out var sessionToken);
            var admitted = gate.TryAdmit(1001UL, 9UL, sessionToken, 1u, out _);
            check(admitted == LitAdmissionGate.AdmissionKind.New, "fixture: 快速转移请求可准入");
            var busyOther = gate.TryAdmit(1001UL, 9UL, sessionToken, 2u, out _);
            check(busyOther == LitAdmissionGate.AdmissionKind.BusyDifferent,
                "租约与服装页/容器整理同一把：在途时第二笔 = BusyDifferent（不并发两笔事务）");
            ledger.MarkFastTransferResult(1001UL, 9UL, sessionToken, 1u,
                LitRequestLedger.RequestState.Committed, TidyCommitResult.Committed, (byte)LitContainerTidyReason.None);
            check(ledger.TryLookup(1001UL, 9UL, sessionToken, 1u, out var cachedEntry)
                    && cachedEntry.IsFastTransferResult && !cachedEntry.IsContainerTidy,
                "账本缓存带快速转移帧形标记（重放绝不回成容器帧/映射帧）");
            leases.Release(1001UL, 1u);

            // 6e. 回路双端全链（真实 LitTidyNetService × 2 + loopback + 假权威）。
            V55RunFullChain(check);
        }

        private static void V55RunFullChain(System.Action<bool, string> check)
        {
            var feature = new FeatureId(LitRuntime.FeatureIdValue);
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var clientRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 1001UL);
            var serverRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, new ContractVersion(2, 0), 2002UL, handshakeInitiator: false);
            var authority = new V55Authority();
            var clientBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var serverBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var serverEvents = new List<TidyCompleted>();
            serverBus.Subscriber(feature).Subscribe<TidyCompleted>(serverEvents.Add);
            InventoryTidyModule.FastTransferPatchInstallerForTests = _ => true;
            InventoryTidyModule.RecoverPatchInstallerForTests = _ => true;
            var clientSettings = new V55SettingsView();
            clientSettings.SetDirection(InventoryTidyModule.DirectionDescendingLabel);
            var clientModule = V55CreateWiredModule(clientBus, clientRuntime, false, authority, clientSettings);
            var serverModule = V55CreateWiredModule(serverBus, serverRuntime, true, authority, new V55SettingsView());
            var clientFrames = new List<byte[]>();
            var serverFrames = new List<byte[]>();
            clientModule.Network.Subscribe(feature, ChannelDirection.FromServer, (s, p) => clientFrames.Add(p));
            serverModule.Network.Subscribe(feature, ChannelDirection.FromClients, (s, p) => serverFrames.Add(p));
            var toasts = new List<string>();
            LitContainerFeedback.ToastSink = toasts.Add;
            var savedLogs = LitRuntime.LogSink;
            var savedErrors = LitRuntime.ErrorLogSink;
            var logs = new List<string>();
            LitRuntime.LogSink = logs.Add;
            LitRuntime.ErrorLogSink = logs.Add; // LogWarning/LogError 走 error 通道（LitRuntime 既有纪律）
            void Pump() { pair.First.Pump(); pair.Second.Pump(); }
            try
            {
                clientRuntime.StartSession(2002UL);
                Pump(); Pump();
                serverModule.Tick(); clientModule.Tick(); Pump();
                check(clientFrames.Count > 0 && clientFrames[0][1] == LitTidyWireCodec.MsgSessionChallenge,
                    "全链：会话 challenge 先行（快速转移与整理共用会话令牌）");
                FastTransferRecoverAdapter.ActiveModule = clientModule;

                // 1. 碎洞未发包 → 消息 9 → 权威恰一次 → 结果帧 10 → 原版静默（无提示、无事件）。
                var dispatched = FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 2, 5, LitContainerTidyKind.WorldContainer, 12345UL));
                check(dispatched == LitTidyRequestResult.Dispatched, "全链：意图请求受理（Dispatched）");
                Pump();
                var outFrame = serverFrames.Find(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgRequestFastTransferRecover);
                check(outFrame != null, "全链：恢复请求以功能私有新消息 9 出线（不借道服装页 2/容器 7）");
                serverModule.Tick(); Pump();
                check(authority.FastCount == 1 && authority.LastFast != null
                        && authority.LastFast.SourcePage == 3 && authority.LastFast.SourceX == 2 && authority.LastFast.SourceY == 5
                        && authority.LastFast.Kind == LitContainerTidyKind.WorldContainer
                        && authority.LastFast.Fingerprint == 12345UL
                        && authority.LastFast.PeerSteamId == 1001UL && authority.LastFast.SortDescending,
                    "全链：权威收到 = 源坐标 + 容器种类/指纹声明 + 请求者（会话身份）+ 方向偏好");
                check(clientFrames.Exists(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgFastTransferRecoverResult),
                    "全链：结果帧 10 回送（账本终态）");
                check(serverEvents.Count == 0, "全链：快速转移恢复不发布 TidyCompleted（不触发整理后压弹）");
                check(!serverFrames.Exists(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgHotkeyFlowAck),
                    "全链：零快捷键 ack（恢复不是按钮整理事务）");
                check(toasts.Count == 0, "全链：成功不弹提示（物品去向可见=原版同步，恢复不当成整理）");

                // 2. 重放命中账本缓存：同 (token,reqId) 再入线 → 权威不再执行、回缓存 10 帧。
                var token = BitConverter.ToUInt64(clientFrames[0], 2);
                var reqId = BitConverter.ToUInt32(outFrame, 1 + 1 + 1 + 8);
                var replay = LitTidyWireCodec.BuildFastTransferRequest(token, reqId, 3, 2, 5,
                    (byte)LitContainerTidyKind.WorldContainer, 12345UL, true);
                clientModule.Network.SendToServer(feature, replay, reliable: true);
                Pump(); serverModule.Tick(); Pump();
                check(authority.FastCount == 1, "全链：重放命中缓存（权威仍恰好一次执行）");
                check(clientFrames.FindAll(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgFastTransferRecoverResult).Count >= 2,
                    "全链：重放收到缓存结果帧重发");

                // 3. 线谎话（pending 活着时）：服务端向快速转移的待确认投两种错 kind 帧
                // → 各按 kind 互斥丢弃；真结果帧随后仍被正常消费（谎话不误清待确认）。
                var serverSideSession = serverRuntime.Sessions.Count > 0 ? serverRuntime.Sessions[0] : null;
                check(serverSideSession != null, "fixture: 服务端会话在册");
                var lieDispatch = FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 1, 1, LitContainerTidyKind.WorldContainer, 44444UL));
                check(lieDispatch == LitTidyRequestResult.Dispatched, "fixture: 谎话用例请求已出、待确认在（服务器未派发）");
                Pump(); // 请求入线（admission 占用租约），但 server.Tick 未跑 → pending 活着
                var lieReqId = BitConverter.ToUInt32(
                    serverFrames.FindLast(p2 => p2.Length > 1 && p2[1] == LitTidyWireCodec.MsgRequestFastTransferRecover), 1 + 1 + 1 + 8);
                var beforeLogs3 = logs.Count;
                serverModule.Network.SendToClient(feature, serverSideSession,
                    LitTidyWireCodec.BuildContainerTidyResult(token, lieReqId, TidyCommitResult.Committed, (byte)LitContainerTidyReason.None), reliable: true);
                serverModule.Network.SendToClient(feature, serverSideSession,
                    LitTidyWireCodec.BuildTidyCommitted(token, lieReqId, TidyCommitResult.Committed, null), reliable: true);
                Pump();
                check(logs.Count >= beforeLogs3 + 2
                        && logs.Exists(l => l.Contains("容器") && l.Contains("FastTransfer") && l.Contains("忽略"))
                        && logs.Exists(l => l.Contains("TidyCommitted") && l.Contains("FastTransfer") && l.Contains("忽略")),
                    "全链：容器结果帧与服装页 TidyCommitted 帧打在活的快速转移待确认上 = 双双按 kind 丢弃留诊断（三 kind 互斥）");
                check(toasts.Count == 0, "全链：谎话帧不产生任何玩家提示");
                var fastCountBeforeLie = authority.FastCount;
                serverModule.Tick(); Pump();
                check(authority.FastCount == fastCountBeforeLie + 1,
                    "全链：谎话未吞请求——权威随后照常恰执行一次");
                check(clientFrames.Exists(p2 => p2.Length > 1 && p2[1] == LitTidyWireCodec.MsgFastTransferRecoverResult),
                    "全链：真结果帧仍被消费（待确认活到真帧到达）");

                // 4. 权威崩溃：Critical + InternalFailure 帧；同 peer 熔断后续请求 = FeatureUnavailable。
                authority.ThrowOnFast = true;
                var crashed = FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 0, 1, LitContainerTidyKind.WorldContainer, 22222UL));
                check(crashed == LitTidyRequestResult.Dispatched, "fixture: 第二次意图请求受理");
                Pump(); serverModule.Tick(); Pump();
                check(clientFrames.Exists(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgFastTransferRecoverResult
                        && p[1 + 1 + 8 + 4 + 1] == (byte)LitContainerTidyReason.InternalFailure),
                    "全链：权威崩溃 = Critical+InternalFailure 回帧（零修改语义的诚实回执）");
                authority.ThrowOnFast = false;
                var after = FastTransferRecoverAdapter.RequestFromIntent(V55Intent(3, 0, 2, LitContainerTidyKind.WorldContainer, 33333UL));
                Pump(); serverModule.Tick(); Pump();
                check(clientFrames.Exists(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgFastTransferRecoverResult
                        && p[1 + 1 + 8 + 4 + 1] == (byte)LitContainerTidyReason.FeatureUnavailable),
                    "全链：peer 熔断在册 → 后续快速转移请求显式 FeatureUnavailable（禁无限重试撞闸）");
                check(after == LitTidyRequestResult.Dispatched,
                    "客机的 per-peer 熔断拒绝发生在权威侧（本机门序仍 Dispatched，不回落伪成功）");
            }
            finally
            {
                LitRuntime.LogSink = savedLogs;
                LitRuntime.ErrorLogSink = savedErrors;
                LitContainerFeedback.ToastSink = null;
                FastTransferRecoverAdapter.ActiveModule = null;
                try { clientModule.Stop(FeatureStopReason.PluginStopping); } catch (Exception) { }
                try { serverModule.Stop(FeatureStopReason.PluginStopping); } catch (Exception) { }
            }
        }

        private static InventoryTidyModule V55CreateWiredModule(
            BetterUnturnedExperience.Core.Events.FeatureEventBus bus,
            BetterUnturnedExperience.Core.Network.BueNetworkRuntime runtime, bool isServer,
            ILitTidyAuthority authority, V55SettingsView settings)
        {
            var feature = new FeatureId(LitRuntime.FeatureIdValue);
            var module = new InventoryTidyModule(feature);
            module.ScopeDirectoryForTests = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "bue-v5-05-chain-" + Guid.NewGuid().ToString("N"));
            module.FaultContextForTests = () => new LitFaultScopeContext("TestMap", 1);
            module.NetServiceFactoryForTests = (m, net, book) => new LitTidyNetService(m, net, authority, () => isServer, book);
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, settings,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, new V55Lifetime(), runtime);
            var started = module.Start(bootstrap);
            if (!started.Started) throw new InvalidOperationException("v5-05 harness: module start failed: " + started.DiagnosticId);
            return module;
        }
    }
}
