using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.NoOpFixture;
using BetterUnturnedExperience.Plugin;
using BetterUnturnedExperience.ClientUi.Internal;
using HarmonyLib;
using SDG.Unturned;
using System.IO;
using BetterUnturnedExperience.Core.Settings;

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
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--logging-verbose-red")
                {
                    AssertLoggingRuntimeVerbosity();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--logging-bue-runtime-red")
                {
                    AssertLoggingBueRuntimeClassification();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--logging-timeout-red")
                {
                    AssertLoggingTimeoutIsNotError();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--logging-aggregate-red")
                {
                    AssertLoggingAggregateSuccess();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--sdk-net-baseline-red")
                {
                    AssertSdkNetTransportBaseline();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-network-contract-red")
                {
                    AssertBueNetworkContract();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-network-runtime-red")
                {
                    AssertBueNetworkRuntime();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-takeover-red")
                {
                    AssertBueTakeover();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v1-compat-red")
                {
                    AssertBueV1Compat();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-config-migration-red")
                {
                    AssertBueConfigMigration();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v1-mirror-timing-red")
                {
                    AssertBueV1MirrorTiming();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-network-definitions-red")
                {
                    AssertBueNetworkRegistrationDefinitions();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-network-panel-red")
                {
                    AssertBueNetworkPanelEntries();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-network-killswitch-red")
                {
                    AssertBueNetworkKillSwitchLifecycle();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-lmn-type-names-red")
                {
                    AssertBueLmnTypeNameAnchor();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-sender-identity-red")
                {
                    AssertBueV2SenderIdentity();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-subscribe-red")
                {
                    AssertBueV2DirectionalSubscribe();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-network-injection-red")
                {
                    AssertBueV2NetworkInjection();
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
                AssertMainMenuEntryLayoutMatchesVanillaRhythm();
                AssertPauseMenuEntryLayoutMatchesNativeColumn();
                AssertPauseColumnShiftAnchorsWithoutDrift();
                AssertPauseShiftFieldsResolveAgainstVanillaAssembly();
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
                AssertLoggingRuntimeVerbosity();
                AssertLoggingBueRuntimeClassification();
                AssertLoggingTimeoutIsNotError();
                AssertLoggingAggregateSuccess();
                AssertSdkNetTransportBaseline();
                AssertBueNetworkContract();
                AssertBueNetworkRuntime();
                AssertBueTakeover();
                AssertBueV1Compat();
                AssertBueConfigMigration();
                AssertBueV1MirrorTiming();
                AssertBueNetworkRegistrationDefinitions();
                AssertBueNetworkPanelEntries();
                AssertBueNetworkKillSwitchLifecycle();
                AssertBueLmnTypeNameAnchor();
                AssertBueV2SenderIdentity();
                AssertBueV2DirectionalSubscribe();
                AssertBueV2NetworkInjection();
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

        // GPT watermark: DEV-16G ticket B red regression. The user's new log
        // policy: load/inject stages announce (Info), errors print a reason
        // (Warning/Error), but in-game RUNTIME events must be SILENT during
        // normal play (Debug level, filtered by BepInEx unless Levels=Debug).
        // This red test drives the BueRuntimeLog seam that does not exist yet:
        // - Runtime (verbose) events go to Debug.
        // - Load one-shots go to Info.
        // - Errors go to Warning/Error AND are never swallowed by the verbosity
        //   gate (ERROR_ALWAYS always passes).
        private static void AssertLoggingRuntimeVerbosity()
        {
            var recorded = new System.Collections.Generic.List<string>();
            var previous = BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder;
            BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder = line => recorded.Add(line);
            try
            {
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Runtime("event=drag-started");
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Runtime("event=placement-decision outcome=Submitted");
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Runtime("event=preview-evaluated state=Candidate");
                Assert(recorded.Count == 3,
                    "runtime verbosity: runtime events record all three");
                Assert(recorded[0].IndexOf("Debug") >= 0 && recorded[1].IndexOf("Debug") >= 0 &&
                    recorded[2].IndexOf("Debug") >= 0,
                    "runtime verbosity: runtime events are emitted at Debug level (silent by default)");

                recorded.Clear();
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Load("event=hooks-installed");
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Load("event=surface-context-dispatched");
                Assert(recorded.Count == 2 && recorded[0].IndexOf("Info") >= 0 && recorded[1].IndexOf("Info") >= 0,
                    "runtime verbosity: load one-shots are emitted at Info level");

                recorded.Clear();
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Error("event=diagnostic-failure reason=cleanup-incomplete");
                Assert(recorded.Count == 1 && recorded[0].IndexOf("Error") >= 0 &&
                    recorded[0].IndexOf("cleanup-incomplete") >= 0,
                    "runtime verbosity: errors are emitted at Error level WITH reason, never swallowed");

                // Error must pass even when the verbosity gate is off (default).
                recorded.Clear();
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Error("event=projection-timed-out reason=timeout");
                Assert(recorded.Count == 1 && recorded[0].IndexOf("Error") >= 0,
                    "runtime verbosity: ERROR_ALWAYS is not swallowed by the runtime-silent gate");
            }
            finally
            {
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder = previous;
            }
        }

        // GPT watermark: DEV-16G ticket-C red regression. The user reported the
        // BUE management-panel still spams `[BUE-UI-TRACE] event=surface-opened /
        // constructor-postfix / create-button-* / add-child-success /
        // container-state` on every menu open and UI rebuild. These are
        // recurring in-game events and must be SILENT (Debug), while true
        // load one-shots (constructed / initialize-complete / patch-installed /
        // host-ui-tick / first-tick) stay Info. RED until the classifier seam
        // (BueRuntimeLog.IsRuntimeEvent) exists.
        private static void AssertLoggingBueRuntimeClassification()
        {
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("surface-opened"),
                "BUE panel surface-opened is a runtime event (silent in normal play)");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("constructor-postfix"),
                "BUE panel constructor-postfix is a runtime event (UI rebuild)");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("create-button-begin"),
                "BUE panel create-button-begin is a runtime event (menu open)");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("create-button-result"),
                "BUE panel create-button-result is a runtime event (menu open)");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("add-child-success"),
                "BUE panel add-child-success is a runtime event (menu open)");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("container-state"),
                "BUE panel container-state is a runtime event (menu state)");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("heartbeat"),
                "BUE panel heartbeat is a runtime event");

            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("constructed"),
                "DEV-16G-D: constructed is demoted to runtime (Debug) — aggregate line replaces per-subsystem Info");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("initialize-complete"),
                "DEV-16G-D: initialize-complete is demoted to runtime (Debug) — aggregate line replaces per-subsystem Info");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("patch-installed"),
                "DEV-16G-D: patch-installed is demoted to runtime (Debug) — aggregate line replaces per-subsystem Info");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("host-ui-tick"),
                "DEV-16G-D: host-ui-tick is demoted to runtime (Debug) — aggregate line replaces per-subsystem Info");
            Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("first-tick"),
                "DEV-16G-D: first-tick is demoted to runtime (Debug) — aggregate line replaces per-subsystem Info");
        }

        // GPT watermark: DEV-16G ticket-C red regression. The user reported a
        // log ERROR (projection-timed-out) while the feature worked fine. Root
        // cause: the AwaitingProjectionController visual budget (2000ms) expired
        // — the placement was ALREADY submitted natively and is authoritative;
        // the timeout only stops the VISUAL wait (no fake rollback). Logging it
        // at Error level is a false-positive severity. It must be emitted at
        // Debug (Runtime), never Error. RED until the sink stops using
        // BueRuntimeLog.Error for this benign condition.
        private static void AssertLoggingTimeoutIsNotError()
        {
            var recorded = new System.Collections.Generic.List<string>();
            var previous = BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder;
            BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder = line => recorded.Add(line);
            try
            {
                var sink = new LoggingInventoryProjectionSink(new BepInEx.Logging.ManualLogSource("test"));
                sink.OnProjectionTimedOut();
                Assert(recorded.Count == 1,
                    "projection timeout emits exactly one line");
                Assert(recorded[0].IndexOf("Debug") >= 0,
                    "projection timeout is a benign visual-budget expiry, emitted at Debug not Error");
                Assert(recorded[0].IndexOf("Error") < 0,
                    "projection timeout must not be logged as Error (placement is authoritative)");
                Assert(recorded[0].IndexOf("reason=native-convergence-timeout") >= 0,
                    "projection timeout Debug line retains the reason for triage");
            }
            finally
            {
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder = previous;
            }
        }

        // GPT watermark: DEV-16G ticket-D red regression. The user wants the
        // whole load story collapsed into ONE aggregate success line after
        // RuntimeReady ("加载成功，界面已注入"), all per-subsystem load Info
        // demoted to Debug, and surface-not-ready split by reason (critical
        // hierarchy-incomplete -> Error; benign empty-grid/scroll -> Debug).
        // This red test drives the seams that do not exist yet.
        private static void AssertLoggingAggregateSuccess()
        {
            var recorded = new System.Collections.Generic.List<string>();
            var previous = BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder;
            BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder = line => recorded.Add(line);
            try
            {
                // Aggregate success line: exactly one, at Info, with the
                // user-facing wording.
                BetterUnturnedExperience.Plugin.BueRuntimeLog.ResetReadyAnnouncement();
                BetterUnturnedExperience.Plugin.BueRuntimeLog.AnnounceReady(false);
                BetterUnturnedExperience.Plugin.BueRuntimeLog.AnnounceReady(false);
                Assert(recorded.Count == 1,
                    "aggregate success line emits exactly once (guard suppresses repeats)");
                Assert(recorded[0].IndexOf("Info") >= 0 &&
                    recorded[0].IndexOf("加载成功") >= 0 && recorded[0].IndexOf("界面已注入") >= 0,
                    "aggregate success line is Info and carries the user-facing wording");

                // Headless variant announces load without UI.
                recorded.Clear();
                BetterUnturnedExperience.Plugin.BueRuntimeLog.ResetReadyAnnouncement();
                BetterUnturnedExperience.Plugin.BueRuntimeLog.AnnounceReady(true);
                Assert(recorded.Count == 1 && recorded[0].IndexOf("Info") >= 0 &&
                    recorded[0].IndexOf("无界面") >= 0,
                    "headless aggregate line announces load without UI");

                // Per-subsystem load events are demoted to Debug (silent).
                Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("hooks-installed"),
                    "DEV-16G-D: hooks-installed is demoted to runtime (Debug)");
                Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("polling-hook-installed"),
                    "DEV-16G-D: polling-hook-installed is demoted to runtime (Debug)");
                Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("surface-context-dispatched"),
                    "DEV-16G-D: surface-context-dispatched is demoted to runtime (Debug)");
                Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("surface-ready"),
                    "DEV-16G-D: surface-ready is demoted to runtime (Debug)");
                Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("surface-discarded"),
                    "DEV-16G-D: surface-discarded is demoted to runtime (Debug)");
                Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("wiring-enabled"),
                    "DEV-16G-D: wiring-enabled is demoted to runtime (Debug)");

                // surface-not-ready: critical reason -> Error, benign -> Debug.
                Assert(!BetterUnturnedExperience.Plugin.BueRuntimeLog.IsRuntimeEvent("surface-not-ready"),
                    "DEV-16G-D: surface-not-ready stays a distinct event (reason decides severity)");
                Assert(BetterUnturnedExperience.Plugin.BueRuntimeLog.IsCriticalNotReadyReason("native-hierarchy-incomplete"),
                    "DEV-16G-D: native-hierarchy-incomplete is a critical not-ready reason (Error)");
                Assert(!BetterUnturnedExperience.Plugin.BueRuntimeLog.IsCriticalNotReadyReason("empty-grid"),
                    "DEV-16G-D: empty-grid is a benign not-ready reason (Debug)");
                Assert(!BetterUnturnedExperience.Plugin.BueRuntimeLog.IsCriticalNotReadyReason("scroll-viewport-not-laid-out"),
                    "DEV-16G-D: scroll-viewport-not-laid-out is a benign not-ready reason (Debug)");
            }
            finally
            {
                BetterUnturnedExperience.Plugin.BueRuntimeLog.Recorder = previous;
                BetterUnturnedExperience.Plugin.BueRuntimeLog.ResetReadyAnnouncement();
            }
        }

        // GPT watermark: DEV-V2-01 SDK baseline lock. The BUE network module is
        // built on the vanilla ITransportConnection interface (SDG.NetTransport).
        // This red test pins the interface member surface so an Unturned SDK
        // update that changes the interface (compilation-breaking) is caught
        // here first, before any network code consumes it. Baseline document:
        // .scratch/bue-v2-lmn-adoption/research/V2-NET-BASELINE-sdg-nettransport-20260903.md
        private static void AssertSdkNetTransportBaseline()
        {
            var transportType = typeof(SDG.NetTransport.ITransportConnection);
            Assert(transportType != null, "SDG.NetTransport.ITransportConnection resolves (Libs refreshed)");

            var iface = transportType.GetInterfaces();
            Assert(System.Array.Exists(iface, i => i.Name == "IEquatable`1"),
                "ITransportConnection implements IEquatable<ITransportConnection>");

            var methods = transportType.GetMethods();
            string[] expected = {
                "TryGetIPv4Address", "TryGetPort", "TryGetSteamId",
                "GetAddress", "GetAddressString", "CloseConnection", "Send"
            };
            foreach (var name in expected)
            {
                Assert(System.Array.Exists(methods, m => m.Name == name),
                    "ITransportConnection member " + name + " present (SDK baseline locked)");
            }

            var send = System.Array.Find(methods, m => m.Name == "Send");
            Assert(send != null && send.GetParameters().Length == 3,
                "Send(buffer, size, ENetReliability) signature (3 params)");
            if (send != null)
            {
                var sendParams = send.GetParameters();
                Assert(sendParams[0].ParameterType == typeof(byte[]) &&
                    sendParams[1].ParameterType == typeof(long) &&
                    sendParams[2].ParameterType == typeof(SDG.NetTransport.ENetReliability),
                    "Send parameter types bound (byte[], long, ENetReliability)");
            }
            var reliability = typeof(SDG.NetTransport.ENetReliability);
            Assert(reliability.IsEnum && System.Enum.GetNames(reliability).Length == 2,
                "ENetReliability has exactly Reliable/Unreliable (2 values)");

            // DEV-V2-12: the sender identity arrives as the out ulong itself.
            // The old resolver read it through a CSteamID field and threw to 0
            // for every connection — pin the SDK shape so any drift fails here.
            var tryGetSteamId = System.Array.Find(methods, m => m.Name == "TryGetSteamId");
            Assert(tryGetSteamId != null && tryGetSteamId.GetParameters().Length == 1,
                "TryGetSteamId has exactly one parameter (SDK baseline locked)");
            if (tryGetSteamId != null)
            {
                var steamIdParam = tryGetSteamId.GetParameters()[0];
                Assert(steamIdParam.IsOut && steamIdParam.ParameterType.GetElementType() == typeof(ulong),
                    "TryGetSteamId(out ulong) signature (the resolved sender is the out value itself)");
            }
        }

        // GPT watermark: DEV-V2-02 red regression. T3 Q1-Q12 froze the
        // BueNetworkApi public contract shape. This red test pins the
        // BueNetwork namespace types before they exist (compile-red CS0246),
        // then asserts their surface after implementation:
        // - Channel = FeatureId, version negotiation returns ContractIncompatible
        // - NetworkSendResult explicit enum (localizable, no exceptions)
        // - IConnectionSession carries SessionId (generation) + events +
        //   Send + PeerSteamId + PeerFeatureVersion + Channels
        // - Send targets by connection context (SendToServer/SendToClients/
        //   SendToClient(session)), no peer-FeatureId addressing
        private static void AssertBueNetworkContract()
        {
            // Q12: public types live in the BueNetwork namespace.
            var apiType = typeof(BetterUnturnedExperience.Contracts.BueNetwork.IBueNetworkApi);
            var sessionType = typeof(BetterUnturnedExperience.Contracts.BueNetwork.IConnectionSession);
            var sendResultType = typeof(BetterUnturnedExperience.Contracts.BueNetwork.NetworkSendResult);
            var registrationResultType = typeof(BetterUnturnedExperience.Contracts.BueNetwork.ChannelRegistrationResult);

            Assert(apiType.Namespace == "BetterUnturnedExperience.Contracts.BueNetwork",
                "Q12: BueNetworkApi public types live in BueNetwork namespace");
            Assert(sendResultType.IsEnum,
                "Q11: NetworkSendResult is an explicit enum");

            // Q1: channel = FeatureId (one module = one named channel).
            var register = apiType.GetMethod("RegisterChannel");
            Assert(register != null,
                "Q1: IBueNetworkApi.RegisterChannel(FeatureId, ContractVersion, ushort) exists");
            if (register != null)
            {
                var ps = register.GetParameters();
                Assert(ps.Length == 3 &&
                    ps[0].ParameterType == typeof(BetterUnturnedExperience.Contracts.FeatureId) &&
                    ps[1].ParameterType == typeof(BetterUnturnedExperience.Contracts.ContractVersion),
                    "Q1/Q2: RegisterChannel takes (FeatureId, MinimumBueContract, featureVersion)");
            }

            // Q2: registration result reuses FeatureRegistrationReason (ContractIncompatible).
            var reasonProp = registrationResultType.GetProperty("Reason");
            Assert(reasonProp != null && reasonProp.PropertyType == typeof(BetterUnturnedExperience.Contracts.FeatureRegistrationReason),
                "Q2: ChannelRegistrationResult.Reason is FeatureRegistrationReason");

            // Q4/Q10: session carries generation + events + peer identity + channels.
            Assert(sessionType.GetProperty("SessionId") != null &&
                sessionType.GetProperty("SessionId").PropertyType == typeof(ulong),
                "Q4: IConnectionSession.SessionId (connection generation)");
            Assert(sessionType.GetProperty("PeerSteamId") != null &&
                sessionType.GetProperty("PeerSteamId").PropertyType == typeof(ulong),
                "Q10: IConnectionSession.PeerSteamId");
            Assert(sessionType.GetProperty("PeerFeatureVersion") != null,
                "Q10: IConnectionSession.PeerFeatureVersion");
            Assert(sessionType.GetProperty("Channels") != null,
                "Q10: IConnectionSession.Channels (negotiated channel version table)");
            Assert(sessionType.GetEvent("Connected") != null &&
                sessionType.GetEvent("Disconnected") != null &&
                sessionType.GetEvent("GenerationChanged") != null,
                "Q4: IConnectionSession Connected/Disconnected/GenerationChanged events");

            // Q9: send by connection context, no peer-FeatureId addressing.
            Assert(apiType.GetMethod("SendToServer") != null &&
                apiType.GetMethod("SendToClients") != null &&
                apiType.GetMethod("SendToClient") != null,
                "Q9: SendToServer/SendToClients/SendToClient(session) — connection-context addressing");
            var sendToClient = apiType.GetMethod("SendToClient");
            Assert(sendToClient != null &&
                sendToClient.GetParameters().Length == 4 &&
                sendToClient.GetParameters()[1].ParameterType == sessionType,
                "Q9: SendToClient(channel, IConnectionSession, payload, reliable) — session is the target context, no peer FeatureId");
        }

        // GPT watermark: DEV-V2-03 red regression. The BueNetworkApi runtime
        // implements the frozen BueNetwork contract surface on top of an
        // INetworkTransport seam (Host-internal, pure C#). This red test drives
        // two runtimes over a LocalLoopbackPair: channel registration, version
        // negotiation (ContractIncompatible), Hello/Ack peer handshake (session
        // established on BOTH sides and Connected fired), and a round-trip
        // send/receive with payload integrity. RED until StartSession and the
        // handshake exist (compile CS0234 / runtime assertion).
        private static void AssertBueNetworkRuntime()
        {
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var localContract = new ContractVersion(2, 0);
            var a = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1002UL);
            var b = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL);

            // Q1: register one named channel per module (FeatureId is the name).
            var channel = new FeatureId("com.example.chat");
            var register = a.RegisterChannel(channel, localContract, 1);
            Assert(register.Accepted && register.Channel.Value == channel.Value && register.Reason == FeatureRegistrationReason.None,
                "Q1: fresh channel registration is accepted");
            // DEV-V2-14: the receiving side registers the channel too — a
            // frame dispatches only once the receiver's channel is registered.
            Assert(b.RegisterChannel(channel, localContract, 1).Accepted,
                "setup: the receiver registers the channel (dispatch gate is the receiver's channel table)");

            // Q2: version negotiation — a channel demanding a higher contract
            // than the local runtime is rejected with ContractIncompatible.
            var tooNew = new ContractVersion(3, 0);
            var incompatible = a.RegisterChannel(new FeatureId("com.example.future"), tooNew, 1);
            Assert(!incompatible.Accepted && incompatible.Reason == FeatureRegistrationReason.ContractIncompatible,
                "Q2: contract-incompatible registration returns ContractIncompatible");

            // Q4: Hello/Ack handshake — StartSession on A establishes a session
            // on BOTH sides; Connected fires on both.
            var aConnected = 0;
            var bConnected = 0;
            var aSession = a.StartSession(2002UL);
            aSession.Connected += () => aConnected++;
            pair.First.Pump(); pair.Second.Pump(); // A Hello -> B (B creates session, sends Ack)
            pair.First.Pump();                      // B Ack -> A (A establishes, fires Connected)
            Assert(aConnected == 1,
                "Q4/F2: initiator Connected fires after Ack (Hello/Ack handshake complete)");
            Assert(b.Sessions.Count == 1 && b.Sessions[0].PeerSteamId == 1002UL,
                "Q4: peer runtime created a session for the initiator after Hello");
            var bSession = b.Sessions[0];
            bSession.Connected += () => bConnected++;
            Assert(bConnected == 0,
                "Q4: peer session was established during handshake (no late Connected)");
            Assert(bSession.PeerContract.Major == localContract.Major,
                "Q10: peer session carries the negotiated contract");

            // Q9+reliability: round-trip send over the loopback with payload
            // integrity; receiver's Subscribe handler gets the bytes.
            var received = new System.Collections.Generic.List<byte[]>();
            // DEV-V2-14: B answered the handshake, so B's inbound frames come
            // FROM CLIENTS (the initiator is the client side of the pair).
            var subscription = b.Subscribe(channel, ChannelDirection.FromClients, (session, payload) => received.Add(payload));
            var payload = new byte[] { 1, 2, 3, 4, 0xAA, 0xBB };
            var send = a.SendToClient(channel, aSession, payload, reliable: true);
            Assert(send == NetworkSendResult.Sent,
                "Q9/Q3: SendToClient(session) returns Sent on the loopback");
            pair.First.Pump(); pair.Second.Pump();
            Assert(received.Count == 1 && received[0].Length == payload.Length &&
                received[0][4] == 0xAA && received[0][5] == 0xBB,
                "runtime: receiver's Subscribe handler receives the exact payload");
            subscription.Dispose();

            // Contract-incompatible peer: Hello is rejected, no session on the
            // peer, initiator Connected never fires.
            var pair2 = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var c = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair2.First, localContract, 3003UL);
            var d = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair2.Second, new ContractVersion(3, 0), 4004UL);
            var cConnected = 0;
            var cSession = c.StartSession(4004UL);
            cSession.Connected += () => cConnected++;
            pair2.First.Pump(); pair2.Second.Pump(); // C Hello -> D (D rejects, sends Reject)
            pair2.First.Pump();                      // D Reject -> C (no Connected, pending session torn down)
            Assert(cConnected == 0 && d.Sessions.Count == 0,
                "Q2/F1: contract-incompatible Hello is rejected (no session, no Connected)");
            Assert(c.Sessions.Count == 0,
                "S3: rejected handshake tears down the initiator's pending session (no ghost)");

            // No-session error: SendToClient with a session context from a
            // different runtime fails. Register the channel on C first so the
            // path under test is the ownership check (NoSession), not an
            // unregistered-channel error.
            var cRegister = c.RegisterChannel(channel, localContract, 1);
            Assert(cRegister.Accepted, "setup: C registers the chat channel before the detached-send check");
            var detached = c.SendToClient(channel, bSession, payload, reliable: true);
            Assert(detached == NetworkSendResult.NoSession,
                "runtime: send targeting a session not owned by this runtime returns NoSession");
        }

        // GPT watermark: DEV-V2-04 red regression. The takeover mechanism
        // (T5): detect standalone LMN via Chainloader.PluginInfos (injected as
        // a Func<bool> seam for tests), then short-circuit MOD/LMN2 frames with
        // a Priority.First prefix so LMN's own prefix never runs. This red test
        // pins the pure-C# decision core: frame classification (MOD legacy /
        // LMN2 namespaced magic bytes), takeover-active gating (no false
        // positive when LMN is absent), and the panel recovery signal. RED
        // until LmnFrameClassifier / LmnTakeoverCoordinator exist (CS0234).
        private static void AssertBueTakeover()
        {
            // Frame classification: MOD legacy magic (0x4D 0x4F 0x44) and LMN2
            // namespaced magic (0x4C 0x4D 0x4E 0x32) are the LMN wire identity
            // (LMN ModRouter.cs:13-24). Everything else must pass through.
            Assert(BetterUnturnedExperience.Core.Network.LmnFrameClassifier.IsLmnFrame(
                new byte[] { 0x4D, 0x4F, 0x44, 0x01, 0xAA }),
                "takeover: MOD legacy frame is classified as LMN");
            Assert(BetterUnturnedExperience.Core.Network.LmnFrameClassifier.IsLmnFrame(
                new byte[] { 0x4C, 0x4D, 0x4E, 0x32, 0x01, 0xBB }),
                "takeover: LMN2 namespaced frame is classified as LMN");
            Assert(!BetterUnturnedExperience.Core.Network.LmnFrameClassifier.IsLmnFrame(
                new byte[] { 0x00, 0x01, 0x02, 0x03 }),
                "takeover: non-LMN frame is not classified");
            Assert(!BetterUnturnedExperience.Core.Network.LmnFrameClassifier.IsLmnFrame(
                new byte[] { 0x4D, 0x4F }),
                "takeover: truncated frame is not classified");

            // Takeover gating: active only when standalone LMN is actually
            // loaded; short-circuits LMN frames only while active.
            var lmnLoaded = false;
            var coordinator = new BetterUnturnedExperience.Core.Network.LmnTakeoverCoordinator(
                () => lmnLoaded);
            Assert(!coordinator.TakeoverActive,
                "takeover: not active before refresh (default)");
            coordinator.Refresh();
            Assert(!coordinator.TakeoverActive,
                "takeover: not active when standalone LMN is absent (no false positive)");
            Assert(!coordinator.ShouldShortCircuit(new byte[] { 0x4D, 0x4F, 0x44, 0x01 }),
                "takeover: LMN frame passes through when takeover inactive (no short-circuit)");

            lmnLoaded = true;
            coordinator.Refresh();
            Assert(coordinator.TakeoverActive,
                "takeover: active when standalone LMN is loaded");
            Assert(coordinator.ShouldShortCircuit(new byte[] { 0x4D, 0x4F, 0x44, 0x01, 0xAA }),
                "takeover: MOD frame short-circuits when active");
            Assert(coordinator.ShouldShortCircuit(new byte[] { 0x4C, 0x4D, 0x4E, 0x32, 0x01 }),
                "takeover: LMN2 frame short-circuits when active");
            Assert(!coordinator.ShouldShortCircuit(new byte[] { 0x00, 0x01, 0x02, 0x03 }),
                "takeover: non-LMN frame passes through even when active");
        }

        // GPT watermark: DEV-V2-05 red regression. The V1 numeric-channel
        // compatibility path (T4): legacy plugins speak int virtual channels
        // over the "MOD" magic frame (["MOD" 3x][channel:1byte][payload] —
        // LMN ModRouter.cs:13-16 and 40-51; handlers keyed by int channel,
        // ModTransport.cs:137,159; range validation ModTransport.cs:695-704).
        // BUE's compat layer is Host-internal (never in Contracts): parse the
        // channel byte, route frames into a registry that mimics the V1
        // registration semantics, honour the official on/off switch
        // (independent from the network module switch), and isolate any fault
        // to the single V1 frame with a diagnostic. RED until LmnV1FrameCodec
        // / LmnV1CompatRegistry / LmnV1CompatLayer exist (CS0234).
        private static void AssertBueV1Compat()
        {
            // Wire format: ["MOD" 3x][channel:1byte][payload]. The codec owns
            // V1 parse/build only — LMN2 (V2 namespaced) frames are never V1.
            byte[] modFrame = { 0x4D, 0x4F, 0x44, 0x67, 0x0A, 0x0B };
            Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.IsV1Frame(modFrame),
                "v1compat: MOD magic frame is a V1 frame");
            Assert(!BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.IsV1Frame(
                    new byte[] { 0x4C, 0x4D, 0x4E, 0x32, 0x01 }),
                "v1compat: LMN2 namespaced frame is not a V1 frame");
            Assert(!BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.IsV1Frame(null),
                "v1compat: null frame is not a V1 frame");
            Assert(!BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.IsV1Frame(new byte[] { 0x4D, 0x4F }),
                "v1compat: truncated frame is not a V1 frame");
            Assert(!BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.IsV1Frame(new byte[] { 0x4D, 0x4F, 0x44 }),
                "v1compat: bare magic without a channel byte is not a routable V1 frame");

            int channel;
            byte[] payload;
            Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryParse(modFrame, out channel, out payload),
                "v1compat: MOD frame parses");
            Assert(channel == 0x67,
                "v1compat: the first byte after the magic is the int channel (0..255)");
            Assert(payload.Length == 2 && payload[0] == 0x0A && payload[1] == 0x0B,
                "v1compat: the payload is the frame remainder after magic and channel");
            Assert(!BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryParse(
                    new byte[] { 0x4D, 0x4F, 0x44 }, out channel, out payload),
                "v1compat: magic without a channel byte does not parse");
            Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryParse(
                    new byte[] { 0x4D, 0x4F, 0x44, 0x00 }, out channel, out payload),
                "v1compat: channel 0 with an empty payload parses");
            Assert(channel == 0 && payload.Length == 0,
                "v1compat: channel 0 boundary keeps an empty payload");
            Assert(!BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryParse(null, out channel, out payload),
                "v1compat: null frame does not parse");

            byte[] built;
            Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(103, new byte[] { 0x01, 0x02 }, out built),
                "v1compat: an outgoing V1 frame builds for a legacy channel");
            Assert(built.Length == 6 && built[0] == 0x4D && built[1] == 0x4F && built[2] == 0x44
                && built[3] == 103 && built[4] == 0x01 && built[5] == 0x02,
                "v1compat: built frame is byte-exact [MOD 3x][channel][payload] (LMN BuildModPacket shape)");
            Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(0, null, out built)
                && built.Length == 4 && built[3] == 0,
                "v1compat: null payload builds as an empty payload");
            Assert(!BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(256, new byte[0], out built),
                "v1compat: channel above 255 is rejected (legacy range validation, ModTransport.cs:695-704)");
            Assert(!BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(-1, new byte[0], out built),
                "v1compat: negative channel is rejected");

            // Registry: mimics the V1 registration semantics — server handlers
            // keyed by int channel receive the sender's 64-bit steam id plus a
            // reader over the payload, client handlers receive the reader
            // (ModTransport.cs:137,159,186,216). The 64-bit steam id is carried
            // in its ulong form so the Core stays pure C#.
            var registry = new BetterUnturnedExperience.Core.Network.LmnV1CompatRegistry();
            ulong sender = 76561198000000123UL;
            ulong gotSender = 0;
            byte[] gotPayload = null;
            registry.RegisterServerHandler(103, (fromId, reader) =>
            {
                gotSender = fromId;
                gotPayload = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            });
            Assert(registry.DispatchServer(103, sender, new byte[] { 0x0A, 0x0B }),
                "v1compat: a registered server handler receives its channel's payload");
            Assert(gotSender == sender && gotPayload != null && gotPayload.Length == 2 && gotPayload[0] == 0x0A,
                "v1compat: the handler sees the sender's 64-bit steam id and the exact payload");
            Assert(!registry.DispatchServer(100, sender, new byte[] { 0x0A }),
                "v1compat: an unregistered channel reports not-handled");

            int clientCalls = 0;
            registry.RegisterClientHandler(103, reader => { clientCalls++; reader.ReadByte(); });
            Assert(registry.DispatchClient(103, new byte[] { 0x01 }),
                "v1compat: a registered client handler receives its channel's payload");
            Assert(clientCalls == 1, "v1compat: the client handler is invoked exactly once");

            registry.UnregisterServerHandler(103);
            registry.UnregisterClientHandler(103);
            Assert(!registry.DispatchServer(103, sender, new byte[] { 0x0A }),
                "v1compat: unregister removes the server handler");
            Assert(!registry.DispatchClient(103, new byte[] { 0x01 }),
                "v1compat: unregister removes the client handler");
            // Idempotence: unregistering an absent handler must not throw.
            registry.UnregisterServerHandler(103);

            int lastWins = 0;
            registry.RegisterServerHandler(104, (fromId, reader) => { lastWins = 1; });
            registry.RegisterServerHandler(104, (fromId, reader) => { lastWins = 2; });
            Assert(registry.DispatchServer(104, sender, new byte[0]) && lastWins == 2,
                "v1compat: re-registering a channel replaces the previous handler (V1 table semantics)");

            bool rangeRejected = false;
            try { registry.RegisterServerHandler(256, (fromId, reader) => { }); }
            catch (ArgumentOutOfRangeException) { rangeRejected = true; }
            Assert(rangeRejected,
                "v1compat: registration validates the 0..255 legacy channel range");

            // Official switch: V1 compat is an official feature the player can
            // turn off (independent from the network module switch). Off hands
            // the frame back unconsumed; on consumes V1 frames only — V2 and
            // vanilla traffic are never touched by this layer.
            var layer = new BetterUnturnedExperience.Core.Network.LmnV1CompatLayer(registry);
            Assert(layer.Enabled, "v1compat: the official switch defaults to enabled");
            Assert(layer.Registry == registry, "v1compat: the layer routes through the injected registry");

            Assert(!layer.RouteFromClient(new byte[] { 0x4C, 0x4D, 0x4E, 0x32, 0x01 }, sender),
                "v1compat: the layer never consumes LMN2 (V2) frames");
            Assert(!layer.RouteFromClient(new byte[] { 0x00, 0x01 }, sender),
                "v1compat: the layer never consumes non-LMN frames");

            byte[] v1Frame;
            Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(103, new byte[] { 0x0A }, out v1Frame),
                "setup: the V1 frame for the routing checks builds");

            layer.Enabled = false;
            Assert(!layer.RouteFromClient(v1Frame, sender),
                "v1compat: switch off hands the V1 frame back unconsumed (vanilla keeps it)");
            Assert(!layer.TryBuildOutgoingFrame(103, new byte[] { 0x0A }, out built),
                "v1compat: switch off refuses outgoing V1 frames");

            layer.Enabled = true;
            byte[] received = null;
            registry.RegisterServerHandler(103, (fromId, reader) =>
            {
                received = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            });
            Assert(layer.RouteFromClient(v1Frame, sender),
                "v1compat: switch on consumes the V1 frame into the compat registry");
            Assert(received != null && received.Length == 1 && received[0] == 0x0A,
                "v1compat: the routed frame reaches the handler with its payload intact");

            int clientGot = 0;
            registry.RegisterClientHandler(101, reader => { clientGot += reader.ReadByte(); });
            byte[] fromServerFrame;
            Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(101, new byte[] { 0x07 }, out fromServerFrame),
                "setup: the client-side inbound frame builds");
            Assert(layer.RouteFromServer(fromServerFrame) && clientGot == 7,
                "v1compat: client-side receive routes to client handlers");
            Assert(layer.TryBuildOutgoingFrame(103, new byte[] { 0x0A }, out built) && built[3] == 103,
                "v1compat: switch on builds outgoing V1 frames with the channel byte");

            // Fault isolation: a handler fault must never propagate — the
            // frame is dropped with a diagnostic and the layer stays healthy.
            var diagnostics = new List<string>();
            var previousSink = BetterUnturnedExperience.Core.Network.LmnV1CompatLayer.DiagnosticLogSink;
            BetterUnturnedExperience.Core.Network.LmnV1CompatLayer.DiagnosticLogSink = line => diagnostics.Add(line);
            try
            {
                registry.RegisterServerHandler(105, (fromId, reader) => { throw new InvalidOperationException("v1-compat-handler-fault"); });
                byte[] faultFrame;
                Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(105, new byte[] { 0x0B }, out faultFrame),
                    "setup: the fault-injection frame builds");
                Assert(layer.RouteFromClient(faultFrame, sender),
                    "v1compat: a throwing handler still consumes the frame — the fault never propagates to V2 or vanilla");
                Assert(diagnostics.Count > 0 && diagnostics[0].Contains("BUE-V1COMPAT-001"),
                    "v1compat: a handler fault emits a diagnostic carrying the compat diagnosticId");

                received = null;
                Assert(layer.RouteFromClient(v1Frame, sender) && received != null,
                    "v1compat: the layer stays healthy after a handler fault (isolation is per-frame)");

                byte[] unknownFrame;
                Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(200, new byte[] { 0x0C }, out unknownFrame),
                    "setup: the unknown-channel frame builds");
                Assert(layer.RouteFromClient(unknownFrame, sender),
                    "v1compat: an enabled layer consumes a V1 frame with no registered channel (drop, never leak)");
                Assert(diagnostics.Count > 1,
                    "v1compat: an unknown-channel drop emits a diagnostic too");
            }
            finally
            {
                BetterUnturnedExperience.Core.Network.LmnV1CompatLayer.DiagnosticLogSink = previousSink;
            }

            // Acceptance fixture (decision record 2026-09-03): a no-op plugin
            // compiled against the LMN V1 numeric-channel API surface (int
            // channel register / int channel send) must keep working through
            // the compat layer without code changes. The LMN sources live
            // outside this repository (the T8 research is the authority), so
            // the fixture replays the V1 call shape host-side.
            var fixture = new LmnV1NoOpPluginFixture(103);
            fixture.Attach(layer);
            byte[] outgoing = fixture.SendToServer(new byte[] { 0x11, 0x22 });
            Assert(outgoing != null && outgoing.Length == 6 && outgoing[0] == 0x4D
                && outgoing[1] == 0x4F && outgoing[2] == 0x44 && outgoing[3] == 103
                && outgoing[4] == 0x11 && outgoing[5] == 0x22,
                "fixture: the old plugin's send leaves as a legacy MOD frame carrying the int channel byte");
            byte[] fixtureInbound;
            Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(103, new byte[] { 0x33 }, out fixtureInbound),
                "setup: the fixture inbound frame builds");
            Assert(layer.RouteFromClient(fixtureInbound, sender),
                "fixture: an inbound legacy frame routes to the old plugin's registration");
            Assert(fixture.Received != null && fixture.Received.Length == 1 && fixture.Received[0] == 0x33
                && fixture.LastSender == sender,
                "fixture: the old plugin receives sender + payload intact, with no code changes");
        }

        // Host-side stand-in for a legacy V1 consumer: it registers by int
        // virtual channel and sends by int virtual channel (the LMN V1 API
        // shape, ModTransport.cs:137,415) — the exact contract the compat
        // layer must keep alive for old binaries.
        private sealed class LmnV1NoOpPluginFixture
        {
            private readonly int virtualChannel;
            private BetterUnturnedExperience.Core.Network.LmnV1CompatLayer layer;
            private byte[] received;
            private ulong lastSender;

            internal LmnV1NoOpPluginFixture(int virtualChannel) { this.virtualChannel = virtualChannel; }

            internal byte[] Received { get { return received; } }
            internal ulong LastSender { get { return lastSender; } }

            internal void Attach(BetterUnturnedExperience.Core.Network.LmnV1CompatLayer compatLayer)
            {
                layer = compatLayer;
                layer.Registry.RegisterServerHandler(virtualChannel, (fromId, reader) =>
                {
                    lastSender = fromId;
                    received = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                });
            }

            internal byte[] SendToServer(byte[] payload)
            {
                byte[] frame;
                return layer.TryBuildOutgoingFrame(virtualChannel, payload, out frame) ? frame : null;
            }
        }

        // GPT watermark: DEV-V2-06 red regression. The takeover wiring +
        // config migration ticket: the network module and its V1 compat
        // switch become official Settings Facets persisted atomically through
        // FileSettingsPersistence, the empty LMN config migration is recorded
        // as a structured no-op diagnostic (T6: LMN V5 has no config
        // system), the standalone LMN V1 handler table is mirrored via
        // reflection so legacy frames keep flowing, LMN2 frames delegate to
        // LMN's own router (BUE stays the only patch decision point), the
        // network module switch is reversible (off hands everything back),
        // the frame format v2 carries the sender's steam id so dispatch
        // resolves the session by source (never "first session") with the
        // reliability bit passed through, and a routing settings editor lets
        // the panel submit network facet edits without touching the BII
        // editor. RED until NetworkModuleAdapter /
        // HostNetworkTransportAdapter / SettingsRuntimeBueEditor /
        // RoutingBueSettingsEditor exist (CS0234).
        private static void AssertBueConfigMigration()
        {
            // 1. Dual-facet persistence roundtrip: both official switches
            //    survive a FileSettingsPersistence atomic commit + reload.
            var persistenceRoot = Path.Combine(Path.GetTempPath(), "bue-v2net-red-" + Guid.NewGuid().ToString("N"));
            var networkDescriptors = NetworkModuleAdapter.CreateNetworkDescriptors();
            var v1CompatDescriptors = NetworkModuleAdapter.CreateV1CompatDescriptors();
            Assert(networkDescriptors.Count == 1 && v1CompatDescriptors.Count == 1,
                "migration: each network facet declares exactly one switch descriptor");
            Assert(networkDescriptors[0].SettingId == "network.enabled" && v1CompatDescriptors[0].SettingId == "v1compat.enabled",
                "migration: the facet switches keep their frozen setting ids");
            Assert(networkDescriptors[0].Kind == SettingKind.Toggle && networkDescriptors[0].Authority == SettingAuthority.ClientLocal
                && networkDescriptors[0].SchemaVersion == 1,
                "migration: the network descriptor is a schema-1 client-local toggle");
            var networkRuntime = new SettingsRuntime(NetworkModuleAdapter.NetworkFeature, networkDescriptors, new FileSettingsPersistence(persistenceRoot));
            var v1Runtime = new SettingsRuntime(NetworkModuleAdapter.V1CompatFeature, v1CompatDescriptors, new FileSettingsPersistence(persistenceRoot));
            Assert(networkRuntime.Submit(new ScopedSettingChangeRequest(11UL, SettingRevisionScope.ClientPreference,
                networkRuntime.GetSnapshot(SettingRevisionScope.ClientPreference).Revision,
                new[] { new SettingMutation("network.enabled", SettingValue.Toggle(false)) })).Accepted,
                "migration: the network facet submits an atomic toggle change");
            Assert(v1Runtime.Submit(new ScopedSettingChangeRequest(12UL, SettingRevisionScope.ClientPreference,
                v1Runtime.GetSnapshot(SettingRevisionScope.ClientPreference).Revision,
                new[] { new SettingMutation("v1compat.enabled", SettingValue.Toggle(false)) })).Accepted,
                "migration: the V1 compat facet submits an atomic toggle change");
            SettingValue persisted;
            uint persistedRevision;
            var reloadedNetwork = new SettingsRuntime(NetworkModuleAdapter.NetworkFeature, networkDescriptors, new FileSettingsPersistence(persistenceRoot));
            Assert(reloadedNetwork.TryGet("network.enabled", out persisted, out persistedRevision) && !persisted.Boolean,
                "migration: the network switch survives a FileSettingsPersistence roundtrip");
            var reloadedV1 = new SettingsRuntime(NetworkModuleAdapter.V1CompatFeature, v1CompatDescriptors, new FileSettingsPersistence(persistenceRoot));
            Assert(reloadedV1.TryGet("v1compat.enabled", out persisted, out persistedRevision) && !persisted.Boolean,
                "migration: the V1 compat switch survives the same roundtrip");

            // 2.+3. Adapter at probe false: own facets, panel status lines,
            //        the structured no-op migration record, zero false positives.
            var diagnostics = new List<string>();
            var previousSink = NetworkModuleAdapter.DiagnosticLogSink;
            NetworkModuleAdapter.DiagnosticLogSink = line => diagnostics.Add(line);
            byte[] modFrame = { 0x4D, 0x4F, 0x44, 0x67, 0x0A };
            byte[] lmn2Frame = { 0x4C, 0x4D, 0x4E, 0x32, 0x01, 0xBB };
            try
            {
                var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2net-red-" + Guid.NewGuid().ToString("N"));
                var dormant = new NetworkModuleAdapter(adapterRoot, () => false, () => null, () => null, () => { });
                dormant.ActivateCore();
                Assert(dormant.NetworkSettings.Feature.Value == NetworkModuleAdapter.NetworkFeature.Value
                    && dormant.V1CompatSettings.Feature.Value == NetworkModuleAdapter.V1CompatFeature.Value,
                    "migration: the adapter owns one settings runtime per official facet");
                Assert(dormant.NetworkSettings.GetSnapshot(SettingRevisionScope.ClientPreference).Entries.Count == 1
                    && dormant.V1CompatSettings.GetSnapshot(SettingRevisionScope.ClientPreference).Entries.Count == 1,
                    "migration: each facet exposes exactly its own switch (no migrated entries exist)");
                Assert(!string.IsNullOrEmpty(dormant.TakeoverStatus),
                    "migration: the takeover status line is always present for the panel");
                Assert(dormant.ConfigMigrationStatus.Contains("无独立配置可迁移"),
                    "migration: the panel line reports the LMN no-config no-op");
                Assert(diagnostics.Exists(line => line.Contains("BUE-V2NET-001") && line.Contains("result=no-op")),
                    "migration: the empty migration is recorded with the structured diagnostic (BUE-V2NET-001)");
                Assert(!dormant.TakeoverActive,
                    "migration: the takeover stays inactive when standalone LMN is absent (no false positive)");
                Assert(!dormant.ShouldConsumeInbound(true, 1UL, modFrame, 0, modFrame.Length, null),
                    "migration: legacy V1 frames pass through while the takeover is inactive");
                Assert(!dormant.ShouldConsumeInbound(false, 0UL, lmn2Frame, 0, lmn2Frame.Length, null),
                    "migration: LMN2 frames pass through while the takeover is inactive");
                Assert(!dormant.ShouldConsumeInbound(true, 1UL, new byte[] { 0x00, 0x01 }, 0, 2, null),
                    "migration: vanilla frames always pass through (zero false positive)");

                // 4.-7. Probe true: the V1 table mirrors from the standalone
                //        LMN process, LMN2 delegates to LMN's own router, the
                //        v1compat switch gates only the legacy path, a failed
                //        mirror degrades to the drop path, and the network
                //        module switch is the reversible kill switch.
                FakeLmnModTransport.Reset();
                FakeLmnModRouter.Reset();
                FakeLmnModTransport.ServerHandlers[103] = (sender, reader) =>
                {
                    FakeLmnModTransport.LastSender = sender.Value;
                    FakeLmnModTransport.LastPayload = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                };
                FakeLmnModTransport.ClientHandlers[103] = reader => { FakeLmnModTransport.ClientCalls++; };
                var takeover = new NetworkModuleAdapter(adapterRoot, () => true, () => typeof(FakeLmnModTransport), () => typeof(FakeLmnModRouter), () => { });
                takeover.ActivateCore();
                Assert(takeover.TakeoverActive, "takeover: the decision core arms when standalone LMN is present");
                Assert(takeover.TakeoverStatus.Contains("已由 BUE 接管"),
                    "takeover: the panel status reports the takeover");
                byte[] v1Frame;
                Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(103, new byte[] { 0x11, 0x22 }, out v1Frame),
                    "setup: the V1 mirror frame builds");
                Assert(takeover.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: the mirrored V1 table consumes the legacy frame");
                Assert(FakeLmnModTransport.LastSender == 424242UL && FakeLmnModTransport.LastPayload != null
                    && FakeLmnModTransport.LastPayload.Length == 2 && FakeLmnModTransport.LastPayload[0] == 0x11,
                    "takeover: the legacy handler receives the steam id (ulong converted) and payload intact");
                Assert(takeover.ShouldConsumeInbound(false, 0UL, v1Frame, 0, v1Frame.Length, null) && FakeLmnModTransport.ClientCalls == 1,
                    "takeover: the client-side receive routes to the mirrored client handler");

                Assert(takeover.V1CompatSettings.Submit(new ScopedSettingChangeRequest(21UL, SettingRevisionScope.ClientPreference,
                    takeover.V1CompatSettings.GetSnapshot(SettingRevisionScope.ClientPreference).Revision,
                    new[] { new SettingMutation("v1compat.enabled", SettingValue.Toggle(false)) })).Accepted,
                    "setup: the V1 compat switch is submitted through its facet runtime");
                takeover.RefreshSwitches();
                Assert(!takeover.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: v1compat off hands legacy frames back unconsumed");
                Assert(takeover.V1CompatSettings.Submit(new ScopedSettingChangeRequest(22UL, SettingRevisionScope.ClientPreference,
                    takeover.V1CompatSettings.GetSnapshot(SettingRevisionScope.ClientPreference).Revision,
                    new[] { new SettingMutation("v1compat.enabled", SettingValue.Toggle(true)) })).Accepted,
                    "setup: the V1 compat switch is re-enabled");
                takeover.RefreshSwitches();
                Assert(takeover.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: v1compat on restores the legacy consumption path");

                FakeLmnModRouter.NextResult = true;
                Assert(takeover.ShouldConsumeInbound(true, 0UL, lmn2Frame, 0, lmn2Frame.Length, null)
                    && FakeLmnModRouter.ClientCalls == 1 && FakeLmnModRouter.LastPacket == lmn2Frame
                    && FakeLmnModRouter.LastOffset == 0 && FakeLmnModRouter.LastSize == lmn2Frame.Length,
                    "takeover: LMN2 frames delegate to LMN's own router with the packet window untouched");
                // DEV-V2-11 (Spec GAP-1): LMN logs nothing per frame, so a
                // delegated frame is indistinguishable from LMN's own prefix
                // path — the delegate emits a one-shot record on its first
                // consumed frame so the real-machine retest can prove the
                // delegation actually happens.
                Assert(CountToken(diagnostics, "event=lmn2-delegate result=delegated") == 1,
                    "takeover: the first consumed LMN2 delegation emits exactly one delegated record");
                Assert(takeover.ShouldConsumeInbound(false, 0UL, lmn2Frame, 0, lmn2Frame.Length, null) && FakeLmnModRouter.ServerCalls == 1,
                    "takeover: the server-direction router delegate is invoked for ReceiveMessageFromServer frames");
                Assert(CountToken(diagnostics, "event=lmn2-delegate result=delegated") == 1,
                    "takeover: the delegated record stays one-shot across further delegations");
                FakeLmnModRouter.NextResult = false;
                Assert(!takeover.ShouldConsumeInbound(true, 0UL, lmn2Frame, 0, lmn2Frame.Length, null),
                    "takeover: an unhandled LMN2 frame passes through (LMN's prefix keeps its self-heal path)");
                Assert(CountToken(diagnostics, "event=lmn2-delegate result=delegated") == 1,
                    "takeover: an unhandled LMN2 pass-through emits no delegated record");

                var broken = new NetworkModuleAdapter(adapterRoot, () => true, () => null, () => typeof(FakeLmnModRouter), () => { });
                broken.ActivateCore();
                // DEV-V2-10 F-A: an unresolvable LMN type is a deferral now —
                // the mirror diagnostic (BUE-V2NET-002) is emitted once with
                // result=deferred, never result=failed (P5 zero false positive).
                Assert(diagnostics.Exists(line => line.Contains("BUE-V2NET-002") && line.Contains("result=deferred")),
                    "takeover: an unavailable handler-table mirror defers with the mirror diagnostic (BUE-V2NET-002)");
                Assert(broken.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: with no mirrored table the legacy frame is still consumed (unknown-channel drop, never a crash)");

                Assert(takeover.NetworkSettings.Submit(new ScopedSettingChangeRequest(31UL, SettingRevisionScope.ClientPreference,
                    takeover.NetworkSettings.GetSnapshot(SettingRevisionScope.ClientPreference).Revision,
                    new[] { new SettingMutation("network.enabled", SettingValue.Toggle(false)) })).Accepted,
                    "setup: the network module switch is turned off");
                takeover.RefreshSwitches();
                Assert(!takeover.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "recovery: network module off hands every legacy frame back (LMN resumes standalone)");
                Assert(!takeover.ShouldConsumeInbound(false, 0UL, lmn2Frame, 0, lmn2Frame.Length, null),
                    "recovery: network module off passes LMN2 frames through");
                Assert(takeover.NetworkSettings.Submit(new ScopedSettingChangeRequest(32UL, SettingRevisionScope.ClientPreference,
                    takeover.NetworkSettings.GetSnapshot(SettingRevisionScope.ClientPreference).Revision,
                    new[] { new SettingMutation("network.enabled", SettingValue.Toggle(true)) })).Accepted,
                    "setup: the network module switch is re-enabled");
                takeover.RefreshSwitches();
                FakeLmnModRouter.NextResult = true;
                Assert(takeover.ShouldConsumeInbound(false, 0UL, lmn2Frame, 0, lmn2Frame.Length, null),
                    "recovery: re-enabling the network module re-arms the takeover (reversible switch)");
            }
            finally
            {
                NetworkModuleAdapter.DiagnosticLogSink = previousSink;
            }

            // 8. Frame format v2 over the real-transport seam: three runtimes
            //    on a hub topology prove sender-carried source dispatch, the
            //    reliability bit, and targeted sends.
            System.Action<byte[]> hubReceiver = null;
            System.Action<byte[]> peerBReceiver = null;
            System.Action<byte[]> peerCReceiver = null;
            var hubLastReliable = false;
            var hubLastTarget = 0UL;
            var transportHub = new BetterUnturnedExperience.Core.Network.HostNetworkTransportAdapter((frame, reliable, target) =>
            {
                hubLastReliable = reliable;
                hubLastTarget = target;
                if (target == 0UL)
                {
                    if (peerBReceiver != null) peerBReceiver(frame);
                    if (peerCReceiver != null) peerCReceiver(frame);
                }
                else if (target == 200UL && peerBReceiver != null) peerBReceiver(frame);
                else if (target == 300UL && peerCReceiver != null) peerCReceiver(frame);
                return true;
            }, callback => hubReceiver = callback);
            var transportB = new BetterUnturnedExperience.Core.Network.HostNetworkTransportAdapter(
                (frame, reliable, target) => { if (hubReceiver != null) hubReceiver(frame); return true; },
                callback => peerBReceiver = callback);
            var transportC = new BetterUnturnedExperience.Core.Network.HostNetworkTransportAdapter(
                (frame, reliable, target) => { if (hubReceiver != null) hubReceiver(frame); return true; },
                callback => peerCReceiver = callback);
            var trioContract = new ContractVersion(2, 0);
            var runtimeHub = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(transportHub, trioContract, 100UL);
            var runtimePeerB = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(transportB, trioContract, 200UL);
            var runtimePeerC = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(transportC, trioContract, 300UL);
            var trioChannel = new FeatureId("io.example.v2net");
            Assert(runtimeHub.RegisterChannel(trioChannel, trioContract, 1).Accepted
                && runtimePeerB.RegisterChannel(trioChannel, trioContract, 1).Accepted
                && runtimePeerC.RegisterChannel(trioChannel, trioContract, 1).Accepted,
                "setup: all three trio runtimes register the channel");
            IConnectionSession lastContext = null;
            byte[] lastPayload = null;
            // DEV-V2-14: the hub answered both handshakes — its inbound frames
            // come FROM CLIENTS; the peers' inbound frames come FROM SERVER.
            runtimeHub.Subscribe(trioChannel, ChannelDirection.FromClients, (session, payload) => { lastContext = session; lastPayload = payload; });
            runtimePeerB.StartSession(100UL);
            runtimePeerC.StartSession(100UL);
            transportB.Pump();
            transportC.Pump();
            transportHub.Pump();
            transportHub.Pump();
            transportB.Pump();
            transportC.Pump();
            Assert(runtimeHub.Sessions.Count == 2,
                "frame v2: the hub holds one session per peer after both handshakes");
            var hubSessionB = runtimeHub.Sessions[0].PeerSteamId == 200UL ? runtimeHub.Sessions[0] : runtimeHub.Sessions[1];
            Assert(runtimePeerB.SendToServer(trioChannel, new byte[] { 0x0B }, true) == NetworkSendResult.Sent,
                "setup: the first peer sends to the hub");
            transportHub.Pump();
            Assert(lastContext != null && lastContext.PeerSteamId == 200UL && lastPayload != null && lastPayload[0] == 0x0B,
                "frame v2: dispatch resolves the session by the frame's sender field (source-addressed, never first-session)");
            Assert(runtimePeerC.SendToServer(trioChannel, new byte[] { 0x0C }, true) == NetworkSendResult.Sent,
                "setup: the second peer sends to the hub");
            transportHub.Pump();
            Assert(lastContext != null && lastContext.PeerSteamId == 300UL,
                "frame v2: the second peer's frame resolves to its own session (first-session shortcut falsified)");
            var hubToB = 0;
            var hubToC = 0;
            runtimePeerB.Subscribe(trioChannel, ChannelDirection.FromServer, (session, payload) => hubToB++);
            runtimePeerC.Subscribe(trioChannel, ChannelDirection.FromServer, (session, payload) => hubToC++);
            Assert(runtimeHub.SendToClients(trioChannel, new byte[] { 0x1F }, true) == NetworkSendResult.Sent,
                "setup: the hub broadcasts");
            Assert(hubLastReliable, "frame v2: SendToClients forwards the reliability bit to the transport seam");
            transportB.Pump();
            transportC.Pump();
            Assert(hubToB == 1 && hubToC == 1, "frame v2: an untargeted server broadcast reaches both peers");
            Assert(runtimeHub.SendToClient(trioChannel, hubSessionB, new byte[] { 0x2F }, false) == NetworkSendResult.Sent,
                "setup: the hub targets the first peer");
            Assert(hubLastTarget == 200UL && !hubLastReliable,
                "frame v2: SendToClient targets the session's peer steam id and honors the unreliable flag");
            transportB.Pump();
            transportC.Pump();
            Assert(hubToB == 2 && hubToC == 1, "frame v2: the targeted send reaches only the addressed session's peer");

            // 9. Routing settings editor: the network facet submits through
            //    its own runtime; every other feature falls back to the BII
            //    editor.
            var routingRoot = Path.Combine(Path.GetTempPath(), "bue-v2net-red-" + Guid.NewGuid().ToString("N"));
            var routedRuntime = new SettingsRuntime(NetworkModuleAdapter.NetworkFeature, NetworkModuleAdapter.CreateNetworkDescriptors(), new FileSettingsPersistence(routingRoot));
            var routedEditor = new SettingsRuntimeBueEditor(routedRuntime);
            var fallbackCalls = 0;
            var routing = new RoutingBueSettingsEditor(new CountingSettingsEditor(() => fallbackCalls++),
                (NetworkModuleAdapter.NetworkFeature, routedEditor));
            var routedApply = routing.Apply(NetworkModuleAdapter.NetworkFeature,
                routing.GetSnapshot(NetworkModuleAdapter.NetworkFeature).Revision,
                new SettingMutation("network.enabled", SettingValue.Toggle(false)));
            Assert(routedApply.Accepted && fallbackCalls == 0,
                "editor routing: the network facet submits through its own SettingsRuntime editor");
            Assert(routedRuntime.TryGet("network.enabled", out persisted, out persistedRevision) && !persisted.Boolean,
                "editor routing: the routed edit persisted through the facet runtime");
            Assert(!routing.Apply(new FeatureId("com.example.unrelated"), 0, new SettingMutation("anything", SettingValue.Toggle(true))).Accepted
                && fallbackCalls == 1,
                "editor routing: an unmatched feature falls back to the BII editor");
        }

        // Host-side stand-in for the Steamworks CSteamID value the LMN V1
        // handler table is keyed by: the mirror constructs the first handler
        // parameter via a public ulong constructor (production passes
        // Steamworks.CSteamID, tests pass this shape).
        private sealed class FakeSteamId
        {
            public FakeSteamId(ulong value) { Value = value; }
            public ulong Value { get; }
        }

        // Host-side stand-in for LMN's ModTransport static handler tables:
        // same field names ("ServerHandlers"/"ClientHandlers") and the same
        // (steamId, BinaryReader) / (BinaryReader) delegate shapes, with the
        // engine steam id replaced by FakeSteamId.
        private static class FakeLmnModTransport
        {
            internal static readonly Dictionary<int, System.Action<FakeSteamId, BinaryReader>> ServerHandlers =
                new Dictionary<int, System.Action<FakeSteamId, BinaryReader>>();
            internal static readonly Dictionary<int, System.Action<BinaryReader>> ClientHandlers =
                new Dictionary<int, System.Action<BinaryReader>>();
            internal static ulong LastSender;
            internal static byte[] LastPayload;
            internal static int ClientCalls;

            internal static void Reset()
            {
                ServerHandlers.Clear();
                ClientHandlers.Clear();
                LastSender = 0UL;
                LastPayload = null;
                ClientCalls = 0;
            }
        }

        // Host-side stand-in for LMN's ModRouter reflection target: the same
        // static TryHandleFromClient/TryHandleFromServer member names and
        // shapes (the engine ITransportConnection first parameter widens to
        // object under reflection invoke).
        private static class FakeLmnModRouter
        {
            internal static bool NextResult;
            internal static int ClientCalls;
            internal static int ServerCalls;
            internal static object LastConnection;
            internal static byte[] LastPacket;
            internal static int LastOffset;
            internal static int LastSize;

            internal static void Reset()
            {
                NextResult = false;
                ClientCalls = 0;
                ServerCalls = 0;
                LastConnection = null;
                LastPacket = null;
                LastOffset = 0;
                LastSize = 0;
            }

            internal static bool TryHandleFromClient(object connection, byte[] packet, int offset, int size)
            {
                ClientCalls++;
                LastConnection = connection;
                LastPacket = packet;
                LastOffset = offset;
                LastSize = size;
                return NextResult;
            }

            internal static bool TryHandleFromServer(byte[] packet, int offset, int size)
            {
                ServerCalls++;
                LastPacket = packet;
                LastOffset = offset;
                LastSize = size;
                return NextResult;
            }
        }

        // DEV-V2-12: host-side ITransportConnection stand-in — only the steam
        // id resolution behavior is parameterized (mirrors the SDK shape the
        // production resolver reflects against); everything else is inert.
        private sealed class FakeTransportConnection : SDG.NetTransport.ITransportConnection
        {
            private readonly ulong steamId;
            private readonly bool resolves;

            internal FakeTransportConnection(ulong steamId, bool resolves)
            {
                this.steamId = steamId;
                this.resolves = resolves;
            }

            public bool TryGetSteamId(out ulong steamId)
            {
                steamId = this.steamId;
                return this.resolves;
            }

            public bool TryGetIPv4Address(out uint address) { address = 0U; return false; }
            public bool TryGetPort(out ushort port) { port = 0; return false; }
            public System.Net.IPAddress GetAddress() { return null; }
            public string GetAddressString(bool withPort) { return string.Empty; }
            public void CloseConnection() { }
            public void Send(byte[] buffer, long size, SDG.NetTransport.ENetReliability reliability) { }
            public bool Equals(SDG.NetTransport.ITransportConnection other) { return ReferenceEquals(this, other); }
            public override bool Equals(object obj) { return ReferenceEquals(this, obj); }
            public override int GetHashCode() { return steamId.GetHashCode(); }
        }

        // GPT watermark: DEV-V2-10 red regression (F-A, real-machine audit
        // configB-verification-r1). BepInEx loads plugins by file-name order,
        // so BUE (B) bootstraps BEFORE the standalone LMN (L) assembly is
        // loaded and its ModTransport handler tables exist — and the legacy
        // plugins register their channels in their own Awake, after LMN's.
        // The bootstrap mirror used to emit result=failed
        // errorType=ArgumentException — one "BUE 错误" line per session (P5
        // violation) and a permanently dead mirror. The anchor replays the
        // real timeline through the PRODUCTION log route
        // (BindProductionLog + BueRuntimeLog): chainloader manifest lists
        // LMN while its assembly is missing (deferred) → LMN's assembly
        // loads but its tables are still EMPTY (the retry stays armed) →
        // the legacy plugin registers channel 250 late (the deferred retry
        // completes the mirror) — every step below the Error level.
        private static void AssertBueV1MirrorTiming()
        {
            var routed = new List<string>();
            var previousRecorder = BueRuntimeLog.Recorder;
            var previousSink = NetworkModuleAdapter.DiagnosticLogSink;
            var previousCompatSink = BetterUnturnedExperience.Core.Network.LmnV1CompatLayer.DiagnosticLogSink;
            BueRuntimeLog.Recorder = line => routed.Add(line);
            try
            {
                var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2net-red-" + Guid.NewGuid().ToString("N"));
                Type modTransportType = null; // BUE bootstraps before the LMN assembly loads
                var adapter = new NetworkModuleAdapter(adapterRoot, () => true, () => modTransportType, () => null, () => { });
                adapter.BindProductionLog();
                adapter.ActivateCore();
                Assert(adapter.TakeoverActive,
                    "mirror timing: the takeover arms from the chainloader manifest even before LMN's assembly loads");
                Assert(routed.Exists(line => line.StartsWith("Debug ") && line.Contains("event=v1-table-mirror") && line.Contains("result=deferred")),
                    "mirror timing: an LMN-not-ready bootstrap mirror defers below the Error level (P5 zero false positive)");
                Assert(CountToken(routed, "result=deferred") == 1,
                    "mirror timing: the deferral is recorded exactly once (silent retries, no spam)");
                Assert(!routed.Exists(line => line.StartsWith("Error ")),
                    "mirror timing: an LMN-not-ready bootstrap emits no ERROR line through the production route");

                // LMN's assembly loads (the type resolves) but its handler
                // tables are still EMPTY — the legacy plugins register later.
                FakeLmnModTransport.Reset();
                modTransportType = typeof(FakeLmnModTransport);
                for (var tick = 0; tick < 2 * NetworkModuleAdapter.DeferredMirrorTickInterval; tick++) adapter.RetryPendingMirror();
                Assert(!routed.Exists(line => line.Contains("result=mirrored")),
                    "mirror timing: an empty handler table is not a completed mirror — the retry stays armed");
                Assert(!routed.Exists(line => line.StartsWith("Error ")),
                    "mirror timing: retrying against an empty table emits no ERROR line");

                // The legacy plugin registers channel 250 AFTER the takeover
                // armed (the DEV-V2-07 fixture shape, LMN ModTransport table).
                ulong lateSender = 0;
                byte[] latePayload = null;
                FakeLmnModTransport.ServerHandlers[250] = (sender, reader) =>
                {
                    lateSender = sender.Value;
                    latePayload = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                };
                FakeLmnModTransport.ClientHandlers[250] = reader => FakeLmnModTransport.ClientCalls++;

                // The plugin Update tick drives the deferred retry on its
                // cadence; the mirror must now complete, still without any
                // error line.
                for (var tick = 0; tick < 3 * NetworkModuleAdapter.DeferredMirrorTickInterval; tick++) adapter.RetryPendingMirror();
                byte[] lateFrame;
                Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(250, new byte[] { 0x5A }, out lateFrame),
                    "setup: the late-registered V1 frame builds");
                Assert(adapter.ShouldConsumeInbound(true, 424242UL, lateFrame, 0, lateFrame.Length, null),
                    "mirror timing: after the deferred retry the late-registered channel routes through the compat layer");
                Assert(lateSender == 424242UL && latePayload != null && latePayload.Length == 1 && latePayload[0] == 0x5A,
                    "mirror timing: the late legacy handler receives the sender and payload intact");
                Assert(adapter.ShouldConsumeInbound(false, 0UL, lateFrame, 0, lateFrame.Length, null),
                    "mirror timing: the mirrored client-side receive consumes the legacy frame as well");
                Assert(FakeLmnModTransport.ClientCalls == 1,
                    "mirror timing: the late-registered client channel routes as well");
                Assert(routed.Exists(line => line.StartsWith("Debug ") && line.Contains("event=v1-table-mirror") && line.Contains("result=mirrored") && line.Contains("deferred=true")),
                    "mirror timing: the deferred mirror completes with one mirrored record below the Error level");
                Assert(CountToken(routed, "event=v1-table-mirror result=mirrored") == 1,
                    "mirror timing: the mirror completion is recorded exactly once");
                Assert(!routed.Exists(line => line.StartsWith("Error ")),
                    "mirror timing: the whole late-registration timeline stays free of ERROR lines (P5)");
            }
            finally
            {
                BueRuntimeLog.Recorder = previousRecorder;
                NetworkModuleAdapter.DiagnosticLogSink = previousSink;
                BetterUnturnedExperience.Core.Network.LmnV1CompatLayer.DiagnosticLogSink = previousCompatSink;
                FakeLmnModTransport.Reset();
            }
        }

        // GPT watermark: DEV-V2-10 red regression (F-B, real-machine audit
        // configB-verification-r1). The real machine rejected the official
        // network registration with reason=InvalidDefinitionArtifact
        // (BUE-REG-004): the baked ArtifactPayloadDigest did not match the
        // payload's SHA-256, so the feature never entered the catalog. The
        // official network definitions must validate through a real
        // FeatureRegistrationRuntime — the payload digest is computed from
        // the payload, never transcribed by hand.
        private static void AssertBueNetworkRegistrationDefinitions()
        {
            var runtime = new FeatureRegistrationRuntime();
            runtime.OpenRegistration();
            var registrations = NetworkModuleFeatureRegistration.CreateOfficialRegistrations();
            Assert(registrations.Length == 2,
                "definitions: the network module registers its two official facets (network + V1 compat, spec-V2-phase1 L74)");
            // DEV-V2-10 R3 (Standards H5): the digest is self-consistent by
            // construction, so it can never catch a payload TYPO — pin the
            // documented payload texts themselves; the qualification kit's
            // definition table must describe exactly these bytes.
            Assert(PayloadText(NetworkModuleFeatureRegistration.CreateNetworkDefinition()) == "BUE-NET-V1",
                "definitions: the network payload is exactly the documented 'BUE-NET-V1' text");
            Assert(PayloadText(NetworkModuleFeatureRegistration.CreateV1CompatDefinition()) == "BUE-NET-V1C",
                "definitions: the V1 compat payload is exactly the documented 'BUE-NET-V1C' text");
            for (var index = 0; index < registrations.Length; index++)
            {
                var result = runtime.Register(registrations[index]);
                Assert(result.Accepted,
                    "definitions: official network definition '" + registrations[index].Definition.Feature.Value
                    + "' is accepted by the real registration runtime (got " + result.Reason + " " + result.DiagnosticId + ")");
            }
        }

        // DEV-V2-10 R3 (Standards H3/H4) red regression: the persisted
        // kill-switch lifecycle. (a) With network.enabled persisted OFF, the
        // module must perform zero mirror work — no LMN type resolution, no
        // deferred diagnostic, and the deferred retry stays inert (the
        // DEV-V2-06 zero-false-positive rule extends to the mirror). (b) A
        // module that STARTS disabled must still re-arm its takeover patches
        // when the player re-enables it (handbook B6): the re-enable path
        // must attempt the patch install instead of silently skipping
        // because the patch desire was never recorded at bootstrap. The test
        // host has no Assembly-CSharp, so the re-arm attempt surfaces as the
        // documented fail-closed takeover-patch diagnostic — its PRESENCE is
        // the anchor.
        private static void AssertBueNetworkKillSwitchLifecycle()
        {
            var diagnostics = new List<string>();
            var previousSink = NetworkModuleAdapter.DiagnosticLogSink;
            NetworkModuleAdapter.DiagnosticLogSink = line => diagnostics.Add(line);
            try
            {
                var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2net-red-" + Guid.NewGuid().ToString("N"));
                var persisted = new NetworkModuleAdapter(adapterRoot, () => true, () => null, () => null, () => { });
                Assert(persisted.NetworkSettings.Submit(new ScopedSettingChangeRequest(41UL, SettingRevisionScope.ClientPreference,
                    persisted.NetworkSettings.GetSnapshot(SettingRevisionScope.ClientPreference).Revision,
                    new[] { new SettingMutation("network.enabled", SettingValue.Toggle(false)) })).Accepted,
                    "setup: the kill switch persists off");

                var off = new NetworkModuleAdapter(adapterRoot, () => true, () => null, () => null, () => { });
                off.ActivateCore();
                Assert(!off.TakeoverActive,
                    "kill switch: the takeover stays inactive while the module is off");
                off.ApplyNetworkPatches();
                Assert(!diagnostics.Exists(line => line.Contains("event=v1-table-mirror")),
                    "kill switch: a disabled module performs no mirror work at all (zero reflection)");
                for (var tick = 0; tick < 3 * NetworkModuleAdapter.DeferredMirrorTickInterval; tick++) off.RetryPendingMirror();
                Assert(!diagnostics.Exists(line => line.Contains("event=v1-table-mirror")),
                    "kill switch: the deferred retry stays inert while the module is off");

                Assert(off.NetworkSettings.Submit(new ScopedSettingChangeRequest(42UL, SettingRevisionScope.ClientPreference,
                    off.NetworkSettings.GetSnapshot(SettingRevisionScope.ClientPreference).Revision,
                    new[] { new SettingMutation("network.enabled", SettingValue.Toggle(true)) })).Accepted,
                    "setup: the kill switch is re-enabled");
                off.RefreshSwitches();
                Assert(diagnostics.Exists(line => line.Contains("event=takeover-patch")),
                    "kill switch: re-enabling a startup-disabled module re-arms the takeover patches (handbook B6)");
            }
            finally
            {
                NetworkModuleAdapter.DiagnosticLogSink = previousSink;
            }
        }

        // GPT watermark: DEV-V2-11 red regression (F-C, real-machine retest
        // audit configB-retest-verification-r1). The standalone LMN type
        // names are an external contract; the authority is the LMN repository
        // source (Routing/ModTransport.cs + ModRouter.cs, both `namespace
        // LaunchMultiplayerNet`). Both production constants were transcribed
        // with an extra ".Routing" level, never resolved on any real machine,
        // so the V1 mirror and the LMN2 delegate silently never happened and
        // the deferred retry spammed a HarmonyX warning per attempt (63-169
        // per session). The anchor pins the production constants to the LMN
        // source names verbatim, and pins the resolver's silence: a missing
        // type must return null with zero log output.
        private static void AssertBueLmnTypeNameAnchor()
        {
            Assert(NetworkModuleFeatureRegistration.ModTransportTypeName == "LaunchMultiplayerNet.ModTransport",
                "lmn types: the ModTransport type name matches the LMN source full name (namespace LaunchMultiplayerNet, no .Routing segment)");
            Assert(NetworkModuleFeatureRegistration.ModRouterTypeName == "LaunchMultiplayerNet.ModRouter",
                "lmn types: the ModRouter type name matches the LMN source full name (namespace LaunchMultiplayerNet, no .Routing segment)");

            var routed = new List<string>();
            var previousRecorder = BueRuntimeLog.Recorder;
            BueRuntimeLog.Recorder = line => routed.Add(line);
            try
            {
                Assert(NetworkModuleFeatureRegistration.TryFindLoadedType("BetterUnturnedExperience.Plugin.Tests.Program") != null,
                    "lmn types: the silent resolver finds a loaded type by full name");
                Assert(NetworkModuleFeatureRegistration.TryFindLoadedType("LaunchMultiplayerNet.Routing.ModTransport") == null,
                    "lmn types: the silent resolver returns null for an absent type (the old wrong name must stay absent)");
                Assert(routed.Count == 0,
                    "lmn types: resolving a missing type emits zero log output (the deferred retry stays silent)");
            }
            finally
            {
                BueRuntimeLog.Recorder = previousRecorder;
            }
        }

        // DEV-V2-12 (F-E): the takeover's inbound dispatch lost the sender
        // identity and delivered every LMN frame twice on the real machine.
        // Root causes (audit 2026-09-04/DEV-V2-11 retest appendix + LMN
        // source): (1) Harmony runs ALL prefixes even after a higher-priority
        // one votes to skip the original, so LMN's own prefix dispatched the
        // same frame BUE had dispatched; (2) the production sender resolver
        // read TryGetSteamId's out value through a CSteamID field while the
        // SDK signature is TryGetSteamId(out ulong) — the read threw to 0 for
        // every connection. Red anchor: resolvable connections yield their
        // real steam id, a live LMN native prefix makes BUE RELEASE instead
        // of dispatching, and an unresolvable client sender is never
        // dispatched as 0.
        private static void AssertBueV2SenderIdentity()
        {
            Assert(NetworkModuleAdapter.LmnPatchOwner == "com.yu80rice.launchmultiplayernet",
                "sender identity: the LMN patch owner matches LMN's Harmony instance id (LaunchMultiplayerNetPlugin.cs:59)");
            Assert(NetworkModuleAdapter.TryGetConnectionSteamId(new FakeTransportConnection(76561199030780228UL, true)) == 76561199030780228UL,
                "sender identity: a resolvable connection yields its real steam id (TryGetSteamId's out ulong is read directly, never through CSteamID fields)");
            Assert(NetworkModuleAdapter.TryGetConnectionSteamId(null) == 0UL,
                "sender identity: a null connection yields 0");
            Assert(NetworkModuleAdapter.TryGetConnectionSteamId(new FakeTransportConnection(0UL, false)) == 0UL,
                "sender identity: an unresolvable connection yields 0");

            var diagnostics = new List<string>();
            var previousSink = NetworkModuleAdapter.DiagnosticLogSink;
            NetworkModuleAdapter.DiagnosticLogSink = line => diagnostics.Add(line);
            byte[] lmn2Frame = { 0x4C, 0x4D, 0x4E, 0x32, 0x01, 0xBB };
            try
            {
                FakeLmnModTransport.Reset();
                FakeLmnModRouter.Reset();
                FakeLmnModTransport.ServerHandlers[103] = (sender, reader) =>
                {
                    FakeLmnModTransport.LastSender = sender.Value;
                    FakeLmnModTransport.LastPayload = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                };
                FakeLmnModTransport.ClientHandlers[103] = reader => { FakeLmnModTransport.ClientCalls++; };
                byte[] v1Frame;
                Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(103, new byte[] { 0x11, 0x22 }, out v1Frame),
                    "setup: the V1 mirror frame builds");

                // LMN-native-dispatch LIVE world (the real machine: LMN's own
                // prefix sits next to BUE's on the same intercept points). BUE
                // must RELEASE every LMN frame — LMN's prefix dispatches it,
                // and a BUE dispatch delivered the SAME frame twice.
                var live = new NetworkModuleAdapter(
                    Path.Combine(Path.GetTempPath(), "bue-v2-sender-live-" + Guid.NewGuid().ToString("N")),
                    () => true, () => typeof(FakeLmnModTransport), () => typeof(FakeLmnModRouter), () => { },
                    isLmnNativeClientDispatchLive: () => true);
                live.ActivateCore();
                Assert(live.LmnNativeClientDispatchLive && live.LmnNativeServerDispatchLive && live.LmnNativeDispatchLive,
                    "takeover: the LMN-native-dispatch live state is observable on the adapter");
                Assert(!live.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: with LMN's native dispatch live the legacy frame is released, never dispatched (exactly-once)");
                Assert(FakeLmnModTransport.LastSender == 0UL && FakeLmnModTransport.LastPayload == null,
                    "takeover: the released legacy frame never reaches the mirrored handler through BUE");
                Assert(!live.ShouldConsumeInbound(false, 0UL, v1Frame, 0, v1Frame.Length, null) && FakeLmnModTransport.ClientCalls == 0,
                    "takeover: the server-direction legacy frame is released while LMN's native dispatch is live");
                FakeLmnModRouter.NextResult = true;
                Assert(!live.ShouldConsumeInbound(true, 0UL, lmn2Frame, 0, lmn2Frame.Length, null) && FakeLmnModRouter.ClientCalls == 0,
                    "takeover: an LMN2 frame is released while LMN's native dispatch is live (the router is LMN's prefix business)");
                Assert(CountToken(diagnostics, "event=v1-frame-release result=released decision=lmn-native-dispatch diagnosticId=BUE-V2NET-003") == 1
                    && CountToken(diagnostics, "event=lmn2-frame-release result=released decision=lmn-native-dispatch diagnosticId=BUE-V2NET-003") == 1,
                    "takeover: the first released frame of each kind emits exactly one structured release record (the retest's positive anchor)");

                // LMN-native-dispatch INERT world (LMN's patch failed): BUE
                // keeps the dispatch path as the only dispatcher, but a client
                // frame whose sender cannot be resolved is released — never
                // dispatched as sender=0 (the F-E identity defect).
                var inert = new NetworkModuleAdapter(
                    Path.Combine(Path.GetTempPath(), "bue-v2-sender-inert-" + Guid.NewGuid().ToString("N")),
                    () => true, () => typeof(FakeLmnModTransport), () => typeof(FakeLmnModRouter), () => { });
                inert.ActivateCore();
                Assert(!inert.LmnNativeDispatchLive,
                    "takeover: without LMN's native dispatch the inert state is observable");
                Assert(inert.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: with LMN's native dispatch inert the resolved legacy frame is dispatched (BUE is the only dispatcher)");
                Assert(FakeLmnModTransport.LastSender == 424242UL,
                    "takeover: the inert-world dispatch carries the resolved sender");
                FakeLmnModTransport.LastSender = 0UL;
                FakeLmnModTransport.LastPayload = null;
                Assert(!inert.ShouldConsumeInbound(true, 0UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: an unresolvable client sender is released — never dispatched as sender=0 (the F-E anchor)");
                Assert(FakeLmnModTransport.LastPayload == null,
                    "takeover: the sender=0 frame never reaches the mirrored handler");
                Assert(CountToken(diagnostics, "event=v1-frame-release result=released decision=unresolved-sender diagnosticId=BUE-V2NET-003") == 1,
                    "takeover: the inert-world unresolved-sender drop emits exactly one boundary record (release is a deliberate non-delivery there)");
                FakeLmnModRouter.Reset();
                FakeLmnModRouter.NextResult = true;
                Assert(inert.ShouldConsumeInbound(true, 0UL, lmn2Frame, 0, lmn2Frame.Length, null) && FakeLmnModRouter.ClientCalls == 1,
                    "takeover: with LMN's native dispatch inert the LMN2 frame still delegates to LMN's router (unchanged)");

                // DEV-V2-12 R2 (Standards S1): the bootstrap-time probe runs
                // BEFORE LMN's Awake installs its prefixes (BUE bootstraps
                // first by file-name order), so the cached inert state is
                // legitimate at startup — the tick path must re-probe on the
                // throttled cadence until LMN's native dispatch appears, or
                // BUE keeps dispatching next to LMN's live prefix (double
                // delivery on the real machine).
                bool lateProbe = false;
                var late = new NetworkModuleAdapter(
                    Path.Combine(Path.GetTempPath(), "bue-v2-sender-late-" + Guid.NewGuid().ToString("N")),
                    () => true, () => typeof(FakeLmnModTransport), () => typeof(FakeLmnModRouter), () => { },
                    isLmnNativeClientDispatchLive: () => lateProbe);
                late.ActivateCore();
                Assert(!late.LmnNativeDispatchLive,
                    "setup: the probe reports inert at bootstrap (LMN's prefixes are not installed yet)");
                Assert(late.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "setup: while inert the adapter still dispatches the resolved legacy frame");
                FakeLmnModTransport.LastSender = 0UL;
                FakeLmnModTransport.LastPayload = null;
                lateProbe = true; // LMN's Awake ran sometime after BUE's bootstrap
                for (var tick = 0; tick < NetworkModuleAdapter.DeferredMirrorTickInterval; tick++) late.RetryPendingMirror();
                Assert(late.LmnNativeDispatchLive,
                    "takeover: the throttled tick re-probe latches LMN's live dispatch once it appears");
                Assert(!late.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null) && FakeLmnModTransport.LastPayload == null,
                    "takeover: once the tick re-probe latches live, frames are released (never dispatched next to LMN's live prefix)");

                // DEV-V2-12 R2 (Spec P5): per-direction liveness — LMN's
                // install is not strictly atomic (a failed server patch can
                // leave the client patch in place). The live direction
                // releases, the inert direction keeps BUE as its only
                // dispatcher; neither direction doubles.
                var partial = new NetworkModuleAdapter(
                    Path.Combine(Path.GetTempPath(), "bue-v2-sender-partial-" + Guid.NewGuid().ToString("N")),
                    () => true, () => typeof(FakeLmnModTransport), () => typeof(FakeLmnModRouter), () => { },
                    isLmnNativeClientDispatchLive: () => true, isLmnNativeServerDispatchLive: () => false);
                partial.ActivateCore();
                Assert(partial.LmnNativeClientDispatchLive && !partial.LmnNativeServerDispatchLive && !partial.LmnNativeDispatchLive,
                    "takeover: the partial state is observable per direction");
                Assert(!partial.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null) && FakeLmnModTransport.LastPayload == null,
                    "takeover: the live direction releases its frames");
                Assert(partial.ShouldConsumeInbound(false, 0UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: the inert direction still dispatches (BUE is that direction's only dispatcher)");

                // DEV-V2-12 R3 (Standards R3): the tick re-probe must run
                // while ANY direction is still inert. A partial install whose
                // server prefix arrives late would otherwise keep BUE
                // dispatching server-direction frames next to LMN's live
                // server prefix — the double delivery returns on that
                // direction.
                bool serverLateClientProbe = true;
                bool serverLateServerProbe = false;
                var serverLate = new NetworkModuleAdapter(
                    Path.Combine(Path.GetTempPath(), "bue-v2-sender-serverlate-" + Guid.NewGuid().ToString("N")),
                    () => true, () => typeof(FakeLmnModTransport), () => typeof(FakeLmnModRouter), () => { },
                    isLmnNativeClientDispatchLive: () => serverLateClientProbe, isLmnNativeServerDispatchLive: () => serverLateServerProbe);
                serverLate.ActivateCore();
                Assert(serverLate.LmnNativeClientDispatchLive && !serverLate.LmnNativeServerDispatchLive,
                    "setup: the partial snapshot latches client live while the server prefix is absent");
                Assert(serverLate.ShouldConsumeInbound(false, 0UL, v1Frame, 0, v1Frame.Length, null),
                    "setup: the inert server direction still dispatches");
                serverLateServerProbe = true; // LMN's server prefix installs late
                for (var tick = 0; tick < NetworkModuleAdapter.DeferredMirrorTickInterval; tick++) serverLate.RetryPendingMirror();
                Assert(serverLate.LmnNativeServerDispatchLive,
                    "takeover: the tick re-probe keeps probing while ANY direction is still inert");
                Assert(!serverLate.ShouldConsumeInbound(false, 0UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: the late-live server direction releases (the double delivery never returns)");

                // DEV-V2-12 R2 (Spec P2): exactly-once DECISION composition.
                // The real machine runs BUE's prefix AND LMN's prefix on the
                // same intercept points; the retest observed LMN's prefix
                // dispatching every LMN frame while live (the second arrival)
                // and nothing while inert. The LMN-side counts below are that
                // frozen causal model (not an in-host fake), and the asserted
                // variable is BUE's own decision: (BUE dispatched ? 1 : 0) +
                // (LMN prefix live ? 1 : 0) must be exactly 1 in every
                // quadrant — BUE's release in the live world is what keeps
                // LMN's 1 from becoming 2.
                FakeLmnModTransport.LastSender = 0UL;
                FakeLmnModTransport.LastPayload = null;
                Assert((live.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null) ? 1 : 0) + 1 == 1,
                    "decision-composition: live client — BUE decides release (0 dispatches), LMN native model constant 1, total 1");
                Assert((live.ShouldConsumeInbound(false, 0UL, v1Frame, 0, v1Frame.Length, null) ? 1 : 0) + 1 == 1,
                    "decision-composition: live server — BUE decides release (0 dispatches), LMN native model constant 1, total 1");
                Assert((inert.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null) ? 1 : 0) + 0 == 1,
                    "decision-composition: inert client — BUE dispatches 1, LMN inert model constant 0, total 1");
                Assert((inert.ShouldConsumeInbound(false, 0UL, v1Frame, 0, v1Frame.Length, null) ? 1 : 0) + 0 == 1,
                    "decision-composition: inert server — BUE dispatches 1, LMN inert model constant 0, total 1");
            }
            finally
            {
                NetworkModuleAdapter.DiagnosticLogSink = previousSink;
            }
        }

        private static string PayloadText(FeatureDefinitionArtifact definition)
        {
            var bytes = new byte[definition.CanonicalPayload.Count];
            for (var index = 0; index < bytes.Length; index++) bytes[index] = definition.CanonicalPayload[index];
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        // GPT watermark: DEV-V2-10 red regression (F-B, panel half). The real
        // machine's sidebar lacked the network entries because the official
        // network registration was REJECTED (BUE-REG-004) — production does
        // not throw on that, the entries are simply missing. The anchor
        // replays the production sequence exactly: Awake registers the three
        // official features, the composition Initialize performs its
        // pre-completion refresh (catalog not yet built — the network entries
        // must be ABSENT), then the host barrier completes and the plugin's
        // completion path (TryRefreshAfterCompletion → RefreshManagementPanel,
        // BetterUnturnedExperiencePlugin.cs) refreshes again — the entries
        // must appear for the handbook B4-B6 / P4b takeover card steps.
        private static void AssertBueNetworkPanelEntries()
        {
            var previousRuntime = BueRuntimeHost.CurrentRuntime;
            try
            {
                var hostRuntime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(hostRuntime);
                hostRuntime.OpenRegistration();
                // Awake-time registrations. A rejected facet is NOT fatal in
                // production (it logs a runtime line) — replay that honestly:
                // the entries assertion below is what catches the rejection.
                Assert(BetterItemInteractionFeatureRegistration.Register().Accepted,
                    "setup: the official BII registration is accepted through the host bridge");
                var registrations = NetworkModuleFeatureRegistration.CreateOfficialRegistrations();
                for (var index = 0; index < registrations.Length; index++)
                {
                    BueRuntimeHost.Register(registrations[index]);
                }

                var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2net-red-" + Guid.NewGuid().ToString("N"));
                var composition = new BueClientUiCompositionRoot(new NetworkModuleAdapter(adapterRoot, () => false, () => null, () => null, () => { }));
                Assert(composition.Initialize(false, false, true), "setup: the composition initializes");
                var beforeCompletion = composition.ManagementPanel.Model.GetEntries();
                Assert(!HasManagementEntry(beforeCompletion, "io.github.yu80rice.bue.network", "BUE 网络模块")
                    && !HasManagementEntry(beforeCompletion, "io.github.yu80rice.bue.network.v1compat", "BUE V1 兼容层"),
                    "panel: the pre-completion refresh (unfrozen catalog) has no network entries yet");

                // The host barrier completes and the plugin's completion path
                // refreshes the panel (TryRefreshAfterCompletion seam).
                Assert(hostRuntime.CompleteRuntime(), "setup: the host barrier completes");
                composition.RefreshManagementPanel();
                var entries = composition.ManagementPanel.Model.GetEntries();
                Assert(HasManagementEntry(entries, "io.github.yu80rice.bue.network", "BUE 网络模块"),
                    "panel: after the completion refresh the catalog projects the BUE 网络模块 entry");
                Assert(HasManagementEntry(entries, "io.github.yu80rice.bue.network.v1compat", "BUE V1 兼容层"),
                    "panel: after the completion refresh the catalog projects the BUE V1 兼容层 entry");
                composition.Destroy();
            }
            finally
            {
                BueRuntimeHost.Bind(previousRuntime);
            }
        }

        private static bool HasManagementEntry(IReadOnlyList<ManagementEntryView> entries, string stableId, string displayName)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index].StableId == stableId && entries[index].DisplayName == displayName) return true;
            }
            return false;
        }

        // DEV-V2-14 red regression (GPT watermark): directional inbound
        // subscribe on the frozen contract surface. The runtime must keep two
        // handler tables keyed by ChannelDirection (frame source, NOT local
        // role), dispatch only after the channel is registered, run handlers
        // outside the state lock, isolate a single handler's exception, and
        // hand out independent idempotent dispose handles. Disabling the
        // network module keeps Register/Unregister/Subscribe legal with zero
        // inbound dispatch and explicit NoSession sends. RED until the
        // directional contract lands (compile CS1503 on the 2-arg call sites,
        // then runtime assertions).
        private static void AssertBueV2DirectionalSubscribe()
        {
            var localContract = new ContractVersion(2, 0);
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var a = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1002UL);
            var b = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL);
            var channel = new FeatureId("io.example.v2sub");
            Assert(a.RegisterChannel(channel, localContract, 1).Accepted && b.RegisterChannel(channel, localContract, 1).Accepted,
                "setup: both runtimes register the channel");
            var aSession = a.StartSession(2002UL);
            pair.First.Pump(); pair.Second.Pump(); // A Hello -> B (Ack)
            pair.First.Pump();                     // B Ack -> A
            Assert(a.Sessions.Count == 1 && b.Sessions.Count == 1, "setup: handshake established both sides");

            // 1. Direction semantics: A initiated the handshake, so A's
            //    inbound frames come FROM SERVER; B's come FROM CLIENTS.
            var aFromServer = 0;
            var aWrongDirection = 0;
            var bFromClients = 0;
            var bWrongDirection = 0;
            a.Subscribe(channel, ChannelDirection.FromServer, (session, payload) => aFromServer++);
            a.Subscribe(channel, ChannelDirection.FromClients, (session, payload) => aWrongDirection++);
            b.Subscribe(channel, ChannelDirection.FromClients, (session, payload) => bFromClients++);
            b.Subscribe(channel, ChannelDirection.FromServer, (session, payload) => bWrongDirection++);
            Assert(a.SendToClient(channel, aSession, new byte[] { 0x01 }, true) == NetworkSendResult.Sent, "setup: A sends to B");
            pair.First.Pump(); pair.Second.Pump();
            Assert(bFromClients == 1 && bWrongDirection == 0,
                "direction: the responder's frame dispatches only the FromClients handler (direction = frame source)");
            Assert(b.SendToClient(channel, b.Sessions[0], new byte[] { 0x02 }, true) == NetworkSendResult.Sent, "setup: B sends to A");
            pair.First.Pump(); pair.Second.Pump();
            Assert(aFromServer == 1 && aWrongDirection == 0,
                "direction: the initiator's frame dispatches only the FromServer handler (direction is not the local role)");

            // 2. Subscribing to an unregistered channel is legal; frames only
            //    dispatch once that channel is registered (handler table and
            //    channel table are decoupled).
            var unregistered = new FeatureId("io.example.v2sub-late");
            var lateHits = 0;
            var lateHandle = b.Subscribe(unregistered, ChannelDirection.FromClients, (session, payload) => lateHits++);
            Assert(lateHandle != null, "unregistered channel: subscribing before RegisterChannel is legal");
            Assert(a.RegisterChannel(unregistered, localContract, 1).Accepted, "setup: the sender registers the unregistered channel");
            Assert(a.SendToClient(unregistered, aSession, new byte[] { 0x03 }, true) == NetworkSendResult.Sent, "setup: frame flows for the unregistered channel");
            pair.First.Pump(); pair.Second.Pump();
            Assert(lateHits == 0, "unregistered channel: no dispatch while the receiving side has not registered the channel");
            Assert(b.RegisterChannel(unregistered, localContract, 1).Accepted, "setup: the receiver registers the channel");
            Assert(a.SendToClient(unregistered, aSession, new byte[] { 0x04 }, true) == NetworkSendResult.Sent, "setup: frame flows after registration");
            pair.First.Pump(); pair.Second.Pump();
            Assert(lateHits == 1, "unregistered channel: dispatch starts once the channel is registered and traffic arrives");
            lateHandle.Dispose();

            // 3. Independent idempotent handles: the same delegate subscribed
            //    twice gets both deliveries; each handle disposes only itself.
            var multiHits = 0;
            Action<IConnectionSession, byte[]> multiHandler = (session, payload) => multiHits++;
            var handleOne = a.Subscribe(channel, ChannelDirection.FromServer, multiHandler);
            var handleTwo = a.Subscribe(channel, ChannelDirection.FromServer, multiHandler);
            Assert(!ReferenceEquals(handleOne, handleTwo), "handles: every subscription returns its own handle");
            Assert(b.SendToClient(channel, b.Sessions[0], new byte[] { 0x05 }, true) == NetworkSendResult.Sent, "setup: frame flows to the double subscriber");
            pair.First.Pump(); pair.Second.Pump();
            Assert(multiHits == 2, "handles: the same delegate subscribed twice is invoked once per handle");
            handleOne.Dispose();
            Assert(b.SendToClient(channel, b.Sessions[0], new byte[] { 0x06 }, true) == NetworkSendResult.Sent, "setup: frame flows after the first dispose");
            pair.First.Pump(); pair.Second.Pump();
            Assert(multiHits == 3, "handles: disposing one handle leaves the other subscription alive");
            handleOne.Dispose();
            handleTwo.Dispose();
            Assert(b.SendToClient(channel, b.Sessions[0], new byte[] { 0x07 }, true) == NetworkSendResult.Sent, "setup: frame flows after the double dispose");
            pair.First.Pump(); pair.Second.Pump();
            Assert(multiHits == 3, "handles: a disposed handle receives nothing and re-dispose is safe (idempotent)");

            // 4. Fail-fast developer errors: null handler and an undefined
            //    direction value throw argument exceptions.
            var nullHandlerThrown = false;
            try { a.Subscribe(channel, ChannelDirection.FromClients, null); }
            catch (ArgumentNullException) { nullHandlerThrown = true; }
            Assert(nullHandlerThrown, "fail-fast: a null handler throws ArgumentNullException");
            var badDirectionThrown = false;
            try { a.Subscribe(channel, (ChannelDirection)42, multiHandler); }
            catch (ArgumentOutOfRangeException) { badDirectionThrown = true; }
            Assert(badDirectionThrown, "fail-fast: an undefined ChannelDirection value throws ArgumentOutOfRangeException");

            // 5. Handler isolation and lock-freedom: a throwing handler does
            //    not stop its peers, and a handler may re-enter the API
            //    (Sessions) because dispatch runs outside the state lock.
            var isolationHits = 0;
            a.Subscribe(channel, ChannelDirection.FromServer, (session, payload) => { throw new InvalidOperationException("bad consumer"); });
            a.Subscribe(channel, ChannelDirection.FromServer, (session, payload) => isolationHits++);
            Assert(b.SendToClient(channel, b.Sessions[0], new byte[] { 0x08 }, true) == NetworkSendResult.Sent, "setup: frame flows to the throwing pair");
            pair.First.Pump(); pair.Second.Pump();
            Assert(isolationHits == 1, "isolation: the surviving handler still ran after its peer threw");

            // 5b. Lock-freedom, proven from a FOREIGN thread: Monitor is
            //      reentrant on the dispatching thread, so calling Sessions
            //      from inside the handler proves nothing. A foreign thread
            //      must be able to take the state lock while the handler runs.
            var foreignAcquiredInTime = false;
            a.Subscribe(channel, ChannelDirection.FromServer, (session, payload) =>
            {
                var foreign = System.Threading.Tasks.Task.Run(() => { var count = a.Sessions.Count; return count; });
                foreignAcquiredInTime = foreign.Wait(TimeSpan.FromSeconds(2));
            });
            Assert(b.SendToClient(channel, b.Sessions[0], new byte[] { 0x09 }, true) == NetworkSendResult.Sent, "setup: frame flows to the lock probe");
            var probePump = System.Threading.Tasks.Task.Run(() => pair.First.Pump());
            Assert(probePump.Wait(TimeSpan.FromSeconds(5)), "lock-freedom: the probe pump completed");
            Assert(foreignAcquiredInTime,
                "lock-freedom: a foreign thread acquired the state lock while the handler ran — dispatch never holds it");

            // 6. Disabled network module: subscriptions stay legal, inbound is
            //    zero, Sessions is an empty snapshot, sends return the existing
            //    enum values, lifecycle never fires, and re-arming needs no
            //    re-subscription.
            var disabledHits = 0;
            var survivingHandle = b.Subscribe(channel, ChannelDirection.FromClients, (session, payload) => disabledHits++);
            b.SetModuleActive(false);
            Assert(b.Sessions.Count == 0, "disabled: Sessions is an empty snapshot");
            var disabledSubscribed = b.Subscribe(channel, ChannelDirection.FromClients, (session, payload) => { });
            Assert(disabledSubscribed != null, "disabled: subscribing while the module is down stays legal");
            var disabledChannel = new FeatureId("io.example.v2sub-disabled");
            Assert(b.RegisterChannel(disabledChannel, localContract, 1).Accepted, "disabled: registering a channel while down stays legal");
            Assert(b.UnregisterChannel(disabledChannel), "disabled: unregistering a channel while down stays legal");
            Assert(b.SendToServer(channel, new byte[] { 0x0A }, true) == NetworkSendResult.NoSession,
                "disabled: SendToServer returns the explicit NoSession result");
            Assert(b.SendToClients(channel, new byte[] { 0x0A }, true) == NetworkSendResult.NoSession,
                "disabled: SendToClients returns the explicit NoSession result");
            Assert(b.SendToClient(channel, b.Sessions.Count > 0 ? b.Sessions[0] : null, new byte[] { 0x0A }, true) == NetworkSendResult.NoSession,
                "disabled: SendToClient returns the explicit NoSession result");
            Assert(b.StartSession(1002UL) == null, "disabled: no session is created while the module is down");
            Assert(a.SendToClient(channel, aSession, new byte[] { 0x0B }, true) == NetworkSendResult.Sent, "setup: the peer still emits frames while B is down");
            pair.First.Pump(); pair.Second.Pump();
            Assert(disabledHits == 0, "disabled: inbound dispatch is zero while the module is down");
            b.SetModuleActive(true);
            // The client re-initiates the handshake (production topology: the
            // answering side re-arms, the initiator re-handshakes). The channel
            // table survives the cycle — no re-registration, no re-subscribe.
            a.StartSession(2002UL);
            pair.First.Pump(); pair.Second.Pump(); // A Hello -> B (B answers, Ack)
            pair.First.Pump();                     // B Ack -> A
            Assert(a.SendToClient(channel, aSession, new byte[] { 0x0C }, true) == NetworkSendResult.Sent,
                "setup: A sends over the re-established topology");
            pair.First.Pump(); pair.Second.Pump();
            Assert(disabledHits == 1, "re-arm: subscriptions survive the disable/enable cycle without re-subscribing");
            survivingHandle.Dispose();
        }

        // DEV-V2-14 red regression (GPT watermark): IFeatureBootstrap.Network
        // injection. The host-side bootstrap composition must hand features a
        // fail-fast non-null IBueNetworkApi that stays the same instance
        // across disable/enable cycles, surfaces explicit results while the
        // module is not ready, and leaves subscription handles safely and
        // repeatably disposable afterwards. RED until the composition exists
        // (compile CS0246, then runtime assertions).
        private static void AssertBueV2NetworkInjection()
        {
            var localContract = new ContractVersion(2, 0);
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var runtime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1002UL);
            var identity = default(FeatureScopeIdentity);
            var nullNetworkThrown = false;
            try { new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(identity, 1UL, null, null, null, null, null, null, null); }
            catch (ArgumentNullException) { nullNetworkThrown = true; }
            Assert(nullNetworkThrown, "injection: the bootstrap fails fast on a null network API (Network is never null)");
            var runtimeAsApi = (IBueNetworkApi)runtime;
            var bootstrap = new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(identity, 1UL, null, null, null, null, null, null, runtimeAsApi);
            Assert(bootstrap.Network != null && ReferenceEquals(bootstrap.Network, runtimeAsApi),
                "injection: Network carries exactly the host-provided IBueNetworkApi instance");
            var channel = new FeatureId("io.example.v2inj");
            Assert(bootstrap.Network.RegisterChannel(channel, localContract, 1).Accepted, "injection: channels register through the injected API");
            var hits = 0;
            var handle = bootstrap.Network.Subscribe(channel, ChannelDirection.FromClients, (session, payload) => hits++);
            Assert(handle != null, "injection: subscribing through the injected API yields a handle");

            // While the network module is not ready the same instance answers
            // with explicit results — never null, never a silent exception.
            runtime.SetModuleActive(false);
            Assert(ReferenceEquals(bootstrap.Network, runtimeAsApi), "injection: disable never substitutes the Network instance");
            Assert(bootstrap.Network.Sessions.Count == 0, "not-ready: Sessions is an explicit empty snapshot");
            Assert(bootstrap.Network.SendToServer(channel, new byte[] { 0x01 }, true) == NetworkSendResult.NoSession,
                "not-ready: sends return the explicit NoSession result");
            var notReadyHandle = bootstrap.Network.Subscribe(channel, ChannelDirection.FromClients, (session, payload) => hits++);
            Assert(notReadyHandle != null, "not-ready: subscribing while not ready stays legal");

            // Feature-stop semantics (frozen "失效或可安全重复释放"): handles
            // are safely and repeatably disposable — never a leak, never a
            // throw — and stay dead afterwards; the channel registration the
            // feature owns is likewise releasable through the same API. The
            // host-side auto-invalidation on IFeatureModule.Stop rides the
            // host start path (DEV-V2-21/22).
            notReadyHandle.Dispose();
            notReadyHandle.Dispose();
            handle.Dispose();
            handle.Dispose();
            Assert(bootstrap.Network.UnregisterChannel(channel),
                "stop semantics: the feature's channel registration is releasable through the same API");
            runtime.SetModuleActive(true);
            var rearmedHits = 0;
            var rearmedHandle = bootstrap.Network.Subscribe(channel, ChannelDirection.FromClients, (session, payload) => rearmedHits++);
            Assert(rearmedHandle != null, "re-arm: the same API instance takes fresh subscriptions");
            Assert(bootstrap.Network.RegisterChannel(channel, localContract, 1).Accepted,
                "setup: the re-armed runtime re-owns the channel for the re-handshake");
            var peerRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL);
            peerRuntime.RegisterChannel(channel, localContract, 1);
            peerRuntime.StartSession(1002UL);
            pair.Second.Pump(); pair.First.Pump(); // peer Hello -> runtime (Ack)
            pair.Second.Pump();
            Assert(runtime.Sessions.Count == 1, "setup: the re-armed runtime established the session");
            Assert(peerRuntime.SendToServer(channel, new byte[] { 0x02 }, true) == NetworkSendResult.Sent, "setup: the peer sends after re-arm");
            pair.First.Pump(); pair.Second.Pump();
            Assert(hits == 0, "stop semantics: disposed handles receive nothing after the module returns");
            Assert(rearmedHits == 1, "stop semantics: the re-armed module dispatches to fresh subscriptions (channel table intact)");
        }

        private static int CountToken(List<string> lines, string token)
        {
            var count = 0;
            for (var index = 0; index < lines.Count; index++)
            {
                if (lines[index].Contains(token)) count++;
            }
            return count;
        }

        private sealed class CountingSettingsEditor : IBueSettingsEditor
        {
            private readonly System.Action onApply;
            internal CountingSettingsEditor(System.Action onApply) { this.onApply = onApply ?? throw new ArgumentNullException(nameof(onApply)); }
            public FeatureSettingsSnapshot GetSnapshot(FeatureId feature)
            {
                return new FeatureSettingsSnapshot(feature, 1, SettingRevisionScope.ClientPreference, 0,
                    SettingSyncState.Unavailable, SettingSnapshotSource.SafeDefault, new SettingEntryView[0]);
            }
            public SettingChangeResult Apply(FeatureId feature, uint expectedRevision, SettingMutation mutation)
            {
                onApply();
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, GetSnapshot(feature));
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

        // DEV-V2-09: the injected main-menu entry must occupy the next vanilla
        // pitch slot below the item-store entry, not the store button's bottom
        // edge. Vanilla MenuDashboardUI left column: 200x50 buttons on a 60px
        // pitch (Play 170 / Survivors 230 / Configuration 290 / Workshop 350 /
        // item-store 410), so adjacent items leave a 10px visual gap; the old
        // hardcoded y=460 sat flush against the store button (0px gap).
        private static void AssertMainMenuEntryLayoutMatchesVanillaRhythm()
        {
            Assert(BueMenuEntryLayout.MainMenuColumnButtonX == 0f, "BUE main-menu entry aligns with the vanilla left column edge");
            Assert(BueMenuEntryLayout.MainMenuColumnButtonWidth == 200f && BueMenuEntryLayout.MainMenuColumnButtonHeight == 50f, "BUE main-menu entry size matches the vanilla 200x50 button geometry");
            Assert(BueMenuEntryLayout.MainMenuColumnSlotPitch == 60f, "BUE main-menu column pitch matches the vanilla 60px rhythm");
            Assert(BueMenuEntryLayout.MainMenuStoreSlotY == 410f, "item-store slot anchor matches the vanilla MenuDashboardUI layout");
            Assert(BueMenuEntryLayout.MainMenuBueSlotY == BueMenuEntryLayout.MainMenuStoreSlotY + BueMenuEntryLayout.MainMenuColumnSlotPitch, "BUE main-menu entry occupies the next pitch slot below the store entry");
            Assert(BueMenuEntryLayout.MainMenuBueSlotY == 470f, "BUE main-menu entry slot is 410 + 60 = 470, restoring the vanilla 10px visual gap");
        }

        // DEV-V2-13: the pause-menu entry must join the native PlayerPauseUI
        // column directly below Return, with every vanilla element below it
        // shifted down exactly one slot. Vanilla column (decompiled): all
        // buttons at X=-100, 200x50, PositionScale 0.5/0.5, Return at Y=-290,
        // 60px pitch; spy mode (onSpyReady) moves the whole column to X=-435.
        private static void AssertPauseMenuEntryLayoutMatchesNativeColumn()
        {
            Assert(BueMenuEntryLayout.PauseReturnSlotY == -290f, "pause Return slot anchor matches the vanilla PlayerPauseUI layout");
            Assert(BueMenuEntryLayout.PauseColumnSlotPitch == 60f, "pause column pitch matches the vanilla 60px rhythm");
            Assert(BueMenuEntryLayout.PauseBueSlotY == BueMenuEntryLayout.PauseReturnSlotY + BueMenuEntryLayout.PauseColumnSlotPitch, "BUE pause entry occupies the next pitch slot below Return");
            Assert(BueMenuEntryLayout.PauseBueSlotY == -230f, "BUE pause entry slot is -290 + 60 = -230, directly below Return");
            Assert(BueMenuEntryLayout.PauseColumnButtonX == -100f && BueMenuEntryLayout.PauseColumnButtonWidth == 200f && BueMenuEntryLayout.PauseColumnButtonHeight == 50f, "BUE pause entry matches the vanilla column geometry (X=-100, 200x50)");
            Assert(BueMenuEntryLayout.PauseSpyColumnButtonX == -435f, "pause spy-mode column X matches the vanilla onSpyReady layout");
            var expectedShiftFields = new[] { "inviteFriendsButton", "optionsButton", "displayButton", "graphicsButton", "controlsButton", "audioButton", "suicideButton", "suicideDisabledLabel", "exitButton", "quitButton" };
            Assert(BueMenuEntryLayout.PauseShiftFieldNames.Length == expectedShiftFields.Length, "pause shift manifest covers exactly the ten vanilla elements below Return");
            for (var index = 0; index < expectedShiftFields.Length; index++)
            {
                Assert(BueMenuEntryLayout.PauseShiftFieldNames[index] == expectedShiftFields[index], "pause shift manifest entry " + index + " matches the vanilla field name");
            }
            Assert(Array.IndexOf(BueMenuEntryLayout.PauseShiftFieldNames, "returnButton") < 0, "pause shift manifest excludes Return (BUE slots directly below it)");
        }

        // DEV-V2-13: the vanilla-column shift must be anchored to each
        // element's captured Y (never accumulate), re-anchor new instances
        // after a UI rebuild, and hand the original layout back on cleanup.
        private static void AssertPauseColumnShiftAnchorsWithoutDrift()
        {
            var shift = new BuePauseColumnShift(60f);
            var keyA = new object();
            var keyB = new object();
            var yA = 100f;
            var yB = -230f;
            var capturedA = shift.Apply(keyA, yA, value => yA = value);
            Assert(capturedA && Math.Abs(yA - 160f) < 0.01f, "first Apply captures the vanilla Y and shifts exactly one pitch");
            yA = 200f;
            var capturedAgain = shift.Apply(keyA, yA, value => yA = value);
            Assert(!capturedAgain && Math.Abs(yA - 160f) < 0.01f, "re-Apply re-anchors from the captured vanilla Y, never from drifted current Y");
            var capturedB = shift.Apply(keyB, yB, value => yB = value);
            Assert(capturedB && Math.Abs(yB - (-170f)) < 0.01f, "each element anchors its own vanilla Y independently");
            var restoredA = shift.Restore(keyA, value => yA = value);
            Assert(restoredA && Math.Abs(yA - 100f) < 0.01f, "Restore hands back the captured vanilla Y");
            Assert(!shift.Restore(keyA, value => yA = value), "Restore without an anchor is a no-op");
            Assert(shift.RemoveAnchor(keyB) && shift.AnchoredCount == 0, "cleanup drains the registry between UI builds");
            var yC = -170f;
            var keyC = new object();
            var capturedC = shift.Apply(keyC, yC, value => yC = value);
            Assert(capturedC && Math.Abs(yC - (-110f)) < 0.01f, "after the registry drains (UI rebuild) a fresh element anchors from its current vanilla Y");
            var snapshot = shift.SnapshotAnchors();
            Assert(snapshot != null && snapshot.Count == shift.AnchoredCount && snapshot.Count == 1 && ReferenceEquals(snapshot[0].Key, keyC), "snapshot mirrors the anchored registry for restoration walks");
            Assert(shift.RemoveAnchor(keyC) && shift.AnchoredCount == 0, "a dead instance's anchor can be dropped for a rebuilt UI");
            Assert(!shift.RemoveAnchor(keyC) && !shift.Restore(keyC, value => yC = value), "dropped anchors no longer restore");
            var yD = -170f;
            var keyD = new object();
            var capturedD = shift.Apply(keyD, yD, value => yD = value);
            Assert(capturedD && Math.Abs(yD - (-110f)) < 0.01f, "a fresh element after the drop anchors from its own current vanilla Y");
            var keyR = new object();
            var yR = -290f;
            shift.Apply(keyR, yR, value => yR = value);
            Assert(Math.Abs(yR - (-230f)) < 0.01f, "shift before a failed restore fixture");
            var setterThrew = false;
            try { shift.Restore(keyR, delegate { throw new InvalidOperationException("restore fixture failure"); }); }
            catch (InvalidOperationException) { setterThrew = true; }
            Assert(setterThrew, "Restore surfaces setter failures instead of swallowing them");
            var yRetry = 0f;
            var retried = shift.Restore(keyR, value => yRetry = value);
            Assert(retried && Math.Abs(yRetry - (-290f)) < 0.01f, "Restore keeps the anchor on a failed attempt so the restore can be retried");
            var collidingA = new CollidingShiftKey();
            var collidingB = new CollidingShiftKey();
            var yA2 = 100f;
            var yB2 = 300f;
            shift.Apply(collidingA, yA2, value => yA2 = value);
            shift.Apply(collidingB, yB2, value => yB2 = value);
            Assert(Math.Abs(yA2 - 160f) < 0.01f && Math.Abs(yB2 - 360f) < 0.01f, "distinct element instances anchor independently even when their Equals collides");
        }

        // DEV-V2-13: every manifest field must resolve against the real vanilla
        // assembly (exitButton/quitButton are public static, the rest non-public),
        // otherwise part of the column would stay put while the rest shifts.
        private static void AssertPauseShiftFieldsResolveAgainstVanillaAssembly()
        {
            var resolved = BueNativeManagementPanel.ResolvePlayerPauseShiftFields();
            Assert(resolved != null && resolved.Length == BueMenuEntryLayout.PauseShiftFieldNames.Length, "pause shift resolver covers the whole manifest");
            for (var index = 0; index < resolved.Length; index++)
            {
                Assert(resolved[index] != null, "pause shift field resolves against the vanilla assembly: " + BueMenuEntryLayout.PauseShiftFieldNames[index]);
            }
        }

        private sealed class CollidingShiftKey
        {
            public override bool Equals(object other) { return true; }
            public override int GetHashCode() { return 0; }
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
