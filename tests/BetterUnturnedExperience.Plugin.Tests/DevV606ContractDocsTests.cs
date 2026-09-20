using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lht;
using BetterUnturnedExperience.Lir;
using BetterUnturnedExperience.Lit;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-06 红测组：契约文档精确度门禁（V6-T6 五项裁定的机器化）。
    /// 被测外部行为：
    ///   1. `eng/Verify-ContractDocs.ps1` 对真实仓库退出 0——手册隔离改指向不再复述、SDK 附录 B.2
    ///      含宿主未就绪码 `BUE-HOST-001`、SDK 有固定节「未实装（本版本）」且登记工件不在其中、
    ///      §4 出现登记工件类型名与公开摘要函数名、摘要实现全员单源（玩家合并集内恰一份定义 + 受理路径调用）；
    ///   2. 门禁**逐规则可红**（票面验收「能红掉隔离复述、缺宿主码、把登记工件标成未实装、digest 双算法」）：
    ///      合成夹具树单点突变各咬出一条具名违规，干净树零命中；
    ///   3. 官方先行消费：官方功能（LIT/LIR/LHT/BII/网络模块）的登记工件摘要=公开函数对同一载荷的输出
    ///      （契约加性面必须有真实消费者，不是只写文档）。
    /// 非空转证明=突变 M3..M6（见 audit/2026-09-19/DEV-V6-06/mutations/）。
    /// </summary>
    internal static class DevV606ContractDocsTests
    {
        private const string GateScript = "Verify-ContractDocs.ps1";
        private const string SolutionFile = "BetterUnturnedExperience.sln";
        private static readonly object ProcessLock = new object();

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
                    catch (Exception error) { reds.Add(name + " 组异常: " + error.GetType().Name + ": " + error.Message); }
                }

                Group("契约文档门禁照真实仓库（零命中）", () => V606GroupRealRepo(Check));
                Group("门禁逐规则可红（合成夹具单点突变）", () => V606GroupGateFixtures(Check));
                Group("官方先行消费（官方登记工件摘要=公开函数输出）", () => V606GroupOfficialFirstConsumption(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：真实仓库 ─────────────────────────

        private static void V606GroupRealRepo(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", GateScript);
            Check(File.Exists(script), "契约文档门禁脚本缺失: eng/" + GateScript + "（缺门禁=红）");
            if (!File.Exists(script)) return;
            var result = RunPowerShellScript(script, "-RepoRoot \"" + repoRoot + "\"", repoRoot, 300);
            Check(result.ExitCode == 0,
                "门禁对真实仓库应退出 0（文档精确度全判据在位），实际 exit=" + result.ExitCode + Environment.NewLine + result.All);
            Check(result.Output.Contains("Contract-docs gate PASS"),
                "门禁必须给出 PASS 判词（可解析的机器面）" + Environment.NewLine + result.All);
            foreach (var rule in new[]
            {
                "CD-ISO-POINTER", "CD-ISO-COOP", "CD-ISO-RESTATE", "CD-PHASE-POINTER",
                "CD-UNWIRED-HANDBOOK-POINTER", "CD-UNWIRED-HANDBOOK-LIST",
                "CD-HOSTCODE", "CD-UNWIRED-SECTION", "CD-UNWIRED-CAPABILITY", "CD-UNWIRED-EVENT",
                "CD-UNWIRED-WINDOW", "CD-UNWIRED-ARTIFACT", "CD-UNWIRED-BOUNDARY", "CD-UNWIRED-OFFSCOPE",
                "CD-UNWIRED-SURFACE",
                "CD-ARTIFACT-NAME", "CD-DIGEST-FUNCTION", "CD-DIGEST-SINGLE-DEF", "CD-DIGEST-CALLSITE", "CD-DIGEST-LEGACY"
            })
                Check(result.Output.Contains(rule), "门禁规则清单缺项（规则未装载=红）: " + rule + Environment.NewLine + result.All);
        }

        // ───────────────────────── 组 2：合成夹具逐规则 ─────────────────────────

        /// <summary>每个规则用一棵最小合成树单点突变：干净树零命中；隔离复述 / 缺宿主码 /
        /// 登记工件进未实装 / 摘要双算法（旧私有实现或受理路径不调用）各咬出对应具名违规。
        /// 突变删任一规则 → 对应夹具断言红。</summary>
        private static void V606GroupGateFixtures(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var script = Path.Combine(repoRoot, "eng", GateScript);
            Check(File.Exists(script), "契约文档门禁脚本缺失: eng/" + GateScript);
            if (!File.Exists(script)) return;

            void RunCase(string name, bool expectPass, string[] mustContain, params KeyValuePair<string, string>[] overrides)
            {
                var files = CleanFixtureFiles();
                foreach (var pair in overrides) files[pair.Key] = pair.Value;
                var list = new List<KeyValuePair<string, string>>();
                foreach (var pair in files) list.Add(pair);
                var root = WriteFixtureTree(list.ToArray());
                try
                {
                    var result = RunPowerShellScript(script, "-RepoRoot \"" + root + "\"", repoRoot, 300);
                    if (expectPass)
                    {
                        Check(result.ExitCode == 0, name + ": 期望 PASS，实际 exit=" + result.ExitCode + Environment.NewLine + result.All);
                        return;
                    }
                    Check(result.ExitCode != 0, name + ": 期望命中，实际 exit=" + result.ExitCode + Environment.NewLine + result.All);
                    foreach (var token in mustContain)
                        Check(result.Output.Contains(token), name + ": 缺具名违规 " + token + Environment.NewLine + result.All);
                }
                finally { Directory.Delete(root, true); }
            }

            // 干净树：全部判据在位 → 零命中（门禁的「不空转」下界）。
            RunCase("干净树零命中", true, new string[0]);

            // 隔离复述（票面验收第 1 项）：手册把隔离范围又复述一遍即红。
            RunCase("隔离复述", false, new[] { "CD-ISO-RESTATE" },
                F("docs/developer/BetterUnturnedExperience-Developer-Handbook.md",
                    CleanHandbook + "顺手再复述一句：单个功能抛异常只隔离该功能。" + "\n"));

            // 缺宿主未就绪码（票面验收第 2 项）：B.2 平台自检码表没有 BUE-HOST-001 即红。
            RunCase("缺宿主未就绪码", false, new[] { "CD-HOSTCODE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace("| `BUE-HOST-001` | 注册时宿主尚未就绪（HostUnavailable） | 推迟到 BUE 前置就绪后再注册 |" + "\n", string.Empty)));

            // 登记工件被标成未实装（票面验收第 3 项）：A.8 里出现 FeatureDefinitionArtifact 即红。
            RunCase("登记工件进未实装节", false, new[] { "CD-UNWIRED-ARTIFACT" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace(SecondItem,
                        SecondItem + "\n"
                        + "- FeatureDefinitionArtifact 未实装。")));

            // digest 双算法 A（票面验收第 4 项）：玩家合并集里又长出一份私有载荷摘要实现即红。
            RunCase("摘要双算法（旧私有实现）", false, new[] { "CD-DIGEST-LEGACY" },
                F("src/BetterUnturnedExperience.Lit/LitTidyRegistration.cs",
                    "namespace X { internal static class Y { private static Digest256 ComputePayloadDigest(byte[] payload) { return default(Digest256); } } }"));

            // digest 双算法 B：受理路径不调用公开函数（自带一套）即红。
            RunCase("摘要双算法（受理路径不调用）", false, new[] { "CD-DIGEST-CALLSITE" },
                F("src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs",
                    "namespace X { internal static class Y { } }"));

            // 隔离措辞改指（票面 Scope 第 1 项）：手册删掉指向句、不复述也不算达标。
            RunCase("手册缺指向句", false, new[] { "CD-ISO-POINTER" },
                F("docs/developer/BetterUnturnedExperience-Developer-Handbook.md", CleanHandbook.Replace("隔离范围以 SDK 附录 A.3 为准。", string.Empty)));

            // 未实装清单只许 SDK 列（T6 Q4）：手册删掉指向 SDK 未实装节的句子即红。
            RunCase("手册缺未实装节指向", false, new[] { "CD-UNWIRED-HANDBOOK-POINTER" },
                F("docs/developer/BetterUnturnedExperience-Developer-Handbook.md",
                    CleanHandbook.Replace("见 SDK 附录「未实装（本版本）」（A.8）。", "。" )));

            // 「只指向」的另一半：手册把清单内容抄了一份同样红（两式写法各一案）。
            RunCase("手册复述未实装项（能力协商）", false, new[] { "CD-UNWIRED-HANDBOOK-LIST" },
                F("docs/developer/BetterUnturnedExperience-Developer-Handbook.md",
                    CleanHandbook + "本版本未实装的项例如能力协商协议。" + "\n"));
            RunCase("手册复述未实装项（状态变化事件）", false, new[] { "CD-UNWIRED-HANDBOOK-LIST" },
                F("docs/developer/BetterUnturnedExperience-Developer-Handbook.md",
                    CleanHandbook + "注意状态变化事件本版本还没接线。" + "\n"));
            RunCase("手册复述未实装项（三段名）", false, new[] { "CD-UNWIRED-HANDBOOK-LIST" },
                F("docs/developer/BetterUnturnedExperience-Developer-Handbook.md",
                    CleanHandbook + "协商三段（Snapshot/Ack）还没启用。" + "\n"));

            // 未实装节只收契约面（T6 Q4 三层落点）：节内条目行出现词汇表层面的治理词条即红。
            RunCase("未实装节收进治理词条（条目行）", false, new[] { "CD-UNWIRED-OFFSCOPE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace(SecondItem,
                        SecondItem + "\n"
                        + "- 治理链接管线未接线。")));

            // 同一越界的**无标记变体**（整段散文，不以 '-' 起首）也必须咬住——规则不做标记符豁免。
            RunCase("未实装节收进治理词条（散文段）", false, new[] { "CD-UNWIRED-OFFSCOPE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace("词汇表层面的治理意图在 CONTEXT.md 词条内标注",
                        "治理链接管线未实装，详见 CONTEXT.md；词汇表层面的治理意图在 CONTEXT.md 词条内标注")));

            // token 集完整性（R3 审查要求）：V6-T4 点名的其余治理词条同样咬住（此处取「资格义务」）。
            RunCase("未实装节收进治理词条（T4 其余词条）", false, new[] { "CD-UNWIRED-OFFSCOPE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace(SecondItem,
                        SecondItem + "\n"
                        + "- 资格义务未实装。")));

            // token 集完整性（R4 审查要求）：V6-T4 Q2 点名的治理内核类型名同样咬住（此处取 Linker）。
            RunCase("未实装节收进治理内核类型名", false, new[] { "CD-UNWIRED-OFFSCOPE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace(SecondItem,
                        SecondItem + "\n"
                        + "- FeatureDefinitionLinker 未实装。")));

            // token 集完整性（R5 审查要求）：T4 点名的 LoadSetIdentity / 发布授权 同样咬住。
            RunCase("未实装节收进 LoadSetIdentity", false, new[] { "CD-UNWIRED-OFFSCOPE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace(SecondItem,
                        SecondItem + "\n"
                        + "- LoadSetIdentity 未实装。")));

            // 条目白名单（防「换个说法塞条目」）：任何未声明的条目——哪怕不含治理词条——同样红。
            RunCase("未实装节塞入未声明条目（- 标记）", false, new[] { "CD-UNWIRED-SURFACE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace(SecondItem,
                        SecondItem + "\n"
                        + "- 打包工具未实装。")));
            RunCase("未实装节塞入未声明条目（* 标记）", false, new[] { "CD-UNWIRED-SURFACE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace(SecondItem,
                        SecondItem + "\n"
                        + "* 打包工具未实装。")));
            // 「夹带」：把外来项塞进已声明条目的标题（冒号之前）——标题不等即红。
            RunCase("未实装节条目夹带外来项", false, new[] { "CD-UNWIRED-SURFACE" },
                F("docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md",
                    CleanSdk.Replace("**能力协商协议（Hello / Snapshot / Ack）未启用**：",
                        "**能力协商协议（Hello / Snapshot / Ack）未启用；打包工具也未实装**：")));
        }

        // ───────────────────────── 组 3：官方先行消费 ─────────────────────────

        /// <summary>契约加性面须有官方先行消费者：官方功能的登记工件摘要必须等于公开函数对同一载荷的
        /// 输出（证明它们真的走公开函数，而不是各自私算一套）。</summary>
        private static void V606GroupOfficialFirstConsumption(Action<bool, string> Check)
        {
            AssertOfficialDigestMatches(Check, "LIT", LitFeatureAssembly.CreateRegistration());
            AssertOfficialDigestMatches(Check, "LIR", LirFeatureAssembly.CreateRegistration());
            AssertOfficialDigestMatches(Check, "LHT", LhtFeatureAssembly.CreateRegistration());
            AssertOfficialDigestMatches(Check, "BII", BiiFeatureAssembly.CreateRegistration());
            foreach (var registration in NetworkModuleFeatureRegistration.CreateOfficialRegistrations())
                AssertOfficialDigestMatches(Check, "网络模块 " + registration.Definition.Feature.Value, registration);
        }

        private static void AssertOfficialDigestMatches(Action<bool, string> Check, string label, IFeatureRegistration registration)
        {
            Check(registration != null && registration.Definition != null, label + ": 登记体可构造");
            if (registration == null || registration.Definition == null) return;
            var payload = new List<byte>(registration.Definition.CanonicalPayload).ToArray();
            var expected = FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload);
            var actual = registration.Definition.ArtifactPayloadDigest;
            Check(actual.Part0 == expected.Part0 && actual.Part1 == expected.Part1
                && actual.Part2 == expected.Part2 && actual.Part3 == expected.Part3,
                label + ": 登记工件摘要应等于公开函数输出（官方先行消费公开函数）");
        }

        // ───────────────────────── 夹具 ─────────────────────────

        private const string CleanHandbook =
            "# 开发手册夹具" + "\n" +
            "隔离范围以 SDK 附录 A.3 为准。这是合作式隔离，不是沙箱。" + "\n" +
            "冻结注册目录之后再注册会被显式拒绝（相位名以 SDK `FeatureRegistrationPhase` 为准）。" + "\n" +
            "契约面上本版本未实装的项见 SDK 附录「未实装（本版本）」（A.8）。" + "\n";

        // 第二条目的完整原文（夹具案的改写锚）：条目标题受门禁白名单约束，正文不冻结。
        private const string SecondItem = "- **`FeatureStatusChangedEvent` 未接线**：本版本只见状态投影，事件呈现未接线。";

        private const string CleanSdk =
            "# 契约文档夹具" + "\n" +
            "\n" +
            "## 4. 编译期引用指引" + "\n" +
            "\n" +
            "登记工件：注册对象的 Definition 是 FeatureDefinitionArtifact；摘要字段由" + "\n" +
            "FeatureDefinitionDigest.ComputeArtifactPayloadDigest 生成。" + "\n" +
            "\n" +
            "## 附录 A：平台服务参考" + "\n" +
            "\n" +
            "### A.8 未实装（本版本）" + "\n" +
            "\n" +
            "- **能力协商协议（Hello / Snapshot / Ack）未启用**：IDependencyCapabilityView 已注入：能问、本版本没有可问的能力。" + "\n" +
            "- **`FeatureStatusChangedEvent` 未接线**：本版本只见状态投影，事件呈现未接线。" + "\n" +
            "\n" +
            "词汇表层面的治理意图在 CONTEXT.md 词条内标注，不进本节（本节只收契约面）。" + "\n" +
            "\n" +
            "## 附录 B：诊断与身份码表" + "\n" +
            "\n" +
            "### B.2 平台自检码 BUE-PLATFORM" + "\n" +
            "\n" +
            "| 码 | 语义 | 处置 |" + "\n" +
            "|---|---|---|" + "\n" +
            "| `BUE-PLATFORM-001` | 占位 | 占位 |" + "\n" +
            "| `BUE-HOST-001` | 注册时宿主尚未就绪（HostUnavailable） | 推迟到 BUE 前置就绪后再注册 |" + "\n";

        private static Dictionary<string, string> CleanFixtureFiles()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "docs/developer/BetterUnturnedExperience-Developer-Handbook.md", CleanHandbook },
                { "docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md", CleanSdk },
                { "src/BetterUnturnedExperience.Contracts/ContractTypes.cs",
                    "namespace BetterUnturnedExperience.Contracts { public static class FeatureDefinitionDigest { public static Digest256 ComputeArtifactPayloadDigest(System.Collections.Generic.IEnumerable<byte> canonicalPayload) { return default(Digest256); } } }" },
                { "src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs",
                    "namespace BetterUnturnedExperience.Core.Registration { internal static class FeatureRegistrationRuntime { private static bool DigestMatches() { return FeatureDefinitionDigest.ComputeArtifactPayloadDigest(new byte[0]).Part0 == 0UL; } } }" }
            };
        }

        // ───────────────────────── 辅助 ─────────────────────────

        private sealed class ProcessResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; }
            public string Errors { get; set; }
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
            var root = Path.Combine(Path.GetTempPath(), "bue-v6-contractdocs-fixture-" + Guid.NewGuid().ToString("N").Substring(0, 8));
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
    }
}
