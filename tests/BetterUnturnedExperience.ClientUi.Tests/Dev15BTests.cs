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
            var input = Input(new TestGrid(8, 6), 20f, 40f, 0f, 0f, 10f, 1f, 0f, 0f, 2, 3, 0.25f, 0.25f, 1);

            presenter.Update(input, new RecordingPreviewSink());

            var candidate = evaluator.Last.Value;
            Assert(Approximately(candidate.CursorGridX, 0.75f) && Approximately(candidate.CursorGridY, 4.75f),
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
            presenter.Update(Input(new TestGrid(8, 6), 20f, 40f, 0f, 0f, 10f, 1f, 0f, 0f, 2, 3, 0.25f, 1.25f, 1), new RecordingPreviewSink());

            var candidate = evaluator.Last.Value;
            Assert(Approximately(candidate.CursorGridX, 1.75f) && Approximately(candidate.CursorGridY, 4.75f),
                "forward rotation maps grab offset as (H - gy, gx); actual=" + candidate.CursorGridX + "," + candidate.CursorGridY);
        }

        private static void AnchorsIconUsingRotatedGrabOffset()
        {
            var evaluator = new FixedEvaluator(new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                new ItemGridPosition(3, 2, 1, 1), 3, 2, PlacementReason.None));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new RecordingPreviewSink();
            presenter.BeginDrag(7);
            presenter.Update(Input(new TestGrid(8, 6), 20f, 40f, 0f, 0f, 10f, 1f, 0f, 0f, 2, 3, 0.25f, 1.25f, 1), sink);

            Assert(Approximately(sink.LastIcon.ScreenX, 17.5f) && Approximately(sink.LastIcon.ScreenY, 47.5f),
                "floating icon anchor consumes the rotated grab offset");
        }

        private static InventoryPreviewInput Input(TestGrid occupancy, float pointerX, float pointerY, float originX, float originY,
            float cellSize, float uiScale, float scrollX, float scrollY, byte width, byte height, float grabX, float grabY, byte rotation = 0)
        {
            return new InventoryPreviewInput(7, new ItemGridPosition(0, 0, 0, 0),
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
    }
}
