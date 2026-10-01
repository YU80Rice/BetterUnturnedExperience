namespace BetterUnturnedExperience.Lir
{
    /// <summary>可选的当前玩家技能账作用域解析缝；不改变既有技能网络接口。
    /// DEV-V7-04 客机缺陷修复②：TryResolveScopeOf 让权威 seat 能解析「任意
    /// 已连接玩家」的账键（回执按请求者 scope 下行），而 TryResolveLocalScope
    /// 只解析本机 seat。</summary>
    internal interface IReloadSkillScopeHooks
    {
        bool TryResolveLocalScope(out ReloadSkillScopeKey scope);

        bool TryResolveScopeOf(ulong steamId, out ReloadSkillScopeKey scope);
    }
}
