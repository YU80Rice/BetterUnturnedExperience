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
        internal LoggingInventoryProjectionSink(BepInEx.Logging.ManualLogSource log)
        {
            // The log source is validated for wiring-time failure visibility,
            // but emissions route through the static BueRuntimeLog seam.
            if (log == null) throw new ArgumentNullException(nameof(log));
        }

        public void OnProjectionSubmitted(ProjectionBinding binding)
        {
            BueRuntimeLog.Runtime("[BUE-DRAG] event=projection-submitted dragGeneration=" + binding.DragGeneration
                + " containerGeneration=" + binding.Container.SessionGeneration
                + " diagnosticId=BUE-DRAG-002");
        }

        public ProjectionConvergence OnNativeInventorySnapshot(NativeInventorySnapshot snapshot)
        {
            BueRuntimeLog.Runtime("[BUE-DRAG] event=native-inventory-snapshot dragGeneration=" + snapshot.DragGeneration
                + " revision=" + snapshot.NativeRevision + " diagnosticId=BUE-DRAG-002");
            return ProjectionConvergence.Ignored;
        }

        public void OnProjectionTimedOut()
        {
            // Real abnormal condition: keep loud with a reason so the user can
            // tell placement confirmation failed.
            BueRuntimeLog.Error("[BUE-DRAG] event=projection-timed-out reason=native-convergence-timeout diagnosticId=BUE-DRAG-002");
        }

        public AwaitingProjectionState ProjectionState { get { return AwaitingProjectionState.Idle; } }
    }
}
