using System;
using System.Collections.Generic;
using System.Text;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-03: BueNetworkApi runtime. Host-internal, pure C# — it implements
    /// the frozen BueNetwork contract surface (channel registration, version
    /// negotiation, session lifecycle, connection-context send) on top of an
    /// INetworkTransport seam. Real IClientTransport / ITransportConnection
    /// wiring (client→server vs server→client asymmetry) is DEV-V2-04; here the
    /// transport is injected (LocalLoopbackTransport in tests).
    /// DEV-V2-14: inbound subscription is directional contract surface —
    /// two handler tables keyed by ChannelDirection (the frame's source,
    /// carried by each session's handshake origin), handlers dispatched
    /// outside the state lock with per-handler exception isolation, and a
    /// host-internal module switch (SetModuleActive) whose inactive state
    /// keeps the channel/subscription tables intact while holding no
    /// sessions and receiving nothing.
    /// DEV-V2-16: sends are session-driven with frozen result semantics —
    /// the public Sessions snapshot is established-only, SendToClients
    /// targets each established session individually and aggregates
    /// per-target outcomes (NoSession / Sent / LocalTransportUnavailable /
    /// PartialFailure), SendToClient validates ownership by identity,
    /// establishment, and the live generation, and no send holds the state
    /// lock across the transport call.
    /// DEV-V2-17: the session lifecycle is automatic — the feature module
    /// carries zero handshake burden. Transport connected → the runtime
    /// sends Hello (initiator role) → the responder Acks (synchronous
    /// establishment) or Rejects (version fail-closed) → Connected fires
    /// only after establishment. Runtime duties: disconnect cleanup
    /// (PeerDisconnected), reconnect with a fresh connection generation
    /// (the stale established session is superseded — Disconnected at drop,
    /// GenerationChanged(newGeneration) on the dead object when the
    /// successor establishes), pending-handshake re-probe with doubling
    /// backoff (TickHandshake, 1s → 8s cap), duplicate-Hello dedup
    /// (idempotent re-Ack), version fail-closed on both sides, and no ghost
    /// sessions (one live session per peer). Ack/Reject match by peer +
    /// handshake nonce — the initiator's session id IS the connection
    /// generation — never by "first un-established session". The control
    /// frames' peer identity is the FRAME HEADER sender (the same
    /// transport-authoritative source DATA dispatch resolves context by);
    /// the control payload's steamId field is declarative only. Control
    /// frames carry the 20-byte payload [steamId 8][major 2][minor 2][nonce 8];
    /// Hello is untargeted (one logical server), Ack/Reject are targeted to
    /// the initiator. Control-frame sends and every lifecycle callback run
    /// OUTSIDE the state lock (repo dispatch convention; the DEV-V2-16 named
    /// deferral hands this lock policy to this ticket). A responder that
    /// re-arms with sessions missing resets its connected peers (a Reject
    /// matching no pending handshake), and the peer's initiator self-heals
    /// with a fresh handshake — the module switch itself still fires no
    /// lifecycle events (DEV-V2-14 frozen semantics).
    /// Frame format (DEV-V2-06 v2) is internal: magic "BUE1" (DEV-V2-18
    /// rename; the frame never shipped, so it carries zero compatibility
    /// cost) + kind byte +
    /// length-prefixed channel id + the sender's steam id (8 bytes LE) +
    /// payload — never exposed into Contracts. The sender field lets the
    /// receiver resolve the dispatch context by source instead of "first
    /// session"; sends carry the reliability bit and either an untargeted
    /// target (0) or the addressed session's peer steam id.
    /// Frame kinds: 0=Data, 1=Hello, 2=Ack, 3=Reject (control frames carry
    /// an empty channel id and the 20-byte control payload).
    /// DEV-V2-18: the frame goes online for real — the decision core's BUE
    /// branch (NetworkModuleAdapter, gated by the network module switch
    /// alone) feeds inbound raw packets into the transport adapter's queue,
    /// and outbound sends resolve through the engine binding; the magic is
    /// owned by <see cref="BueFrameClassifier.FrameMagic"/> so the wire
    /// classifier and the encoder can never drift apart.
    /// </summary>
    public sealed class BueNetworkRuntime : IBueNetworkApi
    {
        private const byte KindData = 0;
        private const byte KindHello = 1;
        private const byte KindAck = 2;
        private const byte KindReject = 3;
        // DEV-V2-17: the pending-handshake re-probe schedule — first retry
        // after 1s, doubling, capped at 8s; the loop runs while the transport
        // reports the peer connected (disconnect cleanup owns the rest).
        internal const int HelloReprobeInitialMs = 1000;
        internal const int HelloReprobeCapMs = 8000;
        private readonly INetworkTransport transport;
        private readonly ContractVersion localContract;
        private readonly ulong localSteamId;
        // DEV-V2-17: the initiator role drives the automatic handshake — the
        // initiator sends Hello when the transport reports a peer connected;
        // the responder answers Hello instead. Production wiring knows the
        // local role (the P2P host answers, the client initiates).
        private readonly bool handshakeInitiator;
        private readonly Func<long> monotonicMilliseconds;
        private readonly Dictionary<string, ChannelRegistration> channels =
            new Dictionary<string, ChannelRegistration>(StringComparer.Ordinal);
        private readonly Dictionary<ulong, BueNetworkSession> sessions =
            new Dictionary<ulong, BueNetworkSession>();
        // DEV-V2-17: the last dropped ESTABLISHED session per peer. When a
        // successor session to the same peer establishes, the dead object
        // fires GenerationChanged(newSessionId) so a module still holding it
        // learns the replacement generation. Bounded by the peer count.
        private readonly Dictionary<ulong, BueNetworkSession> supersededByPeer =
            new Dictionary<ulong, BueNetworkSession>();
        // DEV-V2-14 ①: subscription tables are keyed by (channel, direction)
        // and decoupled from the channel table — subscribing to an
        // unregistered channel is legal, and a frame dispatches only once the
        // receiving side has registered the channel and traffic arrives.
        private readonly Dictionary<string, List<SubscriptionRecord>> fromClientsHandlers =
            new Dictionary<string, List<SubscriptionRecord>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<SubscriptionRecord>> fromServerHandlers =
            new Dictionary<string, List<SubscriptionRecord>>(StringComparer.Ordinal);
        // DEV-V2-14: host-internal module switch. Inactive keeps the channel
        // and subscription tables intact (Register/Unregister/Subscribe stay
        // legal) while holding zero sessions, receiving nothing, and firing
        // no lifecycle events — sends fall out to the existing enum results.
        private bool moduleActive = true;
        private bool receiveAttached = true;
        private readonly object sync = new object();
        // Session ids must be unique across runtime instances: an ownership
        // check (SendToClient looks up its own sessions by id) would otherwise
        // collide when two runtimes both start numbering at 1. Interlocked
        // keeps uniqueness even when two runtimes increment under different
        // instance locks.
        private static long nextSessionId = 1;
        // DEV-V3-04: the per-session send budget + link-health book (numbers
        // fixed by the ticket on NetworkSendGuard) and the structured
        // diagnostic sink (T5: 诊断走内部 seam，本票不新增公开 Logger 成员；
        // 生产绑定宿主 runtime log，测试自捕获). Lines never run under the
        // state lock: decisions produce data, emission happens after release.
        private readonly NetworkSendGuard sendGuard = new NetworkSendGuard();
        private readonly Action<string> diagnosticSink;

        public BueNetworkRuntime(INetworkTransport transport, ContractVersion localContract, ulong localSteamId, bool handshakeInitiator = true, Func<long> monotonicMilliseconds = null, Action<string> diagnosticSink = null)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.localContract = localContract;
            this.localSteamId = localSteamId;
            this.handshakeInitiator = handshakeInitiator;
            this.monotonicMilliseconds = monotonicMilliseconds ?? DefaultMonotonicMilliseconds;
            this.diagnosticSink = diagnosticSink;
            transport.Receive += OnReceive;
            transport.PeerConnected += OnPeerConnected;
            transport.PeerDisconnected += OnPeerDisconnected;
        }

        private void EmitDiagnostic(string line)
        {
            var sink = diagnosticSink;
            if (sink == null || line == null) return;
            try { sink(line); }
            catch (Exception) { }
        }

        private static string ThrottleLine(string channel, ulong generation, int used)
        {
            return "event=network-send result=throttled channel=" + channel + " generation=" + generation
                + " windowMs=" + NetworkSendGuard.BudgetWindowMs + " budget=" + NetworkSendGuard.BudgetPerWindow
                + " used=" + used + " diagnosticId=BUE-NET-001";
        }

        private static string DegradedLine(ulong generation, NetworkSendResult lastFailure, long consecutiveFailures)
        {
            return "event=network-link result=degraded generation=" + generation + " lastResult=" + lastFailure
                + " consecutiveFailures=" + consecutiveFailures + " diagnosticId=BUE-NET-002";
        }

        private static string RecoveredLine(ulong generation, long afterFailures)
        {
            return "event=network-link result=recovered generation=" + generation + " afterFailures=" + afterFailures
                + " diagnosticId=BUE-NET-003";
        }

        private static long DefaultMonotonicMilliseconds()
        {
            return System.Diagnostics.Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
        }

        // Q1: one module = one named channel; FeatureId is the channel name.
        public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
        {
            lock (sync)
            {
                if (channels.ContainsKey(channel.Value))
                {
                    return new ChannelRegistrationResult(false, channel, FeatureRegistrationReason.DuplicateFeature, "DEV-V2-03-DUP");
                }
                // Q2: version negotiation is automatic — a channel demanding a
                // higher contract major than the local runtime is rejected.
                if (minimumBueContract.Major > localContract.Major)
                {
                    return new ChannelRegistrationResult(false, channel, FeatureRegistrationReason.ContractIncompatible, "DEV-V2-03-CONTRACT");
                }
                channels[channel.Value] = new ChannelRegistration(channel, minimumBueContract, featureVersion);
                return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "DEV-V2-03-OK");
            }
        }

        public bool UnregisterChannel(FeatureId channel)
        {
            lock (sync) return channels.Remove(channel.Value);
        }

        public IReadOnlyList<IConnectionSession> Sessions
        {
            // DEV-V2-16 ④: the public snapshot is ESTABLISHED sessions only.
            // A pending session is an internal handshake state — features
            // must never see it, send to it, or count it.
            get { lock (sync) return EstablishedSnapshot(); }
        }

        // Q9: connection-context send. SendToServer addresses the server
        // side: the established snapshot gates availability (empty -> the
        // frozen NoSession), and the frame stays ONE untargeted frame (0)
        // exactly as the client->server transport direction always routed it
        // — one logical target (the server), never one frame per session.
        // DEV-V2-16: the transport call runs OUTSIDE the state lock.
        public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable)
        {
            List<BueNetworkSession> established;
            lock (sync)
            {
                if (!channels.ContainsKey(channel.Value)) return NetworkSendResult.ChannelNotRegistered;
                established = EstablishedSnapshot();
            }
            if (established.Count == 0) return NetworkSendResult.NoSession;
            if (!IsEncapsulatable(channel.Value, payload)) return NetworkSendResult.PayloadTooLarge;
            // DEV-V3-04: the untargeted server-bound frame is accounted to the
            // established session it rides (the client topology has exactly
            // one server peer; multi-session clients are out of the frozen
            // scope). Clock sampled OUTSIDE the state lock (repo convention).
            var attributed = established[0].SessionId;
            var nowMs = monotonicMilliseconds();
            return SendSingle(channel.Value, payload, reliable, attributed, 0UL, nowMs);
        }

        // One budgeted, health-tracked executed send (SendToServer /
        // SendToClient share it): gates already decided, frame NOT built yet.
        private NetworkSendResult SendSingle(string channelId, byte[] payload, bool reliable, ulong generation, ulong target, long nowMs)
        {
            string diagnostic = null;
            lock (sync)
            {
                if (!sendGuard.TryConsume(generation, nowMs, out var used))
                {
                    diagnostic = ThrottleLine(channelId, generation, used);
                }
            }
            if (diagnostic != null)
            {
                EmitDiagnostic(diagnostic);
                return NetworkSendResult.Throttled;
            }
            var one = ExecuteFrame(KindData, channelId, payload, reliable, target);
            NoteOutcome(generation, one);
            return one;
        }

        private NetworkSendResult ExecuteFrame(byte kind, string channelId, byte[] payload, bool reliable, ulong target)
        {
            try
            {
                return SendFrame(kind, channelId, payload, reliable, target);
            }
            catch (Exception)
            {
                return NetworkSendResult.LocalTransportUnavailable;
            }
        }

        // The health note under the state lock; the level lines emit after
        // release (never a callback under the lock).
        private void NoteOutcome(ulong generation, NetworkSendResult one)
        {
            string degraded = null;
            string recovered = null;
            lock (sync)
            {
                sendGuard.NoteSendOutcome(generation, one == NetworkSendResult.Sent, one,
                    out var reportDegraded, out var reportRecovered, out var consecutiveFailures);
                if (reportDegraded) degraded = DegradedLine(generation, one, consecutiveFailures);
                else if (reportRecovered) recovered = RecoveredLine(generation, consecutiveFailures);
            }
            EmitDiagnostic(degraded);
            EmitDiagnostic(recovered);
        }

        // DEV-V2-16 ③: session-driven multicast. SendToClients sends ONE
        // targeted frame per established session (target = the session's
        // peer steam id) — never a single untargeted frame handed to the
        // transport as a broadcast. Result aggregation is frozen:
        // snapshot empty -> NoSession; every target delivered -> Sent; every
        // target failed -> LocalTransportUnavailable; mixed -> PartialFailure.
        // The state lock is taken ONCE (channel gate + snapshot) and never
        // held across a transport call; a session that vanishes after the
        // snapshot still receives its send attempt — the transport's
        // per-target outcome decides delivered/failed.
        public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
        {
            List<BueNetworkSession> targets;
            lock (sync)
            {
                if (!channels.ContainsKey(channel.Value)) return NetworkSendResult.ChannelNotRegistered;
                targets = EstablishedSnapshot();
            }
            if (targets.Count == 0) return NetworkSendResult.NoSession;
            if (!IsEncapsulatable(channel.Value, payload)) return NetworkSendResult.PayloadTooLarge;
            var nowMs = monotonicMilliseconds();
            return SendTargets(KindData, channel.Value, payload, reliable, targets, nowMs);
        }

        // Q9 + DEV-V2-16: per-session addressing WITHOUT a SteamId overload.
        // Precedence is frozen: the channel gate first (ChannelNotRegistered),
        // then the target context — a null context is NoSession. The context
        // must be (a) owned by this runtime — checked by identity, not id
        // equality, so a foreign or forged session object claiming a live
        // generation id is rejected — (b) established, and (c) the live
        // generation: SessionId IS the connection generation and dictionary
        // membership is authoritative, so a dropped/superseded session
        // object is rejected by the same lookup. The transport call runs
        // outside the state lock.
        public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable)
        {
            ulong target;
            ulong generation;
            lock (sync)
            {
                if (!channels.ContainsKey(channel.Value)) return NetworkSendResult.ChannelNotRegistered;
                if (session == null) return NetworkSendResult.NoSession;
                if (!sessions.TryGetValue(session.SessionId, out var live) || !ReferenceEquals(live, session))
                    return NetworkSendResult.NoSession;
                if (!live.Established) return NetworkSendResult.NoSession;
                target = live.PeerSteamId;
                generation = live.SessionId;
            }
            if (!IsEncapsulatable(channel.Value, payload)) return NetworkSendResult.PayloadTooLarge;
            var nowMs = monotonicMilliseconds();
            return SendSingle(channel.Value, payload, reliable, generation, target, nowMs);
        }

        // The established session snapshot, taken under the state lock; the
        // single filter source for Sessions and both snapshot-gated sends.
        private List<BueNetworkSession> EstablishedSnapshot()
        {
            var targets = new List<BueNetworkSession>(sessions.Count);
            foreach (var s in sessions.Values)
            {
                if (s.Established) targets.Add(s);
            }
            return targets;
        }

        // The payload must survive SendFrame's encapsulation for EVERY
        // target: validated once up front so an oversized payload keeps its
        // dedicated result instead of being aggregated into per-target
        // transport failures.
        private static bool IsEncapsulatable(string channelId, byte[] payload)
        {
            if (payload == null || payload.Length > 16 * 1024) return false;
            return Encoding.UTF8.GetByteCount(channelId ?? string.Empty) <= byte.MaxValue;
        }

        // DEV-V2-16: per-target aggregation. Each target is one transport
        // call outside the state lock; a transport that throws is one failed
        // target, never an exception on the caller's hot path (Q11: hot
        // paths never throw).
        // DEV-V3-04 aggregation extension (the frozen table stays intact for
        // the pre-existing combinations): budget decisions run under the
        // state lock in one pass (clock sampled outside), every refused
        // target carries its own throttled line, and the outcomes fold as
        //   every executed target delivered, zero refused -> Sent
        //   every executed target failed on the transport (>=1 executed)   -> LocalTransportUnavailable
        //   every target refused, none executed                            -> Throttled
        //   any delivered together with any refused/failed                 -> PartialFailure
        // A refused target is never folded into a plain success (不静默丢弃).
        private NetworkSendResult SendTargets(byte kind, string channelId, byte[] payload, bool reliable, List<BueNetworkSession> targets, long nowMs)
        {
            var executed = new bool[targets.Count];
            var results = new NetworkSendResult[targets.Count];
            var deferred = new List<string>(targets.Count);
            lock (sync)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    if (sendGuard.TryConsume(targets[i].SessionId, nowMs, out var used)) executed[i] = true;
                    else deferred.Add(ThrottleLine(channelId, targets[i].SessionId, used));
                }
            }
            var delivered = 0;
            var failed = 0;
            var executedCount = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                if (!executed[i]) continue;
                executedCount++;
                results[i] = ExecuteFrame(kind, channelId, payload, reliable, targets[i].PeerSteamId);
                if (results[i] == NetworkSendResult.Sent) delivered++;
                else failed++;
            }
            lock (sync)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    if (!executed[i]) continue;
                    sendGuard.NoteSendOutcome(targets[i].SessionId, results[i] == NetworkSendResult.Sent, results[i],
                        out var reportDegraded, out var reportRecovered, out var consecutiveFailures);
                    if (reportDegraded) deferred.Add(DegradedLine(targets[i].SessionId, results[i], consecutiveFailures));
                    else if (reportRecovered) deferred.Add(RecoveredLine(targets[i].SessionId, consecutiveFailures));
                }
            }
            for (var i = 0; i < deferred.Count; i++) EmitDiagnostic(deferred[i]);
            var refused = targets.Count - executedCount;
            if (executedCount == 0) return NetworkSendResult.Throttled;
            if (delivered > 0 && failed == 0 && refused == 0) return NetworkSendResult.Sent;
            if (delivered == 0) return NetworkSendResult.LocalTransportUnavailable;
            return NetworkSendResult.PartialFailure;
        }

        /// <summary>
        /// DEV-V2-14 ① contract seam: directional inbound subscription. Every
        /// call returns its own idempotent handle whose Dispose removes only
        /// its own delegate (the same handler subscribed twice is invoked
        /// once per handle). A null handler or an undefined direction value
        /// is a developer error and fails fast.
        /// </summary>
        public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (direction != ChannelDirection.FromClients && direction != ChannelDirection.FromServer)
                throw new ArgumentOutOfRangeException(nameof(direction), "undefined ChannelDirection value: " + (byte)direction);
            var record = new SubscriptionRecord(channel.Value, direction, handler);
            var table = direction == ChannelDirection.FromClients ? fromClientsHandlers : fromServerHandlers;
            lock (sync)
            {
                if (!table.TryGetValue(record.Channel, out var list))
                {
                    list = new List<SubscriptionRecord>();
                    table[record.Channel] = list;
                }
                list.Add(record);
            }
            return new Subscription(() => Unsubscribe(record));
        }

        private void Unsubscribe(SubscriptionRecord record)
        {
            var table = record.Direction == ChannelDirection.FromClients ? fromClientsHandlers : fromServerHandlers;
            lock (sync)
            {
                if (record.Removed) return;
                record.Removed = true;
                if (table.TryGetValue(record.Channel, out var list)) list.Remove(record);
            }
        }

        /// <summary>
        /// DEV-V2-14 host-internal module switch (never on the contract
        /// surface — features read state from Sessions and send results).
        /// Deactivating detaches the transport receive and drops all
        /// sessions; reactivating re-attaches and REBUILDS sessions by
        /// automatic handshake for peers the transport still reports
        /// connected (DEV-V2-17): the initiator re-probes with a fresh
        /// Hello, the responder resets each session-less connected peer so
        /// the peer's initiator self-heals. Channel and subscription tables
        /// survive untouched — live subscriptions need no re-subscription.
        /// The switch itself fires no lifecycle events (DEV-V2-14 frozen);
        /// dropped established sessions wait as the superseded generation
        /// and signal GenerationChanged when their successor establishes.
        /// </summary>
        public void SetModuleActive(bool active)
        {
            lock (sync)
            {
                if (moduleActive == active) return;
                moduleActive = active;
                if (!active)
                {
                    foreach (var session in sessions.Values)
                    {
                        if (session.Established) supersededByPeer[session.PeerSteamId] = session;
                    }
                    sessions.Clear();
                    sendGuard.DropAll(); // DEV-V3-04: the kill switch drops the guard's generation state with the sessions
                    if (receiveAttached) { transport.Receive -= OnReceive; receiveAttached = false; }
                }
                else if (!receiveAttached)
                {
                    transport.Receive += OnReceive;
                    receiveAttached = true;
                }
            }
            if (active) ReconcileAfterEnable();
        }

        // DEV-V2-17 re-enable reconciliation (see SetModuleActive). Runs
        // without the state lock; each per-peer step takes it as needed and
        // sends control frames outside.
        private void ReconcileAfterEnable()
        {
            var peers = transport.ConnectedPeers;
            foreach (var peer in peers)
            {
                if (handshakeInitiator) OnPeerConnected(peer);
                else SendSessionReset(peer);
            }
        }

        // The responder side of the re-enable reconciliation: a peer whose
        // runtime still holds a stale established session to us learns, via
        // a Reject matching no pending handshake, that our side holds no
        // session — the peer's initiator drops the stale session and
        // self-heals with a fresh handshake.
        private void SendSessionReset(ulong peerSteamId)
        {
            bool hasSession;
            lock (sync) hasSession = FindSessionByPeerLocked(peerSteamId) != null;
            if (hasSession) return;
            SendFrame(KindReject, string.Empty, EncodeControl(localSteamId, localContract, 0UL), true, peerSteamId);
        }

        /// <summary>
        /// Host-internal handshake initiator seam (manual path; the
        /// automatic path rides the same reconcile through OnPeerConnected).
        /// Creates a pending local session and sends a Hello frame; an
        /// existing PENDING session to the peer is returned as-is (the
        /// re-probe backoff owns it), an existing ESTABLISHED session is
        /// superseded by a fresh generation (Disconnected fires outside the
        /// lock; GenerationChanged on the dead object when the successor
        /// establishes). The peer validates the contract and replies Ack
        /// (session established, Connected fires) or Reject (the matching
        /// pending session tears down). Real connection setup is DEV-V2-04
        /// wiring; this is the pure-C# handshake seam driven by loopback
        /// tests.
        /// </summary>
        public IConnectionSession StartSession(ulong peerSteamId)
        {
            // The injected clock seam is sampled OUTSIDE the state lock (repo
            // convention: state protection never spans external code).
            var nowMs = monotonicMilliseconds();
            BueNetworkSession session;
            BueNetworkSession superseded;
            bool created;
            lock (sync)
            {
                // DEV-V2-14: a deactivated module holds no sessions and sends
                // no Hello — the caller sees an explicit null, not a ghost.
                if (!moduleActive) return null;
                session = ReconcileInitiatorSessionLocked(peerSteamId, out superseded, out created, nowMs);
            }
            if (superseded != null) superseded.FireDisconnected();
            if (created) SendHello(session);
            return session;
        }

        // DEV-V2-17: transport connected → automatic handshake (initiator
        // role; the responder answers Hello instead). The reconcile keeps
        // one live session per peer: none → create pending + Hello; a live
        // pending → keep it (the backoff owns retries); an established one →
        // supersede (a missed disconnect) with a fresh generation.
        private void OnPeerConnected(ulong peerSteamId)
        {
            // Clock sampled outside the state lock (repo convention).
            var nowMs = monotonicMilliseconds();
            BueNetworkSession session;
            BueNetworkSession superseded;
            bool created;
            lock (sync)
            {
                if (!moduleActive || !handshakeInitiator) return;
                session = ReconcileInitiatorSessionLocked(peerSteamId, out superseded, out created, nowMs);
            }
            if (superseded != null) superseded.FireDisconnected();
            if (created) SendHello(session);
        }

        // DEV-V2-17: transport dropped → cleanup. Every session to the peer
        // leaves the table (no ghost); established ones fire Disconnected
        // OUTSIDE the state lock and wait as the superseded generation for a
        // reconnecting successor.
        private void OnPeerDisconnected(ulong peerSteamId)
        {
            List<BueNetworkSession> dropped = null;
            lock (sync)
            {
                // DEV-V2-14: a deactivated module holds no sessions (and the
                // switch fires no lifecycle events of its own).
                if (!moduleActive) return;
                List<BueNetworkSession> matches = null;
                foreach (var session in sessions.Values)
                {
                    if (session.PeerSteamId != peerSteamId) continue;
                    (matches ??= new List<BueNetworkSession>()).Add(session);
                }
                if (matches == null) return;
                foreach (var session in matches)
                {
                    RemoveSessionLocked(session);
                    if (session.Established) (dropped ??= new List<BueNetworkSession>()).Add(session);
                }
            }
            if (dropped == null) return;
            foreach (var session in dropped) session.FireDisconnected();
        }

        // DEV-V2-17: the pending-handshake re-probe driver. Runtime-level
        // seam (like SetModuleActive, never on the IBueNetworkApi surface);
        // the production pump wiring lands with DEV-V2-18. Sessions past
        // their backoff deadline re-send Hello with doubling delay (1s → 8s
        // cap) while the handshake stays pending; established sessions never
        // re-probe. The deadline bookkeeping happens under the state lock,
        // the sends run outside it.
        public void TickHandshake()
        {
            // Clock sampled outside the state lock (repo convention); a
            // monotonic sample taken just before acquiring it is at most
            // conservative about a re-probe deadline.
            var now = monotonicMilliseconds();
            List<BueNetworkSession> due = null;
            lock (sync)
            {
                if (!moduleActive) return;
                foreach (var session in sessions.Values)
                {
                    if (session.Established || session.InboundDirection != ChannelDirection.FromServer) continue;
                    if (now - session.LastHelloAtMs < session.ReprobeBackoffMs) continue;
                    session.NoteHelloAttemptLocked(now);
                    (due ??= new List<BueNetworkSession>()).Add(session);
                }
            }
            if (due == null) return;
            foreach (var session in due) SendHello(session);
        }

        // One live session per peer (the DEV-V2-17 per-peer invariant that
        // the duplicate-Hello dedup and the reconnect replacement maintain).
        private BueNetworkSession FindSessionByPeerLocked(ulong peerSteamId)
        {
            foreach (var session in sessions.Values)
            {
                if (session.PeerSteamId == peerSteamId) return session;
            }
            return null;
        }

        // The initiator reconcile (see OnPeerConnected). Returns the session
        // to use; `created` is true when a NEW pending session was made (the
        // caller sends its Hello outside the lock), `superseded` carries the
        // dropped established session (the caller fires Disconnected outside
        // the lock).
        private BueNetworkSession ReconcileInitiatorSessionLocked(ulong peerSteamId, out BueNetworkSession superseded, out bool created, long nowMs)
        {
            created = false;
            superseded = null;
            var existing = FindSessionByPeerLocked(peerSteamId);
            if (existing != null)
            {
                if (!existing.Established) return existing;
                superseded = existing;
                RemoveSessionLocked(existing);
            }
            created = true;
            return CreateInitiatorSessionLocked(peerSteamId, nowMs);
        }

        private BueNetworkSession CreateInitiatorSessionLocked(ulong peerSteamId, long nowMs)
        {
            var id = unchecked((ulong)System.Threading.Interlocked.Increment(ref nextSessionId));
            // The initiator of the handshake is the client side of this
            // session, so its inbound frames come FROM SERVER. The session id
            // doubles as the handshake nonce — the Ack matches by peer + this
            // id, and the id IS the connection generation. `nowMs` was sampled
            // outside the state lock by the caller (repo convention).
            var session = new BueNetworkSession(id, peerSteamId, localContract, ChannelDirection.FromServer, id, nowMs);
            sessions[id] = session;
            return session;
        }

        private BueNetworkSession RemoveSessionLocked(BueNetworkSession session)
        {
            sessions.Remove(session.SessionId);
            if (session.Established) supersededByPeer[session.PeerSteamId] = session;
            sendGuard.DropGeneration(session.SessionId); // DEV-V3-04: 代际不留残留
            return session;
        }

        private void SendHello(BueNetworkSession session)
        {
            SendFrame(KindHello, string.Empty, EncodeControl(localSteamId, localContract, session.HandshakeNonce), true, 0UL);
        }

        // Fires the superseded generation's GenerationChanged with the
        // successor's session id; called when a successor session to the
        // same peer establishes. Lock-external by construction.
        private void ConsumeSupersededSession(ulong peerSteamId, ulong newSessionId)
        {
            BueNetworkSession superseded;
            lock (sync)
            {
                if (!supersededByPeer.TryGetValue(peerSteamId, out superseded)) return;
                supersededByPeer.Remove(peerSteamId);
            }
            superseded.FireGenerationChanged(newSessionId);
        }

        private void OnReceive(byte[] frame)
        {
            byte kind;
            string channelId;
            ulong sender;
            byte[] payload;
            lock (sync)
            {
                if (!moduleActive) return; // DEV-V2-14: a deactivated module receives nothing
                if (frame == null || frame.Length < 6 || Encoding.ASCII.GetString(frame, 0, 4) != BueFrameClassifier.FrameMagic) return;
                kind = frame[4];
                var channelLength = frame[5];
                if (frame.Length < 14 + channelLength) return; // header: magic 4 + kind 1 + chanLen 1 + sender 8
                channelId = Encoding.UTF8.GetString(frame, 6, channelLength);
                sender = Read64(frame, 6 + channelLength);
                var headerLength = 6 + channelLength + 8;
                var payloadLength = frame.Length - headerLength;
                if (payloadLength > 16 * 1024) return; // cap before allocation (S4)
                payload = new byte[payloadLength];
                Buffer.BlockCopy(frame, headerLength, payload, 0, payloadLength);
            }
            // Dispatch is a separate linearization seam: state protection never
            // spans external code (repo convention, cf. ReadyFrameFence). Handlers
            // and Connected are invoked OUTSIDE the lock.
            switch (kind)
            {
                case KindData:
                    DispatchData(channelId, payload, sender);
                    break;
                case KindHello:
                    HandleHello(payload, sender);
                    break;
                case KindAck:
                    HandleAck(payload, sender);
                    break;
                case KindReject:
                    HandleReject(payload, sender);
                    break;
            }
        }

        // DEV-V2-17 (R1-Spec BLOCKER fix): the control-frame peer identity is
        // the FRAME HEADER sender — the same transport-authoritative source
        // DATA dispatch resolves context by (DEV-V2-16 frozen). The payload's
        // steamId field is declarative only and never decides ownership.
        private void HandleHello(byte[] payload, ulong sender)
        {
            if (payload == null || payload.Length < 20) return;
            var peerContract = new ContractVersion(Read16(payload, 8), Read16(payload, 10));
            var nonce = Read64(payload, 12);
            var peerSteamId = sender;
            // Clock sampled outside the state lock (repo convention).
            var nowMs = monotonicMilliseconds();
            bool reject = false;
            BueNetworkSession created = null;
            BueNetworkSession replaced = null;
            BueNetworkSession reack = null;
            lock (sync)
            {
                // DEV-V2-14: the pump thread re-checked moduleActive in
                // OnReceive but released the lock since — a concurrent disable
                // must not create a session, send an Ack, or fire Connected.
                if (!moduleActive) return;
                if (peerContract.Major != localContract.Major)
                {
                    // Version fail-closed: no session is created; the Reject
                    // echoes the initiator's nonce so the exact pending
                    // handshake tears down (S3).
                    reject = true;
                }
                else
                {
                    var existing = FindSessionByPeerLocked(peerSteamId);
                    if (existing != null && existing.Established && existing.HandshakeNonce == nonce)
                    {
                        // Duplicate Hello (retransmit): idempotent re-Ack,
                        // never a second session.
                        reack = existing;
                    }
                    else
                    {
                        if (existing != null)
                        {
                            // A different handshake from a known peer — a
                            // reconnect with a new generation (the stale
                            // established session supersedes: Disconnected
                            // fires outside, the successor's establishment
                            // fires its GenerationChanged) or a stale pending
                            // one (drops silently).
                            replaced = existing;
                            RemoveSessionLocked(existing);
                        }
                        var id = unchecked((ulong)System.Threading.Interlocked.Increment(ref nextSessionId));
                        // The Hello responder is the server side of this session, so
                        // its inbound frames come FROM CLIENTS.
                        created = new BueNetworkSession(id, peerSteamId, peerContract, ChannelDirection.FromClients, nonce, nowMs);
                        sessions[id] = created;
                        // DEV-V2-16 R1 (Standards BLOCKING): Established is the public
                        // snapshot gate, so it is published under the state lock.
                        created.MarkEstablishedLocked();
                    }
                }
            }
            // Control-frame sends and lifecycle callbacks run OUTSIDE the
            // state lock (repo dispatch convention; the DEV-V2-16 named
            // deferral hands the handshake lock policy to this ticket).
            if (reject)
            {
                SendFrame(KindReject, string.Empty, EncodeControl(localSteamId, localContract, nonce), true, peerSteamId);
                return;
            }
            if (replaced != null && replaced.Established) replaced.FireDisconnected();
            if (created != null)
            {
                SendFrame(KindAck, string.Empty, EncodeControl(localSteamId, localContract, created.HandshakeNonce), true, created.PeerSteamId);
                created.FireConnected();
                ConsumeSupersededSession(created.PeerSteamId, created.SessionId);
            }
            if (reack != null)
            {
                SendFrame(KindAck, string.Empty, EncodeControl(localSteamId, localContract, reack.HandshakeNonce), true, reack.PeerSteamId);
            }
        }

        // DEV-V2-17 (R1-Spec BLOCKER fix): matching is by FRAME HEADER sender
        // (transport-authoritative peer identity) + handshake nonce — never
        // by the payload's declarative steamId, never by "first
        // un-established session to this peer" (a misdelivered Ack for a
        // different handshake must not establish anything).
        private void HandleAck(byte[] payload, ulong sender)
        {
            if (payload == null || payload.Length < 20) return;
            var peerContract = new ContractVersion(Read16(payload, 8), Read16(payload, 10));
            var nonce = Read64(payload, 12);
            BueNetworkSession target = null;
            lock (sync)
            {
                // DEV-V2-14: latching a session established (Connected fires
                // right after) must not happen on a deactivated module.
                if (!moduleActive) return;
                foreach (var session in sessions.Values)
                {
                    if (session.InboundDirection != ChannelDirection.FromServer) continue;
                    if (session.SessionId != nonce || session.PeerSteamId != sender || session.Established) continue;
                    target = session;
                    break;
                }
                if (target != null)
                {
                    if (peerContract.Major != localContract.Major)
                    {
                        // Initiator-side fail-closed: a negotiated major that
                        // does not match the local runtime never establishes;
                        // the pending session tears down (no half state, no
                        // ghost) and is not resurrected by a later Ack.
                        sessions.Remove(target.SessionId);
                        target = null;
                    }
                    else
                    {
                        // DEV-V2-16 R1 (Standards BLOCKING): publish the negotiated
                        // contract and the snapshot gate bit under the state lock.
                        target.EstablishWithContractLocked(peerContract);
                    }
                }
            }
            if (target == null) return;
            target.FireConnected(); // callback outside the lock
            ConsumeSupersededSession(target.PeerSteamId, target.SessionId);
        }

        // DEV-V2-17 (R1-Spec BLOCKER fix): the Reject's peer identity is the
        // FRAME HEADER sender (transport-authoritative), never the payload's
        // declarative steamId.
        private void HandleReject(byte[] payload, ulong sender)
        {
            if (payload == null || payload.Length < 20) return;
            var nonce = Read64(payload, 12);
            var peerSteamId = sender;
            BueNetworkSession dropped = null;
            bool heal = false;
            lock (sync)
            {
                // DEV-V2-14: same disable race as Hello/Ack — stay inert on a
                // deactivated module.
                if (!moduleActive) return;
                BueNetworkSession pending = null;
                foreach (var session in sessions.Values)
                {
                    if (session.InboundDirection != ChannelDirection.FromServer) continue;
                    if (session.SessionId != nonce || session.PeerSteamId != peerSteamId || session.Established) continue;
                    pending = session;
                    break;
                }
                if (pending != null)
                {
                    // A rejected Hello leaves no session on the peer; the
                    // MATCHING pending handshake tears down (S3) — never "all
                    // pending" (an in-flight handshake to another peer
                    // survives) and no retry (a version rejection is not
                    // transient).
                    sessions.Remove(pending.SessionId);
                    return;
                }
                var established = FindSessionByPeerLocked(peerSteamId);
                if (established == null || !established.Established) return;
                // A Reject that matches no pending handshake is a peer-side
                // session reset (the responder re-armed holding no sessions):
                // drop the stale established session; the initiator side
                // self-heals immediately with a fresh handshake, the responder
                // side waits for the peer's Hello (a responder never
                // initiates).
                dropped = established;
                RemoveSessionLocked(established);
                heal = handshakeInitiator;
            }
            dropped.FireDisconnected(); // callback outside the lock
            if (heal) StartSession(dropped.PeerSteamId);
        }

        private void DispatchData(string channelId, byte[] payload, ulong sender)
        {
            if (payload == null || payload.Length > 16 * 1024) return;
            List<SubscriptionRecord> copy;
            // DEV-V3-04: the session-typed context (the handler diagnostic
            // needs the internal direction/generation; the handler receives
            // it as the contract interface unchanged).
            BueNetworkSession context;
            lock (sync)
            {
                // DEV-V2-14: dispatch requires the receiving side to have
                // registered the channel AND traffic to arrive — the
                // subscription table is decoupled from the channel table.
                if (!channels.ContainsKey(channelId)) return;
                // Frame v2: the sender's steam id rides the header, so the
                // dispatch context resolves by source; a frame from an unknown
                // peer is dropped instead of falling back to "first session".
                // DEV-V2-17: the per-peer invariant keeps this unambiguous.
                var matched = FindSessionByPeerLocked(sender);
                if (matched == null) return;
                context = matched;
                // Direction = where the frame came from, carried by the
                // session's handshake origin.
                var table = matched.InboundDirection == ChannelDirection.FromServer ? fromServerHandlers : fromClientsHandlers;
                if (!table.TryGetValue(channelId, out var list) || list.Count == 0) return;
                copy = new List<SubscriptionRecord>(list);
            }
            // Handlers run outside the lock (repo dispatch convention).
            foreach (var record in copy)
            {
                if (record.Removed) continue; // disposed between snapshot and dispatch
                try { record.Handler(context, payload); }
                catch (Exception error)
                {
                    // DEV-V2-14 frozen isolation (never reaches peers or the
                    // transport pump) + DEV-V3-04: the swallow is gone — the
                    // fault surfaces as a structured diagnostic (channel /
                    // direction / generation / error type; no payload bytes,
                    // no re-throw back into the pump thread).
                    EmitDiagnostic("event=network-inbound result=handler-error channel=" + channelId
                        + " direction=" + context.InboundDirection + " generation=" + context.SessionId
                        + " errorType=" + error.GetType().Name + " message=" + error.Message
                        + " diagnosticId=BUE-NET-004");
                }
            }
        }

        // Control payload (DEV-V2-17): [steamId 8][major 2][minor 2][nonce 8]
        // — the nonce is the initiator's session id (the connection
        // generation), echoed verbatim by Ack/Reject so the matching
        // handshake resolves exactly. The steamId field is DECLARATIVE:
        // the frame header sender is the transport-authoritative peer
        // identity (R1-Spec BLOCKER fix) and never decides ownership here.
        private byte[] EncodeControl(ulong steamId, ContractVersion contract, ulong nonce)
        {
            var bytes = new byte[20];
            Write64(bytes, 0, steamId);
            Write16(bytes, 8, contract.Major);
            Write16(bytes, 10, contract.Minor);
            Write64(bytes, 12, nonce);
            return bytes;
        }

        private NetworkSendResult SendFrame(byte kind, string channelId, byte[] payload, bool reliable, ulong target)
        {
            var channelBytes = Encoding.UTF8.GetBytes(channelId ?? string.Empty);
            if (channelBytes.Length > byte.MaxValue) return NetworkSendResult.PayloadTooLarge;
            if (payload == null || payload.Length > 16 * 1024) return NetworkSendResult.PayloadTooLarge;
            var frame = new byte[6 + channelBytes.Length + 8 + payload.Length];
            Buffer.BlockCopy(BueFrameClassifier.MagicBytes, 0, frame, 0, 4);
            frame[4] = kind;
            frame[5] = (byte)channelBytes.Length;
            Buffer.BlockCopy(channelBytes, 0, frame, 6, channelBytes.Length);
            Write64(frame, 6 + channelBytes.Length, localSteamId);
            Buffer.BlockCopy(payload, 0, frame, 6 + channelBytes.Length + 8, payload.Length);
            return transport.Send(frame, reliable, target) ? NetworkSendResult.Sent : NetworkSendResult.LocalTransportUnavailable;
        }

        private static void Write16(byte[] b, int o, ushort v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); }
        private static ushort Read16(byte[] b, int o) { return (ushort)(b[o] | (b[o + 1] << 8)); }
        private static void Write64(byte[] b, int o, ulong v) { for (var i = 0; i < 8; i++) b[o + i] = (byte)(v >> (i * 8)); }
        private static ulong Read64(byte[] b, int o) { ulong v = 0; for (var i = 0; i < 8; i++) v |= ((ulong)b[o + i]) << (i * 8); return v; }

        private sealed class ChannelRegistration
        {
            public ChannelRegistration(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { Channel = channel; MinimumBueContract = minimumBueContract; FeatureVersion = featureVersion; }
            public FeatureId Channel { get; }
            public ContractVersion MinimumBueContract { get; }
            public ushort FeatureVersion { get; }
        }

        private sealed class BueNetworkSession : IConnectionSession
        {
            public BueNetworkSession(ulong sessionId, ulong peerSteamId, ContractVersion peerContract, ChannelDirection inboundDirection, ulong handshakeNonce, long nowMilliseconds)
            {
                SessionId = sessionId;
                PeerSteamId = peerSteamId;
                PeerContract = peerContract;
                InboundDirection = inboundDirection;
                HandshakeNonce = handshakeNonce;
                LastHelloAtMs = nowMilliseconds;
                ReprobeBackoffMs = HelloReprobeInitialMs;
            }
            public ulong SessionId { get; }
            public ulong PeerSteamId { get; }
            public ContractVersion PeerContract { get; private set; }
            public ushort PeerFeatureVersion { get { return 0; } }
            public IReadOnlyList<ChannelVersionEntry> Channels { get { return new ChannelVersionEntry[0]; } }
            // DEV-V2-14: where inbound frames on this session come from —
            // FromServer for the handshake initiator, FromClients for the
            // Hello responder. Frozen: direction is the frame source, never
            // the local role.
            internal ChannelDirection InboundDirection { get; }
            // DEV-V2-17: the handshake nonce — for the initiator its own
            // session id (the connection generation); for the responder the
            // initiator's id echoed in the Hello, re-sent verbatim by an
            // idempotent re-Ack.
            internal ulong HandshakeNonce { get; }
            internal bool Established { get; private set; }
            // DEV-V2-17: re-probe bookkeeping for the pending handshake (the
            // responder side never re-probes — it answers).
            internal long LastHelloAtMs { get; private set; }
            internal int ReprobeBackoffMs { get; private set; }
            internal void NoteHelloAttemptLocked(long nowMilliseconds)
            {
                LastHelloAtMs = nowMilliseconds;
                ReprobeBackoffMs = Math.Min(ReprobeBackoffMs * 2, HelloReprobeCapMs);
            }
            public event Action Connected;
            public event Action Disconnected;
            public event Action<ulong> GenerationChanged;
            public NetworkSendResult Send(byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            // DEV-V2-16 R1 (Standards BLOCKING): the establishment bit feeds
            // the public Sessions snapshot and the send gates, so it is
            // written under the runtime state lock (callers hold it); only
            // the lifecycle callbacks are deferred to outside the lock (repo
            // dispatch convention — state protection never spans external
            // code).
            internal void MarkEstablishedLocked() { Established = true; }
            internal void EstablishWithContractLocked(ContractVersion negotiated) { PeerContract = negotiated; Established = true; }
            internal void FireConnected()
            {
                var connected = Connected;
                if (connected != null) connected();
            }
            // DEV-V2-17: Disconnected fires when an established session is
            // dropped (transport disconnect, supersession, peer-side reset) —
            // only after Connected fired; a pending session vanishes
            // silently. GenerationChanged fires on a superseded (dead)
            // session object when the successor generation to the same peer
            // establishes, carrying the new session id.
            internal void FireDisconnected()
            {
                var disconnected = Disconnected;
                if (disconnected != null) disconnected();
            }
            internal void FireGenerationChanged(ulong newGeneration)
            {
                var changed = GenerationChanged;
                if (changed != null) changed(newGeneration);
            }
        }

        private sealed class SubscriptionRecord
        {
            public SubscriptionRecord(string channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            { Channel = channel; Direction = direction; Handler = handler; }
            public string Channel { get; }
            public ChannelDirection Direction { get; }
            public Action<IConnectionSession, byte[]> Handler { get; }
            // volatile: written under the state lock (Unsubscribe) but read
            // outside it (DispatchData's disposed-between-snapshot-and-
            // dispatch guard) — a plain bool left the read's visibility
            // undefined (R1-Standards SMELL fix).
            public volatile bool Removed;
        }

        private sealed class Subscription : IDisposable
        {
            private Action dispose;
            public Subscription(Action dispose) { this.dispose = dispose; }
            public void Dispose() { var action = dispose; dispose = null; if (action != null) action(); }
        }
    }
}
