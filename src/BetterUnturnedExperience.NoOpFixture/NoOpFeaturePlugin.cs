using System;
using BepInEx;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Plugin;

namespace BetterUnturnedExperience.NoOpFixture
{
    [BepInPlugin("io.github.yu80rice.bue.noop", "BUE No-op Feature Fixture", "0.0.0")]
    [BepInDependency("io.github.yu80rice.betterunturnedexperience", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class NoOpFeaturePlugin : BaseUnityPlugin
    {
        public static FeatureRegistrationResult RegisterWithBue() { return NoOpFeatureRegistration.Register(); }

        private void Awake()
        {
            var result = new NoOpFeatureBootstrap().Awake();
            Logger.LogInfo("BUE no-op fixture featureId=" + (result.Feature.Value ?? string.Empty) + " accepted=" + result.Accepted + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
        }
    }

    public sealed class NoOpFeatureBootstrap
    {
        public FeatureRegistrationResult Awake() { return NoOpFeatureRegistration.Register(); }
    }

    public static class NoOpFeatureRegistration
    {
        /// <summary>
        /// DEV-V3-03: the ecosystem contract probe's observability seam — the
        /// state of the last probe module the fixture factory created (host
        /// tests read it to verify the ECOSYSTEM-side lifecycle consumption;
        /// the official side is anchored by the real LIT module). Sample
        /// fixture surface, not SDK contract.
        /// </summary>
        public sealed class ProbeState
        {
            public bool Started;
            public bool Tracked;
            public bool ResourceDisposed;
            public FeatureState QueriedStateAtStart;
            // DEV-V3-04 ecosystem-side contrast: the probe observes the
            // wired MainThread dispatcher seam and gets an explicit accepted
            // post through it (the full post-wiring chain probe lands with
            // DEV-V3-08).
            public bool MainThreadAvailable;
            public bool MainThreadPosted;
            // DEV-V3-05 HostTick branch (V3-T6 裁决③): the probe subscribes
            // to the host clock through the Events seam and records what the
            // ticks carried — the host-test side asserts the receipt, the
            // sequence advance, the frozen phase and the stop-boundary
            // auto-unsubscription. Sample fixture surface, not SDK contract.
            public bool HostTickSubscribed;
            public int HostTicksReceived;
            public ulong FirstHostTickNumber;
            public ulong LastHostTickNumber;
            public TickPhase LastHostTickPhase;
            public float LastHostTickDeltaSeconds;
        }

        public static ProbeState LastProbe { get; private set; }

        // The fixture's own registration (whitelisted official sample
        // identity) for host tests that drive the probe through a fresh
        // registration runtime.
        public static IFeatureRegistration ProbeRegistration { get; } = new NoOpRegistration();

        public static FeatureRegistrationResult Register()
        {
            return BueRuntimeHost.Register(ProbeRegistration);
        }

        private sealed class NoOpRegistration : IFeatureRegistration
        {
            public FeatureDefinitionArtifact Definition { get; } = new FeatureDefinitionArtifact(new FeatureId("io.github.yu80rice.bue.noop"), 1, "bue-noop", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new NoOpFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class NoOpFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return new NoOpModule(); }
        }

        /// <summary>
        /// DEV-V3-03: the probe is no longer a static empty shell — Start
        /// consumes the frozen lifecycle seams exactly as an ecosystem feature
        /// should: one resource through TryTrack, one read-only status query,
        /// an honest Started=true outcome (the R1 inventory's「NoOp 的 Start
        /// 返回 Started=false 不练这些缝」gap closes here). DEV-V3-08 extends
        /// this probe to the full post-wiring chain.
        /// </summary>
        private sealed class NoOpModule : IFeatureModule
        {
            private ProbeResource resource;
            private IDisposable hostTickSubscription;

            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                var probe = new ProbeState();
                LastProbe = probe;
                resource = new ProbeResource(probe);
                probe.Tracked = bootstrap.Lifetime.TryTrack(resource);
                probe.QueriedStateAtStart = bootstrap.Lifetime.CurrentStatus.State;
                // DEV-V3-04: the dispatcher seam's ecosystem-side contrast —
                // observe the wired view and post through it (fire-and-
                // forget, an accepted explicit result; full-chain probe→08).
                if (bootstrap.MainThread != null)
                {
                    probe.MainThreadAvailable = true;
                    probe.MainThreadPosted = bootstrap.MainThread.Post(() => { }).Posted;
                }
                // DEV-V3-05 HostTick branch: subscribe the frozen clock seam
                // exactly as an ecosystem consumer should (the host drops the
                // subscription at the stop boundary — no self-teardown here);
                // record every tick's timing payload for the host-test side.
                if (bootstrap.Events != null)
                {
                    hostTickSubscription = bootstrap.Events.Subscribe<HostTick>(tick =>
                    {
                        probe.HostTicksReceived++;
                        if (probe.HostTicksReceived == 1) probe.FirstHostTickNumber = tick.TickNumber;
                        probe.LastHostTickNumber = tick.TickNumber;
                        probe.LastHostTickPhase = tick.Phase;
                        probe.LastHostTickDeltaSeconds = tick.DeltaTime;
                    });
                    probe.HostTickSubscribed = hostTickSubscription != null;
                }
                probe.Started = true;
                return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-NOOP-START");
            }

            public void Stop(FeatureStopReason reason) { }
        }

        private sealed class ProbeResource : IDisposable
        {
            private readonly ProbeState owner;

            internal ProbeResource(ProbeState owner) { this.owner = owner; }

            public void Dispose() { owner.ResourceDisposed = true; }
        }
    }
}
