using System;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-04: pure-C# decision core for the standalone-LMN takeover.
    /// Detection is injected as a Func&lt;bool&gt; probe (production binds it to
    /// the host's loaded-plugin registry ContainsKey(LMN_GUID); tests inject a
    /// stub) — the coordinator itself never scans directories or touches
    /// host/bootstrap types, keeping it host-testable. When active, every
    /// MOD/LMN2 frame is short-circuited (the Priority.First harmony prefix
    /// returns false) so LMN's own prefix never runs; other frames pass
    /// through. Red-line: never active when the probe says LMN is absent.
    /// </summary>
    public sealed class LmnTakeoverCoordinator
    {
        private readonly Func<bool> isStandaloneLmnLoaded;
        private bool active;

        public LmnTakeoverCoordinator(Func<bool> isStandaloneLmnLoaded)
        {
            this.isStandaloneLmnLoaded = isStandaloneLmnLoaded ?? throw new ArgumentNullException(nameof(isStandaloneLmnLoaded));
        }

        public bool TakeoverActive { get { return active; } }

        /// <summary>Re-evaluate standalone-LMN presence (call at network-module init).</summary>
        public void Refresh()
        {
            active = isStandaloneLmnLoaded();
        }

        /// <summary>
        /// True when the takeover is active AND the frame is LMN traffic —
        /// the Priority.First prefix returns false for these to short-circuit
        /// LMN's own handling. Non-LMN frames always return false (pass through).
        /// </summary>
        public bool ShouldShortCircuit(byte[] frame)
        {
            if (!active) return false;
            return LmnFrameClassifier.IsLmnFrame(frame);
        }
    }
}
