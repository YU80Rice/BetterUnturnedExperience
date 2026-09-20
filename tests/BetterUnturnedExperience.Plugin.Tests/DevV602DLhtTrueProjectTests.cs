using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lht;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-02D 红测组：Lht（尸潮计数）真分工程迁移（V6-T2 硬项拆法）。
    /// 被测外部行为：
    ///   1. 公开组装类型缝翻真值：LhtFeatureAssembly.CreateRegistration() 返回真登记体
    ///      （冻结身份/定义载荷/最低契约/工厂），宿主 facade 经同一缝取登记——
    ///      02A 骨架期「调用即抛并点名迁移票」的过渡态终止。
    ///   2. 尸潮工程清单自持：Lht csproj 的 Compile 清单恰等目录文件集（R2 收编）；
    ///      宿主 csproj 不再平铺 Lht 源码（R4 摘除）并经组装根合法边工程引用 Lht。
    ///   3. 禁方向拆净：防火墙真实仓库不在册 Lht→Plugin（宿主日志口改组合注入
    ///      BindHostComposition——本票唯一越界面）；Lht 源零部分限定名越界 token。
    ///   4. 宿主组合注入消费与宿主环公开缝：注入在座=真实消费（启动日志到达注入记录器、
    ///      F1b 停用→再启用经注入重绑）、注入缺席=诚实退化（日志静默=LhtRuntime 既有
    ///      「未绑定即吞」契约）、接线实例契约投影与表现缝（面板 Lht 表现态不再触碰
    ///      Lht 内部类型——02E 收缩前的过渡接缝，接缝本体在本票定形）。
    /// 维护锚：与 DevV602BLitTrueProjectTests / DevV602CLirTrueProjectTests /
    /// DevV6FirewallFullsuiteTests 同规约，过渡态断言注明作废票。
    /// </summary>
    internal static class DevV602DLhtTrueProjectTests
    {
        private const string FirewallScript = "Verify-ProjectFirewall.ps1";
        private const string SolutionFile = "BetterUnturnedExperience.sln";

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

                Group("公开组装类型缝返回真登记体", () => V6DGroupSeamReturnsRealRegistration(Check));
                Group("尸潮工程清单自持与宿主解嵌", () => V6DGroupManifestOwnsLhtSources(Check));
                Group("禁方向拆净（防火墙+部分限定名盲区自扫）", () => V6DGroupForbiddenDirectionsRemoved(Check));
                Group("宿主组合注入消费与宿主环公开缝", () => V6DGroupCompositionInjectionAndHostRingSeams(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：公开缝返回真登记体 ─────────────────────────

        /// <summary>02A 骨架把登记缝定为 CreateRegistration() -> IFeatureRegistration（签名冻结，
        /// 02D 迁入真登记体后缝体翻真值、签名不变——作废票：02D）。宿主 facade 必须经同一缝
        /// 取登记（宿主只看那一个公开组装类型，V6-T2 追加）。尸潮登记不带设置 facet（开关走
        /// 宿主注入的 scoped 视图，DEV-V3-06——登记面无 facet 是诚实形状，非缺漏）。</summary>
        private static void V6DGroupSeamReturnsRealRegistration(Action<bool, string> Check)
        {
            IFeatureRegistration registration;
            try
            {
                registration = LhtFeatureAssembly.CreateRegistration();
            }
            catch (Exception error)
            {
                Check(false, "公开组装类型缝应返回真登记体（02A 骨架调用即抛的过渡态已终止，作废票 02D）: "
                    + error.GetType().Name + ": " + error.Message);
                return;
            }
            Check(registration != null, "公开缝不得返回 null（真登记体在 Lht 工程内自持）");

            // 冻结身份与定义载荷（与 Program.cs 登记组同判据，来源同一条登记链）。
            Check(PayloadText(registration.Definition) == "BUE-LHT-V1",
                "登记定义载荷应为文档化的 'BUE-LHT-V1' 文本，实际 " + PayloadText(registration.Definition));
            Check(registration.Definition.Feature.Value == "io.github.yu80rice.bue.horde-tracker",
                "登记身份应为冻结的 LHT FeatureId，实际 " + registration.Definition.Feature.Value);
            Check(registration.MinimumBueContract.Major == 2 && registration.MinimumBueContract.Minor == 0,
                "最低契约应与 (2,0) 门对齐");
            var settingsFace = registration as IFeatureSettingsRegistration;
            Check(settingsFace == null,
                "尸潮登记应为无 facet 的诚实注册（开关走宿主注入 scoped 视图，DEV-V3-06），实际 facet 描述符 "
                + (settingsFace?.SettingDescriptors?.Count.ToString() ?? "null"));
            Check(registration.ModuleFactory != null, "模块工厂应在位（宿主启动路径经工厂装配）");

            // DEV-V6-02E 收口（作废票：02E）：facade 薄转发成员（CreateRegistration/
            // WiredModule/FeatureIdValue）退役——登记体唯一入口=公开组装类型缝；facade 只保留
            // 组装根动作（Register 的注入+桥接）。本断言锁收口后的结构形状。
            var facadeRepoRoot = FindRepoRoot();
            var facadeSource = File.ReadAllText(Path.Combine(facadeRepoRoot, "src", "BetterUnturnedExperience.Plugin", "HordeTrackerFeatureRegistration.cs"));
            Check(!Regex.IsMatch(facadeSource, "internal static IFeatureRegistration CreateRegistration")
                    && !Regex.IsMatch(facadeSource, "internal static IFeatureModule WiredModule"),
                "02E 收口: Lht facade 不再转发登记/接线实例（宿主与测试一律经公开缝取用）");
        }

        // ───────────────────────── 组 2：清单自持与宿主解嵌 ─────────────────────────

        /// <summary>真分工程：Lht csproj 的自有 Compile 清单恰等目录文件集（R2 收编，作废票：
        /// 02D 收编后 02A 相机过渡态熄火）；宿主 csproj 摘除全部 Lht 平铺项（R4 摘除）并经
        /// 组装根合法边 ProjectReference Lht。</summary>
        private static void V6DGroupManifestOwnsLhtSources(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var lhtDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Lht");
            var lhtCsprojPath = Path.Combine(lhtDir, "BetterUnturnedExperience.Lht.csproj");
            var pluginCsprojPath = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Plugin",
                "BetterUnturnedExperience.Plugin.csproj");
            Check(File.Exists(lhtCsprojPath) && File.Exists(pluginCsprojPath), "工程文件缺失（仓库结构破坏）");

            var lhtCsproj = File.ReadAllText(lhtCsprojPath);
            var pluginCsproj = File.ReadAllText(pluginCsprojPath);

            // 目录文件集（递归 .cs，排除 bin/obj，正斜杠——与防火墙 R2 同口径）。
            var dirFiles = Directory.GetFiles(lhtDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .Select(f => f.Substring(lhtDir.Length + 1).Replace('\\', '/'))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();
            var includes = Regex.Matches(lhtCsproj, "<Compile\\b[^>]*>").Cast<Match>()
                .Select(m => Regex.Match(m.Value, "\\bInclude\\s*=\\s*\"([^\"]*)\""))
                .Where(m => m.Success).Select(m => m.Groups[1].Value.Replace('\\', '/'))
                .Where(i => !i.StartsWith("..", StringComparison.Ordinal))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();

            var missing = dirFiles.Except(includes).ToList();
            var stale = includes.Except(dirFiles).ToList();
            Check(missing.Count == 0 && stale.Count == 0,
                "Lht csproj 自有清单应恰等目录文件集（R2 收编，作废票 02D）"
                + (missing.Count > 0 ? "；在目录、不在清单: " + string.Join(", ", missing) : "")
                + (stale.Count > 0 ? "；在清单、不在目录: " + string.Join(", ", stale) : ""));

            Check(!pluginCsproj.Contains("<Compile Include=\"..\\BetterUnturnedExperience.Lht\\"),
                "宿主 csproj 不得再平铺 Lht 源码（R4 摘除，作废票 02D）");
            Check(Regex.IsMatch(pluginCsproj, "<ProjectReference\\b[^>]*BetterUnturnedExperience\\.Lht\\.csproj"),
                "宿主应经组装根合法边工程引用 Lht（V6-T2 白名单）");
        }

        // ───────────────────────── 组 3：禁方向拆净 ─────────────────────────

        /// <summary>防火墙真实仓库：Lht→Plugin 不在册（作废票：02D——宿主日志口按 T2 硬项拆法
        /// 改宿主组合注入 BindHostComposition，本票唯一越界面）。另自扫防火墙全限定名正则的
        /// 盲区：Lht 源内的部分限定名越界 token（如 Plugin.BueRuntimeLog 部分限定写法）不得
        /// 存活。</summary>
        private static void V6DGroupForbiddenDirectionsRemoved(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", FirewallScript);
            Check(File.Exists(script), "防火墙脚本缺失: eng/" + FirewallScript);
            var result = RunPowerShellScript(script, "-RepoRoot \"" + repoRoot + "\"", repoRoot, 300);

            Check(!result.Output.Contains("ref-direction Lht->Plugin"),
                "禁方向应拆净: Lht→Plugin 越界仍在册（宿主日志口改组合注入，作废票 02D）");

            // 部分限定名盲区自扫：防火墙正则 BetterUnturnedExperience\.[A-Za-z0-9]+ 只认全限定
            // token，Plugin.BueRuntimeLog（部分限定）能逃脱——本组补文本级扫描堵住同一方向。
            var lhtDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Lht");
            var offenders = new List<string>();
            var partial = new Regex("(^|[^\\w.])(ClientUi|Plugin|Core)\\.");
            foreach (var file in Directory.GetFiles(lhtDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("\\bin\\") || file.Contains("\\obj\\")) continue;
                var text = File.ReadAllText(file);
                foreach (Match m in partial.Matches(text))
                {
                    var line = text.Substring(0, m.Index).Split('\n').Length;
                    offenders.Add(Path.GetFileName(file) + ":" + line + " '" + m.Value.Trim() + "'");
                }
            }
            Check(offenders.Count == 0,
                "Lht 源应零部分限定名越界 token（防火墙正则盲区自扫，作废票 02D）: " + string.Join("; ", offenders));
        }

        // ───────────────────────── 组 4：宿主组合注入与宿主环公开缝 ─────────────────────────

        /// <summary>宿主环（面板表现 + 生命周期锚）在 Lht 拆出后仍经公开缝工作；组合注入按
        /// T2 硬项拆法定形并被真实消费（本票唯一注入面=宿主日志口）：
        ///   a) 缝形状=编译期绑定（spec Testing Decisions 先例形态「缺类型或错误签名编红」：
        ///      本文件直接消费公开缝成员，成员缺席/签名漂移=编译红，不以反射按名锁接缝）；
        ///   b) 注入在座=真实消费：启动日志到达注入记录器；Stop 解绑后再 Start 从注入委托
        ///      重绑（F1b 语义保持）；
        ///   c) 注入缺席=诚实退化：日志缝未绑=静默吞（LhtRuntime 既有「未绑定即吞」契约，
        ///      与旧插件 null-conditional 同）；
        ///   d) 接线实例契约投影、表现缝与重置（工厂 born-inert 装配 + 簿记投影探针；
        ///      命名缝缺口：Lht 工厂模块的 StartCatalog 全链武装在测试宿主不安全——模块按
        ///      生产语义直读引擎 batch-mode 事实（CanUseClientUi），宿主测试进程不执行
        ///      Unity 调用，故全链武装不进本套件，武装行为由 U3DS 实机探针与既有 harness
        ///      CanUseClientUiForTests 路径覆盖）。
        /// 夹具经 IVT 通道（02A 组 3 形态）构造真实模块并断言其外部可观察行为，不断言
        /// 内部类型的落点或文件位置。</summary>
        private static void V6DGroupCompositionInjectionAndHostRingSeams(Action<bool, string> Check)
        {
            // a) 缝形状=编译期绑定（本行即断言：绑定失败=成员缺席或签名漂移=编译红）。
            Action<Action<string>, Action<string>> bind = LhtFeatureAssembly.BindHostComposition;
            Func<FeaturePresentationState> presentation = () => LhtFeatureAssembly.WiredPresentationState;
            Func<IFeatureModule> wiredRead = () => LhtFeatureAssembly.WiredModule;
            Action resetWired = LhtFeatureAssembly.ResetWiredModule;
            Check((string)LhtFeatureAssembly.FeatureId == "io.github.yu80rice.bue.horde-tracker",
                "身份缝常量 FeatureId 应等于冻结 FeatureId");
            Check((string)LhtFeatureAssembly.DisplayName == "更好的尸潮播报",
                "显示名缝常量 DisplayName 应等于官方中文名");

            try
            {
                // b) 注入在座=真实消费。
                var runtimeLog = new List<string>();
                bind(line => runtimeLog.Add(line), line => { });
                var module = ComposeStartedModule(Check);
                Check(runtimeLog.Any(line => line.Contains("[Lht] 模块已启动")),
                    "宿主日志注入应被真实消费：模块启动日志到达注入记录器，实际 " + runtimeLog.Count + " 行");
                module.Stop(FeatureStopReason.PluginStopping);

                // F1b 重绑语义经注入保持：Stop 解绑生产日志缝后，再 Start 从注入委托重绑。
                runtimeLog.Clear();
                var restart = ComposeStartedModule(Check);
                Check(runtimeLog.Any(line => line.Contains("[Lht] 模块已启动")),
                    "F1b 重绑语义应经注入保持：停用→再启用后启动日志再次到达注入记录器");
                restart.Stop(FeatureStopReason.PluginStopping);
            }
            catch (Exception error)
            {
                Check(false, "注入在座行为链异常: " + error.GetType().Name + ": " + error.Message);
            }

            // c) 注入缺席=诚实退化：日志缝未绑 → BindProductionLog 把缺席如实传播到
            //    领域缝（不得残留上一组的接线——残留=缺席被静默发明成「仍接线」）、
            //    LogInfo/LogError/LogWarning 静默不抛（LhtRuntime「未绑定即吞」契约）、
            //    Start 照常成功（ComposeStartedModule 的 Check(result.Started) 即断言）。
            {
                bind(null, null);
                var module = ComposeStartedModule(Check);
                Check(LhtRuntime.LogSink == null && LhtRuntime.ErrorLogSink == null,
                    "日志口注入缺席应如实传播到领域缝（BindProductionLog 后不得残留既有接线）");
                var silenceObserved = false;
                try { LhtRuntime.LogInfo("[Lht] 02d-absence-probe"); LhtRuntime.LogError("[Lht] 02d-absence-probe-e"); LhtRuntime.LogWarning("[Lht] 02d-absence-probe-w"); silenceObserved = true; }
                catch (Exception) { silenceObserved = false; }
                Check(silenceObserved, "未绑定日志缝的 LogInfo/LogError/LogWarning 应静默不抛（未绑定即吞契约）");
                module.Stop(FeatureStopReason.PluginStopping);
            }

            // d) 接线实例契约投影、表现缝与重置：工厂 born-inert 装配后接线实例在座
            //    （契约类型投影，宿主不触碰 Lht 内部类型）；表现缝反映接线模块的
            //    Available/HeadlessOnly 判定（簿记投影探针经 IVT 通道——见组 4 缺口记名）；
            //    ResetWiredModule 清空（生命周期锚重置语义）。
            {
                resetWired();
                Check(wiredRead() == null, "setup: 重置后接线实例应为空");
                var registration = LhtFeatureAssembly.CreateRegistration();
                var factoryModule = (HordeTrackerModule)registration.ModuleFactory.Create();
                Check(factoryModule != null && !factoryModule.Started,
                    "工厂装配应为 born inert（登记受理前不武装，宿主启动路径才 Start）");
                Check(wiredRead() == factoryModule,
                    "工厂装配后接线实例应经契约类型投影在座（宿主环过渡接缝）");

                var probe = new HordeTrackerModule(new FeatureId(LhtRuntime.FeatureIdValue));
                probe.CanUseClientUiForTests = () => false;
                SetWiredForProjectionProbe(probe);
                Check(presentation() == FeaturePresentationState.HeadlessOnly,
                    "表现缝应反映接线模块的无画面判定（batch-mode 事实=HeadlessOnly）");
                probe.CanUseClientUiForTests = () => true;
                Check(presentation() == FeaturePresentationState.Available,
                    "表现缝应反映接线模块的有头判定（非 batch-mode=Available）");

                resetWired();
                Check(wiredRead() == null, "ResetWiredModule 应清空接线实例（生命周期锚重置语义）");
                Check(presentation() == FeaturePresentationState.Available,
                    "表现缝在无接线实例时应取 Available（与迁移前 lhtModule==null 分支同语义）");
            }

            // 收束：日志口回到未绑（本组前测试进程的缺省态——既有 harness 的诊断断言不依赖
            // 日志路由）、接线实例清空，不给后续组留跨组状态。
            bind(null, null);
            resetWired();
        }

        // ───────────────────────── helpers ─────────────────────────

        /// <summary>与 Program.cs NewLhtModule 同构的最小启动：真总线 + 回环网络 + 录制假件
        /// （authority/HUD 走模块既有测试缝，生产 HordeProductionAuthority 直触引擎不进宿主
        /// 测试进程），生产组合路径（网络注册/补丁安装走生产装配）。
        /// DEV-V6-13：武装 ⇒ 经启动口袋登记（缺口袋=立即自拆），与生产组合同形给真账户。</summary>
        private static HordeTrackerModule ComposeStartedModule(Action<bool, string> Check)
        {
            var feature = new FeatureId(LhtFeatureAssembly.FeatureId);
            var module = new HordeTrackerModule(feature);
            module.AuthorityFactoryForTests = () => new ProbeHordeAuthority();
            module.HudSurfaceFactoryForTests = () => new ProbeHudSurface();
            module.CanUseClientUiForTests = () => false;
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var pocket = LhtTestPocket.Open();
            var bootstrap = new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(
                default(FeatureScopeIdentity), pocket.Generation, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature),
                null, null, null, NewLoopbackNetwork(4701UL), null, pocket.Patching);
            var result = module.Start(bootstrap);
            Check(result.Started, "setup: 模块应经生产组合路径启动，实际 " + result.DiagnosticId);
            return module;
        }

        /// <summary>组 4(d) 簿记投影探针：把带测试缝的模块放入接线簿记（IVT 通道），仅用于
        /// 证明表现缝的投影契约——非生产组合路径（工厂是唯一组合路径，V6-T2）。</summary>
        private static void SetWiredForProjectionProbe(HordeTrackerModule probe)
        {
            HordeTrackerRegistration.WiredModule = probe;
        }

        /// <summary>组 4 探针假件：IHordeTrackingAuthority 全 no-op 录制（引擎缝不进测试进程）。</summary>
        private sealed class ProbeHordeAuthority : IHordeTrackingAuthority
        {
            public bool IsServerRole() { return true; }
            public void SubscribeBeaconUpdated(Action<byte, bool> handler) { }
            public void UnsubscribeBeaconUpdated(Action<byte, bool> handler) { }
            public void SubscribeServerHosted(Action handler) { }
            public void UnsubscribeServerHosted(Action handler) { }
            public bool TryResolveBeacon(byte nav, out HordeBeaconView beacon) { beacon = null; return false; }
            public bool TryReadCounters(HordeBeaconView beacon, out int remaining, out int alive) { remaining = 0; alive = 0; return false; }
            public bool TryFlushHordeCommandRegistration(HordeStatusSource source) { return true; }
            public void DeregisterHordeCommand() { }
            public void NotifyHordeStart(HordeBeaconView beacon) { }
            public void NotifyHordeEnd(HordeBeaconView beacon) { }
        }

        /// <summary>组 4 探针假件：IHordeHudSurface 全 no-op（HUD 缝不进测试进程）。</summary>
        private sealed class ProbeHudSurface : IHordeHudSurface
        {
            public bool Install() { return true; }
            public void Uninstall() { }
            public bool IsLabelReady() { return false; }
            public void SetText(string text) { }
            public void SetVisible(bool visible) { }
            public void DrainDisconnectReset() { }
        }

        private static BetterUnturnedExperience.Core.Network.BueNetworkRuntime NewLoopbackNetwork(ulong nonce)
        {
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            return new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), nonce);
        }

        private static readonly object ProcessLock = new object();

        private static string PayloadText(FeatureDefinitionArtifact definition)
        {
            var bytes = new byte[definition.CanonicalPayload.Count];
            for (var index = 0; index < bytes.Length; index++) bytes[index] = definition.CanonicalPayload[index];
            return Encoding.UTF8.GetString(bytes);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, SolutionFile)))
                dir = dir.Parent;
            if (dir == null)
                throw new InvalidOperationException("仓库根未定位（从测试基目录上溯未找到 " + SolutionFile + "）");
            return dir.FullName;
        }

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

        private sealed class ProcessResult
        {
            public int ExitCode;
            public string Output;
            public string Errors;
            public string All { get { return Output + Environment.NewLine + Errors; } }
        }
    }
}
