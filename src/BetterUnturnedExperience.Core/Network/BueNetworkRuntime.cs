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
    /// Frame format (DEV-V2-06 v2) is internal: magic "BUE2" + kind byte +
    /// length-prefixed channel id + the sender's steam id (8 bytes LE) +
    /// payload — never exposed into Contracts. The sender field lets the
    /// receiver resolve the dispatch context by source instead of "first
    /// session"; sends carry the reliability bit and either an untargeted
    /// target (0) or the addressed session's peer steam id.
    /// Frame kinds: 0=Data, 1=Hello, 2=Ack, 3=Reject (control frames carry an
    /// empty channel id).
    /// </summary>
    public sealed class BueNetworkRuntime : IBueNetworkApi
    {
        private const string FrameMagic = "BUE2";
        private const byte KindData = 0;
        private const byte KindHello = 1;
        private const byte KindAck = 2;
        private const byte KindReject = 3;
        private readonly INetworkTransport transport;
        private readonly ContractVersion localContract;
        private readonly ulong localSteamId;
        private readonly Dictionary<string, ChannelRegistration> channels =
            new Dictionary<string, ChannelRegistration>(StringComparer.Ordinal);
        private readonly Dictionary<ulong, BueNetworkSession> sessions =
            new Dictionary<ulong, BueNetworkSession>();
        private readonly Dictionary<string, List<Action<IConnectionSession, byte[]>>> handlers =
            new Dictionary<string, List<Action<IConnectionSession, byte[]>>>(StringComparer.Ordinal);
        private readonly object sync = new object();
        // Session ids must be unique across runtime instances: an ownership
        // check (SendToClient looks up its own sessions by id) would otherwise
        // collide when two runtimes both start numbering at 1. Interlocked
        // keeps uniqueness even when two runtimes increment under different
        // instance locks.
        private static long nextSessionId = 1;

        public BueNetworkRuntime(INetworkTransport transport, ContractVersion localContract, ulong localSteamId)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.localContract = localContract;
            this.localSteamId = localSteamId;
            transport.Receive += OnReceive;
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
            get { lock (sync) { var list = new List<IConnectionSession>(sessions.Count); foreach (var s in sessions.Values) list.Add(s); return list; } }
        }

        // Q9: connection-context send. Session must be owned by this runtime;
        // otherwise the target context is unknown -> NoSession.
        public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable)
        {
            lock (sync)
            {
                if (!channels.ContainsKey(channel.Value)) return NetworkSendResult.ChannelNotRegistered;
                if (sessions.Count == 0) return NetworkSendResult.NoSession;
                return SendFrame(KindData, channel.Value, payload, reliable, 0UL);
            }
        }

        public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
        {
            lock (sync)
            {
                if (!channels.ContainsKey(channel.Value)) return NetworkSendResult.ChannelNotRegistered;
                if (sessions.Count == 0) return NetworkSendResult.NoSession;
                return SendFrame(KindData, channel.Value, payload, reliable, 0UL);
            }
        }

        public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable)
        {
            if (session == null) return NetworkSendResult.NoSession;
            lock (sync)
            {
                if (!channels.ContainsKey(channel.Value)) return NetworkSendResult.ChannelNotRegistered;
                if (!sessions.ContainsKey(session.SessionId)) return NetworkSendResult.NoSession;
                return SendFrame(KindData, channel.Value, payload, reliable, session.PeerSteamId);
            }
        }

        /// <summary>Host-internal receive subscription (not on the frozen contract
        /// surface — modules subscribe per channel to receive inbound frames).</summary>
        public IDisposable Subscribe(FeatureId channel, Action<IConnectionSession, byte[]> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            lock (sync)
            {
                if (!handlers.TryGetValue(channel.Value, out var list))
                {
                    list = new List<Action<IConnectionSession, byte[]>>();
                    handlers[channel.Value] = list;
                }
                list.Add(handler);
                return new Subscription(() =>
                {
                    lock (sync)
                    {
                        if (handlers.TryGetValue(channel.Value, out var current)) current.Remove(handler);
                    }
                });
            }
        }

        /// <summary>
        /// Host-internal handshake initiator: creates a pending local session
        /// and sends a Hello frame. The peer validates the contract and replies
        /// Ack (session established, Connected fires) or Reject (no session).
        /// Real connection setup is DEV-V2-04 wiring; this is the pure-C#
        /// handshake seam driven by loopback tests.
        /// </summary>
        public IConnectionSession StartSession(ulong peerSteamId)
        {
            lock (sync)
            {
                var id = unchecked((ulong)System.Threading.Interlocked.Increment(ref nextSessionId));
                var session = new BueNetworkSession(id, peerSteamId, localContract);
                sessions[id] = session;
                SendFrame(KindHello, string.Empty, EncodeControl(localSteamId, localContract), true, 0UL);
                return session;
            }
        }

        private void OnReceive(byte[] frame)
        {
            byte kind;
            string channelId;
            ulong sender;
            byte[] payload;
            lock (sync)
            {
                if (frame == null || frame.Length < 6 || Encoding.ASCII.GetString(frame, 0, 4) != FrameMagic) return;
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
                    HandleHello(payload);
                    break;
                case KindAck:
                    HandleAck(payload);
                    break;
                case KindReject:
                    HandleReject(payload);
                    break;
            }
        }

        private void HandleHello(byte[] payload)
        {
            ulong peerSteamId;
            ContractVersion peerContract;
            BueNetworkSession session = null;
            lock (sync)
            {
                if (payload == null || payload.Length < 12) return;
                peerSteamId = Read64(payload, 0);
                peerContract = new ContractVersion(Read16(payload, 8), Read16(payload, 10));
                if (peerContract.Major != localContract.Major)
                {
                    // Reject carries the initiator's steam id so its pending
                    // session can be cleaned up (S3).
                    SendFrame(KindReject, string.Empty, EncodeControl(peerSteamId, localContract), true, 0UL);
                    return;
                }
                var id = unchecked((ulong)System.Threading.Interlocked.Increment(ref nextSessionId));
                session = new BueNetworkSession(id, peerSteamId, peerContract);
                sessions[id] = session;
                SendFrame(KindAck, string.Empty, EncodeControl(localSteamId, localContract), true, 0UL);
            }
            if (session != null) session.MarkEstablished(); // fires Connected outside the lock
        }

        private void HandleAck(byte[] payload)
        {
            BueNetworkSession target = null;
            ContractVersion peerContract = default(ContractVersion);
            lock (sync)
            {
                if (payload == null || payload.Length < 12) return;
                var peerSteamId = Read64(payload, 0);
                peerContract = new ContractVersion(Read16(payload, 8), Read16(payload, 10));
                // S2: match the FIRST un-established session to this peer.
                foreach (var session in sessions.Values)
                {
                    if (session.PeerSteamId == peerSteamId && !session.Established) { target = session; break; }
                }
            }
            if (target != null) target.MarkEstablishedWithContract(peerContract); // fires Connected outside the lock
        }

        private void HandleReject(byte[] payload)
        {
            // A rejected Hello leaves no session on the peer; on the initiator,
            // tear down the pending session so it does not linger as a ghost
            // (S3). Reject only answers the in-flight handshake, so any
            // un-established session is the one to clear.
            lock (sync)
            {
                var ghosts = new List<ulong>();
                foreach (var pair in sessions)
                {
                    if (!pair.Value.Established) ghosts.Add(pair.Key);
                }
                foreach (var id in ghosts) sessions.Remove(id);
            }
        }

        private void DispatchData(string channelId, byte[] payload, ulong sender)
        {
            if (payload == null || payload.Length > 16 * 1024) return;
            List<Action<IConnectionSession, byte[]>> copy;
            IConnectionSession context;
            lock (sync)
            {
                if (!handlers.TryGetValue(channelId, out var list) || list.Count == 0) return;
                // Frame v2: the sender's steam id rides the header, so the
                // dispatch context resolves by source; a frame from an unknown
                // peer is dropped instead of falling back to "first session".
                context = null;
                foreach (var s in sessions.Values)
                {
                    if (s.PeerSteamId == sender) { context = s; break; }
                }
                if (context == null) return;
                copy = new List<Action<IConnectionSession, byte[]>>(list);
            }
            // Handlers run outside the lock (repo dispatch convention).
            foreach (var handler in copy) handler(context, payload);
        }

        private byte[] EncodeControl(ulong steamId, ContractVersion contract)
        {
            var bytes = new byte[12];
            Write64(bytes, 0, steamId);
            Write16(bytes, 8, contract.Major);
            Write16(bytes, 10, contract.Minor);
            return bytes;
        }

        private NetworkSendResult SendFrame(byte kind, string channelId, byte[] payload, bool reliable, ulong target)
        {
            var channelBytes = Encoding.UTF8.GetBytes(channelId ?? string.Empty);
            if (channelBytes.Length > byte.MaxValue) return NetworkSendResult.PayloadTooLarge;
            if (payload == null || payload.Length > 16 * 1024) return NetworkSendResult.PayloadTooLarge;
            var frame = new byte[6 + channelBytes.Length + 8 + payload.Length];
            Encoding.ASCII.GetBytes(FrameMagic, 0, 4, frame, 0);
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
            public BueNetworkSession(ulong sessionId, ulong peerSteamId, ContractVersion peerContract)
            { SessionId = sessionId; PeerSteamId = peerSteamId; PeerContract = peerContract; }
            public ulong SessionId { get; }
            public ulong PeerSteamId { get; }
            public ContractVersion PeerContract { get; private set; }
            public ushort PeerFeatureVersion { get { return 0; } }
            public IReadOnlyList<ChannelVersionEntry> Channels { get { return new ChannelVersionEntry[0]; } }
            internal bool Established { get; private set; }
            public event Action Connected;
#pragma warning disable 0067 // Raised by DEV-V2-04 lifecycle wiring (session teardown / reconnection).
            public event Action Disconnected;
            public event Action<ulong> GenerationChanged;
#pragma warning restore 0067
            public NetworkSendResult Send(byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            internal void MarkEstablished()
            {
                Established = true;
                var connected = Connected;
                if (connected != null) connected();
            }
            internal void MarkEstablishedWithContract(ContractVersion negotiated)
            {
                PeerContract = negotiated;
                MarkEstablished();
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action dispose;
            public Subscription(Action dispose) { this.dispose = dispose; }
            public void Dispose() { var action = dispose; dispose = null; if (action != null) action(); }
        }
    }
}
