using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    // DEV-V4-01：未保存草稿与配置保存模型（spec「未保存草稿与配置保存」节 + V4-T2 裁决）。
    // 本组只经 ManagementPanel 模型 seam 断言外显行为（草稿脏/干净、命令不写权威源、
    // 确认三选一后果、跨源部分成功、单次原子提交、ExpectedRevision 过期文案、无自动保存、
    // 草稿范围排除 ServerAuthority/只读），不断言 Glazier 控件树。
    internal static class DevV4DraftTests
    {
        internal static void Run()
        {
            EditDoesNotWriteAuthoritativeSource();
            DirtyIsByFinalValueFlipBackClears();
            ConfirmSaveDiscardCancelConsequences();
            SaveWhenNotDirtyIsNoOp();
            SaveAppliesAllSettingsInOneAtomicSubmit();
            RevisionConflictCarriesTheExactText();
            CrossSourcePartialSuccessStillAttemptsLaterSources();
            ToggleFailureKeepsIntentInDraft();
            ExternalRestartBadgeOnlyOnSuccessfulWrite();
            PluginVanishedMidSessionIsNotASuccess();
            ExternalConfigWritesInStableOrder();
            DraftScopeExcludesReadOnlyAndServerAuthority();
            NoBackgroundAutoSaveSurvivesRefreshAndRemount();
            SaveCommittedLifecycleIntentFlag();
            CollisionFeatureAndPluginShareStableIdRoutesByKind();
            CollisionFavoriteStarDoesNotCrossKind();
            LegacyBareFavoriteBindsFeatureRowOnly();
            LegacyBareFavoriteForPluginOnlyGuidStaysOnPluginRow();
            RestartBadgeDoesNotCrossKind();
        }

        private static void CollisionFeatureAndPluginShareStableIdRoutesByKind()
        {
            // DEV-V4-09 F4（实机发现）：生态标准形态=插件 GUID 与功能 id 同为
            // io.github.yu80rice.bue.noop（NoOp 样板），侧栏两行共享同一 StableId。
            // 模型必须能按行种类开正确草稿、同 StableId 换种类=换草稿（旧保留
            // 判定只看 StableId=缺陷形态）；单参重载保持 features-first 旧语义。
            var editor = new DraftableSettingsEditor();
            var configEditor = new RecordingPluginEditor();
            var shared = "io.github.yu80rice.bue.noop";
            var feature = new FeatureId(shared);
            editor.Seed(feature, ToggleEntry("probe", true));
            var model = NewModel(editor, configEditor);
            model.Refresh(new[] { Feature(feature, "样板功能", editor) }, new[]
            {
                new LoadedPluginDescriptor(shared, "样板插件", "0.0.0", new[]
                {
                    new PluginConfigEntryView("ButtonText", "ButtonText", PluginConfigValueKind.String, PluginConfigValue.StringValue("插件管理"), false, true),
                })
            });

            // 1) 按插件种类开：外部编辑受理、功能编辑被拒，保存只写外部源。
            model.OpenDetail(shared, ManagementEntryKind.ExternalPlugin);
            Assert(model.DraftEditPluginConfig(shared, "ButtonText", "插件"), "冲突：插件种类草稿受理外部编辑");
            Assert(model.IsDirty, "冲突：插件草稿脏");
            Assert(!model.DraftEditBueSetting("probe", PluginConfigValue.BooleanValue(false)), "冲突：插件种类草稿拒功能编辑");
            var pluginReport = model.SaveDraft();
            Assert(pluginReport.Outcome == DraftSaveOutcome.Success && configEditor.SetCalls == 1 && editor.BatchCalls == 0,
                "冲突：插件草稿保存只触外部源（功能源零写）");

            // 2) 按功能种类开：功能编辑受理、外部编辑被拒，保存只写功能源。
            model.OpenDetail(shared, ManagementEntryKind.BueFeature);
            Assert(model.DraftEditBueSetting("probe", PluginConfigValue.BooleanValue(false)), "冲突：功能种类草稿受理功能编辑");
            Assert(!model.DraftEditPluginConfig(shared, "ButtonText", "x"), "冲突：功能种类草稿拒外部编辑");
            var featureReport = model.SaveDraft();
            Assert(featureReport.Outcome == DraftSaveOutcome.Success && editor.BatchCalls == 1 && configEditor.SetCalls == 1,
                "冲突：功能草稿保存只触功能源（外部源零写）");

            // 3) 同 StableId 换种类=重置草稿（不携带旧种类的脏）。权威值此刻
            // probe=false（第 2 段已保存），改成 true 才算脏。
            model.OpenDetail(shared, ManagementEntryKind.BueFeature);
            model.DraftEditBueSetting("probe", PluginConfigValue.BooleanValue(true));
            Assert(model.IsDirty, "冲突：功能草稿脏");
            model.OpenDetail(shared, ManagementEntryKind.ExternalPlugin);
            Assert(!model.IsDirty, "冲突：换种类重置草稿（功能脏不带入插件页）");

            // 4) 确认框导航带目标种类：脏功能草稿→保存并离开到插件种类行。
            model.OpenDetail(shared, ManagementEntryKind.BueFeature);
            model.DraftEditBueSetting("probe", PluginConfigValue.BooleanValue(true));
            DraftSaveReport leaveReport;
            Assert(model.TryLeaveDetail(shared, ManagementEntryKind.ExternalPlugin, PanelConfirmChoice.Save, out leaveReport)
                    && leaveReport.Outcome == DraftSaveOutcome.Success,
                "冲突：确认保存受理（功能源单写）");
            Assert(editor.BatchCalls == 2 && configEditor.SetCalls == 1, "冲突：确认保存只写功能源");
            Assert(model.DraftEditPluginConfig(shared, "ButtonText", "管理"), "冲突：导航后落在插件种类草稿");
            Assert(!model.DraftEditBueSetting("probe", PluginConfigValue.BooleanValue(true)), "冲突：导航后不再是功能草稿");

            // 5) 单参重载=旧语义（features-first），既有调用方/测试不受影响。
            model.DiscardDraft();
            model.OpenDetail(shared);
            Assert(model.DraftEditBueSetting("probe", PluginConfigValue.BooleanValue(false)), "兼容：单参 OpenDetail=功能优先");
        }

        private static void CollisionFavoriteStarDoesNotCrossKind()
        {
            // POST-P4-03：NoOp 同 id 两行不得共用一颗星。种类+稳定 id 双键；
            // 单参 ToggleFavorite 保持 features-first，不得倒退成两行一起亮。
            var editor = new DraftableSettingsEditor();
            var shared = "io.github.yu80rice.bue.noop";
            var feature = new FeatureId(shared);
            editor.Seed(feature, ToggleEntry("probe", true));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "样板功能", editor) }, new[]
            {
                new LoadedPluginDescriptor(shared, "样板插件", "0.0.0", new PluginConfigEntryView[0])
            });

            Assert(model.ToggleFavorite(shared, ManagementEntryKind.BueFeature), "收藏功能行受理");
            Assert(entry(model, shared, ManagementEntryKind.BueFeature).IsFavorite, "功能行已收藏");
            Assert(!entry(model, shared, ManagementEntryKind.ExternalPlugin).IsFavorite, "同 id 插件行不得跟亮");

            Assert(model.ToggleFavorite(shared, ManagementEntryKind.ExternalPlugin), "收藏插件行受理");
            Assert(entry(model, shared, ManagementEntryKind.BueFeature).IsFavorite, "功能行收藏仍在");
            Assert(entry(model, shared, ManagementEntryKind.ExternalPlugin).IsFavorite, "插件行独立收藏");

            Assert(model.ToggleFavorite(shared, ManagementEntryKind.BueFeature), "取消功能行收藏");
            Assert(!entry(model, shared, ManagementEntryKind.BueFeature).IsFavorite, "功能行已取消");
            Assert(entry(model, shared, ManagementEntryKind.ExternalPlugin).IsFavorite, "取消功能行不得摘掉插件星");

            var fresh = NewModel(editor);
            fresh.Refresh(new[] { Feature(feature, "样板功能", editor) }, new[]
            {
                new LoadedPluginDescriptor(shared, "样板插件", "0.0.0", new PluginConfigEntryView[0])
            });
            Assert(fresh.ToggleFavorite(shared), "单参 ToggleFavorite 受理");
            Assert(entry(fresh, shared, ManagementEntryKind.BueFeature).IsFavorite, "单参=功能优先，功能行亮星");
            Assert(!entry(fresh, shared, ManagementEntryKind.ExternalPlugin).IsFavorite, "单参不得把同 id 插件行一起收藏");
        }

        private static void LegacyBareFavoriteBindsFeatureRowOnly()
        {
            // POST-P4-03：磁盘旧单键 favorite=<id> 只贴功能行；同 id 插件行默认未收藏。
            var editor = new DraftableSettingsEditor();
            var shared = "io.github.yu80rice.bue.noop";
            var feature = new FeatureId(shared);
            editor.Seed(feature, ToggleEntry("probe", true));
            var store = new MemoryStore();
            store.Save(new ManagementPanelPreferences(ManagementSortOrder.NameAscending, new[] { shared }));
            var model = new ManagementPanelModel(store, editor);
            model.Refresh(new[] { Feature(feature, "样板功能", editor) }, new[]
            {
                new LoadedPluginDescriptor(shared, "样板插件", "0.0.0", new PluginConfigEntryView[0])
            });
            Assert(entry(model, shared, ManagementEntryKind.BueFeature).IsFavorite, "旧单键记录贴功能行");
            Assert(!entry(model, shared, ManagementEntryKind.ExternalPlugin).IsFavorite, "旧单键记录不贴同 id 插件行");
        }

        private static void LegacyBareFavoriteForPluginOnlyGuidStaysOnPluginRow()
        {
            // POST-P4-03（Spec R1#1）：插件独有 GUID 的旧裸 id 归桶后不得再漂到功能行。
            var editor = new DraftableSettingsEditor();
            var guid = "com.example.solo";
            var store = new MemoryStore();
            store.Save(new ManagementPanelPreferences(ManagementSortOrder.NameAscending, new[] { guid }));
            var model = new ManagementPanelModel(store, editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                new LoadedPluginDescriptor(guid, "独有插件", "1.0.0", new PluginConfigEntryView[0])
            });
            Assert(entry(model, guid, ManagementEntryKind.ExternalPlugin).IsFavorite, "裸 id 首次刷新归插件桶");

            // 归桶结果写回 store（含 plugin: 前缀），功能行后注册不抢星。
            var prefs = store.Load();
            Assert(prefs.FavoriteIds.Count == 1 && prefs.FavoriteIds[0] == "plugin:" + guid,
                "迁移把裸 id 改写成 plugin: 前缀并持久化");
            editor.Seed(new FeatureId(guid), ToggleEntry("probe", true));
            model.Refresh(new[] { Feature(new FeatureId(guid), "同名功能", editor) }, new[]
            {
                new LoadedPluginDescriptor(guid, "独有插件", "1.0.0", new PluginConfigEntryView[0])
            });
            Assert(!entry(model, guid, ManagementEntryKind.BueFeature).IsFavorite, "后注册同名功能不得抢星");
            Assert(entry(model, guid, ManagementEntryKind.ExternalPlugin).IsFavorite, "插件星不漂走");
        }

        private static void RestartBadgeDoesNotCrossKind()
        {
            // POST-P4-03：RequiresRestart 顶栏徽章跟当前详情种类走，不串到同 id 另一行。
            var editor = new DraftableSettingsEditor();
            var configEditor = new RecordingPluginEditor();
            var shared = "io.github.yu80rice.bue.noop";
            var feature = new FeatureId(shared);
            editor.Seed(feature, ToggleEntry("probe", true));
            var model = NewModel(editor, configEditor);
            model.Refresh(new[] { Feature(feature, "样板功能", editor) }, new[]
            {
                new LoadedPluginDescriptor(shared, "样板插件", "0.0.0", new[]
                {
                    new PluginConfigEntryView("Threads", "线程数", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(2), true, true, 1, 16),
                })
            });

            model.OpenDetail(shared, ManagementEntryKind.ExternalPlugin);
            Assert(model.DraftEditPluginConfig(shared, "Threads", "8"), "插件页重启项进草稿");
            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success && report.RequiresRestart, "插件页写入重启项");
            Assert(model.ShowsRestartBadge, "当前插件详情点亮需要重启");

            model.OpenDetail(shared, ManagementEntryKind.BueFeature);
            Assert(!model.ShowsRestartBadge, "切到同 id 功能页不得点亮需要重启");

            model.OpenDetail(shared, ManagementEntryKind.ExternalPlugin);
            Assert(model.ShowsRestartBadge, "回到写入过重启项的插件页仍点亮");

            // 行级（需要重启）标记只存在于插件配置投影：功能页的行集里不得出现
            // 插件行的重启标记（Spec R1#2 补全「顶栏/行级」两面的种类范围）。
            var pluginRows = model.GetPluginConfigRows(shared);
            Assert(pluginRows.Count == 1 && pluginRows[0].RequiresRestart, "插件页行级（需要重启）如实标记");
            model.OpenDetail(shared, ManagementEntryKind.BueFeature);
            var featureRows = model.GetSettingRows(shared);
            Assert(featureRows.Count == 1 && featureRows[0].SettingId == "probe", "功能页不消费插件配置行");
        }

        private static void EditDoesNotWriteAuthoritativeSource()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft");
            editor.Seed(feature, ToggleEntry("notify", true));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "草稿功能", editor) }, new LoadedPluginDescriptor[0]);

            model.OpenDetail(feature.Value);
            var edited = model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));

            Assert(edited, "草稿编辑受理");
            Assert(!model.HasOpenDetail == false && model.IsDirty, "编辑后条目脏");
            Assert(editor.BatchCalls == 0 && editor.ApplyCalls == 0, "改设置不立刻写权威源");
            Assert(entry(model, feature.Value).BueSettings[0].EffectiveValue.Boolean, "权威快照仍为原值（未落盘）");
        }

        private static void DirtyIsByFinalValueFlipBackClears()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft2");
            editor.Seed(feature, ToggleEntry("notify", true));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "草稿功能", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));
            Assert(model.IsDirty, "改成不同值即脏");
            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(true));
            Assert(!model.IsDirty, "拨回原值不再脏（按最终值判定，不按是否按过控件）");
            Assert(!model.IsBueSettingDirty("notify"), "该行不再标未保存");
        }

        private static void ConfirmSaveDiscardCancelConsequences()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft3");
            var other = new FeatureId("io.github.yu80rice.bue.draft3b");
            editor.Seed(feature, ToggleEntry("notify", true));
            editor.Seed(other, ToggleEntry("notify", true));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "甲", editor), Feature(other, "乙", editor) }, new LoadedPluginDescriptor[0]);

            // 取消：不导航、草稿不动、不写权威源。
            model.OpenDetail(feature.Value);
            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));
            Assert(!model.TryLeaveDetail(other.Value, PanelConfirmChoice.Cancel, out _), "取消不导航");
            Assert(model.OpenStableId == feature.Value && model.IsDirty, "取消后仍停在原条且草稿保留");
            Assert(editor.BatchCalls == 0, "取消不写权威源");

            // 不保存：丢草稿、导航、不写权威源。
            Assert(model.TryLeaveDetail(other.Value, PanelConfirmChoice.Discard, out _), "不保存允许导航");
            Assert(editor.BatchCalls == 0, "不保存不写权威源");
            Assert(model.OpenStableId == other.Value && !model.IsDirty, "不保存后切到新条且无脏草稿");

            // 保存成功：等同保存配置，写权威源后再导航；交回同一份报告。
            model.OpenDetail(feature.Value);
            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));
            DraftSaveReport saved;
            Assert(model.TryLeaveDetail(other.Value, PanelConfirmChoice.Save, out saved), "保存成功后导航");
            Assert(editor.BatchCalls == 1, "确认框保存等同保存配置（一次原子提交）");
            Assert(saved != null && saved.Outcome == DraftSaveOutcome.Success && saved.PrimaryMessage == "配置已保存。", "确认框保存交回保存配置的报告");
            Assert(model.OpenStableId == other.Value, "保存成功后切到目标条");

            // 保存失败：不导航、失败草稿保留、交回带原因的报告（不另造文案）。
            var third = new FeatureId("io.github.yu80rice.bue.draft3c");
            editor.Seed(third, ToggleEntry("notify", true));
            model.Refresh(new[] { Feature(feature, "甲", editor), Feature(other, "乙", editor), Feature(third, "丙", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(third.Value);
            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));
            editor.RejectNextAsStale = true;
            DraftSaveReport failed;
            Assert(!model.TryLeaveDetail(other.Value, PanelConfirmChoice.Save, out failed), "保存失败不导航");
            Assert(failed != null && failed.Outcome == DraftSaveOutcome.PartialFailure && ContainsMessage(failed, "未保存：设置已在别处变更。"), "确认框保存失败交回过期文案");
            Assert(model.OpenStableId == third.Value && model.IsDirty, "保存失败留在本条且草稿保留");
        }

        private static void SaveWhenNotDirtyIsNoOp()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft4");
            editor.Seed(feature, ToggleEntry("notify", true));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.NoChanges, "不脏点保存=空操作");
            Assert(report.PrimaryMessage == "没有需要保存的修改。", "空操作文案");
            Assert(editor.BatchCalls == 0 && editor.ApplyCalls == 0, "空操作不写权威源");
        }

        private static void SaveAppliesAllSettingsInOneAtomicSubmit()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft5");
            editor.Seed(feature, ToggleEntry("notify", true), IntegerEntry("limit", 5));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));
            model.DraftEditBueSetting("limit", PluginConfigValue.IntegerValue(9));
            var report = model.SaveDraft();

            Assert(editor.BatchCalls == 1, "整条草稿字段一次原子提交（非逐字段各写一次）");
            Assert(editor.LastBatchMutations != null && editor.LastBatchMutations.Count == 2, "提交含两条草稿字段");
            Assert(editor.LastExpectedRevision == 0, "提交以进入详情时的基准 revision 为 ExpectedRevision");
            Assert(report.Outcome == DraftSaveOutcome.Success && report.PrimaryMessage == "配置已保存。", "全成功文案");
            Assert(!model.IsDirty, "全成功后草稿清空");
        }

        private static void RevisionConflictCarriesTheExactText()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft6");
            editor.Seed(feature, ToggleEntry("notify", true));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);
            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));

            editor.RejectNextAsStale = true;   // 模拟别处已改：ExpectedRevision 过期
            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.PartialFailure, "过期整源拒写");
            Assert(ContainsMessage(report, "未保存：设置已在别处变更。"), "过期文案");
            Assert(model.IsDirty, "过期草稿全留");
        }

        private static void CrossSourcePartialSuccessStillAttemptsLaterSources()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft7");
            editor.Seed(feature, ToggleEntry("notify", true));
            var toggles = new RecordingToggleHandler();
            var model = NewModel(editor);
            model.FeatureToggleHandler = toggles.Handle;
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));
            model.DraftSetFeatureEnabled(false);
            editor.RejectNextInvalid = true;   // 设置源失败
            var report = model.SaveDraft();

            Assert(report.Outcome == DraftSaveOutcome.PartialFailure, "设置源失败=部分失败");
            Assert(toggles.Calls.Count == 1 && toggles.Calls[0] == false, "设置失败仍尝试后续源（启停照常提交）");
            Assert(model.IsDirty, "失败项仍留草稿");
            Assert(!model.IsFeatureEnableDirty, "成功的启停意图不再脏");
        }

        private static void ToggleFailureKeepsIntentInDraft()
        {
            // DEV-V4-03：生命周期机拒绝不允许的转换（handler=false）→ 该启停
            // 意图留在草稿并给原因（跨源部分成功的启停面）；面板不按 FeatureState
            // 写分支——拒绝语义只在机里。
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft11");
            editor.Seed(feature, ToggleEntry("notify", true));
            var toggles = new RecordingToggleHandler { Result = false };
            var model = NewModel(editor);
            model.FeatureToggleHandler = toggles.Handle;
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);
            model.DraftSetFeatureEnabled(false);

            var report = model.SaveDraft();

            Assert(report.Outcome == DraftSaveOutcome.PartialFailure, "启停被生命周期机拒绝=部分失败");
            Assert(ContainsMessage(report, "未保存：功能启停失败。"), "拒绝给启停失败原因");
            Assert(toggles.Calls.Count == 1 && toggles.Calls[0] == false, "拒绝前确实提交过目标状态");
            Assert(model.IsDirty && model.IsFeatureEnableDirty, "被拒启停意图留在草稿");
        }

        private static void ExternalRestartBadgeOnlyOnSuccessfulWrite()
        {
            var editor = new DraftableSettingsEditor();
            var configEditor = new RecordingPluginEditor();
            var model = NewModel(editor, configEditor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                new LoadedPluginDescriptor("com.example.restart", "重启插件", "1.0.0", new[]
                {
                    new PluginConfigEntryView("Threads", "线程数", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(2), true, true, 1, 16),
                })
            });
            model.OpenDetail("com.example.restart");
            model.DraftEditPluginConfig("com.example.restart", "Threads", "8");
            Assert(model.IsDirty, "外部配置进草稿");
            Assert(configEditor.SetCalls == 0, "编辑外部配置不立刻写");

            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success, "外部配置保存成功");
            Assert(configEditor.SetCalls == 1, "保存才写外部 ConfigEntry");
            Assert(report.RequiresRestart, "RequiresRestart 项成功写入才打需要重启徽章");
        }

        private static void PluginVanishedMidSessionIsNotASuccess()
        {
            var editor = new DraftableSettingsEditor();
            var configEditor = new RecordingPluginEditor();
            var model = NewModel(editor, configEditor);
            var descriptor = new LoadedPluginDescriptor("com.example.gone", "会消失", "1.0.0", new[]
            {
                new PluginConfigEntryView("K", "K", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(1), false, true, 0, 100),
            });
            model.Refresh(new BueFeatureManagementEntry[0], new[] { descriptor });
            model.OpenDetail("com.example.gone");
            Assert(model.DraftEditPluginConfig("com.example.gone", "K", "5"), "外部配置进草稿");
            Assert(model.IsDirty, "改值即脏");

            // 保存前插件从目录消失（Q25：对应 ConfigEntry 保存失败，不得假成功）。
            model.Refresh(new BueFeatureManagementEntry[0], new LoadedPluginDescriptor[0]);
            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.PartialFailure, "插件已卸载≠保存成功");
            Assert(configEditor.SetCalls == 0, "插件没了不写任何源");
            Assert(ContainsMessage(report, "未保存：插件已卸载。"), "给出插件已卸载原因");
            Assert(model.IsDirty, "失败草稿保留");
        }

        private static void ExternalConfigWritesInStableOrder()
        {
            // Q27：外部 ConfigEntry 稳定顺序逐条写（=面板展示序，非编辑字典序）。
            var editor = new DraftableSettingsEditor();
            var configEditor = new RecordingPluginEditor();
            var model = NewModel(editor, configEditor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                new LoadedPluginDescriptor("com.example.order", "顺序", "1.0.0", new[]
                {
                    new PluginConfigEntryView("zeta", "zeta", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(1), false, true, 0, 100),
                    new PluginConfigEntryView("alpha", "alpha", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(1), false, true, 0, 100),
                    new PluginConfigEntryView("mid", "mid", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(1), false, true, 0, 100),
                })
            });
            model.OpenDetail("com.example.order");
            model.DraftEditPluginConfig("com.example.order", "mid", "9");
            model.DraftEditPluginConfig("com.example.order", "zeta", "8");
            model.DraftEditPluginConfig("com.example.order", "alpha", "7");

            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success, "多键外部配置整单成功");
            Assert(string.Join(",", configEditor.WrittenKeys) == "zeta,alpha,mid",
                "外部 ConfigEntry 按 ConfigEntries 稳定顺序逐条写（非草稿字典序）");
        }

        private static void DraftScopeExcludesReadOnlyAndServerAuthority()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft9");
            editor.Seed(feature,
                ToggleEntry("notify", true),
                new SettingEntryView("server-quota", SettingAuthority.ServerAuthoritative, new SettingValueOption(false, default(SettingValue)),
                    false, default(SettingPolicyView), SettingValue.IntegerValue(3), true, false),
                new SettingEntryView("state-proj", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(true)),
                    false, default(SettingPolicyView), SettingValue.Toggle(true), true, false));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            Assert(!model.DraftEditBueSetting("server-quota", PluginConfigValue.IntegerValue(9)), "ServerAuthority 不进草稿");
            Assert(!model.DraftEditBueSetting("state-proj", PluginConfigValue.BooleanValue(false)), "只读行不进草稿");
            Assert(!model.IsDirty, "不可编项被拒后不脏");
            // 收藏即时、不进草稿、不影响脏。
            Assert(model.ToggleFavorite(feature.Value), "收藏即时");
            Assert(!model.IsDirty, "收藏不使草稿变脏");
        }

        private static void NoBackgroundAutoSaveSurvivesRefreshAndRemount()
        {
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft10");
            editor.Seed(feature, ToggleEntry("notify", true));
            var model = NewModel(editor);
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);
            model.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));

            // Glazier 重挂：Refresh + 重开同条 → 不丢草稿、不弹确认、不写盘。
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);
            Assert(model.HasOpenDetail && model.IsDirty, "重挂不丢草稿");
            Assert(editor.BatchCalls == 0 && editor.ApplyCalls == 0, "重挂/刷新不自动保存");

            // 闲置、退出再进也不写。
            model.OpenDetail(feature.Value);
            Assert(editor.BatchCalls == 0, "无后台自动保存");
        }

        private static void SaveCommittedLifecycleIntentFlag()
        {
            // DEV-V4-09 F2（实机缺陷）：启停意图提交成功=机器事实已变，但面板
            // 目录条目的状态/开关投影建目录时缓存——保存后重渲染沿用旧条目，
            // 开关与状态行回跳旧值（重进面板才见真状态）。报告必须声明「本次
            // 保存提交过生命周期意图」，原生面板据此刷新机器事实后再渲染。
            // 只有「提交成功」才声明：设置-only 保存与被生命周期机拒绝的意图
            // 都不声明（机器事实未变，刷新无依据）。
            var editor = new DraftableSettingsEditor();
            var feature = new FeatureId("io.github.yu80rice.bue.draft12");
            editor.Seed(feature, ToggleEntry("notify", true));
            var toggles = new RecordingToggleHandler();
            var model = NewModel(editor);
            model.FeatureToggleHandler = toggles.Handle;
            model.Refresh(new[] { Feature(feature, "甲", editor) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            model.DraftSetFeatureEnabled(false);
            var report = model.SaveDraft();

            Assert(report.Outcome == DraftSaveOutcome.Success, "启停提交 setup：保存成功");
            Assert(report.CommittedLifecycleIntent, "启停提交成功=报告声明生命周期意图已提交");

            var settingsOnly = new DraftableSettingsEditor();
            var featureB = new FeatureId("io.github.yu80rice.bue.draft13");
            settingsOnly.Seed(featureB, ToggleEntry("notify", true));
            var modelB = NewModel(settingsOnly);
            modelB.Refresh(new[] { Feature(featureB, "乙", settingsOnly) }, new LoadedPluginDescriptor[0]);
            modelB.OpenDetail(featureB.Value);
            modelB.DraftEditBueSetting("notify", PluginConfigValue.BooleanValue(false));
            var reportB = modelB.SaveDraft();
            Assert(reportB.Outcome == DraftSaveOutcome.Success && !reportB.CommittedLifecycleIntent,
                "设置-only 保存不声明生命周期提交（机器事实未变）");

            var rejectedEditor = new DraftableSettingsEditor();
            var featureC = new FeatureId("io.github.yu80rice.bue.draft14");
            rejectedEditor.Seed(featureC, ToggleEntry("notify", true));
            var rejectedToggles = new RecordingToggleHandler { Result = false };
            var modelC = NewModel(rejectedEditor);
            modelC.FeatureToggleHandler = rejectedToggles.Handle;
            modelC.Refresh(new[] { Feature(featureC, "丙", rejectedEditor) }, new LoadedPluginDescriptor[0]);
            modelC.OpenDetail(featureC.Value);
            modelC.DraftSetFeatureEnabled(false);
            var reportC = modelC.SaveDraft();
            Assert(reportC.Outcome == DraftSaveOutcome.PartialFailure && !reportC.CommittedLifecycleIntent,
                "被生命周期机拒绝的意图不声明提交（机器事实未变）");
        }

        // ── helpers ──

        private static ManagementPanelModel NewModel(IBueSettingsEditor editor, IPluginConfigEditor configEditor = null)
        {
            return new ManagementPanelModel(new MemoryStore(), editor, configEditor);
        }

        private static ManagementEntryView entry(ManagementPanelModel model, string stableId)
        {
            var rows = model.GetEntries();
            for (var index = 0; index < rows.Count; index++)
                if (rows[index].StableId == stableId) return rows[index];
            throw new InvalidOperationException("entry not found: " + stableId);
        }

        private static ManagementEntryView entry(ManagementPanelModel model, string stableId, ManagementEntryKind kind)
        {
            var rows = model.GetEntries();
            for (var index = 0; index < rows.Count; index++)
                if (rows[index].StableId == stableId && rows[index].Kind == kind) return rows[index];
            throw new InvalidOperationException("entry not found: " + kind + "/" + stableId);
        }

        private static bool ContainsMessage(DraftSaveReport report, string text)
        {
            if (report == null || report.Messages == null) return false;
            for (var index = 0; index < report.Messages.Count; index++)
                if (string.Equals(report.Messages[index], text, StringComparison.Ordinal)) return true;
            return false;
        }

        private static BueFeatureManagementEntry Feature(FeatureId feature, string name, DraftableSettingsEditor editor)
        {
            return new BueFeatureManagementEntry(feature, name, "1.0.0", FeatureState.Running,
                new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1), editor.GetSnapshot(feature));
        }

        private static SettingEntryView ToggleEntry(string id, bool value)
        {
            return new SettingEntryView(id, SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(value)),
                false, default(SettingPolicyView), SettingValue.Toggle(value), true, true);
        }

        private static SettingEntryView IntegerEntry(string id, int value)
        {
            return new SettingEntryView(id, SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.IntegerValue(value)),
                false, default(SettingPolicyView), SettingValue.IntegerValue(value), true, true);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        // ── fakes ──

        private sealed class MemoryStore : IManagementPanelPreferencesStore
        {
            private ManagementPanelPreferences preferences = ManagementPanelPreferences.Empty;
            internal int SaveCount;
            public ManagementPanelPreferences Load() { return preferences; }
            public void Save(ManagementPanelPreferences value) { preferences = value; SaveCount++; }
        }

        private sealed class RecordingToggleHandler
        {
            internal readonly List<bool> Calls = new List<bool>();
            internal bool Result = true;
            internal bool Handle(FeatureId feature, bool enabled) { Calls.Add(enabled); return Result; }
        }

        private sealed class RecordingPluginEditor : IPluginConfigEditor
        {
            internal int SetCalls;
            internal readonly List<string> WrittenKeys = new List<string>();
            // DEV-V4-08：结构化写入结果——接受并如实申报无重启项。
            public PluginConfigEditResult TrySet(string pluginGuid, string key, PluginConfigValue value)
            {
                SetCalls++;
                WrittenKeys.Add(key);
                return new PluginConfigEditResult(true, false, PluginConfigEditRejection.None);
            }
        }

        // A model-side settings authority: single-truth value store + revision +
        // knobs to inject a stale revision or a validation rejection, and call
        // counters that let the draft tests prove the write never happens early.
        private sealed class DraftableSettingsEditor : IBueSettingsEditor
        {
            private readonly Dictionary<string, List<SettingEntryView>> byFeature = new Dictionary<string, List<SettingEntryView>>(StringComparer.Ordinal);
            private readonly Dictionary<string, uint> revisions = new Dictionary<string, uint>(StringComparer.Ordinal);
            internal int BatchCalls;
            internal int ApplyCalls;
            internal IReadOnlyList<SettingMutation> LastBatchMutations;
            internal uint LastExpectedRevision;
            internal bool RejectNextAsStale;
            internal bool RejectNextInvalid;

            internal void Seed(FeatureId feature, params SettingEntryView[] entries)
            {
                byFeature[feature.Value] = new List<SettingEntryView>(entries);
                revisions[feature.Value] = 0;
            }

            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
            {
                return SnapshotFor(feature);
            }

            // DEV-V4-02: the draft tests keep their no-schema fixture honest —
            // rows fall back to SettingId names (DEV-V4-02's own fixture seeds
            // descriptors in DevV4PanelControlsTests).
            public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
            {
                return new SettingDescriptor[0];
            }

            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                ApplyCalls++;
                return ApplyBatch(feature, expectedRevision, new[] { mutation });
            }

            public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
            {
                BatchCalls++;
                LastBatchMutations = mutations;
                LastExpectedRevision = expectedRevision;
                var revision = revisions[feature.Value];
                if (RejectNextAsStale) { RejectNextAsStale = false; return new SettingChangeResult(false, FrameworkErrorCode.SettingRevisionConflict, revision, SnapshotFor(feature)); }
                if (RejectNextInvalid) { RejectNextInvalid = false; return new SettingChangeResult(false, FrameworkErrorCode.SettingValidationFailed, revision, SnapshotFor(feature)); }
                if (expectedRevision != revision) return new SettingChangeResult(false, FrameworkErrorCode.SettingRevisionConflict, revision, SnapshotFor(feature));
                if (mutations == null || mutations.Count == 0) return new SettingChangeResult(true, FrameworkErrorCode.None, revision, SnapshotFor(feature));
                // atomic: apply to a copy, commit only if every mutation targets an editable row
                var list = byFeature[feature.Value];
                var nextRevision = revision + 1;
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
                revisions[feature.Value] = nextRevision;
                return new SettingChangeResult(true, FrameworkErrorCode.None, nextRevision, SnapshotFor(feature));
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
