using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal interface INativeInventoryDragActions
    {
        void StopDrag();
        void SendDragItem(ItemGridPosition source, ItemGridPosition target);
    }

    internal readonly struct NativeDragAdapterInput
    {
        public bool IsDragging { get; }
        public uint DragGeneration { get; }
        public ItemGridPosition Source { get; }
        public ItemPlacementPreview Preview { get; }

        internal NativeDragAdapterInput(bool isDragging, uint dragGeneration, ItemGridPosition source, ItemPlacementPreview preview)
        {
            IsDragging = isDragging;
            DragGeneration = dragGeneration;
            Source = source;
            Preview = preview;
        }
    }

    internal enum NativeDragAdapterOutcome : byte
    {
        PassThrough,
        Submitted,
        Cancelled
    }

    internal sealed class NativeInventoryInteractionAdapter
    {
        private readonly byte slotsPageBoundary;
        private readonly byte areaPage;

        internal NativeInventoryInteractionAdapter(byte slotsPageBoundary, byte areaPage)
        {
            this.slotsPageBoundary = slotsPageBoundary;
            this.areaPage = areaPage;
        }

        internal NativeDragAdapterOutcome HandleRelease(NativeDragAdapterInput input, INativeInventoryDragActions native)
        {
            if (native == null) throw new ArgumentNullException(nameof(native));
            if (!input.IsDragging)
            {
                return NativeDragAdapterOutcome.PassThrough;
            }

            var target = input.Preview.Candidate;
            if (!IsOrdinaryGrid(target.Page))
            {
                return NativeDragAdapterOutcome.PassThrough;
            }

            if (input.Preview.DragGeneration != input.DragGeneration)
            {
                native.StopDrag();
                return NativeDragAdapterOutcome.Cancelled;
            }

            if (IsSamePlacement(input.Source, target))
            {
                return NativeDragAdapterOutcome.PassThrough;
            }

            if (input.Preview.State != PlacementPreviewState.Candidate)
            {
                native.StopDrag();
                return NativeDragAdapterOutcome.Cancelled;
            }

            native.StopDrag();
            native.SendDragItem(input.Source, target);
            return NativeDragAdapterOutcome.Submitted;
        }

        private bool IsOrdinaryGrid(byte page)
        {
            return page >= slotsPageBoundary && page != areaPage;
        }

        private static bool IsSamePlacement(ItemGridPosition source, ItemGridPosition target)
        {
            return source.Page == target.Page && source.X == target.X && source.Y == target.Y && source.Rotation == target.Rotation;
        }
    }
}
