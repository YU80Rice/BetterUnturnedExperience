using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Plugin;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-03 红测组：启动经目录冻结、链接器离开玩家文件。
    /// 被测外部行为（票 21 / V6-T4 Q1+Q5 / spec「未接线分流」）：
    ///   1. 生产就绪路径真正走过 CatalogFrozen 相位再就绪。就绪对调用方仍是原子的，
    ///      中间相位只经相位观察口可见——跳步（RegistrationOpen 直达 RuntimeReady）
    ///      的实现必令本组红；两个测试入口（FreezeCatalog / MarkRuntimeReady）保留。
    ///   2. 治理链接管线退役：链接器源码不在任何玩家工程的编译清单与目录里，
    ///      玩家路径上加载的程序集也不再声明其类型（核心与宿主是本票点名的两处）。
    ///   3. 现网官方功能仍经公共桥受理，就绪链照常到达模块启动阶段（行为不回归）。
    /// 组 1/3 用生产同源入口（宿主运行时工厂 + 完成链），不是自造第二套宿主。
    /// </summary>
    internal static class DevV603CatalogFrozenLinkerTests
    {
        private const string SolutionFile = "BetterUnturnedExperience.sln";
        private const string LinkerSourceFile = "FeatureDefinitionLinker.cs";

        /// <summary>玩家合并集（八工程封闭集，02F）——每一个都是「玩家 DLL」的构建面。</summary>
        private static readonly string[] PlayerProjects =
            { "Plugin", "Contracts", "Core", "ClientUi", "Lit", "Lir", "Lht", "Bii" };

        /// <summary>退役治理链的类型短名（链接管线：片段 → 链接器 → 目录 → 装载门）。</summary>
        private static readonly string[] RetiredGovernanceTypes =
        {
            "FeatureDefinitionLinker", "FeatureDefinitionFragment", "CompiledIdentityCatalog",
            "CompiledFeatureRecord", "LinkResult", "LinkStatus", "FeatureLoadGate", "FeatureAdmissionHandle"
        };

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

                Group("生产就绪先冻结目录再就绪（V6-T4 Q1）", () => V603GroupFreezeBeforeReady(Check));
                Group("链接器退出玩家编译清单（清单/目录/程序集三层）", () => V603GroupLinkerOutOfPlayerManifest(Check));
                Group("官方功能仍受理且完成链到达启动阶段", () => V603GroupOfficialStillAdmitted(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────── 组 1：生产就绪先冻结目录再就绪 ─────────────────

        /// <summary>完成就绪内部真正经过 CatalogFrozen（V6-T4 Q1）：核心侧观察口给出的迁移
        /// 序列含「冻结 → 就绪」且次序正确；相位门禁与幂等语义不变；两个测试入口
        /// （FreezeCatalog / MarkRuntimeReady）保留独立语义；生产路径（宿主运行时工厂 +
        /// 完成链）经宿主观察线报出冻结在就绪之前——就绪对调用方仍是原子的，跳步实现
        /// （RegistrationOpen 直达 RuntimeReady）必令本组红。</summary>
        private static void V603GroupFreezeBeforeReady(Action<bool, string> Check)
        {
            // (a) 核心侧：观察口下的迁移序列 = 先冻结、后就绪。
            var observed = new List<FeatureRegistrationPhase>();
            var runtime = new FeatureRegistrationRuntime(phase => observed.Add(phase));
            runtime.OpenRegistration();
            Check(runtime.CompleteRuntime(), "完成就绪入口应成功（注册已开）");
            Check(runtime.Phase == FeatureRegistrationPhase.RuntimeReady, "完成就绪后相位=RuntimeReady");
            Check(runtime.Catalog != null, "完成就绪后目录已发布");
            Check(observed.Contains(FeatureRegistrationPhase.CatalogFrozen),
                "生产就绪入口内部必须先冻结目录（V6-T4 Q1），实际迁移序列: " + JoinPhases(observed));
            Check(observed.IndexOf(FeatureRegistrationPhase.CatalogFrozen) < observed.IndexOf(FeatureRegistrationPhase.RuntimeReady),
                "冻结相位必须出现在就绪之前，实际迁移序列: " + JoinPhases(observed));
            Check(observed.FindAll(p => p == FeatureRegistrationPhase.RuntimeReady).Count == 1,
                "就绪只宣告一次（对外仍是原子一次就绪），实际迁移序列: " + JoinPhases(observed));
            Check(!runtime.CompleteRuntime(), "已就绪的重复调用仍返回 false（屏障语义不变）");
            Check(observed.FindAll(p => p == FeatureRegistrationPhase.RuntimeReady).Count == 1,
                "重复调用不得再宣告就绪，实际迁移序列: " + JoinPhases(observed));

            // 观察口语义=相位真实迁移流：同一相位的重复调用不再重复宣告
            // （原实现无条件重赋值，外显 Phase 值不变；观察口是本票新面，语义在此钉死）。
            runtime.EnterCoreSafeMode();
            runtime.EnterCoreSafeMode();
            Check(observed.FindAll(p => p == FeatureRegistrationPhase.CoreSafeMode).Count == 1,
                "同相位重复进入只宣告一次（观察口不掺重复调用噪声），实际迁移序列: " + JoinPhases(observed));
            Check(runtime.Phase == FeatureRegistrationPhase.CoreSafeMode, "CoreSafeMode 迁移照常生效");

            // (b) 两个测试入口保留（V6-T4 Q1）：各自独立可达，相位门禁不变。
            var entryRuntime = new FeatureRegistrationRuntime();
            entryRuntime.OpenRegistration();
            Check(!entryRuntime.MarkRuntimeReady(), "未冻结即就绪仍被拒（测试入口门禁不变）");
            Check(entryRuntime.FreezeCatalog() && entryRuntime.Phase == FeatureRegistrationPhase.CatalogFrozen,
                "测试入口 FreezeCatalog 保留：冻结后相位=CatalogFrozen");
            Check(!entryRuntime.CompleteRuntime(), "已冻结的目录不再走生产就绪入口（相位门禁不变）");
            Check(entryRuntime.MarkRuntimeReady() && entryRuntime.Phase == FeatureRegistrationPhase.RuntimeReady,
                "测试入口 MarkRuntimeReady 保留：冻结后可就绪");

            // (c) 生产路径：宿主的运行时工厂 + 完成链——宿主观察线报出「冻结 → 就绪」。
            var previousRecorder = BueRuntimeLog.Recorder;
            var previousHostRuntime = BueRuntimeHost.CurrentRuntime;
            var captured = new List<string>();
            BueRuntimeLog.Recorder = line => captured.Add(line);
            try
            {
                BueRuntimeCompletionChain.ResetForTests();
                BueRuntimeLog.ResetReadyAnnouncement();
                BueRuntimeHost.Clear();
                var hostRuntime = BueRuntimeHost.CreateRuntime();
                BueRuntimeHost.Bind(hostRuntime);
                hostRuntime.OpenRegistration();
                Check(BetterItemInteractionFeatureRegistration.Register().Accepted, "setup: 官方功能经公共桥受理");
                Check(BueRuntimeCompletionChain.TryCompleteRuntimeCore(), "生产完成链应完成就绪");

                var frozen = captured.FindIndex(line => line.Contains("event=registration-phase phase=CatalogFrozen"));
                var ready = captured.FindIndex(line => line.Contains("event=registration-phase phase=RuntimeReady"));
                Check(frozen >= 0, "宿主观察线应报出冻结相位（生产就绪路径可观察），实际行: " + JoinLines(captured));
                Check(ready >= 0, "宿主观察线应报出就绪相位，实际行: " + JoinLines(captured));
                Check(frozen >= 0 && ready > frozen, "生产路径必须先冻结再就绪，实际: 冻结@" + frozen + " 就绪@" + ready);
                Check(captured.Exists(line => line.Contains("加载成功")), "就绪宣告照常（行为不回归）: " + JoinLines(captured));
            }
            finally
            {
                BueRuntimeLog.Recorder = previousRecorder;
                BueRuntimeCompletionChain.ResetForTests();
                BueRuntimeHost.Clear();
                // 恢复本组进入前的宿主绑定（套件内后续用例沿用同一运行时）。
                if (previousHostRuntime != null) BueRuntimeHost.Bind(previousHostRuntime);
            }
        }

        // ───────────────────── 组 2：链接器退出玩家编译清单 ─────────────────────

        /// <summary>玩家 DLL 的构建面不再含链接器源：八个玩家工程各自的 csproj 清单与目录
        /// 皆零命中，src/ 全树不再有该源文件；玩家路径上加载的核心与宿主程序集也不声明
        /// 治理链类型（Mono.Cecil 只读元数据，不加载程序集）。把源文件编回任一玩家工程
        /// 或把文件放回玩家目录的突变必令本组红。</summary>
        private static void V603GroupLinkerOutOfPlayerManifest(Action<bool, string> Check)
        {
            var repoRoot = FindRepoRoot();
            var srcRoot = Path.Combine(repoRoot, "src");
            Check(Directory.Exists(srcRoot), "src 根缺失（仓库结构破坏）: " + srcRoot);

            // (a) 八个玩家工程：清单不含该源、目录不含该文件。
            foreach (var project in PlayerProjects)
            {
                var dir = Path.Combine(srcRoot, "BetterUnturnedExperience." + project);
                var csproj = Path.Combine(dir, "BetterUnturnedExperience." + project + ".csproj");
                Check(File.Exists(csproj), "玩家工程缺 csproj（仓库结构破坏）: " + project);
                var manifest = File.ReadAllText(csproj);
                Check(!manifest.Contains(LinkerSourceFile),
                    "玩家工程把链接器源编进自己（V6-T4 Q5：链接器退出玩家编译清单）: " + project);
                Check(!ListSourceFiles(dir).Any(f => Path.GetFileName(f) == LinkerSourceFile),
                    "玩家工程目录仍含链接器源（在目录、不在清单同样红——防火墙 R2 口径）: " + project);
            }

            // (b) src/ 全树零残留：任何工程（含非玩家工程）都不得把该源编进来。
            foreach (var csproj in Directory.GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\")))
            {
                Check(!File.ReadAllText(csproj).Contains(LinkerSourceFile),
                    "src 工程仍把链接器源编进自己: " + Relative(repoRoot, csproj));
            }
            var strays = Directory.GetFiles(srcRoot, LinkerSourceFile, SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\")).ToList();
            Check(strays.Count == 0, "src 树不得再有链接器源（治理链接管线退役）: " + string.Join(", ", strays.Select(f => Relative(repoRoot, f))));

            // (c) 进程内程序集：玩家路径上加载的核心与宿主都不声明治理链类型。
            AssertNoRetiredGovernanceTypes(Check, typeof(FeatureRegistrationRuntime).Assembly, "核心程序集");
            AssertNoRetiredGovernanceTypes(Check, typeof(BetterUnturnedExperiencePlugin).Assembly, "宿主程序集");

            // (d) 登记必经工件仍在（治理链退役不等于注册面残缺）：退役的是链接管线，
            //     不是 FeatureDefinitionArtifact——它由注册接收入口照旧消费。
            Check(typeof(FeatureDefinitionArtifact).IsValueType || typeof(FeatureDefinitionArtifact).IsClass,
                "登记工件 FeatureDefinitionArtifact 仍在契约面（退役对象是链接管线，不是登记 DTO）");
        }

        private static void AssertNoRetiredGovernanceTypes(Action<bool, string> Check, System.Reflection.Assembly assembly, string label)
        {
            var typeNames = new List<string>();
            var parameters = new Mono.Cecil.ReaderParameters { ReadSymbols = false };
            using (var module = Mono.Cecil.AssemblyDefinition.ReadAssembly(assembly.Location, parameters))
                foreach (var type in module.MainModule.GetTypes()) typeNames.Add(type.FullName);
            foreach (var retired in RetiredGovernanceTypes)
            {
                var hit = typeNames.Find(n => n.EndsWith("." + retired, StringComparison.Ordinal));
                Check(hit == null,
                    label + "不得再声明治理链类型 " + retired + "（链接管线退役，V6-T4 Q5），实际: " + hit);
            }
        }

        // ───────────────────── 组 3：官方受理与完成链不回归 ─────────────────────

        /// <summary>现网官方六项（BII + 整理/换弹/尸潮 + 网络双 facet）仍经各自生产入口受理，
        /// 完成链照常宣布就绪并到达模块启动阶段。完成链是生产同源入口
        /// （BueRuntimeCompletionChain.TryCompleteRuntimeCore）；模块启动语义由各功能工程
        /// 自己的套件锁定（02B..04），本组只锁「冻结/就绪接线不把官方门开不了」。</summary>
        private static void V603GroupOfficialStillAdmitted(Action<bool, string> Check)
        {
            var previousRecorder = BueRuntimeLog.Recorder;
            var previousHostRuntime = BueRuntimeHost.CurrentRuntime;
            var captured = new List<string>();
            BueRuntimeLog.Recorder = line => captured.Add(line);
            try
            {
                BueRuntimeCompletionChain.ResetForTests();
                BueRuntimeLog.ResetReadyAnnouncement();
                BueRuntimeHost.Clear();

                var runtime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(runtime);
                runtime.OpenRegistration();

                Check(BetterItemInteractionFeatureRegistration.Register().Accepted, "官方 BII 应经公共桥受理");
                Check(InventoryTidyFeatureRegistration.Register().Accepted, "官方整理应经公共桥受理");
                Check(InPlaceReloadFeatureRegistration.Register().Accepted, "官方换弹应经公共桥受理");
                Check(HordeTrackerFeatureRegistration.Register().Accepted, "官方尸潮应经公共桥受理");
                foreach (var registration in NetworkModuleFeatureRegistration.CreateOfficialRegistrations())
                    Check(runtime.Register(registration).Accepted, "官方网络 facet 应经公共桥受理: " + registration.Definition.Feature.Value);

                Check(BueRuntimeCompletionChain.TryCompleteRuntimeCore(), "生产完成链应完成就绪");
                Check(runtime.Phase == FeatureRegistrationPhase.RuntimeReady,
                    "完成链后相位应为 RuntimeReady，实际 " + runtime.Phase);
                Check(runtime.Catalog != null && runtime.Catalog.Entries.Count == 6,
                    "冻结目录应承全部六个官方条目，实际 " + (runtime.Catalog == null ? "null" : runtime.Catalog.Entries.Count.ToString()));
                Check(captured.Exists(line => line.Contains("event=module-start")),
                    "完成链应到达模块启动阶段（feature-network 未接线时=结构化 skipped，非静默）");
            }
            finally
            {
                BueRuntimeLog.Recorder = previousRecorder;
                BueRuntimeCompletionChain.ResetForTests();
                BueRuntimeHost.Clear();
                // 恢复本组进入前的宿主绑定（套件内后续用例沿用同一运行时）。
                if (previousHostRuntime != null) BueRuntimeHost.Bind(previousHostRuntime);
            }
        }

        // ───────────────────────── helpers ─────────────────────────

        private static string JoinPhases(IEnumerable<FeatureRegistrationPhase> phases)
        {
            return string.Join(" → ", phases.Select(p => p.ToString()));
        }

        private static string JoinLines(IEnumerable<string> lines)
        {
            return string.Join(" | ", lines.Take(12));
        }

        private static IEnumerable<string> ListSourceFiles(string directory)
        {
            if (!Directory.Exists(directory)) return new string[0];
            return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"));
        }

        private static string Relative(string root, string path)
        {
            return path.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, SolutionFile)))
                dir = dir.Parent;
            if (dir == null) throw new InvalidOperationException("repo root not found");
            return dir.FullName;
        }
    }
}
