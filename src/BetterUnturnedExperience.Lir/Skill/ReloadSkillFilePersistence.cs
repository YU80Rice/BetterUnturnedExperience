using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V7-04 作用域账文件。文件仍位于宿主设置根之下，但按 serverID×地图分家；
    /// 旧 better-inplace-reload.skill-levels.dat 永不读取、不迁移。
    /// 行式 `serverId|steamId|characterId|mapName|level`。
    /// </summary>
    internal sealed class ReloadSkillFilePersistence : IReloadSkillPersistence
    {
        internal const string ScopeDirectoryName = "in-place-reload";
        internal const string ScopeSubdirectoryName = "skill-scopes";
        internal const char FieldSeparator = '|';
        private const string TempSuffix = ".tmp";
        private const int MaxLineChars = 512;

        private readonly string root;
        private readonly string path;
        private readonly bool rootMode;

        // Compatibility/test constructor: reads and writes one explicit new-format file.
        internal ReloadSkillFilePersistence(string path)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            root = null;
            rootMode = false;
        }

        // Production constructor: one existing settings root, many scope files.
        internal ReloadSkillFilePersistence(string settingsRoot, bool productionRoot)
        {
            if (string.IsNullOrEmpty(settingsRoot)) throw new ArgumentException("设置根不能为空", nameof(settingsRoot));
            root = Path.Combine(settingsRoot, ScopeDirectoryName, ScopeSubdirectoryName);
            path = null;
            rootMode = productionRoot;
        }

        internal ReloadSkillFilePersistence(string settingsRoot, string serverId, string mapName)
            : this(Path.Combine(settingsRoot, ScopeDirectoryName, ScopeSubdirectoryName,
                ScopeFileName(serverId, mapName)))
        {
        }

        internal string PathForTests { get { return path; } }

        internal static string ScopeFileName(string serverId, string mapName)
        {
            var server = SafePart(serverId);
            var map = SafePart(mapName);
            return server + "_" + map + "_" + StableHash(serverId + "\n" + mapName) + ".dat";
        }

        public bool TryLoad(out List<ReloadSkillRecord> records, out string error)
        {
            records = new List<ReloadSkillRecord>();
            error = null;
            try
            {
                if (rootMode)
                {
                    if (!Directory.Exists(root)) return true;
                    foreach (var file in Directory.GetFiles(root, "*.dat"))
                        ReadFile(file, records, ref error);
                    return true;
                }
                if (!File.Exists(path)) return true;
                ReadFile(path, records, ref error);
                return true;
            }
            catch (Exception readError)
            {
                error = readError.Message;
                return false;
            }
        }

        public bool TrySave(IReadOnlyList<ReloadSkillRecord> records, out string error)
        {
            error = null;
            try
            {
                if (rootMode)
                {
                    var grouped = new Dictionary<string, List<ReloadSkillRecord>>(StringComparer.Ordinal);
                    for (var i = 0; i < records.Count; i++)
                    {
                        var record = records[i];
                        if (!record.Scope.IsValid) continue;
                        var file = Path.Combine(root, ScopeFileName(record.Scope.ServerId, record.Scope.MapName));
                        List<ReloadSkillRecord> bucket;
                        if (!grouped.TryGetValue(file, out bucket))
                        {
                            bucket = new List<ReloadSkillRecord>();
                            grouped[file] = bucket;
                        }
                        bucket.Add(record);
                    }
                    Directory.CreateDirectory(root);
                    foreach (var pair in grouped) WriteFile(pair.Key, pair.Value);
                    return true;
                }
                WriteFile(path, records);
                return true;
            }
            catch (Exception saveError)
            {
                error = saveError.Message;
                return false;
            }
        }

        private static void ReadFile(string file, List<ReloadSkillRecord> records, ref string error)
        {
            var skipped = 0;
            foreach (var line in File.ReadLines(file, Encoding.UTF8))
            {
                if (string.IsNullOrEmpty(line)) continue;
                var parsed = ParseLine(line);
                if (parsed.HasValue) records.Add(parsed.Value);
                else skipped++;
            }
            if (skipped > 0) error = "跳过旧格式或坏行 " + skipped;
        }

        private static void WriteFile(string file, IReadOnlyList<ReloadSkillRecord> records)
        {
            var directory = System.IO.Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var builder = new StringBuilder();
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (!record.Scope.IsValid) continue;
                builder.Append(Sanitize(record.Scope.ServerId)).Append(FieldSeparator)
                    .Append(record.Scope.SteamId.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(FieldSeparator)
                    .Append(record.Scope.CharacterId.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(FieldSeparator)
                    .Append(Sanitize(record.Scope.MapName)).Append(FieldSeparator)
                    .Append(((int)record.Level).ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Append('\n');
            }
            var temp = file + TempSuffix;
            File.WriteAllText(temp, builder.ToString(), Encoding.UTF8);
            if (File.Exists(file)) File.Delete(file);
            File.Move(temp, file);
        }

        private static Nullable<ReloadSkillRecord> ParseLine(string line)
        {
            if (line.Length > MaxLineChars) return null;
            var fields = line.Split(FieldSeparator);
            if (fields.Length != 5) return null;
            if (fields[0].Length == 0 || fields[3].Length == 0) return null;
            if (!ulong.TryParse(fields[1], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var steamId) || steamId == 0UL) return null;
            if (!byte.TryParse(fields[2], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var characterId)) return null;
            if (!int.TryParse(fields[4], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var level)
                || level < 0 || level > ReloadSkillPolicy.MaxSkillLevel) return null;
            var scope = new ReloadSkillScopeKey(fields[0], steamId, characterId, fields[3]);
            return scope.IsValid ? new ReloadSkillRecord(scope, (byte)level) : (Nullable<ReloadSkillRecord>)null;
        }

        private static string Sanitize(string raw)
        {
            var normalized = ReloadSkillScopeKey.Normalize(raw);
            if (normalized.Length == 0) return normalized;
            var builder = new StringBuilder(normalized.Length);
            for (var i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                builder.Append(c == FieldSeparator || c == '\r' || c == '\n' ? '~' : c);
            }
            return builder.ToString();
        }

        private static string SafePart(string raw)
        {
            var normalized = ReloadSkillScopeKey.Normalize(raw);
            if (normalized.Length == 0) return "empty";
            var builder = new StringBuilder(normalized.Length);
            for (var i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }
            return builder.ToString();
        }

        private static string StableHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(16);
                for (var i = 0; i < 8; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
