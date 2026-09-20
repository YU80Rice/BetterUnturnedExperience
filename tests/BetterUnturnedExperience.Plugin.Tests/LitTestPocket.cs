using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Events;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lit;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V6-11：测试宿主的「宿主已给补丁口袋」等价起点。
    ///
    /// 本票之后，整理模块只允许在生命周期内武装补丁并经启动口袋登记——缺口袋或登记
    /// 被拒=立即自拆、不留半装（票面 Scope）。手工 bootstrap 的老套件（V5-03/04/05、
    /// 联机 harness、02B 宿主环组）此前传的是一个没有补丁口的口袋；本助手按生产组合
    /// （BueFeatureStartRuntime.ComposeBootstrap 的第 12 实参）同形地造真账户：
    /// FeatureRegistrationRuntime（登记目录）+ FeatureLifecycleRuntime（统一代际机）
    /// + BeginStart 开代际 + FeaturePatchingRuntime 视图（句柄进的就是既有资源账）。
    /// 每个用例自持一个实例（静态账互不串，无跨用例状态）。缺口袋 / 登记被拒 / 半装
    /// 三条红向由 DEV-V6-11 组用显式替身专测，不走本助手。
    /// </summary>
    internal sealed class LitTestPocket
    {
        /// <summary>本口袋绑定的功能身份（默认=官方整理）。</summary>
        internal FeatureId Feature { get; private set; }

        /// <summary>本次 BeginStart 分配的生命周期代际（bootstrap 与口袋视图同绑它）。</summary>
        internal ulong Generation { get; private set; }

        /// <summary>启动口袋上的补丁口视图（bootstrap 第 12 实参）。</summary>
        internal IFeaturePatching Patching { get; private set; }

        /// <summary>经口受理的句柄（按登记序）——「进账」直接可观察。</summary>
        internal readonly List<IDisposable> Accepted = new List<IDisposable>();

        /// <summary>被拒次数（替身记账；真实拒绝由本票红测组专测）。</summary>
        internal int RejectedCount { get; private set; }

        /// <summary>代际机自己的诊断行（生产走宿主日志口）——「句柄进既有账」与
        /// 「边界释放」两行在此可判。</summary>
        internal readonly List<string> MachineLines = new List<string>();

        /// <summary>账上是否仍有未释放的句柄（登记后未到边界=1）。</summary>
        internal int TrackedCount { get { return Accepted.Count; } }

        /// <summary>测试侧停机边界（真账逆序释放）：老套件多数不调；调了即验平台拆除。</summary>
        internal bool StopAndRelease()
        {
            if (!Machine.BeginStop(Feature, FeatureStopReason.PluginStopping)) return false;
            return Machine.CompleteStop(Feature);
        }

        /// <summary>隔离边界（机的 Isolate）：撤该功能订阅 + 逆序释放其受跟踪资源
        /// ——与停止边界同一账、同一释放律（DEV-V6-11 的「隔离使已登记补丁失效」腿）。</summary>
        internal bool Isolate()
        {
            return Machine.Isolate(Feature, FrameworkErrorCode.ModuleStartFailed, "v611-isolate", "v611");
        }

        /// <summary>再启用开新代际（机的 BeginStart）：旧代际视图作废、新视图可用
        /// （DEV-V6-11 的「代际作废」腿）。返回新代际号。</summary>
        internal ulong OpenNextGeneration()
        {
            if (!Machine.BeginStart(Feature, out var generation))
                throw new InvalidOperationException("LitTestPocket: 新代际开启失败");
            Generation = generation;
            Patching = new RecordingPatching(
                new FeaturePatchingRuntime(Machine, MachineLines.Add).CreateView(Feature, generation), this);
            return generation;
        }

        private FeatureLifecycleRuntime Machine;

        internal static LitTestPocket Open(string featureId = "io.github.yu80rice.bue.inventory-tidy")
        {
            var registrations = new FeatureRegistrationRuntime();
            registrations.OpenRegistration();
            var accepted = registrations.Register(LitFeatureAssembly.CreateRegistration());
            if (!accepted.Accepted)
                throw new InvalidOperationException("LitTestPocket: LIT 登记被拒 " + accepted.Reason + "/" + accepted.DiagnosticId);
            if (!registrations.CompleteRuntime())
                throw new InvalidOperationException("LitTestPocket: 目录冻结失败");
            var feature = new FeatureId(featureId);
            var pocket = new LitTestPocket { Feature = feature };
            var machine = new FeatureLifecycleRuntime(registrations, new FeatureEventBus(), pocket.MachineLines.Add);
            if (!machine.BeginStart(feature, out var generation))
                throw new InvalidOperationException("LitTestPocket: 开代际失败");
            pocket.Generation = generation;
            pocket.Machine = machine;
            pocket.Patching = new RecordingPatching(
                new FeaturePatchingRuntime(machine, pocket.MachineLines.Add).CreateView(feature, generation), pocket);
            return pocket;
        }

        private sealed class RecordingPatching : IFeaturePatching
        {
            private readonly IFeaturePatching inner;
            private readonly LitTestPocket owner;

            internal RecordingPatching(IFeaturePatching inner, LitTestPocket owner)
            {
                this.inner = inner;
                this.owner = owner;
            }

            public FeaturePatchRegistrationResult Register(IDisposable patchTeardown)
            {
                var result = inner.Register(patchTeardown);
                if (result.Registered) owner.Accepted.Add(patchTeardown);
                else owner.RejectedCount++;
                return result;
            }
        }
    }
}
