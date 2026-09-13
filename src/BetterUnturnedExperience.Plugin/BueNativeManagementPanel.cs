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
        // POST-P4-07: how the F2 single entry paints after a catalog rebuild.
        // Full = list+details (save-stay / confirm-leave); Details = stay on the
        // failed draft; None = close-after-leave (catalog still rebuilds, no frame).
        private enum AfterCommitPaint : byte { Full, Details, None }

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
        // DEV-V4-09 F4（实机发现）：插件 GUID 与功能 id 可同串（NoOp 样板），
        // StableId 单独不再唯一标识一行——选中态必须携带行种类，路由按
        // (种类, StableId) 双键解析，功能页不再被同键插件行遮蔽。
        private ManagementEntryKind selectedKind;
        // POST-P4-04: 外部配置详情区的分类导航筛选态（UPM Unturned.Category 标签）。
        // 归属键=选中的 StableId——换插件即复位；同插件的失效分类另由重绘时的
        // 存在性校验兜底。纯渲染状态，模型侧过滤/集合经 GetPluginConfigRows/
        // GetPluginConfigCategories 两个投影缝。
        private string selectedPluginCategory;
        private string categoryOwnerStableId;
        // DEV-V4-01: unsaved-draft navigation. The immediate-write path is
        // retired — a settings/config edit lands in the model draft and only
        // "保存配置" reaches the authoritative source. When a dirty draft is
        // open, switching entries / refreshing / closing arms the "修改尚未
        // 保存，要保存吗？" confirm (model command TryLeaveDetail), never
        // silently dropping the draft.
        private string pendingNavigation;   // null = none; "" = pending close; else target stableId
        private ManagementEntryKind? pendingNavigationKind;   // F4: the clicked row's kind (null = legacy)
        private bool pendingRefresh;         // reload the model after leaving the current draft
        // DEV-V4-08 (V4-T7 Q67) / POST-P4-03: the top「需要重启」badge lives on the
        // model (ShowsRestartBadge), keyed by (种类, StableId) of the last save
        // that attempted writes. Native chrome only reads it.
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
                var capturedKind = row.Kind;
                var button = Glazier.Get().CreateButton();
                button.PositionOffset_Y = y;
                button.SizeOffset_X = -8f;
                button.SizeOffset_Y = 38f;
                button.SizeScale_X = 1f;
                button.Text = (row.IsFavorite ? "★ " : "☆ ") + row.DisplayName;
                button.TooltipText = row.StableId + "\n版本: " + row.Version;
                button.OnClicked += delegate(ISleekElement ignored)
                {
                    SelectEntry(capturedId, capturedKind);
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
                // F4：(种类, StableId) 双键匹配——同键的功能行/插件行是两个页面。
                if (string.Equals(rows[index].StableId, selectedStableId, StringComparison.Ordinal)
                    && rows[index].Kind == selectedKind)
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
                selectedKind = selected.Kind;
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
            runtime.Model.OpenDetail(selected.StableId, selected.Kind);
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
            var favoriteKind = selected.Kind;
            favoriteButton.OnClicked += delegate(ISleekElement ignored)
            {
                runtime.Model.ToggleFavorite(favoriteId, favoriteKind);
                Render();
            };
            detailScroll.AddChild(favoriteButton);
            y += 40;
            // Q67: lit only when the last attempted save wrote a
            // RequiresRestart item successfully; row markers point at the items.
            if (runtime.Model.ShowsRestartBadge)
                AddDetailLabel(ref y, "需要重启", ESleekFontSize.Small);
            if (selected.Kind == ManagementEntryKind.BueFeature)
            {
                // DEV-V4-07 (V4-T6 Q60): the feature-level one-liner from the
                // chrome copy table — drawn only when the table has the entry
                // (无表则不画、不占位；外部插件分支不画，Q60 对照表只覆盖 BUE
                // 功能目录). The sentence never replaces the status lines below.
                var featureDescription = runtime.Model.GetFeatureDescription(selected.StableId);
                if (!string.IsNullOrEmpty(featureDescription))
                    AddDetailLabel(ref y, featureDescription, ESleekFontSize.Small);
                AddDetailLabel(ref y, "BUE 功能设置", ESleekFontSize.Medium);
                // DEV-V4-02: the panel draws the model row projection (join of
                // snapshot entry + feature schema + draft state), not the raw
                // contract entry — 显示名→描述→控件 lives in AddBueSettingRow.
                var settings = runtime.Model.GetSettingRows(selected.StableId);
                for (var index = 0; index < settings.Count; index++)
                {
                    AddBueSettingRow(ref y, settings[index]);
                }
                // DEV-V4-05: the lifecycle status surface — the read-only state
                // line in player copy (Q46: never FeatureState.ToString()), the
                // isolation reason only when it has a value (不占空位), the
                // presentation state as its own line, and the 启用 toggle as
                // the SAVED TARGET (Q44: draft-backed, 保存配置才交给生命周期
                // 机；目标≠现状时提示待生效，不承诺一定成功). Entries without a
                // stoppable lifecycle seam (Incompatible/unmapped) draw no
                // toggle — the model refuses their draft target too.
                var status = runtime.Model.GetFeatureStatusProjection(selected.StableId);
                AddDetailLabel(ref y, "功能状态：" + status.StateText, ESleekFontSize.Small);
                if (!string.IsNullOrEmpty(status.IsolationReason))
                    AddDetailLabel(ref y, "隔离原因：" + status.IsolationReason, ESleekFontSize.Small);
                AddDetailLabel(ref y, "表现状态：" + status.PresentationText, ESleekFontSize.Small);
                if (status.ShowsEnableToggle)
                {
                    AddDetailLabel(ref y, "启用（保存后生效）", ESleekFontSize.Small);
                    var enableToggle = Glazier.Get().CreateToggle();
                    enableToggle.PositionOffset_Y = y;
                    enableToggle.SizeOffset_X = 40f;
                    enableToggle.SizeOffset_Y = 30f;
                    enableToggle.Value = status.EnableToggleTarget;
                    enableToggle.OnValueChanged += delegate(ISleekToggle ignored, bool value)
                    {
                        runtime.Model.DraftSetFeatureEnabled(value);
                        RenderDetails();
                    };
                    detailScroll.AddChild(enableToggle);
                    y += 36;
                    if (!string.IsNullOrEmpty(status.PendingEffectText))
                        AddDetailLabel(ref y, status.PendingEffectText, ESleekFontSize.Small);
                }
                // DEV-V2-06: the network module's card carries the takeover
                // status, the no-op config migration line, and the reversible
                // hand-back button while the takeover is active.
                if (selected.StableId == NetworkModuleAdapter.NetworkFeature.Value) RenderNetworkTakeoverDetails(ref y);
            }
            else
            {
                AddDetailLabel(ref y, "普通 BepInEx ConfigEntry", ESleekFontSize.Medium);
                // DEV-V4-02: external rows go through the same projection and
                // the same 显示名→描述→控件 structure (同权).
                // POST-P4-04: 多分类才画 chips 导航（单分类=「不画多余导航」）；
                // 筛选态换插件复位，失效分类回落首条。
                var categories = runtime.Model.GetPluginConfigCategories(selected.StableId);
                if (categoryOwnerStableId != selected.StableId)
                {
                    categoryOwnerStableId = selected.StableId;
                    selectedPluginCategory = null;
                }
                IReadOnlyList<PanelConfigRowView> configRows;
                if (categories.Count > 1)
                {
                    var known = false;
                    for (var categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
                        if (string.Equals(categories[categoryIndex], selectedPluginCategory, StringComparison.Ordinal)) known = true;
                    if (string.IsNullOrEmpty(selectedPluginCategory) || !known) selectedPluginCategory = categories[0];
                    y = RenderCategoryChips(y, categories);
                    configRows = runtime.Model.GetPluginConfigRows(selected.StableId, selectedPluginCategory);
                }
                else
                {
                    selectedPluginCategory = null;
                    configRows = runtime.Model.GetPluginConfigRows(selected.StableId);
                }
                for (var index = 0; index < configRows.Count; index++)
                {
                    AddPluginConfigRow(ref y, selected.StableId, configRows[index]);
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

        // DEV-V4-02 (V4-T3 Q37/Q38/Q41/Q43): one setting row = 显示名 → 描述 →
        // 控件. The editable rows no longer print "SettingId = 值" — the current
        // value lives ON the control (toggle state / cycle button text / editor
        // text), and the name label is the descriptor's literal display name.
        // An empty description is NOT painted and takes no space (Q37).
        // A read-only row (ServerAuthority / CanEdit=false / Choice without
        // levels) paints the current value as a read-only label and draws NO
        // greyed-out fake control (Q43). All edits land in the draft only —
        // the authoritative source is touched solely by 保存配置 (DEV-V4-01).
        private void AddBueSettingRow(ref int y, PanelSettingRowView row)
        {
            AddDetailLabel(ref y, row.DisplayName + (row.IsDirty ? "（未保存）" : string.Empty), ESleekFontSize.Small);
            if (!string.IsNullOrEmpty(row.Description)) AddDetailLabel(ref y, row.Description, ESleekFontSize.Small);
            var settingId = row.SettingId;
            switch (row.ControlKind)
            {
                case PanelSettingControlKind.Cycle:
                    var cycle = Glazier.Get().CreateButton();
                    cycle.PositionOffset_Y = y;
                    cycle.SizeOffset_X = 220f;
                    cycle.SizeOffset_Y = 32f;
                    cycle.Text = FormatSetting(row.EffectiveValue);
                    cycle.TooltipText = "左键：下一档；右键：上一档";
                    cycle.OnClicked += delegate(ISleekElement ignored)
                    {
                        runtime.Model.DraftCycleBueSetting(settingId, 1);
                        RenderDetails();
                    };
                    cycle.OnRightClicked += delegate(ISleekElement ignored)
                    {
                        runtime.Model.DraftCycleBueSetting(settingId, -1);
                        RenderDetails();
                    };
                    detailScroll.AddChild(cycle);
                    y += 38;
                    break;
                case PanelSettingControlKind.Toggle:
                    var toggle = Glazier.Get().CreateToggle();
                    toggle.PositionOffset_Y = y;
                    toggle.SizeOffset_X = 40f;
                    toggle.SizeOffset_Y = 30f;
                    toggle.Value = row.EffectiveValue.Boolean;
                    toggle.OnValueChanged += delegate(ISleekToggle ignored, bool value)
                    {
                        runtime.Model.DraftEditBueSetting(settingId, PluginConfigValue.BooleanValue(value));
                        RenderDetails();
                    };
                    detailScroll.AddChild(toggle);
                    y += 36;
                    break;
                case PanelSettingControlKind.TextEditor:
                    var kind = row.Kind;
                    AddTextEditor(ref y, settingId, FormatSetting(row.EffectiveValue), raw =>
                    {
                        PluginConfigValue value;
                        if (!TryConvertSetting(kind, raw, out value)) return false;
                        return runtime.Model.DraftEditBueSetting(settingId, value);
                    }, RenderDetails);
                    break;
                default:
                    AddDetailLabel(ref y, "当前值：" + FormatSetting(row.EffectiveValue), ESleekFontSize.Small);
                    break;
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
                // DEV-V4-04: the legacy network.enabled setting retired — the
                // hand-back submits the disable TARGET through the machine
                // seam (Q21: 独立行动命令，不进草稿), the machine records the
                // durable UserDisabled intent, and the adapter refreshes so a
                // recorded intent disarms the takeover in the same breath.
                var accepted = runtime.Model.TryToggleFeature(NetworkModuleAdapter.NetworkFeature, false);
                if (accepted && NetworkModuleFeatureRegistration.WiredAdapter != null)
                    NetworkModuleFeatureRegistration.WiredAdapter.RefreshSwitches();
                SetStatus(accepted ? "BUE 网络模块已关闭，独立 LMN 恢复运行。" : "BUE 设置被拒绝。", !accepted);
                if (accepted) RenderDetails();
            };
            detailScroll.AddChild(revertButton);
            y += 40;
        }

        // DEV-V4-02: external ConfigEntry rows share the BUE row structure —
        // 显示名 → 描述（空不画）→ 控件, the same cycle control wherever the
        // entry carries 档位 (AllowedChoices arrives with DEV-V4-08's
        // collection; the seam is live here), and a read-only value line with
        // NO fake control where it doesn't. Edits enter the same draft.
        // 需要重启 stays a row-level 固有属性 marker (DEV-V4-08 owns badges).
        private void AddPluginConfigRow(ref int y, string pluginGuid, PanelConfigRowView row)
        {
            var key = row.Key;
            AddDetailLabel(ref y, row.DisplayName
                + (row.RequiresRestart ? "（需要重启）" : string.Empty)
                + (row.IsDirty ? "（未保存）" : string.Empty), ESleekFontSize.Small);
            if (!string.IsNullOrEmpty(row.Description)) AddDetailLabel(ref y, row.Description, ESleekFontSize.Small);
            switch (row.ControlKind)
            {
                case PanelSettingControlKind.Cycle:
                    var cycle = Glazier.Get().CreateButton();
                    cycle.PositionOffset_Y = y;
                    cycle.SizeOffset_X = 220f;
                    cycle.SizeOffset_Y = 32f;
                    cycle.Text = FormatPluginValue(row.Effective);
                    cycle.TooltipText = "左键：下一档；右键：上一档";
                    cycle.OnClicked += delegate(ISleekElement ignored)
                    {
                        runtime.Model.DraftCyclePluginConfig(pluginGuid, key, 1);
                        RenderDetails();
                    };
                    cycle.OnRightClicked += delegate(ISleekElement ignored)
                    {
                        runtime.Model.DraftCyclePluginConfig(pluginGuid, key, -1);
                        RenderDetails();
                    };
                    detailScroll.AddChild(cycle);
                    y += 38;
                    break;
                case PanelSettingControlKind.Toggle:
                    var toggle = Glazier.Get().CreateToggle();
                    toggle.PositionOffset_Y = y;
                    toggle.SizeOffset_X = 40f;
                    toggle.SizeOffset_Y = 30f;
                    toggle.Value = row.Effective.Boolean;
                    toggle.OnValueChanged += delegate(ISleekToggle ignored, bool value)
                    {
                        runtime.Model.DraftEditPluginConfig(pluginGuid, key, value ? "true" : "false");
                        RenderDetails();
                    };
                    detailScroll.AddChild(toggle);
                    y += 36;
                    break;
                case PanelSettingControlKind.TextEditor:
                    // POST-P4-04: UPM 列表行在文本框上方画逐字格式提示（空=不画
                    // 不占位，与描述行同纪律）；编辑仍是进草稿的普通文本框。
                    if (!string.IsNullOrEmpty(row.ControlHint)) AddDetailLabel(ref y, row.ControlHint, ESleekFontSize.Small);
                    AddTextEditor(ref y, key, FormatPluginValue(row.Effective), raw =>
                        runtime.Model.DraftEditPluginConfig(pluginGuid, key, raw), RenderDetails);
                    break;
                default:
                    AddDetailLabel(ref y, "当前值：" + FormatPluginValue(row.Effective), ESleekFontSize.Small);
                    break;
            }
        }

        // POST-P4-04: 分类 chips——固定宽按钮从左到右流式排布（每行 3 个），高亮
        // 当前分类；点击只切筛选、重绘详情区，不动草稿与会话。
        private int RenderCategoryChips(int y, IReadOnlyList<string> categories)
        {
            const float chipWidth = 150f;
            const float chipHeight = 28f;
            const float chipGapX = 8f;
            const int perRow = 3;
            for (var index = 0; index < categories.Count; index++)
            {
                var row = index / perRow;
                var column = index % perRow;
                var category = categories[index];
                var chip = Glazier.Get().CreateButton();
                chip.PositionOffset_X = column * (chipWidth + chipGapX);
                chip.PositionOffset_Y = y + row * (chipHeight + 6f);
                chip.SizeOffset_X = chipWidth;
                chip.SizeOffset_Y = chipHeight;
                chip.Text = category;
                chip.FontSize = ESleekFontSize.Small;
                chip.BackgroundColor = string.Equals(category, selectedPluginCategory, StringComparison.Ordinal)
                    ? new SleekColor(ESleekTint.BACKGROUND, 0.9f)
                    : new SleekColor(ESleekTint.BACKGROUND, 0.25f);
                chip.OnClicked += delegate(ISleekElement ignored)
                {
                    selectedPluginCategory = category;
                    RenderDetails();
                };
                detailScroll.AddChild(chip);
            }
            var rows = (categories.Count + perRow - 1) / perRow;
            return y + (int)(rows * (chipHeight + 6f)) + 8;
        }

        // ── DEV-V4-01 draft navigation (保存 / 放弃 / 确认 command surface) ──

        // Switching the selected entry while a dirty draft is open arms the
        // confirm instead of silently dropping the edits (V4-T2 Q22).
        private void SelectEntry(string stableId, ManagementEntryKind kind)
        {
            // F4：同 StableId 换种类=换页——脏草稿同样武装确认，不静默丢编辑。
            var sameRow = string.Equals(stableId, selectedStableId, StringComparison.Ordinal) && kind == selectedKind;
            if (runtime.Model.IsDirty && !sameRow)
            {
                pendingRefresh = false;
                pendingNavigation = stableId;
                pendingNavigationKind = kind;
                RenderDetails();
                return;
            }
            pendingNavigation = null;
            pendingNavigationKind = null;
            selectedStableId = stableId;
            selectedKind = kind;
            runtime.Model.OpenDetail(stableId, kind);
            RenderDetails();
        }

        private void RequestRefresh()
        {
            if (runtime.Model.IsDirty)
            {
                pendingNavigation = selectedStableId ?? string.Empty;
                pendingNavigationKind = selectedStableId == null ? (ManagementEntryKind?)null : selectedKind;
                pendingRefresh = true;
                RenderDetails();
                return;
            }
            pendingNavigation = null;
            pendingNavigationKind = null;
            pendingRefresh = false;
            if (refreshModel != null) refreshModel();
            Render();
        }

        private void RequestClose()
        {
            if (runtime.Model.IsDirty)
            {
                pendingNavigation = string.Empty;   // "" = the navigation-away is a close
                pendingNavigationKind = null;       // close has no target row
                pendingRefresh = false;
                RenderDetails();
                return;
            }
            pendingNavigation = null;
            pendingNavigationKind = null;
            pendingRefresh = false;
            Close();
        }

        private void ResolveConfirm(PanelConfirmChoice choice)
        {
            var target = pendingNavigation == string.Empty ? null : pendingNavigation;
            var targetKind = pendingNavigationKind;   // F4: the clicked row's kind travels with the navigation
            var wasRefresh = pendingRefresh;
            DraftSaveReport report;
            var leaving = runtime.Model.TryLeaveDetail(target, targetKind, choice, out report);
            pendingNavigation = null;
            pendingNavigationKind = null;
            pendingRefresh = false;
            // The confirm's 保存 reuses 保存配置's report verbatim (Q22 = 等同保存
            // 配置); 取消/不保存 carry no report (no write attempted).
            if (!leaving)
            {
                // 取消, or 保存 where a source failed — stay on the current entry,
                // the failed draft is preserved by the model.
                RefreshCatalogThenPaintCurrentDetail(report, false, AfterCommitPaint.Details);
                return;
            }
            if (target == null)
            {
                RefreshCatalogThenPaintCurrentDetail(report, wasRefresh, AfterCommitPaint.None);
                Close();
                return;
            }
            selectedStableId = target;
            selectedKind = targetKind ?? selectedKind;
            RefreshCatalogThenPaintCurrentDetail(report, wasRefresh, AfterCommitPaint.Full);
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
            RefreshCatalogThenPaintCurrentDetail(report, false, AfterCommitPaint.Full);
        }

        // POST-P4-07 / DEV-V4-09 F2: the one "rebuild catalog then paint current
        // detail" entry. Save-stay, confirm-stay, and confirm-leave (dirty refresh
        // included) call this. Lifecycle intent committed = machine facts changed,
        // so rebuild entries from the composition root before paint; settings-only
        // and rejected intents do not refresh. forceRefresh covers dirty-refresh
        // leave (wasRefresh). Banner last so a platform notice cannot wipe
        // 「配置已保存。」 (Q30/Q23). AfterCommitPaint.None is close-after-leave:
        // catalog still rebuilds, the panel is torn down without a paint frame.
        private void RefreshCatalogThenPaintCurrentDetail(DraftSaveReport report, bool forceRefresh, AfterCommitPaint paint)
        {
            if ((forceRefresh || (report != null && report.CommittedLifecycleIntent)) && refreshModel != null)
                refreshModel();
            if (paint == AfterCommitPaint.None) return;
            if (paint == AfterCommitPaint.Details) RenderDetails();
            else Render();
            RenderDraftReport(report);
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

        // DEV-V4-09 F3（实机缺陷）：文本框此前只在 Enter 提交——玩家输入后直接
        // 点「保存配置」，草稿从未收到编辑（NoChanges「没有需要保存的修改。」），
        // 表现为「无法修改」。现改为与 Toggle/Cycle 同一口径：逐键经 OnTextChanged
        // 静默写入草稿（不重渲染、保持输入焦点），Enter=提交+重渲染、Esc=回到
        // 本帧渲染值（同时回拨该键程的草稿）。submit=校验+写草稿（不渲染）；
        // afterSubmit=提交受理后的重渲染。
        private void AddTextEditor(ref int y, string key, string value, Func<string, bool> submit, System.Action afterSubmit)
        {
            var field = Glazier.Get().CreateStringField();
            field.PositionOffset_Y = y;
            field.SizeOffset_X = 360f;
            field.SizeOffset_Y = 30f;
            field.SizeScale_X = 0f;
            field.Text = value ?? string.Empty;
            field.MaxLength = 4096;
            field.OnTextChanged += delegate(ISleekField changed, string text)
            {
                submit(text == null ? string.Empty : text.Trim());
            };
            field.OnTextSubmitted += delegate(ISleekField submitted)
            {
                if (submit(submitted.Text == null ? string.Empty : submitted.Text.Trim()))
                {
                    if (afterSubmit != null) afterSubmit();
                }
                else submitted.Text = value ?? string.Empty;
            };
            field.OnTextEscaped += delegate(ISleekField escaped)
            {
                escaped.Text = value ?? string.Empty;
                submit(value ?? string.Empty);   // 回拨键程草稿（回到本帧渲染值）
            };
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
                case PluginConfigValueKind.Integer: return value.IsUnsigned ? value.Unsigned64.ToString() : value.Integer64.ToString();
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
