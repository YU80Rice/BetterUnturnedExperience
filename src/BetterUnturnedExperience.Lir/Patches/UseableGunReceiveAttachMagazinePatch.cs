using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: records the NEW magazine's full slot (page, x, y, rot,
    /// size_x, size_y) at the entry of UseableGun.ReceiveAttachMagazine — the
    /// slot the old magazine is written back into (the old patch migrated
    /// verbatim; the guard is the module's own instance, live only while the
    /// module is installed). Call chain: client R → SendAttachMagazine →
    /// server ReceiveAttachMagazine → forceAddItem puts the old magazine
    /// back — the Prefix/Postfix pair brackets the whole method, so a fault
    /// inside the vanilla body can never leak a slot into a later call.
    /// The detach-only branch (page == 255: drop the magazine, no
    /// replacement) never records — the one context decision this adapter
    /// owns.
    /// </summary>
    [HarmonyPatch(typeof(UseableGun), "ReceiveAttachMagazine")]
    internal static class UseableGunReceiveAttachMagazinePatch
    {
        static void Prefix(UseableGun __instance, byte page, byte x, byte y)
        {
            var module = InPlaceReloadModule.ActiveModule;
            if (module == null) return;
            module.Guard.Reset();

            if (page == 255) return;

            Player player = __instance.player;
            if (player == null) return;

            byte index = player.inventory.getIndex(page, x, y);
            if (index == 255) return;

            ItemJar jar = player.inventory.getItem(page, index);
            if (jar == null) return;

            module.Guard.BeginReload(new ReloadSlotContext(page, x, y, jar.rot, jar.size_x, jar.size_y));
        }

        static void Postfix()
        {
            var module = InPlaceReloadModule.ActiveModule;
            if (module == null) return;
            module.Guard.Reset();
        }
    }
}
