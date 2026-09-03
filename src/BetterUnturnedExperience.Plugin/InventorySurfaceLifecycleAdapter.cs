using System;
using System.Collections.Generic;
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
        private NativeItemGridOccupancySnapshot snapshot;
        private bool hasConfiguration;
        private ItemJar configuredExcludedJar;
        private ItemGridPosition configuredSource;
        private int configuredItemCount;

        internal UnturnedGridOccupancyView(Items items)
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
            if (!NativeItemGridOccupancySnapshot.TryCreateFromItems(items, null, out snapshot))
                throw new InvalidOperationException("native inventory occupancy snapshot is invalid");
            configuredItemCount = items.items.Count;
            hasConfiguration = true;
        }

        public byte Width { get { return snapshot.Width; } }
        public byte Height { get { return snapshot.Height; } }

        public bool IsOccupied(byte x, byte y)
        {
            return snapshot.IsOccupied(x, y);
        }

        internal IGridOccupancyView CurrentSnapshot { get { return snapshot; } }

        internal bool RebuildForDrag(ContainerReference sourceContainer, ContainerReference targetContainer,
            ItemGridPosition source, ItemJar dragJar, ItemAssetIdentity sourceAsset,
            byte itemWidth, byte itemHeight, byte sourceRotation)
        {
            ItemJar excludedJar = null;
            var sameContainer = SameContainer(sourceContainer, targetContainer) && source.Page == items.page &&
                sourceAsset.ItemId != 0 &&
                itemWidth != 0 && itemHeight != 0;
            if (sameContainer)
            {
                // Same-container source exclusion is safe only when the native
                // drag jar is the exact member and its source metadata matches.
                if (!SourceExclusionMetadataMatches(dragJar, source, sourceAsset, itemWidth, itemHeight,
                    sourceRotation, items.containsItem(dragJar), AssetIdentityMatches(dragJar, sourceAsset)))
                    return false;
                excludedJar = dragJar;
            }
            else if (SameContainer(sourceContainer, targetContainer) && source.Page == items.page)
            {
                // A same-page source with incomplete identity is ambiguous;
                // do not silently exclude a potentially different item.
                return false;
            }

            if (hasConfiguration && object.ReferenceEquals(configuredExcludedJar, excludedJar) &&
                configuredSource.Page == source.Page && configuredSource.X == source.X &&
                configuredSource.Y == source.Y && configuredSource.Rotation == source.Rotation &&
                configuredItemCount == items.items.Count)
                return true;

            NativeItemGridOccupancySnapshot next;
            if (!NativeItemGridOccupancySnapshot.TryCreateFromItems(items, excludedJar, out next))
                return false;
            snapshot = next;
            configuredExcludedJar = excludedJar;
            configuredSource = source;
            configuredItemCount = items.items.Count;
            hasConfiguration = true;
            return true;
        }

        private static bool SameContainer(ContainerReference left, ContainerReference right)
        {
            return left.SessionGeneration != 0 && left.Kind == right.Kind && left.Page == right.Page &&
                left.SessionGeneration == right.SessionGeneration;
        }

        private static bool AssetIdentityMatches(ItemJar jar, ItemAssetIdentity expected)
        {
            var asset = jar == null ? null : jar.GetAsset();
            if (asset == null || expected.ItemId == 0) return false;
            return expected == ItemAssetIdentity.FromAsset(asset.id, asset.GUID, asset.name);
        }

        // Testable source-exclusion seam. Source rotation comes from the
        // frozen dragFromRot snapshot; the mutable drag jar rotation is never
        // compared here because native updateDraggedItem changes it in place.
        internal static bool SourceExclusionMetadataMatches(ItemJar jar, ItemGridPosition source,
            ItemAssetIdentity expected, byte itemWidth, byte itemHeight, byte sourceRotation,
            bool jarIsInItems, bool assetIdentityMatches)
        {
            return jar != null && jarIsInItems && assetIdentityMatches &&
                jar.x == source.X && jar.y == source.Y &&
                (source.Rotation & 3) == (sourceRotation & 3) &&
                jar.size_x == itemWidth && jar.size_y == itemHeight;
        }

        // GPT watermark: R13-3 source rotation seam. Native rotation mutates
        // the drag jar, while ItemGridPosition.Rotation is the frozen
        // dragFromRot snapshot used for source-footprint exclusion.
        internal static byte ResolveSourceRotation(ItemGridPosition source, byte mutableJarRotation)
        {
            return (byte)(source.Rotation & 3);
        }

        internal void Invalidate()
        {
            hasConfiguration = false;
        }

    }

    /// <summary>
    /// Real-engine implementation of IInventorySurfaceContext for the dashboard
    /// inventory UI (player pages, storage and vehicle trunk share the STORAGE
    /// surface; the storage/trunk distinction lives in ContainerReference.Kind).
    /// </summary>
    internal sealed class UnturnedInventorySurfaceContext : IInventoryPointerSurfaceContext, INativeInventoryOccupancyProvider
    {
        internal enum PointerReadFailure : byte
        {
            None,
            NotReady,
            OutsideViewport,
            InvalidGeometry,
            ReadException
        }

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
            // point is visible. A missing hierarchy is allowed to use the
            // already-captured SleekItems size as an explicit degraded input,
            // but invalid values must never become silent grid-sized defaults.
            if (gridWidth == 0 || gridHeight == 0 || !IsFinite(offsetX) || !IsFinite(offsetY) ||
                !IsFinite(scrollPixelsX) || !IsFinite(scrollPixelsY) || scrollPixelsX < 0f || scrollPixelsY < 0f)
                throw new InvalidOperationException("native inventory viewport values are invalid");
            float clipW;
            float clipH;
            if (hierarchyLive)
            {
                if (!IsFinitePositive(scrollSize.x) || !IsFinitePositive(scrollSize.y))
                    throw new InvalidOperationException("native inventory scroll viewport size is invalid");
                clipW = scrollSize.x;
                clipH = scrollSize.y;
            }
            else
            {
                if (!IsFinitePositive(sizeX) || !IsFinitePositive(sizeY))
                    throw new InvalidOperationException("native inventory degraded viewport size is invalid");
                clipW = sizeX;
                clipH = sizeY;
            }
            return new InventoryGridViewport(0f, 0f, gridWidth, gridHeight,
                scrollPixelsX, scrollPixelsY, clipW, clipH);
        }

        private static bool IsFinitePositive(float value)
        {
            return IsFinite(value) && value > 0f;
        }

        // GPT watermark: R13-R6-scrollsize seam. A freshly opened dashboard's
        // native horizontalScrollView is not laid out on the first frames, so
        // GetAbsoluteSize() returns 0/NaN. That transient state must route to
        // the not-ready retry (return null in BuildSurfaceContext) instead of
        // throwing and isolating the whole feature.
        internal static bool IsValidScrollViewportSize(UnityEngine.Vector2 size)
        {
            return size.x > 0f && size.y > 0f && IsFinite(size.x) && IsFinite(size.y);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return IsFinite(value) && value >= 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
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
            if (!IsFinitePositive(value))
                throw new InvalidOperationException("native inventory UI scale is invalid");
            return value;
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

        internal enum NativeHierarchyState : byte
        {
            NotCreated,
            Ready,
            Incompatible
        }

        internal static NativeHierarchyState ClassifyNativeHierarchy(bool hasOwner, bool membersAvailable,
            bool hasScroll, bool hasGrid, bool hasItemsPanel, bool parentChainValid)
        {
            if (!hasOwner) return NativeHierarchyState.NotCreated;
            if (!membersAvailable) return NativeHierarchyState.Incompatible;
            if (!hasScroll || !hasGrid || !hasItemsPanel)
                return NativeHierarchyState.Incompatible;
            if (!parentChainValid)
                return NativeHierarchyState.Incompatible;
            return NativeHierarchyState.Ready;
        }

        // A null SleekItems means the native surface has not been created yet
        // and is retryable. Once a SleekItems owner exists, missing children or
        // a broken parent chain is a compatibility failure and must isolate the
        // feature instead of spinning forever in "not ready".
        internal static NativeHierarchyState ProbeNativeHierarchy(SleekItems sleekItems,
            out ISleekScrollView scroll, out ISleekElement grid, out ISleekElement itemsPanel)
        {
            scroll = null;
            grid = null;
            itemsPanel = null;
            if (sleekItems == null) return NativeHierarchyState.NotCreated;
            if (NativeScrollField == null || NativeGridField == null || NativeItemsPanelField == null)
                return NativeHierarchyState.Incompatible;
            try
            {
                scroll = NativeScrollField.GetValue(sleekItems) as ISleekScrollView;
                grid = NativeGridField.GetValue(sleekItems) as ISleekElement;
                itemsPanel = NativeItemsPanelField.GetValue(sleekItems) as ISleekElement;
            }
            catch (Exception)
            {
                return NativeHierarchyState.Incompatible;
            }
            var parentChainValid = scroll != null && grid != null && itemsPanel != null &&
                IsNativeHierarchyConsistent(sleekItems, scroll, grid, itemsPanel,
                    scroll.Parent, grid.Parent, itemsPanel.Parent);
            return ClassifyNativeHierarchy(true, true, scroll != null, grid != null,
                itemsPanel != null, parentChainValid);
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
                snapshot.ViewportSize.x <= 0f || snapshot.ViewportSize.y <= 0f ||
                !IsFinitePositive(snapshot.GridSize.x) || !IsFinitePositive(snapshot.GridSize.y) ||
                !IsFinitePositive(snapshot.ViewportSize.x) || !IsFinitePositive(snapshot.ViewportSize.y) ||
                !IsFiniteNonNegative(snapshot.ScrollPixelsY) || !IsFinitePositive(snapshot.UiScale)) return false;
            var pointer = MapNormalizedPointer(snapshot.PointerNormalized.x, snapshot.PointerNormalized.y,
                snapshot.GridSize.x, snapshot.GridSize.y);
            if (pointer == Vector2.zero && (snapshot.PointerNormalized.x != 0f || snapshot.PointerNormalized.y != 0f)) return false;
            var gridWidth = (byte)Mathf.Clamp(Mathf.RoundToInt(snapshot.GridSize.x / 50f), 1, byte.MaxValue);
            var gridHeight = (byte)Mathf.Clamp(Mathf.RoundToInt(snapshot.GridSize.y / 50f), 1, byte.MaxValue);
            viewport = BuildLiveGridViewport(gridWidth, gridHeight, snapshot.ViewportSize.x, snapshot.ViewportSize.y,
                0f, snapshot.ScrollPixelsY);
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
            if (!IsFinitePositive(viewportWidth) || !IsFinitePositive(viewportHeight) ||
                !IsFiniteNonNegative(clipX) || !IsFiniteNonNegative(clipY))
                throw new InvalidOperationException("native inventory live viewport geometry is invalid");
            return new InventoryGridViewport(0f, 0f, gridWidth, gridHeight,
                clipX, clipY, viewportWidth, viewportHeight);
        }

        // GPT watermark: the pointer sampled from SleekItems.grid is in the
        // moving content coordinate space. The clip therefore has to be
        // rebuilt from the current scroll position on every read, rather than
        // being frozen when the inventory surface is first opened.
        internal static InventoryGridViewport BuildDynamicGridViewport(byte gridWidth, byte gridHeight,
            float contentWidth, float contentHeight, float viewportWidth, float viewportHeight,
            float normalizedScrollY, float viewportRatio)
        {
            if (!IsFinitePositive(contentWidth) || !IsFinitePositive(contentHeight) ||
                !IsFinitePositive(viewportWidth) || !IsFinitePositive(viewportHeight))
                throw new InvalidOperationException("native inventory geometry is invalid");
            var clipY = ComputeScrollPixels(normalizedScrollY, viewportRatio, contentHeight);
            return BuildLiveGridViewport(gridWidth, gridHeight, viewportWidth, viewportHeight, 0f, clipY);
        }

        private readonly ContainerReference currentContainer;
        private readonly IVisualContainer topLevelContainer;
        private readonly IVisualContainer gridPanelContainer;
        private readonly InventoryGridViewport viewport;
        private readonly float uiScale;
        private readonly IGridOccupancyView occupancy;
        private readonly UnturnedGridOccupancyView occupancyAdapter;
        private readonly bool hierarchyLive;
        private readonly byte gridWidth;
        private readonly byte gridHeight;
        internal SleekItems NativeItems { get; }
        internal ISleekScrollView NativeScroll { get { return ResolveScrollView(); } }
        internal ISleekElement NativeGrid { get { return ResolveGrid(); } }
        internal ISleekElement NativeItemsPanel { get { return ResolveItemsPanel(); } }

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
            occupancyAdapter = occupancy as UnturnedGridOccupancyView;
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

        public bool TryCreateOccupancyForDrag(ContainerReference sourceContainer, ContainerReference targetContainer,
            ItemGridPosition source, byte itemWidth, byte itemHeight, byte sourceRotation,
            ItemAssetIdentity sourceAsset, out IGridOccupancyView result)
        {
            result = null;
            if (occupancyAdapter == null) return false;
            var dragJarField = typeof(PlayerDashboardInventoryUI).GetField("dragJar",
                BindingFlags.Static | BindingFlags.NonPublic);
            var dragJar = dragJarField == null ? null : dragJarField.GetValue(null) as ItemJar;
            var frozenSourceRotation = UnturnedGridOccupancyView.ResolveSourceRotation(source, sourceRotation);
            if (!occupancyAdapter.RebuildForDrag(sourceContainer, targetContainer, source, dragJar, sourceAsset,
                itemWidth, itemHeight, frozenSourceRotation)) return false;
            result = occupancyAdapter.CurrentSnapshot;
            return true;
        }

        public void InvalidateOccupancy()
        {
            if (occupancyAdapter != null) occupancyAdapter.Invalidate();
        }

        // Sleek coordinates are local to the live inventory surface. Reading
        // the normalized cursor from that same surface closes the coordinate
        // space instead of treating PositionOffset as a screen origin.
        public bool TryGetLocalPointerPixels(out float x, out float y)
        {
            PointerReadFailure ignored;
            return TryGetLocalPointerPixels(out x, out y, out ignored);
        }

        internal bool TryGetLocalPointerPixels(out float x, out float y, out PointerReadFailure failure)
        {
            x = 0f;
            y = 0f;
            failure = PointerReadFailure.None;
            try
            {
                var scroll = ResolveScrollView();
                var native = ResolveGrid();
                if (scroll == null || native == null || occupancy == null)
                {
                    failure = PointerReadFailure.NotReady;
                    return false;
                }
                var viewportNormalized = scroll.GetNormalizedCursorPosition();
                if (!IsFiniteUnit(viewportNormalized))
                {
                    failure = PointerReadFailure.InvalidGeometry;
                    return false;
                }
                if (!IsUnitRange(viewportNormalized))
                {
                    failure = PointerReadFailure.OutsideViewport;
                    return false;
                }
                var normalized = native.GetNormalizedCursorPosition();
                if (!IsFinite(normalized))
                {
                    failure = PointerReadFailure.InvalidGeometry;
                    return false;
                }
                if (!IsUnitRange(normalized))
                {
                    failure = PointerReadFailure.OutsideViewport;
                    return false;
                }
                var size = native.GetAbsoluteSize();
                if (!IsFinitePositive(size.x) || !IsFinitePositive(size.y))
                {
                    failure = PointerReadFailure.InvalidGeometry;
                    return false;
                }
                x = normalized.x * size.x;
                y = normalized.y * size.y;
                return true;
            }
            catch (Exception)
            {
                failure = PointerReadFailure.ReadException;
                return false;
            }
        }

        internal float ReadScrollPixelsY()
        {
            var scrollView = ResolveScrollView();
            if (scrollView == null || occupancy == null || occupancy.Height == 0)
                throw new InvalidOperationException("native inventory scroll hierarchy is unavailable");
            var contentHeight = ReadGridContentHeight();
            if (!IsFinitePositive(contentHeight))
                throw new InvalidOperationException("native inventory content height is invalid");
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
            if (!hierarchyLive)
                throw new InvalidOperationException("native inventory hierarchy is not ready");
            var scroll = ResolveScrollView();
            var grid = ResolveGrid();
            if (scroll == null || grid == null)
                throw new InvalidOperationException("native inventory viewport hierarchy is unavailable");
            var scrollSize = scroll.GetAbsoluteSize();
            var contentSize = grid.GetAbsoluteSize();
            if (!IsFinitePositive(scrollSize.x) || !IsFinitePositive(scrollSize.y) ||
                !IsFinitePositive(contentSize.x) || !IsFinitePositive(contentSize.y))
                throw new InvalidOperationException("native inventory viewport geometry is invalid");
            return BuildDynamicGridViewport(gridWidth, gridHeight,
                contentSize.x, contentSize.y, scrollSize.x, scrollSize.y,
                scroll.NormalizedVerticalPosition, scroll.NormalizedViewportHeight);
        }

        internal static float ComputeScrollPixels(float normalizedPosition, float viewportRatio, float contentPixels)
        {
            if (float.IsNaN(normalizedPosition) || float.IsInfinity(normalizedPosition) ||
                float.IsNaN(viewportRatio) || float.IsInfinity(viewportRatio) ||
                float.IsNaN(contentPixels) || float.IsInfinity(contentPixels) ||
                normalizedPosition < 0f || normalizedPosition > 1f ||
                viewportRatio < 0f || viewportRatio > 1f || contentPixels <= 0f)
                throw new InvalidOperationException("native inventory scroll values are invalid");
            var scrollable = contentPixels * (1f - viewportRatio);
            return normalizedPosition * scrollable;
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }

        private static bool IsFiniteUnit(Vector2 value)
        {
            return IsFinite(value);
        }

        private static bool IsUnitRange(Vector2 value)
        {
            return value.x >= 0f && value.x <= 1f && value.y >= 0f && value.y <= 1f;
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
            if (!IsFiniteNonNegative(nativeScrollPixels))
                throw new InvalidOperationException("native inventory scroll pixels are invalid");
            return nativeScrollPixels;
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
        internal BepInEx.Logging.ManualLogSource Log { get { return log; } }
        private readonly Action<IClientUiInventorySurface> openDispatcher;
        private readonly Action closeDispatcher;
        private readonly ContainerSessionTracker tracker;
        private readonly InventoryLifecycleWatcher watcher;
        private readonly bool enabled;
        private readonly Func<bool> isolateDispatcher;
        private readonly Action hideDispatcher;
        private readonly Action<byte> discardPageDispatcher;
        private readonly Action guardedPoll;
        private string gateDiagnostics;
        internal static string LastPollDiagnostics { get; private set; }
        private Harmony harmony;
        private bool hooksInstalled;
        private bool isolated;
        private bool isolationSucceeded = true;
        private readonly Dictionary<byte, DispatchedSurfaceState> dispatchedSurfaces =
            new Dictionary<byte, DispatchedSurfaceState>();

        internal sealed class DispatchedSurfaceState
        {
            internal readonly uint Generation;
            internal readonly SleekItems NativeItems;
            internal readonly ISleekScrollView NativeScroll;
            internal readonly ISleekElement NativeGrid;
            internal readonly ISleekElement NativeItemsPanel;

            internal DispatchedSurfaceState(uint generation, SleekItems nativeItems,
                ISleekScrollView nativeScroll, ISleekElement nativeGrid, ISleekElement nativeItemsPanel)
            {
                Generation = generation;
                NativeItems = nativeItems;
                NativeScroll = nativeScroll;
                NativeGrid = nativeGrid;
                NativeItemsPanel = nativeItemsPanel;
            }
        }

        // U3-SDK PlayerInventory pages: 2=Hands, 3=Backpack, 4=Vest, 5=Shirt,
        // 6=Pants, 7=Storage/trunk, 8=AREA. Every player grid page is a live
        // surface; AREA stays native (ground). Keep the static dispatch table
        // literal so loading this adapter does not trigger PlayerInventory's
        // network-reflection initializer in a headless test host.
        private const byte HandsPage = 2;
        private const byte BackpackPage = 3;
        private const byte VestPage = 4;
        private const byte ShirtPage = 5;
        private const byte PantsPage = 6;
        private const byte StoragePage = 7;
        private static readonly byte[] SupportedSurfacePages =
            { HandsPage, BackpackPage, VestPage, ShirtPage, PantsPage, StoragePage };

        // GPT watermark: DEV-16G slice A. Per-page readiness state tracker that
        // converts the old per-frame `surface-not-ready` flood into one-shot
        // state-transition lines. A page that stays in the same readiness state
        // is SILENT every frame; only a transition emits one line:
        //   not-ready -> ready: "event=surface-ready page=N"
        //   ready -> not-ready: "event=surface-not-ready page=N reason=<reason>"
        // The gate is pure C# (no Unity), so host tests drive it directly; the
        // production Poll feeds each page's observed readiness each frame.
        internal sealed class SurfaceReadinessGate
        {
            private sealed class PageState
            {
                internal bool Ready;
                internal bool HasObserved;
            }

            private readonly System.Collections.Generic.Dictionary<byte, PageState> states =
                new System.Collections.Generic.Dictionary<byte, PageState>();

            // Returns true when a state transition occurred (caller should log
            // one line). logLine carries the ready/failed line to emit.
            internal bool Observe(byte page, bool ready, string notReadyReason, out string logLine)
            {
                logLine = null;
                PageState state;
                if (!states.TryGetValue(page, out state))
                {
                    state = new PageState();
                    states[page] = state;
                }

                if (!state.HasObserved)
                {
                    // First observation: seed the state WITHOUT emitting, so the
                    // steady-state flood is silent from the start.
                    state.Ready = ready;
                    state.HasObserved = true;
                    return false;
                }

                if (state.Ready == ready)
                {
                    return false;
                }

                state.Ready = ready;
                if (ready)
                {
                    logLine = "[BUE-INVENTORY] event=surface-ready page=" + page
                        + " diagnosticId=BUE-INVENTORY-001";
                }
                else
                {
                    logLine = "[BUE-INVENTORY] event=surface-not-ready page=" + page
                        + " reason=" + (notReadyReason ?? "unknown")
                        + " diagnosticId=BUE-INVENTORY-004";
                }
                return true;
            }
        }

        // GPT watermark: DEV-16G slice B. Static diagnostic emission seam. The
        // rich one-shot failure reasons (LastPollDiagnostics, Describe*) were
        // built but never logged in production; this sink routes them to the
        // BepInEx log per failure stage. Tests swap in a recorder to assert the
        // line is emitted with the reason intact.
        internal static System.Action<string> DiagnosticLogSink = null;

        internal static void EmitDiagnosticOnce(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            var sink = DiagnosticLogSink;
            if (sink != null)
            {
                sink(line);
                return;
            }
            // Production default: no-op unless an adapter binds its log source.
            // Adapters set DiagnosticLogSink during Activate so runtime failures
            // surface in the BepInEx log; the sink is a static seam kept
            // separate from the per-instance log for testability.
        }

        // GPT watermark: DEV-16G slice A. Per-page readiness gate instance fed
        // by Poll every frame; it emits only on state transitions.
        private readonly SurfaceReadinessGate surfaceReadinessGate = new SurfaceReadinessGate();

        // GPT watermark: R13-3 page-local dispatch seam. Removing a rebuilt
        // page must preserve every other live native surface.
        internal static bool RemoveDispatchedSurfaceForPage<T>(
            IDictionary<byte, T> surfaces, byte page)
        {
            if (surfaces == null || surfaces.Count == 0) return false;
            return surfaces.Remove(page);
        }

        internal ContainerSessionTracker Tracker { get { return tracker; } }
        internal bool Enabled { get { return enabled; } }
        internal string GateDiagnostics { get { return gateDiagnostics; } }
        internal bool HooksInstalled { get { return hooksInstalled; } }
        internal bool Isolated { get { return isolated; } }

        internal InventorySurfaceLifecycleAdapter(BepInEx.Logging.ManualLogSource log,
            Action<IClientUiInventorySurface> openDispatcher, Action closeDispatcher,
            Func<bool> isolateDispatcher = null, Action hideDispatcher = null,
            Action<byte> discardPageDispatcher = null)
        {
            this.log = log;
            this.openDispatcher = openDispatcher ?? throw new ArgumentNullException(nameof(openDispatcher));
            this.closeDispatcher = closeDispatcher ?? throw new ArgumentNullException(nameof(closeDispatcher));
            this.isolateDispatcher = isolateDispatcher;
            this.hideDispatcher = hideDispatcher;
            this.discardPageDispatcher = discardPageDispatcher;
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
            var hierarchyProbeComplete = UnturnedInventorySurfaceContext.NativeScrollField != null &&
                UnturnedInventorySurfaceContext.NativeGridField != null &&
                UnturnedInventorySurfaceContext.NativeItemsPanelField != null &&
                UnturnedInventorySurfaceContext.DashboardItemsField != null;
            enabled = result.Enabled && hierarchyProbeComplete;
            gateDiagnostics = hierarchyProbeComplete ? result.Diagnostics
                : "featureId=io.github.yu80rice.bue.better-item-interaction"
                    + " errorCode=NativeHierarchyIncompatible diagnosticId=BUE-INVENTORY-003"
                    + " reason=reflection-members-missing native inventory preserved";
        }

        internal void Activate()
        {
            if (!enabled)
            {
                IsolateAndDispatch();
                return;
            }
            if (hooksInstalled || isolated) return;
            try
            {
                var target = AccessTools.Method(typeof(PlayerUI), "Update");
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(InventorySurfaceLifecycleAdapter), nameof(PlayerUIUpdatePostfix)));
                hooksInstalled = true;
                ActiveAdapter = this;
                // DEV-16G slice B: bind the one-shot failure sink to this
                // adapter's BepInEx log source so lifecycle isolations emit a
                // "reason:" line exactly once.
                DiagnosticLogSink = line => BueRuntimeLog.ErrorFriendly("[BUE-INVENTORY] event=diagnostic-failure " + line + " diagnosticId=BUE-INVENTORY-003");
                // DEV-16G ticket D: polling-hook-installed is a per-subsystem
                // load one-shot, demoted to Debug; the aggregate ready line is
                // the only load-stage Info announcement.
                BueRuntimeLog.Runtime("[BUE-INVENTORY] event=polling-hook-installed target=PlayerUI.Update diagnosticId=BUE-INVENTORY-001");
            }
            catch (Exception error)
            {
                gateDiagnostics = "polling-hook-failed: " + error.GetType().FullName + ": " + error.Message;
                hooksInstalled = false;
                IsolateAndDispatch();
            }
        }


        internal static InventorySurfaceLifecycleAdapter ActiveAdapter { get; private set; }

        internal static void ClearActive(InventorySurfaceLifecycleAdapter adapter)
        {
            if (ReferenceEquals(ActiveAdapter, adapter)) ActiveAdapter = null;
        }

        internal static bool RequiresSurfaceRebind(object dispatchedSurface, object currentSurface)
        {
            return !ReferenceEquals(dispatchedSurface, currentSurface);
        }

        internal static bool ShouldDiscardSurface(bool surfaceDispatched, bool surfaceReady)
        {
            return surfaceDispatched && !surfaceReady;
        }

        internal static bool ShouldIsolateOnHookFailure(bool hookInstalled)
        {
            return !hookInstalled;
        }

        internal static bool ShouldIsolateOnHierarchyProbeFailure(bool probeComplete)
        {
            return !probeComplete;
        }

        internal static bool ShouldIsolateOnHierarchyProbeFailure(UnturnedInventorySurfaceContext.NativeHierarchyState state)
        {
            return state == UnturnedInventorySurfaceContext.NativeHierarchyState.Incompatible;
        }

        private bool IsolateAndDispatch()
        {
            var success = IsolateAndDetach();
            try
            {
                if (isolateDispatcher != null && !isolateDispatcher()) success = false;
            }
            catch (Exception error)
            {
                success = false;
                InventoryDragPreviewAdapter.ReportCleanupFailure("inventory-isolate-dispatch", error);
            }
            if (!success && string.IsNullOrEmpty(InventoryDragPreviewAdapter.LastCleanupDiagnostics))
                InventoryDragPreviewAdapter.ReportCleanupIncomplete("inventory-isolate-dispatch");
            return success;
        }

        internal bool IsolateAndDetach()
        {
            if (isolated) return isolationSucceeded;
            isolated = true;
            isolationSucceeded = true;
            try { harmony.UnpatchSelf(); }
            catch (Exception error)
            {
                isolationSucceeded = false;
                LastPollDiagnostics = "inventory-hook-unpatch-failed: " + error.GetType().FullName + ": " + error.Message;
                InventoryDragPreviewAdapter.ReportCleanupFailure("inventory-hook-unpatch", error);
            }
            hooksInstalled = false;
            ClearActive(this);
            return isolationSucceeded;
        }

        internal static string DescribeAdapterGate(bool hasActiveAdapter, bool isolated)
        {
            if (!hasActiveAdapter) return "adapter-gate reason=adapter-null";
            if (isolated) return "adapter-gate reason=adapter-isolated";
            return "adapter-gate reason=live";
        }

        internal static void PlayerUIUpdatePostfix()
        {
            // GPT watermark: route the Harmony callback to the guarded poll so
            // a dead/isolated adapter is distinguishable from a missing patch.
            var adapter = ActiveAdapter;
            if (adapter == null || adapter.isolated)
            {
                return;
            }
            adapter.RunGuardedPoll();
        }

        private void RunGuardedPoll()
        {
            if (!InvokePollGuarded(guardedPoll, IsolateAndDispatch, CloseAfterPollFailure))
            {
                dispatchedSurfaces.Clear();
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
            return InvokePollGuarded(poll, isolate == null ? null : new Func<bool>(() =>
            {
                isolate();
                return true;
            }), hide);
        }

        internal static bool InvokePollGuarded(Action poll, Func<bool> isolate, Action hide)
        {
            try { poll?.Invoke(); return true; }
            catch (Exception error)
            {
                LastPollDiagnostics = "poll failed: " + error.GetType().FullName + ": " + error.Message;
                var cleanupSucceeded = InventoryDragPreviewAdapter.FailClosedPreviewResult(isolate, hide);
                if (!cleanupSucceeded && !string.IsNullOrEmpty(InventoryDragPreviewAdapter.LastCleanupDiagnostics))
                {
                    // Preserve the canonical FeatureId/ErrorCode/DiagnosticId
                    // chain at the poll boundary; callers must not lose a
                    // CleanupIncomplete result just because the hide path also
                    // threw.
                    LastPollDiagnostics += " cleanup=" + InventoryDragPreviewAdapter.LastCleanupDiagnostics;
                }
                return false;
            }
        }

        // Test seam for validating native viewport/scroll rejection before the
        // guarded poll consumes the geometry.
        internal static bool TryComputeScrollPixels(float normalizedPosition, float viewportRatio,
            float contentPixels, out float pixels)
        {
            try
            {
                pixels = UnturnedInventorySurfaceContext.ComputeScrollPixels(normalizedPosition, viewportRatio, contentPixels);
                return true;
            }
            catch (Exception)
            {
                pixels = 0f;
                return false;
            }
        }

        // GPT watermark: R13-R6-silence discriminant seam. The lifecycle Poll has
        // several silent returns that a real-machine session cannot distinguish.
        // Each returns a named reason so the BepInEx log can tell which gate
        // blocked the feature (no active session vs hierarchy-not-ready).
        internal static string DescribeNoActiveSession(bool dashboardActive, bool isStoring,
            bool isStorageTrunk, bool connected, bool hasActiveSession)
        {
            if (!connected) return "no-active-session reason=disconnected";
            if (!dashboardActive && !isStoring) return "no-active-session reason=dashboard-closed";
            if (!hasActiveSession) return "no-active-session reason=tracker-inactive"
                + " dashboardActive=" + dashboardActive + " isStoring=" + isStoring
                + " isStorageTrunk=" + isStorageTrunk;
            return "no-active-session reason=generation-unknown";
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
                if (dispatchedSurfaces.Count > 0)
                {
                    DiscardAllDispatchedSurfaces("session-closed");
                }
                return;
            }

            for (var pageIndex = 0; pageIndex < SupportedSurfacePages.Length; pageIndex++)
            {
                var page = SupportedSurfacePages[pageIndex];
                var liveNativeItems = ReadDashboardSleekItems(page);
                ISleekScrollView liveScroll;
                ISleekElement liveGrid;
                ISleekElement liveItemsPanel;
                var hierarchyState = UnturnedInventorySurfaceContext.ProbeNativeHierarchy(liveNativeItems,
                    out liveScroll, out liveGrid, out liveItemsPanel);
                var liveSurfaceReady = hierarchyState == UnturnedInventorySurfaceContext.NativeHierarchyState.Ready;
                if (ShouldIsolateOnHierarchyProbeFailure(hierarchyState) && (dashboardActive || isStoring))
                {
                    gateDiagnostics = "featureId=io.github.yu80rice.bue.better-item-interaction"
                        + " errorCode=NativeHierarchyIncompatible diagnosticId=BUE-INVENTORY-003"
                        + " reason=live-parent-chain-invalid page=" + page;
                    LastPollDiagnostics = gateDiagnostics;
                    if (dispatchedSurfaces.Count > 0) DiscardAllDispatchedSurfaces("native-hierarchy-incompatible");
                    IsolateAndDispatch();
                    return;
                }
                DispatchedSurfaceState dispatched;
                if (dispatchedSurfaces.TryGetValue(page, out dispatched) &&
                    (!liveSurfaceReady || !IsDispatchedSurfaceCurrent(dispatched, liveNativeItems)))
                {
                    DiscardDispatchedSurfaceForPage(page, liveSurfaceReady ? "native-surface-rebuilt" : "native-hierarchy-unavailable");
                    dispatched = null;
                }
                if (!liveSurfaceReady)
                {
                    continue;
                }
                if (dispatchedSurfaces.ContainsKey(page) && dispatched != null && dispatched.Generation == generation)
                    continue;

                // DEV-16F: only the Storage/trunk page (7) takes the container
                // session kind; every player grid page (Hands/Backpack/Vest/
                // Shirt/Pants = 2..6) is PlayerInventory regardless of storage.
                var kind = page == PlayerInventory.STORAGE
                    ? tracker.Kind : ContainerSessionKind.PlayerInventory;
                string notReadyReason;
                var context = BuildSurfaceContext(kind, page, generation, out notReadyReason);
                if (context == null)
                {
                    // DEV-16G slice A: per-page readiness gate turns the old
                    // per-frame surface-not-ready flood into one-shot
                    // state-transition lines (silent while the state is stable).
                    // DEV-16G ticket D: benign reasons (empty-grid / scroll
                    // not-yet-laid-out) are Debug; only structural faults
                    // (native-hierarchy-incomplete) are loud Error.
                    string gateLine;
                    if (surfaceReadinessGate.Observe(page, false, notReadyReason, out gateLine) && gateLine != null)
                    {
                        if (BueRuntimeLog.IsCriticalNotReadyReason(notReadyReason))
                            BueRuntimeLog.ErrorFriendly(gateLine);
                        else
                            BueRuntimeLog.Runtime(gateLine);
                    }
                    continue;
                }
                string readyLine;
                if (surfaceReadinessGate.Observe(page, true, null, out readyLine) && readyLine != null)
                {
                    BueRuntimeLog.Runtime(readyLine);
                }
                openDispatcher(context);
                RememberDispatchedSurface(page, new DispatchedSurfaceState(generation, context.NativeItems,
                    context.NativeScroll, context.NativeGrid, context.NativeItemsPanel));
                // [DEV-16C] Geometry calibration readout for the real machine:
                // these are the approximate viewport values DEV-16D consumes.
                // DEV-16G ticket D: demoted to Debug (surface open is a normal
                // in-game event, not a load announcement).
                BueRuntimeLog.Runtime("[BUE-INVENTORY] event=surface-context-dispatched kind=" + kind + " page=" + page + " generation=" + generation
                    + " viewportOrigin=" + context.Viewport.OriginX + "," + context.Viewport.OriginY + " (approx)"
                    + " grid=" + context.Viewport.GridWidth + "x" + context.Viewport.GridHeight
                    + " clip=" + context.Viewport.ClipWidth + "x" + context.Viewport.ClipHeight
                    + " uiScale=" + context.UiScale.ToString("0.##") + " cellPx=" + context.CellPixelSize + " (static)"
                    + " scroll=" + context.ScrollPixelsX.ToString("0.###") + "," + context.ScrollPixelsY.ToString("0.###")
                    + " nativeScrollY=" + context.NativeScrollPixelsY.ToString("0.###")
                    + " diagnosticId=BUE-INVENTORY-001");
            }
        }

        // GPT watermark: page-local lifecycle seam. Poll and the tests use the
        // same operation so a rebuilt non-current source page exercises the
        // exact callback that detaches the native delegate and clears the BUE
        // component's session/preview state.
        internal void RememberDispatchedSurface(byte page, DispatchedSurfaceState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            dispatchedSurfaces[page] = state;
        }

        internal bool DiscardDispatchedSurfaceForPage(byte page, string reason)
        {
            var wasDispatched = RemoveDispatchedSurfaceForPage(dispatchedSurfaces, page);
            if (!wasDispatched) return false;
            // DEV-16G ticket D: surface-discarded is a normal in-game lifecycle
            // transition, demoted to Debug (silent in normal play).
            BueRuntimeLog.Runtime("[BUE-INVENTORY] event=surface-discarded reason=" + reason + " diagnosticId=BUE-INVENTORY-005");
            if (discardPageDispatcher != null) discardPageDispatcher(page);
            else if (dispatchedSurfaces.Count == 0)
            {
                if (hideDispatcher != null) hideDispatcher();
                else closeDispatcher();
            }
            return true;
        }

        private void DiscardAllDispatchedSurfaces(string reason)
        {
            var pages = new List<byte>(dispatchedSurfaces.Keys);
            for (var index = 0; index < pages.Count; index++)
                DiscardDispatchedSurfaceForPage(pages[index], reason);
        }

        private static SleekItems ReadDashboardSleekItems(byte page)
        {
            if (UnturnedInventorySurfaceContext.DashboardItemsField == null) return null;
            var dashboardItems = UnturnedInventorySurfaceContext.DashboardItemsField.GetValue(null) as Array;
            var dashboardIndex = page - PlayerInventory.SLOTS;
            if (dashboardItems == null || dashboardIndex < 0 || dashboardIndex >= dashboardItems.Length) return null;
            return dashboardItems.GetValue(dashboardIndex) as SleekItems;
        }

        private bool IsDispatchedSurfaceCurrent(DispatchedSurfaceState dispatched, SleekItems current)
        {
            if (dispatched == null || !ReferenceEquals(dispatched.NativeItems, current)) return false;
            if (current == null) return false;
            var scroll = UnturnedInventorySurfaceContext.NativeScrollField == null ? null : UnturnedInventorySurfaceContext.NativeScrollField.GetValue(current) as ISleekScrollView;
            var grid = UnturnedInventorySurfaceContext.NativeGridField == null ? null : UnturnedInventorySurfaceContext.NativeGridField.GetValue(current) as ISleekElement;
            var panel = UnturnedInventorySurfaceContext.NativeItemsPanelField == null ? null : UnturnedInventorySurfaceContext.NativeItemsPanelField.GetValue(current) as ISleekElement;
            if (!ReferenceEquals(scroll, dispatched.NativeScroll) || !ReferenceEquals(grid, dispatched.NativeGrid) ||
                !ReferenceEquals(panel, dispatched.NativeItemsPanel)) return false;
            return UnturnedInventorySurfaceContext.IsNativeHierarchyConsistent(current, scroll, grid, panel,
                scroll?.Parent, grid?.Parent, panel?.Parent);
        }

        internal UnturnedInventorySurfaceContext BuildSurfaceContext(ContainerSessionKind kind, byte page, uint generation,
            out string notReadyReason)
        {
            notReadyReason = null;
            var player = Player.LocalPlayer;
            if (player == null) { notReadyReason = "no-player"; return null; }
            var playerInventory = player.inventory;
            if (playerInventory == null || playerInventory.items == null) { notReadyReason = "no-inventory"; return null; }
            if (page >= PlayerInventory.PAGES || playerInventory.items[page] == null) { notReadyReason = "no-items"; return null; }

            if (UnturnedInventorySurfaceContext.DashboardItemsField == null) { notReadyReason = "no-dashboard-field"; return null; }
            var dashboardItems = UnturnedInventorySurfaceContext.DashboardItemsField.GetValue(null) as Array;
            var dashboardIndex = page - PlayerInventory.SLOTS;
            if (dashboardItems == null || dashboardIndex < 0 || dashboardIndex >= dashboardItems.Length) { notReadyReason = "no-dashboard-items"; return null; }

            var sleekItems = dashboardItems.GetValue(dashboardIndex) as SleekItems;
            if (sleekItems == null) { notReadyReason = "no-sleek-items"; return null; }

            var dataItems = playerInventory.items[page];
            // A dashboard page can exist as a SleekItems object before its
            // native inventory has been populated (Storage is height=0 until
            // a container opens). It is not a live target surface yet.
            if (dataItems.width == 0 || dataItems.height == 0) { notReadyReason = "empty-grid"; return null; }

            var nativeScroll = UnturnedInventorySurfaceContext.NativeScrollField == null ? null : UnturnedInventorySurfaceContext.NativeScrollField.GetValue(sleekItems) as ISleekScrollView;
            var nativeGrid = UnturnedInventorySurfaceContext.NativeGridField == null ? null : UnturnedInventorySurfaceContext.NativeGridField.GetValue(sleekItems) as ISleekElement;
            var nativePanel = UnturnedInventorySurfaceContext.NativeItemsPanelField == null ? null : UnturnedInventorySurfaceContext.NativeItemsPanelField.GetValue(sleekItems) as ISleekElement;
            var hierarchyLive = UnturnedInventorySurfaceContext.IsNativeHierarchyComplete(nativeScroll != null, nativeGrid != null, nativePanel != null) &&
                UnturnedInventorySurfaceContext.IsNativeHierarchyConsistent(sleekItems, nativeScroll, nativeGrid, nativePanel,
                    nativeScroll?.Parent, nativeGrid?.Parent, nativePanel?.Parent);
            if (!hierarchyLive)
            {
                notReadyReason = "native-hierarchy-incomplete";
                return null;
            }
            if (PlayerUI.container == null) { notReadyReason = "no-player-ui"; return null; }
            var topLevel = new UnturnedVisualContainer(PlayerUI.container);
            if (nativePanel == null || nativeGrid == null || nativeScroll == null)
                throw new InvalidOperationException("native inventory hierarchy disappeared during surface build");
            var gridPanel = new UnturnedVisualContainer(nativePanel);

            // Geometry is expressed in the live SleekItems local space. The
            // previous implementation copied PositionOffset into a screen
            // origin, which mixed parent and child coordinate systems and
            // yielded OutsideGrid for every real pointer.
            InventoryGridViewport viewport;
            var uiScale = UnturnedInventorySurfaceContext.NormalizeUiScale(GraphicsSettings.userInterfaceScale);

            var scrollSize = nativeScroll.GetAbsoluteSize();
            if (!UnturnedInventorySurfaceContext.IsValidScrollViewportSize(scrollSize))
            {
                // GPT watermark: R13-R6-scrollsize fix. On the first frames after
                // the dashboard opens the native scroll view is not laid out yet,
                // so its absolute size is 0/NaN. This is a transient not-ready
                // condition, not a fatal error: return null so Poll() retries next
                // frame (existing not-ready path) instead of throwing and
                // isolating the whole feature.
                notReadyReason = "scroll-viewport-not-laid-out";
                return null;
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
