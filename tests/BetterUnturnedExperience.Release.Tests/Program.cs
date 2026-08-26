using System;
using BetterUnturnedExperience.Release;

namespace BetterUnturnedExperience.Release.Tests
{
    internal static class Program
    {
        private const string HashA = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string HashB = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        private const string DigestA = "1111111111111111111111111111111111111111111111111111111111111111";
        private const string DigestB = "2222222222222222222222222222222222222222222222222222222222222222";
        private static readonly DateTime Start = new DateTime(2026, 8, 25, 4, 0, 0, DateTimeKind.Utc);
        private static int Main() { try { Run(); Console.WriteLine("DEV-15E qualification evidence tests: PASS"); return 0; } catch (Exception error) { Console.WriteLine("DEV-15E qualification evidence tests: FAIL"); Console.WriteLine(error); return 1; } }
        private static void Run()
        {
            var a = CandidateBuildDescriptor.Create("source-1", DigestA, DigestB, "toolchain-1", "client-ref-1", "u3ds-ref-1", HashA);
            var b = CandidateBuildDescriptor.Create("source-1", DigestA, DigestB, "toolchain-1", "client-ref-1", "u3ds-ref-1", HashB);
            Assert(a.BuildIdentity == b.BuildIdentity, "DLL hash does not contaminate BuildIdentity"); Assert(CandidateBuildDescriptor.Create("source-1", DigestA, DigestB, "toolchain-1", "client-ref-1", "u3ds-ref-1", HashA).BuildIdentity == a.BuildIdentity, "BuildIdentity is deterministic");
            AssertThrows(() => CandidateBuildDescriptor.Create("source-1", "AA", DigestB, "toolchain-1", "client-ref-1", "u3ds-ref-1", HashA), "short digest rejected"); AssertThrows(() => CandidateBuildDescriptor.Create("source-1", DigestA, DigestB, " toolchain-1", "client-ref-1", "u3ds-ref-1", HashA), "identity whitespace rejected");
            var sp = Evidence("case-sp", EvidenceEnvironmentRole.SinglePlayer, a, 0, 10, HashA); var hostOld = Evidence("case-old", EvidenceEnvironmentRole.SteamP2PHost, a, 0, 1, HashA); var clientOld = Evidence("case-old", EvidenceEnvironmentRole.SteamP2PClient, a, 0, 1, HashA); var host = Evidence("case-p2p", EvidenceEnvironmentRole.SteamP2PHost, a, 20, 30, HashA); var client = Evidence("case-p2p", EvidenceEnvironmentRole.SteamP2PClient, a, 21, 29, HashA); var u3ds = Evidence("case-u3ds", EvidenceEnvironmentRole.U3dsHeadless, a, 0, 10, HashA);
            var result = QualificationEvaluator.Evaluate(new[] { sp, hostOld, clientOld, host, client, u3ds }, a, QualificationPolicy.Default);
            Assert(result.For(EvidenceEnvironmentRole.SinglePlayer) == QualificationVerdict.Fulfilled, "SP fulfilled"); Assert(result.For(EvidenceEnvironmentRole.SteamP2PHost) == QualificationVerdict.Fulfilled && result.For(EvidenceEnvironmentRole.SteamP2PClient) == QualificationVerdict.Fulfilled, "P2P pair fulfilled from valid matching window"); Assert(result.For(EvidenceEnvironmentRole.U3dsHeadless) == QualificationVerdict.Fulfilled, "U3DS fulfilled"); Assert(result.For(EvidenceEnvironmentRole.U3dsClientUi) == QualificationVerdict.NotApplicable, "UI role is policy NotApplicable only");
            var stale = Evidence("case-stale", EvidenceEnvironmentRole.SinglePlayer, a, 0, 10, HashB); Assert(QualificationEvaluator.Evaluate(new[] { stale }, a, QualificationPolicy.Default).For(EvidenceEnvironmentRole.SinglePlayer) == QualificationVerdict.Stale, "hash mismatch is stale"); var missing = QualificationEvaluator.Evaluate(new EvidenceCase[0], a, QualificationPolicy.Default); Assert(missing.For(EvidenceEnvironmentRole.U3dsHeadless) == QualificationVerdict.Missing, "missing evidence is missing"); AssertThrows(() => QualificationPolicy.Create(true, true, true, false, false), "policy cannot disable U3DS headless"); Assert(sp.SerializeCanonical().Length > 0, "evidence has canonical serialization");
            var artifact = RuntimeEvidenceArtifact.Create("evidence.log", DigestA, 12, "log");
            var package = RuntimeEvidencePackage.Create("pkg-1", a, Start, "tester", new[] { sp }, new[] { artifact });
            var packageResult = RuntimeEvidencePackageValidator.Validate(package, a);
            Assert(packageResult.IsValid && packageResult.Codes.Count == 1 && packageResult.Codes[0] == EvidencePackageValidationCode.Valid, "valid evidence package accepted");
            Assert(package.SerializeCanonical() == RuntimeEvidencePackage.Create("pkg-1", a, Start, "tester", new[] { sp }, new[] { artifact }).SerializeCanonical(), "package serialization deterministic");
            Assert(package.CanonicalDigest.Length == 64, "package canonical digest is fixed width");
            AssertThrows(() => RuntimeEvidenceArtifact.Create("../escape.log", DigestA, 1, "log"), "path traversal rejected");
            Assert(RuntimeEvidenceArtifact.Create("logs/v1..txt", DigestA, 1, "log").RelativePath == "logs/v1..txt", "safe dotted filename accepted");
            var duplicate = RuntimeEvidencePackage.Create("pkg-2", a, Start, "tester", new[] { sp, sp }, new[] { artifact });
            Assert(!RuntimeEvidencePackageValidator.Validate(duplicate, a).IsValid, "duplicate case id rejected");
            var empty = RuntimeEvidencePackage.Create("pkg-empty", a, Start, "tester", new EvidenceCase[0], new RuntimeEvidenceArtifact[0]);
            Assert(!RuntimeEvidencePackageValidator.Validate(empty, a).IsValid, "empty package rejected");
            var mismatchedArtifact = RuntimeEvidenceArtifact.Create("evidence.log", DigestB, 12, "log");
            Assert(!RuntimeEvidencePackageValidator.Validate(RuntimeEvidencePackage.Create("pkg-mismatch", a, Start, "tester", new[] { sp }, new[] { mismatchedArtifact }), a).IsValid, "artifact digest mismatch rejected");
            var nullCase = RuntimeEvidencePackage.Create("pkg-null-case", a, Start, "tester", new EvidenceCase[] { null }, new[] { artifact });
            Assert(!RuntimeEvidencePackageValidator.Validate(nullCase, a).IsValid, "null evidence case rejected without throwing");
            var nullArtifact = RuntimeEvidencePackage.Create("pkg-null-artifact", a, Start, "tester", new[] { sp }, new RuntimeEvidenceArtifact[] { null });
            Assert(!RuntimeEvidencePackageValidator.Validate(nullArtifact, a).IsValid, "null artifact rejected without throwing");

            var fullPackage = FullPackage(a);
            var fullGate = QualificationEvidenceGate.Evaluate(fullPackage, a, QualificationPolicy.Default);
            Assert(fullGate.Status == QualificationEvidenceGateStatus.TechnicallyQualified, "full same-hash evidence is technically qualified");
            Assert(fullGate.BuildIdentity == a.BuildIdentity && fullGate.DllSha256 == a.DllSha256, "gate remains bound to candidate identity and DLL hash");
            Assert(fullGate.Qualification.For(EvidenceEnvironmentRole.SinglePlayer) == QualificationVerdict.Fulfilled, "gate exposes SP verdict");
            Assert(fullGate.Qualification.For(EvidenceEnvironmentRole.U3dsHeadless) == QualificationVerdict.Fulfilled, "gate exposes U3DS verdict");

            var incompleteGate = QualificationEvidenceGate.Evaluate(RuntimeEvidencePackage.Create("pkg-incomplete", a, Start, "tester", new[] { sp }, new[] { artifact }), a, QualificationPolicy.Default);
            Assert(incompleteGate.Status == QualificationEvidenceGateStatus.QualificationIncomplete, "missing required environments remain incomplete");
            var invalidGate = QualificationEvidenceGate.Evaluate(empty, a, QualificationPolicy.Default);
            Assert(invalidGate.Status == QualificationEvidenceGateStatus.EvidencePackageInvalid, "invalid package is fail-closed before qualification");
            var invalidPolicyGate = QualificationEvidenceGate.Evaluate(fullPackage, a, null);
            Assert(invalidPolicyGate.Status == QualificationEvidenceGateStatus.InvalidPolicy, "null policy is fail-closed without throwing");
        }
        private static RuntimeEvidencePackage FullPackage(CandidateBuildDescriptor candidate)
        {
            var cases = new[]
            {
                EvidenceWithReference("case-sp-full", EvidenceEnvironmentRole.SinglePlayer, candidate, 0, 10, candidate.DllSha256, "sp.log"),
                EvidenceWithReference("case-p2p-full", EvidenceEnvironmentRole.SteamP2PHost, candidate, 20, 30, candidate.DllSha256, "p2p-host.log"),
                EvidenceWithReference("case-p2p-full", EvidenceEnvironmentRole.SteamP2PClient, candidate, 21, 29, candidate.DllSha256, "p2p-client.log"),
                EvidenceWithReference("case-u3ds-full", EvidenceEnvironmentRole.U3dsHeadless, candidate, 40, 50, candidate.DllSha256, "u3ds.log")
            };
            var artifacts = new[]
            {
                RuntimeEvidenceArtifact.Create("sp.log", DigestA, 10, "log"),
                RuntimeEvidenceArtifact.Create("p2p-host.log", DigestA, 10, "log"),
                RuntimeEvidenceArtifact.Create("p2p-client.log", DigestA, 10, "log"),
                RuntimeEvidenceArtifact.Create("u3ds.log", DigestA, 10, "log")
            };
            return RuntimeEvidencePackage.Create("pkg-full", candidate, Start, "tester", cases, artifacts);
        }
        private static EvidenceCase Evidence(string caseId, EvidenceEnvironmentRole role, CandidateBuildDescriptor candidate, int startMinutes, int endMinutes, string hash)
        { return EvidenceWithReference(caseId, role, candidate, startMinutes, endMinutes, hash, "evidence.log"); }
        private static EvidenceCase EvidenceWithReference(string caseId, EvidenceEnvironmentRole role, CandidateBuildDescriptor candidate, int startMinutes, int endMinutes, string hash, string reference)
        { var evidenceCandidate = hash == candidate.DllSha256 ? candidate : CandidateBuildDescriptor.Create(candidate.SourceSnapshotId, candidate.DefinitionSetDigest, candidate.ArtifactPayloadDigest, candidate.ToolchainIdentity, candidate.ClientReferenceSetId, candidate.U3dsReferenceSetId, hash); return EvidenceCase.Create(evidenceCandidate, caseId, role, "machine-" + role, "Unturned-test", "BepInEx-test", "transport-test", "deployment-test", "command-test", Start.AddMinutes(startMinutes), Start.AddMinutes(endMinutes), reference, DigestA, "tester", "diagnostic"); }
        private static void AssertThrows(Action action, string message) { try { action(); throw new InvalidOperationException(message + " (not rejected)"); } catch (ArgumentException) { } }
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
