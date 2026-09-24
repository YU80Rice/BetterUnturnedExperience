namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// 客户端等级镜像（呈现输入，不是事实源）：只装当前作用域的主机确认值。
    /// 作用域变化先清确认，避免换图/换槽窗口显示上一世界等级。
    /// </summary>
    internal static class ReloadSkillLevelMirror
    {
        private static byte confirmedLevel;
        private static bool hasConfirmed;
        private static bool hasScope;
        private static ReloadSkillScopeKey confirmedScope;

        internal static bool HasConfirmed { get { return hasConfirmed; } }
        internal static byte ConfirmedLevel { get { return confirmedLevel; } }
        internal static ReloadSkillScopeKey ConfirmedScope { get { return confirmedScope; } }

        internal static void BeginScope(ReloadSkillScopeKey scope)
        {
            if (!scope.IsValid) return;
            if (!hasScope || !confirmedScope.Equals(scope))
            {
                confirmedScope = scope;
                hasScope = true;
                confirmedLevel = 0;
                hasConfirmed = false;
            }
        }

        internal static void ConfirmLevel(byte level)
        {
            if (level > ReloadSkillPolicy.MaxSkillLevel) return;
            confirmedLevel = level;
            hasConfirmed = true;
        }

        internal static void ConfirmLevel(ReloadSkillScopeKey scope, byte level)
        {
            BeginScope(scope);
            ConfirmLevel(level);
        }

        internal static bool HasConfirmedFor(ReloadSkillScopeKey scope)
        {
            return hasConfirmed && hasScope && confirmedScope.Equals(scope);
        }

        internal static void Clear()
        {
            confirmedLevel = 0;
            hasConfirmed = false;
            hasScope = false;
            confirmedScope = default(ReloadSkillScopeKey);
        }
    }
}
