using System;
using System.Collections.Generic;
using System.Threading;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Settings;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-06: bridges the management panel's per-feature editor seam onto
    /// a network facet's SettingsRuntime. Apply becomes an atomic
    /// ScopedSettingChangeRequest (interlocked request id, client-preference
    /// scope) so the panel toggle persists through FileSettingsPersistence;
    /// after an accepted change the optional callback runs — production binds
    /// the adapter's RefreshSwitches so the toggle takes effect immediately.
    /// </summary>
    internal sealed class SettingsRuntimeBueEditor : IBueSettingsEditor
    {
        private static long nextRequestId;
        private readonly SettingsRuntime runtime;
        private readonly Action onApplied;
        // DEV-V4-02: the row projection's schema join input — the SAME frozen
        // descriptor list the catalog registered (never a re-derived copy), so
        // 面板行形状与宿主校验口径同源. Null = no schema (projection falls back).
        private readonly IReadOnlyList<SettingDescriptor> descriptors;

        internal SettingsRuntimeBueEditor(SettingsRuntime runtime) : this(runtime, null, null) { }

        internal SettingsRuntimeBueEditor(SettingsRuntime runtime, Action onApplied) : this(runtime, onApplied, null) { }

        internal SettingsRuntimeBueEditor(SettingsRuntime runtime, Action onApplied, IReadOnlyList<SettingDescriptor> descriptors)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.onApplied = onApplied;
            this.descriptors = descriptors;
        }

        public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
        {
            return string.Equals(feature.Value, runtime.Feature.Value, StringComparison.Ordinal)
                ? runtime.GetSnapshot(SettingRevisionScope.ClientPreference)
                : UnavailableSnapshot(feature);
        }

        public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
        {
            if (!string.Equals(feature.Value, runtime.Feature.Value, StringComparison.Ordinal)) return new SettingDescriptor[0];
            return descriptors ?? new SettingDescriptor[0];
        }

        public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
        {
            return ApplyBatch(feature, expectedRevision, new[] { mutation });
        }

        // DEV-V4-01: the draft's "保存配置" routes a feature's whole settings
        // draft through ONE ScopedSettingChangeRequest, so the SettingsRuntime
        // is all-or-nothing and its revision advances exactly once.
        public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
        {
            if (!string.Equals(feature.Value, runtime.Feature.Value, StringComparison.Ordinal)) return Rejected(feature);
            var result = runtime.Submit(new ScopedSettingChangeRequest(
                unchecked((ulong)Interlocked.Increment(ref nextRequestId)),
                SettingRevisionScope.ClientPreference,
                expectedRevision,
                mutations ?? new SettingMutation[0]));
            if (result.Accepted && onApplied != null) onApplied();
            return result;
        }

        private static FeatureSettingsSnapshot UnavailableSnapshot(FeatureId feature)
        {
            return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0,
                SettingSyncState.Unavailable, SettingSnapshotSource.SafeDefault, new SettingEntryView[0]);
        }

        private SettingChangeResult Rejected(FeatureId feature)
        {
            return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, UnavailableSnapshot(feature));
        }
    }

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
    /// </summary>
    internal sealed class CatalogRoutingBueSettingsEditor : IBueSettingsEditor
    {
        private readonly IBueSettingsEditor compositionEditor;

        internal CatalogRoutingBueSettingsEditor(IBueSettingsEditor compositionEditor)
        {
            this.compositionEditor = compositionEditor ?? throw new ArgumentNullException(nameof(compositionEditor));
        }

        public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
        {
            var editor = Resolve(feature);
            return editor == null ? UnavailableSnapshot(feature) : editor.GetSnapshot(feature);
        }

        public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
        {
            return ApplyBatch(feature, expectedRevision, new[] { mutation });
        }

        public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
        {
            var editor = Resolve(feature);
            if (editor == null)
            {
                BueRuntimeLog.Runtime("[BUE-SET] event=settings-editor result=rejected feature=" + feature.Value
                    + " stage=panel reason=no-settings-facet diagnosticId=BUE-SET-004");
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, UnavailableSnapshot(feature));
            }
            return editor.ApplyBatch(feature, expectedRevision, mutations);
        }

        // DEV-V4-02: schema lookup answers from the SAME catalog projection the
        // route uses (per call, never a stale copy). No facet → no schema →
        // empty list; the composition (BII) editor owns its honest answer.
        public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
        {
            if (string.Equals(feature.Value, BetterItemInteractionSettingsState.Feature.Value, StringComparison.Ordinal)) return compositionEditor.GetDescriptors(feature);
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Catalog == null) return new SettingDescriptor[0];
            var entries = runtime.Catalog.Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (string.Equals(entry.Definition.Feature.Value, feature.Value, StringComparison.Ordinal))
                    return entry.SettingDescriptors ?? new SettingDescriptor[0];
            }
            return new SettingDescriptor[0];
        }

        private IBueSettingsEditor Resolve(FeatureId feature)
        {
            if (string.Equals(feature.Value, BetterItemInteractionSettingsState.Feature.Value, StringComparison.Ordinal)) return compositionEditor;
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Catalog == null) return null;
            var entries = runtime.Catalog.Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!string.Equals(entry.Definition.Feature.Value, feature.Value, StringComparison.Ordinal)) continue;
                if (entry.SettingDescriptors == null) return null;
                var settingsRuntime = BueSettingsRuntime.Registry.GetOrCreateRuntime(entry.Definition.Feature, entry.SettingDescriptors);
                return settingsRuntime == null ? null : new SettingsRuntimeBueEditor(settingsRuntime, entry.OnSettingsApplied, entry.SettingDescriptors);
            }
            return null;
        }

        private static FeatureSettingsSnapshot UnavailableSnapshot(FeatureId feature)
        {
            return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0,
                SettingSyncState.Unavailable, SettingSnapshotSource.SafeDefault, new SettingEntryView[0]);
        }
    }

    // DEV-V3-06: RoutingBueSettingsEditor (the DEV-V2-06 hardcoded-route
    // list) is DELETED with the retirement of the official feature list —
    // its one unit test moved to the catalog-driven routing group in
    // AssertBueV3SettingsWiringAndPanelRouting; the dead type staying
    // compiled would leave the retired pattern looking supported.
}
