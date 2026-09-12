using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

// DEV-V2-15: migrated from LaunchInventoryTidy (author: YU80Rice, MIT License,
// Copyright (c) 2026 YU80Rice; local source of truth: Archive/2-未闭环验证项目/LaunchInventoryTidy,
// retired by the wayfinder single-source decision). Attribution:
// docs/third-party/LaunchInventoryTidy-attribution.md. Behavior migrated as-is
// unless a DEV-V2-15 note says otherwise.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// 拦截 PlayerDashboardInventoryUI 构造函数，为 5 个标题栏（headers[0..4]，对应
    /// Hands/Backpack/Vest/Shirt/Pants）各注入一颗「整理」按钮（DEV-V4-06：
    /// 标题栏只留一颗——模式与方向改为全局 ClientPreference Choice，在 LIT
    /// 设置页用循环切换改）：
    ///   - 整理按钮 [整理]：60×60，PositionOffset_X=-130（右侧预留 70px 避让
    ///     耐久度文字与品质角标）；左键整理当前栏，Ctrl+左键按已保存的全局
    ///     模式与方向整理全身（不含仓储栏）。
    ///   - 注入与拆除由生命周期事实决定（V4-T5 Q55 九态表）：仅 Running 新开
    ///     页注入；Disabled/Stopped/Isolated/Incompatible 拆除已有按钮；过渡
    ///     态不新增、点击走原生回退。patch 不自持任何可用性布尔。
    ///
    /// 由于编译时不认识 ISleekElement/ISleekButton（在 Glazier.dll 中），
    /// 全部用反射 + Reflection.Emit 动态生成委托。
    ///
    /// DEV-V2-15 迁入改写：
    ///   - Harmony ID 收编 FeatureId（模块的 Harmony 实例持有，Start 装 / Stop UnpatchSelf）；
    ///   - 点击走模块的 RequestTidyFromUiClick（生命周期门禁 + 已保存快照读）；
    ///   - DEV-V4-06：每页内存字典（方向/模式）退役——模式与方向是全局已保存
    ///     设置，按钮引用表仅用于停用/隔离时的拆除。
    /// </summary>
    [HarmonyPatch(typeof(PlayerDashboardInventoryUI), MethodType.Constructor)]
    internal static class InventoryTidyUiPatch
    {
        private const string TAG = "[TidyUI]";

        /// <summary>
        /// The armed module instance the patch routes clicks to. Set by the
        /// module's patch install, cleared by uninstall/stop — a click with
        /// no armed module is a no-op, never a crash. Availability itself is
        /// NOT this reference: the module re-reads the lifecycle fact on
        /// every injection and every click (DEV-V4-06).
        /// </summary>
        internal static InventoryTidyModule ActiveModule { get; set; }

        /// <summary>Host-test observable: whether any injected tidy button is
        /// still tracked (the teardown pass clears the map; the retired
        /// per-page dictionaries never come back). Read-only diagnostics —
        /// never an availability input.</summary>
        internal static bool HasTrackedButtons
        {
            get { return s_TidyButtons.Count > 0; }
        }

        /// <summary>
        /// Host-test seam (the module's NetServiceFactoryForTests precedent):
        /// the host has no Glazier, so the reflection RemoveChild cannot run
        /// there. When set, the teardown pass routes the per-button removal
        /// through it (header element, button) → removed? Null = production
        /// reflection path.
        /// </summary>
        internal static Func<object, object, bool> RemoveChildForTests;

        /// <summary>Host-test seam: provides the header array for the
        /// alive-dashboard re-inject pass (the host has no SDG.Unturned
        /// statics to read). Null = production static-field read.</summary>
        internal static Func<Array> HeadersForTests;

        /// <summary>Host-test seam: when set, the inject pass routes each
        /// per-button creation through it (page, headerElement) → drawn?
        /// Null = production reflection path (Glazier CreateButton). Same
        /// family as RemoveChildForTests / the module's NetServiceFactory
        /// ForTests seam.</summary>
        internal static Func<byte, object, bool> InjectButtonForTests;

        /// <summary>Host-test seam: registers a button reference exactly as
        /// the Glazier Postfix would, so the teardown pass is observable
        /// in-host (which buttons are tracked, unbound, removed, cleared).</summary>
        internal static void TrackButtonForTests(byte page, object button, Delegate clickHandler, object headerElement)
        {
            s_TidyButtons[page] = button;
            s_TidyClickHandlers[page] = clickHandler;
            s_TidyHeaders[page] = headerElement;
        }

        /// <summary>
        /// DEV-V4-06: the teardown pass (Q55「拆掉」): for every tracked
        /// button — unbind the click callback, remove the object from its
        /// native header (never breaking the header itself), then drop the
        /// patch-held reference. Q55 red line「移除失败不得把停用伪装成成功」:
        /// a button whose removal FAILED keeps its tracked reference — the
        /// object still lives in the UI tree, so dropping the reference would
        /// orphan it and fake a clean teardown; the pass logs the failure
        /// with diagnosticId=BUE-LIT-TEARDOWN and a later pass (the next
        /// Stop/withdrawal) retries it. Triggered by the module's Stop
        /// (phase 3) and by the lifecycle machine's resource withdrawal via
        /// <see cref="UiTeardownHandle"/> (isolation never calls Stop).
        /// </summary>
        internal static void RemoveInjectedButtons()
        {
            if (s_TidyButtons.Count == 0 && s_TidyClickHandlers.Count == 0 && s_TidyHeaders.Count == 0) return;
            var removedPages = new List<byte>();
            var failedPages = new List<byte>();
            foreach (var pair in s_TidyButtons)
            {
                var page = pair.Key;
                var button = pair.Value;
                if (button == null) { removedPages.Add(page); continue; }
                // 解绑回调：引用随按钮一起离开 UI 树，回调必须同步松开。
                Delegate handler;
                if (s_TidyClickHandlers.TryGetValue(page, out handler) && handler != null && s_OnClicked != null)
                {
                    try { s_OnClicked.RemoveEventHandler(button, handler); }
                    catch (Exception e) { LogError($"page {page} 整理按钮回调解绑失败（继续移除）: {e.Message}"); }
                }
                object headerElement;
                s_TidyHeaders.TryGetValue(page, out headerElement);
                if (RemoveButtonFromHeader(headerElement, button)) removedPages.Add(page);
                else failedPages.Add(page);
            }
            // 只有确认移除的按钮才清引用；失败页引用保留，下一次拆除重试。
            for (var i = 0; i < removedPages.Count; i++)
            {
                s_TidyButtons.Remove(removedPages[i]);
                s_TidyClickHandlers.Remove(removedPages[i]);
                s_TidyHeaders.Remove(removedPages[i]);
            }
            if (failedPages.Count > 0)
            {
                var pages = string.Join(",", failedPages.ConvertAll(x => x.ToString()).ToArray());
                LogError($"==== 拆除未完成：removed={removedPages.Count} failed={failedPages.Count}（page {pages}）——失败按钮引用保留待重试，不伪装拆除干净 diagnosticId=BUE-LIT-TEARDOWN ====");
            }
            else
            {
                LogInfo($"==== 拆除完成：removed={removedPages.Count}（原生标题栏保持完好）====");
            }
        }

        /// <summary>The reflection RemoveChild against the native header; the
        /// host-test seam replaces it when Glazier cannot exist.</summary>
        private static bool RemoveButtonFromHeader(object headerElement, object button)
        {
            var overrideForTests = RemoveChildForTests;
            if (overrideForTests != null) return overrideForTests(headerElement, button);
            if (headerElement == null || s_RemoveChild == null)
            {
                LogError("无法执行 RemoveChild（原生 RemoveChild 未定位或 header 缺失）——按钮对象可能残留，该页引用保留待重试 diagnosticId=BUE-LIT-TEARDOWN");
                return false;
            }
            try
            {
                s_OneArg[0] = button;
                s_RemoveChild.Invoke(headerElement, s_OneArg);
                return true;
            }
            catch (Exception e)
            {
                LogError($"RemoveChild 调用失败（按钮对象可能残留，该页引用保留待重试）diagnosticId=BUE-LIT-TEARDOWN: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// DEV-V4-06: the module tracks this handle through the host lifecycle
        /// seam (IFeatureLifetime.TryTrack) — the machine's Withdraw disposes
        /// it on isolation AND complete-stop, which reaches the same teardown
        /// pass even when no module.Stop runs (isolation never calls Stop).
        /// Dispose is idempotent and never throws.
        /// </summary>
        internal sealed class UiTeardownHandle : IDisposable
        {
            public void Dispose()
            {
                try { RemoveInjectedButtons(); }
                catch (Exception e) { LogError("UiTeardownHandle.Dispose 拆除失败（引用保留待重试）diagnosticId=BUE-LIT-TEARDOWN: " + e.Message); }
            }
        }

        // ── headers 私有静态字段（Assembly-CSharp 内）──
        private static readonly FieldInfo s_HeadersField =
            typeof(PlayerDashboardInventoryUI).GetField("headers", BindingFlags.Static | BindingFlags.NonPublic);

        // ── 反射缓存 ──
        private static Type     s_GlazierType;
        private static Type     s_ISleekElementType;
        private static Type     s_ISleekButtonType;
        private static MethodInfo s_GlazierGet;
        private static MethodInfo s_CreateButton;
        private static bool      s_CreateButtonResolved;
        private static MethodInfo s_AddChild;
        private static MethodInfo s_RemoveChild;
        // 布局 (ISleekElement)
        private static PropertyInfo s_PosScaleX;
        private static PropertyInfo s_PosOffsetX;
        private static PropertyInfo s_SizeOffsetX;
        private static PropertyInfo s_SizeOffsetY;
        // 按钮 (ISleekButton)
        private static PropertyInfo s_Text;
        private static PropertyInfo s_TooltipText;
        private static EventInfo    s_OnClicked;

        private static readonly object[] s_EmptyArgs = new object[0];
        private static readonly object[] s_OneArg    = new object[1];
        private static bool s_Initialised;

        // ── DEV-V4-06：已注入「整理」按钮的在册引用（仅用于拆除）──
        // key = page (2..6)。patch 不持任何可用性状态：注入/点击都由模块按
        // 生命周期事实现判（ShouldInjectTidyButtonForNewPage / 点击门禁）。
        private static readonly Dictionary<byte, object> s_TidyButtons =
            new Dictionary<byte, object>();
        // key = page，value = 该页点击委托（拆除时解绑）。
        private static readonly Dictionary<byte, Delegate> s_TidyClickHandlers =
            new Dictionary<byte, Delegate>();
        // key = page，value = 该页 header 元素（拆除时 RemoveChild 的父容器）。
        private static readonly Dictionary<byte, object> s_TidyHeaders =
            new Dictionary<byte, object>();

        // ── 按钮布局常量 ──
        // DEV-V4-06：标题栏只画一颗「整理」。右侧预留 70px 安全空间，避让
        // 原版 "100%" 耐久度文字与绿色品质角标。
        //
        // 排版几何（PositionScale_X = 1，相对父容器右边缘）：
        //   - [整理] 按钮 B：宽 60，PositionOffset_X = -130 -> 右边缘 -70，
        //     恰好填满安全区左边界（Q54 冻结：60×60 @ -130，不留锁定空位）。
        //
        // 视觉顺序（从左到右）：[整理]  70px  [原版 100% 耐久度+绿色角标]
        private const float BTN_SIZE_Y         = 60f;
        private const float TIDY_POS_OFFSET_X  = -130f;
        private const float TIDY_SIZE_X        = 60f;

        // ── 容器页（page=STORAGE=7，headers[5]）不注入（v2.0.1 起的既定裁决，
        // DEV-V4-06 延续）：V2 协议仅支持 page 2..6 服装页；容器页涉及
        // InteractableStorage 生命周期、跨玩家并发、工坊虚拟容器等独立权限模型，
        // 不能与服装页共用同一套规则；注入会造成 UI 可点击但服务端确定性拒绝
        // 的误导性 UI。旧 STORAGE 布局常量已随本票退役删除。

        // headers 循环上限：i=0..4 -> page 2..6（SLOTS..PANTS 服装页）。
        private const int HEADER_INJECT_COUNT = 5;

        // DEV-V4-06：tooltip 用玩家语言钉死手势契约（Q54 原文）——模式与方向
        // 已迁往 LIT 设置页（全局 ClientPreference Choice），不在标题栏。
        private const string TOOLTIP_TIDY =
            "左键：整理当前栏；Ctrl+左键：按全局模式和方向整理全身（不含仓储栏）";

        private static void LogError(string msg) => LitRuntime.LogError($"{TAG} {msg}");
        private static void LogInfo(string msg)  => LitRuntime.LogInfo($"{TAG} {msg}");

        // ─────────────────────────────────────────────────────────────────
        // 反射预热
        // ─────────────────────────────────────────────────────────────────
        private static void WarmupReflection()
        {
            if (s_Initialised) return;
            s_Initialised = true;

            if (s_HeadersField == null) { LogError("无法定位 PlayerDashboardInventoryUI.headers 字段！"); return; }
            LogInfo("headers 字段 OK");

            s_GlazierType = AccessTools.TypeByName("SDG.Unturned.Glazier");
            if (s_GlazierType == null) { LogError("无法定位 SDG.Unturned.Glazier 类型！"); return; }
            LogInfo("Glazier 类型 OK");

            s_ISleekElementType = AccessTools.TypeByName("SDG.Unturned.ISleekElement");
            if (s_ISleekElementType == null) { LogError("无法定位 ISleekElement 类型！"); return; }
            LogInfo("ISleekElement 类型 OK");

            s_ISleekButtonType = AccessTools.TypeByName("SDG.Unturned.ISleekButton");
            if (s_ISleekButtonType == null) { LogError("无法定位 ISleekButton 类型！"); return; }
            LogInfo("ISleekButton 类型 OK");

            s_GlazierGet = s_GlazierType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public);
            if (s_GlazierGet == null) { LogError("无法定位 Glazier.Get() 方法！"); return; }

            s_AddChild = GetInterfaceMethod(s_ISleekElementType, "AddChild");
            if (s_AddChild == null) { LogError("无法定位 ISleekElement.AddChild() 方法！"); return; }

            // DEV-V4-06: the teardown pass needs the native RemoveChild; a miss
            // means every removal FAILS (reference retained, BUE-LIT-TEARDOWN
            // logged, retried on the next pass) — never a reason to skip the
            // injection and never a fake clean teardown.
            s_RemoveChild = GetInterfaceMethod(s_ISleekElementType, "RemoveChild");
            if (s_RemoveChild == null) LogError("ISleekElement.RemoveChild() 未定位（拆除将失败留痕并保留引用待重试 diagnosticId=BUE-LIT-TEARDOWN）");

            LogInfo("Glazier.Get / AddChild OK (CreateButton 推迟)");

            s_PosScaleX   = GetInterfaceProperty(s_ISleekElementType, "PositionScale_X");
            s_PosOffsetX  = GetInterfaceProperty(s_ISleekElementType, "PositionOffset_X");
            s_SizeOffsetX = GetInterfaceProperty(s_ISleekElementType, "SizeOffset_X");
            s_SizeOffsetY = GetInterfaceProperty(s_ISleekElementType, "SizeOffset_Y");
            if (s_PosScaleX == null || s_PosOffsetX == null || s_SizeOffsetX == null || s_SizeOffsetY == null)
            {
                LogError("ISleekElement 布局属性定位失败！");
                DumpAvailableMembers(s_ISleekElementType, "ISleekElement");
                return;
            }
            LogInfo("ISleekElement 布局属性 OK");

            s_Text        = GetInterfaceProperty(s_ISleekButtonType, "Text");
            s_TooltipText = GetInterfaceProperty(s_ISleekButtonType, "TooltipText");
            if (s_Text == null || s_TooltipText == null)
            {
                LogError("ISleekButton.Text / TooltipText 属性定位失败！");
                DumpAvailableMembers(s_ISleekButtonType, "ISleekButton");
                return;
            }
            LogInfo("ISleekButton Text / TooltipText OK");

            s_OnClicked = ResolveClickedEvent(s_ISleekButtonType);
            if (s_OnClicked == null)
            {
                LogError("ISleekButton.OnClicked 事件定位失败！");
                DumpAvailableMembers(s_ISleekButtonType, "ISleekButton");
                return;
            }
            LogInfo("OnClicked 事件 OK");

            LogInfo("全部 Reflection 缓存预热成功");
        }

        // ─────────────────────────────────────────────────────────────────
        // 递归接口成员查找（穿透接口继承链）
        // ─────────────────────────────────────────────────────────────────
        private static PropertyInfo GetInterfaceProperty(Type type, string name)
        {
            if (type == null) return null;
            PropertyInfo prop = type.GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null) return prop;
            foreach (Type parent in type.GetInterfaces())
            {
                prop = GetInterfaceProperty(parent, name);
                if (prop != null) return prop;
            }
            return null;
        }

        private static EventInfo GetInterfaceEvent(Type type, string name)
        {
            if (type == null) return null;
            EventInfo ev = type.GetEvent(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (ev != null) return ev;
            foreach (Type parent in type.GetInterfaces())
            {
                ev = GetInterfaceEvent(parent, name);
                if (ev != null) return ev;
            }
            return null;
        }

        private static MethodInfo GetInterfaceMethod(Type type, string name)
        {
            if (type == null) return null;
            MethodInfo m = type.GetMethod(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (m != null) return m;
            foreach (Type parent in type.GetInterfaces())
            {
                m = GetInterfaceMethod(parent, name);
                if (m != null) return m;
            }
            return null;
        }

        private static void DumpAvailableMembers(Type type, string label)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"{TAG} -- {label} ({type.FullName}) 可用成员 --");
                sb.AppendLine("  [Properties]");
                foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    sb.AppendLine($"    {p.PropertyType.Name} {p.Name}");
                foreach (var parent in type.GetInterfaces())
                    foreach (var p in parent.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        sb.AppendLine($"    [{parent.Name}] {p.PropertyType.Name} {p.Name}");
                sb.AppendLine("  [Events]");
                foreach (var e in type.GetEvents(BindingFlags.Public | BindingFlags.Instance))
                    sb.AppendLine($"    {e.Name}");
                foreach (var parent in type.GetInterfaces())
                    foreach (var e in parent.GetEvents(BindingFlags.Public | BindingFlags.Instance))
                        sb.AppendLine($"    [{parent.Name}] {e.Name}");
                sb.AppendLine("  [Methods]");
                foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    sb.AppendLine($"    {m.Name}");
                foreach (var parent in type.GetInterfaces())
                    foreach (var m in parent.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        sb.AppendLine($"    [{parent.Name}] {m.Name}");
                LogInfo(sb.ToString());
            }
            catch { }
        }

        private static EventInfo ResolveClickedEvent(Type type)
        {
            var ev = GetInterfaceEvent(type, "OnClicked");
            if (ev != null) return ev;
            foreach (var e in type.GetEvents(BindingFlags.Public | BindingFlags.Instance))
            {
                if (e.Name.IndexOf("clicked", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    LogInfo("OnClicked 事件模糊匹配 -> " + e.Name);
                    return e;
                }
            }
            foreach (var parent in type.GetInterfaces())
                foreach (var e in parent.GetEvents(BindingFlags.Public | BindingFlags.Instance))
                    if (e.Name.IndexOf("clicked", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        LogInfo("OnClicked 事件模糊匹配(父接口) -> " + parent.Name + "." + e.Name);
                        return e;
                    }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Postfix：构造完成后注入 5 颗「整理」按钮
        // ─────────────────────────────────────────────────────────────────
        public static void Postfix()
        {
            // DEV-V4-06 注入门（Q55）：只有 Running 事实允许新开页注入。模块
            // 缺席（未启动/已停止/已隔离撤 ActiveModule）或生命周期事实非
            // Running，一律不画——「按钮曾被画出」不构成注入资格。
            var module = ActiveModule;
            if (module == null || !module.ShouldInjectTidyButtonForNewPage) return;

            var headers = ReadStaticHeaders();
            if (headers == null)
            {
                LogError("headers 数组为 null！");
                return;
            }
            LogInfo("headers 数组 OK (Length=" + headers.Length + ")");
            // ctor=新仪表盘：旧行为=覆盖在册（不去重）——仪表盘重建时旧按钮随旧
            // headers 丢弃，新页必须重画（DEV-V4-09 泵路径才去重）。
            InjectButtonsInto(headers, skipTrackedPages: false);
        }

        // DEV-V4-09 F1（实机二轮=实时注入）：由模块 Tick 泵每 16 拍调用一次——
        // Start 运行在机器 Starting 态（九态门必然拒绝），重启用落地 Running 后
        // 由泵补注入；仪表盘未构造时静态读失败/为 null=无事可做（首个 ctor
        // Postfix 会注入）。与 ctor 路径的差异：在册去重（同一存活的 headers，
        // 重画=双按钮；Q55 移除失败页引用保留→跳过→下次拆除重试）。
        internal static void TryInjectIntoAliveDashboard(InventoryTidyModule module)
        {
            if (module == null || !module.ShouldInjectTidyButtonForNewPage) return;
            if (InjectButtonForTests == null && !module.PatchesInstalled) return;
            Array headers = HeadersForTests != null ? HeadersForTests() : ReadStaticHeaders();
            if (headers == null) return;
            InjectButtonsInto(headers, skipTrackedPages: true);
        }

        // 静态 headers 读（生产路径）。任何失败（宿主无游戏程序集 / 仪表盘未
        // 构建）= null，调用方按「无存活页」处理，绝不抛。
        private static Array ReadStaticHeaders()
        {
            try
            {
                WarmupReflection();
                if (s_HeadersField == null) return null;
                return s_HeadersField.GetValue(null) as Array;
            }
            catch (Exception) { return null; }
        }

        // 注入主体（ctor Postfix 与实时注入泵共用）：反射解析只走生产路径（宿主
        // 经 InjectButtonForTests 缝绕过）；skipTrackedPages=泵路径在册去重
        // （同一存活 headers 不重画），ctor 路径传 false（新仪表盘覆盖在册）。
        private static void InjectButtonsInto(Array headers, bool skipTrackedPages)
        {
            if (headers == null || headers.Length < HEADER_INJECT_COUNT)
            {
                LogError($"headers 数组为 null 或长度 < {HEADER_INJECT_COUNT}！");
                return;
            }

            object glazier = null;
            if (InjectButtonForTests == null)
            {
                WarmupReflection();
                if (s_GlazierType == null || s_ISleekElementType == null || s_ISleekButtonType == null) return;
                if (s_HeadersField == null || s_OnClicked == null) return;

                try { glazier = s_GlazierGet.Invoke(null, s_EmptyArgs); }
                catch (Exception e) { LogError("Glazier.Get() 调用失败: " + e); return; }
                if (glazier == null) { LogError("Glazier.Get() 返回 null！"); return; }
                LogInfo("Glazier.Get() 单例 OK");

                if (!s_CreateButtonResolved)
                {
                    Type instanceType = glazier.GetType();
                    LogInfo("Glazier 实例运行时类型: " + instanceType.FullName);
                    s_CreateButton = AccessTools.Method(instanceType, "CreateButton", new Type[0]);
                    if (s_CreateButton == null) { LogError("无法在 " + instanceType.FullName + " 上定位 CreateButton()！"); return; }
                    s_CreateButtonResolved = true;
                    LogInfo("CreateButton OK (来自 " + instanceType.Name + ")");
                }
            }

            // 循环注入 5 颗按钮：headers[0..4] -> page 2..6（SLOTS..PANTS 服装页）
            // v2.0.1：不再注入 STORAGE (headers[5])，因为 V2 协议不支持容器页整理。
            // DEV-V4-06：所有注入页统一 TIDY_POS_OFFSET_X（60×60 @ -130，Q54 冻结）。
            int injected = 0;
            for (int i = 0; i < HEADER_INJECT_COUNT; i++)
            {
                byte currentPage = (byte)(i + 2);

                object headerElement = headers.GetValue(i);
                if (headerElement == null)
                {
                    LogError($"headers[{i}] 为 null，跳过");
                    continue;
                }

                // DEV-V4-09 泵路径在册去重（skipTrackedPages=true）：已登记页不
                // 重复画（双按钮）；Q55 移除失败页引用保留=旧按钮仍在 UI 树，
                // 跳过正确、其拆除自愈仍走下次拆除重试。ctor 路径不去重（新
                // 仪表盘覆盖在册=旧行为）。
                if (skipTrackedPages && s_TidyButtons.ContainsKey(currentPage)) continue;

                // ── 创建整理按钮 B：[整理]（唯一按钮）──
                object tidyButton = null;
                Delegate tidyHandler = null;
                if (InjectButtonForTests != null)
                {
                    if (!InjectButtonForTests(currentPage, headerElement)) continue;
                }
                else
                {
                    try { tidyButton = s_CreateButton.Invoke(glazier, s_EmptyArgs); }
                    catch (Exception e) { LogError($"headers[{i}] tidyButton CreateButton 失败: {e}"); continue; }
                    if (tidyButton == null) { LogError($"headers[{i}] tidyButton 返回 null"); continue; }

                    try
                    {
                        s_PosScaleX  .SetValue(tidyButton, 1f,                 null);
                        s_PosOffsetX .SetValue(tidyButton, TIDY_POS_OFFSET_X,  null);
                        s_SizeOffsetX.SetValue(tidyButton, TIDY_SIZE_X,        null);
                        s_SizeOffsetY.SetValue(tidyButton, BTN_SIZE_Y,         null);
                        s_Text       .SetValue(tidyButton, "整理",             null);
                        s_TooltipText.SetValue(tidyButton, TOOLTIP_TIDY,       null);
                    }
                    catch (Exception e) { LogError($"headers[{i}] tidyButton 属性设置失败: {e}"); }

                    // 绑定整理按钮点击事件 -> HandleTidyClick(currentPage)
                    try
                    {
                        tidyHandler = CreatePageDelegate(s_OnClicked.EventHandlerType, currentPage);
                        s_OnClicked.AddEventHandler(tidyButton, tidyHandler);
                    }
                    catch (Exception e) { LogError($"headers[{i}] tidyButton 事件绑定失败: {e}"); }

                    // ── AddChild 到 header ──
                    try
                    {
                        s_OneArg[0] = tidyButton;
                        s_AddChild.Invoke(headerElement, s_OneArg);
                    }
                    catch (Exception e) { LogError($"headers[{i}] AddChild 失败: {e}"); }
                }

                // ── 在册（拆除责任登记：对象/回调/父容器）──
                s_TidyButtons[currentPage] = tidyButton;
                s_TidyClickHandlers[currentPage] = tidyHandler;
                s_TidyHeaders[currentPage] = headerElement;
                injected++;
                LogInfo($"headers[{i}] -> page {currentPage} 整理按钮注入 OK");
            }

            LogInfo($"==== 注入完成：共 {injected}/{HEADER_INJECT_COUNT} 颗整理按钮 ====");
        }

        // ─────────────────────────────────────────────────────────────────
        // 委托生成（Emit）：把 page 常量嵌入到 OnClicked 委托的调用链中。
        // ClickedButton 签名为 void(ISleekElement)，所以 DynamicMethod 接收一个参数。
        // ─────────────────────────────────────────────────────────────────
        private static Delegate CreatePageDelegate(Type delegateType, byte page)
        {
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            ParameterInfo[] parameters = invokeMethod.GetParameters();
            Type[] paramTypes = parameters.Length > 0
                ? new[] { parameters[0].ParameterType }
                : Type.EmptyTypes;

            var dm = new DynamicMethod(
                "TidyClick_" + page + "_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                null,
                paramTypes,
                typeof(InventoryTidyUiPatch));

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, (int)page);
            il.Emit(OpCodes.Call, typeof(InventoryTidyUiPatch).GetMethod("HandleTidyClick",
                BindingFlags.Static | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);

            return dm.CreateDelegate(delegateType);
        }

        // ─────────────────────────────────────────────────────────────────
        // 事件回调：整理按钮点击（唯一按钮）
        //   - Ctrl 按下 -> 按已保存的全局模式与方向整理全身（不含仓储栏）
        //   - 否则     -> 仅整理当前栏
        //
        //   DEV-V4-06：模式与方向不再读本类任何字典——点击处理前先经模块的
        //  RequestTidyFromUiClick 再确认生命周期可用性（Q54：不能只因「按钮
        //  曾被画出」），再由模块读同一 revision 的已保存 ClientPreference
        //  快照（Q59）。结果如实呈现（拒绝=原生回退/显式拒绝，不假成功）。
        // ─────────────────────────────────────────────────────────────────
        private static void HandleTidyClick(byte page)
        {
            Player player = Player.LocalPlayer;
            if (player?.inventory == null)
            {
                LogError("Player.LocalPlayer.inventory 为 null，忽略点击");
                return;
            }

            bool ctrl = InputEx.GetKey(KeyCode.LeftControl) || InputEx.GetKey(KeyCode.RightControl);

            try
            {
                var module = ActiveModule;
                if (module == null)
                {
                    LogError("无已装配的整理模块（未启动或已关闭），忽略点击");
                    return;
                }
                var result = module.RequestTidyFromUiClick(page, allPages: ctrl);
                LogRequestResult(ctrl ? "全身" : $"page {page}", result);
            }
            catch (Exception e)
            {
                LogError($"HandleTidyClick crashed: {e}");
            }
        }

        /// <summary>请求结果的低频诊断：每个显式枚举结果一行，不猜原因。</summary>
        private static void LogRequestResult(string scope, LitTidyRequestResult result)
        {
            switch (result)
            {
                case LitTidyRequestResult.Dispatched:
                    TidyDiagnosticLog.Info("ui-tidy-dispatched", $"[TidyUI] {scope} 整理请求已受理。");
                    break;
                case LitTidyRequestResult.NativeFallback:
                    TidyDiagnosticLog.Info("ui-tidy-native-fallback", $"[TidyUI] {scope} 整理被拒绝：功能当前不可用（生命周期非 Running 或已停止，原生回退）。");
                    break;
                case LitTidyRequestResult.RejectedFaultCircuit:
                    TidyDiagnosticLog.Info("ui-tidy-circuit-open", $"[TidyUI] {scope} 整理被拒绝：熔断已打开。");
                    break;
                case LitTidyRequestResult.RejectedNoSession:
                    TidyDiagnosticLog.Info("ui-tidy-no-session", $"[TidyUI] {scope} 整理被拒绝：尚未建立联机会话或未收到会话 challenge。");
                    break;
                case LitTidyRequestResult.RejectedSendFailed:
                    TidyDiagnosticLog.Info("ui-tidy-send-failed", $"[TidyUI] {scope} 整理请求发送失败（可靠通道未送达）。");
                    break;
                case LitTidyRequestResult.RejectedPreferenceUnavailable:
                    TidyDiagnosticLog.Info("ui-tidy-preference-unavailable", $"[TidyUI] {scope} 整理被拒绝：无法读取已保存的整理模式/方向快照（不发明未保存过的组合）。");
                    break;
                default:
                    TidyDiagnosticLog.Info("ui-tidy-queue-closed", $"[TidyUI] {scope} 整理被拒绝：主线程队列已关闭。");
                    break;
            }
        }
    }
}
