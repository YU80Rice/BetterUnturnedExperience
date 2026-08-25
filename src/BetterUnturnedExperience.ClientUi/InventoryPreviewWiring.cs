using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal readonly struct InventoryGridViewport
    {
        public float OriginX { get; }
        public float OriginY { get; }
        public byte GridWidth { get; }
        public byte GridHeight { get; }
        public float ClipX { get; }
        public float ClipY { get; }
        public float ClipWidth { get; }
        public float ClipHeight { get; }

        internal InventoryGridViewport(float originX, float originY, byte gridWidth, byte gridHeight,
            float clipX, float clipY, float clipWidth, float clipHeight)
        {
            OriginX = originX;
            OriginY = originY;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            ClipX = clipX;
            ClipY = clipY;
            ClipWidth = clipWidth;
            ClipHeight = clipHeight;
        }

        internal bool Contains(float screenX, float screenY)
        {
            return screenX >= ClipX && screenY >= ClipY && screenX < ClipX + ClipWidth && screenY < ClipY + ClipHeight;
        }
    }

    internal readonly struct ItemAssetIdentity : IEquatable<ItemAssetIdentity>
    {
        public ushort ItemId { get; }
        public Guid AssetGuid { get; }
        public string ResourcePath { get; }

        public ItemAssetIdentity(ushort itemId, Guid assetGuid, string resourcePath)
        {
            ItemId = itemId;
            AssetGuid = assetGuid;
            ResourcePath = resourcePath ?? string.Empty;
        }

        public static ItemAssetIdentity FromItemId(ushort itemId)
        {
            return new ItemAssetIdentity(itemId, Guid.Empty, string.Empty);
        }

        public static ItemAssetIdentity FromAsset(ushort itemId, Guid assetGuid, string resourcePath)
        {
            return new ItemAssetIdentity(itemId, assetGuid, resourcePath);
        }

        public bool Equals(ItemAssetIdentity other)
        {
            return ItemId == other.ItemId && AssetGuid.Equals(other.AssetGuid) && string.Equals(ResourcePath, other.ResourcePath, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ItemAssetIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ItemId.GetHashCode();
                hashCode = (hashCode * 397) ^ AssetGuid.GetHashCode();
                hashCode = (hashCode * 397) ^ (ResourcePath != null ? ResourcePath.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ItemAssetIdentity left, ItemAssetIdentity right) => left.Equals(right);
        public static bool operator !=(ItemAssetIdentity left, ItemAssetIdentity right) => !left.Equals(right);
    }

    internal readonly struct InventoryPreviewInput
    {
        public uint DragGeneration { get; }
        public ItemGridPosition Source { get; }
        public ContainerReference TargetContainer { get; }
        public float PointerScreenX { get; }
        public float PointerScreenY { get; }
        public InventoryGridViewport Viewport { get; }
        public float CellPixelSize { get; }
        public float UiScale { get; }
        public float ScrollPixelsX { get; }
        public float ScrollPixelsY { get; }
        public byte ItemWidth { get; }
        public byte ItemHeight { get; }
        public byte CurrentRotation { get; }
        public bool AllowAutomaticRotation { get; }
        public float GrabOffsetX { get; }
        public float GrabOffsetY { get; }
        public ItemAssetIdentity ItemAsset { get; }
        public IGridOccupancyView Occupancy { get; }

        internal InventoryPreviewInput(uint dragGeneration, ItemGridPosition source, ContainerReference targetContainer,
            float pointerScreenX, float pointerScreenY, InventoryGridViewport viewport, float cellPixelSize, float uiScale,
            float scrollPixelsX, float scrollPixelsY, byte itemWidth, byte itemHeight, byte currentRotation,
            bool allowAutomaticRotation, float grabOffsetX, float grabOffsetY, IGridOccupancyView occupancy)
            : this(dragGeneration, source, targetContainer, pointerScreenX, pointerScreenY, viewport, cellPixelSize, uiScale,
                scrollPixelsX, scrollPixelsY, itemWidth, itemHeight, currentRotation, allowAutomaticRotation,
                grabOffsetX, grabOffsetY, default(ItemAssetIdentity), occupancy)
        {
        }

        internal InventoryPreviewInput(uint dragGeneration, ItemGridPosition source, ContainerReference targetContainer,
            float pointerScreenX, float pointerScreenY, InventoryGridViewport viewport, float cellPixelSize, float uiScale,
            float scrollPixelsX, float scrollPixelsY, byte itemWidth, byte itemHeight, byte currentRotation,
            bool allowAutomaticRotation, float grabOffsetX, float grabOffsetY, ItemAssetIdentity itemAsset,
            IGridOccupancyView occupancy)
        {
            DragGeneration = dragGeneration;
            Source = source;
            TargetContainer = targetContainer;
            PointerScreenX = pointerScreenX;
            PointerScreenY = pointerScreenY;
            Viewport = viewport;
            CellPixelSize = cellPixelSize;
            UiScale = uiScale;
            ScrollPixelsX = scrollPixelsX;
            ScrollPixelsY = scrollPixelsY;
            ItemWidth = itemWidth;
            ItemHeight = itemHeight;
            CurrentRotation = currentRotation;
            AllowAutomaticRotation = allowAutomaticRotation;
            GrabOffsetX = grabOffsetX;
            GrabOffsetY = grabOffsetY;
            ItemAsset = itemAsset;
            Occupancy = occupancy;
        }
    }

    internal static class InventoryGridCoordinateAdapter
    {
        internal static bool TryCreateCandidateInput(InventoryPreviewInput input, out PlacementCandidateInput candidate)
        {
            candidate = default(PlacementCandidateInput);
            if (!IsFinite(input.PointerScreenX) || !IsFinite(input.PointerScreenY) || !IsFinite(input.CellPixelSize) ||
                !IsFinite(input.UiScale) || !IsFinite(input.ScrollPixelsX) || !IsFinite(input.ScrollPixelsY) ||
                input.CellPixelSize <= 0f || input.UiScale <= 0f || input.Occupancy == null ||
                !input.Viewport.Contains(input.PointerScreenX, input.PointerScreenY))
            {
                return false;
            }

            var rotation = (byte)(input.CurrentRotation & 3);
            float grabX;
            float grabY;
            byte width;
            byte height;
            if (!TryRotateGrabOffset(input.ItemWidth, input.ItemHeight, input.GrabOffsetX, input.GrabOffsetY, rotation,
                out width, out height, out grabX, out grabY))
            {
                return false;
            }

            var scaledCellSize = input.CellPixelSize * input.UiScale;
            if (!IsFinite(scaledCellSize) || scaledCellSize <= 0f)
            {
                return false;
            }

            var pointerGridX = (input.PointerScreenX - input.Viewport.OriginX + input.ScrollPixelsX) / scaledCellSize;
            var pointerGridY = (input.PointerScreenY - input.Viewport.OriginY + input.ScrollPixelsY) / scaledCellSize;
            var intendedCenterX = pointerGridX + width / 2f - grabX;
            var intendedCenterY = pointerGridY + height / 2f - grabY;
            if (!IsFinite(intendedCenterX) || !IsFinite(intendedCenterY))
            {
                return false;
            }

            candidate = new PlacementCandidateInput(input.DragGeneration, input.Source, input.TargetContainer,
                intendedCenterX, intendedCenterY, input.ItemWidth, input.ItemHeight, rotation,
                input.AllowAutomaticRotation, input.Occupancy);
            return true;
        }

        internal static bool TryGetIconScreenPosition(InventoryPreviewInput input, byte targetRotation,
            out float screenX, out float screenY)
        {
            screenX = input.PointerScreenX;
            screenY = input.PointerScreenY;
            byte width;
            byte height;
            float grabX;
            float grabY;
            if (!IsFinite(input.PointerScreenX) || !IsFinite(input.PointerScreenY) || !IsFinite(input.CellPixelSize) ||
                !IsFinite(input.UiScale) || input.CellPixelSize <= 0f || input.UiScale <= 0f ||
                !TryRotateGrabOffset(input.ItemWidth, input.ItemHeight, input.GrabOffsetX, input.GrabOffsetY,
                    (byte)(targetRotation & 3), out width, out height, out grabX, out grabY))
            {
                return false;
            }

            var scaledCellSize = input.CellPixelSize * input.UiScale;
            if (!IsFinite(scaledCellSize) || scaledCellSize <= 0f)
            {
                return false;
            }

            screenX += (width / 2f - grabX) * scaledCellSize;
            screenY += (height / 2f - grabY) * scaledCellSize;
            return IsFinite(screenX) && IsFinite(screenY);
        }

        private static bool TryRotateGrabOffset(byte baseWidth, byte baseHeight, float baseGrabX, float baseGrabY,
            byte rotation, out byte width, out byte height, out float grabX, out float grabY)
        {
            width = baseWidth;
            height = baseHeight;
            grabX = baseGrabX;
            grabY = baseGrabY;
            if (baseWidth == 0 || baseHeight == 0 || !IsFinite(baseGrabX) || !IsFinite(baseGrabY) ||
                baseGrabX < 0f || baseGrabY < 0f || baseGrabX > baseWidth || baseGrabY > baseHeight)
            {
                return false;
            }

            for (var step = 0; step < rotation; step++)
            {
                var nextX = height - grabY;
                var nextY = grabX;
                grabX = nextX;
                grabY = nextY;
                var nextWidth = height;
                height = width;
                width = nextWidth;
            }

            return grabX >= 0f && grabY >= 0f && grabX <= width && grabY <= height;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    internal enum PreviewFrameKind : byte
    {
        ValidGreen,
        InvalidRed
    }

    internal readonly struct PreviewFrame
    {
        public PreviewFrameKind Kind { get; }
        public ItemGridPosition Candidate { get; }
        public byte Rotation { get { return Candidate.Rotation; } }
        public byte Width { get; }
        public byte Height { get; }
        public float CellPixelSize { get; }
        public PlacementReason Reason { get; }

        internal PreviewFrame(PreviewFrameKind kind, ItemGridPosition candidate, byte width, byte height,
            float cellPixelSize, PlacementReason reason)
        {
            Kind = kind;
            Candidate = candidate;
            Width = width;
            Height = height;
            CellPixelSize = cellPixelSize;
            Reason = reason;
        }
    }

    internal readonly struct PreviewIcon
    {
        public float ScreenX { get; }
        public float ScreenY { get; }
        public byte Rotation { get; }
        public ItemAssetIdentity Asset { get; }

        internal PreviewIcon(float screenX, float screenY, byte rotation, ItemAssetIdentity asset)
        {
            ScreenX = screenX;
            ScreenY = screenY;
            Rotation = rotation;
            Asset = asset;
        }

        internal PreviewIcon(float screenX, float screenY, byte rotation)
            : this(screenX, screenY, rotation, default(ItemAssetIdentity))
        {
        }
    }

    internal interface IInventoryPreviewSink
    {
        void ShowFrame(PreviewFrame frame);
        void ShowIcon(PreviewIcon icon);
        void HideIcon();
        void Hide();
    }

    internal sealed class InventoryPreviewPresenter
    {
        private readonly InventoryDragPresenter dragPresenter;

        internal InventoryPreviewPresenter(InventoryDragPresenter dragPresenter)
        {
            this.dragPresenter = dragPresenter ?? throw new ArgumentNullException(nameof(dragPresenter));
        }

        internal void BeginDrag(uint dragGeneration) { dragPresenter.BeginDrag(dragGeneration); }
        internal void EndDrag() { dragPresenter.EndDrag(); }

        internal void Update(InventoryPreviewInput input, IInventoryPreviewSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            PlacementCandidateInput candidateInput;
            if (!InventoryGridCoordinateAdapter.TryCreateCandidateInput(input, out candidateInput))
            {
                sink.Hide();
                return;
            }

            var preview = dragPresenter.Evaluate(candidateInput);
            if (preview.DragGeneration != input.DragGeneration || preview.State == PlacementPreviewState.Hidden ||
                preview.State == PlacementPreviewState.PendingAuthoritativeProjection)
            {
                sink.Hide();
                return;
            }

            var frameKind = preview.State == PlacementPreviewState.Candidate ? PreviewFrameKind.ValidGreen : PreviewFrameKind.InvalidRed;
            var cellPixelSize = input.CellPixelSize * input.UiScale;
            if (!IsFinite(cellPixelSize) || cellPixelSize <= 0f)
            {
                sink.Hide();
                return;
            }

            sink.ShowFrame(new PreviewFrame(frameKind, preview.Candidate, preview.Width, preview.Height,
                cellPixelSize, preview.Reason));
            if (preview.State == PlacementPreviewState.Candidate)
            {
                float iconX;
                float iconY;
                if (InventoryGridCoordinateAdapter.TryGetIconScreenPosition(input, preview.Candidate.Rotation, out iconX, out iconY))
                {
                    sink.ShowIcon(new PreviewIcon(iconX, iconY, preview.Candidate.Rotation, input.ItemAsset));
                }
                else
                {
                    sink.HideIcon();
                }
            }
            else
            {
                sink.HideIcon();
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
