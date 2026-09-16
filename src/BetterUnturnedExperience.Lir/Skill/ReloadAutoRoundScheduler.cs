using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 运行状态层：2 级自动压弹的待压表。每玩家至多一条目（新一轮
    /// 手动成功替换旧条目——「双击成功后再自动压一轮」=每成功一轮）；到点条目
    /// 先摘除再触发（异常同样消耗——无重试风暴），触发与否的重检在回调方
    /// （等级仍允许/同枪同匣指纹/玩家可解析——不满足=取消，不强制操作）。
    /// Stop 即清：本表不过功能代际。时钟由调用方注入（与冷却窗同一秒源）。
    /// </summary>
    internal sealed class ReloadAutoRoundScheduler
    {
        private struct Entry
        {
            internal object Fingerprint;
            internal double DueAtSeconds;
        }

        private readonly Dictionary<ulong, Entry> byPlayer = new Dictionary<ulong, Entry>();

        internal int PendingCount { get { return byPlayer.Count; } }

        internal void Schedule(ulong steamId, object fingerprint, double dueAtSeconds)
        {
            if (steamId == 0UL) return;
            byPlayer[steamId] = new Entry { Fingerprint = fingerprint, DueAtSeconds = dueAtSeconds };
        }

        /// <summary>到点条目逐个摘除→回调（tryFire 返回与否都不复现=一轮为限）。
        /// 回调异常隔离：吞进诊断，条目照常消耗。</summary>
        internal void Tick(double nowSeconds, Func<ulong, object, bool> tryFire)
        {
            if (byPlayer.Count == 0) return;
            List<ulong> due = null;
            foreach (var pair in byPlayer)
            {
                if (pair.Value.DueAtSeconds <= nowSeconds) (due ??= new List<ulong>()).Add(pair.Key);
            }
            if (due == null) return;
            for (var i = 0; i < due.Count; i++)
            {
                Entry entry;
                if (!byPlayer.TryGetValue(due[i], out entry) || entry.DueAtSeconds > nowSeconds) continue;
                byPlayer.Remove(due[i]);
                try { tryFire(due[i], entry.Fingerprint); }
                catch (Exception error)
                {
                    LirRuntime.LogError("[ReloadSkill] 自动压弹触发异常（条目已消耗）: " + error.Message);
                }
            }
        }

        internal void ResetForGeneration()
        {
            byPlayer.Clear();
        }
    }
}
