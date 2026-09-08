using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: intercepts PlayerInventory.forceAddItem(Item, bool) — the
    /// call ReceiveAttachMagazine makes to put the OLD magazine back. THE
    /// OVERLAY POINT: any other writer of this method (a BII drag-in placing
    /// an item, vanilla pickups) reaches this prefix with NO reload context —
    /// the guard refuses, the prefix returns true and the native
    /// tryFindSpace/dropItem path runs untouched (the red-pinned seam).
    /// With a valid context:
    ///   1. the recorded rot is tried first (old magazine size vs the freed
    ///      slot via checkSpaceEmpty);
    ///   2. bag pages (page >= SLOTS) with non-square items try the 90°
    ///      rotation;
    ///   3. a fit writes in place via items[page].addItem (the internal
    ///      onItemAdded/onStateUpdated events drive the native net sync);
    ///   4. returning false skips the vanilla tryFindSpace/dropItem;
    ///   5. a size mismatch or no fit passes through — the native space
    ///      search decides (never a drop in place of the game).
    /// Magazines are not weapon/useable/clothing, so no auto-equip applies.
    /// </summary>
    [HarmonyPatch(typeof(PlayerInventory), "forceAddItem",
                  new[] { typeof(Item), typeof(bool) })]
    internal static class ForceAddItemPatch
    {
        static bool Prefix(PlayerInventory __instance, Item item)
        {
            if (item == null) return true;

            var module = InPlaceReloadModule.ActiveModule;
            if (module == null) return true;

            if (!module.Guard.TryConsumeSlot(out var slot))
            {
                return true;
            }

            ItemAsset asset = item.GetAsset();
            if (asset == null) return true;

            // Size safety gate: a strict 1:1 coordinate swap only when the old
            // magazine physically matches the freed slot; otherwise the native
            // tryFindSpace space search decides (the old behavior).
            if (asset.size_x != slot.SizeX || asset.size_y != slot.SizeY)
            {
                return true;
            }

            byte rot = slot.Rot;
            bool fits = __instance.checkSpaceEmpty(slot.Page, slot.X, slot.Y, asset.size_x, asset.size_y, rot);

            if (!fits && slot.Page >= PlayerInventory.SLOTS && asset.size_x != asset.size_y)
            {
                rot = (byte)(slot.Rot == 0 ? 1 : 0);
                fits = __instance.checkSpaceEmpty(slot.Page, slot.X, slot.Y, asset.size_x, asset.size_y, rot);
            }

            if (!fits) return true;

            __instance.items[slot.Page].addItem(slot.X, slot.Y, rot, item);
            return false;
        }
    }
}
