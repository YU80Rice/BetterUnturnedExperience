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

        internal PluginConfigEntryView(string key, string displayName, PluginConfigValueKind kind, PluginConfigValue value,
            bool requiresRestart, bool canEdit, double? minimum = null, double? maximum = null, int maximumLength = 4096)
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
        // DEV-V4-01: the atomic batch submit seam. "保存配置" routes ALL of a
        // feature's draft settings through ONE call so the authoritative
        // source is all-or-nothing and the revision advances exactly once
        // (设置提交原子性 / V4-T1). Implementers map this onto a single
        // SettingsRuntime.Submit — never one Submit per field.
        SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, IReadOnlyList<SettingMutation> mutations);
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

    // DEV-V4-01: unsaved-draft and configuration-save model vocabulary
    // (spec「未保存草稿与配置保存」/ V4-T2 裁决). The draft is the panel
    // adapter's session state — it never becomes a second source of truth;
    // only "保存配置" (SaveDraft) or the confirmation dialog's 保存 hands the
    // pending values to the authoritative sources (SettingsRuntime, external
    // ConfigEntry, SetFeatureEnabled). These three types are pure ClientUi
    // model surface (public ≠ 契约): no SDK/contract member is added.

    // The consequence of the confirm dialog's three buttons (V4-T2 Q22/Q34).
    internal enum PanelConfirmChoice : byte { Save, Discard, Cancel }

    // What one "保存配置" command accomplished across the authoritative
    // sources. A draft with nothing dirty is a success-shaped no-op that
    // writes nothing (Q31); a save where any source refused is a partial
    // failure that keeps the failed edits in the draft and does not navigate
    // (Q23/Q24).
    internal enum DraftSaveOutcome : byte { NoChanges, Success, PartialFailure }

    internal sealed class DraftSaveReport
    {
        internal DraftSaveOutcome Outcome { get; }
        // Banner lines in priority order; PrimaryMessage is the first.
        internal IReadOnlyList<string> Messages { get; }
        // Set only when a RequiresRestart external item was WRITTEN this save
        // (Q21: the badge means "saved, needs restart", never "unsaved").
        internal bool RequiresRestart { get; }
        internal string PrimaryMessage
        {
            get { return Messages != null && Messages.Count > 0 ? Messages[0] : string.Empty; }
        }

        internal DraftSaveReport(DraftSaveOutcome outcome, IReadOnlyList<string> messages, bool requiresRestart)
        {
            Outcome = outcome;
            Messages = messages ?? new string[0];
            RequiresRestart = requiresRestart;
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
        // DEV-V4-01: the single open detail draft (the logical panel session).
        // One entry is being edited at a time; the draft lives only in memory,
        // survives a UI-tree re-mount, and is the ONLY thing the edit commands
        // touch until "保存配置" hands values to the authoritative sources.
        private DetailDraft draft;

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
            entries[index] = new PluginConfigEntryView(entry.Key, entry.DisplayName, entry.Kind, value, entry.RequiresRestart, entry.CanEdit, entry.Minimum, entry.Maximum, entry.MaximumLength);
            plugins[pluginGuid] = new LoadedPluginDescriptor(plugin.Guid, plugin.DisplayName, plugin.Version, entries);
            return new PluginConfigEditResult(true, entry.RequiresRestart, PluginConfigEditRejection.None);
        }

        // ── DEV-V4-01：未保存草稿命令缝（保存 / 放弃 / 确认）───────────────
        // 设置/配置行的编辑命令仅写内存草稿；权威源只在 SaveDraft（等同「保存
        // 配置」）里被触碰一次。接管「让我改回独立 LMN」是独立行动命令（Q21 接管
        // 提示不进草稿），不属这里的行编辑。

        /// <summary>打开（或重进）某条详情的草稿——逻辑面板会话。重开同条保留
        /// 既有草稿（UI 重挂 ≠ 会话结束）；开新条或 null 起一份从当前权威
        /// 快照捕获的新草稿。永不写盘。</summary>
        internal void OpenDetail(string stableId)
        {
            EnsurePreferencesLoaded();
            if (draft != null && string.Equals(draft.StableId, stableId, StringComparison.Ordinal)) return;
            draft = null;
            if (string.IsNullOrEmpty(stableId)) return;
            BueFeatureManagementEntry feature;
            if (features.TryGetValue(stableId, out feature))
            {
                var opened = new DetailDraft
                {
                    StableId = stableId,
                    Kind = ManagementEntryKind.BueFeature,
                    Feature = feature.Feature,
                    SettingsBaselineRevision = feature.Settings.Revision,
                    EnableIntentBaseline = IsFeatureCurrentlyEnabled(feature.State)
                };
                foreach (var entry in VisibleSettings(feature.Settings)) opened.BaselineSettings[entry.SettingId] = entry;
                draft = opened;
                return;
            }
            LoadedPluginDescriptor plugin;
            if (plugins.TryGetValue(stableId, out plugin))
            {
                var opened = new DetailDraft { StableId = stableId, Kind = ManagementEntryKind.ExternalPlugin };
                foreach (var entry in plugin.ConfigEntries) opened.BaselineConfig[entry.Key] = entry;
                draft = opened;
            }
        }

        internal bool HasOpenDetail { get { return draft != null; } }
        internal string OpenStableId { get { return draft == null ? null : draft.StableId; } }

        /// <summary>脏 = 草稿中任一可编辑字段的最终值 ≠ 进入详情时的权威快照值
        /// （V4-T2 Q29）。拨回原值不算脏。</summary>
        internal bool IsDirty
        {
            get
            {
                if (draft == null) return false;
                if (draft.Kind == ManagementEntryKind.BueFeature)
                {
                    foreach (var edit in draft.SettingEdits)
                        if (SettingEditDiffers(draft, edit.Key, edit.Value)) return true;
                    if (draft.EnableEdit.HasValue && draft.EnableEdit.Value != draft.EnableIntentBaseline) return true;
                }
                else
                {
                    foreach (var edit in draft.ConfigEdits)
                        if (ConfigValueDiffers(draft.BaselineConfig[edit.Key].Value, edit.Value)) return true;
                }
                return false;
            }
        }

        internal bool DraftEditBueSetting(string settingId, PluginConfigValue value)
        {
            EnsurePreferencesLoaded();
            if (draft == null || draft.Kind != ManagementEntryKind.BueFeature) return false;
            SettingEntryView baseline;
            if (!draft.BaselineSettings.TryGetValue(settingId, out baseline)) return false;
            if (!baseline.CanEdit || baseline.Authority == SettingAuthority.ServerAuthoritative) return false;
            var edited = ToSettingMutation(settingId, value).Value;
            if (SettingValuesEqual(baseline.EffectiveValue, edited)) draft.SettingEdits.Remove(settingId);
            else draft.SettingEdits[settingId] = edited;
            return true;
        }

        internal bool DraftEditPluginConfig(string pluginGuid, string key, string rawValue)
        {
            EnsurePreferencesLoaded();
            if (draft == null || draft.Kind != ManagementEntryKind.ExternalPlugin) return false;
            if (!string.Equals(pluginGuid, draft.StableId, StringComparison.Ordinal)) return false;
            PluginConfigEntryView entry;
            if (!draft.BaselineConfig.TryGetValue(key, out entry)) return false;
            if (entry.Kind == PluginConfigValueKind.Unsupported || !entry.CanEdit) return false;
            PluginConfigValue value;
            if (!TryParse(entry.Kind, rawValue, out value)) return false;
            if (!WithinBounds(entry, value)) return false;
            if (ConfigValueDiffers(entry.Value, value)) draft.ConfigEdits[key] = value;
            else draft.ConfigEdits.Remove(key);
            return true;
        }

        internal bool DraftSetFeatureEnabled(bool enabled)
        {
            EnsurePreferencesLoaded();
            if (draft == null || draft.Kind != ManagementEntryKind.BueFeature) return false;
            if (enabled == draft.EnableIntentBaseline) draft.EnableEdit = null;
            else draft.EnableEdit = enabled;
            return true;
        }

        internal bool IsBueSettingDirty(string settingId)
        {
            if (draft == null || draft.Kind != ManagementEntryKind.BueFeature) return false;
            SettingValue edited;
            return draft.SettingEdits.TryGetValue(settingId, out edited) && SettingEditDiffers(draft, settingId, edited);
        }

        internal bool IsPluginConfigDirty(string key)
        {
            return draft != null && draft.Kind == ManagementEntryKind.ExternalPlugin && draft.ConfigEdits.ContainsKey(key);
        }

        internal bool IsFeatureEnableDirty
        {
            get { return draft != null && draft.Kind == ManagementEntryKind.BueFeature && draft.EnableEdit.HasValue; }
        }

        internal SettingValue EffectiveBueSettingValue(string settingId)
        {
            if (draft != null && draft.Kind == ManagementEntryKind.BueFeature)
            {
                SettingValue edited;
                if (draft.SettingEdits.TryGetValue(settingId, out edited)) return edited;
                SettingEntryView baseline;
                if (draft.BaselineSettings.TryGetValue(settingId, out baseline)) return baseline.EffectiveValue;
            }
            return default(SettingValue);
        }

        internal PluginConfigValue EffectivePluginConfigValue(string key)
        {
            if (draft != null && draft.Kind == ManagementEntryKind.ExternalPlugin)
            {
                PluginConfigValue edited;
                if (draft.ConfigEdits.TryGetValue(key, out edited)) return edited;
                PluginConfigEntryView baseline;
                if (draft.BaselineConfig.TryGetValue(key, out baseline)) return baseline.Value;
            }
            return PluginConfigValue.UnsupportedValue();
        }

        /// <summary>「保存配置」：按冻结写入顺序把草稿交给权威源——BUE 设置
        /// （一次原子 Submit，基准 = 进入详情/本源上次成功的 revision）→ 外部
        /// ConfigEntry（逐条）→ 功能启停（最后，且不以「前面全成功」为前提，
        /// V4-T2 Q27）。各源互不阻塞：失败源保留其草稿并给原因，不影响后续源。
        /// 返回面板要渲染的报告；两种结果草稿会话都不关（不关面板，Q30）。</summary>
        internal DraftSaveReport SaveDraft()
        {
            EnsurePreferencesLoaded();
            if (draft == null || !IsDirty) return new DraftSaveReport(DraftSaveOutcome.NoChanges, new[] { "没有需要保存的修改。" }, false);

            var failures = 0;
            var requiresRestart = false;
            var messages = new List<string>();

            if (draft.Kind == ManagementEntryKind.BueFeature && draft.SettingEdits.Count > 0)
            {
                var mutations = new List<SettingMutation>();
                foreach (var edit in draft.SettingEdits) mutations.Add(new SettingMutation(edit.Key, edit.Value));
                var result = bueSettingsEditor.ApplyBatch(draft.Feature, draft.SettingsBaselineRevision, mutations);
                if (result.Accepted)
                {
                    draft.SettingsBaselineRevision = result.Revision;
                    draft.BaselineSettings.Clear();
                    foreach (var entry in VisibleSettings(result.Snapshot)) draft.BaselineSettings[entry.SettingId] = entry;
                    draft.SettingEdits.Clear();
                    CommitFeatureSnapshot(draft.Feature, result.Snapshot);
                }
                else
                {
                    failures++;
                    messages.Add(result.Error == FrameworkErrorCode.SettingRevisionConflict ? "未保存：设置已在别处变更。"
                        : result.Error == FrameworkErrorCode.SettingValidationFailed ? "未保存：设置校验未通过。（" + result.Error + "）"
                        : "未保存：设置未能保存。（" + result.Error + "）");
                }
            }

            if (draft.Kind == ManagementEntryKind.ExternalPlugin && draft.ConfigEdits.Count > 0)
            {
                LoadedPluginDescriptor plugin;
                if (plugins.TryGetValue(draft.StableId, out plugin))
                {
                    var entries = plugin.ConfigEntries.ToArray();
                    var committed = new List<string>();
                    // Q27: write external ConfigEntry in STABLE order — walk the
                    // entry list (the panel's display order), not the unordered
                    // edit dictionary, so a multi-key save is deterministic.
                    for (var position = 0; position < entries.Length; position++)
                    {
                        var entry = entries[position];
                        PluginConfigValue pending;
                        if (!draft.ConfigEdits.TryGetValue(entry.Key, out pending)) continue;
                        if (pluginConfigEditor != null && pluginConfigEditor.TrySet(draft.StableId, entry.Key, pending))
                        {
                            entries[position] = new PluginConfigEntryView(entry.Key, entry.DisplayName, entry.Kind, pending,
                                entry.RequiresRestart, entry.CanEdit, entry.Minimum, entry.Maximum, entry.MaximumLength);
                            if (entry.RequiresRestart) requiresRestart = true;
                            committed.Add(entry.Key);
                        }
                        else { failures++; messages.Add("未保存：外部配置写入失败。"); }
                    }
                    plugins[draft.StableId] = new LoadedPluginDescriptor(plugin.Guid, plugin.DisplayName, plugin.Version, entries);
                    foreach (var key in committed)
                    {
                        draft.ConfigEdits.Remove(key);
                        for (var i = 0; i < entries.Length; i++)
                            if (string.Equals(entries[i].Key, key, StringComparison.Ordinal)) draft.BaselineConfig[key] = entries[i];
                    }
                }
                else
                {
                    // The plugin vanished between open and save (Q25): its
                    // ConfigEntry edits cannot be written — fail that source,
                    // keep the draft, never report a false success.
                    failures++;
                    messages.Add("未保存：插件已卸载。");
                }
            }

            if (draft.Kind == ManagementEntryKind.BueFeature && draft.EnableEdit.HasValue && draft.EnableEdit.Value != draft.EnableIntentBaseline)
            {
                var target = draft.EnableEdit.Value;
                if (TryToggleFeature(draft.Feature, target)) { draft.EnableIntentBaseline = target; draft.EnableEdit = null; }
                else { failures++; messages.Add("未保存：功能启停失败。"); }
            }

            if (failures == 0)
            {
                var badge = requiresRestart;
                ClearDraftEdits();
                return new DraftSaveReport(DraftSaveOutcome.Success, new[] { "配置已保存。" }, badge);
            }
            return new DraftSaveReport(DraftSaveOutcome.PartialFailure, messages, requiresRestart);
        }

        /// <summary>不保存：丢掉当前条草稿、不写任何源（V4-T2 Q22）。</summary>
        internal void DiscardDraft()
        {
            draft = null;
        }

        /// <summary>确认框三选一的后果。返回 true = 可以导航离开当前条（干净、
        /// 不保存、或保存全部成功），false = 必须留在本条（取消，或保存中任一源
        /// 失败——失败保存不导航，Q22）。targetStableId 为 null 表示关面板。
        /// 当选择为保存时经 <paramref name="report"/> 交回 SaveDraft 的同一份报告
        /// （等同「保存配置」，Q22）——面板据此渲染顶栏，不得另造文案。</summary>
        internal bool TryLeaveDetail(string targetStableId, PanelConfirmChoice choice, out DraftSaveReport report)
        {
            EnsurePreferencesLoaded();
            report = null;
            if (draft == null || !IsDirty)
            {
                OpenDetail(targetStableId);
                return true;
            }
            switch (choice)
            {
                case PanelConfirmChoice.Cancel:
                    return false;
                case PanelConfirmChoice.Discard:
                    draft = null;
                    OpenDetail(targetStableId);
                    return true;
                case PanelConfirmChoice.Save:
                    report = SaveDraft();
                    if (report.Outcome == DraftSaveOutcome.PartialFailure) return false;
                    draft = null;
                    OpenDetail(targetStableId);
                    return true;
                default:
                    return false;
            }
        }

        private void CommitFeatureSnapshot(FeatureId feature, FeatureSettingsSnapshot snapshot)
        {
            BueFeatureManagementEntry current;
            if (!features.TryGetValue(feature.Value, out current)) return;
            features[feature.Value] = new BueFeatureManagementEntry(current.Feature, current.DisplayName, current.Version,
                current.State, current.Presentation, snapshot);
        }

        private void ClearDraftEdits()
        {
            if (draft == null) return;
            draft.SettingEdits.Clear();
            draft.ConfigEdits.Clear();
            draft.EnableEdit = null;
        }

        private static bool SettingEditDiffers(DetailDraft currentDraft, string settingId, SettingValue edited)
        {
            SettingEntryView baseline;
            if (!currentDraft.BaselineSettings.TryGetValue(settingId, out baseline)) return true;
            return !SettingValuesEqual(baseline.EffectiveValue, edited);
        }

        private static bool SettingValuesEqual(SettingValue left, SettingValue right)
        {
            if (left.Kind != right.Kind) return false;
            switch (left.Kind)
            {
                case SettingKind.Toggle: return left.Boolean == right.Boolean;
                case SettingKind.Integer: return left.Integer == right.Integer;
                case SettingKind.Float: return left.Float.Equals(right.Float);
                default: return string.Equals(left.Text ?? string.Empty, right.Text ?? string.Empty, StringComparison.Ordinal);
            }
        }

        private static bool ConfigValueDiffers(PluginConfigValue left, PluginConfigValue right)
        {
            if (left.Kind != right.Kind) return true;
            switch (left.Kind)
            {
                case PluginConfigValueKind.Boolean: return left.Boolean != right.Boolean;
                case PluginConfigValueKind.Integer: return left.Integer64 != right.Integer64;
                case PluginConfigValueKind.Float: return left.Float64 != right.Float64;
                case PluginConfigValueKind.String: return !string.Equals(left.Text ?? string.Empty, right.Text ?? string.Empty, StringComparison.Ordinal);
                default: return true;
            }
        }

        private static bool IsFeatureCurrentlyEnabled(FeatureState state)
        {
            return state == FeatureState.Running || state == FeatureState.Starting;
        }

        // 当前条目详情的内存草稿：存进入详情时捕获的权威值（baseline）与按字段
        // id 索引的待定编辑；SaveDraft 与活动源对账。这里没有持久化——草稿随进程
        // 结束而消失。
        private sealed class DetailDraft
        {
            internal string StableId;
            internal ManagementEntryKind Kind;
            internal FeatureId Feature;
            internal uint SettingsBaselineRevision;
            internal readonly Dictionary<string, SettingEntryView> BaselineSettings = new Dictionary<string, SettingEntryView>(StringComparer.Ordinal);
            internal readonly Dictionary<string, SettingValue> SettingEdits = new Dictionary<string, SettingValue>(StringComparer.Ordinal);
            internal bool EnableIntentBaseline;
            internal bool? EnableEdit;
            internal readonly Dictionary<string, PluginConfigEntryView> BaselineConfig = new Dictionary<string, PluginConfigEntryView>(StringComparer.Ordinal);
            internal readonly Dictionary<string, PluginConfigValue> ConfigEdits = new Dictionary<string, PluginConfigValue>(StringComparer.Ordinal);
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
