using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Events;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lht;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-13：测试宿主的「宿主已给补丁口袋」等价起点（与 DEV-V6-11 的
    /// LitTestPocket / DEV-V6-12 的 LirTestPocket 同形，尸潮身份）。
    ///
    /// 本票之后，尸潮模块只允许在生命周期内武装补丁并经启动口袋登记——缺口袋或登记
    /// 被拒=立即自拆、不留半装（票面 Scope）。手工 bootstrap 的老套件此前传的是一个
    /// 没有补丁口的口袋；本助手按生产组合（BueFeatureStartRuntime.ComposeBootstrap 的
    /// 第 12 实参）同形地造真账户：FeatureRegistrationRuntime（登记目录）+
    /// FeatureLifecycleRuntime（统一代际机）+ BeginStart 开代际 + FeaturePatchingRuntime
    /// 视图（句柄进的就是既有资源账）。每个用例自持一个实例（静态账互不串）。
    ///
    /// 与 LIR 版的两处尸潮专有面：
    ///   - 尸潮的开关是**全停开关**（off = 追踪退订 / 频道注销 / 补丁原生回退；on =
    ///     整链重臂），因此一个代际内同样会出现多次「武装 → 撤销」；
    ///   - 受理的句柄外面套一层计数替身（<see cref="LiveCount"/> / <see cref="ReleasedCount"/>）
    ///     ——账上「活句柄数 == 补丁集在架」是本票的可判据；替身本身幂等：模块撤销
    ///     （句柄 RevokeCurrentPatches）与平台边界释放（账 Dispose）各只计一次，
    ///     「不双拆」因此是可数事实而不是文本推断。
    /// 缺口袋 / 登记被拒 / 半装三条红向由 DEV-V6-13 组用显式替身专测，不走本助手。
    /// </summary>
    internal sealed class LhtTestPocket
    {
        /// <summary>本口袋绑定的功能身份（默认=官方尸潮播报）。</summary>
        internal FeatureId Feature { get; private set; }

        /// <summary>本次 BeginStart 分配的生命周期代际（bootstrap 与口袋视图同绑它）。</summary>
        internal ulong Generation { get; private set; }

        /// <summary>启动口袋上的补丁口视图（bootstrap 第 12 实参）。</summary>
        internal IFeaturePatching Patching { get; private set; }

        /// <summary>经口受理的计数替身（按登记序）——「进账」直接可观察。</summary>
        internal readonly List<IDisposable> Accepted = new List<IDisposable>();

        /// <summary>替身内层：模块交出的真句柄（尸潮的 LhtPatchHandle；按登记序）。</summary>
        internal readonly List<IDisposable> Inner = new List<IDisposable>();

        /// <summary>被拒次数（替身记账；真实拒绝由本票红测组专测）。</summary>
        internal int RejectedCount { get; private set; }

        /// <summary>经平台账释放的条数（账在停止/隔离边界逆序 Dispose 了这些替身；
        /// 替身幂等，已由模块撤销结清的条目在这里只记一次空转）。</summary>
        internal int ReleasedCount
        {
            get
            {
                var released = 0;
                for (var i = 0; i < Accepted.Count; i++)
                    if (((CountingHandle)Accepted[i]).Released) released++;
                return released;
            }
        }

        /// <summary>账目是否已结清：一枚登记条目由其持有的真句柄说了算（模块撤销与平台释放
        /// 都使真句柄结清，两条路径在同一计数下等价——「不双拆」的观察面）。</summary>
        private bool IsSettled(int index)
        {
            var handle = Inner[index] as LhtPatchHandle;
            if (handle != null) return handle.Released;
            return ((CountingHandle)Accepted[index]).Released;
        }

        /// <summary>账上条目总数（本代际经口受理的登记条目数）——「一代恰一条」的直接读数；
        /// 与「句柄是否已结清」是两件事，故与 <see cref="LiveCount"/> 分开呈现。</summary>
        internal int InAccountCount { get { return Accepted.Count; } }

        /// <summary>账上尚未结清的条目数（真句柄未被释放）——「登记仍在账、平台仍拥有拆除」
        /// 的机器判据。注意口径：它**不**表示补丁集在架（在架事实由模块的
        /// `PatchesInstalled` 表达）；开关热摘只撤在架状态、不动登记（登记是代际级的），
        /// 两条事实因此分开断言，互不冒充。</summary>
        internal int LiveCount
        {
            get
            {
                var live = 0;
                for (var i = 0; i < Accepted.Count; i++)
                    if (!IsSettled(i)) live++;
                return live;
            }
        }

        /// <summary>账上已结清但仍列着的条目数（幽灵条目）——票面「禁止账上留下已拆句柄」的
        /// 直接计数：结清后的条目在本代际内不得再被当作活资源。代际内应恒为 0；边界之后本
        /// 夹具保留历史条目，读数不再代表生产账。</summary>
        internal int GhostCount
        {
            get
            {
                var ghosts = 0;
                for (var i = 0; i < Accepted.Count; i++)
                    if (IsSettled(i)) ghosts++;
                return ghosts;
            }
        }

        /// <summary>代际机自己的诊断行（生产走宿主日志口）——「句柄进既有账」与
        /// 「边界释放」两行在此可判。</summary>
        internal readonly List<string> MachineLines = new List<string>();

        /// <summary>测试侧停机边界（真账逆序释放）：老套件多数不调；调了即验平台拆除。</summary>
        internal bool StopAndRelease()
        {
            if (!Machine.BeginStop(Feature, FeatureStopReason.PluginStopping)) return false;
            return Machine.CompleteStop(Feature);
        }

        /// <summary>隔离边界（机的 Isolate）：撤该功能订阅 + 逆序释放其受跟踪资源
        /// ——与停止边界同一账、同一释放律（「隔离使已登记补丁失效」腿）。</summary>
        internal bool Isolate()
        {
            return Machine.Isolate(Feature, FrameworkErrorCode.ModuleStartFailed, "v613-isolate", "v613");
        }

        /// <summary>再启用开新代际（机的 BeginStart）：旧代际视图作废、新视图可用
        /// （「代际作废」腿）。返回新代际号。</summary>
        internal ulong OpenNextGeneration()
        {
            if (!Machine.BeginStart(Feature, out var generation))
                throw new InvalidOperationException("LhtTestPocket: 新代际开启失败");
            Generation = generation;
            Patching = new RecordingPatching(
                new FeaturePatchingRuntime(Machine, MachineLines.Add).CreateView(Feature, generation), this);
            return generation;
        }

        private FeatureLifecycleRuntime Machine;

        internal static LhtTestPocket Open(string featureId = "io.github.yu80rice.bue.horde-tracker")
        {
            var registrations = new FeatureRegistrationRuntime();
            registrations.OpenRegistration();
            var accepted = registrations.Register(LhtFeatureAssembly.CreateRegistration());
            if (!accepted.Accepted)
                throw new InvalidOperationException("LhtTestPocket: LHT 登记被拒 " + accepted.Reason + "/" + accepted.DiagnosticId);
            if (!registrations.CompleteRuntime())
                throw new InvalidOperationException("LhtTestPocket: 目录冻结失败");
            var feature = new FeatureId(featureId);
            var pocket = new LhtTestPocket { Feature = feature };
            var machine = new FeatureLifecycleRuntime(registrations, new FeatureEventBus(), pocket.MachineLines.Add);
            if (!machine.BeginStart(feature, out var generation))
                throw new InvalidOperationException("LhtTestPocket: 开代际失败");
            pocket.Generation = generation;
            pocket.Machine = machine;
            pocket.Patching = new RecordingPatching(
                new FeaturePatchingRuntime(machine, pocket.MachineLines.Add).CreateView(feature, generation), pocket);
            return pocket;
        }

        /// <summary>计数替身：包住模块交出的真句柄，自己的释放幂等（账边界释放经它转发到
        /// 真句柄，重复释放不再拆一次）。</summary>
        private sealed class CountingHandle : IDisposable
        {
            private readonly IDisposable inner;
            private bool released;

            internal CountingHandle(IDisposable inner)
            {
                this.inner = inner;
            }

            internal bool Released { get { return released; } }

            public void Dispose()
            {
                if (released) return;
                released = true;
                if (inner != null) inner.Dispose();
            }
        }

        private sealed class RecordingPatching : IFeaturePatching
        {
            private readonly IFeaturePatching inner;
            private readonly LhtTestPocket owner;

            internal RecordingPatching(IFeaturePatching inner, LhtTestPocket owner)
            {
                this.inner = inner;
                this.owner = owner;
            }

            public FeaturePatchRegistrationResult Register(IDisposable patchTeardown)
            {
                var wrapper = new CountingHandle(patchTeardown);
                var result = inner.Register(wrapper);
                if (result.Registered)
                {
                    owner.Accepted.Add(wrapper);
                    owner.Inner.Add(patchTeardown);
                }
                else
                {
                    owner.RejectedCount++;
                }
                return result;
            }
        }
    }
}
