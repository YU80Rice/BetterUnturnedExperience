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
    internal interface IBueButtonInjectionSeam
    {
        void Inject(string source);
    }

    internal sealed class NativeButtonInjectionSeam : IBueButtonInjectionSeam
    {
        private readonly BueNativeManagementPanel owner;

        internal NativeButtonInjectionSeam(BueNativeManagementPanel owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void Inject(string source)
        {
            new BueButtonInjectionCoordinator(
                () => owner.TryAddDashboardButton(source),
                () => owner.TryAddMainButton(source),
                () => owner.TryAddPauseButton(source),
                owner.LogEntryFailure).Inject();
        }
    }

    internal sealed class BueButtonInjectionCoordinator
    {
        private readonly System.Action dashboard;
        private readonly System.Action workshop;
        private readonly System.Action pause;
        private readonly System.Action<string, Exception> onFailure;

        internal BueButtonInjectionCoordinator(System.Action dashboard, System.Action workshop, System.Action pause, System.Action<string, Exception> onFailure)
        {
            this.dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
            this.workshop = workshop ?? throw new ArgumentNullException(nameof(workshop));
            this.pause = pause ?? throw new ArgumentNullException(nameof(pause));
            this.onFailure = onFailure;
        }

        internal void Inject()
        {
            TryInject("MenuDashboardUI", dashboard);
            TryInject("MenuWorkshopUI", workshop);
            TryInject("PlayerPauseUI", pause);
        }

        private void TryInject(string surface, System.Action action)
        {
            try { action(); }
            catch (Exception error) { if (onFailure != null) onFailure(surface, error); }
        }
    }

    /// <summary>
    /// Native Glazier adapter for the BUE-owned management panel. All UI and
    /// Unity references stop at this file; the panel model remains pure C#.
    /// </summary>
    internal sealed class BueNativeManagementPanel
    {
        internal enum TickSource : byte { Initialize, Update, HostUi, RuntimePump, Harmony }

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
        private readonly BueRuntimeTickDispatcher tickDispatcher;
        private readonly IBueButtonInjectionSeam buttonInjectionSeam;
        private readonly System.Action refreshModel;
        private SleekFullscreenBox panel;
        private ISleekButton dashboardButton;
        private ISleekButton mainButton;
        private ISleekButton pauseButton;
        private ISleekButton closeButton;
        private ISleekButton refreshButton;
        private ISleekButton sortAscendingButton;
        private ISleekButton sortDescendingButton;
        private ISleekLabel title;
        private ISleekLabel status;
        private ISleekScrollView listScroll;
        private ISleekScrollView detailScroll;
        private ISleekElement mainParent;
        private ISleekElement dashboardParent;
        private ISleekElement pauseParent;
        private readonly FieldInfo[] pauseShiftFields;
        private readonly BuePauseColumnShift pauseColumnShift;
        private readonly bool pauseRequiredShiftFieldsPresent;
        private readonly string missingPauseShiftField;
        private bool pauseFieldGapLogged;
        private bool lastPauseColumnReady;
        private readonly List<string> loggedPauseElementReadFailures = new List<string>();
        private SleekFullscreenBox hiddenOrigin;
        private bool opened;
        private bool destroyed;
        private bool firstTickLogged;
        private int lastMainContainerState = -1;
        private int lastPauseContainerState = -1;
        private int updateTickCount;
        private bool hostUiTickLogged;
        private string selectedStableId;
        // DEV-V4-01: unsaved-draft navigation. The immediate-write path is
        // retired — a settings/config edit lands in the model draft and only
        // "保存配置" reaches the authoritative source. When a dirty draft is
        // open, switching entries / refreshing / closing arms the "修改尚未
        // 保存，要保存吗？" confirm (model command TryLeaveDetail), never
        // silently dropping the draft.
        private string pendingNavigation;   // null = none; "" = pending close; else target stableId
        private bool pendingRefresh;         // reload the model after leaving the current draft
        private static BueNativeManagementPanel activeInstance;

        internal static bool RequiresParentRebind(object boundParent, object currentParent)
        {
            return currentParent != null && !ReferenceEquals(boundParent, currentParent);
        }

        internal static bool CanBindNativeUi()
        {
            // Member presence only. Glazier.instance is null until the menu
            // UI builds, so engine readiness must not enter this gate: the
            // injection path's null guards own it (a non-null vanilla
            // container implies a live Glazier).
            return typeof(MenuDashboardUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic) != null
                && typeof(MenuWorkshopUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic) != null
                && typeof(PlayerPauseUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic) != null
                && AccessTools.Constructor(typeof(MenuDashboardUI), Type.EmptyTypes) != null
                && AccessTools.Constructor(typeof(MenuWorkshopUI), Type.EmptyTypes) != null
                && AccessTools.Constructor(typeof(PlayerPauseUI), Type.EmptyTypes) != null
                && AccessTools.Method(typeof(MenuUI), "Update") != null
                && AccessTools.Method(typeof(PlayerUI), "Update") != null;
        }

        internal BueNativeManagementPanel(BueManagementPanelRuntime runtime, ManualLogSource log)
            : this(runtime, log, null, null)
        {
        }

        internal BueNativeManagementPanel(BueManagementPanelRuntime runtime, ManualLogSource log, IBueButtonInjectionSeam buttonInjectionSeam)
            : this(runtime, log, buttonInjectionSeam, null)
        {
        }

        internal BueNativeManagementPanel(BueManagementPanelRuntime runtime, ManualLogSource log, IBueButtonInjectionSeam buttonInjectionSeam, System.Action refreshModel)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.log = log;
            dashboardContainerField = typeof(MenuDashboardUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic);
            // Match the visible vanilla page used by UnturnedPluginManager:
            // the Workshop management page, not MenuDashboardUI.
            workshopContainerField = typeof(MenuWorkshopUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic);
            pauseContainerField = typeof(PlayerPauseUI).GetField("container", BindingFlags.Static | BindingFlags.NonPublic);
            pauseShiftFields = ResolvePlayerPauseShiftFields();
            pauseColumnShift = new BuePauseColumnShift(BueMenuEntryLayout.PauseColumnSlotPitch);
            missingPauseShiftField = FindFirstMissingRequiredShiftField(pauseShiftFields);
            pauseRequiredShiftFieldsPresent = missingPauseShiftField == null;
            harmony = new Harmony("io.github.yu80rice.bue.management-panel");
            tickDispatcher = new BueRuntimeTickDispatcher(TickCore, OnTickFailure, log == null ? (Func<int>)(() => -1) : GetFrameCountSafe);
            this.buttonInjectionSeam = buttonInjectionSeam ?? new NativeButtonInjectionSeam(this);
            this.refreshModel = refreshModel;
            activeInstance = this;
            LogTrace("constructed", "dashboardField=" + (dashboardContainerField != null) + " workshopField=" + (workshopContainerField != null) + " pauseField=" + (pauseContainerField != null));
        }

        internal void Initialize()
        {
            if (destroyed) return;
            runtime.Initialize();
            PatchRebuildHooks();
            LogTrace("initialize-complete", string.Empty);
            Dispatch(TickSource.Initialize);
        }

        internal bool Dispatch(TickSource source)
        {
            return tickDispatcher.Dispatch(source);
        }

        internal bool TickIsolated { get { return tickDispatcher.Isolated; } }

        private void OnTickFailure(Exception error)
        {
            LogTrace("tick-isolated", "errorType=" + error.GetType().FullName + " message=" + error.Message);
            // Keep the vanilla detours: the postfixes still observe the isolated
            // state. This is a one-way isolation - the panel is not rebuilt
            // until a fresh BueNativeManagementPanel is constructed.
            Destroy(unpatchHarmony: false);
        }

        private static int GetFrameCountSafe()
        {
            try
            {
                return Time.frameCount;
            }
            catch (Exception)
            {
                // Pure-C# test hosts do not provide Unity's native frame
                // counter. A stable sentinel preserves fail-closed dispatch
                // semantics without making construction depend on the engine.
                return -1;
            }
        }

        private void TickCore(TickSource source)
        {
            if (destroyed) return;
            if (source == TickSource.Update)
            {
                updateTickCount++;
                if (updateTickCount == 1 || updateTickCount % 120 == 0)
                {
                    LogTrace("heartbeat", "source=Update count=" + updateTickCount, true);
                }
            }
            else if (source == TickSource.HostUi && !hostUiTickLogged)
            {
                hostUiTickLogged = true;
                LogTrace("host-ui-tick", "source=VanillaUiUpdate");
            }
            else if (!firstTickLogged)
            {
                firstTickLogged = true;
                LogTrace("first-tick", "source=" + source);
            }
            buttonInjectionSeam.Inject(source.ToString());
            if (opened && (panel == null || !IsAlive(panel))) Close();
        }

        // unpatchHarmony=false keeps the vanilla detours alive when the game
        // destroys the plugin host mid-session: the MenuUI.Update postfix is
        // the frame driver and must survive component teardown. Only a real
        // application quit may remove the patches.
        internal void Destroy(bool unpatchHarmony = true)
        {
            if (destroyed) return;
            destroyed = true;
            if (ReferenceEquals(activeInstance, this)) activeInstance = null;
            CleanupDashboardButton(dashboardParent);
            CleanupMainButton(mainParent);
            CleanupPauseButton(pauseParent);
            Close();
            if (unpatchHarmony)
            {
                try { harmony.UnpatchSelf(); } catch (Exception error) { Log("unpatch failed: " + error.Message); }
            }
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
            instance.Dispatch(TickSource.Harmony);
        }

        // The vanilla UI objects can be constructed before BepInEx finishes
        // loading this plugin.  Pumping on the public open methods closes that
        // timing gap and mirrors the proven PluginManager integration path.
        private static void OnSurfaceOpened()
        {
            var instance = activeInstance;
            if (instance == null || instance.destroyed) return;
            instance.LogTrace("surface-opened", "source=Harmony");
            instance.Dispatch(TickSource.Harmony);
        }

        // BUE's BaseUnityPlugin lifecycle is not guaranteed to receive Update
        // on every supported host.  The vanilla UI roots do receive their own
        // Update messages, so use them as a reliable main-thread pump.
        private static void OnHostUiTick()
        {
            var instance = activeInstance;
            if (instance == null || instance.destroyed) return;
            instance.Dispatch(TickSource.HostUi);
        }

        internal void TryAddDashboardButton(string source)
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
                // Left button column, one vanilla pitch slot below the
                // item-store entry (y=410). Geometry and the vanilla rhythm
                // it tracks are pinned in BueMenuEntryLayout (DEV-V2-09);
                // Glazier exposes no public child enumeration, so the slot is
                // fixed rather than derived from the vanilla buttons.
                dashboardButton.PositionOffset_X = BueMenuEntryLayout.MainMenuColumnButtonX;
                dashboardButton.PositionOffset_Y = BueMenuEntryLayout.MainMenuBueSlotY;
                dashboardButton.SizeOffset_X = BueMenuEntryLayout.MainMenuColumnButtonWidth;
                dashboardButton.SizeOffset_Y = BueMenuEntryLayout.MainMenuColumnButtonHeight;
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
                throw;
            }
        }

        private static bool OnMenuEscapePrefix()
        {
            var instance = activeInstance;
            if (instance == null || !instance.opened) return true;
            instance.RequestClose();
            return false;
        }

        private static bool OnPlayerEscapePrefix()
        {
            var instance = activeInstance;
            if (instance == null || !instance.opened) return true;
            instance.RequestClose();
            return false;
        }

        private static void OnMenuCloseAllPostfix()
        {
            var instance = activeInstance;
            if (instance != null) instance.Close();
        }

        internal void TryAddMainButton(string source)
        {
            var parent = ReadContainer(workshopContainerField);
            if (parent != null && !IsAlive(parent))
            {
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
                if (mainButton == null) throw new InvalidOperationException("Glazier.CreateButton returned null");
                mainButton.PositionOffset_X = -100f;
                mainButton.PositionOffset_Y = 185f;
                mainButton.PositionScale_X = 0.5f;
                mainButton.PositionScale_Y = 0.5f;
                mainButton.SizeOffset_X = 200f;
                mainButton.SizeOffset_Y = 50f;
                mainButton.Text = "BUE 插件管理";
                mainButton.TooltipText = "查看已加载的 BepInEx 插件，并可在游戏内修改其配置";
                mainButton.FontSize = ESleekFontSize.Medium;
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
                throw;
            }
        }

        internal void TryAddPauseButton(string source)
        {
            // Fail closed when a required vanilla column field is missing:
            // shifting part of the column would park the BUE entry on top of
            // a native element (native fallback over a broken layout).
            if (!pauseRequiredShiftFieldsPresent)
            {
                if (!pauseFieldGapLogged)
                {
                    pauseFieldGapLogged = true;
                    LogTrace("pause-shift-fields-missing", "field=" + missingPauseShiftField + " decision=skip-pause-entry");
                }
                return;
            }
            var parent = ReadContainer(pauseContainerField);
            if (parent != null && !IsAlive(parent))
            {
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
            // Runtime gate: every required column element must be readable and
            // alive before the column is touched, so the entry never overlaps
            // a half-shifted native column (rebuild windows resolve within a
            // tick or two; while unresolved the pause entry stays native).
            var elements = ResolvePauseColumnElements();
            var columnReady = elements != null;
            if (columnReady != lastPauseColumnReady)
            {
                lastPauseColumnReady = columnReady;
                LogTrace("pause-column-ready", "ready=" + (columnReady ? "true" : "false") + " source=" + source);
            }
            if (!columnReady)
            {
                if (pauseParent != null)
                {
                    CleanupPauseButton(pauseParent);
                    pauseParent = null;
                }
                return;
            }
            if (RequiresParentRebind(pauseParent, parent))
            {
                if (pauseParent != null)
                {
                    CleanupPauseButton(pauseParent);
                    pauseParent = null;
                }
                CreatePauseButton(parent, source);
            }
            // Transactional tick: the column either shifts as a whole (and the
            // freshly created button appears in its final slot) or the native
            // layout is handed back untouched for a retry (R3 review).
            if (!ApplyPauseColumnShift(elements, source))
            {
                if (pauseParent != null)
                {
                    CleanupPauseButton(pauseParent);
                    pauseParent = null;
                }
            }
        }

        private void CreatePauseButton(ISleekElement parent, string source)
        {
            try
            {
                LogTrace("create-button-begin", "surface=PlayerPauseUI source=" + source);
                pauseButton = Glazier.Get().CreateButton();
                LogTrace("create-button-result", "surface=PlayerPauseUI created=" + (pauseButton != null) + " source=" + source);
                if (pauseButton == null) throw new InvalidOperationException("Glazier.CreateButton returned null");
                // Native column slot directly below Return (DEV-V2-13);
                // ApplyPauseColumnShift moves the vanilla elements below Return
                // down one pitch and mirrors the column X so this entry also
                // follows the vanilla spy-mode column move.
                pauseButton.PositionOffset_X = BueMenuEntryLayout.PauseColumnButtonX;
                pauseButton.PositionOffset_Y = BueMenuEntryLayout.PauseBueSlotY;
                pauseButton.PositionScale_X = 0.5f;
                pauseButton.PositionScale_Y = 0.5f;
                pauseButton.SizeOffset_X = BueMenuEntryLayout.PauseColumnButtonWidth;
                pauseButton.SizeOffset_Y = BueMenuEntryLayout.PauseColumnButtonHeight;
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
                throw;
            }
        }

        // DEV-V2-13: resolves the manifest elements for this tick. Returns
        // null when a required element is absent, dead or unreadable - the
        // column must never be partially shifted, so the pause entry stays
        // native for that tick (fail closed, R2 review). Optional elements
        // (inviteFriendsButton) may be absent and come back as null entries.
        private ISleekElement[] ResolvePauseColumnElements()
        {
            var names = BueMenuEntryLayout.PauseShiftFieldNames;
            var optional = BueMenuEntryLayout.PauseOptionalShiftFieldNames;
            var elements = new ISleekElement[names.Length];
            for (var index = 0; index < pauseShiftFields.Length; index++)
            {
                var fieldName = names[index];
                ISleekElement element = null;
                try
                {
                    if (pauseShiftFields[index] != null) element = pauseShiftFields[index].GetValue(null) as ISleekElement;
                }
                catch (Exception error)
                {
                    if (!loggedPauseElementReadFailures.Contains(fieldName))
                    {
                        loggedPauseElementReadFailures.Add(fieldName);
                        LogTrace("pause-element-read-failed", "field=" + fieldName + " errorType=" + error.GetType().Name + " message=" + error.Message);
                    }
                    return null;
                }
                if (element == null || !IsAlive(element))
                {
                    if (Array.IndexOf(optional, fieldName) < 0) return null;
                    continue;
                }
                elements[index] = element;
            }
            return elements;
        }

        // DEV-V2-13: moves every vanilla element below Return down one pitch
        // (anchored via BuePauseColumnShift, so repeated ticks and UI rebuilds
        // never drift) to free the second slot for the BUE entry, and mirrors
        // the native column X (spy mode moves the column to -435 and vanilla
        // never moves it back until the UI rebuilds - quirk kept).
        // Transactional: reads all Ys first (any read failure aborts before
        // anything moves); a write failure rolls the already-shifted elements
        // back to their anchors. Returns false when the tick must hand the
        // native column back untouched.
        private bool ApplyPauseColumnShift(ISleekElement[] elements, string source)
        {
            var currentYs = new float[elements.Length];
            for (var index = 0; index < elements.Length; index++)
            {
                var element = elements[index];
                if (element == null) continue;
                try
                {
                    currentYs[index] = element.PositionOffset_Y;
                }
                catch (Exception error)
                {
                    LogTrace("pause-shift-failed", "phase=read field=" + BueMenuEntryLayout.PauseShiftFieldNames[index] + " errorType=" + error.GetType().Name + " message=" + error.Message);
                    return false;
                }
            }
            var anchored = 0;
            var columnX = 0f;
            var haveColumnX = false;
            for (var index = 0; index < elements.Length; index++)
            {
                var element = elements[index];
                if (element == null) continue;
                try
                {
                    if (pauseColumnShift.Apply(element, currentYs[index], value => element.PositionOffset_Y = value)) anchored++;
                    if (!haveColumnX)
                    {
                        columnX = element.PositionOffset_X;
                        haveColumnX = true;
                    }
                }
                catch (Exception error)
                {
                    LogTrace("pause-shift-failed", "phase=write field=" + BueMenuEntryLayout.PauseShiftFieldNames[index] + " errorType=" + error.GetType().Name + " message=" + error.Message);
                    RestorePauseColumn();
                    return false;
                }
            }
            try
            {
                if (haveColumnX && pauseButton != null && IsAlive(pauseButton) && Math.Abs(pauseButton.PositionOffset_X - columnX) > 0.01f)
                {
                    pauseButton.PositionOffset_X = columnX;
                }
            }
            catch (Exception error)
            {
                LogTrace("pause-shift-failed", "phase=mirror errorType=" + error.GetType().Name + " message=" + error.Message);
                RestorePauseColumn();
                return false;
            }
            if (anchored > 0) LogTrace("pause-column-shift", "anchored=" + anchored + " source=" + source);
            return true;
        }

        // Hands the vanilla layout back on BUE teardown or pause-UI rebind.
        // Walks the anchor registry instead of the static fields (a rebuild
        // may already have repointed those): live instances restore their
        // captured Y, dead instances just drop their anchor, and a failed
        // restore keeps its anchor for a retry. Nothing is swept while a
        // retryable anchor remains.
        private void RestorePauseColumn()
        {
            var restored = 0;
            var dropped = 0;
            var failed = 0;
            foreach (var anchor in pauseColumnShift.SnapshotAnchors())
            {
                var element = anchor.Key as ISleekElement;
                if (element == null || !IsAlive(element))
                {
                    pauseColumnShift.RemoveAnchor(anchor.Key);
                    dropped++;
                    continue;
                }
                try
                {
                    if (pauseColumnShift.Restore(element, value => element.PositionOffset_Y = value)) restored++;
                }
                catch (Exception error)
                {
                    failed++;
                    LogTrace("pause-restore-failed", "errorType=" + error.GetType().Name + " message=" + error.Message);
                }
            }
            if (restored > 0 || dropped > 0 || failed > 0) LogTrace("pause-column-restore", "restored=" + restored + " dropped=" + dropped + " failed=" + failed);
        }

        private static string FindFirstMissingRequiredShiftField(FieldInfo[] fields)
        {
            var optional = BueMenuEntryLayout.PauseOptionalShiftFieldNames;
            for (var index = 0; index < fields.Length; index++)
            {
                if (fields[index] != null) continue;
                var name = BueMenuEntryLayout.PauseShiftFieldNames[index];
                var isOptional = false;
                for (var optionalIndex = 0; optionalIndex < optional.Length; optionalIndex++)
                {
                    if (optional[optionalIndex] == name)
                    {
                        isOptional = true;
                        break;
                    }
                }
                if (!isOptional) return name;
            }
            return null;
        }

        internal static FieldInfo[] ResolvePlayerPauseShiftFields()
        {
            var names = BueMenuEntryLayout.PauseShiftFieldNames;
            var fields = new FieldInfo[names.Length];
            for (var index = 0; index < names.Length; index++)
            {
                // exitButton/quitButton are public static in PlayerPauseUI,
                // the rest are non-public - both must resolve (DEV-V2-13 R1).
                fields[index] = typeof(PlayerPauseUI).GetField(names[index], BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            return fields;
        }

        // Optional secondary entry on the Workshop sub-page.  The Dashboard
        // button above is the canonical main-menu entry required by DEV-16B.
        private void OnMainButtonClicked(ISleekElement button) { Open(MenuUI.container, ReadContainer(workshopContainerField)); }
        private void OnPauseButtonClicked(ISleekElement button) { Open(PlayerUI.container, ReadContainer(pauseContainerField)); }
        private void OnDashboardButtonClicked(ISleekElement button) { Open(MenuUI.container, ReadContainer(dashboardContainerField)); }

        private void Open(ISleekElement host, ISleekElement origin)
        {
            if (host == null || destroyed) return;
            try
            {
                EnsurePanel();
                if (refreshModel != null) refreshModel();
                host.AddChild(panel);
                opened = true;
                hiddenOrigin = origin as SleekFullscreenBox;
                if (hiddenOrigin != null && !ReferenceEquals(hiddenOrigin, panel)) hiddenOrigin.AnimateOutOfView(0f, 1f);
                panel.AnimateIntoView();
                Render();
            }
            catch (Exception error) { Log("open management panel failed: " + error.Message); }
        }

        private void EnsurePanel()
        {
            if (panel != null && IsAlive(panel)) return;
            panel = new SleekFullscreenBox();
            // Full-bleed overlay that tracks the game resolution: scale 1,1
            // with symmetric 10px margins, the same pattern the vanilla
            // MenuDashboardUI.container uses. The previous PositionScale_Y=-1
            // pinned the panel to the bottom edge and squashed it at higher
            // resolutions.
            panel.PositionOffset_X = 10f;
            panel.PositionOffset_Y = 10f;
            panel.SizeOffset_X = -20f;
            panel.SizeOffset_Y = -20f;
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
            title.SizeOffset_X = -300f;
            title.SizeScale_X = 1f;
            title.SizeOffset_Y = 40f;
            title.Text = "Better Unturned Experience · 插件管理";
            title.FontSize = ESleekFontSize.Large;
            title.TextAlignment = TextAnchor.MiddleLeft;
            panel.AddChild(title);

            refreshButton = Glazier.Get().CreateButton();
            refreshButton.PositionOffset_X = -260f;
            refreshButton.PositionOffset_Y = 20f;
            refreshButton.PositionScale_X = 1f;
            refreshButton.SizeOffset_X = 100f;
            refreshButton.SizeOffset_Y = 40f;
            refreshButton.Text = "刷新";
            refreshButton.OnClicked += OnRefreshClicked;
            panel.AddChild(refreshButton);

            sortAscendingButton = Glazier.Get().CreateButton();
            sortAscendingButton.PositionOffset_X = 24f;
            sortAscendingButton.PositionOffset_Y = 62f;
            sortAscendingButton.SizeOffset_X = 92f;
            sortAscendingButton.SizeOffset_Y = 32f;
            sortAscendingButton.Text = "A-Z";
            sortAscendingButton.OnClicked += OnSortAscendingClicked;
            panel.AddChild(sortAscendingButton);

            sortDescendingButton = Glazier.Get().CreateButton();
            sortDescendingButton.PositionOffset_X = 122f;
            sortDescendingButton.PositionOffset_Y = 62f;
            sortDescendingButton.SizeOffset_X = 92f;
            sortDescendingButton.SizeOffset_Y = 32f;
            sortDescendingButton.Text = "Z-A";
            sortDescendingButton.OnClicked += OnSortDescendingClicked;
            panel.AddChild(sortDescendingButton);

            closeButton = Glazier.Get().CreateButton();
            closeButton.PositionOffset_X = -130f;
            closeButton.PositionOffset_Y = 20f;
            closeButton.PositionScale_X = 1f;
            closeButton.SizeOffset_X = 110f;
            closeButton.SizeOffset_Y = 40f;
            closeButton.Text = "关闭";
            closeButton.OnClicked += OnCloseClicked;
            panel.AddChild(closeButton);

            listScroll = Glazier.Get().CreateScrollView();
            listScroll.PositionOffset_X = 24f;
            listScroll.PositionOffset_Y = 104f;
            listScroll.SizeOffset_X = 270f;
            listScroll.SizeOffset_Y = -136f;
            listScroll.SizeScale_Y = 1f;
            listScroll.ScaleContentToWidth = true;
            listScroll.HandleScrollWheel = true;
            panel.AddChild(listScroll);

            detailScroll = Glazier.Get().CreateScrollView();
            detailScroll.PositionOffset_X = 310f;
            detailScroll.PositionOffset_Y = 62f;
            detailScroll.SizeOffset_X = -334f;
            detailScroll.SizeOffset_Y = -94f;
            detailScroll.SizeScale_X = 1f;
            detailScroll.SizeScale_Y = 1f;
            detailScroll.ScaleContentToWidth = true;
            detailScroll.HandleScrollWheel = true;
            panel.AddChild(detailScroll);

            status = Glazier.Get().CreateLabel();
            status.PositionOffset_X = 24f;
            status.PositionOffset_Y = -28f;
            status.PositionScale_Y = 1f;
            status.SizeOffset_X = -48f;
            status.SizeScale_X = 1f;
            status.SizeOffset_Y = 24f;
            status.TextAlignment = TextAnchor.MiddleLeft;
            panel.AddChild(status);
        }

        private void Render()
        {
            if (listScroll == null || detailScroll == null) return;
            var rows = runtime.Model.GetEntries();
            ClearChildren(listScroll);
            var y = 0;
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var capturedId = row.StableId;
                var button = Glazier.Get().CreateButton();
                button.PositionOffset_Y = y;
                button.SizeOffset_X = -8f;
                button.SizeOffset_Y = 38f;
                button.SizeScale_X = 1f;
                button.Text = (row.IsFavorite ? "★ " : "☆ ") + row.DisplayName;
                button.TooltipText = row.StableId + "\n版本: " + row.Version;
                button.OnClicked += delegate(ISleekElement ignored)
                {
                    SelectEntry(capturedId);
                };
                listScroll.AddChild(button);
                y += 42;
            }
            listScroll.ContentSizeOffset = new Vector2(0f, y);
            if (rows.Count == 0) SetStatusWithPlatformNotice("当前没有检测到已加载的外部插件。", false);
            RenderDetails();
        }

        // DEV-V2-23 (F1 R1): the double-install notice outranks every other
        // status text and must survive the empty-catalog / unselected early
        // exits — a diagnosis the player must see never yields to a benign
        // fallback line.
        private void SetStatusWithPlatformNotice(string fallbackText, bool fallbackError)
        {
            if (!string.IsNullOrEmpty(runtime.Model.DoubleInstallNotice)) SetStatus(runtime.Model.DoubleInstallNotice, true);
            else SetStatus(fallbackText, fallbackError);
        }

        private void RenderDetails()
        {
            if (detailScroll == null) return;
            ClearChildren(detailScroll);
            var rows = runtime.Model.GetEntries();
            ManagementEntryView selected = default(ManagementEntryView);
            var found = false;
            for (var index = 0; index < rows.Count; index++)
            {
                if (string.Equals(rows[index].StableId, selectedStableId, StringComparison.Ordinal))
                {
                    selected = rows[index];
                    found = true;
                    break;
                }
            }
            if (!found && rows.Count > 0)
            {
                selected = rows[0];
                selectedStableId = selected.StableId;
                found = true;
            }
            var y = 0;
            if (!found)
            {
                AddDetailLabel(ref y, "选择一个功能或插件查看详情。", ESleekFontSize.Medium);
                detailScroll.ContentSizeOffset = new Vector2(0f, y + 10);
                if (!string.IsNullOrEmpty(runtime.Model.DoubleInstallNotice)) SetStatus(runtime.Model.DoubleInstallNotice, true);
                return;
            }
            // Align the model's logical panel session with what is displayed.
            // Re-opening the current entry keeps the in-memory draft (重挂不丢).
            runtime.Model.OpenDetail(selected.StableId);
            AddDetailLabel(ref y, selected.DisplayName, ESleekFontSize.Large);
            AddDetailLabel(ref y, "身份：" + selected.StableId, ESleekFontSize.Small);
            AddDetailLabel(ref y, "版本：" + selected.Version, ESleekFontSize.Small);
            AddDetailLabel(ref y, "收藏：" + (selected.IsFavorite ? "是" : "否"), ESleekFontSize.Small);
            var favoriteButton = Glazier.Get().CreateButton();
            favoriteButton.PositionOffset_Y = y;
            favoriteButton.SizeOffset_X = 180f;
            favoriteButton.SizeOffset_Y = 32f;
            favoriteButton.Text = selected.IsFavorite ? "取消收藏" : "加入收藏";
            var favoriteId = selected.StableId;
            favoriteButton.OnClicked += delegate(ISleekElement ignored)
            {
                runtime.Model.ToggleFavorite(favoriteId);
                Render();
            };
            detailScroll.AddChild(favoriteButton);
            y += 40;
            if (selected.Kind == ManagementEntryKind.BueFeature)
            {
                AddDetailLabel(ref y, "BUE 功能设置", ESleekFontSize.Medium);
                for (var index = 0; index < selected.BueSettings.Count; index++)
                {
                    AddBueSettingControl(ref y, selected, selected.BueSettings[index]);
                }
                AddDetailLabel(ref y, "功能状态：" + selected.FeatureState, ESleekFontSize.Small);
                AddDetailLabel(ref y, "表现状态：" + selected.Presentation.State, ESleekFontSize.Small);
                // DEV-V2-06: the network module's card carries the takeover
                // status, the no-op config migration line, and the reversible
                // hand-back button while the takeover is active.
                if (selected.StableId == NetworkModuleAdapter.NetworkFeature.Value) RenderNetworkTakeoverDetails(ref y);
            }
            else
            {
                AddDetailLabel(ref y, "普通 BepInEx ConfigEntry", ESleekFontSize.Medium);
                for (var index = 0; index < selected.PluginConfig.Count; index++)
                {
                    AddPluginConfigControl(ref y, selected, selected.PluginConfig[index]);
                }
            }
            // DEV-V4-01: draft actions. When a navigation-away is armed show the
            // confirm bar; otherwise show 保存配置 (always visible per Q31 — a
            // clean click is a no-op, not a hidden button) plus 放弃修改 when
            // the open draft is dirty. The buttons never navigate on their own —
            // 保存配置 keeps the panel open and re-renders from the fresh
            // authoritative snapshot.
            if (pendingNavigation != null)
            {
                AddConfirmBar(ref y);
            }
            else
            {
                AddDraftActionButtons(ref y);
            }

            detailScroll.ContentSizeOffset = new Vector2(0f, y + 10);
            // DEV-V2-23: the double-install diagnosis outranks the compatibility
            // notice — the player must remove an unofficial copy before anything
            // else matters.
            if (!string.IsNullOrEmpty(runtime.Model.DoubleInstallNotice)) SetStatus(runtime.Model.DoubleInstallNotice, true);
            else if (runtime.Model.ExternalManagerDetected) SetStatus(runtime.Model.CompatibilityNotice, false);
        }

        private void AddBueSettingControl(ref int y, ManagementEntryView row, SettingEntryView setting)
        {
            // DEV-V4-01: the setting edit lands in the draft (no authoritative
            // write); the row shows the draft's effective value and is tagged
            // 未保存 when it differs from the authoritative snapshot. Read-only
            // and ServerAuthority rows are never editable and never enter the draft.
            var dirty = runtime.Model.IsBueSettingDirty(setting.SettingId);
            var effective = runtime.Model.EffectiveBueSettingValue(setting.SettingId);
            AddDetailLabel(ref y, setting.SettingId + " = " + FormatSetting(effective) + (dirty ? "（未保存）" : string.Empty), ESleekFontSize.Small);
            if (!setting.CanEdit || setting.Authority == SettingAuthority.ServerAuthoritative) return;
            if (effective.Kind == SettingKind.Toggle)
            {
                var toggle = Glazier.Get().CreateToggle();
                toggle.PositionOffset_Y = y;
                toggle.SizeOffset_X = 40f;
                toggle.SizeOffset_Y = 30f;
                toggle.Value = effective.Boolean;
                toggle.OnValueChanged += delegate(ISleekToggle ignored, bool value)
                {
                    runtime.Model.DraftEditBueSetting(setting.SettingId, PluginConfigValue.BooleanValue(value));
                    RenderDetails();
                };
                detailScroll.AddChild(toggle);
                y += 36;
            }
            else
            {
                AddTextEditor(ref y, setting.SettingId, FormatSetting(effective), raw =>
                {
                    PluginConfigValue value;
                    if (!TryConvertSetting(effective.Kind, raw, out value)) return false;
                    if (!runtime.Model.DraftEditBueSetting(setting.SettingId, value)) return false;
                    RenderDetails();
                    return true;
                });
            }
        }

        // DEV-V2-06: takeover status + empty-migration line + the reversible
        // "hand back to standalone LMN" button. The button only turns the
        // official network switch off through the settings contract — LMN's
        // own prefix then resumes standalone, and turning the switch back on
        // re-arms the takeover (no files are touched).
        private void RenderNetworkTakeoverDetails(ref int y)
        {
            var adapter = NetworkModuleFeatureRegistration.WiredAdapter;
            if (adapter == null) return;
            AddDetailLabel(ref y, "接管状态：" + adapter.TakeoverStatus, ESleekFontSize.Small);
            AddDetailLabel(ref y, "配置迁移：" + adapter.ConfigMigrationStatus, ESleekFontSize.Small);
            if (!adapter.TakeoverActive) return;
            var revertButton = Glazier.Get().CreateButton();
            revertButton.PositionOffset_Y = y;
            revertButton.SizeOffset_X = 220f;
            revertButton.SizeOffset_Y = 32f;
            revertButton.Text = "让我改回独立 LMN";
            revertButton.OnClicked += delegate(ISleekElement ignored)
            {
                var result = runtime.Model.TryEditBueSetting(NetworkModuleAdapter.NetworkFeature, "network.enabled", PluginConfigValue.BooleanValue(false));
                SetStatus(result.Accepted ? "BUE 网络模块已关闭，独立 LMN 恢复运行。" : "BUE 设置被拒绝。", !result.Accepted);
                if (result.Accepted) RenderDetails();
            };
            detailScroll.AddChild(revertButton);
            y += 40;
        }

        private void AddPluginConfigControl(ref int y, ManagementEntryView row, PluginConfigEntryView entry)
        {
            // DEV-V4-01: external ConfigEntry edits enter the same draft model.
            var dirty = runtime.Model.IsPluginConfigDirty(entry.Key);
            var draft = runtime.Model.EffectivePluginConfigValue(entry.Key);
            var display = draft.Kind == PluginConfigValueKind.Unsupported ? entry.Value : draft;
            AddDetailLabel(ref y, entry.DisplayName + " = " + FormatPluginValue(display) + (entry.RequiresRestart ? "（需要重启）" : string.Empty) + (dirty ? "（未保存）" : string.Empty), ESleekFontSize.Small);
            if (!entry.CanEdit) return;
            var pluginGuid = row.StableId;
            if (entry.Kind == PluginConfigValueKind.Boolean)
            {
                var toggle = Glazier.Get().CreateToggle();
                toggle.PositionOffset_Y = y;
                toggle.SizeOffset_X = 40f;
                toggle.SizeOffset_Y = 30f;
                toggle.Value = display.Boolean;
                toggle.OnValueChanged += delegate(ISleekToggle ignored, bool value)
                {
                    runtime.Model.DraftEditPluginConfig(pluginGuid, entry.Key, value ? "true" : "false");
                    RenderDetails();
                };
                detailScroll.AddChild(toggle);
                y += 36;
            }
            else
            {
                AddTextEditor(ref y, entry.Key, FormatPluginValue(display), raw =>
                {
                    if (!runtime.Model.DraftEditPluginConfig(pluginGuid, entry.Key, raw)) return false;
                    RenderDetails();
                    return true;
                });
            }
        }

        // ── DEV-V4-01 draft navigation (保存 / 放弃 / 确认 command surface) ──

        // Switching the selected entry while a dirty draft is open arms the
        // confirm instead of silently dropping the edits (V4-T2 Q22).
        private void SelectEntry(string stableId)
        {
            if (runtime.Model.IsDirty && !string.Equals(stableId, selectedStableId, StringComparison.Ordinal))
            {
                pendingRefresh = false;
                pendingNavigation = stableId;
                RenderDetails();
                return;
            }
            pendingNavigation = null;
            selectedStableId = stableId;
            runtime.Model.OpenDetail(stableId);
            RenderDetails();
        }

        private void RequestRefresh()
        {
            if (runtime.Model.IsDirty)
            {
                pendingNavigation = selectedStableId ?? string.Empty;
                pendingRefresh = true;
                RenderDetails();
                return;
            }
            pendingNavigation = null;
            pendingRefresh = false;
            if (refreshModel != null) refreshModel();
            Render();
        }

        private void RequestClose()
        {
            if (runtime.Model.IsDirty)
            {
                pendingNavigation = string.Empty;   // "" = the navigation-away is a close
                pendingRefresh = false;
                RenderDetails();
                return;
            }
            pendingNavigation = null;
            pendingRefresh = false;
            Close();
        }

        private void ResolveConfirm(PanelConfirmChoice choice)
        {
            var target = pendingNavigation == string.Empty ? null : pendingNavigation;
            var wasRefresh = pendingRefresh;
            DraftSaveReport report;
            var leaving = runtime.Model.TryLeaveDetail(target, choice, out report);
            pendingNavigation = null;
            pendingRefresh = false;
            // The confirm's 保存 reuses 保存配置's report verbatim (Q22 = 等同保存
            // 配置); 取消/不保存 carry no report (no write attempted).
            if (!leaving)
            {
                // 取消, or 保存 where a source failed — stay on the current entry,
                // the failed draft is preserved by the model.
                RenderDetails();
                RenderDraftReport(report);   // after re-render, so a platform notice can't wipe the banner (Q23/Q24)
                return;
            }
            if (wasRefresh && refreshModel != null) refreshModel();
            if (target == null) { Close(); return; }   // the navigation-away was a close
            selectedStableId = target;
            Render();
            RenderDraftReport(report);   // frozen save banner wins over any notice (Q30)
        }

        private void AddDraftActionButtons(ref int y)
        {
            // 保存配置 is always visible on an open detail (Q31 — a clean click is
            // a no-op, not a hidden button); 放弃修改 only when the draft is dirty.
            if (runtime.Model.IsDirty)
            {
                AddDetailLabel(ref y, "有未保存的修改。", ESleekFontSize.Small);
                AddDraftButton(ref y, "放弃修改", delegate { runtime.Model.DiscardDraft(); Render(); });
            }
            AddDraftButton(ref y, "保存配置", CommitDraftAndStatus);
        }

        private void CommitDraftAndStatus()
        {
            var report = runtime.Model.SaveDraft();
            Render();                    // re-render first — RenderDetails may restore a platform notice…
            RenderDraftReport(report);   // …then publish the frozen save banner so it wins (Q30/Q23).
        }

        // The one place the draft-save outcome becomes the top banner, so the
        // 保存配置 button and the confirm's 保存 render identical frozen text
        // (Q23/Q24/Q30). Success and NoChanges use the verbatim PrimaryMessage
        // (「配置已保存。」 / 「没有需要保存的修改。」); a partial shows 「部分
        // 保存失败」 + the per-source reasons. The RequiresRestart badge is the
        // external-config surface (DEV-V4-08), never text appended to 配置已保存。
        private void RenderDraftReport(DraftSaveReport report)
        {
            if (report == null) return;
            if (report.Outcome == DraftSaveOutcome.NoChanges || report.Outcome == DraftSaveOutcome.Success) SetStatus(report.PrimaryMessage, false);
            else SetStatus("部分保存失败：" + string.Join(" ", report.Messages), true);
        }

        private void AddConfirmBar(ref int y)
        {
            AddDetailLabel(ref y, "修改尚未保存，要保存吗？", ESleekFontSize.Medium);
            AddDraftButton(ref y, "保存", delegate { ResolveConfirm(PanelConfirmChoice.Save); });
            AddDraftButton(ref y, "不保存", delegate { ResolveConfirm(PanelConfirmChoice.Discard); });
            AddDraftButton(ref y, "取消", delegate { ResolveConfirm(PanelConfirmChoice.Cancel); });
        }

        private void AddDraftButton(ref int y, string text, System.Action onClick)
        {
            var button = Glazier.Get().CreateButton();
            button.PositionOffset_Y = y;
            button.SizeOffset_X = 220f;
            button.SizeOffset_Y = 32f;
            button.Text = text;
            button.OnClicked += delegate(ISleekElement ignored) { onClick(); };
            detailScroll.AddChild(button);
            y += 38;
        }

        private void AddTextEditor(ref int y, string key, string value, Func<string, bool> submit)
        {
            var field = Glazier.Get().CreateStringField();
            field.PositionOffset_Y = y;
            field.SizeOffset_X = 360f;
            field.SizeOffset_Y = 30f;
            field.SizeScale_X = 0f;
            field.Text = value ?? string.Empty;
            field.MaxLength = 4096;
            field.OnTextSubmitted += delegate(ISleekField submitted)
            {
                if (!submit(submitted.Text == null ? string.Empty : submitted.Text.Trim())) submitted.Text = value ?? string.Empty;
            };
            field.OnTextEscaped += delegate(ISleekField escaped) { escaped.Text = value ?? string.Empty; };
            detailScroll.AddChild(field);
            y += 36;
        }

        private static bool TryConvertSetting(SettingKind kind, string raw, out PluginConfigValue value)
        {
            value = PluginConfigValue.UnsupportedValue();
            if (kind == SettingKind.Integer)
            {
                long number;
                if (!long.TryParse(raw, out number)) return false;
                value = PluginConfigValue.IntegerValue(number);
                return true;
            }
            if (kind == SettingKind.Float)
            {
                double number;
                if (!double.TryParse(raw, out number)) return false;
                value = PluginConfigValue.FloatValue(number);
                return true;
            }
            if (kind == SettingKind.Text || kind == SettingKind.Choice || kind == SettingKind.KeyBinding)
            {
                value = PluginConfigValue.StringValue(raw);
                return true;
            }
            return false;
        }

        private static string FormatPluginValue(PluginConfigValue value)
        {
            switch (value.Kind)
            {
                case PluginConfigValueKind.Boolean: return value.Boolean ? "true" : "false";
                case PluginConfigValueKind.Integer: return value.Integer64.ToString();
                case PluginConfigValueKind.Float: return value.Float64.ToString("0.###");
                default: return value.Text ?? string.Empty;
            }
        }

        private void AddDetailLabel(ref int y, string text, ESleekFontSize fontSize)
        {
            var label = Glazier.Get().CreateLabel();
            label.PositionOffset_Y = y;
            label.SizeOffset_X = -10f;
            label.SizeOffset_Y = 28f;
            label.SizeScale_X = 1f;
            label.Text = text ?? string.Empty;
            label.FontSize = fontSize;
            label.TextAlignment = TextAnchor.MiddleLeft;
            detailScroll.AddChild(label);
            y += 30;
        }

        private static void ClearChildren(ISleekElement parent)
        {
            if (parent == null) return;
            parent.RemoveAllChildren();
        }

        private void OnRefreshClicked(ISleekElement button) { RequestRefresh(); }
        private void OnSortAscendingClicked(ISleekElement button) { runtime.Model.SetSortOrder(ManagementSortOrder.NameAscending); Render(); }
        private void OnSortDescendingClicked(ISleekElement button) { runtime.Model.SetSortOrder(ManagementSortOrder.NameDescending); Render(); }
        private void SetStatus(string text, bool error)
        {
            if (status != null) status.Text = (error ? "错误：" : string.Empty) + (text ?? string.Empty);
        }

        private void OnCloseClicked(ISleekElement button) { RequestClose(); }

        private void Close()
        {
            if (!opened) return;
            opened = false;
            // A real close ends the logical panel session (V4-T1：关面板即结束，
            // 丢未保存草稿). The dirty draft is normally confirmed-away before
            // this runs (RequestClose arms the dialog); this is the safety net
            // for the game-driven closes (CloseAll / dead-panel) where no dialog
            // is shown. The re-mount path (OnUiTreeRebuilt) never reaches here,
            // so an attached-during-play session keeps its draft.
            runtime.Model.DiscardDraft();
            pendingNavigation = null;
            pendingRefresh = false;
            try { if (panel != null && IsAlive(panel)) panel.AnimateOutOfView(0f, -1f); } catch (Exception error) { Log("close management panel failed: " + error.Message); }
            try { if (hiddenOrigin != null && IsAlive(hiddenOrigin)) hiddenOrigin.AnimateIntoView(); } catch (Exception error) { Log("restore management origin failed: " + error.Message); }
            hiddenOrigin = null;
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
                if (element is SleekWrapper wrapper)
                {
                    var implementation = wrapper.GetProxyImplementation();
                    if (implementation == null) return false;
                    var type = implementation.GetType();
                    while (type != null && type != typeof(object))
                    {
                        var property = type.GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (property != null) return property.GetValue(implementation, null) != null;
                        var field = type.GetField("<gameObject>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (field != null) return field.GetValue(implementation) != null;
                        type = type.BaseType;
                    }
                    return false;
                }
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

        internal void LogEntryFailure(string surface, Exception error)
        {
            LogTrace("entry-isolated", "surface=" + surface + " errorType=" + error.GetType().FullName + " message=" + error.Message);
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
            RestorePauseColumn();
            if (pauseButton == null) return;
            try { pauseButton.OnClicked -= OnPauseButtonClicked; } catch (Exception) { }
            try { if (parent != null) parent.RemoveChild(pauseButton); } catch (Exception) { }
            pauseButton = null;
        }

        private void LogTrace(string eventName, string details)
        {
            LogTrace(eventName, details, false);
        }

        // DEV-16G ticket C: recurring in-game panel events (menu open / UI
        // rebuild / surface open / heartbeat) route through BueRuntimeLog's
        // classifier -> Debug (silent in normal play). Load one-shots and
        // errors bypass the silent gate (Info / Error). The explicit isRuntime
        // flag remains for callers that know the classification up front (e.g.
        // the heartbeat) and is OR-ed with the classifier.
        private void LogTrace(string eventName, string details, bool isRuntime)
        {
            var line = "[BUE-UI-TRACE] plugin=" + PluginId + " featureId=" + FeatureId + " version=" + Version
                + " environmentRole=Client scenario=" + GetScenario() + " diagnosticId=" + TraceDiagnosticId
                + " event=" + eventName + " " + details;
            if (isRuntime || BueRuntimeLog.IsRuntimeEvent(eventName))
            {
                BueRuntimeLog.Runtime(line);
                return;
            }
            if (log != null)
            {
                log.LogInfo(line);
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
