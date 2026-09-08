using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the per-steam-id request gate — the old RepackRequestGate
    /// migrated verbatim: a 1.5s cooldown against reliable-packet re-fires,
    /// a 120s replay window for OBSERVED request ids (a cooldown-rejected id
    /// is still recorded, so the same reliable packet cannot replay after the
    /// cooldown), hard capacity caps (fail-closed), and the Quarantine state
    /// for half-commit risks (RestoreFailed / post-admission crash) that only
    /// a disconnect, an inventory resync or a manual release lifts. All
    /// operations are main-thread (engine transaction context); the tables
    /// are cleared at the module stop boundary via ResetForGeneration (静态
    /// 表绑功能代际,禁止跨功能泄漏). Engine-coupled by design (Time/
    /// realtime clock) — this class is production-path only; the red chain
    /// exercises the seams around it.
    /// </summary>
    internal static class LirRepackGate
    {
        private static readonly Dictionary<ulong, float> NextAllowedAt = new Dictionary<ulong, float>();

        /// <summary>The highest OBSERVED request id per player inside the replay window — observed, not executed.</summary>
        private static readonly Dictionary<ulong, ReplayEntry> HighestObservedRequestIds = new Dictionary<ulong, ReplayEntry>();

        /// <summary>Quarantined steam ids: any TryAcquire refuses until ReleaseQuarantine.</summary>
        private static readonly HashSet<ulong> Quarantined = new HashSet<ulong>();

        private readonly struct ReplayEntry
        {
            internal readonly ulong RequestId;
            internal readonly long SeenAtTicks;

            internal ReplayEntry(ulong requestId, long seenAtTicks)
            {
                RequestId = requestId;
                SeenAtTicks = seenAtTicks;
            }
        }

        /// <summary>
        /// Tries to acquire a cooldown slot. false = cooling down / quarantined
        /// / capacity cap hit / invalid request id. Main thread only.
        /// </summary>
        internal static bool TryAcquire(ulong id, ulong requestId, out int retryAfterMs)
        {
            float now = Time.realtimeSinceStartup;
            long nowTicks = System.Diagnostics.Stopwatch.GetTimestamp();

            if (id == 0UL || requestId == 0UL)
            {
                retryAfterMs = -3;
                return false;
            }
            PurgeExpiredReplayEntries(nowTicks);
            if (HighestObservedRequestIds.TryGetValue(id, out var previous) && requestId <= previous.RequestId)
            {
                retryAfterMs = -3;
                return false;
            }

            // Record the new id BEFORE the quarantine/cooldown decision — a
            // rejected request must never become replayable on a later packet.
            if (!HighestObservedRequestIds.ContainsKey(id) && HighestObservedRequestIds.Count >= ReloadRuntimePolicy.GateMaxEntries)
            {
                LirRuntime.LogError("[RepackGate] CRITICAL: replay 表已达上限 " + ReloadRuntimePolicy.GateMaxEntries + "，拒绝新请求");
                retryAfterMs = -1;
                return false;
            }
            HighestObservedRequestIds[id] = new ReplayEntry(requestId, nowTicks);

            // 0) Quarantine first (highest priority — only a release lifts it).
            if (Quarantined.Contains(id))
            {
                LirRuntime.LogError("[RepackGate] sender=" + id + " 被 Quarantine，拒绝请求（需库存重同步或人工解除）");
                retryAfterMs = -2;
                return false;
            }

            // 1) Existing entry's cooldown (a cooling player is never dropped
            // by the capacity check for new ids).
            if (NextAllowedAt.TryGetValue(id, out float next) && now < next)
            {
                retryAfterMs = Mathf.CeilToInt((next - now) * 1000f);
                return false;
            }

            // 2) Drop expired cooldowns.
            PurgeExpired(now);

            // 3) Capacity cap only for NEW ids (fail-closed).
            var isNewEntry = !NextAllowedAt.ContainsKey(id);
            if (isNewEntry && NextAllowedAt.Count >= ReloadRuntimePolicy.GateMaxEntries)
            {
                LirRuntime.LogError("[RepackGate] CRITICAL: 字典已达上限 " + ReloadRuntimePolicy.GateMaxEntries + "，拒绝新请求（疑似清理逻辑失效）");
                retryAfterMs = -1;
                return false;
            }
            NextAllowedAt[id] = now + ReloadRuntimePolicy.CooldownSeconds;
            retryAfterMs = 0;
            return true;
        }

        /// <summary>Half-commit-risk isolation until a disconnect / resync / manual release. Main thread only.</summary>
        internal static void Quarantine(ulong id, string reason)
        {
            if (Quarantined.Add(id))
            {
                LirRuntime.LogError("[RepackGate] CRITICAL: sender=" + id + " 已 Quarantine，reason=" + reason);
            }
        }

        /// <summary>Releases a quarantine (disconnect / verified resync / admin action). Main thread only.</summary>
        internal static void ReleaseQuarantine(ulong id)
        {
            Quarantined.Remove(id);
            NextAllowedAt.Remove(id);
            HighestObservedRequestIds.Remove(id);
        }

        /// <summary>The stop-boundary wipe: the gate is feature-generation state, nothing survives it.</summary>
        internal static void ResetForGeneration()
        {
            NextAllowedAt.Clear();
            HighestObservedRequestIds.Clear();
            Quarantined.Clear();
        }

        private static void PurgeExpired(float now)
        {
            List<ulong> expired = null;
            foreach (var kvp in NextAllowedAt)
            {
                if (kvp.Value <= now)
                {
                    if (expired == null) expired = new List<ulong>();
                    expired.Add(kvp.Key);
                }
            }
            if (expired == null) return;
            foreach (var id in expired)
            {
                NextAllowedAt.Remove(id);
            }
        }

        /// <summary>The replay window purge must NOT ride the cooldown purge: the 1.5s cooldown must not clear the replay defense.</summary>
        private static void PurgeExpiredReplayEntries(long nowTicks)
        {
            var maxAgeTicks = (long)(System.Diagnostics.Stopwatch.Frequency * ReloadRuntimePolicy.ReplayWindowSeconds);
            List<ulong> expired = null;
            foreach (var pair in HighestObservedRequestIds)
            {
                if (nowTicks - pair.Value.SeenAtTicks > maxAgeTicks)
                {
                    if (expired == null) expired = new List<ulong>();
                    expired.Add(pair.Key);
                }
            }
            if (expired == null) return;
            foreach (var id in expired)
            {
                HighestObservedRequestIds.Remove(id);
            }
        }
    }
}
