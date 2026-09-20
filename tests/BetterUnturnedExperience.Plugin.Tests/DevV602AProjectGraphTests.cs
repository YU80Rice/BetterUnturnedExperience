using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-02A 红测组：工程图扩展与组装类型骨架（V6-T2 追加裁决）。
    /// 被测外部行为：
    ///   1. 工程图骨架在位：整理/换弹/尸潮三个功能目录与更好的物品交互骨架目录各有恰一工程文件，
    ///      新工程可独立编译（-t:Rebuild 退出 0），全套构建图认识它们。
    ///   2. 恰好一个公开组装类型：Lit/Lir/Lht/Bii/ClientUi 顶层 public 类型恰一（其余保持内部，
    ///      今天四目录顶层 public 类型数为 0，只加不删）；四个登记缝类型暴露 CreateRegistration()
    ///      返回契约 IFeatureRegistration（T2 点名的宿主接线缝；public ≠ 契约，不进 C.1）。
    ///   3. 程序集友元禁令：src 下 InternalsVisibleTo 只许指向 *.Tests（测试对被测工程的既有豁免），
    ///      跨功能友元一律在册即红。
    /// 骨架期边界（如实记入）：旧源码平铺仍在（收缩归 02E/02F），整理/换弹/尸潮的真登记体
    /// 随 02B/C/D 迁入后缝体才返回真值——骨架期调用即抛并点名迁移票。
    /// 维护锚：与 DevV6FirewallFullsuiteTests 照相机同规约，每条注明作废票。
    /// </summary>
    internal static class DevV602AProjectGraphTests
    {
        private const string SolutionFile = "BetterUnturnedExperience.sln";
        private static readonly string[] SkeletonProjects =
            { "Lit", "Lir", "Lht", "Bii" };

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

                Group("工程图骨架在位且可独立编译", () => V6GroupSkeletonProjects(Check));
                Group("恰好一个公开组装类型与登记缝形状", () => V6GroupAssemblyTypeSeam(Check));
                Group("程序集友元禁令（跨功能常规手段禁止）", () => V6GroupFriendAssemblyBan(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：工程图骨架在位且可独立编译 ─────────────────────────

        /// <summary>三个功能目录 + Bii 骨架目录各恰一 csproj（R3 语义；作废票：02B/C/D 起清单随
        /// 真分工程收编而扩张，本组的存在性/唯一性断言不随之作废），且逐工程 -t:Rebuild 独立编译
        /// 退出 0（构建退出码必须核验，杜绝静默编旧二进制）。</summary>
        private static void V6GroupSkeletonProjects(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            foreach (var shortName in SkeletonProjects)
            {
                var dir = Path.Combine(repoRoot, "src", "BetterUnturnedExperience." + shortName);
                Check(Directory.Exists(dir), "工程目录缺失: src/BetterUnturnedExperience." + shortName
                    + "（缺工程目录=红）");
                var csprojs = Directory.Exists(dir)
                    ? Directory.GetFiles(dir, "*.csproj")
                    : new string[0];
                Check(csprojs.Length == 1, "工程目录应恰有一个 .csproj: BetterUnturnedExperience." + shortName
                    + "，实际 " + csprojs.Length + " 个");
                if (csprojs.Length != 1) continue;

                var build = RunDotnetMsbuild(csprojs[0], repoRoot);
                Check(build.ExitCode == 0, "新工程应可独立编译（-t:Rebuild exit 0）: BetterUnturnedExperience."
                    + shortName + "，实际 exit=" + build.ExitCode + Environment.NewLine + build.All);
            }
        }

        // ─────────────────────── 组 2：恰好一个公开组装类型与登记缝形状 ───────────────────────

        /// <summary>V6-T2 追加：Lit/Lir/Lht/Bii/ClientUi 顶层 public 类型恰一，其余保持 internal
        /// （今天四目录为 0，本票只加不删——作废票：无；「不多于一个」是本阶段常驻约束）。
        /// 四个登记缝类型必须暴露 CreateRegistration() 返回契约 IFeatureRegistration（作废票：
        /// DEV-V6-02B/C/D/04 迁入真登记体时缝体翻真值、签名不变）。ClientUi 缝形状归 02E，本组
        /// 只锁「恰一公开类型」。用 Cecil 读编译产物（读元数据不加载执行），防源码正则漏数。</summary>
        private static void V6GroupAssemblyTypeSeam(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            foreach (var shortName in SkeletonProjects)
            {
                var dll = Path.Combine(repoRoot, "src", "BetterUnturnedExperience." + shortName,
                    "bin", "Release", "BetterUnturnedExperience." + shortName + ".dll");
                Check(File.Exists(dll), "新工程编译产物缺失（先独立编译才计数）: " + shortName + ".dll");
                if (!File.Exists(dll)) continue;

                var publicTypes = ListPublicTopLevelTypes(dll);
                Check(publicTypes.Count == 1, "工程顶层 public 类型应恰一: BetterUnturnedExperience."
                    + shortName + "，实际 " + publicTypes.Count + " 个 (" + string.Join(", ", publicTypes) + ")");
                if (publicTypes.Count != 1) continue;

                var seam = FindRegistrationSeam(dll, publicTypes[0]);
                Check(seam != null, "公开组装类型须暴露宿主接线缝 CreateRegistration(): IFeatureRegistration（T2 点名）: "
                    + shortName + "，实际 " + (seam ?? "缺席"));
            }

            // ClientUi：宿主仍平铺其源码（收缩归 02E），本组读套件构建产物计数；缝形状归 02E 定形。
            var clientUiDll = Path.Combine(repoRoot, "src", "BetterUnturnedExperience.ClientUi",
                "bin", "Release", "BetterUnturnedExperience.ClientUi.dll");
            Check(File.Exists(clientUiDll), "界面工程编译产物缺失: ClientUi.dll（套件构建应已产出）");
            if (File.Exists(clientUiDll))
            {
                var clientUiPublic = ListPublicTopLevelTypes(clientUiDll);
                Check(clientUiPublic.Count == 1, "界面工程顶层 public 类型应恰一（V6-T2 追加）: ClientUi，实际 "
                    + clientUiPublic.Count + " 个 (" + string.Join(", ", clientUiPublic) + ")");
            }
        }

        /// <summary>顶层（非嵌套）public 类型全名列表：Cecil 读元数据，不解析依赖（被测 DLL 的
        /// 引用缺失不影响类型枚举）。</summary>
        private static List<string> ListPublicTopLevelTypes(string dllPath)
        {
            var result = new List<string>();
            using (var asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { ReadSymbols = false }))
            {
                foreach (var type in asm.MainModule.Types)
                {
                    if (type.IsPublic) result.Add(type.FullName);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        /// <summary>登记缝形状：公开类型上的 public static CreateRegistration()，返回
        /// BetterUnturnedExperience.Contracts.IFeatureRegistration；命中返回描述串，未命中返回 null。</summary>
        private static string FindRegistrationSeam(string dllPath, string publicTypeFullName)
        {
            using (var asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { ReadSymbols = false }))
            {
                foreach (var type in asm.MainModule.Types)
                {
                    if (!type.IsPublic || type.FullName != publicTypeFullName) continue;
                    foreach (var method in type.Methods)
                    {
                        if (!method.IsPublic || !method.IsStatic || method.Name != "CreateRegistration") continue;
                        if (method.ReturnType == null) continue;
                        if (method.ReturnType.FullName == "BetterUnturnedExperience.Contracts.IFeatureRegistration")
                            return type.FullName + "::CreateRegistration() -> " + method.ReturnType.FullName;
                    }
                }
            }
            return null;
        }

        // ───────────────────────── 组 3：程序集友元禁令 ─────────────────────────

        /// <summary>V6-T2 追加：禁止程序集友元当跨功能常规手段。src 下一切 InternalsVisibleTo
        /// 只许指向 *.Tests（测试对被测工程的既有豁免：ClientUi.Tests/Core→Network.Tests/
        /// Plugin.Tests 三处，T2 Q6「测试对被测工程的 IVT 可保留」）；任何指向功能/宿主/界面
        /// 等非测试程序集的友元即在册红（作废票：无——禁令是本阶段常驻约束，不是过渡态）。</summary>
        private static void V6GroupFriendAssemblyBan(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var srcRoot = Path.Combine(repoRoot, "src");
            var offenders = new List<string>();
            foreach (var file in Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("\\bin\\") || file.Contains("\\obj\\")) continue;
                foreach (Match match in Regex.Matches(File.ReadAllText(file), @"InternalsVisibleTo\s*\(\s*""([^""]+)"""))
                {
                    var target = match.Groups[1].Value;
                    if (!target.EndsWith(".Tests", StringComparison.Ordinal))
                        offenders.Add(Path.GetFileName(file) + " -> " + target);
                }
            }
            Check(offenders.Count == 0, "程序集友元只许指向 *.Tests（跨功能友元被禁）: "
                + string.Join("; ", offenders));
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

        /// <summary>dotnet msbuild 短横线开关重建单工程（与全套重建同族；ProjectReference 契约
        /// 随引随建）。超时杀进程，退出码由调用方核验。</summary>
        private static ProcessResult RunDotnetMsbuild(string csprojPath, string workingDirectory)
        {
            return RunProcess("dotnet", "msbuild \"" + csprojPath + "\" -t:Rebuild -p:Configuration=Release -v:m -nologo",
                workingDirectory, 600);
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
