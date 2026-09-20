using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Events;
using BetterUnturnedExperience.Lht;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-13 红测组：LHT 官方消费者降权与资源所有权归一（票 31 / V6-T5 Q2 追加裁决）。
    /// 被测外部行为（不锁搬家后的类名与落点）：
    ///   1. 宿主私有面清尽：宿主完成链不驱动尸潮、宿主不保存也不直接泵动尸潮运行实例；
    ///      节拍走冻结的 HostTick 事件缝——源级机器锚。
    ///   2. 工厂路径交付惰性模块：Create() 出来的模块零补丁、零登记、未 Started
    ///      （武装只发生在 IFeatureModule.Start，即生命周期内）。
    ///   3. 补丁经启动口袋登记入既有资源账：Start 后恰一枚句柄（一个 Harmony 身份）
    ///      经 IFeaturePatching 进账（BUE-LIFE-ACCEPT 同族行），拆除所有权移交平台。
    ///   4. 拆除所有权单一：登记成功后模块自身不再 UnpatchSelf（Stop 期间零自拆留痕），
    ///      平台在停止边界经账释放句柄（释放行点名 Harmony 身份）。
    ///   5. 设置开关（尸潮是**全停开关**：off = 追踪退订 + 频道注销 + 补丁原生回退，
    ///      on = 整链重臂）在一代内可重复：登记是**代际级**的（一代恰一条账项、句柄不
    ///      提前释放）；每轮热摘经句柄的同一条拆除动作（不双拆、不留已拆句柄），每轮
    ///      热装重新武装同一身份（不新增账项、不换句柄）；70 轮（> 既有资源账每代际
    ///      容量 64）仍零拒绝、零幽灵条目。
    ///   6. 缺口袋 / 登记被拒 / 半装失败：模块立即自拆，平台账外零补丁、零句柄，
    ///      且武装事务就此终止（其余面不再武装）。
    ///   7. 隔离与代际作废使已登记补丁失效（同一账、同一释放律）。
    ///   8. 账释放遇拆除故障：故障沿平台管线隔离（BUE-LIFE-006），不落「已拆除」释放行。
    ///   9. 无画面：HUD 补丁不武装（诚实诊断），权威信标面仍武装且仍登记，且权威行为真做。
    /// 维护锚：与 DevV611 / DevV612 同规约；宿主套件装不上真 Harmony（引擎 ECall 规则），
    /// 拆除动作以「可观察行 + 账上句柄状态」固定并如实标注。
    /// </summary>
    internal static class DevV613LhtDeprivilegeTests
    {
        private const string LhtFeatureId = "io.github.yu80rice.bue.horde-tracker";

        internal static void Run(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, Action body)
                {
                    try { body(); }
                    catch (Exception error)
                    {
                        // 组异常保留首个栈帧：突变态下异常也带位置，免得红态不可定位。
                        var frames = (error.StackTrace ?? string.Empty).Split('\n');
                        reds.Add(name + " 组异常: " + error.Message + (frames.Length > 0 ? " @" + frames[0].Trim() : string.Empty));
                    }
                }

                Group("宿主私有面清尽（无私有泵、不保存运行实例）", () => V613GroupHostPrivateSurfaceGone(Check));
                Group("工厂路径交付惰性模块", () => V613GroupFactoryBornInert(Check));
                Group("补丁经口袋登记入既有账", () => V613GroupPatchPocketAccount(Check));
                Group("拆除所有权：平台拆、模块不双拆", () => V613GroupSingleOwnership(Check));
                Group("设置开关：一代内可重复全停/重臂", () => V613GroupToggleRepeatable(Check));
                Group("开关拆装失败两向（热摘故障 / 拒绝重复安装）", () => V613GroupToggleFailureBothWays(Check));
                Group("缺口袋/拒绝/半装：立即自拆不留半装", () => V613GroupNoHalfInstall(Check));
                Group("隔离使已登记补丁失效", () => V613GroupIsolation(Check));
                Group("代际作废使已登记补丁失效", () => V613GroupGenerations(Check));
                Group("账释放遇拆除故障的语义", () => V613GroupReleaseFault(Check));
                Group("无画面：权威面登记、画面面不武装", () => V613GroupHeadlessAuthority(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：宿主私有面清尽 ─────────────────────────

        /// <summary>票面 Scope：「Plugin 不保存或直接驱动尸潮运行实例」。源级机器锚（宿主
        /// 套件无法在进程内取真机 Harmony 泵路径，故以缺席锚固定）：宿主完成链零尸潮标识、
        /// 宿主源码零尸潮模块名、组装类型公开面零节拍成员、模块帧入口非公开且订阅冻结缝。</summary>
        private static void V613GroupHostPrivateSurfaceGone(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var chainSource = File.ReadAllText(Path.Combine(repoRoot, "src",
                "BetterUnturnedExperience.Plugin", "BueRuntimeCompletionChain.cs"));
            Check(chainSource.IndexOf("Lht", StringComparison.Ordinal) < 0,
                "私有泵：宿主完成链不得点名尸潮（尸潮无宿主节拍泵，帧工作走冻结 HostTick 缝）");
            Check(chainSource.IndexOf("Horde", StringComparison.Ordinal) < 0,
                "私有泵：宿主完成链不得出现尸潮域标识（宿主不泵动尸潮运行实例）");

            var pluginDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Plugin");
            foreach (var file in Directory.GetFiles(pluginDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                Check(source.IndexOf("HordeTrackerModule", StringComparison.Ordinal) < 0,
                    "运行实例：宿主不得保存或直呼尸潮运行实例（点名 HordeTrackerModule 的文件 "
                    + Path.GetFileName(file) + "）");
            }

            var facadeSource = File.ReadAllText(Path.Combine(repoRoot, "src",
                "BetterUnturnedExperience.Lht", "LhtFeatureAssembly.cs"));
            Check(facadeSource.IndexOf("TickWiredModule", StringComparison.Ordinal) < 0,
                "私有泵：尸潮组装类型不得保留宿主专属节拍泵缝（从未有，也不许搬来）");
            foreach (var member in typeof(LhtFeatureAssembly).GetMethods(BindingFlags.Public | BindingFlags.Static))
                Check(member.Name.IndexOf("Tick", StringComparison.Ordinal) < 0,
                    "私有泵：尸潮组装类型不得暴露节拍成员 " + member.Name + "（宿主不得经公开面泵动尸潮）");

            var moduleSource = File.ReadAllText(Path.Combine(repoRoot, "src",
                "BetterUnturnedExperience.Lht", "HordeTrackerModule.cs"));
            Check(moduleSource.IndexOf("Subscribe<HostTick>", StringComparison.Ordinal) >= 0,
                "节拍来源：模块应订阅冻结的 HostTick 事件缝（宿主时钟是平台公共能力，非官方后门）");
            var tickEntry = typeof(HordeTrackerModule).GetMethod("OnHostTick",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Check(tickEntry != null && !tickEntry.IsPublic,
                "节拍来源：模块的帧入口只能是自己的非公开成员（宿主不得经公开面泵动它）");
        }

        // ───────────────────────── 组 2：工厂路径交付惰性模块 ─────────────────────────

        /// <summary>票面 Scope：「所有尸潮相关 Harmony 经 IFeatureBootstrap.Patching 登记」。
        /// 工厂出来的模块必须是惰性的——武装只发生在 IFeatureModule.Start；突变把武装挪回
        /// 工厂即在此留痕。</summary>
        private static void V613GroupFactoryBornInert(Action<bool, string> Check)
        {
            using (var harness = new V613Harness())
            {
                // 安装器缝在座：工厂若回潮提前武装，两条缝会被真实调用（本组据此判红）。
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var module = (HordeTrackerModule)LhtFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                Check(module != null, "工厂路径：Create() 应交付模块实例");
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled && !module.PatchesInstalled,
                    "工厂路径：Create() 出来的模块零补丁（信标=" + module.BeaconPatchesInstalled
                    + " HUD=" + module.HudPatchesInstalled + " 集=" + module.PatchesInstalled + "）");
                Check(!module.Started && !module.PatchTeardownDelegated && module.PatchRegistration == null,
                    "工厂路径：Create() 不得进入 Started/移交态（武装只归生命周期内）");
                Check(harness.BeaconInstalled.Count == 0 && harness.HudInstalled.Count == 0,
                    "工厂路径：提前武装缺席=零安装器调用（实际 beacon=" + harness.BeaconInstalled.Count
                    + " hud=" + harness.HudInstalled.Count + "）");
                Check(ReferenceEquals(LhtFeatureAssembly.WiredModule, module),
                    "工厂路径：接线实例仍在座（重置缝语义不变）");
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 3：补丁经口袋登记 ─────────────────────────

        /// <summary>票面 Scope：「信标补丁与可选 HUD 补丁均经 IFeatureBootstrap.Patching 登记；
        /// Arm 后入账」。一个 Harmony 身份（本功能全部补丁面共用同一实例/ID）对应一枚句柄：
        /// 受理行经既有资源账（BUE-LIFE-ACCEPT 同族），拆除所有权移交平台。</summary>
        private static void V613GroupPatchPocketAccount(Action<bool, string> Check)
        {
            var pocket = LhtTestPocket.Open();
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                Check(started.Started, "登记 setup：模块经生命周期启动，实际 " + started.DiagnosticId);
                Check(module.BeaconPatchesInstalled && module.HudPatchesInstalled && module.PatchesInstalled,
                    "登记：信标面与 HUD 面随生命周期武装（信标=" + module.BeaconPatchesInstalled
                    + " HUD=" + module.HudPatchesInstalled + " 集=" + module.PatchesInstalled + "）");
                Check(harness.BeaconInstalled.Count == 1 && harness.HudInstalled.Count == 1,
                    "登记：两面各装齐（信标 1 + HUD 1），实际 beacon=" + harness.BeaconInstalled.Count
                    + " hud=" + harness.HudInstalled.Count);
                Check(harness.BeaconInstalled.Count == 1 && harness.BeaconInstalled[0] == typeof(BeaconCounterPatches),
                    "登记：权威面装的是信标补丁类（一个 Harmony 身份下的本功能补丁面）");
                Check(harness.HudInstalled.Count == 1 && harness.HudInstalled[0] == typeof(PlayerLifeUiHudPatches),
                    "登记：画面面装的是 HUD 补丁类");
                Check(pocket.Accepted.Count == 1,
                    "登记入账：恰一枚句柄（一个 Harmony 身份=一个补丁所有权事实），实际 " + pocket.Accepted.Count);
                Check(Contains(pocket.MachineLines, "diagnosticId=BUE-LIFE-ACCEPT"),
                    "登记入账：句柄进既有资源跟踪账（不新造第二套账）");
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-001"),
                    "登记入账：模块侧登记行点名补丁口受理（官方功能自有码）");
                Check(module.PatchTeardownDelegated && module.PatchRegistration != null,
                    "登记入账：拆除所有权移交平台账");
                Check(module.PatchRegistration != null && module.PatchRegistration.HarmonyId == LhtFeatureId,
                    "登记入账：句柄点名本功能的 Harmony 身份，实际 "
                    + (module.PatchRegistration != null ? module.PatchRegistration.HarmonyId : "<null>"));
                Check(module.PatchRegistration != null && !module.PatchRegistration.Released,
                    "登记入账：未到停止边界不得拆除");
                Check(pocket.LiveCount == 1, "登记入账：账上恰一枚活句柄（与武装状态一致），实际 " + pocket.LiveCount);
                module.Stop(FeatureStopReason.UserDisabled);
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 4：拆除所有权单一 ─────────────────────────

        /// <summary>票面 Scope：「Arm 后入账，成功后平台是正常拆除的唯一所有者」「Disarm 与
        /// Stop：只由账本所有者拆除；禁止模块在移交后再 UnpatchSelf」。宿主套件装不上真
        /// Harmony（引擎 ECall 规则），故「模块自拆」以模块侧自拆留痕（BUE-LHT-PATCH-005）
        /// 的缺席固定、「平台真拆」以账上句柄状态 + 释放行固定；「不双拆」另以拆除动作
        /// 计数（恰好一次）固定。</summary>
        private static void V613GroupSingleOwnership(Action<bool, string> Check)
        {
            var pocket = LhtTestPocket.Open();
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                Check(started.Started, "所有权 setup：模块启动");
                var handle = module.PatchRegistration;
                Check(handle != null && module.PatchTeardownDelegated, "所有权 setup：补丁句柄已移交平台账");
                Check(harness.RevertCalls == 0, "所有权 setup：武装期零拆除动作");

                module.Stop(FeatureStopReason.UserDisabled);
                Check(harness.CountLines("diagnosticId=BUE-LHT-PATCH-005") == 0,
                    "单一所有权：移交后模块停止不得自拆补丁（自拆留痕应为零，实际 "
                    + harness.CountLines("diagnosticId=BUE-LHT-PATCH-005") + " 条）");
                Check(harness.CountLines("diagnosticId=BUE-LHT-PATCH-004") == 0,
                    "单一所有权：模块停止阶段不得出现平台释放行（释放归边界）");
                Check(harness.RevertCalls == 0, "单一所有权：模块停止阶段零拆除动作，实际 " + harness.RevertCalls);
                Check(handle != null && !handle.Released, "单一所有权：停止返回时账上句柄尚未释放（边界在 CompleteStop）");
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled,
                    "单一所有权：停机后登记面注销（功能停=路径不执行，不是补丁体内死开关）");
                Check(module.PatchesInstalled,
                    "单一所有权：停止返回时补丁集仍在架——拆除归平台边界，模块不自行拆除（在架事实与在册开关分家）");

                Check(pocket.StopAndRelease(), "单一所有权：平台停止边界完成");
                Check(handle != null && handle.Released, "单一所有权：平台经既有账逆序释放句柄（Dispose=该补丁集的拆除动作）");
                Check(harness.RevertCalls == 1, "单一所有权：拆除动作恰一次（平台边界，不双拆），实际 " + harness.RevertCalls);
                Check(!module.PatchesInstalled, "单一所有权：平台边界跑过拆除动作后补丁集归零（在架事实随拆除返回）");
                Check(pocket.InAccountCount == 1 && pocket.LiveCount == 0 && pocket.GhostCount == 1,
                    "单一所有权：登记终结在代际边界（账上那一条已结清），实际 条目=" + pocket.InAccountCount
                    + " 活=" + pocket.LiveCount + " 结清=" + pocket.GhostCount);
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-004"),
                    "单一所有权：释放行点名本功能 Harmony 身份（平台真拆，非对已拆对象的空转）");
                Check(pocket.MachineLines.Count > 0 && Contains(pocket.MachineLines, "diagnosticId=BUE-LIFE-RELEASE"),
                    "单一所有权：释放走既有资源账（BUE-LIFE-RELEASE 同族行）");
                Check(pocket.LiveCount == 0, "单一所有权：边界后账上零活句柄，实际 " + pocket.LiveCount);
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────── 组 5：设置开关一代内可重复全停/重臂 ─────────────────────

        /// <summary>票面 Scope：「设置开关……一代内动态拆装」「Disarm 与 Stop：只由账本所有者
        /// 拆除；禁止模块在移交后再 UnpatchSelf」「禁止账上留下已拆句柄」。尸潮的开关是**全停
        /// 开关**（off = 追踪退订 / 频道注销 / 补丁原生回退），故一个代际内同样会出现多次
        /// 「武装 → 撤销」。本票的形状（循 LIR 收口后的形状；两件事实分开断言，互不冒充）：
        ///   - **登记是代际级的**：一个功能一个生命周期代际恰一条账项；开关热摘热装既不
        ///     新增条目也不释放句柄——账不随开关累积，既有资源账的每代际容量与开关次数
        ///     无关（连做 70 轮 &gt; 容量 64 仍零拒绝）；
        ///   - **撤销经本句柄**：每轮热摘跑同一条拆除动作（恰一次反转，不另起 UnpatchSelf，
        ///     句柄不释放=账上不留已拆句柄）；
        ///   - **在架事实 vs 登记存续**：热摘只改「补丁集是否在架」（模块侧事实），登记记录
        ///     （句柄、账上那一条）不变；登记终结发生在代际边界（平台账释放那一条）。
        ///     「每轮撤销即释放登记」是**被否掉的替代形态**：那会让账项随开关次数累积并撞
        ///     每代际容量上限（LIR 票 R1 以 BLOCKING 否掉过同一形态）。
        ///   - **平台是正常拆除的唯一所有者**：边界那次释放跑同一条动作。</summary>
        private static void V613GroupToggleRepeatable(Action<bool, string> Check)
        {
            const int rounds = 70; // > 既有资源账每代际容量 64：容量与开关次数无关是本组的判据
            var pocket = LhtTestPocket.Open();
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var settings = new V613SettingsView();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started, settings);
                Check(started.Started, "开关 setup：模块启动");
                var handle = module.PatchRegistration;
                Check(handle != null && module.PatchTeardownDelegated,
                    "开关 setup：补丁句柄已移交平台账");
                Check(module.Enabled && module.PatchesInstalled && pocket.Accepted.Count == 1,
                    "开关 setup：默认开启且在架，一代恰一条账项，实际 " + pocket.Accepted.Count);

                for (var round = 0; round < rounds; round++)
                {
                    settings.Enabled = false;
                    module.RefreshSwitches();
                    Check(!module.Enabled && !module.BeaconPatchesInstalled && !module.HudPatchesInstalled && !module.PatchesInstalled,
                        "开关：热摘后补丁集归零（原生回退）@轮 " + round + "（信标=" + module.BeaconPatchesInstalled
                        + " HUD=" + module.HudPatchesInstalled + " 集=" + module.PatchesInstalled + "）");
                    Check(handle.RevokeCount == round + 1,
                        "开关：每轮热摘经句柄的同一条拆除动作（不另起模块自拆），实际 revoke=" + handle.RevokeCount + " @轮 " + round);
                    Check(harness.RevertCalls == round + 1,
                        "开关：热摘恰一次反转 @轮 " + round + "，实际 " + harness.RevertCalls);
                    Check(harness.CountLines("diagnosticId=BUE-LHT-PATCH-005") == 0,
                        "开关：热摘不得走模块账外自拆路径（自拆留痕应为零）@轮 " + round);
                    Check(pocket.Accepted.Count == 1 && pocket.LiveCount == 1 && pocket.GhostCount == 0,
                        "开关：登记是代际级的——热摘不新增账项、不释放句柄、不留已拆句柄 @轮 " + round
                        + "（条目=" + pocket.Accepted.Count + " 活=" + pocket.LiveCount + " 幽灵=" + pocket.GhostCount + "）");
                    // 两条事实分开断言（互不冒充）：补丁集不在架 ≠ 登记终结——登记是代际级的，
                    // 终结发生在代际边界（平台账释放）。故此处同时要求：在架=假 ∧ 账上仍恰一条 ∧
                    // 句柄未结清（同一枚 Harmony 身份的登记仍在平台手里）。
                    Check(!module.PatchesInstalled && pocket.InAccountCount == 1 && pocket.LiveCount == 1,
                        "开关：热摘只撤在架事实——账上仍是同一条代际登记（句柄未结清、平台仍拥有拆除）@轮 " + round
                        + "（在架=" + module.PatchesInstalled + " 条目=" + pocket.InAccountCount
                        + " 活=" + pocket.LiveCount + "）");
                    Check(ReferenceEquals(module.PatchRegistration, handle) && !handle.Released,
                        "开关：热摘后登记记录不变（同一句柄、未释放），修改的只有在架事实 @轮 " + round);
                    Check(harness.Authority.BeaconUnsubscribeCalls == round + 1,
                        "开关：全停语义未因所有权移交而丢——追踪仍退订 @轮 " + round);

                    settings.Enabled = true;
                    module.RefreshSwitches();
                    Check(module.Enabled && module.BeaconPatchesInstalled && module.HudPatchesInstalled && module.PatchesInstalled,
                        "开关：热装重新武装同一身份（信标 + HUD 两面在架）@轮 " + round);
                    Check(ReferenceEquals(module.PatchRegistration, handle),
                        "开关：热装不换句柄（同一 Harmony 身份=同一所有权事实）@轮 " + round);
                    Check(module.PatchTeardownDelegated, "开关：热装后拆除所有权仍在平台账 @轮 " + round);
                    Check(pocket.Accepted.Count == 1 && pocket.LiveCount == 1 && pocket.GhostCount == 0,
                        "开关：热装不新增账项（每代际容量与开关次数无关）@轮 " + round
                        + "（条目=" + pocket.Accepted.Count + " 活=" + pocket.LiveCount + "）");
                    Check(!module.ShuttingDown, "开关：开关不是代际边界（模块仍在运行）@轮 " + round);
                }

                module.Stop(FeatureStopReason.UserDisabled);
                Check(pocket.StopAndRelease(), "开关：平台停止边界完成");
                Check(harness.RevertCalls == rounds + 1,
                    "开关：边界那次释放跑同一条拆除动作（在架真拆恰一次），实际 " + harness.RevertCalls);
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-004"), "开关：边界释放行在册");
                Check(pocket.Accepted.Count == 1 && pocket.LiveCount == 0,
                    "开关：70 轮后账上仍恰一条条目且已结清（容量与开关次数无关），实际 条目="
                    + pocket.Accepted.Count + " 活=" + pocket.LiveCount);
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ─────────────────── 组 5b：开关拆装失败两向（热摘故障 / 拒绝重复安装） ───────────────────

        /// <summary>票面 Scope：「设置变化后账与武装状态一致」「玩法与设置语义不变」。全停开关
        /// 可重复 ⇒ 反转动作必须能失败而不留错账：热摘反转故障 ⇒ 在册状态归零 + 诚实留痕
        /// （BUE-LHT-PATCH-006，不向设置宿主抛出）；此后旧集可能仍在架时**拒绝重复安装**
        /// （BUE-LHT-PATCH-007，fail-closed 保持原版语义，不叠第二份前缀），账目与句柄都不动。</summary>
        private static void V613GroupToggleFailureBothWays(Action<bool, string> Check)
        {
            var pocket = LhtTestPocket.Open();
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var settings = new V613SettingsView();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started, settings);
                var handle = module.PatchRegistration;
                Check(started.Started && handle != null && module.PatchTeardownDelegated, "拆装失败 setup：一代一条账项已入账");

                // 热摘反转故障：拆除动作跑失败。
                harness.RevertFaults = true;
                settings.Enabled = false;
                module.RefreshSwitches();
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled,
                    "热摘故障：在册状态归零（功能面按停用运行，不静默服务）");
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-006"),
                    "热摘故障：诚实留痕点名拆除故障（不向设置宿主抛出）");
                Check(!module.PatchTeardownDelegated || module.PatchRegistration != null,
                    "热摘故障：句柄与账目不错乱（登记不动、句柄不提前释放）");
                Check(pocket.Accepted.Count == 1 && pocket.LiveCount == 1,
                    "热摘故障：账上仍恰一条活条目（登记是代际级的），实际 " + pocket.Accepted.Count + "/" + pocket.LiveCount);

                // 热装：旧集可能仍在架（反转失败 ⇒ patchSetInstalled 未被清）⇒ 先归一；
                // 归一仍失败 ⇒ 拒绝重复安装（fail-closed，不叠第二份前缀）。
                var revertsBefore = harness.RevertCalls;
                settings.Enabled = true;
                module.RefreshSwitches();
                Check(harness.RevertCalls == revertsBefore + 1,
                    "拒绝重复安装：先尽力归一（恰一次反转尝试），实际 " + (harness.RevertCalls - revertsBefore));
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-007"),
                    "拒绝重复安装：旧集可能仍在架且反转未成功 ⇒ fail-closed + 结构化留痕");
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled,
                    "拒绝重复安装：不武装任何面（保持原版语义，绝不叠第二份前缀）");
                Check(pocket.Accepted.Count == 1 && module.PatchTeardownDelegated
                        && ReferenceEquals(module.PatchRegistration, handle),
                    "拒绝重复安装：不新增账项、不换句柄、登记不动（账目与句柄都不动），实际 " + pocket.Accepted.Count);

                // 故障收尾：故障消失后，开关 on 侧是**幂等的重臂点**——一次设置事件即把补丁
                // 重新武装回同一身份（先归一旧集、再装两面），而不是把「显示开启但补丁不在架」
                // 的态永久留下（R2 Spec 轴的 DEVIATION 收口：此腿必须有机器断言，不得只留注释）。
                harness.RevertFaults = false;
                var revertsBeforeRecovery = harness.RevertCalls;
                var subsBeforeRecovery = harness.Authority.BeaconSubscribeCalls;
                var registersBeforeRecovery = harness.Net.RegisterCalls;
                module.RefreshSwitches();
                Check(harness.RevertCalls == revertsBeforeRecovery + 1,
                    "故障恢复：先归一旧集（恰一次反转尝试），实际 +" + (harness.RevertCalls - revertsBeforeRecovery));
                Check(module.PatchesInstalled && module.BeaconPatchesInstalled && module.HudPatchesInstalled,
                    "故障恢复：两面补丁重新在架（信标=" + module.BeaconPatchesInstalled
                    + " HUD=" + module.HudPatchesInstalled + " 集=" + module.PatchesInstalled + "）");
                Check(ReferenceEquals(module.PatchRegistration, handle) && module.PatchTeardownDelegated,
                    "故障恢复：仍是同一枚句柄、登记不动（不新增账项、不换句柄）");
                Check(pocket.Accepted.Count == 1 && pocket.LiveCount == 1,
                    "故障恢复：账上仍恰一条（整条故障链未动账目），实际 " + pocket.Accepted.Count + "/" + pocket.LiveCount);
                Check(module.StartGateDiagnostics.Length == 0,
                    "故障恢复：诊断归零（重臂成功，不再带上一轮的拒绝位），实际 '" + module.StartGateDiagnostics + "'");
                Check(harness.CountLines("diagnosticId=BUE-LHT-PATCH-005") == 0,
                    "故障恢复：全程未走模块账外自拆路径（自拆留痕为零）");
                Check(harness.Authority.BeaconSubscribeCalls == subsBeforeRecovery
                        && harness.Net.RegisterCalls == registersBeforeRecovery,
                    "故障恢复：重臂只补补丁面，不叠追踪订阅/频道注册（整链语义不变；off 已是全停、on 已整链重臂），实际 订阅="
                    + harness.Authority.BeaconSubscribeCalls + "（前 " + subsBeforeRecovery + "）注册="
                    + harness.Net.RegisterCalls + "（前 " + registersBeforeRecovery + "）");
                module.Stop(FeatureStopReason.UserDisabled);
                Check(pocket.StopAndRelease(), "拆装失败：平台停止边界仍完成（账目结清）");
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 6：缺口袋/拒绝/半装 ─────────────────────────

        /// <summary>票面 Scope：「缺口袋、拒绝、半装失败：立即自拆」。不变量贯穿三种失败：
        /// 平台账外零补丁（仍有武装面 ⇒ 必已移交账）、恰一次拆除动作、自拆留痕在册。</summary>
        private static void V613GroupNoHalfInstall(Action<bool, string> Check)
        {
            // a) 缺口袋：宿主未提供补丁口（老形态 bootstrap）——武装后立即自拆，零句柄。
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(1UL, null, out var started);
                Check(started.Started, "缺口袋：模块仍开工（开工不因缺口袋中断，票面不要求抛）");
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled && !module.PatchesInstalled,
                    "缺口袋：平台账外零补丁（立即自拆，不留半装——不得自管补丁账）");
                Check(!module.PatchTeardownDelegated && module.PatchRegistration == null, "缺口袋：零所有权移交");
                Check(harness.RevertCalls == 1, "缺口袋：自拆恰一次拆除动作，实际 " + harness.RevertCalls);
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-003"),
                    "缺口袋：留结构化诊断（可观察，不静默丢补丁）");
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-005"),
                    "缺口袋：自拆留痕在册（武装→立即自拆，而不是从未武装）");
                LhtFeatureAssembly.ResetWiredModule();
            }

            // b) 登记被拒（功能非运行/过期代际/容量）：同样立即自拆。
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var rejector = new V613RejectingPatching(FeaturePatchRegistrationReason.FeatureNotRunning, "BUE-PATCH-002");
                var module = harness.CreateArmed(1UL, rejector, out var started);
                Check(started.Started, "登记被拒：模块仍开工");
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled && !module.PatchesInstalled,
                    "登记被拒：平台账外零补丁（拒绝即自拆）");
                Check(!module.PatchTeardownDelegated && module.PatchRegistration == null,
                    "登记被拒：零所有权移交（不空等平台账）");
                Check(harness.RevertCalls == 1, "登记被拒：自拆恰一次拆除动作，实际 " + harness.RevertCalls);
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-002"),
                    "登记被拒：诊断点名拒绝原因与码（不静默）");
                Check(harness.HasLine(rejector.DiagnosticId), "登记被拒：诊断携带口袋给出的码 " + rejector.DiagnosticId);
                LhtFeatureAssembly.ResetWiredModule();
            }

            // c) 半装失败（HUD 面拒装）：立即整体自拆（信标面一并撤），且武装事务就此
            //    终止——不登记、不留平台账外补丁（票面按模块级读）。
            var pocket = LhtTestPocket.Open();
            using (var harness = new V613Harness())
            {
                harness.Arm(refuseHud: true);
                LhtFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                Check(started.Started, "半装：模块仍开工");
                Check(harness.BeaconInstalled.Count == 1, "半装 setup：信标面先装成（事务已开始）");
                Check(harness.HudInstalled.Count == 0 && harness.HudAttempts == 1,
                    "半装：HUD 面拒装（安装器被调用且答复拒绝），实际 attempts=" + harness.HudAttempts);
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled && !module.PatchesInstalled,
                    "半装失败：模块不停留在半运行——两面全灭（无部分补丁面存活）");
                Check(module.HudStartGateDiagnostics.StartsWith("hud-patch-install-failed", StringComparison.Ordinal),
                    "半装失败：留结构化诊断（画面面自有诊断），实际 " + module.HudStartGateDiagnostics);
                Check(pocket.Accepted.Count == 0 && !module.PatchTeardownDelegated && module.PatchRegistration == null,
                    "半装失败：零句柄进账、零所有权移交（平台账外零补丁）");
                Check(harness.RevertCalls == 1 && harness.HasLine("diagnosticId=BUE-LHT-PATCH-005"),
                    "半装失败：恰一次自拆且留痕在册（宿主装不上真 Harmony，真拆除动作与账状态同族"
                    + "不可达，以自拆留痕 + 源级锚固定），实际 reverts=" + harness.RevertCalls);
                module.Stop(FeatureStopReason.UserDisabled);
                LhtFeatureAssembly.ResetWiredModule();
            }

            // d) 首面拒装：武装事务就此终止——后续面不再武装（不是「试完全部再回滚」）。
            using (var harness = new V613Harness())
            {
                harness.Arm(refuseBeacon: true);
                LhtFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(1UL, null, out var started);
                Check(started.Started, "首面拒装：模块仍开工");
                Check(harness.BeaconAttempts == 1 && harness.HudAttempts == 0,
                    "首面拒装：信标面拒装后其余面不再武装（事务终止），实际 beacon=" + harness.BeaconAttempts
                    + " hud=" + harness.HudAttempts);
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled && !module.PatchesInstalled
                        && harness.RevertCalls == 1,
                    "首面拒装：零补丁留在平台账外 + 恰一次自拆，实际 reverts=" + harness.RevertCalls);
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 7：隔离使已登记补丁失效 ─────────────────────────

        private static void V613GroupIsolation(Action<bool, string> Check)
        {
            var pocket = LhtTestPocket.Open();
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                var first = module.PatchRegistration;
                Check(started.Started && first != null && module.PatchTeardownDelegated,
                    "隔离 setup：第一代际已武装并经口登记");

                Check(pocket.Isolate(), "隔离：机裁决隔离本功能（撤该功能订阅 + 逆序释放其受跟踪资源）");
                Check(first != null && first.Released, "隔离：已登记补丁句柄被账释放（与停止边界同账同律）");
                Check(harness.RevertCalls == 1, "隔离：真拆除动作恰一次（平台边界），实际 " + harness.RevertCalls);
                Check(harness.HasLine("diagnosticId=BUE-LHT-PATCH-004"),
                    "隔离：释放行点名本功能 Harmony 身份");
                Check(Contains(pocket.MachineLines, "diagnosticId=BUE-LIFE-RELEASE"),
                    "隔离：释放走既有资源账（BUE-LIFE-RELEASE 同族行）");
                module.Stop(FeatureStopReason.RuntimeIsolated);
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 8：代际作废使已登记补丁失效 ─────────────────────────

        /// <summary>走生产再启用序列（停止边界 → 新代际），逐段固定失效因果：停机后旧视图
        /// 只是「未运行」，开新代际后按代际失效（GenerationInvalid），旧句柄在停止边界已被
        /// 账释放且不被新代际复用/复活。</summary>
        private static void V613GroupGenerations(Action<bool, string> Check)
        {
            var genPocket = LhtTestPocket.Open();
            using (var genHarness = new V613Harness())
            {
                genHarness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var module = genHarness.CreateArmed(genPocket.Generation, genPocket.Patching, out _);
                var handleA = module.PatchRegistration;
                var staleView = genPocket.Patching;
                var oldGeneration = genPocket.Generation;
                Check(handleA != null && module.PatchTeardownDelegated, "代际作废 setup：第一代际已登记");

                module.Stop(FeatureStopReason.UserDisabled);
                Check(genPocket.StopAndRelease(), "代际作废：停止边界完成（再启用序列第一段）");
                Check(handleA != null && handleA.Released, "代际作废：旧代际句柄在停止边界被账释放");
                var beforeNew = staleView.Register(new ProbeDisposable());
                Check(beforeNew.Reason == FeaturePatchRegistrationReason.FeatureNotRunning,
                    "代际作废 setup：停机后旧视图=FeatureNotRunning（尚未开新代际，笼统未运行），实际 " + beforeNew.Reason);

                var nextGeneration = genPocket.OpenNextGeneration();
                Check(nextGeneration != oldGeneration,
                    "代际作废：开新代际（机 BeginStart），实际 " + oldGeneration + "→" + nextGeneration);
                var afterNew = staleView.Register(new ProbeDisposable());
                Check(afterNew.Reason == FeaturePatchRegistrationReason.GenerationInvalid,
                    "代际作废：旧代际视图登记按代际失效（GenerationInvalid），实际 " + afterNew.Reason);

                // 新代际：同一功能重新武装并经新视图登记恰一条——旧句柄不被复用、不复活。
                var second = genHarness.CreateArmed(nextGeneration, genPocket.Patching, out var secondStart);
                Check(secondStart.Started, "代际作废：新代际模块启动");
                Check(!ReferenceEquals(second.PatchRegistration, handleA) && second.PatchRegistration != null,
                    "代际作废：新代际登记的是新句柄（旧句柄不复活）");
                Check(genPocket.Accepted.Count == 2,
                    "代际作废：一代恰一条账项（两代共两条），实际 " + genPocket.Accepted.Count);
                Check(handleA != null && handleA.Released && second.PatchRegistration != null && !second.PatchRegistration.Released,
                    "代际作废：旧句柄保持已释放态、新句柄在架");
                second.Stop(FeatureStopReason.UserDisabled);
                Check(genPocket.StopAndRelease(), "代际作废：第二代际停止边界完成");
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 9：账释放遇拆除故障的语义 ─────────────────────────

        /// <summary>05 冻结的释放故障语义：故障上浮平台账户释放管线（BUE-LIFE-006 隔离并
        /// 继续），且不落「已拆除」释放行——释放行的含义正是「平台跑过这个补丁集的拆除且它
        /// 返回了」。宿主不可达真引擎逆 JIT，用替身造故障。</summary>
        private static void V613GroupReleaseFault(Action<bool, string> Check)
        {
            var pocket = LhtTestPocket.Open();
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                Check(started.Started && module.PatchRegistration != null, "释放故障 setup：模块启动且补丁在架");

                harness.RevertFaults = true;
                module.Stop(FeatureStopReason.UserDisabled);
                var releasesBefore = harness.CountLines("diagnosticId=BUE-LHT-PATCH-004");
                Check(pocket.StopAndRelease(), "释放故障：平台停止边界仍完成（故障被隔离，不中断清理）");
                Check(harness.RevertCalls == 1, "释放故障：拆除动作被跑过（恰一次），实际 " + harness.RevertCalls);
                Check(harness.CountLines("diagnosticId=BUE-LHT-PATCH-004") == releasesBefore,
                    "释放故障：拆除故障时不得落「已拆除」释放行（不把留痕冒充真拆除）");
                Check(Contains(pocket.MachineLines, "diagnosticId=BUE-LIFE-006"),
                    "释放故障：故障沿平台账户释放管线浮出并隔离（BUE-LIFE-006）");
                Check(!module.BeaconPatchesInstalled && !module.HudPatchesInstalled,
                    "释放故障：在册登记仍被收回（功能开关归零，不静默服务）");
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 10：无画面 ─────────────────────────

        /// <summary>票面 Scope：「无画面不装 HUD 补丁，权威信标逻辑不丢」。判决是决策门禁
        /// （诚实诊断），且缺画面不减所有权移交——权威信标面的句柄照进平台账。</summary>
        private static void V613GroupHeadlessAuthority(Action<bool, string> Check)
        {
            var pocket = LhtTestPocket.Open();
            using (var harness = new V613Harness())
            {
                harness.Arm();
                LhtFeatureAssembly.ResetWiredModule();
                var authority = new V613Authority
                {
                    NextBeacon = new HordeBeaconView(new object(), "发起者甲", "机场", 100),
                    Remaining = 30,
                    Alive = 30,
                };
                var surface = new V613HudSurface();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started,
                    null, authority, surface, canUseClientUi: false);
                Check(started.Started, "无画面 setup：模块启动");
                Check(module.BeaconPatchesInstalled,
                    "无画面：权威信标补丁仍武装——权威尸潮计数不丢");
                Check(!module.HudPatchesInstalled,
                    "无画面：HUD 补丁不武装（画面面是决策门禁）");
                Check(module.HudStartGateDiagnostics == "hud-patch-headless-not-armed",
                    "无画面：HUD 面诊断如实（决策门禁非异常门禁），实际 '" + module.HudStartGateDiagnostics + "'");
                Check(harness.HudInstalled.Count == 0 && harness.HudAttempts == 0,
                    "无画面：HUD 安装器零调用（不武装=不装，不是装完再拆），实际 attempts=" + harness.HudAttempts);
                Check(harness.BeaconInstalled.Count == 1 && harness.RevertCalls == 0,
                    "无画面：信标面装齐（1）且无自拆，实际 beacon=" + harness.BeaconInstalled.Count
                    + " reverts=" + harness.RevertCalls);
                Check(surface.InstallCalls == 0, "无画面：HUD surface 不装载（PlayerLifeUI 隔离）");
                Check(pocket.Accepted.Count == 1 && module.PatchTeardownDelegated,
                    "无画面：权威面的句柄仍经口登记（缺画面不减所有权移交）");

                // 权威行为仍真做：信标激活 → 计数变更置脏 → 下一帧广播。缺画面只砍画面面，
                // 权威追踪与广播照常。计数入口走 Tracking.OnBeaconCounterChanged——真机
                // postfix 经 BeaconCounterPatches → HordeTrackerModule.OnBeaconCounterPatched
                // 落到同一条方法（宿主造不出 InteractableBeacon 实例，故那一跳以源级锚固定，
                // 见下方 patches 源锚断言）。
                module.Tracking.HandleBeaconUpdated(1, true);
                module.OnHostTick(LhtTick(1, 0.1f));
                var beforePatch = harness.Net.Multicasts.Count;
                module.Tracking.OnBeaconCounterChanged(authority.NextBeacon.Beacon);
                module.OnHostTick(LhtTick(2, 0.1f));
                Check(beforePatch == 1 && harness.Net.Multicasts.Count == 2,
                    "无画面：计数变更照常驱动权威广播（激活 1 次 + 计数变更 1 次），实际 before="
                    + beforePatch + " after=" + harness.Net.Multicasts.Count);

                var patchesSource = File.ReadAllText(Path.Combine(FindRepoRoot(), "src",
                    "BetterUnturnedExperience.Lht", "Patches", "BeaconCounterPatches.cs"));
                Check(patchesSource.IndexOf("HordeTrackerModule.OnBeaconCounterPatched(__instance)", StringComparison.Ordinal) >= 0
                        && patchesSource.IndexOf("[HarmonyPostfix]", StringComparison.Ordinal) >= 0,
                    "无画面：信标补丁是纯 Postfix 并落到模块静态入口（补丁跳向权威的那一跳源级在册）");
                module.Stop(FeatureStopReason.UserDisabled);
                LhtFeatureAssembly.ResetWiredModule();
            }
        }

        private static bool Contains(IReadOnlyList<string> lines, string token)
        {
            for (var index = 0; index < lines.Count; index++)
                if (lines[index].Contains(token)) return true;
            return false;
        }

        private static HostTick LhtTick(ulong number, float deltaSeconds)
        {
            return new HostTick(number, deltaSeconds, TickPhase.Update);
        }

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BetterUnturnedExperience.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("repo root not found");
            return directory.FullName;
        }

        /// <summary>过期代际腿的替身句柄：喂进作废视图登记的那个可释放对象。</summary>
        private sealed class ProbeDisposable : IDisposable
        {
            public void Dispose() { }
        }

        /// <summary>拒绝替身：答复结构化拒绝（不抛）——「登记拒绝即自拆」红向。</summary>
        private sealed class V613RejectingPatching : IFeaturePatching
        {
            internal string DiagnosticId { get { return diagnosticId; } }

            private readonly FeaturePatchRegistrationReason reason;
            private readonly string diagnosticId;

            internal V613RejectingPatching(FeaturePatchRegistrationReason reason, string diagnosticId)
            {
                this.reason = reason;
                this.diagnosticId = diagnosticId;
            }

            public FeaturePatchRegistrationResult Register(IDisposable patchTeardown)
            {
                return new FeaturePatchRegistrationResult(false, reason, diagnosticId, 1UL);
            }
        }

        // ───────────────────────── 测试夹具 ─────────────────────────

        /// <summary>
        /// 最小宿主组合：真总线 + 两条安装器缝（信标权威面 / HUD 画面面）——宿主测试进程
        /// 装不上真机补丁，安装器缝是既有家族（LIT/LIR 同形，本票补尸潮两面使「哪一面武装」
        /// 机器可判）；拆除动作走替换缝以便「恰一次」可数。日志走
        /// LhtFeatureAssembly.BindHostComposition 的宿主注入口（生产同路），退出时逐项还原。
        /// 无画面决策是模块自有事实（CanUseClientUiForTests），不在本助手注入面上。
        /// </summary>
        private sealed class V613Harness : IDisposable
        {
            internal readonly List<string> Lines = new List<string>();
            internal readonly List<Type> BeaconInstalled = new List<Type>();
            internal readonly List<Type> HudInstalled = new List<Type>();
            internal readonly FeatureEventBus Bus = new FeatureEventBus();
            internal FeatureId Feature { get { return new FeatureId(LhtFeatureId); } }

            /// <summary>安装器尝试次数（含被拒的那次）——「事务是否已终止」的机器判据。</summary>
            internal int BeaconAttempts;
            internal int HudAttempts;

            /// <summary>真拆除动作被跑过的次数——「不双拆」的机器判据。</summary>
            internal int RevertCalls;

            /// <summary>置 true：Harmony 反打失败（宿主不可达的引擎逆 JIT 故障的替身）。</summary>
            internal bool RevertFaults;

            /// <summary>最近一次 CreateArmed 装配的替身（组断言直接读它）。</summary>
            internal V613Authority Authority;
            internal V613HudSurface Hud;

            /// <summary>启动口袋那一个网络缝（频道登记 / 订阅 / 广播计数都在此）。</summary>
            internal readonly V613Network Net = new V613Network();

            private readonly Action<string> savedLog = LhtRuntime.HostRuntimeLogSink;
            private readonly Action<string> savedError = LhtRuntime.HostErrorLogSink;
            private readonly Func<Type, bool> savedBeacon = HordeTrackerModule.BeaconPatchInstallerForTests;
            private readonly Func<Type, bool> savedHud = HordeTrackerModule.HudPatchInstallerForTests;
            private readonly Func<bool> savedRevert = HordeTrackerModule.PatchRevertForTests;

            /// <summary>配置两条安装器缝与拆除缝（在 Bootstrap/Start 前调用）。</summary>
            internal void Arm(bool refuseBeacon = false, bool refuseHud = false)
            {
                LhtFeatureAssembly.BindHostComposition(Lines.Add, Lines.Add);
                HordeTrackerModule.BeaconPatchInstallerForTests = type =>
                {
                    BeaconAttempts++;
                    if (refuseBeacon) return false;
                    BeaconInstalled.Add(type);
                    return true;
                };
                HordeTrackerModule.HudPatchInstallerForTests = type =>
                {
                    HudAttempts++;
                    if (refuseHud) return false;
                    HudInstalled.Add(type);
                    return true;
                };
                HordeTrackerModule.PatchRevertForTests = () => { RevertCalls++; return !RevertFaults; };
            }

            /// <summary>宿主启动口袋（第 12 实参=补丁口；缺口袋传 null=老形态）。</summary>
            internal IFeatureBootstrap Bootstrap(ulong generation, IFeaturePatching pocket, IScopedFeatureSettings settings = null)
            {
                var feature = Feature;
                return new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(
                    default(FeatureScopeIdentity), generation, settings,
                    Bus.Subscriber(feature), Bus.Publisher(feature), Bus.EventRegistry(feature),
                    null, null, null, Net, null, pocket);
            }

            /// <summary>装配一件模块并启动（工厂路径 + 构造注入），返回本代际实例。</summary>
            internal HordeTrackerModule CreateArmed(ulong generation, IFeaturePatching pocket, out FeatureStartResult result,
                IScopedFeatureSettings settings = null, V613Authority authority = null, V613HudSurface hud = null,
                bool canUseClientUi = true)
            {
                var module = (HordeTrackerModule)LhtFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                Authority = authority ?? new V613Authority();
                Hud = hud ?? new V613HudSurface();
                module.AuthorityFactoryForTests = () => Authority;
                module.HudSurfaceFactoryForTests = () => Hud;
                module.CanUseClientUiForTests = () => canUseClientUi;
                result = module.Start(Bootstrap(generation, pocket, settings));
                return module;
            }

            internal bool HasLine(string token)
            {
                return CountLines(token) > 0;
            }

            internal int CountLines(string token)
            {
                var count = 0;
                for (var index = 0; index < Lines.Count; index++)
                    if (Lines[index].Contains(token)) count++;
                return count;
            }

            public void Dispose()
            {
                LhtFeatureAssembly.ResetWiredModule();
                LhtFeatureAssembly.BindHostComposition(savedLog, savedError);
                HordeTrackerModule.BeaconPatchInstallerForTests = savedBeacon;
                HordeTrackerModule.HudPatchInstallerForTests = savedHud;
                HordeTrackerModule.PatchRevertForTests = savedRevert;
                LhtRuntime.LogSink = null;
                LhtRuntime.ErrorLogSink = null;
            }
        }

        /// <summary>开关面：只答 hordetracker.enabled（冻结 setting id），可按用例翻转。</summary>
        private sealed class V613SettingsView : IScopedFeatureSettings
        {
            internal bool Enabled = true;
            internal uint Revision = 7U;

            public FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)
            {
                return new FeatureSettingsSnapshot(new FeatureId(LhtFeatureId), 1U,
                    revisionScope, Revision, SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent,
                    new List<SettingEntryView>());
            }

            public bool TryGet(string settingId, out SettingValue value, out uint revision)
            {
                value = default(SettingValue);
                revision = Revision;
                if (settingId != "hordetracker.enabled") return false;
                value = SettingValue.Toggle(Enabled);
                return true;
            }

            public SettingChangeResult Submit(ScopedSettingChangeRequest request)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, Revision, default(FeatureSettingsSnapshot));
            }
        }

        /// <summary>权威替身：服务器角色、信标可解析、计数可读——「权威行为真做」的观察面
        /// （广播经启动口袋那一个网络缝计数，见 <see cref="V613Harness.Net"/>）。</summary>
        private sealed class V613Authority : IHordeTrackingAuthority
        {
            internal int BeaconSubscribeCalls, BeaconUnsubscribeCalls;
            internal HordeBeaconView NextBeacon;
            internal int Remaining, Alive;

            public bool IsServerRole() { return true; }
            public void SubscribeBeaconUpdated(Action<byte, bool> handler) { BeaconSubscribeCalls++; }
            public void UnsubscribeBeaconUpdated(Action<byte, bool> handler) { BeaconUnsubscribeCalls++; }
            public void SubscribeServerHosted(Action handler) { }
            public void UnsubscribeServerHosted(Action handler) { }
            public bool TryResolveBeacon(byte nav, out HordeBeaconView beacon) { beacon = NextBeacon; return NextBeacon != null; }
            public bool TryReadCounters(HordeBeaconView beacon, out int remaining, out int alive)
            { remaining = Remaining; alive = Alive; return true; }
            public bool TryFlushHordeCommandRegistration(HordeStatusSource source) { return true; }
            public void DeregisterHordeCommand() { }
            public void NotifyHordeStart(HordeBeaconView beacon) { }
            public void NotifyHordeEnd(HordeBeaconView beacon) { }
        }

        /// <summary>HUD 表面替身（画面面装载计数；不触碰 Glazier）。</summary>
        private sealed class V613HudSurface : IHordeHudSurface
        {
            internal int InstallCalls, UninstallCalls;

            public bool Install() { InstallCalls++; return true; }
            public void Uninstall() { UninstallCalls++; }
            public bool IsLabelReady() { return true; }
            public void SetText(string text) { }
            public void SetVisible(bool visible) { }
            public void DrainDisconnectReset() { }
        }

        /// <summary>回环网络假件：频道登记/订阅恒受理、发送计数（零引擎接触）。</summary>
        private sealed class V613Network : IBueNetworkApi
        {
            internal int RegisterCalls, UnregisterCalls, SubscribeCalls;
            internal readonly List<byte[]> Multicasts = new List<byte[]>();

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { RegisterCalls++; return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "V613"); }

            public bool UnregisterChannel(FeatureId channel) { UnregisterCalls++; return true; }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            { SubscribeCalls++; return new V613Handle(); }

            public IReadOnlyList<IConnectionSession> Sessions { get { return new IConnectionSession[0]; } }
            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
            { Multicasts.Add(payload); return NetworkSendResult.Sent; }
            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }

            private sealed class V613Handle : IDisposable { public void Dispose() { } }
        }
    }
}
