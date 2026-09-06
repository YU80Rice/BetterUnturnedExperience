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
    /// 背包自动整理算法的输入/输出物品表示。
    /// 该类刻意不引用任何 Unity / Unturned 类型，保持算法层纯净，
    /// 便于独立单元测试与跨项目复用。
    /// </summary>
    internal class PackableItem
    {
        /// <summary>
        /// 算法不解释的附加数据；调用方通常在此存放原始 ItemJar 引用，
        /// 算法输出后由调用方读取 Tag 并把坐标写回游戏对象。
        /// </summary>
        public object Tag;

        /// <summary>物品原始（未旋转）宽度（占用的列数）。</summary>
        public byte size_x;

        /// <summary>物品原始（未旋转）高度（占用的行数）。</summary>
        public byte size_y;

        /// <summary>算法输出：在网格中的左上角列坐标。</summary>
        public byte ResultX;

        /// <summary>算法输出：在网格中的左上角行坐标。</summary>
        public byte ResultY;

        /// <summary>算法输出：0 = 不旋转，1 = 旋转 90度（与 Unturned 的 rot 字段语义一致）。</summary>
        public byte ResultRot;

        /// <summary>
        /// 算法输出：true = 已成功放置（ResultX/Y/Rot 有效）；
        /// false = 未放置（尺寸异常或网格装不下），调用方应保留原位。
        /// </summary>
        public bool Placed;

        // ─────────────────────────────────────────────────────────────
        // v2.0.0 扩展：同类聚合 + 稳定排序 + 快捷键迁移支持
        // ─────────────────────────────────────────────────────────────

        /// <summary>同组键，通常 = jar.item.id。同 GroupKey 物品在 SameType 模式下聚合放置。</summary>
        public ushort GroupKey;

        /// <summary>捕获顺序索引（调用方在构建 PackableItem 时填入），用于稳定 tie-break。</summary>
        public int StableOrder;

        /// <summary>物品原始 X 坐标（整理前），用于计算移动距离与快捷键迁移。</summary>
        public byte OriginalX;

        /// <summary>物品原始 Y 坐标（整理前），用于计算移动距离与快捷键迁移。</summary>
        public byte OriginalY;

        /// <summary>物品原始旋转（整理前），用于计算旋转变化。</summary>
        public byte OriginalRot;

        /// <summary>偏好旋转（默认 = OriginalRot）。候选生成时可建议保留原旋转以减少变化。</summary>
        public byte PreferredRotation;
    }

    /// <summary>
    /// 整理算法模式选择。v2.0.0 起 SameType=0 为默认（同类聚合优先）。
    /// </summary>
    internal enum TidyMode : byte
    {
        /// <summary>同类优先：相同 GroupKey 物品聚合放置，组内按 StableOrder 稳定排序。</summary>
        SameType = 0,

        /// <summary>空间优先：剩余大矩形优先（MaxRects + BSSF + 矩形分裂），剩余空间成大块。</summary>
        MaxRects = 1,

        /// <summary>大件优先：FFD First-Fit 行主序扫描贪心。</summary>
        FFD = 2,
    }

    /// <summary>
    /// 2D 装箱求解器，支持三种模式：
    ///  - SameType（默认）：同类聚合 + 多候选 + 评分挑选
    ///  - MaxRects：剩余大矩形优先（BSSF + 矩形分裂）
    ///  - FFD：行主序贪心 First-Fit
    ///
    /// v2.0.0 改造要点：
    ///  - TryPack 内部生成最多 3 个候选布局并按确定性指标选最优
    ///  - 排序比较器末尾必须按 StableOrder 收尾，保证相同输入产生相同输出
    ///  - 候选生成调用现有 TryPackMaxRects / TryPackFFD 作为几何兜底
    ///
    /// v2.0.6.13 Round 8 改造要点：
    ///  - MaxRects/FFD 也生成 2 个候选（主方向 + 反向兜底），避免单排序方向失败时 Rejected
    /// </summary>
    internal static class InventorySolver
    {
        private const int MAX_CANDIDATES = 3;

        /// <summary>
        /// 尝试将所有物品装入 width x height 的虚拟网格。
        ///
        /// 返回值：
        ///   - true = 所有合法物品均已成功放置（异常物品 Placed=false 但不影响整体）
        ///   - false = 部分合法物品未放置（调用方可根据 Placed 标志决定部分重排或放弃）
        ///
        /// result 总是包含所有输入物品（含异常），调用方根据 Placed 区分处理。
        /// v2.0.0：SameType 模式下生成多候选并选最优；MaxRects/FFD 单候选直出。
        /// </summary>
        public static bool TryPack(byte width, byte height, List<PackableItem> items,
                                    out List<PackableItem> result, bool sortDescending = true,
                                    TidyMode mode = TidyMode.SameType)
        {
            result = null;
            if (items == null || items.Count == 0)
            {
                result = new List<PackableItem>(0);
                return true;
            }

            if (width == 0 || height == 0) return false;

            // 步骤 1：标记异常物品 + 复制原始字段
            PrepareItems(items);

            int validCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                PackableItem it = items[i];
                if (it == null) continue;
                bool isValid = it.size_x > 0 && it.size_y > 0
                    && (FitsGrid(it.size_x, it.size_y, width, height)
                        || FitsGrid(it.size_y, it.size_x, width, height));
                if (isValid) validCount++;
            }

            if (mode == TidyMode.SameType)
            {
                // v2.0.1：同类聚合生成 3 个候选并选最优，sortDescending 参数传入候选 C 的几何排序
                return TryPackSameTypeMultiCandidate(items, out result, validCount, width, height, sortDescending);
            }

            // v2.0.6.13 Round 8：几何模式也生成多候选选最优，避免单排序方向失败时 Rejected。
            // 主候选按 sortDescending 排序；兜底候选按反向排序。两者按指标选最优。
            return TryPackGeometricMultiCandidate(items, out result, validCount, width, height, sortDescending, mode);
        }

        private static bool TryPackGeometricMultiCandidate(List<PackableItem> items,
            out List<PackableItem> result, int validCount, byte width, byte height,
            bool sortDescending, TidyMode mode)
        {
            bool useMaxRects = mode == TidyMode.MaxRects;
            var candidates = new List<LayoutCandidate>(2);

            // 候选 A：按 sortDescending 排序（主方向）
            var candidateA = BuildGeometricCandidate(items, width, height, sortDescending, useMaxRects);
            if (candidateA != null) candidates.Add(candidateA);

            // 候选 B：按 !sortDescending 排序（兜底方向，防止小件先放挤占大件位置）
            var candidateB = BuildGeometricCandidate(items, width, height, !sortDescending, useMaxRects);
            if (candidateB != null) candidates.Add(candidateB);

            if (candidates.Count == 0)
            {
                result = CloneForPacking(items);
                return false;
            }

            for (int i = 0; i < candidates.Count; i++)
                candidates[i].ComputeMetrics(width, height);

            LayoutCandidate best = candidates[0];
            for (int i = 1; i < candidates.Count; i++)
            {
                if (candidates[i].CompareTo(best) < 0)
                    best = candidates[i];
            }

            result = best.Items;

            int placedCount = 0;
            for (int i = 0; i < result.Count; i++)
                if (result[i] != null && result[i].Placed) placedCount++;

            return placedCount == validCount;
        }

        // ─────────────────────────────────────────────────────────────
        // 准备与克隆
        // ─────────────────────────────────────────────────────────────

        private static void PrepareItems(List<PackableItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                PackableItem it = items[i];
                if (it == null) continue;
                it.Placed = false;
                it.ResultX = 0;
                it.ResultY = 0;
                it.ResultRot = 0;
                // PreferredRotation 默认 = OriginalRot（调用方未显式设置时）
                if (it.PreferredRotation == 0 && it.OriginalRot != 0)
                    it.PreferredRotation = it.OriginalRot;
            }
        }

        /// <summary>深拷贝 items 列表用于装箱（不复制 Tag 引用，仅复制装箱相关字段）。</summary>
        private static List<PackableItem> CloneForPacking(List<PackableItem> items)
        {
            var clone = new List<PackableItem>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                PackableItem src = items[i];
                if (src == null) { clone.Add(null); continue; }
                clone.Add(new PackableItem
                {
                    Tag = src.Tag,
                    size_x = src.size_x,
                    size_y = src.size_y,
                    GroupKey = src.GroupKey,
                    StableOrder = src.StableOrder,
                    OriginalX = src.OriginalX,
                    OriginalY = src.OriginalY,
                    OriginalRot = src.OriginalRot,
                    PreferredRotation = src.PreferredRotation != 0 ? src.PreferredRotation : src.OriginalRot,
                });
            }
            return clone;
        }

        // ─────────────────────────────────────────────────────────────
        // SameType 多候选生成 + 评分
        // ─────────────────────────────────────────────────────────────

        private static bool TryPackSameTypeMultiCandidate(List<PackableItem> items,
            out List<PackableItem> result, int validCount, byte width, byte height, bool sortDescending)
        {
            var candidates = new List<LayoutCandidate>(MAX_CANDIDATES);

            // 候选 A：同类分组，组按总降序面积 + 组内 StableOrder
            // v2.0.1：sortDescending 控制组排序方向（true=大组优先，false=小组优先）
            var candidateA = BuildSameTypeCandidate(items, width, height,
                groupOrder: GroupOrder.TotalAreaDescending, sortDescending: sortDescending);
            if (candidateA != null) candidates.Add(candidateA);

            // 候选 B：同类分组，组按首次出现顺序 + 组内按尺寸方向
            // v2.0.1：sortDescending 控制组内同 ID 物品按尺寸排序方向
            var candidateB = BuildSameTypeCandidate(items, width, height,
                groupOrder: GroupOrder.FirstAppearance, sortDescending: sortDescending);
            if (candidateB != null) candidates.Add(candidateB);

            // 候选 C：几何兜底（MaxRects），v2.0.1 应用 sortDescending 参数
            var candidateC = BuildGeometricCandidate(items, width, height,
                sortDescending: sortDescending, useMaxRects: true);
            if (candidateC != null) candidates.Add(candidateC);

            if (candidates.Count == 0)
            {
                result = CloneForPacking(items);
                return false;
            }

            // 计算每个候选的指标
            for (int i = 0; i < candidates.Count; i++)
                candidates[i].ComputeMetrics(width, height);

            // 选最优
            LayoutCandidate best = candidates[0];
            for (int i = 1; i < candidates.Count; i++)
            {
                if (candidates[i].CompareTo(best) < 0)
                    best = candidates[i];
            }

            result = best.Items;

            int placedCount = 0;
            for (int i = 0; i < result.Count; i++)
                if (result[i] != null && result[i].Placed) placedCount++;

            return placedCount == validCount;
        }

        private enum GroupOrder
        {
            TotalAreaDescending,
            FirstAppearance,
        }

        private static LayoutCandidate BuildSameTypeCandidate(List<PackableItem> items,
            byte width, byte height, GroupOrder groupOrder, bool sortDescending)
        {
            var clone = CloneForPacking(items);

            // 按 GroupKey 分组
            var groups = new Dictionary<ushort, List<PackableItem>>();
            var firstAppearance = new Dictionary<ushort, int>();
            var groupTotalArea = new Dictionary<ushort, long>();
            int appearanceIdx = 0;
            for (int i = 0; i < clone.Count; i++)
            {
                PackableItem p = clone[i];
                if (p == null || p.size_x == 0 || p.size_y == 0) continue;
                if (!groups.ContainsKey(p.GroupKey))
                {
                    groups[p.GroupKey] = new List<PackableItem>();
                    firstAppearance[p.GroupKey] = appearanceIdx++;
                    groupTotalArea[p.GroupKey] = 0;
                }
                groups[p.GroupKey].Add(p);
                groupTotalArea[p.GroupKey] += (long)p.size_x * p.size_y;
            }

            // 组排序
            var groupKeys = new List<ushort>(groups.Keys);
            switch (groupOrder)
            {
                case GroupOrder.TotalAreaDescending:
                    // v2.0.1：sortDescending 控制组排序方向
                    groupKeys.Sort((a, b) =>
                    {
                        int c = sortDescending
                            ? groupTotalArea[b].CompareTo(groupTotalArea[a])
                            : groupTotalArea[a].CompareTo(groupTotalArea[b]);
                        if (c != 0) return c;
                        return firstAppearance[a].CompareTo(firstAppearance[b]);
                    });
                    break;
                case GroupOrder.FirstAppearance:
                    groupKeys.Sort((a, b) => firstAppearance[a].CompareTo(firstAppearance[b]));
                    break;
            }

            // v2.0.1：组内排序先按尺寸方向，再按 StableOrder 收尾
            // 同 ID 物品通常尺寸相同，此时尺寸排序无效，StableOrder 保证确定性
            // 不同尺寸的同 ID 物品（罕见）按 direction 排序
            foreach (var kv in groups)
            {
                kv.Value.Sort((a, b) =>
                {
                    long areaA = (long)a.size_x * a.size_y;
                    long areaB = (long)b.size_x * b.size_y;
                    if (areaA != areaB)
                        return sortDescending ? areaB.CompareTo(areaA) : areaA.CompareTo(areaB);
                    return a.StableOrder.CompareTo(b.StableOrder);
                });
            }

            // 构建排序后的物品列表：组顺序 × 组内顺序
            var sorted = new List<PackableItem>(clone.Count);
            for (int gi = 0; gi < groupKeys.Count; gi++)
            {
                var groupItems = groups[groupKeys[gi]];
                for (int j = 0; j < groupItems.Count; j++)
                    sorted.Add(groupItems[j]);
            }

            // 添加异常物品到末尾（保留原位）
            for (int i = 0; i < clone.Count; i++)
            {
                PackableItem p = clone[i];
                if (p == null || p.size_x == 0 || p.size_y == 0)
                    sorted.Add(p);
            }

            // 用 MaxRects 装箱（保证剩余空间成大块）
            int placedCount = TryPackMaxRects(sorted, width, height);

            return new LayoutCandidate
            {
                Items = sorted,
                UnplacedCount = CountValidItems(sorted) - placedCount,
            };
        }

        private static LayoutCandidate BuildGeometricCandidate(List<PackableItem> items,
            byte width, byte height, bool sortDescending, bool useMaxRects)
        {
            var clone = CloneForPacking(items);
            SortByGeometry(clone, sortDescending);

            int placedCount;
            if (useMaxRects)
                placedCount = TryPackMaxRects(clone, width, height);
            else
                placedCount = TryPackFFD(clone, width, height);

            return new LayoutCandidate
            {
                Items = clone,
                UnplacedCount = CountValidItems(clone) - placedCount,
            };
        }

        private static int CountValidItems(List<PackableItem> items)
        {
            int count = 0;
            for (int i = 0; i < items.Count; i++)
            {
                PackableItem p = items[i];
                if (p != null && p.size_x > 0 && p.size_y > 0) count++;
            }
            return count;
        }

        // ─────────────────────────────────────────────────────────────
        // 几何排序
        // ─────────────────────────────────────────────────────────────

        private static void SortByGeometry(List<PackableItem> sorted, bool sortDescending)
        {
            sorted.Sort((a, b) =>
            {
                bool aInvalid = a == null || a.size_x == 0 || a.size_y == 0;
                bool bInvalid = b == null || b.size_x == 0 || b.size_y == 0;
                if (aInvalid && bInvalid)
                {
                    // 异常物品之间按 StableOrder 收尾
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return a.StableOrder.CompareTo(b.StableOrder);
                }
                if (aInvalid) return 1;
                if (bInvalid) return -1;

                long areaA = (long)a.size_x * a.size_y;
                long areaB = (long)b.size_x * b.size_y;
                if (areaA != areaB)
                    return sortDescending ? areaB.CompareTo(areaA) : areaA.CompareTo(areaB);
                int longA = Math.Max(a.size_x, a.size_y);
                int longB = Math.Max(b.size_x, b.size_y);
                if (longA != longB)
                    return sortDescending ? longB.CompareTo(longA) : longA.CompareTo(longB);
                if (a.size_x != b.size_x)
                    return sortDescending ? b.size_x.CompareTo(a.size_x) : a.size_x.CompareTo(b.size_x);

                // v2.0.0 tie-break：GroupKey 然后 StableOrder，保证确定性输出
                if (a.GroupKey != b.GroupKey) return a.GroupKey.CompareTo(b.GroupKey);
                return a.StableOrder.CompareTo(b.StableOrder);
            });
        }

        // ─────────────────────────────────────────────────────────────
        // D 模式：FFD First-Fit 行主序扫描
        // ─────────────────────────────────────────────────────────────

        private static int TryPackFFD(List<PackableItem> sorted, byte width, byte height)
        {
            bool[,] virtualGrid = new bool[width, height];
            int placedCount = 0;
            for (int idx = 0; idx < sorted.Count; idx++)
            {
                PackableItem current = sorted[idx];
                if (current == null || current.size_x == 0 || current.size_y == 0) continue;

                if (TryPlaceFirstFit(current, virtualGrid, width, height))
                {
                    placedCount++;
                }
            }
            return placedCount;
        }

        /// <summary>
        /// 判断原始尺寸 (sx, sy) 在不旋转的情况下能否塞进 width x height 网格。
        /// </summary>
        private static bool FitsGrid(byte sx, byte sy, byte width, byte height)
        {
            return sx <= width && sy <= height;
        }

        /// <summary>
        /// 贪心 First-Fit：遍历所有空格（行主序），找第一个能放下 item 的位置。
        /// 优先尝试 PreferredRotation，失败再尝试另一旋转。正方形物品跳过旋转。
        /// </summary>
        private static bool TryPlaceFirstFit(PackableItem item, bool[,] virtualGrid, byte width, byte height)
        {
            byte firstRot = item.PreferredRotation != 0 ? item.PreferredRotation : (byte)0;
            byte secondRot = (byte)(firstRot ^ 1);

            // 行主序扫描
            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    if (virtualGrid[x, y]) continue;

                    if (TryPlace(item, x, y, firstRot, virtualGrid, width, height))
                    {
                        item.ResultX = x; item.ResultY = y; item.ResultRot = firstRot;
                        item.Placed = true;
                        return true;
                    }

                    if (item.size_x != item.size_y &&
                        TryPlace(item, x, y, secondRot, virtualGrid, width, height))
                    {
                        item.ResultX = x; item.ResultY = y; item.ResultRot = secondRot;
                        item.Placed = true;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 尝试把一个物品（按指定 rot）放在 (startX, startY)，并在合法时标记占用矩阵。
        /// </summary>
        private static bool TryPlace(PackableItem item, byte startX, byte startY, byte rot,
                                     bool[,] virtualGrid, byte width, byte height)
        {
            byte w = item.size_x;
            byte h = item.size_y;
            if ((rot & 1) == 1)
            {
                w = item.size_y;
                h = item.size_x;
            }

            int endX = (int)startX + w;
            int endY = (int)startY + h;
            if (endX > width || endY > height) return false;

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (virtualGrid[x, y]) return false;
                }
            }

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    virtualGrid[x, y] = true;
                }
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // C 模式：MaxRects（BSSF + 矩形分裂）
        // ─────────────────────────────────────────────────────────────

        private struct Rect
        {
            public int x, y, w, h;
            public Rect(int x, int y, int w, int h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        }

        private static int TryPackMaxRects(List<PackableItem> sorted, byte width, byte height)
        {
            var freeRects = new List<Rect> { new Rect(0, 0, width, height) };
            int placedCount = 0;

            for (int idx = 0; idx < sorted.Count; idx++)
            {
                PackableItem current = sorted[idx];
                if (current == null || current.size_x == 0 || current.size_y == 0) continue;

                if (TryPlaceMaxRectsBSSF(current, freeRects))
                {
                    placedCount++;
                }
            }
            return placedCount;
        }

        /// <summary>
        /// BSSF：在 freeRects 中找最佳剩余矩形放置物品。
        /// 优先尝试 PreferredRotation，失败再尝试另一旋转。正方形跳过旋转。
        /// </summary>
        private static bool TryPlaceMaxRectsBSSF(PackableItem item, List<Rect> freeRects)
        {
            int bestShortSide = int.MaxValue;
            int bestLongSide = int.MaxValue;
            int bestRectIndex = -1;
            byte bestRot = 0;

            byte prefRot = item.PreferredRotation != 0 ? item.PreferredRotation : (byte)0;
            byte altRot = (byte)(prefRot ^ 1);

            for (int i = 0; i < freeRects.Count; i++)
            {
                Rect r = freeRects[i];

                // 优先旋转
                byte w0 = (prefRot & 1) == 1 ? item.size_y : item.size_x;
                byte h0 = (prefRot & 1) == 1 ? item.size_x : item.size_y;
                if (w0 <= r.w && h0 <= r.h)
                {
                    int leftoverW = r.w - w0;
                    int leftoverH = r.h - h0;
                    int shortSide = Math.Min(leftoverW, leftoverH);
                    int longSide = Math.Max(leftoverW, leftoverH);
                    if (shortSide < bestShortSide ||
                        (shortSide == bestShortSide && longSide < bestLongSide))
                    {
                        bestShortSide = shortSide;
                        bestLongSide = longSide;
                        bestRectIndex = i;
                        bestRot = prefRot;
                    }
                }

                // 备选旋转（正方形跳过）
                if (item.size_x != item.size_y)
                {
                    byte w1 = (altRot & 1) == 1 ? item.size_y : item.size_x;
                    byte h1 = (altRot & 1) == 1 ? item.size_x : item.size_y;
                    if (w1 <= r.w && h1 <= r.h)
                    {
                        int leftoverW = r.w - w1;
                        int leftoverH = r.h - h1;
                        int shortSide = Math.Min(leftoverW, leftoverH);
                        int longSide = Math.Max(leftoverW, leftoverH);
                        if (shortSide < bestShortSide ||
                            (shortSide == bestShortSide && longSide < bestLongSide))
                        {
                            bestShortSide = shortSide;
                            bestLongSide = longSide;
                            bestRectIndex = i;
                            bestRot = altRot;
                        }
                    }
                }
            }

            if (bestRectIndex < 0) return false;

            Rect best = freeRects[bestRectIndex];
            item.ResultX = (byte)best.x;
            item.ResultY = (byte)best.y;
            item.ResultRot = bestRot;
            item.Placed = true;

            byte pw = bestRot == 0 ? item.size_x : item.size_y;
            byte ph = bestRot == 0 ? item.size_y : item.size_x;

            Rect placed = new Rect(best.x, best.y, pw, ph);
            freeRects.RemoveAt(bestRectIndex);
            SplitRect(best, placed, freeRects);
            PruneContainedRects(freeRects);
            return true;
        }

        private static void SplitRect(Rect outer, Rect inner, List<Rect> output)
        {
            if (inner.y > outer.y)
                output.Add(new Rect(outer.x, outer.y, outer.w, inner.y - outer.y));
            int innerBottom = inner.y + inner.h;
            int outerBottom = outer.y + outer.h;
            if (innerBottom < outerBottom)
                output.Add(new Rect(outer.x, innerBottom, outer.w, outerBottom - innerBottom));
            if (inner.x > outer.x)
                output.Add(new Rect(outer.x, inner.y, inner.x - outer.x, inner.h));
            int innerRight = inner.x + inner.w;
            int outerRight = outer.x + outer.w;
            if (innerRight < outerRight)
                output.Add(new Rect(innerRight, inner.y, outerRight - innerRight, inner.h));
        }

        private static void PruneContainedRects(List<Rect> rects)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                for (int j = rects.Count - 1; j > i; j--)
                {
                    if (Contains(rects[i], rects[j]))
                    {
                        rects.RemoveAt(j);
                    }
                    else if (Contains(rects[j], rects[i]))
                    {
                        rects.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            return inner.x >= outer.x && inner.y >= outer.y
                && inner.x + inner.w <= outer.x + outer.w
                && inner.y + inner.h <= outer.y + outer.h;
        }
    }
}
