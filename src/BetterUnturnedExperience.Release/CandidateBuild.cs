using System;
using System.Security.Cryptography;
using System.Text;

namespace BetterUnturnedExperience.Release
{
    public sealed class CandidateBuildDescriptor
    {
        private CandidateBuildDescriptor(string sourceSnapshotId, string definitionSetDigest, string artifactPayloadDigest, string toolchainIdentity, string clientReferenceSetId, string u3dsReferenceSetId, string dllSha256)
        {
            SourceSnapshotId = Required(sourceSnapshotId, nameof(sourceSnapshotId));
            DefinitionSetDigest = DigestValue(definitionSetDigest, nameof(definitionSetDigest));
            ArtifactPayloadDigest = DigestValue(artifactPayloadDigest, nameof(artifactPayloadDigest));
            ToolchainIdentity = RequiredIdentity(toolchainIdentity, nameof(toolchainIdentity));
            ClientReferenceSetId = RequiredIdentity(clientReferenceSetId, nameof(clientReferenceSetId));
            U3dsReferenceSetId = RequiredIdentity(u3dsReferenceSetId, nameof(u3dsReferenceSetId));
            DllSha256 = DigestValue(dllSha256, nameof(dllSha256));
            BuildIdentity = Digest(Canonical(SourceSnapshotId, DefinitionSetDigest, ArtifactPayloadDigest, ToolchainIdentity, ClientReferenceSetId, U3dsReferenceSetId));
        }
        public string SourceSnapshotId { get; }
        public string DefinitionSetDigest { get; }
        public string ArtifactPayloadDigest { get; }
        public string ToolchainIdentity { get; }
        public string ClientReferenceSetId { get; }
        public string U3dsReferenceSetId { get; }
        public string BuildIdentity { get; }
        public string DllSha256 { get; }
        public static CandidateBuildDescriptor Create(string sourceSnapshotId, string definitionSetDigest, string artifactPayloadDigest, string toolchainIdentity, string clientReferenceSetId, string u3dsReferenceSetId, string dllSha256)
        { return new CandidateBuildDescriptor(sourceSnapshotId, definitionSetDigest, artifactPayloadDigest, toolchainIdentity, clientReferenceSetId, u3dsReferenceSetId, dllSha256); }
        internal static string DigestValue(string value, string name)
        {
            var canonical = Required(value, name).ToUpperInvariant();
            if (canonical.Length != 64) throw new ArgumentException("Value must be 64 ASCII hexadecimal characters.", name);
            for (var index = 0; index < canonical.Length; index++) if (!IsHex(canonical[index])) throw new ArgumentException("Value must be ASCII hexadecimal.", name);
            return canonical;
        }
        internal static string RequiredIdentity(string value, string name)
        { var canonical = Required(value, name); if (canonical != canonical.Trim()) throw new ArgumentException("Identity cannot contain surrounding whitespace.", name); return canonical; }
        private static string Canonical(params string[] values) { var builder = new StringBuilder(); foreach (var value in values) builder.Append(value.Length).Append(':').Append(value).Append('\n'); return builder.ToString(); }
        private static string Digest(string value) { using (var sha = SHA256.Create()) { var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value)); var builder = new StringBuilder(bytes.Length * 2); foreach (var item in bytes) builder.Append(item.ToString("X2")); return builder.ToString(); } }
        private static string Required(string value, string name) { if (value == null) throw new ArgumentNullException(name); if (value.Length == 0) throw new ArgumentException("Value is required.", name); return value; }
        private static bool IsHex(char value) { return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F'); }
    }
}
