using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;
using BetterUnturnedExperience.Core.Settings;
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
    ///   - the enabled toggle is the ONE persisted setting (SettingsRuntime,
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

        internal HordeTrackerModule(string settingsRoot)
            : this(new FeatureId(LhtRuntime.FeatureIdValue), new FileSettingsPersistence(settingsRoot))
        {
        }

        internal HordeTrackerModule(FeatureId feature, ISettingsPersistence persistence)
        {
            Feature = feature;
            if (persistence == null) throw new ArgumentNullException(nameof(persistence));
            Settings = new SettingsRuntime(feature, new[] { ToggleDescriptor(feature) }, persistence);
            Enabled = ReadToggle();
        }

        internal FeatureId Feature { get; }
        internal SettingsRuntime Settings { get; }

        /// <summary>The patch-facing module handle (the LIT ActiveModule pattern): live only while the patches are.</summary>
        internal static HordeTrackerModule ActiveModule { get; private set; }

        internal bool Enabled { get; private set; }
        internal bool Started { get; private set; }
        internal bool ShuttingDown { get; private set; }
        internal bool PatchesInstalled { get; private set; }

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
            Started = true;

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

            if (armed) Disarm();
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
        /// </summary>
        internal void RefreshSwitches()
        {
            Enabled = ReadToggle();
            if (!Enabled || ShuttingDown)
            {
                if (armed) Disarm();
                return;
            }
            if (!armed) Arm();
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

        /// <summary>The production log binding (registration time): domain sinks → BueRuntimeLog channels.</summary>
        internal void BindProductionLog()
        {
            LhtRuntime.LogSink = line => BetterUnturnedExperience.Plugin.BueRuntimeLog.Runtime(line);
            LhtRuntime.ErrorLogSink = line => BetterUnturnedExperience.Plugin.BueRuntimeLog.Error(line);
        }

        /// <summary>The enabled activation: network bind → engine subscriptions → patches → HUD surface.</summary>
        private void Arm()
        {
            if (armed) return;
            armed = true;
            BindNetwork();
            Tracking.Activate();
            InstallPatches();
            if (CanUseClientUi)
            {
                HudSurface.Install();
            }
        }

        /// <summary>The full stop (the old OnDestroy teardown shape, riding the settings seam):
        /// the receive gate closes FIRST (a late callback can never write state
        /// after the mailbox reset), then tracking, state, patches, HUD, network.</summary>
        private void Disarm()
        {
            if (!armed) return;
            armed = false;
            Receiver.Close();
            Receiver.DrainToState();
            Receiver.Reset();
            Tracking.Deactivate();
            HordeStateTracker.Clear();
            UninstallPatches();
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

        private void InstallPatches()
        {
            if (PatchesInstalled) return;
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
                harmony.CreateClassProcessor(typeof(BeaconCounterPatches)).Patch();
                if (CanUseClientUi)
                {
                    harmony.CreateClassProcessor(PlayerLifeHudSurface.PatchType).Patch();
                }
                PatchesInstalled = true;
                StartGateDiagnostics = string.Empty;
                LhtRuntime.LogInfo("[Lht] 尸潮补丁已安装（Harmony ID=" + LhtRuntime.FeatureIdValue + "）");
            }
            catch (Exception e)
            {
                // 环境闸（宿主测试进程无法装真机补丁 / 游戏版本漂移）：记录
                // 结构化诊断而非静默失败——半装回滚，失败即整体撤销自身。
                StartGateDiagnostics = "patch-install-failed: " + e.GetType().Name + ":" + e.Message;
                LhtRuntime.LogError("[Lht] 尸潮补丁安装失败（尸潮播报不可用）: " + e.Message);
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
                LhtRuntime.LogInfo("[Lht] 尸潮补丁已撤销（原生回退，仅撤自身 Harmony ID）");
            }
        }

        /// <summary>The patch-facing static entry (BeaconCounterPatches): no live
        /// module → silent release; a live module → the context guard.</summary>
        internal static void OnBeaconCounterPatched(InteractableBeacon beacon)
        {
            var module = ActiveModule;
            module?.Tracking.OnBeaconCounterChanged(beacon);
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
