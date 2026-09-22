using System;
using System.IO;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Lir;
using BetterUnturnedExperience.Lit;

namespace BetterUnturnedExperience.Plugin.Tests
{
    internal static class DevV706CopyHandbookTests
    {
        internal static void Run()
        {
            HandbookUsesFrozenFeatureCopy();
            ChromeCopyUsesFrozenFeatureSentences();
            TidyTooltipsUseFrozenGestures();
            SuccessCopyUsesPlayerFeedbackSink();
            SkillDescriptionsShareFrozenFacts();
            DirectionIsNotAnEffectiveDescriptor();
        }

        private static void HandbookUsesFrozenFeatureCopy()
        {
            var text = File.ReadAllText(FindWorkspaceFile("docs", "BetterUnturnedExperience-Player-Handbook.md"));
            Assert(text.Contains("背包、普通容器和车辆后备箱里直接拖入。可放位置显示绿色半透明框，不能放显示红色。自动旋转只在两个正向之间切换，文字保持可读。不增强丢到地面。"), "手册 BII 三行冻结句");
            Assert(text.Contains("点「整理」整理当前栏；Ctrl+点击整理全身（只动身上五页）。打开世界箱或已授权后备箱时，容器标题栏也有一颗「整理」。物品按固定用途把同类放在一起，同一类里大件优先，从左上紧凑排列，空位留到右下。图标尽量正向。放不下则格子不动并说明原因。旧的三种模式和整理方向不再决定结果。"), "手册 LIT 三行冻结句");
            Assert(text.Contains("持枪双击换弹键立刻压弹。弹药信息区在原版「当前/上限」下显示「总弹药」，开火或背包里匹配弹药变化时立刻更新。换弹技能 0～2 级画在 U 菜单战斗区，用原版经验升级，按地图和角色槽分开计算——0 基础、1 快速换弹、2 自动压弹（每 8 秒给身上五页未满匣填弹，仍可双击立刻压）。整理后仍会合并同 ID 弹匣。只到 2 级，没有超限弹匣。"), "手册 LIR 三行冻结句");
            Assert(!text.Contains("备匣 N · 备弹 M") && !text.Contains("双击成功后固定等待再自动压一轮") && !text.Contains("管理面板只保留「整理方向」"), "手册不保留第五阶段旧承诺");
        }

        private static void ChromeCopyUsesFrozenFeatureSentences()
        {
            Assert(PanelChromeCopy.TryGetDescription("io.github.yu80rice.bue.better-item-interaction", out var bii)
                && bii == "在支持的格子里增强拖入，可放绿、不可放红。", "BII 对照表冻结句");
            Assert(PanelChromeCopy.TryGetDescription("io.github.yu80rice.bue.inventory-tidy", out var lit)
                && lit == "整理背包与装备栏；标题栏点「整理」，同类归拢并从左上紧凑排列。", "LIT 对照表冻结句");
            Assert(PanelChromeCopy.TryGetDescription("io.github.yu80rice.bue.in-place-reload", out var lir)
                && lir == "双击换弹键压弹；持枪显示总弹药；整理后合并弹匣。", "LIR 对照表冻结句");
            Assert(PanelChromeCopy.TryGetDescription("io.github.yu80rice.bue.horde-tracker", out var horde)
                && horde == "由主机追踪尸潮，并在客户端显示播报。", "尸潮对照表保持不变");
            Assert(PanelChromeCopy.TryGetDescription("io.github.yu80rice.bue.noop", out var noop)
                && noop == "生态接入样板，用于展示功能描述、Toggle 和 Choice 在面板中的呈现。", "NoOp 对照表保持不变");
        }

        private static void TidyTooltipsUseFrozenGestures()
        {
            Assert(InventoryTidyUiPatch.TidyTooltipText == "左键：整理当前栏；Ctrl+左键：整理全身（不含容器）", "身上页 Tooltip 冻结句");
            Assert(LitContainerTitleBarAdapter.TooltipText == "左键：整理当前打开的容器（只动这一只，不含身上与地面）", "容器 Tooltip 冻结句");
            Assert(LitTidyCopy.SuccessText == "背包已整理：同类已归拢，并从左上紧凑排列。", "整理成功句冻结句");
        }

        private static void SuccessCopyUsesPlayerFeedbackSink()
        {
            var shown = string.Empty;
            var previous = LitContainerFeedback.ToastSink;
            try
            {
                LitContainerFeedback.ToastSink = text => shown = text;
                LitContainerFeedback.ShowSuccess(LitTidyCopy.SuccessText);
                Assert(shown == "背包已整理：同类已归拢，并从左上紧凑排列。", "成功句经玩家反馈出口显示，不仅写诊断日志");
            }
            finally
            {
                LitContainerFeedback.ToastSink = previous;
            }
        }

        private static void SkillDescriptionsShareFrozenFacts()
        {
            Assert(ReloadSkillPolicy.LevelDescription(0) == "可双击换弹键立刻压弹；有额外技能冷却。", "0 级技能描述冻结句");
            Assert(ReloadSkillPolicy.LevelDescription(1) == "取消额外技能冷却；防重复提交的技术闸仍然保留。", "1 级技能描述冻结句");
            Assert(ReloadSkillPolicy.LevelDescription(2) == "每 8 秒自动为身上五页中的空或未满弹匣，从匹配弹药箱填弹；仍可双击换弹键立刻手动压弹。", "2 级技能描述冻结句");
            Assert(ReloadSkillSettingsSurface.DescriptionForLevel(0) == ReloadSkillPolicy.LevelDescription(0)
                && ReloadSkillSettingsSurface.DescriptionForLevel(1) == ReloadSkillPolicy.LevelDescription(1)
                && ReloadSkillSettingsSurface.DescriptionForLevel(2) == ReloadSkillPolicy.LevelDescription(2), "设置页与技能行共用三句事实源");
        }

        private static void DirectionIsNotAnEffectiveDescriptor()
        {
            Assert(InventoryTidyModule.CreateSettingsDescriptors(new BetterUnturnedExperience.Contracts.FeatureId(LitRuntime.FeatureIdValue)).Count == 0,
                "整理方向描述符不再作为有效设置");
        }

        private static string FindWorkspaceFile(params string[] parts)
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null)
            {
                var candidate = directory.FullName;
                for (var i = 0; i < parts.Length; i++) candidate = Path.Combine(candidate, parts[i]);
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            throw new InvalidOperationException("workspace file not found: " + string.Join("/", parts));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
