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

        // DEV-V5-07 技能面：钩子绑定（null=未接线代际——纯闸门/纯压弹行为与
        // 接生前逐字相同）；升级请求的待确认表（id→目标级，回执按 id 消费）；
        // 技能操作经入站解析线程只入队、主线程消费（引擎接触零跨界，与压弹
        // 业务队列同律）；等级求取每会话一次（客机只显示主机确认等级）。
        private ILirSkillHooks skillHooks;
        private InPlaceReloadModule skillModule;
        private readonly Dictionary<ulong, byte> pendingUpgradeRequests = new Dictionary<ulong, byte>();
        private readonly Queue<ulong> pendingUpgradeOrder = new Queue<ulong>();
        private readonly object skillOpsSync = new object();
        private readonly List<Action> skillOps = new List<Action>();
        private int skillOpsDropped;
        private bool levelStateRequested;
        private bool stopped;

        private bool initialized;

        /// <summary>DEV-V5-07 绑定技能面（模块 Start 期；一次代际一次绑定）。</summary>
        internal void BindSkillHooks(InPlaceReloadModule module, ILirSkillHooks hooks)
        {
            skillModule = module;
            skillHooks = hooks;
        }

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
            if (dead != null)
            {
                for (var i = 0; i < dead.Count; i++)
                {
                    liveSessions.Remove(dead[i]);
                }
            }

            // DEV-V5-07: the client asks for its confirmed level exactly once
            // per generation once a session exists（未确认=分区不画，绝不猜级）。
            if (!levelStateRequested && skillHooks != null && !isServerRole() && liveSessions.Count > 0)
            {
                if (RequestLevelStateFromServer() == LirRepackRequestResult.Dispatched)
                {
                    levelStateRequested = true;
                }
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
            // DEV-V3-09 日志风暴修复：空闲帧（队列无待办）不再向平台 dispatcher
            // 投递空 drain——空 DrainOnce 无工作，投递只会让 dispatcher 每拍 emit
            // 一行 result=posted 诊断（U3DS 实机 ~60 行/秒）。有实际待办才投递，
            // 投递被拒仍下一拍重投（队未清=HasPendingWork 仍真）。
            if (!dispatcher.HasPendingWork && !HasSkillOps) return;
            var posted = seam.Post(DrainOnce);
            if (!posted.Posted)
            {
                LirRuntime.LogDiagnostic("[RepackNet] 主线程投递被拒（reason=" + posted.Reason
                    + ", diagnostic=" + posted.DiagnosticId + "），队内工作下一拍重投");
            }
        }

        /// <summary>The drain body: queued requests execute, queued replies
        /// toast (module's ToastSink). Runs on the host main thread — via the
        /// platform dispatcher pump when wired. DEV-V5-07: the skill-op tail
        /// (upgrade replies / level state / cooldown notices, inbound-parsed
        /// on any thread) applies here too — engine/UI contact stays on the
        /// host frame.</summary>
        internal void DrainOnce()
        {
            if (stopped) return;
            dispatcher.DrainOnMainThread(ExecuteRepackFor, (requestId, total) =>
            {
                var sink = moduleToast;
                sink?.Invoke(SuccessToast(total));
            });
            DrainSkillOps();
        }

        private bool HasSkillOps
        {
            get { lock (skillOpsSync) return skillOps.Count > 0; }
        }

        private void EnqueueSkillOp(Action op)
        {
            lock (skillOpsSync)
            {
                if (stopped) return;
                if (skillOps.Count >= ReloadRuntimePolicy.QueueLimit)
                {
                    skillOpsDropped++;
                    if (skillOpsDropped == 1 || skillOpsDropped % 32 == 0)
                        LirRuntime.LogError("[RepackNet] CRITICAL: 技能操作队列达上限 " + ReloadRuntimePolicy.QueueLimit + "，本条丢弃");
                    return;
                }
                skillOps.Add(op);
            }
        }

        private void DrainSkillOps()
        {
            List<Action> batch = null;
            lock (skillOpsSync)
            {
                if (skillOps.Count == 0) return;
                batch = new List<Action>(skillOps);
                skillOps.Clear();
            }
            for (var i = 0; i < batch.Count; i++)
            {
                try { batch[i](); }
                catch (Exception error)
                {
                    LirRuntime.LogError("[RepackNet] 技能操作异常（已隔离）: " + error.Message);
                }
            }
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
                pendingUpgradeRequests.Clear(); // DEV-V5-07：技能待确认同代际清
                pendingUpgradeOrder.Clear();
            }
            lock (skillOpsSync)
            {
                skillOps.Clear(); // DEV-V5-07：未消费的技能操作不过代际
            }
            levelStateRequested = false;
            Started = false;
            LirRuntime.LogInfo("[RepackNet] 服务已停止（频道注销、会话簿与待确认表已清）");
        }

        // ── inbound frames (any thread — parse + enqueue only) ─────

        private void HandleRequestFrame(IConnectionSession session, byte[] payload)
        {
            if (stopped) return;
            if (session == null)
            {
                dispatcher.IncrementParseErrors();
                LirRuntime.LogDiagnostic("[RepackNet] 服务器收到无会话来源的入站帧，拒绝");
                return;
            }
            // DEV-V5-07: kind dispatch on the payload byte — parse-only here,
            // every engine/UI contact rides the skill-op drain (main thread).
            if (LirRepackWireCodec.TryReadLevelStateRequest(payload))
            {
                var peer = session.PeerSteamId;
                EnqueueSkillOp(() =>
                {
                    var hooks = skillHooks;
                    if (hooks == null) return;
                    SendToPeer(session, LirRepackWireCodec.BuildLevelState(hooks.GetLevelFor(peer)));
                });
                return;
            }
            if (LirRepackWireCodec.TryReadUpgradeRequest(payload, out var upRequestId, out var targetLevel))
            {
                var peer = session.PeerSteamId;
                EnqueueSkillOp(() => ExecuteUpgradeFor(peer, upRequestId, targetLevel, session));
                return;
            }
            if (!LirRepackWireCodec.TryReadRequest(payload, out var requestId))
            {
                dispatcher.IncrementParseErrors();
                LirRuntime.LogDiagnostic("[RepackNet] 服务器收到未知或畸形入站帧，拒绝");
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
            var hooks = skillHooks;
            if (LirRepackWireCodec.TryReadUpgradeResult(payload, out var upReqId, out var accepted, out var newLevel, out var reasonCode))
            {
                byte requestedTarget;
                lock (pendingSync)
                {
                    if (!pendingUpgradeRequests.TryGetValue(upReqId, out requestedTarget))
                    {
                        dispatcher.IncrementParseErrors();
                        LirRuntime.LogDiagnostic("[ReloadSkill] 收到未知或重复的升级回执 id=" + upReqId + "，忽略");
                        return;
                    }
                    pendingUpgradeRequests.Remove(upReqId);
                }
                var capturedTarget = requestedTarget;
                EnqueueSkillOp(() =>
                {
                    var module = skillModule;
                    if (accepted)
                    {
                        ReloadSkillLevelMirror.ConfirmLevel(newLevel);
                        module?.NotifySkillLevelConfirmed(newLevel);
                        var cost = ReloadSkillPolicy.CostForUpgrade(newLevel - 1);
                        module?.ShowSkillToast(ReloadSkillPolicy.MakeUpgradeAcceptedToast(newLevel, cost));
                    }
                    else
                    {
                        module?.ShowSkillToast(ReloadSkillPolicy.MakeUpgradeRejectedToast((ReloadSkillUpgradeReject)reasonCode, capturedTarget));
                    }
                    module?.ResetSkillSettingsRow();
                });
                return;
            }
            if (LirRepackWireCodec.TryReadLevelState(payload, out var level))
            {
                EnqueueSkillOp(() =>
                {
                    ReloadSkillLevelMirror.ConfirmLevel(level);
                    skillModule?.NotifySkillLevelConfirmed(level);
                });
                return;
            }
            if (LirRepackWireCodec.TryReadSkillCooldownNotice(payload, out var remainingMs))
            {
                EnqueueSkillOp(() => skillModule?.ShowSkillToast(ReloadSkillPolicy.MakeCooldownToast(remainingMs / 1000d)));
                return;
            }
            if (!LirRepackWireCodec.TryReadSuccess(payload, out var requestId, out var totalTransferred))
            {
                dispatcher.IncrementParseErrors();
                LirRuntime.LogDiagnostic("[RepackNet] 客机收到未知或畸形下行帧，拒绝");
                return;
            }
            _ = hooks;
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

        // ── DEV-V5-07 技能面：客侧请求 + 主线程执行/呈现 ────────────

        /// <summary>客机升级请求（主机校验并扣原版经验，回执带确认等级）。</summary>
        internal LirRepackRequestResult RequestUpgradeFromServer(byte targetLevel)
        {
            if (stopped || !Started) return LirRepackRequestResult.RejectedNoSession;
            var requestId = NextRequestId();
            lock (pendingSync)
            {
                while (pendingUpgradeOrder.Count >= ReloadRuntimePolicy.QueueLimit)
                {
                    pendingUpgradeRequests.Remove(pendingUpgradeOrder.Dequeue());
                }
                pendingUpgradeOrder.Enqueue(requestId);
                pendingUpgradeRequests[requestId] = targetLevel;
            }
            NetworkSendResult sent;
            try { sent = network.SendToServer(Channel, LirRepackWireCodec.BuildUpgradeRequest(requestId, targetLevel), reliable: true); }
            catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
            if (sent != NetworkSendResult.Sent)
            {
                lock (pendingSync) pendingUpgradeRequests.Remove(requestId);
                LirRuntime.LogWarning("[ReloadSkill] 升级请求发送失败（result=" + sent + "），未建立待确认。");
                return LirRepackRequestResult.RejectedSendFailed;
            }
            LirRuntime.LogInfo("[ReloadSkill] -> 服务器: RequestUpgrade(reqId=" + requestId + ", target=" + targetLevel + ")");
            return LirRepackRequestResult.Dispatched;
        }

        /// <summary>客机一次性求取确认等级（会话建立后；未确认=分区不画）。</summary>
        internal LirRepackRequestResult RequestLevelStateFromServer()
        {
            if (stopped || !Started) return LirRepackRequestResult.RejectedNoSession;
            NetworkSendResult sent;
            try { sent = network.SendToServer(Channel, LirRepackWireCodec.BuildLevelStateRequest(), reliable: true); }
            catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
            return sent == NetworkSendResult.Sent
                ? LirRepackRequestResult.Dispatched
                : LirRepackRequestResult.RejectedSendFailed;
        }

        /// <summary>主线程：主机侧升级执行（校验+扣减+落账全在钩子里；这里只路由回执）。</summary>
        private void ExecuteUpgradeFor(ulong steamId, ulong requestId, byte targetLevel, IConnectionSession session)
        {
            var hooks = skillHooks;
            if (hooks == null) return;
            ReloadSkillUpgradeDecision decision;
            try { decision = hooks.ExecuteUpgrade(steamId, targetLevel); }
            catch (Exception error)
            {
                LirRuntime.LogError("[ReloadSkill] 升级执行异常（拒绝回执）: " + error.Message);
                decision = new ReloadSkillUpgradeDecision { Accepted = false, Reason = ReloadSkillUpgradeReject.LevelDrift };
            }
            var reply = LirRepackWireCodec.BuildUpgradeResult(requestId, decision.Accepted,
                decision.Accepted ? decision.NewLevel : hooks.GetLevelFor(steamId), (byte)decision.Reason);
            SendToPeer(session, reply);
        }

        /// <summary>技能窗拒绝的呈现路由：本机=直接红色 toast；远端=定向 kind 6
        /// （剩余毫秒按主机时钟口径发出，客机只呈现）。</summary>
        private void DeliverSkillCooldownNotice(ulong steamId, double remainingSeconds)
        {
            if (remainingSeconds <= 0d) return;
            if (authority.TryResolveLocalPlayerSteamId(out var localSteamId) && localSteamId == steamId)
            {
                var sink = moduleToast;
                sink?.Invoke(ReloadSkillPolicy.MakeCooldownToast(remainingSeconds));
                return;
            }
            var ms = (int)Math.Ceiling(remainingSeconds * 1000d);
            if (ms <= 0) return;
            IConnectionSession session = null;
            foreach (var pair in liveSessions)
            {
                if (pair.Value != null && pair.Value.PeerSteamId == steamId) { session = pair.Value; break; }
            }
            if (session == null)
            {
                LirRuntime.LogDiagnostic("[ReloadSkill] 冷却回执目标会话不存在（steam=" + steamId + "），本机外静默");
                return;
            }
            SendToPeer(session, LirRepackWireCodec.BuildSkillCooldownNotice(ms));
        }

        private void SendToPeer(IConnectionSession session, byte[] payload)
        {
            if (session == null) return;
            NetworkSendResult sent;
            try { sent = network.SendToClient(Channel, session, payload, reliable: true); }
            catch (Exception) { sent = NetworkSendResult.LocalTransportUnavailable; }
            if (sent != NetworkSendResult.Sent)
            {
                LirRuntime.LogDiagnostic("[ReloadSkill] 技能帧定向发送未送达（result=" + sent + "）");
            }
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
            ExecuteRepackFor(senderSteamId, requestId, false);
        }

        /// <summary>DEV-V5-07: the 技能窗 sits BEFORE the authority transaction
        /// (0 级合并窗 = 技术闸+额外——窗内拒绝连技术闸都不消费)；manual success
        /// with rounds transferred hands the 2 级 auto-round decision to the
        /// module (the service keeps no skill policy of its own).</summary>
        internal void ExecuteRepackFor(ulong senderSteamId, ulong requestId, bool isAuto)
        {
            if (stopped) return;
            var hooks = skillHooks;
            if (hooks != null && !hooks.TryBeginRepackWindow(senderSteamId, out var remainingSeconds))
            {
                DeliverSkillCooldownNotice(senderSteamId, remainingSeconds);
                return;
            }
            // The gate is the authority's engine-side discipline (cooldown /
            // replay / quarantine) — the service only routes outcomes.
            var result = authority.ExecuteRepack(senderSteamId, requestId);
            switch (result.Outcome)
            {
                case LirRepackOutcome.Committed:
                    if (result.TotalTransferred > 0)
                    {
                        DeliverSuccess(senderSteamId, requestId, result.TotalTransferred);
                        if (!isAuto && hooks != null
                            && hooks.GetLevelFor(senderSteamId) == ReloadSkillPolicy.MaxSkillLevel)
                        {
                            var fingerprint = hooks.CaptureFingerprint(senderSteamId);
                            if (fingerprint != null)
                            {
                                var module = skillModule;
                                module?.ScheduleAutoRoundAfterManualSuccess(senderSteamId, fingerprint);
                            }
                        }
                    }
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
