namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// The engine-facing repack/merge work the LIR service orchestrates (the
    /// test seam — the full chain runs on the loopback transport with a fake
    /// authority and zero engine contact, the DEV-V2-21 pattern). Outcomes
    /// mirror the migrated transaction enums one-to-one.
    /// </summary>
    internal enum LirRepackOutcome : byte
    {
        None = 0,
        NoChange = 1,
        Committed = 2,
        RolledBack = 3,
        AbortedStateDrift = 4,
        RestoreFailed = 5,
        PlayerMissing = 6,
        RejectedCooldown = 7,
    }

    internal enum LirMergeOutcome : byte
    {
        None = 0,
        NoChange = 1,
        Committed = 2,
        RolledBack = 3,
        AbortedStateDrift = 4,
        RestoreFailed = 5,
        PlayerMissing = 6,
        RejectedCooldown = 7,
    }

    /// <summary>One repack transaction result (public fields — the LIT authority-result convention).</summary>
    internal struct LirRepackExecution
    {
        public LirRepackOutcome Outcome;
        public int TotalTransferred;
    }

    /// <summary>One merge (same-id magazine) transaction result.</summary>
    internal struct LirMergeExecution
    {
        public LirMergeOutcome Outcome;
        public int TotalMerged;
    }

    internal interface ILirRepackAuthority
    {
        /// <summary>Server/main-thread: the repack-from-ammo-boxes transaction for one sender (功能 B).</summary>
        LirRepackExecution ExecuteRepack(ulong senderSteamId, ulong requestId);

        /// <summary>Main-thread: the merge-same-id-magazines transaction for one target (功能 A, the tidy follow-up).</summary>
        LirMergeExecution ExecuteMerge(ulong targetSteamId, ulong requestId);

        /// <summary>Resolves the LOCAL player's steam id (false/0 when no local player exists — headless).</summary>
        bool TryResolveLocalPlayerSteamId(out ulong steamId);
    }
}
