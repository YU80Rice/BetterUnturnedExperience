using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// Native Glazier adapter for the BUE-owned management panel. All UI and
    /// Unity references stop at this file; the panel model remains pure C#.
    /// </summary>
    internal sealed class BueNativeManagementPanel
    {
        private readonly BueManagementPanelRuntime runtime;
        private readonly ManualLogSource log;
        private readonly Harmony harmony;
        private readonly FieldInfo workshopContainerField;
        private readonly FieldInfo pauseContainerField;
        private SleekFullscreenBox panel;
        private ISleekButton mainButton;
        private ISleekButton pauseButton;
        private ISleekButton closeButton;
        private ISleekLabel title;
        private ISleekLabel body;
        private ISleekElement mainParent;
        private ISleekElement pauseParent;
        private bool opened;
        private bool destroyed;

        internal BueNativeManagementPanel(BueManagementPanelRuntime runtime, ManualLogSource log)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.log = log;
            workshopContainerField = typeof(MenuDashboardUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic);
            pauseContainerField = typeof(PlayerPauseUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic);
            harmony = new Harmony("io.github.yu80rice.bue.management-panel");
        }

        internal void Initialize()
        {
            if (destroyed) return;
            runtime.Initialize();
            PatchRebuildHooks();
            Tick();
        }

        internal void Tick()
        {
            if (destroyed) return;
            TryAddMainButton();
            TryAddPauseButton();
            if (opened && (panel == null || !IsAlive(panel))) Close();
        }

        internal void Destroy()
        {
            if (destroyed) return;
            destroyed = true;
            Close();
            try { harmony.UnpatchSelf(); } catch (Exception error) { Log("unpatch failed: " + error.Message); }
            runtime.Destroy();
        }

        private void PatchRebuildHooks()
        {
            try
            {
                var workshopCtor = AccessTools.Constructor(typeof(MenuDashboardUI), Type.EmptyTypes);
                if (workshopCtor != null) harmony.Patch(workshopCtor, postfix: new HarmonyMethod(typeof(BueNativeManagementPanel), nameof(OnUiRebuilt)));
                var pauseCtor = AccessTools.Constructor(typeof(PlayerPauseUI), Type.EmptyTypes);
                if (pauseCtor != null) harmony.Patch(pauseCtor, postfix: new HarmonyMethod(typeof(BueNativeManagementPanel), nameof(OnUiRebuilt)));
            }
            catch (Exception error)
            {
                Log("UI rebuild hook unavailable; polling remains active: " + error.Message);
            }
        }

        private static void OnUiRebuilt()
        {
            // Instance polling in the owning plugin is the authoritative path;
            // this postfix intentionally contains no state mutation.
        }

        private void TryAddMainButton()
        {
            var parent = ReadContainer(workshopContainerField);
            if (parent == null || ReferenceEquals(mainParent, parent)) return;
            try
            {
                mainButton = Glazier.Get().CreateButton();
                mainButton.PositionOffset_X = -110f;
                mainButton.PositionOffset_Y = 185f;
                mainButton.PositionScale_X = 0.5f;
                mainButton.PositionScale_Y = 0.5f;
                mainButton.SizeOffset_X = 220f;
                mainButton.SizeOffset_Y = 44f;
                mainButton.Text = "BUE 插件管理";
                mainButton.OnClicked += OnMainButtonClicked;
                parent.AddChild(mainButton);
                mainParent = parent;
            }
            catch (Exception error) { Log("main menu entry failed: " + error.Message); }
        }

        private void TryAddPauseButton()
        {
            var parent = ReadContainer(pauseContainerField);
            if (parent == null || ReferenceEquals(pauseParent, parent)) return;
            try
            {
                pauseButton = Glazier.Get().CreateButton();
                pauseButton.PositionOffset_X = 205f;
                pauseButton.PositionOffset_Y = -290f;
                pauseButton.PositionScale_X = 0.5f;
                pauseButton.PositionScale_Y = 0.5f;
                pauseButton.SizeOffset_X = 220f;
                pauseButton.SizeOffset_Y = 44f;
                pauseButton.Text = "BUE 插件管理";
                pauseButton.OnClicked += OnPauseButtonClicked;
                parent.AddChild(pauseButton);
                pauseParent = parent;
            }
            catch (Exception error) { Log("pause menu entry failed: " + error.Message); }
        }

        private void OnMainButtonClicked(ISleekElement button) { Open(ReadContainer(workshopContainerField)); }
        private void OnPauseButtonClicked(ISleekElement button) { Open(ReadContainer(pauseContainerField)); }

        private void Open(ISleekElement parent)
        {
            if (parent == null || destroyed) return;
            try
            {
                EnsurePanel();
                parent.AddChild(panel);
                opened = true;
                Render();
            }
            catch (Exception error) { Log("open management panel failed: " + error.Message); }
        }

        private void EnsurePanel()
        {
            if (panel != null && IsAlive(panel)) return;
            panel = new SleekFullscreenBox();
            panel.PositionOffset_X = 12f;
            panel.PositionOffset_Y = 12f;
            panel.PositionScale_Y = -1f;
            panel.SizeOffset_X = -24f;
            panel.SizeOffset_Y = -24f;
            panel.SizeScale_X = 1f;
            panel.SizeScale_Y = 1f;

            var background = Glazier.Get().CreateBox();
            background.SizeScale_X = 1f;
            background.SizeScale_Y = 1f;
            background.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.92f);
            panel.AddChild(background);

            title = Glazier.Get().CreateLabel();
            title.PositionOffset_X = 24f;
            title.PositionOffset_Y = 20f;
            title.SizeOffset_X = 620f;
            title.SizeOffset_Y = 40f;
            title.Text = "Better Unturned Experience · 插件管理";
            title.FontSize = ESleekFontSize.Large;
            title.TextAlignment = TextAnchor.MiddleLeft;
            panel.AddChild(title);

            closeButton = Glazier.Get().CreateButton();
            closeButton.PositionOffset_X = -130f;
            closeButton.PositionOffset_Y = 20f;
            closeButton.PositionScale_X = 1f;
            closeButton.SizeOffset_X = 110f;
            closeButton.SizeOffset_Y = 40f;
            closeButton.Text = "关闭";
            closeButton.OnClicked += OnCloseClicked;
            panel.AddChild(closeButton);

            body = Glazier.Get().CreateLabel();
            body.PositionOffset_X = 24f;
            body.PositionOffset_Y = 78f;
            body.SizeOffset_X = -48f;
            body.SizeOffset_Y = -100f;
            body.SizeScale_X = 1f;
            body.SizeScale_Y = 1f;
            body.TextAlignment = TextAnchor.UpperLeft;
            panel.AddChild(body);
        }

        private void Render()
        {
            if (body == null) return;
            var rows = runtime.Model.GetEntries();
            var lines = new List<string>();
            lines.Add("收藏优先 · 未收藏排序：" + (runtime.Model.SortOrder == ManagementSortOrder.NameAscending ? "A-Z" : "Z-A"));
            if (runtime.Model.ExternalManagerDetected) lines.Add(runtime.Model.CompatibilityNotice);
            lines.Add(string.Empty);
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var marker = row.IsFavorite ? "★" : "☆";
                lines.Add(marker + " " + row.DisplayName + "  [" + row.StableId + "]  v" + row.Version);
                if (row.Kind == ManagementEntryKind.BueFeature && row.BueSettings != null)
                {
                    for (var settingIndex = 0; settingIndex < row.BueSettings.Count; settingIndex++)
                    {
                        var setting = row.BueSettings[settingIndex];
                        lines.Add("    · " + setting.SettingId + " = " + FormatSetting(setting.EffectiveValue) + (setting.CanEdit ? " (可编辑)" : " (只读)"));
                    }
                }
                if (row.Kind == ManagementEntryKind.ExternalPlugin && row.PluginConfig != null)
                {
                    lines.Add("    · ConfigEntry：" + row.PluginConfig.Count + " 项（仅基础类型可编辑）");
                }
            }
            if (rows.Count == 0) lines.Add("当前没有检测到已加载的外部插件。");
            body.Text = string.Join("\n", lines.ToArray());
        }

        private void OnCloseClicked(ISleekElement button) { Close(); }

        private void Close()
        {
            if (!opened) return;
            opened = false;
            try { if (panel != null && IsAlive(panel)) panel.AnimateOutOfView(0f, -1f); } catch (Exception error) { Log("close management panel failed: " + error.Message); }
        }

        private static ISleekElement ReadContainer(FieldInfo field)
        {
            if (field == null) return null;
            try { return field.GetValue(null) as ISleekElement; } catch (Exception) { return null; }
        }

        private static bool IsAlive(ISleekElement element)
        {
            if (element == null) return false;
            try
            {
                if (element is SleekWrapper wrapper) return wrapper.GetProxyImplementation() != null;
                return true;
            }
            catch (Exception) { return false; }
        }

        private static string FormatSetting(SettingValue value)
        {
            switch (value.Kind)
            {
                case SettingKind.Toggle: return value.Boolean ? "开启" : "关闭";
                case SettingKind.Integer: return value.Integer.ToString();
                case SettingKind.Float: return value.Float.ToString("0.###");
                default: return value.Text ?? string.Empty;
            }
        }

        private void Log(string message)
        {
            if (log != null) log.LogWarning("[BUE Management] " + message);
        }
    }
}
