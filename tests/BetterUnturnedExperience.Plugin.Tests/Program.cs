using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.NoOpFixture;
using BetterUnturnedExperience.Plugin;
using BetterUnturnedExperience.ClientUi.Internal;
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
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r44-red")
                {
                    AssertDev16DR44SymptomsReproduce();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r9-red")
                {
                    AssertDev16DR9CleanupPropagationContracts();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r10-red")
                {
                    AssertGridContentPointerDoesNotDoubleApplyScroll();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r11-red")
                {
                    AssertDragTickDefersFrameCommitUntilSurfaceReady();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-red")
                {
                    AssertDev16DR13CanonicalOccupancyUsesItemJarFootprints();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-boundary-red")
                {
                    AssertDev16DR13RejectsOutOfBoundsFootprints();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-surface-red")
                {
                    AssertDev16DR13TracksBothSupportedSurfaces();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-rotation-red")
                {
                    AssertDev16DR13RotationKeepsSourceExclusion();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-stale-red")
                {
                    AssertDev16DR13StalePreviewFallsThrough();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-page-red")
                {
                    AssertDev16DR13SinglePageRebuildPreservesOtherSurface();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-page-seam-red")
                {
                    AssertDev16DR13NonCurrentPageRebuildUsesLiveDispatchSeam();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-native-delegate-red")
                {
                    AssertDev16DR13NativeDelegateLifecycleIsReversible();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-passthrough-red")
                {
                    AssertDev16DR13UnsupportedSourcePassThrough();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-silence-red")
                {
                    AssertDev16DR13SilenceTraceSeams();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-scrollsize-red")
                {
                    AssertDev16DR13ScrollViewportSizeSeam();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-symrot-red")
                {
                    AssertDev16DR13SymmetricAutoRotation();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-symrot-wide-red")
                {
                    AssertDev16DR13SymmetricAutoRotationWideContainer();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-edge-rot-red")
                {
                    AssertDev16DR13EdgeAutoRotation();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-corner-lift-red")
                {
                    AssertDev16DR13CornerLift();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16d-r13-rotgrab-red")
                {
                    AssertDev16DR13RotatedGrabOffsetCandidate();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16f-source-decouple-red")
                {
                    AssertDev16FSourceDecoupleReachesCandidateSeam();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16f-target-vest-red")
                {
                    AssertDev16FTargetVestEnhancedPreview();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16f-area-source-red")
                {
                    AssertDev16FAreaSourceReachesCandidateSeam();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--dev16f-equip-source-red")
                {
                    AssertDev16FEquipSlotSourceReachesCandidateSeam();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--logging-gate-red")
                {
                    AssertLoggingSurfaceReadinessGate();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--logging-failure-red")
                {
                    AssertLoggingFailureEmission();
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
                AssertSurfaceViewportDegradation();
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
                AssertNativeLikeViewportScaleAndHierarchyBehavior();
                AssertLiveGridScrollContract();
                AssertStrictNativeGeometryRejectsInvalidValues();
                AssertDev16DR5ActivationAndCleanupContracts();
                AssertLiveGridPointerReachesCandidateSeam();
                AssertGridContentPointerDoesNotDoubleApplyScroll();
                AssertDragTickDefersFrameCommitUntilSurfaceReady();
                AssertDev16DR44SymptomsReproduce();
                AssertDynamicViewportTracksCurrentScroll();
                AssertPreviewReadFailureRoutesThroughIsolation();
                AssertNativeCallbackBoundariesAreGuarded();
                AssertDev16DR3IsolationAndGeometryContracts();
                AssertDev16DR4RebindAndFailureProjectionContracts();
                AssertDev16DR9CleanupPropagationContracts();
                AssertDev16DR13CanonicalOccupancyUsesItemJarFootprints();
                AssertDev16DR13RejectsOutOfBoundsFootprints();
                AssertDev16DR13TracksBothSupportedSurfaces();
                AssertDev16DR13PointerRoutesToLiveTargetSurface();
                AssertDev16DR13RotationKeepsSourceExclusion();
                AssertDev16DR13StalePreviewFallsThrough();
                AssertDev16DR13SinglePageRebuildPreservesOtherSurface();
                AssertDev16DR13NonCurrentPageRebuildUsesLiveDispatchSeam();
                AssertDev16DR13NativeDelegateLifecycleIsReversible();
                AssertDev16FSourceDecoupleReachesCandidateSeam();
                AssertDev16FTargetVestEnhancedPreview();
                AssertDev16FAreaSourceReachesCandidateSeam();
                AssertDev16FEquipSlotSourceReachesCandidateSeam();
                AssertLoggingSurfaceReadinessGate();
                AssertLoggingFailureEmission();
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

        // GPT watermark: red regression for the R44 real-machine symptoms.
        // The live viewport must be the native scroll viewport (not the full
        // content grid), and a floating icon must use native drag top-left
        // anchoring rather than a center offset. Both assertions are derived
        // from the U3-SDK PlayerDashboardInventoryUI drag path.
        private static void AssertDev16DR44SymptomsReproduce()
        {
            var viewport = UnturnedInventorySurfaceContext.ResolveViewport(
                true, new UnityEngine.Vector2(320f, 180f), 5, 7, 0f, 0f, 250f, 350f);
            Assert(Math.Abs(viewport.ClipWidth - 320f) < 0.001f && Math.Abs(viewport.ClipHeight - 180f) < 0.001f,
                "R44 regression: live preview clip must match the native scroll viewport");

            var input = new InventoryPreviewInput(1, new ItemGridPosition(3, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 1), 100f, 200f,
                new InventoryGridViewport(0f, 0f, 5, 7, 0f, 0f, 320f, 180f),
                50f, 1f, 0f, 0f, 2, 3, 0, true, 0.5f, 1.25f,
                ItemAssetIdentity.FromItemId(363), 0.25f, 0.5f, -25f, -62.5f,
                new IGridOccupancyViewForTest(5, 7));
            PreviewIcon icon;
            Assert(InventoryGridCoordinateAdapter.TryGetNativeIconPlacement(input, 0, out icon),
                "R44 regression: native floating icon placement can be computed");
            Assert(Math.Abs(icon.PositionScaleX - 0.25f) < 0.001f && Math.Abs(icon.PositionScaleY - 0.5f) < 0.001f,
                "R44 regression: floating icon uses the native top-level pointer anchor");
            Assert(Math.Abs(icon.PositionOffsetX + 25f) < 0.001f && Math.Abs(icon.PositionOffsetY + 62.5f) < 0.001f,
                "R44 regression: floating icon follows native cursor-to-top-left grab offset");
            Assert(Math.Abs(icon.Width - 100f) < 0.001f && Math.Abs(icon.Height - 150f) < 0.001f,
                "R44 regression: floating icon carries the rotated footprint size");
        }

        // GPT watermark: DEV-16D-R13 red regression. U3-SDK Items.items is a
        // compact list, while each ItemJar carries its authoritative origin,
        // dimensions, and rotation. Occupancy must expand those footprints;
        // treating y * width + x as a list index reports empty cells as full
        // and misses sparse or rotated items.
        private static void AssertDev16DR13CanonicalOccupancyUsesItemJarFootprints()
        {
            var items = new Items(7);
            // Avoid invoking Items.loadSize in the host-only regression fixture:
            // the SDK helper touches PlayerInventory's NetReflection static
            // initializer, which is unavailable outside the game process. The
            // width/height fields are the same native inputs the adapter reads.
            typeof(Items).GetField("_width", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(items, (byte)6);
            typeof(Items).GetField("_height", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(items, (byte)4);
            var jar = CreateTestItemJar(3, 1, 1, 2, 3);
            var obstacle = CreateTestItemJar(0, 0, 0, 1, 1);
            items.items.Add(jar);
            items.items.Add(obstacle);

            var occupancy = new UnturnedGridOccupancyView(items);
            Assert(occupancy.IsOccupied(3, 1), "R13 occupancy includes rotated ItemJar top-left");
            Assert(occupancy.IsOccupied(5, 2), "R13 occupancy includes the far cell of a rotated footprint");
            Assert(occupancy.IsOccupied(0, 0), "R13 occupancy includes an ItemJar that is sparse in list order");
            Assert(!occupancy.IsOccupied(2, 3), "R13 occupancy does not leak beyond the ItemJar footprint");

            NativeItemGridOccupancySnapshot initial;
            NativeItemGridOccupancySnapshot withoutSource;
            Assert(NativeItemGridOccupancySnapshot.TryCreateFromItems(items, null, out initial),
                "R13 builds an immutable occupancy snapshot from native items");
            Assert(NativeItemGridOccupancySnapshot.TryCreateFromItems(items, jar, out withoutSource),
                "R13 can build a source-excluded occupancy snapshot");
            Assert(!withoutSource.IsOccupied(3, 1) && !withoutSource.IsOccupied(5, 2),
                "R13 excludes every cell of the dragged source footprint");
            Assert(withoutSource.IsOccupied(0, 0),
                "R13 source exclusion does not remove another ItemJar");
            Assert(!object.ReferenceEquals(initial, withoutSource),
                "R13 source exclusion publishes a replacement immutable snapshot");

            NativeItemGridOccupancySnapshot crossContainer;
            Assert(NativeItemGridOccupancySnapshot.TryCreateFromItems(items, null, out crossContainer) &&
                crossContainer.IsOccupied(3, 1),
                "R13 cross-container occupancy keeps the target container source cells occupied");

            Assert(!occupancy.RebuildForDrag(new ContainerReference(ContainerKind.PlayerInventory, 7, 1),
                    new ContainerReference(ContainerKind.PlayerInventory, 7, 1),
                    new ItemGridPosition(7, 3, 1, 1), jar, ItemAssetIdentity.FromItemId(1), 2, 3, 1),
                "R13 rejects source exclusion when the asset fingerprint is incomplete");
        }

        // GPT watermark: an ItemJar footprint that cannot be represented by the
        // native grid is stale or malformed. The adapter must reject the whole
        // snapshot so the caller can preserve native pass-through; clipping it
        // would silently turn an invalid inventory state into a false vacancy.
        private static void AssertDev16DR13RejectsOutOfBoundsFootprints()
        {
            var items = new Items(7);
            typeof(Items).GetField("_width", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(items, (byte)4);
            typeof(Items).GetField("_height", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(items, (byte)4);
            items.items.Add(CreateTestItemJar(3, 3, 0, 2, 1));

            NativeItemGridOccupancySnapshot snapshot;
            Assert(!NativeItemGridOccupancySnapshot.TryCreateFromItems(items, null, out snapshot),
                "R13 rejects an ItemJar footprint that extends outside the native grid");
            Assert(snapshot == null, "R13 does not publish a partial snapshot for an invalid footprint");
        }

        // GPT watermark: R13 red regression. Both supported SleekItems pages
        // are live at the same time. Registering the Storage page must not
        // discard the Backpack surface needed by a cross-page drag source.
        private static void AssertDev16DR13TracksBothSupportedSurfaces()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());
            var backpack = CreateTestSurface(ContainerKind.PlayerInventory, 3, 901);
            var storage = CreateTestSurface(ContainerKind.Storage, 7, 901);
            component.OnInventoryOpened(backpack);
            component.OnInventoryOpened(storage);
            component.OnDragStarted(90, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));

            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(90, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "cross-page source can still create a preview input after both surfaces are registered");
            Assert(input.TargetContainer.Page == 3,
                "source-page routing keeps the Backpack live surface instead of the last Storage registration");
            component.OnInventoryClosed();
        }

        // GPT watermark: R13 red regression. The source page may be Backpack
        // while the cursor is over Storage; polling must select the live target
        // surface by native pointer hit instead of reusing the source surface.
        private static void AssertDev16DR13PointerRoutesToLiveTargetSurface()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());
            var backpack = new TestSurfaceContext(new ContainerReference(ContainerKind.PlayerInventory, 3, 903),
                new TestVisualContainer(), new TestVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, new EmptyGridForTest(8, 6), false, 100f, 100f);
            var storage = new TestSurfaceContext(new ContainerReference(ContainerKind.Storage, 7, 903),
                new TestVisualContainer(), new TestVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, new EmptyGridForTest(8, 6), true, 200f, 200f);
            component.OnInventoryOpened(backpack);
            component.OnInventoryOpened(storage);
            component.OnDragStarted(92, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));

            IInventorySurfaceContext selected;
            float localX;
            float localY;
            Assert(InventoryDragPreviewAdapter.TrySelectTargetSurface(component,
                    out selected, out localX, out localY),
                "pointer routing finds a live inventory target surface");
            Assert(selected.CurrentContainer.Page == 7 && localX == 200f && localY == 200f,
                "pointer routing selects Storage while the source drag remains on Backpack");
            component.OnInventoryClosed();
        }

        // GPT watermark: R13 red regression. Native rotation mutates dragJar.rot
        // during updateDraggedItem, while dragFromRot remains the original
        // source orientation. Source exclusion must remain valid after that
        // mutation so the original footprint is not treated as a blocker.
        private static void AssertDev16DR13RotationKeepsSourceExclusion()
        {
            var jar = CreateTestItemJar(1, 1, 2, 2, 1);
            var source = new ItemGridPosition(3, 1, 1, 0);
            Assert(UnturnedGridOccupancyView.ResolveSourceRotation(source, jar.rot) == source.Rotation,
                "R13 source occupancy uses frozen dragFromRot instead of mutable ItemJar.rot");
            Assert(UnturnedGridOccupancyView.SourceExclusionMetadataMatches(
                    jar, source,
                    ItemAssetIdentity.FromItemId(1), 2, 1, 0, true, true),
                "rotated native drag jar remains eligible for exclusion using frozen source rotation");
        }

        // GPT watermark: R13 red regression. Once the canonical occupancy
        // snapshot becomes unavailable, a previously visible Candidate must
        // never be submitted on release.
        private static void AssertDev16DR13StalePreviewFallsThrough()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());
            var surface = CreateTestSurface(ContainerKind.PlayerInventory, 3, 902);
            component.OnInventoryOpened(surface);
            component.OnDragStarted(91, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));
            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(91, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "initial occupancy snapshot is available");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "initial preview is a Candidate before occupancy invalidation");
            surface.InvalidateOccupancy();
            component.InvalidateOccupancySnapshot();
            Assert(component.LastPreview.State == PlacementPreviewState.Hidden,
                "occupancy invalidation clears the old preview before any next pointer update");
            Assert(!component.TryCreatePreviewInput(91, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "invalidated occupancy rejects the next preview input");
            Assert(component.LastPreview.State == 0,
                "occupancy rejection clears the old preview instead of retaining Candidate");
            var native = new RecordingNativeDragActions();
            var outcome = component.OnDragReleased(
                new NativeDragAdapterInput(true, 91, new ItemGridPosition(3, 0, 0, 0), component.LastPreview), native);
            Assert(outcome == NativeDragAdapterOutcome.PassThrough && native.SendCount == 0,
                "stale preview release remains native pass-through and cannot submit");
            component.OnInventoryClosed();
        }

        // GPT watermark: DEV-16D-R13 red regression. Rebuilding one native
        // page must not clear the other live page from the dispatch table.
        private static void AssertDev16DR13SinglePageRebuildPreservesOtherSurface()
        {
            var surfaces = new Dictionary<byte, object>
            {
                { 3, new object() },
                { 7, new object() }
            };
            Assert(InventorySurfaceLifecycleAdapter.RemoveDispatchedSurfaceForPage(surfaces, 3),
                "rebuilding a dispatched page removes that page");
            Assert(!surfaces.ContainsKey(3) && surfaces.ContainsKey(7),
                "rebuilding one page preserves the other live surface");
        }

        // GPT watermark: DEV-16D-R13 review regression. The production
        // lifecycle dispatch seam must invalidate a non-current source page
        // while a Storage target is active, clear the published preview and
        // preserve native pass-through on the next release.
        private static void AssertDev16DR13NonCurrentPageRebuildUsesLiveDispatchSeam()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());

            var detachedPages = new List<byte>();
            var lifecycle = new InventorySurfaceLifecycleAdapter(null,
                surface => { },
                () => { },
                null,
                null,
                page =>
                {
                    detachedPages.Add(page);
                    Assert(component.DiscardInventorySurface(page),
                        "live dispatch callback reaches the component page discard seam");
                });

            var backpack = CreateTestSurface(ContainerKind.PlayerInventory, 3, 904);
            var storage = CreateTestSurface(ContainerKind.Storage, 7, 904);
            component.OnInventoryOpened(backpack);
            component.OnInventoryOpened(storage);
            component.OnDragStarted(904, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));
            Assert(component.TrySelectSurfaceForPage(7),
                "Storage becomes the active target while Backpack remains the drag source");

            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(904, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "live target surface creates the preview input before source rebuild");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "live target publishes a Candidate before source rebuild");
            Assert(component.PreviewSink != null && component.PreviewSink.IsFrameVisible,
                "live target preview is visible before source rebuild");
            Assert(component.CurrentDragGeneration == 904 && component.DragOriginContainer.Page == 3,
                "source generation and origin remain bound while Storage is the target");
            Assert(component.HasActiveDragOccupancy,
                "the drag-scoped occupancy snapshot is live before source rebuild");

            lifecycle.RememberDispatchedSurface(3,
                new InventorySurfaceLifecycleAdapter.DispatchedSurfaceState(904, null, null, null, null));
            lifecycle.RememberDispatchedSurface(7,
                new InventorySurfaceLifecycleAdapter.DispatchedSurfaceState(904, null, null, null, null));
            Assert(lifecycle.DiscardDispatchedSurfaceForPage(3, "native-surface-rebuilt"),
                "production dispatch seam discards the rebuilt non-current source page");
            Assert(detachedPages.Count == 1 && detachedPages[0] == 3,
                "only the rebuilt source page reaches detach/rebind callback");
            Assert(component.LiveSurfaceCount == 1 && component.CurrentContainer.Page == 7,
                "surviving Storage target remains live and active");
            Assert(!component.EnhancedDragActive && component.DragSourcePassThrough,
                "source rebuild ends the enhanced drag and restores native source routing");
            Assert(component.LastPreview.State == PlacementPreviewState.Hidden,
                "source rebuild clears the published Candidate before release");
            Assert(component.PreviewSink != null && !component.PreviewSink.IsFrameVisible,
                "source rebuild hides the target visual sink");
            Assert(component.CurrentDragGeneration == 0 && !component.HasActiveDragOccupancy,
                "source rebuild clears generation and occupancy state before release");

            var native = new RecordingNativeDragActions();
            var outcome = component.OnDragReleased(new NativeDragAdapterInput(true, 904,
                new ItemGridPosition(3, 0, 0, 0), component.LastPreview, 7), native);
            Assert(outcome == NativeDragAdapterOutcome.PassThrough && native.SendCount == 0,
                "release after source rebuild remains native pass-through");
            Assert(!lifecycle.DiscardDispatchedSurfaceForPage(3, "duplicate-rebuild"),
                "discarding the source page twice is idempotent");
            Assert(lifecycle.DiscardDispatchedSurfaceForPage(7, "session-closed"),
                "surviving target page can be discarded independently");
            component.OnInventoryClosed();
        }

        // GPT watermark: R13-6 red regression. The production page-discard
        // callback must exercise the real InventoryDragPreviewAdapter native
        // delegate seam: detach only the rebuilt page, restore its exact
        // original delegate, leave the other supported page wrapped, and make
        // the component's source/generation state pass through natively.
        private static void AssertDev16DR13NativeDelegateLifecycleIsReversible()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());

            var nativeBackpackCalls = 0;
            var nativeStorageCalls = 0;
            PlacedItem originalBackpack = (page, x, y) => nativeBackpackCalls++;
            PlacedItem originalStorage = (page, x, y) => nativeStorageCalls++;
            var backpackGrid = CreateNativeSleekItems(3, originalBackpack);
            var storageGrid = CreateNativeSleekItems(7, originalStorage);
            var backpack = CreateNativeSurface(ContainerKind.PlayerInventory, 3, 905, backpackGrid);
            var storage = CreateNativeSurface(ContainerKind.Storage, 7, 905, storageGrid);
            component.OnInventoryOpened(backpack);
            component.OnInventoryOpened(storage);

            var adapter = new InventoryDragPreviewAdapter(null, component);
            Assert(adapter.AttachNativeGrid(backpackGrid, 3),
                "native Backpack grid can be attached through the production seam");
            Assert(adapter.AttachNativeGrid(storageGrid, 7),
                "native Storage grid can be attached through the production seam");
            var backpackWrapper = backpackGrid.onPlacedItem;
            var storageWrapper = storageGrid.onPlacedItem;
            Assert(adapter.AttachedGridCount == 2,
                "both native page delegates are live before a page rebuild");
            Assert(!ReferenceEquals(backpackWrapper, originalBackpack) &&
                !ReferenceEquals(storageWrapper, originalStorage),
                "production seam installs wrappers without losing native delegates");

            component.OnDragStarted(905, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));
            Assert(component.TrySelectSurfaceForPage(7),
                "Storage is selected as the live target while Backpack remains the source");

            var lifecycle = new InventorySurfaceLifecycleAdapter(null,
                surface => { },
                () => { },
                null,
                null,
                page =>
                {
                    Assert(adapter.DetachGridAndDiscardSurface(page),
                        "production callback detaches the native delegate before discarding the page");
                });
            lifecycle.RememberDispatchedSurface(3,
                new InventorySurfaceLifecycleAdapter.DispatchedSurfaceState(905, backpackGrid, null, null, null));
            lifecycle.RememberDispatchedSurface(7,
                new InventorySurfaceLifecycleAdapter.DispatchedSurfaceState(905, storageGrid, null, null, null));

            Assert(lifecycle.DiscardDispatchedSurfaceForPage(3, "native-surface-rebuilt"),
                "non-current Backpack rebuild reaches the production delegate seam");
            Assert(ReferenceEquals(backpackGrid.onPlacedItem, originalBackpack),
                "Backpack rebuild restores the exact original native delegate");
            Assert(ReferenceEquals(storageGrid.onPlacedItem, storageWrapper),
                "Backpack detach leaves the live Storage wrapper untouched");
            Assert(adapter.AttachedGridCount == 1,
                "only the rebuilt page is detached from the native adapter");
            Assert(component.CurrentContainer.Page == 7 && component.LiveSurfaceCount == 1,
                "the surviving Storage surface remains live after source-page rebuild");
            Assert(component.CurrentDragGeneration == 0 && component.DragOriginContainer.Page == 0 &&
                component.DragSourcePassThrough,
                "page rebuild clears drag origin/generation and restores native pass-through");

            Assert(adapter.DetachGrid(3),
                "repeated detach of an already detached page is idempotent");
            Assert(nativeBackpackCalls == 0 && nativeStorageCalls == 0,
                "detaching does not invoke native placement callbacks");

            var rebuiltBackpackNativeCalls = 0;
            PlacedItem rebuiltBackpackOriginal = (page, x, y) => rebuiltBackpackNativeCalls++;
            var rebuiltBackpackGrid = CreateNativeSleekItems(3, rebuiltBackpackOriginal);
            var rebuiltBackpack = CreateNativeSurface(ContainerKind.PlayerInventory, 3, 906, rebuiltBackpackGrid);
            component.OnInventoryOpened(rebuiltBackpack);
            Assert(adapter.AttachNativeGrid(rebuiltBackpackGrid, 3),
                "rebuilt Backpack can be rebound after the original detach");
            var rebuiltWrapper = rebuiltBackpackGrid.onPlacedItem;
            Assert(!ReferenceEquals(rebuiltWrapper, rebuiltBackpackOriginal),
                "rebuilt Backpack receives a fresh BUE wrapper");
            Assert(adapter.DetachGrid(3),
                "rebuilt Backpack detaches cleanly on the second lifecycle edge");
            Assert(ReferenceEquals(rebuiltBackpackGrid.onPlacedItem, rebuiltBackpackOriginal),
                "rebuilt Backpack restores its own exact original delegate");
            Assert(rebuiltBackpackNativeCalls == 0,
                "native delegate remains untouched until the game invokes it");
            component.OnInventoryClosed();
        }

        private static void AssertDev16DR13UnsupportedSourcePassThrough()
        {
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var native = new RecordingNativeDragActions();
            var preview = new ItemPlacementPreview(700, PlacementPreviewState.Candidate,
                new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None);
            // DEV-16F R2: page 8 (AREA) is now a valid enhanced source. A
            // malformed source page (9, beyond AREA) remains the first-gate
            // pass-through case.
            var outcome = adapter.HandleRelease(new NativeDragAdapterInput(true, 701,
                new ItemGridPosition(9, 0, 0, 0), preview), native);
            Assert(outcome == NativeDragAdapterOutcome.PassThrough,
                "unsupported stale source is native pass-through before generation validation");
            Assert(native.SendCount == 0 && native.StopCount == 0 && native.GroundTakeCount == 0,
                "unsupported stale source never invokes enhanced native actions");
        }

        // GPT watermark: R13-R6-silence red regression. The production silent-return
        // points in the inventory lifecycle Poll and the drag Tick must expose a
        // discriminable reason so a real-machine session can tell which gate blocked
        // the feature (no active session vs lifecycle gate vs hierarchy-not-ready).
        // These seams are referenced before they exist: the compile must go red
        // (CS1061) until the instrumentation lands in the adapters.
        private static void AssertDev16DR13SilenceTraceSeams()
        {
            var noSession = InventorySurfaceLifecycleAdapter.DescribeNoActiveSession(
                dashboardActive: false, isStoring: false, isStorageTrunk: false,
                connected: false, hasActiveSession: false);
            Assert(!string.IsNullOrEmpty(noSession) && noSession.IndexOf("no-active-session", StringComparison.Ordinal) >= 0,
                "lifecycle Poll names the no-active-session silent return");

            var gate = InventoryDragPreviewAdapter.DescribeTickGate(
                lifecycleCanRun: false, enhancedDragActive: false, state: FeatureState.Running);
            Assert(!string.IsNullOrEmpty(gate) && gate.IndexOf("lifecycle-gate", StringComparison.Ordinal) >= 0,
                "drag Tick names the lifecycle gate silent return");

            var adapterGate = InventoryDragPreviewAdapter.DescribeAdapterGate(
                enabled: false, isolated: false);
            Assert(!string.IsNullOrEmpty(adapterGate) && adapterGate.IndexOf("adapter-gate", StringComparison.Ordinal) >= 0,
                "drag Tick names the adapter (enabled/isolated) gate silent return");

            var lifecycleAdapterGate = InventorySurfaceLifecycleAdapter.DescribeAdapterGate(
                hasActiveAdapter: false, isolated: false);
            Assert(!string.IsNullOrEmpty(lifecycleAdapterGate) && lifecycleAdapterGate.IndexOf("adapter-gate", StringComparison.Ordinal) >= 0,
                "lifecycle postfix names the adapter (null/isolated) gate silent return");
        }

        // GPT watermark: R13-R6-scrollsize red regression. The real-machine log
        // surfaced `poll-exception ... native inventory scroll viewport size is
        // invalid` (H5): on the first frame after the dashboard opens, the native
        // horizontalScrollView has not been laid out yet and GetAbsoluteSize()
        // returns 0/NaN, so BuildSurfaceContext threw and the fail-closed guard
        // isolated the whole feature. The fix must expose a pure seam
        // (IsValidScrollViewportSize) and route the invalid-layout case to the
        // existing not-ready retry instead of throwing. Referenced before it
        // exists so the compile goes red (CS0117) until the seam lands.
        private static void AssertDev16DR13ScrollViewportSizeSeam()
        {
            Assert(!UnturnedInventorySurfaceContext.IsValidScrollViewportSize(default(UnityEngine.Vector2)),
                "zero scroll viewport size is rejected as not-ready");
            Assert(!UnturnedInventorySurfaceContext.IsValidScrollViewportSize(new UnityEngine.Vector2(float.NaN, 300f)),
                "NaN scroll viewport width is rejected as not-ready");
            Assert(UnturnedInventorySurfaceContext.IsValidScrollViewportSize(new UnityEngine.Vector2(400f, 300f)),
                "finite positive scroll viewport size is valid for dispatch");
        }

        private static void AssertDev16DR13SymmetricAutoRotation()
        {
            // User's real-machine repro (2026-09-01, backpack page): a katana
            // (1 wide x 3 tall) dragged to the bottom row auto-rotates to
            // horizontal and is placed. Re-grabbing that horizontal katana and
            // dragging it to a vertical slot must auto-rotate BACK to vertical
            // (symmetric auto-rotation). The frozen Local-Fit Priority ladder
            // must not trap the item in the horizontal orientation once the
            // current orientation fails to fit locally.
            //
            // 3x3 grid with a 2x2 item at the top-right:
            //   X O O
            //   X O O
            //   X X X
            // X = free, O = occupied by the 2x2. The only vertical slot is
            // column 0; the only horizontal slot is row 2.
            var occupancy = new IGridOccupancyViewForTest(3, 3, new System.ValueTuple<byte, byte>[]
            {
                (1, 0), (2, 0), (1, 1), (2, 1)
            });
            var evaluator = new BetterUnturnedExperience.Core.Placement.PlacementCandidateEvaluator();

            // Vertical katana (rot 0) dragged to the bottom row -> auto-rotate
            // to horizontal (rot 1) at row 2. Cursor at (1.5, 2.4).
            var verticalInput = new PlacementCandidateInput(1,
                new ItemGridPosition(3, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 1),
                1.5f, 2.4f, 1, 3, 0, true, occupancy);
            var verticalResult = evaluator.Evaluate(verticalInput);
            Assert(verticalResult.State == PlacementPreviewState.Candidate, "vertical katana at bottom row stays a candidate");
            Assert(verticalResult.Candidate.Rotation == 1, "vertical katana at bottom row auto-rotates to horizontal");

            // Horizontal katana (rot 1, just re-grabbed) dragged back to column
            // 0 (vertical slot). Cursor at (0.4, 1.5).
            var horizontalInput = new PlacementCandidateInput(2,
                new ItemGridPosition(3, 0, 2, 1),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 2),
                0.4f, 1.5f, 1, 3, 1, true, occupancy);
            var horizontalResult = evaluator.Evaluate(horizontalInput);
            Assert(horizontalResult.State == PlacementPreviewState.Candidate, "horizontal katana at vertical slot stays a candidate");
            Assert(horizontalResult.Width == 1 && horizontalResult.Height == 3,
                "horizontal katana auto-rotates BACK to a vertical footprint when the horizontal footprint cannot fit");
            Assert(horizontalResult.Candidate.Rotation == 2 || horizontalResult.Candidate.Rotation == 0,
                "horizontal katana returns a vertical rotation when dragged to the vertical slot");
        }

        // GPT watermark: R13-symrot red regression. The user's real container is
        // the trunk 6x3 (or backpack 5x7), not the 3x3 illustrative grid. In a
        // wide container a horizontal 1x3 katana fits almost everywhere, so the
        // frozen Local-Fit Priority ladder step 1 ("current orientation fits
        // locally -> return immediately, never check rotation") traps the item
        // in horizontal forever. The frozen decision (ADR-0003, D2) is that a
        // wide container's open middle keeps the current orientation; the
        // auto-rotate-back fix belongs to the empty-area edge rule (edge-rot),
        // not to symmetric rotation in open space. This test pins the D2 guard:
        // a horizontal katana in the open middle of a wide container stays
        // horizontal (no wobble source introduced).
        private static void AssertDev16DR13SymmetricAutoRotationWideContainer()
        {
            // Trunk 6x3, completely empty (no obstacle-carved edge near the
            // cursor). A horizontal 1x3 katana at the open middle must keep
            // horizontal per D2.
            var occupancy = new IGridOccupancyViewForTest(6, 3);
            var evaluator = new BetterUnturnedExperience.Core.Placement.PlacementCandidateEvaluator();

            // Horizontal katana (rot 1, re-grabbed) in the open middle.
            // Cursor at (2.6, 1.5) — not near any empty-area edge.
            var horizontalInput = new PlacementCandidateInput(2,
                new ItemGridPosition(7, 0, 2, 1),
                new ContainerReference(ContainerKind.Storage, 7, 2),
                2.6f, 1.5f, 1, 3, 1, true, occupancy);
            var horizontalResult = evaluator.Evaluate(horizontalInput);
            Assert(horizontalResult.State == PlacementPreviewState.Candidate,
                "D2 guard: horizontal katana in the open middle of a wide container stays a candidate");
            Assert(horizontalResult.Width == 3 && horizontalResult.Height == 1,
                "D2 guard: horizontal katana in the open middle of a wide container keeps horizontal");
            Assert(horizontalResult.Candidate.Rotation == 1,
                "D2 guard: horizontal katana in the open middle returns the horizontal rotation");
        }

        // GPT watermark: R13-edge-rot red regression. ADR-0003 (方案 A, 已确认):
        // when the cursor is at an empty-area edge (the outermost column/row of
        // the free region — container border or obstacle-carved boundary), the
        // auto-rotation must flip the current orientation so the LONG side hugs
        // the edge, even though the current orientation (horizontal) still fits.
        // The user's real machine repro (backpack 5x7 / trunk 6x3): a horizontal
        // 1x3 katana dragged back toward the left column stays horizontal because
        // step 1 of Local-Fit returns it immediately; edge-rot must rotate it
        // vertical so the long side hugs the left edge.
        private static void AssertDev16DR13EdgeAutoRotation()
        {
            var evaluator = new BetterUnturnedExperience.Core.Placement.PlacementCandidateEvaluator();
            var occupancy = new IGridOccupancyViewForTest(6, 3);

            // Horizontal katana (rot 1, re-grabbed) dragged to the left edge
            // column (x=0). Cursor at (0.4, 1.5).
            var leftInput = new PlacementCandidateInput(2,
                new ItemGridPosition(7, 0, 2, 1),
                new ContainerReference(ContainerKind.Storage, 7, 2),
                0.4f, 1.5f, 1, 3, 1, true, occupancy);
            var leftResult = evaluator.Evaluate(leftInput);
            Assert(leftResult.State == PlacementPreviewState.Candidate,
                "edge-rot: horizontal katana at the left edge stays a candidate");
            Assert(leftResult.Width == 1 && leftResult.Height == 3,
                "edge-rot: horizontal katana at the left edge auto-rotates to a vertical footprint (long side hugs the left edge)");
            Assert(leftResult.Candidate.Rotation == 2 || leftResult.Candidate.Rotation == 0,
                "edge-rot: horizontal katana at the left edge returns a vertical rotation");

            // Symmetry: the right edge column (x=5). Cursor at (5.6, 1.5).
            var rightInput = new PlacementCandidateInput(2,
                new ItemGridPosition(7, 0, 2, 1),
                new ContainerReference(ContainerKind.Storage, 7, 2),
                5.6f, 1.5f, 1, 3, 1, true, occupancy);
            var rightResult = evaluator.Evaluate(rightInput);
            Assert(rightResult.State == PlacementPreviewState.Candidate,
                "edge-rot: horizontal katana at the right edge stays a candidate");
            Assert(rightResult.Width == 1 && rightResult.Height == 3,
                "edge-rot: horizontal katana at the right edge auto-rotates to a vertical footprint (long side hugs the right edge)");
            Assert(rightResult.Candidate.Rotation == 2 || rightResult.Candidate.Rotation == 0,
                "edge-rot: horizontal katana at the right edge returns a vertical rotation");

            // Edge-sensing band (spec §11): the trigger is cursor-grid based,
            // band(dim)=clamp(1.0, dim*0.15, 2.0). On a 6x3 the band is 1.0, so a
            // cursor one cell inside the left/right wall (0.9 / 5.1) is still in
            // the vertical band and must auto-rotate vertical (not only x==0).
            var innerLeft = new PlacementCandidateInput(2,
                new ItemGridPosition(7, 0, 2, 1),
                new ContainerReference(ContainerKind.Storage, 7, 2),
                0.9f, 1.5f, 1, 3, 1, true, occupancy);
            var innerLeftResult = evaluator.Evaluate(innerLeft);
            Assert(innerLeftResult.State == PlacementPreviewState.Candidate,
                "edge-rot band: cursor one cell inside the left wall stays a candidate");
            Assert(innerLeftResult.Width == 1 && innerLeftResult.Height == 3,
                "edge-rot band: cursor one cell inside the left wall auto-rotates vertical (long side hugs the left edge)");

            var innerRight = new PlacementCandidateInput(2,
                new ItemGridPosition(7, 0, 2, 1),
                new ContainerReference(ContainerKind.Storage, 7, 2),
                5.1f, 1.5f, 1, 3, 1, true, occupancy);
            var innerRightResult = evaluator.Evaluate(innerRight);
            Assert(innerRightResult.State == PlacementPreviewState.Candidate,
                "edge-rot band: cursor one cell inside the right wall stays a candidate");
            Assert(innerRightResult.Width == 1 && innerRightResult.Height == 3,
                "edge-rot band: cursor one cell inside the right wall auto-rotates vertical (long side hugs the right edge)");
        }

        // GPT watermark: R13-corner-lift red regression. ADR-0003 (Rev 2026-09-02)
        // + spec §11: a horizontal 1x3 katana in the bottom-left corner (overlap
        // of vertical and horizontal sensing bands) keeps its entering horizontal
        // posture (anti-jitter). Lifting the cursor up (leaving the bottom band,
        // entering the left band) must flip it vertical hugging the left wall;
        // pulling it back down to the pure bottom band must flip it horizontal
        // hugging the bottom. "往上一提立起，往下一拉躺平".
        private static void AssertDev16DR13CornerLift()
        {
            var evaluator = new BetterUnturnedExperience.Core.Placement.PlacementCandidateEvaluator();
            var occupancy = new IGridOccupancyViewForTest(6, 3);
            var source = new ItemGridPosition(7, 0, 2, 1);
            var target = new ContainerReference(ContainerKind.Storage, 7, 2);

            // Bottom-left corner (0.4, 2.5): inside both the left vertical band
            // (0.4 < 1.0) and the bottom horizontal band (2.5 >= 2.0). Overlap ->
            // keep entering horizontal posture (no jitter).
            var corner = evaluator.Evaluate(new PlacementCandidateInput(1, source, target,
                0.4f, 2.5f, 1, 3, 1, true, occupancy));
            Assert(corner.State == PlacementPreviewState.Candidate,
                "corner-lift: bottom-left corner stays a candidate");
            Assert(corner.Width == 3 && corner.Height == 1 && corner.Candidate.Rotation == 1,
                "corner-lift: bottom-left corner overlap keeps the entering horizontal posture (anti-jitter)");

            // Lift up to (0.4, 1.5): still in the left band, no longer in the
            // bottom band -> vertical band gravity flips to vertical hugging the
            // left wall.
            var lifted = evaluator.Evaluate(new PlacementCandidateInput(1, source, target,
                0.4f, 1.5f, 1, 3, 1, true, occupancy));
            Assert(lifted.State == PlacementPreviewState.Candidate,
                "corner-lift: lifted cursor stays a candidate");
            Assert(lifted.Width == 1 && lifted.Height == 3 &&
                (lifted.Candidate.Rotation == 2 || lifted.Candidate.Rotation == 0),
                "corner-lift: lifting out of the bottom band flips to vertical hugging the left wall");

            // Pull down to the pure bottom band (1.5, 2.5): no longer in the left
            // band, still in the bottom band -> horizontal band gravity flips back
            // to horizontal hugging the bottom.
            var pulled = evaluator.Evaluate(new PlacementCandidateInput(1, source, target,
                1.5f, 2.5f, 1, 3, 1, true, occupancy));
            Assert(pulled.State == PlacementPreviewState.Candidate,
                "corner-lift: pulled cursor stays a candidate");
            Assert(pulled.Width == 3 && pulled.Height == 1 && pulled.Candidate.Rotation == 1,
                "corner-lift: pulling back to the pure bottom band flips to horizontal hugging the bottom");

            // Spec §11 distinguishing case (the real red anchor): on a LARGE
            // container (13x13) the vertical band is band(13)=clamp(1.0, 1.95, 2.0)
            // = 1.95 cells. A cursor at x=1.5 is inside the left vertical band, so
            // the vertical band gravity must rotate the horizontal katana to
            // vertical hugging the LEFT WALL (x=0) — even though the raw vertical
            // projection would land at x=1 (not flush). The old footprint-based
            // LongSideHugsEdge does NOT fire here (x=1 is not a wall), so this
            // case must be RED on the current implementation.
            var large = new IGridOccupancyViewForTest(13, 13);
            var bandCursor = evaluator.Evaluate(new PlacementCandidateInput(1, source,
                new ContainerReference(ContainerKind.Storage, 7, 2),
                1.5f, 6.5f, 1, 3, 1, true, large));
            Assert(bandCursor.State == PlacementPreviewState.Candidate,
                "edge band: cursor inside the left band of a large container stays a candidate");
            Assert(bandCursor.Width == 1 && bandCursor.Height == 3 &&
                (bandCursor.Candidate.Rotation == 2 || bandCursor.Candidate.Rotation == 0),
                "edge band: cursor inside the left band rotates to vertical hugging the left wall");
            Assert(bandCursor.Candidate.X == 0,
                "edge band: vertical candidate is positioned hugging the left wall (x=0)");
        }

        // GPT watermark: R13-rotgrab red regression. Spec §2 defines
        // grabOffsetInFootprint in the CURRENT rotation coordinate space, and
        // the adapter reads the native dragPivot (already current-rot). But
        // TryCreateCandidateInput re-rotates that grab offset as if it were the
        // base (rot0) footprint, so a re-grabbed HORIZONTAL katana (rot=1, grab
        // offset like 1.48,0.2 in 3x1 space) fails the baseWidth bound
        // (1.48 > 1) -> TryCreateCandidateInput returns false -> presenter keeps
        // HidePreview -> no enhanced render. Vertical (rot=0) coincides with
        // base so it passes. This red test drives the real-machine symptom: a
        // horizontal katana's grab offset must reach the candidate seam.
        private static void AssertDev16DR13RotatedGrabOffsetCandidate()
        {
            var occupancy = new IGridOccupancyViewForTest(5, 7);
            var input = new InventoryPreviewInput(9, new ItemGridPosition(3, 4, 6, 1),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 9),
                175f, 150f,
                new InventoryGridViewport(0f, 0f, 5, 7, 0f, 0f, 250f, 350f),
                50f, 1f, 0f, 0f,
                1, 3, 1, true, 1.48f, 0.2f,
                ItemAssetIdentity.FromItemId(363), 0.25f, 0.5f, -74f, -10f,
                occupancy);
            PlacementCandidateInput candidate;
            Assert(InventoryGridCoordinateAdapter.TryCreateCandidateInput(input, out candidate),
                "rotated grab offset: horizontal katana (rot=1) grab offset reaches the candidate seam");
            Assert(!float.IsNaN(candidate.CursorGridX) && !float.IsNaN(candidate.CursorGridY) &&
                candidate.CursorGridX >= 0f && candidate.CursorGridY >= 0f,
                "rotated grab offset: candidate center is finite and inside the grid");
        }

        // GPT watermark: DEV-16F slice A red regression. The user's real
        // repro (2026-09-02): pick an item up from the Shirt page and carry it
        // into the Backpack — the enhanced preview must appear. The current
        // source gate (`dragSourcePassThrough = !IsSupportedEnhancedPage(source.Page)`)
        // treats any source outside {3,7} as pass-through, so a SHIRT(5) source
        // immediately aborts the enhanced drag and keeps the preview Hidden.
        // Source decoupling means the source page must NOT decide takeover:
        // the pickup enters the BUE drag flow and the TARGET grid decides
        // whether the preview is enhanced. RED until the source gate is
        // decoupled from the source page.
        private static void AssertDev16FSourceDecoupleReachesCandidateSeam()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());
            var backpack = CreateTestSurface(ContainerKind.PlayerInventory, 3, 1101);
            component.OnInventoryOpened(backpack);

            // Drag starts from SHIRT (5) — a grid source page that the current
            // source gate treats as pass-through.
            component.OnDragStarted(1101, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(5, 0, 0, 0));
            Assert(!component.DragSourcePassThrough,
                "source decouple: SHIRT(5) source must not force native pass-through");
            Assert(component.EnhancedDragActive,
                "source decouple: SHIRT(5) source enters the enhanced drag flow");

            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(1101, new ItemGridPosition(5, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "source decouple: SHIRT(5) source can create a preview input toward BACKPACK(3)");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "source decouple: SHIRT(5) source reaches the candidate seam on BACKPACK(3)");
            component.OnInventoryClosed();
        }

        // GPT watermark: DEV-16F slice B red regression. VEST(4) is a grid
        // page like Backpack/Storage but the attach gate is hardcoded to
        // {3,7}, so its onPlacedItem delegate is never wrapped and no preview
        // can be produced over the vest grid. RED until SupportedPages /
        // IsSupportedEnhancedPage / SupportedSurfacePages / IsOrdinaryGrid
        // extend to the full grid set {2,3,4,5,6,7}.
        private static void AssertDev16FTargetVestEnhancedPreview()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());
            var vest = CreateTestSurface(ContainerKind.PlayerInventory, 4, 1102);
            component.OnInventoryOpened(vest);
            Assert(component.TryGetLiveSurface(4, out _),
                "target vest: VEST(4) registers as a live enhanced target surface");

            var vestGrid = CreateNativeSleekItems(4, (page, x, y) => { });
            var adapter = new InventoryDragPreviewAdapter(null, component);
            Assert(adapter.AttachNativeGrid(vestGrid, 4),
                "target vest: native VEST(4) grid attaches through the production seam");
            Assert(adapter.DetachGrid(4),
                "target vest: VEST(4) grid detaches cleanly");

            // A BACKPACK source dragging over VEST must reach the candidate seam.
            var backpack = CreateTestSurface(ContainerKind.PlayerInventory, 3, 1102);
            component.OnInventoryOpened(backpack);
            component.OnDragStarted(1102, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));
            Assert(component.TrySelectSurfaceForPage(4),
                "target vest: VEST(4) is selectable as the live target surface");
            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(1102, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "target vest: preview input targets the VEST(4) grid");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate &&
                component.LastPreview.Candidate.Page == 4,
                "target vest: VEST(4) target publishes an enhanced candidate preview");
            component.OnInventoryClosed();
        }

        // GPT watermark: DEV-16F R2 red regression. Real-machine feedback
        // (2026-09-02): picking up from the GROUND ("附近的物品", AREA=8) and
        // dragging into a supported grid does NOT trigger enhanced preview or
        // auto-rotation. Root cause: OnDragStarted sets
        // `dragSourcePassThrough = !IsSupportedEnhancedPage(source.Page)`, so a
        // source page of 8 (AREA) is treated as pass-through and BUE never
        // takes over. Slice A's "any page pickup enters the enhanced flow"
        // must include AREA as a source: the TARGET grid decides rendering.
        // RED until the source gate no longer excludes AREA(8).
        private static void AssertDev16FAreaSourceReachesCandidateSeam()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());
            var backpack = CreateTestSurface(ContainerKind.PlayerInventory, 3, 1103);
            component.OnInventoryOpened(backpack);

            // Drag starts from the ground (AREA=8) toward BACKPACK(3).
            component.OnDragStarted(1103, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(8, 0, 0, 0));
            Assert(!component.DragSourcePassThrough,
                "area source: ground pickup (AREA=8) must not force native pass-through");
            Assert(component.EnhancedDragActive,
                "area source: ground pickup (AREA=8) enters the enhanced drag flow");

            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(1103, new ItemGridPosition(8, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "area source: ground pickup creates a preview input toward BACKPACK(3)");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "area source: ground pickup reaches the candidate seam on BACKPACK(3)");
            component.OnInventoryClosed();
        }

        // GPT watermark: DEV-16F R2 red regression. Real-machine feedback
        // (2026-09-02): picking up from the HOTBAR equipment slots ("手持的物
        // 品（快捷键1、2栏位）", pages 0/1 Primary/Secondary) and dragging into
        // a supported grid does NOT trigger enhanced preview or auto-rotation.
        // Root cause is the same source gate: `IsSupportedEnhancedPage(0)` is
        // false, so OnDragStarted aborts. Slice A source decoupling must let
        // equipment-slot pickups enter the enhanced flow too; the TARGET grid
        // decides rendering. RED until the source gate no longer excludes
        // equipment slots (0/1).
        private static void AssertDev16FEquipSlotSourceReachesCandidateSeam()
        {
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());
            var backpack = CreateTestSurface(ContainerKind.PlayerInventory, 3, 1104);
            component.OnInventoryOpened(backpack);

            // Drag starts from the Primary equipment slot (page 0).
            component.OnDragStarted(1104, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(0, 0, 0, 0));
            Assert(!component.DragSourcePassThrough,
                "equip source: hotbar slot (page 0) pickup must not force native pass-through");
            Assert(component.EnhancedDragActive,
                "equip source: hotbar slot (page 0) pickup enters the enhanced drag flow");

            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(1104, new ItemGridPosition(0, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "equip source: hotbar slot pickup creates a preview input toward BACKPACK(3)");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "equip source: hotbar slot pickup reaches the candidate seam on BACKPACK(3)");
            component.OnInventoryClosed();
        }

        // GPT watermark: DEV-16G slice A red regression. surface-not-ready was
        // emitted EVERY frame for every 0x0 page (97.3% of all BUE log lines —
        // ~13,768 lines in a single real session). The fix introduces a
        // per-page readiness state tracker: a page that stays in the same
        // state must be SILENT; only a state TRANSITION emits one line
        // (not-ready -> ready logs "loaded grid=WxH", ready -> not-ready logs
        // "failed reason=<reason>"). This red test drives the seam that does
        // not exist yet (compile goes red until the gate lands).
        private static void AssertLoggingSurfaceReadinessGate()
        {
            var gate = new InventorySurfaceLifecycleAdapter.SurfaceReadinessGate();

            string line1;
            string line2;
            string line3;
            string line4;

            // Page 4 starts not-ready. First observation of an unchanged
            // not-ready state must be silent (no flood on every frame).
            Assert(!gate.Observe(4, false, "scroll-viewport-not-laid-out", out line1),
                "readiness gate: first not-ready frame is silent (no per-frame flood)");

            // Same page, same not-ready state, next frame — still silent.
            Assert(!gate.Observe(4, false, "scroll-viewport-not-laid-out", out line2),
                "readiness gate: repeated not-ready frames stay silent");

            // Page becomes ready — transition fires a "loaded" line once.
            Assert(gate.Observe(4, true, null, out line3),
                "readiness gate: not-ready -> ready transition emits one line");
            Assert(line3 != null && line3.IndexOf("page=4") >= 0 && line3.IndexOf("ready") >= 0,
                "readiness gate: ready transition names the page and ready state");

            // Page goes back to not-ready — transition fires a "failed, reason" line.
            Assert(gate.Observe(4, false, "native-hierarchy-incomplete", out line4),
                "readiness gate: ready -> not-ready transition emits one line");
            Assert(line4 != null && line4.IndexOf("page=4") >= 0 && line4.IndexOf("native-hierarchy-incomplete") >= 0,
                "readiness gate: failed transition carries the not-ready reason");

            // Pages are independent: page 5 not-ready is silent even though page 4 just fired.
            string line5;
            Assert(!gate.Observe(5, false, "scroll-viewport-not-laid-out", out line5),
                "readiness gate: per-page state is independent (page 5 silent)");

            // Page 4 stays not-ready after its transition — silent again.
            string line6;
            Assert(!gate.Observe(4, false, "native-hierarchy-incomplete", out line6),
                "readiness gate: post-transition not-ready frames are silent");
        }

        // GPT watermark: DEV-16G slice B red regression. The rich failure
        // reasons (LastPollDiagnostics / LastCleanupDiagnostics / Describe*)
        // are built but NEVER emitted in production — a mid-session isolation
        // leaves no one-shot "xxx failed, reason: yyy" line. The fix routes
        // these through a static emission seam (EmitDiagnosticOnce) so a
        // real failure is logged exactly once. This red test drives the seam
        // that does not exist yet.
        private static void AssertLoggingFailureEmission()
        {
            var emitted = new System.Collections.Generic.List<string>();
            var previous = InventoryDragPreviewAdapter.DiagnosticLogSink;
            InventoryDragPreviewAdapter.DiagnosticLogSink = line => emitted.Add(line);
            try
            {
                // Simulate a real failure path: cleanup incomplete must emit
                // exactly one one-shot diagnostic line carrying the reason.
                InventoryDragPreviewAdapter.ReportCleanupIncomplete("placed-item");
                Assert(emitted.Count == 1,
                    "failure emission: cleanup incomplete emits exactly one one-shot line");
                Assert(emitted[0].IndexOf("CleanupIncomplete") >= 0 &&
                    emitted[0].IndexOf("placed-item") >= 0,
                    "failure emission: emitted line carries the cleanup reason");
            }
            finally
            {
                InventoryDragPreviewAdapter.DiagnosticLogSink = previous;
            }

            var gateEmitted = new System.Collections.Generic.List<string>();
            var previousGate = InventorySurfaceLifecycleAdapter.DiagnosticLogSink;
            InventorySurfaceLifecycleAdapter.DiagnosticLogSink = line => gateEmitted.Add(line);
            try
            {
                // Surface no-active-session reason must be reachable as a
                // one-shot emitted line (the lifecycle silent-return reason).
                var reason = InventorySurfaceLifecycleAdapter.DescribeNoActiveSession(
                    dashboardActive: false, isStoring: false, isStorageTrunk: false,
                    connected: false, hasActiveSession: false);
                InventorySurfaceLifecycleAdapter.EmitDiagnosticOnce(reason);
                Assert(gateEmitted.Count == 1 && gateEmitted[0].IndexOf("no-active-session") >= 0,
                    "failure emission: surface no-active-session reason is emitted once");
            }
            finally
            {
                InventorySurfaceLifecycleAdapter.DiagnosticLogSink = previousGate;
            }
        }

        private static TestSurfaceContext CreateTestSurface(ContainerKind kind, byte page, uint generation)
        {
            return new TestSurfaceContext(new ContainerReference(kind, page, generation),
                new TestVisualContainer(), new TestVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, new EmptyGridForTest(8, 6));
        }

        private static SleekItems CreateNativeSleekItems(byte page, PlacedItem original)
        {
            var native = (SleekItems)System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(typeof(SleekItems));
            typeof(SleekItems).GetField("_page",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(native, page);
            native.onPlacedItem = original;
            return native;
        }

        private static UnturnedInventorySurfaceContext CreateNativeSurface(ContainerKind kind, byte page,
            uint generation, SleekItems nativeItems)
        {
            return new UnturnedInventorySurfaceContext(
                new ContainerReference(kind, page, generation),
                new TestVisualContainer(),
                new TestVisualContainer(),
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f,
                new EmptyGridForTest(8, 6),
                nativeItems,
                false);
        }

        private sealed class FixedCandidateEvaluator : IPlacementCandidateEvaluator
        {
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                return new ItemPlacementPreview(input.DragGeneration, PlacementPreviewState.Candidate,
                    new ItemGridPosition(input.TargetContainer.Page, 1, 1, input.CurrentRotation),
                    input.ItemWidth, input.ItemHeight, PlacementReason.None);
            }
        }

        private sealed class EmptyGridForTest : IGridOccupancyView
        {
            internal EmptyGridForTest(byte width, byte height) { Width = width; Height = height; }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y) { return false; }
        }

        private sealed class TestVisualElement : IVisualElement
        {
            public float PositionScaleX { get; set; }
            public float PositionScaleY { get; set; }
            public float PositionOffsetX { get; set; }
            public float PositionOffsetY { get; set; }
            public float SizeOffsetX { get; set; }
            public float SizeOffsetY { get; set; }
            public byte RotationAngle { get; set; }
            public bool CanRotate { get; set; }
            public bool IsVisible { get; set; }
            public PreviewFrameColor Color { get; set; }
            public ItemAssetIdentity BoundAsset { get; set; }
        }

        private sealed class TestVisualContainer : IVisualContainer
        {
            public IVisualElement CreateBox() { return new TestVisualElement(); }
            public IVisualElement CreateImage() { return new TestVisualElement(); }
            public void AddChild(IVisualElement child) { }
            public void RemoveChild(IVisualElement child) { }
        }

        private sealed class TestClientUiRoot : IClientUiRoot { }
        private sealed class TestRoot : IClientUiRoot { }

        private sealed class RecordingNativeDragActions : INativeInventoryDragActions
        {
            internal int StopCount;
            internal int SendCount;
            internal int GroundTakeCount;
            public void StopDrag() { StopCount++; }
            public void SendDragItem(ItemGridPosition source, ItemGridPosition target) { SendCount++; }
            public void TakeGroundItem(ItemGridPosition target) { GroundTakeCount++; }
        }

        private sealed class TestSurfaceContext : IInventoryPointerSurfaceContext, INativeInventoryOccupancyProvider
        {
            private bool occupancyAvailable = true;
            private readonly bool pointerAvailable;
            private readonly float pointerX;
            private readonly float pointerY;
            public ContainerReference CurrentContainer { get; }
            public IVisualContainer TopLevelContainer { get; }
            public IVisualContainer GridPanelContainer { get; }
            public InventoryGridViewport Viewport { get; }
            public float CellPixelSize { get; }
            public float UiScale { get; }
            public float ScrollPixelsX { get; }
            public float ScrollPixelsY { get; }
            public IGridOccupancyView Occupancy { get; }

            internal TestSurfaceContext(ContainerReference currentContainer, IVisualContainer topLevel,
                IVisualContainer gridPanel, InventoryGridViewport viewport, float cellPixelSize,
                float uiScale, float scrollPixelsX, float scrollPixelsY, IGridOccupancyView occupancy,
                bool pointerAvailable = false, float pointerX = 0f, float pointerY = 0f)
            {
                CurrentContainer = currentContainer;
                TopLevelContainer = topLevel;
                GridPanelContainer = gridPanel;
                Viewport = viewport;
                CellPixelSize = cellPixelSize;
                UiScale = uiScale;
                ScrollPixelsX = scrollPixelsX;
                ScrollPixelsY = scrollPixelsY;
                Occupancy = occupancy;
                this.pointerAvailable = pointerAvailable;
                this.pointerX = pointerX;
                this.pointerY = pointerY;
            }

            public bool TryGetLocalPointerPixels(out float x, out float y)
            {
                x = pointerX;
                y = pointerY;
                return pointerAvailable;
            }

            public bool TryCreateOccupancyForDrag(ContainerReference sourceContainer, ContainerReference targetContainer,
                ItemGridPosition source, byte itemWidth, byte itemHeight, byte sourceRotation,
                ItemAssetIdentity sourceAsset, out IGridOccupancyView occupancy)
            {
                occupancy = occupancyAvailable ? Occupancy : null;
                return occupancyAvailable;
            }

            public void InvalidateOccupancy() { occupancyAvailable = false; }
        }

        private static ItemJar CreateTestItemJar(byte x, byte y, byte rotation, byte width, byte height)
        {
            var jar = (ItemJar)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ItemJar));
            jar.x = x;
            jar.y = y;
            jar.rot = rotation;
            jar.size_x = width;
            jar.size_y = height;
            return jar;
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
            var itemsPanel = type.GetMethod("ResolveItemsPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert(itemsPanel != null, "inventory surface must resolve the native itemsPanel child");
            Assert(Math.Abs(UnturnedInventorySurfaceContext.ComputeScrollPixels(0f, 0.5f, 600f)) < 0.001f, "top scroll maps to zero pixels");
            Assert(Math.Abs(UnturnedInventorySurfaceContext.ComputeScrollPixels(1f, 0.5f, 600f) - 300f) < 0.001f, "bottom scroll maps to remaining pixels");
            Assert(Math.Abs(UnturnedInventorySurfaceContext.ComputeScrollPixels(1f, 1f, 600f)) < 0.001f, "fully visible content has no scroll range");
            float invalidPixels;
            Assert(!InventorySurfaceLifecycleAdapter.TryComputeScrollPixels(float.NaN, 0.5f, 600f, out invalidPixels), "invalid scroll values are rejected before projection");
        }

        private static void AssertNativeLikeViewportScaleAndHierarchyBehavior()
        {
            var mapped = UnturnedInventorySurfaceContext.MapNormalizedPointer(0.5f, 0.5f, 400f, 300f);
            Assert(Math.Abs(mapped.x - 200f) < 0.001f && Math.Abs(mapped.y - 150f) < 0.001f,
                "normalized pointer maps into the live grid viewport");
            Assert(UnturnedInventorySurfaceContext.MapNormalizedPointer(float.NaN, 0.5f, 400f, 300f) == UnityEngine.Vector2.zero,
                "invalid normalized pointer fails closed");
            Assert(UnturnedInventorySurfaceContext.IsNativeHierarchyComplete(true, true, true),
                "native-like SleekItems hierarchy is complete");
            Assert(!UnturnedInventorySurfaceContext.IsNativeHierarchyComplete(true, false, true),
                "incomplete native-like hierarchy fails closed");
            Assert(Math.Abs(UnturnedInventorySurfaceContext.NormalizeUiScale(1.5f) - 1.5f) < 0.001f,
                "non-default UI scale is preserved");
            var invalidScaleRejected = false;
            try { UnturnedInventorySurfaceContext.NormalizeUiScale(float.NaN); }
            catch (InvalidOperationException) { invalidScaleRejected = true; }
            Assert(invalidScaleRejected, "invalid UI scale is rejected before geometry projection");
        }

        // GPT watermark: native-like coordinate contract. A pointer sampled
        // from SleekItems.grid already includes horizontalScrollView's live
        // transform; feeding the same scroll a second time is forbidden.
        private static void AssertLiveGridScrollContract()
        {
            Assert(UnturnedInventorySurfaceContext.ResolveEffectiveScrollPixels(true, 240f) == 0f,
                "live grid pointer absorbs native scroll exactly once");
            Assert(Math.Abs(UnturnedInventorySurfaceContext.ResolveEffectiveScrollPixels(false, 240f) - 240f) < 0.001f,
                "screen-space pointer applies native scroll exactly once");
            var invalidScrollRejected = false;
            try { UnturnedInventorySurfaceContext.ResolveEffectiveScrollPixels(false, float.NaN); }
            catch (InvalidOperationException) { invalidScrollRejected = true; }
            Assert(invalidScrollRejected, "invalid native scroll is rejected before projection");
            Assert((int)UnturnedInventorySurfaceContext.PointerCoordinateMode.ViewportLocalRequiresScroll == 0,
                "surface advertises the viewport-local coordinate mode");
            Assert(Math.Abs(UnturnedInventorySurfaceContext.ResolveEffectiveScrollPixels(false, 240f) - 240f) < 0.001f,
                "viewport-local pointer applies native scroll exactly once");
            var viewport = UnturnedInventorySurfaceContext.BuildLiveGridViewport(8, 12, 400f, 300f);
            Assert(Math.Abs(viewport.OriginY) < 0.001f && Math.Abs(viewport.ClipY) < 0.001f,
                "live grid pointer and clip share one viewport-local origin");
            Assert(Math.Abs(viewport.ClipWidth - 400f) < 0.001f && Math.Abs(viewport.ClipHeight - 300f) < 0.001f,
                "live viewport clip comes from the scroll view size");
            var owner = new object();
            var scroll = new object();
            var grid = new object();
            var panel = new object();
            Assert(UnturnedInventorySurfaceContext.IsNativeHierarchyConsistent(owner, scroll, grid, panel, owner, scroll, grid),
                "fake native SleekItems hierarchy keeps scroll/grid/itemsPanel parent chain");
            Assert(!UnturnedInventorySurfaceContext.IsNativeHierarchyConsistent(owner, scroll, grid, panel, owner, owner, grid),
                "broken native parent chain fails closed");
            UnturnedInventorySurfaceContext.NativeInventoryHierarchySnapshot snapshot =
                new UnturnedInventorySurfaceContext.NativeInventoryHierarchySnapshot(true, true, true, true,
                    new UnityEngine.Vector2(0.5f, 0.5f), new UnityEngine.Vector2(600f, 900f),
                    new UnityEngine.Vector2(400f, 300f), 240f, 1.5f);
            UnityEngine.Vector2 pointer;
            Assert(UnturnedInventorySurfaceContext.TryBuildNativeGeometry(snapshot, out viewport, out pointer),
                "native hierarchy snapshot produces live viewport geometry");
            Assert(Math.Abs(pointer.x - 300f) < 0.001f && Math.Abs(pointer.y - 450f) < 0.001f,
                "pointer is mapped in the native grid content coordinate space exactly once");
            snapshot = new UnturnedInventorySurfaceContext.NativeInventoryHierarchySnapshot(true, true, false, true,
                new UnityEngine.Vector2(0.5f, 0.5f), new UnityEngine.Vector2(600f, 900f),
                new UnityEngine.Vector2(400f, 300f), 240f, 1.5f);
            Assert(!UnturnedInventorySurfaceContext.TryBuildNativeGeometry(snapshot, out viewport, out pointer),
                "incomplete native hierarchy fails closed before projection");
        }

        // GPT watermark: strict native geometry regression. Invalid viewport,
        // scroll, or NaN/Infinity snapshots must not silently turn into a
        // default clip/zero scroll that can produce a false Hidden preview.
        private static void AssertStrictNativeGeometryRejectsInvalidValues()
        {
            var viewportRejected = false;
            try
            {
                UnturnedInventorySurfaceContext.ResolveViewport(true,
                    new UnityEngine.Vector2(float.NaN, 180f), 5, 7, 0f, 0f, 250f, 350f);
            }
            catch (InvalidOperationException) { viewportRejected = true; }
            Assert(viewportRejected, "invalid native viewport dimensions are rejected instead of falling back");

            var liveViewportRejected = false;
            try { UnturnedInventorySurfaceContext.BuildLiveGridViewport(8, 12, float.PositiveInfinity, 300f); }
            catch (InvalidOperationException) { liveViewportRejected = true; }
            Assert(liveViewportRejected, "invalid live viewport size is rejected instead of using grid defaults");

            var snapshot = new UnturnedInventorySurfaceContext.NativeInventoryHierarchySnapshot(
                true, true, true, true, new UnityEngine.Vector2(0.5f, 0.5f),
                new UnityEngine.Vector2(float.NaN, 900f), new UnityEngine.Vector2(400f, 300f), 240f, 1.5f);
            UnityEngine.Vector2 pointer;
            InventoryGridViewport viewport;
            Assert(!UnturnedInventorySurfaceContext.TryBuildNativeGeometry(snapshot, out viewport, out pointer),
                "NaN grid dimensions reject native geometry before projection");

            snapshot = new UnturnedInventorySurfaceContext.NativeInventoryHierarchySnapshot(
                true, true, true, true, new UnityEngine.Vector2(0.5f, 0.5f),
                new UnityEngine.Vector2(600f, 900f), new UnityEngine.Vector2(float.PositiveInfinity, 300f), 240f, 1.5f);
            Assert(!UnturnedInventorySurfaceContext.TryBuildNativeGeometry(snapshot, out viewport, out pointer),
                "infinite viewport dimensions reject native geometry before projection");

            snapshot = new UnturnedInventorySurfaceContext.NativeInventoryHierarchySnapshot(
                true, true, true, true, new UnityEngine.Vector2(0.5f, 0.5f),
                new UnityEngine.Vector2(600f, 900f), new UnityEngine.Vector2(400f, 300f), float.NaN, 1.5f);
            Assert(!UnturnedInventorySurfaceContext.TryBuildNativeGeometry(snapshot, out viewport, out pointer),
                "NaN native scroll rejects geometry before projection");
        }

        // GPT watermark: R5 review regressions. Dependent drag hooks may only
        // activate after the lifecycle heartbeat is live; hierarchy probe
        // failures isolate the feature; cleanup bools must reach the poll
        // diagnostic boundary; and invalid UI scale cannot be normalized.
        private static void AssertDev16DR5ActivationAndCleanupContracts()
        {
            Assert(!BetterUnturnedExperiencePlugin.ShouldActivateDragPreview(false, false),
                "drag preview cannot activate before the inventory lifecycle heartbeat is installed");
            Assert(!BetterUnturnedExperiencePlugin.ShouldActivateDragPreview(true, true),
                "drag preview cannot activate after lifecycle isolation");
            Assert(BetterUnturnedExperiencePlugin.ShouldActivateDragPreview(true, false),
                "drag preview may activate only on a live inventory lifecycle adapter");

            Assert(InventorySurfaceLifecycleAdapter.ShouldIsolateOnHierarchyProbeFailure(false),
                "native hierarchy probe failure enters feature isolation");
            Assert(!InventorySurfaceLifecycleAdapter.ShouldIsolateOnHierarchyProbeFailure(true),
                "complete native hierarchy probe does not force isolation");
            Assert(UnturnedInventorySurfaceContext.ClassifyNativeHierarchy(false, true, false, false, false, false)
                == UnturnedInventorySurfaceContext.NativeHierarchyState.NotCreated,
                "missing SleekItems owner is retryable while the native surface is still being created");
            Assert(UnturnedInventorySurfaceContext.ClassifyNativeHierarchy(true, true, false, false, false, false)
                == UnturnedInventorySurfaceContext.NativeHierarchyState.Incompatible,
                "an existing native owner with missing children enters compatibility isolation");
            Assert(UnturnedInventorySurfaceContext.ClassifyNativeHierarchy(true, false, true, true, true, true)
                == UnturnedInventorySurfaceContext.NativeHierarchyState.Incompatible,
                "missing reflection members are a stable compatibility failure");
            Assert(UnturnedInventorySurfaceContext.ClassifyNativeHierarchy(true, true, true, true, true, false)
                == UnturnedInventorySurfaceContext.NativeHierarchyState.Incompatible,
                "an invalid native parent chain is a stable compatibility failure");
            Assert(UnturnedInventorySurfaceContext.ClassifyNativeHierarchy(true, true, true, true, true, true)
                == UnturnedInventorySurfaceContext.NativeHierarchyState.Ready,
                "a complete native hierarchy is ready for projection");
            Assert(InventorySurfaceLifecycleAdapter.ShouldIsolateOnHierarchyProbeFailure(
                    UnturnedInventorySurfaceContext.NativeHierarchyState.Incompatible),
                "the live poll isolates an incompatible native hierarchy state");
            Assert(!InventorySurfaceLifecycleAdapter.ShouldIsolateOnHierarchyProbeFailure(
                    UnturnedInventorySurfaceContext.NativeHierarchyState.NotCreated),
                "the live poll may retry before the native surface owner is created");

            var guardResult = InventorySurfaceLifecycleAdapter.InvokePollGuarded(
                () => { throw new InvalidOperationException("synthetic cleanup result propagation failure"); },
                () => false, () => { });
            Assert(!guardResult &&
                InventorySurfaceLifecycleAdapter.LastPollDiagnostics.Contains("BUE-DEV15D-CLEANUP-INCOMPLETE"),
                "poll guard carries a false IsolateAndDetach result into the canonical cleanup diagnostic");

            var invalidScaleRejected = false;
            try { UnturnedInventorySurfaceContext.NormalizeUiScale(float.NaN); }
            catch (InvalidOperationException) { invalidScaleRejected = true; }
            Assert(invalidScaleRejected, "invalid UI scale is rejected instead of silently normalized");

            var cleanupComposition = new BueClientUiCompositionRoot();
            Assert(cleanupComposition.Initialize(false, false, true), "cleanup propagation fixture initializes");
            cleanupComposition.OfficialComponent.RegisterCleanupResult(() => false);
            Assert(!cleanupComposition.OfficialComponent.IsolatePreviewFailureResult(),
                "component isolation returns false when a registered cleanup fails");
            Assert(cleanupComposition.OfficialComponent.Lifecycle.LastDiagnosticId == "BUE-DEV15D-CLEANUP-INCOMPLETE",
                "component isolation preserves the canonical cleanup-incomplete diagnostic");
            cleanupComposition.Destroy();
        }
        private static bool RequiresParentRebindSemantics()
        {
            var first = new object();
            var second = new object();
            return !BueNativeManagementPanel.RequiresParentRebind(first, first)
                && BueNativeManagementPanel.RequiresParentRebind(first, second)
                && !BueNativeManagementPanel.RequiresParentRebind(first, null);
        }

        // GPT watermark: end-to-end coordinate regression. A live-grid
        // pointer with non-default scale must reach the candidate seam; a
        // clipped pointer must fail closed before preview projection.
        private static void AssertLiveGridPointerReachesCandidateSeam()
        {
            var input = new InventoryPreviewInput(7, new ItemGridPosition(0, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 0, 7),
                75f, 75f, new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1.5f, 0f, 0f, 1, 1, 0, true, 0.5f, 0.5f,
                new IGridOccupancyViewForTest(8, 6));
            PlacementCandidateInput candidate;
            Assert(InventoryGridCoordinateAdapter.TryCreateCandidateInput(input, out candidate),
                "live-grid pointer reaches candidate seam at UI scale 1.5");
            Assert(Math.Abs(candidate.CursorGridX - 1.0f) < 0.001f && Math.Abs(candidate.CursorGridY - 1.0f) < 0.001f,
                "candidate center uses one scale application");

            var outside = new InventoryPreviewInput(7, new ItemGridPosition(0, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 0, 7),
                450f, 75f, input.Viewport, 50f, 1.5f, 0f, 0f, 1, 1, 0, true, 0.5f, 0.5f,
                new IGridOccupancyViewForTest(8, 6));
            Assert(!InventoryGridCoordinateAdapter.TryCreateCandidateInput(outside, out candidate),
                "pointer outside live viewport fails closed");
        }

        // GPT watermark: DEV-16D R10 red regression. U3-SDK's
        // SleekItems.grid.GetNormalizedCursorPosition() reports a pointer in
        // the scrolled grid-content coordinate space. The candidate adapter
        // must therefore not add the scroll offset a second time.
        private static void AssertGridContentPointerDoesNotDoubleApplyScroll()
        {
            var input = new InventoryPreviewInput(
                10,
                new ItemGridPosition(3, 0, 0, 0),
                new ContainerReference(ContainerKind.PlayerInventory, 3, 10),
                125f, 125f,
                new InventoryGridViewport(0f, 0f, 5, 8, 0f, 100f, 250f, 300f),
                50f, 1f, 0f, 100f,
                1, 1, 0, false, 0.5f, 0.5f,
                ItemAssetIdentity.FromItemId(363),
                float.NaN, float.NaN, float.NaN, float.NaN,
                InventoryPointerCoordinateSpace.GridContentLocal,
                new IGridOccupancyViewForTest(5, 8));
            PlacementCandidateInput candidate;
            Assert(InventoryGridCoordinateAdapter.TryCreateCandidateInput(input, out candidate),
                "scrolled native grid-content pointer reaches the candidate seam");
            Assert(Math.Abs(candidate.CursorGridX - 2.5f) < 0.001f &&
                   Math.Abs(candidate.CursorGridY - 2.5f) < 0.001f,
                "grid-content pointer consumes native scroll exactly once");
        }

        // GPT watermark: DEV-16D R11 red regression. The native
        // updateDraggedItem fast path can run before PlayerUI.Update's
        // lifecycle poll dispatches the first inventory surface. A dragging
        // frame without a surface must remain retryable so the later same
        // frame dispatch can evaluate and render the preview.
        private static void AssertDragTickDefersFrameCommitUntilSurfaceReady()
        {
            Assert(!InventoryDragPreviewAdapter.ShouldCommitPollFrame(true, false),
                "drag tick does not consume a frame before the inventory surface is ready");
            Assert(InventoryDragPreviewAdapter.ShouldCommitPollFrame(true, true),
                "drag tick commits a frame after the inventory surface is ready");
            Assert(InventoryDragPreviewAdapter.ShouldCommitPollFrame(false, false),
                "idle tick remains frame-deduplicated without an inventory surface");
        }

        // GPT watermark: R1 red regression. A surface opened at scroll=0 must
        // expose a live viewport whose content clip follows the same native
        // scroll position after the user scrolls to the bottom.
        private static void AssertDynamicViewportTracksCurrentScroll()
        {
            var top = UnturnedInventorySurfaceContext.BuildDynamicGridViewport(8, 12, 400f, 600f, 400f, 300f, 0f, 0.5f);
            var bottom = UnturnedInventorySurfaceContext.BuildDynamicGridViewport(8, 12, 400f, 600f, 400f, 300f, 1f, 0.5f);
            Assert(Math.Abs(top.ClipY) < 0.001f, "top scroll starts at content clip origin");
            Assert(Math.Abs(bottom.ClipY - 300f) < 0.001f, "bottom scroll advances content clip by the live scroll range");
            Assert(Math.Abs(bottom.ClipHeight - 300f) < 0.001f, "dynamic viewport keeps native viewport height");
        }

        // GPT watermark: R1 red regression. Native reflection/geometry errors
        // must enter one fail-closed seam that isolates the feature and hides
        // any stale projection instead of merely recording a diagnostic.
        private static void AssertPreviewReadFailureRoutesThroughIsolation()
        {
            var isolateCount = 0;
            var hideCount = 0;
            InventoryDragPreviewAdapter.FailClosedPreview(() => isolateCount++, () => hideCount++);
            Assert(isolateCount == 1 && hideCount == 1, "preview read failure isolates once and hides stale visuals");
        }

        // GPT watermark: R2 red regression. Native delegate and surface poll
        // exceptions must not escape into U3-SDK callbacks; they isolate BUE,
        // clear stale visuals, and preserve the native callback when evaluation
        // itself fails.
        private static void AssertNativeCallbackBoundariesAreGuarded()
        {
            var forwarded = 0;
            var isolated = 0;
            var hidden = 0;
            var detached = 0;
            InventoryDragPreviewAdapter.InvokePlacedItemGuarded(
                () => { throw new InvalidOperationException("synthetic evaluate failure"); },
                () => forwarded++, () => isolated++, () => hidden++, () => detached++);
            Assert(forwarded == 1 && isolated == 1 && hidden == 1 && detached == 1,
                "placed-item evaluation failure isolates, hides, and preserves native fallback");

            forwarded = 0;
            isolated = 0;
            hidden = 0;
            detached = 0;
            InventoryDragPreviewAdapter.InvokePlacedItemGuarded(
                () => true,
                () => { throw new InvalidOperationException("synthetic native callback failure"); },
                () => isolated++, () => hidden++, () => detached++);
            Assert(forwarded == 0 && isolated == 1 && hidden == 1 && detached == 1,
                "native placed-item callback failure is contained and isolated");

            var cleanupOk = InventoryDragPreviewAdapter.FailClosedPreview(
                () => { throw new InvalidOperationException("synthetic isolate cleanup failure"); },
                () => { throw new InvalidOperationException("synthetic hide cleanup failure"); });
            Assert(!cleanupOk && InventoryDragPreviewAdapter.LastCleanupDiagnostics.Contains("cleanup"),
                "cleanup exceptions are reported as incomplete instead of being silently swallowed");

            isolated = 0;
            hidden = 0;
            InventorySurfaceLifecycleAdapter.InvokePollGuarded(
                () => { throw new InvalidOperationException("synthetic viewport read failure"); },
                () => isolated++, () => hidden++);
            Assert(isolated == 1 && hidden == 1,
                "surface poll/viewport failure enters the same feature isolation boundary");

            var propagated = InventorySurfaceLifecycleAdapter.InvokePollGuarded(
                () => { throw new InvalidOperationException("synthetic cleanup propagation failure"); },
                () => { }, () => { throw new InvalidOperationException("synthetic cleanup hide failure"); });
            Assert(!propagated &&
                InventorySurfaceLifecycleAdapter.LastPollDiagnostics.Contains("BUE-DEV15D-CLEANUP-INCOMPLETE"),
                "poll guard propagates the canonical cleanup-incomplete diagnostic instead of dropping the cleanup result");
        }

        // GPT watermark: DEV-16D R3 red regressions for the independent
        // Standards/Spec review blockers. These assertions must remain at the
        // adapter/lifecycle seams rather than inspecting private implementation
        // state from the test harness.
        private static void AssertDev16DR3IsolationAndGeometryContracts()
        {
            Assert(!InventoryDragPreviewAdapter.CanAttachGrid(true),
                "isolated drag adapter must reject a rebuilt surface re-attachment");
            Assert(InventoryDragPreviewAdapter.CanAttachGrid(false),
                "active drag adapter may attach a live surface");

            var cleanupOk = InventoryDragPreviewAdapter.FailClosedPreview(
                () => { throw new InvalidOperationException("synthetic isolate cleanup failure"); },
                () => { throw new InvalidOperationException("synthetic hide cleanup failure"); });
            Assert(!cleanupOk && InventoryDragPreviewAdapter.LastCleanupDiagnostics.Contains("BUE-DEV15D-CLEANUP-INCOMPLETE"),
                "cleanup failure must publish the stable DEV-16D incomplete-cleanup diagnostic");

            var settings = new BetterItemInteractionSettingsState();
            var lifecycle = new BetterItemInteractionLifecycle();
            var runtime = new BetterItemInteractionRuntime(settings, lifecycle);
            runtime.Start(true, true);
            runtime.RegisterCleanupResult(() => false);
            runtime.Isolate();
            Assert(runtime.CleanupFailed && lifecycle.LastDiagnosticId == "BUE-DEV15D-CLEANUP-INCOMPLETE",
                "cleanup result failure must propagate to lifecycle isolation diagnostics");

            float ignoredPixels;
            Assert(!InventorySurfaceLifecycleAdapter.TryComputeScrollPixels(float.NaN, 0.5f, 600f, out ignoredPixels),
                "invalid native viewport/scroll values must be rejected before projection");
            Assert(InventorySurfaceLifecycleAdapter.TryComputeScrollPixels(1f, 0.5f, 600f, out ignoredPixels)
                && Math.Abs(ignoredPixels - 300f) < 0.001f,
                "valid native viewport/scroll values still map to pixels");
        }

        // GPT watermark: DEV-16D R4 red regressions for rebind invalidation,
        // hierarchy disappearance and native-hook compatibility projection.
        private static void AssertDev16DR4RebindAndFailureProjectionContracts()
        {
            Assert(!InventoryDragPreviewAdapter.CanContinueGridAttach(false, true),
                "a failed detach must stop the attach path even before isolated state is observed");
            Assert(!InventoryDragPreviewAdapter.CanContinueGridAttach(true, true),
                "an isolated adapter must never continue a grid attach");
            Assert(InventoryDragPreviewAdapter.CanContinueGridAttach(true, false),
                "a successful detach on a live adapter may continue the attach path");

            var firstSurface = new object();
            var rebuiltSurface = new object();
            Assert(InventorySurfaceLifecycleAdapter.RequiresSurfaceRebind(firstSurface, rebuiltSurface),
                "same-generation native UI rebuild is detected by surface identity");
            Assert(!InventorySurfaceLifecycleAdapter.RequiresSurfaceRebind(firstSurface, firstSurface),
                "unchanged native surface does not trigger a redundant rebind");
            Assert(InventorySurfaceLifecycleAdapter.ShouldDiscardSurface(true, false),
                "a dispatched surface is discarded when native hierarchy is temporarily unavailable");
            Assert(!InventorySurfaceLifecycleAdapter.ShouldDiscardSurface(false, false),
                "an undispatched unavailable surface does not emit a duplicate close");

            var composition = new BueClientUiCompositionRoot();
            Assert(composition.Initialize(false, false, true), "composition initializes for failure projection");
            composition.OfficialComponent.IsolatePreviewFailure();
            Assert(composition.OfficialComponent.Lifecycle.State == FeatureState.Isolated &&
                composition.OfficialComponent.Lifecycle.Presentation.State == FeaturePresentationState.PresentationDegraded,
                "native hook/geometry failure isolates the feature and projects degraded presentation");
            composition.Destroy();

            Assert(InventoryDragPreviewAdapter.ShouldIsolateOnHookFailure(false),
                "drag hook incompatibility enters feature isolation");
            Assert(InventorySurfaceLifecycleAdapter.ShouldIsolateOnHookFailure(false),
                "inventory lifecycle hook incompatibility enters feature isolation");
        }

        // GPT watermark: DEV-16D R9 red regressions for the Spec blockers.
        // The component cleanup callback must observe the already-known detach
        // result, and placed-item isolation must preserve Func<bool> failure
        // results instead of coercing them through a void Action seam.
        private static void AssertDev16DR9CleanupPropagationContracts()
        {
            var observedDetachState = true;
            var cleanupResult = InventoryDragPreviewAdapter.CompleteIsolationCleanup(
                false,
                state =>
                {
                    observedDetachState = state;
                    return state;
                },
                () => { });
            Assert(!observedDetachState && !cleanupResult,
                "component isolation observes a failed detach before re-entrant cleanup");

            var forwarded = 0;
            var detached = 0;
            InventoryDragPreviewAdapter.InvokePlacedItemGuarded(
                () => { throw new InvalidOperationException("synthetic placed-item failure"); },
                () => forwarded++,
                () => false,
                () => { },
                () =>
                {
                    detached++;
                    return false;
                });
            Assert(forwarded == 1 && detached == 1 &&
                InventoryDragPreviewAdapter.LastCleanupDiagnostics.Contains("BUE-DEV15D-CLEANUP-INCOMPLETE"),
                "placed-item isolation preserves both detach and component cleanup failures");
        }

        private sealed class IGridOccupancyViewForTest : IGridOccupancyView
        {
            internal IGridOccupancyViewForTest(byte width, byte height) { Width = width; Height = height; }
            internal IGridOccupancyViewForTest(byte width, byte height, System.Collections.Generic.IReadOnlyList<System.ValueTuple<byte, byte>> occupied)
            {
                Width = width;
                Height = height;
                this.occupied = occupied;
            }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y)
            {
                if (occupied == null) return false;
                foreach (var cell in occupied) if (cell.Item1 == x && cell.Item2 == y) return true;
                return false;
            }
            private readonly System.Collections.Generic.IReadOnlyList<System.ValueTuple<byte, byte>> occupied;
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
            var items = new Items(7);
            typeof(Items).GetField("_width", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(items, (byte)3);
            typeof(Items).GetField("_height", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(items, (byte)3);
            items.items.Add(CreateTestItemJar(1, 1, 0, 1, 1));
            var occupancy = new UnturnedGridOccupancyView(items);
            Assert(BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupancy, 1, 1, 2, 2),
                "footprint origin covering the occupied cell counts as occupied");
            Assert(!BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupancy, 2, 2, 1, 1),
                "footprint away from the occupied cell counts as empty");
            Assert(BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupancy, 0, 0, 2, 2),
                "footprint touching the occupied cell at (1,1) counts as occupied");
            Assert(BetterUnturnedExperience.Plugin.InventoryDragPreviewAdapter.FootprintOccupied(occupancy, 2, 2, 2, 2),
                "out-of-bounds footprint fails closed");
        }

        // [R43] Single coordinate space: the viewport is always grid-local
        // (origin 0,0, clip exactly grid pixels) so the native cursor
        // conversion and the clip can never disagree; hierarchy/scroll state
        // only affects degradation logging, never geometry.
        private static void AssertSurfaceViewportDegradation()
        {
            var degraded = BetterUnturnedExperience.Plugin.UnturnedInventorySurfaceContext.ResolveViewport(
                false, new UnityEngine.Vector2(0f, 0f), 5, 7, 3f, 4f, 250f, 350f);
            var live = BetterUnturnedExperience.Plugin.UnturnedInventorySurfaceContext.ResolveViewport(
                true, new UnityEngine.Vector2(400f, 500f), 5, 7, 3f, 4f, 250f, 350f);
            Assert(degraded.OriginX == 0f && degraded.OriginY == 0f && degraded.ClipWidth == 250f && degraded.ClipHeight == 350f,
                "degraded hierarchy still yields the grid-local clip");
            Assert(live.OriginX == 0f && live.ClipWidth == 400f && live.ClipHeight == 500f,
                "live hierarchy consumes the native scroll viewport clip");
            Assert(degraded.Contains(249f, 349f) && !degraded.Contains(251f, 10f),
                "grid-local clip contains in-grid pointers and rejects out-of-grid ones");
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
