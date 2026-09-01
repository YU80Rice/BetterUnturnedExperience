using BetterUnturnedExperience.Contracts;
using SDG.Unturned;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// Immutable occupancy snapshot built from the native Items collection.
    /// Items.items is a compact list; each ItemJar contributes its own
    /// position, dimensions, and rotated footprint.
    /// </summary>
    internal sealed class NativeItemGridOccupancySnapshot : IGridOccupancyView
    {
        private readonly byte width;
        private readonly byte height;
        private readonly bool[] occupied;

        private NativeItemGridOccupancySnapshot(byte width, byte height, bool[] occupied)
        {
            this.width = width;
            this.height = height;
            this.occupied = occupied;
        }

        public byte Width { get { return width; } }
        public byte Height { get { return height; } }

        public bool IsOccupied(byte x, byte y)
        {
            if (x >= width || y >= height) return true;
            return occupied[y * width + x];
        }

        internal static bool TryCreateFromItems(Items items, ItemJar excludedJar,
            out NativeItemGridOccupancySnapshot snapshot)
        {
            snapshot = null;
            if (items == null || items.width == 0 || items.height == 0 || items.items == null) return false;

            var cells = new bool[items.width * items.height];
            foreach (var jar in items.items)
            {
                if (jar == null) return false;
                if (object.ReferenceEquals(jar, excludedJar)) continue;

                var footprintWidth = jar.size_x;
                var footprintHeight = jar.size_y;
                if (footprintWidth == 0 || footprintHeight == 0) return false;
                if ((jar.rot & 1) != 0)
                {
                    var swap = footprintWidth;
                    footprintWidth = footprintHeight;
                    footprintHeight = swap;
                }

                for (var dy = 0; dy < footprintHeight; dy++)
                {
                    for (var dx = 0; dx < footprintWidth; dx++)
                    {
                        var x = jar.x + dx;
                        var y = jar.y + dy;
                        // A native ItemJar footprint is authoritative as a
                        // whole.  Silently clipping a stale/malformed jar
                        // would publish a partial snapshot and could make an
                        // invalid placement look available.  Reject the
                        // snapshot so callers preserve native pass-through.
                        if (x >= items.width || y >= items.height)
                        {
                            snapshot = null;
                            return false;
                        }
                        cells[y * items.width + x] = true;
                    }
                }
            }

            snapshot = new NativeItemGridOccupancySnapshot(items.width, items.height, cells);
            return true;
        }
    }
}
