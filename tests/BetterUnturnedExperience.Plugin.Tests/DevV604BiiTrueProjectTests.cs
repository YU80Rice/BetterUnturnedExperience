using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-04 红测组：BII（更好的物品交互）真分工程迁移+开工收工成真（V6-T3）。
    /// 被测外部行为：
    ///   1. 公开组装类型缝翻真值：BiiFeatureAssembly.CreateRegistration() 返回真登记体
    ///      （冻结身份/定义载荷/最低契约/工厂/界面卫星注册），宿主 facade 经同一缝取登记；
    ///      02A 骨架期「调用即抛并点名迁移票」与 Plugin 侧空壳 Module（Start 返回 default、
    ///      Stop 空体）的过渡态终止——空壳不得保留（T3 Q2）。
    ///   2. 模块开工收工：Start 创建并组装组件+两个适配器（拖拽预览/库存表面生命周期），
    ///      Stop 拆自身补丁并复位；headless 门禁注入在座=true 或界面缝缺席 → 开工返回
    ///      Started 但零武装、不创建界面、不打库存界面补丁、不抛（T3 Q5，ADR 0002 §7）。
    ///      开工失败走既有隔离（StartCatalog 语义零改动）。真 Harmony 武装行为测试宿主
    ///      不可达（updateDraggedItem/PlayerUI.Update 补丁=SDG 面），由 U3DS 实机覆盖
    ///      （02D 工厂武装同款记名缝隙）。
    ///   3. Bii 清单自持与宿主解嵌：Bii csproj 清单恰等目录文件集（R2）；ClientUi csproj
    ///      不再含迁移文件；组合根不再握适配器字段与创建（宿主不握适配器实例，T3 Q1）。
    ///   4. 禁方向拆净：Bii 源码零 ClientUi/Plugin/Core fqn token（R1 同款正则自扫）；
    ///      ClientUi 源码零 Bii token；防火墙真实仓库守卫零命中。
    ///   5. 组合注入消费与宿主环公开缝：注入在座=真实消费（日志到达注入记录器、表面协调
    ///      委托被调、设置快照被消费）；注入缺席=诚实退化（不武装不抛）；宿主 TickPreview/
    ///      IsolateAdapters 经 Bii 公开缝（ClientUiFeatureAssembly.TickDragPreview/
    ///      IsolateInventoryAdapters 过渡公开成员退役，作废票 04）。
    /// 维护锚：与 DevV602B/C/D/ETrueProjectTests 同规约，过渡态断言注明作废票。
    /// </summary>
    internal static class DevV604BiiTrueProjectTests
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

                Group("公开组装类型缝返回真登记体", () => V604GroupSeamReturnsRealRegistration(Check));
                Group("模块开工收工与 headless 门禁", () => V604GroupStartStopAndHeadlessGate(Check));
                Group("Bii 清单自持与宿主解嵌", () => V604GroupManifestOwnsBiiSources(Check));
                Group("禁方向拆净与组合根解嵌", () => V604GroupForbiddenDirectionsRemoved(Check));
                Group("组合注入消费与宿主环公开缝", () => V604GroupCompositionInjectionAndHostSeams(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：公开缝返回真登记体 ─────────────────────────

        /// <summary>02A 骨架把登记缝定为 CreateRegistration() -> IFeatureRegistration（签名冻结，
        /// 04 迁入真登记体后缝体翻真值、签名不变——作废票：04）。宿主 facade 必须经同一缝取
        /// 登记且不得保留私有登记体（空壳 Module 不得保留，T3 Q2）。BII 登记带界面卫星注册
        /// （bue-clientui-embedded，与迁移前同值——组件即卫星本体）。</summary>
        private static void V604GroupSeamReturnsRealRegistration(Action<bool, string> Check)
        {
            IFeatureRegistration registration;
            try
            {
                registration = BiiFeatureAssembly.CreateRegistration();
            }
            catch (Exception error)
            {
                Check(false, "公开组装类型缝应返回真登记体（02A 骨架调用即抛的过渡态已终止，作废票 04）: "
                    + error.GetType().Name + ": " + error.Message);
                return;
            }
            Check(registration != null, "公开缝不得返回 null（真登记体在 Bii 工程内自持）");
            Check(PayloadText(registration.Definition) == "BUE-BII-V1",
                "登记定义载荷应为文档化的 'BUE-BII-V1' 文本，实际 " + PayloadText(registration.Definition));
            Check(registration.Definition.Feature.Value == "io.github.yu80rice.bue.better-item-interaction",
                "登记身份应为冻结的 BII FeatureId，实际 " + registration.Definition.Feature.Value);
            Check(registration.MinimumBueContract.Major == 2 && registration.MinimumBueContract.Minor == 0,
                "最低契约应与 (2,0) 门对齐");
            Check(registration.ModuleFactory != null, "模块工厂应在位（宿主启动路径经工厂装配）");
            Check(registration.ClientUi != null && registration.ClientUi.SatelliteId == "bue-clientui-embedded",
                "界面卫星注册应与迁移前同值（组件即卫星本体，T3 Q4 面板铬留界面）");

            // 空壳不得保留：Plugin 侧 facade 不再持有私有 Registration/ModuleFactory/Module
            // （迁移前 OfficialFeatureRegistration.cs:19-52 的空壳 Module 已随登记体迁 Bii）。
            var repoRoot = FindRepoRoot();
            var facadeSource = File.ReadAllText(Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Plugin", "OfficialFeatureRegistration.cs"));
            Check(!Regex.IsMatch(facadeSource, "private sealed class Registration\\b")
                    && !Regex.IsMatch(facadeSource, "private sealed class Module\\b")
                    && !Regex.IsMatch(facadeSource, "default\\(FeatureStartResult\\)"),
                "空壳不得保留：宿主 facade 不再持有私有登记体/空壳 Module（T3 Q2，作废票 04）");
        }

        // ───────────────────────── 组 2：开工收工与 headless 门禁 ─────────────────────────

        /// <summary>T3 Q2/Q5：Start 创建并组装组件+两个适配器；Stop 拆自身并复位；
        /// headless 门禁（注入 true 或界面缝缺席）→ 开工返回 Started、零武装、不抛；
        /// 真补丁武装测试宿主不可达（U3DS 实机覆盖，02D 同款记名缝隙）。</summary>
        private static void V604GroupStartStopAndHeadlessGate(Action<bool, string> Check)
        {
            try
            {
                // 缺席基线复位：无任何绑定 → 门禁缺席=fail-closed 不武装。
                BiiFeatureAssembly.ResetWiredModule();

                var registration = BiiFeatureAssembly.CreateRegistration();
                var module = registration.ModuleFactory.Create();
                Check(module != null, "工厂应产出 BII 模块真体（空壳不得保留，T3 Q2）");

                // headless 门禁缺席=fail-closed：开工不抛、不武装。
                var headlessResult = module.Start(ComposeTestBootstrap());
                Check(headlessResult.Started,
                    "没画面/门禁缺席时开工仍应成功返回 Started（T3 Q5：不抛）");

                // headless 门禁显式 true：开工成功、零武装。界面缝同时绑上——否则
                // 「界面缝缺席」分支会先行拦下，headless 门禁本身不被区分（M4 突变
                // 咬出的信号缺口，本处补齐：只有门禁能解释零武装）。
                BiiFeatureAssembly.ResetWiredModule();
                BiiFeatureAssembly.BindHostComposition(
                    logRuntime: line => { },
                    headlessDecision: () => true,
                    placementEvaluatorFactory: () => new ProbePlacementEvaluator());
                BiiFeatureAssembly.BindInterfaceComposition(
                    surfaceOpened: () => { },
                    surfaceClosed: () => { },
                    settingsSnapshot: () => default(FeatureSettingsSnapshot));
                registration = BiiFeatureAssembly.CreateRegistration();
                module = registration.ModuleFactory.Create();
                var explicitHeadless = module.Start(ComposeTestBootstrap());
                Check(explicitHeadless.Started, "headless 门禁 true 时开工应成功（T3 Q5）");
                Check(BiiFeatureAssembly.WiredRig == null,
                    "headless 门禁 true 时不得组装界面 rig（不创建界面；界面缝在座故只有门禁可解释零武装）");
                module.Stop(FeatureStopReason.UserDisabled);
                Check(BiiFeatureAssembly.WiredRig == null,
                    "headless 收工应保持零武装（没有可拆的界面面）");

                // 武装路径：headless=false+界面缝在座 → Start 组装组件+两个适配器（实例在位、
                // 门禁探测完成）；不调 Activate（Harmony patch=SDG 面，测试宿主不可达——
                // 02D 工厂武装同款记名缝隙，真武装由 U3DS 实机覆盖）。
                BiiFeatureAssembly.ResetWiredModule();
                var runtimeLog = new List<string>();
                BiiFeatureAssembly.BindHostComposition(
                    logRuntime: runtimeLog.Add,
                    headlessDecision: () => false,
                    placementEvaluatorFactory: () => new ProbePlacementEvaluator());
                BiiFeatureAssembly.BindInterfaceComposition(
                    surfaceOpened: () => { },
                    surfaceClosed: () => { },
                    settingsSnapshot: () => new FeatureSettingsSnapshot(
                        new FeatureId("io.github.yu80rice.bue.better-item-interaction"), 1,
                        SettingRevisionScope.ClientPreference, 0, SettingSyncState.Unavailable,
                        SettingSnapshotSource.SafeDefault, new SettingEntryView[0]));
                registration = BiiFeatureAssembly.CreateRegistration();
                module = registration.ModuleFactory.Create();
                var armed = module.Start(ComposeTestBootstrap());
                Check(armed.Started, "有画面+界面缝在座时开工应成功（T3 Q2）");
                var rig = BiiFeatureAssembly.WiredRig;
                Check(rig != null, "武装开工应组装界面 rig（组件+两个适配器）");
                if (rig != null)
                {
                    Check(rig.Component != null, "rig 应持有 BII 组件（决策+投影状态机）");
                    Check(rig.DragAdapter != null, "rig 应持有拖拽预览适配器（两适配器之一）");
                    Check(rig.LifecycleAdapter != null, "rig 应持有库存表面生命周期适配器（两适配器之二）");
                }

                // 收工=拆除自身：Stop 后 rig 复位且两适配器进入隔离态（补丁拆除代码在
                // 适配器内 UnpatchSelf，T3 Q2 裁决引用 InventoryDragPreviewAdapter.cs:349 /
                // Lifecycle:1144）。隔离态断言在测试宿主可观察（Activate 的 Harmony 补丁
                // 面不可达——02D 同款缝隙，真武装由 U3DS 实机覆盖）。
                var stoppedComponent = rig != null ? rig.Component : null;
                var revisionBeforeStop = stoppedComponent != null ? stoppedComponent.Lifecycle.StateRevision : 0UL;
                module.Stop(FeatureStopReason.UserDisabled);
                Check(BiiFeatureAssembly.WiredRig == null,
                    "收工应复位 rig 簿记（Stop=适配器 UnpatchSelf+组件销毁，T3 Q2）");
                // Teardown 实达的可观察点=组件生命周期机推进（OnUiDestroyed→runtime.Stop→
                // BeginStopping 转移使 StateRevision 前进）。终态在测试宿主可为 Isolated
                // （Arm 期组件隔离已把清理链跑失败，CompleteStopped 如实回落 Isolated）——
                // 故断言「修订前进」而非「终态 Stopped」；rig 字段被 Teardown 清空，组件
                // 引用在 Stop 前捕获。
                Check(stoppedComponent != null && stoppedComponent.Lifecycle.StateRevision > revisionBeforeStop,
                    "收工应驱动组件生命周期机推进（Teardown 实达，T3 Q2），实际 revision "
                    + (stoppedComponent != null ? stoppedComponent.Lifecycle.StateRevision + " vs " + revisionBeforeStop : "component=null"));

                // 组装器日志经注入口（组合注入消费的最小实证——组 4 有完整面）。
                Check(runtimeLog.Count > 0,
                    "开工日志应到达注入记录器（注入在座=真实消费）");
            }
            finally
            {
                BiiFeatureAssembly.ResetWiredModule();
            }
        }

        /// <summary>测试宿主 bootstrap：与 02D 红测同族（FeatureEventBus+FeatureBootstrap+
        /// loopback 网络；BII 模块本体不消费网络，参数仅为满足合成面）。</summary>
        private static IFeatureBootstrap ComposeTestBootstrap()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.better-item-interaction");
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 4702UL);
            return new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(
                default(FeatureScopeIdentity), 1UL, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature),
                null, null, null, network);
        }

        private static string PayloadText(FeatureDefinitionArtifact definition)
        {
            var bytes = new byte[definition.CanonicalPayload.Count];
            for (var index = 0; index < bytes.Length; index++) bytes[index] = definition.CanonicalPayload[index];
            return Encoding.ASCII.GetString(bytes);
        }

        internal static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BetterUnturnedExperience.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("repo root not found");
            return directory.FullName;
        }

        // ───────────────────────── 组 3：清单自持与宿主解嵌 ─────────────────────────

        /// <summary>真分工程：Bii csproj 的自有 Compile 清单恰等目录文件集（R2 收编，作废票：
        /// 04）；ClientUi csproj 摘除全部迁移文件；组合根源码不再握适配器字段/创建/公开转发
        /// （宿主不握适配器实例，T3 Q1——02E 后创建点在组合根，本票移交 Bii 模块 Start）。</summary>
        private static void V604GroupManifestOwnsBiiSources(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var biiDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Bii");
            var biiCsprojPath = Path.Combine(biiDir, "BetterUnturnedExperience.Bii.csproj");
            var clientUiCsprojPath = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.ClientUi",
                "BetterUnturnedExperience.ClientUi.csproj");
            Check(File.Exists(biiCsprojPath) && File.Exists(clientUiCsprojPath), "工程文件缺失（仓库结构破坏）");

            var biiCsproj = File.ReadAllText(biiCsprojPath);
            var clientUiCsproj = File.ReadAllText(clientUiCsprojPath);

            // Bii 目录文件集（递归 .cs，排除 bin/obj，正斜杠——与防火墙 R2 同口径）。
            var dirFiles = Directory.GetFiles(biiDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .Select(f => f.Substring(biiDir.Length + 1).Replace('\\', '/'))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();
            var includes = Regex.Matches(biiCsproj, "<Compile\\b[^>]*>").Cast<Match>()
                .Select(m => Regex.Match(m.Value, "\\bInclude\\s*=\\s*\"([^\"]*)\""))
                .Where(m => m.Success).Select(m => m.Groups[1].Value.Replace('\\', '/'))
                .Where(i => !i.StartsWith("..", StringComparison.Ordinal))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();

            var missing = dirFiles.Except(includes).ToList();
            var stale = includes.Except(dirFiles).ToList();
            Check(missing.Count == 0 && stale.Count == 0,
                "Bii csproj 自有清单应恰等目录文件集（R2 收编，作废票 04）"
                + (missing.Count > 0 ? "；在目录、不在清单: " + string.Join(", ", missing) : "")
                + (stale.Count > 0 ? "；在清单、不在目录: " + string.Join(", ", stale) : ""));
            Check(dirFiles.Count > 5,
                "Bii 工程应承接 BII 拖入子系统文件群（骨架期单文件过渡态终止，作废票 04），实际 " + dirFiles.Count + " 个源文件");

            // ClientUi 解嵌：迁移文件不再在界面工程清单/目录。
            string[] migrated =
            {
                "ItemInteractionUiComponent.cs", "InventoryDragPreviewAdapter.cs",
                "InventorySurfaceLifecycleAdapter.cs", "NativeInventoryInteractionAdapter.cs",
                "NativeItemGridOccupancySnapshot.cs", "InventoryLifecycle.cs",
                "InventoryPreviewWiring.cs", "InventoryDragPresenter.cs",
                "InventoryProjectionRelay.cs", "InventoryProjectionSink.cs",
                "BetterItemInteractionLifecycle.cs"
            };
            foreach (var file in migrated)
            {
                Check(!clientUiCsproj.Contains(file),
                    "ClientUi csproj 不得再含迁移文件 " + file + "（作废票 04）");
                Check(!File.Exists(Path.Combine(repoRoot, "src", "BetterUnturnedExperience.ClientUi", file)),
                    "ClientUi 目录不得再含迁移文件 " + file + "（作废票 04）");
            }

            // 组合根解嵌：适配器字段/创建/公开转发全部退役（迁移前
            // BueClientUiCompositionRoot.cs:30-31,109-111,150-151,160-168,393-397,411-414）。
            var compositionRootSource = File.ReadAllText(Path.Combine(repoRoot, "src", "BetterUnturnedExperience.ClientUi", "BueClientUiCompositionRoot.cs"));
            Check(!compositionRootSource.Contains("InventoryDragPreviewAdapter"),
                "组合根不得再引用拖拽预览适配器（宿主不握适配器实例，T3 Q1，作废票 04）");
            Check(!compositionRootSource.Contains("InventorySurfaceLifecycleAdapter"),
                "组合根不得再引用库存表面生命周期适配器（宿主不握适配器实例，T3 Q1，作废票 04）");
            Check(!compositionRootSource.Contains("inventoryDragAdapter")
                    && !compositionRootSource.Contains("inventoryLifecycleAdapter"),
                "组合根不得再握适配器字段（作废票 04）");
            Check(!compositionRootSource.Contains("BetterItemInteractionUiComponent")
                    && !compositionRootSource.Contains("InventoryDragPreviewAdapter")
                    && !compositionRootSource.Contains("InventorySurfaceLifecycleAdapter"),
                "组合根不得再命名组件/两适配器类型（组件与适配器随 BII 迁 Bii 工程，T3 Q1，作废票 04）");

            // 宿主主文件解嵌：TickDragPreview/IsolateInventoryAdapters 不再经界面公开面。
            var pluginEntry = File.ReadAllText(Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Plugin", "BetterUnturnedExperiencePlugin.cs"));
            Check(pluginEntry.Contains("BiiFeatureAssembly.TickPreview")
                    && pluginEntry.Contains("BiiFeatureAssembly.IsolateAdapters"),
                "宿主启停驱动应经 Bii 公开缝（Plugin→Bii 合法边，作废票 04）");
        }

        // ───────────────────────── 组 4：禁方向拆净 ─────────────────────────

        /// <summary>迁移前先断直呼（T3 追加裁决）：Bii 源码零 ClientUi/Plugin/Core/Lit/Lir/Lht
        /// 的全限定与部分限定 token（文本级含注释）；ClientUi 源码零 Bii 同款 token；防火墙
        /// 真实仓库守卫零命中（02F 守卫态）。</summary>
        private static void V604GroupForbiddenDirectionsRemoved(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            AssertNoToken(Check, Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Bii"),
                new[] { "ClientUi", "Plugin", "Core", "Lit", "Lir", "Lht" },
                "Bii 工程");
            AssertNoToken(Check, Path.Combine(repoRoot, "src", "BetterUnturnedExperience.ClientUi"),
                new[] { "Bii" },
                "ClientUi 工程");

            // 防火墙真实仓库（守卫态零命中——02F 翻转后规则本体不变）。
            var result = RunPowerShellScript(Path.Combine(repoRoot, "eng", FirewallScript),
                "-RepoRoot \"" + repoRoot + "\"", repoRoot, 300);
            Check(result.ExitCode == 0 && !result.Output.Contains("VIOLATION"),
                "防火墙守卫应在真实仓库零命中（02F 守卫态），实际退出码 " + result.ExitCode
                + "；违规行: " + string.Join(" | ", result.Output.Split('\n').Where(l => l.Contains("VIOLATION")).Take(3)));
        }

        /// <summary>02D 惯例：防火墙全限定名正则只认全限定 token，部分限定写法（Plugin.Foo /
        /// ClientUi.Bar）能逃脱——本组以同族正则补扫；IVT 字面量
        /// （BetterUnturnedExperience.X.Tests）的前界是 '.'，不满足 `(^|[^\w.])`，自然不匹配
        /// （与防火墙 Get-ScanText 的 *.Tests 豁免同判据）。</summary>
        private static void AssertNoToken(Action<bool, string> Check, string dir, string[] foreignNames, string label)
        {
            // 防火墙 Get-ScanText 同判据（02B 豁免形态）：InternalsVisibleTo 指向 *.Tests 的
            // 友元声明字符串里不是引用方向——逐字抹除目标串（其他 IVT 目标不豁免，跨功能
            // 友元照常在册；本套件扫的 Bii AssemblyInfo 恰为 *.Tests 字面量形态）。
            var alternatives = string.Join("|", foreignNames);
            var fqn = new Regex("BetterUnturnedExperience[.](" + alternatives + ")[^A-Za-z0-9_]");
            var partial = new Regex("(^|[^A-Za-z0-9_.])(" + alternatives + ")[.]");
            foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\")))
            {
                var text = StripTestsFriendLiterals(File.ReadAllText(file));
                var name = Path.GetFileName(file);
                var fqnMatch = fqn.Match(text);
                Check(!fqnMatch.Success,
                    label + " 源码不得出现 " + fqnMatch.Value.Trim() + " 全限定 token（禁方向拆净，T3 追加裁决）@ " + name);
                var partialMatch = partial.Match(text);
                Check(!partialMatch.Success,
                    label + " 源码不得出现部分限定 " + partialMatch.Value.Trim() + " token（防火墙正则盲区自扫，02D 惯例）@ " + name);
            }
        }

        /// <summary>IVT 豁免抹除（防火墙 Get-ScanText 同判据）：只抹除目标以 .Tests 结尾的
        /// InternalsVisibleTo 目标串，等长空格保持行号；非 Tests 目标不豁免。</summary>
        private static string StripTestsFriendLiterals(string text)
        {
            var pattern = new Regex(@"InternalsVisibleTo\s*\(\s*""([^""]+)""\s*\)");
            foreach (Match m in pattern.Matches(text))
            {
                var target = m.Groups[1].Value;
                if (!target.EndsWith(".Tests", StringComparison.Ordinal)) continue;
                text = text.Remove(m.Groups[1].Index, target.Length).Insert(m.Groups[1].Index, new string(' ', target.Length));
            }
            return text;
        }

        // 02D 惯例（本套件私有副本）：house-style 脚本驱动器。
        private static ProcessResult RunPowerShellScript(string scriptPath, string arguments, string workingDirectory, int timeoutSeconds)
        {
            var exe = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
            if (!File.Exists(exe)) exe = "powershell.exe";
            var quoted = "\"" + scriptPath + "\"";
            if (!string.IsNullOrEmpty(arguments)) quoted += " " + arguments;
            return RunProcess(exe, "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File " + quoted, workingDirectory, timeoutSeconds);
        }

        private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory, int timeoutSeconds)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using (var process = new Process { StartInfo = psi })
            {
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                process.OutputDataReceived += (s, e) => { if (e.Data != null) lock (ProcessLock) stdout.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (ProcessLock) stderr.AppendLine(e.Data); };
                if (!process.Start()) throw new InvalidOperationException("进程启动失败: " + fileName);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (!process.WaitForExit(timeoutSeconds * 1000))
                {
                    try { process.Kill(); } catch { }
                    throw new InvalidOperationException("进程超时 " + timeoutSeconds + "s: " + fileName + " " + arguments);
                }
                return new ProcessResult { ExitCode = process.ExitCode, Output = stdout.ToString(), Errors = stderr.ToString() };
            }
        }

        private static readonly object ProcessLock = new object();

        private sealed class ProcessResult
        {
            internal int ExitCode { get; set; }
            internal string Output { get; set; }
            internal string Errors { get; set; }
        }

        /// <summary>测试宿主放置评估器替身（真实 rig 武装不驱动拖拽求值——本组只验开工收工
        /// 装配形状；拖拽求值语义由既有 Dev15B/D/16D 套件覆盖，评估器非本票被测面）。</summary>
        private sealed class ProbePlacementEvaluator : IPlacementCandidateEvaluator
        {
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                return default(ItemPlacementPreview);
            }
        }

        // ───────────────────────── 组 5：组合注入消费与宿主环公开缝 ─────────────────────────

        /// <summary>注入在座=真实消费（开工日志到达注入记录器、表面协调委托被调、设置快照被
        /// 消费、组件状态投影可读）；注入缺席=诚实退化（不武装不抛）；宿主环公开缝=TickPreview/
        /// IsolateAdapters/组件状态投影（面板官方条目事实源保持组件侧——「面板状态侧非第二
        /// 事实源」）。</summary>
        private static void V604GroupCompositionInjectionAndHostSeams(Action<bool, string> Check)
        {
            try
            {
                BiiFeatureAssembly.ResetWiredModule();

                // (a) 注入在座=真实消费：武装开工后，表面协调委托由组件表面注册路径驱动
                //     （RegisterInventorySurface 合法页→open 链）。
                var log = new List<string>();
                var opened = 0;
                var closed = 0;
                FeatureSettingsSnapshot? consumed = null;
                BiiFeatureAssembly.BindHostComposition(
                    logRuntime: log.Add,
                    headlessDecision: () => false,
                    placementEvaluatorFactory: () => new ProbePlacementEvaluator());
                BiiFeatureAssembly.BindInterfaceComposition(
                    surfaceOpened: () => opened++,
                    surfaceClosed: () => closed++,
                    settingsSnapshot: () =>
                    {
                        var snapshot = new FeatureSettingsSnapshot(
                            new FeatureId("io.github.yu80rice.bue.better-item-interaction"), 1,
                            SettingRevisionScope.ClientPreference, 7, SettingSyncState.Unavailable,
                            SettingSnapshotSource.SafeDefault, new SettingEntryView[0]);
                        consumed = snapshot;
                        return snapshot;
                    });
                var registration = BiiFeatureAssembly.CreateRegistration();
                var module = registration.ModuleFactory.Create();
                var result = module.Start(ComposeTestBootstrap());
                Check(result.Started, "setup: 武装开工应成功");
                var rig = BiiFeatureAssembly.WiredRig;
                Check(rig != null, "setup: rig 应在座");
                if (rig != null)
                {
                    // 设置快照消费：组件在决策点（初始化/开拖）经注入读取器拉取
                    // ——rig 已组件初始化（见 Arm 的 OnUiInitialized 调用）。
                    Check(consumed.HasValue,
                        "设置快照应经注入委托被消费（单事实源=界面设置态；拉模式语义）");
                    // 组件状态投影（面板官方条目事实源）。
                    Check(BiiFeatureAssembly.ComponentState != null,
                        "组件状态投影缝应在位（面板官方条目保持组件事实源）");
                    Check(BiiFeatureAssembly.ComponentPresentation != null,
                        "组件表现投影缝应在位");
                }

                // (b) 收工后再次开工=重新武装（面板启停链：Stop→UnpatchSelf、enable→再武装，
                //     T3 Q2 + DEV-V3-03 面板 command adapter 同一扇门）。
                module.Stop(FeatureStopReason.UserDisabled);
                Check(BiiFeatureAssembly.WiredRig == null, "停用应拆 rig");
                var rearm = registration.ModuleFactory.Create();
                var rearmResult = rearm.Start(ComposeTestBootstrap());
                Check(rearmResult.Started && BiiFeatureAssembly.WiredRig != null,
                    "启用应重新武装（开工收工可观察启停，spec 166 行）");
                rearm.Stop(FeatureStopReason.UserDisabled);

                // (c) 界面缝缺席=诚实退化：headless=false 但界面缝未绑 → 不创建界面。
                BiiFeatureAssembly.ResetWiredModule();
                // 评估器同时绑上（缺席分支的信号缺口=评估器 null 异常抢先，M6 突变咬出；
                // 本处补齐后只有「界面缝缺席」可解释零武装）。
                BiiFeatureAssembly.BindHostComposition(
                    logRuntime: log.Add,
                    headlessDecision: () => false,
                    placementEvaluatorFactory: () => new ProbePlacementEvaluator());
                var degraded = BiiFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                var degradedResult = degraded.Start(ComposeTestBootstrap());
                Check(degradedResult.Started,
                    "界面缝缺席时开工仍应成功（诚实退化，未绑定=缺席同族契约）");
                Check(BiiFeatureAssembly.WiredRig == null,
                    "界面缝缺席=不创建界面（T3 Q5「不创建界面」的缝缺席形态）");
                degraded.Stop(FeatureStopReason.UserDisabled);

                // (d) 宿主环公开缝：TickPreview/IsolateAdapters 在位（停用后再调不抛）。
                BiiFeatureAssembly.TickPreview();
                BiiFeatureAssembly.IsolateAdapters();

                // (f) 开工失败=本地回滚（Spec 轴 R3 裁定：Arm 中途失败不得遗留已装补丁）：
                //     注入日志口在激活步之后抛出（Arm 的 wiring 行），Start 应回滚 rig 并
                //     如实返回 not-started（交给既有隔离路径），簿记不得残留。
                BiiFeatureAssembly.ResetWiredModule();
                var probeThrows = 0;
                // 两个口都挂探针：测试宿主下生命周期的 wiring 行走 warn 分支（hook 不可装），
                // 真机走 runtime 分支——探针覆盖两条形态。
                void ProbeLog(string line)
                {
                    if (!line.Contains("wiring")) return;
                    probeThrows++;
                    throw new InvalidOperationException("probe-log-fault");
                }
                BiiFeatureAssembly.BindHostComposition(
                    logRuntime: ProbeLog,
                    logWarn: ProbeLog,
                    headlessDecision: () => false,
                    placementEvaluatorFactory: () => new ProbePlacementEvaluator());
                BiiFeatureAssembly.BindInterfaceComposition(
                    surfaceOpened: () => { },
                    surfaceClosed: () => { },
                    settingsSnapshot: () => default(FeatureSettingsSnapshot));
                var failing = BiiFeatureAssembly.CreateRegistration().ModuleFactory.Create();
                FeatureStartResult failingResult;
                try
                {
                    failingResult = failing.Start(ComposeTestBootstrap());
                }
                catch (Exception error)
                {
                    Check(false, "开工中途失败不得把异常抛穿给启动器（既有隔离路径要求 not-started 结果），实际抛出 "
                        + error.GetType().Name + ": " + error.Message);
                    return;
                }
                Check(probeThrows > 0, "setup: 注入日志口在激活后抛出（回滚路径被真实触发）");
                Check(!failingResult.Started,
                    "开工中途失败应如实返回 not-started（走既有隔离路径，T3 Q2）");
                Check(BiiFeatureAssembly.WiredRig == null,
                    "开工失败应本地回滚且不登记 rig（Spec 轴 R3 裁定：已装补丁不得遗留）");

                // (e) 公开组装面形态（Spec 轴 R1 收窄锁定）：复位缝是 internal 测试缝
                //     （经 IVT 通道取用，本行即编译期绑定），不得出现在公开面上——
                //     公开面只承载生产组装根动作。
                // Spec 轴 R2 裁定的补强：公开面枚举必须含属性访问器名与字段名——属性翻
                // public 时经 GetMethods 出现的是 get_WiredRig，裸名比对锁不住（该缺口由
                // 突变 M10 证红锁定）。
                var publicSurface = new List<string>();
                foreach (var method in typeof(BiiFeatureAssembly).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                    publicSurface.Add(method.Name);
                foreach (var property in typeof(BiiFeatureAssembly).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                    publicSurface.Add(property.Name);
                foreach (var field in typeof(BiiFeatureAssembly).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                    publicSurface.Add(field.Name);
                Check(!publicSurface.Contains("ResetWiredModule")
                        && !publicSurface.Contains("WiredRig")
                        && !publicSurface.Contains("get_WiredRig")
                        && !publicSurface.Contains("set_WiredRig"),
                    "复位/簿记缝不得出现在公开组装面（internal 测试缝；Spec 轴 R1 裁定收窄），实际公开成员: "
                    + string.Join(",", publicSurface));
            }
            finally
            {
                BiiFeatureAssembly.ResetWiredModule();
            }
        }
    }
}
