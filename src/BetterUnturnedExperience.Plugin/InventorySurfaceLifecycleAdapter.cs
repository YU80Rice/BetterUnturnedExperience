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

        internal UnturnedVisualElement(ISleekElement element)
        {
            this.element = element ?? throw new ArgumentNullException(nameof(element));
            box = element as ISleekBox;
            image = element as ISleekImage;
        }

        public float PositionOffsetX { get { return element.PositionOffset_X; } set { element.PositionOffset_X = value; } }
        public float PositionOffsetY { get { return element.PositionOffset_Y; } set { element.PositionOffset_Y = value; } }
        public float SizeOffsetX { get { return element.SizeOffset_X; } set { element.SizeOffset_X = value; } }
        public float SizeOffsetY { get { return element.SizeOffset_Y; } set { element.SizeOffset_Y = value; } }

        public byte RotationAngle
        {
            get { return image == null ? (byte)0 : (byte)((int)Math.Round(Mathf.Repeat(image.RotationAngle, 360f) / 90f) % 4); }
            set { if (image != null) image.RotationAngle = value * 90f; }
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
            get { return ItemAssetIdentity.FromItemId(0); }
            set
            {
                // Real texture path (DEV-16D): ItemTool.getIcon resolves the
                // native icon asynchronously and the callback assigns the
                // texture. Until the callback lands the element stays blank —
                // the pure-value binding is never reported as rendered.
                if (image == null || value.ItemId == 0)
                {
                    if (image != null) image.Texture = null;
                    return;
                }
                var asset = Assets.find(EAssetType.ITEM, value.ItemId) as ItemAsset;
                try
                {
                    ItemTool.getIcon(value.ItemId, 0, Array.Empty<byte>(), asset,
                        (handle, texture) => { image.Texture = texture; });
                }
                catch (Exception)
                {
                    // Icon resolution is best-effort; a blank texture keeps
                    // the drag alive without faking a rendered icon.
                    image.Texture = null;
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
        public IVisualElement CreateImage() { return new UnturnedVisualElement(Glazier.Get().CreateImage()); }
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
        private readonly ContainerReference currentContainer;
        private readonly IVisualContainer topLevelContainer;
        private readonly IVisualContainer gridPanelContainer;
        private readonly InventoryGridViewport viewport;
        private readonly float uiScale;
        private readonly IGridOccupancyView occupancy;

        internal UnturnedInventorySurfaceContext(ContainerReference currentContainer, IVisualContainer topLevelContainer,
            IVisualContainer gridPanelContainer, InventoryGridViewport viewport, float uiScale, IGridOccupancyView occupancy)
        {
            this.currentContainer = currentContainer;
            this.topLevelContainer = topLevelContainer;
            this.gridPanelContainer = gridPanelContainer;
            this.viewport = viewport;
            this.uiScale = uiScale;
            this.occupancy = occupancy;
        }

        public ContainerReference CurrentContainer { get { return currentContainer; } }
        public IVisualContainer TopLevelContainer { get { return topLevelContainer; } }
        public IVisualContainer GridPanelContainer { get { return gridPanelContainer; } }
        public InventoryGridViewport Viewport { get { return viewport; } }
        public float CellPixelSize { get { return 50f; } }
        public float UiScale { get { return uiScale; } }
        public float ScrollPixelsX { get { return 0f; } }
        public float ScrollPixelsY { get { return 0f; } }
        public IGridOccupancyView Occupancy { get { return occupancy; } }
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
            Action<IClientUiInventorySurface> openDispatcher, Action closeDispatcher)
        {
            this.log = log;
            this.openDispatcher = openDispatcher ?? throw new ArgumentNullException(nameof(openDispatcher));
            this.closeDispatcher = closeDispatcher ?? throw new ArgumentNullException(nameof(closeDispatcher));
            tracker = new ContainerSessionTracker();
            watcher = new InventoryLifecycleWatcher(tracker);
            harmony = new Harmony("io.github.yu80rice.bue.inventory-lifecycle");

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

        internal static void PlayerUIUpdatePostfix()
        {
            var adapter = ActiveAdapter;
            if (adapter == null) return;
            try { adapter.Poll(); } catch (Exception error) { LastPollDiagnostics = "poll failed: " + error.GetType().FullName + ": " + error.Message; }
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
                    + " scroll=0(approx)"
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
            var dashboardItemsField = dashboardFields.GetField("items", BindingFlags.Static | BindingFlags.NonPublic);
            if (dashboardItemsField == null) return null;
            var dashboardItems = dashboardItemsField.GetValue(null) as Array;
            var dashboardIndex = page - PlayerInventory.SLOTS;
            if (dashboardItems == null || dashboardIndex < 0 || dashboardIndex >= dashboardItems.Length) return null;

            var sleekItems = dashboardItems.GetValue(dashboardIndex) as SleekItems;
            if (sleekItems == null) return null;

            var dataItems = playerInventory.items[page];

            var gridPanel = new UnturnedVisualContainer(sleekItems);
            var topLevel = new UnturnedVisualContainer(sleekItems);

            // Approximate geometry: element offsets stand in for the true
            // screen-space origin and the scroll offsets are read as zero;
            // both are calibrated on the real machine with DEV-16D.
            var viewport = new InventoryGridViewport(
                sleekItems.PositionOffset_X, sleekItems.PositionOffset_Y,
                (byte)dataItems.width, (byte)dataItems.height,
                sleekItems.PositionOffset_X, sleekItems.PositionOffset_Y,
                sleekItems.SizeOffset_X, sleekItems.SizeOffset_Y);

            float uiScale = 1f;
            try { uiScale = GraphicsSettings.userInterfaceScale; } catch (Exception) { uiScale = 1f; }
            if (uiScale <= 0f) uiScale = 1f;

            return new UnturnedInventorySurfaceContext(
                new ContainerReference(MapKind(kind), page, generation),
                topLevel, gridPanel, viewport, uiScale,
                new UnturnedGridOccupancyView(dataItems));
        }

        private static ContainerKind MapKind(ContainerSessionKind kind)
        {
            if (kind == ContainerSessionKind.Trunk || kind == ContainerSessionKind.Storage) return ContainerKind.Storage;
            return ContainerKind.PlayerInventory;
        }
    }
}
