using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Core.Network
{
    // DEV-V2-17: the transport seam carries the peer lifecycle alongside the
    // frame path — PeerConnected / PeerDisconnected let the BueNetworkRuntime
    // own the automatic handshake (connected -> Hello) and the disconnect
    // cleanup, and ConnectedPeers lets a re-enabled module rebuild sessions
    // for connections that never dropped. Production bindings raise these
    // from the real transport (DEV-V2-18); tests raise them directly.
    public interface INetworkTransport { event Action<byte[]> Receive; bool Send(byte[] frame, bool reliable, ulong targetSteamId); int Pump(); event Action<ulong> PeerConnected; event Action<ulong> PeerDisconnected; IReadOnlyList<ulong> ConnectedPeers { get; } }
    public sealed class LocalLoopbackTransport : INetworkTransport
    {
        private readonly Queue<byte[]> incoming = new Queue<byte[]>(); private readonly object sync = new object(); private LocalLoopbackTransport peer;
        private readonly List<ulong> connectedPeers = new List<ulong>();
        public event Action<byte[]> Receive;
        public event Action<ulong> PeerConnected;
        public event Action<ulong> PeerDisconnected;
        private LocalLoopbackTransport() { }
        public static LocalLoopbackPair CreatePair() { var first = new LocalLoopbackTransport(); var second = new LocalLoopbackTransport(); first.peer = second; second.peer = first; return new LocalLoopbackPair(first, second); }
        // DEV-V2-06: the loopback is pair-coupled, so the reliability bit and
        // the target steam id are accepted for signature parity and ignored.
        public bool Send(byte[] frame, bool reliable, ulong targetSteamId) { if (frame == null || peer == null) return false; lock (peer.sync) peer.incoming.Enqueue((byte[])frame.Clone()); return true; }
        public int Pump() { var count = 0; while (true) { byte[] frame; lock (sync) { if (incoming.Count == 0) break; frame = incoming.Dequeue(); } var callback = Receive; if (callback != null) callback(frame); count++; } return count; }
        public IReadOnlyList<ulong> ConnectedPeers { get { lock (sync) return connectedPeers.ToArray(); } }
        // DEV-V2-17 test/loopback helpers: track the peer list the runtime
        // re-probes and raise the lifecycle events the runtime reacts to.
        public void ConnectPeer(ulong peerSteamId) { lock (sync) if (!connectedPeers.Contains(peerSteamId)) connectedPeers.Add(peerSteamId); var raised = PeerConnected; if (raised != null) raised(peerSteamId); }
        public void DisconnectPeer(ulong peerSteamId) { lock (sync) connectedPeers.Remove(peerSteamId); var raised = PeerDisconnected; if (raised != null) raised(peerSteamId); }
    }
    public sealed class LocalLoopbackPair { internal LocalLoopbackPair(INetworkTransport first, INetworkTransport second) { First = first; Second = second; } public INetworkTransport First { get; } public INetworkTransport Second { get; } }
}
