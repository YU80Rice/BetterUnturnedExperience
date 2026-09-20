using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lir;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-22 → DEV-V6-02C: the HOST-FACE composition wrapper for the
    /// official in-place-reload (更好的换弹体验) feature. The registration BODY
    /// (definition payload, skill settings facet, module factory) lives in the
    /// Lir project and reaches the host ONLY through LirFeatureAssembly — the
    /// ONE public assembly type (V6-T2); this wrapper is the assembly-root act:
    /// it injects the host-owned composition inputs (log sinks, headless
    /// decision, Steam identity resolvers, the settings root — the T2 硬项拆法
    /// injection direction), hands the seam's registration to the host bridge,
    /// and projects the wired instance back out of the seam for the host ring
    /// (panel composition) until DEV-V6-02E reshapes that ring. Lir has no host
    /// tick pump (its frame work rides the frozen HostTick event seam) and no
    /// UI relay — unlike the tidy wrapper, no pump/relay seams exist here.
    /// </summary>
    internal static class InPlaceReloadFeatureRegistration
    {
        internal static FeatureRegistrationResult Register()
        {
            // DEV-V6-02C (V6-T2 硬项拆法): inject the host-owned composition
            // inputs BEFORE the seam hands out the registration, so every
            // factory-armed generation binds the real sinks/facts. The headless
            // decision, Steam identity and settings root are read lazily at
            // their consumption points (lambda/method-group delegates).
            LirFeatureAssembly.BindHostComposition(
                BueRuntimeLog.Runtime,
                BueRuntimeLog.Error,
                () => BueRuntimeCompletionChain.HeadlessDecision,
                BueEngineNet.LocalSteamId,
                BueEngineNet.FindSteamPlayer,
                () => BueSettingsRuntime.ProductionSettingsRoot);
            var result = BueRuntimeHost.Register(LirFeatureAssembly.CreateRegistration());
            if (!result.Accepted)
            {
                BueRuntimeLog.Runtime("[BUE-V2LIR] event=lir-registration result=rejected feature=" + LirFeatureAssembly.FeatureId
                    + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
            }
            return result;
        }

    }
}
