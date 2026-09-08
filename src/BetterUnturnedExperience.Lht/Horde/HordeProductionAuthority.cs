using System;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the production engine authority — the ONE class that touches
    /// the game for the server-authority piece (BeaconManager / Provider /
    /// Commander / ChatManager / BarricadeManager). Every member is the
    /// migrated old-plugin call, unchanged in kind; the LMN ABI guard and the
    /// ModTransport.Initialize call are gone with the old plugin identity.
    /// </summary>
    internal sealed class HordeProductionAuthority : IHordeTrackingAuthority
    {
        internal static readonly HordeProductionAuthority Instance = new HordeProductionAuthority();

        private const float CommanderUnavailableLogIntervalSeconds = 60f;

        private CommandHorde commandHorde;
        private float nextCommanderUnavailableLogAt;
        private bool registrationGivenUpPermanently;

        // The game's event delegate types (BeaconUpdated / Provider.ServerHosted)
        // are NOT System.Action — the engine-free seam's delegates are forwarded
        // through stored wrappers so -= removes the same instance. One module
        // generation subscribes at a time (the singleton authority serves one
        // live tracking module).
        private BeaconUpdated beaconForwarder;
        private Action<byte, bool> beaconHandler;
        private Provider.ServerHosted hostedForwarder;
        private System.Action hostedHandler;

        private HordeProductionAuthority() { }

        public bool IsServerRole()
        {
            return Provider.isServer;
        }

        public void SubscribeBeaconUpdated(Action<byte, bool> handler)
        {
            beaconHandler = handler;
            if (beaconForwarder == null) beaconForwarder = ForwardBeacon;
            BeaconManager.onBeaconUpdated += beaconForwarder;
        }

        public void UnsubscribeBeaconUpdated(Action<byte, bool> handler)
        {
            if (beaconForwarder != null) BeaconManager.onBeaconUpdated -= beaconForwarder;
            beaconHandler = null;
        }

        public void SubscribeServerHosted(System.Action handler)
        {
            hostedHandler = handler;
            if (hostedForwarder == null) hostedForwarder = ForwardServerHosted;
            Provider.onServerHosted += hostedForwarder;
        }

        public void UnsubscribeServerHosted(System.Action handler)
        {
            if (hostedForwarder != null) Provider.onServerHosted -= hostedForwarder;
            hostedHandler = null;
        }

        private void ForwardBeacon(byte nav, bool hasBeacon)
        {
            beaconHandler?.Invoke(nav, hasBeacon);
        }

        private void ForwardServerHosted()
        {
            hostedHandler?.Invoke();
        }

        /// <summary>读取本波总僵尸数（旧 ResolveTotalZombies 迁移）：
        /// 优先路径 BarricadeDrop.asset → ItemBeaconAsset.wave；
        /// 降级路径 HarmonyLib.Traverse 读 InteractableBeacon.asset 私有字段。</summary>
        public bool TryResolveBeacon(byte nav, out HordeBeaconView beacon)
        {
            beacon = null;
            InteractableBeacon raw = BeaconManager.checkBeacon(nav);
            if (raw == null) return false;

            ushort total = ResolveTotalZombies(raw);
            if (total == 0) return false;

            beacon = new HordeBeaconView(raw, OwnerResolver.Resolve(raw), LocationResolver.Resolve(raw.transform.position), total);
            return true;
        }

        public bool TryReadCounters(HordeBeaconView beacon, out int remaining, out int alive)
        {
            remaining = 0;
            alive = 0;
            var raw = beacon?.Beacon as InteractableBeacon;
            if (raw == null) return false;
            remaining = raw.getRemaining();
            alive = raw.getAlive();
            return true;
        }

        /// <summary>The old TryRegisterHordeCommand: only requests from
        /// Provider.onServerHosted land here via the module's pending flag; the
        /// vanilla global command table must already exist (never initialized,
        /// replaced or cleared by LHT); a registration FAILURE gives up for
        /// good (the old one-shot semantics — no retry spam).</summary>
        public bool TryFlushHordeCommandRegistration(HordeStatusSource source)
        {
            if (!Provider.isServer) return false;
            if (commandHorde != null || registrationGivenUpPermanently) return true;

            if (Commander.commands == null)
            {
                float now = Time.realtimeSinceStartup;
                if (now >= nextCommanderUnavailableLogAt)
                {
                    nextCommanderUnavailableLogAt = now + CommanderUnavailableLogIntervalSeconds;
                    LhtRuntime.LogInfo("[HordeTracker] Commander.commands unavailable; /horde registration remains pending");
                }
                return false;
            }

            try
            {
                commandHorde = new CommandHorde(new Local(), source);
                Commander.register(commandHorde);
            }
            catch (Exception error)
            {
                // true = pending resolved (given up) — the module stops retrying.
                registrationGivenUpPermanently = true;
                commandHorde = null;
                LhtRuntime.LogError("[HordeTracker] /horde registration failed: " + error.Message);
                return true;
            }

            LhtRuntime.LogInfo("[HordeTracker] 已注册 /horde 命令（Commander.register）");
            return true;
        }

        public void DeregisterHordeCommand()
        {
            if (commandHorde != null)
            {
                try
                {
                    if (Commander.commands != null)
                        Commander.deregister(commandHorde);
                }
                catch (Exception error)
                {
                    LhtRuntime.LogWarning("[HordeTracker] /horde deregister failed: " + error.Message);
                }
                commandHorde = null;
            }
            registrationGivenUpPermanently = false;
        }

        /// <summary>尸潮开始的聊天广播（旧 HordeEventNotifier.NotifyHordeStart 折入，
        /// ChatManager.say(string, Color, bool) 服务器广播语义不变）。</summary>
        public void NotifyHordeStart(HordeBeaconView info)
        {
            if (!Provider.isServer) return;
            if (info == null || !info.IsValid) return;

            string msg =
                "<color=#ffcc00>⚠警告 尸潮爆发！</color> " +
                "<color=#ff6666>" + info.LocationName + "</color> " +
                "<color=#aaaaaa>(发起者: <color=#ffffff>" + info.OwnerName + "</color>)</color> | " +
                "<color=#88ff88>总数 " + info.TotalZombies + "</color>";

            ChatManager.say(msg, Palette.SERVER, true);
        }

        public void NotifyHordeEnd(HordeBeaconView info)
        {
            if (!Provider.isServer) return;

            string location = info != null ? info.LocationName : "未知地点";
            int killed = 0;
            int total = info != null ? info.TotalZombies : 0;
            if (info != null && TryReadCounters(info, out int remaining, out int alive))
            {
                int outstanding = remaining + alive;
                killed = total >= outstanding ? total - outstanding : 0;
            }

            string msg =
                "<color=#88ff88>✓ 尸潮已平息</color> " +
                "<color=#aaaaaa>" + location + "</color> " +
                "<color=#888888>(击杀 " + killed + "/" + total + ")</color>";

            ChatManager.say(msg, Palette.SERVER, true);
        }

        private static ushort ResolveTotalZombies(InteractableBeacon beacon)
        {
            Transform root = beacon.transform != null ? beacon.transform.root : null;
            if (root != null)
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(root);
                if (drop != null && drop.asset is ItemBeaconAsset beaconAsset)
                    return beaconAsset.wave;
            }

            var privateAsset = Traverse.Create(beacon).Field("asset").GetValue<ItemBeaconAsset>();
            return privateAsset != null ? privateAsset.wave : (ushort)0;
        }
    }
}
