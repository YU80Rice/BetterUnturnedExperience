using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-06 弹药后备 HUD 的功能私有 adapter——postfix 体与投影/观察/呈现
    /// 三层之间唯一的闸与线。纪律 = 「生命周期登记即唯一开关」（04/05 同律，
    /// 票面「停用则不画」）：入口只看 InPlaceReloadModule.ActiveModule 在册位
    /// （登记交还 = 不观察、不投影、不呈现——原版读数回到裸态），本类型自身
    /// 没有任何 Enabled/Disabled 位可复活（红测 4h 反射钉）。
    /// 三层不得混成一个设置字段（spec 分层裁决）：本票只有「运行状态→HUD
    /// 投影」这一层——功能启停是生命周期，不是设置；等级读数归 07。
    /// 宿主测试面：ScannerForTests/ApplyForTests 替换真引擎观察与 Glazier 呈现
    /// （真机面=具名接缝缺口，实机随 08）。
    /// </summary>
    internal static class AmmoReserveHudAdapter
    {
        /// <summary>Host-test seam: replaces the engine observation read.</summary>
        internal static Func<object, AmmoReserveObservation> ScannerForTests;

        /// <summary>Host-test seam: replaces the Glazier surface write.</summary>
        internal static Action<object, AmmoReserveResult> ApplyForTests;

        /// <summary>Host-test seam: replaces the surface HideAll.</summary>
        internal static Action HideAllForTests;

        /// <summary>
        /// updateInfo postfix 的入口。原版已写好「当前/上限」之后才到此——
        /// 本面只追加后备读数，从不回改原版文本。功能停（登记交还）= 直接
        /// return：连观察都不发生，不是「算了但藏起来」。
        /// </summary>
        internal static void OnGunInfoUpdated(object gun)
        {
            var module = InPlaceReloadModule.ActiveModule;
            if (module == null || !module.Started || module.ShuttingDown) return;
            try
            {
                var scanner = ScannerForTests;
                var observation = scanner != null ? scanner(gun) : AmmoReserveHudEngine.Observe(gun);
                var result = AmmoReserveProjection.Project(observation);
                var apply = ApplyForTests;
                if (apply != null) apply(gun, result);
                else AmmoReserveHudSurface.Apply(gun, result);
            }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[AmmoHud] 投影链异常（已隔离）: " + error.Message);
            }
        }

        /// <summary>
        /// 注销的反操作（InstallPatches 半装失败与 UninstallPatches 都经此）：
        /// 收掉全部已注入读数。未登记面（headless/从未注入）= surface 自身
        /// 空表短路，零反射零日志。
        /// </summary>
        internal static void RevokeAll()
        {
            try
            {
                var hideAll = HideAllForTests;
                if (hideAll != null) hideAll();
                else AmmoReserveHudSurface.HideAll();
            }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[AmmoHud] HideAll 异常（登记已归零，呈现吞掉）: " + error.Message);
            }
        }
    }
}
