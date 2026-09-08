namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the LIR feature-private wire codec (the old AmmoRepackNetwork
    /// protocol layout, carried over the BUE named channel — FeatureId — instead
    /// of the retired LMN channel). The old ModTransport.BuildNamedMessage helper
    /// is gone per spec: the feature assembles its own byte[] (功能内自组,不占契约).
    /// Layout (fail-closed structural validation on read — truncation, trailing
    /// bytes, wrong version/type, requestId=0, total<=0 all refuse):
    ///   client → server: [protocolVersion:1][RequestRepackAmmo=1:1][requestId:8]
    ///   server → client: [protocolVersion:1][RepackSuccess=2:1][requestId:8][totalTransferred:4]
    /// </summary>
    internal static class LirRepackWireCodec
    {
        internal const byte ProtocolVersion = 1;
        internal const byte MsgRequestRepackAmmo = 1;
        internal const byte MsgRepackSuccess = 2;

        internal static byte[] BuildRequest(ulong requestId)
        {
            var payload = new byte[10];
            payload[0] = ProtocolVersion;
            payload[1] = MsgRequestRepackAmmo;
            WriteUInt64(payload, 2, requestId);
            return payload;
        }

        internal static bool TryReadRequest(byte[] payload, out ulong requestId)
        {
            requestId = 0UL;
            if (payload == null || payload.Length != 10) return false;
            if (payload[0] != ProtocolVersion || payload[1] != MsgRequestRepackAmmo) return false;
            requestId = ReadUInt64(payload, 2);
            return requestId != 0UL;
        }

        internal static byte[] BuildSuccess(ulong requestId, int totalTransferred)
        {
            var payload = new byte[14];
            payload[0] = ProtocolVersion;
            payload[1] = MsgRepackSuccess;
            WriteUInt64(payload, 2, requestId);
            WriteInt32(payload, 10, totalTransferred);
            return payload;
        }

        internal static bool TryReadSuccess(byte[] payload, out ulong requestId, out int totalTransferred)
        {
            requestId = 0UL;
            totalTransferred = 0;
            if (payload == null || payload.Length != 14) return false;
            if (payload[0] != ProtocolVersion || payload[1] != MsgRepackSuccess) return false;
            requestId = ReadUInt64(payload, 2);
            totalTransferred = ReadInt32(payload, 10);
            return requestId != 0UL && totalTransferred > 0;
        }

        // Explicit little-endian readers/writers (no BinaryReader on the hot
        // path; the old EndOfStream handling collapses into length checks).
        private static void WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            for (var i = 0; i < 8; i++) buffer[offset + i] = (byte)(value >> (8 * i));
        }

        private static ulong ReadUInt64(byte[] buffer, int offset)
        {
            ulong value = 0;
            for (var i = 0; i < 8; i++) value |= (ulong)buffer[offset + i] << (8 * i);
            return value;
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            for (var i = 0; i < 4; i++) buffer[offset + i] = (byte)(value >> (8 * i));
        }

        private static int ReadInt32(byte[] buffer, int offset)
        {
            var value = 0;
            for (var i = 0; i < 4; i++) value |= buffer[offset + i] << (8 * i);
            return value;
        }
    }
}
