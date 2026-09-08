namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the default tracking policy — carries the current behavior
    /// (the 08 baseline) with the constants lifted out of the old plugin's
    /// scattered fields into the policy object (the ReloadRuntimePolicy
    /// convention). Not a public setting this ticket.
    /// </summary>
    internal sealed class DefaultHordeTrackingPolicy : IHordeTrackingPolicy
    {
        internal const float DefaultFallbackBroadcastIntervalSeconds = 2.0f;
        internal const float DefaultHeartbeatIntervalSeconds = 60.0f;

        public string PolicyId
        {
            get { return "default-horde-v1"; }
        }

        public bool ShouldAcceptActivation(ushort totalZombies)
        {
            return totalZombies > 0;
        }

        public int ClampOutstanding(int outstanding)
        {
            if (outstanding < 0) return 0;
            if (outstanding > ushort.MaxValue) return ushort.MaxValue;
            return outstanding;
        }

        public float FallbackBroadcastIntervalSeconds
        {
            get { return DefaultFallbackBroadcastIntervalSeconds; }
        }

        public float HeartbeatIntervalSeconds
        {
            get { return DefaultHeartbeatIntervalSeconds; }
        }
    }
}
