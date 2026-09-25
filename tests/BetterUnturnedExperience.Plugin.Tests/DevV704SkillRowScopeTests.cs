using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Lir;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V7-04 red-first coverage: scoped skill progress and the vanilla-row
    /// projection must be independent from the passive scheduler and UI runtime.
    /// </summary>
    internal static class DevV704SkillRowScopeTests
    {
        internal static void Run(bool collectAllFailures = false)
        {
            var failures = new List<string>();
            void Check(bool condition, string message)
            {
                if (condition) return;
                if (collectAllFailures) failures.Add(message);
                else throw new InvalidOperationException(message);
            }

            Group("作用域与旧账隔离", failures, collectAllFailures, () => ScopeAndLegacy(Check));
            Group("原版单行投影", failures, collectAllFailures, () => VanillaRow(Check));
            Group("原版技能行视觉结构", failures, collectAllFailures, () => VisualLayout(Check));
            Group("表面隔离与依赖边界", failures, collectAllFailures, () => SurfaceBoundary(Check));

            Console.WriteLine("DEV-V7-04 skill-row-scope tests: "
                + (failures.Count == 0 ? "PASS" : "FAIL"));
            if (failures.Count != 0)
                throw new InvalidOperationException("DEV-V7-04 failures ("
                    + failures.Count + "): " + string.Join(" || ", failures));
        }

        private static void Group(string name, List<string> failures, bool collect,
            Action body)
        {
            try { body(); }
            catch (Exception error) when (collect)
            {
                failures.Add("[" + name + "] " + error.Message);
            }
        }

        private static void ScopeAndLegacy(Action<bool, string> check)
        {
            var firstMap = new ReloadSkillScopeKey("TestServer", 76561197960265729UL, 0, "PEI");
            var secondMap = new ReloadSkillScopeKey("TestServer", 76561197960265729UL, 0, "Washington");
            var secondSlot = new ReloadSkillScopeKey("TestServer", 76561197960265729UL, 1, "PEI");
            var otherPlayer = new ReloadSkillScopeKey("TestServer", 76561197960265730UL, 0, "PEI");
            var store = new ReloadSkillStore();

            check(store.TrySetLevel(firstMap, 2), "作用域账应允许写入 2 级");
            check(store.GetLevel(firstMap) == 2, "同一世界/槽/玩家应读回等级");
            check(store.GetLevel(secondMap) == 0, "换地图必须从 0 级开始");
            check(store.GetLevel(secondSlot) == 0, "换角色槽不得串账");
            check(store.GetLevel(otherPlayer) == 0, "同服不同玩家不得串账");

            check(typeof(ReloadSkillFilePersistence).GetField("FileName",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) == null,
                "生产持久化不得继续暴露旧全局技能文件");
        }

        private static void VanillaRow(Action<bool, string> check)
        {
            var rows = ReloadSkillSectionModel.BuildRows(0,
                (uint)ReloadSkillPolicy.XpCostLevel0To1);
            check(rows.Count == 1, "技能投影必须是一条完整原版风格行");
            var row = rows[0];
            check(row.Name == ReloadSkillPolicy.SkillSectionTitle,
                "单行必须有换弹技能名称");
            check(row.LevelText.Contains("0") && row.LevelText.Contains("2"),
                "单行必须同时表达当前等级与满级");
            check(row.DescriptionText == ReloadSkillPolicy.LevelDescription(0),
                "0 级描述必须来自单一策略源");
            check(row.CostText.Contains("125"), "0 级单行必须显示 125 经验花费");
            check(row.IsClickable && row.TargetLevel == 1,
                "经验足够时整行必须可点击升级");
            check(row.LockCount == 3 && row.UnlockedCount == 0,
                "单行必须有三格锁条");
            check(row.Height == 80 && row.Step == 90,
                "单行必须使用原版约 80 高/90 步进");

            var poor = ReloadSkillSectionModel.BuildRows(0,
                (uint)ReloadSkillPolicy.XpCostLevel0To1 - 1);
            check(!poor[0].IsClickable, "经验不足时整行必须不可点击");
            var full = ReloadSkillSectionModel.BuildRows(ReloadSkillPolicy.MaxSkillLevel,
                uint.MaxValue)[0];
            check(full.IsFull && full.CostText == "Full" && full.TargetLevel == 0,
                "2 级必须显示 Full 且不得产生三级目标");
            check(!ReloadSkillPolicy.LevelDescription(2).Contains("再等一轮"),
                "2 级描述不得保留旧的再等一轮语义");
        }

        private static void VisualLayout(Action<bool, string> check)
        {
            var layoutType = typeof(ReloadSkillDashboardSurface).Assembly.GetType(
                "BetterUnturnedExperience.Lir.ReloadSkillDashboardLayout");
            check(layoutType != null, "战斗区必须暴露可测试且由真实 Render 消费的布局 seam");
            if (layoutType == null) return;
            var create = layoutType.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
            check(create != null, "布局 seam 必须能按原版行数生成布局");
            if (create == null) return;
            var rows = create.Invoke(null, new object[] { 7, 1 }) as System.Collections.IEnumerable;
            check(rows != null, "布局 seam 必须返回行记录列表");
            if (rows == null) return;
            var twoRows = create.Invoke(null, new object[] { 7, 2 }) as System.Collections.IEnumerable;
            check(twoRows != null, "布局 seam 必须支持多行原版步进");
            if (twoRows != null)
            {
                var second = twoRows.GetEnumerator();
                second.MoveNext();
                second.MoveNext();
                var secondLayout = second.Current;
                object ReadSecond(string property)
                {
                    return secondLayout.GetType().GetProperty(property,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(secondLayout);
                }
                check(Convert.ToInt32(ReadSecond("PositionY")) == 720,
                    "第二行必须按原版 90px 步进，不得重复或插入顶隙");
                check(Convert.ToInt32(ReadSecond("ContentHeightAfterRender")) == 800
                    && Convert.ToInt32(ReadSecond("ContentHeightAfterClear")) == 620,
                    "多行追加后的内容高度必须仍按原版网格计算");
            }

            var row = rows.GetEnumerator();
            if (!row.MoveNext())
            {
                check(false, "新增技能布局必须产生一行");
                return;
            }
            var rowLayout = row.Current;
            object Read(string property)
            {
                return rowLayout.GetType().GetProperty(property,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(rowLayout);
            }
            check(Convert.ToInt32(Read("PositionY")) == 630,
                "技能行必须紧接原版第 7 行，不能额外增加顶隙");
            check(Convert.ToInt32(Read("Height")) == 80 && Convert.ToSingle(Read("WidthScale")) == 1f,
                "行根必须按原版 80 高并横向铺满");
            check(Convert.ToSingle(Read("ButtonWidthScale")) == 1f
                && Convert.ToSingle(Read("ButtonHeightScale")) == 1f,
                "整行按钮必须覆盖 80 高行根");
            check(string.Equals(Read("NameAlignment")?.ToString(), "UpperLeft", StringComparison.Ordinal)
                && string.Equals(Read("DescriptionAlignment")?.ToString(), "LowerLeft", StringComparison.Ordinal)
                && string.Equals(Read("CostAlignment")?.ToString(), "LowerRight", StringComparison.Ordinal),
                "三类文本必须使用原版左上、左下、右下对齐");
            check(Convert.ToBoolean(Read("ChildrenUseRowRoot")),
                "文本与锁条必须挂在各自的 80 高行根，不得挂在额外分区盒");
            check(Convert.ToInt32(Read("LockParentHeight")) == 80
                && Convert.ToInt32(Read("FirstLockX")) == -20
                && Convert.ToInt32(Read("LockY")) == 10
                && Convert.ToSingle(Read("LockHeightScale")) == 0.5f,
                "锁条必须相对 80 高行根使用原版右侧半高定位");
            check(Convert.ToInt32(Read("ContentHeightAfterRender")) == 710
                && Convert.ToInt32(Read("ContentHeightAfterClear")) == 620,
                "追加和清除技能行必须恢复原版滚动内容高度公式");
        }

        private static void SurfaceBoundary(Action<bool, string> check)
        {
            check(typeof(ReloadSkillDashboardAdapter).GetMethod("RenderReal",
                BindingFlags.Static | BindingFlags.NonPublic) != null,
                "战斗区必须保留真实呈现适配器");
            check(typeof(ReloadSkillDashboardSurface).GetMethod("RequestUpgrade",
                BindingFlags.Static | BindingFlags.NonPublic) != null,
                "战斗区整行点击必须共用主机升级入口");
            check(typeof(ReloadSkillSettingsSurface).GetMethod("BuildFallbackProjection",
                BindingFlags.Static | BindingFlags.NonPublic) != null,
                "设置页 fallback 必须消费同一套单行投影");
            var dynamicDescriptors = ReloadSkillSettingsSurface.CreateDynamicDescriptors(
                new FeatureId(LirRuntime.FeatureIdValue));
            ReloadSkillSettingsSurface.CurrentLevelProvider = null;
            ReloadSkillSettingsSurface.CurrentExperienceProvider = null;
            try
            {
                var beforeStart = dynamicDescriptors[0];
                check(beforeStart.DescriptionKey.Contains("等级 0/2"),
                    "惰性设置描述符的注册前快照应从 0 级起步");
                ReloadSkillSettingsSurface.CurrentLevelProvider = () => 1;
                ReloadSkillSettingsSurface.CurrentExperienceProvider = () => uint.MaxValue;
                var afterStart = dynamicDescriptors[0];
                check(afterStart.DescriptionKey.Contains("等级 1/2")
                    && afterStart.AllowedValues.Count == 2
                    && afterStart.AllowedValues[1].Text.Contains("升级到 2"),
                    "模块 Start 绑定 provider 后，已注册描述符必须读取最新 projection");
            }
            finally
            {
                ReloadSkillSettingsSurface.CurrentLevelProvider = null;
                ReloadSkillSettingsSurface.CurrentExperienceProvider = null;
            }

            foreach (var field in typeof(ReloadSkillDashboardSurface).GetFields(
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                check(field.FieldType != typeof(ReloadAutoRoundScheduler),
                    "技能 UI 不得持有被动调度器：" + field.Name);
            }
            var savedHeadless = LirRuntime.HostHeadlessDecision;
            try
            {
                LirRuntime.HostHeadlessDecision = () => true;
                var headlessRegistration = LirFeatureAssembly.CreateRegistration();
                check(!(headlessRegistration is IFeatureSettingsRegistration),
                    "U3DS headless 注册不得转为设置页 fallback");
            }
            finally
            {
                LirRuntime.HostHeadlessDecision = savedHeadless;
            }

            ReloadSkillSettingsSurface.CurrentLevelProvider = () => 1;
            ReloadSkillSettingsSurface.CurrentExperienceProvider = () => uint.MaxValue;
            try
            {
                var descriptor = ReloadSkillSettingsSurface.CreateDescriptors(
                    new FeatureId(LirRuntime.FeatureIdValue))[0];
                check(descriptor.DescriptionKey.Contains("等级 1/2")
                    && descriptor.AllowedValues.Count == 2
                    && descriptor.AllowedValues[1].Text.Contains("升级到 2"),
                    "设置页必须按当前等级投影同一单行并只暴露下一等级入口");
                ReloadSkillSettingsSurface.CurrentExperienceProvider = () => 0u;
                var poor = ReloadSkillSettingsSurface.CreateDescriptors(
                    new FeatureId(LirRuntime.FeatureIdValue))[0];
                check(poor.AllowedValues.Count == 1,
                    "设置页经验不足时不得暴露可升级选项");
            }
            finally
            {
                ReloadSkillSettingsSurface.CurrentLevelProvider = null;
                ReloadSkillSettingsSurface.CurrentExperienceProvider = null;
            }
            check(typeof(ReloadSkillStore).GetMethod("TrySetLevel",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(ReloadSkillScopeKey), typeof(byte) }, null) != null,
                "技能账必须可脱离 UI/被动调度器独立写入");

        }
    }
}
