using System;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: /horde 命令（旧 CommandHorde 迁移，引擎壳变薄）。规则不变：
    /// - 权限继续交给原版 ChatManager.process 守门（dedicated server 仅 admin，
    ///   listen server 任意玩家），LHT 不复制权限事实源；
    /// - 每个 SteamID 1.5s 冷却（实现常量，逻辑在 HordeCommandLogic，可测）；
    /// - 仅服务器端执行（命令系统在 listen server 自动派发到这里）。
    /// enabled=false 的完整停摆由注销命令承担（RefreshSwitches/Stop →
    /// DeregisterHordeCommand）；状态源折叠 enabled 门作为深度防御。
    /// </summary>
    internal sealed class CommandHorde : Command
    {
        private readonly HordeStatusSource statusSource;

        public CommandHorde(Local newLocalization, HordeStatusSource statusSource)
        {
            localization = newLocalization;
            this.statusSource = statusSource ?? throw new ArgumentNullException(nameof(statusSource));
            _command = "horde";
            _info = "查询当前活跃尸潮状态";
            _help = "用法: /horde - 显示当前尸潮的剩余数和总数";
        }

        protected override void execute(CSteamID executorID, string parameter)
        {
            // 仅服务器端执行。
            if (!Provider.isServer)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (!HordeCommandLogic.TryAcceptRequest(executorID.m_SteamID, now, out string reply))
            {
                ChatManager.say(executorID, reply, Palette.SERVER, true);
                return;
            }

            if (!statusSource(out HordeBeaconView info) || info == null || !info.IsValid)
            {
                ChatManager.say(executorID, HordeCommandLogic.NoHordeMessage, Palette.SERVER, true);
                return;
            }

            // 仅发给执行者（不广播，避免打扰其他玩家）。
            ChatManager.say(executorID, HordeCommandLogic.FormatStatus(info), Palette.SERVER, true);
        }
    }
}
