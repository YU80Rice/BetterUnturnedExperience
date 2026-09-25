using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using BetterUnturnedExperience.Contracts;
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
            Group("HostTick 生产链与生命周期", failures, collectAllFailures, () => HostTickProductionChain(Check));
            Group("库存事件页闸", failures, collectAllFailures, () => InventoryEventGate(Check));

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

        private static void HostTickProductionChain(Action<bool, string> check)
        {
            var originalScanner = AmmoObservationEngine.FactsReaderForTests;
            var originalGun = AmmoObservationEngine.LocalGunForTests;
            var originalApply = AmmoReserveHudAdapter.TotalApplyForTests;
            var originalHide = AmmoReserveHudAdapter.HideAllForTests;
            var originalDiagnostic = AmmoReserveHudAdapter.DiagnosticForTests;
            var originalCoreInstaller = InPlaceReloadModule.CorePatchInstallerForTests;
            var originalHudInstaller = InPlaceReloadModule.HudPatchInstallerForTests;
            var applied = new List<AmmoTotalResult>();
            var hidden = 0;
            var diagnostics = new List<string>();
            var readerCalls = 0;
            var gunCalls = 0;
            var gun = new object();
            var observation = new AmmoObservation
            {
                GunMagazineCalibers = new ushort[] { 10 },
                GunAllowsZeroCaliber = false,
                LoadedMagazine = new LoadedAmmoMagazine { Id = 5000, CurrentAmmo = 7, Calibers = new ushort[] { 10 }, FillSupplyIds = new ushort[] { 77 } },
                Entries = new[] { Mag(2, 5001, 3, 10), Box(3, 77, 11) },
            };
            InPlaceReloadModule module = null;
            try
            {
                AmmoObservationEngine.LocalGunForTests = () => { gunCalls++; return gun; };
                AmmoObservationEngine.FactsReaderForTests = _ =>
                {
                    readerCalls++;
                    return new AmmoEngineFacts
                    {
                        GunMagazineCalibers = observation.GunMagazineCalibers,
                        GunAllowsZeroCaliber = observation.GunAllowsZeroCaliber,
                        LoadedMagazineId = observation.LoadedMagazine.HasValue ? observation.LoadedMagazine.Value.Id : (ushort)0,
                        LoadedCurrentAmmo = observation.LoadedMagazine.HasValue ? (byte)observation.LoadedMagazine.Value.CurrentAmmo : (byte)0,
                        LoadedMagazineCalibers = observation.LoadedMagazine.HasValue ? observation.LoadedMagazine.Value.Calibers : null,
                        LoadedFillSupplyIds = observation.LoadedMagazine.HasValue ? observation.LoadedMagazine.Value.FillSupplyIds : null,
                        Entries = observation.Entries,
                    };
                };
                AmmoReserveHudAdapter.TotalApplyForTests = (_, result) => applied.Add(result);
                AmmoReserveHudAdapter.DiagnosticForTests = diagnostics.Add;
                InPlaceReloadModule.CorePatchInstallerForTests = _ => true;
                InPlaceReloadModule.HudPatchInstallerForTests = _ => true;
                AmmoReserveHudAdapter.HideAllForTests = () => hidden++;
                module = StartLifecycleModule(check);
                check(module.Started && ReferenceEquals(InPlaceReloadModule.ActiveModule, module), "HostTick 生产链必须由已启动的当前 LIR 代际承载");
                AmmoReserveHudPatch.Postfix(gun);
                check(gunCalls == 0, "updateInfo 入口直接消费已传入枪实例，不应重复解析本地枪");
                check(readerCalls == 1, "updateInfo 必须进入最底层弹药事实读取器");
                check(applied.Count == 1 && applied[0].TotalAmmo == 21, "updateInfo 必须从真实观察读口进入 ApplyTotal");
                module.OnHostTick(new HostTick(1UL, 0.016f, TickPhase.Update));
                check(gunCalls == 1, "HostTick 必须解析一次本地枪实例");
                check(readerCalls == 2, "HostTick 必须重新观察事实源以计算当前帧指纹");
                check(applied.Count == 1, "同一指纹同一拍不重复 Apply");
                AmmoReserveHudAdapter.MarkInventoryDirty();
                module.OnHostTick(new HostTick(2UL, 0.016f, TickPhase.Update));
                check(applied.Count == 2 && applied[1].TotalAmmo == 21, "库存事件 dirty 即使总数暂时相同也强制完成下一拍生产观察");
                observation.Entries = new[] { Mag(2, 5001, 4, 10), Box(3, 77, 11) };
                module.OnHostTick(new HostTick(3UL, 0.016f, TickPhase.Update));
                check(applied.Count == 3 && applied[2].TotalAmmo == 22, "身上匹配匣 amount 变化后下一拍必须 Apply");
                var replacementGun = new object();
                AmmoObservationEngine.LocalGunForTests = () => replacementGun;
                module.OnHostTick(new HostTick(4UL, 0.016f, TickPhase.Update));
                check(applied.Count == 4, "换枪实例后下一帧必须重新 Apply，即使事实指纹相同");
                check(AmmoReserveHudSurface.NeedsSlotRebind(new object(), new object()), "同枪新 infoBox 必须判定为需要重建 Slot");
                check(!AmmoReserveHudSurface.NeedsSlotRebind(replacementGun, replacementGun), "同一 infoBox 身份不应无谓重建 Slot");
                AmmoObservationEngine.LocalGunForTests = () => null;
                module.OnHostTick(new HostTick(5UL, 0.016f, TickPhase.Update));
                module.OnHostTick(new HostTick(6UL, 0.016f, TickPhase.Update));
                check(hidden == 2, "无枪每拍仍必须执行隐藏路径");
                check(diagnostics.Count(d => d.Contains("source=HostTick+inventory-dirty")) >= 1, "库存 dirty 消费必须由 HostTick 明确记录");
                check(diagnostics.Count(d => d.Contains("source=RevokeAll")) == 1, "连续无枪帧只记录一次 RevokeAll 诊断");
            }
            finally
            {
                if (module != null)
                {
                    try { module.Stop(FeatureStopReason.PluginStopping); } catch (Exception) { }
                }
                AmmoObservationEngine.FactsReaderForTests = originalScanner;
                AmmoObservationEngine.LocalGunForTests = originalGun;
                AmmoReserveHudAdapter.TotalApplyForTests = originalApply;
                AmmoReserveHudAdapter.HideAllForTests = originalHide;
                AmmoReserveHudAdapter.DiagnosticForTests = originalDiagnostic;
                InPlaceReloadModule.CorePatchInstallerForTests = originalCoreInstaller;
                InPlaceReloadModule.HudPatchInstallerForTests = originalHudInstaller;
                AmmoReserveHudAdapter.RevokeAll();
            }
        }

        private static InPlaceReloadModule StartLifecycleModule(Action<bool, string> check)
        {
            var helper = typeof(DevV5AmmoReserveHudTests);
            var settingsType = helper.GetNestedType("V56SettingsView", BindingFlags.NonPublic);
            var settings = Activator.CreateInstance(settingsType);
            var start = helper.GetMethod("V56StartModule", BindingFlags.NonPublic | BindingFlags.Static);
            if (start == null) throw new InvalidOperationException("V5-06 lifecycle fixture missing");
            return (InPlaceReloadModule)start.Invoke(null, new object[] { settings, check, false });
        }

        private static void InventoryEventGate(Action<bool, string> check)
        {
            check(InPlaceReloadModule.IsAmmoInventoryPageForTests(2), "SLOTS 页事件进入弹药刷新闸");
            check(InPlaceReloadModule.IsAmmoInventoryPageForTests(6), "PANTS 页事件进入弹药刷新闸");
            check(!InPlaceReloadModule.IsAmmoInventoryPageForTests(1), "装备槽事件不触发身上弹药刷新");
            check(!InPlaceReloadModule.IsAmmoInventoryPageForTests(7), "容器事件不触发身上弹药刷新");
            check(!InPlaceReloadModule.IsAmmoInventoryPageForTests(8), "地面事件不触发身上弹药刷新");
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
