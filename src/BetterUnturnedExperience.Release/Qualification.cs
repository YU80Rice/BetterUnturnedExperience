using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BetterUnturnedExperience.Release
{
    public enum EvidenceEnvironmentRole : byte { SinglePlayer, SteamP2PHost, SteamP2PClient, U3dsHeadless, U3dsClientUi }
    public enum QualificationVerdict : byte { Fulfilled, Failed, Missing, Stale, NotApplicable }

    public sealed class QualificationPolicy
    {
        private readonly IReadOnlyDictionary<EvidenceEnvironmentRole, bool> applicability;
        private QualificationPolicy(IDictionary<EvidenceEnvironmentRole, bool> values)
        { if (!values[EvidenceEnvironmentRole.U3dsHeadless]) throw new ArgumentException("U3DS headless core obligation cannot be NotApplicable.", nameof(values)); applicability = new ReadOnlyDictionary<EvidenceEnvironmentRole, bool>(new Dictionary<EvidenceEnvironmentRole, bool>(values)); }
        public static QualificationPolicy Default { get { return Create(true, true, true, true, false); } }
        public static QualificationPolicy Create(bool singlePlayer, bool p2pHost, bool p2pClient, bool u3dsHeadless, bool u3dsClientUi)
        { return new QualificationPolicy(new Dictionary<EvidenceEnvironmentRole, bool> { { EvidenceEnvironmentRole.SinglePlayer, singlePlayer }, { EvidenceEnvironmentRole.SteamP2PHost, p2pHost }, { EvidenceEnvironmentRole.SteamP2PClient, p2pClient }, { EvidenceEnvironmentRole.U3dsHeadless, u3dsHeadless }, { EvidenceEnvironmentRole.U3dsClientUi, u3dsClientUi } }); }
        internal bool Applies(EvidenceEnvironmentRole role) { return applicability[role]; }
    }

    public sealed class EvidenceCase
    {
        private EvidenceCase(CandidateBuildDescriptor candidate, string caseId, EvidenceEnvironmentRole role, string environmentFingerprint, string gameVersion, string bepInExVersion, string transportVersion, string deploymentSource, string command, DateTime startedUtc, DateTime endedUtc, string evidenceReference, string evidenceDigest, string collector, string diagnosticSummary)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (startedUtc.Kind != DateTimeKind.Utc || endedUtc.Kind != DateTimeKind.Utc || endedUtc <= startedUtc) throw new ArgumentException("Evidence time window must be UTC and have positive duration.");
            Candidate = candidate; CaseId = Required(caseId, nameof(caseId)); Role = role; EnvironmentFingerprint = Required(environmentFingerprint, nameof(environmentFingerprint)); GameVersion = Required(gameVersion, nameof(gameVersion)); BepInExVersion = Required(bepInExVersion, nameof(bepInExVersion)); TransportVersion = Required(transportVersion, nameof(transportVersion)); DeploymentSource = Required(deploymentSource, nameof(deploymentSource)); Command = Required(command, nameof(command)); StartedUtc = startedUtc; EndedUtc = endedUtc; EvidenceReference = Required(evidenceReference, nameof(evidenceReference)); EvidenceDigest = CandidateBuildDescriptor.DigestValue(evidenceDigest, nameof(evidenceDigest)); Collector = Required(collector, nameof(collector)); DiagnosticSummary = Required(diagnosticSummary, nameof(diagnosticSummary));
        }
        public CandidateBuildDescriptor Candidate { get; }
        public string CaseId { get; }
        public EvidenceEnvironmentRole Role { get; }
        public string EnvironmentFingerprint { get; }
        public string GameVersion { get; }
        public string BepInExVersion { get; }
        public string TransportVersion { get; }
        public string DeploymentSource { get; }
        public string Command { get; }
        public DateTime StartedUtc { get; }
        public DateTime EndedUtc { get; }
        public string EvidenceReference { get; }
        public string EvidenceDigest { get; }
        public string Collector { get; }
        public string DiagnosticSummary { get; }
        public string BuildIdentity { get { return Candidate.BuildIdentity; } }
        public string DllSha256 { get { return Candidate.DllSha256; } }
        public static EvidenceCase Create(CandidateBuildDescriptor candidate, string caseId, EvidenceEnvironmentRole role, string environmentFingerprint, string gameVersion, string bepInExVersion, string transportVersion, string deploymentSource, string command, DateTime startedUtc, DateTime endedUtc, string evidenceReference, string evidenceDigest, string collector, string diagnosticSummary)
        { return new EvidenceCase(candidate, caseId, role, environmentFingerprint, gameVersion, bepInExVersion, transportVersion, deploymentSource, command, startedUtc, endedUtc, evidenceReference, evidenceDigest, collector, diagnosticSummary); }
        public string SerializeCanonical()
        { var values = new[] { CaseId, BuildIdentity, Candidate.DefinitionSetDigest, Candidate.ArtifactPayloadDigest, Candidate.ToolchainIdentity, Candidate.ClientReferenceSetId, Candidate.U3dsReferenceSetId, DllSha256, ((byte)Role).ToString(), EnvironmentFingerprint, GameVersion, BepInExVersion, TransportVersion, DeploymentSource, Command, StartedUtc.ToString("O"), EndedUtc.ToString("O"), EvidenceReference, EvidenceDigest, Collector, DiagnosticSummary }; var builder = new StringBuilder(); foreach (var value in values) builder.Append(value.Length).Append(':').Append(value).Append('\n'); return builder.ToString(); }
        private static string Required(string value, string name) { if (value == null) throw new ArgumentNullException(name); if (value.Length == 0 || value != value.Trim()) throw new ArgumentException("Value is required and cannot have surrounding whitespace.", name); return value; }
    }

    public sealed class QualificationResult
    {
        internal QualificationResult(IReadOnlyDictionary<EvidenceEnvironmentRole, QualificationVerdict> verdicts) { Verdicts = verdicts; }
        public IReadOnlyDictionary<EvidenceEnvironmentRole, QualificationVerdict> Verdicts { get; }
        public QualificationVerdict For(EvidenceEnvironmentRole role) { QualificationVerdict result; return Verdicts.TryGetValue(role, out result) ? result : QualificationVerdict.Missing; }
    }

    public static class QualificationEvaluator
    {
        private static readonly EvidenceEnvironmentRole[] Roles = { EvidenceEnvironmentRole.SinglePlayer, EvidenceEnvironmentRole.SteamP2PHost, EvidenceEnvironmentRole.SteamP2PClient, EvidenceEnvironmentRole.U3dsHeadless, EvidenceEnvironmentRole.U3dsClientUi };
        public static QualificationResult Evaluate(IEnumerable<EvidenceCase> evidence, CandidateBuildDescriptor candidate, QualificationPolicy policy)
        {
            if (evidence == null) throw new ArgumentNullException(nameof(evidence)); if (candidate == null) throw new ArgumentNullException(nameof(candidate)); if (policy == null) throw new ArgumentNullException(nameof(policy));
            var all = new List<EvidenceCase>(evidence); var result = new Dictionary<EvidenceEnvironmentRole, QualificationVerdict>(); foreach (var role in Roles) result[role] = EvaluateRole(all, role, candidate, policy); PairP2p(all, candidate, result); return new QualificationResult(new ReadOnlyDictionary<EvidenceEnvironmentRole, QualificationVerdict>(result));
        }
        private static QualificationVerdict EvaluateRole(List<EvidenceCase> all, EvidenceEnvironmentRole role, CandidateBuildDescriptor candidate, QualificationPolicy policy)
        {
            if (!policy.Applies(role)) return QualificationVerdict.NotApplicable; var entries = all.FindAll(item => item.Role == role); if (entries.Count == 0) return QualificationVerdict.Missing; var matching = entries.FindAll(item => SameCandidate(item.Candidate, candidate)); if (matching.Count == 0) return QualificationVerdict.Stale; return matching.Exists(item => item.DllSha256 == candidate.DllSha256) ? QualificationVerdict.Fulfilled : QualificationVerdict.Stale;
        }
        private static void PairP2p(List<EvidenceCase> all, CandidateBuildDescriptor candidate, Dictionary<EvidenceEnvironmentRole, QualificationVerdict> result)
        {
            if (result[EvidenceEnvironmentRole.SteamP2PHost] != QualificationVerdict.Fulfilled || result[EvidenceEnvironmentRole.SteamP2PClient] != QualificationVerdict.Fulfilled) return; var hosts = all.FindAll(item => item.Role == EvidenceEnvironmentRole.SteamP2PHost && SameCandidate(item.Candidate, candidate) && item.DllSha256 == candidate.DllSha256); var clients = all.FindAll(item => item.Role == EvidenceEnvironmentRole.SteamP2PClient && SameCandidate(item.Candidate, candidate) && item.DllSha256 == candidate.DllSha256); foreach (var host in hosts) foreach (var client in clients) if (host.CaseId == client.CaseId && Overlaps(host, client)) return; result[EvidenceEnvironmentRole.SteamP2PHost] = QualificationVerdict.Failed; result[EvidenceEnvironmentRole.SteamP2PClient] = QualificationVerdict.Failed;
        }
        private static bool SameCandidate(CandidateBuildDescriptor left, CandidateBuildDescriptor right) { return left.BuildIdentity == right.BuildIdentity && left.DefinitionSetDigest == right.DefinitionSetDigest && left.ArtifactPayloadDigest == right.ArtifactPayloadDigest && left.ToolchainIdentity == right.ToolchainIdentity && left.ClientReferenceSetId == right.ClientReferenceSetId && left.U3dsReferenceSetId == right.U3dsReferenceSetId; }
        private static bool Overlaps(EvidenceCase left, EvidenceCase right) { return left.StartedUtc < right.EndedUtc && right.StartedUtc < left.EndedUtc; }
    }
}
