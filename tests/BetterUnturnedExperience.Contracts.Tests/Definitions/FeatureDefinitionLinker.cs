// DEV-V6-03 (V6-T4 Q5)：治理链接管线退役——本文件逐字迁出
// src/BetterUnturnedExperience.Core/Definitions/（玩家合并集工程），由本测试工程自行编译，
// 玩家 DLL 构建面不再包含链接器源。运行时行为本就零调用，故无行为变更；命名空间与类型名
// 逐字保留（退役名录：CONTEXT 治理词条 / V6-R1 清单 / audit 皆按此名索引）。测试面仍是
// 治理段的唯一消费者（Program.cs 链接管线断言）。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Definitions
{
    public enum LinkStatus : byte { LinkSucceeded, LinkFailed }

    public sealed class FeatureDefinitionFragment
    {
        public FeatureId Feature { get; }
        public string FragmentKind { get; }
        public ushort SchemaVersion { get; }
        public Digest256 SourceDigest { get; }
        public IReadOnlyList<FeatureId> RequiredFeatures { get; }

        public FeatureDefinitionFragment()
            : this(new FeatureId(string.Empty), string.Empty, 0, new Digest256(), new FeatureId[0]) { }

        public FeatureDefinitionFragment(FeatureId feature, string fragmentKind, ushort schemaVersion, Digest256 sourceDigest, IEnumerable<FeatureId> requiredFeatures)
        {
            Feature = feature;
            FragmentKind = fragmentKind ?? string.Empty;
            SchemaVersion = schemaVersion;
            SourceDigest = sourceDigest;
            RequiredFeatures = new ReadOnlyCollection<FeatureId>((requiredFeatures ?? Enumerable.Empty<FeatureId>()).ToArray());
        }
    }

    public sealed class CompiledFeatureRecord
    {
        internal CompiledFeatureRecord(FeatureId feature, IReadOnlyList<string> fragmentKinds, Digest256 definitionSetDigest)
        {
            Feature = feature;
            FragmentKinds = fragmentKinds;
            DefinitionSetDigest = definitionSetDigest;
        }

        public FeatureId Feature { get; }
        public IReadOnlyList<string> FragmentKinds { get; }
        public Digest256 DefinitionSetDigest { get; }
    }

    public sealed class CompiledIdentityCatalog
    {
        private readonly IReadOnlyDictionary<string, CompiledFeatureRecord> records;

        public CompiledIdentityCatalog(IEnumerable<CompiledFeatureRecord> records, Digest256 definitionSetDigest)
        {
            var list = (records ?? Enumerable.Empty<CompiledFeatureRecord>()).ToArray();
            var map = new Dictionary<string, CompiledFeatureRecord>(StringComparer.Ordinal);
            foreach (var record in list)
            {
                if (record == null || string.IsNullOrEmpty(record.Feature.Value)) throw new ArgumentException("Catalog record identity is required.", nameof(records));
                if (map.ContainsKey(record.Feature.Value)) throw new ArgumentException("Catalog feature identity is duplicated.", nameof(records));
                map.Add(record.Feature.Value, record);
            }
            this.records = new ReadOnlyDictionary<string, CompiledFeatureRecord>(map);
            Features = new ReadOnlyCollection<CompiledFeatureRecord>(list);
            DefinitionSetDigest = definitionSetDigest;
        }

        public IReadOnlyList<CompiledFeatureRecord> Features { get; }
        public Digest256 DefinitionSetDigest { get; }

        public bool TryGet(FeatureId feature, out CompiledFeatureRecord record)
        {
            return records.TryGetValue(feature.Value ?? string.Empty, out record);
        }
    }

    public sealed class LinkResult
    {
        internal LinkResult(LinkStatus status, CompiledIdentityCatalog catalog, IReadOnlyList<string> diagnostics)
        {
            Status = status;
            Catalog = catalog;
            Diagnostics = diagnostics;
        }

        public LinkStatus Status { get; }
        public CompiledIdentityCatalog Catalog { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    public sealed class FeatureDefinitionLinker
    {
        public const ushort SupportedFragmentSchemaVersion = 1;

        public LinkResult Link(IEnumerable<FeatureDefinitionFragment> source)
        {
            var fragments = (source ?? Enumerable.Empty<FeatureDefinitionFragment>()).ToArray();
            var diagnostics = new List<string>();
            if (fragments.Length == 0) diagnostics.Add("EmptyDefinitionSet");
            var ordered = fragments.OrderBy(f => f == null ? string.Empty : f.Feature.Value ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(f => f == null ? string.Empty : f.FragmentKind ?? string.Empty, StringComparer.Ordinal)
                .ToArray();

            foreach (var fragment in ordered)
            {
                if (fragment == null || string.IsNullOrEmpty(fragment.Feature.Value)) diagnostics.Add("InvalidFeatureIdentity");
                else if (string.IsNullOrEmpty(fragment.FragmentKind)) diagnostics.Add("InvalidFragmentKind");
                else if (fragment.SchemaVersion == 0 || fragment.SchemaVersion > SupportedFragmentSchemaVersion) diagnostics.Add("UnsupportedFragmentSchema");
            }

            var duplicateKeys = ordered.Where(f => f != null).GroupBy(f => (f.Feature.Value ?? string.Empty) + "\u001f" + (f.FragmentKind ?? string.Empty), StringComparer.Ordinal).Where(g => g.Count() > 1);
            diagnostics.AddRange(duplicateKeys.Select(_ => "DuplicateFragment"));

            var featureGroups = ordered.Where(f => f != null && !string.IsNullOrEmpty(f.Feature.Value)).GroupBy(f => f.Feature.Value, StringComparer.Ordinal).ToArray();
            foreach (var group in featureGroups)
            {
                var kinds = new HashSet<string>(group.Select(f => f.FragmentKind), StringComparer.Ordinal);
                if (!kinds.Contains("identity")) diagnostics.Add("MissingIdentityFragment:" + group.Key);
                if (!kinds.Contains("binding")) diagnostics.Add("MissingBindingFragment:" + group.Key);
            }

            var featureIds = new HashSet<string>(featureGroups.Select(g => g.Key), StringComparer.Ordinal);
            foreach (var fragment in ordered.Where(f => f != null))
                foreach (var required in fragment.RequiredFeatures)
                    if (required.Value == null || !featureIds.Contains(required.Value)) diagnostics.Add("MissingRequiredFeature:" + (required.Value ?? string.Empty));

            if (diagnostics.Count != 0) return new LinkResult(LinkStatus.LinkFailed, null, new ReadOnlyCollection<string>(diagnostics.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray()));

            var canonical = string.Join("\n", ordered.Select(f => string.Join("|", f.Feature.Value, f.FragmentKind, f.SchemaVersion.ToString("D5"), DigestText(f.SourceDigest), string.Join(",", f.RequiredFeatures.Select(x => x.Value).OrderBy(x => x, StringComparer.Ordinal)))));
            byte[] digestBytes;
            using (var sha256 = SHA256.Create()) digestBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var digest = ToDigest(digestBytes);
            var records = featureGroups.OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new CompiledFeatureRecord(g.First().Feature, new ReadOnlyCollection<string>(g.Select(x => x.FragmentKind).OrderBy(x => x, StringComparer.Ordinal).ToArray()), digest)).ToArray();
            return new LinkResult(LinkStatus.LinkSucceeded, new CompiledIdentityCatalog(records, digest), new ReadOnlyCollection<string>(new string[0]));
        }

        private static string DigestText(Digest256 digest) { return digest.Part0.ToString("X16") + digest.Part1.ToString("X16") + digest.Part2.ToString("X16") + digest.Part3.ToString("X16"); }
        private static Digest256 ToDigest(byte[] bytes) { return new Digest256(BitConverter.ToUInt64(bytes, 0), BitConverter.ToUInt64(bytes, 8), BitConverter.ToUInt64(bytes, 16), BitConverter.ToUInt64(bytes, 24)); }
    }

    public sealed class FeatureAdmissionHandle
    {
        internal FeatureAdmissionHandle(CompiledIdentityCatalog catalog, CompiledFeatureRecord record) { Catalog = catalog; Record = record; }
        public CompiledIdentityCatalog Catalog { get; }
        public CompiledFeatureRecord Record { get; }
    }

    public sealed class FeatureLoadGate
    {
        private readonly CompiledIdentityCatalog catalog;
        public FeatureLoadGate(CompiledIdentityCatalog catalog) { this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)); }
        public bool TryAdmit(FeatureId feature, out FeatureAdmissionHandle handle)
        {
            CompiledFeatureRecord record;
            if (!catalog.TryGet(feature, out record)) { handle = null; return false; }
            handle = new FeatureAdmissionHandle(catalog, record);
            return true;
        }
    }
}
