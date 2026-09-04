using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Core.Network;

namespace BetterUnturnedExperience.Transport
{
    // Experimental boundary only: the adapter accepts transport delegates and no native transport types.
    public sealed class LmnTransportAdapter : INetworkTransport
    {
        private readonly Func<byte[], bool, ulong, bool> send;
        private readonly Action<Action<byte[]>> registerReceiver;
        private readonly Queue<byte[]> incoming = new Queue<byte[]>();
        private readonly object sync = new object();

        public LmnTransportAdapter(Func<byte[], bool, ulong, bool> send, Action<Action<byte[]>> registerReceiver)
        {
            this.send = send ?? throw new ArgumentNullException(nameof(send));
            this.registerReceiver = registerReceiver ?? throw new ArgumentNullException(nameof(registerReceiver));
            this.registerReceiver(Enqueue);
        }

        public event Action<byte[]> Receive;
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
