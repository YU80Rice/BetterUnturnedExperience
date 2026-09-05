using System;
using System.Collections.Generic;
using System.Reflection;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Network;
using BetterUnturnedExperience.Core.Settings;
using HarmonyLib;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-06: the network module adapter — the wiring that turns the
    /// DEV-V2-04 decision core (LmnFrameClassifier + LmnTakeoverCoordinator)
    /// and the DEV-V2-05 V1 compat layer into the live takeover. One adapter
    /// owns the two official settings facets (network module + V1 compat,
    /// each a single client-local toggle persisted atomically), mirrors the
    /// standalone LMN V1 handler table while the takeover is active, delegates
    /// LMN2 frames to LMN's own router, and records the empty LMN config
    /// migration (T6: LMN has no config system — a no-op by design, never
    /// persisted). DEV-V2-12: while LMN's own prefix is live (the real
    /// takeover state) the inbound decision RELEASES every LMN frame to
    /// LMN's native dispatch — a BUE dispatch delivered the same frame twice
    /// because Harmony runs all prefixes — and an unresolvable client sender
    /// is never dispatched as 0. The Harmony prefixes are the ONLY callers of
    /// <see cref="ShouldConsumeInbound"/> in production; tests drive the
    /// decision core directly and never install patches. Diagnostic ids:
    /// BUE-V2NET-001 = migration record, BUE-V2NET-002 = takeover plumbing
    /// fault (mirror / router delegate / patch failure; the mirror's
    /// LMN-not-ready DEFERRAL shares the id but stays below error level —
    /// result=failed is reserved for real shape faults), BUE-V2NET-003 =
    /// takeover lifecycle (patch installed, module isolated).
    /// </summary>
    internal sealed class NetworkModuleAdapter
    {
        internal static readonly FeatureId NetworkFeature = new FeatureId("io.github.yu80rice.bue.network");
        internal static readonly FeatureId V1CompatFeature = new FeatureId("io.github.yu80rice.bue.network.v1compat");

        private const string NetworkSettingId = "network.enabled";
        private const string V1CompatSettingId = "v1compat.enabled";
        private const string MigrationDiagnosticId = "BUE-V2NET-001";
        private const string FaultDiagnosticId = "BUE-V2NET-002";
        private const string LifecycleDiagnosticId = "BUE-V2NET-003";

        /// <summary>
        /// DEV-V2-12: the standalone LMN Harmony instance id (authority:
        /// LaunchMultiplayerNet/Core/LaunchMultiplayerNetPlugin.cs:59). Its
        /// prefixes on the shared NetMessages.Receive* intercept points are
        /// LMN's own live dispatch path — see LmnNativeDispatchLive.
        /// </summary>
        internal const string LmnPatchOwner = "com.yu80rice.launchmultiplayernet";

        /// <summary>Host wiring binds this to the runtime log; tests capture the lines.</summary>
        internal static Action<string> DiagnosticLogSink = null;

        internal static NetworkModuleAdapter ActiveAdapter { get; private set; }

        private static readonly MethodInfo tryGetSteamIdMethod = AccessTools.Method(typeof(SDG.NetTransport.ITransportConnection), "TryGetSteamId");
        // NetMessages is internal to Assembly-CSharp — resolved by reflection
        // exactly like the standalone LMN patches it, never via typeof.
        private static readonly Type netMessagesType = AccessTools.TypeByName("SDG.Unturned.NetMessages");

        private readonly LmnTakeoverCoordinator coordinator;
        private readonly LmnV1CompatRegistry compatRegistry;
        private readonly LmnV1CompatLayer compatLayer;
        private readonly Func<Type> resolveModTransportType;
        private readonly Func<Type> resolveModRouterType;
        private readonly Action refreshPanel;
        private readonly SettingsRuntime networkSettings;
        private readonly SettingsRuntime v1CompatSettings;
        private readonly HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("io.github.yu80rice.bue.network");
        private readonly Func<bool> isLmnNativeClientDispatchLive;
        private readonly Func<bool> isLmnNativeServerDispatchLive;
        private bool networkEnabled = true;
        private string takeoverStatus = string.Empty;
        private string configMigrationStatus = string.Empty;
        private bool patchesInstalled;
        private bool isolated;
        private bool routerResolved;
        private MethodInfo routerClientMethod;
        private MethodInfo routerServerMethod;
        // DEV-V2-12: whether LMN's own prefix is live next to BUE's, per
        // direction. While it is, LMN dispatches that direction's LMN frames
        // itself and BUE must release — a BUE dispatch delivered the same
        // frame twice (the F-E defect). LMN's install is not strictly atomic
        // (a failed server patch leaves the client patch in place), so the
        // two directions are probed independently.
        private bool lmnNativeClientDispatchLive;
        private bool lmnNativeServerDispatchLive;

        internal NetworkModuleAdapter(string settingsRootPath, Func<bool> isStandaloneLmnLoaded, Func<Type> resolveModTransportType, Func<Type> resolveModRouterType, Action refreshPanel, Func<bool> isLmnNativeClientDispatchLive = null, Func<bool> isLmnNativeServerDispatchLive = null)
        {
            networkSettings = new SettingsRuntime(NetworkFeature, CreateNetworkDescriptors(), new FileSettingsPersistence(settingsRootPath));
            v1CompatSettings = new SettingsRuntime(V1CompatFeature, CreateV1CompatDescriptors(), new FileSettingsPersistence(settingsRootPath));
            compatRegistry = new LmnV1CompatRegistry();
            compatLayer = new LmnV1CompatLayer(compatRegistry);
            coordinator = new LmnTakeoverCoordinator(isStandaloneLmnLoaded ?? throw new ArgumentNullException(nameof(isStandaloneLmnLoaded)));
            this.resolveModTransportType = resolveModTransportType ?? throw new ArgumentNullException(nameof(resolveModTransportType));
            this.resolveModRouterType = resolveModRouterType ?? throw new ArgumentNullException(nameof(resolveModRouterType));
            this.refreshPanel = refreshPanel ?? throw new ArgumentNullException(nameof(refreshPanel));
            // Tests inject the per-direction live state (a null server probe
            // mirrors the client one); production (both null) self-detects
            // from the actual patch state on the intercept points.
            this.isLmnNativeClientDispatchLive = isLmnNativeClientDispatchLive;
            this.isLmnNativeServerDispatchLive = isLmnNativeServerDispatchLive ?? isLmnNativeClientDispatchLive;
            ApplySwitchesFromSettings();
            UpdateTakeoverStatus();
        }

        internal SettingsRuntime NetworkSettings { get { return networkSettings; } }
        internal SettingsRuntime V1CompatSettings { get { return v1CompatSettings; } }
        internal bool TakeoverActive { get { return !isolated && networkEnabled && coordinator.TakeoverActive; } }
        internal string TakeoverStatus { get { return takeoverStatus; } }
        internal string ConfigMigrationStatus { get { return configMigrationStatus; } }

        /// <summary>
        /// DEV-V2-12: per-direction live state of LMN's own prefix next to
        /// BUE's. True means that direction's LMN frames are dispatched once
        /// by LMN's native path and BUE must release them — a BUE dispatch
        /// delivered the same frame twice (Harmony runs all prefixes; this
        /// prefix's return-false skips only the vanilla method, never LMN's
        /// lower-priority prefix). Cached from the injected probes (tests)
        /// or the actual patch owners (production) whenever the takeover
        /// arms, and re-probed on the throttled tick cadence while still
        /// inert: BUE bootstraps BEFORE LMN's Awake installs its prefixes
        /// (BepInEx loads by file-name order), so the bootstrap-time probe
        /// is legitimately false. Once latched, the state stays: the
        /// standalone LMN never unpatches mid-session (its unpatch is domain
        /// shutdown only).
        /// </summary>
        internal bool LmnNativeClientDispatchLive { get { return lmnNativeClientDispatchLive; } }
        internal bool LmnNativeServerDispatchLive { get { return lmnNativeServerDispatchLive; } }
        internal bool LmnNativeDispatchLive { get { return lmnNativeClientDispatchLive && lmnNativeServerDispatchLive; } }

        internal static IReadOnlyList<SettingDescriptor> CreateNetworkDescriptors()
        {
            return new[] { ToggleDescriptor(NetworkFeature, NetworkSettingId, 1) };
        }

        internal static IReadOnlyList<SettingDescriptor> CreateV1CompatDescriptors()
        {
            return new[] { ToggleDescriptor(V1CompatFeature, V1CompatSettingId, 2) };
        }

        /// <summary>
        /// One-shot init (production module wiring / test sections): probe the
        /// standalone LMN, apply the persisted switches, mirror the legacy
        /// handler table while the takeover is active, and record the empty
        /// config migration. Never installs patches — patches are a separate
        /// production-only step so tests stay Harmony-free.
        /// </summary>
        internal void ActivateCore()
        {
            if (isolated) return;
            coordinator.Refresh();
            ApplySwitchesFromSettings();
            // DEV-V2-10 R3 (Standards H3): a disabled module does zero mirror
            // work — no LMN type resolution, no deferred diagnostic — same
            // zero-reflection rule RefreshSwitches already follows.
            if (networkEnabled && coordinator.TakeoverActive) MirrorLegacyHandlersSafe();
            RefreshLmnNativeDispatchLive();
            RecordEmptyConfigMigration();
            UpdateTakeoverStatus();
            ActiveAdapter = this;
        }

        /// <summary>
        /// DEV-V2-10 seam: the deferred-mirror retry cadence in plugin Update
        /// ticks (~5s at 60fps). BepInEx loads plugins by file-name order, so
        /// BUE (B) bootstraps before the standalone LMN (L) assembly exists —
        /// the bootstrap mirror defers and the tick retry completes it once
        /// LMN's table is reachable.
        /// </summary>
        internal const int DeferredMirrorTickInterval = 300;

        /// <summary>
        /// Driven by the plugin Update tick (all bootstrap decisions — the
        /// headless host has no client UI but still mirrors the V1 table).
        /// Throttled and silent while LMN stays unresolved, so a broken
        /// install can never spam the log.
        /// </summary>
        internal void RetryPendingMirror()
        {
            if (isolated || !networkEnabled || !coordinator.TakeoverActive) return;
            // DEV-V2-12 R2/R3 (Standards S1): the bootstrap probe runs before
            // LMN's Awake installs its prefixes, so the cached inert state is
            // legitimate at startup — re-probe on the same throttled tick
            // cadence while ANY direction is still inert (a partial install's
            // late direction must still be discovered). Each direction is a
            // monotonic latch (RefreshLmnNativeDispatchLive), so once both
            // are live the re-probe stops: zero reflection afterwards. The
            // standalone LMN never unpatches mid-session (its unpatch is
            // domain shutdown only).
            if ((!lmnNativeClientDispatchLive || !lmnNativeServerDispatchLive) && ++lmnProbeTicks >= DeferredMirrorTickInterval)
            {
                lmnProbeTicks = 0;
                RefreshLmnNativeDispatchLive();
            }
            if (!mirrorPending) return;
            if (++deferredMirrorTicks < DeferredMirrorTickInterval) return;
            deferredMirrorTicks = 0;
            MirrorLegacyHandlersSafe();
        }

        /// <summary>
        /// Production-only: installs the Priority.First prefixes on the shared
        /// intercept points. Hard zero-false-positive rule — when the probe is
        /// false or the module is switched off nothing is patched and nothing
        /// reflects. Idempotent; the takeover-active refresh path re-runs it
        /// so a switch-off/on cycle (or a startup-disabled module being
        /// re-enabled) always re-arms.
        /// </summary>
        internal void ApplyNetworkPatches()
        {
            if (isolated || patchesInstalled) return;
            if (!networkEnabled || !coordinator.TakeoverActive) return;
            try
            {
                if (netMessagesType == null)
                {
                    Emit("[BUE-V2NET] event=takeover-patch result=failed reason=intercept-type-missing diagnosticId=" + FaultDiagnosticId);
                    return;
                }
                var receiveFromClient = AccessTools.Method(netMessagesType, "ReceiveMessageFromClient");
                var receiveFromServer = AccessTools.Method(netMessagesType, "ReceiveMessageFromServer");
                if (receiveFromClient == null || receiveFromServer == null)
                {
                    Emit("[BUE-V2NET] event=takeover-patch result=failed reason=intercept-members-missing diagnosticId=" + FaultDiagnosticId);
                    return;
                }
                harmony.Patch(receiveFromClient, prefix: new HarmonyLib.HarmonyMethod(typeof(NetworkModuleAdapter), nameof(ReceiveFromClientPrefix)) { priority = HarmonyLib.Priority.First });
                harmony.Patch(receiveFromServer, prefix: new HarmonyLib.HarmonyMethod(typeof(NetworkModuleAdapter), nameof(ReceiveFromServerPrefix)) { priority = HarmonyLib.Priority.First });
                patchesInstalled = true;
                RefreshLmnNativeDispatchLive();
                Emit("[BUE-V2NET] event=takeover-patch result=installed priority=first targets=NetMessages.ReceiveMessageFromClient,NetMessages.ReceiveMessageFromServer diagnosticId=" + LifecycleDiagnosticId);
            }
            catch (Exception error)
            {
                Emit("[BUE-V2NET] event=takeover-patch result=failed errorType=" + error.GetType().Name + " diagnosticId=" + FaultDiagnosticId);
            }
        }

        /// <summary>
        /// Re-reads both facet switches after a panel edit: the V1 compat
        /// toggle gates only the legacy path; the network module toggle is the
        /// reversible kill switch — off unhooks the takeover (LMN's own prefix
        /// resumes standalone), on re-arms it through the idempotent patch
        /// install.
        /// </summary>
        internal void RefreshSwitches()
        {
            if (isolated) return;
            ApplySwitchesFromSettings();
            coordinator.Refresh();
            UpdateTakeoverStatus();
            if (!networkEnabled)
            {
                UnpatchSelfSafe();
                return;
            }
            if (coordinator.TakeoverActive)
            {
                MirrorLegacyHandlersSafe();
                // DEV-V2-10 R3 (Standards H4): always attempt the (idempotent)
                // install on the active path — a module that STARTED disabled
                // never recorded a patch desire at bootstrap, so a desire-flag
                // guard would silently skip the re-arm the reversible switch
                // promises (handbook B6). ApplyNetworkPatches self-guards on
                // patchesInstalled.
                ApplyNetworkPatches();
                RefreshLmnNativeDispatchLive();
            }
        }

        /// <summary>Full teardown: unhook patches, drop the decision state, clear the static route.</summary>
        internal void IsolateAndDetach()
        {
            if (isolated) return;
            isolated = true;
            UnpatchSelfSafe();
            networkEnabled = false;
            compatLayer.Enabled = false;
            UpdateTakeoverStatus();
            if (ReferenceEquals(ActiveAdapter, this)) ActiveAdapter = null;
            Emit("[BUE-V2NET] event=network-module-isolated decision=stop-and-hand-back diagnosticId=" + LifecycleDiagnosticId);
        }

        /// <summary>
        /// The prefix decision core. Triple gate: network module on + takeover
        /// active + an LMN frame (V1 legacy or LMN2 namespaced; non-LMN frames
        /// always pass through — zero false positive). DEV-V2-12 two-state
        /// contract, per direction:
        /// - LMN's own prefix live (the real takeover state): RELEASE — the
        ///   frame is handed back so LMN's native path dispatches it exactly
        ///   once with its own correct sender resolution. Dispatching here
        ///   too delivered the same frame twice on the real machine (Harmony
        ///   runs all prefixes; this prefix's return-false skips only the
        ///   vanilla method, never LMN's lower-priority prefix).
        /// - LMN's own prefix inert (its patch failed): BUE is the only
        ///   dispatcher — V1 routes through the compat layer (the official
        ///   switch may hand frames back), LMN2 delegates to LMN's router.
        /// </summary>
        internal bool ShouldConsumeInbound(bool fromClient, ulong senderSteamId, byte[] packet, int offset, int size, object connection)
        {
            if (isolated || !networkEnabled || !coordinator.TakeoverActive) return false;
            if (packet == null || offset < 0 || size < 0 || offset + size > packet.Length) return false;
            var nativeDispatchLive = fromClient ? lmnNativeClientDispatchLive : lmnNativeServerDispatchLive;
            if (LmnFrameClassifier.IsLegacyV1Frame(packet, offset, size))
            {
                if (nativeDispatchLive) return ReleaseToNativeDispatchOnce(isV1: true);
                return ConsumeLegacyFrame(fromClient, senderSteamId, packet, offset, size);
            }
            if (LmnFrameClassifier.IsNamespacedV2Frame(packet, offset, size))
            {
                if (nativeDispatchLive) return ReleaseToNativeDispatchOnce(isV1: false);
                return DelegateNamespacedFrame(fromClient, packet, offset, size, connection);
            }
            return false;
        }

        /// <summary>Production log binding: faults are loud, lifecycle/records stay runtime-silent.</summary>
        internal void BindProductionLog()
        {
            DiagnosticLogSink = line =>
            {
                if (line != null && line.Contains("result=failed")) BueRuntimeLog.ErrorFriendly(line);
                else BueRuntimeLog.Runtime(line);
            };
            LmnV1CompatLayer.DiagnosticLogSink = line => BueRuntimeLog.Runtime(line);
        }

        // DEV-V2-12 scope note: in the LIVE world (standalone LMN patched and
        // dispatching) frames reach V1 handlers through LMN's own prefix
        // regardless of the v1compat switch — it was so before this change
        // too (LMN's prefix ran either way); the switch governs BUE's own V1
        // dispatch (the inert world) and BUE-side diagnostics. A true V1 drop
        // in the live world would require neutralizing LMN's dispatch — a
        // named follow-up, deliberately out of scope here.
        private bool ConsumeLegacyFrame(bool fromClient, ulong senderSteamId, byte[] packet, int offset, int size)
        {
            if (!compatLayer.Enabled) return false;
            // DEV-V2-12 (F-E): a client frame whose sender could not be
            // resolved is RELEASED, never dispatched as sender=0 — a 0-sender
            // callback is a behavior-level defect for any legacy plugin that
            // trusts the steam id (the real machine queued pongs to target=0,
            // which LMN's outbound guard then had to reject). Release is NOT
            // a successful delivery: with LMN's native dispatch inert the
            // released frame falls to the vanilla path, which does not know
            // the MOD magic — the frame is deliberately dropped rather than
            // delivered with a false identity. The first such drop emits a
            // one-shot record (P6 boundary anchor).
            if (fromClient && senderSteamId == 0UL)
            {
                if (!unresolvedSenderReleaseRecorded)
                {
                    unresolvedSenderReleaseRecorded = true;
                    Emit("[BUE-V2NET] event=v1-frame-release result=released decision=unresolved-sender diagnosticId=" + LifecycleDiagnosticId);
                }
                return false;
            }
            var frame = new byte[size];
            Buffer.BlockCopy(packet, offset, frame, 0, size);
            return fromClient ? compatLayer.RouteFromClient(frame, senderSteamId) : compatLayer.RouteFromServer(frame);
        }

        private bool DelegateNamespacedFrame(bool fromClient, byte[] packet, int offset, int size, object connection)
        {
            try
            {
                var routerMethod = ResolveRouterMethod(fromClient);
                if (routerMethod == null) return false;
                var args = fromClient
                    ? new object[] { connection, packet, offset, size }
                    : new object[] { packet, offset, size };
                var handled = routerMethod.Invoke(null, args) is bool consumed && consumed;
                if (handled && !delegatedRecorded)
                {
                    delegatedRecorded = true;
                    Emit("[BUE-V2NET] event=lmn2-delegate result=delegated decision=consume");
                }
                return handled;
            }
            catch (Exception error)
            {
                Emit("[BUE-V2NET] event=lmn2-delegate result=failed errorType=" + error.GetType().Name + " decision=hand-back diagnosticId=" + FaultDiagnosticId);
                return false;
            }
        }

        private MethodInfo ResolveRouterMethod(bool fromClient)
        {
            if (!routerResolved)
            {
                routerResolved = true;
                var routerType = resolveModRouterType();
                if (routerType != null)
                {
                    routerClientMethod = FindRouterMethod(routerType, "TryHandleFromClient", 4);
                    routerServerMethod = FindRouterMethod(routerType, "TryHandleFromServer", 3);
                }
            }
            return fromClient ? routerClientMethod : routerServerMethod;
        }

        private static MethodInfo FindRouterMethod(Type routerType, string name, int parameterCount)
        {
            foreach (var candidate in routerType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(candidate.Name, name, StringComparison.Ordinal)) continue;
                var parameters = candidate.GetParameters();
                if (parameters.Length != parameterCount) continue;
                if (parameters[parameterCount - 3].ParameterType != typeof(byte[])) continue;
                if (parameters[parameterCount - 2].ParameterType != typeof(int)) continue;
                if (parameters[parameterCount - 1].ParameterType != typeof(int)) continue;
                return candidate;
            }
            return null;
        }

        // DEV-V2-10 F-A mirror timing state. BepInEx loads plugins by
        // file-name order, so BUE (B) bootstraps before the standalone LMN
        // (L) assembly is loaded: an unresolvable ModTransport type is a
        // TIMING state, not a fault. The mirror defers (one deferred line,
        // throttled silent retries from the plugin tick) instead of failing,
        // keeping P5's zero-false-positive ERROR budget for real faults.
        private bool mirrorPending;
        private int deferredMirrorTicks;
        // DEV-V2-12 R2: throttled re-probe cadence state for the tick path
        // (drives RefreshLmnNativeDispatchLive until LMN's prefixes appear).
        private int lmnProbeTicks;
        // DEV-V2-11 (Spec GAP-1): LMN logs nothing per frame, so a delegated
        // LMN2 frame is indistinguishable from LMN's own prefix path. The
        // first consumed delegation emits a one-shot record so the real
        // -machine retest can prove the delegation actually happens.
        private bool delegatedRecorded;
        // DEV-V2-12: one-shot positive anchors for the RELEASE decision — in
        // the live world a released frame is dispatched once by LMN's native
        // path, so these records replace the delegation/consumption records
        // as the retest's per-kind proof the decision point was exercised.
        private bool v1ReleaseRecorded;
        private bool lmn2ReleaseRecorded;
        // DEV-V2-12 (Spec R2 P6): one-shot record for the inert-world
        // unresolved-sender drop (release is a deliberate non-delivery there).
        private bool unresolvedSenderReleaseRecorded;

        /// <summary>
        /// DEV-V2-12: while LMN's own prefix is live it dispatches every LMN
        /// frame itself, so a BUE dispatch delivered the SAME frame twice
        /// (Harmony runs all prefixes — BUE's Priority.First return-false
        /// skips only the original, never LMN's lower-priority prefix). BUE
        /// releases the frame instead: LMN's native path keeps the single
        /// dispatch with its own correct sender resolution.
        /// </summary>
        private bool ReleaseToNativeDispatchOnce(bool isV1)
        {
            if (isV1)
            {
                if (!v1ReleaseRecorded)
                {
                    v1ReleaseRecorded = true;
                    Emit("[BUE-V2NET] event=v1-frame-release result=released decision=lmn-native-dispatch diagnosticId=" + LifecycleDiagnosticId);
                }
                return false;
            }
            if (!lmn2ReleaseRecorded)
            {
                lmn2ReleaseRecorded = true;
                Emit("[BUE-V2NET] event=lmn2-frame-release result=released decision=lmn-native-dispatch diagnosticId=" + LifecycleDiagnosticId);
            }
            return false;
        }

        /// <summary>
        /// Mirrors the legacy handler table while the takeover is active.
        /// Three outcomes: an unresolvable LMN type defers silently
        /// (result=deferred, retried on the tick cadence); a reachable but
        /// EMPTY table is not a completed mirror either — the legacy plugins
        /// register their channels in their own Awake, after LMN's, so the
        /// mirror stays pending until something actually mirrors; a
        /// resolved-but-foreign table is the only real fault (result=failed,
        /// no retry — legacy frames take the unknown-channel drop path). A
        /// completed mirror never blocks the takeover either way: LMN's own
        /// prefix keeps its self-heal chain.
        /// </summary>
        private void MirrorLegacyHandlersSafe()
        {
            try
            {
                var modTransportType = resolveModTransportType();
                if (modTransportType == null)
                {
                    if (!mirrorPending)
                    {
                        // Log the deferral once per episode; the throttled
                        // retries stay silent until the outcome changes.
                        mirrorPending = true;
                        deferredMirrorTicks = 0;
                        Emit("[BUE-V2NET] event=v1-table-mirror result=deferred reason=lmn-not-ready decision=retry-on-tick diagnosticId=" + FaultDiagnosticId);
                    }
                    return;
                }
                var mirrored = LmnV1TableMirror.MirrorLegacyHandlers(modTransportType, compatRegistry);
                if (mirrored == 0)
                {
                    // The table is reachable but no legacy channel has
                    // registered yet — keep the retry armed and stay silent.
                    mirrorPending = true;
                    deferredMirrorTicks = 0;
                    return;
                }
                var wasDeferred = mirrorPending;
                mirrorPending = false;
                deferredMirrorTicks = 0;
                Emit("[BUE-V2NET] event=v1-table-mirror result=mirrored channels=" + mirrored + (wasDeferred ? " deferred=true" : string.Empty));
            }
            catch (Exception error)
            {
                mirrorPending = false;
                deferredMirrorTicks = 0;
                // A failed mirror never blocks the takeover: legacy frames fall
                // through to the compat layer's unknown-channel drop path.
                Emit("[BUE-V2NET] event=v1-table-mirror result=failed errorType=" + error.GetType().Name + " decision=drop-path diagnosticId=" + FaultDiagnosticId);
            }
        }

        private void RecordEmptyConfigMigration()
        {
            // T6: LMN V5 has no config system — the mapping is empty by
            // design. Deliberately NOT persisted (idempotent empty run).
            configMigrationStatus = "无独立配置可迁移(LMN 无配置文件)";
            Emit("[BUE-V2NET] event=lmn-config-migration result=no-op mapping=empty reason=lmn-has-no-config diagnosticId=" + MigrationDiagnosticId);
        }

        private void ApplySwitchesFromSettings()
        {
            networkEnabled = ReadToggle(networkSettings, NetworkSettingId);
            compatLayer.Enabled = ReadToggle(v1CompatSettings, V1CompatSettingId);
        }

        private void UpdateTakeoverStatus()
        {
            if (isolated) takeoverStatus = "网络模块已隔离(重启游戏恢复)";
            else if (coordinator.TakeoverActive && networkEnabled) takeoverStatus = "已由 BUE 接管";
            else if (coordinator.TakeoverActive) takeoverStatus = "已改回独立 LMN(BUE 网络模块已停用)";
            else takeoverStatus = "独立 LMN 未加载(BUE 网络模块运行中)";
        }

        private void UnpatchSelfSafe()
        {
            if (!patchesInstalled) return;
            patchesInstalled = false;
            try
            {
                harmony.UnpatchSelf();
                Emit("[BUE-V2NET] event=takeover-patch result=removed decision=hand-back-to-lmn diagnosticId=" + LifecycleDiagnosticId);
            }
            catch (Exception error)
            {
                Emit("[BUE-V2NET] event=takeover-unpatch result=failed errorType=" + error.GetType().Name + " diagnosticId=" + FaultDiagnosticId);
            }
        }

        private static bool ReadToggle(SettingsRuntime runtime, string settingId)
        {
            SettingValue value;
            uint revision;
            return runtime.TryGet(settingId, out value, out revision) ? value.Boolean : true;
        }

        private static SettingDescriptor ToggleDescriptor(FeatureId feature, string settingId, int sortOrder)
        {
            return new SettingDescriptor(feature, settingId, settingId, settingId, SettingKind.Toggle, SettingAuthority.ClientLocal,
                SettingValue.Toggle(true), default(SettingValueOption), default(SettingValueOption), default(SettingValueOption),
                new SettingValue[0], 0, null, 1, sortOrder, null, null);
        }

        private static void Emit(string line)
        {
            var sink = DiagnosticLogSink;
            if (sink == null) return;
            try { sink(line); }
            catch (Exception) { }
        }

        // DEV-V2-12 internal for the red-test anchor: the resolution shape is
        // the F-E defect surface. The SDK signature is TryGetSteamId(out
        // ulong) (V2-T1 baseline, AssertSdkNetTransportBaseline pins it) —
        // the resolved id IS the out value. The old code read it through a
        // CSteamID.m_SteamID FieldInfo, which threw ArgumentException on the
        // boxed ulong and swallowed to 0 for EVERY connection (DEV-V2-11
        // retest F-E: every V1 dispatch carried sender=0).
        internal static ulong TryGetConnectionSteamId(SDG.NetTransport.ITransportConnection connection)
        {
            try
            {
                if (connection == null || tryGetSteamIdMethod == null) return 0UL;
                var args = new object[] { null };
                if (!(tryGetSteamIdMethod.Invoke(connection, args) is bool ok) || !ok || args[0] == null) return 0UL;
                return (ulong)args[0];
            }
            catch (Exception)
            {
                return 0UL;
            }
        }

        /// <summary>
        /// DEV-V2-12: refreshes the cached per-direction LMN-native-dispatch
        /// live state. Tests inject the probes; production self-detects by
        /// looking for an LMN-owned prefix on each intercept point (LMN
        /// fail-fasts when either patch fails, but a failed SERVER patch can
        /// leave the CLIENT patch in place — the directions are checked
        /// independently and the live direction releases while the inert
        /// direction keeps BUE as its only dispatcher). Each direction is a
        /// monotonic latch: once a live prefix is seen it stays live (the
        /// standalone LMN never unpatches mid-session), and a latched
        /// direction is never re-probed.
        /// </summary>
        private void RefreshLmnNativeDispatchLive()
        {
            if (!lmnNativeClientDispatchLive) lmnNativeClientDispatchLive = isLmnNativeClientDispatchLive != null ? isLmnNativeClientDispatchLive() : ProductionLmnNativeDispatchLive(clientDirection: true);
            if (!lmnNativeServerDispatchLive) lmnNativeServerDispatchLive = isLmnNativeServerDispatchLive != null ? isLmnNativeServerDispatchLive() : ProductionLmnNativeDispatchLive(clientDirection: false);
        }

        private bool ProductionLmnNativeDispatchLive(bool clientDirection)
        {
            try
            {
                if (!patchesInstalled || netMessagesType == null) return false;
                var receive = AccessTools.Method(netMessagesType, clientDirection ? "ReceiveMessageFromClient" : "ReceiveMessageFromServer");
                if (receive == null) return false;
                return HasLmnOwnedPrefix(receive);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool HasLmnOwnedPrefix(MethodBase method)
        {
            var info = HarmonyLib.Harmony.GetPatchInfo(method);
            if (info == null || info.Prefixes == null) return false;
            foreach (var patch in info.Prefixes)
            {
                if (patch != null && patch.owner == LmnPatchOwner) return true;
            }
            return false;
        }

        // Priority.First prefixes on the shared intercept points (LMN
        // NetMessagesReceiveClientPatch.cs:56 / ServerPatch.cs:55 target the
        // same methods). Direction guards mirror LMN's own (ClientPatch L58 /
        // ServerPatch L57), so each frame is decided exactly once.
        // DEV-V2-12 correction: returning false skips ONLY the vanilla
        // method — Harmony still runs every lower-priority prefix, so LMN's
        // own prefix dispatched the same frame BUE had dispatched. That is
        // why the live state releases frames (LMN's native dispatch is the
        // exactly-once path) instead of consuming them.

        internal static bool ReceiveFromClientPrefix(SDG.NetTransport.ITransportConnection transportConnection, byte[] packet, int offset, int size)
        {
            if (!SDG.Unturned.Provider.isServer) return true;
            var adapter = ActiveAdapter;
            if (adapter == null) return true;
            return !adapter.ShouldConsumeInbound(true, TryGetConnectionSteamId(transportConnection), packet, offset, size, transportConnection);
        }

        internal static bool ReceiveFromServerPrefix(byte[] packet, int offset, int size)
        {
            if (SDG.Unturned.Provider.isServer) return true;
            var adapter = ActiveAdapter;
            if (adapter == null) return true;
            return !adapter.ShouldConsumeInbound(false, 0UL, packet, offset, size, null);
        }
    }
}
