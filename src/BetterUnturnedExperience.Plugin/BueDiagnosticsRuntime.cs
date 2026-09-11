using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Diagnostics;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V3-07: plugin composition root for the platform diagnostics
    /// (the BueMainThreadRuntime / BueSettingsRuntime precedent — one host-
    /// owned seam created on first use, never feature-specific). It binds the
    /// Core <see cref="DiagnosticRuntime"/> to the plugin's own log channels:
    /// every accepted diagnostic line at its write's level goes to BepInEx
    /// LogOutput (Info/Warning/Error — ALL stay visible in a normal UMM log
    /// export, which is the player-facing evidence chain V3-T8 froze; the
    /// Debug verbosity policy stays for runtime chatter, diagnostics are not
    /// chatter). A faulty writer lands on the ErrorFriendly fallback channel
    /// (the one line the platform still guarantees). The stop/isolation
    /// boundaries ride the start path's existing invalidation sites, and the
    /// DEV-V3-03 lifecycle machine / T5 network runtime sinks get a DOUBLE
    /// BINDING here (absorption): the original line path stays byte-for-byte
    /// what 03/04 anchored, while each coded line additionally feeds the one
    /// unified summary. Internal on purpose: the aggregator is host
    /// composition, not third-party surface — external features bind only
    /// through IFeatureBootstrap.Logger.
    /// </summary>
    internal static class BueDiagnosticsRuntime
    {
        private static readonly object sync = new object();
        private static DiagnosticRuntime runtime;

        /// <summary>The host aggregator (created on first access — the lazy
        /// composition seam never goes null mid-flight).</summary>
        internal static DiagnosticRuntime Runtime
        {
            get
            {
                lock (sync)
                {
                    if (runtime == null)
                    {
                        runtime = new DiagnosticRuntime(WriteLine, null, null, WriteFallback);
                        BueRuntimeLog.Runtime("[BUE-DIAG] BUE diagnostics runtime created diagnosticId=BUE-LOG-CREATED");
                    }
                    return runtime;
                }
            }
        }

        /// <summary>The Logger matrix wiring for one (feature, generation):
        /// opens the generation on the ledger (superseding any older one —
        /// the dispatcher/settings OpenGeneration precedent) and returns the
        /// feature-bound view. Unlike Settings there is NO facet gate: every
        /// started feature is wired (the availability-matrix row 07).</summary>
        internal static IFeatureLogger ComposeViewForStart(FeatureId feature, ulong generation)
        {
            var current = Runtime;
            current.OpenGeneration(feature.Value, generation);
            return current.CreateLoggerView(feature.Value, generation);
        }

        /// <summary>The stop/isolation boundary for one feature's writes (the
        /// InvalidateOwner call sites shared with 04/06). Null-safe.</summary>
        internal static void InvalidateOwner(FeatureId feature, string reason)
        {
            DiagnosticRuntime current;
            lock (sync) current = runtime;
            if (current == null) return;
            try { current.InvalidateOwner(feature.Value, reason); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-DIAG] event=diagnostics-runtime result=invalidate-failed feature=" + feature.Value
                    + " reason=" + reason + " errorType=" + error.GetType().Name + " diagnosticId=BUE-LOG-GEN");
            }
        }

        /// <summary>The host stop boundary (PluginStopping teardown): every
        /// captured view then refuses at the write boundary (explicit, latched
        /// — never silent).</summary>
        internal static void ShutdownHost(string reason)
        {
            DiagnosticRuntime current;
            lock (sync) current = runtime;
            if (current == null) return;
            try { current.ShutdownHost(reason); }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-DIAG] event=diagnostics-runtime result=shutdown-failed reason=" + reason
                    + " errorType=" + error.GetType().Name + " diagnosticId=BUE-LOG-GEN");
            }
        }

        /// <summary>The absorption entry the host seams' double-bound sinks
        /// call (lifecycle machine, network runtime): never throws into the
        /// producing seam. The aggregator is composed on first absorbed line
        /// (the unified sink exists from the host's first diagnostic — the
        /// BeginStart state line CAN arrive before any feature view is
        /// composed, e.g. a factory-fault isolation; refusing to create here
        /// would silently drop the first transition).</summary>
        internal static void AggregateHostLine(string fallbackFeatureId, DiagnosticLevel level, string line)
        {
            try
            {
                Runtime.AggregateHostLine(fallbackFeatureId, level, line);
            }
            catch (Exception)
            {
                // the producing seam's own sink isolation is the outer belt
            }
        }

        /// <summary>Test seam: drop the composed aggregator so a later first
        /// access starts fresh (group isolation, the BueMainThreadRuntime /
        /// BueSettingsRuntime precedent). Production teardown never calls
        /// this — the summary dies with the process (重启不持久).</summary>
        internal static void Clear()
        {
            lock (sync) runtime = null;
        }

        private static void WriteLine(string line, DiagnosticLevel level)
        {
            // Level mapping is the visibility promise: diagnostics never ride
            // the Debug channel (normal-play silence is for runtime chatter —
            // the player-facing chain V3-T8 froze needs the lines IN the
            // exported log).
            switch (level)
            {
                case DiagnosticLevel.Warning: BueRuntimeLog.Warn(line); break;
                case DiagnosticLevel.Error: BueRuntimeLog.Error(line); break;
                default: BueRuntimeLog.Load(line); break;
            }
        }

        private static void WriteFallback(string line)
        {
            // The Core fault line already carries its [BUE-DIAG] tag — the
            // composition root adds no second one (frozen line format, one
            // prefix per line).
            BueRuntimeLog.ErrorFriendly(line);
        }
    }
}
