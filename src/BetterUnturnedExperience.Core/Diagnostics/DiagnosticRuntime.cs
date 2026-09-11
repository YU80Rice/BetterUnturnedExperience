using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;

namespace BetterUnturnedExperience.Core.Diagnostics
{
    /// <summary>
    /// DEV-V3-07: the level of a diagnostic write — the three-method narrow
    /// surface of <see cref="IFeatureLogger"/> mapped one-to-one (Info /
    /// Warning / Error). Not a contract type: the contract freezes the METHODS
    /// (frozen IFeatureLogger shape), this enum is the host-side carrier the
    /// composition root routes visibility by.
    /// </summary>
    public enum DiagnosticLevel : byte { Info = 0, Warning = 1, Error = 2 }

    /// <summary>
    /// DEV-V3-07: one bounded-summary row — per (FeatureId, DiagnosticId) the
    /// highest level seen, the write count and the first/last sighting (UTC
    /// epoch ms). An immutable value projection (the FeatureStatusView
    /// discipline): callers observe, never mutate. The summary is runtime
    /// diagnostic VISIBILITY, never acceptance authority (CaseId/RELEASES stay
    /// human — frozen by V3-T8).
    /// </summary>
    public readonly struct DiagnosticSummaryEntry
    {
        public string FeatureId { get; }
        public string DiagnosticId { get; }
        public DiagnosticLevel Level { get; }
        public long Count { get; }
        public long FirstSeenUtcMs { get; }
        public long LastSeenUtcMs { get; }
        internal DiagnosticSummaryEntry(string featureId, string diagnosticId, DiagnosticLevel level, long count, long firstSeenUtcMs, long lastSeenUtcMs)
        {
            FeatureId = featureId;
            DiagnosticId = diagnosticId;
            Level = level;
            Count = count;
            FirstSeenUtcMs = firstSeenUtcMs;
            LastSeenUtcMs = lastSeenUtcMs;
        }
    }

    /// <summary>
    /// DEV-V3-07: the feature-bound <see cref="IFeatureLogger"/> view — the
    /// module's ONLY diagnostic route (availability-matrix row: null before
    /// this ticket, composed non-null for every started feature from here on).
    /// It stamps its OWN FeatureId and LifecycleGeneration into every write
    /// (the module cannot attribute a line to another feature), and it never
    /// throws: the narrow surface returns void, so ALL failure isolation —
    /// boundary refusals, prefix refusals, sink faults — lives on this side of
    /// the seam (the ticket red line「Logger 异常不得反向破坏模块」). The
    /// generation/withdraw accounting mirrors the DEV-V3-04 dispatcher and the
    /// DEV-V3-06 settings view: writes after stop/isolation/host-shutdown are
    /// refused with ONE latched structured line, never silently.
    /// </summary>
    public sealed class FeatureLoggerView : IFeatureLogger
    {
        private readonly DiagnosticRuntime owner;
        private readonly string featureId;
        private readonly ulong generation;
        private readonly bool isPlatformFeature;
        // Rejection-line latches (view-lifetime): the FIRST refused identifier
        // of each kind surfaces one structured refusal line; further refusals
        // are counted in the summary without repeating the line (the电平式
        // no-flood discipline of the T5 link health). They are ONLY touched
        // under the owner's sync (the runtime's single lock — a module may
        // call the view from any thread; the latch state is per view, so no
        // per-feature cleanup and no unbounded dictionary is needed).
        private readonly HashSet<string> latchedRejections = new HashSet<string>(StringComparer.Ordinal);
        private bool boundaryLatched;

        internal FeatureLoggerView(DiagnosticRuntime owner, string featureId, ulong generation, bool isPlatformFeature)
        {
            this.owner = owner;
            this.featureId = featureId;
            this.generation = generation;
            this.isPlatformFeature = isPlatformFeature;
        }

        public void Info(string eventName, string diagnosticId)
        {
            owner.RecordModuleWrite(this, DiagnosticLevel.Info, eventName, FrameworkErrorCode.None, diagnosticId, null);
        }

        public void Warning(string eventName, FrameworkErrorCode error, string diagnosticId)
        {
            owner.RecordModuleWrite(this, DiagnosticLevel.Warning, eventName, error, diagnosticId, null);
        }

        public void Error(string eventName, FrameworkErrorCode error, string diagnosticId, Exception exception)
        {
            owner.RecordModuleWrite(this, DiagnosticLevel.Error, eventName, error, diagnosticId, exception);
        }

        internal string FeatureId { get { return featureId; } }
        internal ulong Generation { get { return generation; } }
        internal bool IsPlatformFeature { get { return isPlatformFeature; } }

        // Caller MUST hold the owner's sync (the runtime's single decision
        // lock — the emission-half discipline holds: decisions under the lock,
        // writes to the writer outside it).
        internal bool TryLatchRejectionLocked(string key)
        {
            return latchedRejections.Add(key);
        }

        internal bool TryLatchBoundaryLocked()
        {
            if (boundaryLatched) return false;
            boundaryLatched = true;
            return true;
        }
    }

    /// <summary>
    /// DEV-V3-07: the platform diagnostic aggregator — the ONE unified sink
    /// the whole runtime writes through (the FeatureSettingsRegistry /
    /// MainThreadDispatcherRuntime precedent: public host composition, not SDK
    /// contract; external features see only IFeatureLogger). It owns two
    /// surfaces:
    ///   - structured LOG LINES (via the injected lineWriter at the write's
    ///     level — Info/Warning/Error all stay visible in a normal UMM log
    ///     export; the player sees eco and official diagnostics alike, never
    ///     filtered by origin);
    ///   - a BOUNDED SUMMARY aggregated per (FeatureId, DiagnosticId): max
    ///     <see cref="MaxSummaryEntries"/> entries (this ticket's fixed number,
    ///     the observable-capacity precedent), one summary line per entry at
    ///     first sighting and then at most once per
    ///     <see cref="SummaryMinIntervalMs"/> (output rate limit — counting is
    ///     unaffected), identifiers sanitized and truncated
    ///     (<see cref="MaxEventNameLength"/>/<see cref="MaxDiagnosticIdLength"/>
    ///     /<see cref="MaxFaultLength"/>) so a hostile write cannot forge a
    ///     second line with an embedded newline, and NOTHING persists across a
    ///     restart (in-memory only).
    /// The BUE-* diagnostic prefix stays platform-reserved: a feature OUTSIDE
    /// the official reserved segment writing a BUE-* code is refused with a
    /// structured diagnostic (same governance philosophy as the DEV-V3-01
    /// FeatureId reserved segment — impersonation, not observation, is what
    /// gets rejected). Host seams (the DEV-V3-03 lifecycle machine, the T5
    /// link health, the state projection) are absorbed through
    /// <see cref="AggregateHostLine"/> — their raw line paths stay byte-for-
    /// byte the producers' own (the double-binding is an ADDITIVE absorption,
    /// so the 03/04 anchors never move), while the summary sees one unified
    /// evidence chain where each seam stays identifiable by its own code.
    /// </summary>
    public sealed class DiagnosticRuntime
    {
        /// <summary>Summary capacity — the bound this ticket fixes (observable
        /// and testable, the DEV-V3-03 capacity-64 precedent).</summary>
        public const int MaxSummaryEntries = 128;
        /// <summary>Per-entry summary-line rate limit in monotonic ms.</summary>
        public const long SummaryMinIntervalMs = 30000L;
        /// <summary>Token caps: a truncated token is the aggregation key.</summary>
        public const int MaxEventNameLength = 64;
        public const int MaxDiagnosticIdLength = 128;
        public const int MaxFaultLength = 256;

        // The refusal codes this ticket registers (Appendix B batch → DEV-V3-08):
        // BUE-LOG-001 reserved-prefix impersonation / BUE-LOG-002 invalid
        // identifier / BUE-LOG-004 refused at the stop|isolation|host-shutdown
        // boundary. Observations: BUE-LOG-003 summary capacity overflow,
        // BUE-LOG-005 logger-internal fault isolated, BUE-LOG-CREATED /
        // BUE-LOG-GEN (the BUE-MT-GEN/BUE-SET-GEN observation-code precedent).
        internal const string CodePrefixRefused = "BUE-LOG-001";
        internal const string CodeIdentifierRefused = "BUE-LOG-002";
        internal const string CodeCapacityOverflow = "BUE-LOG-003";
        internal const string CodeBoundaryRefused = "BUE-LOG-004";
        internal const string CodeFaultIsolated = "BUE-LOG-005";

        private readonly object sync = new object();
        private readonly Dictionary<string, DiagnosticSummaryEntry> entries =
            new Dictionary<string, DiagnosticSummaryEntry>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> lastSummaryEmissionMs =
            new Dictionary<string, long>(StringComparer.Ordinal);
        // The generation ledger (the dispatcher InvalidateOwner precedent):
        // a feature may write while its CURRENT generation is opened; a
        // withdrawal removes the entry — every older captured view then lands
        // on the boundary refusal, and a re-opened generation writes again.
        private readonly Dictionary<string, ulong> currentGeneration =
            new Dictionary<string, ulong>(StringComparer.Ordinal);
        private bool hostStopped;
        private bool overflowLatched;
        private bool faultLatched;

        private readonly Action<string, DiagnosticLevel> lineWriter;
        private readonly Func<long> utcNowMs;
        private readonly Func<long> monotonicMs;
        private readonly Action<string> faultFallbackWriter;

        public DiagnosticRuntime(Action<string, DiagnosticLevel> lineWriter, Func<long> utcNowMs = null,
            Func<long> monotonicMs = null, Action<string> faultFallbackWriter = null)
        {
            this.lineWriter = lineWriter ?? throw new ArgumentNullException(nameof(lineWriter));
            this.utcNowMs = utcNowMs ?? DefaultUtcNowMs;
            this.monotonicMs = monotonicMs ?? DefaultMonotonicMs;
            this.faultFallbackWriter = faultFallbackWriter;
        }

        /// <summary>Current aggregated-entry count (capacity is observable).</summary>
        public int SummaryEntryCount
        {
            get { lock (sync) return entries.Count; }
        }

        /// <summary>Read one summary row (the host-test/panel seam; no such
        /// query reaches modules — the module surface stays the void methods).</summary>
        public bool TryGetSummaryEntry(string featureId, string diagnosticId, out DiagnosticSummaryEntry entry)
        {
            lock (sync)
            {
                if (featureId != null && diagnosticId != null
                        && entries.TryGetValue(SummaryKey(featureId, diagnosticId), out entry))
                    return true;
            }
            entry = default(DiagnosticSummaryEntry);
            return false;
        }

        /// <summary>The Logger matrix row for one (feature, generation): the
        /// generation ledger opens first (superseding any older one), then the
        /// feature-bound view is composed. The platform side of the prefix gate
        /// is decided by the identity alone — reserved-segment ids exist only
        /// after the DEV-V3-01 whitelist admission (a rogue segment id can
        /// never be registered, BUE-REG-010), so the segment test IS the
        /// official/ecosystem split without a second gate.</summary>
        public FeatureLoggerView CreateLoggerView(string featureId, ulong generation)
        {
            return new FeatureLoggerView(this, featureId, generation,
                featureId != null && OfficialFeatureIdentity.IsReservedSegment(featureId));
        }

        public void OpenGeneration(string featureId, ulong generation)
        {
            if (featureId == null) return;
            // The dispatcher OpenGeneration precedent (DEV-V3-04): opening a
            // generation is a HOST-START act — a same-process reload after a
            // PluginStopping teardown re-arms the ledger (宿主停止=边界非死刑;
            // only the CURRENT generation's views write, every older captured
            // view stays refused).
            lock (sync)
            {
                hostStopped = false;
                currentGeneration[featureId] = generation;
            }
        }

        /// <summary>The stop/isolation boundary for one feature's writes (the
        /// dispatcher/settings InvalidateOwner precedent, same call sites).</summary>
        public void InvalidateOwner(string featureId, string reason)
        {
            if (featureId == null) return;
            lock (sync) currentGeneration.Remove(featureId);
        }

        /// <summary>The host stop boundary: every captured view refuses after
        /// this (PluginStopping teardown; the instance survives for late
        /// host-side absorption during teardown).</summary>
        public void ShutdownHost(string reason)
        {
            lock (sync) hostStopped = true;
        }

        /// <summary>
        /// The unified record pipeline for one module write: boundary gate →
        /// identifier validity → reserved-prefix gate → accepted (line +
        /// aggregation). NOTHING here may throw across the seam — the whole
        /// body is isolated and a fault lands on BUE-LOG-005 (fallback
        /// channel), never on the calling module. Emissions are collected
        /// under the lock and written OUTSIDE it (the 02/04/06 emit-outside-
        /// the-lock discipline): a sink fault can never deadlock the ledger.
        /// </summary>
        internal void RecordModuleWrite(FeatureLoggerView view, DiagnosticLevel level, string eventName,
            FrameworkErrorCode error, string diagnosticId, Exception exception)
        {
            List<PendingLine> pending = null;
            try
            {
                pending = CollectModuleWrite(view, level, eventName, error, diagnosticId, exception);
            }
            catch (Exception fault)
            {
                pending = null;
                FaultIsolated(fault);
            }
            if (pending != null) EmitAll(pending);
        }

        // The decision half of a module write: gates, aggregation and line
        // building under NO emission (the emit-outside-the-lock discipline —
        // every path RETURNS its pending set, none escapes early, so an
        // accepted line, a rejection line and a summary line all reach the
        // writer exactly when their branch decided them).
        private List<PendingLine> CollectModuleWrite(FeatureLoggerView view, DiagnosticLevel level, string eventName,
            FrameworkErrorCode error, string diagnosticId, Exception exception)
        {
            // ONE decision lock for gate + validity + prefix + aggregation +
            // latches: a stop landing between two half-decisions could let an
            // old-generation line slip through the split (R1 finding), and the
            // view latches are touched here so they inherit this lock (the
            // view may be called from any thread). The clock is read OUTSIDE
            // the lock; only the writer call stays outside.
            var pending = new List<PendingLine>();
            var featureId = view.FeatureId;
            var utc = utcNowMs();
            var mono = monotonicMs();
            var safeEvent = SanitizeToken(eventName, MaxEventNameLength);
            var safeId = SanitizeToken(diagnosticId, MaxDiagnosticIdLength);
            lock (sync)
            {
                string boundaryReason = null;
                ulong current;
                if (hostStopped) boundaryReason = "host-stopped";
                else if (!currentGeneration.TryGetValue(featureId ?? string.Empty, out current) || current != view.Generation)
                    boundaryReason = "write-boundary";
                if (boundaryReason != null)
                {
                    // 停止/隔离/宿主停止边界：矩阵行为面=不再产生模块行；显式
                    // 留痕一条（latch），后续拒写静默计数（不炸帧）。
                    ApplyEntryLocked(featureId, CodeBoundaryRefused, DiagnosticLevel.Warning, pending, utc, mono);
                    if (view.TryLatchBoundaryLocked())
                        pending.Add(Rejection("reason=" + boundaryReason, featureId, safeId, CodeBoundaryRefused));
                    return pending;
                }
                if (string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(diagnosticId))
                {
                    ApplyEntryLocked(featureId, CodeIdentifierRefused, DiagnosticLevel.Warning, pending, utc, mono);
                    if (view.TryLatchRejectionLocked("invalid:" + (string.IsNullOrWhiteSpace(eventName) ? safeEvent : safeId)))
                        pending.Add(Rejection("reason=invalid-identifier", featureId, safeId, CodeIdentifierRefused));
                    return pending;
                }
                if (!view.IsPlatformFeature && diagnosticId.StartsWith("BUE-", StringComparison.Ordinal))
                {
                    // The reserved-prefix gate runs on the RAW id (a leading
                    // "BUE-" is the claim itself, sanitizing first would let
                    // casing games slip the gate).
                    ApplyEntryLocked(featureId, CodePrefixRefused, DiagnosticLevel.Warning, pending, utc, mono);
                    if (view.TryLatchRejectionLocked("prefix:" + safeId))
                        pending.Add(Rejection("reason=reserved-prefix", featureId, safeId, CodePrefixRefused));
                    return pending;
                }
                var moduleLine = BuildModuleLine(featureId, safeEvent, level, error, safeId, exception);
                pending.Add(new PendingLine(moduleLine, level));
                // Aggregation happens BEFORE the line is emitted: a sink fault
                // still counts the write (the evidence chain keeps the truth
                // of what was attempted even when visibility fails).
                ApplyEntryLocked(featureId, safeId, level, pending, utc, mono);
                return pending;
            }
        }

        /// <summary>
        /// The absorption entry for host seam lines (T4 isolation / state
        /// projection / T5 link health): the line itself was ALREADY written
        /// by its producing seam (byte-for-byte unchanged — the absorption is
        /// additive, the 03/04 anchors never move); here only its
        /// <c>diagnosticId=</c>/<c>feature=</c> tokens feed the SAME summary
        /// store, so every seam lands in one unified evidence chain while
        /// keeping its own code identity (分 seam 判据可定位，互不遮蔽). A line
        /// without a diagnosticId is not aggregated (no silent entries).
        /// </summary>
        public void AggregateHostLine(string fallbackFeatureId, DiagnosticLevel level, string line)
        {
            if (line == null) return;
            List<PendingLine> pending = null;
            try
            {
                var id = ExtractToken(line, "diagnosticId=");
                if (id == null) return;
                var feature = ExtractToken(line, "feature=") ?? fallbackFeatureId ?? "bue.host";
                var utc = utcNowMs();
                var mono = monotonicMs();
                pending = new List<PendingLine>();
                lock (sync) ApplyEntryLocked(feature, id, level, pending, utc, mono);
            }
            catch (Exception)
            {
                // absorption must never fault the producing seam — and there
                // is no fallback left to complain to (the caller's own sink
                // isolation is the outer belt).
                return;
            }
            if (pending != null) EmitAll(pending);
        }

        // Caller MUST hold the sync lock (the whole decision half of a write
        // is ONE locked region — gate, ledger, aggregation and latches can
        // never observe a torn intermediate; only the line WRITING is kept
        // outside the lock via the returned pending set).
        private void ApplyEntryLocked(string featureId, string diagnosticId, DiagnosticLevel level,
            List<PendingLine> pending, long utc, long mono)
        {
            var key = SummaryKey(featureId, diagnosticId);
            bool isNew;
            string summaryLine = null;
            DiagnosticSummaryEntry existing;
            if (entries.TryGetValue(key, out existing))
            {
                isNew = false;
                var merged = (DiagnosticLevel)Math.Max((int)existing.Level, (int)level);
                entries[key] = new DiagnosticSummaryEntry(featureId, diagnosticId, merged,
                    existing.Count + 1L, existing.FirstSeenUtcMs, utc);
            }
            else
            {
                if (entries.Count >= MaxSummaryEntries)
                {
                    // Capacity degradation: raw lines keep flowing (the
                    // evidence chain is never cut by the bound); ONE overflow
                    // observation line per runtime lifetime.
                    if (!overflowLatched)
                    {
                        overflowLatched = true;
                        pending.Add(new PendingLine("[BUE-DIAG] event=diagnostic-summary result=capacity-overflow entryCap="
                            + MaxSummaryEntries + " attemptedFeature=" + featureId
                            + " attemptedDiagnosticId=" + diagnosticId + " diagnosticId=" + CodeCapacityOverflow,
                            DiagnosticLevel.Warning));
                    }
                    return;
                }
                isNew = true;
                entries[key] = new DiagnosticSummaryEntry(featureId, diagnosticId, level, 1L, utc, utc);
            }
            var row = entries[key];
            var lastEmission = isNew ? 0L : LastEmissionLocked(key);
            if (isNew || mono - lastEmission >= SummaryMinIntervalMs)
            {
                lastSummaryEmissionMs[key] = mono;
                summaryLine = "BUE diagnostic-summary featureId=" + featureId + " diagnosticId=" + diagnosticId
                    + " level=" + LevelToken(row.Level) + " count=" + row.Count
                    + " firstSeen=" + FormatUtc(row.FirstSeenUtcMs) + " lastSeen=" + FormatUtc(row.LastSeenUtcMs);
            }
            if (summaryLine != null) pending.Add(new PendingLine(summaryLine, DiagnosticLevel.Info));
        }

        private long LastEmissionLocked(string key)
        {
            long last;
            return lastSummaryEmissionMs.TryGetValue(key, out last) ? last : 0L;
        }

        private static PendingLine Rejection(string reason, string featureId, string attemptedId, string code)
        {
            return new PendingLine("[BUE-DIAG] event=diagnostic-write " + reason + " result=rejected feature="
                + (featureId ?? string.Empty) + " attemptedDiagnosticId=" + attemptedId + " diagnosticId=" + code,
                DiagnosticLevel.Warning);
        }

        private static string BuildModuleLine(string featureId, string safeEvent, DiagnosticLevel level,
            FrameworkErrorCode error, string safeId, Exception exception)
        {
            var line = "[BUE-DIAG] feature=" + featureId + " event=" + safeEvent + " level=" + LevelToken(level);
            // The error parameter is part of the frozen Warning/Error methods;
            // Info carries none. The exception text rides LAST as a free-text
            // fault (type + message only — no stack, no payload), whitespace
            // flattened so one write stays one line.
            if (level != DiagnosticLevel.Info) line += " error=" + error;
            line += " diagnosticId=" + safeId;
            if (exception != null) line += " fault=" + SanitizeFault(exception);
            return line;
        }

        private void EmitAll(List<PendingLine> pending)
        {
            for (var i = 0; i < pending.Count; i++)
            {
                try { lineWriter(pending[i].Line, pending[i].Level); }
                catch (Exception fault) { FaultIsolated(fault); }
            }
        }

        private void FaultIsolated(Exception fault)
        {
            bool first;
            lock (sync)
            {
                first = !faultLatched;
                if (first) faultLatched = true;
            }
            if (!first) return;
            var fallback = faultFallbackWriter;
            if (fallback == null) return;
            try
            {
                fallback("[BUE-DIAG] event=diagnostic-write result=fault-isolated errorType="
                    + (fault != null ? fault.GetType().Name : "unknown") + " diagnosticId=" + CodeFaultIsolated);
            }
            catch (Exception)
            {
                // primary AND fallback both faulty: still no throw across the
                // seam (the full-isolation red line).
            }
        }

        private struct PendingLine
        {
            internal readonly string Line;
            internal readonly DiagnosticLevel Level;
            internal PendingLine(string line, DiagnosticLevel level) { Line = line; Level = level; }
        }

        private static string SummaryKey(string featureId, string diagnosticId)
        {
            return (featureId ?? string.Empty) + "|" + (diagnosticId ?? string.Empty);
        }

        internal static string SanitizeToken(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var builder = new System.Text.StringBuilder(value.Length < maxLength ? value.Length : maxLength);
            for (var i = 0; i < value.Length && builder.Length < maxLength; i++)
            {
                var c = value[i];
                builder.Append(char.IsWhiteSpace(c) ? '_' : c);
            }
            return builder.ToString();
        }

        private static string SanitizeFault(Exception exception)
        {
            // 冻结格式 Type:Message（冒号后无空格——空格会断开 token 语义，
            // 诊断行解析器按空白切 token）。
            var text = exception.GetType().Name + ":" + exception.Message;
            if (text.Length > MaxFaultLength) text = text.Substring(0, MaxFaultLength);
            return text.Replace('\r', ' ').Replace('\n', ' ');
        }

        // Token extraction with a FIELD-BOUNDARY match (line start or right
        // after whitespace): a plain substring search would let an
        // "attemptedDiagnosticId=" field masquerade as the real
        // "diagnosticId=" one — the absorption contract must not depend on a
        // spelling coincidence to stay correct.
        private static string ExtractToken(string line, string token)
        {
            var search = 0;
            while (search <= line.Length - token.Length)
            {
                var start = line.IndexOf(token, search, StringComparison.Ordinal);
                if (start < 0) return null;
                if (start == 0 || char.IsWhiteSpace(line[start - 1]))
                {
                    var valueStart = start + token.Length;
                    var end = valueStart;
                    while (end < line.Length && !char.IsWhiteSpace(line[end])) end++;
                    return end > valueStart ? line.Substring(valueStart, end - valueStart) : null;
                }
                search = start + token.Length;
            }
            return null;
        }

        private static string LevelToken(DiagnosticLevel level)
        {
            switch (level)
            {
                case DiagnosticLevel.Warning: return "warning";
                case DiagnosticLevel.Error: return "error";
                default: return "info";
            }
        }

        private static string FormatUtc(long utcMs)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(utcMs)
                .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static long DefaultUtcNowMs()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        private static long DefaultMonotonicMs()
        {
            // Frequency-normalized (the raw QPC/10^4 form of the older T5 seam
            // assumes a 10MHz counter — on this host the rate limit stays
            // bounded either way, but the math is exact here).
            return System.Diagnostics.Stopwatch.GetTimestamp() * 1000L / System.Diagnostics.Stopwatch.Frequency;
        }
    }
}
