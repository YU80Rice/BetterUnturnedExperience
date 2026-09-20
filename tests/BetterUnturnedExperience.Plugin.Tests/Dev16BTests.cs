using System;
using System.Collections.Generic;
using System.IO;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.ClientUi.Internal; // 面板/设置态类型仍住界面工程（IVT 通道）
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Plugin.Tests
{
    internal static class Dev16BTests
    {
        internal static void Run()
        {
            FavoriteOrderingAndPersistence();
            LoadedPluginDiscoveryAndStableIdentity();
            ExternalManagerDetectionIsNonDestructive();
            BueSettingsFacetEditingUsesRevision();
            PrimitivePluginConfigEditingIsBounded();
            UiTreeRebuildReattachesOnce();
            FilePreferencesRoundTrip();
            MenuEntriesAreRegisteredForBothSurfaces();
        }

        private static void FavoriteOrderingAndPersistence()
        {
            var store = new MemoryPreferencesStore();
            var model = new ManagementPanelModel(store, new FakeBueSettingsEditor());
            model.Refresh(new[]
            {
                Feature("feature.z", "Zulu"),
                Feature("feature.a", "Alpha")
            }, new LoadedPluginDescriptor[0]);

            Assert(model.ToggleFavorite("feature.z"), "favorite z succeeds");
            Assert(model.ToggleFavorite("feature.a"), "favorite a succeeds");
            var favorites = model.GetEntries();
            Assert(favorites[0].StableId == "feature.z" && favorites[1].StableId == "feature.a", "favorites preserve insertion order");

            Assert(model.ToggleFavorite("feature.z"), "unfavorite z succeeds");
            Assert(model.ToggleFavorite("feature.z"), "re-favorite z succeeds");
            favorites = model.GetEntries();
            Assert(favorites[0].StableId == "feature.a" && favorites[1].StableId == "feature.z", "re-favorite moves to end");

            model.SetSortOrder(ManagementSortOrder.NameDescending);
            model.Refresh(new[]
            {
                Feature("feature.m", "Mike"),
                Feature("feature.a", "Alpha"),
                Feature("feature.z", "Zulu")
            }, new LoadedPluginDescriptor[0]);
            var ordered = model.GetEntries();
            Assert(ordered[0].StableId == "feature.a" && ordered[1].StableId == "feature.z" && ordered[2].StableId == "feature.m", "favorite then descending names");
            Assert(store.SaveCount >= 4, "preferences persisted after mutations");

            var restored = new ManagementPanelModel(store, new FakeBueSettingsEditor());
            restored.Refresh(new[]
            {
                Feature("feature.m", "Mike"),
                Feature("feature.a", "Alpha"),
                Feature("feature.z", "Zulu")
            }, new LoadedPluginDescriptor[0]);
            ordered = restored.GetEntries();
            Assert(ordered[0].StableId == "feature.a" && ordered[1].StableId == "feature.z", "sort and favorites survive reload");
        }

        private static void LoadedPluginDiscoveryAndStableIdentity()
        {
            var model = new ManagementPanelModel(new MemoryPreferencesStore(), new FakeBueSettingsEditor());
            model.Refresh(new[] { Feature("io.github.bue.feature", "BUE Feature") }, new[]
            {
                new LoadedPluginDescriptor("com.example.plugin", "Example Plugin", "1.2.3", new[]
                {
                    new PluginConfigEntryView("Enabled", "Enabled", PluginConfigValueKind.Boolean, PluginConfigValue.BooleanValue(true), false, true),
                })
            });
            var entries = model.GetEntries();
            Assert(entries.Count == 2, "only supplied loaded plugins and BUE features are shown");
            Assert(entries[0].Kind == ManagementEntryKind.BueFeature, "BUE feature entry is typed");
            Assert(entries[1].Kind == ManagementEntryKind.ExternalPlugin, "ordinary plugin entry is typed");
            Assert(entries[1].StableId == "com.example.plugin", "ordinary plugin uses GUID identity");
            Assert(entries[0].StableId == "io.github.bue.feature", "BUE uses FeatureId identity");
        }

        private static void ExternalManagerDetectionIsNonDestructive()
        {
            var model = new ManagementPanelModel(new MemoryPreferencesStore(), new FakeBueSettingsEditor());
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                new LoadedPluginDescriptor("com.trae.pluginmanager", "Other Manager", "1.0.0", new PluginConfigEntryView[0])
            });
            model.SetExternalManagerDetected(true);
            Assert(model.ExternalManagerDetected && model.CompatibilityNotice.Length > 0, "external manager is surfaced as a compatibility notice");
            Assert(model.GetEntries().Count == 1, "compatibility detection does not remove or mutate external plugin");
        }

        private static void BueSettingsFacetEditingUsesRevision()
        {
            var editor = new FakeBueSettingsEditor();
            var model = new ManagementPanelModel(new MemoryPreferencesStore(), editor);
            var feature = Feature("io.github.bue.better-item-interaction", "Better Item Interaction");
            model.Refresh(new[] { feature }, new LoadedPluginDescriptor[0]);
            var result = model.TryEditBueSetting(feature.Feature, "Enabled", PluginConfigValue.BooleanValue(false));
            Assert(result.Accepted, "BUE setting edit accepted by facet editor");
            Assert(editor.LastExpectedRevision == 7 && editor.LastMutation.Value.Boolean == false, "BUE edit uses current snapshot revision and typed mutation");
            Assert(model.GetEntries()[0].BueSettings[0].EffectiveValue.Boolean == false, "BUE row refreshes from returned snapshot");
        }

        private static void PrimitivePluginConfigEditingIsBounded()
        {
            var editor = new FakePluginConfigEditor();
            var model = new ManagementPanelModel(new MemoryPreferencesStore(), new FakeBueSettingsEditor(), editor);
            var descriptor = new LoadedPluginDescriptor("com.example.config", "Config Plugin", "1.0.0", new[]
            {
                new PluginConfigEntryView("Bool", "Bool", PluginConfigValueKind.Boolean, PluginConfigValue.BooleanValue(false), false, true),
                new PluginConfigEntryView("Count", "Count", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(2), true, true, 0, 100),
                new PluginConfigEntryView("Text", "Text", PluginConfigValueKind.String, PluginConfigValue.StringValue("old"), false, true, null, null, 16),
                new PluginConfigEntryView("Complex", "Complex", PluginConfigValueKind.Unsupported, PluginConfigValue.UnsupportedValue(), false, false),
            });
            model.Refresh(new BueFeatureManagementEntry[0], new[] { descriptor });

            var accepted = model.TryEditPluginConfig("com.example.config", "Bool", "true");
            Assert(accepted.Accepted && accepted.RequiresRestart == false && editor.LastValue.Boolean, "bool config is editable");
            accepted = model.TryEditPluginConfig("com.example.config", "Count", "42");
            Assert(accepted.Accepted && accepted.RequiresRestart && editor.LastValue.Integer == 42, "numeric config is editable and restart flag preserved");
            accepted = model.TryEditPluginConfig("com.example.config", "Text", "new value");
            Assert(accepted.Accepted && editor.LastValue.Text == "new value", "string config is editable");
            var rejected = model.TryEditPluginConfig("com.example.config", "Complex", "anything");
            Assert(!rejected.Accepted && rejected.Reason == PluginConfigEditRejection.UnsupportedType, "unsupported config remains read-only");
            rejected = model.TryEditPluginConfig("com.example.config", "Count", "not-a-number");
            Assert(!rejected.Accepted && rejected.Reason == PluginConfigEditRejection.InvalidValue, "invalid numeric input is rejected");
            rejected = model.TryEditPluginConfig("com.example.config", "Count", "101");
            Assert(!rejected.Accepted && rejected.Reason == PluginConfigEditRejection.InvalidValue, "numeric range is enforced");
            accepted = model.TryEditPluginConfig("com.example.config", "Count", "42");
            Assert(accepted.Accepted && editor.LastValue.Integer == 42L, "64-bit integer parser accepts representable values");
            rejected = model.TryEditPluginConfig("com.example.config", "Text", "12345678901234567");
            Assert(!rejected.Accepted && rejected.Reason == PluginConfigEditRejection.InvalidValue, "string length is enforced");
        }

        private static void UiTreeRebuildReattachesOnce()
        {
            var lifecycle = new ManagementPanelLifecycle();
            var first = new RecordingMount();
            var second = new RecordingMount();
            lifecycle.Attach(first);
            lifecycle.Attach(first);
            lifecycle.Attach(second);
            lifecycle.Destroy();
            Assert(first.MountCount == 1 && first.UnmountCount == 1, "old UI tree is unmounted once");
            Assert(second.MountCount == 1 && second.UnmountCount == 1, "new UI tree is mounted and destroyed once");
            Assert(lifecycle.State == ManagementPanelLifecycleState.Destroyed, "lifecycle is terminal after destroy");
        }

        private static void FilePreferencesRoundTrip()
        {
            var path = Path.Combine(Path.GetTempPath(), "bue-dev16b-" + Guid.NewGuid().ToString("N"), "panel.preferences");
            try
            {
                var store = new FileManagementPanelPreferencesStore(path);
                store.Save(new ManagementPanelPreferences(ManagementSortOrder.NameDescending, new[] { "feature.one", "plugin.two" }));
                var loaded = store.Load();
                Assert(loaded.SortOrder == ManagementSortOrder.NameDescending, "file store persists sort order");
                Assert(loaded.FavoriteIds.Count == 2 && loaded.FavoriteIds[1] == "plugin.two", "file store persists ordered favorites");
            }
            finally
            {
                var directory = Path.GetDirectoryName(path);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static void MenuEntriesAreRegisteredForBothSurfaces()
        {
            var bridge = new ManagementPanelMenuBridge(new ManagementPanelLifecycle());
            bridge.AttachMainMenu();
            bridge.AttachPauseMenu();
            Assert(bridge.HasMainMenuEntry && bridge.HasPauseMenuEntry, "management entry is registered for main and pause menus");
            bridge.Destroy();
            Assert(!bridge.HasMainMenuEntry && !bridge.HasPauseMenuEntry, "menu entries clear on destroy");
        }

        private static BueFeatureManagementEntry Feature(string id, string name)
        {
            var settings = new FeatureSettingsSnapshot(new FeatureId(id), 1, SettingRevisionScope.ClientPreference, 7,
                SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new[]
                {
                    new SettingEntryView("Enabled", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(true)), false,
                        default(SettingPolicyView), SettingValue.Toggle(true), true, true)
                });
            return new BueFeatureManagementEntry(new FeatureId(id), name, "1.0.0", FeatureState.Running,
                new FeaturePresentationView(new FeatureId(id), FeaturePresentationState.Available, string.Empty, 1), settings);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class MemoryPreferencesStore : IManagementPanelPreferencesStore
        {
            private ManagementPanelPreferences preferences = ManagementPanelPreferences.Empty;
            internal int SaveCount { get; private set; }
            public ManagementPanelPreferences Load() { return preferences; }
            public void Save(ManagementPanelPreferences value) { preferences = value; SaveCount++; }
        }

        private sealed class FakeBueSettingsEditor : IBueSettingsEditor
        {
            private FeatureSettingsSnapshot snapshot;
            internal uint LastExpectedRevision { get; private set; }
            internal SettingMutation LastMutation { get; private set; }
            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature) { return snapshot; }
            public System.Collections.Generic.IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature) { return new SettingDescriptor[0]; }
            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                return ApplyBatch(feature, expectedRevision, new[] { mutation });
            }
            public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, System.Collections.Generic.IReadOnlyList<SettingMutation> mutations)
            {
                LastExpectedRevision = expectedRevision;
                var list = mutations ?? new SettingMutation[0];
                LastMutation = list.Count > 0 ? list[0] : default(SettingMutation);
                var entries = new System.Collections.Generic.List<SettingEntryView>();
                foreach (var mutation in list)
                    entries.Add(new SettingEntryView(mutation.SettingId, SettingAuthority.ClientLocal,
                        new SettingValueOption(true, mutation.Value), false, default(SettingPolicyView), mutation.Value, true, true));
                snapshot = new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, expectedRevision + 1,
                    SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, entries);
                return new SettingChangeResult(true, FrameworkErrorCode.None, expectedRevision + 1, snapshot);
            }
        }

        private sealed class FakePluginConfigEditor : IPluginConfigEditor
        {
            internal PluginConfigValue LastValue { get; private set; }
            // DEV-V4-08：结构化写入结果——接受并如实申报无重启项。
            public PluginConfigEditResult TrySet(string pluginGuid, string key, PluginConfigValue value)
            {
                LastValue = value;
                return new PluginConfigEditResult(true, false, PluginConfigEditRejection.None);
            }
        }

        private sealed class RecordingMount : IManagementPanelMount
        {
            internal int MountCount { get; private set; }
            internal int UnmountCount { get; private set; }
            public void Mount() { MountCount++; }
            public void Unmount() { UnmountCount++; }
        }
    }
}
