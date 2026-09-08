namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the horde state immutable snapshot (the old HordeSnapshot,
    /// migrated as-is — behavior baseline = the 08 field trial).
    ///
    /// 语义：
    /// - Epoch：每个 beacon 激活时 ++（标识一次尸潮会话）
    /// - Sequence：每次发送（Update 或 Clear）时 ++（标识该 epoch 内的某次状态）
    /// - Clear 也携带 (Epoch, Sequence)，表示该 epoch 的终结
    /// - PublishIfNewer：仅当 candidate.Epoch > current.Epoch
    ///   或 (== 且 Sequence > current.Sequence) 才发布
    ///
    /// 乱序复活拒绝场景：
    ///   Update A(1,1) → 发布 A；Update B(1,2) → 发布 B；Clear C(1,3) → 发布 C；
    ///   延迟 Update B(1,2) → 拒绝（不比 C 新）；最终状态：C。
    /// </summary>
    internal sealed class HordeSnapshot
    {
        internal static readonly HordeSnapshot Empty =
            new HordeSnapshot(false, 0, 0, 0, 0, string.Empty, string.Empty);

        internal HordeSnapshot(bool active, uint epoch, uint sequence,
            ushort remaining, ushort total, string location, string initiator)
        {
            IsActive = active;
            Epoch = epoch;
            Sequence = sequence;
            Remaining = remaining;
            Total = total;
            Location = location ?? string.Empty;
            Initiator = initiator ?? string.Empty;
        }

        internal bool IsActive { get; }
        internal uint Epoch { get; }
        internal uint Sequence { get; }
        internal ushort Remaining { get; }
        internal ushort Total { get; }
        internal string Location { get; }
        internal string Initiator { get; }

        /// <summary>已击杀数 = Total - Remaining（remaining 已含 outstanding = to_spawn + alive）。
        /// remaining > total 时返回 0（防御协议异常）。</summary>
        internal int Killed => Total >= Remaining ? Total - Remaining : 0;

        /// <summary>candidate 是否比 current 新。
        /// 规则：Epoch 更大 → 新；Epoch 相等 → Sequence 更大才新。
        /// current==null 视为 (0,0)，任何 candidate 都 newer（除非自身也是 (0,0)）。</summary>
        internal bool IsNewerThan(HordeSnapshot current)
        {
            if (current == null) return Epoch > 0 || Sequence > 0;
            if (Epoch != current.Epoch) return Epoch > current.Epoch;
            return Sequence > current.Sequence;
        }
    }
}
