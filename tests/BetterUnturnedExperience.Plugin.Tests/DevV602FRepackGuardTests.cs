using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-02F 红测组：发布期合并（ILRepack）+ 工程防火墙守卫态。
    /// 被测外部行为（票 20 / V6-T2 合并与门禁 / V6-T7 交付 A）：
    ///   1. `eng/Invoke-ReleaseRepack.ps1`：-Plan 预检合并集（八工程封闭集：契约/核心/界面/三官方功能/
    ///      Bii 骨架/宿主；发布资格/传输适配/探针夹具/测试不进合并集）；输入缺一即退出 1 并点名；
    ///      -Rebuild 用短横线开关重建八源工程（不含测试工程）；打包产玩家单文件 DLL，
    ///      输出目录恰一个 BetterUnturnedExperience.dll；脚注 sha256 机器可读。
    ///   2. 合并产物引用面零 BetterUnturnedExperience.*（玩家单文件闭包）；八合并工程类型在位；
    ///      排除工程（Transport/NoOpFixture/Release）类型不在。
    ///   3. 三轮「重建+打包」文件哈希逐字节一致（验收锚=文件哈希，不是程序集元数据）。
    ///   4. 防火墙守卫态：真实仓库零命中（02B..02E 拆净 + 02F 两条具名非功能边收编后）；
    ///      合成树植入禁方向 → 驱动器总失败（守卫绕过不可达）；Transport→Core（适配器实现宿主
    ///      传输缝端口）与 NoOpFixture→Plugin（保留段第二插件经公开登记门消费宿主）两条具名边放行，
    ///      封闭枚举不开模式（功能/其它方向照旧在册）。
    /// 突变见 audit/2026-09-19/DEV-V6-02F/mutations/（删输入/混入排除件/翻回度量态/摘具名边/注时效字节）。
    /// </summary>
    internal static class DevV602FRepackGuardTests
    {
        private const string RepackScript = "Invoke-ReleaseRepack.ps1";
        private const string FirewallScript = "Verify-ProjectFirewall.ps1";
        private const string DriverScript = "Run-FullSuite.ps1";
        private const string SolutionFile = "BetterUnturnedExperience.sln";
        private static readonly string[] MergeTargets = { "Contracts", "Core", "ClientUi", "Lit", "Lir", "Lht", "Bii" };
        private static readonly string[] ExcludedProjects = { "Release", "Transport", "NoOpFixture" };

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

                Group("发布期合并 -Plan 预检（真实仓库）", () => V6GroupRepackPlan(Check));
                Group("合并缺步红（合成输入树）", () => V6GroupRepackMissingInput(Check));
                Group("合并产物判定（真实仓库：单文件+引用面+类型在位）", () => V6GroupRepackArtifact(Check));
                Group("三轮重建逐字节一致（真实仓库）", () => V6GroupRebuildDeterminism(Check));
                Group("防火墙守卫态照相机（真实仓库零命中）", () => V6GroupGuardCamera(Check));
                Group("守卫语义（合成树植入违规=总失败）", () => V6GroupGuardBite(Check));
                Group("防火墙具名边封闭性（合成树）", () => V6GroupNamedEdges(Check));
                Group("合并集与防火墙白名单同源", () => V6GroupMergeSetSameSource(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：发布期合并 -Plan 预检 ─────────────────────────

        /// <summary>打包脚本存在；-Plan 对真实仓库退出 0（八输入在位）；列出八合并输入与排除集；
        /// 声明 ILRepack 工具与 -t:Rebuild 重建路径。删任一输入/换工具/换开关的突变必令本组红。</summary>
        private static void V6GroupRepackPlan(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", RepackScript);
            Check(File.Exists(script), "发布期合并脚本缺失: eng/" + RepackScript + "（缺合并=红，票 20 验收①）");

            var result = RunPowerShellScript(script, "-Plan", repoRoot, 300);
            Check(result.ExitCode == 0, "-Plan 预检应退出 0（真实仓库八输入在位），实际 exit=" + result.ExitCode + Environment.NewLine + result.All);
            Check(!result.Output.Contains("PLAN MISSING"), "-Plan 不得报告输入缺失: " + result.Output);

            // 合并集=八工程封闭集：宿主为主件 + 七个合并目标。逐个点名（缺一即红）。
            Check(result.Output.Contains("primary=Plugin") || result.Output.Contains("primary=BetterUnturnedExperience.Plugin"),
                "-Plan 必须点名合并主件=宿主（玩家 DLL 身份来源）: " + result.Output);
            foreach (var target in MergeTargets)
                Check(result.Output.Contains("BetterUnturnedExperience." + target),
                    "-Plan 缺合并输入声明: BetterUnturnedExperience." + target + Environment.NewLine + result.Output);

            // 排除集如实声明：发布资格/传输适配/探针夹具/测试不进合并集（票 20 Scope）。
            foreach (var excluded in ExcludedProjects)
                Check(result.Output.Contains(excluded), "-Plan 须声明排除工程: " + excluded + Environment.NewLine + result.Output);

            Check(result.Output.Contains("ILRepack"), "-Plan 必须声明合并工具 ILRepack: " + result.Output);
            Check(result.Output.Contains("-t:Rebuild"), "-Plan 必须声明重建用短横线开关 -t:Rebuild");
        }

        // ───────────────────────── 组 2：合并缺步红（合成输入树） ─────────────────────────

        /// <summary>合成树放八份占位输入 → -Plan 过；摘掉任一输入 → -Plan 退出 1 并点名缺席件
        /// （合并缺步时红，票 20 验收①）。</summary>
        private static void V6GroupRepackMissingInput(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", RepackScript);
            var tool = Path.Combine(repoRoot, "tools", "ILRepack", "ILRepack.exe");
            Check(File.Exists(tool), "合并工具缺失: tools/ILRepack/ILRepack.exe（vendored 工具须入库）");
            var root = WriteSyntheticRepo(mergeSetStubs: true, firewallScript: false);
            try
            {
                // -Tool 显式指向真实工具（合成仓库树不落工具本体）: 本组锁的是「输入缺一即红」语义。
                var ok = RunPowerShellScript(script, "-Plan -Tool \"" + tool + "\" -RepoRoot \"" + root + "\"", repoRoot, 300);
                Check(ok.ExitCode == 0, "合成树八输入齐全时 -Plan 应退出 0，实际 exit=" + ok.ExitCode + Environment.NewLine + ok.All);

                var missing = Path.Combine(root, "src", "BetterUnturnedExperience.Lht", "bin", "Release", "BetterUnturnedExperience.Lht.dll");
                File.Delete(missing);
                var red = RunPowerShellScript(script, "-Plan -Tool \"" + tool + "\" -RepoRoot \"" + root + "\"", repoRoot, 300);
                Check(red.ExitCode == 1, "合并输入缺一（Lht 摘除）时 -Plan 应退出 1，实际 exit=" + red.ExitCode + Environment.NewLine + red.All);
                Check(red.Output.Contains("BetterUnturnedExperience.Lht"), "缺席输入须被点名（可定位）: " + red.Output);
            }
            finally { Directory.Delete(root, true); }
        }

        // ───────────────────────── 组 3：合并产物判定（真实仓库） ─────────────────────────

        /// <summary>真实仓库打包（不重建，前置=套件 Build 步）到临时目录：退出 0；输出目录恰一个
        /// BetterUnturnedExperience.dll（玩家单文件）；脚注 sha256 可解析；合并产物经 Mono.Cecil
        /// 元数据检查——引用面零 BetterUnturnedExperience.*（单文件闭包）、八合并工程类型在位、
        /// 排除工程类型不在。把 Transport/NoOpFixture/Release 混进合并集的突变必令本组红。</summary>
        private static void V6GroupRepackArtifact(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", RepackScript);
            var outDir = Path.Combine(Path.GetTempPath(), "bue-v6-02f-repack-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            var result = RunPowerShellScript(script, "-OutDir \"" + outDir + "\"", repoRoot, 600);
            try
            {
                Check(result.ExitCode == 0, "发布期合并应退出 0（真实仓库八输入在位），实际 exit=" + result.ExitCode + Environment.NewLine + result.All);

                var dlls = Directory.Exists(outDir) ? Directory.GetFiles(outDir, "*.dll") : new string[0];
                Check(dlls.Length == 1, "玩家侧发布物=恰一个 DLL，实际 " + dlls.Length + " 个: " + string.Join(", ", dlls));
                Check(dlls.Length == 1 && Path.GetFileName(dlls[0]) == "BetterUnturnedExperience.dll",
                    "玩家 DLL 身份=BetterUnturnedExperience.dll（宿主主件身份），实际: " + (dlls.Length == 1 ? Path.GetFileName(dlls[0]) : "?"));
                var sha = Regex.Match(result.Output, @"sha256=([0-9A-Fa-f]{64})");
                Check(sha.Success, "打包脚注缺机器可读 sha256: " + result.Output);
                if (dlls.Length == 1 && sha.Success)
                {
                    var actual = Sha256OfFile(dlls[0]);
                    Check(string.Equals(actual, sha.Groups[1].Value, StringComparison.OrdinalIgnoreCase),
                        "脚注 sha256 与输出文件实哈希不一致: 脚注=" + sha.Groups[1].Value + " 实际=" + actual);
                }

                // 合并产物元数据检查（Mono.Cecil 只读元数据：不解析依赖、不加载程序集、零身份冲突）。
                if (dlls.Length == 1)
                {
                    var assemblyName = InspectMergedAssembly(dlls[0], out var refs, out var typeNames);
                    Check(assemblyName == "BetterUnturnedExperience",
                        "合并产物身份名=BetterUnturnedExperience（宿主主件身份），实际: " + assemblyName);

                    var bueRefs = refs.Where(r => r.StartsWith("BetterUnturnedExperience", StringComparison.Ordinal)).ToList();
                    Check(bueRefs.Count == 0, "合并产物引用面不得含任何 BetterUnturnedExperience.*（玩家单文件闭包），实际: " + string.Join(", ", bueRefs) + "；全部引用: " + string.Join(", ", refs));

                    var types = string.Join(Environment.NewLine, typeNames);
                    Check(typeNames.Contains("BetterUnturnedExperience.Contracts.FeatureId"), "契约类型应在合并产物内: FeatureId");
                    Check(typeNames.Contains("BetterUnturnedExperience.Core.Network.LocalLoopbackTransport"), "核心类型应在合并产物内: LocalLoopbackTransport");
                    Check(typeNames.Contains("BetterUnturnedExperience.ClientUi.ClientUiFeatureAssembly"), "界面唯一公开组装类型应在合并产物内: ClientUiFeatureAssembly");
                    Check(typeNames.Contains("BetterUnturnedExperience.Lit.LitFeatureAssembly"), "整理公开组装类型应在合并产物内: LitFeatureAssembly");
                    Check(typeNames.Contains("BetterUnturnedExperience.Lir.LirFeatureAssembly"), "换弹公开组装类型应在合并产物内: LirFeatureAssembly");
                    Check(typeNames.Contains("BetterUnturnedExperience.Lht.LhtFeatureAssembly"), "尸潮公开组装类型应在合并产物内: LhtFeatureAssembly");
                    Check(typeNames.Contains("BetterUnturnedExperience.Bii.BiiFeatureAssembly"), "Bii 骨架公开组装类型应在合并产物内（02A 骨架已在，票 20 合并集）: BiiFeatureAssembly");
                    Check(typeNames.Contains("BetterUnturnedExperience.Plugin.BueRuntimeHost"), "宿主登记门应在合并产物内: BueRuntimeHost");

                    Check(!types.Contains("BetterUnturnedExperience.Transport."), "传输适配工程不得进合并产物: " + result.Output);
                    Check(!types.Contains("BetterUnturnedExperience.NoOpFixture."), "探针夹具工程不得进合并产物: " + result.Output);
                    Check(!types.Contains("BetterUnturnedExperience.Release."), "发布资格工程不得进合并产物: " + result.Output);
                }
            }
            finally
            {
                try { if (Directory.Exists(outDir)) Directory.Delete(outDir, true); } catch (Exception) { }
            }
        }

        // ───────────────────────── 组 4：三轮重建逐字节一致（真实仓库） ─────────────────────────

        /// <summary>三次「-Rebuild（八源工程 -t:Rebuild 短横线开关，不含测试工程）+ 打包」循环，
        /// 脚注 sha256 三轮相等（票 20 验收②：验收锚=文件哈希，不是程序集元数据）。重建路径换
        /// 工程集/打包引入时效字节的突变必令本组红。</summary>
        private static void V6GroupRebuildDeterminism(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", RepackScript);
            var hashes = new List<string>();
            for (var cycle = 1; cycle <= 3; cycle++)
            {
                var outDir = Path.Combine(Path.GetTempPath(), "bue-v6-02f-det" + cycle + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                var result = RunPowerShellScript(script, "-Rebuild -OutDir \"" + outDir + "\"", repoRoot, 900);
                try
                {
                    Check(result.ExitCode == 0, "重建+打包循环 " + cycle + "/3 应退出 0，实际 exit=" + result.ExitCode + Environment.NewLine + result.All);
                    var sha = Regex.Match(result.Output, @"sha256=([0-9A-Fa-f]{64})");
                    Check(sha.Success, "循环 " + cycle + "/3 脚注缺 sha256: " + result.Output);
                    if (sha.Success) hashes.Add(sha.Groups[1].Value.ToUpperInvariant());
                }
                finally
                {
                    try { if (Directory.Exists(outDir)) Directory.Delete(outDir, true); } catch (Exception) { }
                }
            }
            Check(hashes.Count == 3, "三轮循环须各产出一份哈希，实际 " + hashes.Count);
            Check(hashes.Count == 3 && hashes[0] == hashes[1] && hashes[1] == hashes[2],
                "三轮重建+打包须逐字节一致（文件哈希验收锚），实际: " + string.Join(" vs ", hashes));
        }

        // ───────────────────────── 组 5：防火墙守卫态照相机（真实仓库） ─────────────────────────

        /// <summary>真实仓库防火墙退出 0（02B..02E 拆净 + 02F 两条具名边收编后零命中）；驱动器
        /// -Steps Firewall 总判 PASS；度量态痕迹（METRICS 状态行/「命中不计入总失败」）全部退役。
        /// 回潮或度量态翻回的突变必令本组红（作废票 02F：原度量照相断言随守卫翻转更新）。</summary>
        private static void V6GroupGuardCamera(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var firewall = Path.Combine(repoRoot, "eng", FirewallScript);
            var result = RunPowerShellScript(firewall, "-RepoRoot \"" + repoRoot + "\"", repoRoot, 300);
            Check(result.ExitCode == 0, "守卫态: 真实仓库防火墙应退出 0（零命中），实际 exit=" + result.ExitCode + Environment.NewLine + result.All);
            var footer = Regex.Match(result.Output, @"Project-firewall metrics: (\d+) violation\(s\) rules-fired=(\S+)");
            Check(footer.Success, "防火墙输出缺度量脚注「Project-firewall metrics: N violation(s) rules-fired=…」");
            Check(footer.Success && footer.Groups[1].Value == "0", "守卫态: 真实仓库应零违规（02B..02E 拆净+02F 具名边收编），实际 violations=" + (footer.Success ? footer.Groups[1].Value : "?") + Environment.NewLine + result.Output);
            Check(footer.Success && footer.Groups[2].Value == "-", "守卫态: rules-fired 应为 -（无规则开火），实际 " + (footer.Success ? footer.Groups[2].Value : "?"));

            var driver = Path.Combine(repoRoot, "eng", DriverScript);
            var run = RunPowerShellScript(driver, "-Steps Firewall", repoRoot, 300);
            Check(run.ExitCode == 0, "守卫态: 驱动器 -Steps Firewall 应总判 PASS，实际 exit=" + run.ExitCode + Environment.NewLine + run.All);
            Check(run.Output.Contains("[Firewall] PASS") && run.Output.Contains("FULLSUITE: PASS"),
                "守卫态: 防火墙步骤行与汇总应为 PASS" + Environment.NewLine + run.Output);
            Check(!run.Output.Contains("METRICS") && !run.Output.Contains("命中不计入总失败"),
                "度量态痕迹应随 02F 退役（METRICS 状态行/命中不计入总失败措辞不得再现）: " + run.Output);
            Check(!run.Output.Contains("metrics="), "汇总不应再有 metrics 桶（守卫态无照相豁免）: " + run.Output);
        }

        // ───────────────────────── 组 6：守卫语义（合成树植入违规=总失败） ─────────────────────────

        /// <summary>合成仓库树（sln 桩 + 防火墙脚本副本 + 植入 Lir→Plugin 禁方向的 src）：
        /// 以合成根为工作目录跑真实驱动器 -Steps Firewall → [Firewall] FAILED + FULLSUITE: FAIL
        /// + 退出 1（守卫命中即总失败，绕过不可达）；干净合成树同法 → PASS 退出 0。
        /// 把守卫翻回度量容忍的突变必令植入案红。</summary>
        private static void V6GroupGuardBite(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var driver = Path.Combine(repoRoot, "eng", DriverScript);

            var planted = WriteSyntheticRepo(mergeSetStubs: false, firewallScript: true,
                extraFiles: new[]
                {
                    F("src/BetterUnturnedExperience.Lit/Tidy.cs",
                      "using BetterUnturnedExperience.Plugin;\nnamespace BetterUnturnedExperience.Lit { internal static class T { } }"),
                    F("src/BetterUnturnedExperience.Lit/BetterUnturnedExperience.Lit.csproj", MinimalCsproj("Tidy.cs")),
                    F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                    F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs"))
                });
            try
            {
                var red = RunPowerShellScript(driver, "-Steps Firewall", planted, 300);
                Check(red.ExitCode == 1, "守卫态: 植入禁方向后驱动器应总失败（exit 1），实际 exit=" + red.ExitCode + Environment.NewLine + red.All);
                Check(red.Output.Contains("[Firewall] FAILED"), "守卫态: 防火墙步骤行应为 FAILED: " + red.Output);
                Check(red.Output.Contains("FULLSUITE: FAIL"), "守卫态: 汇总应为 FULLSUITE: FAIL: " + red.Output);
            }
            finally { Directory.Delete(planted, true); }

            var clean = WriteSyntheticRepo(mergeSetStubs: false, firewallScript: true,
                extraFiles: new[]
                {
                    F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                    F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs"))
                });
            try
            {
                var green = RunPowerShellScript(driver, "-Steps Firewall", clean, 300);
                Check(green.ExitCode == 0, "守卫态: 干净合成树驱动器应 PASS（exit 0），实际 exit=" + green.ExitCode + Environment.NewLine + green.All);
                Check(green.Output.Contains("[Firewall] PASS") && green.Output.Contains("FULLSUITE: PASS"),
                    "守卫态: 干净合成树步骤行与汇总应为 PASS: " + green.Output);
            }
            finally { Directory.Delete(clean, true); }
        }

        // ───────────────────────── 组 7：防火墙具名边封闭性（合成树） ─────────────────────────

        /// <summary>02F 两条具名非功能边逐案放行（Transport→Core、NoOpFixture→Plugin，using 与
        /// ProjectReference 双形态），且封闭枚举不开模式：功能指宿主（Lir→Plugin）、Transport 指
        /// ClientUi、NoOpFixture 指 Core 照旧在册。摘具名边（真实仓库回潮）或扩成模式放行的突变必红。</summary>
        private static void V6GroupNamedEdges(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", FirewallScript);

            void RunCase(string name, string expectCount, string[] mustContain, string[] mustNotContain,
                params KeyValuePair<string, string>[] files)
            {
                var root = WriteFixtureTree(files);
                try
                {
                    var result = RunPowerShellScript(script, "-RepoRoot \"" + root + "\"", repoRoot, 300);
                    var footer = Regex.Match(result.Output, @"Project-firewall metrics: (\d+) violation\(s\)");
                    Check(footer.Success, name + ": 缺度量脚注" + Environment.NewLine + result.All);
                    Check(footer.Success && footer.Groups[1].Value == expectCount,
                        name + ": 期望命中数 " + expectCount + "，实际 " + (footer.Success ? footer.Groups[1].Value : "?") + Environment.NewLine + result.Output);
                    foreach (var token in mustContain)
                        Check(result.Output.Contains(token), name + ": 缺命中标记 " + token + Environment.NewLine + result.Output);
                    foreach (var token in mustNotContain)
                        Check(!result.Output.Contains(token), name + ": 不应有命中标记 " + token + Environment.NewLine + result.Output);
                }
                finally { Directory.Delete(root, true); }
            }

            const string CoreCs = "namespace BetterUnturnedExperience.Core { public static class K { } }";
            const string CoreCsproj = "<Project ToolsVersion=\"Current\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"CoreThing.cs\" /></ItemGroup></Project>";
            const string PluginCs = "namespace BetterUnturnedExperience.Plugin { internal static class H { } }";
            const string PluginCsproj = "<Project ToolsVersion=\"Current\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"Host.cs\" /></ItemGroup></Project>";
            const string ClientUiCs = "namespace BetterUnturnedExperience.ClientUi { internal static class P { } }";
            const string ClientUiCsproj = "<Project ToolsVersion=\"Current\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"Panel.cs\" /></ItemGroup></Project>";

            // 具名边①: Transport→Core（适配器实现宿主传输缝端口）——using + ProjectReference 双形态放行。
            RunCase("具名边 Transport->Core 放行", "0", new string[0], new[] { "ref-direction Transport->Core" },
                F("src/BetterUnturnedExperience.Core/CoreThing.cs", CoreCs),
                F("src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj", CoreCsproj),
                F("src/BetterUnturnedExperience.Transport/Adapter.cs",
                    "using BetterUnturnedExperience.Core.Network;\nnamespace BetterUnturnedExperience.Transport { public sealed class A { } }"),
                F("src/BetterUnturnedExperience.Transport/BetterUnturnedExperience.Transport.csproj",
                    "<Project ToolsVersion=\"Current\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"Adapter.cs\" /><ProjectReference Include=\"..\\BetterUnturnedExperience.Core\\BetterUnturnedExperience.Core.csproj\" /></ItemGroup></Project>"));

            // 具名边②: NoOpFixture→Plugin（保留段第二插件经公开登记门消费宿主）——双形态放行。
            RunCase("具名边 NoOpFixture->Plugin 放行", "0", new string[0], new[] { "ref-direction NoOpFixture->Plugin" },
                F("src/BetterUnturnedExperience.Plugin/Host.cs", PluginCs),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", PluginCsproj),
                F("src/BetterUnturnedExperience.NoOpFixture/NoOp.cs",
                    "using BetterUnturnedExperience.Plugin;\nnamespace BetterUnturnedExperience.NoOpFixture { public sealed class N { } }"),
                F("src/BetterUnturnedExperience.NoOpFixture/BetterUnturnedExperience.NoOpFixture.csproj",
                    "<Project ToolsVersion=\"Current\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"NoOp.cs\" /><ProjectReference Include=\"..\\BetterUnturnedExperience.Plugin\\BetterUnturnedExperience.Plugin.csproj\" /></ItemGroup></Project>"));

            // 封闭性负案①: 功能指宿主照旧在册（具名边不外溢到功能工程）。
            RunCase("具名边不外溢: Lir->Plugin 在册", "1", new[] { "VIOLATION R1 ref-direction Lir->Plugin kind=using" }, new string[0],
                F("src/BetterUnturnedExperience.Plugin/Host.cs", PluginCs),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", PluginCsproj),
                F("src/BetterUnturnedExperience.Lir/T.cs",
                    "using BetterUnturnedExperience.Plugin;\nnamespace BetterUnturnedExperience.Lir { internal static class T { } }"),
                F("src/BetterUnturnedExperience.Lir/BetterUnturnedExperience.Lir.csproj", MinimalCsproj("T.cs")));

            // 封闭性负案②: Transport→ClientUi 不在具名边内，照常在册。
            RunCase("具名边不外溢: Transport->ClientUi 在册", "1", new[] { "VIOLATION R1 ref-direction Transport->ClientUi kind=using" }, new string[0],
                F("src/BetterUnturnedExperience.ClientUi/Panel.cs", ClientUiCs),
                F("src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj", ClientUiCsproj),
                F("src/BetterUnturnedExperience.Transport/Adapter.cs",
                    "using BetterUnturnedExperience.ClientUi;\nnamespace BetterUnturnedExperience.Transport { public sealed class A { } }"),
                F("src/BetterUnturnedExperience.Transport/BetterUnturnedExperience.Transport.csproj", MinimalCsproj("Adapter.cs")));

            // 封闭性负案③: NoOpFixture→Core 不在具名边内，照常在册（宿主外工程指 Core 仅 Transport 被点名）。
            RunCase("具名边不外溢: NoOpFixture->Core 在册", "1", new[] { "VIOLATION R1 ref-direction NoOpFixture->Core kind=using" }, new string[0],
                F("src/BetterUnturnedExperience.Core/CoreThing.cs", CoreCs),
                F("src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj", CoreCsproj),
                F("src/BetterUnturnedExperience.NoOpFixture/NoOp.cs",
                    "using BetterUnturnedExperience.Core;\nnamespace BetterUnturnedExperience.NoOpFixture { public sealed class N { } }"),
                F("src/BetterUnturnedExperience.NoOpFixture/BetterUnturnedExperience.NoOpFixture.csproj", MinimalCsproj("NoOp.cs")));
        }

        // ───────────────────────── 组 8：合并集与防火墙白名单同源 ─────────────────────────

        /// <summary>打包脚本合并目标集与防火墙组装根合法边集（$pluginLegalTargets）短名恰等
        /// （除主件 Plugin 自身）：两处封闭集漂移即红。</summary>
        private static void V6GroupMergeSetSameSource(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var repackText = File.ReadAllText(Path.Combine(repoRoot, "eng", RepackScript));
            var firewallText = File.ReadAllText(Path.Combine(repoRoot, "eng", FirewallScript));

            var firewallBlock = Regex.Match(firewallText, @"\$pluginLegalTargets\s*=\s*@\(([^)]*)\)", RegexOptions.Singleline);
            Check(firewallBlock.Success, "防火墙脚本缺 $pluginLegalTargets 封闭白名单块");
            var firewallTargets = firewallBlock.Success
                ? Regex.Matches(firewallBlock.Groups[1].Value, "BetterUnturnedExperience\\.([A-Za-z0-9]+)")
                    .Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList()
                : new List<string>();

            var repackBlock = Regex.Match(repackText, @"\$mergeTargets\s*=\s*@\(([^)]*)\)", RegexOptions.Singleline);
            Check(repackBlock.Success, "打包脚本缺 $mergeTargets 封闭合并集块");
            var repackTargets = repackBlock.Success
                ? Regex.Matches(repackBlock.Groups[1].Value, "'([A-Za-z0-9]+)'")
                    .Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList()
                : new List<string>();

            var expected = MergeTargets.ToList();
            Check(repackTargets.Count == expected.Count && new HashSet<string>(repackTargets).SetEquals(expected),
                "打包合并集应恰为七目标封闭集（主件 Plugin 之外），实际: " + string.Join(",", repackTargets));
            Check(firewallTargets.Count == expected.Count && new HashSet<string>(firewallTargets).SetEquals(expected),
                "防火墙组装根合法边应与合并集同源（V6-T2 封闭白名单），实际: " + string.Join(",", firewallTargets));
        }

        // ───────────────────────── helpers ─────────────────────────

        /// <summary>Mono.Cecil 只读合并产物元数据：返回身份名，输出引用程序集名集与类型全名集。
        /// 不解析依赖（ReaderParameters.ReadSymbols=false 即纯元数据），无程序集加载、无身份冲突。</summary>
        private static string InspectMergedAssembly(string mergedDll, out List<string> refs, out List<string> typeNames)
        {
            refs = new List<string>();
            typeNames = new List<string>();
            var rp = new Mono.Cecil.ReaderParameters();
            rp.ReadSymbols = false;
            using (var asm = Mono.Cecil.AssemblyDefinition.ReadAssembly(mergedDll, rp))
            {
                var name = asm.Name.Name;
                foreach (var reference in asm.MainModule.AssemblyReferences) refs.Add(reference.Name);
                foreach (var type in asm.MainModule.GetTypes()) typeNames.Add(type.FullName);
                return name;
            }
        }

        /// <summary>合成仓库树：sln 桩 + 可选八工程占位输入 + 可选防火墙脚本副本 + 额外文件。
        /// 供合并缺步红（组 2）与守卫咬合（组 6）使用；由调用方删除。</summary>
        private static string WriteSyntheticRepo(bool mergeSetStubs, bool firewallScript, params KeyValuePair<string, string>[] extraFiles)
        {
            var root = Path.Combine(Path.GetTempPath(), "bue-v6-02f-repo-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, SolutionFile), "", new UTF8Encoding(false));
            if (firewallScript)
            {
                var repoRoot = FindRepoRoot();
                Directory.CreateDirectory(Path.Combine(root, "eng"));
                File.Copy(Path.Combine(repoRoot, "eng", FirewallScript), Path.Combine(root, "eng", FirewallScript));
            }
            if (mergeSetStubs)
            {
                foreach (var target in MergeTargets.Concat(new[] { "Plugin" }))
                {
                    var dir = Path.Combine(root, "src", "BetterUnturnedExperience." + target, "bin", "Release");
                    Directory.CreateDirectory(dir);
                    var name = target == "Plugin" ? "BetterUnturnedExperience.dll" : "BetterUnturnedExperience." + target + ".dll";
                    File.WriteAllBytes(Path.Combine(dir, name), new byte[] { 0x4D, 0x5A, 0x90, 0x00 });
                }
            }
            foreach (var file in extraFiles)
            {
                var full = Path.Combine(root, file.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllText(full, file.Value, new UTF8Encoding(false));
            }
            return root;
        }

        private static string Sha256OfFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static readonly object ProcessLock = new object();

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

        private static string WriteFixtureTree(params KeyValuePair<string, string>[] files)
        {
            var root = Path.Combine(Path.GetTempPath(), "bue-v6-02f-fixture-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            foreach (var file in files)
            {
                var full = Path.Combine(root, file.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllText(full, file.Value, new UTF8Encoding(false));
            }
            return root;
        }

        private static KeyValuePair<string, string> F(string relPath, string content)
        {
            return new KeyValuePair<string, string>(relPath, content);
        }

        private static string MinimalCsproj(params string[] compileIncludes)
        {
            var sb = new StringBuilder();
            sb.Append("<Project ToolsVersion=\"Current\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup>");
            foreach (var include in compileIncludes)
                sb.Append("<Compile Include=\"").Append(include).Append("\" />");
            sb.Append("</ItemGroup></Project>");
            return sb.ToString();
        }
    }
}
