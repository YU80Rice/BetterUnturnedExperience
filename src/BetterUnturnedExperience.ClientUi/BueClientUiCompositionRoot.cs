using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    /// <summary>
    /// BUE-owned composition boundary for the client UI. It performs the
    /// environment gate before invoking any component factory.
    /// DEV-V6-02E: moved out of the host project — the ring no longer names
    /// the host project namespace (文本级扫描同防火墙). Host facts and mouths ride the
    /// snapshotted ClientUiHostServices (absent = honestly degraded); the
    /// client-branch activation orchestration the plugin entry used to run
    /// (panel, inventory adapters, diagnostic-sink tagging, wiring logs) is
    /// now InitializeAndActivate, driven by the UI's one public assembly type.
    /// DEV-V6-04: the BII drag-in subsystem (the component + the two inventory
    /// adapters) moved to the Bii project and arms through the module's Start
    /// (T3 Q2) — the composition root no longer holds adapter fields or
    /// creates them; the ring here owns the panel, the settings single-source,
    /// the reconciler and the composition state machine. The registry
    /// projection retired with its sole consumer (the BII component).
    /// </summary>
    internal sealed class BueClientUiCompositionRoot
    {
        private readonly ClientUiHostServices services;
        private readonly ClientUiCompositionRoot composition;
        private readonly BetterItemInteractionSettingsState settingsState;
        private readonly BueManagementPanelRuntime managementPanel;
        private readonly LoadedPluginCatalogAdapter loadedPluginAdapter;
        private readonly bool clientUiAvailable;
        private BueNativeManagementPanel nativePanel;

        // DEV-V6-04: the placement evaluator factory left with the component
        // (it is a Bii construction fact now — the host passes it through the
        // Bii composition bind, not here).
        internal BueClientUiCompositionRoot(ClientUiHostServices hostServices = null)
        {
            services = hostServices ?? new ClientUiHostServices();
            settingsState = new BetterItemInteractionSettingsState();
            composition = new ClientUiCompositionRoot();
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
            // OnSettingsApplied refresh hook. DEV-V6-02E: the host-side half
            // of the route (catalog lookup + SettingsRuntime wrap) crosses as
            // injected delegates — the routing editor consumes them per call.
            IBueSettingsEditor bueSettingsEditor = new CatalogRoutingBueSettingsEditor(new BetterItemInteractionSettingsEditor(settingsState), services);
            managementPanel = new BueManagementPanelRuntime(preferencesPath, bueSettingsEditor, loadedPluginAdapter);
            // DEV-V3-06 (03 具名移交): the enable/disable command adapter —
            // the model forwards to the host's ONE seam (the state machine
            // stays with the host; entries refresh to the truth on the next
            // panel refresh). Unbound seam stays null — the model already
            // treats a null handler as no-stoppable-seam, the same shape as
            // an unbound host ring.
            managementPanel.Model.FeatureToggleHandler = services.FeatureToggleHandler;
        }

        internal bool IsReady { get { return composition.State == ClientUiCompositionState.Ready; } }
        internal ClientUiCompositionState State { get { return composition.State; } }
        internal BueManagementPanelRuntime ManagementPanel { get { return managementPanel; } }

        internal bool Initialize(bool isBatchMode, bool headless, bool nativeUiAvailable)
        {
            var environment = new ClientUiEnvironment(nativeUiAvailable && clientUiAvailable, isBatchMode, headless);
            var initialized = composition.Initialize(environment);
            if (initialized)
            {
                managementPanel.Initialize();
                RefreshManagementPanel();
            }
            return initialized;
        }

        /// <summary>
        /// DEV-V6-02E: the client-branch activation the plugin entry used to
        /// run inline, moved behind the composition gate. DEV-V6-04: the BII
        /// chain (inventory adapters, projection sink, wiring logs) left for
        /// the module's Start (same gate semantics, completion timing — the
        /// official start path drives it); what remains is the panel ring and
        /// the ready announcement.
        /// </summary>
        internal bool InitializeAndActivate(bool isBatchMode, bool headless, bool nativeUiAvailable)
        {
            if (!Initialize(isBatchMode, headless, nativeUiAvailable)) return false;
            nativePanel = new BueNativeManagementPanel(managementPanel, services.HostLog, null, RefreshManagementPanel, services);
            nativePanel.Initialize();
            services.LogRuntime?.Invoke("BUE client UI composition ready featureId=io.github.yu80rice.bue.better-item-interaction diagnosticId=BUE-CLIENTUI-002");
            return true;
        }

        // DEV-V2-24 F1: level-aware routing (feature lines choose Debug/Error
        // themselves) and the generic BUE-CLIENTUI-001 tag is appended ONLY
        // when the line does not already carry its own diagnosticId=, so
        // feature-owned ids are never double-tagged. DEV-V6-02E: the tagging
        // lambda moved host→UI with the composition; the mouths stay injected.
        // DEV-V6-04: the last production emitter (the BII component) left for
        // the Bii project (injected mouths); the seam and this named binding
        // stay for the UI ring and the test anchors (DevV602E tagging probes).
        internal static void BindDiagnosticSink(ClientUiHostServices boundServices)
        {
            ClientUiCompositionRoot.DiagnosticSink = (line, level) =>
            {
                var tagged = line.Contains("diagnosticId=");
                var text = "[BUE-CLIENTUI] " + line + (tagged ? "" : " diagnosticId=BUE-CLIENTUI-001");
                if (level == ClientUiCompositionRoot.ClientUiDiagnosticLevel.Error)
                    boundServices.LogError?.Invoke(text);
                else
                    boundServices.LogRuntime?.Invoke(text);
            };
        }

        private void EmitHostWarning(string line)
        {
            var log = services.HostLog;
            if (log != null) log.LogWarning(line);
        }

        internal void RefreshManagementPanel()
        {
            managementPanel.Refresh(BuildCatalogEntries(), loadedPluginAdapter.CaptureLoadedPlugins());
        }

        // DEV-V3-06: every non-BII catalog entry is built the SAME way —
        // identity from the entry, official display names stay presentation
        // chrome (a label map, not a routing list), the settings snapshot
        // comes from the feature's facet on the host-owned runtime (null for
        // features without one — 不伪造设置页), and the state is the live
        // projection of the host machine (面板状态侧非第二事实源). The old
        // per-feature builders reading module-owned SettingsRuntimes are
        // retired with the hardcoded list.
        // DEV-V6-02E: the catalog rows arrive as contract-typed tuples from
        // the injected reader (the host projects ITS catalog; the UI only
        // orders and projects). Absent reader = the official-only fallback,
        // the same shape as the pre-move runtime==null branch.
        // DEV-V6-04: the official entry's state/presentation facts come from
        // the injected component projections (the component lives in the Bii
        // project now; the host bridges its projection into the services bag).
        // Absent projection = the pre-move component==null fallback semantics.
        private List<BueFeatureManagementEntry> BuildCatalogEntries()
        {
            var result = new List<BueFeatureManagementEntry>();
            var reader = services.GetCatalogEntries;
            if (reader == null)
            {
                result.Add(OfficialManagementEntry(null));
                return result;
            }
            var rows = reader();
            if (rows == null)
            {
                result.Add(OfficialManagementEntry(null));
                return result;
            }
            foreach (var row in rows)
            {
                if (row.Feature.Value == BetterItemInteractionSettingsState.Feature.Value) result.Add(OfficialManagementEntry(row));
                else result.Add(ToManagementEntry(row));
            }
            return result;
        }

        // DEV-V6-05 (V6-T5 Q1): the official entry takes its panel copy from
        // the SAME self-reported row every other entry uses (the BII
        // registration implements the optional metadata facet) — the
        // hardcoded official Chinese literal retired with the door map. A
        // null row (absent catalog reader = the official-only fallback) leaves
        // the display name to the honest FeatureId fallback.
        private BueFeatureManagementEntry OfficialManagementEntry(
            (FeatureId Feature, bool? ClientUiSatellite, IReadOnlyList<SettingDescriptor> SettingDescriptors,
                Func<FeaturePresentationState> PresentationOverride, string DisplayName, bool DirectPresentation)? row)
        {
            var feature = BetterItemInteractionSettingsState.Feature;
            var displayName = row.HasValue ? RowDisplayName(row.Value.DisplayName, feature) : feature.Value;
            var componentState = services.OfficialComponentState != null ? services.OfficialComponentState() : null;
            var hasComponentFacts = componentState.HasValue;
            var state = hasComponentFacts ? componentState.Value : FeatureState.Running;
            FeaturePresentationView presentation;
            if (hasComponentFacts && services.OfficialComponentPresentation != null)
            {
                var componentPresentation = services.OfficialComponentPresentation();
                presentation = componentPresentation.HasValue
                    ? componentPresentation.Value
                    : new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1);
            }
            else
            {
                presentation = new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1);
            }
            // DEV-V4-05: the state line keeps its DEV-15D component source, but
            // WHO stopped the feature is the machine's fact — the stop reason
            // and diagnostic ride from BueFeatureStartRuntime so the projection
            // can tell 已停用 from a non-user stop. No machine record → the
            // safe copy AND no stoppable seam (Q45: the toggle is drawn only
            // for entries the machine currently tracks).
            FeatureStopReason stopReason;
            string statusDiagnostic;
            var hasStoppableSeam = TryReadMachineLifecycleFacts(feature, out _, out stopReason, out statusDiagnostic);
            return new BueFeatureManagementEntry(feature, displayName, "1.0.0", state,
                presentation, settingsState.GetSnapshot(), stopReason, statusDiagnostic, hasStoppableSeam);
        }

        private BueFeatureManagementEntry ToManagementEntry(
            (FeatureId Feature, bool? ClientUiSatellite, IReadOnlyList<SettingDescriptor> SettingDescriptors,
                Func<FeaturePresentationState> PresentationOverride, string DisplayName, bool DirectPresentation) row)
        {
            var feature = row.Feature;
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
            // DEV-V6-05: the display name and the direct-presentation fact are
            // the FEATURE'S OWN self-report, carried on the frozen catalog row
            // (the hardcoded official name map retired — 作废票 05).
            var displayName = RowDisplayName(row.DisplayName, feature);
            FeaturePresentationView presentation;
            if (row.PresentationOverride != null)
            {
                // DEV-V2-20: LHT's presentation is the module's own
                // Available/HeadlessOnly computation (the U3DS fact) — a
                // presentation detail, not a settings route.
                // DEV-V6-02D/02E: the fact rides the host-projected catalog row
                // (the host knows which entry is the horde tracker; the UI never
                // names the feature project) — unbound seam degrades to Available
                // (the pre-migration null-module branch semantics).
                presentation = new FeaturePresentationView(feature, row.PresentationOverride(), string.Empty, 1);
            }
            else if (row.DirectPresentation)
            {
                // DEV-V6-05 (V6-T5 Q1): the feature self-reported that it
                // paints into the game directly (native dashboard/flow, no
                // ClientUi satellite) — Available, not degraded (the retired
                // retired official direct-presentation map's job, now the
                // feature's own declaration).
                presentation = new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1);
            }
            else if (row.ClientUiSatellite == null)
            {
                presentation = new FeaturePresentationView(feature, FeaturePresentationState.PresentationDegraded, "BUE-UI-SATELLITE-001", 1);
            }
            else
            {
                presentation = new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1);
            }
            return new BueFeatureManagementEntry(feature, displayName, "0.0.0", state, presentation, FacetSnapshot(row),
                stopReason, statusDiagnostic, hasStoppableSeam);
        }

        // DEV-V6-05: ONE display-name rule for official and ecosystem rows —
        // the feature's self-report when it made one, else the raw FeatureId
        // (未报显示名的功能面板仍显示 FeatureId).
        private static string RowDisplayName(string reported, FeatureId feature)
        {
            return string.IsNullOrWhiteSpace(reported) ? feature.Value : reported;
        }

        // DEV-V4-05: ONE read of the host machine's facts for a panel entry —
        // the state, WHO stopped it (stop reason + diagnostic) and whether the
        // machine tracks the feature at all (the stoppable-lifecycle seam
        // ownership, Q45). Returns false when the start path never tracked the
        // feature; the caller then keeps the DEV-V3-06 Running display default
        // and the entry owns no stoppable seam. (DEV-V6-02E: the machine read
        // crosses as an injected Func — the FeatureStatusView is a contract
        // type, the host machine stays host-side.)
        private bool TryReadMachineLifecycleFacts(FeatureId feature, out FeatureState state,
            out FeatureStopReason stopReason, out string statusDiagnostic)
        {
            FeatureStatusView? status = services.TryGetMachineStatus != null ? services.TryGetMachineStatus(feature) : null;
            if (status.HasValue)
            {
                state = status.Value.State;
                stopReason = status.Value.StopReason;
                statusDiagnostic = status.Value.DiagnosticId;
                return true;
            }
            state = FeatureState.Running;
            stopReason = FeatureStopReason.None;
            statusDiagnostic = null;
            return false;
        }

        // DEV-V6-05 (V6-T5 Q1): the official display-name map and the
        // direct-presentation predicate retired — both facts ride the row
        // (the row's own name field and DirectPresentation above). 作废票 05.
        private FeatureSettingsSnapshot FacetSnapshot(
            (FeatureId Feature, bool? ClientUiSatellite, IReadOnlyList<SettingDescriptor> SettingDescriptors,
                Func<FeaturePresentationState> PresentationOverride, string DisplayName, bool DirectPresentation) row)
        {
            var snapshot = services.GetFacetSnapshot;
            return snapshot != null ? snapshot(row.Feature, row.SettingDescriptors) : default(FeatureSettingsSnapshot);
        }

        internal void Destroy()
        {
            managementPanel.Destroy();
            composition.Destroy();
        }

        /// <summary>原生面板销毁（宿主 OnDestroy 的显式第一步；组合销毁仍单列以保持销毁序）。</summary>
        internal void DestroyNativePanel()
        {
            if (nativePanel == null) return;
            nativePanel.Destroy();
            nativePanel = null;
        }

        internal bool DispatchPanelTick(BueNativeManagementPanel.TickSource source)
        {
            var panel = nativePanel;
            if (panel == null) return false;
            return panel.Dispatch(source);
        }

        internal bool PanelTickIsolated
        {
            get { return nativePanel != null && nativePanel.TickIsolated; }
        }

        internal void EnterSafeMode(Action<string> diagnostic)
        {
            composition.EnterSafeMode(diagnostic);
        }

        /// <summary>DEV-V6-04: the BII interface seams the host bridges into the
        /// Bii composition bind — the listen-host reconcile (the pre-move
        /// open-callback's UI half) and the settings snapshot reader (the
        /// settings single-source stays here; the Bii component pulls).</summary>
        internal void OnBiiSurfaceOpened()
        {
            // F-B1c: a dashboard open on the listen host is the join-time
            // repair moment — the page range comes from THIS project's bound
            // fields (single source is still the tidy feature's public seam —
            // BindTidyableRange; the UI never names the feature).
            ListenHostProjectionReconciler.OnDashboardSurfaceOpened(
                ListenHostProjectionReconciler.TidyablePageMin, ListenHostProjectionReconciler.TidyablePageMax);
        }

        internal FeatureSettingsSnapshot TakeSettingsSnapshot()
        {
            return settingsState.GetSnapshot();
        }
    }
}
