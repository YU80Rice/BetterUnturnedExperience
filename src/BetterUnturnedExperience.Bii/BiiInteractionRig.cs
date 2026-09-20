using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Bii
{
    /// <summary>
    /// DEV-V6-04: the injected host mouths for the BII drag-in subsystem. The
    /// pre-move adapters read the UI services bag (BueClientUiHostServices,
    /// 02E) which stayed with the UI project; these are the same mouths
    /// crossing the project boundary as named delegates. Unbound = honest
    /// swallow, the same family as the 02B/C/D/E absent-seam contract.
    /// </summary>
    internal sealed class BiiCompositionMouths
    {
        internal Action<string> LogRuntime { get; set; }
        internal Action<string> LogWarn { get; set; }
        internal Action<string> LogError { get; set; }
        internal Action<string> LogErrorFriendly { get; set; }
        internal Func<string, bool> IsCriticalNotReadyReason { get; set; }

        /// <summary>The headless gate (T3 Q5). Absent reads as headless — the
        /// dangerous direction is ARMING, so missing wiring must not arm.</summary>
        internal Func<bool> HeadlessDecision { get; set; }
    }

    /// <summary>
    /// DEV-V6-04: the UI-side composition seams the armed rig needs. The host
    /// binds them on the client branch only (the composition is ready there);
    /// absent = the rig never arms (no UI component, no inventory patches) —
    /// the T3 Q5 headless shape through seam absence instead of a flag.
    /// </summary>
    internal sealed class BiiInterfaceComposition
    {
        /// <summary>A dashboard surface opened (UI-side projection reconcile: the
        /// listen-host repair moment and the management-panel refresh hook).</summary>
        internal Action SurfaceOpened { get; set; }

        /// <summary>The dashboard surface closed.</summary>
        internal Action SurfaceClosed { get; set; }

        /// <summary>The settings single-source snapshot reader (the state object
        /// stays in the UI project; the component pulls at decision time).</summary>
        internal Func<FeatureSettingsSnapshot> SettingsSnapshot { get; set; }
    }

    /// <summary>
    /// DEV-V6-04: the armed rig for Better Item Interaction — the component and
    /// the two adapters (drag preview/commit, inventory surface lifecycle) the
    /// module's Start creates and Stop tears down (T3 Q2). The activation
    /// chain is the pre-move client-branch orchestration moved VERBATIM from
    /// BueClientUiCompositionRoot.InitializeAndActivate (the UI ring owns the
    /// panel; the BII owned everything between the composition gate and the
    /// wiring logs). Order is load-bearing: both inventory adapters are
    /// created before either activates, cleanup registration follows, the
    /// lifecycle heartbeat activates first, and the drag adapter activates
    /// only through the gated decision.
    /// Real Harmony activation is unreproducible in the host test process
    /// (SDG method bodies); the U3DS device run covers it (02D factory-arm
    /// named-seam precedent).
    /// </summary>
    internal sealed class BiiInteractionRig
    {
        private readonly BiiCompositionMouths mouths;
        private readonly BiiInterfaceComposition interfaceComposition;
        private readonly Func<IPlacementCandidateEvaluator> placementEvaluatorFactory;
        private readonly IFeaturePatching patching;
        // DEV-V6-05: whether the platform pocket accepted each patch set's
        // handle (true = the platform owns that set's orderly teardown; the
        // rig's Teardown then skips the adapter's duplicate unpatch).
        private bool dragTeardownDelegated;
        private bool lifecycleTeardownDelegated;

        /// <summary>DEV-V6-05: the drag patch set's teardown ownership (true =
        /// the pocket accepted its handle; the platform's account release at the
        /// stop/isolation boundary performs the unpatch).</summary>
        internal bool DragPatchTeardownDelegated { get { return dragTeardownDelegated; } }

        /// <summary>DEV-V6-05: the inventory-lifecycle patch set's teardown
        /// ownership (see DragPatchTeardownDelegated).</summary>
        internal bool LifecyclePatchTeardownDelegated { get { return lifecycleTeardownDelegated; } }

        internal BetterItemInteractionUiComponent Component { get; private set; }
        internal InventoryDragPreviewAdapter DragAdapter { get; private set; }
        internal InventorySurfaceLifecycleAdapter LifecycleAdapter { get; private set; }

        internal BiiInteractionRig(BiiCompositionMouths mouths, BiiInterfaceComposition interfaceComposition,
            Func<IPlacementCandidateEvaluator> placementEvaluatorFactory, IFeaturePatching patching = null)
        {
            this.mouths = mouths ?? new BiiCompositionMouths();
            this.interfaceComposition = interfaceComposition;
            this.placementEvaluatorFactory = placementEvaluatorFactory;
            this.patching = patching;
        }

        /// <summary>
        /// The activation chain, verbatim from the pre-move
        /// InitializeAndActivate body (minus the retired registry projection:
        /// the component IS the only registry feature component, so the
        /// surface projection now reaches it directly).
        /// </summary>
        internal void Arm()
        {
            // The evaluator's concrete implementation stays host-side (Core);
            // it crosses as its contract interface — same shape as the
            // pre-move composition constructor parameter.
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(
                    placementEvaluatorFactory != null ? placementEvaluatorFactory() : null)),
                new NativeInventoryInteractionAdapter(2, 8),
                interfaceComposition != null ? interfaceComposition.SettingsSnapshot : null,
                mouths.LogRuntime,
                mouths.LogError);
            Component = component;
            // Pre-move the composition registry called OnUiInitialized while
            // initializing (the 1-arg overload = satellite available, not
            // headless); the rig only arms on the non-headless bound path, so
            // the same facts hold here. Order: component first, adapters after.
            component.OnUiInitialized(true, false);

            DragAdapter = new InventoryDragPreviewAdapter(component, mouths);
            LifecycleAdapter = new InventorySurfaceLifecycleAdapter(
                surface =>
                {
                    // Pre-move: composition.OpenInventory(surface) routed the
                    // surface into the registry (whose ONLY component was this
                    // one) + the listen-host reconcile + the grid attach. The
                    // registry is retired with its sole consumer.
                    component.OnInventoryOpened(surface);
                    // F-B1c: a dashboard open on the listen host is the
                    // join-time repair moment — the UI-side half crosses as
                    // the injected seam (absent = no-op, unbound family).
                    interfaceComposition?.SurfaceOpened?.Invoke();
                    // [DEV-16D] Rebind the grid's placed-item delegate on each
                    // fresh session dispatch so a rebuilt UI gets BUE's
                    // decision wrapper.
                    DragAdapter?.AttachGrid(surface);
                },
                () =>
                {
                    DragAdapter?.DetachGrid();
                    component.OnInventoryClosed();
                },
                () => Component == null || Component.IsolatePreviewFailureResult(),
                () =>
                {
                    Component?.HidePreview();
                    DragAdapter?.DetachGrid();
                    component.OnInventoryClosed();
                },
                page =>
                {
                    if (DragAdapter != null)
                        DragAdapter.DetachGridAndDiscardSurface(page);
                    else
                        Component?.DiscardInventorySurface(page);
                }, mouths);
            Component.RegisterCleanupResult(() => LifecycleAdapter.IsolateAndDetach());
            Component.RegisterCleanupResult(() => DragAdapter.IsolateAndDetach(false));

            // DEV-V6-05 (V6-T5 Q2/Q4): BOTH Harmony patch sets enter the
            // platform pocket for this generation BEFORE either adapter can
            // install anything — the account is the single fact source of what
            // patches exist, so a failed activation, a thrown wiring line or a
            // forgotten teardown can no longer leave a hook behind. Accepted
            // registration TRANSFERS the orderly-teardown ownership: the
            // platform's stop/isolation release performs the unpatch, and the
            // rig's own Teardown then skips the adapter's duplicate (the drift
            // paths keep their immediate fail-closed unpatch). Absent pocket
            // (an old host, a bare test bootstrap) = no registration and the
            // pre-move self-managed shape.
            dragTeardownDelegated = RegisterPatchSet(DragAdapter.HarmonyId, DragAdapter.PatchHandle(mouths.LogRuntime));
            lifecycleTeardownDelegated = RegisterPatchSet(LifecycleAdapter.HarmonyId, LifecycleAdapter.PatchHandle(mouths.LogRuntime));

            LifecycleAdapter.Activate();
            if (LifecycleAdapter.HooksInstalled)
                EmitRuntime("BUE inventory lifecycle wiring enabled diagnosticId=BUE-INVENTORY-001");
            else
                EmitWarn("BUE inventory lifecycle wiring disabled diagnosticId=BUE-INVENTORY-003 diagnostics=" + LifecycleAdapter.GateDiagnostics);
            // [DEV-16D] Drag preview/commit adapter driven by the same
            // PlayerUI.Update tick; it is activated only after the lifecycle
            // heartbeat has installed successfully.
            if (ShouldActivateDragPreview(LifecycleAdapter.HooksInstalled, LifecycleAdapter.Isolated))
            {
                DragAdapter.Activate();
            }
            else
            {
                DragAdapter.IsolateAndDetach(false);
                EmitWarn("BUE drag preview wiring disabled because inventory lifecycle is unavailable diagnosticId=BUE-DRAG-003");
            }
            Component.ProjectionSink = new LoggingInventoryProjectionSink(mouths.LogRuntime);
            if (DragAdapter.HooksInstalled)
                EmitRuntime("BUE drag preview wiring enabled diagnosticId=BUE-DRAG-001");
            else
                EmitWarn("BUE drag preview wiring disabled diagnosticId=BUE-DRAG-003 diagnostics=" + DragAdapter.GateDiagnostics);
        }

        /// <summary>Module Stop: isolate both adapters and destroy the component
        /// (runtime stop = cleanup chain, registration order as before).
        /// DEV-V6-05 (V6-T5 Q2/Q4): a patch set whose handle the pocket accepted
        /// is torn down by the PLATFORM's account release at the stop boundary,
        /// so the module skips its own duplicate here (releasePatches=false) —
        /// 平台先经补丁口拆除; a patch set with no accepted registration keeps
        /// the pre-move fail-closed self-unpatch.
        /// ORDER IS LOAD-BEARING (Spec 轴 R2 收口): the lifecycle adapter is
        /// isolated FIRST. The drag adapter's isolation runs the component's
        /// cleanup chain, whose callbacks call IsolateAndDetach with the
        /// fail-closed default — if either adapter were still live at that
        /// point it would self-unpatch and defeat the ownership transfer. Both
        /// are already isolated by then, so those cascades are no-ops; the
        /// drift-triggered cascades (which really do need an immediate unpatch)
        /// never see a pre-isolated pair and keep their fail-closed default.</summary>
        internal void Teardown()
        {
            if (LifecycleAdapter != null) LifecycleAdapter.IsolateAndDetach(releasePatches: !lifecycleTeardownDelegated);
            if (DragAdapter != null) DragAdapter.IsolateAndDetach(true, releasePatches: !dragTeardownDelegated);
            if (Component != null) Component.OnUiDestroyed();
            DragAdapter = null;
            LifecycleAdapter = null;
            Component = null;
        }

        /// <summary>The per-frame drag tick (the host Update driver enters the
        /// public assembly seam; the pump order is unchanged).</summary>
        internal void TickDragPreview()
        {
            DragAdapter?.Tick();
        }

        /// <summary>DEV-V6-05: one patch set enters the platform pocket for this
        /// generation. Returns true when the platform now owns that set's
        /// orderly teardown. A rejection (stopped/isolated feature, stale
        /// generation, duplicate, capacity) is reported and the adapter keeps
        /// managing that patch itself — the platform's teardown guarantee
        /// covers exactly the handles that entered the account (契约诚实:
        /// 未经此口登记的不保证拆).</summary>
        private bool RegisterPatchSet(string harmonyId, IDisposable handle)
        {
            if (patching == null) return false;
            FeaturePatchRegistrationResult result;
            try { result = patching.Register(handle); }
            catch (Exception error)
            {
                EmitWarn("BUE patch pocket registration faulted diagnosticId=BUE-BII-004 harmonyId=" + harmonyId
                    + " errorType=" + error.GetType().Name);
                return false;
            }
            if (result.Registered)
            {
                EmitRuntime("BUE patch pocket registered harmonyId=" + harmonyId
                    + " generation=" + result.LifecycleGeneration + " diagnosticId=BUE-BII-003");
                return true;
            }
            EmitWarn("BUE patch pocket registration rejected harmonyId=" + harmonyId
                + " reason=" + result.Reason + " diagnosticId=" + result.DiagnosticId);
            return false;
        }

        // DEV-16D R5 (pre-move home: the plugin entry class; moved with the
        // adapters it governs): the drag adapter depends on the inventory
        // lifecycle heartbeat. Keep the activation decision at one testable
        // seam so a failed lifecycle hook can never leave a dependent Harmony
        // hook live.
        internal static bool ShouldActivateDragPreview(bool lifecycleHooksInstalled, bool lifecycleIsolated)
        {
            return lifecycleHooksInstalled && !lifecycleIsolated;
        }

        private void EmitRuntime(string line)
        {
            var sink = mouths != null ? mouths.LogRuntime : null;
            if (sink != null) sink(line);
        }

        private void EmitWarn(string line)
        {
            var sink = mouths != null ? mouths.LogWarn : null;
            if (sink != null) sink(line);
        }
    }
}
