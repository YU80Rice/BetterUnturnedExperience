using System;
using System.Collections.Generic;
using SDG.Unturned;

// DEV-V2-21: the production authority behind ILitTidyAuthority — the
// engine-facing half of the multiplayer tidy path, ported from the retired
// standalone plugin's main-thread executor (author: YU80Rice, MIT;
// attribution docs/third-party/LaunchInventoryTidy-attribution.md). The
// service owns the protocol and admission; this type owns the game: the
// peer→Player resolution (server authority first, then the client scan),
// the trusted-fingerprint transaction through ManualTidyService, the ACK
// hotkey restore (the SAME ten-step verification chain LocalTidyExecutor
// uses — reused, never duplicated), and the client convergence probe.
namespace BetterUnturnedExperience.Lit
{
    internal sealed class LitTidyProductionAuthority : ILitTidyAuthority
    {
        private readonly InventoryTidyModule module;

        internal LitTidyProductionAuthority(InventoryTidyModule module)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
        }

        /// <summary>Host-test seam (the BueSettingsRuntime.AuthorityProbeForTests
        /// precedent): overrides the local role probe — the host has no
        /// engine, so tests inject the role deterministically. Null = the
        /// production engine probe.</summary>
        internal static Func<bool> ServerRoleProbeForTests;

        /// <summary>The local role truth: a server process (single player, listen host, U3DS) tidies locally.</summary>
        internal static bool IsServerRole()
        {
            var probe = ServerRoleProbeForTests;
            if (probe != null) return probe();
            try { return Provider.isServer; }
            catch (Exception) { return false; }
        }

        /// <summary>The fault persistence scope context (map + save slot); resolution failure yields an unbound scope.</summary>
        internal static LitFaultScopeContext ResolveFaultScopeContext()
        {
            try
            {
                var map = Provider.map;
                if (string.IsNullOrWhiteSpace(map)) return default(LitFaultScopeContext);
                return new LitFaultScopeContext(map, Characters.selected);
            }
            catch (Exception)
            {
                return default(LitFaultScopeContext);
            }
        }

        public List<HotkeySnapshot> CaptureClientHotkeys()
        {
            return HotkeySnapshotUtil.CaptureLocalHotkeys();
        }

        public LitAuthorityResult ExecuteServerTidy(LitTidyRequestContext request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            // R4-Spec GAP: defense-in-depth page gate at the authority —
            // the wire codec already rejects out-of-range pages, but the
            // transaction must never index the inventory on an unvalidated
            // page (the frozen tidyable range is 2..6 / AllPages).
            if (request.Page != LitRuntime.AllPages
                && (request.Page < HotkeySnapshotUtil.TIDYABLE_PAGE_MIN || request.Page > HotkeySnapshotUtil.TIDYABLE_PAGE_MAX))
            {
                LitRuntime.LogWarning("[TidyNet] 权威端页范围校验失败（page=" + request.Page + ", reqId=" + request.RequestId + "），拒绝整理");
                return LitAuthorityResult.From(TidyOperationOutcome.RejectedNoMutation);
            }
            var player = ResolvePlayerBySteamId(request.PeerSteamId);
            if (player?.inventory == null)
            {
                LitRuntime.LogWarning("[TidyNet] peer " + request.PeerSteamId + " 无对应 Player，拒绝整理（reqId=" + request.RequestId + "）");
                return LitAuthorityResult.From(TidyOperationOutcome.RejectedNoMutation);
            }

            var resolvedHotkeys = HotkeySnapshotUtil.ValidateAndResolve(player.inventory, request.Hotkeys);
            TidyDiagnosticLog.Info("network-hotkey-snapshot",
                $"[TidyNet] 快捷键快照已验证：uploaded={request.Hotkeys.Count}, accepted={resolvedHotkeys.Count}, reqId={request.RequestId}。");

            // Round 9 rule: the server trusts only fingerprints read from the
            // real ItemJars — the uploaded quality/state never enters a
            // restore entry.
            var trusted = new Dictionary<ItemJar, ItemFingerprint>(ReferenceEqualityComparer<ItemJar>.Instance);
            foreach (var pair in resolvedHotkeys)
            {
                var jar = pair.Key;
                if (jar == null || jar.item == null)
                {
                    LitRuntime.LogWarning("[TidyNet] reqId=" + request.RequestId + " hotkey=" + pair.Value.HotkeyIndex + " 解析后 jar/item 为 null，拒绝整理");
                    return LitAuthorityResult.From(TidyOperationOutcome.RejectedNoMutation);
                }
                trusted[jar] = new ItemFingerprint(jar.item);
            }

            var outMapping = new Dictionary<ItemJar, NewPosition>();
            TidyOperationOutcome outcome = request.Page == LitRuntime.AllPages
                ? ManualTidyService.TidyAllPlayerPages(player.inventory, request.SortDescending, request.Mode, outMapping, module.Strategy)
                : ManualTidyService.TidyPage(player.inventory.items[request.Page], request.Page, request.SortDescending, request.Mode, outMapping, module.Strategy);

            if (outcome.Result == TidyCommitResult.CriticalFailure)
            {
                // P0-4: a verified inventory rollback restores the hotkeys to
                // their ORIGINAL coordinates; the restore result decides the
                // fault's persistence (the service opens it).
                if (outcome.RollbackVerified && resolvedHotkeys.Count > 0)
                {
                    var hk = LocalTidyExecutor.TryRestoreHotkeysToOriginalPositions(player, resolvedHotkeys);
                    outcome.HotkeyRestoreAttempted = hk.Attempted;
                    outcome.HotkeyRestoreSucceeded = hk.Succeeded;
                    outcome.HotkeyRestoreFailed = hk.Failed;
                    outcome.HotkeyRollbackVerified = hk.AllVerified;
                }
                else
                {
                    outcome.HotkeyRollbackVerified = (resolvedHotkeys.Count == 0) && outcome.RollbackVerified;
                }
                return LitAuthorityResult.From(outcome);
            }
            if (outcome.Result != TidyCommitResult.Committed)
            {
                return LitAuthorityResult.From(outcome);
            }

            var entries = new List<HotkeyRestoreEntry>();
            var mappings = new List<LitNewPositionMapping>();
            foreach (var pair in resolvedHotkeys)
            {
                var originalJar = pair.Key;
                var snapshot = pair.Value;
                if (!outMapping.TryGetValue(originalJar, out var newPosition)
                    || !trusted.TryGetValue(originalJar, out var fingerprint))
                {
                    // A committed jar without a trusted mapping cannot claim
                    // a successful hotkey restore — skipped with a warning.
                    LitRuntime.LogWarning("[TidyNet] reqId=" + request.RequestId + " hotkey=" + snapshot.HotkeyIndex + " 缺少可信恢复映射");
                    continue;
                }
                entries.Add(new HotkeyRestoreEntry(snapshot.HotkeyIndex, newPosition.Page, newPosition.X, newPosition.Y, fingerprint));
                mappings.Add(new LitNewPositionMapping(snapshot.HotkeyIndex, newPosition.Page, newPosition.X, newPosition.Y, fingerprint.Id));
            }
            return new LitAuthorityResult { Outcome = outcome, Mappings = mappings, RestoreEntries = entries };
        }

        public LitHotkeyRestoreResult RestoreServerHotkeys(ulong peerSteamId, List<HotkeyRestoreEntry> entries)
        {
            var result = new LitHotkeyRestoreResult { Restored = 0, Verified = 0, Cleared = 0, FailedIndices = new List<byte>() };
            var player = ResolvePlayerBySteamId(peerSteamId);
            if (player?.equipment == null || entries == null) return result;
            var canVerify = LocalTidyExecutor.CanVerifyHotkeyState(player);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;
                try
                {
                    if (TryRestoreOneHotkey(player, entry, canVerify, out var verified, out var reason))
                    {
                        result.Restored++;
                        if (verified) result.Verified++;
                        continue;
                    }
                    LitRuntime.LogWarning("[TidyNet] ACK hotkey=" + entry.HotkeyIndex + " restore failed: " + reason);
                    LocalTidyExecutor.ClearHotkey(player, entry.HotkeyIndex);
                    result.Cleared++;
                    result.FailedIndices.Add(entry.HotkeyIndex);
                }
                catch (Exception error)
                {
                    LitRuntime.LogWarning("[TidyNet] ACK 恢复快捷键 " + entry.HotkeyIndex + " 异常: " + error.Message);
                    LocalTidyExecutor.ClearHotkey(player, entry.HotkeyIndex);
                    result.Cleared++;
                    result.FailedIndices.Add(entry.HotkeyIndex);
                }
            }
            return result;
        }

        public bool VerifyClientConvergence(List<LitNewPositionMapping> mappings)
        {
            if (mappings == null || mappings.Count == 0) return true;
            Player player;
            try { player = Player.LocalPlayer; }
            catch (Exception) { return false; }
            if (player?.inventory == null) return false;
            for (int i = 0; i < mappings.Count; i++)
            {
                var mapping = mappings[i];
                if (mapping.NewPage < HotkeySnapshotUtil.TIDYABLE_PAGE_MIN || mapping.NewPage > HotkeySnapshotUtil.TIDYABLE_PAGE_MAX) return false;
                var pageItems = player.inventory.items[mapping.NewPage];
                if (pageItems == null) return false;
                if (mapping.NewX >= pageItems.width || mapping.NewY >= pageItems.height) return false;
                var jarIndex = pageItems.getIndex(mapping.NewX, mapping.NewY);
                if (jarIndex == byte.MaxValue) return false;
                var jar = pageItems.getItem(jarIndex);
                if (jar?.item == null || jar.item.id != mapping.ExpectedItemId) return false;
            }
            return true;
        }

        /// <summary>
        /// The one-hotkey ACK restore: the SAME ten-step fingerprint chain
        /// LocalTidyExecutor verifies with (single source), then the server
        /// binds at the NEW coordinates and re-verifies the bound state.
        /// </summary>
        private static bool TryRestoreOneHotkey(Player player, HotkeyRestoreEntry entry, bool canVerify, out bool verified, out string reason)
        {
            verified = false;
            reason = null;
            if (player?.equipment == null)
            {
                reason = "player/equipment is null";
                return false;
            }
            if (!LocalTidyExecutor.TryResolveExactHotkeyTarget(player.inventory, entry, out var jar, out var asset, out reason)) return false;
            player.equipment.ServerBindItemHotkey(entry.HotkeyIndex, asset, entry.NewPage, entry.NewX, entry.NewY);
            if (!canVerify) return true;
            verified = LocalTidyExecutor.VerifyHotkeyBound(player, entry.HotkeyIndex, entry.NewPage, entry.NewX, entry.NewY, entry.ExpectedFingerprint.Id);
            if (!verified)
            {
                reason = "ServerBindItemHotkey returned but HotkeyInfo did not match expected id/page/x/y";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Peer → Player resolution (the migrated P0-3 rule): the local
        /// authority identity first (listen host / single player), then the
        /// connected-client scan; a remote id is never mis-mapped to the
        /// local player. Steam identity flows through the silent-reflection
        /// resolvers — the plugin keeps zero Steamworks compile reference.
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
            // R3-Standards B4: the resolver returns the SteamPlayer wrapper —
            // the Player lives one property deeper; an `as Player` here would
            // be null for EVERY remote peer.
            var steamPlayer = BetterUnturnedExperience.Plugin.BueEngineNet.FindSteamPlayer(steamId) as SteamPlayer;
            return steamPlayer?.player;
        }
    }
}
