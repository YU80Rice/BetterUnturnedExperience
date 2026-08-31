using System;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    internal static class Dev15DTests
    {
        internal static void Run()
        {
            SettingsDefaultToEnabledAndAutoRotate();
            DisabledSettingsUseNativePassThroughAndReenableNextDrag();
            SettingsChangedDuringDragApplyOnNextDragOnly();
            LifecycleExposesNineStatesWithMonotonicRevision();
            IsolationStopsCallbacksAndContinuesCleanupAfterFailure();
            SafeModeCleansCustomUiOnceAndReportsDegradedPresentation();
            MissingSatelliteKeepsCoreAndSettingsAvailable();
            SnapshotIdentityAndRevisionAreFailClosed();
            CleanupFailureNeverPublishesStopped();
            ComponentDisabledPathReturnsNativePassThrough();
        }

        private static void SettingsDefaultToEnabledAndAutoRotate()
        {
            var state = new BetterItemInteractionSettingsState();
            var policy = state.CaptureForDrag();
            Assert(policy.Enabled && policy.AutoRotate, "DEV-15D defaults enable enhancement and auto-rotation");
        }

        private static void DisabledSettingsUseNativePassThroughAndReenableNextDrag()
        {
            var settings = new BetterItemInteractionSettingsState();
            settings.ApplySnapshot(Snapshot(false, true, 1));
            var runtime = new BetterItemInteractionRuntime(settings, new BetterItemInteractionLifecycle());
            runtime.Start(true, true);
            runtime.BeginDrag(1);
            Assert(!runtime.EnhancedDragActive, "disabled feature does not activate enhanced drag");
            runtime.EndDrag();
            settings.ApplySnapshot(Snapshot(true, true, 2));
            runtime.BeginDrag(2);
            Assert(runtime.EnhancedDragActive, "re-enabled feature activates on next drag");
        }

        private static void SettingsChangedDuringDragApplyOnNextDragOnly()
        {
            var settings = new BetterItemInteractionSettingsState();
            var runtime = new BetterItemInteractionRuntime(settings, new BetterItemInteractionLifecycle());
            runtime.Start(true, true);
            runtime.BeginDrag(10);
            Assert(runtime.ActivePolicy.AutoRotate, "drag captures initial auto-rotation policy");
            settings.ApplySnapshot(Snapshot(true, false, 1));
            Assert(runtime.ActivePolicy.AutoRotate, "mid-drag setting change does not mutate active policy");
            runtime.EndDrag();
            runtime.BeginDrag(11);
            Assert(!runtime.ActivePolicy.AutoRotate, "next drag consumes changed setting");
        }

        private static void LifecycleExposesNineStatesWithMonotonicRevision()
        {
            var lifecycle = new BetterItemInteractionLifecycle();
            Assert(lifecycle.State == FeatureState.Discovered, "lifecycle starts discovered");
            var last = lifecycle.StateRevision;
            lifecycle.Start(false, true);
            Assert(lifecycle.State == FeatureState.Incompatible && lifecycle.StateRevision > last, "incompatible transition is visible");
            lifecycle.Start(true, true);
            Assert(lifecycle.State == FeatureState.Incompatible && lifecycle.LastDiagnosticId == "BUE-DEV15D-INVALID-STATE-TRANSITION", "incompatible feature rejects retry");
            lifecycle = new BetterItemInteractionLifecycle();
            lifecycle.Start(true, false);
            Assert(lifecycle.State == FeatureState.Disabled && lifecycle.StateRevision > last, "disabled transition is visible");
            last = lifecycle.StateRevision;
            lifecycle.Start(true, true);
            Assert(lifecycle.State == FeatureState.Running && lifecycle.StateRevision > last, "starting and running transitions are visible");
            lifecycle.BeginIsolation();
            Assert(lifecycle.State == FeatureState.Isolating, "isolating transition is visible");
            lifecycle.CompleteIsolation();
            Assert(lifecycle.State == FeatureState.Isolated, "isolated transition is visible");
            lifecycle.BeginStopping();
            lifecycle.CompleteStopped();
            Assert(lifecycle.State == FeatureState.Stopped, "stopping and stopped transitions are visible");
            Assert(lifecycle.StateRevision > last, "state revision is monotonic");
        }

        private static void IsolationStopsCallbacksAndContinuesCleanupAfterFailure()
        {
            var lifecycle = new BetterItemInteractionLifecycle();
            lifecycle.Start(true, true);
            var runtime = new BetterItemInteractionRuntime(new BetterItemInteractionSettingsState(), lifecycle);
            runtime.BeginDrag(20);
            var first = false;
            var second = false;
            runtime.RegisterCleanup(() => first = true);
            runtime.RegisterCleanup(() => { throw new InvalidOperationException("injected cleanup failure"); });
            runtime.RegisterCleanup(() => second = true);
            runtime.Isolate();
            Assert(!runtime.EnhancedDragActive && lifecycle.State == FeatureState.Isolated, "isolation disables enhancement and exposes isolated state");
            Assert(first && second && runtime.CleanupFailed, "cleanup continues after one component throws");
        }

        private static void SafeModeCleansCustomUiOnceAndReportsDegradedPresentation()
        {
            var lifecycle = new BetterItemInteractionLifecycle();
            lifecycle.Start(true, true);
            var runtime = new BetterItemInteractionRuntime(new BetterItemInteractionSettingsState(), lifecycle);
            var cleanupCount = 0;
            runtime.RegisterCleanup(() => cleanupCount++);
            runtime.EnterSafeMode();
            runtime.EnterSafeMode();
            Assert(cleanupCount == 1 && runtime.IsSafeMode, "safe mode cleanup is idempotent");
            Assert(lifecycle.Presentation.State == FeaturePresentationState.HeadlessOnly, "safe mode is not presented as healthy UI");
        }

        private static void MissingSatelliteKeepsCoreAndSettingsAvailable()
        {
            var lifecycle = new BetterItemInteractionLifecycle();
            lifecycle.Start(true, true);
            lifecycle.SetPresentationAvailable(false, false);
            Assert(lifecycle.State == FeatureState.Running, "missing satellite does not stop core feature");
            Assert(lifecycle.Presentation.State == FeaturePresentationState.PresentationDegraded, "missing satellite projects degraded presentation");
        }

        private static void SnapshotIdentityAndRevisionAreFailClosed()
        {
            var settings = new BetterItemInteractionSettingsState();
            Assert(settings.ApplySnapshot(Snapshot(true, false, 2)), "matching snapshot is accepted");
            Assert(!settings.ApplySnapshot(new FeatureSettingsSnapshot(new FeatureId("wrong"), 1, SettingRevisionScope.ClientPreference, 3,
                SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new SettingEntryView[0])), "wrong feature snapshot is rejected");
            Assert(!settings.ApplySnapshot(Snapshot(false, true, 1)) && settings.Enabled && !settings.AutoRotate,
                "older snapshot cannot roll settings back");
            Assert(!settings.ApplySnapshot(Snapshot(false, true, 2)), "duplicate revision is rejected fail-closed");
        }

        private static void CleanupFailureNeverPublishesStopped()
        {
            var lifecycle = new BetterItemInteractionLifecycle();
            lifecycle.Start(true, true);
            var runtime = new BetterItemInteractionRuntime(new BetterItemInteractionSettingsState(), lifecycle);
            runtime.RegisterCleanup(() => { throw new InvalidOperationException("cleanup"); });
            runtime.Stop();
            Assert(lifecycle.State == FeatureState.Isolated && lifecycle.LastDiagnosticId == "BUE-DEV15D-CLEANUP-INCOMPLETE",
                "cleanup failure remains isolated instead of publishing stopped");
        }

        private static void ComponentDisabledPathReturnsNativePassThrough()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new Program.TestRoot());
            component.ApplySettingsSnapshot(Snapshot(false, true, 3));
            component.OnDragStarted(30);
            var native = new NativeActions();
            var outcome = component.OnDragReleased(new NativeDragAdapterInput(true, 30,
                new ItemGridPosition(8, 0, 0, 0), new ItemPlacementPreview(30, PlacementPreviewState.Candidate,
                    new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None)), native);
            Assert(outcome == NativeDragAdapterOutcome.PassThrough && native.SendCount == 0 && native.StopCount == 0,
                "disabled component leaves native drag untouched");
            var enabledComponent = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            enabledComponent.OnUiInitialized(new Program.TestRoot());
            enabledComponent.OnDragStarted(31);
            var submitted = enabledComponent.OnDragReleased(new NativeDragAdapterInput(true, 31,
                new ItemGridPosition(8, 0, 0, 0), new ItemPlacementPreview(31, PlacementPreviewState.Candidate,
                    new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None)), new NativeActions());
            Assert(submitted == NativeDragAdapterOutcome.Submitted && !enabledComponent.EnhancedDragActive,
                "submitted release clears enhanced drag state");
        }

        private sealed class FixedEvaluator : IPlacementCandidateEvaluator
        {
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                return new ItemPlacementPreview(input.DragGeneration, PlacementPreviewState.Candidate,
                    new ItemGridPosition(input.TargetContainer.Page, 0, 0, 0), 1, 1, PlacementReason.None);
            }
        }

        private sealed class NativeActions : INativeInventoryDragActions
        {
            internal int SendCount;
            internal int StopCount;
            internal int GroundTakeCount;
            public void SendDragItem(ItemGridPosition source, ItemGridPosition target) { SendCount++; }
            public void StopDrag() { StopCount++; }
            public void TakeGroundItem(ItemGridPosition target) { GroundTakeCount++; }
        }

        private static FeatureSettingsSnapshot Snapshot(bool enabled, bool autoRotate, uint revision)
        {
            var feature = BetterItemInteractionSettingsState.Feature;
            return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, revision, SettingSyncState.Ready,
                SettingSnapshotSource.LocalPersistent, new[]
                {
                    new SettingEntryView("Enabled", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(enabled)), false, default(SettingPolicyView), SettingValue.Toggle(enabled), true, true),
                    new SettingEntryView("AutoRotate", SettingAuthority.ClientLocal, new SettingValueOption(true, SettingValue.Toggle(autoRotate)), false, default(SettingPolicyView), SettingValue.Toggle(autoRotate), true, true)
                });
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
