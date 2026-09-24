using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>一条复刻原版技能行的纯数据投影。</summary>
    internal readonly struct ReloadSkillSectionRow
    {
        internal readonly string Name;
        internal readonly string LevelText;
        internal readonly string DescriptionText;
        internal readonly string CostText;
        internal readonly bool IsClickable;
        internal readonly bool IsFull;
        internal readonly byte TargetLevel;
        internal readonly int LockCount;
        internal readonly int UnlockedCount;
        internal readonly int Height;
        internal readonly int Step;

        internal string Text { get { return Name + " · " + LevelText; } }
        internal bool IsButton { get { return true; } }
        internal bool Enabled { get { return IsClickable; } }

        internal ReloadSkillSectionRow(string name, string levelText, string descriptionText,
            string costText, bool isClickable, bool isFull, byte targetLevel,
            int lockCount, int unlockedCount, int height, int step)
        {
            Name = name;
            LevelText = levelText;
            DescriptionText = descriptionText;
            CostText = costText;
            IsClickable = isClickable;
            IsFull = isFull;
            TargetLevel = targetLevel;
            LockCount = lockCount;
            UnlockedCount = unlockedCount;
            Height = height;
            Step = step;
        }
    }

    /// <summary>V7-04 原版行模型：一条完整行，不生成三级或多行等级阶梯。</summary>
    internal static class ReloadSkillSectionModel
    {
        internal const int RowHeight = 80;
        internal const int RowStep = 90;

        internal static IReadOnlyList<ReloadSkillSectionRow> BuildRows(int level, uint xpBalance)
        {
            if (level < 0) level = 0;
            if (level > ReloadSkillPolicy.MaxSkillLevel) level = ReloadSkillPolicy.MaxSkillLevel;
            var cost = ReloadSkillPolicy.CostForUpgrade(level);
            var full = level >= ReloadSkillPolicy.MaxSkillLevel;
            var clickable = !full && cost > 0 && xpBalance >= (uint)cost;
            var target = full ? (byte)0 : (byte)(level + 1);
            var costText = full ? "Full" : "花费 " + cost + " 经验";
            return new[]
            {
                new ReloadSkillSectionRow(
                    ReloadSkillPolicy.SkillSectionTitle,
                    "等级 " + level + "/" + ReloadSkillPolicy.MaxSkillLevel + " · " + ReloadSkillPolicy.LevelName(level),
                    ReloadSkillPolicy.Description(level),
                    costText,
                    clickable,
                    full,
                    target,
                    ReloadSkillPolicy.MaxSkillLevel + 1,
                    level,
                    RowHeight,
                    RowStep),
            };
        }
    }
}
