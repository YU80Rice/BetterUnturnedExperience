using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the server-authority internal piece (spec「内部双件」之一：
    /// 信标追踪 / epoch / seq / 广播，服务器权威). Engine touch points all ride
    /// IHordeTrackingAuthority (production = HordeProductionAuthority, host
    /// tests = the recording fake), so this class is host-testable without
    /// the game.
    ///
    /// The broadcast is the session-driven SendToClients multicast — the old
    /// Provider.clients handwritten loop, its skip-local identity logic and
    /// its single-player special case are gone BY CONSTRUCTION (the
    /// established snapshot never contains the local host; an empty snapshot
    /// is the frozen NoSession). The local authority publish feeds the host
    /// HUD in the same cycle; no loopback exists. The business-consistency
    /// machinery is preserved as-is: epoch, sequence, single-slot mailbox
    /// (client side), the guard's dirty flag, dual reliability (Update
    /// unreliable / Clear reliable) and the stop gate (LastBroadcastedActive).
    /// </summary>
    internal sealed class HordeTrackingModule
    {
        private readonly IHordeTrackingAuthority authority;
        private readonly IHordeTrackingPolicy policy;
        private readonly IBueNetworkApi network;
        private readonly Func<bool> isEnabled;
        private readonly HordeBeaconContextGuard guard = new HordeBeaconContextGuard();

        private HordeBeaconView activeBeacon;
        private uint currentEpoch;
        private uint currentSequence;
        private bool reportedActive;
        private bool lastBroadcastedActive;
        private bool commandRegistrationPending;
        private float accumulatedSeconds;
        private float nextFallbackAt;
        private float nextHeartbeatAt;
        private bool active;

        internal HordeTrackingModule(IHordeTrackingAuthority authority, IHordeTrackingPolicy policy, IBueNetworkApi network, Func<bool> isEnabled)
        {
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            this.network = network ?? throw new ArgumentNullException(nameof(network));
            this.isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
        }

        /// <summary>The channel identity = the FeatureId (spec「一词一贯」).</summary>
        internal FeatureId Channel { get; } = new FeatureId(LhtRuntime.FeatureIdValue);

        /// <summary>The current epoch (diagnostics + status).</summary>
        internal uint CurrentEpoch => currentEpoch;

        internal HordeBeaconContextGuard Guard => guard;

        /// <summary>Activates the engine subscriptions (server AND client modules —
        /// the event handler gates by role, the migrated shape).</summary>
        internal void Activate()
        {
            if (active) return;
            authority.SubscribeBeaconUpdated(HandleBeaconUpdated);
            authority.SubscribeServerHosted(OnServerHosted);
            // Re-arm the /horde registration attempt: the module may start (or
            // be re-enabled) after the host transition already happened.
            commandRegistrationPending = true;
            active = true;
            LhtRuntime.LogInfo("[HordeTracker] 已订阅 BeaconManager.onBeaconUpdated + Provider.onServerHosted");
        }

        /// <summary>The generation boundary: subscriptions dropped, the command
        /// deregistered, the guard and the tracking state cleared.</summary>
        internal void Deactivate()
        {
            if (!active) return;
            authority.UnsubscribeBeaconUpdated(HandleBeaconUpdated);
            authority.UnsubscribeServerHosted(OnServerHosted);
            commandRegistrationPending = false;
            authority.DeregisterHordeCommand();
            guard.Clear();
            activeBeacon = null;
            reportedActive = false;
            lastBroadcastedActive = false;
            nextFallbackAt = 0f;
            nextHeartbeatAt = 0f;
            active = false;
        }

        /// <summary>The engine beacon event (main thread): activation enters a new
        /// epoch and tracks the beacon; the end clears the track (the Clear
        /// snapshot rides the next tick). Non-server roles track nothing.</summary>
        internal void HandleBeaconUpdated(byte nav, bool hasBeacon)
        {
            if (!active || !isEnabled()) return;
            if (!authority.IsServerRole())
            {
                activeBeacon = null;
                return;
            }

            if (hasBeacon)
            {
                if (!authority.TryResolveBeacon(nav, out HordeBeaconView beacon) || beacon == null)
                {
                    activeBeacon = null;
                    return;
                }

                if (!policy.ShouldAcceptActivation(beacon.TotalZombies))
                {
                    // asset 尚未通过 updateState 注入，等下一次广播周期重试
                    activeBeacon = null;
                    return;
                }

                // beacon 激活进入新 epoch（旧 epoch 的延迟包无法复活新 epoch）
                currentEpoch++;
                currentSequence = 0;
                activeBeacon = beacon;
                guard.Track(beacon.Beacon);
                // 节点 A：尸潮爆发激活 - 立即广播一次让 HUD 显示
                guard.TryRequestBroadcast(beacon.Beacon);

                authority.NotifyHordeStart(beacon);
                LhtRuntime.LogInfo("[HordeTracker] 尸潮爆发 @ nav=" + nav + " epoch=" + currentEpoch
                    + " 地点=" + beacon.LocationName + " 发起人=" + beacon.OwnerName + " 总数=" + beacon.TotalZombies);
            }
            else
            {
                HordeBeaconView endedBeacon = activeBeacon;
                activeBeacon = null;
                guard.Clear();

                if (endedBeacon != null)
                {
                    // 节点 C：尸潮平息隐藏 - 由下一周期广播 Clear
                    authority.NotifyHordeEnd(endedBeacon);
                    LhtRuntime.LogInfo("[HordeTracker] 尸潮结束 @ nav=" + nav + " epoch=" + currentEpoch
                        + " 地点=" + endedBeacon.LocationName);
                }
            }
        }

        /// <summary>The patch-facing counter entry (BeaconCounterPatches). The context
        /// guard releases every non-related call immediately; only the tracked
        /// beacon's counters mark the broadcast dirty.</summary>
        internal void OnBeaconCounterChanged(object beacon)
        {
            if (!active || !isEnabled()) return;
            if (!authority.IsServerRole()) return;
            if (activeBeacon == null) return;
            guard.TryRequestBroadcast(beacon);
        }

        /// <summary>The host-frame broadcast cycle (the old HordeStatusBroadcaster):
        /// pending /horde flush → heartbeat diagnostic → event-driven dirty
        /// broadcast with the 2s fallback → the one clear when the horde ends.
        /// The cadence clock accumulates HostTick deltas (the host clock is the
        /// frozen frame seam; the old Time.unscaledTime reads are gone).</summary>
        internal void Tick(float deltaTime)
        {
            if (!active || !isEnabled()) return;
            accumulatedSeconds += deltaTime;

            if (commandRegistrationPending
                && authority.TryFlushHordeCommandRegistration(TryGetStatus))
            {
                commandRegistrationPending = false;
            }

            if (!authority.IsServerRole()) return;

            try
            {
                if (accumulatedSeconds >= nextHeartbeatAt)
                {
                    nextHeartbeatAt = accumulatedSeconds + policy.HeartbeatIntervalSeconds;
                    LhtRuntime.LogInfo("[HordeBroadcaster] heartbeat: isServer=true hasActive="
                        + (activeBeacon != null && activeBeacon.IsValid) + " reportedActive=" + reportedActive
                        + " epoch=" + currentEpoch);
                }

                if (activeBeacon != null && activeBeacon.IsValid)
                {
                    reportedActive = true;

                    // 事件驱动：dirty 时立即广播（每帧检查，合并同帧多次击杀）
                    bool shouldBroadcast = guard.ConsumeBroadcastDirty();
                    if (!shouldBroadcast && accumulatedSeconds >= nextFallbackAt)
                    {
                        shouldBroadcast = true;
                        nextFallbackAt = accumulatedSeconds + policy.FallbackBroadcastIntervalSeconds;
                    }

                    if (shouldBroadcast)
                    {
                        authority.TryReadCounters(activeBeacon, out int remaining, out int alive);
                        int outstanding = policy.ClampOutstanding(remaining + alive);
                        var (epoch, sequence) = AllocateSequence();
                        var snapshot = new HordeSnapshot(true, epoch, sequence, (ushort)outstanding,
                            activeBeacon.TotalZombies, activeBeacon.LocationName, activeBeacon.OwnerName);
                        BroadcastUpdate(snapshot);
                    }
                }
                else
                {
                    // 没有活跃尸潮时，仅在"上次广播过 active"时发送一次 clear
                    if (reportedActive)
                    {
                        reportedActive = false;
                        var (epoch, sequence) = AllocateSequence();
                        var clearSnapshot = new HordeSnapshot(false, epoch, sequence, 0, 0, string.Empty, string.Empty);
                        if (lastBroadcastedActive)
                        {
                            NetworkSendResult result = SendMulticast(HordeWireCodec.BuildClear(epoch, sequence), reliable: true);
                            lastBroadcastedActive = false;
                            LhtRuntime.LogInfo("[HordeNet] 广播 Clear: epoch=" + epoch + " seq=" + sequence + " result=" + result);
                        }
                        // 服务器端权威发布（房主 HUD 直读）。
                        HordeStateTracker.Publish(clearSnapshot);
                    }
                    nextFallbackAt = accumulatedSeconds + policy.FallbackBroadcastIntervalSeconds;
                }
            }
            catch (Exception error)
            {
                LhtRuntime.LogError("[HordeBroadcaster] broadcast crash: " + error.Message);
            }
        }

        private (uint epoch, uint sequence) AllocateSequence()
        {
            currentSequence++;
            return (currentEpoch, currentSequence);
        }

        /// <summary>The /horde status probe handed to the command shell (folds the
        /// enabled gate — a disabled feature reports no horde; the live
        /// counters ride along for the killed/remaining math).</summary>
        private bool TryGetStatus(out HordeBeaconView beacon)
        {
            beacon = null;
            if (activeBeacon == null || !activeBeacon.IsValid || !isEnabled()) return false;
            beacon = authority.TryReadCounters(activeBeacon, out int remaining, out int alive)
                ? activeBeacon.WithCounters(remaining, alive)
                : activeBeacon;
            return true;
        }

        private void OnServerHosted()
        {
            if (commandRegistrationPending) return;
            commandRegistrationPending = true;
            LhtRuntime.LogInfo("[HordeTracker] /horde registration pending; waiting for existing Commander.commands");
        }

        private void BroadcastUpdate(HordeSnapshot snapshot)
        {
            NetworkSendResult result = SendMulticast(HordeWireCodec.BuildUpdate(snapshot), reliable: false);
            lastBroadcastedActive = true;
            // 服务器端权威发布；网络广播与本地发布同一周期（无回环）。
            HordeStateTracker.Publish(snapshot);
            LhtRuntime.LogInfo("[HordeNet] 广播 Update: epoch=" + snapshot.Epoch + " seq=" + snapshot.Sequence
                + " remaining=" + snapshot.Remaining + "/" + snapshot.Total
                + " loc=" + snapshot.Location + " by=" + snapshot.Initiator + " result=" + result);
        }

        private NetworkSendResult SendMulticast(byte[] payload, bool reliable)
        {
            try
            {
                return network.SendToClients(Channel, payload, reliable);
            }
            catch (Exception error)
            {
                LhtRuntime.LogWarning("[HordeNet] 组播发送异常（按 LocalTransportUnavailable 处理）: " + error.Message);
                return NetworkSendResult.LocalTransportUnavailable;
            }
        }
    }
}
