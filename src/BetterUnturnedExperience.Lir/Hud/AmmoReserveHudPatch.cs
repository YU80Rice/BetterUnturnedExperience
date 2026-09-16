namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-06 唯一补丁面：原版 UseableGun.updateInfo 的 postfix。
    /// 无 [HarmonyPatch] 属性——绑定唯一事实源 = AmmoReserveHudBinder
    /// （显式 MethodBase 解析，宿主可证）。体是纯转发：所有闸（登记=唯一
    /// 开关）、观察、投影、呈现都在 AmmoReserveHudAdapter 及其下游，本类型
    /// 不持有任何状态与开关位（红测 4h 反射钉）。
    /// </summary>
    internal static class AmmoReserveHudPatch
    {
        internal static void Postfix(object __instance)
        {
            AmmoReserveHudAdapter.OnGunInfoUpdated(__instance);
        }
    }
}
