using System;
using System.Collections.Generic;
using System.IO;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Core.Settings;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V4-04: plugin composition root for the durable lifecycle intent
    /// store (the BueSettingsRuntime composition-root precedent — one
    /// host-owned seam composed at bootstrap, root-keyed resolution for
    /// adapters, never feature-specific). The DEFAULT store backs the
    /// machine seam (SetFeatureEnabled records a UserDisabled intent on
    /// every disable success and clears it on every enable success) and the
    /// legacy migration; adapters built with an explicit settings root
    /// resolve the SAME store when the roots match (production: one root,
    /// one store). Not composed → the machine seam silently skips: host
    /// tests without a composed root keep their 03 semantics untouched.
    /// Internal on purpose: lifecycle intent is host composition, not
    /// third-party surface.
    /// </summary>
    internal static class BueFeatureIntentRuntime
    {
        private static readonly object sync = new object();
        private static readonly Dictionary<string, FeatureLifecycleIntentStore> stores =
            new Dictionary<string, FeatureLifecycleIntentStore>(StringComparer.OrdinalIgnoreCase);
        private static FeatureLifecycleIntentStore defaultStore;

        /// <summary>One-shot composition of the default store (first call
        /// wins; re-creation requires Clear — the group-reset seam).</summary>
        internal static bool EnsureCreated(string settingsRoot, Action<string> sink = null)
        {
            lock (sync)
            {
                if (defaultStore != null) return false;
                defaultStore = CreateLocked(settingsRoot, sink);
                return true;
            }
        }

        /// <summary>Composition seam over an EXPLICIT persistence (the
        /// FileSettingsPersistence fault-injection seam drives the real
        /// intent-write-failure path in host tests). Registers the store
        /// under the root too, so the machine hook (Default) and the
        /// migration/adapter (StoreFor) resolve the SAME failing instance —
        /// the production topology: one store, both consumers.</summary>
        internal static bool EnsureCreatedWith(string settingsRoot, ISettingsPersistence persistence, Action<string> sink = null)
        {
            lock (sync)
            {
                if (defaultStore != null) return false;
                var store = new FeatureLifecycleIntentStore(persistence, sink ?? (line => BueRuntimeLog.Runtime("[BUE-LIFE] " + line)));
                defaultStore = store;
                stores[NormalizeRoot(settingsRoot)] = store;
                return true;
            }
        }

        /// <summary>The default store — the machine seam and the migration
        /// consult it; null when never composed (tests keep no-op semantics).</summary>
        internal static FeatureLifecycleIntentStore Default
        {
            get { lock (sync) return defaultStore; }
        }

        /// <summary>Root-keyed resolution: the production adapters and the
        /// start path share ONE store when their roots are the same path;
        /// an injected test root gets its own (deterministic, isolated).</summary>
        internal static FeatureLifecycleIntentStore StoreFor(string settingsRoot)
        {
            lock (sync)
            {
                FeatureLifecycleIntentStore store;
                if (stores.TryGetValue(NormalizeRoot(settingsRoot), out store) && store != null) return store;
                store = CreateLocked(settingsRoot, null);
                if (defaultStore == null) defaultStore = store;
                return store;
            }
        }

        /// <summary>Test seam: drop the composed stores so a later
        /// EnsureCreated starts fresh (group isolation precedent).</summary>
        internal static void Clear()
        {
            lock (sync)
            {
                defaultStore = null;
                stores.Clear();
            }
        }

        /// <summary>The machine seam's record hook (null-store-safe): the
        /// disable paths call this AFTER a successful user-disable/noop so
        /// the intent survives restarts.</summary>
        internal static void RecordUserDisabledOnDefault(FeatureId feature)
        {
            var store = Default;
            if (store == null) return;
            try
            {
                if (!store.RecordUserDisabled(feature))
                    // 意图没落盘=停用事实可能跨重启丢失；显式留痕（旧键在则自愈重试）。
                    BueRuntimeLog.Runtime("[BUE-LIFE] event=lifecycle-intent result=record-failed feature=" + feature.Value
                        + " diagnosticId=BUE-LIFE-INTENT");
            }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-LIFE] event=lifecycle-intent result=record-hook-failed feature=" + feature.Value
                    + " errorType=" + error.GetType().Name + " diagnosticId=BUE-LIFE-INTENT");
            }
        }

        /// <summary>The machine seam's clear hook (null-store-safe): the
        /// enable path calls this AFTER a successful user enable — the
        /// explicit re-enable overturns the durable disable.</summary>
        internal static void ClearUserDisabledOnDefault(FeatureId feature)
        {
            var store = Default;
            if (store == null) return;
            try
            {
                if (!store.Clear(feature))
                    // 清除没落盘=下次启动 honor 路径会再停一次；显式留痕。
                    BueRuntimeLog.Runtime("[BUE-LIFE] event=lifecycle-intent result=clear-failed feature=" + feature.Value
                        + " diagnosticId=BUE-LIFE-INTENT");
            }
            catch (Exception error)
            {
                BueRuntimeLog.Runtime("[BUE-LIFE] event=lifecycle-intent result=clear-hook-failed feature=" + feature.Value
                    + " errorType=" + error.GetType().Name + " diagnosticId=BUE-LIFE-INTENT");
            }
        }

        private static FeatureLifecycleIntentStore CreateLocked(string settingsRoot, Action<string> sink)
        {
            var normalized = NormalizeRoot(settingsRoot);
            var store = new FeatureLifecycleIntentStore(
                new FileSettingsPersistence(settingsRoot),
                sink ?? (line => BueRuntimeLog.Runtime("[BUE-LIFE] " + line)));
            stores[normalized] = store;
            return store;
        }

        private static string NormalizeRoot(string settingsRoot)
        {
            try { return Path.GetFullPath(settingsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
            catch (Exception) { return settingsRoot ?? string.Empty; }
        }
    }
}
