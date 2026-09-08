namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the tracking policy seam (spec「扩展三分法」落点之一). The
    /// DEFAULT implementation carries the current behavior (the 08 baseline)
    /// unchanged; a future variant registers as a new implementation of this
    /// seam inside the same FeatureId — a genuinely independent capability is
    /// a NEW feature module with its own FeatureId.
    /// </summary>
    internal interface IHordeTrackingPolicy
    {
        /// <summary>The policy identity (the ITidyStrategy.StrategyId convention).</summary>
        string PolicyId { get; }

        /// <summary>Whether a beacon activation is accepted for tracking: the asset
        /// must have injected its wave total (total == 0 → wait for the next
        /// beacon update instead of tracking an empty horde).</summary>
        bool ShouldAcceptActivation(ushort totalZombies);

        /// <summary>The broadcast "remaining" clamp: outstanding = to_spawn + alive,
        /// saturated to the wire's ushort range, negatives floored at 0.</summary>
        int ClampOutstanding(int outstanding);

        /// <summary>The no-event fallback broadcast cadence (event-loss cover + new-client sync).</summary>
        float FallbackBroadcastIntervalSeconds { get; }

        /// <summary>The long-term diagnostic heartbeat cadence.</summary>
        float HeartbeatIntervalSeconds { get; }
    }
}
