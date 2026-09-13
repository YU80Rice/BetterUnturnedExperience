using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    // POST-P4-04：UPM 分类导航与列表配置文本路径——面板模型组。判据全部走模型投影缝：
    // 分类集合去重/首现序/空分类归「通用」、单分类只有一条（导航缺席由渲染层按 >1 判定）、
    // 分类过滤行投影、列表行=带格式提示的文本框且编辑只进草稿、保存/直写重建保留
    // 分类与提示。标签→分类/提示的采集判据在 Plugin.Tests（DevTicket04UpmTagParsingTests）。
    internal static class DevTicket04UpmCategoryListTests
    {
        private const string ItemHint = "值格式：物品ID, 物品ID, ...";

        internal static void Run()
        {
            CategoryCollectionDedupedFirstSeenWithDefaultGroup();
            SingleCategoryProducesExactlyOneEntry();
            CategoryFilterSelectsRowsInOrder();
            ListItemRowIsHintedTextEditorEnteringDraft();
            RowProjectionsPreserveCategoryAndHintThroughWrites();
            CycleRowShapeUnaffectedByCategorySeam();
            Console.WriteLine("POST-P4-04 ClientUi category/list tests: PASS");
        }

        // 分类集合：去重、首现序、空分类归「通用」；未注册 stableId 如实空集。
        private static void CategoryCollectionDedupedFirstSeenWithDefaultGroup()
        {
            var model = NewModel();
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.cats",
                    Entry("a", String("v"), category: "战斗"),
                    Entry("b", String("v")),
                    Entry("c", String("v"), category: "战斗"),
                    Entry("d", String("v"), category: "   "),
                    Entry("e", String("v"), category: "外观")),
            });

            var categories = model.GetPluginConfigCategories("com.example.cats");
            Assert(categories.Count == 3, "分类集合去重后只剩三条");
            Assert(categories[0] == "战斗" && categories[1] == "通用" && categories[2] == "外观",
                "分类按首现序排列，空/空白分类归「通用」且只占一位");
            Assert(model.GetPluginConfigCategories("missing.plugin").Count == 0, "未注册插件=空分类集（不造假导航）");
        }

        // 单分类不画多余导航的模型侧根据：整组同分类或全无分类都只产出一条。
        private static void SingleCategoryProducesExactlyOneEntry()
        {
            var model = NewModel();
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.all.one",
                    Entry("x", String("v"), category: "回收"),
                    Entry("y", String("v"), category: "回收")),
                Plugin("com.example.all.none",
                    Entry("p", String("v")),
                    Entry("q", String("v"))),
            });

            Assert(model.GetPluginConfigCategories("com.example.all.one").Count == 1, "整组同分类=单分类");
            var none = model.GetPluginConfigCategories("com.example.all.none");
            Assert(none.Count == 1 && none[0] == "通用", "全无分类=单条「通用」");
        }

        // 分类过滤：命中分类只回该组行、保持条目序；null=全量；无行分类如实空。
        private static void CategoryFilterSelectsRowsInOrder()
        {
            var model = NewModel();
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.filter",
                    Entry("fight1", String("v"), category: "战斗"),
                    Entry("fight2", String("v"), category: "战斗", controlHint: ItemHint),
                    Entry("look", String("v"), category: "外观"),
                    Entry("plain", String("v"))),
            });

            Assert(model.GetPluginConfigRows("com.example.filter").Count == 4, "无分类过滤=全量行");
            var fight = model.GetPluginConfigRows("com.example.filter", "战斗");
            Assert(fight.Count == 2 && fight[0].Key == "fight1" && fight[1].Key == "fight2", "分类过滤保条目序");
            var general = model.GetPluginConfigRows("com.example.filter", "通用");
            Assert(general.Count == 1 && general[0].Key == "plain", "空分类行经「通用」命中");
            Assert(model.GetPluginConfigRows("com.example.filter", "不存在的分类").Count == 0, "无行分类如实空集");
        }

        // 列表行=带格式提示的文本框：改值进草稿不立刻 Save；保存才写权威并更新行。
        private static void ListItemRowIsHintedTextEditorEnteringDraft()
        {
            var editor = new AcceptedPluginEditor();
            var model = NewModel(editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.list",
                    Entry("Items", String("34"), category: "战斗", controlHint: ItemHint)),
            });

            var row = configRow(model.GetPluginConfigRows("com.example.list"), "Items");
            Assert(row.ControlKind == PanelSettingControlKind.TextEditor, "列表行是文本框（非 Cycle、非选择器）");
            Assert(row.ControlHint == ItemHint, "行携带逐字格式提示");
            Assert(row.Category == "战斗", "行携带分类");

            model.OpenDetail("com.example.list");
            Assert(model.DraftEditPluginConfig("com.example.list", "Items", "34, 45"), "列表值编辑进草稿受理");
            Assert(editor.SetCalls == 0, "改列表值不立刻写权威源（同一草稿纪律）");
            Assert(model.IsDirty && model.IsPluginConfigDirty("Items"), "草稿改列表值即脏");
            row = configRow(model.GetPluginConfigRows("com.example.list"), "Items");
            Assert(row.Effective.Text == "34, 45" && row.IsDirty, "行显示草稿生效值并标未保存");

            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success && editor.SetCalls == 1, "保存才写权威（逐条一次）");
            row = configRow(model.GetPluginConfigRows("com.example.list"), "Items");
            Assert(row.Effective.Text == "34, 45" && !row.IsDirty && !model.IsDirty, "保存后权威更新、不再脏");
        }

        // 分类/提示/描述/重启旗标穿过草稿保存与 legacy 直写两条重建路径不漂移。
        private static void RowProjectionsPreserveCategoryAndHintThroughWrites()
        {
            var editor = new AcceptedPluginEditor();
            var model = NewModel(editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.preserve",
                    Entry("Items", String("34"), requiresRestart: true, description: "白名单物品。",
                        category: "战斗", controlHint: ItemHint)),
            });
            model.OpenDetail("com.example.preserve");
            Assert(model.DraftEditPluginConfig("com.example.preserve", "Items", "55"), "编辑进草稿");
            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success && report.RequiresRestart, "RequiresRestart 项保存成功点亮重启语义");
            var saved = configRow(model.GetPluginConfigRows("com.example.preserve", "战斗"), "Items");
            Assert(saved.Category == "战斗" && saved.ControlHint == ItemHint && saved.Description == "白名单物品。",
                "保存重建保留分类/提示/描述（过滤仍命中）");
            Assert(saved.RequiresRestart && saved.Effective.Text == "55", "行级重启旗标与新值保留");

            var legacy = NewModel(new AcceptedPluginEditor());
            legacy.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.preserve2",
                    Entry("Mode", String("OFF"), description: "模式。",
                        allowedChoices: new[] { "OFF", "1" }, category: "战斗", controlHint: "左键：下一档")),
            });
            var write = legacy.TryEditPluginConfig("com.example.preserve2", "Mode", "1");
            Assert(write.Accepted, "legacy 直写缝保持可用（回归）");
            var entry = legacy.GetEntries()[0].PluginConfig[0];
            Assert(entry.Category == "战斗" && entry.ControlHint == "左键：下一档" && entry.Description == "模式。",
                "直写重建保留分类/提示/描述");
            Assert(entry.AllowedChoices.Count == 2 && entry.AllowedChoices[1] == "1", "直写重建保留档位（Cycle 不回退）");
        }

        // 分类缝不动 Cycle 形状（DEV-V4-08 已交付项不重做）：有档位仍 Cycle，过滤照常命中。
        private static void CycleRowShapeUnaffectedByCategorySeam()
        {
            var editor = new AcceptedPluginEditor();
            var model = NewModel(editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.cycle.keep",
                    Entry("Mode", String("OFF"), allowedChoices: new[] { "OFF", "1" }, category: "战斗")),
            });

            var row = configRow(model.GetPluginConfigRows("com.example.cycle.keep", "战斗"), "Mode");
            Assert(row.ControlKind == PanelSettingControlKind.Cycle, "档位行仍 Cycle（分类缝不改变形状）");
            Assert(row.ControlHint.Length == 0, "未带提示的 Cycle 行不造假提示");
            model.OpenDetail("com.example.cycle.keep");
            Assert(model.DraftCyclePluginConfig("com.example.cycle.keep", "Mode", 1)
                && model.EffectivePluginConfigValue("Mode").Text == "1", "Cycle 草稿缝回归保持");
            Assert(editor.SetCalls == 0, "Cycle 仍只进草稿");
        }

        // ── helpers ──

        private static ManagementPanelModel NewModel(IPluginConfigEditor configEditor = null)
        {
            return new ManagementPanelModel(new MemoryStore(), new NoSchemaEditor(), configEditor ?? new AcceptedPluginEditor());
        }

        private static PluginConfigEntryView Entry(string key, PluginConfigValue value, bool requiresRestart = false,
            bool canEdit = true, string description = null, IReadOnlyList<string> allowedChoices = null,
            string category = null, string controlHint = null)
        {
            return new PluginConfigEntryView(key, key, value.Kind, value, requiresRestart, canEdit,
                null, null, 4096, description, allowedChoices, category, controlHint);
        }

        private static PluginConfigValue String(string value)
        {
            return PluginConfigValue.StringValue(value);
        }

        private static LoadedPluginDescriptor Plugin(string guid, params PluginConfigEntryView[] entries)
        {
            return new LoadedPluginDescriptor(guid, guid, "1.0.0", entries);
        }

        private static PanelConfigRowView configRow(IReadOnlyList<PanelConfigRowView> rows, string key)
        {
            for (var index = 0; index < rows.Count; index++)
                if (string.Equals(rows[index].Key, key, StringComparison.Ordinal)) return rows[index];
            throw new InvalidOperationException("config row not found: " + key);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class MemoryStore : IManagementPanelPreferencesStore
        {
            private ManagementPanelPreferences preferences = ManagementPanelPreferences.Empty;
            public ManagementPanelPreferences Load() { return preferences; }
            public void Save(ManagementPanelPreferences value) { preferences = value; }
        }

        // 本组全部走外部插件分支，BUE 设置源零触碰。
        private sealed class NoSchemaEditor : IBueSettingsEditor
        {
            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
            {
                return new FeatureSettingsSnapshot(feature, 0, SettingRevisionScope.ClientPreference, 0,
                    SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new SettingEntryView[0]);
            }

            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingUnknown, expectedRevision, GetSnapshot(feature));
            }

            public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingUnknown, expectedRevision, GetSnapshot(feature));
            }

            public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
            {
                return new SettingDescriptor[0];
            }
        }

        private sealed class AcceptedPluginEditor : IPluginConfigEditor
        {
            internal int SetCalls;
            public PluginConfigEditResult TrySet(string pluginGuid, string key, PluginConfigValue value)
            {
                SetCalls++;
                return new PluginConfigEditResult(true, false, PluginConfigEditRejection.None);
            }
        }
    }
}
