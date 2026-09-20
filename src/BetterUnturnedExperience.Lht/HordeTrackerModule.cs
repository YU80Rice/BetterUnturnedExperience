using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the horde-tracker official feature module (更好的尸潮播报).
    /// One FeatureId composing the internal dual pieces (spec「内部双件」：
    /// HordeTrackingModule 服务器权威 + HordePresentationAdapter HUD 表现).
    /// Owns:
    ///   - the enabled toggle is the ONE persisted setting (DEV-V3-06: the
    ///     host-owned SettingsRuntime behind the injected scoped view,
    ///     ClientLocal; off = FULL stop — tracking unsubscribed, /horde
    ///     deregistered, client receiver gated, HUD dark, channel
    ///     unregistered; on = re-arm);
    ///   - the beacon patches live under the Harmony id = FeatureId (install
    ///     at arm, UnpatchSelf at stop — Stop revokes ONLY its own ID);
    ///   - the frame work rides the frozen HostTick seam (no self-built
    ///     Unity Update pump): mailbox drain → tracking cycle → 10Hz HUD;
    ///   - the network shape rides the frozen IBueNetworkApi seam: register
    ///     the FeatureId channel, subscribe FromServer (client), broadcast
    ///     via the session-driven SendToClients (server);
    ///   - presentation = Available / HeadlessOnly by the batch-mode fact
    ///     (U3DS: the feature is Available, the HUD is HeadlessOnly).
    /// </summary>
    internal sealed class HordeTrackerModule : IFeatureModule
    {
        private const string EnabledSettingId = "hordetracker.enabled";
        private Harmony harmony;
        private readonly List<IDisposable> subscriptionHandles = new List<IDisposable>();
        private bool networkBound;
        private bool armed;
        private float presentationClock;

        // ── DEV-V6-13（V6-T5 Q2 + 2026-09-18 追加裁决）：补丁所有权平台化 ──
        // 武装好的补丁集（一个 Harmony 身份：信标权威面 + 可选 HUD 画面面）经启动口袋的
        // IFeaturePatching 登记进既有资源账；受理即移交拆除所有权（平台是正常拆除的唯一
        // 所有者），一代恰一条账项。缺口袋/拒绝/半装=立即自拆，平台账外零补丁。
        private IFeaturePatching patching;
        // 「补丁集是否真在架」的事实（Harmony 还挂着这套补丁）：只由真的跑过反转动作清掉
        //（ReleasePatchesFromPlatform / SelfUnpatch），与模块的在册开关分家。
        private bool patchSetInstalled;
        private bool armTransactionFailed;
        // 武装期捕获的日志口：停止边界之后模块可能已解绑 sink，释放行仍必须可见。
        private Action<string> patchReleaseLog;

        /// <summary>宿主测试缝（既有家族，LIT/LIR 同形）：信标（权威）面的安装器替身——
        //  真机 Patch() 在宿主进程装不上（引擎 ECall 规则），替身让「哪一面武装」机器可判。</summary>
        internal static Func<Type, bool> BeaconPatchInstallerForTests;

        /// <summary>宿主测试缝：HUD（画面）面的安装器替身（同上，画面面的决策门禁另有专测）。</summary>
        internal static Func<Type, bool> HudPatchInstallerForTests;

        /// <summary>宿主测试缝：唯一的 Harmony 身份反转调用替身（false=反转本身报错）。</summary>
        internal static Func<bool> PatchRevertForTests;

        // DEV-V3-06: the module no longer constructs or holds a
        // SettingsRuntime — the host composes the ONE per-feature runtime
        // from the registration's settings facet and injects the scoped
        // view (IFeatureBootstrap.Settings); schema authority stays with
        // the feature (CreateSettingsDescriptors), file layout unchanged.
        internal HordeTrackerModule()
            : this(new FeatureId(LhtRuntime.FeatureIdValue))
        {
        }

        internal HordeTrackerModule(FeatureId feature)
        {
            Feature = feature;
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

        /// <summary>The patch-facing module handle (the LIT ActiveModule pattern): live only while the patches are.</summary>
        internal static HordeTrackerModule ActiveModule { get; private set; }

        internal bool Enabled { get; private set; }
        internal bool Started { get; private set; }
        internal bool ShuttingDown { get; private set; }

        /// <summary>信标（权威）补丁面在册——权威尸潮计数在无画面环境同样武装。</summary>
        internal bool BeaconPatchesInstalled { get; private set; }

        /// <summary>HUD（画面）补丁面在册——无画面=不武装（决策门禁，诚实诊断）。</summary>
        internal bool HudPatchesInstalled { get; private set; }

        /// <summary>补丁集是否真在架（Harmony 挂着这套补丁且反转动作没跑过）——与「模块在册
        /// 开关」分家的那个事实；开关热摘与平台边界释放都使它与在册状态一起归零。</summary>
        internal bool PatchesInstalled { get { return patchSetInstalled; } }

        /// <summary>画面面的诚实诊断（无画面决策 / 拒装原因）——决策门禁不是异常门禁。</summary>
        internal string HudStartGateDiagnostics { get; private set; } = string.Empty;

        /// <summary>本代际经启动口袋受理的补丁句柄（登记受理后平台账是唯一正常拆除点）。</summary>
        internal LhtPatchHandle PatchRegistration { get; private set; }

        /// <summary>拆除所有权已移交平台账（一代只移交一次；开关热装不再重复登记）。</summary>
        internal bool PatchTeardownDelegated { get; private set; }

        /// <summary>The Harmony instance id the patches live under (= the FeatureId; own ID only).</summary>
        internal string PatchHarmonyId { get; private set; }
        internal string StartGateDiagnostics { get; private set; } = string.Empty;

        /// <summary>The server-authority internal piece.</summary>
        internal HordeTrackingModule Tracking { get; private set; }

        /// <summary>The client receive path (gate + mailbox + state).</summary>
        internal HordeClientReceiver Receiver { get; } = new HordeClientReceiver();

        /// <summary>The HUD presentation internal piece.</summary>
        internal HordePresentationAdapter Presentation { get; private set; }

        internal IHordeTrackingAuthority Authority { get; private set; }
        internal IHordeHudSurface HudSurface { get; private set; }
        internal IBueNetworkApi Network { get; private set; }
        internal ulong LifecycleGeneration { get; private set; }

        /// <summary>Host-test seam: overrides the engine authority composition.</summary>
        internal Func<IHordeTrackingAuthority> AuthorityFactoryForTests { get; set; }

        /// <summary>Host-test seam: overrides the HUD surface composition.</summary>
        internal Func<IHordeHudSurface> HudSurfaceFactoryForTests { get; set; }

        /// <summary>Host-test seam: overrides the client-UI gate (production reads Application.isBatchMode — unavailable in host tests).</summary>
        internal Func<bool> CanUseClientUiForTests { get; set; }

        /// <summary>batch-mode 是加载期不可变能力事实（U3DS UI 隔离）；表现状态只降 HUD，不伤追踪。
        /// The Application read lives in a NoInlining method: host tests never
        /// execute it, and Mono JIT-resolves engine ECalls at method-compile
        /// time — an inlined Unity call would throw in the test process even
        /// on the probe branch (the same discipline that keeps every other
        /// test-touched path Unity-free).</summary>
        private bool CanUseClientUi
        {
            get { return CanUseClientUiForTests != null ? CanUseClientUiForTests() : ReadBatchMode(); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool ReadBatchMode()
        {
            return !Application.isBatchMode;
        }

        /// <summary>The feature is Available in every environment; the HUD presentation is HeadlessOnly under batch mode.</summary>
        internal FeaturePresentationState PresentationState
        {
            get { return CanUseClientUi ? FeaturePresentationState.Available : FeaturePresentationState.HeadlessOnly; }
        }

        public FeatureStartResult Start(IFeatureBootstrap bootstrap)
        {
            if (bootstrap == null) throw new ArgumentNullException(nameof(bootstrap));
            if (bootstrap.Events == null || bootstrap.Network == null)
                throw new ArgumentException("the host bootstrap must compose the event subscriber view and the network API (never null)", nameof(bootstrap));
            Network = bootstrap.Network;
            LifecycleGeneration = bootstrap.LifecycleGeneration;
            // DEV-V6-12 同律（工厂复用实例）：Start 清停机位——停用→再启用必须能重新武装
            //（工厂路径交付接线实例，宿主再启用经同一条工厂取回它）。
            ShuttingDown = false;
            // DEV-V4-09：Stop 解绑生产日志缝（插件卸载卫生），本代际 Start 重绑
            // （与 Lit 同律——停用→再启用后模块诊断不得失明）。
            BindProductionLog();
            // DEV-V3-06 official first consumption: the FULL stop switch
            // rides the host-injected scoped view (the tracking closure
            // reads Enabled from it — the single truth).
            AttachSettingsView(bootstrap.Settings); // DEV-V3-06: bind + read before composing trackers
            Started = true;
            // DEV-V6-13（V6-T5 Q2 + 2026-09-18 追加裁决）：武装与登记都只发生在生命周期内
            // ——工厂路径交付惰性模块；启动口袋的补丁口在这里捕获，代际开启、补丁武装、
            // 经启动口袋登记三件事按序完成（缺口袋/拒绝/半装即立即自拆，平台账外零补丁）。
            patching = bootstrap.Patching;
            PatchTeardownDelegated = false;
            PatchRegistration = null;

            Authority = AuthorityFactoryForTests != null ? AuthorityFactoryForTests() : HordeProductionAuthority.Instance;
            if (Authority == null) throw new InvalidOperationException("the LHT authority must never compose to null");
            HudSurface = HudSurfaceFactoryForTests != null ? HudSurfaceFactoryForTests() : new PlayerLifeHudSurface();
            Tracking = new HordeTrackingModule(Authority, new DefaultHordeTrackingPolicy(), Network, () => Enabled);
            Presentation = new HordePresentationAdapter(HudSurface);

            // The frozen clock seam: the module owns its subscription and the
            // host drops it at the stop boundary (UnsubscribeAll after Stop
            // returns — the DEV-V2-21/22 handoff).
            bootstrap.Events.Subscribe<HostTick>(OnHostTick);

            if (Enabled && !ShuttingDown)
            {
                Arm();
            }
            LhtRuntime.LogInfo("[Lht] 模块已启动（宿主 bootstrap：功能代际=" + bootstrap.LifecycleGeneration + "，表现=" + PresentationState + "）");
            return new FeatureStartResult(true, FrameworkErrorCode.None, "BUE-LHT-START");
        }

        public void Stop(FeatureStopReason reason)
        {
            if (ShuttingDown) return;
            ShuttingDown = true;
            Started = false;

            if (armed) DisarmAtGenerationBoundary();
            // DEV-V6-13：登记受理 ⇒ 拆除由平台账在停止边界执行（模块不得自拆一次）；
            // 未移交（武装期失败/从未武装）走 fail-closed 自拆的防御路径。
            else ReleasePatchOwnership();
            Network = null;
            Tracking = null;
            Presentation = null;
            HudSurface = null;
            Authority = null;
            HordeCommandLogic.ResetForGeneration();
            LhtRuntime.LogInfo("[Lht] 模块停止：网络已注销、追踪已退订、补丁已撤（原生回退）");
            LhtRuntime.LogSink = null;
            LhtRuntime.ErrorLogSink = null;
        }

        /// <summary>
        /// The panel toggle's effect: reads the authoritative setting and
        /// applies the FULL stop / re-arm immediately (off = tracking
        /// unsubscribed, /horde gone, receiver gated, HUD dark, channel
        /// unregistered; on = everything re-arms).
        /// DEV-V6-13（V6-T5 Q2 追加裁决）：补丁所有权移交平台后，热摘经**句柄的同一条拆除
        /// 动作**撤销（不另起模块自拆），热装用同一身份重新武装（不新增账项、不换句柄）。
        /// </summary>
        internal void RefreshSwitches()
        {
            Enabled = ReadToggle();
            if (!Enabled || ShuttingDown)
            {
                if (armed) DisarmForToggle();
                return;
            }
            if (!armed)
            {
                Arm();
                return;
            }
            // DEV-V6-13（R2 Spec 收口）：开关的 on 侧是幂等的重臂点——已在运行但补丁面不在册
            //（上一轮武装被拒/装失败留下的态，含「旧集可能仍在架」的 patchSetInstalled）
            // 时重新尝试武装，免得「设置显示开启而补丁不在架」无从收敛（故障消失后一次设置
            // 事件即完成重臂：先归一旧集、再装两面）。在架时不重复武装（不叠第二份前缀）。
            if (!AnyPatchArmed()) ArmPatches();
        }

        /// <summary>
        /// The ONE host-frame entry (subscribed to the frozen HostTick seam).
        /// Order mirrors the old plugin Update: mailbox drain → tracking
        /// cycle (command flush + broadcast) → 10Hz HUD. Never throws into
        /// the host clock chain.
        /// </summary>
        internal void OnHostTick(HostTick tick)
        {
            if (!Started || ShuttingDown) return;
            try
            {
                presentationClock += tick.DeltaTime;
                Receiver.DrainToState();
                if (Enabled && armed)
                {
                    Tracking?.Tick(tick.DeltaTime);
                    if (CanUseClientUi)
                    {
                        Presentation?.Tick(presentationClock);
                        Presentation?.DrainDisconnectReset();
                    }
                }
            }
            catch (Exception error)
            {
                LhtRuntime.LogError("[Lht] 帧驱动异常（已隔离）: " + error.Message);
            }
        }

        /// <summary>The production log binding (registration/start time): domain sinks ←
        /// the host-injected delegates (DEV-V6-02D, V6-T2 硬项拆法——the horde domain no
        /// longer names the host's log type; the host owns the route). Unbound injection
        /// propagates honestly: the sinks stay null and LogInfo/LogError swallow (the
        /// LhtRuntime 未绑定即吞 contract). Stop clears the sinks and a re-Start rebinds
        /// from the SAME injected delegates (the F1b semantics).</summary>
        internal void BindProductionLog()
        {
            LhtRuntime.LogSink = LhtRuntime.HostRuntimeLogSink;
            LhtRuntime.ErrorLogSink = LhtRuntime.HostErrorLogSink;
        }

        /// <summary>The enabled activation: network bind → engine subscriptions → patches → HUD surface.</summary>
        private void Arm()
        {
            if (armed) return;
            armed = true;
            BindNetwork();
            Tracking.Activate();
            ArmPatches();
            if (CanUseClientUi)
            {
                HudSurface.Install();
            }
        }

        /// <summary>The full stop riding a GENERATION boundary (Stop): the receive gate closes
        /// FIRST (a late callback can never write state after the mailbox reset), then tracking,
        /// state, patches, HUD, network. DEV-V6-13: the patch step here hands the teardown to the
        /// platform account (registered ⇒ the module does not unpatch — the account releases at
        /// this boundary).</summary>
        private void DisarmAtGenerationBoundary()
        {
            DisarmCore(ReleasePatchOwnership);
        }

        /// <summary>The full stop riding the SETTINGS switch (off = native fallback now, not a
        /// generation boundary). DEV-V6-13: the patch step rides the registered handle's ONE
        /// teardown action (no second UnpatchSelf; the account entry stays — one per generation).</summary>
        private void DisarmForToggle()
        {
            DisarmCore(() => DisarmPatchesForToggle("settings-off"));
        }

        /// <summary>The shared full-stop body (the old OnDestroy teardown shape): the receive
        /// gate closes FIRST (a late callback can never write state after the mailbox reset),
        /// then tracking, state, patches, HUD, network. The patch step is the caller's business —
        /// the two boundaries own different patch semantics (DEV-V6-13).</summary>
        private void DisarmCore(System.Action releasePatches)
        {
            if (!armed) return;
            armed = false;
            Receiver.Close();
            Receiver.DrainToState();
            Receiver.Reset();
            Tracking.Deactivate();
            HordeStateTracker.Clear();
            releasePatches();
            HudSurface.Uninstall();
            Presentation.Reset();
            UnbindNetwork();
        }

        private bool BindNetwork()
        {
            if (networkBound) return true;
            var registration = Network.RegisterChannel(Feature, new ContractVersion(2, 0), 1);
            if (!registration.Accepted)
            {
                StartGateDiagnostics = "channel-register-rejected: " + registration.Reason;
                LhtRuntime.LogError("[HordeNet] 频道注册被拒绝（reason=" + registration.Reason + "）——本地追踪与 HUD 仍可用，广播不可用");
                return false;
            }
            IDisposable fromServer = null;
            try
            {
                fromServer = Network.Subscribe(Feature, ChannelDirection.FromServer, Receiver.HandleFrame);
            }
            catch (Exception error)
            {
                // 半注册回滚：已成功的注册撤销，零残留。
                try { Network.UnregisterChannel(Feature); } catch (Exception) { }
                StartGateDiagnostics = "subscribe-failed: " + error.GetType().Name;
                LhtRuntime.LogError("[HordeNet] 入站订阅失败，已回滚注册（零残留）: " + error.Message);
                return false;
            }
            subscriptionHandles.Add(fromServer);
            Receiver.Open();
            networkBound = true;
            LhtRuntime.LogInfo("[HordeNet] 已注册 BUE 命名频道=" + Feature.Value + "（FeatureId 即频道身份，旧频道退役）");
            return true;
        }

        private void UnbindNetwork()
        {
            if (!networkBound) return;
            Receiver.Close();
            for (var i = 0; i < subscriptionHandles.Count; i++)
            {
                try { subscriptionHandles[i]?.Dispose(); } catch (Exception) { }
            }
            subscriptionHandles.Clear();
            try { Network.UnregisterChannel(Feature); } catch (Exception) { }
            networkBound = false;
        }

        /// <summary>
        /// DEV-V6-13（V6-T5 Q2 + 2026-09-18 追加裁决）：武装事务的唯一入口——只由生命周期
        /// （Start 的 Arm）与设置开关（RefreshSwitches 的 on 侧）进入；工厂路径交付的是惰性
        /// 模块（武装只发生在生命周期内）。次序：权威信标面 → 画面面（决策门禁）→ 最后一次
        /// 登记移交；任一面失败即整体自拆且**事务就此终止**（其余面不再武装、不进账——票面
        /// 「半装失败：立即自拆，不留半装」按模块级读）。
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
                    LhtRuntime.LogError("[Lht] 上一套补丁可能仍在架且反转未成功：拒绝重复安装（保持原版语义） diagnosticId=BUE-LHT-PATCH-007");
                    ClearPatchBookkeeping();
                    return;
                }
            }
            patchSetInstalled = true;
            patchReleaseLog = LhtRuntime.HostRuntimeLogSink;
            try
            {
                if (harmony == null)
                {
                    harmony = new Harmony(LhtRuntime.FeatureIdValue);
                    PatchHarmonyId = LhtRuntime.FeatureIdValue;
                }
                // The handle goes live BEFORE Patch(): no game call can
                // interleave (the install runs on the game thread itself).
                ActiveModule = this;
            }
            catch (Exception e)
            {
                // 无 Harmony 运行时/引擎漂移：构造即失败 = 事务零装成（无面在册）。
                StartGateDiagnostics = "patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                LhtRuntime.LogError("[Lht] 补丁上下文构造失败（尸潮播报不可用）: " + e.Message);
                SelfUnpatch("harmony-context-failed");
                return;
            }
            InstallBeaconPatches();
            if (armTransactionFailed) { SelfUnpatch("arm-transaction-aborted"); return; }
            InstallHudPatches();
            if (armTransactionFailed) { SelfUnpatch("arm-transaction-aborted"); return; }
            StartGateDiagnostics = string.Empty;
            LhtRuntime.LogInfo("[Lht] 尸潮补丁已安装（Harmony ID=" + LhtRuntime.FeatureIdValue + "）");
            // DEV-V6-13（V6-T5 Q2 追加裁决）：武装 ⇒ 已登记。平台是正常拆除的唯一所有者；
            // 缺口袋或登记被拒=模块立即自拆（平台账外零补丁、零句柄）。
            HandOverPatchOwnership();
        }

        /// <summary>DEV-V6-13：信标（权威）补丁面的登记——真机 Path() 在宿主测试进程装不上
        /// （引擎 ECall 规则），安装器缝让「哪一面武装」机器可判；拒装=结构化诊断 + 事务失败位
        /// （由 ArmPatches 整体自拆并终止事务，不在此处抛）。</summary>
        private void InstallBeaconPatches()
        {
            if (BeaconPatchesInstalled) return;
            try
            {
                var seam = BeaconPatchInstallerForTests;
                if (seam != null)
                {
                    if (!seam(typeof(BeaconCounterPatches)))
                        throw new InvalidOperationException("beacon patch refused: BeaconCounterPatches");
                }
                else
                {
                    harmony.CreateClassProcessor(typeof(BeaconCounterPatches)).Patch();
                }
                BeaconPatchesInstalled = true;
                LhtRuntime.LogInfo("[Lht] 信标补丁已安装（权威面，Harmony ID=" + LhtRuntime.FeatureIdValue + "）");
            }
            catch (Exception e)
            {
                BeaconPatchesInstalled = false;
                NoteArmTransactionFailure(e);
                LhtRuntime.LogError("[Lht] 信标补丁安装失败（尸潮播报不可用）: " + e.Message);
            }
        }

        /// <summary>DEV-V6-13：HUD（画面）补丁面的登记——无画面=决策门禁（诚实诊断，权威面
        /// 不受影响）；装不上=结构化诊断 + 事务失败位（整体互撤）。</summary>
        private void InstallHudPatches()
        {
            if (HudPatchesInstalled) return;
            if (!CanUseClientUi)
            {
                HudStartGateDiagnostics = "hud-patch-headless-not-armed";
                LhtRuntime.LogInfo("[Lht] U3DS headless：不武装 HUD 补丁（Headless 决策门禁；信标权威面不受影响）");
                return;
            }
            try
            {
                var seam = HudPatchInstallerForTests;
                if (seam != null)
                {
                    if (!seam(PlayerLifeHudSurface.PatchType))
                        throw new InvalidOperationException("hud patch refused: PlayerLifeUiHudPatches");
                }
                else
                {
                    harmony.CreateClassProcessor(PlayerLifeHudSurface.PatchType).Patch();
                }
                HudPatchesInstalled = true;
                HudStartGateDiagnostics = string.Empty;
                LhtRuntime.LogInfo("[Lht] HUD 补丁已安装（画面面，Harmony ID=" + LhtRuntime.FeatureIdValue + "）");
            }
            catch (Exception e)
            {
                HudPatchesInstalled = false;
                HudStartGateDiagnostics = "hud-patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                NoteArmTransactionFailure(e);
                LhtRuntime.LogError("[Lht] HUD 补丁安装失败（HUD 面不可用）: " + e.Message);
            }
        }

        /// <summary>DEV-V6-13: 一次武装事务的失败记账（汇总诊断文本与原「InstallPatches
        /// try/catch」逐字同形 + 置失败位，由 ArmPatches 决定整体自拆与终止）。</summary>
        private void NoteArmTransactionFailure(Exception error)
        {
            StartGateDiagnostics = "patch-install-failed: " + error.GetType().Name + ":" + error.Message;
            armTransactionFailed = true;
        }

        /// <summary>
        /// DEV-V6-13（V6-T5 Q2 + 2026-09-18 追加裁决）：把武装好的补丁交给启动口袋。
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
                LhtRuntime.LogError("[Lht] 启动口袋未提供补丁口：已武装补丁立即自拆（平台账外不留补丁） diagnosticId=BUE-LHT-PATCH-003");
                return;
            }
            var handle = new LhtPatchHandle(LhtRuntime.FeatureIdValue, ReleasePatchesFromPlatform,
                LhtRuntime.HostRuntimeLogSink);
            FeaturePatchRegistrationResult result;
            try
            {
                result = pocket.Register(handle);
            }
            catch (Exception e)
            {
                LhtRuntime.LogError("[Lht] 补丁口登记异常：" + e.GetType().Name
                    + " → 已武装补丁立即自拆（不留半装） diagnosticId=BUE-LHT-PATCH-002");
                SelfUnpatch("patch-pocket-register-faulted");
                return;
            }
            if (result.Registered)
            {
                PatchRegistration = handle;
                PatchTeardownDelegated = true;
                LhtRuntime.LogInfo("[Lht] 补丁经启动口袋登记（拆除所有权移交平台账，句柄=" + handle.HarmonyId
                    + "，代际=" + result.LifecycleGeneration + "） diagnosticId=BUE-LHT-PATCH-001");
                return;
            }
            LhtRuntime.LogError("[Lht] 补丁口登记被拒 reason=" + result.Reason + " pocketCode=" + result.DiagnosticId
                + "：已武装补丁立即自拆（不留半装） diagnosticId=BUE-LHT-PATCH-002");
            SelfUnpatch("patch-pocket-rejected");
        }

        /// <summary>
        /// DEV-V6-13：设置开关 off 的撤销路径（一代内可重复全停）。已登记 ⇒ 撤销经**本句柄的
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
                var wasInstalled = patchSetInstalled;
                handle.RevokeCurrentPatches();
                if (wasInstalled)
                    LhtRuntime.LogInfo("[Lht] 尸潮补丁已撤销（原生回退，仅撤自身 Harmony ID；登记仍归平台账）");
            }
            catch (Exception e)
            {
                // 设置路径不向宿主抛出：在册状态已归零、功能面按停用运行；故障如实留痕
                //（逆 JIT 故障的宿主环境与账释放同族，账那条腿仍走 BUE-LIFE-006 隔离）。
                LhtRuntime.LogError("[Lht] 补丁撤销遇拆除故障（在册状态已归零，功能面按停用运行）reason=" + reason
                    + ": " + e.Message + " diagnosticId=BUE-LHT-PATCH-006");
            }
        }

        /// <summary>
        /// DEV-V6-13: 平台账的释放路径——句柄的 Dispose（停止/隔离边界），即登记受理后的
        /// **唯一**正常拆除点。拆除故障不在这里吞（DEV-V6-05 冻结口径）：它沿平台账户释放
        /// 管线浮出并隔离成 BUE-LIFE-006，这也是句柄只在拆除动作返回后落释放行的原因。
        /// 在册状态照旧在 finally 归零——登记是功能开关，任何情况下都收回。
        /// </summary>
        private void ReleasePatchesFromPlatform()
        {
            try
            {
                // 开关热摘已把补丁撤下时，边界释放是对「当前无在架补丁」的空操作——同一
                // 套补丁不会被拆两次（不双拆），账目照样结清。判据是补丁集是否真在架
                //（patchSetInstalled），不是模块的在册开关（停机时已归零）。
                if (!patchSetInstalled)
                {
                    var sink = patchReleaseLog;
                    if (sink != null)
                    {
                        try { sink("[Lht] 平台边界释放：当前无在架补丁（开关热摘已撤），拆除动作按空操作返回"); }
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
        /// DEV-V6-13: 唯一的 Harmony 身份反转调用。false = 反转本身报错；这个结果如何
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
        /// DEV-V6-13: 模块自己的拆除，只应发生在平台账之外（半装失败、登记被拒、缺口袋、
        /// 停机未移交）——移交成功后这条路径在正常停止上绝不能再跑，否则就是双重拆除。
        /// 留痕行让红测可以把「没跑」钉成事实。
        /// </summary>
        private void SelfUnpatch(string reason)
        {
            var owed = patchSetInstalled || AnyPatchArmed();
            if (owed && UnpatchSelfGuarded(reason)) patchSetInstalled = false;
            ClearPatchBookkeeping();
            if (owed)
                LhtRuntime.LogInfo("[Lht] 尸潮补丁自拆（平台账外）reason=" + reason + " diagnosticId=BUE-LHT-PATCH-005");
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
                    LhtRuntime.LogError("[Lht] 补丁撤销失败（登记已强制收回，功能面按停用运行）reason=" + reason);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                LhtRuntime.LogError("[Lht] 补丁撤销异常（登记已强制收回，功能面按停用运行）reason=" + reason + ": " + e.Message);
                return false;
            }
        }

        /// <summary>本代际是否还有补丁面在册（半装=无面在册但可能已装了一部分）。</summary>
        private bool AnyPatchArmed()
        {
            return BeaconPatchesInstalled || HudPatchesInstalled;
        }

        /// <summary>The one revocation of every armed face's registration
        /// (05-06/05-07 在册同步律): UnpatchSelf is by Harmony ID, so one
        /// removal revokes all faces — the bookkeeping follows it, never the
        /// other way round.</summary>
        private void ClearPatchBookkeeping()
        {
            BeaconPatchesInstalled = false;
            HudPatchesInstalled = false;
            ActiveModule = null;
            // 注意：patchSetInstalled 不在这里清——它跟的是「Harmony 补丁是否还装着」的
            // 事实，只由真的跑过反转动作清掉（见 ReleasePatchesFromPlatform/SelfUnpatch）。
        }

        /// <summary>
        /// DEV-V6-13（V6-T5 Q2 追加裁决）: the generation-boundary patch handoff.
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
                    LhtRuntime.LogInfo("[Lht] 补丁拆除在平台账上：模块停止不自行拆除（代际=" + LifecycleGeneration + "）");
                }
                // The functional switch goes off here either way — the patches
                // themselves are the platform's business from now on.
                ClearPatchBookkeeping();
            }
            PatchTeardownDelegated = false;
            PatchRegistration = null;
        }

        /// <summary>The patch-facing static entry (BeaconCounterPatches): no live
        /// module → silent release; a live module → the context guard.</summary>
        internal static void OnBeaconCounterPatched(InteractableBeacon beacon)
        {
            var module = ActiveModule;
            module?.Tracking.OnBeaconCounterChanged(beacon);
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
