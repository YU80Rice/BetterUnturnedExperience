using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;

// DEV-V2-21: the LIT multiplayer orchestration — the feature-private tidy
// protocol carried over the BUE named channel (FeatureId). The message kinds (five migrated +
// the DEV-V5-03 container pair), the admission chain and the challenge flow are migrated from the
// retired standalone plugin (author: YU80Rice, MIT; attribution
// docs/third-party/LaunchInventoryTidy-attribution.md); the transport is the
// frozen IBueNetworkApi surface (directional subscribe, session-addressed
// reliable sends with explicit results). The server identity authority is
// the SESSION (PeerSteamId), never a payload field; the engine-facing work
// (player resolution, transaction, hotkey restore, convergence) sits behind
// ILitTidyAuthority so the full chain runs on the loopback test transport
// with a fake authority and zero Harmony patches.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>The engine-facing tidy work the service orchestrates (seam for tests).</summary>
    internal interface ILitTidyAuthority
    {
        /// <summary>Client side: captures the local hotkey snapshots to upload with the request.</summary>
        List<HotkeySnapshot> CaptureClientHotkeys();

        /// <summary>Server main thread: the authoritative transaction for one admitted request.</summary>
        LitAuthorityResult ExecuteServerTidy(LitTidyRequestContext request);

        /// <summary>Server main thread: restores/clears the requesting peer's hotkeys after its flow ack.</summary>
        LitHotkeyRestoreResult RestoreServerHotkeys(ulong peerSteamId, List<HotkeyRestoreEntry> entries);

        /// <summary>Client main thread: one bounded convergence probe (mapping targets present at the new coordinates).</summary>
        bool VerifyClientConvergence(List<LitNewPositionMapping> mappings);

        /// <summary>DEV-V5-03: server main thread — the authoritative container tidy
        /// (session re-verify → unified layout → atomic commit; every refusal
        /// names a structured reason and mutates nothing). No ack, no hotkey
        /// flow: the answer is the single result frame.</summary>
        LitContainerAuthorityResult ExecuteServerContainerTidy(LitContainerTidyRequestContext request);

        /// <summary>DEV-V5-05: server main thread — the authoritative fast-transfer
        /// recover (03 session re-verify → source identity + receiving-side unified
        /// layout → ONE two-page atomic transaction; every refusal names a
        /// structured reason and mutates NEITHER side). Like the container flow:
        /// no ack, no hotkey protocol, no TidyCompleted — the single result frame
        /// is the terminal answer (and a client-side refusal stays vanilla-silent
        /// by design — this is a recovery, not a button).</summary>
        LitContainerAuthorityResult ExecuteServerFastTransferRecover(LitFastTransferRequestContext request);
    }

    /// <summary>One admitted fast-transfer recover request, fully resolved.
    /// DEV-V5-05: the requester is the SESSION peer (never a payload field);
    /// sourcePage/x/y is where the client SAW the moved item (authority
    /// re-reads the real jar); kind+fingerprint is the 03 container-session
    /// claim of the grid on the other side of the transfer (receiving side for
    /// player→container, source side for container→player — in BOTH directions
    /// the container grid's version must match the client's mirror).</summary>
    internal sealed class LitFastTransferRequestContext
    {
        public ulong PeerSteamId;
        public ulong ConnectionGeneration;
        public ulong SessionToken;
        public uint RequestId;
        public LitContainerTidyKind Kind;
        public ulong Fingerprint;
        public byte SourcePage;
        public byte SourceX;
        public byte SourceY;
        public bool SortDescending;
    }

    /// <summary>One admitted container tidy request, fully resolved (network
    /// callback captured it). DEV-V5-03: the identity binding — requester is
    /// the SESSION peer (never a payload field), kind+fingerprint is what the
    /// requester claimed to see, and the authority re-reads both before any
    /// mutation.</summary>
    internal sealed class LitContainerTidyRequestContext
    {
        public ulong PeerSteamId;
        public ulong ConnectionGeneration;
        public ulong SessionToken;
        public uint RequestId;
        public LitContainerTidyKind Kind;
        public ulong Fingerprint;
        public bool SortDescending;
    }

    internal sealed class LitContainerAuthorityResult
    {
        public TidyOperationOutcome Outcome;
        public LitContainerTidyReason Reason;
    }

    /// <summary>One admitted tidy request, fully resolved (network callback captured it).</summary>
    internal sealed class LitTidyRequestContext
    {
        public ulong PeerSteamId;
        public ulong ConnectionGeneration;
        public ulong SessionToken;
        public uint RequestId;
        public byte Page;
        public TidyMode Mode;
        public bool SortDescending;
        public List<HotkeySnapshot> Hotkeys;
    }

    internal sealed class LitAuthorityResult
    {
        public TidyOperationOutcome Outcome;
        public List<LitNewPositionMapping> Mappings;
        public List<HotkeyRestoreEntry> RestoreEntries;

        internal static LitAuthorityResult From(TidyOperationOutcome outcome)
        {
            return new LitAuthorityResult { Outcome = outcome, Mappings = null, RestoreEntries = null };
        }
    }

    internal sealed class LitHotkeyRestoreResult
    {
        public int Restored;
        public int Verified;
        public int Cleared;
        public List<byte> FailedIndices;
    }

    /// <summary>
    /// Server-side pending hotkey restores, keyed by (peer, generation,
    /// token, requestId) with a TTL — the ack handler's compound-key lookup;
    /// an ack for an expired/unknown transaction is silently ignored.
    /// R6-Standards S1: rides the shared generic expiring set (one TTL/purge
    /// implementation across the client and server state tables).
    /// </summary>
    internal sealed class LitPendingRestoreBook
    {
        internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(10);

        private readonly LitExpiringKeySet<List<HotkeyRestoreEntry>> records = new LitExpiringKeySet<List<HotkeyRestoreEntry>>(Ttl);

        internal void Store(ulong peer, ulong generation, ulong token, uint requestId, List<HotkeyRestoreEntry> entries)
        {
            records.Set(LitStateKeys.TransactionKey(peer, generation, token, requestId), entries ?? new List<HotkeyRestoreEntry>(0));
        }

        internal bool TryGet(ulong peer, ulong generation, ulong token, uint requestId, out List<HotkeyRestoreEntry> entries)
        {
            return records.TryGet(LitStateKeys.TransactionKey(peer, generation, token, requestId), out entries);
        }

        internal void Remove(ulong peer, ulong generation, ulong token, uint requestId)
        {
            records.Remove(LitStateKeys.TransactionKey(peer, generation, token, requestId));
        }

        internal void DropGeneration(ulong peer, ulong connectionGeneration)
        {
            records.RemoveWhere(key => LitStateKeys.StartsWith(key, LitStateKeys.GenerationPrefix(peer, connectionGeneration)));
        }

        internal void DropAll()
        {
            records.Clear();
        }
    }

    /// <summary>
    /// The tidy network service. One instance per module generation: Start
    /// registers the channel and subscribes BOTH directions (half-registration
    /// failure rolls everything back), Tick reconciles the established
    /// session snapshot (session discovery is poll-based — a session found
    /// established without having been seen is treated as connected, so the
    /// module starting after a handshake still issues its challenge), and
    /// Stop tears the whole feature state down without touching the disk
    /// persistence.
    /// </summary>
    internal sealed class LitTidyNetService
    {
        internal const int ConvergenceMaxAttempts = 60;

        private static readonly FeatureId Channel = new FeatureId(LitRuntime.FeatureIdValue);

        private readonly InventoryTidyModule module;
        private readonly IBueNetworkApi network;
        private readonly ILitTidyAuthority authority;
        private readonly Func<bool> isServerRole;
        private readonly LitTidyFaultScopeBook faultBook;
        private readonly LitServerSessionBook sessions = new LitServerSessionBook();
        private readonly LitRequestLedger ledger = new LitRequestLedger();
        private readonly LitPlayerLeaseGate leases = new LitPlayerLeaseGate();
        private readonly LitAdmissionGate admission;
        private readonly LitClientSessionToken clientToken = new LitClientSessionToken();
        private readonly LitClientPendingTable clientPending = new LitClientPendingTable();
        private readonly LitClientHotkeyResultWait clientWait = new LitClientHotkeyResultWait();
        private readonly LitPendingRestoreBook pendingRestores = new LitPendingRestoreBook();
        private readonly Dictionary<ulong, IConnectionSession> liveSessions = new Dictionary<ulong, IConnectionSession>();
        private readonly HashSet<ulong> challengeFaultLogged = new HashSet<ulong>(); // R3-Standards B3: one fault line per generation
        private readonly HashSet<ulong> sessionEventsWired = new HashSet<ulong>(); // DEV-V2-25: lifecycle wiring once per generation
        private readonly List<IDisposable> subscriptionHandles = new List<IDisposable>();
        private readonly LitChallengeRearmBook challengeRearm;   // DEV-V2-25: challenge re-arm backoff (attempts)
        // DEV-V3-04: the DEV-V2-25 LitSendFailureRateLimiter RETIRED here —
        // the per-frame WARN cadence and the per-episode degradation/
        // recovery surface belong to the platform now (BueNetworkRuntime's
        // session link health: BUE-NET-002 degraded / BUE-NET-003 recovered,
        // 平台报告链路健康，功能处理业务重试). The challenge re-arm backoff
        // (LitChallengeRearmBook) stays feature-private business retry.
        private bool quiesced; // stop phase 1: reject new frames/requests, sends still work for drain compensations
        private bool stopped;  // stop phase 3: full teardown

        /// <summary>Phase-1 gate: frames are ignored, new requests refused, queued work can still compensate (send Rejected).</summary>
        private bool GateClosed
        {
            get { return stopped || quiesced; }
        }

        internal LitTidyNetService(InventoryTidyModule module, IBueNetworkApi network, ILitTidyAuthority authority, Func<bool> isServerRole, LitTidyFaultScopeBook faultBook, Func<DateTime> clock = null)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.network = network ?? throw new ArgumentNullException(nameof(network));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            this.isServerRole = isServerRole ?? throw new ArgumentNullException(nameof(isServerRole));
            this.faultBook = faultBook ?? throw new ArgumentNullException(nameof(faultBook));
            challengeRearm = new LitChallengeRearmBook(clock ?? (Func<DateTime>)(() => DateTime.UtcNow));
            admission = new LitAdmissionGate(sessions, ledger, leases);
        }

        internal bool Started { get; private set; }

        /// <summary>
        /// DEV-V3-03: the live inbound subscription handles this service owns —
        /// the module tracks them through the host lifecycle seam
        /// (IFeatureLifetime.TryTrack) so the stop boundary's reverse disposal
        /// covers them too (handle Dispose is idempotent: the module's own
        /// teardown and the host's withdrawal both run safely).
        /// </summary>
        internal IReadOnlyList<IDisposable> SubscriptionHandles
        {
            get { return new ReadOnlyCollection<IDisposable>(subscriptionHandles); }
        }

        /// <summary>
        /// Registers the channel and subscribes both directions. A failure in
        /// the SECOND subscribe is the half-registration case: every handle
        /// disposed, the channel unregistered, zero residue — the ticket's
        /// rollback rule.
        /// </summary>
        internal bool Start()
        {
            if (stopped) return false;
            var registration = network.RegisterChannel(Channel, new ContractVersion(2, 0), 1);
            if (!registration.Accepted)
            {
                LitRuntime.LogError("[TidyNet] 频道注册被拒绝（reason=" + registration.Reason + "），联机整理不可用");
                return false;
            }
            IDisposable fromClients = null;
            IDisposable fromServer = null;
            try
            {
                fromClients = network.Subscribe(Channel, ChannelDirection.FromClients, HandleServerFrame);
                fromServer = network.Subscribe(Channel, ChannelDirection.FromServer, HandleClientFrame);
            }
            catch (Exception error)
            {
                try { fromClients?.Dispose(); } catch (Exception) { }
                try { fromServer?.Dispose(); } catch (Exception) { }
                network.UnregisterChannel(Channel);
                LitRuntime.LogError("[TidyNet] 双方向订阅半途失败，已回滚注册（errorType=" + error.GetType().Name + "）");
                return false;
            }
            subscriptionHandles.Add(fromClients);
            subscriptionHandles.Add(fromServer);
            Started = true;
            LitRuntime.LogInfo("[TidyNet] 已注册 BUE 命名频道=" + Channel.Value + " 双方向处理器");
            return true;
        }

        /// <summary>
        /// Session discovery/reconciliation (poll-based, main thread): an
        /// established session never seen before is treated as connected
        /// (challenge issued / scope opened); a tracked session missing from
        /// the established snapshot is dropped — every per-peer state table
        /// clears while the disk persistence survives.
        /// </summary>
        /// <summary>Stop phase 1 — the migrated BeginQuiesce: handlers refuse, queued work may still compensate.</summary>
        internal void BeginQuiesce()
        {
            quiesced = true;
        }

        internal void Tick()
        {
            if (GateClosed) return;
            IReadOnlyList<IConnectionSession> current;
            try { current = network.Sessions ?? new IConnectionSession[0]; }
            catch (Exception) { return; }
            var seen = new HashSet<ulong>();
            for (int i = 0; i < current.Count; i++)
            {
                var session = current[i];
                if (session == null) continue;
                seen.Add(session.SessionId);
                if (!liveSessions.ContainsKey(session.SessionId))
                {
                    liveSessions[session.SessionId] = session;
                    OnSessionEstablished(session);
                }
            }
            List<ulong> dead = null;
            foreach (var pair in liveSessions)
            {
                if (!seen.Contains(pair.Key)) (dead ??= new List<ulong>()).Add(pair.Key);
            }
            if (dead == null)
            {
                DriveDueRearms();
                return;
            }
            for (int i = 0; i < dead.Count; i++)
            {
                if (liveSessions.TryGetValue(dead[i], out var dropped)) OnSessionDropped(dropped);
                else liveSessions.Remove(dead[i]);
            }
            DriveDueRearms();
        }

        /// <summary>
        /// DEV-V2-25: the re-arm driver. A backoff-parked generation stays
        /// tracked, so Tick's discovery never re-fires for it — this driver
        /// re-enters the establish path once its deadline passes. Entries are
        /// snapshotted first: the establish path may add/remove tracked
        /// sessions while it runs.
        /// </summary>
        private void DriveDueRearms()
        {
            List<ulong> due = null;
            foreach (var pair in liveSessions)
            {
                if (challengeRearm.HasPending(pair.Key) && challengeRearm.ShouldAttempt(pair.Key))
                {
                    (due ??= new List<ulong>()).Add(pair.Key);
                }
            }
            if (due == null) return;
            for (int i = 0; i < due.Count; i++)
            {
                if (liveSessions.TryGetValue(due[i], out var session)) OnSessionEstablished(session);
            }
        }

        internal void Stop()
        {
            if (stopped) return;
            stopped = true;
            for (int i = 0; i < subscriptionHandles.Count; i++)
            {
                try { subscriptionHandles[i]?.Dispose(); } catch (Exception) { }
            }
            subscriptionHandles.Clear();
            try { network.UnregisterChannel(Channel); } catch (Exception) { }
            sessions.DropAll();
            ledger.DropAll();
            leases.DropAll();
            pendingRestores.DropAll();
            faultBook.DropAll();
            clientToken.DropAll();
            clientPending.ClearAll();
            clientWait.ClearAll();
            liveSessions.Clear();
            challengeRearm.DropAll();
            sessionEventsWired.Clear();
            Started = false;
            LitRuntime.LogInfo("[TidyNet] 服务已停止（频道注销、内存态清空、磁盘持久统计保留）");
        }

        private void OnSessionEstablished(IConnectionSession session)
        {
            // R1-Spec GAP-1 fix (R6 rebuttal recorded): the lifecycle binds
            // to the SESSION events — Disconnected drives the immediate
            // cleanup, GenerationChanged the supersession. The Connected
            // event is deliberately NOT subscribed: per the frozen contract
            // (DEV-V2-16 ④ + DEV-V2-17), Connected fires exactly once AFTER
            // the handshake completes and the public Sessions snapshot is
            // ESTABLISHED-ONLY — a session becomes discoverable here only
            // after Connected has already fired, so a subscription on any
            // snapshot session could never observe a transition (dead
            // code). This discovery IS the Connected equivalent for the
            // snapshot surface: a session found established and unseen is
            // treated as connected (scope open + challenge), and the poll
            // remains the reconciliation safety net. DEV-V2-25: the wiring
            // runs ONCE per generation — the re-arm path re-enters this
            // method for the same session object, and duplicate handlers
            // would double every later drop/supersession line (the paired
            // 会话代际更替 lines in the DEV-V2-24 v6 host log).
            if (sessionEventsWired.Add(session.SessionId))
            {
                session.Disconnected += () => OnSessionEventDisconnected(session);
                session.GenerationChanged += newGeneration => OnSessionEventGenerationChanged(session, newGeneration);
            }
            if (isServerRole())
            {
                // DEV-V2-25: the re-arm backoff gate. A generation whose
                // challenge send keeps failing parks here — it stays TRACKED
                // (the dead-session poll must still see it vanish) but skips
                // the scope/book/challenge work until its backoff deadline;
                // Tick's re-arm driver re-enters this path when due. The
                // first retry stays immediate (F-A frozen contract).
                if (challengeRearm.HasPending(session.SessionId) && !challengeRearm.ShouldAttempt(session.SessionId)) return;
                // R3-Standards B3: a fault here (e.g. the RNG's fail-closed
                // throw) must not orphan the session — the generation leaves
                // the tracked set so the next Tick rediscovers and RETRIES;
                // the failure line logs once per generation.
                try
                {
                    faultBook.OpenPeerScope(session.PeerSteamId, session.SessionId);
                    if (sessions.TryBeginSession(session.PeerSteamId, session.SessionId, out var token))
                    {
                        // DEV-V2-24 F-A (real machine DEV-V2-24-20260908 P2P): a
                        // challenge delivery failure must not stick the
                        // adoption — the token never reached the client, so
                        // this generation is treated as UN-ADOPTED: the book
                        // record (orphan token) and the tracked session go,
                        // and the next re-arm re-issues scope+token+challenge.
                        // This mirrors the exception path's R3-Standards B3
                        // retry semantics; scope re-open for the same
                        // generation is a no-op. Without the rollback a single
                        // transient transport failure (one targeted send
                        // returned LocalTransportUnavailable while LIR/LHT
                        // sends on the SAME session succeeded) locked the
                        // client out of tidy for the whole session (80
                        // refusals on machine). DEV-V2-25: the re-arm is now
                        // backoff-gated (immediate first retry, then 1s→8s
                        // doubling) so sustained unavailability cannot spin
                        // one attempt per frame.
                        if (TrySendToSession(session, LitTidyWireCodec.BuildSessionChallenge(token)) != NetworkSendResult.Sent)
                        {
                            challengeRearm.NoteChallengeFailure(session.SessionId);
                            sessions.DropSession(session.PeerSteamId, session.SessionId);
                            liveSessions.Remove(session.SessionId);
                        }
                        else
                        {
                            challengeRearm.NoteChallengeSent(session.SessionId);
                        }
                    }
                }
                catch (Exception error)
                {
                    if (challengeFaultLogged.Add(session.SessionId))
                    {
                        LitRuntime.LogError("[TidyNet] 会话建立异常（generation=" + session.SessionId + "）: " + error.Message + " —— 下一拍重试");
                    }
                    liveSessions.Remove(session.SessionId);
                }
            }
        }

        /// <summary>The session's own Disconnected event: immediate cleanup for a tracked session (the poll remains the safety net).</summary>
        private void OnSessionEventDisconnected(IConnectionSession session)
        {
            if (GateClosed) return;
            if (!liveSessions.TryGetValue(session.SessionId, out var tracked) || !ReferenceEquals(tracked, session)) return;
            OnSessionDropped(session);
        }

        /// <summary>
        /// The dead session's GenerationChanged (fires when the successor
        /// establishes): the generation-scoped drop runs HERE — the old
        /// scope closes and the old generation's records go, then the
        /// successor's discovery opens its own fresh scope.
        /// </summary>
        private void OnSessionEventGenerationChanged(IConnectionSession session, ulong newGeneration)
        {
            if (GateClosed) return;
            // Supersession order (runtime): Disconnected fires on the dead
            // object FIRST, GenerationChanged SECOND — the tracking guard
            // must NOT gate here (the dead session is already dropped by the
            // Disconnected path); the generation-scoped drop is idempotent
            // and the successor adoption is the point of this event.
            LitRuntime.LogInfo("[TidyNet] 会话代际更替（generation=" + session.SessionId + " → " + newGeneration + "），旧代际状态即清");
            OnSessionDropped(session);
            AdoptSuccessor(newGeneration);
        }

        /// <summary>
        /// R10-Spec GAP fix: the successor generation is adopted AT THE
        /// SESSION EVENT (no Tick latency) — the dead generation's drop runs
        /// first, then the successor (already established when
        /// GenerationChanged fired on the dead object) enters the tracked set
        /// and receives its scope/challenge in the same beat. First-connect
        /// discovery still rides the host frame pump: the public Sessions
        /// snapshot is established-only (DEV-V2-16 ④), so a brand-new
        /// session's Connected has always already fired before any feature
        /// can see it — the Tick reconcile is the only observable channel
        /// for that side (frozen contract, documented in review-rounds.md).
        /// </summary>
        private void AdoptSuccessor(ulong newGeneration)
        {
            try
            {
                if (liveSessions.ContainsKey(newGeneration)) return;
                var snapshot = network.Sessions;
                if (snapshot == null) return;
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var successor = snapshot[i];
                    if (successor == null || successor.SessionId != newGeneration) continue;
                    liveSessions[newGeneration] = successor;
                    OnSessionEstablished(successor);
                    return;
                }
            }
            catch (Exception) { }
        }

        private void OnSessionDropped(IConnectionSession session)
        {
            liveSessions.Remove(session.SessionId);
            var peer = session.PeerSteamId;
            var generation = session.SessionId;
            // R2-Spec GAP fix: the drop is GENERATION-scoped — a disconnect
            // or a supersession (代际更替) closes the old scope (the temp
            // fault clears, the disk-persistent statistics survive and
            // reload) and removes exactly the dead generation's session/
            // ledger/pending records. The lease deliberately SURVIVES while
            // a successor may take over: it serializes the per-player tidy
            // transaction, and an in-flight transaction's finally releases it.
            faultBook.ClosePeerScope(peer, generation);
            sessions.DropSession(peer, generation);
            ledger.DropGeneration(peer, generation);
            pendingRestores.DropGeneration(peer, generation);
            challengeRearm.DropGeneration(generation);
            sessionEventsWired.Remove(generation);
            if (!isServerRole())
            {
                clientToken.DropGeneration(generation);
                clientPending.ClearAll();
                clientWait.ClearAll();
            }
            LitRuntime.LogInfo("[TidyNet] 会话代际已清（peer=" + peer + ", generation=" + generation + "），旧 scope 关闭（临时态清、磁盘持久统计保留）");
        }

        private IConnectionSession ResolveLiveSession(ulong generation)
        {
            return liveSessions.TryGetValue(generation, out var session) ? session : null;
        }

        private NetworkSendResult TrySendToSession(IConnectionSession session, byte[] payload)
        {
            if (session == null) return NetworkSendResult.NoSession;
            try
            {
                var result = network.SendToClient(Channel, session, payload, reliable: true);
                // DEV-V3-04 (V3-T5 ruling): the per-(generation, result) WARN
                // limiter and the BUE-LIT-003 degraded/recovered episodes are
                // RETIRED here — the platform's session link health now
                // raises exactly ONE BUE-NET-002 degraded line per episode
                // and ONE BUE-NET-003 recovered on delivery (电平式，不炸帧,
                // per connection generation), and the send budget answers
                // over-limit sends with an explicit Throttled result +
                // BUE-NET-001 line. LIT keeps only the business re-arm
                // backoff (the caller's LitChallengeRearmBook decides the
                // retry pace from this explicit result) — 平台报告链路健康、
                // 功能处理业务重试. No per-failure WARN line, no silent
                // swallow: the result rides back to the re-arm decision.
                return result;
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[TidyNet] 定向发送异常（errorType=" + error.GetType().Name + "）");
                return NetworkSendResult.LocalTransportUnavailable;
            }
        }

        // ── client request entry (the client role's send path) ─────

        /// <summary>
        /// The client-side tidy request. Gate order is the migrated one: a
        /// live session first, then the ATOMIC server-issued token read for
        /// THAT connection generation (no challenge → no send, no pending,
        /// the 08 baseline line), then the reliable send whose explicit
        /// result decides whether the pending may live.
        /// </summary>
        internal LitTidyRequestResult RequestTidy(byte page, TidyMode mode, bool sortDescending)
        {
            if (GateClosed || !Started) return LitTidyRequestResult.NativeFallback;
            if (isServerRole()) return LitTidyRequestResult.NativeFallback; // the host tidies locally, never self-sends
            // The request rides the RUNTIME's established snapshot (the truth,
            // never the Tick-stale tracked set): a session object here is the
            // live connection generation, so a stale generation can neither
            // send under an old token nor address a dead session.
            IConnectionSession session = null;
            try
            {
                var snapshot = network.Sessions;
                if (snapshot != null && snapshot.Count > 0) session = snapshot[0]; // the client topology has exactly one server peer
            }
            catch (Exception) { }
            if (session == null)
            {
                LitRuntime.LogInfo("[Tidy] 尚未建立 BUE 会话；本次整理请求未发送。");
                return LitTidyRequestResult.RejectedNoSession;
            }
            var generation = session.SessionId;
            if (!clientToken.TryGetServerIssuedToken(generation, out var token))
            {
                LitRuntime.LogInfo("[Tidy] 客户端尚未收到有效服务端 session challenge；本次整理请求未发送。");
                return LitTidyRequestResult.RejectedNoSession;
            }
            var requestId = clientToken.NextRequestId();
            List<HotkeySnapshot> hotkeys;
            try { hotkeys = authority.CaptureClientHotkeys(); }
            catch (Exception error)
            {
                LitRuntime.LogError("[Tidy] 快捷键快照捕获异常，放弃本次整理: " + error.Message);
                return LitTidyRequestResult.RejectedQueueClosed;
            }
            clientPending.SetPending(generation, token, requestId, page, mode, sortDescending, LitClientRequestKind.Tidy);
            NetworkSendResult sent;
            try { sent = network.SendToServer(Channel, LitTidyWireCodec.BuildTidyRequest(token, requestId, page, mode, sortDescending, hotkeys), reliable: true); }
            catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
            if (sent != NetworkSendResult.Sent)
            {
                clientPending.ClearPending(generation, token, requestId);
                LitRuntime.LogWarning("[Tidy] 整理请求发送失败（result=" + sent + "），未建立待确认。");
                return LitTidyRequestResult.RejectedSendFailed;
            }
            LitRuntime.LogInfo("[Tidy] -> 服务器: RequestTidy(reqId=" + requestId + ", page=" + page + ", mode=" + mode + ", desc=" + sortDescending + ", hotkeys=" + (hotkeys?.Count ?? 0) + ")");
            return LitTidyRequestResult.Dispatched;
        }

        /// <summary>
        /// DEV-V5-03: the client-role container tidy request. Same gates as
        /// the player-page path (live session → server-issued token → reliable
        /// send decides the pending), same request-id counter (a transaction
        /// id is unique across both request kinds within the generation), and
        /// the pending entry carries the CONTAINER marker (page 7) so only a
        /// container result can consume it. The request binds kind + content
        /// fingerprint; the requester identity is the session itself.
        /// </summary>
        internal LitTidyRequestResult RequestContainerTidy(LitContainerTidyKind kind, ulong fingerprint, bool sortDescending)
        {
            if (GateClosed || !Started) return LitTidyRequestResult.NativeFallback;
            if (isServerRole()) return LitTidyRequestResult.NativeFallback; // the host tidies locally, never self-sends
            IConnectionSession session = null;
            try
            {
                var snapshot = network.Sessions;
                if (snapshot != null && snapshot.Count > 0) session = snapshot[0]; // the client topology has exactly one server peer
            }
            catch (Exception) { }
            if (session == null)
            {
                LitRuntime.LogInfo("[Tidy容器] 尚未建立 BUE 会话；本次容器整理请求未发送。");
                return LitTidyRequestResult.RejectedNoSession;
            }
            var generation = session.SessionId;
            if (!clientToken.TryGetServerIssuedToken(generation, out var token))
            {
                LitRuntime.LogInfo("[Tidy容器] 客户端尚未收到有效服务端 session challenge；本次容器整理请求未发送。");
                return LitTidyRequestResult.RejectedNoSession;
            }
            var requestId = clientToken.NextRequestId();
            clientPending.SetPending(generation, token, requestId, LitContainerTidyExecution.MOUNT_PAGE, TidyMode.SameType, sortDescending, LitClientRequestKind.Container);
            NetworkSendResult sent;
            try { sent = network.SendToServer(Channel, LitTidyWireCodec.BuildContainerTidyRequest(token, requestId, (byte)kind, fingerprint, sortDescending), reliable: true); }
            catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
            if (sent != NetworkSendResult.Sent)
            {
                clientPending.ClearPending(generation, token, requestId);
                LitRuntime.LogWarning("[Tidy容器] 容器整理请求发送失败（result=" + sent + "），未建立待确认。");
                return LitTidyRequestResult.RejectedSendFailed;
            }
            LitRuntime.LogInfo("[Tidy容器] -> 服务器: RequestContainerTidy(reqId=" + requestId + ", kind=" + kind + ", fp=" + fingerprint + ", desc=" + sortDescending + ")");
            return LitTidyRequestResult.Dispatched;
        }

        /// <summary>
        /// DEV-V5-05: the client-role fast-transfer recover request. Same gates
        /// as the container path (live session → server-issued token → reliable
        /// send decides the pending) and the same request-id counter (unique
        /// across ALL request kinds within the generation). The entry carries
        /// the FastTransfer kind so only a message-10 result may consume it.
        /// </summary>
        internal LitTidyRequestResult RequestFastTransferRecover(LitContainerTidyKind kind, ulong fingerprint,
            byte sourcePage, byte sourceX, byte sourceY, bool sortDescending)
        {
            if (GateClosed || !Started) return LitTidyRequestResult.NativeFallback;
            if (isServerRole()) return LitTidyRequestResult.NativeFallback; // the host recovers locally, never self-sends
            if (sourcePage < HotkeySnapshotUtil.TIDYABLE_PAGE_MIN || sourcePage > LitContainerTidyExecution.MOUNT_PAGE)
                return LitTidyRequestResult.RejectedContainerUnavailable; // 0/1/8/255 never leave this process
            IConnectionSession session = null;
            try
            {
                var snapshot = network.Sessions;
                if (snapshot != null && snapshot.Count > 0) session = snapshot[0]; // the client topology has exactly one server peer
            }
            catch (Exception) { }
            if (session == null)
            {
                LitRuntime.LogInfo("[快速转移恢复] 尚未建立 BUE 会话；本次恢复请求未发送（保持原版静默）。");
                return LitTidyRequestResult.RejectedNoSession;
            }
            var generation = session.SessionId;
            if (!clientToken.TryGetServerIssuedToken(generation, out var token))
            {
                LitRuntime.LogInfo("[快速转移恢复] 客户端尚未收到有效服务端 session challenge；本次恢复请求未发送。");
                return LitTidyRequestResult.RejectedNoSession;
            }
            var requestId = clientToken.NextRequestId();
            clientPending.SetPending(generation, token, requestId, sourcePage, TidyMode.SameType, sortDescending, LitClientRequestKind.FastTransfer);
            NetworkSendResult sent;
            try { sent = network.SendToServer(Channel, LitTidyWireCodec.BuildFastTransferRequest(token, requestId, sourcePage, sourceX, sourceY, (byte)kind, fingerprint, sortDescending), reliable: true); }
            catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
            if (sent != NetworkSendResult.Sent)
            {
                clientPending.ClearPending(generation, token, requestId);
                LitRuntime.LogWarning("[快速转移恢复] 恢复请求发送失败（result=" + sent + "），未建立待确认（原版静默）。");
                return LitTidyRequestResult.RejectedSendFailed;
            }
            LitRuntime.LogInfo("[快速转移恢复] -> 服务器: RequestFastTransferRecover(reqId=" + requestId + ", source=" + sourcePage + "/" + sourceX + "/" + sourceY + ", kind=" + kind + ", fp=" + fingerprint + ")");
            return LitTidyRequestResult.Dispatched;
        }

        // ── inbound frames ──────────────────────────────────────────

        private void HandleServerFrame(IConnectionSession session, byte[] payload)
        {
            if (GateClosed) return;
            if (!LitTidyWireCodec.TryReadEnvelope(payload, out var msgType, out var body)) return;
            switch (msgType)
            {
                case LitTidyWireCodec.MsgRequestTidyV2: HandleTidyRequest(session, body); break;
                case LitTidyWireCodec.MsgHotkeyFlowAck: HandleHotkeyFlowAck(session, body); break;
                case LitTidyWireCodec.MsgRequestContainerTidy: HandleContainerTidyRequest(session, body); break; // DEV-V5-03
                case LitTidyWireCodec.MsgRequestFastTransferRecover: HandleFastTransferRequest(session, body); break; // DEV-V5-05
                default: break; // unknown kinds are ignored (feature-private set)
            }
        }

        private void HandleClientFrame(IConnectionSession session, byte[] payload)
        {
            if (GateClosed) return;
            if (!LitTidyWireCodec.TryReadEnvelope(payload, out var msgType, out var body)) return;
            switch (msgType)
            {
                case LitTidyWireCodec.MsgSessionChallenge: HandleSessionChallenge(session, body); break;
                case LitTidyWireCodec.MsgTidyCommitted: HandleTidyCommitted(session, body); break;
                case LitTidyWireCodec.MsgTidyHotkeyResult: HandleTidyHotkeyResult(session, body); break;
                case LitTidyWireCodec.MsgContainerTidyResult: HandleContainerTidyResult(session, body); break; // DEV-V5-03
                case LitTidyWireCodec.MsgFastTransferRecoverResult: HandleFastTransferResult(session, body); break; // DEV-V5-05
                default: break;
            }
        }

        // ── server: request admission + main-thread execution ──────

        private void HandleTidyRequest(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadTidyRequest(body, out var token, out var requestId, out var page, out var mode, out var sortDescending, out var hotkeys))
            {
                LitRuntime.LogWarning("[TidyNet] 服务器收到畸形 RequestTidy，拒绝（peer=" + session.PeerSteamId + "）");
                return;
            }
            var peer = session.PeerSteamId; // the session is the identity authority — never a payload field
            var generation = session.SessionId;
            var kind = admission.TryAdmit(peer, generation, token, requestId, out var cached);
            switch (kind)
            {
                case LitAdmissionGate.AdmissionKind.InFlight:
                    return; // the original request is still executing — silent
                case LitAdmissionGate.AdmissionKind.Cached:
                    ReplayCached(session, token, requestId, cached);
                    return;
                case LitAdmissionGate.AdmissionKind.BusyDifferent:
                case LitAdmissionGate.AdmissionKind.Rejected:
                    SendCommitted(session, token, requestId, TidyCommitResult.Rejected, null);
                    return;
            }
            var captured = new LitTidyRequestContext
            {
                PeerSteamId = peer,
                ConnectionGeneration = generation,
                SessionToken = token,
                RequestId = requestId,
                Page = page,
                Mode = mode,
                SortDescending = sortDescending,
                Hotkeys = hotkeys,
            };
            var queued = new QueuedTidyRequest
            {
                Work = () => ExecuteServerTidyOnMainThread(captured),
                Cancel = () =>
                {
                    // Stop drain / enqueue failure compensation: the lease
                    // releases, the ledger lands on Failed, the client gets
                    // a terminal Rejected (best effort when transport died).
                    admission.CancelNew(peer, generation, token, requestId);
                    SendCommitted(ResolveLiveSession(generation), token, requestId, TidyCommitResult.Rejected, null);
                },
                Tag = "TidyRequest peer=" + peer + " reqId=" + requestId,
            };
            if (!MainThreadDispatcher.TryEnqueue(queued))
            {
                admission.CancelNew(peer, generation, token, requestId);
                SendCommitted(session, token, requestId, TidyCommitResult.Rejected, null);
            }
        }

        private void ExecuteServerTidyOnMainThread(LitTidyRequestContext req)
        {
            var session = ResolveLiveSession(req.ConnectionGeneration);
            try
            {
                if (!faultBook.IsAllowed(req.PeerSteamId))
                {
                    ledger.MarkResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                        LitRequestLedger.RequestState.Failed, TidyCommitResult.CriticalFailure, null);
                    SendCommitted(session, req.SessionToken, req.RequestId, TidyCommitResult.CriticalFailure, null);
                    PublishCompleted(req, TidyCommitResult.CriticalFailure);
                    return;
                }
                LitAuthorityResult result;
                try { result = authority.ExecuteServerTidy(req); }
                catch (Exception error)
                {
                    faultBook.Open(req.PeerSteamId, "authority crash: " + error.Message, temporary: false);
                    ledger.MarkResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                        LitRequestLedger.RequestState.Failed, TidyCommitResult.CriticalFailure, null);
                    SendCommitted(session, req.SessionToken, req.RequestId, TidyCommitResult.CriticalFailure, null);
                    PublishCompleted(req, TidyCommitResult.CriticalFailure);
                    return;
                }
                var outcome = result.Outcome ?? TidyOperationOutcome.RejectedNoMutation;
                switch (outcome.Result)
                {
                    case TidyCommitResult.CriticalFailure:
                        // Full restoration verified → temporary fault (a
                        // disconnect clears it); anything else → persistent
                        // (disk) — the migrated P0-4 rule.
                        faultBook.Open(req.PeerSteamId, outcome.FailureReason ?? "CriticalFailure during tidy", temporary: outcome.FullRestorationVerified);
                        ledger.MarkResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                            LitRequestLedger.RequestState.Failed, TidyCommitResult.CriticalFailure, null);
                        SendCommitted(session, req.SessionToken, req.RequestId, TidyCommitResult.CriticalFailure, null);
                        break;
                    case TidyCommitResult.ConcurrentMutationAfterCommit:
                        faultBook.Open(req.PeerSteamId, outcome.FailureReason ?? "ConcurrentMutationAfterCommit", temporary: false);
                        ledger.MarkResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                            LitRequestLedger.RequestState.Failed, TidyCommitResult.ConcurrentMutationAfterCommit, null);
                        SendCommitted(session, req.SessionToken, req.RequestId, TidyCommitResult.ConcurrentMutationAfterCommit, null);
                        break;
                    case TidyCommitResult.Rejected:
                        ledger.MarkResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                            LitRequestLedger.RequestState.Failed, TidyCommitResult.Rejected, null);
                        SendCommitted(session, req.SessionToken, req.RequestId, TidyCommitResult.Rejected, null);
                        break;
                    default: // Committed
                        pendingRestores.Store(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId, result.RestoreEntries);
                        ledger.MarkResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                            LitRequestLedger.RequestState.Committed, TidyCommitResult.Committed, result.Mappings);
                        SendCommitted(session, req.SessionToken, req.RequestId, TidyCommitResult.Committed, result.Mappings);
                        break;
                }
                PublishCompleted(req, outcome.Result);
            }
            finally
            {
                // The lease releases when the response is settled — never
                // held across the ack (the ack rides its own book).
                leases.Release(req.PeerSteamId, req.RequestId);
            }
        }

        // ── DEV-V5-03: server container tidy admission + execution ─────
        // Shares the session token, the request ledger and the per-player
        // lease with the player-page flow: one tidy transaction at a time per
        // peer (the lease answers Busy), replays hit the cached container
        // result, and the authority re-verifies kind/access/version before
        // any mutation. No hotkey restore, no TidyCompleted publish — a
        // container move never triggers the reload-after-tidy feature
        // (V5-T4/V5-T7: 容器整理不是背包整理完成事件).

        /// <summary>Which frame replays a cached terminal answer: the entry
        /// itself carries the kind (a container cache must never answer a
        /// mixed-kind replay with the hotkey-mapping frame).</summary>
        private void ReplayCached(IConnectionSession session, ulong token, uint requestId, LitRequestLedger.LedgerEntry cached)
        {
            if (cached.IsFastTransferResult)
            {
                // DEV-V5-05: the cached answer of a fast-transfer request is
                // the message-10 frame — never the container pair, never a
                // mapping frame (replaying across kinds would be a protocol lie).
                SendFastTransferResult(session, token, requestId, cached.Result, cached.ReasonCode);
                return;
            }
            if (cached.IsContainerTidy)
            {
                SendContainerResult(session, token, requestId, cached.Result, cached.ReasonCode);
                return;
            }
            SendCommitted(session, token, requestId, cached.Result, cached.Mappings);
        }

        private void HandleContainerTidyRequest(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadContainerTidyRequest(body, out var token, out var requestId, out var kindByte, out var fingerprint, out var sortDescending))
            {
                LitRuntime.LogWarning("[TidyNet] 服务器收到畸形 RequestContainerTidy，拒绝（peer=" + session.PeerSteamId + "）");
                return;
            }
            var peer = session.PeerSteamId; // the session is the identity authority — never a payload field
            var generation = session.SessionId;
            var admitted = admission.TryAdmit(peer, generation, token, requestId, out var cached);
            switch (admitted)
            {
                case LitAdmissionGate.AdmissionKind.InFlight:
                    return; // the original container request is still executing — silent
                case LitAdmissionGate.AdmissionKind.Cached:
                    ReplayCached(session, token, requestId, cached);
                    return;
                case LitAdmissionGate.AdmissionKind.BusyDifferent:
                case LitAdmissionGate.AdmissionKind.Rejected:
                    SendContainerResult(session, token, requestId, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.Busy);
                    return;
            }
            var captured = new LitContainerTidyRequestContext
            {
                PeerSteamId = peer,
                ConnectionGeneration = generation,
                SessionToken = token,
                RequestId = requestId,
                Kind = (LitContainerTidyKind)kindByte,
                Fingerprint = fingerprint,
                SortDescending = sortDescending,
            };
            var queued = new QueuedTidyRequest
            {
                Work = () => ExecuteServerContainerTidyOnMainThread(captured),
                Cancel = () =>
                {
                    // Stop drain / enqueue failure: the ledger lands Failed
                    // and the client gets a terminal Rejected — with an honest
                    // reason (功能停用中), never a silent nothing.
                    admission.CancelNew(peer, generation, token, requestId);
                    SendContainerResult(ResolveLiveSession(generation), token, requestId, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.FeatureUnavailable);
                },
                Tag = "ContainerTidyRequest peer=" + peer + " reqId=" + requestId,
            };
            if (!MainThreadDispatcher.TryEnqueue(queued))
            {
                admission.CancelNew(peer, generation, token, requestId);
                SendContainerResult(session, token, requestId, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.FeatureUnavailable);
            }
        }

        private void ExecuteServerContainerTidyOnMainThread(LitContainerTidyRequestContext req)
        {
            var session = ResolveLiveSession(req.ConnectionGeneration);
            try
            {
                if (!faultBook.IsAllowed(req.PeerSteamId))
                {
                    ledger.MarkContainerResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                        LitRequestLedger.RequestState.Failed, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.FeatureUnavailable);
                    SendContainerResult(session, req.SessionToken, req.RequestId, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.FeatureUnavailable);
                    return;
                }
                LitContainerAuthorityResult result;
                try { result = authority.ExecuteServerContainerTidy(req); }
                catch (Exception error)
                {
                    faultBook.Open(req.PeerSteamId, "container authority crash: " + error.Message, temporary: false);
                    ledger.MarkContainerResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                        LitRequestLedger.RequestState.Failed, TidyCommitResult.CriticalFailure, (byte)LitContainerTidyReason.InternalFailure);
                    SendContainerResult(session, req.SessionToken, req.RequestId, TidyCommitResult.CriticalFailure, (byte)LitContainerTidyReason.InternalFailure);
                    return;
                }
                if (result == null || result.Outcome == null)
                    result = new LitContainerAuthorityResult { Outcome = TidyOperationOutcome.RejectedNoMutation, Reason = LitContainerTidyReason.InternalFailure };
                var outcome = result.Outcome;
                var reason = result.Reason;
                if (outcome.Result == TidyCommitResult.Committed) reason = LitContainerTidyReason.None;
                else if (reason == LitContainerTidyReason.None) reason = LitContainerTidyReason.LayoutFailed; // never an unnamed refusal
                var terminal = outcome.Result == TidyCommitResult.Committed
                    ? LitRequestLedger.RequestState.Committed
                    : LitRequestLedger.RequestState.Failed;
                if (outcome.Result == TidyCommitResult.CriticalFailure)
                    faultBook.Open(req.PeerSteamId, outcome.FailureReason ?? "container CriticalFailure", temporary: outcome.FullRestorationVerified);
                else if (outcome.Result == TidyCommitResult.ConcurrentMutationAfterCommit)
                    faultBook.Open(req.PeerSteamId, outcome.FailureReason ?? "container ConcurrentMutationAfterCommit", temporary: false);
                ledger.MarkContainerResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                    terminal, outcome.Result, (byte)reason);
                SendContainerResult(session, req.SessionToken, req.RequestId, outcome.Result, (byte)reason);
                TidyDiagnosticLog.Info("container-tidy-authority",
                    "[Tidy容器] 权威处理完成（peer=" + req.PeerSteamId + ", reqId=" + req.RequestId + ", result=" + outcome.Result + ", reason=" + reason + "）。");
            }
            finally
            {
                leases.Release(req.PeerSteamId, req.RequestId);
            }
        }

        // ── DEV-V5-05: server fast-transfer recover admission + execution ──
        // The SAME session token, request ledger and per-player lease as the
        // button tidy and container flows: one tidy-class transaction at a time
        // per peer (a concurrent second request answers Busy), replays hit the
        // cached fast-transfer result, and the authority re-verifies the 03
        // session claim + re-reads the source jar before ANY mutation. No
        // hotkey restore, no TidyCompleted publish — a recovery is not a
        // player-initiated tidy (V5-T5 Q5).

        private void HandleFastTransferRequest(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadFastTransferRequest(body, out var token, out var requestId,
                    out var sourcePage, out var sourceX, out var sourceY, out var kindByte, out var fingerprint, out var sortDescending))
            {
                LitRuntime.LogWarning("[快速转移恢复] 服务器收到畸形 RequestFastTransferRecover，拒绝（peer=" + session.PeerSteamId + "）");
                return;
            }
            var peer = session.PeerSteamId; // the session is the identity authority — never a payload field
            var generation = session.SessionId;
            var admitted = admission.TryAdmit(peer, generation, token, requestId, out var cached);
            switch (admitted)
            {
                case LitAdmissionGate.AdmissionKind.InFlight:
                    return; // the original recover request is still executing — silent
                case LitAdmissionGate.AdmissionKind.Cached:
                    ReplayCached(session, token, requestId, cached);
                    return;
                case LitAdmissionGate.AdmissionKind.BusyDifferent:
                case LitAdmissionGate.AdmissionKind.Rejected:
                    SendFastTransferResult(session, token, requestId, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.Busy);
                    return;
            }
            var captured = new LitFastTransferRequestContext
            {
                PeerSteamId = peer,
                ConnectionGeneration = generation,
                SessionToken = token,
                RequestId = requestId,
                Kind = (LitContainerTidyKind)kindByte,
                Fingerprint = fingerprint,
                SourcePage = sourcePage,
                SourceX = sourceX,
                SourceY = sourceY,
                SortDescending = sortDescending,
            };
            var queued = new QueuedTidyRequest
            {
                Work = () => ExecuteServerFastTransferOnMainThread(captured),
                Cancel = () =>
                {
                    // Stop drain / enqueue failure: ledger Failed + a terminal
                    // Rejected with an honest reason — never a silent nothing.
                    admission.CancelNew(peer, generation, token, requestId);
                    SendFastTransferResult(ResolveLiveSession(generation), token, requestId, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.FeatureUnavailable);
                },
                Tag = "FastTransferRequest peer=" + peer + " reqId=" + requestId,
            };
            if (!MainThreadDispatcher.TryEnqueue(queued))
            {
                admission.CancelNew(peer, generation, token, requestId);
                SendFastTransferResult(session, token, requestId, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.FeatureUnavailable);
            }
        }

        private void ExecuteServerFastTransferOnMainThread(LitFastTransferRequestContext req)
        {
            var session = ResolveLiveSession(req.ConnectionGeneration);
            try
            {
                if (!faultBook.IsAllowed(req.PeerSteamId))
                {
                    ledger.MarkFastTransferResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                        LitRequestLedger.RequestState.Failed, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.FeatureUnavailable);
                    SendFastTransferResult(session, req.SessionToken, req.RequestId, TidyCommitResult.Rejected, (byte)LitContainerTidyReason.FeatureUnavailable);
                    return;
                }
                LitContainerAuthorityResult result;
                try { result = authority.ExecuteServerFastTransferRecover(req); }
                catch (Exception error)
                {
                    faultBook.Open(req.PeerSteamId, "fast-transfer authority crash: " + error.Message, temporary: false);
                    ledger.MarkFastTransferResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                        LitRequestLedger.RequestState.Failed, TidyCommitResult.CriticalFailure, (byte)LitContainerTidyReason.InternalFailure);
                    SendFastTransferResult(session, req.SessionToken, req.RequestId, TidyCommitResult.CriticalFailure, (byte)LitContainerTidyReason.InternalFailure);
                    return;
                }
                if (result == null || result.Outcome == null)
                    result = new LitContainerAuthorityResult { Outcome = TidyOperationOutcome.RejectedNoMutation, Reason = LitContainerTidyReason.InternalFailure };
                var outcome = result.Outcome;
                var reason = result.Reason;
                if (outcome.Result == TidyCommitResult.Committed) reason = LitContainerTidyReason.None;
                else if (reason == LitContainerTidyReason.None) reason = LitContainerTidyReason.LayoutFailed; // never an unnamed refusal
                var terminal = outcome.Result == TidyCommitResult.Committed
                    ? LitRequestLedger.RequestState.Committed
                    : LitRequestLedger.RequestState.Failed;
                if (outcome.Result == TidyCommitResult.CriticalFailure)
                    faultBook.Open(req.PeerSteamId, outcome.FailureReason ?? "fast-transfer CriticalFailure", temporary: outcome.FullRestorationVerified);
                else if (outcome.Result == TidyCommitResult.ConcurrentMutationAfterCommit)
                    faultBook.Open(req.PeerSteamId, outcome.FailureReason ?? "fast-transfer ConcurrentMutationAfterCommit", temporary: false);
                ledger.MarkFastTransferResult(req.PeerSteamId, req.ConnectionGeneration, req.SessionToken, req.RequestId,
                    terminal, outcome.Result, (byte)reason);
                SendFastTransferResult(session, req.SessionToken, req.RequestId, outcome.Result, (byte)reason);
                TidyDiagnosticLog.Info("fast-transfer-authority",
                    "[快速转移恢复] 权威处理完成（peer=" + req.PeerSteamId + ", reqId=" + req.RequestId +
                    ", result=" + outcome.Result + ", reason=" + reason + "）。");
            }
            finally
            {
                leases.Release(req.PeerSteamId, req.RequestId);
            }
        }

        // ── server: hotkey flow ack → restore → result ─────────────

        private void HandleHotkeyFlowAck(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadHotkeyFlowAck(body, out var token, out var requestId)) return;
            var peer = session.PeerSteamId;
            var generation = session.SessionId;
            if (!pendingRestores.TryGet(peer, generation, token, requestId, out var entries))
            {
                return; // expired/unknown transaction — silently ignored
            }
            var queued = new QueuedTidyRequest
            {
                Work = () => ExecuteAckRestoreOnMainThread(peer, generation, token, requestId, entries),
                Cancel = () => pendingRestores.Remove(peer, generation, token, requestId),
                Tag = "TidyAckRestore peer=" + peer + " reqId=" + requestId,
            };
            if (!MainThreadDispatcher.TryEnqueue(queued))
            {
                pendingRestores.Remove(peer, generation, token, requestId);
            }
        }

        private void ExecuteAckRestoreOnMainThread(ulong peer, ulong generation, ulong token, uint requestId, List<HotkeyRestoreEntry> entries)
        {
            pendingRestores.Remove(peer, generation, token, requestId);
            var session = ResolveLiveSession(generation);
            if (session == null)
            {
                LitRuntime.LogInfo("[TidyNet] ACK 对应会话已不存在，恢复结果不再发送（reqId=" + requestId + "）。");
                return;
            }
            LitHotkeyRestoreResult restore;
            try { restore = authority.RestoreServerHotkeys(peer, entries); }
            catch (Exception error)
            {
                LitRuntime.LogError("[TidyNet] ACK 恢复执行异常（reqId=" + requestId + "): " + error.Message);
                restore = new LitHotkeyRestoreResult { Restored = 0, Verified = 0, Cleared = 0, FailedIndices = new List<byte>() };
            }
            var failed = restore.FailedIndices ?? new List<byte>(0);
            SendHotkeyResult(session, token, requestId,
                (byte)Math.Min(restore.Restored, 255),
                (byte)Math.Min(restore.Cleared, 255),
                (byte)Math.Min(failed.Count, 255),
                (byte)Math.Min(restore.Verified, 255),
                failed);
            LitRuntime.LogInfo("[TidyNet] ACK 处理完成（reqId=" + requestId + ", restored=" + restore.Restored + ", verified=" + restore.Verified + ", cleared=" + restore.Cleared + "）。");
        }

        // ── client: challenge / committed / result ─────────────────

        private void HandleSessionChallenge(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadSessionChallenge(body, out var token)) return;
            clientToken.ReplaceWithServerChallenge(session.SessionId, token);
            LitRuntime.LogInfo("[TidyNet] 已接收并应用服务端会话 challenge（generation=" + session.SessionId + "）。");
        }

        private void HandleTidyCommitted(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadTidyCommitted(body, out var token, out var requestId, out var result, out var mappings)) return;
            var generation = session.SessionId;
            if (!clientPending.IsPending(generation, token, requestId))
            {
                LitRuntime.LogWarning("[TidyNet] 收到未发出的 (generation=" + generation + ", reqId=" + requestId + ") 响应，忽略（可能是旧响应或伪造）。");
                return;
            }
            // DEV-V5-03: a player-page committed frame must not consume a
            // CONTAINER pending (and the container handler mirrors this
            // guard) — a mismatched pair is a protocol lie from the wire,
            // dropped with a warning. DEV-V5-05: same law for FAST-TRANSFER
            // pendings (three kinds, each frame only consumes its own).
            if (clientPending.TryGetPending(generation, token, requestId, out var pendingEntry)
                    && (pendingEntry.IsContainerRequest || pendingEntry.IsFastTransferRequest))
            {
                LitRuntime.LogWarning("[TidyNet] 收到服装页 TidyCommitted 但待确认登记为" + pendingEntry.Kind + "请求（reqId=" + requestId + "），忽略。");
                return;
            }
            if (result != TidyCommitResult.Committed)
            {
                clientPending.ClearPending(generation, token, requestId);
                if (result == TidyCommitResult.CriticalFailure)
                {
                    LitRuntime.LogError("[TidyNet] 服务器报告 CriticalFailure，本会话整理已被熔断。");
                }
                else
                {
                    LitRuntime.LogInfo("[TidyNet] <- 服务器 TidyCommitted(reqId=" + requestId + ", result=" + result + ")。");
                }
                return;
            }
            var queued = new QueuedTidyRequest
            {
                Work = () => RunClientConvergence(generation, token, requestId, mappings, ConvergenceMaxAttempts),
                Cancel = () => clientPending.ClearPending(generation, token, requestId),
                Tag = "TidyConvergence reqId=" + requestId,
            };
            if (!MainThreadDispatcher.TryEnqueue(queued))
            {
                clientPending.ClearPending(generation, token, requestId);
            }
        }

        /// <summary>
        /// The bounded convergence loop (main thread, dispatcher-rescheduled
        /// — no GameObject lifecycle): each attempt probes the mapping
        /// targets through the authority; success registers the result wait
        /// and sends the flow ack; exhaustion warns and clears the pending.
        /// </summary>
        private void RunClientConvergence(ulong generation, ulong token, uint requestId, List<LitNewPositionMapping> mappings, int remainingAttempts)
        {
            if (GateClosed) return;
            var session = ResolveLiveSession(generation);
            if (session == null)
            {
                clientPending.ClearPending(generation, token, requestId);
                return;
            }
            bool converged;
            try { converged = authority.VerifyClientConvergence(mappings); }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[TidyNet] 收敛检查异常（reqId=" + requestId + "): " + error.Message);
                converged = false;
            }
            if (converged)
            {
                clientWait.Register(generation, token, requestId);
                // R3-Standards B6: the CLIENT sends through SendToServer
                // (untargeted server-bound) — SendToClient is the server's
                // per-peer path and fails on every real client transport.
                NetworkSendResult sent;
                try { sent = network.SendToServer(Channel, LitTidyWireCodec.BuildHotkeyFlowAck(token, requestId), reliable: true); }
                catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
                if (sent != NetworkSendResult.Sent)
                {
                    LitRuntime.LogWarning("[TidyNet] HotkeyFlowAck 发送未送达（reqId=" + requestId + ", result=" + sent + "），服务器事务将按 TTL 过期");
                }
                clientPending.ClearPending(generation, token, requestId);
                if (sent != NetworkSendResult.Sent) clientWait.Clear(generation, token, requestId);
                else LitRuntime.LogInfo("[TidyNet] -> 服务器 HotkeyFlowAck(reqId=" + requestId + ")。");
                return;
            }
            if (remainingAttempts > 1)
            {
                var requeued = MainThreadDispatcher.TryEnqueue(new QueuedTidyRequest
                {
                    Work = () => RunClientConvergence(generation, token, requestId, mappings, remainingAttempts - 1),
                    Cancel = () => clientPending.ClearPending(generation, token, requestId),
                    Tag = "TidyConvergence reqId=" + requestId,
                });
                if (requeued) return;
            }
            LitRuntime.LogWarning("[TidyNet] 库存收敛超时 reqId=" + requestId + "，部分快捷键可能未恢复。");
            clientPending.ClearPending(generation, token, requestId);
        }

        private void HandleTidyHotkeyResult(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadTidyHotkeyResult(body, out var token, out var requestId, out var restored, out var cleared, out var failed, out var verified, out var failedIndices)) return;
            var generation = session.SessionId;
            if (!clientWait.IsWaiting(generation, token, requestId))
            {
                LitRuntime.LogWarning("[TidyNet] 收到未等待的 HotkeyResult (generation=" + generation + ", reqId=" + requestId + ")，忽略。");
                return;
            }
            clientWait.Clear(generation, token, requestId);
            if (failed > 0)
            {
                var names = failedIndices == null ? string.Empty : string.Join(",", failedIndices);
                LitRuntime.LogWarning("[TidyNet] ⚠ 整理完成，但 " + failed + " 个快捷键未能恢复（已清除绑定）: " + names);
            }
            else if (verified < restored)
            {
                LitRuntime.LogInfo("[TidyNet] 快捷键绑定完成，部分状态无法验证（reqId=" + requestId + ", restored=" + restored + ", verified=" + verified + "）。");
            }
            else
            {
                LitRuntime.LogInfo("[TidyNet] 快捷键已恢复并验证（reqId=" + requestId + ", restored=" + restored + "）。");
            }
        }

        /// <summary>
        /// DEV-V5-03 (client): consume one container tidy result. The pending
        /// entry must exist AND be a container request (kind mismatch on the
        /// wire is a protocol lie — dropped with a warning, never consumed).
        /// Success is the visible grid re-arriving through vanilla's own
        /// item fan-out (no extra sync); every failure answers in the
        /// player's language (T4 Q4: 禁止可点但静默没动静).
        /// </summary>
        private void HandleContainerTidyResult(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadContainerTidyResult(body, out var token, out var requestId, out var result, out var reasonByte)) return;
            var generation = session.SessionId;
            if (!clientPending.TryGetPending(generation, token, requestId, out var entry))
            {
                LitRuntime.LogWarning("[Tidy容器] 收到未发出的容器整理响应（generation=" + generation + ", reqId=" + requestId + "），忽略（旧响应或伪造）。");
                return;
            }
            if (!entry.IsContainerRequest)
            {
                // DEV-V5-05: the marker is now the explicit request kind (a
                // fast-transfer entry may also reference page 7 — as SOURCE).
                LitRuntime.LogWarning("[Tidy容器] 收到容器结果帧但待确认登记为" + entry.Kind + "请求（reqId=" + requestId + "），忽略。");
                return;
            }
            clientPending.ClearPending(generation, token, requestId);
            LitContainerTidyReasons.TryFromWire(reasonByte, out var reason);
            if (result == TidyCommitResult.Committed)
            {
                LitContainerFeedback.ShowNote("结果(reqId=" + requestId + ")", "容器整理已完成。");
                return;
            }
            if (result == TidyCommitResult.CriticalFailure || result == TidyCommitResult.ConcurrentMutationAfterCommit)
                LitRuntime.LogError("[Tidy容器] 服务器报告 " + result + "（reqId=" + requestId + "）");
            LitContainerFeedback.ShowReason("服务器重验未通过（reqId=" + requestId + "）", reason);
        }

        /// <summary>
        /// DEV-V5-05 (client): consume one fast-transfer recover result. The
        /// pending entry must exist AND be a fast-transfer request (kind
        /// mismatch = a wire lie — dropped with a warning, never consumed).
        /// The answer is DIAGNOSTIC ONLY by design: a successful recovery is
        /// visible through vanilla's own item fan-out, and a refused one keeps
        /// the original silent-failure semantics (this is a recovery trigger
        /// off a right-click, not a button — T4's「可点必须回执」law belongs to
        /// buttons; 04's insert recovery answers the same way).
        /// </summary>
        private void HandleFastTransferResult(IConnectionSession session, byte[] body)
        {
            if (!LitTidyWireCodec.TryReadFastTransferResult(body, out var token, out var requestId, out var result, out var reasonByte)) return;
            var generation = session.SessionId;
            if (!clientPending.TryGetPending(generation, token, requestId, out var entry))
            {
                LitRuntime.LogWarning("[快速转移恢复] 收到未发出的恢复响应（generation=" + generation + ", reqId=" + requestId + "），忽略（旧响应或伪造）。");
                return;
            }
            if (!entry.IsFastTransferRequest)
            {
                LitRuntime.LogWarning("[快速转移恢复] 收到快速转移结果帧但待确认登记为" + entry.Kind + "请求（reqId=" + requestId + "），忽略。");
                return;
            }
            clientPending.ClearPending(generation, token, requestId);
            LitContainerTidyReasons.TryFromWire(reasonByte, out var reason);
            if (result == TidyCommitResult.Committed)
            {
                TidyDiagnosticLog.Info("fast-transfer-recover-result",
                    "[快速转移恢复] 服务器已原子提交（reqId=" + requestId + "）：物品随原版同步落位，未发布整理完成。");
                return;
            }
            if (result == TidyCommitResult.CriticalFailure || result == TidyCommitResult.ConcurrentMutationAfterCommit)
                LitRuntime.LogError("[快速转移恢复] 服务器报告 " + result + "（reqId=" + requestId + "）");
            // 拒绝 = 保持原版静默（物品留在原处是原版的失败语义，不是本票要改的行为）。
            LitRuntime.LogInfo("[快速转移恢复] 服务器重验未通过（reqId=" + requestId + ", reason=" + reason + "）：保持原版。");
        }

        // ── shared send / publish helpers ───────────────────────────

        private void SendCommitted(IConnectionSession session, ulong token, uint requestId, TidyCommitResult result, List<LitNewPositionMapping> mappings)
        {
            // R1-Spec DEVIATION-2 fix: the reliable response's explicit
            // result is handled — a non-Sent outcome surfaces as a
            // structured diagnostic and falls back to the ledger recovery
            // path: the Committed cache survives, so the client's duplicate
            // request replays the full response (Cached admission).
            var sendResult = TrySendToSession(session, LitTidyWireCodec.BuildTidyCommitted(token, requestId, result, mappings));
            if (sendResult != NetworkSendResult.Sent)
            {
                LitRuntime.LogWarning("[TidyNet] TidyCommitted 可靠发送未送达（reqId=" + requestId + ", result=" + sendResult + "），账本 Committed 缓存保留，等待客户端重发命中 Cached 路径");
                return;
            }
            if (session != null)
            {
                LitRuntime.LogInfo("[TidyNet] -> 客机 TidyCommitted(reqId=" + requestId + ", result=" + result + ", mappings=" + (mappings?.Count ?? 0) + ")。");
            }
        }

        /// <summary>DEV-V5-03: the container result is the transaction's
        /// terminal answer — reliable like the committed frame; a failed send
        /// leaves the ledger cache so the client's duplicate request replays
        /// it (the same recovery path as the player-page flow).</summary>
        private void SendContainerResult(IConnectionSession session, ulong token, uint requestId, TidyCommitResult result, byte reasonCode)
        {
            var sendResult = TrySendToSession(session, LitTidyWireCodec.BuildContainerTidyResult(token, requestId, result, reasonCode));
            if (sendResult != NetworkSendResult.Sent)
            {
                LitRuntime.LogWarning("[Tidy容器] ContainerTidyResult 可靠发送未送达（reqId=" + requestId + ", result=" + sendResult + "），账本缓存保留，等待客户端重发命中 Cached 路径");
                return;
            }
            if (session != null)
                LitRuntime.LogInfo("[Tidy容器] -> 客机 ContainerTidyResult(reqId=" + requestId + ", result=" + result + ", reason=" + reasonCode + ")。");
        }

        /// <summary>DEV-V5-05: the fast-transfer recover terminal frame —
        /// reliable like the container pair; a failed send leaves the ledger
        /// cache so the client's duplicate request replays it.</summary>
        private void SendFastTransferResult(IConnectionSession session, ulong token, uint requestId, TidyCommitResult result, byte reasonCode)
        {
            var sendResult = TrySendToSession(session, LitTidyWireCodec.BuildFastTransferResult(token, requestId, result, reasonCode));
            if (sendResult != NetworkSendResult.Sent)
            {
                LitRuntime.LogWarning("[快速转移恢复] 结果帧可靠发送未送达（reqId=" + requestId + ", result=" + sendResult + "），账本缓存保留，等待客户端重发命中 Cached 路径");
                return;
            }
            if (session != null)
                LitRuntime.LogInfo("[快速转移恢复] -> 客机 FastTransferRecoverResult(reqId=" + requestId + ", result=" + result + ", reason=" + reasonCode + ")。");
        }

        private void SendHotkeyResult(IConnectionSession session, ulong token, uint requestId, byte restored, byte cleared, byte failed, byte verified, List<byte> failedIndices)
        {
            // The hotkey result is informational (the client's wait rides
            // its TTL): a non-Sent outcome is a named diagnostic — the
            // client reports an unknown result when its wait expires.
            var sendResult = TrySendToSession(session, LitTidyWireCodec.BuildTidyHotkeyResult(token, requestId, restored, cleared, failed, verified, failedIndices));
            if (sendResult != NetworkSendResult.Sent)
            {
                LitRuntime.LogWarning("[TidyNet] TidyHotkeyResult 可靠发送未送达（reqId=" + requestId + ", result=" + sendResult + "），客户端等待将按 TTL 超时提示结果未知");
            }
        }

        private void PublishCompleted(LitTidyRequestContext req, TidyCommitResult result)
        {
            // The authoritative request id IS the transaction identity
            // (feature-private, never zero); the page-range expansion lives
            // at the module's publish entry (single source, R5-Standards S2).
            module.PublishTidyCompletedForPage(req.Page, result, req.ConnectionGeneration, req.RequestId);
        }
    }
}
