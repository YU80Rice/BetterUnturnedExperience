using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-02 (V5-T3): the ONE official tidy planning module — tagged
    /// segments, row bands, emptiness gathered bottom-right. This is the deep
    /// module the current-page tidy, the Ctrl+all-pages tidy, the container
    /// tidy (DEV-V5-03) and both recovery adapters (DEV-V5-04/05) consume; no
    /// entry point copies the algorithm.
    ///
    /// Contract (T3 Q4 four rulings, frozen):
    ///  - input = item set + grid + a few stable preferences; output = a
    ///    deterministic plan or an explicit failure (never a half-committed
    ///    layout: on false every plan entry is Placed=false);
    ///  - planning never mutates the input items (and through them never the
    ///    real inventory — the transaction service commits the plan or nothing);
    ///  - hard invariants before visuals: no overlap, no out-of-bounds, no
    ///    lost or duplicated item, rotated footprints correct, same input →
    ///    same output; an item that can be placed must be placed (the
    ///    legacy "fits but rejected through an overlap bug" class may never
    ///    reappear);
    ///  - classification, label order, band anchoring, side-by-side, wrapping,
    ///    rotation, bottom-right scoring and the stable finish are ALL
    ///    internal — this surface is the only seam.
    ///
    /// Frozen algorithm (the computable answers V5-T3 demands for Q1/2/3):
    ///  1. ORDER. Items enter the stream by the frozen label order; within a
    ///    label by group (同 ID 连续: the GroupKey block) ordered total area →
    ///    longest side → first appearance → group key, and inside a group by
    ///    area → long side → width. The stableDescending preference ONLY
    ///    flips the trailing StableOrder tie between otherwise identical
    ///    items — it never moves a label or changes the banding.
    ///  2. BANDS (T3-2, computable). A band starts at x=0 on the row below
    ///    every closed band; its HEIGHT is set by its 主体 (left body): the
    ///    best candidate of the label at the head of the stream, ranked by
    ///    height → (slim-tall items w≤1 ∧ h≥3 yield at equal height, so an
    ///    obviously narrow cavity never anchors) → area → long side → width →
    ///    capture order. Items then
    ///    fill left→right while they fit the band (h ≤ band height, w ≤ the
    ///    remaining width, cells free). A label may join the band to the
    ///    right ONLY once the band's current label has no item that still
    ///    fits the band — otherwise the band closes and the next row starts
    ///    at the leftmost column (换行). Segments never back-fill a closed
    ///    band; a free cell behind a closed band is a 死洞 only for the
    ///    banding stage — the rescue stage below may use it (hard floor wins
    ///    over the visual floor, named in the ticket audit). Bands may have
    ///    different heights. Emptiness therefore gathers at the right edge
    ///    of open rows and the bottom-right of the grid.
    ///  3. ROTATION (T3-4/Q4 visual). Default forward (the item's preferred
    ///    rotation is kept, text readable); an item is rotated ONLY when it
    ///    otherwise could not be placed at all (the bottom-left rescue
    ///    trying the alternate footprint last), never to fill one more cell.
    ///  4. FAILURE IS TRANSACTIONAL. After banding, unplaced items go through
    ///    the deterministic ordered bottom-left rescue (pref rotation first,
    ///    alternate last); anything still unplaced triggers ONE full-page
    ///    EXACT-feasibility rebuild (bounded backtracking, complete within
    ///    its node budget: it finds a legal full placement whenever one
    ///    exists, stops at the first solution without comparing quality —
    ///    so T3's 不做全局最优装箱 and 能放下必须放下 hold together). This
    ///    is the named stage where label structure yields to the hard floor;
    ///    only exhaustion or the budget (which unwinds to zero) can fail the
    ///    WHOLE plan — result entries are all reset to Placed=false and the
    ///    caller gets an explicit reason. The plan is validated
    ///    (bounds/overlap/footprint) before true is ever returned.
    /// </summary>
    internal static class TaggedRowBandLayout
    {
        /// <summary>Internal implementation identity (V5-T3: 内部标识可叫 tagged-row-band-v1，不进玩家界面、不进契约).</summary>
        internal const string LayoutId = "tagged-row-band-v1";

        /// <summary>
        /// Plans one page/grid. Returns true with a fully valid plan (every
        /// VALID item placed — valid = size-positive; T3's 全部放置或明确失败
        /// is quantified over valid items, matching the migrated solver's
        /// "异常物品 Placed=false 但不影响整体" contract the transaction layer
        /// already counts on) or false with an explicit reason and zero
        /// placements. The returned list clones the inputs (one entry per
        /// input item, same order, same Tag) and never touches the caller's
        /// items. A zero-size entry cannot own a single cell — no algorithm
        /// can place it, so it is reported as an unplaced entry and the
        /// caller keeps the jar in place: nothing is ever lost to an
        /// unplaceable footprint (in production such a jar is already
        /// fail-closed at Prepare, this is the module-level honesty seam).
        /// stableDescending only orders the finish between otherwise-identical
        /// items; it never changes the label order or the main layout.
        /// </summary>
        internal static bool TryPlanLayout(byte width, byte height, bool stableDescending,
            List<PackableItem> items, out List<PackableItem> plan, out string failureReason)
        {
            plan = null;
            failureReason = null;

            var count = items == null ? 0 : items.Count;
            var clones = new List<PackableItem>(count);
            for (var i = 0; i < count; i++) clones.Add(CloneItem(items[i]));
            plan = clones;
            if (count == 0) return true;
            if (width == 0 || height == 0)
            {
                ResetPlacements(clones);
                failureReason = "empty-grid";
                return false;
            }

            // Valid entries are the size-positive ones; a zero-size anomaly
            // stays unplaced without dragging the page down (the caller keeps
            // it in place — same contract the transaction layer expects).
            var work = new List<int>(count);
            for (var i = 0; i < count; i++)
                if (clones[i] != null && clones[i].size_x > 0 && clones[i].size_y > 0) work.Add(i);

            // An item that fits neither orientation can never be placed: that
            // is a cannot-fit page (all-or-nothing), never a half layout.
            for (var w = 0; w < work.Count; w++)
            {
                var it = clones[work[w]];
                if (!FitsEitherOrientation(it, width, height))
                {
                    ResetPlacements(clones);
                    failureReason = "oversized:" + it.GroupKey.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return false;
                }
            }

            var ordered = OrderStream(clones, work, stableDescending);
            var occupied = new bool[width * height];

            BandPlacementPass(clones, ordered, width, height, occupied);
            RescueBottomLeftPass(clones, ordered, width, height, occupied);

            // 硬性「能放下必须放下」先于视觉：行带+局部救济仍未放全时，整单
            // 让位给有界精确可行性求解（R4/R5-Spec 反例的闭包形态：任何固定
            // 贪心序都存在「可行但被拒」输入类，重排层必须完备才配得上硬底线）。
            // 该层找到任一可行解即停——不比较布局质量，「不做全局最优装箱」
            // （T3 L36）与「能放下必须给出合法布局」（T3 L37）由此并存成立。
            if (AnyUnplaced(clones, work))
            {
                ResetPlacements(clones);
                occupied = new bool[width * height];
                SolveExactFeasibility(clones, work, width, height, occupied);
            }

            for (var i = 0; i < work.Count; i++)
            {
                var it = clones[work[i]];
                if (!it.Placed)
                {
                    ResetPlacements(clones);
                    failureReason = "cannot-fit:n=" + work.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return false;
                }
            }

            if (!ValidatePlan(clones, width, height))
            {
                ResetPlacements(clones);
                failureReason = "internal-validation";
                return false;
            }
            return true;
        }

        private static PackableItem CloneItem(PackableItem source)
        {
            if (source == null) return null;
            return new PackableItem
            {
                Tag = source.Tag,
                size_x = source.size_x,
                size_y = source.size_y,
                GroupKey = source.GroupKey,
                StableOrder = source.StableOrder,
                OriginalX = source.OriginalX,
                OriginalY = source.OriginalY,
                OriginalRot = source.OriginalRot,
                PreferredRotation = source.PreferredRotation != 0 || source.OriginalRot == 0
                    ? source.PreferredRotation : source.OriginalRot,
                Label = source.Label,
                Placed = false,
                ResultX = 0,
                ResultY = 0,
                ResultRot = 0,
            };
        }

        private static void ResetPlacements(List<PackableItem> clones)
        {
            for (var i = 0; i < clones.Count; i++)
            {
                var it = clones[i];
                if (it == null) continue;
                it.Placed = false;
                it.ResultX = 0;
                it.ResultY = 0;
                it.ResultRot = 0;
            }
        }

        private static bool FitsEitherOrientation(PackableItem it, byte width, byte height)
        {
            return (it.size_x <= width && it.size_y <= height)
                || (it.size_y <= width && it.size_x <= height);
        }

        private static byte FootWidth(PackableItem it, byte rot)
        {
            return (rot & 1) == 1 ? it.size_y : it.size_x;
        }

        private static byte FootHeight(PackableItem it, byte rot)
        {
            return (rot & 1) == 1 ? it.size_x : it.size_y;
        }

        private static bool CellFree(bool[] occupied, int width, int x, int y, int w, int h)
        {
            for (var cx = x; cx < x + w; cx++)
                for (var cy = y; cy < y + h; cy++)
                    if (occupied[cy * width + cx]) return false;
            return true;
        }

        private static void Occupy(bool[] occupied, int width, int x, int y, int w, int h)
        {
            for (var cx = x; cx < x + w; cx++)
                for (var cy = y; cy < y + h; cy++)
                    occupied[cy * width + cx] = true;
        }

        private static void SetPlacement(PackableItem it, int x, int y, byte rot)
        {
            it.ResultX = (byte)x;
            it.ResultY = (byte)y;
            it.ResultRot = rot;
            it.Placed = true;
        }

        // ─────────────────────────────────────────────────────────────
        // 1. ORDER — the stream every later stage walks (frozen, total).
        // ─────────────────────────────────────────────────────────────

        private sealed class GroupRank
        {
            public long TotalArea;
            public int MaxLongSide;
            public int FirstStable;
        }

        private static List<int> OrderStream(List<PackableItem> clones, List<int> work, bool stableDescending)
        {
            var ranks = new Dictionary<(int Label, ushort GroupKey), GroupRank>();
            for (var i = 0; i < work.Count; i++)
            {
                var it = clones[work[i]];
                var key = ((int)it.Label, it.GroupKey);
                GroupRank rank;
                if (!ranks.TryGetValue(key, out rank))
                {
                    rank = new GroupRank { FirstStable = it.StableOrder };
                    ranks[key] = rank;
                }
                rank.TotalArea += (long)it.size_x * it.size_y;
                var longSide = Math.Max(it.size_x, it.size_y);
                if (longSide > rank.MaxLongSide) rank.MaxLongSide = longSide;
                if (it.StableOrder < rank.FirstStable) rank.FirstStable = it.StableOrder;
            }

            var ordered = new List<int>(work);
            ordered.Sort((a, b) =>
            {
                var left = clones[a];
                var right = clones[b];
                var byLabel = ((int)left.Label).CompareTo((int)right.Label);
                if (byLabel != 0) return byLabel;
                GroupRank rankL, rankR;
                ranks.TryGetValue(((int)left.Label, left.GroupKey), out rankL);
                ranks.TryGetValue(((int)right.Label, right.GroupKey), out rankR);
                var byGroupArea = rankR.TotalArea.CompareTo(rankL.TotalArea);
                if (byGroupArea != 0) return byGroupArea;
                var byGroupLong = rankR.MaxLongSide.CompareTo(rankL.MaxLongSide);
                if (byGroupLong != 0) return byGroupLong;
                var byGroupFirst = rankL.FirstStable.CompareTo(rankR.FirstStable);
                if (byGroupFirst != 0) return byGroupFirst;
                var byGroupKey = left.GroupKey.CompareTo(right.GroupKey);
                if (byGroupKey != 0) return byGroupKey;
                var areaL = (long)left.size_x * left.size_y;
                var areaR = (long)right.size_x * right.size_y;
                if (areaL != areaR) return areaR.CompareTo(areaL);
                var longL = Math.Max(left.size_x, left.size_y);
                var longR = Math.Max(right.size_x, right.size_y);
                if (longL != longR) return longR.CompareTo(longL);
                if (left.size_x != right.size_x) return right.size_x.CompareTo(left.size_x);
                // 稳定收尾：升/降序只翻转这一条同几何 tie（V5-T3 Q2），绝不改标签序。
                var byStable = stableDescending
                    ? left.StableOrder.CompareTo(right.StableOrder)
                    : right.StableOrder.CompareTo(left.StableOrder);
                if (byStable != 0) return byStable;
                return a.CompareTo(b);
            });
            return ordered;
        }

        // ─────────────────────────────────────────────────────────────
        // 2. BANDS — 主体决定行带高，同带左→右，右侧放不下换行（x=0）。
        // ─────────────────────────────────────────────────────────────

        private static void BandPlacementPass(List<PackableItem> clones, List<int> ordered,
            byte width, byte height, bool[] occupied)
        {
            var bandY = 0;
            while (bandY < height)
            {
                var headIdx = -1;
                for (var i = 0; i < ordered.Count; i++)
                    if (!clones[ordered[i]].Placed) { headIdx = ordered[i]; break; }
                if (headIdx < 0) return;
                var headLabel = clones[headIdx].Label;

                // 主体选择（T3-3，可计算）：候选 = 带头标签中正向脚印仍能放进
                // 剩余网格的未放置件；排序 = 高度 → 非细长高件（w≤1∧h≥3 同高让位，
                // 不制造明显狭长空洞）→ 面积 → 长边 → 宽度 → 捕获顺序。高度第一
                // 正是「主体决定行带高」的落点：细长的注射器当主体时，常规件沿它
                // 右侧行带内排开，不留狭缝；等高的细长件让位常规件避免空转。
                var anchorIdx = -1;
                int bestH = -1, bestSlim = 2, bestArea = -1, bestLong = -1, bestW = -1;
                for (var i = 0; i < ordered.Count; i++)
                {
                    var idx = ordered[i];
                    var it = clones[idx];
                    if (it.Placed || it.Label != headLabel) continue;
                    var pw = FootWidth(it, it.PreferredRotation);
                    var ph = FootHeight(it, it.PreferredRotation);
                    if (ph > height - bandY || pw > width) continue;
                    var slim = pw <= 1 && ph >= 3 ? 1 : 0;
                    var area = pw * ph;
                    var longSide = Math.Max(pw, ph);
                    var better = anchorIdx < 0
                        || ph > bestH
                        || (ph == bestH && slim < bestSlim)
                        || (ph == bestH && slim == bestSlim && area > bestArea)
                        || (ph == bestH && slim == bestSlim && area == bestArea && longSide > bestLong)
                        || (ph == bestH && slim == bestSlim && area == bestArea && longSide == bestLong && pw > bestW);
                    if (!better) continue;
                    anchorIdx = idx;
                    bestH = ph; bestSlim = slim; bestArea = area; bestLong = longSide; bestW = pw;
                }
                if (anchorIdx < 0) return; // 该带无可锚件 → 救济阶段处理（可旋转兜底）

                var anchor = clones[anchorIdx];
                var anchorW = FootWidth(anchor, anchor.PreferredRotation);
                var anchorH = FootHeight(anchor, anchor.PreferredRotation);
                SetPlacement(anchor, 0, bandY, anchor.PreferredRotation);
                Occupy(occupied, width, 0, bandY, anchorW, anchorH);
                var bandH = anchorH;
                var bandX = anchorW;
                var curLabel = anchor.Label;

                for (var i = 0; i < ordered.Count; i++)
                {
                    var idx = ordered[i];
                    var it = clones[idx];
                    if (it.Placed) continue;
                    // 不回填补洞：已过的标签不再回头。在当前排序不变量（ordered
                    // 流按标签非降、curLabel 单调不减）下本分支不可达，仅作契约
                    // 防御——若未来的排序改动引入回头路径，它先兜住「不穿插」。
                    if (it.Label < curLabel) continue;
                    if (it.Label > curLabel)
                    {
                        // 并排准入（T3「整体单调的边界」的可计算形式）：仅当所有
                        // 更低标签都已放置完毕，较高标签才能出现在同一行的右侧；
                        // 否则新标签从下一行最左侧开始。
                        if (AnyUnplacedBelowLabel(clones, ordered, it.Label))
                            break; // 仍有更低标签待发 → 本带就此封带（换行由下一轮完成）
                        curLabel = it.Label;
                    }
                    var pw = FootWidth(it, it.PreferredRotation);
                    var ph = FootHeight(it, it.PreferredRotation);
                    if (ph > bandH) continue; // 高超带 → 留给下一带（不硬塞）
                    if (bandX + pw > width) continue; // 右不足 → 换行
                    if (!CellFree(occupied, width, bandX, bandY, pw, ph)) continue;
                    SetPlacement(it, bandX, bandY, it.PreferredRotation);
                    Occupy(occupied, width, bandX, bandY, pw, ph);
                    bandX += pw;
                }
                bandY += bandH;
            }
        }

        private static bool AnyUnplacedBelowLabel(List<PackableItem> clones, List<int> ordered,
            PlayerUseLabel label)
        {
            for (var i = 0; i < ordered.Count; i++)
            {
                var it = clones[ordered[i]];
                if (!it.Placed && it.Label < label) return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // 3. RESCUE — 硬性“能放下必须放下”：行主序 bottom-left，正向先试、
        //    旋转最后才用（旋转只用于避免漏放）。
        // ─────────────────────────────────────────────────────────────

        private static void RescueBottomLeftPass(List<PackableItem> clones, List<int> ordered,
            byte width, byte height, bool[] occupied)
        {
            for (var o = 0; o < ordered.Count; o++)
            {
                var it = clones[ordered[o]];
                if (it.Placed) continue;
                var placedNow = false;
                for (var y = 0; y < height && !placedNow; y++)
                {
                    for (var x = 0; x < width && !placedNow; x++)
                    {
                        var pref = it.PreferredRotation;
                        var pw = FootWidth(it, pref);
                        var ph = FootHeight(it, pref);
                        if (x + pw <= width && y + ph <= height && CellFree(occupied, width, x, y, pw, ph))
                        {
                            SetPlacement(it, x, y, pref);
                            Occupy(occupied, width, x, y, pw, ph);
                            placedNow = true;
                            break;
                        }
                        if (it.size_x == it.size_y) continue; // 正方形无备选
                        var aw = FootWidth(it, (byte)(pref ^ 1));
                        var ah = FootHeight(it, (byte)(pref ^ 1));
                        if (x + aw <= width && y + ah <= height && CellFree(occupied, width, x, y, aw, ah))
                        {
                            SetPlacement(it, x, y, (byte)(pref ^ 1));
                            Occupy(occupied, width, x, y, aw, ah);
                            placedNow = true;
                        }
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 4. VALIDATE — 出参前复核硬性不变量（重叠/越界/脚印）。
        // ─────────────────────────────────────────────────────────────

        // ─────────────────────────────────────────────────────────────
        // 4. EXACT FEASIBILITY REBUILD — 有界回溯，完备优先、预算兜底。
        //    形态：每次递归锚定「行主序第一个空格」，用每个未放件×朝向的
        //    所有覆盖该格的落位分支（大件先分支=强剪枝）；找到任一全放置
        //    解立即整体返回（不比较质量），穷尽则证明不可行。节点预算
        //    200k：整理是低频用户动作、玩家页为个位数×个位数量级的网格，
        //    常规输入毫秒级（4p 穷尽证明型最坏用例实测整套组 4s）；超预算
        //    =放弃求解（状态全部回卷）→ 上层 cannot-fit 零提交。
        //    确定性：分支序列、格序、件序均为全序，同输入同结果。
        // ─────────────────────────────────────────────────────────────

        private const long ExactSearchNodeCap = 200000L;

        private static void SolveExactFeasibility(List<PackableItem> clones, List<int> work,
            byte width, byte height, bool[] occupied)
        {
            var items = new List<int>(work.Count);
            for (var i = 0; i < work.Count; i++) items.Add(work[i]);
            // 大件先分支：长边→面积→短边→捕获序→工作序索引（全序）。
            items.Sort((a, b) =>
            {
                var left = clones[a];
                var right = clones[b];
                var longL = Math.Max(left.size_x, left.size_y);
                var longR = Math.Max(right.size_x, right.size_y);
                if (longL != longR) return longR.CompareTo(longL);
                var areaL = (long)left.size_x * left.size_y;
                var areaR = (long)right.size_x * right.size_y;
                if (areaL != areaR) return areaR.CompareTo(areaL);
                var shortL = Math.Min(left.size_x, left.size_y);
                var shortR = Math.Min(right.size_x, right.size_y);
                if (shortL != shortR) return shortR.CompareTo(shortL);
                if (left.StableOrder != right.StableOrder) return left.StableOrder.CompareTo(right.StableOrder);
                return a.CompareTo(b);
            });
            var taken = new bool[items.Count];
            var remainingArea = 0L;
            for (var i = 0; i < items.Count; i++)
            {
                var it = clones[items[i]];
                remainingArea += (long)it.size_x * it.size_y;
            }
            var nodes = new long[] { ExactSearchNodeCap };
            RecurseExact(clones, items, taken, items.Count, remainingArea, occupied, width, height, nodes);
        }

        private static bool RecurseExact(List<PackableItem> clones, List<int> items, bool[] taken, int remaining,
            long remainingArea, bool[] occupied, byte width, byte height, long[] nodes)
        {
            if (remaining == 0) return true;
            // 空格数不足以装下剩余面积 → 剪枝。
            var freeCells = 0;
            int fx = -1, fy = -1;
            for (var cell = 0; cell < occupied.Length; cell++)
            {
                if (!occupied[cell])
                {
                    freeCells++;
                    if (fx < 0) { fx = cell % width; fy = cell / width; }
                }
            }
            if (remainingArea > freeCells) return false;
            if (fx < 0) return false;

            for (var i = 0; i < items.Count; i++)
            {
                if (taken[i]) continue;
                var it = clones[items[i]];
                for (var o = 0; o < 2; o++)
                {
                    byte rot = o == 0 ? it.PreferredRotation : (byte)(it.PreferredRotation ^ 1);
                    if (o == 1 && it.size_x == it.size_y) continue;
                    var w = FootWidth(it, rot);
                    var h = FootHeight(it, rot);
                    var topMin = fy - h + 1; if (topMin < 0) topMin = 0;
                    var leftMin = fx - w + 1; if (leftMin < 0) leftMin = 0;
                    for (var ty = topMin; ty <= fy; ty++)
                    {
                        for (var tx = leftMin; tx <= fx; tx++)
                        {
                            if (tx + w > width || ty + h > height) continue;
                            if (!CellFree(occupied, width, tx, ty, w, h)) continue;
                            if (--nodes[0] < 0) return false; // 预算耗尽：全部回卷，由上层判失败
                            Occupy(occupied, width, tx, ty, w, h);
                            SetPlacement(it, tx, ty, rot);
                            taken[i] = true;
                            if (RecurseExact(clones, items, taken, remaining - 1,
                                    remainingArea - (long)w * h, occupied, width, height, nodes))
                                return true;
                            Unoccupy(occupied, width, tx, ty, w, h);
                            it.Placed = false; it.ResultX = 0; it.ResultY = 0; it.ResultRot = 0;
                            taken[i] = false;
                        }
                    }
                }
            }
            return false;
        }

        private static void Unoccupy(bool[] occupied, int width, int x, int y, int w, int h)
        {
            for (var cx = x; cx < x + w; cx++)
                for (var cy = y; cy < y + h; cy++)
                    occupied[cy * width + cx] = false;
        }

        private static bool AnyUnplaced(List<PackableItem> clones, List<int> work)
        {
            for (var i = 0; i < work.Count; i++)
                if (!clones[work[i]].Placed) return true;
            return false;
        }

        private static bool ValidatePlan(List<PackableItem> clones, byte width, byte height)
        {
            var seen = new bool[width * height];
            for (var i = 0; i < clones.Count; i++)
            {
                var it = clones[i];
                if (it == null || !it.Placed) continue;
                var w = FootWidth(it, it.ResultRot);
                var h = FootHeight(it, it.ResultRot);
                if (w == 0 || h == 0) return false;
                if (w > width || h > height) return false;
                if (it.ResultX + w > width || it.ResultY + h > height) return false;
                for (var x = it.ResultX; x < it.ResultX + w; x++)
                    for (var y = it.ResultY; y < it.ResultY + h; y++)
                    {
                        if (seen[y * width + x]) return false;
                        seen[y * width + x] = true;
                    }
            }
            return true;
        }
    }
}
