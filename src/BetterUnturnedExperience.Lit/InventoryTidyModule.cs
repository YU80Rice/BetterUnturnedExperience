using System;
using System.Threading;
using BetterUnturnedExperience.Contracts;
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

        public FeatureStartResult Start(IFeatureBootstrap bootstrap)
        {
            EnsureStarted();
            return default(FeatureStartResult);
        }

        public void Stop(FeatureStopReason reason)
        {
            if (ShuttingDown) return;
            ShuttingDown = true;
            Started = false;
            LitRuntime.LogInfo("[Tidy] 模块停止：阶段 1/3 静默（拒绝新整理请求）");

            // 阶段 2：dispatcher 关停 — drain 已排队任务并对每个执行 Cancel
            // 回调（本地路径无 ledger 补偿，Cancel 只记录诊断）。
            MainThreadDispatcher.Shutdown();
            LitRuntime.LogInfo("[Tidy] 模块停止：阶段 2/3 dispatcher 关停完成");

            // 阶段 3：完全关停 — 撤销自身补丁 + 清空功能代际静态表 + 解绑日志缝
            //（解绑放在收尾日志之后，阶段完成信息仍可见）。
            UninstallPatches();
            InventoryTidyUiPatch.ResetStateForShutdown();
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

        /// <summary>Main-thread pump for the dispatcher queue (driven by the plugin update tick).</summary>
        internal void Tick()
        {
            MainThreadDispatcher.ProcessAll();
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
