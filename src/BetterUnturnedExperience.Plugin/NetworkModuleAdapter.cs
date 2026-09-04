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
    /// persisted). The Harmony prefixes are the ONLY callers of
    /// <see cref="ShouldConsumeInbound"/> in production; tests drive the
    /// decision core directly and never install patches. Diagnostic ids:
    /// BUE-V2NET-001 = migration record, BUE-V2NET-002 = takeover plumbing
    /// fault (mirror / router delegate / patch failure), BUE-V2NET-003 =
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

        /// <summary>Host wiring binds this to the runtime log; tests capture the lines.</summary>
        internal static Action<string> DiagnosticLogSink = null;

        internal static NetworkModuleAdapter ActiveAdapter { get; private set; }

        private static readonly MethodInfo tryGetSteamIdMethod = AccessTools.Method(typeof(SDG.NetTransport.ITransportConnection), "TryGetSteamId");
        private static readonly FieldInfo steamIdField = ResolveSteamIdField();
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
        private bool networkEnabled = true;
        private string takeoverStatus = string.Empty;
        private string configMigrationStatus = string.Empty;
        private bool patchesInstalled;
        private bool patchesDesired;
        private bool isolated;
        private bool routerResolved;
        private MethodInfo routerClientMethod;
        private MethodInfo routerServerMethod;

        internal NetworkModuleAdapter(string settingsRootPath, Func<bool> isStandaloneLmnLoaded, Func<Type> resolveModTransportType, Func<Type> resolveModRouterType, Action refreshPanel)
        {
            networkSettings = new SettingsRuntime(NetworkFeature, CreateNetworkDescriptors(), new FileSettingsPersistence(settingsRootPath));
            v1CompatSettings = new SettingsRuntime(V1CompatFeature, CreateV1CompatDescriptors(), new FileSettingsPersistence(settingsRootPath));
            compatRegistry = new LmnV1CompatRegistry();
            compatLayer = new LmnV1CompatLayer(compatRegistry);
            coordinator = new LmnTakeoverCoordinator(isStandaloneLmnLoaded ?? throw new ArgumentNullException(nameof(isStandaloneLmnLoaded)));
            this.resolveModTransportType = resolveModTransportType ?? throw new ArgumentNullException(nameof(resolveModTransportType));
            this.resolveModRouterType = resolveModRouterType ?? throw new ArgumentNullException(nameof(resolveModRouterType));
            this.refreshPanel = refreshPanel ?? throw new ArgumentNullException(nameof(refreshPanel));
            ApplySwitchesFromSettings();
            UpdateTakeoverStatus();
        }

        internal SettingsRuntime NetworkSettings { get { return networkSettings; } }
        internal SettingsRuntime V1CompatSettings { get { return v1CompatSettings; } }
        internal bool TakeoverActive { get { return !isolated && networkEnabled && coordinator.TakeoverActive; } }
        internal string TakeoverStatus { get { return takeoverStatus; } }
        internal string ConfigMigrationStatus { get { return configMigrationStatus; } }

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
            if (coordinator.TakeoverActive) MirrorLegacyHandlersSafe();
            RecordEmptyConfigMigration();
            UpdateTakeoverStatus();
            ActiveAdapter = this;
        }

        /// <summary>
        /// Production-only: installs the Priority.First prefixes on the shared
        /// intercept points. Hard zero-false-positive rule — when the probe is
        /// false or the module is switched off nothing is patched and nothing
        /// reflects. Idempotent; remembers the intent so a switch-off/on cycle
        /// can re-arm.
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
                patchesDesired = true;
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
        /// resumes standalone), on re-arms it when the patches were desired.
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
                if (patchesDesired && !patchesInstalled) ApplyNetworkPatches();
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
        /// active + an LMN frame. Legacy V1 frames route through the compat
        /// layer (its official switch may hand them back); LMN2 frames
        /// delegate to LMN's own router — true consumes (the prefix skips the
        /// vanilla method and LMN's prefix), false or a fault hands the frame
        /// back so the vanilla path and LMN's prefix keep their self-heal
        /// chain. Non-LMN frames always pass through (zero false positive).
        /// </summary>
        internal bool ShouldConsumeInbound(bool fromClient, ulong senderSteamId, byte[] packet, int offset, int size, object connection)
        {
            if (isolated || !networkEnabled || !coordinator.TakeoverActive) return false;
            if (packet == null || offset < 0 || size < 0 || offset + size > packet.Length) return false;
            if (LmnFrameClassifier.IsLegacyV1Frame(packet, offset, size)) return ConsumeLegacyFrame(fromClient, senderSteamId, packet, offset, size);
            if (LmnFrameClassifier.IsNamespacedV2Frame(packet, offset, size)) return DelegateNamespacedFrame(fromClient, packet, offset, size, connection);
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

        private bool ConsumeLegacyFrame(bool fromClient, ulong senderSteamId, byte[] packet, int offset, int size)
        {
            if (!compatLayer.Enabled) return false;
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
                return routerMethod.Invoke(null, args) is bool handled && handled;
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

        private void MirrorLegacyHandlersSafe()
        {
            try
            {
                var modTransportType = resolveModTransportType();
                if (modTransportType == null) throw new ArgumentException("the standalone LMN ModTransport type could not be resolved");
                LmnV1TableMirror.MirrorLegacyHandlers(modTransportType, compatRegistry);
            }
            catch (Exception error)
            {
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

        private static FieldInfo ResolveSteamIdField()
        {
            var steamIdType = AccessTools.TypeByName("Steamworks.CSteamID");
            return steamIdType == null ? null : AccessTools.Field(steamIdType, "m_SteamID");
        }

        private static ulong TryGetConnectionSteamId(SDG.NetTransport.ITransportConnection connection)
        {
            try
            {
                if (connection == null || tryGetSteamIdMethod == null || steamIdField == null) return 0UL;
                var args = new object[] { null };
                if (!(tryGetSteamIdMethod.Invoke(connection, args) is bool ok) || !ok || args[0] == null) return 0UL;
                return (ulong)steamIdField.GetValue(args[0]);
            }
            catch (Exception)
            {
                return 0UL;
            }
        }

        // Priority.First prefixes on the shared intercept points (LMN
        // NetMessagesReceiveClientPatch.cs:56 / ServerPatch.cs:55 target the
        // same methods). Direction guards mirror LMN's own (ClientPatch L58 /
        // ServerPatch L57), so each frame is decided exactly once. Returning
        // true lets the vanilla method (and LMN's lower-priority prefix) run;
        // returning false short-circuits both.

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
