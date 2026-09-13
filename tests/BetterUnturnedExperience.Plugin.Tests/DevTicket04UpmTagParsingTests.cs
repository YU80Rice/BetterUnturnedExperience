using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BetterUnturnedExperience.ClientUi.Internal;

namespace BetterUnturnedExperience.Plugin.Tests
{
    // POST-P4-04：UPM 分类导航与列表配置文本路径——LoadedPluginCatalogAdapter 采集组。
    // 判据走真实 BepInEx ConfigFile（Bind 构造、真实序列化往返），不打桩：
    // Unturned.Category:<名> 解析（trim、空名/无标签回落配置节）；ItemList/BlueprintList/
    // CreatureList（裸标签与冒号后缀两式）→ 带逐字格式提示的字符串文本行、不投影档位、
    // 不是选择器；列表标签优先于 Cycle 标签与 AcceptableValueList；未识别的 XxxList 标签
    // 底层仍是字符串 → 普通可编文本行、不整行只读。面板投影/草稿侧判据在 ClientUi.Tests
    // （DevTicket04UpmCategoryListTests）。
    internal static class DevTicket04UpmTagParsingTests
    {
        private const string ItemHint = "值格式：物品ID, 物品ID, ...";
        private const string BlueprintHint = "值格式：所属物品ID:配方编号, ...";
        private const string CreatureHint = "值格式：Z:僵尸类型 或 A:动物资产ID, ...";

        private static readonly List<string> TempPaths = new List<string>();

        internal static void Run()
        {
            try
            {
                CategoryTagParsesNameOtherwiseFallsBackToSection();
                ListTagsBecomeHintedTextRowsNotPickers();
                ListTagWinsOverCycleAndDiscreteConstraint();
                UnrecognizedListLikeTagStaysPlainEditableText();
                Console.WriteLine("POST-P4-04 Plugin.Tests UPM tag parsing tests: PASS");
            }
            finally
            {
                Cleanup();
            }
        }

        // Category 标签=分类；空名/无标签回落配置节；与其它标签共存互不干扰。
        private static void CategoryTagParsesNameOtherwiseFallsBackToSection()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Combat", "Tagged", "x", new ConfigDescription("甲。", null, new object[] { "Unturned.Category:战斗" }));
            config.Bind("Combat", "Padded", "y", new ConfigDescription("乙。", null, new object[] { "Unturned.Category:  自动回收  " }));
            config.Bind("Combat", "EmptyName", "z", new ConfigDescription("丙。", null, new object[] { "Unturned.Category:" }));
            config.Bind("外观", "NoTag", "w", new ConfigDescription("丁。", null, null));
            config.Bind("Game", "Together", "v", new ConfigDescription("戊。", null, new object[] { "Unturned.Category:名字", "RequiresRestart" }));
            var entries = adapter.CaptureConfigEntries("com.example.p4-04.cat", config);

            Assert(Find(entries, "Tagged").Category == "战斗", "Unturned.Category:<名> 解析为分类");
            Assert(Find(entries, "Padded").Category == "自动回收", "分类名两侧空白被 trim");
            Assert(Find(entries, "EmptyName").Category == "Combat", "空分类名回落配置节");
            Assert(Find(entries, "NoTag").Category == "外观", "无 Category 标签按配置节分组");
            var together = Find(entries, "Together");
            Assert(together.Category == "名字" && together.RequiresRestart, "Category 与 RequiresRestart 标签共存");
        }

        // 三类列表标签 → 字符串文本行 + 逐字格式提示；不投影档位（非选择器）；值照常可写回。
        private static void ListTagsBecomeHintedTextRowsNotPickers()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Items", "34,45", new ConfigDescription("物品表。", null, new object[] { "Unturned.ItemList" }));
            config.Bind("Game", "Blueprints", "34:8", new ConfigDescription("配方表。", null, new object[] { "Unturned.BlueprintList" }));
            config.Bind("Game", "Creatures", "Z:6201", new ConfigDescription("生物表。", null, new object[] { "Unturned.CreatureList" }));
            config.Bind("Game", "ColonForm", "i_sword", new ConfigDescription("冒号式。", null, new object[] { "Unturned.ItemList:i_sword|i_plank" }));
            var entries = adapter.CaptureConfigEntries("com.example.p4-04.lists", config);

            var items = Find(entries, "Items");
            Assert(items.Kind == PluginConfigValueKind.String && items.CanEdit, "ItemList 值仍是字符串、可编");
            Assert(items.ControlHint == ItemHint, "ItemList 提示=物品格式逐字");
            var blueprints = Find(entries, "Blueprints");
            Assert(blueprints.ControlHint == BlueprintHint, "BlueprintList 提示=配方格式逐字");
            var creatures = Find(entries, "Creatures");
            Assert(creatures.ControlHint == CreatureHint, "CreatureList 提示=生物格式逐字");
            Assert(items.AllowedChoices.Count == 0 && blueprints.AllowedChoices.Count == 0
                && creatures.AllowedChoices.Count == 0, "列表行不投影档位（不是选择器/Cycle）");
            Assert(Find(entries, "ColonForm").ControlHint == ItemHint && Find(entries, "ColonForm").CanEdit,
                "冒号后缀式标签同样识别为列表文本行");

            Assert(adapter.TrySet("com.example.p4-04.lists", "Items", PluginConfigValue.StringValue("7, 8")).Accepted,
                "列表值经普通字符串写回");
            Assert(Find(adapter.CaptureConfigEntries("com.example.p4-04.lists", config), "Items").Value.Text == "7, 8",
                "写回真实落盘生效");
        }

        // 优先级镜像 UPM：列表标签先于 Cycle；带列表标签即便挂 AcceptableValueList 也不变选择器。
        private static void ListTagWinsOverCycleAndDiscreteConstraint()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Mixed", "a", new ConfigDescription("混合。", new AcceptableValueList<string>("a", "b"),
                new object[] { "Unturned.ItemList", "Unturned.Cycle:x|y" }));
            var mixed = Find(adapter.CaptureConfigEntries("com.example.p4-04.priority", config), "Mixed");
            Assert(mixed.AllowedChoices.Count == 0, "列表标签压过 Cycle 标签与 AcceptableValueList：不投影档位");
            Assert(mixed.ControlHint == ItemHint, "列表标签决定行=带物品格式提示的文本行");
            Assert(mixed.Kind == PluginConfigValueKind.String && mixed.CanEdit, "类型层与可编性不受标签影响");
        }

        // 未识别的列表样式标签（如 QuestList）：底层是字符串 → 仍是普通可编文本行，
        // 不整行只读、不造假提示（Q69 静默忽略纪律在列表族上的延伸）。
        private static void UnrecognizedListLikeTagStaysPlainEditableText()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Quests", "q1", new ConfigDescription("任务表。", null, new object[] { "Unturned.QuestList" }));
            var quests = Find(adapter.CaptureConfigEntries("com.example.p4-04.unknown", config), "Quests");
            Assert(quests.Kind == PluginConfigValueKind.String && quests.CanEdit, "未识别列表标签+字符串底层=仍可编");
            Assert(quests.ControlHint.Length == 0, "未识别标签不造假格式提示");
            Assert(quests.AllowedChoices.Count == 0, "未识别标签不投影档位");
            Assert(adapter.TrySet("com.example.p4-04.unknown", "Quests", PluginConfigValue.StringValue("q2")).Accepted,
                "普通文本写回不受未识别标签阻碍");
        }

        // ── helpers ──

        private static ConfigFile NewConfig()
        {
            var path = TempConfigPath();
            return new ConfigFile(path, false);
        }

        private static string TempConfigPath()
        {
            var path = Path.Combine(Path.GetTempPath(), "bue-p4-04-" + Guid.NewGuid().ToString("N") + ".cfg");
            TempPaths.Add(path);
            return path;
        }

        private static void Cleanup()
        {
            for (var index = 0; index < TempPaths.Count; index++)
            {
                try { if (File.Exists(TempPaths[index])) File.Delete(TempPaths[index]); }
                catch (IOException) { }
            }
            TempPaths.Clear();
        }

        private static PluginConfigEntryView Find(IReadOnlyList<PluginConfigEntryView> entries, string key)
        {
            for (var index = 0; index < entries.Count; index++)
                if (string.Equals(entries[index].Key, key, StringComparison.Ordinal)) return entries[index];
            throw new InvalidOperationException("config entry not found: " + key);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
