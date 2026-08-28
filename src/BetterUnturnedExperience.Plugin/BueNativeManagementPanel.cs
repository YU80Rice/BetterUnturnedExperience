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
        internal enum TickSource : byte { Initialize, Update, HostUi, Harmony }

        private const string PluginId = "io.github.yu80rice.betterunturnedexperience";
        private const string FeatureId = "io.github.yu80rice.bue.management-panel";
        private const string Version = "0.0.0";
        private const string TraceDiagnosticId = "BUE-MANAGEMENT-TRACE-001";
        private readonly BueManagementPanelRuntime runtime;
        private readonly ManualLogSource log;
        private readonly Harmony harmony;
        private readonly FieldInfo dashboardContainerField;
        private readonly FieldInfo workshopContainerField;
        private readonly FieldInfo pauseContainerField;
        private SleekFullscreenBox panel;
        private ISleekButton dashboardButton;
        private ISleekButton mainButton;
        private ISleekButton pauseButton;
        private ISleekButton closeButton;
        private ISleekLabel title;
        private ISleekLabel body;
        private ISleekElement mainParent;
        private ISleekElement dashboardParent;
        private ISleekElement pauseParent;
        private bool opened;
        private bool destroyed;
        private bool firstTickLogged;
        private int lastMainContainerState = -1;
        private int lastPauseContainerState = -1;
        private int updateTickCount;
        private static BueNativeManagementPanel activeInstance;

        internal static bool RequiresParentRebind(object boundParent, object currentParent)
        {
            return currentParent != null && !ReferenceEquals(boundParent, currentParent);
        }

        internal BueNativeManagementPanel(BueManagementPanelRuntime runtime, ManualLogSource log)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.log = log;
            dashboardContainerField = typeof(MenuDashboardUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic);
            // Match the visible vanilla page used by UnturnedPluginManager:
            // the Workshop management page, not MenuDashboardUI.
            workshopContainerField = typeof(MenuWorkshopUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic);
            pauseContainerField = typeof(PlayerPauseUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic);
            harmony = new Harmony("io.github.yu80rice.bue.management-panel");
            activeInstance = this;
            LogTrace("constructed", "dashboardField=" + (dashboardContainerField != null) + " workshopField=" + (workshopContainerField != null) + " pauseField=" + (pauseContainerField != null));
        }

        internal void Initialize()
        {
            if (destroyed) return;
            runtime.Initialize();
            PatchRebuildHooks();
            LogTrace("initialize-complete", string.Empty);
            Tick(TickSource.Initialize);
        }

        internal void Tick(TickSource source)
        {
            if (destroyed) return;
            if (source == TickSource.Update)
            {
                updateTickCount++;
                if (updateTickCount == 1 || updateTickCount % 120 == 0)
                {
                    LogTrace("heartbeat", "source=Update count=" + updateTickCount);
                }
            }
            else if (!firstTickLogged)
            {
                firstTickLogged = true;
                LogTrace("first-tick", "source=" + source);
            }
            TryAddDashboardButton(source.ToString());
            TryAddMainButton(source.ToString());
            TryAddPauseButton(source.ToString());
            if (opened && (panel == null || !IsAlive(panel))) Close();
        }

        internal void Destroy()
        {
            if (destroyed) return;
            destroyed = true;
            if (ReferenceEquals(activeInstance, this)) activeInstance = null;
            CleanupDashboardButton(dashboardParent);
            CleanupMainButton(mainParent);
            CleanupPauseButton(pauseParent);
            Close();
            try { harmony.UnpatchSelf(); } catch (Exception error) { Log("unpatch failed: " + error.Message); }
            runtime.Destroy();
        }

        private void PatchRebuildHooks()
        {
            PatchPostfix(AccessTools.Constructor(typeof(MenuWorkshopUI), Type.EmptyTypes), "MenuWorkshopUI.constructor", nameof(OnUiRebuilt));
            PatchPostfix(AccessTools.Constructor(typeof(PlayerPauseUI), Type.EmptyTypes), "PlayerPauseUI.constructor", nameof(OnUiRebuilt));
            PatchPostfix(AccessTools.Constructor(typeof(MenuDashboardUI), Type.EmptyTypes), "MenuDashboardUI.constructor", nameof(OnUiRebuilt));
            PatchPostfix(AccessTools.Method(typeof(MenuDashboardUI), "open"), "MenuDashboardUI.open", nameof(OnSurfaceOpened));
            PatchPostfix(AccessTools.Method(typeof(MenuWorkshopUI), "open"), "MenuWorkshopUI.open", nameof(OnSurfaceOpened));
            PatchPostfix(AccessTools.Method(typeof(PlayerPauseUI), "open"), "PlayerPauseUI.open", nameof(OnSurfaceOpened));
            PatchPostfix(AccessTools.Method(typeof(MenuUI), "Update"), "MenuUI.Update", nameof(OnHostUiTick));
            PatchPostfix(AccessTools.Method(typeof(PlayerUI), "Update"), "PlayerUI.Update", nameof(OnHostUiTick));
            try
            {
                var menuEscape = AccessTools.Method(typeof(MenuUI), "escapeMenu");
                if (menuEscape != null)
                {
                    harmony.Patch(menuEscape, prefix: new HarmonyMethod(typeof(BueNativeManagementPanel), nameof(OnMenuEscapePrefix)));
                    LogTrace("patch-installed", "target=MenuUI.escapeMenu");
                }
                var playerEscape = AccessTools.Method(typeof(PlayerUI), "escapeMenu");
                if (playerEscape != null)
                {
                    harmony.Patch(playerEscape, prefix: new HarmonyMethod(typeof(BueNativeManagementPanel), nameof(OnPlayerEscapePrefix)));
                    LogTrace("patch-installed", "target=PlayerUI.escapeMenu");
                }
                var menuCloseAll = AccessTools.Method(typeof(MenuUI), "closeAll");
                if (menuCloseAll != null)
                {
                    harmony.Patch(menuCloseAll, postfix: new HarmonyMethod(typeof(BueNativeManagementPanel), nameof(OnMenuCloseAllPostfix)));
                    LogTrace("patch-installed", "target=MenuUI.closeAll");
                }
            }
            catch (Exception error)
            {
                Log("UI rebuild hook unavailable; polling remains active: " + error.Message);
            }
        }

        private void PatchPostfix(MethodBase target, string targetName, string callbackName)
        {
            if (target == null)
            {
                LogTrace("patch-missing", "target=" + targetName);
                return;
            }
            try
            {
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(BueNativeManagementPanel), callbackName));
                LogTrace("patch-installed", "target=" + targetName);
            }
            catch (Exception error)
            {
                LogTrace("patch-failed", "target=" + targetName + " errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
        }

        private static void OnUiRebuilt()
        {
            var instance = activeInstance;
            if (instance == null || instance.destroyed) return;
            instance.LogTrace("constructor-postfix", "source=Harmony");
            instance.Tick(TickSource.Harmony);
        }

        // The vanilla UI objects can be constructed before BepInEx finishes
        // loading this plugin.  Pumping on the public open methods closes that
        // timing gap and mirrors the proven PluginManager integration path.
        private static void OnSurfaceOpened()
        {
            var instance = activeInstance;
            if (instance == null || instance.destroyed) return;
            instance.LogTrace("surface-opened", "source=Harmony");
            instance.Tick(TickSource.Harmony);
        }

        // BUE's BaseUnityPlugin lifecycle is not guaranteed to receive Update
        // on every supported host.  The vanilla UI roots do receive their own
        // Update messages, so use them as a reliable main-thread pump.
        private static void OnHostUiTick()
        {
            var instance = activeInstance;
            if (instance == null || instance.destroyed) return;
            instance.Tick(TickSource.HostUi);
        }

        private void TryAddDashboardButton(string source)
        {
            var parent = ReadContainer(dashboardContainerField);
            if (parent != null && !IsAlive(parent)) parent = null;
            if (parent == null)
            {
                if (dashboardParent != null)
                {
                    CleanupDashboardButton(dashboardParent);
                    dashboardParent = null;
                }
                return;
            }
            if (!RequiresParentRebind(dashboardParent, parent)) return;
            if (dashboardParent != null)
            {
                CleanupDashboardButton(dashboardParent);
                dashboardParent = null;
            }
            try
            {
                LogTrace("create-button-begin", "surface=MenuDashboardUI source=" + source);
                dashboardButton = Glazier.Get().CreateButton();
                if (dashboardButton == null) throw new InvalidOperationException("Glazier.CreateButton returned null");
                dashboardButton.PositionOffset_Y = 410f;
                dashboardButton.SizeOffset_X = 200f;
                dashboardButton.SizeOffset_Y = 50f;
                dashboardButton.Text = "BUE 插件管理";
                dashboardButton.TooltipText = "查看已加载的 BepInEx 插件，并可在游戏内修改其配置";
                dashboardButton.FontSize = ESleekFontSize.Medium;
                dashboardButton.OnClicked += OnDashboardButtonClicked;
                parent.AddChild(dashboardButton);
                dashboardParent = parent;
                LogTrace("add-child-success", "surface=MenuDashboardUI source=" + source);
            }
            catch (Exception error)
            {
                CleanupDashboardButton(parent);
                LogTrace("entry-failed", "surface=MenuDashboardUI source=" + source + " errorType=" + error.GetType().FullName + " message=" + error.Message);
            }
        }

        private static bool OnMenuEscapePrefix()
        {
            var instance = activeInstance;
            if (instance == null || !instance.opened) return true;
            instance.Close();
            return false;
        }

        private static bool OnPlayerEscapePrefix()
        {
            var instance = activeInstance;
            if (instance == null || !instance.opened) return true;
            instance.Close();
            return false;
        }

        private static void OnMenuCloseAllPostfix()
        {
            var instance = activeInstance;
            if (instance != null) instance.Close();
        }

        private void TryAddMainButton(string source)
        {
            var parent = ReadContainer(workshopContainerField);
            if (parent != null && !IsAlive(parent))
            {
                LogTrace("stale-container", "surface=MenuWorkshopUI source=" + source);
                parent = null;
            }
            var mainState = parent == null ? 0 : 1;
            if (mainState != lastMainContainerState)
            {
                lastMainContainerState = mainState;
                LogTrace("container-state", "surface=MenuWorkshopUI state=" + (parent == null ? "null" : parent.GetType().FullName) + " active=" + SafeActive(typeof(MenuWorkshopUI)) + " source=" + source);
            }
            if (parent == null)
            {
                if (mainParent != null)
                {
                    CleanupMainButton(mainParent);
                    mainParent = null;
                }
                return;
            }
            if (!RequiresParentRebind(mainParent, parent)) return;
            if (mainParent != null)
            {
                CleanupMainButton(mainParent);
                mainParent = null;
            }
            try
            {
                LogTrace("create-button-begin", "surface=MenuWorkshopUI source=" + source);
                mainButton = Glazier.Get().CreateButton();
                LogTrace("create-button-result", "surface=MenuWorkshopUI created=" + (mainButton != null) + " source=" + source);
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
                LogTrace("add-child-success", "surface=MenuWorkshopUI source=" + source);
            }
            catch (Exception error)
            {
                CleanupMainButton(parent);
                LogTrace("entry-failed", "surface=MenuWorkshopUI source=" + source + " errorType=" + error.GetType().FullName + " message=" + error.Message);
                Log("workshop menu entry failed: " + error.Message);
            }
        }

        private void TryAddPauseButton(string source)
        {
            var parent = ReadContainer(pauseContainerField);
            if (parent != null && !IsAlive(parent))
            {
                LogTrace("stale-container", "surface=PlayerPauseUI source=" + source);
                parent = null;
            }
            var pauseState = parent == null ? 0 : 1;
            if (pauseState != lastPauseContainerState)
            {
                lastPauseContainerState = pauseState;
                LogTrace("container-state", "surface=PlayerPauseUI state=" + (parent == null ? "null" : parent.GetType().FullName) + " active=" + SafeActive(typeof(PlayerPauseUI)) + " source=" + source);
            }
            if (parent == null)
            {
                if (pauseParent != null)
                {
                    CleanupPauseButton(pauseParent);
                    pauseParent = null;
                }
                return;
            }
            if (!RequiresParentRebind(pauseParent, parent)) return;
            if (pauseParent != null)
            {
                CleanupPauseButton(pauseParent);
                pauseParent = null;
            }
            try
            {
                LogTrace("create-button-begin", "surface=PlayerPauseUI source=" + source);
                pauseButton = Glazier.Get().CreateButton();
                LogTrace("create-button-result", "surface=PlayerPauseUI created=" + (pauseButton != null) + " source=" + source);
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
                LogTrace("add-child-success", "surface=PlayerPauseUI source=" + source);
            }
            catch (Exception error)
            {
                CleanupPauseButton(parent);
                LogTrace("entry-failed", "surface=PlayerPauseUI source=" + source + " errorType=" + error.GetType().FullName + " message=" + error.Message);
                Log("pause menu entry failed: " + error.Message);
            }
        }

        private void OnMainButtonClicked(ISleekElement button) { Open(ReadContainer(workshopContainerField)); }
        private void OnPauseButtonClicked(ISleekElement button) { Open(ReadContainer(pauseContainerField)); }
        private void OnDashboardButtonClicked(ISleekElement button) { Open(ReadContainer(dashboardContainerField)); }

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

        private static string SafeActive(Type uiType)
        {
            try
            {
                var field = uiType.GetField("active", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return field == null ? "field-missing" : Convert.ToString(field.GetValue(null));
            }
            catch (Exception error)
            {
                return "read-failed:" + error.GetType().Name;
            }
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
            LogTrace("warning", "reasonCode=ManagementFailure message=" + message);
        }

        private void CleanupMainButton(ISleekElement parent)
        {
            if (mainButton == null) return;
            try { mainButton.OnClicked -= OnMainButtonClicked; } catch (Exception) { }
            try { if (parent != null) parent.RemoveChild(mainButton); } catch (Exception) { }
            mainButton = null;
        }

        private void CleanupDashboardButton(ISleekElement parent)
        {
            if (dashboardButton == null) return;
            try { dashboardButton.OnClicked -= OnDashboardButtonClicked; } catch (Exception) { }
            try { if (parent != null) parent.RemoveChild(dashboardButton); } catch (Exception) { }
            dashboardButton = null;
        }

        private void CleanupPauseButton(ISleekElement parent)
        {
            if (pauseButton == null) return;
            try { pauseButton.OnClicked -= OnPauseButtonClicked; } catch (Exception) { }
            try { if (parent != null) parent.RemoveChild(pauseButton); } catch (Exception) { }
            pauseButton = null;
        }

        private void LogTrace(string eventName, string details)
        {
            if (log != null)
            {
                log.LogInfo("[BUE-UI-TRACE] plugin=" + PluginId + " featureId=" + FeatureId + " version=" + Version + " environmentRole=Client scenario=" + GetScenario() + " diagnosticId=" + TraceDiagnosticId + " event=" + eventName + " " + details);
            }
        }

        private static string GetScenario()
        {
            try
            {
                if (Provider.isServer)
                {
                    return "LocalAuthorityOrHost";
                }
                return "RemoteClient";
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }
    }
}
