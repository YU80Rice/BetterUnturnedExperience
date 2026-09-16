using System;
using System.Collections.Generic;
using SDG.Unturned;

// DEV-V5-04 host rule (inherited from DEV-V5-03's MOUNT_PAGE lesson): the
// PlayerInventory TYPE must not appear in this signature — its static
// initializer (NetReflection wiring) is unreachable outside the game process,
// so the planner takes the page array itself and the adapter reads
// inventory.items only on the engine side (InsertRecoverEngine.ReadPages).
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-04 (V5-T5): the insert-recover planner — 五页连同待加入物品一起
    /// 交给统一排版 (spec: 把玩家五页连同这件新东西一起交给统一排版再试一次).
    /// The composition rule lives HERE, the algorithm never does: every page
    /// plan goes through the caller-injected <see cref="ITidyStrategy"/> (in
    /// production the ONE tagged-row-band-v1 adapter of DEV-V5-02), and every
    /// preparation is the SAME <see cref="ManualTidyService.PreparePage"/> the
    /// button tidy prepares with — no second solver, no second validator.
    ///
    /// Candidate order mirrors vanilla's own auto-add loop
    /// (tryAddItemAuto: pages 2..6 ascending, V5-T5 Q2 的「复现同一顺序，不得
    /// 升格」), and the candidate page is the page the pending item joins the
    /// plan of. ALL active pages are prepared (the whole five pages enter the
    /// unified layout, not just the landing page) — the button 全身整理's
    /// shape plus the one new item. Any page whose own plan is invalid
    /// (oversized jar etc.) refuses the WHOLE attempt with zero mutation,
    /// exactly like the button all-pages transaction (硬性「要么整页合法、要么
    /// 明确失败」). The plan never writes the inventory — the caller commits
    /// atomically or not at all.
    /// </summary>
    internal static class InsertRecoverPlanner
    {
        /// <summary>
        /// Builds the recover transaction: true = every active player page has
        /// a valid preparation and the pending item is planned onto
        /// <paramref name="pendingPage"/> (the lowest active page in vanilla
        /// order that can take it). false = zero-mutation refusal with the
        /// refusal identity (no page set is produced).
        /// </summary>
        internal static bool TryBuild(
            Items[] items, Item pending, InsertRecoverAdapter.PendingInfo info,
            bool sortDescending, TidyMode mode, ITidyStrategy strategy,
            out List<PagePreparation> preps, out byte pendingPage, out string refusal)
        {
            preps = null;
            pendingPage = 0;
            refusal = null;
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));

            if (items == null) { refusal = InsertRecoverAdapter.RefusalNoActivePages; return false; }

            // Pass 1 — every ACTIVE player page 2..6, planned without the
            // pending item (the button 全身 range rule, same skip semantics:
            // 0×0 / null pages are vanilla-inactive and never enter).
            var basePreps = new List<PagePreparation>(5);
            var activePages = new List<byte>(5);
            for (var page = HotkeySnapshotUtil.TIDYABLE_PAGE_MIN; page <= HotkeySnapshotUtil.TIDYABLE_PAGE_MAX; page++)
            {
                if (page >= items.Length) break;
                var pageItems = items[page];
                if (pageItems == null || pageItems.width == 0 || pageItems.height == 0) continue;
                PagePreparation basePrep;
                try
                {
                    basePrep = ManualTidyService.PreparePage(pageItems, page, sortDescending, mode, strategy);
                }
                catch (Exception)
                {
                    // A crashing plan for a page is a refusal of the whole
                    // attempt — the caller keeps vanilla failure semantics.
                    refusal = InsertRecoverAdapter.RefusalCannotFit;
                    return false;
                }
                if (!basePrep.Valid)
                {
                    // 该页连自己的东西都排不出合法布局 → 整单失败（与按钮
                    // TidyAll 的「任一活动页 Prepare 失败即零修改拒绝」同律）。
                    refusal = InsertRecoverAdapter.RefusalCannotFit;
                    return false;
                }
                basePreps.Add(basePrep);
                activePages.Add(page);
            }
            if (activePages.Count == 0)
            {
                refusal = InsertRecoverAdapter.RefusalNoActivePages;
                return false;
            }

            // Pass 2 — candidates ascending: page k's plan = its own items +
            // the pending item (one extra packable through the SAME seam).
            for (var i = 0; i < activePages.Count; i++)
            {
                var k = activePages[i];
                var pageItems = items[k];
                var packable = new PackableItem
                {
                    // Tag = the Item itself (no jar yet): the transaction's
                    // pending-aware validators and commit treat exactly this
                    // reference as the item-to-be-added.
                    Tag = pending,
                    size_x = info.SizeX,
                    size_y = info.SizeY,
                    GroupKey = pending.id,
                    StableOrder = pageItems.getItemCount(),
                    OriginalX = 0,
                    OriginalY = 0,
                    OriginalRot = 0,
                    PreferredRotation = 0,
                    // The label rides the SAME 02 classifier seam the button
                    // tidy fills from (PreparePage resolves jar labels through
                    // it); classification failure lands in 其他, never drops.
                    Label = ItemUseSignalsProvider.ResolveFor(pending),
                };
                PagePreparation candidatePrep;
                try
                {
                    candidatePrep = ManualTidyService.PreparePage(pageItems, k, sortDescending, mode, strategy, packable);
                }
                catch (Exception)
                {
                    continue; // 崩溃的候选不算候选
                }
                if (!candidatePrep.Valid) continue;

                // Winner: swap the base preparation of page k for the pending
                // one — one atomic all-pages transaction commits with it.
                var finalPreps = new List<PagePreparation>(basePreps.Count);
                for (var b = 0; b < basePreps.Count; b++)
                {
                    finalPreps.Add(basePreps[b].Page == k ? candidatePrep : basePreps[b]);
                }
                preps = finalPreps;
                pendingPage = k;
                return true;
            }

            refusal = InsertRecoverAdapter.RefusalCannotFit;
            return false;
        }
    }
}
