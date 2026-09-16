using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-06 引擎观察面（真机 only，NoInlining 隔离——宿主测试进程永不
    /// JIT 本类方法体，ECall 纪律同 04/05）。职责只有一件：把「当前武器、枪上
    /// 那本匣、身上五页条目」读成 AmmoReserveObservation 纯 DTO——判定规则
    /// 全在 AmmoReserveProjection（单源），本类零语义。
    /// 一手出处：枪上匣 id = equipment.state[GunStateIndices.MAGAZINE_ID..+1]
    /// （装在枪上的弹匣不是背包 Item，vanilla Attachments.parseFromItemState
    /// 同源）；武器口径集 = ItemGunAsset.magazineCalibers +
    /// allowZeroCaliber = !requiresNonZeroAttachmentCaliber（原版单击 R 搜索
    /// 参数同源）；供弹主路径集合 = AmmoRepackService.CollectCompatibleAmmoIds
    /// （「匹配同现网压弹」由调用同一函数保证）；条目只喂 policy 2..6
    /// （投影还会复验页范围——纵深）。
    /// </summary>
    internal static class AmmoReserveHudEngine
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AmmoReserveObservation Observe(object gunInstance)
        {
            var entries = new List<AmmoReserveEntry>();
            var obs = new AmmoReserveObservation
            {
                GunMagazineCalibers = null,
                GunAllowsZeroCaliber = false,
                LoadedMagazine = null,
                Entries = new AmmoReserveEntry[0],
            };
            var gun = gunInstance as UseableGun;
            if (gun == null) return obs;
            var player = gun.player;
            if (player == null) return obs;

            var gunAsset = player.equipment == null ? null : player.equipment.asset as ItemGunAsset;
            if (gunAsset != null)
            {
                obs.GunMagazineCalibers = gunAsset.magazineCalibers;
                obs.GunAllowsZeroCaliber = !gunAsset.requiresNonZeroAttachmentCaliber;
                var state = player.equipment.state;
                if (state != null && state.Length > GunStateIndices.MAGAZINE_ID + 1)
                {
                    var magazineId = BitConverter.ToUInt16(state, GunStateIndices.MAGAZINE_ID);
                    if (magazineId != 0)
                    {
                        var magazineAsset = Assets.find(EAssetType.ITEM, magazineId) as ItemMagazineAsset;
                        if (magazineAsset != null)
                        {
                            obs.LoadedMagazine = new AmmoReserveLoadedMagazine
                            {
                                Id = magazineId,
                                Calibers = magazineAsset.calibers,
                                FillSupplyIds = AmmoRepackService.CollectCompatibleAmmoIds(magazineAsset).ToArray(),
                            };
                        }
                    }
                }
            }

            var inventory = player.inventory;
            if (inventory != null && inventory.items != null)
            {
                for (byte page = ReloadRuntimePolicy.MinRepackPage; page <= ReloadRuntimePolicy.MaxRepackPage; page++)
                {
                    if (page >= inventory.items.Length) break;
                    Items items = inventory.items[page];
                    if (items == null) continue;
                    byte count = items.getItemCount();
                    for (byte i = 0; i < count; i++)
                    {
                        ItemJar jar = items.getItem(i);
                        Item item = jar == null ? null : jar.item;
                        if (item == null) continue;
                        ItemAsset asset = item.GetAsset();
                        if (asset == null) continue;
                        var caliberAsset = asset as ItemCaliberAsset;
                        entries.Add(new AmmoReserveEntry
                        {
                            Page = page,
                            Id = item.id,
                            IsMagazine = asset is ItemMagazineAsset,
                            IsCaliberAsset = caliberAsset != null,
                            Amount = item.amount,
                            MaxAmount = asset.MaxAmountAsByte,
                            Calibers = caliberAsset == null ? null : caliberAsset.calibers,
                        });
                    }
                }
            }
            obs.Entries = entries.ToArray();
            return obs;
        }
    }
}
