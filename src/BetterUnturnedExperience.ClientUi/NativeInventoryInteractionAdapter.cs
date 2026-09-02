using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal interface INativeInventoryDragActions
    {
        void StopDrag();
        void SendDragItem(ItemGridPosition source, ItemGridPosition target);
        void TakeGroundItem(ItemGridPosition target);
    }

    internal readonly struct NativeDragAdapterInput
    {
        public bool IsDragging { get; }
        public uint DragGeneration { get; }
        public ItemGridPosition Source { get; }
        public ItemPlacementPreview Preview { get; }
        public byte CallbackPage { get; }

        internal NativeDragAdapterInput(bool isDragging, uint dragGeneration, ItemGridPosition source, ItemPlacementPreview preview)
            : this(isDragging, dragGeneration, source, preview, preview.Candidate.Page)
        {
        }

        internal NativeDragAdapterInput(bool isDragging, uint dragGeneration, ItemGridPosition source,
            ItemPlacementPreview preview, byte callbackPage)
        {
            IsDragging = isDragging;
            DragGeneration = dragGeneration;
            Source = source;
            Preview = preview;
            CallbackPage = callbackPage;
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

            // Page support is the first gate. Unsupported source/target pages
            // are always native pass-through, even if a stale visible preview
            // is still present from an earlier supported drag.
            if (!IsEnhancedSourcePage(input.Source.Page))
            {
                return NativeDragAdapterOutcome.PassThrough;
            }

            var target = input.Preview.Candidate;
            if (!IsOrdinaryGrid(target.Page))
            {
                return NativeDragAdapterOutcome.PassThrough;
            }

            if (input.CallbackPage != target.Page)
            {
                return NativeDragAdapterOutcome.PassThrough;
            }

            // A hidden/default preview means the enhancement has no current
            // fact source (for example after an occupancy snapshot invalidation).
            // Never cancel or submit in that state; leave the native callback in
            // control so the vanilla interaction remains available.
            if (input.Preview.State == PlacementPreviewState.Hidden)
            {
                return NativeDragAdapterOutcome.PassThrough;
            }

            if (input.Preview.DragGeneration != input.DragGeneration)
            {
                return NativeDragAdapterOutcome.Cancelled;
            }

            if (input.Preview.State != PlacementPreviewState.Candidate)
            {
                // The plugin caller must still be able to hand an occupied
                // target back to vanilla while isDragging remains true so the
                // native swap branch can run. Non-swap callers stop the drag
                // after this outcome is returned.
                return NativeDragAdapterOutcome.Cancelled;
            }

            if (IsSamePlacement(input.Source, target))
            {
                return NativeDragAdapterOutcome.PassThrough;
            }

            // DEV-16F R2: ground pickups (AREA=8 source) must commit through
            // TakeGroundItem, never sendDragItem — the server rejects
            // sendDragItem with page_0 >= PAGES-1 (PlayerInventory.ReceiveDragItem),
            // so the item would vanish. Equipment/grid sources use sendDragItem.
            if (input.Source.Page == areaPage)
            {
                native.TakeGroundItem(target);
                native.StopDrag();
                return NativeDragAdapterOutcome.Submitted;
            }

            native.SendDragItem(input.Source, target);
            native.StopDrag();
            return NativeDragAdapterOutcome.Submitted;
        }

        // DEV-16F: every player grid page is an ordinary enhanced TARGET grid —
        // 2=Hands, 3=Backpack, 4=Vest, 5=Shirt, 6=Pants, 7=Storage/trunk.
        // AREA(8) and equipment slots (< SLOTS) are never enhanced targets.
        private bool IsOrdinaryGrid(byte page)
        {
            return page >= slotsPageBoundary && page < areaPage;
        }

        // DEV-16F R2: source decoupling must cover EVERY pickup origin. A drag
        // that started from the ground (AREA=8), a hotbar equipment slot (0/1),
        // or any player grid (2-7) can reach an enhanced grid target. The
        // source page no longer decides takeover: only the TARGET grid does.
        // On release the source determines the native call: ground pickups use
        // TakeGroundItem (server rejects sendDragItem with page_0 >= PAGES-1),
        // everything else uses sendDragItem.
        internal bool IsEnhancedSourcePage(byte page)
        {
            return page <= areaPage;
        }

        private static bool IsSamePlacement(ItemGridPosition source, ItemGridPosition target)
        {
            return source.Page == target.Page && source.X == target.X && source.Y == target.Y && source.Rotation == target.Rotation;
        }
    }
}
