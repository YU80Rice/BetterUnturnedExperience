using System;
using HarmonyLib;

namespace BetterUnturnedExperience.Bii
{
    /// <summary>
    /// DEV-V6-05 (V6-T5 Q2/Q4): the patch-pocket handle for one BII Harmony
    /// instance — the official-first consumption of IFeaturePatching. The
    /// handle is a plain IDisposable (不新增契约句柄类型): disposing it IS the
    /// patch teardown the platform owns at the stop/isolation boundary
    /// (Harmony's UnpatchSelf, the same call the adapters' own isolate paths
    /// make — idempotent, so a drift isolation that already unpatched leaves
    /// the platform's release a no-op). A null Harmony instance (an adapter
    /// that never built one) disposes to nothing.
    /// </summary>
    internal sealed class HarmonyPatchHandle : IDisposable
    {
        private readonly Harmony harmony;
        private readonly string harmonyId;
        private readonly Action<string> log;
        private bool released;

        internal HarmonyPatchHandle(Harmony harmony, string harmonyId, Action<string> log = null)
        {
            this.harmony = harmony;
            this.harmonyId = harmonyId ?? string.Empty;
            this.log = log;
        }

        /// <summary>The Harmony identity this handle owns (diagnostics — the
        /// registration line names which patch set entered the pocket).</summary>
        internal string HarmonyId { get { return harmonyId; } }

        /// <summary>True once the platform released this handle (the stop/
        /// isolation boundary ran the patch teardown — the platform-owned
        /// half of the ownership split).</summary>
        internal bool Released { get { return released; } }

        public void Dispose()
        {
            if (released) return;
            released = true;
            // The unpatch runs FIRST: a faulting UnpatchSelf propagates to the
            // account's release pipeline (BUE-LIFE-006 isolates it) and no
            // release line is emitted — the line below therefore means "the
            // platform ran this patch set's teardown and it returned", which is
            // what the stop boundary is judged on.
            if (harmony != null) harmony.UnpatchSelf();
            if (log != null)
            {
                try { log("BUE patch pocket released harmonyId=" + harmonyId + " diagnosticId=BUE-BII-005"); }
                catch (Exception) { }
            }
        }
    }
}
