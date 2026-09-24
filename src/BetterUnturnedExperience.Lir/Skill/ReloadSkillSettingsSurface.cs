using System;
using System.Collections;
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

        internal static Func<byte> CurrentLevelProvider;
        internal static Func<uint> CurrentExperienceProvider;

        private static byte CurrentLevel()
        {
            var provider = CurrentLevelProvider;
            if (provider != null) return provider();
            return ReloadSkillLevelMirror.HasConfirmed ? ReloadSkillLevelMirror.ConfirmedLevel : (byte)0;
        }

        private static uint CurrentExperience()
        {
            var provider = CurrentExperienceProvider;
            return provider != null ? provider() : uint.MaxValue;
        }

        internal static string DescriptionForLevel(int level)
        {
            return BuildFallbackProjection(level, uint.MaxValue).DescriptionText;
        }

        internal static IReadOnlyList<string> ChoiceOptions()
        {
            if (CurrentLevelProvider == null && CurrentExperienceProvider == null)
            {
                return new[]
                {
                    OptionMaintain,
                    ReloadSkillPolicy.UpgradeOptionLabel(1),
                    ReloadSkillPolicy.UpgradeOptionLabel(2),
                };
            }
            var options = new List<string> { OptionMaintain };
            var row = BuildFallbackProjection(CurrentLevel(), CurrentExperience());
            if (row.IsClickable && row.TargetLevel > 0)
                options.Add(ReloadSkillPolicy.UpgradeOptionLabel(row.TargetLevel));
            return options;
        }

        internal static string Description(int level)
        {
            return ReloadSkillPolicy.Description(level);
        }

        internal static IReadOnlyList<string> LevelDescriptions()
        {
            return new[]
            {
                ReloadSkillPolicy.Description(0),
                ReloadSkillPolicy.Description(1),
                ReloadSkillPolicy.Description(2),
            };
        }

        /// <summary>降级面与战斗区共享同一行投影，不创建第二等级事实源。</summary>
        internal static ReloadSkillSectionRow BuildFallbackProjection(int level, uint experienceBalance)
        {
            return ReloadSkillSectionModel.BuildRows(level, experienceBalance)[0];
        }

        internal static string FallbackDescriptionForCurrentState()
        {
            var row = BuildFallbackProjection(CurrentLevel(), CurrentExperience());
            return row.Name + " · " + row.LevelText + "\n" + row.DescriptionText + "\n"
                + row.CostText + "\n锁条 " + row.UnlockedCount + "/" + row.LockCount;
        }

        private static string FallbackDescription()
        {
            return FallbackDescriptionForCurrentState();
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
        internal static IReadOnlyList<SettingDescriptor> CreateDynamicDescriptors(FeatureId feature)
        {
            return new DynamicDescriptorList(feature);
        }

        private sealed class DynamicDescriptorList : IReadOnlyList<SettingDescriptor>
        {
            private readonly FeatureId feature;
            internal DynamicDescriptorList(FeatureId feature) { this.feature = feature; }
            public int Count { get { return 1; } }
            public SettingDescriptor this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    return CreateDescriptors(feature)[0];
                }
            }
            public IEnumerator<SettingDescriptor> GetEnumerator()
            {
                yield return this[0];
            }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }

        internal static IReadOnlyList<SettingDescriptor> CreateDescriptors(FeatureId feature)
        {
            var level = CurrentLevel();
            var balance = CurrentExperience();
            var row = BuildFallbackProjection(level, balance);
            var options = ChoiceOptions();
            var allowed = new List<SettingValue>();
            for (var i = 0; i < options.Count; i++)
                allowed.Add(SettingValue.Choice(options[i]));
            var nextText = row.IsClickable && row.TargetLevel > 0
                ? "；可请求 " + ReloadSkillPolicy.UpgradeOptionLabel(row.TargetLevel)
                : "；当前等级或经验不满足升级条件";
            return new[]
            {
                new SettingDescriptor(
                    feature, ReloadSkillPolicy.UpgradeSettingId, ReloadSkillPolicy.SkillSectionTitle + "（降级表面）",
                    row.Name + " · " + row.LevelText + "\n" + row.DescriptionText + "\n" + row.CostText
                        + "\n锁条 " + row.UnlockedCount + "/" + row.LockCount + nextText
                        + "；等级以主机确认为准。",
                    SettingKind.Choice, SettingAuthority.ClientLocal, SettingValue.Choice(OptionMaintain),
                    default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                    allowed, 96, null, 1, 0, null, null),
            };
        }
    }
}
