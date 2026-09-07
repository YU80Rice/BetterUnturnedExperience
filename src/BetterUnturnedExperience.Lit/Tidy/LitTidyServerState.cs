using System;
using System.Collections.Generic;
using System.Security.Cryptography;

// DEV-V2-21: server-side tidy request admission state, migrated from the
// retired standalone plugin's admission chain (author: YU80Rice, MIT;
// attribution docs/third-party/LaunchInventoryTidy-attribution.md) and
// re-keyed for BUE: every state table binds the FEATURE generation (module
// instance) and the CONNECTION generation (session id), so a reconnect with
// a fresh session generation invalidates old tokens, ledger entries and
// leases by construction — the old SteamP2P scope no longer exists.
// Behavior baseline = the 08 kit: token-only validation, idempotent ledger
// lookup before requestId reservation, Received entries never evicted,
// lease mismatch refuses release.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// R5-Standards S3: the ONE source of the state-table key prefixes —
    /// the peer/peer@generation scans in the session book, the ledger and
    /// the pending-restore book all match through these helpers, so a key
    /// rule drift can never leave a dead generation's state behind.
    /// </summary>
    internal static class LitStateKeys
    {
        internal static string PeerPrefix(ulong peer)
        {
            return peer.ToString(System.Globalization.CultureInfo.InvariantCulture) + "@";
        }

        internal static string GenerationPrefix(ulong peer, ulong connectionGeneration)
        {
            return PeerPrefix(peer) + connectionGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture) + "@";
        }

        internal static bool StartsWith(string key, string prefix)
        {
            return key.StartsWith(prefix, StringComparison.Ordinal);
        }

        /// <summary>The full transaction compound key (peer@generation@token@requestId).</summary>
        internal static string TransactionKey(ulong peer, ulong connectionGeneration, ulong token, uint requestId)
        {
            return GenerationPrefix(peer, connectionGeneration) +
                   token.ToString(System.Globalization.CultureInfo.InvariantCulture) + "@" +
                   requestId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// R6-Standards S2: the ONE prefix-scan removal loop — every state
        /// table's Drop* calls this under ITS OWN lock, so the scan body
        /// itself has a single source.
        /// </summary>
        internal static int RemoveMatching<TValue>(Dictionary<string, TValue> map, string prefix)
        {
            List<string> dead = null;
            foreach (var key in map.Keys)
            {
                if (StartsWith(key, prefix)) (dead ??= new List<string>()).Add(key);
            }
            if (dead == null) return 0;
            for (int i = 0; i < dead.Count; i++) map.Remove(dead[i]);
            return dead.Count;
        }
    }

    /// <summary>
    /// Per-peer session tokens. The server issues one 64-bit token per
    /// (peer, connection generation) — the MSG_SESSION_CHALLENGE payload.
    /// RNG failure fails closed (no timestamp downgrade), matching the
    /// migrated fail-closed rule.
    /// </summary>
    internal sealed class LitServerSessionBook
    {
        private struct SessionRecord
        {
            public ulong Token;
            public uint HighestRequestId;
        }

        private readonly object sync = new object();
        private readonly Dictionary<string, SessionRecord> sessions = new Dictionary<string, SessionRecord>(StringComparer.Ordinal);

        internal bool TryBeginSession(ulong peer, ulong connectionGeneration, out ulong token)
        {
            token = GenerateTokenOrFail();
            var key = SessionKey(peer, connectionGeneration);
            lock (sync)
            {
                sessions[key] = new SessionRecord { Token = token, HighestRequestId = 0 };
            }
            return true;
        }

        /// <summary>Token-only validation (no requestId monotonicity here — the idempotent ledger lookup runs first).</summary>
        internal bool ValidateToken(ulong peer, ulong connectionGeneration, ulong token)
        {
            if (token == 0UL) return false;
            lock (sync)
            {
                return sessions.TryGetValue(SessionKey(peer, connectionGeneration), out var record) && record.Token == token;
            }
        }

        /// <summary>Reserves a monotonically increasing requestId for NEW requests only (the caller checked the ledger first).</summary>
        internal bool TryReserveRequestId(ulong peer, ulong connectionGeneration, ulong token, uint requestId)
        {
            if (token == 0UL || requestId == 0) return false;
            lock (sync)
            {
                var key = SessionKey(peer, connectionGeneration);
                if (!sessions.TryGetValue(key, out var record) || record.Token != token) return false;
                if (requestId <= record.HighestRequestId) return false;
                record.HighestRequestId = requestId;
                sessions[key] = record;
                return true;
            }
        }

        /// <summary>One connection generation dies (supersession/disconnect): only THAT generation's token record goes.</summary>
        internal void DropSession(ulong peer, ulong connectionGeneration)
        {
            lock (sync) sessions.Remove(SessionKey(peer, connectionGeneration));
        }

        /// <summary>Peer gone entirely (no successor): every session generation for the peer dies — old tokens fail closed.</summary>
        internal void DropPeer(ulong peer)
        {
            lock (sync) LitStateKeys.RemoveMatching(sessions, LitStateKeys.PeerPrefix(peer));
        }

        internal void DropAll()
        {
            lock (sync) sessions.Clear();
        }

        private static string SessionKey(ulong peer, ulong connectionGeneration)
        {
            // R7: the key RULE lives in LitStateKeys (single source) — the
            // prefix helpers and the full-key builders cannot drift apart.
            return LitStateKeys.GenerationPrefix(peer, connectionGeneration);
        }

        private static ulong GenerateTokenOrFail()
        {
            var bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var token = BitConverter.ToUInt64(bytes, 0);
            return token == 0UL ? 1UL : token;
        }
    }

    /// <summary>
    /// Request ledger: (peer, generation, token, requestId) → outcome cache.
    /// Two independent bounds (R3-Standards B5 wording fix): CAPACITY
    /// eviction never removes a Received entry (in-flight protection — the
    /// evictable set is terminal entries only), while the TTL is the
    /// ANTI-REPLAY WINDOW: any entry (Received included) stops answering
    /// lookups after it, exactly like the migrated 60s window — an expired
    /// request may be re-admitted, and the per-peer lease still serializes
    /// any transaction that is actually executing.
    /// </summary>
    internal sealed class LitRequestLedger
    {
        internal enum RequestState : byte { Received = 0, Committed = 1, Failed = 2, Expired = 3 }

        internal struct LedgerEntry
        {
            public uint RequestId;
            public RequestState State;
            public TidyCommitResult Result;
            public List<LitNewPositionMapping> Mappings;
        }

        private struct Record
        {
            public LedgerEntry Entry;
            public DateTime RecordedAt;
        }

        internal const int MaxEntriesPerPeer = 64;
        internal static readonly TimeSpan EntryTtl = TimeSpan.FromSeconds(60);

        private readonly object sync = new object();
        private readonly Dictionary<string, List<Record>> entries = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        internal bool TryLookup(ulong peer, ulong connectionGeneration, ulong token, uint requestId, out LedgerEntry entry)
        {
            var key = LedgerKey(peer, connectionGeneration, token);
            lock (sync)
            {
                entry = default(LedgerEntry);
                if (!entries.TryGetValue(key, out var list)) return false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Entry.RequestId != requestId) continue;
                    if (Expired(list[i])) continue;
                    entry = list[i].Entry;
                    return true;
                }
                return false;
            }
        }

        /// <summary>Capacity check + eviction + insert merged (Received entries survive; capacity full of Received rejects).</summary>
        internal bool TryCreateReceived(ulong peer, ulong connectionGeneration, ulong token, uint requestId)
        {
            var key = LedgerKey(peer, connectionGeneration, token);
            lock (sync)
            {
                if (!entries.TryGetValue(key, out var list))
                {
                    list = new List<Record>(MaxEntriesPerPeer);
                    entries[key] = list;
                }
                PurgeExpired(list);
                if (list.Count >= MaxEntriesPerPeer)
                {
                    var victim = -1;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].Entry.State != RequestState.Received) { victim = i; break; }
                    }
                    if (victim < 0) return false;
                    list.RemoveAt(victim);
                }
                list.Add(new Record
                {
                    RecordedAt = DateTime.UtcNow,
                    Entry = new LedgerEntry { RequestId = requestId, State = RequestState.Received, Result = TidyCommitResult.Rejected, Mappings = null },
                });
                return true;
            }
        }

        internal void MarkResult(ulong peer, ulong connectionGeneration, ulong token, uint requestId, RequestState state, TidyCommitResult result, List<LitNewPositionMapping> mappings)
        {
            var key = LedgerKey(peer, connectionGeneration, token);
            lock (sync)
            {
                if (!entries.TryGetValue(key, out var list)) return;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Entry.RequestId != requestId) continue;
                    var updated = list[i].Entry;
                    updated.State = state;
                    updated.Result = result;
                    updated.Mappings = mappings;
                    list[i] = new Record { RecordedAt = list[i].RecordedAt, Entry = updated };
                    return;
                }
            }
        }

        internal void MarkFailed(ulong peer, ulong connectionGeneration, ulong token, uint requestId)
        {
            MarkResult(peer, connectionGeneration, token, requestId, RequestState.Failed, TidyCommitResult.Rejected, null);
        }

        /// <summary>One connection generation dies: its ledger entries (peer@gen@token keys) go; other generations stay.</summary>
        internal void DropGeneration(ulong peer, ulong connectionGeneration)
        {
            lock (sync) LitStateKeys.RemoveMatching(entries, LitStateKeys.GenerationPrefix(peer, connectionGeneration));
        }

        internal void DropPeer(ulong peer)
        {
            lock (sync) LitStateKeys.RemoveMatching(entries, LitStateKeys.PeerPrefix(peer));
        }

        internal void DropAll()
        {
            lock (sync) entries.Clear();
        }

        private static bool Expired(Record record) { return DateTime.UtcNow - record.RecordedAt > EntryTtl; }

        private static void PurgeExpired(List<Record> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (Expired(list[i])) list.RemoveAt(i);
            }
        }

        private static string LedgerKey(ulong peer, ulong connectionGeneration, ulong token)
        {
            return LitStateKeys.TransactionKey(peer, connectionGeneration, token, 0);
        }
    }

    /// <summary>
    /// Per-peer operation lease bound to (peer, token, requestId): one tidy
    /// transaction in flight per peer; a duplicate (same key) is silent, a
    /// different requestId is BusyDifferent; release requires the matching
    /// requestId (mismatch-refusal keeps one transaction from freeing
    /// another's lease).
    /// </summary>
    internal sealed class LitPlayerLeaseGate
    {
        internal enum AcquireResult : byte { Acquired = 0, BusyDuplicate = 1, BusyDifferent = 2 }

        private struct Lease
        {
            public ulong Token;
            public uint RequestId;
        }

        private readonly object sync = new object();
        private readonly Dictionary<ulong, Lease> leases = new Dictionary<ulong, Lease>();

        internal AcquireResult TryAcquire(ulong peer, ulong token, uint requestId)
        {
            if (requestId == 0) return AcquireResult.BusyDifferent;
            lock (sync)
            {
                if (leases.TryGetValue(peer, out var existing))
                {
                    return existing.Token == token && existing.RequestId == requestId
                        ? AcquireResult.BusyDuplicate
                        : AcquireResult.BusyDifferent;
                }
                leases[peer] = new Lease { Token = token, RequestId = requestId };
                return AcquireResult.Acquired;
            }
        }

        internal void Release(ulong peer, uint requestId)
        {
            lock (sync)
            {
                if (!leases.TryGetValue(peer, out var existing)) return;
                if (existing.RequestId != requestId) return; // mismatch refusal
                leases.Remove(peer);
            }
        }

        internal bool IsHeld(ulong peer)
        {
            lock (sync) return leases.ContainsKey(peer);
        }

        internal void DropPeer(ulong peer)
        {
            lock (sync) leases.Remove(peer);
        }

        internal void DropAll()
        {
            lock (sync) leases.Clear();
        }
    }

    /// <summary>
    /// The single-lock atomic admission decision (the migrated template-B
    /// fix): token-only validation → idempotent ledger lookup (InFlight
    /// silent / Cached replay) → lease check (BusyDifferent creates no
    /// ledger entry) → requestId reservation (new requests only) → Received
    /// insert (never evicting Received) → lease acquire. CancelNew is the
    /// compensating transaction for an enqueue failure or a stop drain.
    /// </summary>
    internal sealed class LitAdmissionGate
    {
        internal enum AdmissionKind : byte { New = 0, InFlight = 1, Cached = 2, BusyDifferent = 3, Rejected = 4 }

        private readonly object gate = new object();
        private readonly LitServerSessionBook sessions;
        private readonly LitRequestLedger ledger;
        private readonly LitPlayerLeaseGate leases;

        internal LitAdmissionGate(LitServerSessionBook sessions, LitRequestLedger ledger, LitPlayerLeaseGate leases)
        {
            this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            this.leases = leases ?? throw new ArgumentNullException(nameof(leases));
        }

        internal AdmissionKind TryAdmit(ulong peer, ulong connectionGeneration, ulong token, uint requestId, out LitRequestLedger.LedgerEntry cached)
        {
            lock (gate)
            {
                cached = default(LitRequestLedger.LedgerEntry);
                if (!sessions.ValidateToken(peer, connectionGeneration, token)) return AdmissionKind.Rejected;
                if (ledger.TryLookup(peer, connectionGeneration, token, requestId, out var existing))
                {
                    cached = existing;
                    return existing.State == LitRequestLedger.RequestState.Received ? AdmissionKind.InFlight : AdmissionKind.Cached;
                }
                if (leases.IsHeld(peer)) return AdmissionKind.BusyDifferent;
                if (!sessions.TryReserveRequestId(peer, connectionGeneration, token, requestId)) return AdmissionKind.Rejected;
                if (!ledger.TryCreateReceived(peer, connectionGeneration, token, requestId)) return AdmissionKind.Rejected;
                var acquired = leases.TryAcquire(peer, token, requestId);
                if (acquired != LitPlayerLeaseGate.AcquireResult.Acquired)
                {
                    ledger.MarkFailed(peer, connectionGeneration, token, requestId);
                    return AdmissionKind.Rejected;
                }
                return AdmissionKind.New;
            }
        }

        internal void CancelNew(ulong peer, ulong connectionGeneration, ulong token, uint requestId)
        {
            lock (gate)
            {
                leases.Release(peer, requestId);
                ledger.MarkFailed(peer, connectionGeneration, token, requestId);
            }
        }
    }
}
