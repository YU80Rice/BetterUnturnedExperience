using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Placement;

namespace BetterUnturnedExperience.Placement.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try { Run(); Console.WriteLine("DEV-04 placement evaluator tests: PASS"); return 0; }
            catch (Exception error) { Console.WriteLine("DEV-04 placement evaluator tests: FAIL"); Console.WriteLine(error.Message); return 1; }
        }

        private static void Run()
        {
            var empty = new Grid(8, 6);
            var evaluator = new PlacementCandidateEvaluator();
            var currentLocal = evaluator.Evaluate(Input(empty, 2, 3, 3.5f, 2.5f, 0, true));
            Assert(currentLocal.State == PlacementPreviewState.Candidate && currentLocal.Candidate.X == 3 && currentLocal.Candidate.Y == 1 && currentLocal.Candidate.Rotation == 0, "open area keeps current orientation locally");

            var obstacle = new Grid(8, 6);
            obstacle.Fill(2, 2, 2, 3);
            var rotatedLocal = evaluator.Evaluate(Input(obstacle, 2, 3, 3.5f, 1.1f, 0, true));
            Assert(rotatedLocal.State == PlacementPreviewState.Candidate && rotatedLocal.Width == 3 && rotatedLocal.Height == 2 && rotatedLocal.Candidate.Rotation == 1, "blocked local current rotates into local fit");

            var oddRotationGrid = new Grid(5, 5); oddRotationGrid.Fill(1, 2, 1, 1);
            var oddRotation = evaluator.Evaluate(Input(oddRotationGrid, 2, 3, 2.5f, 2.5f, 1, true));
            Assert(oddRotation.State == PlacementPreviewState.Candidate && oddRotation.Width == 2 && oddRotation.Height == 3 && oddRotation.Candidate.Rotation == 2, "automatic rotation swaps dimensions from odd current rotation");

            var noRotate = evaluator.Evaluate(Input(obstacle, 2, 3, 3.5f, 1.1f, 0, false));
            Assert(noRotate.State == PlacementPreviewState.Candidate && noRotate.Width == 2 && noRotate.Height == 3 && noRotate.Candidate.Rotation == 0, "automatic rotation disabled preserves current orientation");

            var outside = evaluator.Evaluate(Input(empty, 2, 3, -0.1f, 2f, 0, true));
            Assert(outside.State == PlacementPreviewState.Hidden && outside.Reason == PlacementReason.OutsideGrid, "center outside container hides preview");

            var full = new Grid(3, 3); full.Fill(0, 0, 3, 3);
            var occupied = evaluator.Evaluate(Input(full, 3, 3, 1.5f, 1.5f, 0, true));
            Assert(occupied.State == PlacementPreviewState.LocallyInvalid && occupied.Reason == PlacementReason.Occupied, "full grid reports occupied");

            var tooLarge = evaluator.Evaluate(Input(empty, 9, 7, 1f, 1f, 0, true));
            Assert(tooLarge.State == PlacementPreviewState.LocallyInvalid && tooLarge.Reason == PlacementReason.OutsideGrid, "oversized item reports outside grid");

            var edge = evaluator.Evaluate(Input(empty, 1, 5, 7.8f, 5.7f, 0, true));
            Assert(edge.State == PlacementPreviewState.Candidate && edge.Candidate.X == 7 && edge.Candidate.Y == 1, "long item clamps to lower right edge");

            var tie = new Grid(4, 3); tie.Fill(1, 0, 1, 1); tie.Fill(0, 1, 1, 1); tie.Fill(1, 1, 1, 1);
            var expanded = evaluator.Evaluate(Input(tie, 1, 1, 1.5f, 1.5f, 0, true));
            Assert(expanded.State == PlacementPreviewState.Candidate && expanded.Candidate.X == 2 && expanded.Candidate.Y == 1, "expanded search uses deterministic distance then Y/X");

            for (var warmup = 0; warmup < 100; warmup++) evaluator.Evaluate(Input(empty, 2, 3, 3.2f, 2.2f, 0, true));
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            AppDomain.MonitoringIsEnabled = true;
            var before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
            for (var sample = 0; sample < 10000; sample++) evaluator.Evaluate(Input(empty, 2, 3, 3.2f, 2.2f, 0, true));
            var allocated = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - before;
            Assert(allocated == 0, "Evaluate hot path allocates zero bytes; actual=" + allocated);
        }

        private static PlacementCandidateInput Input(Grid grid, byte width, byte height, float x, float y, byte rotation, bool autoRotate)
        {
            return new PlacementCandidateInput(1, new ItemGridPosition(0, 0, 0, 0), new ContainerReference(ContainerKind.PlayerInventory, 0, 1), x, y, width, height, rotation, autoRotate, grid);
        }

        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private sealed class Grid : IGridOccupancyView
        {
            private readonly bool[,] cells;
            public Grid(byte width, byte height) { Width = width; Height = height; cells = new bool[width, height]; }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y) { return cells[x, y]; }
            public void Fill(byte x, byte y, byte width, byte height) { for (var yy = y; yy < y + height; yy++) for (var xx = x; xx < x + width; xx++) cells[xx, yy] = true; }
        }
    }
}
