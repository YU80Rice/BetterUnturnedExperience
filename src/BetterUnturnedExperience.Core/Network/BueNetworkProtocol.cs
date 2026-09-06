using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace BetterUnturnedExperience.Core.Network
{
    public enum BueNetworkMessageKind : ushort
    {
        UpdateModuleConfig = 0x0101,
        ModuleConfigChanged = 0x0102,
        ModuleConfigRejected = 0x0103,
        RequestModuleConfigSnapshot = 0x0104,
        FeatureStatusProjection = 0x0201
    }

    public enum FrameDecodeError : byte { None, Truncated, UnsupportedFenceVersion, NonZeroFlags, NonZeroReserved, InvalidMessageKind, InvalidBinding, PayloadTooLarge }
    public enum FrameDecision : byte { Applied, Duplicate, RequestIdConflict, ReplayWindowFull, ReadInFlightDuplicate, ReadInFlightFull, NoReadyBinding, StaleGeneration, StaleSnapshot, NonceMismatch, InvalidRequestId }
    public readonly struct FrameHandlingResult { public FrameDecision Decision { get; } public FrameHandlingResult(FrameDecision decision) { Decision = decision; } }

    public sealed class ReadyBinding
    {
        public const int NonceLength = 16;
        private readonly byte[] clientNonce;
        private readonly byte[] serverNonce;
        private ReadyBinding(ulong generation, ulong snapshot, byte[] client, byte[] server)
        {
            if (generation == 0) throw new ArgumentOutOfRangeException(nameof(generation));
            if (snapshot == 0) throw new ArgumentOutOfRangeException(nameof(snapshot));
            Validate(client, nameof(client)); Validate(server, nameof(server));
            ConnectionGeneration = generation; SnapshotId = snapshot;
            clientNonce = (byte[])client.Clone(); serverNonce = (byte[])server.Clone();
        }
        public ulong ConnectionGeneration { get; }
        public ulong SnapshotId { get; }
        public byte[] CopyClientNonce() { return (byte[])clientNonce.Clone(); }
        public byte[] CopyServerNonce() { return (byte[])serverNonce.Clone(); }
        public static ReadyBinding Create(ulong generation, ulong snapshot, byte[] client, byte[] server) { return new ReadyBinding(generation, snapshot, client, server); }
        internal bool Matches(byte[] client, byte[] server) { return ByteEquality.FixedEquals(clientNonce, client) && ByteEquality.FixedEquals(serverNonce, server); }
        private static void Validate(byte[] value, string name) { if (value == null) throw new ArgumentNullException(name); if (value.Length != NonceLength) throw new ArgumentException("Nonce must be exactly 16 bytes.", name); }
    }

    public sealed class FencedFrame
    {
        private readonly byte[] clientNonce;
        private readonly byte[] serverNonce;
        private readonly byte[] payload;
        private FencedFrame(BueNetworkMessageKind kind, ulong generation, ulong snapshot, byte[] client, byte[] server, ulong request, byte[] body)
        {
            MessageKind = kind; ConnectionGeneration = generation; SnapshotId = snapshot; RequestId = request;
            clientNonce = client; serverNonce = server; payload = body;
        }
        public BueNetworkMessageKind MessageKind { get; }
        public ulong ConnectionGeneration { get; }
        public ulong SnapshotId { get; }
        public ulong RequestId { get; }
        public byte[] Payload { get { return (byte[])payload.Clone(); } }
        public static FencedFrame Create(BueNetworkMessageKind kind, ReadyBinding binding, ulong requestId, byte[] payload)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (!BueFrameCodec.IsFencedKind(kind)) throw new ArgumentException("Message kind cannot carry a ready fence.", nameof(kind));
            return new FencedFrame(kind, binding.ConnectionGeneration, binding.SnapshotId, binding.CopyClientNonce(), binding.CopyServerNonce(), requestId, (byte[])payload.Clone());
        }
        internal static FencedFrame FromWire(BueNetworkMessageKind kind, ulong generation, ulong snapshot, byte[] client, byte[] server, ulong requestId, byte[] body)
        {
            return new FencedFrame(kind, generation, snapshot, (byte[])client.Clone(), (byte[])server.Clone(), requestId, (byte[])body.Clone());
        }
        internal byte[] CopyClientNonce() { return (byte[])clientNonce.Clone(); }
        internal byte[] CopyServerNonce() { return (byte[])serverNonce.Clone(); }
        internal byte[] CopyPayload() { return (byte[])payload.Clone(); }
    }

    public static class BueFrameCodec
    {
        public const int PrefixLength = 52;
        public const int ReadyPayloadLength = 48;

        public static byte[] EncodeReadyPayload(ReadyBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            var bytes = new byte[ReadyPayloadLength];
            Write64(bytes, 0, binding.ConnectionGeneration);
            Write64(bytes, 8, binding.SnapshotId);
            Buffer.BlockCopy(binding.CopyClientNonce(), 0, bytes, 16, 16);
            Buffer.BlockCopy(binding.CopyServerNonce(), 0, bytes, 32, 16);
            return bytes;
        }

        public static bool TryDecodeReadyPayload(byte[] bytes, out ReadyBinding binding, out FrameDecodeError error)
        {
            binding = null;
            error = FrameDecodeError.None;
            if (bytes == null || bytes.Length != ReadyPayloadLength) { error = FrameDecodeError.Truncated; return false; }
            var client = new byte[16]; var server = new byte[16];
            Buffer.BlockCopy(bytes, 16, client, 0, 16); Buffer.BlockCopy(bytes, 32, server, 0, 16);
            try { binding = ReadyBinding.Create(Read64(bytes, 0), Read64(bytes, 8), client, server); return true; }
            catch (ArgumentOutOfRangeException) { error = FrameDecodeError.InvalidBinding; return false; }
            catch (ArgumentException) { error = FrameDecodeError.Truncated; return false; }
        }

        public static byte[] Encode(FencedFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            var body = frame.CopyPayload();
            var requestBytes = frame.MessageKind == BueNetworkMessageKind.UpdateModuleConfig || frame.MessageKind == BueNetworkMessageKind.RequestModuleConfigSnapshot ? 8 : 0;
            if (requestBytes != 0 && frame.RequestId == 0) throw new ArgumentException("Settings request frame requires a non-zero RequestId.", nameof(frame));
            var bytes = new byte[PrefixLength + requestBytes + body.Length];
            bytes[0] = 1; bytes[1] = 0; Write16(bytes, 2, 0); Write64(bytes, 4, frame.ConnectionGeneration); Write64(bytes, 12, frame.SnapshotId);
            Buffer.BlockCopy(frame.CopyClientNonce(), 0, bytes, 20, 16); Buffer.BlockCopy(frame.CopyServerNonce(), 0, bytes, 36, 16);
            if (requestBytes != 0) Write64(bytes, PrefixLength, frame.RequestId);
            Buffer.BlockCopy(body, 0, bytes, PrefixLength + requestBytes, body.Length); return bytes;
        }
        public static bool TryDecode(BueNetworkMessageKind kind, byte[] bytes, out FencedFrame frame, out FrameDecodeError error)
        {
            frame = null; error = FrameDecodeError.None;
            if (!IsFencedKind(kind)) { error = FrameDecodeError.InvalidMessageKind; return false; }
            if (bytes == null || bytes.Length < PrefixLength) { error = FrameDecodeError.Truncated; return false; }
            if (bytes.Length - PrefixLength > BueEnvelopeCodec.MaxPayloadLength) { error = FrameDecodeError.PayloadTooLarge; return false; }
            if (bytes[0] != 1) { error = FrameDecodeError.UnsupportedFenceVersion; return false; }
            if (bytes[1] != 0) { error = FrameDecodeError.NonZeroFlags; return false; }
            if (Read16(bytes, 2) != 0) { error = FrameDecodeError.NonZeroReserved; return false; }
            var generation = Read64(bytes, 4); var snapshot = Read64(bytes, 12);
            if (generation == 0 || snapshot == 0) { error = FrameDecodeError.InvalidBinding; return false; }
            var client = new byte[16]; var server = new byte[16]; Buffer.BlockCopy(bytes, 20, client, 0, 16); Buffer.BlockCopy(bytes, 36, server, 0, 16);
            var requestBytes = kind == BueNetworkMessageKind.UpdateModuleConfig || kind == BueNetworkMessageKind.RequestModuleConfigSnapshot ? 8 : 0;
            if (bytes.Length < PrefixLength + requestBytes) { error = FrameDecodeError.Truncated; return false; }
            var requestId = requestBytes == 0 ? 0UL : Read64(bytes, PrefixLength);
            if (requestBytes != 0 && requestId == 0) { error = FrameDecodeError.InvalidBinding; return false; }
            var body = new byte[bytes.Length - PrefixLength - requestBytes]; Buffer.BlockCopy(bytes, PrefixLength + requestBytes, body, 0, body.Length);
            frame = FencedFrame.FromWire(kind, generation, snapshot, client, server, requestId, body); return true;
        }
        internal static bool IsFencedKind(BueNetworkMessageKind kind)
        {
            return kind == BueNetworkMessageKind.UpdateModuleConfig || kind == BueNetworkMessageKind.ModuleConfigChanged || kind == BueNetworkMessageKind.ModuleConfigRejected || kind == BueNetworkMessageKind.RequestModuleConfigSnapshot || kind == BueNetworkMessageKind.FeatureStatusProjection;
        }
        private static void Write16(byte[] b, int o, ushort v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); }
        private static ushort Read16(byte[] b, int o) { return (ushort)(b[o] | (b[o + 1] << 8)); }
        private static void Write64(byte[] b, int o, ulong v) { for (var i = 0; i < 8; i++) b[o + i] = (byte)(v >> (i * 8)); }
        private static ulong Read64(byte[] b, int o) { ulong v = 0; for (var i = 0; i < 8; i++) v |= ((ulong)b[o + i]) << (i * 8); return v; }
    }

    public sealed class ReadyFrameFence
    {
        private readonly int writeCapacity; private readonly int readCapacity;
        private readonly Dictionary<ulong, byte[]> writes = new Dictionary<ulong, byte[]>(); private readonly HashSet<ulong> reads = new HashSet<ulong>();
        private readonly object sync = new object();
        // Dispatch is a separate linearization seam: state protection never spans external code.
        private readonly object dispatchSync = new object();
        private ReadyBinding binding;
        public ReadyFrameFence() : this(128, 16) { }
        internal ReadyFrameFence(int writeCapacity, int readCapacity) { if (writeCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(writeCapacity)); if (readCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(readCapacity)); this.writeCapacity = writeCapacity; this.readCapacity = readCapacity; }
        public int WriteReplayCount { get { lock (sync) return writes.Count; } } public int ReadInFlightCount { get { lock (sync) return reads.Count; } }
        public void ReplaceBinding(ReadyBinding next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            lock (dispatchSync)
            {
                lock (sync) { binding = next; writes.Clear(); reads.Clear(); }
            }
        }
        public FrameHandlingResult TryAccept(FencedFrame frame, Action<FencedFrame> handler)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame)); if (handler == null) throw new ArgumentNullException(nameof(handler));
            lock (dispatchSync)
            {
                lock (sync)
                {
                    if (binding == null) return new FrameHandlingResult(FrameDecision.NoReadyBinding);
                    if (frame.ConnectionGeneration != binding.ConnectionGeneration) return new FrameHandlingResult(FrameDecision.StaleGeneration);
                    if (frame.SnapshotId != binding.SnapshotId) return new FrameHandlingResult(FrameDecision.StaleSnapshot);
                    if (!binding.Matches(frame.CopyClientNonce(), frame.CopyServerNonce())) return new FrameHandlingResult(FrameDecision.NonceMismatch);
                    if (frame.MessageKind == BueNetworkMessageKind.RequestModuleConfigSnapshot)
                    {
                        if (frame.RequestId == 0) return new FrameHandlingResult(FrameDecision.InvalidRequestId);
                        if (reads.Contains(frame.RequestId)) return new FrameHandlingResult(FrameDecision.ReadInFlightDuplicate); if (reads.Count >= readCapacity) return new FrameHandlingResult(FrameDecision.ReadInFlightFull);
                        reads.Add(frame.RequestId);
                    }
                    else if (frame.MessageKind == BueNetworkMessageKind.UpdateModuleConfig)
                    {
                        if (frame.RequestId == 0) return new FrameHandlingResult(FrameDecision.InvalidRequestId);
                        using (var sha = SHA256.Create())
                        {
                            var digest = sha.ComputeHash(frame.CopyPayload());
                            if (writes.TryGetValue(frame.RequestId, out var prior)) return new FrameHandlingResult(ByteEquality.FixedEquals(digest, prior) ? FrameDecision.Duplicate : FrameDecision.RequestIdConflict);
                            if (writes.Count >= writeCapacity) return new FrameHandlingResult(FrameDecision.ReplayWindowFull); writes.Add(frame.RequestId, digest);
                        }
                    }
                }
                // The dispatch seam serializes handler entry with ReplaceBinding, while the state lock is released.
                // A failed mutation remains replay-occupied (fail-closed); a failed read releases only its in-flight slot.
                try { handler(frame); }
                catch
                {
                    if (frame.MessageKind == BueNetworkMessageKind.RequestModuleConfigSnapshot) CompleteRead(frame.RequestId);
                    throw;
                }
                return new FrameHandlingResult(FrameDecision.Applied);
            }
        }
        public bool CompleteRead(ulong requestId) { lock (sync) return reads.Remove(requestId); }

    }

    internal static class ByteEquality
    {
        internal static bool FixedEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            var difference = 0;
            for (var index = 0; index < left.Length; index++) difference |= left[index] ^ right[index];
            return difference == 0;
        }
    }
}
