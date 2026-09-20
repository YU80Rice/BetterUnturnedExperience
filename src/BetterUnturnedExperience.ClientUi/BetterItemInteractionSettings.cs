using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal sealed class BetterItemInteractionSettingsState
    {
        internal static readonly FeatureId Feature = new FeatureId("io.github.yu80rice.bue.better-item-interaction");
        private bool enabled = true;
        private bool autoRotate = true;
        private uint revision;
        private bool hasSnapshot;

        internal bool Enabled { get { return enabled; } }
        internal bool AutoRotate { get { return autoRotate; } }
        internal uint Revision { get { return revision; } }


        internal bool ApplySnapshot(FeatureSettingsSnapshot snapshot)
        {
            if (!string.Equals(snapshot.Feature.Value, Feature.Value, StringComparison.Ordinal) || snapshot.RevisionScope != SettingRevisionScope.ClientPreference ||
                (hasSnapshot && snapshot.Revision <= revision))
            {
                return false;
            }
            // DEV-V4-04: the legacy Enabled entry is retired — a legacy
            // snapshot (upgrades) may still carry the row, but it is no
            // longer consumed; AutoRotate stays a normal setting.
            var nextAutoRotate = true;
            if (snapshot.Entries != null)
            {
                for (var index = 0; index < snapshot.Entries.Count; index++)
                {
                    var entry = snapshot.Entries[index];
                    if (entry.Authority != SettingAuthority.ClientLocal || entry.EffectiveValue.Kind != SettingKind.Toggle) continue;
                    if (string.Equals(entry.SettingId, "AutoRotate", StringComparison.OrdinalIgnoreCase)) nextAutoRotate = entry.EffectiveValue.Boolean;
                }
            }
            autoRotate = nextAutoRotate;
            revision = snapshot.Revision;
            hasSnapshot = true;
            return true;
        }

        internal FeatureSettingsSnapshot GetSnapshot()
        {
            // DEV-V4-04: the Enabled master switch is retired from the panel
            // projection (schema retirement) — AutoRotate is the one row left.
            return new FeatureSettingsSnapshot(Feature, 1, SettingRevisionScope.ClientPreference, revision,
                SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new[]
                {
                    new SettingEntryView("AutoRotate", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(autoRotate)), false,
                        default(SettingPolicyView), SettingValue.Toggle(autoRotate), true, true)
                });
        }
    }

    internal sealed class BetterItemInteractionSettingsEditor : IBueSettingsEditor
    {
        private readonly BetterItemInteractionSettingsState state;
        internal BetterItemInteractionSettingsEditor(BetterItemInteractionSettingsState state) { this.state = state ?? throw new ArgumentNullException(nameof(state)); }
        public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
        {
            return string.Equals(feature.Value, BetterItemInteractionSettingsState.Feature.Value, StringComparison.Ordinal)
                ? state.GetSnapshot()
                : new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0, SettingSyncState.Unavailable,
                    SettingSnapshotSource.SafeDefault, new SettingEntryView[0]);
        }
        public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
        {
            return ApplyBatch(feature, expectedRevision, new[] { mutation });
        }

        // DEV-V4-02 → DEV-V4-07: BII is composition chrome with a hand-built
        // snapshot — the row shape still joins through the SAME descriptor
        // seam, now carrying the Q62 frozen copy (显示名「自动旋转」、描述
        // 「拖入时自动旋转物品以适配空位。」). Exactly ONE descriptor: the
        // legacy Enabled master switch stays retired (DEV-V4-04) and never
        // re-enters the schema.
        public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
        {
            if (!string.Equals(feature.Value, BetterItemInteractionSettingsState.Feature.Value, StringComparison.Ordinal)) return new SettingDescriptor[0];
            return new[]
            {
                new SettingDescriptor(BetterItemInteractionSettingsState.Feature, "AutoRotate", "自动旋转", "拖入时自动旋转物品以适配空位。",
                    SettingKind.Toggle, SettingAuthority.ClientLocal, SettingValue.Toggle(true),
                    default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                    null, 16, null, 1, 0, null, null)
            };
        }

        // DEV-V4-01: the BII composition editor honours the batch seam with
        // the same all-or-nothing contract as SettingsRuntime — every mutation
        // must be one of BII's client toggles and the whole batch lands on
        // a single revision bump, so the draft's "保存配置" can never half-
        // apply. An empty batch never advances the revision. DEV-V4-04: the
        // legacy Enabled master switch is retired — mutations for it fall to
        // the unknown-setting rejection (退役不进草稿); AutoRotate stays
        // editable.
        public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations)
        {
            if (!string.Equals(feature.Value, BetterItemInteractionSettingsState.Feature.Value, StringComparison.Ordinal))
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, state.Revision, state.GetSnapshot());
            var list = mutations == null ? new SettingMutation[0] : System.Linq.Enumerable.ToArray(mutations);
            var nextAutoRotate = state.AutoRotate;
            foreach (var mutation in list)
            {
                if (mutation.Value.Kind != SettingKind.Toggle)
                    return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, state.Revision, state.GetSnapshot());
                if (string.Equals(mutation.SettingId, "AutoRotate", StringComparison.OrdinalIgnoreCase)) nextAutoRotate = mutation.Value.Boolean;
                else return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, state.Revision, state.GetSnapshot());
            }
            var snapshot = state.GetSnapshot();
            if (snapshot.Revision != expectedRevision) return new SettingChangeResult(false, FrameworkErrorCode.SettingRevisionConflict, snapshot.Revision, snapshot);
            if (list.Length == 0) return new SettingChangeResult(true, FrameworkErrorCode.None, snapshot.Revision, snapshot);
            var entries = new[]
            {
                new SettingEntryView("AutoRotate", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(nextAutoRotate)), false,
                    default(SettingPolicyView), SettingValue.Toggle(nextAutoRotate), true, true)
            };
            var next = new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, expectedRevision + 1,
                SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, entries);
            state.ApplySnapshot(next);
            return new SettingChangeResult(true, FrameworkErrorCode.None, next.Revision, next);
        }
    }
}
