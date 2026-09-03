using System;
using System.Collections.Generic;
using System.IO;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-05: host-internal registry that mimics the LMN V1
    /// numeric-channel registration semantics so existing V1 consumers keep
    /// working without code changes (LMN ModTransport.cs:137
    /// RegisterServerHandler(int, ...), :159 RegisterClientHandler(int, ...),
    /// :186/:216 unregister). This is NOT a new V1 registration entry point:
    /// it only backs the compatibility path for already-registered legacy
    /// consumers — the production binding that lands legacy registration
    /// calls here is wired separately (DEV-V2-06). The sender's 64-bit steam
    /// id is carried in its ulong form so the Core stays pure C#; the
    /// engine-side conversion stays at the wiring layer. Re-registering a
    /// channel replaces the previous handler (the V1 handler table is keyed
    /// by channel); unregistering an absent handler is a no-op.
    /// </summary>
    public sealed class LmnV1CompatRegistry
    {
        private readonly Dictionary<int, Action<ulong, BinaryReader>> serverHandlers =
            new Dictionary<int, Action<ulong, BinaryReader>>();
        private readonly Dictionary<int, Action<BinaryReader>> clientHandlers =
            new Dictionary<int, Action<BinaryReader>>();

        public void RegisterServerHandler(int channel, Action<ulong, BinaryReader> handler)
        {
            ValidateLegacyChannel(channel);
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            serverHandlers[channel] = handler;
        }

        public void RegisterClientHandler(int channel, Action<BinaryReader> handler)
        {
            ValidateLegacyChannel(channel);
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            clientHandlers[channel] = handler;
        }

        public void UnregisterServerHandler(int channel)
        {
            if (IsLegacyChannel(channel)) serverHandlers.Remove(channel);
        }

        public void UnregisterClientHandler(int channel)
        {
            if (IsLegacyChannel(channel)) clientHandlers.Remove(channel);
        }

        /// <summary>
        /// Dispatches a server-side inbound payload to the channel's registered
        /// server handler. Returns false when no handler is registered — the
        /// caller decides whether to drop the frame with a diagnostic.
        /// </summary>
        public bool DispatchServer(int channel, ulong senderSteamId, byte[] payload)
        {
            if (!IsLegacyChannel(channel)) return false;
            Action<ulong, BinaryReader> handler;
            if (!serverHandlers.TryGetValue(channel, out handler)) return false;
            using (var reader = new BinaryReader(new MemoryStream(payload ?? new byte[0])))
            {
                handler(senderSteamId, reader);
            }
            return true;
        }

        /// <summary>Client-side counterpart of <see cref="DispatchServer"/>.</summary>
        public bool DispatchClient(int channel, byte[] payload)
        {
            if (!IsLegacyChannel(channel)) return false;
            Action<BinaryReader> handler;
            if (!clientHandlers.TryGetValue(channel, out handler)) return false;
            using (var reader = new BinaryReader(new MemoryStream(payload ?? new byte[0])))
            {
                handler(reader);
            }
            return true;
        }

        private static void ValidateLegacyChannel(int channel)
        {
            if (!IsLegacyChannel(channel))
                throw new ArgumentOutOfRangeException(nameof(channel), channel,
                    "the legacy V1 virtual channel must be within 0..255");
        }

        private static bool IsLegacyChannel(int channel)
        {
            return channel >= 0 && channel <= 255;
        }
    }
}
