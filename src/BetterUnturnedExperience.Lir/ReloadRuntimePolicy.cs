namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22 (spec「LIR：补丁、守卫与常量」T5 决策 5): the ONE home for the
    /// reload runtime constants — nothing is scattered across the patches or
    /// the service. The three spec-named values are the double-tap window,
    /// the queue limit and the diagnostic interval; the rest are the old
    /// plugin's behavior constants migrated verbatim (旧独立插件行为保持).
    /// None of these is a public setting this phase (only `enabled` persists).
    /// </summary>
    internal static class ReloadRuntimePolicy
    {
        /// <summary>Spec frozen: the double-tap window for the in-place reload trigger.</summary>
        internal const float DoubleClickWindowSeconds = 0.3f;

        /// <summary>Spec frozen: the pending-request-sender queue limit (fail-closed beyond it).</summary>
        internal const int QueueLimit = 64;

        /// <summary>Spec frozen: the minimum interval between aggregate diagnostic lines.</summary>
        internal const float DiagnosticIntervalSeconds = 5f;

        // ── migrated dispatcher constants (old RepackMainThreadDispatcher) ──
        /// <summary>Bounded pending-success queue (the client reply toasts).</summary>
        internal const int MaxPendingSuccesses = 32;

        /// <summary>Maximum work items drained per host frame.</summary>
        internal const int MaxPerFrame = 16;

        /// <summary>Work older than this is dropped un-executed at drain (入队不等于授权).</summary>
        internal const float WorkTtlSeconds = 3f;

        // ── migrated request-gate constants (old RepackRequestGate) ──
        /// <summary>Per-player cooldown between repack transactions.</summary>
        internal const float CooldownSeconds = 1.5f;

        /// <summary>Server-side replay window for observed request ids (可靠包重放防护).</summary>
        internal const double ReplayWindowSeconds = 120.0;

        /// <summary>Hard cap of the gate dictionaries (fail-closed beyond it).</summary>
        internal const int GateMaxEntries = 128;

        // ── scope + toast constants ──
        /// <summary>The repack/merge scope mirrors PlayerInventory.SLOTS (=2).</summary>
        internal const byte MinRepackPage = 2;

        /// <summary>The repack/merge scope mirrors PlayerInventory.PANTS (=6).</summary>
        internal const byte MaxRepackPage = 6;

        /// <summary>The success toast duration (old RepackToast.DURATION).</summary>
        internal const float ToastDurationSeconds = 2.5f;
    }
}
