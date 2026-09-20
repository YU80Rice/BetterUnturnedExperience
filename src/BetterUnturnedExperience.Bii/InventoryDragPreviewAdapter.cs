using System;
using System.Collections.Generic;
using Action = System.Action;
using BetterUnturnedExperience.Contracts;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Bii
{
    /// <summary>
    /// DEV-16D drag preview/commit adapter. Driven by the vanilla
    /// PlayerDashboardInventoryUI.updateDraggedItem tick (postfix) and a
    /// rebound SleekItems.onPlacedItem delegate; reads only
    /// public native state (isDragging/dragJar/dragFrom*) plus the private
    /// drag fields through cached FieldInfo kept inside this adapter file.
    /// Routes decisions through the composition's ItemInteractionUiComponent
    /// and forwards the native commit via sendDragItem.
    /// </summary>
    internal sealed class InventoryDragPreviewAdapter
    {
        
        private readonly Harmony harmony;
        /// <summary>DEV-V6-05: the frozen Harmony identity of this adapter's patch set —
        /// ONE source for the Harmony instance and the platform pocket handle.</summary>
        internal const string DragHarmonyId = "io.github.yu80rice.bue.drag-preview";
        private readonly bool enabled;
        private string gateDiagnostics;

        private BetterItemInteractionUiComponent component;
        private bool hooksInstalled;
        private bool isolated;
        private bool cleanupSucceeded = true;
        private bool wasDragging;
        private uint dragGeneration;
        private System.Reflection.FieldInfo dragJarField;
        private System.Reflection.FieldInfo dragFromPageField;
        private System.Reflection.FieldInfo dragFromXField;
        private System.Reflection.FieldInfo dragFromYField;
        private System.Reflection.FieldInfo dragFromRotField;
        private System.Reflection.FieldInfo dragPivotField;
        private System.Reflection.FieldInfo dragItemField;
        private NativeDragActions nativeActions;
        private int lastPollFrame = -1;
        private int lastDiagnosticTick;
        private PlacementPreviewState lastDiagnosticState;
        private PlacementReason lastDiagnosticReason;
        private bool hasDiagnostic;

        internal bool Enabled { get { return enabled; } }
        internal string GateDiagnostics { get { return gateDiagnostics; } }
        internal bool HooksInstalled { get { return hooksInstalled; } }
        internal bool Isolated { get { return isolated; } }

        /// <summary>DEV-V6-05: the Harmony identity of this adapter's patch set
        /// (the platform pocket's registration line names it).</summary>
        internal string HarmonyId { get { return DragHarmonyId; } }

        /// <summary>DEV-V6-05: true once this adapter's ORDERLY teardown
        /// skipped its own unpatch because the pocket accepted the handle —
        /// the platform's stop/isolation release owns that unpatch (the
        /// ownership split; the drift-isolation paths below always unpatch
        /// themselves, fail-closed).</summary>
        internal bool PatchTeardownDeferredToPlatform { get; private set; }

        /// <summary>DEV-V6-05: the patch-pocket handle for THIS adapter's patch
        /// set — the rig registers it before any activation, so the platform
        /// owns the stop-boundary teardown of a hook that a failed activation
        /// or a forgotten teardown would otherwise leave live. Real-time
        /// isolation (drift) keeps its own fail-closed unpatch.</summary>
        internal IDisposable PatchHandle(Action<string> log = null)
        {
            return new HarmonyPatchHandle(harmony, DragHarmonyId, log);
        }

        // DEV-V6-02E: the host log mouths ride the injected services bag (the
        // pre-move source called the host's internal BueRuntimeLog directly);
        // unbound = honest swallow. DEV-V6-04: the mouths ride the Bii seams
        // snapshot now (the UI services bag stayed with the UI project).
        private readonly BiiCompositionMouths hostServices;

        internal InventoryDragPreviewAdapter(BetterItemInteractionUiComponent component,
            BiiCompositionMouths mouths = null)
        {
            this.hostServices = mouths;

            this.component = component ?? throw new ArgumentNullException(nameof(component));
            harmony = new Harmony(DragHarmonyId);
            nativeActions = new NativeDragActions();
            dragJarField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragJar");
            dragFromPageField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFromPage");
            dragFromXField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFrom_x");
            dragFromYField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFrom_y");
            dragFromRotField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFromRot");
            dragPivotField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragPivot");
            dragItemField = AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragItem");

            var missing = new System.Collections.Generic.List<string>();
            if (AccessTools.Method(typeof(PlayerDashboardInventoryUI), "onPlacedItem") == null) missing.Add("PlayerDashboardInventoryUI.onPlacedItem");
            if (AccessTools.Method(typeof(PlayerDashboardInventoryUI), "stopDrag") == null) missing.Add("PlayerDashboardInventoryUI.stopDrag");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragJar") == null) missing.Add("PlayerDashboardInventoryUI.dragJar");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragFromPage") == null) missing.Add("PlayerDashboardInventoryUI.dragFromPage");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragPivot") == null) missing.Add("PlayerDashboardInventoryUI.dragPivot");
            if (AccessTools.Field(typeof(PlayerDashboardInventoryUI), "dragItem") == null) missing.Add("PlayerDashboardInventoryUI.dragItem");
            if (AccessTools.Method(typeof(PlayerDashboardInventoryUI), "updateDraggedItem") == null) missing.Add("PlayerDashboardInventoryUI.updateDraggedItem");
            if (missing.Count > 0)
            {
                var diagnostics = new System.Text.StringBuilder("drag-preview-gate: native members missing, wiring disabled, native drag preserved -> ");
                for (var index = 0; index < missing.Count; index++)
                {
                    if (index > 0) diagnostics.Append(", ");
                    diagnostics.Append(missing[index]);
                }
                gateDiagnostics = diagnostics.ToString();
                enabled = false;
                return;
            }
            enabled = true;
        }

        internal void Activate()
        {
            if (!enabled)
            {
                component.IsolatePreviewFailure();
                return;
            }
            if (hooksInstalled || isolated) return;
            try
            {
                // Drives the drag poll on the dashboard-inventory UI tick itself
                // (runs every frame while any inventory page is visible). The
                // onPlacedItem patch is NOT used: its 170-line method body
                // fails Harmony's IL recompile on this game build. Placement
                // interception instead rebinds the public SleekItems
                // .onPlacedItem delegate per dispatch (see AttachGrid).
                harmony.Patch(AccessTools.Method(typeof(PlayerDashboardInventoryUI), "updateDraggedItem"),
                    postfix: new HarmonyMethod(typeof(InventoryDragPreviewAdapter), nameof(DashboardUpdatePostfix)));
                hooksInstalled = true;
                ActiveAdapter = this;
                // DEV-16G slice B: bind the one-shot failure sink to this
                // adapter's host log mouth so runtime isolations emit a
                // "reason:" line exactly once.
                DiagnosticLogSink = line => EmitErrorFriendly("[BUE-DRAG] event=diagnostic-failure " + line + " diagnosticId=BUE-DRAG-003");
                // DEV-16G ticket D: hooks-installed is a per-subsystem load
                // one-shot, demoted to Debug; the aggregate ready line is the
                // only load-stage Info announcement.
                EmitRuntime("[BUE-DRAG] event=hooks-installed targets=updateDraggedItem diagnosticId=BUE-DRAG-001");
            }
            catch (Exception error)
            {
                gateDiagnostics = "hooks-failed: " + error.GetType().FullName + ": " + error.Message;
                hooksInstalled = false;
                component.IsolatePreviewFailure();
            }
        }

        // [DEV-16D] Placement interception without Harmony: each session
        // dispatch rebinds the grid's public onPlacedItem delegate to a BUE
        // wrapper that runs the take-over decision first and only forwards to
        // the vanilla handler on pass-through. Rebinding re-runs per dispatch
        // so a rebuilt UI gets fresh delegates.
        private readonly Dictionary<byte, AttachedGridBinding> attachedGrids =
            new Dictionary<byte, AttachedGridBinding>();

        private sealed class AttachedGridBinding
        {
            internal readonly SleekItems Grid;
            internal readonly PlacedItem NativeHandler;
            internal readonly PlacedItem Wrapper;

            internal AttachedGridBinding(SleekItems grid, PlacedItem nativeHandler, PlacedItem wrapper)
            {
                Grid = grid;
                NativeHandler = nativeHandler;
                Wrapper = wrapper;
            }
        }

        internal int AttachedGridCount { get { return attachedGrids.Count; } }

        // Rebuilt native surfaces are only attachable while this adapter is
        // live. Once isolation starts, no new delegate may be written.
        internal static bool CanAttachGrid(bool isolated)
        {
            return !isolated;
        }

        internal static bool CanContinueGridAttach(bool detachSucceeded, bool isolated)
        {
            return detachSucceeded && !isolated;
        }

        internal static bool ShouldIsolateOnHookFailure(bool hookInstalled)
        {
            return !hookInstalled;
        }

        internal void AttachGrid(IInventorySurfaceContext surface)
        {
            if (!CanAttachGrid(isolated))
            {
                LastPollDiagnostics = "grid-attach-rejected-isolated";
                return;
            }
            try
            {
                var context = surface as UnturnedInventorySurfaceContext;
                var sleek = context?.NativeItems;
                if (sleek == null)
                    throw new InvalidOperationException("context has no native SleekItems");
                var page = context.CurrentContainer.Page;
                AttachNativeGrid(sleek, page);
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "grid-attach-failed: " + error.GetType().FullName + ": " + error.Message;
                EmitWarn("[BUE-DRAG] event=attach-grid-failed diagnosticId=BUE-DRAG-003");
                IsolateAndDetach();
            }
        }

        // GPT watermark: highest-available native delegate seam. Production
        // surface dispatch and host tests both enter this operation so the
        // wrapper, exact-original capture, page-local detach and rebind rules
        // cannot drift into separate synthetic implementations.
        internal bool AttachNativeGrid(SleekItems sleek, byte page)
        {
            if (!CanAttachGrid(isolated))
            {
                LastPollDiagnostics = "grid-attach-rejected-isolated";
                return false;
            }
            if (sleek == null) throw new ArgumentNullException(nameof(sleek));
            if (!IsSupportedPage(page)) return false;
            try
            {
                AttachedGridBinding existing;
                if (attachedGrids.TryGetValue(page, out existing) && ReferenceEquals(sleek, existing.Grid))
                    return true;
                // Re-entry for a rebuilt page must only detach that page; the
                // other supported page remains live for cross-page drags.
                var detachSucceeded = DetachGrid(page);
                if (!CanContinueGridAttach(detachSucceeded, isolated)) return false;
                var wrapper = new PlacedItem(GridPlacedItemWrapper);
                attachedGrids[page] = new AttachedGridBinding(sleek, sleek.onPlacedItem, wrapper);
                sleek.onPlacedItem = wrapper;
                EmitRuntime("[BUE-DRAG] event=placed-item-delegate-rebound page=" + sleek.page + " diagnosticId=BUE-DRAG-001");
                return true;
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "grid-attach-failed: " + error.GetType().FullName + ": " + error.Message;
                EmitWarn("[BUE-DRAG] event=attach-grid-failed diagnosticId=BUE-DRAG-003");
                IsolateAndDetach();
                return false;
            }
        }

        internal bool DetachGrid()
        {
            var success = true;
            foreach (var page in SupportedPages)
            {
                if (!DetachGrid(page)) success = false;
            }
            return success;
        }

        internal bool DetachGrid(byte page)
        {
            AttachedGridBinding binding;
            if (!attachedGrids.TryGetValue(page, out binding)) return true;
            try
            {
                if (ReferenceEquals(binding.Grid.onPlacedItem, binding.Wrapper))
                    binding.Grid.onPlacedItem = binding.NativeHandler;
                attachedGrids.Remove(page);
                return true;
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "grid-detach-failed: " + error.GetType().FullName + ": " + error.Message;
                return false;
            }
        }

        // GPT watermark: the production lifecycle callback uses one ordered
        // operation: restore the exact native delegate first, then discard the
        // corresponding component surface. This is also the regression seam.
        internal bool DetachGridAndDiscardSurface(byte page)
        {
            var detachSucceeded = DetachGrid(page);
            var surfaceDiscarded = component.DiscardInventorySurface(page);
            return detachSucceeded && surfaceDiscarded;
        }

        // U3-SDK PlayerInventory pages: 2=Hands, 3=Backpack, 4=Vest, 5=Shirt,
        // 6=Pants, 7=Storage/trunk, 8=AREA. Every player grid page gets the
        // placed-item wrapper so its grid can be enhanced; AREA stays native.
        // Keep the adapter's static gate independent from PlayerInventory's
        // network-reflection type initializer so headless test hosts can load
        // the plugin without invoking native RPC setup.
        private const byte HandsPage = 2;
        private const byte BackpackPage = 3;
        private const byte VestPage = 4;
        private const byte ShirtPage = 5;
        private const byte PantsPage = 6;
        private const byte StoragePage = 7;
        private static readonly byte[] SupportedPages =
            { HandsPage, BackpackPage, VestPage, ShirtPage, PantsPage, StoragePage };

        internal static bool IsSupportedPage(byte page)
        {
            return page >= HandsPage && page <= StoragePage;
        }

        // GPT watermark: detach every native callback and event subscription
        // before isolating the feature, so an exception cannot leave a stale
        // wrapper or static adapter receiving future frames.
        internal bool IsolateAndDetach()
        {
            return IsolateAndDetach(true);
        }

        /// <summary>DEV-V6-05: releasePatches=false is the ORDERLY teardown
        /// path taken only when the pocket accepted this adapter's handle —
        /// the platform's account release performs the unpatch, so the module
        /// no longer double-owns it. Every other call site (drift isolation,
        /// cleanup chain, hook failure) keeps the default fail-closed unpatch.
        /// </summary>
        internal bool IsolateAndDetach(bool isolateComponent, bool releasePatches = true)
        {
            if (isolated)
            {
                return cleanupSucceeded;
            }
            isolated = true;
            LastCleanupDiagnostics = null;
            PatchTeardownDeferredToPlatform = !releasePatches;
            var detachSucceeded = DetachAndDeactivate(releasePatches);
            // Lock the detach result before entering component isolation.  The
            // component's registered cleanup includes a re-entrant
            // IsolateAndDetach(false) callback; it must observe a failed
            // detach instead of the field's default true value.
            cleanupSucceeded = detachSucceeded;
            cleanupSucceeded = CompleteIsolationCleanup(
                cleanupSucceeded,
                isolateComponent ? new Func<bool, bool>(_ => component.IsolatePreviewFailureResult()) : null,
                component.HidePreview);
            return cleanupSucceeded;
        }

        // GPT watermark: the detach result is seeded before the component
        // callback runs, making nested cleanup observe the real failure state.
        internal static bool CompleteIsolationCleanup(
            bool detachSucceeded,
            Func<bool, bool> isolateComponent,
            Action hide)
        {
            var cleanupSucceeded = detachSucceeded;
            var previewSucceeded = FailClosedPreviewResult(
                isolateComponent == null ? null : new Func<bool>(() => isolateComponent(cleanupSucceeded)),
                hide);
            return cleanupSucceeded && previewSucceeded;
        }

        private bool DetachAndDeactivate(bool releasePatches = true)
        {
            var success = true;
            try { if (!DetachGrid()) success = false; }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("grid-detach", error);
            }
            try { UnsubscribeInventoryEvents(); }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("inventory-unsubscribe", error);
            }
            if (releasePatches)
            {
                try { harmony.UnpatchSelf(); }
                catch (Exception error)
                {
                    success = false;
                    ReportCleanupFailure("hook-unpatch", error);
                }
            }
            hooksInstalled = false;
            ClearActive(this);
            return success;
        }

        private void GridPlacedItemWrapper(byte page, byte x, byte y)
        {
            AttachedGridBinding binding;
            var native = attachedGrids.TryGetValue(page, out binding) ? binding.NativeHandler : null;
            InvokePlacedItemGuarded(
                () => EvaluatePlacement(page, x, y),
                () => native?.Invoke(page, x, y),
                component.IsolatePreviewFailureResult,
                component.HidePreview,
                () => IsolateAndDetach());
        }

        // GPT watermark: the U3-SDK invokes SleekItems.onPlacedItem directly
        // without an exception boundary. Keep the BUE wrapper fail-closed while
        // forwarding evaluation failures back to the vanilla callback.
        internal static void InvokePlacedItemGuarded(Func<bool> evaluate, Action forward, Action isolate, Action hide)
        {
            InvokePlacedItemGuarded(evaluate, forward, isolate, hide, null);
        }

        internal static void InvokePlacedItemGuarded(Func<bool> evaluate, Action forward, Action isolate, Action hide, Action detach)
        {
            InvokePlacedItemGuarded(
                evaluate,
                forward,
                isolate == null ? null : new Func<bool>(() =>
                {
                    isolate();
                    return true;
                }),
                hide,
                detach == null ? null : new Func<bool>(() =>
                {
                    detach();
                    return true;
                }));
        }

        // GPT watermark: placed-item failure handling keeps result-bearing
        // isolation and detach callbacks intact, so CleanupIncomplete can
        // reach the lifecycle instead of being coerced to success.
        internal static void InvokePlacedItemGuarded(Func<bool> evaluate, Action forward,
            Func<bool> isolate, Action hide, Func<bool> detach)
        {
            bool passThrough;
            try
            {
                passThrough = evaluate == null || evaluate();
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "placed-item-evaluate-failed: " + error.GetType().FullName + ": " + error.Message;
                var detachSucceeded = TryDetachResult(detach);
                var previewSucceeded = FailClosedPreviewResult(isolate, hide);
                if (!detachSucceeded || !previewSucceeded)
                    ReportCleanupIncomplete("placed-item");
                try { forward?.Invoke(); } catch (Exception nativeError)
                {
                    LastPollDiagnostics = "placed-item-native-fallback-failed: " + nativeError.GetType().FullName + ": " + nativeError.Message;
                }
                return;
            }

            if (!passThrough) return;
            try { forward?.Invoke(); }
            catch (Exception error)
            {
                LastPollDiagnostics = "placed-item-native-failed: " + error.GetType().FullName + ": " + error.Message;
                var detachSucceeded = TryDetachResult(detach);
                var previewSucceeded = FailClosedPreviewResult(isolate, hide);
                if (!detachSucceeded || !previewSucceeded)
                    ReportCleanupIncomplete("placed-item");
            }
        }

        private static bool TryDetachResult(Func<bool> detach)
        {
            if (detach == null) return true;
            try
            {
                if (detach()) return true;
                ReportCleanupIncomplete("placed-item-detach");
                return false;
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "placed-item-detach-failed: " + error.GetType().FullName + ": " + error.Message;
                ReportCleanupFailure("placed-item-detach", error);
                return false;
            }
        }

        internal static void ReportCleanupFailure(string stage, Exception error)
        {
            LastCleanupDiagnostics = BuildCleanupIncompleteDiagnostics(stage,
                "errorType=" + error.GetType().FullName + " message=" + error.Message);
            LastPollDiagnostics = stage + " cleanup failed: " + error.GetType().FullName + ": " + error.Message;
            EmitDiagnosticOnce(LastCleanupDiagnostics);
        }

        internal static void ReportCleanupIncomplete(string stage)
        {
            LastCleanupDiagnostics = BuildCleanupIncompleteDiagnostics(stage, "result=false");
            EmitDiagnosticOnce(LastCleanupDiagnostics);
        }

        // GPT watermark: DEV-16G slice B. Static one-shot diagnostic emission
        // seam shared with InventorySurfaceLifecycleAdapter. Production binds a
        // host log writer during Activate; tests swap in a recorder to
        // assert a real failure emits its "reason:" line. "Once" here means one
        // line per failure stage (a compound failure may emit one line per
        // stage); the terminal isolation guard keeps the total bounded.
        internal static System.Action<string> DiagnosticLogSink = null;

        internal static void EmitDiagnosticOnce(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            var sink = DiagnosticLogSink;
            if (sink != null)
            {
                sink(line);
                return;
            }
            // No bound sink in this host (headless / test): the line is still
            // retained in LastCleanupDiagnostics for runtime consumption (the
            // IsolateAndDispatch guard and InvokePollGuarded append read it).
        }

        private static string BuildCleanupIncompleteDiagnostics(string stage, string detail)
        {
            return "featureId=io.github.yu80rice.bue.better-item-interaction"
                + " errorCode=CleanupIncomplete diagnosticId=BUE-DEV15D-CLEANUP-INCOMPLETE"
                + " stage=" + stage + " " + detail;
        }

        internal static void DashboardUpdatePostfix()
        {
            var adapter = ActiveAdapter;
            if (adapter == null)
            {
                return;
            }
            if (adapter.isolated)
            {
                return;
            }
            try
            {
                adapter.Tick();
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "poll failed: " + error.GetType().FullName + ": " + error.Message;
                adapter.IsolateAndDetach();
            }
        }

        internal static InventoryDragPreviewAdapter ActiveAdapter { get; private set; }

        internal static void ClearActive(InventoryDragPreviewAdapter adapter)
        {
            if (ReferenceEquals(ActiveAdapter, adapter)) ActiveAdapter = null;
        }

        internal static string LastPollDiagnostics { get; private set; }
        internal static string LastCleanupDiagnostics { get; private set; }

        // GPT watermark: the updateDraggedItem postfix can run before the
        // PlayerUI.Update lifecycle poll has published the first surface for a
        // newly opened inventory. Do not consume that frame while a drag is in
        // progress and no surface exists; the later same-frame lifecycle tick
        // must remain able to evaluate the preview.
        internal static bool ShouldCommitPollFrame(bool isDragging, bool hasSurface)
        {
            return !isDragging || hasSurface;
        }

        // GPT watermark: R13 target routing must use the live native surface
        // whose pointer currently contains the cursor. This seam is kept
        // explicit so the dual-page route is regression-tested independently
        // from Unity's concrete pointer implementation.
        internal static bool TrySelectTargetSurface(BetterItemInteractionUiComponent component,
            out IInventorySurfaceContext surface, out float localX, out float localY)
        {
            surface = null;
            localX = 0f;
            localY = 0f;
            if (component == null) return false;
            return component.TrySelectSurfaceForPointer(out surface, out localX, out localY);
        }

        // GPT watermark: reliable plugin-owned main-thread fallback, matching
        // UPM's BaseUnityPlugin.Update driver; Harmony remains a fast path.
        internal static string DescribeTickGate(bool lifecycleCanRun, bool enhancedDragActive, FeatureState state)
        {
            if (lifecycleCanRun) return "lifecycle-gate reason=can-run";
            if (enhancedDragActive) return "lifecycle-gate reason=enhanced-active";
            return "lifecycle-gate reason=blocked lifecycleCanRun=" + lifecycleCanRun
                + " enhancedDragActive=" + enhancedDragActive + " state=" + state;
        }

        internal static string DescribeAdapterGate(bool enabled, bool isolated)
        {
            if (!enabled) return "adapter-gate reason=disabled";
            if (isolated) return "adapter-gate reason=isolated";
            return "adapter-gate reason=live";
        }

        internal void Tick()
        {
            if (!enabled || isolated)
            {
                return;
            }
            if (!component.LifecycleCanRun && !component.EnhancedDragActive)
            {
                component.HidePreview();
                return;
            }
            var frame = Time.frameCount;
            if (frame == lastPollFrame) return;
            var isDragging = PlayerDashboardInventoryUI.isDragging;
            try
            {
                Poll();
                component.Tick((uint)Environment.TickCount);
                if (ShouldCommitPollFrame(isDragging, component.CurrentSurface != null))
                    lastPollFrame = frame;
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "plugin-update poll failed: " + error.GetType().FullName + ": " + error.Message;
                IsolateAndDetach();
            }
        }

        // GPT watermark: isolate and hide are deliberately separate guarded
        // callbacks so a cleanup exception cannot prevent stale visuals from
        // being cleared or re-enable the native fallback path.
        internal static bool FailClosedPreview(Action isolate, Action hide)
        {
            return FailClosedPreviewResult(isolate == null ? null : new Func<bool>(() =>
            {
                isolate();
                return true;
            }), hide);
        }

        internal static bool FailClosedPreviewResult(Func<bool> isolate, Action hide)
        {
            var success = true;
            try
            {
                if (isolate != null && !isolate())
                {
                    success = false;
                    ReportCleanupIncomplete("preview-isolate");
                }
            }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("preview-isolate", error);
            }
            try { hide?.Invoke(); }
            catch (Exception error)
            {
                success = false;
                ReportCleanupFailure("preview-hide", error);
            }
            return success;
        }

        private void Poll()
        {
            if (!component.LifecycleCanRun && !component.EnhancedDragActive)
            {
                return;
            }
            var isDragging = PlayerDashboardInventoryUI.isDragging;
            if (isDragging && !wasDragging)
            {
                dragGeneration++;
                var jar = ReadDragJar();
                var asset = jar == null ? ItemAssetIdentity.FromItemId(0) : AssetIdentityOf(jar);
                component.OnDragStarted(dragGeneration, asset, ReadDragSource());
                EmitRuntime("[BUE-DRAG] event=drag-started generation=" + dragGeneration
                    + " enhanced=" + component.EnhancedDragActive
                    + " canRun=" + component.LifecycleCanRun
                    + " sinkBound=" + component.PreviewSinkBound
                    + " diagnosticId=BUE-DRAG-001");
            }
            else if (!isDragging && wasDragging)
            {
                // Drag ended without onPlacedItem (ESC, drag-out): cancel visuals.
                component.OnDragCancelled();
                EmitRuntime("[BUE-DRAG] event=drag-cancelled diagnosticId=BUE-DRAG-001");
            }
            wasDragging = isDragging;

            EnsureInventoryEventSubscription();

            if (isDragging)
            {
                // [R45] Read the same native grid/scroll hierarchy used by
                // SleekItems.onClickedGrid. The grid-local content point
                // already includes the scroll transform; no second scroll is
                // added by the pure-C# adapter.
                IInventorySurfaceContext selectedSurface;
                float localX;
                float localY;
                if (!TrySelectTargetSurface(component, out selectedSurface, out localX, out localY))
                {
                    component.HidePreview();
                    if (ShouldEmitDiagnostic(PlacementPreviewState.Hidden, PlacementReason.OutsideGrid))
                        EmitRuntime("[BUE-DRAG] GPT-WATERMARK event=preview-hidden reason=outside-viewport generation=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
                    return;
                }
                var surface = selectedSurface as UnturnedInventorySurfaceContext;
                if (surface == null)
                {
                    component.HidePreview();
                    if (ShouldEmitDiagnostic(PlacementPreviewState.Hidden, PlacementReason.FeatureUnavailable))
                        EmitRuntime("[BUE-DRAG] GPT-WATERMARK event=preview-hidden reason=surface-not-native generation=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
                    return;
                }
                var source = ReadDragSource();
                var dragJar = ReadDragJar();
                var topLevelPointer = ReadTopLevelPointerScale();
                var nativePivot = ReadDragPivot();
                if (component.TryCreatePreviewInput(dragGeneration, source, localX,
                        localY, ReadDragWidth(), ReadDragHeight(), ReadDragRotation(),
                        true, ReadGrabOffsetX(), ReadGrabOffsetY(), AssetIdentityOf(dragJar),
                        topLevelPointer.x, topLevelPointer.y, nativePivot.x, nativePivot.y, out var input))
                {
                    component.OnDragUpdated(input);
                    var state = component.LastPreview.State;
                    if (ShouldEmitDiagnostic(state, component.LastPreview.Reason))
                    {
                        LogPreviewInputReadout(input, Input.mousePosition.x, Input.mousePosition.y,
                            localX, localY, state, component.LastPreview.Reason);
                        EmitRuntime("[BUE-DRAG] GPT-WATERMARK event=preview-evaluated generation=" + dragGeneration + " state=" + state + " diagnosticId=BUE-DRAG-001");
                        if (state == PlacementPreviewState.Candidate || state == PlacementPreviewState.LocallyInvalid)
                            EmitRuntime("[BUE-DRAG] GPT-WATERMARK event=preview-visible generation=" + dragGeneration + " state=" + state + " diagnosticId=BUE-DRAG-001");
                    }
                }
                else
                {
                    component.HidePreview();
                    if (ShouldEmitDiagnostic(PlacementPreviewState.Hidden, PlacementReason.FeatureUnavailable))
                        EmitRuntime("[BUE-DRAG] GPT-WATERMARK event=preview-input-rejected generation=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
                }
            }
        }

        private bool ShouldEmitDiagnostic(PlacementPreviewState state, PlacementReason reason)
        {
            var now = Environment.TickCount;
            if (!hasDiagnostic || state != lastDiagnosticState || reason != lastDiagnosticReason || now - lastDiagnosticTick >= 500)
            {
                hasDiagnostic = true;
                lastDiagnosticState = state;
                lastDiagnosticReason = reason;
                lastDiagnosticTick = now;
                return true;
            }
            return false;
        }

        private void LogPreviewInputReadout(InventoryPreviewInput input, float rawMouseX, float rawMouseY,
            float uiX, float uiY, PlacementPreviewState state, PlacementReason reason)
        {
            var scaledCell = input.CellPixelSize * input.UiScale;
            var gridX = scaledCell > 0f
                ? (uiX - input.Viewport.OriginX + input.ScrollPixelsX) / scaledCell
                : float.NaN;
            var gridY = scaledCell > 0f
                ? (uiY - input.Viewport.OriginY + input.ScrollPixelsY) / scaledCell
                : float.NaN;
            EmitRuntime("[BUE-DRAG] GPT-WATERMARK event=preview-input-readout"
                + " generation=" + input.DragGeneration
                + " pointerScreen=" + rawMouseX.ToString("0.###") + "," + rawMouseY.ToString("0.###")
                + " uiScale=" + input.UiScale.ToString("0.###")
                + " uiCoordinates=" + uiX.ToString("0.###") + "," + uiY.ToString("0.###")
                + " viewportOrigin=" + input.Viewport.OriginX.ToString("0.###") + "," + input.Viewport.OriginY.ToString("0.###")
                + " pointerGrid=" + gridX.ToString("0.###") + "," + gridY.ToString("0.###")
                + " grabOffset=" + input.GrabOffsetX.ToString("0.###") + "," + input.GrabOffsetY.ToString("0.###")
                + " placementReason=" + reason
                + " state=" + state
                + " diagnosticId=BUE-DRAG-001");
        }

        // [DEV-16D] Native convergence feed: the three inventory events build
        // a NativeInventorySnapshot (monotonic NativeRevision) and feed the
        // component's awaiting-projection bridge. Subscribed per-Player, with
        // the subscription re-bound when Player.LocalPlayer changes.
        private Player subscribedPlayer;
        private uint nativeRevision;
        private uint lastSubmittedGeneration;



        private void EnsureInventoryEventSubscription()
        {
            var player = Player.LocalPlayer;
            if (ReferenceEquals(player, subscribedPlayer)) return;
            UnsubscribeInventoryEvents();
            subscribedPlayer = player;
            if (player == null) return;
            player.inventory.onInventoryAdded += OnNativeInventoryEvent;
            player.inventory.onInventoryRemoved += OnNativeInventoryEvent;
            player.inventory.onInventoryUpdated += OnNativeInventoryEvent;
            EmitRuntime("[BUE-DRAG] event=inventory-events-subscribed diagnosticId=BUE-DRAG-001");
        }

        private void UnsubscribeInventoryEvents()
        {
            if (subscribedPlayer == null) return;
            subscribedPlayer.inventory.onInventoryAdded -= OnNativeInventoryEvent;
            subscribedPlayer.inventory.onInventoryRemoved -= OnNativeInventoryEvent;
            subscribedPlayer.inventory.onInventoryUpdated -= OnNativeInventoryEvent;
            subscribedPlayer = null;
        }

        private void OnNativeInventoryEvent(byte page, byte index, ItemJar jar)
        {
            try
            {
                component.InvalidateOccupancySnapshot();
                if (lastSubmittedGeneration == 0) return;
                var asset = jar == null || jar.item == null ? ItemAssetIdentity.FromItemId(0) : AssetIdentityOf(jar);
                var assetInstance = jar == null ? null : jar.GetAsset();
                var itemW = assetInstance == null ? (byte)1 : (byte)Mathf.Min(assetInstance.size_x, byte.MaxValue);
                var itemH = assetInstance == null ? (byte)1 : (byte)Mathf.Min(assetInstance.size_y, byte.MaxValue);
                var fingerprint = new InventoryItemFingerprint(asset, itemW, itemH, jar == null ? (byte)0 : jar.rot);
                var container = component.LastDispatchedContainer;
                if (container.SessionGeneration == 0) return;
                nativeRevision++;
                // index is a list ordinal (compacted by RemoveAt), not a grid
                // position; the jar carries its own authoritative cell.
                var snapshot = new NativeInventorySnapshot(lastSubmittedGeneration, container, fingerprint,
                    new ItemGridPosition(page, jar == null ? (byte)0 : jar.x, jar == null ? (byte)0 : jar.y, jar == null ? (byte)0 : jar.rot),
                    nativeRevision);
                component.OnNativeInventorySnapshot(snapshot);
            }
            catch (Exception error)
            {
                LastPollDiagnostics = "convergence-event-failed: " + error.GetType().FullName + ": " + error.Message;
                IsolateAndDetach();
            }
        }

        private bool EvaluatePlacement(byte page, byte x, byte y)
        {
            // Returns false when BUE takes over the placement (legitimate grid
            // candidate or an invalid candidate the native path must not
            // execute); returns true for pass-through branches (slots, AREA,
            // and swaps onto an occupied same-page cell, which the native
            // onPlacedItem handles via sendSwapItem).
            if (!PlayerDashboardInventoryUI.isDragging) return true;
            var isOrdinaryGrid = page >= PlayerInventory.SLOTS && page != PlayerInventory.AREA;
            if (!isOrdinaryGrid) return true;
            // [R34 fail-open] Enhanced interaction off, or no fresh preview
            // evaluation, means BUE cannot judge the placement: the native
            // path owns it. BUE never blocks what it cannot evaluate.
            if (!component.EnhancedDragActive)
            {
                EmitRuntime("[BUE-DRAG] event=placement-passthrough reason=enhanced-off diagnosticId=BUE-DRAG-001");
                return true;
            }
            var preview = component.LastPreview;
            if (preview.State == 0 || preview.DragGeneration != dragGeneration)
            {
                EmitRuntime("[BUE-DRAG] event=placement-passthrough reason=preview-stale previewGen=" + preview.DragGeneration + " dragGen=" + dragGeneration + " diagnosticId=BUE-DRAG-001");
                return true;
            }

            // Vanilla performs swaps inside onPlacedItem while isDragging is
            // still true. Only an invalid occupied preview is handed back to
            // vanilla; a valid preview may intentionally choose a nearby free
            // cell and must remain BUE-owned.
            if (preview.State == PlacementPreviewState.LocallyInvalid &&
                preview.Reason == PlacementReason.Occupied && IsSwapOntoOccupied(page, x, y))
            {
                component.HidePreview();
                EmitRuntime("[BUE-DRAG] event=placement-passthrough reason=native-swap diagnosticId=BUE-DRAG-001");
                return true;
            }

            var input = new NativeDragAdapterInput(
                PlayerDashboardInventoryUI.isDragging, dragGeneration,
                ReadDragSource(), preview, page);
            var outcome = component.OnDragReleased(input, nativeActions);
            EmitRuntime("[BUE-DRAG] event=placement-decision page=" + page + " x=" + x + " y=" + y
                + " outcome=" + outcome + " diagnosticId=BUE-DRAG-001");
            if (outcome == NativeDragAdapterOutcome.Submitted)
            {
                lastSubmittedGeneration = dragGeneration;
                PlayerDashboardInventoryUI.stopDrag();
                return false;
            }
            if (outcome == NativeDragAdapterOutcome.Cancelled)
            {
                PlayerDashboardInventoryUI.stopDrag();
                return false;
            }
            return true;
        }

        // [DEV-16D] Swap guard footprint query operates on the canonical
        // occupancy seam. It does not build or own a second item grid.
        internal static bool FootprintOccupied(IGridOccupancyView occupancy, int originX, int originY, int itemW, int itemH)
        {
            if (occupancy == null) return true;
            for (var dy = 0; dy < itemH; dy++)
            for (var dx = 0; dx < itemW; dx++)
            {
                var cx = originX + dx;
                var cy = originY + dy;
                if (cx < 0 || cy < 0 || cx >= occupancy.Width || cy >= occupancy.Height) return true;
                if (occupancy.IsOccupied((byte)cx, (byte)cy)) return true;
            }
            return false;
        }

        private bool IsSwapOntoOccupied(byte page, byte x, byte y)
        {
            IGridOccupancyView occupancy;
            if (!component.TryGetOccupancyForDrag(ReadDragSource(), ReadDragWidth(), ReadDragHeight(),
                ReadDragRotation(), AssetIdentityOf(ReadDragJar()), out occupancy))
                return true;
            if (page != component.CurrentContainer.Page) return true;
            return FootprintOccupied(occupancy, x, y, 1, 1);
        }

        private ItemJar ReadDragJar()
        {
            return dragJarField == null ? null : dragJarField.GetValue(null) as ItemJar;
        }

        private ItemGridPosition ReadDragSource()
        {
            var page = dragFromPageField == null ? (byte)0 : (byte)Convert.ToByte(dragFromPageField.GetValue(null));
            var x = dragFromXField == null ? (byte)0 : (byte)Convert.ToByte(dragFromXField.GetValue(null));
            var y = dragFromYField == null ? (byte)0 : (byte)Convert.ToByte(dragFromYField.GetValue(null));
            var rotation = dragFromRotField == null ? (byte)0 : (byte)Convert.ToByte(dragFromRotField.GetValue(null));
            return new ItemGridPosition(page, x, y, rotation);
        }

        private static byte ReadDragWidth()
        {
            var jar = ActiveAdapter == null ? null : ActiveAdapter.ReadDragJar();
            var asset = jar == null ? null : jar.GetAsset();
            return asset == null ? (byte)1 : (byte)Mathf.Min(asset.size_x, byte.MaxValue);
        }

        private static byte ReadDragHeight()
        {
            var jar = ActiveAdapter == null ? null : ActiveAdapter.ReadDragJar();
            var asset = jar == null ? null : jar.GetAsset();
            return asset == null ? (byte)1 : (byte)Mathf.Min(asset.size_y, byte.MaxValue);
        }

        private static byte ReadDragRotation()
        {
            var jar = ActiveAdapter == null ? null : ActiveAdapter.ReadDragJar();
            return jar == null ? (byte)0 : jar.rot;
        }

        private float ReadGrabOffsetX()
        {
            var pivot = dragPivotField == null ? Vector2.zero : (Vector2)dragPivotField.GetValue(null);
            return NativePivotToGrabOffset(pivot).x;
        }

        private float ReadGrabOffsetY()
        {
            var pivot = dragPivotField == null ? Vector2.zero : (Vector2)dragPivotField.GetValue(null);
            return NativePivotToGrabOffset(pivot).y;
        }

        private Vector2 ReadDragPivot()
        {
            return dragPivotField == null ? Vector2.zero : (Vector2)dragPivotField.GetValue(null);
        }

        private SleekItem ReadDragItem()
        {
            return dragItemField == null ? null : dragItemField.GetValue(null) as SleekItem;
        }

        private static Vector2 ReadTopLevelPointerScale()
        {
            try
            {
                if (PlayerUI.container == null || Screen.width <= 0 || Screen.height <= 0)
                    return new Vector2(float.NaN, float.NaN);
                return PlayerUI.container.ViewportToNormalizedPosition(InputEx.NormalizedMousePosition);
            }
            catch (Exception)
            {
                return new Vector2(float.NaN, float.NaN);
            }
        }

        private static float ReadUiScale()
        {
            try { return GraphicsSettings.userInterfaceScale; } catch { return 1f; }
        }

        // GPT watermark: U3-SDK dragPivot is the negative pixel displacement
        // from the cursor to the item origin; the pure-C# seam expects the
        // positive cursor grab point in grid-cell units.
        private static Vector2 NativePivotToGrabOffset(Vector2 pivot)
        {
            return new Vector2(-pivot.x / 50f, -pivot.y / 50f);
        }

        private static ItemAssetIdentity AssetIdentityOf(ItemJar jar)
        {
            var asset = jar == null ? null : jar.GetAsset();
            if (asset == null) return ItemAssetIdentity.FromItemId(0);
            return ItemAssetIdentity.FromAsset(asset.id, asset.GUID, asset.name);
        }

        private sealed class NativeDragActions : INativeInventoryDragActions
        {
            public void StopDrag() { PlayerDashboardInventoryUI.stopDrag(); }

            public void SendDragItem(ItemGridPosition source, ItemGridPosition target)
            {
                var player = Player.LocalPlayer;
                if (player == null || player.inventory == null) return;
                player.inventory.sendDragItem(source.Page, source.X, source.Y,
                    target.Page, target.X, target.Y, target.Rotation);
            }

            public void TakeGroundItem(ItemGridPosition target)
            {
                var adapter = ActiveAdapter;
                var dragItem = adapter == null ? null : adapter.ReadDragItem();
                var interactable = dragItem == null || dragItem.jar == null ? null : dragItem.jar.interactableItem;
                if (interactable == null || interactable.transform == null || interactable.transform.parent == null)
                    throw new InvalidOperationException("native ground item is no longer available");
                ItemManager.takeItem(interactable.transform.parent, target.X, target.Y, target.Rotation, target.Page);
            }
        }

        // DEV-V6-02E host log mouths (unbound = honest swallow, same family
        // as the 02B/C/D absent-seam contract).
        private void EmitRuntime(string line)
        {
            var sink = hostServices != null ? hostServices.LogRuntime : null;
            if (sink != null) sink(line);
        }

        private void EmitErrorFriendly(string line)
        {
            var sink = hostServices != null ? hostServices.LogErrorFriendly : null;
            if (sink != null) sink(line);
        }

        private void EmitWarn(string line)
        {
            var sink = hostServices != null ? hostServices.LogWarn : null;
            if (sink != null) sink(line);
        }
    }
}
