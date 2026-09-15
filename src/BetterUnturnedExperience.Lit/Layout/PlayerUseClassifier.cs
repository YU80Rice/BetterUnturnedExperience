using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-02: the pure, testable signals the classifier consumes — the
    /// engine side (ItemUseSignalsProvider) extracts them from the item asset;
    /// the mapping itself never touches Unity/asset state so the frozen table
    /// is unit-testable on the plain host (red-first TDD).
    /// </summary>
    internal struct PlayerUseSignals
    {
        public ushort Id;
        /// <summary>False = the asset did not resolve (unknown/workshop-less host) — classification must fail to 其他, never drop the item.</summary>
        public bool TypeKnown;
        public EItemType Type;
        /// <summary>True only when the asset is a real ItemMagazineAsset (T3: MAGAZINE 不能无条件当可用弹匣).</summary>
        public bool IsMagazineAsset;
        /// <summary>True when the asset is a caliber asset (ItemCaliberAsset). A caliber that is not a
        /// magazine is the loose-round form of 弹药 — this game build carries no EItemType.AMMO.</summary>
        public bool IsCaliberAsset;
        /// <summary>True when the id appears as a supply of some magazine's FillTargetItem blueprint (弹药箱按蓝图认).</summary>
        public bool IsFillSupply;
    }

    /// <summary>
    /// DEV-V5-02 (V5-T3 frozen ruling): the Item → PlayerUseLabel internal
    /// seam of the unified layout module. Explicit EItemType answers win; the
    /// magazine segment requires a real magazine asset; the ammo-box segment is
    /// the FillTargetItem fill relation (never SUPPLY alone); anything without
    /// a reliable type lands in 其他. Classification failure never removes an
    /// item. This seam is internal: not player UI, not contract.
    ///
    /// Rule order (frozen): ① unresolved asset → 其他; ② the explicit single
    /// types; ③ a real ItemMagazineAsset → 弹匣 (MAGAZINE typed without the
    /// asset does NOT get the segment — T3: 不能无条件当可用弹匣); ④ a caliber
    /// that is not a magazine → 弹药 (this game build carries no AMMO item
    /// type — loose rounds are ItemCaliberAsset, the same shape the production
    /// LIR caliber matching already treats); ⑤ SUPPLY/BOX that a magazine's
    /// FillTargetItem blueprint consumes → 弹药箱; ⑥ plain SUPPLY →
    /// 补给与制作材料; ⑦ the family tables; ⑧ everything else → 其他.
    /// </summary>
    internal static class PlayerUseClassifier
    {
        internal static PlayerUseLabel Classify(in PlayerUseSignals signals)
        {
            if (!signals.TypeKnown) return PlayerUseLabel.Other;

            switch (signals.Type)
            {
                case EItemType.GUN: return PlayerUseLabel.RangedWeapon;
                case EItemType.MEDICAL: return PlayerUseLabel.Medical;
                case EItemType.MELEE: return PlayerUseLabel.Melee;
                case EItemType.THROWABLE: return PlayerUseLabel.Throwable;
                case EItemType.FOOD: return PlayerUseLabel.Food;
                case EItemType.WATER: return PlayerUseLabel.Drink;
                case EItemType.FUEL: return PlayerUseLabel.Fuel;
                case EItemType.KEY: return PlayerUseLabel.Key;
                case EItemType.MAP:
                case EItemType.COMPASS:
                    return PlayerUseLabel.MapCompass;
                case EItemType.TOOL:
                case EItemType.VEHICLE_REPAIR_TOOL:
                case EItemType.VEHICLE_PAINT_TOOL:
                case EItemType.VEHICLE_LOCKPICK_TOOL:
                    return PlayerUseLabel.Tool;
                case EItemType.SIGHT:
                case EItemType.OPTIC:
                case EItemType.TACTICAL:
                case EItemType.GRIP:
                case EItemType.BARREL:
                    return PlayerUseLabel.GunAttachment;
                case EItemType.HAT:
                case EItemType.PANTS:
                case EItemType.SHIRT:
                case EItemType.MASK:
                case EItemType.GLASSES:
                case EItemType.VEST:
                    return PlayerUseLabel.ClothingArmor;
                case EItemType.BACKPACK: return PlayerUseLabel.BackpackContainer;
                case EItemType.BARRICADE:
                case EItemType.STRUCTURE:
                case EItemType.FARM:
                case EItemType.GROWER:
                    return PlayerUseLabel.Build;
                case EItemType.STORAGE:
                case EItemType.BEACON:
                case EItemType.TRAP:
                case EItemType.CHARGE:
                case EItemType.DETONATOR:
                case EItemType.ARREST_START:
                case EItemType.ARREST_END:
                case EItemType.TANK:
                case EItemType.GENERATOR:
                case EItemType.OIL_PUMP:
                case EItemType.FILTER:
                case EItemType.SENTRY:
                    return PlayerUseLabel.FacilityTrap;
            }

            // ③④: the magazine/caliber pair before the fill relation.
            if (signals.IsMagazineAsset) return PlayerUseLabel.Magazine;
            if (signals.IsCaliberAsset) return PlayerUseLabel.Ammo;

            // ⑤⑥: the ammo-box segment is the blueprint relation, never the
            // SUPPLY label alone; an unreferenced BOX has no claimed use.
            if (signals.IsFillSupply
                && (signals.Type == EItemType.SUPPLY || signals.Type == EItemType.BOX))
                return PlayerUseLabel.AmmoBox;
            if (signals.Type == EItemType.SUPPLY) return PlayerUseLabel.SupplyCraft;

            // ⑧: everything without a reliable player-use reading lands in 其他
            // (materials/quest items/unknown workshop types) — never dropped.
            return PlayerUseLabel.Other;
        }
    }
}
