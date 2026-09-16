namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 技能引擎缝（唯一跨界出口）：技能权威核心（ReloadSkillRuntime，
    /// engine-free）与原版接触（characterName 身份、原版经验余额/扣减、枪械
    /// 指纹）之间的薄接口。生产实现 LirSkillEngineHooks 触碰一切引擎类型
    /// （方法体 NoInlining——04 教训：含未解析 ECall 的方法体在无 Unity 运行时
    /// 的进程 JIT 即抛，测试路径绝不进入）；宿主测试以假件替换（模块
    /// SkillHooksForTests）。语义：
    ///   TryBeginRepackWindow = 0 级合并技能窗的权威入口（成交/拒绝+主机剩余秒）；
    ///   ExecuteUpgrade = 主机校验→扣原版经验→落账（全有或全无，内部回滚）；
    ///   GetLevelFor = 按玩家取等级（内部解析角色键；解析不出=0 账 fail-closed）；
    ///   TryResolveLocalLevel = 本机确认等级（SP/房主镜像，客机走线）；
    ///   CaptureFingerprint/FingerprintMatches = 2 级自动压弹到点重检的「同枪
    ///   同匣」判据（不透明 token，引擎细节不外泄）。
    /// </summary>
    internal interface ILirSkillHooks
    {
        bool TryBeginRepackWindow(ulong steamId, out double remainingSeconds);

        ReloadSkillUpgradeDecision ExecuteUpgrade(ulong steamId, byte targetLevel);

        byte GetLevelFor(ulong steamId);

        bool TryResolveLocalLevel(out byte level);

        object CaptureFingerprint(ulong steamId);

        bool FingerprintMatches(object captured, object fresh);
    }
}
