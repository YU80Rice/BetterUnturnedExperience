using BetterUnturnedExperience.Core;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>Single candidate assembly composition marker; feature behavior is deferred.</summary>
    public sealed class PluginAssemblyMarker
    {
        public CoreAssemblyMarker Core { get; } = new CoreAssemblyMarker();
    }
}
