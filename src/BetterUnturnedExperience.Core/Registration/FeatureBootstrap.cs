using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;

namespace BetterUnturnedExperience.Core.Registration
{
    /// <summary>
    /// DEV-V2-14: host-side composition of IFeatureBootstrap. A pure
    /// composition record — it invents no surface of its own; every member is
    /// exactly the dependency the host wiring provides. The one frozen
    /// invariant this ticket owns: Network fail-fasts on null and hands the
    /// feature the same IBueNetworkApi instance for the module's whole
    /// lifetime (network module disable/enable cycles never substitute it).
    /// Members the host runtime does not compose yet (events, logger,
    /// dependencies, lifetime) pass through as provided; the host start path
    /// that constructs this type for real module starts lands with the
    /// tickets that start modules (DEV-V2-21/22).
    /// </summary>
    public sealed class FeatureBootstrap : IFeatureBootstrap
    {
        // DEV-V3-04: the optional trailing mainThread parameter is the
        // additive Minor 2.1 extension (the DEV-V2-21 additive-surface
        // precedent — existing call sites keep compiling and observe the
        // stage-baseline null; the host start path passes the real view).
        public FeatureBootstrap(FeatureScopeIdentity identity, ulong lifecycleGeneration, IScopedFeatureSettings settings, IFeatureEventSubscriber events, IOwnedFeatureEventPublisher ownedEvents, IFeatureEventRegistry eventRegistry, IFeatureLogger logger, IDependencyCapabilityView dependencies, IFeatureLifetime lifetime, IBueNetworkApi network, IFeatureMainThread mainThread = null)
        {
            Identity = identity;
            LifecycleGeneration = lifecycleGeneration;
            Settings = settings;
            Events = events;
            OwnedEvents = ownedEvents;
            EventRegistry = eventRegistry;
            Logger = logger;
            Dependencies = dependencies;
            Lifetime = lifetime;
            Network = network ?? throw new ArgumentNullException(nameof(network));
            MainThread = mainThread;
        }

        public FeatureScopeIdentity Identity { get; }
        public ulong LifecycleGeneration { get; }
        public IScopedFeatureSettings Settings { get; }
        public IFeatureEventSubscriber Events { get; }
        public IOwnedFeatureEventPublisher OwnedEvents { get; }
        // DEV-V3-02: the event-type ownership registration seam — composed by
        // the host start path from DEV-V3-02 on (availability matrix row:
        // EventRegistry available from DEV-V3-02).
        public IFeatureEventRegistry EventRegistry { get; }
        public IFeatureLogger Logger { get; }
        public IDependencyCapabilityView Dependencies { get; }
        public IFeatureLifetime Lifetime { get; }
        public IBueNetworkApi Network { get; }
        // DEV-V3-04: the platform main-thread dispatcher view — composed by
        // the host start path from DEV-V3-04 on (availability matrix row;
        // null until wired, the stage-baseline rule).
        public IFeatureMainThread MainThread { get; }
    }
}
