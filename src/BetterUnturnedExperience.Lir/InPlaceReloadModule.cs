using System;
using System.Threading;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Settings;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the in-place-reload official feature module. Owns:
    ///   - the enabled toggle is the ONE persisted setting (SettingsRuntime,
    ///     ClientLocal; off = native fallback — the patches come off, the
    ///     double tap does nothing, no auto reload after tidy; on = re-arm);
    ///   - the three Harmony adapters live under the Harmony id = FeatureId
    ///     (install at start, UnpatchSelf at stop — spec「Harmony ID 收编」;
    ///     Stop revokes ONLY its own ID, never another feature's patches);
    ///   - the frame work rides the frozen HostTick seam (no self-built Unity
    ///     Update pump): deferred network init on the first game-thread frame
    ///     (the migrated semantic), dispatcher drain, double-tap detection;
    ///   - TidyCompleted consumption rides the frozen feature-event seam
    ///     through TidyCompletedConsumer → AutoReloadAfterTidyAction — the old
    ///     cross-plugin Harmony postfix and its reflection-by-type-name are
    ///     gone by construction;
    ///   - the network service is one per module generation (created at
    ///     Start), torn down at Stop; every static table (dispatcher, gate,
    ///     guard) is cleared at the stop boundary — nothing crosses
    ///     generations (spec「静态表绑功能代际」).
    /// </summary>
    internal sealed class InPlaceReloadModule : IFeatureModule
    {
        private const string EnabledSettingId = "inplacereload.enabled";
        private Harmony harmony;

        internal InPlaceReloadModule(string settingsRoot)
            : this(new FeatureId(LirRuntime.FeatureIdValue), new FileSettingsPersistence(settingsRoot))
        {
        }

        internal InPlaceReloadModule(FeatureId feature, ISettingsPersistence persistence)
        {
            Feature = feature;
            if (persistence == null) throw new ArgumentNullException(nameof(persistence));
            Settings = new SettingsRuntime(feature, new[] { ToggleDescriptor(feature) }, persistence);
            Guard = new ReloadContextGuard();
            Enabled = ReadToggle();
        }

        internal FeatureId Feature { get; }
        internal SettingsRuntime Settings { get; }
        internal ReloadContextGuard Guard { get; }

        /// <summary>The patch-facing module handle (the LIT ActiveModule pattern): live only while the patches are.</summary>
        internal static InPlaceReloadModule ActiveModule { get; private set; }

        internal bool Enabled { get; private set; }
        internal bool Started { get; private set; }
        internal bool ShuttingDown { get; private set; }
        internal bool PatchesInstalled { get; private set; }

        /// <summary>The multiplayer path's honest readiness — false after a failed/rolled-back network init (observable, never assumed).</summary>
        internal bool MultiplayerReady { get; private set; }

        /// <summary>The Harmony instance id the patches live under (= the FeatureId; own ID only).</summary>
        internal string PatchHarmonyId { get; private set; }
        internal string StartGateDiagnostics { get; private set; } = string.Empty;

        // DEV-V2-22: the host-composed seams captured at IFeatureModule.Start.
        internal IOwnedFeatureEventPublisher OwnedEvents { get; private set; }
        internal IFeatureEventSubscriber Events { get; private set; }
        internal IBueNetworkApi Network { get; private set; }
        internal ulong LifecycleGeneration { get; private set; }
        /// <summary>DEV-V3-04: the platform main-thread dispatcher view — the
        /// official first consumer of the seam (泵线程→BUE dispatcher→LIR
        /// 主线程业务). Null until a host wires it (availability-matrix stage
        /// baseline): the network service then falls back to the historical
        /// inline host-frame drain, so pre-wiring hosts keep the old shape.</summary>
        internal IFeatureMainThread MainThread { get; private set; }

        /// <summary>The engine-facing authority (production default or the test fake).</summary>
        internal ILirRepackAuthority Authority { get; private set; }

        internal LirRepackNetwork NetService { get; private set; }
        internal TidyCompletedConsumer Consumer { get; private set; }
        internal ReloadInputDriver InputDriver { get; private set; }

        /// <summary>Production binds LirToast.Show; tests bind a recorder (success toasts route through here).</summary>
        internal Action<string> ToastSink { get; set; }

        /// <summary>Host-test seam: overrides the authority composition.</summary>
        internal Func<ILirRepackAuthority> AuthorityFactoryForTests;

        /// <summary>Host-test seam: composes the network service (fake transport, fake authority).</summary>
        internal Func<InPlaceReloadModule, IBueNetworkApi, LirRepackNetwork> NetServiceFactoryForTests;

        /// <summary>Host-test seam: overrides the reload-key polling (production reads ControlsSettings/InputEx).</summary>
        internal Func<bool> KeyDownProviderForTests;

        /// <summary>Host-test seam: overrides the server-role probe (production reads Provider.isServer — unavailable in host tests).</summary>
        internal Func<bool> RoleProbeForTests;

        private bool multiplayerInitObserved;

        public FeatureStartResult Start(IFeatureBootstrap bootstrap)
        {
            if (bootstrap == null) throw new ArgumentNullException(nameof(bootstrap));
            if (bootstrap.Events == null || bootstrap.Network == null)
                throw new ArgumentException("the host bootstrap must compose the event subscriber view and the network API (never null)", nameof(bootstrap));
            OwnedEvents = bootstrap.OwnedEvents;
            Events = bootstrap.Events;
            Network = bootstrap.Network;
            LifecycleGeneration = bootstrap.LifecycleGeneration;
            MainThread = bootstrap.MainThread; // DEV-V3-04: nullable stage-baseline seam (see the property)
            EnsureStarted();

            Authority = AuthorityFactoryForTests != null ? AuthorityFactoryForTests() : new LirProductionAuthority();
            if (Authority == null) throw new InvalidOperationException("the LIR authority must never compose to null");
            NetService = NetServiceFactoryForTests != null
                ? NetServiceFactoryForTests(this, bootstrap.Network)
                : new LirRepackNetwork(bootstrap.Network, Authority, LirProductionAuthority.IsServerRole);
            // DEV-V3-04 official first consumption: the repack main-thread
            // handoff rides the platform dispatcher instead of LIR's own
            // drain pump (the business queue — per-sender coalesce, reply
            // priority, TTL, throttled summary — stays feature-private).
            NetService.BindMainThread(MainThread);
            if (ToastSink == null) ToastSink = LirToast.Show;
            NetService.BindToastSink(ToastSink);
            // The consumer resolves the event's generation → target player:
            // 0 = the local player, a session generation = that session's peer.
            Consumer = new TidyCompletedConsumer(new AutoReloadAfterTidyAction(this), ResolveReloadTarget);
            InputDriver = new ReloadInputDriver(KeyDownProviderForTests ?? ReadReloadKeyDown, OnDoubleTapReload);

            // The frozen clock/event seams: the module owns its subscriptions
            // and the host drops them at the stop boundary (UnsubscribeAll
            // after Stop returns — the DEV-V2-21/22 handoff).
            Events.Subscribe<HostTick>(OnHostTick);
            Events.Subscribe<TidyCompleted>(Consumer.Handle);

            if (Enabled && !ShuttingDown)
            {
                InstallPatches();
            }
            LirRuntime.LogInfo("[Lir] 模块已启动（宿主 bootstrap：功能代际=" + bootstrap.LifecycleGeneration + "，网络注册延迟至首帧游戏线程）");
            return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-LIR-START");
        }

        public void Stop(FeatureStopReason reason)
        {
            if (ShuttingDown) return;
            ShuttingDown = true;
            Started = false;

            // 1) The network service first: channel unregistered, every
            //    subscription handle disposed, session/pending tables cleared,
            //    and THIS generation's dispatcher queue closed (refuses new
            //    work, clears both queues) — no stale cross-generation work
            //    ever executes.
            NetService?.Stop();
            NetService = null;

            // 2) The patches come off (ONLY this module's Harmony instance —
            //    UnpatchSelf under the FeatureId; BII and other features are
            //    untouched) and the module handle goes with them.
            UninstallPatches();

            // 3) The gate is feature-generation state: cooldowns, the replay
            //    window and quarantines all clear at the stop boundary.
            LirRepackGate.ResetForGeneration();
            Guard.Reset();
            Consumer = null;
            InputDriver = null;
            multiplayerInitObserved = false;
            MultiplayerReady = false;
            LirRuntime.LogInfo("[Lir] 模块停止：网络服务已注销、队列与闸门已清、补丁已撤（原生回退）");
            LirRuntime.LogSink = null;
            LirRuntime.ErrorLogSink = null;
        }

        /// <summary>
        /// The panel toggle's effect: reads the authoritative setting and
        /// applies the patch state immediately (off = native fallback; on =
        /// re-install). Not a generation boundary — the dispatcher queue and
        /// the gate survive a settings flip.
        /// </summary>
        internal void RefreshSwitches()
        {
            Enabled = ReadToggle();
            if (!Enabled || ShuttingDown)
            {
                UninstallPatches();
                return;
            }
            InstallPatches();
        }

        /// <summary>
        /// The ONE host-frame entry (subscribed to the frozen HostTick seam).
        /// Order mirrors the old plugin Update: deferred network init (first
        /// game-thread frame) → session reconcile → dispatcher drain (state
        /// progression + work TTL) → double-tap detection / key polling.
        /// Never throws into the host clock chain.
        /// </summary>
        internal void OnHostTick(HostTick tick)
        {
            if (!Started || ShuttingDown) return;
            try
            {
                if (NetService != null)
                {
                    if (!multiplayerInitObserved)
                    {
                        MultiplayerReady = NetService.EnsureInitializedOnGameThread();
                        multiplayerInitObserved = true;
                        if (!MultiplayerReady)
                        {
                            StartGateDiagnostics = StartGateDiagnostics.Length == 0
                                ? "multiplayer-init-rolled-back"
                                : StartGateDiagnostics + ";multiplayer-init-rolled-back";
                            LirRuntime.LogError("[Lir] 联机压弹初始化失败（已回滚）——单机/本地压弹仍可用");
                        }
                    }
                    NetService.Tick();
                    NetService.Drain();
                }
                InputDriver?.Tick(tick);
            }
            catch (Exception error)
            {
                LirRuntime.LogError("[Lir] 帧驱动异常（已隔离）: " + error.Message);
            }
        }

        /// <summary>
        /// The double-tap trigger (the input driver's callback). The server
        /// role (single player, listen host, U3DS) executes locally through
        /// the SAME main-thread entry the queued remote requests use; a true
        /// client sends the reliable request and waits for the targeted
        /// reply. The enabled switch gates everything.
        /// </summary>
        internal void OnDoubleTapReload()
        {
            if (!Started || !Enabled || ShuttingDown) return;
            try
            {
                var isServerRole = RoleProbeForTests ?? LirProductionAuthority.IsServerRole;
                if (isServerRole())
                {
                    // 单机/房主与远端请求使用同一主线程事务入口（旧语义保持）。
                    if (Authority != null && Authority.TryResolveLocalPlayerSteamId(out var localId) && localId != 0UL)
                    {
                        NetService?.ExecuteRepackFor(localId, LirRepackNetwork.NextRequestId());
                    }
                    else
                    {
                        LirRuntime.LogWarning("[RepackB] 本地玩家不可解析（player.channel.owner.playerID == null），跳过");
                    }
                    return;
                }
                if (NetService == null || !NetService.Started)
                {
                    LirRuntime.LogDiagnostic("[RepackB] 联机网络层未就绪，拒绝发送压弹请求。");
                    return;
                }
                NetService.RequestRepackFromServer();
            }
            catch (Exception error)
            {
                LirRuntime.LogError("[RepackB] uncaught: " + error.Message);
            }
        }

        /// <summary>
        /// The tidy follow-up merge (the AutoReloadAfterTidyAction adapter
        /// entry): gate-checked by the production authority, executed
        /// synchronously on the thread that received the event — the old
        /// postfix nesting shape. Outcome surfaces as structured logs only;
        /// the tidy itself is already committed either way.
        /// </summary>
        internal void ExecuteTidyMerge(ulong targetSteamId, ulong connectionGeneration, ulong transactionId)
        {
            var result = Authority.ExecuteMerge(targetSteamId, LirRepackNetwork.NextRequestId());
            switch (result.Outcome)
            {
                case LirMergeOutcome.Committed:
                    LirRuntime.LogInfo("[MergeA] 整理后自动压弹完成（target=" + targetSteamId + ", merged=" + result.TotalMerged + ", txn=" + transactionId + "）");
                    break;
                case LirMergeOutcome.RestoreFailed:
                    LirRuntime.LogError("[MergeA] CRITICAL: 整理后合并 RestoreFailed（target=" + targetSteamId + "），已 Quarantine");
                    break;
                case LirMergeOutcome.RejectedCooldown:
                    LirRuntime.LogDiagnostic("[MergeA] 整理后自动压弹命中冷却窗口，跳过（target=" + targetSteamId + "）");
                    break;
                case LirMergeOutcome.PlayerMissing:
                    LirRuntime.LogDiagnostic("[MergeA] 整理后自动压弹目标玩家不可解析，跳过");
                    break;
                default:
                    // NoChange / RolledBack / AbortedStateDrift: 一次扫描已发生，无交付。
                    break;
            }
        }

        /// <summary>The consumer's generation → target resolver (0 = unresolvable, the event drops).</summary>
        internal ulong ResolveReloadTarget(ulong connectionGeneration)
        {
            if (connectionGeneration == 0UL)
            {
                return Authority != null && Authority.TryResolveLocalPlayerSteamId(out var localId) ? localId : 0UL;
            }
            return NetService != null ? NetService.ResolveSessionPeer(connectionGeneration) : 0UL;
        }

        /// <summary>Idempotent start: caches the main-thread id for the deferred network init.</summary>
        internal void EnsureStarted()
        {
            if (Started) return;
            LirRuntime.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            Started = true;
        }

        /// <summary>The production log binding (registration time): domain sinks → BueRuntimeLog channels.</summary>
        internal void BindProductionLog()
        {
            LirRuntime.LogSink = line => BetterUnturnedExperience.Plugin.BueRuntimeLog.Runtime(line);
            LirRuntime.ErrorLogSink = line => BetterUnturnedExperience.Plugin.BueRuntimeLog.Error(line);
        }

        /// <summary>The production key poll (chat focus / rebinding swallowing lives inside InputEx.GetKeyDown).</summary>
        private bool ReadReloadKeyDown()
        {
            KeyCode reloadKey = ControlsSettings.reload;
            if (reloadKey == KeyCode.None) return false;
            return InputEx.GetKeyDown(reloadKey);
        }

        private void InstallPatches()
        {
            if (PatchesInstalled) return;
            try
            {
                if (harmony == null)
                {
                    harmony = new Harmony(LirRuntime.FeatureIdValue);
                    PatchHarmonyId = LirRuntime.FeatureIdValue;
                }
                // The handle goes live BEFORE Patch(): no game call can
                // interleave (the install runs on the game thread itself).
                ActiveModule = this;
                harmony.CreateClassProcessor(typeof(UseableGunReceiveAttachMagazinePatch)).Patch();
                harmony.CreateClassProcessor(typeof(ForceAddItemPatch)).Patch();
                PatchesInstalled = true;
                StartGateDiagnostics = string.Empty;
                LirRuntime.LogInfo("[Lir] 原位换弹补丁已安装（Harmony ID=" + LirRuntime.FeatureIdValue + "）");
            }
            catch (Exception e)
            {
                // 环境闸（宿主测试进程无法装真机补丁 / 游戏版本漂移）：记录
                // 结构化诊断而非静默失败——半装回滚，失败即整体撤销自身。
                StartGateDiagnostics = "patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                LirRuntime.LogError("[Lir] 原位换弹补丁安装失败（原位换弹不可用）: " + e.Message);
                try { if (harmony != null) harmony.UnpatchSelf(); } catch (Exception) { }
                ActiveModule = null;
                PatchesInstalled = false;
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
                ActiveModule = null;
                LirRuntime.LogInfo("[Lir] 原位换弹补丁已撤销（原生回退，仅撤自身 Harmony ID）");
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
