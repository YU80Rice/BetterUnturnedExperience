using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// The single official V7-01 tidy adapter. The identity is internal: it is
    /// consumed by the five existing tidy entries and never reaches the player
    /// surface or the public contract.
    /// </summary>
    internal sealed class StableLabelCompactStrategy : ITidyStrategy
    {
        public string StrategyId { get { return StableLabelCompactLayout.LayoutId; } }

        public TidyPlan BuildPlan(TidyInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            List<PackableItem> placements;
            string failureReason;
            var allPlaced = StableLabelCompactLayout.TryPlanLayout(
                input.Width, input.Height, input.Items, out placements, out failureReason);
            if (placements == null) placements = new List<PackableItem>(0);
            return new TidyPlan(StrategyId, placements, allPlaced);
        }
    }
}
