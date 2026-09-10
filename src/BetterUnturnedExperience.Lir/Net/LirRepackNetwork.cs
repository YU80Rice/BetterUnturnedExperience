using System;
using System.Collections.Generic;
using System.Threading;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>Explicit client-side send outcomes (the old diagnostic lines, now typed).</summary>
    internal enum LirRepackRequestResult : byte
    {
        Dispatched = 0,
        RejectedNoSession = 1,
        RejectedSendFailed = 2,
    }

    /// <summary>
    /// DEV-V2-22: the LIR network service — the old AmmoRepackNetwork's
    /// request/reply pair carried over the BUE named channel (the FeatureId
    /// IS the channel identity; the old LMN channel string retires). The
    /// migrated semantics, unchanged in kind:
    ///   - registration is DEFERRED to the first frame on the game thread
    ///     (the old EnsureNetworkInitializedOnGameThread, now riding the host
    ///     clock inside the module lifecycle) with HALF-REGISTRATION ROLLBACK:
    ///     any subscribe failure disposes every handle and unregisters the
    ///     channel — zero residue, the local path stays alive, no retry;
    ///   - inbound handlers only parse and enqueue (any thread); all engine
    ///     work happens in the host-frame drain through the dispatcher;
    ///   - the server reply is TARGETED at the requesting session
    ///     (SendToClient, session-owned identity — never a payload field);
    ///   - the client tracks its pending request ids: a reply for an unknown
    ///     or duplicate id is refused (replay/forge guard).
    /// One instance per module generation; Stop tears everything down.
    /// </summary>
    internal sealed class LirRepackNetwork
    {
        private static readonly FeatureId Channel = new FeatureId(LirRuntime.FeatureIdValue);

        // The old request-id sequence: process-wide monotonic from a time seed.
        private static long requestSequence = DateTime.UtcNow.Ticks;

        internal static ulong NextRequestId()
        {
            return unchecked((ulong)Interlocked.Increment(ref requestSequence));
        }

        private readonly IBueNetworkApi network;
        private readonly ILirRepackAuthority authority;
        private readonly Func<bool> isServerRole;
        private readonly LirRepackDispatcher dispatcher = new LirRepackDispatcher();
        private readonly List<IDisposable> subscriptionHandles = new List<IDisposable>();
        private readonly Dictionary<ulong, IConnectionSession> liveSessions = new Dictionary<ulong, IConnectionSession>();
        private readonly object pendingSync = new object();
        private readonly HashSet<ulong> pendingRequestIds = new HashSet<ulong>();
        private readonly Queue<ulong> pendingRequestOrder = new Queue<ulong>();

        private bool initialized;
        private bool stopped;

        internal LirRepackNetwork(IBueNetworkApi network, ILirRepackAuthority authority, Func<bool> isServerRole)
        {
            this.network = network ?? throw new ArgumentNullException(nameof(network));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            this.isServerRole = isServerRole ?? throw new ArgumentNullException(nameof(isServerRole));
        }

        internal bool Started { get; private set; }

        /// <summary>
        /// The deferred first-frame init (module HostTick, game thread). One
        /// attempt per generation: a failure rolls everything back and the
        /// module keeps its LOCAL paths — the old singleplayer-survives rule.
        /// </summary>
        internal bool EnsureInitializedOnGameThread()
        {
            if (initialized) return Started;
            initialized = true; // one attempt, never retried (the old flag semantics)
            if (stopped) return false;
            if (Thread.CurrentThread.ManagedThreadId != LirRuntime.MainThreadId)
            {
                LirRuntime.LogError("[RepackNet] 网络初始化不在游戏主线程，跳过（联机压弹不可用，单机压弹仍可用）");
                return false;
            }
            var registration = network.RegisterChannel(Channel, new ContractVersion(2, 0), 1);
            if (!registration.Accepted)
            {
                LirRuntime.LogError("[RepackNet] 频道注册被拒绝（reason=" + registration.Reason + "），联机压弹不可用");
                return false;
            }
            IDisposable fromClients = null;
            IDisposable fromServer = null;
            try
            {
                fromClients = network.Subscribe(Channel, ChannelDirection.FromClients, HandleRequestFrame);
                fromServer = network.Subscribe(Channel, ChannelDirection.FromServer, HandleSuccessFrame);
            }
            catch (Exception error)
            {
                try { fromClients?.Dispose(); } catch (Exception) { }
                try { fromServer?.Dispose(); } catch (Exception) { }
                try { network.UnregisterChannel(Channel); } catch (Exception) { }
                LirRuntime.LogError("[RepackNet] 双方向订阅半途失败，已回滚注册（errorType=" + error.GetType().Name + "）");
                return false;
            }
            subscriptionHandles.Add(fromClients);
            subscriptionHandles.Add(fromServer);
            Started = true;
            LirRuntime.LogInfo("[RepackNet] 已注册 BUE 命名频道=" + Channel.Value + " 双方向处理器（首帧游戏线程）");
            return true;
        }

        /// <summary>The session reconcile (poll-based, host frame): tracks established sessions for reply targeting.</summary>
        internal void Tick()
        {
            if (stopped) return;
            IReadOnlyList<IConnectionSession> current;
            try { current = network.Sessions ?? new IConnectionSession[0]; }
            catch (Exception error)
            {
                // R1-Standards INFO-1: the reconcile probe failure is visible
                // (throttled) instead of fully silent — the next tick retries.
                LirRuntime.LogDiagnostic("[RepackNet] 会话快照读取异常（本拍跳过）: " + error.Message);
                return;
            }
            var seen = new HashSet<ulong>();
            for (var i = 0; i < current.Count; i++)
            {
                var session = current[i];
                if (session == null) continue;
                seen.Add(session.SessionId);
                liveSessions[session.SessionId] = session;
            }
            List<ulong> dead = null;
            foreach (var pair in liveSessions)
            {
                if (!seen.Contains(pair.Key)) (dead ??= new List<ulong>()).Add(pair.Key);
            }
            if (dead == null) return;
            for (var i = 0; i < dead.Count; i++)
            {
                liveSessions.Remove(dead[i]);
            }
        }

        /// <summary>Resolves a session generation to its peer steam id (0 = unknown).</summary>
        internal ulong ResolveSessionPeer(ulong generation)
        {
            return liveSessions.TryGetValue(generation, out var session) ? session.PeerSteamId : 0UL;
        }

        // DEV-V3-04: the platform main-thread dispatcher seam (the module
        // binds it at Start from bootstrap.MainThread). Official first
        // consumption of the IFeatureMainThread surface — repack execution
        // moves to the host-frame drain via a POSTED task, so LIR no longer
        // owns the main-thread handoff pump; the business queue (per-sender
        // coalesce, reply priority, TTL, throttled summary) stays feature-
        // private policy. Null = a host that has not wired the seam (stage
        // baseline): the historical inline drain is kept, unchanged.
        private IFeatureMainThread mainThread;

        internal void BindMainThread(IFeatureMainThread view) { mainThread = view; }

        /// <summary>The per-beat drain handoff: posted to the platform
        /// dispatcher when wired (execution rides the host pump beat), inline
        /// on the unwired baseline. A rejected post is visible and retried
        /// next beat — never silently dropped.</summary>
        internal void Drain()
        {
            if (stopped) return;
            var seam = mainThread;
            if (seam == null)
            {
                DrainOnce();
                return;
            }
            var posted = seam.Post(DrainOnce);
            if (!posted.Posted)
            {
                LirRuntime.LogDiagnostic("[RepackNet] 主线程投递被拒（reason=" + posted.Reason
                    + ", diagnostic=" + posted.DiagnosticId + "），队内工作下一拍重投");
            }
        }

        /// <summary>The drain body: queued requests execute, queued replies
        /// toast (module's ToastSink). Runs on the host main thread — via the
        /// platform dispatcher pump when the seam is wired.</summary>
        internal void DrainOnce()
        {
            if (stopped) return;
            dispatcher.DrainOnMainThread(ExecuteRepackFor, (requestId, total) =>
            {
                var sink = moduleToast;
                sink?.Invoke(SuccessToast(total));
            });
        }

        /// <summary>The success toast text — ONE source (R1-Standards SMELL-3).</summary>
        private static string SuccessToast(int totalTransferred)
        {
            return "<b><color=#5ce65c>一键压弹：成功压入 " + totalTransferred + " 发子弹</color></b>";
        }

        private Action<string> moduleToast;

        /// <summary>Production binds LirToast.Show; tests bind a recorder.</summary>
        internal void BindToastSink(Action<string> toastSink)
        {
            moduleToast = toastSink;
        }

        /// <summary>
        /// The client-side request path: a live session, a tracked pending id
        /// (replay/forge guard), the reliable send whose explicit result
        /// decides whether the pending may live.
        /// </summary>
        internal LirRepackRequestResult RequestRepackFromServer()
        {
            if (stopped || !Started) return LirRepackRequestResult.RejectedNoSession;
            IConnectionSession session = null;
            try
            {
                var snapshot = network.Sessions;
                if (snapshot != null && snapshot.Count > 0) session = snapshot[0]; // a client has exactly one server peer
            }
            catch (Exception error)
            {
                // R2-Standards INFO-4: the probe failure is visible (throttled),
                // not folded silently into "no session".
                LirRuntime.LogDiagnostic("[RepackB] 会话快照读取异常（按无会话处理）: " + error.Message);
            }
            if (session == null)
            {
                LirRuntime.LogDiagnostic("[RepackB] 联机网络层未就绪（无已建立会话），拒绝发送压弹请求。");
                return LirRepackRequestResult.RejectedNoSession;
            }
            var requestId = NextRequestId();
            lock (pendingSync)
            {
                while (pendingRequestOrder.Count >= ReloadRuntimePolicy.QueueLimit)
                {
                    pendingRequestIds.Remove(pendingRequestOrder.Dequeue());
                }
                pendingRequestOrder.Enqueue(requestId);
                pendingRequestIds.Add(requestId);
            }
            NetworkSendResult sent;
            try { sent = network.SendToServer(Channel, LirRepackWireCodec.BuildRequest(requestId), reliable: true); }
            catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
            if (sent != NetworkSendResult.Sent)
            {
                lock (pendingSync) pendingRequestIds.Remove(requestId);
                LirRuntime.LogWarning("[RepackB] 压弹请求发送失败（result=" + sent + "），未建立待确认。");
                return LirRepackRequestResult.RejectedSendFailed;
            }
            LirRuntime.LogInfo("[RepackB] -> 服务器: RequestRepackAmmo(reqId=" + requestId + ")");
            return LirRepackRequestResult.Dispatched;
        }

        internal void Stop()
        {
            if (stopped) return;
            stopped = true;
            for (var i = 0; i < subscriptionHandles.Count; i++)
            {
                try { subscriptionHandles[i]?.Dispose(); } catch (Exception) { }
            }
            subscriptionHandles.Clear();
            try { network.UnregisterChannel(Channel); } catch (Exception) { }
            dispatcher.Shutdown();
            liveSessions.Clear();
            lock (pendingSync)
            {
                pendingRequestIds.Clear();
                pendingRequestOrder.Clear();
            }
            Started = false;
            LirRuntime.LogInfo("[RepackNet] 服务已停止（频道注销、会话簿与待确认表已清）");
        }

        // ── inbound frames (any thread — parse + enqueue only) ─────

        private void HandleRequestFrame(IConnectionSession session, byte[] payload)
        {
            if (stopped) return;
            if (session == null || !LirRepackWireCodec.TryReadRequest(payload, out var requestId))
            {
                dispatcher.IncrementParseErrors();
                LirRuntime.LogDiagnostic("[RepackNet] 服务器收到畸形 RequestRepackAmmo，拒绝");
                return;
            }
            // The session is the identity authority — never a payload field.
            if (!dispatcher.TryEnqueueRequest(session.PeerSteamId, requestId))
            {
                dispatcher.IncrementRejected();
            }
        }

        private void HandleSuccessFrame(IConnectionSession session, byte[] payload)
        {
            if (stopped) return;
            if (!LirRepackWireCodec.TryReadSuccess(payload, out var requestId, out var totalTransferred))
            {
                dispatcher.IncrementParseErrors();
                LirRuntime.LogDiagnostic("[RepackNet] 客机收到畸形 RepackSuccess，拒绝");
                return;
            }
            lock (pendingSync)
            {
                if (!pendingRequestIds.Remove(requestId))
                {
                    dispatcher.IncrementParseErrors();
                    LirRuntime.LogDiagnostic("[RepackNet] 收到未知或重复 requestId=" + requestId + " 的回包，忽略");
                    return;
                }
            }
            dispatcher.TryEnqueueSuccess(requestId, totalTransferred);
        }

        // ── main-thread execution (the drain callbacks) ────────────

        /// <summary>
        /// The single main-thread entry for repack work — the local double-tap
        /// and the queued remote requests share it (the old merged entry;
        /// enqueueing is not authorization, the authority re-checks the
        /// player and the gate).
        /// </summary>
        internal void ExecuteRepackFor(ulong senderSteamId, ulong requestId)
        {
            if (stopped) return;
            // The gate is the authority's engine-side discipline (cooldown /
            // replay / quarantine) — the service only routes outcomes.
            var result = authority.ExecuteRepack(senderSteamId, requestId);
            switch (result.Outcome)
            {
                case LirRepackOutcome.Committed:
                    if (result.TotalTransferred > 0) DeliverSuccess(senderSteamId, requestId, result.TotalTransferred);
                    break;
                case LirRepackOutcome.PlayerMissing:
                    dispatcher.IncrementMissingPlayer();
                    break;
                case LirRepackOutcome.RejectedCooldown:
                    dispatcher.IncrementRejected();
                    break;
                case LirRepackOutcome.RestoreFailed:
                    // The authority quarantined the player; this is the
                    // critical diagnostic the old layer carried.
                    LirRuntime.LogError("[RepackNet] CRITICAL: sender=" + senderSteamId + " RestoreFailed 半提交风险（已 Quarantine）");
                    break;
                default:
                    // NoChange / RolledBack / AbortedStateDrift: a scan ran,
                    // nothing to deliver — the old silent outcomes.
                    break;
            }
        }

        private void DeliverSuccess(ulong senderSteamId, ulong requestId, int totalTransferred)
        {
            // Toast ownership: the local player sees the toast here; a remote
            // client gets the targeted reliable reply (U3DS is headless — the
            // toast must render on the client).
            if (authority.TryResolveLocalPlayerSteamId(out var localSteamId) && localSteamId == senderSteamId)
            {
                var sink = moduleToast;
                sink?.Invoke(SuccessToast(totalTransferred));
                return;
            }
            SendRepackSuccess(senderSteamId, requestId, totalTransferred);
        }

        private void SendRepackSuccess(ulong senderSteamId, ulong requestId, int totalTransferred)
        {
            if (totalTransferred <= 0) return;
            IConnectionSession session = null;
            foreach (var pair in liveSessions)
            {
                if (pair.Value != null && pair.Value.PeerSteamId == senderSteamId) { session = pair.Value; break; }
            }
            if (session == null)
            {
                LirRuntime.LogWarning("[RepackNet] 回包目标会话已不存在（sender=" + senderSteamId + ", reqId=" + requestId + "），客户端将按待确认表无果");
                return;
            }
            NetworkSendResult sent;
            try { sent = network.SendToClient(Channel, session, LirRepackWireCodec.BuildSuccess(requestId, totalTransferred), reliable: true); }
            catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
            if (sent != NetworkSendResult.Sent)
            {
                LirRuntime.LogWarning("[RepackNet] RepackSuccess 定向发送未送达（reqId=" + requestId + ", result=" + sent + "）");
            }
            else
            {
                LirRuntime.LogInfo("[RepackNet] -> 客机 RepackSuccess(reqId=" + requestId + ", total=" + totalTransferred + ")");
            }
        }
    }
}
