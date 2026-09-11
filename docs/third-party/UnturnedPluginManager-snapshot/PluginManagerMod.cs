// PluginManagerMod.cs
// 作者：35117+Deepseek-v4-falsh-0731
// 插件功能、使用方法与编译方式详见仓库 README.md。
// 版本 v26.8.11.3

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace PluginManagerMod
{
    [BepInPlugin("com.trae.pluginmanager", "PluginManager 插件管理", "26.8.11.3")]
    public class PluginManagerPlugin : BaseUnityPlugin
    {
        internal static PluginManagerPlugin instance;

        // BaseUnityPlugin.Logger 为 protected 字段，转存为静态引用供其他类使用
        internal static ManualLogSource log;

        private void Awake()
        {
            instance = this;
            log = Logger;

            PluginConfig.Bind(Config);

            try
            {
                Harmony harmony = new Harmony("com.trae.pluginmanager");
                harmony.PatchAll();
                log.LogInfo("[PluginManager] Harmony 补丁已应用");
            }
            catch (Exception exception)
            {
                // 补丁失败时仍有 Update 轮询兜底（按钮注入 + ESC 关闭）
                log.LogError("[PluginManager] Harmony 补丁应用失败（将使用轮询兜底）: " + exception.Message);
            }
        }

        private void Update()
        {
            PluginManagerUI.Tick();
        }
    }

    // 插件自身配置（同样可在游戏内通过插件管理器修改）
    public static class PluginConfig
    {
        public static bool Enabled = true;
        public static string ButtonText = "插件管理";

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "是否在创意工坊界面显示“插件管理”按钮。").Value;
            ButtonText = config.Bind("General", "ButtonText", "插件管理", "创意工坊界面中“插件管理”按钮的显示文字。").Value;
        }
    }

    // 插件管理窗口（全屏覆盖层，叠加在创意工坊界面之上）
    public static class PluginManagerUI
    {
        public static bool IsOpen { get; private set; }

        // 打开来源：主菜单创意工坊 / 游戏内暂停菜单
        private enum EOpenContext
        {
            None,
            Menu,
            InGame,
        }

        private static SleekFullscreenBox container;
        private static ISleekBox backgroundBox;
        private static ISleekLabel titleLabel;
        private static ISleekButton closeButton;
        private static ISleekButton refreshButton;
        private static ISleekScrollView listScroll;
        private static ISleekScrollView detailScroll;
        private static string selectedGuid;
        private static ConfigFile selectedConfigFile;
        private static ISleekLabel statusLabel;
        private static float statusUntilTime;
        private static EOpenContext openContext;

        // 创意工坊入口按钮及其挂载容器（主菜单重建后自动重新注入）
        private static ISleekButton entryButton;
        private static ISleekElement entryButtonParent;
        // 游戏内暂停菜单入口按钮及其挂载容器
        private static ISleekButton pauseButton;
        private static ISleekElement pauseButtonParent;
        // 反射缓存的 MenuWorkshopUI.container / PlayerPauseUI.container 字段
        private static FieldInfo workshopContainerField;
        private static FieldInfo pauseContainerField;
        // 当前插件管理窗口挂载的 UI 根（用于检测主菜单/游戏内界面重建）
        private static SleekFullscreenBox attachedMenuContainer;
        private static ISleekElement attachedPlayerContainer;
        // 打开插件管理时被移出屏幕的源界面容器（关闭时恢复）
        private static SleekFullscreenBox hiddenWorkshopContainer;
        private static SleekFullscreenBox hiddenPauseContainer;
        // 列表型配置项选择器（物品列表 / 配方列表）
        private static ISleekBox pickerBox;
        private static ISleekField pickerSearchField;
        private static ISleekScrollView pickerList;
        private static ISleekButton pickerCloseButton;
        private static ConfigEntryBase pickerEntry;
        private static EPickerKind pickerKind;
        private static List<PickerItem> pickerData;
        // 物品 ID -> 已生成图标纹理（异步 getIcon 结果缓存，避免重复生成）
        private static Dictionary<ushort, Texture2D> iconCache = new Dictionary<ushort, Texture2D>();

        // 从主菜单创意工坊打开
        public static void OpenFromWorkshop()
        {
            CheckUIRebuild();
            if (!EnsureCreated())
            {
                return;
            }
            if (!AttachToParent(MenuUI.container))
            {
                return;
            }
            openContext = EOpenContext.Menu;
            OpenCore();
        }

        // 从游戏内暂停菜单打开
        public static void OpenFromPauseMenu()
        {
            CheckUIRebuild();
            if (!EnsureCreated())
            {
                return;
            }
            if (!AttachToParent(PlayerUI.container))
            {
                return;
            }
            openContext = EOpenContext.InGame;
            OpenCore();
        }

        private static void OpenCore()
        {
            if (IsOpen)
            {
                Refresh();
                return;
            }
            IsOpen = true;
            container.AnimateIntoView();
            Refresh();
            HideOriginUI();
        }

        // 挂载/重挂载到指定 UI 根（AddChild 支持更换父节点）
        private static bool AttachToParent(ISleekElement parent)
        {
            if (parent == null)
            {
                return false;
            }
            try
            {
                if (!IsElementAlive(parent))
                {
                    LogError("挂载插件管理窗口失败: 目标 UI 容器已失效");
                    return false;
                }
                parent.AddChild(container);
                return true;
            }
            catch (Exception exception)
            {
                LogError("挂载插件管理窗口失败: " + exception.Message);
                return false;
            }
        }

        // 检查任意 Sleek 元素底层的 uGUI gameObject 是否仍然有效
        // （游戏 UI 重建后旧引用仍存在，但底层 gameObject 已被销毁置空）
        private static bool IsElementAlive(ISleekElement element)
        {
            if (element == null)
            {
                return false;
            }
            try
            {
                if (element is SleekWrapper)
                {
                    ISleekProxyImplementation implementation = ((SleekWrapper)element).GetProxyImplementation();
                    if (implementation == null)
                    {
                        return false;
                    }
                    object gameObjectValue = GetImplementationGameObject(implementation);
                    return gameObjectValue != null;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 反射获取 uGUI 实现的 gameObject 字段值（遍历类型链）
        private static object GetImplementationGameObject(ISleekProxyImplementation implementation)
        {
            Type implType = implementation.GetType();
            while (implType != null && implType != typeof(object))
            {
                PropertyInfo prop = implType.GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                {
                    return prop.GetValue(implementation, null);
                }
                FieldInfo field = implType.GetField("<gameObject>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.GetValue(implementation);
                }
                implType = implType.BaseType;
            }
            return null;
        }

        public static void CloseIfOpen()
        {
            if (IsOpen)
            {
                CloseSilently();
            }
        }

        // 静默关闭（不恢复源界面）：用于 closeAll 补丁（开始游戏/退出主菜单）及界面销毁兜底
        public static void CloseSilently()
        {
            if (!IsOpen)
            {
                return;
            }
            IsOpen = false;
            if (container != null && IsContainerAlive())
            {
                container.AnimateOutOfView(0f, -1f);
            }
            openContext = EOpenContext.None;
            if (pickerBox != null)
            {
                pickerBox.IsVisible = false;
            }
            if (detailScroll != null)
            {
                detailScroll.IsVisible = true;
            }
            pickerEntry = null;
        }

        // ESC 关闭：关闭后恢复打开时的源界面（创意工坊或暂停菜单）
        public static void CloseAndReturn()
        {
            if (!IsOpen)
            {
                return;
            }
            EOpenContext context = openContext;
            CloseSilently();
            ShowOriginUI(context);
        }

        // 每帧调用：界面重建检测、入口按钮维持、ESC 兜底、状态提示自动消失
        public static void Tick()
        {
            if (PluginManagerPlugin.log == null)
            {
                return;
            }
            CheckUIRebuild();
            TryAddEntryButton();
            TryAddPauseButton();

            if (IsOpen)
            {
                if (openContext == EOpenContext.Menu && Input.GetKeyDown(KeyCode.Escape))
                {
                    // 主菜单 ESC 兜底（escapeMenu 前缀补丁失效时）
                    CloseAndReturn();
                }
                else if (openContext == EOpenContext.InGame && PlayerUI.window == null)
                {
                    // 玩家 UI 已销毁（退出地图等），强制关闭
                    CloseSilently();
                }
                else if (openContext == EOpenContext.InGame && !PlayerPauseUI.active)
                {
                    // 游戏关闭了暂停菜单（ESC/死亡等），不再恢复，直接关闭插件管理
                    CloseSilently();
                }
                else if (openContext == EOpenContext.Menu && !MenuWorkshopUI.active)
                {
                    CloseSilently();
                }
            }

            if (statusLabel != null && statusUntilTime > 0f && Time.realtimeSinceStartup >= statusUntilTime)
            {
                statusLabel.Text = string.Empty;
                statusUntilTime = 0f;
            }
        }

        // 检测主菜单 / 游戏内界面重建（进游戏再返回、重新进入地图等），丢弃失效的 UI 引用
        private static void CheckUIRebuild()
        {
            if (MenuUI.container != null)
            {
                if (attachedMenuContainer == null)
                {
                    attachedMenuContainer = MenuUI.container;
                }
                else if (!ReferenceEquals(attachedMenuContainer, MenuUI.container))
                {
                    attachedMenuContainer = MenuUI.container;
                    ResetAll();
                    LogInfo("检测到主菜单重建，已重置界面状态");
                }
            }
            if (PlayerUI.container != null)
            {
                if (attachedPlayerContainer == null)
                {
                    attachedPlayerContainer = PlayerUI.container;
                }
                else if (!ReferenceEquals(attachedPlayerContainer, PlayerUI.container))
                {
                    attachedPlayerContainer = PlayerUI.container;
                    ResetAll();
                    LogInfo("检测到游戏内界面重建，已重置界面状态");
                }
            }
        }

        private static void ResetAll()
        {
            IsOpen = false;
            container = null;
            backgroundBox = null;
            titleLabel = null;
            closeButton = null;
            refreshButton = null;
            listScroll = null;
            detailScroll = null;
            selectedGuid = null;
            selectedConfigFile = null;
            statusLabel = null;
            statusUntilTime = 0f;
            openContext = EOpenContext.None;
            hiddenWorkshopContainer = null;
            hiddenPauseContainer = null;
            pickerBox = null;
            pickerSearchField = null;
            pickerList = null;
            pickerCloseButton = null;
            pickerEntry = null;
            pickerKind = EPickerKind.None;
            pickerData = null;
            // 注意：entryButton/entryButtonParent、pauseButton/pauseButtonParent 保留，
            // 由对应的 TryAdd 方法根据容器比对自动重新注入
        }

        // 检查插件管理容器底层的 uGUI gameObject 是否仍然有效。
        // 游戏 UI 重建（进入地图/暂停菜单切换/返回主菜单等）会销毁旧 UI 树，
        // 但插件的静态引用仍然指向旧对象，此时其底层 gameObject 已被置空。
        private static bool IsContainerAlive()
        {
            if (container == null)
            {
                return false;
            }
            try
            {
                // SleekWrapper.GetProxyImplementation() 返回底层 uGUI 实现（GlazierProxy_uGUI）
                ISleekProxyImplementation implementation = container.GetProxyImplementation();
                if (implementation == null)
                {
                    return false;
                }
                object gameObjectValue = GetImplementationGameObject(implementation);
                return gameObjectValue != null;
            }
            catch
            {
                return false;
            }
        }

        // 确保创意工坊界面中存在“插件管理”按钮（容器重建后自动重新添加）
        internal static void TryAddEntryButton()
        {
            if (!PluginConfig.Enabled)
            {
                return;
            }
            try
            {
                FieldInfo field = GetWorkshopContainerField();
                if (field == null)
                {
                    return;
                }
                ISleekElement container = field.GetValue(null) as ISleekElement;
                if (container == null)
                {
                    return;
                }
                if (entryButton != null && ReferenceEquals(entryButtonParent, container))
                {
                    return; // 按钮仍然挂在同一个容器上
                }

                ISleekButton button = Glazier.Get().CreateButton();
                button.PositionOffset_X = -100f;
                button.PositionOffset_Y = 185f;
                button.PositionScale_X = 0.5f;
                button.PositionScale_Y = 0.5f;
                button.SizeOffset_X = 200f;
                button.SizeOffset_Y = 50f;
                button.Text = PluginConfig.ButtonText;
                button.TooltipText = "查看已加载的 BepInEx 插件，并可在游戏内修改其配置";
                button.FontSize = ESleekFontSize.Medium;
                button.OnClicked += OnClickedEntryButton;
                container.AddChild(button);

                entryButton = button;
                entryButtonParent = container;
                LogInfo("已在创意工坊界面添加“" + PluginConfig.ButtonText + "”按钮");
            }
            catch (Exception exception)
            {
                LogError("添加按钮失败: " + exception.Message);
            }
        }

        private static void OnClickedEntryButton(ISleekElement button)
        {
            OpenFromWorkshop();
        }

        // 确保游戏内暂停菜单中存在“插件管理”按钮（玩家界面重建后自动重新添加）
        internal static void TryAddPauseButton()
        {
            if (!PluginConfig.Enabled)
            {
                return;
            }
            try
            {
                FieldInfo field = GetPauseContainerField();
                if (field == null)
                {
                    return;
                }
                ISleekElement container = field.GetValue(null) as ISleekElement;
                if (container == null)
                {
                    return;
                }
                if (pauseButton != null && ReferenceEquals(pauseButtonParent, container))
                {
                    return; // 按钮仍然挂在同一个容器上
                }

                ISleekButton button = Glazier.Get().CreateButton();
                button.PositionOffset_X = 205f;
                button.PositionOffset_Y = -290f;
                button.PositionScale_X = 0.5f;
                button.PositionScale_Y = 0.5f;
                button.SizeOffset_X = 200f;
                button.SizeOffset_Y = 50f;
                button.Text = PluginConfig.ButtonText;
                button.TooltipText = "查看已加载的 BepInEx 插件，并可在游戏内修改其配置";
                button.FontSize = ESleekFontSize.Medium;
                button.OnClicked += OnClickedPauseButton;
                container.AddChild(button);

                pauseButton = button;
                pauseButtonParent = container;
                LogInfo("已在游戏内暂停菜单添加“" + PluginConfig.ButtonText + "”按钮");
            }
            catch (Exception exception)
            {
                LogError("添加暂停菜单按钮失败: " + exception.Message);
            }
        }

        private static void OnClickedPauseButton(ISleekElement button)
        {
            OpenFromPauseMenu();
        }

        private static FieldInfo GetWorkshopContainerField()
        {
            if (workshopContainerField == null)
            {
                workshopContainerField = typeof(MenuWorkshopUI).GetField("container", BindingFlags.NonPublic | BindingFlags.Static);
            }
            return workshopContainerField;
        }

        private static SleekFullscreenBox GetWorkshopContainer()
        {
            FieldInfo field = GetWorkshopContainerField();
            if (field == null)
            {
                return null;
            }
            return field.GetValue(null) as SleekFullscreenBox;
        }

        private static FieldInfo GetPauseContainerField()
        {
            if (pauseContainerField == null)
            {
                pauseContainerField = typeof(PlayerPauseUI).GetField("container", BindingFlags.NonPublic | BindingFlags.Static);
            }
            return pauseContainerField;
        }

        private static SleekFullscreenBox GetPauseContainer()
        {
            FieldInfo field = GetPauseContainerField();
            if (field == null)
            {
                return null;
            }
            return field.GetValue(null) as SleekFullscreenBox;
        }

        // 打开插件管理时把源界面（创意工坊/暂停菜单）移出屏幕，关闭时恢复
        private static void HideOriginUI()
        {
            if (openContext == EOpenContext.InGame)
            {
                SleekFullscreenBox box = GetPauseContainer();
                if (box != null)
                {
                    hiddenPauseContainer = box;
                    box.AnimateOutOfView(0f, 1f);
                }
            }
            else if (openContext == EOpenContext.Menu)
            {
                SleekFullscreenBox box = GetWorkshopContainer();
                if (box != null)
                {
                    hiddenWorkshopContainer = box;
                    box.AnimateOutOfView(0f, 1f);
                }
            }
        }

        private static void ShowOriginUI(EOpenContext context)
        {
            if (context == EOpenContext.InGame)
            {
                if (hiddenPauseContainer != null)
                {
                    hiddenPauseContainer.AnimateIntoView();
                }
                hiddenPauseContainer = null;
            }
            else if (context == EOpenContext.Menu)
            {
                if (hiddenWorkshopContainer != null)
                {
                    hiddenWorkshopContainer.AnimateIntoView();
                }
                hiddenWorkshopContainer = null;
                MenuWorkshopUI.open();
            }
        }

        // 界面底部状态提示（保存成功/值无效等），几秒后自动消失
        private static void ShowStatus(string text)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = text;
                statusUntilTime = Time.realtimeSinceStartup + 4f;
            }
        }

        private static void LogInfo(string message)
        {
            if (PluginManagerPlugin.log != null)
            {
                PluginManagerPlugin.log.LogInfo("[PluginManager] " + message);
            }
        }

        private static bool EnsureCreated()
        {
            if (container != null)
            {
                // 游戏 UI 重建（进入地图/暂停菜单切换等）可能销毁了容器底层的 uGUI 对象，
                // 此时引用仍在但 gameObject 已失效，必须重建，否则后续操作会抛空引用。
                if (IsContainerAlive())
                {
                    return true;
                }
                LogInfo("检测到插件管理容器已被游戏销毁，正在重建界面");
                ResetAll();
            }

            // 容器不在此处挂载，由 OpenFromWorkshop/OpenFromPauseMenu 挂到对应的 UI 根
            container = new SleekFullscreenBox();
            container.PositionOffset_X = 10f;
            container.PositionOffset_Y = 10f;
            container.PositionScale_Y = -1f;
            container.SizeOffset_X = -20f;
            container.SizeOffset_Y = -20f;
            container.SizeScale_X = 1f;
            container.SizeScale_Y = 1f;

            // 半透明背景：插件管理打开时创意工坊界面已被移出屏幕，背景仅用于提升文字可读性
            backgroundBox = Glazier.Get().CreateBox();
            backgroundBox.SizeScale_X = 1f;
            backgroundBox.SizeScale_Y = 1f;
            backgroundBox.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.85f);
            container.AddChild(backgroundBox);

            titleLabel = Glazier.Get().CreateLabel();
            titleLabel.PositionOffset_X = 20f;
            titleLabel.PositionOffset_Y = 20f;
            titleLabel.SizeOffset_X = 300f;
            titleLabel.SizeOffset_Y = 36f;
            titleLabel.Text = "插件管理";
            titleLabel.FontSize = ESleekFontSize.Large;
            titleLabel.TextAlignment = TextAnchor.MiddleLeft;
            titleLabel.TextColor = new SleekColor(ESleekTint.FONT);
            container.AddChild(titleLabel);

            refreshButton = Glazier.Get().CreateButton();
            refreshButton.PositionOffset_X = 170f;
            refreshButton.PositionOffset_Y = 20f;
            refreshButton.SizeOffset_X = 100f;
            refreshButton.SizeOffset_Y = 36f;
            refreshButton.Text = "刷新";
            refreshButton.TooltipText = "重新扫描已加载的插件";
            refreshButton.FontSize = ESleekFontSize.Medium;
            refreshButton.OnClicked += OnClickedRefreshButton;
            container.AddChild(refreshButton);

            closeButton = Glazier.Get().CreateButton();
            closeButton.PositionOffset_X = -120f;
            closeButton.PositionOffset_Y = 20f;
            closeButton.PositionScale_X = 1f;
            closeButton.SizeOffset_X = 100f;
            closeButton.SizeOffset_Y = 36f;
            closeButton.Text = "关闭";
            closeButton.TooltipText = "关闭插件管理，返回原界面";
            closeButton.FontSize = ESleekFontSize.Medium;
            closeButton.OnClicked += OnClickedCloseButton;
            container.AddChild(closeButton);

            listScroll = Glazier.Get().CreateScrollView();
            listScroll.PositionOffset_X = 20f;
            listScroll.PositionOffset_Y = 70f;
            listScroll.SizeOffset_X = 280f;
            listScroll.SizeOffset_Y = -90f;
            listScroll.SizeScale_Y = 1f;
            listScroll.ScaleContentToWidth = true;
            listScroll.HandleScrollWheel = true;
            listScroll.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.5f);
            container.AddChild(listScroll);

            detailScroll = Glazier.Get().CreateScrollView();
            detailScroll.PositionOffset_X = 320f;
            detailScroll.PositionOffset_Y = 70f;
            detailScroll.SizeOffset_X = -340f;
            detailScroll.SizeOffset_Y = -90f;
            detailScroll.SizeScale_X = 1f;
            detailScroll.SizeScale_Y = 1f;
            detailScroll.ScaleContentToWidth = true;
            detailScroll.HandleScrollWheel = true;
            detailScroll.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.5f);
            container.AddChild(detailScroll);

            statusLabel = Glazier.Get().CreateLabel();
            statusLabel.PositionOffset_X = 20f;
            statusLabel.PositionOffset_Y = -40f;
            statusLabel.PositionScale_Y = 1f;
            statusLabel.SizeOffset_X = 600f;
            statusLabel.SizeOffset_Y = 28f;
            statusLabel.FontSize = ESleekFontSize.Small;
            statusLabel.TextAlignment = TextAnchor.MiddleLeft;
            statusLabel.TextColor = new SleekColor(ESleekTint.FONT);
            container.AddChild(statusLabel);

            // 物品选择器面板（覆盖在详情区上，默认隐藏）
            pickerBox = Glazier.Get().CreateBox();
            pickerBox.PositionOffset_X = 320f;
            pickerBox.PositionOffset_Y = 70f;
            pickerBox.SizeOffset_X = -340f;
            pickerBox.SizeOffset_Y = -90f;
            pickerBox.SizeScale_X = 1f;
            pickerBox.SizeScale_Y = 1f;
            pickerBox.BackgroundColor = new SleekColor(new Color(0.05f, 0.06f, 0.08f, 1f));
            pickerBox.IsVisible = false;
            container.AddChild(pickerBox);

            ISleekLabel pickerTitle = Glazier.Get().CreateLabel();
            pickerTitle.PositionOffset_X = 10f;
            pickerTitle.PositionOffset_Y = 10f;
            pickerTitle.SizeOffset_X = 700f;
            pickerTitle.SizeOffset_Y = 28f;
            pickerTitle.Text = "选择并添加 - 点击行加入列表（可搜索名称 / ID / 描述）";
            pickerTitle.FontSize = ESleekFontSize.Medium;
            pickerTitle.TextAlignment = TextAnchor.MiddleLeft;
            pickerTitle.TextColor = new SleekColor(ESleekTint.FONT);
            pickerBox.AddChild(pickerTitle);

            pickerSearchField = Glazier.Get().CreateStringField();
            pickerSearchField.PositionOffset_X = 10f;
            pickerSearchField.PositionOffset_Y = 40f;
            pickerSearchField.SizeOffset_X = 500f;
            pickerSearchField.SizeOffset_Y = 30f;
            pickerSearchField.PlaceholderText = "搜索物品名称 / ID / 描述…";
            pickerSearchField.OnTextChanged += OnPickerSearchChanged;
            pickerSearchField.OnTextEscaped += delegate(ISleekField f)
            {
                f.Text = string.Empty;
                RefreshPickerList(string.Empty);
            };
            pickerBox.AddChild(pickerSearchField);

            pickerCloseButton = Glazier.Get().CreateButton();
            pickerCloseButton.PositionOffset_X = -100f;
            pickerCloseButton.PositionOffset_Y = 40f;
            pickerCloseButton.PositionScale_X = 1f;
            pickerCloseButton.SizeOffset_X = 90f;
            pickerCloseButton.SizeOffset_Y = 30f;
            pickerCloseButton.Text = "关闭";
            pickerCloseButton.TooltipText = "关闭物品选择器";
            pickerCloseButton.FontSize = ESleekFontSize.Medium;
            pickerCloseButton.OnClicked += delegate(ISleekElement b)
            {
                CloseItemPicker();
            };
            pickerBox.AddChild(pickerCloseButton);

            pickerList = Glazier.Get().CreateScrollView();
            pickerList.PositionOffset_X = 10f;
            pickerList.PositionOffset_Y = 80f;
            pickerList.SizeOffset_X = -20f;
            pickerList.SizeOffset_Y = -90f;
            pickerList.SizeScale_X = 1f;
            pickerList.SizeScale_Y = 1f;
            pickerList.ScaleContentToWidth = true;
            pickerList.HandleScrollWheel = true;
            pickerList.BackgroundColor = new SleekColor(new Color(0.05f, 0.06f, 0.08f, 1f));
            pickerBox.AddChild(pickerList);

            return true;
        }

        private static void OnClickedRefreshButton(ISleekElement button)
        {
            Refresh();
        }

        private static void OnClickedCloseButton(ISleekElement button)
        {
            CloseAndReturn();
        }

        private static void Refresh()
        {
            RefreshList();

            if (selectedGuid != null && Chainloader.PluginInfos.ContainsKey(selectedGuid))
            {
                ShowPlugin(selectedGuid);
            }
            else
            {
                selectedGuid = null;
                ShowEmptyDetail("在左侧选择一个插件查看详情");
            }
        }

        private static void RefreshList()
        {
            ClearScroll(listScroll);

            List<KeyValuePair<string, PluginInfo>> plugins = new List<KeyValuePair<string, PluginInfo>>(Chainloader.PluginInfos);
            plugins.Sort(delegate(KeyValuePair<string, PluginInfo> a, KeyValuePair<string, PluginInfo> b)
            {
                string an = a.Value.Metadata != null ? a.Value.Metadata.Name : a.Key;
                string bn = b.Value.Metadata != null ? b.Value.Metadata.Name : b.Key;
                return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
            });

            int y = 0;
            foreach (KeyValuePair<string, PluginInfo> pair in plugins)
            {
                string guid = pair.Key;
                PluginInfo info = pair.Value;

                ISleekButton button = Glazier.Get().CreateButton();
                button.PositionOffset_X = 0f;
                button.PositionOffset_Y = y;
                button.SizeOffset_X = 260f;
                button.SizeOffset_Y = 44f;
                string name = info.Metadata != null ? info.Metadata.Name : guid;
                string version = info.Metadata != null ? info.Metadata.Version.ToString() : "?";
                button.Text = Truncate(name, 24);
                button.TooltipText = guid + "\n版本: " + version;
                button.FontSize = ESleekFontSize.Medium;
                button.OnClicked += delegate(ISleekElement b)
                {
                    ShowPlugin(guid);
                };
                listScroll.AddChild(button);
                y += 48;
            }

            listScroll.ContentSizeOffset = new Vector2(0f, y);
        }

        private static void ShowPlugin(string guid)
        {
            selectedGuid = guid;

            PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue(guid, out info))
            {
                ShowEmptyDetail("插件不存在: " + guid);
                return;
            }

            ClearScroll(detailScroll);
            selectedConfigFile = info.Instance != null ? info.Instance.Config : null;

            int y = 0;
            string name = info.Metadata != null ? info.Metadata.Name : guid;
            string version = info.Metadata != null ? info.Metadata.Version.ToString() : "?";
            string location = info.Location != null ? info.Location : "?";

            AddDetailLabel(ref y, "名称: " + name, ESleekFontSize.Default);
            AddDetailLabel(ref y, "GUID: " + guid, ESleekFontSize.Small);
            AddDetailLabel(ref y, "版本: " + version, ESleekFontSize.Small);
            AddDetailLabel(ref y, "程序集: " + location, ESleekFontSize.Small);
            AddDetailLabel(ref y, "配置文件: " + (selectedConfigFile != null ? selectedConfigFile.ConfigFilePath : "(插件实例未加载，无法读取配置)"), ESleekFontSize.Small);
            y += 10;

            if (selectedConfigFile == null)
            {
                AddDetailLabel(ref y, "该插件没有已加载的实例，无法读取配置。", ESleekFontSize.Small);
            }
            else
            {
                try
                {
#pragma warning disable 618 // GetConfigEntries() is obsolete but returns ConfigEntryBase[] which we need
                    List<ConfigEntryBase> entries = new List<ConfigEntryBase>(selectedConfigFile.GetConfigEntries());
#pragma warning restore 618
                    AddDetailLabel(ref y, "配置项（共 " + entries.Count + " 个）：输入框回车保存修改，按 ESC 还原当前项", ESleekFontSize.Small);
                    y += 4;
                    foreach (ConfigEntryBase entry in entries)
                    {
                        AddConfigRow(ref y, entry);
                    }
                }
                catch (Exception exception)
                {
                    LogError("读取插件配置失败: " + exception.Message);
                    AddDetailLabel(ref y, "读取配置失败: " + exception.Message, ESleekFontSize.Small);
                }
            }

            detailScroll.ContentSizeOffset = new Vector2(0f, y);
            detailScroll.ScrollToTop();
        }

        private static void ShowEmptyDetail(string text)
        {
            ClearScroll(detailScroll);
            selectedConfigFile = null;
            int y = 10;
            AddDetailLabel(ref y, text, ESleekFontSize.Default);
            detailScroll.ContentSizeOffset = new Vector2(0f, y);
            detailScroll.ScrollToTop();
        }

        private static void AddDetailLabel(ref int y, string text, ESleekFontSize fontSize)
        {
            ISleekLabel label = Glazier.Get().CreateLabel();
            label.PositionOffset_X = 10f;
            label.PositionOffset_Y = y;
            label.SizeOffset_X = 1400f;
            label.SizeOffset_Y = 28f;
            label.Text = text;
            label.FontSize = fontSize;
            label.TextAlignment = TextAnchor.MiddleLeft;
            label.TextColor = new SleekColor(ESleekTint.FONT);
            detailScroll.AddChild(label);
            y += 30;
        }

        // 一行配置：左侧 节 > 键 + 说明文字，右侧编辑控件（bool 用开关，其余用输入框），最右显示类型
        private static void AddConfigRow(ref int y, ConfigEntryBase entry)
        {
            try
            {
                if (HasItemListTag(entry))
                {
                    AddListRow(ref y, entry, EPickerKind.Items);
                    return;
                }
                if (HasBlueprintListTag(entry))
                {
                    AddListRow(ref y, entry, EPickerKind.Blueprints);
                    return;
                }
                if (HasCycleTag(entry))
                {
                    AddCycleRow(ref y, entry);
                    return;
                }

                string keyText = entry.Definition.Section + " > " + entry.Definition.Key;

                string description = string.Empty;
                AcceptableValueBase acceptableValues = null;
                if (entry.Description != null)
                {
                    description = entry.Description.Description;
                    acceptableValues = entry.Description.AcceptableValues;
                }
                if (acceptableValues != null)
                {
                    string acceptableText = acceptableValues.ToDescriptionString();
                    if (!string.IsNullOrEmpty(acceptableText))
                    {
                        if (description.Length > 0)
                        {
                            description += " ";
                        }
                        description += acceptableText;
                    }
                }
                description = Truncate(description, 120);

                string typeName = entry.SettingType != null ? entry.SettingType.Name : "?";

                ISleekLabel keyLabel = Glazier.Get().CreateLabel();
                keyLabel.PositionOffset_X = 10f;
                keyLabel.PositionOffset_Y = y;
                keyLabel.SizeOffset_X = 290f;
                keyLabel.SizeOffset_Y = 28f;
                keyLabel.Text = keyText;
                keyLabel.FontSize = ESleekFontSize.Small;
                keyLabel.TextAlignment = TextAnchor.MiddleLeft;
                keyLabel.TextColor = new SleekColor(ESleekTint.FONT);
                detailScroll.AddChild(keyLabel);

                if (description.Length > 0)
                {
                    ISleekLabel descriptionLabel = Glazier.Get().CreateLabel();
                    descriptionLabel.PositionOffset_X = 10f;
                    descriptionLabel.PositionOffset_Y = y + 26;
                    descriptionLabel.SizeOffset_X = 1400f;
                    descriptionLabel.SizeOffset_Y = 24f;
                    descriptionLabel.Text = description;
                    descriptionLabel.FontSize = ESleekFontSize.Small;
                    descriptionLabel.TextAlignment = TextAnchor.MiddleLeft;
                    descriptionLabel.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
                    detailScroll.AddChild(descriptionLabel);
                }

                if (entry.SettingType == typeof(bool))
                {
                    ISleekToggle toggle = Glazier.Get().CreateToggle();
                    toggle.PositionOffset_X = 310f;
                    toggle.PositionOffset_Y = y;
                    toggle.SizeOffset_X = 40f;
                    toggle.SizeOffset_Y = 30f;
                    object boxed = entry.BoxedValue;
                    toggle.Value = (boxed is bool) && ((bool)boxed);
                    toggle.OnValueChanged += delegate(ISleekToggle t, bool state)
                    {
                        try
                        {
                            entry.BoxedValue = state;
                            SaveSelectedConfig();
                        }
                        catch (Exception exception)
                        {
                            LogError("保存布尔配置失败: " + exception.Message);
                        }
                    };
                    detailScroll.AddChild(toggle);
                }
                else
                {
                    ISleekField field = Glazier.Get().CreateStringField();
                    field.PositionOffset_X = 310f;
                    field.PositionOffset_Y = y;
                    field.SizeOffset_X = 300f;
                    field.SizeOffset_Y = 30f;
                    field.Text = entry.GetSerializedValue();
                    field.OnTextSubmitted += delegate(ISleekField f)
                    {
                        try
                        {
                            // SetSerializedValue 返回 void：设置后通过序列化值对比判断是否生效
                            string submitted = f.Text.Trim();
                            string oldValue = entry.GetSerializedValue();
                            entry.SetSerializedValue(submitted);
                            string newValue = entry.GetSerializedValue();
                            if (string.Equals(submitted, newValue, StringComparison.Ordinal))
                            {
                                if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                                {
                                    SaveSelectedConfig();
                                }
                            }
                            else
                            {
                                // 提交的值与当前值不同，但设置后值没有变化 => 值无效被拒绝/被修正
                                f.Text = newValue;
                                ShowStatus("配置值无效，已显示当前值。");
                            }
                        }
                        catch (Exception exception)
                        {
                            f.Text = entry.GetSerializedValue();
                            ShowStatus("设置配置失败: " + exception.Message);
                            LogError("设置配置失败: " + exception.Message);
                        }
                    };
                    field.OnTextEscaped += delegate(ISleekField f)
                    {
                        f.Text = entry.GetSerializedValue();
                    };
                    detailScroll.AddChild(field);
                }

                ISleekLabel typeLabel = Glazier.Get().CreateLabel();
                typeLabel.PositionOffset_X = 620f;
                typeLabel.PositionOffset_Y = y;
                typeLabel.SizeOffset_X = 150f;
                typeLabel.SizeOffset_Y = 30f;
                typeLabel.Text = typeName;
                typeLabel.FontSize = ESleekFontSize.Small;
                typeLabel.TextAlignment = TextAnchor.MiddleRight;
                typeLabel.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
                detailScroll.AddChild(typeLabel);

                y += (description.Length > 0) ? 58 : 40;
            }
            catch (Exception exception)
            {
                LogError("构建配置行失败: " + exception.Message);
            }
        }

        private static void SaveSelectedConfig()
        {
            if (selectedConfigFile == null)
            {
                return;
            }
            try
            {
                selectedConfigFile.Save();
            }
            catch (Exception exception)
            {
                LogError("保存配置文件失败: " + exception.Message);
            }
        }

        private static void ClearScroll(ISleekScrollView scroll)
        {
            int count = scroll.GetChildCount();
            for (int i = count - 1; i >= 0; i--)
            {
                ISleekElement child = scroll.GetChildAtIndex(i);
                scroll.RemoveChild(child);
            }
        }

        private static string Truncate(string text, int maxLength)
        {
            if (text == null)
            {
                return string.Empty;
            }
            if (text.Length <= maxLength)
            {
                return text;
            }
            return text.Substring(0, maxLength) + "...";
        }

        private static void LogError(string message)
        {
            if (PluginManagerPlugin.log != null)
            {
                PluginManagerPlugin.log.LogError("[PluginManager] " + message);
            }
        }

        // ===== 列表型配置项专用控件（物品列表 / 配方列表） =====

        private const string ItemListTag = "Unturned.ItemList";
        private const string BlueprintListTag = "Unturned.BlueprintList";
        private const string CycleTag = "Unturned.Cycle";

        private enum EPickerKind
        {
            None,
            Items,
            Blueprints,
        }

        // 选择器数据条目
        private class PickerItem
        {
            public string DisplayText;
            public string SearchText;
            public string SerializedValue;
            public ItemAsset IconAsset;
            public Blueprint BlueprintRef;

            public PickerItem(string displayText, string searchText, string serializedValue, ItemAsset iconAsset)
            {
                DisplayText = displayText;
                SearchText = searchText;
                SerializedValue = serializedValue;
                IconAsset = iconAsset;
                BlueprintRef = null;
            }
        }

        private static bool HasItemListTag(ConfigEntryBase entry)
        {
            return HasTag(entry, ItemListTag);
        }

        private static bool HasBlueprintListTag(ConfigEntryBase entry)
        {
            return HasTag(entry, BlueprintListTag);
        }

        // 循环切换按钮标记：Tag 以 "Unturned.Cycle" 开头即启用（可内嵌选项，如 "Unturned.Cycle:OFF|1|2|3|4"）
        private static bool HasCycleTag(ConfigEntryBase entry)
        {
            if (entry.Description == null || entry.Description.Tags == null)
            {
                return false;
            }
            foreach (object t in entry.Description.Tags)
            {
                if (t is string && ((string)t).StartsWith(CycleTag, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        // 解析循环选项：优先取 Tag 内嵌（冒号后用 | 分隔），否则取 AcceptableValueList
        private static string[] GetCycleOptions(ConfigEntryBase entry)
        {
            if (entry.Description != null && entry.Description.Tags != null)
            {
                foreach (object t in entry.Description.Tags)
                {
                    if (!(t is string))
                    {
                        continue;
                    }
                    string tag = (string)t;
                    if (!tag.StartsWith(CycleTag, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    int colon = tag.IndexOf(':');
                    if (colon >= 0 && colon < tag.Length - 1)
                    {
                        string[] parts = tag.Substring(colon + 1).Split('|');
                        List<string> clean = new List<string>();
                        foreach (string part in parts)
                        {
                            string trimmed = part != null ? part.Trim() : string.Empty;
                            if (trimmed.Length > 0)
                            {
                                clean.Add(trimmed);
                            }
                        }
                        if (clean.Count >= 2)
                        {
                            return clean.ToArray();
                        }
                    }
                    break;
                }
            }
            if (entry.Description != null && entry.Description.AcceptableValues is AcceptableValueList<string>)
            {
                AcceptableValueList<string> list = (AcceptableValueList<string>)entry.Description.AcceptableValues;
                if (list.AcceptableValues != null && list.AcceptableValues.Length >= 2)
                {
                    return list.AcceptableValues;
                }
            }
            return null;
        }

        // 循环切换按钮行：左键下一档，右键上一档
        private static void AddCycleRow(ref int y, ConfigEntryBase entry)
        {
            string keyText = entry.Definition.Section + " > " + entry.Definition.Key;

            string description = string.Empty;
            if (entry.Description != null)
            {
                description = entry.Description.Description;
            }
            description = Truncate(description, 120);

            ISleekLabel keyLabel = Glazier.Get().CreateLabel();
            keyLabel.PositionOffset_X = 10f;
            keyLabel.PositionOffset_Y = y;
            keyLabel.SizeOffset_X = 290f;
            keyLabel.SizeOffset_Y = 28f;
            keyLabel.Text = keyText;
            keyLabel.FontSize = ESleekFontSize.Small;
            keyLabel.TextAlignment = TextAnchor.MiddleLeft;
            keyLabel.TextColor = new SleekColor(ESleekTint.FONT);
            detailScroll.AddChild(keyLabel);

            if (description.Length > 0)
            {
                ISleekLabel descriptionLabel = Glazier.Get().CreateLabel();
                descriptionLabel.PositionOffset_X = 10f;
                descriptionLabel.PositionOffset_Y = y + 26;
                descriptionLabel.SizeOffset_X = 1400f;
                descriptionLabel.SizeOffset_Y = 24f;
                descriptionLabel.Text = description;
                descriptionLabel.FontSize = ESleekFontSize.Small;
                descriptionLabel.TextAlignment = TextAnchor.MiddleLeft;
                descriptionLabel.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
                detailScroll.AddChild(descriptionLabel);
            }

            string[] options = GetCycleOptions(entry);
            string current = entry.GetSerializedValue();
            if (current == null)
            {
                current = string.Empty;
            }

            ISleekButton cycleButton = Glazier.Get().CreateButton();
            cycleButton.PositionOffset_X = 310f;
            cycleButton.PositionOffset_Y = y;
            cycleButton.SizeOffset_X = 180f;
            cycleButton.SizeOffset_Y = 30f;
            cycleButton.Text = current;
            cycleButton.TooltipText = "左键：下一档；右键：上一档";
            cycleButton.FontSize = ESleekFontSize.Medium;
            ConfigEntryBase capturedEntry = entry;
            string[] capturedOptions = options;
            ISleekButton capturedButton = cycleButton;
            cycleButton.OnClicked += delegate(ISleekElement b)
            {
                CycleStep(capturedEntry, capturedButton, capturedOptions, 1);
            };
            cycleButton.OnRightClicked += delegate(ISleekElement b)
            {
                CycleStep(capturedEntry, capturedButton, capturedOptions, -1);
            };
            detailScroll.AddChild(cycleButton);

            ISleekLabel typeLabel = Glazier.Get().CreateLabel();
            typeLabel.PositionOffset_X = 620f;
            typeLabel.PositionOffset_Y = y;
            typeLabel.SizeOffset_X = 150f;
            typeLabel.SizeOffset_Y = 30f;
            typeLabel.Text = "循环切换";
            typeLabel.FontSize = ESleekFontSize.Small;
            typeLabel.TextAlignment = TextAnchor.MiddleRight;
            typeLabel.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
            detailScroll.AddChild(typeLabel);

            y += (description.Length > 0) ? 56 : 34;
        }

        // 档位切换：direction 为 1 下一档，-1 上一档；切换后写入配置并保存
        private static void CycleStep(ConfigEntryBase entry, ISleekButton button, string[] options, int direction)
        {
            if (entry == null || options == null || options.Length == 0)
            {
                return;
            }
            try
            {
                string current = entry.GetSerializedValue();
                int index = 0;
                for (int i = 0; i < options.Length; i++)
                {
                    if (string.Equals(options[i], current, StringComparison.Ordinal))
                    {
                        index = i;
                        break;
                    }
                }
                index = (index + direction + options.Length) % options.Length;
                string next = options[index];
                entry.SetSerializedValue(next);
                SaveSelectedConfig();
                if (button != null)
                {
                    button.Text = next;
                }
                ShowStatus("已切换: " + next);
            }
            catch (Exception exception)
            {
                ShowStatus("切换失败: " + exception.Message);
                LogError("切换失败: " + exception.Message);
            }
        }

        // 检测配置项是否带指定标记
        private static bool HasTag(ConfigEntryBase entry, string tag)
        {
            if (entry.Description == null || entry.Description.Tags == null)
            {
                return false;
            }
            foreach (object t in entry.Description.Tags)
            {
                if (t is string && string.Equals((string)t, tag, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        // 列表配置行：键 + 描述 + "+"按钮 + 已添加条目列表（图标 + 名称 + 删除）
        private static void AddListRow(ref int y, ConfigEntryBase entry, EPickerKind kind)
        {
            string keyText = entry.Definition.Section + " > " + entry.Definition.Key;

            string description = string.Empty;
            if (entry.Description != null)
            {
                description = entry.Description.Description;
            }
            description = Truncate(description, 120);

            ISleekLabel keyLabel = Glazier.Get().CreateLabel();
            keyLabel.PositionOffset_X = 10f;
            keyLabel.PositionOffset_Y = y;
            keyLabel.SizeOffset_X = 290f;
            keyLabel.SizeOffset_Y = 28f;
            keyLabel.Text = keyText;
            keyLabel.FontSize = ESleekFontSize.Small;
            keyLabel.TextAlignment = TextAnchor.MiddleLeft;
            keyLabel.TextColor = new SleekColor(ESleekTint.FONT);
            detailScroll.AddChild(keyLabel);

            if (description.Length > 0)
            {
                ISleekLabel descriptionLabel = Glazier.Get().CreateLabel();
                descriptionLabel.PositionOffset_X = 10f;
                descriptionLabel.PositionOffset_Y = y + 26;
                descriptionLabel.SizeOffset_X = 1400f;
                descriptionLabel.SizeOffset_Y = 24f;
                descriptionLabel.Text = description;
                descriptionLabel.FontSize = ESleekFontSize.Small;
                descriptionLabel.TextAlignment = TextAnchor.MiddleLeft;
                descriptionLabel.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
                detailScroll.AddChild(descriptionLabel);
            }

            // “+”按钮：打开选择器
            ISleekButton addButton = Glazier.Get().CreateButton();
            addButton.PositionOffset_X = 310f;
            addButton.PositionOffset_Y = y;
            addButton.SizeOffset_X = 40f;
            addButton.SizeOffset_Y = 30f;
            addButton.Text = "+";
            addButton.TooltipText = "打开" + (kind == EPickerKind.Blueprints ? "配方" : "物品") + "选择器，点击条目添加到列表";
            addButton.FontSize = ESleekFontSize.Large;
            addButton.OnClicked += delegate(ISleekElement b)
            {
                OpenItemPicker(entry, kind);
            };
            detailScroll.AddChild(addButton);

            ISleekLabel typeLabel = Glazier.Get().CreateLabel();
            typeLabel.PositionOffset_X = 620f;
            typeLabel.PositionOffset_Y = y;
            typeLabel.SizeOffset_X = 150f;
            typeLabel.SizeOffset_Y = 30f;
            typeLabel.Text = (kind == EPickerKind.Blueprints) ? "配方列表" : "物品列表";
            typeLabel.FontSize = ESleekFontSize.Small;
            typeLabel.TextAlignment = TextAnchor.MiddleRight;
            typeLabel.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
            detailScroll.AddChild(typeLabel);

            // 已添加条目列表
            y += (description.Length > 0) ? 56 : 34;
            HashSet<string> values = ParsePickerValues(entry.GetSerializedValue());
            if (values.Count == 0)
            {
                AddEntryHint(ref y, "（空）点击 + 添加");
            }
            else
            {
                int shown = 0;
                foreach (string value in values)
                {
                    if (shown >= 8)
                    {
                        AddEntryHint(ref y, "… 共 " + values.Count + " 条（用 + 管理全部）");
                        break;
                    }
                    AddEntryRow(ref y, entry, value, kind);
                    shown++;
                }
            }
            y += 4;
        }

        // 单条已添加条目：图标 + 名称 + 值 + ×删除；配方走专用公式行
        private static void AddEntryRow(ref int y, ConfigEntryBase entry, string value, EPickerKind kind)
        {
            if (kind == EPickerKind.Blueprints)
            {
                Blueprint bp = FindBlueprint(value);
                if (bp != null)
                {
                    AddBlueprintRow(ref y, entry, bp, value);
                    return;
                }
            }

            ISleekBox rowBox = Glazier.Get().CreateBox();
            rowBox.PositionOffset_X = 10f;
            rowBox.PositionOffset_Y = y;
            rowBox.SizeOffset_X = -20f;
            rowBox.SizeOffset_Y = 44f;
            rowBox.SizeScale_X = 1f;
            rowBox.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.35f);
            detailScroll.AddChild(rowBox);

            ItemAsset iconAsset = null;
            string displayName;
            if (kind == EPickerKind.Blueprints)
            {
                displayName = value;
            }
            else
            {
                ushort id;
                if (ushort.TryParse(value, out id))
                {
                    iconAsset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
                }
                displayName = (iconAsset != null) ? iconAsset.FriendlyName : value;
            }

            ISleekImage icon = Glazier.Get().CreateImage();
            icon.PositionOffset_X = 2f;
            icon.PositionOffset_Y = 2f;
            icon.SizeOffset_X = 40f;
            icon.SizeOffset_Y = 40f;
            icon.TintColor = new SleekColor(ESleekTint.FONT);
            rowBox.AddChild(icon);
            RequestItemIcon(iconAsset, icon);

            ISleekLabel nameLabel = Glazier.Get().CreateLabel();
            nameLabel.PositionOffset_X = 48f;
            nameLabel.PositionOffset_Y = 0f;
            nameLabel.SizeOffset_X = -390f;
            nameLabel.SizeOffset_Y = 44f;
            nameLabel.SizeScale_X = 1f;
            nameLabel.Text = Truncate(displayName, 48);
            nameLabel.FontSize = ESleekFontSize.Medium;
            nameLabel.TextAlignment = TextAnchor.MiddleLeft;
            nameLabel.TextColor = new SleekColor(ESleekTint.FONT);
            rowBox.AddChild(nameLabel);

            ISleekLabel valueLabel = Glazier.Get().CreateLabel();
            valueLabel.PositionOffset_X = -400f;
            valueLabel.PositionOffset_Y = 0f;
            valueLabel.PositionScale_X = 1f;
            valueLabel.SizeOffset_X = 300f;
            valueLabel.SizeOffset_Y = 44f;
            valueLabel.Text = value;
            valueLabel.FontSize = ESleekFontSize.Small;
            valueLabel.TextAlignment = TextAnchor.MiddleRight;
            valueLabel.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
            rowBox.AddChild(valueLabel);

            ISleekButton deleteButton = Glazier.Get().CreateButton();
            deleteButton.PositionOffset_X = -44f;
            deleteButton.PositionOffset_Y = 7f;
            deleteButton.PositionScale_X = 1f;
            deleteButton.SizeOffset_X = 36f;
            deleteButton.SizeOffset_Y = 30f;
            deleteButton.Text = "X";
            deleteButton.TooltipText = "从列表中移除 " + value;
            deleteButton.FontSize = ESleekFontSize.Medium;
            deleteButton.BackgroundColor = new SleekColor(new Color(0.72f, 0.18f, 0.18f, 1f));
            deleteButton.OnClicked += delegate(ISleekElement b)
            {
                RemoveListValue(entry, value);
            };
            rowBox.AddChild(deleteButton);

            y += 48;
        }

        private static void AddEntryHint(ref int y, string text)
        {
            ISleekLabel hint = Glazier.Get().CreateLabel();
            hint.PositionOffset_X = 20f;
            hint.PositionOffset_Y = y;
            hint.SizeOffset_X = 800f;
            hint.SizeOffset_Y = 24f;
            hint.Text = text;
            hint.FontSize = ESleekFontSize.Small;
            hint.TextAlignment = TextAnchor.MiddleLeft;
            hint.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
            detailScroll.AddChild(hint);
            y += 30;
        }

        // 从配置值中移除单条并保存
        private static void RemoveListValue(ConfigEntryBase entry, string value)
        {
            if (entry == null)
            {
                return;
            }
            try
            {
                HashSet<string> values = ParsePickerValues(entry.GetSerializedValue());
                if (!values.Remove(value))
                {
                    return;
                }
                List<string> sorted = new List<string>(values);
                sorted.Sort();
                string joined = string.Join(", ", sorted.ToArray());
                entry.SetSerializedValue(joined);
                SaveSelectedConfig();
                ShowStatus("已移除: " + value);
                Refresh();
            }
            catch (Exception exception)
            {
                ShowStatus("移除失败: " + exception.Message);
                LogError("移除失败: " + exception.Message);
            }
        }

        // 异步加载物品图标（结果缓存；目标图片已销毁时静默忽略）
        private static void RequestItemIcon(ItemAsset asset, ISleekImage target)
        {
            if (asset == null || target == null)
            {
                return;
            }
            Texture2D cached;
            if (iconCache.TryGetValue(asset.id, out cached) && cached != null)
            {
                target.Texture = cached;
                return;
            }
            try
            {
                ItemTool.getIcon(asset.id, 0, 100, asset.getState(), asset, null, string.Empty, string.Empty, 128, 128, false, true,
                    delegate(int handle, Texture2D texture)
                    {
                        if (texture != null && !iconCache.ContainsKey(asset.id))
                        {
                            iconCache[asset.id] = texture;
                        }
                        try
                        {
                            target.Texture = texture;
                        }
                        catch
                        {
                            // 目标图片可能已随列表重建销毁
                        }
                    });
            }
            catch (Exception exception)
            {
                LogError("生成物品图标失败: " + exception.Message);
            }
        }

        // 解析配方值 "物品ID:配方编号" 返回蓝图，找不到返回 null
        private static Blueprint FindBlueprint(string value)
        {
            int colon = value.IndexOf(':');
            if (colon <= 0)
            {
                return null;
            }
            ushort ownerId;
            byte index;
            if (!ushort.TryParse(value.Substring(0, colon), out ownerId))
            {
                return null;
            }
            if (!byte.TryParse(value.Substring(colon + 1), out index))
            {
                return null;
            }
            ItemAsset owner = Assets.find(EAssetType.ITEM, ownerId) as ItemAsset;
            if (owner == null || owner.blueprints == null)
            {
                return null;
            }
            foreach (Blueprint blueprint in owner.blueprints)
            {
                if (blueprint != null && blueprint.Index == index)
                {
                    return blueprint;
                }
            }
            return null;
        }

        // 配方条目行：逐成分显示 [图标] 名称 x 数量，输入 + 输入 = 输出（如 "木材 x 2 + 石头 x 1 = 工具台 x 1"）
        private static void AddBlueprintRow(ref int y, ConfigEntryBase entry, Blueprint bp, string value)
        {
            ISleekBox rowBox = Glazier.Get().CreateBox();
            rowBox.PositionOffset_X = 10f;
            rowBox.PositionOffset_Y = y;
            rowBox.SizeOffset_X = -20f;
            rowBox.SizeOffset_Y = 52f;
            rowBox.SizeScale_X = 1f;
            rowBox.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.35f);
            detailScroll.AddChild(rowBox);

            float cx = 10f;
            float maxX = 1380f;
            bool truncated = false;
            if (bp.supplies != null)
            {
                for (int i = 0; i < bp.supplies.Length; i++)
                {
                    BlueprintSupply supply = bp.supplies[i];
                    if (supply == null)
                    {
                        continue;
                    }
                    if (cx > maxX)
                    {
                        truncated = true;
                        break;
                    }
                    cx = AddFormulaComponent(rowBox, cx, 52f, 28f, supply.FindItemAsset(), supply.amount);
                    if (i < bp.supplies.Length - 1)
                    {
                        cx = AddFormulaSeparator(rowBox, cx, 52f, "+");
                    }
                }
            }
            if (!truncated && bp.outputs != null && bp.outputs.Length > 0)
            {
                cx = AddFormulaSeparator(rowBox, cx, 52f, "=");
                for (int i = 0; i < bp.outputs.Length; i++)
                {
                    BlueprintOutput output = bp.outputs[i];
                    if (output == null)
                    {
                        continue;
                    }
                    if (cx > maxX)
                    {
                        truncated = true;
                        break;
                    }
                    cx = AddFormulaComponent(rowBox, cx, 52f, 28f, output.FindItemAsset(), output.amount);
                    if (i < bp.outputs.Length - 1)
                    {
                        cx = AddFormulaSeparator(rowBox, cx, 52f, "+");
                    }
                }
            }
            if (truncated)
            {
                AddFormulaSeparator(rowBox, cx, 52f, "…");
            }

            ISleekButton deleteButton = Glazier.Get().CreateButton();
            deleteButton.PositionOffset_X = -44f;
            deleteButton.PositionOffset_Y = 11f;
            deleteButton.PositionScale_X = 1f;
            deleteButton.SizeOffset_X = 36f;
            deleteButton.SizeOffset_Y = 30f;
            deleteButton.Text = "X";
            deleteButton.TooltipText = "从列表中移除 " + value;
            deleteButton.FontSize = ESleekFontSize.Medium;
            deleteButton.BackgroundColor = new SleekColor(new Color(0.72f, 0.18f, 0.18f, 1f));
            deleteButton.OnClicked += delegate(ISleekElement b)
            {
                RemoveListValue(entry, value);
            };
            rowBox.AddChild(deleteButton);

            y += 56;
        }

        // 公式单个成分：[iconSize 图标] 名称 x 数量，返回下一个内容的 x 坐标
        private static float AddFormulaComponent(ISleekElement parent, float x, float rowHeight, float iconSize, ItemAsset asset, int amount)
        {
            ISleekImage icon = Glazier.Get().CreateImage();
            icon.PositionOffset_X = x;
            icon.PositionOffset_Y = (rowHeight - iconSize) / 2f;
            icon.SizeOffset_X = iconSize;
            icon.SizeOffset_Y = iconSize;
            icon.TintColor = new SleekColor(ESleekTint.FONT);
            parent.AddChild(icon);
            RequestItemIcon(asset, icon);

            string name = (asset != null && !string.IsNullOrEmpty(asset.FriendlyName)) ? asset.FriendlyName : "未知";
            string text = name + " x " + amount;
            float width = Math.Min(iconSize + text.Length * 13f + 12f, 230f);
            ISleekLabel label = Glazier.Get().CreateLabel();
            label.PositionOffset_X = x + iconSize + 4f;
            label.PositionOffset_Y = 0f;
            label.SizeOffset_X = width;
            label.SizeOffset_Y = rowHeight;
            label.Text = text;
            label.FontSize = ESleekFontSize.Small;
            label.TextAlignment = TextAnchor.MiddleLeft;
            label.TextColor = new SleekColor(ESleekTint.FONT);
            parent.AddChild(label);

            return x + iconSize + 4f + width;
        }

        // 公式分隔符（+ / =），返回下一个内容的 x 坐标
        private static float AddFormulaSeparator(ISleekElement parent, float x, float rowHeight, string separator)
        {
            ISleekLabel label = Glazier.Get().CreateLabel();
            label.PositionOffset_X = x;
            label.PositionOffset_Y = 0f;
            label.SizeOffset_X = 30f;
            label.SizeOffset_Y = rowHeight;
            label.Text = separator;
            label.FontSize = ESleekFontSize.Medium;
            label.TextAlignment = TextAnchor.MiddleCenter;
            label.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
            parent.AddChild(label);
            return x + 30f;
        }

        // 打开选择器（kind 决定数据源：物品 / 配方）
        private static void OpenItemPicker(ConfigEntryBase entry, EPickerKind kind)
        {
            if (pickerBox == null)
            {
                return;
            }
            if (pickerData == null || pickerKind != kind)
            {
                pickerData = BuildPickerData(kind);
                pickerKind = kind;
            }
            pickerEntry = entry;
            if (pickerSearchField != null)
            {
                pickerSearchField.Text = string.Empty;
            }
            // 隐藏下层配置面板，避免透过选择器看到重叠内容
            if (detailScroll != null)
            {
                detailScroll.IsVisible = false;
            }
            pickerBox.IsVisible = true;
            RefreshPickerList(string.Empty);
        }

        private static void CloseItemPicker()
        {
            if (pickerBox != null)
            {
                pickerBox.IsVisible = false;
            }
            // 恢复下层配置面板
            if (detailScroll != null)
            {
                detailScroll.IsVisible = true;
            }
            pickerEntry = null;
        }

        // 按类型构建选择器数据源（物品 / 配方）
        private static List<PickerItem> BuildPickerData(EPickerKind kind)
        {
            List<PickerItem> result = new List<PickerItem>();
            try
            {
                List<ItemAsset> assets = new List<ItemAsset>();
                Assets.find(assets);
                assets.Sort(delegate(ItemAsset a, ItemAsset b)
                {
                    return a.id.CompareTo(b.id);
                });
                if (kind == EPickerKind.Blueprints)
                {
                    foreach (ItemAsset owner in assets)
                    {
                        if (owner.blueprints == null)
                        {
                            continue;
                        }
                        foreach (Blueprint blueprint in owner.blueprints)
                        {
                            if (blueprint == null)
                            {
                                continue;
                            }
                            PickerItem item = CreateBlueprintEntry(owner, blueprint);
                            if (item != null)
                            {
                                result.Add(item);
                            }
                        }
                    }
                }
                else
                {
                    foreach (ItemAsset asset in assets)
                    {
                        result.Add(CreateItemEntry(asset));
                    }
                }
                LogInfo("选择器数据已加载（" + (kind == EPickerKind.Blueprints ? "配方" : "物品") + "），共 " + result.Count + " 条");
            }
            catch (Exception exception)
            {
                LogError("加载选择器数据失败: " + exception.Message);
            }
            return result;
        }

        // 物品条目：序列化值为物品 ID
        private static PickerItem CreateItemEntry(ItemAsset asset)
        {
            string idText = asset.id.ToString();
            string name = asset.FriendlyName != null ? asset.FriendlyName : idText;
            string description = asset.itemDescription != null ? asset.itemDescription : string.Empty;
            string search = name + " " + idText + " " + description;
            string display = "[" + idText + "] " + Truncate(name, 42);
            return new PickerItem(display, search, idText, asset);
        }

        // 配方条目：序列化值为 "所属物品ID:配方编号"；无输入/无输出或存在未知成分的配方直接跳过（返回 null）
        private static PickerItem CreateBlueprintEntry(ItemAsset owner, Blueprint blueprint)
        {
            if (blueprint.supplies == null || blueprint.supplies.Length == 0)
            {
                return null;
            }
            if (blueprint.outputs == null || blueprint.outputs.Length == 0)
            {
                return null;
            }
            foreach (BlueprintSupply bpSupply in blueprint.supplies)
            {
                ItemAsset supplyAsset = bpSupply != null ? bpSupply.FindItemAsset() : null;
                if (supplyAsset == null || string.IsNullOrEmpty(supplyAsset.FriendlyName))
                {
                    return null;
                }
            }
            foreach (BlueprintOutput bpOutput in blueprint.outputs)
            {
                ItemAsset outAsset = bpOutput != null ? bpOutput.FindItemAsset() : null;
                if (outAsset == null || string.IsNullOrEmpty(outAsset.FriendlyName))
                {
                    return null;
                }
            }

            string outputName = "未知";
            ItemAsset outputAsset = null;
            BlueprintOutput output = (blueprint.outputs != null && blueprint.outputs.Length > 0) ? blueprint.outputs[0] : null;
            if (output != null)
            {
                outputAsset = output.FindItemAsset();
                if (outputAsset != null)
                {
                    outputName = outputAsset.FriendlyName != null ? outputAsset.FriendlyName : outputAsset.id.ToString();
                }
            }
            string ownerName = owner.FriendlyName != null ? owner.FriendlyName : owner.id.ToString();
            string display = outputName + "（来自 " + ownerName + "）";
            string serialized = owner.id + ":" + blueprint.Index;
            string search = (blueprint.Name != null ? blueprint.Name : string.Empty) + " " + display + " " + outputName + " " + ownerName + " " + owner.id + " " + serialized;
            PickerItem item = new PickerItem(display, search, serialized, outputAsset);
            item.BlueprintRef = blueprint;
            return item;
        }

        private static void OnPickerSearchChanged(ISleekField field, string text)
        {
            RefreshPickerList(text != null ? text.Trim() : string.Empty);
        }

        // 重建选择器列表；无关键字时最多显示 200 个，避免一次性创建过多按钮
        private static void RefreshPickerList(string query)
        {
            if (pickerList == null)
            {
                return;
            }
            ClearScroll(pickerList);
            if (pickerData == null || pickerData.Count == 0)
            {
                pickerList.ContentSizeOffset = new Vector2(0f, 0f);
                return;
            }

            bool hasQuery = query.Length > 0;
            int limit = hasQuery ? 300 : 200;
            int y = 0;
            int shown = 0;
            foreach (PickerItem item in pickerData)
            {
                if (hasQuery && item.SearchText.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                if (shown >= limit)
                {
                    break;
                }

                ISleekButton row = Glazier.Get().CreateButton();
                row.PositionOffset_X = 0f;
                row.PositionOffset_Y = y;
                row.SizeOffset_X = 0f;
                row.SizeOffset_Y = 40f;
                row.SizeScale_X = 1f;
                row.Text = string.Empty;
                row.TooltipText = item.SearchText;
                PickerItem captured = item;
                row.OnClicked += delegate(ISleekElement b)
                {
                    AddItemToPicker(captured);
                };

                if (item.BlueprintRef != null)
                {
                    // 配方条目：逐成分公式预览 [图标]名称 x 数量 + ... = ...
                    Blueprint bp = item.BlueprintRef;
                    float cx = 6f;
                    if (bp.supplies != null)
                    {
                        for (int i = 0; i < bp.supplies.Length; i++)
                        {
                            BlueprintSupply supply = bp.supplies[i];
                            if (supply == null)
                            {
                                continue;
                            }
                            cx = AddFormulaComponent(row, cx, 40f, 22f, supply.FindItemAsset(), supply.amount);
                            if (i < bp.supplies.Length - 1)
                            {
                                cx = AddFormulaSeparator(row, cx, 40f, "+");
                            }
                        }
                    }
                    cx = AddFormulaSeparator(row, cx, 40f, "=");
                    if (bp.outputs != null && bp.outputs.Length > 0)
                    {
                        for (int i = 0; i < bp.outputs.Length; i++)
                        {
                            BlueprintOutput output = bp.outputs[i];
                            if (output == null)
                            {
                                continue;
                            }
                            cx = AddFormulaComponent(row, cx, 40f, 22f, output.FindItemAsset(), output.amount);
                            if (i < bp.outputs.Length - 1)
                            {
                                cx = AddFormulaSeparator(row, cx, 40f, "+");
                            }
                        }
                    }
                }
                else
                {
                    ISleekImage rowIcon = Glazier.Get().CreateImage();
                    rowIcon.PositionOffset_X = 6f;
                    rowIcon.PositionOffset_Y = 8f;
                    rowIcon.SizeOffset_X = 24f;
                    rowIcon.SizeOffset_Y = 24f;
                    rowIcon.TintColor = new SleekColor(ESleekTint.FONT);
                    row.AddChild(rowIcon);
                    RequestItemIcon(item.IconAsset, rowIcon);

                    ISleekLabel rowText = Glazier.Get().CreateLabel();
                    rowText.PositionOffset_X = 36f;
                    rowText.PositionOffset_Y = 0f;
                    rowText.SizeOffset_X = -46f;
                    rowText.SizeOffset_Y = 40f;
                    rowText.SizeScale_X = 1f;
                    rowText.Text = item.DisplayText;
                    rowText.FontSize = ESleekFontSize.Medium;
                    rowText.TextAlignment = TextAnchor.MiddleLeft;
                    rowText.TextColor = new SleekColor(ESleekTint.FONT);
                    row.AddChild(rowText);
                }

                pickerList.AddChild(row);
                y += 44;
                shown++;
            }

            if (pickerData.Count > limit)
            {
                ISleekLabel hint = Glazier.Get().CreateLabel();
                hint.PositionOffset_X = 0f;
                hint.PositionOffset_Y = y;
                hint.SizeOffset_X = 1000f;
                hint.SizeOffset_Y = 24f;
                hint.Text = "共 " + pickerData.Count + " 条，输入关键字搜索名称 / ID / 描述查看更多";
                hint.FontSize = ESleekFontSize.Small;
                hint.TextAlignment = TextAnchor.MiddleLeft;
                hint.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT);
                pickerList.AddChild(hint);
                y += 28;
            }

            pickerList.ContentSizeOffset = new Vector2(0f, y);
            pickerList.ScrollToTop();
        }

        // 把条目序列化值追加到配置值（逗号分隔、去重、排序），保存并刷新显示
        private static void AddItemToPicker(PickerItem item)
        {
            if (pickerEntry == null)
            {
                return;
            }
            try
            {
                HashSet<string> values = ParsePickerValues(pickerEntry.GetSerializedValue());
                if (!values.Add(item.SerializedValue))
                {
                    ShowStatus("已在列表中: " + item.DisplayText);
                    return;
                }

                List<string> sorted = new List<string>(values);
                sorted.Sort();
                string joined = string.Join(", ", sorted.ToArray());

                pickerEntry.SetSerializedValue(joined);
                SaveSelectedConfig();
                ShowStatus("已添加: " + item.DisplayText);
                Refresh();
            }
            catch (Exception exception)
            {
                ShowStatus("添加失败: " + exception.Message);
                LogError("添加失败: " + exception.Message);
            }
        }

        // 解析逗号分隔的值字符串（不做类型校验，保持原样）
        private static HashSet<string> ParsePickerValues(string serialized)
        {
            HashSet<string> values = new HashSet<string>();
            if (!string.IsNullOrWhiteSpace(serialized))
            {
                string[] parts = serialized.Split(',');
                foreach (string part in parts)
                {
                    string trimmed = part != null ? part.Trim() : string.Empty;
                    if (trimmed.Length > 0)
                    {
                        values.Add(trimmed);
                    }
                }
            }
            return values;
        }

        // 把列表值格式化为可读预览，过长截断（按类型格式化：物品 / 配方）
        private static string FormatListPreview(string serialized, EPickerKind kind)
        {
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return "(空)";
            }
            string result = string.Empty;
            int shown = 0;
            string[] parts = serialized.Split(',');
            foreach (string part in parts)
            {
                string trimmed = part != null ? part.Trim() : string.Empty;
                if (trimmed.Length == 0)
                {
                    continue;
                }
                string piece = (kind == EPickerKind.Blueprints) ? FormatBlueprintValue(trimmed) : FormatItemValue(trimmed);
                if (piece == null)
                {
                    continue;
                }
                if (result.Length > 0)
                {
                    result += "; ";
                }
                result += piece;
                shown++;
                if (shown >= 12)
                {
                    result += " …";
                    break;
                }
            }
            if (result.Length > 60)
            {
                result = result.Substring(0, 60) + "…";
            }
            return result;
        }

        // 单个物品值 -> "ID 名称"
        private static string FormatItemValue(string value)
        {
            ushort id;
            if (!ushort.TryParse(value, out id))
            {
                return value;
            }
            ItemAsset asset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
            return id + " " + (asset != null ? asset.FriendlyName : "未知");
        }

        // 单个配方值 "物品ID:配方编号" -> 输出物品名（找不到则原样返回）
        private static string FormatBlueprintValue(string value)
        {
            int colon = value.IndexOf(':');
            if (colon <= 0)
            {
                return value;
            }
            ushort ownerId;
            byte index;
            if (!ushort.TryParse(value.Substring(0, colon), out ownerId))
            {
                return value;
            }
            if (!byte.TryParse(value.Substring(colon + 1), out index))
            {
                return value;
            }
            ItemAsset owner = Assets.find(EAssetType.ITEM, ownerId) as ItemAsset;
            if (owner == null || owner.blueprints == null)
            {
                return value;
            }
            foreach (Blueprint blueprint in owner.blueprints)
            {
                if (blueprint == null || blueprint.Index != index)
                {
                    continue;
                }
                if (blueprint.outputs != null && blueprint.outputs.Length > 0)
                {
                    ItemAsset outputAsset = blueprint.outputs[0].FindItemAsset();
                    if (outputAsset != null)
                    {
                        return outputAsset.FriendlyName != null ? outputAsset.FriendlyName : outputAsset.id.ToString();
                    }
                }
                return "配方 " + index;
            }
            return value;
        }
    }

    // ===== Harmony 补丁 =====

    // 创意工坊 UI 构建完成后立即注入“插件管理”按钮（快速路径；主菜单重建后由容器比对自动重新注入）
    [HarmonyPatch(typeof(MenuWorkshopUI), MethodType.Constructor)]
    public static class PatchMenuWorkshopUIConstructor
    {
        public static void Postfix()
        {
            PluginManagerUI.TryAddEntryButton();
        }
    }

    // 游戏内暂停菜单构建完成后立即注入“插件管理”按钮（快速路径；玩家界面重建后由容器比对自动重新注入，Tick 轮询兜底）
    [HarmonyPatch(typeof(PlayerPauseUI), MethodType.Constructor)]
    public static class PatchPlayerPauseUIConstructor
    {
        public static void Postfix()
        {
            PluginManagerUI.TryAddPauseButton();
        }
    }

    // 拦截主菜单 ESC：插件管理窗口打开时，ESC 关闭窗口并返回创意工坊（不触发游戏默认的返回主菜单逻辑）
    [HarmonyPatch(typeof(MenuUI), "escapeMenu")]
    public static class PatchMenuUIEscapeMenu
    {
        public static bool Prefix()
        {
            if (PluginManagerUI.IsOpen)
            {
                PluginManagerUI.CloseAndReturn();
                return false;
            }
            return true;
        }
    }

    // 拦截游戏内 ESC：插件管理窗口打开时，ESC 关闭窗口并返回暂停菜单（不触发游戏默认的关闭暂停菜单逻辑）
    [HarmonyPatch(typeof(PlayerUI), "escapeMenu")]
    public static class PatchPlayerUIEscapeMenu
    {
        public static bool Prefix()
        {
            if (PluginManagerUI.IsOpen)
            {
                PluginManagerUI.CloseAndReturn();
                return false;
            }
            return true;
        }
    }

    // 开始游戏/退出主菜单时（closeAll 被调用），自动关闭插件管理窗口
    [HarmonyPatch(typeof(MenuUI), "closeAll")]
    public static class PatchMenuUICloseAll
    {
        public static void Postfix()
        {
            PluginManagerUI.CloseIfOpen();
        }
    }
}
