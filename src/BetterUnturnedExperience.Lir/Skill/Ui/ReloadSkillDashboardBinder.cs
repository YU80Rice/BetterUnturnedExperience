using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 表面 A 绑定唯一事实源（06 同律：名字绑定不可证——显式
    /// MethodBase 解析成宿主可证的单源）。目标 = 原版 U 菜单技能页的私有静态
    /// updateSelection(byte)：三专精共用的行带重建出口（战斗=索引 0），postfix
    /// 在原版行之后追加 BUE 换弹技能分区。updateSelection 本票面无重载，仍按
    /// 名+元数+参数类型全声明扫描要求恰一命中，漂移即抛不静默绑错（抛=接不上
    /// 的判据，注册期探测/安装期同函数）。
    /// </summary>
    internal static class ReloadSkillDashboardBinder
    {
        /// <summary>本票唯一触发面（红测 5e 逐字钉数量与身份——无夹带面）。</summary>
        internal static readonly IReadOnlyList<Type> PatchSurface = new Type[] { typeof(ReloadSkillDashboardPatch) };

        private static bool? surfaceAProbe;

        internal static MethodBase ResolveSectionHostTarget()
        {
            var candidates = new List<MethodBase>();
            var methods = typeof(PlayerDashboardSkillsUI).GetMethods(
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            for (var i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != "updateSelection") continue;
                var parameters = methods[i].GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(byte)) continue;
                candidates.Add(methods[i]);
            }
            if (candidates.Count != 1)
            {
                throw new InvalidOperationException("reload-skill surface-A bind target not provable: PlayerDashboardSkillsUI.updateSelection matches=" + candidates.Count);
            }
            return candidates[0];
        }

        /// <summary>注册期探测（纯反射，零引擎调用；结果按进程缓存——两表面至多
        /// 其一的判定必须稳定）。接不上 = 表面 B（降级设置页）。</summary>
        internal static bool ProbeSurfaceA()
        {
            var cached = surfaceAProbe;
            if (cached.HasValue) return cached.Value;
            bool ok;
            try
            {
                ResolveSectionHostTarget();
                ok = true;
            }
            catch (Exception error)
            {
                LirRuntime.LogWarning("[ReloadSkill] 表面 A 探测失败（U 菜单接不上，降级设置页表面）: " + error.Message);
                ok = false;
            }
            surfaceAProbe = ok;
            return ok;
        }

        /// <summary>测试缝：复位进程级探测缓存。</summary>
        internal static void ResetProbeForTests() { surfaceAProbe = null; }

        /// <summary>显式绑定（6-arg Patch 面）：target ← 唯一事实源，postfix ← 面的 Postfix。</summary>
        internal static void Bind(Harmony harmony, Type patchType)
        {
            if (harmony == null) throw new ArgumentNullException(nameof(harmony));
            if (patchType != typeof(ReloadSkillDashboardPatch))
            {
                throw new InvalidOperationException("reload-skill binder only accepts its single surface, got: " + (patchType == null ? "null" : patchType.Name));
            }
            var target = ResolveSectionHostTarget();
            var postfix = AccessTools.Method(patchType, "Postfix");
            if (postfix == null)
            {
                throw new InvalidOperationException("reload-skill postfix body missing: " + patchType.Name + ".Postfix");
            }
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }
    }
}
