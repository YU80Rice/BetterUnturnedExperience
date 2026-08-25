using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    /// <summary>
    /// Abstract visual element handle used to keep presentation logic independent of
    /// the engine-specific UI runtime during unit testing.
    /// </summary>
    internal interface IVisualElement
    {
        float PositionOffsetX { get; set; }
        float PositionOffsetY { get; set; }
        float SizeOffsetX { get; set; }
        float SizeOffsetY { get; set; }
        byte RotationAngle { get; set; }
        bool IsVisible { get; set; }
        PreviewFrameColor Color { get; set; }
        ItemAssetIdentity BoundAsset { get; set; }
    }

    internal enum PreviewFrameColor : byte
    {
        None = 0,
        ValidGreen = 1,
        InvalidRed = 2
    }

    /// <summary>
    /// Abstract container surface for mounting frame and floating icon elements.
    /// </summary>
    internal interface IVisualContainer
    {
        IVisualElement CreateBox();
        IVisualElement CreateImage();
        void AddChild(IVisualElement child);
        void RemoveChild(IVisualElement child);
    }

    /// <summary>
    /// Rich surface context provided by the concrete inventory UI (ItemClothingUI / ItemStorageUI).
    /// Supplies container reference, geometry, scaling, scroll offsets, and visual mount targets.
    /// </summary>
    internal interface IInventorySurfaceContext : IClientUiInventorySurface
    {
        ContainerReference CurrentContainer { get; }
        IVisualContainer TopLevelContainer { get; }
        IVisualContainer GridPanelContainer { get; }
        InventoryGridViewport Viewport { get; }
        float CellPixelSize { get; }
        float UiScale { get; }
        float ScrollPixelsX { get; }
        float ScrollPixelsY { get; }
        IGridOccupancyView Occupancy { get; }
    }

    /// <summary>
    /// High-performance, zero-allocation implementation of IInventoryPreviewSink.
    /// Pools frame and floating icon elements and manages visual lifecycle.
    /// </summary>
    internal sealed class InventoryPreviewVisualSink : IInventoryPreviewSink
    {
        private readonly IVisualContainer topLevelContainer;
        private readonly IVisualContainer gridPanelContainer;
        private readonly IVisualElement frameElement;
        private readonly IVisualElement iconElement;
        private bool isMounted;

        internal InventoryPreviewVisualSink(IVisualContainer topLevelContainer, IVisualContainer gridPanelContainer)
        {
            this.topLevelContainer = topLevelContainer ?? throw new ArgumentNullException(nameof(topLevelContainer));
            this.gridPanelContainer = gridPanelContainer ?? throw new ArgumentNullException(nameof(gridPanelContainer));
            this.frameElement = gridPanelContainer.CreateBox();
            this.iconElement = topLevelContainer.CreateImage();
            this.frameElement.IsVisible = false;
            this.iconElement.IsVisible = false;
        }

        internal bool IsFrameVisible { get { return frameElement.IsVisible; } }
        internal bool IsIconVisible { get { return iconElement.IsVisible; } }
        internal PreviewFrameColor CurrentFrameColor { get { return frameElement.Color; } }
        internal ItemAssetIdentity BoundIconAsset { get { return iconElement.BoundAsset; } }
        internal float FrameX { get { return frameElement.PositionOffsetX; } }
        internal float FrameY { get { return frameElement.PositionOffsetY; } }
        internal float FrameWidth { get { return frameElement.SizeOffsetX; } }
        internal float FrameHeight { get { return frameElement.SizeOffsetY; } }
        internal float IconX { get { return iconElement.PositionOffsetX; } }
        internal float IconY { get { return iconElement.PositionOffsetY; } }
        internal byte IconRotation { get { return iconElement.RotationAngle; } }

        internal void Mount()
        {
            if (isMounted) return;
            gridPanelContainer.AddChild(frameElement);
            topLevelContainer.AddChild(iconElement);
            isMounted = true;
        }

        internal void Unmount()
        {
            if (!isMounted) return;
            Hide();
            gridPanelContainer.RemoveChild(frameElement);
            topLevelContainer.RemoveChild(iconElement);
            isMounted = false;
        }

        public void ShowFrame(PreviewFrame frame)
        {
            if (!isMounted) Mount();
            frameElement.PositionOffsetX = frame.Candidate.X * frame.CellPixelSize;
            frameElement.PositionOffsetY = frame.Candidate.Y * frame.CellPixelSize;
            frameElement.SizeOffsetX = frame.Width * frame.CellPixelSize;
            frameElement.SizeOffsetY = frame.Height * frame.CellPixelSize;
            frameElement.Color = frame.Kind == PreviewFrameKind.ValidGreen
                ? PreviewFrameColor.ValidGreen
                : PreviewFrameColor.InvalidRed;
            frameElement.IsVisible = true;
        }

        public void ShowIcon(PreviewIcon icon)
        {
            if (!isMounted) Mount();
            iconElement.BoundAsset = icon.Asset;
            iconElement.PositionOffsetX = icon.ScreenX;
            iconElement.PositionOffsetY = icon.ScreenY;
            iconElement.RotationAngle = icon.Rotation;
            iconElement.IsVisible = true;
        }

        public void HideIcon()
        {
            iconElement.IsVisible = false;
            iconElement.BoundAsset = default(ItemAssetIdentity);
        }

        public void Hide()
        {
            frameElement.IsVisible = false;
            iconElement.IsVisible = false;
            iconElement.BoundAsset = default(ItemAssetIdentity);
        }
    }

    /// <summary>
    /// Official ClientUi feature component for Better Item Interaction (DEV-15B Frontend).
    /// Bridges Unturned inventory drag UI events with BUE's Evaluator, Presenter, and Adapter.
    /// </summary>
    internal sealed class BetterItemInteractionUiComponent : IClientUiFeatureComponent
    {
        private readonly InventoryPreviewPresenter previewPresenter;
        private readonly NativeInventoryInteractionAdapter nativeAdapter;
        private IInventorySurfaceContext currentSurface;
        private ContainerReference currentContainer;
        private uint currentSessionGeneration;
        private InventoryPreviewVisualSink previewSink;
        private bool isInventoryOpen;

        internal BetterItemInteractionUiComponent(
            InventoryPreviewPresenter previewPresenter,
            NativeInventoryInteractionAdapter nativeAdapter)
        {
            this.previewPresenter = previewPresenter ?? throw new ArgumentNullException(nameof(previewPresenter));
            this.nativeAdapter = nativeAdapter ?? throw new ArgumentNullException(nameof(nativeAdapter));
        }

        internal bool IsInventoryOpen { get { return isInventoryOpen; } }
        internal ContainerReference CurrentContainer { get { return currentContainer; } }
        internal uint CurrentSessionGeneration { get { return currentSessionGeneration; } }
        internal IInventorySurfaceContext CurrentSurface { get { return currentSurface; } }
        internal InventoryPreviewVisualSink PreviewSink { get { return previewSink; } }

        public void OnUiInitialized(IClientUiRoot root)
        {
            // UI initialized, root registered
        }

        public void OnInventoryOpened(IClientUiInventorySurface inventory)
        {
            isInventoryOpen = true;
            if (inventory is IInventorySurfaceContext surfaceContext)
            {
                currentSurface = surfaceContext;
                currentContainer = surfaceContext.CurrentContainer;
                currentSessionGeneration = surfaceContext.CurrentContainer.SessionGeneration;
                BindVisualSink(surfaceContext.TopLevelContainer, surfaceContext.GridPanelContainer);
            }
        }

        public void OnInventoryClosed()
        {
            isInventoryOpen = false;
            currentSurface = null;
            currentContainer = default(ContainerReference);
            currentSessionGeneration = 0;
            if (previewSink != null)
            {
                previewSink.Unmount();
                previewSink = null;
            }
            previewPresenter.EndDrag();
        }

        public void OnUiDestroyed()
        {
            OnInventoryClosed();
        }

        internal void BindVisualSink(IVisualContainer topLevel, IVisualContainer gridPanel)
        {
            if (previewSink != null)
            {
                previewSink.Unmount();
            }
            previewSink = new InventoryPreviewVisualSink(topLevel, gridPanel);
            previewSink.Mount();
        }

        internal void OnDragStarted(uint dragGeneration)
        {
            previewPresenter.BeginDrag(dragGeneration);
        }

        internal void OnDragUpdated(InventoryPreviewInput input)
        {
            if (!isInventoryOpen || previewSink == null) return;

            // Fail-closed guard: Reject updates directed to a stale container or session generation
            if (currentSessionGeneration != 0 &&
                (input.TargetContainer.SessionGeneration != currentSessionGeneration ||
                 input.TargetContainer.Page != currentContainer.Page ||
                 input.TargetContainer.Kind != currentContainer.Kind))
            {
                previewSink.Hide();
                return;
            }

            previewPresenter.Update(input, previewSink);
        }

        internal NativeDragAdapterOutcome OnDragReleased(NativeDragAdapterInput input, INativeInventoryDragActions nativeActions)
        {
            if (previewSink != null)
            {
                previewSink.Hide();
            }
            previewPresenter.EndDrag();
            return nativeAdapter.HandleRelease(input, nativeActions);
        }

        internal void OnDragCancelled()
        {
            if (previewSink != null)
            {
                previewSink.Hide();
            }
            previewPresenter.EndDrag();
        }

        internal bool TryCreatePreviewInput(uint dragGeneration, ItemGridPosition source, float pointerScreenX, float pointerScreenY,
            byte itemWidth, byte itemHeight, byte currentRotation, bool allowAutomaticRotation, float grabOffsetX, float grabOffsetY,
            ItemAssetIdentity itemAsset, out InventoryPreviewInput input)
        {
            input = default(InventoryPreviewInput);
            if (!isInventoryOpen || currentSurface == null) return false;

            input = new InventoryPreviewInput(dragGeneration, source, currentContainer, pointerScreenX, pointerScreenY,
                currentSurface.Viewport, currentSurface.CellPixelSize, currentSurface.UiScale,
                currentSurface.ScrollPixelsX, currentSurface.ScrollPixelsY, itemWidth, itemHeight, currentRotation,
                allowAutomaticRotation, grabOffsetX, grabOffsetY, itemAsset, currentSurface.Occupancy);
            return true;
        }
    }
}
