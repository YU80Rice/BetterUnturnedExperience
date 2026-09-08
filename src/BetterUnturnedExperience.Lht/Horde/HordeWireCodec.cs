using System.IO;
using System.Text;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the functional-private wire codec (the old
    /// HordeStatusNetwork payload layer; ModTransport.BuildNamedMessage is
    /// gone — the feature assembles its own byte[] per the spec「BuildNamedMessage
    /// 类纯组包 helper 改为功能内自组 byte[]」). The BUE envelope wraps this
    /// payload under the FeatureId channel; the payload keeps its own
    /// protocol version byte (lhtPayloadProtocol=1).
    ///
    /// 协议（负载字节布局不变，08 基线）：
    ///   [byte:1 protocolVersion][byte:messageType]
    ///   type 1 (Update):
    ///     [uint:epoch][uint:sequence][ushort:remaining][ushort:total]
    ///     [string:location(≤128 字符)][string:initiator(≤64 字符)]
    ///   type 2 (Clear):
    ///     [uint:epoch][uint:sequence]
    ///
    /// All reads are fail-closed: truncation, unknown versions, over-long
    /// strings and trailing bytes are refused.
    /// </summary>
    internal static class HordeWireCodec
    {
        internal const byte ProtocolVersion = 1;
        internal const byte MessageUpdate = 1;
        internal const byte MessageClear = 2;

        private const int LocationMaxChars = 128;
        private const int InitiatorMaxChars = 64;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        internal static byte[] BuildUpdate(HordeSnapshot snapshot)
        {
            using (var stream = new MemoryStream(64))
            using (var writer = new BinaryWriter(stream, StrictUtf8))
            {
                writer.Write(ProtocolVersion);
                writer.Write(MessageUpdate);
                writer.Write(snapshot.Epoch);
                writer.Write(snapshot.Sequence);
                writer.Write(snapshot.Remaining);
                writer.Write(snapshot.Total);
                writer.Write(snapshot.Location ?? string.Empty);
                writer.Write(snapshot.Initiator ?? string.Empty);
                writer.Flush();
                return stream.ToArray();
            }
        }

        internal static byte[] BuildClear(uint epoch, uint sequence)
        {
            using (var stream = new MemoryStream(16))
            using (var writer = new BinaryWriter(stream, StrictUtf8))
            {
                writer.Write(ProtocolVersion);
                writer.Write(MessageClear);
                writer.Write(epoch);
                writer.Write(sequence);
                writer.Flush();
                return stream.ToArray();
            }
        }

        internal static bool TryReadUpdate(byte[] payload, out HordeSnapshot snapshot)
        {
            snapshot = null;
            using (var reader = new BinaryReader(new MemoryStream(payload ?? new byte[0]), StrictUtf8))
            {
                if (!HasRemaining(reader, 2)) return false;

                byte protocolVersion = reader.ReadByte();
                if (protocolVersion != ProtocolVersion) return false;

                byte messageType = reader.ReadByte();
                if (messageType != MessageUpdate) return false;

                if (!HasRemaining(reader, 12)) return false;

                uint epoch = reader.ReadUInt32();
                uint sequence = reader.ReadUInt32();
                ushort remaining = reader.ReadUInt16();
                ushort total = reader.ReadUInt16();

                if (!TryReadBoundedString(reader, LocationMaxChars, out string location)) return false;
                if (!TryReadBoundedString(reader, InitiatorMaxChars, out string initiator)) return false;
                if (HasTrailingBytes(reader)) return false;

                snapshot = new HordeSnapshot(true, epoch, sequence, remaining, total, location, initiator);
                return true;
            }
        }

        internal static bool TryReadClear(byte[] payload, out uint epoch, out uint sequence)
        {
            epoch = 0;
            sequence = 0;
            using (var reader = new BinaryReader(new MemoryStream(payload ?? new byte[0]), StrictUtf8))
            {
                if (!HasRemaining(reader, 2)) return false;

                byte protocolVersion = reader.ReadByte();
                if (protocolVersion != ProtocolVersion) return false;

                byte messageType = reader.ReadByte();
                if (messageType != MessageClear) return false;

                if (!HasRemaining(reader, 8)) return false;

                epoch = reader.ReadUInt32();
                sequence = reader.ReadUInt32();
                if (HasTrailingBytes(reader)) return false;
                return true;
            }
        }

        private static bool HasRemaining(BinaryReader reader, int byteCount)
        {
            Stream stream = reader.BaseStream;
            return stream != null && stream.Length - stream.Position >= byteCount;
        }

        private static bool HasTrailingBytes(BinaryReader reader)
        {
            Stream stream = reader.BaseStream;
            return stream == null || stream.Position != stream.Length;
        }

        private static bool TryReadBoundedString(BinaryReader reader, int maximumCharacters, out string value)
        {
            value = string.Empty;
            if (!TryRead7BitEncodedLength(reader, out int byteLength)) return false;

            if (byteLength > maximumCharacters * 4 || !HasRemaining(reader, byteLength)) return false;

            byte[] bytes = reader.ReadBytes(byteLength);
            try
            {
                value = StrictUtf8.GetString(bytes);
                return value.Length <= maximumCharacters;
            }
            catch (System.Text.DecoderFallbackException)
            {
                return false;
            }
        }

        private static bool TryRead7BitEncodedLength(BinaryReader reader, out int value)
        {
            value = 0;
            for (int index = 0; index < 5; index++)
            {
                if (!HasRemaining(reader, 1)) return false;

                byte current = reader.ReadByte();
                if (index == 4 && current > 0x07) return false;

                value |= (current & 0x7F) << (index * 7);
                if ((current & 0x80) == 0) return true;
            }

            return false;
        }
    }
}
