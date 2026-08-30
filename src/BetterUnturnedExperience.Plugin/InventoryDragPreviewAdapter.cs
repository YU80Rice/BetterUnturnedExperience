using System;
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
        private bool wasDragging;
        private uint dragGeneration;
        private System.Reflection.FieldInfo dragJarField;
        private System.Reflection.FieldInfo dragFromPageField;
        private System.Reflection.FieldInfo dragFromXField;
        private System.Reflection.FieldInfo dragFromYField;
        private System.Reflection.FieldInfo dragFromRotField;
        private NativeDragActions nativeActions;

        internal bool Enabled { get { return enabled; } }
        internal string GateDiagnostics { get { return gateDiagnostics; } }
        internal bool HooksInstalled { get { return hooksInstalled; } }

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

            var missing = new System.Collections.Generic.List<string>();
            if (AccessTools.Method(typeof(PlayerDashboardInventoryUI), "onPlacedItem") == null) missing.Add("PlayerDashboardInventoryUI.onPlacedItem");
            if (AccessTools.Method(typeof(PlayerDashboardInventoryUI), "stopDrag") == null) missing.Add("PlayerDashboardInventoryUI.stopDrag");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragJar") == null) missing.Add("PlayerDashboardInventoryUI.dragJar");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFromPage") == null) missing.Add("PlayerDashboardInventoryUI.dragFromPage");
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
            if (!enabled || hooksInstalled) return;
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

        internal void AttachGrid(IClientUiInventorySurface surface)
        {
            var context = surface as UnturnedInventorySurfaceContext;
            var container = context?.GridPanelContainer as UnturnedVisualContainer;
            var sleek = container?.element as SleekItems;
            if (sleek == null)
            {
                log?.LogWarning("[BUE-DRAG] event=attach-grid-failed reason=grid-not-sleekitems diagnosticId=BUE-DRAG-003");
                return;
            }
            if (ReferenceEquals(sleek, attachedGrid)) return;
            attachedGrid = sleek;
            nativePlacedHandler = sleek.onPlacedItem;
            sleek.onPlacedItem = GridPlacedItemWrapper;
            log?.LogInfo("[BUE-DRAG] event=placed-item-delegate-rebound page=" + sleek.page + " diagnosticId=BUE-DRAG-001");
        }

        private void GridPlacedItemWrapper(byte page, byte x, byte y)
        {
            if (!EvaluatePlacement(page, x, y)) return;
            nativePlacedHandler?.Invoke(page, x, y);
        }

        internal static void DashboardUpdatePostfix()
        {
            var adapter = ActiveAdapter;
            if (adapter == null) return;
            try
            {
                adapter.Poll();
                adapter.component?.Tick((uint)Environment.TickCount);
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "poll failed: " + error.GetType().FullName + ": " + error.Message;
            }
        }

        internal static InventoryDragPreviewAdapter ActiveAdapter { get; private set; }

        internal static string LastPollDiagnostics { get; private set; }

        private void Poll()
        {
            var isDragging = PlayerDashboardInventoryUI.isDragging;
            if (isDragging && !wasDragging)
            {
                dragGeneration++;
                var jar = ReadDragJar();
                var asset = jar == null ? ItemAssetIdentity.FromItemId(0) : AssetIdentityOf(jar);
                component.OnDragStarted(dragGeneration, asset);
                log?.LogInfo("[BUE-DRAG] event=drag-started generation=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
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
                if (component.TryCreatePreviewInput(dragGeneration, ReadDragSource(), Input.mousePosition.x,
                        Input.mousePosition.y, ReadDragWidth(), ReadDragHeight(), ReadDragRotation(),
                        allowAutomaticRotation: true, grabOffsetX: 25f, grabOffsetY: 25f,
                        itemAsset: AssetIdentityOf(ReadDragJar()), out var input))
                {
                    component.OnDragUpdated(input);
                }
            }
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
            if (subscribedPlayer != null)
            {
                subscribedPlayer.inventory.onInventoryAdded -= OnNativeInventoryEvent;
                subscribedPlayer.inventory.onInventoryRemoved -= OnNativeInventoryEvent;
                subscribedPlayer.inventory.onInventoryUpdated -= OnNativeInventoryEvent;
            }
            subscribedPlayer = player;
            if (player == null) return;
            player.inventory.onInventoryAdded += OnNativeInventoryEvent;
            player.inventory.onInventoryRemoved += OnNativeInventoryEvent;
            player.inventory.onInventoryUpdated += OnNativeInventoryEvent;
            log?.LogInfo("[BUE-DRAG] event=inventory-events-subscribed diagnosticId=BUE-DRAG-001");
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
            if (!component.EnhancedDragActive) return true;
            var preview = component.LastPreview;
            if (preview.State == 0 || preview.DragGeneration != dragGeneration) return true;

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
            if (outcome == NativeDragAdapterOutcome.Cancelled && IsSwapOntoOccupied(page, x, y))
            {
                // A swap onto an occupied same-page cell is a native operation:
                // undo our stopDrag and let the vanilla path run.
                PlayerDashboardInventoryUI.stopDrag();
                return true;
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
        }
    }
}
