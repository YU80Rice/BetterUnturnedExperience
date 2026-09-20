using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Bii
{
    // DEV-V6-04: the drag policy struct rides with its consumers (Runtime/组件);
    // the pre-move producer was BetterItemInteractionSettingsState.CaptureForDrag
    // on the shared instance — the component constructs it from the injected
    // settings snapshot now (same three facts, same semantics).
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

    internal static class BiiPolicyDefaults
    {
        // The frozen BII identity (same literal as the UI settings state's
        // Feature constant — the single source stays in the UI project, this
        // is the snapshot-matching read side).
        internal const string FeatureId = "io.github.yu80rice.bue.better-item-interaction";
    }

    internal sealed class BetterItemInteractionLifecycle
    {
        private readonly FeatureId feature = new FeatureId(BiiPolicyDefaults.FeatureId);
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
        // DEV-V6-04: the settings single-source stayed in the UI project; the
        // runtime reads the CURRENT policy through the injected reader (the
        // pre-move source read BetterItemInteractionSettingsState.CaptureForDrag
        // on the shared instance — same facts, same timing: Start/BeginDrag).
        private readonly Func<BetterItemInteractionDragPolicy> policyReadout;
        private readonly BetterItemInteractionLifecycle lifecycle;
        private readonly List<Func<bool>> cleanup = new List<Func<bool>>();
        private BetterItemInteractionDragPolicy activePolicy;
        private bool enhancedDragActive;
        private bool cleanupFailed;
        private bool safeMode;
        private bool cleanupCompleted;

        internal BetterItemInteractionRuntime(Func<BetterItemInteractionDragPolicy> policyReadout, BetterItemInteractionLifecycle lifecycle)
        {
            this.policyReadout = policyReadout ?? throw new ArgumentNullException(nameof(policyReadout));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        internal BetterItemInteractionLifecycle Lifecycle { get { return lifecycle; } }
        internal BetterItemInteractionDragPolicy ActivePolicy { get { return activePolicy; } }
        internal bool EnhancedDragActive { get { return enhancedDragActive; } }
        internal bool CleanupFailed { get { return cleanupFailed; } }
        internal bool IsSafeMode { get { return safeMode; } }

        internal void Start(bool compatible, bool uiSatelliteAvailable)
        {
            lifecycle.Start(compatible, policyReadout().Enabled);
            lifecycle.SetPresentationAvailable(uiSatelliteAvailable, false);
        }

        internal void BeginDrag(uint dragGeneration)
        {
            activePolicy = policyReadout();
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
