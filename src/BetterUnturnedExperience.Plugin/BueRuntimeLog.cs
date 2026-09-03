using System;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-16G ticket B: runtime verbosity seam. Implements the user's log
    /// policy — load/inject stages announce at Info, errors print a reason at
    /// Warning/Error, but in-game RUNTIME events are Debug so normal play is
    /// silent unless BepInEx [Logging] Levels includes Debug (or a recorder is
    /// bound for tests).
    /// </summary>
    internal static class BueRuntimeLog
    {
        // Test seam: when set, every emission is routed here with its level
        // prefix (Debug/Info/Error) so host tests can assert the contract.
        internal static Action<string> Recorder = null;

        private static BepInEx.Logging.ManualLogSource log;

        internal static void Bind(BepInEx.Logging.ManualLogSource source)
        {
            log = source;
        }

        // In-game runtime events (drag ticks, placement decisions, preview
        // readouts, heartbeats): Debug level -> silent in normal play.
        internal static void Runtime(string line)
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                recorder("Debug " + line);
                return;
            }
            log?.LogDebug(line);
        }

        // DEV-16G ticket D: production load one-shots are all demoted to Debug;
        // this Info emitter is retained as the level-prefix contract anchor for
        // the ticket-B verbosity test and as the seam the aggregate ready line
        // conceptually belongs to. Normal play sees only AnnounceReady.
        internal static void Load(string line)
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                recorder("Info " + line);
                return;
            }
            log?.LogInfo(line);
        }

        // Errors / isolations: always printed with the reason, never swallowed
        // by the runtime-silent gate.
        internal static void Error(string line)
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                recorder("Error " + line);
                return;
            }
            log?.LogError(line);
        }

        // DEV-16G ticket D: error lines get a human-readable Chinese prefix in
        // front of the structured tokens so a user sees "BUE 错误:" at a glance
        // while the machine-readable featureId/diagnosticId/reason stay intact.
        internal static void ErrorFriendly(string line)
        {
            Error("BUE 错误：" + line);
        }

        // Pure classifier for BueNativeManagementPanel events: recurring
        // in-game events (menu open / UI rebuild / surface open / heartbeat)
        // are Runtime (Debug, silent); load one-shots are Info. Used by the
        // panel's LogTrace to route the same event names consistently with the
        // rest of the plugin's verbosity policy.
        internal static bool IsRuntimeEvent(string eventName)
        {
            switch (eventName)
            {
                case "surface-opened":
                case "constructor-postfix":
                case "create-button-begin":
                case "create-button-result":
                case "add-child-success":
                case "container-state":
                case "heartbeat":
                // DEV-16G ticket D: every per-subsystem load one-shot is
                // demoted to Debug; only the aggregate ready line stays Info.
                case "constructed":
                case "initialize-complete":
                case "patch-installed":
                case "host-ui-tick":
                case "first-tick":
                case "hooks-installed":
                case "polling-hook-installed":
                case "surface-context-dispatched":
                case "surface-ready":
                case "surface-discarded":
                case "wiring-enabled":
                    return true;
                default:
                    return false;
            }
        }

        // DEV-16G ticket D: surface-not-ready reasons that indicate a genuine
        // structural fault (loud Error) vs benign transient states (Debug).
        internal static bool IsCriticalNotReadyReason(string reason)
        {
            return reason == "native-hierarchy-incomplete";
        }

        // DEV-16G ticket D: the aggregate "loaded" line. Emits exactly once at
        // Info (normal play sees a single confirmation); subsequent calls are
        // suppressed. Headless variant announces load without UI.
        private static bool readyAnnounced;

        internal static void AnnounceReady(bool headless)
        {
            if (readyAnnounced) return;
            readyAnnounced = true;
            var line = headless
                ? "Better Unturned Experience 加载成功（无界面）"
                : "Better Unturned Experience 加载成功，界面已注入";
            var recorder = Recorder;
            if (recorder != null)
            {
                recorder("Info " + line);
                return;
            }
            log?.LogInfo(line);
        }

        internal static void ResetReadyAnnouncement()
        {
            readyAnnounced = false;
        }
    }
}
