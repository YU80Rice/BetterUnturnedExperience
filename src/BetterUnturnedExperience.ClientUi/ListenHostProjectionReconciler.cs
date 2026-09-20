using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    /// <summary>
    /// F-B1c decision core: whether a vanilla listen-host inventory UI
    /// projection exactly mirrors the authoritative inventory. Mirrors the
    /// reference-multiset semantics the ecosystem already relies on for this
    /// native defect (rendered + pending must reproduce the authoritative
    /// jar identities exactly; order is irrelevant). Pure, allocation-light,
    /// stateless, engine-free — fully testable on the host path.
    /// </summary>
    internal static class ProjectionReconcileDecision
    {
        internal static bool IsExact(IReadOnlyList<object> authoritative, IReadOnlyList<object> rendered, IReadOnlyList<object> pending)
        {
            if (authoritative == null || rendered == null || pending == null)
                return false;

            var projectedCount = rendered.Count + pending.Count;
            if (projectedCount != authoritative.Count)
                return false;

            var matched = new bool[projectedCount];
            for (var a = 0; a < authoritative.Count; a++)
            {
                var expected = authoritative[a];
                if (expected == null)
                    return false;

                var found = false;
                for (var p = 0; p < projectedCount; p++)
                {
                    if (matched[p])
                        continue;

                    var candidate = p < rendered.Count ? rendered[p] : pending[p - rendered.Count];
                    if (ReferenceEquals(expected, candidate))
                    {
                        matched[p] = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// F-B1c page seam: one dashboard page's authoritative inventory model
    /// versus its live UI projection. The engine implementation wraps the
    /// SDG types (NoInlining, never JIT'd on the host test path); tests bind
    /// fakes. RepairFromAuthoritative rebuilds the projection from the
    /// authoritative model — it never mutates inventory state.
    /// </summary>
    internal interface IInventoryProjectionPageView
    {
        byte Page { get; }
        int AuthoritativeCount { get; }
        object AuthoritativeAt(int index);
        int RenderedCount { get; }
        object RenderedJarAt(int index);
        int PendingCount { get; }
        object PendingJarAt(int index);
        void RepairFromAuthoritative();
    }

    /// <summary>
    /// F-B1c: BUE's tidy commits move items out-of-band, which leaves the
    /// vanilla listen-host dashboard projection stale (stale elements at old
    /// coordinates = the「幽灵图层/物品重叠」symptom). The feature that
    /// invalidated the projection reconciles it: after every tidy commit and
    /// on dashboard open, compare authoritative vs rendered+pending and
    /// rebuild affected pages when — and only when — the reference multisets
    /// differ. Idempotent (an exact projection is never touched), so it
    /// composes safely with the ecosystem's existing event-driven repairs.
    /// Listen-host only by gate: a dedicated server (U3DS) has no shared
    /// listen-host projection and remote clients are vanilla-replicated.
    ///
    /// Dispatchers are test-safe by construction: ReconcileHook (test
    /// observation) routes first; otherwise EngineDispatcher — bound ONLY in
    /// Plugin.Awake — routes to the NoInlining SDG path. With neither bound
    /// (host test runs) the dispatchers are silent no-ops, so no test path
    /// ever JITs an SDG-touching method.
    /// </summary>
    internal static class ListenHostProjectionReconciler
    {
        internal const string DiagnosticId = "BUE-LIT-001";
        internal const string AbortDiagnosticId = "BUE-LIT-002";

        // DEV-V6-02E (宿主日志口, V6-T2 硬项拆法): the host log mouths are
        // injected here instead of naming the host project's internal logger
        // (the pre-move source edge of the ClientUi→Plugin ring). Bound by
        // ClientUiFeatureAssembly.BindHostComposition at composition time;
        // unbound = honest swallow, the same absent-seam contract as the
        // 02B/C/D feature runtimes (未绑定即吞).
        internal static Action<string> HostLogSink = null;
        internal static Action<string> HostWarnSink = null;

        // Production-only engine path (bound in Plugin.Awake). Never set on
        // the host test path — see the class comment for the JIT rationale.
        internal static Action<byte, byte> EngineDispatcher = null;

        // Test observation seam: routes before the engine dispatcher and
        // suppresses it, so tests observe routing without touching SDG.
        internal static Action<byte, byte> ReconcileHook = null;

        internal static Action<byte, byte> BindEngine()
        {
            return ReconcileRangeEngine;
        }

        internal static void OnTidyPagesCommitted(byte firstPage, byte lastPage)
        {
            var hook = ReconcileHook;
            if (hook != null)
            {
                hook(firstPage, lastPage);
                return;
            }
            var engine = EngineDispatcher;
            if (engine != null)
                engine(firstPage, lastPage);
        }

        // DEV-V6-02B (宿主转达, V6-T2): the tidyable page range is bound by the
        // assembly root at Awake from the tidy feature's public seam — the UI
        // layer never names the feature project, and both consumers read the
        // ONE frozen source (unbound 0/0 keeps every gate fail-closed, the
        // same no-op family as an unbound engine dispatcher).
        internal static byte TidyablePageMin;
        internal static byte TidyablePageMax;

        /// <summary>宿主组合绑定：范围来自整理工程公开缝（组装根 Awake，与引擎分派器绑定
        /// 同一时刻；未绑定=所有门 fail-closed，测试宿主引擎路径本就静默 no-op）。</summary>
        internal static void BindTidyableRange(byte min, byte max)
        {
            TidyablePageMin = min;
            TidyablePageMax = max;
        }

        internal static void OnDashboardSurfaceOpened(byte firstPage, byte lastPage)
        {
            // DEV-V6-02B：页码范围由宿主传参（单源=整理工程公开缝，组装根绑定）——界面层
            // 不再指回整理工程内部常量（ClientUi→整理方向摘除）。
            OnTidyPagesCommitted(firstPage, lastPage);
        }

        /// <summary>
        /// Pure repair orchestration over the page seam: consult every page
        /// in the inclusive range, skip exact pages silently, rebuild inexact
        /// pages once and emit one BUE-LIT-001 diagnostic per repair. A null
        /// view (engine gate closed / page ineligible) is skipped — the
        /// fail-closed contract keeps repair best-effort and never throwing.
        /// Returns the repaired page count.
        /// </summary>
        internal static int ReconcileRange(byte firstPage, byte lastPage, Func<byte, IInventoryProjectionPageView> pageFactory)
        {
            if (pageFactory == null)
                return 0;

            var repaired = 0;
            var first = (int)firstPage;
            var last = (int)lastPage;
            for (var page = first; page <= last; page++)
            {
                var view = pageFactory((byte)page);
                if (view == null)
                    continue;

                var authoritative = new List<object>(view.AuthoritativeCount);
                for (var i = 0; i < view.AuthoritativeCount; i++)
                    authoritative.Add(view.AuthoritativeAt(i));
                var rendered = new List<object>(view.RenderedCount);
                for (var i = 0; i < view.RenderedCount; i++)
                    rendered.Add(view.RenderedJarAt(i));
                var pending = new List<object>(view.PendingCount);
                for (var i = 0; i < view.PendingCount; i++)
                    pending.Add(view.PendingJarAt(i));

                if (ProjectionReconcileDecision.IsExact(authoritative, rendered, pending))
                    continue;

                view.RepairFromAuthoritative();
                repaired++;
                var hostLog = HostLogSink;
                if (hostLog != null)
                    hostLog("[Tidy] listen-host 投影对账修复 page=" + view.Page
                        + " authoritative=" + authoritative.Count
                        + " renderedBefore=" + rendered.Count
                        + " pendingBefore=" + pending.Count
                        + " diagnosticId=" + DiagnosticId);
            }
            return repaired;
        }

        // ---- Pure gate decisions (the engine path calls these with engine
        // state; the host tests pin the truth tables without SDG) ----

        /// <summary>
        /// Listen-host eligibility: server AND client on the same instance
        /// with a local player. Mirrors the ecosystem's proven eligibility
        /// expression for this native defect; a dedicated server (U3DS) and
        /// a pure remote client both fail it.
        /// </summary>
        internal static bool IsEligibleLocalHostDecision(bool isServer, bool isClient, bool hasLocalPlayer)
        {
            return isServer && isClient && hasLocalPlayer;
        }

        /// <summary>
        /// Dashboard page range gate — the tidyable mirror of
        /// PlayerInventory.SLOTS..PAGES-1 (2..6), single-sourced from the
        /// host-bound range (DEV-V6-02B 宿主转达: the assembly root binds it
        /// from the tidy feature's public seam) so the engine clamp and the
        /// open-trigger range can never drift apart.
        /// </summary>
        internal static bool IsReconcilablePage(int page)
        {
            return page >= TidyablePageMin
                && page <= TidyablePageMax;
        }

        // ---- Engine path (SDG-touching; NoInlining; never JIT'd by tests) ----

        private static FieldInfo dashboardItemsField;
        private static FieldInfo pendingItemsField;
        private static bool contractResolved;
        private static bool contractAvailable;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReconcileRangeEngine(byte firstPage, byte lastPage)
        {
            try
            {
                if (!IsEligibleLocalHostEngine())
                    return;
                if (!EnsureReflectionContractEngine())
                    return;
                ReconcileRange(firstPage, lastPage, BuildEnginePageView);
            }
            catch (Exception error)
            {
                // UI repair must never interrupt its callers (tidy publish /
                // inventory open): one Warning line, then carry on.
                var hostWarn = HostWarnSink;
                if (hostWarn != null)
                    hostWarn("[Tidy] listen-host 投影对账中止 errorType=" + error.GetType().Name
                        + " message=" + error.Message
                        + " diagnosticId=" + AbortDiagnosticId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsEligibleLocalHostEngine()
        {
            var player = Player.LocalPlayer;
            return IsEligibleLocalHostDecision(Provider.isServer, Provider.isClient, player != null && player.inventory != null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool EnsureReflectionContractEngine()
        {
            if (contractResolved)
                return contractAvailable;
            contractResolved = true;

            FieldInfo dashboard = null;
            FieldInfo pending = null;
            try
            {
                dashboard = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "items");
                pending = AccessTools.Field(typeof(SleekItems), "pendingItems");
            }
            catch (Exception)
            {
                dashboard = null;
                pending = null;
            }
            contractAvailable = dashboard != null && dashboard.FieldType == typeof(SleekItems[])
                && pending != null && pending.FieldType == typeof(List<ItemJar>);
            dashboardItemsField = dashboard;
            pendingItemsField = pending;
            return contractAvailable;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IInventoryProjectionPageView BuildEnginePageView(byte page)
        {
            var player = Player.LocalPlayer;
            if (player == null)
                return null;
            var inventory = player.inventory;
            if (inventory == null || inventory.items == null)
                return null;
            if (!IsReconcilablePage(page) || page >= inventory.items.Length)
                return null;
            var model = inventory.items[page];
            if (model == null)
                return null;
            if (dashboardItemsField == null || pendingItemsField == null)
                return null;
            var dashboardPages = dashboardItemsField.GetValue(null) as SleekItems[];
            if (dashboardPages == null)
                return null;
            var dashboardIndex = page - PlayerInventory.SLOTS;
            if (dashboardIndex < 0 || dashboardIndex >= dashboardPages.Length)
                return null;
            var projection = dashboardPages[dashboardIndex];
            if (projection == null || projection.items == null)
                return null;
            var pending = pendingItemsField.GetValue(projection) as List<ItemJar>;
            if (pending == null)
                return null;
            return new SleekItemsProjectionPageView(page, player, model, projection, pending);
        }

        /// <summary>
        /// Engine page view. Repair mirrors the ecosystem's event-driven
        /// rebuild exactly (clear → resize → re-add in authoritative order →
        /// re-project still-valid vanilla hotkeys on ordinary pages, storage
        /// excluded) and never mutates inventory state.
        /// </summary>
        internal sealed class SleekItemsProjectionPageView : IInventoryProjectionPageView
        {
            private readonly byte page;
            private readonly Player player;
            private readonly Items model;
            private readonly SleekItems projection;
            private readonly List<ItemJar> pending;

            internal SleekItemsProjectionPageView(byte page, Player player, Items model, SleekItems projection, List<ItemJar> pending)
            {
                this.page = page;
                this.player = player;
                this.model = model;
                this.projection = projection;
                this.pending = pending;
            }

            public byte Page { get { return page; } }
            public int AuthoritativeCount { get { return model.getItemCount(); } }
            public object AuthoritativeAt(int index) { return model.getItem((byte)index); }
            public int RenderedCount { get { return projection.items.Count; } }
            public object RenderedJarAt(int index) { return projection.items[index]?.jar; }
            public int PendingCount { get { return pending.Count; } }
            public object PendingJarAt(int index) { return pending[index]; }

            public void RepairFromAuthoritative()
            {
                projection.clear();
                projection.resize(model.width, model.height);
                for (byte index = 0; index < model.getItemCount(); index++)
                {
                    var jar = model.getItem(index);
                    if (jar == null)
                        continue;
                    projection.addItem(jar);
                    // clear() also removes the vanilla hotkey labels —
                    // re-project any still-valid hotkey on ordinary pages;
                    // storage is intentionally excluded (ecosystem parity).
                    if (page < PlayerInventory.STORAGE && player != null && player.equipment != null
                        && player.equipment.isItemHotkeyed(page, index, jar, out var button))
                    {
                        projection.updateHotkey(jar, button);
                    }
                }
            }
        }
    }
}
