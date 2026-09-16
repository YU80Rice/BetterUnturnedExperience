namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 客户端等级镜像（呈现输入，不是事实源）：只装主机确认过的等级
    /// （T7 Q2「客户端只显示主机确认的等级」）。未确认 = 分区不画、不猜默认级。
    /// Stop/注销即清——确认态属连接会话，不过代际；主机侧权威读恒走
    /// ReloadSkillStore，从不读这里。
    /// </summary>
    internal static class ReloadSkillLevelMirror
    {
        private static byte confirmedLevel;
        private static bool hasConfirmed;

        internal static bool HasConfirmed { get { return hasConfirmed; } }

        internal static byte ConfirmedLevel { get { return confirmedLevel; } }

        internal static void ConfirmLevel(byte level)
        {
            if (level > ReloadSkillPolicy.MaxSkillLevel) return; // fail-closed：脏确认不落镜像
            confirmedLevel = level;
            hasConfirmed = true;
        }

        internal static void Clear()
        {
            confirmedLevel = 0;
            hasConfirmed = false;
        }
    }
}
