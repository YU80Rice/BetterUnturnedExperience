using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lir;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-02C 红测组：Lir（原位换弹）真分工程迁移（V6-T2 硬项拆法）。
    /// 被测外部行为：
    ///   1. 公开组装类型缝翻真值：LirFeatureAssembly.CreateRegistration() 返回真登记体
    ///      （冻结身份/定义载荷/最低契约/设置面/工厂），宿主 facade 经同一缝取登记——
    ///      02A 骨架期「调用即抛并点名迁移票」的过渡态终止。
    ///   2. 换弹工程清单自持：Lir csproj 的 Compile 清单恰等目录文件集（R2 收编）；
    ///      宿主 csproj 不再平铺 Lir 源码（R4 摘除）并经组装根合法边工程引用 Lir。
    ///   3. 禁方向拆净：防火墙真实仓库不在册 Lir→Plugin（日志口/设置根/无画面门禁/Steam
    ///      身份改宿主组合注入——BindHostComposition）；Lir 源零部分限定名越界 token。
    ///   4. 宿主组合注入消费与宿主环公开缝：注入缺席=诚实退化（日志静默/画面不裁剪/身份
    ///      解析跳过），注入在座=真实消费（日志到达记录器/无画面不武装/技能账自注入根读档/
    ///      设置根缺席 fail-closed）；接线实例契约投影与重置——面板组合不再触碰 Lir 内部类型
    ///      （02E 收缩前的过渡接缝，接缝本体在本票定形）。
    /// 维护锚：与 DevV602BLitTrueProjectTests / DevV6FirewallFullsuiteTests 同规约，
    /// 过渡态断言注明作废票。
    /// </summary>
    internal static class DevV602CLirTrueProjectTests
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

                Group("公开组装类型缝返回真登记体", () => V6CGroupSeamReturnsRealRegistration(Check));
                Group("换弹工程清单自持与宿主解嵌", () => V6CGroupManifestOwnsLirSources(Check));
                Group("禁方向拆净（防火墙+部分限定名盲区自扫）", () => V6CGroupForbiddenDirectionsRemoved(Check));
                Group("宿主组合注入消费与宿主环公开缝", () => V6CGroupCompositionInjectionAndHostRingSeams(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：公开缝返回真登记体 ─────────────────────────

        /// <summary>02A 骨架把登记缝定为 CreateRegistration() -> IFeatureRegistration（签名冻结，
        /// 02C 迁入真登记体后缝体翻真值、签名不变——作废票：02C）。宿主 facade 必须经同一缝
        /// 取登记（宿主只看那一个公开组装类型，V6-T2 追加）。</summary>
        private static void V6CGroupSeamReturnsRealRegistration(Action<bool, string> Check)
        {
            IFeatureRegistration registration;
            try
            {
                registration = LirFeatureAssembly.CreateRegistration();
            }
            catch (Exception error)
            {
                Check(false, "公开组装类型缝应返回真登记体（02A 骨架调用即抛的过渡态已终止，作废票 02C）: "
                    + error.GetType().Name + ": " + error.Message);
                return;
            }
            Check(registration != null, "公开缝不得返回 null（真登记体在 Lir 工程内自持）");

            // 冻结身份与定义载荷（与 Program.cs 登记组同判据，来源同一条登记链）。
            Check(PayloadText(registration.Definition) == "BUE-LIR-V1",
                "登记定义载荷应为文档化的 'BUE-LIR-V1' 文本，实际 " + PayloadText(registration.Definition));
            Check(registration.Definition.Feature.Value == "io.github.yu80rice.bue.in-place-reload",
                "登记身份应为冻结的 LIR FeatureId，实际 " + registration.Definition.Feature.Value);
            Check(registration.MinimumBueContract.Major == 2 && registration.MinimumBueContract.Minor == 0,
                "最低契约应与 (2,0) 门对齐");
            // 测试宿主（Assembly-CSharp 元数据纯反射可解析）表面 A 探测接得上 → 维持无
            // facet 的诚实注册（等级不造设置项，DEV-V5-07 两表面至多其一）；facet 变体仅在
            // 探测接不上的宿主出现，其行为由 DevV5ReloadSkillTests 表面 B 组覆盖。
            var settingsFace = registration as IFeatureSettingsRegistration;
            Check(settingsFace == null,
                "测试宿主表面 A 探测接得上，登记应为无 facet 的诚实注册（等级不造设置项），实际 facet 描述符 "
                + (settingsFace?.SettingDescriptors?.Count.ToString() ?? "null"));
            Check(registration.ModuleFactory != null, "模块工厂应在位（宿主启动路径经工厂装配）");

            // DEV-V6-02E 收口（作废票：02E）：facade 薄转发成员（CreateRegistration/
            // WiredModule/FeatureIdValue）退役——登记体唯一入口=公开组装类型缝；facade 只保留
            // 组装根动作（Register 的注入+桥接）。本断言锁收口后的结构形状。
            var facadeRepoRoot = FindRepoRoot();
            var facadeSource = File.ReadAllText(Path.Combine(facadeRepoRoot, "src", "BetterUnturnedExperience.Plugin", "InPlaceReloadFeatureRegistration.cs"));
            Check(!Regex.IsMatch(facadeSource, "internal static IFeatureRegistration CreateRegistration")
                    && !Regex.IsMatch(facadeSource, "internal static IFeatureModule WiredModule"),
                "02E 收口: Lir facade 不再转发登记/接线实例（宿主与测试一律经公开缝取用）");
        }

        // ───────────────────────── 组 2：清单自持与宿主解嵌 ─────────────────────────

        /// <summary>真分工程：Lir csproj 的自有 Compile 清单恰等目录文件集（R2 收编，作废票：
        /// 02C 收编后 02A 相机过渡态熄火）；宿主 csproj 摘除全部 Lir 平铺项（R4 摘除）并经
        /// 组装根合法边 ProjectReference Lir。</summary>
        private static void V6CGroupManifestOwnsLirSources(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var lirDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Lir");
            var lirCsprojPath = Path.Combine(lirDir, "BetterUnturnedExperience.Lir.csproj");
            var pluginCsprojPath = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Plugin",
                "BetterUnturnedExperience.Plugin.csproj");
            Check(File.Exists(lirCsprojPath) && File.Exists(pluginCsprojPath), "工程文件缺失（仓库结构破坏）");

            var lirCsproj = File.ReadAllText(lirCsprojPath);
            var pluginCsproj = File.ReadAllText(pluginCsprojPath);

            // 目录文件集（递归 .cs，排除 bin/obj，正斜杠——与防火墙 R2 同口径）。
            var dirFiles = Directory.GetFiles(lirDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .Select(f => f.Substring(lirDir.Length + 1).Replace('\\', '/'))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();
            var includes = Regex.Matches(lirCsproj, "<Compile\\b[^>]*>").Cast<Match>()
                .Select(m => Regex.Match(m.Value, "\\bInclude\\s*=\\s*\"([^\"]*)\""))
                .Where(m => m.Success).Select(m => m.Groups[1].Value.Replace('\\', '/'))
                .Where(i => !i.StartsWith("..", StringComparison.Ordinal))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();

            var missing = dirFiles.Except(includes).ToList();
            var stale = includes.Except(dirFiles).ToList();
            Check(missing.Count == 0 && stale.Count == 0,
                "Lir csproj 自有清单应恰等目录文件集（R2 收编，作废票 02C）"
                + (missing.Count > 0 ? "；在目录、不在清单: " + string.Join(", ", missing) : "")
                + (stale.Count > 0 ? "；在清单、不在目录: " + string.Join(", ", stale) : ""));

            Check(!pluginCsproj.Contains("<Compile Include=\"..\\BetterUnturnedExperience.Lir\\"),
                "宿主 csproj 不得再平铺 Lir 源码（R4 摘除，作废票 02C）");
            Check(Regex.IsMatch(pluginCsproj, "<ProjectReference\\b[^>]*BetterUnturnedExperience\\.Lir\\.csproj"),
                "宿主应经组装根合法边工程引用 Lir（V6-T2 白名单）");
        }

        // ───────────────────────── 组 3：禁方向拆净 ─────────────────────────

        /// <summary>防火墙真实仓库：Lir→Plugin 不在册（作废票：02C——日志口/设置根/无画面
        /// 门禁/Steam 身份按 T2 硬项拆法改宿主组合注入 BindHostComposition）。另自扫防火墙
        /// 全限定名正则的盲区：Lir 源内的部分限定名越界 token（如 Plugin.BueEngineNet 部分
        /// 限定写法）不得存活。</summary>
        private static void V6CGroupForbiddenDirectionsRemoved(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", FirewallScript);
            Check(File.Exists(script), "防火墙脚本缺失: eng/" + FirewallScript);
            var result = RunPowerShellScript(script, "-RepoRoot \"" + repoRoot + "\"", repoRoot, 300);

            Check(!result.Output.Contains("ref-direction Lir->Plugin"),
                "禁方向应拆净: Lir→Plugin 越界仍在册（宿主日志口/设置根/无画面门禁/Steam 身份改组合注入，作废票 02C）");

            // 部分限定名盲区自扫：防火墙正则 BetterUnturnedExperience\.[A-Za-z0-9]+ 只认全限定
            // token，Plugin.BueEngineNet（部分限定）能逃脱——本组补文本级扫描堵住同一方向。
            var lirDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Lir");
            var offenders = new List<string>();
            var partial = new Regex("(^|[^\\w.])(ClientUi|Plugin|Core)\\.");
            foreach (var file in Directory.GetFiles(lirDir, "*.cs", SearchOption.AllDirectories))
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
                "Lir 源应零部分限定名越界 token（防火墙正则盲区自扫，作废票 02C）: " + string.Join("; ", offenders));
        }

        // ───────────────────────── 组 4：宿主组合注入与宿主环公开缝 ─────────────────────────

        /// <summary>宿主环（面板组合 + 生命周期锚）在 Lir 拆出后仍经公开缝工作；组合注入按
        /// T2 硬项拆法定形并被真实消费：
        ///   a) 缝形状=编译期绑定（spec Testing Decisions 先例形态「缺类型或错误签名编红」：
        ///      本文件直接消费公开缝成员，成员缺席/签名漂移=编译红，不以反射按名锁接缝）；
        ///   b) 注入在座=真实消费：启动日志到达注入记录器、无画面门禁=画面面不武装、
        ///      技能账自注入设置根读档；Stop→再 Start 从注入委托重绑（F1b 语义保持）；
        ///   c) 注入缺席=诚实退化：无画面门禁未绑=画面不裁剪（与原字段默认 false 同）；
        ///   d) 设置根缺席=fail-closed：Start 显式失败，绝不静默发明路径；
        ///   e) 接线实例契约投影与重置（登记路径武装，生命周期锚同款）。
        /// 夹具经 IVT 通道（02A 组 3 形态）构造真实模块并断言其外部可观察行为，不断言
        /// 内部类型的落点或文件位置。</summary>
        private static void V6CGroupCompositionInjectionAndHostRingSeams(Action<bool, string> Check)
        {
            // a) 缝形状=编译期绑定（本行即断言：绑定失败=成员缺席或签名漂移=编译红）。
            Action<Action<string>, Action<string>, Func<bool>, Func<ulong>, Func<ulong, object>, Func<string>> bind =
                LirFeatureAssembly.BindHostComposition;
            Func<IFeatureModule> wiredRead = () => LirFeatureAssembly.WiredModule;
            Action resetWired = LirFeatureAssembly.ResetWiredModule;
            Check((string)LirFeatureAssembly.FeatureId == "io.github.yu80rice.bue.in-place-reload",
                "身份缝常量 FeatureId 应等于冻结 FeatureId");

            try
            {
                // b) 注入在座=真实消费。
                var root = NewLirHostSettingsRoot();
                File.WriteAllText(Path.Combine(root, ReloadSkillFilePersistence.FileName),
                    "76561197960265729|testchar|2" + Environment.NewLine, new UTF8Encoding(false));
                var runtimeLog = new List<string>();
                bind(line => runtimeLog.Add(line),
                    line => { },
                    () => true,   // 无画面门禁在座=true：画面面按注入判定不武装
                    () => 0UL,    // 身份在座但解析不出=跳过本地分支（与测试宿主原值同）
                    _ => null,
                    () => root);
                var savedCore = InPlaceReloadModule.CorePatchInstallerForTests;
                try
                {
                    InPlaceReloadModule.CorePatchInstallerForTests = t => true;
                    var module = ComposeStartedModule(Check);
                    Check(runtimeLog.Any(line => line.Contains("[Lir] 模块已启动")),
                        "宿主日志注入应被真实消费：模块启动日志到达注入记录器，实际 " + runtimeLog.Count + " 行");
                    Check(module.HudStartGateDiagnostics == "ammo-hud-headless-not-armed",
                        "无画面门禁注入应被真实消费：HUD 面按注入判定不武装，实际 '" + module.HudStartGateDiagnostics + "'");
                    Check(module.SkillStartGateDiagnostics == "reload-skill-headless-not-armed",
                        "无画面门禁注入应被真实消费：技能分区按注入判定不武装，实际 '" + module.SkillStartGateDiagnostics + "'");
                    Check(module.SkillRuntime != null
                        && module.SkillRuntime.GetLevel(76561197960265729UL, ReloadSkillStore.NormalizeCharKey("testchar")) == 2,
                        "设置根注入应被真实消费：技能等级账从注入根读档（预置 2 级）");
                    module.Stop(FeatureStopReason.PluginStopping);

                    // F1b 重绑语义经注入保持：Stop 解绑生产日志缝后，再 Start 从注入委托重绑。
                    runtimeLog.Clear();
                    var restart = ComposeStartedModule(Check);
                    Check(runtimeLog.Any(line => line.Contains("[Lir] 模块已启动")),
                        "F1b 重绑语义应经注入保持：停用→再启用后启动日志再次到达注入记录器");
                    restart.Stop(FeatureStopReason.PluginStopping);
                }
                finally
                {
                    InPlaceReloadModule.CorePatchInstallerForTests = savedCore;
                }
            }
            catch (Exception error)
            {
                Check(false, "注入在座行为链异常: " + error.GetType().Name + ": " + error.Message);
            }

            // c) 注入缺席=诚实退化（画面不裁剪）：无画面门禁未绑 → 画面面照常武装
            //    （与原宿主字段默认 false 同——缺席绝不发明「无画面」的判断）。
            {
                bind(null, null, null, null, null, () => LirHostSettingsRootForSuite());
                var savedCore = InPlaceReloadModule.CorePatchInstallerForTests;
                var savedHud = InPlaceReloadModule.HudPatchInstallerForTests;
                try
                {
                    InPlaceReloadModule.CorePatchInstallerForTests = t => true;
                    InPlaceReloadModule.HudPatchInstallerForTests = t => true;
                    var module = ComposeStartedModule(Check);
                    Check(module.HudPatchInstalled,
                        "无画面门禁缺席应诚实退化=画面不裁剪（HUD 面照常武装，与原字段默认 false 同）");
                    module.Stop(FeatureStopReason.PluginStopping);
                }
                finally
                {
                    InPlaceReloadModule.CorePatchInstallerForTests = savedCore;
                    InPlaceReloadModule.HudPatchInstallerForTests = savedHud;
                }
            }

            // d) 设置根缺席=fail-closed：未绑设置根且无测试持久化假件 → Start 显式失败
            //    （绝不静默发明持久化路径——宿主组合未给的事实不能猜）。
            {
                bind(null, null, null, null, null, null);
                Exception observed = null;
                try
                {
                    ComposeStartedModule(Check);
                }
                catch (Exception error) { observed = error; }
                Check(observed is InvalidOperationException && observed.Message.Contains("设置根"),
                    "设置根缺席应 fail-closed（Start 显式失败并点名设置根），实际 "
                    + (observed == null ? "未抛" : observed.GetType().Name + ": " + observed.Message));
            }

            // e) 宿主环经公开缝：登记路径武装接线实例（工厂装配并经契约类型投影），
            //    ResetWiredModule 重置（生命周期锚测试同款语义）。
            {
                resetWired();
                var runtime = new BetterUnturnedExperience.Core.Registration.FeatureRegistrationRuntime();
                runtime.OpenRegistration();
                Check(runtime.Register(LirFeatureAssembly.CreateRegistration()).Accepted,
                    "setup: 官方 LIR 登记入探针运行时（经公开缝）");
                Check(runtime.CompleteRuntime(), "setup: 目录冻结");
                BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(4603UL));
                var wired = wiredRead();
                Check(wired != null, "登记路径后接线实例应在座（工厂装配并经契约投影）");
                resetWired();
                Check(wiredRead() == null, "ResetWiredModule 应清空接线实例（生命周期锚重置语义）");
            }

            // 收束：日志/门禁/身份回到未绑（本组前测试进程的缺省态）；设置根保持绑定——
            // 直接构造模块的既有 harness 依赖一个可用根（语义=空档账户，与迁移前等价）。
            bind(null, null, null, null, null, () => LirHostSettingsRootForSuite());
        }

        // ───────────────────────── helpers ─────────────────────────

        /// <summary>与 Program.cs NewLirModule 同构的最小启动：真总线 + 回环网络，生产组合
        /// 路径（persistence 组合走生产装配——根来自组合注入，无测试假件注入）。</summary>
        private static InPlaceReloadModule ComposeStartedModule(Action<bool, string> Check)
        {
            var feature = new FeatureId(LirFeatureAssembly.FeatureId);
            var module = new InPlaceReloadModule(feature);
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            // DEV-V6-12：武装 ⇒ 经启动口袋登记（缺口袋=立即自拆），与生产组合同形给真账户。
            var pocket = LirTestPocket.Open();
            var bootstrap = new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(
                default(FeatureScopeIdentity), pocket.Generation, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature),
                null, null, null, NewLoopbackNetwork(4601UL), null, pocket.Patching);
            var result = module.Start(bootstrap);
            Check(result.Started, "setup: 模块应经生产组合路径启动，实际 " + result.DiagnosticId);
            return module;
        }

        private static string NewLirHostSettingsRoot()
        {
            var dir = Path.Combine(Path.GetTempPath(), "bue-02c-root-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>套件级共享设置根（懒建）：组 4 收束后保持绑定，供直接构造模块的既有
        /// harness 组合生产持久化——缺档=空账，与迁移前 BaseDirectory 根的可观察行为等价。</summary>
        private static string LirHostSettingsRootForSuite()
        {
            if (suiteSettingsRoot == null) suiteSettingsRoot = NewLirHostSettingsRoot();
            return suiteSettingsRoot;
        }

        private static string suiteSettingsRoot;

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
