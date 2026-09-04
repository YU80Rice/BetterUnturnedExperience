using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BetterUnturnedExperience.Release;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QualificationGateRunner
{
    /// <summary>
    /// DEV-V2-07 evidence kit runner. Two modes:
    ///
    ///   identity --libs DIR --dll FILE --snapshot HASH --case-id ID --candidate-build NAME --out FILE
    ///     Computes the candidate identity: Libs reference-set digest, the
    ///     official feature-definition set digest, the DLL artifact payload
    ///     digest, then the CandidateBuildDescriptor BuildIdentity, and writes
    ///     candidate.json. Exit 0 on success.
    ///
    ///   gate --package DIR --candidate FILE
    ///     Reads cases/*/case.json under the package dir, hashes every evidence
    ///     artifact on disk, builds the RuntimeEvidencePackage and runs
    ///     RuntimeEvidencePackageValidator + QualificationEvidenceGate.
    ///     Exit codes: 0 = TechnicallyQualified, 2 = package invalid,
    ///     3 = qualification incomplete, 1 = usage/input error.
    /// </summary>
    internal static class Program
    {
        private const string ToolchainIdentity = "MSBuild-18.9.0.32302|.NETFramework-4.7.2|CSharp-10";

        // The official feature definitions compiled into BetterUnturnedExperience.dll
        // (OfficialFeatureRegistration.cs + NetworkModuleFeatureRegistration.cs).
        // Digest256 text = 4 x X16 parts (the Core linker's DigestText convention).
        // Payload hex is the UTF-8 encoding of the payload text constant.
        // DEV-V2-10: the network payload digest is CORRECTED (the baked values
        // never matched the payload's SHA-256 — real machine BUE-REG-004) and
        // the V1 compat facet joins the official definition set
        // (spec-V2-phase1 L74); both are computed in production
        // (NetworkModuleFeatureRegistration.ComputePayloadDigest).
        private sealed class DefinitionEntry
        {
            public string FeatureId;
            public uint Version;
            public string Name;
            public ulong D0, D1, D2, D3;
            public ulong P0, P1, P2, P3;
            public string PayloadText;
        }

        private static readonly DefinitionEntry[] OfficialDefinitions =
        {
            new DefinitionEntry
            {
                FeatureId = "io.github.yu80rice.bue.better-item-interaction", Version = 1, Name = "bue-better-item-interaction-v1",
                D0 = 1UL, D1 = 0UL, D2 = 0UL, D3 = 14UL,
                P0 = 4239661619294337961UL, P1 = 6084291702436199631UL, P2 = 12736748317259485579UL, P3 = 11302160818330435267UL,
                PayloadText = "BUE-BII-V1"
            },
            new DefinitionEntry
            {
                FeatureId = "io.github.yu80rice.bue.network", Version = 1, Name = "bue-network-v1",
                D0 = 1UL, D1 = 0UL, D2 = 0UL, D3 = 16UL,
                P0 = 3513462337615412056UL, P1 = 9811700189201168883UL, P2 = 15092958732532668990UL, P3 = 2856094613919016762UL,
                PayloadText = "BUE-NET-V1"
            },
            new DefinitionEntry
            {
                FeatureId = "io.github.yu80rice.bue.network.v1compat", Version = 1, Name = "bue-network-v1compat-v1",
                D0 = 1UL, D1 = 0UL, D2 = 0UL, D3 = 17UL,
                P0 = 8807006363499442750UL, P1 = 313719394671523987UL, P2 = 6741820503722070260UL, P3 = 9757911435375117733UL,
                PayloadText = "BUE-NET-V1C"
            }
        };

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && args[0] == "identity") return RunIdentity(args);
                if (args.Length > 0 && args[0] == "gate") return RunGate(args);
                Console.WriteLine("usage: QualificationGateRunner identity --libs DIR --dll FILE --snapshot HASH --case-id ID --candidate-build NAME --out FILE");
                Console.WriteLine("       QualificationGateRunner gate --package DIR --candidate FILE");
                return 1;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("runner-fault errorType=" + error.GetType().Name + " message=" + error.Message);
                return 1;
            }
        }

        private static int RunIdentity(string[] args)
        {
            var libs = RequireValue(args, "--libs");
            var dll = RequireValue(args, "--dll");
            var snapshot = RequireValue(args, "--snapshot");
            var caseId = RequireValue(args, "--case-id");
            var candidateBuild = RequireValue(args, "--candidate-build");
            var outFile = RequireValue(args, "--out");

            var dllBytes = File.ReadAllBytes(dll);
            var dllSha256 = Sha256Hex(dllBytes);
            var definitionSetDigest = ComputeDefinitionSetDigest();
            var referenceSetId = ComputeReferenceSetId(libs);

            var descriptor = CandidateBuildDescriptor.Create(snapshot, definitionSetDigest, dllSha256, ToolchainIdentity, referenceSetId, referenceSetId, dllSha256);
            var document = new JObject
            {
                ["candidateBuild"] = candidateBuild,
                ["caseId"] = caseId,
                ["sourceSnapshotId"] = snapshot,
                ["definitionSetDigest"] = definitionSetDigest,
                ["artifactPayloadDigest"] = dllSha256,
                ["toolchainIdentity"] = ToolchainIdentity,
                ["clientReferenceSetId"] = referenceSetId,
                ["u3dsReferenceSetId"] = referenceSetId,
                ["dllSha256"] = dllSha256,
                ["dllLengthBytes"] = dllBytes.Length,
                ["buildIdentity"] = descriptor.BuildIdentity
            };
            File.WriteAllText(outFile, document.ToString(Newtonsoft.Json.Formatting.Indented) + Environment.NewLine, new UTF8Encoding(false));
            Console.WriteLine("identity candidateBuild=" + candidateBuild);
            Console.WriteLine("identity dllSha256=" + dllSha256);
            Console.WriteLine("identity buildIdentity=" + descriptor.BuildIdentity);
            Console.WriteLine("identity definitionSetDigest=" + definitionSetDigest);
            Console.WriteLine("identity referenceSetId=" + referenceSetId);
            Console.WriteLine("identity written=" + Path.GetFullPath(outFile));
            return 0;
        }

        private static int RunGate(string[] args)
        {
            var packageDir = RequireValue(args, "--package");
            var candidateFile = RequireValue(args, "--candidate");

            var candidateJson = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(candidateFile), new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            var candidate = CandidateBuildDescriptor.Create(
                (string)candidateJson["sourceSnapshotId"],
                (string)candidateJson["definitionSetDigest"],
                (string)candidateJson["artifactPayloadDigest"],
                (string)candidateJson["toolchainIdentity"],
                (string)candidateJson["clientReferenceSetId"],
                (string)candidateJson["u3dsReferenceSetId"],
                (string)candidateJson["dllSha256"]);

            var casesDir = Path.Combine(packageDir, "cases");
            if (!Directory.Exists(casesDir))
            {
                Console.Error.WriteLine("gate-error missing cases directory: " + casesDir);
                return 1;
            }

            var cases = new List<EvidenceCase>();
            var artifacts = new List<RuntimeEvidenceArtifact>();
            string collector = null;
            foreach (var caseDir in Directory.GetDirectories(casesDir))
            {
                var caseFile = Path.Combine(caseDir, "case.json");
                if (!File.Exists(caseFile))
                {
                    Console.Error.WriteLine("gate-error missing case.json in " + caseDir);
                    return 1;
                }
                var json = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(caseFile), new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
                if (collector == null) collector = RequiredText(json, "collector");
                var caseId = RequiredText(json, "caseId");
                var role = (EvidenceEnvironmentRole)Enum.Parse(typeof(EvidenceEnvironmentRole), RequiredText(json, "role"), true);

                var evidenceLog = RequiredText(json, "evidenceLog");
                var evidencePath = Path.Combine(caseDir, evidenceLog.Replace('/', Path.DirectorySeparatorChar));
                artifacts.Add(RuntimeEvidenceArtifact.Create(
                    caseDir.Substring(packageDir.Length).Replace('\\', '/').TrimStart('/') + "/" + evidenceLog,
                    Sha256Hex(File.ReadAllBytes(evidencePath)),
                    new FileInfo(evidencePath).Length,
                    "evidence.log"));
                foreach (var extra in (JArray)json["extraArtifacts"])
                {
                    var extraPath = Path.Combine(caseDir, ((string)extra["path"]).Replace('/', Path.DirectorySeparatorChar));
                    artifacts.Add(RuntimeEvidenceArtifact.Create(
                        caseDir.Substring(packageDir.Length).Replace('\\', '/').TrimStart('/') + "/" + (string)extra["path"],
                        Sha256Hex(File.ReadAllBytes(extraPath)),
                        new FileInfo(extraPath).Length,
                        (string)extra["type"] ?? "supplement"));
                }

                cases.Add(EvidenceCase.Create(
                    candidate,
                    caseId,
                    role,
                    RequiredText(json, "environmentFingerprint"),
                    RequiredText(json, "gameVersion"),
                    RequiredText(json, "bepInExVersion"),
                    RequiredText(json, "transportVersion"),
                    RequiredText(json, "deploymentSource"),
                    RequiredText(json, "command"),
                    ParseUtc(RequiredText(json, "startedUtc")),
                    ParseUtc(RequiredText(json, "endedUtc")),
                    caseDir.Substring(packageDir.Length).Replace('\\', '/').TrimStart('/') + "/" + evidenceLog,
                    Sha256Hex(File.ReadAllBytes(evidencePath)),
                    RequiredText(json, "collector"),
                    RequiredText(json, "diagnosticSummary")));
            }

            var package = RuntimeEvidencePackage.Create(new DirectoryInfo(packageDir).Name, candidate, DateTime.UtcNow, collector, cases, artifacts);
            var validation = RuntimeEvidencePackageValidator.Validate(package, candidate);
            Console.WriteLine("package packageId=" + package.PackageId);
            Console.WriteLine("package canonicalDigest=" + package.CanonicalDigest);
            Console.WriteLine("validation isValid=" + validation.IsValid + " codes=" + string.Join(",", validation.Codes));
            if (!validation.IsValid)
            {
                Console.WriteLine("verdict status=EvidencePackageInvalid");
                return 2;
            }

            var gate = QualificationEvidenceGate.Evaluate(package, candidate, QualificationPolicy.Default);
            Console.WriteLine("verdict status=" + gate.Status + " buildIdentity=" + gate.BuildIdentity + " dllSha256=" + gate.DllSha256);
            foreach (var roleValue in Enum.GetValues(typeof(EvidenceEnvironmentRole)))
                Console.WriteLine("verdict role=" + roleValue + " = " + gate.Qualification.For((EvidenceEnvironmentRole)roleValue));
            return gate.IsTechnicallyQualified ? 0 : 3;
        }

        /// <summary>
        /// Definition-set digest recipe (documented in the DEV-V2-07 delivery
        /// report): official FeatureDefinitionArtifact entries sorted Ordinal by
        /// FeatureId, each rendered as
        /// featureId|version|name|defDigestText|payloadDigestText|payloadHex
        /// (Digest256 text = 4 x X16 parts; payload hex = UTF-8 bytes), joined
        /// with '\n', hashed with SHA-256, uppercase hex.
        /// </summary>
        private static string ComputeDefinitionSetDigest()
        {
            var sorted = (DefinitionEntry[])OfficialDefinitions.Clone();
            Array.Sort(sorted, (left, right) => StringComparer.Ordinal.Compare(left.FeatureId, right.FeatureId));
            var builder = new StringBuilder();
            foreach (var entry in sorted)
            {
                builder.Append(entry.FeatureId).Append('|')
                    .Append(entry.Version).Append('|')
                    .Append(entry.Name).Append('|')
                    .Append(DigestText(entry.D0, entry.D1, entry.D2, entry.D3)).Append('|')
                    .Append(DigestText(entry.P0, entry.P1, entry.P2, entry.P3)).Append('|')
                    .Append(BytesToHex(Encoding.UTF8.GetBytes(entry.PayloadText))).Append('\n');
            }
            return Sha256Hex(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        /// <summary>
        /// Reference-set digest recipe: every Libs *.dll (excluding .bak* files)
        /// sorted Ordinal by file name, each rendered as name|length|sha256,
        /// joined with '\n', hashed with SHA-256, prefixed Libs-ReferenceSet-.
        /// </summary>
        private static string ComputeReferenceSetId(string libsDir)
        {
            var files = Directory.GetFiles(libsDir, "*.dll");
            var names = new List<string>(files);
            names.RemoveAll(name => name.Contains(".bak"));
            names.Sort(StringComparer.Ordinal);
            var builder = new StringBuilder();
            foreach (var name in names)
            {
                var bytes = File.ReadAllBytes(name);
                builder.Append(Path.GetFileName(name)).Append('|').Append(bytes.Length).Append('|').Append(Sha256Hex(bytes)).Append('\n');
            }
            return "Libs-ReferenceSet-" + Sha256Hex(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static string DigestText(ulong p0, ulong p1, ulong p2, ulong p3)
        {
            return p0.ToString("X16", CultureInfo.InvariantCulture) + p1.ToString("X16", CultureInfo.InvariantCulture)
                + p2.ToString("X16", CultureInfo.InvariantCulture) + p3.ToString("X16", CultureInfo.InvariantCulture);
        }

        private static string BytesToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var item in bytes) builder.Append(item.ToString("X2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string Sha256Hex(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BytesToHex(sha.ComputeHash(bytes));
        }

        private static DateTime ParseUtc(string value)
        {
            DateTime parsed;
            try { parsed = DateTime.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); }
            catch (FormatException error) { throw new FormatException("invalid UTC 'O' timestamp: '" + value + "'", error); }
            if (parsed.Kind != DateTimeKind.Utc) throw new ArgumentException("case time must be a UTC 'O' timestamp: " + value);
            return parsed;
        }

        private static string RequiredText(JObject json, string field)
        {
            var value = (string)json[field];
            if (string.IsNullOrWhiteSpace(value) || value == "TODO") throw new ArgumentException("case field '" + field + "' must be filled before gating");
            return value.Trim();
        }

        private static string RequireValue(string[] args, string name)
        {
            var index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("missing " + name + " value");
            return args[index + 1];
        }
    }
}
