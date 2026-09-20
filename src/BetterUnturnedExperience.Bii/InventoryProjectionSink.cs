using System;

namespace BetterUnturnedExperience.Bii
{
    /// <summary>
    /// DEV-16D projection observability sink: routes the awaiting-projection
    /// lifecycle into the host log (Submitted/Converged/ObservedLatestFact/
    /// TimedOut). The awaiting controller itself lives in the interaction
    /// component; this sink is the observation bridge.
    /// </summary>
    internal sealed class LoggingInventoryProjectionSink : IInventoryProjectionSink
    {
        // DEV-V6-02E: emissions ride the injected host log mouth (the
        // pre-move source called the host's internal BueRuntimeLog directly).
        // DEV-V6-04: the BepInEx manual log source left with the host wiring —
        // the runtime mouth alone drives the lines here.
        private readonly Action<string> logRuntime;

        internal LoggingInventoryProjectionSink(Action<string> logRuntime = null)
        {
            this.logRuntime = logRuntime;
        }

        public void OnProjectionSubmitted(ProjectionBinding binding)
        {
            EmitRuntime("[BUE-DRAG] event=projection-submitted dragGeneration=" + binding.DragGeneration
                + " containerGeneration=" + binding.Container.SessionGeneration
                + " diagnosticId=BUE-DRAG-002");
        }

        public ProjectionConvergence OnNativeInventorySnapshot(NativeInventorySnapshot snapshot)
        {
            EmitRuntime("[BUE-DRAG] event=native-inventory-snapshot dragGeneration=" + snapshot.DragGeneration
                + " revision=" + snapshot.NativeRevision + " diagnosticId=BUE-DRAG-002");
            return ProjectionConvergence.Ignored;
        }

        public void OnProjectionTimedOut()
        {
            // DEV-16G ticket C: this is a benign visual-budget expiry, NOT a
            // functional error. The placement was already submitted natively
            // (sendDragItem) and is authoritative on the server; the 2000ms
            // budget only stops the VISUAL wait (no fake rollback). Log at
            // Debug so a normal play session stays silent.
            EmitRuntime("[BUE-DRAG] event=projection-timed-out reason=native-convergence-timeout diagnosticId=BUE-DRAG-002");
        }

        private void EmitRuntime(string line)
        {
            var sink = logRuntime;
            if (sink != null) sink(line);
        }

        public AwaitingProjectionState ProjectionState { get { return AwaitingProjectionState.Idle; } }
    }
}
