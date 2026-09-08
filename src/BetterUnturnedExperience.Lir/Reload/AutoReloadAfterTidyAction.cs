using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: this phase's IReloadAction adapter — 整理后自动压弹. It
    /// reproduces the old TidyServicePostfixPatch behavior through the
    /// feature event instead of a cross-plugin Harmony postfix (and without
    /// the reflection-by-type-name): acquire the request gate for the target
    /// and run the transactional same-id-magazine merge through the
    /// authority. Runs synchronously on the thread that received the event —
    /// the tidy transaction completed on this very thread moments ago (the
    /// old postfix nesting shape). No new switch: the module's enabled total
    /// switch is the only gate above this action (spec T5 决策 3).
    /// </summary>
    internal sealed class AutoReloadAfterTidyAction : IReloadAction
    {
        private readonly InPlaceReloadModule module;

        internal AutoReloadAfterTidyAction(InPlaceReloadModule module)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
        }

        public void Execute(LirReloadActionContext context)
        {
            // The enabled total switch gates EVERY reload behavior (off =
            // native fallback — no patches, no double-tap repack, no merge).
            if (!module.Enabled || module.ShuttingDown || !module.Started) return;
            module.ExecuteTidyMerge(context.TargetSteamId, context.ConnectionGeneration, context.TransactionId);
        }
    }
}
