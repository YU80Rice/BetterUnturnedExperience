using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BetterUnturnedExperience.Plugin;
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

        internal static void OnDashboardSurfaceOpened()
        {
            OnTidyPagesCommitted(BetterUnturnedExperience.Lit.HotkeySnapshotUtil.TIDYABLE_PAGE_MIN, BetterUnturnedExperience.Lit.HotkeySnapshotUtil.TIDYABLE_PAGE_MAX);
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
                BueRuntimeLog.Runtime("[Tidy] listen-host 投影对账修复 page=" + view.Page
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
        /// Lit tidy constants so the engine clamp and the open-trigger range
        /// can never drift apart.
        /// </summary>
        internal static bool IsReconcilablePage(int page)
        {
            return page >= BetterUnturnedExperience.Lit.HotkeySnapshotUtil.TIDYABLE_PAGE_MIN
                && page <= BetterUnturnedExperience.Lit.HotkeySnapshotUtil.TIDYABLE_PAGE_MAX;
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
                BueRuntimeLog.Warn("[Tidy] listen-host 投影对账中止 errorType=" + error.GetType().Name
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
