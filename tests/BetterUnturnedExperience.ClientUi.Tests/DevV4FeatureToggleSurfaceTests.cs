using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    // DEV-V4-05：功能级启停详情页表面（spec「功能级启停表面」节 + V4-T4 Q44–Q46/Q50 裁决）。
    // 本组只经 ManagementPanel 模型 seam 断言外显行为：有开关 iff 可停止生命周期 seam、
    // 九态中文映射（待启动 ≠ 启动中）、草稿目标与只读状态分离、停用原因分档文案、
    // 隔离原因有值才显示、保存走 03 目标提交缝。不断言 Glazier 控件树内部结构。
    internal static class DevV4FeatureToggleSurfaceTests
    {
        internal static void Run()
        {
            ToggleShownIffStoppableLifecycleSeam();
            SeamOwnershipGatesToggleEvenWhenStateMapped();
            ExternalPluginAndUnknownDetailHaveNoToggle();
            NineStateChineseMapping();
            StopReasonSeparatesUserDisabledFromSafeStopCopy();
            DraftTargetSeparatesFromReadOnlyState();
            PendingHintCopyAnchors();
            DraftToggleRejectedWithoutToggle();
            IsolationReasonShownOnlyWhenIsolatedAndNonEmpty();
            SaveSubmitsTargetThroughLifecycleSeam();
            PresentationStateIsItsOwnLine();
            BenignNetworkIsolationDoesNotReadAsUnexplainedFault();
            GenuineIsolationKeepsHonestFailureFace();
        }

        // Q45：有开关 = 拥有可停止的 BUE 功能生命周期 seam（目录 BUE 功能条目）；
        // Incompatible 与未映射状态不在九态映射表内 = 不可用，无开关。
        private static void ToggleShownIffStoppableLifecycleSeam()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.toggle-seam");
            var model = NewModel();
            model.Refresh(new[]
            {
                FeatureWith(feature, FeatureState.Running),
            }, new LoadedPluginDescriptor[0]);

            var running = model.GetFeatureStatusProjection(feature.Value);
            Assert(running.ShowsEnableToggle, "可停止 seam 的 BUE 功能条目有启用开关");

            var mapped = new[]
            {
                FeatureState.Discovered, FeatureState.Starting, FeatureState.Running,
                FeatureState.Stopping, FeatureState.Isolating, FeatureState.Isolated,
                FeatureState.Disabled, FeatureState.Stopped
            };
            for (var index = 0; index < mapped.Length; index++)
            {
                var entry = FeatureWith(feature, mapped[index]);
                model.Refresh(new[] { entry }, new LoadedPluginDescriptor[0]);
                Assert(model.GetFeatureStatusProjection(feature.Value).ShowsEnableToggle,
                    "生命周期映射态有开关：" + mapped[index]);
            }

            model.Refresh(new[] { FeatureWith(feature, FeatureState.Incompatible) }, new LoadedPluginDescriptor[0]);
            Assert(!model.GetFeatureStatusProjection(feature.Value).ShowsEnableToggle, "Incompatible 无开关");
            model.Refresh(new[] { FeatureWith(feature, (FeatureState)200) }, new LoadedPluginDescriptor[0]);
            Assert(!model.GetFeatureStatusProjection(feature.Value).ShowsEnableToggle, "未映射状态无开关");
        }

        // F1（Round 2）：有开关 iff 拥有可停止 seam——状态映射 ≠ seam 所有权。
        // 机器未跟踪（无状态机记录）的 BUE 条目即使状态落在映射态（组合根
        // Running 兜底）也没有可停止 seam：不开关、不收启停意图；状态行仍按
        // 九态映射如实显示（Q46 状态表与 Q45 seam 判据是两条独立裁决）。
        private static void SeamOwnershipGatesToggleEvenWhenStateMapped()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.seamless-running");
            var model = NewModel();
            model.Refresh(new[] { FeatureWith(feature, FeatureState.Running, hasStoppableLifecycle: false) },
                new LoadedPluginDescriptor[0]);

            var projection = model.GetFeatureStatusProjection(feature.Value);
            Assert(!projection.ShowsEnableToggle, "无机器记录（无可停止 seam）的映射态条目无开关");
            Assert(projection.StateText == "运行中", "状态行仍按九态映射显示（不因无 seam 改写状态表）");

            model.OpenDetail(feature.Value);
            Assert(!model.DraftSetFeatureEnabled(false), "无 seam 条目不接受启停意图");
            Assert(!model.IsDirty, "被拒启停不脏");
        }

        // Q45：外部 BepInEx 插件、管理面板自身、核心 Host/Contracts 无进程级启停；
        // 面板上外部条目（第二目录）明确无开关、无功能状态行。
        private static void ExternalPluginAndUnknownDetailHaveNoToggle()
        {
            var model = NewModel();
            model.Refresh(new BueFeatureManagementEntry[0], new[]
            {
                new LoadedPluginDescriptor("com.example.external", "外部插件", "1.0.0", new PluginConfigEntryView[0])
            });

            var external = model.GetFeatureStatusProjection("com.example.external");
            Assert(!external.ShowsEnableToggle, "外部插件详情无启停开关");
            Assert(external.StateText.Length == 0, "外部插件无功能状态行");
            Assert(external.PresentationText.Length == 0, "外部插件无表现状态行");
            Assert(external.IsolationReason.Length == 0, "外部插件无隔离原因行");

            var unknown = model.GetFeatureStatusProjection("com.example.absent");
            Assert(!unknown.ShowsEnableToggle, "未知条目无开关");
        }

        // Q46/Q50：九态中文映射；待启动（Discovered）≠ 启动中（Starting）；
        // Incompatible 与未映射=不可用。面板不 FeatureState.ToString()。
        private static void NineStateChineseMapping()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.nine-states");
            AssertStateText(feature, FeatureState.Discovered, "待启动");
            AssertStateText(feature, FeatureState.Starting, "启动中");
            AssertStateText(feature, FeatureState.Running, "运行中");
            AssertStateText(feature, FeatureState.Stopping, "停用中");
            AssertStateText(feature, FeatureState.Isolating, "隔离处理中");
            AssertStateText(feature, FeatureState.Isolated, "已隔离");
            AssertStateText(feature, FeatureState.Incompatible, "不可用");
            AssertStateText(feature, (FeatureState)200, "不可用");

            var discovered = AssertStateText(feature, FeatureState.Discovered, "待启动");
            var starting = AssertStateText(feature, FeatureState.Starting, "启动中");
            Assert(!string.Equals(discovered, starting), "待启动 ≠ 启动中（Discovered 不是 Starting）");
        }

        private static void StopReasonSeparatesUserDisabledFromSafeStopCopy()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.stop-reasons");
            AssertStateText(feature, FeatureState.Stopped, "已停用", FeatureStopReason.UserDisabled);
            AssertStateText(feature, FeatureState.Disabled, "已停用", FeatureStopReason.UserDisabled);
            AssertStateText(feature, FeatureState.Stopped, "已停止", FeatureStopReason.PluginStopping);
            AssertStateText(feature, FeatureState.Stopped, "已停止", FeatureStopReason.EnvironmentUnavailable);
            AssertStateText(feature, FeatureState.Disabled, "已停止", FeatureStopReason.None);

            var safe = AssertStateText(feature, FeatureState.Stopped, "已停止", FeatureStopReason.PluginStopping);
            Assert(string.Equals(safe, "已停用") == false, "非 UserDisabled 的停止不伪装成用户停用");
        }

        private static void DraftTargetSeparatesFromReadOnlyState()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.draft-target");
            var model = NewModel();
            model.Refresh(new[] { FeatureWith(feature, FeatureState.Running) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            var before = model.GetFeatureStatusProjection(feature.Value);
            Assert(before.EnableToggleTarget, "运行中条目的开关目标=启用（当前意图基线）");
            Assert(before.PendingEffectText.Length == 0, "目标与现状一致无待生效提示");
            Assert(!before.EnableTogglePending, "未编辑时无待生效标记");

            Assert(model.DraftSetFeatureEnabled(false), "停用目标进草稿");
            var pending = model.GetFeatureStatusProjection(feature.Value);
            Assert(pending.StateText == "运行中", "只读状态行仍投影当前事实（不被草稿改写）");
            Assert(!pending.EnableToggleTarget, "开关目标=草稿停用");
            Assert(pending.EnableTogglePending, "目标与现状不一致标记待生效");
            Assert(pending.PendingEffectText == "运行中，保存后将停用", "停用待生效提示（Q44 原文形）");
            Assert(model.IsDirty, "启停目标差异即脏");

            // 拨回原值=不再待生效、不再脏（V4-T2 按最终值判定）。
            model.DraftSetFeatureEnabled(true);
            var restored = model.GetFeatureStatusProjection(feature.Value);
            Assert(!restored.EnableTogglePending && restored.PendingEffectText.Length == 0, "拨回后无待生效");
            Assert(restored.EnableToggleTarget, "拨回后开关目标回到当前意图");
            Assert(!model.IsDirty, "拨回后不脏");
        }

        // Q44 原文锚：「已隔离，保存后将尝试启用」；停用已停用条=目标与现状一致，
        // 无提示（03 语义：空操作成功是生命周期机解释，面板不预演）。
        private static void PendingHintCopyAnchors()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.pending-isolated");
            var model = NewModel();
            model.Refresh(new[] { FeatureWith(feature, FeatureState.Isolated) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);
            Assert(model.DraftSetFeatureEnabled(true), "隔离条目的启用目标进草稿");
            var isolated = model.GetFeatureStatusProjection(feature.Value);
            Assert(isolated.PendingEffectText == "已隔离，保存后将尝试启用", "隔离恢复提示不承诺一定成功（Q44 原文）");
            model.DraftSetFeatureEnabled(false);
            var baseline = model.GetFeatureStatusProjection(feature.Value);
            Assert(!baseline.EnableTogglePending && baseline.PendingEffectText.Length == 0, "隔离+停用=目标与现状一致无提示");

            var stopped = new FeatureId("io.github.yu80rice.bue.pending-stopped");
            model.Refresh(new[]
            {
                FeatureWith(feature, FeatureState.Isolated),
                FeatureWith(stopped, FeatureState.Stopped, FeatureStopReason.UserDisabled)
            }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(stopped.Value);
            model.DraftSetFeatureEnabled(true);
            var stoppedPending = model.GetFeatureStatusProjection(stopped.Value);
            Assert(stoppedPending.PendingEffectText == "已停用，保存后将启用", "停用条启用提示");
        }

        private static void DraftToggleRejectedWithoutToggle()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.no-toggle");
            var model = NewModel();
            model.Refresh(new[] { FeatureWith(feature, FeatureState.Incompatible) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);

            Assert(!model.DraftSetFeatureEnabled(false), "无开关条目不接受启停目标（模型与表面同门禁）");
            Assert(!model.IsDirty, "被拒启停不脏");

            model.Refresh(new[] { FeatureWith(feature, (FeatureState)200) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);
            Assert(!model.DraftSetFeatureEnabled(true), "未映射状态同样无启停目标");
        }

        private static void IsolationReasonShownOnlyWhenIsolatedAndNonEmpty()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.isolation-reason");
            var model = NewModel();
            model.Refresh(new[] { FeatureWith(feature, FeatureState.Isolated, FeatureStopReason.RuntimeIsolated, "factory-null") }, new LoadedPluginDescriptor[0]);
            Assert(model.GetFeatureStatusProjection(feature.Value).IsolationReason == "factory-null", "隔离原因有值才显示");

            model.Refresh(new[] { FeatureWith(feature, FeatureState.Isolated, FeatureStopReason.RuntimeIsolated, null) }, new LoadedPluginDescriptor[0]);
            Assert(model.GetFeatureStatusProjection(feature.Value).IsolationReason.Length == 0, "隔离原因为空不占位");

            model.Refresh(new[] { FeatureWith(feature, FeatureState.Running, FeatureStopReason.None, "start-diagnostic") }, new LoadedPluginDescriptor[0]);
            Assert(model.GetFeatureStatusProjection(feature.Value).IsolationReason.Length == 0, "非隔离态不显示隔离原因");
        }

        private static void SaveSubmitsTargetThroughLifecycleSeam()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.save-target");
            var toggles = new RecordingToggleHandler();
            var model = NewModel();
            model.FeatureToggleHandler = toggles.Handle;
            model.Refresh(new[] { FeatureWith(feature, FeatureState.Stopped, FeatureStopReason.UserDisabled) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(feature.Value);
            model.DraftSetFeatureEnabled(true);

            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.Success, "启用目标保存成功");
            Assert(toggles.Calls.Count == 1 && toggles.Calls[0],
                "保存把目标交给 03 目标提交缝（FeatureToggleHandler=SetFeatureEnabled），非面板分支");
            Assert(!model.IsDirty && !model.GetFeatureStatusProjection(feature.Value).EnableTogglePending, "成功后草稿目标落定不再待生效");

            // 生命周期机拒绝（03 语义）：意图留草稿、待生效提示仍在。
            var rejected = new FeatureId("io.github.yu80rice.bue.save-rejected");
            var rejecting = new RecordingToggleHandler { Result = false };
            model.FeatureToggleHandler = rejecting.Handle;
            model.Refresh(new[] { FeatureWith(rejected, FeatureState.Stopped, FeatureStopReason.UserDisabled) }, new LoadedPluginDescriptor[0]);
            model.OpenDetail(rejected.Value);
            model.DraftSetFeatureEnabled(true);
            var failed = model.SaveDraft();
            Assert(failed.Outcome == DraftSaveOutcome.PartialFailure, "机拒绝=部分失败");
            Assert(rejecting.Calls.Count == 1 && rejecting.Calls[0], "拒绝前确实提交过目标");
            Assert(model.IsDirty, "被拒目标留草稿");
            Assert(model.GetFeatureStatusProjection(rejected.Value).PendingEffectText == "已停用，保存后将启用", "被拒后待生效提示仍在");
        }

        private static void PresentationStateIsItsOwnLine()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.presentation");
            var model = NewModel();
            model.Refresh(new[]
            {
                new BueFeatureManagementEntry(feature, "表现功能", "1.0.0", FeatureState.Running,
                    new FeaturePresentationView(feature, FeaturePresentationState.PresentationDegraded, string.Empty, 1),
                    EmptySnapshot(feature)),
            }, new LoadedPluginDescriptor[0]);
            var degraded = model.GetFeatureStatusProjection(feature.Value);
            Assert(degraded.PresentationText == "表现降级", "表现降级中文");
            Assert(degraded.StateText == "运行中", "功能状态与表现状态独立投影互不改写");

            AssertPresentation(feature, FeaturePresentationState.Available, "可用");
            AssertPresentation(feature, FeaturePresentationState.HeadlessOnly, "仅主机端");
            AssertPresentation(feature, FeaturePresentationState.Failed, "失败");
            AssertPresentation(feature, FeaturePresentationState.NotApplicable, "不适用");
        }

        // 01：网络模块 Isolated 是已知良性投影（module-start not-started，跨端
        // 整理/压弹仍在线）。面板不得同时呈现「已隔离」+ 无解释「启停失败」——
        // 状态文案须说明可继续通信，启停不得再走无解释故障面。
        private static void BenignNetworkIsolationDoesNotReadAsUnexplainedFault()
        {
            var network = new FeatureId("io.github.yu80rice.bue.network");
            var model = NewModel();
            var rejecting = new RecordingToggleHandler { Result = false };
            model.FeatureToggleHandler = rejecting.Handle;
            model.Refresh(new[]
            {
                FeatureWith(network, FeatureState.Isolated, FeatureStopReason.RuntimeIsolated, "start-result")
            }, new LoadedPluginDescriptor[0]);

            var projection = model.GetFeatureStatusProjection(network.Value);
            Assert(projection.StateText != "已隔离", "网络模块良性隔离不得投影成无解释的「已隔离」");
            Assert(projection.StateText.IndexOf("通信", StringComparison.Ordinal) >= 0,
                "网络模块良性隔离文案须说明可继续通信，实际=「" + projection.StateText + "」");
            Assert(!projection.ShowsEnableToggle, "网络模块良性隔离不画启用开关（避免再点出启停失败）");

            model.OpenDetail(network.Value);
            Assert(!model.DraftSetFeatureEnabled(true), "网络模块良性隔离不接受启用目标");
            Assert(!model.IsDirty, "被拒启停不脏");

            // 即便测试强行走保存路径，也不得再报无解释的「功能启停失败」。
            // 基线=未启用（Isolated 不算当前启用），无 EnableEdit 则 SaveDraft 是无修改。
            var report = model.SaveDraft();
            Assert(report.Outcome == DraftSaveOutcome.NoChanges, "无开关则保存不提交启停");
            Assert(rejecting.Calls.Count == 0, "良性隔离不把启用目标交给生命周期机");
            Assert(!ContainsMessage(report, "未保存：功能启停失败。"), "不得再报无解释启停失败");
        }

        // 01 对照：真正故障功能仍走 Isolated + 启用失败诚实面（Q44 不回退）。
        private static void GenuineIsolationKeepsHonestFailureFace()
        {
            var feature = new FeatureId("io.github.yu80rice.bue.genuine-isolated");
            var model = NewModel();
            var rejecting = new RecordingToggleHandler { Result = false };
            model.FeatureToggleHandler = rejecting.Handle;
            model.Refresh(new[]
            {
                FeatureWith(feature, FeatureState.Isolated, FeatureStopReason.RuntimeIsolated, "factory-null")
            }, new LoadedPluginDescriptor[0]);

            var projection = model.GetFeatureStatusProjection(feature.Value);
            Assert(projection.StateText == "已隔离", "真正故障功能仍投影「已隔离」");
            Assert(projection.ShowsEnableToggle, "真正故障功能仍有启用开关（Q44 诚实失败面）");
            Assert(projection.IsolationReason == "factory-null", "真正故障隔离原因有值才显示");

            model.OpenDetail(feature.Value);
            Assert(model.DraftSetFeatureEnabled(true), "真正故障隔离条目的启用目标进草稿");
            var pending = model.GetFeatureStatusProjection(feature.Value);
            Assert(pending.PendingEffectText == "已隔离，保存后将尝试启用", "Q44 原文锚不回退");

            var failed = model.SaveDraft();
            Assert(failed.Outcome == DraftSaveOutcome.PartialFailure, "机拒绝=部分失败");
            Assert(ContainsMessage(failed, "未保存：功能启停失败。"), "真正故障仍给无承诺的启停失败原因");
            Assert(rejecting.Calls.Count == 1 && rejecting.Calls[0], "拒绝前确实提交过目标");
            Assert(model.IsDirty, "被拒目标留草稿");
        }

        private static bool ContainsMessage(DraftSaveReport report, string expected)
        {
            if (report == null || report.Messages == null) return false;
            for (var index = 0; index < report.Messages.Count; index++)
                if (report.Messages[index] == expected) return true;
            return false;
        }

        // ── helpers ──

        private static string AssertStateText(FeatureId feature, FeatureState state, string expected,
            FeatureStopReason reason = FeatureStopReason.None, string diagnostic = null)
        {
            var model = NewModel();
            model.Refresh(new[] { FeatureWith(feature, state, reason, diagnostic) }, new LoadedPluginDescriptor[0]);
            var projection = model.GetFeatureStatusProjection(feature.Value);
            Assert(projection.StateText == expected, state + "（" + reason + "）应投影为「" + expected + "」，实际=「" + projection.StateText + "」");
            return projection.StateText;
        }

        private static void AssertPresentation(FeatureId feature, FeaturePresentationState presentation, string expected)
        {
            var model = NewModel();
            model.Refresh(new[]
            {
                new BueFeatureManagementEntry(feature, "表现功能", "1.0.0", FeatureState.Running,
                    new FeaturePresentationView(feature, presentation, string.Empty, 1), EmptySnapshot(feature)),
            }, new LoadedPluginDescriptor[0]);
            Assert(model.GetFeatureStatusProjection(feature.Value).PresentationText == expected,
                presentation + " 应投影为「" + expected + "」");
        }

        private static ManagementPanelModel NewModel(IBueSettingsEditor editor = null)
        {
            return new ManagementPanelModel(new MemoryStore(), editor ?? new ToggleSeamEditor());
        }

        private static BueFeatureManagementEntry FeatureWith(FeatureId feature, FeatureState state,
            FeatureStopReason reason = FeatureStopReason.None, string diagnostic = null,
            bool hasStoppableLifecycle = true)
        {
            return new BueFeatureManagementEntry(feature, "状态功能", "1.0.0", state,
                new FeaturePresentationView(feature, FeaturePresentationState.Available, string.Empty, 1),
                EmptySnapshot(feature), reason, diagnostic, hasStoppableLifecycle);
        }

        private static FeatureSettingsSnapshot EmptySnapshot(FeatureId feature)
        {
            return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0,
                SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new SettingEntryView[0]);
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

        private sealed class RecordingToggleHandler
        {
            internal readonly List<bool> Calls = new List<bool>();
            internal bool Result = true;
            internal bool Handle(FeatureId feature, bool enabled) { Calls.Add(enabled); return Result; }
        }

        // 状态投影组不需要真实设置源；快照恒空、命令按缝拒绝。
        private sealed class ToggleSeamEditor : IBueSettingsEditor
        {
            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature) { return EmptySnapshot(feature); }
            public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature) { return new SettingDescriptor[0]; }
            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingUnknown, 0, GetSnapshot(feature));
            }
            public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingUnknown, 0, GetSnapshot(feature));
            }
        }
    }
}
