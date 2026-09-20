using System;
using BetterUnturnedExperience.Bii;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2 → DEV-V6-04: the HOST-FACE composition wrapper for the official
    /// Better Item Interaction (更好的物品交互) feature. The registration BODY
    /// (definition payload, satellite registration, the module that starts and
    /// stops the interaction rig) lives in the Bii project and reaches the host
    /// ONLY through BiiFeatureAssembly — the ONE public assembly type (V6-T2;
    /// 02B/02C/02D wrapper precedent); this wrapper is the assembly-root act:
    /// it injects the host-owned composition inputs (log mouths, headless
    /// decision, the placement evaluator factory) BEFORE the seam hands out
    /// the registration, and hands the seam's registration to the host bridge.
    /// The pre-move private registration body with the EMPTY module is gone by
    /// construction (T3 Q2: 空壳不得保留).
    /// </summary>
    internal static class BetterItemInteractionFeatureRegistration
    {
        internal static FeatureRegistrationResult Register()
        {
            // DEV-V6-04 (T3 Q2/Q5): inject the host-owned composition inputs
            // before the seam hands out the registration. The headless decision
            // reads lazily at the consumption point (the completion chain's
            // frozen decision); the evaluator's concrete implementation stays
            // host-side (Core) and crosses as its contract interface.
            BiiFeatureAssembly.BindHostComposition(
                logRuntime: BueRuntimeLog.Runtime,
                logWarn: BueRuntimeLog.Warn,
                logError: BueRuntimeLog.Error,
                logErrorFriendly: BueRuntimeLog.ErrorFriendly,
                isCriticalNotReadyReason: BueRuntimeLog.IsCriticalNotReadyReason,
                headlessDecision: () => BueRuntimeCompletionChain.HeadlessDecision,
                placementEvaluatorFactory: () => new BetterUnturnedExperience.Core.Placement.PlacementCandidateEvaluator());
            return BueRuntimeHost.Register(BiiFeatureAssembly.CreateRegistration());
        }
    }
}
