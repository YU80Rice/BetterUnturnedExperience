using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Placement;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lit;
using BetterUnturnedExperience.Lir;
using BetterUnturnedExperience.Lht;

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
        private readonly NetworkModuleAdapter networkAdapter;
        private readonly InventoryTidyModule litModule;
        private readonly InPlaceReloadModule lirModule;
        private readonly HordeTrackerModule lhtModule;
        private readonly bool clientUiAvailable;
        private int factoryInvocationCount;

        internal BueClientUiCompositionRoot(NetworkModuleAdapter networkAdapter = null, InventoryTidyModule litModule = null, InPlaceReloadModule lirModule = null, HordeTrackerModule lhtModule = null)
        {
            this.networkAdapter = networkAdapter;
            this.litModule = litModule;
            this.lirModule = lirModule;
            this.lhtModule = lhtModule;
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
            // DEV-V3-06: the panel editor routes by the FROZEN REGISTRATION
            // CATALOG's settings-facet projection — the official hardcoded
            // route list (network ×2 / LIT / LIR / LHT) is retired. BII stays
            // an explicit composition route (the plugin's own UI feature is
            // chrome, not a list entry); anything without a facet reaches the
            // honest no-settings editor (不伪造设置页 + BUE-SET-004 line).
            // The panel stays an edit adapter: every answer rides the host-
            // owned per-feature runtime (单事实源) and the facet's own
            // OnSettingsApplied refresh hook.
            IBueSettingsEditor bueSettingsEditor = new CatalogRoutingBueSettingsEditor(new BetterItemInteractionSettingsEditor(settingsState));
            managementPanel = new BueManagementPanelRuntime(preferencesPath, bueSettingsEditor, loadedPluginAdapter);
            // DEV-V3-06 (03 具名移交): the enable/disable command adapter —
            // the model forwards to the host's ONE seam (the state machine
            // stays with the host; entries refresh to the truth on the next
            // panel refresh).
            managementPanel.Model.FeatureToggleHandler = BueFeatureStartRuntime.SetFeatureEnabled;
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
            var component = OfficialComponent;
            var state = component == null ? FeatureState.Running : component.Lifecycle.State;
            var presentation = component == null
                ? new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1)
                : component.Lifecycle.Presentation;
            // DEV-V4-05: the state line keeps its DEV-15D component source, but
            // WHO stopped the feature is the machine's fact — the stop reason
            // and diagnostic ride from BueFeatureStartRuntime so the projection
            // can tell 已停用 from a non-user stop. No machine record → the
            // safe copy AND no stoppable seam (Q45: the toggle is drawn only
            // for entries the machine currently tracks).
            FeatureStopReason stopReason;
            string statusDiagnostic;
            var hasStoppableSeam = TryReadMachineLifecycleFacts(feature, out _, out stopReason, out statusDiagnostic);
            return new BueFeatureManagementEntry(feature, "更好的物品交互", "1.0.0", state,
                presentation, settingsState.GetSnapshot(), stopReason, statusDiagnostic, hasStoppableSeam);
        }

        // DEV-V3-06: every non-BII catalog entry is built the SAME way —
        // identity from the entry, official display names stay presentation
        // chrome (a label map, not a routing list), the settings snapshot
        // comes from the feature's facet on the host-owned runtime (null for
        // features without one — 不伪造设置页), and the state is the live
        // projection of the host machine (面板状态侧非第二事实源). The old
        // per-feature builders reading module-owned SettingsRuntimes are
        // retired with the hardcoded list.
        private BueFeatureManagementEntry ToManagementEntry(FeatureRegistrationEntry entry)
        {
            var feature = entry.Definition.Feature;
            if (feature.Value == BetterItemInteractionSettingsState.Feature.Value) return OfficialManagementEntry();
            // DEV-V4-05: the machine's State/StopReason/DiagnosticId ride with
            // the entry so the panel projection can separate 已停用 (UserDisabled)
            // from a non-user stop, surface the isolation reason when set, and
            // draw the toggle ONLY for entries the machine actually tracks
            // (F1: a mapped state on an untracked entry is not a stoppable
            // seam). TryReadMachineLifecycleFacts also keeps the DEV-V3-06
            // Running display default for tracked-record-less features.
            FeatureStopReason stopReason;
            string statusDiagnostic;
            var hasStoppableSeam = TryReadMachineLifecycleFacts(feature, out var state, out stopReason, out statusDiagnostic);
            var displayName = OfficialDisplayName(entry);
            FeaturePresentationView presentation;
            if (feature.Value == LhtRuntime.FeatureIdValue)
            {
                // DEV-V2-20: LHT's presentation is the module's own
                // Available/HeadlessOnly computation (the U3DS fact) — a
                // presentation detail, not a settings route.
                var lhtState = lhtModule == null ? FeaturePresentationState.Available : lhtModule.PresentationState;
                presentation = new FeaturePresentationView(feature, lhtState, string.Empty, 1);
            }
            else if (IsOfficialDirectPresentationFeature(feature.Value))
            {
                // V2 precedent: these official features patch the native
                // dashboard/flow directly (no ClientUi satellite exists) yet
                // their presentation is Available, not degraded.
                presentation = new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1);
            }
            else if (entry.ClientUi == null)
            {
                presentation = new FeaturePresentationView(feature, FeaturePresentationState.PresentationDegraded, "BUE-UI-SATELLITE-001", 1);
            }
            else
            {
                presentation = new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1);
            }
            return new BueFeatureManagementEntry(feature, displayName, "0.0.0", state, presentation, FacetSnapshot(entry),
                stopReason, statusDiagnostic, hasStoppableSeam);
        }

        // DEV-V4-05: ONE read of the host machine's facts for a panel entry —
        // the state, WHO stopped it (stop reason + diagnostic) and whether the
        // machine tracks the feature at all (the stoppable-lifecycle seam
        // ownership, Q45). Returns false when the start path never tracked the
        // feature; the caller then keeps the DEV-V3-06 Running display default
        // and the entry owns no stoppable seam.
        private static bool TryReadMachineLifecycleFacts(FeatureId feature, out FeatureState state,
            out FeatureStopReason stopReason, out string statusDiagnostic)
        {
            FeatureStatusView status;
            if (BueFeatureStartRuntime.TryGetStatus(feature, out status))
            {
                state = status.State;
                stopReason = status.StopReason;
                statusDiagnostic = status.DiagnosticId;
                return true;
            }
            state = FeatureState.Running;
            stopReason = FeatureStopReason.None;
            statusDiagnostic = null;
            return false;
        }

        private static bool IsOfficialDirectPresentationFeature(string featureValue)
        {
            return featureValue == LitRuntime.FeatureIdValue || featureValue == LirRuntime.FeatureIdValue
                || featureValue == NetworkModuleAdapter.NetworkFeature.Value || featureValue == NetworkModuleAdapter.V1CompatFeature.Value;
        }

        private static string OfficialDisplayName(FeatureRegistrationEntry entry)
        {
            var feature = entry.Definition.Feature;
            if (feature.Value == NetworkModuleAdapter.NetworkFeature.Value) return "BUE 网络模块";
            if (feature.Value == NetworkModuleAdapter.V1CompatFeature.Value) return "BUE V1 兼容层";
            if (feature.Value == LitRuntime.FeatureIdValue) return LitRuntime.DisplayName;
            if (feature.Value == LirRuntime.FeatureIdValue) return LirRuntime.DisplayName;
            if (feature.Value == LhtRuntime.FeatureIdValue) return LhtRuntime.DisplayName;
            return feature.Value;
        }

        private static FeatureSettingsSnapshot FacetSnapshot(FeatureRegistrationEntry entry)
        {
            if (entry.SettingDescriptors == null) return default(FeatureSettingsSnapshot);
            var runtime = BueSettingsRuntime.Registry.GetOrCreateRuntime(entry.Definition.Feature, entry.SettingDescriptors);
            return runtime == null
                ? default(FeatureSettingsSnapshot)
                : runtime.GetSnapshot(SettingRevisionScope.ClientPreference);
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
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new PlacementCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8), settingsState);
            OfficialComponent = component;
            return component;
        }

        internal BetterItemInteractionUiComponent OfficialComponent { get; private set; }

        private sealed class BueClientUiRoot : IClientUiRoot { }
    }
}
