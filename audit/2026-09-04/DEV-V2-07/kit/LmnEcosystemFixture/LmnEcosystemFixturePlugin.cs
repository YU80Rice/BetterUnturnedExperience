using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using BepInEx;
using LaunchMultiplayerNet;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace LmnEcosystemFixture
{
    /// <summary>
    /// DEV-V2-07 evidence fixture. Deliberately an ORDINARY LaunchMultiplayerNet
    /// consumer — it references LMN only, never BetterUnturnedExperience — so the
    /// three-environment network validation exercises the real ecosystem paths:
    /// a legacy V1 numeric-channel consumer (channel 250) and a V2 named-channel
    /// consumer (io.github.yu80rice.bue-fixture.named). Both directions ping/pong
    /// every 10 seconds with sequence numbers, and every step writes one Info log
    /// line so host/client/U3DS logs cross-correlate inside the evidence time
    /// window. Sends happen on the Unity main thread (pong responses are queued
    /// from network callbacks and flushed in Update); every send/receive is
    /// fault-isolated so a fixture failure can never take the game down.
    /// </summary>
    [BepInPlugin("io.github.yu80rice.bue.lmn-fixture", "BUE LMN Ecosystem Fixture", "0.1.0")]
    [BepInDependency("com.yu80rice.launchmultiplayernet", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class LmnEcosystemFixturePlugin : BaseUnityPlugin
    {
        private const int V1Channel = 250;
        private const string NamedChannel = "io.github.yu80rice.bue-fixture.named";
        private const byte KindPing = 1;
        private const byte KindPong = 2;
        private const float SendIntervalSeconds = 10f;

        private struct PendingPong
        {
            public CSteamID Target;
            public bool Named;
            public uint Seq;
        }

        private readonly ConcurrentQueue<PendingPong> pendingPongs = new ConcurrentQueue<PendingPong>();
        private float timer;
        private uint sequence;

        private void Awake()
        {
            var outcomes = new StringBuilder();
            RegisterSafe(outcomes, "v1-server", () => ModTransport.RegisterServerHandler(V1Channel, OnV1Server));
            RegisterSafe(outcomes, "v1-client", () => ModTransport.RegisterClientHandler(V1Channel, OnV1Client));
            RegisterSafe(outcomes, "v2-server", () => ModTransport.RegisterNamedServerHandler(NamedChannel, OnV2Server));
            RegisterSafe(outcomes, "v2-client", () => ModTransport.RegisterNamedClientHandler(NamedChannel, OnV2Client));
            Logger.LogInfo("[LMNFIX] ready fixture=0.1.0 guid=io.github.yu80rice.bue.lmn-fixture lmnOperational=" + ModTransport.IsOperational
                + " channels=" + outcomes);
        }

        private void Update()
        {
            FlushPongs();
            timer += Time.deltaTime;
            if (timer < SendIntervalSeconds) return;
            timer = 0f;
            SendPeriodicPings();
        }

        private void RegisterSafe(StringBuilder outcomes, string name, System.Action register)
        {
            try
            {
                register();
                outcomes.Append(name).Append("=ok;");
            }
            catch (Exception error)
            {
                outcomes.Append(name).Append("=fault:").Append(error.GetType().Name).Append(';');
                Logger.LogWarning("[LMNFIX] register-fault what=" + name + " errorType=" + error.GetType().Name + " message=" + error.Message);
            }
        }

        private void SendPeriodicPings()
        {
            try
            {
                sequence++;
                if (Provider.isServer)
                {
                    // Host/U3DS/single-player: broadcast to remote clients and the
                    // local loopback face (LMN routes loopback through its router).
                    ModTransport.BroadcastToAllClients(V1Channel, BuildPayload(KindPing, sequence), true);
                    Logger.LogInfo("[V1FIX] send-broadcast server->clients seq=" + sequence);
                    ModTransport.BroadcastNamedToAllClients(NamedChannel, BuildPayload(KindPing, sequence), true);
                    Logger.LogInfo("[V2FIX] send-broadcast server->clients seq=" + sequence);
                }
                if (Provider.isClient && !Provider.isServer)
                {
                    // Remote client: ping the server on both protocol paths.
                    ModTransport.SendToServer(V1Channel, BuildPayload(KindPing, sequence), true);
                    Logger.LogInfo("[V1FIX] send-to-server seq=" + sequence);
                    ModTransport.SendNamedToServer(NamedChannel, BuildPayload(KindPing, sequence), true);
                    Logger.LogInfo("[V2FIX] send-to-server seq=" + sequence);
                }
            }
            catch (Exception error)
            {
                Logger.LogWarning("[LMNFIX] send-fault errorType=" + error.GetType().Name + " message=" + error.Message);
            }
        }

        private void FlushPongs()
        {
            while (pendingPongs.TryDequeue(out var pong))
            {
                try
                {
                    if (pong.Named)
                    {
                        ModTransport.SendNamedToClient(pong.Target, NamedChannel, BuildPayload(KindPong, pong.Seq), true);
                        Logger.LogInfo("[V2FIX] send-pong server->client target=" + pong.Target.m_SteamID + " seq=" + pong.Seq);
                    }
                    else
                    {
                        ModTransport.SendToClient(pong.Target, V1Channel, BuildPayload(KindPong, pong.Seq), true);
                        Logger.LogInfo("[V1FIX] send-pong server->client target=" + pong.Target.m_SteamID + " seq=" + pong.Seq);
                    }
                }
                catch (Exception error)
                {
                    Logger.LogWarning("[LMNFIX] pong-fault errorType=" + error.GetType().Name + " message=" + error.Message);
                }
            }
        }

        private void OnV1Server(CSteamID sender, BinaryReader reader) { HandleInbound(true, false, sender, reader); }
        private void OnV1Client(BinaryReader reader) { HandleInbound(false, false, default(CSteamID), reader); }
        private void OnV2Server(CSteamID sender, BinaryReader reader) { HandleInbound(true, true, sender, reader); }
        private void OnV2Client(BinaryReader reader) { HandleInbound(false, true, default(CSteamID), reader); }

        private void HandleInbound(bool fromRemoteClient, bool named, CSteamID sender, BinaryReader reader)
        {
            var tag = named ? "[V2FIX]" : "[V1FIX]";
            try
            {
                var kind = reader.ReadByte();
                var seq = reader.ReadUInt32();
                if (fromRemoteClient)
                {
                    Logger.LogInfo(tag + " recv-from-client sender=" + sender.m_SteamID + " kind=" + KindText(kind) + " seq=" + seq);
                    if (kind == KindPing) pendingPongs.Enqueue(new PendingPong { Target = sender, Named = named, Seq = seq });
                }
                else
                {
                    Logger.LogInfo(tag + " recv-from-server kind=" + KindText(kind) + " seq=" + seq);
                }
            }
            catch (Exception error)
            {
                Logger.LogWarning("[LMNFIX] inbound-fault " + tag + " errorType=" + error.GetType().Name);
            }
        }

        private static string KindText(byte kind) { return kind == KindPing ? "ping" : kind == KindPong ? "pong" : "unknown(" + kind + ")"; }

        private static byte[] BuildPayload(byte kind, uint seq)
        {
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            {
                writer.Write(kind);
                writer.Write(seq);
                writer.Flush();
                return memory.ToArray();
            }
        }
    }
}
