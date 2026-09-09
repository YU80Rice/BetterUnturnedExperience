using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts.BueNetwork;

// DEV-V2-25: the targeted-send health seams for the LIT tidy protocol. The
// DEV-V2-24 machine evidence (four P2P rounds, WARN storms of 1199 / 1057 /
// 8333 / 1650 lines) showed the F-A re-arm loop spinning one challenge
// attempt — and one WARN — per frame while the outbound transport stayed
// unavailable. The archived v6/v7 host logs (scope 4 cross-check,
// audit/2026-09-09/DEV-V2-25/scope4-evidence-check.md) resolve the window:
// each generation's birth rides a transport readiness window bounded by the
// engine-level P2P authentication completing, after which the SAME session
// delivers TidyCommitted/LHT/LIR traffic — time-window selectivity, not
// per-channel. Two bounded policy seams fix the surface: the re-arm backoff
// (below) throttles the ATTEMPTS, and the failure rate limiter bounds the
// LINES and raises one structural degradation diagnostic per episode.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// Server-side challenge re-arm backoff, keyed by connection generation.
    /// The DEV-V2-24 F-A frozen contract is preserved verbatim: the FIRST
    /// retry after a challenge delivery failure stays IMMEDIATE (interval
    /// slot 0 — one transient transport blip must recover on the next Tick,
    /// machine-verified). From the SECOND consecutive failure the wait
    /// doubles along the BueNetworkRuntime handshake re-probe precedent
    /// (HelloReprobe 1s → 8s cap): sustained unavailability de-escalates to
    /// roughly one attempt per 8s instead of one per frame. A Sent challenge
    /// clears the generation's series (the next failure starts fresh), and a
    /// dropped generation leaves no residue.
    /// </summary>
    internal sealed class LitChallengeRearmBook
    {
        /// <summary>Wait before the Nth consecutive attempt; index 0 = the F-A immediate retry, then the 1s→8s doubling cap.</summary>
        internal static readonly int[] BackoffIntervalsMs = { 0, 1000, 2000, 4000, 8000 };

        private sealed class Entry
        {
            internal int ConsecutiveFailures;
            internal DateTime NextDueUtc;
        }

        private readonly Dictionary<ulong, Entry> entries = new Dictionary<ulong, Entry>();
        private readonly Func<DateTime> clock;

        internal LitChallengeRearmBook(Func<DateTime> clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>A pending generation still owes its challenge (the adoption was rolled back by a send failure).</summary>
        internal bool HasPending(ulong generation)
        {
            return entries.ContainsKey(generation);
        }

        internal bool ShouldAttempt(ulong generation)
        {
            return !entries.TryGetValue(generation, out var entry) || clock() >= entry.NextDueUtc;
        }

        internal void NoteChallengeFailure(ulong generation)
        {
            entries.TryGetValue(generation, out var entry);
            var failures = (entry?.ConsecutiveFailures ?? 0) + 1;
            var intervalMs = BackoffIntervalsMs[Math.Min(failures, BackoffIntervalsMs.Length) - 1];
            entries[generation] = new Entry { ConsecutiveFailures = failures, NextDueUtc = clock().AddMilliseconds(intervalMs) };
        }

        internal void NoteChallengeSent(ulong generation)
        {
            entries.Remove(generation);
        }

        internal void DropGeneration(ulong generation)
        {
            entries.Remove(generation);
        }

        internal void DropAll()
        {
            entries.Clear();
        }

        internal int ConsecutiveFailures(ulong generation)
        {
            return entries.TryGetValue(generation, out var entry) ? entry.ConsecutiveFailures : 0;
        }
    }

    /// <summary>
    /// Per-(connection-generation, send-result-kind) failure series. The
    /// first failure of a series warns and every WarnEveryNth-th cumulative
    /// failure warns again (the message carries the running total); the 1st
    /// diagnostic of a degraded state goes out once at the threshold as ONE
    /// BUE-LIT-003 link-degraded line per episode (never per failure —
    /// 不炸帧); the first successful send after a degraded episode reports
    /// link-recovered and resets the series. A different result kind or a
    /// generation drop resets without residue. Client-initiated sends
    /// (RequestTidy, HotkeyFlowAck) are user-paced and ride their own
    /// explicit result handling — this book covers the server's
    /// session-addressed path (challenge / TidyCommitted / TidyHotkeyResult).
    /// </summary>
    internal sealed class LitSendFailureRateLimiter
    {
        internal const int WarnEveryNth = 50;
        internal const int DegradationThreshold = 10;

        private sealed class Series
        {
            internal NetworkSendResult Kind;
            internal long Total;               // consecutive same-kind failures so far
            internal long WarnedAtTotal;       // the total the last WARN went out at
            internal bool DegradationReported;
        }

        private readonly Dictionary<ulong, Series> series = new Dictionary<ulong, Series>();

        /// <summary>Records one failure; true when this failure must produce a WARN line (series total is returned for the cumulative count).</summary>
        internal bool ShouldWarn(ulong generation, NetworkSendResult result, out long seriesTotal)
        {
            var entry = Obtain(generation, result);
            entry.Total++;
            seriesTotal = entry.Total;
            if (entry.Total == 1 || entry.Total - entry.WarnedAtTotal >= WarnEveryNth)
            {
                entry.WarnedAtTotal = entry.Total;
                return true;
            }
            return false;
        }

        /// <summary>True exactly once per episode, when the consecutive same-kind failures reach the threshold.</summary>
        internal bool ShouldReportDegradation(ulong generation, NetworkSendResult result, out long consecutiveFailures)
        {
            consecutiveFailures = 0;
            if (!series.TryGetValue(generation, out var entry) || entry.Kind != result || entry.Total < DegradationThreshold)
            {
                return false;
            }
            if (entry.DegradationReported) return false;
            entry.DegradationReported = true;
            consecutiveFailures = entry.Total;
            return true;
        }

        /// <summary>Records a successful send; true when the just-ended episode had reported degradation (the caller emits link-recovered).</summary>
        internal bool NoteSuccess(ulong generation)
        {
            if (!series.TryGetValue(generation, out var entry)) return false;
            var wasDegraded = entry.DegradationReported;
            series.Remove(generation);
            return wasDegraded;
        }

        internal void DropGeneration(ulong generation)
        {
            series.Remove(generation);
        }

        internal void DropAll()
        {
            series.Clear();
        }

        private Series Obtain(ulong generation, NetworkSendResult result)
        {
            if (series.TryGetValue(generation, out var entry) && entry.Kind == result) return entry;
            entry = new Series { Kind = result };
            series[generation] = entry;
            return entry;
        }
    }
}
