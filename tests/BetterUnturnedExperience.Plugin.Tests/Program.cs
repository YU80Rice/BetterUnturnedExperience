using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.NoOpFixture;
using BetterUnturnedExperience.Plugin;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Plugin.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-red")
                {
                    AssertDev16DRealLogMustContainVisiblePreviewProjection(true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-diagnostics-red")
                {
                    AssertDev16DPreviewDiagnosticContainsCoordinateReadout();
                    return 0;
                }
                AssertSingleDllAssemblyClosure();
                AssertExternalSdkAssemblyIdentity();
                Assert(BootstrapGuard.Decide(false, false, true) == BootstrapDecision.Client, "client decision");
                Assert(BootstrapGuard.Decide(true, true, true) == BootstrapDecision.Headless, "headless decision");
                Assert(BootstrapGuard.Decide(false, true, true) == BootstrapDecision.Headless, "explicit headless decision");
                Assert(BootstrapGuard.Decide(false, false, false) == BootstrapDecision.Unavailable, "unavailable decision");
                BueRuntimeHost.Clear();
                var unavailable = NoOpFeatureRegistration.Register();
                Assert(unavailable.Reason == FeatureRegistrationReason.HostUnavailable, "external fixture fails closed before host initialization");
                var runtime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(runtime);
                runtime.OpenRegistration();
                var official = BetterItemInteractionFeatureRegistration.Register();
                Assert(official.Accepted && official.Feature.Value == "io.github.yu80rice.bue.better-item-interaction", "official feature registers through the same public host bridge");
                var accepted = new NoOpFeatureBootstrap().Awake();
                Assert(accepted.Accepted && accepted.Feature.Value == "io.github.yu80rice.bue.noop", "independent fixture registers through public host bridge");
                Assert(runtime.CompleteRuntime() && runtime.Catalog.Entries.Count == 2, "official and fixture reach runtime ready through host barrier");
                Assert(officialRegistrationHasClientUi(official), "official feature exposes a ClientUi satellite descriptor");
                AssertClientUiCompositionGates();
                AssertNativeUiGateReflectsMemberPresence();
                AssertPanelSurvivesComponentTeardown();
                AssertContainerSessionTrackerLifecycle();
                AssertInventoryLifecycleGateDecisions();
                AssertInventoryLifecycleWatcherDiffing();
                AssertSwapFootprintGuardMatrix();
                AssertDragPreviewAdapterActivatesStaticPump();
                AssertRuntimePumpBridge();
                AssertPluginUpdateDriverForwardsButtonInjection();
                AssertButtonInjectionRoutesAreLocallyIsolated();
                AssertRuntimeDriverDispatchesButtonInjectionSeam();
                AssertPanelDispatchReachesButtonInjectionSeam();
                AssertDragPreviewHasPluginOwnedUpdateDriver();
                AssertInventoryHeartbeatDrivesPreviewFallback();
                AssertNativeDragPivotConvertsToPositiveGrabOffset();
                AssertInventorySurfaceHasRuntimeScrollReader();
                AssertRuntimeCompletionBarrierIsolates();
                AssertManagementPanelConsumesRuntimeCatalog();
                AssertManagementPanelOpenHooks();
                Assert(RequiresParentRebindSemantics(), "management panel resets bindings when UI parent changes");
                Assert(runtime.Phase == FeatureRegistrationPhase.RuntimeReady, "runtime barrier enters RuntimeReady");
                Assert(runtime.Catalog.Entries[0].Definition.Feature.Value == "io.github.yu80rice.bue.better-item-interaction", "catalog order is deterministic by feature identity");
                var late = NoOpFeatureRegistration.Register();
                Assert(late.Reason == FeatureRegistrationReason.PhaseClosed, "fixture late registration is rejected");
                Console.WriteLine("DEV-14/DEV-16B plugin runtime tests: PASS"); return 0;
            }
            catch (Exception error) { Console.WriteLine("DEV-14 official registration parity tests: FAIL"); Console.WriteLine(error.ToString()); return 1; }
        }

        // DEV-16D diagnosis replay: hook and drag-start fire, but no preview
        // projection reaches the visual sink. Intentionally red until fixed.
        private static void AssertDev16DRealLogMustContainVisiblePreviewProjection(bool preFix = false)
        {
            var fixture = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", preFix ? "dev16d-r36-no-preview.log" : "dev16d-fixed-preview.log");
            var text = System.IO.File.ReadAllText(fixture);
            Assert(text.Contains("event=hooks-installed"), "diagnostic replay contains installed drag hook");
            Assert(text.Contains("event=drag-started"), "diagnostic replay contains drag start");
            Assert(text.Contains("event=preview-visible"), "drag start must reach a visible red/green preview projection");
        }

        // GPT watermark: red regression for the 2026-08-30 Hidden diagnosis.
        // The runtime log must expose every coordinate/value needed to explain
        // an OutsideGrid result instead of reporting only state=Hidden.
        private static void AssertDev16DPreviewDiagnosticContainsCoordinateReadout()
        {
            var fixture = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "dev16d-fixed-preview.log");
            var text = System.IO.File.ReadAllText(fixture);
            Assert(text.Contains("event=preview-input-readout"), "preview diagnostic readout event is present");
            Assert(text.Contains("pointerScreen="), "readout includes pointerScreen");
            Assert(text.Contains("uiScale="), "readout includes uiScale");
            Assert(text.Contains("uiCoordinates="), "readout includes converted UI coordinates");
            Assert(text.Contains("viewportOrigin="), "readout includes viewport origin");
            Assert(text.Contains("pointerGrid="), "readout includes pointerGrid");
            Assert(text.Contains("grabOffset="), "readout includes grabOffset");
            Assert(text.Contains("placementReason="), "readout includes PlacementReason");
        }

        private static void AssertDragPreviewHasPluginOwnedUpdateDriver()
        {
            var method = typeof(InventoryDragPreviewAdapter).GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert(method != null, "DEV-16D preview adapter exposes plugin-owned main-thread update driver");
        }

        private static void AssertInventoryHeartbeatDrivesPreviewFallback()
        {
            var method = typeof(InventorySurfaceLifecycleAdapter).GetMethod("PlayerUIUpdatePostfix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert(method != null, "DEV-16D exposes the observed PlayerUI.Update heartbeat seam");
        }

        private static void AssertNativeDragPivotConvertsToPositiveGrabOffset()
        {
            var method = typeof(InventoryDragPreviewAdapter).GetMethod("NativePivotToGrabOffset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert(method != null, "native drag pivot conversion seam exists");
            var result = (UnityEngine.Vector2)method.Invoke(null, new object[] { new UnityEngine.Vector2(-25f, -50f) });
            Assert(Math.Abs(result.x - 0.5f) < 0.001f && Math.Abs(result.y - 1f) < 0.001f,
                "negative native drag pivot becomes positive grid grab offset");
        }

        // GPT watermark: red regression for the remaining DEV-16D blocker.
        private static void AssertInventorySurfaceHasRuntimeScrollReader()
        {
            var type = typeof(UnturnedInventorySurfaceContext);
            var method = type.GetMethod("ReadScrollPixelsY", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert(method != null, "inventory surface must expose a runtime scroll reader");
            var horizontal = type.GetMethod("ReadScrollPixelsX", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert(horizontal != null, "inventory surface must expose an explicit horizontal scroll seam");
        }
        private static bool RequiresParentRebindSemantics()
        {
            var first = new object();
            var second = new object();
            return !BueNativeManagementPanel.RequiresParentRebind(first, first)
                && BueNativeManagementPanel.RequiresParentRebind(first, second)
                && !BueNativeManagementPanel.RequiresParentRebind(first, null);
        }

        private static void AssertRuntimePumpBridge()
        {
            var ticks = 0;
            var pump = new BueRuntimePump(() => ticks++);
            pump.Tick();
            Assert(ticks == 1, "runtime pump forwards one main-thread tick");
            pump.Clear();
            pump.Tick();
            Assert(ticks == 1, "cleared runtime pump does not call stale plugin state");

            var slot = new BueRuntimePumpSlot();
            var first = slot.GetOrCreate(() => { });
            var second = slot.GetOrCreate(() => { });
            Assert(object.ReferenceEquals(first, second), "runtime pump slot is idempotent");
            slot.Clear();
            var third = slot.GetOrCreate(() => { });
            Assert(!object.ReferenceEquals(first, third), "cleared runtime pump slot creates a fresh generation");
        }

        private static void AssertPluginUpdateDriverForwardsButtonInjection()
        {
            var calls = 0;
            var driver = new BuePluginUpdateDriver(() => calls++);
            driver.Update();
            driver.Update();
            Assert(calls == 2, "plugin Update fallback forwards every frame to button injection");
            driver.Clear();
            driver.Update();
            Assert(calls == 2, "cleared plugin Update fallback cannot call stale panel state");
        }

        private static void AssertButtonInjectionRoutesAreLocallyIsolated()
        {
            var dashboard = 0;
            var workshop = 0;
            var pause = 0;
            var failures = 0;
            var routes = new BueButtonInjectionCoordinator(
                () => { dashboard++; throw new InvalidOperationException("dashboard fixture failure"); },
                () => workshop++,
                () => pause++,
                (surface, error) => failures++);
            routes.Inject();
            Assert(dashboard == 1 && workshop == 1 && pause == 1, "one failed entry does not block other menu routes");
            Assert(failures == 1, "failed entry emits one local diagnostic");
        }

        private static void AssertRuntimeDriverDispatchesButtonInjectionSeam()
        {
            var frame = 10;
            var ticks = 0;
            var driver = new BueRuntimeTickDispatcher(source => ticks++, () => frame);
            Assert(driver.Dispatch(BueNativeManagementPanel.TickSource.Update), "runtime driver dispatches the button injection seam");
            Assert(ticks == 1, "button injection seam receives one runtime tick");
            Assert(!driver.Dispatch(BueNativeManagementPanel.TickSource.Update), "runtime driver de-duplicates the same source within one frame");
            Assert(driver.Dispatch(BueNativeManagementPanel.TickSource.Harmony), "runtime driver still permits a distinct source within one frame");
            frame++;
            Assert(driver.Dispatch(BueNativeManagementPanel.TickSource.Update), "runtime driver resets source de-duplication on the next frame");
            driver.Isolate();
            Assert(!driver.Dispatch(BueNativeManagementPanel.TickSource.RuntimePump), "isolated runtime driver rejects stale tick");
            Assert(ticks == 3, "isolated runtime driver does not call button injection seam");

            var failures = 0;
            BueRuntimeTickDispatcher throwing = null;
            throwing = new BueRuntimeTickDispatcher(source =>
            {
                failures++;
                Assert(!throwing.Dispatch(BueNativeManagementPanel.TickSource.Harmony), "reentrant button injection is rejected");
                throw new InvalidOperationException("synthetic button injection failure");
            }, () => 20);
            Assert(!throwing.Dispatch(BueNativeManagementPanel.TickSource.Update), "runtime driver rejects the failing first tick");
            Assert(failures == 1, "failing button injection seam is called once");
            Assert(!throwing.Dispatch(BueNativeManagementPanel.TickSource.Harmony), "runtime driver opens fail-closed barrier after first error");
            Assert(failures == 1, "fail-closed barrier blocks later harmony callbacks");
        }

        private static void AssertPanelDispatchReachesButtonInjectionSeam()
        {
            var composition = new BueClientUiCompositionRoot();
            var recording = new RecordingButtonInjectionSeam();
            var panel = new BueNativeManagementPanel(composition.ManagementPanel, null, recording);
            Assert(panel.Dispatch(BueNativeManagementPanel.TickSource.Update), "panel dispatch reaches the button injection seam");
            Assert(recording.Sources.Count == 1, "panel performs one injection pass for the update source");
            Assert(recording.Sources[0] == "Update", "panel forwards the source identity to injection");
            Assert(!panel.Dispatch(BueNativeManagementPanel.TickSource.Update), "panel rejects duplicate same-frame injection");
            panel.Destroy();

            var failures = 0;
            var failing = new BueNativeManagementPanel(composition.ManagementPanel, null,
                new ThrowingButtonInjectionSeam(() => failures++));
            Assert(!failing.Dispatch(BueNativeManagementPanel.TickSource.RuntimePump), "button injection failure is rejected by panel dispatch");
            Assert(failures == 1, "button injection failure enters the seam exactly once");
            Assert(failing.TickIsolated, "button injection failure isolates the panel");
            Assert(!failing.Dispatch(BueNativeManagementPanel.TickSource.Harmony), "isolated panel rejects later Harmony injection");
            failing.Destroy();
        }

        private static void AssertRuntimeCompletionBarrierIsolates()
        {
            var calls = 0;
            var failing = new BueRuntimeCompletionBarrier(() => { calls++; throw new InvalidOperationException("synthetic barrier failure"); });
            Assert(!failing.TryComplete(), "barrier rejects the throwing completion attempt");
            Assert(!failing.TryComplete(), "barrier does not retry after an exception");
            Assert(calls == 1, "barrier isolation prevents per-frame retry spam");
            Assert(failing.Isolated, "barrier reports isolated after a completion failure");
            Assert(failing.LastFailure is InvalidOperationException, "barrier preserves the isolated exception for diagnostics");

            var notifications = 0;
            var notifying = new BueRuntimeCompletionBarrier(() => { throw new InvalidOperationException("notify once"); }, error => notifications++);
            Assert(!notifying.TryComplete(), "notifying barrier rejects the throwing attempt");
            Assert(!notifying.TryComplete(), "notifying barrier does not retry after isolation");
            Assert(notifications == 1, "first-failure callback fires exactly once");
            Assert(notifying.LastFailure != null, "notifying barrier also preserves the failure");

            var attempts = 0;
            var notReady = new BueRuntimeCompletionBarrier(() => { attempts++; return false; });
            Assert(!notReady.TryComplete(), "not-ready barrier reports incomplete");
            Assert(!notReady.TryComplete(), "not-ready barrier may retry next frame");
            Assert(attempts == 2, "not-ready barrier keeps retrying without isolating");

            var done = 0;
            var ready = new BueRuntimeCompletionBarrier(() => { done++; return true; });
            Assert(ready.TryComplete() && done == 1, "ready barrier completes once");
            Assert(ready.TryComplete() && done == 1, "completed barrier is idempotent");
            Assert(!ready.Isolated, "successful completion does not isolate the barrier");
            Assert(ready.LastFailure == null, "successful completion leaves no failure behind");
        }

        private static void AssertNativeUiGateReflectsMemberPresence()
        {
            // The gate runs during plugin Awake, before the menu UI exists.
            // Glazier.instance stays null until the menu builds, so the gate
            // must reflect vanilla member presence only: engine readiness is
            // owned by the injection path's null guards, not by this gate.
            Assert(BueNativeManagementPanel.CanBindNativeUi(), "native ui gate stays true on vanilla member presence while Glazier is not yet initialized");
        }

        // [R19] The game destroys the BepInEx_Manager host mid-session; only
        // unpatching on real application quit keeps the panel drivable through
        // the vanilla MenuUI.Update postfix (R18 hit map: it ticks every frame).
        private static void AssertPanelSurvivesComponentTeardown()
        {
            var composition = new BueClientUiCompositionRoot();
            var panel = new BueNativeManagementPanel(composition.ManagementPanel, null);
            panel.Initialize();
            panel.Destroy(unpatchHarmony: false);
            Assert(HasOwner(Harmony.GetPatchInfo(AccessTools.Method(typeof(MenuWorkshopUI), "open")), "io.github.yu80rice.bue.management-panel"), "component teardown keeps the workshop-open patch");
            Assert(HasOwner(Harmony.GetPatchInfo(AccessTools.Method(typeof(MenuUI), "Update")), "io.github.yu80rice.bue.management-panel"), "component teardown keeps the MenuUI.Update frame-driver patch");
        }

        // [DEV-16C] Container session lifecycle state machine.
        private static void AssertContainerSessionTrackerLifecycle()
        {
            var tracker = new BetterUnturnedExperience.Plugin.ContainerSessionTracker();
            Assert(!tracker.HasActiveSession, "fresh tracker has no active session");
            Assert(!tracker.TryGetActiveGeneration(out _), "fresh tracker has no active generation");

            tracker.OnPlayerInventoryOpened();
            Assert(tracker.HasActiveSession, "player inventory open starts a session");
            Assert(tracker.Kind == ContainerSessionKind.PlayerInventory, "player session kind");
            Assert(tracker.TryGetActiveGeneration(out var playerGeneration), "player session has a generation");

            tracker.OnStorageOpened(isTrunk: false);
            Assert(tracker.Kind == ContainerSessionKind.Storage, "storage open switches kind");
            Assert(tracker.TryGetActiveGeneration(out var storageGeneration) && storageGeneration == playerGeneration + 1, "switching containers advances the generation exactly once");

            tracker.OnStorageOpened(isTrunk: true);
            Assert(tracker.Kind == ContainerSessionKind.Trunk, "trunk open switches kind");
            Assert(tracker.TryGetActiveGeneration(out var trunkGeneration) && trunkGeneration == storageGeneration + 1, "trunk switch advances the generation");

            tracker.OnContainerClosed();
            Assert(!tracker.HasActiveSession, "close ends the session");
            Assert(!tracker.TryGetActiveGeneration(out _), "closed session generations are stale for projections");

            tracker.OnStorageOpened(isTrunk: false);
            Assert(tracker.TryGetActiveGeneration(out var reopenGeneration) && reopenGeneration > trunkGeneration, "reopening after close starts a fresh generation");

            tracker.OnConnectionLost();
            Assert(!tracker.HasActiveSession, "connection loss invalidates the session");
            Assert(!tracker.TryGetActiveGeneration(out _), "connection loss invalidates old projections");
            tracker.OnStorageOpened(isTrunk: false);
            Assert(tracker.TryGetActiveGeneration(out var afterReconnect) && afterReconnect > reopenGeneration, "post-reconnect open advances the generation");

            tracker.OnStorageSwapped();
            Assert(tracker.TryGetActiveGeneration(out var afterSwap) && afterSwap == afterReconnect + 1, "storage page data swap advances the generation without changing kind");
            Assert(tracker.Kind == ContainerSessionKind.Storage, "swap keeps the storage kind");
        }

        // [DEV-16C] Probe gate: detection result -> decision + structured diagnostics.
        private static void AssertInventoryLifecycleGateDecisions()
        {
            var headless = BetterUnturnedExperience.Plugin.InventoryLifecycleGate.Evaluate(isClientBranch: false, probe: BetterUnturnedExperience.Plugin.InventoryLifecycleProbe.AllPresent());
            Assert(!headless.Enabled, "headless branch disables inventory hooks");
            Assert(headless.Diagnostics.Contains("headless"), "headless disable carries a structured reason");

            var missingProbe = BetterUnturnedExperience.Plugin.InventoryLifecycleProbe.MissingIsStoring();
            var gated = BetterUnturnedExperience.Plugin.InventoryLifecycleGate.Evaluate(isClientBranch: true, probe: missingProbe);
            Assert(!gated.Enabled, "missing probe targets disable the wiring");
            Assert(gated.Diagnostics.Contains("PlayerInventory.isStoring"), "diagnostics name the missing member");
            Assert(!gated.Diagnostics.Contains("PlayerDashboardInventoryUI.active"), "diagnostics only name the missing members");

            var full = BetterUnturnedExperience.Plugin.InventoryLifecycleGate.Evaluate(isClientBranch: true, probe: BetterUnturnedExperience.Plugin.InventoryLifecycleProbe.AllPresent());
            Assert(full.Enabled, "client branch with all members present enables the hooks");
            Assert(full.Diagnostics.Length == 0, "enabled gate carries no diagnostics");
        }

        // [DEV-16C] Watcher diffing: snapshot sequences raise the right
        // lifecycle events on the tracker.
        private static void AssertInventoryLifecycleWatcherDiffing()
        {
            var tracker = new BetterUnturnedExperience.Plugin.ContainerSessionTracker();
            var watcher = new BetterUnturnedExperience.Plugin.InventoryLifecycleWatcher(tracker);
            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(false, false, false, 0, true));
            Assert(!tracker.HasActiveSession, "idle snapshot starts no session");

            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(true, false, false, 0, true));
            Assert(tracker.HasActiveSession && tracker.Kind == ContainerSessionKind.PlayerInventory, "dashboard active raises player inventory open");

            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(true, true, false, 101, true));
            Assert(tracker.Kind == ContainerSessionKind.Storage, "storage open switches the session kind");
            Assert(tracker.TryGetActiveGeneration(out var storageGeneration), "storage session carries a generation");

            var storageIdentity = 101;
            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(true, true, false, storageIdentity, true));
            Assert(tracker.TryGetActiveGeneration(out storageGeneration), "identical storage snapshot is a no-op");

            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(true, true, false, 202, true));
            Assert(tracker.TryGetActiveGeneration(out var swappedGeneration) && swappedGeneration == storageGeneration + 1, "swapping containers advances the generation exactly once");

            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(true, true, true, 202, true));
            Assert(tracker.Kind == ContainerSessionKind.Trunk, "trunk snapshot switches kind");

            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(true, false, false, 0, true));
            Assert(tracker.Kind == ContainerSessionKind.PlayerInventory, "dashboard after storage opens the player session");
            Assert(tracker.TryGetActiveGeneration(out var playerAfterStorage) && playerAfterStorage > swappedGeneration, "storage-to-dashboard transition closes the old session before opening the new one");

            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(false, false, false, 0, true));
            Assert(!tracker.HasActiveSession, "closing the dashboard ends the session");

            watcher.Feed(new BetterUnturnedExperience.Plugin.InventoryLifecycleSnapshot(false, false, false, 0, false));
            Assert(!tracker.HasActiveSession, "disconnect snapshot keeps no session");
        }

        // [DEV-16C] Seam gap: the PlayerUI.Update IL detour compiles only on
        // the Mono game runtime (real-machine verified in R18); the polling
        // hook itself is therefore verified on the real machine, while this
        // host locks the gate, tracker and watcher semantics.
        // [DEV-16D] The static postfix reads ActiveAdapter; activation must
        // publish the adapter or the whole drag pipeline stays silent.
        private static void AssertDragPreviewAdapterActivatesStaticPump()
        {
            var composition = new BueClientUiCompositionRoot();
            Assert(composition.Initialize(false, false, true), "client composition initializes for the drag probe");
            var adapter = new BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter(null, composition.OfficialComponent);
            Assert(adapter.Enabled, "drag preview adapter enables with all native members present");
            adapter.Activate();
            // Environment-adaptive: on the Mono game runtime the hook installs
            // and the adapter publishes itself to the static pump; on a pure
            // .NET host the PlayerUI.Update IL detour fails to compile, so the
            // adapter fails closed with diagnostics (no silent breakage).
            if (adapter.HooksInstalled)
            {
                Assert(BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.ActiveAdapter == adapter, "activation publishes the adapter to the static pump");
            }
            else
            {
                Assert(adapter.GateDiagnostics.Contains("hooks-failed"), "IL failure is recorded as structured diagnostics instead of passing silently");
            }
        }

        // [DEV-16D] Swap guard footprint semantics: a swap onto a cell covered
        // by the dragged item's footprint is a native sendSwapItem operation,
        // not a BUE-cancelled placement.
        private static void AssertSwapFootprintGuardMatrix()
        {
            var occupied = new bool[3, 3];
            occupied[1, 1] = true;
            Assert(BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupied, 3, 3, 1, 1, 2, 2),
                "footprint origin covering the occupied cell counts as occupied");
            Assert(!BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupied, 3, 3, 2, 2, 2, 2),
                "footprint away from the occupied cell counts as empty");
            Assert(BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupied, 3, 3, 0, 0, 2, 2),
                "footprint touching the occupied cell at (1,1) counts as occupied");
            Assert(!BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupied, 3, 3, 0, 2, 2, 2),
                "footprint away from the occupied cell counts as empty");
            Assert(!BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupied, 3, 3, 3, 0, 2, 2),
                "out-of-bounds reads as empty");
        }

        private static void AssertManagementPanelConsumesRuntimeCatalog()
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            var composition = new BueClientUiCompositionRoot();
            composition.RefreshManagementPanel();
            var entries = composition.ManagementPanel.Model.GetEntries();
            Assert(entries.Count >= 2, "management model consumes registered runtime catalog entries");
            var hasNoOp = false;
            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index].StableId == "io.github.yu80rice.bue.noop") hasNoOp = true;
            }
            Assert(hasNoOp, "third-party BUE registration appears in management model");
            Assert(runtime != null, "runtime remains bound while catalog is projected");
            composition.Destroy();
        }

        private sealed class RecordingButtonInjectionSeam : IBueButtonInjectionSeam
        {
            internal readonly System.Collections.Generic.List<string> Sources = new System.Collections.Generic.List<string>();

            public void Inject(string source)
            {
                Sources.Add(source);
            }
        }

        private sealed class ThrowingButtonInjectionSeam : IBueButtonInjectionSeam
        {
            private readonly System.Action onAttempt;

            internal ThrowingButtonInjectionSeam(System.Action onAttempt)
            {
                this.onAttempt = onAttempt;
            }

            public void Inject(string source)
            {
                onAttempt();
                throw new InvalidOperationException("synthetic button injection failure");
            }
        }
        private static void AssertManagementPanelOpenHooks()
        {
            var composition = new BueClientUiCompositionRoot();
            var panel = new BueNativeManagementPanel(composition.ManagementPanel, null);
            try
            {
                panel.Initialize();
                var workshop = Harmony.GetPatchInfo(AccessTools.Method(typeof(MenuWorkshopUI), "open"));
                var pause = Harmony.GetPatchInfo(AccessTools.Method(typeof(PlayerPauseUI), "open"));
                var dashboard = Harmony.GetPatchInfo(AccessTools.Method(typeof(MenuDashboardUI), "open"));
                var menuUpdate = Harmony.GetPatchInfo(AccessTools.Method(typeof(MenuUI), "Update"));
                var playerUpdate = Harmony.GetPatchInfo(AccessTools.Method(typeof(PlayerUI), "Update"));
                Assert(HasOwner(workshop, "io.github.yu80rice.bue.management-panel"), "workshop open hook is installed");
                Assert(HasOwner(pause, "io.github.yu80rice.bue.management-panel"), "pause open hook is installed");
                Assert(HasOwner(dashboard, "io.github.yu80rice.bue.management-panel"), "dashboard open hook is installed");
                Assert(HasOwner(menuUpdate, "io.github.yu80rice.bue.management-panel"), "menu host update hook is installed");
                Assert(HasOwner(playerUpdate, "io.github.yu80rice.bue.management-panel"), "player host update hook is installed");
            }
            finally
            {
                panel.Destroy();
            }
        }
        private static bool HasOwner(HarmonyLib.Patches patches, string owner)
        {
            if (patches == null) return false;
            if (patches.Postfixes == null) return false;
            foreach (var patch in patches.Postfixes)
            {
                if (patch != null && patch.owner == owner) return true;
            }
            return false;
        }
        private static bool officialRegistrationHasClientUi(FeatureRegistrationResult result)
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Catalog == null) return false;
            for (var index = 0; index < runtime.Catalog.Entries.Count; index++)
            {
                var entry = runtime.Catalog.Entries[index];
                if (entry.Definition.Feature.Value == result.Feature.Value) return entry.ClientUi != null;
            }
            return false;
        }
        private static void AssertClientUiCompositionGates()
        {
            var client = new BueClientUiCompositionRoot();
            Assert(client.Initialize(false, false, true), "client composition root initializes on client");
            Assert(client.IsReady, "client composition root reaches ready");
            var firstFactoryCount = client.FactoryInvocationCount;
            Assert(client.Initialize(false, false, true), "repeated client initialization is idempotent");
            Assert(client.FactoryInvocationCount == firstFactoryCount, "repeated initialization does not recreate UI components");
            client.Destroy();
            Assert(!client.Initialize(false, false, true), "destroyed composition root is not reinitialized");

            var headless = new BueClientUiCompositionRoot();
            Assert(!headless.Initialize(true, true, true), "batch/headless gate blocks composition");
            Assert(headless.FactoryInvocationCount == 0, "headless gate never invokes UI factory");

            var unavailable = new BueClientUiCompositionRoot();
            Assert(!unavailable.Initialize(false, false, false), "native UI unavailable blocks composition");
            Assert(unavailable.FactoryInvocationCount == 0, "native UI unavailable never invokes UI factory");
        }
        private static void AssertSingleDllAssemblyClosure()
        {
            Assert(typeof(FeatureId).Assembly == typeof(BueRuntimeHost).Assembly, "public contract types must be embedded in the BUE runtime assembly");
            Assert(typeof(IFeatureRegistration).Assembly == typeof(BueRuntimeHost).Assembly, "registration ABI must resolve from the BUE runtime assembly");
            var references = typeof(BetterUnturnedExperiencePlugin).Assembly.GetReferencedAssemblies();
            foreach (var reference in references)
            {
                Assert(reference.Name != "BetterUnturnedExperience.Core", "BUE main assembly must not require private Core runtime DLL");
                Assert(reference.Name != "BetterUnturnedExperience.Contracts", "BUE main assembly must not require private Contracts runtime DLL");
            }
        }
        private static void AssertExternalSdkAssemblyIdentity()
        {
            var fixtureAssembly = typeof(BetterUnturnedExperience.NoOpFixture.NoOpFeaturePlugin).Assembly;
            var references = fixtureAssembly.GetReferencedAssemblies();
            var bueReference = false;
            foreach (var reference in references)
            {
                Assert(reference.Name != "BetterUnturnedExperience.Contracts", "external SDK output must not bind to Contracts runtime assembly");
                Assert(reference.Name != "BetterUnturnedExperience.Core", "external SDK output must not bind to private Core runtime assembly");
                if (reference.Name == "BetterUnturnedExperience") bueReference = true;
            }
            Assert(bueReference, "external SDK output must bind to the public BUE runtime assembly");
            Assert(typeof(FeatureId).Assembly == typeof(BueRuntimeHost).Assembly, "public ABI identity is resolved by BUE runtime assembly");
        }
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    }
}
