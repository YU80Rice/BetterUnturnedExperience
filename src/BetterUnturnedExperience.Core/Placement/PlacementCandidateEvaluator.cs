using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Placement
{
    public sealed class PlacementCandidateEvaluator : IPlacementCandidateEvaluator
    {
        public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
        {
            var rotation = (byte)(input.CurrentRotation & 3);
            var page = input.TargetContainer.Page;
            var hidden = new ItemGridPosition(page, 0, 0, rotation);
            var occupancy = input.Occupancy;
            if (occupancy == null || occupancy.Width == 0 || occupancy.Height == 0 || float.IsNaN(input.CursorGridX) || float.IsNaN(input.CursorGridY) || float.IsInfinity(input.CursorGridX) || float.IsInfinity(input.CursorGridY) || input.CursorGridX < 0f || input.CursorGridY < 0f || input.CursorGridX >= occupancy.Width || input.CursorGridY >= occupancy.Height)
                return new ItemPlacementPreview(input.DragGeneration, PlacementPreviewState.Hidden, hidden, 0, 0, PlacementReason.OutsideGrid);

            var currentWidth = (rotation & 1) == 0 ? input.ItemWidth : input.ItemHeight;
            var currentHeight = (rotation & 1) == 0 ? input.ItemHeight : input.ItemWidth;
            var currentFitsGrid = currentWidth > 0 && currentHeight > 0 && currentWidth <= occupancy.Width && currentHeight <= occupancy.Height;
            var currentX = 0;
            var currentY = 0;
            if (currentFitsGrid) Project(input.CursorGridX, input.CursorGridY, currentWidth, currentHeight, occupancy.Width, occupancy.Height, out currentX, out currentY);

            // GPT watermark: R13-edge-rot. ADR-0003 (方案 A): when the cursor is
            // at an empty-area edge (outermost column/row of the free region, or
            // an obstacle-carved boundary), the auto-rotation must flip the
            // current orientation so the LONG side hugs the edge — even though
            // the current orientation (horizontal) still fits locally. The
            // rotated candidate is computed up front so step 1 can prefer it;
            // step 2 (current fails locally) still uses the same rotated values.
            var rotated = input.AllowAutomaticRotation && input.ItemWidth != input.ItemHeight;
            var rotatedRotation = (byte)((rotation + 1) & 3);
            var rotatedWidth = (rotation & 1) == 0 ? input.ItemHeight : input.ItemWidth;
            var rotatedHeight = (rotation & 1) == 0 ? input.ItemWidth : input.ItemHeight;
            var rotatedFitsGrid = rotated && rotatedWidth > 0 && rotatedHeight > 0 && rotatedWidth <= occupancy.Width && rotatedHeight <= occupancy.Height;
            var rotatedX = 0;
            var rotatedY = 0;
            if (rotatedFitsGrid) Project(input.CursorGridX, input.CursorGridY, rotatedWidth, rotatedHeight, occupancy.Width, occupancy.Height, out rotatedX, out rotatedY);

            if (currentFitsGrid && Fits(occupancy, currentX, currentY, currentWidth, currentHeight))
            {
                // Edge-sensing band (spec §11, ADR-0003 Rev 2026-09-02): when the
                // cursor is inside a vertical band (near left/right wall) or a
                // horizontal band (near top/bottom wall), the auto-rotation must
                // tend toward the long side hugging that wall — even though the
                // current orientation (horizontal) still fits locally. Trigger is
                // cursor-grid based; physical-fit guard required. Corner overlap
                // (inside both bands) keeps the entering posture (anti-jitter);
                // outside the overlap the band takes over smoothly.
                if (rotated && rotatedFitsGrid &&
                    TryEdgeBandCandidate(occupancy, input, rotation, rotatedWidth, rotatedHeight, rotatedRotation, page, out var bandPreview))
                {
                    return bandPreview;
                }
                // Fallback: obstacle-carved edge gravity (Q6) — a fully blocked
                // row/column boundary behaves like a container wall. If the
                // rotated long side hugs such an edge while the current long side
                // does not, rotate.
                if (rotatedFitsGrid && Fits(occupancy, rotatedX, rotatedY, rotatedWidth, rotatedHeight) &&
                    LongSideHugsEdge(occupancy, rotatedX, rotatedY, rotatedWidth, rotatedHeight) &&
                    !LongSideHugsEdge(occupancy, currentX, currentY, currentWidth, currentHeight))
                {
                    return Candidate(input.DragGeneration, page, rotatedX, rotatedY, rotatedRotation, rotatedWidth, rotatedHeight, PlacementPreviewState.Candidate);
                }
                return Candidate(input.DragGeneration, page, currentX, currentY, rotation, currentWidth, currentHeight, PlacementPreviewState.Candidate);
            }

            if (rotatedFitsGrid && Fits(occupancy, rotatedX, rotatedY, rotatedWidth, rotatedHeight))
                return Candidate(input.DragGeneration, page, rotatedX, rotatedY, rotatedRotation, rotatedWidth, rotatedHeight, PlacementPreviewState.Candidate);

            var foundCurrent = false;
            var bestCurrentX = 0;
            var bestCurrentY = 0;
            var bestCurrentDistance = double.MaxValue;
            if (currentFitsGrid) Search(occupancy, input.CursorGridX, input.CursorGridY, currentWidth, currentHeight, out foundCurrent, out bestCurrentX, out bestCurrentY, out bestCurrentDistance);
            if (foundCurrent) return Candidate(input.DragGeneration, page, bestCurrentX, bestCurrentY, rotation, currentWidth, currentHeight, PlacementPreviewState.Candidate);

            var foundRotated = false;
            var bestRotatedX = 0;
            var bestRotatedY = 0;
            var bestRotatedDistance = double.MaxValue;
            if (rotatedFitsGrid) Search(occupancy, input.CursorGridX, input.CursorGridY, rotatedWidth, rotatedHeight, out foundRotated, out bestRotatedX, out bestRotatedY, out bestRotatedDistance);
            if (foundRotated) return Candidate(input.DragGeneration, page, bestRotatedX, bestRotatedY, rotatedRotation, rotatedWidth, rotatedHeight, PlacementPreviewState.Candidate);

            var attemptedFit = currentFitsGrid || rotatedFitsGrid;
            var reason = attemptedFit ? PlacementReason.Occupied : PlacementReason.OutsideGrid;
            var feedbackX = currentFitsGrid ? currentX : 0;
            var feedbackY = currentFitsGrid ? currentY : 0;
            return new ItemPlacementPreview(input.DragGeneration, PlacementPreviewState.LocallyInvalid, new ItemGridPosition(page, (byte)feedbackX, (byte)feedbackY, rotation), currentWidth, currentHeight, reason);
        }

        // GPT watermark: R13-edge-sensing-band. Spec §11: band(dim) =
        // clamp(1.0, dim*0.15, 2.0) cells, per-axis (vertical band uses
        // containerWidth, horizontal uses containerHeight). Cursor-grid based
        // trigger. Inside a vertical band the rotated (tall) orientation is
        // preferred and positioned hugging the left/right wall; inside a
        // horizontal band the rotated (wide) orientation hugs the top/bottom
        // wall. Corner overlap (both bands) keeps the entering posture. Always
        // gated by the physical-fit guard. Pure, allocation-free, stateless.
        private static bool TryEdgeBandCandidate(IGridOccupancyView occupancy, PlacementCandidateInput input,
            byte rotation, byte rotatedWidth, byte rotatedHeight, byte rotatedRotation, byte page,
            out ItemPlacementPreview preview)
        {
            preview = default(ItemPlacementPreview);
            if (!input.AllowAutomaticRotation || input.ItemWidth == input.ItemHeight) return false;

            var bandW = BandForDimension(occupancy.Width);
            var bandH = BandForDimension(occupancy.Height);
            var inLeft = input.CursorGridX < bandW;
            var inRight = input.CursorGridX >= occupancy.Width - bandW;
            var inTop = input.CursorGridY < bandH;
            var inBottom = input.CursorGridY >= occupancy.Height - bandH;
            var verticalBand = inLeft || inRight;
            var horizontalBand = inTop || inBottom;
            if (verticalBand && horizontalBand) return false; // corner overlap: keep entering posture

            int x;
            int y;
            if (verticalBand)
            {
                // Vertical band: prefer the tall footprint hugging left/right wall.
                var tall = rotatedHeight > rotatedWidth;
                if (!tall) return false;
                x = inLeft ? 0 : occupancy.Width - rotatedWidth;
                y = ProjectAxis(input.CursorGridY, rotatedHeight, occupancy.Height);
            }
            else if (horizontalBand)
            {
                // Horizontal band: prefer the wide footprint hugging top/bottom wall.
                var wide = rotatedWidth > rotatedHeight;
                if (!wide) return false;
                x = ProjectAxis(input.CursorGridX, rotatedWidth, occupancy.Width);
                y = inTop ? 0 : occupancy.Height - rotatedHeight;
            }
            else
            {
                return false; // open middle: no band gravity, keep current (D2)
            }
            if (x < 0 || y < 0 || x + rotatedWidth > occupancy.Width || y + rotatedHeight > occupancy.Height) return false;
            if (!Fits(occupancy, x, y, rotatedWidth, rotatedHeight)) return false;
            preview = Candidate(input.DragGeneration, page, x, y, rotatedRotation, rotatedWidth, rotatedHeight, PlacementPreviewState.Candidate);
            return true;
        }

        private static float BandForDimension(float dimension)
        {
            var raw = dimension * 0.15f;
            return Math.Max(1.0f, Math.Min(2.0f, raw));
        }

        private static int ProjectAxis(float cursor, byte footprint, byte grid)
        {
            var max = grid - footprint;
            var raw = (int)Math.Floor(cursor - footprint / 2f + 0.5f);
            return raw < 0 ? 0 : raw > max ? max : raw;
        }

        private static ItemPlacementPreview Candidate(uint generation, byte page, int x, int y, byte rotation, byte width, byte height, PlacementPreviewState state)
        {
            return new ItemPlacementPreview(generation, state, new ItemGridPosition(page, (byte)x, (byte)y, rotation), width, height, PlacementReason.None);
        }

        private static void Project(float cursorX, float cursorY, byte width, byte height, byte gridWidth, byte gridHeight, out int x, out int y)
        {
            var rawX = (int)Math.Floor(cursorX - width / 2f + 0.5f);
            var rawY = (int)Math.Floor(cursorY - height / 2f + 0.5f);
            var maxX = gridWidth - width;
            var maxY = gridHeight - height;
            x = rawX < 0 ? 0 : rawX > maxX ? maxX : rawX;
            y = rawY < 0 ? 0 : rawY > maxY ? maxY : rawY;
        }

        private static bool Fits(IGridOccupancyView occupancy, int x, int y, byte width, byte height)
        {
            if (x < 0 || y < 0 || x + width > occupancy.Width || y + height > occupancy.Height) return false;
            for (var yy = 0; yy < height; yy++) for (var xx = 0; xx < width; xx++) if (occupancy.IsOccupied((byte)(x + xx), (byte)(y + yy))) return false;
            return true;
        }

        // GPT watermark: R13-edge-rot. True when the footprint's LONG side is
        // flush against an empty-area edge: the container border or a fully
        // blocked neighbor line (obstacle-carved boundary). A horizontal long
        // side hugs the top/bottom border or a fully blocked row above/below;
        // a vertical long side hugs the left/right border or a fully blocked
        // column to either side. Pure, allocation-free, stateless.
        private static bool LongSideHugsEdge(IGridOccupancyView occupancy, int x, int y, byte width, byte height)
        {
            if (width >= height)
            {
                if (y == 0 || y + height >= occupancy.Height) return true;
                if (RowFullyBlocked(occupancy, y - 1, x, width)) return true;
                if (RowFullyBlocked(occupancy, y + height, x, width)) return true;
                return false;
            }
            if (x == 0 || x + width >= occupancy.Width) return true;
            if (ColumnFullyBlocked(occupancy, x - 1, y, height)) return true;
            if (ColumnFullyBlocked(occupancy, x + width, y, height)) return true;
            return false;
        }

        private static bool RowFullyBlocked(IGridOccupancyView occupancy, int row, int x, int width)
        {
            if (row < 0 || row >= occupancy.Height) return true;
            for (var xx = 0; xx < width; xx++) if (!occupancy.IsOccupied((byte)(x + xx), (byte)row)) return false;
            return true;
        }

        private static bool ColumnFullyBlocked(IGridOccupancyView occupancy, int column, int y, int height)
        {
            if (column < 0 || column >= occupancy.Width) return true;
            for (var yy = 0; yy < height; yy++) if (!occupancy.IsOccupied((byte)column, (byte)(y + yy))) return false;
            return true;
        }

        private static void Search(IGridOccupancyView occupancy, float cursorX, float cursorY, byte width, byte height, out bool found, out int bestX, out int bestY, out double bestDistance)
        {
            found = false;
            bestX = 0;
            bestY = 0;
            bestDistance = double.MaxValue;
            var maxX = occupancy.Width - width;
            var maxY = occupancy.Height - height;
            for (var y = 0; y <= maxY; y++)
            {
                for (var x = 0; x <= maxX; x++)
                {
                    if (!Fits(occupancy, x, y, width, height)) continue;
                    var dx = x + width / 2f - cursorX;
                    var dy = y + height / 2f - cursorY;
                    var distance = (double)dx * dx + (double)dy * dy;
                    if (!found || distance < bestDistance || distance == bestDistance && (y < bestY || y == bestY && x < bestX))
                    {
                        found = true;
                        bestX = x;
                        bestY = y;
                        bestDistance = distance;
                    }
                }
            }
        }
    }
}
