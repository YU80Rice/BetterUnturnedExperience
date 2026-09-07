using System;
using BetterUnturnedExperience.Core.Events;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-19: plugin composition root for the host event bus and the
    /// host clock. Created once at plugin Awake (idempotent); driven once per
    /// plugin Update (TickOnce). Feature modules reach the same seams later
    /// through IFeatureBootstrap.Events / OwnedEvents when the host start
    /// path lands (DEV-V2-21/22); nothing here is feature-specific.
    /// Internal on purpose (R1-Standards SMELL-1): the bus is host-owned
    /// composition, not third-party surface — external features bind through
    /// the frozen contract interfaces only, so the reserved host identity
    /// stays unreachable from outside.
    /// </summary>
    internal static class BueHostEventRuntime
    {
        private static FeatureEventBus bus;
        private static HostTickClock clock;

        /// <summary>The host event bus (creates the runtime pair on first access).</summary>
        internal static FeatureEventBus Bus { get { EnsureCreated(); return bus; } }

        /// <summary>
        /// One-shot composition: bus (with the runtime log as its diagnostic
        /// sink) + host clock. Returns true when this call created the pair
        /// (first call), false on later idempotent calls — the「一次成型」
        /// discipline of the DEV-V2-18 arm path.
        /// </summary>
        internal static bool EnsureCreated()
        {
            if (bus != null) return false;
            bus = new FeatureEventBus(line => BueRuntimeLog.Runtime(line));
            clock = new HostTickClock(bus);
            BueRuntimeLog.Runtime("BUE host event runtime created hostClock=" + HostTickClock.HostPublisherId + " diagnosticId=BUE-EVENTS-001");
            return true;
        }

        /// <summary>
        /// The plugin Update pump beat: produce and publish one HostTick.
        /// Never throws into the Update chain (the clock isolates internally;
        /// this is the composition-level belt per the pump discipline).
        /// </summary>
        internal static bool TickOnce()
        {
            var current = clock;
            if (current == null) return false;
            try { return current.Tick(); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("event=host-tick result=failed stage=composition errorType=" + error.GetType().Name + " message=" + error.Message);
                return false;
            }
        }

        /// <summary>
        /// Test seam: drops the composed pair so a later EnsureCreated starts
        /// fresh (the diagnostic sink is per-bus instance, so nothing leaks
        /// across test iterations). Production teardown never calls this —
        /// the clock dies with the process, matching the pump lifecycle.
        /// </summary>
        internal static void Clear()
        {
            bus = null;
            clock = null;
        }
    }
}
