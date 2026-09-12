using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Registration
{
    /// <summary>
    /// DEV-V4-04: one explicitly registered legacy lifecycle alias — the old
    /// master-switch setting of exactly ONE official feature. The registry is
    /// the anti-scan rule made concrete: the migration engine iterates ONLY
    /// the aliases it is handed (never field names, never ecosystem files),
    /// so an ecosystem feature that happens to ship an "xxx.enabled" toggle
    /// is untouched. The legacy value reader answers true/false, or null when
    /// the value does not exist (no document, retired key, or no reader
    /// composed — e.g. the BII composition on headless hosts). The retire
    /// action removes the legacy key from its document; it self-guards (a
    /// second call after retirement is a no-op) and may be null when the
    /// alias has no document footprint.
    /// </summary>
    public sealed class LegacyEnabledAlias
    {
        public LegacyEnabledAlias(FeatureId feature, string legacySettingId, uint legacySchemaVersion, Func<bool?> readLegacyValue, Action retireLegacyValue)
        {
            Feature = feature;
            LegacySettingId = legacySettingId;
            LegacySchemaVersion = legacySchemaVersion;
            ReadLegacyValue = readLegacyValue;
            RetireLegacyValue = retireLegacyValue;
        }

        public FeatureId Feature { get; }
        public string LegacySettingId { get; }
        public uint LegacySchemaVersion { get; }
        /// <summary>true/false = the persisted legacy value; null = absent or unreadable.</summary>
        public Func<bool?> ReadLegacyValue { get; }
        /// <summary>Best-effort document cleanup after a successful migration; the old value is only removed once the new authority durably exists.</summary>
        public Action RetireLegacyValue { get; }
    }

    /// <summary>
    /// DEV-V4-04: the official legacy enabled migration engine (spec「官方
    /// legacy enabled 迁移」). Per registered alias:
    ///   - an existing durable UserDisabled intent IS the new authority — the
    ///     legacy value is not even read; the disable target is re-submitted
    ///     to the lifecycle machine so a feature the catalog just started is
    ///     stopped again (no new generation — the disable is a no-op-shaped
    ///     stop), and a stale legacy key still gets retired;
    ///   - legacy true or absent → NO lifecycle change, nothing written;
    ///   - legacy false → submit the disable target to the machine (its
    ///     DEV-V4-03 semantics interpret: Running→UserDisabled stop,
    ///     Isolated→kept-isolation no-op success, transitions→explicit
    ///     failure); the machine seam durably records the intent fact on
    ///     success — the old value is retired ONLY after the interpreter
    ///     succeeded AND the intent is confirmed present, so any earlier
    ///     fault (machine rejection, persistence fault) leaves the legacy
    ///     value byte-intact for the next load to retry (自愈).
    /// The engine is idempotent across repeated loads: retired keys read as
    /// absent and honored intents produce machine no-ops — no duplicate
    /// generations, no repeated rewrites.
    /// Diagnostic id BUE-LIFE-MIGRATE marks real transitions and failures
    /// only; true/absent aliases stay silent.
    /// </summary>
    public static class LegacyEnabledMigration
    {
        private const string DiagnosticId = "BUE-LIFE-MIGRATE";

        public static void Run(IReadOnlyList<LegacyEnabledAlias> aliases, FeatureLifecycleIntentStore intents, Func<FeatureId, bool> interpretDisableTarget, Action<string> sink)
        {
            if (aliases == null || aliases.Count == 0) return;
            if (intents == null)
            {
                // Fail closed: without the durable intent store a migration
                // could disable a feature with nowhere to record the fact —
                // the legacy value would then re-disable after every user
                // re-enable. Never run half-backed.
                Emit(sink, "event=legacy-enabled-migration result=skipped reason=intent-store-absent");
                return;
            }
            if (interpretDisableTarget == null) return;
            for (var i = 0; i < aliases.Count; i++)
            {
                var alias = aliases[i];
                if (alias == null || string.IsNullOrEmpty(alias.Feature.Value)) continue;
                if (intents.HasUserDisabled(alias.Feature))
                {
                    // 新权威以新为准：不再读旧值；重交停用目标让机把本次启动
                    // 解释回用户停用（停用不新开代际），并顺带清掉可能残留的旧键。
                    if (interpretDisableTarget(alias.Feature))
                    {
                        TryRetire(alias, sink);
                        Emit(sink, "event=legacy-enabled-migration result=honored feature=" + alias.Feature.Value);
                    }
                    else
                    {
                        Emit(sink, "event=legacy-enabled-migration result=honor-failed feature=" + alias.Feature.Value);
                    }
                    continue;
                }
                bool? legacy = alias.ReadLegacyValue == null ? null : SafeRead(alias, sink);
                // true 或不存在 → 不额外改生命周期（静默，无行）。
                if (legacy == null || legacy.Value) continue;
                if (!interpretDisableTarget(alias.Feature))
                {
                    // 机显式拒绝（未注册/过渡期）：旧值原样保留，下次加载重试。
                    Emit(sink, "event=legacy-enabled-migration result=interpret-failed feature=" + alias.Feature.Value);
                    continue;
                }
                if (!intents.HasUserDisabled(alias.Feature))
                {
                    // 机停了但意图事实没落盘（持久故障）：保留旧值自愈，不退役。
                    Emit(sink, "event=legacy-enabled-migration result=intent-missing feature=" + alias.Feature.Value);
                    continue;
                }
                TryRetire(alias, sink);
                Emit(sink, "event=legacy-enabled-migration result=migrated feature=" + alias.Feature.Value
                    + " legacy=" + alias.LegacySettingId);
            }
        }

        private static bool? SafeRead(LegacyEnabledAlias alias, Action<string> sink)
        {
            try { return alias.ReadLegacyValue(); }
            catch (Exception error)
            {
                Emit(sink, "event=legacy-enabled-migration result=read-failed feature=" + alias.Feature.Value
                    + " errorType=" + error.GetType().Name);
                return null;
            }
        }

        private static void TryRetire(LegacyEnabledAlias alias, Action<string> sink)
        {
            var retire = alias.RetireLegacyValue;
            if (retire == null) return;
            try { retire(); }
            catch (Exception error)
            {
                // 退役是尽力而为：旧键残留只意味着下次加载再清一次，但故障必须留痕。
                Emit(sink, "event=legacy-enabled-migration result=retire-failed feature=" + alias.Feature.Value
                    + " errorType=" + error.GetType().Name);
            }
        }

        private static void Emit(Action<string> sink, string line)
        {
            if (sink == null) return;
            try { sink(line + " diagnosticId=" + DiagnosticId); }
            catch (Exception) { }
        }
    }
}
