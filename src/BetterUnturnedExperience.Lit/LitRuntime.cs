using System;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V2-15: the LIT domain's runtime seams, replacing the old plugin's
    /// BepInEx identity. The old LaunchInventoryTidyPlugin statics (Log,
    /// MainThreadId) are gone with [BepInPlugin]; the domain reads them from
    /// here, and the host composition binds the sinks to BueRuntimeLog's
    /// verbosity channels at registration time. Unbound sinks swallow
    /// silently — the same null-conditional contract the old plugin had.
    /// </summary>
    internal static class LitRuntime
    {
        /// <summary>Frozen feature identity (spec「功能身份与显示名」).</summary>
        internal const string FeatureIdValue = "io.github.yu80rice.bue.inventory-tidy";

        /// <summary>Official Chinese display name (fixed text, not identity).</summary>
        internal const string DisplayName = "背包整理";

        /// <summary>
        /// Tidy-all-pages marker from the old protocol (0xFF). Kept as the
        /// module-local request constant; the wire protocol itself belongs to
        /// the multiplayer ticket.
        /// </summary>
        internal const byte AllPages = 0xFF;

        /// <summary>Normal diagnostics (production: BueRuntimeLog.Runtime / Debug channel).</summary>
        internal static Action<string> LogSink;

        /// <summary>Errors (production: BueRuntimeLog.Error — always printed).</summary>
        internal static Action<string> ErrorLogSink;

        /// <summary>Cached Unity main-thread id; the transaction service refuses off-thread work with it.</summary>
        internal static int MainThreadId;

        internal static void LogInfo(string message)
        {
            var sink = LogSink;
            sink?.Invoke(message);
        }

        internal static void LogWarning(string message)
        {
            // Warning-worthy events must stay visible (the old plugin logged
            // them at Warning level); the runtime channel is Debug-silent, so
            // warnings ride the error channel with an explicit prefix.
            var sink = ErrorLogSink;
            sink?.Invoke("WARN " + message);
        }

        internal static void LogError(string message)
        {
            var sink = ErrorLogSink;
            sink?.Invoke(message);
        }
    }
}
