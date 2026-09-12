using System.Collections.Generic;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    // DEV-V4-07 (V4-T6 Q60): the chrome copy table — one player-facing sentence
    // per official feature, keyed by FeatureId. This is PANEL CHROME, not
    // contract: zero 2.1 members, no SDK surface, no registration field (T1:
    // 功能描述若入契约须 Minor 2.2 可选 facet，本阶段不偷渡). Entries with no
    // row here draw NO description and NO placeholder (Q60: 无对照表的生态条目
    // 不画、不占位) — the panel itself is not in the catalog and has no row.
    // The sentences are verbatim from the frozen spec table (「官方文案与 NoOp
    // （V4-T6 → DEV-V4-07）」): the Network line says 「BUE 功能模块」 (never
    // 「仅官方」) and the LIT line names the headers it covers instead of
    // 「服装栏」. The table never replaces feature state, setting descriptions
    // or the SDK — it only answers "what is this feature?".
    internal static class PanelChromeCopy
    {
        private static readonly Dictionary<string, string> Sentences = new Dictionary<string, string>(7)
        {
            // 条目顺序与措辞逐字对齐 spec Q60 表。
            { "io.github.yu80rice.bue.better-item-interaction", "在支持的格子里增强拖入，失败时回到原版操作。" },
            { "io.github.yu80rice.bue.inventory-tidy", "整理背包与装备栏物品；模式和方向在本页设置，背包标题栏点「整理」。" },
            { "io.github.yu80rice.bue.in-place-reload", "换弹尽量留在原位；背包整理完成后自动压缩弹药。" },
            { "io.github.yu80rice.bue.horde-tracker", "由主机追踪尸潮，并在客户端显示播报。" },
            { "io.github.yu80rice.bue.network", "为 BUE 功能模块提供多人通信通道；可停用，停用不等于卸载。" },
            { "io.github.yu80rice.bue.network.v1compat", "为仍使用数字频道的旧插件提供兼容接收，不提供新的注册入口。" },
            { "io.github.yu80rice.bue.noop", "生态接入样板，用于展示功能描述、Toggle 和 Choice 在面板中的呈现。" },
        };

        /// <summary>命中=该功能的一句话；未命中=false（详情页不画、不占位）。
        /// 表的全部外显面就是这个查询——测试只经模型投影缝断言，不暴露内部
        /// 键集合（Testing Decisions「只测外显行为」）。</summary>
        internal static bool TryGetDescription(string featureId, out string description)
        {
            return Sentences.TryGetValue(featureId, out description);
        }
    }
}
