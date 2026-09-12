using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Settings;

namespace BetterUnturnedExperience.Core.Registration
{
    /// <summary>
    /// DEV-V4-04: the durable UserDisabled intent fact store — the "新权威"
    /// the legacy enabled migration writes before the machine interprets it.
    /// One host-owned settings document (feature key = the BUE reserved root,
    /// which no feature can register) carries one Toggle(true) entry per
    /// feature the user turned off, so a user disable survives restarts and
    /// an explicit re-enable clears it. The record is the SINGLE durable
    /// lifecycle intent: the panel disable seam records it on success, the
    /// enable seam clears it on success, and the legacy migration consults
    /// it instead of re-reading retired toggle files.
    ///
    /// All operations are idempotent and fail-closed: a persistence fault
    /// leaves the previous document byte-intact (the FileSettingsPersistence
    /// replace discipline) and reports false, so a migration that could not
    /// land the intent simply retries from the legacy value next load.
    /// Diagnostic id BUE-LIFE-INTENT marks real transitions and faults only
    /// — no-op reads stay silent (the log-storm discipline).
    /// </summary>
    public sealed class FeatureLifecycleIntentStore
    {
        /// <summary>The host document identity: under the reserved root, never
        /// colliding with a feature's own settings document.</summary>
        internal const string HostDocumentFeatureId = "io.github.yu80rice.bue";
        private const uint DocumentSchemaVersion = 1;
        private const string DiagnosticId = "BUE-LIFE-INTENT";

        private readonly object sync = new object();
        private readonly ISettingsPersistence persistence;
        private readonly Action<string> diagnosticSink;
        private readonly FeatureId hostDocumentFeature;

        public FeatureLifecycleIntentStore(ISettingsPersistence persistence, Action<string> diagnosticSink)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.diagnosticSink = diagnosticSink;
            hostDocumentFeature = new FeatureId(HostDocumentFeatureId);
        }

        /// <summary>True while a durable UserDisabled intent exists for the
        /// feature. Faults read as "absent" — the legacy value stays the
        /// fallback authority and the next load re-derives the truth.</summary>
        public bool HasUserDisabled(FeatureId feature)
        {
            if (string.IsNullOrEmpty(feature.Value)) return false;
            SettingValue value;
            return TryReadEntry(feature.Value, out value) && value.Kind == SettingKind.Toggle && value.Boolean;
        }

        /// <summary>Records the UserDisabled intent (idempotent). False = the
        /// durable write failed; the caller's intent did NOT land and any
        /// legacy value must be preserved for the next attempt.</summary>
        public bool RecordUserDisabled(FeatureId feature)
        {
            if (string.IsNullOrEmpty(feature.Value)) return false;
            lock (sync)
            {
                SettingValue existing;
                if (TryReadEntry(feature.Value, out existing) && existing.Kind == SettingKind.Toggle && existing.Boolean) return true;
                if (!CommitEntry(feature.Value, true)) return false;
                Emit("event=lifecycle-intent result=recorded feature=" + feature.Value);
                return true;
            }
        }

        /// <summary>Clears the UserDisabled intent — the explicit user
        /// re-enable path overturns the record (idempotent). False = the
        /// durable write failed; the record still stands.</summary>
        public bool Clear(FeatureId feature)
        {
            if (string.IsNullOrEmpty(feature.Value)) return false;
            lock (sync)
            {
                if (!HasUserDisabledLocked(feature.Value)) return true;
                if (!CommitEntry(feature.Value, false)) return false;
                Emit("event=lifecycle-intent result=cleared feature=" + feature.Value);
                return true;
            }
        }

        // Locking discipline: Record/Clear hold `sync` for their read-modify-
        // write; HasUserDisabled (the hot read) locks inside the helper. The
        // intent document is a boot-time/panel-time surface — never per-frame.
        private bool HasUserDisabledLocked(string featureValue)
        {
            SettingValue value;
            return TryReadEntry(featureValue, out value) && value.Kind == SettingKind.Toggle && value.Boolean;
        }

        private bool TryReadEntry(string featureValue, out SettingValue value)
        {
            value = default(SettingValue);
            lock (sync)
            {
                try
                {
                    var loaded = persistence.Load(hostDocumentFeature, SettingRevisionScope.ClientPreference, DocumentSchemaVersion, null);
                    if (!loaded.IsValid) return false;
                    return loaded.Values.TryGetValue(featureValue, out value);
                }
                catch (Exception error)
                {
                    Emit("event=lifecycle-intent result=read-failed feature=" + featureValue + " errorType=" + error.GetType().Name);
                    return false;
                }
            }
        }

        private bool CommitEntry(string featureValue, bool disabled)
        {
            try
            {
                var loaded = persistence.Load(hostDocumentFeature, SettingRevisionScope.ClientPreference, DocumentSchemaVersion, null);
                var values = new Dictionary<string, SettingValue>(StringComparer.Ordinal);
                if (loaded.IsValid)
                {
                    foreach (var pair in loaded.Values) values[pair.Key] = pair.Value;
                }
                uint revision = loaded.IsValid ? loaded.Revision : 0;
                if (disabled) values[featureValue] = SettingValue.Toggle(true);
                else
                {
                    if (!values.Remove(featureValue)) return true;
                }
                string diagnostic;
                if (!persistence.TryCommit(hostDocumentFeature, SettingRevisionScope.ClientPreference, DocumentSchemaVersion, revision, values, out diagnostic))
                {
                    Emit("event=lifecycle-intent result=commit-failed feature=" + featureValue + " diagnostic=" + diagnostic);
                    return false;
                }
                return true;
            }
            catch (Exception error)
            {
                Emit("event=lifecycle-intent result=commit-failed feature=" + featureValue + " errorType=" + error.GetType().Name);
                return false;
            }
        }

        private void Emit(string line)
        {
            var sink = diagnosticSink;
            if (sink == null) return;
            try { sink(line + " diagnosticId=" + DiagnosticId); }
            catch (Exception) { }
        }
    }
}
