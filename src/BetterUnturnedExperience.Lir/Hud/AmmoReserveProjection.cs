namespace BetterUnturnedExperience.Lir
{
    using System.Collections.Generic;

    /// <summary>
    /// DEV-V5-06 观察条目：身上页一件物品的后备读数相关事实（引擎侧扁平化，
    /// 页范围仍由投影按 ReloadRuntimePolicy 的 2..6 闸复验——「不含容器/地面」
    /// 在本 seam 可测，不只是引擎侧的自律）。Calibers 只有口径资产才有；
    /// 弹匣是 ItemCaliberAsset 的子类，IsMagazine 与 IsCaliberAsset 同真，
    /// 弹药源半边以 IsMagazine 先排除（现网同律：匣永不进 ammoBoxMap）。
    /// </summary>
    internal struct AmmoReserveEntry
    {
        internal byte Page;
        internal ushort Id;
        internal bool IsMagazine;
        internal bool IsCaliberAsset;
        internal byte Amount;
        internal byte MaxAmount;
        internal ushort[] Calibers;
    }

    /// <summary>
    /// 枪上那本弹匣的描述——刻意没有任何弹量字段：当前匣发数属于原版
    /// 「当前/上限」半边，结构上不可能混进后备（票面「不含枪上那本」的
    /// 保证不靠约定靠类型；红测 1f 反射钉死）。Calibers/FillSupplyIds 是
    /// 「箱能否给当前匣供弹」两路匹配的输入；FillSupplyIds 由压弹服务的
    /// CollectCompatibleAmmoIds 单源收集（匹配同现网压弹）。
    /// </summary>
    internal struct AmmoReserveLoadedMagazine
    {
        internal ushort Id;
        internal ushort[] Calibers;
        internal ushort[] FillSupplyIds;
    }

    /// <summary>
    /// 一次投影视图：当前武器口径信号（原版单击 R 搜索同源：
    /// ItemGunAsset.magazineCalibers + allowZeroCaliber）、枪上匣（可无）、
    /// 身上页条目（引擎侧只喂 2..6；投影再闸一次做纵深）。
    /// </summary>
    internal struct AmmoReserveObservation
    {
        internal ushort[] GunMagazineCalibers;
        internal bool GunAllowsZeroCaliber;
        internal AmmoReserveLoadedMagazine? LoadedMagazine;
        internal AmmoReserveEntry[] Entries;
    }

    /// <summary>投影结果：N 备用匣本数、M 后备总发数、冻结文案（0/0 不早退——持枪即在）。</summary>
    internal struct AmmoReserveResult
    {
        internal int SpareMagCount;
        internal int ReserveRounds;
        internal string LabelText;
    }

    /// <summary>
    /// DEV-V5-06 弹药后备 HUD 的纯投影深模块——零引擎类型，宿主可测的唯一
    /// 事实源。规则一手出处（全部逐字复刻，不发明）：
    ///   N = 身上五页（policy 2..6）∧ 弹匣 ∧ amount&gt;0 ∧ 与当前武器口径匹配
    ///       的本数；口径匹配 = vanilla Items.SearchContents 的 MAGAZINE 搜索
    ///       语义（空口径看 allowZeroCaliber；有口径则与枪口径集无零跳过求交，
    ///       CalibersContainAnyOfIds 逐字）。不含枪上那本（类型无弹量输入）、
    ///       不含空匣、不含容器/地面（页闸）。
    ///   M = Σ 这些匣余弹 + Σ 能给当前匣供弹的箱发数。箱侧规则同现网压弹
    ///       （AmmoRepackService.BuildRepackPlan）：弹药源 = 非弹匣 ∧ amount&gt;0
    ///       ∧ MaxAmount&gt;0；主路径 = 箱 id ∈ 当前匣 FillTargetItem supplies 集；
    ///       主路径零候选 ∧ 当前匣有口径 → 仅口径资产参与交集 fallback，
    ///       且 mag 侧零口径跳过（FindCaliberMatch 逐字）。主路径命中即整体
    ///       排除 fallback（candidateBoxLists 律）。无当前匣/无匹配 → 箱侧恒 0。
    /// 本阶段不含超限/技能状态读数（HUD 与等级分层——T7；等级归 07）。
    /// </summary>
    internal static class AmmoReserveProjection
    {
        /// <summary>冻结文案单源（红测逐字钉；漂一个字即红）。紧凑可读：左列小字。</summary>
        internal const string LabelFormat = "备匣 {0} · 备弹 {1}";

        internal static AmmoReserveResult Project(AmmoReserveObservation obs)
        {
            var entries = obs.Entries;
            var spareCount = 0;
            var reserve = 0;
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    if (e.Page < ReloadRuntimePolicy.MinRepackPage || e.Page > ReloadRuntimePolicy.MaxRepackPage) continue;
                    if (e.IsMagazine)
                    {
                        if (e.Amount <= 0) continue;
                        if (!CalibersMatchWeapon(e.Calibers, obs.GunMagazineCalibers, obs.GunAllowsZeroCaliber)) continue;
                        spareCount++;
                        reserve += e.Amount;
                    }
                }
                reserve += ProjectAmmoBoxes(obs, entries);
            }
            return new AmmoReserveResult
            {
                SpareMagCount = spareCount,
                ReserveRounds = reserve,
                LabelText = string.Format(LabelFormat, spareCount, reserve),
            };
        }

        /// <summary>
        /// 箱侧两遍判定（主路径优先、fallback 仅在主路径零候选时启用——
        /// 现网 candidateBoxLists.Count==0 闸）：发数与扣量序无关，故不做
        /// 顺序模拟，只做集合与求和。
        /// </summary>
        private static int ProjectAmmoBoxes(AmmoReserveObservation obs, AmmoReserveEntry[] entries)
        {
            var loaded = obs.LoadedMagazine;
            if (!loaded.HasValue) return 0;
            var fillIds = loaded.Value.FillSupplyIds;
            var mainSum = 0;
            var fallbackSum = 0;
            var anyMain = false;
            for (var i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.IsMagazine) continue;                       // 匣永不进弹药源（现网 ammoBoxMap 律）
                if (e.Page < ReloadRuntimePolicy.MinRepackPage || e.Page > ReloadRuntimePolicy.MaxRepackPage) continue;
                if (e.Amount <= 0 || e.MaxAmount <= 0) continue;  // 现网：amount!=0 ∧ MaxAmountAsByte!=0
                if (Contains(fillIds, e.Id))
                {
                    mainSum += e.Amount;
                    anyMain = true;
                    continue;
                }
                // fallback 候选照常收集；选用规则 = 主路径零候选（返回处），与现网同律。
                if (e.IsCaliberAsset && MagSuppliesMatch(loaded.Value.Calibers, e.Calibers))
                {
                    fallbackSum += e.Amount;
                }
            }
            return anyMain ? mainSum : fallbackSum;
        }

        /// <summary>N 侧口径闸——vanilla Items.SearchContents 的弹匣搜索逐字复刻。</summary>
        internal static bool CalibersMatchWeapon(ushort[] magazineCalibers, ushort[] gunMagazineCalibers, bool allowZeroCaliber)
        {
            if (magazineCalibers == null || magazineCalibers.Length == 0) return allowZeroCaliber;
            if (gunMagazineCalibers == null || gunMagazineCalibers.Length == 0) return false;
            for (var g = 0; g < gunMagazineCalibers.Length; g++)
            {
                for (var m = 0; m < magazineCalibers.Length; m++)
                {
                    if (magazineCalibers[m] == gunMagazineCalibers[g]) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// fallback 交集谓词 = 压弹服务与 HUD 共用的单源（AmmoRepackService.
        /// FindCaliberMatch 也调用本方法——「匹配同现网压弹」由代码复用保证，
        /// 不靠两份实现互相对表）。语义 = 现网 FindCaliberMatch 内层循环逐字：
        /// 当前匣口径为 0 永不配箱；双侧 null/空 = 不配。
        /// </summary>
        internal static bool MagSuppliesMatch(ushort[] magazineCalibers, ushort[] sourceCalibers)
        {
            if (magazineCalibers == null || magazineCalibers.Length == 0) return false;
            if (sourceCalibers == null || sourceCalibers.Length == 0) return false;
            for (var a = 0; a < magazineCalibers.Length; a++)
            {
                var magCal = magazineCalibers[a];
                if (magCal == 0) continue;
                for (var b = 0; b < sourceCalibers.Length; b++)
                {
                    if (magCal == sourceCalibers[b]) return true;
                }
            }
            return false;
        }

        private static bool Contains(ushort[] ids, ushort id)
        {
            if (ids == null) return false;
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] == id) return true;
            }
            return false;
        }
    }
}
