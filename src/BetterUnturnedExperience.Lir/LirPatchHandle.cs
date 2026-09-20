using System;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V6-12（V6-T5 Q2 + 2026-09-18 追加裁决）：换弹的补丁句柄——本功能的一个
    /// Harmony 身份（全部换弹补丁面共用同一 Harmony 实例/ID）对应一枚句柄，经启动口袋的
    /// IFeaturePatching 进入既有资源账（同代际、同逆序释放、同每代际容量）。句柄=BCL
    /// IDisposable（不新增契约句柄类型）；Dispose 即该补丁集的拆除动作，由平台在停止/
    /// 隔离边界按账逆序释放调用——登记成功后这就是唯一的正常拆除点，模块自身不再
    /// UnpatchSelf（禁止平台拆一次、模块再拆一次）。
    ///
    /// 与整理（LIT）版同形，另加换弹专有的「一代内可重复拆装」（设置开关热摘热装）：
    ///   - **登记是代际级的**：一个功能在一个生命周期代际内只登记一条（一代一条账项，
    ///     开关热摘热装既不新增账项也不释放句柄——账不随开关累积，也不留已拆句柄；
    ///     既有资源账的每代际容量因此与开关次数无关）；
    ///   - **撤销经本句柄**：开关 off 走 `RevokeCurrentPatches()`——跑的是**同一条**拆除
    ///     动作（不是模块另起一次 UnpatchSelf），句柄仍归平台账；
    ///   - **平台是正常拆除的唯一所有者**：边界 `Dispose` 是那条动作的第二次也是最后一次
    ///     调用；届时若已由开关撤下，动作识别为「无在架补丁」并按空操作返回（不双拆），
    ///     释放行的含义仍是「平台跑过这个补丁集的拆除且它返回了」。
    ///
    /// 拆除动作由模块提供：账释放路径（ReleasePatchesFromPlatform）只归零在册状态、
    /// 不吞 UnpatchSelf 的异常（反打已装 IL 会重新 JIT 属主方法体，无 Unity 运行时的
    /// 宿主与游戏版本漂移环境会抛——宿主 ECall 规则），异常沿平台账户释放管线隔离成
    /// BUE-LIFE-006；释放行只在拆除动作返回后落，含义即「平台/模块跑过这个补丁集的拆除
    /// 且它返回了」。武装期的自拆（半装失败/缺口袋/登记被拒）走模块自己的受守卫路径
    /// （不抛出 Start），两者分工不同（V5-04 与 05 各自纪律）。
    /// </summary>
    internal sealed class LirPatchHandle : IDisposable
    {
        private readonly Action releasePatches;
        private readonly string harmonyId;
        private readonly Action<string> log;
        private bool released;

        internal LirPatchHandle(string harmonyId, Action releasePatches, Action<string> log)
        {
            this.harmonyId = harmonyId ?? string.Empty;
            this.releasePatches = releasePatches;
            this.log = log;
        }

        /// <summary>本句柄拥有的 Harmony 身份（诊断——登记行/释放行点名整套补丁）。</summary>
        internal string HarmonyId { get { return harmonyId; } }

        /// <summary>已释放（平台边界跑过本句柄的拆除动作）——释放后不得再拆一次。</summary>
        internal bool Released { get { return released; } }

        /// <summary>开关热摘撤销过的次数（「撤销可重复」的机器可判据；登记不因此变动）。</summary>
        internal int RevokeCount { get; private set; }

        /// <summary>
        /// 开关 off 的一代内撤销：跑本句柄那条拆除动作（同一个 Harmony 身份、同一条路径），
        /// **不动登记生命周期**——句柄仍归平台账，边界释放届时对未武装状态空转（不双拆）。
        /// 不吞异常：调用方是设置路径，由模块侧决定如何记账（不在册状态一律归零）。
        /// </summary>
        internal void RevokeCurrentPatches()
        {
            if (released) return;
            RevokeCount++;
            if (releasePatches != null) releasePatches();
        }

        public void Dispose()
        {
            if (released) return;
            released = true;
            // 拆除动作先跑，且不吞异常：抛出的故障沿平台的账户释放管线浮出并隔离
            //（BUE-LIFE-006），此时下面的释放行不会落——释放行的含义因此是「拆除动作
            // 跑过且它返回了」（DEV-V6-05 BII 冻结语义，官方功能同一律）。日志口在武装期
            // 捕获（生产=宿主注入口）：停止边界之后模块可能已解绑 sink，释放行仍必须可见
            // ——拆除事实不因功能自己的日志卫生而消失。开关已撤下（无在架补丁）时那条
            // 动作按空操作返回，本行照样是该事实的留痕（账目结清）。
            if (releasePatches != null) releasePatches();
            if (log != null)
            {
                try { log("BUE 换弹补丁已拆除 harmonyId=" + harmonyId + " diagnosticId=BUE-LIR-PATCH-004"); }
                catch (Exception) { }
            }
        }
    }
}
