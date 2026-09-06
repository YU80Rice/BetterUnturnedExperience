namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V2-15: the single-player minimal fault gate. When the local
    /// transaction ends in CriticalFailure / ConcurrentMutationAfterCommit /
    /// an executor crash, further tidy requests must be refused (fail
    /// closed) instead of compounding damage on an inventory whose state the
    /// module could not prove. This is the single-player subset of the old
    /// TidyFaultCircuit: one local player, no per-peer admission, no disk
    /// persistence and no admin commands — the full circuit (rate limiter,
    /// request ledger, security log, connection-generation scoping, JSON
    /// statistics) belongs to the multiplayer path (DEV-V2-21) where
    /// per-player admission actually applies. The gate resets at the module
    /// generation boundary (Start / re-enable); the management panel's
    /// disable→enable cycle is the user's recovery path.
    /// </summary>
    internal sealed class LocalTidyFaultGate
    {
        public bool Allowed { get; private set; } = true;

        /// <summary>Reason recorded at Open; empty while allowed.</summary>
        public string Reason { get; private set; } = string.Empty;

        /// <summary>Whether the failing transaction had proven full restoration (old circuit's temp/persistent distinction, recorded for diagnostics).</summary>
        public bool LastRestoreVerified { get; private set; }

        public void Open(string reason, bool restoreVerified)
        {
            Allowed = false;
            Reason = reason ?? string.Empty;
            LastRestoreVerified = restoreVerified;
            LitRuntime.LogError(
                "[Tidy] 本地整理熔断已打开（restoreVerified=" + (restoreVerified ? "true" : "false") + "）："
                + Reason + "；在面板重新开启背包整理前不再执行整理。");
        }

        public void Reset()
        {
            Allowed = true;
            Reason = string.Empty;
            LastRestoreVerified = false;
        }
    }
}
