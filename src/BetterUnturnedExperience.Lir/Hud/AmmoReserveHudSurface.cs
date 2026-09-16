using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-06 呈现面（真机 only——宿主测试经 ApplyForTests/HideAllForTests
    /// 替换，本类方法体在宿主永不执行；infoBox 为 vanilla 私有字段，反射读取，
    /// 其余用编译期 Glazier 类型（BueNativeManagementPanel 同面先例）。
    ///
    /// 注入形态 = 「只扩展原版弹药信息区」的字面兑现：把读数标签作为
    /// vanilla UseableGun.infoBox 的子元素放进右下弹药盒左列下半
    /// （ammoLabel 大字号占左列；右列被 firemode/attach 两行占据——几何单源
    /// 见下常量，LHT 顶栏条与右列两行互不相干、零争抢）。随 infoBox 同创建
    /// 同销毁：卸枪/切枪原版 RemoveChild 带走子元素，呈现无跨面残留；功能
    /// 停用 = 补丁注销 + HideAll 即时收（在枪上的残留读数不滞留）。
    /// 原版 ammoLabel 文本一字不动（当前/上限含义保持）。
    /// 写纪律：Text 仅在数值变化时写、IsVisible 仅在翻转时写（稳态零写零日志）。
    /// </summary>
    internal static class AmmoReserveHudSurface
    {
        // ── 几何/样式单源（红测不复算坐标；真机微调只动这里）──
        internal const float ReserveLabelPositionScaleX = 0f;
        internal const float ReserveLabelPositionScaleY = 0.5f;
        internal const float ReserveLabelSizeScaleX = 0.35f;
        internal const float ReserveLabelSizeScaleY = 0.5f;
        internal const string InfoBoxFieldName = "infoBox";

        private sealed class Slot
        {
            internal ISleekLabel label;
            internal string lastText;
            internal bool visible;
        }

        private static readonly object gate = new object();
        private static readonly ConditionalWeakTable<object, Slot> slots = new ConditionalWeakTable<object, Slot>();
        private static readonly List<WeakReference<Slot>> live = new List<WeakReference<Slot>>();
        private static FieldInfo infoBoxField;      // lazy（首次 Apply 真机解析）
        private static bool reflectionProbed;
        private static bool reflectionBrokenLogged;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Apply(object gun, AmmoReserveResult result)
        {
            if (gun == null) return;
            try
            {
                lock (gate)
                {
                    if (!EnsureReflection()) return;
                    object box;
                    try { box = infoBoxField.GetValue(gun); }
                    catch (Exception) { return; }
                    var parent = box as ISleekElement;
                    if (parent == null) return; // 原版弹药区不在场（未装备/未创建）——不画、不抛

                    Slot slot;
                    if (!slots.TryGetValue(gun, out slot))
                    {
                        slot = CreateSlot(parent);
                        if (slot == null) return;
                        slots.Add(gun, slot);
                        live.Add(new WeakReference<Slot>(slot));
                        // 摊销清死引用：WeakReference 槽位本身不弱（长会话高频
                        // 换枪下 live 只增不减 = 真实增长），每次新注入顺带回收。
                        for (var d = live.Count - 1; d >= 0; d--)
                        {
                            Slot dead;
                            if (!live[d].TryGetTarget(out dead)) live.RemoveAt(d);
                        }
                    }
                    if (slot.label == null) return;

                    if (slot.lastText != result.LabelText)
                    {
                        slot.label.Text = result.LabelText;
                        slot.lastText = result.LabelText;
                    }
                    if (!slot.visible)
                    {
                        slot.label.IsVisible = true;
                        slot.visible = true;
                    }
                }
            }
            catch (Exception error)
            {
                // 呈现失败绝不打断原版 updateInfo 链（HUD 是附加读数，非关键路径）。
                LirRuntime.LogDiagnostic("[AmmoHud] 呈现异常（本次吞掉，原版读数不受影响）: " + error.Message);
            }
        }

        /// <summary>功能注销 = 收掉所有已注入标签（登记=唯一开关的反操作）。</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HideAll()
        {
            lock (gate)
            {
                if (live.Count == 0) return; // 从未注入（headless/宿主）——零反射零日志
                for (var i = 0; i < live.Count; i++)
                {
                    Slot slot;
                    if (!live[i].TryGetTarget(out slot) || slot == null || slot.label == null) continue;
                    try
                    {
                        slot.label.IsVisible = false;
                        slot.visible = false;
                        slot.lastText = null;
                    }
                    catch (Exception) { }
                }
                live.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool EnsureReflection()
        {
            if (reflectionProbed) return infoBoxField != null;
            reflectionProbed = true;
            infoBoxField = AccessTools.Field(typeof(UseableGun), InfoBoxFieldName);
            if (infoBoxField == null && !reflectionBrokenLogged)
            {
                reflectionBrokenLogged = true;
                LirRuntime.LogError("[AmmoHud] 原版弹药盒字段不可定位（版本漂移？）——后备 HUD 停用，压弹功能不受影响");
            }
            return infoBoxField != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Slot CreateSlot(ISleekElement infoBox)
        {
            try
            {
                var label = Glazier.Get().CreateLabel();
                label.PositionScale_X = ReserveLabelPositionScaleX;
                label.PositionScale_Y = ReserveLabelPositionScaleY;
                label.SizeScale_X = ReserveLabelSizeScaleX;
                label.SizeScale_Y = ReserveLabelSizeScaleY;
                label.FontSize = ESleekFontSize.Small;
                label.IsVisible = false;
                infoBox.AddChild(label);
                return new Slot { label = label };
            }
            catch (Exception error)
            {
                if (!reflectionBrokenLogged)
                {
                    reflectionBrokenLogged = true;
                    LirRuntime.LogError("[AmmoHud] Glazier 标签注入失败（后备 HUD 停用）: " + error.Message);
                }
                return null;
            }
        }
    }
}
