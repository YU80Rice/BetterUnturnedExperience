using System;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// HUD consumer boundary. V7 production consumes the HUD-free ammo source;
    /// legacy V5 seams remain only for the existing lifecycle harness.
    /// </summary>
    internal static class AmmoReserveHudAdapter
    {
        internal static Func<object, AmmoReserveObservation> ScannerForTests;
        internal static Action<object, AmmoReserveResult> ApplyForTests;
        internal static Func<object, AmmoObservation> TotalScannerForTests;
        internal static Action<object, AmmoTotalResult> TotalApplyForTests;
        internal static Action HideAllForTests;

        private static object lastGun;
        private static string lastFingerprint;

        internal static void OnGunInfoUpdated(object gun)
        {
            var module = InPlaceReloadModule.ActiveModule;
            if (module == null || !module.Started || module.ShuttingDown) return;
            try
            {
                // Existing V5 seams are retained for historical tests only.
                var legacyScanner = ScannerForTests;
                var legacyApply = ApplyForTests;
                if (legacyScanner != null || legacyApply != null)
                {
                    var observation = legacyScanner != null ? legacyScanner(gun) : AmmoObservationEngine.Observe(gun);
                    var result = AmmoReserveProjection.Project(observation);
                    if (legacyApply != null) legacyApply(gun, result);
                    else AmmoReserveHudSurface.Apply(gun, result);
                    return;
                }

                var scanner = TotalScannerForTests;
                var observationV7 = scanner != null ? scanner(gun) : AmmoObservationEngine.ObserveTotal(gun);
                var fingerprint = AmmoTotalProjection.BuildFingerprint(observationV7);
                if (ReferenceEquals(lastGun, gun) && string.Equals(lastFingerprint, fingerprint, StringComparison.Ordinal)) return;
                lastGun = gun;
                lastFingerprint = fingerprint;
                var resultV7 = AmmoTotalProjection.Project(observationV7);
                var apply = TotalApplyForTests;
                if (apply != null) apply(gun, resultV7);
                else AmmoReserveHudSurface.ApplyTotal(gun, resultV7);
            }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[AmmoHud] 投影链异常（已隔离）: " + error.Message);
            }
        }

        internal static void RefreshOnHostTick()
        {
            var module = InPlaceReloadModule.ActiveModule;
            if (module == null || !module.Started || module.ShuttingDown) return;
            try
            {
                var gun = AmmoObservationEngine.ResolveLocalGun();
                if (gun == null) return;
                OnGunInfoUpdated(gun);
            }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[AmmoHud] 帧刷新异常（已隔离）: " + error.Message);
            }
        }

        internal static void RevokeAll()
        {
            lastGun = null;
            lastFingerprint = null;
            try
            {
                var hideAll = HideAllForTests;
                if (hideAll != null) hideAll();
                else AmmoReserveHudSurface.HideAll();
            }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[AmmoHud] HideAll 异常（登记已归零，呈现吞掉）: " + error.Message);
            }
        }
    }
}
