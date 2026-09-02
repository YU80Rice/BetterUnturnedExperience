using System;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    internal static class Dev15BTests
    {
        internal static void Run()
        {
            ConvertsScaledScrolledPointerUsingGrabOffset();
            UsesRotatedFootprintCenterForCurrentRotation();
            HidesWhenPointerIsOutsideViewport();
            RendersGreenCandidateAndFloatingIcon();
            RendersRedInvalidFrameWithoutIcon();
            HidesAllVisualsForHiddenPreview();
            RejectsStaleGenerationBeforeEvaluator();
            IgnoresStaleEvaluatorProjection();
            HidesAwaitingProjectionState();
            PreviewHotPathAllocatesZeroBytes();
            AppliesForwardGrabOffsetRotation();
            AnchorsIconUsingRotatedGrabOffset();
            SleekPreviewSinkShowsGreenFrameAndFloatingIcon();
            SleekPreviewSinkUsesNativeTopLevelAnchorAndRotationGeometry();
            SleekPreviewSinkShowsRedFrameAndHidesIconOnInvalid();
            SleekPreviewSinkBindsItemAssetIdentityToVisualIcon();
            BetterItemInteractionUiComponentLifecycleAndDragFlow();
            BetterItemInteractionUiComponentBindsSurfaceContextAndCreatesInput();
            BetterItemInteractionUiComponentRejectsStaleSessionGenerationAndSwitchedContainer();
            BetterItemInteractionUiComponentRearmsDragWhenSurfaceArrivesAfterDragStart();
            BetterItemInteractionUiComponentDestructionAndSafeModeCleansUpVisuals();
            SleekSinkHotPathZeroAllocationTest();
            NativeMouseCoordinatesMatchSleekTopLeftSpace();
            TopLevelIconFallbackRejectsGridLocalPointer();
            GeometryFailureClearsLastPreviewBeforeRelease();
        }

        // GPT watermark: R2 red regression. A pointer sampled from the native
        // grid content must never be reused as a top-level screen coordinate
        // when the native top-level anchor is unavailable.
        private static void TopLevelIconFallbackRejectsGridLocalPointer()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(31, PlacementPreviewState.Candidate,
                new ItemGridPosition(2, 1, 0, 0), 1, 1, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(31);
            var input = new InventoryPreviewInput(31, new ItemGridPosition(0, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 31),
                75f, 50f, new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, 1, 1, 0, true, 0.5f, 0.5f,
                ItemAssetIdentity.FromItemId(363), float.NaN, float.NaN, float.NaN, float.NaN,
                InventoryPointerCoordinateSpace.GridContentLocal, new TestGrid(8, 6));

            presenter.Update(input, sink);

            Assert(sink.FrameCount == 1, "candidate frame still renders when top-level icon anchor is unavailable");
            Assert(sink.IconCount == 0, "grid-local pointer never falls back to a top-level icon coordinate");
        }

        // GPT watermark: DEV-16D-R13-4 red regression. A non-finite viewport
        // must clear the previous Candidate, not merely hide the sink.
        private static void GeometryFailureClearsLastPreviewBeforeRelease()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(32,
                PlacementPreviewState.Candidate, new ItemGridPosition(3, 1, 1, 0),
                1, 1, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(32);
            var valid = Input(new TestGrid(8, 6), 10f, 10f, 0f, 0f, 1f, 1f,
                0f, 0f, 1, 1, 0.5f, 0.5f, generation: 32);
            presenter.Update(valid, sink);
            Assert(presenter.LastPreview.State == PlacementPreviewState.Candidate,
                "valid geometry publishes a Candidate");

            var invalid = new InventoryPreviewInput(32, valid.Source, valid.TargetContainer,
                valid.PointerScreenX, valid.PointerScreenY,
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, float.NaN, 100f),
                valid.CellPixelSize, valid.UiScale, valid.ScrollPixelsX, valid.ScrollPixelsY,
                valid.ItemWidth, valid.ItemHeight, valid.CurrentRotation,
                valid.AllowAutomaticRotation, valid.GrabOffsetX, valid.GrabOffsetY,
                valid.ItemAsset, valid.Occupancy);
            presenter.Update(invalid, sink);
            Assert(presenter.LastPreview.State == PlacementPreviewState.Hidden,
                "geometry failure clears LastPreview before release");
        }

        private static void NativeMouseCoordinatesMatchSleekTopLeftSpace()
        {
            var method = typeof(InventoryGridCoordinateAdapter).GetMethod("ToUiScreenCoordinates", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert(method != null, "native mouse coordinate conversion seam exists");
            var result = (System.ValueTuple<float, float>)method.Invoke(null, new object[] { 100f, 200f, 1000f, 2f });
            Assert(Approximately(result.Item1, 50f) && Approximately(result.Item2, 400f),
                "mouse coordinates invert Y and apply UI scale before Sleek projection");
        }

        private static void ConvertsScaledScrolledPointerUsingGrabOffset()
        {
            var occupancy = new TestGrid(8, 6);
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 0), 2, 3, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            var input = Input(occupancy, 60f, 90f, 10f, 20f, 20f, 1f, 10f, 20f, 2, 3, 0.5f, 1.5f);

            presenter.Update(input, sink);

            Assert(evaluator.Last.HasValue, "coordinate adapter invokes evaluator");
            var candidate = evaluator.Last.Value;
            Assert(Approximately(candidate.CursorGridX, 3.5f) && Approximately(candidate.CursorGridY, 4.5f),
                "screen pointer, scale, scroll and grab offset produce intended item center");
        }

        private static void UsesRotatedFootprintCenterForCurrentRotation()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 1), 3, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            presenter.BeginDrag(7);
            // GPT watermark: R13-rotgrab. Spec §2 defines grabOffsetInFootprint in
            // the CURRENT rotation coordinate space. The native dragPivot is
            // already current-rot, so the grab offset is used as given: a 2x3
            // base item at rot=1 has a 3x2 current footprint, and grabOffset
            // (0.25, 0.25) is a point inside that 3x2 footprint (x in [0,3]).
            // Center = pointerGrid + currentFootprintCenter - grab = (3.25, 4.75).
            var input = Input(new TestGrid(8, 6), 20f, 40f, 0f, 0f, 10f, 1f, 0f, 0f, 2, 3, 0.25f, 0.25f, 1);

            presenter.Update(input, new RecordingPreviewSink());

            var candidate = evaluator.Last.Value;
            Assert(Approximately(candidate.CursorGridX, 3.25f) && Approximately(candidate.CursorGridY, 4.75f),
                "odd current rotation swaps footprint dimensions before center correction");
        }

        private static void HidesWhenPointerIsOutsideViewport()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 0), 2, 3, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            presenter.Update(Input(new TestGrid(8, 6), -1f, 20f, 0f, 0f, 1f, 1f, 0f, 0f, 2, 3, 0.5f, 1.5f), sink);

            Assert(sink.HideCount == 1 && sink.FrameCount == 0 && sink.IconCount == 0, "outside viewport hides all preview visuals");
            Assert(!evaluator.Last.HasValue, "outside viewport does not invoke evaluator");
        }

        private static void RendersGreenCandidateAndFloatingIcon()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 1), 3, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            presenter.Update(Input(new TestGrid(8, 6), 10f, 10f, 0f, 0f, 1f, 1f, 0f, 0f, 2, 3, 0.5f, 1.5f), sink);

            Assert(sink.LastFrame.Kind == PreviewFrameKind.ValidGreen && sink.LastFrame.Width == 3 && sink.LastFrame.Height == 2 && sink.LastFrame.Rotation == 1, "candidate renders green frame from evaluator");
            Assert(sink.LastIcon.Rotation == 1 && sink.IconCount == 1, "candidate renders floating icon with final rotation");
        }

        private static void RendersRedInvalidFrameWithoutIcon()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.LocallyInvalid,
                new ItemGridPosition(3, 2, 1, 0), 2, 3, PlacementReason.Occupied));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            presenter.Update(Input(new TestGrid(8, 6), 10f, 10f, 0f, 0f, 1f, 1f, 0f, 0f, 2, 3, 0.5f, 1.5f), sink);

            Assert(sink.LastFrame.Kind == PreviewFrameKind.InvalidRed && sink.IconCount == 0, "invalid candidate renders only red frame");
        }

        private static void HidesAllVisualsForHiddenPreview()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Hidden,
                new ItemGridPosition(3, 0, 0, 0), 0, 0, PlacementReason.OutsideGrid));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            presenter.Update(Input(new TestGrid(8, 6), 10f, 10f, 0f, 0f, 1f, 1f, 0f, 0f, 2, 3, 0.5f, 1.5f), sink);

            Assert(sink.HideCount == 1 && sink.FrameCount == 0 && sink.IconCount == 0, "hidden evaluator result clears all visuals");
        }

        private static void RejectsStaleGenerationBeforeEvaluator()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 0), 2, 3, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(8);
            presenter.Update(Input(new TestGrid(8, 6), 10f, 10f, 0f, 0f, 1f, 1f, 0f, 0f, 2, 3, 0.5f, 1.5f), sink);

            Assert(sink.HideCount == 1 && !evaluator.Last.HasValue, "stale generation hides before evaluator and rendering");
        }

        private static void PreviewHotPathAllocatesZeroBytes()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 1), 3, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            var input = Input(new TestGrid(8, 6), 10f, 10f, 0f, 0f, 1f, 1f, 0f, 0f, 2, 3, 0.5f, 1.5f);
            presenter.BeginDrag(7);
            for (var warmup = 0; warmup < 100; warmup++) presenter.Update(input, sink);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            AppDomain.MonitoringIsEnabled = true;
            var before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
            for (var sample = 0; sample < 10000; sample++) presenter.Update(input, sink);
            var allocated = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - before;
            Assert(allocated == 0, "preview hot path allocates zero bytes; actual=" + allocated);
        }

        private static void IgnoresStaleEvaluatorProjection()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(6, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 0), 2, 3, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            presenter.Update(Input(new TestGrid(8, 6), 10f, 10f, 0f, 0f, 1f, 1f, 0f, 0f, 2, 3, 0.5f, 1.5f), sink);

            Assert(sink.HideCount == 1 && sink.FrameCount == 0 && sink.IconCount == 0, "stale evaluator projection is hidden");
        }

        private static void HidesAwaitingProjectionState()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.PendingAuthoritativeProjection,
                new ItemGridPosition(3, 2, 1, 0), 2, 3, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            presenter.Update(Input(new TestGrid(8, 6), 10f, 10f, 0f, 0f, 1f, 1f, 0f, 0f, 2, 3, 0.5f, 1.5f), sink);

            Assert(sink.HideCount == 1 && sink.FrameCount == 0 && sink.IconCount == 0, "awaiting projection does not render a red rejection");
        }

        private static void AppliesForwardGrabOffsetRotation()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 1), 3, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            presenter.BeginDrag(7);
            // GPT watermark: R13-rotgrab. Spec §2: grabOffsetInFootprint lives in
            // the CURRENT rotation footprint (the adapter reads the native
            // dragPivot which updatePivot already current-rot-transformed). A 2x3
            // base item at rot=1 has a 3x2 current footprint; grabOffset
            // (0.25, 1.25) is used as given. Center = pointerGrid + center -
            // grab = (3.25, 3.75) — NOT a forward (H - gy, gx) re-rotation from a
            // base frame, which would fail for rotated items (base grab bound).
            presenter.Update(Input(new TestGrid(8, 6), 20f, 40f, 0f, 0f, 10f, 1f, 0f, 0f, 2, 3, 0.25f, 1.25f, 1), new RecordingPreviewSink());

            var candidate = evaluator.Last.Value;
            Assert(Approximately(candidate.CursorGridX, 3.25f) && Approximately(candidate.CursorGridY, 3.75f),
                "current-space grab offset feeds the rotated footprint center; actual=" + candidate.CursorGridX + "," + candidate.CursorGridY);
        }

        private static void AnchorsIconUsingRotatedGrabOffset()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 1), 3, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            // GPT watermark: R13-rotgrab. Grab offset is current-space (spec §2),
            // so at rot=1 (3x2 current footprint) grab=(0.25, 1.25) is used as-is:
            // screen = pointer + (center - grab) * cell = (32.5, 37.5).
            presenter.Update(Input(new TestGrid(8, 6), 20f, 40f, 0f, 0f, 10f, 1f, 0f, 0f, 2, 3, 0.25f, 1.25f, 1), sink);

            Assert(Approximately(sink.LastIcon.ScreenX, 32.5f) && Approximately(sink.LastIcon.ScreenY, 37.5f),
                "floating icon anchor consumes the current-space grab offset");
        }

        private static void SleekPreviewSinkShowsGreenFrameAndFloatingIcon()
        {
            var topLevel = new MockVisualContainer();
            var gridPanel = new MockVisualContainer();
            var sink = new InventoryPreviewVisualSink(topLevel, gridPanel);
            sink.Mount();

            var asset = ItemAssetIdentity.FromItemId(363);
            sink.ShowFrame(new PreviewFrame(PreviewFrameKind.ValidGreen, new ItemGridPosition(2, 3, 4, 1), 2, 3, 50f, PlacementReason.None));
            sink.ShowIcon(new PreviewIcon(150f, 250f, 1, asset));

            Assert(sink.IsFrameVisible, "frame is visible when shown");
            Assert(sink.IsIconVisible, "icon is visible when shown");
            Assert(sink.CurrentFrameColor == PreviewFrameColor.ValidGreen, "frame color is valid green");
            Assert(Approximately(sink.FrameX, 150f) && Approximately(sink.FrameY, 200f), "frame position mapped from grid coordinate");
            Assert(Approximately(sink.FrameWidth, 100f) && Approximately(sink.FrameHeight, 150f), "frame size mapped from item dimensions");
            Assert(Approximately(sink.IconX, 150f) && Approximately(sink.IconY, 250f), "icon position mapped from screen anchor");
            Assert(sink.IconRotation == 1, "icon rotation is preserved");
            Assert(sink.BoundIconAsset == asset, "icon asset identity is bound to visual element");

            sink.Hide();
            Assert(!sink.IsFrameVisible, "hide sets frame visibility to false");
            Assert(!sink.IsIconVisible, "hide sets icon visibility to false");
            Assert(sink.BoundIconAsset == default(ItemAssetIdentity), "hide clears bound icon asset");
            sink.Unmount();
        }

        private static void SleekPreviewSinkShowsRedFrameAndHidesIconOnInvalid()
        {
            var topLevel = new MockVisualContainer();
            var gridPanel = new MockVisualContainer();
            var sink = new InventoryPreviewVisualSink(topLevel, gridPanel);
            sink.Mount();

            sink.ShowFrame(new PreviewFrame(PreviewFrameKind.InvalidRed, new ItemGridPosition(2, 1, 1, 0), 1, 2, 20f, PlacementReason.Occupied));
            sink.HideIcon();

            Assert(sink.IsFrameVisible, "invalid frame is visible");
            Assert(!sink.IsIconVisible, "icon is hidden on invalid placement");
            Assert(sink.CurrentFrameColor == PreviewFrameColor.InvalidRed, "frame color is invalid red");
            Assert(sink.BoundIconAsset == default(ItemAssetIdentity), "hidden icon clears asset");
            sink.Unmount();
        }

        private static void SleekPreviewSinkUsesNativeTopLevelAnchorAndRotationGeometry()
        {
            var topLevel = new MockVisualContainer();
            var gridPanel = new MockVisualContainer();
            var sink = new InventoryPreviewVisualSink(topLevel, gridPanel);
            sink.Mount();

            sink.ShowIcon(new PreviewIcon(0f, 0f, 1, ItemAssetIdentity.FromItemId(363),
                0.25f, 0.5f, -25f, -62.5f, 100f, 150f, true));

            Assert(Approximately(sink.IconScaleX, 0.25f) && Approximately(sink.IconScaleY, 0.5f),
                "native floating icon preserves the PlayerUI top-level scale anchor");
            Assert(Approximately(sink.IconX, -25f) && Approximately(sink.IconY, -62.5f),
                "native floating icon preserves the drag pivot offset");
            Assert(Approximately(sink.IconWidth, 100f) && Approximately(sink.IconHeight, 150f),
                "native floating icon receives its rotated footprint size");
            Assert(sink.IconCanRotate, "native floating icon enables the Glazier rotation path");
            sink.Unmount();
        }

        private static void SleekPreviewSinkBindsItemAssetIdentityToVisualIcon()
        {
            var topLevel = new MockVisualContainer();
            var gridPanel = new MockVisualContainer();
            var sink = new InventoryPreviewVisualSink(topLevel, gridPanel);
            sink.Mount();

            var guid = Guid.NewGuid();
            var asset = ItemAssetIdentity.FromAsset(101, guid, "Items/Weapons/Maplestrike");
            sink.ShowIcon(new PreviewIcon(100f, 200f, 2, asset));

            Assert(sink.IsIconVisible, "icon is visible");
            Assert(sink.BoundIconAsset.ItemId == 101, "item id matches bound asset");
            Assert(sink.BoundIconAsset.AssetGuid == guid, "asset guid matches bound asset");
            Assert(sink.BoundIconAsset.ResourcePath == "Items/Weapons/Maplestrike", "resource path matches bound asset");
            sink.Unmount();
        }

        private static void BetterItemInteractionUiComponentLifecycleAndDragFlow()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(10, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 1, 2, 0), 2, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var component = new BetterItemInteractionUiComponent(presenter, adapter);

            component.OnUiInitialized(new Program.TestRoot());
            Assert(!component.IsInventoryOpen, "inventory not open initially");

            var surface = new MockInventorySurfaceContext(
                new ContainerReference(ContainerKind.PlayerInventory, 3, 100),
                new MockVisualContainer(), new MockVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 800f, 600f),
                50f, 1f, 0f, 0f, new TestGrid(8, 6));

            component.OnInventoryOpened(surface);
            Assert(component.IsInventoryOpen, "inventory marked open");
            Assert(component.CurrentSessionGeneration == 100, "session generation bound from surface");
            Assert(component.PreviewSink != null, "visual sink bound from surface");

            component.OnDragStarted(10, ItemAssetIdentity.FromItemId(363), new ItemGridPosition(3, 0, 0, 0));
            var asset = ItemAssetIdentity.FromItemId(363);
            InventoryPreviewInput input;
            var created = component.TryCreatePreviewInput(10, new ItemGridPosition(3, 0, 0, 0), 100f, 100f, 2, 2, 0, true, 0.5f, 0.5f, asset, out input);
            Assert(created, "TryCreatePreviewInput constructs input from active surface context");
            Assert(input.ItemAsset == asset, "input carries item asset identity");
            Assert(input.TargetContainer.SessionGeneration == 100, "input target container carries surface session generation");

            component.OnDragUpdated(input);
            Assert(component.PreviewSink.IsFrameVisible, "frame visible after drag update");
            Assert(component.PreviewSink.IsIconVisible, "icon visible after drag update");
            Assert(component.PreviewSink.BoundIconAsset == asset, "bound asset visible on icon element");

            var nativeActions = new Program.RecordingNativeDragActions();
            var releaseInput = new NativeDragAdapterInput(true, 10, new ItemGridPosition(3, 0, 0, 0),
                new ItemPlacementPreview(10, PlacementPreviewState.Candidate, new ItemGridPosition(3, 1, 2, 0), 2, 2, PlacementReason.None));
            var outcome = component.OnDragReleased(releaseInput, nativeActions);
            Assert(outcome == NativeDragAdapterOutcome.Submitted, "ordinary candidate submitted on release");
            Assert(!component.PreviewSink.IsFrameVisible, "visuals hidden after release");

            component.OnInventoryClosed();
            Assert(!component.IsInventoryOpen, "inventory closed");
            Assert(component.PreviewSink == null, "visual sink unmounted on inventory close");
            Assert(component.CurrentSessionGeneration == 0, "session generation reset on close");
        }

        private static void BetterItemInteractionUiComponentBindsSurfaceContextAndCreatesInput()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(15, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 3, 0), 1, 1, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var component = new BetterItemInteractionUiComponent(presenter, adapter);

            var surface = new MockInventorySurfaceContext(
                new ContainerReference(ContainerKind.Storage, 7, 202),
                new MockVisualContainer(), new MockVisualContainer(),
                new InventoryGridViewport(50f, 100f, 10, 8, 50f, 100f, 500f, 400f),
                40f, 1.25f, 10f, 20f, new TestGrid(10, 8));

            component.OnInventoryOpened(surface);
            Assert(component.CurrentContainer.Kind == ContainerKind.Storage, "container kind bound");
            Assert(component.CurrentContainer.Page == 7, "container page bound");
            Assert(component.CurrentContainer.SessionGeneration == 202, "container session generation bound");

            var asset = ItemAssetIdentity.FromAsset(519, Guid.NewGuid(), "Items/Bags/Alicepack");
            InventoryPreviewInput input;
            var ok = component.TryCreatePreviewInput(15, new ItemGridPosition(0, 0, 0, 0), 200f, 300f, 1, 1, 0, false, 0.5f, 0.5f, asset, out input);
            Assert(ok, "TryCreatePreviewInput succeeded with surface context");
            Assert(input.CellPixelSize == 40f, "cell pixel size mapped from surface");
            Assert(input.UiScale == 1.25f, "ui scale mapped from surface");
            Assert(input.ScrollPixelsX == 10f && input.ScrollPixelsY == 20f, "scroll pixels mapped from surface");
            Assert(input.ItemAsset == asset, "asset identity carried to input");

            component.OnInventoryClosed();
        }

        private static void BetterItemInteractionUiComponentRejectsStaleSessionGenerationAndSwitchedContainer()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(20, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 1, 1, 0), 2, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var component = new BetterItemInteractionUiComponent(presenter, adapter);

            var surfaceA = new MockInventorySurfaceContext(
                new ContainerReference(ContainerKind.Storage, 3, 301),
                new MockVisualContainer(), new MockVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 800f, 600f),
                50f, 1f, 0f, 0f, new TestGrid(8, 6));

            component.OnInventoryOpened(surfaceA);
            component.OnDragStarted(20, ItemAssetIdentity.FromItemId(363), new ItemGridPosition(3, 0, 0, 0));

            // Stale session generation update should hide visual sink:
            var staleInput = new InventoryPreviewInput(20, new ItemGridPosition(0, 0, 0, 0),
                new ContainerReference(ContainerKind.Storage, 3, 300), // Stale session generation (300 != 301)
                100f, 100f, surfaceA.Viewport, 50f, 1f, 0f, 0f, 2, 2, 0, true, 0.5f, 0.5f, surfaceA.Occupancy);

            component.OnDragUpdated(staleInput);
            Assert(!component.PreviewSink.IsFrameVisible, "stale session generation update is rejected and hidden");

            // Container switch: open surfaceB (new container / new session)
            var surfaceB = new MockInventorySurfaceContext(
                new ContainerReference(ContainerKind.Storage, 7, 302),
                new MockVisualContainer(), new MockVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 800f, 600f),
                50f, 1f, 0f, 0f, new TestGrid(8, 6));

            component.OnInventoryOpened(surfaceB);
            Assert(component.CurrentSessionGeneration == 302, "session generation updated to new container");
            Assert(component.CurrentContainer.Page == 7, "container page updated to new container");

            component.OnInventoryClosed();
        }

        // GPT watermark: DEV-16D R12 red regression. The native
        // updateDraggedItem callback can observe isDragging before the
        // lifecycle poll publishes its first surface. Opening that surface
        // must not end the active presenter drag, or all later updates remain
        // stale and no preview can become visible.
        private static void BetterItemInteractionUiComponentRearmsDragWhenSurfaceArrivesAfterDragStart()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(41, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 1, 0, 0), 1, 1, PlacementReason.None));
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator)),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new Program.TestRoot());
            component.OnDragStarted(41, ItemAssetIdentity.FromItemId(363), new ItemGridPosition(3, 0, 0, 0));

            var surface = new MockInventorySurfaceContext(
                new ContainerReference(ContainerKind.PlayerInventory, 3, 501),
                new MockVisualContainer(), new MockVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, new TestGrid(8, 6));
            component.OnInventoryOpened(surface);

            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(41, new ItemGridPosition(3, 0, 0, 0),
                125f, 125f, 1, 1, 0, true, 0.5f, 0.5f,
                ItemAssetIdentity.FromItemId(363), out input),
                "surface arrival during an active drag creates a preview input");
            component.OnDragUpdated(input);
            Assert(component.PreviewSink != null && component.PreviewSink.IsFrameVisible,
                "surface arrival preserves the active presenter drag and renders a preview");
            component.OnInventoryClosed();
        }

        private static void BetterItemInteractionUiComponentDestructionAndSafeModeCleansUpVisuals()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(25, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 1, 1, 0), 2, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var component = new BetterItemInteractionUiComponent(presenter, adapter);

            var surface = new MockInventorySurfaceContext(
                new ContainerReference(ContainerKind.PlayerInventory, 3, 401),
                new MockVisualContainer(), new MockVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 800f, 600f),
                50f, 1f, 0f, 0f, new TestGrid(8, 6));

            component.OnInventoryOpened(surface);
            Assert(component.PreviewSink != null, "sink active");

            component.OnUiDestroyed();
            Assert(!component.IsInventoryOpen, "inventory marked closed on UI destruction");
            Assert(component.PreviewSink == null, "sink unmounted and destroyed");
            Assert(component.CurrentSurface == null, "surface context detached");
        }

        private static void SleekSinkHotPathZeroAllocationTest()
        {
            var topLevel = new MockVisualContainer();
            var gridPanel = new MockVisualContainer();
            var sink = new InventoryPreviewVisualSink(topLevel, gridPanel);
            sink.Mount();
            var asset = ItemAssetIdentity.FromItemId(363);
            var frame = new PreviewFrame(PreviewFrameKind.ValidGreen, new ItemGridPosition(2, 3, 4, 1), 2, 3, 50f, PlacementReason.None);
            var icon = new PreviewIcon(150f, 250f, 1, asset);

            for (var warmup = 0; warmup < 100; warmup++)
            {
                sink.ShowFrame(frame);
                sink.ShowIcon(icon);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            AppDomain.MonitoringIsEnabled = true;
            var before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
            for (var sample = 0; sample < 10000; sample++)
            {
                sink.ShowFrame(frame);
                sink.ShowIcon(icon);
            }
            var allocated = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - before;
            Assert(allocated == 0, "visual sink hot path allocates zero bytes; actual=" + allocated);
            sink.Unmount();
        }

        private static InventoryPreviewInput Input(TestGrid occupancy, float pointerX, float pointerY, float originX, float originY,
            float cellSize, float uiScale, float scrollX, float scrollY, byte width, byte height, float grabX, float grabY, byte rotation = 0, uint generation = 7)
        {
            return new InventoryPreviewInput(generation, new ItemGridPosition(0, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 9), pointerX, pointerY,
                new InventoryGridViewport(originX, originY, 8, 6, 0, 0, 100, 100),
                cellSize, uiScale, scrollX, scrollY, width, height, rotation, true, grabX, grabY, occupancy);
        }

        private static bool Approximately(float left, float right) { return Math.Abs(left - right) < 0.0001f; }
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private sealed class FixedEvaluator : IPlacementCandidateEvaluator
        {
            private readonly ItemPlacementPreview result;
            internal PlacementCandidateInput? Last { get; private set; }
            internal FixedEvaluator(ItemPlacementPreview result) { this.result = result; }
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input) { Last = input; return result; }
        }

        private sealed class TestGrid : IGridOccupancyView
        {
            internal TestGrid(byte width, byte height) { Width = width; Height = height; }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y) { return false; }
        }

        private sealed class RecordingPreviewSink : IInventoryPreviewSink
        {
            internal int HideCount;
            internal int FrameCount;
            internal int IconCount;
            internal PreviewFrame LastFrame;
            internal PreviewIcon LastIcon;
            public void ShowFrame(PreviewFrame frame) { LastFrame = frame; FrameCount++; }
            public void ShowIcon(PreviewIcon icon) { LastIcon = icon; IconCount++; }
            public void HideIcon() { }
            public void Hide() { HideCount++; }
        }

        private sealed class MockVisualElement : IVisualElement
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

        private sealed class MockVisualContainer : IVisualContainer
        {
            internal IVisualElement LastBox { get; private set; }
            internal IVisualElement LastImage { get; private set; }
            public IVisualElement CreateBox() { return LastBox = new MockVisualElement(); }
            public IVisualElement CreateImage() { return LastImage = new MockVisualElement(); }
            public void AddChild(IVisualElement child) { }
            public void RemoveChild(IVisualElement child) { }
        }

        private sealed class MockInventorySurfaceContext : IInventorySurfaceContext
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

            public MockInventorySurfaceContext(ContainerReference currentContainer, IVisualContainer topLevelContainer,
                IVisualContainer gridPanelContainer, InventoryGridViewport viewport, float cellPixelSize, float uiScale,
                float scrollPixelsX, float scrollPixelsY, IGridOccupancyView occupancy)
            {
                CurrentContainer = currentContainer;
                TopLevelContainer = topLevelContainer;
                GridPanelContainer = gridPanelContainer;
                Viewport = viewport;
                CellPixelSize = cellPixelSize;
                UiScale = uiScale;
                ScrollPixelsX = scrollPixelsX;
                ScrollPixelsY = scrollPixelsY;
                Occupancy = occupancy;
            }
        }
    }
}
