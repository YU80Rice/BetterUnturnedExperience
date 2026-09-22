using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// LIR-owned ammo observation facts. This file is deliberately HUD-free so
    /// reload automation can consume the same observation without depending on
    /// a presentation lifecycle.
    /// </summary>
    internal struct AmmoEntry
    {
        internal byte Page;
        internal ushort Id;
        internal bool IsMagazine;
        internal bool IsCaliberAsset;
        internal byte Amount;
        internal byte MaxAmount;
        internal ushort[] Calibers;
    }

    internal struct LoadedAmmoMagazine
    {
        internal ushort Id;
        internal int CurrentAmmo;
        internal ushort[] Calibers;
        internal ushort[] FillSupplyIds;
    }

    internal struct AmmoObservation
    {
        internal ushort[] GunMagazineCalibers;
        internal bool GunAllowsZeroCaliber;
        internal LoadedAmmoMagazine? LoadedMagazine;
        internal AmmoEntry[] Entries;
    }

    internal struct AmmoTotalResult
    {
        internal int TotalAmmo;
        internal string LabelText;
    }

    /// <summary>
    /// The single production source for inventory matching and total-ammo math.
    /// It has no reference to HUD adapters, surfaces, binders, or lifecycle types.
    /// </summary>
    internal static class AmmoTotalProjection
    {
        internal const string LabelFormat = "总弹药 {0}";

        internal static AmmoTotalResult Project(AmmoObservation observation)
        {
            var total = observation.LoadedMagazine.HasValue
                ? Math.Max(0, observation.LoadedMagazine.Value.CurrentAmmo)
                : 0;
            var entries = observation.Entries;
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry.Page < ReloadRuntimePolicy.MinRepackPage || entry.Page > ReloadRuntimePolicy.MaxRepackPage)
                        continue;
                    if (entry.IsMagazine)
                    {
                        if (entry.Amount <= 0 || !CalibersMatchWeapon(entry.Calibers, observation.GunMagazineCalibers, observation.GunAllowsZeroCaliber))
                            continue;
                        total += entry.Amount;
                    }
                }
                total += ProjectAmmoBoxes(observation, entries);
            }
            return new AmmoTotalResult
            {
                TotalAmmo = total,
                LabelText = string.Format(LabelFormat, total),
            };
        }

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

        /// <summary>Fallback predicate shared by the HUD and transactional repack service.</summary>
        internal static bool MagSuppliesMatch(ushort[] magazineCalibers, ushort[] sourceCalibers)
        {
            if (magazineCalibers == null || magazineCalibers.Length == 0) return false;
            if (sourceCalibers == null || sourceCalibers.Length == 0) return false;
            for (var a = 0; a < magazineCalibers.Length; a++)
            {
                var magazineCaliber = magazineCalibers[a];
                if (magazineCaliber == 0) continue;
                for (var b = 0; b < sourceCalibers.Length; b++)
                {
                    if (magazineCaliber == sourceCalibers[b]) return true;
                }
            }
            return false;
        }

        internal static string BuildFingerprint(AmmoObservation observation)
        {
            var parts = new List<string>();
            var loaded = observation.LoadedMagazine;
            if (loaded.HasValue)
            {
                parts.Add(loaded.Value.Id.ToString());
                parts.Add(loaded.Value.CurrentAmmo.ToString());
            }
            else
            {
                parts.Add("none");
            }
            var entries = observation.Entries;
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    if (e.Page < ReloadRuntimePolicy.MinRepackPage || e.Page > ReloadRuntimePolicy.MaxRepackPage) continue;
                    parts.Add(e.Page + ":" + e.Id + ":" + e.Amount + ":" + e.MaxAmount + ":" + (e.IsMagazine ? "m" : "b"));
                }
            }
            return string.Join("|", parts.ToArray());
        }

        private static int ProjectAmmoBoxes(AmmoObservation observation, AmmoEntry[] entries)
        {
            var loaded = observation.LoadedMagazine;
            if (!loaded.HasValue) return 0;
            var mainSum = 0;
            var fallbackSum = 0;
            var anyMain = false;
            var fillIds = loaded.Value.FillSupplyIds;
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry.Page < ReloadRuntimePolicy.MinRepackPage || entry.Page > ReloadRuntimePolicy.MaxRepackPage) continue;
                if (entry.IsMagazine || entry.Amount <= 0 || entry.MaxAmount <= 0) continue;
                if (Contains(fillIds, entry.Id))
                {
                    anyMain = true;
                    mainSum += entry.Amount;
                    continue;
                }
                if (entry.IsCaliberAsset && MagSuppliesMatch(loaded.Value.Calibers, entry.Calibers))
                    fallbackSum += entry.Amount;
            }
            return anyMain ? mainSum : fallbackSum;
        }

        private static bool Contains(ushort[] values, ushort value)
        {
            if (values == null) return false;
            for (var i = 0; i < values.Length; i++)
                if (values[i] == value) return true;
            return false;
        }
    }
}
