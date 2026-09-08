using System;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the LHT domain's runtime seams, replacing the old plugin's
    /// BepInEx identity (the LIT/LIR migration shape). The old
    /// LaunchHordeTrackerPlugin statics (Instance, Logger) are gone with
    /// [BepInPlugin]; the domain reads them from here, and the host
    /// composition binds the sinks to BueRuntimeLog's channels at
    /// registration time. Unbound sinks swallow silently — the same
    /// null-conditional contract the old plugin had.
    /// </summary>
    internal static class LhtRuntime
    {
        /// <summary>Frozen feature identity (spec「功能身份与显示名」). The old
        /// channel string io.github.yu80rice.launchhordetracker.horde-status
        /// retires with the standalone plugin; the FeatureId is ALSO the
        /// network channel identity and the Harmony ID.</summary>
        internal const string FeatureIdValue = "io.github.yu80rice.bue.horde-tracker";

        /// <summary>Official Chinese display name (fixed text, not identity).</summary>
        internal const string DisplayName = "更好的尸潮播报";

        /// <summary>Normal diagnostics (production: BueRuntimeLog.Runtime / Debug channel —
        /// the visibility band of the old plugin's LogDebug lines).</summary>
        internal static Action<string> LogSink;

        /// <summary>Errors (production: BueRuntimeLog.Error — always printed).</summary>
        internal static Action<string> ErrorLogSink;

        internal static void LogInfo(string message)
        {
            LogSink?.Invoke(message);
        }

        internal static void LogWarning(string message)
        {
            // Warning-worthy events must stay visible; the runtime channel is
            // Debug-silent, so warnings ride the error channel with a prefix
            // (the LIT/LIR runtime convention).
            ErrorLogSink?.Invoke("WARN " + message);
        }

        internal static void LogError(string message)
        {
            ErrorLogSink?.Invoke(message);
        }
    }
}
