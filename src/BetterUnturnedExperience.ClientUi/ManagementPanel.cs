using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal enum ManagementSortOrder : byte
    {
        NameAscending,
        NameDescending
    }

    internal enum ManagementEntryKind : byte
    {
        BueFeature,
        ExternalPlugin
    }

    internal readonly struct BueFeatureManagementEntry
    {
        internal FeatureId Feature { get; }
        internal string DisplayName { get; }
        internal string Version { get; }
        internal FeatureState State { get; }
        internal FeaturePresentationView Presentation { get; }
        internal FeatureSettingsSnapshot Settings { get; }

        internal BueFeatureManagementEntry(FeatureId feature, string displayName, string version, FeatureState state,
            FeaturePresentationView presentation, FeatureSettingsSnapshot settings)
        {
            Feature = feature;
            DisplayName = displayName ?? string.Empty;
            Version = version ?? string.Empty;
            State = state;
            Presentation = presentation;
            Settings = settings;
        }
    }

    internal enum PluginConfigValueKind : byte
    {
        Boolean,
        Integer,
        Float,
        String,
        Unsupported
    }

    internal readonly struct PluginConfigValue
    {
        internal PluginConfigValueKind Kind { get; }
        internal bool Boolean { get; }
        internal long Integer { get; }
        internal float Float { get; }
        internal long Integer64 { get; }
        internal double Float64 { get; }
        internal string Text { get; }

        private PluginConfigValue(PluginConfigValueKind kind, bool boolean, long integer, double @float, string text)
        {
            Kind = kind;
            Boolean = boolean;
            Integer = integer;
            Float = (float)@float;
            Integer64 = integer;
            Float64 = @float;
            Text = text ?? string.Empty;
        }

        internal static PluginConfigValue BooleanValue(bool value) { return new PluginConfigValue(PluginConfigValueKind.Boolean, value, 0, 0f, string.Empty); }
        internal static PluginConfigValue IntegerValue(long value) { return new PluginConfigValue(PluginConfigValueKind.Integer, false, value, 0f, string.Empty); }
        internal static PluginConfigValue FloatValue(double value) { return new PluginConfigValue(PluginConfigValueKind.Float, false, 0, value, string.Empty); }
        internal static PluginConfigValue StringValue(string value) { return new PluginConfigValue(PluginConfigValueKind.String, false, 0, 0f, value); }
        internal static PluginConfigValue UnsupportedValue() { return new PluginConfigValue(PluginConfigValueKind.Unsupported, false, 0, 0f, string.Empty); }
    }

    // Row widget selector for external-plugin config entries, resolved by the
    // catalog adapter from the UnturnedPluginManager (UPM) compatibility tags
    // ("Unturned.Cycle" / "Unturned.ItemList" / "Unturned.BlueprintList" /
    // "Unturned.CreatureList"). Field is the plain text editor fallback.
    internal enum PluginConfigControlKind : byte
    {
        Field,
        Toggle,
        Cycle,
        List
    }

    internal readonly struct PluginConfigEntryView
    {
        internal string Key { get; }
        internal string DisplayName { get; }
        internal PluginConfigValueKind Kind { get; }
        internal PluginConfigValue Value { get; }
        internal bool RequiresRestart { get; }
        internal bool CanEdit { get; }
        internal double? Minimum { get; }
        internal double? Maximum { get; }
        internal int MaximumLength { get; }

        // UPM compatibility surface: description text and the category group
        // ("Unturned.Category:<name>" tag, falling back to the config section)
        // drive the details rendering; CycleOptions feeds the cycle stepper.
        internal string Description { get; }
        internal string Category { get; }
        internal PluginConfigControlKind Control { get; }
        internal IReadOnlyList<string> CycleOptions { get; }
        internal string ControlHint { get; }

        internal PluginConfigEntryView(string key, string displayName, PluginConfigValueKind kind, PluginConfigValue value,
            bool requiresRestart, bool canEdit, double? minimum = null, double? maximum = null, int maximumLength = 4096,
            string description = null, string category = null, PluginConfigControlKind control = PluginConfigControlKind.Field,
            IReadOnlyList<string> cycleOptions = null, string controlHint = null)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
            Value = value;
            RequiresRestart = requiresRestart;
            CanEdit = canEdit && kind != PluginConfigValueKind.Unsupported;
            Minimum = minimum;
            Maximum = maximum;
            MaximumLength = maximumLength;
            Description = description ?? string.Empty;
            Category = category ?? string.Empty;
            Control = control;
            CycleOptions = cycleOptions;
            ControlHint = controlHint ?? string.Empty;
        }
    }

    // Pure-C# support for the UPM compatibility tags so the model and the
    // native panel share one implementation and the tests can exercise it
    // without the host config API or the game assemblies.
    internal static class PluginConfigSupport
    {
        // direction > 0 steps to the next option, direction < 0 to the
        // previous one, both wrapping around. An unknown current value lands
        // on the first option stepping forward and on the last stepping back.
        internal static string NextCycleOption(IReadOnlyList<string> options, string current, int direction)
        {
            if (options == null || options.Count == 0) return null;
            var step = direction < 0 ? -1 : 1;
            var index = -1;
            for (var i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i], current, StringComparison.Ordinal))
                {
                    index = i;
                    break;
                }
            }
            if (index < 0) return step > 0 ? options[0] : options[options.Count - 1];
            index = (index + step + options.Count) % options.Count;
            return options[index];
        }

        // Distinct categories in first-seen order; empty categories collapse
        // into the shared default group so they still render a usable chip.
        internal const string DefaultCategory = "通用";

        internal static IReadOnlyList<string> CollectCategories(IReadOnlyList<PluginConfigEntryView> entries)
        {
            var result = new List<string>();
            if (entries == null) return result;
            for (var index = 0; index < entries.Count; index++)
            {
                var category = string.IsNullOrWhiteSpace(entries[index].Category) ? DefaultCategory : entries[index].Category;
                if (!result.Contains(category)) result.Add(category);
            }
            return result;
        }
    }

    internal sealed class LoadedPluginDescriptor
    {
        internal string Guid { get; }
        internal string DisplayName { get; }
        internal string Version { get; }
        internal IReadOnlyList<PluginConfigEntryView> ConfigEntries { get; }

        internal LoadedPluginDescriptor(string guid, string displayName, string version, IReadOnlyList<PluginConfigEntryView> configEntries)
        {
            if (string.IsNullOrWhiteSpace(guid)) throw new ArgumentException("Plugin GUID is required.", nameof(guid));
            Guid = guid;
            DisplayName = displayName ?? string.Empty;
            Version = version ?? string.Empty;
            ConfigEntries = new ReadOnlyCollection<PluginConfigEntryView>((configEntries ?? new PluginConfigEntryView[0]).ToArray());
        }
    }

    internal readonly struct ManagementEntryView
    {
        internal ManagementEntryKind Kind { get; }
        internal string StableId { get; }
        internal string DisplayName { get; }
        internal string Version { get; }
        internal FeatureState FeatureState { get; }
        internal FeaturePresentationView Presentation { get; }
        internal IReadOnlyList<SettingEntryView> BueSettings { get; }
        internal IReadOnlyList<PluginConfigEntryView> PluginConfig { get; }
        internal bool IsFavorite { get; }

        internal ManagementEntryView(ManagementEntryKind kind, string stableId, string displayName, string version,
            FeatureState featureState, FeaturePresentationView presentation, IReadOnlyList<SettingEntryView> bueSettings,
            IReadOnlyList<PluginConfigEntryView> pluginConfig, bool isFavorite)
        {
            Kind = kind;
            StableId = stableId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Version = version ?? string.Empty;
            FeatureState = featureState;
            Presentation = presentation;
            BueSettings = bueSettings ?? new ReadOnlyCollection<SettingEntryView>(new SettingEntryView[0]);
            PluginConfig = pluginConfig ?? new ReadOnlyCollection<PluginConfigEntryView>(new PluginConfigEntryView[0]);
            IsFavorite = isFavorite;
        }
    }

    internal sealed class ManagementPanelPreferences
    {
        internal static readonly ManagementPanelPreferences Empty = new ManagementPanelPreferences(ManagementSortOrder.NameAscending, new string[0]);
        internal ManagementSortOrder SortOrder { get; }
        internal IReadOnlyList<string> FavoriteIds { get; }

        internal ManagementPanelPreferences(ManagementSortOrder sortOrder, IReadOnlyList<string> favoriteIds)
        {
            SortOrder = sortOrder;
            FavoriteIds = new ReadOnlyCollection<string>((favoriteIds ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray());
        }
    }

    internal interface IManagementPanelPreferencesStore
    {
        ManagementPanelPreferences Load();
        void Save(ManagementPanelPreferences value);
    }

    internal sealed class FileManagementPanelPreferencesStore : IManagementPanelPreferencesStore
    {
        private readonly string path;

        internal FileManagementPanelPreferencesStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Preferences path is required.", nameof(path));
            this.path = path;
        }

        public ManagementPanelPreferences Load()
        {
            try
            {
                if (!File.Exists(path)) return ManagementPanelPreferences.Empty;
                var lines = File.ReadAllLines(path);
                var sort = ManagementSortOrder.NameAscending;
                var favorites = new List<string>();
                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index] ?? string.Empty;
                    if (line == "sort=Z-A") sort = ManagementSortOrder.NameDescending;
                    else if (line == "sort=A-Z") sort = ManagementSortOrder.NameAscending;
                    else if (line.StartsWith("favorite=", StringComparison.Ordinal))
                    {
                        var encoded = line.Substring("favorite=".Length);
                        try { favorites.Add(Uri.UnescapeDataString(encoded)); }
                        catch (UriFormatException) { }
                    }
                }
                return new ManagementPanelPreferences(sort, favorites);
            }
            catch (IOException)
            {
                return ManagementPanelPreferences.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return ManagementPanelPreferences.Empty;
            }
        }

        public void Save(ManagementPanelPreferences value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temporary = path + ".tmp";
            var lines = new List<string> { value.SortOrder == ManagementSortOrder.NameDescending ? "sort=Z-A" : "sort=A-Z" };
            for (var index = 0; index < value.FavoriteIds.Count; index++)
            {
                lines.Add("favorite=" + Uri.EscapeDataString(value.FavoriteIds[index]));
            }
            File.WriteAllLines(temporary, lines);
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(temporary, path, null);
                    return;
                }
                catch (PlatformNotSupportedException) { }
                catch (IOException) { }
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }
    }

    internal interface IBueSettingsEditor
    {
        FeatureSettingsSnapshot GetSnapshot(FeatureId feature);
        SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation);
    }

    internal interface IPluginConfigEditor
    {
        bool TrySet(string pluginGuid, string key, PluginConfigValue value);
    }

    internal enum PluginConfigEditRejection : byte
    {
        None,
        PluginNotFound,
        EntryNotFound,
        UnsupportedType,
        ReadOnly,
        InvalidValue,
        PersistenceFailed
    }

    internal readonly struct PluginConfigEditResult
    {
        internal bool Accepted { get; }
        internal bool RequiresRestart { get; }
        internal PluginConfigEditRejection Reason { get; }
        internal PluginConfigEditResult(bool accepted, bool requiresRestart, PluginConfigEditRejection reason)
        {
            Accepted = accepted;
            RequiresRestart = requiresRestart;
            Reason = reason;
        }
    }

    internal sealed class ManagementPanelModel
    {
        private readonly IManagementPanelPreferencesStore preferencesStore;
        private readonly IBueSettingsEditor bueSettingsEditor;
        private readonly IPluginConfigEditor pluginConfigEditor;
        private readonly Dictionary<string, BueFeatureManagementEntry> features = new Dictionary<string, BueFeatureManagementEntry>(StringComparer.Ordinal);
        private readonly Dictionary<string, LoadedPluginDescriptor> plugins = new Dictionary<string, LoadedPluginDescriptor>(StringComparer.Ordinal);
        private readonly List<string> favoriteIds;
        private ManagementSortOrder sortOrder;
        private bool preferencesLoaded;
        private bool externalManagerDetected;
        private string doubleInstallNotice = string.Empty;

        internal ManagementPanelModel(IManagementPanelPreferencesStore preferencesStore, IBueSettingsEditor bueSettingsEditor,
            IPluginConfigEditor pluginConfigEditor = null)
        {
            this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
            this.bueSettingsEditor = bueSettingsEditor ?? throw new ArgumentNullException(nameof(bueSettingsEditor));
            this.pluginConfigEditor = pluginConfigEditor;
            favoriteIds = new List<string>();
            sortOrder = ManagementSortOrder.NameAscending;
        }

        internal ManagementSortOrder SortOrder { get { EnsurePreferencesLoaded(); return sortOrder; } }
        internal bool ExternalManagerDetected { get { return externalManagerDetected; } }
        internal string CompatibilityNotice { get { return externalManagerDetected ? "检测到外部插件管理器；BUE 不会重复修改其状态。" : string.Empty; } }

        // DEV-V2-23: the double-install self-check notice (BUE-PLATFORM-001).
        // Empty = no conflict. Set once by the host after the Awake self-check
        // and intentionally untouched by Refresh — the finding reflects the
        // loaded-assembly state at startup, not the catalog snapshot.
        internal string DoubleInstallNotice { get { return doubleInstallNotice; } }

        internal void SetDoubleInstallNotice(string value)
        {
            doubleInstallNotice = value ?? string.Empty;
        }

        internal void SetExternalManagerDetected(bool detected)
        {
            externalManagerDetected = detected;
        }

        internal void Refresh(IEnumerable<BueFeatureManagementEntry> bueFeatures, IEnumerable<LoadedPluginDescriptor> loadedPlugins)
        {
            EnsurePreferencesLoaded();
            features.Clear();
            plugins.Clear();
            foreach (var feature in bueFeatures ?? Enumerable.Empty<BueFeatureManagementEntry>())
            {
                if (string.IsNullOrWhiteSpace(feature.Feature.Value)) continue;
                features[feature.Feature.Value] = feature;
            }
            foreach (var plugin in loadedPlugins ?? Enumerable.Empty<LoadedPluginDescriptor>())
            {
                if (plugin == null) continue;
                plugins[plugin.Guid] = plugin;
            }
            PruneFavorites();
        }

        internal IReadOnlyList<ManagementEntryView> GetEntries()
        {
            EnsurePreferencesLoaded();
            var all = new List<ManagementEntryView>();
            foreach (var feature in features.Values)
            {
                all.Add(new ManagementEntryView(ManagementEntryKind.BueFeature, feature.Feature.Value, feature.DisplayName,
                    feature.Version, feature.State, feature.Presentation, VisibleSettings(feature.Settings), null,
                    favoriteIds.Contains(feature.Feature.Value, StringComparer.Ordinal)));
            }
            foreach (var plugin in plugins.Values)
            {
                all.Add(new ManagementEntryView(ManagementEntryKind.ExternalPlugin, plugin.Guid, plugin.DisplayName,
                    plugin.Version, default(FeatureState), default(FeaturePresentationView), null, plugin.ConfigEntries,
                    favoriteIds.Contains(plugin.Guid, StringComparer.Ordinal)));
            }

            var favoriteSet = new HashSet<string>(favoriteIds, StringComparer.Ordinal);
            var orderedFavorites = all.Where(x => favoriteSet.Contains(x.StableId))
                .OrderBy(x => favoriteIds.IndexOf(x.StableId)).ToList();
            var others = all.Where(x => !favoriteSet.Contains(x.StableId));
            others = sortOrder == ManagementSortOrder.NameAscending
                ? others.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.StableId, StringComparer.Ordinal)
                : others.OrderByDescending(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.StableId, StringComparer.Ordinal);
            orderedFavorites.AddRange(others);
            return new ReadOnlyCollection<ManagementEntryView>(orderedFavorites);
        }

        internal bool ToggleFavorite(string stableId)
        {
            EnsurePreferencesLoaded();
            if (!ContainsStableId(stableId)) return false;
            var index = favoriteIds.IndexOf(stableId);
            if (index >= 0) favoriteIds.RemoveAt(index);
            else favoriteIds.Add(stableId);
            SavePreferences();
            return true;
        }

        internal void SetSortOrder(ManagementSortOrder value)
        {
            EnsurePreferencesLoaded();
            if (sortOrder == value) return;
            sortOrder = value;
            SavePreferences();
        }

        internal SettingChangeResult TryEditBueSetting(FeatureId feature, string settingId, PluginConfigValue value)
        {
            EnsurePreferencesLoaded();
            if (!features.ContainsKey(feature.Value)) return new SettingChangeResult(false, FrameworkErrorCode.SettingUnknown, 0, default(FeatureSettingsSnapshot));
            var snapshot = features[feature.Value].Settings;
            var mutation = ToSettingMutation(settingId, value);
            var result = bueSettingsEditor.Apply(feature, snapshot.Revision, mutation);
            if (result.Accepted)
            {
                var current = features[feature.Value];
                features[feature.Value] = new BueFeatureManagementEntry(current.Feature, current.DisplayName, current.Version,
                    current.State, current.Presentation, result.Snapshot);
            }
            return result;
        }

        /// <summary>
        /// DEV-V3-06 (03 具名移交「面板按钮接线随 06 动态路由落地」): the panel
        /// enable/disable COMMAND adapter. The model forwards the command to
        /// the host's ONE seam (BueFeatureStartRuntime.SetFeatureEnabled,
        /// bound by the composition root) — the state machine stays with the
        /// host, the panel keeps no private enable ledger, and entries
        /// refresh to the projected truth on the next panel refresh. Unbound
        /// (no host wired) answers false honestly. The native button UI is
        /// the named deferral of this ticket (随 09 实机面).
        /// </summary>
        internal System.Func<FeatureId, bool, bool> FeatureToggleHandler;

        internal bool TryToggleFeature(FeatureId feature, bool enabled)
        {
            EnsurePreferencesLoaded();
            if (!features.ContainsKey(feature.Value)) return false;
            var handler = FeatureToggleHandler;
            if (handler == null) return false;
            return handler(feature, enabled);
        }

        internal PluginConfigEditResult TryEditPluginConfig(string pluginGuid, string key, string rawValue)
        {
            EnsurePreferencesLoaded();
            if (pluginConfigEditor == null || !plugins.ContainsKey(pluginGuid)) return new PluginConfigEditResult(false, false, PluginConfigEditRejection.PluginNotFound);
            var plugin = plugins[pluginGuid];
            var entries = plugin.ConfigEntries.ToArray();
            var index = Array.FindIndex(entries, x => string.Equals(x.Key, key, StringComparison.Ordinal));
            if (index < 0) return new PluginConfigEditResult(false, false, PluginConfigEditRejection.EntryNotFound);
            var entry = entries[index];
            if (entry.Kind == PluginConfigValueKind.Unsupported) return new PluginConfigEditResult(false, false, PluginConfigEditRejection.UnsupportedType);
            if (!entry.CanEdit) return new PluginConfigEditResult(false, entry.RequiresRestart, PluginConfigEditRejection.ReadOnly);
            PluginConfigValue value;
            if (!TryParse(entry.Kind, rawValue, out value)) return new PluginConfigEditResult(false, entry.RequiresRestart, PluginConfigEditRejection.InvalidValue);
            if (!WithinBounds(entry, value)) return new PluginConfigEditResult(false, entry.RequiresRestart, PluginConfigEditRejection.InvalidValue);
            if (!pluginConfigEditor.TrySet(pluginGuid, key, value)) return new PluginConfigEditResult(false, entry.RequiresRestart, PluginConfigEditRejection.PersistenceFailed);
            entries[index] = new PluginConfigEntryView(entry.Key, entry.DisplayName, entry.Kind, value, entry.RequiresRestart, entry.CanEdit,
                entry.Minimum, entry.Maximum, entry.MaximumLength, entry.Description, entry.Category, entry.Control, entry.CycleOptions, entry.ControlHint);
            plugins[pluginGuid] = new LoadedPluginDescriptor(plugin.Guid, plugin.DisplayName, plugin.Version, entries);
            return new PluginConfigEditResult(true, entry.RequiresRestart, PluginConfigEditRejection.None);
        }

        private void EnsurePreferencesLoaded()
        {
            if (preferencesLoaded) return;
            preferencesLoaded = true;
            var value = preferencesStore.Load() ?? ManagementPanelPreferences.Empty;
            sortOrder = value.SortOrder;
            favoriteIds.AddRange(value.FavoriteIds);
        }

        private void SavePreferences()
        {
            preferencesStore.Save(new ManagementPanelPreferences(sortOrder, favoriteIds));
        }

        private void PruneFavorites()
        {
            for (var index = favoriteIds.Count - 1; index >= 0; index--)
            {
                if (!ContainsStableId(favoriteIds[index])) favoriteIds.RemoveAt(index);
            }
            SavePreferences();
        }

        private bool ContainsStableId(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && (features.ContainsKey(id) || plugins.ContainsKey(id));
        }

        private static IReadOnlyList<SettingEntryView> VisibleSettings(FeatureSettingsSnapshot snapshot)
        {
            if (snapshot.Entries == null) return new ReadOnlyCollection<SettingEntryView>(new SettingEntryView[0]);
            return new ReadOnlyCollection<SettingEntryView>(snapshot.Entries.Where(x => x.IsVisible).ToArray());
        }

        private static SettingMutation ToSettingMutation(string id, PluginConfigValue value)
        {
            SettingValue setting;
            switch (value.Kind)
            {
                case PluginConfigValueKind.Boolean: setting = SettingValue.Toggle(value.Boolean); break;
                case PluginConfigValueKind.Integer:
                    setting = value.Integer < int.MinValue || value.Integer > int.MaxValue
                        ? SettingValue.TextValue(value.Integer.ToString(CultureInfo.InvariantCulture))
                        : SettingValue.IntegerValue((int)value.Integer);
                    break;
                case PluginConfigValueKind.Float: setting = SettingValue.FloatValue(value.Float); break;
                default: setting = SettingValue.TextValue(value.Text); break;
            }
            return new SettingMutation(id, setting);
        }

        private static bool TryParse(PluginConfigValueKind kind, string raw, out PluginConfigValue value)
        {
            value = PluginConfigValue.UnsupportedValue();
            switch (kind)
            {
                case PluginConfigValueKind.Boolean:
                    bool boolean;
                    if (!bool.TryParse(raw, out boolean)) return false;
                    value = PluginConfigValue.BooleanValue(boolean);
                    return true;
                case PluginConfigValueKind.Integer:
                    long integer;
                    if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return false;
                    value = PluginConfigValue.IntegerValue(integer);
                    return true;
                case PluginConfigValueKind.Float:
                    double @float;
                    if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out @float) || double.IsNaN(@float) || double.IsInfinity(@float)) return false;
                    value = PluginConfigValue.FloatValue(@float);
                    return true;
                case PluginConfigValueKind.String:
                    value = PluginConfigValue.StringValue(raw ?? string.Empty);
                    return true;
                default:
                    return false;
            }
        }

        private static bool WithinBounds(PluginConfigEntryView entry, PluginConfigValue value)
        {
            if (value.Kind == PluginConfigValueKind.String) return (value.Text ?? string.Empty).Length <= entry.MaximumLength;
            var number = value.Kind == PluginConfigValueKind.Integer ? (double)value.Integer64 : value.Kind == PluginConfigValueKind.Float ? value.Float64 : double.NaN;
            if (!double.IsNaN(number) && entry.Minimum.HasValue && number < entry.Minimum.Value) return false;
            if (!double.IsNaN(number) && entry.Maximum.HasValue && number > entry.Maximum.Value) return false;
            return true;
        }
    }

    internal interface IManagementPanelMount
    {
        void Mount();
        void Unmount();
    }

    internal enum ManagementPanelLifecycleState : byte
    {
        Detached,
        Attached,
        Destroyed
    }

    internal sealed class ManagementPanelLifecycle
    {
        private IManagementPanelMount mount;
        private ManagementPanelLifecycleState state;

        internal ManagementPanelLifecycleState State { get { return state; } }

        internal void Attach(IManagementPanelMount next)
        {
            if (state == ManagementPanelLifecycleState.Destroyed || next == null) return;
            if (ReferenceEquals(mount, next)) return;
            if (mount != null) mount.Unmount();
            mount = next;
            mount.Mount();
            state = ManagementPanelLifecycleState.Attached;
        }

        internal void Detach()
        {
            if (mount == null || state == ManagementPanelLifecycleState.Destroyed) return;
            mount.Unmount();
            mount = null;
            state = ManagementPanelLifecycleState.Detached;
        }

        internal void Destroy()
        {
            if (state == ManagementPanelLifecycleState.Destroyed) return;
            if (mount != null)
            {
                mount.Unmount();
                mount = null;
            }
            state = ManagementPanelLifecycleState.Destroyed;
        }
    }

    internal sealed class ManagementPanelMenuBridge
    {
        private readonly ManagementPanelLifecycle lifecycle;
        internal bool HasMainMenuEntry { get; private set; }
        internal bool HasPauseMenuEntry { get; private set; }
        internal ManagementPanelMenuBridge(ManagementPanelLifecycle lifecycle) { this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle)); }
        internal void AttachMainMenu() { HasMainMenuEntry = true; }
        internal void AttachPauseMenu() { HasPauseMenuEntry = true; }
        internal void OnUiTreeRebuilt(IManagementPanelMount mount) { lifecycle.Attach(mount); }
        internal void Destroy() { HasMainMenuEntry = false; HasPauseMenuEntry = false; lifecycle.Destroy(); }
    }
}
