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

        public static bool IsLmnFrame(byte[] frame)
        {
            if (frame == null) return false;
            if (frame.Length >= 3 && frame[0] == Legacy0 && frame[1] == Legacy1 && frame[2] == Legacy2) return true;
            if (frame.Length >= 4 && frame[0] == Namespaced0 && frame[1] == Namespaced1 && frame[2] == Namespaced2 && frame[3] == Namespaced3) return true;
            return false;
        }
    }
}
