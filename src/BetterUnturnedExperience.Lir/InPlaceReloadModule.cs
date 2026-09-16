using System;
using System.Collections.Generic;
using System.Threading;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the in-place-reload official feature module. Owns:
    ///   - the enabled toggle is the ONE persisted setting (DEV-V3-06: the
    ///     host-owned SettingsRuntime behind the injected scoped view,
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

        // DEV-V3-06: the module no longer constructs or holds a
        // SettingsRuntime — the host composes the ONE per-feature runtime
        // from the registration's settings facet and injects the scoped
        // view (IFeatureBootstrap.Settings); schema authority stays with
        // the feature (CreateSettingsDescriptors), file layout unchanged.
        internal InPlaceReloadModule()
            : this(new FeatureId(LirRuntime.FeatureIdValue))
        {
        }

        internal InPlaceReloadModule(FeatureId feature)
        {
            Feature = feature;
            Guard = new ReloadContextGuard();
            Enabled = ReadToggle();
        }

        internal static IReadOnlyList<SettingDescriptor> CreateSettingsDescriptors(FeatureId feature)
        {
            return new[] { ToggleDescriptor(feature) };
        }

        internal FeatureId Feature { get; }
        // DEV-V3-06: the host-injected scoped settings view (null only on
        // hand-composed stage-baseline bootstraps; then the descriptor
        // default applies).
        internal IScopedFeatureSettings SettingsView { get; private set; }
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

        /// <summary>DEV-V5-06: 弹药后备 HUD 面（画面类）的在册位——登记=会画、
        /// 注销+HideAll=不画；与两压弹权威面同代际共存、半装互撤归零。</summary>
        internal bool HudPatchInstalled { get; private set; }
        internal string HudStartGateDiagnostics { get; private set; } = string.Empty;

        /// <summary>DEV-V5-06: host-test seam replacing the real binder.Bind
        /// for the ammo-HUD surface (false = refuse → whole install rolls back).</summary>
        internal static Func<Type, bool> HudPatchInstallerForTests;

        /// <summary>DEV-V5-06: host-test seam for the two pre-existing reload
        /// surfaces (the same all-or-none installer pattern as 04/05 — without
        /// it the host can never even REACH the HUD surface install, because a
        /// real Patch() on an ECall-touching method throws first). Null in
        /// production: the historical attribute-style install stays verbatim.</summary>
        internal static Func<Type, bool> CorePatchInstallerForTests;

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
            // DEV-V4-09：Stop 解绑生产日志缝（插件卸载卫生），本代际 Start 重绑
            // （与 Lit 同构——停用→再启用后模块诊断不得失明）。
            BindProductionLog();
            // DEV-V3-06 official first consumption: the enabled toggle rides
            // the host-injected scoped view before the patches arm.
            AttachSettingsView(bootstrap.Settings); // DEV-V3-06: bind + read BEFORE arming
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
                InstallCoreReloadPatches();
                // DEV-V5-06: 三面同代共存——HUD 面失败 = 整体互撤归零（04/05
                // 同律，禁半装；headless 跳过画面面是决策不是失败）。
                InstallAmmoReserveHud();
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
                // DEV-V5-06: 在册同步归零 + 呈现即时收（不留「补丁没了、登记
                // 还在」的半态；HUD 面从未注入 = RevokeAll 空表零操作）。
                HudPatchInstalled = false;
                AmmoReserveHudAdapter.RevokeAll();
            }
        }

        /// <summary>DEV-V5-06: 既有两压弹权威面的登记（无测试缝时与迁移前逐字
        /// 同形；有缝时按 04/05 installer 模式走——生命周期红测需三面同形驱动，
        /// 否则真 Patch() 先在 ECall 边界抛，HUD 面根本到不了）。拒装 = 上抛 →
        /// InstallPatches 三面互撤归零。</summary>
        private void InstallCoreReloadPatches()
        {
            var seam = CorePatchInstallerForTests;
            if (seam != null)
            {
                if (!seam(typeof(UseableGunReceiveAttachMagazinePatch)))
                    throw new InvalidOperationException("lir-core patch refused: UseableGunReceiveAttachMagazinePatch");
                if (!seam(typeof(ForceAddItemPatch)))
                    throw new InvalidOperationException("lir-core patch refused: ForceAddItemPatch");
                return;
            }
            harmony.CreateClassProcessor(typeof(UseableGunReceiveAttachMagazinePatch)).Patch();
            harmony.CreateClassProcessor(typeof(ForceAddItemPatch)).Patch();
        }

        /// <summary>
        /// DEV-V5-06: 弹药后备 HUD 面的登记（U3DS headless 不武装——只砍画面
        /// 裁决，T1 同律；两压弹权威面在头less照常登记）。绑定唯一事实源 =
        /// AmmoReserveHudBinder（显式 updateInfo 解析，宿主可证）；假 installer
        /// 缝返回 false = 拒装 → 异常上抛 → InstallPatches 三面互撤归零。
        /// </summary>
        private void InstallAmmoReserveHud()
        {
            if (HudPatchInstalled) return;
            if (BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision)
            {
                HudStartGateDiagnostics = "ammo-hud-headless-not-armed";
                LirRuntime.LogInfo("[AmmoHud] U3DS headless：不武装弹药后备 HUD（Headless 决策门禁；压弹权威面不受影响）");
                return;
            }
            var types = AmmoReserveHudBinder.PatchSurface;
            for (var i = 0; i < types.Count; i++)
            {
                var seam = HudPatchInstallerForTests;
                if (seam != null)
                {
                    if (!seam(types[i])) throw new InvalidOperationException("ammo-hud patch refused: " + types[i].Name);
                    continue;
                }
                AmmoReserveHudBinder.Bind(harmony, types[i]);
            }
            HudPatchInstalled = true;
            HudStartGateDiagnostics = string.Empty;
            LirRuntime.LogInfo("[AmmoHud] 弹药后备 HUD 补丁已登记（Harmony ID=" + LirRuntime.FeatureIdValue + "）");
        }

        private void UninstallPatches()
        {
            if (!PatchesInstalled && !HudPatchInstalled) return;
            try
            {
                if (harmony != null) harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                // 04 实证教训：撤装反转 IL 会在无 Unity 运行时的进程触发再 JIT
                // 异常——边界绝不向生命周期机抛出，下方记账必须落地。
                LirRuntime.LogError("[Lir] 补丁撤销异常（登记已强制收回，功能面按停用运行）: " + e.Message);
            }
            finally
            {
                PatchesInstalled = false;
                ActiveModule = null;
                // DEV-V5-06: 注销=不画（登记交还 + 在枪上的残留读数即时收）。
                HudPatchInstalled = false;
                AmmoReserveHudAdapter.RevokeAll();
                LirRuntime.LogInfo("[Lir] 原位换弹补丁已撤销（原生回退，仅撤自身 Harmony ID）");
            }
        }

        /// <summary>DEV-V3-06: bind the host-injected scoped settings view and
        /// re-read the switch (Start does this from the bootstrap; host-test
        /// fixtures attach directly).</summary>
        internal void AttachSettingsView(IScopedFeatureSettings view)
        {
            SettingsView = view;
            Enabled = ReadToggle();
        }

        private bool ReadToggle()
        {
            var view = SettingsView;
            if (view == null) return true; // no wired view yet = the descriptor default (on)
            SettingValue value;
            uint revision;
            return view.TryGet(EnabledSettingId, out value, out revision) && value.Boolean;
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
