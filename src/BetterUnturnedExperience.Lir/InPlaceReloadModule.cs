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

        /// <summary>本次武装事务整体成功（四面的武装事务跑完；headless 的决策跳过不是失败）——
        /// DEV-V6-11 同名的「整笔成功」语义，不是「权威面在册」。</summary>
        internal bool PatchesInstalled { get; private set; }

        // ── DEV-V6-12（V6-T5 Q2 + 2026-09-18 追加裁决）：补丁所有权归一 ──

        /// <summary>权威压弹面（两处 Harmony）在册位——与画面面分家，半装判定按面可判。</summary>
        internal bool CorePatchesInstalled { get; private set; }
        internal string CoreStartGateDiagnostics { get; private set; } = string.Empty;

        /// <summary>已移交平台账的句柄（撤销=对它 Dispose；平台边界释放同一枚）。</summary>
        internal LirPatchHandle PatchRegistration { get; private set; }

        /// <summary>拆除所有权是否已移交平台账（移交后模块在正常停止路径不再自拆）。</summary>
        internal bool PatchTeardownDelegated { get; private set; }

        /// <summary>DEV-V6-12 host-test seam：替换 Harmony 反打（宿主不可达真引擎逆 JIT 故障，
        /// 与 DEV-V6-11 的 LIT 同款替身缝）。null = 生产真 UnpatchSelf。</summary>
        internal static Func<bool> PatchRevertForTests;

        /// <summary>启动口袋的补丁口（Start 捕获；RefreshSwitches 的同代际重登记走同一视图）。</summary>
        private IFeaturePatching patching;

        /// <summary>本次武装事务是否已失败（失败=立即整体自拆且事务终止，其余面不再武装）。</summary>
        private bool armTransactionFailed;

        /// <summary>本代际的 Harmony 补丁集**真在架**（含半装）：真拆除动作只在它为真时跑
        /// ——从未武装过的一代不产生拆除动作与自拆留痕。只由「反转动作真的返回了」清掉
        /// （停止边界的在册归零不碰它：那是在册开关，不是补丁是否还装着的事实）。
        /// 边界释放（平台账）据此判断该真拆还是对未武装状态空转。</summary>
        private bool patchSetInstalled;

        /// <summary>武装期捕获的宿主日志口：边界释放发生在模块自己解绑 sink 之后
        /// （Stop 的解绑卫生），释放事实不得因此消失（与句柄的释放行同律）。</summary>
        private Action<string> patchReleaseLog;

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
            // DEV-V6-12（与 DEV-V4-06 的整理同律）：一次 Start 就是一代——上一代停止
            // 边界不得泄漏进来，否则工厂复用的接线实现在停用→再启用后永远不再武装。
            ShuttingDown = false;
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
                System.IO.Path.Combine(RequireHostSettingsRoot(), ReloadSkillFilePersistence.FileName));
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

            // DEV-V6-12（V6-T5 Q2 + 2026-09-18 追加裁决）：武装与登记都只发生在生命周期内
            // ——工厂路径交付惰性模块；启动口袋的补丁口在这里捕获，代际开启、补丁武装、
            // 经启动口袋登记三件事按序完成（缺口袋/拒绝/半装即立即自拆，平台账外零补丁）。
            patching = bootstrap.Patching;
            PatchTeardownDelegated = false;
            PatchRegistration = null;
            ArmPatches();
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

            // 2) 补丁所有权交接（DEV-V6-12）：登记受理 ⇒ 拆除在平台账上，模块在正常
            //    停止路径不再自拆（平台拆一次）；未移交才 fail-closed 自拆（防御路径）。
            ReleasePatchOwnership();

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
        /// DEV-V6-12（V6-T5 Q2 追加裁决）：一代内可重复拆装——登记是**代际级**的（一代恰
        /// 一条账项、句柄不提前释放），开关只切拆装：off 经句柄的同一条拆除动作撤销（不双拆、
        /// 账上不留已拆句柄），on 用同一身份重新武装（不新增账项、不换句柄）。
        /// </summary>
        internal void RefreshSwitches()
        {
            Enabled = ReadToggle();
            if (!Enabled || ShuttingDown)
            {
                DisarmPatchesForToggle("settings-off");
                return;
            }
            ArmPatches();
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

        /// <summary>Idempotent start: caches the main-thread id for the deferred network init.
        /// DEV-V6-12: called ONLY from Start (the lifecycle owns the generation boundary) — the
        /// factory path delivers a born-inert module (no early Started/armed state).</summary>
        internal void EnsureStarted()
        {
            if (Started) return;
            LirRuntime.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            Started = true;
        }

        /// <summary>The production log binding (registration + Start rebind): domain sinks read
        /// the host-injected composition delegates (DEV-V6-02C — the feature no longer names the
        /// host). Unbound injection = null sinks = the existing swallow-silently contract; Stop
        /// clears the sinks and a re-Start rebinds from the SAME injected delegates (the F1b
        /// semantics).</summary>
        internal void BindProductionLog()
        {
            LirRuntime.LogSink = LirRuntime.HostRuntimeLogSink;
            LirRuntime.ErrorLogSink = LirRuntime.HostErrorLogSink;
        }

        /// <summary>DEV-V6-02C: the skill account's file root is a host-owned composition fact.
        /// Absent injection (a host that never composed this feature) fails closed at Start —
        /// the feature never invents a persistence path.</summary>
        private static string RequireHostSettingsRoot()
        {
            var root = LirRuntime.HostSettingsRoot;
            if (root == null)
                throw new InvalidOperationException(
                    "LIR host composition did not bind the settings root (设置根) — the skill account persistence refuses to guess a path");
            return root();
        }

        /// <summary>DEV-V6-02C: the headless (no-screen) decision is a host-owned composition
        /// fact. Unbound injection degrades honestly to "not headless" (don't trim) — the same
        /// semantics as the original host field default false; the gate never invents a screen.</summary>
        private static bool IsHostHeadless()
        {
            var decision = LirRuntime.HostHeadlessDecision;
            return decision != null && decision();
        }

        /// <summary>The production key poll (chat focus / rebinding swallowing lives inside InputEx.GetKeyDown).</summary>
        private bool ReadReloadKeyDown()
        {
            KeyCode reloadKey = ControlsSettings.reload;
            if (reloadKey == KeyCode.None) return false;
            return InputEx.GetKeyDown(reloadKey);
        }

        /// <summary>
        /// DEV-V6-12（V6-T5 Q2 + 2026-09-18 追加裁决）：武装事务的唯一入口——只由
        /// 生命周期（Start）与设置开关（RefreshSwitches 的 on 侧）进入；工厂路径交付的
        /// 是惰性模块（武装只发生在生命周期内）。次序：权威面 → 画面面（决策门禁）→
        /// 技能面 → 最后一次登记移交；任一面失败即整体自拆且**事务就此终止**（其余面
        /// 不再武装、不进账——票面「半装失败：立即自拆，不留半装」按模块级读）。
        /// </summary>
        private void ArmPatches()
        {
            if (ShuttingDown || !Enabled) return;
            armTransactionFailed = false;
            // 上一套补丁集可能仍在架（上一轮撤销/边界反转遇过故障，patchSetInstalled 没有被
            // 清掉）：先尽力归一（守卫反转）。归一不成就**拒绝重复安装**——同一 Harmony 身份
            // 再把各面 Patch() 一遍会叠出第二份前缀，那是玩法语义变化，不是「重新武装」
            //（票面「设置变化后账与武装状态一致」「玩法与设置语义不变」）。fail-closed：
            // 保持原版语义 + 结构化留痕，账目与句柄都不动。
            if (patchSetInstalled)
            {
                if (UnpatchSelfGuarded("rearm-with-live-set")) patchSetInstalled = false;
                else
                {
                    StartGateDiagnostics = "rearm-refused-live-set";
                    LirRuntime.LogError("[Lir] 上一套补丁可能仍在架且反转未成功：拒绝重复安装（保持原版语义） diagnosticId=BUE-LIR-PATCH-007");
                    ClearPatchBookkeeping();
                    return;
                }
            }
            patchSetInstalled = true;
            patchReleaseLog = LirRuntime.HostRuntimeLogSink;
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
            }
            catch (Exception e)
            {
                // 无 Harmony 运行时/引擎漂移：构造即失败 = 事务零装成（无面在册）。
                StartGateDiagnostics = "patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                LirRuntime.LogError("[Lir] 补丁上下文构造失败（原位换弹不可用）: " + e.Message);
                SelfUnpatch("harmony-context-failed");
                return;
            }
            InstallCoreReloadPatches();
            if (armTransactionFailed) { SelfUnpatch("arm-transaction-aborted"); return; }
            // DEV-V5-06: 三面同代共存——HUD 面失败 = 整体互撤归零（04/05 同律，
            // 禁半装；headless 跳过画面面是决策不是失败）。
            InstallAmmoReserveHud();
            if (armTransactionFailed) { SelfUnpatch("arm-transaction-aborted"); return; }
            // DEV-V5-07: 第四面（技能分区）入列，互撤纪律不变。
            InstallReloadSkillSurface();
            if (armTransactionFailed) { SelfUnpatch("arm-transaction-aborted"); return; }
            PatchesInstalled = true;
            StartGateDiagnostics = string.Empty;
            LirRuntime.LogInfo("[Lir] 原位换弹补丁已安装（Harmony ID=" + LirRuntime.FeatureIdValue + "）");
            // DEV-V6-12（V6-T5 Q2 追加裁决）：武装 ⇒ 已登记。平台是正常拆除的唯一所有者；
            // 缺口袋或登记被拒=模块立即自拆（平台账外零补丁、零句柄）。
            HandOverPatchOwnership();
        }

        /// <summary>DEV-V5-06: 既有两压弹权威面的登记（无测试缝时与迁移前逐字
        /// 同形；有缝时按 04/05 installer 模式走——生命周期红测需四面同形驱动，
        /// 否则真 Patch() 先在 ECall 边界抛，画面面根本到不了）。拒装 = 记录结构化
        /// 诊断 + 置事务失败位（由 ArmPatches 整体自拆并终止事务，不在此处抛）。</summary>
        private void InstallCoreReloadPatches()
        {
            if (CorePatchesInstalled) return;
            try
            {
                var seam = CorePatchInstallerForTests;
                if (seam != null)
                {
                    if (!seam(typeof(UseableGunReceiveAttachMagazinePatch)))
                        throw new InvalidOperationException("lir-core patch refused: UseableGunReceiveAttachMagazinePatch");
                    if (!seam(typeof(ForceAddItemPatch)))
                        throw new InvalidOperationException("lir-core patch refused: ForceAddItemPatch");
                }
                else
                {
                    harmony.CreateClassProcessor(typeof(UseableGunReceiveAttachMagazinePatch)).Patch();
                    harmony.CreateClassProcessor(typeof(ForceAddItemPatch)).Patch();
                }
                CorePatchesInstalled = true;
                CoreStartGateDiagnostics = string.Empty;
                LirRuntime.LogInfo("[Lir] 权威压弹补丁已登记（Harmony ID=" + LirRuntime.FeatureIdValue + "）");
            }
            catch (Exception e)
            {
                CoreStartGateDiagnostics = "core-patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                NoteArmTransactionFailure(e);
                LirRuntime.LogError("[Lir] 权威压弹补丁安装失败（原位换弹不可用）: " + e.Message);
            }
        }

        /// <summary>DEV-V6-12: 一次武装事务的失败记账（汇总诊断文本与旧「InstallPatches
        /// 四面包裹」逐字同形，拒装点原样带出）+ 置失败位，由 ArmPatches 决定整体自拆与
        /// 终止（面方法本身不抛，免得半装态被异常路径掩盖）。</summary>
        private void NoteArmTransactionFailure(Exception error)
        {
            StartGateDiagnostics = "patch-install-failed: " + error.GetType().Name + ":" + error.Message;
            armTransactionFailed = true;
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
            if (IsHostHeadless())
            {
                HudStartGateDiagnostics = "ammo-hud-headless-not-armed";
                LirRuntime.LogInfo("[AmmoHud] U3DS headless：不武装弹药后备 HUD（Headless 决策门禁；压弹权威面不受影响）");
                return;
            }
            try
            {
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
            catch (Exception e)
            {
                // DEV-V6-12: 拒装 = 结构化诊断 + 事务失败位（整体互撤 + 事务终止由 ArmPatches 执行）。
                HudStartGateDiagnostics = "ammo-hud-patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                NoteArmTransactionFailure(e);
                LirRuntime.LogError("[AmmoHud] 弹药后备 HUD 补丁安装失败（HUD 面不可用）: " + e.Message);
            }
        }

        /// <summary>
        /// DEV-V6-12（V6-T5 Q2 + 2026-09-18 追加裁决）：把武装好的补丁交给启动口袋。
        /// 受理 ⇒ 拆除所有权移交平台账（此后模块在正常停止路径不再 UnpatchSelf）；
        /// 缺口袋 ⇒ 立即自拆（不自管补丁账）；拒绝/登记异常 ⇒ 立即自拆并留原因与码。
        /// 一个 Harmony 身份（本功能全部补丁面共用同一实例/ID）对应一枚句柄。
        /// </summary>
        private void HandOverPatchOwnership()
        {
            if (!AnyPatchArmed()) return;
            // 一代一条：本代际已登记过就不再登记（开关热装不新增账项、不新句柄——
            // 账不随开关累积，也不留已拆句柄；每代际容量与开关次数无关）。
            if (PatchTeardownDelegated) return;
            var pocket = patching;
            if (pocket == null)
            {
                SelfUnpatch("patch-pocket-missing");
                LirRuntime.LogError("[Lir] 启动口袋未提供补丁口：已武装补丁立即自拆（平台账外不留补丁） diagnosticId=BUE-LIR-PATCH-003");
                return;
            }
            var handle = new LirPatchHandle(LirRuntime.FeatureIdValue, ReleasePatchesFromPlatform,
                LirRuntime.HostRuntimeLogSink);
            FeaturePatchRegistrationResult result;
            try
            {
                result = pocket.Register(handle);
            }
            catch (Exception e)
            {
                LirRuntime.LogError("[Lir] 补丁口登记异常：" + e.GetType().Name
                    + " → 已武装补丁立即自拆（不留半装） diagnosticId=BUE-LIR-PATCH-002");
                SelfUnpatch("patch-pocket-register-faulted");
                return;
            }
            if (result.Registered)
            {
                PatchRegistration = handle;
                PatchTeardownDelegated = true;
                LirRuntime.LogInfo("[Lir] 补丁经启动口袋登记（拆除所有权移交平台账，句柄=" + handle.HarmonyId
                    + "，代际=" + result.LifecycleGeneration + "） diagnosticId=BUE-LIR-PATCH-001");
                return;
            }
            LirRuntime.LogError("[Lir] 补丁口登记被拒 reason=" + result.Reason + " pocketCode=" + result.DiagnosticId
                + "：已武装补丁立即自拆（不留半装） diagnosticId=BUE-LIR-PATCH-002");
            SelfUnpatch("patch-pocket-rejected");
        }

        /// <summary>
        /// DEV-V6-12：设置开关 off 的撤销路径（一代内动态拆装）。已登记 ⇒ 撤销经**本句柄的
        /// 同一条拆除动作**（不另起 UnpatchSelf，也不动登记：一代一条账项、句柄不释放——
        /// 账上既不累积新条目，也不留已拆句柄；平台边界的释放届时对未武装状态空转，不双拆）。
        /// 未登记（缺口袋/被拒/半装留下的防御态）走受守卫的账外自拆。
        /// </summary>
        private void DisarmPatchesForToggle(string reason)
        {
            var handle = PatchRegistration;
            if (handle == null)
            {
                SelfUnpatch(reason);
                return;
            }
            try
            {
                // 在册归零在拆除动作里（ReleasePatchesFromPlatform 的 finally 恒跑）——
                // 这里不再清一次，免得同一代际的呈现收两次。
                handle.RevokeCurrentPatches();
            }
            catch (Exception e)
            {
                // 设置路径不向宿主抛出：在册状态已归零、功能面按停用运行；故障如实留痕
                //（逆 JIT 故障的宿主环境与账释放同族，账那条腿仍走 BUE-LIFE-006 隔离）。
                LirRuntime.LogError("[Lir] 补丁撤销遇拆除故障（在册状态已归零，功能面按停用运行）reason=" + reason
                    + ": " + e.Message + " diagnosticId=BUE-LIR-PATCH-006");
            }
        }

        /// <summary>
        /// DEV-V6-12: 平台账的释放路径——句柄的 Dispose（停止/隔离边界），即登记受理后的
        /// **唯一**正常拆除点。拆除故障不在这里吞（Spec 轴口径）：它沿平台账户释放管线
        /// 浮出并隔离成 BUE-LIFE-006（DEV-V6-05 冻结语义），这也是句柄只在拆除动作返回后
        /// 落释放行的原因。在册状态照旧在 finally 归零——登记是功能开关，任何情况下都
        /// 收回，卡住的活钩子经适配器闸按拒绝答复而不是服务。
        /// </summary>
        private void ReleasePatchesFromPlatform()
        {
            try
            {
                // 开关热摘已把补丁撤下时，边界释放是对「当前无在架补丁」的空操作——同一
                // 套补丁不会被拆两次（不双拆），账目照样结清。判据是补丁集是否真在架
                // （patchSetInstalled），不是模块的在册开关（停机时已归零）。
                if (!patchSetInstalled)
                {
                    var sink = patchReleaseLog;
                    if (sink != null)
                    {
                        try { sink("[Lir] 平台边界释放：当前无在架补丁（开关热摘已撤），拆除动作按空操作返回"); }
                        catch (Exception) { }
                    }
                    return;
                }
                if (!RevertPatches())
                    throw new InvalidOperationException("harmony unpatch faulted (UnpatchSelf)");
                patchSetInstalled = false;
            }
            finally
            {
                ClearPatchBookkeeping();
            }
        }

        /// <summary>
        /// DEV-V6-12: 唯一的 Harmony 身份反转调用。false = 反转本身报错；这个结果如何
        /// 传播是各边界自己的冻结纪律——账绑定释放任其上浮账户管线（见
        /// ReleasePatchesFromPlatform），武装期自拆受守卫吞下（见 UnpatchSelfGuarded）。
        /// </summary>
        private bool RevertPatches()
        {
            var seam = PatchRevertForTests;
            if (seam != null) return seam();
            if (harmony != null) harmony.UnpatchSelf();
            return true;
        }

        /// <summary>
        /// DEV-V6-12: 模块自己的拆除，只应发生在平台账之外（半装失败、登记被拒、缺口袋、
        /// 停机未移交）——移交成功后这条路径在正常停止上绝不能再跑，否则就是双重拆除。
        /// 留痕行让红测可以把「没跑」钉成事实。
        /// </summary>
        private void SelfUnpatch(string reason)
        {
            var owed = patchSetInstalled || AnyPatchArmed();
            if (owed && UnpatchSelfGuarded(reason)) patchSetInstalled = false;
            ClearPatchBookkeeping();
            if (owed)
                LirRuntime.LogInfo("[Lir] 换弹补丁自拆（平台账外）reason=" + reason + " diagnosticId=BUE-LIR-PATCH-005");
        }

        /// <summary>
        /// 04 实证教训：撤装反转已装 IL 会在无 Unity 运行时的进程触发再 JIT 异常——
        /// 这条路径在平台账之外运行（它撤销的是刚失败的半装），绝不向模块自己的
        /// Start/设置回调抛出：在册状态才是功能开关，任何情况下都归零，而失败的反转
        /// 在日志里保持可观察。账绑定释放（ReleasePatchesFromPlatform）刻意不吞——
        /// 那里账户管线才是隔离点（BUE-LIFE-006）。
        /// </summary>
        private bool UnpatchSelfGuarded(string reason)
        {
            try
            {
                if (!RevertPatches())
                {
                    LirRuntime.LogError("[Lir] 补丁撤销失败（登记已强制收回，功能面按停用运行）reason=" + reason);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                LirRuntime.LogError("[Lir] 补丁撤销异常（登记已强制收回，功能面按停用运行）reason=" + reason + ": " + e.Message);
                return false;
            }
        }

        /// <summary>本代际是否还有补丁面在册（半装=无面在册但可能已装了一部分）。</summary>
        private bool AnyPatchArmed()
        {
            return CorePatchesInstalled || PatchesInstalled || HudPatchInstalled || SkillPatchInstalled;
        }

        /// <summary>The one revocation of every armed face's registration
        /// (05-06/05-07 在册同步律): UnpatchSelf is by Harmony ID, so one
        /// removal revokes all faces — the bookkeeping follows it, never the
        /// other way round.</summary>
        private void ClearPatchBookkeeping()
        {
            PatchesInstalled = false;
            CorePatchesInstalled = false;
            ActiveModule = null;
            // DEV-V5-06: 注销=不画（登记交还 + 在枪上的残留读数即时收）。
            HudPatchInstalled = false;
            AmmoReserveHudAdapter.RevokeAll();
            // DEV-V5-07: 技能分区面同律注销；等级确认镜像=连接态，随面收。
            SkillPatchInstalled = false;
            ReloadSkillDashboardAdapter.Unregister();
            ReloadSkillLevelMirror.Clear();
            // 注意：patchSetInstalled 不在这里清——它跟的是「Harmony 补丁是否还装着」的
            // 事实，只由真的跑过反转动作清掉（见 ReleasePatchesFromPlatform/SelfUnpatch）。
        }

        /// <summary>
        /// DEV-V6-12（V6-T5 Q2 追加裁决）: the stop-boundary patch handoff.
        /// 登记受理 ⇒ 平台是正常拆除的唯一所有者（账在本方法返回后于 CompleteStop
        /// 逆序释放句柄），模块在这里不得自拆——拆两次正是本票要消除的缺陷。没有移交
        /// 记录 ⇒ 补丁仍归模块，fail-closed 自拆（武装期的各腿已自拆过，这里是防御路径）。
        /// </summary>
        private void ReleasePatchOwnership()
        {
            var armed = AnyPatchArmed() || patchSetInstalled;
            if (armed && !PatchTeardownDelegated)
            {
                SelfUnpatch("stop-without-handover");
            }
            else
            {
                if (armed)
                {
                    LirRuntime.LogInfo("[Lir] 补丁拆除在平台账上：模块停止不自行拆除（代际=" + LifecycleGeneration + "）");
                }
                // The functional switch goes off here either way — the patches
                // themselves are the platform's business from now on.
                ClearPatchBookkeeping();
            }
            PatchTeardownDelegated = false;
            PatchRegistration = null;
        }

        // ── DEV-V5-07：换弹技能面 ────────────────────────────────────────

        /// <summary>分区面登记（第四画面面；headless 不武装=决策非失败；
        /// 表面 A 探测接不上=降级表面 B 在注册侧接管，本面静默缺席零抛）。</summary>
        private void InstallReloadSkillSurface()
        {
            if (SkillPatchInstalled) return;
            if (IsHostHeadless())
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
            try
            {
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
            catch (Exception e)
            {
                // DEV-V6-12: 拒装 = 结构化诊断 + 事务失败位（整体互撤 + 事务终止由 ArmPatches 执行）。
                SkillStartGateDiagnostics = "reload-skill-patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                NoteArmTransactionFailure(e);
                LirRuntime.LogError("[ReloadSkill] 换弹技能分区补丁安装失败（分区面不可用）: " + e.Message);
            }
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
