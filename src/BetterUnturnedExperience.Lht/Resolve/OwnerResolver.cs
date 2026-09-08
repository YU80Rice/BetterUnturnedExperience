using System;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: 通过 InteractableBeacon 反查放置者玩家名（旧 OwnerResolver
    /// 原样迁移，100% 创意工坊信标兼容）。
    ///
    /// 多态适配策略：不审核物品 ID，仅通过 BarricadeDrop 类型检查 +
    /// GetServersideData 公共 API；反射降级读 _serversideData 私有字段。
    /// 审计依据：InteractableBeacon 作为 Component 添加到 barricade GameObject
    /// （BarricadeTool.cs:188）；BarricadeData.owner 是 ulong SteamID64；
    /// SteamPlayerID.playerName 是 public 只读属性（streamer mode 可能匿名）。
    /// </summary>
    internal static class OwnerResolver
    {
        private const string UnknownOwner = "未知";

        public static string Resolve(InteractableBeacon beacon)
        {
            if (beacon == null)
                return UnknownOwner;

            // 安全：beacon 可能在场景切换中已被销毁，transform 引用失效
            Transform root;
            try { root = beacon.transform != null ? beacon.transform.root : null; }
            catch (Exception e) { LhtRuntime.LogWarning("[OwnerResolver] transform 读取异常，按未知处理: " + e.Message); return UnknownOwner; }
            if (root == null)
                return UnknownOwner;

            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(root);
            if (drop == null)
                return UnknownOwner;

            BarricadeData data = drop.GetServersideData();
            if (data == null)
            {
                // 反射降级：工坊自定义 barricade 可能未走标准初始化路径
                data = TraverseCreate(drop).Field("_serversideData").GetValue<BarricadeData>();
                if (data == null)
                    return UnknownOwner;
            }

            ulong ownerSteamId = data.owner;
            if (ownerSteamId == 0UL)
                return "世界";

            string playerName = LookupPlayerName(ownerSteamId);
            if (!string.IsNullOrEmpty(playerName))
                return playerName;

            // 玩家已下线，显示 SteamID 前 7 位作为简记
            string sid = ownerSteamId.ToString();
            return "玩家#" + (sid.Length > 7 ? sid.Substring(0, 7) : sid);
        }

        /// <summary>封装 Traverse 创建，便于异常隔离。</summary>
        private static Traverse TraverseCreate(object target)
        {
            try { return Traverse.Create(target); }
            catch (Exception e) { LhtRuntime.LogWarning("[OwnerResolver] Traverse 创建失败: " + e.Message); return null; }
        }

        private static string LookupPlayerName(ulong ownerSteamId)
        {
            // Provider.clients 可能在某些生命周期阶段为 null
            var clients = Provider.clients;
            if (clients == null)
                return null;

            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null)
                    continue;

                // SteamPlayerID == 运算符陷阱：operator == 内部访问 playerID_0.steamID，
                // 当任一操作数为 null 时抛 NRE。必须用 ReferenceEquals 判空。
                if (ReferenceEquals(sp.playerID, null))
                    continue;

                // CSteamID 仅有显式 ulong 转换
                if ((ulong)sp.playerID.steamID == ownerSteamId)
                    return sp.playerID.playerName;
            }
            return null;
        }
    }
}
