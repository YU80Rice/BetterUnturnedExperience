namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the HUD surface seam (spec「表现 adapter 变体位」). The
    /// presentation adapter renders through this interface only — the
    /// production surface is the migrated PlayerLifeUI reflection injection;
    /// a future chat-broadcast or panel variant implements the same seam;
    /// a genuinely independent capability is a NEW feature module.
    /// </summary>
    internal interface IHordeHudSurface
    {
        /// <summary>Prepares the surface (idempotent). false = unavailable
        /// (the implementation logs its own diagnostics).</summary>
        bool Install();

        /// <summary>Releases the surface and its caches (the generation boundary).</summary>
        void Uninstall();

        /// <summary>Whether the injected label is live and writable.</summary>
        bool IsLabelReady();

        void SetText(string text);

        void SetVisible(bool visible);

        /// <summary>The client-disconnect drain (the label lifecycle protection).</summary>
        void DrainDisconnectReset();
    }
}
