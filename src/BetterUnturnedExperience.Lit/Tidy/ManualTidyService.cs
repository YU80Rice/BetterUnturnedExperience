using System;
using System.Collections.Generic;
using System.Threading;
using SDG.Unturned;

// DEV-V2-15: migrated from LaunchInventoryTidy (author: YU80Rice, MIT License,
// Copyright (c) 2026 YU80Rice; local source of truth: Archive/2-未闭环验证项目/LaunchInventoryTidy,
// retired by the wayfinder single-source decision). Attribution:
// docs/third-party/LaunchInventoryTidy-attribution.md. Behavior migrated as-is
// unless a DEV-V2-15 note says otherwise.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// 物品指纹：id + amount + quality + state（byte[] 完整比对）。
    /// 用于整理前后的多重集合守恒验证。
    /// </summary>
    internal struct ItemFingerprint : IEquatable<ItemFingerprint>
    {
        public ushort Id;
        public byte Amount;
        public byte Quality;
        public byte[] State;

        public ItemFingerprint(Item item)
        {
            Id = item?.id ?? 0;
            Amount = item?.amount ?? 0;
            Quality = item?.quality ?? 0;
            byte[] src = item?.state;
            if (src == null || src.Length == 0)
            {
                State = Array.Empty<byte>();
            }
            else
            {
                State = new byte[src.Length];
                Buffer.BlockCopy(src, 0, State, 0, src.Length);
            }
        }

        /// <summary>v2.0.2 新增：按值字段构造指纹，用于 JarSnapshot.Fingerprint 属性。</summary>
        public ItemFingerprint(ushort id, byte amount, byte quality, byte[] state)
        {
            Id = id;
            Amount = amount;
            Quality = quality;
            if (state == null || state.Length == 0)
            {
                State = Array.Empty<byte>();
            }
            else
            {
                State = new byte[state.Length];
                Buffer.BlockCopy(state, 0, State, 0, state.Length);
            }
        }

        public bool Equals(ItemFingerprint other)
        {
            if (Id != other.Id || Amount != other.Amount || Quality != other.Quality) return false;
            if (State == null || other.State == null) return State == other.State;
            if (State.Length != other.State.Length) return false;
            for (int i = 0; i < State.Length; i++)
                if (State[i] != other.State[i]) return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hash = Id.GetHashCode() ^ (Amount << 16) ^ (Quality << 24);
            if (State != null)
            {
                for (int i = 0; i < State.Length; i++)
                    hash = (hash * 31) ^ State[i];
            }
            return hash;
        }
    }

    /// <summary>
    /// 整理事务结果。
    /// v2.0.1 修订：
    ///   - Committed = 全部页面提交且指纹守恒
    ///   - Rejected = 候选布局存在未放置物品或静态验证失败，未修改任何物品
    ///   - CriticalFailure = 提交或守恒验证失败；已尝试回滚，玩家被熔断
    /// v2.0.6.5 新增（Codex v2.0.6.4 审计 §五阻断项 1）：
    ///   - ConcurrentMutationAfterCommit = 检测到已提交页在本事务提交后被并发修改，
    ///     回滚被拒绝（避免覆盖合法并发变更），进入安全隔离状态，需人工处置
    /// </summary>
    internal enum TidyCommitResult : byte
    {
        Committed = 0,
        Rejected = 1,
        CriticalFailure = 2,
        /// <summary>v2.0.6.5：已提交页被并发修改，禁止回滚，安全隔离。</summary>
        ConcurrentMutationAfterCommit = 3,
    }

    /// <summary>
    /// v2.0.6.4 新增：单页 Commit 阶段的细粒度结果。
    /// Codex v2.0.6.3 审计 §五阻断项 1 指出：
    ///   - 用 InvalidOperationException 泛化处理 Commit 前库存变化，会触发上层 catch
    ///     进入 CriticalFailure 路径并错误回滚未修改页，覆盖合法并发变更
    ///   - 必须区分"Commit 前已变化（未开始任何 removeItem）"与"Commit 中途异常（已开始 removeItem）"
    ///
    /// v2.0.6.5 修订（Codex v2.0.6.4 审计 §五阻断项 2）：
    ///   - 原枚举 MutationStarted 仅以 catch 作为判据，不是实际写入状态
    ///   - 新增 MutationMayHaveStarted 替代，表示"第一次 removeItem 已调用，但写入状态不确定"
    ///   - 异常路径必须先做 post-commit 写前比较，状态不确定时禁止盲目快照覆盖
    /// </summary>
    internal enum CommitPageResult : byte
    {
        Committed = 0,
        NotStartedInventoryChanged = 1,
        /// <summary>v2.0.6.5：异常路径，removeItem 可能已开始也可能未开始，状态不确定。</summary>
        MutationMayHaveStarted = 2,
    }

    /// <summary>
    /// v2.0.3 新增：完整事务结果，携带真实回滚状态。
    /// v2.0.4 P0-4 新增：HotkeyRollbackVerified 字段，快捷键恢复结果纳入事务状态。
    ///
    /// 网络层处理原则：
    ///   - Rejected + MutationStarted=false -> 不熔断
    ///   - CriticalFailure + RollbackVerified=true + HotkeyRollbackVerified=true -> 临时熔断（restoreVerified=true）
    ///   - CriticalFailure + 任意 RollbackVerified=false -> 持久熔断（restoreVerified=false，写盘）
    /// </summary>
    internal sealed class TidyOperationOutcome
    {
        public TidyCommitResult Result;
        /// <summary>是否已开始修改库存（Commit 阶段已执行至少一页）。</summary>
        public bool MutationStarted;
        /// <summary>是否尝试了回滚。</summary>
        public bool RollbackAttempted;
        /// <summary>库存回滚是否经过坐标+旋转+指纹完整验证。</summary>
        public bool RollbackVerified;
        /// <summary>v2.0.4 P0-4：快捷键回滚是否全部成功（原坐标 ServerBindItemHotkey 验证）。</summary>
        public bool HotkeyRollbackVerified;
        /// <summary>v2.0.4 P0-4：快捷键回滚尝试恢复的总数。</summary>
        public int HotkeyRestoreAttempted;
        /// <summary>v2.0.4 P0-4：快捷键回滚成功恢复的数量。</summary>
        public int HotkeyRestoreSucceeded;
        /// <summary>v2.0.4 P0-4：快捷键回滚失败的数量（>0 时 HotkeyRollbackVerified=false）。</summary>
        public int HotkeyRestoreFailed;
        /// <summary>失败原因（CriticalFailure 时非空）。</summary>
        public string FailureReason;

        /// <summary>v2.0.4 P0-4：完整恢复验证 = 库存回滚 + 快捷键回滚全部成功。</summary>
        public bool FullRestorationVerified => RollbackVerified && HotkeyRollbackVerified;

        public static readonly TidyOperationOutcome RejectedNoMutation =
            new TidyOperationOutcome { Result = TidyCommitResult.Rejected, MutationStarted = false, HotkeyRollbackVerified = true };

        public static readonly TidyOperationOutcome Committed =
            new TidyOperationOutcome { Result = TidyCommitResult.Committed, MutationStarted = true, HotkeyRollbackVerified = true };
    }

    /// <summary>
    /// 单页整理的新位置映射：旧 ItemJar -> 新 (page, x, y, rot)。
    /// </summary>
    internal struct NewPosition
    {
        public byte Page;
        public byte X;
        public byte Y;
        public byte Rot;

        public NewPosition(byte page, byte x, byte y, byte rot)
        {
            Page = page; X = x; Y = y; Rot = rot;
        }
    }

    /// <summary>
    /// 单个 ItemJar 的值快照（v2.0.2 修订：真正的值拷贝，不再保留可变 Item 引用）。
    ///
    /// v2.0.1 缺陷：原设计仅保存原 Item 引用，Item.amount/quality/state 均为可变字段，
    ///              若 Commit 阶段修改了原 Item（或外部 Patch 干预），回滚无法恢复提交前的真实值。
    ///
    /// v2.0.2 修复：
    ///   - 保存 OriginalJar 引用（用于 ValidateTagConsistency 验证 Tag 来自 before）
    ///   - 保存 Id/Amount/Quality/State.Clone() 值拷贝
    ///   - RecreateItem() 创建全新的 Item 实例用于回滚
    /// </summary>
    internal sealed class JarSnapshot
    {
        public ItemJar OriginalJar;
        public byte X;
        public byte Y;
        public byte Rot;
        public ushort Id;
        public byte Amount;
        public byte Quality;
        public byte[] State;

        public ItemFingerprint Fingerprint => new ItemFingerprint(Id, Amount, Quality, State);

        public JarSnapshot(ItemJar jar)
        {
            OriginalJar = jar;
            X = jar.x;
            Y = jar.y;
            Rot = jar.rot;
            Item item = jar.item;
            Id = item?.id ?? 0;
            Amount = item?.amount ?? 0;
            Quality = item?.quality ?? 0;
            byte[] src = item?.state;
            if (src == null || src.Length == 0)
            {
                State = Array.Empty<byte>();
            }
            else
            {
                State = new byte[src.Length];
                Buffer.BlockCopy(src, 0, State, 0, src.Length);
            }
        }

        /// <summary>按快照值创建全新的 Item 实例，避免共享可变引用。</summary>
        public Item RecreateItem()
        {
            byte[] stateCopy = State == null || State.Length == 0
                ? null
                : (byte[])State.Clone();
            return new Item(Id, Amount, Quality, stateCopy);
        }
    }

    /// <summary>
    /// v2.0.6.6 新增（Codex v2.0.6.5 审计 §三 Critical 1 修复）：
    /// 单个写入操作的 mutation journal 条目。记录操作类型、操作后预期状态、
/// 用于异常路径回滚前验证当前状态是否由本事务造成。
    ///
    /// 修复方向：原 v2.0.6.5 实现仅以"当前状态 != BeforeJars"推断 removeItem 已执行，
    /// 但合法重入修改、第三方模组修改或异常前的状态变化同样会"不等于 Before"，
    /// 仍会被覆盖。改为可验证的逐步 mutation journal：
    ///   - 每次 removeAll / addItem 后捕获轻量预期状态快照
    ///   - 异常路径回滚前遍历 journal，找到第一个匹配当前状态的预期中间态
    ///   - 匹配 = 当前状态由本事务造成，可安全按 BeforeJars 重建
    ///   - 不匹配任何 journal 条目 = 未知状态，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离
    /// </summary>
    /// v2.0.6.7 修订（Codex v2.0.6.6 审计 §三 Critical 1 修复）：
    /// journal 必须在**每个写调用前**建立，含可比较的 before/after 全量状态：
    ///   - 每次 removeItem(i) 调用前：捕获 ExpectedStateBefore + 计算 ExpectedStateAfter（移除第 i 项后）
    ///   - 每次 addItem(x,y,rot,item) 调用前：捕获 ExpectedStateBefore + 计算 ExpectedStateAfter（添加该 item 后）
    ///   - 异常后只接受当前精确匹配某个 journal 条目的 before 或 after 状态，再执行可验证回滚
    ///   - 任何不能证明的状态禁止清空/重建，进入 ConcurrentMutationAfterCommit 安全隔离 + 持久熔断
    ///   - 修复 v2.0.6.6 的"整个 while(removeItem) 循环完成后才记一条 RemoveAll"问题
    /// </summary>
    internal sealed class MutationJournalEntry
    {
        /// <summary>步骤序号（从 0 开始，每个 removeItem / addItem 递增）。</summary>
        public int StepIndex;
        /// <summary>操作类型："RemoveItem" 或 "AddItem"。</summary>
        public string OperationType;
        /// <summary>
        /// 操作前的预期状态（轻量元组列表，按值比较）。
        /// v2.0.6.7 新增：用于异常路径验证当前状态是否"未执行到此步"。
        /// </summary>
        public List<(byte x, byte y, byte rot, ItemFingerprint fp)> ExpectedStateBefore;
        /// <summary>
        /// 操作后的预期状态（轻量元组列表，按值比较）。
        /// 用于异常路径验证当前状态是否"已执行完此步"。
        /// </summary>
        public List<(byte x, byte y, byte rot, ItemFingerprint fp)> ExpectedStateAfter;
        /// <summary>
        /// AddItem 操作对应的 PackableItem（仅 AddItem 有，RemoveItem 为 null）。
        /// </summary>
        public PackableItem AddedItem;
        /// <summary>
        /// RemoveItem 操作的目标索引（仅 RemoveItem 有意义，AddItem 为 -1）。
        /// v2.0.6.7 新增：用于精确描述 removeItem(i) 的目标。
        /// </summary>
        public int RemoveItemIndex;
    }

    /// <summary>
    /// 单页准备结果：装箱求解 + 静态验证。提交前所有验证必须通过。
    /// v2.0.6.5 新增：PostCommitJars 字段，用于回滚前的 post-commit 写前比较。
    /// v2.0.6.6 新增：MutationJournal 字段，记录每个 remove/add 操作后的预期中间状态。
    /// </summary>
    /// <summary>
    /// v2.0.6.10 修订（Codex v2.0.6.9 审计 §三 Critical）：
    ///   - struct 改为 sealed class，避免 CommitPage/TryRollbackPage* 按值传参时 journal/post-commit 快照丢失
    ///   - 所有字段写入通过引用回传到 List 元素，调用方回滚可见
    ///   - 字段集合不变，构造方式不变（new PagePreparation { ... }）
    /// </summary>
    internal sealed class PagePreparation
    {
        public byte Page;
        public bool Valid;          // true = 可提交
        public IReadOnlyList<PackableItem> Result;
        public List<JarSnapshot> BeforeJars;  // 提交前快照（用于回滚）
        /// <summary>v2.0.6.5：提交后快照（Committed 后捕获），用于回滚前写前比较。</summary>
        public List<JarSnapshot> PostCommitJars;
        /// <summary>v2.0.6.5：CommitPageResult，Committed=true 时 PostCommitJars 有效。</summary>
        public CommitPageResult CommitResult;
        /// <summary>v2.0.6.6：mutation journal，记录每个 remove/add 操作后的预期中间状态。</summary>
        public List<MutationJournalEntry> MutationJournal;
        public Items ItemsInstance;
        public byte Width;
        public byte Height;
        /// <summary>DEV-V5-04: the item-to-be-added riding this page's plan
        /// (insert recovery only — Tag = the vanilla Item, no jar yet, and the
        /// whole validation/commit/conservation chain treats it as part of the
        /// page's expected content). Null = the button-tidy shape: byte-for-byte
        /// the pre-DEV-V5-04 semantics.</summary>
        public PackableItem Pending;

        /// <summary>DEV-V5-05: the item-to-be-removed of a fast-transfer recovery
        /// (Tag = the leaving ItemJar, the mirror of Pending). The page's plan is
        /// the IDENTITY layout of everything except this jar — 源网格不整理的字面
        /// 落实 — and the validation/commit/conservation chain treats it as part
        /// of the page's pre-state and NOT of its post-state. Null = every
        /// pre-DEV-V5-05 shape unchanged.</summary>
        public PackableItem Leaving;
    }

    /// <summary>
    /// 手动整理服务：v2.0.1 真事务化重写。
    ///
    /// 流程：
    ///   1) Prepare 阶段：捕获全量快照 + 装箱 + 静态验证（无任何副作用）
    ///   2) 若任何一页 Prepare 失败 -> 返回 Rejected（零副作用）
    ///   3) Commit 阶段：清空 + 重添
    ///   4) 若 Commit 异常 -> Rollback 阶段：按快照重建
    ///   5) 若 Rollback 失败 -> 熔断玩家
    ///   6) Verify 阶段：捕获重排后指纹 + 比对
    ///   7) 若 Verify 失败 -> Rollback + 熔断
    /// </summary>
    internal static class ManualTidyService
    {
        /// <summary>
        /// 对玩家 page 2..6 五个多格页原子化整理。
        /// 全部页面先 Prepare，任一失败则全局不修改；Commit 失败则只回滚已开始提交的页面。
        /// v2.0.3 修订：返回 TidyOperationOutcome，携带真实 RollbackAttempted/RollbackVerified。
        /// v2.0.3 P1-M11 修订：仅回滚 lastCommitStartedIndex 之前（含）已开始提交的页面，
        ///                     未开始提交的页面保持零副作用。
        /// v2.0.3 P1-M12 修订：CriticalFailure + RollbackVerified=true 时，网络层负责
        ///                     按原坐标恢复快捷键（服务层不直接访问 PlayerEquipment）。
        /// v2.0.6.4 修订（Codex v2.0.6.3 审计 §五阻断项 1）：
        ///                     CommitPage 不再抛异常，改用 CommitPageResult 返回值。
        ///                     - NotStartedInventoryChanged：当前页未修改，绝不回滚当前页，
        ///                       仅回滚之前已 Committed 的页面（原子性），整体返回 Rejected
        ///                     - MutationStarted：当前页破损，必须回滚当前页 + 之前已 Committed 的页面，
        ///                       整体返回 CriticalFailure
        /// DEV-V2-15 修订：装箱计划不再直呼 InventorySolver，而是经调用方注入的
        ///                     ITidyStrategy（default-grid-v1 包住原求解器，行为不变）。
        ///                     strategy 是显式参数不是隐藏默认——null 是开发者错误，fail-fast。
        /// </summary>
        public static TidyOperationOutcome TidyAllPlayerPages(PlayerInventory inv, bool sortDescending,
            TidyMode mode, Dictionary<ItemJar, NewPosition> outMapping, ITidyStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));

            // v2.0.6.4：主线程检查改为返回值，失败时返回 RejectedNoMutation（不触发熔断）。
            // Codex v2.0.6.3 审计 §五阻断项 2 指出：主线程断言失败不应进入持久熔断。
            if (!IsMainThread("TidyAllPlayerPages"))
            {
                return TidyOperationOutcome.RejectedNoMutation;
            }

            if (inv == null) return TidyOperationOutcome.RejectedNoMutation;

            // 阶段 1：Prepare 所有页面
            // v2.0.6.14 修订（Codex U3DS-LIT-ALLPAGES-01 Agent 第 1 轮修复蓝图 §3.1）：
            //   - ALL_PAGES 仅整理存在且尺寸大于零的页面
            //   - 原生 0x0 page（未装备/未扩容服装容器）属于合法不活动页，跳过且不触发 fail-closed
            //   - 活动页（非零尺寸）任何 Prepare 失败或异常仍必须 fail-closed，整体 RejectedNoMutation
            //   - 不允许把 TidyPage 单页零尺寸拒绝改成 success（仅 ALL_PAGES 跳过原生不活动页）
            var preparations = new List<PagePreparation>(PlayerInventory.PANTS - PlayerInventory.SLOTS + 1);
            for (byte page = PlayerInventory.SLOTS; page <= PlayerInventory.PANTS; page++)
            {
                Items pageItems = inv.items[page];

                // 跳过原生不活动页：null 或 0x0
                // 不跳过任何非零尺寸页的 Prepare 失败
                if (pageItems == null || pageItems.width == 0 || pageItems.height == 0)
                {
                    TidyDiagnosticLog.Info("transaction-inactive-page",
                        $"ALL_PAGES skip inactive page={page}, " +
                        $"width={(pageItems == null ? 0 : pageItems.width)}, " +
                        $"height={(pageItems == null ? 0 : pageItems.height)}.");
                    continue;
                }

                try
                {
                    PagePreparation prep = PreparePage(pageItems, page, sortDescending, mode, strategy);
                    if (!prep.Valid)
                    {
                        LitRuntime.LogWarning(
                            $"[Tidy] ALL_PAGES active page={page} Prepare failed; reject with zero mutation.");
                        return TidyOperationOutcome.RejectedNoMutation;
                    }

                    preparations.Add(prep);
                }
                catch (Exception exception)
                {
                    LitRuntime.LogError(
                        $"[Tidy] ALL_PAGES active page={page} Prepare crashed; " +
                        $"reject with zero mutation: {exception}");
                    return TidyOperationOutcome.RejectedNoMutation;
                }
            }

            // v2.0.6.14：没有任何活动页时不得伪报成功
            if (preparations.Count == 0)
            {
                LitRuntime.LogWarning(
                    "[Tidy] ALL_PAGES rejected: no active inventory pages exist in range 2..6.");
                return TidyOperationOutcome.RejectedNoMutation;
            }

            // DEV-V5-04：Commit/Verify/回滚阶段整体提取为 CommitPreparations
            // （行为逐字节不变），供按钮全身整理与入包恢复共用同一原子事务出口。
            return CommitPreparations(preparations, outMapping);
        }

        /// <summary>
        /// The ONE all-pages atomic transaction exit (DEV-V5-04 extraction —
        /// the code below is TidyAllPlayerPages' v2.0.6.x commit/verify/rollback
        /// chain moved verbatim): commit the given preparations atomically
        /// (all-or-nothing with journal-verified rollback and fingerprint
        /// conservation), page by page in list order. Both the button 全身整理
        /// (through TidyAllPlayerPages) and the insert recovery (through
        /// InsertRecoverAdapter, with a pending-bound preparation) commit here —
        /// no second transaction, no copied failure semantics. An empty list is
        /// refused, never a false success.
        /// </summary>
        internal static TidyOperationOutcome CommitPreparations(
            List<PagePreparation> preparations, Dictionary<ItemJar, NewPosition> outMapping)
        {
            // 空提交集不伪报成功（调用方各自在前置层已守，本防御与
            // TidyAllPlayerPages 的「无活动页拒绝」同律）。
            if (preparations == null || preparations.Count == 0)
            {
                LitRuntime.LogWarning("[Tidy] CommitPreparations rejected: empty preparation set (no false success).");
                return TidyOperationOutcome.RejectedNoMutation;
            }

            // v2.0.6.4：Commit 阶段使用 CommitPageResult 返回值，不再依赖 try-catch 控制流。
            // lastCommittedIndex 追踪已成功 Committed 的页面索引（用于原子性回滚）。
            int lastCommittedIndex = -1;

            for (int i = 0; i < preparations.Count; i++)
            {
                CommitPageResult result = CommitPage(preparations[i], outMapping);

                if (result == CommitPageResult.Committed)
                {
                    lastCommittedIndex = i;
                    continue;
                }

                if (result == CommitPageResult.NotStartedInventoryChanged)
                {
                    // 当前页未修改：绝不回滚当前页（保留并发合法变更）
                    // 仅回滚之前已 Committed 的页面（0..i-1）以维持原子性
                    // v2.0.6.5：使用 TryRollbackRangeWithPreCheck，若任一已提交页被并发修改，
                    //           禁止回滚，返回 ConcurrentMutationAfterCommit
                    LitRuntime.LogWarning(
                        $"[Tidy] page {preparations[i].Page} Commit 前库存已变更（并发修改），" +
                        $"停止整理，回滚之前 {i} 个已提交页面（带 post-commit 写前比较，当前页保持不动）");

                    bool rollbackOk = TryRollbackRangeWithPreCheck(preparations, 0, i - 1);
                    if (!rollbackOk)
                    {
                        LitRuntime.LogError(
                            "[Tidy] 原子性回滚检测到并发修改，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离");
                        return new TidyOperationOutcome
                        {
                            Result = TidyCommitResult.ConcurrentMutationAfterCommit,
                            MutationStarted = i > 0,
                            RollbackAttempted = false,  // 禁止回滚
                            RollbackVerified = false,
                            FailureReason = $"Page {preparations[i].Page} NotStartedInventoryChanged; ConcurrentMutationAfterCommit detected during atomic rollback of previous committed pages; rollback refused",
                        };
                    }

                    // 回滚成功：整体事务原子性保持，返回 Rejected（不触发熔断）
                    TidyDiagnosticLog.Info("transaction-atomic-rollback",
                        $"原子性回滚成功：0..{i - 1} 共 {i} 页已恢复，当前页 {preparations[i].Page} 保留并发变更。");
                    return new TidyOperationOutcome
                    {
                        Result = TidyCommitResult.Rejected,
                        MutationStarted = false,  // 最终状态匹配原始（已回滚）
                        RollbackAttempted = i > 0,
                        RollbackVerified = i > 0,
                        HotkeyRollbackVerified = true,
                        FailureReason = $"Page {preparations[i].Page} NotStartedInventoryChanged; {i} previous pages rolled back atomically with post-commit pre-check",
                    };
                }

                if (result == CommitPageResult.MutationMayHaveStarted)
                {
                    // v2.0.6.5：当前页破损（removeItem 可能已开始，状态不确定）
                    // 必须回滚当前页 + 之前已 Committed 的页面
                    // 使用 TryRollbackPageWithPreCheck：若 post-commit 快照存在且当前状态不匹配，
                    // 说明已提交页被并发修改，禁止回滚，返回 ConcurrentMutationAfterCommit
                    LitRuntime.LogError(
                        $"[Tidy] page {preparations[i].Page} Commit 中途异常（MutationMayHaveStarted），" +
                        $"回滚当前页 + 之前 {i} 个已提交页面（带 post-commit 写前比较）");

                    bool rollbackOkCurrent = TryRollbackPageWithPreCheck(preparations[i]);
                    // 若当前页检测到并发修改，禁止回滚之前已提交的页面（避免连锁吞物）
                    if (!rollbackOkCurrent)
                    {
                        LitRuntime.LogError(
                            $"[Tidy] page {preparations[i].Page} post-commit 写前比较失败，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离");
                        return new TidyOperationOutcome
                        {
                            Result = TidyCommitResult.ConcurrentMutationAfterCommit,
                            MutationStarted = true,
                            RollbackAttempted = false,  // 禁止回滚
                            RollbackVerified = false,
                            FailureReason = $"Page {preparations[i].Page} ConcurrentMutationAfterCommit detected during rollback of current page; rollback refused to avoid overwriting concurrent legitimate changes",
                        };
                    }

                    // 当前页回滚成功，继续回滚之前已 Committed 的页面（带 post-commit 写前比较）
                    bool rollbackOkPrev = TryRollbackRangeWithPreCheck(preparations, 0, i - 1);
                    if (!rollbackOkPrev)
                    {
                        LitRuntime.LogError(
                            $"[Tidy] 回滚之前 {i} 个已提交页面时检测到并发修改，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离");
                        return new TidyOperationOutcome
                        {
                            Result = TidyCommitResult.ConcurrentMutationAfterCommit,
                            MutationStarted = true,
                            RollbackAttempted = true,  // 当前页已回滚
                            RollbackVerified = false,
                            FailureReason = $"Page {preparations[i].Page} MutationMayHaveStarted; ConcurrentMutationAfterCommit detected during rollback of previous committed pages; partial rollback only",
                        };
                    }

                    return new TidyOperationOutcome
                    {
                        Result = TidyCommitResult.CriticalFailure,
                        MutationStarted = true,
                        RollbackAttempted = true,
                        RollbackVerified = true,
                        FailureReason = $"Page {preparations[i].Page} MutationMayHaveStarted during Commit; current + {i} previous pages rolled back with post-commit pre-check",
                    };
                }
            }

            // 阶段 3：Verify 指纹守恒
            for (int i = 0; i < preparations.Count; i++)
            {
                var prep = preparations[i];
                if (!VerifyFingerprintConservation(prep))
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {prep.Page} 指纹守恒失败，开始全局回滚（带 post-commit 写前比较）");
                    // v2.0.6.5：指纹守恒失败时使用 TryRollbackAllWithPreCheck
                    // 若任一已提交页被并发修改，禁止回滚，返回 ConcurrentMutationAfterCommit
                    bool rollbackOk = TryRollbackAllWithPreCheck(preparations);
                    if (!rollbackOk)
                    {
                        LitRuntime.LogError(
                            "[Tidy] 全局回滚检测到并发修改，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离");
                    }
                    return new TidyOperationOutcome
                    {
                        Result = rollbackOk ? TidyCommitResult.CriticalFailure : TidyCommitResult.ConcurrentMutationAfterCommit,
                        MutationStarted = true,
                        RollbackAttempted = rollbackOk,
                        RollbackVerified = rollbackOk,
                        FailureReason = rollbackOk
                            ? $"Fingerprint conservation failed at page {prep.Page}; all pages rolled back with post-commit pre-check"
                            : $"Fingerprint conservation failed at page {prep.Page}; ConcurrentMutationAfterCommit detected during global rollback; rollback refused",
                    };
                }
            }

            // 全部成功
            int totalPlaced = 0;
            for (int i = 0; i < preparations.Count; i++)
                totalPlaced += CountPlaced(preparations[i].Result);
            TidyDiagnosticLog.Info("transaction-all-committed",
                $"TidyAll 成功：{preparations.Count} 页共 {totalPlaced} 件物品重排，指纹守恒验证通过。");
            return TidyOperationOutcome.Committed;
        }

        /// <summary>
        /// 单页整理。流程同 TidyAllPlayerPages 但只针对一页。
        /// v2.0.3 修订：返回 TidyOperationOutcome，携带真实 RollbackAttempted/RollbackVerified。
        /// v2.0.3 P1-M12 修订：CriticalFailure + RollbackVerified=true 时，网络层负责
        ///                     按原坐标恢复快捷键（服务层不直接访问 PlayerEquipment）。
        /// v2.0.6.4 修订（Codex v2.0.6.3 审计 §五阻断项 1）：
        ///                     CommitPage 不再抛异常，改用 CommitPageResult 返回值。
        ///                     - NotStartedInventoryChanged：直接返回 RejectedNoMutation（零副作用）
        ///                     - MutationStarted：回滚当前页后返回 CriticalFailure
        /// </summary>
        internal static TidyOperationOutcome TidyPage(Items items, byte page, bool sortDescending,
            TidyMode mode, Dictionary<ItemJar, NewPosition> outMapping, ITidyStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));

            // v2.0.6.4：主线程检查改为返回值，失败时返回 RejectedNoMutation（不触发熔断）。
            if (!IsMainThread("TidyPage"))
            {
                return TidyOperationOutcome.RejectedNoMutation;
            }

            if (items == null)
            {
                LitRuntime.LogWarning(
                    $"[Tidy] page {page}: items is null, 跳过");
                return TidyOperationOutcome.RejectedNoMutation;
            }
            if (items.width == 0 || items.height == 0)
            {
                LitRuntime.LogWarning(
                    $"[Tidy] page {page}: items.width={items.width} height={items.height}，跳过");
                return TidyOperationOutcome.RejectedNoMutation;
            }

            // 阶段 1：Prepare
            PagePreparation prep;
            try
            {
                prep = PreparePage(items, page, sortDescending, mode, strategy);
            }
            catch (Exception e)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page} Prepare crashed: {e}");
                return TidyOperationOutcome.RejectedNoMutation;
            }

            if (!prep.Valid)
            {
                LitRuntime.LogWarning(
                    $"[Tidy] page {page}: Prepare 失败（候选布局存在未放置物品或静态验证失败），拒绝整理");
                return TidyOperationOutcome.RejectedNoMutation;
            }

            // 阶段 2：Commit（使用返回值，不再依赖 try-catch 控制流）
            CommitPageResult commitResult = CommitPage(prep, outMapping);

            if (commitResult == CommitPageResult.NotStartedInventoryChanged)
            {
                // 当前页未修改：直接返回 Rejected，不触发回滚（保留并发合法变更）
                LitRuntime.LogWarning(
                    $"[Tidy] page {page}: Commit 前库存已变更（并发修改），拒绝整理（零副作用，未回滚）");
                return TidyOperationOutcome.RejectedNoMutation;
            }

            if (commitResult == CommitPageResult.MutationMayHaveStarted)
            {
                // v2.0.6.5：当前页破损（removeItem 可能已开始，状态不确定）
                // 使用 TryRollbackPageWithPreCheck：若 post-commit 快照存在且当前状态不匹配，
                // 说明已提交页被并发修改，禁止回滚，返回 ConcurrentMutationAfterCommit
                LitRuntime.LogError(
                    $"[Tidy] page {page} Commit 中途异常（MutationMayHaveStarted），回滚当前页（带 post-commit 写前比较）");
                bool rollbackOk = TryRollbackPageWithPreCheck(prep);
                if (!rollbackOk)
                {
                    // 检测到并发修改，禁止回滚，返回 ConcurrentMutationAfterCommit
                    LitRuntime.LogError(
                        $"[Tidy] page {page} post-commit 写前比较失败，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离");
                    return new TidyOperationOutcome
                    {
                        Result = TidyCommitResult.ConcurrentMutationAfterCommit,
                        MutationStarted = true,
                        RollbackAttempted = false,
                        RollbackVerified = false,
                        FailureReason = $"Page {page} MutationMayHaveStarted; ConcurrentMutationAfterCommit detected during rollback; rollback refused",
                    };
                }
                return new TidyOperationOutcome
                {
                    Result = TidyCommitResult.CriticalFailure,
                    MutationStarted = true,
                    RollbackAttempted = true,
                    RollbackVerified = true,
                    FailureReason = $"Page {page} MutationMayHaveStarted during Commit; rolled back with post-commit pre-check",
                };
            }

            // 阶段 3：Verify
            if (!VerifyFingerprintConservation(prep))
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page} 指纹守恒失败，开始回滚（带 post-commit 写前比较）");
                bool rollbackOk = TryRollbackPageWithPreCheck(prep);
                if (!rollbackOk)
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {page} post-commit 写前比较失败，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离");
                }
                return new TidyOperationOutcome
                {
                    Result = rollbackOk ? TidyCommitResult.CriticalFailure : TidyCommitResult.ConcurrentMutationAfterCommit,
                    MutationStarted = true,
                    RollbackAttempted = rollbackOk,
                    RollbackVerified = rollbackOk,
                    FailureReason = rollbackOk
                        ? $"Fingerprint conservation failed at page {page}; rolled back with post-commit pre-check"
                        : $"Fingerprint conservation failed at page {page}; ConcurrentMutationAfterCommit detected during rollback; rollback refused",
                };
            }

            int placedCount = CountPlaced(prep.Result);
            TidyDiagnosticLog.Info("transaction-page-committed",
                $"page {page}: placed={placedCount}, strategy={strategy.StrategyId} 指纹守恒验证通过。");
            return TidyOperationOutcome.Committed;
        }

        // ─────────────────────────────────────────────────────────────
        // Prepare 阶段：捕获快照 + 装箱 + 静态验证（无副作用）
        // ─────────────────────────────────────────────────────────────

        // internal（DEV-V5-02 宿主测试缝）：标签填充→统一排版的官方先行消费在
        // Prepare 层的形状可无 Unity 观察；提交链仍由 TidyPage/TidyAllPlayerPages
        // 的事务层承担，本缝不复制算法、不写库存。
        internal static PagePreparation PreparePage(Items items, byte page, bool sortDescending, TidyMode mode, ITidyStrategy strategy)
        {
            return PreparePage(items, page, sortDescending, mode, strategy, null);
        }

        /// <summary>DEV-V5-04: the optional <paramref name="pending"/> is the
        /// item-to-be-added of an insert-recovery attempt (Tag = the vanilla
        /// Item, no jar yet). It enters the SAME unified-layout plan, must be
        /// placed for the preparation to be valid (禁吞物：报成功却没放入的
        /// v1.4.0 形状在这里就是非法), and joins the fingerprint/Tag
        /// conservation checks. pending = null reproduces the button path
        /// exactly.</summary>
        internal static PagePreparation PreparePage(Items items, byte page, bool sortDescending, TidyMode mode, ITidyStrategy strategy, PackableItem pending)
        {
            var prep = new PagePreparation
            {
                Page = page,
                ItemsInstance = items,
                Valid = false,
            };

            if (items == null || items.width == 0 || items.height == 0)
                return prep;

            prep.Width = items.width;
            prep.Height = items.height;

            // v2.0.2：捕获值快照（Id/Amount/Quality/State.Clone）+ OriginalJar 引用
            // v2.0.3 P1-M17：fail-closed（不再跳过异常数据）
            // DEV-V5-05：捕获段提取为 TryCaptureSnapshots（行为与日志逐字不变），
            // 供本页与 PreparePageLeave 共用，不留第二份快照拷贝。
            if (!TryCaptureSnapshots(items, page, out var jars))
                return prep;  // Valid = false

            var packList = new List<PackableItem>(jars.Count);
            for (byte i = 0; i < jars.Count; i++)
            {
                ItemJar jar = jars[i].OriginalJar;
                packList.Add(new PackableItem
                {
                    Tag = jar,
                    size_x = jar.size_x,
                    size_y = jar.size_y,
                    GroupKey = jar.item.id,
                    StableOrder = i,
                    OriginalX = jar.x,
                    OriginalY = jar.y,
                    OriginalRot = jar.rot,
                    PreferredRotation = jar.rot,
                    // DEV-V5-02 (V5-T3): the frozen player-use label comes from the
                    // module-internal classifier seam — a lookup failure lands in
                    // 其他 (never drops the jar), so both the single-page and the
                    // all-pages consumer enter the SAME tagged row-band plan.
                    Label = ItemUseSignalsProvider.ResolveFor(jar.item),
                });
            }

            prep.BeforeJars = jars;

            // DEV-V5-04: the item-to-be-added joins the stream AFTER the page's
            // own jars (capture order = count) and binds to this preparation —
            // every downstream check (placement completeness, Tag consistency,
            // fingerprint multiset) counts it.
            if (pending != null)
            {
                prep.Pending = pending;
                packList.Add(pending);
            }

            if (packList.Count == 0)
            {
                prep.Valid = true;  // 空页面视为合法
                prep.Result = packList;
                return prep;
            }

            // 装箱（DEV-V2-15：经 ITidyStrategy 策略 seam；DEV-V5-02：唯一内置
            // adapter 是 tagged-row-band-v1 统一排版——旧 mode 仍随输入传输但不再
            // 决定算法（V5-T3 Q2 裁决）；换 adapter 即换计划输出）
            TidyPlan plan = strategy.BuildPlan(new TidyInput(items.width, items.height, sortDescending, mode, packList));
            prep.Result = plan.Placements;

            // 检查未放置物品（沿用旧口径：结果列表中存在合法尺寸但未放置的物品即拒绝；
            // packOk 对应旧 TryPack 的返回值，由 plan.AllPlaced 承载）
            int unplaced = 0;
            for (int i = 0; i < plan.Placements.Count; i++)
                if (plan.Placements[i] != null && !plan.Placements[i].Placed && plan.Placements[i].size_x > 0 && plan.Placements[i].size_y > 0)
                    unplaced++;

            if (unplaced > 0 || !plan.AllPlaced)
            {
                LitRuntime.LogWarning(
                    $"[Tidy] page {page}: {unplaced} 个未放置物品，Prepare 失败");
                return prep;  // Valid = false
            }

            // 静态验证 1：无重叠 + 边界内
            if (!ValidateNoOverlap(plan.Placements, items.width, items.height))
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page}: 静态验证失败（重叠）");
                return prep;
            }
            if (!ValidateBounds(plan.Placements, items.width, items.height))
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page}: 静态验证失败（越界）");
                return prep;
            }

            // 静态验证 2：result 中的 Tag 与 before 中的 ItemJar 一一对应（无外来/重复/遗漏）；
            // DEV-V5-04：待加入物品以 Item 引用恰出现一次（多了=外来、少了=吞物、Placed=false
            // 已被上面的未放置计数拒绝）。
            if (!ValidateTagConsistency(plan.Placements, jars, pending, null))
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page}: 静态验证失败（Tag 一致性）");
                return prep;
            }

            // 静态验证 3：指纹多重集合匹配（result 中所有物品的指纹 = before 中所有物品的指纹；
            // DEV-V5-04 有 pending 时并入该件指纹一次）
            if (!ValidateFingerprintMultiset(plan.Placements, jars, pending, null))
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page}: 静态验证失败（指纹多重集不匹配）");
                return prep;
            }

            prep.Valid = true;
            return prep;
        }

        /// <summary>
        /// DEV-V5-05 extraction（行为与日志逐字不变）：fail-closed 值快照捕获。
        /// 任何 jar / jar.item 为 null 或数量不配 = 拒绝（异常数据绝不静默跳过）。
        /// </summary>
        private static bool TryCaptureSnapshots(Items items, byte page, out List<JarSnapshot> jars)
        {
            byte count = items.getItemCount();
            jars = new List<JarSnapshot>(count);
            for (byte i = 0; i < count; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar == null || jar.item == null)
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {page}: jar[{i}] 或 jar.item 为 null，Prepare fail-closed（不再跳过异常数据）");
                    return false;
                }
                jars.Add(new JarSnapshot(jar));
            }
            // v2.0.3 P1-M17：强校验 - 快照数量必须等于 items.getItemCount()
            if (jars.Count != count)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page}: 数量不一致 count={count}, jars.Count={jars.Count}，Prepare fail-closed");
                return false;
            }
            return true;
        }

        /// <summary>
        /// DEV-V5-05 (V5-T5 快速转移恢复): the source page's preparation = the
        /// IDENTITY layout of everything except the one leaving jar. This is
        /// 「源网格不整理」in the literal sense: every remaining item keeps its
        /// exact x/y/rot (the plan is read straight off the live page, never
        /// through the strategy — re-flowing the source would be the ticket's
        /// forbidden 转移失败但源已被整理), while the SAME static validation and
        /// the SAME atomic transaction (CommitPreparations) still bind the page:
        /// jar absent = refusal (零修改), conservation expects exactly one less
        /// fingerprint, and a verified rollback restores the leaving item with
        /// the page (两边都不动). The leaving jar must be one of this page's
        /// jars — an off-page leave fails closed, never a silent empty remove.
        /// </summary>
        internal static PagePreparation PreparePageLeave(Items items, byte page, ItemJar leave)
        {
            var prep = new PagePreparation
            {
                Page = page,
                ItemsInstance = items,
                Valid = false,
                Leaving = leave == null ? null : new PackableItem
                {
                    Tag = leave,
                    size_x = leave.size_x,
                    size_y = leave.size_y,
                    GroupKey = leave.item == null ? (ushort)0 : leave.item.id,
                    OriginalX = leave.x,
                    OriginalY = leave.y,
                    OriginalRot = leave.rot,
                },
            };

            if (items == null || items.width == 0 || items.height == 0 || leave == null)
                return prep;

            prep.Width = items.width;
            prep.Height = items.height;

            if (!TryCaptureSnapshots(items, page, out var jars))
                return prep;

            // 离场件必须在场（引用同一性）——不在场 = 本票禁的形状：绝不空摘。
            var leaveIndex = -1;
            for (int i = 0; i < jars.Count; i++)
            {
                if (ReferenceEquals(jars[i].OriginalJar, leave)) { leaveIndex = i; break; }
            }
            if (leaveIndex < 0)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page}: 离场物品不在该页（id={leave.item?.id}），Prepare fail-closed（绝不空摘）");
                return prep;
            }

            prep.BeforeJars = jars;

            // 恒等计划：除离场件外全部原位原旋转、全部 Placed；离场件只入 Leaving。
            var plan = new List<PackableItem>(jars.Count - 1);
            for (int i = 0; i < jars.Count; i++)
            {
                if (i == leaveIndex) continue;
                var jar = jars[i].OriginalJar;
                plan.Add(new PackableItem
                {
                    Tag = jar,
                    size_x = jar.size_x,
                    size_y = jar.size_y,
                    GroupKey = jar.item.id,
                    StableOrder = i,
                    OriginalX = jar.x,
                    OriginalY = jar.y,
                    OriginalRot = jar.rot,
                    PreferredRotation = jar.rot,
                    ResultX = jar.x,
                    ResultY = jar.y,
                    ResultRot = jar.rot,
                    Placed = true,
                });
            }
            prep.Result = plan;

            if (plan.Count == 0)
            {
                // 页上只剩离场件：空 Result 合法（这一件本就该离页；它的去向由接收侧
                // Pending 守恒钉住，空页不是伪成功）。
                prep.Valid = true;
                return prep;
            }

            if (!ValidateNoOverlap(plan, items.width, items.height))
            {
                LitRuntime.LogError($"[Tidy] page {page}: Leave 静态验证失败（重叠——原页面即已非法）");
                return prep;
            }
            if (!ValidateBounds(plan, items.width, items.height))
            {
                LitRuntime.LogError($"[Tidy] page {page}: Leave 静态验证失败（越界——原页面即已非法）");
                return prep;
            }
            if (!ValidateTagConsistency(plan, jars, null, prep.Leaving))
            {
                LitRuntime.LogError($"[Tidy] page {page}: Leave 静态验证失败（Tag 一致性）");
                return prep;
            }
            if (!ValidateFingerprintMultiset(plan, jars, null, prep.Leaving))
            {
                LitRuntime.LogError($"[Tidy] page {page}: Leave 静态验证失败（指纹多重集）");
                return prep;
            }

            prep.Valid = true;
            return prep;
        }

        /// <summary>
        /// v2.0.2 修订：验证 result 中每个 Placed 物品的 Tag 都来自 before 列表的 OriginalJar，
        /// 且每个 before jar 恰好出现一次。防止外来 Jar / 重复 Jar / 遗漏 Jar 通过验证。
        /// DEV-V5-04：pending 非空时，其 Tag（vanilla Item 引用，尚无 jar）必须在
        /// Placed 条目中恰好出现一次——多一次=外来、少一次=吞物，都直接非法。
        /// DEV-V5-05：leaving 非空时（与 pending 互斥的场景由调用方结构保证），其 Tag
        /// 必须是 before 里在场的 jar，且在 result 中恰好缺席——出现一次=目标被双重落格、
        /// 缺席但不在 before=空摘，都直接非法；Placed 数 = before - 1。
        /// </summary>
        private static bool ValidateTagConsistency(IReadOnlyList<PackableItem> result, List<JarSnapshot> before, PackableItem pending, PackableItem leaving)
        {
            if (result == null) return pending == null && leaving == null;

            // 构建 before OriginalJar 引用集合（用于验证 Tag 来自 before）
            var beforeSet = new HashSet<ItemJar>(ReferenceEqualityComparer<ItemJar>.Instance);
            for (int i = 0; i < before.Count; i++)
            {
                if (before[i]?.OriginalJar != null)
                    beforeSet.Add(before[i].OriginalJar);
            }

            object pendingTag = pending == null ? null : pending.Tag;
            object leavingTag = leaving == null ? null : leaving.Tag;
            if (leavingTag != null && !(leavingTag is ItemJar leaveJar && beforeSet.Contains(leaveJar))) return false; // 空摘
            var seen = new HashSet<ItemJar>(ReferenceEqualityComparer<ItemJar>.Instance);
            var pendingSeen = 0;
            for (int i = 0; i < result.Count; i++)
            {
                PackableItem p = result[i];
                if (p == null || !p.Placed) continue;
                if (p.Tag is ItemJar jar)
                {
                    if (jar.item == null) return false;
                    // DEV-V5-05：离场件绝不双重落格（result 里再出现 = 摘了个重复）。
                    if (leavingTag != null && ReferenceEquals(p.Tag, leavingTag)) return false;
                    // v2.0.2：Tag 必须来自 before 集合（防止外来 Jar）
                    if (!beforeSet.Contains(jar)) return false;
                    if (!seen.Add(jar)) return false;  // 重复
                    continue;
                }
                // DEV-V5-04：唯一允许的非 jar Tag = 待加入物品本体（引用相等）。
                if (pendingTag == null || !ReferenceEquals(p.Tag, pendingTag)) return false;
                pendingSeen++;
            }
            // result 中 Placed jar 数 = before 数（无遗漏）或 before-1（离场件恰缺席），
            // 且待加入物品恰好一次。
            var expectedSeen = before.Count - (leaving != null ? 1 : 0);
            return seen.Count == expectedSeen && pendingSeen == (pending != null ? 1 : 0);
        }

        /// <summary>
        /// 验证 result 中所有 Placed 物品的指纹多重集合 = before 中所有物品的指纹多重集合。
        /// 这是删除前的静态守恒验证。v2.0.2：使用 JarSnapshot 值字段而非可变 Item 引用。
        /// DEV-V5-04：pending 非空时，pending 的指纹并入 before 侧（提交后的页面必须多它一件）。
        /// DEV-V5-05：leaving 非空时，其快照指纹从 before 侧恰减一次（提交后的页面 =
        /// 原内容 - 它；离场件的归属由接收侧 Pending 守恒钉住，两侧合起来总量不变）。
        /// </summary>
        private static bool ValidateFingerprintMultiset(IReadOnlyList<PackableItem> result, List<JarSnapshot> before, PackableItem pending, PackableItem leaving)
        {
            if (result == null) return pending == null && leaving == null;
            var beforeList = new List<ItemFingerprint>(before.Count + 1);
            ItemJar leaveJar = leaving == null ? null : leaving.Tag as ItemJar;
            var leaveRemoved = false;
            for (int i = 0; i < before.Count; i++)
            {
                // 离场件按 OriginalJar 引用恰减一次（引用匹配优先于指纹匹配，
                // 同 id 同指纹的两件不会互相顶包）。
                if (leaveJar != null && !leaveRemoved && ReferenceEquals(before[i]?.OriginalJar, leaveJar))
                {
                    leaveRemoved = true;
                    continue;
                }
                beforeList.Add(before[i].Fingerprint);
            }
            if (leaveJar != null && !leaveRemoved) return false; // 离场件根本不在场 = 非法
            // DEV-V5-04：待加入物品的指纹进期望侧（提交后的页面 = 原内容 + 它）。
            if (pending != null && pending.Tag is Item pendingItem)
                beforeList.Add(new ItemFingerprint(pendingItem));

            object pendingTag = pending == null ? null : pending.Tag;
            var afterList = new List<ItemFingerprint>(before.Count + 1);
            for (int i = 0; i < result.Count; i++)
            {
                PackableItem p = result[i];
                if (p == null || !p.Placed) continue;
                if (p.Tag is ItemJar jar)
                {
                    if (jar?.item == null) return false;
                    afterList.Add(new ItemFingerprint(jar.item));
                    continue;
                }
                if (pendingTag == null || !ReferenceEquals(p.Tag, pendingTag)) return false;
                afterList.Add(new ItemFingerprint((Item)p.Tag));
            }

            if (beforeList.Count != afterList.Count) return false;

            beforeList.Sort(CompareFingerprint);
            afterList.Sort(CompareFingerprint);
            for (int i = 0; i < beforeList.Count; i++)
                if (!beforeList[i].Equals(afterList[i])) return false;
            return true;
        }

        private static int CompareFingerprint(ItemFingerprint a, ItemFingerprint b)
        {
            if (a.Id != b.Id) return a.Id.CompareTo(b.Id);
            if (a.Amount != b.Amount) return a.Amount.CompareTo(b.Amount);
            if (a.Quality != b.Quality) return a.Quality.CompareTo(b.Quality);
            if (a.State == null && b.State == null) return 0;
            if (a.State == null) return -1;
            if (b.State == null) return 1;
            if (a.State.Length != b.State.Length) return a.State.Length.CompareTo(b.State.Length);
            for (int i = 0; i < a.State.Length; i++)
                if (a.State[i] != b.State[i]) return a.State[i].CompareTo(b.State[i]);
            return 0;
        }

        // ─────────────────────────────────────────────────────────────
        // Commit 阶段：清空 + 按 result 重添
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// v2.0.6.7 修订（Codex v2.0.6.6 审计 §三 Critical 1 修复）：
        /// CommitPage 改为**每个写调用前**记录 journal 条目，含 before/after 全量状态。
        ///
        /// 修复 v2.0.6.6 的阻断项：
        ///   - v2.0.6.6 整个 while(removeItem) 循环完成后才记一条 "RemoveAll"
        ///   - 若第 N 次 removeItem 抛异常，前面已发生的写入没有可匹配 journal 状态
        ///   - 返回 ConcurrentMutationAfterCommit 不回滚 -> 背包保持部分删除/部分添加状态
        ///
        /// v2.0.6.7 新流程：
        ///   1. 入口前：捕获 ExpectedStateBefore0（= BeforeJars，原初始状态）
        ///   2. 每次 removeItem(i) 前：记录 journal 条目，含 ExpectedStateBefore（当前）+ ExpectedStateAfter（移除第 i 项后）
        ///   3. 每次 addItem(x,y,rot,item) 前：记录 journal 条目，含 ExpectedStateBefore（当前）+ ExpectedStateAfter（添加后）
        ///   4. 异常路径：TryRollbackPageWithPreCheck 遍历 journal 找匹配的 before 或 after 状态
        ///      - 匹配 before[i]：操作 i 未执行，可从 before[i] 状态回滚到 BeforeJars
        ///      - 匹配 after[i]：操作 i 已执行完，可从 after[i] 状态回滚到 BeforeJars
        ///      - 无匹配：未知状态，禁止回滚，ConcurrentMutationAfterCommit + 持久熔断
        /// </summary>
        private static CommitPageResult CommitPage(PagePreparation prep, Dictionary<ItemJar, NewPosition> outMapping)
        {
            Items items = prep.ItemsInstance;
            if (items == null) return CommitPageResult.Committed;

            // v2.0.6.4：Commit 前二次校验。失败 = NotStartedInventoryChanged（removeItem 未开始）
            if (!ValidateInventoryUnchanged(items, prep.BeforeJars, prep.Page))
            {
                LitRuntime.LogWarning(
                    $"[Tidy] page {prep.Page} Commit 前二次校验失败：库存已变更（并发修改），返回 NotStartedInventoryChanged");
                return CommitPageResult.NotStartedInventoryChanged;
            }

            // 空页面直接返回 Committed，并捕获 post-commit 快照（空快照）。
            // DEV-V5-05：有离场件时空 Result 也必须真走删除循环（页上只剩这一件时
            // 提交后应为空页——直接早退会把离场件留在页上，守恒随后按配平失败处理，
            // 但功能上就是错的，这里从提交形状上堵死）。
            if ((prep.Result == null || prep.Result.Count == 0) && prep.Leaving == null)
            {
                prep.PostCommitJars = CapturePostCommitSnapshot(items, prep.Page);
                prep.CommitResult = CommitPageResult.Committed;
                return CommitPageResult.Committed;
            }

            // v2.0.6.7：journal 改为每个写调用前记录
            // 不再使用"整个循环完成后才记一条 RemoveAll"
            // v2.0.6.8：Codex v2.0.6.7 审计 §三 Medium 5 模板 D 修复：
            //   - 每次 removeItem/addItem 调用后，立即捕获实际状态并与 ExpectedStateAfter 比较
            //   - 若不匹配（原版 API 静默失败、重入修改、调用前抛、改变后抛等），
            //     抛异常触发 catch 块，由 TryRollbackPageWithPreCheck 决定是否安全回滚
            //   - 若当前状态不匹配任何 journal 条目的 before/after，进入 ConcurrentMutationAfterCommit
            // v2.0.6.10：Codex v2.0.6.9 审计 §三 P0-2 故障注入钩子点：
            //   - NotifyCommitPageStart / OnAfterRemoveItem / OnAfterAddItem
            // v2.0.6.11：Codex v2.0.6.10 审计 §三 P0-1 修复：
            //   - 所有故障注入 hook 用 #if TIDY_TEST_HARNESS 包裹
            //   - Release 构建中 hook 为 no-op，不存在能对真实背包抛故障的代码路径
            //   - NotifyCommitPageStart 签名改为接受 page 参数，用于精确匹配 TargetPage
            prep.MutationJournal = new List<MutationJournalEntry>();
            int stepIdx = 0;
            try
            {
                // ===== 进入 MutationMayHaveStarted 区间 =====
                // v2.0.6.7：每个 removeItem(0) 前记录 journal 条目
                // v2.0.6.8：每个 removeItem(0) 后验证实际状态匹配 ExpectedStateAfter
                while (items.getItemCount() > 0)
                {
                    // 捕获 before 状态
                    var stateBefore = CaptureLightweightState(items);

                    // 计算 after 状态：移除第 0 项
                    var stateAfter = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(stateBefore.Count - 1);
                    for (int j = 1; j < stateBefore.Count; j++)
                        stateAfter.Add(stateBefore[j]);

                    // 在调用 removeItem 前记录 journal 条目
                    prep.MutationJournal.Add(new MutationJournalEntry
                    {
                        StepIndex = stepIdx++,
                        OperationType = "RemoveItem",
                        ExpectedStateBefore = stateBefore,
                        ExpectedStateAfter = stateAfter,
                        AddedItem = null,
                        RemoveItemIndex = 0,
                    });

                    // 执行 removeItem（可能在内部抛异常）
                    items.removeItem(0);

                    // v2.0.6.8 模板 D：post-call 状态验证
                    // 若 removeItem 返回但实际状态 != ExpectedStateAfter，说明原版 API 静默失败或被并发修改
                    // 抛异常触发 catch 块，由 TryRollbackPageWithPreCheck 检查当前状态是否匹配 journal
                    var actualAfterRemove = CaptureLightweightState(items);
                    if (!StateMatches(actualAfterRemove, stateAfter))
                    {
                        throw new InvalidOperationException(
                            $"removeItem(0) post-call state mismatch: expected {stateAfter.Count} items, " +
                            $"actual {actualAfterRemove.Count} items; vanilla API silent failure or concurrent modification");
                    }

                    // v2.0.6.10：Codex v2.0.6.9 审计 §三 P0-2 故障注入钩子
                    // 在 post-call 状态验证后，测试运行器可能抛 InvalidOperationException
                    // 该异常会被本方法 catch 块捕获，触发 MutationMayHaveStarted 路径
                    // v2.0.6.11：Codex v2.0.6.10 审计 §三 P0-1 修复：
                    //   - hook 用 #if TIDY_TEST_HARNESS 包裹，Release 为 no-op
                }

                // v2.0.6.7：每个 addItem 前记录 journal 条目
                // v2.0.6.8：每个 addItem 后验证实际状态匹配 ExpectedStateAfter
                // DEV-V5-04：待加入物品（Tag=Item 引用）也走同一 journal 协议
                // ——它的 addItem 失败/静默失配同样进入 MutationMayHaveStarted
                // 可验证回滚域；该件没有旧 jar，不进快捷键映射。
                object pendingTag = prep.Pending == null ? null : prep.Pending.Tag;
                for (int i = 0; i < prep.Result.Count; i++)
                {
                    PackableItem p = prep.Result[i];
                    if (!p.Placed) continue;
                    Item addTarget;
                    ItemJar jar = null;
                    if (p.Tag is ItemJar pageJar)
                    {
                        if (pageJar.item == null) continue;
                        jar = pageJar;
                        addTarget = pageJar.item;
                    }
                    else if (pendingTag != null && ReferenceEquals(p.Tag, pendingTag))
                    {
                        addTarget = (Item)p.Tag; // 待加入物品本体（生产=Items.addItem 新建 jar）
                    }
                    else continue;

                    // 捕获 before 状态
                    var stateBefore = CaptureLightweightState(items);

                    // 计算 after 状态：添加 (p.ResultX, p.ResultY, p.ResultRot, 物品指纹)
                    var stateAfter = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(stateBefore.Count + 1);
                    for (int j = 0; j < stateBefore.Count; j++)
                        stateAfter.Add(stateBefore[j]);
                    stateAfter.Add((p.ResultX, p.ResultY, p.ResultRot, new ItemFingerprint(addTarget)));

                    // 在调用 addItem 前记录 journal 条目
                    prep.MutationJournal.Add(new MutationJournalEntry
                    {
                        StepIndex = stepIdx++,
                        OperationType = "AddItem",
                        ExpectedStateBefore = stateBefore,
                        ExpectedStateAfter = stateAfter,
                        AddedItem = p,
                        RemoveItemIndex = -1,
                    });

                    // 执行 addItem（可能在内部抛异常）
                    items.addItem(p.ResultX, p.ResultY, p.ResultRot, addTarget);

                    // v2.0.6.8 模板 D：post-call 状态验证
                    var actualAfterAdd = CaptureLightweightState(items);
                    if (!StateMatches(actualAfterAdd, stateAfter))
                    {
                        throw new InvalidOperationException(
                            $"addItem post-call state mismatch: expected {stateAfter.Count} items, " +
                            $"actual {actualAfterAdd.Count} items; vanilla API silent failure or concurrent modification");
                    }

                    // v2.0.6.10：Codex v2.0.6.9 审计 §三 P0-2 故障注入钩子
                    // v2.0.6.11：Codex v2.0.6.10 审计 §三 P0-1 修复：#if 包裹，Release no-op

                    if (outMapping != null && jar != null)
                        outMapping[jar] = new NewPosition(prep.Page, p.ResultX, p.ResultY, p.ResultRot);
                }

                // ===== 离开 MutationMayHaveStarted 区间，进入 Committed =====
                prep.PostCommitJars = CapturePostCommitSnapshot(items, prep.Page);
                prep.CommitResult = CommitPageResult.Committed;
                return CommitPageResult.Committed;
            }
            catch (Exception e)
            {
                // v2.0.6.7：异常路径，状态为 MutationMayHaveStarted
                // journal 已记录每个写调用前的 before/after 预期状态
                // 调用方必须使用 TryRollbackPageWithPreCheck，它会：
                //   1. 若有 post-commit 快照：比较，不一致则 ConcurrentMutationAfterCommit
                //   2. 若无 post-commit 快照（本路径）：
                //      a. 若当前 == BeforeJars：removeItem 未执行，无需回滚
                //      b. 若当前匹配某个 journal[i].ExpectedStateBefore：操作 i 未执行，可安全按 BeforeJars 重建
                //      c. 若当前匹配某个 journal[i].ExpectedStateAfter：操作 i 已执行完，可安全按 BeforeJars 重建
                //      d. 否则：未知状态，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离 + 持久熔断
                LitRuntime.LogError(
                    $"[Tidy] page {prep.Page} Commit 中途异常（MutationMayHaveStarted，已记录 {prep.MutationJournal.Count} 个 journal 条目）: {e}");
                prep.CommitResult = CommitPageResult.MutationMayHaveStarted;
                return CommitPageResult.MutationMayHaveStarted;
            }
        }

        /// <summary>
        /// v2.0.6.5 新增：捕获 post-commit 快照。
        /// 在 CommitPage 成功后调用，记录本事务提交后的页面状态。
        /// 用于回滚前的写前比较：若回滚时当前状态 != post-commit 快照，说明已提交页被并发修改。
        /// </summary>
        private static List<JarSnapshot> CapturePostCommitSnapshot(Items items, byte page)
        {
            if (items == null) return new List<JarSnapshot>(0);
            byte count = items.getItemCount();
            var snapshots = new List<JarSnapshot>(count);
            for (byte i = 0; i < count; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null) continue;
                snapshots.Add(new JarSnapshot(jar));
            }
            return snapshots;
        }

        /// <summary>
        /// v2.0.6.5 新增：回滚前的 post-commit 写前比较。
        /// Codex v2.0.6.4 审计 §五阻断项 1 核心修复：
        ///   "回滚前以 x,y,rot,id,amount,quality,state 比较当前状态与该 post-commit 快照；
        ///    不一致则严禁 removeItem/重建，返回新的 ConcurrentMutationAfterCommit 安全失败"
        ///
        /// 返回值：
        ///   - true = 当前状态与 post-commit 快照一致，可安全回滚
        ///   - false = 当前状态已被并发修改，禁止回滚（调用方应返回 ConcurrentMutationAfterCommit）
        /// </summary>
        private static bool ValidatePostCommitUnchanged(Items items, List<JarSnapshot> postCommitJars, byte page)
        {
            if (items == null) return false;
            if (postCommitJars == null) return true;  // 无 post-commit 快照（空页面或异常路径），允许回滚

            byte currentCount = items.getItemCount();
            if (currentCount != postCommitJars.Count)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page} post-commit 写前比较失败：物品数量变更 postCommit={postCommitJars.Count}, current={currentCount}（已提交页被并发修改，禁止回滚）");
                return false;
            }

            // 构建 (x, y, rot, fingerprint) 元组列表，排序后逐项比较
            var currentTuples = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(currentCount);
            for (byte i = 0; i < currentCount; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null)
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {page} post-commit 写前比较失败：jar[{i}] 或 jar.item 为 null（已提交页被并发修改）");
                    return false;
                }
                currentTuples.Add((jar.x, jar.y, jar.rot, new ItemFingerprint(jar.item)));
            }

            var postTuples = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(postCommitJars.Count);
            for (int i = 0; i < postCommitJars.Count; i++)
            {
                var snap = postCommitJars[i];
                postTuples.Add((snap.X, snap.Y, snap.Rot, snap.Fingerprint));
            }

            if (currentTuples.Count != postTuples.Count)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page} post-commit 写前比较失败：元组数量不一致 postCommit={postTuples.Count}, current={currentTuples.Count}");
                return false;
            }

            currentTuples.Sort(CompareRollbackTuple);
            postTuples.Sort(CompareRollbackTuple);

            for (int i = 0; i < currentTuples.Count; i++)
            {
                var p = postTuples[i];
                var c = currentTuples[i];
                if (p.x != c.x || p.y != c.y || p.rot != c.rot)
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {page} post-commit 写前比较失败：坐标/旋转不匹配 postCommit=({p.x},{p.y},{p.rot}), current=({c.x},{c.y},{c.rot})（已提交页被并发修改，禁止回滚）");
                    return false;
                }
                if (!p.fp.Equals(c.fp))
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {page} post-commit 写前比较失败：指纹不匹配 at ({p.x},{p.y},{p.rot}) postCommit={{{p.fp.Id},{p.fp.Amount},{p.fp.Quality}}}, current={{{c.fp.Id},{c.fp.Amount},{c.fp.Quality}}}（已提交页被并发修改，禁止回滚）");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// v2.0.6.6 新增：捕获轻量状态（仅 x,y,rot,ItemFingerprint 元组列表）。
        /// 用于 mutation journal 的预期中间态记录，比 JarSnapshot 更轻量。
        /// </summary>
        private static List<(byte x, byte y, byte rot, ItemFingerprint fp)> CaptureLightweightState(Items items)
        {
            if (items == null) return new List<(byte, byte, byte, ItemFingerprint)>(0);
            byte count = items.getItemCount();
            var list = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(count);
            for (byte i = 0; i < count; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null) continue;
                list.Add((jar.x, jar.y, jar.rot, new ItemFingerprint(jar.item)));
            }
            return list;
        }

        /// <summary>
        /// v2.0.6.6 新增，v2.0.6.7 修订（Codex v2.0.6.6 审计 §三 Critical 1 修复）：
        /// 验证当前 items 状态是否匹配 mutation journal 中某个条目的 before 或 after 预期状态。
        ///
        /// v2.0.6.7 修订：
        ///   - journal 条目现在含 ExpectedStateBefore + ExpectedStateAfter
        ///   - 匹配 before[i]：操作 i 未执行（异常发生在 i 之前）
        ///   - 匹配 after[i]：操作 i 已执行完（异常发生在 i 之后）
        ///   - 两种情况都允许安全回滚到 BeforeJars
        ///
        /// Codex v2.0.6.6 审计 §三 Critical 1 修复要求：
        ///   "只有当前状态精确等于某个本事务预期中间态才允许逆向恢复；
        ///    任何未知状态均禁止清空/重建，进入持久隔离与人工恢复证据路径"
        ///
        /// 返回值：
        ///   - 匹配的 journal 条目索引（>=0）= 当前状态由本事务造成，可安全回滚
        ///   - -1 = 未知状态，禁止回滚（调用方应返回 ConcurrentMutationAfterCommit）
        /// </summary>
        private static int FindMatchingJournalEntry(Items items, List<MutationJournalEntry> journal, byte page)
        {
            if (items == null || journal == null || journal.Count == 0) return -1;

            byte currentCount = items.getItemCount();

            // 构建 current 元组列表（一次构建，多次复用）
            var currentTuples = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(currentCount);
            bool currentValid = true;
            for (byte i = 0; i < currentCount; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null) { currentValid = false; break; }
                currentTuples.Add((jar.x, jar.y, jar.rot, new ItemFingerprint(jar.item)));
            }
            if (!currentValid) return -1;
            currentTuples.Sort(CompareRollbackTuple);

            // 遍历 journal，找第一个匹配的 before 或 after 状态
            for (int idx = 0; idx < journal.Count; idx++)
            {
                MutationJournalEntry entry = journal[idx];

                // 检查 ExpectedStateBefore
                if (entry?.ExpectedStateBefore != null && currentCount == entry.ExpectedStateBefore.Count)
                {
                    if (StateMatches(currentTuples, entry.ExpectedStateBefore))
                        return idx;
                }

                // 检查 ExpectedStateAfter
                if (entry?.ExpectedStateAfter != null && currentCount == entry.ExpectedStateAfter.Count)
                {
                    if (StateMatches(currentTuples, entry.ExpectedStateAfter))
                        return idx;
                }
            }
            return -1;
        }

        /// <summary>
        /// v2.0.6.7 新增：比较 currentTuples 与 expected 列表。
        /// v2.0.6.12 修订（Codex v2.0.6.11 单机冒烟复盘 §3.1）：
        ///   消除"调用者必须预排序 currentSorted"的隐式契约。
        ///   Items 的容器枚举顺序不是业务不变量（vanilla removeItem(0) 可能交换末尾元素），
        ///   因此本函数对 current 与 expected 都做副本规范化排序后再逐项比较。
        ///   原列表不被修改（MutationJournal 必须保持记录时的原始顺序）。
        ///   比较字段 (x,y,rot,fp) 不放宽，任一差异仍返回 false。
        /// </summary>
        private static bool StateMatches(
            List<(byte x, byte y, byte rot, ItemFingerprint fp)> current,
            List<(byte x, byte y, byte rot, ItemFingerprint fp)> expected)
        {
            if (current == null || expected == null) return false;
            if (current.Count != expected.Count) return false;

            // Items 的枚举顺序不是业务不变量：复制后对两侧统一排序。
            // 禁止原地排序，MutationJournal 必须保持记录时的原始顺序。
            var currentCanonical =
                new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(current);
            var expectedCanonical =
                new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(expected);

            currentCanonical.Sort(CompareRollbackTuple);
            expectedCanonical.Sort(CompareRollbackTuple);

            for (int i = 0; i < currentCanonical.Count; i++)
            {
                var actual = currentCanonical[i];
                var wanted = expectedCanonical[i];
                if (actual.x != wanted.x || actual.y != wanted.y || actual.rot != wanted.rot)
                    return false;
                if (!actual.fp.Equals(wanted.fp))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// v2.0.6.5 新增：回滚前的 post-commit 写前比较。
        /// Codex v2.0.6.4 审计 §五阻断项 1 核心修复：
        ///   "回滚前以 x,y,rot,id,amount,quality,state 比较当前状态与该 post-commit 快照；
        ///    不一致则严禁 removeItem/重建，返回新的 ConcurrentMutationAfterCommit 安全失败"
        ///
        /// v2.0.6.6 修订（Codex v2.0.6.5 审计 §三 Critical 1 修复）：
        ///   异常路径（无 post-commit 快照）改为可验证 mutation journal：
        ///   - 当前 == BeforeJars：removeItem 未执行，无需回滚
        ///   - 当前匹配某个 journal 预期中间态：本事务造成，可安全按 BeforeJars 重建
        ///   - 否则：未知状态，禁止回滚，返回 ConcurrentMutationAfterCommit 安全隔离
        ///
        /// 返回值：
        ///   - true = 可安全回滚（或无需回滚）
        ///   - false = 检测到并发修改或未知状态，禁止回滚（调用方应返回 ConcurrentMutationAfterCommit）
        /// </summary>
        private static bool TryRollbackPageWithPreCheck(PagePreparation prep)
        {
            Items items = prep.ItemsInstance;
            if (items == null || prep.BeforeJars == null) return true;

            // v2.0.6.5：若有 post-commit 快照，先做写前比较
            if (prep.PostCommitJars != null)
            {
                if (!ValidatePostCommitUnchanged(items, prep.PostCommitJars, prep.Page))
                {
                    // 已提交页被并发修改，禁止回滚
                    LitRuntime.LogError(
                        $"[Tidy] page {prep.Page} post-commit 写前比较失败：禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离");
                    return false;
                }
            }
            else
            {
                // v2.0.6.7：异常路径（MutationMayHaveStarted），无 post-commit 快照
                // journal 已记录每个写调用前的 before/after 预期状态
                //   1. 若当前 == BeforeJars：removeItem 未执行，无需回滚
                //   2. 若当前匹配某个 journal[i].ExpectedStateBefore：操作 i 未执行，可安全按 BeforeJars 重建
                //   3. 若当前匹配某个 journal[i].ExpectedStateAfter：操作 i 已执行完，可安全按 BeforeJars 重建
                //   4. 否则：未知状态，禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离 + 持久熔断
                if (ValidateInventoryUnchanged(items, prep.BeforeJars, prep.Page))
                {
                    // 当前状态 == BeforeJars，说明 removeItem 未执行，无需回滚
                    TidyDiagnosticLog.Info("transaction-no-mutation",
                        $"page {prep.Page} 异常路径但当前状态 == Prepare 快照，removeItem 未执行，无需回滚。");
                    return true;
                }

                // 当前状态 != BeforeJars，遍历 mutation journal 验证是否由本事务造成
                int matchedIdx = FindMatchingJournalEntry(items, prep.MutationJournal, prep.Page);
                if (matchedIdx < 0)
                {
                    // 未知状态：禁止回滚
                    LitRuntime.LogError(
                        $"[Tidy] page {prep.Page} 异常路径：当前状态不匹配任何 mutation journal 条目的 before/after 预期状态，" +
                        $"禁止回滚，进入 ConcurrentMutationAfterCommit 安全隔离 + 持久熔断（人工恢复）");
                    return false;
                }

                // 当前状态匹配 journal[{matchedIdx}]，本事务造成，可安全按 BeforeJars 重建
                TidyDiagnosticLog.Info("transaction-journal-rollback",
                    $"page {prep.Page} 异常路径：当前状态匹配 mutation journal[{matchedIdx}] " +
                    $"({prep.MutationJournal[matchedIdx].OperationType})，可安全按 Prepare 快照重建");
            }

            // 执行回滚（按 BeforeJars 重建）
            return TryRollbackPage(prep);
        }

        // ─────────────────────────────────────────────────────────────
        // Verify 阶段：捕获重排后指纹 + 比对
        // ─────────────────────────────────────────────────────────────

        /// <summary>DEV-V5-05: internal for the host red tests (the Leave
        /// conservation shape) — behavior unchanged for every pre-existing
        /// caller. Post-commit conservation: the page must hold exactly
        /// before + pending - leaving fingerprints (DEV-V5-05 离场件 = the fast
        /// transfer's source side; 错摘/漏摘被抓, same law as 04 的禁吞物).</summary>
        internal static bool VerifyFingerprintConservation(PagePreparation prep)
        {
            Items items = prep.ItemsInstance;
            if (items == null) return true;

            // DEV-V5-04：待加入物品计入期望多重集（提交后页面必须多它一件；
            // 空页 + pending 也走完整守恒，不再被「空页面直接通过」短路）。
            // DEV-V5-05：离场件反向配平（提交后页面 = 原内容 - 它）。
            int expectedCount = (prep.BeforeJars == null ? 0 : prep.BeforeJars.Count)
                + (prep.Pending == null ? 0 : 1)
                - (prep.Leaving == null ? 0 : 1);

            // 空页面直接通过（仅当既无旧物也无 pending）
            if (expectedCount == 0) return true;

            var after = new List<ItemFingerprint>(expectedCount);
            byte count = items.getItemCount();
            for (byte i = 0; i < count; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null) continue;
                after.Add(new ItemFingerprint(jar.item));
            }

            if (after.Count != expectedCount)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {prep.Page} 守恒失败：before={(prep.BeforeJars == null ? 0 : prep.BeforeJars.Count)}" +
                    (prep.Pending != null ? "+pending" : "") + $", after={after.Count}");
                return false;
            }

            var beforeList = new List<ItemFingerprint>(expectedCount);
            var leaveJar = prep.Leaving == null ? null : prep.Leaving.Tag as ItemJar;
            var leaveRemoved = false;
            for (int i = 0; i < (prep.BeforeJars == null ? 0 : prep.BeforeJars.Count); i++)
            {
                if (leaveJar != null && !leaveRemoved && ReferenceEquals(prep.BeforeJars[i]?.OriginalJar, leaveJar))
                {
                    leaveRemoved = true;
                    continue;
                }
                beforeList.Add(prep.BeforeJars[i].Fingerprint);
            }
            if (leaveJar != null && !leaveRemoved) return false; // 离场件不在快照 = 配平失败
            if (prep.Pending != null && prep.Pending.Tag is Item pendingItem)
                beforeList.Add(new ItemFingerprint(pendingItem));

            beforeList.Sort(CompareFingerprint);
            after.Sort(CompareFingerprint);

            for (int i = 0; i < beforeList.Count; i++)
            {
                if (!beforeList[i].Equals(after[i]))
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {prep.Page} 守恒失败：diff[{i}] before={{id={beforeList[i].Id},amt={beforeList[i].Amount},q={beforeList[i].Quality}}}, " +
                        $"after={{id={after[i].Id},amt={after[i].Amount},q={after[i].Quality}}}");
                    return false;
                }
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // Rollback 阶段：按值快照重建原布局（v2.0.2 修订）
        // ─────────────────────────────────────────────────────────────

        private static bool TryRollbackPage(PagePreparation prep)
        {
            Items items = prep.ItemsInstance;
            if (items == null || prep.BeforeJars == null) return true;

            try
            {
                // 清空当前状态
                while (items.getItemCount() > 0)
                    items.removeItem(0);

                // v2.0.2：按值快照创建全新 Item 实例，避免共享可变引用
                for (int i = 0; i < prep.BeforeJars.Count; i++)
                {
                    var snap = prep.BeforeJars[i];
                    if (snap == null) continue;
                    Item rebuilt = snap.RecreateItem();
                    items.addItem(snap.X, snap.Y, snap.Rot, rebuilt);
                }

                // v2.0.2：回滚验证必须同时验证物品多重集合 + 原坐标 + 原旋转 + 页面尺寸
                if (!VerifyRollbackRestoration(prep))
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {prep.Page} 回滚后验证失败：坐标/旋转/指纹未完全恢复");
                    return false;
                }

                TidyDiagnosticLog.Info("transaction-rollback-verified",
                    $"page {prep.Page} 回滚成功，原布局已按值快照恢复并验证通过。");
                return true;
            }
            catch (Exception e)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {prep.Page} 回滚异常: {e}");
                return false;
            }
        }

        /// <summary>
        /// v2.0.2 新增：回滚后逐坐标验证 id+amount+quality+state+x+y+rot 完全恢复。
        /// 任何坐标、旋转或指纹差异都判定恢复失败。
        /// </summary>
        private static bool VerifyRollbackRestoration(PagePreparation prep)
        {
            Items items = prep.ItemsInstance;
            if (items == null) return false;

            // 页面尺寸必须一致
            if (items.width != prep.Width || items.height != prep.Height)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {prep.Page} 回滚验证失败：页面尺寸 before={prep.Width}x{prep.Height}, after={items.width}x{items.height}");
                return false;
            }

            // 构建 after jar 列表
            byte afterCount = items.getItemCount();
            if (afterCount != prep.BeforeJars.Count)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {prep.Page} 回滚验证失败：jar 数量 before={prep.BeforeJars.Count}, after={afterCount}");
                return false;
            }

            // 逐坐标验证：每个 before jar 必须在相同 (x,y,rot) 位置有相同指纹的 jar
            var afterJars = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(afterCount);
            for (byte i = 0; i < afterCount; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null)
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {prep.Page} 回滚验证失败：after jar[{i}] 为 null");
                    return false;
                }
                afterJars.Add((jar.x, jar.y, jar.rot, new ItemFingerprint(jar.item)));
            }

            // 多重集合匹配：(x, y, rot, fingerprint) 必须完全一致
            var beforeTuples = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(prep.BeforeJars.Count);
            for (int i = 0; i < prep.BeforeJars.Count; i++)
            {
                var snap = prep.BeforeJars[i];
                beforeTuples.Add((snap.X, snap.Y, snap.Rot, snap.Fingerprint));
            }

            // 排序后逐项比较
            beforeTuples.Sort(CompareRollbackTuple);
            afterJars.Sort(CompareRollbackTuple);

            for (int i = 0; i < beforeTuples.Count; i++)
            {
                var b = beforeTuples[i];
                var a = afterJars[i];
                if (b.x != a.x || b.y != a.y || b.rot != a.rot)
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {prep.Page} 回滚验证失败：坐标/旋转不匹配 before=({b.x},{b.y},{b.rot}), after=({a.x},{a.y},{a.rot})");
                    return false;
                }
                if (!b.fp.Equals(a.fp))
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {prep.Page} 回滚验证失败：指纹不匹配 at ({b.x},{b.y},{b.rot}) before={{{b.fp.Id},{b.fp.Amount},{b.fp.Quality}}}, after={{{a.fp.Id},{a.fp.Amount},{a.fp.Quality}}}");
                    return false;
                }
            }
            return true;
        }

        private static int CompareRollbackTuple(
            (byte x, byte y, byte rot, ItemFingerprint fp) a,
            (byte x, byte y, byte rot, ItemFingerprint fp) b)
        {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            if (a.y != b.y) return a.y.CompareTo(b.y);
            if (a.rot != b.rot) return a.rot.CompareTo(b.rot);
            return CompareFingerprint(a.fp, b.fp);
        }

        private static bool TryRollbackAll(List<PagePreparation> preps)
        {
            bool allOk = true;
            for (int i = 0; i < preps.Count; i++)
            {
                if (!TryRollbackPage(preps[i]))
                    allOk = false;
            }
            return allOk;
        }

        /// <summary>
        /// v2.0.6.5 新增：带 post-commit 写前比较的全局回滚。
        /// 若任一已提交页被并发修改，立即停止回滚并返回 false（ConcurrentMutationAfterCommit）。
        /// 已成功回滚的页面不撤销（部分回滚状态由调用方记录）。
        /// </summary>
        private static bool TryRollbackAllWithPreCheck(List<PagePreparation> preps)
        {
            bool allOk = true;
            for (int i = 0; i < preps.Count; i++)
            {
                if (!TryRollbackPageWithPreCheck(preps[i]))
                {
                    allOk = false;
                    break;  // 检测到并发修改，立即停止
                }
            }
            return allOk;
        }

        /// <summary>
        /// v2.0.6.4 修订：通用范围回滚，回滚 preps[startIdx..endIdxInclusive]。
        /// 替代 v2.0.3 的 TryRollbackCommittedPages（语义更明确）。
        /// </summary>
        private static bool TryRollbackRange(List<PagePreparation> preps, int startIdx, int endIdxInclusive)
        {
            if (startIdx < 0 || endIdxInclusive < 0) return true;  // 空范围
            if (startIdx > endIdxInclusive) return true;  // 空范围
            if (endIdxInclusive >= preps.Count) endIdxInclusive = preps.Count - 1;
            if (startIdx >= preps.Count) return true;  // 空范围

            bool allOk = true;
            for (int i = startIdx; i <= endIdxInclusive; i++)
            {
                if (!TryRollbackPage(preps[i]))
                    allOk = false;
            }
            return allOk;
        }

        /// <summary>
        /// v2.0.6.5 新增：带 post-commit 写前比较的范围回滚。
        /// 若任一已提交页被并发修改，立即停止回滚并返回 false（ConcurrentMutationAfterCommit）。
        /// 已成功回滚的页面不撤销（部分回滚状态由调用方记录）。
        /// </summary>
        private static bool TryRollbackRangeWithPreCheck(List<PagePreparation> preps, int startIdx, int endIdxInclusive)
        {
            if (startIdx < 0 || endIdxInclusive < 0) return true;  // 空范围
            if (startIdx > endIdxInclusive) return true;  // 空范围
            if (endIdxInclusive >= preps.Count) endIdxInclusive = preps.Count - 1;
            if (startIdx >= preps.Count) return true;  // 空范围

            bool allOk = true;
            for (int i = startIdx; i <= endIdxInclusive; i++)
            {
                if (!TryRollbackPageWithPreCheck(preps[i]))
                {
                    allOk = false;
                    break;  // 检测到并发修改，立即停止
                }
            }
            return allOk;
        }

        // ─────────────────────────────────────────────────────────────
        // 静态验证工具
        // ─────────────────────────────────────────────────────────────

        private static bool ValidateNoOverlap(IReadOnlyList<PackableItem> result, byte width, byte height)
        {
            if (result == null) return true;
            bool[,] grid = new bool[width, height];
            for (int i = 0; i < result.Count; i++)
            {
                PackableItem p = result[i];
                if (p == null || !p.Placed) continue;
                byte w = (p.ResultRot & 1) == 1 ? p.size_y : p.size_x;
                byte h = (p.ResultRot & 1) == 1 ? p.size_x : p.size_y;
                int endX = p.ResultX + w;
                int endY = p.ResultY + h;
                if (endX > width || endY > height) return false;
                for (int x = p.ResultX; x < endX; x++)
                    for (int y = p.ResultY; y < endY; y++)
                        if (grid[x, y]) return false;
                        else grid[x, y] = true;
            }
            return true;
        }

        private static bool ValidateBounds(IReadOnlyList<PackableItem> result, byte width, byte height)
        {
            if (result == null) return true;
            for (int i = 0; i < result.Count; i++)
            {
                PackableItem p = result[i];
                if (p == null || !p.Placed) continue;
                byte w = (p.ResultRot & 1) == 1 ? p.size_y : p.size_x;
                byte h = (p.ResultRot & 1) == 1 ? p.size_x : p.size_y;
                if (p.ResultX + w > width || p.ResultY + h > height) return false;
                if (p.ResultX >= width || p.ResultY >= height) return false;
            }
            return true;
        }

        private static int CountPlaced(IReadOnlyList<PackableItem> result)
        {
            if (result == null) return 0;
            int count = 0;
            for (int i = 0; i < result.Count; i++)
                if (result[i] != null && result[i].Placed) count++;
            return count;
        }

        // ─────────────────────────────────────────────────────────────
        // v2.0.6.3 新增：并发安全加固（主线程断言 + Commit 前二次库存校验）
        // v2.0.6.4 修订：主线程断言改为返回 bool（不抛异常），失败时调用方返回 RejectedNoMutation
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// v2.0.6.4 修订（Codex v2.0.6.3 审计 §五阻断项 2）：
        /// 主线程检查改为返回 bool，失败时返回 false，调用方返回 RejectedNoMutation。
        /// 不再抛 InvalidOperationException，避免被上层 catch 进入 CriticalFailure + 持久熔断。
        ///
        /// Codex 审计原意：主线程断言不等于线程安全，但断言失败仅是"拒绝/排队"信号，
        /// 不应进入持久熔断。旧联机路径的玩家级互斥（PlayerOperationGate）属 DEV-V2-21。
        /// </summary>
        /// <param name="operationName">操作名（仅用于日志）。</param>
        /// <returns>true = 当前线程为 Unity 主线程；false = 非主线程（调用方应返回 Rejected）。</returns>
        private static bool IsMainThread(string operationName)
        {
            int mainId = LitRuntime.MainThreadId;
            if (mainId == 0)
            {
                // 插件 Awake 未完成（理论上不可能，因为 Awake 会缓存 MainThreadId 并先调用 Guard）
                LitRuntime.LogError(
                    $"[Tidy] {operationName}: MainThreadId 未初始化（模块启动未完成），拒绝整理");
                return false;
            }
            int currentId = Thread.CurrentThread.ManagedThreadId;
            if (currentId != mainId)
            {
                LitRuntime.LogError(
                    $"[Tidy] {operationName}: 必须在 Unity 主线程执行（currentThreadId={currentId}, mainThreadId={mainId}），拒绝整理（不触发熔断）");
                return false;
            }
            return true;
        }

        /// <summary>
        /// v2.0.6.3 P0：Commit 前二次库存校验。在 Prepare（捕获快照）和 Commit（写入背包）两个状态之间，
        /// 重新读取原版背包容器，验证物品总数 + 每个 ItemJar 的 (x, y, rot, 指纹) 与 Prepare 阶段快照完全一致。
        /// 任何不一致（说明玩家在此期间手动拿走了物品，或发生了其他线程篡改）-> 立即返回 false，
        /// 调用方（CommitPage）必须无条件撤销整笔整理事务（抛异常触发上层 catch -> CriticalFailure + 回滚）。
        ///
        /// 此校验防止的攻击场景：
        ///   1. 客户端在 Prepare 后、Commit 前发送第二个整理请求（已被 1.5s 冷却闸门限制但作双保险）
        ///   2. 玩家在 Prepare 后、Commit 前手动拖动物品到其他页面（导致 removeItem 时索引错位）
        ///   3. 其他模组的 Patch 在 Prepare 后修改了背包（破坏快照-实际一致性）
        ///   4. 旧联机路径的畸形包延迟到达绕过账本防重放（单人路径无该入口，联机属 DEV-V2-21）
        /// </summary>
        /// <param name="items">原版 Items 实例。</param>
        /// <param name="beforeJars">Prepare 阶段捕获的快照列表。</param>
        /// <param name="page">页面索引（仅用于日志）。</param>
        /// <returns>true = 库存未变更，可继续 Commit；false = 库存已变更，必须 Abort。</returns>
        private static bool ValidateInventoryUnchanged(Items items, List<JarSnapshot> beforeJars, byte page)
        {
            if (items == null) return false;
            if (beforeJars == null) return false;

            byte currentCount = items.getItemCount();
            if (currentCount != beforeJars.Count)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page} Commit 前二次校验失败：物品数量变更 before={beforeJars.Count}, current={currentCount}（Prepare 与 Commit 之间物品被篡改）");
                return false;
            }

            // 构建 (x, y, rot, fingerprint) 元组列表，排序后逐项比较
            var currentTuples = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(currentCount);
            for (byte i = 0; i < currentCount; i++)
            {
                ItemJar jar = items.getItem(i);
                if (jar?.item == null)
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {page} Commit 前二次校验失败：jar[{i}] 或 jar.item 为 null（Prepare 与 Commit 之间物品被篡改）");
                    return false;
                }
                currentTuples.Add((jar.x, jar.y, jar.rot, new ItemFingerprint(jar.item)));
            }

            var beforeTuples = new List<(byte x, byte y, byte rot, ItemFingerprint fp)>(beforeJars.Count);
            for (int i = 0; i < beforeJars.Count; i++)
            {
                var snap = beforeJars[i];
                beforeTuples.Add((snap.X, snap.Y, snap.Rot, snap.Fingerprint));
            }

            // 数量已校验，理论上 Count 相等；防御性二次校验
            if (currentTuples.Count != beforeTuples.Count)
            {
                LitRuntime.LogError(
                    $"[Tidy] page {page} Commit 前二次校验失败：元组数量不一致 before={beforeTuples.Count}, current={currentTuples.Count}");
                return false;
            }

            currentTuples.Sort(CompareRollbackTuple);
            beforeTuples.Sort(CompareRollbackTuple);

            for (int i = 0; i < currentTuples.Count; i++)
            {
                var b = beforeTuples[i];
                var c = currentTuples[i];
                if (b.x != c.x || b.y != c.y || b.rot != c.rot)
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {page} Commit 前二次校验失败：坐标/旋转不匹配 before=({b.x},{b.y},{b.rot}), current=({c.x},{c.y},{c.rot})");
                    return false;
                }
                if (!b.fp.Equals(c.fp))
                {
                    LitRuntime.LogError(
                        $"[Tidy] page {page} Commit 前二次校验失败：指纹不匹配 at ({b.x},{b.y},{b.rot}) before={{{b.fp.Id},{b.fp.Amount},{b.fp.Quality}}}, current={{{c.fp.Id},{c.fp.Amount},{c.fp.Quality}}}");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 引用相等比较器（用于 ValidateTagConsistency 中的 HashSet<ItemJar>）。
    /// ItemJar 没有重写 Equals/GetHashCode，默认就是引用相等，但显式声明更清晰。
    /// </summary>
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
        private static readonly ReferenceEqualityComparer<T> _instance =
            new ReferenceEqualityComparer<T>();

        public static ReferenceEqualityComparer<T> Instance => _instance;

        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => obj == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
