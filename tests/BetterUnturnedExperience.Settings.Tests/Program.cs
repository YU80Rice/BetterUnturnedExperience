using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Settings;

namespace BetterUnturnedExperience.Settings.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                Run();
                Console.WriteLine("DEV-03 settings runtime tests: PASS");
                return 0;
            }
            catch (Exception error)
            {
                Console.WriteLine("DEV-03 settings runtime tests: FAIL");
                Console.WriteLine(error.GetType().FullName);
                Console.WriteLine(error.Message);
                return 1;
            }
        }

        private static void Run()
        {
            var feature = new FeatureId("io.example.settings");
            var descriptors = new[]
            {
                Descriptor(feature, "enabled", SettingKind.Toggle, SettingAuthority.ClientLocal, SettingValue.Toggle(false)),
                Descriptor(feature, "limit", SettingKind.Integer, SettingAuthority.ServerAuthoritative, SettingValue.IntegerValue(3), 0, 10),
                Descriptor(feature, "theme", SettingKind.Choice, SettingAuthority.ServerPolicyWithClientPreference, SettingValue.Choice("dark"), allowed: new[] { "dark", "light" })
            };

            var memory = new InMemorySettingsPersistence();
            var runtime = new SettingsRuntime(feature, descriptors, memory);
            var local = runtime.GetSnapshot(SettingRevisionScope.ClientPreference);
            Assert(local.Revision == 0 && local.Entries.Count == 3, "initial complete snapshot");
            Assert(runtime.TryGet("enabled", out var enabled, out var revision) && !enabled.Boolean && revision == 0, "default value is readable");

            var first = runtime.Submit(new ScopedSettingChangeRequest(1, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(true)), new SettingMutation("theme", SettingValue.Choice("light")) }));
            Assert(first.Accepted && first.Revision == 1 && first.Snapshot.Revision == 1, "multi-field commit is atomic and increments once");
            var replay = runtime.Submit(new ScopedSettingChangeRequest(1, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(true)), new SettingMutation("theme", SettingValue.Choice("light")) }));
            Assert(replay.Accepted && replay.Revision == 1, "same RequestId replay is idempotent");
            var conflict = runtime.Submit(new ScopedSettingChangeRequest(1, SettingRevisionScope.ClientPreference, 1,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(false)) }));
            Assert(!conflict.Accepted && conflict.Error == FrameworkErrorCode.RequestIdConflict, "RequestId payload conflict is rejected");
            var replayAfterConflict = runtime.Submit(new ScopedSettingChangeRequest(1, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(true)), new SettingMutation("theme", SettingValue.Choice("light")) }));
            Assert(replayAfterConflict.Accepted && replayAfterConflict.Revision == 1, "original RequestId replay remains intact after conflict");

            var stale = runtime.Submit(new ScopedSettingChangeRequest(2, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(false)) }));
            Assert(!stale.Accepted && stale.Error == FrameworkErrorCode.SettingRevisionConflict && stale.Snapshot.Revision == 1, "stale revision returns full current snapshot");
            var unknown = runtime.Submit(new ScopedSettingChangeRequest(3, SettingRevisionScope.ClientPreference, 1,
                new[] { new SettingMutation("unknown", SettingValue.Toggle(true)) }));
            Assert(!unknown.Accepted && unknown.Error == FrameworkErrorCode.SettingUnknown && unknown.Snapshot.Revision == 1, "unknown setting does not partially commit");

            var noOp = runtime.Submit(new ScopedSettingChangeRequest(9, SettingRevisionScope.ClientPreference, 1,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(true)) }));
            Assert(noOp.Accepted && noOp.Revision == 1, "no-op commit is accepted without advancing revision");

            var server = runtime.Submit(new ScopedSettingChangeRequest(4, SettingRevisionScope.ServerAuthority, 0,
                new[] { new SettingMutation("limit", SettingValue.IntegerValue(8)) }));
            Assert(server.Accepted && server.Revision == 1 && runtime.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 1, "scope revisions are independent");

            runtime.ActivateConnectionGeneration(43);
            var generationWrite = runtime.Submit(new ScopedSettingChangeRequest(8, SettingRevisionScope.ClientPreference, 1,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(false)) }));
            Assert(generationWrite.Accepted && generationWrite.Revision == 2, "generation-scoped write succeeds");
            runtime.ActivateConnectionGeneration(44);
            var replayAfterGeneration = runtime.Submit(new ScopedSettingChangeRequest(8, SettingRevisionScope.ClientPreference, 2,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(true)) }));
            Assert(replayAfterGeneration.Accepted && replayAfterGeneration.Revision == 3, "connection generation clears old RequestId replay state");

            var policy = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal)
            {
                { "theme", new SettingPolicyView(new SettingValueOption(false, default(SettingValue)), new SettingValueOption(false, default(SettingValue)), new SettingValueOption(false, default(SettingValue)), new[] { SettingValue.Choice("dark") }, 0, string.Empty) }
            };
            Assert(runtime.ApplyServerPolicy(45, policy), "valid policy applies");
            var policySnapshot = runtime.GetSnapshot(SettingRevisionScope.ClientPreference);
            var theme = Find(policySnapshot, "theme");
            Assert(theme.HasPolicy && theme.EffectiveValue.Text == "dark" && theme.ClientPreference.Value.Text == "light", "policy is a session overlay and preserves preference");
            var policyValues = theme.Policy.AllowedValues as IList<SettingValue>;
            Assert(policyValues != null, "policy values expose list seam for immutability test");
            try { policyValues.Add(SettingValue.Choice("mutated")); throw new InvalidOperationException("snapshot policy list is mutable"); } catch (NotSupportedException) { }
            var invalidPolicy = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal)
            {
                { "theme", new SettingPolicyView(new SettingValueOption(false, default(SettingValue)), new SettingValueOption(false, default(SettingValue)), new SettingValueOption(false, default(SettingValue)), new[] { SettingValue.Toggle(true) }, 0, string.Empty) }
            };
            Assert(!runtime.ApplyServerPolicy(45, invalidPolicy) && Find(runtime.GetSnapshot(SettingRevisionScope.ClientPreference), "theme").EffectiveValue.Text == "dark", "invalid policy is rejected atomically");
            var wideningPolicy = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal)
            {
                { "limit", new SettingPolicyView(new SettingValueOption(true, SettingValue.IntegerValue(-1)), new SettingValueOption(true, SettingValue.IntegerValue(20)), new SettingValueOption(false, default(SettingValue)), new SettingValue[0], 0, string.Empty) }
            };
            Assert(!runtime.ApplyServerPolicy(45, wideningPolicy), "policy cannot widen static numeric bounds");
            var choiceWideningPolicy = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal)
            {
                { "theme", new SettingPolicyView(new SettingValueOption(false, default(SettingValue)), new SettingValueOption(false, default(SettingValue)), new SettingValueOption(true, SettingValue.IntegerValue(1)), new[] { SettingValue.Choice("blue") }, 0, string.Empty) }
            };
            Assert(!runtime.ApplyServerPolicy(45, choiceWideningPolicy), "policy cannot widen static choice schema");
            Assert(!runtime.ApplyServerPolicy(44, policy) && Find(runtime.GetSnapshot(SettingRevisionScope.ClientPreference), "theme").EffectiveValue.Text == "dark", "older policy generation is rejected");
            runtime.ClearSessionOverlay(45);
            Assert(Find(runtime.GetSnapshot(SettingRevisionScope.ClientPreference), "theme").EffectiveValue.Text == "light", "generation clear removes overlay");
            Assert(!runtime.ApplyServerPolicy(45, policy), "cleared generation cannot be replayed");

            memory.FailNextCommit = true;
            var failedPersist = runtime.Submit(new ScopedSettingChangeRequest(5, SettingRevisionScope.ClientPreference, 3,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(false)) }));
            Assert(!failedPersist.Accepted && failedPersist.Error == FrameworkErrorCode.SettingPersistenceFailed && failedPersist.Snapshot.Revision == 3, "persistence failure keeps old snapshot");

            var root = Path.Combine(Path.GetTempPath(), "BUE-DEV03-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var file = new FileSettingsPersistence(root);
                var fileRuntime = new SettingsRuntime(feature, descriptors, file);
                var committed = fileRuntime.Submit(new ScopedSettingChangeRequest(6, SettingRevisionScope.ClientPreference, 0,
                    new[] { new SettingMutation("enabled", SettingValue.Toggle(true)) }));
                Assert(committed.Accepted, "file persistence commit succeeds: " + committed.Error + ":" + fileRuntime.LastPersistenceDiagnosticId);
                var reloaded = new SettingsRuntime(feature, descriptors, file);
                Assert(reloaded.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 1, "file persistence reloads committed revision");
                var path = file.GetPath(feature, SettingRevisionScope.ClientPreference);
                var oldBytes = File.ReadAllBytes(path);
                file.FailNextReplace = true;
                var replaceFailed = fileRuntime.Submit(new ScopedSettingChangeRequest(12, SettingRevisionScope.ClientPreference, 1,
                    new[] { new SettingMutation("enabled", SettingValue.Toggle(false)) }));
                Assert(!replaceFailed.Accepted && replaceFailed.Error == FrameworkErrorCode.SettingPersistenceFailed, "injected replace failure rejects commit");
                Assert(oldBytes.SequenceEqual(File.ReadAllBytes(path)), "replace failure preserves previous target file");
                Assert(fileRuntime.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 1, "replace failure preserves previous snapshot");
                RewriteSchema(path, 99);
                var futureSchema = new SettingsRuntime(feature, descriptors, file);
                Assert(futureSchema.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 0, "future schema is quarantined and falls back");
                Assert(Directory.GetFiles(root, "*.corrupt.*.bak").Length == 1, "future schema leaves quarantine evidence");
                File.WriteAllText(file.GetPath(feature, SettingRevisionScope.ClientPreference), "corrupted");
                var recovered = new SettingsRuntime(feature, descriptors, file);
                Assert(recovered.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 0, "corrupt persistence falls back to safe defaults");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }

            var loopback = new LocalLoopbackSettingsTransport(runtime);
            var loopResult = loopback.Send(new UpdateModuleConfigCommand(7, feature, SettingRevisionScope.ClientPreference, 3,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(false)) }));
            Assert(loopResult.Accepted && loopResult.Revision == 4, "LocalLoopback forwards process-local command");

            var capacityDescriptor = Descriptor(feature, "capacity", SettingKind.Integer, SettingAuthority.ClientLocal, SettingValue.IntegerValue(0), 0, 200);
            var capacityRuntime = new SettingsRuntime(feature, new[] { capacityDescriptor }, new InMemorySettingsPersistence());
            for (ulong id = 100; id < 228; id++)
            {
                var capacityResult = capacityRuntime.Submit(new ScopedSettingChangeRequest(id, SettingRevisionScope.ClientPreference, (uint)(id - 100),
                    new[] { new SettingMutation("capacity", SettingValue.IntegerValue((int)(id - 99))) }));
                Assert(capacityResult.Accepted, "replay window accepts bounded request " + id);
            }
            var fullReplay = capacityRuntime.Submit(new ScopedSettingChangeRequest(228, SettingRevisionScope.ClientPreference, 128,
                new[] { new SettingMutation("enabled", SettingValue.Toggle(true)) }));
            Assert(!fullReplay.Accepted && fullReplay.Error == FrameworkErrorCode.RateLimited, "full replay window rejects new request without eviction");

            try
            {
                new SettingsRuntime(feature, new[] { Descriptor(feature, "bad", SettingKind.Integer, SettingAuthority.ClientLocal, SettingValue.IntegerValue(99), 0, 10) }, new InMemorySettingsPersistence());
                throw new InvalidOperationException("invalid default descriptor was accepted");
            }
            catch (ArgumentException) { }

            var allowedInput = new[] { SettingValue.Choice("dark"), SettingValue.Choice("light") };
            var isolatedDescriptor = new SettingDescriptor(feature, "isolated", "isolated", "isolated", SettingKind.Choice, SettingAuthority.ClientLocal, SettingValue.Choice("dark"), new SettingValueOption(false, default(SettingValue)), new SettingValueOption(false, default(SettingValue)), new SettingValueOption(false, default(SettingValue)), allowedInput, 32, string.Empty, 1, 0, string.Empty, string.Empty);
            var isolatedRuntime = new SettingsRuntime(feature, new[] { isolatedDescriptor }, new InMemorySettingsPersistence());
            allowedInput[0] = SettingValue.Choice("mutated");
            var isolatedMutation = isolatedRuntime.Submit(new ScopedSettingChangeRequest(10, SettingRevisionScope.ClientPreference, 0, new[] { new SettingMutation("isolated", SettingValue.Choice("mutated")) }));
            Assert(!isolatedMutation.Accepted && isolatedMutation.Error == FrameworkErrorCode.SettingValidationFailed, "descriptor nested values are isolated");

            var unknownScope = runtime.Submit(new ScopedSettingChangeRequest(11, (SettingRevisionScope)99, 0, new SettingMutation[0]));
            Assert(!unknownScope.Accepted && unknownScope.Error == FrameworkErrorCode.SettingRejected, "unknown revision scope fails closed");
        }

        private static void RewriteSchema(string path, uint schema)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8).ToList();
            lines[3] = "schema=" + schema;
            var body = string.Join("\n", lines.Take(lines.Count - 1));
            using (var sha = SHA256.Create()) lines[lines.Count - 1] = "digest=" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", string.Empty);
            File.WriteAllLines(path, lines, new UTF8Encoding(false));
        }

        private static SettingEntryView Find(FeatureSettingsSnapshot snapshot, string id)
        {
            foreach (var entry in snapshot.Entries) if (entry.SettingId == id) return entry;
            throw new InvalidOperationException("missing entry: " + id);
        }

        private static SettingDescriptor Descriptor(FeatureId feature, string id, SettingKind kind, SettingAuthority authority, SettingValue value, int min = 0, int max = 0, string[] allowed = null)
        {
            var hasRange = kind == SettingKind.Integer;
            var optionMin = new SettingValueOption(hasRange, SettingValue.IntegerValue(min));
            var optionMax = new SettingValueOption(hasRange, SettingValue.IntegerValue(max));
            return new SettingDescriptor(feature, id, id, id, kind, authority, value, optionMin, optionMax,
                new SettingValueOption(false, default(SettingValue)), allowed == null ? new SettingValue[0] : Array.ConvertAll(allowed, SettingValue.Choice), 256, string.Empty, 1, 0, string.Empty, string.Empty);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
