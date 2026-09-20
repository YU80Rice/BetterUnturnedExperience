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

        /// <summary>
        /// DEV-V6-03: the host-owned registration-runtime factory — the ONE
        /// production construction site. It binds the machine's phase
        /// observation mouth to the runtime trace, so the boot sequence's
        /// registration phases are log-visible: the production ready entry
        /// performs the catalog freeze internally and its barrier stays atomic
        /// to callers, which makes this trace the only place the freeze step
        /// shows up. Debug (runtime) verbosity — silent in normal play, and
        /// host tests read it through the log recorder.
        /// </summary>
        internal static FeatureRegistrationRuntime CreateRuntime()
        {
            return new FeatureRegistrationRuntime(phase =>
                BueRuntimeLog.Runtime("[BUE-BOOTSTRAP] event=registration-phase phase=" + phase));
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
