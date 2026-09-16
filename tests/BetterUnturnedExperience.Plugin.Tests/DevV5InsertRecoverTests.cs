using System;
using System.Collections.Generic;
using System.Reflection;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lit;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V5-04（V5-T5 → 拾取与合成入包恢复）红测先行组。判据面 = 票面验收条逐字对位：
    /// 仅原版入包失败触发（postfix 只认 __result==false；成功放入不排）；触发面只接线
    /// 已验证的主机拾取/合成两条 RPC（InsertRecoverScope 不在场 = 其它获得路径零恢复）；
    /// 仍放不下 = 原版失败 + 背包逐格未改；恢复成功不发布 TidyCompleted（不当成点了整理、
    /// 不触发压弹/合匣）；功能停 = 注销即不执行（登记=唯一开关，无独立开关、无 Prefix 内
    /// 死开关）；只动玩家五页 2..6（主副手/AREA/容器页排除）；待加入物品必须出现在计划与
    /// 提交结果内（禁吞物），提交失败回滚零修改。
    /// 引擎面（真 Harmony 登记、Items.addItem 真提交、装备尾/快捷键真重绑）为具名接缝缺口
    /// （宿主无引擎，同 DEV-V5-03 口径），实机随 DEV-V5-08；此处全部判据走宿主可跑面：
    /// 真实 Items/Item 网格 + PreparePage 真规划（消费 02 唯一出口）+ 假事务钉计划消费形状。
    /// </summary>
    internal static class DevV5InsertRecoverTests
    {
        internal static void Run(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            // 事务层主线程闸（IsMainThread）读 LitRuntime.MainThreadId；真机由模块
            // EnsureStarted 设置，独立红入口必须显式设置同线程 id。
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
                    var savedModule = InsertRecoverAdapter.ActiveModule;
                    var savedProbe = InsertRecoverAdapter.PendingProbeForTests;
                    var savedCommit = InsertRecoverAdapter.CommitForTests;
                    var savedPages = InsertRecoverAdapter.PagesForTests;
                    var savedInstaller = InventoryTidyModule.RecoverPatchInstallerForTests;
                    var savedRole = LitTidyProductionAuthority.ServerRoleProbeForTests;
                    var savedLabel = ItemUseSignalsProvider.ResolveForTests;
                    var savedHeadless = BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision;
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
                        InsertRecoverAdapter.ActiveModule = savedModule;
                        InsertRecoverAdapter.PendingProbeForTests = savedProbe;
                        InsertRecoverAdapter.CommitForTests = savedCommit;
                        InsertRecoverAdapter.PagesForTests = savedPages;
                        InventoryTidyModule.RecoverPatchInstallerForTests = savedInstaller;
                        LitTidyProductionAuthority.ServerRoleProbeForTests = savedRole;
                        ItemUseSignalsProvider.ResolveForTests = savedLabel;
                        BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision = savedHeadless;
                        InsertRecoverScope.ResetForTests();
                    }
                }

                Group("触发面与接线范围", () => RecoverGroupTriggerAndWiring(Check));
                Group("生命周期登记即开关", () => RecoverGroupLifecycle(Check));
                Group("恢复决策与五页范围", () => RecoverGroupDecision(Check));
                Group("统一排版出口消费", () => RecoverGroupOfficialFirst(Check));
                Group("事务层待加入物品语义", () => RecoverGroupTransaction(Check));
                Group("不发整理完成", () => RecoverGroupNoTidyCompleted(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V5-04 insert-recover collection: ALL GREEN (0 failures) — groups: 触发面与接线范围/生命周期登记即开关/恢复决策与五页范围/统一排版出口消费/事务层待加入物品语义/不发整理完成");
            Console.WriteLine("DEV-V5-04 insert-recover tests: " + (collectAllFailures && reds.Count > 0 ? "FAIL" : "PASS"));
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V5-04 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // ─────────────────────────────────────────────────────────────────
        // 宿主假件（DEV-V5-03 同款构造法：真实 Items/Item/ItemJar 网格，反射直填
        // 尺寸绕开 loadSize/NetReflection；PlayerInventory 用 GetUninitializedObject
        // 的裸壳只装 items[]，方法一个不调——tryAddItemAuto 的真身绝不进宿主路径）。
        // ─────────────────────────────────────────────────────────────────

        private static Items V54Page(byte page, byte width, byte height)
        {
            var items = new Items(page);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(Items).GetField("_width", flags).SetValue(items, width);
            typeof(Items).GetField("_height", flags).SetValue(items, height);
            typeof(Items).GetField("slots", flags).SetValue(items, new bool[width, height]);
            return items;
        }

        private static ItemJar V54Jar(Items items, byte x, byte y, byte rot, byte sx, byte sy, ushort id, byte amount)
        {
            var jar = (ItemJar)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ItemJar));
            jar.x = x; jar.y = y; jar.rot = rot; jar.size_x = sx; jar.size_y = sy;
            SetV54ItemField(jar, new Item(id, amount, 100, new byte[0]));
            items.items.Add(jar);
            return jar;
        }

        private static void SetV54ItemField(ItemJar jar, Item item)
        {
            foreach (var field in typeof(ItemJar).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
            {
                if (field.FieldType == typeof(Item)) { field.SetValue(jar, item); return; }
            }
            throw new InvalidOperationException("no Item field on ItemJar — fixture cannot run");
        }

        // DEV-V5-04 红测纪律（DEV-V5-03 MOUNT_PAGE 教训）：宿主绝不触碰
        // PlayerInventory 类型本身——其静态初始化（NetReflection 接线）在
        // 游戏进程外必抛。场景 = 裸 Items[] 页数组直接驱动 engine-free 核心
        // （InsertRecoverAdapter.TryRecoverPages），owner 恒 null = 引擎尾巴
        // （快捷键/装可用/投影）不进宿主 JIT，其真机面随 08。
        private static List<(byte page, byte x, byte y, byte rot, ushort id)> V54Snapshot(Items[] pages)
        {
            var list = new List<(byte, byte, byte, byte, ushort)>();
            if (pages == null) return list;
            for (byte page = 0; page < pages.Length; page++)
            {
                var items = pages[page];
                if (items == null) continue;
                for (byte i = 0; i < items.getItemCount(); i++)
                {
                    var jar = items.getItem(i);
                    list.Add((page, jar.x, jar.y, jar.rot, jar.item.id));
                }
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────────────
        // 待加入物品描述缝（引擎资产读的宿主替身）+ 方向偏好假件 +
        // 假提交事务（同语义消费计划：jar 直写坐标，pending 以裸壳 jar 入账）。
        // ─────────────────────────────────────────────────────────────────

        private static InsertRecoverAdapter.PendingInfo Info(byte sx, byte sy, bool blocked = false)
        {
            return new InsertRecoverAdapter.PendingInfo { Blocked = blocked, SizeX = sx, SizeY = sy };
        }

        private sealed class V54SettingsView : IScopedFeatureSettings
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

        /// <summary>DEV-V5-04 宿主事务（PageTransactionForTests 同款语义缺口）：
        /// 真提交链的 removeItem/addItem 触 SDG.Unturned.Assets 静态初始化、游戏外
        /// 必抛（DEV-V5-03 已具名），所以「提交成功写面」在宿主不可跑。本假事务保持
        /// 同一语义：逐 prep 消费 Result 坐标直写回 jar；prep.Pending 对应的待加入
        /// 物品以裸壳 ItemJar 入账（生产 = Items.addItem 新建 jar）。把「恢复必须经
        /// 统一排版计划 + 待加入物品必须随提交入账」钉死在宿主；真提交链随 DEV-V5-08
        /// 实机验收（本票具名接缝缺口）。</summary>
        private sealed class V54CommitRecorder
        {
            public int Calls;
            public List<PagePreparation> LastPreps;
            public Dictionary<ItemJar, NewPosition> LastMapping;
            public TidyOperationOutcome Result = TidyOperationOutcome.Committed;

            public TidyOperationOutcome Commit(List<PagePreparation> preps, Dictionary<ItemJar, NewPosition> mapping)
            {
                Calls++;
                LastPreps = preps; LastMapping = mapping;
                if (Result != TidyOperationOutcome.Committed &&
                    (Result == null || Result.Result != TidyCommitResult.Committed)) return Result;
                foreach (var prep in preps)
                {
                    var pendingTag = prep.Pending == null ? null : prep.Pending.Tag;
                    foreach (var p in prep.Result)
                    {
                        if (p == null || !p.Placed) continue;
                        if (pendingTag != null && ReferenceEquals(p.Tag, pendingTag))
                        {
                            // 待加入物品随提交入账（生产语义：items.addItem 新建 jar）。
                            var jar = (ItemJar)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ItemJar));
                            var item = (Item)p.Tag;
                            jar.x = p.ResultX; jar.y = p.ResultY; jar.rot = p.ResultRot;
                            jar.size_x = p.size_x; jar.size_y = p.size_y;
                            SetV54ItemField(jar, item);
                            prep.ItemsInstance.items.Add(jar);
                            continue;
                        }
                        if (p.Tag is ItemJar existing)
                        {
                            existing.x = p.ResultX; existing.y = p.ResultY; existing.rot = p.ResultRot;
                            mapping[existing] = new NewPosition(prep.Page, p.ResultX, p.ResultY, p.ResultRot);
                        }
                    }
                }
                return TidyOperationOutcome.Committed;
            }
        }

        /// <summary>计数直通策略：钉「恢复的计划确实经模块注入的策略出口」（官方
        /// 先行消费锚），排版本身仍委托 02 统一排版。</summary>
        private sealed class V54CountingStrategy : ITidyStrategy
        {
            private readonly TaggedRowBandV1Strategy inner = new TaggedRowBandV1Strategy();
            public int BuildPlanCalls;
            public string LastStrategyId { get { return inner.StrategyId; } }
            public string StrategyId { get { return inner.StrategyId; } }
            public TidyPlan BuildPlan(TidyInput input) { BuildPlanCalls++; return inner.BuildPlan(input); }
        }

        /// <summary>恒拒策略（DEV-V5-03 fixture 同款形）：证明恢复的成败完全由
        /// 计划出口决定，拒绝 = 零修改。</summary>
        private sealed class V54RejectStrategy : ITidyStrategy
        {
            public string StrategyId { get { return "v5-04-reject-fixture"; } }
            public TidyPlan BuildPlan(TidyInput input)
            {
                var placements = new List<PackableItem>();
                foreach (var item in input.Items)
                    placements.Add(new PackableItem { Tag = item.Tag, size_x = item.size_x, size_y = item.size_y, Placed = false });
                return new TidyPlan(StrategyId, placements, false);
            }
        }

        // 常用场景：page2 = 4×2、四枚 1×1 摆成棋盘（自由格 (1,0)/(0,1)/(2,1)/(3,0)
        // 无两格横向或纵向相邻 → 2×1 的原版 tryFindSpace 必败=碎洞），待加入 = id 1001
        // 的 2×1。恢复 = 五页重排 + 该件入 plan + 原子提交，仍合法（6 格 ≤ 8）。
        private static Items[] V54FragmentedPages(out Item pending)
        {
            pending = new Item(1001, 1, 100, new byte[0]);
            var pages = new Items[9];
            var p2 = V54Page(2, 4, 2);
            V54Jar(p2, 0, 0, 0, 1, 1, 701, 1);
            V54Jar(p2, 2, 0, 0, 1, 1, 702, 1);
            V54Jar(p2, 1, 1, 0, 1, 1, 703, 1);
            V54Jar(p2, 3, 1, 0, 1, 1, 704, 1);
            pages[2] = p2;
            return pages;
        }

        private static InventoryTidyModule V54Module(V54SettingsView settings)
        {
            var module = new InventoryTidyModule(new FeatureId(LitRuntime.FeatureIdValue));
            module.AttachSettingsView(settings);
            return module;
        }

        private static V54SettingsView V54DescendingSettings()
        {
            var settings = new V54SettingsView();
            settings.SetDirection(InventoryTidyModule.DirectionDescendingLabel);
            return settings;
        }

        /// <summary>组1 场景的页数组载体（PagesForTests 缝的闭包目标，随组重设）。</summary>
        private static Items[] RecoverV54Pages;

        // ─────────────────────────────────────────────────────────────────
        // 组1：触发面与接线范围（票面「仅原版第一次失败触发 / 成功放入不排 /
        // 只接线已验证的主机拾取/合成 / 不挂拖放、不接 sendDragItem」）。
        // ─────────────────────────────────────────────────────────────────
        private static void RecoverGroupTriggerAndWiring(System.Action<bool, string> check)
        {
            var recorder = new V54CommitRecorder();
            InsertRecoverAdapter.CommitForTests = recorder.Commit;
            InsertRecoverAdapter.PendingProbeForTests = _ => Info(2, 1);

            // 宿主注入页数组（PlayerInventory 裸壳不可造，见 V54Snapshot 注释）。
            InsertRecoverAdapter.PagesForTests = _ => RecoverV54Pages;

            // 1a. 成功放入不排：postfix 见 __result==true 原样返回（碎洞面都摆好，
            //     唯一能挡它的只有成功本身）。owner=null：原版成功路径零介入。
            Item pending;
            RecoverV54Pages = V54FragmentedPages(out pending);
            var inv = RecoverV54Pages;
            var before = V54Snapshot(inv);
            InsertRecoverAdapter.ActiveModule = V54Module(V54DescendingSettings());
            InsertRecoverScope.Enter();
            var okResult = true;
            InsertRecoverAutoAddPatch.Postfix(null, pending, false, ref okResult);
            check(okResult && recorder.Calls == 0 && V54Snapshot(inv).Count == before.Count,
                "触发面：原版成功放入 = 零介入（成功放入不排，格子一件不动）");
            InsertRecoverScope.Exit();

            // 1b. 仅已验证上下文：原版失败但 scope 关（其它获得路径共用 tryAddItemAuto
            //     的类型事实）→ 拒绝=未接线，零修改。
            InsertRecoverAdapter.ActiveModule = V54Module(V54DescendingSettings());
            var outScope = InsertRecoverAdapter.TryRecoverPages(inv, null, pending, false);
            check(!outScope.Recovered && outScope.Refusal == InsertRecoverAdapter.RefusalOutOfScope && recorder.Calls == 0,
                "接线范围：非拾取/合成上下文的原版失败不恢复（其它获得路径不共用汇聚点自动享受）");
            check(V54Snapshot(inv).Count == before.Count, "接线范围：越界拒绝零修改");

            // 1c. 已验证上下文 + 原版失败恰好一次恢复：__result 翻 true（原版调用方
            //     按成功入包继续），待加入物品已入账。
            InsertRecoverScope.Enter();
            var failResult = false;
            InsertRecoverAutoAddPatch.Postfix(null, pending, false, ref failResult);
            InsertRecoverScope.Exit();
            check(failResult, "触发面：恢复提交成功 = 原版失败翻为成功（拾取销毁地面物/合成不掉回的既有语义）");
            check(recorder.Calls == 1, "触发面：一次原版失败恰好一次恢复（不逐页反复重排）");
            var landed = false;
            foreach (var tuple in V54Snapshot(inv))
                if (tuple.id == 1001 && tuple.page == 2) landed = true;
            check(landed, "触发面：恢复成功=待加入物品随事务入到玩家页（不是报了成功没放入）");
            InsertRecoverAdapter.ActiveModule = null;

            // 1d. 补丁面三处逐字钉死（R2：绑定唯一事实源=InsertRecoverBinder，
            //     宿主按同一解析器核实——拾取开合/合成开合/失败行为位；没有第四条
            //     目标，sendDragItem/拖放预览/Items 类型页=禁区反钉）。
            var surface = InsertRecoverBinder.PatchSurface;
            check(surface.Count == 3
                    && surface[0] == typeof(InsertRecoverPickupScopePatch)
                    && surface[1] == typeof(InsertRecoverCraftScopePatch)
                    && surface[2] == typeof(InsertRecoverAutoAddPatch),
                "补丁面恰三条且安装序固定：拾取开合→合成开合→行为位（无第四条误触发面）");
            var take = InsertRecoverBinder.ResolveTarget(typeof(InsertRecoverPickupScopePatch)) as MethodInfo;
            check(take != null && take.DeclaringType == typeof(ItemManager) && take.Name == "ReceiveTakeItemRequest"
                    && take.IsStatic && take.GetParameters().Length == 8
                    && take.GetParameters()[0].ParameterType.IsByRef
                    && take.GetParameters()[0].ParameterType.GetElementType() == typeof(ServerInvocationContext)
                    && take.GetParameters()[3].ParameterType == typeof(uint),
                "接线范围：拾取开合器绑到主机拾取 RPC 真身（SERVERSIDE 执行侧；in-上下文按引用形状核实——R2 实证 byref）");
            var craftTarget = InsertRecoverBinder.ResolveTarget(typeof(InsertRecoverCraftScopePatch)) as MethodInfo;
            check(craftTarget != null && craftTarget.DeclaringType == typeof(PlayerCrafting) && craftTarget.Name == "ReceiveCraft"
                    && craftTarget.GetParameters().Length == 4
                    && craftTarget.GetParameters()[1].ParameterType == typeof(Guid),
                "接线范围：合成开合器绑到 GUID 重载（ONLY_FROM_OWNER 执行侧；[Obsolete] ushort 转发 stub 被显式排除——R1-Spec 追问后的精确绑定）");
            var legacyCraft = 0;
            foreach (var m in typeof(PlayerCrafting).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public))
                if (m.Name == "ReceiveCraft") legacyCraft++;
            check(legacyCraft == 2,
                "接线范围：ReceiveCraft 确有两个重载（名绑定在此必歧义——显式 binder 的意义所在）");
            var behavior = InsertRecoverBinder.ResolveTarget(typeof(InsertRecoverAutoAddPatch)) as MethodInfo;
            check(behavior != null && behavior.DeclaringType == typeof(PlayerInventory) && behavior.Name == "tryAddItemAuto"
                    && behavior.ReturnType == typeof(bool) && behavior.GetParameters().Length == 5
                    && behavior.GetParameters()[0].ParameterType == typeof(Item),
                "接线范围：行为位绑到 tryAddItemAuto(Item,4×bool) 失败返回面（拾取自动入包与合成 forceAddItem 的共同委托终点）");
            check(InsertRecoverBinder.ResolveTarget(typeof(DevV5InsertRecoverTests)) == null,
                "接线范围：未知补丁类解析为 null（面外类型永不成为触发点，fail-closed）");

            // 1e. scope 计数器 = 纯括号语义（finalizer 平衡闸的宿主替身）：嵌套、
            //     空出不清负、复位。
            InsertRecoverScope.ResetForTests();
            check(!InsertRecoverScope.IsOpen && InsertRecoverScope.Depth == 0, "scope：初始关闭");
            InsertRecoverScope.Enter(); InsertRecoverScope.Enter();
            check(InsertRecoverScope.IsOpen && InsertRecoverScope.Depth == 2, "scope：嵌套计数");
            InsertRecoverScope.Exit(); InsertRecoverScope.Exit(); InsertRecoverScope.Exit();
            check(InsertRecoverScope.Depth == 0, "scope：退出配对不产生负深度（异常路径 finalizer 同形）");

            // 1f. 委托链入口在真实程序集上在册 + R2 实证留钉：in 首参对 plain
            //     typeof 永不相配（这正是 binder 用 byref-aware 扫描的原因）。
            var auto2Arg = typeof(PlayerInventory).GetMethod("tryAddItem",
                new[] { typeof(Item), typeof(bool) });
            check(auto2Arg != null && auto2Arg.ReturnType == typeof(bool) && auto2Arg.IsPublic,
                "目标真实：拾取自动入包的 (Item,bool) 入口重载在册（其委托终点 tryAddItemAuto=行为位，U3-SDK 逐字链见 binder 注释；端到端实机面随 08）");
            var craftViaPlainSic = typeof(PlayerCrafting).GetMethod("ReceiveCraft",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
                null,
                new[] { typeof(ServerInvocationContext), typeof(Guid), typeof(byte), typeof(bool) },
                null);
            check(craftViaPlainSic == null,
                "绑定实证（R2）：in-ServerInvocationContext 参数是 byref 形状，plain typeof 绑不到——名绑定双重载必歧义，binder 扫描是唯一正确形状");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组2：生命周期登记即开关（Q3：跟随背包整理生命周期，无独立开关、
        // 禁止 Prefix 内死开关；优先 Start 登记/Stop 注销；U3DS 是权威面也
        // 必须登记——恢复要真做，不随 UI 的 headless 不武装决策被砍掉）。
        // ─────────────────────────────────────────────────────────────────
        private static void RecoverGroupLifecycle(System.Action<bool, string> check)
        {
            var recorder = new V54CommitRecorder();
            InsertRecoverAdapter.CommitForTests = recorder.Commit;
            InsertRecoverAdapter.PendingProbeForTests = _ => Info(2, 1);

            var installed = new List<Type>();
            InventoryTidyModule.RecoverPatchInstallerForTests = type => { installed.Add(type); return true; };

            // 2a. Start（EnsureStarted）登记三处补丁并接上行为位。
            var settings = V54DescendingSettings();
            var module = V54Module(settings);
            module.EnsureStarted();
            check(module.RecoverPatchesInstalled, "生命周期：Start=恢复补丁登记（RecoverPatchesInstalled）");
            check(ReferenceEquals(InsertRecoverAdapter.ActiveModule, module), "生命周期：登记后行为位接上本代际模块");
            check(installed.Count == 3
                    && installed[0] == typeof(InsertRecoverPickupScopePatch)
                    && installed[1] == typeof(InsertRecoverCraftScopePatch)
                    && installed[2] == typeof(InsertRecoverAutoAddPatch),
                "生命周期：恰好登记三条且为 拾取开合→合成开合→行为位（无遗漏无多余）");

            // 2b. Stop = 注销：登记面清空后，scope 开 + 原版失败也不再执行（路径不
            //     执行由注销保证，不是靠补丁体内 if 短路——见 2d 反射钉）。
            module.Stop(FeatureStopReason.UserDisabled);
            check(!module.RecoverPatchesInstalled && InsertRecoverAdapter.ActiveModule == null,
                "生命周期：Stop=补丁注销（ActiveModule 清空）");
            Item pending;
            var inv = V54FragmentedPages(out pending);
            var before = V54Snapshot(inv);
            InsertRecoverScope.Enter();
            var stopped = InsertRecoverAdapter.TryRecoverPages(inv, null, pending, false);
            InsertRecoverScope.Exit();
            check(!stopped.Recovered && stopped.Refusal == InsertRecoverAdapter.RefusalNotRunning
                    && recorder.Calls == 0 && V54Snapshot(inv).Count == before.Count,
                "功能停=路径不执行：注销后的失败入包完全回到原版（零修改零事务）");

            // 2c. 换代际重登记：再 Start 一个新代际 = 重新登记并接管。
            var second = V54Module(settings);
            second.EnsureStarted();
            check(ReferenceEquals(InsertRecoverAdapter.ActiveModule, second) && second.RecoverPatchesInstalled,
                "生命周期：再启用换代际重登记（新代际接管，旧代际不复用）");
            second.Stop(FeatureStopReason.UserDisabled);
            check(InsertRecoverAdapter.ActiveModule == null, "生命周期：代际停止即交还（无悬挂登记）");

            // 2d. 无独立开关/无死开关：恢复面类型不存在任何 Enabled/Disabled
            //     成员（开关=生命周期登记本身，V1.4.1 审计口径）。
            var switchTypes = new[]
            {
                typeof(InsertRecoverAdapter), typeof(InsertRecoverScope),
                typeof(InsertRecoverPickupScopePatch), typeof(InsertRecoverCraftScopePatch),
                typeof(InsertRecoverAutoAddPatch),
            };
            foreach (var type in switchTypes)
            {
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                    check(field.Name.IndexOf("enabled", StringComparison.OrdinalIgnoreCase) < 0 &&
                          !field.Name.Equals("Disabled", StringComparison.OrdinalIgnoreCase),
                        "无独立开关：字段面不含 Enabled/Disabled（" + type.Name + "." + field.Name + "）");
                foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                    check(prop.Name.IndexOf("enabled", StringComparison.OrdinalIgnoreCase) < 0 &&
                          !prop.Name.Equals("Disabled", StringComparison.OrdinalIgnoreCase),
                        "无独立开关：属性面不含 Enabled/Disabled（" + type.Name + "." + prop.Name + "）");
            }

            // 2e. 登记失败 = 半装整体撤销（与既有 UI 安装失败同纪律：不存在只装
            //     一半的恢复面）。
            var half = 0;
            InventoryTidyModule.RecoverPatchInstallerForTests = type => { half++; return half < 3; };
            var broken = V54Module(settings);
            broken.EnsureStarted();
            check(!broken.RecoverPatchesInstalled && InsertRecoverAdapter.ActiveModule == null,
                "登记失败=整体撤销：三处未全装成则恢复面不可用（无半装触发面）");
            check(broken.RecoverStartGateDiagnostics.IndexOf("recover-patch-install-failed", StringComparison.Ordinal) >= 0,
                "登记失败留结构化诊断（决策面可观察，不静默）");
            InventoryTidyModule.RecoverPatchInstallerForTests = null;

            // 2f. U3DS headless：画面补丁不武装，但权威恢复必须登记（story 25 /
            //     T1 Headless 裁决）；既有 headless 诊断口径不变。
            installed.Clear();
            InventoryTidyModule.RecoverPatchInstallerForTests = type => { installed.Add(type); return true; };
            BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision = true;
            var headless = V54Module(settings);
            headless.EnsureStarted();
            BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision = false;
            check(installed.Count == 3 && headless.RecoverPatchesInstalled,
                "headless：U3DS 仍登记入包恢复（权威行为不随 UI 决策被砍）");
            check(!headless.PatchesInstalled && headless.StartGateDiagnostics == "headless-ui-not-armed",
                "headless：整理按钮补丁仍不武装（既有九态诊断口径不变）");
            headless.Stop(FeatureStopReason.UserDisabled);
        }

        // ─────────────────────────────────────────────────────────────────
        // 组3：恢复决策与五页范围（含待加入物品→再试放入；仍塞不进=原版失败+
        // 背包不动；主副手/容器/AREA 页排除；门禁逐项=零修改）。
        // ─────────────────────────────────────────────────────────────────
        private static void RecoverGroupDecision(System.Action<bool, string> check)
        {
            var recorder = new V54CommitRecorder();
            InsertRecoverAdapter.CommitForTests = recorder.Commit;
            InsertRecoverAdapter.PendingProbeForTests = _ => Info(2, 1);

            // 3a. 碎洞成功全形：恢复=五页重排+该件入 plan+原子提交；候选=活动页 2。
            var module = V54Module(V54DescendingSettings());
            InsertRecoverAdapter.ActiveModule = module;
            Item pending;
            var inv = V54FragmentedPages(out pending);
            InsertRecoverScope.Enter();
            var a = InsertRecoverAdapter.TryRecoverPages(inv, null, pending, false);
            InsertRecoverScope.Exit();
            check(a.Recovered && a.PendingPage == 2 && recorder.Calls == 1,
                "决策：碎洞失败→五页重排（含该件）成功→恢复翻为成功");
            var preps = recorder.LastPreps;
            check(preps.Count == 1 && preps[0].Page == 2,
                "决策：提交面只含活动页 2（无活动页不进事务）");
            check(preps[0].Pending != null && ReferenceEquals(preps[0].Pending.Tag, pending),
                "决策：待加入物品绑定在提交计划内（Tag=该 Item 本体，不是影子复制）");
            var p2 = inv[2];
            check(p2.getItemCount() == 5, "决策：提交后页 2 = 原四件 + 待加入一件（不吞物不丢物）");
            var cells = new HashSet<(byte, byte)>();
            bool legal = true, hasPending = false;
            for (byte i = 0; i < p2.getItemCount(); i++)
            {
                var jar = p2.getItem(i);
                if (jar.x + jar.size_x > 4 || jar.y + jar.size_y > 2) legal = false;
                for (var dx = 0; dx < jar.size_x; dx++)
                    for (var dy = 0; dy < jar.size_y; dy++)
                        if (!cells.Add(((byte)(jar.x + dx), (byte)(jar.y + dy)))) legal = false;
                if (jar.item.id == 1001) hasPending = true;
            }
            check(legal && hasPending, "决策：提交格合法（不越界/不重叠/该件在场）——恢复成功⇔放入成功");

            // 3b. 仍塞不进 = 原版失败 + 背包逐格未改：页 2 = 2×1 全占（无洞），其余页不活动。
            var full = new Items[9];
            var fp = V54Page(2, 2, 1);
            V54Jar(fp, 0, 0, 0, 1, 1, 701, 1);
            V54Jar(fp, 1, 0, 0, 1, 1, 702, 1);
            full[2] = fp;
            var fullInv = full;
            var fullBefore = V54Snapshot(fullInv);
            var callsBefore = recorder.Calls;
            InsertRecoverScope.Enter();
            var b = InsertRecoverAdapter.TryRecoverPages(fullInv, null, new Item(1001, 1, 100, new byte[0]), false);
            InsertRecoverScope.Exit();
            check(!b.Recovered && b.Refusal == InsertRecoverAdapter.RefusalCannotFit,
                "决策：重排后仍放不下 = 明确失败原因（cannot-fit），不半提交");
            check(recorder.Calls == callsBefore && V54Snapshot(fullInv).Count == fullBefore.Count,
                "决策：放不下零修改（背包不动、不触事务；物品留原处由原版失败语义承担）");

            // 3c. 候选页按页升序复现原版入包顺序（不升格全身挑选）：页 2 放不下、
            //     页 4 碎洞可排下 → 落页 4，两页同事务。
            var pages = new Items[7];
            var tight = V54Page(2, 2, 1);
            V54Jar(tight, 0, 0, 0, 1, 1, 711, 1);
            V54Jar(tight, 1, 0, 0, 1, 1, 712, 1);
            pages[2] = tight;
            var band = V54Page(4, 4, 2);
            V54Jar(band, 0, 0, 0, 1, 1, 721, 1);
            V54Jar(band, 2, 0, 0, 1, 1, 722, 1);
            V54Jar(band, 1, 1, 0, 1, 1, 723, 1);
            V54Jar(band, 3, 1, 0, 1, 1, 724, 1);
            pages[4] = band;
            for (byte pg = 3; pg <= 6; pg++) if (pages[pg] == null) pages[pg] = V54Page(pg, 0, 0);
            var orderInv = pages;
            InsertRecoverScope.Enter();
            var c = InsertRecoverAdapter.TryRecoverPages(orderInv, null, new Item(1001, 1, 100, new byte[0]), false);
            InsertRecoverScope.Exit();
            check(c.Recovered && c.PendingPage == 4, "决策：候选按页升序（页 2 放不下→页 4；同原版 2..6 循环的落点次序）");
            var listed = new List<byte>();
            foreach (var prep in recorder.LastPreps) listed.Add(prep.Page);
            check(listed.Contains(2) && listed.Contains(4), "决策：五页随同一事务（不放该件的活动页也重排——「连同这五页交给统一排版」）");

            // 3d. 主副手/容器/AREA 页排除：页 2 满、0/1/7/8 有大片空位 → 仍 cannot-fit，
            //     且这些页零触碰。规划器白盒钉页集合 ⊆ 2..6。
            var excl = new Items[9];
            excl[0] = V54Page(0, 2, 2);
            excl[1] = V54Page(1, 2, 2);
            var full2 = V54Page(2, 2, 1);
            V54Jar(full2, 0, 0, 0, 1, 1, 731, 1);
            V54Jar(full2, 1, 0, 0, 1, 1, 732, 1);
            excl[2] = full2;
            for (byte pg = 3; pg <= 6; pg++) excl[pg] = V54Page(pg, 0, 0);
            excl[7] = V54Page(7, 8, 8);
            excl[8] = V54Page(8, 8, 8);
            var exclInv = excl;
            var exclBefore = V54Snapshot(exclInv);
            var exclCalls = recorder.Calls;
            InsertRecoverScope.Enter();
            var d = InsertRecoverAdapter.TryRecoverPages(exclInv, null, new Item(1001, 1, 100, new byte[0]), false);
            InsertRecoverScope.Exit();
            check(!d.Recovered && d.Refusal == InsertRecoverAdapter.RefusalCannotFit
                    && recorder.Calls == exclCalls,
                "范围：STORAGE/AREA/装备槽的空位不是恢复的落点（只玩家五页）");
            check(V54Snapshot(exclInv).Count == exclBefore.Count, "范围：全拒时所有页逐格未动");
            List<PagePreparation> prepsOut; byte candidate; string refusal;
            var built = InsertRecoverPlanner.TryBuild(exclInv, new Item(1001, 1, 100, new byte[0]), Info(2, 1),
                true, TidyMode.SameType, new TaggedRowBandV1Strategy(), out prepsOut, out candidate, out refusal);
            check(!built, "范围：规划器对该局面给不出候选（页 0/1/7/8 不参与）");
            var ok2 = InsertRecoverPlanner.TryBuild(V54FragmentedPages(out pending), pending, Info(2, 1),
                true, TidyMode.SameType, new TaggedRowBandV1Strategy(), out prepsOut, out candidate, out refusal);
            check(ok2 && prepsOut.TrueForAll(p => p.Page >= 2 && p.Page <= 6),
                "范围：规划器产出的页集合 ⊆ 玩家五页 2..6");

            // 3e. 门禁逐项 = 零修改（描述缝拒绝 / 熔断开 / 偏好不可读 / 无活动页）。
            var gateCalls = recorder.Calls;
            InsertRecoverScope.Enter();
            InsertRecoverAdapter.PendingProbeForTests = _ => null;
            var g1 = InsertRecoverAdapter.TryRecoverPages(inv, null, new Item(1002, 1, 100, new byte[0]), false);
            InsertRecoverAdapter.PendingProbeForTests = _ => Info(2, 1, blocked: true);
            var g2 = InsertRecoverAdapter.TryRecoverPages(inv, null, new Item(1002, 1, 100, new byte[0]), false);
            InsertRecoverAdapter.PendingProbeForTests = _ => Info(2, 1);
            var nullItem = InsertRecoverAdapter.TryRecoverPages(inv, null, null, false);
            var circuit = V54Module(V54DescendingSettings());
            circuit.FaultGate.Open("v504-red-fault", false);
            InsertRecoverAdapter.ActiveModule = circuit;
            var gf = InsertRecoverAdapter.TryRecoverPages(inv, null, pending, false);
            circuit.FaultGate.Reset();
            var noPref = V54Module(new V54SettingsView()); // 无 direction 条目
            InsertRecoverAdapter.ActiveModule = noPref;
            var gp = InsertRecoverAdapter.TryRecoverPages(inv, null, pending, false);
            noPref.AttachSettingsView(null);
            var gpNull = InsertRecoverAdapter.TryRecoverPages(inv, null, pending, false);
            var emptyPages = new Items[9];
            InsertRecoverAdapter.ActiveModule = V54Module(V54DescendingSettings());
            var gn = InsertRecoverAdapter.TryRecoverPages(emptyPages, null, pending, false);
            InsertRecoverScope.Exit();
            check(g1.Refusal == InsertRecoverAdapter.RefusalInvalidItem
                    && g2.Refusal == InsertRecoverAdapter.RefusalInvalidItem
                    && nullItem.Refusal == InsertRecoverAdapter.RefusalInvalidItem,
                "门禁：item=null/asset 不可判定/isPro 同闸（原版门禁照抄，恢复从不绕行）");
            check(gf.Refusal == InsertRecoverAdapter.RefusalFaultCircuit,
                "门禁：熔断打开=回原版（与按钮整理同一熔断，不另造闸）");
            check(gp.Refusal == InsertRecoverAdapter.RefusalPreference && gpNull.Refusal == InsertRecoverAdapter.RefusalPreference,
                "门禁：已保存方向偏好不可读=诚实拒绝（Q59 同规：不发明默认）");
            check(gn.Refusal == InsertRecoverAdapter.RefusalNoActivePages,
                "门禁：身上五页全不活动=无处恢复（不伪报、不碰容器/AREA 页）");
            check(recorder.Calls == gateCalls, "门禁：六项拒绝全部未触事务（零修改）");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组4：统一排版出口消费（票面「真实拾取/合成失败路径经 02 计划」的宿主钉）。
        // ─────────────────────────────────────────────────────────────────
        private static void RecoverGroupOfficialFirst(System.Action<bool, string> check)
        {
            var recorder = new V54CommitRecorder();
            InsertRecoverAdapter.CommitForTests = recorder.Commit;
            InsertRecoverAdapter.PendingProbeForTests = _ => Info(2, 1);

            // 4a. 生产接线默认策略 = 02 唯一内置 adapter（票的模块不另造排版）。
            var wiringModule = V54Module(V54DescendingSettings());
            check(wiringModule.Strategy != null && wiringModule.Strategy.StrategyId == TaggedRowBandLayout.LayoutId,
                "官方先行消费：恢复消费的模块策略默认就是 tagged-row-band-v1 统一排版");

            // 4b. 计划确实经模块策略出口逐页调用（计数直通策略），产出仍是真排版。
            var counting = new V54CountingStrategy();
            var countingModule = V54Module(V54DescendingSettings());
            countingModule.Strategy = counting;
            InsertRecoverAdapter.ActiveModule = countingModule;
            Item pending;
            var inv = V54FragmentedPages(out pending);
            InsertRecoverScope.Enter();
            var a = InsertRecoverAdapter.TryRecoverPages(inv, null, pending, false);
            InsertRecoverScope.Exit();
            check(a.Recovered && counting.BuildPlanCalls >= 1
                    && counting.LastStrategyId == TaggedRowBandLayout.LayoutId,
                "官方先行消费：恢复的每一次排版都走模块注入的策略出口（不复制算法、不旁路求解）");

            // 4c. 恢复成功后的格子 = 02 对同一输入（原页物品+待加入物品）确定计划的
            //     逐格兑现（消费形状与 03 组4 同锚）。
            var expected = new List<PackableItem>();
            var pageItems = new Items(2);
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(Items).GetField("_width", flags).SetValue(pageItems, (byte)4);
                typeof(Items).GetField("_height", flags).SetValue(pageItems, (byte)2);
                typeof(Items).GetField("slots", flags).SetValue(pageItems, new bool[4, 2]);
            }
            V54Jar(pageItems, 0, 0, 0, 1, 1, 701, 1);
            V54Jar(pageItems, 2, 0, 0, 1, 1, 702, 1);
            V54Jar(pageItems, 1, 1, 0, 1, 1, 703, 1);
            V54Jar(pageItems, 3, 1, 0, 1, 1, 704, 1);
            for (byte i = 0; i < pageItems.getItemCount(); i++)
            {
                var jar = pageItems.getItem(i);
                expected.Add(new PackableItem
                {
                    Tag = jar, size_x = jar.size_x, size_y = jar.size_y,
                    GroupKey = jar.item.id, StableOrder = i,
                    OriginalX = jar.x, OriginalY = jar.y, OriginalRot = jar.rot, PreferredRotation = jar.rot,
                    Label = ItemUseSignalsProvider.ResolveFor(jar.item),
                });
            }
            expected.Add(new PackableItem
            {
                Tag = pending, size_x = 2, size_y = 1,
                GroupKey = pending.id, StableOrder = 4,
                OriginalX = 0, OriginalY = 0, OriginalRot = 0, PreferredRotation = 0,
                Label = ItemUseSignalsProvider.ResolveFor(pending),
            });
            var plan = new TaggedRowBandV1Strategy().BuildPlan(new TidyInput(4, 2, true, TidyMode.SameType, expected));
            check(plan.AllPlaced, "官方先行消费：期望计划本身合法（同输入统一排版全部放置）");
            var landedModule = V54Module(V54DescendingSettings());
            InsertRecoverAdapter.ActiveModule = landedModule;
            var landedInv = V54FragmentedPages(out pending);
            InsertRecoverScope.Enter();
            var landed = InsertRecoverAdapter.TryRecoverPages(landedInv, null, pending, false);
            InsertRecoverScope.Exit();
            check(landed.Recovered, "官方先行消费：落点场景恢复成功");
            // 对照键=物品 id（本场景每 id 恰一件；Tag 引用跨夹具不可比，id 是值面）。
            var actualById = new Dictionary<ushort, (byte x, byte y, byte rot)>();
            var landedPage = landedInv[2];
            for (byte i = 0; i < landedPage.getItemCount(); i++)
            {
                var jar = landedPage.getItem(i);
                actualById[jar.item.id] = (jar.x, jar.y, jar.rot);
            }
            var geometryOk = true;
            foreach (var entry in plan.Placements)
            {
                if (!entry.Placed) { geometryOk = false; continue; }
                ushort id = entry.Tag is ItemJar taggedJar ? taggedJar.item.id : ((Item)entry.Tag).id;
                var where = default((byte x, byte y, byte rot));
                if (!actualById.TryGetValue(id, out where)) { geometryOk = false; break; }
                if (where.x != entry.ResultX || where.y != entry.ResultY || where.rot != entry.ResultRot) geometryOk = false;
            }
            check(geometryOk, "官方先行消费：提交落格=同一输入再算一遍的 tagged-row-band-v1 计划（逐格对照）");

            // 4d. 分类器接缝消费：待加入物品带标签进计划（不是恢复面自造分类）。
            ItemUseSignalsProvider.ResolveForTests = item => item.id == 1001 ? PlayerUseLabel.Medical : (PlayerUseLabel?)null;
            var labeledModule = V54Module(V54DescendingSettings());
            InsertRecoverAdapter.ActiveModule = labeledModule;
            var labeledInv = V54FragmentedPages(out pending);
            InsertRecoverScope.Enter();
            var labeled = InsertRecoverAdapter.TryRecoverPages(labeledInv, null, pending, false);
            InsertRecoverScope.Exit();
            PlayerUseLabel pendingLabel = PlayerUseLabel.Other;
            if (labeled.Recovered)
                foreach (var entry in recorder.LastPreps)
                    if (entry.Pending != null) pendingLabel = FindV54Label(entry, pending);
            check(labeled.Recovered && pendingLabel == PlayerUseLabel.Medical,
                "官方先行消费：待加入物品的标签来自 02 分类器接缝（Medical），随统一排版分段");

            // 4e. 恒拒策略 = 计划出口直接决定恢复成败：拒绝 → cannot-fit + 零修改。
            var rejecting = V54Module(V54DescendingSettings());
            rejecting.Strategy = new V54RejectStrategy();
            InsertRecoverAdapter.ActiveModule = rejecting;
            var rejectInv = V54FragmentedPages(out pending);
            var rejectBefore = V54Snapshot(rejectInv);
            var rejectCalls = recorder.Calls;
            InsertRecoverScope.Enter();
            var rejected = InsertRecoverAdapter.TryRecoverPages(rejectInv, null, pending, false);
            InsertRecoverScope.Exit();
            check(!rejected.Recovered && rejected.Refusal == InsertRecoverAdapter.RefusalCannotFit
                    && recorder.Calls == rejectCalls && V54Snapshot(rejectInv).Count == rejectBefore.Count,
                "官方先行消费：计划出口说不 = 原版失败 + 零修改（成败判定与排版同缝，不各排各的）");
        }

        private static PlayerUseLabel FindV54Label(PagePreparation prep, Item pending)
        {
            foreach (var entry in prep.Result)
                if (entry != null && ReferenceEquals(entry.Tag, pending)) return entry.Label;
            return PlayerUseLabel.Other;
        }

        // ─────────────────────────────────────────────────────────────────
        // 组5：事务层「待加入物品」语义（禁吞物=静态验证必须要求该件在计划内
        // 放置；失败=零修改；按钮路径（pending=null）逐字节不变）。
        // ─────────────────────────────────────────────────────────────────
        private static void RecoverGroupTransaction(System.Action<bool, string> check)
        {
            var strategy = new TaggedRowBandV1Strategy();

            // 5a. 碎洞页 + pending：PreparePage(+pending) 出合法计划且该件在场。
            Item pending;
            var inv = V54FragmentedPages(out pending);
            var packPending = new PackableItem
            {
                Tag = pending, size_x = 2, size_y = 1, GroupKey = pending.id,
                StableOrder = 4, Label = ItemUseSignalsProvider.ResolveFor(pending),
            };
            var prep = ManualTidyService.PreparePage(inv[2], 2, true, TidyMode.SameType, strategy, packPending);
            check(prep.Valid && prep.Pending != null, "事务层：碎洞页对（原物+该件）给出合法计划（pending 绑定在 prep 上）");
            var placedPending = 0;
            foreach (var entry in prep.Result)
                if (entry != null && ReferenceEquals(entry.Tag, pending)) { placedPending++; check(entry.Placed, "事务层：合法计划中该件必须已落格"); }
            check(placedPending == 1, "事务层：该件在计划中恰一次（不重复不落空）");
            check(inv[2].getItemCount() == 4, "事务层：Prepare 零副作用（规划不写库存）");

            // 5b. 放不下 → Prepare 失败（不半提交）。
            var full = V54Page(2, 2, 1);
            V54Jar(full, 0, 0, 0, 1, 1, 701, 1);
            V54Jar(full, 1, 0, 0, 1, 1, 702, 1);
            var fullPending = new PackableItem { Tag = new Item(1001, 1, 100, new byte[0]), size_x = 2, size_y = 1, GroupKey = 1001, StableOrder = 2, Label = PlayerUseLabel.Other };
            var fullPrep = ManualTidyService.PreparePage(full, 2, true, TidyMode.SameType, strategy, fullPending);
            check(!fullPrep.Valid, "事务层：放不下的 (页+该件) 组合 = Prepare 直接失败（无半成品计划）");

            // 5c. 空格 + pending：合法成功（恢复进空页也走同一条验证）。
            var empty = V54Page(2, 4, 2);
            var emptyPending = new PackableItem { Tag = new Item(1001, 1, 100, new byte[0]), size_x = 2, size_y = 1, GroupKey = 1001, StableOrder = 0, Label = PlayerUseLabel.Other };
            var emptyPrep = ManualTidyService.PreparePage(empty, 2, true, TidyMode.SameType, strategy, emptyPending);
            check(emptyPrep.Valid && emptyPrep.Pending == emptyPending, "事务层：空页+该件=合法（before 零件时 pending 仍是守恒的一部分）");

            // 5d. 禁吞物的静态验证：策略把其它件全放好、唯独漏掉该件 → Prepare 必须
            //     fail（旧 v1.4.0 谎报成功的确切形状在此被钉死）。
            var swallow = V54FragmentedPages(out pending);
            var swallowPending = new PackableItem { Tag = pending, size_x = 2, size_y = 1, GroupKey = pending.id, StableOrder = 4, Label = PlayerUseLabel.Other };
            var swallowPrep = ManualTidyService.PreparePage(swallow[2], 2, true, TidyMode.SameType, new V54DropPendingStrategy(), swallowPending);
            check(!swallowPrep.Valid, "事务层：计划漏掉待加入物品 = 直接非法（禁止报成功却没放入的吞物形）");

            // 5e. 按钮入口逐字节不变：pending=null 的 6 参重载 = 5 参旧形。
            var cmp1 = V54FragmentedPages(out pending);
            var cmp2 = V54FragmentedPages(out pending);
            var legacy = ManualTidyService.PreparePage(cmp1[2], 2, true, TidyMode.SameType, strategy);
            var asNew = ManualTidyService.PreparePage(cmp2[2], 2, true, TidyMode.SameType, strategy, null);
            var same = legacy.Valid == asNew.Valid && legacy.Pending == null && asNew.Pending == null
                && legacy.Result.Count == asNew.Result.Count;
            if (same)
                for (int i = 0; i < legacy.Result.Count; i++)
                    if (legacy.Result[i].Placed != asNew.Result[i].Placed ||
                        legacy.Result[i].ResultX != asNew.Result[i].ResultX ||
                        legacy.Result[i].ResultY != asNew.Result[i].ResultY ||
                        legacy.Result[i].ResultRot != asNew.Result[i].ResultRot) same = false;
            check(same, "事务层：无 pending 的计划路径与旧入口逐格一致（按钮整理零影响）");

            // 5f. 提交出口对空计划集不伪报成功。
            var emptyCommit = ManualTidyService.CommitPreparations(new List<PagePreparation>(), null);
            check(emptyCommit.Result == TidyCommitResult.Rejected && !emptyCommit.MutationStarted,
                "事务层：CommitPreparations 空集 = RejectedNoMutation（共享出口的防御，不空转谎称已提交）");
        }

        /// <summary>把「非 Item-Tag」（=待加入物品）单独漏放的策略：还原 v1.4.0
        /// 谎报成功的形状，钉静态验证必须拒绝。</summary>
        private sealed class V54DropPendingStrategy : ITidyStrategy
        {
            public string StrategyId { get { return "v5-04-drop-pending-fixture"; } }
            public TidyPlan BuildPlan(TidyInput input)
            {
                var placements = new List<PackableItem>();
                foreach (var item in input.Items)
                {
                    if (item.Tag is ItemJar jar)
                    {
                        placements.Add(new PackableItem
                        {
                            Tag = jar, size_x = item.size_x, size_y = item.size_y, GroupKey = item.GroupKey,
                            StableOrder = item.StableOrder, OriginalX = jar.x, OriginalY = jar.y, OriginalRot = jar.rot,
                            PreferredRotation = jar.rot, Label = item.Label,
                            Placed = true, ResultX = jar.x, ResultY = jar.y, ResultRot = jar.rot,
                        });
                        continue;
                    }
                    placements.Add(new PackableItem
                    {
                        Tag = item.Tag, size_x = item.size_x, size_y = item.size_y, GroupKey = item.GroupKey,
                        StableOrder = item.StableOrder, Label = item.Label, Placed = false,
                    });
                }
                return new TidyPlan(StrategyId, placements, true); // 谎报：AllPlaced=true 但漏了该件
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // 组6：恢复成功不发布整理完成（Q5：不当成点了整理→不触发压弹/合匣，
        // 不新增公开事件；总线记录器全量观察）。
        // ─────────────────────────────────────────────────────────────────
        private static void RecoverGroupNoTidyCompleted(System.Action<bool, string> check)
        {
            var recorder = new V54CommitRecorder();
            InsertRecoverAdapter.CommitForTests = recorder.Commit;
            InsertRecoverAdapter.PendingProbeForTests = _ => Info(2, 1);
            InventoryTidyModule.RecoverPatchInstallerForTests = _ => true;

            var feature = new FeatureId(LitRuntime.FeatureIdValue);
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var runtime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 1001UL);
            var module = new InventoryTidyModule(feature);
            module.ScopeDirectoryForTests = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "bue-v5-04-lit-" + Guid.NewGuid().ToString("N"));
            module.FaultContextForTests = () => new LitFaultScopeContext("TestMap", 1);
            module.NetServiceFactoryForTests = (m, net, book) => new LitTidyNetService(m, net, new V54NoopAuthority(), () => true, book);
            var settings = V54DescendingSettings();
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, settings,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, runtime);
            var started = module.Start(bootstrap);
            var events = new List<TidyCompleted>();
            bus.Subscriber(feature).Subscribe<TidyCompleted>(events.Add);
            try
            {
                check(started.Started && ReferenceEquals(InsertRecoverAdapter.ActiveModule, module),
                    "总线形：模块经真实 bootstrap 启动且恢复面登记接上");
                Item pending;
                var inv = V54FragmentedPages(out pending);
                InsertRecoverScope.Enter();
                var a = InsertRecoverAdapter.TryRecoverPages(inv, null, pending, false);
                InsertRecoverScope.Exit();
                check(a.Recovered && recorder.Calls == 1, "总线形：恢复成功（对照组前提）");
                check(events.Count == 0, "不发整理完成：恢复成功零 TidyCompleted（不当成点了整理；LIR 压弹/合匣不触发）");
                check(module.LastLocalOutcome == null && module.NextTransactionId() == 1UL,
                    "不发整理完成：恢复不占用按钮整理的事务身份账（不是被冒充成一次点整理的新路径）");
            }
            finally
            {
                module.Stop(FeatureStopReason.UserDisabled);
                InventoryTidyModule.RecoverPatchInstallerForTests = null;
            }
        }

        private sealed class V54NoopAuthority : ILitTidyAuthority
        {
            public List<HotkeySnapshot> CaptureClientHotkeys() { return new List<HotkeySnapshot>(); }
            public LitAuthorityResult ExecuteServerTidy(LitTidyRequestContext request)
            { return LitAuthorityResult.From(TidyOperationOutcome.Committed); }
            public LitContainerAuthorityResult ExecuteServerContainerTidy(LitContainerTidyRequestContext request)
            { return new LitContainerAuthorityResult { Outcome = TidyOperationOutcome.RejectedNoMutation, Reason = LitContainerTidyReason.None }; }
            public LitHotkeyRestoreResult RestoreServerHotkeys(ulong peerSteamId, List<HotkeyRestoreEntry> entries)
            { return new LitHotkeyRestoreResult { Restored = 0, Verified = 0, Cleared = 0, FailedIndices = new List<byte>() }; }
            public bool VerifyClientConvergence(List<LitNewPositionMapping> mappings) { return true; }
        }
    }
}
