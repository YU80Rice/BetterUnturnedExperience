using System;
using System.Runtime.CompilerServices;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-03: the container-tidy authority execution core — verify first
    /// (never touching the grid on any failure), then the SAME transactional
    /// single-page service the player pages use, planned by the ONE injected
    /// strategy (DEV-V5-02 tagged-row-band-v1). Entries: the local (server
    /// role) path and the multiplayer authority path both run through here,
    /// so no entry point copies the algorithm or the verification order.
    ///
    /// The grid is mutated ONLY through the transaction's removeItem/addItem
    /// pair — which keeps the vanilla sync intact: for a world crate the
    /// mounted Items is the crate's own instance, so its onStateUpdated
    /// re-persists via BarricadeManager.updateState and the opener's client
    /// receives the item add/remove fan-out (updateItems subscriptions), and
    /// for a trunk the same applies to the vehicle's trunkItems. No parallel
    /// BUE lock, no hand-rolled sync (V5-T4 Q3: 原版访问控制保持).
    /// </summary>
    internal static class LitContainerTidyExecution
    {
        /// <summary>The page number the container mounts on in vanilla
        /// (PlayerInventory.STORAGE). It is a MOUNT POSITION and only ever
        /// reaches the transaction's diagnostics/mapping here — identity is
        /// the session facts + claim, never this byte (V5-T4: 第 7 页只是原版
        /// 挂载位置，不是身份). It is a literal rather than the vanilla field
        /// on purpose: reading PlayerInventory.STORAGE would force its static
        /// initializer (net-reflection wiring) into every JIT of this core,
        /// and the host test path must never touch it.</summary>
        internal const byte MOUNT_PAGE = 7;

        internal sealed class Result
        {
            public LitContainerTidyReason Reason;
            public TidyCommitResult Commit;

            internal static Result Refused(LitContainerTidyReason reason)
            {
                return new Result { Reason = reason, Commit = TidyCommitResult.Rejected };
            }
        }

        /// <summary>
        /// DEV-V5-03 host-test seam (the NetServiceFactoryForTests /
        /// ServerRoleProbeForTests family): replaces the single-page
        /// transaction call. Null = the production path (ManualTidyService.
        /// TidyPage, which commits via the vanilla removeItem/addItem — those
        /// resolve item assets through SDG.Unturned.Assets, which fails
        /// outside the game process, so the host injects a same-semantics
        /// fake that applies the plan straight to the jar coordinates; the
        /// real commit chain rides DEV-V5-08 machine acceptance — the named
        /// seam gap this ticket records).
        /// </summary>
        internal static Func<Items, byte, bool, ITidyStrategy, TidyOperationOutcome> PageTransactionForTests;

        /// <summary>Re-reads the fingerprint FROM THE REAL JARS (the Round-9
        /// rule the player pages already follow: trust only what the server's
        /// own read proves), runs the ordered verifier, and only then the
        /// transaction. Mapping out of the caller is intentionally absent:
        /// container pages carry no hotkeys (the hotkey gate already refuses
        /// any page outside 2..6, so an all-container transaction never
        /// touches them).</summary>
        internal static Result Commit(
            LitContainerLiveFacts live,
            LitContainerTidyClaim claim,
            Items containerItems,
            bool sortDescending,
            ITidyStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            if (containerItems == null)
            {
                // The mount the fact table said is open is gone between the
                // read and here — that is a closed/changed container, zero
                // mutation, never a half transaction.
                return Result.Refused(LitContainerTidyReason.ContainerClosed);
            }
            var checkedLive = live;
            checkedLive.LiveFingerprint = LitContainerContentFingerprint.FromItems(containerItems);
            var reason = LitContainerTidyVerifier.Verify(checkedLive, claim);
            if (reason != LitContainerTidyReason.None) return Result.Refused(reason);

            TidyOperationOutcome outcome;
            try
            {
                var transaction = PageTransactionForTests;
                outcome = transaction != null
                    ? transaction(containerItems, MOUNT_PAGE, sortDescending, strategy)
                    : ManualTidyService.TidyPage(
                        containerItems, MOUNT_PAGE, sortDescending, TidyMode.SameType, null, strategy);
            }
            catch (Exception error)
            {
                LitRuntime.LogError("[Tidy容器] 整理事务异常（未改动物品）: " + error.Message);
                return new Result { Reason = LitContainerTidyReason.InternalFailure, Commit = TidyCommitResult.Rejected };
            }

            switch (outcome.Result)
            {
                case TidyCommitResult.Committed:
                    TidyDiagnosticLog.Info("container-tidy-committed",
                        "[Tidy容器] 容器整理已提交（kind=" + claim.Kind + ", strategy=" + strategy.StrategyId + "）。");
                    return new Result { Reason = LitContainerTidyReason.None, Commit = TidyCommitResult.Committed };
                case TidyCommitResult.ConcurrentMutationAfterCommit:
                    LitRuntime.LogWarning("[Tidy容器] 提交后检测到并发修改，已安全隔离（kind=" + claim.Kind + "）");
                    return new Result { Reason = LitContainerTidyReason.ContentChanged, Commit = outcome.Result };
                case TidyCommitResult.CriticalFailure:
                    LitRuntime.LogError("[Tidy容器] 整理事务关键失败（回滚状态=" + (outcome.RollbackVerified ? "已验证" : "未验证") + "）");
                    return new Result { Reason = LitContainerTidyReason.InternalFailure, Commit = outcome.Result };
                default:
                    // The version was just re-verified against the real jars,
                    // so a plain Rejected here is the planner refusing the
                    // grid (cannot-fit / validation) — the honest cause is
                    // 排版失败, zero mutation either way.
                    return new Result { Reason = LitContainerTidyReason.LayoutFailed, Commit = TidyCommitResult.Rejected };
            }
        }
    }

    /// <summary>The player-facing feedback channel for container tidy
    /// (T4 Q4: 禁止可点但静默没动静). Every refused click answers with the
    /// Chinese text of its structured reason — through the injectable sink
    /// (production: the on-screen toast bound with the UI patch; host tests:
    /// a recorder; U3DS: unbound, so the authoritative execution stays and
    /// only the log line lands). The log line ALWAYS writes.</summary>
    internal static class LitContainerFeedback
    {
        internal static Action<string> ToastSink;

        /// <summary>The production sink identity (module Start binds it when
        /// the client UI is armed; Stop unbinds only this one, never a test
        /// recorder left by a host fixture).</summary>
        internal static readonly Action<string> ProductionSink = LitContainerToast.Show;

        internal static void ShowReason(string context, LitContainerTidyReason reason)
        {
            var text = LitContainerTidyReasons.ChineseText(reason);
            if (string.IsNullOrEmpty(text)) return;
            LitRuntime.LogWarning("[Tidy容器] " + context + "：" + text);
            var sink = ToastSink;
            if (sink != null)
            {
                try { sink(text); }
                catch (Exception error) { LitRuntime.LogWarning("[Tidy容器] 提示渲染失败（原因仍见日志）: " + error.Message); }
            }
        }

        internal static void ShowNote(string context, string message)
        {
            TidyDiagnosticLog.Info("container-tidy", "[Tidy容器] " + context + "：" + message);
        }
    }

    /// <summary>DEV-V5-03: the on-screen answer for container tidy refusals —
    /// the native NPC_CUSTOM message lane (the LirToast precedent), bottom-
    /// anchored, never intercepting the vanilla message area. Engine-only:
    /// NoInlining + guarded so the host test path never JITs it.</summary>
    internal static class LitContainerToast
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Show(string message)
        {
            try { SDG.Unturned.PlayerUI.message(SDG.Unturned.EPlayerMessage.NPC_CUSTOM, message, 4f); }
            catch (Exception) { /* no client surface — the log line already carries the reason */ }
        }
    }
}
