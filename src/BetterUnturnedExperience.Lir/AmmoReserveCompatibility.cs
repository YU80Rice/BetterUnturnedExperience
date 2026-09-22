namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// Compatibility DTOs retained for the V5 host harness. Production HUD
    /// computation lives in AmmoObservation.cs and returns AmmoTotalResult.
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

    internal struct AmmoReserveLoadedMagazine
    {
        internal ushort Id;
        internal ushort[] Calibers;
        internal ushort[] FillSupplyIds;
    }

    internal struct AmmoReserveObservation
    {
        internal ushort[] GunMagazineCalibers;
        internal bool GunAllowsZeroCaliber;
        internal AmmoReserveLoadedMagazine? LoadedMagazine;
        internal AmmoReserveEntry[] Entries;
    }

    internal struct AmmoReserveResult
    {
        internal int SpareMagCount;
        internal int ReserveRounds;
        internal string LabelText;
    }

    /// <summary>
    /// Legacy V5 projection adapter. It is not used by the V7 production HUD;
    /// it remains only so historical lifecycle fixtures can be migrated without
    /// duplicating the V7 matching predicate.
    /// </summary>
    internal static class AmmoReserveProjection
    {
        internal const string LabelFormat = "备匣 {0} · 备弹 {1}";

        internal static AmmoReserveResult Project(AmmoReserveObservation observation)
        {
            var spare = 0;
            var reserve = 0;
            var entries = observation.Entries;
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry.Page < ReloadRuntimePolicy.MinRepackPage || entry.Page > ReloadRuntimePolicy.MaxRepackPage) continue;
                    if (!entry.IsMagazine || entry.Amount <= 0) continue;
                    if (!AmmoTotalProjection.CalibersMatchWeapon(entry.Calibers, observation.GunMagazineCalibers, observation.GunAllowsZeroCaliber)) continue;
                    spare++;
                    reserve += entry.Amount;
                }
                var loaded = observation.LoadedMagazine;
                if (loaded.HasValue)
                {
                    var anyMain = false;
                    var main = 0;
                    var fallback = 0;
                    for (var i = 0; i < entries.Length; i++)
                    {
                        var entry = entries[i];
                        if (entry.Page < ReloadRuntimePolicy.MinRepackPage || entry.Page > ReloadRuntimePolicy.MaxRepackPage || entry.IsMagazine || entry.Amount <= 0 || entry.MaxAmount <= 0) continue;
                        if (Contains(loaded.Value.FillSupplyIds, entry.Id))
                        {
                            anyMain = true;
                            main += entry.Amount;
                        }
                        else if (entry.IsCaliberAsset && AmmoTotalProjection.MagSuppliesMatch(loaded.Value.Calibers, entry.Calibers))
                        {
                            fallback += entry.Amount;
                        }
                    }
                    reserve += anyMain ? main : fallback;
                }
            }
            return new AmmoReserveResult
            {
                SpareMagCount = spare,
                ReserveRounds = reserve,
                LabelText = string.Format(LabelFormat, spare, reserve),
            };
        }

        internal static bool CalibersMatchWeapon(ushort[] magazineCalibers, ushort[] gunMagazineCalibers, bool allowZeroCaliber)
        {
            return AmmoTotalProjection.CalibersMatchWeapon(magazineCalibers, gunMagazineCalibers, allowZeroCaliber);
        }

        internal static bool MagSuppliesMatch(ushort[] magazineCalibers, ushort[] sourceCalibers)
        {
            return AmmoTotalProjection.MagSuppliesMatch(magazineCalibers, sourceCalibers);
        }

        private static bool Contains(ushort[] values, ushort value)
        {
            if (values == null) return false;
            for (var i = 0; i < values.Length; i++) if (values[i] == value) return true;
            return false;
        }
    }
}
