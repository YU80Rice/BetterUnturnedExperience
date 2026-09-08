using System;
using System.Collections.Generic;

// DEV-V2-21: client-side tidy session state, migrated from the retired
// standalone plugin (author: YU80Rice, MIT; attribution
// docs/third-party/LaunchInventoryTidy-attribution.md) and re-keyed for BUE:
// the server-issued challenge token is stored PER CONNECTION GENERATION, so
// a reconnect (fresh session id) cannot read, send under, or accept
// responses for a stale token — the TOCTOU-safe atomic read and the
// fail-closed RNG rule carry over unchanged.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// The client's session token gate: holds the SERVER-ISSUED challenge
    /// tokens per connection generation (R8/R10: the construction-era
    /// temporary token is gone — the send gate only ever accepts
    /// server-issued tokens, so the pre-challenge state is simply the empty
    /// token set). A send is only allowed when the CURRENT generation holds
    /// a non-zero token (atomic read — never two separate reads).
    /// </summary>
    internal sealed class LitClientSessionToken
    {
        // R8-Standards: the constructor-era "temporary token" was dead weight
        // — the send gate only ever accepts SERVER-ISSUED tokens, so the
        // construction-time RNG (and its fail-closed flag) is gone; the
        // pre-challenge refusal is the natural empty-token state.
        private readonly object sync = new object();
        private readonly Dictionary<ulong, ulong> serverTokens = new Dictionary<ulong, ulong>();
        private uint nextRequestId;

        /// <summary>Atomically reads the CURRENT generation's server-issued token; false = not ready, the caller must not send.</summary>
        internal bool TryGetServerIssuedToken(ulong connectionGeneration, out ulong token)
        {
            lock (sync)
            {
                token = 0UL;
                return serverTokens.TryGetValue(connectionGeneration, out token) && token != 0UL;
            }
        }

        internal void ReplaceWithServerChallenge(ulong connectionGeneration, ulong token)
        {
            if (token == 0UL) return;
            lock (sync)
            {
                serverTokens[connectionGeneration] = token;
            }
        }

        /// <summary>A generation change drops the old token immediately (fail closed against replays across connections).</summary>
        internal void DropGeneration(ulong connectionGeneration)
        {
            lock (sync) serverTokens.Remove(connectionGeneration);
        }

        internal void DropAll()
        {
            lock (sync) serverTokens.Clear();
        }

        /// <summary>Client request ids are monotonic within this client identity (never zero).</summary>
        internal uint NextRequestId()
        {
            lock (sync)
            {
                nextRequestId++;
                if (nextRequestId == 0) nextRequestId = 1;
                return nextRequestId;
            }
        }
    }

    /// <summary>
    /// Shared expiring-entry set (R3/R5 S2: one Purge/Clear/Key
    /// implementation for the client state tables).
    /// </summary>
    internal sealed class LitExpiringKeySet<TValue>
    {
        private struct ExpiringEntry
        {
            public TValue Value;
            public DateTime CreatedAt;
        }

        private readonly object sync = new object();
        private readonly Dictionary<string, ExpiringEntry> entries = new Dictionary<string, ExpiringEntry>(StringComparer.Ordinal);
        private readonly TimeSpan ttl;

        internal LitExpiringKeySet(TimeSpan ttl) { this.ttl = ttl; }

        internal void Set(string key, TValue value)
        {
            lock (sync)
            {
                PurgeExpired();
                entries[key] = new ExpiringEntry { Value = value, CreatedAt = DateTime.UtcNow };
            }
        }

        internal bool TryGet(string key, out TValue value)
        {
            lock (sync)
            {
                PurgeExpired();
                if (entries.TryGetValue(key, out var entry))
                {
                    value = entry.Value;
                    return true;
                }
                value = default(TValue);
                return false;
            }
        }

        internal bool Contains(string key)
        {
            TValue ignored;
            return TryGet(key, out ignored);
        }

        internal void Remove(string key)
        {
            lock (sync) entries.Remove(key);
        }

        internal void Clear()
        {
            lock (sync) entries.Clear();
        }

        /// <summary>Removes every key matching the predicate (under the lock); returns the removal count.</summary>
        internal int RemoveWhere(Predicate<string> match)
        {
            lock (sync)
            {
                PurgeExpired();
                List<string> dead = null;
                foreach (var key in entries.Keys)
                {
                    if (match(key)) (dead ??= new List<string>()).Add(key);
                }
                if (dead == null) return 0;
                for (int i = 0; i < dead.Count; i++) entries.Remove(dead[i]);
                return dead.Count;
            }
        }

        private void PurgeExpired()
        {
            var now = DateTime.UtcNow;
            List<string> dead = null;
            foreach (var pair in entries)
            {
                if (now - pair.Value.CreatedAt > ttl) (dead ??= new List<string>()).Add(pair.Key);
            }
            if (dead == null) return;
            for (int i = 0; i < dead.Count; i++) entries.Remove(dead[i]);
        }
    }

    /// <summary>The (generation, token, requestId) key shared by the client state tables.</summary>
    internal static class LitTidyClientKeys
    {
        internal static string Request(ulong connectionGeneration, ulong token, uint requestId)
        {
            return connectionGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture) + "@" +
                   token.ToString(System.Globalization.CultureInfo.InvariantCulture) + "@" +
                   requestId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Client pending request table: only responses matching a pending
    /// (generation, token, requestId) are accepted. Entries expire.
    /// </summary>
    internal sealed class LitClientPendingTable
    {
        internal struct PendingEntry
        {
            public byte Page;
            public TidyMode Mode;
            public bool SortDescending;
        }

        internal static readonly TimeSpan EntryTtl = TimeSpan.FromSeconds(30);

        private readonly LitExpiringKeySet<PendingEntry> pending = new LitExpiringKeySet<PendingEntry>(EntryTtl);

        internal void SetPending(ulong connectionGeneration, ulong token, uint requestId, byte page, TidyMode mode, bool sortDescending)
        {
            pending.Set(LitTidyClientKeys.Request(connectionGeneration, token, requestId),
                new PendingEntry { Page = page, Mode = mode, SortDescending = sortDescending });
        }

        internal bool IsPending(ulong connectionGeneration, ulong token, uint requestId)
        {
            return pending.Contains(LitTidyClientKeys.Request(connectionGeneration, token, requestId));
        }

        internal void ClearPending(ulong connectionGeneration, ulong token, uint requestId)
        {
            pending.Remove(LitTidyClientKeys.Request(connectionGeneration, token, requestId));
        }

        internal void ClearAll()
        {
            pending.Clear();
        }
    }

    /// <summary>
    /// Client hotkey-result wait state: registered when the flow ack goes
    /// out, consumed by the server's result message — a result for a request
    /// the client is not waiting on is rejected (late/duplicate/forbidden).
    /// </summary>
    internal sealed class LitClientHotkeyResultWait
    {
        internal static readonly TimeSpan WaitTtl = TimeSpan.FromSeconds(10);

        private readonly LitExpiringKeySet<byte> waiting = new LitExpiringKeySet<byte>(WaitTtl);

        internal void Register(ulong connectionGeneration, ulong token, uint requestId)
        {
            waiting.Set(LitTidyClientKeys.Request(connectionGeneration, token, requestId), 1);
        }

        internal bool IsWaiting(ulong connectionGeneration, ulong token, uint requestId)
        {
            return waiting.Contains(LitTidyClientKeys.Request(connectionGeneration, token, requestId));
        }

        internal void Clear(ulong connectionGeneration, ulong token, uint requestId)
        {
            waiting.Remove(LitTidyClientKeys.Request(connectionGeneration, token, requestId));
        }

        internal void ClearAll()
        {
            waiting.Clear();
        }
    }
}
