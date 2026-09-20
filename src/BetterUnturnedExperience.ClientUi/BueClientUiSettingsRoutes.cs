using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    /// <summary>
    /// DEV-V3-06: catalog-based panel routing — the 官方硬编码清单退役. Routes
    /// resolve PER CALL from the frozen registration catalog's settings-facet
    /// projection (never a hardcoded feature list, never a stale route table),
    /// answer through the feature's OWN host-owned runtime (single source of
    /// truth) and fire the facet's OnSettingsApplied hook after an accepted
    /// edit. The composition's own BII editor is an explicit route (plugin
    /// chrome, not a list entry); everything else reaches the honest
    /// no-settings editor — a feature without a facet gets NO fabricated
    /// settings page and its rejection leaves a structured line (BUE-SET-004).
    ///
    /// DEV-V6-02E: the host-side half (catalog lookup + SettingsRuntime wrap)
    /// stayed in the host project and crosses as the injected Route*
    /// delegates on ClientUiHostServices — RouteGetDescriptors returning null
    /// means "no facet" (the pre-move Resolve()==null), a non-null empty list
    /// means a facet without schema. The BUE-SET-004 rejection line stays
    /// UI-side log policy and rides the injected LogRuntime mouth.
    /// </summary>
    internal sealed class CatalogRoutingBueSettingsEditor : IBueSettingsEditor
    {
        private readonly IBueSettingsEditor compositionEditor;
        private readonly ClientUiHostServices hostServices;

        internal CatalogRoutingBueSettingsEditor(IBueSettingsEditor compositionEditor, ClientUiHostServices hostServices)
        {
            this.compositionEditor = compositionEditor ?? throw new ArgumentNullException(nameof(compositionEditor));
            this.hostServices = hostServices ?? new ClientUiHostServices();
        }

        public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
        {
            return Resolve(feature).GetSnapshot(feature);
        }

        public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
        {
            return ApplyBatch(feature, expectedRevision, new[] { mutation });
        }

        public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
        {
            return Resolve(feature).ApplyBatch(feature, expectedRevision, mutations);
        }

        // DEV-V4-02: schema lookup answers from the SAME catalog projection the
        // route uses (per call, never a stale copy). No facet → no schema →
        // empty list; the composition (BII) editor owns its honest answer.
        public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
        {
            return Resolve(feature).GetDescriptors(feature);
        }

        /// <summary>Per-call route resolution: the BII identity routes to the
        /// composition's own editor; any other feature reaches the injected
        /// host route (null = no facet = honest no-settings editor).</summary>
        private IBueSettingsEditor Resolve(FeatureId feature)
        {
            if (string.Equals(feature.Value, BetterItemInteractionSettingsState.Feature.Value, StringComparison.Ordinal)) return compositionEditor;
            return new HostRouteBueSettingsEditor(feature, hostServices);
        }

        private static FeatureSettingsSnapshot UnavailableSnapshot(FeatureId feature)
        {
            return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0,
                SettingSyncState.Unavailable, SettingSnapshotSource.SafeDefault, new SettingEntryView[0]);
        }

        /// <summary>The per-feature view of the injected host route: null
        /// descriptors = no facet (Resolve surfaces null), otherwise the route
        /// operations answer with the same contract types the pre-move
        /// SettingsRuntimeBueEditor produced.</summary>
        private sealed class HostRouteBueSettingsEditor : IBueSettingsEditor
        {
            private readonly FeatureId feature;
            private readonly ClientUiHostServices hostServices;

            internal HostRouteBueSettingsEditor(FeatureId feature, ClientUiHostServices hostServices)
            {
                this.feature = feature;
                this.hostServices = hostServices;
            }

            private bool HasFacet
            {
                get
                {
                    var descriptors = hostServices.RouteGetDescriptors;
                    return descriptors != null && descriptors(feature) != null;
                }
            }

            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
            {
                var snapshot = hostServices.RouteGetSnapshot;
                if (!HasFacet || snapshot == null) return UnavailableSnapshot(feature);
                var resolved = snapshot(feature);
                return resolved.HasValue ? resolved.Value : UnavailableSnapshot(feature);
            }

            public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
            {
                var descriptors = hostServices.RouteGetDescriptors;
                if (!HasFacet || descriptors == null) return new SettingDescriptor[0];
                return descriptors(feature) ?? new SettingDescriptor[0];
            }

            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                return ApplyBatch(feature, expectedRevision, new[] { mutation });
            }

            public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
            {
                var applyBatch = hostServices.RouteApplyBatch;
                if (!HasFacet || applyBatch == null)
                {
                    // BUE-SET-004: a feature without a settings facet gets NO
                    // fabricated settings page — the rejection leaves the same
                    // structured line the pre-move no-facet branch emitted.
                    var logRuntime = hostServices.LogRuntime;
                    if (logRuntime != null)
                        logRuntime("[BUE-SET] event=settings-editor result=rejected feature=" + feature.Value
                            + " stage=panel reason=no-settings-facet diagnosticId=BUE-SET-004");
                    return Rejected(feature);
                }
                return applyBatch(feature, expectedRevision, mutations);
            }

            private SettingChangeResult Rejected(FeatureId feature)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, UnavailableSnapshot(feature));
            }
        }
    }

    // DEV-V3-06: RoutingBueSettingsEditor (the DEV-V2-06 hardcoded-route
    // list) is DELETED with the retirement of the official feature list —
    // its one unit test moved to the catalog-driven routing group in
    // AssertBueV3SettingsWiringAndPanelRouting; the dead type staying
    // compiled would leave the retired pattern looking supported.
}
