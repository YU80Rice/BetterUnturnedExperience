using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-05: the engine-facing half of the fast-transfer recover adapter —
    /// every method NoInlining + exception-guarded so the host test process
    /// never JITs an engine body through the decision path (the Mono ECall
    /// rule the 03/04 probes follow; the 真机探针形状 is the named seam gap
    /// riding DEV-V5-08). The intent probe reproduces vanilla's OWN branch
    /// guards verbatim (PlayerDashboardInventoryUI.onSelectedItem, U3-SDK):
    /// Ctrl(ControlsSettings.other) held, inventory.isStoring, the fast-transfer
    /// source page (2..6 or the storage mount 7 — primary/secondary hands and
    /// the ground AREA preview are NOT this ticket's 背包↔容器 range: 0/1 and 8
    /// refuse here exactly like the wire codec refuses them), the selection-
    /// close short-circuit (right-clicking the already-selected slot only
    /// closes the selection — NOT a transfer attempt; reading it must fail
    /// closed to "no request"), the jar present, and the 03 container-session
    /// observation yielding a SUPPORTED kind + a real fingerprint (virtual /
    /// display cabinets: 与 03「不画按钮」同一边界，权威侧再 fail-closed 一道).
    /// </summary>
    internal static class FastTransferRecoverEngine
    {
        /// <summary>Host-test seam (the PagesForTests family): replaces the
        /// whole intent read. Null = the production engine probe.</summary>
        internal static Func<byte, byte, byte, FastTransferIntent> IntentProbeForTests;

        /// <summary>Runs inside the onSelectedItem Prefix (main thread, UI
        /// click flow). Null = the vanilla branch would not have applied (or
        /// anything was unreadable) → no scope, no request, pure vanilla.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static FastTransferIntent TryReadIntent(byte page, byte x, byte y)
        {
            var seam = IntentProbeForTests;
            if (seam != null)
            {
                try { return seam(page, x, y); }
                catch (Exception error)
                {
                    LitRuntime.LogWarning("[快速转移恢复] 意图假缝异常（按不请求处理）: " + error.Message);
                    return null;
                }
            }
            try
            {
                if (!InputEx.GetKey(ControlsSettings.other)) return null;
                var inv = Player.LocalPlayer?.inventory;
                if (inv == null || !inv.isStoring) return null;

                // 快速转移源页域：玩家五页或容器挂载页（0/1 主副手与 AREA 地面
                // 摊是票面具名排除——前者归「背包↔容器」之外，后者是拾取 RPC 归 04）。
                var playerSource = page >= HotkeySnapshotUtil.TIDYABLE_PAGE_MIN && page <= HotkeySnapshotUtil.TIDYABLE_PAGE_MAX;
                var containerSource = page == LitContainerTidyExecution.MOUNT_PAGE;
                if (!playerSource && !containerSource) return null;

                // 原版选中短路：同槽再右键只关选框，不发起转移（读不到 = 不请求）。
                if (page == PlayerDashboardInventoryUI.selectedPage
                    && x == PlayerDashboardInventoryUI.selected_x
                    && y == PlayerDashboardInventoryUI.selected_y) return null;

                var jarIndex = inv.getIndex(page, x, y);
                if (jarIndex == byte.MaxValue) return null;
                var jar = inv.getItem(page, jarIndex);
                if (jar == null || jar.item == null) return null;

                var view = LitContainerSessionProbe.ObserveClient();
                if (view == null || view.Fingerprint == 0UL) return null;
                var obs = view.Observation;
                if (!obs.SessionActive || !obs.GridNonEmpty || !obs.PermissionGranted) return null;
                LitContainerTidyKind kind;
                if (obs.Kind == LitContainerSessionKind.WorldContainer) kind = LitContainerTidyKind.WorldContainer;
                else if (obs.Kind == LitContainerSessionKind.VehicleTrunk) kind = LitContainerTidyKind.VehicleTrunk;
                else return null; // 虚拟箱/展示柜/无会话 = 不请求（权威端还会独立重验一遍）

                return new FastTransferIntent
                {
                    SourcePage = page,
                    SourceX = x,
                    SourceY = y,
                    Kind = kind,
                    Fingerprint = view.Fingerprint,
                };
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[快速转移恢复] 真机意图探针失败（按不请求处理）: " + error.Message);
                return null;
            }
        }

        /// <summary>The page array of one Player's inventory for the engine-free
        /// core (04's ReadPages reuse is deliberate: the PlayerInventory type
        /// must never be touched off-game, and this is the identical read).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Items[] ReadPages(Player player)
        {
            try
            {
                return player?.inventory == null ? null : InsertRecoverEngine.ReadPages(player.inventory);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Hotkey bundle for the LOCAL player's inventory only (remote
        /// peers' server-side _hotkeys are null — the 04-named vanilla-parity
        /// limitation). Capture + validate + trusted fingerprints BEFORE the
        /// commit, exactly like the button executor's order.</summary>
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
                    if (jar == null || jar.item == null) return null; // fail-closed like 04
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

        /// <summary>Commit success: rebind ONLY the bindings whose jars are in
        /// this transaction's mapping FOR THE TIDIED TARGET PAGE. The source
        /// identity page maps its jars to their ORIGINAL coordinates (nothing
        /// moved → nothing rebinds), and the moved item's own binding (if any)
        /// dangles exactly like vanilla's drag leaves it — recovery never
        /// invents hotkey bookkeeping vanilla does not do. Every step is
        /// individually guarded — a committed recovery never degrades into a
        /// thrown failure.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AfterCommitted(object bundle, byte targetPage, Dictionary<ItemJar, NewPosition> mapping)
        {
            try
            {
                var local = bundle as Bundle;
                if (local == null || local.Resolved == null || local.Resolved.Count == 0) return;
                if (mapping == null || mapping.Count == 0) return;
                var moved = new Dictionary<ItemJar, HotkeySnapshot>();
                foreach (var pair in local.Resolved)
                {
                    NewPosition position;
                    if (!mapping.TryGetValue(pair.Key, out position)) continue;
                    if (position.Page != targetPage) continue; // 恒等源页/容器页一律不重绑
                    moved[pair.Key] = pair.Value;
                }
                if (moved.Count == 0) return;
                var trustedMoved = new Dictionary<ItemJar, ItemFingerprint>(ReferenceEqualityComparer<ItemJar>.Instance);
                foreach (var jar in moved.Keys)
                {
                    ItemFingerprint fp;
                    if (local.Trusted != null && local.Trusted.TryGetValue(jar, out fp)) trustedMoved[jar] = fp;
                }
                LocalTidyExecutor.RestoreHotkeysToNewPositions(local.Player, moved, trustedMoved, mapping);
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[快速转移恢复] 快捷键重绑异常（转移结果不受影响）: " + error.Message);
            }
        }

        /// <summary>Commit failure with a VERIFIED rollback: the tidied page is
        /// back at the original coordinates, so any bindings that could have
        /// been re-pointed go back with it (04's rule; here rebind only ever
        /// runs on success, so this normally restores nothing — kept for the
        /// same-shape defense against half-applied external effects).</summary>
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
                LitRuntime.LogWarning("[快速转移恢复] 回滚后快捷键恢复异常（忽略）: " + error.Message);
            }
        }

        /// <summary>The listen-host dashboard projection is stale after a
        /// committed player-page move (04's F-B1c anchor, single page range —
        /// 「只动该动的那一格」 applies to the reconcile face too).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReconcileListenHost(byte touchedPlayerPage)
        {
            try
            {
                if (touchedPlayerPage < HotkeySnapshotUtil.TIDYABLE_PAGE_MIN || touchedPlayerPage > HotkeySnapshotUtil.TIDYABLE_PAGE_MAX)
                    return; // 容器页不在投影域
                // DEV-V6-02B (V6-T2 硬项拆法): the reconcile request rides the
                // HOST-BOUND relay port — the feature never names the UI layer.
                LitFeatureAssembly.RelayProjectionReconcile(touchedPlayerPage, touchedPlayerPage);
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[快速转移恢复] 听主机投影 reconcile 异常（忽略）: " + error.Message);
            }
        }
    }
}
