using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the beacon counter Postfixes — LHT's OWN patches (the
    /// ticket's 勘误口径：是信标补丁，不是 LIR 补丁), installed under the
    /// Harmony id = the FeatureId. The two Postfixes share their native call
    /// sites (InteractableBeacon.spawnRemaining / despawnAlive — server-side
    /// counter mutations, InteractableBeacon.cs:62-93); the business context
    /// never leaves the HordeBeaconContextGuard: a call not carrying the
    /// tracked active beacon releases immediately and the native flow is
    /// untouched (pure Postfix — no prefix, no skip, no rewrite).
    ///
    /// 节点 B 实时进度更新：spawnRemaining 后 remaining-- alive++（已击杀不变，
    /// HUD 仍需刷新）；despawnAlive 后 alive--（已击杀 +1）。两个 Postfix 都只
    /// 置脏，由广播周期下一帧统一发送，避免同帧多次击杀产生过多广播包。
    /// </summary>
    [HarmonyPatch(typeof(InteractableBeacon))]
    internal static class BeaconCounterPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("spawnRemaining")]
        private static void SpawnRemainingPostfix(InteractableBeacon __instance)
        {
            HordeTrackerModule.OnBeaconCounterPatched(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("despawnAlive")]
        private static void DespawnAlivePostfix(InteractableBeacon __instance)
        {
            HordeTrackerModule.OnBeaconCounterPatched(__instance);
        }
    }
}
