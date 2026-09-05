namespace BetterUnturnedExperience.Plugin
{
    // DEV-V2-09: geometry of the injected main-menu left-column entry, pinned
    // to the vanilla MenuDashboardUI rhythm. Every vanilla entry in that column
    // is a 200x50 button on a 60px vertical pitch (Play 170 / Survivors 230 /
    // Configuration 290 / Workshop 350 / item-store 410), leaving a 10px visual
    // gap between adjacent items. BUE takes the next pitch slot below the
    // store entry: flush (y=460) left a zero gap, breaking the vanilla rhythm.
    // The store button is created by the game only when store listings exist
    // and Glazier exposes no child enumeration, so the slot is fixed rather
    // than probed at runtime; with the store present (the normal case) the gap
    // above is exactly the vanilla 10px.
    internal static class BueMenuEntryLayout
    {
        internal const float MainMenuColumnButtonX = 0f;
        internal const float MainMenuColumnButtonWidth = 200f;
        internal const float MainMenuColumnButtonHeight = 50f;
        internal const float MainMenuColumnSlotPitch = 60f;
        internal const float MainMenuStoreSlotY = 410f;
        internal const float MainMenuBueSlotY = MainMenuStoreSlotY + MainMenuColumnSlotPitch;
    }
}
