using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Events;
using BetterUnturnedExperience.Lit;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-11 红测组：LIT 官方消费者降权与资源所有权归一（票 29 / V6-T5 Q2 追加裁决）。
    /// 被测外部行为（不锁搬家后的类名与落点）：
    ///   1. 宿主私有面清尽：完成链不再有整理私有节拍泵（TickWiredModule 及其调用），
    ///      宿主不保存也不直接泵动整理运行实例——源级机器锚。
    ///   2. 工厂路径不再提前武装：ModuleFactory.Create() 出来的模块是惰性的（零补丁），
    ///      武装只发生在 IFeatureModule.Start（生命周期内）。
    ///   3. 补丁经启动口袋登记入既有资源账：Start 后恰一枚句柄（一个 Harmony 身份）
    ///      经 IFeaturePatching 进账（BUE-LIFE-ACCEPT 同族行），拆除所有权移交平台。
    ///   4. 拆除所有权单一：登记成功后模块自身不再 UnpatchSelf（Stop 期间零自拆留痕），
    ///      平台在停止边界经账释放句柄（释放行点名 Harmony 身份）。
    ///   5. 缺口袋 / 登记被拒 / 半装失败：模块立即自拆，平台账外零补丁、零句柄。
    ///   6. 节拍走冻结 HostTick 事件缝：宿主时钟的一拍即驱动整理的 dispatcher 拍。
    ///   7. 无画面：客户端补丁不武装，权威补丁仍武装且仍经口登记。
    /// 维护锚：与 DevV602B/DevV605 同规约；宿主套件装不上真 Harmony（引擎 ECall 规则），
    /// 拆除动作以「可观察行 + 账上句柄状态」固定并如实标注。
    /// </summary>
    internal static class DevV611LitDeprivilegeTests
    {
        private const string LitFeatureId = "io.github.yu80rice.bue.inventory-tidy";

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

                Group("宿主私有面清尽（私有 Tick 与实例直呼）", () => V611GroupHostPrivateSurfaceGone(Check));
                Group("工厂路径不再提前武装", () => V611GroupFactoryBornInert(Check));
                Group("补丁经口袋登记入既有账", () => V611GroupPatchPocketAccount(Check));
                Group("拆除所有权：平台拆、模块不双拆", () => V611GroupSingleOwnership(Check));
                Group("缺口袋/拒绝/半装：立即自拆不留半装", () => V611GroupNoHalfInstall(Check));
                Group("节拍走冻结 HostTick 缝", () => V611GroupHostTickBeat(Check));
                Group("隔离使已登记补丁失效", () => V611GroupIsolation(Check));
                Group("代际作废使已登记补丁失效", () => V611GroupGenerations(Check));
                Group("账释放遇拆除故障的语义", () => V611GroupReleaseFault(Check));
                Group("无画面：权威面登记、画面面不武装", () => V611GroupHeadlessAuthority(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：宿主私有面清尽 ─────────────────────────

        /// <summary>票面 Scope：「移除宿主完成链对整理的私有 Tick 驱动（今日
        /// LitFeatureAssembly.TickWiredModule()）……Plugin 不保存整理运行实例、不直接调用
        /// 其运行方法」。源级机器锚（宿主套件无法在进程内取真机 Harmony 泵路径，故以
        /// 缺席锚固定）：完成链文件零整理标识、组装类型零泵成员。</summary>
        private static void V611GroupHostPrivateSurfaceGone(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var chainSource = File.ReadAllText(Path.Combine(repoRoot, "src",
                "BetterUnturnedExperience.Plugin", "BueRuntimeCompletionChain.cs"));
            Check(chainSource.IndexOf("TickWiredModule", StringComparison.Ordinal) < 0,
                "私有 Tick：宿主完成链不得再出现 TickWiredModule（票面点名拆除的私有泵）");
            Check(chainSource.IndexOf("LitFeatureAssembly", StringComparison.Ordinal) < 0,
                "私有 Tick：宿主完成链不得再点名整理组装类型（宿主不泵动整理运行实例）");

            var facadeSource = File.ReadAllText(Path.Combine(repoRoot, "src",
                "BetterUnturnedExperience.Lit", "LitFeatureAssembly.cs"));
            Check(facadeSource.IndexOf("TickWiredModule", StringComparison.Ordinal) < 0,
                "私有 Tick：整理组装类型不得再保留宿主专属节拍泵缝（拆除而非搬家）");
            var pump = typeof(LitFeatureAssembly).GetMethod("TickWiredModule",
                BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            Check(pump == null, "私有 Tick：LitFeatureAssembly 公开面不得再有 TickWiredModule()");
            // 语义锚（不限文本）：组装类型公开面不得留任何节拍/泵成员——改名搬家同样在册。
            foreach (var member in typeof(LitFeatureAssembly).GetMethods(BindingFlags.Public | BindingFlags.Static))
                Check(member.Name.IndexOf("Tick", StringComparison.Ordinal) < 0,
                    "私有 Tick：整理组装类型不得暴露节拍成员 " + member.Name + "（拆除而非改名）");

            var pumpMethod = typeof(InventoryTidyModule).GetMethod("Tick",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Check(pumpMethod != null && !pumpMethod.IsPublic,
                "私有 Tick：模块的拍体只能是模块自己的非公开成员（宿主不得经公开面泵动它）");
            var moduleSource = File.ReadAllText(Path.Combine(repoRoot, "src",
                "BetterUnturnedExperience.Lit", "InventoryTidyModule.cs"));
            Check(moduleSource.IndexOf("Subscribe<HostTick>", StringComparison.Ordinal) >= 0,
                "节拍来源：模块应订阅冻结的 HostTick 事件缝（宿主时钟是平台公共能力，非官方后门）");
        }

        // ───────────────────────── 组 2：工厂路径不再提前武装 ─────────────────────────

        /// <summary>票面 Scope：「所有整理补丁（含可能在正式开工前提前武装的工厂路径）
        /// 收拢到生命周期内武装」。工厂出来的模块必须是惰性的——武装只发生在
        /// IFeatureModule.Start；突变把 EnsureStarted() 挪回工厂即在此留痕。</summary>
        private static void V611GroupFactoryBornInert(Action<bool, string> Check)
        {
            using (var harness = new V611Harness())
            {
                // 安装器缝在座：工厂若回潮提前武装，三条缝会被真实调用（本组据此判红）。
                harness.Arm();
                LitFeatureAssembly.ResetWiredModule();
                var module = (InventoryTidyModule)LitFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                Check(module != null, "工厂路径：Create() 应交付模块实例");
                Check(!module.PatchesInstalled && !module.RecoverPatchesInstalled && !module.FastTransferPatchesInstalled,
                    "工厂路径：Create() 出来的模块零补丁（画面=" + module.PatchesInstalled
                    + " 恢复=" + module.RecoverPatchesInstalled + " 快速转移=" + module.FastTransferPatchesInstalled + "）");
                Check(!module.PatchTeardownDelegated && module.PatchRegistration == null && harness.UiInstalled.Count == 0
                        && harness.RecoverInstalled.Count == 0 && harness.FastInstalled.Count == 0,
                    "工厂路径：提前武装缺席=零安装器调用、零登记、零移交（武装只归生命周期内）");
                Check(ReferenceEquals(LitFeatureAssembly.WiredModule, module),
                    "工厂路径：接线实例仍在座（重置缝语义不变）");
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 3：补丁经口袋登记 ─────────────────────────

        /// <summary>票面 Scope：「经 IFeatureBootstrap.Patching 登记；句柄进既有 TryTrack 账」。
        /// 一个 Harmony 身份（该功能的全部补丁面共用同一 Harmony 实例/ID）对应一枚句柄：
        /// 受理行经既有资源账（BUE-LIFE-ACCEPT 同族），拆除所有权移交平台。</summary>
        private static void V611GroupPatchPocketAccount(Action<bool, string> Check)
        {
            var pocket = LitTestPocket.Open();
            using (var harness = new V611Harness())
            {
                harness.Arm();
                LitFeatureAssembly.ResetWiredModule();
                var module = (InventoryTidyModule)LitFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                var started = module.Start(harness.Bootstrap(pocket.Generation, pocket.Patching));
                Check(started.Started, "登记 setup：模块经生命周期启动，实际 " + started.DiagnosticId);
                Check(module.RecoverPatchesInstalled && module.FastTransferPatchesInstalled,
                    "登记：权威补丁面（入包恢复三件套 + 快速转移两件）随生命周期武装");
                Check(module.PatchesInstalled, "登记 setup：画面补丁面武装（宿主进程经安装器缝，见 V611Harness）");
                Check(pocket.Accepted.Count == 1,
                    "登记入账：恰一枚句柄（一个 Harmony 身份=一个补丁所有权事实），实际 " + pocket.Accepted.Count);
                Check(Contains(pocket.MachineLines, "diagnosticId=BUE-LIFE-ACCEPT"),
                    "登记入账：句柄进既有资源跟踪账（不新造第二套账）");
                Check(harness.HasLine("diagnosticId=BUE-LIT-PATCH-001"),
                    "登记入账：模块侧登记行点名补丁口受理（官方功能自有码）");
                Check(module.PatchTeardownDelegated && module.PatchRegistration != null,
                    "登记入账：拆除所有权移交平台账");
                Check(module.PatchRegistration != null && module.PatchRegistration.HarmonyId == LitFeatureId,
                    "登记入账：句柄点名本功能的 Harmony 身份，实际 " + (module.PatchRegistration != null ? module.PatchRegistration.HarmonyId : "<null>"));
                Check(module.PatchRegistration != null && !module.PatchRegistration.Released, "登记入账：未到停止边界不得拆除");
                module.Stop(FeatureStopReason.UserDisabled);
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 4：拆除所有权单一 ─────────────────────────

        /// <summary>票面 Scope：「登记成功后模块不得再自行 UnpatchSelf（禁止平台拆一次、
        /// 模块再拆一次）」「Stop/隔离/代际作废使已登记补丁失效」。宿主套件装不上真
        /// Harmony（引擎 ECall 规则），故「模块自拆」以模块侧自拆留痕（BUE-LIT-PATCH-005）
        /// 的缺席固定、「平台真拆」以账上句柄状态 + 释放行固定。</summary>
        private static void V611GroupSingleOwnership(Action<bool, string> Check)
        {
            var pocket = LitTestPocket.Open();
            using (var harness = new V611Harness())
            {
                harness.Arm();
                LitFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                Check(started.Started, "所有权 setup：模块启动");
                var handle = module.PatchRegistration;
                Check(handle != null && module.PatchTeardownDelegated, "所有权 setup：补丁句柄已移交平台账");

                module.Stop(FeatureStopReason.UserDisabled);
                Check(harness.CountLines("diagnosticId=BUE-LIT-PATCH-005") == 0,
                    "单一所有权：移交后模块停止不得自拆补丁（自拆留痕应为零，实际 "
                    + harness.CountLines("diagnosticId=BUE-LIT-PATCH-005") + " 条）");
                Check(harness.CountLines("diagnosticId=BUE-LIT-PATCH-004") == 0,
                    "单一所有权：模块停止阶段不得出现平台释放行（释放归边界）");
                Check(handle != null && !handle.Released, "单一所有权：停止返回时账上句柄尚未释放（边界在 CompleteStop）");
                Check(!module.PatchesInstalled && !module.RecoverPatchesInstalled && !module.FastTransferPatchesInstalled,
                    "单一所有权：停机后登记面注销（功能停=路径不执行，不是补丁体内死开关）");

                Check(pocket.StopAndRelease(), "单一所有权：平台停止边界完成");
                Check(handle != null && handle.Released, "单一所有权：平台经既有账逆序释放句柄（Dispose=该补丁集的拆除动作）");
                Check(harness.HasLine("diagnosticId=BUE-LIT-PATCH-004"),
                    "单一所有权：释放行点名本功能 Harmony 身份（平台真拆，非对已拆对象的空转）");
                Check(pocket.MachineLines.Count > 0 && Contains(pocket.MachineLines, "diagnosticId=BUE-LIFE-RELEASE"),
                    "单一所有权：释放走既有资源账（BUE-LIFE-RELEASE 同族行）");
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 5：缺口袋/拒绝/半装 ─────────────────────────

        /// <summary>票面 Scope：「缺口袋、登记拒绝、半装失败：模块立即自拆，不留半装」。
        /// 不变量贯穿三种失败：平台账外零补丁（仍有武装面 ⇒ 必已移交账）。</summary>
        private static void V611GroupNoHalfInstall(Action<bool, string> Check)
        {
            // a) 缺口袋：宿主未提供补丁口（老形态 bootstrap）——武装后立即自拆，零句柄。
            using (var harness = new V611Harness())
            {
                harness.Arm();
                LitFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(1UL, null, out var started);
                Check(started.Started, "缺口袋：模块仍开工（开工不因缺口袋中断，票面不要求抛）");
                Check(!module.PatchesInstalled && !module.RecoverPatchesInstalled && !module.FastTransferPatchesInstalled,
                    "缺口袋：平台账外零补丁（立即自拆，不留半装——不得自管补丁账）");
                Check(!module.PatchTeardownDelegated && module.PatchRegistration == null, "缺口袋：零所有权移交");
                Check(harness.HasLine("diagnosticId=BUE-LIT-PATCH-003"),
                    "缺口袋：留结构化诊断（可观察，不静默丢补丁）");
                Check(harness.HasLine("diagnosticId=BUE-LIT-PATCH-005"),
                    "缺口袋：自拆留痕在册（武装→立即自拆，而不是从未武装）");
                LitFeatureAssembly.ResetWiredModule();
            }

            // b) 登记被拒（功能非运行/过期代际/容量）：同样立即自拆。
            using (var harness = new V611Harness())
            {
                harness.Arm();
                LitFeatureAssembly.ResetWiredModule();
                var rejector = new RejectingPatching(FeaturePatchRegistrationReason.FeatureNotRunning, "BUE-PATCH-002");
                var module = harness.CreateArmed(1UL, rejector, out var started);
                Check(started.Started, "登记被拒：模块仍开工");
                Check(!module.PatchesInstalled && !module.RecoverPatchesInstalled && !module.FastTransferPatchesInstalled,
                    "登记被拒：平台账外零补丁（拒绝即自拆）");
                Check(!module.PatchTeardownDelegated, "登记被拒：零所有权移交（不空等平台账）");
                Check(harness.HasLine("diagnosticId=BUE-LIT-PATCH-002"),
                    "登记被拒：诊断点名拒绝原因与码（不静默）");
                Check(harness.HasLine(rejector.DiagnosticId), "登记被拒：诊断携带口袋给出的码 " + rejector.DiagnosticId);
                LitFeatureAssembly.ResetWiredModule();
            }

            // c) 半装失败（恢复三件套装到第二件即拒）：立即整体自拆，且武装事务就此
            //    终止——其余面不再武装、不登记、不留平台账外补丁（票面按模块级读）。
            var pocket = LitTestPocket.Open();
            using (var harness = new V611Harness())
            {
                var half = 0;
                harness.Arm(recover: _ => { half++; return half < 2; });
                LitFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                Check(started.Started, "半装：模块仍开工");
                Check(!module.RecoverPatchesInstalled,
                    "半装失败：恢复面整体撤销（三处未全装成即不可用）");
                Check(module.RecoverStartGateDiagnostics.StartsWith("recover-patch-install-failed", StringComparison.Ordinal),
                    "半装失败：留结构化诊断，实际 " + module.RecoverStartGateDiagnostics);
                Check(harness.HasLine("diagnosticId=BUE-LIT-PATCH-005"),
                    "半装失败：半装即自拆留痕在册（武装→立即自拆，不留半装；宿主装不上真 Harmony，"
                    + "真拆除动作与账状态同族不可达，以自拆留痕 + 源级锚固定）");
                Check(!AnyArmed(module), "半装失败：模块不停留在半运行——三面全灭（无部分补丁面存活）");
                Check(pocket.Accepted.Count == 0 && !module.PatchTeardownDelegated && module.PatchRegistration == null,
                    "半装失败：零句柄进账、零所有权移交（平台账外零补丁）");
                Check(harness.FastInstalled.Count == 0 && harness.UiInstalled.Count == 0,
                    "半装失败：武装事务终止——其余面安装器零调用（恢复 3 之后不再装快速转移/画面），实际 fast="
                    + harness.FastInstalled.Count + " ui=" + harness.UiInstalled.Count);
                module.Stop(FeatureStopReason.UserDisabled);
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        private static bool AnyArmed(InventoryTidyModule module)
        {
            return module.PatchesInstalled || module.RecoverPatchesInstalled || module.FastTransferPatchesInstalled;
        }

        private static bool Contains(IReadOnlyList<string> lines, string token)
        {
            for (var index = 0; index < lines.Count; index++)
                if (lines[index].Contains(token)) return true;
            return false;
        }

        /// <summary>代际作废腿的替身句柄：过期代际视图登记必须被拒时喂进去的那个可释放对象。</summary>
        private sealed class ProbeDisposable : IDisposable
        {
            public void Dispose() { }
        }

        /// <summary>说谎/拒绝替身：答复结构化拒绝（不抛）——「登记拒绝即自拆」红向。</summary>
        private sealed class RejectingPatching : IFeaturePatching
        {
            internal string DiagnosticId { get { return diagnosticId; } }

            private readonly FeaturePatchRegistrationReason reason;
            private readonly string diagnosticId;

            internal RejectingPatching(FeaturePatchRegistrationReason reason, string diagnosticId)
            {
                this.reason = reason;
                this.diagnosticId = diagnosticId;
            }

            public FeaturePatchRegistrationResult Register(IDisposable patchTeardown)
            {
                return new FeaturePatchRegistrationResult(false, reason, diagnosticId, 1UL);
            }
        }

        // ───────────────────────── 组 6：节拍走冻结 HostTick 缝 ─────────────────────────

        /// <summary>票面 Scope：「节拍走已经公开的宿主时钟或事件面……Plugin 不保存整理
        /// 运行实例、不直接调用其运行方法」。宿主时钟（HostTickClock，宿主身份发布）一拍
        /// 即驱动整理的 dispatcher 拍——与换弹/尸潮同一缝，无官方后门。</summary>
        private static void V611GroupHostTickBeat(Action<bool, string> Check)
        {
            var pocket = LitTestPocket.Open();
            using (var harness = new V611Harness())
            {
                harness.Arm(headless: true);
                LitFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                Check(started.Started, "节拍 setup：模块启动");

                MainThreadDispatcher.ResetForTests();
                var ran = false;
                Check(MainThreadDispatcher.TryEnqueue(new QueuedTidyRequest
                {
                    Work = () => ran = true,
                    Cancel = () => { },
                    Tag = "v611 host-tick probe",
                }), "节拍 setup：整理的 dispatcher 队列开放（模块代际已开）");

                var clock = new HostTickClock(harness.Bus, () => 1000L);
                Check(clock.Tick(), "节拍：宿主时钟以宿主身份发布一拍 HostTick");
                Check(ran, "节拍：HostTick 一拍即驱动整理的 dispatcher 拍（帧工作走冻结事件缝）");

                ran = false;
                Check(MainThreadDispatcher.TryEnqueue(new QueuedTidyRequest
                {
                    Work = () => ran = true,
                    Cancel = () => { },
                    Tag = "v611 host-tick probe 2",
                }), "节拍 setup：第二个探针入队");
                module.Stop(FeatureStopReason.UserDisabled);
                Check(!ran, "节拍：停止后未到下一次拍，探针未执行");
                Check(clock.Tick(), "节拍：停机后宿主时钟照常走拍（平台不因某功能停机停钟）");
                Check(!ran, "节拍：代际作废后模块的拍不再驱动队列（停机=路径不执行）");
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 7：无画面 ─────────────────────────

        /// <summary>票面验收：「隔离使已登记补丁失效」——真 Isolate = 撤该功能订阅 +
        /// 逆序释放其受跟踪资源（与停止边界同一账、同一释放律）。</summary>
        private static void V611GroupIsolation(Action<bool, string> Check)
        {
            var pocket = LitTestPocket.Open();
            using (var harness = new V611Harness())
            {
                harness.Arm();
                LitFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                var first = module.PatchRegistration;
                Check(started.Started && first != null && module.PatchTeardownDelegated,
                    "隔离 setup：第一代际已武装并经口登记");

                Check(pocket.Isolate(), "隔离：机裁决隔离本功能（撤该功能订阅 + 逆序释放其受跟踪资源）");
                Check(first != null && first.Released, "隔离：已登记补丁句柄被账释放（与停止边界同账同律）");
                Check(harness.HasLine("diagnosticId=BUE-LIT-PATCH-004"),
                    "隔离：释放行点名本功能 Harmony 身份");
                Check(Contains(pocket.MachineLines, "diagnosticId=BUE-LIFE-RELEASE"),
                    "隔离：释放走既有资源账（BUE-LIFE-RELEASE 同族行）");
                module.Stop(FeatureStopReason.RuntimeIsolated);
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        /// <summary>票面验收：「代际作废使已登记补丁失效」——走生产再启用序列
        /// （停止边界 → 新代际），逐段固定失效因果：停机后旧视图只是「未运行」，
        /// 开新代际后按代际失效（GenerationInvalid），旧句柄在停止边界已被账释放且
        /// 不被新代际复用/复活。</summary>
        private static void V611GroupGenerations(Action<bool, string> Check)
        {
            var genPocket = LitTestPocket.Open();
            using (var genHarness = new V611Harness())
            {
                genHarness.Arm();
                LitFeatureAssembly.ResetWiredModule();
                var module = genHarness.CreateArmed(genPocket.Generation, genPocket.Patching, out _);
                var handleA = module.PatchRegistration;
                var staleView = genPocket.Patching; // 旧代际的登记视图
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
                Check(afterNew.Reason == FeaturePatchRegistrationReason.GenerationInvalid
                        && afterNew.DiagnosticId == "BUE-PATCH-003",
                    "代际作废：旧代际视图登记=GenerationInvalid/BUE-PATCH-003（登记口按代际失效，不是笼统未运行），实际 "
                    + afterNew.Reason + "/" + afterNew.DiagnosticId);

                var restarted = genHarness.CreateArmed(nextGeneration, genPocket.Patching, out var secondStart);
                Check(secondStart.Started, "代际作废：新代际经 Start 重新武装");
                Check(restarted.PatchRegistration != null && !ReferenceEquals(restarted.PatchRegistration, handleA),
                    "代际作废：新代际持新句柄（旧句柄不被新代际复用/复活）");
                Check(genPocket.Accepted.Count == 2, "代际作废：两代各一枚句柄进账，实际 " + genPocket.Accepted.Count);
                var releasesBeforeRepeat = genHarness.CountLines("diagnosticId=BUE-LIT-PATCH-004");
                if (handleA != null) handleA.Dispose(); // 幂等：已释放句柄重复释放不得再拆一次
                Check(genHarness.CountLines("diagnosticId=BUE-LIT-PATCH-004") == releasesBeforeRepeat,
                    "代际作废：旧句柄重复释放幂等（不双拆）");
                var handleB = restarted.PatchRegistration;
                Check(genPocket.StopAndRelease() && handleB != null && handleB.Released,
                    "代际作废：新代际句柄在下一停止边界同样被账释放（跨代不脱账）");
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        /// <summary>05 冻结的释放故障语义（Spec 轴 R1 收口）：故障上浮平台账户释放管线
        ///（BUE-LIFE-006 隔离并继续），且不落「已拆除」释放行——释放行的含义正是
        ///「平台跑过这个补丁集的拆除且它返回了」。宿主不可达真引擎逆 JIT，用替身造故障。</summary>
        private static void V611GroupReleaseFault(Action<bool, string> Check)
        {
            var faultPocket = LitTestPocket.Open();
            using (var faultHarness = new V611Harness())
            {
                faultHarness.Arm();
                LitFeatureAssembly.ResetWiredModule();
                var faultModule = faultHarness.CreateArmed(faultPocket.Generation, faultPocket.Patching, out var faultStarted);
                var faultHandle = faultModule.PatchRegistration;
                Check(faultStarted.Started && faultHandle != null, "释放故障 setup：已登记一枚句柄");
                var releasesBefore = faultHarness.CountLines("diagnosticId=BUE-LIT-PATCH-004");
                faultHarness.RevertFaults = true;
                Check(faultPocket.StopAndRelease(), "释放故障：平台停止边界完成（故障不打断清理）");
                Check(faultHandle != null && faultHandle.Released,
                    "释放故障：句柄仍被标记为已释放（平台跑过拆除；在册状态不因故障复活）");
                Check(faultHarness.CountLines("diagnosticId=BUE-LIT-PATCH-004") == releasesBefore,
                    "释放故障：拆除故障时不得落「已拆除」释放行（不把留痕冒充真拆除）");
                Check(Contains(faultPocket.MachineLines, "diagnosticId=BUE-LIFE-006"),
                    "释放故障：故障沿平台账户释放管线浮出并隔离（BUE-LIFE-006）");
                Check(!AnyArmed(faultModule), "释放故障：在册登记仍被收回（功能开关归零，不静默服务）");
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 8：无画面 ─────────────────────────
        /// <summary>票面 Scope：「无画面下客户端补丁不武装，权威整理不丢」。判决仍是
        /// 决策门禁（诚实诊断），且缺画面不减所有权移交——权威面的句柄照进平台账。</summary>
        private static void V611GroupHeadlessAuthority(Action<bool, string> Check)
        {
            var pocket = LitTestPocket.Open();
            using (var harness = new V611Harness())
            {
                harness.Arm(headless: true);
                LitFeatureAssembly.ResetWiredModule();
                var module = harness.CreateArmed(pocket.Generation, pocket.Patching, out var started);
                Check(started.Started, "无画面 setup：模块启动");
                Check(module.RecoverPatchesInstalled && module.FastTransferPatchesInstalled,
                    "无画面：权威补丁面（入包恢复 + 快速转移）仍武装——权威整理不丢");
                Check(!module.PatchesInstalled && module.StartGateDiagnostics == "headless-ui-not-armed",
                    "无画面：客户端补丁不武装且诊断如实（决策门禁非异常门禁），实际 "
                    + module.StartGateDiagnostics);
                Check(harness.UiInstalled.Count == 0, "无画面：画面安装器零调用（不武装=不装，不是装完再拆）");
                Check(harness.RecoverInstalled.Count == 3 && harness.FastInstalled.Count == 2,
                    "无画面：权威面按 binder 序各装齐（恢复 3 + 快速转移 2）");
                Check(pocket.Accepted.Count == 1 && module.PatchTeardownDelegated,
                    "无画面：权威面的句柄仍经口登记（缺画面不减所有权移交）");

                // 权威行为仍真做：服务端角色下一次本地整理请求仍派发并被拍排空。
                LitTidyProductionAuthority.ServerRoleProbeForTests = () => true;
                try
                {
                    MainThreadDispatcher.ResetForTests();
                    Check(module.RequestLocalTidy(3, TidyMode.SameType, true) == LitTidyRequestResult.Dispatched,
                        "无画面：权威整理请求照常派发（不因缺画面停摆）");
                    module.Tick();
                    Check(MainThreadDispatcher.PendingCount == 0, "无画面：权威派发被拍排空（主线程路径在跑）");
                }
                finally
                {
                    LitTidyProductionAuthority.ServerRoleProbeForTests = null;
                }
                module.Stop(FeatureStopReason.UserDisabled);
                LitFeatureAssembly.ResetWiredModule();
            }
        }

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BetterUnturnedExperience.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("repo root not found");
            return directory.FullName;
        }

        // ───────────────────────── 测试夹具 ─────────────────────────

        /// <summary>
        /// 最小宿主组合：真总线 + 回环网络 + 三个安装器缝（画面/恢复/快速转移）——
        /// 宿主测试进程装不上真机 Glazier 与引擎补丁，安装器缝是既有家族（V5-04/05
        /// 两枚同形），本票补画面面一枚使「哪一面武装」机器可判。日志走
        /// LitFeatureAssembly.BindHostComposition 的宿主注入口（生产同路），
        /// 退出时逐项还原（含 LitRuntime 的静态缝与三条安装器缝）。
        /// </summary>
        private sealed class V611Harness : IDisposable
        {
            internal readonly List<string> Lines = new List<string>();
            internal readonly List<Type> UiInstalled = new List<Type>();
            internal readonly List<Type> RecoverInstalled = new List<Type>();
            internal readonly List<Type> FastInstalled = new List<Type>();
            internal readonly FeatureEventBus Bus = new FeatureEventBus();
            internal FeatureId Feature { get { return new FeatureId(LitFeatureId); } }

            private readonly Action<string> savedLog = LitFeatureAssembly.HostRuntimeLogSink;
            private readonly Action<string> savedError = LitFeatureAssembly.HostErrorLogSink;
            private readonly Func<bool> savedHeadless = LitFeatureAssembly.HostHeadlessDecision;
            private readonly Func<ulong> savedSteam = LitFeatureAssembly.HostLocalSteamId;
            private readonly Func<ulong, object> savedFind = LitFeatureAssembly.HostFindSteamPlayer;
            private readonly Func<Type, bool> savedRecover = InventoryTidyModule.RecoverPatchInstallerForTests;
            private readonly Func<Type, bool> savedFast = InventoryTidyModule.FastTransferPatchInstallerForTests;
            private readonly Func<Type, bool> savedUi = InventoryTidyModule.UiPatchInstallerForTests;
            private readonly Func<bool> savedRevert = InventoryTidyModule.PatchRevertForTests;
            private readonly string faultDir = Path.Combine(Path.GetTempPath(), "bue-v6-11-lit-" + Guid.NewGuid().ToString("N"));

            /// <summary>置 true：Harmony 反打失败（宿主不可达的引擎逆 JIT 故障的替身）。</summary>
            internal bool RevertFaults;

            internal V611Harness()
            {
                Directory.CreateDirectory(faultDir);
                MainThreadDispatcher.ResetForTests();
            }

            /// <summary>配置三个安装器缝与 headless 决策（在 Bootstrap/Start 前调用）。</summary>
            internal void Arm(bool headless = false, Func<Type, bool> recover = null, Func<Type, bool> fast = null, Func<Type, bool> ui = null)
            {
                LitFeatureAssembly.BindHostComposition(Lines.Add, Lines.Add, () => headless, null, null);
                InventoryTidyModule.RecoverPatchInstallerForTests = recover ?? (type => { RecoverInstalled.Add(type); return true; });
                InventoryTidyModule.FastTransferPatchInstallerForTests = fast ?? (type => { FastInstalled.Add(type); return true; });
                InventoryTidyModule.UiPatchInstallerForTests = ui ?? (type => { UiInstalled.Add(type); return true; });
                InventoryTidyModule.PatchRevertForTests = () => !RevertFaults;
            }

            /// <summary>宿主启动口袋（第 12 实参=补丁口；缺口袋传 null=老形态）。</summary>
            internal IFeatureBootstrap Bootstrap(ulong generation, IFeaturePatching pocket, IScopedFeatureSettings settings = null)
            {
                var feature = Feature;
                var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(
                    pair.First, new ContractVersion(2, 0), 6110UL);
                return new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(
                    default(FeatureScopeIdentity), generation, settings,
                    Bus.Subscriber(feature), Bus.Publisher(feature), Bus.EventRegistry(feature),
                    null, null, null, network, null, pocket);
            }

            /// <summary>装配一件已武装的模块（工厂路径 + 构造注入），返回本次代际。</summary>
            internal InventoryTidyModule CreateArmed(ulong generation, IFeaturePatching pocket, out FeatureStartResult result,
                IScopedFeatureSettings settings = null)
            {
                var module = (InventoryTidyModule)LitFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                module.ScopeDirectoryForTests = faultDir;
                module.FaultContextForTests = () => new LitFaultScopeContext("V611", 1);
                result = module.Start(Bootstrap(generation, pocket, settings));
                return module;
            }

            internal bool HasLine(string token)
            {
                for (var index = 0; index < Lines.Count; index++)
                    if (Lines[index].Contains(token)) return true;
                return false;
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
                LitFeatureAssembly.ResetWiredModule();
                LitFeatureAssembly.BindHostComposition(savedLog, savedError, savedHeadless, savedSteam, savedFind);
                InventoryTidyModule.RecoverPatchInstallerForTests = savedRecover;
                InventoryTidyModule.FastTransferPatchInstallerForTests = savedFast;
                InventoryTidyModule.UiPatchInstallerForTests = savedUi;
                InventoryTidyModule.PatchRevertForTests = savedRevert;
                LitRuntime.LogSink = null;
                LitRuntime.ErrorLogSink = null;
                MainThreadDispatcher.ResetForTests();
                try { Directory.Delete(faultDir, true); } catch { }
            }
        }
    }
}
