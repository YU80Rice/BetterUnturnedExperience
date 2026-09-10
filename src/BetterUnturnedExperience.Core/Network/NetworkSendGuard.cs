using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts.BueNetwork;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V3-04: the per-session send guard — the platform 保底速率限制
    /// (fixed window 2000ms / 256 data sends per connection generation, the
    /// ticket-fixed observable numbers) and the session link-health book
    /// (10 consecutive transport failures → ONE degraded episode; the first
    /// delivered send after it → ONE recovered + reset — the DEV-V2-25
    /// threshold precedent, now platform-owned).
    ///
    /// Boundary rules frozen by V3-T5:
    ///   - budget exhaustion = the frame is NOT handed to the transport and
    ///     NOT silently dropped: an explicit Throttled result with a
    ///     structured diagnostic accompanies every refused target;
    ///   - Throttled is a platform decision, not a transport failure — it
    ///     never feeds the failure series (误伤 would turn rate limiting
    ///     into a fake link-degradation storm);
    ///   - state is keyed by ConnectionGeneration: a new generation starts
    ///     with a fresh budget and a fresh health series (跨代际清零), and a
    ///     dropped generation leaves no residue;
    ///   - official and ecosystem channels share the same session budget —
    ///     no identity exemption (契约面同权).
    /// The class holds NO lock of its own: every method is called by
    /// BueNetworkRuntime under its state lock, with the monotonic clock
    /// sampled OUTSIDE the lock by the caller (repo convention — state
    /// protection never spans the injected clock delegate). Diagnostics are
    /// decided here as data; the runtime emits the lines OUTSIDE the lock.
    /// </summary>
    internal sealed class NetworkSendGuard
    {
        internal const int BudgetWindowMs = 2000;
        internal const int BudgetPerWindow = 256;
        internal const int LinkDegradationThreshold = 10;

        private sealed class Entry
        {
            internal long WindowStartMs;
            internal int Used;
            internal long ConsecutiveFailures;
            internal bool Degraded;
            internal NetworkSendResult LastFailure;
        }

        private readonly Dictionary<ulong, Entry> byGeneration = new Dictionary<ulong, Entry>();

        /// <summary>
        /// Consumes one budget token for the generation, sliding into a fresh
        /// window when the fixed window has passed. False = the send is
        /// refused (Throttled) WITHOUT touching the transport or the health
        /// series; usedAfter reports the window's consumed count.
        /// </summary>
        internal bool TryConsume(ulong generation, long nowMs, out int usedAfter)
        {
            var entry = Obtain(generation, nowMs);
            if (nowMs - entry.WindowStartMs >= BudgetWindowMs)
            {
                entry.WindowStartMs = nowMs;
                entry.Used = 0;
            }
            if (entry.Used >= BudgetPerWindow)
            {
                usedAfter = entry.Used;
                return false;
            }
            entry.Used++;
            usedAfter = entry.Used;
            return true;
        }

        /// <summary>Records one executed send's outcome for the health level.
        /// True + consecutiveFailures = this failure reached the threshold
        /// and the ONE degraded line is due (never repeated inside the
        /// episode). A delivered send with a degraded episode pending returns
        /// recovered=true exactly once and resets the series.</summary>
        internal bool NoteSendOutcome(ulong generation, bool delivered, NetworkSendResult failureResult,
            out bool reportDegraded, out bool reportRecovered, out long consecutiveFailures)
        {
            reportDegraded = false;
            reportRecovered = false;
            consecutiveFailures = 0;
            if (!byGeneration.TryGetValue(generation, out var entry))
            {
                if (delivered) return false;
                entry = new Entry { WindowStartMs = 0L };
                byGeneration[generation] = entry;
            }
            if (delivered)
            {
                reportRecovered = entry.Degraded;
                consecutiveFailures = entry.ConsecutiveFailures;
                entry.ConsecutiveFailures = 0;
                entry.Degraded = false;
                entry.LastFailure = NetworkSendResult.None;
                return reportRecovered;
            }
            entry.ConsecutiveFailures++;
            entry.LastFailure = failureResult;
            consecutiveFailures = entry.ConsecutiveFailures;
            if (!entry.Degraded && entry.ConsecutiveFailures >= LinkDegradationThreshold)
            {
                entry.Degraded = true;
                reportDegraded = true;
            }
            return reportDegraded || reportRecovered;
        }

        /// <summary>Generation drop: budget and health leave together (no
        /// residue crosses generations).</summary>
        internal void DropGeneration(ulong generation)
        {
            byGeneration.Remove(generation);
        }

        internal void DropAll()
        {
            byGeneration.Clear();
        }

        private Entry Obtain(ulong generation, long nowMs)
        {
            if (!byGeneration.TryGetValue(generation, out var entry))
            {
                entry = new Entry { WindowStartMs = nowMs };
                byGeneration[generation] = entry;
            }
            return entry;
        }
    }
}
