using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BetterUnturnedExperience.ClientUi.Internal;

namespace BetterUnturnedExperience.Plugin.Tests
{
    // DEV-V4-08：外部配置同等升级——LoadedPluginCatalogAdapter 组（spec「外部配置同等升级
    // （V4-T7 → DEV-V4-08）」节 + V4-T7 Q65/Q66/Q67/Q69/Q70 裁决）。判据全部走真实 BepInEx
    // ConfigFile/ConfigEntryBase（Bind 构造、真实序列化往返、真实落盘），不打桩：
    // 描述采集与空不画；Cycle 仅当值类型受支持+约束被识别+候选可写回；未识别离散标签静默
    // 忽略仍普通控件；非基础类型只读；结构化失败结果（不匹配异常文本）；UPM GUID 不在
    // adapter 层禁编。面板投影/草稿侧判据在 ClientUi.Tests（DevV4ExternalConfigParityTests）。
    internal static class DevV4ExternalConfigParityTests
    {
        private enum SampleKind { Alpha, Beta }

        private static readonly List<string> TempPaths = new List<string>();

        internal static void Run()
        {
            try
            {
                DescriptionCapturedRawAndEmptyStaysEmpty();
                CycleTagBecomesEditableCycleCandidates();
                AcceptableValueListBecomesCycleCandidates();
                UnwritableCandidatesFallBackToPlainControl();
                CycleCandidatesRespectRealClrRange();
                SingleCandidateStillCycles();
                BareCycleTagFallsBackToAcceptableValueList();
                UnrecognizedTagsAreSilentlyIgnored();
                UnsupportedClrTypeStaysReadOnly();
                UnsignedFullRangeStaysEditable();
                StructuredFailureClassification();
                UpmGuidCapturedAndEditable();
                RequiresRestartRowAttributePinned();
                Console.WriteLine("DEV-V4-08 Plugin.Tests adapter tests: PASS");
            }
            finally
            {
                Cleanup();
            }
        }

        // Q65：配置行描述来自 ConfigDescription.Description，空则不画、不占位；
        // 采集保原文（120 截断归投影层，Q37），不冒充插件级长描述。
        private static void DescriptionCapturedRawAndEmptyStaysEmpty()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Named", "OFF", new ConfigDescription("拖入时自动旋转物品以适配空位。", null, null));
            config.Bind("Game", "Empty", "x", new ConfigDescription(string.Empty, null, null));
            config.Bind("Game", "Null", "y", (ConfigDescription)null);
            config.Bind("Game", "Long", "z", new ConfigDescription(new string('长', 130), null, null));
            var entries = adapter.CaptureConfigEntries("com.example.desc", config);
            Assert(entries.Count == 4, "全部条目采集");
            Assert(Find(entries, "Named").Description == "拖入时自动旋转物品以适配空位。", "描述=ConfigDescription.Description 采集原文");
            Assert(Find(entries, "Empty").Description.Length == 0, "空描述=空串（空不画由渲染层跳行）");
            Assert(Find(entries, "Null").Description.Length == 0, "无 ConfigDescription=空串不画");
            Assert(Find(entries, "Long").Description.Length == 130, "采集保原文（120 截断归投影层，Q37）");
            Assert(Find(entries, "Named").DisplayName == "Named", "DisplayName=条目键照常采集（Q65：不用描述冒充长描述）");
        }

        // Q66：Cycle 形状仅当值类型受支持、约束被识别（Unturned.Cycle）、候选非空且可写回。
        private static void CycleTagBecomesEditableCycleCandidates()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Mode", "OFF", new ConfigDescription("模式。", null, new object[] { "Unturned.Cycle:OFF|1|2|3|4" }));
            var entries = adapter.CaptureConfigEntries("com.example.cycle", config);
            var mode = Find(entries, "Mode");
            Assert(mode.Kind == PluginConfigValueKind.String, "Cycle 标签不改变类型层（Q66：类型 vs 形状两层）");
            Assert(mode.CanEdit, "候选可写回 → 可编");
            Assert(ChoicesEqual(mode.AllowedChoices, "OFF", "1", "2", "3", "4"), "Cycle 标签冒号后档位按 | 分割");
            var written = adapter.TrySet("com.example.cycle", "Mode", PluginConfigValue.StringValue("2"));
            Assert(written.Accepted, "结构化 TrySet 接受档位写入");
            Assert(Find(adapter.CaptureConfigEntries("com.example.cycle", config), "Mode").Value.Text == "2", "档位写回 ConfigEntry 真实生效");
        }

        // Q66：AcceptableValueList 不再整行只读（原 HasDiscreteConstraint→Unsupported 摘除）。
        private static void AcceptableValueListBecomesCycleCandidates()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Count", 1, new ConfigDescription("数量。", new AcceptableValueList<int>(1, 2, 3), null));
            var count = Find(adapter.CaptureConfigEntries("com.example.avl", config), "Count");
            Assert(count.Kind == PluginConfigValueKind.Integer, "AcceptableValueList 条目类型层仍 Integer");
            Assert(count.CanEdit, "值类型受支持 → 可编（不再因离散约束整行只读）");
            Assert(ChoicesEqual(count.AllowedChoices, "1", "2", "3"), "AcceptableValueList 候选=档位（InvariantCulture 投影）");
            Assert(adapter.TrySet("com.example.avl", "Count", PluginConfigValue.IntegerValue(2)).Accepted, "档位写入接受");
            var bad = adapter.TrySet("com.example.avl", "Count", PluginConfigValue.IntegerValue(99));
            Assert(!bad.Accepted && bad.Reason == PluginConfigEditRejection.InvalidValue, "底层静默拒绝（值不变）→ 结构化 值不合法（Q70）");
            Assert(Find(adapter.CaptureConfigEntries("com.example.avl", config), "Count").Value.Integer64 == 2, "非法值不落盘、合法档位已生效");
        }

        // Q66：候选不可完整写回 → 不变成选择器；底层仍属基础类型则普通控件进草稿。
        private static void UnwritableCandidatesFallBackToPlainControl()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Numeric", 1, new ConfigDescription("数字。", null, new object[] { "Unturned.Cycle:ON|OFF" }));
            var numeric = Find(adapter.CaptureConfigEntries("com.example.fallback", config), "Numeric");
            Assert(numeric.Kind == PluginConfigValueKind.Integer && numeric.CanEdit, "候选不可写回 → 不画 Cycle，基础类型仍普通控件");
            Assert(numeric.AllowedChoices.Count == 0, "不可写回候选不投影档位");
            Assert(adapter.TrySet("com.example.fallback", "Numeric", PluginConfigValue.IntegerValue(3)).Accepted, "普通控件数值仍可写回");
        }

        // Q66（Spec R1 blocking 修复）：候选写回按真实 CLR 类型校验——byte 拒 300、
        // uint 拒 -1（标签候选是字符串，任一越界整组不画 Cycle，仍普通控件）。
        private static void CycleCandidatesRespectRealClrRange()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "ByteOk", (byte)1, new ConfigDescription("字节。", null, new object[] { "Unturned.Cycle:1|200" }));
            config.Bind("Game", "ByteOver", (byte)1, new ConfigDescription("字节越界。", null, new object[] { "Unturned.Cycle:1|300" }));
            config.Bind("Game", "UintNeg", 0u, new ConfigDescription("无符号。", null, new object[] { "Unturned.Cycle:0|-1" }));
            var entries = adapter.CaptureConfigEntries("com.example.range", config);
            Assert(ChoicesEqual(Find(entries, "ByteOk").AllowedChoices, "1", "200"), "byte 档位在域内 → Cycle");
            var over = Find(entries, "ByteOver");
            Assert(over.AllowedChoices.Count == 0 && over.CanEdit, "byte 档位 300 越界 → 不画 Cycle、仍普通控件");
            var neg = Find(entries, "UintNeg");
            Assert(neg.AllowedChoices.Count == 0 && neg.CanEdit, "uint 档位 -1 越界 → 不画 Cycle、仍普通控件");
        }

        // Q66「候选非空」下限：单档仍 Cycle（与 BUE Choice 单档同形，Q43 同构）。
        private static void SingleCandidateStillCycles()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Single", "A", new ConfigDescription("单档。", null, new object[] { "Unturned.Cycle:ONLY" }));
            Assert(Find(adapter.CaptureConfigEntries("com.example.single", config), "Single").AllowedChoices.Count == 1, "候选非空即 Cycle（单档）");
        }

        // R1 6.3：无冒号档位的 Cycle 标签回落 AcceptableValueList（UPM 快照同序）。
        private static void BareCycleTagFallsBackToAcceptableValueList()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Bare", "A", new ConfigDescription("裸标签。", new AcceptableValueList<string>("A", "B"), new object[] { "Unturned.Cycle" }));
            Assert(ChoicesEqual(Find(adapter.CaptureConfigEntries("com.example.bare", config), "Bare").AllowedChoices, "A", "B"), "裸 Cycle 标签回落 AcceptableValueList");
        }

        // Q69：未识别离散标签静默忽略，不变成选择器；底层仍属基础类型则普通控件进草稿。
        private static void UnrecognizedTagsAreSilentlyIgnored()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Items", "sword", new ConfigDescription("物品。", null,
                new object[] { "Unturned.ItemList:i_sword|i_plank", "Unturned.BlueprintList:bp1", "SomeCustom.Constraint:x" }));
            var items = Find(adapter.CaptureConfigEntries("com.example.tags", config), "Items");
            Assert(items.Kind == PluginConfigValueKind.String && items.CanEdit, "未识别标签静默忽略：基础类型仍普通控件");
            Assert(items.AllowedChoices.Count == 0, "未识别标签不变成选择器（缺席断言）");
        }

        // Q66：其它 CLR 类型 Unsupported 只读——Cycle/编辑都不开（枚举=BepInEx 可序列化
        // 但不在 bool/数字/字符串基础类型内，真实 Bind 构造，无反射）。
        private static void UnsupportedClrTypeStaysReadOnly()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Complex", SampleKind.Alpha, new ConfigDescription("枚举。", null, new object[] { "Unturned.Cycle:Alpha|Beta" }));
            var complex = Find(adapter.CaptureConfigEntries("com.example.unsupported", config), "Complex");
            Assert(complex.Kind == PluginConfigValueKind.Unsupported && !complex.CanEdit, "非基础类型（枚举）Unsupported 只读");
            Assert(complex.AllowedChoices.Count == 0, "Unsupported 类型不开 Cycle（Cycle 仅当值类型受支持）");
            Assert(!adapter.TrySet("com.example.unsupported", "Complex", PluginConfigValue.StringValue("Beta")).Accepted, "Unsupported 条目拒绝写入");
        }

        // Q70：结构化失败结果——插件卸载或条目失效 / 文件权限锁定写入失败 / 校验转换值域失败。
        private static void StructuredFailureClassification()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "K", "v", new ConfigDescription(string.Empty, null, null));
            adapter.CaptureConfigEntries("com.example.failures", config);
            var missing = adapter.TrySet("com.example.missing", "K", PluginConfigValue.StringValue("x"));
            Assert(!missing.Accepted && missing.Reason == PluginConfigEditRejection.PluginNotFound, "未捕获 GUID → PluginNotFound（插件已卸载）");
            var missingEntry = adapter.TrySet("com.example.failures", "Nope", PluginConfigValue.StringValue("x"));
            Assert(!missingEntry.Accepted && missingEntry.Reason == PluginConfigEditRejection.EntryNotFound, "条目失效 → EntryNotFound（插件已卸载类）");
            // ReadOnly 的场景在 BepInEx 5.4.23 不可构造（ConfigFile.IsReadOnly 是恒
            // false 的桩：无后备字段、无 setter），其文案映射由 ClientUi.Tests 模型组
            // 钉住；adapter 层的真实「无法写入」路径由下面的文件锁组覆盖。

            var lockPath = TempConfigPath();
            var lockConfig = new ConfigFile(lockPath, false);
            lockConfig.Bind("Game", "K", "v", new ConfigDescription(string.Empty, null, null));
            adapter.CaptureConfigEntries("com.example.locked", lockConfig);
            using (File.Open(lockPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var blocked = adapter.TrySet("com.example.locked", "K", PluginConfigValue.StringValue("x2"));
                Assert(!blocked.Accepted && blocked.Reason == PluginConfigEditRejection.PersistenceFailed,
                    "文件被锁 → PersistenceFailed（配置文件无法写入，Q70）");
            }
        }

        // Q68：检测到 com.trae.pluginmanager——识别标签 ≠ 剥夺能力，adapter 不按 GUID 禁编。
        private static void UpmGuidCapturedAndEditable()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Manager", "Visibility", true, new ConfigDescription("显示。", null, null));
            var entries = adapter.CaptureConfigEntries("com.trae.pluginmanager", config);
            Assert(Find(entries, "Visibility").CanEdit, "UPM GUID 不在 adapter 层禁编");
            Assert(adapter.TrySet("com.trae.pluginmanager", "Visibility", PluginConfigValue.BooleanValue(false)).Accepted, "UPM 受支持 cfg 照常写回");
        }

        // Spec R2 blocking 修复：ulong 全域可编（ADR-0002 #9 数字不折半）——超 long
        // 的值走无符号载体采集/写回，逐位保留；两载体写同一条目同值合法。
        private static void UnsignedFullRangeStaysEditable()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "Big", 18446744073709551615UL, new ConfigDescription("大数。", null,
                new object[] { "Unturned.Cycle:18446744073709551614|18446744073709551615" }));
            var big = Find(adapter.CaptureConfigEntries("com.example.ulong", config), "Big");
            Assert(big.Kind == PluginConfigValueKind.Integer && big.CanEdit, "ulong 超 long 域仍是可编数字（不再 Unsupported）");
            Assert(big.Value.IsUnsigned && big.Value.Unsigned64 == 18446744073709551615UL, "无符号载体逐位保留");
            Assert(ChoicesEqual(big.AllowedChoices, "18446744073709551614", "18446744073709551615"), "超 long 档位候选照常投影");
            Assert(adapter.TrySet("com.example.ulong", "Big", PluginConfigValue.ULongValue(18446744073709551614UL)).Accepted, "无符号档位写入接受");
            Assert(Find(adapter.CaptureConfigEntries("com.example.ulong", config), "Big").Value.Unsigned64 == 18446744073709551614UL, "档位写回逐位保留");

            config.Bind("Game", "Small", 5UL, new ConfigDescription("小数。", null, null));
            var small = Find(adapter.CaptureConfigEntries("com.example.ulong", config), "Small");
            Assert(small.Value.IsUnsigned && small.Value.Unsigned64 == 5UL, "ulong 小值同走无符号载体");
            Assert(adapter.TrySet("com.example.ulong", "Small", PluginConfigValue.IntegerValue(6)).Accepted, "有符号载体写回 ulong 条目（序列化同值）");
        }

        // Q67：行级「需要重启」=该项固有属性，照常采集（顶部徽章归草稿保存成功项）。
        private static void RequiresRestartRowAttributePinned()
        {
            var adapter = new LoadedPluginCatalogAdapter();
            var config = NewConfig();
            config.Bind("Game", "RestartFlag", 1, new ConfigDescription("重启。", null, new object[] { "RequiresRestart" }));
            Assert(Find(adapter.CaptureConfigEntries("com.example.restart", config), "RestartFlag").RequiresRestart, "RequiresRestart 行级旗标照常采集");
        }

        // ── helpers ──

        private static ConfigFile NewConfig()
        {
            var path = TempConfigPath();
            return new ConfigFile(path, false);
        }

        private static string TempConfigPath()
        {
            var path = Path.Combine(Path.GetTempPath(), "bue-v4-08-" + Guid.NewGuid().ToString("N") + ".cfg");
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

        private static bool ChoicesEqual(IReadOnlyList<string> choices, params string[] expected)
        {
            if (choices == null || choices.Count != expected.Length) return false;
            for (var index = 0; index < expected.Length; index++)
                if (!string.Equals(choices[index], expected[index], StringComparison.Ordinal)) return false;
            return true;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
