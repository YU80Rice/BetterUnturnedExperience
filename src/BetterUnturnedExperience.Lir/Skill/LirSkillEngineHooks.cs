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
    internal sealed class LirSkillEngineHooks : ILirSkillHooks
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
                return runtime.TryAdmitDoubleTap(steamId, CharacterKeyOf(steamId), out remainingSeconds);
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
                runtime.ArmWindowAfterCommit(steamId, CharacterKeyOf(steamId));
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
                return runtime.GetLevel(steamId, CharacterKeyOf(steamId));
            }
            catch (Exception)
            {
                return 0; // 身份解析不出 = 0 级账（fail-closed，不猜等级）
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
                var charKey = CharacterKeyOfPlayer(player);
                var balance = player.skills != null ? player.skills.experience : 0u;
                var decision = runtime.TryAuthorizeUpgrade(steamId, charKey, targetLevel, balance);
                if (!decision.Accepted) return decision;
                SpendExperience(player, (uint)decision.Cost);
                if (!runtime.TryCommitUpgrade(steamId, charKey, decision.NewLevel, out var commitError))
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

        // ── 指纹：equipment state 向量 + 存活位（粗筛；终裁仍是压弹事务）──

        public object CaptureFingerprint(ulong steamId)
        {
            try
            {
                var player = playerResolver(steamId);
                if (player == null || player.equipment == null || player.equipment.state == null || player.life == null)
                    return null;
                return new SkillGunFingerprint((byte[])player.equipment.state.Clone(), !player.life.isDead);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool FingerprintMatches(object captured, object fresh)
        {
            var a = captured as SkillGunFingerprint;
            var b = fresh as SkillGunFingerprint;
            if (a == null || b == null) return ReferenceEquals(captured, fresh);
            return a.Equals(b);
        }

        // ── 引擎接触方法体（NoInlining：宿主测试进程绝不在编译路径内）──

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string CharacterKeyOf(ulong steamId)
        {
            var player = playerResolver(steamId);
            return player == null ? null : CharacterKeyOfPlayer(player);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string CharacterKeyOfPlayer(Player player)
        {
            // 宿主红线（08 实机钉死）：SDG.Unturned.SteamPlayerID 的自定义 == 运算符
            // 无判空（op_Equality 两侧直接 callvirt get_steamID），`playerID == null`
            // 写法无论实值是否为 null 必抛 NRE=真机技能窗/升级链 100% 断。判等一律
            // `is null`（IL 引用比较，不经运算符；Lht OwnerResolver 生产先例同律）。
            if (player is null) return null;
            var channel = player.channel;
            if (channel is null) return null;
            var owner = channel.owner; // SteamPlayer
            if (owner is null) return null;
            var pid = owner.playerID;
            if (pid is null) return null;
            return ReloadSkillStore.NormalizeCharKey(pid.characterName);
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

    /// <summary>2 级自动压弹到点重检的不透明 token：装备槽位向量 + 存活位。
    /// 「同枪同匣未切枪」的充分粗筛（变枪/换匣附件位必动 state），未死位
    /// 单独入 token——死了必取消。弹药余量等细目不在这层（事务重检）。</summary>
    internal sealed class SkillGunFingerprint
    {
        private readonly byte[] state;
        private readonly bool alive;

        internal SkillGunFingerprint(byte[] state, bool alive)
        {
            this.state = state;
            this.alive = alive;
        }

        public override bool Equals(object other)
        {
            var typed = other as SkillGunFingerprint;
            if (typed == null || typed.alive != alive) return false;
            if (ReferenceEquals(typed.state, state)) return true;
            if (typed.state == null || state == null || typed.state.Length != state.Length) return false;
            for (var i = 0; i < state.Length; i++)
            {
                if (typed.state[i] != state[i]) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hash = alive ? 1 : 0;
            if (state != null)
            {
                for (var i = 0; i < state.Length; i++) hash = unchecked(hash * 31 + state[i]);
            }
            return hash;
        }
    }
}
