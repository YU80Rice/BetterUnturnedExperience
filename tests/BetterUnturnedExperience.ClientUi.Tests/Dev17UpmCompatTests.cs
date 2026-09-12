using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    // DEV-V3-10 (UPM compatibility tags): the panel renders the
    // UnturnedPluginManager tag surface (Cycle / ItemList / BlueprintList /
    // CreatureList / Category) instead of collapsing every discrete value
    // into a read-only row. These tests cover the pure-C# half: the cycle
    // stepper, the category collector, and the model's metadata-preserving
    // edit writeback. Tag extraction from ConfigEntryBase lives in the
    // catalog adapter and is exercised by the game-side wiring.
    internal static class Dev17UpmCompatTests
    {
        internal static void Run()
        {
            CycleStepperWalksOptionsWithWraparound();
            CycleStepperHandlesUnknownCurrentAndEmptyOptions();
            CategoryCollectorDedupeAndDefaultGroup();
            EntryViewDefaultsStayFieldBackwardCompatible();
            ModelEditPreservesUpmMetadata();
            CycleEditRoutesThroughTypedParser();
        }

        private static void CycleStepperWalksOptionsWithWraparound()
        {
            var options = Options("OFF", "1", "2", "3");
            Assert(PluginConfigSupport.NextCycleOption(options, "OFF", 1) == "1", "cycle forward leaves first option");
            Assert(PluginConfigSupport.NextCycleOption(options, "3", 1) == "OFF", "cycle forward wraps past last option");
            Assert(PluginConfigSupport.NextCycleOption(options, "1", -1) == "OFF", "cycle backward leaves second option");
            Assert(PluginConfigSupport.NextCycleOption(options, "OFF", -1) == "3", "cycle backward wraps past first option");
        }

        private static void CycleStepperHandlesUnknownCurrentAndEmptyOptions()
        {
            var options = Options("A", "B");
            Assert(PluginConfigSupport.NextCycleOption(options, "not-an-option", 1) == "A", "unknown current lands on first option forward");
            Assert(PluginConfigSupport.NextCycleOption(options, "not-an-option", -1) == "B", "unknown current lands on last option backward");
            Assert(PluginConfigSupport.NextCycleOption(null, "A", 1) == null, "null options yield null");
            Assert(PluginConfigSupport.NextCycleOption(new string[0], "A", 1) == null, "empty options yield null");
            Assert(PluginConfigSupport.NextCycleOption(Options("ONLY"), "ONLY", 1) == "ONLY", "single option toggles in place");
        }

        private static void CategoryCollectorDedupeAndDefaultGroup()
        {
            var entries = new[]
            {
                Config("a", category: "自动回收"),
                Config("b", category: "自动合成"),
                Config("c", category: "自动回收"),
                Config("d", category: null),
                Config("e", category: string.Empty)
            };
            var categories = PluginConfigSupport.CollectCategories(entries);
            Assert(categories.Count == 3, "categories dedupe preserving first-seen order");
            Assert(categories[0] == "自动回收" && categories[1] == "自动合成", "category order follows first appearance");
            Assert(categories[2] == PluginConfigSupport.DefaultCategory, "empty categories collapse into the default group");
            Assert(PluginConfigSupport.CollectCategories(null).Count == 0, "null entry list yields no categories");
            Assert(PluginConfigSupport.CollectCategories(new PluginConfigEntryView[0]).Count == 0, "empty entry list yields no categories");
        }

        private static void EntryViewDefaultsStayFieldBackwardCompatible()
        {
            var entry = new PluginConfigEntryView("Key", "Key", PluginConfigValueKind.String, PluginConfigValue.StringValue("v"), false, true);
            Assert(entry.Control == PluginConfigControlKind.Field, "control defaults to the plain field editor");
            Assert(entry.Description.Length == 0 && entry.Category.Length == 0 && entry.ControlHint.Length == 0, "metadata defaults are empty strings");
            Assert(entry.CycleOptions == null, "cycle options default to null");
            Assert(entry.CanEdit, "plain field entry stays editable");
        }

        private static void ModelEditPreservesUpmMetadata()
        {
            var editor = new FakePluginConfigEditor();
            var model = new ManagementPanelModel(new MemoryPreferencesStore(), new FakeBueSettingsEditor(), editor);
            var options = Options("OFF", "1", "2");
            var descriptor = new LoadedPluginDescriptor("com.example.upm", "UPM Plugin", "1.0.0", new[]
            {
                new PluginConfigEntryView("Mode", "Mode", PluginConfigValueKind.String, PluginConfigValue.StringValue("OFF"), false, true,
                    null, null, 4096, "工作模式", "自动回收", PluginConfigControlKind.Cycle, options, "左键：下一档；右键：上一档"),
            });
            model.Refresh(new BueFeatureManagementEntry[0], new[] { descriptor });

            var result = model.TryEditPluginConfig("com.example.upm", "Mode", "1");
            Assert(result.Accepted, "cycle string edit is accepted");
            var entry = model.GetEntries()[0].PluginConfig[0];
            Assert(entry.Value.Text == "1", "edited value lands in the snapshot");
            Assert(entry.Description == "工作模式" && entry.Category == "自动回收", "edit writeback preserves description and category");
            Assert(entry.Control == PluginConfigControlKind.Cycle && entry.ControlHint.Length > 0, "edit writeback preserves control kind and hint");
            Assert(entry.CycleOptions.Count == 3 && entry.CycleOptions[2] == "2", "edit writeback preserves cycle options");
        }

        private static void CycleEditRoutesThroughTypedParser()
        {
            var editor = new FakePluginConfigEditor();
            var model = new ManagementPanelModel(new MemoryPreferencesStore(), new FakeBueSettingsEditor(), editor);
            var descriptor = new LoadedPluginDescriptor("com.example.numeric", "Numeric Plugin", "1.0.0", new[]
            {
                new PluginConfigEntryView("Level", "Level", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(0), false, true,
                    null, null, 0, "档位", "通用", PluginConfigControlKind.Cycle, Options("0", "1", "2"), string.Empty),
                new PluginConfigEntryView("Choice", "Choice", PluginConfigValueKind.String, PluginConfigValue.StringValue("A"), false, true,
                    null, null, 4096, "受约束文本", "通用", PluginConfigControlKind.Field, null, string.Empty),
            });
            model.Refresh(new BueFeatureManagementEntry[0], new[] { descriptor });

            var accepted = model.TryEditPluginConfig("com.example.numeric", "Level", "2");
            Assert(accepted.Accepted && editor.LastValue.Integer == 2L, "numeric cycle option parses through the typed path");
            var rejected = model.TryEditPluginConfig("com.example.numeric", "Level", "not-a-number");
            Assert(!rejected.Accepted && rejected.Reason == PluginConfigEditRejection.InvalidValue, "non-numeric option is rejected for integer entries");
            accepted = model.TryEditPluginConfig("com.example.numeric", "Choice", "B");
            Assert(accepted.Accepted && editor.LastValue.Text == "B", "AcceptableValueList<string> entries stay editable as text");
        }

        private static string[] Options(params string[] values) { return values; }

        private static PluginConfigEntryView Config(string key, string category)
        {
            return new PluginConfigEntryView(key, key, PluginConfigValueKind.String, PluginConfigValue.StringValue("v"), false, true,
                null, null, 4096, null, category, PluginConfigControlKind.Field, null, null);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class MemoryPreferencesStore : IManagementPanelPreferencesStore
        {
            private ManagementPanelPreferences preferences = ManagementPanelPreferences.Empty;
            public ManagementPanelPreferences Load() { return preferences; }
            public void Save(ManagementPanelPreferences value) { preferences = value; }
        }

        private sealed class FakeBueSettingsEditor : IBueSettingsEditor
        {
            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature) { return default(FeatureSettingsSnapshot); }
            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                var entry = new SettingEntryView(mutation.SettingId, SettingAuthority.ClientLocal,
                    new SettingValueOption(true, mutation.Value), false, default(SettingPolicyView), mutation.Value, true, true);
                var snapshot = new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, expectedRevision + 1,
                    SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new[] { entry });
                return new SettingChangeResult(true, FrameworkErrorCode.None, expectedRevision + 1, snapshot);
            }
        }

        private sealed class FakePluginConfigEditor : IPluginConfigEditor
        {
            internal PluginConfigValue LastValue { get; private set; }
            public bool TrySet(string pluginGuid, string key, PluginConfigValue value) { LastValue = value; return true; }
        }
    }
}
