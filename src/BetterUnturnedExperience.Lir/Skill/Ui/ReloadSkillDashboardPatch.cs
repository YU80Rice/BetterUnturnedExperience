namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 表面 A 唯一补丁面：原版 PlayerDashboardSkillsUI.updateSelection
    /// 的 postfix（战斗/防御/支援三页共用出口，分区只属战斗页——过滤在闸内）。
    /// 无 [HarmonyPatch] 属性——绑定唯一事实源 = ReloadSkillDashboardBinder
    /// （显式 MethodBase 解析，宿主可证）。体是纯转发：登记闸、镜像闸、等级
    /// 读、行模型、Glazier 注入全在 ReloadSkillDashboardAdapter 及其下游，本
    /// 类型不持有任何状态与开关位（红测 5e 反射钉）。
    /// </summary>
    internal static class ReloadSkillDashboardPatch
    {
        internal static void Postfix(byte specialityIndex)
        {
            ReloadSkillDashboardAdapter.OnSkillsSectionRebuilt(specialityIndex);
        }
    }
}
