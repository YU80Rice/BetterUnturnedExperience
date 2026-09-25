using System;
using System.Runtime.CompilerServices;

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
        internal static Action RefreshOnHostTickForTests;
        internal static bool BypassLifecycleForTests;

        private static bool InvokeApplyAndSucceed(Action<object, AmmoTotalResult> apply, object gun, AmmoTotalResult result)
        {
            apply(gun, result);
            return true;
        }

        private static object lastGun;
        private static string lastFingerprint;
        private static bool inventoryDirty;
        private static object lastDiagnosticGun;
        private static string lastDiagnosticFingerprint;
        private static bool lastDiagnosticApplied;
        private static bool diagnosticStateInitialized;
        private static bool lastRevoked;

        internal static Action<string> DiagnosticForTests;

        private static void EmitDiagnostic(string message)
        {
            var sink = DiagnosticForTests;
            if (sink != null) sink(message);
            else LirRuntime.LogDiagnostic(message);
        }

        private static void EmitDiagnosticAnchor(string message)
        {
            var sink = DiagnosticForTests;
            if (sink != null) sink(message);
            else LirRuntime.LogDiagnosticAnchor(message);
        }

        private static void LogDiagnosticState(object gun, string fingerprint, AmmoTotalResult result, bool applied, string source)
        {
            if (diagnosticStateInitialized
                && ReferenceEquals(lastDiagnosticGun, gun)
                && string.Equals(lastDiagnosticFingerprint, fingerprint, StringComparison.Ordinal)
                && lastDiagnosticApplied == applied)
                return;
            diagnosticStateInitialized = true;
            lastDiagnosticGun = gun;
            lastDiagnosticFingerprint = fingerprint;
            lastDiagnosticApplied = applied;
            EmitDiagnosticAnchor("[BUE-V7-02] source=" + source
                + " gun=" + (gun == null ? "none" : RuntimeHelpers.GetHashCode(gun).ToString())
                + " fingerprint=" + (fingerprint ?? "none")
                + " total=" + result.TotalAmmo
                + " label=\"" + result.LabelText + "\""
                + " apply=" + applied);
        }

        internal static void MarkInventoryDirty()
        {
            inventoryDirty = true;
        }

        internal static void OnGunInfoUpdated(object gun, string source = "updateInfo")
        {
            var module = InPlaceReloadModule.ActiveModule;
            if (!BypassLifecycleForTests && (module == null || !module.Started || module.ShuttingDown)) return;
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
                lastRevoked = false;
                var observationV7 = scanner != null ? scanner(gun) : AmmoObservationEngine.ObserveTotal(gun);
                var fingerprint = AmmoTotalProjection.BuildFingerprint(observationV7);
                if (ReferenceEquals(lastGun, gun) && string.Equals(lastFingerprint, fingerprint, StringComparison.Ordinal)) return;
                lastGun = gun;
                lastFingerprint = fingerprint;
                var resultV7 = AmmoTotalProjection.Project(observationV7);
                var apply = TotalApplyForTests;
                var applied = apply != null ? InvokeApplyAndSucceed(apply, gun, resultV7) : AmmoReserveHudSurface.ApplyTotal(gun, resultV7);
                LogDiagnosticState(gun, fingerprint, resultV7, applied, source);
                if (!applied)
                {
                    lastGun = null;
                    lastFingerprint = null;
                    return;
                }
                lastGun = gun;
                lastFingerprint = fingerprint;
            }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[AmmoHud] 投影链异常（已隔离）: " + error.Message);
            }
        }

        internal static void RefreshOnHostTick()
        {
            var testRefresh = RefreshOnHostTickForTests;
            if (testRefresh != null)
            {
                testRefresh();
                return;
            }
            var module = InPlaceReloadModule.ActiveModule;
            if (!BypassLifecycleForTests && (module == null || !module.Started || module.ShuttingDown)) return;
            try
            {
                var gun = AmmoObservationEngine.ResolveLocalGun();
                var hadInventoryDirty = inventoryDirty;
                inventoryDirty = false;
                if (hadInventoryDirty)
                {
                    lastFingerprint = null;
                    EmitDiagnosticAnchor("[BUE-V7-02] source=HostTick+inventory-dirty gun="
                        + (gun == null ? "none" : RuntimeHelpers.GetHashCode(gun).ToString())
                        + " consumed=true");
                }
                if (gun == null)
                {
                    RevokeAll();
                    return;
                }
                OnGunInfoUpdated(gun, hadInventoryDirty ? "HostTick+inventory-dirty" : "HostTick");
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
            inventoryDirty = false;
            lastDiagnosticGun = null;
            lastDiagnosticFingerprint = null;
            lastDiagnosticApplied = false;
            diagnosticStateInitialized = false;
            if (!lastRevoked)
            {
                lastRevoked = true;
                EmitDiagnosticAnchor("[BUE-V7-02] source=RevokeAll gun=none fingerprint=none total=0 label=\"hidden\" apply=true");
            }
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
