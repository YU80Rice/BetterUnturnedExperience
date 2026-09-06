using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Definitions;
using BetterUnturnedExperience.Core.Registration;

namespace BetterUnturnedExperience.Contracts.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                return Run();
            }
            catch (Exception error)
            {
                Console.WriteLine("DEV-10 registration runtime tests: FAIL");
                Console.WriteLine(error.GetType().FullName);
                Console.WriteLine(error.Message);
                return 1;
            }
        }

        private static int Run()
        {
            Assert((byte)FeatureState.Running == 4, "FeatureState values are frozen");
            Assert((byte)PlacementPreviewState.Candidate == 1, "PlacementPreviewState values are frozen");
            Assert((ushort)FrameworkErrorCode.StaleConnectionGeneration == 1007, "FrameworkErrorCode values are frozen");
            Assert(typeof(HandshakeReject).IsValueType, "handshake reject DTO exists");
            Assert(typeof(SnapshotChunkEnvelope).IsValueType, "snapshot chunk DTO exists");
            Assert((byte)SnapshotKind.SettingsSnapshot == 2, "snapshot kind values are frozen");
            var linker = new FeatureDefinitionLinker();
            var empty = linker.Link(new FeatureDefinitionFragment[0]);
            Assert(empty.Status == LinkStatus.LinkFailed, "empty definition set fails atomically");
            Assert(empty.Catalog == null && empty.Diagnostics.Count > 0, "failed link has no catalog and has diagnostics");
            var a = new FeatureId("io.example.alpha");
            var b = new FeatureId("io.example.beta");
            var forward = linker.Link(new[] { Fragment(a, "identity"), Fragment(a, "binding"), Fragment(b, "identity"), Fragment(b, "binding") });
            var reverse = linker.Link(new[] { Fragment(b, "binding"), Fragment(b, "identity"), Fragment(a, "binding"), Fragment(a, "identity") });
            Assert(forward.Status == LinkStatus.LinkSucceeded && reverse.Status == LinkStatus.LinkSucceeded, "complete fragments link");
            Assert(SameDigest(forward.Catalog.DefinitionSetDigest, reverse.Catalog.DefinitionSetDigest), "canonical ordering is deterministic");
            Assert(linker.Link(new[] { Fragment(a, "identity"), Fragment(a, "identity"), Fragment(a, "binding") }).Status == LinkStatus.LinkFailed, "duplicate fragments fail");
            var missingIdentity = linker.Link(new[] { Fragment(a, "binding") });
            var missingBinding = linker.Link(new[] { Fragment(a, "identity") });
            Assert(HasDiagnostic(missingIdentity, "MissingIdentityFragment:" + a.Value), "missing identity is diagnosed");
            Assert(HasDiagnostic(missingBinding, "MissingBindingFragment:" + a.Value), "missing binding is diagnosed");
            Assert(linker.Link(new[] { new FeatureDefinitionFragment(a, "identity", 2, new Digest256(), new FeatureId[0]), Fragment(a, "binding") }).Status == LinkStatus.LinkFailed, "unsupported schema fails");
            var dangling = linker.Link(new[] { Fragment(a, "identity", new FeatureId("io.example.missing")), Fragment(a, "binding") });
            Assert(HasDiagnostic(dangling, "MissingRequiredFeature:io.example.missing"), "dangling required feature is diagnosed");
            var requiredSnapshot = new[] { new FeatureId("io.example.missing") };
            var copiedFragment = new FeatureDefinitionFragment(a, "identity", 1, new Digest256(1, 2, 3, 4), requiredSnapshot);
            requiredSnapshot[0] = a;
            Assert(HasDiagnostic(linker.Link(new[] { copiedFragment, Fragment(a, "binding") }), "MissingRequiredFeature:io.example.missing"), "fragment input is snapshotted");
            var diagnosticLeft = linker.Link(new[] { new FeatureDefinitionFragment(a, "identity", 2, new Digest256(), new FeatureId[0]), Fragment(a, "identity") });
            var diagnosticRight = linker.Link(new[] { Fragment(a, "identity"), new FeatureDefinitionFragment(a, "identity", 2, new Digest256(), new FeatureId[0]) });
            Assert(JoinDiagnostics(diagnosticLeft) == JoinDiagnostics(diagnosticRight), "diagnostics are fully stable across input order");
            Assert(ReadOnlyListRejectsAdd(forward.Catalog.Features), "catalog feature list is immutable");
            Assert(ReadOnlyListRejectsAdd(forward.Catalog.Features[0].FragmentKinds), "fragment kind list is immutable");
            FeatureAdmissionHandle handle;
            Assert(!new FeatureLoadGate(new CompiledIdentityCatalog(new CompiledFeatureRecord[0], new Digest256())).TryAdmit(new FeatureId("missing"), out handle), "unknown feature is not admitted");
            Assert(new FeatureLoadGate(forward.Catalog).TryAdmit(a, out handle) && handle != null, "known feature is admitted with opaque reference handle");
            Assert(object.ReferenceEquals(handle.Catalog, forward.Catalog) && handle.Record.Feature.Value == a.Value, "admission handle binds catalog and record");

            IPlacementCandidateEvaluator evaluator = null;
            IFeatureBootstrap bootstrap = null;
            Assert(typeof(IPlacementCandidateEvaluator).IsInterface, "placement seam is an interface");
            Assert(typeof(IFeatureBootstrap).IsInterface, "bootstrap seam is an interface");
            Assert(evaluator == null && bootstrap == null, "contract seams have no runtime implementation in DEV-01");

            var registration = new StubRegistration("io.example.tracer");
            var runtime = new FeatureRegistrationRuntime();
            FeatureRegistrationResult result;
            result = runtime.Register(registration);
            Assert(result.Reason == FeatureRegistrationReason.HostUnavailable, "host starting rejects registration");
            runtime.OpenRegistration();
            result = runtime.Register(registration);
            Assert(result.Accepted && result.Feature.Value == "io.example.tracer", "registration open accepts explicit registration");
            result = runtime.Register(registration);
            Assert(!result.Accepted && result.Reason == FeatureRegistrationReason.DuplicateFeature, "duplicate feature is rejected");
            Assert(runtime.FreezeCatalog(), "catalog freezes after registration");
            Assert(runtime.Phase == FeatureRegistrationPhase.CatalogFrozen, "catalog frozen phase is observable");
            result = runtime.Register(new StubRegistration("io.example.late"));
            Assert(!result.Accepted && result.Reason == FeatureRegistrationReason.PhaseClosed, "late registration is rejected");
            runtime.MarkRuntimeReady();
            Assert(runtime.Phase == FeatureRegistrationPhase.RuntimeReady, "runtime ready phase is observable");
            Assert(runtime.Catalog != null && runtime.Catalog.Entries.Count == 1, "frozen catalog contains accepted entry");
            Assert(runtime.Catalog.CatalogRevision != 0UL, "catalog revision is deterministic nonzero identity");

            var firstOrder = new FeatureRegistrationRuntime();
            var secondOrder = new FeatureRegistrationRuntime();
            firstOrder.OpenRegistration();
            secondOrder.OpenRegistration();
            firstOrder.Register(new StubRegistration("io.example.beta"));
            firstOrder.Register(new StubRegistration("io.example.alpha"));
            secondOrder.Register(new StubRegistration("io.example.alpha"));
            secondOrder.Register(new StubRegistration("io.example.beta"));
            Assert(firstOrder.FreezeCatalog() && secondOrder.FreezeCatalog(), "both catalogs freeze");
            Assert(firstOrder.Catalog.CatalogRevision == secondOrder.Catalog.CatalogRevision, "catalog revision ignores registration arrival order");
            Assert(firstOrder.Catalog.Entries[0].Definition.Feature.Value == "io.example.alpha", "catalog order is ordinal by feature id");
            var invalidRuntime = new FeatureRegistrationRuntime();
            invalidRuntime.OpenRegistration();
            var invalid = invalidRuntime.Register(new StubRegistration("io.example.invalid", true));
            Assert(!invalid.Accepted && invalid.Reason == FeatureRegistrationReason.InvalidDefinitionArtifact, "invalid artifact is rejected");
            Assert(invalidRuntime.FreezeCatalog() && invalidRuntime.Catalog.Entries.Count == 0, "invalid registration does not pollute catalog");
            var invalidUiRuntime = new FeatureRegistrationRuntime();
            invalidUiRuntime.OpenRegistration();
            var invalidUi = invalidUiRuntime.Register(new StubRegistration("io.example.bad-ui", false, new StubSatellite(string.Empty, string.Empty)));
            Assert(!invalidUi.Accepted && invalidUi.Reason == FeatureRegistrationReason.InvalidClientUiRegistration, "invalid client ui metadata is rejected");
            var mutableRuntime = new FeatureRegistrationRuntime();
            mutableRuntime.OpenRegistration();
            var mutable = new MutableRegistration("io.example.stable");
            Assert(mutableRuntime.Register(mutable).Accepted, "mutable registration is initially accepted");
            mutable.Definition = new FeatureDefinitionArtifact(new FeatureId("io.example.changed"), 1, "tracer", new Digest256(9, 9, 9, 9), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
            Assert(mutableRuntime.FreezeCatalog() && mutableRuntime.Catalog.Entries[0].Definition.Feature.Value == "io.example.stable", "catalog uses registration snapshot and does not drift");
            var throwingRuntime = new FeatureRegistrationRuntime();
            throwingRuntime.OpenRegistration();
            var throwing = throwingRuntime.Register(new ThrowingRegistration());
            Assert(!throwing.Accepted && throwing.Reason == FeatureRegistrationReason.InvalidDefinitionArtifact, "throwing registration getter fails closed");
            var presentation = new FeaturePresentationView(new FeatureId("io.example.tracer"), FeaturePresentationState.PresentationDegraded, "BUE-UI-001", 1UL);
            Assert(presentation.State == FeaturePresentationState.PresentationDegraded && presentation.PresentationRevision == 1UL, "presentation state is a separate value projection");
            Console.WriteLine("DEV-10 registration runtime tests: PASS");
            return 0;
        }

        private sealed class StubRegistration : IFeatureRegistration
        {
            public StubRegistration(string feature)
                : this(feature, false)
            {
            }

            public StubRegistration(string feature, bool invalid)
                : this(feature, invalid, null)
            {
            }

            public StubRegistration(string feature, bool invalid, IClientUiSatelliteRegistration clientUi)
            {
                Definition = invalid
                    ? new FeatureDefinitionArtifact(new FeatureId(feature), 0, string.Empty, new Digest256(), new Digest256(), new byte[0])
                    : new FeatureDefinitionArtifact(new FeatureId(feature), 1, "tracer", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
                MinimumBueContract = new ContractVersion(1, 0);
                ModuleFactory = new StubFactory();
                ClientUi = clientUi;
            }
            public FeatureDefinitionArtifact Definition { get; private set; }
            public ContractVersion MinimumBueContract { get; private set; }
            public IFeatureModuleFactory ModuleFactory { get; private set; }
            public IClientUiSatelliteRegistration ClientUi { get; private set; }
        }

        private sealed class StubFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return null; }
        }

        private sealed class MutableRegistration : IFeatureRegistration
        {
            public MutableRegistration(string feature) { Definition = new FeatureDefinitionArtifact(new FeatureId(feature), 1, "tracer", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 }); }
            public FeatureDefinitionArtifact Definition { get; set; }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(1, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new StubFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class ThrowingRegistration : IFeatureRegistration
        {
            public FeatureDefinitionArtifact Definition { get { throw new InvalidOperationException("test getter failure"); } }
            public ContractVersion MinimumBueContract { get { throw new InvalidOperationException("test getter failure"); } }
            public IFeatureModuleFactory ModuleFactory { get { throw new InvalidOperationException("test getter failure"); } }
            public IClientUiSatelliteRegistration ClientUi { get { throw new InvalidOperationException("test getter failure"); } }
        }

        private sealed class StubSatellite : IClientUiSatelliteRegistration
        {
            public StubSatellite(string satelliteId, string token) { SatelliteId = satelliteId; RegistrationToken = token; }
            public string SatelliteId { get; private set; }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(1, 0); } }
            public string RegistrationToken { get; private set; }
        }

        private static FeatureDefinitionFragment Fragment(FeatureId feature, string kind)
        {
            return Fragment(feature, kind, new FeatureId[0]);
        }

        private static FeatureDefinitionFragment Fragment(FeatureId feature, string kind, params FeatureId[] required)
        {
            return new FeatureDefinitionFragment(feature, kind, 1, new Digest256(1, 2, 3, 4), required);
        }

        private static bool SameDigest(Digest256 left, Digest256 right)
        {
            return left.Part0 == right.Part0 && left.Part1 == right.Part1 && left.Part2 == right.Part2 && left.Part3 == right.Part3;
        }

        private static bool HasDiagnostic(LinkResult result, string expected)
        {
            foreach (var diagnostic in result.Diagnostics) if (diagnostic == expected) return true;
            return false;
        }

        private static string JoinDiagnostics(LinkResult result)
        {
            return string.Join("|", result.Diagnostics);
        }

        private static bool ReadOnlyListRejectsAdd<T>(IReadOnlyList<T> values)
        {
            var list = values as IList<T>;
            if (list == null) return false;
            try { list.Add(default(T)); return false; }
            catch (NotSupportedException) { return true; }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
