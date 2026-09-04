using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Core.Network
{
    public interface INetworkTransport { event Action<byte[]> Receive; bool Send(byte[] frame, bool reliable, ulong targetSteamId); int Pump(); }
    public sealed class LocalLoopbackTransport : INetworkTransport
    {
        private readonly Queue<byte[]> incoming = new Queue<byte[]>(); private readonly object sync = new object(); private LocalLoopbackTransport peer;
        public event Action<byte[]> Receive;
        private LocalLoopbackTransport() { }
        public static LocalLoopbackPair CreatePair() { var first = new LocalLoopbackTransport(); var second = new LocalLoopbackTransport(); first.peer = second; second.peer = first; return new LocalLoopbackPair(first, second); }
        // DEV-V2-06: the loopback is pair-coupled, so the reliability bit and
        // the target steam id are accepted for signature parity and ignored.
        public bool Send(byte[] frame, bool reliable, ulong targetSteamId) { if (frame == null || peer == null) return false; lock (peer.sync) peer.incoming.Enqueue((byte[])frame.Clone()); return true; }
        public int Pump() { var count = 0; while (true) { byte[] frame; lock (sync) { if (incoming.Count == 0) break; frame = incoming.Dequeue(); } var callback = Receive; if (callback != null) callback(frame); count++; } return count; }
    }
    public sealed class LocalLoopbackPair { internal LocalLoopbackPair(INetworkTransport first, INetworkTransport second) { First = first; Second = second; } public INetworkTransport First { get; } public INetworkTransport Second { get; } }
}
