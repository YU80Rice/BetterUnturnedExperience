using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: 地图多态自适应地名反查器（旧 LocationResolver 原样迁移，
    /// 100% 创意工坊地图兼容）。
    ///
    /// 1. 现代优先路径：LocationDevkitNodeSystem.Get().GetAllNodes()
    ///    （不过滤 isVisibleOnMap：工坊地图作者可能未设此标记，由距离阀门兜底）
    /// 2. Legacy 降级路径：LevelNodes.nodes（[Obsolete] 但兼容旧地图）
    /// 3. 距离安全阀门：250 米（sqr 阈值 62500），超出 → 荒野/偏远区域
    /// 本地化策略：key = name.Replace(' ', '_'); map.has(key) ? map.format(key) : name
    /// </summary>
    internal static class LocationResolver
    {
        private const float MAX_DISTANCE_METERS = 250f;
        private const float MAX_DISTANCE_SQR = MAX_DISTANCE_METERS * MAX_DISTANCE_METERS;
        private const string WildernessFallback = "荒野/偏远区域";

        public static string Resolve(Vector3 worldPosition)
        {
            LocationDevkitNode closestDevkit = FindClosestDevkitNode(worldPosition, out float devkitSqr);
            if (closestDevkit != null && devkitSqr <= MAX_DISTANCE_SQR)
            {
                string name = closestDevkit.locationName;
                if (!string.IsNullOrWhiteSpace(name))
                    return LocalizeName(name);
            }

            LocationNode closestLegacy = FindClosestLegacyLocationNode(worldPosition, out float legacySqr);
            if (closestLegacy != null && legacySqr <= MAX_DISTANCE_SQR)
            {
                string name = closestLegacy.name;
                if (!string.IsNullOrWhiteSpace(name))
                    return LocalizeName(name);
            }

            // 分支 B：超出 250m 或地图无地标节点 -> Fallback
            return WildernessFallback;
        }

        private static LocationDevkitNode FindClosestDevkitNode(Vector3 pos, out float closestSqr)
        {
            closestSqr = float.MaxValue;
            LocationDevkitNode closest = null;

            LocationDevkitNodeSystem system = LocationDevkitNodeSystem.Get();
            if (system == null) return null;

            var nodes = system.GetAllNodes();
            if (nodes == null) return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                LocationDevkitNode node = nodes[i];
                if (node == null) continue;

                Transform t = node.transform;
                if (t == null) continue;

                float sqr = (t.position - pos).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = node;
                }
            }
            return closest;
        }

        /// <summary>LevelNodes.nodes 标记为 [Obsolete]，部分老地图未迁移，保留作降级。</summary>
        private static LocationNode FindClosestLegacyLocationNode(Vector3 pos, out float closestSqr)
        {
            closestSqr = float.MaxValue;
            LocationNode closest = null;

#pragma warning disable 0618 // LevelNodes.nodes 已 Obsolete，此处作降级路径
            var nodes = LevelNodes.nodes;
#pragma warning restore 0618
            if (nodes == null) return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                if (node == null) continue;
                if (node.type != ENodeType.LOCATION) continue;

                if (!(node is LocationNode locNode)) continue;

                float sqr = (locNode.point - pos).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = locNode;
                }
            }
            return closest;
        }

        /// <summary>按 PlayerDashboardInformationUI 的模式做本地化。</summary>
        private static string LocalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return WildernessFallback;

            LevelInfo info = Level.info;
            if (info == null)
                return name;

            Local local = info.getLocalization();
            if (local == null)
                return name;

            string key = name.Replace(' ', '_');
            if (local.has(key))
                return local.format(key);

            return name;
        }
    }
}
