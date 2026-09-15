using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-02 (V5-T3): the ONE built-in tidy adapter — the unified tagged
    /// row-band plan through the ITidyStrategy seam. The legacy mode is still
    /// carried through TidyInput for wire/protocol compatibility but no
    /// longer decides anything (V5-T3 Q2: 旧档位只做存盘兼容与迁移；不再决定
    /// 算法); the stable-direction preference feeds the plan's stable finish.
    /// The strategy id is internal implementation identity — it never reaches
    /// the player UI and never enters the contract (2.1, zero additions).
    /// </summary>
    internal sealed class TaggedRowBandV1Strategy : ITidyStrategy
    {
        public string StrategyId
        {
            get { return TaggedRowBandLayout.LayoutId; }
        }

        public TidyPlan BuildPlan(TidyInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            List<PackableItem> placements;
            string failureReason;
            bool allPlaced = TaggedRowBandLayout.TryPlanLayout(
                input.Width, input.Height, input.SortDescending, input.Items,
                out placements, out failureReason);
            if (placements == null) placements = new List<PackableItem>(0);
            return new TidyPlan(StrategyId, placements, allPlaced);
        }
    }
}
