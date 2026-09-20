using System;
using System.Collections.Generic;
using System.Reflection;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.Lir;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V5-06（V5-T7 → 弹药后备 HUD）红测先行组。判据面 = 票面验收条逐字对位：
    /// N/M 定义（N = 身上五页 2..6、与当前武器口径匹配、amount&gt;0 的备用匣本数，不含
    /// 枪上那本、不含空匣、不含容器/地面；M = 这些匣余弹 + 能经 FillTargetItem 给当前匣
    /// 供弹的箱，匹配同现网压弹——主路径 supplies 命中即排除 fallback，零口径跳过、
    /// MaxAmount==0 不计、弹匣永不进弹药源，逐条对齐 AmmoRepackService.BuildRepackPlan）；
    /// 功能停不画 = 生命周期登记是唯一开关（Start 登记/Stop 注销+HideAll/开关热摘，
    /// 补丁体内无功能 bool；U3DS headless 不武装 HUD 面）；官方先行消费 = 真实持枪的
    /// 投影链走更好的换弹体验（postfix → LIR 登记闸 → 注入观察 → 纯投影 → 注入呈现），
    /// binder 显式解析 UseableGun.updateInfo 为绑定唯一事实源（宿主可证）。
    /// 真机面（Glazier 实际注入/右下 infoBox 几何/真 updateInfo 频率/真背包扫描）为
    /// 具名接缝缺口（03/04/05 同界），实机随 DEV-V5-08。
    /// </summary>
    internal static class DevV5AmmoReserveHudTests
    {
        internal static void Run(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            LirRuntime.MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    var savedScanner = AmmoReserveHudAdapter.ScannerForTests;
                    var savedApply = AmmoReserveHudAdapter.ApplyForTests;
                    var savedHideAll = AmmoReserveHudAdapter.HideAllForTests;
                    var savedHudInstaller = InPlaceReloadModule.HudPatchInstallerForTests;
                    var savedCoreInstaller = InPlaceReloadModule.CorePatchInstallerForTests;
                    var savedHeadless = LirRuntime.HostHeadlessDecision; // DEV-V6-02C: headless 决策经宿主注入缝读入
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        var reported = error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message;
                        if (error.InnerException != null) reported += " <INNER " + error.InnerException.GetType().Name + ": " + error.InnerException.Message + ">";
                        reds.Add("[" + name + "] " + reported);
                    }
                    finally
                    {
                        if (InPlaceReloadModule.ActiveModule != null)
                        {
                            try { InPlaceReloadModule.ActiveModule.Stop(FeatureStopReason.PluginStopping); } catch (Exception) { }
                        }
                        AmmoReserveHudAdapter.ScannerForTests = savedScanner;
                        AmmoReserveHudAdapter.ApplyForTests = savedApply;
                        AmmoReserveHudAdapter.HideAllForTests = savedHideAll;
                        InPlaceReloadModule.HudPatchInstallerForTests = savedHudInstaller;
                        InPlaceReloadModule.CorePatchInstallerForTests = savedCoreInstaller;
                        LirRuntime.HostHeadlessDecision = savedHeadless;
                    }
                }

                Group("N 定义（身上五页口径匹配非空备用匣）", () => V56GroupSpareMagDefinition(Check));
                Group("M 定义（备用余弹+匹配弹药箱，同现网压弹）", () => V56GroupReserveDefinition(Check));
                Group("文案冻结单源", () => V56GroupLabel(Check));
                Group("生命周期登记即开关与停画", () => V56GroupLifecycle(Check));
                Group("官方先行消费与 binder 宿主可证", () => V56GroupOfficialFirst(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V5-06 ammo-reserve-hud collection: ALL GREEN (0 failures) — groups: N 定义/M 定义/文案冻结单源/生命周期登记即开关与停画/官方先行消费与 binder 宿主可证");
            Console.WriteLine("DEV-V5-06 ammo-reserve-hud tests: " + (collectAllFailures && reds.Count > 0 ? "FAIL" : "PASS"));
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V5-06 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // ─────────────────────────────────────────────────────────────────
        // 组1 — N 定义：身上五页、与当前武器口径匹配、amount>0 的备用匣本数。
        // 不含枪上那本（装载匣描述无弹量字段=结构保证+反射钉）、不含空匣、
        // 不含容器/地面（Page 不在 2..6 的条目一律不参算）。口径匹配谓词
        // 逐字复刻原版 Items.SearchContents（MAGAZINE 搜索：非口径资产不进、
        // 空口径看 allowZeroCaliber、否则集合相等交集、无零跳过）。
        // ─────────────────────────────────────────────────────────────────
        private static void V56GroupSpareMagDefinition(System.Action<bool, string> check)
        {
            // 1a. 页范围 = 身上五页：page 1（装备槽）/7（容器）/8（地面/AREA）不参算。
            var obs = new AmmoReserveObservation
            {
                GunMagazineCalibers = new ushort[] { 10 },
                GunAllowsZeroCaliber = false,
                LoadedMagazine = null,
                Entries = new[]
                {
                    V56Mag(2, 5001, 1, 10),
                    V56Mag(1, 5002, 4, 10),   // 装备槽页：不计
                    V56Mag(7, 5003, 3, 10),   // 容器页：不计
                    V56Mag(8, 5004, 9, 10),   // 地面/AREA 页：不计
                },
            };
            var r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 1 && r.ReserveRounds == 1, "N 只数身上五页（2..6）条目：page1/7/8 全不参算");

            // 1b. 空匣（amount==0）不计，非空计入 M 的备用余弹半边。
            obs.Entries = new[]
            {
                V56Mag(2, 5001, 0, 10),
                V56Mag(2, 5002, 5, 10),
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 1 && r.ReserveRounds == 5, "空匣不计 N；备用匣余弹进 M");

            // 1c. 口径不匹配的备用匣不进 N（也不进 M 备用半边）。
            obs.Entries = new[]
            {
                V56Mag(2, 5001, 4, 20),      // 口径 20 vs 枪 10 → 不匹配
                V56Mag(3, 5002, 6, 10),
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 1 && r.ReserveRounds == 6, "口径不匹配的备用匣不算（N/M 双侧）");

            // 1d. 原版 allowZeroCaliber 语义：口径未填的弹匣仅在枪允许零口径时计入。
            obs.Entries = new[] { V56Mag(2, 5001, 7, 0, unspecifiedCaliber: true) };
            obs.GunAllowsZeroCaliber = true;
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 1, "空口径弹匣在 allowZeroCaliber=true 时计入（原版同口径）");
            obs.GunAllowsZeroCaliber = false;
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 0, "空口径弹匣在 allowZeroCaliber=false 时不计（原版同口径）");
            obs.GunAllowsZeroCaliber = false;

            // 1e. N 侧交集无零跳过（原版 CalibersContainAnyOfIds 逐字语义：双方含 0 即中），
            // 与 fallback 侧（跳零，见组2）刻意不同源——各按一手出处复刻。
            check(AmmoReserveProjection.CalibersMatchWeapon(new ushort[] { 0 }, new ushort[] { 0 }, false),
                "N 匹配复刻 vanilla：无零跳过，0==0 即口径匹配");
            check(!AmmoReserveProjection.CalibersMatchWeapon(new ushort[] { 11 }, new ushort[0], false) &&
                  !AmmoReserveProjection.CalibersMatchWeapon(new ushort[] { 11 }, null, false),
                "枪口径集为空 → 有口径弹匣不匹配（无匹配池）");

            // 1f. 「不含枪上那本」的结构保证：装载匣描述类型不存在任何弹量输入。
            var loadedFields = typeof(AmmoReserveLoadedMagazine).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var hasAmmoField = false;
            foreach (var f in loadedFields)
            {
                if (f.Name.IndexOf("mount", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    f.Name.IndexOf("Ammo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    f.Name.IndexOf("Rounds", StringComparison.OrdinalIgnoreCase) >= 0) hasAmmoField = true;
            }
            check(!hasAmmoField, "枪上匣无弹量字段（amount/ammo/rounds 任一）——当前匣发数结构上不可能混入后备");

            // 1g. 同 ID 备用匣按本数计（两本各算一本、弹量各计其数）。
            obs.Entries = new[]
            {
                V56Mag(2, 5001, 3, 10),
                V56Mag(3, 5001, 4, 10),
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 2 && r.ReserveRounds == 7, "同 ID 备用匣按本数计");

            // 1h. 弹药箱/其他物品永不进 N。
            obs.Entries = new[]
            {
                new AmmoReserveEntry { Page = 2, Id = 9001, IsMagazine = false, IsCaliberAsset = false, Amount = 40, MaxAmount = 60, Calibers = null },
                new AmmoReserveEntry { Page = 3, Id = 9002, IsMagazine = false, IsCaliberAsset = true, Amount = 8, MaxAmount = 20, Calibers = new ushort[] { 10 } },
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 0, "非弹匣条目不进 N（无 Loaded → 也不进 M）");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组2 — M 定义：备用匣余弹 + 能给「当前匣（枪上匣）」供弹的弹药箱发数，
        // 匹配规则同现网压弹（AmmoRepackService.BuildRepackPlan 逐条对齐：
        // 弹药源 = 非弹匣 ∧ amount>0 ∧ MaxAmount>0；主路径 = 箱 id ∈ 当前匣
        // FillTargetItem supplies 集；主路径零候选 ∧ 当前匣有口径 → 仅口径资产
        // 参与交集 fallback（mag 侧零口径跳过，FindCaliberMatch 逐字语义）；
        // 主路径命中即整体排除 fallback；弹匣永不进弹药源（其弹量只走备用半边）。
        // ─────────────────────────────────────────────────────────────────
        private static void V56GroupReserveDefinition(System.Action<bool, string> check)
        {
            var gunCal = new ushort[] { 10 };
            var loaded = new AmmoReserveLoadedMagazine { Id = 5000, Calibers = new ushort[] { 10 }, FillSupplyIds = new ushort[] { 77 } };

            // 2a. 基线：备用余弹 + 主路径命中箱。
            var obs = new AmmoReserveObservation
            {
                GunMagazineCalibers = gunCal,
                GunAllowsZeroCaliber = false,
                LoadedMagazine = loaded,
                Entries = new[]
                {
                    V56Mag(2, 5001, 3, 10),
                    V56Mag(3, 5002, 4, 10),
                    V56Box(2, 77, 10),            // 主路径：id ∈ FillSupplyIds
                    V56Box(3, 78, 99),            // 非匹配箱：不占
                },
            };
            var r = AmmoReserveProjection.Project(obs);
            check(r.ReserveRounds == 3 + 4 + 10, "M = Σ备用余弹 + Σ主路径命中箱（非匹配箱不占）");

            // 2b. 主路径有候选 = fallback 整体排除（现网 candidateBoxLists 律，禁双计）。
            obs.Entries = new[]
            {
                V56Box(2, 77, 10, calibers: new ushort[] { 10 }),      // 主路径命中
                new AmmoReserveEntry { Page = 3, Id = 88, IsMagazine = false, IsCaliberAsset = true, Amount = 20, MaxAmount = 30, Calibers = new ushort[] { 10 } }, // 口径资产、交集成立、但 id 不命中
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.ReserveRounds == 10, "主路径命中即排除口径 fallback（同现网：候选箱列表非空则不问 fallback）");

            // 2c. 主路径无候选 → 口径 fallback：仅口径资产参与（非口径箱不计），
            // 交集成立的箱计、不成立的箱不计。
            obs.LoadedMagazine = new AmmoReserveLoadedMagazine { Id = 5000, Calibers = new ushort[] { 10, 12 }, FillSupplyIds = new ushort[0] };
            obs.Entries = new[]
            {
                new AmmoReserveEntry { Page = 2, Id = 88, IsMagazine = false, IsCaliberAsset = true, Amount = 7, MaxAmount = 30, Calibers = new ushort[] { 12 } },
                new AmmoReserveEntry { Page = 3, Id = 89, IsMagazine = false, IsCaliberAsset = true, Amount = 6, MaxAmount = 30, Calibers = new ushort[] { 30 } }, // 交集不成立
                new AmmoReserveEntry { Page = 4, Id = 90, IsMagazine = false, IsCaliberAsset = false, Amount = 9, MaxAmount = 30, Calibers = new ushort[] { 10 } }, // 普通弹药箱不触发 fallback（现网注释钉死）
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.ReserveRounds == 7, "主路径无候选才走口径 fallback，且仅 ItemCaliberAsset 资产参算");

            // 2d. fallback 零口径跳过（现网 FindCaliberMatch 逐字：mag 侧 0 不配）。
            check(!AmmoReserveProjection.MagSuppliesMatch(new ushort[] { 0 }, new ushort[] { 0 }),
                "当前匣口径 0 永不配箱（现网零跳过；与 N 侧 vanilla 语义不同源）");
            check(AmmoReserveProjection.MagSuppliesMatch(new ushort[] { 0, 12 }, new ushort[] { 12 }),
                "零口径跳过后仍有交集即配");
            obs.LoadedMagazine = new AmmoReserveLoadedMagazine { Id = 5000, Calibers = new ushort[] { 0 }, FillSupplyIds = new ushort[0] };
            obs.Entries = new[]
            {
                new AmmoReserveEntry { Page = 2, Id = 88, IsMagazine = false, IsCaliberAsset = true, Amount = 7, MaxAmount = 30, Calibers = new ushort[] { 0 } },
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.ReserveRounds == 0, "投影层同样执行零跳过（双侧 0 口径 = 不配）");

            // 2e. 当前匣无 Fill 蓝图且无口径 → 箱零供弹；有匣无匹配的箱同理。
            obs.LoadedMagazine = new AmmoReserveLoadedMagazine { Id = 5000, Calibers = null, FillSupplyIds = null };
            obs.Entries = new[] { V56Box(2, 77, 20) };
            r = AmmoReserveProjection.Project(obs);
            check(r.ReserveRounds == 0, "无蓝图无口径 → 箱不供弹（现图铁规 1 的同源判定）");

            // 2f. 无当前匣（枪上无弹匣）→ 不存在「给当前匣供弹」，箱全不占。
            obs.LoadedMagazine = null;
            obs.Entries = new[] { V56Box(2, 77, 20), V56Mag(3, 5001, 5, 10) };
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 1 && r.ReserveRounds == 5, "无当前匣 → 备用余弹照计、箱侧恒 0");

            // 2g. MaxAmount==0 的弹药源不计（现网 ammoBoxMap 闸同）。
            obs.LoadedMagazine = loaded;
            obs.Entries = new[]
            {
                new AmmoReserveEntry { Page = 2, Id = 77, IsMagazine = false, IsCaliberAsset = false, Amount = 5, MaxAmount = 0, Calibers = null },
                V56Box(3, 77, 10),
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.ReserveRounds == 10, "MaxAmount==0 弹药源排除（同现网）");

            // 2h. 备用匣满弹照算（原版搜索无 ExcludeFullAmount；M 语义=能打的余弹）。
            obs.Entries = new[] { V56Mag(2, 5001, 30, 10, max: 30) };
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 1 && r.ReserveRounds == 30, "满弹备用匣计入 N/M（非「待填匣」口径）");

            // 2i. 弹匣 id 命中 Fill supplies 也只当备用匣（永不进弹药源，禁双计）。
            obs.Entries = new[]
            {
                new AmmoReserveEntry { Page = 2, Id = 77, IsMagazine = true, IsCaliberAsset = true, Amount = 5, MaxAmount = 30, Calibers = new ushort[] { 10 } },
            };
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 1 && r.ReserveRounds == 5, "弹匣 id 命中 supplies 不双计（现网：匣不进 ammoBoxMap）");

            // 2j. 箱也受页范围闸（容器里的箱不算后备）。
            obs.Entries = new[] { V56BoxEntry(7, 77, 40) };
            r = AmmoReserveProjection.Project(obs);
            check(r.ReserveRounds == 0, "page 7 的匹配箱不参算（后备 = 身上，不含容器）");

            // 2k. 主路径命中集内多箱求和（同 id 多本、多 id 命中都计）。
            obs.LoadedMagazine = new AmmoReserveLoadedMagazine { Id = 5000, Calibers = new ushort[] { 10 }, FillSupplyIds = new ushort[] { 77, 78 } };
            obs.Entries = new[] { V56Box(2, 77, 10), V56Box(3, 77, 20), V56Box(4, 78, 5) };
            r = AmmoReserveProjection.Project(obs);
            check(r.ReserveRounds == 35, "主路径命中多本求和（无最满优先等额外序——发数与序无关）");

            // 2l. 无当前匣时 N 匹配仍按枪口径（口径闸独立于 Loaded）。
            obs.LoadedMagazine = null;
            obs.Entries = new[] { V56Mag(2, 5001, 6, 10) };
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 1 && r.ReserveRounds == 6, "N 的口径闸对「枪」成立（与是否已装匣无关——无匣时更该看得见备匣）");

            // 2m. 空 Entries/null Entries 健壮：0/0 出文，不抛。
            obs.Entries = null;
            r = AmmoReserveProjection.Project(obs);
            check(r.SpareMagCount == 0 && r.ReserveRounds == 0 && r.LabelText.Length > 0, "空观察 → 0/0 出文不抛");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组3 — 文案冻结单源：格式串与渲染结果都钉死（漂移即红）。
        // ─────────────────────────────────────────────────────────────────
        private static void V56GroupLabel(System.Action<bool, string> check)
        {
            check(AmmoReserveProjection.LabelFormat == "备匣 {0} · 备弹 {1}",
                "HUD 文案格式冻结：备匣 {0} · 备弹 {1}");
            var obs = new AmmoReserveObservation
            {
                GunMagazineCalibers = new ushort[] { 10 },
                GunAllowsZeroCaliber = false,
                LoadedMagazine = new AmmoReserveLoadedMagazine { Id = 5000, Calibers = new ushort[] { 10 }, FillSupplyIds = new ushort[] { 77 } },
                Entries = new[] { V56Mag(2, 5001, 20, 10), V56Box(3, 77, 37) },
            };
            var r = AmmoReserveProjection.Project(obs);
            check(r.LabelText == "备匣 1 · 备弹 57", "渲染逐字钉死（N=1 M=57）");
            obs.Entries = new AmmoReserveEntry[0];
            r = AmmoReserveProjection.Project(obs);
            check(r.LabelText == "备匣 0 · 备弹 0", "0/0 不早退——原版数字旁恒有后备读数（持枪即在）");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组4 — 生命周期登记 = 唯一开关（票面「停用则不画」；04/05 同纪律）：
        // Start 经假 installer 缝登记本票唯一面；Stop 注销 + HideAll（残留读数
        // 不滞留）；开关热摘/冷关不登记；U3DS headless 不武装画面面（权威压弹
        // 面照常）；半装失败三面互撤归零；补丁面类型不含自身功能 bool。
        // ─────────────────────────────────────────────────────────────────
        private static void V56GroupLifecycle(System.Action<bool, string> check)
        {
            // 三面同形：两权威压弹面也走假 installer（宿主真 Patch() 在 ECall
            // 边界不可靠——04/05 同律：生命周期判据走登记缝，真装真撤留 08）。
            InPlaceReloadModule.CorePatchInstallerForTests = t => true;
            var hudInstalled = new List<Type>();
            InPlaceReloadModule.HudPatchInstallerForTests = t => { hudInstalled.Add(t); return true; };
            var view = new V56SettingsView();
            var module = V56StartModule(view, check);

            // 4a. 登记即开：Start = 恰一面对本票 binder 面，ActiveModule 代际交付。
            check(hudInstalled.Count == 1 && hudInstalled[0] == typeof(AmmoReserveHudPatch),
                "Start 登记恰为弹药 HUD 面（唯一面，无夹带）");
            check(module.HudPatchInstalled && ReferenceEquals(InPlaceReloadModule.ActiveModule, module),
                "HudPatchInstalled 随登记为真；ActiveModule = 本代际");
            check(module.Started, "harness: 模块已启动");
            // DEV-V6-12：武装 ⇒ 已登记（一个 Harmony 身份一枚句柄进既有资源账）。
            check(V56Pocket.Accepted.Count == 1 && module.PatchTeardownDelegated && module.PatchRegistration != null,
                "登记入账：恰一枚句柄经启动口袋进账、拆除所有权移交平台（官方与生态同一公开接入面）");

            // 4b. 真持枪形状走 LIR 链：postfix 入口 → 登记闸 → 观察 → 纯投影 → 呈现。
            var applied = new List<AmmoReserveResult>();
            object seenGun = null;
            int scans = 0;
            AmmoReserveHudAdapter.ScannerForTests = gun =>
            {
                scans++;
                seenGun = gun;
                return new AmmoReserveObservation
                {
                    GunMagazineCalibers = new ushort[] { 10 },
                    GunAllowsZeroCaliber = false,
                    LoadedMagazine = new AmmoReserveLoadedMagazine { Id = 5000, Calibers = new ushort[] { 10 }, FillSupplyIds = new ushort[] { 77 } },
                    Entries = new[] { V56Mag(2, 5001, 5, 10), V56Box(3, 77, 10) },
                };
            };
            var appliedGuns = new List<object>();
            AmmoReserveHudAdapter.ApplyForTests = (gun, result) => { applied.Add(result); appliedGuns.Add(gun); };
            var fakeGun = new object();
            AmmoReserveHudAdapter.OnGunInfoUpdated(fakeGun);
            check(scans == 1 && ReferenceEquals(seenGun, fakeGun), "观察入口收到持枪实例本身（真实持枪时投影走此链）");
            check(applied.Count == 1 && applied[0].SpareMagCount == 1 && applied[0].ReserveRounds == 15
                    && applied[0].LabelText == "备匣 1 · 备弹 15"
                    && ReferenceEquals(appliedGuns[0], fakeGun),
                "在册 → 投影→呈现逐字（M=5+10），呈现收到同一持枪实例");

            // 4c. Stop = 注销：在册归零 + HideAll 恰一次；入口对功能停零介入。
            int hideAll = 0;
            AmmoReserveHudAdapter.HideAllForTests = () => hideAll++;
            module.Stop(FeatureStopReason.PluginStopping);
            check(!module.HudPatchInstalled && InPlaceReloadModule.ActiveModule == null,
                "Stop 后 HUD 面注销（补丁没了、登记也交还——功能停不画由注销保证）");
            check(module.PatchRegistration == null && !module.PatchTeardownDelegated,
                "Stop：登记交还模块（补丁本体在平台账上等边界释放，模块不自拆）");
            check(hideAll == 1, "Stop 触发一次 HideAll（在枪上的残留读数即时消失）");
            applied.Clear();
            scans = 0;
            AmmoReserveHudAdapter.OnGunInfoUpdated(fakeGun);
            check(scans == 0 && applied.Count == 0, "功能停 = 入口不观察不呈现（不是补丁内 if 短路的『关了还在跑』）");

            // 4d. 开关热摘/热装（RefreshSwitches 即装卸，登记闸同步）。
            var module2 = V56StartModule(view, check);
            hudInstalled.Clear();
            hideAll = 0;
            view.Enabled = false;
            module2.RefreshSwitches();
            check(hudInstalled.Count == 0 && !module2.HudPatchInstalled && hideAll == 1,
                "开关 off = 注销 + HideAll（原版数字回到裸态）");
            // DEV-V6-12：off = 经句柄撤销（同一拆除动作，不双拆）；登记是代际级的——
            // 账上仍恰一条账项、零已拆句柄（句柄在边界才释放）。
            var toggleHandle = module2.PatchRegistration;
            check(toggleHandle != null && ReferenceEquals(module2.PatchRegistration, toggleHandle)
                    && V56Pocket.LiveCount == 1 && V56Pocket.GhostCount == 0 && V56Pocket.ReleasedCount == 0,
                "开关 off：撤销经句柄、登记不动（一代一条账项、零已拆句柄），实际 live=" + V56Pocket.LiveCount
                + " ghost=" + V56Pocket.GhostCount + " accountReleases=" + V56Pocket.ReleasedCount);
            view.Enabled = true;
            module2.RefreshSwitches();
            check(hudInstalled.Count == 1 && hudInstalled[0] == typeof(AmmoReserveHudPatch) && module2.HudPatchInstalled,
                "开关 on = 重登记（同代际重挂，无第三次握手残留）");
            check(ReferenceEquals(module2.PatchRegistration, toggleHandle) && V56Pocket.Accepted.Count == 1
                    && V56Pocket.LiveCount == 1,
                "开关 on：重新武装而不新增账项（账与武装状态一致），实际 live=" + V56Pocket.LiveCount
                + " accepted=" + V56Pocket.Accepted.Count);
            module2.Stop(FeatureStopReason.PluginStopping);

            // 4e. 冷关：停用状态下 Start 根本不武装。
            var coldView = new V56SettingsView();
            coldView.Enabled = false;
            hudInstalled.Clear();
            var cold = V56StartModule(coldView, check);
            check(hudInstalled.Count == 0 && !cold.HudPatchInstalled && InPlaceReloadModule.ActiveModule == null,
                "冷关 Start：HUD 面不登记（开关在生命周期外层，补丁体无第二把锁）");
            check(cold.PatchRegistration == null && V56Pocket.Accepted.Count == 0,
                "冷关 Start：零句柄进账（关闭态不虚占账位）");
            cold.Stop(FeatureStopReason.PluginStopping);

            // 4f. U3DS headless：画面闸不挡功能——HUD 面不武装 + 诚实诊断。
            LirRuntime.HostHeadlessDecision = () => true; // DEV-V6-02C: 经注入缝模拟 headless
            hudInstalled.Clear();
            var headless = V56StartModule(new V56SettingsView(), check);
            check(hudInstalled.Count == 0 && !headless.HudPatchInstalled && headless.Started,
                "U3DS：弹药 HUD 不武装（画面类，T1 Headless 裁决只砍画面）");
            check(headless.HudStartGateDiagnostics == "ammo-hud-headless-not-armed",
                "headless 跳过登记 = 结构化诊断，不是静默");
            check(headless.PatchRegistration != null && V56Pocket.Accepted.Count == 1,
                "headless：权威压弹面的句柄照进平台账（缺画面不减所有权移交）");
            headless.Stop(FeatureStopReason.PluginStopping);
            LirRuntime.HostHeadlessDecision = null;

            // 4g. 半装互撤：HUD 面被拒 = 全面回滚（不留半装；登记交还、在册归零）。
            InPlaceReloadModule.HudPatchInstallerForTests = t => false;
            var half = V56StartModule(new V56SettingsView(), check);
            check(!half.PatchesInstalled && !half.HudPatchInstalled && InPlaceReloadModule.ActiveModule == null,
                "HUD 拒装 = 三面（两压弹面+HUD 面）互撤归零，禁半装");
            check(half.StartGateDiagnostics.Contains("ammo-hud patch refused"), "拒装原因落结构化诊断");
            check(half.PatchRegistration == null && V56Pocket.Accepted.Count == 0,
                "HUD 拒装：零句柄进账（平台账外零补丁，武装事务终止）");
            half.Stop(FeatureStopReason.PluginStopping);

            // 4h. 补丁面无自身功能 bool（登记=唯一开关的反证面：类型里找不到开关位）。
            check(V56NoSwitchMembers(typeof(AmmoReserveHudAdapter), check) &&
                  V56NoSwitchMembers(typeof(AmmoReserveHudPatch), check) &&
                  V56NoSwitchMembers(typeof(AmmoReserveHudSurface), check),
                "adapter/patch/surface 无 Enabled/Disabled 类成员（无 Prefix 内死开关可复活）");
        }

        // ─────────────────────────────────────────────────────────────────
        // 组5 — 官方先行消费与 binder 宿主可证：绑定唯一事实源 = 显式解析
        // UseableGun.updateInfo（多重载/漂移即抛，不静默绑错）；匹配主路径
        // supplies 集合 = 压弹服务单源（internal 共享面）；fallback 谓词纯函数
        // 双侧同消费；postfix 签名面。
        // ─────────────────────────────────────────────────────────────────
        private static void V56GroupOfficialFirst(System.Action<bool, string> check)
        {
            // 5a. binder：恰一面、目标 = UseableGun.updateInfo 私有零参、唯一命中。
            check(AmmoReserveHudBinder.PatchSurface.Count == 1 && AmmoReserveHudBinder.PatchSurface[0] == typeof(AmmoReserveHudPatch),
                "绑定面清单单源：恰 AmmoReserveHudPatch 一面");
            var target = AmmoReserveHudBinder.ResolveInfoUpdateTarget();
            check(target != null && target.DeclaringType == typeof(UseableGun) && target.Name == "updateInfo"
                    && target.GetParameters().Length == 0,
                "绑定目标 = SDG.Unturned.UseableGun.updateInfo（私有零参，宿主可证唯一）");
            var post = AccessTools.Method(typeof(AmmoReserveHudPatch), "Postfix");
            var ps = post == null ? null : post.GetParameters();
            check(post != null && post.IsStatic && ps.Length == 1 && ps[0].ParameterType == typeof(object),
                "postfix 面：static Postfix(object __instance)（不提前 JIT 引擎类型）");

            // 5b. 匹配同源：压弹服务的 supplies 集合收集升为共享面（HUD 引擎侧消费）。
            var collect = AccessTools.Method(typeof(AmmoRepackService), "CollectCompatibleAmmoIds");
            check(collect != null && collect.IsStatic && collect.IsAssembly
                    && collect.GetParameters().Length == 1
                    && collect.GetParameters()[0].ParameterType == typeof(ItemMagazineAsset)
                    && collect.ReturnType == typeof(List<ushort>),
                "CollectCompatibleAmmoIds = internal 共享单源（禁 HUD 复制第二套匹配）");

            // 5c. fallback 交集谓词纯函数边界：null/空双侧不配；零跳过。
            check(!AmmoReserveProjection.MagSuppliesMatch(null, new ushort[] { 1 }) &&
                  !AmmoReserveProjection.MagSuppliesMatch(new ushort[] { 1 }, null) &&
                  !AmmoReserveProjection.MagSuppliesMatch(new ushort[0], new ushort[] { 1 }),
                "fallback 谓词：任一侧 null/空 = 不配（现图 FindCaliberMatch 闸同）");
        }

        // ─────────────────────────────────────────────────────────────────
        // 夹具
        // ─────────────────────────────────────────────────────────────────
        private static AmmoReserveEntry V56Mag(byte page, ushort id, byte amount, ushort caliber, ushort max = 30, bool unspecifiedCaliber = false)
        {
            return new AmmoReserveEntry
            {
                Page = page,
                Id = id,
                IsMagazine = true,
                IsCaliberAsset = true,
                Amount = amount,
                MaxAmount = (byte)max,
                Calibers = unspecifiedCaliber ? new ushort[0] : new ushort[] { caliber },
            };
        }

        private static AmmoReserveEntry V56Box(byte page, ushort id, byte amount, ushort[] calibers = null)
        {
            return new AmmoReserveEntry
            {
                Page = page,
                Id = id,
                IsMagazine = false,
                IsCaliberAsset = calibers != null,
                Amount = amount,
                MaxAmount = 120,
                Calibers = calibers,
            };
        }

        private static AmmoReserveEntry V56BoxEntry(byte page, ushort id, byte amount)
        {
            return new AmmoReserveEntry
            {
                Page = page,
                Id = id,
                IsMagazine = false,
                IsCaliberAsset = false,
                Amount = amount,
                MaxAmount = 120,
                Calibers = null,
            };
        }

        /// <summary>反射面：类型上不存在 Enabled/Disabled 语义的功能 bool 成员（登记=唯一开关的反证）。</summary>
        private static bool V56NoSwitchMembers(Type type, System.Action<bool, string> check)
        {
            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var m in members)
            {
                if (m.MemberType != MemberTypes.Field && m.MemberType != MemberTypes.Property) continue;
                var name = m.Name;
                if (name == null) continue;
                if (name.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("HudOn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    check(false, "补丁面类型含自身功能开关成员：" + type.Name + "." + name);
                    return false;
                }
            }
            return true;
        }

        /// <summary>本组最近一次 Start 用的启动口袋（DEV-V6-12：宿主已给口袋=等价起点；
        /// 断言「恰一枚句柄进账 / 开关后账与武装状态一致」用）。</summary>
        private static LirTestPocket V56Pocket;

        private static InPlaceReloadModule V56StartModule(V56SettingsView view, System.Action<bool, string> check, bool isServer = false)
        {
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var feature = new FeatureId(LirRuntime.FeatureIdValue);
            var module = new InPlaceReloadModule(feature);
            module.AuthorityFactoryForTests = () => new V56Authority();
            module.NetServiceFactoryForTests = (m, net) => new LirRepackNetwork(net, m.Authority, () => isServer);
            module.KeyDownProviderForTests = () => false;
            module.RoleProbeForTests = () => false;
            // DEV-V6-12：本票之后武装 ⇒ 经启动口袋登记（缺口袋=立即自拆），老套件按生产
            // 组合同形地给真账户（真登记目录 + 真代际机 + 真补丁口视图）。
            V56Pocket = LirTestPocket.Open();
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), V56Pocket.Generation, view,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, new V56Network(),
                null, V56Pocket.Patching);
            var result = module.Start(bootstrap);
            check(result.Started, "harness: LIR 模块 Start 失败: " + result.DiagnosticId);
            return module;
        }

        private sealed class V56SettingsView : IScopedFeatureSettings
        {
            public bool Enabled = true;
            public uint Revision = 7U;

            public FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)
            {
                return new FeatureSettingsSnapshot(new FeatureId(LirRuntime.FeatureIdValue), 1U,
                    revisionScope, Revision, SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, new List<SettingEntryView>());
            }

            public bool TryGet(string settingId, out SettingValue value, out uint revision)
            {
                value = default(SettingValue);
                revision = Revision;
                if (settingId != "inplacereload.enabled") return false; // 冻结 setting id（04/05 迁移别名同源）
                value = SettingValue.Toggle(Enabled);
                return true;
            }

            public SettingChangeResult Submit(ScopedSettingChangeRequest request)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, Revision, default(FeatureSettingsSnapshot));
            }
        }

        private sealed class V56Authority : ILirRepackAuthority
        {
            public LirRepackExecution ExecuteRepack(ulong senderSteamId, ulong requestId, bool hostInitiated = false)
            { return new LirRepackExecution { Outcome = LirRepackOutcome.NoChange, TotalTransferred = 0 }; }

            public LirMergeExecution ExecuteMerge(ulong targetSteamId, ulong requestId)
            { return new LirMergeExecution { Outcome = LirMergeOutcome.NoChange, TotalMerged = 0 }; }

            public bool TryResolveLocalPlayerSteamId(out ulong steamId)
            { steamId = 0UL; return false; }
        }

        private sealed class V56Network : IBueNetworkApi
        {
            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "V56"); }

            public bool UnregisterChannel(FeatureId channel) { return true; }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            { return new V56Handle(); }

            public IReadOnlyList<IConnectionSession> Sessions { get { return new IConnectionSession[0]; } }
            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }

            private sealed class V56Handle : IDisposable { public void Dispose() { } }
        }
    }
}
