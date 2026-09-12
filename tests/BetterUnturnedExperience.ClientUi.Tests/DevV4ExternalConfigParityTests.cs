using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    // DEV-V4-08：外部配置同等升级（spec「外部配置同等升级（V4-T7 → DEV-V4-08）」节 + V4-T7 Q68/Q70 裁决）。
    // 本组只经 ManagementPanel 模型 seam 断言外显行为：保存失败三类短中文（结构化 adapter
    // 结果分类 → 插件已卸载 / 配置文件无法写入 / 值不合法，不匹配异常文本、不画堆栈）、
    // 检测到 UPM 仍可编受支持 cfg 且底栏文案逐字（识别标签 ≠ 剥夺能力）。
    // adapter 采集缝（描述 / Cycle 档位 / 类型形状）的判据在 Plugin.Tests 的
    // LoadedPluginCatalogAdapter 组——本组不重复。
    internal static class DevV4ExternalConfigParityTests
    {
        internal static void Run()
        {
            SaveFailureClassifiesThreeShortReasons();
            UpmDetectedStillEditable();
            BoolAndUnsignedCycleTransitions();
            Console.WriteLine("DEV-V4-08 ClientUi tests: PASS");
        }

        // Spec R2 blocking 修复的草稿转移面：bool 档位按解析值匹配（当前值 "True"
        // 必须找得到候选 "true"，左键才真正到下一档而不是回落首档）；ulong 全域值
        // 走无符号载体在草稿里逐位往返、拨回原值不算脏。
        private static void BoolAndUnsignedCycleTransitions()
        {
            var editor = new AcceptedPluginEditor();
            var model = NewModel(new NoSchemaEditor(), editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.boolcycle", new PluginConfigEntryView("Enabled", "Enabled",
                    PluginConfigValueKind.Boolean, PluginConfigValue.BooleanValue(true), false, true, null, null, 4096,
                    null, new[] { "true", "false" })),
            });
            model.OpenDetail("com.example.boolcycle");
            Assert(model.DraftCyclePluginConfig("com.example.boolcycle", "Enabled", 1), "bool 行 Cycle 受理");
            Assert(!model.EffectivePluginConfigValue("Enabled").Boolean, "bool 左键 true→false（按解析值匹配档位）");
            Assert(model.DraftCyclePluginConfig("com.example.boolcycle", "Enabled", -1), "bool 行右键受理");
            Assert(model.EffectivePluginConfigValue("Enabled").Boolean, "bool 右键回 true（到头循环）");
            Assert(!model.IsDirty, "拨回原值不算脏");
            Assert(editor.SetCalls == 0, "Cycle 只进草稿不写盘");

            var model2 = NewModel(new NoSchemaEditor(), new AcceptedPluginEditor());
            model2.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.big", new PluginConfigEntryView("Big", "Big",
                    PluginConfigValueKind.Integer, PluginConfigValue.ULongValue(18446744073709551615UL), false, true, null, null, 4096,
                    null, new[] { "18446744073709551614", "18446744073709551615" })),
            });
            model2.OpenDetail("com.example.big");
            Assert(model2.DraftEditPluginConfig("com.example.big", "Big", "18446744073709551614"), "超 long 值进草稿");
            Assert(model2.EffectivePluginConfigValue("Big").Unsigned64 == 18446744073709551614UL, "无符号载体逐位保留");
            Assert(model2.DraftCyclePluginConfig("com.example.big", "Big", 1), "ulong 行 Cycle 受理");
            Assert(model2.EffectivePluginConfigValue("Big").Unsigned64 == 18446744073709551615UL, "当前档文本匹配→左键到下一档（末档回绕）");
            Assert(model2.DraftEditPluginConfig("com.example.big", "Big", "18446744073709551615"), "拨回原值");
            Assert(!model2.IsDirty, "跨载体同值不算脏");
        }

        // Q70：写入失败按结构化结果分三类短中文；失败项草稿保留、不打成功后的重启
        // 徽章（RequiresRestart 徽章仅针对本次保存成功写入的项，Q67）。
        private static void SaveFailureClassifiesThreeShortReasons()
        {
            // 插件卸载或条目失效 → 插件已卸载
            AssertFailureText(PluginConfigEditRejection.PluginNotFound, "未保存：插件已卸载。", false, "PluginNotFound → 插件已卸载（Q70）");
            AssertFailureText(PluginConfigEditRejection.EntryNotFound, "未保存：插件已卸载。", false, "EntryNotFound（条目失效）→ 插件已卸载（Q70）");
            // 文件/权限/锁定/adapter 写入失败 → 配置文件无法写入
            AssertFailureText(PluginConfigEditRejection.ReadOnly, "未保存：配置文件无法写入。", false, "ReadOnly → 配置文件无法写入（Q70）");
            AssertFailureText(PluginConfigEditRejection.PersistenceFailed, "未保存：配置文件无法写入。", false, "PersistenceFailed → 配置文件无法写入（Q70）");
            // ConfigEntry 校验、转换或值域失败 → 值不合法
            AssertFailureText(PluginConfigEditRejection.UnsupportedType, "未保存：值不合法。", false, "UnsupportedType → 值不合法（Q70）");
            AssertFailureText(PluginConfigEditRejection.InvalidValue, "未保存：值不合法。", false, "InvalidValue → 值不合法（Q70）");
            // 未绑定 adapter（组合根未接）也按 adapter 写入失败投影，不伪造别的原因。
            AssertFailureText(PluginConfigEditRejection.None, "未保存：配置文件无法写入。", true, "无 adapter → 配置文件无法写入（adapter 写入失败兜底）");
            // RequiresRestart 项写入失败：草稿保留 + 失败原因，不打成功后的重启徽章（Q67）。
            AssertFailureBadgeNotLitOnFailure("RequiresRestart 项写入失败不打重启徽章（Q67）");
        }

        private static void AssertFailureText(PluginConfigEditRejection reason, string expectedText, bool withoutEditor, string label)
        {
            var editor = withoutEditor ? null : (IPluginConfigEditor)new RejectionPluginEditor { Reason = reason };
            var report = SaveExternalWithRejection(editor);
            Assert(report != null && report.Outcome == DraftSaveOutcome.PartialFailure, label + "：保存=部分失败");
            Assert(ContainsMessage(report, expectedText), label + "：文案=「" + expectedText + "」逐字");
            Assert(!ContainsMessage(report, "未保存：外部配置写入失败。"), label + "：旧通用文案退役（不匹配异常文本的兜底句不再出现）");
            Assert(report != null && !report.RequiresRestart, label + "：失败不打重启徽章");
        }

        private static void AssertFailureBadgeNotLitOnFailure(string label)
        {
            var editor = new RejectionPluginEditor { Reason = PluginConfigEditRejection.PersistenceFailed };
            var model = NewModel(new NoSchemaEditor(), editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.restart.fail", new PluginConfigEntryView("Threads", "线程数",
                    PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(2), true, true, 1, 16)),
            });
            model.OpenDetail("com.example.restart.fail");
            Assert(model.DraftEditPluginConfig("com.example.restart.fail", "Threads", "8"), label + "：编辑进草稿");
            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.PartialFailure && !report.RequiresRestart, label + "：RequiresRestart 行失败不打顶部徽章");
            Assert(model.IsDirty, label + "：失败草稿保留");
        }

        // Q68：检测到 com.trae.pluginmanager 仍列出、受支持 cfg 按统一草稿/Cycle/校验/保存
        // 编辑；底栏「不修改其状态」=加载与运行状态，逐字保留。
        private static void UpmDetectedStillEditable()
        {
            var editor = new AcceptedPluginEditor();
            var model = NewModel(new NoSchemaEditor(), editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.trae.pluginmanager",
                    new PluginConfigEntryView("Visibility", "Visibility", PluginConfigValueKind.Boolean, PluginConfigValue.BooleanValue(true), false, true),
                    new PluginConfigEntryView("PageSize", "PageSize", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(10), false, true, 1, 50),
                    new PluginConfigEntryView("ThumbSize", "ThumbSize", PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(10), false, true, 1, 100, 4096,
                        null, new[] { "10", "20", "50" })),
            });
            model.SetExternalManagerDetected(true);
            Assert(model.ExternalManagerDetected, "检测到 UPM");
            Assert(model.CompatibilityNotice == "检测到外部插件管理器；BUE 不会重复修改其状态。", "底栏文案=Q68 冻结原文逐字");

            model.OpenDetail("com.trae.pluginmanager");
            var rows = model.GetPluginConfigRows("com.trae.pluginmanager");
            Assert(rows.Count == 3, "UPM 条目仍列出全部配置行");
            Assert(rows[0].ControlKind == PanelSettingControlKind.Toggle, "UPM bool 行=正常 Toggle（不因检测到而只读）");
            Assert(rows[1].ControlKind == PanelSettingControlKind.TextEditor, "UPM 数字行=普通控件");
            Assert(rows[2].ControlKind == PanelSettingControlKind.Cycle, "UPM 声明档位的项=同一 Cycle 控件");

            Assert(model.DraftEditPluginConfig("com.trae.pluginmanager", "PageSize", "20"), "检测到 UPM 仍可编受支持 cfg");
            Assert(model.DraftCyclePluginConfig("com.trae.pluginmanager", "ThumbSize", 1), "UPM 档位项走同一 Cycle 草稿缝");
            Assert(model.EffectivePluginConfigValue("ThumbSize").Integer64 == 20, "Cycle 下一档生效于草稿");
            Assert(editor.SetCalls == 0, "编辑不立刻写 UPM ConfigEntry（同一草稿纪律）");
            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success, "UPM 配置保存走同一草稿保存");
            Assert(editor.SetCalls == 2, "保存才写 UPM ConfigEntry（逐条）");
            Assert(model.IsPluginConfigDirty("PageSize") == false, "成功项不再脏");
        }

        // ── helpers ──

        private static DraftSaveReport SaveExternalWithRejection(IPluginConfigEditor editor)
        {
            var model = NewModel(new NoSchemaEditor(), editor);
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                Plugin("com.example.fail", new PluginConfigEntryView("Count", "Count",
                    PluginConfigValueKind.Integer, PluginConfigValue.IntegerValue(1), false, true, 0, 100)),
            });
            model.OpenDetail("com.example.fail");
            Assert(model.DraftEditPluginConfig("com.example.fail", "Count", "2"), "编辑进草稿");
            return model.SaveDraft();
        }

        private static ManagementPanelModel NewModel(IBueSettingsEditor editor, IPluginConfigEditor configEditor = null)
        {
            return new ManagementPanelModel(new MemoryStore(), editor, configEditor);
        }

        private static LoadedPluginDescriptor Plugin(string guid, params PluginConfigEntryView[] entries)
        {
            return new LoadedPluginDescriptor(guid, guid, "1.0.0", entries);
        }

        private static bool ContainsMessage(DraftSaveReport report, string text)
        {
            if (report == null || report.Messages == null) return false;
            for (var index = 0; index < report.Messages.Count; index++)
                if (string.Equals(report.Messages[index], text, StringComparison.Ordinal)) return true;
            return false;
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

        // 面板 BUE 侧的空编辑器假件——本组全部断言走外部插件分支，BUE 设置源零触碰。
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

        // 结构化拒绝假件：按 Reason 返回结构化失败结果（Q70 分类缝的测试面）。
        private sealed class RejectionPluginEditor : IPluginConfigEditor
        {
            internal PluginConfigEditRejection Reason;
            public PluginConfigEditResult TrySet(string pluginGuid, string key, PluginConfigValue value)
            {
                return new PluginConfigEditResult(false, false, Reason);
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
