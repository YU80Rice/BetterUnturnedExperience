using System;
using System.Collections.Generic;
using BepInEx.Logging;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    /// <summary>
    /// DEV-V6-02E: the host-service composition snapshot (V6-T2 硬项拆法 —
    /// the UI ring no longer names the host project; every host fact or mouth
    /// crosses here as a contract-typed delegate). One instance is snapshotted
    /// per composition root at construction (via ClientUiFeatureAssembly
    /// BindHostComposition/CreateComposition), so a later bind never re-binds
    /// a live ring. Unbound field = honestly degraded: log mouths swallow,
    /// catalog facts fall back to the official-only row, settings routes
    /// answer no-facet — the same absent-seam family as 02B/C/D. The
    /// delegate count is the composition face itself (02C 的六参数过渡 clump
    /// 在本票收口为这一处具名注入缝); no other ClientUi member names the host.
    /// </summary>
    internal sealed class ClientUiHostServices
    {
        // Host log mouths (BueRuntimeLog policy lives host-side; the moved UI
        // code routes through these exactly where it previously called the
        // host's internal logger — same lines, same levels).
        internal Action<string> LogRuntime;
        internal Action<string> LogWarn;
        internal Action<string> LogError;
        internal Action<string> LogErrorFriendly;
        internal Func<string, bool> IsCriticalNotReadyReason;

        // The plugin's own BepInEx source: the native panel's LogTrace and the
        // activation gate warnings ride the direct channel (no test recorder
        // routing) exactly as the pre-move host code did.
        internal ManualLogSource HostLog;

        // Panel enable/disable ONE seam (BueFeatureStartRuntime.SetFeatureEnabled).
        internal Func<FeatureId, bool, bool> FeatureToggleHandler;

        // DEV-V6-04: the official BII component's lifecycle facts, bridged by
        // the host from the Bii public seam (the component lives in the Bii
        // project now). Absent = the pre-move component==null fallback branch.
        internal Func<FeatureState?> OfficialComponentState;
        internal Func<FeaturePresentationView?> OfficialComponentPresentation;

        // Catalog→panel projection facts (the host machine owns all of these;
        // the UI only consumes contract-typed answers). DEV-V6-05: the display
        // name and the direct-presentation fact ride the ROW (the feature's
        // own self-report, copied into the frozen catalog by the host) — the
        // two dedicated name/predicate delegates retired with the hardcoded
        // official name map (作废票 05).
        internal Func<FeatureId, FeatureStatusView?> TryGetMachineStatus;
        internal Func<FeatureId, IReadOnlyList<SettingDescriptor>, FeatureSettingsSnapshot> GetFacetSnapshot;
        internal Func<IReadOnlyList<(FeatureId Feature, bool? ClientUiSatellite,
            IReadOnlyList<SettingDescriptor> SettingDescriptors, Func<FeaturePresentationState> PresentationOverride,
            string DisplayName, bool DirectPresentation)>> GetCatalogEntries;

        // Host settings routes (per call, catalog-driven — DEV-V3-06 semantics
        // preserved host-side; RouteGetDescriptors returning null = no facet).
        internal Func<FeatureId, FeatureSettingsSnapshot?> RouteGetSnapshot;
        internal Func<FeatureId, IReadOnlyList<SettingDescriptor>> RouteGetDescriptors;
        internal Func<FeatureId, uint, SettingMutation, SettingChangeResult> RouteApply;
        internal Func<FeatureId, uint, IReadOnlyList<SettingMutation>, SettingChangeResult> RouteApplyBatch;

        // Network takeover card facts (DEV-V2-06): the host owns the network
        // adapter's identity and takeover state; the UI renders the card and
        // submits the hand-back disable through the machine seam. Unbound =
        // the network card simply never renders (no fabricated facts).
        internal Func<FeatureId> GetNetworkFeature;
        internal Func<string> GetTakeoverStatus;
        internal Func<string> GetConfigMigrationStatus;
        internal Func<bool> IsTakeoverActive;
        internal Action RefreshNetworkSwitches;

        // The host's log-policy classifier (the verbosity routing table);
        // duplicated policy would drift, so it crosses as a delegate.
        internal Func<string, bool> IsRuntimeEvent;
    }
}
