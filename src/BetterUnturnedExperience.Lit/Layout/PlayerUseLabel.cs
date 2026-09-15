using System;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-02 (V5-T3 frozen): the player-use label set the unified
    /// tagged-row-band layout segments by. The enum ORDER is the frozen
    /// segment order — 远程武器、弹匣、医疗用品、近战武器、投掷物、弹药、
    /// 弹药箱、枪械配件、食物、饮水、工具、服装与防护装备、背包与容器、燃料、
    /// 补给与制作材料、建造、设施与陷阱、地图与指南针、钥匙、其他 — and it is
    /// NOT the EItemType enum order. The labels are internal implementation
    /// identity: they never enter the player UI, never enter the contract, and
    /// no third party loads or registers them (V5-T1: 契约 2.1 零扩面).
    /// </summary>
    internal enum PlayerUseLabel : byte
    {
        /// <summary>远程武器 (first segment).</summary>
        RangedWeapon = 0,
        /// <summary>弹匣 — only a real ItemMagazineAsset qualifies (T3: MAGAZINE 不能无条件当可用弹匣).</summary>
        Magazine = 1,
        /// <summary>医疗用品.</summary>
        Medical = 2,
        /// <summary>近战武器.</summary>
        Melee = 3,
        /// <summary>投掷物.</summary>
        Throwable = 4,
        /// <summary>弹药 (loose rounds — the explicit AMMO type wins over any fill relation).</summary>
        Ammo = 5,
        /// <summary>弹药箱 — recognized by the FillTargetItem-supplies-a-magazine blueprint relation, never by SUPPLY alone (T3 ruling).</summary>
        AmmoBox = 6,
        /// <summary>枪械配件 (sights, optics, tactical, grips, barrels).</summary>
        GunAttachment = 7,
        /// <summary>食物.</summary>
        Food = 8,
        /// <summary>饮水.</summary>
        Drink = 9,
        /// <summary>工具.</summary>
        Tool = 10,
        /// <summary>服装与防护装备 (hat/pants/shirt/mask/glasses/vest).</summary>
        ClothingArmor = 11,
        /// <summary>背包与容器.</summary>
        BackpackContainer = 12,
        /// <summary>燃料.</summary>
        Fuel = 13,
        /// <summary>补给与制作材料 (a SUPPLY with no magazine-fill relation).</summary>
        SupplyCraft = 14,
        /// <summary>建造 (barricades, structures, farms, growers).</summary>
        Build = 15,
        /// <summary>设施与陷阱 (traps, charges, detonators, beacons, deployable storage, base machinery).</summary>
        FacilityTrap = 16,
        /// <summary>地图与指南针.</summary>
        MapCompass = 17,
        /// <summary>钥匙.</summary>
        Key = 18,
        /// <summary>其他 — the classification-failure home (T3: 分类失败进「其他」，不得丢物).</summary>
        Other = 19,
    }

    /// <summary>Frozen diagnostics name table for the layout labels (log/test
    /// evidence only — this never reaches a player surface).</summary>
    internal static class PlayerUseLabels
    {
        internal const int Count = 20;

        internal static string DiagnosticName(PlayerUseLabel label)
        {
            switch (label)
            {
                case PlayerUseLabel.RangedWeapon: return "远程武器";
                case PlayerUseLabel.Magazine: return "弹匣";
                case PlayerUseLabel.Medical: return "医疗用品";
                case PlayerUseLabel.Melee: return "近战武器";
                case PlayerUseLabel.Throwable: return "投掷物";
                case PlayerUseLabel.Ammo: return "弹药";
                case PlayerUseLabel.AmmoBox: return "弹药箱";
                case PlayerUseLabel.GunAttachment: return "枪械配件";
                case PlayerUseLabel.Food: return "食物";
                case PlayerUseLabel.Drink: return "饮水";
                case PlayerUseLabel.Tool: return "工具";
                case PlayerUseLabel.ClothingArmor: return "服装与防护装备";
                case PlayerUseLabel.BackpackContainer: return "背包与容器";
                case PlayerUseLabel.Fuel: return "燃料";
                case PlayerUseLabel.SupplyCraft: return "补给与制作材料";
                case PlayerUseLabel.Build: return "建造";
                case PlayerUseLabel.FacilityTrap: return "设施与陷阱";
                case PlayerUseLabel.MapCompass: return "地图与指南针";
                case PlayerUseLabel.Key: return "钥匙";
                default: return "其他";
            }
        }
    }
}
