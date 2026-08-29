using BepInEx;
using System;
using HarmonyLib;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin
{
    // [DEBUG-min] R17 bisection probe build: UPM-shaped minimal plugin. Only
    // one Harmony constructor postfix and one Update log; everything else is
    // bypassed to find which BUE Awake statement breaks the frame pipeline.
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
                var target = AccessTools.Constructor(typeof(SDG.Unturned.MenuWorkshopUI), Type.EmptyTypes);
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(BetterUnturnedExperiencePlugin), nameof(MenuWorkshopCtorPostfix)));
                log.Info("[DEBUG-min] event=patch-installed target=" + (target != null));
            }
            catch (Exception error)
            {
                log.Warn("[DEBUG-min] event=patch-failed errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
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

        private static void MenuWorkshopCtorPostfix()
        {
            log.Info("[DEBUG-min] event=ctor-postfix-hit source=MenuWorkshopUI.constructor");
        }
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
