using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lit;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-02B 红测组：Lit（背包整理）真分工程迁移（V6-T2 硬项拆法）。
    /// 被测外部行为：
    ///   1. 公开组装类型缝翻真值：LitFeatureAssembly.CreateRegistration() 返回真登记体
    ///      （冻结身份/定义载荷/最低契约/设置面/工厂），宿主 facade 经同一缝取登记——
    ///      02A 骨架期「调用即抛并点名迁移票」的过渡态终止。
    ///   2. 整理工程清单自持：Lit csproj 的 Compile 清单恰等目录文件集（R2 收编）；
    ///      宿主 csproj 不再平铺 Lit 源码（R4 摘除）并经组装根合法边工程引用 Lit。
    ///   3. 禁方向拆净：防火墙真实仓库不在册 Lit→ClientUi / Lit→Plugin / ClientUi→Lit
    ///      （页码范围按 02B 票面 Scope 的「否则」分支走宿主转达/绑定单源）；Lit 源零部分限定名越界
    ///      token（防火墙全限定名正则的盲区自扫，如 ClientUi.Internal 部分限定）。
    ///   4. 宿主环经公开缝继续工作：节拍泵（完成链同款拍）、投影对账宿主转达口、
    ///      接线实例契约投影与重置——面板组合与完成链不再触碰 Lit 内部类型
    ///      （02E 收缩前的过渡接缝，接缝本体在本票定形）。
    /// 维护锚：与 DevV602AProjectGraphTests / DevV6FirewallFullsuiteTests 同规约，
    /// 过渡态断言注明作废票。
    /// </summary>
    internal static class DevV602BLitTrueProjectTests
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

                Group("公开组装类型缝返回真登记体", () => V6GroupSeamReturnsRealRegistration(Check));
                Group("整理工程清单自持与宿主解嵌", () => V6GroupManifestOwnsLitSources(Check));
                Group("禁方向拆净（防火墙+部分限定名盲区自扫）", () => V6GroupForbiddenDirectionsRemoved(Check));
                Group("宿主环公开缝（节拍泵/转达口/接线投影）", () => V6GroupHostRingSeams(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：公开缝返回真登记体 ─────────────────────────

        /// <summary>02A 骨架把登记缝定为 CreateRegistration() -> IFeatureRegistration（签名冻结，
        /// 02B 迁入真登记体后缝体翻真值、签名不变——作废票：02B）。宿主 facade 必须经同一缝
        /// 取登记（宿主只看那一个公开组装类型，V6-T2 追加）。</summary>
        private static void V6GroupSeamReturnsRealRegistration(Action<bool, string> Check)
        {
            IFeatureRegistration registration;
            try
            {
                registration = LitFeatureAssembly.CreateRegistration();
            }
            catch (Exception error)
            {
                Check(false, "公开组装类型缝应返回真登记体（02A 骨架调用即抛的过渡态已终止，作废票 02B）: "
                    + error.GetType().Name + ": " + error.Message);
                return;
            }
            Check(registration != null, "公开缝不得返回 null（真登记体在 Lit 工程内自持）");

            // 冻结身份与定义载荷（与 Program.cs 登记组同判据，来源同一条登记链）。
            Check(PayloadText(registration.Definition) == "BUE-LIT-V1",
                "登记定义载荷应为文档化的 'BUE-LIT-V1' 文本，实际 " + PayloadText(registration.Definition));
            Check(registration.Definition.Feature.Value == "io.github.yu80rice.bue.inventory-tidy",
                "登记身份应为冻结的 LIT FeatureId，实际 " + registration.Definition.Feature.Value);
            Check(registration.MinimumBueContract.Major == 2 && registration.MinimumBueContract.Minor == 0,
                "最低契约应与 (2,0) 门对齐");
            var settingsFace = registration as IFeatureSettingsRegistration;
            Check(settingsFace != null && settingsFace.SettingDescriptors != null && settingsFace.SettingDescriptors.Count == 1,
                "设置面应为整理方向单一描述符（DEV-V5-02 收编后恰一），实际 "
                + (settingsFace?.SettingDescriptors?.Count.ToString() ?? "null"));
            Check(registration.ModuleFactory != null, "模块工厂应在位（宿主启动路径经工厂装配）");

            // DEV-V6-02E 收口（作废票：02E）：facade 薄转发成员（CreateRegistration/
            // WiredModule/FeatureIdValue）退役——登记体唯一入口=公开组装类型缝；facade 只保留
            // 组装根动作（Register 的注入+桥接）。本断言锁收口后的结构形状。
            var facadeRepoRoot = FindRepoRoot();
            var facadeSource = File.ReadAllText(Path.Combine(facadeRepoRoot, "src", "BetterUnturnedExperience.Plugin", "InventoryTidyFeatureRegistration.cs"));
            Check(!Regex.IsMatch(facadeSource, "internal static IFeatureRegistration CreateRegistration")
                    && !Regex.IsMatch(facadeSource, "internal static IFeatureModule WiredModule"),
                "02E 收口: Lit facade 不再转发登记/接线实例（宿主与测试一律经公开缝取用）");
        }

        // ───────────────────────── 组 2：清单自持与宿主解嵌 ─────────────────────────

        /// <summary>真分工程：Lit csproj 的自有 Compile 清单恰等目录文件集（R2 收编，作废票：
        /// 02B 收编后 02A 相机过渡态熄火）；宿主 csproj 摘除全部 Lit 平铺项（R4 摘除）并经
        /// 组装根合法边 ProjectReference Lit。</summary>
        private static void V6GroupManifestOwnsLitSources(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var litDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Lit");
            var litCsprojPath = Path.Combine(litDir, "BetterUnturnedExperience.Lit.csproj");
            var pluginCsprojPath = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Plugin",
                "BetterUnturnedExperience.Plugin.csproj");
            Check(File.Exists(litCsprojPath) && File.Exists(pluginCsprojPath), "工程文件缺失（仓库结构破坏）");

            var litCsproj = File.ReadAllText(litCsprojPath);
            var pluginCsproj = File.ReadAllText(pluginCsprojPath);

            // 目录文件集（递归 .cs，排除 bin/obj，正斜杠——与防火墙 R2 同口径）。
            var dirFiles = Directory.GetFiles(litDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .Select(f => f.Substring(litDir.Length + 1).Replace('\\', '/'))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();
            var includes = Regex.Matches(litCsproj, "<Compile\\b[^>]*>").Cast<Match>()
                .Select(m => Regex.Match(m.Value, "\\bInclude\\s*=\\s*\"([^\"]*)\""))
                .Where(m => m.Success).Select(m => m.Groups[1].Value.Replace('\\', '/'))
                .Where(i => !i.StartsWith("..", StringComparison.Ordinal))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();

            var missing = dirFiles.Except(includes).ToList();
            var stale = includes.Except(dirFiles).ToList();
            Check(missing.Count == 0 && stale.Count == 0,
                "Lit csproj 自有清单应恰等目录文件集（R2 收编，作废票 02B）"
                + (missing.Count > 0 ? "；在目录、不在清单: " + string.Join(", ", missing) : "")
                + (stale.Count > 0 ? "；在清单、不在目录: " + string.Join(", ", stale) : ""));

            Check(!pluginCsproj.Contains("<Compile Include=\"..\\BetterUnturnedExperience.Lit\\"),
                "宿主 csproj 不得再平铺 Lit 源码（R4 摘除，作废票 02B）");
            Check(Regex.IsMatch(pluginCsproj, "<ProjectReference\\b[^>]*BetterUnturnedExperience\\.Lit\\.csproj"),
                "宿主应经组装根合法边工程引用 Lit（V6-T2 白名单）");
        }

        // ───────────────────────── 组 3：禁方向拆净 ─────────────────────────

        /// <summary>防火墙真实仓库：Lit→ClientUi / Lit→Plugin / ClientUi→Lit 不在册（作废票：
        /// 02B——页码范围按票面 Scope 走契约加性单源后 ClientUi→Lit 一并摘除；ClientUi→Plugin
        /// 工程环归 02E）。另自扫防火墙全限定名正则的盲区：Lit 源内的部分限定名越界 token
        /// （如 ClientUi.Internal.）不得存活。</summary>
        private static void V6GroupForbiddenDirectionsRemoved(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", FirewallScript);
            Check(File.Exists(script), "防火墙脚本缺失: eng/" + FirewallScript);
            var result = RunPowerShellScript(script, "-RepoRoot \"" + repoRoot + "\"", repoRoot, 300);

            Check(!result.Output.Contains("ref-direction Lit->ClientUi"),
                "禁方向应拆净: Lit→ClientUi 全限定名越界仍在册（作废票 02B）");
            Check(!result.Output.Contains("ref-direction Lit->Plugin"),
                "禁方向应拆净: Lit→Plugin 越界仍在册（作废票 02B）");
            Check(!result.Output.Contains("ref-direction ClientUi->Lit"),
                "禁方向应拆净: ClientUi→Lit 越界仍在册（页码范围经宿主绑定单源后摘除，作废票 02B）");

            // 部分限定名盲区自扫：防火墙正则 BetterUnturnedExperience\.[A-Za-z0-9]+ 只认全限定
            // token，ClientUi.Internal.（部分限定）能逃脱——本组补文本级扫描堵住同一方向。
            var litDir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.Lit");
            var offenders = new List<string>();
            var partial = new Regex("(^|[^\\w.])(ClientUi|Plugin|Core)\\.");
            foreach (var file in Directory.GetFiles(litDir, "*.cs", SearchOption.AllDirectories))
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
                "Lit 源应零部分限定名越界 token（防火墙正则盲区自扫，作废票 02B）: " + string.Join("; ", offenders));
        }

        // ───────────────────────── 组 4：宿主环公开缝 ─────────────────────────

        /// <summary>宿主环（面板组合 + 帧节拍 + 投影对账）在 Lit 拆出后仍经公开缝工作：
        ///   a) 转达口：整理提交同拍路由宿主转达口，且 Lit 不再直呼界面协调器（把转达口接到
        ///      记录器而非协调器——直呼若存活，协调器 hook 会被误触发）；作废票：02B。
        ///   b) 帧节拍（DEV-V6-11 定形）：宿主完成链不再有整理私有泵——节拍走冻结的
        ///      HostTick 事件缝，宿主时钟一拍即驱动接线实例的 dispatcher 拍。
        /// 新缝以反射定位（红先于实现：缝缺席即红），行为断言用真实模块。</summary>
        private static void V6GroupHostRingSeams(Action<bool, string> Check)
        {
            var seamType = typeof(LitFeatureAssembly);
            var pump = seamType.GetMethod("TickWiredModule",
                StaticPublic, null, Type.EmptyTypes, null);
            Check(pump == null, "宿主私有节拍泵缝应已拆除（DEV-V6-11：节拍走冻结 HostTick 事件缝，作废票 02B）");
            var bindRelay = seamType.GetMethod("BindProjectionRelay",
                StaticPublic, null, new[] { typeof(Action<byte, byte>) }, null);
            Check(bindRelay != null, "投影对账宿主转达口绑定缝 BindProjectionRelay 缺席（作废票 02B）");
            var wiredProperty = seamType.GetProperty("WiredModule");
            var resetWired = seamType.GetMethod("ResetWiredModule",
                StaticPublic, null, Type.EmptyTypes, null);
            Check(wiredProperty != null && resetWired != null,
                "接线实例契约投影缝（WiredModule/ResetWiredModule）缺席（作废票 02B）");

            // a) 转达口行为：真模块 + 真总线，提交后同拍路由。
            if (bindRelay != null)
            {
                var faultDir = NewLitFaultDirectory();
                try
                {
                    var module = ComposeStartedModule(faultDir, Check);
                    var relayPages = new List<KeyValuePair<byte, byte>>();
                    bindRelay.Invoke(null, new object[]
                    {
                        (Action<byte, byte>)((first, last) => relayPages.Add(new KeyValuePair<byte, byte>(first, last)))
                    });
                    var hookPages = new List<KeyValuePair<byte, byte>>();
                    BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.ReconcileHook =
                        (first, last) => hookPages.Add(new KeyValuePair<byte, byte>(first, last));
                    try
                    {
                        module.PublishTidyCompleted(3, 3, TidyCommitResult.Committed, 0UL, 1UL);
                        Check(relayPages.Count == 1 && relayPages[0].Key == 3 && relayPages[0].Value == 3,
                            "整理提交应同拍路由宿主转达口 (3,3)，实际 "
                            + relayPages.Count + " 页 " + (relayPages.Count > 0 ? relayPages[0].Key + ".." + relayPages[0].Value : ""));
                        Check(hookPages.Count == 0,
                            "Lit 不得再直呼界面协调器（转达口接到记录器时协调器 hook 不应被触发）");
                    }
                    finally
                    {
                        BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.ReconcileHook = null;
                    }
                }
                finally
                {
                    try { Directory.Delete(faultDir, true); } catch { }
                }
            }

            // b) 帧节拍行为：经登记路径启动的接线实例由冻结的 HostTick 缝驱动（宿主时钟
            //    与完成链同一根 BueHostEventRuntime；DEV-V6-11 拔泵后的唯一节拍来源）。
            if (wiredProperty != null && resetWired != null)
            {
                var feature = new FeatureId(LitFeatureAssembly.FeatureId);
                LitFeatureAssembly.ResetWiredModule();
                var litRuntime = new BetterUnturnedExperience.Core.Registration.FeatureRegistrationRuntime();
                litRuntime.OpenRegistration();
                Check(litRuntime.Register(LitFeatureAssembly.CreateRegistration()).Accepted,
                    "setup: 官方 LIT 登记入探针运行时（经公开缝）");
                Check(litRuntime.CompleteRuntime(), "setup: 目录冻结");
                BueFeatureStartRuntime.StartCatalog(litRuntime, NewLoopbackNetwork(4402UL));
                try
                {
                    var wired = LitFeatureAssembly.WiredModule;
                    Check(wired != null, "登记路径后接线实例应在座（工厂装配并投影）");
                    var markerRan = false;
                    var enqueued = MainThreadDispatcher.TryEnqueue(new QueuedTidyRequest
                    {
                        Work = () => markerRan = true,
                        Cancel = () => { },
                        Tag = "02B host-tick probe",
                    });
                    Check(enqueued, "setup: dispatcher 队列开放（模块代际已开）");
                    BueHostEventRuntime.EnsureCreated();
                    Check(BueHostEventRuntime.TickOnce(), "setup: 宿主时钟拍出一拍");
                    Check(markerRan, "帧节拍：冻结的 HostTick 缝一拍即驱动接线模块的 dispatcher 拍");
                }
                finally
                {
                    LitFeatureAssembly.ResetWiredModule();
                }
            }
        }

        /// <summary>与 Program.cs LitMultiplayerHarness.CreateModule 同构的最小启动：真总线 +
        /// 回环网络，生产组合路径（NetService 走生产装配），无测试接缝注入。</summary>
        private static InventoryTidyModule ComposeStartedModule(string faultDir, Action<bool, string> Check)
        {
            var feature = new FeatureId(LitRuntime.FeatureIdValue);
            var module = new InventoryTidyModule(feature);
            module.ScopeDirectoryForTests = faultDir;
            module.FaultContextForTests = () => new LitFaultScopeContext("02B", 1);
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var network = NewLoopbackNetwork(4401UL);
            var bootstrap = new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(
                default(FeatureScopeIdentity), 1UL, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature),
                null, null, null, network);
            var result = module.Start(bootstrap);
            Check(result.Started, "setup: 模块应经生产组合路径启动，实际 " + result.DiagnosticId);
            return module;
        }

        private static string NewLitFaultDirectory()
        {
            var dir = Path.Combine(Path.GetTempPath(), "bue-02b-red-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        // ───────────────────────── helpers ─────────────────────────

        private const System.Reflection.BindingFlags StaticPublic =
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;

        private static readonly object ProcessLock = new object();

        private static string PayloadText(FeatureDefinitionArtifact definition)
        {
            var bytes = new byte[definition.CanonicalPayload.Count];
            for (var index = 0; index < bytes.Length; index++) bytes[index] = definition.CanonicalPayload[index];
            return Encoding.UTF8.GetString(bytes);
        }

        private static BetterUnturnedExperience.Core.Network.BueNetworkRuntime NewLoopbackNetwork(ulong nonce)
        {
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            return new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), nonce);
        }

        private sealed class ProcessResult
        {
            public int ExitCode;
            public string Output;
            public string Errors;
            public string All { get { return Output + Environment.NewLine + Errors; } }
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
            return RunProcess("powershell",
                "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" " + arguments,
                workingDirectory, timeoutSeconds);
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
    }
}
