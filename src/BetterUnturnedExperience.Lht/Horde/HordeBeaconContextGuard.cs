namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the frozen beacon context guard (spec「上下文守卫原则」：
    /// 补丁可共享原生调用点，不能共享业务上下文，非相关路径立即放行).
    /// The two beacon counter Postfixes are LHT's only patches and share their
    /// native call sites (InteractableBeacon.spawnRemaining / despawnAlive)
    /// with anything else that ever touches them — the business context never
    /// leaves this guard: only a call carrying the tracked active beacon's
    /// instance marks the broadcast dirty; every other call releases
    /// immediately (returns false, touches nothing). Clear is the generation
    /// boundary. The dirty flag lives here and nowhere else — the patch
    /// writes it, the broadcast cycle consumes it.
    /// </summary>
    internal sealed class HordeBeaconContextGuard
    {
        private object trackedBeacon;
        private bool broadcastDirty;

        /// <summary>The tracking module records the active beacon instance on activation.</summary>
        internal void Track(object beacon)
        {
            trackedBeacon = beacon;
        }

        /// <summary>The patch-facing entry: true = the call carries this feature's
        /// context (the tracked beacon) and the broadcast is marked dirty;
        /// false = a non-related path through the shared call site — released
        /// immediately, zero state change.</summary>
        internal bool TryRequestBroadcast(object beacon)
        {
            if (beacon == null || !ReferenceEquals(trackedBeacon, beacon)) return false;
            broadcastDirty = true;
            return true;
        }

        /// <summary>The broadcast cycle's consumption: true at most once per dirty mark
        /// (same-frame multi-kills coalesce into one update frame).</summary>
        internal bool ConsumeBroadcastDirty()
        {
            if (!broadcastDirty) return false;
            broadcastDirty = false;
            return true;
        }

        /// <summary>The generation boundary: context and dirty flag both drop.</summary>
        internal void Clear()
        {
            trackedBeacon = null;
            broadcastDirty = false;
        }
    }
}
