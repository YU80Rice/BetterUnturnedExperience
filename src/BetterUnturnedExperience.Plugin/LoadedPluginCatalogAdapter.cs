using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BetterUnturnedExperience.ClientUi.Internal;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// Adapts the already-loaded Chainloader registry into the BUE panel model.
    /// It never scans or loads assemblies; ConfigEntry writes stay on the public
    /// ConfigFile surface and unsupported types remain read-only.
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
                    var entries = new List<PluginConfigEntryView>();
                    var instance = info.Instance;
                    var config = instance == null ? null : instance.Config;
                    if (config != null)
                    {
                        configFiles[info.Metadata.GUID] = config;
                        foreach (var entry in ((IDictionary<ConfigDefinition, ConfigEntryBase>)config).Values)
                        {
                            var definition = entry == null ? null : entry.Definition;
                            if (definition == null || entry == null) continue;
                            var value = HasDiscreteConstraint(entry) ? PluginConfigValue.UnsupportedValue() : ToValue(entry.SettingType, entry.BoxedValue);
                            double? minimum;
                            double? maximum;
                            GetBounds(entry, out minimum, out maximum);
                            var maximumLength = entry.SettingType == typeof(string) ? 4096 : 0;
                            entries.Add(new PluginConfigEntryView(definition.Key, definition.Key, value.Kind,
                                value, RequiresRestart(entry), value.Kind != PluginConfigValueKind.Unsupported && !config.IsReadOnly,
                                minimum, maximum, maximumLength));
                        }
                    }
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

        public bool TrySet(string pluginGuid, string key, PluginConfigValue value)
        {
            ConfigFile config;
            if (!configFiles.TryGetValue(pluginGuid, out config) || config == null || config.IsReadOnly) return false;
            foreach (var entry in ((IDictionary<ConfigDefinition, ConfigEntryBase>)config).Values)
            {
                if (entry == null || entry.Definition == null || !string.Equals(entry.Definition.Key, key, StringComparison.Ordinal)) continue;
                if (entry == null || !CanEdit(entry.SettingType, value.Kind)) return false;
                var oldValue = string.Empty;
                try
                {
                    oldValue = entry.GetSerializedValue();
                    var submitted = ToSerialized(value);
                    entry.SetSerializedValue(submitted);
                    var actual = entry.GetSerializedValue();
                    if (!EquivalentSerialized(entry.SettingType, submitted, actual))
                    {
                        entry.SetSerializedValue(oldValue);
                        return false;
                    }
                    config.Save();
                    return true;
                }
                catch (Exception)
                {
                    try { entry.SetSerializedValue(oldValue); } catch (Exception) { }
                    return false;
                }
            }
            return false;
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
            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            {
                try { return PluginConfigValue.IntegerValue(Convert.ToInt64(value, CultureInfo.InvariantCulture)); } catch (Exception) { return PluginConfigValue.UnsupportedValue(); }
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
                case PluginConfigValueKind.Integer: return value.Integer64.ToString(CultureInfo.InvariantCulture);
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

        private static bool HasDiscreteConstraint(ConfigEntryBase entry)
        {
            var acceptable = entry == null || entry.Description == null ? null : entry.Description.AcceptableValues;
            return acceptable != null && acceptable.GetType().Name.IndexOf("AcceptableValueList", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
