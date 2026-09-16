using System;
using System.Collections.Generic;
using SDG.Unturned;

// DEV-V5-04 host rule (inherited): the PlayerInventory TYPE must not appear in
// this signature — the planner takes the page array + the mounted container
// grid; the engine-facing reads stay in the adapter/authority (05 同界).
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-05 (V5-T5): the fast-transfer recover planner — 只整理接收的那一侧
    /// 再试这一件, as ONE two-page transaction. The source side gets the
    /// IDENTITY preparation (ManualTidyService.PreparePageLeave: everything
    /// except the moved jar at its exact coordinates — 「源网格不整理」), the
    /// receiving side gets the DEV-V5-02 unified layout WITH the moved item as
    /// the pending packable (04 semantics). No third page enters the list:
    /// a player-page tidy here is exactly one page (不得升格全身整理), and the
    /// candidate order mirrors vanilla's own scan (tryFindSpace walks pages
    /// SLOTS..PANTS ascending — the ticket's「原版选定的那一页」under failure is
    /// the first page whose tidy+place plan succeeds in that same order).
    /// The composition rule lives HERE; the algorithm never does.
    /// </summary>
    internal static class FastTransferRecoverPlanner
    {
        /// <summary>Vanilla's destination-count gate (PlayerInventory.
        /// ReceiveDragItem refuses when the target page already holds 200
        /// jars) — recovery reproduces it, never exceeds it.</summary>
        internal const int DEST_JAR_LIMIT = 200;

        internal const string RefusalSourceGone = "source-gone";
        internal const string RefusalCannotFit = "cannot-fit";

        /// <summary>
        /// Build the two preparations. true = both pages hold a valid plan and
        /// the moved item is on the receiving plan; false = zero-mutation
        /// refusal with the refusal identity (no page set is produced).
        /// <paramref name="sourcePage"/> is 7 (container→player) or a player
        /// page 2..6 (player→container); anything else is refused by the
        /// caller's gates before reaching here.
        /// </summary>
        internal static bool TryBuild(
            Items[] pages, Items containerGrid, byte sourcePage, byte sourceX, byte sourceY,
            bool sortDescending, TidyMode mode, ITidyStrategy strategy,
            out List<PagePreparation> preps, out byte targetPage, out string refusal)
        {
            preps = null;
            targetPage = 0;
            refusal = null;
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            if (containerGrid == null) { refusal = RefusalSourceGone; return false; }

            // ── the moved jar, as the AUTHORITY's own read states it ──
            var sourceIsContainer = sourcePage == LitContainerTidyExecution.MOUNT_PAGE;
            Items sourceItems;
            if (sourceIsContainer) sourceItems = containerGrid;
            else if (pages != null && sourcePage >= HotkeySnapshotUtil.TIDYABLE_PAGE_MIN
                     && sourcePage <= HotkeySnapshotUtil.TIDYABLE_PAGE_MAX && sourcePage < pages.Length)
                sourceItems = pages[sourcePage];
            else { refusal = RefusalSourceGone; return false; }
            if (sourceItems == null || sourceItems.width == 0 || sourceItems.height == 0)
            {
                refusal = RefusalSourceGone;
                return false;
            }
            // 源件查找走纯列表扫描（jar 原点匹配）：Items.getIndex 会读
            // PlayerInventory.SLOTS 静态字段——宿主无引擎必 cctor 爆（04 同界）；
            // 语义与原版 getIndex 的原点等值一致（右键命中的就是图标锚点格）。
            var jar = FindJarAtOrigin(sourceItems, sourceX, sourceY);
            if (jar == null || jar.item == null) { refusal = RefusalSourceGone; return false; }
            var moved = jar.item;

            // ── source page: identity-minus-this-item (no strategy, no reflow) ──
            PagePreparation sourcePrep;
            try
            {
                sourcePrep = ManualTidyService.PreparePageLeave(sourceItems, sourcePage, jar);
            }
            catch (Exception)
            {
                refusal = RefusalCannotFit;
                return false;
            }
            if (!sourcePrep.Valid)
            {
                // 源页连自己的恒等布局都非法（异常数据/重叠）→ 整单零修改。
                refusal = RefusalCannotFit;
                return false;
            }

            // ── receiving side: 02 plan + pending ──
            if (sourceIsContainer)
            {
                // 箱子→玩家：原版固定页序 2..6 升序，第一个「整理+放入」成立的页。
                if (pages == null) { refusal = RefusalCannotFit; return false; }
                for (var page = HotkeySnapshotUtil.TIDYABLE_PAGE_MIN;
                     page <= HotkeySnapshotUtil.TIDYABLE_PAGE_MAX; page++)
                {
                    if (page >= pages.Length) break;
                    var pageItems = pages[page];
                    if (pageItems == null || pageItems.width == 0 || pageItems.height == 0) continue; // 原生不活动页（04 同规）
                    if (pageItems.getItemCount() >= DEST_JAR_LIMIT) continue; // 原版满闸
                    var candidate = TryBuildTarget(pageItems, (byte)page, moved, jar, sortDescending, mode, strategy);
                    if (candidate == null) continue;
                    preps = new List<PagePreparation> { sourcePrep, candidate };
                    targetPage = (byte)page;
                    return true;
                }
                refusal = RefusalCannotFit;
                return false;
            }
            else
            {
                // 玩家→箱子：接收侧 = 当前受支持容器网格（范围与 03 一致；kind/
                // 会话/版本已由调用方的 03 verifier 重验）。7→7 不是恢复对象
                // （源即目标，「整理源」被禁，票面具名排除）。
                if (containerGrid.getItemCount() >= DEST_JAR_LIMIT) { refusal = RefusalCannotFit; return false; }
                var target = TryBuildTarget(containerGrid, LitContainerTidyExecution.MOUNT_PAGE, moved, jar, sortDescending, mode, strategy);
                if (target == null) { refusal = RefusalCannotFit; return false; }
                preps = new List<PagePreparation> { sourcePrep, target };
                targetPage = LitContainerTidyExecution.MOUNT_PAGE;
                return true;
            }
        }

        private static ItemJar FindJarAtOrigin(Items items, byte x, byte y)
        {
            var jars = items.items;
            for (byte i = 0; i < jars.Count; i++)
            {
                var jar = jars[i];
                if (jar != null && jar.x == x && jar.y == y) return jar;
            }
            return null;
        }

        private static PagePreparation TryBuildTarget(
            Items grid, byte page, Item moved, ItemJar jar,
            bool sortDescending, TidyMode mode, ITidyStrategy strategy)
        {
            var pending = new PackableItem
            {
                // Tag = the live Item instance (sits in the source jar until the
                // transaction removes that jar first — vanilla ReceiveDragItem's
                // remove-then-add keeps the SAME reference; 04's pending shape).
                Tag = moved,
                size_x = jar.size_x,
                size_y = jar.size_y,
                GroupKey = moved.id,
                StableOrder = grid.getItemCount(),
                OriginalX = 0,
                OriginalY = 0,
                OriginalRot = 0,
                PreferredRotation = 0,
                // The label rides the SAME 02 classifier seam (04 planner rule):
                // classification failure lands in 其他, never drops.
                Label = ItemUseSignalsProvider.ResolveFor(moved),
            };
            try
            {
                var prep = ManualTidyService.PreparePage(grid, page, sortDescending, mode, strategy, pending);
                return prep != null && prep.Valid ? prep : null;
            }
            catch (Exception)
            {
                return null; // 崩溃的候选不算候选（04 同规）
            }
        }
    }
}
