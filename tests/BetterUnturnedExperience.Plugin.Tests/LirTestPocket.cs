using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Events;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lir;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-12：测试宿主的「宿主已给补丁口袋」等价起点（与 DEV-V6-11 的
    /// LitTestPocket 同形，换弹身份）。
    ///
    /// 本票之后，换弹模块只允许在生命周期内武装补丁并经启动口袋登记——缺口袋或登记
    /// 被拒=立即自拆、不留半装（票面 Scope）。手工 bootstrap 的老套件此前传的是一个
    /// 没有补丁口的口袋；本助手按生产组合（BueFeatureStartRuntime.ComposeBootstrap 的
    /// 第 12 实参）同形地造真账户：FeatureRegistrationRuntime（登记目录）+
    /// FeatureLifecycleRuntime（统一代际机）+ BeginStart 开代际 + FeaturePatchingRuntime
    /// 视图（句柄进的就是既有资源账）。每个用例自持一个实例（静态账互不串）。
    ///
    /// 与 LIT 版的两处换弹专有面：
    ///   - 受理的句柄外面套一层计数替身（<see cref="LiveCount"/> / <see cref="ReleasedCount"/>）
    ///     ——换弹的开关可在同一代际内反复装卸，账上「活句柄数 == 武装状态」是本票的可判据；
    ///   - 替身本身幂等：模块撤销（句柄 Dispose）与平台边界释放（账 Dispose）各只计一次，
    ///     「不双拆」因此是可数事实而不是文本推断。
    /// 缺口袋 / 登记被拒 / 半装三条红向由 DEV-V6-12 组用显式替身专测，不走本助手。
    /// </summary>
    internal sealed class LirTestPocket
    {
        /// <summary>本口袋绑定的功能身份（默认=官方换弹）。</summary>
        internal FeatureId Feature { get; private set; }

        /// <summary>本次 BeginStart 分配的生命周期代际（bootstrap 与口袋视图同绑它）。</summary>
        internal ulong Generation { get; private set; }

        /// <summary>启动口袋上的补丁口视图（bootstrap 第 12 实参）。</summary>
        internal IFeaturePatching Patching { get; private set; }

        /// <summary>经口受理的计数替身（按登记序）——「进账」直接可观察。</summary>
        internal readonly List<IDisposable> Accepted = new List<IDisposable>();

        /// <summary>替身内层：模块交出的真句柄（换弹的 LirPatchHandle；按登记序）。</summary>
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
            var handle = Inner[index] as LirPatchHandle;
            if (handle != null) return handle.Released;
            return ((CountingHandle)Accepted[index]).Released;
        }

        /// <summary>账上活条目数——「设置变化后账与武装状态一致」的机器判据。</summary>
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

        /// <summary>账上已拆但仍列着的条目数（幽灵条目）——票面「禁止账上留下已拆句柄」的
        /// 直接计数。代际内应恒为 0；边界之后本夹具保留历史条目，读数不再代表生产账。</summary>
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
            return Machine.Isolate(Feature, FrameworkErrorCode.ModuleStartFailed, "v612-isolate", "v612");
        }

        /// <summary>再启用开新代际（机的 BeginStart）：旧代际视图作废、新视图可用
        /// （「代际作废」腿）。返回新代际号。</summary>
        internal ulong OpenNextGeneration()
        {
            if (!Machine.BeginStart(Feature, out var generation))
                throw new InvalidOperationException("LirTestPocket: 新代际开启失败");
            Generation = generation;
            Patching = new RecordingPatching(
                new FeaturePatchingRuntime(Machine, MachineLines.Add).CreateView(Feature, generation), this);
            return generation;
        }

        private FeatureLifecycleRuntime Machine;

        internal static LirTestPocket Open(string featureId = "io.github.yu80rice.bue.in-place-reload")
        {
            var registrations = new FeatureRegistrationRuntime();
            registrations.OpenRegistration();
            var accepted = registrations.Register(LirFeatureAssembly.CreateRegistration());
            if (!accepted.Accepted)
                throw new InvalidOperationException("LirTestPocket: LIR 登记被拒 " + accepted.Reason + "/" + accepted.DiagnosticId);
            if (!registrations.CompleteRuntime())
                throw new InvalidOperationException("LirTestPocket: 目录冻结失败");
            var feature = new FeatureId(featureId);
            var pocket = new LirTestPocket { Feature = feature };
            var machine = new FeatureLifecycleRuntime(registrations, new FeatureEventBus(), pocket.MachineLines.Add);
            if (!machine.BeginStart(feature, out var generation))
                throw new InvalidOperationException("LirTestPocket: 开代际失败");
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
            private readonly LirTestPocket owner;

            internal RecordingPatching(IFeaturePatching inner, LirTestPocket owner)
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
