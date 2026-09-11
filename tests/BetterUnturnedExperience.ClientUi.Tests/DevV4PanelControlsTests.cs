using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    // DEV-V4-02：描述行、基础控件与循环切换（spec「描述行与循环切换（V4-T3 → DEV-V4-02）」+
    // V4-T3 裁决 Q35–Q43）。只经面板模型投影缝断言外显行为（行形状、档位、脏/草稿、
    // 截断与占位规则），不断言 Glazier 控件树（Testing Decisions「只测外显行为」）。
    internal static class DevV4PanelControlsTests
    {
        internal static void Run()
        {
            ChoiceRowProjectsCycleShape();
            CycleWrapsAtBothEnds();
            CycleBackToOriginalIsNotDirty();
            SaveSubmitsChoiceMutationToAuthority();
            ChoiceWithoutLevelsIsReadOnly();
            SingleLevelCycleStaysEditableButNeverChanges();
            DescriptionIsTruncatedAtOneHundredTwenty();
            EmptyDescriptionDoesNotOccupyRow();
            DisplayNameFallsBackToSettingIdWithoutDescriptor();
            PolicyLevelsProjectToCycle();
            ServerAuthorityAndReadOnlyRowsDoNotEnterDraft();
            ExternalConfigRowProjectsCycleSeam();
            ToggleAndTextRowsKeepShapeWithNames();
        }

        // Q37/Q38/Q43：Choice+非空档位 → Cycle 行；显示名/描述来自描述符（键当字面文本）；
        // 改档位只进草稿，不碰权威源；通用文本编辑缝对 Choice 关闭（档位只能循环切换）。
        private static void ChoiceRowProjectsCycleShape()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.cycle");
            editor.Seed(feature, ChoiceEntry("mode", "同类"));
            editor.SeedDescriptors(feature, Descriptor(feature, "mode", "整理模式",
                "同类：把相同物品聚在一起；空间：优先保留大块空位；大件：优先放置大件。", SettingKind.Choice, SettingValue.Choice("同类"), "同类", "空间", "大件"));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var row = settingRow(model, feature.Value, "mode");
            Assert(row.ControlKind == PanelSettingControlKind.Cycle, "Choice+档位 → 循环切换控件");
            Assert(row.DisplayName == "整理模式", "显示名=DisplayNameKey 字面文本（不再是 SettingId）");
            Assert(row.AllowedValues.Count == 3 && row.AllowedValues[1] == "空间", "档位字面量投影");
            Assert(row.Kind == SettingKind.Choice, "Kind 投影");

            Assert(!model.DraftEditBueSetting("mode", PluginConfigValue.StringValue("大件")), "Choice 不走文本编辑缝");
            Assert(model.DraftCycleBueSetting("mode", 1), "左键下一档受理");
            Assert(editor.BatchCalls == 0, "改档位不立刻写权威源（改的是草稿）");
            Assert(model.IsDirty && model.IsBueSettingDirty("mode"), "档位改动即脏、行标未保存");
            Assert(settingRow(model, feature.Value, "mode").EffectiveValue.Text == "空间", "行显示草稿生效值");
        }

        // Q41：最后档左键回第一档，第一档右键回最后档；当前值不在档位表（脏数据）→
        // 左键落第一档、右键落最后档，切换永远落在合法档位上。
        private static void CycleWrapsAtBothEnds()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.wrap");
            editor.Seed(feature, ChoiceEntry("dir", "降序"));
            editor.SeedDescriptors(feature, Descriptor(feature, "dir", "整理方向", "降序：大件优先；升序：小件优先。",
                SettingKind.Choice, SettingValue.Choice("降序"), "降序", "升序"));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            Assert(model.DraftCycleBueSetting("dir", -1), "首档右键受理");
            Assert(settingRow(model, feature.Value, "dir").EffectiveValue.Text == "升序", "首档右键回最后档");
            Assert(model.DraftCycleBueSetting("dir", 1), "末档左键受理");
            Assert(settingRow(model, feature.Value, "dir").EffectiveValue.Text == "降序", "末档左键循环回第一档");
        }

        private static void CycleBackToOriginalIsNotDirty()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.flip");
            editor.Seed(feature, ChoiceEntry("mode", "同类"));
            editor.SeedDescriptors(feature, Descriptor(feature, "mode", "整理模式", "档位说明。",
                SettingKind.Choice, SettingValue.Choice("同类"), "同类", "空间", "大件"));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            model.DraftCycleBueSetting("mode", 1);
            Assert(model.IsDirty, "切换后脏");
            model.DraftCycleBueSetting("mode", -1);
            Assert(!model.IsDirty, "拨回原值不再脏（Q41：最终值=权威值则不脏）");
            Assert(!settingRow(model, feature.Value, "mode").IsDirty, "行不再标未保存");
        }

        // 档位改动经「保存配置」一次原子提交，mutation 必须是 Choice 形状（不是 Text），
        // 权威快照如实推进（夹具 Choice 穿过控件缝——本票官方先行消费锚，spec 允许 LIT 延至 06）。
        private static void SaveSubmitsChoiceMutationToAuthority()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.save");
            editor.Seed(feature, ChoiceEntry("mode", "同类"));
            editor.SeedDescriptors(feature, Descriptor(feature, "mode", "整理模式", "档位说明。",
                SettingKind.Choice, SettingValue.Choice("同类"), "同类", "空间", "大件"));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);
            model.DraftCycleBueSetting("mode", 1);

            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success && report.PrimaryMessage == "配置已保存。", "保存成功文案");
            Assert(editor.BatchCalls == 1, "档位草稿一次原子提交");
            Assert(editor.LastBatchMutations.Count == 1
                && editor.LastBatchMutations[0].Value.Kind == SettingKind.Choice
                && editor.LastBatchMutations[0].Value.Text == "空间", "提交的 mutation 为 Choice 形状（非 Text 降级）");
            var row = settingRow(model, feature.Value, "mode");
            Assert(!model.IsDirty && !row.IsDirty, "保存后不再脏");
            Assert(model.GetSettingRows(feature.Value).Count == 1 && row.EffectiveValue.Text == "空间", "权威快照如实反映新档位");
        }

        // Q38/Q43：无非空档位的 Choice → 只读行；不降级文本框、不画假控件、不进草稿。
        private static void ChoiceWithoutLevelsIsReadOnly()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.nolevels");
            editor.Seed(feature, ChoiceEntry("orphan", "未知值"));
            editor.SeedDescriptors(feature, Descriptor(feature, "orphan", "孤立选项", "无档位的选项。",
                SettingKind.Choice, SettingValue.Choice("未知值")));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var row = settingRow(model, feature.Value, "orphan");
            Assert(row.ControlKind == PanelSettingControlKind.ReadOnly, "无档位 Choice 只读");
            Assert(row.ControlKind != PanelSettingControlKind.TextEditor, "不降级文本框");
            Assert(row.AllowedValues.Count == 0, "无档位");
            Assert(!model.DraftCycleBueSetting("orphan", 1), "只读行不进 Cycle 草稿");
            Assert(!model.DraftEditBueSetting("orphan", PluginConfigValue.StringValue("随便")), "只读行不进编辑草稿");
            Assert(!model.IsDirty && !row.IsDirty, "只读不进草稿、不标未保存");
        }

        // Q41：单档可编（Cycle 形状），左右键都不变值、最终值=权威值则不脏。
        private static void SingleLevelCycleStaysEditableButNeverChanges()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.single");
            editor.Seed(feature, ChoiceEntry("only", "唯一"));
            editor.SeedDescriptors(feature, Descriptor(feature, "only", "单选项", "只有一个档位。",
                SettingKind.Choice, SettingValue.Choice("唯一"), "唯一"));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            Assert(settingRow(model, feature.Value, "only").ControlKind == PanelSettingControlKind.Cycle, "单档仍走 Cycle 控件（可编）");
            Assert(model.DraftCycleBueSetting("only", 1), "单档左键受理");
            Assert(settingRow(model, feature.Value, "only").EffectiveValue.Text == "唯一", "左键不变档");
            Assert(model.DraftCycleBueSetting("only", -1), "单档右键受理");
            Assert(settingRow(model, feature.Value, "only").EffectiveValue.Text == "唯一", "右键不变档");
            Assert(!model.IsDirty, "最终值=权威值则不脏");
        }

        // Q37：截断 120（对齐 UPM：前 120 字 + 省略号）；≤120 原样。
        private static void DescriptionIsTruncatedAtOneHundredTwenty()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.trunc");
            editor.Seed(feature, ToggleEntry("notify", true), ToggleEntry("brief", false));
            var longDescription = new string('字', 130);
            editor.SeedDescriptors(feature,
                Descriptor(feature, "notify", "通知", longDescription, SettingKind.Toggle, SettingValue.Toggle(true)),
                Descriptor(feature, "brief", "简报", "恰好很短。", SettingKind.Toggle, SettingValue.Toggle(false)));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var truncated = settingRow(model, feature.Value, "notify").Description;
            Assert(truncated.Length == 123 && truncated.EndsWith("...", StringComparison.Ordinal)
                && truncated.Substring(0, 120) == longDescription.Substring(0, 120), "描述截断 120 对齐 UPM（120 字 + 省略号）");
            Assert(settingRow(model, feature.Value, "brief").Description == "恰好很短。", "≤120 原样展示");
        }

        // Q37：描述空则不画、不占位——投影层面就是空字符串（渲染层据空串跳行）。
        private static void EmptyDescriptionDoesNotOccupyRow()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.emptydesc");
            editor.Seed(feature, ToggleEntry("notify", true));
            editor.SeedDescriptors(feature, Descriptor(feature, "notify", "通知", "",
                SettingKind.Toggle, SettingValue.Toggle(true)));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var row = settingRow(model, feature.Value, "notify");
            Assert(row.Description == string.Empty, "空描述投影为空串（渲染不占位）");
            Assert(row.DisplayName == "通知", "显示名仍画");
        }

        // 组合面（BII 等）今天没有 schema：无描述符时显示名回退 SettingId、形状按值 Kind，
        // 与 01 行为连续；文案的中文化是 07 的对照表工作，不属本票。
        private static void DisplayNameFallsBackToSettingIdWithoutDescriptor()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.fallback");
            editor.Seed(feature, ToggleEntry("AutoRotate", true));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var row = settingRow(model, feature.Value, "AutoRotate");
            Assert(row.DisplayName == "AutoRotate", "无描述符→显示名回退 SettingId");
            Assert(row.Description == string.Empty, "无描述符→无描述");
            Assert(row.ControlKind == PanelSettingControlKind.Toggle, "形状按值 Kind 维持 Toggle");
        }

        // 档位来源优先级（镜像 SettingsRuntime 校验口径）：服务端策略收窄的非空 AllowedValues
        // 覆盖描述符档位——Cycle 只在策略允许的档位里循环。
        private static void PolicyLevelsProjectToCycle()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.policy");
            var policy = new SettingPolicyView(new SettingValueOption(false, default(SettingValue)), new SettingValueOption(false, default(SettingValue)),
                new SettingValueOption(false, default(SettingValue)), new[] { SettingValue.Choice("dark") }, 0, string.Empty);
            editor.Seed(feature, new SettingEntryView("theme", SettingAuthority.ServerPolicyWithClientPreference,
                new SettingValueOption(true, SettingValue.Choice("dark")), true, policy, SettingValue.Choice("dark"), true, true));
            editor.SeedDescriptors(feature, Descriptor(feature, "theme", "主题", "服务端可收窄的选项。",
                SettingKind.Choice, SettingValue.Choice("dark"), "dark", "light"));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var row = settingRow(model, feature.Value, "theme");
            Assert(row.ControlKind == PanelSettingControlKind.Cycle, "策略档位仍走 Cycle");
            Assert(row.AllowedValues.Count == 1 && row.AllowedValues[0] == "dark", "策略非空档位覆盖描述符档位");
            Assert(model.DraftCycleBueSetting("theme", 1) && !model.IsDirty, "收窄到单档后左右键都不变脏");
        }

        // Q43：ServerAuthority / CanEdit=false 画只读行——不画灰掉的假控件、不进草稿、
        // 不标未保存、不参与保存（01 已裁草稿范围，本票在投影缝上锁形状）。
        private static void ServerAuthorityAndReadOnlyRowsDoNotEnterDraft()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.server");
            editor.Seed(feature,
                ToggleEntry("notify", true),
                new SettingEntryView("server-quota", SettingAuthority.ServerAuthoritative, new SettingValueOption(false, default(SettingValue)),
                    false, default(SettingPolicyView), SettingValue.IntegerValue(3), true, false),
                new SettingEntryView("state-proj", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(true)),
                    false, default(SettingPolicyView), SettingValue.Toggle(true), true, false));
            editor.SeedDescriptors(feature,
                Descriptor(feature, "server-quota", "服务器配额", "由服务器决定。", SettingKind.Integer, SettingValue.IntegerValue(3)),
                Descriptor(feature, "state-proj", "状态投影", "只读状态。", SettingKind.Toggle, SettingValue.Toggle(true)));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            Assert(settingRow(model, feature.Value, "server-quota").ControlKind == PanelSettingControlKind.ReadOnly, "ServerAuthority 只读行");
            Assert(settingRow(model, feature.Value, "state-proj").ControlKind == PanelSettingControlKind.ReadOnly, "CanEdit=false 只读行");
            Assert(!model.DraftCycleBueSetting("server-quota", 1) && !model.DraftCycleBueSetting("state-proj", 1), "只读行不进 Cycle 草稿");
            Assert(!model.IsDirty && editor.BatchCalls == 0, "不可编项不脏不写");
        }

        // 外部配置的「同一控件缝」（Q38/Q40/Q42，本票只留缝、不做采集——采集与失败分类归 08）：
        // PluginConfigEntryView 携带 Description 与 AllowedChoices，投影出与 BUE 行同形状的
        // 显示名→描述→控件；有档位即 Cycle、改动进同一草稿。布尔维持 Toggle、数字维持文本框、
        // Unsupported 只读。
        private static void ExternalConfigRowProjectsCycleSeam()
        {
            var editor = new ControlFixtureEditor();
            var configEditor = new RecordingPluginEditor();
            var model = NewModel(editor, configEditor);
            var longDescription = new string('设', 130);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                new LoadedPluginDescriptor("com.example.v402", "缝插件", "1.0.0", new[]
                {
                    new PluginConfigEntryView("gfx.mode", "画面模式", PluginConfigValueKind.String, PluginConfigValue.StringValue("窗口"),
                        false, true, null, null, 4096, "窗口：无边框前切换。", new[] { "窗口", "全屏", "无边框" }),
                    new PluginConfigEntryView("gfx.long", "长说明", PluginConfigValueKind.String, PluginConfigValue.StringValue("x"),
                        false, true, null, null, 4096, longDescription),
                    new PluginConfigEntryView("core.enabled", "核心开关", PluginConfigValueKind.Boolean, PluginConfigValue.BooleanValue(true),
                        false, true),
                    new PluginConfigEntryView("core.rate", "速率", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(4),
                        false, true, 0, 100),
                    new PluginConfigEntryView("core.complex", "复杂值", PluginConfigValueKind.Unsupported, PluginConfigValue.UnsupportedValue(),
                        false, false),
                })
            });
            model.OpenDetail("com.example.v402");

            var cycle = configRow(model, "com.example.v402", "gfx.mode");
            Assert(cycle.ControlKind == PanelSettingControlKind.Cycle, "外部 AllowedChoices 非空 → 同一 Cycle 控件缝");
            Assert(cycle.DisplayName == "画面模式" && cycle.Description == "窗口：无边框前切换。", "外部行走同一显示名→描述结构");
            Assert(configRow(model, "com.example.v402", "gfx.long").Description == longDescription.Substring(0, 120) + "...", "外部描述同用 120 截断");
            Assert(configRow(model, "com.example.v402", "core.enabled").ControlKind == PanelSettingControlKind.Toggle, "布尔维持 Toggle 形状");
            Assert(configRow(model, "com.example.v402", "core.rate").ControlKind == PanelSettingControlKind.TextEditor, "数字维持文本编辑形状");
            Assert(configRow(model, "com.example.v402", "core.complex").ControlKind == PanelSettingControlKind.ReadOnly, "Unsupported 只读、不画假控件");

            Assert(model.DraftCyclePluginConfig("com.example.v402", "gfx.mode", 1), "外部档位左键受理");
            Assert(configEditor.SetCalls == 0 && model.IsDirty, "外部档位只进草稿不写盘");
            Assert(configRow(model, "com.example.v402", "gfx.mode").Effective.Text == "全屏", "外部 Cycle 显示草稿档位");
            model.DraftCyclePluginConfig("com.example.v402", "gfx.mode", -1);
            Assert(!model.IsDirty, "外部拨回原档不脏");
            Assert(!model.DraftCyclePluginConfig("com.example.v402", "core.enabled", 1), "无档位行不可 Cycle");
        }

        // Q39：Toggle/Integer/Float/Text 维持形状只补显示名描述；KeyBinding 无专用捕获
        // （维持文本编辑形状，不录制快捷键）。
        private static void ToggleAndTextRowsKeepShapeWithNames()
        {
            var editor = new ControlFixtureEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.v402.shape");
            editor.Seed(feature, ToggleEntry("notify", true), IntegerEntry("limit", 5),
                new SettingEntryView("reload-key", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.KeyBinding("r")),
                    false, default(SettingPolicyView), SettingValue.KeyBinding("r"), true, true));
            editor.SeedDescriptors(feature,
                Descriptor(feature, "notify", "开启通知", "弹出交接提示。", SettingKind.Toggle, SettingValue.Toggle(true)),
                Descriptor(feature, "limit", "上限", "整数值。", SettingKind.Integer, SettingValue.IntegerValue(5)),
                Descriptor(feature, "reload-key", "换弹键", "按键文本。", SettingKind.KeyBinding, SettingValue.KeyBinding("r")));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            Assert(settingRow(model, feature.Value, "notify").ControlKind == PanelSettingControlKind.Toggle, "Toggle 形状维持");
            Assert(settingRow(model, feature.Value, "notify").DisplayName == "开启通知", "Toggle 补显示名");
            Assert(settingRow(model, feature.Value, "limit").ControlKind == PanelSettingControlKind.TextEditor, "Integer 形状维持");
            Assert(settingRow(model, feature.Value, "reload-key").ControlKind == PanelSettingControlKind.TextEditor, "KeyBinding 无专用捕获：维持文本形状");
        }

        // ── helpers ──

        private static ManagementPanelModel NewModel(IBueSettingsEditor editor, IPluginConfigEditor configEditor = null)
        {
            return new ManagementPanelModel(new MemoryStore(), editor, configEditor);
        }

        private static BueFeatureManagementEntry Feature(FeatureId feature, ControlFixtureEditor editor)
        {
            return new BueFeatureManagementEntry(feature, "面板控件", "1.0.0", FeatureState.Running,
                new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1), editor.GetSnapshot(feature));
        }

        private static SettingEntryView ToggleEntry(string id, bool value)
        {
            return new SettingEntryView(id, SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(value)),
                false, default(SettingPolicyView), SettingValue.Toggle(value), true, true);
        }

        private static SettingEntryView ChoiceEntry(string id, string value)
        {
            return new SettingEntryView(id, SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Choice(value)),
                false, default(SettingPolicyView), SettingValue.Choice(value), true, true);
        }

        private static SettingEntryView IntegerEntry(string id, int value)
        {
            return new SettingEntryView(id, SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.IntegerValue(value)),
                false, default(SettingPolicyView), SettingValue.IntegerValue(value), true, true);
        }

        private static SettingDescriptor Descriptor(FeatureId feature, string id, string displayName, string description,
            SettingKind kind, SettingValue defaultValue, params string[] allowed)
        {
            var allowedValues = new List<SettingValue>();
            foreach (var value in allowed) allowedValues.Add(SettingValue.Choice(value));
            return new SettingDescriptor(feature, id, displayName, description, kind, SettingAuthority.ClientLocal,
                defaultValue, default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                allowedValues, 256, string.Empty, 1, 0, string.Empty, string.Empty);
        }

        private static PanelSettingRowView settingRow(ManagementPanelModel model, string stableId, string settingId)
        {
            var rows = model.GetSettingRows(stableId);
            for (var index = 0; index < rows.Count; index++)
                if (rows[index].SettingId == settingId) return rows[index];
            throw new InvalidOperationException("setting row not found: " + settingId);
        }

        private static PanelConfigRowView configRow(ManagementPanelModel model, string stableId, string key)
        {
            var rows = model.GetPluginConfigRows(stableId);
            for (var index = 0; index < rows.Count; index++)
                if (rows[index].Key == key) return rows[index];
            throw new InvalidOperationException("config row not found: " + key);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        // ── fakes ──

        private sealed class MemoryStore : IManagementPanelPreferencesStore
        {
            private ManagementPanelPreferences preferences = ManagementPanelPreferences.Empty;
            public ManagementPanelPreferences Load() { return preferences; }
            public void Save(ManagementPanelPreferences value) { preferences = value; }
        }

        private sealed class RecordingPluginEditor : IPluginConfigEditor
        {
            internal int SetCalls;
            internal readonly List<string> WrittenKeys = new List<string>();
            public bool TrySet(string pluginGuid, string key, PluginConfigValue value) { SetCalls++; WrittenKeys.Add(key); return true; }
        }

        // 面板控件缝的权威源假件：值库 + revision + 描述符表（DEV-V4-02 的投影输入），
        // BatchCalls/SetCalls 计数用于证明「Cycle 改的是草稿、不立刻写盘」。
        private sealed class ControlFixtureEditor : IBueSettingsEditor
        {
            private readonly Dictionary<string, List<SettingEntryView>> byFeature = new Dictionary<string, List<SettingEntryView>>(StringComparer.Ordinal);
            private readonly Dictionary<string, uint> revisions = new Dictionary<string, uint>(StringComparer.Ordinal);
            private readonly Dictionary<string, List<SettingDescriptor>> descriptors = new Dictionary<string, List<SettingDescriptor>>(StringComparer.Ordinal);
            internal int BatchCalls;
            internal IReadOnlyList<SettingMutation> LastBatchMutations;

            internal void Seed(FeatureId feature, params SettingEntryView[] entries)
            {
                byFeature[feature.Value] = new List<SettingEntryView>(entries);
                revisions[feature.Value] = 0;
            }

            internal void SeedDescriptors(FeatureId feature, params SettingDescriptor[] schema)
            {
                descriptors[feature.Value] = new List<SettingDescriptor>(schema);
            }

            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature) { return SnapshotFor(feature); }

            public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
            {
                List<SettingDescriptor> list;
                return descriptors.TryGetValue(feature.Value, out list) ? list : (IReadOnlyList<SettingDescriptor>)new SettingDescriptor[0];
            }

            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                return ApplyBatch(feature, expectedRevision, new[] { mutation });
            }

            public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
            {
                BatchCalls++;
                LastBatchMutations = mutations;
                var revision = revisions[feature.Value];
                if (expectedRevision != revision) return new SettingChangeResult(false, FrameworkErrorCode.SettingRevisionConflict, revision, SnapshotFor(feature));
                if (mutations == null || mutations.Count == 0) return new SettingChangeResult(true, FrameworkErrorCode.None, revision, SnapshotFor(feature));
                var list = byFeature[feature.Value];
                var committed = new List<SettingEntryView>(list.Count);
                foreach (var entryView in list)
                {
                    var updated = entryView;
                    foreach (var mutation in mutations)
                        if (string.Equals(mutation.SettingId, entryView.SettingId, StringComparison.Ordinal))
                            updated = new SettingEntryView(entryView.SettingId, entryView.Authority, new SettingValueOption(true, mutation.Value),
                                entryView.HasPolicy, entryView.Policy, mutation.Value, entryView.IsVisible, entryView.CanEdit);
                    committed.Add(updated);
                }
                byFeature[feature.Value] = committed;
                revisions[feature.Value] = revision + 1;
                return new SettingChangeResult(true, FrameworkErrorCode.None, revision + 1, SnapshotFor(feature));
            }

            private FeatureSettingsSnapshot SnapshotFor(FeatureId feature)
            {
                var revision = revisions.TryGetValue(feature.Value, out var r) ? r : 0u;
                List<SettingEntryView> list;
                if (!byFeature.TryGetValue(feature.Value, out list)) list = new List<SettingEntryView>();
                return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, revision,
                    SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, list);
            }
        }
    }
}
