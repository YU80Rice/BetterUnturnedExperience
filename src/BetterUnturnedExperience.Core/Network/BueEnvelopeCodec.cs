using System;

namespace BetterUnturnedExperience.Core.Network
{
    public enum BueEnvelopeMessageKind : ushort
    {
        CapabilityHello = 0x0001,
        CapabilitySnapshot = 0x0002,
        CapabilityAck = 0x0003,
        SessionReady = 0x0004,
        UpdateModuleConfig = 0x0101,
        ModuleConfigChanged = 0x0102,
        ModuleConfigRejected = 0x0103,
        RequestModuleConfigSnapshot = 0x0104,
        FeatureStatusProjection = 0x0201
    }

    public enum EnvelopeDecodeError : byte { None, Truncated, LengthMismatch, PayloadTooLarge, UnknownMessageKind, ReservedBootstrapCollision }

    public sealed class BueEnvelope
    {
        public BueEnvelope(ushort contractMajor, ushort contractMinor, BueEnvelopeMessageKind messageKind, byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            ContractMajor = contractMajor; ContractMinor = contractMinor; MessageKind = messageKind; PayloadBytes = (byte[])payload.Clone();
        }
        internal BueEnvelope(ushort major, ushort minor, BueEnvelopeMessageKind kind, byte[] payload, bool owned)
        { ContractMajor = major; ContractMinor = minor; MessageKind = kind; PayloadBytes = owned ? payload : (byte[])payload.Clone(); }
        public ushort ContractMajor { get; }
        public ushort ContractMinor { get; }
        public BueEnvelopeMessageKind MessageKind { get; }
        public byte[] Payload { get { return (byte[])PayloadBytes.Clone(); } }
        internal byte[] PayloadBytes { get; }
    }

    public static class BueEnvelopeCodec
    {
        public const int HeaderLength = 10;
        public const int MaxPayloadLength = 16 * 1024;
        public static byte[] Encode(BueEnvelope envelope)
        {
            if (!TryEncode(envelope, out var bytes, out var error)) throw new ArgumentException(error.ToString(), nameof(envelope));
            return bytes;
        }
        public static bool TryEncode(BueEnvelope envelope, out byte[] bytes, out EnvelopeDecodeError error)
        {
            bytes = null; error = EnvelopeDecodeError.None;
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (envelope.ContractMajor == 0x5542 && envelope.ContractMinor == 0x4245) { error = EnvelopeDecodeError.ReservedBootstrapCollision; return false; }
            if (!IsSupportedKind(envelope.MessageKind)) { error = EnvelopeDecodeError.UnknownMessageKind; return false; }
            if (envelope.PayloadBytes.Length > MaxPayloadLength) { error = EnvelopeDecodeError.PayloadTooLarge; return false; }
            bytes = new byte[HeaderLength + envelope.PayloadBytes.Length];
            Write16(bytes, 0, envelope.ContractMajor); Write16(bytes, 2, envelope.ContractMinor); Write16(bytes, 4, (ushort)envelope.MessageKind); Write32(bytes, 6, (uint)envelope.PayloadBytes.Length);
            Buffer.BlockCopy(envelope.PayloadBytes, 0, bytes, HeaderLength, envelope.PayloadBytes.Length); return true;
        }
        public static bool TryDecode(byte[] bytes, out BueEnvelope envelope, out EnvelopeDecodeError error)
        {
            envelope = null; error = EnvelopeDecodeError.None;
            if (bytes == null || bytes.Length < HeaderLength) { error = EnvelopeDecodeError.Truncated; return false; }
            if (Read16(bytes, 0) == 0x5542 && Read16(bytes, 2) == 0x4245) { error = EnvelopeDecodeError.ReservedBootstrapCollision; return false; }
            var length = Read32(bytes, 6); if (length > MaxPayloadLength) { error = EnvelopeDecodeError.PayloadTooLarge; return false; }
            if (length != (uint)(bytes.Length - HeaderLength)) { error = EnvelopeDecodeError.LengthMismatch; return false; }
            var kind = Read16(bytes, 4); if (!IsSupportedKind(kind)) { error = EnvelopeDecodeError.UnknownMessageKind; return false; }
            var payload = new byte[length]; Buffer.BlockCopy(bytes, HeaderLength, payload, 0, payload.Length);
            envelope = new BueEnvelope(Read16(bytes, 0), Read16(bytes, 2), (BueEnvelopeMessageKind)kind, payload, true); return true;
        }
        private static bool IsSupportedKind(BueEnvelopeMessageKind kind) { return IsSupportedKind((ushort)kind); }
        private static bool IsSupportedKind(ushort kind)
        {
            switch (kind)
            {
                case 0x0001: case 0x0002: case 0x0003: case 0x0004:
                case 0x0101: case 0x0102: case 0x0103: case 0x0104:
                case 0x0201: return true;
                default: return false;
            }
        }
        private static void Write16(byte[] b, int o, ushort v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); }
        private static void Write32(byte[] b, int o, uint v) { for (var i = 0; i < 4; i++) b[o + i] = (byte)(v >> (i * 8)); }
        private static ushort Read16(byte[] b, int o) { return (ushort)(b[o] | (b[o + 1] << 8)); }
        private static uint Read32(byte[] b, int o) { uint v = 0; for (var i = 0; i < 4; i++) v |= ((uint)b[o + i]) << (i * 8); return v; }
    }

    public enum BootstrapDecodeError : byte { None, Truncated, InvalidMagic, UnsupportedVersion, InvalidKind, InvalidLength, InvalidBinding }
    public sealed class BueBootstrapReject
    {
        private BueBootstrapReject(ulong generation, ulong clientHigh, ulong clientLow, ushort errorCode, ushort supportedMajor)
        { if (generation == 0) throw new ArgumentOutOfRangeException(nameof(generation)); ConnectionGeneration = generation; ClientNonceHigh = clientHigh; ClientNonceLow = clientLow; ErrorCode = errorCode; SupportedContractMajor = supportedMajor; }
        public ulong ConnectionGeneration { get; } public ulong ClientNonceHigh { get; } public ulong ClientNonceLow { get; } public ushort ErrorCode { get; } public ushort SupportedContractMajor { get; }
        public static BueBootstrapReject Create(ulong generation, ulong clientHigh, ulong clientLow, ushort errorCode, ushort supportedMajor) { return new BueBootstrapReject(generation, clientHigh, clientLow, errorCode, supportedMajor); }
    }
    public static class BueBootstrapCodec
    {
        public const int FrameLength = 36; private const ushort PayloadLength = 28;
        public static byte[] Encode(BueBootstrapReject reject)
        {
            if (reject == null) throw new ArgumentNullException(nameof(reject)); var b = new byte[FrameLength]; b[0] = (byte)'B'; b[1] = (byte)'U'; b[2] = (byte)'E'; b[3] = (byte)'B'; b[4] = 1; b[5] = 1; Write16(b, 6, PayloadLength); Write64(b, 8, reject.ConnectionGeneration); Write64(b, 16, reject.ClientNonceHigh); Write64(b, 24, reject.ClientNonceLow); Write16(b, 32, reject.ErrorCode); Write16(b, 34, reject.SupportedContractMajor); return b;
        }
        public static bool TryDecode(byte[] b, out BueBootstrapReject reject, out BootstrapDecodeError error)
        {
            reject = null; error = BootstrapDecodeError.None; if (b == null || b.Length < FrameLength) { error = BootstrapDecodeError.Truncated; return false; } if (b.Length != FrameLength) { error = BootstrapDecodeError.InvalidLength; return false; } if (b[0] != (byte)'B' || b[1] != (byte)'U' || b[2] != (byte)'E' || b[3] != (byte)'B') { error = BootstrapDecodeError.InvalidMagic; return false; } if (b[4] != 1) { error = BootstrapDecodeError.UnsupportedVersion; return false; } if (b[5] != 1) { error = BootstrapDecodeError.InvalidKind; return false; } if (Read16(b, 6) != PayloadLength || Read64(b, 8) == 0) { error = BootstrapDecodeError.InvalidBinding; return false; } reject = BueBootstrapReject.Create(Read64(b, 8), Read64(b, 16), Read64(b, 24), Read16(b, 32), Read16(b, 34)); return true;
        }
        private static void Write16(byte[] b, int o, ushort v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); } private static void Write64(byte[] b, int o, ulong v) { for (var i = 0; i < 8; i++) b[o + i] = (byte)(v >> (i * 8)); } private static ushort Read16(byte[] b, int o) { return (ushort)(b[o] | (b[o + 1] << 8)); } private static ulong Read64(byte[] b, int o) { ulong v = 0; for (var i = 0; i < 8; i++) v |= ((ulong)b[o + i]) << (i * 8); return v; }
    }
}
