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
    //
    // DEV-V2-13: the pause-menu entry joins the native PlayerPauseUI column
    // instead of floating beside it. Vanilla column (decompiled): every
    // button at X=-100, 200x50, PositionScale 0.5/0.5, Return at Y=-290,
    // 60px pitch; onSpyReady moves the whole column to X=-435 (and vanilla
    // never moves it back until the UI is rebuilt). BUE slots directly below
    // Return, and every vanilla element below Return (including the suicide
    // disabled label, which shares the suicide slot) shifts down one pitch.
    internal static class BueMenuEntryLayout
    {
        internal const float MainMenuColumnButtonX = 0f;
        internal const float MainMenuColumnButtonWidth = 200f;
        internal const float MainMenuColumnButtonHeight = 50f;
        internal const float MainMenuColumnSlotPitch = 60f;
        internal const float MainMenuStoreSlotY = 410f;
        internal const float MainMenuBueSlotY = MainMenuStoreSlotY + MainMenuColumnSlotPitch;

        internal const float PauseColumnButtonX = -100f;
        internal const float PauseColumnButtonWidth = 200f;
        internal const float PauseColumnButtonHeight = 50f;
        internal const float PauseColumnSlotPitch = 60f;
        internal const float PauseReturnSlotY = -290f;
        internal const float PauseBueSlotY = PauseReturnSlotY + PauseColumnSlotPitch;
        internal const float PauseSpyColumnButtonX = -435f;

        // Vanilla PlayerPauseUI static fields below Return, shifted down one
        // pitch by reflection. suicideDisabledLabel shares the suicide slot,
        // so it must travel with the button. Return and the container are
        // intentionally absent.
        internal static readonly string[] PauseShiftFieldNames =
        {
            "inviteFriendsButton",
            "optionsButton",
            "displayButton",
            "graphicsButton",
            "controlsButton",
            "audioButton",
            "suicideButton",
            "suicideDisabledLabel",
            "exitButton",
            "quitButton"
        };

        // inviteFriendsButton only exists when the vanilla constructor creates
        // it (Steam overlay enabled and not hosting), so its absence is normal.
        // Every other manifest field is required: a missing required field
        // fails the pause entry closed (native column untouched) instead of
        // partially shifting the column onto the BUE slot.
        internal static readonly string[] PauseOptionalShiftFieldNames = { "inviteFriendsButton" };
    }
}
