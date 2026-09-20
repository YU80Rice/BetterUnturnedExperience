using System;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V6-11（V6-T5 Q2 + 2026-09-18 追加裁决）：整理的补丁句柄——本功能的一个
    /// Harmony 身份（全部整理补丁面共用同一 Harmony 实例/ID）对应一枚句柄，经启动口袋的
    /// IFeaturePatching 进入既有资源账（同代际、同逆序释放、同每代际容量）。句柄=BCL
    /// IDisposable（不新增契约句柄类型）；Dispose 即该补丁集的拆除动作，由平台在停止/
    /// 隔离边界按账逆序释放调用——登记成功后这就是唯一的正常拆除点，模块自身不再
    /// UnpatchSelf（禁止平台拆一次、模块再拆一次）。
    ///
    /// 拆除动作由模块提供：账释放路径（ReleasePatchesFromPlatform）只归零在册状态、
    /// 不吞 UnpatchSelf 的异常（反打已装 IL 会重新 JIT 属主方法体，无 Unity 运行时的
    /// 宿主与游戏版本漂移环境会抛——宿主 ECall 规则），异常沿平台账户释放管线隔离成
    /// BUE-LIFE-006；释放行只在拆除动作返回后落，含义即「平台跑过这个补丁集的拆除且它
    /// 返回了」，正是停止边界被判定的事实。武装期的自拆（半装失败/缺口袋/登记被拒）
    /// 走模块自己的受守卫路径（不抛出 Start），两者分工不同（V5-04 与 05 各自纪律）。
    /// </summary>
    internal sealed class TidyPatchHandle : IDisposable
    {
        private readonly Action releasePatches;
        private readonly string harmonyId;
        private readonly Action<string> log;
        private bool released;

        internal TidyPatchHandle(string harmonyId, Action releasePatches, Action<string> log)
        {
            this.harmonyId = harmonyId ?? string.Empty;
            this.releasePatches = releasePatches;
            this.log = log;
        }

        /// <summary>本句柄拥有的 Harmony 身份（诊断——登记行/释放行点名整套补丁）。</summary>
        internal string HarmonyId { get { return harmonyId; } }

        /// <summary>平台账已释放（停止/隔离边界跑过本句柄的拆除动作）。</summary>
        internal bool Released { get { return released; } }

        public void Dispose()
        {
            if (released) return;
            released = true;
            // 拆除动作先跑，且不吞异常：抛出的故障沿平台的账户释放管线浮出并隔离
            //（BUE-LIFE-006），此时下面的释放行不会落——释放行的含义因此是「平台跑过
            // 这个补丁集的拆除且它返回了」（DEV-V6-05 BII 冻结语义，官方功能同一律）。
            // 日志口在武装期捕获（生产=宿主注入口）：停止边界之后模块可能已解绑 sink，
            // 释放行仍必须可见——拆除事实不因功能自己的日志卫生而消失。
            if (releasePatches != null) releasePatches();
            if (log != null)
            {
                try { log("BUE 整理补丁由平台账拆除 harmonyId=" + harmonyId + " diagnosticId=BUE-LIT-PATCH-004"); }
                catch (Exception) { }
            }
        }
    }
}
