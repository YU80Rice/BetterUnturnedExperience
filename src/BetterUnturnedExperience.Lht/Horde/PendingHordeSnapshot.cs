namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the single-slot latest-snapshot mailbox (the old
    /// PendingHordeSnapshot, migrated as-is). Lock-protected compare,
    /// replace and extract; out-of-order snapshots are refused by
    /// (epoch, sequence). The functional-private consistency machinery the
    /// ticket freezes in place.
    /// </summary>
    internal static class PendingHordeSnapshot
    {
        private static readonly object Gate = new object();
        private static HordeSnapshot _latest = HordeSnapshot.Empty;
        private static bool _hasPending;

        /// <summary>入站回调线程调用：仅当 candidate 比 _latest 新才保留。
        /// 旧包、乱序包和 null 均静默拒绝。锁内仅做比较与字段赋值。</summary>
        internal static void Store(HordeSnapshot candidate)
        {
            if (candidate == null) return;
            lock (Gate)
            {
                if (_hasPending && !candidate.IsNewerThan(_latest)) return;
                _latest = candidate;
                _hasPending = true;
            }
        }

        /// <summary>主线程调用：原子提取唯一保留的 candidate 并按新旧发布。
        /// 保留 _latest 作为后续迟到包的排序基线。</summary>
        internal static void DrainToState()
        {
            HordeSnapshot candidate;
            lock (Gate)
            {
                if (!_hasPending) return;
                candidate = _latest;
                _hasPending = false;
                // 不清空 _latest：作为后续迟到包的排序基线
            }
            HordeStateTracker.PublishIfNewer(candidate);
        }

        /// <summary>代际边界调用：重置邮箱到初始状态。</summary>
        internal static void Reset()
        {
            lock (Gate)
            {
                _latest = HordeSnapshot.Empty;
                _hasPending = false;
            }
        }
    }
}
