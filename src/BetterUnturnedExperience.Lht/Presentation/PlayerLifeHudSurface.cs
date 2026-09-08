using System;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the production HUD surface — the IHordeHudSurface binding
    /// over the migrated PlayerLifeUiHudPatches. The Harmony patch itself is
    /// installed by the module (under the FeatureId Harmony ID, gated by the
    /// batch-mode fact); the surface manages the label cache lifecycle and
    /// forwards the render calls.
    /// </summary>
    internal sealed class PlayerLifeHudSurface : IHordeHudSurface
    {
        /// <summary>The patch type the module installs (HUD injection, client UI only).</summary>
        internal static Type PatchType => typeof(PlayerLifeUiHudPatches);

        public bool Install()
        {
            // The label arrives through the patch's constructor postfix; the
            // surface only resets its caches so a re-arm starts clean.
            PlayerLifeUiHudPatches.Cleanup();
            return true;
        }

        public void Uninstall()
        {
            PlayerLifeUiHudPatches.Cleanup();
        }

        public bool IsLabelReady()
        {
            return PlayerLifeUiHudPatches.IsLabelReady();
        }

        public void SetText(string text)
        {
            PlayerLifeUiHudPatches.SetText(text);
        }

        public void SetVisible(bool visible)
        {
            PlayerLifeUiHudPatches.SetVisible(visible);
        }

        public void DrainDisconnectReset()
        {
            PlayerLifeUiHudPatches.DrainClientDisconnectReset();
        }
    }
}
