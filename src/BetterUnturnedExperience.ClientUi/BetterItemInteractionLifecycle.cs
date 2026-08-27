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
            var nextEnabled = true;
            var nextAutoRotate = true;
            if (snapshot.Entries != null)
            {
                for (var index = 0; index < snapshot.Entries.Count; index++)
                {
                    var entry = snapshot.Entries[index];
                    if (entry.Authority != SettingAuthority.ClientLocal || entry.EffectiveValue.Kind != SettingKind.Toggle) continue;
                    if (string.Equals(entry.SettingId, "Enabled", StringComparison.OrdinalIgnoreCase)) nextEnabled = entry.EffectiveValue.Boolean;
                    else if (string.Equals(entry.SettingId, "AutoRotate", StringComparison.OrdinalIgnoreCase)) nextAutoRotate = entry.EffectiveValue.Boolean;
                }
            }
            enabled = nextEnabled;
            autoRotate = nextAutoRotate;
            revision = snapshot.Revision;
            hasSnapshot = true;
            return true;
        }

        internal FeatureSettingsSnapshot GetSnapshot()
        {
            return new FeatureSettingsSnapshot(Feature, 1, SettingRevisionScope.ClientPreference, revision,
                SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new[]
                {
                    new SettingEntryView("Enabled", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(enabled)), false,
                        default(SettingPolicyView), SettingValue.Toggle(enabled), true, true),
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
            if (!string.Equals(feature.Value, BetterItemInteractionSettingsState.Feature.Value, StringComparison.Ordinal) ||
                (!string.Equals(mutation.SettingId, "Enabled", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(mutation.SettingId, "AutoRotate", StringComparison.OrdinalIgnoreCase)) ||
                mutation.Value.Kind != SettingKind.Toggle)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, state.Revision, state.GetSnapshot());
            }
            var snapshot = state.GetSnapshot();
            if (snapshot.Revision != expectedRevision) return new SettingChangeResult(false, FrameworkErrorCode.SettingRevisionConflict, snapshot.Revision, snapshot);
            var entries = new[]
            {
                new SettingEntryView("Enabled", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(string.Equals(mutation.SettingId, "Enabled", StringComparison.OrdinalIgnoreCase) ? mutation.Value.Boolean : state.Enabled)), false,
                    default(SettingPolicyView), SettingValue.Toggle(string.Equals(mutation.SettingId, "Enabled", StringComparison.OrdinalIgnoreCase) ? mutation.Value.Boolean : state.Enabled), true, true),
                new SettingEntryView("AutoRotate", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(string.Equals(mutation.SettingId, "AutoRotate", StringComparison.OrdinalIgnoreCase) ? mutation.Value.Boolean : state.AutoRotate)), false,
                    default(SettingPolicyView), SettingValue.Toggle(string.Equals(mutation.SettingId, "AutoRotate", StringComparison.OrdinalIgnoreCase) ? mutation.Value.Boolean : state.AutoRotate), true, true)
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
        private readonly List<Action> cleanup = new List<Action>();
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
            if (activePolicy.Enabled && lifecycle.State == FeatureState.Disabled)
            {
                lifecycle.Start(true, true);
            }
            else if (!activePolicy.Enabled && lifecycle.State == FeatureState.Running)
            {
                lifecycle.Disable();
            }
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
            cleanup.Add(action);
        }

        internal void Isolate()
        {
            if (safeMode) return;
            lifecycle.BeginIsolation();
            EndDrag();
            RunCleanupOnce();
            lifecycle.CompleteIsolation(!cleanupFailed);
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
                try { cleanup[index](); }
                catch (Exception) { cleanupFailed = true; }
            }
        }
    }
}
