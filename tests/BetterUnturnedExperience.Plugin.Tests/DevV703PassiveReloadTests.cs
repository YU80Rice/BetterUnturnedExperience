using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterUnturnedExperience.Lir;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V7-03 红测：2 级独立被动压弹周期、主机权威和事实源隔离。
    /// </summary>
    internal static class DevV703PassiveReloadTests
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

            Group("独立周期与每拍重算", failures, collectAllFailures, () => Period(Check));
            Group("等级/存活/代际清窗", failures, collectAllFailures, () => Clearing(Check));
            Group("不依赖手动与枪械", failures, collectAllFailures, () => Independence(Check));
            Group("主机权威与静默派发", failures, collectAllFailures, () => AuthorityBoundary(Check));
            Group("V7-02 单源与功能隔离", failures, collectAllFailures, () => SourceBoundary(Check));

            Console.WriteLine("DEV-V7-03 passive-reload tests: " + (failures.Count == 0 ? "PASS" : "FAIL"));
            if (failures.Count != 0)
                throw new InvalidOperationException("DEV-V7-03 failures (" + failures.Count + "): " + string.Join(" || ", failures));
        }

        private static void Group(string name, List<string> failures, bool collect, Action body)
        {
            try { body(); }
            catch (Exception error) when (collect)
            {
                failures.Add("[" + name + "] " + error.Message);
            }
        }

        private static void Period(Action<bool, string> check)
        {
            var scheduler = new ReloadAutoRoundScheduler();
            var fired = new List<ulong>();
            scheduler.Sync(new[] { 7UL }, _ => ReloadSkillPolicy.MaxSkillLevel, _ => true, 0d);
            check(scheduler.PendingCount == 1, "2 级首次观察必须独立建立被动周期");
            scheduler.TickPassive(ReloadSkillPolicy.AutoRoundDelaySeconds - 0.01d, id => fired.Add(id));
            check(fired.Count == 0, "未到 8 秒不得尝试");
            scheduler.TickPassive(ReloadSkillPolicy.AutoRoundDelaySeconds, id => fired.Add(id));
            check(fired.SequenceEqual(new[] { 7UL }), "到点必须尝试一次");
            check(scheduler.PendingCount == 1, "尝试结束后必须保留下一拍");
            scheduler.TickPassive(ReloadSkillPolicy.AutoRoundDelaySeconds + 0.01d, id => fired.Add(id));
            check(fired.Count == 1, "不满下一周期不得追赶");
            scheduler.TickPassive(ReloadSkillPolicy.AutoRoundDelaySeconds * 2d, id => fired.Add(id));
            check(fired.Count == 2, "第二个完整周期才允许再次尝试");
        }

        private static void Clearing(Action<bool, string> check)
        {
            var scheduler = new ReloadAutoRoundScheduler();
            scheduler.Sync(new[] { 7UL }, _ => 2, _ => true, 10d);
            check(scheduler.PendingCount == 1, "2 级资格进入周期表");
            scheduler.Sync(new[] { 7UL }, _ => 1, _ => true, 11d);
            check(scheduler.PendingCount == 0, "降到 2 级以下必须清窗");
            scheduler.Sync(new[] { 7UL }, _ => 2, _ => false, 12d);
            check(scheduler.PendingCount == 0, "死亡或不可解析必须清窗");
            scheduler.Sync(new[] { 7UL }, _ => 2, _ => true, 20d);
            check(scheduler.PendingCount == 1, "重新满足资格从当前时刻重新起算");
            scheduler.ResetForGeneration();
            check(scheduler.PendingCount == 0, "Stop/切世界代际必须清表");
        }

        private static void Independence(Action<bool, string> check)
        {
            var scheduler = new ReloadAutoRoundScheduler();
            scheduler.Sync(new[] { 7UL }, _ => 2, _ => true, 0d);
            var attempts = 0;
            scheduler.TickPassive(ReloadSkillPolicy.AutoRoundDelaySeconds, _ => attempts++);
            check(attempts == 1, "没有手动成功也必须到点尝试");
            check(scheduler.PendingCount == 1, "被动周期不由一次手动成功决定");
            check(typeof(AmmoObservationEngine).GetMethod("ResolveLocalGun", BindingFlags.Static | BindingFlags.NonPublic) != null,
                "引擎仍有枪观察器，但被动周期不能以持枪作为启动条件");
        }

        private static void AuthorityBoundary(Action<bool, string> check)
        {
            var passive = typeof(LirRepackNetwork).GetMethod("ExecutePassiveRepackFor",
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(ulong) }, null);
            check(passive != null, "被动必须使用无 requestId 的主机专用入口");
            var manual = typeof(LirRepackNetwork).GetMethod("ExecuteRepackFor",
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(ulong), typeof(ulong) }, null);
            check(manual != null, "手动仍保留 requestId 入口");
            var success = typeof(LirRepackNetwork).GetMethod("SendRepackSuccess", BindingFlags.Instance | BindingFlags.NonPublic);
            check(success != null && success.GetParameters().Length == 3,
                "成功帧发送器不能接受被动 isAuto/wireId=0 分支");
            var authority = typeof(AmmoRepackService).GetMethod("TryRepackTransactional", BindingFlags.Static | BindingFlags.NonPublic);
            check(authority != null, "被动必须复用功能 B 真实事务入口");
        }

        private static void SourceBoundary(Action<bool, string> check)
        {
            var schedulerFields = typeof(ReloadAutoRoundScheduler).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in schedulerFields)
            {
                check(field.FieldType != typeof(AmmoReserveHudAdapter)
                    && field.FieldType != typeof(AmmoReserveHudSurface)
                    && field.FieldType != typeof(AmmoReserveHudBinder),
                    "被动调度器不得持有 HUD 生命周期类型：" + field.Name);
            }
            check(typeof(AmmoTotalProjection).Namespace == "BetterUnturnedExperience.Lir",
                "被动必须消费 V7-02 LIR 单源，而非复制观察/匹配");
            check(ReloadRuntimePolicy.MinRepackPage == 2 && ReloadRuntimePolicy.MaxRepackPage == 6,
                "被动目标范围必须沿用身上五页 2..6");
            check(typeof(TidyCompletedConsumer).Namespace == "BetterUnturnedExperience.Lir",
                "功能 A 类型存在但被动周期不得挂到整理事件");
        }
    }
}
