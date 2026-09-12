using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal readonly struct BetterItemInteractionDragPolicy
    {
        internal bool Enabled { get; }
        internal bool AutoRotate { get; }
        internal uint SettingsRevision { get; }

        internal BetterItemInteractionDragPolicy(bool enabled, bool autoRotate, uint settingsRevision)
        {
            Enabled = enabled;
            AutoRotate = autoRotate;
            SettingsRevision = settingsRevision;
        }
    }

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

        internal BetterItemInteractionDragPolicy CaptureForDrag()
        {
            return new BetterItemInteractionDragPolicy(enabled, autoRotate, revision);
        }

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

        // DEV-V4-02: BII is composition chrome with a hand-built snapshot and
        // NO declared schema — the honest answer is an empty list, so the row
        // projection falls back (display name = SettingId, shape from the
        // effective value's kind). 官方人读文案是 DEV-V4-07 的对照表工作。
        public IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
        {
            return new SettingDescriptor[0];
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

    internal sealed class BetterItemInteractionLifecycle
    {
        private readonly FeatureId feature = BetterItemInteractionSettingsState.Feature;
        private FeatureState state = FeatureState.Discovered;
        private FeaturePresentationState presentationState = FeaturePresentationState.NotApplicable;
        private ulong stateRevision;
        private ulong presentationRevision;
        private string diagnosticId = string.Empty;
        private bool safeMode;
        internal string LastDiagnosticId { get { return diagnosticId; } }

        internal FeatureState State { get { return state; } }
        internal ulong StateRevision { get { return stateRevision; } }
        internal bool SafeMode { get { return safeMode; } }
        internal bool CanRun { get { return state == FeatureState.Running && !safeMode; } }
        internal FeaturePresentationView Presentation
        {
            get { return new FeaturePresentationView(feature, presentationState, diagnosticId, presentationRevision); }
        }

        internal void Start(bool compatible, bool enabled)
        {
            if (safeMode || state == FeatureState.Stopped) return;
            if (state != FeatureState.Discovered && state != FeatureState.Disabled) { RejectTransition(); return; }
            if (!compatible)
            {
                Transition(FeatureState.Incompatible, "BUE-DEV15D-INCOMPATIBLE");
                return;
            }
            if (!enabled)
            {
                Transition(FeatureState.Disabled, "BUE-DEV15D-DISABLED");
                return;
            }
            Transition(FeatureState.Starting, string.Empty);
            Transition(FeatureState.Running, string.Empty);
        }

        internal void Disable()
        {
            if (!safeMode && (state == FeatureState.Running || state == FeatureState.Starting)) Transition(FeatureState.Disabled, "BUE-DEV15D-DISABLED");
            else if (state != FeatureState.Disabled) RejectTransition();
        }

        internal void BeginIsolation()
        {
            if (state == FeatureState.Running || state == FeatureState.Starting || state == FeatureState.Stopping) Transition(FeatureState.Isolating, "BUE-DEV15D-ISOLATING");
            else if (state != FeatureState.Isolated) RejectTransition();
        }

        internal void CompleteIsolation(bool cleanupSucceeded = true)
        {
            if (state == FeatureState.Isolating)
            {
                Transition(FeatureState.Isolated, cleanupSucceeded ? "BUE-DEV15D-ISOLATED" : "BUE-DEV15D-CLEANUP-INCOMPLETE");
            }
        }

        internal void BeginStopping()
        {
            if (!safeMode && (state == FeatureState.Running || state == FeatureState.Disabled || state == FeatureState.Isolated)) Transition(FeatureState.Stopping, "BUE-DEV15D-STOPPING");
            else if (state != FeatureState.Stopping && state != FeatureState.Stopped) RejectTransition();
        }

        internal void CompleteStopped(bool cleanupSucceeded = true)
        {
            if (!safeMode && state == FeatureState.Stopping)
            {
                Transition(cleanupSucceeded ? FeatureState.Stopped : FeatureState.Isolated,
                    cleanupSucceeded ? "BUE-DEV15D-STOPPED" : "BUE-DEV15D-CLEANUP-INCOMPLETE");
            }
        }

        internal void SetPresentationAvailable(bool satelliteAvailable, bool headless)
        {
            if (safeMode) return;
            var next = headless ? FeaturePresentationState.HeadlessOnly : satelliteAvailable ? FeaturePresentationState.Available : FeaturePresentationState.PresentationDegraded;
            if (presentationState == next) return;
            presentationState = next;
            presentationRevision++;
        }

        internal void EnterSafeMode()
        {
            if (safeMode) return;
            safeMode = true;
            if (state != FeatureState.Isolated && state != FeatureState.Stopped)
            {
                state = FeatureState.Isolating;
                stateRevision++;
                Transition(FeatureState.Isolated, "BUE-DEV15D-SAFEMODE");
            }
            BeginIsolation();
            CompleteIsolation();
            presentationState = FeaturePresentationState.HeadlessOnly;
            presentationRevision++;
            diagnosticId = "BUE-DEV15D-SAFEMODE";
        }

        private void RejectTransition()
        {
            diagnosticId = "BUE-DEV15D-INVALID-STATE-TRANSITION";
        }

        private void Transition(FeatureState next, string diagnostic)
        {
            if (state == next) return;
            state = next;
            stateRevision++;
            if (!string.IsNullOrEmpty(diagnostic)) diagnosticId = diagnostic;
        }
    }

    internal sealed class BetterItemInteractionRuntime
    {
        private readonly BetterItemInteractionSettingsState settings;
        private readonly BetterItemInteractionLifecycle lifecycle;
        private readonly List<Func<bool>> cleanup = new List<Func<bool>>();
        private BetterItemInteractionDragPolicy activePolicy;
        private bool enhancedDragActive;
        private bool cleanupFailed;
        private bool safeMode;
        private bool cleanupCompleted;

        internal BetterItemInteractionRuntime(BetterItemInteractionSettingsState settings, BetterItemInteractionLifecycle lifecycle)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        internal BetterItemInteractionSettingsState Settings { get { return settings; } }
        internal BetterItemInteractionLifecycle Lifecycle { get { return lifecycle; } }
        internal BetterItemInteractionDragPolicy ActivePolicy { get { return activePolicy; } }
        internal bool EnhancedDragActive { get { return enhancedDragActive; } }
        internal bool CleanupFailed { get { return cleanupFailed; } }
        internal bool IsSafeMode { get { return safeMode; } }

        internal void Start(bool compatible, bool uiSatelliteAvailable)
        {
            lifecycle.Start(compatible, settings.Enabled);
            lifecycle.SetPresentationAvailable(uiSatelliteAvailable, false);
        }

        internal void BeginDrag(uint dragGeneration)
        {
            activePolicy = settings.CaptureForDrag();
            // DEV-V4-04: the legacy Enabled auto start/disable legs retired —
            // the lifecycle is the ONE switch (a user-disabled feature must
            // not silently revive because a drag happened; the migration and
            // the panel toggle own the transitions now).
            enhancedDragActive = lifecycle.CanRun && activePolicy.Enabled && !safeMode;
        }

        internal void EndDrag()
        {
            enhancedDragActive = false;
            activePolicy = default(BetterItemInteractionDragPolicy);
        }

        internal void RegisterCleanup(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (cleanupCompleted) return;
            cleanup.Add(() =>
            {
                action();
                return true;
            });
        }

        internal void RegisterCleanupResult(Func<bool> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (cleanupCompleted) return;
            cleanup.Add(action);
        }

        internal bool Isolate()
        {
            if (safeMode) return !cleanupFailed;
            lifecycle.BeginIsolation();
            EndDrag();
            RunCleanupOnce();
            lifecycle.CompleteIsolation(!cleanupFailed);
            return !cleanupFailed;
        }

        internal void EnterSafeMode()
        {
            if (safeMode) return;
            safeMode = true;
            EndDrag();
            RunCleanupOnce();
            lifecycle.EnterSafeMode();
        }

        internal void Stop()
        {
            EndDrag();
            lifecycle.BeginStopping();
            RunCleanupOnce();
            lifecycle.CompleteStopped(!cleanupFailed);
        }

        private void RunCleanupOnce()
        {
            if (cleanupCompleted) return;
            cleanupCompleted = true;
            for (var index = cleanup.Count - 1; index >= 0; index--)
            {
                try
                {
                    if (!cleanup[index]()) cleanupFailed = true;
                }
                catch (Exception)
                {
                    cleanupFailed = true;
                }
            }
        }
    }
}
