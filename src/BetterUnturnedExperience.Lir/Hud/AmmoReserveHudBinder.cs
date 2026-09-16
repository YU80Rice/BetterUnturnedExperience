using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-06 绑定唯一事实源：弹药后备 HUD 的补丁面清单与目标方法解析都
    /// 集中在这里（04 教训：名字绑定不可证——显式 MethodBase 解析成宿主可证
    /// 的单源）。目标 = SDG.Unturned.UseableGun 的私有零参 updateInfo（原版
    /// 写 ammoLabel「当前/上限」的唯一出口，postfix 追加后备读数；updateInfo
    /// 非重载，仍按名+元数全声明扫描要求恰一命中，漂移即抛不静默绑错）。
    /// </summary>
    internal static class AmmoReserveHudBinder
    {
        /// <summary>本票唯一触发面（红测 5a 逐字钉数量与身份——无夹带面）。</summary>
        internal static readonly IReadOnlyList<Type> PatchSurface = new Type[] { typeof(AmmoReserveHudPatch) };

        /// <summary>恰一 updateInfo；0 或多于 1 = 版本漂移，抛（半装=整体撤销）。</summary>
        internal static MethodBase ResolveInfoUpdateTarget()
        {
            var candidates = new List<MethodBase>();
            var methods = typeof(UseableGun).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            for (var i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != "updateInfo") continue;
                if (methods[i].GetParameters().Length != 0) continue;
                candidates.Add(methods[i]);
            }
            if (candidates.Count != 1)
            {
                throw new InvalidOperationException("ammo-hud bind target not provable: UseableGun.updateInfo matches=" + candidates.Count);
            }
            return candidates[0];
        }

        /// <summary>显式绑定（6-arg Patch 面）：target ← 唯一事实源，postfix ← PatchSurface 面的 Postfix。</summary>
        internal static void Bind(Harmony harmony, Type patchType)
        {
            if (harmony == null) throw new ArgumentNullException(nameof(harmony));
            if (patchType != typeof(AmmoReserveHudPatch))
            {
                throw new InvalidOperationException("ammo-hud binder only accepts its single surface, got: " + (patchType == null ? "null" : patchType.Name));
            }
            var target = ResolveInfoUpdateTarget();
            var postfix = AccessTools.Method(patchType, "Postfix");
            if (postfix == null)
            {
                throw new InvalidOperationException("ammo-hud postfix body missing: " + patchType.Name + ".Postfix");
            }
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }
    }
}
