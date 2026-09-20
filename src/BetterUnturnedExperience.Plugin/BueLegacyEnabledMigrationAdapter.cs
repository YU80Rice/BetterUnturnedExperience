using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Core.Settings;
using BetterUnturnedExperience.Lht;
using BetterUnturnedExperience.Lir;
using BetterUnturnedExperience.Lit;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V4-04: the official legacy enabled migration adapter (spec「官方
    /// legacy enabled 迁移」). It composes the EXPLICIT six-alias registry —
    /// LIT/LIR/LHT/Network/v1compat 旧 `*.enabled` plus BII `Enabled`; BII
    /// AutoRotate, the NoOp probe toggle, undeclared aliases and ecosystem
    /// features are NOT registered (no field-name scanning, ever) — and runs
    /// the Core engine against the real composition:
    ///   - the legacy value is read straight from the feature's old settings
    ///     document (the facet is retired in the same change, so the runtime
    ///     no longer exposes the key; the document is the upgrade input) —
    ///     uniform for all six aliases, BII included;
    ///   - the interpreter is the machine's own target-submit seam
    ///     (BueFeatureStartRuntime.SetFeatureEnabled) — its DEV-V4-03
    ///     semantics decide whether the disable actually happens, and its
    ///     success path durably records the UserDisabled intent fact;
    ///   - the legacy key is retired only after that success is confirmed.
    /// RunProduction rides the runtime completion chain right after the
    /// catalog start (every environment — the legacy files exist host-side
    /// too), then refreshes the network adapter so a migrated-off network or
    /// v1compat intent disarms the takeover in the same boot.
    /// </summary>
    internal static class BueLegacyEnabledMigrationAdapter
    {
        private const string LegacyInventoryTidy = "inventorytidy.enabled";
        private const string LegacyInPlaceReload = "inplacereload.enabled";
        private const string LegacyHordeTracker = "hordetracker.enabled";
        private const string LegacyNetwork = "network.enabled";
        private const string LegacyV1Compat = "v1compat.enabled";
        private const string LegacyBiiEnabled = "Enabled";

        /// <summary>The explicit six-alias registry. Only these migrate; the
        /// count and ids are frozen by the ticket (六项官方 alias). All six
        /// read their legacy value from the feature's OWN legacy settings
        /// document — including BII's `Enabled` (its composition snapshot is
        /// in-memory only, so the durable old-world fact is the document) —
        /// never from a field-name scan.</summary>
        internal static IReadOnlyList<LegacyEnabledAlias> ComposeAliases(string settingsRoot)
        {
            return ComposeAliases(new FileSettingsPersistence(settingsRoot));
        }

        /// <summary>The alias composition over an EXPLICIT persistence (the
        /// overload above is the production convenience; host tests inject a
        /// FileSettingsPersistence with its fault seam to drive the real
        /// retire-write-failure path).</summary>
        internal static IReadOnlyList<LegacyEnabledAlias> ComposeAliases(ISettingsPersistence persistence)
        {
            return new[]
            {
                DocAlias(new FeatureId(LitFeatureAssembly.FeatureId), LegacyInventoryTidy, persistence),
                DocAlias(new FeatureId(LirFeatureAssembly.FeatureId), LegacyInPlaceReload, persistence),
                DocAlias(new FeatureId(LhtFeatureAssembly.FeatureId), LegacyHordeTracker, persistence),
                DocAlias(NetworkModuleAdapter.NetworkFeature, LegacyNetwork, persistence),
                DocAlias(NetworkModuleAdapter.V1CompatFeature, LegacyV1Compat, persistence),
                DocAlias(ClientUiFeatureAssembly.OfficialFeature, LegacyBiiEnabled, persistence)
            };
        }

        /// <summary>Production entry: the completion chain calls this once per
        /// boot after StartCatalog. Refreshes the network adapter afterwards
        /// so a migrated-off network or v1compat intent disarms the takeover
        /// in the same boot (the adapter init already consulted the store,
        /// but the record may have been written by THIS migration).</summary>
        internal static void RunProduction()
        {
            Run(BueSettingsRuntime.ProductionSettingsRoot, line => BueRuntimeLog.Runtime("[BUE-V4MIG] " + line));
            try { NetworkModuleFeatureRegistration.WiredAdapter?.RefreshSwitches(); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-V4MIG] event=legacy-enabled-migration result=refresh-failed errorType=" + error.GetType().Name + " diagnosticId=BUE-LIFE-MIGRATE");
            }
        }

        /// <summary>The engine run against the real machine seam and the
        /// root-keyed intent store. Host tests drive this directly; the
        /// optional alias persistence lets a test inject the retire-write
        /// failure path (production always composes a fresh one).</summary>
        internal static void Run(string settingsRoot, Action<string> sink, ISettingsPersistence aliasPersistence = null)
        {
            LegacyEnabledMigration.Run(
                ComposeAliases(aliasPersistence ?? new FileSettingsPersistence(settingsRoot)),
                BueFeatureIntentRuntime.StoreFor(settingsRoot),
                feature => BueFeatureStartRuntime.SetFeatureEnabled(feature, false),
                sink);
        }

        /// <summary>One document-backed alias: the reader answers the legacy
        /// toggle's persisted value (null = no document / retired key / wrong
        /// shape); the retire rewrites the document WITHOUT the legacy key,
        /// preserving every other entry and the revision — and self-guards on
        /// an already-retired document.</summary>
        private static LegacyEnabledAlias DocAlias(FeatureId feature, string settingId, ISettingsPersistence persistence)
        {
            return new LegacyEnabledAlias(
                feature,
                settingId,
                1,
                () =>
                {
                    var loaded = persistence.Load(feature, SettingRevisionScope.ClientPreference, 1, null);
                    if (!loaded.IsValid) return null;
                    SettingValue value;
                    if (!loaded.Values.TryGetValue(settingId, out value) || value.Kind != SettingKind.Toggle) return null;
                    return value.Boolean;
                },
                () =>
                {
                    var loaded = persistence.Load(feature, SettingRevisionScope.ClientPreference, 1, null);
                    if (!loaded.IsValid || !loaded.Values.ContainsKey(settingId)) return;
                    var values = new Dictionary<string, SettingValue>(StringComparer.Ordinal);
                    foreach (var pair in loaded.Values)
                    {
                        if (!string.Equals(pair.Key, settingId, StringComparison.Ordinal)) values[pair.Key] = pair.Value;
                    }
                    string diagnostic;
                    persistence.TryCommit(feature, SettingRevisionScope.ClientPreference, 1, loaded.Revision, values, out diagnostic);
                });
        }
    }
}
