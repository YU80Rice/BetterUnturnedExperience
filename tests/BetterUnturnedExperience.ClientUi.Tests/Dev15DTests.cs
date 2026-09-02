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
            SourcePagePassThroughMatrixIsExplicit();
            StaleSessionMismatchClearsLastPreviewBeforeRelease();
            UnsupportedSourceWithStalePreviewRemainsNativePassThrough();
            RebuildingAnyLivePageEndsCrossPageDragAndPreservesSurvivor();
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
            component.OnDragStarted(30, ItemAssetIdentity.FromItemId(363), new ItemGridPosition(3, 0, 0, 0));
            var native = new NativeActions();
            var outcome = component.OnDragReleased(new NativeDragAdapterInput(true, 30,
                new ItemGridPosition(3, 0, 0, 0), new ItemPlacementPreview(30, PlacementPreviewState.Candidate,
                    new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None)), native);
            Assert(outcome == NativeDragAdapterOutcome.PassThrough && native.SendCount == 0 && native.StopCount == 0,
                "disabled component leaves native drag untouched");
            var enabledComponent = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            enabledComponent.OnUiInitialized(new Program.TestRoot());
            enabledComponent.OnDragStarted(31, ItemAssetIdentity.FromItemId(363), new ItemGridPosition(3, 0, 0, 0));
            var submitted = enabledComponent.OnDragReleased(new NativeDragAdapterInput(true, 31,
                new ItemGridPosition(3, 0, 0, 0), new ItemPlacementPreview(31, PlacementPreviewState.Candidate,
                    new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None)), new NativeActions());
            Assert(submitted == NativeDragAdapterOutcome.Submitted && !enabledComponent.EnhancedDragActive,
                "submitted release clears enhanced drag state");
        }

        // GPT watermark: DEV-16D-R13 red regression (updated for DEV-16F R2).
        // Source decoupling: the source page no longer decides takeover. AREA
        // (ground pickup) and equipment slots are now valid enhanced sources
        // that submit through their correct native ports when targeting an
        // ordinary grid; malformed pages beyond AREA remain pass-through.
        private static void SourcePagePassThroughMatrixIsExplicit()
        {
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var native = new NativeActions();
            var ordinaryTarget = new ItemPlacementPreview(60, PlacementPreviewState.Candidate,
                new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None);

            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 60,
                    new ItemGridPosition(8, 0, 0, 0), ordinaryTarget), native) == NativeDragAdapterOutcome.Submitted,
                "AREA source targeting a grid submits through the ground port");
            Assert(native.GroundTakeCount == 1 && native.SendCount == 0 && native.StopCount == 1,
                "AREA source commits via TakeGroundItem, never sendDragItem");

            native.Reset();
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 61,
                    new ItemGridPosition(0, 0, 0, 0),
                    new ItemPlacementPreview(61, PlacementPreviewState.Candidate,
                        new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None)), native) == NativeDragAdapterOutcome.Submitted,
                "equipment source targeting a grid submits through sendDragItem");
            Assert(native.SendCount == 1 && native.GroundTakeCount == 0 && native.StopCount == 1,
                "equipment source commits via SendDragItem");

            native.Reset();
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 62,
                    new ItemGridPosition(9, 0, 0, 0), ordinaryTarget), native) == NativeDragAdapterOutcome.PassThrough,
                "malformed source page beyond AREA remains native pass-through");
            Assert(native.SendCount == 0 && native.GroundTakeCount == 0 && native.StopCount == 0,
                "malformed source page never invokes enhanced native actions");

            Assert(!BetterItemInteractionUiComponent.IsSupportedEnhancedPage(8),
                "AREA is excluded from the enhanced TARGET surface matrix");
            Assert(BetterItemInteractionUiComponent.IsSupportedEnhancedPage(2) &&
                BetterItemInteractionUiComponent.IsSupportedEnhancedPage(3) &&
                BetterItemInteractionUiComponent.IsSupportedEnhancedPage(4) &&
                BetterItemInteractionUiComponent.IsSupportedEnhancedPage(5) &&
                BetterItemInteractionUiComponent.IsSupportedEnhancedPage(6) &&
                BetterItemInteractionUiComponent.IsSupportedEnhancedPage(7),
                "every player grid page (Hands/Backpack/Vest/Shirt/Pants/Storage) is an enhanced target surface");
            Assert(!BetterItemInteractionUiComponent.IsSupportedEnhancedPage(0) &&
                !BetterItemInteractionUiComponent.IsSupportedEnhancedPage(1),
                "equipment slots are excluded from the enhanced target surface matrix");
        }

        // GPT watermark: DEV-16D-R13 red regression. A visible Candidate must
        // be cleared immediately when the target session changes; release can
        // happen before another pointer poll and must not consume stale state.
        private static void StaleSessionMismatchClearsLastPreviewBeforeRelease()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new Program.TestRoot());
            var surface = new TestSurfaceContext(
                new ContainerReference(ContainerKind.PlayerInventory, 3, 711),
                new TestVisualContainer(), new TestVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, new TestGrid(8, 6));
            component.OnInventoryOpened(surface);
            component.OnDragStarted(711, ItemAssetIdentity.FromItemId(363), new ItemGridPosition(3, 0, 0, 0));
            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(711, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "fresh session creates preview input");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "fresh session publishes a Candidate");

            var stale = new InventoryPreviewInput(711, input.Source,
                new ContainerReference(ContainerKind.PlayerInventory, 3, 710),
                input.PointerScreenX, input.PointerScreenY, input.Viewport, input.CellPixelSize,
                input.UiScale, input.ScrollPixelsX, input.ScrollPixelsY, input.ItemWidth,
                input.ItemHeight, input.CurrentRotation, input.AllowAutomaticRotation,
                input.GrabOffsetX, input.GrabOffsetY, input.ItemAsset, input.Occupancy);
            component.OnDragUpdated(stale);
            Assert(component.LastPreview.State == PlacementPreviewState.Hidden,
                "stale session mismatch clears LastPreview immediately");
            component.OnInventoryClosed();
        }

        // GPT watermark: DEV-16D-R13 red regression (updated for DEV-16F R2).
        // Page support is the first release gate. A malformed source page
        // (beyond AREA) must pass through even when a stale visible preview
        // would otherwise fail the generation check. AREA(8) is now a valid
        // enhanced source, so the stale-source case is tested at page 9.
        private static void UnsupportedSourceWithStalePreviewRemainsNativePassThrough()
        {
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var native = new NativeActions();
            var staleCandidate = new ItemPlacementPreview(700,
                PlacementPreviewState.Candidate,
                new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None);
            var outcome = adapter.HandleRelease(new NativeDragAdapterInput(true, 701,
                new ItemGridPosition(9, 0, 0, 0), staleCandidate), native);
            Assert(outcome == NativeDragAdapterOutcome.PassThrough,
                "unsupported stale source is native pass-through before generation validation");
            Assert(native.SendCount == 0 && native.StopCount == 0 && native.GroundTakeCount == 0,
                "unsupported stale source never invokes enhanced native actions");
        }

        // GPT watermark: DEV-16D-R13-4 red regression. Rebuilding either
        // surface invalidates the whole in-flight drag dependency graph even
        // when the other page remains live.
        private static void RebuildingAnyLivePageEndsCrossPageDragAndPreservesSurvivor()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new Program.TestRoot());
            var backpack = new TestSurfaceContext(
                new ContainerReference(ContainerKind.PlayerInventory, 3, 801),
                new TestVisualContainer(), new TestVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, new TestGrid(8, 6));
            var storage = new TestSurfaceContext(
                new ContainerReference(ContainerKind.Storage, 7, 801),
                new TestVisualContainer(), new TestVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, new TestGrid(8, 6));
            component.OnInventoryOpened(backpack);
            component.OnInventoryOpened(storage);
            component.OnDragStarted(801, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));
            Assert(component.EnhancedDragActive, "cross-page drag is active before rebuild");

            Assert(component.DiscardInventorySurface(3), "source page rebuild is accepted");
            Assert(component.LiveSurfaceCount == 1, "surviving Storage surface remains live");
            Assert(component.CurrentContainer.Page == 7, "surviving Storage surface becomes current");
            Assert(!component.EnhancedDragActive && component.LastPreview.State == PlacementPreviewState.Hidden,
                "page rebuild ends enhanced drag and clears preview immediately");
            component.OnInventoryClosed();
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
            internal void Reset() { SendCount = 0; StopCount = 0; GroundTakeCount = 0; }
        }

        private sealed class TestGrid : IGridOccupancyView
        {
            internal TestGrid(byte width, byte height) { Width = width; Height = height; }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y) { return false; }
        }

        private sealed class TestVisualElement : IVisualElement
        {
            public float PositionScaleX { get; set; }
            public float PositionScaleY { get; set; }
            public float PositionOffsetX { get; set; }
            public float PositionOffsetY { get; set; }
            public float SizeOffsetX { get; set; }
            public float SizeOffsetY { get; set; }
            public byte RotationAngle { get; set; }
            public bool CanRotate { get; set; }
            public bool IsVisible { get; set; }
            public PreviewFrameColor Color { get; set; }
            public ItemAssetIdentity BoundAsset { get; set; }
        }

        private sealed class TestVisualContainer : IVisualContainer
        {
            public IVisualElement CreateBox() { return new TestVisualElement(); }
            public IVisualElement CreateImage() { return new TestVisualElement(); }
            public void AddChild(IVisualElement child) { }
            public void RemoveChild(IVisualElement child) { }
        }

        private sealed class TestSurfaceContext : IInventorySurfaceContext
        {
            public ContainerReference CurrentContainer { get; }
            public IVisualContainer TopLevelContainer { get; }
            public IVisualContainer GridPanelContainer { get; }
            public InventoryGridViewport Viewport { get; }
            public float CellPixelSize { get; }
            public float UiScale { get; }
            public float ScrollPixelsX { get; }
            public float ScrollPixelsY { get; }
            public IGridOccupancyView Occupancy { get; }

            internal TestSurfaceContext(ContainerReference currentContainer, IVisualContainer topLevel,
                IVisualContainer gridPanel, InventoryGridViewport viewport, float cellPixelSize,
                float uiScale, float scrollPixelsX, float scrollPixelsY, IGridOccupancyView occupancy)
            {
                CurrentContainer = currentContainer;
                TopLevelContainer = topLevel;
                GridPanelContainer = gridPanel;
                Viewport = viewport;
                CellPixelSize = cellPixelSize;
                UiScale = uiScale;
                ScrollPixelsX = scrollPixelsX;
                ScrollPixelsY = scrollPixelsY;
                Occupancy = occupancy;
            }
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
