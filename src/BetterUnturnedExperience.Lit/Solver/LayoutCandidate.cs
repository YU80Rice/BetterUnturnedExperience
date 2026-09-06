using System;
using System.Collections.Generic;

// DEV-V2-15: migrated from LaunchInventoryTidy (author: YU80Rice, MIT License,
// Copyright (c) 2026 YU80Rice; local source of truth: Archive/2-未闭环验证项目/LaunchInventoryTidy,
// retired by the wayfinder single-source decision). Attribution:
// docs/third-party/LaunchInventoryTidy-attribution.md. Behavior migrated as-is
// unless a DEV-V2-15 note says otherwise.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// 候选布局：一次装箱尝试的完整结果，附带评分指标用于确定性挑选。
    /// 同一输入可生成多个候选，按 Score 比较后选最优。
    /// </summary>
    internal class LayoutCandidate
    {
        /// <summary>候选包含的所有物品（含未放置项，Placed=false）。</summary>
        public List<PackableItem> Items;

        /// <summary>未放置物品数量（必须为 0 才算合法候选）。</summary>
        public int UnplacedCount;

        /// <summary>同类（同 GroupKey）物品在二维布局中形成的连通块数量。越少越好。</summary>
        public int SameTypeConnectedBlocks;

        /// <summary>行主序扫描下，相邻不同 GroupKey 的边界数。越少越好。</summary>
        public int RowMajorSegmentCount;

        /// <summary>物品从 OriginalX/Y 移动到 ResultX/Y 的曼哈顿距离总和。越小越好。</summary>
        public int TotalMovementDistance;

        /// <summary>物品从 OriginalRot 改变到 ResultRot 的数量。越少越好。</summary>
        public int RotationChangeCount;

        // v2.0.1：LargestRemainingRect 字段已删除（审计 P2-1：死指标，无装箱器赋值）。
        // 原设计的第 6 评分项无数据源，删除后比较器只有 5 项 tie-break。

        /// <summary>
        /// 与另一候选比较，返回负数表示本候选更优，正数表示另一候选更优，0 表示等价。
        /// 确定性 tie-break 顺序：
        ///   1. UnplacedCount（少者优）
        ///   2. SameTypeConnectedBlocks（少者优）
        ///   3. RowMajorSegmentCount（少者优）
        ///   4. TotalMovementDistance（小者优）
        ///   5. RotationChangeCount（少者优）
        /// </summary>
        public int CompareTo(LayoutCandidate other)
        {
            if (ReferenceEquals(other, null)) return -1;
            if (UnplacedCount != other.UnplacedCount) return UnplacedCount.CompareTo(other.UnplacedCount);
            if (SameTypeConnectedBlocks != other.SameTypeConnectedBlocks) return SameTypeConnectedBlocks.CompareTo(other.SameTypeConnectedBlocks);
            if (RowMajorSegmentCount != other.RowMajorSegmentCount) return RowMajorSegmentCount.CompareTo(other.RowMajorSegmentCount);
            if (TotalMovementDistance != other.TotalMovementDistance) return TotalMovementDistance.CompareTo(other.TotalMovementDistance);
            if (RotationChangeCount != other.RotationChangeCount) return RotationChangeCount.CompareTo(other.RotationChangeCount);
            return 0;
        }

        /// <summary>计算候选的评分指标。必须在物品 Placed 状态确定后调用。</summary>
        public void ComputeMetrics(byte width, byte height)
        {
            if (Items == null)
            {
                UnplacedCount = 0;
                SameTypeConnectedBlocks = 0;
                RowMajorSegmentCount = 0;
                TotalMovementDistance = 0;
                RotationChangeCount = 0;
                return;
            }

            UnplacedCount = 0;
            TotalMovementDistance = 0;
            RotationChangeCount = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                PackableItem p = Items[i];
                if (p == null || !p.Placed) { if (p != null) UnplacedCount++; continue; }
                TotalMovementDistance += Math.Abs((int)p.ResultX - p.OriginalX)
                                       + Math.Abs((int)p.ResultY - p.OriginalY);
                if (p.ResultRot != p.OriginalRot) RotationChangeCount++;
            }

            SameTypeConnectedBlocks = ComputeSameTypeConnectedBlocks(width, height);
            RowMajorSegmentCount = ComputeRowMajorSegments();
        }

        /// <summary>
        /// 在 width x height 网格上，按 4-邻接连通性统计同 GroupKey 物品形成的连通块数量。
        /// 未放置物品不参与统计。空格不参与统计。
        /// </summary>
        private int ComputeSameTypeConnectedBlocks(byte width, byte height)
        {
            if (width == 0 || height == 0 || Items == null || Items.Count == 0) return 0;

            // 构建 (x,y) -> GroupKey 映射；同一坐标可能被多格物品覆盖
            // 物品占用 ResultX..ResultX+w-1 × ResultY..ResultY+h-1
            int w = width, h = height;
            ushort[,] cellGroup = new ushort[w, h];
            bool[,] cellFilled = new bool[w, h];
            for (int i = 0; i < Items.Count; i++)
            {
                PackableItem p = Items[i];
                if (p == null || !p.Placed) continue;
                byte pw = (p.ResultRot & 1) == 1 ? p.size_y : p.size_x;
                byte ph = (p.ResultRot & 1) == 1 ? p.size_x : p.size_y;
                int endX = Math.Min(w, p.ResultX + pw);
                int endY = Math.Min(h, p.ResultY + ph);
                for (int x = p.ResultX; x < endX; x++)
                    for (int y = p.ResultY; y < endY; y++)
                    {
                        cellGroup[x, y] = p.GroupKey;
                        cellFilled[x, y] = true;
                    }
            }

            // BFS 统计同 GroupKey 连通块
            bool[,] visited = new bool[w, h];
            int blocks = 0;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (!cellFilled[x, y] || visited[x, y]) continue;
                    blocks++;
                    ushort g = cellGroup[x, y];
                    var queue = new Queue<(int, int)>();
                    queue.Enqueue((x, y));
                    visited[x, y] = true;
                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        for (int k = 0; k < 4; k++)
                        {
                            int nx = cx + dx[k];
                            int ny = cy + dy[k];
                            if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                            if (visited[nx, ny] || !cellFilled[nx, ny]) continue;
                            if (cellGroup[nx, ny] != g) continue;
                            visited[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            return blocks;
        }

        /// <summary>
        /// 行主序扫描（y 从 0 到 height-1，x 从 0 到 width-1）下，相邻不同 GroupKey 的边界数。
        /// 物品在行主序上形成"段"，同 GroupKey 连续段越多说明同类越分散。越少越好。
        /// </summary>
        private int ComputeRowMajorSegments()
        {
            if (Items == null || Items.Count == 0) return 0;

            // 按行主序提取每个物品的"出现顺序"，然后扫描相邻物品的 GroupKey
            // 注意：物品是多格的，行主序扫描应基于物品左上角的 (ResultY, ResultX)
            var sorted = new List<PackableItem>(Items.Count);
            for (int i = 0; i < Items.Count; i++)
            {
                PackableItem p = Items[i];
                if (p != null && p.Placed) sorted.Add(p);
            }
            sorted.Sort((a, b) =>
            {
                int c = a.ResultY.CompareTo(b.ResultY);
                if (c != 0) return c;
                return a.ResultX.CompareTo(b.ResultX);
            });

            int segments = 0;
            ushort lastGroup = 0;
            bool hasLast = false;
            for (int i = 0; i < sorted.Count; i++)
            {
                ushort g = sorted[i].GroupKey;
                if (!hasLast) { segments = 1; lastGroup = g; hasLast = true; continue; }
                if (g != lastGroup) { segments++; lastGroup = g; }
            }
            return segments;
        }
    }
}
