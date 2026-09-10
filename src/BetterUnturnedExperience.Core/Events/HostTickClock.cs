using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Events
{
    /// <summary>
    /// DEV-V2-19: the host clock — the single producer of HostTick on the
    /// host event bus. Frozen invariants (spec「宿主时钟」+ T5/T6):
    ///   - the host produces it uniformly (publishes under the reserved host
    ///     identity through the bus's internal host path — the identity
    ///     cannot be minted into a public publisher view, so no feature can
    ///     forge a tick);
    ///   - the frequency and the phase are fixed: exactly one tick per pump
    ///     beat, Phase = Update (frozen value 0); further phases would be a
    ///     contract-registry addition;
    ///   - sequence numbers start at 1 and are strictly monotonic (+1 per
    ///     tick, never reset by the clock's lifetime);
    ///   - DeltaTime is the monotonic-time increment since the previous tick
    ///     (0 on the first tick; a time source stepping back is clamped to 0
    ///     and leaves the baseline at its high-water mark — R3-Spec fix);
    ///   - the payload carries only sequence/time/phase — never feature
    ///     logic;
    ///   - Tick() never throws into its pump caller (a fault surfaces as a
    ///     structured diagnostic and a false return).
    /// The main-thread guarantee rests on the pump: the single production
    /// driver is the plugin Update chain (BueHostEventRuntime.TickOnce), and
    /// the clock adds no threading of its own (main-thread-only by
    /// construction, same declaration as the DEV-V2-18 engine binding).
    /// Feature modules must never build their own Unity Update pumps — they
    /// subscribe to this seam instead.
    /// DEV-V3-05 (V3-T6 registration, zero new contract surface): pause/
    /// resume semantics are plain absence of beats — while the host pump is
    /// paused no tick is produced and the first tick after resume carries the
    /// real accumulated interval (no catch-up, no reset; ignoring large
    /// intervals is the feature's own choice). The recommended low-frequency
    /// pattern for features is SELF-THROTTLING on DeltaTime/sequence
    /// accumulation (the LHT 10Hz presentation cadence is the official
    /// precedent); derived clocks and per-feature scheduling stay foggy.
    /// </summary>
    public sealed class HostTickClock
    {
        /// <summary>The reserved platform host identity the clock publishes under.</summary>
        public const string HostPublisherId = "io.github.yu80rice.bue.host";

        private readonly FeatureEventBus bus;
        private readonly Func<long> monotonicMilliseconds;
        private long lastMilliseconds;
        private ulong tickNumber;
        private bool started;

        public HostTickClock(FeatureEventBus bus, Func<long> monotonicMilliseconds = null)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.monotonicMilliseconds = monotonicMilliseconds ?? DefaultMonotonicMilliseconds;
        }

        /// <summary>
        /// One pump beat: produce and publish exactly one HostTick. Returns
        /// false only when the publish was rejected or the clock itself
        /// failed — never throws.
        /// </summary>
        public bool Tick()
        {
            try
            {
                var nowMs = monotonicMilliseconds();
                float delta;
                if (!started)
                {
                    started = true;
                    delta = 0f;
                    lastMilliseconds = nowMs;
                }
                else
                {
                    // DeltaTime is the adjacent-tick difference of MONOTONIC
                    // time (R3-Spec fix): a time source stepping back is
                    // clamped to a zero delta AND leaves the baseline at its
                    // high-water mark, so a later small reading can never
                    // produce a bogus positive delta against the stepped-back
                    // value.
                    var diff = nowMs - lastMilliseconds;
                    if (diff > 0) lastMilliseconds = nowMs;
                    if (diff < 0) diff = 0;
                    delta = diff / 1000f;
                }
                tickNumber++;
                return bus.TryPublishHost(HostTick.EventId, new HostTick(tickNumber, delta, TickPhase.Update));
            }
            catch (Exception error)
            {
                // DEV-V3-05 (T6 semantics ⑦): the failure surfaces as a
                // structured diagnostic with its registered code family
                // BUE-CLOCK-001 — explicit false return, zero dispatch, the
                // sequence is not consumed and the time baseline does not
                // move on a failed beat (pinned by the fake-clock group).
                bus.EmitDiagnostic("event=host-tick result=failed errorType=" + error.GetType().Name
                    + " message=" + error.Message + " diagnosticId=BUE-CLOCK-001");
                return false;
            }
        }

        private static long DefaultMonotonicMilliseconds()
        {
            return MonotonicMilliseconds(System.Diagnostics.Stopwatch.GetTimestamp(), System.Diagnostics.Stopwatch.Frequency);
        }

        /// <summary>
        /// R1-Spec DEVIATION-1 fix: the raw Stopwatch timestamp is scaled by
        /// the actual Stopwatch.Frequency (raw ticks are 1/Frequency seconds,
        /// never assumed 10 MHz), so the millisecond reading is portable.
        /// Pure and deterministic — pinned by the fake-clock group.
        /// </summary>
        public static long MonotonicMilliseconds(long stopwatchTimestamp, long stopwatchFrequency)
        {
            if (stopwatchFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(stopwatchFrequency));
            return stopwatchTimestamp * 1000L / stopwatchFrequency;
        }
    }
}
