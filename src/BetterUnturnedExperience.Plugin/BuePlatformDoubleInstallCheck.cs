using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-23: one loaded-assembly view seen by the double-install self
    /// check. The decision core consumes these records instead of
    /// <see cref="System.Reflection.Assembly"/> objects so host tests can
    /// inject the assembly list without touching the file system (spec seam
    /// 7: 程序集列表注入，不触文件系统).
    /// </summary>
    internal sealed class BueLoadedAssemblyView
    {
        internal BueLoadedAssemblyView(string simpleName, string location, bool isSelf)
        {
            SimpleName = simpleName ?? string.Empty;
            Location = location ?? string.Empty;
            IsSelf = isSelf;
        }

        /// <summary>Simple assembly name, empty when unavailable (dynamic or hostile assembly).</summary>
        internal string SimpleName { get; }

        /// <summary>Assembly location, empty when the runtime cannot provide one.</summary>
        internal string Location { get; }

        /// <summary>True for the running BUE assembly itself — never a conflict.</summary>
        internal bool IsSelf { get; }
    }

    /// <summary>
    /// DEV-V2-23: the BUE-PLATFORM-001 report. Explicit result (never null);
    /// diagnostic-only — it names the conflicting copies and suggests removal,
    /// it never deletes or moves user files (处置留给用户).
    /// </summary>
    internal sealed class BueDoubleInstallReport
    {
        internal BueDoubleInstallReport(bool hasConflict, string assemblyName, IReadOnlyList<string> conflictLocations,
            string selfPath, string suggestion, string noticeLine)
        {
            HasConflict = hasConflict;
            AssemblyName = assemblyName ?? string.Empty;
            ConflictLocations = conflictLocations ?? new string[0];
            SelfPath = selfPath ?? string.Empty;
            Suggestion = suggestion ?? string.Empty;
            NoticeLine = noticeLine ?? string.Empty;
        }

        internal bool HasConflict { get; }
        internal string DiagnosticId { get { return BuePlatformDoubleInstallCheck.DiagnosticId; } }
        internal string AssemblyName { get; }
        internal IReadOnlyList<string> ConflictLocations { get; }
        internal string SelfPath { get; }
        internal string Suggestion { get; }
        internal string NoticeLine { get; }

        /// <summary>One structured log line per conflicting copy — self-contained
        /// (each line carries the id, both paths and the suggestion) so no
        /// separator ambiguity can split a Windows path.</summary>
        internal string BuildConflictLogLine(string conflictLocation)
        {
            return "BUE double-install detected diagnosticId=" + DiagnosticId
                + " assembly=" + AssemblyName
                + " conflictLocation=" + (conflictLocation ?? string.Empty)
                + " selfPath=" + SelfPath
                + " suggestion=" + Suggestion;
        }
    }

    /// <summary>
    /// DEV-V2-23: the BUE Awake injection self-check (BUE-PLATFORM-001).
    /// Boundary (措辞冻结): only assemblies already inside the AppDomain; not a
    /// replacement for BepInEx GUID dedup (同 GUID 双装 = BepInEx 原生留一跳一 +
    /// 文档 FAQ); never claims to catch copies that never loaded, failed to
    /// load, or live in isolate contexts; NOT a complete anti-double-load
    /// system; never deletes user files.
    /// </summary>
    internal static class BuePlatformDoubleInstallCheck
    {
        internal const string DiagnosticId = "BUE-PLATFORM-001";
        internal const string BueAssemblySimpleName = "BetterUnturnedExperience";
        internal const string UnknownLocationToken = "(路径不可用)";
        private const string SuggestionText = "移除非官方副本";

        // The self-check's own isolation id (scan-side failures: unreadable
        // assembly metadata, source wiring faults) — BUE-PLATFORM-001 stays
        // exclusively the double-install conflict diagnosis.
        internal const string PlatformIsolationDiagnosticId = "BUE-PLATFORM-002";

        /// <summary>Test seam: when set, replaces the AppDomain assembly scan.
        /// Null in production; the default source never returns null.</summary>
        internal static Func<IReadOnlyList<BueLoadedAssemblyView>> LoadedAssembliesSource = null;

        /// <summary>Production entry: scan (or injected source), decide, emit one
        /// Warning line per conflicting copy, and return the explicit report for
        /// the caller (the plugin wires the notice into the management panel).</summary>
        internal static BueDoubleInstallReport Run()
        {
            var source = LoadedAssembliesSource ?? DefaultAppDomainSource;
            var loaded = source();
            if (loaded == null) throw new InvalidOperationException("double-install assembly source returned null");
            var report = Check(loaded, BueAssemblySimpleName, ReadSelfPath());
            if (report.HasConflict) Emit(report);
            return report;
        }

        /// <summary>Emit one Warning-level line per conflicting copy through the
        /// runtime log so the diagnosis survives the runtime-verbosity gate.</summary>
        internal static void Emit(BueDoubleInstallReport report)
        {
            if (report == null || !report.HasConflict) return;
            for (var index = 0; index < report.ConflictLocations.Count; index++)
            {
                BueRuntimeLog.Warn(report.BuildConflictLogLine(report.ConflictLocations[index]));
            }
        }

        /// <summary>The decision core: same simple name as BUE, not the running
        /// BUE assembly, and not the same path as BUE itself = a conflicting
        /// copy. Name match is OrdinalIgnoreCase — fusion binds names
        /// case-insensitively, so a diagnostic must over-report, never hide.
        /// Listings are deduped case-insensitively per path (one file per path),
        /// first observation wins; missing locations collapse into the
        /// unknown-location bucket. Fail-fast on a null list/name: a developer
        /// error, not a silently clean scan.</summary>
        internal static BueDoubleInstallReport Check(IReadOnlyList<BueLoadedAssemblyView> loadedAssemblies,
            string selfAssemblyName, string selfPath)
        {
            if (loadedAssemblies == null) throw new ArgumentNullException(nameof(loadedAssemblies));
            if (selfAssemblyName == null) throw new ArgumentNullException(nameof(selfAssemblyName));
            var safeSelfPath = selfPath ?? string.Empty;
            var conflicts = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < loadedAssemblies.Count; index++)
            {
                var view = loadedAssemblies[index];
                if (view == null) continue;
                if (string.IsNullOrEmpty(view.SimpleName) || view.IsSelf) continue;
                if (!string.Equals(view.SimpleName, selfAssemblyName, StringComparison.OrdinalIgnoreCase)) continue;
                if (safeSelfPath.Length > 0 && string.Equals(view.Location, safeSelfPath, StringComparison.OrdinalIgnoreCase)) continue;
                var location = string.IsNullOrEmpty(view.Location) ? UnknownLocationToken : view.Location;
                if (seen.Add(location)) conflicts.Add(location);
            }
            var notice = conflicts.Count > 0
                ? "检测到 " + selfAssemblyName + " 冲突副本（" + DiagnosticId + "）：请" + SuggestionText + "后重启游戏，详见 BUE 日志。"
                : string.Empty;
            return new BueDoubleInstallReport(conflicts.Count > 0, selfAssemblyName, conflicts, safeSelfPath, SuggestionText, notice);
        }

        /// <summary>Production source: every assembly already inside the AppDomain,
        /// mapped defensively — one hostile/dynamic assembly must not break the
        /// scan of the rest (per-assembly try/catch, empty fields on failure).</summary>
        internal static IReadOnlyList<BueLoadedAssemblyView> DefaultAppDomainSource()
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies();
            var self = SelfAssembly;
            var views = new List<BueLoadedAssemblyView>(loaded.Length);
            for (var index = 0; index < loaded.Length; index++)
            {
                var assembly = loaded[index];
                if (assembly == null) continue;
                views.Add(new BueLoadedAssemblyView(TrySimpleName(assembly), TryLocation(assembly),
                    self != null && ReferenceEquals(assembly, self)));
            }
            return views;
        }

        private static System.Reflection.Assembly SelfAssembly
        {
            get { return typeof(BuePlatformDoubleInstallCheck).Assembly; }
        }

        internal static string ReadSelfPath()
        {
            return TryLocation(SelfAssembly);
        }

        private static string TrySimpleName(System.Reflection.Assembly assembly)
        {
            try { return assembly.GetName().Name ?? string.Empty; }
            catch (Exception error)
            {
                // An assembly that loaded but cannot report its name is anomalous
                // enough to surface; the scan degrades (empty name = never a
                // match candidate) instead of failing the whole check.
                BueRuntimeLog.Warn("BUE double-install self-check skipped an unreadable assembly name diagnosticId=" + PlatformIsolationDiagnosticId + " errorType=" + error.GetType().Name);
                return string.Empty;
            }
        }

        private static string TryLocation(System.Reflection.Assembly assembly)
        {
            try { return assembly.Location ?? string.Empty; }
            catch (NotSupportedException)
            {
                // Dynamic assemblies have no location — a normal runtime state,
                // not a fault; the view just carries an empty path.
                return string.Empty;
            }
            catch (Exception error)
            {
                BueRuntimeLog.Warn("BUE double-install self-check skipped an unreadable assembly location diagnosticId=" + PlatformIsolationDiagnosticId + " errorType=" + error.GetType().Name);
                return string.Empty;
            }
        }
    }
}
