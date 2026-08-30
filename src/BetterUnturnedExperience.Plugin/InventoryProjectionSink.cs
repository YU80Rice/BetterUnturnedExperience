using System;
using BetterUnturnedExperience.ClientUi.Internal;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-16D projection observability sink: routes the awaiting-projection
    /// lifecycle into the plugin log (Submitted/Converged/ObservedLatestFact/
    /// TimedOut). The awaiting controller itself lives in the official UI
    /// component; this sink is the plugin-side observation bridge.
    /// </summary>
    internal sealed class LoggingInventoryProjectionSink : IInventoryProjectionSink
    {
        private readonly BepInEx.Logging.ManualLogSource log;

        internal LoggingInventoryProjectionSink(BepInEx.Logging.ManualLogSource log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void OnProjectionSubmitted(ProjectionBinding binding)
        {
            log.LogWarning("[BUE-DRAG] event=projection-submitted dragGeneration=" + binding.DragGeneration
                + " containerGeneration=" + binding.Container.SessionGeneration
                + " diagnosticId=BUE-DRAG-002");
        }

        public ProjectionConvergence OnNativeInventorySnapshot(NativeInventorySnapshot snapshot)
        {
            log.LogInfo("[BUE-DRAG] event=native-inventory-snapshot dragGeneration=" + snapshot.DragGeneration
                + " revision=" + snapshot.NativeRevision + " diagnosticId=BUE-DRAG-002");
            return ProjectionConvergence.Ignored;
        }

        public void OnProjectionTimedOut()
        {
            log.LogWarning("[BUE-DRAG] event=projection-timed-out diagnosticId=BUE-DRAG-002");
        }

        public AwaitingProjectionState ProjectionState { get { return AwaitingProjectionState.Idle; } }
    }
}
