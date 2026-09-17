using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>主机侧升级校验拒绝码（线协议 reasonCode 的字节值即此枚举）。</summary>
    internal enum ReloadSkillUpgradeReject : byte
    {
        None = 0,
        InsufficientExperience = 1,
        AlreadyMax = 2,
        TargetBeyondMax = 3,
        LevelDrift = 4,
    }

    /// <summary>一次升级校验/执行的结论（纯数据，可上线可断言）。</summary>
    internal struct ReloadSkillUpgradeDecision
    {
        internal bool Accepted;
        internal byte NewLevel;
        internal int Cost;
        internal ReloadSkillUpgradeReject Reason;
    }

    /// <summary>
    /// DEV-V5-07 技能权威核心（engine-free 单出口）：等级读、合并技能窗、
    /// 升级校验与提交。裁决（票面 Comments 2026-09-16）：
    ///   1. 0 级双击的合并窗 = 技术闸(1.5s)+额外(8s)=9.5s 单窗，成交时刻锚定；
    ///      窗内再双击 = 技能侧拒绝（红色剩余秒，主机为准，窗头技术段包含在
    ///      合并窗内=Q3「最终可用时间=技术闸+额外」的字面兑现）。≥1 级额外段
    ///      为 0：技能层永不拒绝，纯技术闸拒绝（1.5s 内刷屏、功能 A 合匣撞车）
    ///      保持现网静默——技术闸触发不伪装成技能冷却（T7 Q3）。
    ///   2. 拒绝不推进窗（连点不罚更长）。
    ///   3. 升级：只升下一级、经验先验后扣（authorize 只读；扣费+提交在调用方
    ///      按序编排），TryCommitUpgrade 落库+持久化，落盘失败回滚内存账。
    /// 时钟注入（生产=Stopwatch 单调秒；测试=手拨）。冷却窗属运行状态层：
    /// ResetForGeneration 即清，不过功能代际。
    /// </summary>
    internal sealed class ReloadSkillRuntime
    {
        private readonly ReloadSkillStore store;
        private readonly Func<double> clockSeconds;
        private readonly IReloadSkillPersistence persistence; // null = 纯内存（测试装配）
        private readonly Dictionary<ulong, double> windowReadyAt = new Dictionary<ulong, double>();

        internal ReloadSkillRuntime(ReloadSkillStore store, Func<double> clockSeconds)
            : this(store, clockSeconds, null)
        {
        }

        internal ReloadSkillRuntime(ReloadSkillStore store, Func<double> clockSeconds, IReloadSkillPersistence persistence)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.clockSeconds = clockSeconds ?? throw new ArgumentNullException(nameof(clockSeconds));
            this.persistence = persistence;
        }

        internal ReloadSkillStore Store { get { return store; } }

        internal byte GetLevel(ulong steamId, string charKey)
        {
            return store.GetLevel(steamId, charKey);
        }

        /// <summary>功能 B 双击的成交准入（技能层）：true=放行并（0 级）锚定新合并窗；
        /// false=窗内拒绝，remainingSeconds=主机口径剩余（>0，含未走完的窗头技术段）。</summary>
        internal bool TryAdmitDoubleTap(ulong steamId, string charKey, out double remainingSeconds)
        {
            remainingSeconds = 0d;
            if (steamId == 0UL) return true; // 身份不可解析=技能层不表态（现网行为保持，闸门层自有 fail-closed）
            var extra = ReloadSkillPolicy.ExtraCooldownSeconds(store.GetLevel(steamId, charKey));
            if (extra <= 0d) return true; // ≥1 级：额外段取消，本层永不武装（技术闸在 LirRepackGate）
            var now = clockSeconds();
            double readyAt;
            if (windowReadyAt.TryGetValue(steamId, out readyAt) && now < readyAt)
            {
                remainingSeconds = readyAt - now;
                return false; // 拒绝不推进窗
            }
            return true; // 放行；窗只在 ArmWindowAfterCommit（权威成交 >0 发）后武装
        }

        /// <summary>权威事务成交（压进 &gt;0 发）后才武装 0 级合并窗。≥1 级 extra=0
        /// 本层永不武装。已在窗内不刷新（拒绝不推进窗的对称：成交才开、开了不叠）。</summary>
        internal void ArmWindowAfterCommit(ulong steamId, string charKey)
        {
            if (steamId == 0UL) return;
            var extra = ReloadSkillPolicy.ExtraCooldownSeconds(store.GetLevel(steamId, charKey));
            if (extra <= 0d) return;
            var now = clockSeconds();
            double readyAt;
            if (windowReadyAt.TryGetValue(steamId, out readyAt) && now < readyAt) return;
            windowReadyAt[steamId] = now + ReloadRuntimePolicy.CooldownSeconds + extra;
        }

        /// <summary>只读校验（不落账、不扣费）：authorize 是纯函数，写路径只在 Commit。</summary>
        internal ReloadSkillUpgradeDecision TryAuthorizeUpgrade(ulong steamId, string charKey, byte targetLevel, uint experienceBalance)
        {
            var current = store.GetLevel(steamId, charKey);
            if (targetLevel > ReloadSkillPolicy.MaxSkillLevel)
                return Reject(ReloadSkillUpgradeReject.TargetBeyondMax);
            if (current == ReloadSkillPolicy.MaxSkillLevel)
                return Reject(ReloadSkillUpgradeReject.AlreadyMax);
            if (targetLevel != current + 1)
                return Reject(ReloadSkillUpgradeReject.LevelDrift);
            var cost = ReloadSkillPolicy.CostForUpgrade(current);
            if (cost < 0 || experienceBalance < (uint)cost)
                return Reject(ReloadSkillUpgradeReject.InsufficientExperience);
            return new ReloadSkillUpgradeDecision { Accepted = true, NewLevel = targetLevel, Cost = cost };
        }

        private static ReloadSkillUpgradeDecision Reject(ReloadSkillUpgradeReject reason)
        {
            return new ReloadSkillUpgradeDecision { Accepted = false, Reason = reason };
        }

        /// <summary>提交 = 写账 + 立即持久化；落盘失败回滚内存（账不落=这笔升级不存在，
        /// 调用方据此退款/回执——禁止「经验扣了账没落」的半态留在内存里）。</summary>
        internal bool TryCommitUpgrade(ulong steamId, string charKey, byte newLevel, out string error)
        {
            error = null;
            var previous = store.GetLevel(steamId, charKey);
            if (newLevel != previous + 1 || newLevel > ReloadSkillPolicy.MaxSkillLevel) return false;
            if (!store.TrySetLevel(steamId, charKey, newLevel))
            {
                error = "等级账写入被拒（键越界）";
                return false;
            }
            var persister = persistence;
            if (persister != null)
            {
                string saveError;
                if (!persister.TrySave(store.Snapshot(), out saveError))
                {
                    store.TrySetLevel(steamId, charKey, previous); // 回滚
                    error = saveError ?? "持久化失败";
                    return false;
                }
            }
            return true;
        }

        /// <summary>运行状态层不过代际：冷却窗 Stop 即清（等级账在 Store/文件，另论）。</summary>
        internal void ResetForGeneration()
        {
            windowReadyAt.Clear();
        }
    }
}
