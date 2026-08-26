using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace BetterUnturnedExperience.Release
{
    public enum EvidencePackageValidationCode : byte
    {
        MissingPackage,
        CandidateMismatch,
        DuplicateCaseId,
        DuplicateArtifactPath,
        MissingArtifactReference,
        EmptyPackage,
        ArtifactDigestMismatch,
        NullEvidenceCase,
        NullArtifact,
        InvalidArtifactDigest,
        Valid
    }

    public sealed class RuntimeEvidenceArtifact
    {
        private RuntimeEvidenceArtifact(string relativePath, string sha256, long lengthBytes, string evidenceType)
        {
            RelativePath = ValidatePath(relativePath);
            Sha256 = CandidateBuildDescriptor.DigestValue(sha256, nameof(sha256));
            if (lengthBytes < 0) throw new ArgumentOutOfRangeException(nameof(lengthBytes));
            LengthBytes = lengthBytes;
            EvidenceType = Required(evidenceType, nameof(evidenceType));
        }

        public string RelativePath { get; }
        public string Sha256 { get; }
        public long LengthBytes { get; }
        public string EvidenceType { get; }

        public static RuntimeEvidenceArtifact Create(string relativePath, string sha256, long lengthBytes, string evidenceType)
        { return new RuntimeEvidenceArtifact(relativePath, sha256, lengthBytes, evidenceType); }

        private static string ValidatePath(string value)
        {
            var path = Required(value, nameof(value)).Replace('\\', '/');
            var segments = path.Split('/');
            if (path.StartsWith("/", StringComparison.Ordinal) || path.IndexOf(':') >= 0 || path == "." || Array.Exists(segments, segment => segment == "." || segment == ".." || segment.Length == 0))
                throw new ArgumentException("Artifact path must be a safe relative path.", nameof(value));
            return path;
        }

        private static string Required(string value, string name)
        {
            if (value == null) throw new ArgumentNullException(name);
            if (value.Length == 0 || value != value.Trim()) throw new ArgumentException("Value is required and cannot have surrounding whitespace.", name);
            return value;
        }
    }

    public sealed class RuntimeEvidencePackage
    {
        private RuntimeEvidencePackage(string packageId, CandidateBuildDescriptor candidate, DateTime createdUtc, string collector, IEnumerable<EvidenceCase> cases, IEnumerable<RuntimeEvidenceArtifact> artifacts)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (createdUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Package timestamp must be UTC.", nameof(createdUtc));
            PackageId = Required(packageId, nameof(packageId));
            Candidate = candidate;
            CreatedUtc = createdUtc;
            Collector = Required(collector, nameof(collector));
            Cases = new ReadOnlyCollection<EvidenceCase>(new List<EvidenceCase>(cases ?? throw new ArgumentNullException(nameof(cases))));
            Artifacts = new ReadOnlyCollection<RuntimeEvidenceArtifact>(new List<RuntimeEvidenceArtifact>(artifacts ?? throw new ArgumentNullException(nameof(artifacts))));
        }

        public string PackageId { get; }
        public CandidateBuildDescriptor Candidate { get; }
        public DateTime CreatedUtc { get; }
        public string Collector { get; }
        public IReadOnlyList<EvidenceCase> Cases { get; }
        public IReadOnlyList<RuntimeEvidenceArtifact> Artifacts { get; }
        public string CanonicalDigest { get { return Digest256(SerializeCanonical()); } }

        public static RuntimeEvidencePackage Create(string packageId, CandidateBuildDescriptor candidate, DateTime createdUtc, string collector, IEnumerable<EvidenceCase> cases, IEnumerable<RuntimeEvidenceArtifact> artifacts)
        { return new RuntimeEvidencePackage(packageId, candidate, createdUtc, collector, cases, artifacts); }

        public string SerializeCanonical()
        {
            var values = new List<string> { PackageId, Candidate.BuildIdentity, Candidate.DllSha256, CreatedUtc.ToString("O"), Collector };
            var sortedCases = new List<EvidenceCase>(Cases); sortedCases.Sort((left, right) => StringComparer.Ordinal.Compare(left.CaseId, right.CaseId));
            foreach (var item in sortedCases) values.Add(item.SerializeCanonical());
            var sortedArtifacts = new List<RuntimeEvidenceArtifact>(Artifacts); sortedArtifacts.Sort((left, right) => StringComparer.Ordinal.Compare(left.RelativePath, right.RelativePath));
            foreach (var item in sortedArtifacts) values.Add(item.RelativePath + "|" + item.Sha256 + "|" + item.LengthBytes + "|" + item.EvidenceType);
            var builder = new StringBuilder(); foreach (var value in values) builder.Append(value.Length).Append(':').Append(value).Append('\n'); return builder.ToString();
        }

        private static string Required(string value, string name)
        {
            if (value == null) throw new ArgumentNullException(name);
            if (value.Length == 0 || value != value.Trim()) throw new ArgumentException("Value is required and cannot have surrounding whitespace.", name);
            return value;
        }

        private static string Digest256(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) builder.Append(item.ToString("X2"));
                return builder.ToString();
            }
        }
    }

    public sealed class EvidencePackageValidationResult
    {
        internal EvidencePackageValidationResult(bool isValid, IReadOnlyList<EvidencePackageValidationCode> codes) { IsValid = isValid; Codes = codes; }
        public bool IsValid { get; }
        public IReadOnlyList<EvidencePackageValidationCode> Codes { get; }
    }

    public static class RuntimeEvidencePackageValidator
    {
        public static EvidencePackageValidationResult Validate(RuntimeEvidencePackage package, CandidateBuildDescriptor candidate)
        {
            var codes = new List<EvidencePackageValidationCode>();
            if (package == null) { codes.Add(EvidencePackageValidationCode.MissingPackage); return Result(codes); }
            if (candidate == null) { codes.Add(EvidencePackageValidationCode.CandidateMismatch); return Result(codes); }
            if (package.Cases.Count == 0 || package.Artifacts.Count == 0) codes.Add(EvidencePackageValidationCode.EmptyPackage);
            if (!SameCandidate(package.Candidate, candidate)) codes.Add(EvidencePackageValidationCode.CandidateMismatch);
            var caseRoles = new Dictionary<string, HashSet<EvidenceEnvironmentRole>>(StringComparer.Ordinal);
            foreach (var item in package.Cases)
            {
                if (item == null) { codes.Add(EvidencePackageValidationCode.NullEvidenceCase); continue; }
                if (!SameCandidate(item.Candidate, candidate) || item.DllSha256 != candidate.DllSha256) codes.Add(EvidencePackageValidationCode.CandidateMismatch);
                HashSet<EvidenceEnvironmentRole> roles;
                if (!caseRoles.TryGetValue(item.CaseId, out roles))
                {
                    roles = new HashSet<EvidenceEnvironmentRole>();
                    caseRoles.Add(item.CaseId, roles);
                }
                var allowedP2PPair = roles.Count == 1 && ((roles.Contains(EvidenceEnvironmentRole.SteamP2PHost) && item.Role == EvidenceEnvironmentRole.SteamP2PClient) || (roles.Contains(EvidenceEnvironmentRole.SteamP2PClient) && item.Role == EvidenceEnvironmentRole.SteamP2PHost));
                if (roles.Contains(item.Role) || (roles.Count > 0 && !allowedP2PPair))
                    codes.Add(EvidencePackageValidationCode.DuplicateCaseId);
                roles.Add(item.Role);
            }
            var artifactPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artifact in package.Artifacts)
            {
                if (artifact == null) { codes.Add(EvidencePackageValidationCode.NullArtifact); continue; }
                if (!artifactPaths.Add(artifact.RelativePath)) codes.Add(EvidencePackageValidationCode.DuplicateArtifactPath);
                try { CandidateBuildDescriptor.DigestValue(artifact.Sha256, nameof(artifact.Sha256)); } catch (ArgumentException) { codes.Add(EvidencePackageValidationCode.InvalidArtifactDigest); }
            }
            foreach (var item in package.Cases)
            {
                if (item == null) continue;
                var reference = item.EvidenceReference.Replace('\\', '/');
                if (!artifactPaths.Contains(reference)) codes.Add(EvidencePackageValidationCode.MissingArtifactReference);
                else
                {
                    RuntimeEvidenceArtifact referenced = null;
                    foreach (var artifact in package.Artifacts)
                        if (artifact != null && artifact.RelativePath == reference) { referenced = artifact; break; }
                    if (referenced != null && referenced.Sha256 != item.EvidenceDigest) codes.Add(EvidencePackageValidationCode.ArtifactDigestMismatch);
                }
            }
            return Result(codes);
        }

        private static EvidencePackageValidationResult Result(List<EvidencePackageValidationCode> codes)
        {
            if (codes.Count == 0) codes.Add(EvidencePackageValidationCode.Valid);
            codes.Sort((left, right) => ((byte)left).CompareTo((byte)right));
            return new EvidencePackageValidationResult(codes.Count == 1 && codes[0] == EvidencePackageValidationCode.Valid, new ReadOnlyCollection<EvidencePackageValidationCode>(codes));
        }
        private static bool SameCandidate(CandidateBuildDescriptor left, CandidateBuildDescriptor right)
        { return left != null && right != null && left.BuildIdentity == right.BuildIdentity && left.DllSha256 == right.DllSha256 && left.DefinitionSetDigest == right.DefinitionSetDigest && left.ArtifactPayloadDigest == right.ArtifactPayloadDigest && left.ToolchainIdentity == right.ToolchainIdentity && left.ClientReferenceSetId == right.ClientReferenceSetId && left.U3dsReferenceSetId == right.U3dsReferenceSetId; }
    }
}
