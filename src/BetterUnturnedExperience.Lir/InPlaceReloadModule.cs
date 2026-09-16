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

        // ── DEV-V5-07：换弹技能 0～2（进度=等级账 / 运行状态=冷却窗+自动轮 /
        // 呈现=分区与回执）——三层分家，全部经本模块编排。──

        /// <summary>进度层（Start 建、从自有文件恢复；Stop 不清账）。</summary>
        internal ReloadSkillRuntime SkillRuntime { get; private set; }

        /// <summary>运行状态层：2 级自动压弹的待压表（Stop 即清，不过代际）。</summary>
        internal ReloadAutoRoundScheduler AutoRounds { get; } = new ReloadAutoRoundScheduler();

        /// <summary>技能引擎缝（测试假件或生产实现）。</summary>
        internal ILirSkillHooks SkillHooks { get; private set; }

        /// <summary>分区画面面在册位（与两压弹面/HUD 面同代际共存互撤）。</summary>
        internal bool SkillPatchInstalled { get; private set; }
        internal string SkillStartGateDiagnostics { get; private set; } = string.Empty;

        /// <summary>DEV-V5-07: host-test seam for the surface-A install (false
        /// = refuse → the whole four-face install rolls back — 04/05/06 同律).</summary>
        internal static Func<Type, bool> SkillPatchInstallerForTests;

        /// <summary>DEV-V5-07: host-test seams — fake hooks (the production
        /// LirSkillEngineHooks touches engine types; host tests replace it
        /// wholesale), manual clock (production = Stopwatch 单调秒), and
        /// persistence (production = 自有文件；测试 = 内存假件).</summary>
        internal ILirSkillHooks SkillHooksForTests;
        internal Func<double> SkillClockForTests;
        internal IReloadSkillPersistence SkillPersistenceForTests;

        /// <summary>技能秒源（单调；与压弹闸门的重放窗同源口径，纯托管零引擎）。</summary>
        private double SkillNowSeconds
        {
            get
            {
                var seam = SkillClockForTests;
                if (seam != null) return seam();
                return System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;
            }
        }

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
            // DEV-V5-07 技能面装配：进度层从自有文件恢复（读失败=空账继续，
            // 提交时全账重写——绝不拿脏账当权威），运行状态层全新（窗/轮不过
            // 代际），引擎缝 = 测试假件或生产实现。绑定到网络服务后，双击成交
            // 的技能窗、升级执行、2 级自动压弹全部生效。
            var persistence = SkillPersistenceForTests ?? (IReloadSkillPersistence)new ReloadSkillFilePersistence(
                System.IO.Path.Combine(
                    BetterUnturnedExperience.Plugin.BueSettingsRuntime.ProductionSettingsRoot,
                    ReloadSkillFilePersistence.FileName));
            var store = new ReloadSkillStore();
            if (!persistence.TryLoad(out var savedRecords, out var loadError))
            {
                LirRuntime.LogError("[ReloadSkill] 等级账读取失败（本代以空账继续）: " + loadError);
            }
            var applied = store.Restore(savedRecords ?? new List<ReloadSkillRecord>(), out var rejectedRecords);
            if (rejectedRecords > 0)
            {
                LirRuntime.LogWarning("[ReloadSkill] 等级账恢复丢弃坏记录 " + rejectedRecords + " 条（应用 " + applied + " 条）");
            }
            SkillRuntime = new ReloadSkillRuntime(store, ReadSkillClockSeconds, persistence);
            SkillHooks = SkillHooksForTests ?? new LirSkillEngineHooks(SkillRuntime);
            NetService.BindSkillHooks(this, SkillHooks);
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
            //    DEV-V5-07: 技能运行状态层同律（冷却窗+自动轮不过代际；等级账
            //    在 Store/自有文件，跨代际存活——Stop 绝不写账）。
            LirRepackGate.ResetForGeneration();
            SkillRuntime?.ResetForGeneration();
            AutoRounds.ResetForGeneration();
            Guard.Reset();
            Consumer = null;
            InputDriver = null;
            SkillHooks = null;
            SkillRuntime = null;
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
                PumpSkillRuntime();
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
                // DEV-V5-07: 第四面（技能分区）入列，互撤纪律不变。
                InstallAmmoReserveHud();
                InstallReloadSkillSurface();
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
                // 还在」的半态；HUD/技能面从未注入 = 注销空表零操作）。
                // DEV-V5-07: 技能面同律入互撤。
                HudPatchInstalled = false;
                AmmoReserveHudAdapter.RevokeAll();
                SkillPatchInstalled = false;
                ReloadSkillDashboardAdapter.Unregister();
                ReloadSkillLevelMirror.Clear();
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
            if (!PatchesInstalled && !HudPatchInstalled && !SkillPatchInstalled) return;
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
                // DEV-V5-07: 技能分区面同律注销；等级确认镜像=连接态，随面收。
                SkillPatchInstalled = false;
                ReloadSkillDashboardAdapter.Unregister();
                ReloadSkillLevelMirror.Clear();
                LirRuntime.LogInfo("[Lir] 原位换弹补丁已撤销（原生回退，仅撤自身 Harmony ID）");
            }
        }

        // ── DEV-V5-07：换弹技能面 ────────────────────────────────────────

        /// <summary>分区面登记（第四画面面；headless 不武装=决策非失败；
        /// 表面 A 探测接不上=降级表面 B 在注册侧接管，本面静默缺席零抛）。</summary>
        private void InstallReloadSkillSurface()
        {
            if (SkillPatchInstalled) return;
            if (BetterUnturnedExperience.Plugin.BueRuntimeCompletionChain.HeadlessDecision)
            {
                SkillStartGateDiagnostics = "reload-skill-headless-not-armed";
                LirRuntime.LogInfo("[ReloadSkill] U3DS headless：不武装换弹技能分区（Headless 决策门禁；技能权威/账/自动压弹不受影响）");
                return;
            }
            if (!ReloadSkillDashboardBinder.ProbeSurfaceA())
            {
                SkillStartGateDiagnostics = "reload-skill-surface-a-not-probed(fallback=settings-page)";
                LirRuntime.LogInfo("[ReloadSkill] 表面 A 接不上：等级面降级为该功能设置页（注册侧已挂 facet）");
                return;
            }
            var seam = SkillPatchInstallerForTests;
            if (seam != null)
            {
                if (!seam(typeof(ReloadSkillDashboardPatch)))
                    throw new InvalidOperationException("reload-skill surface-A patch refused");
            }
            else
            {
                ReloadSkillDashboardBinder.Bind(harmony, typeof(ReloadSkillDashboardPatch));
            }
            ReloadSkillDashboardAdapter.Register();
            SkillPatchInstalled = true;
            SkillStartGateDiagnostics = string.Empty;
            LirRuntime.LogInfo("[ReloadSkill] 换弹技能分区已登记（Harmony ID=" + LirRuntime.FeatureIdValue + "）");
        }

        private double ReadSkillClockSeconds() { return SkillNowSeconds; }

        /// <summary>主机帧泵：本机等级自确认（SP/房主直读自账，与客机线确认同
        /// 语义）+ 自动轮到点重检触发。每拍 O(pending) 且空表零操作。</summary>
        internal void PumpSkillRuntime()
        {
            var hooks = SkillHooks;
            if (hooks == null) return;
            if (!ReloadSkillLevelMirror.HasConfirmed && hooks.TryResolveLocalLevel(out var selfLevel))
            {
                ReloadSkillLevelMirror.ConfirmLevel(selfLevel);
            }
            AutoRounds.Tick(SkillNowSeconds, TryFireAutoRound);
        }

        /// <summary>到点重检（票面裁决 4）：等级仍 2 ∧ 同枪同匣指纹 ∧ 玩家可
        /// 解析；任一不满足=取消不强制（条目已被调度器消耗，不重试）。通过则
        /// 走同一权威入口（自动轮也吃技能窗与闸门，2 级无额外窗=与手动同速）。</summary>
        private bool TryFireAutoRound(ulong steamId, object capturedFingerprint)
        {
            var hooks = SkillHooks;
            if (hooks == null || !Started || ShuttingDown || !Enabled) return false;
            if (hooks.GetLevelFor(steamId) < ReloadSkillPolicy.MaxSkillLevel) return false;
            var fresh = hooks.CaptureFingerprint(steamId);
            if (fresh == null || !hooks.FingerprintMatches(capturedFingerprint, fresh)) return false;
            NetService?.ExecuteRepackFor(steamId, LirRepackNetwork.NextRequestId(), true);
            return true;
        }

        /// <summary>网络服务在手动双击成交（Committed∧total&gt;0）且等级=2 时
        /// 交来指纹：排固定等待的一轮（同玩家再排=替换——每成功至多一轮）。</summary>
        internal void ScheduleAutoRoundAfterManualSuccess(ulong steamId, object fingerprint)
        {
            AutoRounds.Schedule(steamId, fingerprint, SkillNowSeconds + ReloadSkillPolicy.AutoRoundDelaySeconds);
            LirRuntime.LogDiagnostic("[ReloadSkill] 2 级自动压弹已排队（steam=" + steamId + "，等待 "
                + ReloadSkillPolicy.AutoRoundDelaySeconds + "s）");
        }

        /// <summary>升级请求唯一入口（表面 A 按钮 / 表面 B 档位 / 测试直调共用）：
        /// 主机角色本地执行（校验+扣原版经验+落账+回执呈现），纯客机发功能私有
        /// 线请求，回执路径做镜像/toast/行复位。无三级目标。</summary>
        internal void HandleSkillUpgradeRequest(byte targetLevel)
        {
            if (!Started || !Enabled || ShuttingDown) return;
            if (targetLevel == 0 || targetLevel > ReloadSkillPolicy.MaxSkillLevel) return;
            var hooks = SkillHooks;
            if (hooks == null || Authority == null) return;
            try
            {
                var isServer = RoleProbeForTests ?? LirProductionAuthority.IsServerRole;
                if (isServer())
                {
                    if (Authority.TryResolveLocalPlayerSteamId(out var localId) && localId != 0UL)
                    {
                        var decision = hooks.ExecuteUpgrade(localId, targetLevel);
                        if (decision.Accepted)
                        {
                            ReloadSkillLevelMirror.ConfirmLevel(decision.NewLevel);
                            ShowSkillToast(ReloadSkillPolicy.MakeUpgradeAcceptedToast(decision.NewLevel, decision.Cost));
                        }
                        else
                        {
                            ShowSkillToast(ReloadSkillPolicy.MakeUpgradeRejectedToast(decision.Reason, targetLevel));
                        }
                        ResetSkillSettingsRow();
                        NotifySkillLevelConfirmed(ReloadSkillLevelMirror.ConfirmedLevel);
                        return;
                    }
                    LirRuntime.LogDiagnostic("[ReloadSkill] 本机玩家不可解析（headless），升级无面可收（U3DS 不经此路）");
                    return;
                }
                NetService?.RequestUpgradeFromServer(targetLevel);
            }
            catch (Exception error)
            {
                LirRuntime.LogError("[ReloadSkill] 升级请求异常（已隔离）: " + error.Message);
            }
        }

        /// <summary>表面 B 的 OnSettingsApplied 出口（注册侧 facet 挂这里）：把
        /// 「请求升级到 N」档翻译成升级请求并发完即复位。面板不是第二事实源。</summary>
        internal void HandleSkillSettingsApplied()
        {
            if (!Started || !Enabled || ShuttingDown) return;
            var view = SettingsView;
            if (view == null) return; // 表面 B 未装备（facet 未注册或无 view）
            try
            {
                if (!view.TryGet(ReloadSkillPolicy.UpgradeSettingId, out var value, out _)) return;
                var target = ReloadSkillSettingsSurface.MapOptionToTargetLevel(value.Text);
                if (target <= 0) return;
                HandleSkillUpgradeRequest((byte)target);
            }
            catch (Exception error)
            {
                LirRuntime.LogError("[ReloadSkill] 设置页请求处理异常（已隔离）: " + error.Message);
            }
        }

        /// <summary>请求发出后行复位「维持」（ClientLocal 写回；失败只留诊断——
        /// 等级真相在主机账/回执，行复位丢一帧无害）。</summary>
        internal void ResetSkillSettingsRow()
        {
            var view = SettingsView;
            if (view == null) return;
            try
            {
                if (!view.TryGet(ReloadSkillPolicy.UpgradeSettingId, out _, out var revision)) return;
                var request = ReloadSkillSettingsSurface.BuildResetRequest(Feature, LirRepackNetwork.NextRequestId(), revision);
                if (request == null) return;
                var result = view.Submit(request.Value);
                if (!result.Accepted)
                {
                    LirRuntime.LogDiagnostic("[ReloadSkill] 升级行复位被拒（等级真相仍以主机回执为准）");
                }
            }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[ReloadSkill] 升级行复位异常（忽略）: " + error.Message);
            }
        }

        /// <summary>网络服务/本地路径的 toast 出口（登记在案的 ToastSink，测试=记录器）。</summary>
        internal void ShowSkillToast(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try { ToastSink?.Invoke(text); }
            catch (Exception error) { LirRuntime.LogDiagnostic("[ReloadSkill] toast 呈现异常（忽略）: " + error.Message); }
        }

        /// <summary>等级确认后的分区即时重建（不重建=旧镜像挂到下次原版重建）。</summary>
        internal void NotifySkillLevelConfirmed(byte level)
        {
            _ = level;
            try { ReloadSkillDashboardAdapter.RequestRebuild(); }
            catch (Exception error)
            {
                LirRuntime.LogDiagnostic("[ReloadSkill] 分区重建异常（下次原版重建自然生效）: " + error.Message);
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
