using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>Public, narrow registration bridge owned by the BUE Host plugin.</summary>
    public static class BueRuntimeHost
    {
        private static IBueFeatureRegistrationHost current;

        public static FeatureRegistrationPhase Phase
        {
            get
            {
                var host = current;
                return host == null ? FeatureRegistrationPhase.HostStarting : host.Phase;
            }
        }

        public static FeatureRegistrationResult Register(IFeatureRegistration registration)
        {
            var host = current;
            return host == null
                ? new FeatureRegistrationResult(false, new FeatureId(string.Empty), FeatureRegistrationReason.HostUnavailable, "BUE-HOST-001")
                : host.Register(registration);
        }

        internal static FeatureRegistrationRuntime CurrentRuntime
        {
            get { return current as FeatureRegistrationRuntime; }
        }

        internal static void Bind(IBueFeatureRegistrationHost host)
        {
            current = host;
        }

        internal static void Clear()
        {
            current = null;
        }
    }
}
