using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Registration
{
    /// <summary>
    /// DEV-V6-05 (V6-T5 Q2 + 2026-09-18 追加裁决): the host-owned composition
    /// behind the IFeaturePatching contract seam (public like its siblings
    /// FeatureEventBus / FeatureLifecycleRuntime / MainThreadDispatcherRuntime —
    /// public is not SDK contract, V3-T1; the surface modules see is
    /// IFeaturePatching). The host start path composes ONE view per
    /// (feature, lifecycle generation) and hands it to the module's bootstrap
    /// pocket; the module side has no other route to the account.
    ///
    /// Frozen semantics this class implements:
    ///   - a registered handle is a PLAIN IDisposable (不新增契约句柄类型: the
    ///     same shape every tracked resource already has). Disposing it is the
    ///     patch teardown (the module's own UnpatchSelf);
    ///   - accepted handles land in the EXISTING resource account
    ///     (IFeatureLifetime.TryTrack / FeatureLifecycleRuntime) — no second
    ///     ledger: same (FeatureId, generation) binding, same reverse-order
    ///     release at the stop/isolation boundary, same per-generation capacity;
    ///   - the view answers for its own binding: once the generation dies the
    ///     registration is an explicit rejection with a diagnostic, never a
    ///     silent accept and never an exception across the module boundary;
    ///   - rejection reasons mirror the account's own pipeline (null handle,
    ///     feature not running, stale generation, duplicate instance, capacity)
    ///     so a module can tell WHY it must keep managing its own patches —
    ///     patches hung outside this pocket are honestly NOT guaranteed to be
    ///     torn down by the platform.
    /// </summary>
    public sealed class FeaturePatchingRuntime
    {
        private readonly FeatureLifecycleRuntime lifetime;
        private readonly Action<string> diagnosticSink;

        public FeaturePatchingRuntime(FeatureLifecycleRuntime lifetime, Action<string> diagnosticSink = null)
        {
            this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            this.diagnosticSink = diagnosticSink;
        }

        /// <summary>
        /// The contract view handed to one module for one lifecycle generation
        /// (bootstrap.Patching). The view answers for its own (owner,
        /// generation) binding; after that generation dies it keeps answering —
        /// explicitly, with FeatureNotRunning / GenerationInvalid.
        /// </summary>
        public IFeaturePatching CreateView(FeatureId owner, ulong generation)
        {
            return new FeaturePatchingView(this, owner, generation);
        }

        /// <summary>The registration pipeline: null gate → the account's own
        /// track pipeline (one account, one rule set) → structured contract
        /// result. The account emits its own BUE-LIFE-* lines; this seam adds
        /// the BUE-PATCH-* decision line so the pocket's verdict is locatable
        /// in the same sink.</summary>
        internal FeaturePatchRegistrationResult Register(FeatureId owner, ulong generation, IDisposable patchTeardown)
        {
            if (patchTeardown == null)
            {
                Emit("event=feature-patch result=register-rejected feature=" + owner.Value
                    + " generation=" + generation + " reason=null-handle diagnosticId=BUE-PATCH-001");
                return new FeaturePatchRegistrationResult(false, FeaturePatchRegistrationReason.InvalidPatch, "BUE-PATCH-001", generation);
            }
            var rejection = lifetime.Track(owner, generation, patchTeardown);
            if (rejection != FeatureLifecycleRuntime.TrackRejection.None)
            {
                var reason = rejection == FeatureLifecycleRuntime.TrackRejection.StaleGeneration
                    ? FeaturePatchRegistrationReason.GenerationInvalid
                    : rejection == FeatureLifecycleRuntime.TrackRejection.DuplicateRegistration
                        ? FeaturePatchRegistrationReason.DuplicatePatch
                        : rejection == FeatureLifecycleRuntime.TrackRejection.CapacityExceeded
                            ? FeaturePatchRegistrationReason.CapacityExceeded
                            : FeaturePatchRegistrationReason.FeatureNotRunning;
                var diagnosticId = reason == FeaturePatchRegistrationReason.GenerationInvalid ? "BUE-PATCH-003"
                    : reason == FeaturePatchRegistrationReason.DuplicatePatch ? "BUE-PATCH-004"
                    : reason == FeaturePatchRegistrationReason.CapacityExceeded ? "BUE-PATCH-005"
                    : "BUE-PATCH-002";
                Emit("event=feature-patch result=register-rejected feature=" + owner.Value
                    + " generation=" + generation + " reason=" + reason + " diagnosticId=" + diagnosticId);
                return new FeaturePatchRegistrationResult(false, reason, diagnosticId, generation);
            }
            Emit("event=feature-patch result=registered feature=" + owner.Value
                + " generation=" + generation + " diagnosticId=BUE-PATCH-ACCEPT");
            return new FeaturePatchRegistrationResult(true, FeaturePatchRegistrationReason.None, "BUE-PATCH-ACCEPT", generation);
        }

        private void Emit(string line)
        {
            var sink = diagnosticSink;
            if (sink == null) return;
            try { sink(line); }
            catch (Exception) { }
        }

        private sealed class FeaturePatchingView : IFeaturePatching
        {
            private readonly FeaturePatchingRuntime owner;
            private readonly FeatureId feature;
            private readonly ulong generation;

            internal FeaturePatchingView(FeaturePatchingRuntime owner, FeatureId feature, ulong generation)
            {
                this.owner = owner;
                this.feature = feature;
                this.generation = generation;
            }

            public FeaturePatchRegistrationResult Register(IDisposable patchTeardown)
            {
                return owner.Register(feature, generation, patchTeardown);
            }
        }
    }
}
