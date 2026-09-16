using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.NoOpFixture;
using BetterUnturnedExperience.Plugin;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Lit;
using BetterUnturnedExperience.Lir;
using BetterUnturnedExperience.Lht;
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
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-fb1-red")
                {
                    AssertPreviewUpdateTransientFaultIsAbsorbedAndVisible();
                    AssertSinkRemountsAfterThirdPartyPanelClear();
                AssertSinkRebuildsElementsOnNativeWriteFault();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-fb1c-red")
                {
                    AssertBueV2Fb1cProjectionReconciler();
                    Console.WriteLine("DEV-V2-24 F-B1c projection reconciler collection: ALL GREEN (5 groups) — groups: 决策核矩阵/分派路由+no-op/纯编排+TIDYABLE全域/BUE-LIT-001诊断行/harness全链接线");
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-fd-red")
                {
                    AssertBueV2FdHeadlessCompletionSurvival();
                    Console.WriteLine("DEV-V2-24 F-D headless completion survival collection: ALL GREEN (5 groups) — groups: 场景驱动完成链/共享tick链帧去重/退出teardown/纯门决策真值表/链Reset哨兵归位");
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-fe-red")
                {
                    AssertBueV2FeEnginePeerIdentity();
                    Console.WriteLine("DEV-V2-24 F-E engine peer identity collection: ALL GREEN (2 groups) — groups: SteamIdPlausible 段校验/ClientPeer·LocalSteamId 决策真值表");
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-lit-sendhealth-red")
                {
                    AssertBueV2LitSendHealth(collectAllFailures: true);
                    Console.WriteLine("DEV-V2-25 LIT send health collection: ALL GREEN (3 groups) — groups: 重臂退避真值表/限频+降级+恢复清零/harness 持续失败全链");
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
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-lit-red")
                {
                    AssertLitSingleplayerPath();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v5-02-tagged-layout-red")
                {
                    AssertDevV502TaggedRowBand();
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v5-03-container-session-red")
                {
                    DevV5ContainerSessionTests.Run(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v5-04-insert-recover-red")
                {
                    DevV5InsertRecoverTests.Run(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-send-semantics-red")
                {
                    AssertBueV2SessionDrivenSendSemantics(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-handshake-red")
                {
                    AssertBueV2AutoHandshakeLifecycle(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-frame-binding-red")
                {
                    AssertBueV2FrameBinding(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-event-clock-red")
                {
                    AssertBueV2EventBusAndHostTick(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v3-event-routing-red")
                {
                    AssertBueV3EventOwnershipRouting(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v3-lifecycle-red")
                {
                    AssertBueV3LifecycleProjection(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v3-network-red")
                {
                    AssertBueV3NetworkTransportRules(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v3-clock-red")
                {
                    AssertBueV3HostClockSemantics(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v3-settings-red")
                {
                    AssertBueV3SettingsWiringAndPanelRouting(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v3-diagnostics-red")
                {
                    AssertBueV3DiagnosticsWiringAndSummary(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v3-probe-red")
                {
                    AssertBueV3EcosystemUnifiedProbe(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-lit-multiplayer-red")
                {
                    AssertBueV2LitMultiplayerPath(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-lir-red")
                {
                    AssertBueV2LirAdoption(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-lht-red")
                {
                    AssertBueV2LhtAdoption(collectAllFailures: true);
                    return 0;
                }
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "--bue-v2-platform-red")
                {
                    AssertBueV2PlatformSelfCheck(collectAllFailures: true);
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
                AssertTransientIsolationGate();
                AssertPreviewUpdateTransientFaultIsAbsorbedAndVisible();
                AssertSinkRemountsAfterThirdPartyPanelClear();
                AssertSinkRebuildsElementsOnNativeWriteFault();
                AssertBueV2Fb1cProjectionReconciler();
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
                AssertBueV2SessionDrivenSendSemantics();
                AssertBueV2AutoHandshakeLifecycle();
                AssertBueV2FrameBinding();
                AssertBueV2EventBusAndHostTick();
                AssertBueV2LitMultiplayerPath();
                AssertBueV2LitSendHealth();
                AssertBueV2LirAdoption();
                AssertBueV2LhtAdoption();
                AssertBueV2PlatformSelfCheck();
                AssertPlatformPanelNotice();
                AssertLitSingleplayerPath();
                AssertDevV502TaggedRowBand();
                DevV5ContainerSessionTests.Run();
                DevV5InsertRecoverTests.Run();
                AssertRuntimeCompletionBarrierIsolates();
                AssertManagementPanelConsumesRuntimeCatalog();
                AssertManagementPanelOpenHooks();
                Assert(RequiresParentRebindSemantics(), "management panel resets bindings when UI parent changes");
                Assert(runtime.Phase == FeatureRegistrationPhase.RuntimeReady, "runtime barrier enters RuntimeReady");
                Assert(runtime.Catalog.Entries[0].Definition.Feature.Value == "io.github.yu80rice.bue.better-item-interaction", "catalog order is deterministic by feature identity");
                var late = NoOpFeatureRegistration.Register();
                Assert(late.Reason == FeatureRegistrationReason.PhaseClosed, "fixture late registration is rejected");
                AssertBueV3RegistrationGateAndBootstrapMatrix();
                AssertBueV3EventOwnershipRouting();
                AssertBueV3LifecycleProjection();
                AssertBueV3NetworkTransportRules();
                AssertBueV3HostClockSemantics();
                AssertBueV3SettingsWiringAndPanelRouting();
                AssertBueV3DiagnosticsWiringAndSummary();
                AssertBueV3EcosystemUnifiedProbe();
                DevV4ExternalConfigParityTests.Run();
                DevTicket04UpmTagParsingTests.Run();
                // F-E: pure truth tables, no host state — runs before F-D.
                AssertBueV2FeEnginePeerIdentity();
                // F-D: runs last — it replaces the bound runtime and clears the
                // host on purpose, so nothing after it may depend on that state.
                AssertBueV2FdHeadlessCompletionSurvival();
                Console.WriteLine("DEV-14/DEV-16B plugin runtime tests: PASS"); return 0;
            }
            catch (Exception error) { Console.WriteLine("DEV-14 official registration parity tests: FAIL"); Console.WriteLine(error.ToString()); return 1; }
        }

        // DEV-16D diagnosis replay: hook and drag-start fire, but no preview
        // projection reaches the visual sink. Intentionally red until fixed.
        private static void AssertDev16DRealLogMustContainVisiblePreviewProjection(bool preFix = false)
        {
            // Tracked-safe .log.txt suffix: the global *.log gitignore ban would leave .log fixtures out of clean clones (POST-P4-02).
            var fixture = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", preFix ? "dev16d-r36-no-preview.log.txt" : "dev16d-fixed-preview.log.txt");
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
            var fixture = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "dev16d-fixed-preview.log.txt");
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

        // DEV-V2-24 F-B2 red regression. The real machine (DEV-V2-24-20260908
        // P2P host) showed the isolation latch firing on ONE transient
        // hierarchy-probe failure during the close/reopen transition — BUE's
        // Better-Item-Interaction surface died for the whole session with
        // zero log lines. The fix debounces: only PERSISTENT incompatibility
        // (threshold consecutive frames) latches the isolation, transient
        // frames self-heal, and every first failure plus the latch itself
        // emit one-shot diagnostics through the DiagnosticLogSink seam.
        // RED until the TransientIsolationGate exists (compile CS0246).
        private static void AssertTransientIsolationGate()
        {
            var gate = new InventorySurfaceLifecycleAdapter.TransientIsolationGate();

            // Transient: a few incompatible frames then a ready frame must
            // never isolate — the reopen-rebuild window self-heals.
            var isolated = false;
            string line;
            for (var frame = 0; frame < 3; frame++)
            {
                if (gate.Observe(2, true, out line)) isolated = true;
                Assert(line == null || frame == 0,
                    "isolation gate: first failure logs once, subsequent strike frames stay silent");
            }
            Assert(!isolated, "isolation gate: 3 transient incompatible frames do not isolate");
            Assert(!gate.Observe(2, false, out line), "isolation gate: a ready frame resets the strikes");
            Assert(line != null && line.IndexOf("recover", System.StringComparison.Ordinal) >= 0,
                "isolation gate: recovery after strikes emits a one-shot recovered line");

            // Persistent: threshold consecutive frames isolate exactly on the
            // threshold frame, and the latch line names the diagnostic.
            for (var frame = 1; frame < InventorySurfaceLifecycleAdapter.TransientIsolationGate.IsolationStrikeThreshold; frame++)
            {
                Assert(!gate.Observe(2, true, out _),
                    "isolation gate: strike frames below the threshold stay silent and un-isolated");
            }
            string latchLine;
            Assert(gate.Observe(2, true, out latchLine),
                "isolation gate: persistent incompatibility isolates exactly at the threshold frame");
            Assert(latchLine != null && latchLine.IndexOf("BUE-INVENTORY-003", System.StringComparison.Ordinal) >= 0,
                "isolation gate: the latch line carries the BUE-INVENTORY-003 diagnostic");
            Assert(!gate.Observe(2, true, out _),
                "isolation gate: post-latch observations are dormant (one-way latch)");

            // Poll failures share the gate with their own strike lane.
            var pollGate = new InventorySurfaceLifecycleAdapter.TransientIsolationGate();
            for (var frame = 1; frame < InventorySurfaceLifecycleAdapter.TransientIsolationGate.IsolationStrikeThreshold; frame++)
            {
                Assert(!pollGate.ObservePollFailure(out _),
                    "isolation gate: poll-failure strikes below the threshold stay un-isolated");
            }
            string pollLatch;
            Assert(pollGate.ObservePollFailure(out pollLatch) && pollLatch != null,
                "isolation gate: persistent poll failures isolate at the threshold with a latch line");
            Assert(pollGate.ObservePollSuccess(), "isolation gate: a successful poll resets the failure strikes");
            Assert(!pollGate.ObservePollFailure(out _),
                "isolation gate: post-reset poll failures start a fresh strike lane");
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
                // DEV-V4-04：facet 退役后 adapter 不再持有每功能设置 runtime——总开关
                // 的停用事实由生命周期意图库承载（下面各开关段改走意图记录/清除）。
                Assert(BueFeatureIntentRuntime.StoreFor(adapterRoot) != null,
                    "migration: the adapter resolves the lifecycle intent store for its settings root");
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

                Assert(BueFeatureIntentRuntime.StoreFor(adapterRoot).RecordUserDisabled(NetworkModuleAdapter.V1CompatFeature),
                    "setup: the V1 compat switch is disabled via the lifecycle intent record");
                takeover.RefreshSwitches();
                Assert(!takeover.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "takeover: v1compat off hands legacy frames back unconsumed");
                Assert(BueFeatureIntentRuntime.StoreFor(adapterRoot).Clear(NetworkModuleAdapter.V1CompatFeature),
                    "setup: the V1 compat switch is re-enabled (intent cleared)");
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

                Assert(BueFeatureIntentRuntime.StoreFor(adapterRoot).RecordUserDisabled(NetworkModuleAdapter.NetworkFeature),
                    "setup: the network module switch is turned off (lifecycle intent record)");
                takeover.RefreshSwitches();
                Assert(!takeover.ShouldConsumeInbound(true, 424242UL, v1Frame, 0, v1Frame.Length, null),
                    "recovery: network module off hands every legacy frame back (LMN resumes standalone)");
                Assert(!takeover.ShouldConsumeInbound(false, 0UL, lmn2Frame, 0, lmn2Frame.Length, null),
                    "recovery: network module off passes LMN2 frames through");
                Assert(BueFeatureIntentRuntime.StoreFor(adapterRoot).Clear(NetworkModuleAdapter.NetworkFeature),
                    "setup: the network module switch is re-enabled (intent cleared)");
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

            // 9. (DEV-V3-06) The former hardcoded-list routing editor unit is
            //    retired with RoutingBueSettingsEditor: catalog-driven panel
            //    routing is anchored in AssertBueV3SettingsWiringAndPanelRouting
            //    (面板目录路由/不伪造页/官方与生态并列/直连单元下方).
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
                // DEV-V4-04：总开关退役后，网络模块的停用事实改由生命周期意图库承载
                // （旧 network.enabled 文档键退役；facet 与设置页不再暴露该开关）。
                var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2net-red-" + Guid.NewGuid().ToString("N"));
                BueFeatureIntentRuntime.EnsureCreated(adapterRoot, null);
                var intents = BueFeatureIntentRuntime.StoreFor(adapterRoot);
                var armed = new NetworkModuleAdapter(adapterRoot, () => true, () => null, () => null, () => { });
                armed.ActivateCore();
                Assert(armed.TakeoverActive,
                    "kill switch setup: the takeover arms while no disable intent exists");
                diagnostics.Clear(); // 基线自检行（含 mirror-deferred）不参与停用后的零反射判据

                Assert(intents.RecordUserDisabled(NetworkModuleAdapter.NetworkFeature),
                    "setup: the kill switch persists off (UserDisabled 意图事实)");

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

                Assert(intents.Clear(NetworkModuleAdapter.NetworkFeature),
                    "setup: the kill switch is re-enabled (disable intent cleared)");
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
            // DEV-V2-17: re-initiating over the still-listed established
            // session supersedes it with ONE fresh connection generation and
            // returns the new pending session — the re-established topology
            // rides the replacement, the stale object is gone.
            var aRearmedSession = a.StartSession(2002UL);
            pair.First.Pump(); pair.Second.Pump(); // A Hello -> B (B answers, Ack)
            pair.First.Pump();                     // B Ack -> A
            Assert(aRearmedSession != null && a.Sessions.Count == 1 && ReferenceEquals(a.Sessions[0], aRearmedSession),
                "re-arm: the re-handshake supersedes the stale session with one fresh generation");
            Assert(a.SendToClient(channel, aRearmedSession, new byte[] { 0x0C }, true) == NetworkSendResult.Sent,
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
            try { new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(identity, 1UL, null, null, null, null, null, null, null, null); }
            catch (ArgumentNullException) { nullNetworkThrown = true; }
            Assert(nullNetworkThrown, "injection: the bootstrap fails fast on a null network API (Network is never null)");
            var runtimeAsApi = (IBueNetworkApi)runtime;
            var bootstrap = new BetterUnturnedExperience.Core.Registration.FeatureBootstrap(identity, 1UL, null, null, null, null, null, null, null, runtimeAsApi);
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

        // DEV-V2-21 red anchor: the LIT multiplayer path over the BUE named
        // channel. Five collected groups — the fake-transport full chain
        // (challenge → request → authoritative transaction → reliable
        // committed → flow ack → hotkey restore → result), the session
        // challenge gates (no-challenge refusal, generation invalidation),
        // the connection-generation fault scope (memory clears, disk
        // persists), the TidyCompleted publish semantics, and the
        // half-registration rollback. The engine-facing authority is faked
        // (ILitTidyAuthority) so the full protocol chain runs on the
        // loopback pair with zero Harmony patches.
        private static void AssertBueV2LitMultiplayerPath(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                Group("双端收发全链", () => LitMultiplayerGroupFullChain(Check));
                Group("session challenge", () => LitMultiplayerGroupChallenge(Check));
                Group("挑战发送失败重臂", () => LitMultiplayerGroupChallengeSendFailureRearm(Check));
                Group("代际 fault scope", () => LitMultiplayerGroupFaultScope(Check));
                Group("TidyCompleted 发布", () => LitMultiplayerGroupTidyCompleted(Check));
                Group("半注册回滚", () => LitMultiplayerGroupHalfRegistration(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V2-21 LIT multiplayer collection: ALL GREEN (0 failures) — groups: 双端收发全链/session challenge/挑战发送失败重臂/代际 fault scope/TidyCompleted 发布/半注册回滚");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-21 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        /// <summary>The re-arm backoff truth table against a fake clock: the
        /// FIRST retry after a challenge failure stays immediate (the frozen
        /// DEV-V2-24 F-A contract — one transient blip recovers on the next
        /// Tick), from the second consecutive failure the wait doubles along
        /// the BueNetworkRuntime re-probe precedent (1s → 8s cap, monotone),
        /// a Sent challenge clears the series (failures start fresh), and a
        /// dropped generation leaves no residue.</summary>
        private static void LitSendHealthGroupRearmBackoff(System.Action<bool, string> check)
        {
            var stamp = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() { return stamp; }
            void AdvanceMs(int ms) { stamp = stamp.AddMilliseconds(ms); }

            var book = new LitChallengeRearmBook(Clock);
            check(book.ShouldAttempt(2UL) && !book.HasPending(2UL),
                "退避真值表：新代际无退避记录，立即允许重臂");

            book.NoteChallengeFailure(2UL);
            check(book.HasPending(2UL) && book.ShouldAttempt(2UL),
                "退避真值表：首次失败后下一拍立即允许重试（F-A 冻结契约，零等待位）");

            book.NoteChallengeFailure(2UL);
            AdvanceMs(999);
            check(!book.ShouldAttempt(2UL), "退避真值表：第二次连续失败后 999ms 内不允许重臂");
            AdvanceMs(1);
            check(book.ShouldAttempt(2UL), "退避真值表：第二次连续失败后满 1000ms 允许重臂");

            book.NoteChallengeFailure(2UL);
            AdvanceMs(1999);
            check(!book.ShouldAttempt(2UL), "退避真值表：第三次连续失败后 1999ms 内不允许重臂");
            AdvanceMs(1);
            check(book.ShouldAttempt(2UL), "退避真值表：第三次连续失败后满 2000ms 允许重臂");

            book.NoteChallengeFailure(2UL);
            AdvanceMs(3999);
            check(!book.ShouldAttempt(2UL), "退避真值表：第四次连续失败后 3999ms 内不允许重臂");
            AdvanceMs(1);
            check(book.ShouldAttempt(2UL), "退避真值表：第四次连续失败后满 4000ms 允许重臂");

            book.NoteChallengeFailure(2UL);
            AdvanceMs(7999);
            check(!book.ShouldAttempt(2UL), "退避真值表：第五次连续失败后 7999ms 内不允许重臂");
            AdvanceMs(1);
            check(book.ShouldAttempt(2UL), "退避真值表：第五次连续失败后满 8000ms 允许重臂");

            book.NoteChallengeFailure(2UL);
            AdvanceMs(7999);
            check(!book.ShouldAttempt(2UL), "退避真值表：第六次连续失败仍在 8000ms 封顶内");
            AdvanceMs(1);
            check(book.ShouldAttempt(2UL), "退避真值表：封顶后每 8000ms 允许一次重臂");

            check(book.ConsecutiveFailures(2UL) == 6, "退避真值表：连续失败计数贯穿全程");

            book.NoteChallengeSent(2UL);
            check(!book.HasPending(2UL) && book.ShouldAttempt(2UL) && book.ConsecutiveFailures(2UL) == 0,
                "退避真值表：challenge 送达即清零（恢复后失败序列从零重开）");

            book.NoteChallengeFailure(3UL);
            book.DropGeneration(3UL);
            check(!book.HasPending(3UL) && book.ConsecutiveFailures(3UL) == 0,
                "退避真值表：代际丢弃即清该代际记录（更替/断开不留残留）");
        }

        /// <summary>The sustained-failure harness run: a server whose
        /// targeted sends stay refused while the fake clock crosses
        /// minutes. On machine (DEV-V2-24, v6 P2P) this shape produced 8333
        /// WARN lines by re-arming every frame; here the re-arm backoff and
        /// the WARN limiter must bound both the attempts (monotone gaps —
        /// the two same-instant attempts are the F-A next-beat retry, then
        /// 1s/2s/4s/8s/8s) and the log lines (first + every 50th, ONE
        /// BUE-LIT-003 degradation diagnostic), and flipping the transport
        /// back must deliver the challenge and recover the full tidy
        /// chain.</summary>
        private static void LitSendHealthGroupSustainedFailureHarness(System.Action<bool, string> check)
        {
            var faultDir = NewLitFaultDirectory();
            var clock = new FakeClock();
            SustainedFailureNetwork injector = null;
            var harness = LitMultiplayerHarness.Create(faultDir, net => injector = new SustainedFailureNetwork(net, clock.Now), clock.Now);
            harness.Handshake();

            var emitted = new List<string>();
            var infoEmitted = new List<string>();
            var previousSink = LitRuntime.ErrorLogSink;
            var previousInfoSink = LitRuntime.LogSink;
            LitRuntime.ErrorLogSink = line => emitted.Add(line);
            LitRuntime.LogSink = line => infoEmitted.Add(line);
            try
            {
                static int CountLines(List<string> lines, string needle)
                {
                    var count = 0;
                    for (int i = 0; i < lines.Count; i++) { if (lines[i].Contains(needle)) count++; }
                    return count;
                }
                Func<int> warnCount = () => CountLines(emitted, "定向发送未送达");
                Func<int> degradedCount = () => CountLines(emitted, "BUE-LIT-003 event=link-degraded");
                Func<int> recoveredCount = () => CountLines(infoEmitted, "BUE-LIT-003 event=link-recovered");

                harness.ServerModule.Tick();
                harness.Pump();
                harness.ServerModule.Tick();
                harness.Pump();
                check(injector.SendToClientCalls == 2,
                    "harness 持续失败：首次采纳+下一拍立即重臂=恰好两次尝试（F-A 冻结契约保留）");
                check(harness.ClientRawFromServer.Count == 0,
                    "harness 持续失败：两次尝试均未送达（客户端零收到）");

                for (int i = 0; i < 5; i++) { harness.ServerModule.Tick(); harness.Pump(); }
                check(injector.SendToClientCalls == 2 && warnCount() == 0,
                    "harness 持续失败：退避窗口内逐拍 Tick 不再自旋（尝试钉在 2）；DEV-V3-04 退役锚：不再产逐帧发送失败 WARN（告警限频被平台链路健康接管）");

                clock.AdvanceMs(1000); harness.ServerModule.Tick(); harness.Pump();
                clock.AdvanceMs(2000); harness.ServerModule.Tick(); harness.Pump();
                clock.AdvanceMs(4000); harness.ServerModule.Tick(); harness.Pump();
                clock.AdvanceMs(8000); harness.ServerModule.Tick(); harness.Pump();
                clock.AdvanceMs(8000); harness.ServerModule.Tick(); harness.Pump();
                check(injector.SendToClientCalls == 7,
                    "harness 持续失败：1s/2s/4s/8s/8s 各到期一次（尝试总数=7）");
                var gaps = injector.SendGapMs();
                var monotone = true;
                for (int i = 1; i < gaps.Count; i++) { if (gaps[i] < gaps[i - 1]) { monotone = false; break; } }
                check(monotone && gaps[0] == 0 && gaps[1] == 0 && gaps[2] == 1000 && gaps[3] == 2000 && gaps[4] == 4000 && gaps[5] == 8000 && gaps[6] == 8000,
                    "harness 持续失败：重臂间隔序列（0,0=下一拍 F-A 重试,1000,2000,4000,8000,8000）单调不减");

                for (int i = 0; i < 5; i++) { clock.AdvanceMs(8000); harness.ServerModule.Tick(); harness.Pump(); }
                check(degradedCount() == 0,
                    "harness 持续失败：BUE-LIT-003 link-degraded 已退役——链路事实由平台 BUE-NET-002 电平诊断呈现（「链路健康电平」组锚），LIT 不再自产");

                for (int i = 0; i < 43; i++) { clock.AdvanceMs(8000); harness.ServerModule.Tick(); harness.Pump(); }
                check(injector.SendToClientCalls == 55,
                    "harness 持续失败：模拟 ~7 分钟共 55 次尝试（封顶后每 8s 一次，业务重臂保留）");
                check(warnCount() == 0,
                    "harness 持续失败：55 次尝试零逐帧 WARN（告警限频退役——机上 8333 条形态从源头根除）");
                check(degradedCount() == 0 && recoveredCount() == 0,
                    "harness 持续失败：episode 两端均由平台链路健康呈现，LIT 侧 BUE-LIT-003 不再上抛");

                injector.Fail = false;
                clock.AdvanceMs(8000);
                harness.ServerModule.Tick();
                harness.Pump();
                check(injector.SendToClientCalls == 56 &&
                    harness.ClientRawFromServer.Count > 0 &&
                    harness.ClientRawFromServer[0][1] == LitTidyWireCodec.MsgSessionChallenge,
                    "harness 持续失败：传输恢复后下一次到期重臂即送达 challenge");
                check(recoveredCount() == 0,
                    "harness 持续失败：恢复拍 LIT 侧也不产 recovered 行（退役彻底——平台 BUE-NET-003 呈现恢复）");
                check(warnCount() == 0,
                    "harness 持续失败：成功拍与失败拍均无 LIT 逐帧告警（限频器退役）");

                harness.ClientModule.Tick();
                harness.Pump();
                var request = harness.ClientModule.RequestTidy(3, TidyMode.SameType, true);
                check(request == LitTidyRequestResult.Dispatched,
                    "harness 持续失败：恢复后客户端整理请求受理（token 全链可用）");
                harness.Pump();
                harness.ServerModule.Tick();
                harness.Pump();
                check(harness.ClientRawFromServer.Exists(p => p[1] == LitTidyWireCodec.MsgTidyCommitted),
                    "harness 持续失败：恢复后 TidyCommitted 全链复通");
            }
            finally
            {
                LitRuntime.ErrorLogSink = previousSink;
                LitRuntime.LogSink = previousInfoSink;
            }
        }

        /// <summary>Delegating IBueNetworkApi wrapper whose SendToClient calls
        /// are refused with LocalTransportUnavailable while Fail is set (no
        /// delivery) — the sustained-transport-unavailability injector for
        /// the DEV-V2-25 harness group; call stamps ride the injected clock
        /// for the re-arm-gap assertions.</summary>
        private sealed class SustainedFailureNetwork : IBueNetworkApi
        {
            private readonly IBueNetworkApi inner;
            private readonly Func<DateTime> clock;
            private readonly List<DateTime> callStamps = new List<DateTime>();

            internal bool Fail = true;
            internal int SendToClientCalls { get { return callStamps.Count; } }

            internal SustainedFailureNetwork(IBueNetworkApi inner, Func<DateTime> clock)
            {
                this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
                this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            }

            internal List<long> SendGapMs()
            {
                var gaps = new List<long>();
                for (int i = 1; i < callStamps.Count; i++)
                {
                    gaps.Add((long)(callStamps[i] - callStamps[i - 1]).TotalMilliseconds);
                }
                if (callStamps.Count > 0) gaps.Insert(0, 0);
                return gaps;
            }

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { return inner.RegisterChannel(channel, minimumBueContract, featureVersion); }

            public bool UnregisterChannel(FeatureId channel) { return inner.UnregisterChannel(channel); }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            { return inner.Subscribe(channel, direction, handler); }

            public IReadOnlyList<IConnectionSession> Sessions { get { return inner.Sessions; } }

            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable)
            { return inner.SendToServer(channel, payload, reliable); }

            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
            { return inner.SendToClients(channel, payload, reliable); }

            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable)
            {
                callStamps.Add(clock());
                if (Fail) return NetworkSendResult.LocalTransportUnavailable;
                return inner.SendToClient(channel, session, payload, reliable);
            }
        }

        /// <summary>Injectable fake clock for the send-health seams (the Lit
        /// domain reads wall-clock DateTime; the books take the reader as a
        /// delegate so tests step simulated minutes instantly).</summary>
        private sealed class FakeClock
        {
            internal DateTime UtcNow = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);
            internal DateTime Now() { return UtcNow; }
            internal void AdvanceMs(int ms) { UtcNow = UtcNow.AddMilliseconds(ms); }
        }

        // DEV-V4-06: the fake scoped settings view feeding the LIT click seam —
        // it counts GetSnapshot calls (the same-revision rule is "one read
        // serves both values") and serves a canned ClientPreference snapshot.
        private sealed class FakeTidySettingsView : IScopedFeatureSettings
        {
            public readonly List<SettingEntryView> Entries = new List<SettingEntryView>();
            public uint Revision = 7U;
            public int GetSnapshotCalls;

            public FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)
            {
                GetSnapshotCalls++;
                return new FeatureSettingsSnapshot(new FeatureId(LitRuntime.FeatureIdValue), 1U,
                    revisionScope, Revision, SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent, Entries);
            }

            public bool TryGet(string settingId, out SettingValue value, out uint revision)
            {
                value = default(SettingValue);
                revision = Revision;
                for (var index = 0; index < Entries.Count; index++)
                {
                    if (Entries[index].SettingId != settingId) continue;
                    value = Entries[index].EffectiveValue;
                    return true;
                }
                return false;
            }

            public SettingChangeResult Submit(ScopedSettingChangeRequest request)
            {
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, Revision, default(FeatureSettingsSnapshot));
            }

            /// <summary>Sets or replaces one canned entry (schema drift legs drop entries).</summary>
            internal void SetEntry(string settingId, SettingValue value)
            {
                for (var index = 0; index < Entries.Count; index++)
                {
                    if (Entries[index].SettingId != settingId) continue;
                    Entries.RemoveAt(index);
                    break;
                }
                Entries.Add(new SettingEntryView(settingId, SettingAuthority.ClientLocal,
                    new SettingValueOption(true, value), false, default(SettingPolicyView), value, true, true));
            }
        }

        // DEV-V4-06: the fake lifecycle view — the module's availability gate
        // must read THIS (machine truth), never a patch-private bool.
        private sealed class FakeTidyLifetime : IFeatureLifetime
        {
            public FeatureState State = FeatureState.Running;
            public readonly List<IDisposable> Tracked = new List<IDisposable>();

            public bool TryTrack(IDisposable registration)
            {
                Tracked.Add(registration);
                return true;
            }

            public FeatureStatusView CurrentStatus
            {
                get { return new FeatureStatusView(new FeatureId(LitRuntime.FeatureIdValue), State, FrameworkErrorCode.None, FeatureStopReason.None, "fake-lifetime", 1UL); }
            }
        }

        /// <summary>The fake engine authority: records the calls the service makes; defaults produce a committed transaction.</summary>
        private sealed class FakeLitAuthority : ILitTidyAuthority
        {
            public int ExecuteCount;
            public uint LastRequestId;
            public byte LastPage;
            // DEV-V4-06: record the mode/direction that traveled the wire so
            // the click→snapshot→request→protocol chain is observable.
            public TidyMode LastMode;
            public bool LastSortDescending;
            public int RestoreCount;
            public int ConvergenceCount;

            public List<HotkeySnapshot> CaptureClientHotkeys() { return new List<HotkeySnapshot>(); }

            public LitAuthorityResult ExecuteServerTidy(LitTidyRequestContext request)
            {
                ExecuteCount++;
                LastRequestId = request.RequestId;
                LastPage = request.Page;
                LastMode = request.Mode;
                LastSortDescending = request.SortDescending;
                var page = request.Page == LitRuntime.AllPages ? (byte)2 : request.Page;
                return new LitAuthorityResult
                {
                    Outcome = TidyOperationOutcome.Committed,
                    Mappings = new List<LitNewPositionMapping> { new LitNewPositionMapping(0, page, 0, 0, 1) },
                    RestoreEntries = new List<HotkeyRestoreEntry> { new HotkeyRestoreEntry(0, page, 0, 0, new ItemFingerprint(1, 1, 100, new byte[0])) },
                };
            }

            public LitHotkeyRestoreResult RestoreServerHotkeys(ulong peerSteamId, List<HotkeyRestoreEntry> entries)
            {
                RestoreCount++;
                return new LitHotkeyRestoreResult { Restored = entries?.Count ?? 0, Verified = entries?.Count ?? 0, Cleared = 0, FailedIndices = new List<byte>() };
            }

            /// <summary>DEV-V5-03: the player-page fake never serves container
            /// tidy — the container chain rides its own authority fake in
            /// DevV5ContainerSessionTests. A call here would mean the two
            /// paths crossed, so it answers loudly.</summary>
            public LitContainerAuthorityResult ExecuteServerContainerTidy(LitContainerTidyRequestContext request)
            {
                throw new NotSupportedException("FakeLitAuthority does not serve container tidy");
            }

            public bool VerifyClientConvergence(List<LitNewPositionMapping> mappings)
            {
                ConvergenceCount++;
                return true;
            }
        }

        /// <summary>A network stub whose SECOND subscribe throws — the half-registration rollback surface.</summary>
        private sealed class HalfRegistrationNetwork : IBueNetworkApi
        {
            public int SubscribeCalls;
            public int DisposedHandles;
            public int UnregisterCalls;

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "STUB"); }

            public bool UnregisterChannel(FeatureId channel) { UnregisterCalls++; return true; }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            {
                if (++SubscribeCalls >= 2) throw new InvalidOperationException("synthetic second-subscribe failure");
                return new TrackingHandle(this);
            }

            public IReadOnlyList<IConnectionSession> Sessions { get { return new IConnectionSession[0]; } }
            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }

            private sealed class TrackingHandle : IDisposable
            {
                private readonly HalfRegistrationNetwork owner;
                internal TrackingHandle(HalfRegistrationNetwork owner) { this.owner = owner; }
                public void Dispose() { owner.DisposedHandles++; }
            }
        }

        /// <summary>The two-peer loopback harness: client (1001) initiates, server (2002) answers, both modules run with fake authorities.</summary>
        private sealed class LitMultiplayerHarness
        {
            public BetterUnturnedExperience.Core.Network.LocalLoopbackPair Pair;
            public BetterUnturnedExperience.Core.Network.BueNetworkRuntime ServerRuntime;
            public BetterUnturnedExperience.Core.Network.BueNetworkRuntime ClientRuntime;
            public InventoryTidyModule ServerModule;
            public InventoryTidyModule ClientModule;
            // DEV-V4-06: the client module's lifecycle view (null = the stage-
            // baseline hand-composed bootstrap, exactly like the pre-06 groups).
            public FakeTidyLifetime ClientLifetime;
            public FakeLitAuthority ServerAuthority;
            public FakeLitAuthority ClientAuthority;
            public BetterUnturnedExperience.Core.Events.FeatureEventBus ServerBus;
            public BetterUnturnedExperience.Core.Events.FeatureEventBus ClientBus;
            public readonly List<TidyCompleted> ServerTidyEvents = new List<TidyCompleted>();
            public readonly List<byte[]> ClientRawFromServer = new List<byte[]>();
            public readonly List<byte[]> ServerRawFromClients = new List<byte[]>();
            public IConnectionSession ServerSession;
            public IConnectionSession ClientSession;

            public static LitMultiplayerHarness Create(string faultDir, Func<IBueNetworkApi, IBueNetworkApi> serverNetworkDecorator = null, Func<DateTime> clock = null, FakeTidyLifetime clientLifetime = null)
            {
                var localContract = new ContractVersion(2, 0);
                var feature = new FeatureId(LitRuntime.FeatureIdValue);
                var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                var clientRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1001UL);
                var serverRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL, handshakeInitiator: false);
                var harness = new LitMultiplayerHarness
                {
                    Pair = pair,
                    ClientRuntime = clientRuntime,
                    ServerRuntime = serverRuntime,
                    ServerAuthority = new FakeLitAuthority(),
                    ClientAuthority = new FakeLitAuthority(),
                    ClientLifetime = clientLifetime,
                };
                harness.ServerBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                harness.ServerModule = CreateModule(harness.ServerBus, serverRuntime, isServer: true, harness.ServerAuthority, faultDir, serverNetworkDecorator, clock);
                harness.ClientBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                harness.ClientModule = CreateModule(harness.ClientBus, clientRuntime, isServer: false, harness.ClientAuthority, faultDir, lifetime: clientLifetime);
                harness.ClientModule.Network.Subscribe(feature, ChannelDirection.FromServer, (s, p) => harness.ClientRawFromServer.Add(p));
                harness.ServerModule.Network.Subscribe(feature, ChannelDirection.FromClients, (s, p) => harness.ServerRawFromClients.Add(p));
                harness.ServerBus.Subscriber(feature).Subscribe<TidyCompleted>(harness.ServerTidyEvents.Add);
                return harness;
            }

            private static InventoryTidyModule CreateModule(BetterUnturnedExperience.Core.Events.FeatureEventBus bus, BetterUnturnedExperience.Core.Network.BueNetworkRuntime runtime, bool isServer, FakeLitAuthority authority, string faultDir, Func<IBueNetworkApi, IBueNetworkApi> networkDecorator = null, Func<DateTime> clock = null, FakeTidyLifetime lifetime = null)
            {
                var feature = new FeatureId(LitRuntime.FeatureIdValue);
                var module = new InventoryTidyModule(feature);
                module.ScopeDirectoryForTests = faultDir;
                module.FaultContextForTests = () => new LitFaultScopeContext("TestMap", 1);
                module.NetServiceFactoryForTests = (m, net, book) => new LitTidyNetService(m, networkDecorator != null ? networkDecorator(net) : net, authority, () => isServer, book, clock);
                var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, null, bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, lifetime, runtime);
                var result = module.Start(bootstrap);
                if (!result.Started) throw new InvalidOperationException("harness: module start failed: " + result.DiagnosticId);
                return module;
            }

            /// <summary>The automatic handshake: the client initiates; two pump rounds establish both sides.</summary>
            public void Handshake()
            {
                ClientSession = ClientRuntime.StartSession(2002UL);
                Pump();
                Pump();
                if (ClientRuntime.Sessions.Count != 1 || ServerRuntime.Sessions.Count != 1)
                    throw new InvalidOperationException("harness: handshake did not establish both sides");
                ServerSession = ServerRuntime.Sessions[0];
            }

            /// <summary>
            /// Drives session discovery on BOTH services and pumps: the
            /// server issues the challenge, the client discovers its live
            /// session (the request gate needs the established session in
            /// the service's tracked set).
            /// </summary>
            public void EstablishChallenge()
            {
                ServerModule.Tick();
                ClientModule.Tick();
                Pump();
            }

            public void Pump()
            {
                Pair.First.Pump();
                Pair.Second.Pump();
            }

            public void TickBoth()
            {
                ServerModule.Tick();
                ClientModule.Tick();
            }
        }

        private static string NewLitFaultDirectory()
        {
            return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bue-v2lit-mp-" + Guid.NewGuid().ToString("N"));
        }

        private static void LitMultiplayerGroupFullChain(System.Action<bool, string> check)
        {
            var faultDir = NewLitFaultDirectory();
            var harness = LitMultiplayerHarness.Create(faultDir);
            harness.Handshake();
            harness.EstablishChallenge();
            check(harness.ClientRawFromServer.Count > 0 && harness.ClientRawFromServer[0][1] == LitTidyWireCodec.MsgSessionChallenge,
                "全链：服务器在会话建立后向客户端发送 session challenge（功能私有消息 6）");

            var request = harness.ClientModule.RequestTidy(3, TidyMode.SameType, true);
            check(request == LitTidyRequestResult.Dispatched, "全链：challenge 就绪后客户端请求受理（Dispatched）");
            harness.Pump();
            harness.ServerModule.Tick();
            harness.Pump();
            harness.ClientModule.Tick();
            harness.Pump();
            harness.ServerModule.Tick();
            harness.Pump();
            check(harness.ServerAuthority.ExecuteCount == 1 && harness.ServerAuthority.LastPage == 3 && harness.ServerAuthority.LastRequestId == 1,
                "全链：主机权威恰好执行一次（reqId=1, page=3）");
            check(harness.ServerTidyEvents.Count == 1
                && harness.ServerTidyEvents[0].ConnectionGeneration == harness.ServerSession.SessionId
                && harness.ServerTidyEvents[0].TransactionId == 1UL
                && harness.ServerTidyEvents[0].FirstPage == 3 && harness.ServerTidyEvents[0].LastPage == 3
                && harness.ServerTidyEvents[0].Result == TidyCompletionResult.Succeeded
                && harness.ServerTidyEvents[0].Publisher.Value == LitRuntime.FeatureIdValue,
                "全链：权威事务终态发布 TidyCompleted（代际=会话代际, 事务=requestId, 范围=单页, Succeeded）");
            check(harness.ClientAuthority.ConvergenceCount >= 1, "全链：客户端在收到 TidyCommitted 后执行收敛检查");
            check(harness.ServerAuthority.RestoreCount == 1, "全链：客户端 HotkeyFlowAck 到达后服务器执行快捷键恢复");
            check(harness.ClientRawFromServer.Exists(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgTidyCommitted),
                "全链：客户端收到 TidyCommitted 回包（功能私有消息 3）");
            check(harness.ClientRawFromServer.Exists(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgTidyHotkeyResult),
                "全链：客户端收到 TidyHotkeyResult（功能私有消息 5）");
            check(harness.ServerRawFromClients.Exists(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgHotkeyFlowAck),
                "全链：服务器收到 HotkeyFlowAck（功能私有消息 4）");

            // Duplicate request (same token + requestId): the ledger replays
            // the cached committed — the authority executes EXACTLY once.
            harness.ClientModule.Network.SendToServer(new FeatureId(LitRuntime.FeatureIdValue),
                LitTidyWireCodec.BuildTidyRequest(ReadChallengeToken(harness.ClientRawFromServer[0]), 1, 3, TidyMode.SameType, true, new List<HotkeySnapshot>()), reliable: true);
            harness.Pump();
            harness.ServerModule.Tick();
            harness.Pump();
            check(harness.ServerAuthority.ExecuteCount == 1, "重放：重复请求命中账本缓存，权威不再执行");
            check(harness.ClientRawFromServer.FindAll(p => p.Length > 1 && p[1] == LitTidyWireCodec.MsgTidyCommitted).Count >= 2,
                "重放：重复请求收到缓存的完整 Committed 重发");
        }

        /// <summary>The challenge envelope's 64-bit token ([1][6][token8]) — reused by the replay probe.</summary>
        private static ulong ReadChallengeToken(byte[] challenge)
        {
            return BitConverter.ToUInt64(challenge, 2);
        }

        private static void LitMultiplayerGroupChallenge(System.Action<bool, string> check)
        {
            var harness = LitMultiplayerHarness.Create(NewLitFaultDirectory());
            harness.Handshake();
            // 1. No challenge yet → the client refuses to send (08 line: 客户端尚未收到有效服务端 session challenge).
            var early = harness.ClientModule.RequestTidy(3, TidyMode.SameType, true);
            check(early == LitTidyRequestResult.RejectedNoSession,
                "challenge：未收到 challenge 客户端拒绝发送（RejectedNoSession，不建 pending）");
            harness.Pump();
            harness.ServerModule.Tick();
            harness.Pump();
            check(harness.ServerAuthority.ExecuteCount == 0, "challenge：challenge 前的请求从未到达权威");

            // 2. Challenge arrives → the request path opens.
            harness.EstablishChallenge();
            var ok = harness.ClientModule.RequestTidy(3, TidyMode.SameType, true);
            check(ok == LitTidyRequestResult.Dispatched, "challenge：challenge 就绪后请求受理（Dispatched）");
            // Drain the in-flight request first — a frame dispatched after a
            // supersession is generation-mismatched (fail-closed), which the
            // FULL-chain group already pins; here the probe needs a quiesced
            // ledger.
            harness.Pump();
            harness.ServerModule.Tick();
            harness.Pump();

            // 3. Generation invalidation: a re-handshake supersedes the
            //    session with a fresh generation — the OLD token must fail
            //    closed everywhere (client gate AND server admission).
            var oldToken = ReadChallengeToken(harness.ClientRawFromServer[0]);
            harness.Handshake();
            // R10 fix: the successor challenge is adopted AT THE SESSION
            // EVENT — by the end of the handshake the client already holds
            // the NEW generation's token, so the stale-token window lives
            // SERVER-SIDE only: a crafted old-token request must fail the
            // token-only admission against the new generation's record.
            var feature = new FeatureId(LitRuntime.FeatureIdValue);
            var executedBeforeProbe = harness.ServerAuthority.ExecuteCount;
            harness.ClientModule.Network.SendToServer(feature,
                LitTidyWireCodec.BuildTidyRequest(oldToken, 99, 3, TidyMode.SameType, true, new List<HotkeySnapshot>()), reliable: true);
            harness.Pump();
            harness.ServerModule.Tick();
            harness.Pump();
            check(harness.ServerAuthority.ExecuteCount == executedBeforeProbe,
                "challenge：旧 token 的伪造请求在服务器 token-only 准入失败（fail-closed，权威零新增执行）");

            // 4. The fresh challenge (delivered at the event beat) re-arms
            //    the request path with no extra tick.
            var rearm = harness.ClientModule.RequestTidy(3, TidyMode.SameType, true);
            check(rearm == LitTidyRequestResult.Dispatched, "challenge：新代际 challenge 事件拍送达后请求恢复受理");
        }

        private static void LitMultiplayerGroupFaultScope(System.Action<bool, string> check)
        {
            // Persistent fault: opens with the session scope, writes the
            // feature-private JSON immediately, survives the disconnect
            // (memory reloads from the disk authority), and blocks the
            // reconnected peer.
            var faultDir = NewLitFaultDirectory();
            var harness = LitMultiplayerHarness.Create(faultDir);
            harness.Handshake();
            harness.EstablishChallenge();
            var peer = harness.ServerSession.PeerSteamId;
            var book = harness.ServerModule.FaultBook;
            check(book.ScopeActive && book.IsAllowed(peer), "fault：会话建立后 peer scope 开启（允许整理）");
            book.Open(peer, "host-red-test persistent", temporary: false);
            check(book.IsFaulted(peer) && !book.IsAllowed(peer), "fault：持久熔断后该 peer 被拒绝");
            check(book.ScopeFilePath != null && System.IO.File.Exists(book.ScopeFilePath)
                && System.IO.File.ReadAllText(book.ScopeFilePath).Contains("\"steamId\":" + peer.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                "fault：持久熔断立即写盘（功能私有 JSON 键结构不变）");
            var request = harness.ClientModule.RequestTidy(3, TidyMode.SameType, true);
            check(request == LitTidyRequestResult.Dispatched, "fault：客户端仍可发送（熔断由服务器权威拒绝）");
            harness.Pump();
            harness.ServerModule.Tick();
            harness.Pump();
            check(harness.ServerAuthority.ExecuteCount == 0, "fault：熔断路径零权威执行（请求被 CriticalFailure 拒绝）");
            ((BetterUnturnedExperience.Core.Network.LocalLoopbackTransport)harness.Pair.Second).DisconnectPeer(1001UL);
            harness.ServerModule.Tick();
            check(!book.ScopeActive, "fault：断线关闭该 peer 的 scope");
            check(book.IsFaulted(peer), "fault：断线清临时态后磁盘持久统计保留（从磁盘权威重新载入内存）");
            harness.Handshake();
            harness.ServerModule.Tick();
            harness.Pump();
            check(book.ScopeActive && !book.IsAllowed(peer),
                "fault：重连新代际重开 scope，持久熔断继续阻断（历史跨会话保留）");

            // Temporary fault: never touches the disk and clears at the
            // scope close (the 08 disconnect rule, generation-bound).
            var tempDir = NewLitFaultDirectory();
            var tempHarness = LitMultiplayerHarness.Create(tempDir);
            tempHarness.Handshake();
            tempHarness.EstablishChallenge();
            var tempBook = tempHarness.ServerModule.FaultBook;
            tempBook.Open(tempHarness.ServerSession.PeerSteamId, "host-red-test temp", temporary: true);
            check(tempBook.IsFaulted(tempHarness.ServerSession.PeerSteamId)
                && (tempBook.ScopeFilePath == null || !System.IO.File.Exists(tempBook.ScopeFilePath)),
                "fault：临时熔断不写盘（restoreVerified=true）");
            // Generation supersession (re-handshake, no disconnect): the old
            // generation's TEMP fault dies with it; the successor's scope
            // opens fresh (the R2-Spec GAP fix). R10-Spec GAP: the successor
            // is adopted AT THE SESSION EVENT — the successor challenge is
            // issued in the same beat, with no Tick latency.
            tempHarness.Handshake();
            check(tempHarness.ClientRawFromServer.Count >= 2,
                "fault：代际更替由会话事件即时接管（后继 challenge 事件拍发出，无 Tick 延迟——R10-Spec GAP 修复）");
            tempHarness.ServerModule.Tick();
            tempHarness.Pump();
            check(tempBook.ScopeActive && !tempBook.IsFaulted(tempHarness.ServerSession.PeerSteamId),
                "fault：代际更替关闭旧 scope（临时态随代际清除，后继 scope 重开）");
            ((BetterUnturnedExperience.Core.Network.LocalLoopbackTransport)tempHarness.Pair.Second).DisconnectPeer(1001UL);
            tempHarness.ServerModule.Tick();
            check(!tempBook.IsFaulted(tempHarness.ServerSession.PeerSteamId),
                "fault：断线清内存临时熔断（无磁盘残留）");

            // Corrupt persistence file → fail CLOSED (R10-Standards BLOCKING):
            // a parse failure must degrade the book, never silently open an
            // allow-all scope over a corrupted history.
            System.IO.File.WriteAllText(book.ScopeFilePath, "{\"formatVersion\":2,\"records\":[{\"reason\":\"broken\"");
            var corruptHarness = LitMultiplayerHarness.Create(faultDir);
            corruptHarness.Handshake();
            corruptHarness.EstablishChallenge();
            check(corruptHarness.ServerModule.FaultBook.Degraded
                && !corruptHarness.ServerModule.FaultBook.IsAllowed(corruptHarness.ServerSession.PeerSteamId),
                "fault：持久统计文件损坏 → 解析失败进入全局降级（fail-closed——R10-Standards BLOCKING 修复）");
        }

        private static void LitMultiplayerGroupTidyCompleted(System.Action<bool, string> check)
        {
            var feature = new FeatureId(LitRuntime.FeatureIdValue);
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var events = new List<TidyCompleted>();
            bus.Subscriber(feature).Subscribe<TidyCompleted>(events.Add);
            var module = new InventoryTidyModule(feature);
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var runtime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 1001UL);
            var result = module.Start(new FeatureBootstrap(default(FeatureScopeIdentity), 7UL, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, runtime));
            check(result.Started, "发布：宿主 bootstrap 启动返回 Started=true");
            module.PublishTidyCompleted(3, 3, TidyCommitResult.Committed, 0UL, 42UL);
            check(events.Count == 1 && events[0].Result == TidyCompletionResult.Succeeded
                && events[0].ConnectionGeneration == 0UL && events[0].TransactionId == 42UL
                && events[0].Publisher.Value == LitRuntime.FeatureIdValue,
                "发布：Committed→Succeeded，本地代际 0，事务号与发布者透传");
            module.PublishTidyCompleted(2, 6, TidyCommitResult.Rejected, 5UL, 43UL);
            check(events[1].Result == TidyCompletionResult.Rejected && events[1].FirstPage == 2 && events[1].LastPage == 6
                && events[1].ConnectionGeneration == 5UL,
                "发布：Rejected→Rejected，全页范围（2..6）与联机代际透传");
            module.PublishTidyCompleted(2, 6, TidyCommitResult.CriticalFailure, 5UL, 44UL);
            module.PublishTidyCompleted(2, 6, TidyCommitResult.ConcurrentMutationAfterCommit, 5UL, 45UL);
            check(events[2].Result == TidyCompletionResult.Failed && events[3].Result == TidyCompletionResult.Failed,
                "发布：CriticalFailure/ConcurrentMutationAfterCommit→Failed");
            module.PublishTidyCompleted(2, 2, TidyCommitResult.Committed, 0UL, 0UL);
            check(events[4].TransactionId != 0UL, "发布：事务号 0 由模块代际单调号补齐（事件永不为 0）");
        }

        // DEV-V2-24 F-A red: a challenge send that fails (real machine: one
        // targeted send returned LocalTransportUnavailable while other sends
        // on the same session succeeded) must NOT stick the adoption — the
        // generation leaves the tracked set and the next Tick rediscovers it
        // and re-issues token+challenge. RED while the failed send leaves the
        // session tracked (the client stays challenge-less for the whole
        // session; on machine it produced 80 refusals until disconnect).
        private static void LitMultiplayerGroupChallengeSendFailureRearm(System.Action<bool, string> check)
        {
            var faultDir = NewLitFaultDirectory();
            var harness = LitMultiplayerHarness.Create(faultDir, net => new FlakyFirstSendToClientNetwork(net, 1));
            harness.Handshake();

            harness.ServerModule.Tick();
            harness.Pump();
            check(harness.ClientRawFromServer.Count == 0,
                "挑战重臂：注入失败生效——第一次采纳的 challenge 发送未送达（客户端零收到）");

            harness.ServerModule.Tick();
            harness.Pump();
            check(harness.ClientRawFromServer.Count > 0 && harness.ClientRawFromServer[0][1] == LitTidyWireCodec.MsgSessionChallenge,
                "挑战重臂：发送失败后下一拍重新采纳并重发 challenge（采纳不粘滞，F-A）");

            harness.ClientModule.Tick();
            harness.Pump();
            var request = harness.ClientModule.RequestTidy(3, TidyMode.SameType, true);
            check(request == LitTidyRequestResult.Dispatched,
                "挑战重臂：恢复后客户端请求受理（新 token 全链可用，孤儿 token 已随回滚丢弃）");
        }

        /// <summary>Delegating IBueNetworkApi wrapper whose first N SendToClient
        /// calls fail with LocalTransportUnavailable without delivering — the
        /// transient-transport-failure injector for the challenge-rearm red.</summary>
        private sealed class FlakyFirstSendToClientNetwork : IBueNetworkApi
        {
            private readonly IBueNetworkApi inner;
            private int remainingFailures;

            internal FlakyFirstSendToClientNetwork(IBueNetworkApi inner, int failures)
            {
                this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
                remainingFailures = failures;
            }

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            {
                return inner.RegisterChannel(channel, minimumBueContract, featureVersion);
            }

            public bool UnregisterChannel(FeatureId channel)
            {
                return inner.UnregisterChannel(channel);
            }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            {
                return inner.Subscribe(channel, direction, handler);
            }

            public IReadOnlyList<IConnectionSession> Sessions
            {
                get { return inner.Sessions; }
            }

            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable)
            {
                return inner.SendToServer(channel, payload, reliable);
            }

            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
            {
                return inner.SendToClients(channel, payload, reliable);
            }

            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable)
            {
                if (remainingFailures > 0)
                {
                    remainingFailures--;
                    return NetworkSendResult.LocalTransportUnavailable;
                }
                return inner.SendToClient(channel, session, payload, reliable);
            }
        }

        private static void LitMultiplayerGroupHalfRegistration(System.Action<bool, string> check)
        {
            var feature = new FeatureId(LitRuntime.FeatureIdValue);
            var stub = new HalfRegistrationNetwork();
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var module = new InventoryTidyModule(feature);
            module.NetServiceFactoryForTests = (m, net, book) => new LitTidyNetService(m, net, new FakeLitAuthority(), () => true, book);
            module.Start(new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, stub));
            check(stub.SubscribeCalls == 2 && stub.DisposedHandles == 1 && stub.UnregisterCalls == 1,
                "回滚：第二方向订阅失败 → 已挂句柄释放 + 频道注销（零残留）");
            check(module.NetService != null && !module.NetService.Started,
                "回滚：联机服务显式未启动（本地单人路径不受影响）");
            check(!module.MultiplayerReady,
                "回滚：模块启动面诚实暴露联机未就绪（MultiplayerReady=false，可观察不静默）");
        }

        // DEV-V2-25 red anchor: the LIT targeted-send health surfaces. Three
        // collected groups — the challenge re-arm backoff truth table (first
        // retry immediate = the frozen DEV-V2-24 F-A contract; from the
        // second consecutive failure the interval doubles along the
        // BueNetworkRuntime re-probe precedent 1s → 8s cap, monotone,
        // cleared on success), the per-(generation, send-result-kind)
        // failure rate limiter (first WARN + every 50th with the cumulative
        // count, ONE BUE-LIT-003 link-degraded diagnostic per episode at
        // the threshold, link-recovered on the first success, series reset),
        // and the harness-level sustained-failure run (bounded WARN lines
        // over a storm that on machine produced 8333, monotone attempt
        // gaps, exactly one degradation line, full-chain recovery after the
        // transport returns). RED until the send-health seams exist
        // (compile CS0246, then runtime assertions).
        private static void AssertBueV2LitSendHealth(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                Group("重臂退避真值表", () => LitSendHealthGroupRearmBackoff(Check));
                // DEV-V3-04: the「限频+降级+恢复清零」truth-table group retired
                // with LitSendFailureRateLimiter — link visibility is platform
                // property now (BUE-NET-002/003, anchored in the DEV-V3-04
                // 「链路健康电平」group); the harness group below carries the
                // retirement absence anchor (零逐帧 WARN、零 BUE-LIT-003).
                Group("harness 持续失败全链", () => LitSendHealthGroupSustainedFailureHarness(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V2-25 LIT send health collection: ALL GREEN (0 failures) — groups: 重臂退避真值表/harness 持续失败全链（限频真值表组随 DEV-V3-04 退役）");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-25 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V2-15 red anchor: LIT (inventory tidy) adoption, single-player
        // path. Freezes the four red surfaces the ticket names — strategy
        // replacement on the ITidyStrategy seam, the enabled=false native
        // fallback, the direct algorithm tests (DEV-V5-02: these moved to the
        // unified TaggedRowBandLayout group), and the harness exclusion from
        // the production compile — plus the registration / panel / settings
        // identity. RED until the Lit domain exists
        // (compile CS0246, then runtime assertions).
        private static void AssertLitSingleplayerPath()
        {
            // ── 1. Strategy seam: StrategyId + replacement changes the plan. ──
            // DEV-V5-02: the ONE built-in adapter is now the unified tagged
            // row-band plan (the three-档 default-grid-v1 is retired).
            var defaultStrategy = new TaggedRowBandV1Strategy();
            Assert(defaultStrategy.StrategyId == "tagged-row-band-v1",
                "strategy: the built-in adapter identifies as 'tagged-row-band-v1'");

            var input = new TidyInput(6, 5, true, TidyMode.SameType, new List<PackableItem>
            {
                LitTestItem("a", 2, 1, 10, 0, 5, 0),
                LitTestItem("b", 2, 1, 10, 1, 3, 0),
                LitTestItem("c", 1, 1, 20, 2, 0, 4),
            });
            var plan = defaultStrategy.BuildPlan(input);
            Assert(plan.StrategyId == "tagged-row-band-v1", "strategy: the plan carries the producing adapter's StrategyId");
            Assert(plan.Placements != null && plan.Placements.Count == 3,
                "strategy: the plan always accounts for every input item");
            Assert(plan.AllPlaced, "strategy: the unified plan places every valid item on a loose grid");
            for (var index = 0; index < plan.Placements.Count; index++)
            {
                var placement = plan.Placements[index];
                Assert(placement != null && (string)placement.Tag == LitTestTag(index),
                    "strategy: placement " + index + " preserves the caller's tag identity");
                var width = (placement.ResultRot & 1) == 1 ? placement.size_y : placement.size_x;
                var height = (placement.ResultRot & 1) == 1 ? placement.size_x : placement.size_y;
                Assert(placement.Placed && placement.ResultX + width <= 6 && placement.ResultY + height <= 5,
                    "strategy: placement " + index + " lands inside the grid with its rotated footprint");
            }

            // Replacement: a different adapter answering with a fixed plan IS
            // the plan output — the seam decides, not the solver.
            var fixedPlacements = new List<PackableItem>
            {
                LitTestItem("a", 2, 1, 10, 0, 5, 0),
                LitTestItem("b", 2, 1, 10, 1, 3, 0),
                LitTestItem("c", 1, 1, 20, 2, 0, 4),
            };
            fixedPlacements[0].ResultX = 0; fixedPlacements[0].ResultY = 0; fixedPlacements[0].ResultRot = 0; fixedPlacements[0].Placed = true;
            fixedPlacements[1].Placed = false;
            fixedPlacements[2].Placed = false;
            var replacement = new FixedPlanStrategyAdapter("test-fixed-v1",
                new TidyPlan("test-fixed-v1", fixedPlacements, allPlaced: false));
            var replacedPlan = replacement.BuildPlan(input);
            Assert(replacedPlan.StrategyId == "test-fixed-v1" && replacedPlan.Placements.Count == 3
                && replacedPlan.Placements[0].ResultX == 0 && !replacedPlan.AllPlaced,
                "strategy: replacing the adapter changes the plan output for the same input (seam, not solver, decides)");
            var replacedAgain = defaultStrategy.BuildPlan(input);
            Assert(replacedAgain.StrategyId == "tagged-row-band-v1" && replacedAgain.AllPlaced,
                "strategy: the default adapter still answers with its own plan after the replacement probe");

            // ── 1b. (DEV-V5-02) 纯算法直测已迁到统一排版模块的独立红测组：
            // AssertDevV502TaggedRowBand 直接测 TaggedRowBandLayout.TryPlanLayout
            // （确定性/硬性不变量/失败零提交/规格列举输入类型），本组只留策略缝。
            // The old default-grid-v1 direct tests retired with the solver.

            // ── 2. ManualTidyService consumes the strategy (service-level seam). ──
            LitRuntime.MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var page = new Items(7);
            typeof(Items).GetField("_width", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(page, (byte)4);
            typeof(Items).GetField("_height", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(page, (byte)4);
            var jar = CreateTestItemJar(3, 0, 0, 1, 1);
            SetJarItem(jar, new Item(1, 1, 100, new byte[0]));
            page.items.Add(jar);
            var countBefore = page.getItemCount();

            // A strategy whose plan places nothing must fail Prepare with zero
            // mutation — the service follows the plan it was handed.
            var rejectPlacements = new List<PackableItem>
            {
                LitTestItem("j", 1, 1, 1, 0, 3, 0),
            };
            rejectPlacements[0].Placed = false;
            var rejectStrategy = new FixedPlanStrategyAdapter("test-reject-v1",
                new TidyPlan("test-reject-v1", rejectPlacements, allPlaced: false));
            var rejected = ManualTidyService.TidyPage(page, 3, true, TidyMode.SameType, null, rejectStrategy);
            Assert(rejected.Result == TidyCommitResult.Rejected && !rejected.MutationStarted,
                "service: an all-unplaced plan fails Prepare as Rejected with zero mutation");
            Assert(page.getItemCount() == countBefore,
                "service: the rejected plan leaves the page untouched");

            // A null strategy is a developer error (fail-fast), not a silent
            // fallback to some hidden default.
            var nullStrategyThrown = false;
            try { ManualTidyService.TidyPage(page, 3, true, TidyMode.SameType, null, null); }
            catch (ArgumentNullException) { nullStrategyThrown = true; }
            Assert(nullStrategyThrown, "service: a null strategy throws ArgumentNullException (no hidden default)");

            // ── 3. lifecycle-gated tidy serving (DEV-V4-06: the legacy enabled
            // master switch is RETIRED — the lifecycle machine is the only
            // switch; the module schema is the mode/direction choice pair). ──
            var settingsFeature = new FeatureId("io.github.yu80rice.bue.inventory-tidy");
            // DEV-V3-06: the settings authority is the host-owned registry
            // (single source); the module consumes the very values a panel
            // write lands on, through the same injected scoped view.
            var litSettings = new BetterUnturnedExperience.Core.Settings.FeatureSettingsRegistry(new InMemorySettingsPersistence(), () => true, null);
            litSettings.GetOrCreateRuntime(settingsFeature, InventoryTidyModule.CreateSettingsDescriptors(settingsFeature));
            litSettings.OpenGeneration(settingsFeature, 1UL);
            var settingsView = litSettings.CreateView(settingsFeature, 1UL);
            var module = new InventoryTidyModule(settingsFeature);
            module.AttachSettingsView(settingsView);
            Assert(module.Feature.Value == "io.github.yu80rice.bue.inventory-tidy",
                "module: the feature identity is the frozen LIT FeatureId");
            Assert(module.Strategy.StrategyId == "tagged-row-band-v1",
                "module: the module default strategy is the built-in adapter (DEV-V5-02: the unified tagged row-band plan)");
            Assert(!module.PatchesInstalled,
                "module: construction installs no Harmony patches (installation is an explicit start step)");
            Assert(module.RequestLocalTidy(3, TidyMode.SameType, true) == LitTidyRequestResult.NativeFallback,
                "module: before start the tidy request falls back to native (not started, no patches)");

            module.EnsureStarted();
            Assert(module.PatchesInstalled || module.StartGateDiagnostics.Length > 0,
                "module: start installs the UI patch or records the environment gate diagnostic (no silent state)");

            // DEV-V4-06: the click seam reads the SAVED ClientPreference
            // snapshot (never the panel draft, never per-page memory) — the
            // descriptor defaults serve an empty store. DEV-V5-02: only the
            // direction default remains (降序 stable-finish); the mode output
            // is the frozen wire placeholder, no longer a read of any档.
            Assert(module.TryReadSavedTidyPreference(out var savedMode, out var savedDescending, out _)
                && savedMode == TidyMode.SameType && savedDescending,
                "module: the saved-preference read serves the schema default (降序) on an empty store (DEV-V5-02: mode is the wire placeholder, not an算法档)");

            module.FaultGate.Open("host-red-test", restoreVerified: false);
            Assert(module.RequestLocalTidy(3, TidyMode.SameType, true) == LitTidyRequestResult.RejectedFaultCircuit,
                "fault gate: an open circuit rejects tidy requests without dispatching work");

            // Dispatch path: a started module with a closed gate enqueues the
            // local tidy work for the main-thread pump.
            MainThreadDispatcher.ResetForTests();
            module.FaultGate.Reset();
            Assert(module.RequestLocalTidy(3, TidyMode.SameType, true) == LitTidyRequestResult.Dispatched,
                "dispatch: a started module with a closed gate dispatches the local tidy work");
            Assert(MainThreadDispatcher.PendingCount == 1, "dispatch: exactly one work item is queued");
            module.Tick();
            Assert(MainThreadDispatcher.PendingCount == 0,
                "dispatch: the pump drains the queue on the bound main thread (host: no local player, work no-ops safely)");

            // Stop = three phases: quiesce, dispatcher shutdown, full teardown.
            Assert(module.RequestLocalTidy(2, TidyMode.SameType, true) == LitTidyRequestResult.Dispatched,
                "setup: one more work item before the stop");
            module.Stop(FeatureStopReason.PluginStopping);
            Assert(module.ShuttingDown && MainThreadDispatcher.PendingCount == 0 && !module.PatchesInstalled,
                "stop: quiesce + dispatcher drain + patch teardown all observed");
            Assert(module.RequestLocalTidy(3, TidyMode.SameType, true) == LitTidyRequestResult.NativeFallback,
                "stop: a stopped module answers tidy requests with the native fallback");

            // DEV-V4-06 re-arm: the SAME wired instance re-enables through the
            // panel's enable seam (a new-generation Start) — the stop boundary
            // must not leave sticky shut-down state in the module.
            MainThreadDispatcher.ResetForTests();
            var rearmBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var rearmPair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var rearmNetwork = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(rearmPair.First, new ContractVersion(2, 0), 1901UL);
            module.ScopeDirectoryForTests = NewLitFaultDirectory();
            module.FaultContextForTests = () => new LitFaultScopeContext("TestMap", 1);
            var rearmStart = module.Start(new FeatureBootstrap(default(FeatureScopeIdentity), 2UL, settingsView,
                rearmBus.Subscriber(settingsFeature), rearmBus.Publisher(settingsFeature),
                rearmBus.EventRegistry(settingsFeature), null, null, null, rearmNetwork));
            Assert(rearmStart.Started, "re-arm setup: the module generation restarts through Start");
            Assert(!module.ShuttingDown,
                "re-arm: the stop boundary does not leak into the new module generation");
            Assert(module.RequestLocalTidy(3, TidyMode.SameType, true) == LitTidyRequestResult.Dispatched,
                "re-arm: a re-enabled module generation serves tidy requests again (no sticky stopped state)");

            // ── 4. Harness and old-plugin types are excluded from production. ──
            // Compile-list level (the ticket's literal wording: 排除出生产编译列表):
            // the Plugin csproj's <Compile Include> set must carry zero fixture /
            // old-plugin sources and zero harness define. Located by walking up to
            // the solution root — a miss is a failure, never a silent skip.
            var solutionRoot = new DirectoryInfo(AppContext.BaseDirectory);
            while (solutionRoot != null && !File.Exists(Path.Combine(solutionRoot.FullName, "BetterUnturnedExperience.sln")))
                solutionRoot = solutionRoot.Parent;
            Assert(solutionRoot != null, "exclusion: solution root located from the test base directory");
            var pluginCsproj = File.ReadAllText(Path.Combine(solutionRoot.FullName, "src", "BetterUnturnedExperience.Plugin", "BetterUnturnedExperience.Plugin.csproj"));
            Assert(pluginCsproj.Contains("EmbeddedLit\\Layout\\TaggedRowBandLayout.cs"),
                "exclusion: the Plugin compile list is the real production list (positive control: migrated Lit sources present)");
            var excludedSources = new[]
            {
                "AutoTestDriver.cs", "CommandTidyAutoTest.cs", "CommandTidyFaults.cs", "CommandTidyFaultInjectionTest.cs",
                "FaultInjectionTestRunner.cs", "FixtureValidator.cs", "TestFixtureSession.cs", "NetworkTestProbe.cs",
                "ShutdownTestProbe.cs", "ShutdownBarrier.cs", "ConvergenceCheckBehaviour.cs", "HotkeyResultWaitBehaviour.cs",
                "IndependentSnapshot.cs", "LmnDependencyGuard.cs", "LaunchInventoryTidyPlugin.cs", "ManualTidyNetwork.cs",
                "ItemsTryAddItemPatch.cs", "ManualTidyWatcher.cs", "ClientSessionNonce.cs", "ServerSessionRegistry.cs",
                "RequestAdmissionStore.cs", "PlayerOperationGate.cs", "TidyTransaction.cs", "TidyFaultCircuit.cs",
                "TidyFaultCircuitPersistence.cs", "TidyAdminAuth.cs",
            };
            foreach (var excluded in excludedSources)
            {
                Assert(!pluginCsproj.Contains(excluded),
                    "exclusion: fixture/old-plugin source '" + excluded + "' is not in the production compile list");
            }
            Assert(!pluginCsproj.Contains("TIDY_TEST_HARNESS"),
                "exclusion: the harness define is gone from the production build definition");

            // Artifact level: the built production assembly carries none of them.
            var production = typeof(BetterUnturnedExperiencePlugin).Assembly;
            var excludedNames = new[]
            {
                "AutoTestDriver", "CommandTidyAutoTest", "CommandTidyFaults", "CommandTidyFaultInjectionTest",
                "CommandTidyUnfault", "CommandTidyFaultRecover", "FaultInjectionTestRunner", "FixtureValidator",
                "TestFixtureSession", "NetworkTestProbe", "ShutdownTestProbe", "ShutdownBarrier",
                "ConvergenceCheckBehaviour", "HotkeyResultWaitBehaviour", "IndependentSnapshot",
                "LmnDependencyGuard", "LaunchInventoryTidyPlugin", "ManualTidyNetwork", "ItemsTryAddItemPatch",
            };
            var productionTypeNames = new HashSet<string>();
            var namespaceLeaks = new List<string>();
            foreach (var type in production.GetTypes())
            {
                productionTypeNames.Add(type.Name);
                // Compiler-generated types carry a null namespace — guard, don't NRE.
                if (type.Namespace != null && type.Namespace.StartsWith("LaunchInventoryTidy", StringComparison.Ordinal))
                    namespaceLeaks.Add(type.FullName);
            }
            Assert(namespaceLeaks.Count == 0,
                "exclusion: no type keeps the old plugin namespace in the production assembly (found " + string.Join(",", namespaceLeaks) + ")");
            foreach (var excluded in excludedNames)
            {
                Assert(!productionTypeNames.Contains(excluded),
                    "exclusion: harness/old-plugin type '" + excluded + "' is not in the production compile");
            }

            // ── 5. Registration, panel entry and settings identity. ──
            var runtime = new FeatureRegistrationRuntime();
            runtime.OpenRegistration();
            var registration = InventoryTidyFeatureRegistration.CreateRegistration();
            Assert(PayloadText(registration.Definition) == "BUE-LIT-V1",
                "registration: the definition payload is exactly the documented 'BUE-LIT-V1' text");
            Assert(registration.Definition.Feature.Value == "io.github.yu80rice.bue.inventory-tidy",
                "registration: the feature identity is the frozen LIT FeatureId");
            Assert(registration.MinimumBueContract.Major == 2 && registration.MinimumBueContract.Minor == 0,
                "registration: the minimum contract aligns with the (2,0) gate");
            var registered = runtime.Register(registration);
            Assert(registered.Accepted,
                "registration: the LIT definition is accepted by the real registration runtime (got "
                + registered.Reason + " " + registered.DiagnosticId + ")");

            var previousRuntime = BueRuntimeHost.CurrentRuntime;
            try
            {
                var hostRuntime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(hostRuntime);
                hostRuntime.OpenRegistration();
                Assert(BetterItemInteractionFeatureRegistration.Register().Accepted,
                    "setup: the official BII registration is accepted through the host bridge");
                Assert(BueRuntimeHost.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                    "setup: the official LIT registration is accepted through the host bridge");
                var composition = new BueClientUiCompositionRoot();
                Assert(composition.Initialize(false, false, true), "setup: the composition initializes");
                Assert(hostRuntime.CompleteRuntime(), "setup: the host barrier completes");
                composition.RefreshManagementPanel();
                Assert(HasManagementEntry(composition.ManagementPanel.Model.GetEntries(),
                        "io.github.yu80rice.bue.inventory-tidy", "背包整理"),
                    "panel: the catalog projects the LIT entry under the official Chinese display name 背包整理");
                Assert(HasManagementEntry(composition.ManagementPanel.Model.GetEntries(),
                        "io.github.yu80rice.bue.better-item-interaction", "更好的物品交互"),
                    "panel: the catalog projects the BII entry under the official Chinese display name 更好的物品交互 (DEV-V2-24 D0-b, spec story 3)");
                composition.Destroy();
            }
            finally
            {
                BueRuntimeHost.Bind(previousRuntime);
            }
        }

        // DEV-V5-02 (V5-T3) 统一标签分段行带排版：红测先行组。断言面 = 冻结标签
        // 表、分类器纯映射（含弹药箱按 FillTargetItem 蓝图关系认）、TryPlanLayout
        // 硬性不变量（不重叠/不越界/不丢物/确定性/失败零提交/规划不改输入）、
        // 规格列举输入类型（同 ID 医疗堆、细长混放、并排、换行、旋转改善 vs 更差、
        // 「其他」、弹药箱 vs SUPPLY、放得下却重叠失败不得再现）、官方先行消费
        // （模块默认策略+PreparePage 标签填充+旧三档不再决定算法）、设置迁移
        // （mode 从 schema/面板退役、旧档可读不决定算法、下次保存归一），以及
        // 生产编译列表与程序集类型层面的旧三套退役。
        private static void AssertDevV502TaggedRowBand()
        {
            // ── 1. 冻结标签表：20 档、固定序，不是 EItemType 枚举序 ──
            Assert(PlayerUseLabels.Count == 20, "labels: the frozen player-use table has exactly 20 segments");
            Assert((int)PlayerUseLabel.RangedWeapon == 0 && (int)PlayerUseLabel.Magazine == 1
                && (int)PlayerUseLabel.Medical == 2 && (int)PlayerUseLabel.Melee == 3
                && (int)PlayerUseLabel.Throwable == 4 && (int)PlayerUseLabel.Ammo == 5
                && (int)PlayerUseLabel.AmmoBox == 6 && (int)PlayerUseLabel.GunAttachment == 7
                && (int)PlayerUseLabel.Food == 8 && (int)PlayerUseLabel.Drink == 9
                && (int)PlayerUseLabel.Tool == 10 && (int)PlayerUseLabel.ClothingArmor == 11
                && (int)PlayerUseLabel.BackpackContainer == 12 && (int)PlayerUseLabel.Fuel == 13
                && (int)PlayerUseLabel.SupplyCraft == 14 && (int)PlayerUseLabel.Build == 15
                && (int)PlayerUseLabel.FacilityTrap == 16 && (int)PlayerUseLabel.MapCompass == 17
                && (int)PlayerUseLabel.Key == 18 && (int)PlayerUseLabel.Other == 19,
                "labels: 远程武器、弹匣、医疗用品、近战武器、投掷物、弹药、弹药箱、枪械配件、食物、饮水、工具、服装与防护装备、背包与容器、燃料、补给与制作材料、建造、设施与陷阱、地图与指南针、钥匙、其他 (V5-T3 frozen order)");
            Assert(PlayerUseLabels.DiagnosticName(PlayerUseLabel.AmmoBox) == "弹药箱"
                && PlayerUseLabels.DiagnosticName(PlayerUseLabel.Other) == "其他",
                "labels: diagnostics names land in the player language (internal surface only, never a control)");

            // ── 2. 分类器纯映射（T3 四条裁决之一：Item → PlayerUseLabel 内部缝）──
            // 明确类型优先：
            Assert(DevV502Classify(EItemType.GUN) == PlayerUseLabel.RangedWeapon
                && DevV502Classify(EItemType.MELEE) == PlayerUseLabel.Melee
                && DevV502Classify(EItemType.MEDICAL) == PlayerUseLabel.Medical
                && DevV502Classify(EItemType.THROWABLE) == PlayerUseLabel.Throwable
                && DevV502ClassifyCaliber() == PlayerUseLabel.Ammo
                && DevV502Classify(EItemType.FOOD) == PlayerUseLabel.Food
                && DevV502Classify(EItemType.WATER) == PlayerUseLabel.Drink
                && DevV502Classify(EItemType.FUEL) == PlayerUseLabel.Fuel
                && DevV502Classify(EItemType.KEY) == PlayerUseLabel.Key,
                "classifier: explicit EItemType answers win (远程/近战/医疗/投掷/弹药/食物/饮水/燃料/钥匙)");
            // 弹匣不能把所有 MAGAZINE 无条件当可用弹匣：
            Assert(DevV502Classify(EItemType.MAGAZINE, true, false) == PlayerUseLabel.Magazine
                && DevV502Classify(EItemType.MAGAZINE, false, false) == PlayerUseLabel.Other,
                "classifier: MAGAZINE counts as 弹匣 only with a real ItemMagazineAsset — never unconditionally");
            // 弹药箱按给弹匣供弹的蓝图关系认，不单看 SUPPLY；AMMO 显式类型优先：
            Assert(DevV502Classify(EItemType.SUPPLY, false, true) == PlayerUseLabel.AmmoBox
                && DevV502Classify(EItemType.SUPPLY, false, false) == PlayerUseLabel.SupplyCraft
                && DevV502Classify(EItemType.BOX, false, true) == PlayerUseLabel.AmmoBox
                && DevV502Classify(EItemType.BOX, false, false) == PlayerUseLabel.Other
                && DevV502ClassifyCaliber(isFillSupply: true) == PlayerUseLabel.Ammo,
                "classifier: 弹药箱 = FillTargetItem fill relation (SUPPLY/BOX in the magazine-supplies set, never SUPPLY alone); a caliber loose-round stays 弹药 even when it is a fill supply");
            // 配件族与其余族段：
            Assert(DevV502Classify(EItemType.SIGHT) == PlayerUseLabel.GunAttachment
                && DevV502Classify(EItemType.OPTIC) == PlayerUseLabel.GunAttachment
                && DevV502Classify(EItemType.TACTICAL) == PlayerUseLabel.GunAttachment
                && DevV502Classify(EItemType.GRIP) == PlayerUseLabel.GunAttachment
                && DevV502Classify(EItemType.BARREL) == PlayerUseLabel.GunAttachment
                && DevV502Classify(EItemType.HAT) == PlayerUseLabel.ClothingArmor
                && DevV502Classify(EItemType.VEST) == PlayerUseLabel.ClothingArmor
                && DevV502Classify(EItemType.BACKPACK) == PlayerUseLabel.BackpackContainer
                && DevV502Classify(EItemType.TOOL) == PlayerUseLabel.Tool
                && DevV502Classify(EItemType.BARRICADE) == PlayerUseLabel.Build
                && DevV502Classify(EItemType.STRUCTURE) == PlayerUseLabel.Build
                && DevV502Classify(EItemType.TRAP) == PlayerUseLabel.FacilityTrap
                && DevV502Classify(EItemType.MAP) == PlayerUseLabel.MapCompass
                && DevV502Classify(EItemType.COMPASS) == PlayerUseLabel.MapCompass,
                "classifier: attachment/clothing/backpack/tool/build/facility/map families map per the frozen table");
            // 分类失败/无可靠类型 → 其他（弹药箱 vs SUPPLY 的另一半：工坊杂项不猜用途）：
            Assert(DevV502ClassifyUnknownType() == PlayerUseLabel.Other
                && DevV502Classify(EItemType.CLOUD) == PlayerUseLabel.Other
                && DevV502Classify(EItemType.LIBRARY) == PlayerUseLabel.Other
                && DevV502Classify(EItemType.TIRE) == PlayerUseLabel.Other,
                "classifier: items without a reliable type land in 其他 — never dropped (材料/任务物品裁决)");

            // ── 3. TryPlanLayout 硬性不变量（V5-T3：硬性先于视觉）──
            // 3a 确定性：相同输入两轮，逐件坐标/旋转一致。
            var detItemsA = DevV502FixtureMixedLabels();
            var detItemsB = DevV502FixtureMixedLabels();
            bool detOkA = TaggedRowBandLayout.TryPlanLayout(6, 4, true, detItemsA, out var detPlanA, out _);
            bool detOkB = TaggedRowBandLayout.TryPlanLayout(6, 4, true, detItemsB, out var detPlanB, out _);
            Assert(detOkA && detOkB, "layout: the mixed-label fixture plans fully on a 6x4 (能放下必须放下)");
            Assert(detPlanA.Count == detPlanB.Count, "layout: determinism keeps the entry count");
            for (var i = 0; i < detPlanA.Count; i++)
                Assert(detPlanA[i].ResultX == detPlanB[i].ResultX && detPlanA[i].ResultY == detPlanB[i].ResultY
                    && detPlanA[i].ResultRot == detPlanB[i].ResultRot,
                    "layout: identical input produces an identical plan (deterministic)");

            // 3b 规划不改真实背包：TryPlanLayout 不得写入输入 items（克隆出参）。
            var untouched = new List<PackableItem> { DevV502Item("u1", 2, 1, PlayerUseLabel.RangedWeapon, 10, 0, 3, 0, 0, 0) };
            untouched[0].Placed = true; untouched[0].ResultX = 7; untouched[0].ResultY = 6;
            TaggedRowBandLayout.TryPlanLayout(6, 4, true, untouched, out var clonePlan, out _);
            Assert(untouched[0].Placed && untouched[0].ResultX == 7 && untouched[0].ResultY == 6,
                "layout: TryPlanLayout never writes the caller's items (规划不改真实背包)");
            Assert(!ReferenceEquals(clonePlan[0], untouched[0]) && (string)clonePlan[0].Tag == "u1",
                "layout: the plan is a fresh clone list carrying the caller's tags");

            // 3c 失败零提交：放不下 → false + 全部 Placed=false + 明确原因。
            bool tightOk = TaggedRowBandLayout.TryPlanLayout(2, 2, true, new List<PackableItem>
            {
                DevV502Item("t1", 2, 2, PlayerUseLabel.Food, 1, 0, 0, 0, 0, 0),
                DevV502Item("t2", 1, 1, PlayerUseLabel.Drink, 2, 0, 0, 0, 0, 0),
            }, out var tightPlan, out var tightReason);
            Assert(!tightOk, "layout: a grid that cannot hold every valid item fails explicitly");
            Assert(tightPlan != null && tightPlan.Count == 2, "layout: a failed plan still accounts for every input item");
            Assert(!tightPlan[0].Placed && !tightPlan[1].Placed,
                "layout: failure answers with ZERO placements — never a half-committed layout");
            Assert(!string.IsNullOrEmpty(tightReason), "layout: the failure carries an explicit reason");

            // 3d/3e 空页平凡成功；零尺寸异常件保留未放置但整页成功——「全部放置
            // 或明确失败」按 T3 在合法件（尺寸为正）上量化（迁入求解器既有契约，
            // 服务层未放置计数同口径 size>0 才计），异常件由调用方原位保留=不丢物。
            Assert(TaggedRowBandLayout.TryPlanLayout(4, 4, true, new List<PackableItem>(), out var emptyPlan, out _)
                && emptyPlan.Count == 0,
                "layout: an empty page plans trivially");
            Assert(TaggedRowBandLayout.TryPlanLayout(4, 4, true, new List<PackableItem>
            {
                DevV502Item("ok", 1, 1, PlayerUseLabel.Medical, 1, 0, 0, 0, 0, 0),
                DevV502Item("zero", 0, 0, PlayerUseLabel.Medical, 2, 0, 0, 0, 0, 0),
            }, out var zeroPlan, out _)
                && zeroPlan.Count == 2 && zeroPlan[0].Placed && !zeroPlan[1].Placed,
                "layout: a zero-size anomaly stays unplaced (valid-items contract) and nothing is dropped by the plan");

            // 3f 超限件：任何朝向都放不进 → 整单失败（无半成品）。
            Assert(!TaggedRowBandLayout.TryPlanLayout(3, 3, true, new List<PackableItem>
            {
                DevV502Item("big", 4, 4, PlayerUseLabel.BackpackContainer, 1, 0, 0, 0, 0, 0),
                DevV502Item("small", 1, 1, PlayerUseLabel.Medical, 2, 0, 0, 0, 0, 0),
            }, out var bigPlan, out _)
                && !bigPlan[0].Placed && !bigPlan[1].Placed,
                "layout: an oversized item fails the whole plan (all-or-nothing), keeping every entry unplaced");

            // 硬性验证器本身跑在核心夹具上（不重叠/不越界/每件一次）。
            Assert(DevV502ValidatePlan(detPlanA, 6, 4, detItemsA.Count),
                "layout: the mixed-label plan satisfies every hard invariant");

            // ── 4. 规格列举输入类型（V5-T3 Q4：先准备这些输入类型）──
            // 4a 同 ID 医疗堆：同组件在段内连续（左到右一行，段不穿插）。
            Assert(TaggedRowBandLayout.TryPlanLayout(6, 3, true, new List<PackableItem>
            {
                DevV502Item("m1", 1, 1, PlayerUseLabel.Medical, 500, 0, 0, 0, 0, 0),
                DevV502Item("m2", 1, 1, PlayerUseLabel.Medical, 500, 0, 1, 0, 0, 0),
                DevV502Item("m3", 1, 1, PlayerUseLabel.Medical, 500, 0, 2, 0, 0, 0),
            }, out var stackPlan, out _), "layout: the medical stack places every item");
            Assert(DevV502ValidatePlan(stackPlan, 6, 3, 3)
                && DevV502AllOnRow(stackPlan, 0)
                && new HashSet<byte> { stackPlan[0].ResultX, stackPlan[1].ResultX, stackPlan[2].ResultX }.Count == 3,
                "layout: same-ID stack lands in one row segment (同 ID 在同标签内尽量连续)");

            // 4b 细长混放：左侧主体决定行带高，细长高件不得把同类挤成难看断裂。
            // 6x4: 注射器 1x3、绷带 2x1 ×2、纱布 1x1 ×3。
            var slimItems = new List<PackableItem>
            {
                DevV502Item("syringe", 1, 3, PlayerUseLabel.Medical, 501, 1, 0, 0, 0, 0),
                DevV502Item("band-a", 2, 1, PlayerUseLabel.Medical, 502, 1, 0, 0, 0, 0),
                DevV502Item("band-b", 2, 1, PlayerUseLabel.Medical, 502, 1, 0, 0, 0, 0),
                DevV502Item("gauze-a", 1, 1, PlayerUseLabel.Medical, 503, 1, 0, 0, 0, 0),
                DevV502Item("gauze-b", 1, 1, PlayerUseLabel.Medical, 503, 1, 0, 0, 0, 0),
                DevV502Item("gauze-c", 1, 1, PlayerUseLabel.Medical, 503, 1, 0, 0, 0, 0),
            };
            Assert(TaggedRowBandLayout.TryPlanLayout(6, 4, true, slimItems, out var slimPlan, out _)
                && DevV502ValidatePlan(slimPlan, 6, 4, 6),
                "layout: the mixed-slimness same-label fixture places fully");
            var syringe = DevV502FindByTag(slimPlan, "syringe");
            var bandA = DevV502FindByTag(slimPlan, "band-a");
            Assert(syringe.Placed && syringe.ResultRot == 0 && syringe.ResultY == 0
                && bandA.ResultY == 0 && bandA.ResultX > syringe.ResultX,
                "layout: the tall slim item forms the left body of the band and the regular items run to its right (主体决定行带高，细长件不制造断裂)");
            Assert(syringe.ResultX == 0 && bandA.ResultX == 1,
                "layout: the medical band is anchored at the grid left edge with the anchor flush first");

            // 4c 可横向并排的多标签：一行带内 食物→饮水→工具，边界整体单调。
            Assert(TaggedRowBandLayout.TryPlanLayout(6, 2, true, new List<PackableItem>
            {
                DevV502Item("f1", 2, 1, PlayerUseLabel.Food, 600, 1, 0, 0, 0, 0),
                DevV502Item("d1", 1, 1, PlayerUseLabel.Drink, 601, 1, 0, 0, 0, 0),
                DevV502Item("d2", 1, 1, PlayerUseLabel.Drink, 601, 1, 0, 0, 0, 0),
                DevV502Item("t1", 2, 1, PlayerUseLabel.Tool, 602, 1, 0, 0, 0, 0),
            }, out var sidePlan, out _), "layout: the side-by-side labels fixture plans fully");
            Assert(DevV502AllOnRow(sidePlan, 0),
                "layout: labels sharing a row band sit side by side in one band (横向并排可计算)");
            Assert(DevV502RowMajorLabelsMonotone(sidePlan),
                "layout: the side-by-side boundary is monotone (不同标签不穿插，整体单调)");

            // 4d 右侧放不下必须换行：新标签从下一行最左侧开始，不回填补洞。
            Assert(TaggedRowBandLayout.TryPlanLayout(3, 2, true, new List<PackableItem>
            {
                DevV502Item("wrap-food", 2, 1, PlayerUseLabel.Food, 600, 1, 0, 0, 0, 0),
                DevV502Item("wrap-drink", 2, 1, PlayerUseLabel.Drink, 601, 1, 0, 0, 0, 0),
            }, out var wrapPlan, out _), "layout: the wrap fixture places both items");
            var wrapFood = DevV502FindByTag(wrapPlan, "wrap-food");
            var wrapDrink = DevV502FindByTag(wrapPlan, "wrap-drink");
            Assert(wrapFood.ResultY == 0 && wrapFood.ResultX == 0
                && wrapDrink.ResultY == 1 && wrapDrink.ResultX == 0,
                "layout: a label that cannot fit to the right continues on the NEXT row at the leftmost column (换行规则)");

            // 4e 有旋转才能放下：1x2 装进 2x1 网格 → rot=1，脚印正确。
            Assert(TaggedRowBandLayout.TryPlanLayout(2, 1, true, new List<PackableItem>
            {
                DevV502Item("rot-need", 1, 2, PlayerUseLabel.Medical, 700, 1, 0, 0, 0, 0),
            }, out var rotPlan, out _) && rotPlan[0].Placed && rotPlan[0].ResultRot == 1
                && DevV502ValidatePlan(rotPlan, 2, 1, 1),
                "layout: rotation is used when it is the only way to avoid a drop (避免漏放才转)");

            // 4f 不旋转也能放下但旋转更差：全正向保持，一次都不转。
            Assert(TaggedRowBandLayout.TryPlanLayout(4, 2, true, new List<PackableItem>
            {
                DevV502Item("keep-a", 1, 2, PlayerUseLabel.Medical, 701, 1, 0, 0, 0, 0),
                DevV502Item("keep-b", 1, 2, PlayerUseLabel.Medical, 701, 1, 0, 0, 0, 0),
                DevV502Item("keep-c", 2, 1, PlayerUseLabel.Medical, 702, 1, 0, 0, 0, 0),
            }, out var keepPlan, out _) && keepPlan.Count == 3 && DevV502AllPlaced(keepPlan)
                && DevV502AllUnrotated(keepPlan) && DevV502ValidatePlan(keepPlan, 4, 2, 3),
                "layout: forward orientation is kept whenever it fits (默认正向，不为整洁而转)");

            // 4g/4j 标签不穿插 + 「其他」收尾：乱序输入 → 行主序标签非降。
            var mixedPlan = detPlanA;
            Assert(DevV502RowMajorLabelsMonotone(mixedPlan),
                "layout: shuffled input lands as non-decreasing label segments (标签分段从左上到右下)");
            Assert(DevV502FindByTag(mixedPlan, "x-other").ResultY
                    >= DevV502FindByTag(mixedPlan, "x-med").ResultY,
                "layout: the 其他 segment sits at the tail of the order (分类失败收进段尾)");

            // 4m 救济路守「旋转只为避免漏放」：正向放不下的件在 bottom-left 以
            // 旋转脚印落地（rot=1），而正向可放件绝不会被旋转（4f 已钉）。
            Assert(TaggedRowBandLayout.TryPlanLayout(3, 1, true, new List<PackableItem>
            {
                DevV502Item("rescue-rot", 1, 3, PlayerUseLabel.Medical, 703, 1, 0, 0, 0, 0),
            }, out var rescuePlan, out _) && rescuePlan[0].Placed && rescuePlan[0].ResultRot == 1
                && rescuePlan[0].ResultX == 0 && rescuePlan[0].ResultY == 0,
                "layout: the rescue rotates ONLY the item that cannot fit otherwise (旋转只为避免漏放)");

            // 4i 「放得下却重叠失败」不得再现：V2-15 验收级拥挤页，成功且无重叠。
            Assert(TaggedRowBandLayout.TryPlanLayout(5, 5, true, new List<PackableItem>
            {
                DevV502Item("cr-1", 2, 3, PlayerUseLabel.RangedWeapon, 801, 1, 0, 0, 0, 0),
                DevV502Item("cr-2", 3, 2, PlayerUseLabel.RangedWeapon, 802, 1, 0, 0, 0, 0),
                DevV502Item("cr-3", 2, 2, PlayerUseLabel.Melee, 803, 1, 0, 0, 0, 0),
                DevV502Item("cr-4", 1, 1, PlayerUseLabel.Ammo, 804, 1, 0, 0, 0, 0),
                DevV502Item("cr-5", 1, 1, PlayerUseLabel.Ammo, 804, 1, 0, 0, 0, 0),
            }, out var crowdPlan, out _) && DevV502AllPlaced(crowdPlan)
                && DevV502ValidatePlan(crowdPlan, 5, 5, 5),
                "layout: a crowded-but-feasible page plans legally (旧「放得下却重叠失败」类不得再现)");

            // 4p 精确重排层的完备性双面：4×4 上 2×3+2×3+2×2 面积恰满 16 但
            // 几何不可行（两个 2×3 平行后只剩 4×1/1×4 条）——求解器必须证明
            // 不可行并零提交，不得虚报可行。
            Assert(!TaggedRowBandLayout.TryPlanLayout(4, 4, true, new List<PackableItem>
            {
                DevV502Item("r5d", 2, 3, PlayerUseLabel.Food, 971, 1, 0, 0, 0, 0),
                DevV502Item("r5e", 2, 3, PlayerUseLabel.Food, 972, 1, 0, 0, 0, 0),
                DevV502Item("r5f", 2, 2, PlayerUseLabel.Food, 973, 1, 0, 0, 0, 0),
            }, out var r5Infeasible, out var r5Reason)
                && !DevV502AnyPlaced(r5Infeasible) && !string.IsNullOrEmpty(r5Reason),
                "layout: area-full-but-geometrically-infeasible page is PROVED and refused with zero placements (精确层完备性)");

            // 4o 占角型可行反例（R5 常驻化）：4×4 上 1×3+2×2+2×3——单一固定
            // 大件优先 BL 会先放 2×3 占角致 2×2 无处可放；可行布局
            // 1×3(0,0)、2×2(1,0)、2×3(1,2)。整单重排多确定性序族必须拿下。
            Assert(TaggedRowBandLayout.TryPlanLayout(4, 4, true, new List<PackableItem>
            {
                DevV502Item("r5a", 1, 3, PlayerUseLabel.Medical, 961, 1, 0, 0, 0, 0),
                DevV502Item("r5b", 2, 2, PlayerUseLabel.Medical, 962, 1, 0, 0, 0, 0),
                DevV502Item("r5c", 2, 3, PlayerUseLabel.Medical, 963, 1, 0, 0, 0, 0),
            }, out var r5Plan, out _) && DevV502AllPlaced(r5Plan) && DevV502ValidatePlan(r5Plan, 4, 4, 3),
                "layout: the corner-blocking counterexample (1x3 + 2x2 + 2x3 on 4x4) is never refused (能放下必须放下·R5 反例回归)");

            // 4n 贪心失败但整体可行不得误拒（R4 反例常驻化）：4×4 上
            // 1×2 + 2×3 + 2×3 需救济路旋转才全放；同标签与跨标签两种流序变体都测。
            Assert(TaggedRowBandLayout.TryPlanLayout(4, 4, true, new List<PackableItem>
            {
                DevV502Item("r4n-a", 1, 2, PlayerUseLabel.Medical, 951, 1, 0, 0, 0, 0),
                DevV502Item("r4n-b", 2, 3, PlayerUseLabel.Medical, 952, 1, 0, 0, 0, 0),
                DevV502Item("r4n-c", 2, 3, PlayerUseLabel.Medical, 953, 1, 0, 0, 0, 0),
            }, out var r4nSame, out _) && DevV502AllPlaced(r4nSame) && DevV502ValidatePlan(r4nSame, 4, 4, 3),
                "layout: the greedy-stall counterexample (1x2 + 2x3 + 2x3 on 4x4) resolves fully in one label (能放下必须放下·反例回归)");
            Assert(TaggedRowBandLayout.TryPlanLayout(4, 4, true, new List<PackableItem>
            {
                DevV502Item("r4m-a", 1, 2, PlayerUseLabel.Medical, 951, 1, 0, 0, 0, 0),
                DevV502Item("r4m-b", 2, 3, PlayerUseLabel.BackpackContainer, 952, 1, 0, 0, 0, 0),
                DevV502Item("r4m-c", 2, 3, PlayerUseLabel.BackpackContainer, 953, 1, 0, 0, 0, 0),
            }, out var r4nCross, out _) && DevV502AllPlaced(r4nCross) && DevV502ValidatePlan(r4nCross, 4, 4, 3),
                "layout: the same counterexample resolves across labels too (medical-first stream must not refuse the page)");

            // 4k 升降序只影响完全相同条件下的稳定收尾：几何各异 → 两向同版。
            bool descOk = TaggedRowBandLayout.TryPlanLayout(6, 4, true, DevV502FixtureMixedLabels(), out var finDesc, out _);
            bool ascOk = TaggedRowBandLayout.TryPlanLayout(6, 4, false, DevV502FixtureMixedLabels(), out var finAsc, out _);
            Assert(descOk && ascOk && finDesc.Count == finAsc.Count, "layout: both stable-finish directions plan the mixed fixture");
            for (var i = 0; i < finDesc.Count; i++)
                Assert(finDesc[i].ResultX == finAsc[i].ResultX && finDesc[i].ResultY == finAsc[i].ResultY
                    && finDesc[i].ResultRot == finAsc[i].ResultRot,
                    "layout: 升/降序 never changes the label order or the main layout (只动稳定收尾)");

            // ── 5. 官方先行消费（V5-T3 Q5：凡背包整理都走同一模块）──
            // 5a 模块默认策略 = 统一排版（真实整理按钮的计划来源）。
            var v5Feature = new FeatureId("io.github.yu80rice.bue.inventory-tidy");
            var v5Module = new InventoryTidyModule(v5Feature);
            Assert(v5Module.Strategy.StrategyId == "tagged-row-band-v1",
                "official-first: the module's ONE built-in strategy is the tagged row-band plan (真实整理按钮走新计划)");
            // 5b 旧三档不再决定算法：同一输入任何 mode 值得到完全相同的计划。
            var modeProbe = DevV502FixtureMixedLabels();
            var planSame = new TaggedRowBandV1Strategy().BuildPlan(new TidyInput(6, 4, true, TidyMode.SameType, modeProbe));
            var planRects = new TaggedRowBandV1Strategy().BuildPlan(new TidyInput(6, 4, true, TidyMode.MaxRects, modeProbe));
            var planFfd = new TaggedRowBandV1Strategy().BuildPlan(new TidyInput(6, 4, true, TidyMode.FFD, modeProbe));
            Assert(planSame.AllPlaced && planRects.AllPlaced && planFfd.AllPlaced,
                "official-first: the fixture plans fully whatever the legacy mode says");
            for (var i = 0; i < planSame.Placements.Count; i++)
                Assert(planRects.Placements[i].ResultX == planSame.Placements[i].ResultX
                    && planRects.Placements[i].ResultY == planSame.Placements[i].ResultY
                    && planRects.Placements[i].ResultRot == planSame.Placements[i].ResultRot
                    && planFfd.Placements[i].ResultX == planSame.Placements[i].ResultX
                    && planFfd.Placements[i].ResultY == planSame.Placements[i].ResultY
                    && planFfd.Placements[i].ResultRot == planSame.Placements[i].ResultRot,
                    "official-first: 同类/空间/大件 no longer decide anything — the plan is one unified method (入口不得复制算法)");
            // 5c PreparePage 标签缝：真实消费点在 Prepare 层填标签并进新计划。
            var prepPage = new Items(3);
            typeof(Items).GetField("_width", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(prepPage, (byte)4);
            typeof(Items).GetField("_height", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(prepPage, (byte)4);
            var medJar = CreateTestItemJar(3, 0, 0, 1, 1);
            SetJarItem(medJar, new Item(901, 1, 100, new byte[0]));
            var boxJar = CreateTestItemJar(3, 1, 0, 1, 1);
            SetJarItem(boxJar, new Item(902, 1, 100, new byte[0]));
            prepPage.items.Add(medJar);
            prepPage.items.Add(boxJar);
            ItemUseSignalsProvider.ResolveForTests = item => item.id == 901 ? PlayerUseLabel.Medical
                : item.id == 902 ? PlayerUseLabel.AmmoBox : (PlayerUseLabel?)null;
            try
            {
                var prep = ManualTidyService.PreparePage(prepPage, 3, true, TidyMode.SameType, v5Module.Strategy);
                Assert(prep.Valid, "official-first: the tagged Prepare page passes static validation (计划合法可提交)");
                var prepMed = DevV502FindByJar(prep.Result, medJar);
                var prepBox = DevV502FindByJar(prep.Result, boxJar);
                Assert(prepMed.Label == PlayerUseLabel.Medical && prepBox.Label == PlayerUseLabel.AmmoBox,
                    "official-first: PreparePage fills the frozen labels from the classifier seam");
                Assert(prepMed.ResultY < prepBox.ResultY
                    || (prepMed.ResultY == prepBox.ResultY && prepMed.ResultX < prepBox.ResultX),
                    "official-first: 医疗用品 segments ahead of 弹药箱 in the real consumer path");
            }
            finally
            {
                ItemUseSignalsProvider.ResolveForTests = null;
            }

            // ── 6. 设置迁移：旧 mode/direction 首次读取归一，不再决定算法 ──
            // 6a schema：只剩「整理方向」一条 Choice；三档字样绝不再出现在任何
            // 玩家可见 descriptor 字段（档位从玩家界面退役）。
            var v5Descriptors = InventoryTidyModule.CreateSettingsDescriptors(v5Feature);
            Assert(v5Descriptors.Count == 1 && v5Descriptors[0].SettingId == "inventorytidy.direction",
                "migration: the schema declares exactly ONE choice (整理方向) — the mode row retired with the three档 (V5-T3 Q3)");
            Assert(v5Descriptors[0].DisplayNameKey == "整理方向"
                && v5Descriptors[0].DescriptionKey == "降序/升序只影响完全相同条件物品的收尾摆放顺序，不改变统一分段排版的结果。",
                "migration: 整理方向 carries the DEV-V5-02 frozen copy (Q61 pair retired with the档)");
            Assert(v5Descriptors[0].AllowedValues.Count == 2
                && v5Descriptors[0].AllowedValues[0].Text == "降序"
                && v5Descriptors[0].AllowedValues[1].Text == "升序",
                "migration: the surviving choice keeps the Q56 frozen literals (降序/升序)");
            Assert(v5Descriptors[0].Authority == SettingAuthority.ClientLocal,
                "migration: direction stays a ClientPreference (never ServerAuthority)");
            Assert(v5Descriptors[0].DefaultValue.Text == "降序",
                "migration: the default stays 降序 (same stable-finish default as before)");
            var legacy档Words = new[] { "同类", "空间", "大件" };
            foreach (var descriptor in v5Descriptors)
            {
                foreach (var word in legacy档Words)
                {
                    Assert(descriptor.DisplayNameKey.IndexOf(word, StringComparison.Ordinal) < 0
                        && descriptor.DescriptionKey.IndexOf(word, StringComparison.Ordinal) < 0,
                        "migration: legacy档 word '" + word + "' never re-enters a player-facing descriptor (设置里不再用三档决定算法)");
                    if (descriptor.AllowedValues != null)
                        foreach (var allowed in descriptor.AllowedValues)
                            Assert(allowed.Text == null || allowed.Text.IndexOf(word, StringComparison.Ordinal) < 0,
                                "migration: legacy档 value '" + word + "' is not an allowed value of the surviving choice");
                }
                Assert(descriptor.SettingId != "inventorytidy.mode",
                    "migration: inventorytidy.mode has no descriptor (旧键只作存盘兼容)");
            }
            // 6b 点击缝只读方向：旧存档残留的 mode 值（含未知字面量）一律忽略，
            // 绝不拦点击；方向未知仍显式拒绝（Q59 纪律不松）。
            var legacyStoreView = new FakeTidySettingsView();
            legacyStoreView.SetEntry("inventorytidy.mode", SettingValue.Choice("空间"));
            legacyStoreView.SetEntry("inventorytidy.direction", SettingValue.Choice("升序"));
            var v5Lifetime = new FakeTidyLifetime { State = FeatureState.Running };
            var v5ServingModule = V5StartLitModuleWith(v5Lifetime, legacyStoreView);
            LitTidyProductionAuthority.ServerRoleProbeForTests = () => true;
            try
            {
                Assert(v5ServingModule.TryReadSavedTidyPreference(out var legacyMode, out var legacyDescending, out _),
                    "migration: an old store with the retired mode entry reads fine (旧存档仍能读，只做迁移)");
                Assert(!legacyDescending && legacyMode == TidyMode.SameType,
                    "migration: 升序 lands as the stable-finish preference and the legacy mode answers the frozen wire placeholder");
                MainThreadDispatcher.ResetForTests();
                Assert(v5ServingModule.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.Dispatched,
                    "migration: the title-bar click dispatches on an old档 store (旧 mode 不再决定算法、也不拦门)");
                MainThreadDispatcher.ResetForTests();
                legacyStoreView.SetEntry("inventorytidy.mode", SettingValue.Choice("任何未知字面量"));
                Assert(v5ServingModule.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.Dispatched,
                    "migration: even a junk legacy mode value cannot refuse the unified click (首次读取归一)");
                MainThreadDispatcher.ResetForTests();
                legacyStoreView.SetEntry("inventorytidy.direction", SettingValue.Choice("从小到大"));
                Assert(!v5ServingModule.TryReadSavedTidyPreference(out _, out _, out _)
                    && v5ServingModule.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.RejectedPreferenceUnavailable,
                    "migration: the surviving direction keeps the honest refusal for unknown literals (Q59 不发明保存过的组合)");
                MainThreadDispatcher.ResetForTests();
            }
            finally
            {
                LitTidyProductionAuthority.ServerRoleProbeForTests = null;
                v5ServingModule.Stop(FeatureStopReason.PluginStopping);
            }
            // 6c 真实 store：旧文档（mode+direction 两条）被新 schema 完整装载，
            // 下次保存后 mode 键自然消失（归一后保存为新规范值）。
            var v5Persistence = new InMemorySettingsPersistence();
            Assert(v5Persistence.TryCommit(v5Feature, SettingRevisionScope.ClientPreference, 1u, 9u,
                new Dictionary<string, SettingValue>(StringComparer.Ordinal)
                {
                    ["inventorytidy.mode"] = SettingValue.Choice("空间"),
                    ["inventorytidy.direction"] = SettingValue.Choice("升序"),
                }, out _),
                "migration seed: an old-shape persisted document is planted (同类/空间/大件 era)");
            var v5Registry = new BetterUnturnedExperience.Core.Settings.FeatureSettingsRegistry(v5Persistence, () => true, null);
            v5Registry.GetOrCreateRuntime(v5Feature, InventoryTidyModule.CreateSettingsDescriptors(v5Feature));
            v5Registry.OpenGeneration(v5Feature, 1UL);
            var v5StoreView = v5Registry.CreateView(v5Feature, 1UL);
            var v5Snapshot = v5StoreView.GetSnapshot(SettingRevisionScope.ClientPreference);
            Assert(v5Snapshot.Entries.Count == 1
                && v5Snapshot.Entries[0].SettingId == "inventorytidy.direction"
                && v5Snapshot.Entries[0].EffectiveValue.Text == "升序",
                "migration: the old document loads — the legacy mode entry is ignored, the direction survives unchanged");
            var v5StoreModule = new InventoryTidyModule(v5Feature);
            v5StoreModule.AttachSettingsView(v5StoreView);
            Assert(v5StoreModule.TryReadSavedTidyPreference(out _, out var storeDescending, out _) && !storeDescending,
                "migration: the click seam reads the migrated store honestly");
            Assert(v5StoreView.Submit(new ScopedSettingChangeRequest(1UL, SettingRevisionScope.ClientPreference,
                v5Snapshot.Revision, new[] { new SettingMutation("inventorytidy.direction", SettingValue.Choice("降序")) })).Accepted,
                "migration setup: the first saved write under the new schema is accepted");
            SettingsPersistenceLoadResult afterSave;
            afterSave = v5Persistence.Load(v5Feature, SettingRevisionScope.ClientPreference, 1u, v5Descriptors);
            Assert(afterSave.Values.ContainsKey("inventorytidy.direction")
                && !afterSave.Values.ContainsKey("inventorytidy.mode"),
                "migration: the next commit persists ONLY the new canonical entries (旧 mode 键随保存归一消失)");
            // 6d 私有线协议不变：mode 字节仍能传输与读取（帧形零改动）。
            var legacyFrame = LitTidyWireCodec.BuildTidyRequest(5UL, 7u, 3, TidyMode.FFD, true, null);
            Assert(LitTidyWireCodec.TryReadEnvelope(legacyFrame, out var frameType, out var frameBody)
                && frameType == LitTidyWireCodec.MsgRequestTidyV2,
                "migration: the built frame keeps the [version][msgType] envelope");
            Assert(LitTidyWireCodec.TryReadTidyRequest(frameBody, out var frameToken, out var frameReq, out var framePage,
                    out var frameMode, out var frameDesc, out _),
                "migration: the private tidy-request frame still round-trips the legacy mode byte (协议不扩面)");
            Assert(frameMode == TidyMode.FFD && frameDesc && frameToken == 5UL && framePage == 3,
                "migration: the frame keeps every legacy field byte-for-byte (契约 2.1 零扩面)");

            // ── 7. 生产退役：旧三套实现不得留在编译列表或程序集类型里 ──
            var v5SolutionRoot = new DirectoryInfo(AppContext.BaseDirectory);
            while (v5SolutionRoot != null && !File.Exists(Path.Combine(v5SolutionRoot.FullName, "BetterUnturnedExperience.sln")))
                v5SolutionRoot = v5SolutionRoot.Parent;
            Assert(v5SolutionRoot != null, "retirement: solution root located");
            var v5Csproj = File.ReadAllText(Path.Combine(v5SolutionRoot.FullName, "src", "BetterUnturnedExperience.Plugin", "BetterUnturnedExperience.Plugin.csproj"));
            Assert(v5Csproj.Contains("Layout\\TaggedRowBandLayout.cs") && v5Csproj.Contains("TaggedRowBandV1Strategy.cs"),
                "retirement: the unified layout module is in the production compile list (positive control)");
            Assert(!v5Csproj.Contains("DefaultGridV1Strategy.cs") && !v5Csproj.Contains("InventorySolver.cs")
                && !v5Csproj.Contains("LayoutCandidate.cs"),
                "retirement: the old solver trio is gone from the compile list (不保留旧实现并行)");
            var v5AsmTypes = new HashSet<string>();
            foreach (var type in typeof(BetterUnturnedExperiencePlugin).Assembly.GetTypes()) v5AsmTypes.Add(type.Name);
            Assert(!v5AsmTypes.Contains("InventorySolver") && !v5AsmTypes.Contains("LayoutCandidate")
                && !v5AsmTypes.Contains("DefaultGridV1Strategy"),
                "retirement: the old solver trio carries no type in the shipped assembly either");
            Assert(v5AsmTypes.Contains("TaggedRowBandLayout") && v5AsmTypes.Contains("PlayerUseClassifier"),
                "retirement: the new deep module and classifier ship in the production assembly");
        }

        private static PackableItem DevV502FindByJar(IReadOnlyList<PackableItem> plan, ItemJar jar)
        {
            foreach (var item in plan)
                if (item != null && ReferenceEquals(item.Tag, jar)) return item;
            throw new InvalidOperationException("DEV-V5-02 fixture jar missing from the plan");
        }

        // Test-only helpers for the DEV-V5-02 layout group (DEV-V5-02).
        private static PackableItem DevV502Item(string tag, byte sx, byte sy, PlayerUseLabel label,
            ushort groupKey, int stableOrder, byte originalX, byte originalY, byte originalRot, byte preferredRot)
        {
            return new PackableItem
            {
                Tag = tag,
                size_x = sx,
                size_y = sy,
                Label = label,
                GroupKey = groupKey,
                StableOrder = stableOrder,
                OriginalX = originalX,
                OriginalY = originalY,
                OriginalRot = originalRot,
                PreferredRotation = preferredRot,
            };
        }

        private static PackableItem DevV502FindByTag(List<PackableItem> plan, string tag)
        {
            foreach (var item in plan)
                if (item != null && (string)item.Tag == tag) return item;
            throw new InvalidOperationException("DEV-V5-02 fixture tag missing: " + tag);
        }

        private static bool DevV502AnyPlaced(List<PackableItem> plan)
        {
            foreach (var item in plan)
                if (item != null && item.Placed) return true;
            return false;
        }

        private static bool DevV502AllPlaced(List<PackableItem> plan)
        {
            foreach (var item in plan)
                if (item == null || !item.Placed) return false;
            return true;
        }

        private static bool DevV502AllUnrotated(List<PackableItem> plan)
        {
            foreach (var item in plan)
                if (item == null || item.ResultRot != 0) return false;
            return true;
        }

        private static bool DevV502AllOnRow(List<PackableItem> plan, byte row)
        {
            foreach (var item in plan)
                if (item == null || !item.Placed || item.ResultY != row) return false;
            return true;
        }

        private static List<PackableItem> DevV502FixtureMixedLabels()
        {
            // 乱序输入（倒序+混组），覆盖 弹匣/医疗/弹药箱/背包/其他 五段 + 多尺寸。
            return new List<PackableItem>
            {
                DevV502Item("x-other", 1, 1, PlayerUseLabel.Other, 901, 1, 4, 3, 0, 0),
                DevV502Item("x-gun", 2, 1, PlayerUseLabel.RangedWeapon, 902, 2, 3, 2, 0, 0),
                DevV502Item("x-mag", 1, 2, PlayerUseLabel.Magazine, 903, 3, 2, 1, 0, 0),
                DevV502Item("x-med", 1, 1, PlayerUseLabel.Medical, 904, 4, 1, 0, 0, 0),
                DevV502Item("x-box", 2, 2, PlayerUseLabel.AmmoBox, 905, 5, 0, 0, 0, 0),
                DevV502Item("x-bag", 3, 1, PlayerUseLabel.BackpackContainer, 906, 6, 0, 0, 0, 0),
                DevV502Item("x-ammo", 1, 1, PlayerUseLabel.Ammo, 907, 7, 5, 3, 0, 0),
            };
        }

        /// <summary>Hard-invariant validator: every Placed entry sits in-bounds with its
        /// rotated footprint, no two overlap, and the plan accounts for the input once.</summary>
        private static bool DevV502ValidatePlan(List<PackableItem> plan, byte width, byte height, int inputCount)
        {
            if (plan == null || plan.Count != inputCount) return false;
            var occupied = new bool[width, height];
            foreach (var placement in plan)
            {
                if (placement == null || !placement.Placed) continue;
                var w = (placement.ResultRot & 1) == 1 ? placement.size_y : placement.size_x;
                var h = (placement.ResultRot & 1) == 1 ? placement.size_x : placement.size_y;
                if (w == 0 || h == 0) return false;
                if (placement.ResultX + w > width || placement.ResultY + h > height) return false;
                for (var cx = placement.ResultX; cx < placement.ResultX + w; cx++)
                    for (var cy = placement.ResultY; cy < placement.ResultY + h; cy++)
                    {
                        if (occupied[cx, cy]) return false;
                        occupied[cx, cy] = true;
                    }
            }
            return true;
        }

        /// <summary>Structural invariant: scanning placed items row-major (top-left then
        /// left-to-right) the label ordinals never decrease — segments do not interleave.</summary>
        private static bool DevV502RowMajorLabelsMonotone(List<PackableItem> plan)
        {
            var placed = new List<PackableItem>();
            foreach (var item in plan)
                if (item != null && item.Placed) placed.Add(item);
            placed.Sort((a, b) =>
            {
                int c = a.ResultY.CompareTo(b.ResultY);
                if (c != 0) return c;
                return a.ResultX.CompareTo(b.ResultX);
            });
            int previous = -1;
            foreach (var item in placed)
            {
                var current = (int)item.Label;
                if (current < previous) return false;
                previous = current;
            }
            return true;
        }

        // Test-only signal builders for the pure classifier table (DEV-V5-02).
        private static PlayerUseLabel DevV502Classify(EItemType type, bool isMagazineAsset = false, bool isFillSupply = false)
        {
            return PlayerUseClassifier.Classify(new PlayerUseSignals
            {
                Id = 7,
                TypeKnown = true,
                Type = type,
                IsMagazineAsset = isMagazineAsset,
                IsCaliberAsset = isMagazineAsset,
                IsFillSupply = isFillSupply,
            });
        }

        // 弹药 in this game build: a caliber asset that is not a magazine (no
        // EItemType.AMMO exists) — e.g. a loose-round ItemCaliberAsset.
        private static PlayerUseLabel DevV502ClassifyCaliber(bool isFillSupply = false)
        {
            return PlayerUseClassifier.Classify(new PlayerUseSignals
            {
                Id = 7,
                TypeKnown = true,
                Type = EItemType.MAGAZINE,
                IsMagazineAsset = false,
                IsCaliberAsset = true,
                IsFillSupply = isFillSupply,
            });
        }

        // DEV-V5-02: hand-composed LIT module through the host bootstrap (the
        // V4-06 group owns the equivalent local helper; a second copy lives
        // here because that helper is a local function of another test).
        private static InventoryTidyModule V5StartLitModuleWith(FakeTidyLifetime lifetime, IScopedFeatureSettings settings)
        {
            var lit = new FeatureId(LitRuntime.FeatureIdValue);
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 2102UL);
            var litModule = new InventoryTidyModule(lit);
            litModule.ScopeDirectoryForTests = NewLitFaultDirectory();
            litModule.FaultContextForTests = () => new LitFaultScopeContext("TestMap", 1);
            var started = litModule.Start(new FeatureBootstrap(default(FeatureScopeIdentity), 9UL, settings,
                bus.Subscriber(lit), bus.Publisher(lit), bus.EventRegistry(lit), null, null, lifetime, network));
            Assert(started.Started, "DEV-V5-02 setup：模块经宿主 bootstrap 启动");
            return litModule;
        }

        private static PlayerUseLabel DevV502ClassifyUnknownType()
        {
            return PlayerUseClassifier.Classify(new PlayerUseSignals
            {
                Id = 7,
                TypeKnown = false,
            });
        }

        // DEV-V2-16 red regression (GPT watermark): session-driven multicast
        // and frozen send-result semantics (T3 decision 4). SendToClients
        // targets each ESTABLISHED session individually — never one
        // untargeted fire-and-forget frame handed to the transport — and the
        // result aggregates per-target outcomes into the frozen values
        // (snapshot empty -> NoSession, >=1 success -> Sent, every target
        // failed -> LocalTransportUnavailable, mixed -> PartialFailure).
        // Sessions narrows to the established snapshot (pending sessions are
        // internal-only), SendToClient validates ownership by identity,
        // establishment, and the live connection generation, and no send
        // holds the state lock across the transport call. RED until the
        // runtime rewrite lands (compile CS0117 on
        // NetworkSendResult.PartialFailure, then runtime assertions).
        private static void AssertBueV2SessionDrivenSendSemantics(bool collectAllFailures = false)
        {
            // The --bue-v2-send-semantics-red flag collects every failed
            // assertion across all four frozen groups into one transcript
            // (the red evidence names each group); the suite path stays
            // fail-fast.
            var reds = new System.Collections.Generic.List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }
                var localContract = new ContractVersion(2, 0);
                var channel = new FeatureId("io.example.v2send");

                // 1. Established-only snapshot: a pending session (StartSession
                //    before the Ack) is invisible to Sessions and forms no target
                //    set — the frozen table maps it to NoSession, not a send.
                var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                var a = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1002UL);
                var b = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL);
                Check(a.RegisterChannel(channel, localContract, 1).Accepted && b.RegisterChannel(channel, localContract, 1).Accepted,
                    "setup: both runtimes register the send channel");
                var pending = a.StartSession(2002UL);
                Check(pending != null, "setup: the initiator holds a pending session before the Ack");
                Check(a.Sessions.Count == 0,
                    "snapshot: a pending session is invisible to Sessions (established-only)");
                Check(a.SendToClients(channel, new byte[] { 0x01 }, true) == NetworkSendResult.NoSession,
                    "no-session: multicast with an empty established snapshot returns NoSession (pending-only is not a target set)");
                Check(a.SendToServer(channel, new byte[] { 0x01 }, true) == NetworkSendResult.NoSession,
                    "no-session: SendToServer with an empty established snapshot returns NoSession");
                Check(a.SendToClient(channel, pending, new byte[] { 0x01 }, true) == NetworkSendResult.NoSession,
                    "established: a pending session is not a valid SendToClient target");
                pair.First.Pump(); pair.Second.Pump(); pair.First.Pump(); // Hello -> Ack -> established
                Check(a.Sessions.Count == 1 && a.Sessions[0].SessionId == pending.SessionId,
                    "snapshot: the established session appears in Sessions after the Ack (same connection generation)");

                // 2. Per-target multicast over a controlled hub topology: the
                //    transport seam records every target; a recorded zero target
                //    is the old untargeted fire-and-forget defect.
                System.Action<byte[]> hubReceiver = null;
                System.Action<byte[]> peerBReceiver = null;
                System.Action<byte[]> peerCReceiver = null;
                var hubTargets = new System.Collections.Generic.List<ulong>();
                var hubReliableBits = new System.Collections.Generic.List<bool>();
                var peerBDelivers = true;
                var peerCDelivers = true;
                var transportHub = new BetterUnturnedExperience.Core.Network.HostNetworkTransportAdapter((frame, reliable, target) =>
                {
                    hubTargets.Add(target);
                    hubReliableBits.Add(reliable);
                    if (target == 0UL)
                    {
                        if (peerBReceiver != null) peerBReceiver(frame);
                        if (peerCReceiver != null) peerCReceiver(frame);
                    }
                    else if (target == 200UL) { if (peerBDelivers && peerBReceiver != null) peerBReceiver(frame); return peerBDelivers; }
                    else if (target == 300UL) { if (peerCDelivers && peerCReceiver != null) peerCReceiver(frame); return peerCDelivers; }
                    return true;
                }, callback => hubReceiver = callback);
                var transportPeerB = new BetterUnturnedExperience.Core.Network.HostNetworkTransportAdapter(
                    (frame, reliable, target) => { if (hubReceiver != null) hubReceiver(frame); return true; },
                    callback => peerBReceiver = callback);
                var transportPeerC = new BetterUnturnedExperience.Core.Network.HostNetworkTransportAdapter(
                    (frame, reliable, target) => { if (hubReceiver != null) hubReceiver(frame); return true; },
                    callback => peerCReceiver = callback);
                var hub = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(transportHub, localContract, 100UL);
                var peerB = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(transportPeerB, localContract, 200UL);
                var peerC = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(transportPeerC, localContract, 300UL);
                Check(hub.RegisterChannel(channel, localContract, 1).Accepted
                    && peerB.RegisterChannel(channel, localContract, 1).Accepted
                    && peerC.RegisterChannel(channel, localContract, 1).Accepted,
                    "setup: the trio registers the send channel");
                peerB.StartSession(100UL);
                peerC.StartSession(100UL);
                transportPeerB.Pump(); transportPeerC.Pump();
                transportHub.Pump(); transportHub.Pump();
                transportPeerB.Pump(); transportPeerC.Pump();
                Check(hub.Sessions.Count == 2, "setup: the hub holds two established sessions");
                var hubSessionB = hub.Sessions[0].PeerSteamId == 200UL ? hub.Sessions[0] : hub.Sessions[1];
                hubTargets.Clear();
                Check(hub.SendToClients(channel, new byte[] { 0x2A }, true) == NetworkSendResult.Sent,
                    "aggregate: every targeted send succeeded -> Sent");
                Check(hubTargets.Count == 2 && hubTargets.Contains(200UL) && hubTargets.Contains(300UL),
                    "multicast: SendToClients targets each established session's peer individually (never one untargeted frame)");
                Check(hubReliableBits.TrueForAll(reliableBit => reliableBit),
                    "multicast: every targeted frame carries the reliability bit");
                Check(hub.SendToClient(channel, hubSessionB, new byte[16 * 1024 + 1], true) == NetworkSendResult.PayloadTooLarge,
                    "payload: SendToClient keeps the dedicated oversized-payload result (validated once, not a transport failure)");

                // 3. Frozen result aggregation: PartialFailure (>=1 success and
                //    >=1 failure) and LocalTransportUnavailable (every target
                //    failed). An oversized multicast payload keeps its dedicated
                //    result instead of being aggregated into transport failures.
                peerBDelivers = false;
                hubTargets.Clear();
                Check(hub.SendToClients(channel, new byte[] { 0x2B }, true) == NetworkSendResult.PartialFailure,
                    "aggregate: some targets delivered and some failed -> PartialFailure");
                Check(hubTargets.Count == 2,
                    "aggregate: PartialFailure still attempted every established target");
                peerCDelivers = false;
                Check(hub.SendToClients(channel, new byte[] { 0x2C }, true) == NetworkSendResult.LocalTransportUnavailable,
                    "aggregate: every target failed -> LocalTransportUnavailable");
                peerBDelivers = true;
                peerCDelivers = true;
                Check(hub.SendToClients(channel, new byte[16 * 1024 + 1], true) == NetworkSendResult.PayloadTooLarge,
                    "payload: an oversized multicast payload keeps its dedicated result (never swallowed by aggregation)");

                // 4. SendToClient validates ownership by identity: a foreign
                //    session object claiming a live generation id is rejected (no
                //    id-equality shortcut), and a session dropped from this
                //    runtime (stale generation) is rejected too.
                var stubSession = new ForeignSessionStub(hub.Sessions[0].SessionId, 999UL);
                Check(hub.SendToClient(channel, stubSession, new byte[] { 0x2D }, true) == NetworkSendResult.NoSession,
                    "ownership: a foreign session object claiming a live generation id is rejected");
                Check(hub.SendToClient(channel, null, new byte[] { 0x2D }, true) == NetworkSendResult.NoSession,
                    "ownership: a null session context is NoSession");
                hub.SetModuleActive(false);
                Check(hub.Sessions.Count == 0, "setup: disabling the module drops the snapshot");
                Check(hub.SendToClient(channel, hubSessionB, new byte[] { 0x2E }, true) == NetworkSendResult.NoSession,
                    "generation: a session object from a dropped generation is rejected");
                // DEV-V2-14 linkage regression: sends while the module is down
                // stay on the existing enum value — no new error code.
                Check(hub.SendToClients(channel, new byte[] { 0x2E }, true) == NetworkSendResult.NoSession,
                    "disabled: SendToClients stays on the existing NoSession code");
                Check(hub.SendToServer(channel, new byte[] { 0x2E }, true) == NetworkSendResult.NoSession,
                    "disabled: SendToServer stays on the existing NoSession code");
                hub.SetModuleActive(true);

                // 5. ChannelNotRegistered precedence and the re-armed empty
                //    snapshot survive the rewrite unchanged. The channel gate
                //    precedes the target-context checks on ALL send paths —
                //    including SendToClient's null session (R1 fix round: the
                //    null check used to run before the channel gate).
                var unregistered = new FeatureId("io.example.v2send-late");
                Check(hub.SendToClients(unregistered, new byte[] { 0x2F }, true) == NetworkSendResult.ChannelNotRegistered,
                    "channel: an unregistered channel is still reported before any target work");
                Check(hub.SendToClient(unregistered, null, new byte[] { 0x2F }, true) == NetworkSendResult.ChannelNotRegistered,
                    "channel: SendToClient reports an unregistered channel before the null-context check");
                Check(hub.SendToClient(unregistered, ForeignSessionStub.Anonymous(), new byte[] { 0x2F }, true) == NetworkSendResult.ChannelNotRegistered,
                    "channel: SendToClient reports an unregistered channel before the ownership check");
                Check(hub.SendToClients(channel, new byte[] { 0x30 }, true) == NetworkSendResult.NoSession,
                    "re-arm: a re-enabled module starts from an empty established snapshot");

                // 6. Lock-freedom proven from a foreign thread: a targeted send
                //    blocked inside the transport must not hold the state lock.
                var probeEntered = new System.Threading.ManualResetEventSlim(false);
                var releaseProbe = new System.Threading.ManualResetEventSlim(false);
                System.Action<byte[]> probePeerReceiver = null;
                System.Action<byte[]> probeHubReceiver = null;
                var transportProbeHub = new BetterUnturnedExperience.Core.Network.HostNetworkTransportAdapter((frame, reliable, target) =>
                {
                    // Block the first DATA frame inside the transport — in the
                    // red round that is the untargeted fire-and-forget frame,
                    // in the green round the per-target frame; either way the
                    // lock probe runs while a send is in flight.
                    var isData = frame != null && frame.Length > 4 && frame[4] == 0;
                    if (isData)
                    {
                        probeEntered.Set();
                        releaseProbe.Wait(System.TimeSpan.FromSeconds(5)); // block the send inside the transport
                        if (probePeerReceiver != null) probePeerReceiver(frame);
                        return true;
                    }
                    if (target == 0UL && probePeerReceiver != null) probePeerReceiver(frame); // handshake frames flow
                    return true;
                }, callback => probeHubReceiver = callback);
                var transportProbePeer = new BetterUnturnedExperience.Core.Network.HostNetworkTransportAdapter(
                    (frame, reliable, target) => { if (probeHubReceiver != null) probeHubReceiver(frame); return true; },
                    callback => probePeerReceiver = callback);
                var probeHub = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(transportProbeHub, localContract, 100UL);
                var probePeer = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(transportProbePeer, localContract, 400UL);
                Check(probeHub.RegisterChannel(channel, localContract, 1).Accepted && probePeer.RegisterChannel(channel, localContract, 1).Accepted,
                    "setup: the lock-probe pair registers the channel");
                probePeer.StartSession(100UL);
                transportProbeHub.Pump();
                transportProbePeer.Pump();
                Check(probeHub.Sessions.Count == 1, "setup: the lock-probe hub holds one established session");
                var sendTask = System.Threading.Tasks.Task.Run(() => probeHub.SendToClients(channel, new byte[] { 0x31 }, true));
                Check(probeEntered.Wait(System.TimeSpan.FromSeconds(5)),
                    "lock-freedom: the targeted send reached the transport");
                var foreign = System.Threading.Tasks.Task.Run(() => probeHub.Sessions.Count);
                Check(foreign.Wait(System.TimeSpan.FromSeconds(2)),
                    "lock-freedom: a foreign thread acquired the state lock while the targeted send blocked in the transport");
                releaseProbe.Set();
                Check(sendTask.Wait(System.TimeSpan.FromSeconds(5)) && sendTask.Result == NetworkSendResult.Sent,
                    "lock-freedom: the blocked send completes with Sent once released");
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-16 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V2-16 red-regression helper: a session object that does not
        // belong to the runtime under test but claims a (live) generation id
        // — the forged-ownership probe for SendToClient.
        private sealed class ForeignSessionStub : IConnectionSession
        {
            public ForeignSessionStub(ulong sessionId, ulong peerSteamId) { SessionId = sessionId; PeerSteamId = peerSteamId; }
            public static ForeignSessionStub Anonymous() { return new ForeignSessionStub(0UL, 0UL); }
            public ulong SessionId { get; }
            public ulong PeerSteamId { get; }
            public ContractVersion PeerContract { get { return default(ContractVersion); } }
            public ushort PeerFeatureVersion { get { return 0; } }
            public IReadOnlyList<ChannelVersionEntry> Channels { get { return new ChannelVersionEntry[0]; } }
#pragma warning disable 0067 // Stub never raises lifecycle events.
            public event System.Action Connected;
            public event System.Action Disconnected;
            public event System.Action<ulong> GenerationChanged;
#pragma warning restore 0067
            public NetworkSendResult Send(byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
        }

        // DEV-V2-17 red regression (GPT watermark): automatic handshake and
        // session lifecycle. The runtime owns the whole handshake — transport
        // connected -> auto Hello -> Ack/Reject -> Connected — plus duplicate
        // Hello dedup, Ack matching by peer + handshake nonce (the initiator's
        // session id IS the connection generation), disconnect cleanup,
        // reconnect generation replacement, timeout re-probe backoff, and
        // version fail-closed. Frozen groups: 流程/时序, 去重, 匹配, 断线与
        // 重连换代际, fail-closed, 退避, 锁外回调, 重启用重建. RED until the
        // lifecycle seam (PeerConnected/PeerDisconnected/ConnectedPeers),
        // the initiator switch, and TickHandshake exist.
        private static void AssertBueV2AutoHandshakeLifecycle(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                // Each group collects independently so the stub-stage transcript
                // names a failure per frozen group instead of aborting at the first.
                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                var localContract = new ContractVersion(2, 0);

                Group("流程/时序", () =>
                {
                    // ---- 流程/时序：transport connected → 自动 Hello → Ack → Connected；
                    //      握手完成前功能模块视角为空快照 + 显式 NoSession（零握手负担）。
                    var clientT = new HandshakeTestTransport();
                    var serverT = new HandshakeTestTransport();
                    var client = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(clientT, localContract, 1001UL);
                    var server = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(serverT, localContract, 2001UL, handshakeInitiator: false);
                    var lifecycleChannel = new FeatureId("io.example.v2hand");
                    Check(client.RegisterChannel(lifecycleChannel, localContract, 1).Accepted && server.RegisterChannel(lifecycleChannel, localContract, 1).Accepted,
                        "setup: the flow pair registers the lifecycle channel");
                    Check(clientT.Sent.Count == 0, "流程：连接建立前运行时不发送任何握手帧");
                    clientT.ConnectPeer(2001UL);
                    Check(clientT.Sent.Count == 1 && clientT.Sent[0].Frame[4] == HandshakeKindHello && clientT.Sent[0].Target == 0UL && clientT.Sent[0].Reliable,
                        "流程：transport connected 后运行时自动发出 Hello（单帧 untargeted、可靠位携带）");
                    Check(client.Sessions.Count == 0, "时序：握手完成前 Sessions 为空快照（pending 不可见，established-only）");
                    Check(client.SendToServer(lifecycleChannel, new byte[] { 0x01 }, true) == NetworkSendResult.NoSession,
                        "时序：握手完成前功能模块发送得到显式 NoSession（零握手负担、无静默）");
                    serverT.ConnectPeer(1001UL);
                    Check(server.Sessions.Count == 0 && serverT.Sent.Count == 0,
                        "流程：响应方在 transport connected 时不主动握手（Hello 仅由发起方发出）");
                    var helloFrame = clientT.Sent[0].Frame;
                    clientT.ForwardPending(serverT, null);
                    serverT.Pump();
                    Check(server.Sessions.Count == 1 && server.Sessions[0].PeerSteamId == 1001UL,
                        "流程：服务器收到 Hello 即建立会话（响应方同步建立）");
                    Check(serverT.Sent.Count == 1 && serverT.Sent[0].Frame[4] == HandshakeKindAck && serverT.Sent[0].Target == 1001UL,
                        "匹配：Ack 定向发回发起方（target=发起方 steam id，不广播）");
                    serverT.ForwardPending(clientT, null);
                    clientT.Pump();
                    Check(client.Sessions.Count == 1, "流程：发起方收到 Ack 后会话进入公开快照");
                    Check(client.Sessions[0].SessionId == HandshakeRead64(helloFrame, 26),
                        "代际：Hello 携带的握手 nonce 即发起方会话的连接代际（SessionId）");
                    var flowSession = client.Sessions[0];
                    Check(flowSession.PeerContract.Major == 2, "契约：Ack 协商的契约发布到会话（PeerContract）");
                    var replayConnected = 0;
                    flowSession.Connected += () => replayConnected++;
                    clientT.InjectFrame((byte[])serverT.Sent[0].Frame.Clone());
                    clientT.Pump();
                    Check(replayConnected == 0, "时序：已建立会话重放 Ack 不重复触发 Connected");
                    var flowReceived = new List<byte[]>();
                    server.Subscribe(lifecycleChannel, ChannelDirection.FromClients, (session, payload) => flowReceived.Add(payload));
                    Check(client.SendToClient(lifecycleChannel, flowSession, new byte[] { 0x17 }, true) == NetworkSendResult.Sent,
                        "流程：握手完成后功能模块直接收发（零握手负担）");
                    clientT.ForwardPending(serverT, null);
                    serverT.Pump();
                    Check(flowReceived.Count == 1 && flowReceived[0].Length == 1 && flowReceived[0][0] == 0x17,
                        "流程：Data 帧经会话上下文送达订阅处理器");
                });

                Group("去重", () =>
                {
                    // ---- 去重：响应方对重放 Hello 幂等重 Ack（不产第二会话）；
                    //      发起方在在途握手期间重复收到连接事件不重建。
                    var dedupClientT = new HandshakeTestTransport();
                    var dedupServerT = new HandshakeTestTransport();
                    var dedupClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(dedupClientT, localContract, 1101UL);
                    var dedupServer = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(dedupServerT, localContract, 2101UL, handshakeInitiator: false);
                    dedupClientT.ConnectPeer(2101UL);
                    dedupClientT.ForwardPending(dedupServerT, null);
                    dedupServerT.Pump();
                    Check(dedupServer.Sessions.Count == 1, "去重：首次 Hello 建立唯一会话");
                    var dedupNonce = HandshakeRead64(dedupServerT.Sent[0].Frame, 26);
                    dedupServerT.InjectFrame((byte[])dedupClientT.Sent[0].Frame.Clone());
                    dedupServerT.Pump();
                    Check(dedupServer.Sessions.Count == 1, "去重：重放 Hello 不产生第二个会话（按发起方 nonce 去重）");
                    Check(dedupServerT.CountKind(HandshakeKindAck) == 2, "去重：重放 Hello 得到幂等重 Ack（客户端可恢复）");
                    Check(HandshakeRead64(dedupServerT.Sent[1].Frame, 26) == dedupNonce, "去重：重 Ack 回带同一握手 nonce");
                    var dedupHellos = dedupClientT.CountKind(HandshakeKindHello);
                    dedupClientT.ConnectPeer(2101UL);
                    Check(dedupClientT.CountKind(HandshakeKindHello) == dedupHellos,
                        "去重：发起方在在途握手期间重复收到连接事件不重发 Hello（重探归退避管）");
                    dedupClientT.ForwardPending(dedupServerT, null);
                    dedupServerT.Pump();
                    Check(dedupServer.Sessions.Count == 1, "去重：重复连接事件后服务器侧仍只有一条会话");
                });

                Group("匹配", () =>
                {
                    // ---- 匹配：他人 Ack（nonce 不符）不建立会话——废弃「第一个未建立会话」匹配法；
                    //      服务器 Ack 逐发起方定向。
                    var hubT = new HandshakeTestTransport();
                    var peerBT = new HandshakeTestTransport();
                    var peerCT = new HandshakeTestTransport();
                    var hub = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(hubT, localContract, 100UL, handshakeInitiator: false);
                    var peerB = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(peerBT, localContract, 200UL);
                    var peerC = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(peerCT, localContract, 300UL);
                    peerB.StartSession(100UL);
                    peerC.StartSession(100UL);
                    peerBT.ForwardPending(hubT, null);
                    peerCT.ForwardPending(hubT, null);
                    hubT.Pump();
                    Check(hub.Sessions.Count == 2, "匹配：hub 为两个发起方各建立一条会话");
                    Check(hubT.CountKind(HandshakeKindAck) == 2
                        && hubT.Sent.FindAll(record => record.Frame[4] == HandshakeKindAck && record.Target == 200UL).Count == 1
                        && hubT.Sent.FindAll(record => record.Frame[4] == HandshakeKindAck && record.Target == 300UL).Count == 1,
                        "匹配：服务器为每个发起方定向回 Ack（target=该发起方），不交底层广播");
                    // TakeCursor resets make each frame routable to SEVERAL
                    // destinations (misroute + correct route).
                    hubT.TakeCursor = 0;
                    hubT.ForwardPending(peerCT, record => record.Target == 200UL); // B 的 Ack 误投给 C
                    peerCT.Pump();
                    Check(peerC.Sessions.Count == 0,
                        "匹配：他人 Ack（nonce 不符）不建立会话——废弃「第一个未建立会话」匹配法");
                    hubT.TakeCursor = 0;
                    hubT.ForwardPending(peerCT, record => record.Target == 300UL);
                    peerCT.Pump();
                    Check(peerC.Sessions.Count == 1, "匹配：自己的 Ack（peer+nonce 命中）建立会话");
                    hubT.TakeCursor = 0;
                    hubT.ForwardPending(peerBT, record => record.Target == 200UL);
                    peerBT.Pump();
                    Check(peerB.Sessions.Count == 1, "匹配：B 的会话按自己的 Ack 建立");
                });

                Group("断线与重连换代际", () =>
                {
                    // ---- 断线与重连换代际：断开清理 + Disconnected；重连新会话身份；
                    //      旧会话对象收到 GenerationChanged(新代际)；漏断线时新握手替换旧会话。
                    var genClientT = new HandshakeTestTransport();
                    var genServerT = new HandshakeTestTransport();
                    var genClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(genClientT, localContract, 1401UL);
                    var genServer = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(genServerT, localContract, 2401UL, handshakeInitiator: false);
                    genClientT.ConnectPeer(2401UL);
                    genClientT.ForwardPending(genServerT, null);
                    genServerT.Pump();
                    genServerT.ForwardPending(genClientT, null);
                    genClientT.Pump();
                    Check(genClient.Sessions.Count == 1 && genServer.Sessions.Count == 1, "重连：基线握手建立");
                    var genOldClientSession = genClient.Sessions[0];
                    var genOldServerSession = genServer.Sessions[0];
                    var genClientDisconnected = 0;
                    ulong genClientGenerationSignal = 0;
                    genOldClientSession.Disconnected += () => genClientDisconnected++;
                    genOldClientSession.GenerationChanged += generation => genClientGenerationSignal = generation;
                    var genServerDisconnected = 0;
                    ulong genServerGenerationSignal = 0;
                    genOldServerSession.Disconnected += () => genServerDisconnected++;
                    genOldServerSession.GenerationChanged += generation => genServerGenerationSignal = generation;
                    var genOldClientSessionId = genOldClientSession.SessionId;
                    genClientT.DisconnectPeer(2401UL);
                    Check(genClientDisconnected == 1, "断线：transport 断开触发 Disconnected（established 会话）");
                    Check(genClient.Sessions.Count == 0, "断线：断开方会话即刻出快照（无 ghost）");
                    genServerT.DisconnectPeer(1401UL);
                    Check(genServerDisconnected == 1 && genServer.Sessions.Count == 0, "断线：对端同样清理（两端各自响应 transport 断开）");
                    genClientT.ConnectPeer(2401UL);
                    genClientT.ForwardPending(genServerT, null);
                    genServerT.Pump();
                    genServerT.ForwardPending(genClientT, null);
                    genClientT.Pump();
                    Check(genClient.Sessions.Count == 1 && genClient.Sessions[0].SessionId != genOldClientSessionId,
                        "重连：重连建立新会话身份（连接代际更替）");
                    Check(genClientGenerationSignal == genClient.Sessions[0].SessionId,
                        "重连：旧会话对象收到 GenerationChanged(新代际) 信号");
                    Check(genServer.Sessions.Count == 1 && genServer.Sessions[0].SessionId != genOldServerSession.SessionId,
                        "重连：服务器侧换代际（新 Hello nonce 替换旧会话）");
                    Check(genServerGenerationSignal == genServer.Sessions[0].SessionId,
                        "重连：服务器旧会话对象同样收到 GenerationChanged(新代际)");
                    Check(genClientT.CountKind(HandshakeKindHello) == 2, "重连：重连发出新 Hello（新 nonce）");
                    var staleSession = genClient.Sessions[0];
                    var staleDisconnected = 0;
                    ulong staleGeneration = 0;
                    staleSession.Disconnected += () => staleDisconnected++;
                    staleSession.GenerationChanged += generation => staleGeneration = generation;
                    var staleSessionId = staleSession.SessionId;
                    genClientT.ConnectPeer(2401UL); // 漏断线：未断开即再次连接
                    Check(staleDisconnected == 1, "重连：漏断线时既有 established 会话被替换并触发 Disconnected");
                    genClientT.ForwardPending(genServerT, null);
                    genServerT.Pump();
                    Check(genServer.Sessions.Count == 1, "重连：服务器侧按新 nonce 替换（不双会话）");
                    genServerT.ForwardPending(genClientT, null);
                    genClientT.Pump();
                    Check(genClient.Sessions.Count == 1 && genClient.Sessions[0].SessionId != staleSessionId,
                        "重连：替换后新代际建立");
                    Check(staleGeneration == genClient.Sessions[0].SessionId,
                        "重连：被替换旧会话对象收到 GenerationChanged(新代际)");
                });

                Group("fail-closed", () =>
                {
                    // ---- fail-closed：Reject 按 peer+nonce 精确拆除（另一条握手不受影响、不重试）；
                    //      发起方对契约 Major 不符的 Ack 拒绝建立且不复活 pending。
                    var failClientT = new HandshakeTestTransport();
                    var failGoodServerT = new HandshakeTestTransport();
                    var failBadServerT = new HandshakeTestTransport();
                    var failClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(failClientT, localContract, 1501UL);
                    var failGoodServer = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(failGoodServerT, localContract, 2501UL, handshakeInitiator: false);
                    var failBadServer = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(failBadServerT, new ContractVersion(3, 0), 2601UL, handshakeInitiator: false);
                    failClientT.ConnectPeer(2601UL);
                    failClientT.ConnectPeer(2501UL);
                    failClientT.ForwardPending(failBadServerT, record => record.Index == 0);
                    failClientT.TakeCursor = 0;
                    failClientT.ForwardPending(failGoodServerT, record => record.Index == 1);
                    failBadServerT.Pump();
                    Check(failBadServer.Sessions.Count == 0, "fail-closed：不兼容 Hello 不建立会话");
                    Check(failBadServerT.CountKind(HandshakeKindReject) == 1, "fail-closed：服务器对不兼容 Hello 回 Reject");
                    failGoodServerT.Pump();
                    failBadServerT.ForwardPending(failClientT, null);
                    failGoodServerT.ForwardPending(failClientT, null);
                    failClientT.Pump();
                    Check(failClient.Sessions.Count == 1 && failClient.Sessions[0].PeerSteamId == 2501UL,
                        "fail-closed：Reject 仅拆除匹配的 pending（peer+nonce），另一条握手不受影响");
                    Check(failClientT.CountKind(HandshakeKindHello) == 2, "fail-closed：Reject 后不重发 Hello（版本不兼容不重试）");
                // 帧头权威（R1-Spec BLOCKER 修复轮钉子）：Ack 的 peer 归属以帧头
                // sender 为准——payload steamId 声明不可伪造归属。
                var spoofT = new HandshakeTestTransport();
                var spoofClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(spoofT, localContract, 1503UL);
                spoofT.ConnectPeer(2801UL);
                var spoofNonce = HandshakeRead64(spoofT.Sent[0].Frame, 26);
                spoofT.ForwardPending(null, null);
                spoofT.InjectFrame(BuildControlFrame(HandshakeKindAck, 2801UL, 999UL, 2, 0, spoofNonce));
                spoofT.Pump();
                Check(spoofClient.Sessions.Count == 1 && spoofClient.Sessions[0].PeerSteamId == 2801UL,
                    "身份：Ack 的 peer 归属以帧头 sender 为权威（payload steamId 声明不可伪造归属）");
                    var rogueClientT = new HandshakeTestTransport();
                    var rogueClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(rogueClientT, localContract, 1502UL);
                    rogueClientT.ConnectPeer(2701UL);
                    var rogueNonce = HandshakeRead64(rogueClientT.Sent[0].Frame, 26);
                    rogueClientT.ForwardPending(null, null); // Hello 无人应答
                    rogueClientT.InjectFrame(BuildControlFrame(HandshakeKindAck, 2701UL, 2701UL, 3, 0, rogueNonce));
                    rogueClientT.Pump();
                    Check(rogueClient.Sessions.Count == 0, "fail-closed：Ack 契约 Major 与本地不符 → 不建立（发起方 fail-closed）");
                    rogueClientT.InjectFrame(BuildControlFrame(HandshakeKindAck, 2701UL, 2701UL, 2, 0, rogueNonce));
                    rogueClientT.Pump();
                    Check(rogueClient.Sessions.Count == 0, "fail-closed：被拆除的 pending 不复活（无 ghost）");
                });

                Group("退避", () =>
                {
                    // ---- 退避：1s 起步倍增、8s 封顶、跨重探在途握手可被迟到的 Ack 建立、建立后停止。
                    var backoffNow = 0L;
                    var backoffClientT = new HandshakeTestTransport();
                    var backoffClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(backoffClientT, localContract, 1601UL, true, () => backoffNow);
                    backoffClientT.ConnectPeer(2601UL);
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 1, "退避：连接即发首个 Hello");
                    backoffNow = 999; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 1, "退避：首个重探期限（1s）未到不重发");
                    backoffNow = 1000; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 2, "退避：1s 无 Ack 重探");
                    backoffNow = 2999; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 2, "退避：倍增间隔（2s）未到不重发");
                    backoffNow = 3000; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 3, "退避：第二次重探（累计 3s，间隔 2s）");
                    backoffNow = 7000; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 4, "退避：第三次重探（累计 7s，间隔 4s）");
                    backoffNow = 14999; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 4, "退避：封顶间隔（8s）未到不重发");
                    backoffNow = 15000; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 5, "退避：第四次重探（间隔封顶 8s）");
                    backoffNow = 23000; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 6, "退避：封顶后按 8s 周期续探");
                    Check(backoffClientT.Sent.TrueForAll(record => record.Reliable && record.Target == 0UL && record.Frame[4] == HandshakeKindHello),
                        "退避：重探帧与首帧同形（Hello/untargeted/可靠）");
                    backoffClientT.InjectFrame(BuildControlFrame(HandshakeKindAck, 2601UL, 2601UL, 2, 0, HandshakeRead64(backoffClientT.Sent[0].Frame, 26)));
                    backoffClientT.Pump();
                    Check(backoffClient.Sessions.Count == 1, "退避：迟到的 Ack 建立会话（在途握手跨重探存活）");
                    backoffNow = 100000; backoffClient.TickHandshake();
                    Check(backoffClientT.CountKind(HandshakeKindHello) == 6, "退避：建立后不再重探");
                });

                Group("锁外回调", () =>
                {
                    // ---- 锁外回调：Connected / Disconnected 阻塞期间外线线程可取得状态锁。
                    var lockClientT = new HandshakeTestTransport();
                    var lockServerT = new HandshakeTestTransport();
                    var lockClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(lockClientT, localContract, 1701UL);
                    var lockServer = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(lockServerT, localContract, 2701UL, handshakeInitiator: false);
                    var lockPending = lockClient.StartSession(2701UL); // 手动路径：建立前即可订阅
                    var connectedEntered = new System.Threading.ManualResetEventSlim(false);
                    var releaseConnected = new System.Threading.ManualResetEventSlim(false);
                    var connectedFired = 0;
                    lockPending.Connected += () => { connectedFired++; connectedEntered.Set(); releaseConnected.Wait(System.TimeSpan.FromSeconds(5)); };
                    lockClientT.ForwardPending(lockServerT, null);
                    lockServerT.Pump();
                    var lockPumpTask = System.Threading.Tasks.Task.Run(() => { lockServerT.ForwardPending(lockClientT, null); lockClientT.Pump(); });
                    Check(connectedEntered.Wait(System.TimeSpan.FromSeconds(5)), "锁外：Connected 回调在 Ack 处理中被触发");
                    var lockForeignConnected = System.Threading.Tasks.Task.Run(() => lockClient.Sessions.Count);
                    Check(lockForeignConnected.Wait(System.TimeSpan.FromSeconds(2)),
                        "锁外：Connected 回调阻塞期间外线线程可取得状态锁（Connected 不持状态锁执行）");
                    releaseConnected.Set();
                    Check(lockPumpTask.Wait(System.TimeSpan.FromSeconds(5)) && connectedFired == 1, "锁外：阻塞释放后握手完成恰好一次 Connected");
                    var lockSession = lockClient.Sessions[0];
                    var disconnectedEntered = new System.Threading.ManualResetEventSlim(false);
                    var releaseDisconnected = new System.Threading.ManualResetEventSlim(false);
                    var disconnectedFired = 0;
                    lockSession.Disconnected += () => { disconnectedFired++; disconnectedEntered.Set(); releaseDisconnected.Wait(System.TimeSpan.FromSeconds(5)); };
                    var lockDropTask = System.Threading.Tasks.Task.Run(() => lockClientT.DisconnectPeer(2701UL));
                    Check(disconnectedEntered.Wait(System.TimeSpan.FromSeconds(5)), "锁外：Disconnected 回调被触发");
                    var lockForeignDisconnected = System.Threading.Tasks.Task.Run(() => lockClient.Sessions.Count);
                    Check(lockForeignDisconnected.Wait(System.TimeSpan.FromSeconds(2)),
                        "锁外：Disconnected 回调阻塞期间外线线程可取得状态锁（Disconnected 不持状态锁执行）");
                    releaseDisconnected.Set();
                    Check(lockDropTask.Wait(System.TimeSpan.FromSeconds(5)) && disconnectedFired == 1 && lockClient.Sessions.Count == 0,
                        "锁外：断线清理完成后快照为空");
                });

                Group("重启用重建", () =>
                {
                    // ---- 重启用重建：发起侧重启用按 ConnectedPeers 自动重握手（新代际、订阅无需重挂）；
                    //      响应侧重启用对无会话的对端发复位 Reject，发起方自愈重握手。
                    var rearmClientT = new HandshakeTestTransport();
                    var rearmServerT = new HandshakeTestTransport();
                    var rearmClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(rearmClientT, localContract, 1801UL);
                    var rearmServer = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(rearmServerT, localContract, 2801UL, handshakeInitiator: false);
                    var rearmChannel = new FeatureId("io.example.v2hand");
                    rearmClient.RegisterChannel(rearmChannel, localContract, 1);
                    rearmServer.RegisterChannel(rearmChannel, localContract, 1);
                    var rearmReceived = new List<byte[]>();
                    rearmClient.Subscribe(rearmChannel, ChannelDirection.FromServer, (session, payload) => rearmReceived.Add(payload));
                    rearmClientT.ConnectPeer(2801UL);
                    rearmServerT.ConnectPeer(1801UL);
                    rearmClientT.ForwardPending(rearmServerT, null);
                    rearmServerT.Pump();
                    rearmServerT.ForwardPending(rearmClientT, null);
                    rearmClientT.Pump();
                    var rearmOldClientSession = rearmClient.Sessions[0];
                    var rearmOldDisconnected = 0;
                    ulong rearmOldGeneration = 0;
                    rearmOldClientSession.Disconnected += () => rearmOldDisconnected++;
                    rearmOldClientSession.GenerationChanged += generation => rearmOldGeneration = generation;
                    rearmClient.SetModuleActive(false);
                    Check(rearmClient.Sessions.Count == 0, "停用：快照清空（DEV-V2-14 语义保持）");
                    Check(rearmOldDisconnected == 0, "停用：模块开关本身不触发 Disconnected（DEV-V2-14 冻结语义）");
                    rearmServer.SetModuleActive(false);
                    rearmServer.SetModuleActive(true); // 服务器先重启用（接收先挂回）
                    rearmClient.SetModuleActive(true); // 客户端重启用即按 ConnectedPeers 自动重握手
                    Check(rearmClientT.CountKind(HandshakeKindHello) == 2, "重启用：发起方按传输层仍连接的对端快照自动重发 Hello");
                    rearmClientT.ForwardPending(rearmServerT, null);
                    rearmServerT.Pump();
                    rearmServerT.ForwardPending(rearmClientT, null);
                    rearmClientT.Pump();
                    Check(rearmClient.Sessions.Count == 1 && rearmClient.Sessions[0].SessionId != rearmOldClientSession.SessionId,
                        "重启用：重建会话使用新连接代际");
                    Check(rearmOldGeneration == rearmClient.Sessions[0].SessionId,
                        "重启用：停用前的旧会话对象收到 GenerationChanged(新代际)");
                    Check(rearmServer.Sessions.Count == 1, "重启用：服务器侧同样重建");
                    Check(rearmServer.SendToClients(rearmChannel, new byte[] { 0x33 }, true) == NetworkSendResult.Sent,
                        "重启用：订阅表未重挂即可发送");
                    rearmServerT.ForwardPending(rearmClientT, null);
                    rearmClientT.Pump();
                    Check(rearmReceived.Count == 1 && rearmReceived[0][0] == 0x33, "重启用：已挂订阅无需重挂（Data 直达原订阅）");
                    // 响应侧停用/重启用：对端（发起方）仍持有 established 会话 —— 复位 Reject 触发自愈。
                    var healClientT = new HandshakeTestTransport();
                    var healServerT = new HandshakeTestTransport();
                    var healClient = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(healClientT, localContract, 1901UL);
                    var healServer = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(healServerT, localContract, 2901UL, handshakeInitiator: false);
                    healClientT.ConnectPeer(2901UL);
                    healServerT.ConnectPeer(1901UL);
                    healClientT.ForwardPending(healServerT, null);
                    healServerT.Pump();
                    healServerT.ForwardPending(healClientT, null);
                    healClientT.Pump();
                    var healOldSession = healClient.Sessions[0];
                    var healOldDisconnected = 0;
                    ulong healOldGeneration = 0;
                    healOldSession.Disconnected += () => healOldDisconnected++;
                    healOldSession.GenerationChanged += generation => healOldGeneration = generation;
                    healServer.SetModuleActive(false);
                    healServer.SetModuleActive(true);
                    Check(healServerT.CountKind(HandshakeKindReject) == 1, "自愈：响应侧重启用对无会话的对端发复位 Reject");
                    healServerT.ForwardPending(healClientT, null);
                    healClientT.Pump();
                    Check(healOldDisconnected == 1, "自愈：复位 Reject 拆除发起方的过期 established 会话并触发 Disconnected");
                    Check(healClientT.CountKind(HandshakeKindHello) == 2, "自愈：发起方收到复位后自动重发 Hello");
                    healClientT.ForwardPending(healServerT, null);
                    healServerT.Pump();
                    healServerT.ForwardPending(healClientT, null);
                    healClientT.Pump();
                    Check(healClient.Sessions.Count == 1 && healClient.Sessions[0].SessionId != healOldSession.SessionId,
                        "自愈：复位后以新代际重建（无 ghost）");
                    Check(healOldGeneration == healClient.Sessions[0].SessionId, "自愈：被复位旧会话对象收到 GenerationChanged(新代际)");
                    Check(healServer.Sessions.Count == 1, "自愈：响应方侧重建唯一会话");
                });

            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-17 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V2-18 red anchor: BUE frames come online for real. The inbound
        // decision core gains the BUE branch as step ① (before the LMN seam's
        // ③④⑤), the patch install gate decouples from the standalone-LMN
        // probe (gate = the network module switch), the wire magic renames to
        // BUE1, and the runtime binds to the engine through an injectable
        // binding (fake-transport E2E: subscribe → frame goes live through
        // the decision core → dispatch, both directions). Tests drive the
        // decision core directly and never install patches — the production
        // prefix stays the only ShouldConsumeInbound caller.
        // DEV-V2-22: the LIR adoption red collection — seven groups over the
        // new seams (ReloadContextGuard / IReloadAction+TidyCompletedConsumer
        // / HostTick driver / feature-private repack channel), the frozen
        // overlay point (BII drag-in → forceAddItem prefix → context=false →
        // no reload logic), the own-Harmony-ID stop rule, and the end-to-end
        // tidy→repack chain with the DEV-V2-21 publisher on the loopback
        // transport. Zero Harmony patches are installed by these tests; the
        // overlay point is pinned at the guard's input/output per spec.
        private static void AssertBueV2LirAdoption(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                Group("叠加点 guard", () => LirGroupOverlayGuard(Check));
                Group("Stop 只撤自身", () => LirGroupStopOwnHarmony(Check));
                Group("事件消费", () => LirGroupTidyCompletedConsumer(Check));
                Group("HostTick 双击驱动", () => LirGroupHostTickDriver(Check));
                Group("整理→压弹全链", () => LirGroupTidyRepackChain(Check));
                Group("半注册回滚+首帧延迟", () => LirGroupDeferredInitRollback(Check));
                Group("enabled 原生回退", () => LirGroupEnabledFallback(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V2-22 LIR adoption collection: ALL GREEN (0 failures) — groups: 叠加点 guard/Stop 只撤自身/事件消费/HostTick 双击驱动/整理→压弹全链/半注册回滚+首帧延迟/enabled 原生回退");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-22 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        /// <summary>Records every engine-facing call the LIR net service makes; defaults produce committed transactions.</summary>
        private sealed class FakeLirAuthority : ILirRepackAuthority
        {
            public int RepackCount;
            public ulong LastRepackSteamId;
            public ulong LastRepackRequestId;
            public LirRepackOutcome NextRepackOutcome = LirRepackOutcome.Committed;
            public int NextRepackTotal = 60;

            public int MergeCount;
            public ulong LastMergeSteamId;
            public ulong LastMergeRequestId;
            public LirMergeOutcome NextMergeOutcome = LirMergeOutcome.Committed;

            public int LocalResolveCount;
            public ulong LocalSteamId = 2002UL;
            public bool LocalKnown = true;

            public LirRepackExecution ExecuteRepack(ulong senderSteamId, ulong requestId)
            {
                RepackCount++;
                LastRepackSteamId = senderSteamId;
                LastRepackRequestId = requestId;
                return new LirRepackExecution { Outcome = NextRepackOutcome, TotalTransferred = NextRepackTotal };
            }

            public LirMergeExecution ExecuteMerge(ulong targetSteamId, ulong requestId)
            {
                MergeCount++;
                LastMergeSteamId = targetSteamId;
                LastMergeRequestId = requestId;
                return new LirMergeExecution { Outcome = NextMergeOutcome, TotalMerged = 2 };
            }

            public bool TryResolveLocalPlayerSteamId(out ulong steamId)
            {
                LocalResolveCount++;
                steamId = LocalKnown ? LocalSteamId : 0UL;
                return LocalKnown;
            }
        }

        /// <summary>The IReloadAction test double: records contexts, optionally throws once.</summary>
        private sealed class RecordingLirAction : IReloadAction
        {
            public readonly List<LirReloadActionContext> Calls = new List<LirReloadActionContext>();
            public bool ThrowOnExecute;

            public void Execute(LirReloadActionContext context)
            {
                Calls.Add(context);
                if (ThrowOnExecute) throw new InvalidOperationException("synthetic action failure");
            }
        }

        /// <summary>A probe network: counts registration/subscription calls, delivers nothing.</summary>
        private sealed class LirProbeNetwork : IBueNetworkApi
        {
            public int RegisterCalls;
            public int UnregisterCalls;
            public int SubscribeCalls;
            public int DisposedHandles;

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { RegisterCalls++; return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "PROBE"); }

            public bool UnregisterChannel(FeatureId channel) { UnregisterCalls++; return true; }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            {
                SubscribeCalls++;
                return new ProbeHandle(this);
            }

            public IReadOnlyList<IConnectionSession> Sessions { get { return new IConnectionSession[0]; } }
            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }

            private sealed class ProbeHandle : IDisposable
            {
                private readonly LirProbeNetwork owner;
                internal ProbeHandle(LirProbeNetwork owner) { this.owner = owner; }
                public void Dispose() { owner.DisposedHandles++; }
            }
        }

        /// <summary>A network stub whose SECOND subscribe throws — the half-registration rollback surface (DEV-V2-21 pattern).</summary>
        private sealed class LirRollbackNetwork : IBueNetworkApi
        {
            public int SubscribeCalls;
            public int DisposedHandles;
            public int UnregisterCalls;

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "STUB"); }

            public bool UnregisterChannel(FeatureId channel) { UnregisterCalls++; return true; }

            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            {
                if (++SubscribeCalls >= 2) throw new InvalidOperationException("synthetic second-subscribe failure");
                return new RollbackHandle(this);
            }

            public IReadOnlyList<IConnectionSession> Sessions { get { return new IConnectionSession[0]; } }
            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }

            private sealed class RollbackHandle : IDisposable
            {
                private readonly LirRollbackNetwork owner;
                internal RollbackHandle(LirRollbackNetwork owner) { this.owner = owner; }
                public void Dispose() { owner.DisposedHandles++; }
            }
        }

        /// <summary>Builds a started LIR module on a fresh bus with the given fake authority (no patches land: host-test process).</summary>
        private static InPlaceReloadModule NewLirModule(BetterUnturnedExperience.Core.Events.FeatureEventBus bus, IBueNetworkApi network, FakeLirAuthority authority, bool isServer, out FakeLirAuthority wired)
        {
            var feature = new FeatureId(LirRuntime.FeatureIdValue);
            var module = new InPlaceReloadModule(feature);
            module.AuthorityFactoryForTests = () => authority;
            module.NetServiceFactoryForTests = (m, net) => new LirRepackNetwork(net, m.Authority, () => isServer);
            // DEV-V3-06: the persisted toggle reaches the module only through
            // the injected scoped view — the fixture wires it exactly like the
            // host does (registry -> generation -> view).
            var lirSettings = new FeatureSettingsRegistry(new InMemorySettingsPersistence(), () => true, null);
            lirSettings.GetOrCreateRuntime(feature, InPlaceReloadModule.CreateSettingsDescriptors(feature));
            lirSettings.OpenGeneration(feature, 1UL);
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, lirSettings.CreateView(feature, 1UL), bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, network);
            var result = module.Start(bootstrap);
            if (!result.Started) throw new InvalidOperationException("harness: LIR module start failed: " + result.DiagnosticId);
            wired = authority;
            return module;
        }

        /// <summary>One fake host frame: monotonic tick numbers, the given delta seconds.</summary>
        private static HostTick LirTick(ulong number, float deltaSeconds)
        {
            return new HostTick(number, deltaSeconds, TickPhase.Update);
        }

        // Group 1 — the frozen overlay point: BII drag-in reaches the
        // forceAddItem prefix with NO reload context (the UseableGun prefix
        // never ran), so the guard refuses consumption and the LIR in-place
        // placement decision is never executed — the native path continues.
        // Pinned at the guard's input/output; no Harmony patch is installed.
        private static void LirGroupOverlayGuard(Action<bool, string> check)
        {
            var guard = new ReloadContextGuard();
            check(!guard.TryConsumeSlot(out var slot),
                "叠加点：BII 拖入（无换弹上下文）→ TryConsumeSlot=false → 不执行原位放置逻辑（放行原版）");
            check(slot.Page == 0 && slot.X == 0 && slot.Y == 0 && slot.Rot == 0 && slot.SizeX == 0 && slot.SizeY == 0,
                "叠加点：拒绝消费时槽位六元组输出归零（不残留数据）");
            check(!guard.HasPendingContext, "叠加点：无上下文时 HasPendingContext=false");

            // A real reload: the UseableGun prefix records the new magazine's slot.
            guard.BeginReload(new ReloadSlotContext(2, 3, 4, 0, 1, 2));
            check(guard.HasPendingContext, "叠加点：BeginReload 记录新弹匣完整槽位（page,x,y,rot,sx,sy）");
            check(guard.TryConsumeSlot(out slot)
                && slot.Page == 2 && slot.X == 3 && slot.Y == 4 && slot.Rot == 0 && slot.SizeX == 1 && slot.SizeY == 2,
                "叠加点：有效上下文消费返回完整槽位六元组");
            check(!guard.TryConsumeSlot(out _),
                "叠加点：槽位单次消费——二次消费拒绝（防后续 forceAddItem 误用）");

            // The Postfix bottom-clean: state never leaks across calls.
            guard.BeginReload(new ReloadSlotContext(3, 0, 0, 1, 2, 2));
            guard.Reset();
            check(!guard.HasPendingContext && !guard.TryConsumeSlot(out _),
                "叠加点：Reset 兜底清理（异常路径防状态泄漏到下次调用）");

            // The detach-only (page==255) branch never calls BeginReload —
            // that responsibility sits in the adapter; the guard stores what
            // it is given without second-guessing (one decision, one place).
            guard.BeginReload(new ReloadSlotContext(255, 0, 0, 0, 1, 1));
            check(guard.TryConsumeSlot(out slot) && slot.Page == 255,
                "叠加点：guard 无 255 特判（detach-only 由 adapter 承担，单一决策点）");
            guard.Reset();
        }

        // Group 2 — the stop rule: the module's patches live under the LIR
        // FeatureId Harmony instance and Stop revokes exactly that instance
        // (UnpatchSelf) — never another feature's ID. Cross-feature
        // isolation is structural: each feature owns its own Harmony
        // instance, so an LIR UnpatchSelf cannot touch BII/LIT patches.
        private static void LirGroupStopOwnHarmony(Action<bool, string> check)
        {
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var module = NewLirModule(bus, new LirProbeNetwork(), new FakeLirAuthority(), isServer: true, out _);
            check(module.PatchHarmonyId == LirRuntime.FeatureIdValue,
                "Stop 只撤自身：补丁 Harmony ID = LIR FeatureId（io.github.yu80rice.bue.in-place-reload，收编非旧 GUID）");

            module.Stop(FeatureStopReason.PluginStopping);
            check(!module.PatchesInstalled, "Stop 只撤自身：Stop 后自身补丁已撤销（UnpatchSelf）");
            check(module.PatchHarmonyId == LirRuntime.FeatureIdValue,
                "Stop 只撤自身：撤销对象始终是自己的 Harmony ID（从不触他功能 ID）");
            check(InPlaceReloadModule.ActiveModule == null,
                "Stop 只撤自身：补丁句柄已清空——adapter 层即刻不再持有本模块（双保险）");

            module.Stop(FeatureStopReason.UserDisabled);
            check(!module.PatchesInstalled, "Stop 只撤自身：重复 Stop 幂等");
        }

        // Group 3 — TidyCompleted consumption: success + scope + idempotency +
        // target resolution + exception isolation. The consumer never acts on
        // a blind event (spec「LIR 不得见事件就盲执行」).
        private static void LirGroupTidyCompletedConsumer(Action<bool, string> check)
        {
            var action = new RecordingLirAction();
            // generation → steamId: 0 → local (2002), 7 → peer 1001, unknown → 0.
            Func<ulong, ulong> resolver = gen => gen == 0UL ? 2002UL : (gen == 7UL ? 1001UL : 0UL);
            var consumer = new TidyCompletedConsumer(action, resolver);
            var lit = new FeatureId(LitRuntime.FeatureIdValue);

            consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, 0UL, 1UL));
            check(action.Calls.Count == 1 && action.Calls[0].TargetSteamId == 2002UL,
                "消费：成功+范围符合（gen=0 本地）→ 动作恰好执行一次且目标=本地玩家");
            check(action.Calls[0].ConnectionGeneration == 0UL && action.Calls[0].TransactionId == 1UL,
                "消费：动作上下文携带连接代际与事务号（可追溯）");

            consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Rejected, 0UL, 2UL));
            consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Failed, 0UL, 3UL));
            check(action.Calls.Count == 1, "消费：结果非成功（Rejected/Failed）→ 不执行（盲执行被禁止）");

            consumer.Handle(new TidyCompleted(lit, 1, 6, TidyCompletionResult.Succeeded, 0UL, 4UL));
            consumer.Handle(new TidyCompleted(lit, 2, 7, TidyCompletionResult.Succeeded, 0UL, 5UL));
            check(action.Calls.Count == 1, "消费：范围出界（压弹域 2..6 之外）→ 不执行");

            // R1-Spec DEVIATION-1 fix: a malformed range (inverted bounds) is
            // not a valid scope either — it must never reach an action.
            consumer.Handle(new TidyCompleted(lit, 6, 2, TidyCompletionResult.Succeeded, 0UL, 9UL));
            check(action.Calls.Count == 1, "消费：非法范围（首页>末页）→ 不执行");

            consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, 7UL, 6UL));
            check(action.Calls.Count == 2 && action.Calls[1].TargetSteamId == 1001UL,
                "消费：联机会话代际 → 经会话解析目标 peer（服务器代执行整理的旧 postfix 语义保持）");

            consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, 9UL, 7UL));
            check(action.Calls.Count == 2, "消费：会话代际解析失败（目标不可知）→ 跳过不执行");

            consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, 7UL, 6UL));
            consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, 7UL, 5UL));
            check(action.Calls.Count == 2, "消费：事务号幂等（重复/回退事务号不二次执行）");

            consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, 0UL, 0UL));
            check(action.Calls.Count == 2, "消费：事务号 0 非法 → 拒绝");

            action.ThrowOnExecute = true;
            Exception leaked = null;
            try { consumer.Handle(new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, 0UL, 8UL)); }
            catch (Exception error) { leaked = error; }
            check(leaked == null, "消费：动作异常被隔离，不扩散回事件总线");
            action.ThrowOnExecute = false;
        }

        // Group 4 — HostTick-driven period work: the double-tap detector with
        // the 0.3s window (single click passes the native reload through),
        // and the bounded main-thread dispatcher (queue limit 64, per-sender
        // coalescing, success-first drain, work TTL expiry).
        private static void LirGroupHostTickDriver(Action<bool, string> check)
        {
            int fires = 0;
            bool keyDown = false;
            var driver = new ReloadInputDriver(() => keyDown, () => fires++);
            ulong tickNumber = 0;
            void Frame(bool pressed, float dt)
            {
                keyDown = pressed;
                driver.Tick(LirTick(++tickNumber, dt));
            }

            Frame(false, 0.1f); Frame(false, 0.1f);
            check(fires == 0, "驱动：无按键帧不触发");

            Frame(true, 0.1f);
            check(fires == 0, "驱动：单击只落基线（原版换弹放行）");
            Frame(false, 0.05f);
            Frame(true, 0.05f);
            check(fires == 1, "驱动：0.3s 窗口内第二次按下触发原位压弹（恰一次）");

            // After a trigger the baseline is CLEARED: the next press only
            // re-arms (the anti-machine-gun rule), the one after fires.
            Frame(false, 0.05f);
            Frame(true, 0.05f);
            check(fires == 1, "驱动：触发后下一按只重锚基线（连击防护）");
            Frame(false, 0.05f);
            Frame(true, 0.05f);
            check(fires == 2, "驱动：重臂后的下一次窗口内双击再次触发");
            Frame(false, 0.02f);

            Frame(true, 0.1f); Frame(false, 0.4f); Frame(true, 0.05f);
            check(fires == 2, "驱动：窗口外（>0.3s）第二次按下不触发，基线重锚");

            Frame(false, 0.1f);
            Frame(true, 0.15f); Frame(false, 0.0f); Frame(true, 0.15f);
            check(fires == 3, "驱动：窗口边界（0.3s 含）内触发（旧 <= 阈值语义保持）");

            keyDown = false;
            var boomDriver = new ReloadInputDriver(() => { throw new InvalidOperationException("synthetic key fault"); }, () => fires++);
            Exception leaked = null;
            try { boomDriver.Tick(LirTick(++tickNumber, 0.01f)); }
            catch (Exception error) { leaked = error; }
            check(leaked == null, "驱动：键轮询异常被隔离，不抛回宿主时钟链");

            // ── the bounded main-thread dispatcher (state progression + TTL) ──
            // MaxPerFrame=16 mirrors the old per-frame cap — the host drains
            // every frame, so the tests drain to empty the same way.
            var executed = new List<string>();
            var toasts = new List<int>();
            void DrainAll(LirRepackDispatcher target)
            {
                for (var round = 0; round < 8; round++) target.DrainOnMainThread((sender, reqId) => executed.Add("req:" + sender + ":" + reqId), (reqId, total) => toasts.Add(total));
            }

            var queue = new LirRepackDispatcher();
            for (ulong i = 1; i <= ReloadRuntimePolicy.QueueLimit; i++)
            {
                if (!queue.TryEnqueueRequest(1000UL + i, i)) { check(false, "队列：第 " + i + " 个发送者应受理"); break; }
            }
            check(!queue.TryEnqueueRequest(9999UL, 9999UL),
                "队列：上限 " + ReloadRuntimePolicy.QueueLimit + " 满——第 65 个发送者拒绝（fail-closed）");
            DrainAll(queue);
            check(executed.Count == ReloadRuntimePolicy.QueueLimit, "队列：drain 状态推进——64 项全部派发");

            var coalesceQueue = new LirRepackDispatcher();
            check(coalesceQueue.TryEnqueueRequest(1001UL, 1UL), "队列：新队列受理");
            check(coalesceQueue.TryEnqueueRequest(1001UL, 2UL), "队列：同发送者合并（coalesce 返回 true 不占槽）");
            DrainAll(coalesceQueue);
            check(executed.Count == ReloadRuntimePolicy.QueueLimit + 1 && executed[executed.Count - 1] == "req:1001:1",
                "队列：同发送者只派发首次请求（旧合并语义保持）");

            var priorityQueue = new LirRepackDispatcher();
            priorityQueue.TryEnqueueRequest(1001UL, 1UL);
            priorityQueue.TryEnqueueSuccess(9UL, 60);
            var order = new List<string>();
            priorityQueue.DrainOnMainThread((sender, reqId) => order.Add("req"), (reqId, total) => order.Add("toast:" + total));
            check(order.Count == 2 && order[0] == "toast:60" && order[1] == "req",
                "队列：回包优先于请求（防请求洪泛饿死客户端 UI，旧语义保持）");

            var ttlQueue = new LirRepackDispatcher();
            long fakeTicks = 1_000_000;
            ttlQueue.TicksForTests = () => fakeTicks;
            check(ttlQueue.TryEnqueueRequest(1001UL, 1UL), "TTL：受理");
            fakeTicks += (long)(System.Diagnostics.Stopwatch.Frequency * (ReloadRuntimePolicy.WorkTtlSeconds + 1f));
            DrainAll(ttlQueue);
            check(executed.Count == ReloadRuntimePolicy.QueueLimit + 1,
                "TTL：超时判断——超过工作 TTL 的积压项被丢弃不执行（入队不等于授权）");

            var closedQueue = new LirRepackDispatcher();
            closedQueue.Shutdown();
            check(!closedQueue.TryEnqueueRequest(1001UL, 1UL), "队列：Shutdown 后拒绝入队（停机不接受新工作）");
        }

        /// <summary>The two-peer loopback harness for the LIR chain: client (1001) double-taps, server (2002) executes; both modules run with fake authorities.</summary>
        private sealed class LirMultiplayerHarness
        {
            public BetterUnturnedExperience.Core.Network.LocalLoopbackPair Pair;
            public BetterUnturnedExperience.Core.Network.BueNetworkRuntime ServerRuntime;
            public BetterUnturnedExperience.Core.Network.BueNetworkRuntime ClientRuntime;
            public InPlaceReloadModule ServerLir;
            public InPlaceReloadModule ClientLir;
            public FakeLirAuthority ServerAuthority;
            public FakeLirAuthority ClientAuthority;
            public BetterUnturnedExperience.Core.Events.FeatureEventBus ServerBus;
            public BetterUnturnedExperience.Core.Events.FeatureEventBus ClientBus;
            public readonly List<string> ServerToasts = new List<string>();
            public readonly List<string> ClientToasts = new List<string>();
            public IConnectionSession ServerSession;
            public IConnectionSession ClientSession;
            private ulong tickNumber;

            public static LirMultiplayerHarness Create()
            {
                var localContract = new ContractVersion(2, 0);
                var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                var clientRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1001UL);
                var serverRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL, handshakeInitiator: false);
                var harness = new LirMultiplayerHarness
                {
                    Pair = pair,
                    ClientRuntime = clientRuntime,
                    ServerRuntime = serverRuntime,
                    ServerAuthority = new FakeLirAuthority(),
                    ClientAuthority = new FakeLirAuthority(),
                };
                harness.ServerBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                harness.ServerLir = CreateModule(harness.ServerBus, serverRuntime, isServer: true, harness.ServerAuthority, harness.ServerToasts);
                harness.ClientBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                harness.ClientLir = CreateModule(harness.ClientBus, clientRuntime, isServer: false, harness.ClientAuthority, harness.ClientToasts);
                return harness;
            }

            private static InPlaceReloadModule CreateModule(BetterUnturnedExperience.Core.Events.FeatureEventBus bus, BetterUnturnedExperience.Core.Network.BueNetworkRuntime runtime, bool isServer, FakeLirAuthority authority, List<string> toasts)
            {
                var feature = new FeatureId(LirRuntime.FeatureIdValue);
                var module = new InPlaceReloadModule(feature);
                module.AuthorityFactoryForTests = () => authority;
                module.NetServiceFactoryForTests = (m, net) => new LirRepackNetwork(net, m.Authority, () => isServer);
                module.RoleProbeForTests = () => isServer;
                module.ToastSink = message => toasts.Add(message);
                var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, null, bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, runtime);
                var result = module.Start(bootstrap);
                if (!result.Started) throw new InvalidOperationException("harness: LIR module start failed: " + result.DiagnosticId);
                return module;
            }

            /// <summary>The automatic handshake: the client initiates; two pump rounds establish both sides.</summary>
            public void Handshake()
            {
                ClientSession = ClientRuntime.StartSession(2002UL);
                Pump();
                Pump();
                if (ClientRuntime.Sessions.Count != 1 || ServerRuntime.Sessions.Count != 1)
                    throw new InvalidOperationException("harness: handshake did not establish both sides");
                ServerSession = ServerRuntime.Sessions[0];
            }

            /// <summary>Drives one host frame on BOTH modules (session reconcile, deferred init, dispatcher drain, input).</summary>
            public void TickAll()
            {
                tickNumber++;
                ServerLir.OnHostTick(LirTick(tickNumber, 0.016f));
                ClientLir.OnHostTick(LirTick(tickNumber, 0.016f));
            }

            public void Pump()
            {
                Pair.First.Pump();
                Pair.Second.Pump();
            }
        }

        // Group 5 — the tidy→repack chain end-to-end on the loopback transport:
        // DEV-V2-21's TidyCompleted publish feeds LIR's consumer (server-side,
        // generation→peer target resolution), and the client double-tap rides
        // the feature-private repack channel to the server authority and back.
        private static void LirGroupTidyRepackChain(Action<bool, string> check)
        {
            var harness = LirMultiplayerHarness.Create();
            var lit = new FeatureId(LitRuntime.FeatureIdValue);

            // Client double-tap before any session: refused, nothing on the wire.
            harness.ClientLir.OnDoubleTapReload();
            check(harness.ServerAuthority.RepackCount == 0, "全链：无会话时客机压弹请求拒绝发送（联机网络层未就绪语义保持）");

            harness.Handshake();
            harness.TickAll();
            check(harness.ServerSession != null && harness.ClientSession != null, "全链：会话建立");

            // The DEV-V2-21 publisher: LIT reports a committed tidy for the
            // client's session generation → LIR merges THAT peer's magazines.
            var published = harness.ServerBus.Publisher(lit).TryPublish(TidyCompleted.EventId,
                new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, harness.ServerSession.SessionId, 42UL));
            check(published, "全链：TidyCompleted 经宿主总线发布（DEV-V2-21 发布端）");
            check(harness.ServerAuthority.MergeCount == 1 && harness.ServerAuthority.LastMergeSteamId == 1001UL && harness.ServerAuthority.LastMergeRequestId != 0UL,
                "全链：LIR 消费验成功+范围 → 整理后自动压弹恰好一次且目标=整理发起 peer（代际解析）");

            // A failed tidy never reaches the reload action.
            harness.ServerBus.Publisher(lit).TryPublish(TidyCompleted.EventId,
                new TidyCompleted(lit, 2, 6, TidyCompletionResult.Rejected, harness.ServerSession.SessionId, 43UL));
            check(harness.ServerAuthority.MergeCount == 1, "全链：整理失败（Rejected）→ 不自动压弹");

            // Client double-tap repack over the wire: request → server
            // authority → targeted reliable reply → client toast.
            harness.ClientLir.OnDoubleTapReload();
            check(harness.ServerAuthority.RepackCount == 0, "全链：请求入队后须待服务器主线程 drain 才执行（状态推进）");
            harness.Pump();
            harness.TickAll();
            check(harness.ServerAuthority.RepackCount == 1 && harness.ServerAuthority.LastRepackSteamId == 1001UL && harness.ServerAuthority.LastRepackRequestId != 0UL,
                "全链：服务器权威执行压弹事务（目标=请求 peer，请求号非零）");
            harness.Pump();
            harness.TickAll();
            check(harness.ClientToasts.Count == 1 && harness.ClientToasts[0].Contains("60"),
                "全链：服务器按会话定向回包 → 客机 toast 显示压入 60 发（requestId 匹配回包链）");
            check(harness.ServerToasts.Count == 0, "全链：远端客户端的成功不在服务器本地显示（toast 归属正确）");

            // The server-role double-tap (single player / listen host): local
            // execution through the same main-thread entry, local toast.
            harness.ServerLir.OnDoubleTapReload();
            harness.TickAll();
            check(harness.ServerAuthority.RepackCount == 2 && harness.ServerAuthority.LastRepackSteamId == 2002UL,
                "全链：单机/房主双击走本地主线程事务入口（与远端请求共用，不自发网络包）");
            check(harness.ServerToasts.Count == 1 && harness.ServerToasts[0].Contains("60"),
                "全链：本地玩家成功 toast 本地显示");
        }

        // Group 6 — the network registration is deferred to the first frame
        // game thread (the migrated semantic, now inside the module
        // lifecycle) with half-registration rollback: a second-subscribe
        // failure disposes every handle, unregisters the channel and leaves
        // the local path alive — and never retries.
        private static void LirGroupDeferredInitRollback(Action<bool, string> check)
        {
            // Deferred: no channel traffic at Start, exactly once at the
            // first HostTick.
            var probe = new LirProbeNetwork();
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var module = NewLirModule(bus, probe, new FakeLirAuthority(), isServer: true, out _);
            check(probe.RegisterCalls == 0 && probe.SubscribeCalls == 0,
                "首帧延迟：模块 Start 不注册频道（注册延迟到首帧游戏线程，语义保持）");
            module.OnHostTick(LirTick(1UL, 0.016f));
            check(probe.RegisterCalls == 1 && probe.SubscribeCalls == 2,
                "首帧延迟：首个 HostTick 注册频道+双方向订阅（各恰一次）");
            module.OnHostTick(LirTick(2UL, 0.016f));
            check(probe.RegisterCalls == 1 && probe.SubscribeCalls == 2,
                "首帧延迟：后续帧不重复初始化");
            module.Stop(FeatureStopReason.PluginStopping);
            check(probe.UnregisterCalls == 1 && probe.DisposedHandles == 2,
                "首帧延迟：Stop 注销频道并释放全部订阅句柄");

            // Half-registration rollback: the SECOND subscribe throws.
            var rollback = new LirRollbackNetwork();
            var rollbackBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var rollbackModule = NewLirModule(rollbackBus, rollback, new FakeLirAuthority(), isServer: true, out _);
            Exception leaked = null;
            try { rollbackModule.OnHostTick(LirTick(1UL, 0.016f)); }
            catch (Exception error) { leaked = error; }
            check(leaked == null, "半注册：初始化异常不抛回宿主时钟链");
            check(rollbackModule.MultiplayerReady == false,
                "半注册：联机子系统启动失败如实上报（MultiplayerReady=false）");
            check(rollback.UnregisterCalls == 1 && rollback.DisposedHandles == 1,
                "半注册：已成功方向被撤销（句柄 Dispose+频道注销），零残留");
            rollbackModule.OnHostTick(LirTick(2UL, 0.016f));
            check(rollback.SubscribeCalls == 2, "半注册：失败后不重试（单机路径保持可用）");
            check(rollbackModule.Started, "半注册：模块本体保持 Started（本地路径存活）");
        }

        // Group 7 — the single persisted switch: off = native fallback (own
        // patches off, no reload work at all), on = everything re-arms.
        private static void LirGroupEnabledFallback(Action<bool, string> check)
        {
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var probe = new LirProbeNetwork();
            var authority = new FakeLirAuthority();
            var module = NewLirModule(bus, probe, authority, isServer: true, out _);
            check(module.Enabled, "设置：默认开启（唯一持久化开关 enabled）");

            var off = SubmitLirToggle(module, false);
            check(off, "设置：面板开关可关闭");
            module.RefreshSwitches();
            check(!module.Enabled && !module.PatchesInstalled, "设置：关闭=原生回退（自身补丁已撤）");
            module.OnDoubleTapReload();
            module.OnHostTick(LirTick(10UL, 0.016f));
            check(authority.RepackCount == 0, "设置：关闭后双击不产生任何压弹行为");
            module.OnHostTick(LirTick(11UL, 0.016f));
            var lit = new FeatureId(LitRuntime.FeatureIdValue);
            bus.Publisher(lit).TryPublish(TidyCompleted.EventId, new TidyCompleted(lit, 2, 6, TidyCompletionResult.Succeeded, 0UL, 1UL));
            check(authority.MergeCount == 0, "设置：关闭后整理完成事件不触发自动压弹");

            var on = SubmitLirToggle(module, true);
            check(on, "设置：可重新开启（提交受理）");
            module.RefreshSwitches();
            check(module.Enabled, "设置：重新开启后模块翻转回启用");
            check(module.PatchesInstalled || module.StartGateDiagnostics.Length > 0, "设置：开启=补丁重装（测试进程装不上时如实留诊断）");
            module.Stop(FeatureStopReason.UserDisabled);
        }

        private static ulong lirToggleRequestId;

        /// <summary>Submits the LIR enabled toggle through the frozen scoped settings seam (unique request id, current revision); returns acceptance.</summary>
        private static bool SubmitLirToggle(InPlaceReloadModule module, bool enabled)
        {
            var revision = module.SettingsView.GetSnapshot(SettingRevisionScope.ClientPreference).Revision;
            var result = module.SettingsView.Submit(new ScopedSettingChangeRequest(++lirToggleRequestId, SettingRevisionScope.ClientPreference, revision,
                new[] { new SettingMutation("inplacereload.enabled", SettingValue.Toggle(enabled)) }));
            return result.Accepted;
        }

        private static void AssertBueV2FrameBinding(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                // Each group collects independently so the red transcript names
                // a failure per frozen group instead of aborting at the first.
                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                var localContract = new ContractVersion(2, 0);
                var bindingChannel = new FeatureId("io.example.v2bind");

                Group("帧分类", () =>
                {
                    Check(BetterUnturnedExperience.Core.Network.BueFrameClassifier.FrameMagic == "BUE1",
                        "帧分类：线帧魔数冻结为 BUE1（4 字节帧头布局不变，产品语言一律「BUE 帧」）");
                    var bueFrame = BuildBue1Frame(bindingChannel.Value, 1001UL, new byte[] { 0x01, 0x02 });
                    var modFrame = new byte[] { 0x4D, 0x4F, 0x44, 0x67, 0x0A };
                    var lmn2Frame = new byte[] { 0x4C, 0x4D, 0x4E, 0x32, 0x01, 0xBB };
                    Check(BetterUnturnedExperience.Core.Network.BueFrameClassifier.IsBueFrame(bueFrame, 0, bueFrame.Length),
                        "帧分类：BUE1 帧被识别为 BUE 帧");
                    Check(!BetterUnturnedExperience.Core.Network.BueFrameClassifier.IsBueFrame(modFrame, 0, modFrame.Length)
                        && !BetterUnturnedExperience.Core.Network.BueFrameClassifier.IsBueFrame(lmn2Frame, 0, lmn2Frame.Length),
                        "帧分类：MOD/LMN2 帧不误分类为 BUE 帧");
                    Check(!BetterUnturnedExperience.Core.Network.LmnFrameClassifier.IsLmnFrame(bueFrame),
                        "帧分类：BUE 帧不误分类为 LMN 帧（两类识别互斥，零误分类）");
                    Check(!BetterUnturnedExperience.Core.Network.BueFrameClassifier.IsBueFrame(null, 0, 0)
                        && !BetterUnturnedExperience.Core.Network.BueFrameClassifier.IsBueFrame(new byte[] { (byte)'B', (byte)'U', (byte)'E' }, 0, 3),
                        "帧分类：null 与短于魔数的窗口不分类为 BUE 帧");
                    var padded = new byte[] { 0x00, 0x7F, (byte)'B', (byte)'U', (byte)'E', (byte)'1', 0x00, 0x02 };
                    Check(BetterUnturnedExperience.Core.Network.BueFrameClassifier.IsBueFrame(padded, 2, 6)
                        && !BetterUnturnedExperience.Core.Network.BueFrameClassifier.IsBueFrame(padded, 8, 2),
                        "帧分类：offset/size 窗口语义与越界防御成立");
                });

                Group("六步顺序", () =>
                {
                    var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2bind-" + Guid.NewGuid().ToString("N"));
                    // 探针 false + 网络开：BUE 帧仍被消费（① BUE 识别 → ② 模块开，
                    // 先于 ③ LMN 探针门）——旧决策核把全部分支压在一个 takeover
                    // 布尔下，此断言在旧实现上必红。
                    var engine = new FakeBueEngine { IsServer = true, LocalSteamId = 2002UL };
                    var dormant = new NetworkModuleAdapter(adapterRoot, () => false, () => null, () => null, () => { }, null, null, engine.ToBinding());
                    dormant.ActivateCore();
                    dormant.TickNetwork();
                    Check(dormant.NetworkApi != null,
                        "六步：网络模块开时运行时武装（探针 false 不阻 BUE seam）");
                    if (dormant.NetworkApi == null) return;
                    dormant.NetworkApi.RegisterChannel(bindingChannel, localContract, 1);
                    // 建立响应方会话：探针 false 的适配器同样吃 BUE Hello（直驱决策核）
                    var dormantHello = BuildBue1HelloFrame(1001UL, 2, 0, 12345UL);
                    Check(dormant.ShouldConsumeInbound(true, 1001UL, dormantHello, 0, dormantHello.Length, null),
                        "六步：探针 false 时 BUE Hello 同样被消费");
                    dormant.TickNetwork();
                    Check(dormant.NetworkApi.Sessions.Count == 1 && dormant.NetworkApi.Sessions[0].PeerSteamId == 1001UL,
                        "六步：消费的 Hello 在运行时建立会话（帧头 sender 归属）");
                    var received = new List<byte[]>();
                    dormant.NetworkApi.Subscribe(bindingChannel, ChannelDirection.FromClients, (session, payload) => received.Add(payload));
                    var dataFrame = BuildBue1Frame(bindingChannel.Value, 1001UL, new byte[] { 0x2A });
                    Check(dormant.ShouldConsumeInbound(true, 1001UL, dataFrame, 0, dataFrame.Length, null),
                        "六步：探针 false 时 BUE 帧仍被决策核消费（① BUE 识别 → ② 模块开 → BUE 消费，先于 ③）");
                    dormant.TickNetwork();
                    Check(received.Count == 1 && received[0].Length == 1 && received[0][0] == 0x2A,
                        "六步：消费的 BUE 帧经泵上线并派发到 FromClients 订阅");
                    var modFrame = new byte[] { 0x4D, 0x4F, 0x44, 0x67, 0x0A };
                    Check(!dormant.ShouldConsumeInbound(true, 7UL, modFrame, 0, modFrame.Length, null),
                        "六步：探针 false 时 MOD 帧原样交还（LMN seam 整体不进，零 LMN 动作）");
                    var lmn2Frame = new byte[] { 0x4C, 0x4D, 0x4E, 0x32, 0x01, 0xBB };
                    Check(!dormant.ShouldConsumeInbound(false, 0UL, lmn2Frame, 0, lmn2Frame.Length, null),
                        "六步：探针 false 时 LMN2 帧原样交还");

                    // 探针 true + LMN 双向 live：BUE 帧不受 live 释放影响——
                    // live 释放只作用于 MOD/LMN2（每帧恰一次由 LMN 派发），两 seam 不共用布尔。
                    var liveEngine = new FakeBueEngine { IsServer = true, LocalSteamId = 2002UL };
                    var live = new NetworkModuleAdapter(adapterRoot, () => true, () => null, () => null, () => { }, () => true, () => true, liveEngine.ToBinding());
                    live.ActivateCore();
                    live.TickNetwork();
                    Check(live.NetworkApi != null, "六步：LMN live 下运行时照常武装");
                    if (live.NetworkApi == null) return;
                    live.NetworkApi.RegisterChannel(bindingChannel, localContract, 1);
                    var liveHello = BuildBue1HelloFrame(1001UL, 2, 0, 12346UL);
                    Check(live.ShouldConsumeInbound(true, 1001UL, liveHello, 0, liveHello.Length, null),
                        "六步：LMN live 时 BUE Hello 仍由 BUE 消费");
                    live.TickNetwork();
                    Check(live.NetworkApi.Sessions.Count == 1, "六步：LMN live 下 Hello 照常建立会话");
                    var liveReceived = new List<byte[]>();
                    live.NetworkApi.Subscribe(bindingChannel, ChannelDirection.FromClients, (session, payload) => liveReceived.Add(payload));
                    Check(live.ShouldConsumeInbound(true, 1001UL, dataFrame, 0, dataFrame.Length, null),
                        "六步：LMN live 时 BUE 帧仍由 BUE 消费（不落入 live 释放分支）");
                    live.TickNetwork();
                    Check(liveReceived.Count == 1, "六步：LMN live 下 BUE 帧照常上线派发");
                });

                Group("patch 门", () =>
                {
                    var diagnostics = new List<string>();
                    var previousSink = NetworkModuleAdapter.DiagnosticLogSink;
                    NetworkModuleAdapter.DiagnosticLogSink = line => diagnostics.Add(line);
                    try
                    {
                        var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2bind-" + Guid.NewGuid().ToString("N"));
                        // patch 安装门 = 网络模块启用（与 LMN 探针解耦）：探针 false
                        // 且网络开时安装仍被尝试。安装尝试不依赖 TypeByName 是否
                        // 成功——fail-closed 或真装都浮出同一 event=takeover-patch
                        // 诊断，其存在即锚点；旧实现此处零输出。
                        // 计数探针钉「零 LMN 探测动作」：live 探测函数一次都不得被
                        // 调用（R1 修复钉子——安装路径的探测调用已加门）。
                        // 组末 IsolateAndDetach：若 TypeByName 在本宿主真装了
                        // Harmony 前缀，隔离确保不留残留补丁（R2 SMELL fix）。
                        var clientProbeCalls = 0;
                        var serverProbeCalls = 0;
                        var bare = new NetworkModuleAdapter(adapterRoot, () => false, () => null, () => null, () => { },
                            () => { clientProbeCalls++; return false; },
                            () => { serverProbeCalls++; return false; });
                        bare.ActivateCore();
                        bare.ApplyNetworkPatches();
                        Check(CountToken(diagnostics, "event=takeover-patch") == 1,
                            "patch 门：探针 false 且网络开时 patch 安装仍被尝试（安装门与 LMN 探针解耦）");
                        Check(clientProbeCalls == 0 && serverProbeCalls == 0,
                            "patch 门：探针 false 时 LMN live 探测零调用（零 LMN 反射，R1 修复钉子）");
                        Check(!diagnostics.Exists(line => line.Contains("event=v1-table-mirror")),
                            "patch 门：探针 false 时零 LMN 相关镜像动作（零镜像）");
                        Check(!bare.LmnNativeDispatchLive,
                            "patch 门：探针 false 时不探测 LMN 自有前缀状态（零 LMN 反射）");
                        bare.IsolateAndDetach();
                    }
                    finally { NetworkModuleAdapter.DiagnosticLogSink = previousSink; }
                });

                Group("live 恰一次", () =>
                {
                    var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2bind-" + Guid.NewGuid().ToString("N"));
                    FakeLmnModTransport.Reset();
                    FakeLmnModTransport.ClientHandlers[103] = reader => { FakeLmnModTransport.ClientCalls++; };
                    var engine = new FakeBueEngine { IsServer = true, LocalSteamId = 2002UL };
                    // client 方向 live、server 方向 inert（DEV-V2-12 冻结：live/inert
                    // 每方向独立判断）——新决策核必须原样保持该语义。
                    var mixed = new NetworkModuleAdapter(adapterRoot, () => true, () => typeof(FakeLmnModTransport), () => typeof(FakeLmnModRouter), () => { }, () => true, () => false, engine.ToBinding());
                    mixed.ActivateCore();
                    mixed.TickNetwork();
                    Assert(BetterUnturnedExperience.Core.Network.LmnV1FrameCodec.TryBuild(103, new byte[] { 0x11 }, out var v1Frame),
                        "setup: the V1 mirror frame builds");
                    Check(!mixed.ShouldConsumeInbound(true, 4242UL, v1Frame, 0, v1Frame.Length, null),
                        "live：client 方向 live 时 V1 帧被释放（LMN 原生派发恰一次，BUE 不重复派发）");
                    Check(mixed.ShouldConsumeInbound(false, 0UL, v1Frame, 0, v1Frame.Length, null) && FakeLmnModTransport.ClientCalls == 1,
                        "live：server 方向 inert 时 V1 帧由 BUE 处理（方向独立判断）");
                });

                Group("异常隔离", () =>
                {
                    var clockNow = 1000000L;
                    var diagnostics = new List<string>();
                    var previousSink = NetworkModuleAdapter.DiagnosticLogSink;
                    NetworkModuleAdapter.DiagnosticLogSink = line => diagnostics.Add(line);
                    try
                    {
                        var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2bind-" + Guid.NewGuid().ToString("N"));
                        var engine = new FakeBueEngine { IsServer = false, LocalSteamId = 1001UL, ClientPeer = 2002UL };
                        engine.SendOverride = (frame, reliable, target) => { throw new InvalidOperationException("engine send fault"); };
                        var client = new NetworkModuleAdapter(adapterRoot, () => false, () => null, () => null, () => { }, null, null, engine.ToBinding(), () => clockNow);
                        client.ActivateCore();
                        var threw = false;
                        try { client.TickNetwork(); }
                        catch (Exception) { threw = true; }
                        Check(!threw, "异常：引擎发送异常不逃逸 TickNetwork（泵级隔离，游戏 Update 链不断）");
                        Check(CountToken(diagnostics, "event=bue-runtime-pump result=failed") == 1,
                            "异常：泵故障浮出结构化故障行（每级隔离一次，不刷屏）");
                        Check(client.NetworkApi != null, "异常：泵故障后运行时保持武装（隔离不拆运行时）");
                        // 自愈：发送恢复 + 退避到期 → 泵内 TickHandshake 重探 → Hello 上线
                        engine.SendOverride = null;
                        clockNow += 1500;
                        client.TickNetwork();
                        Check(engine.Sent.Count == 1 && engine.Sent[0].Frame[4] == 1 && engine.Sent[0].Target == 0UL,
                            "异常：退避到期后 TickHandshake 经泵重发 Hello（自愈路径上线）");
                        // 隔离后 BUE 帧交还 vanilla（hand-back，不吞不炸）
                        client.IsolateAndDetach();
                        var lateFrame = BuildBue1Frame(bindingChannel.Value, 2002UL, new byte[] { 1 });
                        Check(!client.ShouldConsumeInbound(false, 0UL, lateFrame, 0, lateFrame.Length, null),
                            "异常：隔离后 BUE 帧交还 vanilla（决策核 hand-back，无残留状态引用）");
                    }
                    finally { NetworkModuleAdapter.DiagnosticLogSink = previousSink; }
                });

                Group("网络关闭", () =>
                {
                    var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2bind-" + Guid.NewGuid().ToString("N"));
                    var engine = new FakeBueEngine { IsServer = true, LocalSteamId = 2002UL, ServerPeers = { 1001UL } };
                    var adapter = new NetworkModuleAdapter(adapterRoot, () => false, () => null, () => null, () => { }, null, null, engine.ToBinding());
                    adapter.ActivateCore();
                    adapter.ApplyNetworkPatches();
                    adapter.TickNetwork();
                    Check(adapter.NetworkApi != null, "网络关闭：武装基线成立");
                    if (adapter.NetworkApi == null) return;
                    adapter.NetworkApi.RegisterChannel(bindingChannel, localContract, 1);
                    var hello = BuildBue1HelloFrame(1001UL, 2, 0, 12347UL);
                    Check(adapter.ShouldConsumeInbound(true, 1001UL, hello, 0, hello.Length, null),
                        "网络关闭：开启基线下 BUE 帧被消费");
                    adapter.TickNetwork();
                    Check(adapter.NetworkApi.Sessions.Count == 1, "网络关闭：会话建立基线");
                    // 关闭：BUE 帧不消费（交还 vanilla）；同一开关移除 patch 并
                    // 停用运行时（测试宿主无法真装 patch，patch 移除的生产证据
                    // 随实机验收 24；此处钉行为面）。DEV-V4-04：开关事实改由
                    // 生命周期意图库承载（旧 network.enabled 文档键退役）。
                    Assert(BueFeatureIntentRuntime.StoreFor(adapterRoot).RecordUserDisabled(NetworkModuleAdapter.NetworkFeature),
                        "setup: the network switch is turned off (lifecycle intent record)");
                    adapter.RefreshSwitches();
                    var offFrame = BuildBue1Frame(bindingChannel.Value, 1001UL, new byte[] { 0x66 });
                    Check(!adapter.ShouldConsumeInbound(true, 1001UL, hello, 0, hello.Length, null)
                        && !adapter.ShouldConsumeInbound(true, 1001UL, offFrame, 0, offFrame.Length, null),
                        "网络关闭：BUE 帧一律不消费（网络关闭不消费契约）");
                    Check(adapter.NetworkApi.Sessions.Count == 0,
                        "网络关闭：停用语义零会话（DEV-V2-14 冻结）");
                    Check(adapter.NetworkApi.SendToServer(bindingChannel, new byte[] { 1 }, true) == NetworkSendResult.NoSession,
                        "网络关闭：发送显式 NoSession（不新增专用错误码）");
                    // 重开：消费恢复（可逆开关）
                    Assert(BueFeatureIntentRuntime.StoreFor(adapterRoot).Clear(NetworkModuleAdapter.NetworkFeature),
                        "setup: the network switch is re-enabled (disable intent cleared)");
                    adapter.RefreshSwitches();
                    Check(adapter.ShouldConsumeInbound(true, 1001UL, hello, 0, hello.Length, null),
                        "网络关闭：重开后 BUE 帧消费恢复");
                    adapter.TickNetwork();
                    Check(adapter.NetworkApi.Sessions.Count == 1, "网络关闭：重开后会话重建（重启用语义）");
                });

                Group("生产绑定E2E", () =>
                {
                    var adapterRoot = Path.Combine(Path.GetTempPath(), "bue-v2bind-" + Guid.NewGuid().ToString("N"));
                    var serverEngine = new FakeBueEngine { IsServer = true, LocalSteamId = 2002UL };
                    var clientEngine = new FakeBueEngine { IsServer = false, LocalSteamId = 1001UL };
                    serverEngine.ServerPeers.Add(1001UL);
                    var server = new NetworkModuleAdapter(adapterRoot, () => false, () => null, () => null, () => { }, null, null, serverEngine.ToBinding());
                    var client = new NetworkModuleAdapter(adapterRoot, () => false, () => null, () => null, () => { }, null, null, clientEngine.ToBinding());
                    server.ActivateCore();
                    client.ActivateCore();
                    server.TickNetwork(); // server 角色即决：首泵即武装
                    Check(server.NetworkApi != null,
                        "E2E：server 角色首泵即武装（client 角色随连接决，见 Hello 上线）");
                    if (server.NetworkApi == null) return;
                    server.NetworkApi.RegisterChannel(bindingChannel, localContract, 1);
                    // 客户端上线（武装先于注册：角色随连接决，Hello 无需频道注册）
                    clientEngine.ClientPeer = 2002UL;
                    client.TickNetwork();
                    Check(client.NetworkApi != null, "E2E：client 角色随连接决并武装");
                    if (client.NetworkApi == null) return;
                    client.NetworkApi.RegisterChannel(bindingChannel, localContract, 1);
                    var serverGot = new List<CapturedDispatch>();
                    var clientGot = new List<CapturedDispatch>();
                    server.NetworkApi.Subscribe(bindingChannel, ChannelDirection.FromClients, (session, payload) => serverGot.Add(new CapturedDispatch { Sender = session.PeerSteamId, Payload = payload }));
                    client.NetworkApi.Subscribe(bindingChannel, ChannelDirection.FromServer, (session, payload) => clientGot.Add(new CapturedDispatch { Sender = session.PeerSteamId, Payload = payload }));

                    Check(clientEngine.Sent.Count == 1 && clientEngine.Sent[0].Frame[4] == 1 && clientEngine.Sent[0].Target == 0UL,
                        "E2E：客户端泵武装后自动发出 Hello（单帧 untargeted，生产绑定发起方）");
                    Check(clientEngine.Sent[0].Frame[0] == (byte)'B' && clientEngine.Sent[0].Frame[1] == (byte)'U'
                        && clientEngine.Sent[0].Frame[2] == (byte)'E' && clientEngine.Sent[0].Frame[3] == (byte)'1',
                        "E2E：上线帧魔数为 BUE1（线形 pin）");
                    // 帧上线：Hello 进入服务器决策核（生产 prefix 唯一调用者的直驱替身）→ 泵派发
                    var hello = clientEngine.Sent[0].Frame;
                    Check(server.ShouldConsumeInbound(true, 1001UL, hello, 0, hello.Length, null),
                        "E2E：服务器决策核消费客户端 BUE 帧");
                    server.TickNetwork();
                    Check(serverEngine.Sent.Count == 1 && serverEngine.Sent[0].Frame[4] == 2 && serverEngine.Sent[0].Target == 1001UL,
                        "E2E：响应方同步 Ack 定向回发起方");
                    Check(server.NetworkApi.Sessions.Count == 1 && server.NetworkApi.Sessions[0].PeerSteamId == 1001UL,
                        "E2E：服务器侧会话以帧头 sender 建立（established 快照可见）");
                    var ack = serverEngine.Sent[0].Frame;
                    Check(client.ShouldConsumeInbound(false, 0UL, ack, 0, ack.Length, null),
                        "E2E：客户端决策核消费服务器 Ack");
                    client.TickNetwork();
                    Check(client.NetworkApi.Sessions.Count == 1,
                        "E2E：发起方握手完成（established 快照可见）");
                    var clientSession = client.NetworkApi.Sessions[0];
                    var clientSessionDisconnected = 0;
                    clientSession.Disconnected += () => clientSessionDisconnected++;

                    // client→server 生产发送（SendToServer：单帧 untargeted）
                    Check(client.NetworkApi.SendToServer(bindingChannel, new byte[] { 0x33 }, true) == NetworkSendResult.Sent,
                        "E2E：客户端 SendToServer 经生产绑定送出");
                    var uplink = FindSentByKind(clientEngine.Sent, 0);
                    Check(uplink != null && uplink.Target == 0UL && uplink.Reliable,
                        "E2E：上行数据帧 untargeted 且可靠位透传");
                    Check(server.ShouldConsumeInbound(true, 1001UL, uplink.Frame, 0, uplink.Frame.Length, null),
                        "E2E：服务器决策核消费上行数据帧");
                    server.TickNetwork();
                    Check(serverGot.Count == 1 && serverGot[0].Sender == 1001UL && serverGot[0].Payload[0] == 0x33,
                        "E2E：上行帧按帧头 sender 解析会话并派发 FromClients 订阅");

                    // server→client 生产发送（SendToClient：会话寻径 targeted）
                    var serverSession = server.NetworkApi.Sessions[0];
                    Check(server.NetworkApi.SendToClient(bindingChannel, serverSession, new byte[] { 0x44 }, false) == NetworkSendResult.Sent,
                        "E2E：服务器 SendToClient 按会话定向发送");
                    var downlink = FindSentByKind(serverEngine.Sent, 0);
                    Check(downlink != null && downlink.Target == 1001UL && !downlink.Reliable,
                        "E2E：下行数据帧定向到会话 peer 且可靠位透传");
                    Check(client.ShouldConsumeInbound(false, 0UL, downlink.Frame, 0, downlink.Frame.Length, null),
                        "E2E：客户端决策核消费下行数据帧");
                    client.TickNetwork();
                    Check(clientGot.Count == 1 && clientGot[0].Sender == 2002UL && clientGot[0].Payload[0] == 0x44,
                        "E2E：下行帧派发 FromServer 订阅（订阅 → 帧上线 → 派发 全链绿）");

                    // SendToClients 会话驱动组播（每会话一帧 targeted，非无目标广播）
                    Check(server.NetworkApi.SendToClients(bindingChannel, new byte[] { 0x55 }, true) == NetworkSendResult.Sent,
                        "E2E：SendToClients 组播聚合 Sent");
                    var multicast = FindSentByKind(serverEngine.Sent, 0);
                    Check(multicast != null && multicast.Target == 1001UL,
                        "E2E：组播逐会话定向（target=peer）");
                    Check(client.ShouldConsumeInbound(false, 0UL, multicast.Frame, 0, multicast.Frame.Length, null),
                        "E2E：客户端决策核消费组播帧");
                    client.TickNetwork();
                    Check(clientGot.Count == 2 && clientGot[1].Payload[0] == 0x55,
                        "E2E：组播帧派发到既有订阅");

                    // 断线清理：客户端对端快照归零 → PeerDisconnected → 无 ghost
                    clientEngine.ClientPeer = 0UL;
                    client.TickNetwork();
                    Check(clientSessionDisconnected == 1, "E2E：对端断开触发 Disconnected（会话对象回调）");
                    Check(client.NetworkApi.Sessions.Count == 0, "E2E：断线清理无 ghost");
                    serverEngine.ServerPeers.Remove(1001UL);
                    server.TickNetwork();
                    Check(server.NetworkApi.Sessions.Count == 0, "E2E：服务器侧断线清理同步无 ghost");
                });

                // groups continue (辅助桩与方法)
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V2-18 frame-binding collection: ALL GREEN (0 failures) — groups: 帧分类/六步顺序/patch 门/live 恰一次/异常隔离/网络关闭/生产绑定E2E");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-18 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V2-19: contract-piece red regression — the TidyCompleted feature
        // event and the HostTick host clock (registry entries ⑤⑥). Each group
        // collects independently so the red transcript names a failure per
        // frozen group instead of aborting at the first (DEV-V2-18 pattern).
        private static void AssertBueV2EventBusAndHostTick(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                var litFeature = new FeatureId("io.github.yu80rice.bue.inventory-tidy");
                var thirdParty = new FeatureId("io.example.thirdparty");

                Group("事件发布订阅", () =>
                {
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    var lit = bus.Publisher(litFeature);
                    var sub = bus.Subscriber(litFeature);
                    var received = new List<TidyCompleted>();
                    var handle = sub.Subscribe<TidyCompleted>(received.Add);
                    var evt = new TidyCompleted(litFeature, 2, 6, TidyCompletionResult.Succeeded, 0UL, 77UL);
                    Check(lit.TryPublish(TidyCompleted.EventId, evt),
                        "发布订阅：发布者身份派生的合法身份串 TryPublish=true");
                    Check(received.Count == 1, "发布订阅：订阅者收到事件");
                    Check(received.Count == 1 && received[0].Publisher.Value == litFeature.Value && received[0].FirstPage == 2
                        && received[0].LastPage == 6 && received[0].Result == TidyCompletionResult.Succeeded
                        && received[0].ConnectionGeneration == 0UL && received[0].TransactionId == 77UL,
                        "发布订阅：载荷逐字段保真（发布者/范围/结果/代际/事务标识）");
                    var received2 = new List<TidyCompleted>();
                    var handle2a = sub.Subscribe<TidyCompleted>(received2.Add);
                    var handle2b = sub.Subscribe<TidyCompleted>(received2.Add);
                    lit.TryPublish(TidyCompleted.EventId, evt);
                    Check(received2.Count == 2, "发布订阅：同一委托两次订阅各得一份派发（独立句柄）");
                    handle2a.Dispose();
                    handle2a.Dispose();
                    received2.Clear();
                    lit.TryPublish(TidyCompleted.EventId, evt);
                    Check(received2.Count == 1, "发布订阅：Dispose 只注销自己的委托且重复 Dispose 安全");
                    handle2b.Dispose();
                    handle.Dispose();
                    bool nullHandlerRejected = false;
                    try { sub.Subscribe<TidyCompleted>(null); }
                    catch (ArgumentNullException) { nullHandlerRejected = true; }
                    Check(nullHandlerRejected, "发布订阅：null handler 属开发者错误，参数异常 fail-fast");
                });

                Group("发布者语义", () =>
                {
                    var diagnostics = new List<string>();
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus(diagnostics.Add);
                    var lit = bus.Publisher(litFeature);
                    var sub = bus.Subscriber(litFeature);
                    var received = new List<TidyCompleted>();
                    sub.Subscribe<TidyCompleted>(received.Add);
                    Check(!lit.TryPublish("io.example.someone-else/tidy-completed", default(TidyCompleted)),
                        "发布者语义：非本功能派生的身份串 TryPublish=false（Owned 语义）");
                    Check(!lit.TryPublish(null, default(TidyCompleted)),
                        "发布者语义：null 身份串 TryPublish=false");
                    Check(!lit.TryPublish(HostTick.EventId, default(HostTick)),
                        "发布者语义：功能发布者不能以宿主身份发布 HostTick（时钟由宿主统一产生）");
                    Check(received.Count == 0, "发布者语义：被拒发布零派发");
                    lit.TryPublish("io.example.bad/evt", default(TidyCompleted));
                    Check(diagnostics.Count > 0 && diagnostics[diagnostics.Count - 1].IndexOf("io.example.bad/evt", StringComparison.Ordinal) >= 0,
                        "发布者语义：拒绝发布浮出结构化诊断（不静默吞）");
                    var emptyBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    Check(emptyBus.Publisher(litFeature).TryPublish(TidyCompleted.EventId, default(TidyCompleted)),
                        "发布者语义：零订阅者发布仍为 true（发布≠派发）");
                });

                Group("异常隔离", () =>
                {
                    var got = new List<TidyCompleted>();
                    var diagnostics = new List<string>();
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus(diagnostics.Add);
                    var lit = bus.Publisher(litFeature);
                    var sub = bus.Subscriber(litFeature);
                    sub.Subscribe<TidyCompleted>(delegate { throw new InvalidOperationException("bad consumer"); });
                    sub.Subscribe<TidyCompleted>(got.Add);
                    var evt = new TidyCompleted(litFeature, 2, 2, TidyCompletionResult.Succeeded, 0UL, 5UL);
                    Check(lit.TryPublish(TidyCompleted.EventId, evt),
                        "异常隔离：单订阅者异常不冲击发布者（TryPublish 正常返回 true）");
                    Check(got.Count == 1, "异常隔离：坏订阅者不阻断后续订阅者的派发");
                    Check(diagnostics.Count > 0 && diagnostics[diagnostics.Count - 1].IndexOf(TidyCompleted.EventId, StringComparison.Ordinal) >= 0,
                        "异常隔离：订阅者异常浮出结构化诊断（不静默吞）");
                });

                Group("假时钟单调", () =>
                {
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    long nowMs = 0;
                    var clock = new BetterUnturnedExperience.Core.Events.HostTickClock(bus, () => nowMs);
                    var ticks = new List<HostTick>();
                    bus.Subscriber(thirdParty).Subscribe<HostTick>(ticks.Add);
                    nowMs = 0; clock.Tick();
                    nowMs = 16; clock.Tick();
                    nowMs = 16; clock.Tick();
                    nowMs = 50; clock.Tick();
                    Check(ticks.Count == 4, "假时钟：每 Tick 恰一 HostTick");
                    Check(ticks.Count == 4 && ticks[0].TickNumber == 1UL && ticks[1].TickNumber == 2UL
                        && ticks[2].TickNumber == 3UL && ticks[3].TickNumber == 4UL,
                        "假时钟：序号从 1 起严格单调 +1");
                    Check(ticks.Count == 4 && Math.Abs(ticks[0].DeltaTime) < 0.0001f
                        && Math.Abs(ticks[1].DeltaTime - 0.016f) < 0.001f
                        && Math.Abs(ticks[2].DeltaTime) < 0.0001f
                        && Math.Abs(ticks[3].DeltaTime - 0.034f) < 0.001f,
                        "假时钟：时间增量=相邻 tick 的单调时差（首 tick 0）");
                    bool allUpdate = true;
                    foreach (var t in ticks) if (t.Phase != TickPhase.Update) allUpdate = false;
                    Check(allUpdate, "假时钟：阶段固定为 Update");
                    Check((byte)TickPhase.Update == 0, "假时钟：TickPhase.Update 值冻结为 0");
                    Check(HostTick.EventId == "io.github.yu80rice.bue.host/host-tick",
                        "假时钟：HostTick 身份串由宿主标识派生并冻结");
                    nowMs = 10; clock.Tick();
                    Check(ticks.Count == 5 && ticks[4].TickNumber == 5UL && ticks[4].DeltaTime == 0f,
                        "假时钟：时间源回退钳 0 且序号仍严格 +1（R1 修复钉）");
                    nowMs = 20; clock.Tick();
                    Check(ticks.Count == 6 && ticks[5].TickNumber == 6UL && ticks[5].DeltaTime == 0f,
                        "假时钟：回退后基线保持高水位——后续 tick 增量仍为 0 而非回退差值（R3 修复钉）");
                    Check(BetterUnturnedExperience.Core.Events.HostTickClock.MonotonicMilliseconds(10_000_000L, 10_000_000L) == 1000L,
                        "假时钟：默认时钟换算按 Stopwatch.Frequency 标定（1 秒 = 1000ms，R1 修复钉）");
                    Check(BetterUnturnedExperience.Core.Events.HostTickClock.MonotonicMilliseconds(5_000_000L, 10_000_000L) == 500L,
                        "假时钟：默认时钟换算 0.5 秒 = 500ms（R1 修复钉）");
                    Check(BetterUnturnedExperience.Core.Events.HostTickClock.MonotonicMilliseconds(3_000_000L, 3_000_000L) == 1000L,
                        "假时钟：换算按实际 Frequency 标定（3MHz 计数器 1 秒 = 1000ms——旧式固定除 10^7 的回归在此必红，R2 修复钉）");
                    bool badFrequencyRejected = false;
                    try { BetterUnturnedExperience.Core.Events.HostTickClock.MonotonicMilliseconds(1L, 0L); }
                    catch (ArgumentOutOfRangeException) { badFrequencyRejected = true; }
                    Check(badFrequencyRejected, "假时钟：换算函数对非正 Frequency fail-fast");
                });

                Group("停止注销", () =>
                {
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    long nowMs = 0;
                    var clock = new BetterUnturnedExperience.Core.Events.HostTickClock(bus, () => nowMs);
                    var litTicks = new List<HostTick>();
                    var ecoTicks = new List<HostTick>();
                    bus.Subscriber(litFeature).Subscribe<HostTick>(litTicks.Add);
                    bus.Subscriber(thirdParty).Subscribe<HostTick>(ecoTicks.Add);
                    clock.Tick();
                    Check(litTicks.Count == 1 && ecoTicks.Count == 1, "停止注销：注销前双方都收到时钟");
                    Check(bus.UnsubscribeAll(litFeature), "停止注销：宿主注销功能订阅返回 true");
                    clock.Tick();
                    Check(litTicks.Count == 1, "停止注销：功能停止后不再收到时钟（自动注销语义）");
                    Check(ecoTicks.Count == 2, "停止注销：其它功能的订阅不受影响");
                    Check(!bus.UnsubscribeAll(new FeatureId("io.example.never-subscribed")),
                        "停止注销：无订阅的功能注销返回 false");
                    Check(clock.Tick(), "停止注销：注销后时钟照常产针");
                });

                Group("宿主身份保留", () =>
                {
                    // R1-Spec DEVIATION-2 修复：宿主标识是总线保留身份——不可铸入
                    // 公开发布者视图（fail-fast），否则任何代码都能伪造宿主时钟。
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    bool hostIdentityBlocked = false;
                    try { bus.Publisher(new FeatureId(BetterUnturnedExperience.Core.Events.HostTickClock.HostPublisherId)); }
                    catch (ArgumentException) { hostIdentityBlocked = true; }
                    Check(hostIdentityBlocked, "宿主身份保留：宿主标识不可铸入公开发布者视图（fail-fast）");
                    long nowMs = 0;
                    var clock = new BetterUnturnedExperience.Core.Events.HostTickClock(bus, () => nowMs);
                    var ticks = new List<HostTick>();
                    bus.Subscriber(thirdParty).Subscribe<HostTick>(ticks.Add);
                    nowMs = 7; clock.Tick();
                    Check(ticks.Count == 1 && ticks[0].TickNumber == 1UL,
                        "宿主身份保留：宿主时钟内部发布路径不受保留门影响");
                });

                Group("生态同权", () =>
                {
                    // 生态功能（非官方注册路径）与官方功能走完全相同的公开缝：
                    // 同一事件总线订阅官方 LIT 的 TidyCompleted 与宿主 HostTick。
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    var litPub = bus.Publisher(litFeature);
                    var ecoSub = bus.Subscriber(thirdParty);
                    var tidy = new List<TidyCompleted>();
                    var ticks = new List<HostTick>();
                    ecoSub.Subscribe<TidyCompleted>(tidy.Add);
                    ecoSub.Subscribe<HostTick>(ticks.Add);
                    var clock = new BetterUnturnedExperience.Core.Events.HostTickClock(bus, () => 100L);
                    clock.Tick();
                    litPub.TryPublish(TidyCompleted.EventId,
                        new TidyCompleted(litFeature, 2, 6, TidyCompletionResult.Succeeded, 0UL, 1UL));
                    Check(ticks.Count == 1 && tidy.Count == 1,
                        "生态同权：生态订阅者经同一公开缝收到宿主时钟与官方功能事件");
                    Check(!bus.Publisher(thirdParty).TryPublish(TidyCompleted.EventId, default(TidyCompleted)),
                        "生态同权：生态功能同样不能伪造他人身份串（同权=同一 Owned 规则，不是无规则）");
                });

                Group("生产接线", () =>
                {
                    // 插件组合根：总线+宿主时钟一次成型；Update 泵驱动；订阅者
                    // 异常不逃逸泵链（决策核四级隔离泵同一纪律）。
                    BetterUnturnedExperience.Plugin.BueHostEventRuntime.Clear();
                    try
                    {
                        Check(BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated(),
                            "生产接线：组合根首次创建总线与时钟");
                        Check(!BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated(),
                            "生产接线：重复创建幂等（一次成型）");
                        var productionTicks = new List<HostTick>();
                        BetterUnturnedExperience.Plugin.BueHostEventRuntime.Bus.Subscriber(thirdParty).Subscribe<HostTick>(productionTicks.Add);
                        Check(BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce(),
                            "生产接线：Update 泵驱动的 TickOnce 正常产针");
                        Check(productionTicks.Count == 1 && productionTicks[0].TickNumber == 1UL,
                            "生产接线：生产时钟经公开缝派发（序号从 1 起）");
                        BetterUnturnedExperience.Plugin.BueHostEventRuntime.Bus.Subscriber(thirdParty).Subscribe<HostTick>(
                            delegate { throw new InvalidOperationException("pump poison"); });
                        Check(BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce(),
                            "生产接线：订阅者异常不逃逸泵链（TickOnce 仍正常返回）");
                        Check(productionTicks.Count == 2,
                            "生产接线：毒订阅者不阻断正常订阅者的后续派发");
                    }
                    finally { BetterUnturnedExperience.Plugin.BueHostEventRuntime.Clear(); }
                });
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V2-19 event-clock collection: ALL GREEN (0 failures) — groups: 事件发布订阅/发布者语义/异常隔离/假时钟单调/停止注销/宿主身份保留/生态同权/生产接线");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-19 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V2-18 red-regression engine stub: the full engine-facing binding
        // surface (role resolve, local identity, server peer snapshot, client
        // peer, engine send, optional injected clock) as plain delegates with
        // captured sends, so the production wiring is driven without touching
        // Assembly-CSharp. SendOverride injects engine faults for the
        // pump-isolation group.
        private sealed class FakeBueEngine
        {
            internal sealed class SentFrame { internal byte[] Frame; internal bool Reliable; internal ulong Target; }

            internal bool IsServer;
            internal ulong LocalSteamId;
            internal readonly List<ulong> ServerPeers = new List<ulong>();
            internal ulong ClientPeer;
            internal Func<long> Clock = null; // null = the runtime's default monotonic clock
            internal Func<byte[], bool, ulong, bool> SendOverride;
            internal readonly List<SentFrame> Sent = new List<SentFrame>();

            internal BueEngineNetBinding ToBinding()
            {
                var self = this;
                return new BueEngineNetBinding(
                    () => self.IsServer,
                    () => self.LocalSteamId,
                    () => self.ServerPeers.ToArray(),
                    () => self.ClientPeer,
                    (frame, reliable, target) =>
                    {
                        if (self.SendOverride != null) return self.SendOverride(frame, reliable, target);
                        lock (self.Sent) self.Sent.Add(new SentFrame { Frame = (byte[])frame.Clone(), Reliable = reliable, Target = target });
                        return true;
                    },
                    self.Clock);
            }
        }

        private sealed class CapturedDispatch
        {
            internal ulong Sender;
            internal byte[] Payload;
        }

        // DEV-V2-18: hand-built BUE1 wire frame (magic 4 + kind 1 + chanLen 1 +
        // channel + sender 8 + payload) for direct decision-core feeding.
        private static byte[] BuildBue1Frame(string channelId, ulong sender, byte[] payload)
        {
            var channelBytes = System.Text.Encoding.UTF8.GetBytes(channelId ?? string.Empty);
            var frame = new byte[14 + channelBytes.Length + payload.Length];
            frame[0] = (byte)'B'; frame[1] = (byte)'U'; frame[2] = (byte)'E'; frame[3] = (byte)'1';
            frame[4] = 0; // KindData
            frame[5] = (byte)channelBytes.Length;
            Buffer.BlockCopy(channelBytes, 0, frame, 6, channelBytes.Length);
            for (var i = 0; i < 8; i++) frame[6 + channelBytes.Length + i] = (byte)(sender >> (i * 8));
            Buffer.BlockCopy(payload, 0, frame, 14 + channelBytes.Length, payload.Length);
            return frame;
        }

        // DEV-V2-18: hand-built BUE1 Hello (34 bytes — magic 4 + kind 1 +
        // chanLen 0 + sender 8 + control payload [steamId 8][major 2][minor 2]
        // [nonce 8]) for establishing a responder-side session without the
        // runtime's own send path.
        private static byte[] BuildBue1HelloFrame(ulong sender, ushort major, ushort minor, ulong nonce)
        {
            var frame = new byte[34];
            frame[0] = (byte)'B'; frame[1] = (byte)'U'; frame[2] = (byte)'E'; frame[3] = (byte)'1';
            frame[4] = 1; // KindHello
            frame[5] = 0;
            for (var i = 0; i < 8; i++) frame[6 + i] = (byte)(sender >> (i * 8));
            for (var i = 0; i < 8; i++) frame[14 + i] = (byte)(sender >> (i * 8));
            frame[22] = (byte)major;
            frame[23] = (byte)(major >> 8);
            frame[24] = (byte)minor;
            frame[25] = (byte)(minor >> 8);
            for (var i = 0; i < 8; i++) frame[26 + i] = (byte)(nonce >> (i * 8));
            return frame;
        }

        private static FakeBueEngine.SentFrame FindSentByKind(List<FakeBueEngine.SentFrame> sent, byte kind)
        {
            FakeBueEngine.SentFrame found = null;
            lock (sent)
            {
                foreach (var record in sent)
                {
                    if (record.Frame[4] != kind) continue;
                    found = record;
                }
            }
            return found;
        }

        // DEV-V2-17 red-regression transport: full lifecycle control — records
        // every send, raises PeerConnected/PeerDisconnected on demand, injects
        // raw frames into the receive path, and exposes the ConnectedPeers
        // snapshot the runtime re-probes on re-enable.
        private sealed class HandshakeTestTransport : BetterUnturnedExperience.Core.Network.INetworkTransport
        {
            private readonly Queue<byte[]> incoming = new Queue<byte[]>();
            private readonly List<ulong> connectedPeers = new List<ulong>();
            public event Action<byte[]> Receive;
            public event Action<ulong> PeerConnected;
            public event Action<ulong> PeerDisconnected;
            public IReadOnlyList<ulong> ConnectedPeers { get { lock (connectedPeers) return connectedPeers.ToArray(); } }
            internal readonly List<SentRecord> Sent = new List<SentRecord>();
            internal int TakeCursor;
            public bool Send(byte[] frame, bool reliable, ulong targetSteamId)
            {
                if (frame == null) return false;
                lock (Sent) Sent.Add(new SentRecord(Sent.Count, (byte[])frame.Clone(), reliable, targetSteamId));
                return true;
            }
            public int Pump()
            {
                var count = 0;
                while (true)
                {
                    byte[] frame;
                    lock (incoming) { if (incoming.Count == 0) break; frame = incoming.Dequeue(); }
                    var callback = Receive;
                    if (callback != null) callback(frame);
                    count++;
                }
                return count;
            }
            internal void ConnectPeer(ulong peer)
            {
                lock (connectedPeers) if (!connectedPeers.Contains(peer)) connectedPeers.Add(peer);
                var raised = PeerConnected;
                if (raised != null) raised(peer);
            }
            internal void DisconnectPeer(ulong peer)
            {
                lock (connectedPeers) connectedPeers.Remove(peer);
                var raised = PeerDisconnected;
                if (raised != null) raised(peer);
            }
            internal void InjectFrame(byte[] frame)
            {
                if (frame == null) return;
                lock (incoming) incoming.Enqueue((byte[])frame.Clone());
            }
            internal void ForwardPending(HandshakeTestTransport to, Func<SentRecord, bool> filter)
            {
                while (true)
                {
                    SentRecord record;
                    lock (Sent)
                    {
                        if (TakeCursor >= Sent.Count) return;
                        record = Sent[TakeCursor];
                        TakeCursor++;
                    }
                    if (to != null && (filter == null || filter(record))) to.InjectFrame(record.Frame);
                }
            }
            internal int CountKind(byte kind)
            {
                var count = 0;
                lock (Sent) foreach (var record in Sent) if (record.Frame[4] == kind) count++;
                return count;
            }
        }

        private sealed class SentRecord
        {
            internal SentRecord(int index, byte[] frame, bool reliable, ulong target)
            { Index = index; Frame = frame; Reliable = reliable; Target = target; }
            internal int Index { get; }
            internal byte[] Frame { get; }
            internal bool Reliable { get; }
            internal ulong Target { get; }
        }

        // Wire-shape pin for the fail-closed probes: magic "BUE1" (DEV-V2-18
        // rename) + kind +
        // chanLen 0 + header sender 8 + control payload [steamId 8][major 2]
        // [minor 2][nonce 8] = 34 bytes. Kind byte pins: 1=Hello, 2=Ack, 3=Reject.
        private const byte HandshakeKindHello = 1;
        private const byte HandshakeKindAck = 2;
        private const byte HandshakeKindReject = 3;

        private static byte[] BuildControlFrame(byte kind, ulong headerSender, ulong payloadSteamId, ushort major, ushort minor, ulong nonce)
        {
            var frame = new byte[34];
            frame[0] = (byte)'B'; frame[1] = (byte)'U'; frame[2] = (byte)'E'; frame[3] = (byte)'1';
            frame[4] = kind;
            frame[5] = 0;
            HandshakeWrite64(frame, 6, headerSender);
            HandshakeWrite64(frame, 14, payloadSteamId);
            frame[22] = (byte)major;
            frame[23] = (byte)(major >> 8);
            frame[24] = (byte)minor;
            frame[25] = (byte)(minor >> 8);
            HandshakeWrite64(frame, 26, nonce);
            return frame;
        }

        private static void HandshakeWrite64(byte[] bytes, int offset, ulong value)
        {
            for (var i = 0; i < 8; i++) bytes[offset + i] = (byte)(value >> (i * 8));
        }

        private static ulong HandshakeRead64(byte[] bytes, int offset)
        {
            ulong value = 0;
            for (var i = 0; i < 8; i++) value |= ((ulong)bytes[offset + i]) << (i * 8);
            return value;
        }

        private static string LitTestTag(int index)
        {
            return index == 0 ? "a" : index == 1 ? "b" : "c";
        }

        // ItemJar.item is a read-only native property; the host fixture
        // injects the Item through whichever instance field carries it.
        private static void SetJarItem(ItemJar jar, Item item)
        {
            foreach (var field in typeof(ItemJar).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
            {
                if (field.FieldType == typeof(Item))
                {
                    field.SetValue(jar, item);
                    return;
                }
            }
            throw new InvalidOperationException("no Item field on ItemJar — fixture cannot run");
        }

        private static PackableItem LitTestItem(string tag, byte sizeX, byte sizeY, ushort groupKey, int stableOrder, byte originalX, byte originalY)
        {
            return new PackableItem
            {
                Tag = tag,
                size_x = sizeX,
                size_y = sizeY,
                GroupKey = groupKey,
                StableOrder = stableOrder,
                OriginalX = originalX,
                OriginalY = originalY,
                OriginalRot = 0,
                PreferredRotation = 0,
            };
        }

        private sealed class FixedPlanStrategyAdapter : ITidyStrategy
        {
            private readonly TidyPlan plan;
            internal FixedPlanStrategyAdapter(string strategyId, TidyPlan plan)
            {
                StrategyId = strategyId;
                this.plan = plan;
            }
            public string StrategyId { get; }
            public TidyPlan BuildPlan(TidyInput input)
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                var placements = new List<PackableItem>(plan.Placements.Count);
                for (var index = 0; index < plan.Placements.Count; index++)
                {
                    var source = plan.Placements[index];
                    placements.Add(new PackableItem
                    {
                        Tag = source?.Tag,
                        size_x = source?.size_x ?? 0,
                        size_y = source?.size_y ?? 0,
                        GroupKey = source?.GroupKey ?? 0,
                        StableOrder = index,
                        OriginalX = source?.OriginalX ?? 0,
                        OriginalY = source?.OriginalY ?? 0,
                        OriginalRot = source?.OriginalRot ?? 0,
                        PreferredRotation = source?.PreferredRotation ?? 0,
                        Placed = source != null && source.Placed,
                        ResultX = source?.ResultX ?? 0,
                        ResultY = source?.ResultY ?? 0,
                        ResultRot = source?.ResultRot ?? 0,
                    });
                }
                return new TidyPlan(StrategyId, placements, plan.AllPlaced);
            }
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
            public SettingChangeResult ApplyBatch(FeatureId feature, uint expectedRevision, System.Collections.Generic.IReadOnlyList<SettingMutation> mutations)
            {
                onApply();
                return new SettingChangeResult(false, FrameworkErrorCode.SettingRejected, 0, GetSnapshot(feature));
            }
            // DEV-V4-02: no schema on this counting fake — the row projection
            // falls back to SettingId names, which is what these wiring tests
            // assert (they never depend on display text).
            public System.Collections.Generic.IReadOnlyList<SettingDescriptor> GetDescriptors(FeatureId feature)
            {
                return new SettingDescriptor[0];
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

        // GPT watermark: DEV-V2-24 F-B1 red regression. A single transient
        // fault inside the preview-update lane (the production repro: a
        // third-party listen-host panel repair racing the drag tick) must be
        // absorbed with a one-shot diagnostic, NOT isolate BII for the whole
        // session. Only consecutive-fault persistence (60-frame debounce,
        // same semantics as the surface-lane TransientIsolationGate) may
        // isolate, and the isolating fault must be visible with its identity.
        private static void AssertPreviewUpdateTransientFaultIsAbsorbedAndVisible()
        {
            var transientEmitted = new List<KeyValuePair<string, ClientUiCompositionRoot.ClientUiDiagnosticLevel>>();
            ClientUiCompositionRoot.DiagnosticSink = (line, level) => transientEmitted.Add(new KeyValuePair<string, ClientUiCompositionRoot.ClientUiDiagnosticLevel>(line, level));
            try
            {
                var component = new BetterItemInteractionUiComponent(
                    new InventoryPreviewPresenter(new InventoryDragPresenter(new ThrowOnceThenFixedCandidateEvaluator())),
                    new NativeInventoryInteractionAdapter(2, 8));
                component.OnUiInitialized(new TestRoot());
                var surface = new TestSurfaceContext(
                    new ContainerReference(ContainerKind.PlayerInventory, 3, 941),
                    new TestVisualContainer(), new TestVisualContainer(),
                    new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                    50f, 1f, 0f, 0f, new EmptyGridForTest(8, 6), true, 100f, 100f);
                component.OnInventoryOpened(surface);
                component.OnDragStarted(941, ItemAssetIdentity.FromItemId(363),
                    new ItemGridPosition(3, 0, 0, 0));
                InventoryPreviewInput input;
                Assert(component.TryCreatePreviewInput(941, new ItemGridPosition(3, 0, 0, 0),
                        100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                        ItemAssetIdentity.FromItemId(363), out input),
                    "FB1 fault gate: a healthy drag builds a preview input");
                component.OnDragUpdated(input);
                Assert(component.LifecycleCanRun,
                    "FB1 fault gate: a single preview-update fault must not isolate the feature");
                Assert(transientEmitted.Exists(entry => entry.Key.Contains("event=preview-update-threw")
                        && entry.Key.Contains("InvalidOperationException")
                        && entry.Key.Contains("stack=")
                        && entry.Key.Contains("diagnosticId=BUE-DRAG-004")
                        && !entry.Key.Contains("BUE-CLIENTUI-001")
                        && entry.Value == ClientUiCompositionRoot.ClientUiDiagnosticLevel.Debug),
                    "FB1 fault gate: a transient preview-update fault emits a one-shot BUE-DRAG-004 Debug diagnostic with the exception identity and no doubled id");
                component.OnDragUpdated(input);
                Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                    "FB1 fault gate: the preview recovers on the next healthy frame");
                Assert(transientEmitted.Exists(entry => entry.Key.Contains("event=preview-update-recovered")
                        && entry.Key.Contains("diagnosticId=BUE-DRAG-004")
                        && !entry.Key.Contains("BUE-CLIENTUI-001")
                        && entry.Value == ClientUiCompositionRoot.ClientUiDiagnosticLevel.Debug),
                    "FB1 fault gate: recovery after absorbed faults emits a one-shot BUE-DRAG-004 Debug recovery line and no doubled id");

                var persistentEmitted = new List<KeyValuePair<string, ClientUiCompositionRoot.ClientUiDiagnosticLevel>>();
                ClientUiCompositionRoot.DiagnosticSink = (line, level) => persistentEmitted.Add(new KeyValuePair<string, ClientUiCompositionRoot.ClientUiDiagnosticLevel>(line, level));
                var persistent = new BetterItemInteractionUiComponent(
                    new InventoryPreviewPresenter(new InventoryDragPresenter(new ThrowingCandidateEvaluator())),
                    new NativeInventoryInteractionAdapter(2, 8));
                persistent.OnUiInitialized(new TestRoot());
                var persistentSurface = new TestSurfaceContext(
                    new ContainerReference(ContainerKind.PlayerInventory, 3, 942),
                    new TestVisualContainer(), new TestVisualContainer(),
                    new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                    50f, 1f, 0f, 0f, new EmptyGridForTest(8, 6), true, 100f, 100f);
                persistent.OnInventoryOpened(persistentSurface);
                persistent.OnDragStarted(942, ItemAssetIdentity.FromItemId(363),
                    new ItemGridPosition(3, 0, 0, 0));
                InventoryPreviewInput persistentInput;
                Assert(persistent.TryCreatePreviewInput(942, new ItemGridPosition(3, 0, 0, 0),
                        100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                        ItemAssetIdentity.FromItemId(363), out persistentInput),
                    "FB1 fault gate: the persistent-fault drag builds a preview input");
                for (var frame = 1; frame <= 59; frame++)
                {
                    persistent.OnDragUpdated(persistentInput);
                }
                var threwLineCount = 0;
                foreach (var emittedEntry in persistentEmitted)
                {
                    if (emittedEntry.Key.Contains("event=preview-update-threw")) threwLineCount++;
                }
                Assert(threwLineCount == 1,
                    "FB1 fault gate: absorbed frames stay silent — the threw line is emitted exactly once per streak");
                Assert(persistent.LifecycleCanRun,
                    "FB1 fault gate: 59 consecutive preview-update faults stay absorbed by the debounce gate");
                persistent.OnDragUpdated(persistentInput);
                Assert(!persistent.LifecycleCanRun,
                    "FB1 fault gate: 60 consecutive preview-update faults isolate the feature");
                Assert(persistentEmitted.Exists(entry => entry.Key.Contains("event=preview-update-isolated")
                        && entry.Key.Contains("InvalidOperationException")
                        && entry.Key.Contains("stack=")
                        && entry.Key.Contains("diagnosticId=BUE-DRAG-004")
                        && !entry.Key.Contains("BUE-CLIENTUI-001")
                        && entry.Value == ClientUiCompositionRoot.ClientUiDiagnosticLevel.Error),
                    "FB1 fault gate: the isolating preview-update fault emits a BUE-DRAG-004 Error diagnostic with the exception identity and no doubled id");

                // Streak scoping: a fresh drag generation resets the absorbed
                // streak instead of resuming it toward the threshold — 59
                // absorbed frames, then a new drag, then ONE fault must stay
                // absorbed (without the reset that single fault would be the
                // 60th consecutive frame and would isolate).
                var resumed = new BetterItemInteractionUiComponent(
                    new InventoryPreviewPresenter(new InventoryDragPresenter(new ThrowingCandidateEvaluator())),
                    new NativeInventoryInteractionAdapter(2, 8));
                resumed.OnUiInitialized(new TestRoot());
                var resumedSurface = new TestSurfaceContext(
                    new ContainerReference(ContainerKind.PlayerInventory, 3, 945),
                    new TestVisualContainer(), new TestVisualContainer(),
                    new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                    50f, 1f, 0f, 0f, new EmptyGridForTest(8, 6), true, 100f, 100f);
                resumed.OnInventoryOpened(resumedSurface);
                resumed.OnDragStarted(945, ItemAssetIdentity.FromItemId(363),
                    new ItemGridPosition(3, 0, 0, 0));
                InventoryPreviewInput resumedInput;
                Assert(resumed.TryCreatePreviewInput(945, new ItemGridPosition(3, 0, 0, 0),
                        100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                        ItemAssetIdentity.FromItemId(363), out resumedInput),
                    "FB1 fault gate: the streak-scope drag builds a preview input");
                for (var frame = 0; frame < 59; frame++)
                {
                    resumed.OnDragUpdated(resumedInput);
                }
                Assert(resumed.LifecycleCanRun,
                    "FB1 fault gate: 59 consecutive faults stay absorbed by the debounce gate");
                resumed.OnDragStarted(946, ItemAssetIdentity.FromItemId(363),
                    new ItemGridPosition(3, 0, 0, 0));
                InventoryPreviewInput resumedInput2;
                Assert(resumed.TryCreatePreviewInput(946, new ItemGridPosition(3, 0, 0, 0),
                        100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                        ItemAssetIdentity.FromItemId(363), out resumedInput2),
                    "FB1 fault gate: the reset-scope drag builds its own preview input");
                resumed.OnDragUpdated(resumedInput2);
                Assert(resumed.LifecycleCanRun,
                    "FB1 fault gate: a new drag generation resets the streak — the first fault of the next drag is not the 60th consecutive frame");
            }
            finally
            {
                ClientUiCompositionRoot.DiagnosticSink = null;
            }
        }

        // GPT watermark: DEV-V2-24 F-B1 red regression. The listen-host panel
        // repair (SPF-style reconcile) clears the native items panel children
        // — including the mounted preview frame — behind BUE's back. The next
        // drag start must re-assert the sink children so one rebuild cannot
        // leave the preview lane invisibly dead for the session.
        private static void AssertSinkRemountsAfterThirdPartyPanelClear()
        {
            var gridPanel = new RecordingVisualContainer();
            var topLevel = new RecordingVisualContainer();
            var component = new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
            component.OnUiInitialized(new TestRoot());
            var surface = new TestSurfaceContext(
                new ContainerReference(ContainerKind.PlayerInventory, 3, 943),
                topLevel, gridPanel,
                new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                50f, 1f, 0f, 0f, new EmptyGridForTest(8, 6), true, 100f, 100f);
            component.OnInventoryOpened(surface);
            component.OnDragStarted(943, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));
            Assert(gridPanel.Children.Count == 1 && topLevel.Children.Count == 1,
                "FB1 sink remount: the preview frame and icon mount into their containers");
            gridPanel.SimulateThirdPartyClear();
            topLevel.SimulateThirdPartyClear();
            component.OnDragStarted(944, ItemAssetIdentity.FromItemId(363),
                new ItemGridPosition(3, 0, 0, 0));
            Assert(gridPanel.Children.Count == 1 && topLevel.Children.Count == 1,
                "FB1 sink remount: the next drag start re-asserts the sink children after a third-party clear");
            InventoryPreviewInput input;
            Assert(component.TryCreatePreviewInput(944, new ItemGridPosition(3, 0, 0, 0),
                    100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                    ItemAssetIdentity.FromItemId(363), out input),
                "FB1 sink remount: a preview input still builds after the third-party clear");
            component.OnDragUpdated(input);
            Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                "FB1 sink remount: the preview stays healthy across the rebuild boundary");
            component.OnInventoryClosed();
        }

        // GPT watermark: DEV-V2-24 F-B1b red regression. Glazier pools its box
        // elements and nulls the uGUI Image component on pool release, so a
        // native pool-release race behind BUE's back turns every preview
        // frame write into an NRE (machine 20260909_001648: stack=
        // GlazierBox_uGUI.set_BackgroundColor, 60 consecutive frames). The
        // sink must rebuild its elements fresh and retry once instead of
        // leaving the preview lane dead.
        // F-B1c: vanilla listen-host UI projection goes stale after BUE's
        // out-of-band tidy moves (and join-time churn) — the feature that
        // invalidated the projection reconciles it. Red test freezes: the
        // reference-exact decision core, the test-safe dispatcher routing
        // (hook first, engine dispatcher bound in-game only), the pure
        // repair orchestration over the page-view seam, the diagnostic
        // line identity, and the end-to-end tidy-publish wiring through
        // the multiplayer harness. Engine (SDG-touching) paths stay
        // NoInlining and are never JIT'd on the host test path.
        private sealed class FakeProjectionPage : IInventoryProjectionPageView
        {
            internal List<object> Authoritative = new List<object>();
            internal List<object> Rendered = new List<object>();
            internal List<object> Pending = new List<object>();
            internal int RepairCount;
            internal byte PageValue;

            public byte Page { get { return PageValue; } }
            public int AuthoritativeCount { get { return Authoritative.Count; } }
            public object AuthoritativeAt(int index) { return Authoritative[index]; }
            public int RenderedCount { get { return Rendered.Count; } }
            public object RenderedJarAt(int index) { return Rendered[index]; }
            public int PendingCount { get { return Pending.Count; } }
            public object PendingJarAt(int index) { return Pending[index]; }

            public void RepairFromAuthoritative()
            {
                RepairCount++;
                // Mirror what the vanilla repair does: projection becomes the
                // authoritative multiset, pending queue drains.
                Rendered = new List<object>(Authoritative);
                Pending.Clear();
            }
        }

        private static void AssertBueV2Fb1cProjectionReconciler()
        {
            // 组 1：决策核 — reference-exact match matrix（SPF ProjectionIsExact 同语义）。
            var jarA = new object();
            var jarB = new object();
            var jarC = new object();
            Assert(ProjectionReconcileDecision.IsExact(new[] { jarA, jarB }, new[] { jarB }, new[] { jarA }),
                "F-B1c: rendered+pending must match authoritative as an order-insensitive reference multiset");
            Assert(ProjectionReconcileDecision.IsExact(new object[0], new object[0], new object[0]),
                "F-B1c: an empty page is exact (no repair needed)");
            Assert(!ProjectionReconcileDecision.IsExact(new[] { jarA }, new[] { jarA, jarB }, new object[0]),
                "F-B1c: a stale rendered element (rendered extra) is inexact");
            Assert(!ProjectionReconcileDecision.IsExact(new[] { jarA }, new object[0], new[] { jarA, jarB }),
                "F-B1c: a stale pending element (pending extra) is inexact");
            Assert(!ProjectionReconcileDecision.IsExact(new[] { jarA, jarB, jarC }, new[] { jarA }, new[] { jarB }),
                "F-B1c: a missing projection element (authoritative extra) is inexact");
            Assert(!ProjectionReconcileDecision.IsExact(new[] { jarA }, new[] { jarB }, new object[0]),
                "F-B1c: reference identity matters — two distinct jars are never equal");
            Assert(!ProjectionReconcileDecision.IsExact(null, new object[0], new object[0]),
                "F-B1c: null authoritative fails closed to inexact");

            // 组 2：分派路由 — hook 先行，hook 未绑时分派器为静默 no-op
            // （宿主测试路径永不 JIT 引擎方法）。
            byte routedFirst = 0;
            byte routedLast = 0;
            var routedCount = 0;
            ListenHostProjectionReconciler.ReconcileHook = (first, last) =>
            {
                routedFirst = first;
                routedLast = last;
                routedCount++;
            };
            try
            {
                ListenHostProjectionReconciler.OnTidyPagesCommitted(3, 3);
                Assert(routedCount == 1 && routedFirst == 3 && routedLast == 3,
                    "F-B1c: the tidy-commit dispatcher routes the committed page range to the reconcile hook");
                ListenHostProjectionReconciler.OnDashboardSurfaceOpened();
                Assert(routedCount == 2 && routedFirst == HotkeySnapshotUtil.TIDYABLE_PAGE_MIN && routedLast == HotkeySnapshotUtil.TIDYABLE_PAGE_MAX,
                    "F-B1c: the dashboard-open dispatcher reconciles the whole tidyable page range");
            }
            finally
            {
                ListenHostProjectionReconciler.ReconcileHook = null;
            }
            ListenHostProjectionReconciler.OnTidyPagesCommitted(3, 3);
            Assert(routedCount == 2,
                "F-B1c: with no hook and no engine dispatcher the dispatchers are a silent no-op (test-safe)");

            // 组 3：纯修复编排 — page-view seam 上的精确跳过/陈旧重建/fail-closed。
            var requestedPages = new List<byte>();
            Func<byte, IInventoryProjectionPageView> factory = page =>
            {
                requestedPages.Add(page);
                if (page == 3)
                {
                    var stale = new FakeProjectionPage { PageValue = page };
                    stale.Authoritative.Add(jarA);
                    stale.Rendered.Add(jarA);
                    stale.Rendered.Add(jarB); // 陈旧元素：权威里没有
                    return stale;
                }
                var exact = new FakeProjectionPage { PageValue = page };
                exact.Authoritative.Add(jarA);
                exact.Rendered.Add(jarA);
                return exact;
            };
            var repaired = ListenHostProjectionReconciler.ReconcileRange(2, 4, factory);
            Assert(requestedPages.Count == 3 && requestedPages[0] == 2 && requestedPages[1] == 3 && requestedPages[2] == 4,
                "F-B1c: the orchestrator consults the factory for every page in the inclusive range");
            Assert(repaired == 1,
                "F-B1c: exactly the inexact page is repaired");
            Assert(!requestedPages.Contains(255),
                "F-B1c: no page outside the requested range is consulted");

            Func<byte, IInventoryProjectionPageView> nullFactory = page => null;
            Assert(ListenHostProjectionReconciler.ReconcileRange(2, 6, nullFactory) == 0,
                "F-B1c: a factory that declines every page (engine gate closed) repairs nothing and never throws");

            var orderPage = new FakeProjectionPage { PageValue = 2 };
            orderPage.Authoritative.Add(jarB);
            orderPage.Authoritative.Add(jarA);
            orderPage.Rendered.Add(jarA);
            Assert(ListenHostProjectionReconciler.ReconcileRange(2, 2, p => orderPage) == 1,
                "F-B1c: a stale page repairs exactly once");
            Assert(orderPage.RepairCount == 1,
                "F-B1c: the repair command is issued once per inexact page");

            // 组 3b：TIDYABLE 全域 —— 开包路径恰好咨询 2..6 每页一次。
            var fullRangePages = new List<byte>();
            Func<byte, IInventoryProjectionPageView> rangeFactory = page => { fullRangePages.Add(page); return null; };
            ListenHostProjectionReconciler.ReconcileRange(HotkeySnapshotUtil.TIDYABLE_PAGE_MIN, HotkeySnapshotUtil.TIDYABLE_PAGE_MAX, rangeFactory);
            Assert(fullRangePages.Count == 5 && fullRangePages[0] == 2 && fullRangePages[4] == 6,
                "F-B1c: the open-trigger range consults exactly the five dashboard pages 2..6");

            // 组 4：诊断行 — 修复发生时一条 Debug 行，携带 BUE-LIT-001 身份与计数。
            var previousRecorder = BueRuntimeLog.Recorder;
            var captured = new List<string>();
            try
            {
                BueRuntimeLog.Recorder = line => captured.Add(line);
                var stalePage = new FakeProjectionPage { PageValue = 3 };
                stalePage.Authoritative.Add(jarA);
                stalePage.Rendered.Add(jarA);
                stalePage.Rendered.Add(jarB);
                ListenHostProjectionReconciler.ReconcileRange(3, 3, p => stalePage);
                Assert(captured.Exists(line => line.Contains("listen-host 投影对账修复")
                        && line.Contains("page=3")
                        && line.Contains("authoritative=1")
                        && line.Contains("renderedBefore=2")
                        && line.Contains("diagnosticId=BUE-LIT-001")),
                    "F-B1c: a repair emits the BUE-LIT-001 reconcile line with page identity and counts");

                captured.Clear();
                var exactPage = new FakeProjectionPage { PageValue = 3 };
                exactPage.Authoritative.Add(jarA);
                exactPage.Rendered.Add(jarA);
                ListenHostProjectionReconciler.ReconcileRange(3, 3, p => exactPage);
                Assert(captured.Count == 0,
                    "F-B1c: an exact page emits no diagnostic (silent exact-match skip)");
            }
            finally
            {
                BueRuntimeLog.Recorder = previousRecorder;
            }

            // 组 5：端到端接线 — 整理提交发布 TidyCompleted 的同一拍路由对账。
            var faultDir = NewLitFaultDirectory();
            var harness = LitMultiplayerHarness.Create(faultDir);
            harness.Handshake();
            harness.EstablishChallenge();
            var reconcilePages = new List<KeyValuePair<byte, byte>>();
            ListenHostProjectionReconciler.ReconcileHook = (first, last) => reconcilePages.Add(new KeyValuePair<byte, byte>(first, last));
            try
            {
                var request = harness.ClientModule.RequestTidy(3, TidyMode.SameType, true);
                Assert(request == LitTidyRequestResult.Dispatched,
                    "F-B1c wiring: the client tidy request is dispatched");
                harness.Pump();
                harness.ServerModule.Tick();
                harness.Pump();
                harness.ClientModule.Tick();
                harness.Pump();
                harness.ServerModule.Tick();
                harness.Pump();
                Assert(harness.ServerAuthority.ExecuteCount == 1,
                    "F-B1c wiring: the authority executes the tidy exactly once");
                Assert(harness.ServerTidyEvents.Count == 1,
                    "F-B1c wiring: the reconcile wiring does not alter TidyCompleted publication semantics");
                Assert(reconcilePages.Count == 1 && reconcilePages[0].Key == 3 && reconcilePages[0].Value == 3,
                    "F-B1c wiring: the tidy commit routes the committed page to the reconcile hook on the same beat");
            }
            finally
            {
                ListenHostProjectionReconciler.ReconcileHook = null;
            }
        }

        // F-D: on the U3DS headless boot the game destroyed the plugin host
        // component between Awake and the first Start/Update frame; the old
        // OnDestroy cleared the runtime host and the completion barrier never
        // fired again — AnnounceReady/module starts/network arm never ran.
        // The fix staticizes the completion drive (static scene-loaded core +
        // preserved runtime host on a non-quit sweep) and adds a DDOL
        // headless survival pump driving the shared per-frame tick chain
        // (mirror/network/clock/tidy/completion, frame-deduped). Red test
        // freezes: the scene drive completes an open runtime after host
        // loss, the drive is idempotent and reaches the module-start path,
        // the tick chain dedupes drivers within one frame and stays
        // monotonic, and quit teardown still fully detaches the chain.
        // F-E: on a FakeIP direct-connect (U3DS without a GSLT token) the
        // client engine leaves Provider.server at zero while the server's own
        // identity is a self-assigned non-steam64 value — the initiator never
        // arms (ClientPeer==0) so no BUE session can form. The fix converges
        // both sides onto a shared placeholder whenever the engine identity
        // is not a plausible steam64 individual account; plausible ids
        // (listen host, GSLT server) pass through unchanged. Pure truth
        // tables — the session bookkeeping key never rides the wire.
        private static void AssertBueV2FeEnginePeerIdentity()
        {
            // 组 1：SteamIdPlausible 段校验。
            Assert(BueEngineNet.SteamIdPlausible(76561199030780228UL),
                "F-E: a real steam64 individual account id is plausible");
            Assert(!BueEngineNet.SteamIdPlausible(0UL),
                "F-E: zero is never a plausible engine identity");
            Assert(!BueEngineNet.SteamIdPlausible(90292445196063768UL),
                "F-E: the FakeIP self-assigned id (U3DS no-GSLT) is outside the steam64 account segment");
            Assert(!BueEngineNet.SteamIdPlausible(76561197960265727UL),
                "F-E: an id below the steam64 base is not plausible");
            Assert(BueEngineNet.SteamIdPlausible(76561197960265728UL),
                "F-E: the steam64 base itself is plausible (half-open interval lower bound)");
            Assert(BueEngineNet.SteamIdPlausible(76561197960265728UL + 4294967295UL),
                "F-E: base+2^32-1 (the last account number) is plausible");
            Assert(!BueEngineNet.SteamIdPlausible(76561197960265728UL + 4294967296UL),
                "F-E: base+2^32 falls outside the individual account segment");
            Assert(!BueEngineNet.SteamIdPlausible(BueEngineNet.PlaceholderServerPeerId),
                "F-E: the placeholder itself lies outside the steam64 account segment (collision-free bookkeeping key)");

            // 组 2：决策真值表 —— 直连 FakeIP/0/listen host/菜单 四景。
            Assert(BueEngineNet.ClientPeerDecision(false, false, 76561199030780228UL) == 0UL,
                "F-E: the menu state (not connected) never yields a peer");
            Assert(BueEngineNet.ClientPeerDecision(true, true, 76561199030780228UL) == 0UL,
                "F-E: the server role never yields a client peer");
            Assert(BueEngineNet.ClientPeerDecision(true, false, 76561199030780228UL) == 76561199030780228UL,
                "F-E: a plausible server id passes through unchanged (listen host / GSLT)");
            Assert(BueEngineNet.ClientPeerDecision(true, false, 0UL) == BueEngineNet.PlaceholderServerPeerId,
                "F-E: the FakeIP direct-connect client (Provider.server zero) converges on the placeholder peer");
            Assert(BueEngineNet.ClientPeerDecision(true, false, 90292445196063768UL) == BueEngineNet.PlaceholderServerPeerId,
                "F-E: an implausible non-zero server id also converges on the placeholder peer");
            Assert(BueEngineNet.LocalSteamIdDecision(true, 76561199030780228UL) == 76561199030780228UL,
                "F-E: the listen host keeps its real identity");
            Assert(BueEngineNet.LocalSteamIdDecision(true, 90292445196063768UL) == BueEngineNet.PlaceholderServerPeerId,
                "F-E: the U3DS FakeIP self-id converges on the placeholder so both sides pair");
            Assert(BueEngineNet.LocalSteamIdDecision(true, 0UL) == BueEngineNet.PlaceholderServerPeerId,
                "F-E: an unresolvable server identity arms with the placeholder instead of fail-closing");
            Assert(BueEngineNet.LocalSteamIdDecision(false, 76561199721762479UL) == 76561199721762479UL,
                "F-E: the client's own id passes through unchanged");
            Assert(BueEngineNet.LocalSteamIdDecision(false, 0UL) == 0UL,
                "F-E: an unresolvable client identity still fail-closes to zero");
        }

        private static void AssertBueV2FdHeadlessCompletionSurvival()
        {
            // 组 1：场景驱动完成链 —— 宿主死亡后仍可完成注册。
            BueRuntimeCompletionChain.ResetForTests();
            BueRuntimeLog.ResetReadyAnnouncement();
            var previousRecorder = BueRuntimeLog.Recorder;
            var captured = new List<string>();
            BueRuntimeLog.Recorder = line => captured.Add(line);
            try
            {
                BueRuntimeHost.Clear();
                var runtime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(runtime);
                runtime.OpenRegistration();
                BueRuntimeCompletionChain.HeadlessDecision = true;
                var healed = 0;
                BueRuntimeCompletionChain.HeadlessPumpHealer = () => healed++;

                BueRuntimeCompletionChain.OnSceneLoadedCore();
                Assert(BueRuntimeHost.CurrentRuntime != null
                        && BueRuntimeHost.CurrentRuntime.Phase == FeatureRegistrationPhase.RuntimeReady,
                    "F-D: the scene-loaded drive completes the open runtime even after the plugin host is gone");
                Assert(captured.Exists(line => line.Contains("加载成功")),
                    "F-D: completion announces ready through the survival chain");
                Assert(healed == 1,
                    "F-D: the scene drive invokes the headless pump healer before completing");
                Assert(captured.Exists(line => line.Contains("event=module-start result=skipped reason=feature-network-unavailable")),
                    "F-D: completion reaches the module-start path (skipped without a wired network adapter)");

                BueRuntimeCompletionChain.OnSceneLoadedCore();
                Assert(healed == 2,
                    "F-D: repeated drives keep healing the headless pump");
                Assert(captured.FindAll(line => line.Contains("加载成功")).Count == 1,
                    "F-D: the ready announcement fires exactly once across repeated drives");

                // healer 抛异常不破坏驱动（never-throw 契约 + 失败留痕）。
                BueRuntimeCompletionChain.HeadlessPumpHealer = () => { throw new InvalidOperationException("healer boom"); };
                BueRuntimeCompletionChain.OnSceneLoadedCore();
                Assert(BueRuntimeHost.CurrentRuntime != null
                        && BueRuntimeHost.CurrentRuntime.Phase == FeatureRegistrationPhase.RuntimeReady,
                    "F-D: a throwing healer never breaks the scene drive");
                Assert(captured.Exists(line => line.Contains("event=headless-pump-heal-failed")),
                    "F-D: a healer failure is diagnosed, not swallowed");
            }
            finally
            {
                BueRuntimeLog.Recorder = previousRecorder;
                BueRuntimeCompletionChain.ResetForTests();
                BueRuntimeHost.Clear();
            }

            // 组 2：共享 tick 链 —— 同帧去重、跨帧单调、时钟接续。
            BetterUnturnedExperience.Plugin.BueHostEventRuntime.Clear();
            Assert(BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated(),
                "F-D: the host event runtime is created for the tick chain");
            var chainFeature = new FeatureId("io.github.yu80rice.bue.test.fdchain");
            var ticks = new List<HostTick>();
            BetterUnturnedExperience.Plugin.BueHostEventRuntime.Bus.Subscriber(chainFeature).Subscribe<HostTick>(ticks.Add);
            var frameCounter = 100;
            BueRuntimeTickChain.FrameProvider = () => frameCounter;
            try
            {
                BueRuntimeTickChain.Tick();
                BueRuntimeTickChain.Tick();
                Assert(ticks.Count == 1,
                    "F-D: the tick chain dedupes multiple drivers within one frame");
                frameCounter = 101;
                BueRuntimeTickChain.Tick();
                Assert(ticks.Count == 2,
                    "F-D: the next frame ticks exactly once through the chain");
                Assert(ticks[0].TickNumber == 1 && ticks[1].TickNumber == 2,
                    "F-D: host tick numbering stays strictly monotonic through the chain");
                Assert(ticks.TrueForAll(t => t.Phase == TickPhase.Update),
                    "F-D: chain-driven ticks carry the frozen Update phase");
            }
            finally
            {
                BueRuntimeTickChain.FrameProvider = null;
                BetterUnturnedExperience.Plugin.BueHostEventRuntime.Clear();
            }

            // 组 3：退出 teardown —— 摘干净链条（与扫毁保留语义相对）。
            BueRuntimeCompletionChain.ResetForTests();
            BueRuntimeLog.ResetReadyAnnouncement();
            var teardownRecorder = BueRuntimeLog.Recorder;
            var teardownCaptured = new List<string>();
            BueRuntimeLog.Recorder = line => teardownCaptured.Add(line);
            try
            {
                BueRuntimeHost.Clear();
                var runtime2 = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(runtime2);
                runtime2.OpenRegistration();
                var unsubscribed = 0;
                BueRuntimeCompletionChain.SceneLoadedUnsubscriber = () => unsubscribed++;

                BueRuntimeCompletionChain.TeardownForQuit();
                Assert(unsubscribed == 1,
                    "F-D: quit teardown unsubscribes the static scene drive");
                Assert(BueRuntimeHost.CurrentRuntime == null,
                    "F-D: quit teardown clears the runtime host");
                teardownCaptured.Clear();
                BueRuntimeCompletionChain.OnSceneLoadedCore();
                Assert(!teardownCaptured.Exists(line => line.Contains("加载成功")),
                    "F-D: after quit teardown a scene drive neither completes nor announces");
            }
            finally
            {
                BueRuntimeLog.Recorder = teardownRecorder;
                BueRuntimeCompletionChain.ResetForTests();
                BueRuntimeHost.Clear();
            }

            // 组 4：纯门决策真值表 —— listen-host 资格与仪表盘页域（无 SDG）。
            // 生产端 IsEligibleLocalHostEngine/BuildEnginePageView 用引擎状态
            // 调用这两个纯函数；真值表在此钉死。
            Assert(BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsEligibleLocalHostDecision(true, true, true),
                "F-D gate: listen host (server+client+local player) is eligible");
            Assert(!BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsEligibleLocalHostDecision(true, false, true),
                "F-D gate: a dedicated server (U3DS) is not eligible");
            Assert(!BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsEligibleLocalHostDecision(false, true, true),
                "F-D gate: a pure remote client is not eligible");
            Assert(!BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsEligibleLocalHostDecision(true, true, false),
                "F-D gate: a missing local player is not eligible");
            Assert(BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsReconcilablePage(2)
                    && BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsReconcilablePage(6),
                "F-D gate: the tidyable dashboard bounds 2..6 are reconcilable");
            Assert(!BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsReconcilablePage(1)
                    && !BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsReconcilablePage(7)
                    && !BetterUnturnedExperience.ClientUi.Internal.ListenHostProjectionReconciler.IsReconcilablePage(255),
                "F-D gate: pages outside the dashboard range are rejected");

            // 组 5：共享链 Reset —— 哨兵归位：同一帧号在 Reset 后可再次执行
            // （不 Reset 则帧去重会跳过它，这是判别性差异）。
            BueRuntimeTickChain.FrameProvider = () => 5;
            BueRuntimeTickChain.Tick();
            BueRuntimeTickChain.Tick();
            BueRuntimeTickChain.Reset();
            BueRuntimeTickChain.FrameProvider = () => 5;
            var ticksAfterReset = new List<HostTick>();
            BetterUnturnedExperience.Plugin.BueHostEventRuntime.Clear();
            Assert(BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated(), "F-D: host event runtime recreated for the reset group");
            BetterUnturnedExperience.Plugin.BueHostEventRuntime.Bus.Subscriber(new FeatureId("io.github.yu80rice.bue.test.fdreset")).Subscribe<HostTick>(ticksAfterReset.Add);
            try
            {
                BueRuntimeTickChain.Tick();
                Assert(ticksAfterReset.Count == 1,
                    "F-D: after chain reset the same frame number ticks again (sentinel cleared)");
            }
            finally
            {
                BueRuntimeTickChain.Reset();
                BetterUnturnedExperience.Plugin.BueHostEventRuntime.Clear();
            }
        }

        private static void AssertSinkRebuildsElementsOnNativeWriteFault()
        {
            var emitted = new List<KeyValuePair<string, ClientUiCompositionRoot.ClientUiDiagnosticLevel>>();
            ClientUiCompositionRoot.DiagnosticSink = (line, level) => emitted.Add(new KeyValuePair<string, ClientUiCompositionRoot.ClientUiDiagnosticLevel>(line, level));
            try
            {
                var gridPanel = new RecordingVisualContainer();
                var topLevel = new RecordingVisualContainer();
                var component = new BetterItemInteractionUiComponent(
                    new InventoryPreviewPresenter(new InventoryDragPresenter(new FixedCandidateEvaluator())),
                    new NativeInventoryInteractionAdapter(2, 8));
                component.OnUiInitialized(new TestRoot());
                var surface = new TestSurfaceContext(
                    new ContainerReference(ContainerKind.PlayerInventory, 3, 946),
                    topLevel, gridPanel,
                    new InventoryGridViewport(0f, 0f, 8, 6, 0f, 0f, 400f, 300f),
                    50f, 1f, 0f, 0f, new EmptyGridForTest(8, 6), true, 100f, 100f);
                component.OnInventoryOpened(surface);
                component.OnDragStarted(946, ItemAssetIdentity.FromItemId(363),
                    new ItemGridPosition(3, 0, 0, 0));
                Assert(gridPanel.Children.Count == 1,
                    "FB1b sink rebuild: the preview frame mounts into the grid panel");
                var poisonedElement = gridPanel.LastCreatedElement;
                gridPanel.PoisonLastCreatedElement();
                InventoryPreviewInput input;
                Assert(component.TryCreatePreviewInput(946, new ItemGridPosition(3, 0, 0, 0),
                        100f, 100f, 1, 1, 0, false, 0.5f, 0.5f,
                        ItemAssetIdentity.FromItemId(363), out input),
                    "FB1b sink rebuild: the drag builds a preview input");
                component.OnDragUpdated(input);
                Assert(component.LifecycleCanRun,
                    "FB1b sink rebuild: a poisoned native element write must not isolate the feature");
                Assert(gridPanel.Children.Count == 1 && !gridPanel.Children.Contains(poisonedElement),
                    "FB1b sink rebuild: the poisoned element was replaced by a fresh mount");
                var rebuiltFrame = (TestVisualElement)gridPanel.Children[0];
                Assert(!rebuiltFrame.Poisoned && rebuiltFrame.PositionOffsetX == 50f,
                    "FB1b sink rebuild: the rebuilt frame element received the frame write");
                Assert(component.LastPreview.State == PlacementPreviewState.Candidate,
                    "FB1b sink rebuild: the preview stays Candidate across the native write fault");
                var gateLines = 0;
                foreach (var emittedEntry in emitted)
                {
                    if (emittedEntry.Key.Contains("preview-update-threw")) gateLines++;
                }
                Assert(gateLines == 0,
                    "FB1b sink rebuild: a healed native write fault never reaches the preview fault gate");
                var poisonedIcon = (TestVisualElement)topLevel.Children[0];
                poisonedIcon.Poisoned = true;
                component.OnDragUpdated(input);
                Assert(topLevel.Children.Count == 1 && !topLevel.Children.Contains(poisonedIcon),
                    "FB1b sink rebuild: a poisoned icon element is rebuilt through the same contract");
                Assert(component.LifecycleCanRun,
                    "FB1b sink rebuild: the icon rebuild keeps the feature alive");

                // Unhealable scenario: poison the CURRENT elements so the very
                // next write throws, and poison all newly created ones so the
                // rebuild retry cannot heal — 60 consecutive frames must walk
                // the preview fault gate into isolation.
                ((TestVisualElement)gridPanel.Children[0]).Poisoned = true;
                ((TestVisualElement)topLevel.Children[0]).Poisoned = true;
                gridPanel.PoisonAllNewElements = true;
                topLevel.PoisonAllNewElements = true;
                for (var frame = 1; frame <= 60; frame++)
                {
                    component.OnDragUpdated(input);
                }
                Assert(!component.LifecycleCanRun,
                    "FB1b sink rebuild: a persistently poisoned container isolates through the preview fault gate");
                Assert(emitted.Exists(entry => entry.Key.Contains("event=preview-update-isolated")
                        && entry.Key.Contains("InvalidOperationException")
                        && entry.Key.Contains("diagnosticId=BUE-DRAG-004")),
                    "FB1b sink rebuild: the unhealable fault surfaces through the gate with its identity");
                component.OnInventoryClosed();
            }
            finally
            {
                ClientUiCompositionRoot.DiagnosticSink = null;
            }
        }

        private sealed class ThrowOnceThenFixedCandidateEvaluator : IPlacementCandidateEvaluator
        {
            private bool threw;
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                if (!threw)
                {
                    threw = true;
                    throw new InvalidOperationException("FB1 simulated transient preview fault");
                }
                return new ItemPlacementPreview(input.DragGeneration, PlacementPreviewState.Candidate,
                    new ItemGridPosition(input.TargetContainer.Page, 1, 1, input.CurrentRotation),
                    input.ItemWidth, input.ItemHeight, PlacementReason.None);
            }
        }

        private sealed class ThrowingCandidateEvaluator : IPlacementCandidateEvaluator
        {
            public ItemPlacementPreview Evaluate(PlacementCandidateInput input)
            {
                throw new InvalidOperationException("FB1 simulated persistent preview fault");
            }
        }

        private sealed class RecordingVisualContainer : IVisualContainer
        {
            internal readonly List<IVisualElement> Children = new List<IVisualElement>();
            internal IVisualElement LastCreatedElement;
            internal bool PoisonAllNewElements;

            public IVisualElement CreateBox() { LastCreatedElement = NewElement(); return LastCreatedElement; }
            public IVisualElement CreateImage() { LastCreatedElement = NewElement(); return LastCreatedElement; }
            private TestVisualElement NewElement()
            {
                var element = new TestVisualElement();
                if (PoisonAllNewElements) element.Poisoned = true;
                return element;
            }
            internal void PoisonLastCreatedElement() { ((TestVisualElement)LastCreatedElement).Poisoned = true; }
            public void AddChild(IVisualElement child)
            {
                if (!Children.Contains(child)) Children.Add(child);
            }
            public void RemoveChild(IVisualElement child) { Children.Remove(child); }
            internal void SimulateThirdPartyClear() { Children.Clear(); }
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
            // FB1b: simulates a Glazier pooled element whose uGUI component
            // was released natively — property writes throw.
            public bool Poisoned;
            public float PositionScaleX { get; set; }
            public float PositionScaleY { get; set; }
            public float PositionOffsetX
            {
                get { return positionOffsetX; }
                set
                {
                    if (Poisoned) throw new InvalidOperationException("FB1b simulated native pool-release NRE");
                    positionOffsetX = value;
                }
            }
            private float positionOffsetX;
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

        // DEV-V3-01: the official identity whitelist on the REAL public bridge
        // (every shipped official FeatureId registers, a rogue reserved-segment
        // identity is rejected), plus the bootstrap availability matrix
        // observed on the REAL host start composition (StartCatalog). The
        // matrix is the living truth per ticket: the five frozen members are
        // never null; DEV-V3-03 wired Lifetime/Dependencies (now non-null);
        // DEV-V3-06 wired Settings (facet=non-null, no-facet=honest null);
        // DEV-V3-07 wired Logger (non-null for every started feature) — the
        // spec's availability matrix is the only truth.
        private static void AssertBueV3RegistrationGateAndBootstrapMatrix()
        {
            var previousRuntime = BueRuntimeHost.CurrentRuntime;
            try
            {
                var hostRuntime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(hostRuntime);
                hostRuntime.OpenRegistration();
                Assert(BetterItemInteractionFeatureRegistration.Register().Accepted,
                    "DEV-V3-01: official BII registers through the public bridge (whitelist positive)");
                Assert(BueRuntimeHost.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                    "DEV-V3-01: official LIT registers through the public bridge (whitelist positive)");
                Assert(BueRuntimeHost.Register(InPlaceReloadFeatureRegistration.CreateRegistration()).Accepted,
                    "DEV-V3-01: official LIR registers through the public bridge (whitelist positive)");
                Assert(BueRuntimeHost.Register(HordeTrackerFeatureRegistration.CreateRegistration()).Accepted,
                    "DEV-V3-01: official LHT registers through the public bridge (whitelist positive)");
                var officialNetwork = NetworkModuleFeatureRegistration.CreateOfficialRegistrations();
                for (var index = 0; index < officialNetwork.Length; index++)
                {
                    Assert(BueRuntimeHost.Register(officialNetwork[index]).Accepted,
                        "DEV-V3-01: the official network pair registers through the public bridge (whitelist positive)");
                }
                var rogue = BueRuntimeHost.Register(new MatrixProbeRegistration("io.github.yu80rice.bue.rogue"));
                Assert(!rogue.Accepted && rogue.DiagnosticId == "BUE-REG-010",
                    "DEV-V3-01: the public bridge rejects a rogue reserved-segment identity with BUE-REG-010");

                var probe = new MatrixProbeRegistration("io.example.matrix-probe");
                var probeRuntime = new FeatureRegistrationRuntime();
                probeRuntime.OpenRegistration();
                Assert(probeRuntime.Register(probe).Accepted, "setup: the matrix probe registers on a fresh runtime");
                Assert(probeRuntime.CompleteRuntime(), "setup: the probe catalog completes");
                var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 1001UL);
                BueFeatureStartRuntime.StartCatalog(probeRuntime, network);
                Assert(probe.Module.Bootstrap != null, "DEV-V3-01: the probe module started on the real host composition");
                var captured = probe.Module.Bootstrap;
                Assert(captured.Events != null && captured.OwnedEvents != null && captured.Network != null,
                    "DEV-V3-01: Events/OwnedEvents/Network are never null on the start composition");
                Assert(captured.LifecycleGeneration != 0UL,
                    "DEV-V3-01: LifecycleGeneration is a real host-allocated generation");
                Assert(captured.Identity.Id.Value == "io.example.matrix-probe",
                    "DEV-V3-01: Identity binds the feature's own registration identity (not the default record)");
                Assert(captured.Identity.DefinitionSetId == "bue-v3-matrix-probe",
                    "DEV-V3-01: Identity carries the definition set the registration was admitted with");
                Assert(captured.Settings == null && captured.Logger != null,
                    "DEV-V3-01/03/06/07: this probe registers NO settings facet, so Settings stays the honest null (the 06 row is wired: facet=composed view, no-facet=not provided, nothing faked); Logger turned non-null — the 07 row is wired for every started feature (availability matrix: 接线后永非 null on the start composition)");
                Assert(captured.Lifetime != null && captured.Dependencies != null,
                    "DEV-V3-03: Lifetime/Dependencies turned non-null on the start composition (availability matrix rows wired by DEV-V3-03)");
                Assert(captured.MainThread != null,
                    "DEV-V3-04: MainThread turned non-null on the start composition (availability matrix row wired by DEV-V3-04)");
            }
            finally
            {
                BueRuntimeHost.Bind(previousRuntime);
            }
        }

        // DEV-V3-02: the event-type ownership routing. The bus routes by the
        // registered (EventId, EventType, owner) triple with ONE owning
        // EventId per payload type; the official types (TidyCompleted → LIT,
        // HostTick → the reserved host identity) are host-registered at bus
        // composition; ecosystem features register their own payload types
        // through the public IFeatureEventRegistry seam (bootstrap.EventRegistry)
        // after module registration and before publishing/subscribing.
        // Rejections are explicit results with structured diagnostics and
        // zero dispatch; a subscription to an unregistered type is a
        // developer error and fails fast (the null-handler discipline).
        private static void AssertBueV3EventOwnershipRouting(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                var litFeature = new FeatureId("io.github.yu80rice.bue.inventory-tidy");
                var thirdParty = new FeatureId("io.example.thirdparty");

                Group("归属拒绝", () =>
                {
                    // The anti-forgery invariant (T3 plan A): the publisher
                    // owner must equal the payload type's registered owner. A
                    // perfectly valid own-prefix eventId carrying SOMEONE
                    // ELSE'S payload type is rejected — the prefix rule alone
                    // cannot catch this forge.
                    var diagnostics = new List<string>();
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus(diagnostics.Add);
                    var received = new List<TidyCompleted>();
                    bus.Subscriber(thirdParty).Subscribe<TidyCompleted>(received.Add);
                    Check(!bus.Publisher(thirdParty).TryPublish("io.example.thirdparty/tidy-completed",
                            new TidyCompleted(litFeature, 2, 6, TidyCompletionResult.Succeeded, 0UL, 1UL)),
                        "归属拒绝：他人载荷类型经自有前缀身份串发布被拒（发布者 owner==类型归属 owner）");
                    Check(received.Count == 0, "归属拒绝：被拒发布零派发");
                    Check(diagnostics.Count > 0 && diagnostics[diagnostics.Count - 1].IndexOf("owner-mismatch", StringComparison.Ordinal) >= 0,
                        "归属拒绝：拒绝浮出结构化诊断（不静默吞）");
                });

                Group("未登记类型", () =>
                {
                    var diagnostics = new List<string>();
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus(diagnostics.Add);
                    var received = new List<RoutingProbePayload>();
                    bool subscribeRejected = false;
                    try { bus.Subscriber(thirdParty).Subscribe<RoutingProbePayload>(received.Add); }
                    catch (ArgumentException) { subscribeRejected = true; }
                    Check(subscribeRejected, "未登记类型：订阅未登记类型 fail-fast 拒绝（开发期错误，同 null handler 纪律）");
                    Check(received.Count == 0, "未登记类型：未登记订阅零回调");
                    Check(!bus.Publisher(thirdParty).TryPublish("io.example.thirdparty/probe-payload", new RoutingProbePayload(1UL)),
                        "未登记类型：未登记类型发布显式拒绝");
                    Check(diagnostics.Count > 0 && diagnostics[diagnostics.Count - 1].IndexOf("type-not-registered", StringComparison.Ordinal) >= 0,
                        "未登记类型：拒绝浮出结构化诊断（不静默吞）");
                });

                Group("登记成功", () =>
                {
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    var result = bus.EventRegistry(thirdParty).Register<EcoPayload>("io.example.thirdparty/eco-thing");
                    Check(result.Registered && result.Reason == FeatureEventRegistrationReason.None && result.DiagnosticId == "BUE-EVT-ACCEPT",
                        "登记成功：登记返回显式成功结果（None/BUE-EVT-ACCEPT）");
                    var received = new List<EcoPayload>();
                    bus.Subscriber(thirdParty).Subscribe<EcoPayload>(received.Add);
                    Check(bus.Publisher(thirdParty).TryPublish("io.example.thirdparty/eco-thing", new EcoPayload(7UL)),
                        "登记成功：登记后发布通过（登记时机=发布前）");
                    Check(received.Count == 1 && received[0].Marker == 7UL,
                        "登记成功：订阅者经登记的（EventId, Type）路由收到载荷");
                });

                Group("登记失败", () =>
                {
                    var diagnostics = new List<string>();
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus(diagnostics.Add);
                    var registry = bus.EventRegistry(thirdParty);
                    var nullId = registry.Register<EcoPayload>(null);
                    Check(!nullId.Registered && nullId.Reason == FeatureEventRegistrationReason.InvalidEventId && nullId.DiagnosticId == "BUE-EVT-001",
                        "登记失败：null 身份串=显式 InvalidEventId/BUE-EVT-001");
                    var emptyId = registry.Register<EcoPayload>(string.Empty);
                    Check(!emptyId.Registered && emptyId.Reason == FeatureEventRegistrationReason.InvalidEventId && emptyId.DiagnosticId == "BUE-EVT-001",
                        "登记失败：空身份串=BUE-EVT-001");
                    var noSlash = registry.Register<EcoPayload>("no-slash");
                    Check(!noSlash.Registered && noSlash.Reason == FeatureEventRegistrationReason.InvalidEventId && noSlash.DiagnosticId == "BUE-EVT-001",
                        "登记失败：无分隔符身份串=BUE-EVT-001");
                    var emptyName = registry.Register<EcoPayload>("io.example.thirdparty/");
                    Check(!emptyName.Registered && emptyName.Reason == FeatureEventRegistrationReason.InvalidEventId && emptyName.DiagnosticId == "BUE-EVT-001",
                        "登记失败：空事件名=BUE-EVT-001");
                    var emptyPrefix = registry.Register<EcoPayload>("/name");
                    Check(!emptyPrefix.Registered && emptyPrefix.Reason == FeatureEventRegistrationReason.InvalidEventId && emptyPrefix.DiagnosticId == "BUE-EVT-001",
                        "登记失败：空前缀=BUE-EVT-001");
                    var foreign = registry.Register<EcoPayload>("io.other.owner/eco-thing");
                    Check(!foreign.Registered && foreign.Reason == FeatureEventRegistrationReason.EventIdNotDerivedFromOwner && foreign.DiagnosticId == "BUE-EVT-002",
                        "登记失败：非本功能前缀=显式 EventIdNotDerivedFromOwner/BUE-EVT-002");
                    var hostPrefix = registry.Register<EcoPayload>("io.github.yu80rice.bue.host/eco-thing");
                    Check(!hostPrefix.Registered && hostPrefix.Reason == FeatureEventRegistrationReason.EventIdNotDerivedFromOwner && hostPrefix.DiagnosticId == "BUE-EVT-002",
                        "登记失败：宿主保留前缀不可由功能登记（BUE-EVT-002）");
                    Check(diagnostics.Count >= 7, "登记失败：每次拒绝浮出结构化诊断（不静默吞）");
                });

                Group("重复登记", () =>
                {
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    var registry = bus.EventRegistry(thirdParty);
                    Check(registry.Register<EcoPayload>("io.example.thirdparty/eco-thing").Registered, "setup: 首次登记成功");
                    var sameAgain = registry.Register<EcoPayload>("io.example.thirdparty/eco-thing");
                    Check(!sameAgain.Registered && sameAgain.Reason == FeatureEventRegistrationReason.EventTypeAlreadyRegistered && sameAgain.DiagnosticId == "BUE-EVT-003",
                        "重复登记：同类型同身份串再登记=显式拒绝（不覆盖）");
                    var differentId = registry.Register<EcoPayload>("io.example.thirdparty/eco-thing-2");
                    Check(!differentId.Registered && differentId.Reason == FeatureEventRegistrationReason.EventTypeAlreadyRegistered && differentId.DiagnosticId == "BUE-EVT-003",
                        "重复登记：同类型不同身份串=显式拒绝（一个载荷类型恰对应一个归属 EventId）");
                    var received = new List<EcoPayload>();
                    bus.Subscriber(thirdParty).Subscribe<EcoPayload>(received.Add);
                    Check(bus.Publisher(thirdParty).TryPublish("io.example.thirdparty/eco-thing", new EcoPayload(1UL)),
                        "重复登记：原归属身份串照常路由（拒绝未覆盖原映射）");
                    Check(!bus.Publisher(thirdParty).TryPublish("io.example.thirdparty/eco-thing-2", new EcoPayload(2UL)),
                        "重复登记：第二身份串无登记映射路由被拒");
                    Check(received.Count == 1 && received[0].Marker == 1UL, "重复登记：仅原归属身份串派发一份");
                    var idCollision = registry.Register<SecondPayload>("io.example.thirdparty/eco-thing");
                    Check(!idCollision.Registered && idCollision.Reason == FeatureEventRegistrationReason.EventIdAlreadyRegistered && idCollision.DiagnosticId == "BUE-EVT-004",
                        "重复登记：同身份串不同类型=显式拒绝（身份串唯一绑定类型）");
                });

                Group("同类型唯一归属", () =>
                {
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    var registry = bus.EventRegistry(thirdParty);
                    Check(registry.Register<SoloPayload>("io.example.thirdparty/solo-event").Registered, "setup: 独占类型登记成功");
                    var received = new List<SoloPayload>();
                    bus.Subscriber(thirdParty).Subscribe<SoloPayload>(received.Add);
                    Check(bus.Publisher(thirdParty).TryPublish("io.example.thirdparty/solo-event", new SoloPayload(5UL)),
                        "同类型唯一归属：登记身份串发布通过");
                    Check(!bus.Publisher(thirdParty).TryPublish("io.example.thirdparty/other-event", new SoloPayload(6UL)),
                        "同类型唯一归属：同类型第二身份串无路由（未登记映射显式拒绝）");
                    Check(received.Count == 1 && received[0].Marker == 5UL,
                        "同类型唯一归属：订阅者仅收到登记身份串的派发（不存在同类型不同事件互收）");
                });

                Group("宿主保留身份", () =>
                {
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    bool registryMintBlocked = false;
                    try { bus.EventRegistry(new FeatureId(BetterUnturnedExperience.Core.Events.HostTickClock.HostPublisherId)); }
                    catch (ArgumentException) { registryMintBlocked = true; }
                    Check(registryMintBlocked, "宿主保留身份：宿主标识不可铸入登记视图（与发布者视图同门，fail-fast）");
                    var eco = bus.EventRegistry(thirdParty);
                    var reTick = eco.Register<HostTick>(HostTick.EventId);
                    Check(!reTick.Registered && reTick.Reason == FeatureEventRegistrationReason.EventIdNotDerivedFromOwner && reTick.DiagnosticId == "BUE-EVT-002",
                        "宿主保留身份：功能登记宿主身份串被前缀门拒绝（宿主归属仅宿主可登记）");
                    Check(!bus.Publisher(thirdParty).TryPublish(HostTick.EventId, default(HostTick)),
                        "宿主保留身份：功能发布 HostTick 仍被归属检查拒绝");
                });

                Group("bootstrap 登记缝", () =>
                {
                    // The REAL host start composition hands every module a
                    // registry view bound to its own identity (availability
                    // matrix row: EventRegistry available from DEV-V3-02).
                    var probe = new MatrixProbeRegistration("io.example.routing-probe");
                    var probeRuntime = new FeatureRegistrationRuntime();
                    probeRuntime.OpenRegistration();
                    Check(probeRuntime.Register(probe).Accepted, "setup: 路由探针经登记桥受理");
                    Check(probeRuntime.CompleteRuntime(), "setup: 探针目录冻结");
                    var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                    var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 1002UL);
                    BueFeatureStartRuntime.StartCatalog(probeRuntime, network);
                    Check(probe.Module.Bootstrap != null && probe.Module.Bootstrap.EventRegistry != null,
                        "bootstrap 登记缝：真实 StartCatalog 组装 EventRegistry（永非 null）");
                    var registered = probe.Module.Bootstrap.EventRegistry.Register<RoutingProbePayload>("io.example.routing-probe/probe-payload");
                    Check(registered.Registered && registered.Reason == FeatureEventRegistrationReason.None && registered.DiagnosticId == "BUE-EVT-ACCEPT",
                        "bootstrap 登记缝：探针经 bootstrap 视图登记自己的载荷类型（视图绑定自身身份）");
                });

                Group("官方先行消费锚", () =>
                {
                    // The REAL LIT module publishes TidyCompleted through the
                    // host-registered ownership path and a real ecosystem
                    // consumer receives it; the consumer's own owner cannot
                    // forge the same payload type (ownership rejection).
                    var diagnostics = new List<string>();
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus(diagnostics.Add);
                    var consumer = new FeatureId("io.example.tidy-consumer");
                    var received = new List<TidyCompleted>();
                    bus.Subscriber(consumer).Subscribe<TidyCompleted>(received.Add);
                    var module = new InventoryTidyModule(litFeature);
                    var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                    var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 1003UL);
                    var result = module.Start(new FeatureBootstrap(default(FeatureScopeIdentity), 7UL, null,
                        bus.Subscriber(litFeature), bus.Publisher(litFeature), bus.EventRegistry(litFeature), null, null, null, network));
                    Check(result.Started, "官方先行消费锚：真实 LIT 模块经宿主 bootstrap 启动");
                    module.PublishTidyCompleted(3, 3, TidyCommitResult.Committed, 0UL, 42UL);
                    Check(received.Count == 1 && received[0].Publisher.Value == litFeature.Value
                            && received[0].Result == TidyCompletionResult.Succeeded && received[0].TransactionId == 42UL,
                        "官方先行消费锚：LIT 经登记路径发布 TidyCompleted 被真实消费（载荷透传）");
                    Check(!bus.Publisher(consumer).TryPublish("io.example.tidy-consumer/tidy-completed", new TidyCompleted(consumer, 2, 6, TidyCompletionResult.Succeeded, 0UL, 43UL)),
                        "官方先行消费锚：消费方 owner 冒发 TidyCompleted 被归属检查拒绝");
                    Check(received.Count == 1, "官方先行消费锚：伪造发布零派发");
                    Check(diagnostics.Count > 0 && diagnostics[diagnostics.Count - 1].IndexOf("owner-mismatch", StringComparison.Ordinal) >= 0,
                        "官方先行消费锚：伪造发布浮出结构化诊断（不静默吞）");
                });
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V3-02 event ownership routing collection: ALL GREEN (0 failures) — groups: 归属拒绝/未登记类型/登记成功/登记失败/重复登记/同类型唯一归属/宿主保留身份/bootstrap 登记缝/官方先行消费锚");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V3-02 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V3-02 routing probe payloads: RoutingProbePayload stays
        // unregistered on the 归属拒绝/未登记类型 groups' fresh buses to observe
        // the unregistered-type rejections; EcoPayload/SecondPayload/SoloPayload
        // are registered through the seam inside their own groups' buses.
        private readonly struct RoutingProbePayload
        {
            public ulong Marker { get; }
            public RoutingProbePayload(ulong marker) { Marker = marker; }
        }

        private readonly struct EcoPayload
        {
            public ulong Marker { get; }
            public EcoPayload(ulong marker) { Marker = marker; }
        }

        private readonly struct SecondPayload
        {
            public ulong Marker { get; }
            public SecondPayload(ulong marker) { Marker = marker; }
        }

        private readonly struct SoloPayload
        {
            public ulong Marker { get; }
            public SoloPayload(ulong marker) { Marker = marker; }
        }

        private sealed class MatrixProbeRegistration : IFeatureRegistration
        {
            internal readonly MatrixProbeModule Module = new MatrixProbeModule();

            internal MatrixProbeRegistration(string featureId)
            {
                Definition = new FeatureDefinitionArtifact(new FeatureId(featureId), 1, "bue-v3-matrix-probe", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
            }

            public FeatureDefinitionArtifact Definition { get; }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new MatrixProbeFactory(Module); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private sealed class MatrixProbeFactory : IFeatureModuleFactory
        {
            internal MatrixProbeFactory(MatrixProbeModule module) { Module = module; }
            internal MatrixProbeModule Module { get; }
            public IFeatureModule Create() { return Module; }
        }

        private sealed class MatrixProbeModule : IFeatureModule
        {
            internal IFeatureBootstrap Bootstrap { get; private set; }
            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                Bootstrap = bootstrap;
                return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V3-PROBE-START");
            }
            public void Stop(FeatureStopReason reason) { }
        }

        // DEV-V3-06 settings facet probes: SettingsFacetProbeRegistration is
        // the 矩阵「有 facet」侧（一个 ClientLocal 开关），FacetProbeRegistration
        // 为可配置形状（BUE-REG-011 逐例与目录投影断言用）。
        private sealed class SettingsFacetProbeRegistration : IFeatureRegistration, IFeatureSettingsRegistration
        {
            internal readonly MatrixProbeModule Module = new MatrixProbeModule();
            private readonly FeatureId feature;

            internal SettingsFacetProbeRegistration(string featureId)
            {
                feature = new FeatureId(featureId);
                Definition = new FeatureDefinitionArtifact(feature, 1, "bue-v3-settings-probe", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
            }

            public FeatureDefinitionArtifact Definition { get; }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new MatrixProbeFactory(Module); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
            public IReadOnlyList<SettingDescriptor> SettingDescriptors { get { return new[] { SettingsProbeToggle(feature, "probe.enabled") }; } }
            public System.Action OnSettingsApplied { get { return null; } }
        }

        private sealed class FacetProbeRegistration : IFeatureRegistration, IFeatureSettingsRegistration
        {
            private readonly IReadOnlyList<SettingDescriptor> descriptors;
            private readonly System.Action onApplied;

            internal FacetProbeRegistration(string featureId, IReadOnlyList<SettingDescriptor> descriptors, System.Action onApplied)
            {
                this.descriptors = descriptors;
                this.onApplied = onApplied;
                Definition = new FeatureDefinitionArtifact(new FeatureId(featureId), 1, "bue-v3-facet-probe", new Digest256(1, 2, 3, 5), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
            }

            public FeatureDefinitionArtifact Definition { get; }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new MatrixProbeFactory(new MatrixProbeModule()); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
            public IReadOnlyList<SettingDescriptor> SettingDescriptors { get { return descriptors; } }
            public System.Action OnSettingsApplied { get { return onApplied; } }
        }

        private static SettingDescriptor SettingsProbeToggle(FeatureId feature, string id)
        {
            return new SettingDescriptor(feature, id, id, id, SettingKind.Toggle, SettingAuthority.ClientLocal, SettingValue.Toggle(true),
                default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                new SettingValue[0], 0, string.Empty, 1, 0, string.Empty, string.Empty);
        }

        private static SettingDescriptor SettingsProbeDescriptor(FeatureId feature, string id)
        {
            return new SettingDescriptor(feature, id, id, id, SettingKind.Integer, SettingAuthority.ClientLocal, SettingValue.IntegerValue(5),
                new SettingValueOption(true, SettingValue.IntegerValue(0)), new SettingValueOption(true, SettingValue.IntegerValue(10)), default(SettingValueOption),
                new SettingValue[0], 0, string.Empty, 1, 0, string.Empty, string.Empty);
        }

        // DEV-V3-03 lifecycle probes: a fake module family that drives the REAL
        // host start composition (StartCatalog) and records every lifecycle
        // observation on an ordered event log (start/stop/track-result/dispose),
        // so the state machine tests assert outer-visible behavior only —
        // never the machine's internal tables.
        private sealed class ProbeResource : IDisposable
        {
            internal readonly string Name;
            private readonly List<string> eventLog;
            private readonly string fault;
            internal int DisposeCalls;

            internal ProbeResource(string name, List<string> eventLog, string fault = null)
            {
                Name = name;
                this.eventLog = eventLog;
                this.fault = fault;
            }

            public void Dispose()
            {
                DisposeCalls++;
                eventLog.Add("dispose:" + Name);
                if (fault != null) throw new InvalidOperationException(fault);
            }
        }

        private readonly struct LifecyclePayload
        {
            public ulong Marker { get; }
            public LifecyclePayload(ulong marker) { Marker = marker; }
        }

        private sealed class LifecycleProbeModule : IFeatureModule
        {
            // Every module instance the probe factory hands out (fixed or
            // provider-created) — the panel enable/disable groups observe the
            // fresh-instance re-enable through it.
            internal static readonly List<LifecycleProbeModule> Created = new List<LifecycleProbeModule>();
            internal IFeatureBootstrap Bootstrap { get; private set; }
            internal readonly List<string> EventLog = new List<string>();
            internal readonly List<ProbeResource> Tracked = new List<ProbeResource>();
            internal Func<LifecycleProbeModule, IFeatureBootstrap, FeatureStartResult> StartBehavior =
                (module, bootstrap) => new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V3-LIFECYCLE-START");

            public FeatureStartResult Start(IFeatureBootstrap bootstrap)
            {
                Bootstrap = bootstrap;
                EventLog.Add("start:" + bootstrap.LifecycleGeneration);
                return StartBehavior(this, bootstrap);
            }

            public void Stop(FeatureStopReason reason) { EventLog.Add("stop:" + reason); }

            internal bool Track(string name, string fault = null)
            {
                var resource = new ProbeResource(name, EventLog, fault);
                Tracked.Add(resource);
                var tracked = Bootstrap.Lifetime.TryTrack(resource);
                EventLog.Add("track:" + name + ":" + tracked);
                return tracked;
            }
        }

        private sealed class LifecycleProbeFactory : IFeatureModuleFactory
        {
            private readonly LifecycleProbeRegistration owner;
            internal LifecycleProbeFactory(LifecycleProbeRegistration owner) { this.owner = owner; }
            public IFeatureModule Create()
            {
                if (owner.FactoryError != null) throw owner.FactoryError;
                var module = owner.ModuleProvider != null ? owner.ModuleProvider() : owner.Module;
                LifecycleProbeModule.Created.Add(module);
                return module;
            }
        }

        private sealed class LifecycleProbeRegistration : IFeatureRegistration
        {
            internal readonly LifecycleProbeModule Module = new LifecycleProbeModule();
            internal Func<LifecycleProbeModule> ModuleProvider = null;
            internal Exception FactoryError = null;

            internal LifecycleProbeRegistration(string featureId)
            {
                Definition = new FeatureDefinitionArtifact(new FeatureId(featureId), 1, "bue-v3-lifecycle-probe", new Digest256(1, 2, 3, 4), new Digest256(5317555933983313923UL, 8642148531063968556UL, 2942485310001909708UL, 9366110643396117629UL), new byte[] { 1, 2, 3 });
            }

            internal string FeatureIdValue { get { return Definition.Feature.Value; } }
            public FeatureDefinitionArtifact Definition { get; }
            public ContractVersion MinimumBueContract { get { return new ContractVersion(2, 0); } }
            public IFeatureModuleFactory ModuleFactory { get { return new LifecycleProbeFactory(this); } }
            public IClientUiSatelliteRegistration ClientUi { get { return null; } }
        }

        private static BetterUnturnedExperience.Core.Network.BueNetworkRuntime NewLoopbackNetwork(ulong nonce)
        {
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            return new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), nonce);
        }

        // DEV-V3-03: the unified lifecycle state projection — the host owns the
        // FeatureState/FeatureStatusView/StateRevision machine, IFeatureLifetime
        // carries TryTrack (reverse disposal at the stop boundary, capacity
        // bound, stopped/isolated features cannot re-track) plus the minimal
        // read-only status query, Dependencies is the frozen-catalog read-only
        // capability lookup (Has/TryGet, no solver), and the panel enable/disable
        // seam rides FeatureStopReason.UserDisabled with a NEW lifecycle
        // generation on re-enable. Official LIT consumes the real seam first;
        // the NoOp fixture is the ecosystem-side control. Every group drives the
        // REAL StartCatalog composition with fake probe modules.
        private static void AssertBueV3LifecycleProjection(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                FeatureRegistrationRuntime StartProbe(params LifecycleProbeRegistration[] probes)
                {
                    var probeRuntime = new FeatureRegistrationRuntime();
                    probeRuntime.OpenRegistration();
                    foreach (var probe in probes) probeRuntime.Register(probe);
                    probeRuntime.CompleteRuntime();
                    BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(probeRuntime.Catalog.CatalogRevision));
                    return probeRuntime;
                }

                Group("矩阵接线侧", () =>
                {
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-a");
                    StartProbe(probe);
                    Check(probe.Module.Bootstrap != null, "矩阵接线侧：探针经真实 StartCatalog 启动");
                    Check(probe.Module.Bootstrap.Lifetime != null, "矩阵接线侧：Lifetime 接线后永非 null（可用性矩阵行=DEV-V3-03 起可用）");
                    Check(probe.Module.Bootstrap.Dependencies != null, "矩阵接线侧：Dependencies 接线后永非 null（可用性矩阵行=DEV-V3-03 起可用）");
                    Check(probe.Module.Bootstrap.EventRegistry != null, "矩阵接线侧：EventRegistry 行保持非 null（DEV-V3-02 行回归）");
                    Check(probe.Module.Bootstrap.Settings == null && probe.Module.Bootstrap.Logger != null,
                        "矩阵接线侧：Settings 行=有 facet 才接线（探针无 facet=null，06 红线保留）；Logger 行=DEV-V3-07 接线后永非 null（07 红线兑现）");
                });

                Group("TryTrack 登记", () =>
                {
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-track");
                    probe.Module.StartBehavior = (module, bootstrap) =>
                    {
                        var first = module.Track("r1");
                        var second = module.Track("r2");
                        var third = module.Track("r3");
                        module.EventLog.Add("track-results:" + first + second + third);
                        return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V3-LIFECYCLE-START");
                    };
                    StartProbe(probe);
                    Check(probe.Module.Bootstrap != null && probe.Module.Bootstrap.Lifetime != null, "TryTrack 登记：Lifetime 缝可用");
                    Check(probe.Module.EventLog.Contains("track-results:TrueTrueTrue"),
                        "TryTrack 登记：运行中功能 TryTrack 三次全部成功（DEV-V3-03 红测锚：Lifetime 接线前为 null）");
                });

                Group("逆序释放", () =>
                {
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-reverse");
                    probe.Module.StartBehavior = (module, bootstrap) =>
                    {
                        module.Track("r1");
                        module.Track("r2");
                        module.Track("r3");
                        return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V3-LIFECYCLE-START");
                    };
                    var probeRuntime = StartProbe(probe);
                    Check(probe.Module.Tracked.Count == 3, "逆序释放：三资源登记成功（Lifetime 缝可用）");
                    BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    var stopIndex = probe.Module.EventLog.IndexOf("stop:PluginStopping");
                    var r3Index = probe.Module.EventLog.IndexOf("dispose:r3");
                    var r2Index = probe.Module.EventLog.IndexOf("dispose:r2");
                    var r1Index = probe.Module.EventLog.IndexOf("dispose:r1");
                    Check(stopIndex >= 0 && r3Index > stopIndex && r2Index > r3Index && r1Index > r2Index,
                        "逆序释放：Stop 先于 Dispose，随后按注册逆序 r3→r2→r1 释放（DEV-V3-03 红测锚：接线前无资源清理）");
                    Check(probe.Module.Tracked.Count == 3 && probe.Module.Tracked[0].DisposeCalls == 1 && probe.Module.Tracked[2].DisposeCalls == 1,
                        "逆序释放：每资源恰释放一次");
                });

                Group("停止不可再登记", () =>
                {
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-stopped");
                    var probeRuntime = StartProbe(probe);
                    BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    Check(probe.Module.Bootstrap.Lifetime != null, "停止不可再登记：Lifetime 缝可用（接线前 null=红测锚）");
                    Check(!probe.Module.Bootstrap.Lifetime.TryTrack(new ProbeResource("late", probe.Module.EventLog)),
                        "停止不可再登记：已停止功能 TryTrack=显式 false");
                });

                Group("隔离资源撤销", () =>
                {
                    List<LifecyclePayload> isolatedReceived = null;
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-isolated");
                    probe.Module.StartBehavior = (module, bootstrap) =>
                    {
                        module.Track("a1");
                        bootstrap.EventRegistry.Register<LifecyclePayload>("io.example.lifecycle-isolated/payload");
                        var received = new List<LifecyclePayload>();
                        isolatedReceived = received;
                        bootstrap.Events.Subscribe<LifecyclePayload>(payload => received.Add(payload));
                        throw new InvalidOperationException("probe-start-failure");
                    };
                    var probeRuntime = StartProbe(probe);
                    Check(probe.Module.EventLog.Contains("dispose:a1"),
                        "隔离资源撤销：Start 抛异常→Isolating 撤资源→已登记资源被释放（DEV-V3-03 红测锚：接线前跳过无清理）");
                    BueHostEventRuntime.Bus.Publisher(new FeatureId("io.example.lifecycle-isolated"))
                        .TryPublish("io.example.lifecycle-isolated/payload", new LifecyclePayload(1UL));
                    Check(isolatedReceived != null && isolatedReceived.Count == 0,
                        "隔离资源撤销：隔离功能订阅已撤（发布零派发）");
                    Check(probeRuntime.Phase == FeatureRegistrationPhase.RuntimeReady,
                        "隔离资源撤销：单功能 Start 失败不升级 CoreSafeMode（回归锚：Phase 保持 RuntimeReady）");
                });

                Group("CoreSafeMode 不升级", () =>
                {
                    var boom = new LifecycleProbeRegistration("io.example.lifecycle-boom");
                    boom.FactoryError = new InvalidOperationException("probe-factory-failure");
                    var survivor = new LifecycleProbeRegistration("io.example.lifecycle-survivor");
                    var probeRuntime = StartProbe(boom, survivor);
                    Check(survivor.Module.Bootstrap != null, "CoreSafeMode 不升级：工厂抛异常只跳过该功能，后续功能继续启动");
                    Check(probeRuntime.Phase == FeatureRegistrationPhase.RuntimeReady,
                        "CoreSafeMode 不升级：组合期后运行期单功能失败永不升级 CoreSafeMode（Phase 保持 RuntimeReady）");
                });

                Group("容量与拒绝", () =>
                {
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-capacity");
                    var allAccepted = true;
                    bool overCapacity = false, nullRejected = false;
                    ProbeResource duplicated = null;
                    var duplicateResults = "??";
                    probe.Module.StartBehavior = (module, bootstrap) =>
                    {
                        // 63 fills + the duplicate probe = 64 accepted (the ticket
                        // picks the bound: 64 per feature per generation).
                        for (var i = 0; i < 63; i++) allAccepted &= module.Track("c" + i);
                        duplicated = new ProbeResource("dup", module.EventLog);
                        var first = bootstrap.Lifetime.TryTrack(duplicated);
                        var second = bootstrap.Lifetime.TryTrack(duplicated);
                        duplicateResults = (first ? "T" : "F") + (second ? "T" : "F");
                        overCapacity = module.Track("c63");
                        nullRejected = bootstrap.Lifetime.TryTrack(null);
                        return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V3-LIFECYCLE-START");
                    };
                    var captured = new List<string>();
                    BueRuntimeLog.Recorder = captured.Add;
                    try { StartProbe(probe); }
                    finally { BueRuntimeLog.Recorder = null; }
                    Check(allAccepted, "容量与拒绝：容量内 63+1 次登记全部成功（容量=64/功能/代际，本票定值可观察可测试）");
                    Check(duplicateResults == "TF", "容量与拒绝：同一实例重复登记显式拒绝（不覆盖不重复释放）");
                    Check(!overCapacity, "容量与拒绝：第 65 次登记显式拒绝（超容量=false 不静默）");
                    Check(!nullRejected, "容量与拒绝：TryTrack(null)=false");
                    Check(ContainsDiagnostic(captured, "BUE-LIFE-002"),
                        "容量与拒绝：超容量拒绝浮出结构化诊断（BUE-LIFE-002）");
                    Check(ContainsDiagnostic(captured, "BUE-LIFE-001"),
                        "容量与拒绝：null 登记浮出结构化诊断（BUE-LIFE-001）");
                    Check(ContainsDiagnostic(captured, "BUE-LIFE-005"),
                        "容量与拒绝：重复实例登记浮出结构化诊断（BUE-LIFE-005）");
                });

                Group("面板启停", () =>
                {
                    var feature = new FeatureId("io.example.lifecycle-panel");
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-panel");
                    List<LifecyclePayload> panelReceived = null;
                    probe.ModuleProvider = () =>
                    {
                        var module = new LifecycleProbeModule();
                        module.StartBehavior = (m, bootstrap) =>
                        {
                            m.Track("p1");
                            bootstrap.EventRegistry.Register<LifecyclePayload>("io.example.lifecycle-isolated/payload");
                            var received = new List<LifecyclePayload>();
                            panelReceived = received;
                            bootstrap.Events.Subscribe<LifecyclePayload>(payload => received.Add(payload));
                            return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V3-LIFECYCLE-START");
                        };
                        return module;
                    };
                    var baseCount = LifecycleProbeModule.Created.Count;
                    var probeRuntime = StartProbe(probe);
                    var first = LifecycleProbeModule.Created[baseCount];
                    Check(first != null && first.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running,
                        "面板启停：面板停用前 Running");
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(feature, false), "面板启停：UserDisabled 停止 seam 显式成功");
                    var stoppedView = first.Bootstrap.Lifetime.CurrentStatus;
                    Check(stoppedView.State == FeatureState.Stopped && stoppedView.StopReason == FeatureStopReason.UserDisabled,
                        "面板启停：停止投影=Stopped/UserDisabled（面板=command adapter，同一状态机）");
                    Check(first.EventLog.Contains("stop:UserDisabled"), "面板启停：模块收到 UserDisabled 停止回调");
                    Check(first.Tracked.TrueForAll(resource => resource.DisposeCalls == 1), "面板启停：停止边界释放已登记资源");
                    BueHostEventRuntime.Bus.Publisher(new FeatureId("io.example.lifecycle-isolated"))
                        .TryPublish("io.example.lifecycle-isolated/payload", new LifecyclePayload(2UL));
                    Check(panelReceived != null && panelReceived.Count == 0, "面板启停：停止后不再收事件（订阅已撤）");
                    Check(probeRuntime.Phase == FeatureRegistrationPhase.RuntimeReady, "面板启停：用户停用不触碰 CoreSafeMode");
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(feature, true), "面板启停：再启用 seam 显式成功");
                    var second = LifecycleProbeModule.Created[baseCount + 1];
                    Check(second != null && !ReferenceEquals(second, first), "面板启停：再启用经工厂获得新模块实例");
                    Check(second.Bootstrap.LifecycleGeneration > first.Bootstrap.LifecycleGeneration,
                        "面板启停：再启用=新生命周期代际（旧代际全失效）");
                    Check(second.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running, "面板启停：再启用后 Running");
                    Check(!first.Bootstrap.Lifetime.TryTrack(new ProbeResource("stale", first.EventLog)),
                        "面板启停：旧代际视图 TryTrack=false（旧代际资源句柄失效）");
                    Check(first.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running,
                        "面板启停：旧代际视图查询仍可用并如实返回当前状态");
                    Check(second.Bootstrap.Lifetime.CurrentStatus.StateRevision > stoppedView.StateRevision,
                        "面板启停：StateRevision 跨停用/再启用单调推进");
                    Check(!BueFeatureStartRuntime.SetFeatureEnabled(new FeatureId("io.example.never-registered"), false),
                        "面板启停：未注册功能 disable=显式 false");
                    // Isolated 不自动重启：手动再启用才从 Isolated 进入新代际 Starting。
                    var isolated = new LifecycleProbeRegistration("io.example.lifecycle-panel-iso");
                    var isoCreates = 0;
                    isolated.ModuleProvider = () =>
                    {
                        var module = new LifecycleProbeModule();
                        if (isoCreates++ == 0)
                            module.StartBehavior = (m, bootstrap) => throw new InvalidOperationException("probe-start-failure");
                        return module;
                    };
                    var isolatedBase = LifecycleProbeModule.Created.Count;
                    var isoRuntime = new FeatureRegistrationRuntime();
                    isoRuntime.OpenRegistration();
                    isoRuntime.Register(isolated);
                    isoRuntime.CompleteRuntime();
                    BueFeatureStartRuntime.StartCatalog(isoRuntime, NewLoopbackNetwork(isoRuntime.Catalog.CatalogRevision));
                    var isolatedFirst = LifecycleProbeModule.Created[isolatedBase];
                    Check(isolatedFirst != null && isolatedFirst.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Isolated,
                        "面板启停：Isolated 保持（不自动重启）");
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(new FeatureId("io.example.lifecycle-panel-iso"), true),
                        "面板启停：用户明确再启用 Isolated=显式成功");
                    var isolatedSecond = LifecycleProbeModule.Created[isolatedBase + 1];
                    Check(isolatedSecond != null
                        && isolatedSecond.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running
                        && isolatedSecond.Bootstrap.LifecycleGeneration > isolatedFirst.Bootstrap.LifecycleGeneration,
                        "面板启停：Isolated→手动再启用=新代际 Running");
                });

                // DEV-V4-03: the panel submits a TARGET state (启用/停用); the
                // machine interprets it at submit time. Agreement = success-
                // shaped no-op (现网对这三条路径返回失败——本组先红后绿修的是机，
                // 不是面板 if); difference moves (停跑/复停/复离); disallowed
                // transitions fail so the caller keeps its draft intent.
                Group("目标提交空操作", () =>
                {
                    // A. Running + 目标启用 = 空操作成功（现网 enable-rejected
                    // invalid-state 先红）：状态、修订、工厂实例三不变。
                    var feature = new FeatureId("io.example.lifecycle-target-running");
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-target-running");
                    var baseCount = LifecycleProbeModule.Created.Count;
                    StartProbe(probe);
                    var running = probe.Module;
                    Check(running.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running,
                        "目标提交 setup：探针 Running");
                    var runningRevision = running.Bootstrap.Lifetime.CurrentStatus.StateRevision;
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(feature, true),
                        "目标提交：Running 再提交启用=空操作成功（现网 invalid-state 拒，先红）");
                    var afterNoopEnable = running.Bootstrap.Lifetime.CurrentStatus;
                    Check(afterNoopEnable.State == FeatureState.Running && afterNoopEnable.StateRevision == runningRevision,
                        "目标提交：Running 空操作后状态与修订不变（无假迁移）");
                    Check(LifecycleProbeModule.Created.Count == baseCount + 1,
                        "目标提交：Running 空操作不经工厂（不新开代际）");

                    // B. 已用户停用再提交停用 = 空操作成功（现网 disable-rejected
                    // not-running 先红）：投影停在 Stopped/UserDisabled 原样。
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(feature, false),
                        "目标提交 setup：面板停用=UserDisabled");
                    var stoppedView = running.Bootstrap.Lifetime.CurrentStatus;
                    Check(stoppedView.State == FeatureState.Stopped && stoppedView.StopReason == FeatureStopReason.UserDisabled,
                        "目标提交 setup：停用投影=Stopped/UserDisabled");
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(feature, false),
                        "目标提交：已停用再提交停用=空操作成功（现网 not-running 拒，先红）");
                    var afterNoopDisable = running.Bootstrap.Lifetime.CurrentStatus;
                    Check(afterNoopDisable.State == FeatureState.Stopped
                            && afterNoopDisable.StopReason == FeatureStopReason.UserDisabled
                            && afterNoopDisable.StateRevision == stoppedView.StateRevision,
                        "目标提交：已停用空操作后投影原样（无假迁移无新代际）");
                    Check(LifecycleProbeModule.Created.Count == baseCount + 1,
                        "目标提交：已停用空操作不经工厂");

                    // C. Isolated + 目标停用 = 空操作成功（现网 not-running 拒，
                    // 先红）：保持隔离不走会失败的 disable，不新开代际。
                    var isoFeature = new FeatureId("io.example.lifecycle-target-iso");
                    var isoProbe = new LifecycleProbeRegistration("io.example.lifecycle-target-iso");
                    var isoCreates = 0;
                    isoProbe.ModuleProvider = () =>
                    {
                        var module = new LifecycleProbeModule();
                        if (isoCreates++ == 0)
                            module.StartBehavior = (m, bootstrap) => throw new InvalidOperationException("probe-start-failure");
                        return module;
                    };
                    var isoBase = LifecycleProbeModule.Created.Count;
                    StartProbe(isoProbe);
                    var isolatedFirst = LifecycleProbeModule.Created[isoBase];
                    Check(isolatedFirst != null && isolatedFirst.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Isolated,
                        "目标提交 setup：Start 抛→Isolated");
                    var isoRevision = isolatedFirst.Bootstrap.Lifetime.CurrentStatus.StateRevision;
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(isoFeature, false),
                        "目标提交：Isolated 再提交停用=空操作成功（保持隔离，现网 not-running 拒，先红）");
                    var isoAfter = isolatedFirst.Bootstrap.Lifetime.CurrentStatus;
                    Check(isoAfter.State == FeatureState.Isolated && isoAfter.StopReason == FeatureStopReason.RuntimeIsolated
                            && isoAfter.StateRevision == isoRevision,
                        "目标提交：Isolated 空操作后保持隔离（无假迁移无新代际）");
                    Check(LifecycleProbeModule.Created.Count == isoBase + 1,
                        "目标提交：Isolated 空操作不经工厂");

                    // D. Isolated + 目标启用 = 恢复并新代际（既有语义的镜像锚，
                    // 与「面板启停」组的 Isolated 再启用同判据收进本组）。
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(isoFeature, true),
                        "目标提交：Isolated 目标启用=恢复成功");
                    Check(LifecycleProbeModule.Created.Count == isoBase + 2,
                        "目标提交：Isolated 恢复经工厂新开代际");
                    var revived = LifecycleProbeModule.Created[isoBase + 1];
                    Check(revived != null && revived.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running
                            && revived.Bootstrap.LifecycleGeneration > isolatedFirst.Bootstrap.LifecycleGeneration,
                        "目标提交：Isolated 恢复=新代际 Running");

                    // E. 过渡期（Starting）内重入提交两个方向都不允许——显式失败，
                    // 且不污染本次启动（不迁移不撤销，Start 照常完成 Running）。
                    var reentry = new FeatureId("io.example.lifecycle-target-reentry");
                    var reentryProbe = new LifecycleProbeRegistration("io.example.lifecycle-target-reentry");
                    var disableDuringStarting = true;
                    var enableDuringStarting = true;
                    reentryProbe.ModuleProvider = () =>
                    {
                        var module = new LifecycleProbeModule();
                        module.StartBehavior = (m, bootstrap) =>
                        {
                            disableDuringStarting = BueFeatureStartRuntime.SetFeatureEnabled(reentry, false);
                            enableDuringStarting = BueFeatureStartRuntime.SetFeatureEnabled(reentry, true);
                            return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V3-LIFECYCLE-START");
                        };
                        return module;
                    };
                    StartProbe(reentryProbe);
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(reentry, false), "目标提交 setup：reentry 停用");
                    var reentryBase = LifecycleProbeModule.Created.Count;
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(reentry, true),
                        "目标提交 setup：reentry 再启用（Start 内重入提交）");
                    Check(!disableDuringStarting, "目标提交：Starting 过渡期提交停用=显式失败（不允许转换）");
                    Check(!enableDuringStarting, "目标提交：Starting 过渡期提交启用=显式失败（不允许转换）");
                    var reentryModule = LifecycleProbeModule.Created[reentryBase];
                    Check(reentryModule != null && reentryModule.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running,
                        "目标提交：过渡期失败的提交不污染本次启动（仍 Running）");

                    // F. 未注册功能两个方向的目标提交都显式失败（不允许转换；
                    // disable 侧与「面板启停」组的 never-registered 锚同构）。
                    Check(!BueFeatureStartRuntime.SetFeatureEnabled(new FeatureId("io.example.never-registered-target"), true),
                        "目标提交：未注册功能提交启用=显式失败");
                });

                Group("隔离不扩散", () =>
                {
                    var boom = new LifecycleProbeRegistration("io.example.lifecycle-iso-view");
                    boom.Module.StartBehavior = (module, bootstrap) =>
                    {
                        module.Track("b1");
                        throw new InvalidOperationException("probe-start-failure");
                    };
                    var okA = new LifecycleProbeRegistration("io.example.lifecycle-iso-ok-a");
                    var okB = new LifecycleProbeRegistration("io.example.lifecycle-iso-ok-b");
                    StartProbe(boom, okA, okB);
                    var view = boom.Module.Bootstrap.Lifetime.CurrentStatus;
                    Check(view.State == FeatureState.Isolated, "隔离不扩散：Start 抛异常→Isolated（状态机记录，不扩散）");
                    Check(view.StopReason == FeatureStopReason.RuntimeIsolated && view.Error == FrameworkErrorCode.ModuleStartFailed,
                        "隔离不扩散：隔离投影携带 RuntimeIsolated/ModuleStartFailed");
                    Check(!string.IsNullOrEmpty(view.DiagnosticId), "隔离不扩散：隔离投影携带诊断");
                    Check(okA.Module.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running
                        && okB.Module.Bootstrap.Lifetime.CurrentStatus.State == FeatureState.Running,
                        "隔离不扩散：其他功能不受影响继续 Running（单模块状态变化不改变其他模块状态）");
                    Check(boom.Module.EventLog.Contains("dispose:b1"), "隔离不扩散：Isolating 撤资源（已登记资源释放）");
                    var again = boom.Module.Bootstrap.Lifetime.CurrentStatus;
                    Check(again.State == FeatureState.Isolated && again.StateRevision == view.StateRevision,
                        "隔离不扩散：隔离后查询仍可用、状态如实且无自动重启（修订稳定）");
                    Check(!boom.Module.Bootstrap.Lifetime.TryTrack(new ProbeResource("late", boom.Module.EventLog)),
                        "隔离不扩散：隔离后 TryTrack=显式 false（已隔离功能不可再登记）");
                });

                Group("状态投影", () =>
                {
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-view");
                    FeatureState atStartState = FeatureState.Discovered;
                    ulong atStartRevision = 0;
                    probe.Module.StartBehavior = (module, bootstrap) =>
                    {
                        var view = bootstrap.Lifetime.CurrentStatus;
                        atStartState = view.State;
                        atStartRevision = view.StateRevision;
                        return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-V3-LIFECYCLE-START");
                    };
                    StartProbe(probe);
                    var status = probe.Module.Bootstrap.Lifetime.CurrentStatus;
                    Check(atStartState == FeatureState.Starting, "状态投影：模块内 Start 期查询可用且如实报 Starting（接线后任何阶段可用）");
                    Check(status.State == FeatureState.Running && status.Feature.Value == "io.example.lifecycle-view",
                        "状态投影：启动后查询=Running 且视图绑定自身 FeatureId");
                    Check(status.Error == FrameworkErrorCode.None && status.StopReason == FeatureStopReason.None,
                        "状态投影：Running 投影 Error/StopReason 为 None");
                    Check(status.StateRevision > atStartRevision && status.StateRevision > 0,
                        "状态投影：StateRevision 随合法变化单调推进");
                    var stable = status.StateRevision;
                    Check(probe.Module.Bootstrap.Lifetime.CurrentStatus.StateRevision == stable,
                        "状态投影：无状态变化时修订稳定（不为查询生成新视图）");
                });

                Group("目录能力查询", () =>
                {
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-deps");
                    var sibling = new LifecycleProbeRegistration("io.example.lifecycle-dep-sibling");
                    StartProbe(probe, sibling);
                    var deps = probe.Module.Bootstrap.Dependencies;
                    NegotiatedFeatureView found;
                    Check(deps.Has("io.example.lifecycle-dep-sibling", null, 0),
                        "目录能力查询：Has=冻结目录内依赖（纯存在性查询）");
                    Check(deps.Has("io.example.lifecycle-dep-sibling", string.Empty, 0),
                        "目录能力查询：空能力串=存在性查询");
                    Check(!deps.Has("io.example.never-registered", null, 0), "目录能力查询：目录外依赖 Has=false");
                    Check(!deps.Has("io.example.lifecycle-dep-sibling", "some-capability", 1),
                        "目录能力查询：无能力登记源→非空能力显式不可证（fail-closed，不求解器）");
                    Check(!deps.Has(null, null, 0) && !deps.Has(string.Empty, null, 0),
                        "目录能力查询：空依赖 id fail-closed");
                    Check(deps.TryGet("io.example.lifecycle-dep-sibling", out found)
                        && found.Feature.Value == "io.example.lifecycle-dep-sibling"
                        && found.State == NegotiationState.Available && found.Error == FrameworkErrorCode.None,
                        "目录能力查询：TryGet 命中=Available 投影");
                    Check(found.AcceptedCapabilities != null && found.AcceptedCapabilities.Count == 0,
                        "目录能力查询：能力投影为空集（非 null）");
                    Check(!deps.TryGet("io.example.never-registered", out _),
                        "目录能力查询：TryGet 目录外=显式 false");
                });

                Group("代际轴分离", () =>
                {
                    var probe = new LifecycleProbeRegistration("io.example.lifecycle-generations");
                    probe.ModuleProvider = () => new LifecycleProbeModule();
                    var baseCount = LifecycleProbeModule.Created.Count;
                    StartProbe(probe);
                    var first = LifecycleProbeModule.Created[baseCount];
                    var networkFirst = first.Bootstrap.Network;
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(new FeatureId("io.example.lifecycle-generations"), false),
                        "代际轴分离 setup：面板停止");
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(new FeatureId("io.example.lifecycle-generations"), true),
                        "代际轴分离 setup：面板再启用");
                    var second = LifecycleProbeModule.Created[baseCount + 1];
                    Check(second != null && second.Bootstrap.Network == networkFirst,
                        "代际轴分离：生命周期代际推进不动网络域（同一 IBueNetworkApi 实例）");
                    Check(second.Bootstrap.LifecycleGeneration != first.Bootstrap.LifecycleGeneration,
                        "代际轴分离：LifecycleGeneration 推进而不触碰 ConnectionGeneration 轴（两代际轴完全分离）");
                    Check(second.Bootstrap.Network.Sessions.Count == 0,
                        "代际轴分离：启停循环不产生/不销毁网络会话（断线不误判为模块停止）");
                });

                Group("官方先行消费锚", () =>
                {
                    var litFeature = new FeatureId(InventoryTidyFeatureRegistration.FeatureIdValue);
                    InventoryTidyFeatureRegistration.WiredModule = null;
                    var litRuntime = new FeatureRegistrationRuntime();
                    litRuntime.OpenRegistration();
                    Check(litRuntime.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                        "官方锚 setup：官方 LIT 登记入探针运行时");
                    Check(litRuntime.CompleteRuntime(), "官方锚 setup：目录冻结");
                    var captured = new List<string>();
                    BueRuntimeLog.Recorder = captured.Add;
                    BueFeatureStartRuntime.StartCatalog(litRuntime, NewLoopbackNetwork(2001UL));
                    BueRuntimeLog.Recorder = null;
                    var litModule = InventoryTidyFeatureRegistration.WiredModule;
                    Check(litModule != null, "官方锚：官方 LIT 经真实 StartCatalog 启动（工厂装配实例）");
                    Check(ContainsDiagnostic(captured, "event=feature-resource") && ContainsDiagnostic(captured, "io.github.yu80rice.bue.inventory-tidy"),
                        "官方锚：官方模块 TryTrack 真实资源浮出结构化登记行");
                    Check(litModule.Lifetime != null
                        && litModule.Lifetime.CurrentStatus.State == FeatureState.Running,
                        "官方锚：官方模块经 bootstrap.Lifetime 只读查询自身状态=Running（状态查询缝真实消费）");
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(litFeature, false),
                        "官方锚：官方功能走真 UserDisabled 面板停止 seam");
                    Check(litModule.Lifetime.CurrentStatus.State == FeatureState.Stopped
                        && litModule.Lifetime.CurrentStatus.StopReason == FeatureStopReason.UserDisabled,
                        "官方锚：官方功能停止投影=Stopped/UserDisabled（与生态同一状态机）");
                    InventoryTidyFeatureRegistration.WiredModule = null;
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(litFeature, true), "官方锚：官方功能经面板 seam 再启用");
                    var litModule2 = InventoryTidyFeatureRegistration.WiredModule;
                    Check(litModule2 != null && !ReferenceEquals(litModule2, litModule), "官方锚：再启用经工厂获得新 LIT 实例");
                    Check(litModule2.LifecycleGeneration > litModule.LifecycleGeneration, "官方锚：再启用=新生命周期代际");
                    Check(litModule2.Lifetime != null
                        && litModule2.Lifetime.CurrentStatus.State == FeatureState.Running,
                        "官方锚：新实例查询=Running（重新运行成功）");
                    // DEV-V4-03 目标提交：官方功能 Running 时再提交启用=空操作
                    // 成功（现网 invalid-state 拒，先红）——官方先行消费空操作语义。
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(litFeature, true),
                        "官方锚：Running 官方功能再提交启用=空操作成功（现网拒，先红）");
                    Check(ReferenceEquals(InventoryTidyFeatureRegistration.WiredModule, litModule2),
                        "官方锚：空操作不经工厂（官方实例不变，无新代际）");
                    var received = new List<TidyCompleted>();
                    BueHostEventRuntime.Bus.Subscriber(litFeature).Subscribe<TidyCompleted>(received.Add);
                    litModule2.PublishTidyCompleted(3, 3, TidyCommitResult.Committed, 0UL, 900UL);
                    Check(received.Count == 1 && received[0].TransactionId == 900UL,
                        "官方锚：再启用后官方发布路径真实恢复（TidyCompleted 经登记路径透传）");
                });

                Group("生态对照 NoOp", () =>
                {
                    var noopRuntime = new FeatureRegistrationRuntime();
                    noopRuntime.OpenRegistration();
                    Check(noopRuntime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted,
                        "生态对照 setup：NoOp probe 登记入探针运行时（生态样例同桥同规）");
                    Check(noopRuntime.CompleteRuntime(), "生态对照 setup：目录冻结");
                    BueFeatureStartRuntime.StartCatalog(noopRuntime, NewLoopbackNetwork(2002UL));
                    Check(NoOpFeatureRegistration.LastProbe != null && NoOpFeatureRegistration.LastProbe.Started,
                        "生态对照：NoOp probe 经真实 StartCatalog 启动（Started=true，练缝不再是空壳）");
                    Check(NoOpFeatureRegistration.LastProbe.Tracked && !NoOpFeatureRegistration.LastProbe.ResourceDisposed,
                        "生态对照：probe 经 TryTrack 登记资源且运行中未释放");
                    Check(NoOpFeatureRegistration.LastProbe.QueriedStateAtStart == FeatureState.Starting,
                        "生态对照：probe 经只读查询在 Start 期观察到 Starting（状态查询缝生态侧消费）");
                    BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    Check(NoOpFeatureRegistration.LastProbe.ResourceDisposed,
                        "生态对照：停止边界 probe 资源被宿主释放（逆序清理对生态样例同权）");
                });

                // DEV-V4-04 → DEV-V4-06：官方 legacy enabled 迁移的退役面在 06
                // 翻转为「facet 回归」——LIT 声明两条全局 Choice（mode/direction），
                // 而 enabled 总开关保持退役（descriptor 集里没有它）；启动路径为
                // 两条 Choice 组装设置 runtime（面板行投影与点击快照的同一数据源）。
                Group("legacy facet 退役", () =>
                {
                    var litFeature = new FeatureId(InventoryTidyFeatureRegistration.FeatureIdValue);
                    var settingsRoot = Path.Combine(Path.GetTempPath(), "bue-v404-retire-" + Guid.NewGuid().ToString("N"));
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(settingsRoot, () => true, null);
                    try
                    {
                        InventoryTidyFeatureRegistration.WiredModule = null;
                        var litRuntime = new FeatureRegistrationRuntime();
                        litRuntime.OpenRegistration();
                        Check(litRuntime.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                            "退役 setup：官方 LIT 登记");
                        Check(litRuntime.CompleteRuntime(), "退役 setup：目录冻结");
                        FeatureRegistrationEntry litEntry = null;
                        var catalogEntries = litRuntime.Catalog.Entries;
                        for (var i = 0; i < catalogEntries.Count; i++)
                            if (catalogEntries[i].Definition.Feature.Value == litFeature.Value) litEntry = catalogEntries[i];
                        Check(litEntry != null && litEntry.SettingDescriptors != null && litEntry.SettingDescriptors.Count == 1,
                            "DEV-V5-02：目录条目声明恰一条全局 Choice（统一排版后仅剩 direction 收尾偏好，mode 档随三模式退役）");
                        var hasEnabledDescriptor = false;
                        var hasModeDescriptor = false;
                        var hasDirectionDescriptor = false;
                        if (litEntry != null && litEntry.SettingDescriptors != null)
                        {
                            for (var i = 0; i < litEntry.SettingDescriptors.Count; i++)
                            {
                                var descriptorId = litEntry.SettingDescriptors[i].SettingId;
                                if (descriptorId == "inventorytidy.enabled") hasEnabledDescriptor = true;
                                if (descriptorId == "inventorytidy.mode") hasModeDescriptor = true;
                                if (descriptorId == "inventorytidy.direction") hasDirectionDescriptor = true;
                            }
                        }
                        Check(hasDirectionDescriptor && !hasModeDescriptor && !hasEnabledDescriptor,
                            "enabled 保持退役 + DEV-V5-02：descriptor 集恰含 direction、绝无 enabled 与 mode（三档不复活、旧总开关不复进 schema）");
                        BueFeatureStartRuntime.StartCatalog(litRuntime, NewLoopbackNetwork(litRuntime.Catalog.CatalogRevision));
                        Check(BueSettingsRuntime.Registry.TryGetRuntime(litFeature) != null,
                            "DEV-V4-06：启动路径为两条 Choice 组装设置 runtime（同一权威源供面板与点击快照）");
                        var litWired = InventoryTidyFeatureRegistration.WiredModule;
                        Check(litWired != null && litWired.SettingsView != null,
                            "DEV-V4-06：真实启动的 LIT 模块持注入 view（点击读快照的数据源在位）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        try { if (Directory.Exists(settingsRoot)) Directory.Delete(settingsRoot, true); } catch (IOException) { }
                    }
                });

                // DEV-V4-04：官方 legacy enabled 迁移 e2e——五宿主功能走真实登记/目录/
                // 目标提交（BII 的读源由组合层登记，这里以表内容锚定）。显式别名表，
                // 不做字段名扫描；false→交 03 机解释；意图事实=持久权威。
                Group("legacy 迁移六项", () =>
                {
                    var root = Path.Combine(Path.GetTempPath(), "bue-v404-migrate-" + Guid.NewGuid().ToString("N"));
                    var previousHostRuntime = BueRuntimeHost.CurrentRuntime;
                    Directory.CreateDirectory(root);
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, () => true, null);
                    BueFeatureIntentRuntime.Clear();
                    BueFeatureIntentRuntime.EnsureCreated(root, null);
                    try
                    {
                        var litFeature = new FeatureId(InventoryTidyFeatureRegistration.FeatureIdValue);
                        var lirFeature = new FeatureId(InPlaceReloadFeatureRegistration.FeatureIdValue);
                        var lhtFeature = new FeatureId(HordeTrackerFeatureRegistration.FeatureIdValue);
                        WriteLegacyToggleDoc(root, litFeature, "inventorytidy.enabled", false);
                        WriteLegacyToggleDoc(root, lirFeature, "inplacereload.enabled", false);
                        WriteLegacyToggleDoc(root, NetworkModuleAdapter.NetworkFeature, "network.enabled", false);
                        WriteLegacyToggleDoc(root, NetworkModuleAdapter.V1CompatFeature, "v1compat.enabled", true);
                        // LHT 故意不给旧文档（不存在 → 不改生命周期）。

                        var biiFeature = BetterItemInteractionSettingsState.Feature;
                        InventoryTidyFeatureRegistration.WiredModule = null;
                        InPlaceReloadFeatureRegistration.WiredModule = null;
                        HordeTrackerFeatureRegistration.WiredModule = null;
                        var migrationRuntime = new FeatureRegistrationRuntime();
                        BueRuntimeHost.Bind(migrationRuntime);
                        migrationRuntime.OpenRegistration();
                        Check(migrationRuntime.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted, "迁移 setup：LIT 登记");
                        Check(migrationRuntime.Register(InPlaceReloadFeatureRegistration.CreateRegistration()).Accepted, "迁移 setup：LIR 登记");
                        Check(migrationRuntime.Register(HordeTrackerFeatureRegistration.CreateRegistration()).Accepted, "迁移 setup：LHT 登记");
                        foreach (var registration in NetworkModuleFeatureRegistration.CreateOfficialRegistrations())
                            Check(migrationRuntime.Register(registration).Accepted, "迁移 setup：network 双 facet 登记");
                        // BII 登记进同一目录（宿主模块=良性隔离壳）：其旧 Enabled
                        // 是磁盘文档事实，迁移路径与其余五项完全同构。
                        Check(BetterItemInteractionFeatureRegistration.Register().Accepted, "迁移 setup：BII 经公共桥登记");
                        WriteLegacyToggleDoc(root, biiFeature, "Enabled", false);
                        Check(migrationRuntime.CompleteRuntime(), "迁移 setup：目录冻结");
                        BueFeatureStartRuntime.StartCatalog(migrationRuntime, NewLoopbackNetwork(migrationRuntime.Catalog.CatalogRevision));
                        // DEV-V4-06 回归锚：facet 回归后宿主 runtime 先于迁移加载
                        // 同一文档——文件层不得把「缺新 schema 键」的 legacy 文档当
                        // 损坏隔离，否则迁移读不到旧值，升级会静默丢停用偏好。
                        var legacyDocPath = new BetterUnturnedExperience.Core.Settings.FileSettingsPersistence(root).GetPath(litFeature, SettingRevisionScope.ClientPreference);
                        Check(File.Exists(legacyDocPath),
                            "迁移窗口：legacy 文档在宿主 runtime 首读后原样在位（不被当损坏隔离）");
                        // LHT 在测试宿主上工厂/补丁不可用=启动期 Isolated（宿主局限，
                        // 与迁移无关）——「不改生命周期」的外显=迁移前后状态原样。
                        FeatureStatusView lhtBefore;
                        Check(BueFeatureStartRuntime.TryGetStatus(lhtFeature, out lhtBefore), "迁移 setup：LHT 状态可查");

                        BueLegacyEnabledMigrationAdapter.Run(root, line => { });

                        FeatureStatusView status;
                        Check(BueFeatureStartRuntime.TryGetStatus(litFeature, out status) && status.State == FeatureState.Stopped
                                && status.StopReason == FeatureStopReason.UserDisabled,
                            "迁移：LIT 旧 enabled=false → 升级后用户停用（Stopped/UserDisabled，机解释 Running→停）");
                        Check(BueFeatureStartRuntime.TryGetStatus(lirFeature, out status) && status.State == FeatureState.Stopped
                                && status.StopReason == FeatureStopReason.UserDisabled,
                            "迁移：LIR 旧 enabled=false → 升级后用户停用");
                        Check(BueFeatureStartRuntime.TryGetStatus(NetworkModuleAdapter.NetworkFeature, out status)
                                && status.State == FeatureState.Isolated,
                            "迁移：network Isolated+停用提交=空操作成功（保持隔离不盲调，先例非回归）");
                        Check(BueFeatureStartRuntime.TryGetStatus(biiFeature, out status)
                                && status.State == FeatureState.Isolated,
                            "迁移：BII Isolated+停用提交=空操作成功（良性隔离壳，先例非回归）");
                        var intents = BueFeatureIntentRuntime.StoreFor(root);
                        Check(intents.HasUserDisabled(litFeature) && intents.HasUserDisabled(lirFeature)
                                && intents.HasUserDisabled(NetworkModuleAdapter.NetworkFeature)
                                && intents.HasUserDisabled(biiFeature),
                            "迁移：UserDisabled 意图事实持久落盘（LIT/LIR/network/BII=新权威）");
                        Check(BueFeatureStartRuntime.TryGetStatus(lhtFeature, out status) && status.State == lhtBefore.State
                                && status.StateRevision == lhtBefore.StateRevision,
                            "迁移：LHT 无旧文档 → 不改生命周期（迁移前后状态/修订原样）");
                        Check(!intents.HasUserDisabled(NetworkModuleAdapter.V1CompatFeature) && !intents.HasUserDisabled(lhtFeature),
                            "迁移：v1compat 旧值 true / LHT 无文档 → 不落意图");
                        bool legacyValue;
                        Check(!LegacyDocHasToggle(root, litFeature, "inventorytidy.enabled", out legacyValue),
                            "迁移：LIT 旧键退役（schema+面板已退役，文档层同步退役）");
                        Check(!LegacyDocHasToggle(root, NetworkModuleAdapter.NetworkFeature, "network.enabled", out legacyValue),
                            "迁移：network 旧键退役");
                        Check(LegacyDocHasToggle(root, NetworkModuleAdapter.V1CompatFeature, "v1compat.enabled", out legacyValue) && legacyValue,
                            "迁移：v1compat 旧值 true → 文档原样保留（不额外触碰）");
                        Check(!LegacyDocHasToggle(root, biiFeature, "Enabled", out legacyValue),
                            "迁移：BII 旧 Enabled=false → 意图落盘且旧键退役（与其余五项同构）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        BueRuntimeHost.Bind(previousHostRuntime);
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        BueFeatureIntentRuntime.Clear();
                        try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (IOException) { }
                    }
                });

                // DEV-V4-04：幂等（重复加载不重复代际/不重复退役）、未声明 enabled 不迁、
                // 显式别名表恰六项（AutoRotate 与 noop.probe-toggle 不登记）。
                Group("legacy 迁移幂等与未声明", () =>
                {
                    var root = Path.Combine(Path.GetTempPath(), "bue-v404-idem-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(root);
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, () => true, null);
                    BueFeatureIntentRuntime.Clear();
                    BueFeatureIntentRuntime.EnsureCreated(root, null);
                    try
                    {
                        var litFeature = new FeatureId(InventoryTidyFeatureRegistration.FeatureIdValue);
                        var lhtFeature = new FeatureId(HordeTrackerFeatureRegistration.FeatureIdValue);
                        var strangerFeature = new FeatureId("io.example.ecosystem-legacy");
                        WriteLegacyToggleDoc(root, litFeature, "inventorytidy.enabled", false);
                        WriteLegacyToggleDoc(root, lhtFeature, "hordetracker.enabled", false);
                        WriteLegacyToggleDoc(root, NetworkModuleAdapter.V1CompatFeature, "v1compat.enabled", false);
                        WriteLegacyToggleDoc(root, strangerFeature, "myservice.enabled", false);

                        InventoryTidyFeatureRegistration.WiredModule = null;
                        HordeTrackerFeatureRegistration.WiredModule = null;
                        var idemRuntime = new FeatureRegistrationRuntime();
                        idemRuntime.OpenRegistration();
                        Check(idemRuntime.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted, "幂等 setup：LIT 登记");
                        Check(idemRuntime.Register(HordeTrackerFeatureRegistration.CreateRegistration()).Accepted, "幂等 setup：LHT 登记");
                        foreach (var registration in NetworkModuleFeatureRegistration.CreateOfficialRegistrations())
                            Check(idemRuntime.Register(registration).Accepted, "幂等 setup：network 双 facet 登记");
                        Check(idemRuntime.CompleteRuntime(), "幂等 setup：目录冻结");
                        BueFeatureStartRuntime.StartCatalog(idemRuntime, NewLoopbackNetwork(idemRuntime.Catalog.CatalogRevision));

                        BueLegacyEnabledMigrationAdapter.Run(root, line => { });
                        FeatureStatusView firstStatus;
                        Check(BueFeatureStartRuntime.TryGetStatus(litFeature, out firstStatus)
                                && firstStatus.State == FeatureState.Stopped && firstStatus.StopReason == FeatureStopReason.UserDisabled,
                            "幂等 setup：首次迁移=用户停用");
                        var wiredAfterFirst = InventoryTidyFeatureRegistration.WiredModule;

                        BueLegacyEnabledMigrationAdapter.Run(root, line => { });
                        FeatureStatusView secondStatus;
                        Check(BueFeatureStartRuntime.TryGetStatus(litFeature, out secondStatus)
                                && secondStatus.State == FeatureState.Stopped && secondStatus.StopReason == FeatureStopReason.UserDisabled
                                && secondStatus.StateRevision == firstStatus.StateRevision,
                            "幂等：重复加载=空操作成功（投影原样，不新开代际不重复迁移）");
                        Check(ReferenceEquals(InventoryTidyFeatureRegistration.WiredModule, wiredAfterFirst),
                            "幂等：重复加载不经工厂（模块实例不变，无新代际）");

                        // 六项 false→UserDisabled 的真实 e2e 收口：LIT/LIR/network/BII
                        // 在「迁移六项」组，LHT/v1compat 的 false 在此补齐（机解释
                        // Isolated→空操作成功，意图落盘+旧键退役与其余四项同构）。
                        var idemIntents = BueFeatureIntentRuntime.StoreFor(root);
                        Check(idemIntents.HasUserDisabled(lhtFeature)
                                && idemIntents.HasUserDisabled(NetworkModuleAdapter.V1CompatFeature),
                            "六项收口：LHT/v1compat 旧值 false → UserDisabled 意图落盘（Isolated 空操作成功）");
                        bool lhtLegacy;
                        Check(!LegacyDocHasToggle(root, lhtFeature, "hordetracker.enabled", out lhtLegacy),
                            "六项收口：LHT 旧键退役");
                        Check(!LegacyDocHasToggle(root, NetworkModuleAdapter.V1CompatFeature, "v1compat.enabled", out lhtLegacy),
                            "六项收口：v1compat 旧键退役");
                        bool strangerValue;
                        Check(!BueFeatureIntentRuntime.StoreFor(root).HasUserDisabled(strangerFeature),
                            "幂等：未登记功能不迁（生态 enabled 命名不触发字段名扫描）");
                        Check(LegacyDocHasToggle(root, strangerFeature, "myservice.enabled", out strangerValue) && !strangerValue,
                            "幂等：未登记功能的旧文档原样保留");

                        var aliases = BueLegacyEnabledMigrationAdapter.ComposeAliases(root);
                        Check(aliases.Count == 6, "别名表：恰六项显式登记");
                        Check(HasAlias(aliases, InventoryTidyFeatureRegistration.FeatureIdValue, "inventorytidy.enabled")
                                && HasAlias(aliases, InPlaceReloadFeatureRegistration.FeatureIdValue, "inplacereload.enabled")
                                && HasAlias(aliases, HordeTrackerFeatureRegistration.FeatureIdValue, "hordetracker.enabled")
                                && HasAlias(aliases, NetworkModuleAdapter.NetworkFeature.Value, "network.enabled")
                                && HasAlias(aliases, NetworkModuleAdapter.V1CompatFeature.Value, "v1compat.enabled")
                                && HasAlias(aliases, BetterItemInteractionSettingsState.Feature.Value, "Enabled"),
                            "别名表：五宿主旧 `*.enabled` + BII Enabled 显式登记");
                        Check(NotRegistered(aliases, "AutoRotate") && NotRegistered(aliases, "noop.probe-toggle")
                                && !HasAlias(aliases, "io.github.yu80rice.bue.noop", "noop.probe-toggle"),
                            "别名表：BII AutoRotate 与 NoOp probe-toggle 不登记（仍是普通设置）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        BueFeatureIntentRuntime.Clear();
                        try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (IOException) { }
                    }
                });

                // DEV-V4-04：「成功写入新权威前不得丢旧值」落在真实文档路径——
                // 注入真实 FileSettingsPersistence 的一次性替换故障，意图落盘失败
                // →机已停但旧键必须原样保留（自愈前提）；故障解除后下一次加载
                // 自愈：空操作成功→意图落盘→旧键退役。
                Group("legacy 迁移失败保留旧值（真实文档路径）", () =>
                {
                    var root = Path.Combine(Path.GetTempPath(), "bue-v404-failkeep-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(root);
                    var previousHostRuntime = BueRuntimeHost.CurrentRuntime;
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, () => true, null);
                    BueFeatureIntentRuntime.Clear();
                    try
                    {
                        var litFeature = new FeatureId(InventoryTidyFeatureRegistration.FeatureIdValue);
                        WriteLegacyToggleDoc(root, litFeature, "inventorytidy.enabled", false);
                        InventoryTidyFeatureRegistration.WiredModule = null;
                        var failRuntime = new FeatureRegistrationRuntime();
                        BueRuntimeHost.Bind(failRuntime);
                        failRuntime.OpenRegistration();
                        Check(failRuntime.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted, "保留 setup：LIT 登记");
                        Check(failRuntime.CompleteRuntime(), "保留 setup：目录冻结");
                        BueFeatureStartRuntime.StartCatalog(failRuntime, NewLoopbackNetwork(failRuntime.Catalog.CatalogRevision));

                        // 故障注入进组合根：Default（机钩）与 StoreFor（迁移）
                        // 解析同一失败实例=生产拓扑，RecordUserDisabled 真实写败。
                        var failLines = new List<string>();
                        var failingPersistence = new FileSettingsPersistence(root);
                        failingPersistence.FailNextReplace = true;
                        Check(BueFeatureIntentRuntime.EnsureCreatedWith(root, failingPersistence, failLines.Add),
                            "保留 setup：故障库经组合根装配");
                        BueLegacyEnabledMigrationAdapter.Run(root, line => { });

                        FeatureStatusView failStatus;
                        Check(BueFeatureStartRuntime.TryGetStatus(litFeature, out failStatus)
                                && failStatus.State == FeatureState.Stopped && failStatus.StopReason == FeatureStopReason.UserDisabled,
                            "保留 setup：机已解释停用（Running→停）");
                        Check(failLines.Exists(l => l.Contains("event=lifecycle-intent") && l.Contains("result=commit-failed")
                                && l.Contains("diagnosticId=BUE-LIFE-INTENT")),
                            "保留：意图写败有结构化留痕（不静默）");
                        bool failValue;
                        Check(LegacyDocHasToggle(root, litFeature, "inventorytidy.enabled", out failValue) && !failValue,
                            "保留：意图落盘失败 → 旧键仍在文件里（成功前旧值不丢）");
                        Check(!BueFeatureIntentRuntime.StoreFor(root).HasUserDisabled(litFeature),
                            "保留：意图未落盘（无半份权威）");

                        BueLegacyEnabledMigrationAdapter.Run(root, line => { });
                        bool healedValue;
                        Check(!LegacyDocHasToggle(root, litFeature, "inventorytidy.enabled", out healedValue)
                                && BueFeatureIntentRuntime.StoreFor(root).HasUserDisabled(litFeature),
                            "自愈：下次加载空操作成功 → 意图落盘且旧键退役（幂等不新开代际）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        BueRuntimeHost.Bind(previousHostRuntime);
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        BueFeatureIntentRuntime.Clear();
                        try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (IOException) { }
                    }
                });

                // DEV-V4-04：迁移「退役写」自身的失败也走真实文档路径——别名持久
                // 注入 FailNextReplace：机停用成功、意图落盘成功，但旧键改写失败
                // → 旧键必须原样保留（TryCommit 失败字节不动）；故障解除后 honor
                // 路径再清一次（自愈幂等）。
                Group("legacy 迁移退役失败保留旧值（真实文档路径）", () =>
                {
                    var root = Path.Combine(Path.GetTempPath(), "bue-v404-retirefail-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(root);
                    var previousHostRuntime = BueRuntimeHost.CurrentRuntime;
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, () => true, null);
                    BueFeatureIntentRuntime.Clear();
                    BueFeatureIntentRuntime.EnsureCreated(root, null);
                    try
                    {
                        var litFeature = new FeatureId(InventoryTidyFeatureRegistration.FeatureIdValue);
                        WriteLegacyToggleDoc(root, litFeature, "inventorytidy.enabled", false);
                        InventoryTidyFeatureRegistration.WiredModule = null;
                        var retireRuntime = new FeatureRegistrationRuntime();
                        BueRuntimeHost.Bind(retireRuntime);
                        retireRuntime.OpenRegistration();
                        Check(retireRuntime.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted, "退役失败 setup：LIT 登记");
                        Check(retireRuntime.CompleteRuntime(), "退役失败 setup：目录冻结");
                        BueFeatureStartRuntime.StartCatalog(retireRuntime, NewLoopbackNetwork(retireRuntime.Catalog.CatalogRevision));

                        var failingAliasPersistence = new FileSettingsPersistence(root);
                        failingAliasPersistence.FailNextReplace = true;
                        BueLegacyEnabledMigrationAdapter.Run(root, line => { }, failingAliasPersistence);

                        FeatureStatusView retireStatus;
                        Check(BueFeatureStartRuntime.TryGetStatus(litFeature, out retireStatus)
                                && retireStatus.State == FeatureState.Stopped && retireStatus.StopReason == FeatureStopReason.UserDisabled,
                            "退役失败 setup：机已解释停用");
                        Check(BueFeatureIntentRuntime.StoreFor(root).HasUserDisabled(litFeature),
                            "退役失败：意图事实已落盘（新权威在库）");
                        bool retireFailValue;
                        Check(LegacyDocHasToggle(root, litFeature, "inventorytidy.enabled", out retireFailValue) && !retireFailValue,
                            "退役失败：旧键改写失败 → 旧键仍在文件里（退役失败不丢旧值）");

                        BueLegacyEnabledMigrationAdapter.Run(root, line => { });
                        bool healedRetireValue;
                        Check(!LegacyDocHasToggle(root, litFeature, "inventorytidy.enabled", out healedRetireValue)
                                && BueFeatureIntentRuntime.StoreFor(root).HasUserDisabled(litFeature),
                            "退役失败自愈：honor 路径再清一次 → 旧键退役（幂等）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        BueRuntimeHost.Bind(previousHostRuntime);
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        BueFeatureIntentRuntime.Clear();
                        try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (IOException) { }
                    }
                });
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V3-03 lifecycle projection collection: ALL GREEN (0 failures)");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V3-03 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V3-03: structured-diagnostic probe over the captured runtime log
        // lines (the Recorder output carries a level prefix, so the match is a
        // substring probe).
        private static bool ContainsDiagnostic(List<string> captured, string token)
        {
            for (var i = 0; i < captured.Count; i++)
            {
                if (captured[i].IndexOf(token, StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        // DEV-V4-04：旧世界落盘形状——schema-1 ClientLocal toggle 文档（升级前
        // 五个官方功能的唯一 facet）。迁移引擎的输入契约。
        private static void WriteLegacyToggleDoc(string root, FeatureId feature, string settingId, bool value)
        {
            var descriptor = new SettingDescriptor(feature, settingId, settingId, settingId, SettingKind.Toggle,
                SettingAuthority.ClientLocal, SettingValue.Toggle(true),
                default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                new SettingValue[0], 0, string.Empty, 1, 0, string.Empty, string.Empty);
            var runtime = new SettingsRuntime(feature, new[] { descriptor }, new FileSettingsPersistence(root));
            // true 与默认值同形是不落盘的 no-op 提交——两段提交保证最终值落盘，
            // 第二段以第一段返回的实际 revision 为基准（no-op 时 revision 不推进）。
            var first = runtime.Submit(new ScopedSettingChangeRequest(1UL, SettingRevisionScope.ClientPreference, 0,
                new[] { new SettingMutation(settingId, SettingValue.Toggle(!value)) }));
            Assert(first.Accepted, "setup: legacy toggle doc write (stage 1) for " + feature.Value);
            var result = runtime.Submit(new ScopedSettingChangeRequest(2UL, SettingRevisionScope.ClientPreference, first.Revision,
                new[] { new SettingMutation(settingId, SettingValue.Toggle(value)) }));
            Assert(result.Accepted, "setup: legacy toggle doc write for " + feature.Value + ":" + result.Error);
        }

        // DEV-V4-04：直接读旧文档（退役后 facet 不再声明该键，只有迁移 adapter 以
        // 显式别名方式读它）——返回键是否存在 + 当前布尔值。
        private static bool LegacyDocHasToggle(string root, FeatureId feature, string settingId, out bool value)
        {
            value = false;
            var loaded = new FileSettingsPersistence(root).Load(feature, SettingRevisionScope.ClientPreference, 1, null);
            if (!loaded.IsValid) return false;
            SettingValue raw;
            if (!loaded.Values.TryGetValue(settingId, out raw) || raw.Kind != SettingKind.Toggle) return false;
            value = raw.Boolean;
            return true;
        }

        private static bool HasAlias(IReadOnlyList<LegacyEnabledAlias> aliases, string featureValue, string settingId)
        {
            for (var i = 0; i < aliases.Count; i++)
                if (aliases[i].Feature.Value == featureValue && aliases[i].LegacySettingId == settingId) return true;
            return false;
        }

        private static bool NotRegistered(IReadOnlyList<LegacyEnabledAlias> aliases, string settingId)
        {
            for (var i = 0; i < aliases.Count; i++)
                if (aliases[i].LegacySettingId == settingId) return false;
            return true;
        }

        // DEV-V3-04: BueNetwork 传输规则与主线程投递。行为面冻结：平台每会话
        // 发送预算（窗口 2000ms/256 条，本票定值，超出=显式拒绝不静默丢弃）、
        // 链路健康电平（连续失败 10 恰一条 degraded、恢复恰一条 recovered+清零、
        // 每会话代际独立）、入站 handler 异常结构化诊断（不扩散不打穿泵）、
        // DeferredBueNetworkApi 回放失败四态投影（not-ready/detached/
        // replay-failed/transport-unavailable，禁空 catch 无痕折叠）。
        // Round-1 子组全部只依赖既有可编译 API（经 NetworkModuleAdapter 的
        // DiagnosticLogSink 观察诊断行）——观测红；Round-2 子组（矩阵两侧、
        // dispatcher 最小行为面、LIR 官方先行消费锚、NoOp 对照、未知结果值）
        // 引用本票新面——编译红。
        private static void AssertBueV3NetworkTransportRules(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                Group("发送预算超限显式拒绝", () => NetworkV3GroupBudgetFirstReject(Check));
                Group("预算恢复与跨代际清零", () => NetworkV3GroupBudgetRecoveryAndGenerations(Check));
                Group("组播聚合预算", () => NetworkV3GroupMulticastAggregation(Check));
                Group("官方生态同权预算", () => NetworkV3GroupEqualRightsBudget(Check));
                Group("链路健康电平", () => NetworkV3GroupLinkHealth(Check));
                Group("入站 handler 异常诊断", () => NetworkV3GroupInboundHandlerError(Check));
                Group("回放失败投影四态", () => NetworkV3GroupDeferredProjections(Check));
                Group("未知结果旧模块安全降级", () => NetworkV3GroupUnknownResultSafeDegrade(Check));
                Group("矩阵 MainThread 接线两侧", () => NetworkV3GroupMatrixMainThread(Check));
                Group("dispatcher 最小行为面", () => NetworkV3GroupDispatcherSeam(Check));
                Group("LIR 官方先行消费锚", () => NetworkV3GroupLirDispatcherConsumption(Check));
                Group("LIR 空闲不投递（DEV-V3-09 日志风暴修复）", () => NetworkV3GroupLirIdleDrainNoPost(Check));
                Group("NoOp 生态对照", () => NetworkV3GroupNoOpMainThread(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V3-04 network transport rules collection: ALL GREEN (0 failures) — groups: 发送预算超限显式拒绝/预算恢复与跨代际清零/组播聚合预算/官方生态同权预算/链路健康电平/入站 handler 异常诊断/回放失败投影四态/未知结果旧模块安全降级/矩阵 MainThread 接线两侧/dispatcher 最小行为面/LIR 官方先行消费锚/NoOp 生态对照");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V3-04 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V3-04 ticket-fixed budget numbers (asserted as behavior; the
        // internal consts are Core-internal and not visible to this project).
        private const int V3SendBudget = 256;
        private const long V3BudgetWindowMs = 2000;
        private const int V3LinkDegradationThreshold = 10;

        /// <summary>DEV-V3-04 fixture: a server-role NetworkModuleAdapter over
        /// the injectable FakeBueEngine + fake monotonic clock, with every
        /// diagnostic line captured through the existing DiagnosticLogSink
        /// seam (residual-safe restore). Sessions establish through the frozen
        /// responder path (consumed Hello → TickNetwork → established).</summary>
        private sealed class NetworkV3Fixture
        {
            internal NetworkModuleAdapter Adapter;
            internal FakeBueEngine Engine;
            internal List<string> Lines;
            internal long ClockMs = 1000000L;
            private Action<string> previousSink;

            internal static NetworkV3Fixture Create(string name, bool arm = true)
            {
                var fx = new NetworkV3Fixture();
                fx.Lines = new List<string>();
                fx.previousSink = NetworkModuleAdapter.DiagnosticLogSink;
                NetworkModuleAdapter.DiagnosticLogSink = line => fx.Lines.Add(line);
                fx.Engine = new FakeBueEngine { IsServer = true, LocalSteamId = 7001UL, Clock = () => fx.ClockMs };
                fx.Adapter = new NetworkModuleAdapter(
                    Path.Combine(Path.GetTempPath(), "bue-v3net-" + name + "-" + Guid.NewGuid().ToString("N")),
                    () => false, () => null, () => null, () => { }, null, null, fx.Engine.ToBinding());
                fx.Adapter.ActivateCore();
                if (arm) fx.Adapter.TickNetwork();
                return fx;
            }

            internal void AdvanceMs(long ms) { ClockMs += ms; }

            /// <summary>The frozen responder establishment path: consume a peer
            /// Hello, pump, and return the established session to that peer.</summary>
            internal IConnectionSession Establish(ulong peer, ulong nonce)
            {
                var hello = BuildBue1HelloFrame(peer, 2, 0, nonce);
                Adapter.ShouldConsumeInbound(true, peer, hello, 0, hello.Length, null);
                Adapter.TickNetwork();
                var snapshot = Adapter.NetworkApi.Sessions;
                for (var i = 0; i < snapshot.Count; i++)
                {
                    if (snapshot[i].PeerSteamId == peer) return snapshot[i];
                }
                return null;
            }

            internal int CountLine(string token)
            {
                return CountToken(Lines, token);
            }

            internal void Dispose()
            {
                try { Adapter.IsolateAndDetach(); }
                catch (Exception) { }
                NetworkModuleAdapter.DiagnosticLogSink = previousSink;
            }
        }

        private static void NetworkV3GroupBudgetFirstReject(System.Action<bool, string> check)
        {
            var fx = NetworkV3Fixture.Create("v3budget");
            try
            {
                var channel = new FeatureId("io.example.v3budget");
                var api = fx.Adapter.NetworkApi;
                check(api != null && api.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted,
                    "setup: 服务端运行时武装且频道注册成功");
                var session = fx.Establish(1001UL, 9001UL);
                check(session != null, "setup: 响应方会话经 Hello 建立");
                var burstOk = true;
                for (var i = 0; i < V3SendBudget; i++)
                {
                    if (api.SendToClient(channel, session, new byte[] { 1 }, true) != NetworkSendResult.Sent) { burstOk = false; break; }
                }
                check(burstOk, "预算：窗内前 " + V3SendBudget + " 条全部 Sent（保底不误伤正常流量）");
                var over = api.SendToClient(channel, session, new byte[] { 2 }, true);
                check(over != NetworkSendResult.Sent
                        && over != NetworkSendResult.LocalTransportUnavailable
                        && over != NetworkSendResult.PayloadTooLarge,
                    "预算：第 " + (V3SendBudget + 1) + " 条被显式拒绝（平台节流自有结果值——非 Sent、非传输失败、非超限）");
                var throttled = fx.CountLine("event=network-send result=throttled");
                check(throttled >= 1, "预算：节流拒绝浮出结构化诊断行（不静默丢弃）");
                var line = fx.Lines.Find(l => l.IndexOf("event=network-send result=throttled", StringComparison.Ordinal) >= 0);
                check(line != null && line.Contains("diagnosticId=BUE-NET-001")
                        && line.Contains("generation=" + session.SessionId) && line.Contains(channel.Value),
                    "预算：诊断行携带身份码与代际/频道（可定位「谁在哪个会话被限」）");
            }
            finally { fx.Dispose(); }
        }

        private static void NetworkV3GroupBudgetRecoveryAndGenerations(System.Action<bool, string> check)
        {
            var fx = NetworkV3Fixture.Create("v3budgetgen");
            try
            {
                var channel = new FeatureId("io.example.v3budgetgen");
                var api = fx.Adapter.NetworkApi;
                check(api.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted, "setup: 频道注册");
                var sessionA = fx.Establish(1001UL, 9101UL);
                check(sessionA != null, "setup: 会话 A 建立");
                for (var i = 0; i <= V3SendBudget; i++)
                {
                    api.SendToClient(channel, sessionA, new byte[] { 1 }, true);
                }
                check(api.SendToClient(channel, sessionA, new byte[] { 1 }, true) != NetworkSendResult.Sent,
                    "setup: A 当前窗口预算已耗尽");
                // 窗口滑出：新窗口恢复放行
                fx.AdvanceMs(V3BudgetWindowMs + 1);
                check(api.SendToClient(channel, sessionA, new byte[] { 1 }, true) == NetworkSendResult.Sent,
                    "预算：滑出 " + V3BudgetWindowMs + "ms 窗口后恢复发送（保底限流非永久封禁）");
                // 跨代际清零：A 的代际断开（ServerPeers 摘除→泵差分）后新代际重连
                for (var i = 0; i <= V3SendBudget; i++)
                {
                    api.SendToClient(channel, sessionA, new byte[] { 2 }, true);
                }
                fx.Engine.ServerPeers.Add(1001UL);
                fx.Adapter.TickNetwork(); // peer 进入引擎快照（无操作，仅登记）
                fx.Engine.ServerPeers.Clear();
                fx.Adapter.TickNetwork(); // 快照差分 → PeerDisconnected → 运行时清会话
                check(api.Sessions.Count == 0, "setup: 代际断开后会话快照清空");
                var sessionB = fx.Establish(1001UL, 9102UL);
                check(sessionB != null && sessionB.SessionId != sessionA.SessionId, "setup: 同一对等新代际重连");
                check(api.SendToClient(channel, sessionB, new byte[] { 1 }, true) == NetworkSendResult.Sent,
                    "预算：预算状态随 ConnectionGeneration 隔离——新代际首条即放行（跨代际清零）");
                check(api.SendToClient(channel, sessionA, new byte[] { 1 }, true) == NetworkSendResult.NoSession,
                    "预算：旧代际对象已死，寻址拒绝（既有冻结语义回归）");
            }
            finally { fx.Dispose(); }
        }

        private static void NetworkV3GroupMulticastAggregation(System.Action<bool, string> check)
        {
            var fx = NetworkV3Fixture.Create("v3mcast");
            try
            {
                var channel = new FeatureId("io.example.v3mcast");
                var api = fx.Adapter.NetworkApi;
                check(api.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted, "setup: 频道注册");
                var sessionA = fx.Establish(1001UL, 9201UL);
                var sessionB = fx.Establish(1002UL, 9202UL);
                check(sessionA != null && sessionB != null && api.Sessions.Count == 2, "setup: 双会话建立");
                // 全目标正常：聚合 Sent（既有冻结语义回归锚）
                check(api.SendToClients(channel, new byte[] { 1 }, true) == NetworkSendResult.Sent,
                    "聚合：全目标送达仍 Sent（既有语义回归）");
                // 只耗尽 A 的预算（逐目标寻址），B 保持余量
                for (var i = 0; i <= V3SendBudget; i++)
                {
                    api.SendToClient(channel, sessionA, new byte[] { 1 }, true);
                }
                var mixed = api.SendToClients(channel, new byte[] { 2 }, true);
                check(mixed != NetworkSendResult.Sent && mixed != NetworkSendResult.LocalTransportUnavailable,
                    "聚合：一目标被节流未执行、一目标送达——不得静默报全 Sent，也不是传输失败");
                check(api.SendToClients(channel, new byte[] { 3 }, true) != NetworkSendResult.Sent,
                    "聚合：持续超限的组播保持显式拒绝（不静默丢弃）");
                check(fx.CountLine("event=network-send result=throttled") >= 2,
                    "聚合：被节流的每个目标都有结构化诊断（逐目标可定位）");
            }
            finally { fx.Dispose(); }
        }

        private static void NetworkV3GroupEqualRightsBudget(System.Action<bool, string> check)
        {
            var fx = NetworkV3Fixture.Create("v3parity");
            try
            {
                var ecoChannel = new FeatureId("io.example.v3parity");
                var officialChannel = new FeatureId("io.github.yu80rice.bue.inventory-tidy");
                var api = fx.Adapter.NetworkApi;
                check(api.RegisterChannel(ecoChannel, new ContractVersion(2, 0), 1).Accepted, "setup: 生态频道注册");
                check(api.RegisterChannel(officialChannel, new ContractVersion(2, 0), 1).Accepted, "setup: 官方身份频道注册");
                var session = fx.Establish(1001UL, 9301UL);
                check(session != null, "setup: 会话建立");
                // 生态频道把该会话预算打满
                for (var i = 0; i <= V3SendBudget; i++)
                {
                    api.SendToClient(ecoChannel, session, new byte[] { 1 }, true);
                }
                // 官方身份在同一会话同一窗口的首条同样被限（无身份豁免）
                var official = api.SendToClient(officialChannel, session, new byte[] { 1 }, true);
                check(official != NetworkSendResult.Sent,
                    "同权：官方身份频道在预算耗尽的会话上同样被节流（官方无私有捷径）");
                check(official != NetworkSendResult.LocalTransportUnavailable && official != NetworkSendResult.PayloadTooLarge,
                    "同权：官方频道的拒绝同样是显式平台节流结果（非伪装传输失败）");
                check(fx.CountLine("event=network-send result=throttled channel=" + officialChannel.Value) >= 1,
                    "同权：官方频道的节流拒绝浮出自身诊断行（逐频道可定位）");
            }
            finally { fx.Dispose(); }
        }

        private static int CountGenerationLines(List<string> lines, string eventToken, ulong generation)
        {
            var count = 0;
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].IndexOf(eventToken, StringComparison.Ordinal) >= 0
                    && lines[i].IndexOf("generation=" + generation, StringComparison.Ordinal) >= 0) count++;
            }
            return count;
        }

        private static void NetworkV3GroupLinkHealth(System.Action<bool, string> check)
        {
            var fx = NetworkV3Fixture.Create("v3health");
            try
            {
                var channel = new FeatureId("io.example.v3health");
                var api = fx.Adapter.NetworkApi;
                check(api.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted, "setup: 频道注册");
                var sessionA = fx.Establish(1001UL, 9401UL);
                var sessionB = fx.Establish(1002UL, 9402UL);
                check(sessionA != null && sessionB != null, "setup: 双会话建立");
                fx.Engine.SendOverride = (frame, reliable, target) => false; // 传输持续失败
                for (var i = 1; i < V3LinkDegradationThreshold; i++)
                {
                    check(api.SendToClient(channel, sessionA, new byte[] { 1 }, true) == NetworkSendResult.LocalTransportUnavailable,
                        "健康：失败发送如实返回传输不可达（既有结果语义回归）");
                }
                check(CountGenerationLines(fx.Lines, "event=network-link result=degraded", sessionA.SessionId) == 0,
                    "健康：阈值前不上抛 degraded（第 1..9 条零电平诊断，不逐帧刷屏）");
                api.SendToClient(channel, sessionA, new byte[] { 1 }, true);
                check(CountGenerationLines(fx.Lines, "event=network-link result=degraded", sessionA.SessionId) == 1,
                    "健康：第 " + V3LinkDegradationThreshold + " 条连续失败上抛恰一条 degraded（电平）");
                var degradedLine = fx.Lines.Find(l => l.Contains("event=network-link result=degraded") && l.Contains("generation=" + sessionA.SessionId));
                check(degradedLine != null && degradedLine.Contains("diagnosticId=BUE-NET-002")
                        && degradedLine.Contains("consecutiveFailures=" + V3LinkDegradationThreshold),
                    "健康：degraded 行携带身份码/末次结果/连续失败计数（定位「发送持续失败」是链路问题）");
                for (var i = 0; i < 20; i++)
                {
                    api.SendToClient(channel, sessionA, new byte[] { 1 }, true);
                }
                check(CountGenerationLines(fx.Lines, "event=network-link result=degraded", sessionA.SessionId) == 1,
                    "健康：episode 内 degraded 始终恰一条（电平式不炸帧）");
                check(CountGenerationLines(fx.Lines, "event=network-link result=degraded", sessionB.SessionId) == 0,
                    "健康：每会话代际独立——B 的链路不受 A 劣化影响");
                fx.Engine.SendOverride = null; // 传输恢复
                check(api.SendToClient(channel, sessionB, new byte[] { 1 }, true) == NetworkSendResult.Sent,
                    "setup: B 正常送达");
                check(CountGenerationLines(fx.Lines, "event=network-link result=recovered", sessionB.SessionId) == 0,
                    "健康：未劣化过的会话成功不产 recovered（电平无跳变）");
                check(api.SendToClient(channel, sessionA, new byte[] { 1 }, true) == NetworkSendResult.Sent,
                    "setup: A 恢复送达");
                check(CountGenerationLines(fx.Lines, "event=network-link result=recovered", sessionA.SessionId) == 1,
                    "健康：恢复后上抛恰一条 recovered");
                var recoveredLine = fx.Lines.Find(l => l.Contains("event=network-link result=recovered") && l.Contains("generation=" + sessionA.SessionId));
                check(recoveredLine != null && recoveredLine.Contains("diagnosticId=BUE-NET-003"),
                    "健康：recovered 行携带身份码（与 degraded 成对呈现）");
                fx.Engine.SendOverride = (frame, reliable, target) => false;
                for (var i = 1; i <= V3LinkDegradationThreshold; i++)
                {
                    api.SendToClient(channel, sessionA, new byte[] { 1 }, true);
                }
                check(CountGenerationLines(fx.Lines, "event=network-link result=degraded", sessionA.SessionId) == 2,
                    "健康：recovered 清零后再满阈值重开新 episode（阈值重新累计）");
                check(fx.CountLine("event=network-link") == 3,
                    "健康：全程电平诊断恰 3 条（2 degraded + 1 recovered，无逐帧噪音——episode 数=电平跳变数）");
            }
            finally { fx.Dispose(); }
        }

        private static void NetworkV3GroupInboundHandlerError(System.Action<bool, string> check)
        {
            var fx = NetworkV3Fixture.Create("v3inbound");
            try
            {
                var channel = new FeatureId("io.example.v3inbound");
                var api = fx.Adapter.NetworkApi;
                check(api.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted, "setup: 频道注册");
                var session = fx.Establish(1001UL, 9501UL);
                check(session != null, "setup: 会话建立");
                var received = new List<byte[]>();
                api.Subscribe(channel, ChannelDirection.FromClients, (s, payload) => throw new InvalidOperationException("v3-handler-fault"));
                api.Subscribe(channel, ChannelDirection.FromClients, (s, payload) => received.Add(payload));
                var dataFrame = BuildBue1Frame(channel.Value, 1001UL, new byte[] { 0x2A });
                fx.Adapter.ShouldConsumeInbound(true, 1001UL, dataFrame, 0, dataFrame.Length, null);
                var pumpThrew = false;
                try { fx.Adapter.TickNetwork(); }
                catch (Exception) { pumpThrew = true; }
                check(!pumpThrew, "入站：handler 异常不打穿传输泵（既有隔离语义回归）");
                check(received.Count == 1, "入站：异常 handler 不扩散其他订阅者（既有隔离语义回归）");
                check(fx.CountLine("event=network-inbound result=handler-error") == 1,
                    "入站：handler 异常从静默吞改为结构化诊断（一异常一行，可查）");
                var line = fx.Lines.Find(l => l.IndexOf("event=network-inbound result=handler-error", StringComparison.Ordinal) >= 0);
                check(line != null && line.Contains("diagnosticId=BUE-NET-004")
                        && line.Contains("channel=" + channel.Value)
                        && line.Contains("generation=" + session.SessionId)
                        && line.Contains("errorType=InvalidOperationException"),
                    "入站：诊断行携带模块/频道/方向/代际/异常类型（T5 冻结字段）");
                fx.Adapter.ShouldConsumeInbound(true, 1001UL, dataFrame, 0, dataFrame.Length, null);
                fx.Adapter.TickNetwork();
                check(received.Count == 2, "入站：异常后订阅表完好，下一帧照常派发（泵线程存活）");
                check(fx.CountLine("event=network-inbound result=handler-error") == 2,
                    "入站：诊断逐次如实（每异常一行，不折叠不静默）");
            }
            finally { fx.Dispose(); }
        }

        private static void NetworkV3GroupDeferredProjections(System.Action<bool, string> check)
        {
            // (a)(b)(c) 走 NetworkModuleAdapter 既有 DiagnosticLogSink 观察缝。
            var fx = NetworkV3Fixture.Create("v3defer", arm: false);
            try
            {
                var channel = new FeatureId("io.example.v3defer");
                var badChannel = new FeatureId("io.example.v3defer-future");
                var facade = fx.Adapter.FeatureNetworkApi;
                check(facade != null, "setup: 适配器 feature 门面存在");
                check(facade.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted, "setup: 频道延迟受理");
                check(facade.RegisterChannel(badChannel, new ContractVersion(3, 0), 1).Accepted,
                    "setup: 未来合同频道在延迟层同样先受理（未就绪不是拒绝）");
                for (var i = 0; i < 10; i++)
                {
                    check(facade.SendToServer(channel, new byte[] { 1 }, true) == NetworkSendResult.NoSession,
                        "回放：未就绪发送=显式 NoSession（既有结果语义，不静默）");
                }
                check(fx.CountLine("event=network-deferred result=not-ready") == 1,
                    "回放：未就绪态浮出结构化诊断且每 episode 恰一条（10 次发送不刷屏）");
                fx.Adapter.TickNetwork(); // 先武装（未武装时 BUE 帧交还不消费，与 DEV-V2-18 语义一致）
                var session = fx.Establish(1001UL, 9601UL);
                check(session != null, "setup: 门面接线后（TickNetwork 武装）会话建立");
                check(fx.CountLine("event=network-deferred result=replay-failed") >= 1,
                    "回放：坏频道重放被拒不再无痕——replay-failed 投影（Attach 重放失败可查）");
                var replayLine = fx.Lines.Find(l => l.IndexOf("event=network-deferred result=replay-failed", StringComparison.Ordinal) >= 0);
                check(replayLine != null && replayLine.Contains("diagnosticId=BUE-NET-005")
                        && replayLine.Contains(badChannel.Value) && replayLine.Contains("stage=replay-register"),
                    "回放：投影行携带身份码/频道/阶段（与「未就绪」可区分）");
                check(facade.SendToServer(channel, new byte[] { 1 }, true) == NetworkSendResult.Sent,
                    "回放：接线后好频道重放可用（正向回归）");
                fx.Adapter.IsolateAndDetach();
                check(facade.SendToServer(channel, new byte[] { 1 }, true) == NetworkSendResult.NoSession,
                    "回放：detach 后发送=显式 NoSession（既有语义）");
                check(fx.CountLine("event=network-deferred result=detached") == 1,
                    "回放：模块停止/撤接线态与「从未就绪」可区分（detached 投影）");
            }
            finally { fx.Dispose(); }
            // (d) 节流不计入链路失败（预算与健康的边界不相串）。
            var fx2 = NetworkV3Fixture.Create("v3thrtl-no-fail");
            try
            {
                var channel = new FeatureId("io.example.v3thrtl");
                var api = fx2.Adapter.NetworkApi;
                check(api.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted, "setup: 频道注册");
                var session = fx2.Establish(1001UL, 9701UL);
                check(session != null, "setup: 会话建立");
                var throttles = 0;
                for (var i = 0; i < V3SendBudget + 30; i++)
                {
                    var r = api.SendToClient(channel, session, new byte[] { 1 }, true);
                    if (r != NetworkSendResult.Sent) throttles++;
                }
                check(throttles > 0, "setup: 窗口内已出现节流拒绝");
                check(CountGenerationLines(fx2.Lines, "event=network-link result=degraded", session.SessionId) == 0,
                    "健康：Throttled 不计入连续失败（平台节流≠传输失败，链路电平不被误触发）");
            }
            finally { fx2.Dispose(); }
            // (e) 第四态（R1-Spec Gap1 补锚）：接线后的活运行时从门面底下抛=
            // transport-unavailable——直接以门面+抛错假运行时驱动（生产运行时
            // 热路径不抛，此分支是门面对「底下真传输会抛」的防御契约），
            // 且与前三态 token 互不混淆（四态区分完整）。
            var facadeLines = new List<string>();
            var directFacade = new BetterUnturnedExperience.Core.Network.DeferredBueNetworkApi(facadeLines.Add);
            directFacade.Attach(new NetworkV3ThrowingNetwork());
            var thrown = directFacade.SendToServer(new FeatureId("io.example.v3defer-throw"), new byte[] { 1 }, true);
            check(thrown == NetworkSendResult.LocalTransportUnavailable,
                "回放：活运行时抛出被门面收住=显式 LocalTransportUnavailable（异常不逃逸功能边界）");
            check(CountToken(facadeLines, "event=network-deferred result=transport-unavailable") == 1
                    && CountToken(facadeLines, "stage=send-to-server") == 1,
                "回放：transport-unavailable 投影恰一条（stage 定位发送方法）");
            check(!ContainsDiagnostic(facadeLines, "result=not-ready") && !ContainsDiagnostic(facadeLines, "result=detached"),
                "回放：transport-unavailable 与未就绪/模块停止两态互斥（可区分）");
            var sessionsCount = -1;
            var sessionsThrew = false;
            try { sessionsCount = directFacade.Sessions.Count; }
            catch (Exception) { sessionsThrew = true; }
            check(!sessionsThrew && sessionsCount == 0 && CountToken(facadeLines, "stage=sessions") >= 1,
                "回放：Sessions 读取抛出同样投影（异常被门面收住=空快照+诊断，不逃逸不静默）");
        }

        /// <summary>DEV-V3-04 deferred-projection stub: a live IBueNetworkApi
        /// whose send/sessions calls throw — the 「门面底下会抛」 shape the
        /// transport-unavailable branch defends (the real runtime's hot paths
        /// never throw; a future engine-bound transport can).</summary>
        private sealed class NetworkV3ThrowingNetwork : IBueNetworkApi
        {
            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "THROWING"); }
            public bool UnregisterChannel(FeatureId channel) { return true; }
            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            { return new NullHandle(); }
            public IReadOnlyList<IConnectionSession> Sessions { get { throw new InvalidOperationException("v3-sessions-fault"); } }
            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable) { throw new InvalidOperationException("v3-send-fault"); }
            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable) { throw new InvalidOperationException("v3-send-fault"); }
            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable) { throw new InvalidOperationException("v3-send-fault"); }
            private sealed class NullHandle : IDisposable { public void Dispose() { } }
        }

        // 2.0 旧模块（枚举里还没有 Throttled 值）面对新结果必须安全降级：
        // 只有显式 Sent 算成功，其余（含未知默认分支）保守处理、不静默当成功。
        private static bool LegacyModuleTreatsAsSuccess(NetworkSendResult result)
        {
            switch (result)
            {
                case NetworkSendResult.Sent: return true;
                case NetworkSendResult.ChannelNotRegistered:
                case NetworkSendResult.NoSession:
                case NetworkSendResult.PeerUnreachable:
                case NetworkSendResult.PayloadTooLarge:
                case NetworkSendResult.PartialFailure:
                case NetworkSendResult.LocalTransportUnavailable: return false;
                default: return false; // 旧模块的未知值保守分支（SDK 纪律：不得把未知枚举当成功）
            }
        }

        private static void NetworkV3GroupUnknownResultSafeDegrade(System.Action<bool, string> check)
        {
            var fx = NetworkV3Fixture.Create("v3legacy");
            try
            {
                var channel = new FeatureId("io.example.v3legacy");
                var api = fx.Adapter.NetworkApi;
                check(api.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted, "setup: 频道注册");
                var session = fx.Establish(1001UL, 9801UL);
                check(session != null, "setup: 会话建立");
                for (var i = 0; i < V3SendBudget; i++)
                {
                    api.SendToClient(channel, session, new byte[] { 1 }, true);
                }
                var over = api.SendToClient(channel, session, new byte[] { 1 }, true);
                check(over == NetworkSendResult.Throttled,
                    "降级：平台节流=新加性结果值 Throttled（本票冻结 (ushort)=205，不复用既有值）");
                check(!LegacyModuleTreatsAsSuccess(over),
                    "降级：旧模块形状对 Throttled 保守处理（未知/新结果不当成功）");
                var values = (Array)Enum.GetValues(typeof(NetworkSendResult));
                var seen205 = 0;
                for (var i = 0; i < values.Length; i++)
                {
                    if ((ushort)values.GetValue(i) == 205) seen205++;
                }
                check(seen205 == 1, "降级：Throttled 的数值 205 在结果集中唯一（无值复用）");
            }
            finally { fx.Dispose(); }
        }

        private static void NetworkV3GroupMatrixMainThread(System.Action<bool, string> check)
        {
            // 接线后侧：真实 StartCatalog 组合把 MainThread 装配非 null，且投递
            // 经宿主统一泵执行（禁自建泵的平台侧兑现=泵在宿主 Update 链上）。
            var probe = new MatrixProbeRegistration("io.example.matrix-mt");
            var probeRuntime = new FeatureRegistrationRuntime();
            probeRuntime.OpenRegistration();
            check(probeRuntime.Register(probe).Accepted, "setup: 矩阵探针受理");
            check(probeRuntime.CompleteRuntime(), "setup: 探针目录冻结");
            var previousRuntime = BueRuntimeHost.CurrentRuntime;
            BueRuntimeHost.Bind(probeRuntime);
            try
            {
                BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(9901));
                var captured = probe.Module.Bootstrap;
                check(captured != null && captured.MainThread != null,
                    "矩阵：真实 StartCatalog 组装 MainThread 视图（DEV-V3-04 后非 null，可用性矩阵行兑现）");
                var ran = false;
                var posted = captured.MainThread.Post(() => ran = true);
                check(posted.Posted && posted.Reason == MainThreadPostReason.None && posted.DiagnosticId == "BUE-MT-ACCEPT",
                    "矩阵：探针经视图投递=显式成功结果（BUE-MT-ACCEPT）");
                check(!ran, "矩阵：fire-and-forget——投递返回时任务尚未执行");
                BueMainThreadRuntime.Dispatcher.Pump(); // 宿主统一泵（BueRuntimeTickChain 同缝）
                check(ran, "矩阵：宿主泵拍执行投递的任务（生态作者不需要自建泵）");
                // 宿主停止：投递显式失败（阶段红线：停止/隔离/宿主停止后不再受理）
                BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                var afterStop = captured.MainThread.Post(() => ran = false);
                check(!afterStop.Posted && afterStop.Reason == MainThreadPostReason.GenerationInvalid
                        && afterStop.DiagnosticId == "BUE-MT-002",
                    "矩阵：宿主停止后投递=显式失败+诊断（不静默吞）");
                // 接线前基线侧：既有手工组装（未传主线程视图=旧宿主形态）成员为 null
                var manual = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, null, null, null, null, null, null, null,
                    NewLoopbackNetwork(9902));
                check(manual.MainThread == null,
                    "矩阵：未接线组装的 MainThread=null（阶段基线侧——模块须容忍 null，接线前语义保持）");
            }
            finally
            {
                BueRuntimeHost.Bind(previousRuntime);
                BueMainThreadRuntime.Clear();
            }
        }

        private static void NetworkV3GroupNoOpMainThread(System.Action<bool, string> check)
        {
            var probeRuntime = new FeatureRegistrationRuntime();
            probeRuntime.OpenRegistration();
            check(probeRuntime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted, "setup: NoOp 样例受理");
            check(probeRuntime.CompleteRuntime(), "setup: NoOp 目录冻结");
            var previousRuntime = BueRuntimeHost.CurrentRuntime;
            BueRuntimeHost.Bind(probeRuntime);
            try
            {
                BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(9911));
                var probe = NoOpFeatureRegistration.LastProbe;
                check(probe != null && probe.Started && probe.MainThreadAvailable,
                    "生态对照：NoOp probe 在真实宿主组装下观察到 MainThread 非 null（矩阵行生态侧）");
                check(probe.MainThreadPosted,
                    "生态对照：probe 经 MainThread 投递获显式受理（投递缝生态侧消费；全链 probe 归 DEV-V3-08）");
                BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
            }
            finally
            {
                BueRuntimeHost.Bind(previousRuntime);
                BueMainThreadRuntime.Clear();
            }
        }

        private static void NetworkV3GroupDispatcherSeam(System.Action<bool, string> check)
        {
            var feature = new FeatureId("io.example.v3dispatcher");
            var diagnostics = new List<string>();
            var dispatcher = new BetterUnturnedExperience.Core.Dispatch.MainThreadDispatcherRuntime(diagnostics.Add);
            dispatcher.OpenGeneration(feature, 1UL);
            var view = dispatcher.CreateView(feature, 1UL);

            // 恰一个投递方法 + 单向 fire-and-forget（返回显式结果，无等待句柄）
            var methods = typeof(IFeatureMainThread).GetMethods();
            check(methods.Length == 1 && methods[0].Name == "Post"
                    && methods[0].ReturnType == typeof(MainThreadPostResult)
                    && methods[0].GetParameters()[0].ParameterType == typeof(System.Action),
                "dispatcher：契约面恰一个投递方法 Post(Action)→MainThreadPostResult（无句柄/无 Task 返回）");

            // FIFO 顺序执行
            var order = new List<int>();
            check(view.Post(() => order.Add(1)).Posted && view.Post(() => order.Add(2)).Posted && view.Post(() => order.Add(3)).Posted,
                "dispatcher：投递受理返回显式成功");
            dispatcher.Pump();
            check(order.Count == 3 && order[0] == 1 && order[1] == 2 && order[2] == 3,
                "dispatcher：FIFO 顺序在主线程泵拍执行");

            // 容量上限：256 受理、第 257 条显式拒绝+诊断（不静默丢弃）
            var accepted = 0;
            MainThreadPostResult overflow = default(MainThreadPostResult);
            for (var i = 0; i < 300; i++)
            {
                var r = view.Post(() => { });
                if (r.Posted) accepted++;
                else { overflow = r; break; }
            }
            check(accepted == 256, "dispatcher：队列容量=256（本票定值，可观察）");
            check(!overflow.Posted && overflow.Reason == MainThreadPostReason.CapacityExceeded && overflow.DiagnosticId == "BUE-MT-001",
                "dispatcher：超限=显式容量拒绝（第三态与成功/失效可区分）");
            check(diagnostics.Exists(l => l.Contains("event=main-thread") && l.Contains("result=post-rejected")
                    && l.Contains("reason=capacity-exceeded") && l.Contains("diagnosticId=BUE-MT-001")),
                "dispatcher：容量拒绝浮出结构化诊断（不静默丢）");
            // 每拍至多执行 32——清空 256 积压需 8 拍（8×32）
            for (var drainBeat = 0; drainBeat < 7; drainBeat++) dispatcher.Pump();
            check(dispatcher.PendingCount == 256 - 7 * 32,
                "dispatcher：积压按每拍 32 递减（7 拍后恰余 " + (256 - 7 * 32) + "）");
            dispatcher.Pump();
            check(dispatcher.PendingCount == 0, "dispatcher：第 8 拍清空积压");
            check(view.Post(() => { }).Posted, "dispatcher：泵后队列腾出容量恢复受理");

            // 每拍执行上限=32（宿主帧预算受控，剩余排后拍）——干净队列上复测
            dispatcher.Pump(); // 执行上一行投递
            var executed = 0;
            for (var i = 0; i < 100; i++) { view.Post(() => executed++); }
            dispatcher.Pump();
            check(executed == 32 && dispatcher.PendingCount == 68,
                "dispatcher：每拍至多执行 32 任务（帧预算受控）");
            dispatcher.Pump(); dispatcher.Pump(); dispatcher.Pump();
            check(executed == 100 && dispatcher.PendingCount == 0, "dispatcher：后续泵拍清空积压");

            // 代际绑定：旧代际视图投递=失效；换代即撤旧代未执行任务
            var oldRan = false;
            check(view.Post(() => oldRan = true).Posted, "setup: 旧代际任务受理");
            dispatcher.OpenGeneration(feature, 2UL); // 再启用=新代际，旧代际全失效
            var stale = view.Post(() => { });
            check(!stale.Posted && stale.Reason == MainThreadPostReason.GenerationInvalid && stale.DiagnosticId == "BUE-MT-002",
                "dispatcher：任务绑定提交时 LifecycleGeneration——旧代际视图投递=显式失效");
            dispatcher.Pump();
            check(!oldRan, "dispatcher：代际失效后未执行任务不再执行（换代撤账）");
            var view2 = dispatcher.CreateView(feature, 2UL);
            var newRan = false;
            check(view2.Post(() => newRan = true).Posted, "dispatcher：新代际视图正常受理");
            dispatcher.Pump();
            check(newRan, "dispatcher：新代际任务在泵拍执行");

            // 停止/隔离边界：pending 撤账不执行、投递显式失败；重新开代恢复
            var stoppedRan = false;
            check(view2.Post(() => stoppedRan = true).Posted, "setup: 停止前任务在队");
            dispatcher.InvalidateOwner(feature, "feature-stopped");
            var afterInvalidate = view2.Post(() => { });
            check(!afterInvalidate.Posted && afterInvalidate.Reason == MainThreadPostReason.GenerationInvalid,
                "dispatcher：模块停止/隔离后投递=显式失败");
            dispatcher.Pump();
            check(!stoppedRan, "dispatcher：停止边界未执行任务不再执行（模块停止时所属待处理工作失效）");
            dispatcher.OpenGeneration(feature, 3UL);
            check(dispatcher.CreateView(feature, 3UL).Post(() => { }).Posted, "dispatcher：重开代际恢复受理（再启用=新代际）");

            // 宿主停止：全部所有者显式失败
            dispatcher.ShutdownHost("plugin-stopping");
            var afterHostStop = dispatcher.CreateView(feature, 4UL).Post(() => { });
            check(!afterHostStop.Posted && afterHostStop.Reason == MainThreadPostReason.GenerationInvalid,
                "dispatcher：宿主停止后投递=显式失败+诊断（宿主级失效）");

            // 执行期单任务异常隔离（不扩散、不打穿主线程泵）
            var reopened = new BetterUnturnedExperience.Core.Dispatch.MainThreadDispatcherRuntime(diagnostics.Add);
            reopened.OpenGeneration(feature, 1UL);
            var rv = reopened.CreateView(feature, 1UL);
            var afterFault = false;
            var pumpThrew = false;
            rv.Post(() => throw new InvalidOperationException("v3-task-fault"));
            rv.Post(() => afterFault = true);
            try { reopened.Pump(); }
            catch (Exception) { pumpThrew = true; }
            check(!pumpThrew && afterFault, "dispatcher：单任务异常隔离进诊断，泵拍继续执行其余任务");
            check(diagnostics.Exists(l => l.Contains("event=main-thread") && l.Contains("result=task-error")
                    && l.Contains("errorType=InvalidOperationException") && l.Contains("diagnosticId=BUE-MT-003")),
                "dispatcher：任务异常浮出结构化诊断（归属 feature/generation 可定位）");

            // null 任务=开发期错误：先诊断后 fail-fast（同 null-handler 纪律）
            var threw = false;
            try { rv.Post(null); }
            catch (ArgumentException) { threw = true; }
            check(threw && diagnostics.Exists(l => l.Contains("result=invalid-task") && l.Contains("diagnosticId=BUE-MT-004")),
                "dispatcher：null 任务浮出 BUE-MT-004 诊断后 fail-fast（开发期错误不静默）");

            // 非主线程泵拒绝（线程守卫：执行只发生在组合线程=宿主主线程）
            var guarded = new BetterUnturnedExperience.Core.Dispatch.MainThreadDispatcherRuntime(diagnostics.Add);
            guarded.OpenGeneration(feature, 1UL);
            var gv = guarded.CreateView(feature, 1UL);
            var foreignRan = false;
            gv.Post(() => foreignRan = true);
            System.Threading.Tasks.Task.Run(() => guarded.Pump()).Wait();
            check(!foreignRan && diagnostics.Exists(l => l.Contains("result=pump-rejected") && l.Contains("diagnosticId=BUE-MT-005")),
                "dispatcher：非主线程泵拒绝（任务不逃逸到线程池执行，主线程语义构造性保证）");
            guarded.Pump();
            check(foreignRan, "dispatcher：主线程泵正常执行（拒绝不留伤）");
        }

        private static void NetworkV3GroupLirDispatcherConsumption(System.Action<bool, string> check)
        {
            // 官方先行消费锚：真实 LIR 模块经真实 bootstrap.MainThread 视图消费
            // 平台 dispatcher——入站帧（传输泵线程）后的主线程业务执行只发生在
            // 平台泵拍上（泵线程→BUE dispatcher→LIR 主线程业务），生态用同一接口。
            var localContract = new ContractVersion(2, 0);
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var clientRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1001UL);
            var serverRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL, handshakeInitiator: false);
            var feature = new FeatureId(LirRuntime.FeatureIdValue);
            var diagnostics = new List<string>();
            var dispatcher = new BetterUnturnedExperience.Core.Dispatch.MainThreadDispatcherRuntime(diagnostics.Add);
            dispatcher.OpenGeneration(feature, 5UL);
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var authority = new FakeLirAuthority();
            var toasts = new List<string>();
            var module = new InPlaceReloadModule(feature);
            module.AuthorityFactoryForTests = () => authority;
            module.NetServiceFactoryForTests = (m, net) => new LirRepackNetwork(net, m.Authority, () => true);
            module.RoleProbeForTests = () => true;
            module.KeyDownProviderForTests = () => false; // no SDG input touch in host tests
            module.ToastSink = message => toasts.Add(message);
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 5UL, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, serverRuntime,
                dispatcher.CreateView(feature, 5UL));
            var start = module.Start(bootstrap);
            check(start.Started, "setup: 真实 LIR 模块经带 MainThread 视图的宿主 bootstrap 启动");
            check(module.NetService.EnsureInitializedOnGameThread(), "setup: 服务端网络初始化（首帧游戏线程）");
            // 建立客机→服务端会话（客机发起握手）
            clientRuntime.StartSession(2002UL);
            pair.First.Pump(); pair.Second.Pump(); pair.First.Pump();
            check(serverRuntime.Sessions.Count == 1, "setup: 服务端建立客机会话");
            var serverSession = serverRuntime.Sessions[0];
            check(serverSession.PeerSteamId == 1001UL, "setup: 会话对端=客机");
            // 真实线路径：客机频道发请求帧 → 服务端传输泵派发进 LIR 入站 handler
            // （handler 只解析+入队，主线程执行改由平台 dispatcher 承担）
            check(clientRuntime.RegisterChannel(feature, localContract, 1).Accepted, "setup: 客机频道注册");
            check(clientRuntime.SendToServer(feature, LirRepackWireCodec.BuildRequest(777UL), true) == NetworkSendResult.Sent,
                "setup: 压弹请求帧上线");
            pair.First.Pump(); pair.Second.Pump();
            check(authority.RepackCount == 0, "官方先行消费锚：入站只入队——主线程业务未经平台泵不执行（LIR 不再自建线程泵）");
            // 宿主主线程一帧：LIR 的节拍把业务 drain 投递给平台 dispatcher（而非
            // 自己内联执行），真正的执行发生在平台泵拍上。
            module.OnHostTick(new HostTick(1UL, 0.016f, TickPhase.Update));
            check(authority.RepackCount == 0, "官方先行消费锚：投递=fire-and-forget，OnHostTick 内不执行");
            dispatcher.Pump();
            check(authority.RepackCount == 1 && authority.LastRepackSteamId == 1001UL && authority.LastRepackRequestId == 777UL,
                "官方先行消费锚：泵线程→BUE dispatcher→LIR 主线程业务（压弹事务在平台泵拍执行）");
            // 停止边界：模块停止+代际失效后，旧视图投递显式失败、未执行任务不再执行
            var postStopRan = false;
            module.Stop(FeatureStopReason.PluginStopping);
            dispatcher.InvalidateOwner(feature, "plugin-stopping");
            var afterStop = bootstrap.MainThread.Post(() => postStopRan = true);
            check(!afterStop.Posted && afterStop.Reason == MainThreadPostReason.GenerationInvalid,
                "官方先行消费锚：模块停止后投递=显式失败（与生态同缝同权）");
            dispatcher.Pump();
            check(!postStopRan, "官方先行消费锚：停止边界后 pending 不再执行");
        }

        private static void NetworkV3GroupLirIdleDrainNoPost(System.Action<bool, string> check)
        {
            // DEV-V3-09 日志风暴修复锚：三环境 U3DS 实机发现 LIR→平台 dispatcher
            // 每帧空投（每投递一行 Debug result=posted，~63 行/秒）。根因=OnHostTick
            // 每帧无条件 Drain()→空队列仍 seam.Post(DrainOnce)。修复（F1）=有实际
            // 待办才投递，空闲帧不投；既有非破坏语义（有工作照常投递+泵拍执行）不动。
            var localContract = new ContractVersion(2, 0);
            var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
            var clientRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1001UL);
            var serverRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL, handshakeInitiator: false);
            var feature = new FeatureId(LirRuntime.FeatureIdValue);
            var diagnostics = new List<string>();
            var dispatcher = new BetterUnturnedExperience.Core.Dispatch.MainThreadDispatcherRuntime(diagnostics.Add);
            dispatcher.OpenGeneration(feature, 5UL);
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var authority = new FakeLirAuthority();
            var toasts = new List<string>();
            var module = new InPlaceReloadModule(feature);
            module.AuthorityFactoryForTests = () => authority;
            module.NetServiceFactoryForTests = (m, net) => new LirRepackNetwork(net, m.Authority, () => true);
            module.RoleProbeForTests = () => true;
            module.KeyDownProviderForTests = () => false;
            module.ToastSink = message => toasts.Add(message);
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 5UL, null,
                bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, serverRuntime,
                dispatcher.CreateView(feature, 5UL));
            check(module.Start(bootstrap).Started, "setup: 真实 LIR 模块经带 MainThread 视图的宿主 bootstrap 启动");
            check(module.NetService.EnsureInitializedOnGameThread(), "setup: 服务端网络初始化（首帧游戏线程）");

            System.Func<int> postedCount = () =>
            {
                var n = 0;
                foreach (var d in diagnostics)
                    if (d != null && d.Contains("result=posted") && d.Contains("BUE-MT-ACCEPT")) n++;
                return n;
            };

            // 空闲态：队列为空（尚无任何压弹请求），连续多拍宿主帧驱动。
            for (var beat = 1UL; beat <= 5UL; beat++)
                module.OnHostTick(new HostTick(beat, 0.016f, TickPhase.Update));
            check(postedCount() == 0,
                "日志风暴修复：空闲多拍→0 次 dispatcher 投递（每帧空投即被拒；pre-fix 每拍一行 result=posted=红）");

            // 有工作态：客机上线并送一个真实压弹请求帧（入站 handler 只入队）。
            clientRuntime.StartSession(2002UL);
            pair.First.Pump(); pair.Second.Pump(); pair.First.Pump();
            check(clientRuntime.RegisterChannel(feature, localContract, 1).Accepted, "setup: 客机频道注册");
            check(clientRuntime.SendToServer(feature, LirRepackWireCodec.BuildRequest(777UL), true) == NetworkSendResult.Sent,
                "setup: 压弹请求帧上线入队");
            pair.First.Pump(); pair.Second.Pump();
            var beforeActive = postedCount();
            module.OnHostTick(new HostTick(6UL, 0.016f, TickPhase.Update));
            check(postedCount() == beforeActive + 1,
                "非破坏：有排队工作时 Drain() 恰投递一次（守卫不误伤正常路径）");
            dispatcher.Pump();
            check(authority.RepackCount == 1 && authority.LastRepackSteamId == 1001UL && authority.LastRepackRequestId == 777UL,
                "投递后泵拍执行压弹事务（修复不破坏 drain 语义）");

            // 排空后再空闲：队列已空，继续多拍不得再投递。
            var afterDrain = postedCount();
            for (var beat = 7UL; beat <= 10UL; beat++)
                module.OnHostTick(new HostTick(beat, 0.016f, TickPhase.Update));
            check(postedCount() == afterDrain,
                "排空即转静默：队列空后空闲帧不再产生投递（风暴不复现）");

            module.Stop(FeatureStopReason.PluginStopping);
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
        // ═══════════════════════════════════════════════════════════════════
        // DEV-V2-20: LHT adoption (更好的尸潮播报) — the red-test surface.
        // Groups (ticket acceptance, 先红后绿):
        //   1. 信标守卫        — context=false → immediate release (guard input/output;
        //                        patches share native call sites, never business context)
        //   2. 组播不含本地     — session-driven SendToClients replaces the
        //                        Provider.clients handwritten loop + skip-local logic
        //   3. BUE 帧不可靠 1:1 — the 08 baseline proved 1:1 on the LMN path; the
        //                        BUE-frame path re-proves it + epoch/seq lifecycle parity
        //   4. enabled=false 完整停摆 — server stops tracking/broadcast, client stops
        //                        HUD, /horde has no LHT behavior, channel unregistered
        //   5. U3DS 双状态      — feature Available + presentation HeadlessOnly; identity
        //   6. 端到端全链       — register channel → subscribe → SendToClients, fake
        //                        (loopback) transport: the platform's first real consumer
        // No behavior is pinned through an installed Harmony patch — the guard
        // seam, the tracking module entries and the loopback runtime carry it.
        // ═══════════════════════════════════════════════════════════════════
        // DEV-V3-05: 宿主时钟语义登记的红测补齐组（fake-clock 钉死组先例
        // =DEV-V2-19）。八条语义中既有已钉的（序号从 1 严格单调/首拍 0/回拨
        // 钳零高水位/Phase=Update=0 冻结/EventId 冻结/保留身份 fail-fast/
        // 每拍恰一同帧去重/订阅者异常不逃逸/停止自动注销）在 01..04 各组
        // 继续作回归锚；本组补齐 T6 裁决②点名未覆盖面：暂停恢复语义/时钟
        // 故障拍显式 false+结构化诊断/主线程构造性（同步+同线程）/官方
        // 先行消费者经真时钟真总线消费。
        private static void AssertBueV3HostClockSemantics(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                var thirdParty = new FeatureId("io.example.thirdparty");

                Group("暂停恢复语义", () =>
                {
                    // 语义⑤登记：暂停期间宿主不拍针（无额外 Tick）；恢复后
                    // 下一拍 DeltaTime=实际间隔；序号跨暂停严格 +1（无追帧无
                    // 重置）；是否忽略大间隔由功能自决（SDK 自节流模式条目）。
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    long nowMs = 1000;
                    var clock = new BetterUnturnedExperience.Core.Events.HostTickClock(bus, () => nowMs);
                    var ticks = new List<HostTick>();
                    bus.Subscriber(thirdParty).Subscribe<HostTick>(ticks.Add);
                    clock.Tick();
                    nowMs = 6500; clock.Tick();   // 5.5s 无拍（=宿主 Update 暂停）后恢复
                    nowMs = 6516; clock.Tick();
                    Check(ticks.Count == 3, "暂停恢复：暂停期不产生额外 Tick，恢复一拍也不追帧");
                    Check(ticks.Count == 3 && ticks[0].TickNumber == 1UL && ticks[1].TickNumber == 2UL
                            && ticks[2].TickNumber == 3UL,
                        "暂停恢复：序号跨暂停严格 +1 不重置");
                    Check(ticks.Count == 3 && Math.Abs(ticks[1].DeltaTime - 5.5f) < 0.001f,
                        "暂停恢复：恢复后下一拍 DeltaTime=实际间隔（暂停时长如实反映，无隐藏感知）");
                    Check(ticks.Count == 3 && Math.Abs(ticks[2].DeltaTime - 0.016f) < 0.001f,
                        "暂停恢复：恢复后再下一拍回到正常增量");
                });

                Group("Tick 失败不扩散与结构化诊断", () =>
                {
                    // 语义⑦登记：时钟故障拍=显式 false 返回+结构化诊断行
                    // （BUE-CLOCK-001 码族，本票定案），不抛回泵调用方、零
                    // 派发；失败拍不消耗序号（序号只在成功产生时推进）、不
                    // 推进也不回退时间基线（下一成功拍以最后成功基线计增量）。
                    var diagnostics = new List<string>();
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus(diagnostics.Add);
                    long nowMs = 100;
                    var poison = false;
                    var clock = new BetterUnturnedExperience.Core.Events.HostTickClock(bus, () =>
                    {
                        if (poison) throw new InvalidOperationException("clock source fault");
                        return nowMs;
                    });
                    var ticks = new List<HostTick>();
                    bus.Subscriber(thirdParty).Subscribe<HostTick>(ticks.Add);
                    Check(clock.Tick() && ticks.Count == 1 && ticks[0].TickNumber == 1UL,
                        "失败隔离：正常首拍产生 Tick（序号 1）");
                    poison = true;
                    Check(!clock.Tick(), "失败隔离：故障拍返回显式 false（bool 只表达宿主已记录处理失败，不要求调用方善后）");
                    Check(ticks.Count == 1, "失败隔离：故障拍零派发（不半途发出不完整 tick）");
                    poison = false; nowMs = 300;
                    Check(clock.Tick(), "失败隔离：故障恢复后照常产针（时钟跨故障存活）");
                    Check(ticks.Count == 2 && ticks[1].TickNumber == 2UL,
                        "失败隔离：失败拍不消耗序号——下一成功拍严格 +1 无缝隙");
                    Check(ticks.Count == 2 && Math.Abs(ticks[1].DeltaTime - 0.2f) < 0.001f,
                        "失败隔离：故障拍不推进时间基线——恢复拍增量=距最后成功拍的单调差");
                    Check(diagnostics.Exists(l => l.Contains("event=host-tick") && l.Contains("result=failed")
                            && l.Contains("errorType=InvalidOperationException") && l.Contains("diagnosticId=BUE-CLOCK-001")),
                        "失败隔离：故障浮出结构化诊断行（event/result/errorType/diagnosticId 字段齐备，不静默吞）");
                });

                Group("主线程构造性保证", () =>
                {
                    // 语义⑧登记：时钟不内置线程/不隐藏后台派发——handler 在
                    // Tick() 调用方线程上同步执行、Tick() 返回前派发已完成
                    // （宿主 Update 链单生产驱动的构造性前提）。
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    long nowMs = 0;
                    var clock = new BetterUnturnedExperience.Core.Events.HostTickClock(bus, () => nowMs);
                    var seen = new List<HostTick>();
                    var handlerThread = -1;
                    bus.Subscriber(thirdParty).Subscribe<HostTick>(t =>
                    {
                        handlerThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                        seen.Add(t);
                    });
                    var callerThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    clock.Tick();
                    Check(seen.Count == 1,
                        "主线程构造：Tick() 返回前派发已完成（同步链，无隐藏队列/线程切换）");
                    Check(handlerThread == callerThread,
                        "主线程构造：handler 在调用方线程上执行——生态可在回调内安全调用主线程限定 API");
                    nowMs = 10; clock.Tick();
                    Check(seen.Count == 2 && handlerThread == callerThread,
                        "主线程构造：持续拍维持同一线程（构造性保证无例外路径）");
                });

                Group("官方先行消费锚：LHT 经真宿主时钟", () =>
                {
                    // LHT=既有官方消费者回归锚（裁决六同权三条）：真实宿主
                    // 时钟经真总线派发驱动 LHT 的唯一宿主帧入口（Start 里经
                    // bootstrap.Events 订阅的冻结缝，非手调 OnHostTick）；
                    // 每拍恰一帧工作、大间隔恢复不追帧（10Hz HUD 自节流=
                    // 官方推荐模式的功能侧先例）。
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    var surface = new FakeHudSurface();
                    var module = NewLhtModule(bus, new LhtSendProbeNetwork(), new FakeHordeAuthority(), surface, isServer: true);
                    long nowMs = 0;
                    var clock = new BetterUnturnedExperience.Core.Events.HostTickClock(bus, () => nowMs);
                    nowMs = 16; clock.Tick();
                    nowMs = 32; clock.Tick();
                    nowMs = 48; clock.Tick();
                    Check(surface.DrainCalls == 3,
                        "官方锚：三拍真时钟=三次 LHT 帧入口（经订阅缝逐拍恰一派发，不重不漏）");
                    nowMs = 1148; clock.Tick();
                    Check(surface.DrainCalls == 4,
                        "官方锚：1.1s 间隔的恢复拍只驱动一次帧工作（大间隔不追帧，节奏归功能自决）");
                    module.Stop(FeatureStopReason.PluginStopping);
                    nowMs = 1164; clock.Tick();
                    Check(surface.DrainCalls == 4,
                        "官方锚：停止后的 LHT 实例不再消费时钟（模块停止守卫在帧入口生效）");
                });

                Group("生态对照 NoOp HostTick 支线", () =>
                {
                    // T6 裁决③：probe 补 HostTick 支线——注册→真 StartCatalog
                    // 组装→经 bootstrap.Events 取订阅入口→收宿主时钟 tick
                    // （检查载荷三字段）→停止边界自动注销。生产总线+生产泵
                    // =生态作者的真实路径，与官方 LHT 同缝同权。
                    var noopRuntime = new FeatureRegistrationRuntime();
                    noopRuntime.OpenRegistration();
                    Check(noopRuntime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted,
                        "NoOp setup：样例登记入探针运行时");
                    Check(noopRuntime.CompleteRuntime(), "NoOp setup：目录冻结");
                    BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated();
                    BueFeatureStartRuntime.StartCatalog(noopRuntime, NewLoopbackNetwork(2003UL));
                    var probe = NoOpFeatureRegistration.LastProbe;
                    Check(probe != null && probe.Started, "NoOp setup：probe 经真实 StartCatalog 启动");
                    Check(probe.HostTickSubscribed,
                        "支线：probe 经 Events 缝完成 HostTick 订阅（订阅入口生态可得）");
                    // The production bus is host-shared across the suite (probe
                    // instances of earlier lifecycle/network groups keep their
                    // subscriptions on it until their own stop boundaries), so
                    // per-iteration receipts are observed through THIS group's
                    // own unique-identity subscription; the probe's own count
                    // rides along as the production-path smoke (the full-chain
                    // probe lands with DEV-V3-08).
                    var seen = new List<HostTick>();
                    var seenHandle = BetterUnturnedExperience.Plugin.BueHostEventRuntime.Bus
                        .Subscriber(new FeatureId("io.github.yu80rice.bue.test.v3clockprobe"))
                        .Subscribe<HostTick>(seen.Add);
                    var before = probe.HostTicksReceived;
                    Check(BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce(),
                        "支线：宿主泵拍 TickOnce 产针");
                    Check(BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce(),
                        "支线：下一泵拍再产针");
                    Check(seen.Count == 2, "支线：每拍恰一 tick 到达独立订阅者（不重不漏）");
                    Check(seen.Count == 2 && seen[1].TickNumber == seen[0].TickNumber + 1UL,
                        "支线：序号逐拍推进 +1（生产真时钟跨两拍严格单调）");
                    Check(probe.HostTicksReceived == before + 2,
                        "支线：probe 自身同两拍到达（生产泵真实驱动订阅者集合）");
                    Check(probe.LastHostTickPhase == TickPhase.Update
                            && probe.LastHostTickDeltaSeconds >= 0f,
                        "支线：载荷三字段检查=Phase 冻结 Update/DeltaTime 非负（时序面无业务内容）");
                    var probeAtStop = probe.HostTicksReceived;
                    BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce();
                    Check(probe.HostTicksReceived == probeAtStop,
                        "支线：停止边界自动注销——后续泵拍不再到达 probe（停止自动注销生态侧）");
                    seenHandle.Dispose();
                    BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                });
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V3-05 host clock collection: ALL GREEN (0 failures) — groups: 暂停恢复语义/Tick 失败不扩散与结构化诊断/主线程构造性保证/官方先行消费锚 LHT 经真宿主时钟/生态对照 NoOp HostTick 支线");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V3-05 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V3-06: BueSettings 接线与面板动态路由。面板路由以冻结注册目录
        // 为唯一来源（官方硬编码清单退役）；面板=编辑 adapter 非第二事实源；
        // 未提供设置的功能不伪造设置页；注入 view 绑 (feature, 代际)，读永
        // 可用、写经代际门+权威侧门；官方先行消费=真实 LIT 经注入 view。
        private static void AssertBueV3SettingsWiringAndPanelRouting(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            var settingsRoots = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                var litFeature = new FeatureId(LitRuntime.FeatureIdValue);
                string EnsureSettings(Func<bool> authoritySide = null)
                {
                    // 每子组确定性临时持久根（BueMainThreadRuntime.Clear/
                    // EnsureCreated 先例）：跨组静态注册表不复用旧状态。
                    var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BUE-V3-06-" + Guid.NewGuid().ToString("N"));
                    settingsRoots.Add(root);
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, authoritySide ?? (() => true), null);
                    return root;
                }

                Group("面板目录路由：官方条目不依赖装配参数", () =>
                {
                    // 官方硬编码清单退役的正面锚：组合根只经注册目录+设置
                    // facet 得到 LIT 的设置页与编辑路由，不再依赖生产构造参
                    // 数（BII 组合根编辑器保留为显式路由，插件自身 UI 功能
                    // 非「清单」）。
                    EnsureSettings();
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    Check(BetterItemInteractionFeatureRegistration.Register().Accepted,
                        "目录路由：BII 经公共桥注册（组合根显式路由前提）");
                    Check(BueRuntimeHost.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                        "目录路由：LIT 经公共桥注册（facet 目录条目前提）");
                    Check(runtime.CompleteRuntime(), "目录路由：探测目录完成屏障");
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        var entries = composition.ManagementPanel.Model.GetEntries();
                        var litEntry = default(ManagementEntryView);
                        var hasLitEntry = false;
                        for (var index = 0; index < entries.Count; index++)
                        {
                            if (entries[index].StableId != litFeature.Value) continue;
                            litEntry = entries[index];
                            hasLitEntry = true;
                        }
                        // DEV-V4-06：LIT 设置 facet 以全局 Choice 回归；DEV-V5-02：
                        // 三模式退役后仅剩 direction 收尾偏好一条；enabled 总开关
                        // 保持退役（DEV-V4-04），不再以任何形状回到面板。
                        Check(hasLitEntry && litEntry.BueSettings.Count == 1,
                            "目录路由：LIT 设置页=一条全局 Choice（统一排版后仅剩 direction），mode/enabled 保持退役");
                        var hasModeRow = false;
                        var hasDirectionRow = false;
                        for (var settingIndex = 0; settingIndex < litEntry.BueSettings.Count; settingIndex++)
                        {
                            if (litEntry.BueSettings[settingIndex].SettingId == "inventorytidy.mode") hasModeRow = true;
                            if (litEntry.BueSettings[settingIndex].SettingId == "inventorytidy.direction") hasDirectionRow = true;
                        }
                        Check(hasDirectionRow && !hasModeRow,
                            "目录路由：LIT 仅 direction 以冻结 SettingId 出现在目录条目上（整理模式行随三档退役）");
                        var edit = composition.ManagementPanel.Model.TryEditBueSetting(litFeature,
                            "inventorytidy.enabled", PluginConfigValue.BooleanValue(false));
                        Check(!edit.Accepted,
                            "目录路由：退役总开关的编辑=显式拒绝（facet 已退役，编辑缝不再路由到 LIT 设置面）");
                    }
                    finally { composition.Destroy(); }
                    BueRuntimeHost.Bind(previousRuntime);
                });

                Group("未提供设置的功能不伪造设置页", () =>
                {
                    // 无 facet 的功能=无平台管理设置：条目零设置行、编辑显式
                    // 拒，且经统一诊断通道留痕（BUE-SET-004，回退编辑器拒编辑
                    // 未提供设置者），不得静默也不得借他人设置页冒充。
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    var plain = new MatrixProbeRegistration("io.example.settings-nofacet");
                    Check(runtime.Register(plain).Accepted, "不伪造页：无 facet 探针注册成功");
                    Check(runtime.CompleteRuntime(), "不伪造页：探测目录完成屏障");
                    var composition = new BueClientUiCompositionRoot();
                    var lines = new List<string>();
                    var previousRecorder = BueRuntimeLog.Recorder;
                    BueRuntimeLog.Recorder = lines.Add;
                    try
                    {
                        composition.RefreshManagementPanel();
                        var entries = composition.ManagementPanel.Model.GetEntries();
                        var plainEntry = default(ManagementEntryView);
                        var hasPlain = false;
                        for (var index = 0; index < entries.Count; index++)
                        {
                            if (entries[index].StableId != "io.example.settings-nofacet") continue;
                            plainEntry = entries[index];
                            hasPlain = true;
                        }
                        Check(hasPlain && plainEntry.BueSettings.Count == 0,
                            "不伪造页：无 facet 功能条目零设置行");
                        var edit = composition.ManagementPanel.Model.TryEditBueSetting(
                            new FeatureId("io.example.settings-nofacet"), "anything", PluginConfigValue.BooleanValue(true));
                        Check(!edit.Accepted, "不伪造页：对无 facet 功能的编辑显式拒");
                        var sawRejectionLine = false;
                        for (var index = 0; index < lines.Count; index++)
                        {
                            if (lines[index].Contains("diagnosticId=BUE-SET-004")
                                && lines[index].Contains("io.example.settings-nofacet")) sawRejectionLine = true;
                        }
                        Check(sawRejectionLine,
                            "不伪造页：拒编辑经统一诊断通道留痕（BUE-SET-004 码族，本票定案）");
                    }
                    finally
                    {
                        BueRuntimeLog.Recorder = previousRecorder;
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });

                Group("矩阵接线两侧", () =>
                {
                    // 可用性矩阵行 Settings=DEV-V3-06 后可用的两侧红线：有
                    // facet 的功能在真实 StartCatalog 上得到可注入 view（可用
                    // 侧=本票接线证据）；无 facet 的功能=null（未提供不伪造+
                    // 阶段基线纪律，旧「接线前 null」侧语义保留）；Logger 行
                    // =DEV-V3-07 接线后可用（07 红线在本票新组断言）。
                    EnsureSettings();
                    var facetProbe = new SettingsFacetProbeRegistration("io.example.settings-facet-probe");
                    var plainProbe = new MatrixProbeRegistration("io.example.settings-plain-probe");
                    var runtime = new FeatureRegistrationRuntime();
                    runtime.OpenRegistration();
                    Check(runtime.Register(facetProbe).Accepted, "setup: 有 facet 功能受理（facet 进目录）");
                    Check(runtime.Register(plainProbe).Accepted, "setup: 无 facet 功能受理");
                    Check(runtime.CompleteRuntime(), "setup: 目录冻结");
                    BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(3101UL));
                    var captured = facetProbe.Module.Bootstrap;
                    Check(captured != null && captured.Settings != null,
                        "矩阵可用侧：有 facet 功能在真实 StartCatalog 上得到注入 Settings view");
                    var snapshot = captured.Settings.GetSnapshot(SettingRevisionScope.ClientPreference);
                    Check(snapshot.Entries.Count == 1 && snapshot.Entries[0].SettingId == "probe.enabled" && snapshot.Revision == 0u,
                        "矩阵可用侧：view 快照=自身 facet 作用域（一枚举开关，新库 revision 0）");
                    var commit = captured.Settings.Submit(new ScopedSettingChangeRequest(1, SettingRevisionScope.ClientPreference, 0,
                        new[] { new SettingMutation("probe.enabled", SettingValue.Toggle(false)) }));
                    Check(commit.Accepted && commit.Revision == 1u, "矩阵可用侧：经 view 提交生效（revision 推进 1）");
                    Check(plainProbe.Module.Bootstrap != null && plainProbe.Module.Bootstrap.Settings == null,
                        "矩阵 null 侧：无 facet 功能=Settings null（未提供不伪造；阶段基线纪律）");
                    Check(captured.Logger != null,
                        "矩阵可用侧：Logger=DEV-V3-07 接线后非 null（每功能一律注入 view，阶段基线 null 侧由手工组装锚另断）");
                });

                Group("官方与生态并列同面板", () =>
                {
                    // 票面红线「官方+生态条目并列、同样可见可编辑」：同一面板
                    // 目录路由下，官方 LIT 与生态 facet 功能并列可见、经同一
                    // 编辑 seam 可编辑、各自作用域独立提交互不越染（同权四条
                    // 之②的面板侧锚）。
                    EnsureSettings();
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    BetterItemInteractionFeatureRegistration.Register();
                    Check(runtime.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                        "并列 setup:官方 LIT 登记受理");
                    var ecoFeature = new FeatureId("io.example.ecosystem-panel");
                    Check(runtime.Register(new SettingsFacetProbeRegistration("io.example.ecosystem-panel")).Accepted,
                        "并列 setup:生态 facet 登记受理");
                    Check(runtime.CompleteRuntime(), "并列 setup:目录冻结");
                    BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(3501UL));
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        var biiFeature = BetterItemInteractionSettingsState.Feature;
                        var litSettings = -1;
                        var biiSettings = -1;
                        var ecoSettings = -1;
                        var entries = composition.ManagementPanel.Model.GetEntries();
                        for (var index = 0; index < entries.Count; index++)
                        {
                            if (entries[index].StableId == litFeature.Value) litSettings = entries[index].BueSettings.Count;
                            if (entries[index].StableId == biiFeature.Value) biiSettings = entries[index].BueSettings.Count;
                            if (entries[index].StableId == ecoFeature.Value) ecoSettings = entries[index].BueSettings.Count;
                        }
                        // DEV-V4-06：官方 LIT 以全局 Choice 回归；DEV-V5-02：
                        // 三模式退役后恰剩 direction 一行；并列可编辑锚仍由官方
                        // BII（组合路由，恰剩 AutoRotate）承担。
                        Check(litSettings == 1 && ecoSettings == 1,
                            "并列可见:官方 LIT 恰一条全局 Choice 行(统一排版后仅剩 direction，mode 档随三模式退役)与生态条目各按其事实投影(同一目录规则)");
                        Check(biiSettings == 1,
                            "并列可见:BII 设置页恰剩 AutoRotate 一行(Enabled 退役,仍是普通设置)");
                        var litEdit = composition.ManagementPanel.Model.TryEditBueSetting(litFeature,
                            "inventorytidy.enabled", PluginConfigValue.BooleanValue(false));
                        var biiEdit = composition.ManagementPanel.Model.TryEditBueSetting(biiFeature,
                            "AutoRotate", PluginConfigValue.BooleanValue(false));
                        var ecoEdit = composition.ManagementPanel.Model.TryEditBueSetting(ecoFeature,
                            "probe.enabled", PluginConfigValue.BooleanValue(false));
                        Check(!litEdit.Accepted && biiEdit.Accepted && ecoEdit.Accepted && biiEdit.Revision == 1u && ecoEdit.Revision == 1u,
                            "并列可编辑:退役行显式拒,同一编辑 seam 官方(BII)与生态各自受理,revision 独立推进(同权四条之②)");
                        composition.RefreshManagementPanel();
                        entries = composition.ManagementPanel.Model.GetEntries();
                        var biiReflected = false;
                        for (var index = 0; index < entries.Count; index++)
                        {
                            if (entries[index].StableId != biiFeature.Value) continue;
                            for (var s = 0; s < entries[index].BueSettings.Count; s++)
                                if (entries[index].BueSettings[s].SettingId == "AutoRotate"
                                    && !entries[index].BueSettings[s].EffectiveValue.Boolean) biiReflected = true;
                        }
                        SettingValue ecoValue;
                        uint ecoRevision;
                        var ecoStore = BetterUnturnedExperience.Plugin.BueSettingsRuntime.Registry;
                        Check(biiReflected
                                && ecoStore.TryGetRuntime(ecoFeature).TryGet("probe.enabled", out ecoValue, out ecoRevision) && !ecoValue.Boolean,
                            "并列可编辑:两功能的权威面各自落地,互不越染(作用域隔离经同一面板)");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });

                Group("官方先行消费锚：面板自身草稿→保存经真实宿主设置面", () =>
                {
                    // DEV-V4-01 验收③：至少一条 ClientPreference 走「未保存
                    // 草稿→保存配置」——编辑只进内存、点保存才经一次原子 Submit
                    // 落宿主 runtime、revision 单次推进、保存后快照如实反映新值。
                    // DEV-V4-04：LIT 的 enabled facet 退役——宿主设置面锚改挂
                    // NoOp 样板（noop.probe-toggle，官方出货的生态接入样板，
                    // 经同一公共桥+同一面板路由）。
                    EnsureSettings();
                    var noopFeature = new FeatureId("io.github.yu80rice.bue.noop");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    BetterItemInteractionFeatureRegistration.Register();
                    Check(runtime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted,
                        "草稿锚 setup：NoOp 样板 facet 登记受理");
                    Check(runtime.CompleteRuntime(), "草稿锚 setup：目录冻结");
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        var store = BetterUnturnedExperience.Plugin.BueSettingsRuntime.Registry;
                        SettingValue before; uint beforeRevision;
                        Check(store.TryGetRuntime(noopFeature).TryGet("noop.probe-toggle", out before, out beforeRevision),
                            "草稿锚 setup：ClientPreference（noop.probe-toggle）经宿主 runtime 可读");
                        var desired = !before.Boolean;

                        var model = composition.ManagementPanel.Model;
                        model.OpenDetail(noopFeature.Value);
                        Check(model.DraftEditBueSetting("noop.probe-toggle", PluginConfigValue.BooleanValue(desired)),
                            "草稿锚：ClientPreference 进草稿");
                        SettingValue during; uint duringRevision;
                        store.TryGetRuntime(noopFeature).TryGet("noop.probe-toggle", out during, out duringRevision);
                        Check(during.Boolean == before.Boolean && duringRevision == beforeRevision,
                            "草稿锚：改设置不立刻写权威源（宿主 runtime 值/revision 不动）");
                        Check(model.IsDirty, "草稿锚：改值即脏");

                        var report = model.SaveDraft();
                        Check(report.Outcome == DraftSaveOutcome.Success && report.PrimaryMessage == "配置已保存。",
                            "草稿锚：保存成功文案");
                        SettingValue after; uint afterRevision;
                        store.TryGetRuntime(noopFeature).TryGet("noop.probe-toggle", out after, out afterRevision);
                        Check(after.Boolean == desired && afterRevision == beforeRevision + 1,
                            "草稿锚：保存才经一次原子 Submit 落宿主 runtime（revision 单次推进）");
                        Check(!model.IsDirty, "草稿锚：全成功后草稿清空");

                        composition.RefreshManagementPanel();
                        var reflected = false;
                        foreach (var row in model.GetEntries())
                        {
                            if (row.StableId != noopFeature.Value) continue;
                            foreach (var setting in row.BueSettings)
                                if (setting.SettingId == "noop.probe-toggle" && setting.EffectiveValue.Boolean == desired) reflected = true;
                        }
                        Check(reflected, "草稿锚：保存后面板快照如实反映新值（面板=编辑 adapter，非第二事实源）");
                    }
                    finally
                    {
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });

                Group("登记 facet 侧逐例", () =>
                {
                    // facet 无效=确定性拒绝（reason=InvalidDefinitionArtifact
                    // 复用冻结枚举，诊断码 BUE-REG-011 本票定案）：空列表、
                    // 描述符 feature 错配（schema 必须是自己的）、超 64/功能
                    // 上限（本票定值）逐例；有效 facet 进目录投影（面板动态
                    // 路由的唯一来源）。
                    var runtime = new FeatureRegistrationRuntime();
                    runtime.OpenRegistration();
                    var emptyResult = runtime.Register(new FacetProbeRegistration(
                        "io.example.facet-empty", new SettingDescriptor[0], null));
                    Check(!emptyResult.Accepted && emptyResult.Reason == FeatureRegistrationReason.InvalidDefinitionArtifact
                            && emptyResult.DiagnosticId == "BUE-REG-011",
                        "011：空 facet 显式拒（提供空 schema 不=提供设置页，也不静默）");
                    var mismatched = runtime.Register(new FacetProbeRegistration("io.example.facet-mismatch",
                        new[] { SettingsProbeDescriptor(new FeatureId("io.example.someone-else"), "probe.enabled") }, null));
                    Check(!mismatched.Accepted && mismatched.DiagnosticId == "BUE-REG-011",
                        "011：描述符 Feature 错配=拒（只能声明自己功能的 schema）");
                    var tooMany = new List<SettingDescriptor>();
                    var capacityFeature = new FeatureId("io.example.facet-capacity");
                    for (var i = 0; i < 65; i++) tooMany.Add(SettingsProbeDescriptor(capacityFeature, "cap-" + i.ToString()));
                    Check(!runtime.Register(new FacetProbeRegistration("io.example.facet-capacity", tooMany, null)).Accepted,
                        "011：超 64/功能上限（本票定值）=显式拒（可观察可测试）");
                    var goodFeature = new FeatureId("io.example.facet-good");
                    var applied = false;
                    Check(runtime.Register(new FacetProbeRegistration("io.example.facet-good",
                        new[] { SettingsProbeDescriptor(goodFeature, "probe.enabled") }, () => applied = true)).Accepted,
                        "setup: 有效 facet 受理");
                    Check(runtime.CompleteRuntime(), "setup: 目录冻结（只含有效 facet 功能）");
                    var entry = runtime.Catalog.Entries[0];
                    Check(entry.Definition.Feature.Value == "io.example.facet-good"
                            && entry.SettingDescriptors != null && entry.SettingDescriptors.Count == 1
                            && entry.OnSettingsApplied != null,
                        "目录 facet 投影：目录条目暴露 schema+刷新钩子（面板路由唯一来源）");
                    entry.OnSettingsApplied();
                    Check(applied, "目录 facet 投影：刷新钩子经目录可达（不反射不读路径）");
                });

                // DEV-V4-04 注记：DEV-V3-06 的「官方先行消费锚真实 LIT 经注入
                // view」全链组随 enabled facet 退役而退役（LIT 不再有 facet，
                // 面板行/OnSettingsApplied 路由不存在）；同缝由「生态对照 NoOp
                // 设置支线」与 Settings.Tests 的 view/代际组继续覆盖，DEV-V4-06
                // 将以 LIT mode/direction 重新落面板路由锚。
                Group("生态对照 NoOp 设置支线", () =>
                {
                    // 生态作者视角最小对照（全链 probe 归 08）：注册带 facet
                    // → 注入 view → 读快照 → 合法提交观察 revision 推进 → 非
                    // 法提交观察显式拒。不伪造 ServerAuthority 写入口（NoOp 只
                    // 声明已承诺 ClientPreference 作用域）。
                    EnsureSettings(() => false);
                    var runtime = new FeatureRegistrationRuntime();
                    runtime.OpenRegistration();
                    Check(runtime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted,
                        "支线 setup：NoOp probe（含 facet）登记受理");
                    Check(runtime.CompleteRuntime(), "支线 setup：目录冻结");
                    BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(3301UL));
                    var probe = NoOpFeatureRegistration.LastProbe;
                    Check(probe != null && probe.Started, "支线 setup：probe 经真实 StartCatalog 启动");
                    Check(probe.SettingsAvailable && probe.SettingsSchemaVisible,
                        "支线：probe 观察到注入 view 且自身 schema 完整可见");
                    Check(probe.SettingsCommitAccepted && probe.SettingsRevisionAdvanced,
                        "支线：合法提交被接受且 revision 推进可观察");
                    Check(probe.SettingsInvalidRejected,
                        "支线：非法提交（未知 setting）显式拒可观察");
                    BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                });

                Group("面板启停命令适配器 03 移交", () =>
                {
                    // 03 具名移交「面板按钮接线随 06 动态路由落地」：面板=
                    // command adapter——启停命令经模型 seam 转发宿主
                    // SetFeatureEnabled（状态机归宿主），条目状态投影如实反映
                    // （面板非第二事实源的状态侧）。原生按钮 UI=具名递延随 09。
                    EnsureSettings();
                    var probeFeature = new FeatureId("io.example.settings-toggle-probe");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    var probe = new SettingsFacetProbeRegistration("io.example.settings-toggle-probe");
                    Check(runtime.Register(probe).Accepted, "启停 setup：facet 探针受理");
                    Check(runtime.CompleteRuntime(), "启停 setup：目录冻结");
                    BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(3401UL));
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        Check(composition.ManagementPanel.Model.TryToggleFeature(probeFeature, false),
                            "命令 adapter：面板停用=显式成功（转发 SetFeatureEnabled/UserDisabled）");
                        composition.RefreshManagementPanel();
                        var stoppedState = default(FeatureState);
                        var seen = false;
                        var entries = composition.ManagementPanel.Model.GetEntries();
                        for (var index = 0; index < entries.Count; index++)
                        {
                            if (entries[index].StableId != probeFeature.Value) continue;
                            stoppedState = entries[index].FeatureState;
                            seen = true;
                        }
                        Check(seen && stoppedState == FeatureState.Stopped,
                            "状态投影：停用后面板条目如实=Stopped（宿主唯一状态机，非面板私账）");
                        Check(composition.ManagementPanel.Model.TryToggleFeature(probeFeature, true),
                            "命令 adapter：面板启用=显式成功（新代际重臂）");
                        composition.RefreshManagementPanel();
                        entries = composition.ManagementPanel.Model.GetEntries();
                        for (var index = 0; index < entries.Count; index++)
                        {
                            if (entries[index].StableId != probeFeature.Value) continue;
                            stoppedState = entries[index].FeatureState;
                        }
                        Check(stoppedState == FeatureState.Running,
                            "状态投影：启用后条目如实=Running（同一投影链）");
                        Check(probe.Module.Bootstrap != null && probe.Module.Bootstrap.Settings != null,
                            "重启续用：再启用代际仍得到注入 view");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });

                // DEV-V4-05 官方先行消费锚（T1 检验点 ①）：组合根把宿主状态机
                // 事实（State + StopReason + Diagnostic）喂给面板条目，面板模型
                // 投影九态中文与启用开关目标。停用后面板条目=已停用（UserDisabled
                // 随条目走），再启用=运行中；外部插件条目无开关由 ClientUi 组覆盖。
                Group("功能级启停表面投影 05", () =>
                {
                    EnsureSettings();
                    var surfaceFeature = new FeatureId("io.example.toggle-surface-probe");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    var probe = new SettingsFacetProbeRegistration("io.example.toggle-surface-probe");
                    Check(runtime.Register(probe).Accepted, "表面 setup：facet 探针受理");
                    Check(runtime.CompleteRuntime(), "表面 setup：目录冻结");
                    BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(3501UL));
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        var model = composition.ManagementPanel.Model;
                        var projection = model.GetFeatureStatusProjection(surfaceFeature.Value);
                        Check(projection.ShowsEnableToggle,
                            "表面投影：目录 BUE 功能条目有启用开关（官方先行消费锚①）");
                        Check(projection.StateText == "运行中",
                            "表面投影：Running=运行中（九态中文，面板不 FeatureState.ToString()）");
                        Check(projection.PresentationText.Length > 0,
                            "表面投影：表现状态独立一行有值");
                        Check(model.TryToggleFeature(surfaceFeature, false),
                            "表面 setup：面板停用受理（走 03 目标提交缝）");
                        composition.RefreshManagementPanel();
                        projection = model.GetFeatureStatusProjection(surfaceFeature.Value);
                        Check(projection.StateText == "已停用",
                            "表面投影：Stopped+UserDisabled=已停用（停用原因随条目走组合根）");
                        Check(!projection.EnableToggleTarget && projection.PendingEffectText.Length == 0,
                            "表面投影：停用条开关目标=关且无待生效提示");
                        Check(model.TryToggleFeature(surfaceFeature, true),
                            "表面 setup：面板再启用受理");
                        composition.RefreshManagementPanel();
                        projection = model.GetFeatureStatusProjection(surfaceFeature.Value);
                        Check(projection.StateText == "运行中" && projection.EnableToggleTarget,
                            "表面投影：再启用后条目=运行中且开关目标=开（同一投影链）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });

                // DEV-V4-05 F1（Round 2）：有开关 iff 机器当前跟踪（可停止 seam
                // 所有权），不按状态枚举推断——登记进目录但从未 StartCatalog 的
                // 条目即使组合根状态兜底 Running 也不得画出死端开关。
                Group("无缝条目不开关 05", () =>
                {
                    EnsureSettings();
                    var untrackedFeature = new FeatureId("io.example.toggle-untracked-probe");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    var probe = new SettingsFacetProbeRegistration("io.example.toggle-untracked-probe");
                    Check(runtime.Register(probe).Accepted, "无缝 setup：探针受理");
                    Check(runtime.CompleteRuntime(), "无缝 setup：目录冻结（刻意不 StartCatalog）");
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        var model = composition.ManagementPanel.Model;
                        var projection = model.GetFeatureStatusProjection(untrackedFeature.Value);
                        Check(!projection.ShowsEnableToggle,
                            "无缝条目：机器未跟踪的目录条目无启用开关（iff seam，不按状态推断）");
                        Check(projection.StateText == "运行中",
                            "无缝条目：状态行仍按九态映射显示组合根兜底状态（状态表与 seam 判据独立）");
                        model.OpenDetail(untrackedFeature.Value);
                        Check(!model.DraftSetFeatureEnabled(false),
                            "无缝条目：模型拒绝启停意图（表面与模型同门禁）");
                    }
                    finally
                    {
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });

                // DEV-V4-06：LIT 标题栏与 mode/direction。手装模块助手：真实
                // LIT 模块 + 宿主 bootstrap（假生命周期/假设置 view 可注入），
                // 本地路径经 ServerRoleProbeForTests 走 RequestLocalTidy。
                InventoryTidyModule StartLitModuleWith(FakeTidyLifetime lifetime, IScopedFeatureSettings settings)
                {
                    var lit = new FeatureId(LitRuntime.FeatureIdValue);
                    var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                    var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                    var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 2101UL);
                    var litModule = new InventoryTidyModule(lit);
                    litModule.ScopeDirectoryForTests = NewLitFaultDirectory();
                    litModule.FaultContextForTests = () => new LitFaultScopeContext("TestMap", 1);
                    var started = litModule.Start(new FeatureBootstrap(default(FeatureScopeIdentity), 9UL, settings,
                        bus.Subscriber(lit), bus.Publisher(lit), bus.EventRegistry(lit), null, null, lifetime, network));
                    Check(started.Started, "DEV-V4-06 setup：模块经宿主 bootstrap 启动");
                    return litModule;
                }

                // DEV-V4-09：泵助手——跨过 16 拍节流窗（首拍即试，20 拍必触）。
                void PumpTidyModule(InventoryTidyModule module)
                {
                    for (var tick = 0; tick < 20; tick++) module.Tick();
                }

                // 九态决定按钮存在性（Q55）：注入/拆除/暂留由生命周期事实
                // （IFeatureLifetime.CurrentStatus）决定，不由 patch 私有布尔；
                // 过渡态点击=安全回退、不报假成功；无生命周期事实=fail-closed。
                Group("DEV-V4-06 九态决定按钮存在性", () =>
                {
                    Check(InventoryTidyModule.DecideTidyUiAction(FeatureState.Running) == InventoryTidyModule.TidyUiLifecycleAction.Inject,
                        "九态：Running 新开页=注入");
                    Check(InventoryTidyModule.DecideTidyUiAction(FeatureState.Disabled) == InventoryTidyModule.TidyUiLifecycleAction.RemoveExisting
                        && InventoryTidyModule.DecideTidyUiAction(FeatureState.Stopped) == InventoryTidyModule.TidyUiLifecycleAction.RemoveExisting
                        && InventoryTidyModule.DecideTidyUiAction(FeatureState.Isolated) == InventoryTidyModule.TidyUiLifecycleAction.RemoveExisting
                        && InventoryTidyModule.DecideTidyUiAction(FeatureState.Incompatible) == InventoryTidyModule.TidyUiLifecycleAction.RemoveExisting,
                        "九态：Disabled/Stopped/Isolated/Incompatible=拆除已有按钮");
                    Check(InventoryTidyModule.DecideTidyUiAction(FeatureState.Starting) == InventoryTidyModule.TidyUiLifecycleAction.KeepWithoutNew
                        && InventoryTidyModule.DecideTidyUiAction(FeatureState.Stopping) == InventoryTidyModule.TidyUiLifecycleAction.KeepWithoutNew
                        && InventoryTidyModule.DecideTidyUiAction(FeatureState.Isolating) == InventoryTidyModule.TidyUiLifecycleAction.KeepWithoutNew
                        && InventoryTidyModule.DecideTidyUiAction(FeatureState.Discovered) == InventoryTidyModule.TidyUiLifecycleAction.KeepWithoutNew,
                        "九态：Starting/Stopping/Isolating/Discovered=不新增不拆除（过渡暂留）");

                    var lifetime = new FakeTidyLifetime { State = FeatureState.Stopping };
                    var servingView = new FakeTidySettingsView();
                    servingView.SetEntry("inventorytidy.mode", SettingValue.Choice("同类"));
                    servingView.SetEntry("inventorytidy.direction", SettingValue.Choice("降序"));
                    var module = StartLitModuleWith(lifetime, servingView);
                    LitTidyProductionAuthority.ServerRoleProbeForTests = () => true;
                    try
                    {
                        MainThreadDispatcher.ResetForTests();
                        Check(module.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.NativeFallback,
                            "九态：Stopping 点击=原生回退（不报假成功）");
                        Check(MainThreadDispatcher.PendingCount == 0,
                            "九态：Stopping 点击零派发");
                        lifetime.State = FeatureState.Running;
                        Check(module.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.Dispatched,
                            "九态：Running 点击=派发（同一模块代际，状态事实翻转即恢复服务）");
                        Check(MainThreadDispatcher.PendingCount == 1,
                            "九态：Running 点击恰一个工作项入队");
                        MainThreadDispatcher.ResetForTests();

                        var bareModule = StartLitModuleWith(null, new FakeTidySettingsView());
                        Check(bareModule.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.NativeFallback,
                            "九态：无生命周期事实（view 缺席）点击=fail-closed 原生回退");
                        MainThreadDispatcher.ResetForTests();

                        // U3DS 不武装（T1 Q17）：headless 决策下补丁不装，诊断
                        // 如实落 StartGateDiagnostics（决策门禁，非异常门禁）。
                        BueRuntimeCompletionChain.HeadlessDecision = true;
                        try
                        {
                            var headless = StartLitModuleWith(null, new FakeTidySettingsView());
                            Check(!headless.PatchesInstalled && headless.StartGateDiagnostics == "headless-ui-not-armed",
                                "九态：U3DS headless 不武装整理按钮补丁（决策门禁非异常门禁）");
                        }
                        finally { BueRuntimeCompletionChain.HeadlessDecision = false; }
                    }
                    finally { LitTidyProductionAuthority.ServerRoleProbeForTests = null; }
                });

                // 点击只读同一 revision 的已保存 ClientPreference 快照（Q59）：
                // DEV-V5-02：一次 GetSnapshot 供出 direction 收尾偏好（旧 mode 条
                // 目残留=读取忽略、绝不拦点击）；未知 direction/schema 缺项/
                // view 缺席=诚实拒绝（禁止拼出从未存在过的组合）；每次点击现读
                // 快照（保存后下一次点击即用新值，不要求重画标题栏）。
                Group("DEV-V4-06 点击读同一 revision 快照", () =>
                {
                    var view = new FakeTidySettingsView();
                    view.SetEntry("inventorytidy.mode", SettingValue.Choice("大件"));
                    view.SetEntry("inventorytidy.direction", SettingValue.Choice("升序"));
                    var lifetime = new FakeTidyLifetime { State = FeatureState.Running };
                    var module = StartLitModuleWith(lifetime, view);
                    LitTidyProductionAuthority.ServerRoleProbeForTests = () => true;
                    try
                    {
                        Check(module.TryReadSavedTidyPreference(out var savedMode, out var savedDescending, out var savedRevision)
                            && savedMode == TidyMode.SameType && !savedDescending && savedRevision == 7U,
                            "快照：一次读取拿到 direction=升序(false) 且携带 store revision；mode 是线协议占位（DEV-V5-02）");
                        Check(view.GetSnapshotCalls == 1,
                            "快照：同一 revision 规则=恰一次 GetSnapshot（两次读取拼装=违例）");
                        MainThreadDispatcher.ResetForTests();
                        Check(module.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.Dispatched,
                            "快照：点击派发成功（点击只读已保存快照，不读面板草稿）");
                        Check(view.GetSnapshotCalls == 2 && MainThreadDispatcher.PendingCount == 1,
                            "快照：每次点击现读快照（不缓存上一次点击，保存后下一次点击即新值）");
                        MainThreadDispatcher.ResetForTests();

                        view.SetEntry("inventorytidy.direction", SettingValue.Choice("从小到大"));
                        Check(!module.TryReadSavedTidyPreference(out _, out _, out _),
                            "快照：未知 direction 字面量=拒绝读取（不发明映射）");
                        Check(module.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.RejectedPreferenceUnavailable,
                            "快照：未知 direction 点击=显式拒绝（RejectedPreferenceUnavailable），不假成功");
                        MainThreadDispatcher.ResetForTests();

                        var partialView = new FakeTidySettingsView();
                        partialView.SetEntry("inventorytidy.mode", SettingValue.Choice("同类"));
                        var partialModule = StartLitModuleWith(lifetime, partialView);
                        Check(!partialModule.TryReadSavedTidyPreference(out _, out _, out _),
                            "快照：方向条目缺席（schema 漂移）=拒绝读取，不落默认值拼装");
                        Check(partialModule.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.RejectedPreferenceUnavailable,
                            "快照：schema 缺项点击=显式拒绝");
                        MainThreadDispatcher.ResetForTests();

                        var noViewModule = StartLitModuleWith(lifetime, null);
                        Check(!noViewModule.TryReadSavedTidyPreference(out _, out _, out _),
                            "快照：无注入 view=拒绝读取");
                        Check(noViewModule.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.RejectedPreferenceUnavailable,
                            "快照：无 view 点击=显式拒绝（生命周期 Running 也不猜默认值）");
                        MainThreadDispatcher.ResetForTests();
                    }
                    finally { LitTidyProductionAuthority.ServerRoleProbeForTests = null; }
                });

                // 接线级锚：点击→已保存快照→RequestTidy→真实协议→服务端权威。
                // 服务端权威收到的 desc 必须与快照一致（Q59 全链）；DEV-V5-02：
                // mode 字段以占位值传输（旧协议字段保留、不再决定算法），保存新
                // direction 后下一次点击即用新快照（不重画标题栏）。
                Group("DEV-V4-06 点击经真实协议用已保存快照", () =>
                {
                    var view = new FakeTidySettingsView();
                    view.SetEntry("inventorytidy.mode", SettingValue.Choice("大件"));
                    view.SetEntry("inventorytidy.direction", SettingValue.Choice("升序"));
                    var lifetime = new FakeTidyLifetime { State = FeatureState.Running };
                    var harness = LitMultiplayerHarness.Create(NewLitFaultDirectory(), clientLifetime: lifetime);
                    var client = harness.ClientModule;
                    client.AttachSettingsView(view);
                    harness.Handshake();
                    harness.EstablishChallenge();
                    Check(client.RequestTidyFromUiClick(3, allPages: false) == LitTidyRequestResult.Dispatched,
                        "接线：点击=派发（快照 direction=升序）");
                    var deadline = DateTime.UtcNow.AddSeconds(10);
                    while (harness.ServerAuthority.ExecuteCount == 0 && DateTime.UtcNow < deadline)
                    {
                        harness.TickBoth();
                        harness.Pump();
                    }
                    Check(harness.ServerAuthority.ExecuteCount == 1 && harness.ServerAuthority.LastPage == 3
                        && harness.ServerAuthority.LastMode == TidyMode.SameType && !harness.ServerAuthority.LastSortDescending,
                        "接线：服务端权威收到 page 3 + 升序；mode 字段=线协议占位 SameType（旧档位值不再跨端决定算法）");

                    view.SetEntry("inventorytidy.direction", SettingValue.Choice("降序"));
                    Check(client.RequestTidyFromUiClick(LitRuntime.AllPages, allPages: true) == LitTidyRequestResult.Dispatched,
                        "接线：Ctrl 语义（allPages）派发，收尾方向按保存后的新值");
                    deadline = DateTime.UtcNow.AddSeconds(10);
                    while (harness.ServerAuthority.ExecuteCount == 1 && DateTime.UtcNow < deadline)
                    {
                        harness.TickBoth();
                        harness.Pump();
                    }
                    Check(harness.ServerAuthority.ExecuteCount == 2
                        && harness.ServerAuthority.LastMode == TidyMode.SameType && harness.ServerAuthority.LastSortDescending,
                        "接线：下一次点击即用新保存快照（降序），无需重画标题栏（Q59）");
                });

                // 停用/隔离拆除（Q55）：Stop 阶段 3 执行拆除（移除按钮对象+
                // 解绑+清 patch 自持引用，幂等）；隔离走生命周期机不调 module.
                // Stop——拆除挂在模块向 IFeatureLifetime.TryTrack 登记的销毁句柄
                // 上，宿主撤回（Withdraw）即触发同一拆除路径。宿主无 Glazier，
                // 移除操作经 RemoveChildForTests 注入缝观察（与模块的
                // NetServiceFactoryForTests 同类宿主缝）。移除失败=引用保留+
                // BUE-LIT-TEARDOWN 留痕+下次拆除重试（Q55 红线：不把停用伪装
                // 成拆除成功）。
                Group("DEV-V4-06 停用/隔离拆除已注入按钮", () =>
                {
                    var lifetime = new FakeTidyLifetime { State = FeatureState.Running };
                    var module = StartLitModuleWith(lifetime, new FakeTidySettingsView());
                    var removed = new List<byte>();
                    var removeResult = true;
                    InventoryTidyUiPatch.RemoveChildForTests = (header, button) =>
                    {
                        if (!removeResult) return false;
                        removed.Add((byte)button);
                        return true;
                    };
                    try
                    {
                        for (byte page = 2; page <= 6; page++)
                            InventoryTidyUiPatch.TrackButtonForTests(page, page, null, null);
                        Check(InventoryTidyUiPatch.HasTrackedButtons,
                            "拆除 setup：五页按钮引用在册（headers[0..4]=page 2..6）");

                        module.Stop(FeatureStopReason.UserDisabled);
                        Check(removed.Count == 5,
                            "拆除：Stop（UserDisabled）对每个在册按钮对象执行一次移除（页 2..6 各一）");
                        Check(!InventoryTidyUiPatch.HasTrackedButtons,
                            "拆除：Stop 后 patch 自持引用清空（停用不残留可点按钮状态）");
                        InventoryTidyUiPatch.RemoveInjectedButtons();
                        Check(removed.Count == 5,
                            "拆除：重复拆除=空操作（幂等，不移除原生标题栏内容）");

                        // 移除失败路径：引用保留（对象仍在 UI 树上），显式失败
                        // 留痕，不静默清引用伪装成功；下次拆除重试成功后引用清空。
                        for (byte page = 2; page <= 6; page++)
                            InventoryTidyUiPatch.TrackButtonForTests(page, page, null, null);
                        removeResult = false;
                        InventoryTidyUiPatch.RemoveInjectedButtons();
                        Check(removed.Count == 5 && InventoryTidyUiPatch.HasTrackedButtons,
                            "拆除失败：引用保留不清空（对象仍在 UI 树，清引用=伪装拆除成功）");
                        removeResult = true;
                        InventoryTidyUiPatch.RemoveInjectedButtons();
                        Check(removed.Count == 10 && !InventoryTidyUiPatch.HasTrackedButtons,
                            "拆除失败：下一次拆除重试成功（同一拆除路径自愈，五页按钮全部移除）");

                        for (byte page = 2; page <= 6; page++)
                            InventoryTidyUiPatch.TrackButtonForTests(page, page, null, null);
                        Check(InventoryTidyUiPatch.HasTrackedButtons && removed.Count == 10,
                            "隔离 setup：按钮引用重新在册、撤回尚未发生");
                        Check(lifetime.Tracked.Count >= 1,
                            "隔离 setup：模块向生命周期缝登记了代际资源句柄");
                        for (var index = 0; index < lifetime.Tracked.Count; index++)
                            lifetime.Tracked[index].Dispose();
                        Check(removed.Count == 15,
                            "隔离：生命周期撤回（Dispose）触发同一拆除路径（机不调 Stop 也拆干净）");
                        Check(!InventoryTidyUiPatch.HasTrackedButtons,
                            "隔离：撤回后 patch 自持引用清空");
                    }
                    finally
                    {
                        InventoryTidyUiPatch.RemoveChildForTests = null;
                        InventoryTidyUiPatch.RemoveInjectedButtons();
                    }
                });

                // DEV-V4-09 F1（实机缺陷）：PlayerDashboardInventoryUI 构造函数整个
                // 会话只运行一次——停用拆除按钮+撤销补丁后，再启用永远等不到下一个
                // 构造事件，按钮永不复装（实机：停用→保存→按钮消失 ✓；再启用→
                // 保存→按钮不再出现 ✗，重启游戏才恢复）。修复语义：模块每次 Start
                // 尾部对「仍存活」的仪表盘立即重注入——同一生命周期门禁（仅 Running）
                // + 在册去重 + 未武装不画死按钮。宿主无 Glazier：headers 经
                // HeadersForTests 缝提供、单按钮注入经 InjectButtonForTests 缝观察
                // （与 RemoveChildForTests 同类宿主缝）。
                // DEV-V4-09 F1 实机二轮（修复 v2 无效的取证）：Start 运行在机器
                // Starting 态，九态门（过渡不新增）必然拒绝 Start 期补注入——v2 的
                // Start 尾部重注入在真实机器时序下永远被自己的门禁拒绝（实机
                // 194456 包：gen8 启动后零 [TidyUI] 注入行）。修复=注入尝试挂模块
                // 每 tick 泵（16 拍节流首拍即试）：重启用落地 Running 后按钮自动
                // 复装，背包开着也当场出现。缝先行设置（宿主游戏程序集可加载，
                // 不设缝会走生产反射路径向真实 Glazier 注入）。
                Group("DEV-V4-09 停用→再启用经模块泵实时重注入", () =>
                {
                    var injectedPages = new List<byte>();
                    InventoryTidyUiPatch.HeadersForTests = () => new object[5] { new object(), new object(), new object(), new object(), new object() };
                    InventoryTidyUiPatch.InjectButtonForTests = (page, header) => { injectedPages.Add(page); return true; };
                    InventoryTidyUiPatch.RemoveChildForTests = (header, button) => true;
                    var lifetime1 = new FakeTidyLifetime { State = FeatureState.Running };
                    var module = StartLitModuleWith(lifetime1, new FakeTidySettingsView());
                    try
                    {
                        Check(injectedPages.Count == 0,
                            "实时注入：Start 期不注入（真实机器 Starting 态门禁会拒；注入归模块泵）");
                        PumpTidyModule(module);
                        Check(injectedPages.Count == 5 && injectedPages[0] == 2 && injectedPages[4] == 6,
                            "实时注入：Running 事实下模块泵对存活仪表盘注入五页（headers[0..4]→page 2..6）");
                        Check(InventoryTidyUiPatch.HasTrackedButtons,
                            "实时注入：按钮引用在册（拆除责任登记随注入恢复）");

                        // 用户实机场景：停用（拆除+清册）→ 再启用 → 泵立即重注入。
                        module.Stop(FeatureStopReason.UserDisabled);
                        Check(!InventoryTidyUiPatch.HasTrackedButtons && injectedPages.Count == 5,
                            "实时注入 setup：停用拆除清册（不再新增注入）");
                        injectedPages.Clear();

                        var lit = new FeatureId(LitRuntime.FeatureIdValue);
                        var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                        var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                        var network = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, new ContractVersion(2, 0), 2202UL);
                        var lifetime2 = new FakeTidyLifetime { State = FeatureState.Running };
                        var restarted = module.Start(new FeatureBootstrap(default(FeatureScopeIdentity), 10UL, new FakeTidySettingsView(),
                            bus.Subscriber(lit), bus.Publisher(lit), bus.EventRegistry(lit), null, null, lifetime2, network));
                        Check(restarted.Started && injectedPages.Count == 0,
                            "实时注入 setup：同实例再启动成功且 Start 期不注入");
                        PumpTidyModule(module);
                        Check(injectedPages.Count == 5 && injectedPages[0] == 2 && injectedPages[4] == 6,
                            "实时注入：停用后再启用由模块泵立即重注入五页（不再等永不复跑的构造事件）");
                        Check(InventoryTidyUiPatch.HasTrackedButtons,
                            "实时注入：按钮引用重新在册（拆除责任登记随注入恢复）");

                        var beforeDedupe = injectedPages.Count;
                        for (var dedupeTick = 0; dedupeTick < 4; dedupeTick++) module.Tick();
                        Check(injectedPages.Count == beforeDedupe,
                            "实时注入：已在册页不重复注入（在册去重，不画双按钮）");

                        // DEV-V4-09（用户预警「日志要刷疯了」）：全页在册后泵尝试必须
                        // **静默短路**——不打注入完成行、不走反射解析。16 拍一次 ≈
                        // 60fps 下每秒 3.75 次，稳态刷日志=小时万行级纯噪音（项目
                        // 在 Phase-3 抓过 63 行/秒风暴，同类缺陷零容忍）。
                        var previousSink = LitRuntime.LogSink;
                        var tidyLines = new List<string>();
                        LitRuntime.LogSink = line => { if (line != null && line.IndexOf("[TidyUI]", StringComparison.Ordinal) >= 0) tidyLines.Add(line); };
                        try
                        {
                            PumpTidyModule(module);
                            Check(tidyLines.Count == 0,
                                "实时注入：全页在册后泵静默短路（稳态零 [TidyUI] 日志行，真做事才打日志）");
                        }
                        finally
                        {
                            LitRuntime.LogSink = previousSink;
                        }

                        injectedPages.Clear();
                        var bus3 = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                        var pair3 = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                        var network3 = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair3.First, new ContractVersion(2, 0), 2203UL);
                        var lifetime3 = new FakeTidyLifetime { State = FeatureState.Stopped };
                        var stoppedStart = module.Start(new FeatureBootstrap(default(FeatureScopeIdentity), 11UL, new FakeTidySettingsView(),
                            bus3.Subscriber(lit), bus3.Publisher(lit), bus3.EventRegistry(lit), null, null, lifetime3, network3));
                        PumpTidyModule(module);
                        Check(stoppedStart.Started && injectedPages.Count == 0,
                            "实时注入：非 Running 生命周期事实泵不注入（同一九态门，不画死按钮）");
                    }
                    finally
                    {
                        InventoryTidyUiPatch.HeadersForTests = null;
                        InventoryTidyUiPatch.InjectButtonForTests = null;
                        InventoryTidyUiPatch.RemoveChildForTests = (header, button) => true;
                        InventoryTidyUiPatch.RemoveInjectedButtons();
                        InventoryTidyUiPatch.RemoveChildForTests = null;
                        module.Stop(FeatureStopReason.PluginStopping);
                    }
                });

                // DEV-V4-09 F1b（实机取证盲区）：Stop 阶段 3 解绑生产日志缝
                // （LitRuntime.LogSink=null，插件卸载卫生语义），但 Start 从不重绑
                // ——首次停用→再启用后 [Tidy]/[TidyUI]/注入诊断全部失明（实机
                // gen8/gen9 重启零 [Tidy] 行，注入路径对取证不可见）。Start 必须
                // 重绑；Stop 的解绑语义保持不变。
                Group("DEV-V4-09 停止解绑日志缝后 Start 重绑", () =>
                {
                    LitRuntime.LogSink = null;
                    LitRuntime.ErrorLogSink = null;
                    var lifetime = new FakeTidyLifetime { State = FeatureState.Running };
                    var module = StartLitModuleWith(lifetime, new FakeTidySettingsView());
                    try
                    {
                        Check(LitRuntime.LogSink != null && LitRuntime.ErrorLogSink != null,
                            "日志缝：Start 重绑生产日志缝（重启后 [Tidy]/[TidyUI] 诊断不失明）");
                    }
                    finally
                    {
                        module.Stop(FeatureStopReason.PluginStopping);
                    }
                    Check(LitRuntime.LogSink == null && LitRuntime.ErrorLogSink == null,
                        "日志缝：Stop 仍解绑（插件卸载卫生语义不变）");
                });

                // T1 检验点②（官方先行消费）：LIT 设置页真实消费两条 Choice——
                // 真实注册、真实目录、真实组合根：两行 Cycle（档位/默认=冻结
                // 值），草稿循环切换→保存→模块的注入 view（点击将读的同一权威
                // 源）立刻读到新值；enabled 不在任何行里（与 DEV-V4-04 对拍）。
                Group("DEV-V4-06 LIT 设置页真实消费两条 Choice（T1 检验点②）", () =>
                {
                    EnsureSettings();
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    Check(BueRuntimeHost.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                        "消费锚 setup：真实 LIT 注册受理（DEV-V5-02：facet=一条 Choice）");
                    Check(runtime.CompleteRuntime(), "消费锚 setup：目录冻结");
                    BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(3601UL));
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        var model = composition.ManagementPanel.Model;
                        model.OpenDetail(litFeature.Value);
                        var rows = model.GetSettingRows(litFeature.Value);
                        Check(rows.Count == 1, "消费锚：LIT 详情页恰一行（enabled 与 mode 三档均退役不占行）");
                        var hasEnabledRow = false;
                        var hasModeRow = false;
                        PanelSettingRowView directionRow = default(PanelSettingRowView);
                        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                        {
                            if (rows[rowIndex].SettingId == "inventorytidy.enabled") hasEnabledRow = true;
                            if (rows[rowIndex].SettingId == "inventorytidy.mode") hasModeRow = true;
                            if (rows[rowIndex].SettingId == "inventorytidy.direction") directionRow = rows[rowIndex];
                        }
                        Check(!hasEnabledRow, "消费锚：enabled 不在行投影（退役总开关不复画）");
                        Check(!hasModeRow, "消费锚：整理模式行不在投影——同类/空间/大件不再出现在玩家算法档（DEV-V5-02 验收钉）");
                        Check(directionRow.DisplayName == "整理方向",
                            "消费锚：显示名=Q56 冻结文案（整理方向，不再暴露内部键名）");
                        Check(directionRow.ControlKind == PanelSettingControlKind.Cycle && directionRow.Kind == SettingKind.Choice,
                            "消费锚：整理方向行=Choice→Cycle 控件（02 行投影真实消费官方 Choice）");
                        Check(directionRow.AllowedValues.Count == 2 && directionRow.AllowedValues[0] == "降序"
                            && directionRow.AllowedValues[1] == "升序",
                            "消费锚：整理方向档位=降序/升序（Q56 冻结；收尾偏好语义见 07 对拍描述句钉）");
                        Check(directionRow.EffectiveValue.Text == "降序",
                            "消费锚：默认值=降序（快照生效值，空存储即默认）");
                        Check(directionRow.Authority == SettingAuthority.ClientLocal,
                            "消费锚：Choice 作用域=ClientPreference（ClientLocal，不进 ServerAuthority）");

                        Check(model.DraftCycleBueSetting("inventorytidy.direction", 1) && model.IsDirty,
                            "消费锚：循环切换整理方向进草稿（降序→升序，不立即写入）");
                        var save = model.SaveDraft();
                        Check(save.Outcome == DraftSaveOutcome.Success,
                            "消费锚：草稿保存受理（设置提交原子缝，整单成败）");
                        var wired = InventoryTidyFeatureRegistration.WiredModule;
                        Check(wired != null && wired.SettingsView != null,
                            "消费锚 setup：WiredModule 持宿主注入 view");
                        var afterSave = wired.SettingsView.GetSnapshot(SettingRevisionScope.ClientPreference);
                        var savedDirection = string.Empty;
                        for (var entryIndex = 0; entryIndex < afterSave.Entries.Count; entryIndex++)
                            if (afterSave.Entries[entryIndex].SettingId == "inventorytidy.direction")
                                savedDirection = afterSave.Entries[entryIndex].EffectiveValue.Text;
                        Check(savedDirection == "升序",
                            "消费锚：保存后模块注入 view 读到新值（点击将用的同一权威源，revision 推进）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });

                // ── DEV-V4-07：官方文案、设置中文与 NoOp Choice（spec「官方文案与 NoOp
                // （V4-T6 → DEV-V4-07）」+ Q60–Q64）。三缝分开：chrome 对照表（面板 chrome，
                // 非契约）、设置显示名/描述（Q61/Q62/Q64 逐字，全部经面板行投影缝断言，
                // 不直读描述符/注册面——Testing Decisions「只测外显行为」）、运行时状态
                // 投影（05 已落，本票不动）。候选纪律=不授候选不加 RELEASES 不授 CaseId。──

                Group("DEV-V4-07 NoOp 探针档位 Cycle 改草稿（T1 检验点③：NoOp 描述+Toggle+Choice）", () =>
                {
                    EnsureSettings();
                    var noopFeature = new FeatureId("io.github.yu80rice.bue.noop");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    Check(runtime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted,
                        "锚③ setup：NoOp 样板登记受理（同公共桥同路由）");
                    Check(runtime.CompleteRuntime(), "锚③ setup：目录冻结");
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        var model = composition.ManagementPanel.Model;
                        Check(model.GetFeatureDescription(noopFeature.Value)
                                == "生态接入样板，用于展示功能描述、Toggle 和 Choice 在面板中的呈现。",
                            "锚③：NoOp 功能级一句话经 chrome 对照表投影可达（面板 chrome，非契约成员）");
                        model.OpenDetail(noopFeature.Value);
                        var rows = model.GetSettingRows(noopFeature.Value);
                        Check(rows.Count == 2, "锚③：NoOp 详情恰两行（Toggle+Choice 供生态作者对照）");
                        PanelSettingRowView toggleRow = default(PanelSettingRowView);
                        PanelSettingRowView choiceRow = default(PanelSettingRowView);
                        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                        {
                            if (rows[rowIndex].SettingId == "noop.probe-toggle") toggleRow = rows[rowIndex];
                            if (rows[rowIndex].SettingId == "noop.probe-choice") choiceRow = rows[rowIndex];
                        }
                        Check(toggleRow.DisplayName == "探针开关"
                                && toggleRow.Description == "样板用的开关，证明生态 Toggle 能出现在面板。"
                                && toggleRow.ControlKind == PanelSettingControlKind.Toggle
                                && toggleRow.EffectiveValue.Boolean,
                            "锚③：探针开关行=中文文案+Toggle 控件+默认开（Q64 冻结原文逐字）");
                        Check(choiceRow.DisplayName == "探针档位"
                                && choiceRow.Description == "样板用的循环切换，证明生态 Choice 能出现在面板。"
                                && choiceRow.ControlKind == PanelSettingControlKind.Cycle
                                && choiceRow.AllowedValues.Count == 2 && choiceRow.AllowedValues[0] == "甲" && choiceRow.AllowedValues[1] == "乙"
                                && choiceRow.EffectiveValue.Text == "甲",
                            "锚③：探针档位行=中文文案+Cycle 控件+档位甲/乙+默认甲");
                        Check(model.DraftCycleBueSetting("noop.probe-choice", 1) && model.IsDirty,
                            "锚③：左键下一档（甲→乙）进草稿即脏");
                        var store = BetterUnturnedExperience.Plugin.BueSettingsRuntime.Registry;
                        SettingValue during; uint duringRevision;
                        Check(store.TryGetRuntime(noopFeature).TryGet("noop.probe-choice", out during, out duringRevision)
                                && during.Text == "甲",
                            "锚③：改档位不立刻写权威源（宿主 runtime 仍甲）");
                        Check(model.DraftCycleBueSetting("noop.probe-choice", -1) && !model.IsDirty,
                            "锚③：同一草稿会话内右键拨回默认档（乙→甲）不再脏（Q41）");
                        Check(model.DraftCycleBueSetting("noop.probe-choice", 1) && model.IsDirty,
                            "锚③：再次左键（甲→乙）待保存");
                        var report = model.SaveDraft();
                        Check(report.Outcome == DraftSaveOutcome.Success && report.PrimaryMessage == "配置已保存。",
                            "锚③：保存成功文案（Choice 走一次原子 Submit）");
                        SettingValue after; uint afterRevision;
                        Check(store.TryGetRuntime(noopFeature).TryGet("noop.probe-choice", out after, out afterRevision)
                                && after.Text == "乙" && afterRevision == duringRevision + 1,
                            "锚③：保存后宿主 runtime 读到乙（revision 单次推进）");
                        var noopSave = model.SaveDraft();
                        Check(noopSave.Outcome == DraftSaveOutcome.NoChanges && noopSave.PrimaryMessage == "没有需要保存的修改。",
                            "锚③：不脏保存=空操作文案（不误写盘）");
                    }
                    finally
                    {
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });

                Group("DEV-V4-07 行投影 Q61 描述逐字与退役 enabled 对拍（与 DEV-V4-04 对拍）", () =>
                {
                    // 文案与退役对拍全部经面板行投影缝（GetSettingRows）断言，
                    // 不直读描述符/注册面（Testing Decisions「只测外显行为」）。
                    // BII 的行级对拍在 ClientUi.Tests DevV4OfficialCopyTests（真实
                    // BII 编辑器）；此处锚 LIT+NoOp 的真实宿主路由。
                    EnsureSettings();
                    var lit = new FeatureId("io.github.yu80rice.bue.inventory-tidy");
                    var noopFeature = new FeatureId("io.github.yu80rice.bue.noop");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var runtime = new FeatureRegistrationRuntime();
                    BueRuntimeHost.Bind(runtime);
                    runtime.OpenRegistration();
                    Check(BueRuntimeHost.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                        "07 对拍 setup：真实 LIT 注册受理（facet=两条 Choice）");
                    Check(runtime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted,
                        "07 对拍 setup：NoOp 样板登记受理");
                    Check(runtime.CompleteRuntime(), "07 对拍 setup：目录冻结");
                    BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(3602UL));
                    var composition = new BueClientUiCompositionRoot();
                    try
                    {
                        composition.RefreshManagementPanel();
                        var model = composition.ManagementPanel.Model;
                        model.OpenDetail(lit.Value);
                        var litRows = model.GetSettingRows(lit.Value);
                        Check(litRows.Count == 1, "07→V5-02：LIT 详情恰一行（enabled 与 mode 三档均退役不占行）");
                        var litHasMode = false;
                        var litDirectionDescription = string.Empty;
                        var litHasEnabled = false;
                        for (var i = 0; i < litRows.Count; i++)
                        {
                            if (litRows[i].SettingId == "inventorytidy.mode") litHasMode = true;
                            if (litRows[i].SettingId == "inventorytidy.direction") litDirectionDescription = litRows[i].Description;
                            if (string.Equals(litRows[i].SettingId, "inventorytidy.enabled", StringComparison.Ordinal)) litHasEnabled = true;
                        }
                        Check(!litHasMode, "07→V5-02：整理模式行已退役（Q61 原文随三档一起退场，经行投影缝）");
                        Check(litDirectionDescription == "降序/升序只影响完全相同条件物品的收尾摆放顺序，不改变统一分段排版的结果。",
                            "07→V5-02：整理方向描述=统一排版冻结新句逐字（经行投影缝，不直读描述符）");
                        Check(!litHasEnabled, "07：LIT 行投影无 inventorytidy.enabled（退役键不因文案票回潮，与 04 对拍）");
                        model.OpenDetail(noopFeature.Value);
                        var noopRows = model.GetSettingRows(noopFeature.Value);
                        var noopHasEnabled = false;
                        for (var i = 0; i < noopRows.Count; i++)
                            if (noopRows[i].SettingId.IndexOf("enabled", StringComparison.OrdinalIgnoreCase) >= 0) noopHasEnabled = true;
                        Check(noopRows.Count == 2 && !noopHasEnabled,
                            "07：NoOp 恰两行（Toggle+Choice）且无 enabled 别名行（两探针均非生命周期代理，Q64：不登记 legacy alias）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        composition.Destroy();
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                });
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            finally
            {
                BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                for (var index = 0; index < settingsRoots.Count; index++)
                {
                    try { if (System.IO.Directory.Exists(settingsRoots[index])) System.IO.Directory.Delete(settingsRoots[index], true); }
                    catch (Exception) { }
                }
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V3-06 settings collection: ALL GREEN (0 failures) — groups: 面板目录路由/不伪造页/矩阵两侧/官方与生态并列/登记 facet 侧/官方先行消费真实 LIT/生态对照 NoOp/面板启停命令适配器/DEV-V4-07 官方文案与 NoOp Choice");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V3-06 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V3-07: BueDiagnostics 统一诊断。可用性矩阵行 Logger=DEV-V3-07 后可用
        // 的两侧红线（真实 StartCatalog 组合永非 null；手工未接线组装=null 阶段
        // 基线，模块须容忍）；三方法窄面→统一结构化行（Info/Warning/Error 级别
        // 映射进 LogOutput 正常可见级别，不被静默过滤）→按 (FeatureId,
        // DiagnosticId) 聚合的有界摘要（容量 128 本票定值、摘要行限频 30000ms
        // 本票定值、字符串消毒截断、重启不持久）；BUE-* 保留前缀纪律（保留段外
        // 身份冒用=拒绝写入+诊断，治理哲学=T2 保留段）；停止/隔离/宿主 shutdown
        // 边界后写入=拒（BUE-LOG-004 留痕，再启用新代际恢复）；Logger 内部故障
        // 不反噬模块（BUE-LOG-005 隔离+fallback 留痕）；T4 隔离/T5 链路健康/状态
        // 投影经生产 sink 双绑收编进统一摘要（原行路径逐字不变，分 seam 判据可
        // 定位互不遮蔽）；官方先行消费锚=真实 LIT 启停诊断行经注入 view；NoOp
        // 生态对照支线。零新增契约面（IFeatureLogger 三方法形状入 Contracts 锚）。
        private static void AssertBueV3DiagnosticsWiringAndSummary(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            var settingsRoots = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                string EnsureSettings()
                {
                    // 每子组确定性临时持久根（06 组先例）：StartCatalog 的
                    // Settings 组合不落到生产根。
                    var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BUE-V3-07-" + Guid.NewGuid().ToString("N"));
                    settingsRoots.Add(root);
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, () => true, null);
                    return root;
                }

                Group("矩阵 Logger 接线两侧", () =>
                {
                    // 接线后可用侧：真实 StartCatalog 组装 Logger 永非 null，经
                    // view 写入=同 LogOutput 缝的结构化行+摘要计数；阶段基线侧：
                    // 手工未接线组装 Logger=null（04/06 先例，模块容忍 null 的
                    // 纪律继续有锚）。
                    var probe = new MatrixProbeRegistration("io.example.diagnostics-matrix");
                    var probeRuntime = new FeatureRegistrationRuntime();
                    probeRuntime.OpenRegistration();
                    Check(probeRuntime.Register(probe).Accepted, "setup: 矩阵探针受理");
                    Check(probeRuntime.CompleteRuntime(), "setup: 探针目录冻结");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var previousRecorder = BueRuntimeLog.Recorder;
                    var lines = new List<string>();
                    BueRuntimeHost.Bind(probeRuntime);
                    BueRuntimeLog.Recorder = lines.Add;
                    try
                    {
                        // 组间卫生：前序票组的 StopAll(PluginStopping) 会给已组合
                        // 聚合器落下宿主停止账——本票组从干净代际账起跑（04/06
                        // Clear 先例）。
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(3301UL));
                        var captured = probe.Module.Bootstrap;
                        Check(captured != null && captured.Logger != null,
                            "矩阵接线侧：真实 StartCatalog 组装 Logger view（可用性矩阵行=DEV-V3-07 后可用兑现）");
                        captured.Logger.Info("matrix-probe-info", "io.example.diagnostics-matrix-001");
                        Check(lines.Exists(l => l.StartsWith("Info ") && l.Contains("[BUE-DIAG]")
                                && l.Contains("feature=io.example.diagnostics-matrix")
                                && l.Contains("event=matrix-probe-info") && l.Contains("level=info")
                                && l.Contains("diagnosticId=io.example.diagnostics-matrix-001")),
                            "矩阵接线侧：经 view 写入=进同一 LogOutput 的结构化行（Info 级别正常播放可见，不被静默过滤）");
                        BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                        Check(BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                "io.example.diagnostics-matrix", "io.example.diagnostics-matrix-001", out entry)
                                && entry.Count == 1L && entry.Level == BetterUnturnedExperience.Core.Diagnostics.DiagnosticLevel.Info,
                            "矩阵接线侧：摘要按 (FeatureId,DiagnosticId) 计数（count=1 level=Info）");
                        var manual = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, null, null, null, null, null, null, null,
                            NewLoopbackNetwork(3302UL));
                        Check(manual.Logger == null,
                            "矩阵 null 侧：未接线手工组装 Logger=null（阶段基线——模块须容忍 null，接线前语义保持）");
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    }
                    finally
                    {
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                });

                Group("三方法窄面与级别映射", () =>
                {
                    // 契约冻结的 Info/Warning/Error 三方法逐个核：级别 token、
                    // error= 仅 Warning/Error 携带、Error 的 fault= 含异常类型+
                    // 消息不含堆栈（不保存敏感 payload）、条目级别=max(所见)、
                    // 摘要行首见即一条（限频规则=下一组）。null exception 的
                    // Error=合法写入无 fault 段。
                    var lines = new List<string>();
                    long utcMs = 1700000000000L;
                    long monoMs = 1000L;
                    var diagnostics = new BetterUnturnedExperience.Core.Diagnostics.DiagnosticRuntime(
                        (line, level) => lines.Add(level + " " + line), () => utcMs, () => monoMs, null);
                    var feature = "io.example.diagnostics-narrow";
                    diagnostics.OpenGeneration(feature, 1UL);
                    var view = diagnostics.CreateLoggerView(feature, 1UL);
                    view.Info("narrow-info", feature + "-001");
                    Check(lines.Exists(l => l == "Info [BUE-DIAG] feature=" + feature + " event=narrow-info level=info diagnosticId=" + feature + "-001"),
                        "窄面：Info 行=结构化四字段（feature/event/level/diagnosticId），级别映射 Info");
                    view.Warning("narrow-warn", FrameworkErrorCode.SettingRejected, feature + "-002");
                    Check(lines.Exists(l => l.StartsWith("Warning [BUE-DIAG] ") && l.Contains("event=narrow-warn")
                            && l.Contains("error=SettingRejected") && l.Contains("diagnosticId=" + feature + "-002")),
                        "窄面：Warning 行带 error= 枚举（级别映射 Warning）");
                    view.Error("narrow-error", FrameworkErrorCode.ModuleRuntimeIsolated, feature + "-003",
                        new InvalidOperationException("bad payload"));
                    var errorLine = lines.Find(l => l.Contains("event=narrow-error"));
                    Check(errorLine != null && errorLine.StartsWith("Error ") && errorLine.Contains("error=ModuleRuntimeIsolated")
                            && errorLine.Contains("fault=InvalidOperationException:bad payload")
                            && !errorLine.Contains("at Better"),
                        "窄面：Error 行带 error= 与 fault=类型:消息，不含堆栈文本（不保存敏感 payload）");
                    view.Error("narrow-error-null", FrameworkErrorCode.None, feature + "-004", null);
                    Check(lines.Exists(l => l.Contains("event=narrow-error-null") && !l.Contains("fault=")),
                        "窄面：null exception=合法写入，无 fault 段（不炸不吞）");
                    BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                    Check(diagnostics.TryGetSummaryEntry(feature, feature + "-001", out entry) && entry.Count == 1L
                            && entry.Level == BetterUnturnedExperience.Core.Diagnostics.DiagnosticLevel.Info
                            && entry.FirstSeenUtcMs == 1700000000000L && entry.LastSeenUtcMs == 1700000000000L,
                        "摘要：首见条目 count/level/firstSeen/lastSeen 齐备");
                    Check(diagnostics.TryGetSummaryEntry(feature, feature + "-003", out entry)
                            && entry.Level == BetterUnturnedExperience.Core.Diagnostics.DiagnosticLevel.Error,
                        "摘要：条目级别=所见最高（Error 行记 Error）");
                    var summaryLines = lines.FindAll(l => l.Contains("BUE diagnostic-summary"));
                    Check(summaryLines.Count == 4, "摘要：四条目首见各一条摘要行（BUE diagnostic-summary 前缀冻结格式）");
                    Check(summaryLines.Exists(l => l.Contains("featureId=" + feature) && l.Contains("diagnosticId=" + feature + "-001")
                            && l.Contains("level=info") && l.Contains("count=1") && l.Contains("firstSeen=") && l.Contains("lastSeen=")),
                        "摘要行字段=T8 冻结形状 featureId/diagnosticId/count/firstSeen/lastSeen(+level)");
                });

                Group("BUE-* 前缀纪律", () =>
                {
                    // 票面红线「冒用=拒绝写入+诊断」：保留段外身份（生态）经
                    // view 写 BUE-* → 模块行一条不写+拒绝诊断恰一条（后续再冒用
                    // 静默计数=不炸帧）+条目零聚合；写自身 FeatureId 派生前缀=
                    // 放行。保留段内身份（官方/样例=平台侧，治理哲学=T2：段内
                    // 身份经白名单准入才存在）用 BUE-*=合法（官方先行消费锚即
                    // BUE-LIT-*，本组在 view 层直接证同权）。
                    var probe = new MatrixProbeRegistration("io.example.diagnostics-prefix");
                    var probeRuntime = new FeatureRegistrationRuntime();
                    probeRuntime.OpenRegistration();
                    Check(probeRuntime.Register(probe).Accepted, "setup: 前缀探针受理");
                    Check(probeRuntime.CompleteRuntime(), "setup: 前缀目录冻结");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var previousRecorder = BueRuntimeLog.Recorder;
                    var lines = new List<string>();
                    BueRuntimeHost.Bind(probeRuntime);
                    BueRuntimeLog.Recorder = lines.Add;
                    try
                    {
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(3311UL));
                        var logger = probe.Module.Bootstrap.Logger;
                        Check(logger != null, "setup: 生态探针 Logger 已接线");
                        logger.Warning("stolen-write", FrameworkErrorCode.ModuleStartFailed, "BUE-STOLEN-001");
                        Check(lines.Exists(l => l.StartsWith("Warning ") && l.Contains("[BUE-DIAG]")
                                && l.Contains("event=diagnostic-write") && l.Contains("result=rejected")
                                && l.Contains("reason=reserved-prefix") && l.Contains("attemptedDiagnosticId=BUE-STOLEN-001")
                                && l.Contains("diagnosticId=BUE-LOG-001")),
                            "前缀纪律：生态冒用 BUE-*=拒绝写入+结构化拒绝诊断（BUE-LOG-001 本票定案码）");
                        Check(!lines.Exists(l => l.Contains("event=stolen-write")),
                            "前缀纪律：冒用=模块行一条不写（不是「写了再标记」）");
                        BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                        Check(!BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                "io.example.diagnostics-prefix", "BUE-STOLEN-001", out entry),
                            "前缀纪律：被拒的码零聚合（摘要=运行时诊断可见性，非第二真相也不收冒用者）");
                        logger.Error("stolen-again", FrameworkErrorCode.CoreRuntimeFailure, "BUE-STOLEN-001", null);
                        Check(BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                "io.example.diagnostics-prefix", "BUE-LOG-001", out entry) && entry.Count == 2L,
                            "前缀纪律：再冒用静默计数（拒绝行不重复=不炸帧，episode 计数在摘要）");
                        Check(lines.FindAll(l => l.Contains("reason=reserved-prefix")).Count == 1,
                            "前缀纪律：拒绝诊断行恰一条/冒用码（latch，第二例起只进计数）");
                        logger.Info("own-prefix", "io.example.diagnostics-prefix-001");
                        Check(lines.Exists(l => l.Contains("event=own-prefix") && l.Contains("diagnosticId=io.example.diagnostics-prefix-001")),
                            "前缀纪律：生态自身 FeatureId 派生前缀=放行（FeatureId 派生命名规则的通过侧）");
                    }
                    finally
                    {
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                    var seamLines = new List<string>();
                    var diagnostics = new BetterUnturnedExperience.Core.Diagnostics.DiagnosticRuntime(
                        (line, level) => seamLines.Add(level + " " + line), () => 1700000000000L, () => 1000L, null);
                    var official = "io.github.yu80rice.bue.seam-official";
                    diagnostics.OpenGeneration(official, 1UL);
                    var officialView = diagnostics.CreateLoggerView(official, 1UL);
                    officialView.Error("official-write", FrameworkErrorCode.None, "BUE-SEAM-001", null);
                    Check(seamLines.Exists(l => l.Contains("event=official-write") && l.Contains("diagnosticId=BUE-SEAM-001"))
                            && !seamLines.Exists(l => l.Contains("result=rejected")),
                        "前缀纪律：保留段内（官方/平台侧）身份写 BUE-*=合法（官方先行消费锚 BUE-LIT-* 的通过性前提）");
                    var rogue = "io.github.yu80rice.bue.rogue.seam";
                    diagnostics.OpenGeneration(rogue, 1UL);
                    var rogueView = diagnostics.CreateLoggerView(rogue, 1UL);
                    rogueView.Info("rogue-write", "BUE-SEAM-002");
                    Check(rogueView != null && seamLines.Exists(l => l.Contains("event=rogue-write")),
                        "前缀纪律：段内未列白名单者不存在于真实宿主（登记桥 BUE-REG-010 已拒）——view 层按段判定不重复造门禁");
                });

                Group("停止与宿主 shutdown 边界", () =>
                {
                    // 矩阵行行为面（Settings/MainThread 停止边界同构）：功能停用
                    // 后捕获 view 写入=不再产生模块行+边界拒绝诊断恰一条
                    // （BUE-LOG-004 latch）+静默计数；面板重启用新代际 view 恢复
                    // 可写；宿主 PluginStopping 后一切经 view 写入=拒（不静默吞，
                    // 留痕一次）。
                    var probe = new MatrixProbeRegistration("io.example.diagnostics-stop");
                    var probeRuntime = new FeatureRegistrationRuntime();
                    probeRuntime.OpenRegistration();
                    Check(probeRuntime.Register(probe).Accepted, "setup: 停止探针受理");
                    Check(probeRuntime.CompleteRuntime(), "setup: 停止目录冻结");
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var previousRecorder = BueRuntimeLog.Recorder;
                    var lines = new List<string>();
                    BueRuntimeHost.Bind(probeRuntime);
                    BueRuntimeLog.Recorder = lines.Add;
                    var probeFeature = new FeatureId("io.example.diagnostics-stop");
                    BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                    try
                    {
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(3321UL));
                        var capturedLogger = probe.Module.Bootstrap.Logger;
                        capturedLogger.Info("before-stop", "io.example.diagnostics-stop-001");
                        var moduleLinesBefore = lines.FindAll(l => l.Contains("event=before-stop") || l.Contains("event=after-stop")).Count;
                        Check(moduleLinesBefore == 1, "setup: 停止前经 view 写入一行");
                        Check(BueFeatureStartRuntime.SetFeatureEnabled(probeFeature, false),
                            "setup: 面板停用 seam 成功");
                        lines.Clear();
                        capturedLogger.Warning("after-stop", FrameworkErrorCode.ModuleStartFailed, "io.example.diagnostics-stop-002");
                        Check(!lines.Exists(l => l.Contains("event=after-stop") && l.Contains("level=warning")),
                            "停止边界：停用代际捕获 view 写入=模块行不再产生（矩阵行为面）");
                        Check(lines.Exists(l => l.StartsWith("Warning ") && l.Contains("result=rejected")
                                && l.Contains("reason=write-boundary") && l.Contains("diagnosticId=BUE-LOG-004")
                                && l.Contains("feature=io.example.diagnostics-stop")),
                            "停止边界：边界拒写显式留痕（BUE-LOG-004，不静默吞）");
                        capturedLogger.Info("after-stop-2", "io.example.diagnostics-stop-003");
                        Check(BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                "io.example.diagnostics-stop", "BUE-LOG-004", out entry) && entry.Count == 2L,
                            "停止边界：持续写入静默计数（拒绝行 latch 恰一条=不炸帧）");
                        Check(lines.FindAll(l => l.Contains("diagnosticId=BUE-LOG-004") && l.Contains("result=rejected")).Count == 1,
                            "停止边界：拒绝诊断行每 latch 期恰一条");
                        Check(BueFeatureStartRuntime.SetFeatureEnabled(probeFeature, true),
                            "setup: 面板重启用（新代际）");
                        var revivedLogger = probe.Module.Bootstrap.Logger;
                        Check(!ReferenceEquals(revivedLogger, capturedLogger),
                            "再启用：新代际=新 view（旧捕获 view 不代表当代际）");
                        lines.Clear();
                        revivedLogger.Info("rearmed", "io.example.diagnostics-stop-004");
                        Check(lines.Exists(l => l.Contains("event=rearmed")),
                            "再启用：新代际 view 恢复正常写入（停止不是永久封禁，重臂=新代际）");
                        capturedLogger.Info("stale-revival", "io.example.diagnostics-stop-005");
                        Check(!lines.Exists(l => l.Contains("event=stale-revival")),
                            "再启用：旧代际捕获 view 永不再写（代际账=dispatcher/settings 同构）");
                        // 宿主停止边界：PluginStopping 走 StopAll（逐 feature 撤账
                        // +ShutdownHost）；捕获 view 再写=拒+留痕，且必须发生在
                        // Recorder 复位前（行可观察）。
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        var postHostLogger = probe.Module.Bootstrap.Logger;
                        lines.Clear();
                        postHostLogger.Info("post-host-shutdown", "io.example.diagnostics-stop-006");
                        Check(!lines.Exists(l => l.Contains("event=post-host-shutdown") && l.Contains("level=info")),
                            "宿主 shutdown：PluginStopping 后捕获 view 写入=拒（原行不产）");
                        Check(lines.Exists(l => l.Contains("diagnosticId=BUE-LOG-004") && l.Contains("result=rejected")),
                            "宿主 shutdown：边界留痕（与停用同码族，reason token 区分代际失效/宿主停止）");
                        // R1-P1 修复钉：同进程 reload——宿主 shutdown 后新一次
                        // StartCatalog 经 ComposeBootstrap.OpenGeneration 重臂账
                        // （04 同构先例），当代 view 恢复可写、旧捕获 view 仍拒。
                        BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(3322UL));
                        var reloadLogger = probe.Module.Bootstrap.Logger;
                        Check(reloadLogger != null && !ReferenceEquals(reloadLogger, postHostLogger),
                            "reload setup: 新代际新 view（宿主 shutdown 不冻结后续目录启动）");
                        lines.Clear();
                        reloadLogger.Info("reload-revival", "io.example.diagnostics-stop-007");
                        Check(lines.Exists(l => l.Contains("event=reload-revival") && l.Contains("level=info")),
                            "R1-P1：reload 后代际账重臂，新代际 view 恢复正常写入（宿主停止=边界非死刑）");
                        postHostLogger.Info("reload-stale", "io.example.diagnostics-stop-008");
                        Check(!lines.Exists(l => l.Contains("event=reload-stale")),
                            "R1-P1：reload 不救旧捕获 view（代际账继续拒=新臂只归当代际）");
                    }
                    finally
                    {
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                });

                Group("容量受限与消毒截断", () =>
                {
                    // 摘要内存有界（128 条=本票定值，先例=dispatcher 256/LIFE 64
                    // 可观察定值）：第 129 个不同 diagnosticId 起不再开条目，
                    // BUE-LOG-003 溢出观察行恰一条（再溢出静默=不炸帧），但模块
                    // 原行照写——容量只限聚合，永不掐证据。消毒=标识符 token 空
                    // 白→下划线+按上限截断（64/128 本票定值）：一次写入至多一行
                    // （伪造换行不产第二行）；null/空标识符=BUE-LOG-002 拒写。
                    var lines = new List<string>();
                    var diagnostics = new BetterUnturnedExperience.Core.Diagnostics.DiagnosticRuntime(
                        (line, level) => lines.Add(level + " " + line), () => 1700000000000L, () => 5000L, null);
                    var feature = "io.example.diagnostics-cap";
                    diagnostics.OpenGeneration(feature, 1UL);
                    var view = diagnostics.CreateLoggerView(feature, 1UL);
                    BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                    Check(diagnostics.SummaryEntryCount == 0, "容量：新聚合器空条目（重启不持久=内存账，无跨实例残留）");
                    // 消毒先行（不占容量断言的前置）：脏标识符清洗为单行+截断入
                    // 条目；null/空白=显式拒写。
                    var hugeId = new string('x', 200);
                    var dirtyKey = (feature + "-" + hugeId).Substring(0, 128);
                    view.Info("dirty-event one\nsecond", feature + "-" + hugeId);
                    Check(lines.FindAll(l => l.Contains("event=dirty-event")).Count == 1
                            && lines.Exists(l => l.Contains("event=dirty-event_one_second")
                                    && l.Contains("diagnosticId=" + dirtyKey) && !l.Contains("\n")),
                        "消毒：空白→下划线+128 截断，一次写入恰一行（伪造换行不产第二行/不裂解条目键）");
                    Check(diagnostics.TryGetSummaryEntry(feature, dirtyKey, out entry)
                            && entry.DiagnosticId.Length == 128 && entry.Count == 1L,
                        "消毒：diagnosticId 按 128 上限截断入条目（本票定值，截断后即键）");
                    lines.Clear();
                    view.Info(null, feature + "-null-event");
                    view.Warning("null-id", FrameworkErrorCode.None, "   ");
                    Check(lines.FindAll(l => l.Contains("event=diagnostic-write") && l.Contains("result=rejected")
                            && l.Contains("reason=invalid-identifier") && l.Contains("diagnosticId=BUE-LOG-002")).Count == 2,
                        "消毒：null/空白标识符=显式拒写+留痕（BUE-LOG-002=本票定案码，逐例各一条）");
                    Check(!lines.Exists(l => l.Contains("event=null-id") && l.Contains("level=warning")),
                        "消毒：被拒写零模块行（拒=不写，不是写了再标）");
                    // 容量主体用独立 runtime（上面 dirty 条目已占一格）。
                    var capLines = new List<string>();
                    var capRuntime = new BetterUnturnedExperience.Core.Diagnostics.DiagnosticRuntime(
                        (line, level) => capLines.Add(level + " " + line), () => 1700000000000L, () => 5000L, null);
                    capRuntime.OpenGeneration(feature, 1UL);
                    var capView = capRuntime.CreateLoggerView(feature, 1UL);
                    for (var i = 0; i < 128; i++)
                    {
                        capView.Info("cap-fill-" + i, feature + "-cap-" + i);
                    }
                    Check(capRuntime.SummaryEntryCount == 128, "容量：128 条聚合条目=本票定值上限");
                    capLines.Clear();
                    capView.Info("cap-overflow", feature + "-cap-128");
                    Check(capLines.Exists(l => l.Contains("event=cap-overflow") && l.Contains("diagnosticId=" + feature + "-cap-128")),
                        "容量：溢出只限聚合——模块原行照写（证据链不被容量掐断）");
                    Check(capLines.Exists(l => l.Contains("event=diagnostic-summary") && l.Contains("result=capacity-overflow")
                            && l.Contains("diagnosticId=BUE-LOG-003") && l.Contains("entryCap=128")),
                        "容量：溢出显式观察行（BUE-LOG-003=本票定案码，不静默丢弃）");
                    capLines.Clear();
                    capView.Info("cap-overflow-2", feature + "-cap-129");
                    Check(capLines.FindAll(l => l.Contains("BUE-LOG-003")).Count == 0
                            && capLines.Exists(l => l.Contains("event=cap-overflow-2")),
                        "容量：溢出观察行恰一条 latch（后续溢出静默，不炸帧）");
                    Check(!capRuntime.TryGetSummaryEntry(feature, feature + "-cap-128", out entry),
                        "容量：溢出码零条目（内存账不无限增长）");
                    Check(capRuntime.SummaryEntryCount == 128, "容量：溢出后条目数恒=上限");
                });

                Group("摘要输出限频", () =>
                {
                    // 票面红线「输出限频」：同一条目的摘要行=首见一条+此后每
                    // 30000ms（本票定值）至多一条，限频只作用于摘要行，模块原
                    // 行逐条如常（限频≠过滤）。fake clock 先例=DEV-V3-04 预算窗。
                    var lines = new List<string>();
                    long monoMs = 1000L;
                    var diagnostics = new BetterUnturnedExperience.Core.Diagnostics.DiagnosticRuntime(
                        (line, level) => lines.Add(level + " " + line), () => 1700000000000L, () => monoMs, null);
                    var feature = "io.example.diagnostics-rate";
                    diagnostics.OpenGeneration(feature, 1UL);
                    var view = diagnostics.CreateLoggerView(feature, 1UL);
                    var id = feature + "-001";
                    view.Info("rate-probe", id);
                    Check(lines.FindAll(l => l.Contains("diagnostic-summary") && l.Contains("diagnosticId=" + id)).Count == 1,
                        "限频：条目首见即一条摘要行");
                    monoMs += 10000L;
                    view.Info("rate-probe", id);
                    monoMs += 10000L;
                    view.Info("rate-probe", id);
                    Check(lines.FindAll(l => l.Contains("diagnostic-summary") && l.Contains("diagnosticId=" + id)).Count == 1,
                        "限频：窗口内（20s<30s）再写入不出第二摘要行（不逐条重复、摘要不制造刷屏）");
                    Check(lines.FindAll(l => l.Contains("event=rate-probe")).Count == 3,
                        "限频：模块原行逐条如常（限频只限摘要行，不静默过滤诊断）");
                    monoMs += 10001L;
                    view.Info("rate-probe", id);
                    var rateLines = lines.FindAll(l => l.Contains("diagnostic-summary") && l.Contains("diagnosticId=" + id));
                    Check(rateLines.Count == 2 && rateLines[1].Contains("count=4"),
                        "限频：越过窗口出下条摘要行且 count 如实累计（=4）");
                    BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                    Check(diagnostics.TryGetSummaryEntry(feature, id, out entry) && entry.Count == 4L,
                        "限频：聚合计数独立于输出限频（计数=真相，输出=可见性节奏）");
                });

                Group("故障隔离不反噬模块", () =>
                {
                    // 三方法面返回 void——契约不给报错面，隔离义务全在 view/聚合
                    // 器侧：sink 抛异常绝不反噬调用方（否则生态功能因平台诊断
                    // 故障崩掉=「Logger 异常不得反向破坏模块」的倒置）；故障经
                    // 独立 fallback 通道留恰一条（BUE-LOG-005，不炸帧）；聚合账
                    // 在行发射之前——写失败计数仍如实（证据先于可见性）；
                    // fallback 同时抛=仍不反噬（全隔离）。
                    var writes = 0;
                    var fallback = new List<string>();
                    var diagnostics = new BetterUnturnedExperience.Core.Diagnostics.DiagnosticRuntime(
                        (line, level) => { writes++; throw new System.NotSupportedException("v3-diag-sink-fault"); },
                        () => 1700000000000L, () => 9000L, fallback.Add);
                    var feature = "io.example.diagnostics-fault";
                    diagnostics.OpenGeneration(feature, 1UL);
                    var view = diagnostics.CreateLoggerView(feature, 1UL);
                    var threw = false;
                    try
                    {
                        for (var i = 0; i < 5; i++) view.Info("fault-probe", feature + "-001");
                        view.Error("fault-probe-error", FrameworkErrorCode.CoreRuntimeFailure, feature + "-002",
                            new System.Exception("v3-fault"));
                    }
                    catch (System.Exception) { threw = true; }
                    Check(!threw, "故障隔离：sink 恒抛，6 次写入全部不反噬模块（void 窄面=无异常面承诺）");
                    Check(writes >= 6, "故障隔离：每次写入都到 sink 后才被隔离（无提前吞）");
                    BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                    Check(diagnostics.TryGetSummaryEntry(feature, feature + "-001", out entry) && entry.Count == 5L,
                        "故障隔离：聚合计数不依赖行写成功（计数=证据链真相）");
                    Check(fallback.FindAll(l => l.Contains("diagnosticId=BUE-LOG-005") && l.Contains("NotSupportedException")).Count == 1,
                        "故障隔离：故障观察走 fallback 通道恰一条（BUE-LOG-005=本票定案码，episode latch）");
                    var hardRuntime = new BetterUnturnedExperience.Core.Diagnostics.DiagnosticRuntime(
                        (line, level) => throw new System.InvalidOperationException("primary dead"),
                        () => 1700000000000L, () => 9000L, line => throw new System.InvalidOperationException("fallback dead"));
                    hardRuntime.OpenGeneration("io.example.fault-hard", 1UL);
                    var hardView = hardRuntime.CreateLoggerView("io.example.fault-hard", 1UL);
                    var hardThrew = false;
                    try { hardView.Warning("dead-everywhere", FrameworkErrorCode.None, "io.example.fault-hard-001"); }
                    catch (System.Exception) { hardThrew = true; }
                    Check(!hardThrew, "故障隔离：主+fallback 双故障仍不反噬（全隔离）");
                });

                Group("统一 sink 收编：T4/T5/状态投影进摘要", () =>
                {
                    // 票面红线「进统一诊断 sink（分 seam 判据可定位，不互相遮蔽）」
                    // 的生产路径证法：生命周期状态投影/停用过渡行经 StartCatalog
                    // 真实组装的双绑 sink——原行逐字不变（03/04 既有锚零回归），
                    // 另路进同一聚合器；T5 degraded 电平经 NetworkModuleAdapter
                    // 生产运行时 sink 双绑同证（假传输驱动，04 fixture 先例）。
                    // 无 diagnosticId 的行只照写不聚合（不造静默条目）。
                    var previousRecorder = BueRuntimeLog.Recorder;
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var lines = new List<string>();
                    BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                    try
                    {
                        BueRuntimeLog.Recorder = lines.Add;
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        var probe = new MatrixProbeRegistration("io.example.diagnostics-absorb");
                        var probeRuntime = new FeatureRegistrationRuntime();
                        BueRuntimeHost.Bind(probeRuntime);
                        probeRuntime.OpenRegistration();
                        Check(probeRuntime.Register(probe).Accepted, "收编 setup: 探针受理");
                        Check(probeRuntime.CompleteRuntime(), "收编 setup: 目录冻结");
                        BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(3331UL));
                        Check(BueFeatureStartRuntime.SetFeatureEnabled(new FeatureId("io.example.diagnostics-absorb"), false),
                            "收编 setup: 面板停用驱动生命周期过渡");
                        Check(lines.Exists(l => l.Contains("event=feature-state") && l.Contains("feature=io.example.diagnostics-absorb")),
                            "收编：原行路径逐字不变（双绑=照写不误，03 既有锚语义零变更）");
                        Check(BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                "io.example.diagnostics-absorb", "BUE-LIFE-STATE", out entry) && entry.Count >= 4L,
                            "状态投影收编：feature-state 过渡进摘要（T4 ③：Starting/Running/Stopping/Stopped ≥4 计数）");
                        Check(lines.Exists(l => l.StartsWith("Info ") && l.Contains("BUE diagnostic-summary")
                                && l.Contains("diagnosticId=BUE-LIFE-STATE")),
                            "状态投影收编：摘要行对玩家可见级进 LogOutput（导出即带走）");
                        var absorbed = BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime;
                        var beforeCount = absorbed.SummaryEntryCount;
                        absorbed.AggregateHostLine("bue.host", BetterUnturnedExperience.Core.Diagnostics.DiagnosticLevel.Info,
                            "event=no-code line=whatever");
                        Check(absorbed.SummaryEntryCount == beforeCount,
                            "收编：无 diagnosticId 的行不聚合（不造静默条目，聚合=按码）");
                        absorbed.AggregateHostLine("bue.host", BetterUnturnedExperience.Core.Diagnostics.DiagnosticLevel.Warning,
                            "event=lit-fault feature=io.github.yu80rice.bue.inventory-tidy diagnosticId=BUE-LIT-900");
                        Check(absorbed.TryGetSummaryEntry("io.github.yu80rice.bue.inventory-tidy", "BUE-LIT-900", out entry)
                                && entry.Count == 1L,
                            "收编：行内 feature= 优先于回落归属（分 seam 判据可定位，不互相遮蔽）");
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    }
                    finally
                    {
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                    }
                    var fx = NetworkV3Fixture.Create("v3absorb");
                    try
                    {
                        var channel = new FeatureId("io.example.v3absorb");
                        var api = fx.Adapter.NetworkApi;
                        Check(api.RegisterChannel(channel, new ContractVersion(2, 0), 1).Accepted, "收编 setup：网络频道注册");
                        var session = fx.Establish(1001UL, 9601UL);
                        Check(session != null, "收编 setup：会话建立");
                        fx.Engine.SendOverride = (frame, reliable, target) => false;
                        for (var i = 0; i < V3LinkDegradationThreshold; i++)
                        {
                            api.SendToClient(channel, session, new byte[] { 1 }, true);
                        }
                        Check(fx.CountLine("event=network-link result=degraded") == 1,
                            "收编 setup：degraded 电平产生（04 语义回归）");
                        Check(fx.Lines.Exists(l => l.Contains("diagnosticId=BUE-NET-002")),
                            "T5 收编：链路健康行仍在 adapter sink（原路径逐字不变）");
                        Check(BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                "io.github.yu80rice.bue.network", "BUE-NET-002", out entry) && entry.Count == 1L,
                            "T5 收编：degraded 以网络功能身份进统一摘要（与生态诊断同聚合器同权）");
                    }
                    finally
                    {
                        fx.Dispose();
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                });

                Group("官方先行消费锚真实 LIT 启停经注入 view", () =>
                {
                    // T8 ⑧②红线「不能只保留私有 Logger.LogWarning 路径而宣称已
                    // 消费」：真实 LIT 模块的启停诊断行经 IFeatureBootstrap.Logger
                    // 注入 view 落进统一缝（官方=保留段身份→BUE-LIT-* 合法，与
                    // 生态同一行格式同一聚合器同权）。中文人读行保持原通道（玩家
                    // 可读性不回归），结构化行=证据链同权面。
                    EnsureSettings();
                    var previousRecorder = BueRuntimeLog.Recorder;
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var lines = new List<string>();
                    try
                    {
                        BueRuntimeLog.Recorder = lines.Add;
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        var runtime = new FeatureRegistrationRuntime();
                        BueRuntimeHost.Bind(runtime);
                        runtime.OpenRegistration();
                        Check(BetterItemInteractionFeatureRegistration.Register().Accepted, "官方锚 setup: BII 注册");
                        Check(runtime.Register(InventoryTidyFeatureRegistration.CreateRegistration()).Accepted,
                            "官方锚 setup: LIT 注册");
                        Check(runtime.CompleteRuntime(), "官方锚 setup: 目录冻结");
                        // LIT 工厂复用静态 WiredModule，而模块的 ShuttingDown 是
                        // 单向旗标（既有冻结语义）——清掉前序组实例，本组经
                        // ArmNewModule 得到真·全新模块，Start/Stop 语义如实演练
                        // （06 官方锚组的同一清零先例）。
                        InventoryTidyFeatureRegistration.WiredModule = null;
                        BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(3341UL));
                        var litFeature = "io.github.yu80rice.bue.inventory-tidy";
                        Check(lines.Exists(l => l.StartsWith("Info ") && l.Contains("[BUE-DIAG]")
                                && l.Contains("feature=" + litFeature) && l.Contains("event=tidy-module-started")
                                && l.Contains("level=info") && l.Contains("diagnosticId=BUE-LIT-START")),
                            "官方先行消费：LIT 启动诊断行走注入 IFeatureLogger view（结构化行=经 view 才存在，私有 LitRuntime 通道产不出这行）");
                        BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                        Check(BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                litFeature, "BUE-LIT-START", out entry) && entry.Count == 1L,
                            "官方先行消费：官方行与生态行进同一聚合器同权计数");
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                        Check(lines.Exists(l => l.Contains("event=tidy-module-stopped") && l.Contains("feature=" + litFeature)
                                && l.Contains("diagnosticId=BUE-LIT-STOP")),
                            "官方先行消费：LIT 停止行同经 view（停止边界在 Stop 返回后撤账=Stop 内可写，settings 同构）");
                    }
                    finally
                    {
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                        InventoryTidyFeatureRegistration.WiredModule = null;
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                });

                Group("生态对照 NoOp Logger 支线", () =>
                {
                    // T8 ⑧③ probe 链生态侧最小对照（全链 probe→08 递延具名）：
                    // 样例功能经注入 view 走三方法（NoOp=白名单官方样例身份，
                    // 用 BUE-NOOP-* 码=平台侧通过性；生态前缀拒绝锚在「BUE-* 前
                    // 缀纪律」组以 io.example 身份证，两者互不遮蔽）；宿主侧断言
                    // 行与摘要计数（probe 自证=只观察调用不炸，聚合归宿主缝）。
                    var probeRuntime = new FeatureRegistrationRuntime();
                    probeRuntime.OpenRegistration();
                    Check(probeRuntime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted,
                        "生态对照 setup: NoOp 样例受理");
                    Check(probeRuntime.CompleteRuntime(), "生态对照 setup: NoOp 目录冻结");
                    var previousRecorder = BueRuntimeLog.Recorder;
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var lines = new List<string>();
                    BueRuntimeHost.Bind(probeRuntime);
                    BueRuntimeLog.Recorder = lines.Add;
                    try
                    {
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BueFeatureStartRuntime.StartCatalog(probeRuntime, NewLoopbackNetwork(3351UL));
                        var probe = NoOpFeatureRegistration.LastProbe;
                        Check(probe != null && probe.Started && probe.LoggerAvailable,
                            "生态对照：NoOp probe 在真实宿主组装下观察到 Logger 非 null（矩阵行生态侧=每功能接线）");
                        Check(probe.LoggerInfoWritten && probe.LoggerWarningWritten && probe.LoggerErrorWritten,
                            "生态对照：三方法面可调不炸（void 窄面=无异常面承诺，样例如实演示生态姿势）");
                        var noopFeature = "io.github.yu80rice.bue.noop";
                        Check(lines.Exists(l => l.Contains("feature=" + noopFeature) && l.Contains("event=noop-probe-warning")
                                && l.Contains("error=SettingRejected") && l.Contains("diagnosticId=BUE-NOOP-WARN")),
                            "生态对照：样例写入落进同一 LogOutput 结构化解（与官方同一行格式=同权①）");
                        BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry entry;
                        Check(BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                noopFeature, "BUE-NOOP-ERROR", out entry) && entry.Count == 1L
                                && entry.Level == BetterUnturnedExperience.Core.Diagnostics.DiagnosticLevel.Error,
                            "生态对照：样例行进同一摘要聚合器（同字段同限频规则=不因来源过滤）");
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    }
                    finally
                    {
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                });
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            finally
            {
                BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                for (var index = 0; index < settingsRoots.Count; index++)
                {
                    try { if (System.IO.Directory.Exists(settingsRoots[index])) System.IO.Directory.Delete(settingsRoots[index], true); }
                    catch (Exception) { }
                }
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V3-07 diagnostics collection: ALL GREEN (0 failures) — groups: 矩阵 Logger 接线两侧/三方法窄面与级别映射/BUE-* 前缀纪律/停止与宿主 shutdown 边界/容量受限与消毒截断/摘要输出限频/故障隔离不反噬模块/统一 sink 收编/官方先行消费锚 LIT/生态对照 NoOp");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V3-07 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        // DEV-V3-08: the unified ecosystem contract probe (V3-T9 裁决④ / spec
        // 「SDK 契约文档」节)。NoOpFixture 升格为 注册→Bootstrap→Events→
        // TryTrack/Lifecycle→Network→HostTick→Settings→Logger→停止与隔离 的
        // 全链可运行样本，七枚缝步各自独立判据（ProbeStepOutcome+自己的诊断
        // 行），失败分 seam 可定位：一步 Mismatch 不遮蔽其余步（一步一段
        // StepMismatches 详情），NotRun≠Passed（链未跑到≠通过）。可定位性由
        // 「一次红一缝」旋钮红测证明——旋钮只错置该步的期望，真实拒绝来自
        // 真实缝（总线归属路由/网络通道门/设置乐观并发/生命周期账/诊断消毒）。
        // 文档锚定子组把 SDK 附录的结构与代码表、四条件、自检清单和「示例=
        // NoOpFixture 源码逐字」的双向锚全部钉成机器判据（验收条件①②③）。
        private static void AssertBueV3EcosystemUnifiedProbe(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            var settingsRoots = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message));
                    }
                }

                // 一次完整探针运行：独立注册运行时+临时持久根+干净诊断/投递
                // 账+唯一 nonce 的环回网络（生产组合根全程，与 03..07 各组同
                // 卫生模式）。返回捕获行与链末探针状态；运行完即 StopAll。
                void RunProbe(NoOpProbeFault fault, ulong nonce,
                    out NoOpFeatureRegistration.ProbeState probe, out List<string> lines)
                {
                    var runtime = new FeatureRegistrationRuntime();
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var previousRecorder = BueRuntimeLog.Recorder;
                    lines = new List<string>();
                    probe = null;
                    var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BUE-V3-08-" + Guid.NewGuid().ToString("N"));
                    settingsRoots.Add(root);
                    BueRuntimeHost.Bind(runtime);
                    BueRuntimeLog.Recorder = lines.Add;
                    try
                    {
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, () => true, null);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated();
                        NoOpFeatureRegistration.NextProbeFault = fault;
                        NoOpFeatureRegistration.ResetLastProbe();
                        runtime.OpenRegistration();
                        Check(runtime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted,
                            "RunProbe setup：样例登记受理（nonce=" + nonce + "）");
                        Check(runtime.CompleteRuntime(), "RunProbe setup：目录冻结");
                        BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(nonce));
                        BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce();
                        probe = NoOpFeatureRegistration.LastProbe;
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    }
                    finally
                    {
                        NoOpFeatureRegistration.NextProbeFault = NoOpProbeFault.None;
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                }

                Group("统一探针：全链绿（票后终态矩阵+七缝独立判据）", () =>
                {
                    var runtime = new FeatureRegistrationRuntime();
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var previousRecorder = BueRuntimeLog.Recorder;
                    var lines = new List<string>();
                    var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BUE-V3-08-" + Guid.NewGuid().ToString("N"));
                    settingsRoots.Add(root);
                    BueRuntimeHost.Bind(runtime);
                    BueRuntimeLog.Recorder = lines.Add;
                    try
                    {
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, () => true, null);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated();
                        NoOpFeatureRegistration.NextProbeFault = NoOpProbeFault.None;
                        NoOpFeatureRegistration.ResetLastProbe();
                        runtime.OpenRegistration();
                        var registration = runtime.Register(NoOpFeatureRegistration.ProbeRegistration);
                        Check(registration.Accepted && registration.DiagnosticId == "BUE-REG-ACCEPT"
                                && registration.Feature.Value == "io.github.yu80rice.bue.noop",
                            "链·注册缝：公开桥受理=显式结果四元组（BUE-REG-ACCEPT+FeatureId 绑定）");
                        Check(runtime.CompleteRuntime(), "链·注册缝：目录冻结");
                        BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(4101UL));
                        var probe = NoOpFeatureRegistration.LastProbe;
                        Check(probe != null && probe.Started, "链·启动：probe 经真实 StartCatalog 全链跑完");
                        Check(probe.BootstrapStep == ProbeStepOutcome.Passed,
                            "Bootstrap 缝：票后终态可用性矩阵全体非 null（11 成员快照判据）");
                        Check(probe.BootstrapGenerationAtStart != 0UL,
                            "Bootstrap 缝：LifecycleGeneration=宿主实发代际（≠0）");
                        Check(probe.EventsStep == ProbeStepOutcome.Passed && probe.EventsSelfReceived == 1,
                            "Events 缝：登记→订阅→自有 EventId 发布被收并自回一帧（四判据齐）");
                        // 常跑序列里同型探针已在先前票组注册过该类型（受理行
                        // 属于首跑的组）——本组判据=登记尝试必有结构化答复行：
                        // 首跑=BUE-EVT-ACCEPT，跨组/跨代幂等=BUE-EVT-003。
                        Check(ContainsDiagnostic(lines, "diagnosticId=BUE-EVT-ACCEPT")
                                || (ContainsDiagnostic(lines, "result=register-rejected")
                                    && ContainsDiagnostic(lines, "diagnosticId=BUE-EVT-003")),
                            "Events 缝诊断行：类型归属登记答复行进同一 sink（首跑受理或跨代幂等拒，均结构化）");
                        Check(ContainsDiagnostic(lines, "event=feature-event result=publish-rejected")
                                && ContainsDiagnostic(lines, "reason=event-id-mismatch"),
                            "Events 缝反判据行：错挂声明 EventId 发布=显式拒+零派发");
                        Check(probe.LifecycleStep == ProbeStepOutcome.Passed && probe.Tracked && probe.TrackedSecond,
                            "Lifecycle 缝：两资源 TryTrack 受理（逆序释放账归停止子组）");
                        Check(probe.QueriedStateAtStart == FeatureState.Starting,
                            "Lifecycle 缝：Start 期只读状态查询观察到 Starting（最小行为面）");
                        Check(probe.NetworkStep == ProbeStepOutcome.Passed,
                            "Network 缝：通道登记/established 空快照/降级发送/负通道/投递 五判据");
                        Check(probe.NetworkSendObserved == NetworkSendResult.NoSession
                                && probe.NetworkWrongChannelObserved == NetworkSendResult.ChannelNotRegistered,
                            "Network 缝：无会话=NoSession、未注册通道=ChannelNotRegistered（显式结果，禁静默丢）");
                        Check(probe.MainThreadPosted,
                            "Network 缝：主线程投递经接线 dispatcher 受理（04 支线入链）");
                        Check(probe.HostTickStep == ProbeStepOutcome.Passed && probe.HostTickSubscribed,
                            "HostTick 缝：宿主时钟订阅经 Events 缝取得句柄");
                        var beforeTicks = probe.HostTicksReceived;
                        Check(BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce()
                                && BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce(),
                            "HostTick 缝：宿主泵拍产针");
                        Check(probe.HostTicksReceived == beforeTicks + 2,
                            "HostTick 缝：每拍恰一到达（生产真时钟驱动样本订阅）");
                        Check(probe.LastHostTickPhase == TickPhase.Update && probe.LastHostTickDeltaSeconds >= 0f,
                            "HostTick 缝：载荷时序三字段（Phase 冻结 Update/DeltaTime 非负）");
                        Check(probe.SettingsStep == ProbeStepOutcome.Passed && probe.SettingsCommitAccepted
                                && probe.SettingsRevisionAdvanced && probe.SettingsInvalidRejected && probe.SettingsStaleRejected,
                            "Settings 缝：读快照/合法提交推进/未知 id 拒/过期 ExpectedRevision 拒（五判据）");
                        Check(probe.LoggerStep == ProbeStepOutcome.Passed && probe.LoggerInfoWritten
                                && probe.LoggerWarningWritten && probe.LoggerErrorWritten,
                            "Logger 缝：三方法窄面各调一次");
                        Check(ContainsDiagnostic(lines, "feature=io.github.yu80rice.bue.noop")
                                && ContainsDiagnostic(lines, "event=noop-probe-info")
                                && ContainsDiagnostic(lines, "event=noop-probe-warning")
                                && ContainsDiagnostic(lines, "event=noop-probe-error"),
                            "Logger 缝诊断行：三结构化行进同一 LogOutput 缝（不被静默过滤）");
                        BetterUnturnedExperience.Core.Diagnostics.DiagnosticSummaryEntry summaryEntry;
                        Check(BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Runtime.TryGetSummaryEntry(
                                "io.github.yu80rice.bue.noop", "BUE-NOOP-INFO", out summaryEntry)
                                && summaryEntry.Count == 1L,
                            "Logger 缝摘要判据：按 (FeatureId,DiagnosticId) 聚合计数");
                        Check(probe.StepMismatches.Count == 0,
                            "全链绿：七缝零 Mismatch（详情=" + string.Join("|", probe.StepMismatches) + "）");
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    }
                    finally
                    {
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                });

                Group("统一探针：停止边界与再启用（逆序释放/边界写拒/新代际续账）", () =>
                {
                    var runtime = new FeatureRegistrationRuntime();
                    var previousRuntime = BueRuntimeHost.CurrentRuntime;
                    var previousRecorder = BueRuntimeLog.Recorder;
                    var lines = new List<string>();
                    var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BUE-V3-08-" + Guid.NewGuid().ToString("N"));
                    settingsRoots.Add(root);
                    BueRuntimeHost.Bind(runtime);
                    BueRuntimeLog.Recorder = lines.Add;
                    var noopFeature = new FeatureId("io.github.yu80rice.bue.noop");
                    try
                    {
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueSettingsRuntime.EnsureCreated(root, () => true, null);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueHostEventRuntime.EnsureCreated();
                        NoOpFeatureRegistration.NextProbeFault = NoOpProbeFault.None;
                        NoOpFeatureRegistration.ResetLastProbe();
                        runtime.OpenRegistration();
                        Check(runtime.Register(NoOpFeatureRegistration.ProbeRegistration).Accepted, "停止子组 setup：样例受理");
                        Check(runtime.CompleteRuntime(), "停止子组 setup：目录冻结");
                        BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(4111UL));
                        var probe1 = NoOpFeatureRegistration.LastProbe;
                        Check(probe1 != null && probe1.Started, "停止子组 setup：全链启动完成");
                        BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce();
                        // ── 链尾「停止与隔离」：面板停用走 command seam（用户
                        // 驱动），停止边界由宿主兑现——逆序释放、总线自动注销、
                        // 三视图写显式拒+各自诊断行、状态投影如实。──
                        Check(BueFeatureStartRuntime.SetFeatureEnabled(noopFeature, false),
                            "停止缝：面板停用命令受理（UserDisabled）");
                        Check(string.Join(",", probe1.DisposalOrder) == "second,first",
                            "停止缝：TryTrack 资源按注册逆序自动释放（first 后行=先登后放）");
                        var frozenTicks = probe1.HostTicksReceived;
                        BetterUnturnedExperience.Plugin.BueHostEventRuntime.TickOnce();
                        Check(probe1.HostTicksReceived == frozenTicks,
                            "停止缝：总线订阅经宿主自动注销（后续泵拍不再到达样本）");
                        var lateWrite = probe1.SettingsView.Submit(new ScopedSettingChangeRequest(
                            9001UL, SettingRevisionScope.ClientPreference, 0u,
                            new[] { new SettingMutation("noop.probe-toggle", SettingValue.Toggle(true)) }));
                        Check(!lateWrite.Accepted && ContainsDiagnostic(lines, "BUE-SET-001"),
                            "停止缝：停止后设置写入=显式拒+BUE-SET-001 留痕（捕获 view 不伪造）");
                        probe1.LoggerView.Info("noop-post-stop-write", "BUE-NOOP-POSTSTOP");
                        Check(!ContainsDiagnostic(lines, "event=noop-post-stop-write") && ContainsDiagnostic(lines, "BUE-LOG-004"),
                            "停止缝：停止后 Logger 写=不产模块行+BUE-LOG-004 留痕");
                        var latePost = probe1.MainThreadView.Post(() => { });
                        Check(!latePost.Posted && latePost.Reason == MainThreadPostReason.GenerationInvalid
                                && ContainsDiagnostic(lines, "BUE-MT-002"),
                            "停止缝：停止后投递=GenerationInvalid 显式拒+BUE-MT-002");
                        FeatureStatusView stoppedStatus;
                        Check(BueFeatureStartRuntime.TryGetStatus(noopFeature, out stoppedStatus)
                                && stoppedStatus.State == FeatureState.Stopped
                                && stoppedStatus.StopReason == FeatureStopReason.UserDisabled,
                            "停止缝：状态投影如实停用（宿主唯一事实源，面板侧可见）");
                        var capturedQuery = probe1.LifetimeView.CurrentStatus;
                        Check(capturedQuery.State == FeatureState.Stopped
                                && capturedQuery.Feature.Value == "io.github.yu80rice.bue.noop",
                            "停止缝：捕获视图只读查询停止后仍可用且如实（A.3 最小行为面第五条的生态消费）");
                        // ── 再启用=新代际全链重跑；跨代持久账正确续接：事件
                        // 类型登记幂等（显式重复拒、归属不变仍可发布），设置
                        // revision 不清零，通道所有权幂等。──
                        Check(BueFeatureStartRuntime.SetFeatureEnabled(noopFeature, true),
                            "再启用缝：显式启用重臂（Stopped→新代际）");
                        var probe2 = NoOpFeatureRegistration.LastProbe;
                        Check(!ReferenceEquals(probe2, probe1) && probe2.Started,
                            "再启用缝：新模块实例重跑全链");
                        Check(probe2.BootstrapStep == ProbeStepOutcome.Passed && probe2.EventsStep == ProbeStepOutcome.Passed
                                && probe2.LifecycleStep == ProbeStepOutcome.Passed && probe2.NetworkStep == ProbeStepOutcome.Passed
                                && probe2.HostTickStep == ProbeStepOutcome.Passed && probe2.SettingsStep == ProbeStepOutcome.Passed
                                && probe2.LoggerStep == ProbeStepOutcome.Passed && probe2.StepMismatches.Count == 0,
                            "再启用缝：新代际七缝全绿（分 seam 判据逐个复核）");
                        Check(probe2.BootstrapGenerationAtStart != probe1.BootstrapGenerationAtStart,
                            "再启用缝：新 LifecycleGeneration（旧代际全失效）");
                        Check(ContainsDiagnostic(lines, "BUE-EVT-003"),
                            "再启用缝：事件类型重登记=显式重复拒（跨代幂等，归属不变仍可发布收帧）");
                        Check(probe2.SettingsRevisionAfterCommit > probe1.SettingsRevisionAfterCommit,
                            "再启用缝：设置 revision 跨代续账（不清零、无第二事实源）");
                        BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                    }
                    finally
                    {
                        NoOpFeatureRegistration.NextProbeFault = NoOpProbeFault.None;
                        BueRuntimeLog.Recorder = previousRecorder;
                        BueRuntimeHost.Bind(previousRuntime);
                        BetterUnturnedExperience.Plugin.BueDiagnosticsRuntime.Clear();
                        BetterUnturnedExperience.Plugin.BueMainThreadRuntime.Clear();
                    }
                });

                Group("统一探针：Start 故障隔离与显式再启用（Isolated 不自动重启/NotRun≠Passed）", () =>
                {
                    NoOpFeatureRegistration.ProbeState crashedProbe;
                    List<string> crashLines;
                    // 一次带 StartFault 旋钮的完整运行：模块 Start 真抛，宿主
                    // 按功能级隔离（不升级 CoreSafeMode）；探针链未跑完=各步
                    // NotRun——报告把「未跑到」与「通过」严格区分。
                    RunProbe(NoOpProbeFault.StartFault, 4121, out crashedProbe, out crashLines);
                    Check(crashedProbe != null && !crashedProbe.Started,
                        "隔离缝：Start 崩溃=链未完成（Started 显式 false，样本不伪装成功）");
                    Check(crashedProbe.BootstrapStep == ProbeStepOutcome.NotRun
                            && crashedProbe.EventsStep == ProbeStepOutcome.NotRun
                            && crashedProbe.LifecycleStep == ProbeStepOutcome.NotRun
                            && crashedProbe.NetworkStep == ProbeStepOutcome.NotRun
                            && crashedProbe.HostTickStep == ProbeStepOutcome.NotRun
                            && crashedProbe.SettingsStep == ProbeStepOutcome.NotRun
                            && crashedProbe.LoggerStep == ProbeStepOutcome.NotRun,
                        "隔离缝：未跑到的缝=NotRun（≠Passed，全链 PASS/FAIL 不得遮蔽链位置）");
                    Check(ContainsDiagnostic(crashLines, "stage=start error=")
                            && ContainsDiagnostic(crashLines, "BUE-LIFE-ISOLATE"),
                        "隔离缝：Start 故障→功能级隔离诊断行（单功能故障只隔离该功能）");
                    var noopFeature = new FeatureId("io.github.yu80rice.bue.noop");
                    FeatureStatusView isolated;
                    Check(BueFeatureStartRuntime.TryGetStatus(noopFeature, out isolated)
                            && isolated.State == FeatureState.Isolated,
                        "隔离缝：状态投影如实 Isolated");
                    // Isolated 不自动重启：没有任何东西把它重新臂起；DEV-V4-03
                    // 目标提交语义下停用提交=空操作成功（现网 not-running 拒，
                    // 本锚先红后绿），只有显式启用才开新代际。
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(noopFeature, false),
                        "隔离缝：对 Isolated 发停用=空操作成功（目标提交语义，先红）");
                    FeatureStatusView isolatedKept;
                    Check(BueFeatureStartRuntime.TryGetStatus(noopFeature, out isolatedKept)
                            && isolatedKept.State == FeatureState.Isolated
                            && isolatedKept.StopReason == FeatureStopReason.RuntimeIsolated
                            && isolatedKept.StateRevision == isolated.StateRevision,
                        "隔离缝：空操作停用后保持隔离（不走会失败的 disable，无假迁移不新开代际）");
                    // 显式再启用（旋钮已随 RunProbe 复位 None）→ 同一登记记录
                    // 新代际全链重跑（宿主停止/隔离是边界不是死刑，04/07 同构）。
                    Check(BueFeatureStartRuntime.SetFeatureEnabled(noopFeature, true),
                        "隔离缝：用户显式启用把 Isolated 重新臂起（唯一入口）");
                    var revivedProbe = NoOpFeatureRegistration.LastProbe;
                    Check(revivedProbe != null && revivedProbe.Started
                            && revivedProbe.StepMismatches.Count == 0
                            && revivedProbe != crashedProbe,
                        "隔离缝：显式启用→新代际全链绿（从干净状态重试）");
                    Check(revivedProbe.BootstrapGenerationAtStart != crashedProbe.BootstrapGenerationAtStart,
                        "隔离缝：再启用代际=新值（隔离代际不再臂任务）");
                    BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                });

                // ── 失败分 seam 可定位：一次只红一缝——旋钮让该步真实观察到
                // 它不期望的合同行为（真实拒绝来自真实缝），报告必须恰指向
                // 该缝：状态位 Mismatch+StepMismatches 唯一条目+其余六步照常
                // Passed+链照常跑完（不异常遮蔽）。Logger 缝判据=诊断行。──
                Group("分 seam 定位：Events 一红", () =>
                {
                    NoOpFeatureRegistration.ProbeState probe;
                    List<string> lines;
                    RunProbe(NoOpProbeFault.EventsPublishExpectation, 4131, out probe, out lines);
                    Check(probe != null && probe.Started,
                        "非遮蔽：Events 一红链仍跑完（一步失败不异常遮蔽全链）");
                    Check(probe.EventsStep == ProbeStepOutcome.Mismatch
                            && ContainsDiagnostic(lines, "reason=event-id-mismatch"),
                        "定位：错挂声明被真实路由拒=仅 Events 步 Mismatch+负判据行");
                    Check(probe.StepMismatches.Count == 1 && probe.StepMismatches[0].StartsWith("events:"),
                        "定位：失败详情恰一条且指向 Events 缝（详情=" + string.Join("|", probe.StepMismatches) + "）");
                    Check(probe.BootstrapStep == ProbeStepOutcome.Passed && probe.LifecycleStep == ProbeStepOutcome.Passed
                            && probe.NetworkStep == ProbeStepOutcome.Passed && probe.HostTickStep == ProbeStepOutcome.Passed
                            && probe.SettingsStep == ProbeStepOutcome.Passed && probe.LoggerStep == ProbeStepOutcome.Passed,
                        "非遮蔽：其余六步照常 Passed");
                });

                Group("分 seam 定位：Network 一红", () =>
                {
                    NoOpFeatureRegistration.ProbeState probe;
                    List<string> lines;
                    RunProbe(NoOpProbeFault.NetworkSendExpectation, 4132, out probe, out lines);
                    Check(probe != null && probe.Started && probe.NetworkStep == ProbeStepOutcome.Mismatch
                            && probe.NetworkSendObserved == NetworkSendResult.ChannelNotRegistered,
                        "定位：未注册通道真实被通道门禁拒=仅 Network 步 Mismatch（观察值入详情）");
                    Check(probe.StepMismatches.Count == 1 && probe.StepMismatches[0].StartsWith("network:"),
                        "定位：失败详情恰一条且指向 Network 缝");
                    Check(probe.EventsStep == ProbeStepOutcome.Passed && probe.SettingsStep == ProbeStepOutcome.Passed
                            && probe.LoggerStep == ProbeStepOutcome.Passed && probe.HostTickStep == ProbeStepOutcome.Passed
                            && probe.LifecycleStep == ProbeStepOutcome.Passed && probe.BootstrapStep == ProbeStepOutcome.Passed,
                        "非遮蔽：其余六步照常 Passed");
                });

                Group("分 seam 定位：Settings 一红", () =>
                {
                    NoOpFeatureRegistration.ProbeState probe;
                    List<string> lines;
                    RunProbe(NoOpProbeFault.SettingsCommitExpectation, 4133, out probe, out lines);
                    Check(probe != null && probe.Started && probe.SettingsStep == ProbeStepOutcome.Mismatch
                            && !probe.SettingsCommitAccepted,
                        "定位：过期 ExpectedRevision 被乐观并发真实拒=仅 Settings 步 Mismatch");
                    Check(probe.StepMismatches.Count == 1 && probe.StepMismatches[0].StartsWith("settings:"),
                        "定位：失败详情恰一条且指向 Settings 缝");
                    Check(probe.EventsStep == ProbeStepOutcome.Passed && probe.NetworkStep == ProbeStepOutcome.Passed
                            && probe.LoggerStep == ProbeStepOutcome.Passed && probe.HostTickStep == ProbeStepOutcome.Passed
                            && probe.LifecycleStep == ProbeStepOutcome.Passed && probe.BootstrapStep == ProbeStepOutcome.Passed,
                        "非遮蔽：其余六步照常 Passed");
                });

                Group("分 seam 定位：Lifecycle 一红", () =>
                {
                    NoOpFeatureRegistration.ProbeState probe;
                    List<string> lines;
                    RunProbe(NoOpProbeFault.LifecycleTrackExpectation, 4134, out probe, out lines);
                    Check(probe != null && probe.Started && probe.LifecycleStep == ProbeStepOutcome.Mismatch
                            && probe.Tracked,
                        "定位：TryTrack 真实受理被错置期望=仅 Lifecycle 步 Mismatch（真实行为未动）");
                    Check(probe.StepMismatches.Count == 1 && probe.StepMismatches[0].StartsWith("lifecycle:"),
                        "定位：失败详情恰一条且指向 Lifecycle 缝");
                    Check(probe.EventsStep == ProbeStepOutcome.Passed && probe.NetworkStep == ProbeStepOutcome.Passed
                            && probe.LoggerStep == ProbeStepOutcome.Passed && probe.HostTickStep == ProbeStepOutcome.Passed
                            && probe.SettingsStep == ProbeStepOutcome.Passed && probe.BootstrapStep == ProbeStepOutcome.Passed,
                        "非遮蔽：其余六步照常 Passed");
                });

                Group("分 seam 定位：Logger 一红（判据=独立诊断行）", () =>
                {
                    NoOpFeatureRegistration.ProbeState probe;
                    List<string> lines;
                    RunProbe(NoOpProbeFault.LoggerInvalidEventWrite, 4135, out probe, out lines);
                    Check(probe != null && probe.Started && probe.LoggerInvalidAttempt,
                        "setup：无效标识符负写已发生（07 冻结=拒写面）");
                    Check(!ContainsDiagnostic(lines, "diagnosticId=BUE-NOOP-INVALID")
                            && ContainsDiagnostic(lines, "BUE-LOG-002")
                            && ContainsDiagnostic(lines, "event=noop-probe-info"),
                        "定位：无效写被消毒纪律真实拒=BUE-LOG-002 留痕且无模块行，同时三正行照常（互不遮蔽）");
                    Check(probe.BootstrapStep == ProbeStepOutcome.Passed && probe.EventsStep == ProbeStepOutcome.Passed
                            && probe.NetworkStep == ProbeStepOutcome.Passed && probe.HostTickStep == ProbeStepOutcome.Passed
                            && probe.LifecycleStep == ProbeStepOutcome.Passed && probe.SettingsStep == ProbeStepOutcome.Passed
                            && probe.LoggerStep == ProbeStepOutcome.Passed && probe.StepMismatches.Count == 0,
                        "非遮蔽：void 窄面不受单行拒影响，七步状态全绿（判据全在行级）");
                });

                Group("注册缝：受理失败=链不运行（无伪装启动）", () =>
                {
                    NoOpFeatureRegistration.ResetLastProbe();
                    var runtime = new FeatureRegistrationRuntime();
                    runtime.OpenRegistration();
                    Check(runtime.CompleteRuntime(), "setup：目录先冻结（注册窗口关）");
                    var late = runtime.Register(NoOpFeatureRegistration.ProbeRegistration);
                    Check(!late.Accepted && late.Reason == FeatureRegistrationReason.PhaseClosed
                            && late.DiagnosticId == "BUE-REG-003",
                        "注册缝：窗口外注册=显式拒（PhaseClosed/BUE-REG-003，作者按 reason 分支降级）");
                    BueFeatureStartRuntime.StartCatalog(runtime, NewLoopbackNetwork(4136UL));
                    Check(NoOpFeatureRegistration.LastProbe == null,
                        "注册缝：被拒登记不产启动——链无探针可报（LastProbe=null 显式可区分于「跑过」）");
                    BueFeatureStartRuntime.StopAll(FeatureStopReason.PluginStopping);
                });

                Group("SDK 附录与活样板双向锚定（结构/码表/清单/示例=源码逐字）", () =>
                {
                    var solutionRoot = new DirectoryInfo(AppContext.BaseDirectory);
                    while (solutionRoot != null && !System.IO.File.Exists(System.IO.Path.Combine(solutionRoot.FullName, "BetterUnturnedExperience.sln")))
                        solutionRoot = solutionRoot.Parent;
                    Check(solutionRoot != null, "文档锚：从测试基目录定位到仓库根");
                    var docPath = System.IO.Path.Combine(solutionRoot.FullName, "docs", "sdk", "BetterUnturnedExperience-SDK-Assembly-Identity.md");
                    var fixturePath = System.IO.Path.Combine(solutionRoot.FullName, "src", "BetterUnturnedExperience.NoOpFixture", "NoOpFeaturePlugin.cs");
                    Check(System.IO.File.Exists(docPath), "文档锚：SDK 契约文档在唯一事实源位置");
                    var doc = System.IO.File.ReadAllText(docPath);
                    var fixtureSource = System.IO.File.ReadAllText(fixturePath);
                    // 结构：三附录成文、A 恰七节、B/C 标题、清单在 C 内。
                    var sliceAStart = doc.IndexOf("## 附录 A", StringComparison.Ordinal);
                    var sliceBStart = doc.IndexOf("## 附录 B", StringComparison.Ordinal);
                    var sliceCStart = doc.IndexOf("## 附录 C", StringComparison.Ordinal);
                    Check(sliceAStart >= 0 && sliceBStart > sliceAStart && sliceCStart > sliceBStart,
                        "结构锚：附录 A/B/C 依序成文");
                    // 收集模式下结构缺失时干净收束（红测自身卫生：后续切片
                    // 断言以结构存在为前提，不得以 Substring 异常冒充红因）。
                    if (sliceAStart < 0 || sliceBStart <= sliceAStart || sliceCStart <= sliceBStart) return;
                    var sliceA = doc.Substring(sliceAStart, sliceBStart - sliceAStart);
                    var sliceB = doc.Substring(sliceBStart, sliceCStart - sliceBStart);
                    var sliceC = doc.Substring(sliceCStart);
                    for (var i = 1; i <= 7; i++)
                        Check(sliceA.Contains("### A." + i + " "), "结构锚：A." + i + " 节成文");
                    Check(sliceA.Contains("NoOpFeaturePlugin.cs"), "结构锚：附录 A 指回活样板出处");
                    // 附录 B：逐码登记（拒绝码表+观察行+前缀纪律+保留段示例）。
                    var codes = new[]
                    {
                        "BUE-REG-ACCEPT", "BUE-REG-001", "BUE-REG-002", "BUE-REG-003", "BUE-REG-004", "BUE-REG-005",
                        "BUE-REG-006", "BUE-REG-007", "BUE-REG-008", "BUE-REG-009", "BUE-REG-010", "BUE-REG-011",
                        "BUE-PLATFORM-001", "BUE-PLATFORM-002",
                        "BUE-EVT-ACCEPT", "BUE-EVT-001", "BUE-EVT-002", "BUE-EVT-003", "BUE-EVT-004",
                        "BUE-LIFE-STATE", "BUE-LIFE-ACCEPT", "BUE-LIFE-RELEASE", "BUE-LIFE-ISOLATE",
                        "BUE-LIFE-001", "BUE-LIFE-002", "BUE-LIFE-003", "BUE-LIFE-004", "BUE-LIFE-005", "BUE-LIFE-006",
                        "BUE-NET-001", "BUE-NET-002", "BUE-NET-003", "BUE-NET-004", "BUE-NET-005",
                        "BUE-MT-ACCEPT", "BUE-MT-001", "BUE-MT-002", "BUE-MT-003", "BUE-MT-004", "BUE-MT-005", "BUE-MT-006",
                        "BUE-MT-GEN", "BUE-MT-CREATED",
                        "BUE-CLOCK-001",
                        "BUE-SET-001", "BUE-SET-002", "BUE-SET-003", "BUE-SET-004", "BUE-SET-005",
                        "BUE-SET-CREATED", "BUE-SET-GEN",
                        "BUE-LOG-001", "BUE-LOG-002", "BUE-LOG-003", "BUE-LOG-004", "BUE-LOG-005",
                        "BUE-LOG-CREATED", "BUE-LOG-GEN",
                    };
                    foreach (var code in codes)
                        Check(sliceB.Contains("`" + code + "`"), "码表锚：" + code + " 入附录 B");
                    Check(sliceB.Contains("宿主观察行") && sliceB.Contains("拒绝码表"),
                        "码表锚：观察行 vs 拒绝码两分类口径成文（04/05/06 先例总装）");
                    Check(sliceB.Contains("com.acme.medical-overlay") && sliceB.Contains("io.github.yu80rice.bue.medical-overlay"),
                        "身份锚：FeatureId 合法/非法示例对照成文");
                    Check(sliceB.Contains("diagnostic-summary"),
                        "行形锚：T8 冻结摘要行形入 B（featureId/diagnosticId/level/count/firstSeen/lastSeen）");
                    // 附录 C：2.1 条目、安全降级、Major 纪律、四条件门禁、RELEASES。
                    Check(sliceC.Contains("2.0→2.1") && sliceC.Contains("安全降级") && sliceC.Contains("RELEASES"),
                        "版本锚：2.0→2.1 加性条目+安全降级+RELEASES 注记成文");
                    Check(sliceC.Contains("四条件") && sliceC.Contains("重评义务") && sliceC.Contains("继续暂缓")
                            && sliceC.Contains("触发信号") && sliceC.Contains("编译脱耦"),
                        "门禁锚：Contracts 拆分四条件逐条含定义/事实判定/触发信号/重评义务，当前=继续暂缓");
                    for (var item = 1; item <= 11; item++)
                        Check(sliceC.Contains("- [ ] " + item + "."), "自检清单锚：第 " + item + " 项可打勾");
                    // 双向锚定：文档示例=NoOpFixture 源码逐字（示例可编译的机器
                    // 化替身——编译期验证工具属 T9 裁决①不建项，示例即活样板
                    // 本体，活样板已被本套件在跑）；样板在文档有出处。
                    var sharedSnippets = new[]
                    {
                        "[BepInDependency(\"io.github.yu80rice.betterunturnedexperience\", BepInDependency.DependencyFlags.HardDependency)]",
                        "BueRuntimeHost.Register(ProbeRegistration)",
                        "bootstrap.EventRegistry.Register<NoOpProbeEvent>(ProbeEventId)",
                        "bootstrap.OwnedEvents.TryPublish(publishId, new NoOpProbeEvent(1UL))",
                        "bootstrap.Lifetime.TryTrack(first)",
                        "bootstrap.Network.RegisterChannel(channel, new ContractVersion(2, 0), 1)",
                        "bootstrap.MainThread.Post(() => { })",
                        "settings.GetSnapshot(SettingRevisionScope.ClientPreference)",
                        "logger.Info(\"noop-probe-info\", \"BUE-NOOP-INFO\")",
                    };
                    foreach (var snippet in sharedSnippets)
                    {
                        Check(fixtureSource.Contains(snippet), "双向锚：示例代码行真实存在于活样板源码 " + snippet);
                        Check(doc.Contains(snippet), "双向锚：示例代码行逐字进文档 " + snippet);
                    }
                    Check(doc.Contains("io.github.yu80rice.bue.noop/probe-completed"),
                        "双向锚：样例事件身份串（文档示例↔ProbeEventId）两处一致");
                    Check(fixtureSource.Contains("ProbeEventId = \"io.github.yu80rice.bue.noop/probe-completed\""),
                        "双向锚：ProbeEventId 常量=文档所引事件身份串");
                });
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            finally
            {
                // 06/07 先例卫生：设置运行时清账+临时持久根删除（收集不删除
                // =泄漏，R1-Standards P2 修复）。
                BetterUnturnedExperience.Plugin.BueSettingsRuntime.Clear();
                for (var index = 0; index < settingsRoots.Count; index++)
                {
                    try { if (System.IO.Directory.Exists(settingsRoots[index])) System.IO.Directory.Delete(settingsRoots[index], true); }
                    catch (Exception) { }
                }
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V3-08 unified probe collection: ALL GREEN (0 failures) — groups: 全链绿/停止边界与再启用/Start 故障隔离/分 seam 定位 Events·Network·Settings·Lifecycle·Logger/注册缝拒/文档双向锚定");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V3-08 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        private static void AssertBueV2LhtAdoption(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        var frame = error.StackTrace != null ? error.StackTrace.Split(new[] { '\n' }, 2)[0].Trim() : "<no stack>";
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message + " @ " + frame));
                    }
                }

                Group("信标守卫", () => LhtGroupBeaconContextGuard(Check));
                Group("组播不含本地", () => LhtGroupMulticastNoLocal(Check));
                Group("BUE 帧不可靠 1:1", () => LhtGroupUnreliableOneToOneEpoch(Check));
                Group("enabled=false 完整停摆", () => LhtGroupFullStop(Check));
                Group("U3DS 双状态", () => LhtGroupPresentationStates(Check));
                Group("端到端全链", () => LhtGroupEndToEnd(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V2-20 LHT adoption collection: ALL GREEN (0 failures) — groups: 信标守卫/组播不含本地/BUE 帧不可靠 1:1/enabled=false 完整停摆/U3DS 双状态/端到端全链");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-20 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        /// <summary>Records every engine-facing call the LHT tracking module makes (test double).</summary>
        private sealed class FakeHordeAuthority : IHordeTrackingAuthority
        {
            public int BeaconSubscribeCalls, BeaconUnsubscribeCalls, HostedSubscribeCalls, HostedUnsubscribeCalls;
            public int CommandFlushCalls, CommandDeregisterCalls, StartNotifyCalls, EndNotifyCalls;
            public bool ServerRole = true;
            public HordeBeaconView NextBeacon;
            public int Remaining, Alive;

            public bool IsServerRole() { return ServerRole; }
            public void SubscribeBeaconUpdated(System.Action<byte, bool> handler) { BeaconSubscribeCalls++; }
            public void UnsubscribeBeaconUpdated(System.Action<byte, bool> handler) { BeaconUnsubscribeCalls++; }
            public void SubscribeServerHosted(System.Action handler) { HostedSubscribeCalls++; }
            public void UnsubscribeServerHosted(System.Action handler) { HostedUnsubscribeCalls++; }
            public bool TryResolveBeacon(byte nav, out HordeBeaconView beacon) { beacon = NextBeacon; return NextBeacon != null; }
            public bool TryReadCounters(HordeBeaconView beacon, out int remaining, out int alive)
            { remaining = Remaining; alive = Alive; return true; }
            public bool TryFlushHordeCommandRegistration(HordeStatusSource source) { CommandFlushCalls++; return true; }
            public void DeregisterHordeCommand() { CommandDeregisterCalls++; }
            public void NotifyHordeStart(HordeBeaconView beacon) { StartNotifyCalls++; }
            public void NotifyHordeEnd(HordeBeaconView beacon) { EndNotifyCalls++; }
        }

        /// <summary>Recording HUD surface (the presentation adapter's test double).</summary>
        private sealed class FakeHudSurface : IHordeHudSurface
        {
            public int InstallCalls, UninstallCalls, DrainCalls;
            public bool LabelReady = true;
            public bool ThrowOnRender;
            public readonly List<string> Texts = new List<string>();
            public readonly List<bool> Visibility = new List<bool>();

            public bool Install() { InstallCalls++; return true; }
            public void Uninstall() { UninstallCalls++; }
            public bool IsLabelReady() { return LabelReady; }
            public void SetText(string text)
            { if (ThrowOnRender) throw new InvalidOperationException("hud fault"); Texts.Add(text); }
            public void SetVisible(bool visible)
            { if (ThrowOnRender) throw new InvalidOperationException("hud fault"); Visibility.Add(visible); }
            public void DrainDisconnectReset() { DrainCalls++; }
        }

        /// <summary>A probe network for the tracking/receive paths: counts registration and
        /// subscription traffic and records every multicast attempt (payload + reliability).</summary>
        private sealed class LhtSendProbeNetwork : IBueNetworkApi
        {
            public int RegisterCalls, UnregisterCalls, SubscribeCalls, DisposedHandles;
            public readonly List<(byte[] Payload, bool Reliable)> Multicasts = new List<(byte[], bool)>();

            public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
            { RegisterCalls++; return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "PROBE"); }
            public bool UnregisterChannel(FeatureId channel) { UnregisterCalls++; return true; }
            public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
            { SubscribeCalls++; return new LhtProbeHandle(this); }
            public IReadOnlyList<IConnectionSession> Sessions { get { return new IConnectionSession[0]; } }
            public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }
            public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
            { Multicasts.Add((payload, reliable)); return NetworkSendResult.Sent; }
            public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable) { return NetworkSendResult.NoSession; }

            private sealed class LhtProbeHandle : IDisposable
            {
                private readonly LhtSendProbeNetwork owner;
                internal LhtProbeHandle(LhtSendProbeNetwork owner) { this.owner = owner; }
                public void Dispose() { owner.DisposedHandles++; }
            }
        }

        /// <summary>Builds a started LHT module on a fresh bus with the given fakes (no patches pinned: host-test process).</summary>
        private static HordeTrackerModule NewLhtModule(BetterUnturnedExperience.Core.Events.FeatureEventBus bus, IBueNetworkApi network, FakeHordeAuthority authority, FakeHudSurface surface, bool isServer, bool canUseClientUi = true)
        {
            var feature = new FeatureId(LhtRuntime.FeatureIdValue);
            var module = new HordeTrackerModule(feature);
            module.AuthorityFactoryForTests = () => authority;
            module.HudSurfaceFactoryForTests = () => surface;
            module.CanUseClientUiForTests = () => canUseClientUi;
            authority.ServerRole = isServer;
            // DEV-V3-06: same host-shaped settings wiring as the LIR fixture.
            var lhtSettings = new FeatureSettingsRegistry(new InMemorySettingsPersistence(), () => true, null);
            lhtSettings.GetOrCreateRuntime(feature, HordeTrackerModule.CreateSettingsDescriptors(feature));
            lhtSettings.OpenGeneration(feature, 1UL);
            var bootstrap = new FeatureBootstrap(default(FeatureScopeIdentity), 1UL, lhtSettings.CreateView(feature, 1UL), bus.Subscriber(feature), bus.Publisher(feature), bus.EventRegistry(feature), null, null, null, network);
            var result = module.Start(bootstrap);
            if (!result.Started) throw new InvalidOperationException("harness: LHT module start failed: " + result.DiagnosticId);
            return module;
        }

        /// <summary>One fake host frame: monotonic tick numbers, the given delta seconds.</summary>
        private static HostTick LhtTick(ulong number, float deltaSeconds)
        {
            return new HostTick(number, deltaSeconds, TickPhase.Update);
        }

        private static ulong lhtToggleRequestId;

        /// <summary>Submits the LHT enabled toggle through the frozen scoped settings seam (unique request id, current revision); returns acceptance.</summary>
        private static bool SubmitLhtToggle(HordeTrackerModule module, bool enabled)
        {
            var revision = module.SettingsView.GetSnapshot(SettingRevisionScope.ClientPreference).Revision;
            var result = module.SettingsView.Submit(new ScopedSettingChangeRequest(++lhtToggleRequestId, SettingRevisionScope.ClientPreference, revision,
                new[] { new SettingMutation("hordetracker.enabled", SettingValue.Toggle(enabled)) }));
            return result.Accepted;
        }

        /// <summary>The loopback two-endpoint harness: a server-side and a client-side LHT
        /// module over one automatic-handshake runtime pair (the LirMultiplayerHarness shape).
        /// Wire probes on both runtimes record exactly who receives what — the loopback
        /// proof that the local host is never its own multicast target.</summary>
        private sealed class LhtMultiplayerHarness
        {
            public BetterUnturnedExperience.Core.Network.LocalLoopbackPair Pair;
            public BetterUnturnedExperience.Core.Network.BueNetworkRuntime ServerRuntime;
            public BetterUnturnedExperience.Core.Network.BueNetworkRuntime ClientRuntime;
            public HordeTrackerModule ServerLht;
            public HordeTrackerModule ClientLht;
            public FakeHordeAuthority ServerAuthority;
            public FakeHordeAuthority ClientAuthority;
            public FakeHudSurface ServerSurface;
            public FakeHudSurface ClientSurface;
            public readonly List<byte[]> ClientInbound = new List<byte[]>();
            public readonly List<byte[]> ServerInbound = new List<byte[]>();
            public IConnectionSession ServerSession;
            private ulong tickNumber;

            public static LhtMultiplayerHarness Create()
            {
                var localContract = new ContractVersion(2, 0);
                var pair = BetterUnturnedExperience.Core.Network.LocalLoopbackTransport.CreatePair();
                var clientRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.First, localContract, 1001UL);
                var serverRuntime = new BetterUnturnedExperience.Core.Network.BueNetworkRuntime(pair.Second, localContract, 2002UL, handshakeInitiator: false);
                var harness = new LhtMultiplayerHarness
                {
                    Pair = pair,
                    ClientRuntime = clientRuntime,
                    ServerRuntime = serverRuntime,
                    ServerAuthority = new FakeHordeAuthority(),
                    ClientAuthority = new FakeHordeAuthority(),
                    ServerSurface = new FakeHudSurface(),
                    ClientSurface = new FakeHudSurface(),
                };
                var channel = new FeatureId(LhtRuntime.FeatureIdValue);
                clientRuntime.Subscribe(channel, ChannelDirection.FromServer, (s, p) => harness.ClientInbound.Add(p));
                serverRuntime.Subscribe(channel, ChannelDirection.FromClients, (s, p) => harness.ServerInbound.Add(p));
                serverRuntime.Subscribe(channel, ChannelDirection.FromServer, (s, p) => harness.ServerInbound.Add(p));
                harness.ServerBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                harness.ServerLht = NewLhtModule(harness.ServerBus, serverRuntime, harness.ServerAuthority, harness.ServerSurface, isServer: true);
                harness.ClientBus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
                harness.ClientLht = NewLhtModule(harness.ClientBus, clientRuntime, harness.ClientAuthority, harness.ClientSurface, isServer: false);
                return harness;
            }

            public BetterUnturnedExperience.Core.Events.FeatureEventBus ServerBus;
            public BetterUnturnedExperience.Core.Events.FeatureEventBus ClientBus;

            /// <summary>The automatic handshake: the client initiates; two pump rounds establish both sides.</summary>
            public void Handshake()
            {
                ClientRuntime.StartSession(2002UL);
                Pump();
                Pump();
                if (ClientRuntime.Sessions.Count != 1 || ServerRuntime.Sessions.Count != 1)
                    throw new InvalidOperationException("harness: handshake did not establish both sides");
                ServerSession = ServerRuntime.Sessions[0];
            }

            public void TickServer() { ServerLht.OnHostTick(LhtTick(++tickNumber, 0.1f)); }
            public void TickClient() { ClientLht.OnHostTick(LhtTick(++tickNumber, 0.1f)); }
            public void Pump() { Pair.First.Pump(); Pair.Second.Pump(); }
        }

        // Group 1 — the frozen beacon context guard: only the tracked active
        // beacon's counter changes mark the broadcast dirty; every other call
        // through the same native call site releases immediately with zero
        // state change, and the stop boundary clears the guard (spec「上下文
        // 守卫原则」：补丁可共享原生调用点，不能共享业务上下文).
        private static void LhtGroupBeaconContextGuard(Action<bool, string> check)
        {
            var guard = new HordeBeaconContextGuard();
            var unrelated = new object();
            check(!guard.TryRequestBroadcast(unrelated),
                "信标守卫：无追踪信标时补丁入口立即放行（context=false，不置脏不抛异常）");
            check(!guard.ConsumeBroadcastDirty(), "信标守卫：放行路径零脏标记");

            var beaconA = new object();
            guard.Track(beaconA);
            check(!guard.TryRequestBroadcast(unrelated),
                "信标守卫：非追踪信标走同一调用点立即放行（共享调用点不共享业务上下文）");
            check(!guard.ConsumeBroadcastDirty(), "信标守卫：无关信标不产生广播脏标记");
            check(guard.TryRequestBroadcast(beaconA), "信标守卫：追踪信标自身的计数变更命中上下文");
            check(guard.ConsumeBroadcastDirty() && !guard.ConsumeBroadcastDirty(),
                "信标守卫：脏标记单次消费（同帧多次击杀合并为一帧广播）");

            guard.Clear();
            check(!guard.TryRequestBroadcast(beaconA) && !guard.ConsumeBroadcastDirty(),
                "信标守卫：Clear 后上下文与脏标记零残留（停止闸门）");

            HordeTrackerModule.OnBeaconCounterPatched(null);
            check(true, "信标守卫：无活动模块时补丁静态入口静默放行（不 NRE 不抛）");
        }

        // Group 2 — session-driven multicast replaces the Provider.clients
        // handwritten loop + skip-local logic: the established session
        // snapshot is the target set, the local host is never in it (zero
        // loopback — structural, not a hand-written skip), and the host HUD
        // is driven by the local authority publish, never a network echo.
        private static void LhtGroupMulticastNoLocal(Action<bool, string> check)
        {
            HordeStateTracker.Clear();
            PendingHordeSnapshot.Reset();
            var harness = LhtMultiplayerHarness.Create();
            try
            {
                // Beacon activation with no session yet: local authority only, nothing on the wire.
                harness.ServerAuthority.NextBeacon = new HordeBeaconView(new object(), "发起者甲", "机场", 100);
                harness.ServerLht.Tracking.HandleBeaconUpdated(1, true);
                harness.TickServer(); harness.TickClient();
                check(harness.ClientInbound.Count == 0 && harness.ServerInbound.Count == 0,
                    "组播：无会话时广播静默（快照空 → NoSession，零上线帧）");
                check(HordeStateTracker.Read().IsActive && HordeStateTracker.Read().Total == 100,
                    "组播：本地权威发布照常（房主 HUD 数据源不依赖网络回环）");

                harness.Handshake();

                // One counter change → one dirty → exactly ONE update frame per established session.
                harness.ServerAuthority.Remaining = 37;
                harness.ServerAuthority.Alive = 40;
                harness.ServerLht.Tracking.OnBeaconCounterChanged(harness.ServerAuthority.NextBeacon.Beacon);
                harness.TickServer(); harness.Pump();
                check(harness.ClientInbound.Count == 1,
                    "组播：一次脏标记恰好一帧 Update 上线（会话驱动组播，1 会话 1 帧）");
                check(harness.ServerInbound.Count == 0,
                    "组播：本地主机零回环——服务器双方向入站探针全空（本地身份不在远端会话集合）");

                // Both HUDs render: the host from the local authority (same tick,
                // no Pump), the client after the frame arrives and drains.
                check(harness.ServerSurface.Texts.Count >= 1
                    && harness.ServerSurface.Texts[harness.ServerSurface.Texts.Count - 1].Contains("机场"),
                    "组播：房主 HUD 由本地权威驱动（广播同帧渲染，无需网络回环）");
                harness.TickClient();
                check(harness.ClientSurface.Texts.Count >= 1
                    && harness.ClientSurface.Texts[harness.ClientSurface.Texts.Count - 1].Contains("机场"),
                    "组播：客户端 HUD 收到广播后渲染（10Hz 表现层）");

                // The fallback cadence: the first event-free tick broadcasts once
                // (the migrated timer shape), then the 2s window holds — 25 more
                // ticks (2.5s) add exactly one more frame.
                var before = harness.ClientInbound.Count;
                harness.TickServer(); harness.Pump();
                check(harness.ClientInbound.Count == before + 1, "组播：兜底路径上线（防事件丢失 + 新客机同步）");
                before = harness.ClientInbound.Count;
                for (int i = 0; i < 25; i++) harness.TickServer();
                harness.Pump();
                check(harness.ClientInbound.Count == before + 1, "组播：2s 周期兜底在窗口内恰好一帧");
            }
            finally
            {
                harness.ServerLht.Stop(FeatureStopReason.PluginStopping);
                harness.ClientLht.Stop(FeatureStopReason.PluginStopping);
            }
        }

        // Group 3 — the BUE-frame unreliable 1:1 re-proof (T2 handover point:
        // the 08 baseline proved 1:1 on the LMN path; the session-driven
        // SendToClients BUE-frame path must re-prove it) plus the epoch /
        // sequence lifecycle parity with the 08 baseline.
        private static void LhtGroupUnreliableOneToOneEpoch(Action<bool, string> check)
        {
            HordeStateTracker.Clear();
            PendingHordeSnapshot.Reset();
            var authority = new FakeHordeAuthority
            {
                NextBeacon = new HordeBeaconView(new object(), "发起者乙", "工厂", 50),
                Remaining = 20,
                Alive = 25,
            };
            var probe = new LhtSendProbeNetwork();
            var tracking = new HordeTrackingModule(authority, new DefaultHordeTrackingPolicy(), probe, () => true);
            tracking.Activate();
            try
            {
                tracking.HandleBeaconUpdated(1, true);
                tracking.Tick(0.1f);
                check(probe.Multicasts.Count == 1 && !probe.Multicasts[0].Reliable,
                    "1:1：Update 广播走不可靠通道（Update 不可靠 / Clear 可靠语义保持）");

                for (int i = 0; i < 63; i++)
                {
                    tracking.OnBeaconCounterChanged(authority.NextBeacon.Beacon);
                    tracking.Tick(0.1f);
                }
                check(probe.Multicasts.Count == 64,
                    "1:1：64 个事件窗口恰好 64 帧不可靠 Update（同帧合并 + 每帧至多一帧）");

                var seen = new HashSet<uint>();
                for (int i = 0; i < 64; i++)
                {
                    check(HordeWireCodec.TryReadUpdate(probe.Multicasts[i].Payload, out var snapshot)
                        && snapshot.Epoch == 1 && seen.Add(snapshot.Sequence),
                        "1:1：第 " + (i + 1) + " 帧 epoch=1 且 sequence 无重复键（08 基线语义）");
                }

                tracking.HandleBeaconUpdated(1, false);
                tracking.Tick(0.1f);
                check(probe.Multicasts.Count == 65 && probe.Multicasts[64].Reliable,
                    "1:1：Clear 恰一帧且走可靠通道（尸潮结束不丢）");
                check(HordeWireCodec.TryReadClear(probe.Multicasts[64].Payload, out var clearEpoch, out var clearSeq)
                    && clearEpoch == 1 && clearSeq == 65,
                    "1:1：Clear 携带 epoch=1 的终结 sequence=65（Update/Clear 共享序号空间）");

                tracking.Tick(0.1f);
                tracking.Tick(0.1f);
                check(probe.Multicasts.Count == 65, "1:1：Clear 后零重复 clear（LastBroadcastedActive 停止闸门）");

                // Reactivation: a NEW epoch, the sequence space restarts at 1.
                tracking.HandleBeaconUpdated(2, true);
                tracking.Tick(0.1f);
                check(probe.Multicasts.Count == 66
                    && HordeWireCodec.TryReadUpdate(probe.Multicasts[65].Payload, out var reborn)
                    && reborn.Epoch == 2 && reborn.Sequence == 1,
                    "1:1：信标再激活进入 epoch=2 且序号从 1 重启（旧 epoch 延迟包无法复活新 epoch）");
            }
            finally { tracking.Deactivate(); }
            check(authority.BeaconUnsubscribeCalls == 1, "1:1：Deactivate 退订引擎事件（代际清理）");
        }

        // Group 4 — enabled=false is a FULL stop: server tracking unsubscribed
        // (no events → no broadcasts, no /horde), the client subscription is
        // released + mailbox/state cleared, the HUD goes dark, the channel is
        // unregistered; re-enable re-arms everything (the old OnDestroy
        // teardown shape, now riding the settings seam).
        private static void LhtGroupFullStop(Action<bool, string> check)
        {
            HordeStateTracker.Clear();
            PendingHordeSnapshot.Reset();
            var bus = new BetterUnturnedExperience.Core.Events.FeatureEventBus();
            var probe = new LhtSendProbeNetwork();
            var authority = new FakeHordeAuthority
            {
                NextBeacon = new HordeBeaconView(new object(), "发起者丙", "农场", 80),
                Remaining = 30,
                Alive = 30,
            };
            var surface = new FakeHudSurface();
            var module = NewLhtModule(bus, probe, authority, surface, isServer: true);
            check(module.Enabled, "停摆：默认开启（唯一持久化开关 enabled）");
            check(probe.SubscribeCalls == 1, "停摆：启动时客户端接收恰一次订阅");

            check(SubmitLhtToggle(module, false), "停摆：面板开关可关闭");
            module.RefreshSwitches();
            check(!module.Enabled && !module.PatchesInstalled, "停摆：关闭后自身补丁已撤（原生回退）");
            check(authority.BeaconUnsubscribeCalls == 1, "停摆：服务器信标事件已退订（追踪停摆）");
            check(authority.CommandDeregisterCalls == 1, "停摆：/horde 已注销（关闭后无 LHT 行为）");
            check(probe.UnregisterCalls == 1 && probe.DisposedHandles == 1,
                "停摆：频道已注销 + 接收订阅已释放（完整停摆含网络）");

            // Server: beacon activation produces nothing at all.
            module.Tracking.HandleBeaconUpdated(1, true);
            module.OnHostTick(LhtTick(1, 0.1f));
            check(probe.Multicasts.Count == 0 && authority.StartNotifyCalls == 0,
                "停摆：关闭后信标激活零广播零通知");

            // Client: the receive gate is closed, the state cleared, the HUD dark.
            check(!module.Receiver.AcceptFrames, "停摆：客户端接收闸门已关（旧回调无法回写状态）");
            check(HordeStateTracker.Read().IsActive == false, "停摆：状态追踪已清空");
            check(surface.UninstallCalls == 1, "停摆：HUD surface 已卸载");
            module.OnHostTick(LhtTick(2, 0.1f));
            check(surface.Texts.Count == 0 && surface.Visibility.Count == 0, "停摆：关闭后 HUD 零渲染");

            // Re-enable re-arms: network re-bind, events re-subscribed, patches re-installed.
            check(SubmitLhtToggle(module, true), "停摆：可重新开启（提交受理）");
            module.RefreshSwitches();
            check(module.Enabled && probe.RegisterCalls == 2 && probe.SubscribeCalls == 2,
                "停摆：重臂后频道重注册 + 接收重订阅");
            check(authority.BeaconSubscribeCalls == 2, "停摆：重臂后信标事件重订阅");
            check(module.PatchesInstalled || module.StartGateDiagnostics.Length > 0,
                "停摆：重臂后补丁重装（测试进程装不上时如实留诊断）");
            module.Tracking.HandleBeaconUpdated(1, true);
            module.OnHostTick(LhtTick(3, 0.1f));
            check(probe.Multicasts.Count == 1, "停摆：重臂后广播恢复");
            module.Stop(FeatureStopReason.UserDisabled);
        }

        // Group 5 — identity + the U3DS presentation split: the feature state
        // is Available in both processes (the module starts headless), the
        // presentation state is HeadlessOnly when the batch-mode gate is
        // closed (no PlayerLifeUI HUD), Available otherwise.
        private static void LhtGroupPresentationStates(Action<bool, string> check)
        {
            check(LhtRuntime.FeatureIdValue == "io.github.yu80rice.bue.horde-tracker",
                "身份：FeatureId 冻结为 io.github.yu80rice.bue.horde-tracker（频道身份同串，旧频道退役）");
            check(LhtRuntime.DisplayName == "更好的尸潮播报", "身份：官方中文名「更好的尸潮播报」");

            // Headless (batch-mode): starts fine, the HUD surface is never installed.
            var headlessSurface = new FakeHudSurface();
            var headlessAuthority = new FakeHordeAuthority();
            var headless = NewLhtModule(new BetterUnturnedExperience.Core.Events.FeatureEventBus(), new LhtSendProbeNetwork(),
                headlessAuthority, headlessSurface, isServer: true, canUseClientUi: false);
            check(headless.Started, "U3DS：无头进程模块正常启动（功能 Available，表现不阻塞验收）");
            check(headless.PresentationState == FeaturePresentationState.HeadlessOnly, "U3DS：无头进程表现状态=HeadlessOnly");
            check(headlessSurface.InstallCalls == 0, "U3DS：无头不装 HUD surface（PlayerLifeUI 隔离）");
            check(headlessAuthority.BeaconSubscribeCalls == 1, "U3DS：无头进程追踪照常激活（服务器权威与表现解耦）");
            headless.Stop(FeatureStopReason.PluginStopping);

            // Headful: Available.
            var headful = NewLhtModule(new BetterUnturnedExperience.Core.Events.FeatureEventBus(), new LhtSendProbeNetwork(),
                new FakeHordeAuthority(), new FakeHudSurface(), isServer: true, canUseClientUi: true);
            check(headful.PresentationState == FeaturePresentationState.Available, "U3DS：有头进程表现状态=Available");
            headful.Stop(FeatureStopReason.PluginStopping);

            // HUD 失败只降表现不伤权威追踪：surface 渲染异常被表现层适配器隔离。
            HordeStateTracker.Clear();
            HordeStateTracker.Publish(new HordeSnapshot(true, 1, 1, 10, 100, "地点", "发起者"));
            var throwingAdapter = new HordePresentationAdapter(new FakeHudSurface { ThrowOnRender = true });
            Exception hudLeak = null;
            try { throwingAdapter.Tick(10f); }
            catch (Exception error) { hudLeak = error; }
            check(hudLeak == null, "U3DS：HUD 渲染异常被表现层隔离（只降表现不伤权威追踪）");
            HordeStateTracker.Clear();

            // The official registration: definition identity + contract floor.
            var registration = HordeTrackerFeatureRegistration.CreateRegistration();
            check(registration.Definition.Feature.Value == LhtRuntime.FeatureIdValue,
                "身份：官方注册定义携带 FeatureId（面板条目身份=FeatureId）");
            check(registration.MinimumBueContract.Major == 2, "身份：MinimumBueContract 对齐契约 2.0");
        }

        // Group 6 — the platform's FIRST real consumer end-to-end: register
        // channel → subscribe → SendToClients over the loopback transport
        // (fake transport, full chain green), epoch lifecycle + 1:1 delivery
        // on the BUE frame path.
        private static void LhtGroupEndToEnd(Action<bool, string> check)
        {
            HordeStateTracker.Clear();
            PendingHordeSnapshot.Reset();
            var harness = LhtMultiplayerHarness.Create();
            try
            {
                check(harness.ServerLht.Tracking.Channel.Value == LhtRuntime.FeatureIdValue,
                    "全链：频道身份 = FeatureId（一词一贯）");

                // Activation before any session: local authority only, zero wire noise.
                harness.ServerAuthority.NextBeacon = new HordeBeaconView(new object(), "发起者丁", "军事基地", 100);
                harness.ServerLht.Tracking.HandleBeaconUpdated(1, true);
                harness.TickServer(); harness.TickClient();
                check(harness.ClientInbound.Count == 0, "全链：无会话时广播静默（NoSession，客机零噪声）");
                check(harness.ServerSurface.Texts.Count >= 1, "全链：房主 HUD 本地权威渲染");

                harness.Handshake();
                check(harness.ServerSession != null, "全链：会话建立（自动握手）");

                // 32 kill windows: one dirty each → exactly 32 unreliable frames 1:1.
                for (int i = 0; i < 32; i++)
                {
                    harness.ServerAuthority.Remaining = 90 - i * 2;
                    harness.ServerLht.Tracking.OnBeaconCounterChanged(harness.ServerAuthority.NextBeacon.Beacon);
                    harness.TickServer(); harness.Pump(); harness.TickClient();
                }
                check(harness.ClientInbound.Count == 32,
                    "全链：32 个事件窗口恰好 32 帧上线（BUE 帧不可靠 1:1 复验）");
                var seqs = new List<uint>();
                foreach (var payload in harness.ClientInbound)
                {
                    if (HordeWireCodec.TryReadUpdate(payload, out var snapshot)) seqs.Add(snapshot.Sequence);
                }
                check(seqs.Count == 32, "全链：客户端收到的 32 帧全部可解析");
                var monotonic = seqs.Count > 0;
                for (int i = 1; i < seqs.Count; i++) monotonic &= seqs[i] == seqs[i - 1] + 1;
                check(monotonic, "全链：客户端视角 sequence 严格连续无重复键（08 基线）");
                check(harness.ClientSurface.Texts.Count >= 1
                    && harness.ClientSurface.Texts[harness.ClientSurface.Texts.Count - 1].Contains("军事基地"),
                    "全链：客户端 HUD 渲染尸潮地点（10Hz 表现层）");

                // The horde ends: one clear, the client HUD hides.
                harness.ServerLht.Tracking.HandleBeaconUpdated(1, false);
                harness.TickServer(); harness.Pump(); harness.TickClient();
                check(harness.ClientInbound.Count == 33, "全链：尸潮平息 Clear 恰一帧");
                check(harness.ClientSurface.Visibility.Count >= 1
                    && harness.ClientSurface.Visibility[harness.ClientSurface.Visibility.Count - 1] == false,
                    "全链：Clear 后客户端 HUD 隐藏（分支 B）");
                check(HordeStateTracker.Read().IsActive == false, "全链：状态追踪回到空（epoch 终结）");
            }
            finally
            {
                harness.ServerLht.Stop(FeatureStopReason.PluginStopping);
                harness.ClientLht.Stop(FeatureStopReason.PluginStopping);
            }
        }

        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        // ═════════════════════════════════════════════════════════════════════
        // DEV-V2-23: the platform double-install self-check red collection —
        // the ticket-frozen six cases over the assembly-list injection seam
        // (no filesystem):
        //   1. 无冲突          — clean domain (injected + the real test AppDomain):
        //                        no conflict, no emission, mapper marks self
        //   2. 同程序集名冲突   — same simple name at a foreign path (incl.
        //                        case-variant name: binding-fusion conservative
        //                        over-report) → exactly the copy path reported
        //   3. 不同程序集名     — Contracts/Core/foreign names → clean
        //   4. 空路径           — null/empty location → still reported with the
        //                        unknown-location token, never throws
        //   5. 重复条目         — duplicated listings dedupe to one finding
        //   6. 诊断 id 与关键字段 — BUE-PLATFORM-001 + assembly/location/selfPath/
        //                        suggestion on the log line and panel notice;
        //                        Warning-level emission, one line per location
        // The check is diagnostic-only: it never deletes files and never blocks
        // bootstrap (no file/IO call exists on the decision core).
        // Panel visibility is ticket scope too but NOT part of the frozen
        // six-case injection-seam anchor (F1 R1): AssertPlatformPanelNotice
        // rides the always-run suite below instead.
        // ═════════════════════════════════════════════════════════════════════
        private static void AssertBueV2PlatformSelfCheck(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) when (collectAllFailures)
                    {
                        var frame = error.StackTrace != null ? error.StackTrace.Split(new[] { '\n' }, 2)[0].Trim() : "<no stack>";
                        reds.Add("[" + name + "] " + (error is InvalidOperationException ? error.Message : "UNEXPECTED " + error.GetType().Name + ": " + error.Message + " @ " + frame));
                    }
                }

                Group("无冲突", () => PlatformGroupNoConflict(Check));
                Group("同程序集名冲突", () => PlatformGroupNameConflict(Check));
                Group("不同程序集名", () => PlatformGroupDifferentName(Check));
                Group("空路径", () => PlatformGroupEmptyPath(Check));
                Group("重复条目", () => PlatformGroupDuplicateEntries(Check));
                Group("诊断 id 与关键字段", () => PlatformGroupDiagnosticFields(Check));
            }
            catch (Exception error) when (collectAllFailures)
            {
                reds.Add("UNEXPECTED: " + error.GetType().FullName + ": " + error.Message);
            }
            if (collectAllFailures && reds.Count == 0)
                Console.WriteLine("DEV-V2-23 platform self-check collection: ALL GREEN (0 failures) — groups: 无冲突/同程序集名冲突/不同程序集名/空路径/重复条目/诊断 id 与关键字段");
            if (collectAllFailures && reds.Count > 0)
                throw new InvalidOperationException("DEV-V2-23 red collection (" + reds.Count + "): " + string.Join(" || ", reds));
        }

        private static BueLoadedAssemblyView PlatformView(string simpleName, string location, bool isSelf)
        {
            return new BueLoadedAssemblyView(simpleName, location, isSelf);
        }

        private static readonly string PlatformSelfPath = "E:\\Steam\\steamapps\\common\\Unturned\\BepInEx\\plugins\\BetterUnturnedExperience.dll";
        private static readonly string PlatformCopyPath = "E:\\Steam\\steamapps\\common\\Unturned\\BepInEx\\plugins\\SomeInventoryExtension\\BetterUnturnedExperience.dll";

        // Group 1: no conflict — injected clean list AND the real test AppDomain
        // (exactly one BUE assembly loaded) both report clean with zero emission.
        private static void PlatformGroupNoConflict(Action<bool, string> check)
        {
            var clean = new List<BueLoadedAssemblyView>
            {
                PlatformView("BetterUnturnedExperience", PlatformSelfPath, true),
                PlatformView("Assembly-CSharp", "E:\\Steam\\steamapps\\common\\Unturned\\Unturned_Data\\Managed\\Assembly-CSharp.dll", false),
                PlatformView("0Harmony", "E:\\Steam\\steamapps\\common\\Unturned\\BepInEx\\core\\0Harmony.dll", false)
            };
            var report = BuePlatformDoubleInstallCheck.Check(clean, BuePlatformDoubleInstallCheck.BueAssemblySimpleName, PlatformSelfPath);
            check(report != null, "无冲突：Check 返回显式报告（永非 null）");
            check(!report.HasConflict, "无冲突：干净清单无冲突");
            check(report.ConflictLocations.Count == 0, "无冲突：冲突路径列表为空");

            var emitted = new List<string>();
            var previousRecorder = BueRuntimeLog.Recorder;
            BueRuntimeLog.Recorder = line => emitted.Add(line);
            var savedSource = BuePlatformDoubleInstallCheck.LoadedAssembliesSource;
            try
            {
                BuePlatformDoubleInstallCheck.LoadedAssembliesSource = () => clean;
                var runReport = BuePlatformDoubleInstallCheck.Run();
                check(!runReport.HasConflict, "无冲突：注入干净清单的 Run 无冲突");
                check(emitted.Count == 0, "无冲突：干净清单零日志发射");
            }
            finally
            {
                BuePlatformDoubleInstallCheck.LoadedAssembliesSource = savedSource;
                BueRuntimeLog.Recorder = previousRecorder;
            }

            // Production wiring: the real AppDomain mapper sees the running BUE
            // assembly and marks it as self; the real-domain Run is clean.
            var realViews = BuePlatformDoubleInstallCheck.DefaultAppDomainSource();
            check(realViews.Count > 0, "无冲突：真实 AppDomain 映射非空");
            BueLoadedAssemblyView selfView = null;
            foreach (var view in realViews)
            {
                if (view.IsSelf) selfView = view;
            }
            check(selfView != null, "无冲突：真实 AppDomain 含 BUE 自身条目");
            check(selfView != null && selfView.SimpleName == BuePlatformDoubleInstallCheck.BueAssemblySimpleName, "无冲突：自身条目程序集名匹配");
            var realReport = BuePlatformDoubleInstallCheck.Run();
            check(!realReport.HasConflict, "无冲突：真实测试域只有一份 BUE，无冲突");
        }

        // Group 2: same simple name at a foreign path — the exact case the
        // self-check exists for. Case-variant names are reported too: fusion
        // binds names case-insensitively, so a diagnostic must over-report.
        private static void PlatformGroupNameConflict(Action<bool, string> check)
        {
            var loaded = new List<BueLoadedAssemblyView>
            {
                PlatformView("BetterUnturnedExperience", PlatformSelfPath, true),
                PlatformView("BetterUnturnedExperience", PlatformCopyPath, false)
            };
            var report = BuePlatformDoubleInstallCheck.Check(loaded, BuePlatformDoubleInstallCheck.BueAssemblySimpleName, PlatformSelfPath);
            check(report.HasConflict, "同程序集名冲突：异路径同名副本检出");
            check(report.ConflictLocations.Count == 1, "同程序集名冲突：恰一冲突副本");
            check(report.ConflictLocations[0] == PlatformCopyPath, "同程序集名冲突：报告的是副本路径");
            check(report.AssemblyName == "BetterUnturnedExperience", "同程序集名冲突：报告程序集名");
            check(report.SelfPath == PlatformSelfPath, "同程序集名冲突：报告当前 BUE 路径");
            check(report.Suggestion != null && report.Suggestion.Contains("移除非官方副本"), "同程序集名冲突：含移除建议");

            var caseVariant = new List<BueLoadedAssemblyView>
            {
                PlatformView("BetterUnturnedExperience", PlatformSelfPath, true),
                PlatformView("betterunturnedexperience", PlatformCopyPath, false)
            };
            var caseReport = BuePlatformDoubleInstallCheck.Check(caseVariant, BuePlatformDoubleInstallCheck.BueAssemblySimpleName, PlatformSelfPath);
            check(caseReport.HasConflict, "同程序集名冲突：大小写变体按保守口径检出");
        }

        // Group 3: different simple names never match — including BUE's own
        // satellite artifacts (Contracts/Core) whose names merely share a prefix.
        private static void PlatformGroupDifferentName(Action<bool, string> check)
        {
            var loaded = new List<BueLoadedAssemblyView>
            {
                PlatformView("BetterUnturnedExperience", PlatformSelfPath, true),
                PlatformView("BetterUnturnedExperience.Contracts", PlatformCopyPath, false),
                PlatformView("BetterUnturnedExperience.Core", PlatformCopyPath, false),
                PlatformView("SomeInventoryExtension", PlatformCopyPath, false)
            };
            var report = BuePlatformDoubleInstallCheck.Check(loaded, BuePlatformDoubleInstallCheck.BueAssemblySimpleName, PlatformSelfPath);
            check(!report.HasConflict, "不同程序集名：非同名程序集不构成冲突");
            check(report.ConflictLocations.Count == 0, "不同程序集名：冲突列表为空");
        }

        // Group 4: null/empty locations (dynamic or inaccessible assemblies)
        // still surface as conflicts under the unknown-location token, never
        // throw, and collapse into one finding per unknown bucket.
        private static void PlatformGroupEmptyPath(Action<bool, string> check)
        {
            var loaded = new List<BueLoadedAssemblyView>
            {
                PlatformView("BetterUnturnedExperience", PlatformSelfPath, true),
                PlatformView("BetterUnturnedExperience", null, false),
                PlatformView("BetterUnturnedExperience", string.Empty, false)
            };
            var report = BuePlatformDoubleInstallCheck.Check(loaded, BuePlatformDoubleInstallCheck.BueAssemblySimpleName, PlatformSelfPath);
            check(report.HasConflict, "空路径：位置缺失的同名副本仍检出");
            check(report.ConflictLocations.Count == 1, "空路径：未知位置去重为一项");
            check(report.ConflictLocations[0] == BuePlatformDoubleInstallCheck.UnknownLocationToken, "空路径：使用未知位置占位符");

            // A missing self path must not suppress detection of a real copy.
            var missingSelf = new List<BueLoadedAssemblyView>
            {
                PlatformView("BetterUnturnedExperience", PlatformCopyPath, false)
            };
            var missingReport = BuePlatformDoubleInstallCheck.Check(missingSelf, BuePlatformDoubleInstallCheck.BueAssemblySimpleName, string.Empty);
            check(missingReport.HasConflict && missingReport.ConflictLocations[0] == PlatformCopyPath, "空路径：自身路径缺失不吞真实副本");
        }

        // Group 5: duplicated listings (the same copy enumerated repeatedly,
        // with and without path case variants) dedupe to a single finding.
        private static void PlatformGroupDuplicateEntries(Action<bool, string> check)
        {
            var emitted = new List<string>();
            var previousRecorder = BueRuntimeLog.Recorder;
            BueRuntimeLog.Recorder = line => emitted.Add(line);
            var savedSource = BuePlatformDoubleInstallCheck.LoadedAssembliesSource;
            try
            {
                BuePlatformDoubleInstallCheck.LoadedAssembliesSource = () => new List<BueLoadedAssemblyView>
                {
                    PlatformView("BetterUnturnedExperience", PlatformSelfPath, true),
                    PlatformView("BetterUnturnedExperience", PlatformCopyPath, false),
                    PlatformView("BetterUnturnedExperience", PlatformCopyPath, false),
                    PlatformView("BetterUnturnedExperience", PlatformCopyPath.ToLowerInvariant(), false)
                };
                var report = BuePlatformDoubleInstallCheck.Run();
                check(report.HasConflict, "重复条目：重复同名副本检出");
                check(report.ConflictLocations.Count == 1, "重复条目：重复路径去重为一项");
                check(report.ConflictLocations[0] == PlatformCopyPath, "重复条目：保留首个观测到的路径写法");
                check(emitted.Count == 1, "重复条目：每冲突副本恰一条日志");
            }
            finally
            {
                BuePlatformDoubleInstallCheck.LoadedAssembliesSource = savedSource;
                BueRuntimeLog.Recorder = previousRecorder;
            }
        }

        // Group 6: the diagnostic contract — id, structured fields, Warning-level
        // emission (one line per conflict location), panel notice carries the id
        // and the removal suggestion, and a null assembly list is a developer
        // error (fail-fast) rather than a silently clean scan.
        private static void PlatformGroupDiagnosticFields(Action<bool, string> check)
        {
            check(BuePlatformDoubleInstallCheck.DiagnosticId == "BUE-PLATFORM-001", "诊断 id：平台自检诊断 id 为 BUE-PLATFORM-001");
            var loaded = new List<BueLoadedAssemblyView>
            {
                PlatformView("BetterUnturnedExperience", PlatformSelfPath, true),
                PlatformView("BetterUnturnedExperience", PlatformCopyPath, false)
            };
            var report = BuePlatformDoubleInstallCheck.Check(loaded, BuePlatformDoubleInstallCheck.BueAssemblySimpleName, PlatformSelfPath);
            check(report.DiagnosticId == "BUE-PLATFORM-001", "诊断 id：报告携带诊断 id");
            var logLine = report.BuildConflictLogLine(report.ConflictLocations[0]);
            check(logLine.Contains("diagnosticId=BUE-PLATFORM-001"), "关键字段：日志行含诊断 id");
            check(logLine.Contains("assembly=BetterUnturnedExperience"), "关键字段：日志行含检出的程序集名");
            check(logLine.Contains("conflictLocation=" + PlatformCopyPath), "关键字段：日志行含冲突副本路径");
            check(logLine.Contains("selfPath=" + PlatformSelfPath), "关键字段：日志行含当前 BUE 路径");
            check(logLine.Contains("suggestion=移除非官方副本"), "关键字段：日志行含移除非官方副本建议");
            check(report.NoticeLine.Contains("BUE-PLATFORM-001") && report.NoticeLine.Contains("移除非官方副本"), "关键字段：面板通知含诊断 id 与移除建议");

            var emitted = new List<string>();
            var previousRecorder = BueRuntimeLog.Recorder;
            BueRuntimeLog.Recorder = line => emitted.Add(line);
            var savedSource = BuePlatformDoubleInstallCheck.LoadedAssembliesSource;
            try
            {
                BuePlatformDoubleInstallCheck.LoadedAssembliesSource = () => new List<BueLoadedAssemblyView>
                {
                    PlatformView("BetterUnturnedExperience", PlatformSelfPath, true),
                    PlatformView("BetterUnturnedExperience", PlatformCopyPath, false),
                    PlatformView("BetterUnturnedExperience", "E:\\somewhere\\else\\BetterUnturnedExperience.dll", false)
                };
                BuePlatformDoubleInstallCheck.Run();
                check(emitted.Count == 2, "诊断发射：两个冲突副本恰两条日志");
                check(emitted[0].StartsWith("Warning ", StringComparison.Ordinal), "诊断发射：日志级别为 Warning");
                check(CountToken(emitted, "diagnosticId=BUE-PLATFORM-001") == 2, "诊断发射：每条日志都携带诊断 id");
            }
            finally
            {
                BuePlatformDoubleInstallCheck.LoadedAssembliesSource = savedSource;
                BueRuntimeLog.Recorder = previousRecorder;
            }

            var threw = false;
            try { BuePlatformDoubleInstallCheck.Check(null, BuePlatformDoubleInstallCheck.BueAssemblySimpleName, PlatformSelfPath); }
            catch (ArgumentNullException) { threw = true; }
            check(threw, "参数契约：null 程序集清单 fail-fast");
        }

        // Panel visibility (ticket acceptance "诊断在日志与面板可见") — separate
        // from the frozen six-case injection-seam anchor (F1 R1): rides the
        // always-run suite. The model exposes the notice (default empty,
        // survives Refresh, entries untouched) so the native panel can render
        // it next to the compatibility notice.
        private static void AssertPlatformPanelNotice()
        {
            var composition = new BueClientUiCompositionRoot();
            try
            {
                var model = composition.ManagementPanel.Model;
                Assert(string.IsNullOrEmpty(model.DoubleInstallNotice), "面板可见：默认无双装通知");
                var entriesBefore = model.GetEntries().Count;
                model.SetDoubleInstallNotice("检测到 BetterUnturnedExperience 冲突副本（BUE-PLATFORM-001）：请移除非官方副本后重启游戏，详见 BUE 日志。");
                Assert(model.DoubleInstallNotice.Contains("BUE-PLATFORM-001"), "面板可见：通知可写入并携带诊断 id");
                model.Refresh(new BueFeatureManagementEntry[0], new LoadedPluginDescriptor[0]);
                Assert(model.DoubleInstallNotice.Contains("BUE-PLATFORM-001"), "面板可见：通知在 Refresh 后保留");
                Assert(model.GetEntries().Count == entriesBefore, "面板可见：通知不改变条目集合");
                model.SetDoubleInstallNotice(null);
                Assert(string.IsNullOrEmpty(model.DoubleInstallNotice), "面板可见：null 通知归位为空");
            }
            finally
            {
                composition.Destroy();
            }
        }

    }
}
