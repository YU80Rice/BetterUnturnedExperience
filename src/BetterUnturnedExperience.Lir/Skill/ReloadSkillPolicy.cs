using System;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07（V5-T7 → 换弹技能 0～2）技能段唯一数值/文案事实源。
    /// 经验花费与 0 级额外冷却 = 票面（DEV-V5-07 Comments 2026-09-16）具名
    /// 常量的逐字兑现（规格「先例」条：常量必须先落票面再进代码，测试只引用
    /// 本类与本组一次性字面钉，禁止第二处发明数值）；2 级等待沿用规格建议 8s
    /// 且本阶段不可设。技术安全闸不在这里——它保持现网 LirRepackGate
    /// （CooldownSeconds=1.5s），一级也不得删（T7 Q3）。
    /// 三层分离（T7 定音）：进度=等级账（ReloadSkillStore），运行状态=冷却窗
    /// +自动轮（本类的秒数只描述状态层的时长），投影=分区/回执文案（本类冻结
    /// 字符串）——三层不得混成一个设置字段。
    /// </summary>
    internal static class ReloadSkillPolicy
    {
        /// <summary>票面具名：0→1 经验花费 125（用户裁定 2026-09-16）。</summary>
        internal const int XpCostLevel0To1 = 125;

        /// <summary>票面具名：1→2 经验花费 150（用户裁定 2026-09-16）。</summary>
        internal const int XpCostLevel1To2 = 150;

        /// <summary>票面具名：0 级额外技能冷却秒数 8（用户裁定 2026-09-16）。</summary>
        internal const float Level0ExtraCooldownSeconds = 8f;

        /// <summary>票面冻结：2 级双击成功后的固定等待（规格建议 8s，本阶段不可设）。</summary>
        internal const float AutoRoundDelaySeconds = 8f;

        /// <summary>技能只到 2 级：没有三级、没有超限按钮、没有占位（T7/票面）。</summary>
        internal const int MaxSkillLevel = 2;

        /// <summary>表面 B（降级设置页）的升级请求 setting id（功能私有 schema）。</summary>
        internal const string UpgradeSettingId = "inplacereload.skill.upgrade";

        /// <summary>花费表单源：只有 0→1/1→2 有价；其余（含满级以上）无花费。</summary>
        internal static int CostForUpgrade(int fromLevel)
        {
            if (fromLevel == 0) return XpCostLevel0To1;
            if (fromLevel == 1) return XpCostLevel1To2;
            return -1;
        }

        /// <summary>额外技能冷却只属 0 级；1/2 级取消该段（技术闸另在闸门层）。</summary>
        internal static double ExtraCooldownSeconds(int level)
        {
            return level <= 0 ? Level0ExtraCooldownSeconds : 0d;
        }

        // ── 冻结文案（06 同律：单源组装，呈现与回执都从这里出）──

        internal const string SkillSectionTitle = "换弹技能";

        internal static string LevelName(int level)
        {
            switch (level)
            {
                case 0: return "基础";
                case 1: return "快速换弹";
                default: return "自动压弹";
            }
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

        /// <summary>冷却中的再双击回执：正式名「换弹技能冷却中」（不叫自动压弹冷却），
        /// 红色，剩余秒向上取整——秒数由主机算、客户端只呈现（T7 Q3）。</summary>
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

        /// <summary>表面 B 升级档位文案（「维持」复位档归 ReloadSkillSettingsSurface）。</summary>
        internal static string UpgradeOptionLabel(int targetLevel)
        {
            return "请求升级到 " + targetLevel + " 级 · 花费 " + CostForUpgrade(targetLevel - 1) + " 经验";
        }
    }
}
