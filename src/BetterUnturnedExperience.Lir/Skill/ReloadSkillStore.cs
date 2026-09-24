using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>换弹技能账的世界/玩家/角色作用域。</summary>
    internal readonly struct ReloadSkillScopeKey : IEquatable<ReloadSkillScopeKey>
    {
        internal readonly string ServerId;
        internal readonly ulong SteamId;
        internal readonly byte CharacterId;
        internal readonly string MapName;

        internal ReloadSkillScopeKey(string serverId, ulong steamId, byte characterId, string mapName)
        {
            ServerId = Normalize(serverId);
            SteamId = steamId;
            CharacterId = characterId;
            MapName = Normalize(mapName);
        }

        internal bool IsValid
        {
            get { return ServerId.Length != 0 && SteamId != 0UL && MapName.Length != 0; }
        }

        public bool Equals(ReloadSkillScopeKey other)
        {
            return SteamId == other.SteamId
                && CharacterId == other.CharacterId
                && string.Equals(ServerId, other.ServerId, StringComparison.Ordinal)
                && string.Equals(MapName, other.MapName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ReloadSkillScopeKey && Equals((ReloadSkillScopeKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(ServerId);
                hash = hash * 31 + SteamId.GetHashCode();
                hash = hash * 31 + CharacterId.GetHashCode();
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(MapName);
                return hash;
            }
        }

        internal static string Normalize(string raw)
        {
            return raw == null ? string.Empty : raw.Trim();
        }

        public override string ToString()
        {
            return ServerId + "|" + SteamId + "|" + CharacterId + "|" + MapName;
        }
    }

    /// <summary>一条等级账：世界×玩家×原版角色槽×地图 → 0..2。</summary>
    internal readonly struct ReloadSkillRecord
    {
        internal readonly ReloadSkillScopeKey Scope;
        internal readonly byte Level;
        internal readonly string LegacyCharacterKey;

        internal ReloadSkillRecord(ReloadSkillScopeKey scope, byte level)
            : this(scope, level, null)
        {
        }

        internal ReloadSkillRecord(ReloadSkillScopeKey scope, byte level, string legacyCharacterKey)
        {
            Scope = scope;
            Level = level;
            LegacyCharacterKey = legacyCharacterKey;
        }

        // Legacy test-fixture shape only. Production identity never uses display names.
        internal ReloadSkillRecord(ulong steamId, string legacyCharacterKey, byte level)
            : this(new ReloadSkillScopeKey(string.Empty, steamId, 0, string.Empty), level,
                ReloadSkillScopeKey.Normalize(legacyCharacterKey))
        {
        }

        internal ulong SteamId { get { return Scope.SteamId; } }
        internal string CharKey { get { return LegacyCharacterKey ?? string.Empty; } }
    }

    /// <summary>持久化出口：生产=自有作用域文件适配器，测试=内存假件。</summary>
    internal interface IReloadSkillPersistence
    {
        bool TryLoad(out List<ReloadSkillRecord> records, out string error);
        bool TrySave(IReadOnlyList<ReloadSkillRecord> records, out string error);
    }

    /// <summary>
    /// DEV-V7-04 等级账。生产读写只接受 ReloadSkillScopeKey；旧全局账不进入本表。
    /// 兼容重载仅服务已有 V5 纯域夹具，不被引擎身份解析使用。
    /// </summary>
    internal sealed class ReloadSkillStore
    {
        private readonly Dictionary<ReloadSkillScopeKey, byte> byScope = new Dictionary<ReloadSkillScopeKey, byte>();
        private readonly Dictionary<ulong, Dictionary<string, byte>> legacyBySteam = new Dictionary<ulong, Dictionary<string, byte>>();

        internal byte GetLevel(ReloadSkillScopeKey scope)
        {
            byte level;
            return byScope.TryGetValue(scope, out level) ? level : (byte)0;
        }

        internal bool TrySetLevel(ReloadSkillScopeKey scope, byte level)
        {
            if (!scope.IsValid || level > ReloadSkillPolicy.MaxSkillLevel) return false;
            byScope[scope] = level;
            return true;
        }

        // V5 fixture compatibility; not used by production identity or persistence.
        internal static string NormalizeCharKey(string raw)
        {
            return ReloadSkillScopeKey.Normalize(raw);
        }

        internal byte GetLevel(ulong steamId, string legacyCharacterKey)
        {
            Dictionary<string, byte> labels;
            if (!legacyBySteam.TryGetValue(steamId, out labels)) return 0;
            byte level;
            return labels.TryGetValue(ReloadSkillScopeKey.Normalize(legacyCharacterKey), out level) ? level : (byte)0;
        }

        internal bool TrySetLevel(ulong steamId, string legacyCharacterKey, byte level)
        {
            if (steamId == 0UL || level > ReloadSkillPolicy.MaxSkillLevel) return false;
            Dictionary<string, byte> labels;
            if (!legacyBySteam.TryGetValue(steamId, out labels))
            {
                labels = new Dictionary<string, byte>(StringComparer.Ordinal);
                legacyBySteam[steamId] = labels;
            }
            labels[ReloadSkillScopeKey.Normalize(legacyCharacterKey)] = level;
            return true;
        }

        internal List<ReloadSkillRecord> Snapshot()
        {
            var keys = new List<ReloadSkillScopeKey>(byScope.Keys);
            keys.Sort(Compare);
            var records = new List<ReloadSkillRecord>(keys.Count);
            for (var i = 0; i < keys.Count; i++)
                records.Add(new ReloadSkillRecord(keys[i], byScope[keys[i]]));
            var legacySteamIds = new List<ulong>(legacyBySteam.Keys);
            legacySteamIds.Sort();
            for (var i = 0; i < legacySteamIds.Count; i++)
            {
                var labels = legacyBySteam[legacySteamIds[i]];
                var names = new List<string>(labels.Keys);
                names.Sort(StringComparer.Ordinal);
                for (var j = 0; j < names.Count; j++)
                    records.Add(new ReloadSkillRecord(legacySteamIds[i], names[j], labels[names[j]]));
            }
            return records;
        }

        internal int Restore(IEnumerable<ReloadSkillRecord> records, out int rejected)
        {
            rejected = 0;
            var applied = 0;
            if (records == null) return 0;
            foreach (var record in records)
            {
                if (record.Scope.ServerId.Length == 0 && record.Scope.MapName.Length == 0)
                {
                    if (!TrySetLevel(record.Scope.SteamId, record.LegacyCharacterKey, record.Level)) rejected++;
                    else applied++;
                    continue;
                }
                if (!record.Scope.IsValid || !TrySetLevel(record.Scope, record.Level)) rejected++;
                else applied++;
            }
            return applied;
        }

        private static int Compare(ReloadSkillScopeKey left, ReloadSkillScopeKey right)
        {
            var result = string.CompareOrdinal(left.ServerId, right.ServerId);
            if (result != 0) return result;
            result = left.SteamId.CompareTo(right.SteamId);
            if (result != 0) return result;
            result = left.CharacterId.CompareTo(right.CharacterId);
            return result != 0 ? result : string.CompareOrdinal(left.MapName, right.MapName);
        }
    }
}
