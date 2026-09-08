using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22 (spec T5 决策 2/3): the reload-action adapter seam — the ONE
    /// extension point for reload strategies inside LIR. Today's only adapter
    /// is <see cref="AutoReloadAfterTidyAction"/> (整理后自动压弹); future
    /// variants (补同 ID 弹匣 / 按武器类型处理 / 手动快速压弹) plug in here
    /// without touching the lifecycle, the patches or the network wiring.
    /// </summary>
    internal interface IReloadAction
    {
        /// <summary>Executes the reload action for a fully resolved target. Implementations must be idempotent-safe and engine-fault-isolated by their caller (the consumer).</summary>
        void Execute(LirReloadActionContext context);
    }

    /// <summary>The immutable action request: who to reload, traced back to the triggering event.</summary>
    internal readonly struct LirReloadActionContext
    {
        /// <summary>The resolved target player's steam id (never 0 — unresolved targets never reach an action).</summary>
        public ulong TargetSteamId { get; }

        /// <summary>The triggering event's connection generation (0 = the local/single-player path).</summary>
        public ulong ConnectionGeneration { get; }

        /// <summary>The event publisher (the tidy feature).</summary>
        public FeatureId Publisher { get; }

        /// <summary>The triggering event's transaction identity.</summary>
        public ulong TransactionId { get; }

        public LirReloadActionContext(ulong targetSteamId, ulong connectionGeneration, FeatureId publisher, ulong transactionId)
        {
            TargetSteamId = targetSteamId;
            ConnectionGeneration = connectionGeneration;
            Publisher = publisher;
            TransactionId = transactionId;
        }
    }
}
