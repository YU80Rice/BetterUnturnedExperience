using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Placement;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin.Tests
{
    internal static class DevV705PreviewUprightTests
    {
        internal static void Run(bool collectAllFailures = false)
        {
            var failures = new List<string>();
            Group("two readable rotations", failures, collectAllFailures, TwoReadableRotations);
            Group("no readable fit stays red", failures, collectAllFailures, NoReadableFitStaysRed);
            Group("occupied local target does not search remotely", failures, collectAllFailures, OccupiedLocalTargetDoesNotSearchRemotely);
            Group("square and open-middle guards", failures, collectAllFailures, SquareAndOpenMiddleGuards);
            Group("preview ghost policy", failures, collectAllFailures, PreviewGhostPolicy);
            Group("red frame and icon share rotation", failures, collectAllFailures, RedFrameAndIconShareRotation);
            Group("frozen frame colors", failures, collectAllFailures, FrozenFrameColors);
            Console.WriteLine("DEV-V7-05 preview-upright: " + (failures.Count == 0 ? "PASS" : "FAIL"));
            if (failures.Count != 0)
                throw new InvalidOperationException("DEV-V7-05 failures (" + failures.Count + "): " + string.Join(" || ", failures));
        }

        private static void Group(string name, List<string> failures, bool collect, Action body)
        {
            try { body(); }
            catch (Exception error) when (collect) { failures.Add("[" + name + "] " + error.Message); }
        }

        private static void TwoReadableRotations()
        {
            var evaluator = new PlacementCandidateEvaluator();
            var occupancy = new Grid(3, 3);
            occupancy.Fill(1, 0, 2, 3);
            var source = new ItemGridPosition(3, 0, 2, 0);
            var target = new ContainerReference(ContainerKind.PlayerInventory, 3, 2);
            var preview = evaluator.Evaluate(new PlacementCandidateInput(1, source, target,
                0.4f, 1.5f, 1, 3, 1, true, occupancy));

            Assert(preview.State == PlacementPreviewState.Candidate, "倒置拉回场景必须仍是可放候选");
            Assert(preview.Width == 1 && preview.Height == 3, "横向当前姿态失败时必须回到竖 footprint");
            Assert(preview.Candidate.Rotation == 0, "自动旋转不得把 rot=1 再加到倒置 rot=2");
            Assert(preview.Candidate.Rotation <= 1, "自动旋转候选不得包含文字倒置姿态");
            Assert((preview.Candidate.Rotation & 1) == (preview.Width > preview.Height ? 1 : 0),
                "最终旋转的奇偶性必须与候选 footprint 一致");

            var nonZeroBaseline = evaluator.Evaluate(new PlacementCandidateInput(6,
                new ItemGridPosition(3, 0, 2, 2), target, 0.4f, 1.5f,
                1, 3, 3, true, occupancy));
            Assert(nonZeroBaseline.State == PlacementPreviewState.Candidate &&
                nonZeroBaseline.Candidate.Rotation == 0 &&
                nonZeroBaseline.Width == 1 && nonZeroBaseline.Height == 3,
                "非零历史 rot 也必须归一到绝对 row=0 的可读竖正向");

            var nonZeroCurrent = evaluator.Evaluate(new PlacementCandidateInput(7,
                new ItemGridPosition(3, 0, 2, 1), target, 2.6f, 1.5f,
                1, 3, 2, true, new Grid(6, 3)));
            Assert(nonZeroCurrent.State == PlacementPreviewState.Candidate &&
                nonZeroCurrent.Candidate.Rotation <= 1 &&
                nonZeroCurrent.Candidate.Rotation == 0 &&
                nonZeroCurrent.Width == 1 && nonZeroCurrent.Height == 3,
                "当前 rot=2 在自动预览中不得作为可读姿态保留");

            var manualInverted = evaluator.Evaluate(new PlacementCandidateInput(8,
                new ItemGridPosition(3, 0, 2, 2), target, 2.6f, 1.5f,
                1, 3, 2, false, new Grid(6, 3)));
            Assert(manualInverted.State == PlacementPreviewState.Candidate &&
                manualInverted.Candidate.Rotation == 2 &&
                manualInverted.Width == 1 && manualInverted.Height == 3,
                "自动旋转关闭时必须保留玩家手动 rot=2，不得静默拉回 row=0");
            var narrow = new Grid(1, 3);
            var narrowPreview = evaluator.Evaluate(new PlacementCandidateInput(5,
                new ItemGridPosition(3, 0, 0, 0), target, 0.4f, 1.5f,
                1, 3, 2, true, narrow));
            Assert(narrowPreview.State == PlacementPreviewState.Candidate && narrowPreview.Candidate.Rotation == 0,
                "替代正向 footprint 超出容器时，倒置竖姿态仍须在边缘带拉回可读竖姿态");
        }

        private static void NoReadableFitStaysRed()
        {
            var evaluator = new PlacementCandidateEvaluator();
            var blocked = new Grid(3, 3);
            blocked.Fill(0, 0, 3, 3);
            var preview = evaluator.Evaluate(new PlacementCandidateInput(4,
                new ItemGridPosition(3, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 4),
                1.5f, 1.5f, 1, 3, 2, true, blocked));
            Assert(preview.State == PlacementPreviewState.LocallyInvalid,
                "两个可读正向都无法放下时必须保留红框状态");
            Assert(preview.Reason == PlacementReason.Occupied,
                "两个可读正向都无法放下时必须报告占用原因而不是伪造成功");
        }

        private static void OccupiedLocalTargetDoesNotSearchRemotely()
        {
            var evaluator = new PlacementCandidateEvaluator();
            var occupancy = new Grid(6, 3);
            occupancy.Fill(0, 0, 1, 3);
            var preview = evaluator.Evaluate(new PlacementCandidateInput(8,
                new ItemGridPosition(3, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 8),
                0.5f, 1.5f, 1, 3, 0, true, occupancy));

            Assert(preview.State == PlacementPreviewState.LocallyInvalid,
                "occupied local target must remain red even when a distant free cell exists");
            Assert(preview.Reason == PlacementReason.Occupied,
                "occupied local target must report Occupied instead of silently searching elsewhere");
            Assert(preview.Candidate.X == 0 && preview.Candidate.Y == 0,
                "invalid feedback must stay anchored to the local projected target");
        }

        private static void SquareAndOpenMiddleGuards()
        {
            var evaluator = new PlacementCandidateEvaluator();
            var open = new Grid(6, 3);
            var target = new ContainerReference(ContainerKind.Storage, 7, 2);
            var openPreview = evaluator.Evaluate(new PlacementCandidateInput(2,
                new ItemGridPosition(7, 0, 2, 1), target, 2.6f, 1.5f,
                1, 3, 1, true, open));
            Assert(openPreview.State == PlacementPreviewState.Candidate && openPreview.Candidate.Rotation == 1,
                "开阔中部必须保持当前可读正向，不能自行蠕动");

            var cornerObstacle = new Grid(6, 3);
            cornerObstacle.Fill(1, 2, 2, 1);
            var cornerPreview = evaluator.Evaluate(new PlacementCandidateInput(7,
                new ItemGridPosition(7, 0, 2, 0), target, 0.4f, 2.5f,
                1, 3, 0, true, cornerObstacle));
            Assert(cornerPreview.State == PlacementPreviewState.Candidate &&
                cornerPreview.Candidate.Rotation == 0 &&
                cornerPreview.Width == 1 && cornerPreview.Height == 3,
                "角落感应带与障碍边界重叠时必须优先保持进入正向");

            var square = evaluator.Evaluate(new PlacementCandidateInput(3,
                new ItemGridPosition(3, 0, 0, 0), new ContainerReference(ContainerKind.PlayerInventory, 3, 3),
                1.5f, 1.5f, 2, 2, 0, true, new Grid(4, 4)));
            Assert(square.State == PlacementPreviewState.Candidate && square.Candidate.Rotation == 0,
                "正方形物品不得自动旋转");

            var squareInverted = evaluator.Evaluate(new PlacementCandidateInput(10,
                new ItemGridPosition(3, 0, 0, 2), target, 2.6f, 1.5f,
                2, 2, 2, true, new Grid(6, 3)));
            Assert(squareInverted.State == PlacementPreviewState.Candidate &&
                squareInverted.Candidate.Rotation == 2 &&
                squareInverted.Width == 2 && squareInverted.Height == 2,
                "自动模式下正方形当前 rot=2 必须保留手动姿态和 footprint");

            var squareInvertedThree = evaluator.Evaluate(new PlacementCandidateInput(11,
                new ItemGridPosition(3, 0, 0, 3), target, 2.6f, 1.5f,
                2, 2, 3, true, new Grid(6, 3)));
            Assert(squareInvertedThree.State == PlacementPreviewState.Candidate &&
                squareInvertedThree.Candidate.Rotation == 3 &&
                squareInvertedThree.Width == 2 && squareInvertedThree.Height == 2,
                "自动模式下正方形当前 rot=3 必须保留手动姿态和 footprint");

            var squareBlocked = new Grid(6, 3);
            squareBlocked.Fill(2, 1, 2, 2);
            var squareInvalid = evaluator.Evaluate(new PlacementCandidateInput(12,
                new ItemGridPosition(3, 0, 0, 2), target, 2.6f, 1.5f,
                2, 2, 2, true, squareBlocked));
            Assert(squareInvalid.State == PlacementPreviewState.LocallyInvalid &&
                squareInvalid.Candidate.Rotation == 2 &&
                squareInvalid.Width == 2 && squareInvalid.Height == 2,
                "正方形占用反馈也必须保留手动 rot=2 和 footprint");
        }

        private static void RedFrameAndIconShareRotation()
        {
            var evaluator = new FixedPreviewEvaluator(new ItemPlacementPreview(9,
                PlacementPreviewState.LocallyInvalid,
                new ItemGridPosition(3, 1, 2, 0), 1, 3, PlacementReason.Occupied));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new PreviewSink();
            presenter.BeginDrag(9);
            presenter.Update(new InventoryPreviewInput(9,
                new ItemGridPosition(3, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 9),
                75f, 125f, new InventoryGridViewport(0f, 0f, 5, 7, 0f, 0f, 250f, 350f),
                50f, 1f, 0f, 0f, 1, 3, 0, true, 0.5f, 1.5f,
                ItemAssetIdentity.FromItemId(363), 0.25f, 0.5f, -25f, -62.5f,
                new Grid(5, 7)), sink);
            Assert(sink.LastFrame.Kind == PreviewFrameKind.InvalidRed && sink.IconCount == 1,
                "红框状态必须仍显示同一候选图标");
            Assert(sink.LastIcon.Rotation == sink.LastFrame.Rotation,
                "红框与图标必须共享 Candidate.Rotation");
        }

        private static void FrozenFrameColors()
        {
            var green = UnturnedVisualElement.PreviewFrameRgba(PreviewFrameColor.ValidGreen);
            var red = UnturnedVisualElement.PreviewFrameRgba(PreviewFrameColor.InvalidRed);
            Assert(Approximately(green.r, 0.2f) && Approximately(green.g, 1f) &&
                Approximately(green.b, 0.3f) && Approximately(green.a, 0.85f),
                "valid frame uses frozen green RGBA");
            Assert(Approximately(red.r, 1f) && Approximately(red.g, 0.25f) &&
                Approximately(red.b, 0.2f) && Approximately(red.a, 0.85f),
                "invalid frame uses frozen red RGBA");
        }

        private static void PreviewGhostPolicy()
        {
            Assert(InventoryDragPreviewAdapter.ShouldSuppressNativeDragGhost(
                isDragging: true, enhancedDragActive: true, targetGridOwned: true,
                previewState: PlacementPreviewState.Candidate),
                "BUE 接管的可放预览必须压制原版 dragItem 幽灵");
            Assert(InventoryDragPreviewAdapter.ShouldSuppressNativeDragGhost(
                isDragging: true, enhancedDragActive: true, targetGridOwned: true,
                previewState: PlacementPreviewState.LocallyInvalid),
                "BUE 接管的红框预览必须压制原版 dragItem 幽灵");
            Assert(!InventoryDragPreviewAdapter.ShouldSuppressNativeDragGhost(
                isDragging: true, enhancedDragActive: true, targetGridOwned: false,
                previewState: PlacementPreviewState.Candidate),
                "非 BUE 目标网格必须保留原版 dragItem 幽灵");
            Assert(!InventoryDragPreviewAdapter.ShouldSuppressNativeDragGhost(
                isDragging: false, enhancedDragActive: true, targetGridOwned: true,
                previewState: PlacementPreviewState.Candidate),
                "拖拽结束后必须恢复原版 dragItem 幽灵");
            Assert(InventoryDragPreviewAdapter.ShouldRestoreNativeDragGhostAfterRelease(
                NativeDragAdapterOutcome.PassThrough),
                "原版直通释放必须保留原版 dragItem 可见性");
            Assert(!InventoryDragPreviewAdapter.ShouldRestoreNativeDragGhostAfterRelease(
                NativeDragAdapterOutcome.Submitted) &&
                !InventoryDragPreviewAdapter.ShouldRestoreNativeDragGhostAfterRelease(
                    NativeDragAdapterOutcome.Cancelled),
                "提交或取消已结束原版拖拽，不得再次复活 dragItem 幽灵");
            Assert(!InventoryDragPreviewAdapter.ShouldRestoreNativeDragGhostAfterDragEnded(true) &&
                InventoryDragPreviewAdapter.ShouldRestoreNativeDragGhostAfterDragEnded(false),
                "下一次 drag-ended poll 必须继承 BUE-owned 结束状态且新原版结束仍恢复 ghost");

            var lifecycle = new InventoryDragPreviewAdapter.NativeDragGhostLifecycle();
            lifecycle.BeginDrag();
            lifecycle.Complete(NativeDragAdapterOutcome.Submitted);
            Assert(!lifecycle.ShouldRestoreAfterEnded(),
                "BUE 提交后的下一次 drag-ended 状态不得恢复 ghost");
            lifecycle.BeginDrag();
            Assert(lifecycle.ShouldRestoreAfterEnded(),
                "新拖拽结束时必须恢复原版 ghost");
            lifecycle.BeginDrag();
            lifecycle.Complete(NativeDragAdapterOutcome.PassThrough);
            Assert(lifecycle.ShouldRestoreAfterEnded(),
                "原版直通结束时必须恢复原版 ghost");
        }

        private static bool Approximately(float actual, float expected)
        {
            return Math.Abs(actual - expected) < 0.0001f;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class PreviewSink : IInventoryPreviewSink
        {
            internal int IconCount;
            internal PreviewFrame LastFrame;
            internal PreviewIcon LastIcon;
            public void ShowFrame(PreviewFrame frame) { LastFrame = frame; }
            public void ShowIcon(PreviewIcon icon) { LastIcon = icon; IconCount++; }
            public void HideIcon() { }
            public void Hide() { }
        }

        private sealed class FixedPreviewEvaluator : IPlacementCandidateEvaluator
        {
            private readonly ItemPlacementPreview result;
            internal FixedPreviewEvaluator(ItemPlacementPreview result) { this.result = result; }
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input) { return result; }
        }

        private sealed class Grid : IGridOccupancyView
        {
            private readonly bool[,] cells;
            public Grid(byte width, byte height) { Width = width; Height = height; cells = new bool[width, height]; }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y) { return cells[x, y]; }
            public void Fill(byte x, byte y, byte width, byte height)
            {
                for (var yy = y; yy < y + height; yy++)
                    for (var xx = x; xx < x + width; xx++)
                        cells[xx, yy] = true;
            }
        }
    }
}
