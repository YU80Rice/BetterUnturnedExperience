using System;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-05: pure-C# codec for the LMN V1 numeric-channel wire format,
    /// fixed here inside the host (never in Contracts — third parties only
    /// ever receive the canonical implementation). Legacy V1 plugins speak
    /// int virtual channels over the "MOD" magic frame:
    /// ["MOD" 3x][channel:1byte][payload] (LMN ModRouter.cs:13-16 magic,
    /// :40-51 BuildModPacket, :73-107 first-byte channel routing; the 0..255
    /// range matches LMN's ValidateLegacyChannel, ModTransport.cs:695-704).
    /// The codec never throws on wire paths — parse/build report failure via
    /// the bool return, so a malformed frame degrades to "not consumed".
    /// </summary>
    public static class LmnV1FrameCodec
    {
        private const byte Legacy0 = 0x4D; // M
        private const byte Legacy1 = 0x4F; // O
        private const byte Legacy2 = 0x44; // D

        /// <summary>Minimum routable V1 frame: magic plus the channel byte.</summary>
        private const int MinimumV1FrameLength = 4;

        public static bool IsV1Frame(byte[] frame)
        {
            return frame != null && frame.Length >= MinimumV1FrameLength
                && frame[0] == Legacy0 && frame[1] == Legacy1 && frame[2] == Legacy2;
        }

        /// <summary>
        /// Splits a V1 frame into its int channel and payload. The payload is
        /// a copy, so the caller cannot mutate the received frame buffer.
        /// </summary>
        public static bool TryParse(byte[] frame, out int channel, out byte[] payload)
        {
            channel = 0;
            payload = null;
            if (!IsV1Frame(frame)) return false;
            channel = frame[3];
            int payloadLength = frame.Length - MinimumV1FrameLength;
            payload = new byte[payloadLength];
            Array.Copy(frame, MinimumV1FrameLength, payload, 0, payloadLength);
            return true;
        }

        /// <summary>
        /// Builds the V1 wire frame for an outgoing legacy send. A null
        /// payload is treated as empty. Returns false for channels outside
        /// the 0..255 legacy range (and leaves frame null).
        /// </summary>
        public static bool TryBuild(int channel, byte[] payload, out byte[] frame)
        {
            frame = null;
            if (!IsLegacyChannel(channel)) return false;
            int payloadLength = payload != null ? payload.Length : 0;
            frame = new byte[MinimumV1FrameLength + payloadLength];
            frame[0] = Legacy0;
            frame[1] = Legacy1;
            frame[2] = Legacy2;
            frame[3] = (byte)channel;
            if (payloadLength > 0) Array.Copy(payload, 0, frame, MinimumV1FrameLength, payloadLength);
            return true;
        }

        private static bool IsLegacyChannel(int channel)
        {
            return channel >= 0 && channel <= 255;
        }
    }
}
