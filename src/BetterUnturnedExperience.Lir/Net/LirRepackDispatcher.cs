using System;
using System.Collections.Generic;
using System.Threading;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the bounded main-thread dispatcher queue — the old
    /// RepackMainThreadDispatcher migrated (structure, limits, TTL and
    /// diagnostic discipline), now a PER-SERVICE INSTANCE instead of a
    /// process-wide static: one queue per module generation, owned by the
    /// network service, closed at Stop — nothing survives the stop boundary
    /// (spec「静态表绑功能代际」), and two module instances (host tests'
    /// two-peer harness) can never drain each other's work. Inbound network
    /// callbacks (any thread) only parse and enqueue; the execution
    /// callbacks the service passes into DrainOnMainThread run on the host
    /// frame thread. Requests coalesce per sender, replies outrank requests
    /// (a request flood cannot starve the client UI), work older than the
    /// TTL is dropped un-executed (入队不等于授权), and background counters
    /// surface as one throttled aggregate diagnostic line per window.
    /// lock(sync) guards the queues and the sets only — never game state.
    /// </summary>
    internal sealed class LirRepackDispatcher
    {
        /// <summary>Test seam: the enqueue timestamp source (production: Stopwatch.GetTimestamp).</summary>
        internal Func<long> TicksForTests;

        private struct WorkItem
        {
            public bool IsRequest;
            public ulong SenderSteamId;
            public ulong RequestId;
            public int TotalTransferred;
            public long EnqueuedTicks;
        }

        private readonly object sync = new object();
        private readonly Queue<WorkItem> requests = new Queue<WorkItem>(ReloadRuntimePolicy.QueueLimit);
        private readonly Queue<WorkItem> successes = new Queue<WorkItem>(ReloadRuntimePolicy.MaxPendingSuccesses);
        private readonly HashSet<ulong> pendingRequestSenders = new HashSet<ulong>();
        private bool accepting;

        // Background threads only increment; the main thread accumulates and
        // reports through the throttled diagnostic.
        private int dropped;
        private int coalesced;
        private int parseErrors;
        private int expired;
        private int unreportedDropped;
        private int unreportedCoalesced;
        private int unreportedParseErrors;
        private int unreportedExpired;
        private int dispatchCount;
        private int missingPlayer;
        private int rejected;
        private long nextDiagnosticAtMs;

        /// <summary>Opens the queue (a fresh instance starts closed — the old EnsureOpen semantics at the module start).</summary>
        internal LirRepackDispatcher()
        {
            accepting = true;
        }

        /// <summary>Module-stop close: refuses new work and clears the queues — no stale cross-generation work ever executes.</summary>
        internal void Shutdown()
        {
            lock (sync)
            {
                accepting = false;
                requests.Clear();
                successes.Clear();
                pendingRequestSenders.Clear();
            }
        }

        /// <summary>Any-thread enqueue of a repack request; same-sender requests coalesce into the queued one.</summary>
        internal bool TryEnqueueRequest(ulong senderSteamId, ulong requestId)
        {
            lock (sync)
            {
                if (!accepting)
                {
                    Interlocked.Increment(ref dropped);
                    return false;
                }
                if (senderSteamId == 0UL || requestId == 0UL)
                {
                    Interlocked.Increment(ref parseErrors);
                    return false;
                }
                if (pendingRequestSenders.Contains(senderSteamId))
                {
                    Interlocked.Increment(ref coalesced);
                    return true;
                }
                if (requests.Count >= ReloadRuntimePolicy.QueueLimit)
                {
                    Interlocked.Increment(ref dropped);
                    return false;
                }
                pendingRequestSenders.Add(senderSteamId);
                requests.Enqueue(new WorkItem
                {
                    IsRequest = true,
                    SenderSteamId = senderSteamId,
                    RequestId = requestId,
                    EnqueuedTicks = (TicksForTests ?? DefaultTicks)(),
                });
                return true;
            }
        }

        /// <summary>Any-thread enqueue of a repack-success reply (the client-side toast work).</summary>
        internal bool TryEnqueueSuccess(ulong requestId, int totalTransferred)
        {
            lock (sync)
            {
                if (!accepting || successes.Count >= ReloadRuntimePolicy.MaxPendingSuccesses)
                {
                    Interlocked.Increment(ref dropped);
                    return false;
                }
                successes.Enqueue(new WorkItem
                {
                    IsRequest = false,
                    RequestId = requestId,
                    TotalTransferred = totalTransferred,
                    EnqueuedTicks = (TicksForTests ?? DefaultTicks)(),
                });
                return true;
            }
        }

        /// <summary>
        /// The host-frame drain: at most MaxPerFrame items per beat, replies
        /// first, TTL-expired work dropped un-executed. The execution
        /// callbacks belong to the service (engine-facing decisions stay out
        /// of the queue).
        /// </summary>
        internal void DrainOnMainThread(Action<ulong, ulong> executeRequest, Action<ulong, int> executeSuccess)
        {
            if (executeRequest == null) throw new ArgumentNullException(nameof(executeRequest));
            if (executeSuccess == null) throw new ArgumentNullException(nameof(executeSuccess));
            AccumulateAndLogDiagnostics();

            var maxAgeTicks = (long)(System.Diagnostics.Stopwatch.Frequency * ReloadRuntimePolicy.WorkTtlSeconds);
            for (var i = 0; i < ReloadRuntimePolicy.MaxPerFrame; i++)
            {
                WorkItem work;
                lock (sync)
                {
                    if (successes.Count != 0)
                    {
                        work = successes.Dequeue();
                    }
                    else if (requests.Count != 0)
                    {
                        work = requests.Dequeue();
                        pendingRequestSenders.Remove(work.SenderSteamId);
                    }
                    else
                    {
                        return;
                    }
                }

                var nowTicks = (TicksForTests ?? DefaultTicks)();
                if (nowTicks - work.EnqueuedTicks > maxAgeTicks)
                {
                    Interlocked.Increment(ref expired);
                    continue;
                }

                Interlocked.Increment(ref dispatchCount);
                if (work.IsRequest) executeRequest(work.SenderSteamId, work.RequestId);
                else executeSuccess(work.RequestId, work.TotalTransferred);
            }
        }

        /// <summary>Handler-side counters (protocol parse faults).</summary>
        internal void IncrementParseErrors()
        {
            Interlocked.Increment(ref parseErrors);
        }

        internal void IncrementMissingPlayer()
        {
            Interlocked.Increment(ref missingPlayer);
        }

        internal void IncrementRejected()
        {
            Interlocked.Increment(ref rejected);
        }

        /// <summary>The throttled aggregate line (one per diagnostic window, never a per-frame flood).</summary>
        private void AccumulateAndLogDiagnostics()
        {
            unreportedDropped += Interlocked.Exchange(ref dropped, 0);
            unreportedCoalesced += Interlocked.Exchange(ref coalesced, 0);
            unreportedParseErrors += Interlocked.Exchange(ref parseErrors, 0);
            unreportedExpired += Interlocked.Exchange(ref expired, 0);

            var nowMs = System.Diagnostics.Stopwatch.GetTimestamp() * 1000L / System.Diagnostics.Stopwatch.Frequency;
            if (nowMs < Volatile.Read(ref nextDiagnosticAtMs)) return;
            var dispatches = Interlocked.Exchange(ref dispatchCount, 0);
            var missing = Interlocked.Exchange(ref missingPlayer, 0);
            var rejectedCount = Interlocked.Exchange(ref rejected, 0);
            if (unreportedDropped + unreportedCoalesced + unreportedParseErrors + unreportedExpired == 0
                && dispatches == 0 && missing == 0 && rejectedCount == 0)
            {
                return;
            }
            Volatile.Write(ref nextDiagnosticAtMs, nowMs + (long)(ReloadRuntimePolicy.DiagnosticIntervalSeconds * 1000f));
            LirRuntime.LogInfo("[RepackNet] dispatcher summary: dispatches=" + dispatches
                + ", missingPlayer=" + missing + ", rejected=" + rejectedCount
                + ", dropped=" + unreportedDropped + ", coalesced=" + unreportedCoalesced
                + ", parseErrors=" + unreportedParseErrors + ", expired=" + unreportedExpired);
            unreportedDropped = 0;
            unreportedCoalesced = 0;
            unreportedParseErrors = 0;
            unreportedExpired = 0;
        }

        private static long DefaultTicks()
        {
            return System.Diagnostics.Stopwatch.GetTimestamp();
        }
    }
}
