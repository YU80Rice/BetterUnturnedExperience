using System;
using System.Collections.Generic;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V2-15: the single-player execution end. The old plugin reached
    /// this work through the LMN network loop even in single-player (request
    /// → server handler → dispatcher); the adopted module runs it directly:
    /// capture hotkeys, run the transactional tidy through the module's
    /// strategy on the local inventory, then re-bind hotkeys to the new
    /// positions (fingerprint-verified) — the same restore logic the old
    /// server performed at ACK time, ported from ManualTidyNetwork. Every
    /// failure path opens the module's fault gate; nothing here sends or
    /// receives frames (the wire protocol belongs to DEV-V2-21).
    ///
    /// Runs on the Unity main thread via MainThreadDispatcher (the module's
    /// tick pumps the queue); the transaction service re-asserts the thread.
    /// </summary>
    internal static class LocalTidyExecutor
    {
        internal static void Execute(InventoryTidyModule module, byte page, TidyMode mode, bool sortDescending)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            Player player;
            try { player = Player.LocalPlayer; }
            catch (Exception) { player = null; }
            if (player == null || player.inventory == null)
            {
                TidyDiagnosticLog.Info("local-no-player",
                    "[Tidy] 本地整理执行时找不到本地玩家或背包，安全跳过。");
                return;
            }

            // 快捷键快照在执行帧捕获（离 Commit 最近）；本地路径无跨端协议，
            // 捕获 → 验证 → 事务 → 恢复全部同帧同线程完成。
            List<HotkeySnapshot> snapshots;
            try { snapshots = HotkeySnapshotUtil.CaptureLocalHotkeys(); }
            catch (Exception e)
            {
                LitRuntime.LogError("[Tidy] 本地快捷键快照捕获异常，放弃本次整理: " + e.Message);
                return;
            }
            var resolved = HotkeySnapshotUtil.ValidateAndResolve(player.inventory, snapshots);

            // 服务端可信指纹（Round 9 原则照搬）：绝不信任快照字段，从真实 ItemJar 取指纹。
            var trusted = new Dictionary<ItemJar, ItemFingerprint>(ReferenceEqualityComparer<ItemJar>.Instance);
            foreach (KeyValuePair<ItemJar, HotkeySnapshot> pair in resolved)
            {
                ItemJar jar = pair.Key;
                if (jar == null || jar.item == null)
                {
                    LitRuntime.LogError("[Tidy] 本地整理：热键目标 jar/item 为 null，放弃本次整理。");
                    return;
                }
                trusted[jar] = new ItemFingerprint(jar.item);
            }

            var outMapping = new Dictionary<ItemJar, NewPosition>();
            TidyOperationOutcome outcome;
            try
            {
                outcome = page == LitRuntime.AllPages
                    ? ManualTidyService.TidyAllPlayerPages(player.inventory, sortDescending, mode, outMapping, module.Strategy)
                    : ManualTidyService.TidyPage(player.inventory.items[page], page, sortDescending, mode, outMapping, module.Strategy);
            }
            catch (Exception e)
            {
                LitRuntime.LogError("[Tidy] 本地整理执行崩溃（服务层未捕获）: " + e);
                module.FaultGate.Open("local tidy crashed: " + e.Message, restoreVerified: false);
                return;
            }

            switch (outcome.Result)
            {
                case TidyCommitResult.Committed:
                    RestoreHotkeysToNewPositions(player, resolved, trusted, outMapping);
                    TidyDiagnosticLog.Info("local-committed",
                        $"[Tidy] 本地整理已提交（page={page}, mode={mode}, mappings={outMapping.Count}）。");
                    break;

                case TidyCommitResult.CriticalFailure:
                {
                    // v2.0.4 P0-4 口径：库存已验证回滚时按原坐标恢复快捷键，
                    // 恢复结果决定熔断记录的 restoreVerified。
                    bool fullRestored;
                    if (outcome.RollbackVerified && resolved.Count > 0)
                    {
                        HotkeyRestoreOutcome hk = TryRestoreHotkeysToOriginalPositions(player, resolved);
                        fullRestored = hk.AllVerified;
                    }
                    else
                    {
                        fullRestored = (resolved.Count == 0) && outcome.RollbackVerified;
                    }
                    module.FaultGate.Open(outcome.FailureReason ?? "CriticalFailure during local tidy", restoreVerified: fullRestored);
                    break;
                }

                case TidyCommitResult.ConcurrentMutationAfterCommit:
                    LitRuntime.LogError(
                        "[Tidy] ConcurrentMutationAfterCommit：已提交页被并发修改或异常路径状态未知，回滚被拒绝以保护合法并发变更。" +
                        "本模块已停止继续整理（熔断，面板重新开启前不恢复）。原版背包、物品使用和存档不受影响。");
                    module.FaultGate.Open(
                        "ConcurrentMutationAfterCommit: unknown inventory state during local tidy, rollback refused; only this module's tidy is blocked, vanilla inventory is not frozen",
                        restoreVerified: false);
                    break;

                default:
                    TidyDiagnosticLog.Info("local-rejected",
                        "[Tidy] 本地整理被安全拒绝且未修改物品。");
                    break;
            }

            module.LastLocalOutcome = outcome;
        }

        // ─────────────────────────────────────────────────────────────
        // 提交成功：按新坐标恢复热键（移植 ManualTidyNetwork ACK 恢复链）
        // ─────────────────────────────────────────────────────────────

        private static void RestoreHotkeysToNewPositions(Player player,
            Dictionary<ItemJar, HotkeySnapshot> resolved,
            Dictionary<ItemJar, ItemFingerprint> trusted,
            Dictionary<ItemJar, NewPosition> outMapping)
        {
            if (player?.equipment == null) return;
            bool canVerify = CanVerifyHotkeyState(player);

            foreach (KeyValuePair<ItemJar, HotkeySnapshot> pair in resolved)
            {
                HotkeySnapshot snapshot = pair.Value;
                if (!outMapping.TryGetValue(pair.Key, out NewPosition newPosition)
                    || !trusted.TryGetValue(pair.Key, out ItemFingerprint fingerprint))
                {
                    // 已提交但缺映射时不能谎称恢复成功；降级为清除该热键。
                    LitRuntime.LogWarning($"[Tidy] 热键 {snapshot.HotkeyIndex} 缺少可信恢复映射，清除绑定。");
                    ClearHotkey(player, snapshot.HotkeyIndex);
                    continue;
                }

                var entry = new HotkeyRestoreEntry(snapshot.HotkeyIndex, newPosition.Page, newPosition.X, newPosition.Y, fingerprint);
                try
                {
                    bool verified;
                    string reason;
                    if (TryRestoreOneHotkey(player, entry, canVerify, out verified, out reason))
                        continue;

                    LitRuntime.LogWarning($"[Tidy] 热键 {entry.HotkeyIndex} 恢复失败: {reason}，清除绑定。");
                    ClearHotkey(player, entry.HotkeyIndex);
                }
                catch (Exception ex)
                {
                    LitRuntime.LogWarning($"[Tidy] 热键 {entry.HotkeyIndex} 恢复异常: {ex.Message}，清除绑定。");
                    ClearHotkey(player, entry.HotkeyIndex);
                }
            }
        }

        private static void ClearHotkey(Player player, byte hotkeyIndex)
        {
            try { player.equipment.ServerClearItemHotkey(hotkeyIndex); }
            catch (Exception clearException)
            {
                LitRuntime.LogWarning($"[Tidy] 热键 {hotkeyIndex} 清除失败: {clearException.Message}");
            }
        }

        private static bool CanVerifyHotkeyState(Player player)
        {
            if (player?.equipment == null) return false;
            try { return player.equipment.hotkeys != null; }
            catch { return false; }
        }

        private static bool VerifyHotkeyBound(Player player, byte hotkeyIndex,
            byte expectedPage, byte expectedX, byte expectedY, ushort expectedItemId)
        {
            if (player?.equipment == null) return false;
            var hotkeys = player.equipment.hotkeys;
            if (hotkeys == null || hotkeyIndex >= hotkeys.Length) return false;
            var hk = hotkeys[hotkeyIndex];
            if (hk.id != expectedItemId) return false;
            if (hk.page != expectedPage) return false;
            if (hk.x != expectedX) return false;
            if (hk.y != expectedY) return false;
            return true;
        }

        /// <summary>
        /// 按完整指纹解析并验证热键恢复目标（TryResolveExactHotkeyTarget 十步校验链原样移植）。
        /// </summary>
        private static bool TryResolveExactHotkeyTarget(PlayerInventory inventory,
            HotkeyRestoreEntry entry, out ItemJar jar, out ItemAsset asset, out string reason)
        {
            jar = null;
            asset = null;
            reason = null;

            if (inventory == null || inventory.items == null)
            {
                reason = "inventory/items is null";
                return false;
            }
            if (entry == null || entry.HotkeyIndex >= HotkeySnapshotUtil.HOTKEY_COUNT)
            {
                reason = "entry is null or hotkey index is out of range";
                return false;
            }
            if (entry.NewPage < HotkeySnapshotUtil.TIDYABLE_PAGE_MIN ||
                entry.NewPage > HotkeySnapshotUtil.TIDYABLE_PAGE_MAX ||
                entry.NewPage >= inventory.items.Length)
            {
                reason = "target page is out of range";
                return false;
            }

            Items page = inventory.items[entry.NewPage];
            if (page == null || entry.NewX >= page.width || entry.NewY >= page.height)
            {
                reason = "target coordinate is outside page";
                return false;
            }

            byte index = page.getIndex(entry.NewX, entry.NewY);
            if (index == byte.MaxValue)
            {
                reason = "no ItemJar at target coordinate";
                return false;
            }

            ItemJar candidate = page.getItem(index);
            if (candidate == null || candidate.item == null)
            {
                reason = "target ItemJar/item is null";
                return false;
            }
            // getIndex may identify a multi-cell jar from a covered cell. Binding must always use its origin.
            if (candidate.x != entry.NewX || candidate.y != entry.NewY)
            {
                reason = $"target is covered cell; jar origin=({candidate.x},{candidate.y})";
                return false;
            }

            ItemFingerprint actual = new ItemFingerprint(candidate.item);
            if (!actual.Equals(entry.ExpectedFingerprint))
            {
                reason = $"fingerprint mismatch expected=id:{entry.ExpectedFingerprint.Id}/amt:{entry.ExpectedFingerprint.Amount}/q:{entry.ExpectedFingerprint.Quality}, " +
                         $"actual=id:{actual.Id}/amt:{actual.Amount}/q:{actual.Quality}";
                return false;
            }

            ItemAsset candidateAsset = candidate.GetAsset();
            if (candidateAsset == null)
            {
                reason = "ItemAsset is null";
                return false;
            }
            if (!ItemTool.checkUseable(entry.NewPage, candidateAsset.id))
            {
                reason = $"ItemTool.checkUseable rejected id={candidateAsset.id} on page={entry.NewPage}";
                return false;
            }

            jar = candidate;
            asset = candidateAsset;
            return true;
        }

        private static bool TryRestoreOneHotkey(Player player, HotkeyRestoreEntry entry,
            bool canVerify, out bool verified, out string reason)
        {
            verified = false;
            reason = null;

            if (player == null || player.equipment == null)
            {
                reason = "player/equipment is null";
                return false;
            }

            ItemJar jar;
            ItemAsset asset;
            if (!TryResolveExactHotkeyTarget(player.inventory, entry, out jar, out asset, out reason))
                return false;

            player.equipment.ServerBindItemHotkey(
                entry.HotkeyIndex, asset, entry.NewPage, entry.NewX, entry.NewY);

            if (!canVerify)
                return true;

            verified = VerifyHotkeyBound(player, entry.HotkeyIndex,
                entry.NewPage, entry.NewX, entry.NewY, entry.ExpectedFingerprint.Id);
            if (!verified)
            {
                reason = "ServerBindItemHotkey returned but HotkeyInfo did not match expected id/page/x/y";
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // 回滚成功：按原坐标恢复热键（TryRestoreHotkeysToOriginalPositions 原样移植）
        // ─────────────────────────────────────────────────────────────

        private struct HotkeyRestoreOutcome
        {
            public int Attempted;
            public int Succeeded;
            public int Failed;
            public bool AllVerified => Failed == 0 && Attempted == Succeeded;
        }

        private static HotkeyRestoreOutcome TryRestoreHotkeysToOriginalPositions(Player player,
            Dictionary<ItemJar, HotkeySnapshot> resolvedHotkeys)
        {
            var outcome = new HotkeyRestoreOutcome();
            if (player?.equipment == null || resolvedHotkeys == null) return outcome;

            outcome.Attempted = resolvedHotkeys.Count;
            foreach (var kv in resolvedHotkeys)
            {
                HotkeySnapshot snap = kv.Value;
                try
                {
                    Items pageItems = player.inventory.items[snap.OldPage];
                    if (pageItems == null) { outcome.Failed++; continue; }
                    byte jarIdx = pageItems.getIndex(snap.OldX, snap.OldY);
                    if (jarIdx == byte.MaxValue) { outcome.Failed++; continue; }
                    ItemJar jar = pageItems.getItem(jarIdx);
                    if (jar?.item == null || jar.item.id != snap.ExpectedItemId) { outcome.Failed++; continue; }

                    ItemAsset asset = jar.GetAsset();
                    if (asset == null || !ItemTool.checkUseable(snap.OldPage, asset.id))
                    {
                        outcome.Failed++;
                        continue;
                    }

                    player.equipment.ServerBindItemHotkey(snap.HotkeyIndex, asset, snap.OldPage, snap.OldX, snap.OldY);
                    outcome.Succeeded++;
                }
                catch (Exception e)
                {
                    LitRuntime.LogWarning($"[Tidy] 回滚后恢复热键 {snap.HotkeyIndex} 异常: {e.Message}");
                    outcome.Failed++;
                }
            }

            TidyDiagnosticLog.Info("local-rollback-hotkeys",
                $"[Tidy] 回滚后的热键恢复：attempted={outcome.Attempted}, succeeded={outcome.Succeeded}, failed={outcome.Failed}。");
            return outcome;
        }
    }
}
