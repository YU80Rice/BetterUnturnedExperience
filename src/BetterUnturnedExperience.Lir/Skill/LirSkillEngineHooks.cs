using System;
using System.Runtime.CompilerServices;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 引擎钩子生产实现：技能权威核心（纯）与原版接触之间的薄层。
    /// 方法体含引擎类型接触（channel/PlayerSkills/PlayerEquipment/PlayerLife）
    /// ——宿主测试进程永不进入（04 教训：未解析 ECall 的方法体 JIT 即抛），
    /// 全部 NoInlining 并逐方法 try/catch fail-closed（身份解析不出=等级账 0、
    /// 指纹解析不出=null=自动轮取消，绝不猜）。
    /// 扣原版经验用 PlayerSkills.askSpend（R5 §4.3：独立经验 API，不必新建
    /// 技能槽；无余额门——本类先读 player.skills.experience 预检，经核心
    /// TryAuthorizeUpgrade 校验后才扣，杜绝 uint 下溢）。退款=askAward（对称）。
    /// 持久化失败的回滚顺序：先扣经验→落账失败→立即 askAward 原额退回——
    /// 「经验扣了账没落」不得成为终态。真机各角色（SP/房主/客机 U3DS）扣减
    /// 复制行为 = 具名接缝缺口，实机随 DEV-V5-08 验。
    /// </summary>
    internal sealed class LirSkillEngineHooks : ILirSkillHooks, IReloadSkillScopeHooks
    {
        private readonly ReloadSkillRuntime runtime;
        private readonly Func<ulong, Player> playerResolver;
        private readonly Func<string, bool> isServerRole;

        internal LirSkillEngineHooks(ReloadSkillRuntime runtime)
            : this(runtime, LirProductionAuthority.ResolvePlayerBySteamId, _ => LirProductionAuthority.IsServerRole())
        {
        }

        // 宿主测试构造：解析器注入，方法体不触引擎。
        internal LirSkillEngineHooks(ReloadSkillRuntime runtime, Func<ulong, Player> playerResolver, Func<string, bool> isServerRole)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.playerResolver = playerResolver ?? throw new ArgumentNullException(nameof(playerResolver));
            this.isServerRole = isServerRole ?? throw new ArgumentNullException(nameof(isServerRole));
        }

        // ── 身份与等级（纯数据访问，无 ECall 风险，但照 04 规约统一隔离）──

        public bool TryBeginRepackWindow(ulong steamId, out double remainingSeconds)
        {
            try
            {
                return runtime.TryAdmitDoubleTap(ScopeOf(steamId), out remainingSeconds);
            }
            catch (Exception error)
            {
                LirRuntime.LogError("[ReloadSkill] 技能窗判定异常（本击不拦截，闸门层仍守）: " + error.Message);
                remainingSeconds = 0d;
                return true;
            }
        }

        public void ArmRepackWindowAfterCommit(ulong steamId)
        {
            try
            {
                runtime.ArmWindowAfterCommit(ScopeOf(steamId));
            }
            catch (Exception)
            {
                // 身份解析不出 = 不武装（fail-closed；下一次双击仍可用）
            }
        }

        public byte GetLevelFor(ulong steamId)
        {
            try
            {
                return runtime.GetLevel(ScopeOf(steamId));
            }
            catch (Exception)
            {
                return 0; // 身份解析不出 = 0 级账（fail-closed，不猜等级）
            }
        }

        public bool TryResolveLocalScope(out ReloadSkillScopeKey scope)
        {
            scope = default(ReloadSkillScopeKey);
            try
            {
                var resolver = LirRuntime.HostLocalSteamId;
                var localId = resolver != null ? resolver() : 0UL;
                if (localId == 0UL) return false;
                scope = ScopeOf(localId);
                return scope.IsValid;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryResolveLocalLevel(out byte level)
        {
            level = 0;
            try
            {
                // DEV-V6-02C: Steam identity rides the host-injected resolver
                // (unbound = 0UL = unresolvable = no local level — the former
                // test-host default, the feature never names the host).
                var resolver = LirRuntime.HostLocalSteamId;
                var localId = resolver != null ? resolver() : 0UL;
                if (localId == 0UL) return false;
                level = GetLevelFor(localId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        // ── 升级：校验→扣原版经验→落账（失败退款）──

        public ReloadSkillUpgradeDecision ExecuteUpgrade(ulong steamId, byte targetLevel)
        {
            try
            {
                var player = playerResolver(steamId);
                if (player == null || player.channel == null || player.channel.owner == null)
                    return Refuse(ReloadSkillUpgradeReject.LevelDrift);
                var scope = ScopeOfPlayer(player, steamId);
                if (!scope.IsValid) return Refuse(ReloadSkillUpgradeReject.LevelDrift);
                var balance = player.skills != null ? player.skills.experience : 0u;
                var decision = runtime.TryAuthorizeUpgrade(scope, targetLevel, balance);
                if (!decision.Accepted) return decision;
                SpendExperience(player, (uint)decision.Cost);
                if (!runtime.TryCommitUpgrade(scope, decision.NewLevel, out var commitError))
                {
                    AwardExperience(player, (uint)decision.Cost); // 全有或全无：账落不下=原额退回
                    LirRuntime.LogError("[ReloadSkill] 升级落账失败已退款（steam=" + steamId + "）: " + commitError);
                    return Refuse(ReloadSkillUpgradeReject.LevelDrift);
                }
                return decision;
            }
            catch (Exception error)
            {
                LirRuntime.LogError("[ReloadSkill] 升级执行异常（拒绝，账零改）: " + error.Message);
                return Refuse(ReloadSkillUpgradeReject.LevelDrift);
            }
        }

        private static ReloadSkillUpgradeDecision Refuse(ReloadSkillUpgradeReject reason)
        {
            return new ReloadSkillUpgradeDecision { Accepted = false, Reason = reason };
        }

        public bool IsPlayerAvailable(ulong steamId)
        {
            try
            {
                var player = playerResolver(steamId);
                return player != null && player.life != null && !player.life.isDead;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ── 引擎接触方法体（NoInlining：宿主测试进程绝不在编译路径内）──

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ReloadSkillScopeKey ScopeOf(ulong steamId)
        {
            var player = playerResolver(steamId);
            return player == null ? default(ReloadSkillScopeKey) : ScopeOfPlayer(player, steamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ReloadSkillScopeKey ScopeOfPlayer(Player player, ulong steamId)
        {
            if (player is null) return default(ReloadSkillScopeKey);
            var channel = player.channel;
            if (channel is null) return default(ReloadSkillScopeKey);
            var owner = channel.owner;
            if (owner is null) return default(ReloadSkillScopeKey);
            var pid = owner.playerID;
            if (pid is null) return default(ReloadSkillScopeKey);
            var serverId = Provider.serverID;
            var mapName = Level.info == null ? null : Level.info.name;
            return new ReloadSkillScopeKey(serverId, steamId, pid.characterID, mapName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SpendExperience(Player player, uint cost)
        {
            player.skills.askSpend(cost);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AwardExperience(Player player, uint refund)
        {
            player.skills.askAward(refund);
        }
    }

}
