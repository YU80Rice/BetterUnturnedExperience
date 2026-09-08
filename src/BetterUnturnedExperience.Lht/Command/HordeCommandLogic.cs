using System.Collections.Generic;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the pure /horde command logic (cooldown, status formatting)
    /// lifted out of the old CommandHorde so the engine shell stays thin and
    /// the rules are host-testable. The cooldown clock comes in as a
    /// parameter (production passes Time.realtimeSinceStartup — the monotonic
    /// game clock, the migrated choice). The cooldown table is generation
    /// state: ResetForGeneration runs at the module stop boundary (spec「静态表
    /// 绑功能代际」).
    /// </summary>
    internal static class HordeCommandLogic
    {
        /// <summary>Per-steam-id request cooldown — an implementation constant, not a setting.</summary>
        internal const float RequestCooldownSeconds = 1.5f;

        internal const string CooldownMessage = "<color=#888888>[尸潮监视] 请求过快，请稍后。</color>";
        internal const string NoHordeMessage = "<color=#888888>[尸潮监视] 当前无活跃尸潮</color>";

        private const int CleanupEveryRequests = 64;
        private static readonly Dictionary<ulong, float> LastAcceptedAt = new Dictionary<ulong, float>();
        private static int requestCount;

        /// <summary>Accepts (and records) a request, or refuses it inside the cooldown
        /// window with the cooldown reply. Every 64 requests the expired entries
        /// are swept (the unbounded-growth guard, migrated).</summary>
        internal static bool TryAcceptRequest(ulong steamId, float now, out string reply)
        {
            if (LastAcceptedAt.TryGetValue(steamId, out float previous) &&
                now - previous < RequestCooldownSeconds)
            {
                reply = CooldownMessage;
                return false;
            }

            LastAcceptedAt[steamId] = now;
            if ((++requestCount % CleanupEveryRequests) == 0)
            {
                foreach (ulong id in new List<ulong>(LastAcceptedAt.Keys))
                    if (now - LastAcceptedAt[id] >= RequestCooldownSeconds * 4f)
                        LastAcceptedAt.Remove(id);
            }

            reply = null;
            return true;
        }

        /// <summary>The status reply (the old format, migrated verbatim).</summary>
        internal static string FormatStatus(HordeBeaconView info)
        {
            int killed = info.Killed;
            int total = info.TotalZombies;
            int remaining = total - killed;
            if (remaining < 0) remaining = 0;

            return
                "<color=#ffcc00>[尸潮监视]</color> " +
                "<color=#ff6666>" + info.LocationName + "</color> " +
                "<color=#aaaaaa>(发起者: <color=#ffffff>" + info.OwnerName + "</color>)</color> | " +
                "<color=#88ff88>剩余 " + remaining + " / 总数 " + total + "</color>";
        }

        /// <summary>The generation boundary: the cooldown table drops.</summary>
        internal static void ResetForGeneration()
        {
            LastAcceptedAt.Clear();
            requestCount = 0;
        }
    }
}
