using System;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-18: pure-C# BUE wire-frame recognition for the inbound decision
    /// core's step ①. The magic constant is the frame format's single home
    /// (BueNetworkRuntime encodes it on send and matches it on receive); the
    /// frozen wire magic is "BUE1" — the product language is 「BUE 帧」, and
    /// the 4-byte header layout is unchanged by the rename (the frame never
    /// shipped, so the rename has zero compatibility cost). Offset-aware
    /// zero-copy shape mirrors LmnFrameClassifier: the decision core
    /// classifies every inbound packet window without materializing a
    /// sub-array per frame.
    /// </summary>
    public static class BueFrameClassifier
    {
        public const string FrameMagic = "BUE1";

        // The wire bytes are DERIVED from the magic string — one source, no
        // parallel byte constants to drift (R1-Standards SMELL fix).
        internal static readonly byte[] MagicBytes = System.Text.Encoding.ASCII.GetBytes(FrameMagic);

        public static bool IsBueFrame(byte[] frame)
        {
            if (frame == null) return false;
            return IsBueFrame(frame, 0, frame.Length);
        }

        public static bool IsBueFrame(byte[] packet, int offset, int size)
        {
            return packet != null && offset >= 0 && size >= MagicBytes.Length && offset + size <= packet.Length
                && packet[offset] == MagicBytes[0] && packet[offset + 1] == MagicBytes[1] && packet[offset + 2] == MagicBytes[2] && packet[offset + 3] == MagicBytes[3];
        }
    }
}
