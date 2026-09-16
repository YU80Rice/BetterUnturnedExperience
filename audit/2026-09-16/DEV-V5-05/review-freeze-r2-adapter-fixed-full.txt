using System;
using System.Collections.Generic;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-05 (V5-T5): the 快速转移恢复 adapter — the second recovery
    /// trigger, for the path vanilla's insert patch can never see (Ctrl+right-
    /// click goes through sendDragItem, NOT tryAddItem). Two roles, one core:
    ///
    /// CLIENT SURFACE (RequestFromIntent — called by the onSelectedItem
    /// Finalizer only when vanilla sent nothing): lifecycle/fault/preference
    /// gates first (same order the button click path obeys, Q59 read), then the
    /// server role runs the SAME authoritative execution locally (SP / listen
    /// host — never a fall-back to unauthoritative local grids), a true client
    /// sends the feature-private request (kind 9) bound to kind+fingerprint.
    ///
    /// AUTHORITY CORE (TryRecover — SP local turn AND the remote-admitted
    /// request, no entry copies the decision): the gates, in frozen order
    /// (every refusal mutates nothing and answers with a structured reason —
    /// 失败则两边都不动):
    ///   1. registration revoked / shutting down → module-not-registered
    ///      (生命周期登记=唯一开关; no feature bool, no dead switch)
    ///   2. fault circuit open                    → fault-circuit-open
    ///   3. request shape                         → request-invalid
    ///      (source page must be 2..6 or the storage mount 7; the claimed kind
    ///      must have a 03 adapter — virtual/unknown refuses here too)
    ///   4. 03 session re-verification            → the container reason
    ///      (会话 → 种类 → 权限 → 版本，权威从真 jar 现读指纹)
    ///   5. planner (source identity + 02 target) → cannot-fit → LayoutFailed
    ///   6. ONE two-page CommitPreparations       → rejected → commit-rejected,
    ///      critical/concurrent → commit-critical + 同一把熔断
    ///
    /// Success = the moved item is on the receiving plan AND committed with the
    /// source removal in the SAME transaction (禁「转移失败但目标已被单独整理」
    /// is structural: there is only one commit exit). 成功不发布 TidyCompleted、
    /// 不触发压弹/合匣、不新增公开事件 (V5-T5 Q5) — only an internal diagnostic,
    /// then the 04-style tail: local hotkey rebind (mapping-domain filtered) +
    /// listen-host projection reconcile. Failure keeps vanilla's silence (this
    /// is not a button; 「可点但静默失败」 law never promises a toast here).
    /// </summary>
    internal static class FastTransferRecoverAdapter
    {
        // ── refusal identities (single source for log + red tests; the 04
        // vocabulary repeats deliberately — each recovery names its own gates) ──
        internal const string RefusalNotRunning = "module-not-registered";
        internal const string RefusalFaultCircuit = "fault-circuit-open";
        internal const string RefusalInvalidRequest = "request-invalid";
        internal const string RefusalCommitRejected = "commit-rejected";
        internal const string RefusalCommitCritical = "commit-critical";

        /// <summary>Attempt result: Recovered ⇔ the two-page transaction
        /// committed with the moved item on the receiving side. Reason is the
        /// 03 structured reason (frozen 8-cause vocabulary — no new enum),
        /// None on success.</summary>
        internal sealed class Attempt
        {
            public bool Recovered;
            public string Refusal;
            public LitContainerTidyReason Reason;
            public TidyCommitResult Commit;
            public byte TargetPage;
        }

        /// <summary>Live registration: set when the fast-transfer patch pair is
        /// armed (module Start), nulled on Stop/isolation. THE switch.</summary>
        internal static InventoryTidyModule ActiveModule;

        /// <summary>Host-test seam (04's CommitForTests family): replaces the
        /// two-page commit — outside the game process Items.addItem JITs
        /// SDG.Unturned.Assets (the DEV-V5-03 named gap), so the host pins the
        /// plan-consumption shape with a same-semantics fake while the real
        /// commit chain rides DEV-V5-08 machine acceptance.</summary>
        internal static Func<List<PagePreparation>, Dictionary<ItemJar, NewPosition>, TidyOperationOutcome> CommitForTests;

        private static bool recovering; // re-entrancy tripwire (single attempt)

        /// <summary>The client surface entry (onSelectedItem Finalizer → here
        /// when vanilla sent nothing). Never throws back into the UI: every
        /// answer is a result code the caller logs; the player-visible outcome
        /// of a refusal is vanilla's own silence.</summary>
        internal static LitTidyRequestResult RequestFromIntent(FastTransferIntent intent)
        {
            try
            {
                var module = ActiveModule;
                if (module == null || module.ShuttingDown)
                {
                    LitRuntime.LogInfo("[快速转移恢复] 未登记（生命周期=唯一开关）：保持原版，不请求。");
                    return LitTidyRequestResult.NativeFallback;
                }
                return module.RequestFastTransferRecoverFromIntent(intent);
            }
            catch (Exception error)
            {
                LitRuntime.LogError("[快速转移恢复] 请求路径异常（保持原版失败语义）: " + error.Message);
                return LitTidyRequestResult.NativeFallback;
            }
        }

        /// <summary>
        /// The engine-free authority core — BOTH the SP local turn and the
        /// multiplayer authority run through here (03's single-execution-exit
        /// rule). <paramref name="owner"/> is the PlayerInventory the source
        /// page array belongs to (null on the host so no engine-facing tail is
        /// ever JITted); remote peers reach it through the production authority.
        /// </summary>
        internal static Attempt TryRecover(
            object owner, Items[] pages, Items containerGrid, LitContainerLiveFacts live,
            LitContainerTidyClaim claim, byte sourcePage, byte sourceX, byte sourceY,
            bool sortDescending)
        {
            var attempt = new Attempt { Commit = TidyCommitResult.Rejected };
            try
            {
                var module = ActiveModule;
                if (module == null || module.ShuttingDown)
                    return Refuse(attempt, RefusalNotRunning, LitContainerTidyReason.FeatureUnavailable, sourcePage);
                if (!module.FaultGate.Allowed)
                    return Refuse(attempt, RefusalFaultCircuit, LitContainerTidyReason.FeatureUnavailable, sourcePage);
                if (recovering)
                    return Refuse(attempt, RefusalInvalidRequest, LitContainerTidyReason.LayoutFailed, sourcePage);

                // Gate 3 — request shape (the codec already refuses 0/1/8/255 on
                // the wire; the authority never trusts that: same refusal here).
                var sourceIsPlayerPage = sourcePage >= HotkeySnapshotUtil.TIDYABLE_PAGE_MIN
                    && sourcePage <= HotkeySnapshotUtil.TIDYABLE_PAGE_MAX;
                var sourceIsContainer = sourcePage == LitContainerTidyExecution.MOUNT_PAGE;
                if (!sourceIsPlayerPage && !sourceIsContainer)
                    return Refuse(attempt, RefusalInvalidRequest, LitContainerTidyReason.UnsupportedKind, sourcePage);
                if (LitContainerSessionAdapters.Resolve(claim.Kind) == null)
                    return Refuse(attempt, RefusalInvalidRequest, LitContainerTidyReason.UnsupportedKind, sourcePage);
                if (containerGrid == null)
                    return Refuse(attempt, RefusalInvalidRequest, LitContainerTidyReason.ContainerClosed, sourcePage);

                // Gate 4 — the 03 verifier, authority-side, from the REAL jars
                // (会话→种类→权限→版本; same ordered law and same single source
                // as the container button tidy — no parallel rule here).
                var checkedLive = live;
                checkedLive.LiveFingerprint = LitContainerContentFingerprint.FromItems(containerGrid);
                var reason = LitContainerTidyVerifier.Verify(checkedLive, claim);
                if (reason != LitContainerTidyReason.None)
                    return Refuse(attempt, "session-" + reason, reason, sourcePage);

                recovering = true;
                try
                {
                    List<PagePreparation> preps;
                    byte targetPage;
                    string refusal;
                    if (!FastTransferRecoverPlanner.TryBuild(pages, containerGrid, sourcePage, sourceX, sourceY,
                            sortDescending, TidyMode.SameType, module.Strategy, out preps, out targetPage, out refusal))
                    {
                        var layoutReason = refusal == FastTransferRecoverPlanner.RefusalSourceGone
                            ? LitContainerTidyReason.ContentChanged   // 源件已走 = 内容已变
                            : LitContainerTidyReason.LayoutFailed;      // 放不下 = 排版失败
                        return Refuse(attempt, refusal ?? FastTransferRecoverPlanner.RefusalCannotFit, layoutReason, sourcePage);
                    }

                    // Hotkeys ride 04's chain but filtered to the MOVED page:
                    // capture before the commit (coordinates die in the rewrite),
                    // rebind only after. Container pages carry no hotkeys, so a
                    // player→container commit rebinds nothing (the source page's
                    // identity entries map to their ORIGINAL coordinates — the
                    // filter below keeps 「源网格不整理」 true on the binding face
                    // too, and the moved item's own binding stays vanilla-dangling).
                    var mapping = new Dictionary<ItemJar, NewPosition>();
                    object hotkeyBundle = null;
                    if (owner != null)
                        hotkeyBundle = FastTransferRecoverEngine.CaptureLocalHotkeys(owner);

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
                        LitRuntime.LogError("[快速转移恢复] 提交流程异常（未信任其写面）: " + error.Message);
                        outcome = new TidyOperationOutcome { Result = TidyCommitResult.Rejected };
                    }

                    if (outcome != null && outcome.Result == TidyCommitResult.Committed)
                    {
                        if (owner != null)
                            FastTransferRecoverEngine.AfterCommitted(hotkeyBundle, targetPage, mapping);
                        var touchedPage = sourceIsPlayerPage ? sourcePage : targetPage;
                        attempt.Recovered = true;
                        attempt.Refusal = null;
                        attempt.Reason = LitContainerTidyReason.None;
                        attempt.Commit = TidyCommitResult.Committed;
                        attempt.TargetPage = targetPage;
                        TidyDiagnosticLog.Info("fast-transfer-recover-committed",
                            "[快速转移恢复] 接收侧经统一排版连同该件提交成功（source=" + sourcePage +
                            ", target=" + targetPage + ", strategy=" + module.Strategy.StrategyId +
                            "）；未发布整理完成。");
                        if (owner != null)
                            FastTransferRecoverEngine.ReconcileListenHost(touchedPage);
                        return attempt;
                    }

                    var result = outcome == null ? TidyCommitResult.Rejected : outcome.Result;
                    if (owner != null)
                        FastTransferRecoverEngine.AfterFailed(hotkeyBundle, owner, outcome);
                    if (result == TidyCommitResult.CriticalFailure || result == TidyCommitResult.ConcurrentMutationAfterCommit)
                    {
                        module.FaultGate.Open("fast-transfer " + result + ": " +
                            (outcome != null && outcome.FailureReason != null ? outcome.FailureReason : "no detail"),
                            restoreVerified: outcome != null && outcome.RollbackVerified);
                        return Refuse(attempt, RefusalCommitCritical, LitContainerTidyReason.InternalFailure, sourcePage);
                    }
                    // Plain Rejected = the page changed between plan and commit
                    // (journal 拒绝零副作用) — honestly 内容已变, zero mutation.
                    return Refuse(attempt, RefusalCommitRejected, LitContainerTidyReason.ContentChanged, sourcePage);
                }
                finally
                {
                    recovering = false;
                }
            }
            catch (Exception error)
            {
                // The adapter never throws back into its caller: an unexpected
                // fault answers vanilla failure, zero mutation.
                LitRuntime.LogError("[快速转移恢复] 恢复流程崩溃（保持原版失败语义）: " + error.Message);
                return Refuse(attempt, FastTransferRecoverPlanner.RefusalCannotFit, LitContainerTidyReason.InternalFailure, sourcePage);
            }
        }

        private static Attempt Refuse(Attempt attempt, string refusal, LitContainerTidyReason reason, byte sourcePage)
        {
            attempt.Refusal = refusal;
            attempt.Reason = reason;
            LitRuntime.LogWarning("[快速转移恢复] 拒绝（" + refusal + ", page=" + sourcePage +
                "）：保持原版失败，两侧未动。");
            return attempt;
        }
    }
}
