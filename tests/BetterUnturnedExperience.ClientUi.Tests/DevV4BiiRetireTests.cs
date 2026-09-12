using System;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    // DEV-V4-04：BII 的 Enabled 退役（spec「官方 legacy enabled 迁移」——旧字段从
    // schema 与面板退役，生命周期成为唯一开关；AutoRotate 不在迁移登记内，仍是普通设置）。
    // 只断言外显缝：快照行、批量编辑受理面、拖拽门与生命周期投影（Testing Decisions「只测外显行为」）。
    internal static class DevV4BiiRetireTests
    {
        internal static void Run()
        {
            SnapshotCarriesAutoRotateOnly();
            EnabledMutationIsRejected();
            SnapshotIgnoresLegacyEnabledEntry();
            DisabledLifecycleNeverAutoRevivesOnDrag();
        }

        // 退役后快照（面板行投影的源）不再暴露 Enabled 总开关；AutoRotate 原样保留。
        private static void SnapshotCarriesAutoRotateOnly()
        {
            var state = new BetterItemInteractionSettingsState();
            var snapshot = state.GetSnapshot();
            Assert(snapshot.Entries.Count == 1, "BII 快照只剩一行（Enabled 退役）");
            Assert(snapshot.Entries[0].SettingId == "AutoRotate", "唯一保留行=AutoRotate（不登记别名，仍是普通设置）");
        }

        // 退役 Enabled 不进草稿：编辑缝对它显式拒绝；AutoRotate 照常可编辑。
        private static void EnabledMutationIsRejected()
        {
            var state = new BetterItemInteractionSettingsState();
            var editor = new BetterItemInteractionSettingsEditor(state);
            var feature = BetterItemInteractionSettingsState.Feature;
            var rejected = editor.Apply(feature, 0, new SettingMutation("Enabled", SettingValue.Toggle(false)));
            Assert(!rejected.Accepted, "退役 Enabled 的编辑=显式拒绝（不进草稿不落状态）");
            var rotate = editor.Apply(feature, 0, new SettingMutation("AutoRotate", SettingValue.Toggle(false)));
            Assert(rotate.Accepted, "AutoRotate 仍是普通设置（可编辑）");
        }

        // 升级兼容读：旧快照仍被接受，但 Enabled 行不再被消费——旧值不复活成设置，
        // 生命周期是唯一开关（旧值 false 的落点=迁移 adapter 写意图事实，不是这里的开关）。
        private static void SnapshotIgnoresLegacyEnabledEntry()
        {
            var state = new BetterItemInteractionSettingsState();
            var entries = new[]
            {
                new SettingEntryView("Enabled", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(false)), false,
                    default(SettingPolicyView), SettingValue.Toggle(false), true, true),
                new SettingEntryView("AutoRotate", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(true)), false,
                    default(SettingPolicyView), SettingValue.Toggle(true), true, true)
            };
            var legacy = new FeatureSettingsSnapshot(BetterItemInteractionSettingsState.Feature, 1, SettingRevisionScope.ClientPreference, 3,
                SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, entries);
            Assert(state.ApplySnapshot(legacy), "旧快照（含 Enabled 行）仍被接受（升级兼容读）");
            Assert(state.Enabled, "旧 Enabled=false 不再被消费（旧值走迁移，不复活为开关）");
            Assert(state.AutoRotate, "AutoRotate 照常消费");
            Assert(state.Revision == 3, "revision 照常推进");
        }

        // 生命周期停用后拖拽保持原生直通，且不再自动复活——旧世界里 Enabled=true 的
        // 拖拽复活腿会把用户停用悄悄翻回来，退役后必须消失。
        private static void DisabledLifecycleNeverAutoRevivesOnDrag()
        {
            var lifecycle = new BetterItemInteractionLifecycle();
            var runtime = new BetterItemInteractionRuntime(new BetterItemInteractionSettingsState(), lifecycle);
            runtime.Start(true, true);
            lifecycle.Disable();
            Assert(lifecycle.State == FeatureState.Disabled, "setup：生命周期用户停用（迁移/面板路径）");
            runtime.BeginDrag(1);
            Assert(!runtime.EnhancedDragActive, "停用后拖拽=原生直通");
            runtime.EndDrag();
            runtime.BeginDrag(2);
            Assert(lifecycle.State == FeatureState.Disabled && !runtime.EnhancedDragActive,
                "拖拽不再自动复活用户停用的功能（旧 Enabled 复活腿退役）");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("DEV-V4-04 BII retire: " + message);
        }
    }
}
