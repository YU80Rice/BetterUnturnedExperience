using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Bii
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
    // DEV-V6-04: the pre-move IClientUiInventorySurface marker retired with the
    // UI composition registry (its sole consumer was this subsystem).
    internal interface IInventorySurfaceContext
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

    internal interface IInventoryPointerSurfaceContext : IInventorySurfaceContext
    {
        bool TryGetLocalPointerPixels(out float x, out float y);
    }

    /// <summary>
    /// Native surfaces may provide a drag-specific immutable occupancy view.
    /// The provider is an internal seam so the pure Bii layer never needs
    /// to reference ItemJar or other Unity/Unturned types.
    /// </summary>
    internal interface INativeInventoryOccupancyProvider
    {
        bool TryCreateOccupancyForDrag(ContainerReference sourceContainer, ContainerReference targetContainer,
            ItemGridPosition source, byte itemWidth, byte itemHeight, byte sourceRotation,
            ItemAssetIdentity sourceAsset, out IGridOccupancyView occupancy);
        void InvalidateOccupancy();
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
        // FB1b: element references are REBUILDABLE — the native Glazier pool
        // can release the backing uGUI components behind BUE's back (machine
        // 20260909_001648: set_BackgroundColor NRE'd 60 consecutive frames on
        // a pooled box whose imageComponent was nulled), so a failed write
        // rebuilds both elements fresh and retries once.
        private IVisualElement frameElement;
        private IVisualElement iconElement;
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

        // DEV-V2-24 F-B1: a third-party rebuild of the native items panel
        // (e.g. the listen-host projection repair clearing panel children)
        // can remove the mounted preview elements behind BUE's back. Each new
        // drag start re-asserts the children so one rebuild cannot leave the
        // preview lane invisibly dead for the rest of the session.
        internal void EnsureMounted()
        {
            if (!isMounted) { Mount(); return; }
            // Best-effort re-assert: a native container may reject the
            // remove/add of a child a third-party rebuild already detached.
            // Per the never-throw host-event convention, this degrades to
            // "no remount this drag" instead of escaping into the drag tick.
            try
            {
                gridPanelContainer.RemoveChild(frameElement);
                gridPanelContainer.AddChild(frameElement);
                topLevelContainer.RemoveChild(iconElement);
                topLevelContainer.AddChild(iconElement);
            }
            catch (Exception) { }
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
            // FB1b: one rebuild-and-retry on a native write fault — a second
            // consecutive fault propagates to the preview fault gate. If the
            // ICON write rebuilds mid-drag, the frame visual re-appears on
            // the next poll frame (at most one frame without the frame box).
            // Deliberately NOT a shared delegate helper: the sink hot path is
            // under the zero-allocation contract and a closure would allocate
            // per frame (client-ui tests sink allocation guard).
            try { ApplyFrame(frame); }
            catch (Exception)
            {
                RebuildElements();
                ApplyFrame(frame);
            }
        }

        public void ShowIcon(PreviewIcon icon)
        {
            // FB1b: same rebuild-and-retry contract as ShowFrame (duplicated
            // inline for the same zero-allocation reason).
            try { ApplyIcon(icon); }
            catch (Exception)
            {
                RebuildElements();
                ApplyIcon(icon);
            }
        }

        private void ApplyFrame(PreviewFrame frame)
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

        // FB1b: fresh elements from the live container factories replace any
        // natively released backing, then both are re-mounted. Removal of the
        // old elements is best-effort (a pooled element may already be
        // detached natively — never-throw, per the host-event convention);
        // isMounted flips true only AFTER both children are in, mirroring
        // Mount()'s ordering.
        private void RebuildElements()
        {
            try
            {
                gridPanelContainer.RemoveChild(frameElement);
                topLevelContainer.RemoveChild(iconElement);
            }
            catch (Exception) { }
            frameElement = gridPanelContainer.CreateBox();
            iconElement = topLevelContainer.CreateImage();
            frameElement.IsVisible = false;
            iconElement.IsVisible = false;
            gridPanelContainer.AddChild(frameElement);
            topLevelContainer.AddChild(iconElement);
            isMounted = true;
        }

        private void ApplyIcon(PreviewIcon icon)
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
    /// Official Better Item Interaction feature component (DEV-15B Frontend).
    /// Bridges Unturned inventory drag UI events with BUE's Evaluator, Presenter, and Adapter.
    /// </summary>
    internal sealed class BetterItemInteractionUiComponent
    {
        private readonly InventoryPreviewPresenter previewPresenter;
        private readonly NativeInventoryInteractionAdapter nativeAdapter;
        // DEV-V6-04: the settings single-source stays in the UI project; the
        // component pulls a snapshot through this injected reader at drag/decision
        // time (the pre-move state was the SAME shared instance, so reading the
        // latest snapshot is behavior-equivalent). Null reader = the pre-move
        // default policy (enabled, auto-rotate).
        private readonly Func<FeatureSettingsSnapshot> settingsSnapshot;
        // DEV-V6-04: the preview fault diagnostics used to ride the UI
        // composition's static DiagnosticSink; the mouths are injected now
        // (error=true goes to the error mouth, otherwise runtime), unbound =
        // honest swallow, same family as the 02B/C/D absent-seam contract.
        private readonly Action<string> previewLogRuntime;
        private readonly Action<string> previewLogError;
        private readonly BetterItemInteractionLifecycle lifecycle;
        private readonly BetterItemInteractionRuntime runtime;
        private readonly AwaitingProjectionController awaitingProjection = new AwaitingProjectionController();
        private IInventoryProjectionSink projectionSink;
        private uint visualClockMs;
        private IInventorySurfaceContext currentSurface;
        private ContainerReference currentContainer;
        private uint currentSessionGeneration;
        private uint currentDragGeneration;
        private ContainerReference dragOriginContainer;
        private bool dragSourcePassThrough;
        // Both supported native inventory pages are live while the dashboard
        // is open. The active surface is selected per drag/source/target page;
        // registering a second page must not discard the first one.
        private readonly Dictionary<byte, IInventorySurfaceContext> liveSurfaces =
            new Dictionary<byte, IInventorySurfaceContext>();
        // DEV-16F: U3-SDK page model — 2=Hands, 3=Backpack, 4=Vest, 5=Shirt,
        // 6=Pants, 7=Storage/trunk. Only these player grid pages are live
        // TARGET surfaces. AREA(8) and equipment slots (0/1) are not grids and
        // are never enhanced targets (they can still be pickup SOURCES; R2).
        private static readonly byte[] SupportedLiveSurfacePages = { 2, 3, 4, 5, 6, 7 };
        private IGridOccupancyView activeDragOccupancy;
        private IInventorySurfaceContext activeDragOccupancySurface;
        private ContainerReference activeDragOccupancySourceContainer;
        private ContainerReference activeDragOccupancyTargetContainer;
        private ItemGridPosition activeDragOccupancySource;
        private byte activeDragOccupancyWidth;
        private byte activeDragOccupancyHeight;
        private byte activeDragOccupancyRotation;
        private ItemAssetIdentity activeDragOccupancyAsset;
        private bool hasActiveDragOccupancy;
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
            : this(previewPresenter, nativeAdapter, null)
        {
        }

        internal BetterItemInteractionUiComponent(
            InventoryPreviewPresenter previewPresenter,
            NativeInventoryInteractionAdapter nativeAdapter,
            Func<FeatureSettingsSnapshot> settingsSnapshot,
            Action<string> previewLogRuntime = null,
            Action<string> previewLogError = null)
        {
            this.previewPresenter = previewPresenter ?? throw new ArgumentNullException(nameof(previewPresenter));
            this.nativeAdapter = nativeAdapter ?? throw new ArgumentNullException(nameof(nativeAdapter));
            this.settingsSnapshot = settingsSnapshot;
            this.previewLogRuntime = previewLogRuntime;
            this.previewLogError = previewLogError;
            lifecycle = new BetterItemInteractionLifecycle();
            runtime = new BetterItemInteractionRuntime(CaptureCurrentPolicy, lifecycle);
            runtime.RegisterCleanup(CleanupUiAndDrag);
        }

        // The pre-move policy source was BetterItemInteractionSettingsState
        // .CaptureForDrag on the shared instance; the same three facts ride the
        // snapshot now (enabled stays constantly true by DEV-V4-04 retirement).
        private BetterItemInteractionDragPolicy CaptureCurrentPolicy()
        {
            if (settingsSnapshot == null)
            {
                return new BetterItemInteractionDragPolicy(true, true, 0);
            }
            var snapshot = settingsSnapshot();
            if (snapshot.RevisionScope != SettingRevisionScope.ClientPreference
                || !string.Equals(snapshot.Feature.Value, BiiPolicyDefaults.FeatureId, StringComparison.Ordinal))
            {
                return new BetterItemInteractionDragPolicy(true, true, 0);
            }
            var autoRotate = true;
            if (snapshot.Entries != null)
            {
                for (var index = 0; index < snapshot.Entries.Count; index++)
                {
                    var entry = snapshot.Entries[index];
                    if (entry.Authority != SettingAuthority.ClientLocal || entry.EffectiveValue.Kind != SettingKind.Toggle) continue;
                    if (string.Equals(entry.SettingId, "AutoRotate", StringComparison.OrdinalIgnoreCase)) autoRotate = entry.EffectiveValue.Boolean;
                }
            }
            return new BetterItemInteractionDragPolicy(true, autoRotate, snapshot.Revision);
        }

        internal bool IsInventoryOpen { get { return isInventoryOpen; } }
        internal ContainerReference CurrentContainer { get { return currentContainer; } }
        internal ContainerReference LastDispatchedContainer { get { return currentContainer; } }
        internal uint CurrentSessionGeneration { get { return currentSessionGeneration; } }
        internal IInventorySurfaceContext CurrentSurface { get { return currentSurface; } }
        internal InventoryPreviewVisualSink PreviewSink { get { return previewSink; } }
        internal BetterItemInteractionLifecycle Lifecycle { get { return lifecycle; } }
        internal bool EnhancedDragActive { get { return runtime.EnhancedDragActive; } }
        internal bool LifecycleCanRun { get { return lifecycle.CanRun; } }
        internal bool PreviewSinkBound { get { return previewSink != null; } }
        internal int LiveSurfaceCount { get { return liveSurfaces.Count; } }
        internal bool DragSourcePassThrough { get { return dragSourcePassThrough; } }
        internal uint CurrentDragGeneration { get { return currentDragGeneration; } }
        internal ContainerReference DragOriginContainer { get { return dragOriginContainer; } }
        internal bool HasActiveDragOccupancy { get { return hasActiveDragOccupancy; } }

        internal static bool IsSupportedEnhancedPage(byte page)
        {
            // DEV-16F: every player grid page is an enhanced TARGET surface —
            // 2=Hands, 3=Backpack, 4=Vest, 5=Shirt, 6=Pants, 7=Storage/trunk.
            // AREA(8) and equipment slots (0/1) are never enhanced targets
            // (they can still be pickup sources; R2).
            return page >= 2 && page <= 7;
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
        internal bool IsolatePreviewFailureResult()
        {
            var cleanupSucceeded = runtime.Isolate();
            // A native hook/geometry failure is a feature-local presentation
            // failure, not a headless runtime.  Project the degraded state
            // after isolation so the management surface cannot continue to
            // advertise the preview as available while vanilla drag remains
            // the active fallback.
            lifecycle.SetPresentationAvailable(false, headless);
            HidePreview();
            return cleanupSucceeded;
        }

        internal void IsolatePreviewFailure()
        {
            IsolatePreviewFailureResult();
        }

        private void EmitPreviewDiagnostic(string line, bool error)
        {
            var sink = error ? previewLogError : previewLogRuntime;
            if (sink != null) sink(line);
        }

        internal void OnUiInitialized(bool satelliteAvailable, bool headless)
        {
            this.satelliteAvailable = satelliteAvailable;
            this.headless = headless;
            runtime.Start(true, satelliteAvailable);
            lifecycle.SetPresentationAvailable(satelliteAvailable, headless);
        }

        internal void OnInventoryOpened(IInventorySurfaceContext surfaceContext)
        {
            if (lifecycle.State == FeatureState.Discovered) runtime.Start(true, satelliteAvailable);
            if (surfaceContext == null)
            {
                OnInventoryClosed();
                return;
            }
            RegisterInventorySurface(surfaceContext, true);
        }

        // Registers one live native page without closing other supported pages.
        // The caller may make the surface active when it is the current target
        // of a drag; registration itself never ends an in-flight drag.
        internal bool RegisterInventorySurface(IInventorySurfaceContext surfaceContext, bool makeActive)
        {
            if (surfaceContext == null || lifecycle.SafeMode || !lifecycle.CanRun || !satelliteAvailable || headless)
            {
                return false;
            }
            if (!IsSupportedEnhancedPage(surfaceContext.CurrentContainer.Page))
            {
                // Unknown/equipment/AREA pages never become an enhanced target
                // surface. Their native grid remains entirely in pass-through.
                return false;
            }

            var rearmDrag = runtime.EnhancedDragActive;
            var rearmGeneration = currentDragGeneration;
            if (isInventoryOpen && liveSurfaces.Count > 0 && !HasMatchingSession(surfaceContext.CurrentContainer))
            {
                CleanupUiAndDrag();
            }

            isInventoryOpen = true;
            liveSurfaces[surfaceContext.CurrentContainer.Page] = surfaceContext;
            if (makeActive || currentSurface == null ||
                currentSurface.CurrentContainer.Page == surfaceContext.CurrentContainer.Page)
            {
                ActivateSurface(surfaceContext);
            }

            if (rearmDrag && rearmGeneration != 0)
            {
                previewPresenter.BeginDrag(rearmGeneration);
            }
            return true;
        }

        internal bool TrySelectSurfaceForPage(byte page)
        {
            IInventorySurfaceContext surface;
            if (!liveSurfaces.TryGetValue(page, out surface)) return false;
            ActivateSurface(surface);
            return true;
        }

        internal bool TryGetLiveSurface(byte page, out IInventorySurfaceContext surface)
        {
            return liveSurfaces.TryGetValue(page, out surface);
        }

        internal bool TrySelectSurfaceForPointer(out IInventorySurfaceContext surface, out float localX, out float localY)
        {
            surface = null;
            localX = 0f;
            localY = 0f;
            // Dictionary order is intentionally not used as a routing rule.
            // Supported page order is stable and gives the earliest grid page
            // (Hands) precedence if native panels overlap at a boundary.
            for (var index = 0; index < SupportedLiveSurfacePages.Length; index++)
            {
                IInventorySurfaceContext candidate;
                if (!liveSurfaces.TryGetValue(SupportedLiveSurfacePages[index], out candidate)) continue;
                var pointerSurface = candidate as IInventoryPointerSurfaceContext;
                if (pointerSurface == null) continue;
                if (!pointerSurface.TryGetLocalPointerPixels(out localX, out localY)) continue;
                surface = candidate;
                ActivateSurface(candidate);
                return true;
            }
            return false;
        }

        private bool HasMatchingSession(ContainerReference container)
        {
            foreach (var surface in liveSurfaces.Values)
            {
                if (surface.CurrentContainer.SessionGeneration == container.SessionGeneration &&
                    surface.CurrentContainer.SessionGeneration != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void ActivateSurface(IInventorySurfaceContext surfaceContext)
        {
            if (ReferenceEquals(currentSurface, surfaceContext) && previewSink != null)
            {
                currentContainer = surfaceContext.CurrentContainer;
                currentSessionGeneration = surfaceContext.CurrentContainer.SessionGeneration;
                return;
            }

            currentSurface = surfaceContext;
            currentContainer = surfaceContext.CurrentContainer;
            currentSessionGeneration = surfaceContext.CurrentContainer.SessionGeneration;
            BindVisualSink(surfaceContext.TopLevelContainer, surfaceContext.GridPanelContainer);
        }

        internal void OnInventoryClosed()
        {
            HidePreview();
            isInventoryOpen = false;
            previewUpdateFaultFrames = 0;
            currentSurface = null;
            currentContainer = default(ContainerReference);
            currentSessionGeneration = 0;
            liveSurfaces.Clear();
            if (previewSink != null)
            {
                previewSink.Unmount();
                previewSink = null;
            }
            runtime.EndDrag();
            previewPresenter.EndDrag();
            currentDragGeneration = 0;
            dragOriginContainer = default(ContainerReference);
            dragSourcePassThrough = false;
            ClearActiveDragOccupancy();
        }

        // GPT watermark: DEV-16D-R13 page-local rebuild handling. A native
        // Backpack or Storage surface may be recreated independently while
        // the other supported page remains live. Remove only the rebuilt page,
        // reselect a surviving page when possible, and clear any preview that
        // was anchored to the removed hierarchy.
        internal bool DiscardInventorySurface(byte page)
        {
            var hadActiveDrag = currentDragGeneration != 0 || runtime.EnhancedDragActive;
            if (!liveSurfaces.Remove(page)) return false;

            if (currentSurface != null && currentSurface.CurrentContainer.Page == page)
            {
                IInventorySurfaceContext replacement = null;
                for (var index = 0; index < SupportedLiveSurfacePages.Length; index++)
                {
                    if (liveSurfaces.TryGetValue(SupportedLiveSurfacePages[index], out replacement)) break;
                }

                if (replacement != null)
                {
                    ActivateSurface(replacement);
                    isInventoryOpen = true;
                }
                else
                {
                    if (previewSink != null)
                    {
                        previewSink.Unmount();
                        previewSink = null;
                    }
                    currentSurface = null;
                    currentContainer = default(ContainerReference);
                    currentSessionGeneration = 0;
                    isInventoryOpen = false;
                }
                HidePreview();
                ClearActiveDragOccupancy();
            }

            if (hadActiveDrag)
            {
                // A surface rebuild invalidates both source and target
                // dependencies, including cross-page drags where the removed
                // page was not the currently selected target.
                runtime.EndDrag();
                previewPresenter.EndDrag();
                currentDragGeneration = 0;
                dragOriginContainer = default(ContainerReference);
                dragSourcePassThrough = true;
                HidePreview();
                ClearActiveDragOccupancy();
            }

            return true;
        }

        internal void OnUiDestroyed()
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
            OnDragStarted(dragGeneration, dragAsset, default(ItemGridPosition));
        }

        internal void OnDragStarted(uint dragGeneration, ItemAssetIdentity dragAsset, ItemGridPosition source)
        {
            if (source.Page != 0 && TrySelectSurfaceForPage(source.Page))
            {
                // Source-page selection ensures a Backpack <-> Storage drag
                // captures the correct origin container before target routing.
            }
            currentDragGeneration = dragGeneration;
            currentDragAsset = dragAsset;
            // DEV-16F R2 source decoupling: the source page no longer decides
            // takeover. Any pickup origin (grid 2-7, equipment 0/1, ground
            // AREA=8) enters the enhanced drag flow; the TARGET grid decides
            // whether a preview renders (only pages 2-7 are live surfaces).
            // Only a malformed page beyond AREA is pass-through.
            dragSourcePassThrough = !nativeAdapter.IsEnhancedSourcePage(source.Page);
            dragOriginContainer = source.Page == currentContainer.Page && currentContainer.SessionGeneration != 0
                ? currentContainer : default(ContainerReference);
            ClearActiveDragOccupancy();
            // A new drag generation scopes the preview-update fault streak:
            // 59 absorbed faults in one drag must never leak into the next
            // drag's first frame (the streak measures CONSECUTIVE frames of
            // one drag, not cumulative history).
            previewUpdateFaultFrames = 0;
            if (dragSourcePassThrough)
            {
                runtime.EndDrag();
                previewPresenter.EndDrag();
                HidePreview();
                return;
            }
            runtime.BeginDrag(dragGeneration);
            if (runtime.EnhancedDragActive && previewSink == null && currentSurface != null && satelliteAvailable && !headless)
            {
                BindVisualSink(currentSurface.TopLevelContainer, currentSurface.GridPanelContainer);
                isInventoryOpen = true;
            }
            else if (runtime.EnhancedDragActive && previewSink != null)
            {
                previewSink.EnsureMounted();
            }
            if (runtime.EnhancedDragActive) previewPresenter.BeginDrag(dragGeneration);
            else previewPresenter.EndDrag();
        }

        private ItemAssetIdentity currentDragAsset;

        // DEV-V2-24 F-B1: the preview-update lane shares the surface lane's
        // fault semantics — a single transient fault (e.g. a third-party
        // listen-host panel repair racing the drag tick) is absorbed with a
        // one-shot diagnostic; only 60 CONSECUTIVE fault frames isolate, and
        // any healthy frame resets the streak.
        private const int PreviewUpdateFaultIsolationThreshold = 60;
        private int previewUpdateFaultFrames;

        internal void OnDragUpdated(InventoryPreviewInput input)
        {
            if (dragSourcePassThrough || !isInventoryOpen || previewSink == null || !runtime.EnhancedDragActive || !lifecycle.CanRun)
            {
                HidePreview();
                return;
            }

            // Fail-closed guard: Reject updates directed to a stale container or session generation
            if (currentSessionGeneration != 0 &&
                (input.TargetContainer.SessionGeneration != currentSessionGeneration ||
                 input.TargetContainer.Page != currentContainer.Page ||
                 input.TargetContainer.Kind != currentContainer.Kind))
            {
                HidePreview();
                return;
            }

            try
            {
                previewPresenter.Update(input, previewSink);
                if (previewUpdateFaultFrames != 0)
                {
                    previewUpdateFaultFrames = 0;
                    EmitPreviewDiagnostic("[BUE-DRAG] event=preview-update-recovered diagnosticId=BUE-DRAG-004", false);
                }
            }
            catch (Exception error)
            {
                previewUpdateFaultFrames++;
                // DEV-V2-24 F-B1b: name the thrower — the first stack frame of
                // a swallowed NRE is the difference between a named fix and
                // another blind repro round.
                var stackTrace = error.StackTrace ?? string.Empty;
                var newlineIndex = stackTrace.IndexOf('\n');
                var firstFrame = (newlineIndex > 0 ? stackTrace.Substring(0, newlineIndex) : stackTrace).Trim();
                if (previewUpdateFaultFrames >= PreviewUpdateFaultIsolationThreshold)
                {
                    EmitPreviewDiagnostic("[BUE-DRAG] event=preview-update-isolated"
                        + " consecutive=" + previewUpdateFaultFrames
                        + " errorType=" + error.GetType().Name
                        + " message=" + error.Message
                        + " stack=" + firstFrame
                        + " diagnosticId=BUE-DRAG-004", true);
                    runtime.Isolate();
                    HidePreview();
                    return;
                }
                if (previewUpdateFaultFrames == 1)
                {
                    EmitPreviewDiagnostic("[BUE-DRAG] event=preview-update-threw"
                        + " consecutive=" + previewUpdateFaultFrames
                        + " errorType=" + error.GetType().Name
                        + " message=" + error.Message
                        + " stack=" + firstFrame
                        + " diagnosticId=BUE-DRAG-004", false);
                }
                HidePreview();
                return;
            }
        }

        internal ItemPlacementPreview LastPreview { get { return previewPresenter.LastPreview; } }

        internal NativeDragAdapterOutcome OnDragReleased(NativeDragAdapterInput input, INativeInventoryDragActions nativeActions)
        {
            HidePreview();
            if (!runtime.EnhancedDragActive)
            {
                runtime.EndDrag();
                previewPresenter.EndDrag();
                currentDragGeneration = 0;
                dragOriginContainer = default(ContainerReference);
                dragSourcePassThrough = false;
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
                currentDragGeneration = 0;
                dragOriginContainer = default(ContainerReference);
                dragSourcePassThrough = false;
                return outcome;
            }
            catch (Exception)
            {
                runtime.Isolate();
                currentDragGeneration = 0;
                dragOriginContainer = default(ContainerReference);
                dragSourcePassThrough = false;
                return NativeDragAdapterOutcome.PassThrough;
            }
        }

        internal ProjectionConvergence OnNativeInventorySnapshot(NativeInventorySnapshot snapshot)
        {
            // The awaiting controller decides convergence; the sink only
            // observes (its return value is ignored by design).
            var convergence = awaitingProjection.Apply(snapshot);
            ClearActiveDragOccupancy();
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
            HidePreview();
            runtime.EndDrag();
            previewPresenter.EndDrag();
            currentDragGeneration = 0;
            dragOriginContainer = default(ContainerReference);
            dragSourcePassThrough = false;
            ClearActiveDragOccupancy();
        }

        internal void HidePreview()
        {
            if (previewSink != null) previewSink.Hide();
            previewPresenter.HidePreview();
        }

        private void CleanupUiAndDrag()
        {
            HidePreview();
            if (previewSink != null)
            {
                previewSink.Unmount();
                previewSink = null;
            }
            isInventoryOpen = false;
            currentSurface = null;
            currentContainer = default(ContainerReference);
            currentSessionGeneration = 0;
            liveSurfaces.Clear();
            previewPresenter.EndDrag();
            dragOriginContainer = default(ContainerReference);
            dragSourcePassThrough = false;
            ClearActiveDragOccupancy();
        }

        internal void InvalidateOccupancySnapshot()
        {
            var currentProvider = currentSurface as INativeInventoryOccupancyProvider;
            if (currentProvider != null) currentProvider.InvalidateOccupancy();
            foreach (var surface in liveSurfaces.Values)
            {
                var provider = surface as INativeInventoryOccupancyProvider;
                if (provider == null || object.ReferenceEquals(provider, currentProvider)) continue;
                provider.InvalidateOccupancy();
            }
            ClearActiveDragOccupancy();
            HidePreview();
        }

        internal bool TryGetOccupancyForDrag(ItemGridPosition source, byte itemWidth, byte itemHeight,
            byte sourceRotation, ItemAssetIdentity sourceAsset, out IGridOccupancyView occupancy)
        {
            occupancy = null;
            if (currentSurface == null) return false;
            if (hasActiveDragOccupancy && object.ReferenceEquals(activeDragOccupancySurface, currentSurface) &&
                SameContainer(activeDragOccupancySourceContainer, dragOriginContainer) &&
                SameContainer(activeDragOccupancyTargetContainer, currentContainer) &&
                SameSource(activeDragOccupancySource, source) &&
                activeDragOccupancyWidth == itemWidth && activeDragOccupancyHeight == itemHeight &&
                activeDragOccupancyRotation == sourceRotation && activeDragOccupancyAsset == sourceAsset)
            {
                occupancy = activeDragOccupancy;
                return true;
            }

            var provider = currentSurface as INativeInventoryOccupancyProvider;
            if (provider != null)
            {
                if (!provider.TryCreateOccupancyForDrag(dragOriginContainer, currentContainer, source,
                    itemWidth, itemHeight, sourceRotation, sourceAsset, out occupancy))
                {
                    ClearActiveDragOccupancy();
                    return false;
                }
            }
            else
            {
                occupancy = currentSurface.Occupancy;
                if (occupancy == null) return false;
            }

            activeDragOccupancy = occupancy;
            activeDragOccupancySurface = currentSurface;
            activeDragOccupancySourceContainer = dragOriginContainer;
            activeDragOccupancyTargetContainer = currentContainer;
            activeDragOccupancySource = source;
            activeDragOccupancyWidth = itemWidth;
            activeDragOccupancyHeight = itemHeight;
            activeDragOccupancyRotation = sourceRotation;
            activeDragOccupancyAsset = sourceAsset;
            hasActiveDragOccupancy = true;
            return true;
        }

        private static bool SameContainer(ContainerReference left, ContainerReference right)
        {
            return left.Kind == right.Kind && left.Page == right.Page &&
                left.SessionGeneration == right.SessionGeneration;
        }

        private static bool SameSource(ItemGridPosition left, ItemGridPosition right)
        {
            return left.Page == right.Page && left.X == right.X && left.Y == right.Y &&
                left.Rotation == right.Rotation;
        }

        private void ClearActiveDragOccupancy()
        {
            activeDragOccupancy = null;
            activeDragOccupancySurface = null;
            activeDragOccupancySourceContainer = default(ContainerReference);
            activeDragOccupancyTargetContainer = default(ContainerReference);
            activeDragOccupancySource = default(ItemGridPosition);
            activeDragOccupancyWidth = 0;
            activeDragOccupancyHeight = 0;
            activeDragOccupancyRotation = 0;
            activeDragOccupancyAsset = default(ItemAssetIdentity);
            hasActiveDragOccupancy = false;
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

            IGridOccupancyView occupancy;
            if (!TryGetOccupancyForDrag(source, itemWidth, itemHeight, currentRotation, itemAsset, out occupancy))
            {
                // Occupancy is the single fact source for both preview and
                // native swap decisions. Once it is invalidated, discard any
                // previously published Candidate so release cannot reuse stale
                // visual state; the caller will preserve native pass-through.
                previewPresenter.HidePreview();
                if (previewSink != null) previewSink.Hide();
                return false;
            }

            var scrollPixelsX = pointerAlreadyIncludesScroll ? 0f : currentSurface.ScrollPixelsX;
            var scrollPixelsY = pointerAlreadyIncludesScroll ? 0f : currentSurface.ScrollPixelsY;
            input = new InventoryPreviewInput(dragGeneration, source, currentContainer, pointerScreenX, pointerScreenY,
                currentSurface.Viewport, currentSurface.CellPixelSize, currentSurface.UiScale,
                scrollPixelsX, scrollPixelsY, itemWidth, itemHeight, currentRotation,
                runtime.EnhancedDragActive && runtime.ActivePolicy.AutoRotate && allowAutomaticRotation,
                grabOffsetX, grabOffsetY, itemAsset, topLevelPointerScaleX, topLevelPointerScaleY,
                nativeDragPivotX, nativeDragPivotY, pointerCoordinateSpace, occupancy);
            return true;
        }
    }
}
