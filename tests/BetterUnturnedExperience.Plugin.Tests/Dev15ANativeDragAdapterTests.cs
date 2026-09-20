using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-04：原 UI 套件 DEV-15A 面随 BII 类型迁入插件套件（spec 166 行「插件套件锁
    /// BII 开工收工后的可观察启停」；Bii 工程经 IVT 对 *.Tests 开放，02A 组 3 形态）。
    /// 判据面=拖拽 presenter 代数语义 + 原生拖拽端口提交/直通矩阵（WAS Area/装备页/
    /// 过期/同格 各分支）——纯 C# 决策语义，与原套件逐字同判据。
    /// </summary>
    internal static class Dev15ANativeDragAdapterTests
    {
        internal static void Run()
        {
            DragPresenterGenerationSemantics();
            NativeDragAdapterSubmitAndPassThroughMatrix();
        }

        /// <summary>DEV-V6-02E 原判据（作废票：04 随类型迁移）：presenter 的代数语义——
        /// 当前代转评估器、过期代拒、结束拖拽拒。</summary>
        private static void DragPresenterGenerationSemantics()
        {
            var evaluator = new FixedCandidateEvaluator();
            var presenter = new InventoryDragPresenter(evaluator);
            presenter.BeginDrag(7);
            var input = new PlacementCandidateInput(7, new ItemGridPosition(0, 0, 0, 0), new ContainerReference(ContainerKind.PlayerInventory, 0, 1), 1.2f, 1.2f, 1, 1, 0, true, new EmptyGrid(4, 4));
            Assert(presenter.Evaluate(input).State == PlacementPreviewState.Candidate, "current generation delegates to evaluator");
            var stale = new PlacementCandidateInput(6, input.Source, input.TargetContainer, input.CursorGridX, input.CursorGridY, input.ItemWidth, input.ItemHeight, input.CurrentRotation, input.AllowAutomaticRotation, input.Occupancy);
            Assert(presenter.Evaluate(stale).Reason == PlacementReason.StaleDrag, "stale generation is rejected");
            presenter.EndDrag();
            Assert(presenter.Evaluate(input).Reason == PlacementReason.StaleDrag, "ended drag rejects callbacks");
        }

        private static void NativeDragAdapterSubmitAndPassThroughMatrix()
        {
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var native = new RecordingNativeDragActions();
            var source = new ItemGridPosition(7, 1, 2, 0);
            var target = new ItemGridPosition(7, 3, 4, 1);
            var candidate = new ItemPlacementPreview(12, PlacementPreviewState.Candidate, target, 2, 3, PlacementReason.None);
            var input = new NativeDragAdapterInput(true, 12, source, candidate);

            Assert(adapter.HandleRelease(input, native) == NativeDragAdapterOutcome.Submitted, "ordinary grid candidate submits through native port");
            Assert(native.StopCount == 1 && native.SendCount == 1, "ordinary candidate submits and then clears the native drag");
            Assert(native.OperationOrder == "send>stop", "ordinary candidate sends before stopping the native drag");
            Assert(native.LastSource.Page == 7 && native.LastTarget.Page == 7 && native.LastTarget.Rotation == 1, "native submission preserves source and candidate coordinates");

            native.Reset();
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 12, source, candidate, 3), native)
                == NativeDragAdapterOutcome.PassThrough,
                "callback page mismatch passes through instead of submitting a candidate from another page");
            Assert(native.StopCount == 0 && native.SendCount == 0,
                "callback page mismatch never invokes enhanced native actions");

            native.Reset();
            var groundSource = new ItemGridPosition(8, 0, 0, 0);
            var groundInput = new NativeDragAdapterInput(true, 13, groundSource,
                new ItemPlacementPreview(13, PlacementPreviewState.Candidate, new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None));
            Assert(adapter.HandleRelease(groundInput, native) == NativeDragAdapterOutcome.Submitted,
                "DEV-16F: AREA source targeting a grid submits through the ground port");
            Assert(native.GroundTakeCount == 1 && native.SendCount == 0 && native.StopCount == 1,
                "DEV-16F: AREA source commits via TakeGroundItem, never sendDragItem");

            native.Reset();
            var equipmentTarget = new ItemPlacementPreview(14, PlacementPreviewState.Candidate, new ItemGridPosition(1, 0, 0, 0), 1, 1, PlacementReason.None);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 14, source, equipmentTarget), native) == NativeDragAdapterOutcome.PassThrough, "equipment target is native pass-through");
            Assert(native.StopCount == 0 && native.SendCount == 0, "special target never invokes enhanced native port");

            native.Reset();
            var areaTarget = new ItemPlacementPreview(15, PlacementPreviewState.Candidate, new ItemGridPosition(8, 0, 0, 0), 1, 1, PlacementReason.None);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 15, source, areaTarget), native) == NativeDragAdapterOutcome.PassThrough, "area target is native pass-through");
            Assert(native.StopCount == 0 && native.SendCount == 0, "area target never invokes enhanced native port");

            native.Reset();
            var staleSpecialTarget = new ItemPlacementPreview(20, PlacementPreviewState.Candidate, new ItemGridPosition(1, 0, 0, 0), 1, 1, PlacementReason.None);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 21, source, staleSpecialTarget), native) == NativeDragAdapterOutcome.PassThrough, "stale special target passes through natively");
            Assert(native.StopCount == 0 && native.SendCount == 0, "stale special target never reaches native pass-through");

            native.Reset();
            var invalid = new ItemPlacementPreview(16, PlacementPreviewState.LocallyInvalid, new ItemGridPosition(7, 3, 4, 1), 2, 3, PlacementReason.Occupied);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 16, source, invalid), native) == NativeDragAdapterOutcome.Cancelled, "invalid ordinary candidate cancels enhanced drag");
            Assert(native.StopCount == 0 && native.SendCount == 0, "invalid ordinary candidate leaves native drag live for swap/pass-through decision");

            native.Reset();
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(false, 17, source, candidate), native) == NativeDragAdapterOutcome.PassThrough, "ended drag is native pass-through");
            Assert(native.StopCount == 0 && native.SendCount == 0, "ended drag does not invoke enhanced native port");

            native.Reset();
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 19, source, candidate), native) == NativeDragAdapterOutcome.Cancelled, "stale ordinary candidate fails closed");
            Assert(native.StopCount == 0 && native.SendCount == 0, "stale ordinary candidate is dropped without native action");

            native.Reset();
            var same = new ItemPlacementPreview(18, PlacementPreviewState.Candidate, source, 1, 1, PlacementReason.None);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 18, source, same), native) == NativeDragAdapterOutcome.PassThrough, "same placement is native cancellation path");
            Assert(native.StopCount == 0 && native.SendCount == 0, "same placement does not submit a duplicate native request");

            native.Reset();
            var ordinarySource = new ItemGridPosition(7, 3, 4, 0);
            var sameInvalid = new ItemPlacementPreview(22, PlacementPreviewState.LocallyInvalid, new ItemGridPosition(7, 3, 4, 0), 1, 1, PlacementReason.Occupied);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 22, ordinarySource, sameInvalid), native) == NativeDragAdapterOutcome.Cancelled, "same placement with invalid preview cancels enhanced drag");
            Assert(native.StopCount == 0 && native.SendCount == 0, "same placement invalid preview keeps native drag live for vanilla swap handling");
        }

        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        /// <summary>DEV-V6-04：contract-typed evaluator stand-in（迁入自 UI 套件；具体
        /// Core 评估器是宿主的事，本套件只钉 presenter 自身语义）。</summary>
        private sealed class FixedCandidateEvaluator : IPlacementCandidateEvaluator
        {
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                return new ItemPlacementPreview(input.DragGeneration, PlacementPreviewState.Candidate,
                    input.Source, input.ItemWidth, input.ItemHeight, PlacementReason.None);
            }
        }

        private sealed class EmptyGrid : IGridOccupancyView
        {
            public EmptyGrid(byte width, byte height) { Width = width; Height = height; }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y) { return false; }
        }

        private sealed class RecordingNativeDragActions : INativeInventoryDragActions
        {
            public int StopCount { get; private set; }
            public int SendCount { get; private set; }
            public int GroundTakeCount { get; private set; }
            public ItemGridPosition LastSource { get; private set; }
            public ItemGridPosition LastTarget { get; private set; }
            public string OperationOrder { get; private set; } = string.Empty;

            public void StopDrag() { StopCount++; OperationOrder += "stop"; if (SendCount > 0) OperationOrder = "send>stop"; }

            public void SendDragItem(ItemGridPosition source, ItemGridPosition target)
            {
                SendCount++;
                LastSource = source;
                LastTarget = target;
                OperationOrder += "send";
            }

            public void TakeGroundItem(ItemGridPosition target)
            {
                GroundTakeCount++;
                LastTarget = target;
                OperationOrder += "ground";
            }

            public void Reset()
            {
                StopCount = 0;
                SendCount = 0;
                GroundTakeCount = 0;
                LastSource = default(ItemGridPosition);
                LastTarget = default(ItemGridPosition);
                OperationOrder = string.Empty;
            }
        }
    }
}
