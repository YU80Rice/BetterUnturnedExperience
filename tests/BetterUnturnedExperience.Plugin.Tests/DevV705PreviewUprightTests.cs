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
            Group("frame drawing layer guards", failures, collectAllFailures, FrameDrawingLayerGuards);
            Group("frame mounts to top-level container", failures, collectAllFailures, FrameMountsToTopLevelContainer);
            Group("frame parent chain drives real container", failures, collectAllFailures, FrameParentChainDrivesRealContainer);
            Group("frame anchor survives scrolling", failures, collectAllFailures, FrameAnchorSurvivesScrolling);
            Group("real frame color write", failures, collectAllFailures, RealFrameColorWrite);
            Group("hidden clears last preview", failures, collectAllFailures, HiddenPointerClearsLastPreview);
            Group("dashboard close ghost policy", failures, collectAllFailures, DashboardCloseGhostPolicy);
            Group("dashboard close full chain", failures, collectAllFailures, DashboardCloseFullChain);
            Group("drag ended edge never resurrects ghost", failures, collectAllFailures, DragEndedEdgeNeverRestoresGhost);
            Group("session end anchors preview hidden", failures, collectAllFailures, SessionEndAnchorsPreviewHidden);
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
        }

        // 2026-09-27 SP return defect 1: the state machine emits preview-visible
        // correctly, so the drawing layer must be enforced. A frame element that
        // cannot take the color (no ISleekBox behind it) or a mount parent that
        // is hidden used to "succeed" silently and leave the vanilla dark cell
        // showing; both must fault into the sink's rebuild-retry gate instead.
        private static void FrameDrawingLayerGuards()
        {
            var nonColorTopLevel = new StubVisualContainer(new StubVisualElement(canWriteColor: false));
            var sink = new InventoryPreviewVisualSink(nonColorTopLevel);
            var threw = false;
            try
            {
                sink.ShowFrame(AnchoredFrame(PreviewFrameKind.ValidGreen));
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(threw, "颜色无法写入真实元素时必须进入失败路径，不得静默显示深色框");

            var hiddenTopLevel = new StubVisualContainer(new StubVisualElement(canWriteColor: true)) { IsVisible = false };
            var hiddenSink = new InventoryPreviewVisualSink(hiddenTopLevel);
            threw = false;
            try
            {
                hiddenSink.ShowFrame(AnchoredFrame(PreviewFrameKind.InvalidRed));
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(threw, "框的父级不可见时不得静默报告框已显示");

            var anchorlessSink = new InventoryPreviewVisualSink(
                new StubVisualContainer(new StubVisualElement(canWriteColor: true)));
            threw = false;
            try
            {
                anchorlessSink.ShowFrame(new PreviewFrame(PreviewFrameKind.ValidGreen,
                    new ItemGridPosition(3, 1, 1, 0), 1, 3, 50f, PlacementReason.None));
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(threw, "无顶层锚的框没有诚实的绘制位置，必须故障而不是瞎画");
        }

        // 2026-09-27 research D-1: the frame mounts into the SAME ever-alive
        // top-level container as the icon (frame first, icon on top) and its
        // geometry follows the anchor, not the grid-local panel.
        private static void FrameMountsToTopLevelContainer()
        {
            var topLevel = new StubVisualContainer(
                new StubVisualElement(canWriteColor: true));
            var sink = new InventoryPreviewVisualSink(topLevel);
            sink.ShowFrame(AnchoredFrame(PreviewFrameKind.ValidGreen));

            // Mount always seats BOTH elements (frame child 0, hidden icon
            // child 1 until an icon write shows it).
            Assert(topLevel.ChildCount == 2,
                "框与图标必须同挂顶层容器（PlayerUI.container），不再挂滚动内容层");
            Assert(topLevel.ChildElement.IsVisible, "顶层容器中的框已显示");
            Assert(Approximately(sink.FrameScaleX, 0.25f) && Approximately(sink.FrameScaleY, 0.5f),
                "框保留 PlayerUI 顶层 scale 锚");
            Assert(Approximately(sink.FrameX, -50f) && Approximately(sink.FrameY, -50f),
                "框偏移来自 container 空间换算（框屏幕位−指针位）");
            Assert(Approximately(sink.FrameWidth, 50f) && Approximately(sink.FrameHeight, 150f),
                "框尺寸保持未缩放逻辑格像素");

            // Coordinates follow the grid: a different candidate cell moves
            // the anchor offset by the cell delta.
            sink.ShowFrame(AnchoredFrame(PreviewFrameKind.ValidGreen,
                new ItemGridPosition(3, 3, 1, 0)));
            Assert(Approximately(sink.FrameX, 50f) && Approximately(sink.FrameY, -50f),
                "框坐标必须随候选格子变化（每格 50 逻辑像素）");
        }

        // 2026-09-27 fourth-round return (ticket item 2): the migration
        // assertion must drive the REAL production container/element wrappers
        // (UnturnedVisualContainer/UnturnedVisualElement) over the native
        // interface fakes, not only the sink's stub containers. The Glazier
        // factory and overlay canvas stay the named engine seam — they cannot
        // be constructed in the host.
        private static void FrameParentChainDrivesRealContainer()
        {
            var fakeContainerElement = new FakeSleekElement();
            var container = new UnturnedVisualContainer(fakeContainerElement);
            var frameNative = new FakeSleekBox();
            var frameElement = new UnturnedVisualElement(frameNative);
            var iconNative = new FakeSleekElement();
            var iconElement = new UnturnedVisualElement(iconNative);

            container.AddChild(frameElement);
            container.AddChild(iconElement);

            Assert(fakeContainerElement.NativeChildren.Count == 2,
                "真实容器包装必须把框与图标挂进同一原生父级");
            Assert(ReferenceEquals(fakeContainerElement.NativeChildren[0], frameNative),
                "原生子级 0 必须是框（先挂=在下层）");
            Assert(ReferenceEquals(fakeContainerElement.NativeChildren[1], iconNative),
                "原生子级 1 必须是图标（后挂=在上层）");

            frameElement.PositionScaleX = 0.25f;
            frameElement.PositionOffsetX = -50f;
            frameElement.SizeOffsetY = 150f;
            frameElement.Color = PreviewFrameColor.ValidGreen;
            frameElement.IsVisible = true;
            Assert(frameNative.BackgroundColor.Equals(new SDG.Unturned.SleekColor(
                    UnturnedVisualElement.PreviewFrameRgba(PreviewFrameColor.ValidGreen))),
                "真实元素包装把冻结绿写入原生 ISleekBox");
            Assert(Approximately(frameNative.PositionScale_X, 0.25f) &&
                Approximately(frameNative.PositionOffset_X, -50f) &&
                Approximately(frameNative.SizeOffset_Y, 150f),
                "真实元素包装把锚定几何写入原生元素");
            Assert(frameNative.IsVisible, "真实元素包装把可见性落到原生元素");
        }

        // 2026-09-27 fourth-round return (ticket item 1): the pointer and the
        // candidate BOTH live in the scrolled content space on the production
        // GridContentLocal path, so the anchor offset must be scroll
        // invariant. A regression that re-applies the surface scroll on top
        // of the content-local pointer (double count) shifts the frame by the
        // scroll amount and goes red here.
        private static void FrameAnchorSurvivesScrolling()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedPreviewEvaluator(
                    new ItemPlacementPreview(31, PlacementPreviewState.Candidate,
                        new ItemGridPosition(3, 2, 2, 0), 1, 3, PlacementReason.None)))),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(true, false);

            var unscrolled = new ScrollableSurfaceContext(new StubVisualContainer(
                new StubVisualElement(canWriteColor: true)), new Grid(8, 6), 0f, 0f);
            var scrolled = new ScrollableSurfaceContext(new StubVisualContainer(
                new StubVisualElement(canWriteColor: true)), new Grid(8, 6), 0f, 150f);

            component.OnInventoryOpened(unscrolled);
            component.OnDragStarted(31, ItemAssetIdentity.FromItemId(0), new ItemGridPosition(3, 0, 0, 0));
            InventoryPreviewInput unscrolledInput;
            Assert(component.TryCreatePreviewInput(31, new ItemGridPosition(3, 0, 0, 0),
                    100f, 250f, 1, 3, 0, true, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(0), 0.25f, 0.5f, float.NaN, float.NaN, out unscrolledInput),
                "前置：未滚动输入可构造");
            component.OnDragUpdated(unscrolledInput);
            var unscrolledX = component.PreviewSink.FrameX;
            var unscrolledY = component.PreviewSink.FrameY;
            component.OnInventoryClosed();

            component.OnInventoryOpened(scrolled);
            component.OnDragStarted(32, ItemAssetIdentity.FromItemId(0), new ItemGridPosition(3, 0, 0, 0));
            InventoryPreviewInput scrolledInput;
            Assert(component.TryCreatePreviewInput(32, new ItemGridPosition(3, 0, 0, 0),
                    100f, 250f, 1, 3, 0, true, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(0), 0.25f, 0.5f, float.NaN, float.NaN, out scrolledInput),
                "前置：滚动输入可构造");
            Assert(scrolledInput.ScrollPixelsY == 0f,
                "内容本地指针已含滚动：组件必须把 surface 滚动清零，防止双重计数");
            component.OnDragUpdated(scrolledInput);
            var scrolledX = component.PreviewSink.FrameX;
            var scrolledY = component.PreviewSink.FrameY;
            component.OnInventoryClosed();

            Assert(Approximately(unscrolledX, scrolledX) && Approximately(unscrolledY, scrolledY),
                "内容本地路径下框锚偏移必须与滚动量无关（指针与候选同处内容空间）");
            Assert(Approximately(unscrolledX, 0f) && Approximately(unscrolledY, -150f),
                "框偏移=候选格内容位−指针内容位（cell 50、指针 100,250、候选 2,2）");
        }

        private static PreviewFrame AnchoredFrame(PreviewFrameKind kind)
        {
            return AnchoredFrame(kind, new ItemGridPosition(3, 1, 1, 0));
        }

        private static PreviewFrame AnchoredFrame(PreviewFrameKind kind, ItemGridPosition candidate)
        {
            // Mirrors TryGetNativeFramePlacement for pointer (100,100), origin
            // (0,0), no scroll, cell 50, uiScale 1: offset = cellPx − pointer.
            return new PreviewFrame(kind, candidate, 1, 3, 50f, PlacementReason.None,
                new PreviewFramePlacement(0.25f, 0.5f,
                    (candidate.X * 50f - 100f), (candidate.Y * 50f - 100f), 50f, 150f));
        }

        // 2026-09-27 SP return defect 1: when the pointer leaves the grid the
        // Hidden result must clear LastPreview — a stale Candidate would feed
        // the release path a placement the player can no longer see.
        private static void HiddenPointerClearsLastPreview()
        {
            var evaluator = new ScriptedPreviewEvaluator(
                new ItemPlacementPreview(7, PlacementPreviewState.Candidate,
                    new ItemGridPosition(3, 1, 1, 0), 1, 3, PlacementReason.None),
                new ItemPlacementPreview(7, PlacementPreviewState.Hidden,
                    new ItemGridPosition(3, 0, 0, 0), 0, 0, PlacementReason.OutsideGrid));
            var presenter = new InventoryPreviewPresenter(new InventoryDragPresenter(evaluator));
            var sink = new PreviewSink();
            presenter.BeginDrag(7);
            var input = new InventoryPreviewInput(7,
                new ItemGridPosition(3, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 7),
                75f, 125f, new InventoryGridViewport(0f, 0f, 5, 7, 0f, 0f, 250f, 350f),
                50f, 1f, 0f, 0f, 1, 3, 0, true, 0.5f, 1.5f,
                ItemAssetIdentity.FromItemId(363), 0.25f, 0.5f, -25f, -62.5f,
                new Grid(5, 7));
            presenter.Update(input, sink);
            Assert(presenter.LastPreview.State == PlacementPreviewState.Candidate,
                "前置：网格内候选必须记录为 Candidate");
            presenter.Update(input, sink);
            Assert(presenter.LastPreview.State != PlacementPreviewState.Candidate,
                "指针不在网格内时 Hidden 行为不得把 LastPreview 留成 Candidate");
        }

        // 2026-09-27 SP return defect 1: the color must travel through the REAL
        // UnturnedVisualElement Color setter into a REAL ISleekBox-typed
        // element. The host cannot instantiate a Glazier-backed box (engine
        // ECalls), so this fake implements the native SDG.Unturned interface
        // contract and records the BackgroundColor write; the Glazier-owned
        // box itself stays a named real-machine seam (DEV-V7-07 round).
        private static void RealFrameColorWrite()
        {
            var box = new FakeSleekBox();
            var element = new UnturnedVisualElement(box);
            Assert(element.Color == PreviewFrameColor.None,
                "新元素必须如实报告尚未落色");
            element.Color = PreviewFrameColor.ValidGreen;
            Assert(element.Color == PreviewFrameColor.ValidGreen,
                "落盘写入后 getter 必须回读真实状态");
            Assert(box.BackgroundColor.Equals(new SDG.Unturned.SleekColor(
                    UnturnedVisualElement.PreviewFrameRgba(PreviewFrameColor.ValidGreen))),
                "颜色必须经过真实 Color setter 写入真实 ISleekBox 背景色（冻结绿色）");
            element.Color = PreviewFrameColor.InvalidRed;
            Assert(box.BackgroundColor.Equals(new SDG.Unturned.SleekColor(
                    UnturnedVisualElement.PreviewFrameRgba(PreviewFrameColor.InvalidRed))),
                "红框写入同样必须落盘到真实 ISleekBox（冻结红色）");

            var nonBox = new UnturnedVisualElement(new FakeSleekElement());
            nonBox.Color = PreviewFrameColor.ValidGreen;
            Assert(nonBox.Color == PreviewFrameColor.None,
                "box==null 时不得静默伪装写入成功");
        }


        // 2026-09-27 SP return defect 2: closing the dashboard while an item is
        // held leaves vanilla isDragging true and kills the updateDraggedItem
        // poll, so the drag-ended edge never fires. The close combination must
        // end the session immediately: restore dragItem visibility handling,
        // clear the suppress flag, and never wait for a later edge.
        private static void DashboardCloseGhostPolicy()
        {
            Assert(InventoryDragPreviewAdapter.ShouldEndDragSessionForDashboardClose(
                dashboardActive: false, isDragging: true),
                "dashboardActive=false 且 isDragging=true 必须立即结束拖拽会话，不得等待 drag-ended 边沿");
            Assert(!InventoryDragPreviewAdapter.ShouldEndDragSessionForDashboardClose(
                dashboardActive: true, isDragging: true),
                "面板仍在时不得触发关闭结束路径");
            Assert(!InventoryDragPreviewAdapter.ShouldEndDragSessionForDashboardClose(
                dashboardActive: false, isDragging: false),
                "非拖拽状态关闭不得触发关闭结束路径");
            Assert(!InventoryDragPreviewAdapter.ShouldRestoreNativeDragGhostWhenTargetLost(false),
                "面板关闭后丢失目标表面不得恢复原版幽灵（冻结残留根源）");
            Assert(InventoryDragPreviewAdapter.ShouldRestoreNativeDragGhostWhenTargetLost(true),
                "面板打开时丢失目标表面仍恢复原版幽灵");

            // research D-2a: the suppress latch is retired with the edge
            // restore — the edge now pins the ghost hidden unconditionally
            // (covered end-to-end by drag ended edge never resurrects ghost).
        }

        // 2026-09-27 07-verification return closure (ticket option 1): drive
        // the REAL Poll close path on a host-constructed adapter. Vanilla
        // isDragging is a static auto-property with a non-public setter, so
        // the test reflection-writes its backing field (the same AccessTools
        // pattern the adapter itself uses); active stays at its host default
        // false. stopDrag is observed through the injected
        // INativeInventoryDragActions port (the same port the release path
        // uses), and the ghost visibility command is observed on the adapter.
        // The physical dragItem write and the vanilla stopDrag IL remain the
        // named engine seams.
        private static void SetVanillaIsDragging(bool value)
        {
            var field = typeof(SDG.Unturned.PlayerDashboardInventoryUI).GetField(
                "<isDragging>k__BackingField",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert(field != null, "vanilla isDragging backing field 必须存在");
            field.SetValue(null, value);
        }

        private static void DashboardCloseFullChain()
        {
            var previousIsDragging = SDG.Unturned.PlayerDashboardInventoryUI.isDragging;
            var closeLogs = new System.Collections.Generic.List<string>();
            try
            {
                var component = new BetterItemInteractionUiComponent(
                    new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedPreviewEvaluator(
                        new ItemPlacementPreview(11, PlacementPreviewState.Candidate,
                            new ItemGridPosition(3, 1, 1, 0), 1, 3, PlacementReason.None)))),
                    new NativeInventoryInteractionAdapter(2, 8),
                    null, line => closeLogs.Add(line), null);
                component.OnUiInitialized(true, false);
                var recorder = new RecordingCloseNativeActions();
                var adapter = new InventoryDragPreviewAdapter(component, null, recorder);
                Assert(adapter.LastNativeDragGhostVisibilityCommand == null,
                    "前置：新适配器尚未发出任何幽灵可见性命令");

                // BUE owns a live drag session; a preview must be VISIBLE when
                // the dashboard closes - that is the real-machine shape
                // (preview-visible is the last event in 27/28 round-3 drags)
                // and it feeds the close anchor gate.
                var surface = new AnchorSurfaceContext(new StubVisualContainer(
                    new StubVisualElement(canWriteColor: true)), new Grid(8, 6));
                component.OnInventoryOpened(surface);
                component.OnDragStarted(11, ItemAssetIdentity.FromItemId(0),
                    new ItemGridPosition(3, 0, 0, 0));
                Assert(component.CurrentDragGeneration == 11 && component.EnhancedDragActive,
                    "前置：BUE 拖拽会话已建立");
                InventoryPreviewInput previewInput;
                Assert(component.TryCreatePreviewInput(11, new ItemGridPosition(3, 0, 0, 0),
                        100f, 250f, 1, 3, 0, true, 0.5f, 0.5f,
                        ItemAssetIdentity.FromItemId(0), out previewInput),
                    "前置：拖拽输入可构造");
                component.OnDragUpdated(previewInput);
                Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                    "前置：关闭前预览可见（Candidate）");

                // The dashboard closes while the vanilla drag session is live.
                SetVanillaIsDragging(true);
                adapter.Poll();

                Assert(recorder.StopCount == 1,
                    "真实关闭路径必须经原生端口提交原版 stopDrag");
                Assert(component.CurrentDragGeneration == 0 && !component.EnhancedDragActive,
                    "真实关闭路径必须立即结束 BUE 拖拽会话");
                Assert(adapter.LastNativeDragGhostVisibilityCommand == false,
                    "真实关闭路径必须发出幽灵隐藏命令（面板关闭期间冻结幽灵不得渲染）");
                Assert(!adapter.LastObservedVanillaDragging,
                    "真实关闭路径必须消费 drag-ended 边沿（wasDragging 复位，后续边沿不存在）");
                var closeAnchored = false;
                foreach (var line in closeLogs)
                {
                    if (line.Contains("event=preview-hidden") && line.Contains("reason=close")) closeAnchored = true;
                }
                Assert(closeAnchored,
                    "关闭链结束会话必须落 event=preview-hidden reason=close 日志锚（不得落成 drag-cancelled）");

                // Edge independence: a second full Poll cannot be driven in the
                // host (vanilla Player cctor is engine-bound — named seam), so
                // edge consumption is proven by the reset wasDragging state
                // above plus the single stopDrag command below.
                Assert(recorder.StopCount == 1, "关闭路径只提交一次 stopDrag");
            }
            finally
            {
                SetVanillaIsDragging(previousIsDragging);
            }
        }

        // 2026-09-27 third machine round (research §D-2a): vanilla stopDrag
        // already hides the ghost when a drag ends; the drag-ended edge that
        // forces it visible again is the residue source. The edge must pin the
        // ghost hidden and anchor event=preview-hidden — never light it up.
        private static void DragEndedEdgeNeverRestoresGhost()
        {
            var previousIsDragging = SDG.Unturned.PlayerDashboardInventoryUI.isDragging;
            var previousActive = SDG.Unturned.PlayerDashboardInventoryUI.active;
            try
            {
                var logs = new System.Collections.Generic.List<string>();
                var component = new BetterItemInteractionUiComponent(
                    new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedPreviewEvaluator(
                        new ItemPlacementPreview(21, PlacementPreviewState.Candidate,
                            new ItemGridPosition(3, 1, 1, 0), 1, 3, PlacementReason.None)))),
                    new NativeInventoryInteractionAdapter(2, 8),
                    null, line => logs.Add(line), null);
                component.OnUiInitialized(true, false);
                var recorder = new RecordingCloseNativeActions();
                var adapter = new InventoryDragPreviewAdapter(component, null, recorder);

                // Vanilla dashboard open, drag active, no BUE surfaces: the
                // pass-through ghost legitimately shows while vanilla owns it.
                SDG.Unturned.PlayerDashboardInventoryUI.active = true;
                SetVanillaIsDragging(true);
                adapter.Poll();
                Assert(adapter.LastNativeDragGhostVisibilityCommand == true,
                    "前置：原版活跃且拖拽中，目标丢失分支恢复原版幽灵");

                // The drag ends. Vanilla stopDrag hid the ghost; the edge must
                // NOT resurrect it.
                SetVanillaIsDragging(false);
                adapter.Poll();

                Assert(adapter.LastNativeDragGhostVisibilityCommand == false,
                    "drag-ended 边沿必须钉住幽灵隐藏，绝不复活原版 dragItem");
                Assert(recorder.StopCount == 0, "普通拖拽结束不触发关闭链的 stopDrag");
            }
            finally
            {
                SetVanillaIsDragging(previousIsDragging);
                SDG.Unturned.PlayerDashboardInventoryUI.active = previousActive;
            }
        }

        // 2026-09-27 third machine round (research §D-2b): 27/28 drags ended
        // with the last preview event still preview-visible — the BUE icon on
        // the ever-alive PlayerUI.container never got a closing anchor. Every
        // session end (cancel/submit/close/isolate) must emit one.
        private static void SessionEndAnchorsPreviewHidden()
        {
            var logs = new System.Collections.Generic.List<string>();
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedPreviewEvaluator(
                    new ItemPlacementPreview(22, PlacementPreviewState.Candidate,
                        new ItemGridPosition(3, 1, 1, 0), 1, 3, PlacementReason.None)))),
                new NativeInventoryInteractionAdapter(2, 8),
                null, line => logs.Add(line), null);
            component.OnUiInitialized(true, false);
            var topLevel = new StubVisualContainer(new StubVisualElement(canWriteColor: true));
            var surface = new AnchorSurfaceContext(topLevel, new Grid(8, 6));
            component.OnInventoryOpened(surface);
            component.OnDragStarted(22, ItemAssetIdentity.FromItemId(0), new ItemGridPosition(3, 0, 0, 0));
            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(22, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 3, 0, true, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(0), out input),
                "前置：拖拽输入可构造");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "前置：预览已可见（Candidate）");
            component.OnDragCancelled();

            var anchored = false;
            foreach (var line in logs)
            {
                if (line.Contains("event=preview-hidden") && line.Contains("reason=drag-cancelled")) anchored = true;
            }
            Assert(anchored, "拖拽取消结束会话必须落 event=preview-hidden 日志锚");

            // research D-2b: the release path is a session end too — the
            // submitted placement must anchor reason=native-release.
            component.OnDragStarted(23, ItemAssetIdentity.FromItemId(0), new ItemGridPosition(3, 0, 0, 0));
            Assert(component.TryCreatePreviewInput(23, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 3, 0, true, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(0), out var releaseInput),
                "前置：释放场景输入可构造");
            component.OnDragUpdated(releaseInput);
            var releaseOutcome = component.OnDragReleased(new NativeDragAdapterInput(true, 23,
                new ItemGridPosition(3, 0, 0, 0),
                new ItemPlacementPreview(23, PlacementPreviewState.Candidate,
                    new ItemGridPosition(3, 1, 1, 0), 1, 3, PlacementReason.None), 3),
                new RecordingCloseNativeActions());
            Assert(releaseOutcome == NativeDragAdapterOutcome.Submitted,
                "前置：释放走提交路径");
            var releasedAnchor = false;
            foreach (var line in logs)
            {
                if (line.Contains("event=preview-hidden") && line.Contains("reason=native-release")) releasedAnchor = true;
            }
            Assert(releasedAnchor, "提交释放结束会话必须落 event=preview-hidden reason=native-release 日志锚");

            // research D-2b: isolation is also a session end — the anchor must
            // fire even though runtime.Isolate() runs CleanupUiAndDrag (which
            // clears LastPreview) BEFORE the isolate path can observe it.
            component.OnDragStarted(24, ItemAssetIdentity.FromItemId(0), new ItemGridPosition(3, 0, 0, 0));
            Assert(component.TryCreatePreviewInput(24, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 3, 0, true, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(0), out var isolateInput),
                "前置：隔离场景输入可构造");
            component.OnDragUpdated(isolateInput);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "前置：隔离前预览可见");
            component.IsolatePreviewFailureResult();

            var isolatedAnchor = false;
            foreach (var line in logs)
            {
                if (line.Contains("event=preview-hidden") && line.Contains("reason=isolated")) isolatedAnchor = true;
            }
            Assert(isolatedAnchor, "隔离结束会话必须落 event=preview-hidden 日志锚");
        }

        private sealed class ScrollableSurfaceContext : IInventorySurfaceContext
        {
            private readonly IVisualContainer topLevel;
            private readonly IGridOccupancyView occupancy;
            private readonly float scrollPixelsX;
            private readonly float scrollPixelsY;
            internal ScrollableSurfaceContext(IVisualContainer topLevel, IGridOccupancyView occupancy,
                float scrollPixelsX, float scrollPixelsY)
            {
                this.topLevel = topLevel;
                this.occupancy = occupancy;
                this.scrollPixelsX = scrollPixelsX;
                this.scrollPixelsY = scrollPixelsY;
            }
            public ContainerReference CurrentContainer { get { return new ContainerReference(ContainerKind.PlayerInventory, 3, 31); } }
            public IVisualContainer TopLevelContainer { get { return topLevel; } }
            public InventoryGridViewport Viewport { get { return new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 600f); } }
            public float CellPixelSize { get { return 50f; } }
            public float UiScale { get { return 1f; } }
            public float ScrollPixelsX { get { return scrollPixelsX; } }
            public float ScrollPixelsY { get { return scrollPixelsY; } }
            public IGridOccupancyView Occupancy { get { return occupancy; } }
        }

        private sealed class AnchorSurfaceContext : IInventorySurfaceContext
        {
            private readonly IVisualContainer topLevel;
            private readonly IGridOccupancyView occupancy;
            internal AnchorSurfaceContext(IVisualContainer topLevel, IGridOccupancyView occupancy)
            {
                this.topLevel = topLevel;
                this.occupancy = occupancy;
            }
            public ContainerReference CurrentContainer { get { return new ContainerReference(ContainerKind.PlayerInventory, 3, 22); } }
            public IVisualContainer TopLevelContainer { get { return topLevel; } }
            public InventoryGridViewport Viewport { get { return new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f); } }
            public float CellPixelSize { get { return 50f; } }
            public float UiScale { get { return 1f; } }
            public float ScrollPixelsX { get { return 0f; } }
            public float ScrollPixelsY { get { return 0f; } }
            public IGridOccupancyView Occupancy { get { return occupancy; } }
        }

        private sealed class RecordingCloseNativeActions : INativeInventoryDragActions
        {
            internal int StopCount;
            public void StopDrag() { StopCount++; }
            public void SendDragItem(ItemGridPosition source, ItemGridPosition target) { }
            public void TakeGroundItem(ItemGridPosition target) { }
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
            public void HideFrame() { }
            public void HideIcon() { }
            public void Hide() { }
        }

        private sealed class FixedPreviewEvaluator : IPlacementCandidateEvaluator
        {
            private readonly ItemPlacementPreview result;
            internal FixedPreviewEvaluator(ItemPlacementPreview result) { this.result = result; }
            // Echo the incoming drag generation: a fixed generation would be
            // stale-filtered by the presenter on any later drag session.
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                return new ItemPlacementPreview(input.DragGeneration, result.State, result.Candidate,
                    result.Width, result.Height, result.Reason);
            }
        }

        private sealed class ScriptedPreviewEvaluator : IPlacementCandidateEvaluator
        {
            private readonly ItemPlacementPreview[] results;
            private int index;
            internal ScriptedPreviewEvaluator(params ItemPlacementPreview[] results) { this.results = results; }
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                var result = results[Math.Min(index, results.Length - 1)];
                index++;
                return result;
            }
        }

        private sealed class StubVisualElement : IVisualElement
        {
            private PreviewFrameColor color;
            internal StubVisualElement(bool canWriteColor) { CanWriteColor = canWriteColor; }
            public float PositionScaleX { get; set; }
            public float PositionScaleY { get; set; }
            public float PositionOffsetX { get; set; }
            public float PositionOffsetY { get; set; }
            public float SizeOffsetX { get; set; }
            public float SizeOffsetY { get; set; }
            public byte RotationAngle { get; set; }
            public bool CanRotate { get; set; }
            public bool IsVisible { get; set; }
            public PreviewFrameColor Color
            {
                // Mirrors the real element contract: a write that cannot land
                // (no ISleekBox backing) never reports the requested color.
                get { return CanWriteColor ? color : PreviewFrameColor.None; }
                set { color = value; }
            }
            public ItemAssetIdentity BoundAsset { get; set; }
            public bool CanWriteColor { get; set; }
        }

        private sealed class StubVisualContainer : IVisualContainer
        {
            // Distinct box and image elements: the sink writes frame and icon
            // geometry to DIFFERENT elements, so a shared instance would let
            // the icon overwrite the frame coordinates the tests read back.
            private readonly StubVisualElement box;
            private readonly StubVisualElement image;
            private readonly System.Collections.Generic.List<IVisualElement> children =
                new System.Collections.Generic.List<IVisualElement>();
            internal StubVisualContainer(StubVisualElement box)
            {
                this.box = box;
                image = new StubVisualElement(box.CanWriteColor);
                IsVisible = true;
            }
            public bool IsVisible { get; set; }
            internal int ChildCount { get { return children.Count; } }
            internal IVisualElement ChildElement { get { return children.Count == 0 ? null : children[0]; } }
            public IVisualElement CreateBox() { return box; }
            public IVisualElement CreateImage() { return image; }
            public void AddChild(IVisualElement child) { if (!children.Contains(child)) children.Add(child); }
            public void RemoveChild(IVisualElement child) { children.Remove(child); }
        }

        // Host-safe natives: managed fakes of the SDG.Unturned Glazier
        // interface contract. No engine ECalls are touched — the members only
        // record writes, so JIT compilation stays inside managed code.
        private class FakeSleekElement : SDG.Unturned.ISleekElement
        {
            // Native child recording: lets tests drive the REAL production
            // container/element wrappers and assert the true parent chain.
            private readonly System.Collections.Generic.List<SDG.Unturned.ISleekElement> nativeChildren =
                new System.Collections.Generic.List<SDG.Unturned.ISleekElement>();
            internal System.Collections.Generic.IReadOnlyList<SDG.Unturned.ISleekElement> NativeChildren { get { return nativeChildren; } }
            public bool IsVisible { get; set; }
            public SDG.Unturned.ISleekElement Parent { get { return null; } }
            public SDG.Unturned.ISleekLabel SideLabel { get { return null; } }
            public float PositionOffset_X { get; set; }
            public float PositionOffset_Y { get; set; }
            public float PositionScale_X { get; set; }
            public float PositionScale_Y { get; set; }
            public float SizeOffset_X { get; set; }
            public float SizeOffset_Y { get; set; }
            public float SizeScale_X { get; set; }
            public float SizeScale_Y { get; set; }
            public SDG.Unturned.ISleekElement AttachmentRoot { get { return null; } }
            public bool IsAnimatingTransform { get { return false; } }
            public bool UseManualLayout { get; set; }
            public bool UseWidthLayoutOverride { get; set; }
            public bool UseHeightLayoutOverride { get; set; }
            public SDG.Unturned.ESleekChildLayout UseChildAutoLayout { get; set; }
            public SDG.Unturned.ESleekChildPerpendicularAlignment ChildPerpendicularAlignment { get; set; }
            public bool ExpandChildren { get; set; }
            public bool IgnoreLayout { get; set; }
            public float ChildAutoLayoutPadding { get; set; }
            public void InternalDestroy() { }
            public void AnimatePositionOffset(float a, float b, SDG.Unturned.ESleekLerp mode, float t) { }
            public void AnimatePositionScale(float a, float b, SDG.Unturned.ESleekLerp mode, float t) { }
            public void AnimateSizeOffset(float a, float b, SDG.Unturned.ESleekLerp mode, float t) { }
            public void AnimateSizeScale(float a, float b, SDG.Unturned.ESleekLerp mode, float t) { }
            public void AddChild(SDG.Unturned.ISleekElement child) { if (!nativeChildren.Contains(child)) nativeChildren.Add(child); }
            public void AddLabel(string text, SDG.Unturned.ESleekSide side) { }
            public void AddLabel(string text, Color color, SDG.Unturned.ESleekSide side) { }
            public void UpdateLabel(string text) { }
            public int FindIndexOfChild(SDG.Unturned.ISleekElement child) { return -1; }
            public SDG.Unturned.ISleekElement GetChildAtIndex(int index) { return null; }
            public int GetChildCount() { return 0; }
            public void Update() { }
            public void RemoveChild(SDG.Unturned.ISleekElement child) { nativeChildren.Remove(child); }
            public void RemoveAllChildren() { }
            public Vector2 ViewportToNormalizedPosition(Vector2 viewportPoint) { return default(Vector2); }
            public Vector2 GetNormalizedCursorPosition() { return default(Vector2); }
            public Vector2 GetAbsoluteSize() { return default(Vector2); }
            public void SetAsFirstSibling() { }
            public void ForceLayoutUpdate() { }
        }

        private sealed class FakeSleekBox : FakeSleekElement, SDG.Unturned.ISleekBox
        {
            public SDG.Unturned.SleekColor BackgroundColor { get; set; }
            public string Text { get; set; }
            public FontStyle FontStyle { get; set; }
            public TextAnchor TextAlignment { get; set; }
            public SDG.Unturned.ESleekFontSize FontSize { get; set; }
            public SDG.Unturned.ETextContrastContext TextContrastContext { get; set; }
            public SDG.Unturned.SleekColor TextColor { get; set; }
            public bool AllowRichText { get; set; }
            public string TooltipText { get; set; }
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
