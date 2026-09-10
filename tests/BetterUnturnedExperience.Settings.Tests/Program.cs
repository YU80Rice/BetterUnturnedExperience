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

            RunV3ScopedSettingsView();
        }

        // DEV-V3-06: the host-owned settings registry + the scoped view
        // (IFeatureBootstrap.Settings). The view is the ONLY ecosystem-facing
        // shape (GetSnapshot/TryGet/Submit — never the runtime's session
        // overlay mutators), bound to (feature, lifecycle generation): reads
        // are the feature's own persisted truth and always answer; writes ride
        // the generation gate + the authority-side gate, every rejection an
        // explicit result + a structured diagnostic line (BUE-SET code family).
        private static void RunV3ScopedSettingsView()
        {
            var feature = new FeatureId("io.example.v3settings");
            Func<FeatureId, SettingDescriptor[]> descriptorsFor = f => new[]
            {
                Descriptor(f, "volume", SettingKind.Integer, SettingAuthority.ClientLocal, SettingValue.IntegerValue(5), 0, 10),
                Descriptor(f, "quota", SettingKind.Integer, SettingAuthority.ServerAuthoritative, SettingValue.IntegerValue(2), 0, 10),
                Descriptor(f, "theme", SettingKind.Choice, SettingAuthority.ServerPolicyWithClientPreference, SettingValue.Choice("dark"), allowed: new[] { "dark", "light" })
            };
            var descriptors = descriptorsFor(feature);
            var lines = new List<string>();
            var memory = new InMemorySettingsPersistence();
            var registry = new FeatureSettingsRegistry(memory, () => true, lines.Add);

            // ── 单源与创建：同一功能恒一 runtime；二次 Ensure 复用不重建。 ──
            var runtimeA = registry.GetOrCreateRuntime(feature, descriptors);
            Assert(runtimeA != null, "v3: registry composes the feature's runtime from the facet schema");
            Assert(ReferenceEquals(runtimeA, registry.GetOrCreateRuntime(feature, descriptors)),
                "v3: the feature has ONE host-owned runtime (re-ensure reuses it — no second source of truth)");
            var createdLines = lines.Count(l => l.Contains("diagnosticId=BUE-SET-CREATED"));
            Assert(createdLines == 1, "v3: exactly one created line per feature runtime (observable single source)");

            registry.OpenGeneration(feature, 1UL);
            var view = registry.CreateView(feature, 1UL);
            Assert(view != null, "v3: the scoped view composes for a live generation");
            Assert(view.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 0
                && view.GetSnapshot(SettingRevisionScope.ClientPreference).Entries.Count == 3,
                "v3: view snapshot = the feature's own scope (complete, revision 0 at fresh store)");
            SettingValue volumeValue;
            uint volumeRevision;
            Assert(view.TryGet("volume", out volumeValue, out volumeRevision) && volumeValue.Integer == 5 && volumeRevision == 0,
                "v3: view TryGet reads defaults with the scope revision");

            // ── 提交成功链：合法写 → revision 单调推进，视图可见新值。 ──
            var commit = view.Submit(new ScopedSettingChangeRequest(1, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("volume", SettingValue.IntegerValue(7)) }));
            Assert(commit.Accepted && commit.Revision == 1, "v3: legal commit advances the revision monotonically");
            Assert(view.TryGet("volume", out volumeValue, out volumeRevision) && volumeValue.Integer == 7 && volumeRevision == 1,
                "v3: the same view observes the new value and revision (single truth)");

            // ── 校验失败/ExpectedRevision 过期：显式拒 + revision 不推进。 ──
            var invalid = view.Submit(new ScopedSettingChangeRequest(2, SettingRevisionScope.ClientPreference, 1,
                new[] { new SettingMutation("volume", SettingValue.IntegerValue(99)) }));
            Assert(!invalid.Accepted && invalid.Error == FrameworkErrorCode.SettingValidationFailed && invalid.Revision == 1,
                "v3: out-of-range commit is explicitly rejected without advancing the revision");
            var stale = view.Submit(new ScopedSettingChangeRequest(3, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("volume", SettingValue.IntegerValue(2)) }));
            Assert(!stale.Accepted && stale.Error == FrameworkErrorCode.SettingRevisionConflict && stale.Revision == 1
                && stale.Snapshot.Revision == 1,
                "v3: ExpectedRevision below the live revision = conflict + full current snapshot (stale UI can never overwrite)");

            // ── 损坏安全默认：视图层如实落到默认值（revision 0）。 ──
            var corruptFeature = new FeatureId("io.example.v3corrupt");
            memory.CorruptNextLoad = true;
            var corruptRuntime = registry.GetOrCreateRuntime(corruptFeature, descriptorsFor(corruptFeature));
            var corruptView = registry.CreateView(corruptFeature, 1UL);
            Assert(corruptRuntime != null && corruptView.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 0
                && corruptView.TryGet("volume", out volumeValue, out volumeRevision) && volumeValue.Integer == 5,
                "v3: a corrupt store falls back to safe defaults through the view (readable, revision 0)");

            // ── 作用域隔离：视图只见自己功能的作用域。 ──
            var other = new FeatureId("io.example.v3other");
            var otherDescriptors = new[] { Descriptor(other, "volume", SettingKind.Integer, SettingAuthority.ClientLocal, SettingValue.IntegerValue(9), 0, 10) };
            registry.GetOrCreateRuntime(other, otherDescriptors);
            registry.OpenGeneration(other, 1UL);
            var otherView = registry.CreateView(other, 1UL);
            SettingValue crossValue;
            uint crossRevision;
            Assert(otherView.GetSnapshot(SettingRevisionScope.ClientPreference).Entries.Count == 1
                && !otherView.TryGet("quota", out crossValue, out crossRevision),
                "v3: a view cannot read another feature's scope (presence or presence-keys)");
            var crossCommit = otherView.Submit(new ScopedSettingChangeRequest(4, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("theme", SettingValue.Choice("light")) }));
            Assert(!crossCommit.Accepted && crossCommit.Error == FrameworkErrorCode.SettingUnknown
                && view.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 1,
                "v3: a view cannot commit a foreign setting id (explicit unknown, the owner's revision untouched)");

            // ── 代际门：换代/停止后旧视图写拒读可;新代际续用同一事实。 ──
            registry.OpenGeneration(feature, 2UL);
            var view2 = registry.CreateView(feature, 2UL);
            var staleGen = view.Submit(new ScopedSettingChangeRequest(5, SettingRevisionScope.ClientPreference, 1,
                new[] { new SettingMutation("volume", SettingValue.IntegerValue(4)) }));
            Assert(!staleGen.Accepted && staleGen.Error == FrameworkErrorCode.SettingRejected
                && lines.Exists(l => l.Contains("diagnosticId=BUE-SET-001") && l.Contains("io.example.v3settings") && l.Contains("generation=1")),
                "v3: writes from a superseded generation = explicit rejection + BUE-SET-001 line");
            Assert(view.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 1,
                "v3: reads on a stale view still answer (the feature's own persisted truth, no mutation surface)");
            var gen2Commit = view2.Submit(new ScopedSettingChangeRequest(6, SettingRevisionScope.ClientPreference, 1,
                new[] { new SettingMutation("volume", SettingValue.IntegerValue(4)) }));
            Assert(gen2Commit.Accepted && gen2Commit.Revision == 2,
                "v3: the live generation continues the SAME revision counter (single truth across generations)");
            registry.InvalidateOwner(feature, "user-disabled");
            var stopped = view2.Submit(new ScopedSettingChangeRequest(7, SettingRevisionScope.ClientPreference, 2,
                new[] { new SettingMutation("volume", SettingValue.IntegerValue(6)) }));
            Assert(!stopped.Accepted && lines.Exists(l => l.Contains("diagnosticId=BUE-SET-001") && l.Contains("reason=owner-invalidated")),
                "v3: the stop boundary refuses later writes (reads stay honest)");
            registry.OpenGeneration(feature, 3UL);
            var view3 = registry.CreateView(feature, 3UL);
            Assert(view3.GetSnapshot(SettingRevisionScope.ClientPreference).Revision == 2
                && view3.TryGet("volume", out volumeValue, out volumeRevision) && volumeValue.Integer == 4,
                "v3: a re-enabled generation sees the pre-stop truth (monotonic, never resurrected state)");

            // ── 权威侧门：ServerAuthority 写只在权威端；两环境(provider=true)同语义。 ──
            var clientRegistry = new FeatureSettingsRegistry(new InMemorySettingsPersistence(), () => false, lines.Add);
            var clientFeature = new FeatureId("io.example.v3client");
            clientRegistry.GetOrCreateRuntime(clientFeature, descriptorsFor(clientFeature));
            clientRegistry.OpenGeneration(clientFeature, 1UL);
            var clientView = clientRegistry.CreateView(clientFeature, 1UL);
            var forged = clientView.Submit(new ScopedSettingChangeRequest(8, SettingRevisionScope.ServerAuthority, 0,
                new[] { new SettingMutation("quota", SettingValue.IntegerValue(9)) }));
            Assert(!forged.Accepted && forged.Error == FrameworkErrorCode.UnauthorizedSender
                && lines.Exists(l => l.Contains("diagnosticId=BUE-SET-002") && l.Contains("io.example.v3client")),
                "v3: a non-authority side cannot forge ServerAuthority commits (BUE-SET-002 line, runtime untouched)");
            Assert(clientView.Submit(new ScopedSettingChangeRequest(9, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("volume", SettingValue.IntegerValue(6)) })).Accepted,
                "v3: the client's own ClientPreference scope is unaffected by the authority gate");
            var u3ds = new FeatureSettingsRegistry(new InMemorySettingsPersistence(), () => true, lines.Add);
            var p2p = new FeatureSettingsRegistry(new InMemorySettingsPersistence(), () => true, lines.Add);
            var u3dsFeature = new FeatureId("io.example.v3authority");
            var p2pFeature = new FeatureId("io.example.v3authority");
            u3ds.GetOrCreateRuntime(u3dsFeature, descriptorsFor(u3dsFeature));
            p2p.GetOrCreateRuntime(p2pFeature, descriptorsFor(p2pFeature));
            u3ds.OpenGeneration(u3dsFeature, 1UL);
            p2p.OpenGeneration(p2pFeature, 1UL);
            var u3dsCommit = u3ds.CreateView(u3dsFeature, 1UL).Submit(new ScopedSettingChangeRequest(10, SettingRevisionScope.ServerAuthority, 0,
                new[] { new SettingMutation("quota", SettingValue.IntegerValue(9)) }));
            var p2pCommit = p2p.CreateView(p2pFeature, 1UL).Submit(new ScopedSettingChangeRequest(10, SettingRevisionScope.ServerAuthority, 0,
                new[] { new SettingMutation("quota", SettingValue.IntegerValue(9)) }));
            Assert(u3dsCommit.Accepted && p2pCommit.Accepted && u3dsCommit.Revision == p2pCommit.Revision
                && u3dsCommit.Snapshot.Revision == p2pCommit.Snapshot.Revision,
                "v3: the two authority environments (U3DS / P2P listen host) share ONE code path — same semantics by construction");

            // ── 会话覆盖断线清除：policy 投影经视图可见，永不污染持久 revision。 ──
            Assert(!(view is SettingsRuntime) && !(view is ISettingsPersistence),
                "v3: the injected view is NOT the runtime (class body stays out of contract; session-overlay mutators are unreachable from the module side)");
            var overlayFeature = new FeatureId("io.example.v3overlay");
            var overlayRuntime = registry.GetOrCreateRuntime(overlayFeature, descriptorsFor(overlayFeature));
            registry.OpenGeneration(overlayFeature, 1UL);
            var overlayView = registry.CreateView(overlayFeature, 1UL);
            var beforePolicy = overlayView.Submit(new ScopedSettingChangeRequest(11, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation("theme", SettingValue.Choice("light")) }));
            Assert(beforePolicy.Accepted && beforePolicy.Revision == 1, "v3: preference committed before the session overlay");
            overlayRuntime.ActivateConnectionGeneration(70UL);
            var themePolicy = new Dictionary<string, SettingPolicyView>(StringComparer.Ordinal)
            {
                { "theme", new SettingPolicyView(default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                    new[] { SettingValue.Choice("dark") }, 0, string.Empty) }
            };
            Assert(overlayRuntime.ApplyServerPolicy(70UL, themePolicy), "v3: host applies a session policy at the connection generation");
            var projected = overlayView.GetSnapshot(SettingRevisionScope.ClientPreference);
            var projectedTheme = Find(projected, "theme");
            Assert(projected.Source == SettingSnapshotSource.SessionProjection && projectedTheme.HasPolicy
                && projectedTheme.EffectiveValue.Text == "dark" && projectedTheme.ClientPreference.Value.Text == "light"
                && projected.Revision == 1,
                "v3: the overlay is a projection the view reads honestly — preference preserved, revision NOT polluted");
            overlayRuntime.ClearSessionOverlay(70UL);
            var restored = overlayView.GetSnapshot(SettingRevisionScope.ClientPreference);
            Assert(restored.Source == SettingSnapshotSource.LocalPersistent && Find(restored, "theme").EffectiveValue.Text == "light"
                && restored.Revision == 1,
                "v3: disconnect clears the overlay, the local preference returns, the persisted revision is exactly as before");
            Assert(overlayView.Submit(new ScopedSettingChangeRequest(12, SettingRevisionScope.ClientPreference, 1,
                new[] { new SettingMutation("volume", SettingValue.IntegerValue(3)) })).Accepted,
                "v3: after the clear the scope keeps accepting writes on the unbroken revision counter");

            // ── schema 冲突：同功能第二 schema = KeepExisting + 显式诊断行。 ──
            var conflicting = new[] { Descriptor(feature, "volume", SettingKind.Integer, SettingAuthority.ClientLocal, SettingValue.IntegerValue(5), 0, 10) };
            var kept = registry.GetOrCreateRuntime(feature, conflicting);
            Assert(ReferenceEquals(kept, runtimeA)
                && lines.Exists(l => l.Contains("diagnosticId=BUE-SET-003") && l.Contains("io.example.v3settings")),
                "v3: a second schema for one feature never replaces the live runtime (no second source of truth) and is surfaced");
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
