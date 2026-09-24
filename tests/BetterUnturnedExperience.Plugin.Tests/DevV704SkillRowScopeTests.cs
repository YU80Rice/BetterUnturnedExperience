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
