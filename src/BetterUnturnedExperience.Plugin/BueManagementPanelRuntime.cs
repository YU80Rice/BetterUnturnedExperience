using System;
using System.Collections.Generic;
using System.Linq;
using BetterUnturnedExperience.ClientUi.Internal;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// BUE-owned management panel runtime. Native menu widgets are supplied by
    /// the later adapter slice; this class owns the stable model and lifecycle.
    /// </summary>
    internal sealed class BueManagementPanelRuntime
    {
        private readonly ManagementPanelLifecycle lifecycle;
        private readonly ManagementPanelMenuBridge menuBridge;

        internal BueManagementPanelRuntime(string preferencesPath, IBueSettingsEditor settingsEditor, IPluginConfigEditor configEditor = null)
        {
            if (settingsEditor == null) throw new ArgumentNullException(nameof(settingsEditor));
            Model = new ManagementPanelModel(new FileManagementPanelPreferencesStore(preferencesPath), settingsEditor, configEditor);
            lifecycle = new ManagementPanelLifecycle();
            menuBridge = new ManagementPanelMenuBridge(lifecycle);
        }

        internal ManagementPanelModel Model { get; }
        internal ManagementPanelMenuBridge MenuBridge { get { return menuBridge; } }

        internal void Initialize()
        {
            menuBridge.AttachMainMenu();
            menuBridge.AttachPauseMenu();
        }

        internal void Refresh(IEnumerable<BueFeatureManagementEntry> features, IEnumerable<LoadedPluginDescriptor> loadedPlugins)
        {
            var list = new List<LoadedPluginDescriptor>(loadedPlugins ?? new LoadedPluginDescriptor[0]);
            Model.Refresh(features, list);
            Model.SetExternalManagerDetected(list.Any(x => string.Equals(x.Guid, "com.trae.pluginmanager", StringComparison.Ordinal)));
        }

        internal void OnUiTreeRebuilt(IManagementPanelMount mount)
        {
            menuBridge.OnUiTreeRebuilt(mount);
        }

        internal void Destroy()
        {
            menuBridge.Destroy();
        }
    }
}
