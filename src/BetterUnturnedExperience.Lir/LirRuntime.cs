using System;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the LIR domain's runtime seams, replacing the old plugin's
    /// BepInEx identity (the same shape as the LIT migration). The old
    /// LaunchInPlaceReloadPlugin statics (Instance, Logger, LogNormalDiagnostic)
    /// are gone with [BepInPlugin]; the domain reads them from here, and the
    /// host composition binds the sinks to BueRuntimeLog's channels at
    /// registration time. Unbound sinks swallow silently — the same
    /// null-conditional contract the old plugin had.
    /// </summary>
    internal static class LirRuntime
    {
        /// <summary>Frozen feature identity (spec「功能身份与显示名」). The old
        /// channel string com.yu80rice.launchinplacereload.repack retires with
        /// the standalone plugin; the FeatureId is ALSO the network channel
        /// identity and the Harmony ID.</summary>
        internal const string FeatureIdValue = "io.github.yu80rice.bue.in-place-reload";

        /// <summary>Official Chinese display name (fixed text, not identity).</summary>
        internal const string DisplayName = "更好的换弹体验";

        /// <summary>Normal diagnostics (production: BueRuntimeLog.Runtime / Debug channel).</summary>
        internal static Action<string> LogSink;

        /// <summary>Errors (production: BueRuntimeLog.Error — always printed).</summary>
        internal static Action<string> ErrorLogSink;

        /// <summary>Cached Unity main-thread id; the deferred network init and the transaction paths compare against it.</summary>
        internal static int MainThreadId;

        // The old LogNormalDiagnostic throttle (5s window, repeat counter),
        // migrated off Unity's Time.realtimeSinceStartup onto a monotonic
        // Stopwatch so the domain stays engine-free on this path.
        private static readonly object diagGate = new object();
        private static long nextDiagnosticAtMs;
        private static int suppressedDiagnostics;

        internal static void LogInfo(string message)
        {
            var sink = LogSink;
            sink?.Invoke(message);
        }

        internal static void LogWarning(string message)
        {
            // Warning-worthy events must stay visible; the runtime channel is
            // Debug-silent, so warnings ride the error channel with a prefix
            // (the LIT runtime convention).
            var sink = ErrorLogSink;
            sink?.Invoke("WARN " + message);
        }

        internal static void LogError(string message)
        {
            var sink = ErrorLogSink;
            sink?.Invoke(message);
        }

        /// <summary>
        /// The old LogNormalDiagnostic: repeats inside the diagnostic window
        /// are suppressed with a counter; the first line after the window
        /// carries the suppressed count. Keeps a hot repeated path from
        /// flooding the log without hiding the fault entirely.
        /// </summary>
        internal static void LogDiagnostic(string message)
        {
            lock (diagGate)
            {
                var nowMs = System.Diagnostics.Stopwatch.GetTimestamp() * 1000L / System.Diagnostics.Stopwatch.Frequency;
                if (nowMs < nextDiagnosticAtMs)
                {
                    suppressedDiagnostics++;
                    return;
                }
                var suffix = suppressedDiagnostics == 0 ? string.Empty : "（此前抑制 " + suppressedDiagnostics + " 条重复诊断）";
                suppressedDiagnostics = 0;
                nextDiagnosticAtMs = nowMs + (long)(ReloadRuntimePolicy.DiagnosticIntervalSeconds * 1000f);
                LogInfo("[诊断] " + message + suffix);
            }
        }
    }
}
