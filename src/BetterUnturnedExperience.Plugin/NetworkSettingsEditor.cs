using System;
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

        internal SettingsRuntimeBueEditor(SettingsRuntime runtime) : this(runtime, null) { }

        internal SettingsRuntimeBueEditor(SettingsRuntime runtime, Action onApplied)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.onApplied = onApplied;
        }

        public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
        {
            return string.Equals(feature.Value, runtime.Feature.Value, StringComparison.Ordinal)
                ? runtime.GetSnapshot(SettingRevisionScope.ClientPreference)
                : UnavailableSnapshot(feature);
        }

        public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
        {
            if (!string.Equals(feature.Value, runtime.Feature.Value, StringComparison.Ordinal)) return Rejected(feature);
            var result = runtime.Submit(new ScopedSettingChangeRequest(
                unchecked((ulong)Interlocked.Increment(ref nextRequestId)),
                SettingRevisionScope.ClientPreference,
                expectedRevision,
                new[] { mutation }));
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
    /// DEV-V2-06: routes the panel's editor seam by feature id — the network
    /// facets answer through their own SettingsRuntime editors, everything
    /// else falls back to the composition's default editor (BII first). First
    /// matching route wins; the fallback never sees a routed feature.
    /// </summary>
    internal sealed class RoutingBueSettingsEditor : IBueSettingsEditor
    {
        private readonly IBueSettingsEditor fallback;
        private readonly (FeatureId feature, IBueSettingsEditor editor)[] routes;

        internal RoutingBueSettingsEditor(IBueSettingsEditor fallback, params (FeatureId feature, IBueSettingsEditor editor)[] routes)
        {
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            this.routes = routes ?? new (FeatureId, IBueSettingsEditor)[0];
        }

        public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
        {
            foreach (var route in routes)
            {
                if (string.Equals(route.feature.Value, feature.Value, StringComparison.Ordinal)) return route.editor.GetSnapshot(feature);
            }
            return fallback.GetSnapshot(feature);
        }

        public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
        {
            foreach (var route in routes)
            {
                if (string.Equals(route.feature.Value, feature.Value, StringComparison.Ordinal)) return route.editor.Apply(feature, expectedRevision, mutation);
            }
            return fallback.Apply(feature, expectedRevision, mutation);
        }
    }
}
