using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    // DEV-V4-07：官方文案、设置中文与 NoOp Choice（spec「官方文案与 NoOp（V4-T6 → DEV-V4-07）」
    // + V4-T6 裁决 Q60–Q64）。只经面板模型投影缝断言外显文案：对照表七句逐字、无表不画不占位、
    // BII AutoRotate 中文显示名+描述、退役 enabled 不在描述符/行（与 04 对拍）。不断言 Glazier
    // 控件树（Testing Decisions「只测外显行为」）；候选纪律=不授候选不加 RELEASES 不授 CaseId。
    internal static class DevV4OfficialCopyTests
    {
        internal static void Run()
        {
            ChromeCopyTableSevenSentencesVerbatim();
            EcosystemEntryWithoutChromeCopyDrawsNoDescription();
            ExternalPluginAndUnknownStableIdDrawNoDescription();
            BiiAutoRotateRowGetsChineseDisplayNameAndDescription();
            BiiRetiredEnabledStaysOutOfPanelRows();
        }

        // Q60：功能级一句话=ClientUi chrome 对照表（不进契约）。七句以 spec 表为准、
        // 全部经模型投影缝（GetFeatureDescription）逐字可定位；「无表不画」组覆盖
        // 未命中=空串（管理面板自身不在目录，无条目可命中）。
        private static void ChromeCopyTableSevenSentencesVerbatim()
        {
            var model = NewModel(new FixtureEditor());
            model.Refresh(new[]
            {
                Feature("io.github.yu80rice.bue.better-item-interaction", "更好的物品交互"),
                Feature("io.github.yu80rice.bue.inventory-tidy", "背包整理"),
                Feature("io.github.yu80rice.bue.in-place-reload", "更好的换弹体验"),
                Feature("io.github.yu80rice.bue.horde-tracker", "更好的尸潮播报"),
                Feature("io.github.yu80rice.bue.network", "BUE 网络模块"),
                Feature("io.github.yu80rice.bue.network.v1compat", "BUE V1 兼容层"),
                Feature("io.github.yu80rice.bue.noop", "NoOpFixture"),
            }, new LoadedPluginDescriptor[0]);

            Assert(model.GetFeatureDescription("io.github.yu80rice.bue.better-item-interaction")
                == "在支持的格子里增强拖入，失败时回到原版操作。", "BII 一句话逐字");
            Assert(model.GetFeatureDescription("io.github.yu80rice.bue.inventory-tidy")
                == "整理背包与装备栏物品；模式和方向在本页设置，背包标题栏点「整理」。", "LIT 一句话逐字（覆盖 Hands/Backpack/Vest/Shirt/Pants，不写「服装栏」）");
            Assert(model.GetFeatureDescription("io.github.yu80rice.bue.in-place-reload")
                == "换弹尽量留在原位；背包整理完成后自动压缩弹药。", "LIR 一句话逐字");
            Assert(model.GetFeatureDescription("io.github.yu80rice.bue.horde-tracker")
                == "由主机追踪尸潮，并在客户端显示播报。", "LHT 一句话逐字");
            Assert(model.GetFeatureDescription("io.github.yu80rice.bue.network")
                == "为 BUE 功能模块提供多人通信通道；可停用，停用不等于卸载。", "Network 一句话逐字（「BUE 功能模块」不缩成「仅官方」）");
            Assert(model.GetFeatureDescription("io.github.yu80rice.bue.network.v1compat")
                == "为仍使用数字频道的旧插件提供兼容接收，不提供新的注册入口。", "v1compat 一句话逐字");
            Assert(model.GetFeatureDescription("io.github.yu80rice.bue.noop")
                == "生态接入样板，用于展示功能描述、Toggle 和 Choice 在面板中的呈现。", "NoOp 一句话逐字");
        }

        // Q60/用户故事 21：无对照表的生态条目不画功能级描述、不出现空白占位——
        // 投影层面就是空串，渲染层据空串跳行。
        private static void EcosystemEntryWithoutChromeCopyDrawsNoDescription()
        {
            var editor = new FixtureEditor();
            var model = NewModel(editor);
            model.Refresh(new[] { Feature("io.example.ecosystem.no-copy", "生态功能") }, new LoadedPluginDescriptor[0]);

            Assert(model.GetFeatureDescription("io.example.ecosystem.no-copy") == string.Empty,
                "无对照表的生态条目=空串（不画不占位，不伪造占位句）");
        }

        // 对照表只覆盖 BUE 功能：外部插件与未知 stableId 都没有功能级描述。
        private static void ExternalPluginAndUnknownStableIdDrawNoDescription()
        {
            var editor = new FixtureEditor();
            var model = NewModel(editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                new LoadedPluginDescriptor("com.example.nocopy", "外部插件", "1.0.0", new PluginConfigEntryView[0])
            });

            Assert(model.GetFeatureDescription("com.example.nocopy") == string.Empty, "外部插件无功能级描述（对照表是 BUE 功能目录）");
            Assert(model.GetFeatureDescription("io.github.yu80rice.bue.not-registered") == string.Empty, "未知 stableId=空串");
        }

        // Q62：BII AutoRotate 显示名「自动旋转」、描述「拖入时自动旋转物品以适配空位。」
        // 经真实 BII 编辑器的描述符联表到达行投影（不再是 SettingId 兜底）；AutoRotate
        // 仍是普通可编辑设置（Toggle 草稿→保存照常）。
        private static void BiiAutoRotateRowGetsChineseDisplayNameAndDescription()
        {
            var state = new BetterItemInteractionSettingsState();
            var editor = new BetterItemInteractionSettingsEditor(state);
            var feature = BetterItemInteractionSettingsState.Feature;
            var model = NewModel(editor);
            model.Refresh(new[] { new BueFeatureManagementEntry(feature, "更好的物品交互", "1.0.0", FeatureState.Running,
                new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1), editor.GetSnapshot(feature)) },
                new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var row = settingRow(model, feature.Value, "AutoRotate");
            Assert(row.DisplayName == "自动旋转", "AutoRotate 显示名=「自动旋转」（Q62 冻结）");
            Assert(row.Description == "拖入时自动旋转物品以适配空位。", "AutoRotate 描述=Q62 冻结原文");
            Assert(row.ControlKind == PanelSettingControlKind.Toggle, "AutoRotate 维持 Toggle 形状");

            Assert(model.DraftEditBueSetting("AutoRotate", PluginConfigValue.BooleanValue(false)), "AutoRotate 仍是普通设置（草稿受理）");
            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success, "AutoRotate 保存照常");
            Assert(!state.AutoRotate, "保存后权威状态如实翻转");
        }

        // 与 DEV-V4-04 对拍：退役 enabled 不因文案票回潮——行投影恰一条 AutoRotate
        //（无 Enabled 行；断言全经面板行投影缝，不直读描述符）。
        private static void BiiRetiredEnabledStaysOutOfPanelRows()
        {
            var state = new BetterItemInteractionSettingsState();
            var editor = new BetterItemInteractionSettingsEditor(state);
            var feature = BetterItemInteractionSettingsState.Feature;
            var model = NewModel(editor);
            model.Refresh(new[] { new BueFeatureManagementEntry(feature, "更好的物品交互", "1.0.0", FeatureState.Running,
                new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1), editor.GetSnapshot(feature)) },
                new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var rows = model.GetSettingRows(feature.Value);
            Assert(rows.Count == 1, "BII 行投影恰一条（Enabled 退役不回潮）");
            Assert(rows[0].SettingId == "AutoRotate", "唯一行=AutoRotate（不登记别名，仍是普通设置）");
            for (var index = 0; index < rows.Count; index++)
                Assert(!string.Equals(rows[index].SettingId, "Enabled", StringComparison.OrdinalIgnoreCase),
                    "退役 Enabled 不作为设置行出现（不区分大小写对拍）");
        }

        // ── helpers ──

        private static ManagementPanelModel NewModel(IBueSettingsEditor editor)
        {
            return new ManagementPanelModel(new MemoryStore(), editor);
        }

        private static BueFeatureManagementEntry Feature(string featureId, string displayName)
        {
            var feature = new FeatureId(featureId);
            return new BueFeatureManagementEntry(feature, displayName, "1.0.0", FeatureState.Running,
                new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1),
                new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0,
                    SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new SettingEntryView[0]));
        }

        private static PanelSettingRowView settingRow(ManagementPanelModel model, string stableId, string settingId)
        {
            var rows = model.GetSettingRows(stableId);
            for (var index = 0; index < rows.Count; index++)
                if (rows[index].SettingId == settingId) return rows[index];
            throw new InvalidOperationException("setting row not found: " + settingId);
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

        // 对照表/无表条目测试的最小权威源假件：快照只读、无 schema、不参与保存。
        private sealed class FixtureEditor : IBueSettingsEditor
        {
            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
            {
                return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0,
                    SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new SettingEntryView[0]);
            }

            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                return ApplyBatch(feature, expectedRevision, new[] { mutation });
            }

            public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, GetSnapshot(feature));
            }

            public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
            {
                return new SettingDescriptor[0];
            }
        }
    }
}
