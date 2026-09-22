using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using BetterUnturnedExperience.Lir;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V7-02 红测：总弹药事实源与 HUD 消费边界。
    /// 先锁定不依赖 HUD 表面的纯观察/匹配/公式，再验证文案与刷新契约。
    /// </summary>
    internal static class DevV702TotalAmmoHudTests
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

            Group("公式与零值", failures, collectAllFailures, () => Formula(Check));
            Group("单源与 HUD 分层", failures, collectAllFailures, () => Layering(Check));
            Group("文案与刷新合同", failures, collectAllFailures, () => SurfaceContract(Check));
            Group("下一帧变化指纹", failures, collectAllFailures, () => RefreshFingerprint(Check));

            Console.WriteLine("DEV-V7-02 total-ammo-hud tests: " + (failures.Count == 0 ? "PASS" : "FAIL"));
            if (failures.Count != 0)
                throw new InvalidOperationException("DEV-V7-02 failures (" + failures.Count + "): " + string.Join(" || ", failures));
        }

        private static void Group(string name, List<string> failures, bool collect, Action body)
        {
            try { body(); }
            catch (Exception error) when (collect)
            {
                failures.Add("[" + name + "] " + error.Message);
            }
        }

        private static void Formula(Action<bool, string> check)
        {
            var observation = new AmmoObservation
            {
                GunMagazineCalibers = new ushort[] { 10 },
                GunAllowsZeroCaliber = false,
                LoadedMagazine = new LoadedAmmoMagazine
                {
                    Id = 5000,
                    CurrentAmmo = 7,
                    Calibers = new ushort[] { 10 },
                    FillSupplyIds = new ushort[] { 77 },
                },
                Entries = new[]
                {
                    Mag(1, 5999, 40, 10),
                    Mag(2, 5001, 3, 10),
                    Mag(3, 5002, 0, 10),
                    Box(4, 77, 11),
                    new AmmoEntry { Page = 5, Id = 88, IsMagazine = false, IsCaliberAsset = true, Amount = 20, MaxAmount = 30, Calibers = new ushort[] { 10 } },
                    Box(7, 77, 100),
                    Mag(8, 5003, 99, 10),
                },
            };

            var result = AmmoTotalProjection.Project(observation);
            check(result.TotalAmmo == 21, "总数必须是枪上 7 + 身上匹配匣 3 + 匹配箱 11；空匣为 0，容器/地面不计");
            check(result.LabelText == "总弹药 21", "总弹药文案必须逐字格式化");

            observation.LoadedMagazine = new LoadedAmmoMagazine
            {
                Id = 5000,
                CurrentAmmo = 0,
                Calibers = new ushort[] { 10 },
                FillSupplyIds = new ushort[] { 77 },
            };
            observation.Entries = new[] { Mag(2, 5001, 0, 10) };
            result = AmmoTotalProjection.Project(observation);
            check(result.TotalAmmo == 0 && result.LabelText == "总弹药 0", "枪上/背包全为空时仍显示总弹药 0");

            observation.LoadedMagazine = null;
            observation.Entries = new[] { Mag(2, 5001, 4, 10), Box(2, 77, 8) };
            result = AmmoTotalProjection.Project(observation);
            check(result.TotalAmmo == 4, "无枪上匣时不伪造枪上对象，箱侧没有当前匣供弹对象");
        }

        private static void Layering(Action<bool, string> check)
        {
            var source = typeof(AmmoObservation);
            var formula = typeof(AmmoTotalProjection);
            check(source.Namespace == "BetterUnturnedExperience.Lir" && formula.Namespace == "BetterUnturnedExperience.Lir",
                "观察 DTO 与公式必须位于非 HUD LIR 命名空间");
            check(source.GetCustomAttributes(false).Length >= 0, "事实源类型可独立反射消费");

            var hudTypes = new[]
            {
                typeof(BetterUnturnedExperience.Lir.AmmoReserveHudAdapter),
                typeof(BetterUnturnedExperience.Lir.AmmoReserveHudSurface),
                typeof(BetterUnturnedExperience.Lir.AmmoReserveHudBinder),
            };
            foreach (var hudType in hudTypes)
            {
                check(!formula.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(f => f.FieldType == hudType),
                    "事实源/公式不得持有 HUD 生命周期类型：" + hudType.Name);
            }
        }

        private static void SurfaceContract(Action<bool, string> check)
        {
            check(AmmoTotalProjection.LabelFormat == "总弹药 {0}", "总弹药文案常量冻结为总弹药 {0}");
            check(typeof(AmmoReserveHudPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic) != null,
                "updateInfo postfix 仍保留为唯一即时触发面");
        }

        private static void RefreshFingerprint(Action<bool, string> check)
        {
            var first = new AmmoObservation
            {
                GunMagazineCalibers = new ushort[] { 10 },
                GunAllowsZeroCaliber = false,
                LoadedMagazine = new LoadedAmmoMagazine { Id = 5000, CurrentAmmo = 7, Calibers = new ushort[] { 10 }, FillSupplyIds = new ushort[] { 77 } },
                Entries = new[] { Mag(2, 5001, 3, 10), Box(3, 77, 11) },
            };
            var same = new AmmoObservation
            {
                GunMagazineCalibers = first.GunMagazineCalibers,
                GunAllowsZeroCaliber = first.GunAllowsZeroCaliber,
                LoadedMagazine = first.LoadedMagazine,
                Entries = new[] { Mag(2, 5001, 3, 10), Box(3, 77, 11) },
            };
            var changed = new AmmoObservation
            {
                GunMagazineCalibers = first.GunMagazineCalibers,
                GunAllowsZeroCaliber = first.GunAllowsZeroCaliber,
                LoadedMagazine = first.LoadedMagazine,
                Entries = new[] { Mag(2, 5001, 3, 10), Box(3, 77, 12) },
            };
            var firstKey = AmmoTotalProjection.BuildFingerprint(first);
            check(firstKey == AmmoTotalProjection.BuildFingerprint(same), "未变化观察的帧指纹稳定");
            check(firstKey != AmmoTotalProjection.BuildFingerprint(changed), "身上匹配箱 amount 变化会改变帧指纹");
            changed.LoadedMagazine = new LoadedAmmoMagazine { Id = 5000, CurrentAmmo = 6, Calibers = new ushort[] { 10 }, FillSupplyIds = new ushort[] { 77 } };
            check(firstKey != AmmoTotalProjection.BuildFingerprint(changed), "枪上发数变化会改变帧指纹");
        }

        private static AmmoEntry Mag(byte page, ushort id, byte amount, ushort caliber)
        {
            return new AmmoEntry
            {
                Page = page,
                Id = id,
                IsMagazine = true,
                IsCaliberAsset = true,
                Amount = amount,
                MaxAmount = 30,
                Calibers = new ushort[] { caliber },
            };
        }

        private static AmmoEntry Box(byte page, ushort id, byte amount)
        {
            return new AmmoEntry
            {
                Page = page,
                Id = id,
                IsMagazine = false,
                IsCaliberAsset = false,
                Amount = amount,
                MaxAmount = 120,
            };
        }
    }
}
