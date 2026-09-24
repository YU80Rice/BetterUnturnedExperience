using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V7-03 运行状态层：2 级被动压弹周期。候选玩家首次达到 2 级时
    /// 从当前时钟起算固定等待；每次尝试结束后重新等待同一间隔，不追赶错过的拍。
    /// 候选同步负责等级降级、玩家离场和死亡/不可解析清窗；Stop 即清。
    /// </summary>
    internal sealed class ReloadAutoRoundScheduler
    {
        private readonly Dictionary<ulong, double> dueByPlayer = new Dictionary<ulong, double>();

        internal int PendingCount { get { return dueByPlayer.Count; } }

        internal void Sync(IEnumerable<ulong> steamIds, Func<ulong, byte> levelFor,
            Func<ulong, bool> isAvailable, double nowSeconds)
        {
            if (levelFor == null) throw new ArgumentNullException(nameof(levelFor));
            if (isAvailable == null) throw new ArgumentNullException(nameof(isAvailable));
            var eligible = new HashSet<ulong>();
            if (steamIds != null)
            {
                foreach (var steamId in steamIds)
                {
                    if (steamId == 0UL || levelFor(steamId) < ReloadSkillPolicy.MaxSkillLevel
                        || !isAvailable(steamId)) continue;
                    eligible.Add(steamId);
                    if (!dueByPlayer.ContainsKey(steamId))
                        dueByPlayer[steamId] = nowSeconds + ReloadSkillPolicy.AutoRoundDelaySeconds;
                }
            }
            var stale = new List<ulong>();
            foreach (var pair in dueByPlayer)
            {
                if (!eligible.Contains(pair.Key)) stale.Add(pair.Key);
            }
            for (var i = 0; i < stale.Count; i++) dueByPlayer.Remove(stale[i]);
        }

        internal void TickPassive(double nowSeconds, Action<ulong> tryFire)
        {
            if (tryFire == null) throw new ArgumentNullException(nameof(tryFire));
            if (dueByPlayer.Count == 0) return;
            var due = new List<ulong>();
            foreach (var pair in dueByPlayer)
            {
                if (pair.Value <= nowSeconds) due.Add(pair.Key);
            }
            for (var i = 0; i < due.Count; i++)
            {
                var steamId = due[i];
                if (!dueByPlayer.ContainsKey(steamId)) continue;
                try { tryFire(steamId); }
                catch (Exception error)
                {
                    LirRuntime.LogError("[ReloadSkill] 被动压弹触发异常（本拍已结束）: " + error.Message);
                }
                dueByPlayer[steamId] = nowSeconds + ReloadSkillPolicy.AutoRoundDelaySeconds;
            }
        }

        internal void ResetForGeneration()
        {
            dueByPlayer.Clear();
        }
    }
}
