using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Placement;
using BetterUnturnedExperience.Core.Registration;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// BUE-owned composition boundary for the embedded official ClientUi.
    /// It performs the environment gate before invoking any component factory.
    /// </summary>
    internal sealed class BueClientUiCompositionRoot
    {
        private readonly ClientUiCompositionRoot composition;
        private readonly IClientUiRoot root = new BueClientUiRoot();
        private readonly BetterItemInteractionSettingsState settingsState;
        private readonly BueManagementPanelRuntime managementPanel;
        private readonly LoadedPluginCatalogAdapter loadedPluginAdapter;
        private readonly bool clientUiAvailable;
        private int factoryInvocationCount;

        internal BueClientUiCompositionRoot()
        {
            settingsState = new BetterItemInteractionSettingsState();
            var feature = BetterItemInteractionSettingsState.Feature;
            var registry = new GeneratedClientUiRegistry(new[]
            {
                new ClientUiRegistration(feature, CreateOfficialComponent)
            });
            composition = new ClientUiCompositionRoot(registry);
            clientUiAvailable = true;
            loadedPluginAdapter = new LoadedPluginCatalogAdapter();
            // Keep construction free of Unity static calls so headless/test hosts
            // can probe the composition root without invoking native ECalls.
            var preferencesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BetterUnturnedExperience", "management.preferences");
            managementPanel = new BueManagementPanelRuntime(preferencesPath, new BetterItemInteractionSettingsEditor(settingsState), loadedPluginAdapter);
        }

        internal bool IsReady { get { return composition.State == ClientUiCompositionState.Ready; } }
        internal ClientUiCompositionState State { get { return composition.State; } }
        internal int FactoryInvocationCount { get { return factoryInvocationCount; } }
        internal IReadOnlyList<FeatureId> IsolatedFeatureIds { get { return composition.IsolatedFeatureIds; } }
        internal BueManagementPanelRuntime ManagementPanel { get { return managementPanel; } }

        internal bool Initialize(bool isBatchMode, bool headless, bool nativeUiAvailable)
        {
            var environment = new ClientUiEnvironment(nativeUiAvailable && clientUiAvailable, isBatchMode, headless);
            var initialized = composition.Initialize(environment, root);
            if (initialized)
            {
                managementPanel.Initialize();
                RefreshManagementPanel();
            }
            return initialized;
        }

        internal void RefreshManagementPanel()
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            var entries = runtime == null || runtime.Catalog == null
                ? new[] { OfficialManagementEntry() }
                : runtime.Catalog.Entries.Select(ToManagementEntry).ToArray();
            managementPanel.Refresh(entries, loadedPluginAdapter.CaptureLoadedPlugins());
        }

        private BueFeatureManagementEntry OfficialManagementEntry()
        {
            var feature = BetterItemInteractionSettingsState.Feature;
            return new BueFeatureManagementEntry(feature, "Better Item Interaction", "1.0.0", FeatureState.Running,
                new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1), settingsState.GetSnapshot());
        }

        private BueFeatureManagementEntry ToManagementEntry(FeatureRegistrationEntry entry)
        {
            var feature = entry.Definition.Feature;
            if (feature.Value == BetterItemInteractionSettingsState.Feature.Value) return OfficialManagementEntry();
            var presentation = entry.ClientUi == null
                ? new FeaturePresentationView(feature, FeaturePresentationState.PresentationDegraded, "BUE-UI-SATELLITE-001", 1)
                : new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1);
            return new BueFeatureManagementEntry(feature, feature.Value, "0.0.0", FeatureState.Running, presentation, default(FeatureSettingsSnapshot));
        }

        internal void OpenInventory(IClientUiInventorySurface surface)
        {
            composition.OpenInventory(surface);
        }

        internal void CloseInventory()
        {
            composition.CloseInventory();
        }

        internal void Destroy()
        {
            managementPanel.Destroy();
            composition.Destroy();
        }

        internal void EnterSafeMode(Action<string> diagnostic)
        {
            composition.EnterSafeMode(diagnostic);
        }

        private IClientUiFeatureComponent CreateOfficialComponent()
        {
            factoryInvocationCount++;
            return new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new PlacementCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8), settingsState);
        }

        private sealed class BueClientUiRoot : IClientUiRoot { }
    }
}
