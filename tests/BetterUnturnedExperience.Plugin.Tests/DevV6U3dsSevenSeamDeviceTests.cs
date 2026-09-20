using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.NoOpFixture;
using BetterUnturnedExperience.Plugin;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-08 红测组：候选 DLL 与 NoOp 七缝 U3DS 装置化（V6-T7 交付 B）。
    /// 被测外部行为：
    ///   1. `eng/Run-U3dsSevenSeam.ps1` 判决器（-Judge &lt;目录&gt;）：读旁路文件
    ///      BUE-NoOpProbe-results.txt 上的七枚分缝结果机器判决——七步全 Passed
    ///      +probe-loaded 头+chain-complete 才 exit 0；缺任一分缝结果、任何
    ///      outcome≠Passed、探针未加载（无旁路/无头）、链未跑完（无
    ///      chain-complete）一律非零，且 REASON 逐条可定位（NotRun≠Passed）。
    ///   2. 探针落盘：NoOp 模块 Start 走冻结链序时把七枚 outcome 与 mismatch
    ///      详情逐缝追加到旁路文件（一步 Mismatch 不遮蔽其余缝步，链不中途
    ///      天折；StartFault 时零缝步、零 chain-complete）。
    ///   3. 两套退出码：装置与一条命令跑全套（Run-FullSuite.ps1）互不调用、
    ///      各自独立退出码——结构性守卫（不合成一个脚本）。
    /// 判决器对 U3DS 的部署/启动/回收流程由 -Plan 预检锁组合（红测接缝兼
    /// 操作员 affordance；真机 boot 只在实机验收跑，不进全套）。
    /// </summary>
    internal static class DevV6U3dsSevenSeamDeviceTests
    {
        private const string DeviceScript = "Run-U3dsSevenSeam.ps1";
        private const string FullsuiteScript = "Run-FullSuite.ps1";
        private const string SolutionFile = "BetterUnturnedExperience.sln";
        private const string SidecarFileName = "BUE-NoOpProbe-results.txt";
        private const string ProbeLinePrefix = "BUE-NOOP-PROBE-V1";
        // 冻结链序（NoOpModule.Start 的七缝顺序，与 SDK 附录 A/NoOp 探针同源）。
        internal static readonly string[] FrozenStepNames =
            { "bootstrap", "events", "lifecycle", "network", "hosttick", "settings", "logger" };

        private static int loopbackNonce = 4680;

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

                Group("判决器：七缝旁路机器判决（合成旁路文件）", () => V6GroupJudgeSyntheticSidecars(Check));
                Group("探针落盘：真实探针链的旁路写入", () => V6GroupProbeSidecarEmission(Check));
                Group("两套退出码与部署预检（-Plan）", () => V6GroupExitCodeSeparationAndPlan(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：判决器（合成旁路文件） ─────────────────────────

        /// <summary>每个案例一棵最小旁路工件树单独锁判决行为：全绿 exit 0；缺任一分缝结果、
        /// outcome≠Passed、探针未加载、链未跑完、协议头不符一律非零且 REASON 可定位。
        /// 突变（判决器摘任一判据）必令对应案例红。</summary>
        private static void V6GroupJudgeSyntheticSidecars(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", DeviceScript);
            Check(File.Exists(script), "装置脚本缺失: eng/" + DeviceScript + "（缺装置=红）");

            string[] GreenSidecar()
            {
                var lines = new List<string> { ProbeLinePrefix + " event=probe-loaded sha256=0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF" };
                foreach (var step in FrozenStepNames)
                    lines.Add(ProbeLinePrefix + " step=" + step + " outcome=Passed");
                lines.Add(ProbeLinePrefix + " event=chain-complete");
                return lines.ToArray();
            }

            void RunCase(string name, string[] sidecarLines, bool expectPass, string[] mustContain, string[] mustNotContain)
            {
                var root = WriteArtifactDir(sidecarLines);
                try
                {
                    var result = RunPowerShellScript(script, "-Judge \"" + root + "\"", repoRoot, 120);
                    if (expectPass)
                        Check(result.ExitCode == 0, name + ": 应 exit 0，实际 " + result.ExitCode + Environment.NewLine + result.All);
                    else
                        Check(result.ExitCode != 0, name + ": 应非零退出，实际 0" + Environment.NewLine + result.All);
                    foreach (var token in mustContain)
                        Check(result.Output.Contains(token), name + ": 缺判决标记 " + token + Environment.NewLine + result.Output);
                    foreach (var token in mustNotContain)
                        Check(!result.Output.Contains(token), name + ": 不应有判决标记 " + token + Environment.NewLine + result.Output);
                    Check(result.Output.Contains("U3DS-SEVEN-SEAM: " + (expectPass ? "PASS" : "FAIL")),
                        name + ": 缺总判行 U3DS-SEVEN-SEAM: " + (expectPass ? "PASS" : "FAIL") + Environment.NewLine + result.Output);
                }
                finally { Directory.Delete(root, true); }
            }

            // a) 全绿：七步全 Passed + 头 + chain-complete → exit 0，逐缝回显。
            RunCase("全绿旁路判决", GreenSidecar(), true,
                new[] { "[JUDGE] step=bootstrap outcome=Passed", "[JUDGE] step=logger outcome=Passed", "[JUDGE] chain=complete" },
                new[] { "REASON:" });

            // b) 缺一分缝结果（删 settings 行）→ 非零 + 点名缺失缝（NotRun≠Passed）。
            var missing = new List<string>(GreenSidecar());
            missing.RemoveAll(l => l.Contains("step=settings "));
            RunCase("缺 settings 缝行", missing.ToArray(), false,
                new[] { "REASON: step=settings missing", "U3DS-SEVEN-SEAM: FAIL" },
                new string[0]);

            // c) 缝步 Mismatch（network 翻红）→ 非零 + 点名缝与实测 outcome。
            var mismatch = new List<string>(GreenSidecar());
            for (var i = 0; i < mismatch.Count; i++)
                if (mismatch[i].Contains("step=network "))
                    mismatch[i] = ProbeLinePrefix + " step=network outcome=Mismatch";
            mismatch.Insert(mismatch.Count - 1, ProbeLinePrefix + " mismatch=network: owned=False send=Throttled");
            RunCase("network 缝 Mismatch", mismatch.ToArray(), false,
                new[] { "REASON: step=network outcome=Mismatch", "U3DS-SEVEN-SEAM: FAIL" },
                new[] { "REASON: step=settings" });

            // d) 探针未加载（无旁路文件）→ 非零 + probe-not-loaded。
            RunCase("旁路文件缺失（探针未加载）", null, false,
                new[] { "REASON: probe-not-loaded", "U3DS-SEVEN-SEAM: FAIL" },
                new string[0]);

            // e) 探针加载但链未跑（仅头、零缝步）→ 非零，七缝逐条 missing。
            RunCase("仅加载头零缝步", new[] { ProbeLinePrefix + " event=probe-loaded sha256=0123" }, false,
                new[] { "REASON: step=bootstrap missing", "REASON: step=logger missing", "U3DS-SEVEN-SEAM: FAIL" },
                new string[0]);

            // f) 无协议头（只有缝步，缺 probe-loaded）→ 非零 + probe-not-loaded。
            var noHeader = new List<string>();
            foreach (var step in FrozenStepNames) noHeader.Add(ProbeLinePrefix + " step=" + step + " outcome=Passed");
            noHeader.Add(ProbeLinePrefix + " event=chain-complete");
            RunCase("缺 probe-loaded 头", noHeader.ToArray(), false,
                new[] { "REASON: probe-not-loaded", "U3DS-SEVEN-SEAM: FAIL" },
                new string[0]);

            // g) 链未跑完（有七步、无 chain-complete）→ 非零 + chain-incomplete。
            var noChain = new List<string>(GreenSidecar());
            noChain.RemoveAt(noChain.Count - 1);
            RunCase("缺 chain-complete", noChain.ToArray(), false,
                new[] { "REASON: chain-incomplete", "U3DS-SEVEN-SEAM: FAIL" },
                new string[0]);

            // h) 协议版本不符（V2 行不认）→ 非零 + probe-not-loaded（V1 判决器只认 V1 行）。
            var wrongVersion = new List<string>();
            foreach (var line in GreenSidecar()) wrongVersion.Add(line.Replace(ProbeLinePrefix, "BUE-NOOP-PROBE-V2"));
            RunCase("V2 协议行不认", wrongVersion.ToArray(), false,
                new[] { "REASON: probe-not-loaded", "U3DS-SEVEN-SEAM: FAIL" },
                new string[0]);
        }

        // ───────────────────────── 组 2：探针落盘（真实探针链） ─────────────────────────

        /// <summary>真实探针链（StartCatalog 生产组合）把七枚分缝结果逐缝追加到旁路文件：
        /// 绿链=七步 Passed 且冻结链序落盘+chain-complete；缺口定位=单缝 Mismatch 不遮蔽
        /// 其余缝步、mismatch 详情行在案、链仍跑完；StartFault=零缝步零完成标记（链未跑
        /// 不得伪造成跑完）。旁路路径=夹具程序集旁边（测试前清理默认落点）。</summary>
        private static void V6GroupProbeSidecarEmission(Action<bool, string> Check)
        {
            var sidecarPath = DefaultSidecarPath();

            string[] ProbeLines()
            {
                return File.ReadAllLines(sidecarPath);
            }

            string[] StepLines(string[] lines)
            {
                var steps = new List<string>();
                foreach (var line in lines)
                    if (line.StartsWith(ProbeLinePrefix + " step=", StringComparison.Ordinal))
                        steps.Add(line);
                return steps.ToArray();
            }

            // a) 绿链：七步全 Passed 且按冻结链序落盘，chain-complete 在案，零 mismatch。
            if (File.Exists(sidecarPath)) File.Delete(sidecarPath);
            var greenProbe = RunProbeChain(NoOpProbeFault.None, new List<string>());
            Check(greenProbe != null && greenProbe.Started, "落盘·绿链 setup：探针链经真实 StartCatalog 跑完");
            Check(File.Exists(sidecarPath), "落盘·绿链：旁路文件应落在夹具程序集旁 " + sidecarPath);
            var greenLines = ProbeLines();
            var greenSteps = StepLines(greenLines);
            Check(greenSteps.Length == 7, "落盘·绿链：应恰七条缝步行，实际 " + greenSteps.Length + Environment.NewLine + string.Join(Environment.NewLine, greenLines));
            for (var i = 0; i < FrozenStepNames.Length; i++)
            {
                Check(i < greenSteps.Length && greenSteps[i] == ProbeLinePrefix + " step=" + FrozenStepNames[i] + " outcome=Passed",
                    "落盘·绿链：第 " + (i + 1) + " 缝应按冻结链序为 " + FrozenStepNames[i] + " outcome=Passed，实际 "
                    + (i < greenSteps.Length ? greenSteps[i] : "<缺>") + Environment.NewLine + string.Join(Environment.NewLine, greenLines));
            }
            var greenHasChain = false;
            foreach (var line in greenLines)
                if (line == ProbeLinePrefix + " event=chain-complete") greenHasChain = true;
            Check(greenHasChain, "落盘·绿链：chain-complete 应在案" + Environment.NewLine + string.Join(Environment.NewLine, greenLines));
            var greenMismatchCount = 0;
            foreach (var line in greenLines)
                if (line.StartsWith(ProbeLinePrefix + " mismatch=", StringComparison.Ordinal)) greenMismatchCount++;
            Check(greenMismatchCount == 0, "落盘·绿链：零 mismatch 详情行，实际 " + greenMismatchCount);

            // b) 缺口定位：SettingsCommitExpectation 只翻 settings 缝，其余六缝照常 Passed、
            //    mismatch 详情行在案、链仍跑完（一步翻红不遮蔽其余缝）。
            if (File.Exists(sidecarPath)) File.Delete(sidecarPath);
            var faultProbe = RunProbeChain(NoOpProbeFault.SettingsCommitExpectation, new List<string>());
            Check(faultProbe != null && faultProbe.Started && faultProbe.SettingsStep == ProbeStepOutcome.Mismatch,
                "落盘·缺口 setup：settings 缝应 Mismatch 而链照常跑完");
            var faultLines = ProbeLines();
            var faultSteps = StepLines(faultLines);
            Check(faultSteps.Length == 7, "落盘·缺口：七条缝步行都应在案（单缝翻红不吞行），实际 " + faultSteps.Length);
            foreach (var line in faultSteps)
            {
                if (line.Contains("step=settings "))
                    Check(line == ProbeLinePrefix + " step=settings outcome=Mismatch",
                        "落盘·缺口：settings 缝应 outcome=Mismatch，实际 " + line);
                else
                    Check(line.EndsWith(" outcome=Passed", StringComparison.Ordinal),
                        "落盘·缺口：其余缝应保持 Passed，实际 " + line);
            }
            var faultHasMismatchDetail = false;
            foreach (var line in faultLines)
                if (line.StartsWith(ProbeLinePrefix + " mismatch=settings:", StringComparison.Ordinal)) faultHasMismatchDetail = true;
            Check(faultHasMismatchDetail, "落盘·缺口：settings 的 mismatch 详情行应在案" + Environment.NewLine + string.Join(Environment.NewLine, faultLines));

            // c) StartFault 守卫：Start 即抛 → 零缝步、零 chain-complete（链未跑不得伪造成跑完）。
            if (File.Exists(sidecarPath)) File.Delete(sidecarPath);
            var faultStartProbe = RunProbeChain(NoOpProbeFault.StartFault, new List<string>());
            Check(faultStartProbe != null && !faultStartProbe.Started, "落盘·StartFault setup：模块应被隔离未启动");
            if (File.Exists(sidecarPath))
            {
                var startFaultLines = ProbeLines();
                foreach (var line in startFaultLines)
                {
                    Check(!line.StartsWith(ProbeLinePrefix + " step=", StringComparison.Ordinal),
                        "落盘·StartFault：不得出现缝步行，实际 " + line);
                    Check(line != ProbeLinePrefix + " event=chain-complete",
                        "落盘·StartFault：不得出现 chain-complete（链未跑）");
                }
            }
            // d) 生产 not-ready 形状（实机缝隙红测）：U3DS 上目录启动走 scene-loaded
            //    驱动、网络运行时 arm 晚于探针 Start——探针在真机面对的是 never-attached
            //    的 DeferredBueNetworkApi，其冻结 not-ready 投影把一切发送答成 NoSession。
            //    network 缝判据必须接受这一显式不投递投影（活门分支 ChannelNotRegistered
            //    由宿主套件 loopback 已附着路径钉死），否则装置在真机永远红。
            var deferredNetwork = new BetterUnturnedExperience.Core.Network.DeferredBueNetworkApi(null);
            var deferredProbe = RunProbeChain(NoOpProbeFault.None, new List<string>(), deferredNetwork);
            Check(deferredProbe != null && deferredProbe.Started, "落盘·not-ready setup：探针链照常跑完");
            Check(deferredProbe.NetworkStep == ProbeStepOutcome.Passed,
                "落盘·not-ready：network 缝在 Deferred facade 未附着时应 Passed（显式不投递投影），实际 "
                + deferredProbe.NetworkStep + " send=" + deferredProbe.NetworkSendObserved
                + " wrong=" + deferredProbe.NetworkWrongChannelObserved);
            Check(deferredProbe.NetworkSendObserved == BetterUnturnedExperience.Contracts.BueNetwork.NetworkSendResult.NoSession
                && deferredProbe.NetworkWrongChannelObserved == BetterUnturnedExperience.Contracts.BueNetwork.NetworkSendResult.NoSession
                && deferredProbe.NetworkSessionsAtStart == 0,
                "落盘·not-ready：无会话=NoSession、错挂通道在未附着 facade 下=NoSession（冻结 not-ready 投影）、会话数=0");

            if (File.Exists(sidecarPath)) File.Delete(sidecarPath);
        }

        // ───────────────────────── 组 3：两套退出码与部署预检 ─────────────────────────

        /// <summary>a) 结构守卫：装置与一条命令跑全套互不调用（不合成一个脚本，退出码体系分离）；
        /// b/c) -Plan 预检：合成 U3DS 根布局全在位 → exit 0；缺 Unturned.exe → exit 1 + PLAN MISSING。
        /// 突变（把两脚本合一/删预检组件检查）必令对应断言红。</summary>
        private static void V6GroupExitCodeSeparationAndPlan(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var device = Path.Combine(repoRoot, "eng", DeviceScript);
            var fullsuite = Path.Combine(repoRoot, "eng", FullsuiteScript);
            Check(File.Exists(fullsuite), "一条命令驱动器缺失: eng/" + FullsuiteScript);
            Check(File.Exists(device), "装置脚本缺失: eng/" + DeviceScript + "（缺装置=红）");

            // a) 互不调用：任一脚本的可执行行不引用对方（不合成一个脚本，退出码体系分离；
            //    注释行提及对方名字属文档不算调用）。
            if (File.Exists(device))
            {
                Check(!ExecutableLines(device).Contains(FullsuiteScript),
                    "装置脚本不得调用 " + FullsuiteScript + "（两套退出码须分离）");
                Check(File.ReadAllText(device).Contains("U3DS-SEVEN-SEAM:"),
                    "装置脚本须声明自己的总判行 U3DS-SEVEN-SEAM:（独立退出码体系）");
            }
            Check(!ExecutableLines(fullsuite).Contains(DeviceScript),
                "一条命令跑全套不得调用装置脚本 " + DeviceScript + "（两套退出码须分离）");

            // b) -Plan：合成 U3DS 根布局全在位 → exit 0，组合行齐。
            var goodRoot = Path.Combine(Path.GetTempPath(), "bue-v6-08-u3ds-root-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                Directory.CreateDirectory(Path.Combine(goodRoot, "BepInEx", "plugins"));
                Directory.CreateDirectory(Path.Combine(goodRoot, "BepInEx", "core"));
                File.WriteAllText(Path.Combine(goodRoot, "Unturned.exe"), string.Empty);
                File.WriteAllText(Path.Combine(goodRoot, "BepInEx", "core", "BepInEx.dll"), string.Empty);
                var plan = RunPowerShellScript(device, "-Plan -U3dsRoot \"" + goodRoot + "\"", repoRoot, 120);
                Check(plan.ExitCode == 0, "-Plan 布局在位应 exit 0，实际 " + plan.ExitCode + Environment.NewLine + plan.All);
                Check(plan.Output.Contains("PLAN complete"), "-Plan 应以 PLAN complete 收尾" + Environment.NewLine + plan.Output);
                Check(plan.Output.Contains("-t:Rebuild"), "-Plan 必须声明重建用短横线开关 -t:Rebuild");
                Check(plan.Output.Contains("BuePublishedDll"), "-Plan 必须声明探针按发布候选 BuePublishedDll 编译");
                Check(plan.Output.Contains("published candidate"), "-Plan 必须明确七缝验收绑定发布候选，而非未合并 Plugin 中间 DLL");
                var deviceScriptText = File.ReadAllText(device);
                Check(ExecutableLines(device).Contains("-p:BuePublishedDll="), "装置构建必须把 BuePublishedDll 传给 NoOpFixture");
                Check(!ExecutableLines(device).Contains("$pluginDllRel = 'src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll'"), "装置不得把未合并 Plugin/bin/Release DLL 当发布候选");
                Check(deviceScriptText.Contains("PublishedDll"), "装置脚本必须保留候选路径与身份绑定");
                Check(plan.Output.Contains("Unturned.exe"), "-Plan 必须声明启动 U3DS 可执行文件");
                Check(plan.Output.Contains(SidecarFileName), "-Plan 必须声明旁路文件 " + SidecarFileName);
            }
            finally { Directory.Delete(goodRoot, true); }

            // c) -Plan：根布局缺 Unturned.exe → exit 1 + PLAN MISSING。
            var badRoot = Path.Combine(Path.GetTempPath(), "bue-v6-08-u3ds-root-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                Directory.CreateDirectory(Path.Combine(badRoot, "BepInEx", "plugins"));
                var plan = RunPowerShellScript(device, "-Plan -U3dsRoot \"" + badRoot + "\"", repoRoot, 120);
                Check(plan.ExitCode == 1, "-Plan 布局缺组件应 exit 1，实际 " + plan.ExitCode + Environment.NewLine + plan.All);
                Check(plan.Output.Contains("PLAN MISSING"), "-Plan 缺组件须报 PLAN MISSING" + Environment.NewLine + plan.Output);
            }
            finally { Directory.Delete(badRoot, true); }
        }

        // ───────────────────────── 共享助手 ─────────────────────────

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, SolutionFile)))
                dir = dir.Parent;
            if (dir == null)
                throw new InvalidOperationException("仓库根未定位（从测试基目录上溯未找到 " + SolutionFile + "）");
            return dir.FullName;
        }

        private sealed class ProcessResult
        {
            public int ExitCode;
            public string Output;
            public string Errors;
            public string All { get { return Output + Environment.NewLine + Errors; } }
        }

        private static readonly object ProcessLock = new object();

        /// <summary>孵化 powershell.exe 跑 -File 脚本；超时杀进程；输出按 UTF-8 收。</summary>
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

        /// <summary>临时目录下一棵工件树：可选旁路文件行集；返回目录路径，调用方负责删除。</summary>
        private static string WriteArtifactDir(string[] sidecarLines)
        {
            var root = Path.Combine(Path.GetTempPath(), "bue-v6-08-device-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(root);
            if (sidecarLines != null)
                File.WriteAllText(Path.Combine(root, SidecarFileName), string.Join(Environment.NewLine, sidecarLines) + Environment.NewLine, new UTF8Encoding(false));
            return root;
        }

        /// <summary>NoOp 探针旁路文件的默认落点（夹具程序集旁边）——装置契约路径，测试独立推导。</summary>
        private static string DefaultSidecarPath()
        {
            var fixtureDir = Path.GetDirectoryName(typeof(NoOpFeaturePlugin).Assembly.Location);
            return Path.Combine(fixtureDir, SidecarFileName);
        }

        /// <summary>脚本的可执行行拼文（剥离 # 注释行）——互不调用守卫的取材面。</summary>
        private static string ExecutableLines(string scriptPath)
        {
            var sb = new StringBuilder();
            foreach (var line in File.ReadAllLines(scriptPath))
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        /// <summary>一次完整探针链运行：独立注册运行时+临时持久根+干净诊断/投递账+唯一
        /// nonce 环回网络（与 DEV-V3-08 统一探针组同卫生模式）。返回链末探针状态；
        /// 运行完即 StopAll 复位。featureNetwork 可注入 never-attached 的 Deferred
        /// facade 以复现生产 U3DS 的 not-ready 形状（arm 晚于目录启动）。</summary>
        private static NoOpFeatureRegistration.ProbeState RunProbeChain(NoOpProbeFault fault, List<string> capturedLines, BetterUnturnedExperience.Contracts.BueNetwork.IBueNetworkApi featureNetwork = null)
        {
            var runtime = new BetterUnturnedExperience.Core.Registration.FeatureRegistrationRuntime();
            var previousRuntime = BueRuntimeHost.CurrentRuntime;
            var previousRecorder = BueRuntimeLog.Recorder;
            if (capturedLines == null) capturedLines = new List<string>();
            var root = Path.Combine(Path.GetTempPath(), "BUE-V6-08-" + Guid.NewGuid().ToString("N"));
            probeSettingsRoots.Add(root);
            BueRuntimeHost.Bind(runtime);
            BueRuntimeLog.Recorder = capturedLines.Add;
            try
            {
                BueSettingsRuntime.Clear();
                BueSettingsRuntime.EnsureCreated(root, () => true, null);
                BueDiagnosticsRuntime.Clear();
                BueMainThreadRuntime.Clear();
                BueHostEventRuntime.EnsureCreated();
                NoOpFeatureRegistration.NextProbeFault = fault;
                NoOpFeatureRegistration.ResetLastProbe();
                runtime.OpenRegistration();
                if (!runtime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted)
                    throw new InvalidOperationException("RunProbeChain setup：样例登记受理失败");
                if (!runtime.CompleteRuntime())
                    throw new InvalidOperationException("RunProbeChain setup：目录冻结失败");
                BueFeatureStartRuntime.StartCatalog(runtime, featureNetwork ?? NewLoopbackNetwork((ulong)loopbackNonce++));
                BueHostEventRuntime.TickOnce();
                var probe = NoOpFeatureRegistration.LastProbe;
                BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                return probe;
            }
            finally
            {
                NoOpFeatureRegistration.NextProbeFault = NoOpProbeFault.None;
                BueRuntimeLog.Recorder = previousRecorder;
                BueRuntimeHost.Bind(previousRuntime);
                BueDiagnosticsRuntime.Clear();
                BueMainThreadRuntime.Clear();
            }
        }

        private static readonly List<string> probeSettingsRoots = new List<string>();

        private static BetterUnturnedExperience.Core.Network.BueNetworkRuntime NewLoopbackNetwork(ulong nonce)
        {
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            return new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), nonce);
        }
    }
}
