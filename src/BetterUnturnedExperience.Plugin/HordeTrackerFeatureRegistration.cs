using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lht;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-20 → DEV-V6-02D: the HOST-FACE composition wrapper for the official
    /// horde-tracker (更好的尸潮播报) feature. The registration BODY (definition payload,
    /// module factory) lives in the Lht project and reaches the host ONLY through
    /// LhtFeatureAssembly — the ONE public assembly type (V6-T2); this wrapper is the
    /// assembly-root act: it injects the host-owned composition input (the log sinks —
    /// the T2 硬项拆法 injection direction; this ticket's ONLY forbidden direction), hands
    /// the seam's registration to the host bridge, and projects the wired instance and
    /// the panel presentation fact back out of the seam for the host ring (panel
    /// composition). DEV-V6-02E 收口:
    /// that ring is reshaped (the composition no longer holds module instances); only
    /// the registration act remains. Lht has no host tick pump (its
    /// frame work rides the frozen HostTick event seam), no UI relay and no settings
    /// root — unlike the tidy/reload wrappers, only the log injection exists here.
    /// </summary>
    internal static class HordeTrackerFeatureRegistration
    {
        internal static FeatureRegistrationResult Register()
        {
            // DEV-V6-02D (V6-T2 硬项拆法): inject the host-owned composition input BEFORE
            // the seam hands out the registration, so every factory-armed generation binds
            // the real sinks (the method-group delegates read lazily at each rebind).
            LhtFeatureAssembly.BindHostComposition(
                BueRuntimeLog.Runtime,
                BueRuntimeLog.Error);
            var result = BueRuntimeHost.Register(LhtFeatureAssembly.CreateRegistration());
            if (!result.Accepted)
            {
                BueRuntimeLog.Runtime("[BUE-V2LHT] event=lht-registration result=rejected feature=" + LhtFeatureAssembly.FeatureId
                    + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
            }
            return result;
        }

    }
}
