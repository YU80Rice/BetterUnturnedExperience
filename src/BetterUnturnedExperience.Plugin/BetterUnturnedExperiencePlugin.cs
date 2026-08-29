using BepInEx;
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin
{
    // [DEBUG-min] R18 patch-level bisection probe: UPM-shaped minimal plugin
    // plus ALL 11 vanilla patches from the full build, each postfix logging
    // independently so one run shows exactly which patches hit and which
    // never do. Removed after diagnosis.
    [BepInPlugin("io.github.yu80rice.betterunturnedexperience", "Better Unturned Experience", "0.0.0")]
    public sealed class BetterUnturnedExperiencePlugin : BaseUnityPlugin
    {
        internal static ManualLogSourceProxy log;

        private int updateTicks;
        private Harmony harmony;

        private void Awake()
        {
            log = new ManualLogSourceProxy(Logger);
            log.Info("[DEBUG-min] event=awake-entered");
            try
            {
                harmony = new Harmony("io.github.yu80rice.bue.management-panel");
                Patch("MenuWorkshopUI.constructor", AccessTools.Constructor(typeof(SDG.Unturned.MenuWorkshopUI), Type.EmptyTypes), nameof(MenuWorkshopCtorPostfix));
                Patch("PlayerPauseUI.constructor", AccessTools.Constructor(typeof(SDG.Unturned.PlayerPauseUI), Type.EmptyTypes), nameof(PlayerPauseCtorPostfix));
                Patch("MenuDashboardUI.constructor", AccessTools.Constructor(typeof(SDG.Unturned.MenuDashboardUI), Type.EmptyTypes), nameof(MenuDashboardCtorPostfix));
                Patch("MenuDashboardUI.open", AccessTools.Method(typeof(SDG.Unturned.MenuDashboardUI), "open"), nameof(MenuDashboardOpenPostfix));
                Patch("MenuWorkshopUI.open", AccessTools.Method(typeof(SDG.Unturned.MenuWorkshopUI), "open"), nameof(MenuWorkshopOpenPostfix));
                Patch("PlayerPauseUI.open", AccessTools.Method(typeof(SDG.Unturned.PlayerPauseUI), "open"), nameof(PlayerPauseOpenPostfix));
                Patch("MenuUI.Update", AccessTools.Method(typeof(SDG.Unturned.MenuUI), "Update"), nameof(MenuUIUpdatePostfix));
                Patch("PlayerUI.Update", AccessTools.Method(typeof(SDG.Unturned.PlayerUI), "Update"), nameof(PlayerUIUpdatePostfix));
                Patch("MenuUI.escapeMenu", AccessTools.Method(typeof(SDG.Unturned.MenuUI), "escapeMenu"), nameof(MenuEscapePostfix));
                Patch("PlayerUI.escapeMenu", AccessTools.Method(typeof(SDG.Unturned.PlayerUI), "escapeMenu"), nameof(PlayerEscapePostfix));
                Patch("MenuUI.closeAll", AccessTools.Method(typeof(SDG.Unturned.MenuUI), "closeAll"), nameof(MenuCloseAllPostfix));
            }
            catch (Exception error)
            {
                log.Warn("[DEBUG-min] event=patch-failed errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
        }

        private void Patch(string name, MethodBase target, string postfixName)
        {
            var installed = false;
            try
            {
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(BetterUnturnedExperiencePlugin), postfixName));
                installed = true;
            }
            catch (Exception error)
            {
                log.Warn("[DEBUG-min] event=patch-failed target=" + name + " errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
            log.Info("[DEBUG-min] event=patch-installed target=" + name + " installed=" + installed);
        }

        private void Start()
        {
            log.Info("[DEBUG-min] event=start-entered");
        }

        private void Update()
        {
            updateTicks++;
            if (updateTicks == 1 || updateTicks % 120 == 0)
                log.Info("[DEBUG-min] event=plugin-update count=" + updateTicks);
        }

        private void OnDisable()
        {
            log.Info("[DEBUG-min] event=on-disabled");
        }

        private static void Hit(string target) { log.Info("[DEBUG-min] event=hit target=" + target); }
        private static void MenuWorkshopCtorPostfix() { Hit("MenuWorkshopUI.constructor"); }
        private static void PlayerPauseCtorPostfix() { Hit("PlayerPauseUI.constructor"); }
        private static void MenuDashboardCtorPostfix() { Hit("MenuDashboardUI.constructor"); }
        private static void MenuDashboardOpenPostfix() { Hit("MenuDashboardUI.open"); }
        private static void MenuWorkshopOpenPostfix() { Hit("MenuWorkshopUI.open"); }
        private static void PlayerPauseOpenPostfix() { Hit("PlayerPauseUI.open"); }
        private static void MenuUIUpdatePostfix() { Hit("MenuUI.Update"); }
        private static void PlayerUIUpdatePostfix() { Hit("PlayerUI.Update"); }
        private static void MenuEscapePostfix() { Hit("MenuUI.escapeMenu"); }
        private static void PlayerEscapePostfix() { Hit("PlayerUI.escapeMenu"); }
        private static void MenuCloseAllPostfix() { Hit("MenuUI.closeAll"); }
    }

    // [DEBUG-min] thin logger wrapper; removed after diagnosis.
    internal sealed class ManualLogSourceProxy
    {
        private readonly BepInEx.Logging.ManualLogSource source;

        internal ManualLogSourceProxy(BepInEx.Logging.ManualLogSource source) { this.source = source; }

        internal void Info(string message) { source?.LogInfo(message); }
        internal void Warn(string message) { source?.LogWarning(message); }
    }
}
