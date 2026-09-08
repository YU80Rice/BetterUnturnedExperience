using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the repack success toast — the old RepackToast migrated
    /// verbatim (native EPlayerMessage.NPC_CUSTOM rendering, bottom-anchored
    /// like the vanilla reload hint, never intercepting the vanilla message
    /// area). The module binds it as the production ToastSink; host tests
    /// record into their own sink instead.
    /// </summary>
    internal static class LirToast
    {
        internal static void Show(string message)
        {
            PlayerUI.message(EPlayerMessage.NPC_CUSTOM, message, ReloadRuntimePolicy.ToastDurationSeconds);
        }
    }
}
