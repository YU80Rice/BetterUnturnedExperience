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
        float PositionScaleX { get; set; }
        float PositionScaleY { get; set; }
        float PositionOffsetX { get; set; }
        float PositionOffsetY { get; set; }
        float SizeOffsetX { get; set; }
        float SizeOffsetY { get; set; }
        byte RotationAngle { get; set; }
        bool CanRotate { get; set; }
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
    /// DEV-16D awaiting-projection bridge: submitted placements enter the
    /// visual-await state and converge when a native inventory snapshot
    /// matches the binding; the 2000ms budget only affects the visual wait.
    /// </summary>
    internal interface IInventoryProjectionSink
    {
        void OnProjectionSubmitted(ProjectionBinding binding);
        ProjectionConvergence OnNativeInventorySnapshot(NativeInventorySnapshot snapshot);
        void OnProjectionTimedOut();
        AwaitingProjectionState ProjectionState { get; }
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
        internal float IconScaleX { get { return iconElement.PositionScaleX; } }
        internal float IconScaleY { get { return iconElement.PositionScaleY; } }
        internal float IconWidth { get { return iconElement.SizeOffsetX; } }
        internal float IconHeight { get { return iconElement.SizeOffsetY; } }
        internal bool IconCanRotate { get { return iconElement.CanRotate; } }
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
            iconElement.CanRotate = true;
            iconElement.PositionScaleX = icon.UsesTopLevelAnchor ? icon.PositionScaleX : 0f;
            iconElement.PositionScaleY = icon.UsesTopLevelAnchor ? icon.PositionScaleY : 0f;
            iconElement.PositionOffsetX = icon.UsesTopLevelAnchor ? icon.PositionOffsetX : icon.ScreenX;
            iconElement.PositionOffsetY = icon.UsesTopLevelAnchor ? icon.PositionOffsetY : icon.ScreenY;
            if (icon.Width > 0f && icon.Height > 0f)
            {
                iconElement.SizeOffsetX = icon.Width;
                iconElement.SizeOffsetY = icon.Height;
            }
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
        private readonly BetterItemInteractionSettingsState settingsState;
        private readonly BetterItemInteractionLifecycle lifecycle;
        private readonly BetterItemInteractionRuntime runtime;
        private readonly AwaitingProjectionController awaitingProjection = new AwaitingProjectionController();
        private IInventoryProjectionSink projectionSink;
        private uint visualClockMs;
        private IInventorySurfaceContext currentSurface;
        private ContainerReference currentContainer;
        private uint currentSessionGeneration;
        private InventoryPreviewVisualSink previewSink;
        private bool isInventoryOpen;
        private bool satelliteAvailable = true;
        private bool headless;

        internal IInventoryProjectionSink ProjectionSink { set { projectionSink = value; } }

        internal void Tick(uint nowMilliseconds)
        {
            visualClockMs = nowMilliseconds;
            if (awaitingProjection.Tick(nowMilliseconds) && projectionSink != null)
            {
                // Visual budget expired: the native projection may still land
                // later, but the visual wait stops here. No fake rollback.
                projectionSink.OnProjectionTimedOut();
            }
        }

        internal BetterItemInteractionUiComponent(
            InventoryPreviewPresenter previewPresenter,
            NativeInventoryInteractionAdapter nativeAdapter)
            : this(previewPresenter, nativeAdapter, new BetterItemInteractionSettingsState())
        {
        }

        internal BetterItemInteractionUiComponent(
            InventoryPreviewPresenter previewPresenter,
            NativeInventoryInteractionAdapter nativeAdapter,
            BetterItemInteractionSettingsState settingsState)
        {
            this.previewPresenter = previewPresenter ?? throw new ArgumentNullException(nameof(previewPresenter));
            this.nativeAdapter = nativeAdapter ?? throw new ArgumentNullException(nameof(nativeAdapter));
            this.settingsState = settingsState ?? throw new ArgumentNullException(nameof(settingsState));
            lifecycle = new BetterItemInteractionLifecycle();
            runtime = new BetterItemInteractionRuntime(settingsState, lifecycle);
            runtime.RegisterCleanup(CleanupUiAndDrag);
        }

        internal bool IsInventoryOpen { get { return isInventoryOpen; } }
        internal ContainerReference CurrentContainer { get { return currentContainer; } }
        internal ContainerReference LastDispatchedContainer { get { return currentContainer; } }
        internal uint CurrentSessionGeneration { get { return currentSessionGeneration; } }
        internal IInventorySurfaceContext CurrentSurface { get { return currentSurface; } }
        internal InventoryPreviewVisualSink PreviewSink { get { return previewSink; } }
        internal BetterItemInteractionLifecycle Lifecycle { get { return lifecycle; } }
        internal BetterItemInteractionSettingsState SettingsState { get { return settingsState; } }
        internal bool EnhancedDragActive { get { return runtime.EnhancedDragActive; } }
        internal bool LifecycleCanRun { get { return lifecycle.CanRun; } }
        internal bool PreviewSinkBound { get { return previewSink != null; } }

        internal void ApplySettingsSnapshot(FeatureSettingsSnapshot snapshot)
        {
            if (!settingsState.ApplySnapshot(snapshot)) return;
            if (!runtime.EnhancedDragActive && !settingsState.Enabled)
            {
                lifecycle.Disable();
                CleanupUiAndDrag();
            }
        }

        internal void SetClientUiSatelliteAvailable(bool available, bool headless)
        {
            satelliteAvailable = available;
            this.headless = headless;
            lifecycle.SetPresentationAvailable(available, headless);
            if (!available || headless) runtime.Isolate();
        }

        internal void EnterSafeMode()
        {
            runtime.EnterSafeMode();
        }

        internal void RegisterCleanupResult(Func<bool> cleanupAction)
        {
            runtime.RegisterCleanupResult(cleanupAction);
        }

        // GPT watermark: all native preview read failures enter this one
        // feature-local isolation seam. Cleanup unmounts pooled visuals and
        // disables enhanced drag while vanilla input remains untouched.
        internal void IsolatePreviewFailure()
        {
            runtime.Isolate();
            HidePreview();
        }

        public void OnUiInitialized(IClientUiRoot root)
        {
            OnUiInitialized(root, true, false);
        }

        internal void OnUiInitialized(IClientUiRoot root, bool satelliteAvailable, bool headless)
        {
            this.satelliteAvailable = satelliteAvailable;
            this.headless = headless;
            runtime.Start(true, satelliteAvailable);
            lifecycle.SetPresentationAvailable(satelliteAvailable, headless);
        }

        public void OnInventoryOpened(IClientUiInventorySurface inventory)
        {
            if (lifecycle.State == FeatureState.Discovered) runtime.Start(true, satelliteAvailable);
            CleanupUiAndDrag();
            if (lifecycle.SafeMode || !lifecycle.CanRun || !satelliteAvailable || headless)
            {
                isInventoryOpen = false;
                return;
            }
            isInventoryOpen = true;
            if (inventory is IInventorySurfaceContext surfaceContext)
            {
                currentSurface = surfaceContext;
                currentContainer = surfaceContext.CurrentContainer;
                currentSessionGeneration = surfaceContext.CurrentContainer.SessionGeneration;
                BindVisualSink(surfaceContext.TopLevelContainer, surfaceContext.GridPanelContainer);
            }
            else
            {
                OnInventoryClosed();
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
            runtime.EndDrag();
            previewPresenter.EndDrag();
        }

        public void OnUiDestroyed()
        {
            runtime.Stop();
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
            OnDragStarted(dragGeneration, default(ItemAssetIdentity));
        }

        internal void OnDragStarted(uint dragGeneration, ItemAssetIdentity dragAsset)
        {
            currentDragAsset = dragAsset;
            runtime.BeginDrag(dragGeneration);
            if (runtime.EnhancedDragActive && previewSink == null && currentSurface != null && satelliteAvailable && !headless)
            {
                BindVisualSink(currentSurface.TopLevelContainer, currentSurface.GridPanelContainer);
                isInventoryOpen = true;
            }
            if (runtime.EnhancedDragActive) previewPresenter.BeginDrag(dragGeneration);
            else previewPresenter.EndDrag();
        }

        private ItemAssetIdentity currentDragAsset;

        internal void OnDragUpdated(InventoryPreviewInput input)
        {
            if (!isInventoryOpen || previewSink == null || !runtime.EnhancedDragActive || !lifecycle.CanRun)
            {
                if (previewSink != null) previewSink.Hide();
                return;
            }

            // Fail-closed guard: Reject updates directed to a stale container or session generation
            if (currentSessionGeneration != 0 &&
                (input.TargetContainer.SessionGeneration != currentSessionGeneration ||
                 input.TargetContainer.Page != currentContainer.Page ||
                 input.TargetContainer.Kind != currentContainer.Kind))
            {
                previewSink.Hide();
                return;
            }

            try
            {
                previewPresenter.Update(input, previewSink);
            }
            catch (Exception)
            {
                runtime.Isolate();
                if (previewSink != null) previewSink.Hide();
            }
        }

        internal ItemPlacementPreview LastPreview { get { return previewPresenter.LastPreview; } }

        internal NativeDragAdapterOutcome OnDragReleased(NativeDragAdapterInput input, INativeInventoryDragActions nativeActions)
        {
            if (previewSink != null)
            {
                previewSink.Hide();
            }
            if (!runtime.EnhancedDragActive)
            {
                runtime.EndDrag();
                previewPresenter.EndDrag();
                return NativeDragAdapterOutcome.PassThrough;
            }
            previewPresenter.EndDrag();
            try
            {
                var outcome = nativeAdapter.HandleRelease(input, nativeActions);
                runtime.EndDrag();
                if (outcome == NativeDragAdapterOutcome.Submitted)
                {
                    var fingerprint = new InventoryItemFingerprint(currentDragAsset, (byte)input.Preview.Width, (byte)input.Preview.Height, input.Preview.Candidate.Rotation);
                    // No live session (SessionGeneration==0) means there is no
                    // container to converge against; skip the awaiting state.
                    if (currentContainer.SessionGeneration != 0)
                    {
                        var binding = new ProjectionBinding(input.DragGeneration, currentContainer, fingerprint);
                        // The awaiting controller lives in this component; the sink
                        // is observability only (it must never gate convergence).
                        awaitingProjection.Begin(binding, visualClockMs);
                        projectionSink?.OnProjectionSubmitted(binding);
                    }
                }
                return outcome;
            }
            catch (Exception)
            {
                runtime.Isolate();
                return NativeDragAdapterOutcome.PassThrough;
            }
        }

        internal ProjectionConvergence OnNativeInventorySnapshot(NativeInventorySnapshot snapshot)
        {
            // The awaiting controller decides convergence; the sink only
            // observes (its return value is ignored by design).
            var convergence = awaitingProjection.Apply(snapshot);
            projectionSink?.OnNativeInventorySnapshot(snapshot);
            if (convergence == ProjectionConvergence.Converged)
            {
                // Native projection landed: the placement is authoritative now.
                currentSessionGeneration = snapshot.Container.SessionGeneration;
            }
            return convergence;
        }

        internal void OnDragCancelled()
        {
            if (previewSink != null)
            {
                previewSink.Hide();
            }
            runtime.EndDrag();
            previewPresenter.EndDrag();
        }

        internal void HidePreview()
        {
            if (previewSink != null) previewSink.Hide();
            previewPresenter.HidePreview();
        }

        private void CleanupUiAndDrag()
        {
            if (previewSink != null)
            {
                previewSink.Unmount();
                previewSink = null;
            }
            isInventoryOpen = false;
            currentSurface = null;
            currentContainer = default(ContainerReference);
            currentSessionGeneration = 0;
            previewPresenter.EndDrag();
        }

        internal bool TryCreatePreviewInput(uint dragGeneration, ItemGridPosition source, float pointerScreenX, float pointerScreenY,
            byte itemWidth, byte itemHeight, byte currentRotation, bool allowAutomaticRotation, float grabOffsetX, float grabOffsetY,
            ItemAssetIdentity itemAsset, out InventoryPreviewInput input)
        {
            return TryCreatePreviewInputCore(dragGeneration, source, pointerScreenX, pointerScreenY, itemWidth, itemHeight,
                currentRotation, allowAutomaticRotation, grabOffsetX, grabOffsetY, itemAsset,
                false, float.NaN, float.NaN, float.NaN, float.NaN,
                InventoryPointerCoordinateSpace.Screen, out input);
        }

        internal bool TryCreatePreviewInput(uint dragGeneration, ItemGridPosition source, float pointerScreenX, float pointerScreenY,
            byte itemWidth, byte itemHeight, byte currentRotation, bool allowAutomaticRotation, float grabOffsetX, float grabOffsetY,
            ItemAssetIdentity itemAsset, float topLevelPointerScaleX, float topLevelPointerScaleY,
            float nativeDragPivotX, float nativeDragPivotY, out InventoryPreviewInput input)
        {
            return TryCreatePreviewInputCore(dragGeneration, source, pointerScreenX, pointerScreenY, itemWidth, itemHeight,
                currentRotation, allowAutomaticRotation, grabOffsetX, grabOffsetY, itemAsset,
                true, topLevelPointerScaleX, topLevelPointerScaleY, nativeDragPivotX, nativeDragPivotY,
                InventoryPointerCoordinateSpace.GridContentLocal, out input);
        }

        internal bool TryCreatePreviewInput(uint dragGeneration, ItemGridPosition source, float pointerScreenX, float pointerScreenY,
            byte itemWidth, byte itemHeight, byte currentRotation, bool allowAutomaticRotation, float grabOffsetX, float grabOffsetY,
            ItemAssetIdentity itemAsset, float topLevelPointerScaleX, float topLevelPointerScaleY,
            float nativeDragPivotX, float nativeDragPivotY, InventoryPointerCoordinateSpace pointerCoordinateSpace,
            out InventoryPreviewInput input)
        {
            return TryCreatePreviewInputCore(dragGeneration, source, pointerScreenX, pointerScreenY, itemWidth, itemHeight,
                currentRotation, allowAutomaticRotation, grabOffsetX, grabOffsetY, itemAsset,
                pointerCoordinateSpace == InventoryPointerCoordinateSpace.Screen,
                topLevelPointerScaleX, topLevelPointerScaleY, nativeDragPivotX, nativeDragPivotY,
                pointerCoordinateSpace, out input);
        }

        private bool TryCreatePreviewInputCore(uint dragGeneration, ItemGridPosition source, float pointerScreenX, float pointerScreenY,
            byte itemWidth, byte itemHeight, byte currentRotation, bool allowAutomaticRotation, float grabOffsetX, float grabOffsetY,
            ItemAssetIdentity itemAsset, bool pointerAlreadyIncludesScroll, float topLevelPointerScaleX, float topLevelPointerScaleY,
            float nativeDragPivotX, float nativeDragPivotY, InventoryPointerCoordinateSpace pointerCoordinateSpace,
            out InventoryPreviewInput input)
        {
            input = default(InventoryPreviewInput);
            if (!isInventoryOpen || currentSurface == null) return false;

            var scrollPixelsX = pointerAlreadyIncludesScroll ? 0f : currentSurface.ScrollPixelsX;
            var scrollPixelsY = pointerAlreadyIncludesScroll ? 0f : currentSurface.ScrollPixelsY;
            input = new InventoryPreviewInput(dragGeneration, source, currentContainer, pointerScreenX, pointerScreenY,
                currentSurface.Viewport, currentSurface.CellPixelSize, currentSurface.UiScale,
                scrollPixelsX, scrollPixelsY, itemWidth, itemHeight, currentRotation,
                runtime.EnhancedDragActive && runtime.ActivePolicy.AutoRotate && allowAutomaticRotation,
                grabOffsetX, grabOffsetY, itemAsset, topLevelPointerScaleX, topLevelPointerScaleY,
                nativeDragPivotX, nativeDragPivotY, pointerCoordinateSpace, currentSurface.Occupancy);
            return true;
        }
    }
}
