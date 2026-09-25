using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// V7-01 pure packing module: stable player-use labels, large items first
    /// within a label, and a deterministic top-left compact fill.
    /// </summary>
    internal static class StableLabelCompactLayout
    {
        internal const string LayoutId = "StableLabelCompact";
        private const long SearchNodeCap = 200000L;

        internal static bool TryPlanLayout(byte width, byte height, List<PackableItem> items,
            out List<PackableItem> plan, out string failureReason)
        {
            failureReason = null;
            var count = items == null ? 0 : items.Count;
            plan = new List<PackableItem>(count);
            for (var i = 0; i < count; i++) plan.Add(Clone(items[i]));
            if (count == 0) return true;
            if (width == 0 || height == 0)
            {
                Reset(plan);
                failureReason = "empty-grid";
                return false;
            }

            var order = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                var item = plan[i];
                if (item == null || item.size_x == 0 || item.size_y == 0)
                {
                    Reset(plan);
                    failureReason = "invalid-item";
                    return false;
                }
                if (!FitsEither(item, width, height))
                {
                    Reset(plan);
                    failureReason = "oversized";
                    return false;
                }
                order.Add(i);
            }

            var sortablePlan = plan;
            order.Sort((left, right) =>
            {
                var result = Compare(sortablePlan[left], sortablePlan[right]);
                return result != 0 ? result : left.CompareTo(right);
            });

            var occupied = new bool[width * height];
            var nodes = SearchNodeCap;
            if (!PlaceNext(plan, order, 0, occupied, width, height, ref nodes))
            {
                Reset(plan);
                failureReason = nodes <= 0 ? "search-budget" : "cannot-fit";
                return false;
            }

            if (!Validate(plan, width, height) || !ValidateLabelConnectivity(plan, width, height))
            {
                Reset(plan);
                failureReason = "internal-validation";
                return false;
            }
            return true;
        }

        private static bool PlaceNext(List<PackableItem> plan, List<int> order, int position,
            bool[] occupied, byte width, byte height, ref long nodes)
        {
            if (position >= order.Count) return true;
            if (nodes-- <= 0) return false;

            var item = plan[order[position]];
            var preferred = ForwardRotation(item.PreferredRotation);
            var alternate = (byte)(preferred ^ 1);
            for (var orientation = 0; orientation < 2; orientation++)
            {
                var rotation = orientation == 0 ? preferred : alternate;
                if (orientation == 1 && item.size_x == item.size_y) continue;
                var itemWidth = FootWidth(item, rotation);
                var itemHeight = FootHeight(item, rotation);
                for (var y = 0; y + itemHeight <= height; y++)
                {
                    for (var x = 0; x + itemWidth <= width; x++)
                    {
                        if (!CanPlace(occupied, width, x, y, itemWidth, itemHeight)) continue;
                        if (!AllowsLabelConnection(plan, order, position, occupied, width, height,
                                x, y, itemWidth, itemHeight)) continue;

                        Occupy(occupied, width, x, y, itemWidth, itemHeight, true);
                        item.ResultX = (byte)x;
                        item.ResultY = (byte)y;
                        item.ResultRot = rotation;
                        item.Placed = true;

                        var validShape = !CreatesEnclosedHole(occupied, width, height)
                            && PlaceNext(plan, order, position + 1, occupied, width, height, ref nodes);
                        if (validShape) return true;

                        Occupy(occupied, width, x, y, itemWidth, itemHeight, false);
                        item.Placed = false;
                        item.ResultX = 0;
                        item.ResultY = 0;
                        item.ResultRot = 0;
                        if (nodes <= 0) return false;
                    }
                }
            }
            return false;
        }

        private static bool AllowsLabelConnection(List<PackableItem> plan, List<int> order,
            int position, bool[] occupied, byte width, byte height, int x, int y, int itemWidth, int itemHeight)
        {
            var item = plan[order[position]];
            if (position == 0) return x == 0 && y == 0;

            var hasSameLabel = false;
            var previousLabel = PlayerUseLabel.RangedWeapon;
            var hasPreviousLabel = false;
            for (var i = 0; i < position; i++)
            {
                var prior = plan[order[i]];
                if (!prior.Placed) continue;
                if (prior.Label == item.Label) hasSameLabel = true;
                if (!hasPreviousLabel || prior.Label > previousLabel)
                {
                    previousLabel = prior.Label;
                    hasPreviousLabel = true;
                }
            }

            var touchesAny = false;
            var touchesSameLabel = false;
            var touchesPreviousLabel = false;
            var touchesOlderLabel = false;
            var touchesOtherLabel = false;
            for (var cx = x; cx < x + itemWidth; cx++)
            {
                for (var cy = y; cy < y + itemHeight; cy++)
                {
                    InspectNeighbor(plan, occupied, width, cx - 1, cy, item.Label, previousLabel,
                        ref touchesAny, ref touchesSameLabel, ref touchesPreviousLabel,
                        ref touchesOlderLabel, ref touchesOtherLabel);
                    InspectNeighbor(plan, occupied, width, cx + 1, cy, item.Label, previousLabel,
                        ref touchesAny, ref touchesSameLabel, ref touchesPreviousLabel,
                        ref touchesOlderLabel, ref touchesOtherLabel);
                    InspectNeighbor(plan, occupied, width, cx, cy - 1, item.Label, previousLabel,
                        ref touchesAny, ref touchesSameLabel, ref touchesPreviousLabel,
                        ref touchesOlderLabel, ref touchesOtherLabel);
                    InspectNeighbor(plan, occupied, width, cx, cy + 1, item.Label, previousLabel,
                        ref touchesAny, ref touchesSameLabel, ref touchesPreviousLabel,
                        ref touchesOlderLabel, ref touchesOtherLabel);
                }
            }

            if (!hasSameLabel)
            {
                // A new label may continue the immediately preceding label's
                // outer contour, or start in an empty region that is still open
                // to the grid boundary. Touching an older label would reopen a
                // frozen region and is rejected.
                var openContour = IsBoundaryConnectedEmpty(occupied, width, height,
                    x, y, itemWidth, itemHeight);
                return hasPreviousLabel && touchesPreviousLabel && !touchesOtherLabel;
            }

            // A label normally joins its own region. At an open grid edge it
            // may also complete the contour beside an older frozen label; that
            // is filling the exposed boundary, not reopening an enclosed area.
            var openContinuation = IsBoundaryConnectedEmpty(occupied, width, height,
                x, y, itemWidth, itemHeight);
            return (touchesSameLabel || (openContinuation && touchesPreviousLabel))
                && (!touchesOlderLabel || openContinuation);
        }

        private static bool IsBoundaryConnectedEmpty(bool[] occupied, byte width, byte height,
            int x, int y, int itemWidth, int itemHeight)
        {
            var reachable = new bool[occupied.Length];
            var queue = new Queue<int>();
            for (var edgeX = 0; edgeX < width; edgeX++)
            {
                EnqueueEmpty(edgeX, 0, occupied, reachable, queue, width, height);
                EnqueueEmpty(edgeX, height - 1, occupied, reachable, queue, width, height);
            }
            for (var edgeY = 0; edgeY < height; edgeY++)
            {
                EnqueueEmpty(0, edgeY, occupied, reachable, queue, width, height);
                EnqueueEmpty(width - 1, edgeY, occupied, reachable, queue, width, height);
            }
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var cellX = index % width;
                var cellY = index / width;
                EnqueueEmpty(cellX - 1, cellY, occupied, reachable, queue, width, height);
                EnqueueEmpty(cellX + 1, cellY, occupied, reachable, queue, width, height);
                EnqueueEmpty(cellX, cellY - 1, occupied, reachable, queue, width, height);
                EnqueueEmpty(cellX, cellY + 1, occupied, reachable, queue, width, height);
            }
            for (var cellX = x; cellX < x + itemWidth; cellX++)
                for (var cellY = y; cellY < y + itemHeight; cellY++)
                    if (!reachable[cellY * width + cellX]) return false;
            return true;
        }

        private static void InspectNeighbor(List<PackableItem> plan, bool[] occupied, byte width,
            int x, int y, PlayerUseLabel currentLabel, PlayerUseLabel previousLabel,
            ref bool touchesAny, ref bool touchesSameLabel, ref bool touchesPreviousLabel,
            ref bool touchesOlderLabel, ref bool touchesOtherLabel)
        {
            if (x < 0 || y < 0 || x >= width) return;
            var height = occupied.Length / width;
            if (y >= height || !occupied[y * width + x]) return;
            var prior = FindPlacedAt(plan, x, y, width);
            if (prior == null) return;
            touchesAny = true;
            if (prior.Label == currentLabel) touchesSameLabel = true;
            if (prior.Label == previousLabel) touchesPreviousLabel = true;
            if (prior.Label < currentLabel) touchesOlderLabel = true;
            if (prior.Label != currentLabel && prior.Label != previousLabel) touchesOtherLabel = true;
        }

        private static bool ValidateLabelConnectivity(List<PackableItem> plan, byte width, byte height)
        {
            var labels = new HashSet<PlayerUseLabel>();
            for (var i = 0; i < plan.Count; i++) if (plan[i] != null && plan[i].Placed) labels.Add(plan[i].Label);
            foreach (var label in labels)
            {
                var cells = new List<int>();
                for (var i = 0; i < plan.Count; i++)
                {
                    var item = plan[i];
                    if (item == null || !item.Placed || item.Label != label) continue;
                    var itemWidth = FootWidth(item, item.ResultRot);
                    var itemHeight = FootHeight(item, item.ResultRot);
                    for (var x = item.ResultX; x < item.ResultX + itemWidth; x++)
                        for (var y = item.ResultY; y < item.ResultY + itemHeight; y++) cells.Add(y * width + x);
                }
                if (cells.Count == 0) continue;
                var seen = new HashSet<int>();
                var queue = new Queue<int>();
                queue.Enqueue(cells[0]);
                seen.Add(cells[0]);
                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    var x = index % width;
                    var y = index / width;
                    var neighbors = new[] { index - 1, index + 1, index - width, index + width };
                    for (var n = 0; n < neighbors.Length; n++)
                    {
                        var next = neighbors[n];
                        if (next < 0 || next >= width * height || seen.Contains(next) || !cells.Contains(next)) continue;
                        if (Math.Abs((next % width) - x) + Math.Abs((next / width) - y) != 1) continue;
                        seen.Add(next);
                        queue.Enqueue(next);
                    }
                }
                if (seen.Count != cells.Count) return false;
            }
            return true;
        }

        private static PackableItem FindPlacedAt(List<PackableItem> plan, int x, int y, byte width)
        {
            for (var i = 0; i < plan.Count; i++)
            {
                var item = plan[i];
                if (item == null || !item.Placed) continue;
                var itemWidth = FootWidth(item, item.ResultRot);
                var itemHeight = FootHeight(item, item.ResultRot);
                if (x >= item.ResultX && x < item.ResultX + itemWidth
                    && y >= item.ResultY && y < item.ResultY + itemHeight) return item;
            }
            return null;
        }

        private static int Compare(PackableItem left, PackableItem right)
        {
            var byLabel = ((int)left.Label).CompareTo((int)right.Label);
            if (byLabel != 0) return byLabel;
            var leftArea = (int)left.size_x * left.size_y;
            var rightArea = (int)right.size_x * right.size_y;
            if (leftArea != rightArea) return rightArea.CompareTo(leftArea);
            var leftLong = Math.Max(left.size_x, left.size_y);
            var rightLong = Math.Max(right.size_x, right.size_y);
            if (leftLong != rightLong) return rightLong.CompareTo(leftLong);
            var leftShort = Math.Min(left.size_x, left.size_y);
            var rightShort = Math.Min(right.size_x, right.size_y);
            if (leftShort != rightShort) return rightShort.CompareTo(leftShort);
            if (left.GroupKey != right.GroupKey) return left.GroupKey.CompareTo(right.GroupKey);
            if (left.OriginalY != right.OriginalY) return left.OriginalY.CompareTo(right.OriginalY);
            if (left.OriginalX != right.OriginalX) return left.OriginalX.CompareTo(right.OriginalX);
            return left.StableOrder.CompareTo(right.StableOrder);
        }

        private static bool CanPlace(bool[] occupied, byte width, int x, int y, int itemWidth, int itemHeight)
        {
            for (var cx = x; cx < x + itemWidth; cx++)
                for (var cy = y; cy < y + itemHeight; cy++)
                    if (occupied[cy * width + cx]) return false;
            return true;
        }

        private static void Occupy(bool[] occupied, byte width, int x, int y, int itemWidth, int itemHeight, bool value)
        {
            for (var cx = x; cx < x + itemWidth; cx++)
                for (var cy = y; cy < y + itemHeight; cy++) occupied[cy * width + cx] = value;
        }

        private static bool CreatesEnclosedHole(bool[] occupied, byte width, byte height)
        {
            var reachable = new bool[occupied.Length];
            var queue = new Queue<int>();
            for (var x = 0; x < width; x++)
            {
                EnqueueEmpty(x, 0, occupied, reachable, queue, width);
                EnqueueEmpty(x, height - 1, occupied, reachable, queue, width);
            }
            for (var y = 0; y < height; y++)
            {
                EnqueueEmpty(0, y, occupied, reachable, queue, width);
                EnqueueEmpty(width - 1, y, occupied, reachable, queue, width);
            }
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % width;
                var y = index / width;
                EnqueueEmpty(x - 1, y, occupied, reachable, queue, width, height);
                EnqueueEmpty(x + 1, y, occupied, reachable, queue, width, height);
                EnqueueEmpty(x, y - 1, occupied, reachable, queue, width, height);
                EnqueueEmpty(x, y + 1, occupied, reachable, queue, width, height);
            }
            for (var i = 0; i < occupied.Length; i++)
                if (!occupied[i] && !reachable[i]) return true;
            return false;
        }

        private static void EnqueueEmpty(int x, int y, bool[] occupied, bool[] reachable,
            Queue<int> queue, byte width, int height = -1)
        {
            if (x < 0 || y < 0 || x >= width) return;
            var actualHeight = height < 0 ? occupied.Length / width : height;
            if (y >= actualHeight) return;
            var index = y * width + x;
            if (occupied[index] || reachable[index]) return;
            reachable[index] = true;
            queue.Enqueue(index);
        }

        private static bool FitsEither(PackableItem item, byte width, byte height)
        {
            return (item.size_x <= width && item.size_y <= height)
                || (item.size_y <= width && item.size_x <= height);
        }

        private static byte ForwardRotation(byte rotation)
        {
            // PreferredRotation is normalized to the absolute readable 0/1
            // base at the planning boundary; only its quarter-turn candidate
            // is considered next.
            return TidyReadableRotation.Normalize(rotation);
        }
        private static byte FootWidth(PackableItem item, byte rotation) { return (rotation & 1) == 1 ? item.size_y : item.size_x; }
        private static byte FootHeight(PackableItem item, byte rotation) { return (rotation & 1) == 1 ? item.size_x : item.size_y; }

        private static bool Validate(List<PackableItem> plan, byte width, byte height)
        {
            var occupied = new bool[width * height];
            for (var i = 0; i < plan.Count; i++)
            {
                var item = plan[i];
                if (item == null || !item.Placed) return false;
                var baseRotation = TidyReadableRotation.Normalize(item.PreferredRotation);
                if (item.ResultRot != baseRotation && item.ResultRot != (byte)(baseRotation ^ 1)) return false;
                var itemWidth = FootWidth(item, item.ResultRot);
                var itemHeight = FootHeight(item, item.ResultRot);
                if (item.ResultX + itemWidth > width || item.ResultY + itemHeight > height) return false;
                for (var x = item.ResultX; x < item.ResultX + itemWidth; x++)
                    for (var y = item.ResultY; y < item.ResultY + itemHeight; y++)
                    {
                        var index = y * width + x;
                        if (occupied[index]) return false;
                        occupied[index] = true;
                    }
            }
            return true;
        }

        private static PackableItem Clone(PackableItem source)
        {
            if (source == null) return null;
            return new PackableItem
            {
                Tag = source.Tag, size_x = source.size_x, size_y = source.size_y,
                GroupKey = source.GroupKey, StableOrder = source.StableOrder,
                OriginalX = source.OriginalX, OriginalY = source.OriginalY, OriginalRot = source.OriginalRot,
                PreferredRotation = TidyReadableRotation.Normalize(source.PreferredRotation), Label = source.Label,
                ResultX = 0, ResultY = 0, ResultRot = 0, Placed = false,
            };
        }

        private static void Reset(List<PackableItem> plan)
        {
            for (var i = 0; i < plan.Count; i++)
            {
                if (plan[i] == null) continue;
                plan[i].Placed = false;
                plan[i].ResultX = 0;
                plan[i].ResultY = 0;
                plan[i].ResultRot = 0;
            }
        }
    }
}
