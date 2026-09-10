using System;
using System.Collections.Generic;
using System.IO;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Settings;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V3-06: plugin composition root for the platform settings service
    /// (the BueMainThreadRuntime composition-root precedent — one host-owned
    /// seam created at bootstrap, never feature-specific). It owns the
    /// per-root FeatureSettingsRegistry instances: the DEFAULT registry backs
    /// the start path, the panel routing and the scoped views; adapters built
    /// with an explicit root resolve the SAME registry when the roots match
    /// (production: one root, one runtime per feature — a single source of
    /// truth, so the settings file can never face two live writers).
    /// Internal on purpose: the registry is host composition, not third-party
    /// surface — external features bind only through IFeatureBootstrap.Settings.
    /// </summary>
    internal static class BueSettingsRuntime
    {
        private static readonly object sync = new object();
        private static readonly Dictionary<string, FeatureSettingsRegistry> registries =
            new Dictionary<string, FeatureSettingsRegistry>(StringComparer.OrdinalIgnoreCase);
        private static FeatureSettingsRegistry defaultRegistry;

        /// <summary>The production persistence root (the V2 file layout is
        /// unchanged — player settings carry over seamlessly; the contract
        /// never hardcodes paths, this is the platform-side adapter choice).</summary>
        internal static string ProductionSettingsRoot
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BetterUnturnedExperience"); }
        }

        /// <summary>Host-test seam: replaces the engine authority probe.</summary>
        internal static Func<bool> AuthorityProbeForTests;

        /// <summary>One-shot composition of the default registry (first call
        /// wins; re-creation requires Clear — the group-reset seam).</summary>
        internal static bool EnsureCreated(string settingsRoot, Func<bool> authoritySide = null, Action<string> sink = null)
        {
            lock (sync)
            {
                if (defaultRegistry != null) return false;
                defaultRegistry = CreateLocked(settingsRoot, authoritySide, sink);
                return true;
            }
        }

        /// <summary>The default registry (lazily composed against the
        /// production root — the start path and the panel always agree on it).</summary>
        internal static FeatureSettingsRegistry Registry
        {
            get
            {
                lock (sync)
                {
                    if (defaultRegistry == null) defaultRegistry = CreateLocked(ProductionSettingsRoot, null, null);
                    return defaultRegistry;
                }
            }
        }

        /// <summary>Root-keyed resolution: the production adapter and the
        /// start path share ONE registry when their roots are the same path;
        /// an injected test root gets its own (deterministic, isolated).</summary>
        internal static FeatureSettingsRegistry RegistryFor(string settingsRoot)
        {
            lock (sync)
            {
                FeatureSettingsRegistry registry;
                if (registries.TryGetValue(NormalizeRoot(settingsRoot), out registry) && registry != null) return registry;
                registry = CreateLocked(settingsRoot, null, null);
                if (defaultRegistry == null) defaultRegistry = registry;
                return registry;
            }
        }

        /// <summary>
        /// The Settings matrix wiring for one (feature, generation): composes
        /// the runtime from the registration's own facet schema, opens the
        /// generation and returns the scoped view. Null = the feature
        /// declared NO settings facet (the honest null side of the
        /// availability matrix: nothing provided, nothing faked).
        /// </summary>
        internal static IScopedFeatureSettings ComposeViewForStart(FeatureId feature, IReadOnlyList<SettingDescriptor> descriptors, ulong generation)
        {
            if (descriptors == null) return null;
            var registry = Registry;
            var runtime = registry.GetOrCreateRuntime(feature, descriptors);
            if (runtime == null) return null; // the registry left its explicit line (BUE-SET-003/005)
            registry.OpenGeneration(feature, generation);
            return registry.CreateView(feature, generation);
        }

        /// <summary>The stop/isolation boundary for one feature's settings
        /// writes (the dispatcher InvalidateOwner precedent). Null-safe: a
        /// never-composed seam has no boundary to withdraw.</summary>
        internal static void InvalidateOwner(FeatureId feature, string reason)
        {
            FeatureSettingsRegistry registry;
            lock (sync) registry = defaultRegistry;
            if (registry == null) return;
            try { registry.InvalidateOwner(feature, reason); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-SET] event=settings-registry result=invalidate-failed feature=" + feature.Value
                    + " reason=" + reason + " errorType=" + error.GetType().Name + " diagnosticId=BUE-SET-GEN");
            }
        }

        /// <summary>Test seam: drop the composed registries so a later
        /// EnsureCreated starts fresh (group isolation, the BueMainThreadRuntime
        /// precedent). Production teardown never calls this.</summary>
        internal static void Clear()
        {
            lock (sync)
            {
                defaultRegistry = null;
                registries.Clear();
            }
        }

        private static FeatureSettingsRegistry CreateLocked(string settingsRoot, Func<bool> authoritySide, Action<string> sink)
        {
            var normalized = NormalizeRoot(settingsRoot);
            var registry = new FeatureSettingsRegistry(
                new FileSettingsPersistence(settingsRoot),
                authoritySide ?? DefaultAuthoritySide(),
                sink ?? (line => BueRuntimeLog.Runtime("[BUE-SET] " + line)));
            registries[normalized] = registry;
            return registry;
        }

        private static Func<bool> DefaultAuthoritySide()
        {
            // U3DS is an authority side by the bootstrap decision alone (no
            // engine call needed); a client is authority exactly when it is
            // the listen host — the SAME engine truth the feature-side
            // authorities use (Provider.isServer), fail-closed on faults.
            // The two environments share ONE code path: U3DS and the P2P
            // listen host mean the same for ServerAuthority scope (V3-T7 裁决③).
            return () =>
            {
                if (BueRuntimeCompletionChain.HeadlessDecision) return true;
                var probe = AuthorityProbeForTests;
                if (probe != null) return probe();
                return EngineAuthoritySide();
            };
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool EngineAuthoritySide()
        {
            try { return SDG.Unturned.Provider.isServer; }
            catch (Exception) { return false; }
        }

        private static string NormalizeRoot(string settingsRoot)
        {
            try { return Path.GetFullPath(settingsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
            catch (Exception) { return settingsRoot ?? string.Empty; }
        }
    }
}
