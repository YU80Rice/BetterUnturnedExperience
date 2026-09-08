using System;
using System.Collections.Generic;
using System.Linq;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// 弹药压弹服务：把玩家背包内同类弹匣的子弹合并 + 从弹药箱给未满弹匣压弹。
    /// (DEV-V2-22 自旧独立插件原样迁移——算法、事务结构与行为保持；仅日志缝
    /// 换为 LirRuntime、命名空间并入 BUE 单 DLL。旧实机测试桩 ExportCanonicalSha256
    /// 按「夹具不进玩家 DLL」规则不迁。作者:YU80Rice,MIT;署名见仓库第三方文档。)
    ///
    /// 双端适配网络同步路径：
    ///   - 服务器端调用：items.removeItem + items.addItem 触发 onItemRemoved/onItemAdded，
    ///     PlayerInventory 订阅这些事件做原生网络同步 -> 客机端自动收到 inventory 更新包
    ///   - sendUpdateAmount 同样触发 onItemUpdated -> 网络同步
    ///
    /// 严格遵循迁移前三大铁规：
    ///   1) 检测到蓝图自动填入，无蓝图直接跳过（仅处理含 FillTargetItem 蓝图的弹匣）
    ///   2) 仅处理背包内容（page 2..6 = SLOTS..PANTS），跳过装备槽/储物箱/区域
    ///   3) 功能 A = 整理后自动压弹（TidyCompleted 事件消费）；功能 B = 双击换弹键
    ///
    /// 事务化处理：扫描 -> 快照 -> 指纹复核 -> Apply -> 失败回滚
    ///   - InventorySnapshot：捕获 page 2..6 的 id/amount/quality/state/x/y/rot
    ///   - RepackPlan/MergePlan：纯计算结果，禁止在 Build 期间触碰 Inventory API 写入
    ///   - 失败必须 RestoreExact，不得半提交
    /// </summary>
    internal static class AmmoRepackService
    {
        /// <summary>
        /// 功能 A 事务化入口（TidyCompleted 消费链经权威调用）。
        ///
        /// 流程：
        ///   1) before = InventorySnapshot.CapturePages(SLOTS..PANTS)
        ///   2) plan = BuildMergePlan(before)（纯计算，禁止 Inventory API 写入）
        ///   3) if (plan.IsEmpty) return NoChange
        ///   4) current = InventorySnapshot.CapturePages(SLOTS..PANTS)
        ///   5) if (!before.Equals(current)) return AbortedStateDrift
        ///   6) try { CommitMerge(inv, plan, before); return Committed }
        ///      catch { RestoreAndVerify; 失败返回 RolledBack/RestoreFailed }
        /// </summary>
        internal static MergeCommitResult TryMergeSameIdMagazinesTransactional(Player player)
        {
            if (player == null)
            {
                LirRuntime.LogDiagnostic("[MergeA] player == null，跳过");
                return MergeCommitResult.NoChange();
            }
            PlayerInventory inv = player.inventory;
            if (inv == null)
            {
                LirRuntime.LogDiagnostic("[MergeA] player.inventory == null，跳过");
                return MergeCommitResult.NoChange();
            }

            InventorySnapshot before = InventorySnapshot.CapturePages(inv,
                PlayerInventory.SLOTS, PlayerInventory.PANTS);

            MergePlan plan = BuildMergePlan(before);
            if (plan.IsEmpty)
            {
                return MergeCommitResult.NoChange();
            }

            // 指纹复核：Build 期间库存布局/数量若发生漂移，中止事务不入库
            InventorySnapshot current = InventorySnapshot.CapturePages(inv,
                PlayerInventory.SLOTS, PlayerInventory.PANTS);
            if (!before.Equals(current))
            {
                LirRuntime.LogDiagnostic("[MergeA] 库存布局/数量在扫描期间漂移，事务中止");
                return MergeCommitResult.AbortedStateDrift();
            }

            try
            {
                CommitMerge(inv, plan, before);
                return MergeCommitResult.Committed(plan.TotalMerged);
            }
            catch (Exception applyEx)
            {
                LirRuntime.LogError("[MergeA] CommitMerge rejected: " + applyEx);
                bool ok = RestoreAndVerify(before, inv, "MergeA");
                return ok ? MergeCommitResult.RolledBack() : MergeCommitResult.RestoreFailed();
            }
        }

        /// <summary>
        /// 功能 B 事务化入口（双击换弹键链经权威调用）。
        ///
        /// 流程与功能 A 同构：快照 -> 纯计算 BuildRepackPlan -> 指纹复核 ->
        /// CommitRepack -> 失败 RestoreAndVerify。
        /// </summary>
        internal static RepackCommitResult TryRepackTransactional(Player player)
        {
            if (player == null)
            {
                LirRuntime.LogDiagnostic("[RepackB] player == null，跳过");
                return RepackCommitResult.NoChange();
            }
            PlayerInventory inv = player.inventory;
            if (inv == null)
            {
                LirRuntime.LogDiagnostic("[RepackB] player.inventory == null，跳过");
                return RepackCommitResult.NoChange();
            }

            InventorySnapshot before = InventorySnapshot.CapturePages(inv,
                PlayerInventory.SLOTS, PlayerInventory.PANTS);

            RepackPlan plan = BuildRepackPlan(player);
            if (plan.TotalTransferred == 0)
            {
                return RepackCommitResult.NoChange();
            }

            // 指纹复核：Build 期间库存布局/数量若发生漂移，中止事务不入库
            InventorySnapshot current = InventorySnapshot.CapturePages(inv,
                PlayerInventory.SLOTS, PlayerInventory.PANTS);
            if (!before.Equals(current))
            {
                LirRuntime.LogDiagnostic("[RepackB] 库存布局/数量在扫描期间漂移，事务中止");
                return RepackCommitResult.AbortedStateDrift();
            }

            try
            {
                CommitRepack(inv, plan, before);
                return RepackCommitResult.Committed(plan.TotalTransferred);
            }
            catch (Exception applyEx)
            {
                LirRuntime.LogError("[RepackB] CommitRepack rejected: " + applyEx);
                bool ok = RestoreAndVerify(before, inv, "RepackB");
                return ok ? RepackCommitResult.RolledBack() : RepackCommitResult.RestoreFailed();
            }
        }

        /// <summary>
        /// 恢复并验证：RestoreExact 后重抓快照，与 before 比较。
        /// 返回 true = 恢复成功且指纹一致（调用方应返回 RolledBack）；
        /// 返回 false = 恢复失败或指纹不一致（调用方应返回 RestoreFailed，半提交风险）。
        /// 供 Repack 与 Merge 共享。
        /// </summary>
        private static bool RestoreAndVerify(
            InventorySnapshot before, PlayerInventory inv, string tag)
        {
            try
            {
                before.RestoreExact(inv);
                InventorySnapshot restored = InventorySnapshot.CapturePages(inv,
                    PlayerInventory.SLOTS, PlayerInventory.PANTS);
                if (!before.Equals(restored))
                {
                    LirRuntime.LogError("[" + tag + "] RestoreExact 完成但恢复后指纹与 before 不一致（半提交风险）");
                    return false;
                }
                LirRuntime.LogDiagnostic("[" + tag + "] RestoreExact 成功，事务已回滚");
                return true;
            }
            catch (Exception restoreEx)
            {
                LirRuntime.LogError("[" + tag + "] RestoreExact crash (半提交风险): " + restoreEx);
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 事务化内部：纯计算 BuildRepackPlan + 写入 CommitRepack
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 纯计算：扫描 page 2..6，构建压弹计划（amountUpdates + deletions + totalTransferred）。
        /// 禁止在此方法内调用任何 Inventory API 写入（sendUpdateAmount / removeItem / addItem）。
        /// </summary>
        private static RepackPlan BuildRepackPlan(Player player)
        {
            var plan = new RepackPlan();

            if (player == null) return plan;
            PlayerInventory inv = player.inventory;
            if (inv == null) return plan;

            // 1) 扫描 page 2..6，收集未满弹匣 + 所有可堆叠物品（潜在弹药源）
            var unfilledMags = new List<MagRef>();
            var ammoBoxMap = new Dictionary<ushort, List<AmmoBoxRef>>();

            for (byte page = PlayerInventory.SLOTS; page <= PlayerInventory.PANTS; page++)
            {
                Items items = inv.items[page];
                if (items == null) continue;
                if (items.width == 0 || items.height == 0) continue;

                byte count = items.getItemCount();
                for (byte i = 0; i < count; i++)
                {
                    ItemJar jar = items.getItem(i);
                    if (jar?.item == null) continue;

                    ItemAsset asset = jar.item.GetAsset<ItemAsset>();
                    if (asset == null) continue;

                    // 构建 preimage（含 id/quality/state），供 CommitRepack 与 BuildExpectedAfter 使用
                    var preimage = new SlotPreimage
                    {
                        page = page,
                        x = jar.x,
                        y = jar.y,
                        id = jar.item.id,
                        amount = jar.item.amount,
                        quality = jar.item.quality,
                        state = jar.item.state == null ? null : (byte[])jar.item.state.Clone(),
                    };

                    if (asset is ItemMagazineAsset magAsset)
                    {
                        if (magAsset.MaxAmountAsByte == 0) continue;
                        if (jar.item.amount >= magAsset.MaxAmountAsByte) continue;

                        // 主路径：蓝图关联匹配 - 收集所有 FillTargetItem 蓝图的 supplies ID
                        List<ushort> compatibleIds = CollectCompatibleAmmoIds(magAsset);

                        unfilledMags.Add(new MagRef
                        {
                            page = page,
                            x = jar.x,
                            y = jar.y,
                            currentAmount = jar.item.amount,
                            maxAmount = magAsset.MaxAmountAsByte,
                            compatibleAmmoIds = compatibleIds,
                            magAsset = magAsset,
                            preimage = preimage,
                        });
                    }
                    else
                    {
                        if (jar.item.amount == 0) continue;
                        if (asset.MaxAmountAsByte == 0) continue;

                        if (!ammoBoxMap.TryGetValue(asset.id, out var list))
                        {
                            list = new List<AmmoBoxRef>();
                            ammoBoxMap[asset.id] = list;
                        }
                        list.Add(new AmmoBoxRef
                        {
                            page = page,
                            index = i,
                            x = jar.x,
                            y = jar.y,
                            initialAmount = jar.item.amount,
                            currentAmount = jar.item.amount,
                            shouldDeleteAtZero = asset.ShouldDeleteAtZeroAmount,
                            asset = asset,
                            preimage = preimage,
                        });
                    }
                }
            }

            if (unfilledMags.Count == 0)
            {
                return plan;
            }

            // 2) 为每个未满弹匣查找兼容弹药源并计算转移量
            // 弹药源内层循环只维护工作值；所有弹匣处理完成后统一物化 mutation，
            // 确保每个实际槽位最多生成一条更新或删除操作。
            var amountUpdates = new List<AmountUpdate>();
            var deletions = new List<Deletion>();

            int totalTransferred = 0;

            foreach (var mag in unfilledMags)
            {
                int remaining = mag.maxAmount - mag.currentAmount;
                if (remaining <= 0) continue;

                // 收集所有候选弹药源列表（多 Asset 合并）
                var candidateBoxLists = new List<List<AmmoBoxRef>>();

                // 主路径：蓝图关联匹配
                if (mag.compatibleAmmoIds.Count > 0)
                {
                    foreach (ushort ammoId in mag.compatibleAmmoIds)
                    {
                        if (ammoBoxMap.TryGetValue(ammoId, out var list) && list.Count > 0)
                            candidateBoxLists.Add(list);
                    }
                }

                // Fallback：若主路径没找到任何匹配，且弹匣有 calibers，按 caliber 匹配
                if (candidateBoxLists.Count == 0)
                {
                    var fallbackLists = FindCaliberMatch(mag.magAsset, ammoBoxMap);
                    if (fallbackLists.Count > 0)
                    {
                        candidateBoxLists = fallbackLists;
                    }
                }

                if (candidateBoxLists.Count == 0)
                {
                    continue;
                }

                int newMagAmount = mag.currentAmount;

                foreach (var boxList in candidateBoxLists)
                {
                    if (remaining <= 0) break;

                    for (int bi = 0; bi < boxList.Count; bi++)
                    {
                        if (remaining <= 0) break;
                        AmmoBoxRef box = boxList[bi];
                        if (box.currentAmount == 0) continue;

                        int transfer = Math.Min(remaining, box.currentAmount);
                        if (transfer <= 0) continue;

                        newMagAmount += transfer;
                        remaining -= transfer;

                        int newBoxAmount = box.currentAmount - transfer;
                        if (newBoxAmount <= 0 && box.shouldDeleteAtZero)
                        {
                            box.currentAmount = 0;
                            box.deleteAtCommit = true;
                        }
                        else
                        {
                            box.currentAmount = (byte)Math.Max(1, newBoxAmount);
                            box.deleteAtCommit = false;
                        }

                        totalTransferred += transfer;
                    }
                }

                if (newMagAmount != mag.currentAmount)
                {
                    amountUpdates.Add(new AmountUpdate
                    {
                        page = mag.page,
                        x = mag.x,
                        y = mag.y,
                        newAmount = (byte)newMagAmount,
                        expectedBefore = mag.preimage,
                    });
                }
            }

            // 3) 所有弹匣处理完后，唯一化物化弹药源 mutation（每个弹药源最多一条 update 或 deletion）
            plan.AmountUpdates = amountUpdates;
            plan.Deletions = deletions;
            MaterializeAmmoSourceMutations(ammoBoxMap, plan);
            plan.TotalTransferred = totalTransferred;
            return plan;
        }

        /// <summary>
        /// 唯一化物化弹药源 mutation：遍历所有 AmmoBoxRef，按 SlotKey 去重，
        /// 为每个有变化的弹药源生成一条 AmountUpdate 或一条 Deletion。
        /// 同一 SlotKey 出现两次 -> throw InvalidOperationException（防御性，理论不可达）。
        /// </summary>
        private static void MaterializeAmmoSourceMutations(
            Dictionary<ushort, List<AmmoBoxRef>> ammoBoxMap, RepackPlan plan)
        {
            if (ammoBoxMap == null) return;
            if (plan.AmountUpdates == null) plan.AmountUpdates = new List<AmountUpdate>();
            if (plan.Deletions == null) plan.Deletions = new List<Deletion>();

            var seen = new HashSet<SlotKey>();
            foreach (var kvp in ammoBoxMap)
            {
                List<AmmoBoxRef> boxes = kvp.Value;
                if (boxes == null) continue;
                foreach (AmmoBoxRef box in boxes)
                {
                    if (box == null) continue;
                    var key = new SlotKey(box.page, box.x, box.y);
                    if (!seen.Add(key))
                        throw new InvalidOperationException(
                            $"Duplicate ammo source slot at page={box.page} x={box.x} y={box.y}");

                    if (box.deleteAtCommit)
                    {
                        plan.Deletions.Add(new Deletion
                        {
                            page = box.page,
                            x = box.x,
                            y = box.y,
                            expectedBefore = box.preimage,
                        });
                    }
                    else if (box.currentAmount != box.initialAmount)
                    {
                        plan.AmountUpdates.Add(new AmountUpdate
                        {
                            page = box.page,
                            x = box.x,
                            y = box.y,
                            newAmount = box.currentAmount,
                            expectedBefore = box.preimage,
                        });
                    }
                    // else: 无变化，跳过
                }
            }
        }

        /// <summary>
        /// 校验计划无重复目标和 update/delete 重叠。fail-closed。
        /// 任一重复或重叠均抛 PostCommitMismatchException，由 Commit 调用方 catch
        /// 触发 RestoreAndVerify。这是 Build 阶段已出错的防御性断言。
        /// </summary>
        private static void ValidateUniquePlan(RepackPlan plan)
        {
            if (plan == null) return;
            var targets = new HashSet<SlotKey>();
            if (plan.AmountUpdates != null)
            {
                foreach (var op in plan.AmountUpdates)
                {
                    var key = new SlotKey(op.page, op.x, op.y);
                    if (!targets.Add(key))
                        throw new PostCommitMismatchException(
                            $"duplicate amount target at page={op.page} x={op.x} y={op.y}");
                }
            }
            if (plan.Deletions != null)
            {
                foreach (var op in plan.Deletions)
                {
                    var key = new SlotKey(op.page, op.x, op.y);
                    if (!targets.Add(key))
                        throw new PostCommitMismatchException(
                            $"update/delete target overlap at page={op.page} x={op.x} y={op.y}");
                }
            }
        }

        private static void ValidateUniquePlan(MergePlan plan)
        {
            if (plan == null) return;
            var targets = new HashSet<SlotKey>();
            if (plan.AmountUpdates != null)
            {
                foreach (var op in plan.AmountUpdates)
                {
                    var key = new SlotKey(op.page, op.x, op.y);
                    if (!targets.Add(key))
                        throw new PostCommitMismatchException(
                            $"duplicate merge amount target at page={op.page} x={op.x} y={op.y}");
                }
            }
            if (plan.Deletions != null)
            {
                foreach (var op in plan.Deletions)
                {
                    var key = new SlotKey(op.page, op.x, op.y);
                    if (!targets.Add(key))
                        throw new PostCommitMismatchException(
                            $"merge update/delete target overlap at page={op.page} x={op.x} y={op.y}");
                }
            }
        }

        /// <summary>
        /// 提交压弹计划：每步 preimage 校验 + 写入 + 写入后校验 + 提交后指纹比对。
        /// 任一步骤失败抛 PostCommitMismatchException，由调用方 catch 调 RestoreAndVerify。
        /// 提交前先校验唯一计划；删除按降序遍历，每次重新解析索引并验证结果。
        /// </summary>
        private static void CommitRepack(PlayerInventory inv, RepackPlan plan,
            InventorySnapshot before)
        {
            if (inv == null) throw new ArgumentNullException(nameof(inv));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            // 0) fail-closed：任何物品最多一个 mutation（防止 Build 阶段重复 preimage）
            ValidateUniquePlan(plan);

            // 1) 纯内存计算 expectedAfter（不调用 Inventory API）
            InventorySnapshot expectedAfter = before.BuildExpectedAfter(plan);

            // 2) amount 更新：preimage 校验 -> 写入 -> 写入后 amount 校验
            if (plan.AmountUpdates != null)
            {
                foreach (var op in plan.AmountUpdates)
                {
                    RequireExactSlot(inv, op.expectedBefore);
                    inv.sendUpdateAmount(op.page, op.x, op.y, op.newAmount);
                    RequireAmount(inv, op.page, op.x, op.y, op.newAmount);
                }
            }

            // 3) 删除：降序排序 -> 每次重新解析索引 -> removeItem -> 验证
            if (plan.Deletions != null && plan.Deletions.Count > 0)
            {
                var orderedDeletions = plan.Deletions
                    .OrderByDescending(d => d.page)
                    .ThenByDescending(d => d.y)
                    .ThenByDescending(d => d.x)
                    .ToList();

                foreach (var deletion in orderedDeletions)
                {
                    byte currentIndex = FindExactSlotOrThrow(inv, deletion.expectedBefore);
                    Items items = inv.items[deletion.page];
                    if (items == null)
                        throw new PostCommitMismatchException(
                            $"page {deletion.page} null before removeItem");
                    if (currentIndex >= items.getItemCount())
                        throw new PostCommitMismatchException(
                            $"page {deletion.page} index {currentIndex} out of range before removeItem");
                    items.removeItem(currentIndex);
                    if (FindExactSlot(inv, deletion.expectedBefore) != byte.MaxValue)
                        throw new PostCommitMismatchException(
                            $"removeItem did not remove exact preimage at page={deletion.page} x={deletion.x} y={deletion.y}");
                }
            }

            // 4) 提交后指纹比对：actual == expectedAfter
            InventorySnapshot actual = InventorySnapshot.CapturePages(inv,
                PlayerInventory.SLOTS, PlayerInventory.PANTS);
            if (!expectedAfter.Equals(actual))
                throw new PostCommitMismatchException(
                    "post-commit inventory fingerprint mismatch (RepackPlan)");
        }

        /// <summary>提交合并计划：与 CommitRepack 同模式（preimage/写入后/提交后指纹三重校验）。</summary>
        private static void CommitMerge(PlayerInventory inv, MergePlan plan,
            InventorySnapshot before)
        {
            if (inv == null) throw new ArgumentNullException(nameof(inv));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            ValidateUniquePlan(plan);

            // 1) 纯内存计算 expectedAfter
            InventorySnapshot expectedAfter = before.BuildExpectedAfter(plan);

            // 2) amount 更新：preimage 校验 -> 写入 -> 写入后 amount 校验
            if (plan.AmountUpdates != null)
            {
                foreach (var op in plan.AmountUpdates)
                {
                    RequireExactSlot(inv, op.expectedBefore);
                    inv.sendUpdateAmount(op.page, op.x, op.y, op.newAmount);
                    RequireAmount(inv, op.page, op.x, op.y, op.newAmount);
                }
            }

            // 3) 删除：降序排序 -> 每次重新解析当前索引，不缓存裸 index
            if (plan.Deletions != null && plan.Deletions.Count > 0)
            {
                var orderedDeletions = plan.Deletions
                    .OrderByDescending(d => d.page)
                    .ThenByDescending(d => d.y)
                    .ThenByDescending(d => d.x)
                    .ToList();

                foreach (var deletion in orderedDeletions)
                {
                    byte currentIndex = FindExactSlotOrThrow(inv, deletion.expectedBefore);
                    Items items = inv.items[deletion.page];
                    if (items == null)
                        throw new PostCommitMismatchException(
                            $"page {deletion.page} null before removeItem");
                    if (currentIndex >= items.getItemCount())
                        throw new PostCommitMismatchException(
                            $"page {deletion.page} index {currentIndex} out of range before removeItem");
                    items.removeItem(currentIndex);
                    if (FindExactSlot(inv, deletion.expectedBefore) != byte.MaxValue)
                        throw new PostCommitMismatchException(
                            $"removeItem did not remove exact preimage at page={deletion.page} x={deletion.x} y={deletion.y}");
                }
            }

            // 4) 提交后指纹比对
            InventorySnapshot actual = InventorySnapshot.CapturePages(inv,
                PlayerInventory.SLOTS, PlayerInventory.PANTS);
            if (!expectedAfter.Equals(actual))
                throw new PostCommitMismatchException(
                    "post-commit inventory fingerprint mismatch (MergePlan)");
        }

        // ─────────────────────────────────────────────────────────────
        // Preimage 校验辅助
        // ─────────────────────────────────────────────────────────────

        /// <summary>验证 inv 中存在与 expected 完全匹配的槽位（page/x/y/id/amount/quality/state）。不匹配或缺失抛 PostCommitMismatchException。</summary>
        private static void RequireExactSlot(PlayerInventory inv, SlotPreimage expected)
        {
            Items items = inv.items[expected.page];
            if (items == null)
                throw new PostCommitMismatchException(
                    $"RequireExactSlot: page {expected.page} null");
            byte count = items.getItemCount();
            for (byte i = 0; i < count; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null) continue;
                if (jar.x != expected.x || jar.y != expected.y) continue;
                if (jar.item.id != expected.id)
                    throw new PostCommitMismatchException(
                        $"RequireExactSlot: id mismatch at {expected.page}/({expected.x},{expected.y}) got {jar.item.id} expected {expected.id}");
                if (jar.item.amount != expected.amount)
                    throw new PostCommitMismatchException(
                        $"RequireExactSlot: amount mismatch at {expected.page}/({expected.x},{expected.y}) got {jar.item.amount} expected {expected.amount}");
                if (jar.item.quality != expected.quality)
                    throw new PostCommitMismatchException(
                        $"RequireExactSlot: quality mismatch at {expected.page}/({expected.x},{expected.y})");
                if (!StateEquals(jar.item.state, expected.state))
                    throw new PostCommitMismatchException(
                        $"RequireExactSlot: state mismatch at {expected.page}/({expected.x},{expected.y})");
                return;  // 找到匹配
            }
            throw new PostCommitMismatchException(
                $"RequireExactSlot: slot {expected.page}/({expected.x},{expected.y}) not found");
        }

        /// <summary>按 preimage 反查槽位索引。缺失或不匹配抛 PostCommitMismatchException。</summary>
        private static byte FindExactSlotOrThrow(PlayerInventory inv, SlotPreimage expected)
        {
            Items items = inv.items[expected.page];
            if (items == null)
                throw new PostCommitMismatchException(
                    $"FindExactSlotOrThrow: page {expected.page} null");
            byte count = items.getItemCount();
            for (byte i = 0; i < count; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null) continue;
                if (jar.x != expected.x || jar.y != expected.y) continue;
                if (jar.item.id != expected.id)
                    throw new PostCommitMismatchException(
                        $"FindExactSlotOrThrow: id mismatch at {expected.page}/({expected.x},{expected.y})");
                if (jar.item.amount != expected.amount)
                    throw new PostCommitMismatchException(
                        $"FindExactSlotOrThrow: amount mismatch at {expected.page}/({expected.x},{expected.y})");
                if (jar.item.quality != expected.quality)
                    throw new PostCommitMismatchException(
                        $"FindExactSlotOrThrow: quality mismatch at {expected.page}/({expected.x},{expected.y})");
                if (!StateEquals(jar.item.state, expected.state))
                    throw new PostCommitMismatchException(
                        $"FindExactSlotOrThrow: state mismatch at {expected.page}/({expected.x},{expected.y})");
                return i;
            }
            throw new PostCommitMismatchException(
                $"FindExactSlotOrThrow: slot {expected.page}/({expected.x},{expected.y}) not found");
        }

        /// <summary>按 preimage 反查槽位索引。缺失或不匹配返回 byte.MaxValue（不抛）。用于删除后验证槽位已空。</summary>
        private static byte FindExactSlot(PlayerInventory inv, SlotPreimage expected)
        {
            Items items = inv.items[expected.page];
            if (items == null) return byte.MaxValue;
            byte count = items.getItemCount();
            for (byte i = 0; i < count; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null) continue;
                if (jar.x != expected.x || jar.y != expected.y) continue;
                if (jar.item.id != expected.id) return byte.MaxValue;
                if (jar.item.amount != expected.amount) return byte.MaxValue;
                if (jar.item.quality != expected.quality) return byte.MaxValue;
                if (!StateEquals(jar.item.state, expected.state)) return byte.MaxValue;
                return i;
            }
            return byte.MaxValue;
        }

        /// <summary>验证 (page,x,y) 槽位的 amount 已更新为 expected。不匹配抛 PostCommitMismatchException。</summary>
        private static void RequireAmount(PlayerInventory inv, byte page, byte x, byte y,
            byte expected)
        {
            Items items = inv.items[page];
            if (items == null)
                throw new PostCommitMismatchException(
                    $"RequireAmount: page {page} null");
            byte count = items.getItemCount();
            for (byte i = 0; i < count; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar == null || jar.item == null) continue;
                if (jar.x != x || jar.y != y) continue;
                if (jar.item.amount != expected)
                    throw new PostCommitMismatchException(
                        $"RequireAmount: amount at {page}/({x},{y}) got {jar.item.amount} expected {expected} (sendUpdateAmount did not land)");
                return;
            }
            throw new PostCommitMismatchException(
                $"RequireAmount: slot {page}/({x},{y}) not found after sendUpdateAmount");
        }

        private static bool StateEquals(byte[] a, byte[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // 蓝图关联匹配辅助
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 收集弹匣所有 FillTargetItem 蓝图的 supplies 物品 ID。
        /// 这些 ID 构成"兼容弹药源 ID 列表"，用于绕过工坊作者乱填/漏填 Caliber 的硬伤。
        /// </summary>
        private static List<ushort> CollectCompatibleAmmoIds(ItemMagazineAsset magAsset)
        {
            var result = new HashSet<ushort>();
            if (magAsset?.blueprints == null) return new List<ushort>(result);

            for (int i = 0; i < magAsset.blueprints.Count; i++)
            {
                Blueprint bp = magAsset.blueprints[i];
                if (bp == null) continue;
                if (bp.Operation != EBlueprintOperation.FillTargetItem) continue;
                if (bp.supplies == null) continue;

                for (int j = 0; j < bp.supplies.Length; j++)
                {
                    BlueprintSupply supply = bp.supplies[j];
                    if (supply == null) continue;
                    ItemAsset supplyAsset = supply.FindItemAsset();
                    if (supplyAsset == null) continue;
                    result.Add(supplyAsset.id);
                }
            }
            return new List<ushort>(result);
        }

        /// <summary>
        /// Fallback：当弹匣无 FillTargetItem 蓝图时，按 calibers 数组交集匹配背包内物品。
        /// 仅对同为 ItemCaliberAsset 的物品生效（普通弹药箱不会触发此路径）。
        /// </summary>
        private static List<List<AmmoBoxRef>> FindCaliberMatch(
            ItemMagazineAsset magAsset,
            Dictionary<ushort, List<AmmoBoxRef>> ammoBoxMap)
        {
            var result = new List<List<AmmoBoxRef>>();
            ushort[] magCalibers = magAsset?.calibers;
            if (magCalibers == null || magCalibers.Length == 0) return result;

            foreach (var kvp in ammoBoxMap)
            {
                List<AmmoBoxRef> boxList = kvp.Value;
                if (boxList == null || boxList.Count == 0) continue;
                ItemAsset firstAsset = boxList[0].asset;
                if (!(firstAsset is ItemCaliberAsset boxCaliberAsset)) continue;
                if (boxCaliberAsset.calibers == null || boxCaliberAsset.calibers.Length == 0) continue;

                bool hasCommon = false;
                foreach (ushort magCal in magCalibers)
                {
                    if (magCal == 0) continue;
                    foreach (ushort boxCal in boxCaliberAsset.calibers)
                    {
                        if (magCal == boxCal) { hasCommon = true; break; }
                    }
                    if (hasCommon) break;
                }
                if (hasCommon) result.Add(boxList);
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // 功能 A 内部实现（事务化：纯计算 BuildMergePlan + 写入 CommitMerge）
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 纯计算：从 InventorySnapshot 构建合并计划。
        /// 禁止在此方法内调用任何 Inventory API 写入（sendUpdateAmount / removeItem / addItem）。
        /// 不持有将被清空的 ItemJar 引用，全部数据从 snapshot 读取。
        /// 每个 operation 携带 expectedBefore preimage，供 CommitMerge 校验。
        /// </summary>
        private static MergePlan BuildMergePlan(InventorySnapshot before)
        {
            var plan = new MergePlan();
            if (before == null) return plan;

            // 1) 解析 snapshot 中所有"含 FillTargetItem 蓝图的弹匣"条目
            var magEntries = new List<MagEntry>();
            for (int i = 0; i < before.Entries.Count; i++)
            {
                var e = before.Entries[i];
                ItemMagazineAsset magAsset = ResolveMagazineAsset(e.id);
                if (magAsset == null) continue;
                if (magAsset.MaxAmountAsByte == 0) continue;
                if (FindFillBlueprint(magAsset) == null) continue;

                magEntries.Add(new MagEntry
                {
                    page = e.page,
                    x = e.x,
                    y = e.y,
                    id = e.id,
                    currentAmount = e.amount,
                    maxAmount = magAsset.MaxAmountAsByte,
                    shouldDeleteAtZero = magAsset.ShouldDeleteAtZeroAmount,
                    preimage = new SlotPreimage
                    {
                        page = e.page,
                        x = e.x,
                        y = e.y,
                        id = e.id,
                        amount = e.amount,
                        quality = e.quality,
                        state = e.state == null ? null : (byte[])e.state.Clone(),
                    },
                });
            }

            if (magEntries.Count == 0) return plan;

            // 2) 按弹匣 asset id 分组
            var magGroups = new Dictionary<ushort, List<MagEntry>>();
            foreach (var mag in magEntries)
            {
                if (!magGroups.TryGetValue(mag.id, out var list))
                {
                    list = new List<MagEntry>();
                    magGroups[mag.id] = list;
                }
                list.Add(mag);
            }

            // 3) 为每组 >= 2 个弹匣计算目标 amount + 删除标记
            var amountUpdates = new List<MergeAmountUpdate>();
            var deletions = new List<MergeDeletion>();
            int totalMerged = 0;

            foreach (var kvp in magGroups)
            {
                var entries = kvp.Value;
                if (entries.Count < 2) continue;

                byte maxAmount = 0;
                int totalBullets = 0;
                foreach (var mag in entries)
                {
                    totalBullets += mag.currentAmount;
                    if (mag.maxAmount > maxAmount)
                        maxAmount = mag.maxAmount;
                }
                if (maxAmount == 0) continue;

                // 按现有 amount 降序排序：最满的优先填满
                entries.Sort((a, b) => b.currentAmount.CompareTo(a.currentAmount));

                int bulletsLeft = totalBullets;
                bool anyChange = false;
                foreach (var mag in entries)
                {
                    byte targetAmount;
                    bool shouldDelete = false;
                    if (bulletsLeft >= maxAmount)
                    {
                        targetAmount = maxAmount;
                        bulletsLeft -= maxAmount;
                    }
                    else if (bulletsLeft > 0)
                    {
                        targetAmount = (byte)bulletsLeft;
                        bulletsLeft = 0;
                    }
                    else
                    {
                        targetAmount = 0;
                        if (mag.shouldDeleteAtZero)
                            shouldDelete = true;
                    }

                    if (shouldDelete)
                    {
                        deletions.Add(new MergeDeletion
                        {
                            page = mag.page,
                            x = mag.x,
                            y = mag.y,
                            expectedBefore = mag.preimage,
                        });
                        anyChange = true;
                    }
                    else if (targetAmount != mag.currentAmount)
                    {
                        amountUpdates.Add(new MergeAmountUpdate
                        {
                            page = mag.page,
                            x = mag.x,
                            y = mag.y,
                            newAmount = targetAmount,
                            expectedBefore = mag.preimage,
                        });
                        anyChange = true;
                    }
                }
                if (anyChange) totalMerged += entries.Count;
            }

            plan.AmountUpdates = amountUpdates;
            plan.Deletions = deletions;
            plan.TotalMerged = totalMerged;
            return plan;
        }

        /// <summary>从 item id 解析 ItemMagazineAsset（不持有 Item 引用）。</summary>
        private static ItemMagazineAsset ResolveMagazineAsset(ushort id)
        {
            Asset asset = Assets.find(EAssetType.ITEM, id);
            return asset as ItemMagazineAsset;
        }

        // ─────────────────────────────────────────────────────────────
        // 蓝图工具
        // ─────────────────────────────────────────────────────────────

        /// <summary>查找弹匣资产的 FillTargetItem 蓝图。返回 null 表示该弹匣不支持被弹药箱填入（功能 A/B 均跳过）。</summary>
        private static Blueprint FindFillBlueprint(ItemMagazineAsset magAsset)
        {
            if (magAsset?.blueprints == null) return null;
            for (int i = 0; i < magAsset.blueprints.Count; i++)
            {
                Blueprint bp = magAsset.blueprints[i];
                if (bp == null) continue;
                if (bp.Operation == EBlueprintOperation.FillTargetItem) return bp;
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────
        // 私有结构体
        // ─────────────────────────────────────────────────────────────

        private struct MagRef
        {
            public byte page;
            public byte x, y;
            public byte currentAmount;
            public byte maxAmount;
            /// <summary>主路径：蓝图关联匹配的兼容弹药源 ID 列表（可能为空）。</summary>
            public List<ushort> compatibleAmmoIds;
            /// <summary>弹匣资产引用，用于 Fallback 路径的 caliber 数组比对。</summary>
            public ItemMagazineAsset magAsset;
            /// <summary>提交前 preimage（含 id/quality/state），用于 CommitRepack 校验与 BuildExpectedAfter。</summary>
            public SlotPreimage preimage;
        }

        private class AmmoBoxRef
        {
            public byte page;
            public byte index;
            public byte x, y;
            /// <summary>扫描时不可变数量。Build 阶段仅递减 currentAmount；Materialize 时与 currentAmount 比较决定是否物化 mutation。</summary>
            public byte initialAmount;
            public byte currentAmount;
            /// <summary>延迟物化标记：Build 阶段消耗至 0 且 shouldDeleteAtZero 时置 true，Materialize 时生成 Deletion。</summary>
            public bool deleteAtCommit;
            public bool shouldDeleteAtZero;
            /// <summary>物品资产引用，用于 Fallback 路径判断是否为 ItemCaliberAsset。</summary>
            public ItemAsset asset;
            /// <summary>提交前 preimage（含 id/quality/state），用于 CommitRepack 校验与 BuildExpectedAfter。</summary>
            public SlotPreimage preimage;
        }

        /// <summary>功能 A 纯计算用：从 InventorySnapshot 解析的弹匣条目（不持有 Item 引用）。</summary>
        private struct MagEntry
        {
            public byte page;
            public byte x, y;
            public ushort id;
            public byte currentAmount;
            public byte maxAmount;
            public bool shouldDeleteAtZero;
            /// <summary>提交前 preimage（含 id/quality/state），供 CommitMerge 校验。</summary>
            public SlotPreimage preimage;
        }
    }

    /// <summary>
    /// 压弹事务结果枚举（功能 B）。
    /// </summary>
    internal enum RepackOutcome
    {
        /// <summary>无可压弹目标（背包内无未满弹匣或无兼容弹药源）。</summary>
        NoChange,
        /// <summary>事务已提交（amount 更新 + 删除已应用）。</summary>
        Committed,
        /// <summary>事务已回滚（Apply 失败，RestoreExact 成功且恢复后指纹与 before 一致）。</summary>
        RolledBack,
        /// <summary>事务已中止（Build 期间库存布局/数量漂移）。</summary>
        AbortedStateDrift,
        /// <summary>恢复失败（RestoreExact 抛异常，或恢复后指纹与 before 不一致；库存可能半提交，调用方必须 Critical 处理）。</summary>
        RestoreFailed,
    }

    /// <summary>压弹事务结果（含转移数量与结果类型，功能 B）。</summary>
    internal struct RepackCommitResult
    {
        internal RepackOutcome Outcome { get; private set; }
        internal int TotalTransferred { get; private set; }

        public static RepackCommitResult NoChange() =>
            new RepackCommitResult { Outcome = RepackOutcome.NoChange, TotalTransferred = 0 };

        public static RepackCommitResult Committed(int totalTransferred) =>
            new RepackCommitResult { Outcome = RepackOutcome.Committed, TotalTransferred = totalTransferred };

        public static RepackCommitResult RolledBack() =>
            new RepackCommitResult { Outcome = RepackOutcome.RolledBack, TotalTransferred = 0 };

        public static RepackCommitResult AbortedStateDrift() =>
            new RepackCommitResult { Outcome = RepackOutcome.AbortedStateDrift, TotalTransferred = 0 };

        public static RepackCommitResult RestoreFailed() =>
            new RepackCommitResult { Outcome = RepackOutcome.RestoreFailed, TotalTransferred = 0 };
    }

    /// <summary>
    /// 合并事务结果枚举（功能 A，与 RepackOutcome 对齐但独立枚举以区分调用路径）。
    /// </summary>
    internal enum MergeOutcome
    {
        /// <summary>无可合并目标（背包内无 >= 2 个同 ID 弹匣）。</summary>
        NoChange,
        /// <summary>事务已提交（amount 更新 + 删除已应用且通过提交后指纹比对）。</summary>
        Committed,
        /// <summary>事务已回滚（Apply 失败，RestoreExact 成功且恢复后指纹与 before 一致）。</summary>
        RolledBack,
        /// <summary>事务已中止（Build 期间库存布局/数量漂移）。</summary>
        AbortedStateDrift,
        /// <summary>恢复失败（RestoreExact 抛异常，或恢复后指纹与 before 不一致；库存可能半提交，调用方必须 Critical 处理）。</summary>
        RestoreFailed,
    }

    /// <summary>合并事务结果（含合并弹匣数与结果类型，功能 A）。</summary>
    internal struct MergeCommitResult
    {
        internal MergeOutcome Outcome { get; private set; }
        internal int TotalMerged { get; private set; }

        public static MergeCommitResult NoChange() =>
            new MergeCommitResult { Outcome = MergeOutcome.NoChange, TotalMerged = 0 };

        public static MergeCommitResult Committed(int totalMerged) =>
            new MergeCommitResult { Outcome = MergeOutcome.Committed, TotalMerged = totalMerged };

        public static MergeCommitResult RolledBack() =>
            new MergeCommitResult { Outcome = MergeOutcome.RolledBack, TotalMerged = 0 };

        public static MergeCommitResult AbortedStateDrift() =>
            new MergeCommitResult { Outcome = MergeOutcome.AbortedStateDrift, TotalMerged = 0 };

        public static MergeCommitResult RestoreFailed() =>
            new MergeCommitResult { Outcome = MergeOutcome.RestoreFailed, TotalMerged = 0 };
    }

    /// <summary>
    /// 压弹计划（纯计算结果，功能 B）。
    /// Commit 阶段必须按 AmountUpdates -> Deletions 顺序写入，每步做 preimage 校验 + 提交后指纹比对。
    /// </summary>
    internal class RepackPlan
    {
        public List<AmountUpdate> AmountUpdates;
        public List<Deletion> Deletions;
        public int TotalTransferred;
    }

    /// <summary>RepackPlan 的 amount 更新操作。expectedBefore 携带完整 preimage 用于提交前/后校验。</summary>
    internal struct AmountUpdate
    {
        public byte page;
        public byte x, y;
        public byte newAmount;
        public SlotPreimage expectedBefore;
    }

    /// <summary>RepackPlan 的删除操作。expectedBefore 携带完整 preimage。</summary>
    internal struct Deletion
    {
        public byte page;
        public byte x, y;
        public SlotPreimage expectedBefore;
    }

    /// <summary>
    /// 合并计划（纯计算结果，功能 A，从 InventorySnapshot 构建）。
    /// Commit 阶段必须按 AmountUpdates -> Deletions 顺序写入，每步做 preimage 校验 + 提交后指纹比对。
    /// </summary>
    internal class MergePlan
    {
        public List<MergeAmountUpdate> AmountUpdates;
        public List<MergeDeletion> Deletions;
        public int TotalMerged;

        public bool IsEmpty =>
            (AmountUpdates == null || AmountUpdates.Count == 0) &&
            (Deletions == null || Deletions.Count == 0);
    }

    /// <summary>MergePlan 的 amount 更新操作。expectedBefore 携带完整 preimage。</summary>
    internal struct MergeAmountUpdate
    {
        public byte page;
        public byte x, y;
        public byte newAmount;
        public SlotPreimage expectedBefore;
    }

    /// <summary>MergePlan 的删除操作。expectedBefore 携带完整 preimage。</summary>
    internal struct MergeDeletion
    {
        public byte page;
        public byte x, y;
        public SlotPreimage expectedBefore;
    }

    /// <summary>
    /// 槽位 preimage：提交前对目标槽位的完整状态记录（page/x/y/id/amount/quality/state）。
    /// 用于 Commit 阶段验证目标槽位未被外部修改，以及 BuildExpectedAfter 纯内存计算。
    /// </summary>
    internal struct SlotPreimage
    {
        public byte page;
        public byte x, y;
        public ushort id;
        public byte amount;
        public byte quality;
        public byte[] state;
    }

    /// <summary>
    /// 槽位唯一键（page/x/y）：mutation 去重与提交前 fail-closed 校验用。
    /// </summary>
    internal struct SlotKey
    {
        public byte page;
        public byte x;
        public byte y;

        public SlotKey(byte page, byte x, byte y)
        {
            this.page = page;
            this.x = x;
            this.y = y;
        }

        public override int GetHashCode() => (page << 16) | (x << 8) | y;

        public override bool Equals(object obj) =>
            obj is SlotKey other && other.page == page && other.x == x && other.y == y;
    }

    /// <summary>
    /// 提交后指纹不匹配异常：Apply 阶段任一步骤 preimage 校验失败、写入未落地、
    /// 或提交后 actual != expectedAfter 时抛出。调用方 catch 后调 RestoreAndVerify。
    /// </summary>
    internal sealed class PostCommitMismatchException : Exception
    {
        public PostCommitMismatchException(string message) : base(message) { }
        public PostCommitMismatchException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// 库存快照（page 范围内的 id/amount/quality/state/x/y/rot）。
    /// 用于事务前指纹复核与失败回滚。
    /// </summary>
    internal sealed class InventorySnapshot
    {
        private readonly List<SnapshotEntry> _entries = new List<SnapshotEntry>();

        internal struct SnapshotEntry
        {
            public byte page;
            public byte x, y, rot;
            public ushort id;
            public byte amount;
            public byte quality;
            public byte[] state;
        }

        /// <summary>暴露 entries 给 BuildMergePlan 做纯计算（不得用于写入）。</summary>
        internal IReadOnlyList<SnapshotEntry> Entries => _entries;

        /// <summary>捕获 page 范围 [fromPage..toPage] 内所有 jar 的指纹。主线程调用。</summary>
        internal static InventorySnapshot CapturePages(PlayerInventory inv, byte fromPage, byte toPage)
        {
            var snap = new InventorySnapshot();
            if (inv == null) return snap;

            for (byte page = fromPage; page <= toPage; page++)
            {
                Items items = inv.items[page];
                if (items == null) continue;

                byte count = items.getItemCount();
                for (byte i = 0; i < count; i++)
                {
                    ItemJar jar = items.getItem(i);
                    if (jar?.item == null) continue;

                    var entry = new SnapshotEntry
                    {
                        page = page,
                        x = jar.x,
                        y = jar.y,
                        rot = jar.rot,
                        id = jar.item.id,
                        amount = jar.item.amount,
                        quality = jar.item.quality,
                        state = jar.item.state == null ? null : (byte[])jar.item.state.Clone(),
                    };
                    snap._entries.Add(entry);
                }
            }

            snap._entries.Sort(EntryCompare);
            return snap;
        }

        /// <summary>规范化比较：必须 page+x+y+rot+id+amount+quality+state 全一致。</summary>
        internal bool Equals(InventorySnapshot other)
        {
            if (other == null) return false;
            if (_entries.Count != other._entries.Count) return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                SnapshotEntry a = _entries[i];
                SnapshotEntry b = other._entries[i];
                if (a.page != b.page) return false;
                if (a.x != b.x) return false;
                if (a.y != b.y) return false;
                if (a.rot != b.rot) return false;
                if (a.id != b.id) return false;
                if (a.amount != b.amount) return false;
                if (a.quality != b.quality) return false;
                if (!StateEquals(a.state, b.state)) return false;
            }
            return true;
        }

        /// <summary>
        /// 纯内存计算：基于当前快照 + RepackPlan 构建提交后预期快照。
        /// 不调用任何 Inventory API。供 CommitRepack 提交后比对。
        /// 缺失或重复目标均 fail-closed 抛异常。
        /// </summary>
        internal InventorySnapshot BuildExpectedAfter(RepackPlan plan)
        {
            var result = new InventorySnapshot();
            // 复制当前 entries
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                result._entries.Add(new SnapshotEntry
                {
                    page = e.page,
                    x = e.x,
                    y = e.y,
                    rot = e.rot,
                    id = e.id,
                    amount = e.amount,
                    quality = e.quality,
                    state = e.state == null ? null : (byte[])e.state.Clone(),
                });
            }

            // 应用 amount 更新：缺失或重复目标均抛异常（fail-closed）
            if (plan?.AmountUpdates != null)
            {
                var seenSlots = new HashSet<SlotKey>();
                foreach (var op in plan.AmountUpdates)
                {
                    var key = new SlotKey(op.page, op.x, op.y);
                    if (!seenSlots.Add(key))
                        throw new PostCommitMismatchException(
                            $"BuildExpectedAfter(RepackPlan): duplicate amount target at page={op.page} x={op.x} y={op.y}");

                    int foundAt = -1;
                    for (int i = 0; i < result._entries.Count; i++)
                    {
                        var e = result._entries[i];
                        if (e.page == op.page && e.x == op.x && e.y == op.y)
                        {
                            foundAt = i;
                            break;
                        }
                    }
                    if (foundAt < 0)
                        throw new PostCommitMismatchException(
                            $"BuildExpectedAfter(RepackPlan): amount target not found page={op.page} x={op.x} y={op.y}");
                    var entry = result._entries[foundAt];
                    entry.amount = op.newAmount;
                    result._entries[foundAt] = entry;
                }
            }

            // 应用删除：缺失或重复目标均抛异常（fail-closed）
            if (plan?.Deletions != null && plan.Deletions.Count > 0)
            {
                var seenDelSlots = new HashSet<SlotKey>();
                var removeSet = new HashSet<int>();
                for (int di = 0; di < plan.Deletions.Count; di++)
                {
                    var del = plan.Deletions[di];
                    var key = new SlotKey(del.page, del.x, del.y);
                    if (!seenDelSlots.Add(key))
                        throw new PostCommitMismatchException(
                            $"BuildExpectedAfter(RepackPlan): duplicate deletion target at page={del.page} x={del.x} y={del.y}");

                    int foundAt = -1;
                    for (int i = 0; i < result._entries.Count; i++)
                    {
                        if (removeSet.Contains(i)) continue;
                        var e = result._entries[i];
                        if (e.page == del.page && e.x == del.x && e.y == del.y)
                        {
                            foundAt = i;
                            break;
                        }
                    }
                    if (foundAt < 0)
                        throw new PostCommitMismatchException(
                            $"BuildExpectedAfter(RepackPlan): deletion target not found page={del.page} x={del.x} y={del.y}");
                    removeSet.Add(foundAt);
                }
                // 倒序删除避免索引位移
                var sorted = new List<int>(removeSet);
                sorted.Sort((a, b) => b.CompareTo(a));
                foreach (var idx in sorted)
                {
                    result._entries.RemoveAt(idx);
                }
            }

            result._entries.Sort(EntryCompare);
            return result;
        }

        /// <summary>
        /// 纯内存计算：基于当前快照 + MergePlan 构建提交后预期快照。
        /// 不调用任何 Inventory API。供 CommitMerge 提交后比对。
        /// 缺失或重复目标均抛异常（fail-closed），与 RepackPlan 采用同一校验口径。
        /// </summary>
        internal InventorySnapshot BuildExpectedAfter(MergePlan plan)
        {
            var result = new InventorySnapshot();
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                result._entries.Add(new SnapshotEntry
                {
                    page = e.page,
                    x = e.x,
                    y = e.y,
                    rot = e.rot,
                    id = e.id,
                    amount = e.amount,
                    quality = e.quality,
                    state = e.state == null ? null : (byte[])e.state.Clone(),
                });
            }

            if (plan?.AmountUpdates != null)
            {
                var seenSlots = new HashSet<SlotKey>();
                foreach (var op in plan.AmountUpdates)
                {
                    var key = new SlotKey(op.page, op.x, op.y);
                    if (!seenSlots.Add(key))
                        throw new PostCommitMismatchException(
                            $"BuildExpectedAfter(MergePlan): duplicate amount target at page={op.page} x={op.x} y={op.y}");

                    int foundAt = -1;
                    for (int i = 0; i < result._entries.Count; i++)
                    {
                        var e = result._entries[i];
                        if (e.page == op.page && e.x == op.x && e.y == op.y)
                        {
                            foundAt = i;
                            break;
                        }
                    }
                    if (foundAt < 0)
                        throw new PostCommitMismatchException(
                            $"BuildExpectedAfter(MergePlan): amount target not found page={op.page} x={op.x} y={op.y}");
                    var entry = result._entries[foundAt];
                    entry.amount = op.newAmount;
                    result._entries[foundAt] = entry;
                }
            }

            if (plan?.Deletions != null && plan.Deletions.Count > 0)
            {
                var seenDelSlots = new HashSet<SlotKey>();
                var removeSet = new HashSet<int>();
                for (int di = 0; di < plan.Deletions.Count; di++)
                {
                    var del = plan.Deletions[di];
                    var key = new SlotKey(del.page, del.x, del.y);
                    if (!seenDelSlots.Add(key))
                        throw new PostCommitMismatchException(
                            $"BuildExpectedAfter(MergePlan): duplicate deletion target at page={del.page} x={del.x} y={del.y}");

                    int foundAt = -1;
                    for (int i = 0; i < result._entries.Count; i++)
                    {
                        if (removeSet.Contains(i)) continue;
                        var e = result._entries[i];
                        if (e.page == del.page && e.x == del.x && e.y == del.y)
                        {
                            foundAt = i;
                            break;
                        }
                    }
                    if (foundAt < 0)
                        throw new PostCommitMismatchException(
                            $"BuildExpectedAfter(MergePlan): deletion target not found page={del.page} x={del.x} y={del.y}");
                    removeSet.Add(foundAt);
                }
                var sorted = new List<int>(removeSet);
                sorted.Sort((a, b) => b.CompareTo(a));
                foreach (var idx in sorted)
                {
                    result._entries.RemoveAt(idx);
                }
            }

            result._entries.Sort(EntryCompare);
            return result;
        }

        /// <summary>
        /// 精确恢复：清空 page 范围内所有 jar，按快照重新 addItem。
        /// 主线程调用。失败抛异常由调用方捕获（RestoreAndVerify）。
        /// </summary>
        internal void RestoreExact(PlayerInventory inv)
        {
            if (inv == null) throw new ArgumentNullException(nameof(inv));

            // 按页分组：先清空，再按快照顺序 addItem
            var byPage = new Dictionary<byte, List<SnapshotEntry>>();
            foreach (var e in _entries)
            {
                if (!byPage.TryGetValue(e.page, out var list))
                {
                    list = new List<SnapshotEntry>();
                    byPage[e.page] = list;
                }
                list.Add(e);
            }

            foreach (var kvp in byPage)
            {
                Items items = inv.items[kvp.Key];
                if (items == null) continue;

                // 清空
                while (items.getItemCount() > 0)
                {
                    items.removeItem(0);
                }

                // 重新 addItem
                foreach (var e in kvp.Value)
                {
                    var newItem = new Item(e.id, e.amount, e.quality, e.state);
                    items.addItem(e.x, e.y, e.rot, newItem);
                }
            }
        }

        private static int EntryCompare(SnapshotEntry a, SnapshotEntry b)
        {
            int c = a.page.CompareTo(b.page);
            if (c != 0) return c;
            c = a.x.CompareTo(b.x);
            if (c != 0) return c;
            c = a.y.CompareTo(b.y);
            if (c != 0) return c;
            return a.rot.CompareTo(b.rot);
        }

        private static bool StateEquals(byte[] a, byte[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
