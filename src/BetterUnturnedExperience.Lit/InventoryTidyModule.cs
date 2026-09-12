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
    ///     (built-in default-grid-v1 wrapping the migrated InventorySolver);
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
        // Q56). The Chinese literals are BOTH the machine values and the
        // display values this phase; a later i18n ticket must split
        // machine/display explicitly, never reuse these as a stable protocol.
        internal const string ModeSettingId = "inventorytidy.mode";
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
            Strategy = new DefaultGridV1Strategy();
            FaultGate = new LocalTidyFaultGate();
        }

        /// <summary>The settings facet schema this feature declares (V3-T7:
        /// 功能拥有 Schema; DEV-V4-06: the two global choices replace the
        /// retired enabled toggle) — the host composes the runtime from it.
        /// The display names are the Q56 frozen copy (整理模式/整理方向) and
        /// the description sentences are the Q61 frozen copy, landed by
        /// DEV-V4-07 (描述解释玩家可见效果). MaximumUtf8Bytes covers the
        /// frozen Chinese literals (2 chars = 6 UTF-8 bytes each) with
        /// headroom; values only arrive through the Cycle seam, which is
        /// AllowedValues-restricted anyway.</summary>
        internal static IReadOnlyList<SettingDescriptor> CreateSettingsDescriptors(FeatureId feature)
        {
            return new[]
            {
                new SettingDescriptor(
                    feature, ModeSettingId, ModeDisplayName, ModeDescription,
                    SettingKind.Choice, SettingAuthority.ClientLocal, SettingValue.Choice(ModeSameTypeLabel),
                    default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                    new[] { SettingValue.Choice(ModeSameTypeLabel), SettingValue.Choice(ModeMaxRectsLabel), SettingValue.Choice(ModeFfdLabel) },
                    16, null, 1, 0, null, null),
                new SettingDescriptor(
                    feature, DirectionSettingId, DirectionDisplayName, DirectionDescription,
                    SettingKind.Choice, SettingAuthority.ClientLocal, SettingValue.Choice(DirectionDescendingLabel),
                    default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                    new[] { SettingValue.Choice(DirectionDescendingLabel), SettingValue.Choice(DirectionAscendingLabel) },
                    16, null, 1, 1, null, null),
            };
        }

        // Q56 frozen literal↔solver mapping: 降序 = the existing 大件优先
        // direction; 升序 = the opposite. The labels are the machine values.
        internal const string ModeSameTypeLabel = "同类";
        internal const string ModeMaxRectsLabel = "空间";
        internal const string ModeFfdLabel = "大件";
        internal const string DirectionDescendingLabel = "降序";
        internal const string DirectionAscendingLabel = "升序";
        // Q56 frozen display names (the panel's row title) + DEV-V4-07 (Q61)
        // frozen description sentences — the description explains the PLAYER-
        // VISIBLE effect of each level, never the solver algorithm or
        // O-LIT-1 (V4-T6 Q61: 描述解释玩家可见效果，不承诺排序算法或 O-LIT-1；
        // 120 是显示截断，不是正文契约上限).
        internal const string ModeDisplayName = "整理模式";
        internal const string DirectionDisplayName = "整理方向";
        internal const string ModeDescription =
            "同类：把相同物品聚在一起；空间：优先保留大块空位；大件：优先放置大件。对当前栏整理和全身整理都生效。";
        internal const string DirectionDescription = "降序：大件优先；升序：小件优先。与整理模式共同决定整理顺序。";

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
        /// SAVED ClientPreference snapshot is read (one read serves both
        /// mode and direction; never the panel draft, never per-page memory,
        /// never a previous-click cache) and the request rides the ordinary
        /// RequestTidy seam. Refusals are explicit results, never fake
        /// successes: transitions answer NativeFallback, an unreadable or
        /// unknown-valued snapshot answers RejectedPreferenceUnavailable
        /// (no combination that was never saved may be invented).
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
        /// DEV-V4-06: the same-revision saved-preference read (Q59). The whole
        /// preference rides ONE GetSnapshot — the store revision is captured
        /// atomically with both values, so a torn mode/direction combination
        /// that never existed cannot be assembled. Unknown literals or a
        /// schema-missing entry refuse the read honestly (the caller must not
        /// silently fall back to defaults: the saved truth is unknown then).
        /// </summary>
        internal bool TryReadSavedTidyPreference(out TidyMode mode, out bool sortDescending, out uint revision)
        {
            mode = TidyMode.SameType;
            sortDescending = true;
            revision = 0;
            var view = SettingsView;
            if (view == null) return false;
            var snapshot = view.GetSnapshot(SettingRevisionScope.ClientPreference);
            var modeText = string.Empty;
            var directionText = string.Empty;
            var hasMode = false;
            var hasDirection = false;
            for (var i = 0; i < snapshot.Entries.Count; i++)
            {
                var entry = snapshot.Entries[i];
                if (entry.SettingId == ModeSettingId) { modeText = entry.EffectiveValue.Text; hasMode = true; }
                else if (entry.SettingId == DirectionSettingId) { directionText = entry.EffectiveValue.Text; hasDirection = true; }
            }
            if (!hasMode || !hasDirection) return false;
            if (!TryMapModeLabel(modeText, out mode)) return false;
            if (!TryMapDirectionLabel(directionText, out sortDescending)) return false;
            revision = snapshot.Revision;
            return true;
        }

        internal static bool TryMapModeLabel(string label, out TidyMode mode)
        {
            switch (label)
            {
                case ModeSameTypeLabel: mode = TidyMode.SameType; return true;
                case ModeMaxRectsLabel: mode = TidyMode.MaxRects; return true;
                case ModeFfdLabel: mode = TidyMode.FFD; return true;
                default: mode = TidyMode.SameType; return false;
            }
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
                try { if (harmony != null) harmony.UnpatchSelf(); } catch (Exception) { }
                InventoryTidyUiPatch.ActiveModule = null;
            }
        }

        private void UninstallPatches()
        {
            if (!PatchesInstalled) return;
            try
            {
                if (harmony != null) harmony.UnpatchSelf();
            }
            finally
            {
                PatchesInstalled = false;
                InventoryTidyUiPatch.ActiveModule = null;
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
