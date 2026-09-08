using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// DEV-V2-21: the tidy fault circuit re-scoped to BUE connection generations,
// migrated from the retired standalone plugin (author: YU80Rice, MIT;
// attribution docs/third-party/LaunchInventoryTidy-attribution.md). Frozen
// semantics (spec「LIT：设置、熔断与夹具」): a connection (new session
// generation) opens THIS session's fault scope; a disconnect / generation
// change closes the old scope and clears the in-memory TEMPORARY state while
// the DISK-PERSISTED statistics survive (the feature-private JSON key
// structure is unchanged and stays the single source of truth — never copied
// into a second one). The old SteamP2PFriends.BeginScope("p2p") caller is
// gone: the scope context (map/slot) resolves through the injectable
// provider (production: Provider.map + Characters.selected; tests: fixed
// values), no external type is referenced.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>The world-scoped persistence context (map name + save slot).</summary>
    internal struct LitFaultScopeContext
    {
        public string MapName;
        public int SaveSlot;
        public LitFaultScopeContext(string mapName, int saveSlot) { MapName = mapName; SaveSlot = saveSlot; }
    }

    internal sealed class LitTidyFaultScopeBook
    {
        internal struct FaultRecord
        {
            public ulong SteamId;
            public string Reason;
            public DateTime OpenedAt;
            public bool Temporary; // true = temporary (clears at scope close; the migrated plugin called this "restoreVerified"), false = persistent (disk)
            public ulong ScopeGeneration; // the connection generation this fault belongs to (R4-Spec: supersession removes only the dying generation's temps)
        }

        private readonly object sync = new object();
        private readonly string scopeDirectory;
        private readonly Func<LitFaultScopeContext> contextProvider;
        private readonly Dictionary<ulong, FaultRecord> records = new Dictionary<ulong, FaultRecord>();
        private readonly Dictionary<ulong, ulong> peerScopes = new Dictionary<ulong, ulong>();
        private ulong scopeGeneration;
        private string scopeFilePath;
        private string currentFileKey;

        /// <summary>Global degraded state: a failed persistence write refuses ALL tidy until an explicit recovery — fail closed.</summary>
        internal bool Degraded { get; private set; }
        internal ulong ScopeGeneration { get { lock (sync) return scopeGeneration; } }
        internal bool ScopeActive { get { lock (sync) return peerScopes.Count > 0; } }
        internal string ScopeFilePath { get { lock (sync) return scopeFilePath; } }

        internal LitTidyFaultScopeBook(string scopeDirectory, Func<LitFaultScopeContext> contextProvider)
        {
            if (string.IsNullOrWhiteSpace(scopeDirectory)) throw new ArgumentException("scope directory is required", nameof(scopeDirectory));
            this.scopeDirectory = scopeDirectory;
            this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        }

        /// <summary>
        /// Connected / new connection generation for ONE peer → open that
        /// peer's scope. A changed world context (map/slot) switches the
        /// persistence file and reloads it as the authority; the same world
        /// keeps the file binding. The same (peer, generation) re-opening is
        /// a no-op.
        /// </summary>
        internal void OpenPeerScope(ulong peer, ulong connectionGeneration)
        {
            lock (sync)
            {
                if (peerScopes.TryGetValue(peer, out var openGen) && openGen == connectionGeneration) return;
                peerScopes[peer] = connectionGeneration;
                scopeGeneration = connectionGeneration;
                var filePath = ResolveScopeFilePath();
                if (!string.Equals(filePath, scopeFilePath, StringComparison.Ordinal))
                {
                    // World switch: the NEW file is the persistent authority.
                    scopeFilePath = filePath;
                    records.Clear();
                    LoadFromDiskLocked();
                }
                LitRuntime.LogInfo("[TidyFault] peer scope 已开启（peer=" + peer + ", generation=" + connectionGeneration + "）");
            }
        }

        /// <summary>
        /// One connection generation dies (disconnect OR supersession). When
        /// the dying generation IS the peer's open scope, the scope closes:
        /// the temporary fault clears and the disk-persistent statistics
        /// reload. When a SUCCESSOR already owns the scope (R4-Spec race: the
        /// old generation's drop can run after the successor's discovery),
        /// only the DYING generation's temporary fault is removed — the
        /// successor's scope stays open and other generations are untouched.
        /// </summary>
        internal void ClosePeerScope(ulong peer, ulong dyingGeneration)
        {
            lock (sync)
            {
                var ownsScope = peerScopes.TryGetValue(peer, out var openGen) && openGen == dyingGeneration;
                if (records.TryGetValue(peer, out var record) && record.Temporary && record.ScopeGeneration == dyingGeneration)
                {
                    records.Remove(peer);
                }
                if (!ownsScope) return;
                peerScopes.Remove(peer);
                // The disk is the persistent authority: re-sync THIS peer
                // from the file, so a temporary record that overwrote a
                // persistent one in memory cannot erase the disk's history.
                ReloadPeerFromDiskLocked(peer);
                LitRuntime.LogInfo("[TidyFault] peer scope 已关闭（peer=" + peer + ", generation=" + dyingGeneration + "，临时态已清，磁盘持久统计保留）");
            }
        }

        private void ReloadPeerFromDiskLocked(ulong peer)
        {
            try
            {
                if (string.IsNullOrEmpty(scopeFilePath) || !File.Exists(scopeFilePath)) return;
                var scratch = new Dictionary<ulong, FaultRecord>();
                if (!ParseJsonRecords(File.ReadAllText(scopeFilePath), scratch)) return;
                if (scratch.TryGetValue(peer, out var diskRecord) && !diskRecord.Temporary)
                {
                    records[peer] = diskRecord;
                }
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[TidyFault] peer 持久记录回同步异常: " + error.Message);
            }
        }

        /// <summary>Function-generation teardown: everything in memory drops; the disk statistics are NEVER touched here.</summary>
        internal void DropAll()
        {
            lock (sync)
            {
                peerScopes.Clear();
                records.Clear();
            }
        }

        /// <summary>Whether the peer may tidy: a degraded persistence layer or a closed scope refuses (fail closed).</summary>
        internal bool IsAllowed(ulong peer)
        {
            lock (sync)
            {
                if (Degraded || !peerScopes.ContainsKey(peer)) return false;
                return !records.ContainsKey(peer);
            }
        }

        /// <summary>Host-test observability: whether the peer currently carries an in-memory fault record.</summary>
        internal bool IsFaulted(ulong peer)
        {
            lock (sync) return records.ContainsKey(peer);
        }

        /// <summary>
        /// Opens a fault for the peer. temporary=true is a TEMPORARY
        /// fault (cleared at the next scope close); false is PERSISTENT and
        /// writes to disk immediately — a failed write degrades the book.
        /// </summary>
        internal void Open(ulong peer, string reason, bool temporary)
        {
            lock (sync)
            {
                peerScopes.TryGetValue(peer, out var scopeGen);
                records[peer] = new FaultRecord { SteamId = peer, Reason = reason ?? "unknown", OpenedAt = DateTime.UtcNow, Temporary = temporary, ScopeGeneration = scopeGen };
                LitRuntime.LogError("[TidyFault] peer " + peer + " 已熔断（reason=" + (reason ?? "unknown") + ", temporary=" + temporary + "）");
                if (!temporary && !SaveLocked())
                {
                    Degraded = true;
                    LitRuntime.LogError("[TidyFault] 持久熔断写盘失败，进入全局降级（所有整理拒绝，直到显式恢复）");
                }
            }
        }

        /// <summary>Explicit recovery path (admin/explicit verification only): clears the record and rewrites disk when it was persistent.</summary>
        internal bool TryClose(ulong peer)
        {
            lock (sync)
            {
                if (!records.TryGetValue(peer, out var record)) return false;
                records.Remove(peer);
                if (!record.Temporary && !SaveLocked())
                {
                    Degraded = true;
                    LitRuntime.LogError("[TidyFault] 解除持久熔断后写盘失败，进入全局降级");
                }
                return true;
            }
        }

        /// <summary>Explicit recovery of a degraded persistence layer: reload from disk; success clears the degraded flag.</summary>
        internal bool TryRecoverFromDegraded()
        {
            lock (sync)
            {
                scopeFilePath = ResolveScopeFilePath();
                records.Clear();
                var ok = LoadFromDiskLocked();
                if (ok) Degraded = false;
                return ok;
            }
        }

        // ── persistence (feature-private JSON, key structure unchanged) ──

        private bool SaveLocked()
        {
            try
            {
                if (string.IsNullOrEmpty(scopeFilePath)) return false;
                Directory.CreateDirectory(scopeDirectory);
                var tmp = scopeFilePath + ".tmp";
                WriteJsonLocked(tmp);
                if (File.Exists(scopeFilePath))
                {
                    var bak = scopeFilePath + ".bak";
                    File.Replace(tmp, scopeFilePath, bak);
                }
                else
                {
                    File.Move(tmp, scopeFilePath);
                }
                return true;
            }
            catch (Exception error)
            {
                LitRuntime.LogError("[TidyFault] 持久统计写盘异常: " + error.Message);
                return false;
            }
        }

        private bool LoadFromDiskLocked()
        {
            try
            {
                if (string.IsNullOrEmpty(scopeFilePath) || !File.Exists(scopeFilePath)) return true; // first boot: empty scope
                var ok = ParseJsonLocked(File.ReadAllText(scopeFilePath));
                if (!ok)
                {
                    // R10-Standards BLOCKING: a PARSE failure is as fail-closed
                    // as an IO failure — a corrupted persistent file must never
                    // silently open as an empty (allow-all) scope; the degraded
                    // gate refuses everyone until the explicit recovery path
                    // re-reads it.
                    LitRuntime.LogError("[TidyFault] 持久统计文件解析失败（可能损坏），进入全局降级（所有整理拒绝，直到显式恢复）");
                    Degraded = true;
                }
                return ok;
            }
            catch (Exception error)
            {
                LitRuntime.LogError("[TidyFault] 持久统计读盘异常，保持空 scope（fail-closed 需显式恢复）: " + error.Message);
                Degraded = true;
                return false;
            }
        }

        private void WriteJsonLocked(string path)
        {
            var sb = new StringBuilder(256 + records.Count * 128);
            sb.Append("{\"formatVersion\":2,\"records\":[");
            var first = true;
            foreach (var pair in records.Values)
            {
                if (pair.Temporary) continue; // only PERSISTENT records reach the disk
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"steamId\":").Append(pair.SteamId.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"reason\":\"").Append(EscapeJson(pair.Reason)).Append('"');
                sb.Append(",\"openedAt\":\"").Append(pair.OpenedAt.ToString("o", CultureInfo.InvariantCulture)).Append('"');
                sb.Append('}');
            }
            sb.Append("]}");
            File.WriteAllText(path, sb.ToString());
        }

        private bool ParseJsonLocked(string text)
        {
            return ParseJsonRecords(text, records);
        }

        private static bool ParseJsonRecords(string text, Dictionary<ulong, FaultRecord> target)
        {
            try
            {
                var index = text.IndexOf("\"records\"", StringComparison.Ordinal);
                if (index < 0) return false;
                index = text.IndexOf('[', index);
                if (index < 0) return false;
                while (true)
                {
                    var objStart = text.IndexOf('{', index);
                    if (objStart < 0) break;
                    var objEnd = text.IndexOf('}', objStart);
                    if (objEnd < 0) return false;
                    var obj = text.Substring(objStart, objEnd - objStart + 1);
                    if (!TryParseRecord(obj, out var record)) return false;
                    // Disk records are persistent by definition (format v2).
                    record.Temporary = false;
                    target[record.SteamId] = record;
                    index = objEnd + 1;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryParseRecord(string obj, out FaultRecord record)
        {
            record = default(FaultRecord);
            if (!TryReadUlong(obj, "steamId", out var steamId)) return false;
            record.SteamId = steamId;
            record.Reason = TryReadString(obj, "reason");
            if (!TryReadDateTime(obj, "openedAt", out var openedAt)) return false;
            record.OpenedAt = openedAt;
            return true;
        }

        private static bool TryReadUlong(string obj, string key, out ulong value)
        {
            value = 0;
            var needle = "\"" + key + "\":";
            var start = obj.IndexOf(needle, StringComparison.Ordinal);
            if (start < 0) return false;
            start += needle.Length;
            var end = start;
            while (end < obj.Length && obj[end] >= '0' && obj[end] <= '9') end++;
            return ulong.TryParse(obj.Substring(start, end - start), NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private static string TryReadString(string obj, string key)
        {
            var needle = "\"" + key + "\":\"";
            var start = obj.IndexOf(needle, StringComparison.Ordinal);
            if (start < 0) return null;
            start += needle.Length;
            var sb = new StringBuilder();
            while (start < obj.Length)
            {
                var c = obj[start];
                if (c == '\\' && start + 1 < obj.Length)
                {
                    sb.Append(obj[start + 1]);
                    start += 2;
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
                start++;
            }
            return sb.ToString();
        }

        private static bool TryReadDateTime(string obj, string key, out DateTime value)
        {
            value = default(DateTime);
            var raw = TryReadString(obj, key);
            if (raw == null) return false;
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (c == '\\' || c == '"') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }

        private string ResolveScopeFilePath()
        {
            LitFaultScopeContext context;
            try { context = contextProvider(); }
            catch (Exception) { context = default(LitFaultScopeContext); }
            var mapName = context.MapName;
            if (string.IsNullOrWhiteSpace(mapName))
            {
                LitRuntime.LogWarning("[TidyFault] scope 上下文不可用（map 为空），持久统计文件未绑定");
                return null;
            }
            var safe = new StringBuilder(mapName.Length);
            foreach (var c in mapName)
            {
                safe.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }
            var safeMap = safe.ToString().Trim('_');
            if (safeMap.Length == 0) safeMap = "map";
            if (safeMap.Length > 48) safeMap = safeMap.Substring(0, 48);
            string mapHash;
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(mapName));
                var hex = new StringBuilder(16);
                for (int i = 0; i < 8; i++) hex.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                mapHash = hex.ToString();
            }
            var fileKey = safeMap + "_" + mapHash + "_slot" + context.SaveSlot.ToString(CultureInfo.InvariantCulture);
            if (string.Equals(currentFileKey, fileKey, StringComparison.Ordinal) && scopeFilePath != null) return scopeFilePath;
            currentFileKey = fileKey;
            return Path.Combine(scopeDirectory, "lit-persistent-faults_" + fileKey + ".json");
        }
    }
}
