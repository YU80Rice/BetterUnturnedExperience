using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lit;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-15 → DEV-V6-02B: the HOST-FACE composition wrapper for the
    /// official inventory-tidy (背包整理) feature. The registration BODY
    /// (definition payload, settings facet, module factory) lives in the Lit
    /// project and reaches the host ONLY through LitFeatureAssembly — the ONE
    /// public assembly type (V6-T2); this wrapper is the assembly-root act:
    /// it injects the host-owned composition inputs (log sinks, headless
    /// decision, Steam identity resolvers — the T2 硬项拆法 injection
    /// direction), hands the seam's registration to the host bridge, and
    /// projects the wired instance / tick pump back out of the seam for the
    /// host ring (panel composition + completion chain). DEV-V6-02E 收口: that ring is
    /// reshaped — the composition no longer holds module instances, and this
    /// wrapper keeps ONLY the assembly-root registration act (inject + bridge).
    /// </summary>
    internal static class InventoryTidyFeatureRegistration
    {
        internal static FeatureRegistrationResult Register()
        {
            // DEV-V6-02B (V6-T2 硬项拆法): inject the host-owned composition
            // inputs BEFORE the seam hands out the registration, so every
            // factory-armed generation binds the real sinks/facts. The
            // headless decision and Steam identity are read lazily at their
            // consumption points (lambda/method-group delegates).
            LitFeatureAssembly.BindHostComposition(
                BueRuntimeLog.Runtime,
                BueRuntimeLog.Error,
                () => BueRuntimeCompletionChain.HeadlessDecision,
                BueEngineNet.LocalSteamId,
                BueEngineNet.FindSteamPlayer);
            var result = BueRuntimeHost.Register(LitFeatureAssembly.CreateRegistration());
            if (!result.Accepted)
            {
                BueRuntimeLog.Runtime("[BUE-V2LIT] event=lit-registration result=rejected feature=" + LitFeatureAssembly.FeatureId
                    + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
            }
            return result;
        }

    }
}
