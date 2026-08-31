using System;
using Action = System.Action;
using System.Reflection;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// Wraps a real Glazier element as the pure-C# IVisualElement used by the
    /// preview sink. Color and rotation forward to the typed uGUI interfaces
    /// when the underlying element supports them.
    /// </summary>
    internal sealed class UnturnedVisualElement : IVisualElement
    {
        internal readonly ISleekElement element;
        private readonly ISleekBox box;
        private readonly ISleekImage image;
        private readonly SleekItemIcon itemIcon;
        private ItemAssetIdentity boundAsset;
        private byte rotation;

        internal UnturnedVisualElement(ISleekElement element)
        {
            this.element = element ?? throw new ArgumentNullException(nameof(element));
            box = element as ISleekBox;
            image = element as ISleekImage;
            itemIcon = element as SleekItemIcon;
            if (image != null) image.CanRotate = true;
            if (itemIcon != null) itemIcon.isAngled = true;
        }

        public float PositionScaleX { get { return element.PositionScale_X; } set { element.PositionScale_X = value; } }
        public float PositionScaleY { get { return element.PositionScale_Y; } set { element.PositionScale_Y = value; } }
        public float PositionOffsetX { get { return element.PositionOffset_X; } set { element.PositionOffset_X = value; } }
        public float PositionOffsetY { get { return element.PositionOffset_Y; } set { element.PositionOffset_Y = value; } }
        public float SizeOffsetX { get { return element.SizeOffset_X; } set { element.SizeOffset_X = value; } }
        public float SizeOffsetY { get { return element.SizeOffset_Y; } set { element.SizeOffset_Y = value; } }

        public byte RotationAngle
        {
            get { return rotation; }
            set
            {
                rotation = (byte)(value & 3);
                if (image != null) image.RotationAngle = rotation * 90f;
                if (itemIcon != null) itemIcon.rot = rotation;
            }
        }

        public bool CanRotate
        {
            get { return image != null && image.CanRotate || itemIcon != null; }
            set
            {
                if (image != null) image.CanRotate = value;
                if (itemIcon != null) itemIcon.isAngled = value;
            }
        }

        public bool IsVisible { get { return element.IsVisible; } set { element.IsVisible = value; } }

        public PreviewFrameColor Color
        {
            get { return PreviewFrameColor.None; }
            set
            {
                if (box == null) return;
                if (value == PreviewFrameColor.ValidGreen) box.BackgroundColor = new SleekColor(new Color(0.2f, 1f, 0.3f, 0.85f));
                else if (value == PreviewFrameColor.InvalidRed) box.BackgroundColor = new SleekColor(new Color(1f, 0.25f, 0.2f, 0.85f));
                else box.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.6f);
            }
        }

        public ItemAssetIdentity BoundAsset
        {
            get { return boundAsset; }
            set
            {
                if (boundAsset == value) return;
                boundAsset = value;
                if (value.ItemId == 0)
                {
                    if (image != null) image.Texture = null;
                    if (itemIcon != null) itemIcon.Clear();
                    return;
                }
                var asset = Assets.find(EAssetType.ITEM, value.ItemId) as ItemAsset;
                if (asset == null)
                {
                    if (image != null) image.Texture = null;
                    if (itemIcon != null) itemIcon.Clear();
                    return;
                }
                try
                {
                    if (itemIcon != null)
                    {
                        itemIcon.Refresh(value.ItemId, 100, asset.getState(), asset);
                    }
                    else if (image != null)
                    {
                        image.Texture = null;
                        ItemTool.getIcon(value.ItemId, 100, asset.getState(), asset,
                            (handle, texture) => image.Texture = texture);
                    }
                }
                catch (Exception)
                {
                    if (image != null) image.Texture = null;
                    if (itemIcon != null) itemIcon.Clear();
                }
            }
        }
    }

    /// <summary>
    /// Wraps a real Glazier container as IVisualContainer. Child factories go
    /// through the live Glazier factory so created elements are engine-native.
    /// </summary>
    internal sealed class UnturnedVisualContainer : IVisualContainer
    {
        internal readonly ISleekElement element;

        internal UnturnedVisualContainer(ISleekElement element)
        {
            this.element = element ?? throw new ArgumentNullException(nameof(element));
        }

        public IVisualElement CreateBox() { return new UnturnedVisualElement(Glazier.Get().CreateBox()); }
        public IVisualElement CreateImage() { return new UnturnedVisualElement(new SleekItemIcon()); }
        public void AddChild(IVisualElement child) { element.AddChild(Unwrap(child)); }
        public void RemoveChild(IVisualElement child) { element.RemoveChild(Unwrap(child)); }

        private static ISleekElement Unwrap(IVisualElement child)
        {
            var wrapper = child as UnturnedVisualElement;
            if (wrapper == null) throw new ArgumentException("only engine-backed visual elements can be mounted", nameof(child));
            return wrapper.element;
        }
    }

    /// <summary>
    /// Adapts a real Items grid into the pure IGridOccupancyView.
    /// </summary>
    internal sealed class UnturnedGridOccupancyView : IGridOccupancyView
    {
        private readonly Items items;

        internal UnturnedGridOccupancyView(Items items)
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public byte Width { get { return (byte)Mathf.Min(items.width, byte.MaxValue); } }
        public byte Height { get { return (byte)Mathf.Min(items.height, byte.MaxValue); } }

        public bool IsOccupied(byte x, byte y)
        {
            if (x >= items.width || y >= items.height) return true;
            var index = y * items.width + x;
            return index < items.items.Count && items.items[index] != null;
        }
    }

    /// <summary>
    /// Real-engine implementation of IInventorySurfaceContext for the dashboard
    /// inventory UI (player pages, storage and vehicle trunk share the STORAGE
    /// surface; the storage/trunk distinction lives in ContainerReference.Kind).
    /// </summary>
    internal sealed class UnturnedInventorySurfaceContext : IInventorySurfaceContext
    {
        // [R41] Single authority for viewport selection: live geometry when
        // the native hierarchy and scroll size are trustworthy, otherwise the
        // offset/size approximation. Never null, never throws.
        internal static InventoryGridViewport ResolveViewport(bool hierarchyLive, UnityEngine.Vector2 scrollSize,
            byte gridWidth, byte gridHeight, float offsetX, float offsetY, float sizeX, float sizeY)
        {
            return ResolveViewport(hierarchyLive, scrollSize, gridWidth, gridHeight, offsetX, offsetY, sizeX, sizeY, 0f, 0f);
        }

        internal static InventoryGridViewport ResolveViewport(bool hierarchyLive, UnityEngine.Vector2 scrollSize,
            byte gridWidth, byte gridHeight, float offsetX, float offsetY, float sizeX, float sizeY,
            float scrollPixelsX, float scrollPixelsY)
        {
            // Native SleekItems exposes the pointer relative to the grid's
            // content, while the scroll view determines whether that content
            // point is visible. Consume the real scroll viewport dimensions
            // whenever they are available; the fallback remains bounded and
            // does not import a parent PositionOffset into this local space.
            var clipW = hierarchyLive && IsFinitePositive(scrollSize.x) ? scrollSize.x
                : hierarchyLive && IsFinitePositive(sizeX) ? sizeX : gridWidth * 50f;
            var clipH = hierarchyLive && IsFinitePositive(scrollSize.y) ? scrollSize.y
                : hierarchyLive && IsFinitePositive(sizeY) ? sizeY : gridHeight * 50f;
            return new InventoryGridViewport(0f, 0f, gridWidth, gridHeight,
                IsFiniteNonNegative(scrollPixelsX) ? scrollPixelsX : 0f,
                IsFiniteNonNegative(scrollPixelsY) ? scrollPixelsY : 0f, clipW, clipH);
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }


        internal static readonly FieldInfo NativeScrollField = typeof(SleekItems).GetField("horizontalScrollView", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly FieldInfo NativeGridField = typeof(SleekItems).GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly FieldInfo NativeItemsPanelField = typeof(SleekItems).GetField("itemsPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly FieldInfo DashboardItemsField = typeof(PlayerDashboardInventoryUI).GetField("items", BindingFlags.Static | BindingFlags.NonPublic);
        internal enum PointerCoordinateMode
        {
            GridContentIncludesScroll = 0,
            ViewportLocalRequiresScroll = GridContentIncludesScroll,
            ScreenRequiresScroll = 1
        }

        internal static float NormalizeUiScale(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : 1f;
        }
        internal static Vector2 MapNormalizedPointer(float nx, float ny, float width, float height)
        {
            if (float.IsNaN(nx) || float.IsInfinity(nx) || float.IsNaN(ny) || float.IsInfinity(ny) ||
                float.IsNaN(width) || float.IsInfinity(width) || float.IsNaN(height) || float.IsInfinity(height) ||
                width <= 0f || height <= 0f || nx < 0f || nx > 1f || ny < 0f || ny > 1f) return Vector2.zero;
            return new Vector2(nx * width, ny * height);
        }

        internal static bool IsNativeHierarchyComplete(bool hasScroll, bool hasGrid, bool hasItemsPanel)
        {
            return hasScroll && hasGrid && hasItemsPanel;
        }

        internal static bool IsNativeHierarchyConsistent(object owner, object scroll, object grid, object itemsPanel,
            object scrollParent, object gridParent, object itemsPanelParent)
        {
            return owner != null && scroll != null && grid != null && itemsPanel != null &&
                SameNativeElement(scrollParent, owner) && SameNativeElement(gridParent, scroll) &&
                SameNativeElement(itemsPanelParent, grid);
        }

        private static bool SameNativeElement(object left, object right)
        {
            if (ReferenceEquals(left, right)) return true;
            var leftElement = left as ISleekElement;
            var rightElement = right as ISleekElement;
            if (leftElement == null || rightElement == null) return false;
            return ReferenceEquals(leftElement.AttachmentRoot, rightElement.AttachmentRoot);
        }

        internal readonly struct NativeInventoryHierarchySnapshot
        {
            internal readonly bool HasScroll;
            internal readonly bool HasGrid;
            internal readonly bool HasItemsPanel;
            internal readonly bool ParentChainValid;
            internal readonly Vector2 PointerNormalized;
            internal readonly Vector2 GridSize;
            internal readonly Vector2 ViewportSize;
            internal readonly float ScrollPixelsY;
            internal readonly float UiScale;

            internal NativeInventoryHierarchySnapshot(bool hasScroll, bool hasGrid, bool hasItemsPanel,
                bool parentChainValid, Vector2 pointerNormalized, Vector2 gridSize, Vector2 viewportSize,
                float scrollPixelsY, float uiScale)
            {
                HasScroll = hasScroll; HasGrid = hasGrid; HasItemsPanel = hasItemsPanel;
                ParentChainValid = parentChainValid; PointerNormalized = pointerNormalized;
                GridSize = gridSize; ViewportSize = viewportSize; ScrollPixelsY = scrollPixelsY; UiScale = uiScale;
            }
        }

        internal static bool TryBuildNativeGeometry(NativeInventoryHierarchySnapshot snapshot,
            out InventoryGridViewport viewport, out Vector2 pointerPixels)
        {
            viewport = default(InventoryGridViewport);
            pointerPixels = Vector2.zero;
            if (!IsNativeHierarchyComplete(snapshot.HasScroll, snapshot.HasGrid, snapshot.HasItemsPanel) ||
                !snapshot.ParentChainValid || snapshot.GridSize.x <= 0f || snapshot.GridSize.y <= 0f ||
                snapshot.ViewportSize.x <= 0f || snapshot.ViewportSize.y <= 0f) return false;
            var pointer = MapNormalizedPointer(snapshot.PointerNormalized.x, snapshot.PointerNormalized.y,
                snapshot.GridSize.x, snapshot.GridSize.y);
            if (pointer == Vector2.zero && (snapshot.PointerNormalized.x != 0f || snapshot.PointerNormalized.y != 0f)) return false;
            viewport = BuildLiveGridViewport(0, 0, snapshot.ViewportSize.x, snapshot.ViewportSize.y,
                0f, Mathf.Max(0f, snapshot.ScrollPixelsY));
            pointerPixels = pointer;
            return true;
        }

        internal static InventoryGridViewport BuildLiveGridViewport(byte gridWidth, byte gridHeight,
            float viewportWidth, float viewportHeight)
        {
            return BuildLiveGridViewport(gridWidth, gridHeight, viewportWidth, viewportHeight, 0f, 0f);
        }

        internal static InventoryGridViewport BuildLiveGridViewport(byte gridWidth, byte gridHeight,
            float viewportWidth, float viewportHeight, float clipX, float clipY)
        {
            if (float.IsNaN(viewportWidth) || float.IsInfinity(viewportWidth) || viewportWidth <= 0f) viewportWidth = gridWidth * 50f;
            if (float.IsNaN(viewportHeight) || float.IsInfinity(viewportHeight) || viewportHeight <= 0f) viewportHeight = gridHeight * 50f;
            return new InventoryGridViewport(0f, 0f, gridWidth, gridHeight,
                IsFiniteNonNegative(clipX) ? clipX : 0f,
                IsFiniteNonNegative(clipY) ? clipY : 0f, viewportWidth, viewportHeight);
        }

        // GPT watermark: the pointer sampled from SleekItems.grid is in the
        // moving content coordinate space. The clip therefore has to be
        // rebuilt from the current scroll position on every read, rather than
        // being frozen when the inventory surface is first opened.
        internal static InventoryGridViewport BuildDynamicGridViewport(byte gridWidth, byte gridHeight,
            float contentWidth, float contentHeight, float viewportWidth, float viewportHeight,
            float normalizedScrollY, float viewportRatio)
        {
            if (!IsFinitePositive(contentWidth)) contentWidth = gridWidth * 50f;
            if (!IsFinitePositive(contentHeight)) contentHeight = gridHeight * 50f;
            var clipY = ComputeScrollPixels(normalizedScrollY, viewportRatio, contentHeight);
            return BuildLiveGridViewport(gridWidth, gridHeight, viewportWidth, viewportHeight, 0f, clipY);
        }

        private readonly ContainerReference currentContainer;
        private readonly IVisualContainer topLevelContainer;
        private readonly IVisualContainer gridPanelContainer;
        private readonly InventoryGridViewport viewport;
        private readonly float uiScale;
        private readonly IGridOccupancyView occupancy;
        private readonly bool hierarchyLive;
        private readonly byte gridWidth;
        private readonly byte gridHeight;
        internal SleekItems NativeItems { get; }

        private readonly FieldInfo scrollViewField;
        private readonly FieldInfo gridField;
        private readonly FieldInfo itemsPanelField;
        internal PointerCoordinateMode CoordinateMode { get { return PointerCoordinateMode.GridContentIncludesScroll; } }
        internal bool ScrollAppliedByNativeGrid { get { return true; } }
        internal float NativeScrollPixelsY { get { return ReadScrollPixelsY(); } }

        internal UnturnedInventorySurfaceContext(ContainerReference currentContainer, IVisualContainer topLevelContainer,
            IVisualContainer gridPanelContainer, InventoryGridViewport viewport, float uiScale, IGridOccupancyView occupancy,
            SleekItems nativeItems, bool hierarchyLive)
        {
            this.currentContainer = currentContainer;
            this.topLevelContainer = topLevelContainer;
            this.gridPanelContainer = gridPanelContainer;
            this.viewport = viewport;
            this.uiScale = NormalizeUiScale(uiScale);
            this.occupancy = occupancy;
            this.hierarchyLive = hierarchyLive;
            gridWidth = occupancy == null ? (byte)0 : occupancy.Width;
            gridHeight = occupancy == null ? (byte)0 : occupancy.Height;
            NativeItems = nativeItems;
            scrollViewField = NativeScrollField;
            gridField = NativeGridField;
            itemsPanelField = NativeItemsPanelField;
        }

        public ContainerReference CurrentContainer { get { return currentContainer; } }
        public IVisualContainer TopLevelContainer { get { return topLevelContainer; } }
        public IVisualContainer GridPanelContainer { get { return gridPanelContainer; } }
        public InventoryGridViewport Viewport { get { return ReadLiveViewport(); } }
        public float CellPixelSize { get { return 50f; } }
        public float UiScale { get { return uiScale; } }
        public float ScrollPixelsX { get { return ReadScrollPixelsX(); } }
        public float ScrollPixelsY { get { return ReadScrollPixelsY(); } }
        public IGridOccupancyView Occupancy { get { return occupancy; } }

        // Sleek coordinates are local to the live inventory surface. Reading
        // the normalized cursor from that same surface closes the coordinate
        // space instead of treating PositionOffset as a screen origin.
        internal bool TryGetLocalPointerPixels(out float x, out float y)
        {
            x = 0f;
            y = 0f;
            var scroll = ResolveScrollView();
            var native = ResolveGrid();
            if (scroll == null || native == null || occupancy == null) return false;
            var viewportNormalized = scroll.GetNormalizedCursorPosition();
            if (float.IsNaN(viewportNormalized.x) || float.IsInfinity(viewportNormalized.x) ||
                float.IsNaN(viewportNormalized.y) || float.IsInfinity(viewportNormalized.y) ||
                viewportNormalized.x < 0f || viewportNormalized.x > 1f ||
                viewportNormalized.y < 0f || viewportNormalized.y > 1f) return false;
            var normalized = native.GetNormalizedCursorPosition();
            if (float.IsNaN(normalized.x) || float.IsInfinity(normalized.x) ||
                float.IsNaN(normalized.y) || float.IsInfinity(normalized.y)) return false;
            if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f) return false;
            var size = native.GetAbsoluteSize();
            if (size.x <= 0f || size.y <= 0f || float.IsNaN(size.x) || float.IsNaN(size.y)) return false;
            x = normalized.x * size.x;
            y = normalized.y * size.y;
            return true;
        }

        internal float ReadScrollPixelsY()
        {
            var scrollView = ResolveScrollView();
            if (scrollView == null || occupancy == null || occupancy.Height == 0) return 0f;
            var contentHeight = ReadGridContentHeight();
            if (!IsFinitePositive(contentHeight)) contentHeight = occupancy.Height * CellPixelSize * UiScale;
            return ComputeScrollPixels(scrollView.NormalizedVerticalPosition,
                scrollView.NormalizedViewportHeight, contentHeight);
        }

        private float ReadGridContentHeight()
        {
            var grid = ResolveGrid();
            if (grid == null) return 0f;
            var size = grid.GetAbsoluteSize();
            return size.y;
        }

        private InventoryGridViewport ReadLiveViewport()
        {
            if (!hierarchyLive) return viewport;
            var scroll = ResolveScrollView();
            var grid = ResolveGrid();
            if (scroll == null || grid == null) return viewport;
            var scrollSize = scroll.GetAbsoluteSize();
            var contentSize = grid.GetAbsoluteSize();
            return BuildDynamicGridViewport(gridWidth, gridHeight,
                contentSize.x, contentSize.y, scrollSize.x, scrollSize.y,
                scroll.NormalizedVerticalPosition, scroll.NormalizedViewportHeight);
        }

        internal static float ComputeScrollPixels(float normalizedPosition, float viewportRatio, float contentPixels)
        {
            if (float.IsNaN(normalizedPosition) || float.IsInfinity(normalizedPosition) ||
                float.IsNaN(viewportRatio) || float.IsInfinity(viewportRatio) ||
                float.IsNaN(contentPixels) || float.IsInfinity(contentPixels)) return 0f;
            var scrollable = Mathf.Max(0f, contentPixels) * (1f - Mathf.Clamp01(viewportRatio));
            return Mathf.Clamp01(normalizedPosition) * scrollable;
        }

        internal float ReadScrollPixelsX()
        {
            // SleekItems disables horizontal wheel input and sizes its content
            // to the viewport width, so supported inventory surfaces have no
            // horizontal overflow. Keep the seam explicit and fail closed.
            return 0f;
        }

        internal static float ResolveEffectiveScrollPixels(bool pointerAlreadyIncludesScroll, float nativeScrollPixels)
        {
            if (pointerAlreadyIncludesScroll) return 0f;
            return float.IsNaN(nativeScrollPixels) || float.IsInfinity(nativeScrollPixels)
                ? 0f : Mathf.Max(0f, nativeScrollPixels);
        }

        private ISleekScrollView ResolveScrollView()
        {
            if (NativeItems == null) return null;
            return scrollViewField == null ? null : scrollViewField.GetValue(NativeItems) as ISleekScrollView;
        }

        internal ISleekElement ResolveItemsPanel()
        {
            if (NativeItems == null) return null;
            return itemsPanelField == null ? null : itemsPanelField.GetValue(NativeItems) as ISleekElement;
        }

        private ISleekElement ResolveGrid()
        {
            if (NativeItems == null) return null;
            return gridField == null ? null : gridField.GetValue(NativeItems) as ISleekElement;
        }

    }

    /// <summary>
    /// DEV-16C adapter: probes the public native inventory members, keeps a
    /// polling watcher fed from the vanilla PlayerUI.Update postfix (R18 proved
    /// it survives the host sweep), tracks container sessions and generations,
    /// and projects the real surface into the pure-C# seam. Native objects
    /// never leave this file.
    /// </summary>
    internal sealed class InventorySurfaceLifecycleAdapter
    {
        private readonly BepInEx.Logging.ManualLogSource log;
        private readonly Action<IClientUiInventorySurface> openDispatcher;
        private readonly Action closeDispatcher;
        private readonly ContainerSessionTracker tracker;
        private readonly InventoryLifecycleWatcher watcher;
        private readonly bool enabled;
        private readonly Action isolateDispatcher;
        private readonly Action hideDispatcher;
        private readonly Action guardedPoll;
        private string gateDiagnostics;
        internal static string LastPollDiagnostics { get; private set; }
        private Harmony harmony;
        private bool hooksInstalled;
        private bool surfaceDispatched;
        private uint dispatchedGeneration;

        internal ContainerSessionTracker Tracker { get { return tracker; } }
        internal bool Enabled { get { return enabled; } }
        internal string GateDiagnostics { get { return gateDiagnostics; } }
        internal bool HooksInstalled { get { return hooksInstalled; } }

        internal InventorySurfaceLifecycleAdapter(BepInEx.Logging.ManualLogSource log,
            Action<IClientUiInventorySurface> openDispatcher, Action closeDispatcher,
            Action isolateDispatcher = null, Action hideDispatcher = null)
        {
            this.log = log;
            this.openDispatcher = openDispatcher ?? throw new ArgumentNullException(nameof(openDispatcher));
            this.closeDispatcher = closeDispatcher ?? throw new ArgumentNullException(nameof(closeDispatcher));
            this.isolateDispatcher = isolateDispatcher;
            this.hideDispatcher = hideDispatcher;
            tracker = new ContainerSessionTracker();
            watcher = new InventoryLifecycleWatcher(tracker);
            harmony = new Harmony("io.github.yu80rice.bue.inventory-lifecycle");
            guardedPoll = RunPollAndDragTick;

            var probe = new InventoryLifecycleProbe
            {
                DashboardActiveField = typeof(PlayerDashboardInventoryUI).GetField("active", BindingFlags.Static | BindingFlags.Public) != null,
                IsStoringField = typeof(PlayerInventory).GetField("isStoring", BindingFlags.Instance | BindingFlags.Public) != null,
                IsStorageTrunkField = typeof(PlayerInventory).GetField("isStorageTrunk", BindingFlags.Instance | BindingFlags.Public) != null,
                StorageField = typeof(PlayerInventory).GetField("storage", BindingFlags.Instance | BindingFlags.Public) != null,
                ConnectedProperty = AccessTools.Property(typeof(Provider), "isConnected") != null,
                LocalPlayerProperty = AccessTools.Property(typeof(Player), "LocalPlayer") != null
            };
            var result = InventoryLifecycleGate.Evaluate(isClientBranch: true, probe: probe);
            enabled = result.Enabled;
            gateDiagnostics = result.Diagnostics;
        }

        internal void Activate()
        {
            if (!enabled || hooksInstalled) return;
            try
            {
                var target = AccessTools.Method(typeof(PlayerUI), "Update");
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(InventorySurfaceLifecycleAdapter), nameof(PlayerUIUpdatePostfix)));
                hooksInstalled = true;
                ActiveAdapter = this;
                log?.LogInfo("[BUE-INVENTORY] event=polling-hook-installed target=PlayerUI.Update diagnosticId=BUE-INVENTORY-001");
            }
            catch (Exception error)
            {
                gateDiagnostics = "polling-hook-failed: " + error.GetType().FullName + ": " + error.Message;
                hooksInstalled = false;
            }
        }


        internal static InventorySurfaceLifecycleAdapter ActiveAdapter { get; private set; }

        internal static void ClearActive(InventorySurfaceLifecycleAdapter adapter)
        {
            if (ReferenceEquals(ActiveAdapter, adapter)) ActiveAdapter = null;
        }

        internal static void PlayerUIUpdatePostfix()
        {
            var adapter = ActiveAdapter;
            if (adapter == null) return;
            adapter.RunGuardedPoll();
        }

        private void RunGuardedPoll()
        {
            if (!InvokePollGuarded(guardedPoll, isolateDispatcher, CloseAfterPollFailure))
            {
                surfaceDispatched = false;
                dispatchedGeneration = 0;
            }
        }

        private void CloseAfterPollFailure()
        {
            try { hideDispatcher?.Invoke(); }
            finally { closeDispatcher(); }
        }

        private void RunPollAndDragTick()
        {
            Poll();
            // GPT watermark: PlayerUI.Update is a proven, observed main-thread
            // heartbeat in the runtime logs. Drive the drag preview from this
            // same callback instead of relying solely on an unscheduled child
            // Update method.
            InventoryDragPreviewAdapter.ActiveAdapter?.Tick();
        }

        // GPT watermark: all surface reflection/geometry failures terminate at
        // one guarded boundary, isolate only Better Item Interaction, and leave
        // vanilla inventory input available.
        internal static bool InvokePollGuarded(Action poll, Action isolate, Action hide)
        {
            try { poll?.Invoke(); return true; }
            catch (Exception error)
            {
                LastPollDiagnostics = "poll failed: " + error.GetType().FullName + ": " + error.Message;
                InventoryDragPreviewAdapter.FailClosedPreview(isolate, hide);
                return false;
            }
        }

        internal void Poll()
        {
            var player = Player.LocalPlayer;
            var inventory = player == null ? null : player.inventory;
            var dashboardActive = PlayerDashboardInventoryUI.active;
            var isStoring = inventory != null && inventory.isStoring;
            var isStorageTrunk = inventory != null && inventory.isStorageTrunk;
            var storage = inventory == null ? null : inventory.storage;
            // Identity anchor via RuntimeHelpers.GetHashCode: rare collisions
            // only risk one extra generation tick, never missed invalidation.
            var storageIdentity = storage == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(storage);
            var connected = Provider.isConnected;

            watcher.Feed(new InventoryLifecycleSnapshot(dashboardActive, isStoring, isStorageTrunk, storageIdentity, connected));

            if (!tracker.TryGetActiveGeneration(out var generation))
            {
                if (surfaceDispatched)
                {
                    surfaceDispatched = false;
                    closeDispatcher();
                }
                return;
            }

            if (!surfaceDispatched || generation != dispatchedGeneration)
            {
                var kind = tracker.Kind;
                // PlayerInventory V1 surfaces the backpack grid page; storage
                // and trunk both live on the shared STORAGE page.
                var page = kind == ContainerSessionKind.PlayerInventory ? (byte)PlayerInventory.BACKPACK : (byte)PlayerInventory.STORAGE;
                var context = BuildSurfaceContext(kind, page, generation);
                if (context == null) return;
                openDispatcher(context);
                surfaceDispatched = true;
                dispatchedGeneration = generation;
                // [DEV-16C] Geometry calibration readout for the real machine:
                // these are the approximate viewport values DEV-16D consumes.
                log?.LogInfo("[BUE-INVENTORY] event=surface-context-dispatched kind=" + kind + " page=" + page + " generation=" + generation
                    + " viewportOrigin=" + context.Viewport.OriginX + "," + context.Viewport.OriginY + " (approx)"
                    + " grid=" + context.Viewport.GridWidth + "x" + context.Viewport.GridHeight
                    + " clip=" + context.Viewport.ClipWidth + "x" + context.Viewport.ClipHeight
                    + " uiScale=" + context.UiScale.ToString("0.##") + " cellPx=" + context.CellPixelSize + " (static)"
                    + " scroll=" + context.ScrollPixelsX.ToString("0.###") + "," + context.ScrollPixelsY.ToString("0.###")
                    + " nativeScrollY=" + context.NativeScrollPixelsY.ToString("0.###")
                    + " diagnosticId=BUE-INVENTORY-001");
            }
        }

        internal UnturnedInventorySurfaceContext BuildSurfaceContext(ContainerSessionKind kind, byte page, uint generation)
        {
            var player = Player.LocalPlayer;
            if (player == null) return null;
            var playerInventory = player.inventory;
            if (playerInventory == null || playerInventory.items == null) return null;
            if (page >= PlayerInventory.PAGES || playerInventory.items[page] == null) return null;

            var dashboardFields = typeof(PlayerDashboardInventoryUI);
            if (UnturnedInventorySurfaceContext.DashboardItemsField == null) return null;
            var dashboardItems = UnturnedInventorySurfaceContext.DashboardItemsField.GetValue(null) as Array;
            var dashboardIndex = page - PlayerInventory.SLOTS;
            if (dashboardItems == null || dashboardIndex < 0 || dashboardIndex >= dashboardItems.Length) return null;

            var sleekItems = dashboardItems.GetValue(dashboardIndex) as SleekItems;
            if (sleekItems == null) return null;

            var dataItems = playerInventory.items[page];

            var nativeScroll = UnturnedInventorySurfaceContext.NativeScrollField == null ? null : UnturnedInventorySurfaceContext.NativeScrollField.GetValue(sleekItems) as ISleekScrollView;
            var nativeGrid = UnturnedInventorySurfaceContext.NativeGridField == null ? null : UnturnedInventorySurfaceContext.NativeGridField.GetValue(sleekItems) as ISleekElement;
            var nativePanel = UnturnedInventorySurfaceContext.NativeItemsPanelField == null ? null : UnturnedInventorySurfaceContext.NativeItemsPanelField.GetValue(sleekItems) as ISleekElement;
            // [R40 fail-open] The live hierarchy snapshot is an upgrade, not a
            // gate: when it is incomplete or inconsistent we degrade to the
            // offset/size approximation (R37 path) instead of dropping the
            // whole surface, which left the preview chain dead on the real
            // machine (214114 package: surface-context-dispatched == 0).
            var hierarchyLive = UnturnedInventorySurfaceContext.IsNativeHierarchyComplete(nativeScroll != null, nativeGrid != null, nativePanel != null) &&
                UnturnedInventorySurfaceContext.IsNativeHierarchyConsistent(sleekItems, nativeScroll, nativeGrid, nativePanel,
                    nativeScroll?.Parent, nativeGrid?.Parent, nativePanel?.Parent);
            if (!hierarchyLive)
            {
                log?.LogWarning("[BUE-INVENTORY] event=hierarchy-snapshot-degraded page=" + page + " diagnosticId=BUE-INVENTORY-003");
            }
            if (PlayerUI.container == null) return null;
            var topLevel = new UnturnedVisualContainer(PlayerUI.container);
            if (nativePanel == null || nativeGrid == null || nativeScroll == null) return null;
            var gridPanel = new UnturnedVisualContainer(nativePanel);

            // Geometry is expressed in the live SleekItems local space. The
            // previous implementation copied PositionOffset into a screen
            // origin, which mixed parent and child coordinate systems and
            // yielded OutsideGrid for every real pointer.
            InventoryGridViewport viewport;
            float uiScale = 1f;
            uiScale = GraphicsSettings.userInterfaceScale;
            if (uiScale <= 0f) uiScale = 1f;

            var scrollSize = Vector2.zero;
            if (hierarchyLive)
            {
                scrollSize = nativeScroll.GetAbsoluteSize();
            }
            if (scrollSize.x <= 0f || scrollSize.y <= 0f || float.IsNaN(scrollSize.x) || float.IsNaN(scrollSize.y))
            {
                // [R40 fail-open] Live scroll size unavailable: degrade to the
                // offset/size approximation so the session still dispatches
                // and the preview chain stays alive (calibration can follow).
                log?.LogWarning("[BUE-INVENTORY] event=scroll-size-degraded page=" + page + " diagnosticId=BUE-INVENTORY-003");
            }
            var scrollPixelsX = 0f;
            var scrollPixelsY = 0f;
            if (nativeScroll != null)
            {
                scrollPixelsY = UnturnedInventorySurfaceContext.ComputeScrollPixels(
                    nativeScroll.NormalizedVerticalPosition, nativeScroll.NormalizedViewportHeight,
                    dataItems.height * 50f * uiScale);
            }
            viewport = UnturnedInventorySurfaceContext.ResolveViewport(hierarchyLive, scrollSize,
                (byte)dataItems.width, (byte)dataItems.height,
                sleekItems.PositionOffset_X, sleekItems.PositionOffset_Y,
                sleekItems.SizeOffset_X, sleekItems.SizeOffset_Y, scrollPixelsX, scrollPixelsY);

            return new UnturnedInventorySurfaceContext(
                new ContainerReference(MapKind(kind), page, generation),
                 topLevel, gridPanel, viewport, uiScale,
                 new UnturnedGridOccupancyView(dataItems), sleekItems, hierarchyLive);
        }

        private static ContainerKind MapKind(ContainerSessionKind kind)
        {
            if (kind == ContainerSessionKind.Trunk || kind == ContainerSessionKind.Storage) return ContainerKind.Storage;
            return ContainerKind.PlayerInventory;
        }
    }
}
