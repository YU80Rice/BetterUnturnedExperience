using System;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>The /horde status probe the command shell hands back to ask for the
    /// current active horde (false = none to report).</summary>
    internal delegate bool HordeStatusSource(out HordeBeaconView beacon);

    /// <summary>
    /// DEV-V2-20: the tracking module's engine seam. EVERY engine touch point
    /// of the server-authority piece rides this interface — production binds
    /// HordeProductionAuthority (BeaconManager / Provider / Commander /
    /// ChatManager), host tests bind a recording fake. The tracking module
    /// itself stays engine-free and host-testable.
    /// </summary>
    internal interface IHordeTrackingAuthority
    {
        /// <summary>Provider.isServer — the per-call role probe (never a cached role).</summary>
        bool IsServerRole();

        void SubscribeBeaconUpdated(Action<byte, bool> handler);
        void UnsubscribeBeaconUpdated(Action<byte, bool> handler);
        void SubscribeServerHosted(Action handler);
        void UnsubscribeServerHosted(Action handler);

        /// <summary>Resolves the nav beacon to the tracked view (beacon instance,
        /// owner name, location name, wave total). false = absent or the asset
        /// has not injected its total yet (wait for the next beacon update).</summary>
        bool TryResolveBeacon(byte nav, out HordeBeaconView beacon);

        /// <summary>The live counters (getRemaining/getAlive — server-side only).</summary>
        bool TryReadCounters(HordeBeaconView beacon, out int remaining, out int alive);

        /// <summary>The pending /horde flush: registers the command when the vanilla
        /// command table is ready (Provider.onServerHosted arrived first).
        /// true = the pending request is RESOLVED (registered, already there,
        /// or given up for good — the old one-shot failure semantics);
        /// false = keep pending and retry next tick.</summary>
        bool TryFlushHordeCommandRegistration(HordeStatusSource source);

        /// <summary>Deregisters the command (idempotent; safe when never registered).</summary>
        void DeregisterHordeCommand();

        /// <summary>The horde start/end chat notifications (server broadcast, ChatManager.say).</summary>
        void NotifyHordeStart(HordeBeaconView beacon);
        void NotifyHordeEnd(HordeBeaconView beacon);
    }
}
