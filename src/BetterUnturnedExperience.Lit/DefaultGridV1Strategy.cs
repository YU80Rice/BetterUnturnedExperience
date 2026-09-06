using System;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V2-15: the one built-in tidy adapter. It wraps the migrated
    /// InventorySolver so the plan output is byte-for-byte the old plugin's
    /// behavior (排序与放置都不改 — O-LIT-1 勘误口径), while the seam makes
    /// the strategy replaceable. The plan carries the solver's bool (every
    /// valid item placed) as <see cref="TidyPlan.AllPlaced"/>.
    /// </summary>
    internal sealed class DefaultGridV1Strategy : ITidyStrategy
    {
        public string StrategyId
        {
            get { return "default-grid-v1"; }
        }

        public TidyPlan BuildPlan(TidyInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            bool allPlaced = InventorySolver.TryPack(
                input.Width, input.Height, input.Items,
                out var placements, input.SortDescending, input.Mode);
            return new TidyPlan(StrategyId, placements, allPlaced);
        }
    }
}
