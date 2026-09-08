using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the HostTick-driven input detector — the old plugin
    /// Update-loop double-tap logic migrated onto the frozen host clock seam
    /// (功能模块不得自建 Unity Update 泵). Responsibilities per spec T5 决策 4:
    /// the double-tap detection (0.3s window, single click re-anchors the
    /// baseline and the native reload passes through) and the reload-key
    /// polling (the key state provider is injected; production wires
    /// InputEx.GetKeyDown(ControlsSettings.reload) so chat focus / rebinding
    /// swallowing keeps working). Time comes from the accumulated HostTick
    /// deltas (the clock's monotonic increments). Tick never throws into the
    /// clock chain: a faulting key provider or a faulting trigger callback is
    /// isolated with a structured diagnostic.
    /// </summary>
    internal sealed class ReloadInputDriver
    {
        private readonly Func<bool> keyDown;
        private readonly Action onDoubleTap;
        private double lastReloadKeyDownSeconds = -1d;
        private double elapsedSeconds;
        private ulong lastTickNumber;

        internal ReloadInputDriver(Func<bool> keyDown, Action onDoubleTap)
        {
            this.keyDown = keyDown ?? throw new ArgumentNullException(nameof(keyDown));
            this.onDoubleTap = onDoubleTap ?? throw new ArgumentNullException(nameof(onDoubleTap));
        }

        internal void Tick(HostTick tick)
        {
            // Sequence guard: the clock is strictly monotonic; a replayed or
            // stale tick must not advance (or rewind) the accumulated time.
            if (tick.TickNumber <= lastTickNumber) return;
            lastTickNumber = tick.TickNumber;
            if (tick.DeltaTime > 0f) elapsedSeconds += tick.DeltaTime;

            bool pressed;
            try { pressed = keyDown(); }
            catch (Exception error)
            {
                LirRuntime.LogError("[LirInput] 换弹键轮询异常（本帧跳过）: " + error.Message);
                return;
            }
            if (!pressed) return;

            // The migrated double-tap difference algorithm: a press inside the
            // window after a previous press fires once and clears the baseline
            // (a held key cannot machine-gun triggers); a press outside the
            // window (or the very first press) only re-anchors the baseline.
            var now = elapsedSeconds;
            var deltaT = now - lastReloadKeyDownSeconds;
            if (lastReloadKeyDownSeconds > 0d && deltaT <= ReloadRuntimePolicy.DoubleClickWindowSeconds)
            {
                lastReloadKeyDownSeconds = -1d;
                try { onDoubleTap(); }
                catch (Exception error)
                {
                    LirRuntime.LogError("[LirInput] 双击触发回调异常（已隔离）: " + error.Message);
                }
            }
            else
            {
                lastReloadKeyDownSeconds = now;
            }
        }
    }
}
