using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Dispatch;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V3-04: plugin composition root for the platform main-thread
    /// dispatcher (the BueHostEventRuntime precedent — one host-owned seam
    /// composed at bootstrap, driven once per plugin Update beat, never
    /// feature-specific). The dispatcher instance is created on the Unity/
    /// Unturned main thread (Awake / first host start), which is also its
    /// pump-thread guard; features reach it through their
    /// IFeatureMainThread bootstrap view (IFeatureBootstrap.MainThread,
    /// availability matrix row wired by DEV-V3-04), the generation lifecycle
    /// rides the host start path (BueFeatureStartRuntime), and the execution
    /// rides the shared tick chain (BueRuntimeTickChain.Tick — the platform's
    /// ONE sanctioned pump, so features need no pump of their own).
    /// Internal on purpose: the dispatcher is host composition, not
    /// third-party surface — external features bind through the contract
    /// interfaces only.
    /// </summary>
    internal static class BueMainThreadRuntime
    {
        private static MainThreadDispatcherRuntime dispatcher;

        /// <summary>The host dispatcher (creates the seam on first access, on
        /// the composing main thread).</summary>
        internal static MainThreadDispatcherRuntime Dispatcher
        {
            get
            {
                EnsureCreated();
                return dispatcher;
            }
        }

        /// <summary>One-shot composition: the dispatcher with the runtime log
        /// as its diagnostic sink (the T5 internal-seam discipline — every
        /// post decision, rejection, withdraw and task fault surfaces as a
        /// structured line, never silently).</summary>
        internal static bool EnsureCreated()
        {
            if (dispatcher != null) return false;
            dispatcher = new MainThreadDispatcherRuntime(line => BueRuntimeLog.Runtime("[BUE-MT] " + line));
            BueRuntimeLog.Runtime("BUE main-thread dispatcher created host=io.github.yu80rice.bue.host diagnosticId=BUE-MT-CREATED");
            return true;
        }

        /// <summary>
        /// The plugin Update chain pump beat: drain up to the per-beat
        /// budget of posted tasks. Never throws into the tick chain (the
        /// dispatcher isolates task faults internally; the composition-level
        /// belt mirrors the host clock's TickOnce discipline).
        /// </summary>
        internal static void TickOnce()
        {
            var current = dispatcher;
            if (current == null) return; // nothing was ever composed/posted — no seam to pump
            try { current.Pump(); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("event=main-thread result=pump-failed stage=composition errorType=" + error.GetType().Name + " diagnosticId=BUE-MT-006");
            }
        }

        /// <summary>
        /// The host stop boundary (plugin teardown): refuses every post and
        /// drains the queue un-executed. The dispatcher instance survives so
        /// the diagnostic sink stays wired (the seam itself never goes null
        /// mid-teardown; a new-process start reopens via OpenGeneration).
        /// </summary>
        internal static void Shutdown(string reason)
        {
            var current = dispatcher;
            if (current == null) return;
            current.ShutdownHost(reason);
        }

        /// <summary>Test seam: drop the composed dispatcher so a later
        /// EnsureCreated starts fresh (the pump-thread guard binds to the
        /// composing thread; host tests create per group). Production
        /// teardown never calls this — the seam dies with the process.</summary>
        internal static void Clear()
        {
            dispatcher = null;
        }
    }
}
