using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;

namespace BetterUnturnedExperience.Contracts.Tests
{
    /// <summary>
    /// DEV-V6-06 红测组：登记摘要的公开函数与现网受理门禁同源（V6-T6 Q5 + 2026-09-18 追加裁决）。
    /// 被测外部行为（作者可观察面）：
    ///   1. 契约公开面上恰有一个摘要生成函数
    ///      `FeatureDefinitionDigest.ComputeArtifactPayloadDigest`——第三方作者用它生成
    ///      `FeatureDefinitionArtifact.ArtifactPayloadDigest`，不必手拆 SHA-256；
    ///   2. 该函数输出=现网受理门禁重算的摘要：按该函数填摘要的工件被真实
    ///      `FeatureRegistrationRuntime` 受理；只改摘要一位即被拒 `InvalidDefinitionArtifact`
    ///      （门禁真在比对，不是「不比对就收」）——「公开函数与现网摘要一致」的判据；
    ///   3. 算法冻结（SHA-256 → little-endian 四段 64 位）：换算法/换字节序必红
    ///      （「digest 双算法」的机器咬合点）；
    ///   4. 单源锚：受理路径源码调用该公开函数，且不再保留旧私有载荷摘要实现
    ///      （与 `eng/Verify-ContractDocs.ps1` 的同名规则同判据，红态一致）。
    /// 非空转证明=突变 M1/M2/M7（见 audit/2026-09-19/DEV-V6-06/mutations/）。
    /// </summary>
    internal static class DevV606DigestHelperTests
    {
        private const string HelperTypeName = "BetterUnturnedExperience.Contracts.FeatureDefinitionDigest";
        private const string HelperMethodName = "ComputeArtifactPayloadDigest";

        internal static void Run(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, Action body)
                {
                    try { body(); }
                    catch (Exception error) { reds.Add(name + " 组异常: " + error.GetType().Name + ": " + error.Message); }
                }

                Group("公开摘要函数形状（恰一个公开方法，签名冻结）", () => V606GroupShape(Check));
                Group("算法冻结值（SHA-256 → little-endian 四段；换算法或字节序必红）", () => V606GroupFrozenValues(Check));
                Group("与现网受理同源（公开函数输出被受理；改一位即被拒）", () => V606GroupProductionGate(Check));
                Group("单源源锚（受理路径调用公开函数，无第二份实现）", () => V606GroupSourceSingleSource(Check));
                Group("参数契约（null fail-fast；归一稳定）", () => V606GroupArgumentContract(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        // ───────────────────────── 组 1：形状锚 ─────────────────────────

        /// <summary>公开面形状：静态类、恰一个公开方法、签名 (IEnumerable&lt;byte&gt;) → Digest256。
        /// 形状即契约——多一个公开方法是加性扩面，须走账本，故此处钉死「恰一个」。</summary>
        private static void V606GroupShape(Action<bool, string> Check)
        {
            var helper = typeof(FeatureDefinitionDigest);
            Check(helper.Name == "FeatureDefinitionDigest" && helper.Namespace == "BetterUnturnedExperience.Contracts",
                "公开函数宿主类型名/命名空间冻结（" + HelperTypeName + "）");
            Check(helper.IsAbstract && helper.IsSealed, "静态类（abstract+sealed）才是纯函数宿主，不是可实例化类型");
            var publics = helper.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Check(publics.Length == 1, "公开函数面应恰一个方法，实际 " + publics.Length + " 个");
            if (publics.Length != 1) return;
            var method = publics[0];
            Check(method.Name == HelperMethodName, "方法名冻结: " + method.Name + "（应为 " + HelperMethodName + "）");
            Check(method.ReturnType == typeof(Digest256), "返回类型应为 Digest256，实际 " + method.ReturnType.Name);
            var parameters = method.GetParameters();
            Check(parameters.Length == 1 && parameters[0].ParameterType == typeof(IEnumerable<byte>),
                "参数应恰一个 IEnumerable<byte>（与登记工件 CanonicalPayload 同形）");
        }

        // ───────────────────────── 组 2：冻结值 ─────────────────────────

        /// <summary>算法冻结值：SHA-256 摘要按 little-endian 切成四段 64 位。期望值独立算出
        /// （非从实现抄回），换一套算法或字节序必红。</summary>
        private static void V606GroupFrozenValues(Action<bool, string> Check)
        {
            var probe = new byte[] { 66, 85, 69, 45, 86, 54, 48, 54 }; // "BUE-V606"
            var digest = FeatureDefinitionDigest.ComputeArtifactPayloadDigest(probe);
            Check(digest.Part0 == 1137537942891974517UL, "BUE-V606 Part0 冻结，实际 " + digest.Part0);
            Check(digest.Part1 == 1874477857413678475UL, "BUE-V606 Part1 冻结，实际 " + digest.Part1);
            Check(digest.Part2 == 12351249358527232899UL, "BUE-V606 Part2 冻结，实际 " + digest.Part2);
            Check(digest.Part3 == 11203296418397064621UL, "BUE-V606 Part3 冻结，实际 " + digest.Part3);

            // 二值探针：与 NoOpFixture 活样板内联常量同源（sha256({1,2,3}) 的四段）。
            var small = FeatureDefinitionDigest.ComputeArtifactPayloadDigest(new byte[] { 1, 2, 3 });
            Check(small.Part0 == 5317555933983313923UL && small.Part1 == 8642148531063968556UL
                && small.Part2 == 2942485310001909708UL && small.Part3 == 9366110643396117629UL,
                "sha256({1,2,3}) 四段冻结（与活样板常量一致）");
        }

        // ───────────────────────── 组 3：与现网受理同源 ─────────────────────────

        /// <summary>作者路径实证：用公开函数填摘要→现网受理；摘要改一位→拒 InvalidDefinitionArtifact。
        /// 第二条断言是「门禁真在比对」的咬合点（只测受理会被「不比对就收」的实现骗过）。</summary>
        private static void V606GroupProductionGate(Action<bool, string> Check)
        {
            var payload = new byte[] { 66, 85, 69, 45, 86, 54, 48, 54 }; // "BUE-V606"
            var digest = FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload);
            var runtime = new FeatureRegistrationRuntime();
            runtime.OpenRegistration();
            var accepted = runtime.Register(new DigestProbeRegistration(new FeatureDefinitionArtifact(
                new FeatureId("io.example.digest-helper"), 1, "bue-v606-probe-set", new Digest256(1UL, 0UL, 0UL, 6UL), digest, payload)));
            Check(accepted.Accepted, "按公开函数填摘要的工件应被现网受理，实际 reason=" + accepted.Reason + " diagnosticId=" + accepted.DiagnosticId);
            Check(runtime.FreezeCatalog() && runtime.Catalog.Entries.Count == 1, "受理后目录恰含该条目");
            if (runtime.Catalog != null && runtime.Catalog.Entries.Count == 1)
                Check(SameDigest(runtime.Catalog.Entries[0].Definition.ArtifactPayloadDigest, digest),
                    "目录条目摘要素=公开函数输出（受理即绑定，不重算成第二套）");

            var tampered = new Digest256(digest.Part0 + 1UL, digest.Part1, digest.Part2, digest.Part3);
            var rejecting = new FeatureRegistrationRuntime();
            rejecting.OpenRegistration();
            var rejected = rejecting.Register(new DigestProbeRegistration(new FeatureDefinitionArtifact(
                new FeatureId("io.example.digest-tampered"), 1, "bue-v606-probe-set", new Digest256(1UL, 0UL, 0UL, 6UL), tampered, payload)));
            Check(!rejected.Accepted && rejected.Reason == FeatureRegistrationReason.InvalidDefinitionArtifact,
                "摘要不符应被拒 InvalidDefinitionArtifact，实际 accepted=" + rejected.Accepted + " reason=" + rejected.Reason);
            Check(rejected.DiagnosticId == "BUE-REG-004", "拒因诊断码应为 BUE-REG-004，实际 " + rejected.DiagnosticId);
        }

        // ───────────────────────── 组 4：单源源锚 ─────────────────────────

        /// <summary>源级锚：受理路径（玩家合并集内）调用公开函数，且旧的私有载荷摘要实现已清。
        /// 锚的性质=文本锚（非执行证明），如实标注；执行级同源由组 3 的受理比对承担。</summary>
        private static void V606GroupSourceSingleSource(Action<bool, string> Check)
        {
            var root = FindRepoRoot();
            Check(root != null, "从测试基目录能定位到仓库根（BetterUnturnedExperience.sln）");
            if (root == null) return;
            var runtimePath = Path.Combine(root, "src", "BetterUnturnedExperience.Core", "Registration", "FeatureRegistrationRuntime.cs");
            Check(File.Exists(runtimePath), "受理路径源码在位: " + runtimePath);
            if (!File.Exists(runtimePath)) return;
            var source = File.ReadAllText(runtimePath);
            Check(source.Contains("FeatureDefinitionDigest.ComputeArtifactPayloadDigest"),
                "受理路径必须调用公开摘要函数（单源；不得自带第二份实现）");
            Check(source.IndexOf("ComputePayloadDigest", StringComparison.Ordinal) < 0,
                "受理路径不得保留旧私有载荷摘要实现（双算法残留）");
        }

        // ───────────────────────── 组 5：参数契约 ─────────────────────────

        private static void V606GroupArgumentContract(Action<bool, string> Check)
        {
            var threw = false;
            try { FeatureDefinitionDigest.ComputeArtifactPayloadDigest(null); }
            catch (ArgumentNullException) { threw = true; }
            Check(threw, "null 载荷=开发期错误 fail-fast（ArgumentNullException），不得静默当空载荷");

            var payload = new byte[] { 7, 8, 9 };
            var fromArray = FeatureDefinitionDigest.ComputeArtifactPayloadDigest(payload);
            var fromList = FeatureDefinitionDigest.ComputeArtifactPayloadDigest(new List<byte>(payload));
            Check(SameDigest(fromArray, fromList), "数组与列表输入同值（输入归一稳定，非仅接受 byte[]）");
        }

        // ───────────────────────── 辅助 ─────────────────────────

        private sealed class DigestProbeRegistration : IFeatureRegistration
        {
            private readonly FeatureDefinitionArtifact definition;

            internal DigestProbeRegistration(FeatureDefinitionArtifact definition) { this.definition = definition; }

            public FeatureDefinitionArtifact Definition { get { return definition; } }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new DigestProbeFactory(); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class DigestProbeFactory : IFeatureModuleFactory
        {
            public IFeatureModule Create() { return null; }
        }

        private static bool SameDigest(Digest256 left, Digest256 right)
        {
            return left.Part0 == right.Part0 && left.Part1 == right.Part1 && left.Part2 == right.Part2 && left.Part3 == right.Part3;
        }

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BetterUnturnedExperience.sln")))
                directory = directory.Parent;
            return directory == null ? null : directory.FullName;
        }
    }
}
