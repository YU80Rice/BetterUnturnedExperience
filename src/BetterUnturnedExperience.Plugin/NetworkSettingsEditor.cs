using System;
using System.Collections.Generic;
using System.Threading;
using BetterUnturnedExperience.ClientUi;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Core.Settings;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-06: bridges the management panel's per-feature editor seam onto
    /// a network facet's SettingsRuntime. Apply becomes an atomic
    /// ScopedSettingChangeRequest (interlocked request id, client-preference
    /// scope) so the panel toggle persists through FileSettingsPersistence;
    /// after an accepted change the optional callback runs — production binds
    // the adapter's RefreshSwitches so the toggle takes effect immediately.
    ///
    /// DEV-V6-02E: the UI no longer names the host project, so this half of
    /// the settings routing (the catalog lookup + the Core SettingsRuntime
    /// wrap — both host-owned types) stays here and crosses to the UI as the
    /// four injected Route* delegates (ClientUiFeatureAssembly
    /// .BindHostComposition). The BII identity check rides the UI's public
    /// OfficialFeature seam instead of the internal settings-state class.
    /// </summary>
    internal sealed class SettingsRuntimeBueEditor
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

        internal FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
        {
            return string.Equals(feature.Value, runtime.Feature.Value, StringComparison.Ordinal)
                ? runtime.GetSnapshot(SettingRevisionScope.ClientPreference)
                : UnavailableSnapshot(feature);
        }

        internal IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
        {
            if (!string.Equals(feature.Value, runtime.Feature.Value, StringComparison.Ordinal)) return new SettingDescriptor[0];
            return descriptors ?? new SettingDescriptor[0];
        }

        internal SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
        {
            return ApplyBatch(feature, expectedRevision, new[] { mutation });
        }

        // DEV-V4-01: the draft's "保存配置" routes a feature's whole settings
        // draft through ONE ScopedSettingChangeRequest, so the SettingsRuntime
        // is all-or-nothing and its revision advances exactly once.
        internal SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
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
    /// DEV-V6-02E: the host-side settings routes for the UI's catalog-driven
    /// routing editor. Every delegate resolves PER CALL from the frozen
    /// registration catalog (never a stale route table — DEV-V3-06); a null
    /// descriptor answer means "no facet" on the UI side (honest no-settings
    /// editor + BUE-SET-004 line, emitted UI-side). The BII feature is routed
    /// by the UI itself; the host route only sees the catalog.
    /// </summary>
    internal static class BueClientUiSettingsRoutes
    {
        private static FeatureRegistrationEntry FindCatalogEntry(FeatureId feature)
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Catalog == null) return null;
            var entries = runtime.Catalog.Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (string.Equals(entry.Definition.Feature.Value, feature.Value, StringComparison.Ordinal))
                    return entry;
            }
            return null;
        }

        private static SettingsRuntimeBueEditor ResolveEditor(FeatureId feature)
        {
            var entry = FindCatalogEntry(feature);
            if (entry == null || entry.SettingDescriptors == null) return null;
            var settingsRuntime = BueSettingsRuntime.Registry.GetOrCreateRuntime(entry.Definition.Feature, entry.SettingDescriptors);
            return settingsRuntime == null ? null : new SettingsRuntimeBueEditor(settingsRuntime, entry.OnSettingsApplied, entry.SettingDescriptors);
        }

        /// <summary>Null = no facet (UI: honest no-settings editor). BII never
        /// reaches this route (the UI routes its own editor first).</summary>
        internal static Func<FeatureId, IReadOnlyList<SettingDescriptor>> GetDescriptorsRoute()
        {
            return feature =>
            {
                var editor = ResolveEditor(feature);
                return editor == null ? null : editor.GetDescriptors(feature);
            };
        }

        internal static Func<FeatureId, FeatureSettingsSnapshot?> GetSnapshotRoute()
        {
            return feature =>
            {
                var editor = ResolveEditor(feature);
                return editor == null ? default(FeatureSettingsSnapshot?) : editor.GetSnapshot(feature);
            };
        }

        internal static Func<FeatureId, uint, SettingMutation, SettingChangeResult> GetApplyRoute()
        {
            return (feature, expectedRevision, mutation) => ApplyBatchCore(feature, expectedRevision, new[] { mutation });
        }

        internal static Func<FeatureId, uint, IReadOnlyList<SettingMutation>, SettingChangeResult> GetApplyBatchRoute()
        {
            return ApplyBatchCore;
        }

        private static SettingChangeResult ApplyBatchCore(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
        {
            var editor = ResolveEditor(feature);
            return editor == null
                ? new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, UnavailableSnapshot(feature))
                : editor.ApplyBatch(feature, expectedRevision, mutations);
        }

        private static FeatureSettingsSnapshot UnavailableSnapshot(FeatureId feature)
        {
            return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0,
                SettingSyncState.Unavailable, SettingSnapshotSource.SafeDefault, new SettingEntryView[0]);
        }
    }
}
