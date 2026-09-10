using System;
using System.Collections.Generic;

// DEV-V2-25: the targeted-send health seams for the LIT tidy protocol. The
// DEV-V2-24 machine evidence (four P2P rounds, WARN storms of 1199 / 1057 /
// 8333 / 1650 lines) showed the F-A re-arm loop spinning one challenge
// attempt — and one WARN — per frame while the outbound transport stayed
// unavailable. The archived v6/v7 host logs (scope 4 cross-check,
// audit/2026-09-09/DEV-V2-25/scope4-evidence-check.md) resolve the window:
// each generation's birth rides a transport readiness window bounded by the
// engine-level P2P authentication completing, after which the SAME session
// delivers TidyCommitted/LHT/LIR traffic — time-window selectivity, not
// per-channel. DEV-V2-25 fixed the surface with two bounded policy seams:
// the re-arm backoff (below) throttling the ATTEMPTS, and the failure rate
// limiter bounding the LINES. DEV-V3-04 retires the second seam — platform
// link health (BUE-NET-002/003 电平式诊断) and the send budget (Throttled +
// BUE-NET-001) now own failure visibility; the re-arm backoff below STAYS
// (业务重试/退避不上收 — V3-T5 裁决一).
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

    // DEV-V3-04 RETIRED (the whole LitSendFailureRateLimiter class lived
    // here): the per-frame WARN cadence (first + every 50th) and the
    // BUE-LIT-003 degraded/recovered episode surface are platform
    // responsibilities now — BueNetworkRuntime's session link health raises
    // ONE BUE-NET-002 degraded per episode and ONE BUE-NET-003 recovered on
    // delivery, the send budget answers over-limit sends with an explicit
    // Throttled + BUE-NET-001 line (官方报告链路健康，功能处理业务重试；
    // T5 裁决一「LIT 告警限频被链路健康接管后可退役」). Only the business
    // re-arm backoff (LitChallengeRearmBook above) stays feature-private.
    // The DEV-V2-25 harness evidence (DEV-V2-24 F-A 风暴 8333/1199/1057/1650
    // 条逐帧告警) is the regression this retirement root-fixes; the retired
    // absence is red-anchored in the DEV-V3-04 test groups.
}
