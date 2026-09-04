using System;
using System.Collections;
using System.Reflection;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-06: reflection mirror from the standalone LMN V1 handler table
    /// into the BUE compat registry. While the takeover is active, BUE reads
    /// the legacy plugin registrations — the static "ServerHandlers" /
    /// "ClientHandlers" dictionaries keyed by int channel — and re-registers
    /// each one so existing V1 consumers keep working without code changes.
    /// The engine steam id value type is resolved from the handler signature
    /// itself and constructed from its ulong form per dispatch, so the Core
    /// never names the engine type (tests substitute their own shape). A
    /// missing field or shape mismatch throws so the wiring layer can emit
    /// its mirror diagnostic and continue — V1 frames then take the
    /// unknown-channel drop path.
    /// </summary>
    public static class LmnV1TableMirror
    {
        /// <summary>
        /// Mirrors both handler tables. Returns the number of mirrored
        /// handlers; throws when a table field is missing or malformed.
        /// </summary>
        public static int MirrorLegacyHandlers(Type modTransportType, LmnV1CompatRegistry target)
        {
            if (modTransportType == null) throw new ArgumentNullException(nameof(modTransportType));
            if (target == null) throw new ArgumentNullException(nameof(target));
            var mirrored = MirrorHandlerTable(modTransportType, "ServerHandlers", target, mirrorServer: true);
            mirrored += MirrorHandlerTable(modTransportType, "ClientHandlers", target, mirrorServer: false);
            return mirrored;
        }

        private static int MirrorHandlerTable(Type modTransportType, string fieldName, LmnV1CompatRegistry target, bool mirrorServer)
        {
            var field = modTransportType.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(modTransportType.FullName ?? modTransportType.Name, fieldName);
            if (!(field.GetValue(null) is IDictionary table)) throw new ArgumentException("the " + fieldName + " handler table is not a dictionary", nameof(modTransportType));
            var mirrored = 0;
            foreach (DictionaryEntry entry in table)
            {
                if (!(entry.Key is int channel)) continue;
                var handler = entry.Value as Delegate;
                if (handler == null) continue;
                if (mirrorServer) target.RegisterServerHandler(channel, WrapServerHandler(handler));
                else target.RegisterClientHandler(channel, WrapClientHandler(handler));
                mirrored++;
            }
            return mirrored;
        }

        private static Action<ulong, System.IO.BinaryReader> WrapServerHandler(Delegate handler)
        {
            var parameters = handler.Method.GetParameters();
            if (parameters.Length != 2) throw new ArgumentException("the legacy server handler shape is (steamId, reader)", nameof(handler));
            var steamIdType = parameters[0].ParameterType;
            return (ulong sender, System.IO.BinaryReader reader) =>
            {
                var steamId = Activator.CreateInstance(steamIdType, sender);
                handler.DynamicInvoke(steamId, reader);
            };
        }

        private static Action<System.IO.BinaryReader> WrapClientHandler(Delegate handler)
        {
            return reader => handler.DynamicInvoke(reader);
        }
    }
}
