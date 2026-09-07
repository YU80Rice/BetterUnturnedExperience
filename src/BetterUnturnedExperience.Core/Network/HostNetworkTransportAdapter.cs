using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-06: the real-transport seam for the BueNetworkApi runtime.
    /// Structurally the twin of <see cref="LmnTransportAdapter"/>, widened
    /// with the two engine-facing send axes the wire needs: the reliability
    /// bit (mapped to the engine's two-value reliability enum at the binding
    /// layer) and the target steam id (0 = untargeted / broadcast).
    /// Production binds the delegates to the game's transport via reflection;
    /// tests bind loopback delegates. Pure C# — no engine types cross this
    /// seam.
    /// DEV-V2-18: the lifecycle seam is live. The peer events are raised by
    /// <see cref="PollPeerState"/> diffing the injected
    /// <see cref="PeerStateSource"/> (production: the engine binding's peer
    /// resolvers; tests: stubs or the Raise* helpers), and
    /// <see cref="ConnectedPeers"/> answers with the live source snapshot the
    /// runtime's re-enable reconcile re-probes. The adapter stays passive
    /// about engine truth — it only shapes it into transport events.
    /// </summary>
    public sealed class HostNetworkTransportAdapter : INetworkTransport
    {
        private readonly Func<byte[], bool, ulong, bool> send;
        private readonly Action<Action<byte[]>> registerReceiver;
        private readonly Queue<byte[]> incoming = new Queue<byte[]>();
        private readonly object sync = new object();

        public HostNetworkTransportAdapter(Func<byte[], bool, ulong, bool> send, Action<Action<byte[]>> registerReceiver)
        {
            this.send = send ?? throw new ArgumentNullException(nameof(send));
            this.registerReceiver = registerReceiver ?? throw new ArgumentNullException(nameof(registerReceiver));
            this.registerReceiver(Enqueue);
        }

        public event Action<byte[]> Receive;
        // DEV-V2-18: raised by PollPeerState (or the test Raise* helpers) from
        // the engine's connection truth; the runtime's automatic handshake and
        // disconnect cleanup react to them.
        public event Action<ulong> PeerConnected;
        public event Action<ulong> PeerDisconnected;

        private static readonly ulong[] EmptyPeers = new ulong[0];
        // Last polled snapshot (main-thread only, like every engine touch in
        // the production pump). A raise that throws leaves it untouched, so
        // the next poll re-raises — the runtime's per-peer reconcile is
        // idempotent, which keeps a faulted poll self-correcting.
        private IReadOnlyList<ulong> lastPolledPeers = EmptyPeers;

        /// <summary>
        /// DEV-V2-18: the engine's current connected-peer snapshot (production:
        /// the BueEngineNetBinding peer resolvers; tests: stubs). Null keeps
        /// the adapter inert — ConnectedPeers empty, polls no-op.
        /// </summary>
        public Func<IReadOnlyList<ulong>> PeerStateSource { get; set; }

        public IReadOnlyList<ulong> ConnectedPeers
        {
            get
            {
                var source = PeerStateSource;
                var peers = source != null ? source() : null;
                return peers ?? EmptyPeers;
            }
        }

        /// <summary>
        /// DEV-V2-18: diffs the peer-state source against the last poll and
        /// raises PeerConnected/PeerDisconnected — the transport-level
        /// lifecycle truth the automatic handshake rides.
        /// </summary>
        public void PollPeerState()
        {
            var source = PeerStateSource;
            if (source == null) return;
            var current = source() ?? (IReadOnlyList<ulong>)EmptyPeers;
            foreach (var peer in lastPolledPeers)
            {
                if (!ContainsPeer(current, peer)) RaisePeerDisconnected(peer);
            }
            foreach (var peer in current)
            {
                if (!ContainsPeer(lastPolledPeers, peer)) RaisePeerConnected(peer);
            }
            lastPolledPeers = current;
        }

        /// <summary>DEV-V2-18: direct raise helpers (test seams; production raises via PollPeerState).</summary>
        public void RaisePeerConnected(ulong peerSteamId) { var raised = PeerConnected; if (raised != null) raised(peerSteamId); }
        public void RaisePeerDisconnected(ulong peerSteamId) { var raised = PeerDisconnected; if (raised != null) raised(peerSteamId); }

        private static bool ContainsPeer(IReadOnlyList<ulong> peers, ulong peer)
        {
            for (var index = 0; index < peers.Count; index++)
            {
                if (peers[index] == peer) return true;
            }
            return false;
        }

        public bool Send(byte[] frame, bool reliable, ulong targetSteamId) { return frame != null && send((byte[])frame.Clone(), reliable, targetSteamId); }
        public int Pump()
        {
            var count = 0;
            while (true)
            {
                byte[] frame;
                lock (sync) { if (incoming.Count == 0) break; frame = incoming.Dequeue(); }
                Dispatch(frame); count++;
            }
            return count;
        }

        private void Enqueue(byte[] frame)
        {
            if (frame == null) return;
            lock (sync) incoming.Enqueue((byte[])frame.Clone());
        }

        private void Dispatch(byte[] frame)
        {
            var callback = Receive;
            if (callback != null && frame != null) callback((byte[])frame.Clone());
        }
    }
}
