using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>一条等级账：玩家(steamId)×角色(characterKey) → 0..2。</summary>
    internal readonly struct ReloadSkillRecord
    {
        internal readonly ulong SteamId;
        internal readonly string CharKey;
        internal readonly byte Level;

        internal ReloadSkillRecord(ulong steamId, string charKey, byte level)
        {
            SteamId = steamId;
            CharKey = charKey;
            Level = level;
        }
    }

    /// <summary>持久化出口（纯接口：生产=自有文件适配器，测试=内存假件）。
    /// 自有文件不是 *.bue-settings 设置文档（等级是玩家进度不是偏好），
    /// 更不是原版 Skill[][]（票面：不写入原版技能数组）。</summary>
    internal interface IReloadSkillPersistence
    {
        bool TryLoad(out List<ReloadSkillRecord> records, out string error);
        bool TrySave(IReadOnlyList<ReloadSkillRecord> records, out string error);
    }

    /// <summary>
    /// DEV-V5-07 进度层（三层之「玩家进度」）：按玩家/角色的换弹技能等级账。
    /// engine-free 纯内存表 + 可注入持久化出口；键归一（trim/空串统一）保证
    /// 同一角色只有一条账。新档不送等级（读缺省=0）。Restore 对坏记录
    /// fail-closed（steamId=0 或等级>Max 整条丢弃并计数——脏账不进权威）。
    /// 主线程纪律由调用方（模块）保证；本类不做线程防御。
    /// </summary>
    internal sealed class ReloadSkillStore
    {
        private readonly Dictionary<ulong, Dictionary<string, byte>> bySteam = new Dictionary<ulong, Dictionary<string, byte>>();

        internal static string NormalizeCharKey(string raw)
        {
            return raw == null ? string.Empty : raw.Trim();
        }

        internal byte GetLevel(ulong steamId, string charKey)
        {
            Dictionary<string, byte> chars;
            if (!bySteam.TryGetValue(steamId, out chars)) return 0;
            byte level;
            return chars.TryGetValue(NormalizeCharKey(charKey), out level) ? level : (byte)0;
        }

        /// <summary>唯一写入口：范围外一律拒（无三级账落得下来）。</summary>
        internal bool TrySetLevel(ulong steamId, string charKey, byte level)
        {
            if (steamId == 0UL || level > ReloadSkillPolicy.MaxSkillLevel) return false;
            Dictionary<string, byte> chars;
            if (!bySteam.TryGetValue(steamId, out chars))
            {
                chars = new Dictionary<string, byte>(StringComparer.Ordinal);
                bySteam[steamId] = chars;
            }
            chars[NormalizeCharKey(charKey)] = level;
            return true;
        }

        /// <summary>确定性快照（steamId 升序→键升序），持久化文件可 diff。</summary>
        internal List<ReloadSkillRecord> Snapshot()
        {
            var steamIds = new List<ulong>(bySteam.Keys);
            steamIds.Sort();
            var records = new List<ReloadSkillRecord>();
            for (var i = 0; i < steamIds.Count; i++)
            {
                var chars = bySteam[steamIds[i]];
                var keys = new List<string>(chars.Keys);
                keys.Sort(StringComparer.Ordinal);
                for (var j = 0; j < keys.Count; j++)
                {
                    records.Add(new ReloadSkillRecord(steamIds[i], keys[j], chars[keys[j]]));
                }
            }
            return records;
        }

        /// <summary>Start 期重建：坏记录整条丢弃（返回应用数与拒绝数）。</summary>
        internal int Restore(IEnumerable<ReloadSkillRecord> records, out int rejected)
        {
            rejected = 0;
            var applied = 0;
            if (records == null) return 0;
            foreach (var record in records)
            {
                if (record.SteamId == 0UL || record.CharKey == null || record.Level > ReloadSkillPolicy.MaxSkillLevel)
                {
                    rejected++;
                    continue;
                }
                TrySetLevel(record.SteamId, record.CharKey, record.Level);
                applied++;
            }
            return applied;
        }
    }
}
