using System;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the production authority — every engine contact of the
    /// repack path lives here (player resolution, the request gate, the
    /// transactional services), behind ILirRepackAuthority so the full chain
    /// runs on the loopback test transport with a fake authority and zero
    /// engine work. Outcome mapping is one-to-one onto the migrated
    /// transaction enums; RestoreFailed quarantines the player (the
    /// half-commit-risk rule — a disconnect, an inventory resync or a manual
    /// release lifts it). Main thread only (the host frame chain drives it).
    /// Steam identity flows through the silent-reflection resolvers
    /// (BueEngineNet) — the plugin keeps zero Steamworks compile reference
    /// (the DEV-V2-21 LIT authority convention).
    /// </summary>
    internal sealed class LirProductionAuthority : ILirRepackAuthority
    {
        /// <summary>The production role probe (single player, listen host and U3DS are all server roles).</summary>
        internal static bool IsServerRole()
        {
            try { return Provider.isServer; }
            catch (Exception) { return false; }
        }

        public LirRepackExecution ExecuteRepack(ulong senderSteamId, ulong requestId)
        {
            // Enqueueing is not authorization: the player is resolved again
            // here (the old merged-entry rule), then the gate, then the
            // transaction.
            var admission = Admit(senderSteamId, requestId, out var player);
            if (admission != GateAdmission.Proceed)
            {
                return new LirRepackExecution { Outcome = admission == GateAdmission.PlayerMissing ? LirRepackOutcome.PlayerMissing : LirRepackOutcome.RejectedCooldown };
            }
            var result = AmmoRepackService.TryRepackTransactional(player);
            switch (result.Outcome)
            {
                case RepackOutcome.Committed:
                    return new LirRepackExecution { Outcome = LirRepackOutcome.Committed, TotalTransferred = result.TotalTransferred };
                case RepackOutcome.RestoreFailed:
                    // Half-commit risk: quarantine until a resync / release.
                    LirRepackGate.Quarantine(senderSteamId, "Repack RestoreExact verification failed");
                    return new LirRepackExecution { Outcome = LirRepackOutcome.RestoreFailed };
                case RepackOutcome.RolledBack:
                    return new LirRepackExecution { Outcome = LirRepackOutcome.RolledBack };
                case RepackOutcome.AbortedStateDrift:
                    return new LirRepackExecution { Outcome = LirRepackOutcome.AbortedStateDrift };
                default:
                    return new LirRepackExecution { Outcome = LirRepackOutcome.NoChange };
            }
        }

        public LirMergeExecution ExecuteMerge(ulong targetSteamId, ulong requestId)
        {
            var admission = Admit(targetSteamId, requestId, out var player);
            if (admission != GateAdmission.Proceed)
            {
                return new LirMergeExecution { Outcome = admission == GateAdmission.PlayerMissing ? LirMergeOutcome.PlayerMissing : LirMergeOutcome.RejectedCooldown };
            }
            var result = AmmoRepackService.TryMergeSameIdMagazinesTransactional(player);
            switch (result.Outcome)
            {
                case MergeOutcome.Committed:
                    return new LirMergeExecution { Outcome = LirMergeOutcome.Committed, TotalMerged = result.TotalMerged };
                case MergeOutcome.RestoreFailed:
                    // The old tidy-postfix rule: a merge restore failure
                    // quarantines the player (half-commit risk).
                    LirRepackGate.Quarantine(targetSteamId, "Merge RestoreExact verification failed");
                    return new LirMergeExecution { Outcome = LirMergeOutcome.RestoreFailed };
                case MergeOutcome.RolledBack:
                    return new LirMergeExecution { Outcome = LirMergeOutcome.RolledBack };
                case MergeOutcome.AbortedStateDrift:
                    return new LirMergeExecution { Outcome = LirMergeOutcome.AbortedStateDrift };
                default:
                    return new LirMergeExecution { Outcome = LirMergeOutcome.NoChange };
            }
        }

        public bool TryResolveLocalPlayerSteamId(out ulong steamId)
        {
            steamId = BetterUnturnedExperience.Plugin.BueEngineNet.LocalSteamId();
            return steamId != 0UL;
        }

        private enum GateAdmission : byte { Proceed, PlayerMissing, RejectedCooldown }

        /// <summary>The shared admission pair (R1-Standards SMELL-2): player re-resolution THEN the request gate.</summary>
        private GateAdmission Admit(ulong steamId, ulong requestId, out Player player)
        {
            player = ResolvePlayerBySteamId(steamId);
            if (player == null || player.inventory == null) return GateAdmission.PlayerMissing;
            if (!LirRepackGate.TryAcquire(steamId, requestId, out _)) return GateAdmission.RejectedCooldown;
            return GateAdmission.Proceed;
        }

        /// <summary>
        /// The LIT authority's resolver shape: the server role first checks
        /// the LOCAL player (a remote id is never mis-mapped to it), then the
        /// connected-client scan through the silent-reflection resolver — the
        /// SteamPlayer wrapper's Player lives one property deeper.
        /// </summary>
        private static Player ResolvePlayerBySteamId(ulong steamId)
        {
            if (steamId == 0UL) return null;
            if (IsServerRole())
            {
                try
                {
                    if (BetterUnturnedExperience.Plugin.BueEngineNet.LocalSteamId() == steamId)
                    {
                        var localPlayer = Player.LocalPlayer;
                        if (localPlayer != null) return localPlayer;
                    }
                }
                catch (Exception)
                {
                    // fall through to the client scan
                }
            }
            var steamPlayer = BetterUnturnedExperience.Plugin.BueEngineNet.FindSteamPlayer(steamId) as SteamPlayer;
            return steamPlayer?.player;
        }
    }
}
