using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BetterUnturnedExperience.ClientUi;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-02E 红测组：ClientUi 与 Plugin 工程环拆除 + 双编译收缩（V6-T2 追加可见性裁决）。
    /// 被测外部行为：
    ///   1. 工程图双边摘净：ClientUi.csproj 无 Plugin 工程引用（环的界面边摘除）且 Compile 清单
    ///      收编 ListenHostProjectionReconciler.cs（检查盲区协调文件，不再只藏在宿主平铺里）；
    ///      Plugin.csproj 零外目录 Compile 平铺（Core/ClientUi 摘除）并经组装根合法边工程引用；
    ///      ClientUi.Tests 摘宿主引用（界面测试只测界面工程——同名类型不再在同一测试进程出现两份）。
    ///   2. 类型单一身份：ClientUi/Core 的类型只存在于各自工程程序集（Plugin 程序集零
    ///      ClientUi/Core 命名空间类型）——真分工程后「多工程可编、测试无类型分裂」出门形态。
    ///   3. 公开组装面组合缝：ClientUiFeatureAssembly 从空标记翻真值为唯一公开组合面——
    ///      BindHostComposition 组合注入口（宿主日志口×5/面板日志/启停 handler/机器事实/显示名/
    ///      直显判定/facet 快照/目录条目投影/设置路由×4，全部契约型签名）+ 生命周期公开成员
    ///      （BindEngineDispatcher 生产 JIT 门/BindTidyableRange/OnTidyPagesCommitted/
    ///      CreateComposition/Initialize/RefreshManagementPanel/SetDoubleInstallNotice/
    ///      DispatchPanelTick/TickDragPreview/Destroy×2/CanBindNativeUi）。
    ///      注入在座=真实消费（日志经注入记录器、诊断打标逻辑随组合根迁入界面侧、设置路由经
    ///      注入解析）；注入缺席=诚实退化（未绑定即吞，与 02B/C/D 同族契约）。
    ///   4. 禁方向拆净盲区自扫：ClientUi 源零部分限定名越界 token（IVT-to-*.Tests 豁免形态与
    ///      防火墙同规约）；友元只指 *.Tests（02A 字面量形态，本票新增 ClientUi→Plugin.Tests
    ///      一处——T2 Q6「测试对被测工程的 IVT 可保留」，防火墙豁免形态）。
    /// 维护锚：与 DevV602BLitTrueProjectTests / DevV602CLirTrueProjectTests /
    /// DevV602DLhtTrueProjectTests / DevV6FirewallFullsuiteTests 同规约。
    /// </summary>
    internal static class DevV602EClientUiPluginCycleTests
    {
        private const string FirewallScript = "Verify-ProjectFirewall.ps1";

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

                Group("工程图双边摘净（csproj 文本钉）", () => V6EGroupProjectGraphDecoupled(Check));
                Group("类型单一身份（测试进程无类型分裂）", () => V6EGroupTypeIdentitySingle(Check));
                Group("公开组装面组合缝（编译期绑定消费）", () => V6EGroupPublicCompositionFace(Check));
                Group("禁方向拆净盲区自扫与友元豁免形态", () => V6EGroupForbiddenDirectionAndFriendShape(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：工程图双边摘净 ─────────────────────────

        /// <summary>csproj 文本级钉死（与防火墙同一文本前提）：界面工程不引用宿主、宿主不平铺
        /// 已独立工程源码、界面测试只引用界面工程、检查盲区协调文件收编进界面清单。</summary>
        private static void V6EGroupProjectGraphDecoupled(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var clientUiCsproj = ReadFile(repoRoot, "src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj");
            var pluginCsproj = ReadFile(repoRoot, "src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj");
            var clientUiTestsCsproj = ReadFile(repoRoot, "tests/BetterUnturnedExperience.ClientUi.Tests/BetterUnturnedExperience.ClientUi.Tests.csproj");
            var pluginTestsCsproj = ReadFile(repoRoot, "tests/BetterUnturnedExperience.Plugin.Tests/BetterUnturnedExperience.Plugin.Tests.csproj");

            // 环的界面边摘除：ClientUi 不再 ProjectReference Plugin（作废票 02E——02A 骨架期过渡边）。
            Check(!clientUiCsproj.Contains("BetterUnturnedExperience.Plugin.csproj"),
                "工程环摘除失败: ClientUi.csproj 仍引用 Plugin 工程（02E 票面 Scope①）");
            Check(clientUiCsproj.Contains("BetterUnturnedExperience.Contracts.csproj"),
                "界面工程应仍经契约工程取得契约类型（跨工程只走契约）");

            // 检查盲区协调文件收编：ListenHostProjectionReconciler.cs 进 ClientUi 工程清单，
            // 不再只藏在宿主平铺里（R2 清单漂移的修复锚）。
            Check(Regex.IsMatch(clientUiCsproj, "<Compile\\s+Include=\"ListenHostProjectionReconciler\\.cs\""),
                "清单收编失败: ListenHostProjectionReconciler.cs 不在 ClientUi 工程清单（02E 票面 Scope①）");

            // 宿主零平铺：Core/ClientUi 源码不再 Compile Link 进宿主（R4 摘净），改经组装根
            // 合法边工程引用组装（02E 票面 Scope②）。
            Check(!Regex.IsMatch(pluginCsproj, "<Compile\\s+Include=\"\\.\\.\\\\BetterUnturnedExperience\\."),
                "平铺摘除失败: Plugin.csproj 仍有外目录 Compile Link（02E 票面 Scope②）");
            Check(pluginCsproj.Contains("BetterUnturnedExperience.Core.csproj"),
                "组装根缺工程引用: Plugin.csproj 应工程引用 Core（平铺改工程引用）");
            Check(pluginCsproj.Contains("BetterUnturnedExperience.ClientUi.csproj"),
                "组装根缺工程引用: Plugin.csproj 应工程引用 ClientUi（平铺改工程引用）");

            // 界面测试摘宿主引用：同名类型不再在同一测试进程出现两份（02E 票面 Scope③；
            // T2 Q6 禁令的前提=宿主嵌入界面源码，摘嵌后无重叠、禁令只剩单向边本身）。
            Check(!clientUiTestsCsproj.Contains("BetterUnturnedExperience.Plugin.csproj"),
                "双引用摘除失败: ClientUi.Tests 仍引用 Plugin 工程（02E 票面 Scope③）");
            Check(clientUiTestsCsproj.Contains("BetterUnturnedExperience.ClientUi.csproj"),
                "界面测试应引用界面工程本体");
            Check(!clientUiTestsCsproj.Contains("BetterUnturnedExperience.Core.csproj"),
                "界面测试不得新增 Core 引用（烟测的 PlacementCandidateEvaluator 改测试替身，"
                + "引用面保持「界面工程+契约」不变）");

            // 测试工程引用无重叠（类型分裂禁令的前提核对）：Plugin.Tests 引用 ClientUi/Core 时
            // 宿主必须已不嵌其源码——本组前两条断言成立时本断言才允许在册。
            Check(pluginTestsCsproj.Contains("BetterUnturnedExperience.ClientUi.csproj"),
                "Plugin.Tests 应引用 ClientUi 工程（宿主不再内嵌界面类型后，harness 经工程引用+友元取类型）");
        }

        // ───────────────────────── 组 2：类型单一身份 ─────────────────────────

        /// <summary>真分工程后每个类型只有一份身份：ClientUi/Core 的类型住在各自程序集里，
        /// Plugin 程序集不再内嵌任何外工程命名空间类型——同名类型不再在同一测试进程出现两份。</summary>
        private static void V6EGroupTypeIdentitySingle(Action<bool, string> Check)
        {
            // ClientUi 类型住在 ClientUi 程序集（迁移前住在 Plugin 内嵌副本里=类型分裂根源）。
            Check(typeof(ManagementPanelModel).Assembly.GetName().Name == "BetterUnturnedExperience.ClientUi",
                "类型分裂: ClientUi 类型仍在程序集 " + typeof(ManagementPanelModel).Assembly.GetName().Name
                + "（应为 BetterUnturnedExperience.ClientUi——宿主摘平铺后单源单编译）");
            Check(typeof(ListenHostProjectionReconciler).Assembly.GetName().Name == "BetterUnturnedExperience.ClientUi",
                "类型分裂: 检查盲区协调文件 ListenHostProjectionReconciler 未住进 ClientUi 程序集");

            // Core 类型住在 Core 程序集（宿主不再内嵌 Core 源码）。
            Check(typeof(CoreAssemblyMarker).Assembly.GetName().Name == "BetterUnturnedExperience.Core",
                "类型分裂: Core 类型仍在程序集 " + typeof(CoreAssemblyMarker).Assembly.GetName().Name
                + "（应为 BetterUnturnedExperience.Core）");

            // Plugin 程序集零外工程命名空间类型（正反双向钉死：任何回潮平铺立即可见）。
            var pluginAssembly = typeof(BetterUnturnedExperiencePlugin).Assembly;
            var foreignNamespaces = pluginAssembly.GetTypes()
                .Select(t => t.Namespace ?? string.Empty)
                .Where(n => n.StartsWith("BetterUnturnedExperience.ClientUi", StringComparison.Ordinal)
                    || n.StartsWith("BetterUnturnedExperience.Core", StringComparison.Ordinal))
                .Distinct().ToList();
            Check(foreignNamespaces.Count == 0,
                "宿主程序集仍含外工程命名空间类型: " + string.Join(", ", foreignNamespaces));
        }

        // ───────────────────────── 组 3：公开组装面组合缝 ─────────────────────────

        /// <summary>ClientUiFeatureAssembly 从空标记翻真值为唯一公开组合面（编译期绑定消费，
        /// 不用反射按名锁缝）。注入在座=真实消费；注入缺席=诚实退化（未绑定即吞）。服务实例
        /// 由 CreateComposition/组合根构造时快照，静态 pending 不跨构造串味。</summary>
        private static void V6EGroupPublicCompositionFace(Action<bool, string> Check)
        {
            // ① 缺席（pending=null）：公开面创建与门禁诚实退化。宿主测试路径不调
            //    BindEngineDispatcher（生产 JIT 门），引擎路径保持静默 no-op。
            ClientUiFeatureAssembly.ResetPendingHostCompositionForTests();
            Check(ClientUiFeatureAssembly.CreateComposition(), // DEV-V6-04: 工厂随 BII 组件迁 Bii（作废票 04）
                "组合缝: 未注入宿主服务也应可创建组合（诚实退化路径，不抛不伪造）");
            Check(!ClientUiFeatureAssembly.Initialize(true, false, false),
                "组合缝: batch 门禁应拒绝合成（CanCompose=available && !batch && !headless）");
            ClientUiFeatureAssembly.DestroyComposition();

            // ② 在座（注入消费）：记录型注入 + 组合根真实消费。
            var runtimeLines = new List<string>();
            var errorLines = new List<string>();
            var toggleCalls = new List<string>();
            var services = new ClientUiHostServices();
            services.LogRuntime = line => runtimeLines.Add(line);
            services.LogError = line => errorLines.Add(line);
            services.FeatureToggleHandler = (feature, enabled) => { toggleCalls.Add(feature.Value + "=" + enabled); return true; };
            // DEV-V6-05（作废票 05）：显示名与直显判定随目录条目行跨界，两条专用注入缝
            // （ResolveDisplayName / IsDirectPresentationFeature）退役——本组改以行内事实驱动
            // 同一条「注入在座=真实消费」判据（宿主投影仍然注入，消费点仍是同一投影函数）。
            services.GetCatalogEntries = () => new[]
            {
                (Feature: new FeatureId("io.example.panelrow"), ClientUiSatellite: default(bool?),
                 SettingDescriptors: (IReadOnlyList<SettingDescriptor>)new SettingDescriptor[0],
                 PresentationOverride: default(Func<FeaturePresentationState>),
                 DisplayName: "显示名甲", DirectPresentation: false)
            };
            services.TryGetMachineStatus = feature => default(FeatureStatusView?);
            services.GetFacetSnapshot = (feature, descriptors) => default(FeatureSettingsSnapshot);

            var composition = new BueClientUiCompositionRoot(services); // DEV-V6-04: 评估器工厂随组件迁 Bii（作废票 04）
            Check(composition.Initialize(false, false, true), "在座组合根应通过门禁合成（客户端环境）");
            // 诊断打标绑定与迁移前同时序（拖拽配器之后、清单配器可发射之前）；测试直驱该缝。
            BueClientUiCompositionRoot.BindDiagnosticSink(services);
            composition.RefreshManagementPanel();
            var entries = composition.ManagementPanel.Model.GetEntries();
            Check(entries.Any(e => e.DisplayName == "显示名甲"),
                "目录条目投影应经注入消费（显示名随目录行跨界，专用注入缝退役——作废票 05），实际 rows="
                + string.Join("|", entries.Select(e => e.DisplayName)));

            // 诊断打标逻辑随组合根迁入界面侧：Error 级诊断经注入 logError 到达，缺 diagnosticId=
            // 的行补 BUE-CLIENTUI-001、自带 diagnosticId= 的行原样透传（旧宿主绑定 lambda 的语义逐字保留）。
            ClientUiCompositionRoot.EmitDiagnostic("boom reason=x", ClientUiCompositionRoot.ClientUiDiagnosticLevel.Error);
            ClientUiCompositionRoot.EmitDiagnostic("carries diagnosticId=BUE-X-9", ClientUiCompositionRoot.ClientUiDiagnosticLevel.Debug);
            Check(errorLines.Count == 1 && errorLines[0] == "[BUE-CLIENTUI] boom reason=x diagnosticId=BUE-CLIENTUI-001",
                "组合诊断应经注入 logError 到达并保留打标语义，实际 " + string.Join("|", errorLines));
            Check(runtimeLines.Any(l => l.Contains("carries diagnosticId=BUE-X-9")),
                "Debug 级诊断应经注入 logRuntime 到达且自带 diagnosticId 的行不重复打标");

            // 启停 handler 注入：面板模型 ONE seam 直达注入记录器。
            Check(composition.ManagementPanel.Model.FeatureToggleHandler(new FeatureId("io.example.t"), true),
                "启停 handler 应经注入消费");
            Check(toggleCalls.Count == 1 && toggleCalls[0] == "io.example.t=True",
                "启停注入记录应恰一跳，实际 " + string.Join("|", toggleCalls));

            // ③ ListenHost 日志口：协调文件的宿主日志改注入缝——修复路径经注入 logRuntime 到达
            //    （迁移前为直呼 BueRuntimeLog=环的源码边）。
            var reconcileLines = new List<string>();
            // 协调器是静态缝持有者（引擎分派器/测试钩子同族）——日志口经公开面 BindHostComposition
            // 绑定（重绑即换源；absent=吞，与 02D LhtRuntime 同族）。
            var previousReconcileLog = ListenHostProjectionReconciler.HostLogSink;
            ClientUiFeatureAssembly.BindHostComposition(logRuntime: reconcileLines.Add);
            try
            {
                ClientUiFeatureAssembly.BindTidyableRange(2, 6);
            var repaired = ListenHostProjectionReconciler.ReconcileRange(2, 2, page => new HarnessInexactPageView());
            Check(repaired == 1, "对账决策应修复不精确页，实际 repaired=" + repaired);
            Check(reconcileLines.Any(l => l.Contains("listen-host 投影对账修复") && l.Contains("diagnosticId=BUE-LIT-001")),
                "对账修复日志应经注入 logRuntime 到达（环的源码边摘除），实际 " + string.Join("|", reconcileLines));
            }
            finally { ListenHostProjectionReconciler.HostLogSink = previousReconcileLog; }

            // ④ 公开缝转发：OnTidyPagesCommitted 经 ReconcileHook 可观察（宿主转达口的接收端）。
            var relayCalls = new List<string>();
            ListenHostProjectionReconciler.ReconcileHook = (f, l) => relayCalls.Add(f + "-" + l);
            try
            {
                ClientUiFeatureAssembly.OnTidyPagesCommitted(2, 6);
                Check(relayCalls.Count == 1 && relayCalls[0] == "2-6",
                    "OnTidyPagesCommitted 公开缝应转发协调器，实际 " + string.Join("|", relayCalls));
            }
            finally { ListenHostProjectionReconciler.ReconcileHook = null; }

            composition.Destroy();
        }

        // ───────────────────────── 组 4：禁方向自扫与友元形态 ─────────────────────────

        /// <summary>ClientUi 源零部分限定名越界 token（含注释/字符串，IVT-to-*.Tests 豁免形态与
        /// 防火墙 Get-ScanText 同规约）；友元只指 *.Tests（02A 字面量形态；本票新增
        /// ClientUi→Plugin.Tests——T2 Q6 测试友元豁免，防火墙同等豁免）。</summary>
        private static void V6EGroupForbiddenDirectionAndFriendShape(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var clientUiDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.ClientUi");
            var forbiddenRegex = new Regex(@"BetterUnturnedExperience\.(Plugin|Core|Lit|Lir|Lht)\b");
            var ivtRegex = new Regex("InternalsVisibleTo\\s*\\(\\s*\"([^\"]+)\"\\s*\\)");
            var hits = new List<string>();
            foreach (var file in Directory.GetFiles(clientUiDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("\\bin\\") || file.Contains("\\obj\\")) continue;
                var text = File.ReadAllText(file);
                // 与防火墙 Get-ScanText 同规约：IVT 指向 *.Tests 的声明串先抹除（等长空格），再扫 token。
                var scan = ivtRegex.Replace(text, m => m.Groups[1].Value.EndsWith(".Tests", StringComparison.Ordinal)
                    ? m.Value.Replace(m.Groups[1].Value, new string(' ', m.Groups[1].Length))
                    : m.Value);
                foreach (Match m in forbiddenRegex.Matches(scan))
                {
                    var line = ((scan.Substring(0, m.Index).Split('\n')).Length);
                    hits.Add(Path.GetFileName(file) + ":" + line + " " + m.Value);
                }
            }
            Check(hits.Count == 0,
                "ClientUi 源存在部分限定名越界 token（禁方向：宿主/核心/功能工程）: " + string.Join("; ", hits));

            // 友元形态：恰两个 *.Tests 目标（自有测试 + 宿主 harness），无任何非 Tests 友元。
            var assemblyInfo = File.ReadAllText(Path.Combine(clientUiDir, "Properties", "AssemblyInfo.cs"));
            var ivtTargets = ivtRegex.Matches(assemblyInfo).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Check(ivtTargets.Count == 2
                    && ivtTargets.Contains("BetterUnturnedExperience.ClientUi.Tests")
                    && ivtTargets.Contains("BetterUnturnedExperience.Plugin.Tests"),
                "ClientUi 友元应恰指两个 *.Tests 目标（自有+宿主 harness；T2 Q6 测试友元豁免），实际 "
                + string.Join(", ", ivtTargets));
        }

        // ───────────────────────── 辅助 ─────────────────────────

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BetterUnturnedExperience.sln")))
                dir = dir.Parent;
            if (dir == null) throw new InvalidOperationException("未找到仓库根（BetterUnturnedExperience.sln）");
            return dir.FullName;
        }

        private static string ReadFile(string repoRoot, string relativePath)
        {
            return File.ReadAllText(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        /// <summary>契约型评估替身：固定返回候选预览（组合根工厂缝的消费探针，不做放置语义断言）。</summary>
        private sealed class HarnessPlacementEvaluator : IPlacementCandidateEvaluator
        {
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                return new ItemPlacementPreview(input.DragGeneration, PlacementPreviewState.Candidate,
                    input.Source, input.ItemWidth, input.ItemHeight, PlacementReason.None);
            }
        }

        /// <summary>对账不精确页替身：权威 1 件、渲染 0 件 → IsExact=false → 走修复+日志路径。</summary>
        private sealed class HarnessInexactPageView : IInventoryProjectionPageView
        {
            public byte Page { get { return 2; } }
            public int AuthoritativeCount { get { return 1; } }
            public object AuthoritativeAt(int index) { return new object(); }
            public int RenderedCount { get { return 0; } }
            public object RenderedJarAt(int index) { return null; }
            public int PendingCount { get { return 0; } }
            public object PendingJarAt(int index) { return null; }
            public void RepairFromAuthoritative() { }
        }
    }
}

