using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V2-15 (T4 决策 1): the tidy strategy seam. The algorithm migrates
    /// unchanged, but the plan is now produced through this interface so a
    /// future adapter (or the eventual strategy-governance ticket) can swap
    /// the ordering/placement behavior without touching the transaction
    /// service. The built-in adapter <see cref="DefaultGridV1Strategy"/>
    /// wraps InventorySolver; no strategy picker UI and no third-party
    /// dynamic loading exist in this phase (spec: 不做).
    /// </summary>
    internal interface ITidyStrategy
    {
        /// <summary>Stable identity of the strategy that produced a plan.</summary>
        string StrategyId { get; }

        /// <summary>
        /// Produces the placement plan for one page. Input and output are
        /// pure C# types (InventorySolver itself has zero Unity
        /// dependencies). The plan consumer treats the returned placements
        /// as its own; the producing strategy must not retain the list.
        /// </summary>
        TidyPlan BuildPlan(TidyInput input);
    }

    /// <summary>
    /// Pure input record for one tidy plan: the page geometry, the sort
    /// direction and packing mode (both per-page memory state, explicitly
    /// non-persisted per the spec), and the items to place. <see cref="Items"/>
    /// is the concrete list the solver contract expects; building the plan
    /// resets the solver-output fields (Placed/ResultX/ResultY/ResultRot) on
    /// these items exactly like InventorySolver.TryPack always did.
    /// </summary>
    internal sealed class TidyInput
    {
        public TidyInput(byte width, byte height, bool sortDescending, TidyMode mode, List<PackableItem> items)
        {
            Width = width;
            Height = height;
            SortDescending = sortDescending;
            Mode = mode;
            Items = items;
        }

        public byte Width { get; }
        public byte Height { get; }
        public bool SortDescending { get; }
        public TidyMode Mode { get; }
        public List<PackableItem> Items { get; }
    }

    /// <summary>
    /// Pure plan output: one placement entry per input item (same order,
    /// same Tag), the producing strategy's id, and whether every valid item
    /// found a spot (the old TryPack bool, carried under a name that says
    /// what it means).
    /// </summary>
    internal sealed class TidyPlan
    {
        public TidyPlan(string strategyId, IReadOnlyList<PackableItem> placements, bool allPlaced)
        {
            StrategyId = strategyId ?? throw new ArgumentNullException(nameof(strategyId));
            Placements = placements ?? throw new ArgumentNullException(nameof(placements));
            AllPlaced = allPlaced;
        }

        public string StrategyId { get; }
        public IReadOnlyList<PackableItem> Placements { get; }
        public bool AllPlaced { get; }
    }
}
