using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Settings
{
    public sealed class SettingsPersistenceLoadResult
    {
        internal SettingsPersistenceLoadResult(bool valid, bool usedSafeDefaults, uint revision, IReadOnlyDictionary<string, SettingValue> values, string diagnosticId)
        {
            IsValid = valid;
            UsedSafeDefaults = usedSafeDefaults;
            Revision = revision;
            Values = values;
            DiagnosticId = diagnosticId ?? string.Empty;
        }

        public bool IsValid { get; }
        public bool UsedSafeDefaults { get; }
        public uint Revision { get; }
        public IReadOnlyDictionary<string, SettingValue> Values { get; }
        public string DiagnosticId { get; }
    }

    public interface ISettingsPersistence
    {
        SettingsPersistenceLoadResult Load(FeatureId feature, SettingRevisionScope scope, uint schemaVersion, IReadOnlyList<SettingDescriptor> descriptors);
        bool TryCommit(FeatureId feature, SettingRevisionScope scope, uint schemaVersion, uint revision, IReadOnlyDictionary<string, SettingValue> values, out string diagnosticId);
        string GetPath(FeatureId feature, SettingRevisionScope scope);
    }

    public sealed class InMemorySettingsPersistence : ISettingsPersistence
    {
        private readonly object sync = new object();
        private readonly Dictionary<string, MemoryDocument> documents = new Dictionary<string, MemoryDocument>(StringComparer.Ordinal);

        public bool FailNextCommit { get; set; }
        public bool FailNextReplace { get; set; }
        public bool CorruptNextLoad { get; set; }

        public SettingsPersistenceLoadResult Load(FeatureId feature, SettingRevisionScope scope, uint schemaVersion, IReadOnlyList<SettingDescriptor> descriptors)
        {
            lock (sync)
            {
                if (CorruptNextLoad)
                {
                    CorruptNextLoad = false;
                    return Defaults("InMemoryCorrupt");
                }
                MemoryDocument document;
                if (!documents.TryGetValue(Key(feature, scope), out document)) return Defaults("InMemoryDefaults");
                return new SettingsPersistenceLoadResult(true, false, document.Revision, Copy(document.Values), string.Empty);
            }
        }

        public bool TryCommit(FeatureId feature, SettingRevisionScope scope, uint schemaVersion, uint revision, IReadOnlyDictionary<string, SettingValue> values, out string diagnosticId)
        {
            lock (sync)
            {
                if (FailNextCommit)
                {
                    FailNextCommit = false;
                    diagnosticId = "InMemoryCommitFailure";
                    return false;
                }
                documents[Key(feature, scope)] = new MemoryDocument(revision, Copy(values));
                diagnosticId = string.Empty;
                return true;
            }
        }

        public string GetPath(FeatureId feature, SettingRevisionScope scope) { return string.Empty; }

        private static string Key(FeatureId feature, SettingRevisionScope scope) { return (feature.Value ?? string.Empty) + "\u001f" + scope; }
        private static IReadOnlyDictionary<string, SettingValue> Copy(IReadOnlyDictionary<string, SettingValue> values) { var copy = new Dictionary<string, SettingValue>(StringComparer.Ordinal); foreach (var pair in values) copy.Add(pair.Key, pair.Value); return new ReadOnlyDictionary<string, SettingValue>(copy); }
        private static SettingsPersistenceLoadResult Defaults(string diagnostic) { return new SettingsPersistenceLoadResult(false, true, 0, new ReadOnlyDictionary<string, SettingValue>(new Dictionary<string, SettingValue>(StringComparer.Ordinal)), diagnostic); }

        private sealed class MemoryDocument
        {
            public MemoryDocument(uint revision, IReadOnlyDictionary<string, SettingValue> values) { Revision = revision; Values = values; }
            public uint Revision { get; }
            public IReadOnlyDictionary<string, SettingValue> Values { get; }
        }
    }

    public sealed class FileSettingsPersistence : ISettingsPersistence
    {
        private const string Magic = "BUE_SETTINGS_DOCUMENT_V1";
        private readonly string rootPath;
        public bool FailNextReplace { get; set; }

        public FileSettingsPersistence(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("Settings root is required.", nameof(rootPath));
            this.rootPath = Path.GetFullPath(rootPath);
            Directory.CreateDirectory(this.rootPath);
        }

        public string GetPath(FeatureId feature, SettingRevisionScope scope)
        {
            var safeFeature = feature.Value ?? string.Empty;
            if (safeFeature.Length == 0 || safeFeature.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("Feature identity cannot be used as a settings file name.", nameof(feature));
            return Path.Combine(rootPath, safeFeature + "." + scope + ".bue-settings");
        }

        public SettingsPersistenceLoadResult Load(FeatureId feature, SettingRevisionScope scope, uint schemaVersion, IReadOnlyList<SettingDescriptor> descriptors)
        {
            var path = GetPath(feature, scope);
            if (!File.Exists(path)) return Defaults("FileDefaults");
            try
            {
                uint revision;
                Dictionary<string, SettingValue> values;
                string error;
                if (TryRead(path, feature, scope, schemaVersion, descriptors, out revision, out values, out error)) return new SettingsPersistenceLoadResult(true, false, revision, new ReadOnlyDictionary<string, SettingValue>(values), string.Empty);
                var quarantine = path + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + "." + Guid.NewGuid().ToString("N") + ".bak";
                try { File.Move(path, quarantine); } catch (IOException) { }
                return Defaults(error.Length == 0 ? "FileCorrupt" : error);
            }
            catch (Exception error)
            {
                return Defaults("FileLoadFailure:" + error.GetType().Name);
            }
        }

        public bool TryCommit(FeatureId feature, SettingRevisionScope scope, uint schemaVersion, uint revision, IReadOnlyDictionary<string, SettingValue> values, out string diagnosticId)
        {
            var target = GetPath(feature, scope);
            var temporary = target + ".tmp." + Guid.NewGuid().ToString("N");
            try
            {
                var body = Serialize(feature, scope, schemaVersion, revision, values);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(body);
                    writer.Flush();
                    stream.Flush(true);
                }
                uint checkedRevision;
                Dictionary<string, SettingValue> checkedValues;
                string error;
                if (!TryRead(temporary, feature, scope, schemaVersion, null, out checkedRevision, out checkedValues, out error) || checkedRevision != revision || !SameValues(values, checkedValues))
                {
                    diagnosticId = "PersistenceReadBackFailed:" + error;
                    return false;
                }
                if (FailNextReplace)
                {
                    FailNextReplace = false;
                    diagnosticId = "PersistenceReplaceFailed:Injected";
                    return false;
                }
                if (File.Exists(target)) File.Replace(temporary, target, null);
                else File.Move(temporary, target);
                diagnosticId = string.Empty;
                return true;
            }
            catch (Exception error)
            {
                diagnosticId = "PersistenceWriteFailed:" + error.GetType().Name + ":" + error.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException) { }
            }
        }

        private static string Serialize(FeatureId feature, SettingRevisionScope scope, uint schemaVersion, uint revision, IReadOnlyDictionary<string, SettingValue> values)
        {
            var lines = new List<string> { Magic, "feature=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(feature.Value ?? string.Empty)), "scope=" + scope, "schema=" + schemaVersion.ToString(CultureInfo.InvariantCulture), "revision=" + revision.ToString(CultureInfo.InvariantCulture) };
            foreach (var pair in values.OrderBy(p => p.Key, StringComparer.Ordinal)) lines.Add("entry=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(pair.Key)) + "|" + EncodeValue(pair.Value));
            var body = string.Join("\n", lines);
            return body + "\ndigest=" + DigestText(body) + "\n";
        }

        private static bool TryRead(string path, FeatureId feature, SettingRevisionScope scope, uint schemaVersion, IReadOnlyList<SettingDescriptor> descriptors, out uint revision, out Dictionary<string, SettingValue> values, out string error)
        {
            revision = 0;
            values = new Dictionary<string, SettingValue>(StringComparer.Ordinal);
            error = string.Empty;
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 6 || lines[0] != Magic) { error = "FileFormatUnsupported"; return false; }
            var expectedDigest = lines[lines.Length - 1].StartsWith("digest=", StringComparison.Ordinal) ? lines[lines.Length - 1].Substring(7) : string.Empty;
            if (expectedDigest.Length == 0) { error = "FileIntegrityMissing"; return false; }
            var body = string.Join("\n", lines.Take(lines.Length - 1));
            if (!string.Equals(expectedDigest, DigestText(body), StringComparison.Ordinal)) { error = "FileIntegrityFailed"; return false; }
            if (!ReadField(lines, "feature=", out var encodedFeature) || !string.Equals(Encoding.UTF8.GetString(Convert.FromBase64String(encodedFeature)), feature.Value ?? string.Empty, StringComparison.Ordinal)) { error = "FileFeatureMismatch"; return false; }
            if (!ReadField(lines, "scope=", out var storedScope) || !string.Equals(storedScope, scope.ToString(), StringComparison.Ordinal)) { error = "FileScopeMismatch"; return false; }
            if (!ReadField(lines, "schema=", out var storedSchema) || !uint.TryParse(storedSchema, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedSchema) || parsedSchema != schemaVersion) { error = "FileSchemaMismatch"; return false; }
            if (!ReadField(lines, "revision=", out var storedRevision) || !uint.TryParse(storedRevision, NumberStyles.None, CultureInfo.InvariantCulture, out revision)) { error = "FileRevisionInvalid"; return false; }
            foreach (var line in lines.Where(x => x.StartsWith("entry=", StringComparison.Ordinal)))
            {
                var parts = line.Substring(6).Split('|');
                if (parts.Length != 6) { error = "FileEntryInvalid"; return false; }
                string id;
                try { id = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])); } catch (FormatException) { error = "FileEntryInvalid"; return false; }
                if (values.ContainsKey(id)) { error = "FileDuplicateSetting"; return false; }
                SettingValue value;
                if (!TryDecodeValue(parts, out value)) { error = "FileValueInvalid"; return false; }
                values.Add(id, value);
            }
            // DEV-V4-06: the document's digest is the integrity gate (any
            // dropped entry line breaks it → FileIntegrityFailed → the file
            // quarantines). Whether every current descriptor key is PRESENT
            // is not a corruption question: a legacy document from before a
            // schema change legitimately lacks the new keys (the DEV-V4-04
            // migration window — the file must survive untouched until the
            // migration retires the legacy key). Per-key decisions belong to
            // the runtime's LoadScope (unknown keys ignored, absent keys get
            // descriptor defaults), never to the file layer.
            return true;
        }

        private static bool ReadField(string[] lines, string prefix, out string value) { var line = lines.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal)); if (line == null) { value = string.Empty; return false; } value = line.Substring(prefix.Length); return true; }
        private static string EncodeValue(SettingValue value) { return ((byte)value.Kind).ToString(CultureInfo.InvariantCulture) + "|" + (value.Boolean ? "1" : "0") + "|" + value.Integer.ToString(CultureInfo.InvariantCulture) + "|" + value.Float.ToString("R", CultureInfo.InvariantCulture) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Text ?? string.Empty)); }
        private static bool TryDecodeValue(string[] parts, out SettingValue value)
        {
            value = default(SettingValue);
            if (!byte.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var kindByte) || !Enum.IsDefined(typeof(SettingKind), kindByte) || (parts[2] != "0" && parts[2] != "1") || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) || !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var @float) || float.IsNaN(@float) || float.IsInfinity(@float)) return false;
            string text; try { text = Encoding.UTF8.GetString(Convert.FromBase64String(parts[5])); } catch (FormatException) { return false; }
            value = new SettingValue((SettingKind)kindByte, parts[2] == "1", integer, @float, text);
            return true;
        }
        private static SettingsPersistenceLoadResult Defaults(string diagnostic) { return new SettingsPersistenceLoadResult(false, true, 0, new ReadOnlyDictionary<string, SettingValue>(new Dictionary<string, SettingValue>(StringComparer.Ordinal)), diagnostic); }
        private static string DigestText(string body) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", string.Empty); }
        private static bool SameValues(IReadOnlyDictionary<string, SettingValue> left, IReadOnlyDictionary<string, SettingValue> right) { return left.Count == right.Count && left.All(pair => right.ContainsKey(pair.Key) && SettingsRuntime.ValuesEqual(pair.Value, right[pair.Key])); }
    }

    public sealed class SettingsRuntime : IScopedFeatureSettings
    {
        private const int ReplayLimit = 128;
        private readonly object sync = new object();
        private readonly FeatureId feature;
        private readonly IReadOnlyList<SettingDescriptor> descriptors;
        private readonly Dictionary<string, SettingDescriptor> descriptorMap;
        private readonly ISettingsPersistence persistence;
        private readonly ScopeState client;
        private readonly ScopeState server;
        private ulong overlayGeneration;
        private ulong generationWatermark;
        private bool generationActive;
        private Dictionary<string, SettingPolicyView> policies = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal);
        public string LastPersistenceDiagnosticId { get; private set; }

        public SettingsRuntime(FeatureId feature, IEnumerable<SettingDescriptor> descriptors, ISettingsPersistence persistence)
        {
            if (string.IsNullOrWhiteSpace(feature.Value)) throw new ArgumentException("Feature identity is required.", nameof(feature));
            if (persistence == null) throw new ArgumentNullException(nameof(persistence));
            var list = (descriptors ?? Enumerable.Empty<SettingDescriptor>()).ToArray();
            var diagnostics = ValidateDescriptors(feature, list);
            if (diagnostics.Count != 0) throw new ArgumentException(string.Join(";", diagnostics), nameof(descriptors));
            this.feature = feature;
            this.descriptors = new ReadOnlyCollection<SettingDescriptor>(list.Select(CloneDescriptor).OrderBy(x => x.SortOrder).ThenBy(x => x.SettingId, StringComparer.Ordinal).ToArray());
            descriptorMap = this.descriptors.ToDictionary(x => x.SettingId, StringComparer.Ordinal);
            this.persistence = persistence;
            client = LoadScope(SettingRevisionScope.ClientPreference);
            server = LoadScope(SettingRevisionScope.ServerAuthority);
        }

        public FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope) { if (!IsKnownScope(revisionScope)) throw new ArgumentOutOfRangeException(nameof(revisionScope)); lock (sync) return BuildSnapshot(revisionScope); }

        public bool TryGet(string settingId, out SettingValue value, out uint revision)
        {
            lock (sync)
            {
                var scope = client;
                SettingValue stored;
                if (!descriptorMap.ContainsKey(settingId) || !scope.Values.TryGetValue(settingId, out stored)) { value = default(SettingValue); revision = scope.Revision; return false; }
                var entry = BuildEntry(descriptorMap[settingId], stored);
                value = entry.EffectiveValue;
                revision = scope.Revision;
                return true;
            }
        }

        public SettingChangeResult Submit(ScopedSettingChangeRequest request)
        {
            lock (sync)
            {
                if (!IsKnownScope(request.RevisionScope)) return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, default(FeatureSettingsSnapshot));
                var state = request.RevisionScope == SettingRevisionScope.ClientPreference ? client : server;
                var fingerprint = Fingerprint(request);
                ReplayRecord replay;
                if (request.RequestId != 0 && state.Replay.TryGetValue(request.RequestId, out replay))
                {
                    if (string.Equals(replay.Fingerprint, fingerprint, StringComparison.Ordinal)) return replay.Result;
                    return Reject(FrameworkErrorCode.RequestIdConflict, state);
                }
                if (request.RequestId != 0 && state.Replay.Count >= ReplayLimit) return Reject(FrameworkErrorCode.RateLimited, state);
                SettingChangeResult result;
                if (request.RequestId == 0) result = Reject(FrameworkErrorCode.SettingRejected, state);
                else if (request.ExpectedRevision != state.Revision) result = Reject(FrameworkErrorCode.SettingRevisionConflict, state);
                else result = Apply(state, request);
                return Remember(state, request.RequestId, fingerprint, result);
            }
        }

        public bool ApplyServerPolicy(ulong connectionGeneration, IReadOnlyDictionary<string, SettingPolicyView> policy)
        {
            lock (sync)
            {
                if (connectionGeneration == 0 || policy == null) return false;
                var copy = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal);
                foreach (var pair in policy)
                {
                    SettingDescriptor descriptor;
                    if (!descriptorMap.TryGetValue(pair.Key, out descriptor) || descriptor.Authority != SettingAuthority.ServerPolicyWithClientPreference || !PolicyCompatible(descriptor, pair.Value)) return false;
                    copy.Add(pair.Key, ClonePolicy(pair.Value));
                }
                if (generationActive)
                {
                    if (connectionGeneration < overlayGeneration) return false;
                    if (connectionGeneration > overlayGeneration) ActivateConnectionGenerationCore(connectionGeneration);
                }
                else
                {
                    if (connectionGeneration <= generationWatermark) return false;
                    ActivateConnectionGenerationCore(connectionGeneration);
                }
                overlayGeneration = connectionGeneration;
                generationActive = true;
                policies = copy;
                return true;
            }
        }

        public void ClearSessionOverlay(ulong connectionGeneration)
        {
            lock (sync) { if (generationActive && connectionGeneration == overlayGeneration) { generationActive = false; policies = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal); client.Replay.Clear(); server.Replay.Clear(); } }
        }

        public void ActivateConnectionGeneration(ulong connectionGeneration)
        {
            if (connectionGeneration == 0) throw new ArgumentOutOfRangeException(nameof(connectionGeneration));
            lock (sync)
            {
                if (generationActive)
                {
                    if (connectionGeneration > overlayGeneration) ActivateConnectionGenerationCore(connectionGeneration);
                }
                else if (connectionGeneration > generationWatermark) ActivateConnectionGenerationCore(connectionGeneration);
            }
        }

        public FeatureId Feature { get { return feature; } }

        internal static bool ValuesEqual(SettingValue left, SettingValue right)
        {
            return left.Kind == right.Kind && left.Boolean == right.Boolean && left.Integer == right.Integer && left.Float.Equals(right.Float) && string.Equals(left.Text ?? string.Empty, right.Text ?? string.Empty, StringComparison.Ordinal);
        }

        private ScopeState LoadScope(SettingRevisionScope scope)
        {
            var loaded = persistence.Load(feature, scope, SchemaVersion, descriptors);
            var values = DefaultsForScope(scope);
            var loadedValuesAreCompatible = true;
            foreach (var pair in loaded.Values)
            {
                SettingDescriptor descriptor;
                if (!descriptorMap.TryGetValue(pair.Key, out descriptor)) loadedValuesAreCompatible = false;
                else if (ScopeAllows(descriptor, scope))
                {
                    if (descriptor.Kind == pair.Value.Kind && ValueValid(descriptor, pair.Value, null)) values[pair.Key] = pair.Value;
                    else loadedValuesAreCompatible = false;
                }
            }
            return new ScopeState(loadedValuesAreCompatible ? loaded.Revision : 0, values);
        }

        private void ActivateConnectionGenerationCore(ulong connectionGeneration)
        {
            if (generationActive && overlayGeneration == connectionGeneration) return;
            overlayGeneration = connectionGeneration;
            generationWatermark = connectionGeneration;
            generationActive = true;
            policies = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal);
            client.Replay.Clear();
            server.Replay.Clear();
        }

        private SettingChangeResult Apply(ScopeState state, ScopedSettingChangeRequest request)
        {
            var mutations = request.Mutations == null ? new SettingMutation[0] : request.Mutations.ToArray();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var candidate = new Dictionary<string, SettingValue>(state.Values, StringComparer.Ordinal);
            foreach (var mutation in mutations)
            {
                SettingDescriptor descriptor;
                if (string.IsNullOrWhiteSpace(mutation.SettingId) || !seen.Add(mutation.SettingId)) return Reject(FrameworkErrorCode.SettingRejected, state);
                if (!descriptorMap.TryGetValue(mutation.SettingId, out descriptor)) return Reject(FrameworkErrorCode.SettingUnknown, state);
                if (!ScopeAllows(descriptor, request.RevisionScope)) return Reject(FrameworkErrorCode.UnauthorizedSender, state);
                if (mutation.Value.Kind != descriptor.Kind) return Reject(FrameworkErrorCode.SettingTypeMismatch, state);
                SettingPolicyView policy;
                if (!ValueValid(descriptor, mutation.Value, policies.TryGetValue(mutation.SettingId, out policy) ? (SettingPolicyView?)policy : null)) return Reject(FrameworkErrorCode.SettingValidationFailed, state);
                candidate[mutation.SettingId] = mutation.Value;
            }
            var changed = candidate.Any(pair => !ValuesEqual(pair.Value, state.Values[pair.Key]));
            if (!changed) return new SettingChangeResult(true, FrameworkErrorCode.None, state.Revision, BuildSnapshot(request.RevisionScope));
            var nextRevision = checked(state.Revision + 1);
            string diagnostic;
            if (!persistence.TryCommit(feature, request.RevisionScope, SchemaVersion, nextRevision, candidate, out diagnostic)) { LastPersistenceDiagnosticId = diagnostic; return Reject(FrameworkErrorCode.SettingPersistenceFailed, state); }
            state.Values = candidate;
            state.Revision = nextRevision;
            return new SettingChangeResult(true, FrameworkErrorCode.None, nextRevision, BuildSnapshot(request.RevisionScope));
        }

        private SettingChangeResult Remember(ScopeState state, ulong requestId, string fingerprint, SettingChangeResult result)
        {
            if (requestId == 0) return result;
            state.Replay[requestId] = new ReplayRecord(fingerprint, result);
            return result;
        }

        private SettingChangeResult Reject(FrameworkErrorCode error, ScopeState state) { var scope = ReferenceEquals(state, client) ? SettingRevisionScope.ClientPreference : SettingRevisionScope.ServerAuthority; return new SettingChangeResult(false, error, state.Revision, BuildSnapshot(scope)); }
        private FeatureSettingsSnapshot BuildSnapshot(SettingRevisionScope scope)
        {
            var state = scope == SettingRevisionScope.ClientPreference ? client : server;
            var entries = descriptors.Select(descriptor => BuildEntry(descriptor, state.Values[descriptor.SettingId])).ToArray();
            var source = policies.Count == 0 ? SettingSnapshotSource.LocalPersistent : SettingSnapshotSource.SessionProjection;
            return new FeatureSettingsSnapshot(feature, SchemaVersion, scope, state.Revision, SettingSyncState.Ready, source, new ReadOnlyCollection<SettingEntryView>(entries));
        }
        private SettingEntryView BuildEntry(SettingDescriptor descriptor, SettingValue preference)
        {
            var policy = default(SettingPolicyView);
            var hasPolicy = descriptor.Authority == SettingAuthority.ServerPolicyWithClientPreference && policies.TryGetValue(descriptor.SettingId, out policy);
            var effective = preference;
            if (hasPolicy && !ValueValid(descriptor, preference, policy)) effective = descriptor.DefaultValue;
            var hasPreference = descriptor.Authority != SettingAuthority.ServerAuthoritative;
            return new SettingEntryView(descriptor.SettingId, descriptor.Authority, new SettingValueOption(hasPreference, preference), hasPolicy, hasPolicy ? policy : default(SettingPolicyView), effective, true, descriptor.Authority != SettingAuthority.ServerAuthoritative || hasPolicy == false || ValueValid(descriptor, preference, policy));
        }
        private Dictionary<string, SettingValue> DefaultsForScope(SettingRevisionScope scope) { return descriptors.ToDictionary(x => x.SettingId, x => x.DefaultValue, StringComparer.Ordinal); }
        private uint SchemaVersion { get { return descriptors.Max(x => x.SchemaVersion); } }
        private static bool ScopeAllows(SettingDescriptor descriptor, SettingRevisionScope scope) { return descriptor.Authority == SettingAuthority.ClientLocal ? scope == SettingRevisionScope.ClientPreference : descriptor.Authority == SettingAuthority.ServerAuthoritative ? scope == SettingRevisionScope.ServerAuthority : scope == SettingRevisionScope.ClientPreference; }
        private static bool PolicyCompatible(SettingDescriptor descriptor, SettingPolicyView policy)
        {
            if (descriptor.Kind == SettingKind.Choice)
            {
                return !policy.Minimum.HasValue && !policy.Maximum.HasValue && !policy.Step.HasValue && policy.AllowedValues != null && policy.AllowedValues.Count > 0 && policy.AllowedValues.All(x => x.Kind == descriptor.Kind) && policy.AllowedValues.Distinct(new SettingValueComparer()).Count() == policy.AllowedValues.Count && policy.AllowedValues.All(value => descriptor.AllowedValues.Any(allowed => ValuesEqual(allowed, value)));
            }
            if (descriptor.Kind != SettingKind.Integer && descriptor.Kind != SettingKind.Float && descriptor.Kind != SettingKind.Text && descriptor.Kind != SettingKind.KeyBinding && descriptor.Kind != SettingKind.Toggle) return false;
            if (policy.Minimum.HasValue && policy.Minimum.Value.Kind != descriptor.Kind) return false;
            if (policy.Maximum.HasValue && policy.Maximum.Value.Kind != descriptor.Kind) return false;
            if (policy.Step.HasValue && (policy.Step.Value.Kind != descriptor.Kind || (descriptor.Kind == SettingKind.Integer && policy.Step.Value.Integer <= 0) || (descriptor.Kind == SettingKind.Float && (policy.Step.Value.Float <= 0f || float.IsNaN(policy.Step.Value.Float) || float.IsInfinity(policy.Step.Value.Float))))) return false;
            if (policy.Minimum.HasValue && descriptor.Minimum.HasValue && (descriptor.Kind == SettingKind.Integer ? policy.Minimum.Value.Integer < descriptor.Minimum.Value.Integer : policy.Minimum.Value.Float < descriptor.Minimum.Value.Float)) return false;
            if (policy.Maximum.HasValue && descriptor.Maximum.HasValue && (descriptor.Kind == SettingKind.Integer ? policy.Maximum.Value.Integer > descriptor.Maximum.Value.Integer : policy.Maximum.Value.Float > descriptor.Maximum.Value.Float)) return false;
            if (policy.Step.HasValue && descriptor.Step.HasValue)
            {
                if (descriptor.Kind == SettingKind.Integer && policy.Step.Value.Integer % descriptor.Step.Value.Integer != 0) return false;
                if (descriptor.Kind == SettingKind.Float && Math.Abs((policy.Step.Value.Float / descriptor.Step.Value.Float) - (float)Math.Round(policy.Step.Value.Float / descriptor.Step.Value.Float)) > 0.0001f) return false;
            }
            if (policy.Minimum.HasValue && policy.Maximum.HasValue && (descriptor.Kind == SettingKind.Integer ? policy.Minimum.Value.Integer > policy.Maximum.Value.Integer : descriptor.Kind == SettingKind.Float && (float.IsNaN(policy.Minimum.Value.Float) || float.IsInfinity(policy.Minimum.Value.Float) || float.IsNaN(policy.Maximum.Value.Float) || float.IsInfinity(policy.Maximum.Value.Float) || policy.Minimum.Value.Float > policy.Maximum.Value.Float))) return false;
            return true;
        }
        private static bool ValueValid(SettingDescriptor descriptor, SettingValue value, SettingPolicyView? policy)
        {
            if (value.Kind != descriptor.Kind) return false;
            if (descriptor.Kind == SettingKind.Integer || descriptor.Kind == SettingKind.Float)
            {
                if (descriptor.Kind == SettingKind.Float && (float.IsNaN(value.Float) || float.IsInfinity(value.Float))) return false;
                var min = policy.HasValue && policy.Value.Minimum.HasValue ? policy.Value.Minimum : descriptor.Minimum;
                var max = policy.HasValue && policy.Value.Maximum.HasValue ? policy.Value.Maximum : descriptor.Maximum;
                if (min.HasValue && (descriptor.Kind == SettingKind.Integer ? value.Integer < min.Value.Integer : value.Float < min.Value.Float)) return false;
                if (max.HasValue && (descriptor.Kind == SettingKind.Integer ? value.Integer > max.Value.Integer : value.Float > max.Value.Float)) return false;
                var step = policy.HasValue && policy.Value.Step.HasValue ? policy.Value.Step : descriptor.Step;
                if (step.HasValue && descriptor.Kind == SettingKind.Integer && min.HasValue && (value.Integer - min.Value.Integer) % step.Value.Integer != 0) return false;
                if (step.HasValue && descriptor.Kind == SettingKind.Float && min.HasValue && Math.Abs(((value.Float - min.Value.Float) / step.Value.Float) - (float)Math.Round((value.Float - min.Value.Float) / step.Value.Float)) > 0.0001f) return false;
            }
            if (descriptor.Kind == SettingKind.Text || descriptor.Kind == SettingKind.KeyBinding || descriptor.Kind == SettingKind.Choice)
            {
                var maximumBytes = policy.HasValue && policy.Value.MaximumUtf8Bytes != 0 ? policy.Value.MaximumUtf8Bytes : descriptor.MaximumUtf8Bytes;
                if (Encoding.UTF8.GetByteCount(value.Text ?? string.Empty) > maximumBytes) return false;
                var allowed = policy.HasValue && policy.Value.AllowedValues != null && policy.Value.AllowedValues.Count > 0 ? policy.Value.AllowedValues : descriptor.AllowedValues;
                if (descriptor.Kind == SettingKind.Choice && (allowed == null || !allowed.Any(x => ValuesEqual(x, value)))) return false;
            }
            return true;
        }
        // DEV-V3-06: internal — the settings facet gate (registration +
        // registry) shares this one validation, so a schema accepted at the
        // bridge can never fault the runtime constructor later.
        internal static List<string> ValidateDescriptors(FeatureId feature, IReadOnlyList<SettingDescriptor> list)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            uint schema = 0;
            foreach (var descriptor in list)
            {
                if (!string.Equals(descriptor.Feature.Value, feature.Value, StringComparison.Ordinal)) errors.Add("FeatureMismatch:" + descriptor.SettingId);
                if (string.IsNullOrWhiteSpace(descriptor.SettingId) || !ids.Add(descriptor.SettingId)) errors.Add("DuplicateOrMissingSettingId:" + descriptor.SettingId);
                if (descriptor.SchemaVersion == 0 || descriptor.Kind != descriptor.DefaultValue.Kind) errors.Add("InvalidDescriptor:" + descriptor.SettingId);
                if (schema == 0) schema = descriptor.SchemaVersion; else if (descriptor.SchemaVersion != schema) errors.Add("MixedSchemaVersion:" + descriptor.SettingId);
                if (descriptor.SchemaVersion != 0 && descriptor.Kind == descriptor.DefaultValue.Kind && !ValueValid(descriptor, descriptor.DefaultValue, null)) errors.Add("InvalidDefault:" + descriptor.SettingId);
                if (descriptor.Minimum.HasValue && descriptor.Minimum.Value.Kind != descriptor.Kind) errors.Add("InvalidMinimum:" + descriptor.SettingId);
                if (descriptor.Maximum.HasValue && descriptor.Maximum.Value.Kind != descriptor.Kind) errors.Add("InvalidMaximum:" + descriptor.SettingId);
                if (descriptor.Kind == SettingKind.Float && ((descriptor.Minimum.HasValue && (float.IsNaN(descriptor.Minimum.Value.Float) || float.IsInfinity(descriptor.Minimum.Value.Float))) || (descriptor.Maximum.HasValue && (float.IsNaN(descriptor.Maximum.Value.Float) || float.IsInfinity(descriptor.Maximum.Value.Float))))) errors.Add("InvalidRange:" + descriptor.SettingId);
                if (descriptor.Kind == SettingKind.Choice && (descriptor.AllowedValues == null || descriptor.AllowedValues.Count == 0 || descriptor.AllowedValues.Distinct(new SettingValueComparer()).Count() != descriptor.AllowedValues.Count)) errors.Add("InvalidChoice:" + descriptor.SettingId);
                if ((descriptor.Kind == SettingKind.Toggle || descriptor.Kind == SettingKind.Choice) && (descriptor.Minimum.HasValue || descriptor.Maximum.HasValue || descriptor.Step.HasValue)) errors.Add("InvalidRange:" + descriptor.SettingId);
                if ((descriptor.Kind == SettingKind.Integer || descriptor.Kind == SettingKind.Float) && descriptor.Minimum.HasValue && descriptor.Maximum.HasValue && (descriptor.Kind == SettingKind.Integer ? descriptor.Minimum.Value.Integer > descriptor.Maximum.Value.Integer : descriptor.Minimum.Value.Float > descriptor.Maximum.Value.Float)) errors.Add("InvalidRange:" + descriptor.SettingId);
                if ((descriptor.Kind == SettingKind.Integer || descriptor.Kind == SettingKind.Float) && descriptor.Step.HasValue && descriptor.Step.Value.Kind != descriptor.Kind) errors.Add("InvalidStep:" + descriptor.SettingId);
                if (descriptor.Kind == SettingKind.Integer && descriptor.Step.HasValue && descriptor.Step.Value.Integer <= 0) errors.Add("InvalidStep:" + descriptor.SettingId);
                if (descriptor.Kind == SettingKind.Float && descriptor.Step.HasValue && (descriptor.Step.Value.Float <= 0f || float.IsNaN(descriptor.Step.Value.Float) || float.IsInfinity(descriptor.Step.Value.Float))) errors.Add("InvalidStep:" + descriptor.SettingId);
            }
            return errors;
        }
        private static SettingDescriptor CloneDescriptor(SettingDescriptor descriptor)
        {
            return new SettingDescriptor(descriptor.Feature, descriptor.SettingId, descriptor.DisplayNameKey, descriptor.DescriptionKey, descriptor.Kind, descriptor.Authority, descriptor.DefaultValue, descriptor.Minimum, descriptor.Maximum, descriptor.Step, descriptor.AllowedValues == null ? new ReadOnlyCollection<SettingValue>(new SettingValue[0]) : new ReadOnlyCollection<SettingValue>(descriptor.AllowedValues.ToArray()), descriptor.MaximumUtf8Bytes, descriptor.ValidationRuleId, descriptor.SchemaVersion, descriptor.SortOrder, descriptor.VisibilityRuleId, descriptor.EnablementRuleId);
        }
        private static SettingPolicyView ClonePolicy(SettingPolicyView policy)
        {
            return new SettingPolicyView(policy.Minimum, policy.Maximum, policy.Step, policy.AllowedValues == null ? new ReadOnlyCollection<SettingValue>(new SettingValue[0]) : new ReadOnlyCollection<SettingValue>(policy.AllowedValues.ToArray()), policy.MaximumUtf8Bytes, policy.ValidationRuleId);
        }
        private static bool IsKnownScope(SettingRevisionScope scope) { return scope == SettingRevisionScope.ClientPreference || scope == SettingRevisionScope.ServerAuthority; }
        private static string Fingerprint(ScopedSettingChangeRequest request)
        {
            var text = request.RequestId + "|" + request.RevisionScope + "|" + request.ExpectedRevision + "|" + string.Join(";", (request.Mutations ?? new SettingMutation[0]).Select(x => x.SettingId + "=" + (byte)x.Value.Kind + ":" + x.Value.Boolean + ":" + x.Value.Integer + ":" + x.Value.Float.ToString("R", CultureInfo.InvariantCulture) + ":" + (x.Value.Text ?? string.Empty)).OrderBy(x => x, StringComparer.Ordinal));
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        private sealed class ScopeState
        {
            public ScopeState(uint revision, Dictionary<string, SettingValue> values) { Revision = revision; Values = values; }
            public uint Revision;
            public Dictionary<string, SettingValue> Values;
            public Dictionary<ulong, ReplayRecord> Replay = new Dictionary<ulong, ReplayRecord>();
        }
        private sealed class ReplayRecord { public ReplayRecord(string fingerprint, SettingChangeResult result) { Fingerprint = fingerprint; Result = result; } public string Fingerprint { get; } public SettingChangeResult Result { get; } }
        private sealed class SettingValueComparer : IEqualityComparer<SettingValue> { public bool Equals(SettingValue x, SettingValue y) { return ValuesEqual(x, y); } public int GetHashCode(SettingValue value) { return (int)value.Kind * 397 ^ value.Integer ^ value.Boolean.GetHashCode() ^ value.Float.GetHashCode() ^ (value.Text ?? string.Empty).GetHashCode(); } }
    }

    public sealed class LocalLoopbackSettingsTransport
    {
        private readonly SettingsRuntime runtime;
        public LocalLoopbackSettingsTransport(SettingsRuntime runtime) { this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime)); }
        public SettingChangeResult Send(UpdateModuleConfigCommand command)
        {
            if (!string.Equals(command.Feature.Value, runtime.Feature.Value, StringComparison.Ordinal)) return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, default(FeatureSettingsSnapshot));
            return runtime.Submit(new ScopedSettingChangeRequest(command.RequestId, command.RevisionScope, command.ExpectedRevision, command.Mutations));
        }
    }
}
