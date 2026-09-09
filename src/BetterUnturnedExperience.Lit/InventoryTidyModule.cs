using System;
using System.Threading;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Settings;
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
    }

    /// <summary>
    /// DEV-V2-15: the inventory-tidy official feature module. Owns the
    /// single-player tidy path:
    ///   - the enabled toggle is the ONE persisted setting (SettingsRuntime,
    ///     ClientLocal; off → the patches come off and requests fall back to
    ///     native, on → re-arm with a fresh fault gate, spec「LIT：设置」);
    ///   - the tidy plan is produced through the module's ITidyStrategy
    ///     (built-in default-grid-v1 wrapping the migrated InventorySolver);
    ///   - the UI patch set lives under the Harmony id = FeatureId (install
    ///     at start, UnpatchSelf at stop — spec「Harmony ID 收编」);
    ///   - static page state (direction/mode dictionaries, button refs) is
    ///     module-generation memory state, cleared at the stop boundary —
    ///     never leaked across generations (spec「静态表绑功能代际」);
    ///   - Stop maps the old three-phase unload: quiesce (refuse new
    ///     requests) → dispatcher shutdown (drain + cancel queued work) →
    ///     full teardown (unpatch + clear state).
    /// </summary>
    internal sealed class InventoryTidyModule : IFeatureModule
    {
        private const string EnabledSettingId = "inventorytidy.enabled";
        private Harmony harmony;

        internal InventoryTidyModule(string settingsRoot)
            : this(new FeatureId(LitRuntime.FeatureIdValue), new FileSettingsPersistence(settingsRoot))
        {
        }

        internal InventoryTidyModule(FeatureId feature, ISettingsPersistence persistence)
        {
            Feature = feature;
            if (persistence == null) throw new ArgumentNullException(nameof(persistence));
            Settings = new SettingsRuntime(feature, new[] { ToggleDescriptor(feature) }, persistence);
            Strategy = new DefaultGridV1Strategy();
            FaultGate = new LocalTidyFaultGate();
            Enabled = ReadToggle();
        }

        internal FeatureId Feature { get; }
        internal SettingsRuntime Settings { get; }
        internal LocalTidyFaultGate FaultGate { get; }

        /// <summary>The strategy the module plans with; replacement is a developer seam, null is a developer error.</summary>
        internal ITidyStrategy Strategy { get; set; }

        internal bool Enabled { get; private set; }
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
            OwnedEvents = bootstrap.OwnedEvents;
            Events = bootstrap.Events;
            Network = bootstrap.Network;
            LifecycleGeneration = bootstrap.LifecycleGeneration;
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
            // DEV-V2-21: stop phase 1 also quiesces the network service —
            // frames are refused, but the dispatcher drain below can still
            // compensate queued requests with a terminal Rejected send.
            NetService?.BeginQuiesce();
            LitRuntime.LogInfo("[Tidy] 模块停止：阶段 1/3 静默（拒绝新整理请求）");

            // 阶段 2：dispatcher 关停 — drain 已排队任务并对每个执行 Cancel
            // 回调（本地路径无 ledger 补偿，Cancel 只记录诊断）。
            MainThreadDispatcher.Shutdown();
            LitRuntime.LogInfo("[Tidy] 模块停止：阶段 2/3 dispatcher 关停完成");

            // 阶段 3：完全关停 — 撤销自身补丁 + 清空功能代际静态表 + 解绑日志缝
            //（解绑放在收尾日志之后，阶段完成信息仍可见）。
            UninstallPatches();
            InventoryTidyUiPatch.ResetStateForShutdown();
            // DEV-V2-21: the network service fully tears down AFTER the
            // dispatcher drain — queued compensations already ran; the
            // channel unregisters and every memory table drops while the
            // disk persistence survives untouched.
            NetService?.Stop();
            NetService = null;
            FaultBook = null;
            LitRuntime.LogInfo("[Tidy] 模块停止：阶段 3/3 完全关停（补丁已撤、静态表已清）");
            LitRuntime.LogSink = null;
            LitRuntime.ErrorLogSink = null;
        }

        /// <summary>
        /// Idempotent start: caches the main-thread id for the transaction
        /// service and installs the UI patch when enabled. Production binds
        /// this at Awake (the host start path that drives
        /// IFeatureModule.Start belongs to a later ticket); Start forwards
        /// here so the module behaves correctly when that path exists.
        /// </summary>
        internal void EnsureStarted()
        {
            if (Started) return;
            LitRuntime.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            // Generation boundary: reopen the dispatcher queue this module
            // generation owns (a previous generation's Stop closed it).
            MainThreadDispatcher.EnsureOpen();
            Started = true;
            if (Enabled && !ShuttingDown)
            {
                InstallPatches();
            }
        }

        /// <summary>
        /// The panel toggle's effect: reads the authoritative setting and
        /// applies the patch state immediately. Disable = native fallback
        /// (patches off, requests refused); enable = patches re-installed
        /// and the fault gate reset. This is NOT a module generation
        /// boundary: the dispatcher queue state is untouched here (reopen
        /// belongs to EnsureStarted; Shutdown belongs to Stop).
        /// </summary>
        internal void RefreshSwitches()
        {
            Enabled = ReadToggle();
            if (!Enabled || ShuttingDown)
            {
                UninstallPatches();
                return;
            }
            FaultGate.Reset();
            InstallPatches();
        }

        internal LitTidyRequestResult RequestLocalTidy(byte page, TidyMode mode, bool sortDescending)
        {
            if (!Started || !Enabled || ShuttingDown) return LitTidyRequestResult.NativeFallback;
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
        /// DEV-V2-21: the ONE tidy request entry the UI drives. The server
        /// role (single player, listen host, dedicated server) tidies
        /// LOCALLY — the authority path with generation 0 — and a true
        /// client sends the reliable network request; a client never falls
        /// back to a local unauthoritative tidy.
        /// </summary>
        internal LitTidyRequestResult RequestTidy(byte page, TidyMode mode, bool sortDescending)
        {
            if (!Started || !Enabled || ShuttingDown) return LitTidyRequestResult.NativeFallback;
            if (LitTidyProductionAuthority.IsServerRole()) return RequestLocalTidy(page, mode, sortDescending);
            var service = NetService;
            if (service == null || !service.Started) return LitTidyRequestResult.RejectedNoSession;
            return service.RequestTidy(page, mode, sortDescending);
        }

        /// <summary>Main-thread pump for the dispatcher queue (driven by the plugin update tick).</summary>
        internal void Tick()
        {
            MainThreadDispatcher.ProcessAll();
            // DEV-V2-21: session discovery rides the same pump — an
            // established session found here receives its challenge/scope
            // even when the module started after the handshake.
            NetService?.Tick();
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

        private bool ReadToggle()
        {
            SettingValue value;
            uint revision;
            return Settings.TryGet(EnabledSettingId, out value, out revision) && value.Boolean;
        }

        private static SettingDescriptor ToggleDescriptor(FeatureId feature)
        {
            return new SettingDescriptor(
                feature, EnabledSettingId, EnabledSettingId, EnabledSettingId,
                SettingKind.Toggle, SettingAuthority.ClientLocal, SettingValue.Toggle(true),
                default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                null, 0, null, 1, 0, null, null);
        }
    }
}
