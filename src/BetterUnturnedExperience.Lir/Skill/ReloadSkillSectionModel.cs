using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>分区的一行投影：纯文本行，或一颗（可用/禁用两态）升级按钮行。
    /// 按钮行自带目标级（呈现层不反解文案——文案改动不破坏点击语义）。</summary>
    internal readonly struct ReloadSkillSectionRow
    {
        internal readonly string Text;
        internal readonly bool IsButton;
        internal readonly bool Enabled;
        internal readonly byte TargetLevel;

        internal ReloadSkillSectionRow(string text, bool isButton, bool enabled = false, byte targetLevel = 0)
        {
            Text = text;
            IsButton = isButton;
            Enabled = enabled;
            TargetLevel = targetLevel;
        }
    }

    /// <summary>
    /// DEV-V5-07 表面 A 的纯投影（两种 presentation adapter 共用的行内容，
    /// U 菜单分区与设置页降级面画的是同一套行模型）：标题 + 同一套等级
    /// （0/1/2 三行，票面「看到同一套等级」的字面兑现）+ 至多一颗下一级
    /// 升级按钮；满级 = 已满级行、无按钮、无占位。没有三级、没有超限
    /// （T7：三级完整超限不进本阶段不进技能 UI——行数与文案从结构上给不出）。
    /// 余额只决定按钮可点态（显示门）；权威复核永远在主机（TryAuthorizeUpgrade）。
    /// </summary>
    internal static class ReloadSkillSectionModel
    {
        internal static IReadOnlyList<ReloadSkillSectionRow> BuildRows(int level, uint xpBalance)
        {
            if (level < 0) level = 0;
            if (level > ReloadSkillPolicy.MaxSkillLevel) level = ReloadSkillPolicy.MaxSkillLevel;
            var rows = new List<ReloadSkillSectionRow>
            {
                new ReloadSkillSectionRow(ReloadSkillPolicy.SkillSectionTitle, false),
            };
            for (var step = 0; step <= ReloadSkillPolicy.MaxSkillLevel; step++)
            {
                if (step == ReloadSkillPolicy.MaxSkillLevel && level == ReloadSkillPolicy.MaxSkillLevel)
                {
                    rows.Add(new ReloadSkillSectionRow(ReloadSkillPolicy.MaxLevelRowLabel, false));
                    break;
                }
                rows.Add(new ReloadSkillSectionRow(ReloadSkillPolicy.LevelRowLabel(step), false));
            }
            if (level < ReloadSkillPolicy.MaxSkillLevel)
            {
                var cost = ReloadSkillPolicy.CostForUpgrade(level);
                rows.Add(new ReloadSkillSectionRow(ReloadSkillPolicy.UpgradeButtonLabel(level + 1), true,
                    cost > 0 && xpBalance >= (uint)cost, (byte)(level + 1)));
            }
            return rows;
        }
    }
}
