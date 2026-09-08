using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22 (spec「LIT ↔ LIR：TidyCompleted 功能事件」T5 决策 3): the ONE
    /// consumer of the TidyCompleted feature event. The frozen validation —
    /// an event is acted on only when the completion result is Succeeded AND
    /// the page scope fits the reload domain (2..6); a blind execution is
    /// forbidden. Idempotency rides the publisher's monotonic transaction id
    /// (per-publisher high-water mark: duplicates and regressions are
    /// dropped). The target player is resolved from the connection
    /// generation — 0 = the local player (single player / listen host tidy),
    /// a session generation = that session's peer (the server executed the
    /// client's tidy; the old postfix merged the tidied inventory — parity
    /// through the frozen payload, no new wire field). Action exceptions are
    /// isolated: a bad adapter never propagates into the host event bus.
    /// </summary>
    internal sealed class TidyCompletedConsumer
    {
        private readonly IReloadAction action;
        private readonly Func<ulong, ulong> resolveTarget;
        private readonly Dictionary<string, ulong> lastProcessedTransactionId = new Dictionary<string, ulong>(StringComparer.Ordinal);

        /// <summary>
        /// resolveTarget: connection generation → target steam id (0 = the
        /// generation cannot be resolved — the event is dropped). Injected so
        /// the consumer stays engine-free.
        /// </summary>
        internal TidyCompletedConsumer(IReloadAction action, Func<ulong, ulong> resolveTarget)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.resolveTarget = resolveTarget ?? throw new ArgumentNullException(nameof(resolveTarget));
        }

        internal void Handle(TidyCompleted evt)
        {
            try
            {
                HandleInner(evt);
            }
            catch (Exception error)
            {
                // Exception isolation: a faulting action never breaks the bus
                // dispatch chain (the host clock / other features ride it).
                LirRuntime.LogError("[LirConsumer] TidyCompleted 消费处理异常（已隔离）: " + error.Message);
            }
        }

        private void HandleInner(TidyCompleted evt)
        {
            // Result gate: only a SUCCEEDED tidy triggers the follow-up reload.
            if (evt.Result != TidyCompletionResult.Succeeded) return;

            // Scope gate: the reload domain is the player-inventory page
            // range 2..6 (mirrors PlayerInventory.SLOTS..PANTS); anything
            // outside — including a malformed inverted range — never reaches
            // an action (R1-Spec DEVIATION-1 fix).
            if (evt.FirstPage > evt.LastPage) return;
            if (evt.FirstPage < ReloadRuntimePolicy.MinRepackPage || evt.LastPage > ReloadRuntimePolicy.MaxRepackPage) return;

            // Identity gate: the transaction id is the dedup key; zero is not
            // a valid transaction identity (the publisher contract forbids it).
            if (evt.TransactionId == 0UL) return;
            var publisherKey = evt.Publisher.Value ?? string.Empty;
            if (lastProcessedTransactionId.TryGetValue(publisherKey, out var seen) && evt.TransactionId <= seen) return;

            // Target resolution: generation 0 = the local player; a session
            // generation = the peer whose tidy the server just executed. An
            // unresolvable target drops the event (never a blind execution).
            var target = resolveTarget(evt.ConnectionGeneration);
            if (target == 0UL)
            {
                LirRuntime.LogDiagnostic("[LirConsumer] 整理完成事件的目标玩家不可解析（generation=" + evt.ConnectionGeneration + "），跳过自动压弹");
                return;
            }

            lastProcessedTransactionId[publisherKey] = evt.TransactionId;
            action.Execute(new LirReloadActionContext(target, evt.ConnectionGeneration, evt.Publisher, evt.TransactionId));
        }
    }
}
