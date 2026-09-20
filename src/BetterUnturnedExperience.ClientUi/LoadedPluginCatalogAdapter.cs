using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    /// <summary>
    /// Adapts the already-loaded Chainloader registry into the BUE panel model.
    /// It never scans or loads assemblies; ConfigEntry writes stay on the public
    /// ConfigFile surface and unsupported types remain read-only.
    /// DEV-V4-08 (V4-T7): captures ConfigDescription descriptions and Cycle
    /// candidates (Unturned.Cycle tag / AcceptableValueList) for supported base
    /// types, classifies write failures structurally (Q70), and leaves
    /// unrecognized discrete constraints as plain editable controls (Q66).
    /// POST-P4-04 (票 04): also captures the UPM category tag ("Unturned.Category:")
    /// and renders ItemList/BlueprintList/CreatureList as format-hinted string
    /// text rows — list tags outrank the cycle sources, no visual picker.
    /// </summary>
    internal sealed class LoadedPluginCatalogAdapter : IPluginConfigEditor
    {
        private readonly Dictionary<string, ConfigFile> configFiles = new Dictionary<string, ConfigFile>(StringComparer.Ordinal);

        internal IReadOnlyList<LoadedPluginDescriptor> CaptureLoadedPlugins()
        {
            var result = new List<LoadedPluginDescriptor>();
            configFiles.Clear();
            try
            {
                foreach (var pair in Chainloader.PluginInfos)
                {
                    var info = pair.Value;
                    if (info == null || info.Metadata == null) continue;
                    var instance = info.Instance;
                    var config = instance == null ? null : instance.Config;
                    var entries = CaptureConfigEntries(info.Metadata.GUID, config);
                    var version = info.Metadata.Version == null ? string.Empty : info.Metadata.Version.ToString();
                    result.Add(new LoadedPluginDescriptor(info.Metadata.GUID, info.Metadata.Name, version, entries));
                }
            }
            catch (Exception)
            {
                result.Clear();
                configFiles.Clear();
            }
            return result;
        }

        // DEV-V4-08: the per-plugin capture seam — CaptureLoadedPlugins hands
        // each instance ConfigFile here; a null config contributes no entries
        // and no TrySet registration.
        internal IReadOnlyList<PluginConfigEntryView> CaptureConfigEntries(string pluginGuid, ConfigFile config)
        {
            if (config == null) return new PluginConfigEntryView[0];
            configFiles[pluginGuid] = config;
            var entries = new List<PluginConfigEntryView>();
            foreach (var entry in ((IDictionary<ConfigDefinition, ConfigEntryBase>)config).Values)
            {
                var definition = entry == null ? null : entry.Definition;
                if (definition == null || entry == null) continue;
                var value = ToValue(entry.SettingType, entry.BoxedValue);
                double? minimum;
                double? maximum;
                GetBounds(entry, out minimum, out maximum);
                var maximumLength = entry.SettingType == typeof(string) ? 4096 : 0;
                var canEdit = value.Kind != PluginConfigValueKind.Unsupported && !config.IsReadOnly;
                // POST-P4-04: a recognized UPM list tag makes the row a plain
                // format-hinted String text row — precedence mirrors UPM, so
                // the Cycle tag / AcceptableValueList grant it NO choices.
                var listHint = ListFormatHint(entry);
                var allowedChoices = listHint.Length > 0 ? new string[0] : CycleCandidates(entry, value.Kind);
                entries.Add(new PluginConfigEntryView(definition.Key, definition.Key, value.Kind,
                    value, RequiresRestart(entry), canEdit, minimum, maximum, maximumLength,
                    DescriptionOf(entry), allowedChoices, CategoryOf(entry), listHint));
            }
            return entries;
        }

        public PluginConfigEditResult TrySet(string pluginGuid, string key, PluginConfigValue value)
        {
            ConfigFile config;
            if (!configFiles.TryGetValue(pluginGuid, out config) || config == null)
                return new PluginConfigEditResult(false, false, PluginConfigEditRejection.PluginNotFound);
            if (config.IsReadOnly)
                return new PluginConfigEditResult(false, false, PluginConfigEditRejection.ReadOnly);
            foreach (var entry in ((IDictionary<ConfigDefinition, ConfigEntryBase>)config).Values)
            {
                if (entry == null || entry.Definition == null || !string.Equals(entry.Definition.Key, key, StringComparison.Ordinal)) continue;
                if (!CanEdit(entry.SettingType, value.Kind))
                    return new PluginConfigEditResult(false, RequiresRestart(entry), PluginConfigEditRejection.UnsupportedType);
                var requiresRestart = RequiresRestart(entry);
                var oldValue = string.Empty;
                try
                {
                    oldValue = entry.GetSerializedValue();
                    var submitted = ToSerialized(value);
                    entry.SetSerializedValue(submitted);
                    var actual = entry.GetSerializedValue();
                    // BepInEx silently ignores values its constraint rejects
                    // (keeps the old value) — only the round-trip compare
                    // exposes that as a rejection (Q70 值不合法).
                    if (!EquivalentSerialized(entry.SettingType, submitted, actual))
                    {
                        entry.SetSerializedValue(oldValue);
                        return new PluginConfigEditResult(false, requiresRestart, PluginConfigEditRejection.InvalidValue);
                    }
                    config.Save();
                    return new PluginConfigEditResult(true, requiresRestart, PluginConfigEditRejection.None);
                }
                catch (ArgumentException)
                {
                    return RejectedAfterRestore(entry, oldValue, requiresRestart, PluginConfigEditRejection.InvalidValue);
                }
                catch (FormatException)
                {
                    return RejectedAfterRestore(entry, oldValue, requiresRestart, PluginConfigEditRejection.InvalidValue);
                }
                catch (Exception)
                {
                    return RejectedAfterRestore(entry, oldValue, requiresRestart, PluginConfigEditRejection.PersistenceFailed);
                }
            }
            return new PluginConfigEditResult(false, false, PluginConfigEditRejection.EntryNotFound);
        }

        private static PluginConfigEditResult RejectedAfterRestore(ConfigEntryBase entry, string oldValue,
            bool requiresRestart, PluginConfigEditRejection reason)
        {
            try { entry.SetSerializedValue(oldValue); } catch (Exception) { }
            return new PluginConfigEditResult(false, requiresRestart, reason);
        }

        private static bool CanEdit(Type type, PluginConfigValueKind kind)
        {
            return kind != PluginConfigValueKind.Unsupported &&
                ((type == typeof(bool) && kind == PluginConfigValueKind.Boolean) ||
                 ((type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)) && kind == PluginConfigValueKind.Integer) ||
                 ((type == typeof(float) || type == typeof(double) || type == typeof(decimal)) && kind == PluginConfigValueKind.Float) ||
                 (type == typeof(string) && kind == PluginConfigValueKind.String));
        }

        private static PluginConfigValue ToValue(Type type, object value)
        {
            if (type == typeof(bool)) return PluginConfigValue.BooleanValue(value is bool && (bool)value);
            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long))
            {
                try { return PluginConfigValue.IntegerValue(Convert.ToInt64(value, CultureInfo.InvariantCulture)); } catch (Exception) { return PluginConfigValue.UnsupportedValue(); }
            }
            if (type == typeof(ulong))
            {
                // 数字 per ADR-0002 #9 keeps its full 64-bit range: the signed
                // carrier cannot hold a ulong above long.MaxValue, so the
                // unsigned carrier does (IsUnsigned/Unsigned64).
                try { return PluginConfigValue.ULongValue(Convert.ToUInt64(value, CultureInfo.InvariantCulture)); } catch (Exception) { return PluginConfigValue.UnsupportedValue(); }
            }
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                try { return PluginConfigValue.FloatValue(Convert.ToDouble(value, CultureInfo.InvariantCulture)); } catch (Exception) { return PluginConfigValue.UnsupportedValue(); }
            }
            if (type == typeof(string)) return PluginConfigValue.StringValue(value as string ?? string.Empty);
            return PluginConfigValue.UnsupportedValue();
        }

        private static string ToSerialized(PluginConfigValue value)
        {
            switch (value.Kind)
            {
                case PluginConfigValueKind.Boolean: return value.Boolean.ToString(CultureInfo.InvariantCulture);
                case PluginConfigValueKind.Integer: return value.IsUnsigned ? value.Unsigned64.ToString(CultureInfo.InvariantCulture) : value.Integer64.ToString(CultureInfo.InvariantCulture);
                case PluginConfigValueKind.Float: return value.Float64.ToString(CultureInfo.InvariantCulture);
                case PluginConfigValueKind.String: return value.Text ?? string.Empty;
                default: return string.Empty;
            }
        }

        private static bool EquivalentSerialized(Type type, string expected, string actual)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal)) return true;
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                double expectedNumber;
                double actualNumber;
                return double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out expectedNumber)
                    && double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out actualNumber)
                    && Math.Abs(expectedNumber - actualNumber) <= 0.0000001d;
            }
            return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiresRestart(ConfigEntryBase entry)
        {
            return entry.Description != null && entry.Description.Tags != null && Array.Exists(entry.Description.Tags, x => string.Equals(Convert.ToString(x, CultureInfo.InvariantCulture), "RequiresRestart", StringComparison.OrdinalIgnoreCase));
        }

        private static void GetBounds(ConfigEntryBase entry, out double? minimum, out double? maximum)
        {
            minimum = null;
            maximum = null;
            var acceptable = entry.Description == null ? null : entry.Description.AcceptableValues;
            if (acceptable != null)
            {
                var minProperty = acceptable.GetType().GetProperty("MinValue");
                var maxProperty = acceptable.GetType().GetProperty("MaxValue");
                try { if (minProperty != null) minimum = Convert.ToDouble(minProperty.GetValue(acceptable, null), CultureInfo.InvariantCulture); } catch (Exception) { }
                try { if (maxProperty != null) maximum = Convert.ToDouble(maxProperty.GetValue(acceptable, null), CultureInfo.InvariantCulture); } catch (Exception) { }
            }
        }

        // Q65: the row description is the ConfigDescription's own text — raw at
        // capture (the panel projection truncates at 120, Q37); null stays an
        // empty string so the renderer skips the line (空不画).
        private static string DescriptionOf(ConfigEntryBase entry)
        {
            var description = entry.Description == null ? null : entry.Description.Description;
            return description ?? string.Empty;
        }

        // Q66: the Cycle SHAPE only when the value type is supported, the
        // constraint is a recognized Unturned.Cycle tag or AcceptableValueList,
        // and EVERY candidate is non-empty and convertible back into the base
        // kind. An unrecognized discrete constraint grants no choices (the row
        // stays a plain editable control); an unsupported type grants nothing.
        private static IReadOnlyList<string> CycleCandidates(ConfigEntryBase entry, PluginConfigValueKind kind)
        {
            if (kind == PluginConfigValueKind.Unsupported) return new string[0];
            IReadOnlyList<string> candidates = null;
            var tag = FindTag(entry, "Unturned.Cycle");
            if (tag != null)
            {
                var colon = tag.IndexOf(':');
                if (colon >= 0 && colon + 1 < tag.Length)
                    candidates = tag.Substring(colon + 1).Split('|');
                // A bare tag (no colon suffix) falls through to the
                // AcceptableValueList — the UPM snapshot's same order (R1 6.3).
            }
            if (candidates == null)
                candidates = AcceptableValueListCandidates(entry);
            if (candidates == null || candidates.Count == 0) return new string[0];
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (string.IsNullOrEmpty(candidate) || !WritableCandidate(entry.SettingType, candidate)) return new string[0];
            }
            return candidates;
        }

        private static IReadOnlyList<string> AcceptableValueListCandidates(ConfigEntryBase entry)
        {
            var acceptable = entry.Description == null ? null : entry.Description.AcceptableValues;
            if (acceptable == null) return null;
            if (acceptable.GetType().Name.IndexOf("AcceptableValueList", StringComparison.OrdinalIgnoreCase) < 0) return null;
            var acceptedProperty = acceptable.GetType().GetProperty("AcceptableValues");
            if (acceptedProperty == null) return null;
            var accepted = acceptedProperty.GetValue(acceptable, null) as IEnumerable;
            if (accepted == null) return null;
            var candidates = new List<string>();
            foreach (var value in accepted)
            {
                if (value == null) return null;
                candidates.Add(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
            return candidates;
        }

        // ===== POST-P4-04: UnturnedPluginManager (UPM) 分类与列表标签 =====
        // "Unturned.Category:<名>" selects the row's category (trailing/leading
        // blanks trimmed; an empty name falls back to the config section, and a
        // tagless entry groups by section too — 空分类归「通用」是投影侧规则).
        // ItemList / BlueprintList / CreatureList — bare or colon-suffixed —
        // turn the row into a format-hinted String TEXT row (值仍是字符串、编辑
        // 走草稿), never an icon/search picker. An unrecognized list-like tag
        // stays silently ignored (Q69): the string base keeps the row editable
        // and no hint is invented for it.

        private const string ItemListTag = "Unturned.ItemList";
        private const string BlueprintListTag = "Unturned.BlueprintList";
        private const string CreatureListTag = "Unturned.CreatureList";
        private const string CategoryTagPrefix = "Unturned.Category:";

        private const string ItemListHint = "值格式：物品ID, 物品ID, ...";
        private const string BlueprintListHint = "值格式：所属物品ID:配方编号, ...";
        private const string CreatureListHint = "值格式：Z:僵尸类型 或 A:动物资产ID, ...";

        // Fixed Item > Blueprint > Creature precedence (UPM's check order),
        // independent of the order the tags appear on one entry.
        private static string ListFormatHint(ConfigEntryBase entry)
        {
            if (HasListTag(entry, ItemListTag)) return ItemListHint;
            if (HasListTag(entry, BlueprintListTag)) return BlueprintListHint;
            if (HasListTag(entry, CreatureListTag)) return CreatureListHint;
            return string.Empty;
        }

        private static bool HasListTag(ConfigEntryBase entry, string tag)
        {
            var tags = entry.Description == null ? null : entry.Description.Tags;
            if (tags == null) return false;
            for (var index = 0; index < tags.Length; index++)
            {
                var text = tags[index] == null ? null : Convert.ToString(tags[index], CultureInfo.InvariantCulture);
                if (text == null) continue;
                if (string.Equals(text, tag, StringComparison.Ordinal)) return true;
                if (text.Length > tag.Length + 1 && text[tag.Length] == ':' && text.StartsWith(tag, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static string CategoryOf(ConfigEntryBase entry)
        {
            var tag = FindTag(entry, CategoryTagPrefix);
            if (tag != null)
            {
                var name = tag.Substring(CategoryTagPrefix.Length).Trim();
                if (name.Length > 0) return name;
            }
            var section = entry.Definition.Section;
            return section ?? string.Empty;
        }

        private static string FindTag(ConfigEntryBase entry, string prefix)
        {
            var tags = entry.Description == null ? null : entry.Description.Tags;
            if (tags == null) return null;
            for (var index = 0; index < tags.Length; index++)
            {
                var tag = tags[index] == null ? null : Convert.ToString(tags[index], CultureInfo.InvariantCulture);
                if (tag != null && tag.StartsWith(prefix, StringComparison.Ordinal)) return tag;
            }
            return null;
        }

        // Writability = each candidate converts back into the entry's REAL CLR
        // type (Q66 可完整写回): range and signedness are checked against the
        // concrete type (byte rejects 300, uint rejects -1), floats reject
        // NaN/Infinity, and the parse rules stay the ones the panel's draft
        // edit applies — a Cycle the adapter grants never dead-ends in the draft.
        private static bool WritableCandidate(Type type, string candidate)
        {
            if (type == typeof(bool))
            {
                bool boolean;
                return bool.TryParse(candidate, out boolean);
            }
            if (type == typeof(string)) return true;
            if (type == typeof(float))
            {
                float single;
                return float.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out single)
                    && !float.IsNaN(single) && !float.IsInfinity(single);
            }
            if (type == typeof(double))
            {
                double number;
                return double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                    && !double.IsNaN(number) && !double.IsInfinity(number);
            }
            if (type == typeof(decimal))
            {
                decimal precise;
                return decimal.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out precise);
            }
            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            {
                try
                {
                    Convert.ChangeType(candidate, type, CultureInfo.InvariantCulture);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }
    }
}
