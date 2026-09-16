using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-04: the engine-facing half of the insert-recover adapter — every
    /// method here is NoInlining + exception-guarded so the host test process
    /// (no asset database, no local player, no equipment) never JITs an engine
    /// body through the recovery decision path (the Mono ECall JIT rule the
    /// LIT/LIR probes already follow). The production tails replicate exactly
    /// what vanilla tryAddItemAuto itself does AFTER a successful page add:
    ///   - autoEquipUseable → ServerEquip for a usable landing on an
    ///     equippable page while no valid useable is held;
    ///   - the hotkey migration reuses LocalTidyExecutor's single-source chain
    ///     (capture → validate → trusted fingerprints → rebind at the new
    ///     coordinates / restore originals after a verified rollback);
    ///   - the listen-host dashboard projection reconciles over the tidied
    ///     range, the same F-B1c anchor the button path publishes through.
    /// Nothing here publishes TidyCompleted or any contract event (V5-T5 Q5).
    /// </summary>
    internal static class InsertRecoverEngine
    {
        /// <summary>Production page-array read: the target inventory's nine
        /// Items pages. NoInlining + exception-guarded so the host never JITs
        /// a PlayerInventory member access (its static init — the NetReflection
        /// wiring the DEV-V5-03 MOUNT_PAGE comment names — is unreachable
        /// outside the game process; the host injects pages through
        /// InsertRecoverAdapter.PagesForTests instead).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Items[] ReadPages(PlayerInventory inv)
        {
            try
            {
                return inv == null ? null : inv.items;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>The engine asset read behind the pending gate: null asset,
        /// isPro or non-positive size all block recovery (the vanilla
        /// tryAddItemAuto gate reproduced — recovery never smuggles past it).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool TryDescribeAsset(Item item, out InsertRecoverAdapter.PendingInfo info)
        {
            info = default(InsertRecoverAdapter.PendingInfo);
            try
            {
                var asset = item.GetAsset();
                if (asset == null || asset.isPro)
                {
                    info.Blocked = true;
                    return true;
                }
                info.SizeX = asset.size_x;
                info.SizeY = asset.size_y;
                return true;
            }
            catch (Exception)
            {
                return false; // unreadable asset = blocked, never assumed placeable
            }
        }

        /// <summary>The local hotkey bundle for the tidied inventory (null when
        /// the engine is unavailable or the inventory is not the local
        /// player's). Capture + validate + trusted fingerprints run BEFORE the
        /// commit, exactly like LocalTidyExecutor.Execute's order.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object CaptureLocalHotkeys(object owner)
        {
            try
            {
                var inv = owner as PlayerInventory;
                if (inv == null) return null;
                var player = Player.LocalPlayer;
                if (player == null || !ReferenceEquals(player.inventory, inv)) return null;
                var snapshots = HotkeySnapshotUtil.CaptureLocalHotkeys();
                var resolved = HotkeySnapshotUtil.ValidateAndResolve(inv, snapshots);
                var trusted = new Dictionary<ItemJar, ItemFingerprint>(ReferenceEqualityComparer<ItemJar>.Instance);
                foreach (var pair in resolved)
                {
                    var jar = pair.Key;
                    if (jar == null || jar.item == null) return null; // fail-closed like the executor
                    trusted[jar] = new ItemFingerprint(jar.item);
                }
                return new Bundle { Player = player, Resolved = resolved, Trusted = trusted };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private sealed class Bundle
        {
            internal Player Player;
            internal Dictionary<ItemJar, HotkeySnapshot> Resolved;
            internal Dictionary<ItemJar, ItemFingerprint> Trusted;
        }

        /// <summary>Commit success: rebind hotkeys to the new coordinates,
        /// replay vanilla's auto-equip-usable tail at the landing slot, and
        /// reconcile the listen-host projection over the tidied range. Every
        /// step is individually guarded — a committed recovery never degrades
        /// into a thrown failure for the vanilla caller.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AfterCommitted(object bundle, object owner, Item pending,
            List<PagePreparation> preps, byte pendingPage, Dictionary<ItemJar, NewPosition> mapping, bool autoEquipUseable)
        {
            try
            {
                var local = bundle as Bundle;
                if (local != null)
                    LocalTidyExecutor.RestoreHotkeysToNewPositions(local.Player, local.Resolved, local.Trusted, mapping);
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[入包恢复] 快捷键重绑异常（入包结果不受影响）: " + error.Message);
            }
            try
            {
                EquipUsableTail(owner, pending, preps, pendingPage, autoEquipUseable);
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[入包恢复] 可用物自动装备尾异常（入包结果不受影响）: " + error.Message);
            }
            try
            {
                BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.OnTidyPagesCommitted(
                    HotkeySnapshotUtil.TIDYABLE_PAGE_MIN, HotkeySnapshotUtil.TIDYABLE_PAGE_MAX);
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[入包恢复] 听主机投影 reconcile 异常（忽略）: " + error.Message);
            }
        }

        /// <summary>Vanilla's post-add useable tail (PlayerInventory.tryAddItemAuto
        /// pages loop): equip when no valid useable is held, the landed page
        /// accepts the slot and the asset can be player-equipped.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EquipUsableTail(object owner, Item pending,
            List<PagePreparation> preps, byte pendingPage, bool autoEquipUseable)
        {
            if (!autoEquipUseable) return;
            var inv = owner as PlayerInventory;
            if (inv == null) return;
            var player = inv.player;
            if (player == null || player.equipment == null) return;
            if (player.equipment.HasValidUseable) return;
            var asset = pending.GetAsset();
            if (asset == null || !asset.canPlayerEquip) return;
            if (!asset.slot.canEquipInPage(pendingPage)) return;
            byte x = 0, y = 0;
            foreach (var prep in preps)
            {
                if (prep.Page != pendingPage || prep.Pending == null) continue;
                foreach (var entry in prep.Result)
                {
                    if (entry != null && ReferenceEquals(entry.Tag, pending) && entry.Placed)
                    {
                        x = entry.ResultX; y = entry.ResultY;
                        player.equipment.ServerEquip(pendingPage, x, y);
                        return;
                    }
                }
            }
        }

        /// <summary>Commit failure with a VERIFIED rollback: the tidied pages
        /// are back at the original coordinates, so the hotkeys go back with
        /// them (the same rule the button executor applies). Unverified states
        /// are left untouched — the fault gate isolation owns them.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AfterFailed(object bundle, object owner, TidyOperationOutcome outcome)
        {
            try
            {
                var local = bundle as Bundle;
                if (local == null || outcome == null || !outcome.RollbackVerified) return;
                if (local.Resolved != null && local.Resolved.Count > 0)
                    LocalTidyExecutor.TryRestoreHotkeysToOriginalPositions(local.Player, local.Resolved);
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[入包恢复] 回滚后快捷键恢复异常（忽略）: " + error.Message);
            }
        }
    }
}
