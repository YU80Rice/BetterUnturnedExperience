namespace BetterUnturnedExperience.Plugin
{
    public enum BootstrapDecision : byte { Unavailable, Client, Headless }

    public static class BootstrapGuard
    {
        public static BootstrapDecision Decide(bool isBatchMode, bool headless, bool clientUiAvailable)
        {
            if (isBatchMode || headless) return BootstrapDecision.Headless;
            return clientUiAvailable ? BootstrapDecision.Client : BootstrapDecision.Unavailable;
        }
    }
}
