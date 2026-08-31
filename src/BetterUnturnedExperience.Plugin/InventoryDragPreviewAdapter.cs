using System;
using Action = System.Action;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-16D drag preview/commit adapter. Driven by the vanilla
    /// PlayerDashboardInventoryUI.updateDraggedItem tick (postfix) and a
    /// rebound SleekItems.onPlacedItem delegate; reads only
    /// public native state (isDragging/dragJar/dragFrom*) plus the private
    /// drag fields through cached FieldInfo kept inside this adapter file.
    /// Routes decisions through the composition's ItemInteractionUiComponent
    /// and forwards the native commit via sendDragItem.
    /// </summary>
    internal sealed class InventoryDragPreviewAdapter
    {
        
        private readonly BepInEx.Logging.ManualLogSource log;
        private readonly Harmony harmony;
        private readonly bool enabled;
        private string gateDiagnostics;

        private BetterItemInteractionUiComponent component;
        private bool hooksInstalled;
        private bool isolated;
        private bool wasDragging;
        private uint dragGeneration;
        private System.Reflection.FieldInfo dragJarField;
        private System.Reflection.FieldInfo dragFromPageField;
        private System.Reflection.FieldInfo dragFromXField;
        private System.Reflection.FieldInfo dragFromYField;
        private System.Reflection.FieldInfo dragFromRotField;
        private System.Reflection.FieldInfo dragPivotField;
        private System.Reflection.FieldInfo dragItemField;
        private NativeDragActions nativeActions;
        private int lastPollFrame = -1;
        private int lastDiagnosticTick;
        private PlacementPreviewState lastDiagnosticState;
        private PlacementReason lastDiagnosticReason;
        private bool hasDiagnostic;

        internal bool Enabled { get { return enabled; } }
        internal string GateDiagnostics { get { return gateDiagnostics; } }
        internal bool HooksInstalled { get { return hooksInstalled; } }
        internal bool Isolated { get { return isolated; } }

        internal InventoryDragPreviewAdapter(BepInEx.Logging.ManualLogSource log,
            BetterItemInteractionUiComponent component)
        {
            this.log = log;

            this.component = component ?? throw new ArgumentNullException(nameof(component));
            harmony = new Harmony("io.github.yu80rice.bue.drag-preview");
            nativeActions = new NativeDragActions();
            dragJarField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragJar");
            dragFromPageField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFromPage");
            dragFromXField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFrom_x");
            dragFromYField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFrom_y");
            dragFromRotField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFromRot");
            dragPivotField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragPivot");
            dragItemField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragItem");

            var missing = new System.Collections.Generic.List<string>();
            if (AccessTools.Method(typeof(PlayerDashboardInventoryUI), "onPlacedItem") == null) missing.Add("PlayerDashboardInventoryUI.onPlacedItem");
            if (AccessTools.Method(typeof(PlayerDashboardInventoryUI), "stopDrag") == null) missing.Add("PlayerDashboardInventoryUI.stopDrag");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragJar") == null) missing.Add("PlayerDashboardInventoryUI.dragJar");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFromPage") == null) missing.Add("PlayerDashboardInventoryUI.dragFromPage");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragPivot") == null) missing.Add("PlayerDashboardInventoryUI.dragPivot");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragItem") == null) missing.Add("PlayerDashboardInventoryUI.dragItem");
            if (AccessTools.Method(typeof(PlayerDashboardInventoryUI), "updateDraggedItem") == null) missing.Add("PlayerDashboardInventoryUI.updateDraggedItem");
            if (missing.Count > 0)
            {
                var diagnostics = new System.Text.StringBuilder("drag-preview-gate: native members missing, wiring disabled, native drag preserved -> ");
                for (var index = 0; index < missing.Count; index++)
                {
                    if (index > 0) diagnostics.Append(", ");
                    diagnostics.Append(missing[index]);
                }
                gateDiagnostics = diagnostics.ToString();
                enabled = false;
                return;
            }
            enabled = true;
        }

        internal void Activate()
        {
            if (!enabled || hooksInstalled || isolated) return;
            try
            {
                // Drives the drag poll on the dashboard-inventory UI tick itself
                // (runs every frame while any inventory page is visible). The
                // onPlacedItem patch is NOT used: its 170-line method body
                // fails Harmony's IL recompile on this game build. Placement
                // interception instead rebinds the public SleekItems
                // .onPlacedItem delegate per dispatch (see AttachGrid).
                harmony.Patch(AccessTools.Method(typeof(PlayerDashboardInventoryUI), "updateDraggedItem"),
                    postfix: new HarmonyMethod(typeof(InventoryDragPreviewAdapter), nameof(DashboardUpdatePostfix)));
                hooksInstalled = true;
                ActiveAdapter = this;
                log?.LogInfo("[BUE-DRAG] event=hooks-installed targets=updateDraggedItem diagnosticId=BUE-DRAG-001");
            }
            catch (Exception error)
            {
                gateDiagnostics = "hooks-failed: " + error.GetType().FullName + ": " + error.Message;
                hooksInstalled = false;
            }
        }

        // [DEV-16D] Placement interception without Harmony: each session
        // dispatch rebinds the grid's public onPlacedItem delegate to a BUE
        // wrapper that runs the take-over decision first and only forwards to
        // the vanilla handler on pass-through. Rebinding re-runs per dispatch
        // so a rebuilt UI gets fresh delegates.
        private SleekItems attachedGrid;
        private PlacedItem nativePlacedHandler;

        // Rebuilt native surfaces are only attachable while this adapter is
        // live. Once isolation starts, no new delegate may be written.
        internal static bool CanAttachGrid(bool isolated)
        {
            return !isolated;
        }

        internal void AttachGrid(IClientUiInventorySurface surface)
        {
            if (!CanAttachGrid(isolated))
            {
                LastPollDiagnostics = "grid-attach-rejected-isolated";
                return;
            }
            try
            {
                var context = surface as UnturnedInventorySurfaceContext;
                var sleek = context?.NativeItems;
                if (sleek == null)
                    throw new InvalidOperationException("context has no native SleekItems");
                // Idempotence: storage and trunk share one SleekItems (page 7),
                // and re-entry here used to wrap our own wrapper, stacking
                // evaluation layers until a placement crashed the game.
                if (ReferenceEquals(sleek, attachedGrid) || ReferenceEquals(sleek.onPlacedItem?.Target, this)) return;
                DetachGrid();
                attachedGrid = sleek;
                nativePlacedHandler = sleek.onPlacedItem;
                sleek.onPlacedItem = GridPlacedItemWrapper;
                log?.LogInfo("[BUE-DRAG] event=placed-item-delegate-rebound page=" + sleek.page + " diagnosticId=BUE-DRAG-001");
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "grid-attach-failed: " + error.GetType().FullName + ": " + error.Message;
                log?.LogWarning("[BUE-DRAG] event=attach-grid-failed diagnosticId=BUE-DRAG-003");
                IsolateAndDetach();
            }
        }

        internal void DetachGrid()
        {
            if (attachedGrid == null) return;
            try
            {
                DetachGridCore();
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "grid-detach-failed: " + error.GetType().FullName + ": " + error.Message;
                IsolateAndDetach();
            }
        }

        private void DetachGridCore()
        {
            if (attachedGrid == null) return;
            if (ReferenceEquals(attachedGrid.onPlacedItem?.Target, this))
                attachedGrid.onPlacedItem = nativePlacedHandler;
            attachedGrid = null;
            nativePlacedHandler = null;
        }

        // GPT watermark: detach every native callback and event subscription
        // before isolating the feature, so an exception cannot leave a stale
        // wrapper or static adapter receiving future frames.
        internal bool IsolateAndDetach()
        {
            return IsolateAndDetach(true);
        }

        internal bool IsolateAndDetach(bool isolateComponent)
        {
            if (isolated)
            {
                return string.IsNullOrEmpty(LastCleanupDiagnostics);
            }
            isolated = true;
            LastCleanupDiagnostics = null;
            var detachSucceeded = DetachAndDeactivate();
            var previewSucceeded = FailClosedPreview(isolateComponent ? component.IsolatePreviewFailure : null, component.HidePreview);
            return detachSucceeded && previewSucceeded;
        }

        private bool DetachAndDeactivate()
        {
            var success = true;
            try { DetachGridCore(); }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("grid-detach", error);
            }
            try { UnsubscribeInventoryEvents(); }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("inventory-unsubscribe", error);
            }
            try { harmony.UnpatchSelf(); }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("hook-unpatch", error);
            }
            hooksInstalled = false;
            ClearActive(this);
            return success;
        }

        private void GridPlacedItemWrapper(byte page, byte x, byte y)
        {
            var native = nativePlacedHandler;
            InvokePlacedItemGuarded(
                () => EvaluatePlacement(page, x, y),
                () => native?.Invoke(page, x, y),
                component.IsolatePreviewFailure,
                component.HidePreview,
                () => { IsolateAndDetach(); });
        }

        // GPT watermark: the U3-SDK invokes SleekItems.onPlacedItem directly
        // without an exception boundary. Keep the BUE wrapper fail-closed while
        // forwarding evaluation failures back to the vanilla callback.
        internal static void InvokePlacedItemGuarded(Func<bool> evaluate, Action forward, Action isolate, Action hide)
        {
            InvokePlacedItemGuarded(evaluate, forward, isolate, hide, null);
        }

        internal static void InvokePlacedItemGuarded(Func<bool> evaluate, Action forward, Action isolate, Action hide, Action detach)
        {
            bool passThrough;
            try
            {
                passThrough = evaluate == null || evaluate();
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "placed-item-evaluate-failed: " + error.GetType().FullName + ": " + error.Message;
                TryDetach(detach);
                FailClosedPreview(isolate, hide);
                try { forward?.Invoke(); } catch (Exception nativeError)
                {
                    LastPollDiagnostics = "placed-item-native-fallback-failed: " + nativeError.GetType().FullName + ": " + nativeError.Message;
                }
                return;
            }

            if (!passThrough) return;
            try { forward?.Invoke(); }
            catch (Exception error)
            {
                LastPollDiagnostics = "placed-item-native-failed: " + error.GetType().FullName + ": " + error.Message;
                TryDetach(detach);
                FailClosedPreview(isolate, hide);
            }
        }

        private static void TryDetach(Action detach)
        {
            try { detach?.Invoke(); }
            catch (Exception error)
            {
                LastPollDiagnostics = "placed-item-detach-failed: " + error.GetType().FullName + ": " + error.Message;
            }
        }

        internal static void ReportCleanupFailure(string stage, Exception error)
        {
            LastCleanupDiagnostics = "featureId=io.github.yu80rice.bue.better-item-interaction"
                + " errorCode=CleanupIncomplete diagnosticId=BUE-DEV16D-CLEANUP-INCOMPLETE"
                + " stage=" + stage + " errorType=" + error.GetType().FullName + " message=" + error.Message;
            LastPollDiagnostics = stage + " cleanup failed: " + error.GetType().FullName + ": " + error.Message;
        }

        internal static void DashboardUpdatePostfix()
        {
            var adapter = ActiveAdapter;
            if (adapter == null) return;
            try
            {
                adapter.Tick();
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "poll failed: " + error.GetType().FullName + ": " + error.Message;
                adapter.IsolateAndDetach();
            }
        }

        internal static InventoryDragPreviewAdapter ActiveAdapter { get; private set; }

        internal static void ClearActive(InventoryDragPreviewAdapter adapter)
        {
            if (ReferenceEquals(ActiveAdapter, adapter)) ActiveAdapter = null;
        }

        internal static string LastPollDiagnostics { get; private set; }
        internal static string LastCleanupDiagnostics { get; private set; }

        // GPT watermark: reliable plugin-owned main-thread fallback, matching
        // UPM's BaseUnityPlugin.Update driver; Harmony remains a fast path.
        internal void Tick()
        {
            if (!enabled || isolated) return;
            if (!component.LifecycleCanRun && !component.EnhancedDragActive)
            {
                component.HidePreview();
                return;
            }
            if (Time.frameCount == lastPollFrame) return;
            lastPollFrame = Time.frameCount;
            try { Poll(); component.Tick((uint)Environment.TickCount); }
            catch (Exception error)
            {
                LastPollDiagnostics = "plugin-update poll failed: " + error.GetType().FullName + ": " + error.Message;
                IsolateAndDetach();
            }
        }

        // GPT watermark: isolate and hide are deliberately separate guarded
        // callbacks so a cleanup exception cannot prevent stale visuals from
        // being cleared or re-enable the native fallback path.
        internal static bool FailClosedPreview(Action isolate, Action hide)
        {
            var success = true;
            try { isolate?.Invoke(); }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("preview-isolate", error);
            }
            try { hide?.Invoke(); }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("preview-hide", error);
            }
            return success;
        }

        private void Poll()
        {
            if (!component.LifecycleCanRun && !component.EnhancedDragActive) return;
            var isDragging = PlayerDashboardInventoryUI.isDragging;
            if (isDragging && !wasDragging)
            {
                dragGeneration++;
                var jar = ReadDragJar();
                var asset = jar == null ? ItemAssetIdentity.FromItemId(0) : AssetIdentityOf(jar);
                component.OnDragStarted(dragGeneration, asset);
                log?.LogInfo("[BUE-DRAG] event=drag-started generation=" + dragGeneration
                    + " enhanced=" + component.EnhancedDragActive
                    + " canRun=" + component.LifecycleCanRun
                    + " sinkBound=" + component.PreviewSinkBound
                    + " diagnosticId=BUE-DRAG-001");
            }
            else if (!isDragging && wasDragging)
            {
                // Drag ended without onPlacedItem (ESC, drag-out): cancel visuals.
                component.OnDragCancelled();
                log?.LogInfo("[BUE-DRAG] event=drag-cancelled diagnosticId=BUE-DRAG-001");
            }
            wasDragging = isDragging;

            EnsureInventoryEventSubscription();

            if (isDragging)
            {
                // [R45] Read the same native grid/scroll hierarchy used by
                // SleekItems.onClickedGrid. The grid-local content point
                // already includes the scroll transform; no second scroll is
                // added by the pure-C# adapter.
                var surface = component.CurrentSurface as UnturnedInventorySurfaceContext;
                if (surface == null)
                {
                    component.HidePreview();
                    if (ShouldEmitDiagnostic(PlacementPreviewState.Hidden, PlacementReason.OutsideGrid))
                        log?.LogInfo("[BUE-DRAG] GPT-WATERMARK event=preview-hidden reason=outside-viewport generation=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
                    return;
                }
                UnturnedInventorySurfaceContext.PointerReadFailure pointerFailure;
                if (!surface.TryGetLocalPointerPixels(out var localX, out var localY, out pointerFailure))
                {
                    if (pointerFailure == UnturnedInventorySurfaceContext.PointerReadFailure.InvalidGeometry ||
                        pointerFailure == UnturnedInventorySurfaceContext.PointerReadFailure.ReadException)
                        throw new InvalidOperationException("BUE-DEV16D-VIEWPORT-READ-FAILED failure=" + pointerFailure);
                    component.HidePreview();
                    if (ShouldEmitDiagnostic(PlacementPreviewState.Hidden, PlacementReason.OutsideGrid))
                        log?.LogInfo("[BUE-DRAG] GPT-WATERMARK event=preview-hidden reason=outside-viewport generation=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
                    return;
                }
                var topLevelPointer = ReadTopLevelPointerScale();
                var nativePivot = ReadDragPivot();
                if (component.TryCreatePreviewInput(dragGeneration, ReadDragSource(), localX,
                        localY, ReadDragWidth(), ReadDragHeight(), ReadDragRotation(),
                        true, ReadGrabOffsetX(), ReadGrabOffsetY(), AssetIdentityOf(ReadDragJar()),
                        topLevelPointer.x, topLevelPointer.y, nativePivot.x, nativePivot.y, out var input))
                {
                    component.OnDragUpdated(input);
                    var state = component.LastPreview.State;
                    if (ShouldEmitDiagnostic(state, component.LastPreview.Reason))
                    {
                        LogPreviewInputReadout(input, Input.mousePosition.x, Input.mousePosition.y,
                            localX, localY, state, component.LastPreview.Reason);
                        log?.LogInfo("[BUE-DRAG] GPT-WATERMARK event=preview-evaluated generation=" + dragGeneration + " state=" + state + " diagnosticId=BUE-DRAG-001");
                        if (state == PlacementPreviewState.Candidate || state == PlacementPreviewState.LocallyInvalid)
                            log?.LogInfo("[BUE-DRAG] GPT-WATERMARK event=preview-visible generation=" + dragGeneration + " state=" + state + " diagnosticId=BUE-DRAG-001");
                    }
                }
                else
                {
                    if (ShouldEmitDiagnostic(PlacementPreviewState.Hidden, PlacementReason.FeatureUnavailable))
                        log?.LogInfo("[BUE-DRAG] GPT-WATERMARK event=preview-input-rejected generation=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
                }
            }
        }

        private bool ShouldEmitDiagnostic(PlacementPreviewState state, PlacementReason reason)
        {
            var now = Environment.TickCount;
            if (!hasDiagnostic || state != lastDiagnosticState || reason != lastDiagnosticReason || now - lastDiagnosticTick >= 500)
            {
                hasDiagnostic = true;
                lastDiagnosticState = state;
                lastDiagnosticReason = reason;
                lastDiagnosticTick = now;
                return true;
            }
            return false;
        }

        private void LogPreviewInputReadout(InventoryPreviewInput input, float rawMouseX, float rawMouseY,
            float uiX, float uiY, PlacementPreviewState state, PlacementReason reason)
        {
            var scaledCell = input.CellPixelSize * input.UiScale;
            var gridX = scaledCell > 0f
                ? (uiX - input.Viewport.OriginX + input.ScrollPixelsX) / scaledCell
                : float.NaN;
            var gridY = scaledCell > 0f
                ? (uiY - input.Viewport.OriginY + input.ScrollPixelsY) / scaledCell
                : float.NaN;
            log?.LogInfo("[BUE-DRAG] GPT-WATERMARK event=preview-input-readout"
                + " generation=" + input.DragGeneration
                + " pointerScreen=" + rawMouseX.ToString("0.###") + "," + rawMouseY.ToString("0.###")
                + " uiScale=" + input.UiScale.ToString("0.###")
                + " uiCoordinates=" + uiX.ToString("0.###") + "," + uiY.ToString("0.###")
                + " viewportOrigin=" + input.Viewport.OriginX.ToString("0.###") + "," + input.Viewport.OriginY.ToString("0.###")
                + " pointerGrid=" + gridX.ToString("0.###") + "," + gridY.ToString("0.###")
                + " grabOffset=" + input.GrabOffsetX.ToString("0.###") + "," + input.GrabOffsetY.ToString("0.###")
                + " placementReason=" + reason
                + " state=" + state
                + " diagnosticId=BUE-DRAG-001");
        }

        // [DEV-16D] Native convergence feed: the three inventory events build
        // a NativeInventorySnapshot (monotonic NativeRevision) and feed the
        // component's awaiting-projection bridge. Subscribed per-Player, with
        // the subscription re-bound when Player.LocalPlayer changes.
        private Player subscribedPlayer;
        private uint nativeRevision;
        private uint lastSubmittedGeneration;



        private void EnsureInventoryEventSubscription()
        {
            var player = Player.LocalPlayer;
            if (ReferenceEquals(player, subscribedPlayer)) return;
            UnsubscribeInventoryEvents();
            subscribedPlayer = player;
            if (player == null) return;
            player.inventory.onInventoryAdded += OnNativeInventoryEvent;
            player.inventory.onInventoryRemoved += OnNativeInventoryEvent;
            player.inventory.onInventoryUpdated += OnNativeInventoryEvent;
            log?.LogInfo("[BUE-DRAG] event=inventory-events-subscribed diagnosticId=BUE-DRAG-001");
        }

        private void UnsubscribeInventoryEvents()
        {
            if (subscribedPlayer == null) return;
            subscribedPlayer.inventory.onInventoryAdded -= OnNativeInventoryEvent;
            subscribedPlayer.inventory.onInventoryRemoved -= OnNativeInventoryEvent;
            subscribedPlayer.inventory.onInventoryUpdated -= OnNativeInventoryEvent;
            subscribedPlayer = null;
        }

        private void OnNativeInventoryEvent(byte page, byte index, ItemJar jar)
        {
            try
            {
                if (lastSubmittedGeneration == 0) return;
                var asset = jar == null || jar.item == null ? ItemAssetIdentity.FromItemId(0) : AssetIdentityOf(jar);
                var assetInstance = jar == null ? null : jar.GetAsset();
                var itemW = assetInstance == null ? (byte)1 : (byte)Mathf.Min(assetInstance.size_x, byte.MaxValue);
                var itemH = assetInstance == null ? (byte)1 : (byte)Mathf.Min(assetInstance.size_y, byte.MaxValue);
                var fingerprint = new InventoryItemFingerprint(asset, itemW, itemH, jar == null ? (byte)0 : jar.rot);
                var container = component.LastDispatchedContainer;
                if (container.SessionGeneration == 0) return;
                nativeRevision++;
                // index is a list ordinal (compacted by RemoveAt), not a grid
                // position; the jar carries its own authoritative cell.
                var snapshot = new NativeInventorySnapshot(lastSubmittedGeneration, container, fingerprint,
                    new ItemGridPosition(page, jar == null ? (byte)0 : jar.x, jar == null ? (byte)0 : jar.y, jar == null ? (byte)0 : jar.rot),
                    nativeRevision);
                component.OnNativeInventorySnapshot(snapshot);
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "convergence-event-failed: " + error.GetType().FullName + ": " + error.Message;
                IsolateAndDetach();
            }
        }

        private bool EvaluatePlacement(byte page, byte x, byte y)
        {
            // Returns false when BUE takes over the placement (legitimate grid
            // candidate or an invalid candidate the native path must not
            // execute); returns true for pass-through branches (slots, AREA,
            // and swaps onto an occupied same-page cell, which the native
            // onPlacedItem handles via sendSwapItem).
            if (!PlayerDashboardInventoryUI.isDragging) return true;
            var isOrdinaryGrid = page >= PlayerInventory.SLOTS && page != PlayerInventory.AREA;
            if (!isOrdinaryGrid) return true;
            // [R34 fail-open] Enhanced interaction off, or no fresh preview
            // evaluation, means BUE cannot judge the placement: the native
            // path owns it. BUE never blocks what it cannot evaluate.
            if (!component.EnhancedDragActive)
            {
                log?.LogInfo("[BUE-DRAG] event=placement-passthrough reason=enhanced-off diagnosticId=BUE-DRAG-001");
                return true;
            }
            var preview = component.LastPreview;
            if (preview.State == 0 || preview.DragGeneration != dragGeneration)
            {
                log?.LogInfo("[BUE-DRAG] event=placement-passthrough reason=preview-stale previewGen=" + preview.DragGeneration + " dragGen=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
                return true;
            }

            // Vanilla performs swaps inside onPlacedItem while isDragging is
            // still true. Only an invalid occupied preview is handed back to
            // vanilla; a valid preview may intentionally choose a nearby free
            // cell and must remain BUE-owned.
            if (preview.State == PlacementPreviewState.LocallyInvalid &&
                preview.Reason == PlacementReason.Occupied && IsSwapOntoOccupied(page, x, y))
            {
                component.HidePreview();
                log?.LogInfo("[BUE-DRAG] event=placement-passthrough reason=native-swap diagnosticId=BUE-DRAG-001");
                return true;
            }

            var input = new NativeDragAdapterInput(
                PlayerDashboardInventoryUI.isDragging, dragGeneration,
                ReadDragSource(), preview);
            var outcome = component.OnDragReleased(input, nativeActions);
            log?.LogInfo("[BUE-DRAG] event=placement-decision page=" + page + " x=" + x + " y=" + y
                + " outcome=" + outcome + " diagnosticId=BUE-DRAG-001");
            if (outcome == NativeDragAdapterOutcome.Submitted)
            {
                lastSubmittedGeneration = dragGeneration;
                PlayerDashboardInventoryUI.stopDrag();
                return false;
            }
            if (outcome == NativeDragAdapterOutcome.Cancelled)
            {
                PlayerDashboardInventoryUI.stopDrag();
                return false;
            }
            return true;
        }

        // [DEV-16D] Swap guard footprint semantics: a swap onto a cell covered
        // by the dragged item's footprint is a native sendSwapItem operation,
        // not a BUE-cancelled placement. Matches the native Items.findIndex
        // coverage (any footprint cell occupied counts).
        internal static bool FootprintOccupied(bool[,] occupancy, int gridW, int gridH, int originX, int originY, int itemW, int itemH)
        {
            for (var dy = 0; dy < itemH; dy++)
            for (var dx = 0; dx < itemW; dx++)
            {
                var cx = originX + dx;
                var cy = originY + dy;
                if (cx < 0 || cy < 0 || cx >= gridW || cy >= gridH) continue;
                if (occupancy[cx, cy]) return true;
            }
            return false;
        }

        private bool IsSwapOntoOccupied(byte page, byte x, byte y)
        {
            var player = Player.LocalPlayer;
            if (player == null || player.inventory == null) return false;
            var pageItems = player.inventory.items[page];
            if (pageItems == null) return false;
            // Occupy cells straight from each jar's own authoritative grid
            // position and rotated footprint (matches native Items.findIndex
            // coverage: rot%2 swaps the size axes).
            for (var listIndex = 0; listIndex < pageItems.items.Count; listIndex++)
            {
                var cell = pageItems.items[listIndex];
                if (cell == null) continue;
                var cellAsset = cell.GetAsset();
                if (cellAsset == null) continue;
                var w = (cell.rot % 2 == 0) ? cellAsset.size_x : cellAsset.size_y;
                var h = (cell.rot % 2 == 0) ? cellAsset.size_y : cellAsset.size_x;
                for (var dy = 0; dy < h; dy++)
                for (var dx = 0; dx < w; dx++)
                {
                    if (cell.x + dx == x && cell.y + dy == y) return true;
                }
            }
            return false;
        }

        private ItemJar ReadDragJar()
        {
            return dragJarField == null ? null : dragJarField.GetValue(null) as ItemJar;
        }

        private ItemGridPosition ReadDragSource()
        {
            var page = dragFromPageField == null ? (byte)0 : (byte)Convert.ToByte(dragFromPageField.GetValue(null));
            var x = dragFromXField == null ? (byte)0 : (byte)Convert.ToByte(dragFromXField.GetValue(null));
            var y = dragFromYField == null ? (byte)0 : (byte)Convert.ToByte(dragFromYField.GetValue(null));
            var rotation = dragFromRotField == null ? (byte)0 : (byte)Convert.ToByte(dragFromRotField.GetValue(null));
            return new ItemGridPosition(page, x, y, rotation);
        }

        private static byte ReadDragWidth()
        {
            var jar = ActiveAdapter == null ? null : ActiveAdapter.ReadDragJar();
            var asset = jar == null ? null : jar.GetAsset();
            return asset == null ? (byte)1 : (byte)Mathf.Min(asset.size_x, byte.MaxValue);
        }

        private static byte ReadDragHeight()
        {
            var jar = ActiveAdapter == null ? null : ActiveAdapter.ReadDragJar();
            var asset = jar == null ? null : jar.GetAsset();
            return asset == null ? (byte)1 : (byte)Mathf.Min(asset.size_y, byte.MaxValue);
        }

        private static byte ReadDragRotation()
        {
            var jar = ActiveAdapter == null ? null : ActiveAdapter.ReadDragJar();
            return jar == null ? (byte)0 : jar.rot;
        }

        private float ReadGrabOffsetX()
        {
            var pivot = dragPivotField == null ? Vector2.zero : (Vector2)dragPivotField.GetValue(null);
            return NativePivotToGrabOffset(pivot).x;
        }

        private float ReadGrabOffsetY()
        {
            var pivot = dragPivotField == null ? Vector2.zero : (Vector2)dragPivotField.GetValue(null);
            return NativePivotToGrabOffset(pivot).y;
        }

        private Vector2 ReadDragPivot()
        {
            return dragPivotField == null ? Vector2.zero : (Vector2)dragPivotField.GetValue(null);
        }

        private SleekItem ReadDragItem()
        {
            return dragItemField == null ? null : dragItemField.GetValue(null) as SleekItem;
        }

        private static Vector2 ReadTopLevelPointerScale()
        {
            try
            {
                if (PlayerUI.container == null || Screen.width <= 0 || Screen.height <= 0)
                    return new Vector2(float.NaN, float.NaN);
                return PlayerUI.container.ViewportToNormalizedPosition(InputEx.NormalizedMousePosition);
            }
            catch (Exception)
            {
                return new Vector2(float.NaN, float.NaN);
            }
        }

        private static float ReadUiScale()
        {
            try { return GraphicsSettings.userInterfaceScale; } catch { return 1f; }
        }

        // GPT watermark: U3-SDK dragPivot is the negative pixel displacement
        // from the cursor to the item origin; the pure-C# seam expects the
        // positive cursor grab point in grid-cell units.
        private static Vector2 NativePivotToGrabOffset(Vector2 pivot)
        {
            return new Vector2(-pivot.x / 50f, -pivot.y / 50f);
        }

        private static ItemAssetIdentity AssetIdentityOf(ItemJar jar)
        {
            var asset = jar == null ? null : jar.GetAsset();
            if (asset == null) return ItemAssetIdentity.FromItemId(0);
            return ItemAssetIdentity.FromAsset(asset.id, asset.GUID, asset.name);
        }

        private sealed class NativeDragActions : INativeInventoryDragActions
        {
            public void StopDrag() { PlayerDashboardInventoryUI.stopDrag(); }

            public void SendDragItem(ItemGridPosition source, ItemGridPosition target)
            {
                var player = Player.LocalPlayer;
                if (player == null || player.inventory == null) return;
                player.inventory.sendDragItem(source.Page, source.X, source.Y,
                    target.Page, target.X, target.Y, target.Rotation);
            }

            public void TakeGroundItem(ItemGridPosition target)
            {
                var adapter = ActiveAdapter;
                var dragItem = adapter == null ? null : adapter.ReadDragItem();
                var interactable = dragItem == null || dragItem.jar == null ? null : dragItem.jar.interactableItem;
                if (interactable == null || interactable.transform == null || interactable.transform.parent == null)
                    throw new InvalidOperationException("native ground item is no longer available");
                ItemManager.takeItem(interactable.transform.parent, target.X, target.Y, target.Rotation, target.Page);
            }
        }
    }
}
