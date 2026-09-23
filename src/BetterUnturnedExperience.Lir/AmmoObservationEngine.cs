using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// Thin engine adapter. It only reads vanilla gun/inventory facts and maps
    /// them to the HUD-free AmmoObservation source; matching and arithmetic are
    /// owned by AmmoTotalProjection.
    /// </summary>
    internal sealed class AmmoEngineFacts
    {
        internal ushort[] GunMagazineCalibers;
        internal bool GunAllowsZeroCaliber;
        internal ushort LoadedMagazineId;
        internal byte LoadedCurrentAmmo;
        internal ushort[] LoadedMagazineCalibers;
        internal ushort[] LoadedFillSupplyIds;
        internal AmmoEntry[] Entries;
    }

    internal static class AmmoObservationEngine
    {
        // Host-test seams replace only raw engine facts, never DTO assembly or
        // adapter orchestration. Production leaves these null and uses SDG.
        internal static Func<object, AmmoEngineFacts> FactsReaderForTests;
        internal static Func<object> LocalGunForTests;
        private static FieldInfo ammoField;
        private static bool ammoFieldProbed;

        private static byte ReadVanillaAmmo(UseableGun gun, byte[] state)
        {
            if (!ammoFieldProbed)
            {
                ammoFieldProbed = true;
                ammoField = AccessTools.Field(typeof(UseableGun), "ammo");
            }
            if (ammoField != null)
            {
                try
                {
                    var value = ammoField.GetValue(gun);
                    if (value is byte byteValue) return byteValue;
                }
                catch (Exception error)
                {
                    LirRuntime.LogDiagnostic("[AmmoHud] 原版 ammo 字段读取失败，回退 state[AMMO]: " + error.Message);
                }
            }
            return state != null && state.Length > GunStateIndices.AMMO ? state[GunStateIndices.AMMO] : (byte)0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object ResolveLocalGun()
        {
            var testReader = LocalGunForTests;
            if (testReader != null) return testReader();
            try
            {
                var local = Player.LocalPlayer;
                var useable = local == null || local.equipment == null ? null : local.equipment.useable;
                return useable as UseableGun;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AmmoObservation ObserveTotal(object gunInstance)
        {
            var factsReader = FactsReaderForTests;
            if (factsReader != null)
            {
                var facts = factsReader(gunInstance);
                return facts == null ? new AmmoObservation { Entries = new AmmoEntry[0] } : FromFacts(facts);
            }
            var entries = new List<AmmoEntry>();
            var observation = new AmmoObservation
            {
                GunMagazineCalibers = null,
                GunAllowsZeroCaliber = false,
                LoadedMagazine = null,
                Entries = new AmmoEntry[0],
            };
            var gun = gunInstance as UseableGun;
            if (gun == null) return observation;
            var player = gun.player;
            if (player == null) return observation;

            var equipment = player.equipment;
            var gunAsset = equipment == null ? null : equipment.asset as ItemGunAsset;
            if (gunAsset != null)
            {
                observation.GunMagazineCalibers = gunAsset.magazineCalibers;
                observation.GunAllowsZeroCaliber = !gunAsset.requiresNonZeroAttachmentCaliber;
                var state = equipment.state;
                if (state != null)
                {
                    var currentAmmo = ReadVanillaAmmo(gun, state);
                    if (state.Length > GunStateIndices.MAGAZINE_ID + 1)
                    {
                        var magazineId = BitConverter.ToUInt16(state, GunStateIndices.MAGAZINE_ID);
                        if (magazineId != 0)
                        {
                            var magazineAsset = Assets.find(EAssetType.ITEM, magazineId) as ItemMagazineAsset;
                            if (magazineAsset != null)
                            {
                                observation.LoadedMagazine = new LoadedAmmoMagazine
                                {
                                    Id = magazineId,
                                    CurrentAmmo = currentAmmo,
                                    Calibers = magazineAsset.calibers,
                                    FillSupplyIds = AmmoRepackService.CollectCompatibleAmmoIds(magazineAsset).ToArray(),
                                };
                            }
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
                    var items = inventory.items[page];
                    if (items == null) continue;
                    var count = items.getItemCount();
                    for (byte i = 0; i < count; i++)
                    {
                        var jar = items.getItem(i);
                        var item = jar == null ? null : jar.item;
                        if (item == null) continue;
                        var asset = item.GetAsset();
                        if (asset == null) continue;
                        var caliberAsset = asset as ItemCaliberAsset;
                        entries.Add(new AmmoEntry
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
            observation.Entries = entries.ToArray();
            return observation;
        }

        private static AmmoObservation FromFacts(AmmoEngineFacts facts)
        {
            return new AmmoObservation
            {
                GunMagazineCalibers = facts.GunMagazineCalibers,
                GunAllowsZeroCaliber = facts.GunAllowsZeroCaliber,
                LoadedMagazine = facts.LoadedMagazineId == 0 ? (LoadedAmmoMagazine?)null : new LoadedAmmoMagazine
                {
                    Id = facts.LoadedMagazineId,
                    CurrentAmmo = facts.LoadedCurrentAmmo,
                    Calibers = facts.LoadedMagazineCalibers,
                    FillSupplyIds = facts.LoadedFillSupplyIds,
                },
                Entries = facts.Entries ?? new AmmoEntry[0],
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AmmoReserveObservation Observe(object gunInstance)
        {
            var total = ObserveTotal(gunInstance);
            var entries = new AmmoReserveEntry[total.Entries == null ? 0 : total.Entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = total.Entries[i];
                entries[i] = new AmmoReserveEntry
                {
                    Page = entry.Page,
                    Id = entry.Id,
                    IsMagazine = entry.IsMagazine,
                    IsCaliberAsset = entry.IsCaliberAsset,
                    Amount = entry.Amount,
                    MaxAmount = entry.MaxAmount,
                    Calibers = entry.Calibers,
                };
            }
            AmmoReserveLoadedMagazine? loaded = null;
            if (total.LoadedMagazine.HasValue)
            {
                var value = total.LoadedMagazine.Value;
                loaded = new AmmoReserveLoadedMagazine
                {
                    Id = value.Id,
                    Calibers = value.Calibers,
                    FillSupplyIds = value.FillSupplyIds,
                };
            }
            return new AmmoReserveObservation
            {
                GunMagazineCalibers = total.GunMagazineCalibers,
                GunAllowsZeroCaliber = total.GunAllowsZeroCaliber,
                LoadedMagazine = loaded,
                Entries = entries,
            };
        }
    }
}
