using System;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: PlayerLifeUI HUD 注入器（旧 PlayerLifeUIPatches 原样迁移，
    /// v1.1.3 反射模式）。数据与渲染完全分离：构造函数 Postfix 创建
    /// ISleekLabel（默认隐藏），渲染刷新由 HordePresentationAdapter 10Hz 驱动。
    ///
    /// 反射模式关键规范（LaunchInventoryTidy 固化经验）：
    /// - 必须使用 AccessTools.TypeByName 跨 DLL 检索 Glazier / ISleekLabel / ISleekElement，
    ///   严禁 Type.GetType（跨 Assembly 返回 null）
    /// - 必须使用 GetInterfaceProperty 递归穿透接口继承链查找属性
    /// - CreateLabel 推迟到运行时从 glazier 实例类型解析（Glazier.Get 返回子类实例）
    ///
    /// 位置排版（v1.1.3）：800x35 @ 屏幕水平居中、指南针下方（PositionOffset_Y=80），
    /// MiddleCenter；跳过 TextColor（SleekColor/ESleekTint 反射类型不匹配陷阱），
    /// 纯靠富文本控制颜色。
    ///
    /// 生命周期保护：监听 Provider.onClientDisconnected，玩家退出时经
    /// DrainClientDisconnectReset（主线程派发）销毁缓存；下次 PlayerLifeUI 重建
    /// 时构造函数 Postfix 自动再次注入。日志走 LhtRuntime（BepInEx Logger 随旧
    /// 插件身份消失）。主线程断言随 LIT/LIR 先例去除（HostTick 链即主线程纪律）。
    /// </summary>
    [HarmonyPatch(typeof(PlayerLifeUI))]
    internal static class PlayerLifeUiHudPatches
    {
        private const string TAG = "[HordeTrackerUI]";

        // ── 反射缓存：类型 ──
        private static Type s_GlazierType;
        private static Type s_ISleekElementType;
        private static Type s_ISleekLabelType;
        private static bool s_TypesResolved;

        // ── 反射缓存：方法 ──
        private static MethodInfo s_GlazierGet;
        private static MethodInfo s_CreateLabel;
        private static bool s_CreateLabelResolved;
        private static MethodInfo s_AddChild;

        // ── 反射缓存：布局属性（ISleekElement）──
        private static PropertyInfo s_PosScaleX;
        private static PropertyInfo s_PosOffsetX;
        private static PropertyInfo s_PosScaleY;
        private static PropertyInfo s_PosOffsetY;
        private static PropertyInfo s_SizeOffsetX;
        private static PropertyInfo s_SizeOffsetY;

        // ── 反射缓存：标签属性（ISleekLabel）──
        private static PropertyInfo s_Text;
        private static PropertyInfo s_FontSize;
        private static PropertyInfo s_IsVisible;
        private static PropertyInfo s_AllowRichText;
        private static PropertyInfo s_TextAlignment;
        private static PropertyInfo s_TextContrastContext;

        // ── 反射缓存是否完整 ──
        private static bool s_ReflectionReady;

        // ── Label 实例 + 已创建标记 ──
        private static object _hordeLabel;
        private static bool _labelCreated;
        private static bool _disconnectHooked;
        // coalesced flag 替代 ConcurrentQueue：1=有待处理的 disconnect 重置。
        private static int _disconnectResetPending;

        // ─────────────────────────────────────────────────────────────────
        // 反射预热（首次调用时执行，幂等）
        // ─────────────────────────────────────────────────────────────────
        private static void WarmupReflection()
        {
            if (s_TypesResolved) return;
            s_TypesResolved = true;

            try
            {
                s_GlazierType = AccessTools.TypeByName("SDG.Unturned.Glazier");
                if (s_GlazierType == null) { LogError("无法定位 SDG.Unturned.Glazier 类型"); return; }

                s_ISleekElementType = AccessTools.TypeByName("SDG.Unturned.ISleekElement");
                if (s_ISleekElementType == null) { LogError("无法定位 SDG.Unturned.ISleekElement 类型"); return; }

                s_ISleekLabelType = AccessTools.TypeByName("SDG.Unturned.ISleekLabel");
                if (s_ISleekLabelType == null) { LogError("无法定位 SDG.Unturned.ISleekLabel 类型"); return; }

                s_GlazierGet = s_GlazierType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public);
                if (s_GlazierGet == null) { LogError("无法定位 Glazier.Get() 静态方法"); return; }

                s_AddChild = GetInterfaceMethod(s_ISleekElementType, "AddChild");
                if (s_AddChild == null) { LogError("无法定位 ISleekElement.AddChild 方法"); return; }

                s_PosScaleX  = GetInterfaceProperty(s_ISleekElementType, "PositionScale_X");
                s_PosOffsetX = GetInterfaceProperty(s_ISleekElementType, "PositionOffset_X");
                s_PosScaleY  = GetInterfaceProperty(s_ISleekElementType, "PositionScale_Y");
                s_PosOffsetY = GetInterfaceProperty(s_ISleekElementType, "PositionOffset_Y");
                s_SizeOffsetX = GetInterfaceProperty(s_ISleekElementType, "SizeOffset_X");
                s_SizeOffsetY = GetInterfaceProperty(s_ISleekElementType, "SizeOffset_Y");
                if (s_PosScaleX == null || s_PosOffsetX == null || s_PosScaleY == null ||
                    s_PosOffsetY == null || s_SizeOffsetX == null || s_SizeOffsetY == null)
                {
                    LogError("ISleekElement 布局属性定位失败");
                    DumpAvailableMembers(s_ISleekElementType, "ISleekElement");
                    return;
                }

                s_Text                = GetInterfaceProperty(s_ISleekLabelType, "Text");
                s_FontSize            = GetInterfaceProperty(s_ISleekLabelType, "FontSize");
                s_IsVisible           = GetInterfaceProperty(s_ISleekLabelType, "IsVisible");
                s_AllowRichText       = GetInterfaceProperty(s_ISleekLabelType, "AllowRichText");
                s_TextAlignment       = GetInterfaceProperty(s_ISleekLabelType, "TextAlignment");
                s_TextContrastContext = GetInterfaceProperty(s_ISleekLabelType, "TextContrastContext");
                if (s_Text == null || s_IsVisible == null)
                {
                    LogError("ISleekLabel Text / IsVisible 属性定位失败");
                    DumpAvailableMembers(s_ISleekLabelType, "ISleekLabel");
                    return;
                }

                s_ReflectionReady = true;
                LogInfo("反射缓存预热成功：Glazier / ISleekElement / ISleekLabel 全部就绪");
            }
            catch (Exception e)
            {
                LogError("WarmupReflection 异常: " + e);
            }
        }

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
                sb.AppendLine("  [Methods]");
                foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    sb.AppendLine($"    {m.Name}");
                foreach (var parent in type.GetInterfaces())
                    foreach (var m in parent.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        sb.AppendLine($"    [{parent.Name}] {m.Name}");
                LogInfo(sb.ToString());
            }
            catch (Exception e) { LogWarning("成员 dump 失败: " + e.Message); }
        }

        // ─────────────────────────────────────────────────────────────────
        // Postfix：PlayerLifeUI 构造完成后注入 ISleekLabel
        // ─────────────────────────────────────────────────────────────────
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        private static void ConstructorPostfix()
        {
            WarmupReflection();
            if (!s_ReflectionReady)
            {
                LogError("反射缓存未就绪，跳过 HUD 注入");
                return;
            }

            try
            {
                SleekFullscreenBox container = PlayerLifeUI.container;
                if (container == null)
                {
                    LogError("PlayerLifeUI.container 为 null");
                    return;
                }

                // 已创建且实例仍有效 -> 跳过
                if (_labelCreated && _hordeLabel != null) return;

                object glazier = s_GlazierGet.Invoke(null, Array.Empty<object>());
                if (glazier == null) { LogError("Glazier.Get() 返回 null"); return; }

                if (!s_CreateLabelResolved)
                {
                    Type instanceType = glazier.GetType();
                    s_CreateLabel = AccessTools.Method(instanceType, "CreateLabel", new Type[0]);
                    if (s_CreateLabel == null)
                    {
                        LogError("无法在 " + instanceType.FullName + " 上定位 CreateLabel()");
                        return;
                    }
                    s_CreateLabelResolved = true;
                    LogInfo("CreateLabel 方法 OK (来自 " + instanceType.Name + ")");
                }

                object label = s_CreateLabel.Invoke(glazier, Array.Empty<object>());
                if (label == null) { LogError("CreateLabel 返回 null"); return; }

                // 800x35 + MiddleCenter：文本中心对齐屏幕中心（v1.1.3 修正）
                s_PosScaleX .SetValue(label, 0.5f,  null);
                s_PosOffsetX.SetValue(label, -400f, null);
                s_PosScaleY .SetValue(label, 0f,    null);
                s_PosOffsetY.SetValue(label, 80f,   null);
                s_SizeOffsetX.SetValue(label, 800f, null);
                s_SizeOffsetY.SetValue(label, 35f,  null);

                // 跳过 TextColor（SleekColor/ESleekTint 反射类型不匹配陷阱），纯富文本上色
                SetValueSafe(s_AllowRichText,       label, true);
                SetValueSafe(s_FontSize,            label, ESleekFontSize.Medium);
                SetValueSafe(s_TextAlignment,       label, TextAnchor.MiddleCenter);
                SetValueSafe(s_TextContrastContext, label, ETextContrastContext.ColorfulBackdrop);
                SetValueSafe(s_Text,                label, string.Empty);
                SetValueSafe(s_IsVisible,           label, false);

                s_AddChild.Invoke(container, new object[] { label });

                _hordeLabel = label;
                _labelCreated = true;

                EnsureDisconnectHook();

                LogInfo("HUD ISleekLabel 已注入 PlayerLifeUI.container (800x35 @ MiddleCenter)");
            }
            catch (Exception e)
            {
                LogError("ConstructorPostfix 异常: " + e);
            }
        }

        private static void SetValueSafe(PropertyInfo prop, object target, object value)
        {
            if (prop == null) return;
            try { prop.SetValue(target, value, null); }
            catch (Exception e) { LogError($"属性 {prop.Name} 设置失败: {e.Message}"); }
        }

        private static void EnsureDisconnectHook()
        {
            if (_disconnectHooked) return;
            try
            {
                Provider.onClientDisconnected += OnClientDisconnected;
                _disconnectHooked = true;
                LogInfo("已订阅 Provider.onClientDisconnected（Label 生命周期保护）");
            }
            catch (Exception e)
            {
                LogError("订阅 onClientDisconnected 失败: " + e.Message);
            }
        }

        private static void OnClientDisconnected()
        {
            // Provider.onClientDisconnected 调度线程不可假设，仅原子置位标志；
            // 实际清理由宿主帧链 DrainClientDisconnectReset 在主线程执行。
            System.Threading.Interlocked.Exchange(ref _disconnectResetPending, 1);
        }

        /// <summary>宿主帧链主线程派发：Label 缓存清理 + HordeStateTracker 清空。幂等。</summary>
        internal static void DrainClientDisconnectReset()
        {
            if (System.Threading.Interlocked.Exchange(ref _disconnectResetPending, 0) == 0) return;
            try
            {
                // Label 随 PlayerLifeUI 一起由 vanilla 销毁，这里只重置缓存
                _hordeLabel = null;
                _labelCreated = false;
                HordeStateTracker.Clear();
                LogInfo("onClientDisconnected（主线程派发）：Label 缓存已重置 + HordeStateTracker 已清空");
            }
            catch (Exception e)
            {
                LogError("DrainClientDisconnectReset 执行异常: " + e.Message);
            }
        }

        /// <summary>代际边界调用：解绑 disconnect 钩子并清缓存。</summary>
        internal static void Cleanup()
        {
            if (_disconnectHooked)
            {
                try { Provider.onClientDisconnected -= OnClientDisconnected; }
                catch (Exception e) { LogWarning("退订 onClientDisconnected 失败: " + e.Message); }
                _disconnectHooked = false;
            }
            _hordeLabel = null;
            _labelCreated = false;
        }

        // ─────────────────────────────────────────────────────────────────
        // 表现层使用的反射辅助：设置 Text + IsVisible
        // ─────────────────────────────────────────────────────────────────
        internal static void SetText(string text)
        {
            if (_hordeLabel == null || s_Text == null) return;
            try { s_Text.SetValue(_hordeLabel, text, null); }
            catch (Exception e) { LogError("SetText 失败: " + e.Message); }
        }

        internal static void SetVisible(bool visible)
        {
            if (_hordeLabel == null || s_IsVisible == null) return;
            try { s_IsVisible.SetValue(_hordeLabel, visible, null); }
            catch (Exception e) { LogError("SetVisible 失败: " + e.Message); }
        }

        internal static bool IsLabelReady()
        {
            return s_ReflectionReady && _labelCreated && _hordeLabel != null;
        }

        private static void LogInfo(string msg)  => LhtRuntime.LogInfo($"{TAG} {msg}");
        private static void LogWarning(string msg) => LhtRuntime.LogWarning($"{TAG} {msg}");
        private static void LogError(string msg) => LhtRuntime.LogError($"{TAG} {msg}");
    }
}
