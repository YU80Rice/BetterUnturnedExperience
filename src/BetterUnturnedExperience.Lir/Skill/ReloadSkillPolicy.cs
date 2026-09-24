using System;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V7-04 技能段唯一数值/文案事实源。等级账、被动调度和 UI 只消费本类
    /// 的稳定语义，不互相调用。
    /// </summary>
    internal static class ReloadSkillPolicy
    {
        internal const int XpCostLevel0To1 = 125;
        internal const int XpCostLevel1To2 = 150;
        internal const float Level0ExtraCooldownSeconds = 8f;
        internal const float AutoRoundDelaySeconds = 8f;
        internal const int MaxSkillLevel = 2;
        internal const string UpgradeSettingId = "inplacereload.skill.upgrade";
        internal const string SkillSectionTitle = "换弹技能";

        internal static int CostForUpgrade(int fromLevel)
        {
            if (fromLevel == 0) return XpCostLevel0To1;
            if (fromLevel == 1) return XpCostLevel1To2;
            return -1;
        }

        internal static double ExtraCooldownSeconds(int level)
        {
            return level <= 0 ? Level0ExtraCooldownSeconds : 0d;
        }

        internal static string LevelName(int level)
        {
            switch (level)
            {
                case 0: return "基础";
                case 1: return "快速换弹";
                default: return "自动压弹";
            }
        }

        internal static string Description(int level)
        {
            switch (level)
            {
                case 0: return "可双击换弹键立刻压弹；有额外技能冷却。";
                case 1: return "取消额外技能冷却；防重复提交的技术闸仍然保留。";
                default: return "每 8 秒自动为身上五页中的空或未满弹匣，从匹配弹药箱填弹；仍可双击换弹键立刻手动压弹。";
            }
        }

        internal static string LevelDescription(int level)
        {
            return Description(level);
        }
        internal static string LevelRowLabel(int level)
        {
            return "等级 " + level + " · " + LevelName(level);
        }



        internal static string MaxLevelRowLabel
        {
            get { return LevelRowLabel(MaxSkillLevel) + " · 已满级"; }
        }

        internal static string UpgradeButtonLabel(int targetLevel)
        {
            return "升级到 " + targetLevel + " 级 · 花费 " + CostForUpgrade(targetLevel - 1) + " 经验";
        }

        internal static string UpgradeOptionLabel(int targetLevel)
        {
            return "请求升级到 " + targetLevel + " 级 · 花费 " + CostForUpgrade(targetLevel - 1) + " 经验";
        }

        internal static string MakeCooldownToast(double remainingSeconds)
        {
            var secs = (int)Math.Ceiling(remainingSeconds < 0d ? 0d : remainingSeconds);
            return "<b><color=#ff5c5c>换弹技能冷却中：还需 " + secs + " 秒</color></b>";
        }

        internal static string MakeUpgradeAcceptedToast(int newLevel, int cost)
        {
            return "<b><color=#5ce65c>换弹技能已升级到 " + newLevel + " 级 · 消耗 " + cost + " 经验</color></b>";
        }

        internal static string MakeUpgradeRejectedToast(ReloadSkillUpgradeReject reason, int targetLevel)
        {
            switch (reason)
            {
                case ReloadSkillUpgradeReject.InsufficientExperience:
                    return "<b><color=#ff5c5c>经验不足：升级到 " + targetLevel + " 级需要 "
                        + CostForUpgrade(targetLevel - 1) + " 经验</color></b>";
                case ReloadSkillUpgradeReject.AlreadyMax:
                    return "<b><color=#ff5c5c>换弹技能已满级（最高 " + MaxSkillLevel + " 级）</color></b>";
                default:
                    return "<b><color=#ff5c5c>换弹技能等级已更新，请刷新重试</color></b>";
            }
        }
    }
}
