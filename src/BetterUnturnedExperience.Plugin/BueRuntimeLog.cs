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

        // Load/inject stage one-shots (wiring, hooks-installed, surface
        // dispatch): Info level -> always announced.
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
    }
}
