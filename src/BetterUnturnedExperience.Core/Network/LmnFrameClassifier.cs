using System;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-04: pure-C# frame classifier for the LMN takeover decision core.
    /// LMN identifies its own traffic by magic bytes (LMN ModRouter.cs:13-24):
    /// V1 legacy "MOD" (0x4D 0x4F 0x44) and V2 namespaced "LMN2"
    /// (0x4C 0x4D 0x4E 0x32). BUE's Priority.First prefix short-circuits only
    /// these frames; everything else passes through untouched.
    /// </summary>
    public static class LmnFrameClassifier
    {
        private const byte Legacy0 = 0x4D; // M
        private const byte Legacy1 = 0x4F; // O
        private const byte Legacy2 = 0x44; // D

        private const byte Namespaced0 = 0x4C; // L
        private const byte Namespaced1 = 0x4D; // M
        private const byte Namespaced2 = 0x4E; // N
        private const byte Namespaced3 = 0x32; // 2

        private const int LegacyFrameLength = 3;
        private const int NamespacedFrameLength = 4;

        public static bool IsLmnFrame(byte[] frame)
        {
            if (frame == null) return false;
            return IsLmnFrame(frame, 0, frame.Length);
        }

        // DEV-V2-06: offset-aware hot-path overloads — the takeover decision
        // core classifies every inbound packet window zero-copy, without
        // materializing a sub-array per frame.
        public static bool IsLmnFrame(byte[] packet, int offset, int size)
        {
            return IsLegacyV1Frame(packet, offset, size) || IsNamespacedV2Frame(packet, offset, size);
        }

        public static bool IsLegacyV1Frame(byte[] packet, int offset, int size)
        {
            return packet != null && offset >= 0 && size >= LegacyFrameLength && offset + size <= packet.Length
                && packet[offset] == Legacy0 && packet[offset + 1] == Legacy1 && packet[offset + 2] == Legacy2;
        }

        public static bool IsNamespacedV2Frame(byte[] packet, int offset, int size)
        {
            return packet != null && offset >= 0 && size >= NamespacedFrameLength && offset + size <= packet.Length
                && packet[offset] == Namespaced0 && packet[offset + 1] == Namespaced1 && packet[offset + 2] == Namespaced2 && packet[offset + 3] == Namespaced3;
        }
    }
}
