using System.Threading;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the current horde snapshot holder (the old HordeStateTracker,
    /// migrated as-is). The SERVER local-authority publish and the CLIENT
    /// receive path both land here; Read is reference-swap safe from any
    /// thread, writes are main-thread discipline (the host tick chain).
    /// The holder is generation state: the module clears it at the stop /
    /// disable boundary (spec「静态表绑功能代际」).
    /// </summary>
    internal static class HordeStateTracker
    {
        private static HordeSnapshot _current = HordeSnapshot.Empty;

        /// <summary>读取当前快照（任意线程安全，返回不可变引用）。</summary>
        internal static HordeSnapshot Read() => Volatile.Read(ref _current);

        /// <summary>无条件发布新快照（主线程调用）。
        /// 仅供服务器端本地同步路径使用（已确认为最新）。</summary>
        internal static void Publish(HordeSnapshot snapshot)
        {
            Interlocked.Exchange(ref _current, snapshot ?? HordeSnapshot.Empty);
        }

        /// <summary>仅当 candidate 比 current 新才发布。返回是否实际发布。</summary>
        internal static bool PublishIfNewer(HordeSnapshot candidate)
        {
            if (candidate == null) return false;

            HordeSnapshot current = Volatile.Read(ref _current);
            if (!candidate.IsNewerThan(current)) return false;

            Interlocked.Exchange(ref _current, candidate);
            return true;
        }

        /// <summary>清空状态（功能停止 / 客户端断线时调用）。
        /// 网络路径的 Clear 应构造带 (Epoch, Sequence) 的 snapshot 走 PublishIfNewer
        /// 路径，而非本方法。本方法仅供代际边界无条件清空使用。</summary>
        internal static void Clear() => Publish(HordeSnapshot.Empty);
    }
}
