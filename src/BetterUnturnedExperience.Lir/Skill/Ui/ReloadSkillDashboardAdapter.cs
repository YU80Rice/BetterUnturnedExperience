using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 表面 A 的登记闸 + 编排（06 同律：登记=唯一开关——Start 武装
    /// 分区面=会画、Stop 注销+清节点=不画；补丁体与闸内零功能 bool，红测 5e
    /// 反射钉）。postfix 每拍重建原版行带后回调这里：三重闸（在册 ∧ 等级已获
    /// 主机确认 ∧ 战斗专精页）全开才注入；行内容=ReloadSkillSectionModel 纯
    /// 投影（等级阶梯 0/1/2 + 至多一颗下一级升级按钮，无三级无占位）；按钮
    /// 点击 → 主机升级路径（与功能 B 双击同一道权威门，扣原版经验）。
    /// 引擎接触（Glazier 节点、原版容器、本机余额读）全部 NoInlining 薄方法
    /// ——宿主测试进程不进方法体（04 教训），注入经 RenderForTests 缝替换，
    /// 真机呈现=具名接缝缺口（06 同界）随 DEV-V5-08。
    /// </summary>
    internal static class ReloadSkillDashboardAdapter
    {
        /// <summary>行注入缝（真机 = 原版技能页容器挂载；测试 = 记录器）。</summary>
        internal static Action<IReadOnlyList<ReloadSkillSectionRow>> RenderForTests;

        /// <summary>本机原版经验余额读缝（真机 = PlayerSkills.experience；测试 = 手拨）。</summary>
        internal static Func<uint> LocalExperienceForTests;

        private static bool sectionRegistered;

        /// <summary>分区面在册位（登记=唯一开关；名字避开 Enabled/Disabled 族，红测反射钉反证）。</summary>
        internal static bool SectionRegistered { get { return sectionRegistered; } }

        /// <summary>模块 InstallPatches 的分区面登记（绑定已由 binder 显式完成）。</summary>
        internal static void Register()
        {
            sectionRegistered = true;
            LirRuntime.LogInfo("[ReloadSkill] 换弹技能分区已登记（U 菜单战斗区下方）");
        }

        /// <summary>模块 UninstallPatches/互撤 的分区面注销：在册=假 → 不画。</summary>
        internal static void Unregister()
        {
            if (!sectionRegistered) return;
            sectionRegistered = false;
            try
            {
                var seam = RenderForTests;
                if (seam != null) seam(null); // 测试缝：null = 收（记录器清账）
                else ClearRealSection();       // 真机：移除已注入节点
            }
            catch (Exception error)
            {
                LirRuntime.LogError("[ReloadSkill] 分区注销清理异常（登记已收回）: " + error.Message);
            }
            LirRuntime.LogInfo("[ReloadSkill] 换弹技能分区已注销（原生回退）");
        }

        /// <summary>三重闸的纯判据（宿主可证）：在册 ∧ 已获主机确认等级 ∧（调用侧过滤战斗页）。</summary>
        internal static bool ShouldRenderSection()
        {
            return sectionRegistered && ReloadSkillLevelMirror.HasConfirmed;
        }

        /// <summary>postfix 入口（原版 updateSelection 之后）。任何异常吞进诊断——
        /// 绝不打断原版技能页重建。</summary>
        internal static void OnSkillsSectionRebuilt(byte specialityIndex)
        {
            try
            {
                if (specialityIndex != 0) return; // 分区只属战斗页（T7：战斗区下方）
                if (!ShouldRenderSection()) return;
                var level = ReloadSkillLevelMirror.ConfirmedLevel;
                var seamXp = LocalExperienceForTests;
                var xp = seamXp != null ? seamXp() : ReadLocalExperience();
                var rows = ReloadSkillSectionModel.BuildRows(level, xp);
                var seamRender = RenderForTests;
                if (seamRender != null)
                {
                    seamRender(rows);
                    return;
                }
                RenderReal(rows);
            }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[ReloadSkill] 分区重建异常（本次吞掉，原版技能页不受影响）: " + error.Message);
            }
        }

        /// <summary>等级确认后的即时重建：有测试缝=直接走投影记录；真机=回调
        /// 原版 updateSelection（引擎薄面，异常由调用方吞进诊断）。</summary>
        internal static void RequestRebuild()
        {
            if (RenderForTests != null)
            {
                OnSkillsSectionRebuilt(0);
                return;
            }
            RebuildReal();
        }

        // ── 引擎薄面（NoInlining；宿主测试进程不进方法体）──

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RebuildReal()
        {
            var target = ReloadSkillDashboardBinder.ResolveSectionHostTarget();
            var selected = typeof(PlayerDashboardSkillsUI)
                .GetField("selectedSpeciality", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(null);
            target.Invoke(null, new object[] { selected is byte b ? b : (byte)0 });
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static uint ReadLocalExperience()
        {
            try
            {
                var local = Player.LocalPlayer;
                return local == null || local.skills == null ? 0u : local.skills.experience;
            }
            catch (Exception)
            {
                return 0u; // 余额读不出 = 按钮禁用态（不弹假承诺）
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RenderReal(IReadOnlyList<ReloadSkillSectionRow> rows)
        {
            // 真机注入：原版技能页的私有容器（skillsScrollBox）反射读取、节点
            // 重建幂等（先清后画）、几何单源常量——实机随 DEV-V5-08 验收。
            ReloadSkillDashboardSurface.Render(rows);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ClearRealSection()
        {
            ReloadSkillDashboardSurface.Clear();
        }
    }

    /// <summary>
    /// DEV-V5-07 表面 A 的呈现薄壳（真机 only；06 AmmoReserveHudSurface 同律）。
    /// 注入形态 = 「U 菜单战斗区下方追加分区」的字面兑现：把分区作为原版
    /// skillsScrollBox 的子节点排在原版行带之后，并扩展内容高（原版只按 22
    /// 槽行高定滚域）。先清后画（updateSelection 每次全量重建触发）；
    /// 升级按钮点击 → InPlaceReloadModule.HandleSkillUpgradeRequest（主机门）。
    /// </summary>
    internal static class ReloadSkillDashboardSurface
    {
        // ── 几何单源（真机微调只动这里）──
        private const int RowHeight = 36;
        private const int ButtonHeight = 40;
        private const int SectionTopGap = 8;
        private const float SectionWidthScale = 1f;

        private static readonly object gate = new object();
        private static System.Reflection.FieldInfo scrollField;
        private static System.Reflection.FieldInfo skillCountField;
        private static bool reflectionProbed;
        private static ISleekElement injectedBox;

        internal static void Render(IReadOnlyList<ReloadSkillSectionRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            lock (gate)
            {
                if (!EnsureReflection()) return;
                var scroll = scrollField.GetValue(null) as ISleekScrollView;
                if (scroll == null) return;
                ClearLocked();
                var skills = skillCountField.GetValue(null) as Array;
                var vanillaRows = skills == null ? 0 : skills.Length;
                var box = Glazier.Get().CreateBox();
                box.PositionOffset_X = 0;
                box.PositionOffset_Y = vanillaRows * 90 + SectionTopGap; // 原版行带（90px/行）之下
                box.SizeScale_X = SectionWidthScale;
                box.SizeOffset_Y = rows.Count * RowHeight + ButtonHeight + SectionTopGap;
                var y = 0;
                for (var i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row.IsButton)
                    {
                        var button = Glazier.Get().CreateButton();
                        button.PositionOffset_Y = y;
                        button.SizeOffset_Y = ButtonHeight;
                        button.SizeScale_X = 0.5f;
                        button.Text = row.Text;
                        var targetLevel = row.TargetLevel;
                        if (row.Enabled && targetLevel > 0)
                        {
                            button.OnClicked += _ => RequestUpgrade(targetLevel);
                        }
                        box.AddChild(button);
                        y += ButtonHeight;
                        continue;
                    }
                    var label = Glazier.Get().CreateLabel();
                    label.PositionOffset_Y = y;
                    label.SizeOffset_Y = RowHeight;
                    label.SizeScale_X = 1f;
                    label.Text = row.Text;
                    label.FontSize = ESleekFontSize.Medium;
                    box.AddChild(label);
                    y += RowHeight;
                }
                scroll.AddChild(box);
                injectedBox = box;
                var size = scroll.ContentSizeOffset;
                size.y = vanillaRows * 90 + SectionTopGap + y;
                scroll.ContentSizeOffset = size;
            }
        }

        internal static void Clear()
        {
            lock (gate)
            {
                try
                {
                    if (scrollField == null) { injectedBox = null; return; }
                    var scroll = scrollField.GetValue(null) as ISleekScrollView;
                    ClearLocked();
                    if (scroll != null)
                    {
                        var skills = skillCountField.GetValue(null) as Array;
                        var size = scroll.ContentSizeOffset;
                        size.y = (skills == null ? 0 : skills.Length) * 90 - 10; // 交还原版内容高
                        scroll.ContentSizeOffset = size;
                    }
                }
                catch (Exception error)
                {
                    LirRuntime.LogDiagnostic("[ReloadSkill] 分区清节点异常: " + error.Message);
                }
            }
        }

        private static void ClearLocked()
        {
            if (injectedBox == null) return;
            try
            {
                var scroll = scrollField.GetValue(null) as ISleekElement;
                scroll?.RemoveChild(injectedBox);
            }
            catch (Exception) { }
            injectedBox = null;
        }

        private static void RequestUpgrade(byte targetLevel)
        {
            var module = InPlaceReloadModule.ActiveModule;
            if (module == null) return; // 补丁活着但模块没了——不请求
            module.HandleSkillUpgradeRequest(targetLevel);
        }

        private static bool EnsureReflection()
        {
            if (reflectionProbed) return scrollField != null && skillCountField != null;
            reflectionProbed = true;
            scrollField = AccessTools_Static_Field(typeof(PlayerDashboardSkillsUI), "skillsScrollBox");
            skillCountField = AccessTools_Static_Field(typeof(PlayerDashboardSkillsUI), "skills");
            if (scrollField == null || skillCountField == null)
            {
                LirRuntime.LogError("[ReloadSkill] 原版技能页容器字段不可定位（版本漂移？）——分区停用");
            }
            return scrollField != null && skillCountField != null;
        }

        private static System.Reflection.FieldInfo AccessTools_Static_Field(Type type, string name)
        {
            return type.GetField(name, System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        }
    }
}
