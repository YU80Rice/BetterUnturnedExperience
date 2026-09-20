using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lir;
using BetterUnturnedExperience.Lit;
using BetterUnturnedExperience.Lht;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V6-02E: the host-side catalog→panel projection — the delegates the
    /// entry binds into ClientUiFeatureAssembly.BindHostComposition (and the
    /// harness replays to drive the real flow). The host owns the machine,
    /// the catalog and the feature seams' consts; the UI consumes
    /// contract-typed answers only. The horde tracker's presentation fact
    /// rides its catalog row (the host knows which entry is LHT — the UI
    /// never names the feature project).
    /// DEV-V6-05 (V6-T5 Q1): the hardcoded official display-name map and the
    /// direct-presentation predicate RETIRED — both facts now ride each row
    /// from the feature's OWN self-report (the optional
    /// IFeaturePresentationRegistration facet, copied into the frozen catalog
    /// at admission). This file no longer names any official feature's copy.
    /// </summary>
    internal static class BueClientUiHostProjection
    {
        internal static FeatureStatusView? TryGetMachineStatus(FeatureId feature)
        {
            return BueFeatureStartRuntime.TryGetStatus(feature, out var status) ? status : default(FeatureStatusView?);
        }

        internal static FeatureSettingsSnapshot GetFacetSnapshot(FeatureId feature, IReadOnlyList<SettingDescriptor> descriptors)
        {
            if (descriptors == null) return default(FeatureSettingsSnapshot);
            var runtime = BueSettingsRuntime.Registry.GetOrCreateRuntime(feature, descriptors);
            return runtime == null
                ? default(FeatureSettingsSnapshot)
                : runtime.GetSnapshot(SettingRevisionScope.ClientPreference);
        }

        internal static IReadOnlyList<(FeatureId Feature, bool? ClientUiSatellite,
            IReadOnlyList<SettingDescriptor> SettingDescriptors, Func<FeaturePresentationState> PresentationOverride,
            string DisplayName, bool DirectPresentation)> GetCatalogRows()
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Catalog == null) return null;
            var rows = new List<(FeatureId Feature, bool? ClientUiSatellite,
                IReadOnlyList<SettingDescriptor> SettingDescriptors, Func<FeaturePresentationState> PresentationOverride,
                string DisplayName, bool DirectPresentation)>();
            var entries = runtime.Catalog.Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var feature = entry.Definition.Feature;
                rows.Add((feature,
                    // The catalog entry holds the satellite registration object;
                    // the panel row only consumes its presence (degraded when absent).
                    entry.ClientUi == null ? default(bool?) : true,
                    entry.SettingDescriptors,
                    feature.Value == LhtFeatureAssembly.FeatureId ? new Func<FeaturePresentationState>(() => LhtFeatureAssembly.WiredPresentationState) : null,
                    // DEV-V6-05: the feature's own self-report (null/empty =
                    // it reported nothing; the panel then draws the FeatureId).
                    entry.DisplayName,
                    entry.DirectPresentation));
            }
            return rows;
        }
    }
}
