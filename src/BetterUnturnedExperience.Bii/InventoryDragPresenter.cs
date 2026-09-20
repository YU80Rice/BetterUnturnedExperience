using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Bii
{
    internal sealed class InventoryDragPresenter
    {
        private readonly IPlacementCandidateEvaluator evaluator;
        private uint activeDragGeneration;
        private bool dragging;

        internal InventoryDragPresenter(IPlacementCandidateEvaluator evaluator)
        {
            this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        internal void BeginDrag(uint dragGeneration)
        {
            activeDragGeneration = dragGeneration;
            dragging = true;
        }

        internal void EndDrag()
        {
            dragging = false;
        }

        internal ItemPlacementPreview Evaluate(PlacementCandidateInput input)
        {
            if (!dragging || input.DragGeneration != activeDragGeneration)
            {
                return new ItemPlacementPreview(input.DragGeneration, PlacementPreviewState.Hidden, input.Source, 0, 0, PlacementReason.StaleDrag);
            }
            return evaluator.Evaluate(input);
        }
    }
}
