using System;
using System.Collections.Generic;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-02: the engine-side half of the classifier seam — turns a real
    /// Unturned <see cref="Item"/> into the frozen label through
    /// <see cref="PlayerUseClassifier"/>, plus the lazy process-wide ammo-box
    /// index: an id is a fill supply when it appears among the supplies of
    /// some ItemMagazineAsset's FillTargetItem blueprint (V5-T3: 弹药箱按给弹匣
    /// 供弹的蓝图认，不单看 SUPPLY — the same relation the production LIR repack
    /// matching already consumes). Every asset lookup is failure-guarded: an
    /// unresolved or throwing lookup classifies to 其他 and NEVER drops the
    /// item, so a headless/test host without item assets stays safe.
    /// </summary>
    internal static class ItemUseSignalsProvider
    {
        private static readonly object sync = new object();
        private static HashSet<ushort> fillSupplyIds;
        private static int builtFromAssetCount = -1;

        /// <summary>Host-test seam: when set, overrides the label for the given
        /// item (the frozen-table tests exercise the mapping directly through
        /// PlayerUseClassifier.Classify instead). Null = production path.</summary>
        internal static Func<Item, PlayerUseLabel?> ResolveForTests;

        internal static PlayerUseLabel ResolveFor(Item item)
        {
            if (item == null) return PlayerUseLabel.Other;
            var testOverride = ResolveForTests;
            if (testOverride != null)
            {
                var forced = testOverride(item);
                if (forced.HasValue) return forced.Value;
            }
            try
            {
                var signals = BuildSignals(item);
                return PlayerUseClassifier.Classify(signals);
            }
            catch (Exception)
            {
                // Classification failure lands in 其他 — the layout must never
                // lose an item over an asset lookup fault.
                return PlayerUseLabel.Other;
            }
        }

        private static PlayerUseSignals BuildSignals(Item item)
        {
            var signals = new PlayerUseSignals
            {
                Id = item.id,
                TypeKnown = false,
                IsMagazineAsset = false,
                IsCaliberAsset = false,
                IsFillSupply = IsFillSupply(item.id),
            };
            ItemAsset asset = null;
            try { asset = item.GetAsset(); }
            catch (Exception) { asset = null; }
            if (asset == null) return signals;
            signals.TypeKnown = true;
            signals.Type = asset.type;
            signals.IsMagazineAsset = asset is ItemMagazineAsset;
            signals.IsCaliberAsset = asset is ItemCaliberAsset;
            return signals;
        }

        /// <summary>True when the id supplies some magazine's FillTargetItem
        /// blueprint. Empty (never throwing) when the asset database is not
        /// available — a box then falls back to its plain type mapping.</summary>
        internal static bool IsFillSupply(ushort id)
        {
            try
            {
                lock (sync)
                {
                    EnsureIndex();
                    var set = fillSupplyIds;
                    return set != null && set.Contains(id);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void EnsureIndex()
        {
            List<ItemAsset> all;
            try
            {
                // Non-deprecated form of the whole-database lookup (the EAssetType
                // array overload warns as obsolete under TreatWarningsAsErrors).
                all = new List<ItemAsset>();
                Assets.find(all);
            }
            catch (Exception) { all = null; }
            var assets = all == null ? 0 : all.Count;
            if (fillSupplyIds != null && builtFromAssetCount == assets) return;

            var set = new HashSet<ushort>();
            if (all != null)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    var mag = all[i] as ItemMagazineAsset;
                    if (mag == null) continue;
                    var blueprints = mag.blueprints;
                    if (blueprints == null) continue;
                    for (int b = 0; b < blueprints.Count; b++)
                    {
                        var bp = blueprints[b];
                        if (bp == null || bp.Operation != EBlueprintOperation.FillTargetItem) continue;
                        var supplies = bp.supplies;
                        if (supplies == null) continue;
                        for (int s = 0; s < supplies.Length; s++)
                        {
                            var supply = supplies[s];
                            if (supply == null) continue;
                            ItemAsset supplyAsset = null;
                            try { supplyAsset = supply.FindItemAsset(); }
                            catch (Exception) { supplyAsset = null; }
                            if (supplyAsset != null) set.Add(supplyAsset.id);
                        }
                    }
                }
            }
            fillSupplyIds = set;
            builtFromAssetCount = assets;
        }

        /// <summary>Host seam: forces the next index rebuild (asset reloads in
        /// tests / workshop refresh) without leaking state into production.</summary>
        internal static void ResetIndexForTests()
        {
            lock (sync)
            {
                fillSupplyIds = null;
                builtFromAssetCount = -1;
            }
        }
    }
}
