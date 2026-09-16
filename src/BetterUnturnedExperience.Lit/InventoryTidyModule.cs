using System;
using System.Collections.Generic;
using System.Threading;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using HarmonyLib;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>Explicit result of a local tidy request — the UI never guesses.</summary>
    internal enum LitTidyRequestResult : byte
    {
        Dispatched = 0,
        NativeFallback = 1,
        RejectedFaultCircuit = 2,
        RejectedQueueClosed = 3,
        // DEV-V2-21: the multiplayer path's explicit refusals — the client
        // role never falls back to a local (unauthoritative) tidy.
        RejectedNoSession = 4,
        RejectedSendFailed = 5,
        // DEV-V4-06: the saved mode/direction snapshot could not be read or
        // carried a value outside the frozen choice literals — the click is
        // refused honestly instead of inventing a combination that was never
        // saved (V4-T5 Q59).
        RejectedPreferenceUnavailable = 6,
        // DEV-V5-03: the container click refused at the capability projection
        // (closed / no access / unsupported kind / grid gone) — the player
        // already got the structured Chinese reason through the feedback
        // channel; this result is the machine-side counterpart (never a
        // silent no-op).
        RejectedContainerUnavailable = 7,
    }

    /// <summary>
    /// DEV-V2-15: the inventory-tidy official feature module. Owns the
    /// single-player tidy path:
    ///   - DEV-V4-06: the legacy enabled master switch is RETIRED (the V4
    ///     migration made the lifecycle machine the only switch) — the module
    ///     schema is the global mode/direction choice pair, the tidy UI
    ///     availability is decided by lifecycle FACTS
    ///     (IFeatureLifetime.CurrentStatus, never a patch-private bool), and
    ///     the title-bar click reads the SAVED ClientPreference snapshot
    ///     (one GetSnapshot read serves both values — the panel draft, the
    ///     old per-page memory dictionaries and the previous click cache are
    ///     never consulted);
    ///   - the tidy plan is produced through the module's ITidyStrategy
    ///     (DEV-V5-02: the ONE built-in adapter is TaggedRowBandV1Strategy —
    ///     the unified tagged row-band layout; the three legacy modes are
    ///     retired to save-compatibility values and decide nothing);
    ///   - the UI patch set lives under the Harmony id = FeatureId (install
    ///     at start, UnpatchSelf at stop — spec「Harmony ID 收编」); U3DS
    ///     headless never arms it (T1 Q17, a decision gate — not exceptions);
    ///   - Stop maps the old three-phase unload: quiesce (refuse new
    ///     requests) → dispatcher shutdown (drain + cancel queued work) →
    ///     full teardown (unpatch + injected-button removal + clear state).
    /// </summary>
    internal sealed class InventoryTidyModule : IFeatureModule
    {
        /// <summary>DEV-V4-06: the tidy-title-bar action the CURRENT lifecycle
        /// fact dictates (V4-T5 Q55 nine-state table). Inject = only Running;
        /// RemoveExisting = Disabled/Stopped/Isolated/Incompatible tear the
        /// already-injected buttons down; KeepWithoutNew = transitions keep
        /// whatever exists but serve nothing new and refuse clicks.</summary>
        internal enum TidyUiLifecycleAction : byte
        {
            Inject = 0,
            RemoveExisting = 1,
            KeepWithoutNew = 2,
        }

        // DEV-V4-06: the global mode/direction ClientPreference choices (V4-T5
        // Q56). DEV-V5-02 (V5-T3): the 同类/空间/大件 mode row is RETIRED from
        // the schema and the panel — the unified tagged row-band layout is the
        // one official method, so no saved档 decides the algorithm any more.
        // The legacy key stays name-visible here for the save-compatibility
        // contract: an old store entry loads harmlessly, is ignored, and
        // disappears on the next persisted write (first-read normalization).
        // Direction survives as a stable-finish preference only. The Chinese
        // literals remain BOTH machine and display values this phase; a later
        // i18n ticket must split machine/display explicitly, never reuse these
        // as a stable protocol.
        internal const string ModeSettingId = "inventorytidy.mode"; // legacy read-compat key (no descriptor, no row)
        internal const string DirectionSettingId = "inventorytidy.direction";

        private Harmony harmony;

        // DEV-V3-06: the module no longer constructs or holds a
        // SettingsRuntime — the host composes the ONE per-feature runtime
        // from the registration's settings facet and injects the scoped view
        // (IFeatureBootstrap.Settings). The feature still owns its schema
        // (CreateSettingsDescriptors below), its authority semantics and the
        // runtime read; the persistence file layout is unchanged.
        internal InventoryTidyModule()
            : this(new FeatureId(LitRuntime.FeatureIdValue))
        {
        }

        internal InventoryTidyModule(FeatureId feature)
        {
            Feature = feature;
            Strategy = new TaggedRowBandV1Strategy();
            FaultGate = new LocalTidyFaultGate();
        }

        /// <summary>The settings facet schema this feature declares (V3-T7:
        /// 功能拥有 Schema; DEV-V4-06: the two global choices replaced the
        /// retired enabled toggle; DEV-V5-02: the mode row retires with the
        /// three档 — ONE descriptor remains, 整理方向, whose values are the
        /// stable-finish preference the unified layout consumes). The host
        /// composes the runtime from this list. The display name keeps the Q56
        /// frozen literal (整理方向); the description is the DEV-V5-02 frozen
        /// sentence replacing the Q61 pair (the档 retired with it). Values
        /// only arrive through the Cycle seam, which is AllowedValues-
        /// restricted anyway; MaximumUtf8Bytes covers the frozen Chinese
        /// literals (2 chars = 6 UTF-8 bytes each) with headroom.</summary>
        internal static IReadOnlyList<SettingDescriptor> CreateSettingsDescriptors(FeatureId feature)
        {
            return new[]
            {
                new SettingDescriptor(
                    feature, DirectionSettingId, DirectionDisplayName, DirectionDescription,
                    SettingKind.Choice, SettingAuthority.ClientLocal, SettingValue.Choice(DirectionDescendingLabel),
                    default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                    new[] { SettingValue.Choice(DirectionDescendingLabel), SettingValue.Choice(DirectionAscendingLabel) },
                    16, null, 1, 0, null, null),
            };
        }

        // Q56 frozen literals survive for direction: 降序/升序 are BOTH the
        // machine values and the display values (stable-finish preference,
        // DEV-V5-02). The retired mode档 literals (同类/空间/大件) no longer
        // appear anywhere in the schema or the panel.
        internal const string DirectionDescendingLabel = "降序";
        internal const string DirectionAscendingLabel = "升序";
        internal const string DirectionDisplayName = "整理方向";
        // DEV-V5-02 (V5-T3) frozen description — replaces the Q61 pair that
        // retired with the three档. Still explains the PLAYER-VISIBLE effect
        // only (V4-T6 Q61 discipline), never the internal algorithm: the
        // direction is now a stable-finish preference, and the tagged
        // row-band layout is the one method.
        internal const string DirectionDescription =
            "降序/升序只影响完全相同条件物品的收尾摆放顺序，不改变统一分段排版的结果。";

        /// <summary>DEV-V4-06: the nine-state → tidy-UI action table (V4-T5
        /// Q55). Pure so the host tests pin every state explicitly; the module
        /// feeds it the LIFECYCLE FACT (IFeatureLifetime.CurrentStatus.State),
        /// never its own booleans.</summary>
        internal static TidyUiLifecycleAction DecideTidyUiAction(FeatureState state)
        {
            switch (state)
            {
                case FeatureState.Running:
                    return TidyUiLifecycleAction.Inject;
                case FeatureState.Disabled:
                case FeatureState.Stopped:
                case FeatureState.Isolated:
                case FeatureState.Incompatible:
                    return TidyUiLifecycleAction.RemoveExisting;
                default:
                    // Starting/Stopping/Isolating/Discovered: keep what exists,
                    // add nothing, serve nothing (clicks fall back natively).
                    return TidyUiLifecycleAction.KeepWithoutNew;
            }
        }

        /// <summary>The lifecycle fact view this generation answers from; null
        /// only on hand-composed stage-baseline bootstraps — then there is NO
        /// lifecycle fact and the UI availability is fail-closed.</summary>
        internal TidyUiLifecycleAction CurrentTidyUiAction
        {
            get
            {
                var lifetime = Lifetime;
                if (lifetime == null) return TidyUiLifecycleAction.KeepWithoutNew;
                return DecideTidyUiAction(lifetime.CurrentStatus.State);
            }
        }

        /// <summary>DEV-V4-06: whether a NEWLY CONSTRUCTED inventory page may
        /// receive the tidy button. Only the Running fact injects.</summary>
        internal bool ShouldInjectTidyButtonForNewPage
        {
            get { return CurrentTidyUiAction == TidyUiLifecycleAction.Inject; }
        }

        internal FeatureId Feature { get; }
        // DEV-V3-06: the host-injected scoped settings view (the official
        // first consumption of the Settings matrix row). DEV-V4-06: it is
        // ONLY the saved store the tidy click snapshots (Q59) — null on
        // hand-composed stage-baseline bootstraps, and then the click
        // refuses (RejectedPreferenceUnavailable); no default is ever
        // invented without a saved snapshot.
        internal IScopedFeatureSettings SettingsView { get; private set; }
        internal LocalTidyFaultGate FaultGate { get; }

        /// <summary>The strategy the module plans with; replacement is a developer seam, null is a developer error.</summary>
        internal ITidyStrategy Strategy { get; set; }

        internal bool PatchesInstalled { get; private set; }
        internal bool Started { get; private set; }
        internal bool ShuttingDown { get; private set; }
        internal string StartGateDiagnostics { get; private set; } = string.Empty;

        /// <summary>DEV-V5-04: the insert-recovery patch trio (scope openers +
        /// the tryAddItemAuto behavior point) is its OWN registration — an
        /// AUTHORITY patch set, armed on U3DS too (story 25: 权威行为主机必须
        /// 真做, the Headless decision gate covers only 画面类). Kept separate
        /// from PatchesInstalled so a headless/UI-failure generation still
        /// proves the recovery surface state honestly.</summary>
        internal bool RecoverPatchesInstalled { get; private set; }
        internal string RecoverStartGateDiagnostics { get; private set; } = string.Empty;

        /// <summary>DEV-V5-05: the fast-transfer patch pair (intent opener +
        /// send witness) is its OWN registration beside 04's trio — an
        /// AUTHORITY-side surface armed on U3DS too (the recovery EXECUTES
        /// where the request arrives; on real U3DS the UI never runs, so the
        /// intent window simply never opens there — story 25).</summary>
        internal bool FastTransferPatchesInstalled { get; private set; }
        internal string FastTransferStartGateDiagnostics { get; private set; } = string.Empty;

        /// <summary>Host-test seam (the installer/observer family): replaces
        /// the real Harmony processor per patch type (true = installed). Null
        /// = production patching. Pins install order, the all-or-none rollback,
        /// and the U3DS arming without JITting engine methods in the test host.</summary>
        internal static Func<Type, bool> RecoverPatchInstallerForTests;

        /// <summary>DEV-V5-05 seam for the fast-transfer pair (same family).</summary>
        internal static Func<Type, bool> FastTransferPatchInstallerForTests;

        /// <summary>Last completed transaction outcome, observable for the future TidyCompleted publisher (DEV-V2-19/21).</summary>
        internal TidyOperationOutcome LastLocalOutcome { get; set; }

        // DEV-V2-21: the host-composed seams captured at IFeatureModule.Start.
        // OwnedEvents is the ONLY publish route for TidyCompleted (owned
        // publisher identity); Events is the feature's subscriber view; the
        // network API is the stable never-null facade from the bootstrap.
        internal IOwnedFeatureEventPublisher OwnedEvents { get; private set; }
        internal IFeatureEventSubscriber Events { get; private set; }
        internal IBueNetworkApi Network { get; private set; }
        internal ulong LifecycleGeneration { get; private set; }
        // DEV-V3-03: the module's own lifetime view from the bootstrap — the
        // read-only status query seam (IFeatureLifetime.CurrentStatus) and the
        // resource registry the module tracked its real network handles into.
        internal IFeatureLifetime Lifetime { get; private set; }
        // DEV-V3-07: the platform logger view captured at Start (nullable
        // stage-baseline seam, the LIR MainThread precedent) — the module's
        // start/stop diagnostics ride this seam into the unified structured
        // line + bounded summary (official-first consumption of the matrix
        // row; hand-composed test bootstraps may pass null, then the module
        // keeps its private LitRuntime channels unchanged).
        internal IFeatureLogger Logger { get; private set; }
        private long nextTransactionId;

        // DEV-V2-21: the multiplayer service is one per module generation —
        // created at Start (the network seam only exists on the bootstrap),
        // quiesced at stop phase 1, fully torn down at phase 3.
        internal LitTidyNetService NetService { get; private set; }
        internal LitTidyFaultScopeBook FaultBook { get; private set; }

        /// <summary>
        /// R1-Spec GAP-3 fix: the multiplayer path's honest readiness —
        /// false when the service's channel registration/subscription failed
        /// (the rollback is complete and the local single-player path stays
        /// alive; the state is observable, never silently assumed).
        /// </summary>
        internal bool MultiplayerReady { get; private set; }

        /// <summary>
        /// Host-test seam: when set, Start composes the network service
        /// through it instead of the production authority — the full
        /// protocol chain runs on the loopback transport with a fake
        /// authority and zero engine work. Null = production composition.
        /// </summary>
        internal Func<InventoryTidyModule, IBueNetworkApi, LitTidyFaultScopeBook, LitTidyNetService> NetServiceFactoryForTests;

        /// <summary>Host-test seam: overrides the fault persistence root directory.</summary>
        internal string ScopeDirectoryForTests;

        /// <summary>Host-test seam: overrides the fault scope context provider (map/slot).</summary>
        internal Func<LitFaultScopeContext> FaultContextForTests;

        /// <summary>DEV-V5-03 host-test seam: replaces the engine container
        /// session observation (and, on the local server-role path, the live
        /// facts + mounted grid to execute on). Null = production probe reads.
        /// Same ForTests family as NetServiceFactoryForTests / ServerRoleProbe.</summary>
        internal Func<LitContainerClientView> ContainerProbeOverride;

        public FeatureStartResult Start(IFeatureBootstrap bootstrap)
        {
            if (bootstrap == null) throw new ArgumentNullException(nameof(bootstrap));
            if (bootstrap.OwnedEvents == null || bootstrap.Network == null)
                throw new ArgumentException("the host bootstrap must compose the owned event publisher and the network API (never null)", nameof(bootstrap));
            // DEV-V4-09 F1b（与 Lir/Lht 同构放置）：Start 开头即重绑生产日志缝——
            // Stop 阶段 3 的解绑是插件卸载卫生语义，本代际从第一行日志起就必须
            // 可见（InstallPatches 的武装诊断不得走已解绑的 sink）。
            BindProductionLog();
            // DEV-V4-06: a Start IS a new module generation (the machine lands
            // it after Starting). The previous generation's stop boundary must
            // not leak in — otherwise the SAME wired instance could never
            // serve again after the panel's disable→enable round trip.
            ShuttingDown = false;
            OwnedEvents = bootstrap.OwnedEvents;
            Events = bootstrap.Events;
            Network = bootstrap.Network;
            LifecycleGeneration = bootstrap.LifecycleGeneration;
            Logger = bootstrap.Logger; // DEV-V3-07: nullable stage-baseline seam (see the property)
            // DEV-V3-06 official first consumption: the scoped settings view
            // rides the bootstrap (DEV-V4-06: the mode/direction snapshot the
            // tidy click reads comes through it) and binds BEFORE the patches
            // arm.
            AttachSettingsView(bootstrap.Settings); // DEV-V3-06: bind BEFORE arming
            EnsureStarted();
            // DEV-V2-21: the fault scope book binds the feature-private disk
            // persistence (JSON key structure unchanged); the production
            // context provider resolves map/slot at scope time.
            FaultBook = new LitTidyFaultScopeBook(ScopeDirectoryForTests ?? DefaultScopeDirectory(), FaultContextForTests ?? LitTidyProductionAuthority.ResolveFaultScopeContext);
            NetService = NetServiceFactoryForTests != null
                ? NetServiceFactoryForTests(this, bootstrap.Network, FaultBook)
                : new LitTidyNetService(this, bootstrap.Network, new LitTidyProductionAuthority(this), LitTidyProductionAuthority.IsServerRole, FaultBook);
            MultiplayerReady = NetService.Start();
            if (!MultiplayerReady)
            {
                // R7-Spec rebuttal: Start describes the MODULE (patches,
                // dispatcher and the local authoritative path are live), so
                // it stays Started=true — returning false here would drop
                // the module from the host's tracked set and SKIP its stop
                // handoff (UnsubscribeAll), a worse lifecycle violation.
                // The failed MULTIPLAYER SUBSYSTEM rolled back completely and
                // is honestly surfaced via MultiplayerReady + this record.
                LitRuntime.LogError("[Tidy] 联机整理服务启动失败（频道注册或双方向订阅被拒，已完整回滚）——本地单人路径保持可用");
                StartGateDiagnostics = StartGateDiagnostics.Length == 0
                    ? "multiplayer-start-rolled-back"
                    : StartGateDiagnostics + ";multiplayer-start-rolled-back";
            }
            // DEV-V3-03: the official first consumption of the lifecycle
            // resource seam — the module tracks its real inbound network
            // subscription handles through the host's IFeatureLifetime so the
            // stop boundary's reverse disposal covers them (handle Dispose is
            // idempotent against the module's own teardown), and keeps the
            // view for read-only status queries. The member is a wired
            // availability-matrix row on the host start path; hand-composed
            // module-level test bootstraps may still pass null, in which case
            // there is simply no registry to deliver to.
            if (bootstrap.Lifetime != null)
            {
                Lifetime = bootstrap.Lifetime;
                var handles = NetService.SubscriptionHandles;
                for (var i = 0; i < handles.Count; i++) bootstrap.Lifetime.TryTrack(handles[i]);
                // DEV-V4-06: the tidy-title-bar teardown rides the SAME
                // resource seam — the machine's Withdraw (isolation AND
                // complete-stop) disposes this handle, which removes the
                // buttons already injected into open pages (Q55: 隔离后移除).
                // Tracking is unconditional: Dispose is idempotent and the
                // removal pass is a no-op when nothing is tracked.
                bootstrap.Lifetime.TryTrack(new InventoryTidyUiPatch.UiTeardownHandle());
            }
            // DEV-V3-07 official-first consumption of the Logger matrix row:
            // the structured start diagnostic rides the INJECTED view (the
            // ticket red line「不能只保留私有路径而宣称已消费」— the player-
            // facing Chinese line above stays on the human channel, the
            // machine-readable line only exists because the view is wired).
            if (Logger != null) Logger.Info("tidy-module-started", "BUE-LIT-START");
            // DEV-V4-09 F1 实机二轮：Start 期**不**做存活仪表盘补注入——Start 运行
            // 在机器 Starting 态（九态门=过渡不新增，必然自我拒绝；实机 194456 包
            // gen8 启动后零注入行）。注入归 Tick 泵（见 Tick 注释）。
            LitRuntime.LogInfo("[Tidy] 模块已启动（宿主 bootstrap：功能代际=" + bootstrap.LifecycleGeneration + "）");
            return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-LIT-START");
        }

        /// <summary>
        /// DEV-V2-21: the one TidyCompleted publish route. Terminal tidy
        /// outcomes map onto the frozen result enum (Committed→Succeeded,
        /// Rejected→Rejected, CriticalFailure/ConcurrentMutationAfterCommit→
        /// Failed); the connection generation is 0 on the local path and the
        /// established session id on the multiplayer path. The transaction
        /// id is feature-generation monotonic and never zero.
        /// </summary>
        internal void PublishTidyCompleted(byte firstPage, byte lastPage, TidyCommitResult result, ulong connectionGeneration, ulong transactionId)
        {
            var publisher = OwnedEvents;
            if (publisher == null || ShuttingDown) return;
            if (transactionId == 0UL) transactionId = NextTransactionId();
            TidyCompletionResult mapped;
            switch (result)
            {
                case TidyCommitResult.Committed: mapped = TidyCompletionResult.Succeeded; break;
                case TidyCommitResult.Rejected: mapped = TidyCompletionResult.Rejected; break;
                default: mapped = TidyCompletionResult.Failed; break;
            }
            publisher.TryPublish(TidyCompleted.EventId, new TidyCompleted(Feature, firstPage, lastPage, mapped, connectionGeneration, transactionId));
            // F-B1c: the tidy moves are out-of-band mutations — they leave the
            // vanilla listen-host dashboard projection stale. The feature that
            // invalidated the projection reconciles it on the same beat (any
            // outcome: a compensated failure has also moved items back). The
            // dispatcher is engine-gated and a no-op off the listen host.
            ClientUi.Internal.ListenHostProjectionReconciler.OnTidyPagesCommitted(firstPage, lastPage);
        }

        /// <summary>
        /// R5-Standards S2: the ONE page-range expansion — a single page
        /// publishes that page, the all-pages marker publishes the frozen
        /// tidyable range 2..6. Both the local executor and the multiplayer
        /// service publish through here.
        /// </summary>
        internal void PublishTidyCompletedForPage(byte page, TidyCommitResult result, ulong connectionGeneration, ulong transactionId)
        {
            var firstPage = page == LitRuntime.AllPages ? HotkeySnapshotUtil.TIDYABLE_PAGE_MIN : page;
            var lastPage = page == LitRuntime.AllPages ? HotkeySnapshotUtil.TIDYABLE_PAGE_MAX : page;
            PublishTidyCompleted(firstPage, lastPage, result, connectionGeneration, transactionId);
        }

        /// <summary>Feature-generation monotonic transaction identity (never zero).</summary>
        internal ulong NextTransactionId()
        {
            return (ulong)Interlocked.Increment(ref nextTransactionId);
        }

        public void Stop(FeatureStopReason reason)
        {
            if (ShuttingDown) return;
            ShuttingDown = true;
            Started = false;
            // DEV-V3-07: the stop diagnostic rides the injected view too —
            // the host withdraws the generation AFTER Stop returns (same
            // ordering as the settings boundary), so the line is writable
            // here and refused for the captured view afterwards.
            if (Logger != null) Logger.Info("tidy-module-stopped", "BUE-LIT-STOP");
            // DEV-V2-21: stop phase 1 also quiesces the network service —
            // frames are refused, but the dispatcher drain below can still
            // compensate queued requests with a terminal Rejected send.
            NetService?.BeginQuiesce();
            LitRuntime.LogInfo("[Tidy] 模块停止：阶段 1/3 静默（拒绝新整理请求）");

            // 阶段 2：dispatcher 关停 — drain 已排队任务并对每个执行 Cancel
            // 回调（本地路径无 ledger 补偿，Cancel 只记录诊断）。
            MainThreadDispatcher.Shutdown();
            LitRuntime.LogInfo("[Tidy] 模块停止：阶段 2/3 dispatcher 关停完成");

            // 阶段 3：完全关停 — 撤销自身补丁 + 拆除已注入按钮（移除对象/解绑
            // 回调/清引用，Q55：停用后已打开页面不留死按钮）+ 清空功能代际静态
            // 表 + 解绑日志缝（解绑放在收尾日志之后，阶段完成信息仍可见）。
            UninstallPatches();
            InventoryTidyUiPatch.RemoveInjectedButtons();
            // DEV-V5-03: the production container-tidy toast is bound by the UI
            // when it actually draws (never in Start, so host recorders are not
            // clobbered); teardown unbinds ONLY our own production sink.
            if (ReferenceEquals(LitContainerFeedback.ToastSink, LitContainerFeedback.ProductionSink))
                LitContainerFeedback.ToastSink = null;
            // DEV-V2-21: the network service fully tears down AFTER the
            // dispatcher drain — queued compensations already ran; the
            // channel unregisters and every memory table drops while the
            // disk persistence survives untouched.
            NetService?.Stop();
            NetService = null;
            FaultBook = null;
            // 阶段 3 收尾日志按拆除事实如实区分：全部清干净 vs 仍有失败页引用
            // 在册待重试（Q55 红线：不把停用伪装成拆除成功）。
            if (InventoryTidyUiPatch.HasTrackedButtons)
                LitRuntime.LogInfo("[Tidy] 模块停止：阶段 3/3 完全关停（补丁已撤；部分按钮引用因移除失败仍在册，待下次拆除重试 diagnosticId=BUE-LIT-TEARDOWN）");
            else
                LitRuntime.LogInfo("[Tidy] 模块停止：阶段 3/3 完全关停（补丁已撤、按钮引用已清）");
            LitRuntime.LogSink = null;
            LitRuntime.ErrorLogSink = null;
        }

        /// <summary>
        /// Idempotent start: caches the main-thread id for the transaction
        /// service and arms the UI patch for this module generation (the
        /// lifecycle machine is the only switch; a shutting-down generation
        /// and U3DS headless never arm). Production binds this at Awake
        /// (the host start path that drives IFeatureModule.Start belongs to
        /// a later ticket); Start forwards here so the module behaves
        /// correctly when that path exists.
        /// </summary>
        internal void EnsureStarted()
        {
            if (Started) return;
            LitRuntime.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            // Generation boundary: reopen the dispatcher queue this module
            // generation owns (a previous generation's Stop closed it).
            MainThreadDispatcher.EnsureOpen();
            Started = true;
            if (!ShuttingDown)
            {
                // DEV-V4-06: the lifecycle machine is the ONLY switch (the
                // legacy enabled master switch is retired) — arming follows
                // the module generation, U3DS headless never arms (T1 Q17).
                InstallPatches();
            }
        }

        internal LitTidyRequestResult RequestLocalTidy(byte page, TidyMode mode, bool sortDescending)
        {
            // DEV-V4-06: the generation gates only — the legacy enabled master
            // switch is retired; a stopped/shutting module refuses, a running
            // generation serves.
            if (!Started || ShuttingDown) return LitTidyRequestResult.NativeFallback;
            if (Strategy == null) throw new InvalidOperationException("InventoryTidyModule.Strategy must never be null (developer error)");
            if (!FaultGate.Allowed) return LitTidyRequestResult.RejectedFaultCircuit;

            var capturedPage = page;
            var capturedMode = mode;
            var capturedSort = sortDescending;
            bool enqueued = MainThreadDispatcher.TryEnqueue(new QueuedTidyRequest
            {
                Work = () => LocalTidyExecutor.Execute(this, capturedPage, capturedMode, capturedSort),
                Cancel = () => LitRuntime.LogInfo("[Tidy] 模块停止 drain：已入队的本地整理任务被取消（未执行，无副作用）"),
                Tag = "LocalTidy page=" + capturedPage,
            });
            return enqueued ? LitTidyRequestResult.Dispatched : LitTidyRequestResult.RejectedQueueClosed;
        }

        /// <summary>
        /// DEV-V4-06: the title-bar button's ONE entry (Q54/Q59). Availability
        /// is re-confirmed from the LIFECYCLE FACT before anything runs — a
        /// button merely having been drawn never entitles a click — then the
        /// SAVED ClientPreference snapshot is read (DEV-V5-02: one read serves
        /// the direction stable-finish preference; the retired mode row is not
        /// consulted and a legacy mode entry cannot block the click; never the
        /// panel draft, never per-page memory, never a previous-click cache)
        /// and the request rides the ordinary RequestTidy seam. Refusals are
        /// explicit results, never fake successes: transitions answer
        /// NativeFallback, an unreadable or unknown-valued DIRECTION answers
        /// RejectedPreferenceUnavailable (no combination that was never saved
        /// may be invented). The wire still carries a mode byte — the frozen
        /// placeholder value — so the private protocol frame is unchanged.
        /// </summary>
        internal LitTidyRequestResult RequestTidyFromUiClick(byte page, bool allPages)
        {
            if (!Started || ShuttingDown || CurrentTidyUiAction != TidyUiLifecycleAction.Inject)
                return LitTidyRequestResult.NativeFallback;
            TidyMode mode;
            bool sortDescending;
            if (!TryReadSavedTidyPreference(out mode, out sortDescending, out _))
                return LitTidyRequestResult.RejectedPreferenceUnavailable;
            return RequestTidy(allPages ? LitRuntime.AllPages : page, mode, sortDescending);
        }

        /// <summary>
        /// DEV-V4-06: the same-revision saved-preference read (Q59) — ONE
        /// GetSnapshot, the store revision captured atomically with the value,
        /// so a torn combination that never existed cannot be assembled.
        /// DEV-V5-02: the retired mode row is NO LONGER consulted (a legacy
        /// inventorytidy.mode entry in the same store is ignored — 旧档只做
        /// 存盘兼容，首次读取归一后不再决定算法); only the direction
        /// stable-finish preference is read. Unknown direction literals or a
        /// schema-missing entry still refuse the read honestly (the caller
        /// must not silently fall back to defaults: the saved truth is unknown
        /// then). The mode output survives as the frozen wire placeholder so
        /// the private protocol keeps its frame shape unchanged.
        /// </summary>
        internal bool TryReadSavedTidyPreference(out TidyMode mode, out bool sortDescending, out uint revision)
        {
            mode = TidyMode.SameType;
            sortDescending = true;
            revision = 0;
            var view = SettingsView;
            if (view == null) return false;
            var snapshot = view.GetSnapshot(SettingRevisionScope.ClientPreference);
            var directionText = string.Empty;
            var hasDirection = false;
            for (var i = 0; i < snapshot.Entries.Count; i++)
            {
                var entry = snapshot.Entries[i];
                if (entry.SettingId == DirectionSettingId) { directionText = entry.EffectiveValue.Text; hasDirection = true; }
            }
            if (!hasDirection) return false;
            if (!TryMapDirectionLabel(directionText, out sortDescending)) return false;
            revision = snapshot.Revision;
            return true;
        }

        internal static bool TryMapDirectionLabel(string label, out bool sortDescending)
        {
            switch (label)
            {
                case DirectionDescendingLabel: sortDescending = true; return true;
                case DirectionAscendingLabel: sortDescending = false; return true;
                default: sortDescending = true; return false;
            }
        }

        /// <summary>
        /// DEV-V2-21: the ONE tidy request entry the UI drives. The server
        /// role (single player, listen host, dedicated server) tidies
        /// LOCALLY — the authority path with generation 0 — and a true
        /// client sends the reliable network request; a client never falls
        /// back to a local unauthoritative tidy.
        /// </summary>
        internal LitTidyRequestResult RequestTidy(byte page, TidyMode mode, bool sortDescending)
        {
            if (!Started || ShuttingDown) return LitTidyRequestResult.NativeFallback;
            if (LitTidyProductionAuthority.IsServerRole()) return RequestLocalTidy(page, mode, sortDescending);
            var service = NetService;
            if (service == null || !service.Started) return LitTidyRequestResult.RejectedNoSession;
            return service.RequestTidy(page, mode, sortDescending);
        }

        /// <summary>
        /// DEV-V5-03: the visibility-side capability answer for the container
        /// title bar. The lifecycle fact is stamped HERE (never a patch bool),
        /// the title bar presence comes from the surface, and the rest of the
        /// projection is the pure capability module. The click path
        /// (RequestContainerTidyFromUiClick) runs the SAME projection against
        /// the live observation — a button having been drawn never entitles a
        /// click, and a refused click always answers in words.
        /// </summary>
        internal bool ContainerCapabilityAvailable(bool titleBarPresent, out LitContainerTidyReason reason)
        {
            string ignoredDiagnostic;
            return ContainerCapabilityAvailable(titleBarPresent, out reason, out ignoredDiagnostic);
        }

        /// <summary>DEV-V5-03 (Spec R2): the refusal ALSO carries the
        /// observation's free-text diagnostic (T4 Q1「留下可诊断原因」 — e.g.
        /// virtual-container:state-hooked), so the surface log and the click
        /// feedback can trace WHY the projection refused, instead of only the
        /// six generic Chinese sentences.</summary>
        internal bool ContainerCapabilityAvailable(bool titleBarPresent, out LitContainerTidyReason reason, out string diagnostic)
        {
            diagnostic = null;
            var view = ContainerProbeOverride != null ? ContainerProbeOverride() : LitContainerSessionProbe.ObserveClient();
            if (view == null) { reason = LitContainerTidyReason.None; return false; }
            var obs = view.Observation;
            obs.TitleBarPresent = titleBarPresent;
            obs.FeatureRunning = CurrentTidyUiAction == TidyUiLifecycleAction.Inject;
            var available = LitContainerCapability.TryProject(obs, out reason);
            if (!available) diagnostic = obs.UnsupportedDiagnostic;
            return available;
        }

        /// <summary>
        /// DEV-V5-03 (V5-T4): the container title-bar button's ONE entry, the
        /// mirror shape of the player-page click seam — availability is the
        /// LIFECYCLE FACT first (a drawn button never entitles a click), the
        /// saved direction is read with the same Q59 rule, and the actual
        /// decision is the capability projection over live session facts. The
        /// click never touches a grid itself: the server role enqueues the
        /// authoritative execution (kind+claim bound, live re-read inside the
        /// queue turn), a client role sends the session-addressed container
        /// request. Every refusal answers with the structured reason through
        /// the feedback channel — 「可点但静默失败」被逐条堵住.
        /// </summary>
        internal LitTidyRequestResult RequestContainerTidyFromUiClick()
        {
            if (!Started || ShuttingDown || CurrentTidyUiAction != TidyUiLifecycleAction.Inject)
                return LitTidyRequestResult.NativeFallback;
            if (Strategy == null) throw new InvalidOperationException("InventoryTidyModule.Strategy must never be null (developer error)");
            TidyMode mode;
            bool sortDescending;
            if (!TryReadSavedTidyPreference(out mode, out sortDescending, out _))
                return LitTidyRequestResult.RejectedPreferenceUnavailable;
            var view = ContainerProbeOverride != null ? ContainerProbeOverride() : LitContainerSessionProbe.ObserveClient();
            if (view == null)
                return LitTidyRequestResult.RejectedContainerUnavailable; // observer itself failed — honest absence
            var obs = view.Observation;
            obs.FeatureRunning = true; // past the Inject gate: the lifecycle fact speaks, never a patch bool
            if (!LitContainerCapability.TryProject(obs, out var reason))
            {
                if (reason != LitContainerTidyReason.None)
                    LitContainerFeedback.ShowReason(string.IsNullOrEmpty(obs.UnsupportedDiagnostic)
                        ? "点击被拒" : "点击被拒（诊断=" + obs.UnsupportedDiagnostic + "）", reason);
                return LitTidyRequestResult.RejectedContainerUnavailable;
            }
            var claimKind = (LitContainerTidyKind)(byte)obs.Kind;
            if (LitTidyProductionAuthority.IsServerRole())
            {
                if (!FaultGate.Allowed) return LitTidyRequestResult.RejectedFaultCircuit;
                var captured = new LitContainerTidyClaim { Kind = claimKind, Fingerprint = view.Fingerprint };
                var capturedSort = sortDescending;
                var fromOverride = view.Live.HasValue && view.ContainerItems != null;
                var overrideLive = fromOverride ? view.Live.Value : default(LitContainerLiveFacts);
                var overrideGrid = view.ContainerItems;
                bool enqueued = MainThreadDispatcher.TryEnqueue(new QueuedTidyRequest
                {
                    // The live facts are (re)read INSIDE the queue turn: a
                    // container closing between click and execution must land
                    // as ContainerClosed/ContentChanged with zero mutation,
                    // never as a tidy of a detached grid.
                    Work = () =>
                    {
                        LitContainerLiveFacts execLive;
                        SDG.Unturned.Items execGrid;
                        if (fromOverride) { execLive = overrideLive; execGrid = overrideGrid; }
                        else if (!LitContainerSessionProbe.TryReadServerLive(SDG.Unturned.Player.LocalPlayer, out execLive, out execGrid))
                        {
                            LitContainerFeedback.ShowReason("本地执行", LitContainerTidyReason.InternalFailure);
                            return;
                        }
                        ExecuteLocalContainerTidy(execLive, captured, execGrid, capturedSort);
                    },
                    Cancel = () => LitRuntime.LogInfo("[Tidy容器] 模块停止 drain：已入队的容器整理被取消（未执行，无副作用）"),
                    Tag = "LocalContainerTidy kind=" + captured.Kind,
                });
                return enqueued ? LitTidyRequestResult.Dispatched : LitTidyRequestResult.RejectedQueueClosed;
            }
            var containerService = NetService;
            if (containerService == null || !containerService.Started) return LitTidyRequestResult.RejectedNoSession;
            return containerService.RequestContainerTidy(claimKind, view.Fingerprint, sortDescending);
        }

        /// <summary>DEV-V5-03: the local authoritative container execution
        /// (server role — SP / listen host; U3DS answers remote requests
        /// through the same execution core on its authority path). A verified
        /// internal failure opens the fault gate exactly like the player
        /// pages: the same circuit, no parallel BUE tidy lock.</summary>
        private void ExecuteLocalContainerTidy(LitContainerLiveFacts live, LitContainerTidyClaim claim, SDG.Unturned.Items grid, bool sortDescending)
        {
            LitContainerTidyExecution.Result exec;
            try
            {
                exec = LitContainerTidyExecution.Commit(live, claim, grid, sortDescending, Strategy);
            }
            catch (Exception error)
            {
                LitRuntime.LogError("[Tidy容器] 本地容器整理执行崩溃（未改动物品）: " + error);
                FaultGate.Open("local container tidy crashed: " + error.Message, restoreVerified: false);
                LitContainerFeedback.ShowReason("本地容器整理", LitContainerTidyReason.InternalFailure);
                return;
            }
            switch (exec.Commit)
            {
                case TidyCommitResult.CriticalFailure:
                    FaultGate.Open(exec.Reason + " during local container tidy", restoreVerified: true);
                    LitContainerFeedback.ShowReason("本地容器整理", exec.Reason);
                    break;
                case TidyCommitResult.ConcurrentMutationAfterCommit:
                    FaultGate.Open("ConcurrentMutationAfterCommit in local container tidy", restoreVerified: true);
                    LitContainerFeedback.ShowReason("本地容器整理", exec.Reason);
                    break;
                case TidyCommitResult.Committed:
                    LitContainerFeedback.ShowNote("本地", "容器整理已完成。");
                    break;
                default:
                    LitContainerFeedback.ShowReason("本地容器整理", exec.Reason);
                    break;
            }
        }

        /// <summary>
        /// DEV-V5-05 (V5-T5): the fast-transfer recover entry, reached ONLY from
        /// the onSelectedItem Finalizer when vanilla's own verdict was 「no
        /// packet sent」. Gate order mirrors the button paths (Q59 saved
        /// preference — never the panel draft; fault circuit — the same gate;
        /// request shape — defense in depth, the engine probe and the wire
        /// codec both gate it too). The server role (SP / listen host) runs the
        /// SAME authoritative execution as a remote peer's admitted request —
        /// live facts are (re)read INSIDE the queue turn, so a container closed
        /// between click and execution lands as ContainerClosed with zero
        /// mutation; a true client only sends the session-addressed request and
        /// NEVER touches its own grids (「禁止只在本地改网格」 — the client's
        /// mirror updates through vanilla's own sync after the authority
        /// commits). Every refusal is silence + log: this is a recovery off a
        /// right-click, not a clickable control (V5-T5 Q1 原版失败语义).
        /// </summary>
        internal LitTidyRequestResult RequestFastTransferRecoverFromIntent(FastTransferIntent intent)
        {
            if (!Started || ShuttingDown) return LitTidyRequestResult.NativeFallback;
            if (intent == null) return LitTidyRequestResult.RejectedContainerUnavailable;
            if (Strategy == null) throw new InvalidOperationException("InventoryTidyModule.Strategy must never be null (developer error)");
            if (!FaultGate.Allowed) return LitTidyRequestResult.RejectedFaultCircuit;
            TidyMode mode;
            bool sortDescending;
            if (!TryReadSavedTidyPreference(out mode, out sortDescending, out _))
                return LitTidyRequestResult.RejectedPreferenceUnavailable;
            var pageOk = (intent.SourcePage >= HotkeySnapshotUtil.TIDYABLE_PAGE_MIN && intent.SourcePage <= HotkeySnapshotUtil.TIDYABLE_PAGE_MAX)
                || intent.SourcePage == LitContainerTidyExecution.MOUNT_PAGE;
            if (!pageOk || LitContainerSessionAdapters.Resolve(intent.Kind) == null)
                return LitTidyRequestResult.RejectedContainerUnavailable;
            if (LitTidyProductionAuthority.IsServerRole())
            {
                var captured = intent;
                var capturedSort = sortDescending;
                var view = ContainerProbeOverride != null ? ContainerProbeOverride() : null;
                var fromOverride = view != null && view.Live.HasValue && view.ContainerItems != null;
                var overrideLive = fromOverride ? view.Live.Value : default(LitContainerLiveFacts);
                var overrideGrid = view == null ? null : view.ContainerItems;
                bool enqueued = MainThreadDispatcher.TryEnqueue(new QueuedTidyRequest
                {
                    Work = () =>
                    {
                        LitContainerLiveFacts execLive;
                        SDG.Unturned.Items execGrid;
                        var localPlayer = SDG.Unturned.Player.LocalPlayer;
                        if (fromOverride) { execLive = overrideLive; execGrid = overrideGrid; }
                        else if (!LitContainerSessionProbe.TryReadServerLive(localPlayer, out execLive, out execGrid))
                        {
                            LitRuntime.LogWarning("[快速转移恢复] 本地现读失败：保持原版，零修改。");
                            return;
                        }
                        var claim = new LitContainerTidyClaim { Kind = captured.Kind, Fingerprint = captured.Fingerprint };
                        var attempt = FastTransferRecoverAdapter.TryRecover(
                            localPlayer == null ? null : localPlayer.inventory,
                            FastTransferRecoverEngine.ReadPages(localPlayer),
                            execGrid, execLive, claim,
                            captured.SourcePage, captured.SourceX, captured.SourceY, capturedSort);
                        if (!attempt.Recovered)
                            LitRuntime.LogInfo("[快速转移恢复] 本地恢复未成立（" + attempt.Refusal + "）：保持原版失败。");
                    },
                    Cancel = () => LitRuntime.LogInfo("[快速转移恢复] 模块停止 drain：已入队的恢复被取消（未执行，无副作用）"),
                    Tag = "LocalFastTransferRecover kind=" + captured.Kind,
                });
                return enqueued ? LitTidyRequestResult.Dispatched : LitTidyRequestResult.RejectedQueueClosed;
            }
            var service = NetService;
            if (service == null || !service.Started) return LitTidyRequestResult.RejectedNoSession;
            return service.RequestFastTransferRecover(intent.Kind, intent.Fingerprint,
                intent.SourcePage, intent.SourceX, intent.SourceY, sortDescending);
        }

        /// <summary>Tick 节拍计数（实时注入节流；uint 回绕无害）。</summary>
        private uint injectTickCounter;

        /// <summary>实时注入节流窗：16 拍（60fps 下 ~0.27s；首拍即试，按位与取模）。</summary>
        private const uint InjectTickPeriodMask = 0xF;

        /// <summary>Main-thread pump for the dispatcher queue (driven by the plugin update tick).</summary>
        internal void Tick()
        {
            MainThreadDispatcher.ProcessAll();
            // DEV-V2-21: session discovery rides the same pump — an
            // established session found here receives its challenge/scope
            // even when the module started after the handshake.
            NetService?.Tick();
            // DEV-V4-09 F1（实机二轮，用户裁定「实时注入」）：重启用落地 Running
            // 后按钮由本泵复装——PlayerDashboardInventoryUI ctor 一次会话只跑一次，
            // 等构造事件=等不到；Start 期又必然处于 Starting 态被九态门拒绝。门禁
            // 不变（仅 Running+在册去重+未武装不画死按钮，全在
            // TryInjectIntoAliveDashboard）；成功后在册非空即短路，节流 16 拍。
            if ((injectTickCounter++ & InjectTickPeriodMask) == 0)
                InventoryTidyUiPatch.TryInjectIntoAliveDashboard(this);
        }

        /// <summary>The feature-private fault persistence root (BUE/inventory-tidy/fault_scopes).</summary>
        internal static string DefaultScopeDirectory()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BetterUnturnedExperience", "inventory-tidy", "fault_scopes");
        }

        internal void BindProductionLog()
        {
            LitRuntime.LogSink = line => BetterUnturnedExperience.Plugin.BueRuntimeLog.Runtime(line);
            LitRuntime.ErrorLogSink = line => BetterUnturnedExperience.Plugin.BueRuntimeLog.Error(line);
        }

        private void InstallPatches()
        {
            // DEV-V5-04: the recovery trio is an AUTHORITY patch set — it arms
            // on every surface including U3DS (V5-T1 Headless 裁决只砍画面),
            // BEFORE and independent of the UI decision gate below.
            InstallRecoverPatches();
            // DEV-V5-05: the fast-transfer pair likewise arms on every surface
            // (its authoritative half = the admitted-request execution; the
            // intent window simply never opens headless — no dashboard UI).
            InstallFastTransferPatches();

            // DEV-V4-06: U3DS never arms the Glazier injection (T1 Q17) — a
            // DECISION gate recorded as an honest diagnostic, never an
            // exception pretending to be a gate.
            if (BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision)
            {
                StartGateDiagnostics = "headless-ui-not-armed";
                LitRuntime.LogInfo("[Tidy] U3DS headless：不武装整理按钮补丁（Headless 决策门禁）");
                return;
            }
            if (PatchesInstalled) return;
            try
            {
                if (harmony == null) harmony = new Harmony(LitRuntime.FeatureIdValue);
                harmony.CreateClassProcessor(typeof(InventoryTidyUiPatch)).Patch();
                InventoryTidyUiPatch.ActiveModule = this;
                PatchesInstalled = true;
                StartGateDiagnostics = string.Empty;
                LitRuntime.LogInfo("[Tidy] 整理按钮补丁已安装（Harmony ID=" + LitRuntime.FeatureIdValue + "）");
            }
            catch (Exception e)
            {
                // 环境闸（宿主测试进程无法装真机补丁 / 游戏版本漂移）：记录
                // 结构化诊断而非静默失败——按钮不可用是可观察状态。
                StartGateDiagnostics = "patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                LitRuntime.LogError("[Tidy] 整理按钮补丁安装失败（整理功能不可用）: " + e.Message);
                // 半装回滚：处理器可能已挂上部分补丁，失败即整体撤销自身。
                // DEV-V5-04：UnpatchSelf 按 Harmony ID 全撤，恢复三件套同被摘除
                // ——两面的在册状态必须同步归零（不留「补丁没了、登记还在」）。
                // DEV-V5-05：快速转移两面同闸同理（三面在册一起归零）。
                try { if (harmony != null) harmony.UnpatchSelf(); } catch (Exception) { }
                InventoryTidyUiPatch.ActiveModule = null;
                RecoverPatchesInstalled = false;
                InsertRecoverAdapter.ActiveModule = null;
                FastTransferPatchesInstalled = false;
                FastTransferRecoverAdapter.ActiveModule = null;
            }
        }

        /// <summary>DEV-V5-04: register the insert-recovery trio (pickup scope
        /// opener → craft scope opener → tryAddItemAuto behavior point) and
        /// hand the adapter this generation. The surface list and the exact
        /// method binding live in InsertRecoverBinder (single source,
        /// host-verifiable — see the R2 binding note there). All-or-none: if
        /// any point fails, everything under this Harmony id is rolled back
        /// and NO half trigger surface stays live (the environment-gate
        /// diagnostic rides RecoverStartGateDiagnostics, separate from the UI
        /// 口径).</summary>
        private void InstallRecoverPatches()
        {
            if (RecoverPatchesInstalled) return;
            var types = InsertRecoverBinder.PatchSurface;
            try
            {
                if (harmony == null) harmony = new Harmony(LitRuntime.FeatureIdValue);
                for (var i = 0; i < types.Count; i++)
                {
                    var seam = RecoverPatchInstallerForTests;
                    if (seam != null)
                    {
                        if (!seam(types[i])) throw new InvalidOperationException("recover patch refused: " + types[i].Name);
                        continue;
                    }
                    InsertRecoverBinder.Bind(harmony, types[i]);
                }
                InsertRecoverAdapter.ActiveModule = this;
                RecoverPatchesInstalled = true;
                RecoverStartGateDiagnostics = string.Empty;
                LitRuntime.LogInfo("[入包恢复] 恢复补丁已登记（Harmony ID=" + LitRuntime.FeatureIdValue + "）");
            }
            catch (Exception e)
            {
                RecoverStartGateDiagnostics = "recover-patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                LitRuntime.LogError("[入包恢复] 恢复补丁登记失败（入包保持原版，不半装）: " + e.Message);
                // 半装 = 整体撤销（含先装的按钮面尚未装——本方法在 UI 安装前运行）。
                try { if (harmony != null) harmony.UnpatchSelf(); } catch (Exception) { }
                InsertRecoverAdapter.ActiveModule = null;
                RecoverPatchesInstalled = false;
            }
        }

        /// <summary>DEV-V5-05: register the fast-transfer patch pair (intent
        /// opener on onSelectedItem → send witness on sendDragItem). All-or-none
        /// like 04: any failure revokes everything under this Harmony id and the
        /// registration, never a half surface; the diagnostic rides
        /// FastTransferStartGateDiagnostics.</summary>
        private void InstallFastTransferPatches()
        {
            if (FastTransferPatchesInstalled) return;
            var types = FastTransferRecoverBinder.PatchSurface;
            try
            {
                if (harmony == null) harmony = new Harmony(LitRuntime.FeatureIdValue);
                for (var i = 0; i < types.Count; i++)
                {
                    var seam = FastTransferPatchInstallerForTests;
                    if (seam != null)
                    {
                        if (!seam(types[i])) throw new InvalidOperationException("fast-transfer patch refused: " + types[i].Name);
                        continue;
                    }
                    FastTransferRecoverBinder.Bind(harmony, types[i]);
                }
                FastTransferRecoverAdapter.ActiveModule = this;
                FastTransferPatchesInstalled = true;
                FastTransferStartGateDiagnostics = string.Empty;
                LitRuntime.LogInfo("[快速转移恢复] 恢复补丁已登记（Harmony ID=" + LitRuntime.FeatureIdValue + "）");
            }
            catch (Exception e)
            {
                FastTransferStartGateDiagnostics = "fast-transfer-patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                LitRuntime.LogError("[快速转移恢复] 恢复补丁登记失败（快速转移保持原版，不半装）: " + e.Message);
                try { if (harmony != null) harmony.UnpatchSelf(); } catch (Exception) { }
                FastTransferRecoverAdapter.ActiveModule = null;
                FastTransferPatchesInstalled = false;
                // UnpatchSelf 同 ID 全撤：另一恢复面与 UI 的在册状态必须同步归零
                //（04 同款在册同步律）。
                RecoverPatchesInstalled = false;
                InsertRecoverAdapter.ActiveModule = null;
                PatchesInstalled = false;
                InventoryTidyUiPatch.ActiveModule = null;
            }
        }

        private void UninstallPatches()
        {
            if (!PatchesInstalled && !RecoverPatchesInstalled && !FastTransferPatchesInstalled) return;
            // DEV-V5-04: the unpatch itself is guarded — reversing applied IL
            // re-JITs the ORIGINAL method bodies, which in a host without the
            // Unity runtime throws (the Mono ECall rule). The stop boundary
            // must never throw into the lifecycle machine, and the bookkeeping
            // below has to land either way: the registration (ActiveModule /
            // installed flags) is the functional switch, and it is always
            // revoked — a stuck live IL hook with no registration answers as
            // plain refusal through the adapter gates, while a failed reversal
            // stays observable in the log.
            try
            {
                if (harmony != null) harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                LitRuntime.LogError("[Tidy] 补丁撤销异常（登记已强制收回，功能面按停用运行）: " + e.Message);
            }
            finally
            {
                PatchesInstalled = false;
                InventoryTidyUiPatch.ActiveModule = null;
                // DEV-V5-04: Stop/隔离 = 恢复面注销——补丁没了、登记也交还，
                // 「功能停路径不执行」由这条注销保证（无补丁内死开关）。
                RecoverPatchesInstalled = false;
                InsertRecoverAdapter.ActiveModule = null;
                // DEV-V5-05: 快速转移面同款注销（两恢复面同闸同代，交还一起交还）。
                FastTransferPatchesInstalled = false;
                FastTransferRecoverAdapter.ActiveModule = null;
                LitRuntime.LogInfo("[Tidy] 整理按钮补丁已撤销（原生回退）");
            }
        }

        /// <summary>DEV-V3-06: bind the host-injected scoped settings view
        /// (Start does this from the bootstrap; host-test fixtures attach
        /// directly). DEV-V4-06: binding only — the retired enabled toggle
        /// had its read here; the mode/direction snapshot is read at CLICK
        /// time (Q59), never cached on the module.</summary>
        internal void AttachSettingsView(IScopedFeatureSettings view)
        {
            SettingsView = view;
        }
    }
}
