using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SDG.NetTransport;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// DEV-V2-18: the engine-facing transport binding for the BUE network
    /// runtime — five engine resolvers plus an optional injected clock, all
    /// behind delegates so the runtime wiring stays host-testable (tests bind
    /// stubs; production binds the silent-reflection resolvers in
    /// <see cref="BueEngineNet"/> via <see cref="Production"/>). The
    /// production resolvers NEVER THROW (R1-Standards fix): every
    /// engine-facing failure — a type that cannot resolve, a reflection
    /// miss, a game send crash — degrades to a safe default (false / 0 /
    /// empty / send-failed), the same swallow-to-default contract as the V1
    /// takeover's TryGetConnectionSteamId. Pump stages and the re-arm path
    /// keep their own isolation on top for non-binding faults.
    /// </summary>
    internal sealed class BueEngineNetBinding
    {
        internal BueEngineNetBinding(Func<bool> isServer, Func<ulong> localSteamId, Func<IReadOnlyList<ulong>> serverPeers, Func<ulong> clientPeer, Func<byte[], bool, ulong, bool> send, Func<long> monotonicMilliseconds = null)
        {
            IsServer = isServer ?? throw new ArgumentNullException(nameof(isServer));
            LocalSteamId = localSteamId ?? throw new ArgumentNullException(nameof(localSteamId));
            ServerPeers = serverPeers ?? throw new ArgumentNullException(nameof(serverPeers));
            ClientPeer = clientPeer ?? throw new ArgumentNullException(nameof(clientPeer));
            Send = send ?? throw new ArgumentNullException(nameof(send));
            MonotonicMilliseconds = monotonicMilliseconds;
        }

        /// <summary>Server role truth (U3DS / listen host = true).</summary>
        internal Func<bool> IsServer { get; }
        /// <summary>The local steam id carried as the frame-header sender.</summary>
        internal Func<ulong> LocalSteamId { get; }
        /// <summary>Server side: current connected remote players' steam ids.</summary>
        internal Func<IReadOnlyList<ulong>> ServerPeers { get; }
        /// <summary>Client side: the connected server's steam id, 0 = disconnected/menu.</summary>
        internal Func<ulong> ClientPeer { get; }
        /// <summary>Engine send: target 0 = client→server (untargeted), otherwise server→client (targeted).</summary>
        internal Func<byte[], bool, ulong, bool> Send { get; }
        /// <summary>Null = the runtime's default monotonic clock (handshake backoff).</summary>
        internal Func<long> MonotonicMilliseconds { get; }

        /// <summary>Production binding (BueEngineNet silent-reflection resolvers).</summary>
        internal static BueEngineNetBinding Production { get; } = new BueEngineNetBinding(BueEngineNet.IsServer, BueEngineNet.LocalSteamId, BueEngineNet.ServerPeers, BueEngineNet.ClientPeer, BueEngineNet.Send);
    }

    /// <summary>
    /// DEV-V2-18 production engine resolvers. Silent reflection only — game
    /// types resolve BY NAME through <see cref="NetworkModuleFeatureRegistration.TryFindLoadedType"/>
    /// (never AccessTools.TypeByName, whose per-miss HarmonyX warning spammed
    /// real sessions; never compile-time SDG.Unturned types, keeping the
    /// no-Steamworks/no-Assembly-CSharp reference policy intact), and an
    /// unresolvable engine degrades to safe defaults instead of throwing.
    /// Wire truth mirrors the standalone LMN source (authority:
    /// LaunchMultiplayerNet/Core/NetReflectionHelper.cs + Routing/ModTransport.cs):
    /// client→server rides Provider.clientTransport (IClientTransport.Send,
    /// ENetReliability); server→client rides SteamPlayer.transportConnection
    /// (ITransportConnection.Send); peer ids resolve through the CSteamID
    /// m_SteamID field the same way the V1 takeover resolves TryGetSteamId.
    /// SDG.NetTransport interfaces (IClientTransport/ITransportConnection/
    /// ENetReliability) ARE compile-time — the plugin references that
    /// assembly already.
    /// </summary>
    internal static class BueEngineNet
    {
        private const string ProviderTypeName = "SDG.Unturned.Provider";
        private const string SteamPlayerTypeName = "SDG.Unturned.SteamPlayer";

        private static readonly IReadOnlyList<ulong> EmptyPeers = new ulong[0];
        // Resolution latch: the flag is set BEFORE the fields populate, so a
        // second entry sees `resolved` true and skips — a concurrent second
        // entrant could observe half-populated fields, which is why Resolve
        // is only reached from the main-thread pump and the send path (the
        // declared main-thread model; no off-main send is claimed). No
        // static initializer touches game types, so a non-game host stays
        // loadable and resolves to safe defaults (R2 SMELL note fix).
        private static bool resolved;
        private static PropertyInfo providerIsServerProperty;      // public static bool Provider.isServer
        private static PropertyInfo providerIsConnectedProperty;   // public static bool Provider.isConnected
        private static PropertyInfo providerServerProperty;        // public static CSteamID Provider.server
        private static PropertyInfo providerClientProperty;        // public static CSteamID Provider.client
        private static PropertyInfo providerClientsProperty;       // public static List<SteamPlayer> Provider.clients
        private static FieldInfo providerClientTransportField;     // internal static IClientTransport Provider.clientTransport (LMN NetReflectionHelper.cs:31)
        private static PropertyInfo steamPlayerIdProperty;         // public SteamPlayerID SteamPlayer.playerID
        private static PropertyInfo steamPlayerIdSteamIdProperty;  // public CSteamID SteamPlayerID.steamID
        private static PropertyInfo transportConnectionProperty;   // public ITransportConnection SteamPlayer.transportConnection (SteamConnectedClientBase; LMN NetReflectionHelper.cs:73)
        private static FieldInfo steamIdRawField;                  // public ulong CSteamID.m_SteamID

        private static void Resolve()
        {
            if (resolved) return;
            resolved = true;
            var providerType = NetworkModuleFeatureRegistration.TryFindLoadedType(ProviderTypeName);
            if (providerType == null) return;
            providerIsServerProperty = providerType.GetProperty("isServer", BindingFlags.Public | BindingFlags.Static);
            providerIsConnectedProperty = providerType.GetProperty("isConnected", BindingFlags.Public | BindingFlags.Static);
            providerServerProperty = providerType.GetProperty("server", BindingFlags.Public | BindingFlags.Static);
            providerClientProperty = providerType.GetProperty("client", BindingFlags.Public | BindingFlags.Static);
            providerClientsProperty = providerType.GetProperty("clients", BindingFlags.Public | BindingFlags.Static);
            providerClientTransportField = providerType.GetField("clientTransport", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            // The CSteamID type is reached through the property's runtime
            // signature — no compile-time Steamworks reference needed.
            var csteamIdType = providerServerProperty != null ? providerServerProperty.PropertyType : null;
            if (csteamIdType != null) steamIdRawField = csteamIdType.GetField("m_SteamID", BindingFlags.Public | BindingFlags.Instance);
            var steamPlayerType = NetworkModuleFeatureRegistration.TryFindLoadedType(SteamPlayerTypeName);
            if (steamPlayerType == null) return;
            steamPlayerIdProperty = steamPlayerType.GetProperty("playerID", BindingFlags.Public | BindingFlags.Instance);
            if (steamPlayerIdProperty != null)
            {
                var steamPlayerIdType = steamPlayerIdProperty.PropertyType;
                steamPlayerIdSteamIdProperty = steamPlayerIdType.GetProperty("steamID", BindingFlags.Public | BindingFlags.Instance);
            }
            transportConnectionProperty = steamPlayerType.GetProperty("transportConnection", BindingFlags.Public | BindingFlags.Instance);
        }

        // F-E: engine identity plausibility + peer identity decisions (pure;
        // truth tables pinned by --bue-v2-fe-red). Steam64 individual accounts
        // occupy [76561197960265728, +2^32); FakeIP self-assigned ids (the
        // U3DS-without-GSLT case, e.g. 90292445196063768) and zero fall
        // outside — the initiator and the responder must converge on the same
        // session bookkeeping key or the initiator never arms (the key never
        // rides the wire: client→server sends are untargeted over the client
        // transport pipe, server→client targets the client's real id).
        internal const ulong Steam64Base = 76561197960265728UL;
        internal const ulong PlaceholderServerPeerId = 0xB0E0000000000001UL;

        internal static bool SteamIdPlausible(ulong raw)
        {
            return raw >= Steam64Base && raw - Steam64Base <= 4294967295UL;
        }

        internal static ulong ClientPeerDecision(bool connected, bool isServer, ulong providerServerRaw)
        {
            if (!connected || isServer) return 0UL;
            return SteamIdPlausible(providerServerRaw) ? providerServerRaw : PlaceholderServerPeerId;
        }

        internal static ulong LocalSteamIdDecision(bool isServer, ulong providerSelfRaw)
        {
            if (!isServer) return providerSelfRaw;
            return SteamIdPlausible(providerSelfRaw) ? providerSelfRaw : PlaceholderServerPeerId;
        }

        internal static bool IsServer()
        {
            try
            {
                Resolve();
                if (providerIsServerProperty == null) return false;
                return providerIsServerProperty.GetValue(null) is bool isServer && isServer;
            }
            catch (Exception) { return false; }
        }

        internal static ulong LocalSteamId()
        {
            try
            {
                Resolve();
                var isServer = IsServer();
                var selfProperty = isServer ? providerServerProperty : providerClientProperty;
                return LocalSteamIdDecision(isServer, SteamIdOfProviderValue(selfProperty));
            }
            catch (Exception) { return 0UL; }
        }

        internal static IReadOnlyList<ulong> ServerPeers()
        {
            try
            {
                Resolve();
                var clients = ReadClients();
                if (clients == null) return EmptyPeers;
                var ids = new List<ulong>();
                foreach (var client in clients)
                {
                    var id = SteamIdOfPlayer(client);
                    if (id != 0UL) ids.Add(id);
                }
                return ids;
            }
            catch (Exception) { return EmptyPeers; }
        }

        internal static ulong ClientPeer()
        {
            try
            {
                Resolve();
                var connected = providerIsConnectedProperty != null && providerIsConnectedProperty.GetValue(null) is bool c && c;
                return ClientPeerDecision(connected, IsServer(), SteamIdOfProviderValue(providerServerProperty));
            }
            catch (Exception) { return 0UL; }
        }

        internal static bool Send(byte[] frame, bool reliable, ulong targetSteamId)
        {
            try
            {
                if (frame == null || frame.Length == 0) return false;
                Resolve();
                var reliability = reliable ? ENetReliability.Reliable : ENetReliability.Unreliable;
                if (targetSteamId == 0UL)
                {
                    // Client→server: one untargeted raw packet over the client
                    // transport (the same pipe the ReceiveMessageFromServer
                    // prefix intercepts on the server side).
                    if (providerClientTransportField == null) return false;
                    var transport = providerClientTransportField.GetValue(null) as IClientTransport;
                    if (transport == null) return false;
                    transport.Send(frame, frame.Length, reliability);
                    return true;
                }
                // Server→client: one targeted raw packet over the peer's
                // transport connection.
                var connection = FindTransportConnection(targetSteamId);
                if (connection == null) return false;
                connection.Send(frame, frame.Length, reliability);
                return true;
            }
            catch (Exception) { return false; } // a game send crash is one failed send, never a pump fault
        }

        private static IEnumerable ReadClients()
        {
            if (providerClientsProperty == null) return null;
            return providerClientsProperty.GetValue(null) as IEnumerable;
        }

        private static ulong SteamIdOfProviderValue(PropertyInfo property)
        {
            if (property == null || steamIdRawField == null) return 0UL;
            var value = property.GetValue(null);
            if (value == null) return 0UL;
            return steamIdRawField.GetValue(value) is ulong id ? id : 0UL;
        }

        private static ulong SteamIdOfPlayer(object player)
        {
            if (player == null || steamPlayerIdProperty == null || steamPlayerIdSteamIdProperty == null || steamIdRawField == null) return 0UL;
            var playerId = steamPlayerIdProperty.GetValue(player);
            if (playerId == null) return 0UL;
            var value = steamPlayerIdSteamIdProperty.GetValue(playerId);
            if (value == null) return 0UL;
            return steamIdRawField.GetValue(value) is ulong id ? id : 0UL;
        }

        private static ITransportConnection FindTransportConnection(ulong targetSteamId)
        {
            var clients = ReadClients();
            if (clients == null || steamPlayerIdProperty == null || transportConnectionProperty == null) return null;
            foreach (var client in clients)
            {
                if (client == null) continue;
                if (SteamIdOfPlayer(client) != targetSteamId) continue;
                return transportConnectionProperty.GetValue(client) as ITransportConnection;
            }
            return null;
        }

        /// <summary>
        /// DEV-V2-21: resolves the connected client's SteamPlayer OBJECT by
        /// steam id (the LIT authority resolves the requesting peer's Player
        /// through it). Silent reflection, same resolver chain as the send
        /// path; null when the peer is not connected or the engine type is
        /// unavailable. CSteamID is never named — the plugin keeps its
        /// zero-Steamworks-compile-reference policy.
        /// </summary>
        internal static object FindSteamPlayer(ulong steamId)
        {
            try
            {
                Resolve();
                var clients = ReadClients();
                if (clients == null || steamPlayerIdProperty == null || steamIdRawField == null) return null;
                foreach (var client in clients)
                {
                    if (client == null) continue;
                    if (SteamIdOfPlayer(client) == steamId) return client;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
