using System;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the LIR domain's runtime seams, replacing the old plugin's
    /// BepInEx identity (the same shape as the LIT migration). The old
    /// LaunchInPlaceReloadPlugin statics (Instance, Logger, LogNormalDiagnostic)
    /// are gone with [BepInPlugin]; the domain reads them from here, and the
    /// host composition binds the sinks through the injected composition facts
    /// at registration time (DEV-V6-02C: the feature no longer names the host).
    /// Unbound sinks swallow silently — the same null-conditional contract the
    /// old plugin had.
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

        // ── DEV-V6-02C 宿主组合事实（V6-T2 硬项拆法：注入方向=宿主→换弹）──
        // 换弹工程不引用宿主：日志口、无画面判定、Steam 身份、设置根由组装根在
        // 登记前经 LirFeatureAssembly.BindHostComposition 注入，落点在本运行缝
        // （域内唯一事实面，与上面两个日志 sink 同一家族）。缺席语义各自诚实退化：
        // 日志静默（既有「未绑定即吞」契约）、无画面判定不裁剪（与原宿主字段默认
        // false 同）、Steam 解析跳过本地分支/远端 wrapper null（与测试宿主原值同）、
        // 设置根缺席=fail-closed（Start 显式失败，绝不静默发明持久化路径）。
        internal static Action<string> HostRuntimeLogSink;
        internal static Action<string> HostErrorLogSink;
        internal static Func<bool> HostHeadlessDecision;
        internal static Func<ulong> HostLocalSteamId;
        internal static Func<ulong, object> HostFindSteamPlayer;
        internal static Func<string> HostSettingsRoot;

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
