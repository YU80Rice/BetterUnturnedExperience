using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        /// <summary>宿主测试时钟（秒）。null = 生产走 Time.realtimeSinceStartup。
        /// 闸门方法体仍含 Unity 类型引用，但 Now 注入后冷却/回放分支可在无引擎
        /// 进程断言（08 U3DS 自动轮红测需要）。</summary>
        internal static Func<float> NowSecondsForTests;

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
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float ReadEngineNowSeconds()
        {
            return Time.realtimeSinceStartup;
        }

        internal static bool TryAcquire(ulong id, ulong requestId, out int retryAfterMs)
        {
            return TryAcquire(id, requestId, false, out retryAfterMs);
        }

        /// <summary>主机发起（2 级自动轮）：跳过客户端 requestId 回放比较，仍吃
        /// 1.5s 技术闸与隔离。U3DS 探针 #5：自动轮 NextRequestId 常小于客机上次
        /// 手动 id → 回放闸 rejected=1，到点提交零成交。</summary>
        internal static bool TryAcquireHostInitiated(ulong id, out int retryAfterMs)
        {
            return TryAcquire(id, 0UL, true, out retryAfterMs);
        }

        private static bool TryAcquire(ulong id, ulong requestId, bool hostInitiated, out int retryAfterMs)
        {
            float now = NowSecondsForTests != null ? NowSecondsForTests() : ReadEngineNowSeconds();
            long nowTicks = System.Diagnostics.Stopwatch.GetTimestamp();

            if (id == 0UL || (!hostInitiated && requestId == 0UL))
            {
                retryAfterMs = -3;
                return false;
            }
            PurgeExpiredReplayEntries(nowTicks);
            if (!hostInitiated
                && HighestObservedRequestIds.TryGetValue(id, out var previous)
                && requestId <= previous.RequestId)
            {
                retryAfterMs = -3;
                return false;
            }

            // Record the new id BEFORE the quarantine/cooldown decision — a
            // rejected request must never become replayable on a later packet.
            // 主机发起不写入回放表（自动轮 id 与客机手动 id 不在同一序号空间）。
            if (!hostInitiated)
            {
                if (!HighestObservedRequestIds.ContainsKey(id) && HighestObservedRequestIds.Count >= ReloadRuntimePolicy.GateMaxEntries)
                {
                    LirRuntime.LogError("[RepackGate] CRITICAL: replay 表已达上限 " + ReloadRuntimePolicy.GateMaxEntries + "，拒绝新请求");
                    retryAfterMs = -1;
                    return false;
                }
                HighestObservedRequestIds[id] = new ReplayEntry(requestId, nowTicks);
            }

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
                retryAfterMs = (int)((next - now) * 1000f) + 1;
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
