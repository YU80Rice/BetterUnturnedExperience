namespace BetterUnturnedExperience.Lir
{
    /// <summary>可选的当前玩家技能账作用域解析缝；不改变既有技能网络接口。</summary>
    internal interface IReloadSkillScopeHooks
    {
        bool TryResolveLocalScope(out ReloadSkillScopeKey scope);
    }
}
