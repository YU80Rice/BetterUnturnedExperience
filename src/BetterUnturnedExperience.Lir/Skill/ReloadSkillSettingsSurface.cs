using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 表面 B（降级设置页）——「U 菜单接不上则降为该功能设置页：
    /// 同一产品意图的另一种表面」（票面裁决 3）。注册期 binder 探测判定两表面
    /// 至多其一；本面把等级请求做成一条 ClientLocal Choice 行（LIT 行式先例）：
    /// 玩家选「请求升级到 N」→ 面板写本地档 → OnSettingsApplied 把它翻译成
    /// 功能私有的升级请求交给主机权威（与 U 菜单同一道门）→ 请求发完即复位
    /// 「维持」——行不是第二事实源，等级真相恒在主机账/回执镜像。
    /// 纯映射 + 描述符在此；Submit 编排在模块。
    /// </summary>
    internal static class ReloadSkillSettingsSurface
    {
        /// <summary>复位档 = 不发请求（默认值与写回目标同字面，单源）。</summary>
        internal const string OptionMaintain = "维持当前等级（不请求）";

        internal static IReadOnlyList<string> ChoiceOptions()
        {
            return new[]
            {
                OptionMaintain,
                ReloadSkillPolicy.UpgradeOptionLabel(1),
                ReloadSkillPolicy.UpgradeOptionLabel(2),
            };
        }

        /// <summary>档位→目标级（未识别/维持 = 0 不发请求——映射绝不发明目标）。</summary>
        internal static int MapOptionToTargetLevel(string optionText)
        {
            if (string.Equals(optionText, ReloadSkillPolicy.UpgradeOptionLabel(1), StringComparison.Ordinal)) return 1;
            if (string.Equals(optionText, ReloadSkillPolicy.UpgradeOptionLabel(2), StringComparison.Ordinal)) return 2;
            return 0;
        }

        /// <summary>请求发出后的行复位写请求（客户端偏好写回「维持」）。</summary>
        internal static ScopedSettingChangeRequest? BuildResetRequest(FeatureId feature, ulong requestId, uint expectedRevision)
        {
            if (string.IsNullOrEmpty(feature.Value) || !string.Equals(feature.Value, LirRuntime.FeatureIdValue, StringComparison.Ordinal)) return null;
            return new ScopedSettingChangeRequest(requestId, SettingRevisionScope.ClientPreference, expectedRevision,
                new[] { new SettingMutation(ReloadSkillPolicy.UpgradeSettingId, SettingValue.Choice(OptionMaintain)) });
        }

        /// <summary>表面 B 的唯一 descriptor（schema 归功能自持——契约 2.1 facet 先例）。</summary>
        internal static IReadOnlyList<SettingDescriptor> CreateDescriptors(FeatureId feature)
        {
            return new[]
            {
                new SettingDescriptor(
                    feature, ReloadSkillPolicy.UpgradeSettingId, ReloadSkillPolicy.SkillSectionTitle + "（降级表面）", "U 菜单分区接不上时的等级请求行：选档=向主机请求升级（校验经验、扣原版经验），行自动复位为维持。等级以主机确认为准。",
                    SettingKind.Choice, SettingAuthority.ClientLocal, SettingValue.Choice(OptionMaintain),
                    default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                    new[]
                    {
                        SettingValue.Choice(OptionMaintain),
                        SettingValue.Choice(ReloadSkillPolicy.UpgradeOptionLabel(1)),
                        SettingValue.Choice(ReloadSkillPolicy.UpgradeOptionLabel(2)),
                    },
                    96, null, 1, 0, null, null),
            };
        }
    }
}
