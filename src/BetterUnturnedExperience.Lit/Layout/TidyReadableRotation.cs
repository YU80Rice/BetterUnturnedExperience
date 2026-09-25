using System;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// The tidy planner's default readable pair. Historical inventory rotations
    /// are retained separately as OriginalRot; planning uses the absolute
    /// readable 0/1 pair only.
    /// </summary>
    internal static class TidyReadableRotation
    {
        internal const byte Default = 0;

        internal static byte Normalize(byte historicalRotation)
        {
            return (byte)(historicalRotation & 1);
        }
    }
}
