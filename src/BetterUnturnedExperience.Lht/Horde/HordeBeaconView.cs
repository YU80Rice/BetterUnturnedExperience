namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the server-side active-horde view (the old BeaconInfo with
    /// the engine member types lifted out — the beacon instance travels as an
    /// opaque object reference, its identity consumed only by the context
    /// guard's reference match; the live counters are read back through
    /// IHordeTrackingAuthority, so this type stays engine-free).
    ///
    /// OwnerName / LocationName / TotalZombies: beacon 注册时计算一次。
    /// Remaining/Alive 仅服务器端维护（InteractableBeacon 的 !Provider.isServer
    /// 守卫），客户端不持有本类型——客户端只读广播快照。
    /// </summary>
    internal sealed class HordeBeaconView
    {
        internal HordeBeaconView(object beacon, string ownerName, string locationName, ushort totalZombies)
            : this(beacon, ownerName, locationName, totalZombies, -1, -1)
        {
        }

        private HordeBeaconView(object beacon, string ownerName, string locationName, ushort totalZombies, int remainingLive, int aliveLive)
        {
            Beacon = beacon;
            OwnerName = ownerName ?? string.Empty;
            LocationName = locationName ?? string.Empty;
            TotalZombies = totalZombies;
            RemainingLive = remainingLive;
            AliveLive = aliveLive;
        }

        /// <summary>The InteractableBeacon instance (identity only — context guard reference match).</summary>
        internal object Beacon { get; }

        internal string OwnerName { get; }

        internal string LocationName { get; }

        internal ushort TotalZombies { get; }

        /// <summary>The live counters captured at the last read (-1 = not read yet; server-side only).</summary>
        internal int RemainingLive { get; }
        internal int AliveLive { get; }

        /// <summary>outstanding = to_spawn + alive（-1 = 尚无实时计数）。</summary>
        internal int Outstanding => RemainingLive >= 0 && AliveLive >= 0 ? RemainingLive + AliveLive : -1;

        /// <summary>已击杀数 = 总数 - outstanding（outstanding 未知时为 0）。</summary>
        internal int Killed => Outstanding >= 0 ? (TotalZombies >= Outstanding ? TotalZombies - Outstanding : 0) : 0;

        /// <summary>Beacon 实例仍然有效且总数 &gt; 0。</summary>
        internal bool IsValid => Beacon != null && TotalZombies > 0;

        /// <summary>The derived view carrying one live counter read (same identity, immutable).</summary>
        internal HordeBeaconView WithCounters(int remaining, int alive)
        {
            return new HordeBeaconView(Beacon, OwnerName, LocationName, TotalZombies, remaining, alive);
        }
    }
}
