using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-01 红测组：工程防火墙（度量态）+ 一条命令跑全套。
    /// 被测外部行为（V6-T2 追加 / V6-T7 交付 A）：
    ///   1. `eng/Verify-ProjectFirewall.ps1` 照相真实仓库：R1 引用方向（跨工程只许经契约 + 组装根合法边，
    ///      含全限定名 token 文本级扫描）、R2 清单等于目录文件集（自有 csproj 清单 + 无 csproj 功能目录
    ///      的 Plugin 平铺清单双形态）、R3 工程目录恰一 csproj、R4 禁平铺外目录源码。已知违规类在册状态
///      随票推进翻转（每条注明作废票）：DEV-V6-02A 落地三个功能工程文件后 R3 熄火、R2 增骨架期
///      平铺未收编过渡态；DEV-V6-02B 收编 Lit 后 Lit 的 R2/R4 过渡态熄火、Lit↔ClientUi 双向与
///      Lit→Plugin 越界拆净（页码范围走契约加性单源）；DEV-V6-02C 收编 Lir 后 Lir 的 R2/R4
///      过渡态熄火、Lir→Plugin 越界拆净（宿主组合注入）；DEV-V6-02E 收编 ListenHost 协调文件进
///      ClientUi 清单、宿主摘 Core/ClientUi 平铺改工程引用、拆 ClientUi→Plugin 工程环与
///      ClientUi.Tests 双引用后：R2/R4 熄火、ClientUi→Plugin 越界熄火（余量=NoOpFixture→Plugin
///      与 Transport→Core 两族，归 02F）；DEV-V6-02F 收编两条具名非功能边后 R1 熄火、防火墙翻
///      守卫态（真实仓库零命中；守卫咬合语义归 02F 红测组）。
    ///   2. `eng/Run-FullSuite.ps1` 仓库根一条命令：-t:Rebuild 重建 + 七套测试 exe + 现有六道门禁 + 防火墙；
    ///      -Plan 预检组合；防火墙守卫态（DEV-V6-02F 翻转）：命中即总失败，真实仓库零命中；
    ///      NoUiTokens(Contracts) 已知基线命中（ContractTypes.cs:Glazier，V2-02 冻结）如实双记不计入总失败。
    /// 非空转证明=突变 M1..M10（见 audit/2026-09-19/DEV-V6-01/）：删规则/删组件/翻容忍语义必令对应组红。
    /// 维护锚：真实仓库照相机组的每条断言都注明会被哪张后续票作废（02B/C/D/E/F 修复即更新该条）。
    /// </summary>
    internal static class DevV6FirewallFullsuiteTests
    {
        private const string FirewallScript = "Verify-ProjectFirewall.ps1";
        private const string DriverScript = "Run-FullSuite.ps1";
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

                Group("防火墙真实仓库照相机（已知违规类必须在册）", () => V6GroupFirewallRealRepo(Check));
                Group("防火墙规则精确性（合成夹具树逐规则）", () => V6GroupFirewallFixtures(Check));
                Group("一条命令驱动器组合完整（-Plan 预检）", () => V6GroupDriverPlan(Check));
                Group("度量态与已知基线容忍语义（真实小步运行）", () => V6GroupDriverSemantics(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：真实仓库照相机 ─────────────────────────

        /// <summary>防火墙对真实仓库照相：退出 1（今天确有违规）、度量脚注可解析、在册规则开火状态、
        /// 票面点名的已知违规类逐条在册/熄火。每条断言注明作废票：对应票修复该类后须把断言翻转。</summary>
        private static void V6GroupFirewallRealRepo(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", FirewallScript);
            Check(File.Exists(script), "防火墙脚本缺失: eng/" + FirewallScript + "（缺防火墙=红）");
            var result = RunPowerShellScript(script, "-RepoRoot \"" + repoRoot + "\"", repoRoot, 300);

            // 作废票 02F（守卫翻转）：真实仓库已零命中（02B..02E 拆净 + 02F 收编两条具名非功能边），
            // 防火墙照相机从「退出 1（度量态确有违规）」翻转为「退出 0（守卫态零命中）」。
            Check(result.ExitCode == 0,
                "防火墙对真实仓库应退出 0（守卫态零命中；余 4 命中已由 02F 收编为具名边），实际 exit=" + result.ExitCode + Environment.NewLine + result.Output);
            var footer = Regex.Match(result.Output, @"Project-firewall metrics: (\d+) violation\(s\) rules-fired=(\S+)");
            Check(footer.Success, "防火墙输出缺度量脚注「Project-firewall metrics: N violation(s) rules-fired=…」");
            var rulesFired = footer.Success ? footer.Groups[2].Value : "";
            // R3 熄火是 DEV-V6-02A 的落地锚（作废票：02A——三个功能工程文件在位后 project-file-absent 不再出现）。
            // R2/R4 熄火是 DEV-V6-02E 的落地锚（作废票：02E——ListenHost 协调文件收编进 ClientUi 清单、
            // 宿主摘 Core/ClientUi 平铺改工程引用后，R2 清单漂移与 R4 foreign-embed 均不再出现；
            // R1 余量（NoOpFixture→Plugin 与 Transport→Core 两族）已随 DEV-V6-02F 收编为具名非功能边熄火）。
            // 作废票 02F（守卫翻转）：rules-fired 从「恰 R1」翻转为「-」（无任何规则开火）。
            Check(rulesFired == "-",
                "守卫态: R1 应随 02F 具名边收编熄火、R2/R4 已随 02E 熄火、R3 已随 02A 熄火（rules-fired=-），实际 rules-fired=" + rulesFired);

            // R1 已知越界（作废票：DEV-V6-02C/D/E 修复后逐条更新为「不在册」）。
            // Lit→ClientUi / Lit→Plugin / ClientUi→Lit 已随 DEV-V6-02B 拆净（照相机翻转，作废票：
            // 02B）——不在册；ClientUi→Lit 摘除走 02B 票面 Scope 的「否则」分支：页码范围经组装根
            // 绑定单源（宿主转达），不进契约。
            // Lir→Plugin 已随 DEV-V6-02C 拆净（照相机翻转，作废票：02C）——不在册：日志口/设置根/
            // 无画面门禁/Steam 身份按 T2 硬项拆法改宿主组合注入 BindHostComposition。
            Check(!result.Output.Contains("ref-direction Lit->ClientUi"),
                "已知违规熄火失败: Lit→ClientUi 应已随 02B 拆净");
            Check(!result.Output.Contains("ref-direction Lit->Plugin"),
                "已知违规熄火失败: Lit→Plugin 应已随 02B 拆净");
            Check(!result.Output.Contains("ref-direction ClientUi->Lit"),
                "已知违规熄火失败: ClientUi→Lit 应已随 02B 页码范围宿主绑定单源摘除");
            Check(!result.Output.Contains("ref-direction Lir->Plugin"),
                "已知违规熄火失败: Lir→Plugin 应已随 02C 组合注入拆净");
            Check(!result.Output.Contains("ref-direction Lht->Plugin"),
                "已知违规熄火失败: Lht→Plugin 应已随 02D 日志口组合注入拆净（作废票 02D）");
            // ClientUi→Plugin 已随 DEV-V6-02E 拆净（照相机翻转，作废票：02E）——不在册：工程环
            // 双边（csproj 工程引用 + ListenHost 协调文件的 using）同步摘除，协调文件收编进
            // ClientUi 工程清单，宿主日志口改 BindHostComposition 组合注入。
            Check(!result.Output.Contains("ref-direction ClientUi->Plugin"),
                "已知违规熄火失败: ClientUi→Plugin 应已随 02E 工程环拆净（作废票 02E）");

            // NoOpFixture→Plugin 与 Transport→Core 已随 DEV-V6-02F 收编为具名非功能边（照相机翻转，
            // 作废票：02F）——不在册：前者=保留段第二插件经宿主公开登记门消费宿主（仅公开 API），
            // 后者=传输适配器实现宿主传输缝端口；封闭枚举逐条点名，非模式放行（守卫语义见 02F 红测组）。
            Check(!result.Output.Contains("ref-direction NoOpFixture->Plugin"),
                "已知违规熄火失败: NoOpFixture→Plugin 应已随 02F 收编为具名边（作废票 02F）");
            Check(!result.Output.Contains("ref-direction Transport->Core"),
                "已知违规熄火失败: Transport→Core 应已随 02F 收编为具名边（作废票 02F）");

            // R3 已随 DEV-V6-02A 落地三个功能工程文件而熄火（照相机翻转，作废票：02A）——不在册。
            Check(!result.Output.Contains("project-file-absent: BetterUnturnedExperience.Lit"),
                "已知违规熄火失败: Lit 不应再被照出缺工程文件（02A 已落地）");
            Check(!result.Output.Contains("project-file-absent: BetterUnturnedExperience.Lir"),
                "已知违规熄火失败: Lir 不应再被照出缺工程文件（02A 已落地）");
            Check(!result.Output.Contains("project-file-absent: BetterUnturnedExperience.Lht"),
                "已知违规熄火失败: Lht 不应再被照出缺工程文件（02A 已落地）");

            // R2 骨架期平铺未收编（DEV-V6-02A 过渡态，作废票：02C/D 收编后逐目录翻不在册）：
            // 旧源码仍由宿主平铺代管（R4 在册），新工程清单只含公开组装类型 → 「在目录、不在清单」按目录在册。
            // Lit 目录已随 DEV-V6-02B 收编（照相机翻转，作废票：02B）——不在册。
            // Lir 目录已随 DEV-V6-02C 收编（照相机翻转，作废票：02C）——不在册。
            Check(!result.Output.Contains("manifest-missing-file: BetterUnturnedExperience.Lit/"),
                "已知违规熄火失败: Lit 清单应已随 02B 收编恰等目录文件集");
            Check(!result.Output.Contains("manifest-missing-file: BetterUnturnedExperience.Lir/"),
                "已知违规熄火失败: Lir 清单应已随 02C 收编恰等目录文件集");
            Check(!result.Output.Contains("manifest-missing-file: BetterUnturnedExperience.Lht/"),
                "已知违规熄火失败: Lht 清单应已随 02D 收编恰等目录文件集（作废票 02D）");

            // R2 清单漂移已随 DEV-V6-02E 收编熄火（照相机翻转，作废票：02E）——
            // R2 研究点名的 ListenHostProjectionReconciler 已入 ClientUi 工程清单。
            Check(!result.Output.Contains("manifest-missing-file: BetterUnturnedExperience.ClientUi/"),
                "已知违规熄火失败: ClientUi 清单应已随 02E 收编恰等目录文件集（作废票 02E）");

            // R4 Compile Link 平铺已随 DEV-V6-02E 摘净（照相机翻转，作废票：02E）——宿主对
            // 已独立工程的源码平铺全部退役：Lit 已随 02B 摘除、Lir 已随 02C 摘除、Lht 已随 02D
            // 摘除、Contracts 已随 02B 契约单编译摘除（Lit 缝交接契约类型对象的类型同一性硬前提）、
            // Core/ClientUi 已随 02E 摘除改工程引用（组装根合法边）。熄火以显式负断言在册。
            Check(!result.Output.Contains("foreign-embed: BetterUnturnedExperience.Plugin embeds BetterUnturnedExperience.Core"),
                "已知违规熄火失败: Plugin 平铺 Core 应已随 02E 摘除（作废票 02E）");
            Check(!result.Output.Contains("foreign-embed: BetterUnturnedExperience.Plugin embeds BetterUnturnedExperience.ClientUi"),
                "已知违规熄火失败: Plugin 平铺 ClientUi 应已随 02E 摘除（作废票 02E）");
            Check(!result.Output.Contains("foreign-embed: BetterUnturnedExperience.Plugin embeds BetterUnturnedExperience.Lit"),
                "已知违规熄火失败: Plugin 平铺 Lit 应已随 02B 摘除");
            Check(!result.Output.Contains("foreign-embed: BetterUnturnedExperience.Plugin embeds BetterUnturnedExperience.Lht"),
                "已知违规熄火失败: Plugin 平铺 Lht 应已随 02D 摘除（作废票 02D）");
            Check(!result.Output.Contains("foreign-embed: BetterUnturnedExperience.Plugin embeds BetterUnturnedExperience.Lir"),
                "已知违规熄火失败: Plugin 平铺 Lir 应已随 02C 摘除");
            Check(!result.Output.Contains("foreign-embed: BetterUnturnedExperience.Plugin embeds BetterUnturnedExperience.Contracts"),
                "已知违规熄火失败: Plugin 平铺 Contracts 应已随 02B 契约单编译摘除");
        }

        // ───────────────────────── 组 3：一条命令驱动器组合 ─────────────────────────

        /// <summary>-Plan 预检：驱动器存在、组合完整（重建短横线开关 + 七套 exe + 六道门禁七次调用 +
        /// 防火墙 + 已知基线声明）、无 MISSING。删组件/换开关的突变必令本组红。</summary>
        private static void V6GroupDriverPlan(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var driver = Path.Combine(repoRoot, "eng", DriverScript);
            Check(File.Exists(driver), "一条命令驱动器缺失: eng/" + DriverScript + "（缺命令=红）");
            var result = RunPowerShellScript(driver, "-Plan", repoRoot, 120);
            Check(result.ExitCode == 0, "-Plan 预检应退出 0（全部组件在位），实际 exit=" + result.ExitCode + Environment.NewLine + result.All);
            Check(!result.Output.Contains("PLAN MISSING"), "-Plan 不得报告组件缺失: " + result.Output);

            Check(result.Output.Contains("-t:Rebuild"), "-Plan 必须声明重建用短横线开关 -t:Rebuild");
            Check(result.Output.Contains("dotnet msbuild") && result.Output.Contains("BetterUnturnedExperience.sln")
                && result.Output.Contains("-p:Configuration=Release"), "-Plan 必须声明对解决方案的 Release 重建");

            foreach (var suite in new[] { "Contracts", "Network", "Placement", "Settings", "ClientUi", "Plugin", "Release" })
                Check(result.Output.Contains("BetterUnturnedExperience." + suite + ".Tests.exe"),
                    "-Plan 缺测试套件可执行文件: " + suite + ".Tests.exe");

            // DEV-V6-06：契约文档精确度门禁（ContractDocs）随本票并入组合——门禁缺席=红。
            foreach (var gate in new[] { "DeveloperHandbook", "ContractDocs", "NoUiTokens:Contracts", "NoUiTokens:Core",
                "RefreshModelDeduped", "SpecV4R9Ingested", "TestFixturesTracked", "TestRunnerHostDlls" })
                Check(result.Output.Contains(gate), "-Plan 缺门禁步骤: " + gate);

            Check(result.Output.Contains(FirewallScript), "-Plan 缺防火墙步骤");
            Check(result.Output.Contains("ContractTypes.cs:Glazier"),
                "-Plan 必须声明 NoUiTokens(Contracts) 已知基线命中 ContractTypes.cs:Glazier（如实双记的账面）");
        }

        // ───────────────────────── 组 2：合成夹具树逐规则 ─────────────────────────

        /// <summary>每个规则用一棵最小合成树单独锁行为：干净树零命中；越界/漂移/缺工程/平铺各正命中。
        /// 突变删任一规则 → 对应断言红。</summary>
        private static void V6GroupFirewallFixtures(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", FirewallScript);
            Check(File.Exists(script), "防火墙脚本缺失: eng/" + FirewallScript);

            void RunCase(string name, string expectFooterCount, string[] mustContain, string[] mustNotContain,
                params KeyValuePair<string, string>[] files)
            {
                var root = WriteFixtureTree(files);
                try
                {
                    var result = RunPowerShellScript(script, "-RepoRoot \"" + root + "\"", repoRoot, 300);
                    var footer = Regex.Match(result.Output, @"Project-firewall metrics: (\d+) violation\(s\)");
                    Check(footer.Success, name + ": 缺度量脚注" + Environment.NewLine + result.All);
                    Check(footer.Success && footer.Groups[1].Value == expectFooterCount,
                        name + ": 期望命中数 " + expectFooterCount + "，实际 " + (footer.Success ? footer.Groups[1].Value : "?") + Environment.NewLine + result.Output);
                    foreach (var token in mustContain)
                        Check(result.Output.Contains(token), name + ": 缺命中标记 " + token + Environment.NewLine + result.Output);
                    foreach (var token in mustNotContain)
                        Check(!result.Output.Contains(token), name + ": 不应有命中标记 " + token + Environment.NewLine + result.Output);
                }
                finally { Directory.Delete(root, true); }
            }

            const string ContractsCs = "namespace BetterUnturnedExperience.Contracts { public static class C { } }";
            const string CoreCs = "using BetterUnturnedExperience.Contracts;\nnamespace BetterUnturnedExperience.Core { public static class K { } }";

            // 干净树：Contracts+Core 合法边（Core→Contracts），清单等于目录 → 0 命中。
            RunCase("干净树零命中", "0", new string[0], new[] { "VIOLATION" },
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Core/CoreThing.cs", CoreCs),
                F("src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj", MinimalCsproj("CoreThing.cs")));

            // R1 全限定名越界：Lit→ClientUi（含注释/字符串的文本级命中同样在册）。
            RunCase("R1 全限定名越界 Lit->ClientUi", "1", new[] { "VIOLATION R1 ref-direction Lit->ClientUi kind=fqn" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.ClientUi/Panel.cs", "namespace BetterUnturnedExperience.ClientUi { internal static class P { } }"),
                F("src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj", MinimalCsproj("Panel.cs")),
                F("src/BetterUnturnedExperience.Lit/Tidy.cs", "namespace BetterUnturnedExperience.Lit { internal static class T { internal static void Go() { BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.OnTidyPagesCommitted(2, 6); } } }"),
                F("src/BetterUnturnedExperience.Lit/BetterUnturnedExperience.Lit.csproj", MinimalCsproj("Tidy.cs")));

            // R1 using 指令越界：ClientUi→Lit。
            RunCase("R1 using 越界 ClientUi->Lit", "1", new[] { "VIOLATION R1 ref-direction ClientUi->Lit kind=using" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.ClientUi/Panel.cs", "using BetterUnturnedExperience.Lit;\nnamespace BetterUnturnedExperience.ClientUi { internal static class P { } }"),
                F("src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj", MinimalCsproj("Panel.cs")),
                F("src/BetterUnturnedExperience.Lit/Tidy.cs", "namespace BetterUnturnedExperience.Lit { internal static class T { } }"),
                F("src/BetterUnturnedExperience.Lit/BetterUnturnedExperience.Lit.csproj", MinimalCsproj("Tidy.cs")));

            // R1 合法边不报：组装根 Plugin→Lit 与 经契约 Core→Contracts。
            RunCase("R1 合法边放行", "0", new string[0], new[] { "VIOLATION R1" },
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Core/CoreThing.cs", CoreCs),
                F("src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj", MinimalCsproj("CoreThing.cs")),
                F("src/BetterUnturnedExperience.Lit/Tidy.cs", "namespace BetterUnturnedExperience.Lit { internal static class T { } }"),
                F("src/BetterUnturnedExperience.Lit/BetterUnturnedExperience.Lit.csproj", MinimalCsproj("Tidy.cs")),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "using BetterUnturnedExperience.Lit;\nnamespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs")));

            // R2 清单漂移：目录多出的文件不在清单；清单幽灵文件不在目录。
            RunCase("R2 清单缺目录文件", "1", new[] { "VIOLATION R2 manifest-missing-file: BetterUnturnedExperience.Core/Extra.cs" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Core/CoreThing.cs", CoreCs),
                F("src/BetterUnturnedExperience.Core/Extra.cs", "namespace BetterUnturnedExperience.Core { internal static class E { } }"),
                F("src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj", MinimalCsproj("CoreThing.cs")));
            RunCase("R2 清单幽灵条目", "1", new[] { "VIOLATION R2 manifest-stale-entry: BetterUnturnedExperience.Core/Ghost.cs" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Core/CoreThing.cs", CoreCs),
                F("src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj", MinimalCsproj("CoreThing.cs", "Ghost.cs")));

            // R2 通配 Include（清单必须显式等于目录文件集）。
            RunCase("R2 通配符 Include 禁止", "1", new[] { "VIOLATION R2 wildcard-include: BetterUnturnedExperience.Core" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Core/CoreThing.cs", CoreCs),
                F("src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj", MinimalCsproj("**\\*.cs")));

            // R2b+R3+R4 组合：无 csproj 的 Lit 由 Plugin 平铺清单代管——平铺缺一文件即漂移，平铺本身即 R4。
            RunCase("R2b 平铺清单漂移 + R3 + R4", "3",
                new[] { "VIOLATION R2 embed-manifest-missing-file: BetterUnturnedExperience.Lit/Extra.cs",
                    "VIOLATION R3 project-file-absent: BetterUnturnedExperience.Lit",
                    "VIOLATION R4 foreign-embed: BetterUnturnedExperience.Plugin embeds BetterUnturnedExperience.Lit" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Lit/Tidy.cs", "namespace BetterUnturnedExperience.Lit { internal static class T { } }"),
                F("src/BetterUnturnedExperience.Lit/Extra.cs", "namespace BetterUnturnedExperience.Lit { internal static class E { } }"),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj",
                    MinimalCsproj("Host.cs", "..\\BetterUnturnedExperience.Lit\\Tidy.cs")));

            // R4 独立：宿主平铺有自有 csproj 的工程（Core）照出 foreign-embed；R2b 不适用（Core 自有清单）。
            RunCase("R4 平铺自有工程目录", "1", new[] { "VIOLATION R4 foreign-embed: BetterUnturnedExperience.Plugin embeds BetterUnturnedExperience.Core files=1" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Core/CoreThing.cs", CoreCs),
                F("src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj", MinimalCsproj("CoreThing.cs")),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj",
                    MinimalCsproj("Host.cs", "..\\BetterUnturnedExperience.Core\\CoreThing.cs")));

            // R3 独立：工程目录缺 csproj，且无 Plugin 平铺可代管 → R3 + R2 embed-manifest-absent 各一。
            RunCase("R3 缺工程文件", "2",
                new[] { "VIOLATION R3 project-file-absent: BetterUnturnedExperience.Lit",
                    "VIOLATION R2 embed-manifest-absent: BetterUnturnedExperience.Lit" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Lit/Tidy.cs", "namespace BetterUnturnedExperience.Lit { internal static class T { } }"));

            // R1 ProjectReference 边：ClientUi→Plugin 工程引用越界。
            RunCase("R1 ProjectReference 越界", "1", new[] { "VIOLATION R1 ref-direction ClientUi->Plugin kind=project-reference" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs")),
                F("src/BetterUnturnedExperience.ClientUi/Panel.cs", "namespace BetterUnturnedExperience.ClientUi { internal static class P { } }"),
                F("src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj",
                    "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"Panel.cs\" /><ProjectReference Include=\"..\\BetterUnturnedExperience.Plugin\\BetterUnturnedExperience.Plugin.csproj\" /></ItemGroup></Project>"));

            // R1 组装根合法边=封闭白名单（官方功能/ClientUi/Core/契约/Bii）：Plugin→白名单外工程必须命中，
            // 不得因「来自组装根」而通配放行（R1 审查缺口修复的回归锚）。
            RunCase("R1 组装根白名单外引用", "1", new[] { "VIOLATION R1 ref-direction Plugin->Release kind=using" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Release/Qual.cs", "namespace BetterUnturnedExperience.Release { internal static class Q { } }"),
                F("src/BetterUnturnedExperience.Release/BetterUnturnedExperience.Release.csproj", MinimalCsproj("Qual.cs")),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "using BetterUnturnedExperience.Release;\nnamespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs")));

            // R1 ProjectReference 属性换序（Condition 在 Include 前）同样必须被逮住（R1 审查缺口修复的回归锚）。
            RunCase("R1 ProjectReference 属性换序", "1", new[] { "VIOLATION R1 ref-direction ClientUi->Plugin kind=project-reference" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs")),
                F("src/BetterUnturnedExperience.ClientUi/Panel.cs", "namespace BetterUnturnedExperience.ClientUi { internal static class P { } }"),
                F("src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj",
                    "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"Panel.cs\" /><ProjectReference Condition=\"'1'=='1'\" Include=\"..\\BetterUnturnedExperience.Plugin\\BetterUnturnedExperience.Plugin.csproj\" /></ItemGroup></Project>"));

            // R1 ProjectReference 单引号 Include 同样必须被逮住（R2 审查缺口修复的回归锚；突变 M11 证红）。
            RunCase("R1 ProjectReference 单引号", "1", new[] { "VIOLATION R1 ref-direction ClientUi->Plugin kind=project-reference" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs")),
                F("src/BetterUnturnedExperience.ClientUi/Panel.cs", "namespace BetterUnturnedExperience.ClientUi { internal static class P { } }"),
                F("src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj",
                    "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"Panel.cs\" /><ProjectReference Condition=\"'$X'=='Y'\" Include='..\\BetterUnturnedExperience.Plugin\\BetterUnturnedExperience.Plugin.csproj' /></ItemGroup></Project>"));

            // R1 豁免：InternalsVisibleTo 指向 *.Tests 的友元声明字符串不计越界（DEV-V6-02B 点名的
            // 门禁精度缺口：功能工程对测试工程的既有豁免是 02A 组 3 的既定形态，字符串文本里的
            // 程序集名不是引用方向；非 .Tests 目标不豁免）。真分工程迁移（02B/C/D）需要它，
            // 缺豁免时合法 IVT 声明会被照成 Lit->Plugin 越界（本例 1≠0 即红）。
            RunCase("R1 豁免 IVT-to-Tests 字符串", "0", new string[0], new[] { "ref-direction Lit->Plugin" },
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Lit/Tidy.cs",
                    "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"BetterUnturnedExperience.Plugin.Tests\")]\nnamespace BetterUnturnedExperience.Lit { internal static class T { } }"),
                F("src/BetterUnturnedExperience.Lit/BetterUnturnedExperience.Lit.csproj", MinimalCsproj("Tidy.cs")),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs")));

            // R1 豁免边界：IVT 指向非 *.Tests 程序集（跨功能友元）不豁免，照常在册（02A 组 3 禁令同源）。
            RunCase("R1 IVT 非 Tests 目标不豁免", "1", new[] { "VIOLATION R1 ref-direction Lit->Plugin kind=fqn" }, new string[0],
                F("src/BetterUnturnedExperience.Contracts/ContractTypes.cs", ContractsCs),
                F("src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj", MinimalCsproj("ContractTypes.cs")),
                F("src/BetterUnturnedExperience.Lit/Tidy.cs",
                    "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"BetterUnturnedExperience.Plugin\")]\nnamespace BetterUnturnedExperience.Lit { internal static class T { } }"),
                F("src/BetterUnturnedExperience.Lit/BetterUnturnedExperience.Lit.csproj", MinimalCsproj("Tidy.cs")),
                F("src/BetterUnturnedExperience.Plugin/Host.cs", "namespace BetterUnturnedExperience.Plugin { internal static class H { } }"),
                F("src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj", MinimalCsproj("Host.cs")));
        }

        // ───────────────────────── 组 4：度量态与基线容忍语义 ─────────────────────────

        /// <summary>真实小步运行（不触发重建/测试套件，杜绝套件递归自嵌套）：
        /// a) -Steps Firewall：守卫态零命中 → 防火墙步骤 PASS 且总判 PASS（作废票 02F：原「度量
        ///    照相不计入总失败」语义随守卫翻转退役，守卫咬合语义由 DEV-V6-02F 红测组合成树案锁定）；
        /// b) -Steps Gates -GateFilter NoUiTokens*：Contracts 侧已知基线命中如实双记（步骤行+汇总行）不计入总失败，
        ///    Core 侧正常 PASS。突变（把容忍语义翻成计失败）必令本组红。</summary>
        private static void V6GroupDriverSemantics(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var driver = Path.Combine(repoRoot, "eng", DriverScript);
            Check(File.Exists(driver), "一条命令驱动器缺失: eng/" + DriverScript);

            var firewallRun = RunPowerShellScript(driver, "-Steps Firewall", repoRoot, 300);
            Check(firewallRun.ExitCode == 0,
                "守卫语义: 真实仓库零命中时驱动器须 PASS，实际 exit=" + firewallRun.ExitCode + Environment.NewLine + firewallRun.All);
            Check(firewallRun.Output.Contains("[Firewall] PASS") && firewallRun.Output.Contains("FULLSUITE: PASS"),
                "守卫语义: 防火墙步骤行与汇总须 PASS（守卫态零命中）" + Environment.NewLine + firewallRun.Output);
            Check(!firewallRun.Output.Contains("METRICS") && !firewallRun.Output.Contains("metrics="),
                "守卫语义: 度量态痕迹（METRICS 状态行/metrics 桶）应随 02F 退役" + Environment.NewLine + firewallRun.Output);

            var gatesRun = RunPowerShellScript(driver, "-Steps Gates -GateFilter NoUiTokens*", repoRoot, 300);
            Check(gatesRun.ExitCode == 0,
                "基线语义: 已知基线命中须容忍（不计入总失败），实际 exit=" + gatesRun.ExitCode + Environment.NewLine + gatesRun.All);
            Check(gatesRun.Output.Contains("[Gates:NoUiTokens:Contracts] KNOWN-BASELINE"),
                "基线语义: Contracts 侧须记 KNOWN-BASELINE 步骤行" + Environment.NewLine + gatesRun.Output);
            Check(gatesRun.Output.Contains("ContractTypes.cs:Glazier"),
                "基线语义: 步骤行/汇总须点名基线命中 ContractTypes.cs:Glazier（如实双记）" + Environment.NewLine + gatesRun.Output);
            Check(gatesRun.Output.Contains("[Gates:NoUiTokens:Core] PASS"),
                "基线语义: Core 侧无界面词扫描须 PASS" + Environment.NewLine + gatesRun.Output);
            Check(gatesRun.Output.Contains("known-baseline=1") && gatesRun.Output.Contains("FULLSUITE: PASS"),
                "基线语义: 汇总须双记 known-baseline=1 且总判 PASS" + Environment.NewLine + gatesRun.Output);
        }

        // ───────────────────────── helpers ─────────────────────────

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

        /// <summary>孵化 powershell.exe 跑 -File 脚本；超时杀树；输出按 UTF-8 收（脚本首行自设 UTF-8 控制台编码）。</summary>
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

        /// <summary>在临时目录下落一棵合成仓库树（相对路径→内容），返回根路径；由调用方负责删除。</summary>
        private static string WriteFixtureTree(params KeyValuePair<string, string>[] files)
        {
            var root = Path.Combine(Path.GetTempPath(), "bue-v6-firewall-fixture-" + Guid.NewGuid().ToString("N").Substring(0, 8));
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
