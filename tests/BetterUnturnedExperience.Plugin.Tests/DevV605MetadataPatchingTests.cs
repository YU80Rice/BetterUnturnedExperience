using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Events;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.NoOpFixture;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-05 红测组：显示名自报与补丁口（V6-T5 Q1/Q2/Q3/Q4 + 2026-09-18 追加裁决）。
    /// 被测外部行为：
    ///   1. 契约形状：启动口袋新增 Patching（宿主组合非空，与 MainThread 同构）；补丁口
    ///      恰一个 Register(IDisposable) → FeaturePatchRegistrationResult（句柄不新增
    ///      契约类型=BCL IDisposable）；可选元数据面=类型发现接口，旧登记接口零字段。
    ///   2. 补丁口句柄进既有资源跟踪账：经此口登记=显式受理并绑定（FeatureId, 代际）；
    ///      Stop/Isolate 边界由平台对经此口登记的句柄 UnpatchSelf（账本逆序释放）；
    ///      拒绝腿=null/非运行/过期代际/重复/容量 各有显式原因与诊断码（BUE-PATCH-00x）。
    ///   3. 显示名自报：功能经可选面自报显示名与「直接画在游戏里」；未报=面板显示
    ///      FeatureId；门口硬编码中文名对照（宿主投影的五路 if 与直显判定）删除，官方
    ///      先行者（网络双面/LIT/LIR/LHT/BII）各自自报。
    ///   4. BII 两处 Harmony（inventory-lifecycle / drag-preview）经补丁口登记为官方先行
    ///      消费：武装期两枚句柄进账，停用期平台经既有账拆两枚。
    ///   5. 探针第八步（假钩子，不碰游戏原生方法）：登记→停用→确认拆过；缺口袋或句柄
    ///      不进账时该步=红。第八步只在测试宿主开关下运行（真机第一期仍七步）。
    /// 维护锚：与 DevV602B/C/D/E、DevV604 同规约；过渡态/退役面逐条注明作废票 05。
    /// </summary>
    internal static class DevV605MetadataPatchingTests
    {
        private const string PocketFeatureId = "io.example.v605-pocket-feature";
        private const string NamedFeatureId = "io.example.v605-named-feature";
        private const string PlainFeatureId = "io.example.v605-plain-feature";
        private const string BiiFeatureId = "io.github.yu80rice.bue.better-item-interaction";
        private const string DragHarmonyId = "io.github.yu80rice.bue.drag-preview";
        private const string LifecycleHarmonyId = "io.github.yu80rice.bue.inventory-lifecycle";

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
                    catch (Exception error) { reds.Add(name + " 组异常: " + error.Message); }
                }

                Group("契约形状：补丁口上口袋，元数据面可选", () => V605GroupContractShape(Check));
                Group("补丁口：句柄进既有账与边界拆除", () => V605GroupPatchPocketAccount(Check));
                Group("显示名自报与门口硬编码对照删除", () => V605GroupDisplayNameSelfReport(Check));
                Group("BII 两处钩子经口登记", () => V605GroupBiiPatchesThroughPocket(Check));
                Group("探针第八步（假钩子登记→停用→确认拆过）", () => V605GroupProbeEighthStep(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：契约形状 ─────────────────────────

        /// <summary>V6-T5 Q1/Q2：补丁口挂在启动口袋上（IFeatureBootstrap 第 12 成员，与
        /// MainThread 的 Minor 加性先例同构）；元数据面=可选类型发现接口，绝不写进
        /// IFeatureRegistration（2.0 生态登记类型零破坏的实现在侧规则）。</summary>
        private static void V605GroupContractShape(Action<bool, string> Check)
        {
            var bootstrapProperties = typeof(IFeatureBootstrap).GetProperties();
            Check(bootstrapProperties.Length == 12,
                "IFeatureBootstrap 应恰 12 成员（DEV-V6-05 补丁口加性：11+1），实际 " + bootstrapProperties.Length);
            var patchingProperty = typeof(IFeatureBootstrap).GetProperty("Patching");
            Check(patchingProperty != null && patchingProperty.PropertyType == typeof(IFeaturePatching),
                "启动口袋应有 IFeaturePatching Patching 成员（宿主组合非空，旧模块不碰该格子仍能跑）");

            Check(typeof(IFeaturePatching).IsInterface, "IFeaturePatching 应是契约接口");
            var patchMethods = typeof(IFeaturePatching).GetMethods();
            Check(patchMethods.Length == 1 && patchMethods[0].Name == "Register"
                    && patchMethods[0].ReturnType == typeof(FeaturePatchRegistrationResult)
                    && patchMethods[0].GetParameters().Length == 1
                    && patchMethods[0].GetParameters()[0].ParameterType == typeof(IDisposable),
                "补丁口应恰一个 Register(IDisposable) -> FeaturePatchRegistrationResult"
                + "（句柄=BCL IDisposable，不新增契约句柄类型；显式结果不抛）");

            Check((byte)FeaturePatchRegistrationReason.None == 0
                    && (byte)FeaturePatchRegistrationReason.InvalidPatch == 1
                    && (byte)FeaturePatchRegistrationReason.FeatureNotRunning == 2
                    && (byte)FeaturePatchRegistrationReason.GenerationInvalid == 3
                    && (byte)FeaturePatchRegistrationReason.DuplicatePatch == 4
                    && (byte)FeaturePatchRegistrationReason.CapacityExceeded == 5,
                "FeaturePatchRegistrationReason 值冻结（None/InvalidPatch/FeatureNotRunning/GenerationInvalid/DuplicatePatch/CapacityExceeded = 0..5）");

            var metadataProperties = typeof(IFeaturePresentationRegistration).GetProperties();
            Check(typeof(IFeaturePresentationRegistration).IsInterface
                    && metadataProperties.Length == 2
                    && metadataProperties.Any(p => p.Name == "DisplayName" && p.PropertyType == typeof(string))
                    && metadataProperties.Any(p => p.Name == "DirectPresentation" && p.PropertyType == typeof(bool)),
                "可选元数据面应恰两成员（string DisplayName + bool DirectPresentation）");
            Check(typeof(IFeatureRegistration).GetProperty("DisplayName") == null
                    && typeof(IFeatureRegistration).GetProperty("DirectPresentation") == null,
                "旧登记接口不得加字段（类型发现而非接口扩张；2.0 生态登记类型不因新增成员加载失败）");
        }

        // ───────────────────────── 组 2：补丁口与既有资源账 ─────────────────────────

        /// <summary>V6-T5 Q2 + 追加裁决：经此口登记的句柄进既有 TryTrack 账（同代际、
        /// 同逆序释放、容量仍 64/代际），Stop/Isolate 时平台拆经此口登记的补丁；未经此口
        /// 的不进账、平台不保证拆（契约诚实）。每条拒绝=显式原因+诊断码，不抛。</summary>
        private static void V605GroupPatchPocketAccount(Action<bool, string> Check)
        {
            var lines = new List<string>();
            var machine = NewMachine(lines, out _, PocketFeatureId, out var feature);
            var pocket = new FeaturePatchingRuntime(machine, lines.Add);
            Check(machine.BeginStart(feature, out var generation), "补丁口 setup：代际开启");
            var view = pocket.CreateView(feature, generation);
            Check(view != null, "补丁口：视图非空（宿主保证非空，模块侧无需容忍 null 的这一格）");

            var first = new ProbePatchHandle("first");
            var accepted = view.Register(first);
            Check(accepted.Registered && accepted.Reason == FeaturePatchRegistrationReason.None
                    && accepted.DiagnosticId == "BUE-PATCH-ACCEPT" && accepted.LifecycleGeneration == generation,
                "句柄进账：经补丁口登记=显式受理+绑定代际，实际 " + accepted.Registered + "/" + accepted.Reason + "/" + accepted.DiagnosticId);
            Check(Contains(lines, "diagnosticId=BUE-LIFE-ACCEPT"),
                "句柄进账：句柄进入既有资源跟踪账（BUE-LIFE-ACCEPT 同族行，不新造第二套账）");
            Check(!first.Disposed, "句柄进账：未到停止/隔离边界不得拆除（平台在边界拆）");

            var second = new ProbePatchHandle("second");
            Check(view.Register(second).Registered, "句柄进账：第二枚句柄受理（同代际多补丁）");

            // 拒绝腿：重复（同实例不双登）与 null（显式拒，不抛）。
            Check(view.Register(first).Reason == FeaturePatchRegistrationReason.DuplicatePatch
                    && view.Register(first).DiagnosticId == "BUE-PATCH-004",
                "拒绝腿：重复句柄=DuplicatePatch/BUE-PATCH-004（不双拆）");
            Check(view.Register(null).Reason == FeaturePatchRegistrationReason.InvalidPatch
                    && view.Register(null).DiagnosticId == "BUE-PATCH-001",
                "拒绝腿：null 句柄=InvalidPatch/BUE-PATCH-001（显式拒，不抛越界异常）");

            // 停止边界：平台经既有账逆序释放（后登的先拆），两枚假钩子都被拆。
            Check(machine.BeginStop(feature, FeatureStopReason.PluginStopping), "补丁口：停止边界开启");
            Check(machine.CompleteStop(feature), "补丁口：停止边界完成");
            Check(first.Disposed && second.Disposed,
                "停用拆除：经此口登记的两枚句柄由平台在停止边界释放（first=" + first.Disposed + " second=" + second.Disposed + "）");
            Check(string.Join(",", first.Disposed ? "x" : "-") == "x"
                    && Contains(lines, "diagnosticId=BUE-LIFE-RELEASE"),
                "停用拆除：释放走既有资源账（BUE-LIFE-RELEASE 行）");

            // 停止后用旧视图：显式拒（功能非运行），不复活旧代际。
            var afterStop = view.Register(new ProbePatchHandle("late"));
            Check(afterStop.Reason == FeaturePatchRegistrationReason.FeatureNotRunning
                    && afterStop.DiagnosticId == "BUE-PATCH-002",
                "拒绝腿：停止后旧视图登记=FeatureNotRunning/BUE-PATCH-002，实际 " + afterStop.Reason + "/" + afterStop.DiagnosticId);

            // 再启用=新代际：旧视图的登记=过期代际（GenerationInvalid），新视图可用。
            Check(machine.BeginStart(feature, out var nextGeneration) && nextGeneration != generation,
                "补丁口：再启用开新代际");
            var stale = view.Register(new ProbePatchHandle("stale"));
            Check(stale.Reason == FeaturePatchRegistrationReason.GenerationInvalid
                    && stale.DiagnosticId == "BUE-PATCH-003",
                "拒绝腿：过期代际视图登记=GenerationInvalid/BUE-PATCH-003，实际 " + stale.Reason + "/" + stale.DiagnosticId);
            var fresh = pocket.CreateView(feature, nextGeneration);
            var freshHandle = new ProbePatchHandle("fresh");
            Check(fresh.Register(freshHandle).Registered, "补丁口：新代际视图可登记");
            Check(machine.BeginStop(feature, FeatureStopReason.PluginStopping) && machine.CompleteStop(feature),
                "补丁口：第二代际停止边界");
            Check(freshHandle.Disposed, "停用拆除：第二代际句柄同走既有账释放");

            // 容量腿：容量仍受既有每代际上限（64），第 65 枚显式拒。
            var capacityLines = new List<string>();
            var capacityMachine = NewMachine(capacityLines, out _, PocketFeatureId + "-capacity", out var capacityFeature);
            var capacityPocket = new FeaturePatchingRuntime(capacityMachine, capacityLines.Add);
            Check(capacityMachine.BeginStart(capacityFeature, out var capacityGeneration), "容量腿 setup：代际开启");
            var capacityView = capacityPocket.CreateView(capacityFeature, capacityGeneration);
            var acceptedCount = 0;
            for (var index = 0; index < 64; index++)
                if (capacityView.Register(new ProbePatchHandle("cap-" + index)).Registered) acceptedCount++;
            Check(acceptedCount == 64, "容量腿：每代际 64 枚受理，实际 " + acceptedCount);
            var overflow = capacityView.Register(new ProbePatchHandle("cap-overflow"));
            Check(overflow.Reason == FeaturePatchRegistrationReason.CapacityExceeded
                    && overflow.DiagnosticId == "BUE-PATCH-005",
                "容量腿：第 65 枚=CapacityExceeded/BUE-PATCH-005（容量仍受既有账上限，不新造容器）");
        }

        // ───────────────────────── 组 3：显示名自报 ─────────────────────────

        /// <summary>V6-T5 Q1：可选元数据面经类型发现（旧登记接口零字段）；自报者面板用
        /// 其显示名与直显判定，未报者显示 FeatureId 且照旧按卫星缺席降级；门口硬编码
        /// 中文名对照（宿主投影五路 if + 直显判定）删除，官方先行者各自自报。</summary>
        private static void V605GroupDisplayNameSelfReport(Action<bool, string> Check)
        {
            // a) 类型发现落进冻结目录：自报者带显示名+直显，未报者诚实留空。
            var lines = new List<string>();
            var registrations = new FeatureRegistrationRuntime();
            registrations.OpenRegistration();
            Check(registrations.Register(new NamedProbeRegistration(NamedFeatureId, "显示名甲", true)).Accepted,
                "目录投影 setup：自报功能登记受理");
            Check(registrations.Register(new PlainProbeRegistration(PlainFeatureId)).Accepted,
                "目录投影 setup：未报功能登记受理");
            Check(registrations.CompleteRuntime(), "目录投影 setup：目录冻结");
            var named = FindEntry(registrations, NamedFeatureId);
            var plain = FindEntry(registrations, PlainFeatureId);
            Check(named != null && named.DisplayName == "显示名甲" && named.DirectPresentation,
                "可选面自报：登记受理时经类型发现抄进冻结目录条目（显示名+直显）");
            Check(plain != null && string.IsNullOrEmpty(plain.DisplayName) && !plain.DirectPresentation,
                "未报显示名：目录条目诚实留空（面板回退 FeatureId，不伪造名字）");

            // b) 官方先行者自报（不再靠宿主五路 if 对照表）。
            AssertSelfReported(Check, "整理", BetterUnturnedExperience.Lit.LitFeatureAssembly.CreateRegistration(), "背包整理", true);
            AssertSelfReported(Check, "换弹", BetterUnturnedExperience.Lir.LirFeatureAssembly.CreateRegistration(), "更好的换弹体验", true);
            AssertSelfReported(Check, "尸潮", BetterUnturnedExperience.Lht.LhtFeatureAssembly.CreateRegistration(), "更好的尸潮播报", true);
            AssertSelfReported(Check, "BII", BiiFeatureAssembly.CreateRegistration(), "更好的物品交互", true);
            var networkRegistrations = NetworkModuleFeatureRegistration.CreateOfficialRegistrations();
            AssertSelfReported(Check, "网络", networkRegistrations[0], "BUE 网络模块", true);
            AssertSelfReported(Check, "V1 兼容层", networkRegistrations[1], "BUE V1 兼容层", true);

            // c) 面板消费：自报者用自报名+直显 Available；未报者=FeatureId 且按卫星缺席降级。
            var services = new BetterUnturnedExperience.ClientUi.Internal.ClientUiHostServices();
            services.GetCatalogEntries = () => new[]
            {
                (Feature: new FeatureId(NamedFeatureId), ClientUiSatellite: default(bool?),
                    SettingDescriptors: (IReadOnlyList<SettingDescriptor>)null,
                    PresentationOverride: default(Func<FeaturePresentationState>),
                    DisplayName: "显示名甲", DirectPresentation: true),
                (Feature: new FeatureId(PlainFeatureId), ClientUiSatellite: default(bool?),
                    SettingDescriptors: (IReadOnlyList<SettingDescriptor>)null,
                    PresentationOverride: default(Func<FeaturePresentationState>),
                    DisplayName: null, DirectPresentation: false)
            };
            var composition = new BetterUnturnedExperience.ClientUi.Internal.BueClientUiCompositionRoot(services);
            try
            {
                Check(composition.Initialize(false, false, true), "面板投影 setup：组合初始化");
                composition.RefreshManagementPanel();
                var panels = composition.ManagementPanel.Model.GetEntries();
                var hasNamedRow = TryFindPanelEntry(panels, NamedFeatureId, out var namedRow);
                var hasPlainRow = TryFindPanelEntry(panels, PlainFeatureId, out var plainRow);
                Check(hasNamedRow && namedRow.DisplayName == "显示名甲",
                    "面板消费：自报显示名到达面板条目（硬编码对照表退役，作废票 05）");
                Check(hasNamedRow && namedRow.Presentation.State == FeaturePresentationState.Available,
                    "面板消费：直显功能无边卫星仍判 Available（自报取代门口直显判定）");
                Check(hasPlainRow && plainRow.DisplayName == PlainFeatureId,
                    "面板消费：未报显示名=面板显示原始 FeatureId，实际 " + (hasPlainRow ? plainRow.DisplayName : "<缺>"));
                Check(hasPlainRow && plainRow.Presentation.State == FeaturePresentationState.PresentationDegraded
                        && plainRow.Presentation.DiagnosticId == "BUE-UI-SATELLITE-001",
                    "面板消费：未报直显且无卫星=照旧降级（本次不降这条门槛）");
            }
            finally
            {
                composition.Destroy();
            }

            // d) 门口硬编码对照删除（源码级：宿主投影与界面服务袋都不得再留对照表）。
            var repoRoot = FindRepoRoot();
            var projectionSource = File.ReadAllText(Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Plugin", "BueClientUiHostProjection.cs"));
            Check(!projectionSource.Contains("ResolveDisplayName") && !projectionSource.Contains("IsDirectPresentationFeature"),
                "宿主投影不得再留显示名/直显对照缝（门口硬编码对照表删除）");
            foreach (var name in new[] { "更好的物品交互", "BUE 网络模块", "BUE V1 兼容层", "背包整理", "更好的换弹体验", "更好的尸潮播报" })
                Check(!projectionSource.Contains(name),
                    "宿主投影不得再硬编码官方显示名 " + name + "（显示名由功能自报）");
            var hostServicesSource = File.ReadAllText(Path.Combine(repoRoot, "src", "BetterUnturnedExperience.ClientUi", "ClientUiHostServices.cs"));
            Check(!hostServicesSource.Contains("ResolveDisplayName") && !hostServicesSource.Contains("IsDirectPresentationFeature"),
                "界面服务袋不得再留显示名/直显注入缝（事实随目录条目跨界，作废票 05）");
            var compositionRootSource = File.ReadAllText(Path.Combine(repoRoot, "src", "BetterUnturnedExperience.ClientUi", "BueClientUiCompositionRoot.cs"));
            Check(!compositionRootSource.Contains("更好的物品交互") && !compositionRootSource.Contains("OfficialDisplayName")
                    && !compositionRootSource.Contains("IsOfficialDirectPresentationFeature"),
                "组合根不得再硬编码官方显示名或保留直显判定助手（作废票 05）");
        }

        // ───────────────────────── 组 4：BII 两处钩子经口登记 ─────────────────────────

        /// <summary>V6-T5 Q4 + 追加裁决：BII 两处 Harmony（drag-preview / inventory-lifecycle）
        /// 作为官方先行消费迁入口子——武装期两枚句柄进既有资源账（同代际），停用期平台经
        /// 该账逆序拆除；缺口袋=登记整条缺席（零句柄进账），模块照旧自管（阶段基线容忍）。</summary>
        private static void V605GroupBiiPatchesThroughPocket(Action<bool, string> Check)
        {
            var lines = new List<string>();
            var machine = NewMachine(lines, out _, BiiFeatureId, out var feature, BiiFeatureAssembly.CreateRegistration());
            var pocket = new FeaturePatchingRuntime(machine, lines.Add);
            Check(machine.BeginStart(feature, out var generation), "BII 补丁口 setup：代际开启");
            var bootstrap = ComposePocketBootstrap(machine, pocket, feature, generation, withPocket: true);
            var log = new List<string>();
            try
            {
                BiiFeatureAssembly.ResetWiredModule();
                BindArmedBii(log);
                var module = BiiFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                var started = module.Start(bootstrap);
                Check(started.Started, "BII 开工：有画面+界面缝在座=Started（T3 Q2 语义不变）");
                var accepts = CountLines(lines, "feature=" + BiiFeatureId + " generation=" + generation, "diagnosticId=BUE-LIFE-ACCEPT");
                Check(accepts == 2,
                    "BII 两处钩子：两枚补丁句柄经口登记进既有资源账（恰 2 条 BUE-LIFE-ACCEPT），实际 " + accepts);
                Check(Contains(log, DragHarmonyId) && Contains(log, LifecycleHarmonyId),
                    "BII 两处钩子：登记行使两个 Harmony 身份点名（drag-preview / inventory-lifecycle 各一）");

                var armedRig = BiiFeatureAssembly.WiredRig;
                Check(armedRig != null && armedRig.DragAdapter != null && armedRig.LifecycleAdapter != null,
                    "BII 补丁口 setup：武装 rig 持有两适配器（对照组 2 已有实例断言）");
                // 归属断言（Spec 轴 R1 收口）：受理 ⇒ 所有权移交平台账，模块侧有序拆除
                // 跳过自身 unpatch（跳过机制另由下方直构适配器对照证）；随后释放行点名
                // 两个 Harmony 身份，证明「平台真拆」而非对已拆对象的空转。
                Check(armedRig.DragPatchTeardownDelegated && armedRig.LifecyclePatchTeardownDelegated,
                    "BII 收工归属：两枚补丁集的拆除所有权移交平台账，实际 drag="
                    + armedRig.DragPatchTeardownDelegated + " lifecycle=" + armedRig.LifecyclePatchTeardownDelegated);
                AssertAdapterTeardownOwnership(Check);
                // 接线锚（宿主套件执行不到的态：真 Harmony 装不上=02D/04 记名缝隙，
                // 有序拆除路径在本宿主不可达）——源级固定「有序拆除按登记结果跳过自身
                // unpatch」，突变 M9 即以此证红。
                var rigSource = File.ReadAllText(Path.Combine(FindRepoRoot(), "src",
                    "BetterUnturnedExperience.Bii", "BiiInteractionRig.cs"));
                var lifecycleLine = rigSource.IndexOf("LifecycleAdapter.IsolateAndDetach(releasePatches: !lifecycleTeardownDelegated)", StringComparison.Ordinal);
                var dragLine = rigSource.IndexOf("DragAdapter.IsolateAndDetach(true, releasePatches: !dragTeardownDelegated)", StringComparison.Ordinal);
                Check(lifecycleLine >= 0 && dragLine >= 0,
                    "归属接线：有序拆除按登记结果跳过自身 unpatch（源级机器锚，宿主缝隙记名）");
                // 次序锚（Spec 轴 R2 收口）：生命周期适配器必须先隔离——拖拽侧的隔离会
                // 跑组件清理链，链上回调用 fail-closed 默认值；若那时任一适配器仍活，
                // 就会自拆而架空归属移交。两枚都在此前 isolated，级联成 no-op。
                Check(lifecycleLine >= 0 && dragLine >= 0 && lifecycleLine < dragLine,
                    "归属接线次序：生命周期适配器先隔离（清理链级联不得在移交前自拆），实际 lifecycle@" + lifecycleLine + " drag@" + dragLine);

                Check(machine.BeginStop(feature, FeatureStopReason.PluginStopping), "BII 收工 setup：停止边界开启");
                module.Stop(FeatureStopReason.PluginStopping);
                Check(!Contains(log, "released harmonyId=" + DragHarmonyId),
                    "BII 收工归属：模块自身有序拆除期间不得出现平台释放行（拆除尚未交接）");
                Check(machine.CompleteStop(feature), "BII 收工：停止边界完成");
                Check(Contains(log, "released harmonyId=" + DragHarmonyId) && Contains(log, "released harmonyId=" + LifecycleHarmonyId),
                    "BII 收工归属：平台释放行点名两个 Harmony 身份（平台真拆，不是模块代拆）");
                var releases = CountLines(lines, "feature=" + BiiFeatureId, "diagnosticId=BUE-LIFE-RELEASE");
                Check(releases == 2,
                    "BII 收工：平台经既有账逆序拆两枚补丁句柄（恰 2 条 BUE-LIFE-RELEASE），实际 " + releases);
            }
            finally
            {
                BiiFeatureAssembly.ResetWiredModule();
            }

            // 缺口袋：句柄不进账（未经此口的补丁平台不保证拆），模块仍开工不抛。
            var noPocketLines = new List<string>();
            var noPocketMachine = NewMachine(noPocketLines, out _, BiiFeatureId, out var noPocketFeature, BiiFeatureAssembly.CreateRegistration());
            Check(noPocketMachine.BeginStart(noPocketFeature, out var noPocketGeneration), "缺口袋 setup：代际开启");
            var noPocketBootstrap = ComposePocketBootstrap(noPocketMachine, null, noPocketFeature, noPocketGeneration, withPocket: false);
            try
            {
                BiiFeatureAssembly.ResetWiredModule();
                BindArmedBii(new List<string>());
                var module = BiiFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                Check(module.Start(noPocketBootstrap).Started, "缺口袋：BII 照旧开工（旧宿主阶段基线容忍，不新断点）");
                Check(CountLines(noPocketLines, "diagnosticId=BUE-LIFE-ACCEPT") == 0,
                    "缺口袋：零句柄进账（经此口登记才是平台保证拆除的凭据，契约诚实）");
                var bareRig = BiiFeatureAssembly.WiredRig;
                Check(bareRig != null && !bareRig.DragPatchTeardownDelegated && !bareRig.LifecyclePatchTeardownDelegated,
                    "缺口袋：零所有权移交（拆除仍由模块自管，不空等平台账）");
                module.Stop(FeatureStopReason.UserDisabled);
            }
            finally
            {
                BiiFeatureAssembly.ResetWiredModule();
            }
        }

        // ───────────────────────── 组 5：探针第八步 ─────────────────────────

        /// <summary>V6-T5 Q3：补丁口进契约 ⇒ NoOp 探针加第八步（假钩子登记→停用→确认
        /// 拆过，不碰游戏原生方法）。第八步只在测试宿主开关下运行（真机第一期仍七步，
        /// T5 Q3/T7）；缺口袋或句柄不进账时该步=红。</summary>
        private static void V605GroupProbeEighthStep(Action<bool, string> Check)
        {
            // a) 绿链：真实宿主组合（StartCatalog）下第八步 Passed，停止边界确认拆过。
            var lines = new List<string>();
            var runtime = new FeatureRegistrationRuntime();
            var previousRuntime = BueRuntimeHost.CurrentRuntime;
            var previousRecorder = BueRuntimeLog.Recorder;
            var previousProbeToggle = NoOpFeatureRegistration.PatchSeamStepEnabled;
            var settingsRoot = Path.Combine(Path.GetTempPath(), "BUE-V6-05-" + Guid.NewGuid().ToString("N"));
            BueRuntimeHost.Bind(runtime);
            BueRuntimeLog.Recorder = lines.Add;
            try
            {
                BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(settingsRoot, () => true, null);
                BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated();
                NoOpFeatureRegistration.NextProbeFault = NoOpProbeFault.None;
                NoOpFeatureRegistration.ResetLastProbe();
                NoOpFeatureRegistration.PatchSeamStepEnabled = true;
                runtime.OpenRegistration();
                Check(runtime.Phase == FeatureRegistrationPhase.RegistrationOpen, "第八步 setup：登记窗开启");
                Check(runtime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted, "第八步 setup：样例受理");
                Check(runtime.CompleteRuntime(), "第八步 setup：目录冻结");
                BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(5105UL));
                var probe = NoOpFeatureRegistration.LastProbe;
                Check(probe != null && probe.Started, "第八步 setup：探针链经真实 StartCatalog 跑完");
                Check(probe != null && probe.PatchStep == ProbeStepOutcome.Passed,
                    "第八步：假钩子经补丁口登记=Passed（宿主组合非空口袋），实际 "
                    + (probe != null ? probe.PatchStep.ToString() : "<无探针>"));
                Check(probe != null && probe.PatchRegistered && probe.PatchGenerationBound,
                    "第八步：登记受理且绑定本次代际（探针自证经口登记）");
                Check(probe != null && !probe.PatchTeardownObserved, "第八步：未到停止边界前不拆");
                BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                Check(probe != null && probe.PatchTeardownObserved,
                    "第八步：停止边界确认拆过（假钩子被平台释放，登记→停用→确认拆过成链）");
            }
            finally
            {
                NoOpFeatureRegistration.PatchSeamStepEnabled = previousProbeToggle;
                NoOpFeatureRegistration.NextProbeFault = NoOpProbeFault.None;
                BueRuntimeLog.Recorder = previousRecorder;
                BueRuntimeHost.Bind(previousRuntime);
                BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
            }

            // b) 缺口袋=红：同一模块拿到 Patching=null 的启动口袋 → 第八步 Mismatch。
            NoOpFeatureRegistration.ResetLastProbe();
            NoOpFeatureRegistration.PatchSeamStepEnabled = true;
            try
            {
                var bareBootstrap = ComposePocketBootstrap(null, null, new FeatureId(NoOpProbeFeatureId), 7UL, withPocket: false, noopIdentity: true);
                var module = NoOpFeatureRegistration.ProbeRegistration.ModuleFactory.Create();
                Check(module.Start(bareBootstrap).Started, "缺口袋 setup：模块照常开工（探针不抛）");
                var probe = NoOpFeatureRegistration.LastProbe;
                Check(probe != null && probe.PatchStep == ProbeStepOutcome.Mismatch,
                    "缺口袋=红：第八步应 Mismatch（缺口袋时红），实际 "
                    + (probe != null ? probe.PatchStep.ToString() : "<无探针>"));
                Check(probe != null && probe.StepMismatches.Any(x => x.StartsWith("patch:", StringComparison.Ordinal)),
                    "缺口袋=红：分 seam 可定位（patch 段 mismatch 详情行）");

                // c) 句柄不进账=红：说谎的口袋（答受理但不进账）→ 停止边界拆不掉。
                var lying = new LyingPatching();
                var lyingBootstrap = ComposePocketBootstrap(null, null, new FeatureId(NoOpProbeFeatureId), 8UL, withPocket: false, noopIdentity: true, pocketOverride: lying);
                module = NoOpFeatureRegistration.ProbeRegistration.ModuleFactory.Create();
                Check(module.Start(lyingBootstrap).Started, "不进账 setup：模块照常开工");
                Check(NoOpFeatureRegistration.LastProbe != null && NoOpFeatureRegistration.LastProbe.PatchStep == ProbeStepOutcome.Passed,
                    "不进账 setup：说谎口袋答复受理（探针只知道答复）");
                Check(NoOpFeatureRegistration.LastProbe != null
                        && !NoOpFeatureRegistration.LastProbe.PatchTeardownObserved,
                    "句柄不进账=红：未经账本的句柄在边界无人拆（PatchTeardownObserved 恒 false ⇒ 绿链的「确认拆过」断言即红）");
            }
            finally
            {
                NoOpFeatureRegistration.PatchSeamStepEnabled = false;
                NoOpFeatureRegistration.ResetLastProbe();
            }
        }

        private const string NoOpProbeFeatureId = "io.github.yu80rice.bue.noop";

        /// <summary>说谎的补丁口袋：答复受理但不把句柄交给任何账本——「句柄不进账时红」
        /// 那条红向的替身（真实边界的拆除由既有账驱动，谎报者拆不掉）。</summary>
        private sealed class LyingPatching : IFeaturePatching
        {
            public FeaturePatchRegistrationResult Register(IDisposable patchTeardown)
            {
                return new FeaturePatchRegistrationResult(true, FeaturePatchRegistrationReason.None, "BUE-PATCH-ACCEPT", 8UL);
            }
        }

        // ───────────────────────── 辅助 ─────────────────────────

        private sealed class ProbePatchHandle : IDisposable
        {
            private readonly string name;

            internal ProbePatchHandle(string name) { this.name = name; }

            internal string Name { get { return name; } }
            internal bool Disposed { get; private set; }

            public void Dispose() { Disposed = true; }
        }

        private static bool Contains(IReadOnlyList<string> lines, string token)
        {
            for (var index = 0; index < lines.Count; index++)
                if (lines[index].Contains(token)) return true;
            return false;
        }

        private static int CountLines(IReadOnlyList<string> lines, params string[] tokens)
        {
            var count = 0;
            for (var index = 0; index < lines.Count; index++)
            {
                var hit = true;
                for (var token = 0; token < tokens.Length; token++)
                    if (!lines[index].Contains(tokens[token])) { hit = false; break; }
                if (hit) count++;
            }
            return count;
        }

        private static FeatureLifecycleRuntime NewMachine(List<string> lines, out FeatureRegistrationRuntime registrations,
            string featureId, out FeatureId feature, IFeatureRegistration registration = null)
        {
            registrations = new FeatureRegistrationRuntime();
            registrations.OpenRegistration();
            var accepted = registrations.Register(registration ?? (IFeatureRegistration)new PlainProbeRegistration(featureId));
            if (!accepted.Accepted)
                throw new InvalidOperationException("V605 探针登记被拒: " + accepted.Reason + "/" + accepted.DiagnosticId);
            if (!registrations.CompleteRuntime())
                throw new InvalidOperationException("V605 探针目录冻结失败");
            feature = new FeatureId(featureId);
            return new FeatureLifecycleRuntime(registrations, new FeatureEventBus(), lines.Add);
        }

        private static IFeatureBootstrap ComposePocketBootstrap(FeatureLifecycleRuntime machine, FeaturePatchingRuntime pocket,
            FeatureId feature, ulong generation, bool withPocket, bool noopIdentity = false, IFeaturePatching pocketOverride = null)
        {
            var bus = new FeatureEventBus();
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 5105UL);
            var identity = noopIdentity
                ? new FeatureScopeIdentity(feature, null, null, "bue-noop", new Digest256(1UL, 2UL, 3UL, 4UL))
                : new FeatureScopeIdentity(feature, null, null, "v605-probe-set", new Digest256(1UL, 2UL, 3UL, 4UL));
            return new FeatureBootstrap(
                identity,
                generation,
                null,
                bus.Subscriber(feature),
                bus.Publisher(feature),
                bus.EventRegistry(feature),
                null,
                null,
                machine != null ? machine.CreateLifetimeView(feature, generation) : null,
                network,
                null,
                pocketOverride ?? (withPocket && pocket != null ? pocket.CreateView(feature, generation) : null));
        }

        /// <summary>DEV-V6-05 归属机制直证：模块侧有序拆除在「移交平台」时跳过自身
        /// unpatch、在「未移交」时照旧自拆（同型两适配器对照；真 Harmony 装不上是
        /// 宿主缝隙本身，不影响该分支判定）。</summary>
        private static void AssertAdapterTeardownOwnership(Action<bool, string> Check)
        {
            var delegated = NewBareDragAdapter();
            delegated.IsolateAndDetach(false, releasePatches: false);
            Check(delegated.PatchTeardownDeferredToPlatform,
                "归属机制：releasePatches=false（平台已受理登记）=模块自身 unpatch 跳过");
            var selfManaged = NewBareDragAdapter();
            selfManaged.IsolateAndDetach(false);
            Check(!selfManaged.PatchTeardownDeferredToPlatform,
                "归属机制：releasePatches=true（未登记/漂移路径）=模块照旧 fail-closed 自拆");
        }

        private static InventoryDragPreviewAdapter NewBareDragAdapter()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new ProbePlacementEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8),
                null, null, null);
            return new InventoryDragPreviewAdapter(component, new BiiCompositionMouths());
        }

        private static BetterUnturnedExperience.Core.Network.BueNetworkRuntime NewLoopbackNetwork(ulong nonce)
        {
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            return new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), nonce);
        }

        private static void BindArmedBii(List<string> log)
        {
            BiiFeatureAssembly.BindHostComposition(
                logRuntime: log.Add,
                headlessDecision: () => false,
                placementEvaluatorFactory: () => new ProbePlacementEvaluator());
            BiiFeatureAssembly.BindInterfaceComposition(
                surfaceOpened: () => { },
                surfaceClosed: () => { },
                settingsSnapshot: () => default(FeatureSettingsSnapshot));
        }

        private sealed class ProbePlacementEvaluator : IPlacementCandidateEvaluator
        {
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input) { return default(ItemPlacementPreview); }
        }

        private sealed class PlainProbeRegistration : IFeatureRegistration
        {
            private readonly FeatureId feature;

            internal PlainProbeRegistration(string featureId) { feature = new FeatureId(featureId); }

            public FeatureDefinitionArtifact Definition { get { return ProbeDefinition(feature); } }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new ProbeModuleFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        /// <summary>带可选元数据面的探针登记（类型发现路径：宿主经 as 判定发现）。</summary>
        private sealed class NamedProbeRegistration : IFeatureRegistration, IFeaturePresentationRegistration
        {
            private readonly FeatureId feature;
            private readonly string displayName;

            internal NamedProbeRegistration(string featureId, string displayName, bool directPresentation)
            {
                feature = new FeatureId(featureId);
                this.displayName = displayName;
                DirectPresentation = directPresentation;
            }

            public FeatureDefinitionArtifact Definition { get { return ProbeDefinition(feature); } }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new ProbeModuleFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
            public string DisplayName { get { return displayName; } }
            public bool DirectPresentation { get; }
        }

        private static FeatureDefinitionArtifact ProbeDefinition(FeatureId feature)
        {
            var payload = new byte[] { 66, 85, 69, 45, 86, 54, 48, 53 }; // "BUE-V605"
            // DEV-V6-06：载荷摘要改经契约公开函数（原先借用宿主私有副本，收编后单源）。
            var payloadDigest = FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload);
            return new FeatureDefinitionArtifact(feature, 1, "v605-probe-set", new Digest256(1UL, 0UL, 0UL, 31UL), payloadDigest, payload);
        }

        private sealed class ProbeModuleFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return new ProbeModule(); }
        }

        private sealed class ProbeModule : IFeatureModule
        {
            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V605-PROBE-START");
            }

            public void Stop(FeatureStopReason reason) { }
        }

        private static FeatureRegistrationEntry FindEntry(FeatureRegistrationRuntime runtime, string featureId)
        {
            var entries = runtime.Catalog.Entries;
            for (var index = 0; index < entries.Count; index++)
                if (entries[index].Definition.Feature.Value == featureId) return entries[index];
            return null;
        }

        private static bool TryFindPanelEntry(
            IReadOnlyList<BetterUnturnedExperience.ClientUi.Internal.ManagementEntryView> entries, string stableId,
            out BetterUnturnedExperience.ClientUi.Internal.ManagementEntryView entry)
        {
            for (var index = 0; index < entries.Count; index++)
                if (entries[index].StableId == stableId) { entry = entries[index]; return true; }
            entry = default(BetterUnturnedExperience.ClientUi.Internal.ManagementEntryView);
            return false;
        }

        private static void AssertSelfReported(Action<bool, string> Check, string label, IFeatureRegistration registration,
            string expectedName, bool expectedDirect)
        {
            var facet = registration as IFeaturePresentationRegistration;
            Check(facet != null,
                "官方先行者自报：" + label + " 登记应实现可选元数据面（不再靠宿主硬编码对照表）");
            if (facet == null) return;
            Check(facet.DisplayName == expectedName,
                "官方先行者自报：" + label + " 显示名应为「" + expectedName + "」，实际 " + (facet.DisplayName ?? "<null>"));
            Check(facet.DirectPresentation == expectedDirect,
                "官方先行者自报：" + label + " 直显判定应为 " + expectedDirect);
        }

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BetterUnturnedExperience.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("repo root not found");
            return directory.FullName;
        }
    }
}
