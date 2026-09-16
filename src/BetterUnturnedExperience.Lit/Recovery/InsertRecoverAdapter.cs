using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-04 (V5-T5): the 入包恢复 adapter — 拾取/合成因碎洞失败时，主机把
    /// 玩家五页连同这件新东西交给统一排版再试一次；仍塞不进则东西留原处、背包
    /// 不动。Reached ONLY from the tryAddItemAuto failure point AND only while
    /// a verified pickup/craft RPC (V5-T5 Q2「第一版只接线主机权威拾取/合成」)
    /// holds the scope open.
    ///
    /// The gates, in frozen order (every refusal mutates nothing and answers
    /// with a diagnostic identity — 禁止可点但静默没动静 的服务器侧同律):
    ///   1. scope closed        → not-wired-context（其它获得路径不自动享受）
    ///   2. no live registration→ module-not-registered（生命周期登记=唯一开关；
    ///      补丁随 LIT Start 挂上、Stop/隔离 摘掉 — V1.4.1 审计禁 Prefix 内
    ///      死开关，本类型没有任何功能启停 bool）
    ///   3. fault circuit open  → fault-circuit-open（与按钮整理同一熔断）
    ///   4. item / asset gate   → item-blocked（null item、null asset、isPro —
    ///      原版 tryAddItemAuto 的门禁逐条照抄，恢复从不绕行）
    ///   5. saved preference    → preference-unreadable（Q59 同规：方向收尾
    ///      偏好读已保存快照，读不到不发明默认）
    ///   6. planner             → cannot-fit / no-active-player-pages
    ///   7. transaction         → commit-rejected / commit-critical
    ///
    /// Success = the SAME atomic all-pages transaction the button 全身整理
    /// commits (DEV-V5-02 plan through ManualTidyService's shared commit exit,
    /// pending-item aware since this ticket — 待加入物品不在提交结果内不算成
    /// 功，禁吞物；失败回滚零修改)。成功不发布 TidyCompleted、不触发压弹/合匣
    /// (V5-T5 Q5)，只记内部诊断；随后复刻原版的两个成功尾巴：
    ///   - autoEquipUseable 时给可用物装手（ServerEquip，真机侧）；
    ///   - 本地玩家背包走既有快捷键重绑链（远端玩家的服务器侧 _hotkeys 为
    ///     null，无从重绑 = 与原版自身移物同款遗留，具名见票面设计节）。
    /// </summary>
    internal static class InsertRecoverAdapter
    {
        /// <summary>The pending item's recovery-relevant facts (asset gate +
        /// footprint). Blocked = the vanilla auto-add gate would refuse it
        /// (null asset / isPro / non-positive size) — recovery never bypasses.</summary>
        internal struct PendingInfo
        {
            public bool Blocked;
            public byte SizeX;
            public byte SizeY;
        }

        /// <summary>Attempt result: recovered ⇔ the transaction committed with
        /// the pending item placed. Refusal carries the diagnostic identity.</summary>
        internal sealed class Attempt
        {
            public bool Recovered;
            public string Refusal;
            public byte PendingPage;
            public TidyCommitResult Commit;
        }

        // ── refusal identities (single source for log + red tests) ──
        internal const string RefusalOutOfScope = "not-wired-context";
        internal const string RefusalNotRunning = "module-not-registered";
        internal const string RefusalFaultCircuit = "fault-circuit-open";
        internal const string RefusalInvalidItem = "item-blocked";
        internal const string RefusalPreference = "preference-unreadable";
        internal const string RefusalNoActivePages = "no-active-player-pages";
        internal const string RefusalCannotFit = "cannot-fit";
        internal const string RefusalCommitRejected = "commit-rejected";
        internal const string RefusalCommitCritical = "commit-critical";

        /// <summary>Live registration: set when the recovery patch trio is
        /// armed (module Start / a new generation re-enabling), nulled on
        /// Stop/isolation. THE switch — not a config bool.</summary>
        internal static InventoryTidyModule ActiveModule;

        /// <summary>Host-test seams (the PageTransactionForTests family):
        /// replace the engine asset read and the all-pages commit — outside
        /// the game process Items.addItem JITs SDG.Unturned.Assets and throws
        /// (DEV-V5-03 具名缺口), so the host pins plan consumption and refusal
        /// shapes with same-semantics fakes while the real commit chain rides
        /// DEV-V5-08 machine acceptance.</summary>
        internal static Func<Item, PendingInfo?> PendingProbeForTests;
        internal static Func<List<PagePreparation>, Dictionary<ItemJar, NewPosition>, TidyOperationOutcome> CommitForTests;
        /// <summary>DEV-V5-04 host seam (see InsertRecoverEngine.ReadPages):
        /// supplies the page array without the PlayerInventory type ever
        /// being touched — its static initializer is unreachable off-game
        /// (the DEV-V5-03 MOUNT_PAGE lesson). Null = production read.</summary>
        internal static Func<PlayerInventory, Items[]> PagesForTests;

        private static bool recovering; // re-entrancy tripwire (single attempt)

        /// <summary>The one entry: vanilla tryAddItemAuto has just returned
        /// false inside a verified pickup/craft call. Never mutates anything
        /// unless the returned attempt is Recovered.</summary>
        internal static Attempt TryRecover(PlayerInventory inv, Item item, bool autoEquipUseable)
        {
            var pages = PagesForTests != null ? PagesForTests(inv) : InsertRecoverEngine.ReadPages(inv);
            return TryRecoverPages(pages, inv, item, autoEquipUseable);
        }

        /// <summary>The engine-free core. <paramref name="owner"/> is the
        /// PlayerInventory the add targets — null on the host, so every
        /// engine-facing tail (hotkey capture/rebind, the vanilla useable-equip
        /// tail, the listen-host projection reconcile) stays un-JITted here
        /// (the Mono ECall rule the LIT/LIR probes follow).</summary>
        internal static Attempt TryRecoverPages(Items[] pages, object owner, Item item, bool autoEquipUseable)
        {
            var attempt = new Attempt { Commit = TidyCommitResult.Rejected };
            try
            {
                if (!InsertRecoverScope.IsOpen) return Refuse(attempt, RefusalOutOfScope, item);
                if (recovering) return Refuse(attempt, RefusalOutOfScope, item); // 恰一次，不自递归
                // Registration IS the lifecycle fact: ActiveModule is handed
                // out inside EnsureStarted (which sets Started first) and
                // revoked by Stop/uninstall — a live handle therefore already
                // implies a started generation; ShuttingDown covers the stop
                // window between the flag flip and the revocation.
                var module = ActiveModule;
                if (module == null || module.ShuttingDown)
                    return Refuse(attempt, RefusalNotRunning, item);
                if (!module.FaultGate.Allowed) return Refuse(attempt, RefusalFaultCircuit, item);
                if (item == null) return Refuse(attempt, RefusalInvalidItem, item);

                PendingInfo info;
                if (!TryDescribePending(item, out info)) return Refuse(attempt, RefusalInvalidItem, item);

                TidyMode mode;
                bool sortDescending;
                if (!module.TryReadSavedTidyPreference(out mode, out sortDescending, out _))
                    return Refuse(attempt, RefusalPreference, item);

                List<PagePreparation> preps;
                byte pendingPage;
                string refusal;
                recovering = true;
                try
                {
                    if (!InsertRecoverPlanner.TryBuild(pages, item, info, sortDescending, mode, module.Strategy,
                            out preps, out pendingPage, out refusal))
                    {
                        return Refuse(attempt, refusal ?? RefusalCannotFit, item);
                    }

                    // Hotkeys ride the SAME chain the button tidy restores with,
                    // but only while the tidied inventory IS the local player's
                    // (server-side _hotkeys for remote players are null — the
                    // vanilla-parity limitation named on the ticket). The whole
                    // engine tail is owner-gated: with no owner (host) none of
                    // its Unity-touched bodies is ever entered/JITted.
                    var mapping = new Dictionary<ItemJar, NewPosition>();
                    object hotkeyBundle = null;
                    if (owner != null)
                        hotkeyBundle = InsertRecoverEngine.CaptureLocalHotkeys(owner);

                    TidyOperationOutcome outcome;
                    try
                    {
                        var seam = CommitForTests;
                        outcome = seam != null
                            ? seam(preps, mapping)
                            : ManualTidyService.CommitPreparations(preps, mapping);
                    }
                    catch (Exception error)
                    {
                        LitRuntime.LogError("[入包恢复] 提交流程异常（未信任其写面）: " + error.Message);
                        outcome = new TidyOperationOutcome { Result = TidyCommitResult.Rejected };
                    }

                    if (outcome != null && outcome.Result == TidyCommitResult.Committed)
                    {
                        if (owner != null)
                            InsertRecoverEngine.AfterCommitted(hotkeyBundle, owner, item, preps, pendingPage, mapping, autoEquipUseable);
                        TidyDiagnosticLog.Info("insert-recover-committed",
                            "[入包恢复] 五页连同待加入物品经统一排版提交成功（page=" + pendingPage +
                            ", strategy=" + module.Strategy.StrategyId + "）；未发布整理完成。");
                        attempt.Recovered = true;
                        attempt.Refusal = null;
                        attempt.PendingPage = pendingPage;
                        attempt.Commit = TidyCommitResult.Committed;
                        return attempt;
                    }

                    var result = outcome == null ? TidyCommitResult.Rejected : outcome.Result;
                    if (owner != null)
                        InsertRecoverEngine.AfterFailed(hotkeyBundle, owner, outcome);
                    if (result == TidyCommitResult.CriticalFailure || result == TidyCommitResult.ConcurrentMutationAfterCommit)
                    {
                        module.FaultGate.Open("insert-recover " + result + ": " +
                            (outcome != null && outcome.FailureReason != null ? outcome.FailureReason : "no detail"),
                            restoreVerified: outcome != null && outcome.RollbackVerified);
                        return Refuse(attempt, RefusalCommitCritical, item);
                    }
                    return Refuse(attempt, RefusalCommitRejected, item);
                }
                finally
                {
                    recovering = false;
                }
            }
            catch (Exception error)
            {
                // The adapter never throws back into the vanilla add: an
                // unexpected fault answers vanilla failure, zero mutation.
                LitRuntime.LogError("[入包恢复] 恢复流程崩溃（保持原版失败语义）: " + error.Message);
                return Refuse(attempt, RefusalCannotFit, item);
            }
        }

        private static Attempt Refuse(Attempt attempt, string refusal, Item item)
        {
            attempt.Refusal = refusal;
            LitRuntime.LogWarning("[入包恢复] 拒绝（" + refusal + (item == null ? "" : ", id=" + item.id) + "）：保持原版失败，背包未动。");
            return attempt;
        }

        /// <summary>The pending item's facts: the test seam when set, else the
        /// engine asset read. Null/blocked → the vanilla gate refuses, so
        /// recovery refuses identically.</summary>
        internal static bool TryDescribePending(Item item, out PendingInfo info)
        {
            info = default(PendingInfo);
            var seam = PendingProbeForTests;
            if (seam != null)
            {
                var described = seam(item);
                if (described == null) return false;
                info = described.Value;
            }
            else
            {
                if (!InsertRecoverEngine.TryDescribeAsset(item, out info)) return false;
            }
            return !info.Blocked && info.SizeX > 0 && info.SizeY > 0;
        }
    }
}
