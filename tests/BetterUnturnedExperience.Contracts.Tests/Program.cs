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

            // DEV-V2-14 ①②: directional inbound subscribe and the bootstrap
            // network member are frozen contract surface (change log entries
            // ① Subscribe+ChannelDirection, ② IFeatureBootstrap.Network).
            Assert(typeof(BueNetwork.ChannelDirection).IsEnum
                && Enum.GetUnderlyingType(typeof(BueNetwork.ChannelDirection)) == typeof(byte),
                "DEV-V2-14: ChannelDirection is a byte enum");
            Assert((byte)BueNetwork.ChannelDirection.FromClients == 0 && (byte)BueNetwork.ChannelDirection.FromServer == 1,
                "DEV-V2-14: ChannelDirection values are frozen (FromClients=0, FromServer=1)");
            var apiSubscribe = typeof(BueNetwork.IBueNetworkApi).GetMethod("Subscribe");
            Assert(apiSubscribe != null && apiSubscribe.ReturnType == typeof(IDisposable)
                && apiSubscribe.GetParameters().Length == 3
                && apiSubscribe.GetParameters()[0].ParameterType == typeof(FeatureId)
                && apiSubscribe.GetParameters()[1].ParameterType == typeof(BueNetwork.ChannelDirection)
                && apiSubscribe.GetParameters()[2].ParameterType == typeof(Action<BueNetwork.IConnectionSession, byte[]>),
                "DEV-V2-14: IBueNetworkApi.Subscribe(FeatureId, ChannelDirection, handler) returns an independent dispose handle");
            var bootstrapNetwork = typeof(IFeatureBootstrap).GetProperty("Network");
            Assert(bootstrapNetwork != null && bootstrapNetwork.PropertyType == typeof(BueNetwork.IBueNetworkApi),
                "DEV-V2-14: IFeatureBootstrap.Network carries the pure-C# network API (no host/LMN/Unity type leakage)");

            // DEV-V2-19 ⑤: the TidyCompleted feature event is frozen contract
            // surface — a readonly struct whose identity string derives from
            // the publisher FeatureId (registry entries ⑤, change log 2.0).
            Assert(typeof(TidyCompleted).IsValueType, "DEV-V2-19: TidyCompleted is a readonly struct (value type)");
            Assert(TidyCompleted.EventId == "io.github.yu80rice.bue.inventory-tidy/tidy-completed",
                "DEV-V2-19: TidyCompleted event identity derives from the publisher FeatureId (io.github.yu80rice.bue.inventory-tidy/tidy-completed)");
            var tidyPublisher = typeof(TidyCompleted).GetProperty("Publisher");
            Assert(tidyPublisher != null && tidyPublisher.PropertyType == typeof(FeatureId),
                "DEV-V2-19: TidyCompleted.Publisher carries the publisher FeatureId");
            Assert(typeof(TidyCompleted).GetProperty("FirstPage")?.PropertyType == typeof(byte)
                && typeof(TidyCompleted).GetProperty("LastPage")?.PropertyType == typeof(byte),
                "DEV-V2-19: TidyCompleted scope is the inclusive page range FirstPage..LastPage");
            var tidyResult = typeof(TidyCompleted).GetProperty("Result");
            Assert(tidyResult != null && tidyResult.PropertyType == typeof(TidyCompletionResult),
                "DEV-V2-19: TidyCompleted.Result carries the frozen completion result enum");
            Assert(typeof(TidyCompleted).GetProperty("ConnectionGeneration")?.PropertyType == typeof(ulong),
                "DEV-V2-19: TidyCompleted.ConnectionGeneration is ulong (0 = not applicable, local path)");
            Assert(typeof(TidyCompleted).GetProperty("TransactionId")?.PropertyType == typeof(ulong),
                "DEV-V2-19: TidyCompleted.TransactionId is ulong (feature-private operation identity)");
            Assert(typeof(TidyCompletionResult).IsEnum && Enum.GetUnderlyingType(typeof(TidyCompletionResult)) == typeof(byte)
                && (byte)TidyCompletionResult.Succeeded == 1 && (byte)TidyCompletionResult.Rejected == 2 && (byte)TidyCompletionResult.Failed == 3,
                "DEV-V2-19: TidyCompletionResult is a byte enum frozen (Succeeded=1, Rejected=2, Failed=3)");

            // DEV-V2-19 ⑥: the HostTick host clock is frozen contract surface
            // — a readonly struct carrying only seq/delta/phase (no feature
            // logic payload).
            Assert(typeof(HostTick).IsValueType, "DEV-V2-19: HostTick is a readonly struct (value type)");
            Assert(HostTick.EventId == "io.github.yu80rice.bue.host/host-tick",
                "DEV-V2-19: HostTick event identity derives from the platform host identity (io.github.yu80rice.bue.host/host-tick)");
            Assert(typeof(HostTick).GetProperty("TickNumber")?.PropertyType == typeof(ulong),
                "DEV-V2-19: HostTick.TickNumber is the monotonic ulong sequence");
            Assert(typeof(HostTick).GetProperty("DeltaTime")?.PropertyType == typeof(float),
                "DEV-V2-19: HostTick.DeltaTime is the float seconds increment");
            var tickPhase = typeof(HostTick).GetProperty("Phase");
            Assert(tickPhase != null && tickPhase.PropertyType == typeof(TickPhase),
                "DEV-V2-19: HostTick.Phase carries the frozen tick phase enum");
            Assert(typeof(TickPhase).IsEnum && Enum.GetUnderlyingType(typeof(TickPhase)) == typeof(byte)
                && (byte)TickPhase.Update == 0,
                "DEV-V2-19: TickPhase is a byte enum with Update=0 frozen");

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

            // DEV-V2-14: the contract Major bump raises the registration gate
            // to (2,0) — aligned registrations pass, futures are rejected.
            var gateRuntime = new FeatureRegistrationRuntime();
            gateRuntime.OpenRegistration();
            var aligned = gateRuntime.Register(new StubRegistration("io.example.gate-aligned"));
            Assert(aligned.Accepted, "DEV-V2-14: a (2,0) minimum-contract registration passes the raised gate");
            var gateTooNewRuntime = new FeatureRegistrationRuntime();
            gateTooNewRuntime.OpenRegistration();
            var gateTooNew = gateTooNewRuntime.Register(new HighContractRegistration("io.example.gate-toonew"));
            Assert(!gateTooNew.Accepted && gateTooNew.Reason == FeatureRegistrationReason.ContractIncompatible,
                "DEV-V2-14: a (3,0) minimum-contract registration is rejected with ContractIncompatible");

            // DEV-V3-01: the registration code table BUE-REG-001..010 is
            // anchored code by code (Reason AND DiagnosticId), the official
            // identity whitelist gates the reserved segment behind a fixed
            // decision order, and the contract gate opens for the Minor 2.1
            // additive batch while (2,0) stays registrable.
            Assert((ushort)FeatureRegistrationReason.ReservedFeatureId == 206,
                "DEV-V3-01: ReservedFeatureId is the additive reason value 206");
            var codeRuntime = new FeatureRegistrationRuntime();
            var codeResult = codeRuntime.Register(new StubRegistration("io.example.code-001"));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.HostUnavailable && codeResult.DiagnosticId == "BUE-REG-001",
                "DEV-V3-01: BUE-REG-001 anchors HostUnavailable");
            codeRuntime = new FeatureRegistrationRuntime();
            codeRuntime.EnterCoreSafeMode();
            codeResult = codeRuntime.Register(new StubRegistration("io.example.code-002"));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.CoreUnavailable && codeResult.DiagnosticId == "BUE-REG-002",
                "DEV-V3-01: BUE-REG-002 anchors CoreUnavailable");
            codeRuntime = new FeatureRegistrationRuntime();
            codeRuntime.OpenRegistration();
            codeRuntime.FreezeCatalog();
            codeResult = codeRuntime.Register(new StubRegistration("io.example.code-003"));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.PhaseClosed && codeResult.DiagnosticId == "BUE-REG-003",
                "DEV-V3-01: BUE-REG-003 anchors PhaseClosed");
            codeRuntime = new FeatureRegistrationRuntime();
            codeRuntime.OpenRegistration();
            codeResult = codeRuntime.Register(new StubRegistration("io.example.code-004", true));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.InvalidDefinitionArtifact && codeResult.DiagnosticId == "BUE-REG-004",
                "DEV-V3-01: BUE-REG-004 anchors InvalidDefinitionArtifact");
            codeResult = codeRuntime.Register(new NoFactoryRegistration("io.example.code-005"));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.InvalidModuleFactory && codeResult.DiagnosticId == "BUE-REG-005",
                "DEV-V3-01: BUE-REG-005 anchors InvalidModuleFactory");
            codeResult = codeRuntime.Register(new HighContractRegistration("io.example.code-006"));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.ContractIncompatible && codeResult.DiagnosticId == "BUE-REG-006",
                "DEV-V3-01: BUE-REG-006 anchors ContractIncompatible");
            var codeDupRuntime = new FeatureRegistrationRuntime();
            codeDupRuntime.OpenRegistration();
            Assert(codeDupRuntime.Register(new StubRegistration("io.github.yu80rice.bue.inventory-tidy")).Accepted,
                "DEV-V3-01: a whitelisted official identity registers (whitelist positive)");
            codeResult = codeDupRuntime.Register(new StubRegistration("io.github.yu80rice.bue.inventory-tidy"));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.DuplicateFeature && codeResult.DiagnosticId == "BUE-REG-007",
                "DEV-V3-01: BUE-REG-007 anchors DuplicateFeature behind the whitelist pass");
            codeResult = codeDupRuntime.Register(new StubRegistration("io.example.code-008", false, new StubSatellite(string.Empty, string.Empty)));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.InvalidClientUiRegistration && codeResult.DiagnosticId == "BUE-REG-008",
                "DEV-V3-01: BUE-REG-008 anchors InvalidClientUiRegistration");
            codeResult = codeDupRuntime.Register(new ThrowingRegistration());
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.InvalidDefinitionArtifact && codeResult.DiagnosticId == "BUE-REG-009",
                "DEV-V3-01: BUE-REG-009 anchors the fail-closed registration getter path");
            codeResult = codeDupRuntime.Register(new StubRegistration("io.github.yu80rice.bue.rogue"));
            Assert(!codeResult.Accepted && codeResult.Reason == FeatureRegistrationReason.ReservedFeatureId && codeResult.DiagnosticId == "BUE-REG-010",
                "DEV-V3-01: BUE-REG-010 anchors the reserved-segment rejection");
            var whitelistRuntime = new FeatureRegistrationRuntime();
            whitelistRuntime.OpenRegistration();
            var officialIds = new[]
            {
                "io.github.yu80rice.bue.better-item-interaction", "io.github.yu80rice.bue.inventory-tidy",
                "io.github.yu80rice.bue.in-place-reload", "io.github.yu80rice.bue.horde-tracker",
                "io.github.yu80rice.bue.network", "io.github.yu80rice.bue.network.v1compat", "io.github.yu80rice.bue.noop"
            };
            foreach (var officialId in officialIds)
            {
                Assert(whitelistRuntime.Register(new StubRegistration(officialId)).Accepted,
                    "DEV-V3-01: whitelisted official identity registers: " + officialId);
            }
            var rootRuntime = new FeatureRegistrationRuntime();
            rootRuntime.OpenRegistration();
            var rootResult = rootRuntime.Register(new StubRegistration("io.github.yu80rice.bue"));
            Assert(!rootResult.Accepted && rootResult.Reason == FeatureRegistrationReason.ReservedFeatureId && rootResult.DiagnosticId == "BUE-REG-010",
                "DEV-V3-01: the bare reserved root itself is not registrable");
            var siblingRuntime = new FeatureRegistrationRuntime();
            siblingRuntime.OpenRegistration();
            Assert(siblingRuntime.Register(new StubRegistration("io.github.yu80rice.bue2.rogue")).Accepted,
                "DEV-V3-01: a sibling reverse-domain outside the reserved segment registers");
            var orderRuntime = new FeatureRegistrationRuntime();
            orderRuntime.OpenRegistration();
            var reservedOverContract = orderRuntime.Register(new HighContractRegistration("io.github.yu80rice.bue.rogue"));
            Assert(!reservedOverContract.Accepted && reservedOverContract.Reason == FeatureRegistrationReason.ReservedFeatureId,
                "DEV-V3-01: the reserved-segment gate precedes the contract gate");
            var formatRuntime = new FeatureRegistrationRuntime();
            formatRuntime.OpenRegistration();
            var formatOverReserved = formatRuntime.Register(new NoFactoryRegistration("io.github.yu80rice.bue.rogue"));
            Assert(!formatOverReserved.Accepted && formatOverReserved.Reason == FeatureRegistrationReason.InvalidModuleFactory,
                "DEV-V3-01: artifact format checks precede the reserved-segment gate");
            var minorRuntime = new FeatureRegistrationRuntime();
            minorRuntime.OpenRegistration();
            Assert(minorRuntime.Register(new VersionedRegistration("io.example.minor-21", new ContractVersion(2, 1))).Accepted,
                "DEV-V3-01: a (2,1) minimum-contract registration passes the gate (Minor 2.1)");
            var minorCompatRuntime = new FeatureRegistrationRuntime();
            minorCompatRuntime.OpenRegistration();
            Assert(minorCompatRuntime.Register(new VersionedRegistration("io.example.minor-20", new ContractVersion(2, 0))).Accepted,
                "DEV-V3-01: a (2,0) registration stays registrable (compatibility)");
            var minorToonewRuntime = new FeatureRegistrationRuntime();
            minorToonewRuntime.OpenRegistration();
            var minorToonew = minorToonewRuntime.Register(new VersionedRegistration("io.example.minor-22", new ContractVersion(2, 2)));
            Assert(!minorToonew.Accepted && minorToonew.Reason == FeatureRegistrationReason.ContractIncompatible,
                "DEV-V3-01: a (2,2) minimum-contract registration is rejected");
            var majorFloorRuntime = new FeatureRegistrationRuntime();
            majorFloorRuntime.OpenRegistration();
            var majorFloor = majorFloorRuntime.Register(new VersionedRegistration("io.example.major-1", new ContractVersion(1, 0)));
            Assert(!majorFloor.Accepted && majorFloor.Reason == FeatureRegistrationReason.ContractIncompatible && majorFloor.DiagnosticId == "BUE-REG-006",
                "DEV-V3-01: the host gate is Major 2 — a (1,0) minimum-contract registration is rejected");

            // DEV-V3-02: the event-type ownership registration seam — the
            // public contract surface (Minor 2.1 additive). The reason table
            // values are frozen (BUE-EVT-001..004), the result is the explicit
            // triple (Registered/Reason/DiagnosticId), and the registry hangs
            // off the bootstrap as its own service seam.
            Assert((byte)FeatureEventRegistrationReason.None == 0
                && (byte)FeatureEventRegistrationReason.InvalidEventId == 1
                && (byte)FeatureEventRegistrationReason.EventIdNotDerivedFromOwner == 2
                && (byte)FeatureEventRegistrationReason.EventTypeAlreadyRegistered == 3
                && (byte)FeatureEventRegistrationReason.EventIdAlreadyRegistered == 4,
                "DEV-V3-02: FeatureEventRegistrationReason values are frozen (BUE-EVT-001..004)");
            var evtAccepted = new FeatureEventRegistrationResult(true, FeatureEventRegistrationReason.None, "BUE-EVT-ACCEPT");
            Assert(evtAccepted.Registered && evtAccepted.Reason == FeatureEventRegistrationReason.None && evtAccepted.DiagnosticId == "BUE-EVT-ACCEPT",
                "DEV-V3-02: FeatureEventRegistrationResult carries the explicit acceptance triple");
            var evtRejected = new FeatureEventRegistrationResult(false, FeatureEventRegistrationReason.EventTypeAlreadyRegistered, "BUE-EVT-003");
            Assert(!evtRejected.Registered && evtRejected.Reason == FeatureEventRegistrationReason.EventTypeAlreadyRegistered && evtRejected.DiagnosticId == "BUE-EVT-003",
                "DEV-V3-02: FeatureEventRegistrationResult carries the explicit rejection triple");
            Assert(typeof(IFeatureEventRegistry).IsInterface,
                "DEV-V3-02: IFeatureEventRegistry is the public event-type registration seam");
            Assert(typeof(IFeatureBootstrap).GetProperty("EventRegistry") != null,
                "DEV-V3-02: IFeatureBootstrap composes the EventRegistry seam member");

            // DEV-V3-03: the lifecycle projection surface (Minor 2.1 additive).
            // The read-only status query joins IFeatureLifetime (member named by
            // DEV-V3-03); the nine-state FeatureState machine, the frozen
            // FeatureStopReason values and the six-member FeatureStatusView
            // projection keep their frozen shapes; the two bootstrap members
            // this ticket wires (Lifetime/Dependencies) are the availability
            // matrix rows that turn non-null from DEV-V3-03 on.
            Assert(typeof(IFeatureLifetime).IsInterface
                && typeof(IFeatureLifetime).GetMethod("TryTrack") != null
                && typeof(IFeatureLifetime).GetProperty("CurrentStatus") != null,
                "DEV-V3-03: IFeatureLifetime carries TryTrack plus the read-only status query member (CurrentStatus)");
            Assert(Enum.GetValues(typeof(FeatureState)).Length == 9,
                "DEV-V3-03: FeatureState stays the frozen nine-state machine");
            Assert((byte)FeatureStopReason.PluginStopping == 1
                && (byte)FeatureStopReason.UserDisabled == 2
                && (byte)FeatureStopReason.RuntimeIsolated == 4
                && (byte)FeatureStopReason.CoreSafeMode == 7,
                "DEV-V3-03: FeatureStopReason values stay frozen (PluginStopping=1/UserDisabled=2/RuntimeIsolated=4/CoreSafeMode=7)");
            var statusViewProperties = typeof(FeatureStatusView).GetProperties();
            var statusViewMemberCount = 0;
            var statusViewHasState = false;
            var statusViewHasRevision = false;
            for (var i = 0; i < statusViewProperties.Length; i++)
            {
                statusViewMemberCount++;
                if (statusViewProperties[i].Name == "State") statusViewHasState = true;
                if (statusViewProperties[i].Name == "StateRevision") statusViewHasRevision = true;
            }
            Assert(statusViewMemberCount == 6 && statusViewHasState && statusViewHasRevision,
                "DEV-V3-03: FeatureStatusView stays the six-member immutable projection (Feature/State/Error/StopReason/DiagnosticId/StateRevision)");
            Assert(typeof(IFeatureBootstrap).GetProperty("Lifetime") != null
                && typeof(IFeatureBootstrap).GetProperty("Dependencies") != null,
                "DEV-V3-03: the two matrix members wired by DEV-V3-03 exist on the bootstrap");
            Assert(typeof(IDependencyCapabilityView).GetMethod("Has") != null
                && typeof(IDependencyCapabilityView).GetMethod("TryGet") != null,
                "DEV-V3-03: Dependencies stays the read-only catalog capability lookup (Has/TryGet, no solver)");

            // DEV-V3-04: the network transport-rules surface (Minor 2.1
            // additive). The Throttled send result joins the frozen outcome
            // set with a dedicated value; the platform main-thread dispatcher
            // seam (IFeatureBootstrap.MainThread) carries EXACTLY one
            // fire-and-forget post method returning an explicit three-way
            // result (success / capacity rejection / generation invalid) —
            // no result object, no wait handle (need a result: event or
            // network reply). Old modules must degrade safely on the new
            // enum value: only explicit Sent counts as success.
            Assert(Enum.GetUnderlyingType(typeof(BueNetwork.NetworkSendResult)) == typeof(ushort)
                    && (ushort)BueNetwork.NetworkSendResult.Throttled == 205,
                "DEV-V3-04: NetworkSendResult.Throttled is the additive value 205 in the frozen ushort set");
            var sendValues = (Array)Enum.GetValues(typeof(BueNetwork.NetworkSendResult));
            var throttledSeen = 0;
            for (var valueIndex = 0; valueIndex < sendValues.Length; valueIndex++)
            {
                if ((ushort)sendValues.GetValue(valueIndex) == 205) throttledSeen++;
            }
            Assert(throttledSeen == 1, "DEV-V3-04: the Throttled value 205 is unique (no reuse of a frozen value)");
            Assert(typeof(BueNetwork.IBueNetworkApi).GetMethods().Length == 7
                    && typeof(BueNetwork.IBueNetworkApi).GetMethod("IsNetworkReady") == null
                    && typeof(BueNetwork.IBueNetworkApi).GetMethod("GetBindingState") == null
                    && typeof(BueNetwork.IBueNetworkApi).GetMethod("GetTransportHealth") == null,
                "DEV-V3-04: the network API surface stays the frozen seven members (no binding-state/health query surface added)");
            var mtMethods = typeof(IFeatureMainThread).GetMethods();
            Assert(typeof(IFeatureMainThread).IsInterface && mtMethods.Length == 1
                    && mtMethods[0].Name == "Post"
                    && mtMethods[0].ReturnType == typeof(MainThreadPostResult)
                    && mtMethods[0].GetParameters().Length == 1
                    && mtMethods[0].GetParameters()[0].ParameterType == typeof(Action),
                "DEV-V3-04: IFeatureMainThread carries exactly one fire-and-forget Post(Action) seam");
            Assert(typeof(IFeatureBootstrap).GetProperty("MainThread") != null
                    && typeof(IFeatureBootstrap).GetProperty("MainThread").PropertyType == typeof(IFeatureMainThread),
                "DEV-V3-04: IFeatureBootstrap composes the MainThread dispatcher member (matrix row wired by DEV-V3-04)");
            Assert((byte)MainThreadPostReason.None == 0
                    && (byte)MainThreadPostReason.CapacityExceeded == 1
                    && (byte)MainThreadPostReason.GenerationInvalid == 2,
                "DEV-V3-04: MainThreadPostReason values are frozen (None=0/CapacityExceeded=1/GenerationInvalid=2)");
            var mtAccepted = new MainThreadPostResult(true, MainThreadPostReason.None, "BUE-MT-ACCEPT");
            var mtRejected = new MainThreadPostResult(false, MainThreadPostReason.CapacityExceeded, "BUE-MT-001");
            Assert(mtAccepted.Posted && mtAccepted.Reason == MainThreadPostReason.None && mtAccepted.DiagnosticId == "BUE-MT-ACCEPT"
                    && !mtRejected.Posted && mtRejected.Reason == MainThreadPostReason.CapacityExceeded && mtRejected.DiagnosticId == "BUE-MT-001",
                "DEV-V3-04: MainThreadPostResult carries the explicit outcome triple (posted/reason/diagnostic)");

            // DEV-V3-05: the host clock payload-shape registration — this
            // ticket adds ZERO new contract surface. HostTick carries exactly
            // the three timing properties (TickNumber/DeltaTime/Phase — no
            // business payload ever rides the clock) under the frozen reserved
            // identity string; TickPhase stays the single frozen member
            // Update=0 (further phases = an explicit additive registry
            // extension, never implied); the event-seam views stay their
            // single-method shapes and IFeatureBootstrap stays at its eleven
            // members (the matrix unchanged by this ticket).
            var tickProperties = typeof(HostTick).GetProperties();
            var tickHasSequence = false;
            var tickHasDelta = false;
            var tickHasPhase = false;
            for (var propertyIndex = 0; propertyIndex < tickProperties.Length; propertyIndex++)
            {
                if (tickProperties[propertyIndex].Name == "TickNumber") tickHasSequence = true;
                if (tickProperties[propertyIndex].Name == "DeltaTime") tickHasDelta = true;
                if (tickProperties[propertyIndex].Name == "Phase") tickHasPhase = true;
            }
            Assert(tickProperties.Length == 3 && tickHasSequence && tickHasDelta && tickHasPhase,
                "DEV-V3-05: HostTick stays the three-field timing payload (TickNumber/DeltaTime/Phase) — no business field rides the clock");
            Assert(HostTick.EventId == "io.github.yu80rice.bue.host/host-tick" && typeof(HostTick).IsValueType,
                "DEV-V3-05: the HostTick reserved identity string and value-type payload shape stay frozen");
            Assert(Enum.GetValues(typeof(TickPhase)).Length == 1 && (byte)TickPhase.Update == 0,
                "DEV-V3-05: TickPhase stays the single frozen member Update=0 — further phases are an explicit contract-registry addition");
            Assert(typeof(IOwnedFeatureEventPublisher).GetMethods().Length == 1
                    && typeof(IFeatureEventSubscriber).GetMethods().Length == 1
                    && typeof(IFeatureEventRegistry).GetMethods().Length == 1
                    && typeof(IFeatureBootstrap).GetProperties().Length == 11,
                "DEV-V3-05: zero new contract surface — the event-seam views stay single-method and IFeatureBootstrap stays at its eleven members");

            // DEV-V3-06: the settings facet + scoped view shapes (Minor 2.1
            // additive). The optional settings facet a registration may
            // additionally implement carries EXACTLY two members (schema +
            // post-apply refresh hook); IFeatureRegistration itself stays its
            // four frozen properties (adding a member to an implementer-side
            // interface would break 2.0 ecosystem implementations at runtime —
            // the facet is discovered by type test, never demanded); the
            // injected view stays exactly three methods (the session-overlay
            // mutators and any other-feature query surface are permanently
            // unreachable from the module side; the runtime class body stays
            // out of contract).
            Assert(typeof(IFeatureSettingsRegistration).IsInterface
                    && typeof(IFeatureSettingsRegistration).GetProperties().Length == 2
                    && typeof(IFeatureSettingsRegistration).GetProperty("SettingDescriptors") != null
                    && typeof(IFeatureSettingsRegistration).GetProperty("OnSettingsApplied") != null,
                "DEV-V3-06: IFeatureSettingsRegistration is the optional settings facet with exactly two members (SettingDescriptors/OnSettingsApplied)");
            Assert(typeof(IFeatureRegistration).GetProperties().Length == 4
                    && typeof(IFeatureRegistration).GetProperty("SettingDescriptors") == null,
                "DEV-V3-06: IFeatureRegistration stays the four frozen properties (facet discovered by type test, never added to the interface)");
            Assert(typeof(IScopedFeatureSettings).GetMethods().Length == 3
                    && typeof(IScopedFeatureSettings).GetMethod("ApplyServerPolicy") == null
                    && typeof(IScopedFeatureSettings).GetMethod("ClearSessionOverlay") == null
                    && typeof(IScopedFeatureSettings).GetMethod("ActivateConnectionGeneration") == null,
                "DEV-V3-06: the scoped settings view stays GetSnapshot/TryGet/Submit — runtime-overlay mutators never join the module-facing surface");
            Assert(typeof(IFeatureBootstrap).GetProperty("Settings") != null
                    && typeof(IFeatureBootstrap).GetProperty("Settings").PropertyType == typeof(IScopedFeatureSettings),
                "DEV-V3-06: IFeatureBootstrap.Settings is the matrix member this ticket wires (the scoped view type, never the runtime type)");

            // DEV-V3-07 shape anchor: the wiring ticket adds ZERO contract
            // surface — IFeatureLogger stays the frozen three-method narrow
            // surface (void returns: no result type may creep in, the fault
            // isolation is a view-side obligation), and the bootstrap keeps
            // its eleven members (any implicit expansion must redden the 05
            // anchor — machine-enforced registration discipline).
            Assert(typeof(IFeatureBootstrap).GetProperty("Logger") != null
                    && typeof(IFeatureBootstrap).GetProperty("Logger").PropertyType == typeof(IFeatureLogger),
                "DEV-V3-07: IFeatureBootstrap.Logger is the matrix member this ticket wires (the feature-bound view, never a runtime type)");
            var loggerMethods = typeof(IFeatureLogger).GetMethods();
            Assert(loggerMethods.Length == 3
                    && typeof(IFeatureLogger).GetMethod("Info").GetParameters().Length == 2
                    && typeof(IFeatureLogger).GetMethod("Warning").GetParameters().Length == 3
                    && typeof(IFeatureLogger).GetMethod("Error").GetParameters().Length == 4
                    && typeof(IFeatureLogger).GetMethod("Info").ReturnType == typeof(void)
                    && typeof(IFeatureLogger).GetMethod("Warning").ReturnType == typeof(void)
                    && typeof(IFeatureLogger).GetMethod("Error").ReturnType == typeof(void),
                "DEV-V3-07: IFeatureLogger stays the three-method void narrow surface (Info/Warning/Error, no result type — the frozen shape the wiring must not widen)");
            Assert(typeof(IFeatureBootstrap).GetProperties().Length == 11,
                "DEV-V3-07: the wiring ticket adds zero new bootstrap members (still eleven — zero-new-surface machine anchor)");

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
                MinimumBueContract = new ContractVersion(2, 0);
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
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new StubFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class HighContractRegistration : IFeatureRegistration
        {
            public HighContractRegistration(string feature) { Definition = new FeatureDefinitionArtifact(new FeatureId(feature), 1, "tracer", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 }); }
            public FeatureDefinitionArtifact Definition { get; }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(3, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new StubFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class NoFactoryRegistration : IFeatureRegistration
        {
            public NoFactoryRegistration(string feature) { Definition = new FeatureDefinitionArtifact(new FeatureId(feature), 1, "tracer", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 }); }
            public FeatureDefinitionArtifact Definition { get; }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return null; } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class VersionedRegistration : IFeatureRegistration
        {
            public VersionedRegistration(string feature, ContractVersion minimumContract)
            {
                Definition = new FeatureDefinitionArtifact(new FeatureId(feature), 1, "tracer", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
                MinimumBueContract = minimumContract;
            }
            public FeatureDefinitionArtifact Definition { get; }
            public ContractVersion MinimumBueContract { get; }
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
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
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
