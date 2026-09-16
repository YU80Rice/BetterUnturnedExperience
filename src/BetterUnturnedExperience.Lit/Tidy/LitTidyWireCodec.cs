using System;
using System.Collections.Generic;
using System.IO;

// DEV-V2-21: the LIT multiplayer wire protocol — feature-private by design
// (spec「保留为功能私有」: payload byte layout and version envelope never
// enter the contract). Migrated from the retired standalone plugin
// (author: YU80Rice, MIT; attribution docs/third-party/LaunchInventoryTidy-
// attribution.md) with the transport swapped: LMN ModTransport named-channel
// framing is replaced by self-built byte[] payloads carried over the BUE
// named channel (FeatureId). Message kinds, field order and the V3 protocol
// semantics (64-bit session token, 7-byte hotkey mapping) are preserved so
// the 08 kit behavior baseline holds.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>One hotkey migration entry on the wire (7 bytes, frozen ABI).</summary>
    internal struct LitNewPositionMapping
    {
        public byte HotkeyIndex;
        public byte NewPage;
        public byte NewX;
        public byte NewY;
        public ushort ExpectedItemId;

        public LitNewPositionMapping(byte hotkeyIndex, byte newPage, byte newX, byte newY, ushort expectedItemId)
        { HotkeyIndex = hotkeyIndex; NewPage = newPage; NewX = newX; NewY = newY; ExpectedItemId = expectedItemId; }
    }

    /// <summary>
    /// The feature-private message kinds and their codecs (five migrated +
    /// the DEV-V5-03 container pair: MsgRequestContainerTidy / MsgContainerTidyResult). Envelope:
    /// [NamedPayloadVersion=1][msgType][body…]. All integers little-endian
    /// (BinaryWriter default). Every decode validates the exact byte shape —
    /// short packets, reserved-byte violations and trailing data fail closed.
    /// </summary>
    internal static class LitTidyWireCodec
    {
        internal const byte PayloadVersion = 1;
        internal const byte ProtocolVersionV3 = 3;
        internal const byte MsgRequestTidyV2 = 2;        // client → server
        internal const byte MsgTidyCommitted = 3;        // server → client
        internal const byte MsgHotkeyFlowAck = 4;        // client → server (semantics: hotkey flow ack)
        internal const byte MsgTidyHotkeyResult = 5;     // server → client
        internal const byte MsgSessionChallenge = 6;     // server → client
        internal const byte MsgRequestContainerTidy = 7; // client → server (DEV-V5-03)
        internal const byte MsgContainerTidyResult = 8;  // server → client (DEV-V5-03)
        internal const byte MsgRequestFastTransferRecover = 9; // client → server (DEV-V5-05)
        internal const byte MsgFastTransferRecoverResult = 10; // server → client (DEV-V5-05)
        internal const int MappingWireSize = 7;          // hotkey(1)+page(1)+x(1)+y(1)+id(2)+reserved(1)
        internal const byte HotkeyCountLimit = (byte)HotkeySnapshotUtil.HOTKEY_COUNT;

        // ── encode ─────────────────────────────────────────────────

        internal static byte[] BuildSessionChallenge(ulong token)
        {
            using (var stream = new MemoryStream(1 + 1 + 8))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgSessionChallenge);
                w.Write(token);
                return stream.ToArray();
            }
        }

        internal static byte[] BuildTidyRequest(ulong sessionToken, uint requestId, byte page, TidyMode mode, bool sortDescending, List<HotkeySnapshot> hotkeys)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgRequestTidyV2);
                w.Write(ProtocolVersionV3);
                w.Write(sessionToken);
                w.Write(requestId);
                w.Write(page);
                w.Write((byte)mode);
                w.Write(sortDescending);
                var count = hotkeys == null ? 0 : hotkeys.Count;
                if (count > HotkeyCountLimit) throw new ArgumentException("hotkey snapshot count exceeds the wire limit", nameof(hotkeys));
                w.Write((byte)count);
                for (int i = 0; i < count; i++)
                {
                    w.Write(hotkeys[i].HotkeyIndex);
                    w.Write(hotkeys[i].ExpectedItemId);
                    w.Write(hotkeys[i].OldPage);
                    w.Write(hotkeys[i].OldX);
                    w.Write(hotkeys[i].OldY);
                }
                return stream.ToArray();
            }
        }

        internal static byte[] BuildTidyCommitted(ulong sessionToken, uint requestId, TidyCommitResult result, List<LitNewPositionMapping> mappings)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgTidyCommitted);
                w.Write(sessionToken);
                w.Write(requestId);
                w.Write((byte)result);
                var count = mappings == null ? 0 : mappings.Count;
                if (count > byte.MaxValue) count = byte.MaxValue;
                w.Write((byte)count);
                for (int i = 0; i < count; i++) WriteMapping(w, mappings[i]);
                return stream.ToArray();
            }
        }

        internal static byte[] BuildHotkeyFlowAck(ulong sessionToken, uint requestId)
        {
            using (var stream = new MemoryStream(1 + 1 + 8 + 4))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgHotkeyFlowAck);
                w.Write(sessionToken);
                w.Write(requestId);
                return stream.ToArray();
            }
        }

        internal static byte[] BuildTidyHotkeyResult(ulong sessionToken, uint requestId, byte restoredCount, byte clearedCount, byte failedCount, byte verifiedCount, List<byte> failedIndices)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgTidyHotkeyResult);
                w.Write(sessionToken);
                w.Write(requestId);
                w.Write(restoredCount);
                w.Write(clearedCount);
                w.Write(failedCount);
                w.Write(verifiedCount);
                if (failedIndices != null)
                {
                    for (int i = 0; i < failedIndices.Count && i < failedCount; i++) w.Write(failedIndices[i]);
                }
                return stream.ToArray();
            }
        }

        // ── DEV-V5-03 container tidy (feature-private kinds 7/8) ──────
        // The request binds KIND + CONTENT FINGERPRINT + the session token —
        // never a page number (the seventh page is a mount position, not an
        // identity, so it never enters the frame). The result carries the
        // frozen TidyCommitResult byte plus a STRUCTURED reason byte so a
        // refusal can always answer in words (no silent no-op).

        /// <summary>Request body: [proto][token][requestId][kind][fingerprint][desc] — 23 bytes.</summary>
        internal static byte[] BuildContainerTidyRequest(ulong sessionToken, uint requestId, byte kind, ulong fingerprint, bool sortDescending)
        {
            using (var stream = new MemoryStream(1 + 1 + 1 + 8 + 4 + 1 + 8 + 1))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgRequestContainerTidy);
                w.Write(ProtocolVersionV3);
                w.Write(sessionToken);
                w.Write(requestId);
                w.Write(kind);
                w.Write(fingerprint);
                w.Write(sortDescending);
                return stream.ToArray();
            }
        }

        /// <summary>Result body: [token][requestId][result][reasonCode] — 14 bytes.</summary>
        internal static byte[] BuildContainerTidyResult(ulong sessionToken, uint requestId, TidyCommitResult result, byte reasonCode)
        {
            using (var stream = new MemoryStream(1 + 1 + 8 + 4 + 1 + 1))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgContainerTidyResult);
                w.Write(sessionToken);
                w.Write(requestId);
                w.Write((byte)result);
                w.Write(reasonCode);
                return stream.ToArray();
            }
        }

        // ── DEV-V5-05 fast-transfer recover (feature-private kinds 9/10) ──
        // The request binds the SOURCE SLOT (page/x/y as the client saw it —
        // the authority re-reads the real jar) plus the SAME 03 container
        // identity claim (kind + content fingerprint): no page-7-as-identity,
        // no item payload, no requester field (the session is the identity).
        // The result reuses the frozen TidyCommitResult byte + the 03
        // structured reason byte (8 causes, no new enum).

        /// <summary>Request body: [proto][token][requestId][sourcePage][sourceX]
        /// [sourceY][kind][fingerprint][desc] — 26 bytes.</summary>
        internal static byte[] BuildFastTransferRequest(ulong sessionToken, uint requestId,
            byte sourcePage, byte sourceX, byte sourceY, byte kind, ulong fingerprint, bool sortDescending)
        {
            using (var stream = new MemoryStream(1 + 1 + 1 + 8 + 4 + 1 + 1 + 1 + 1 + 8 + 1))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgRequestFastTransferRecover);
                w.Write(ProtocolVersionV3);
                w.Write(sessionToken);
                w.Write(requestId);
                w.Write(sourcePage);
                w.Write(sourceX);
                w.Write(sourceY);
                w.Write(kind);
                w.Write(fingerprint);
                w.Write(sortDescending);
                return stream.ToArray();
            }
        }

        /// <summary>Result body: [token][requestId][result][reasonCode] — 14 bytes
        /// (the same frozen pair law as the container result).</summary>
        internal static byte[] BuildFastTransferResult(ulong sessionToken, uint requestId, TidyCommitResult result, byte reasonCode)
        {
            using (var stream = new MemoryStream(1 + 1 + 8 + 4 + 1 + 1))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(PayloadVersion);
                w.Write(MsgFastTransferRecoverResult);
                w.Write(sessionToken);
                w.Write(requestId);
                w.Write((byte)result);
                w.Write(reasonCode);
                return stream.ToArray();
            }
        }

        // ── envelope ────────────────────────────────────────────────

        /// <summary>Envelope gate: payload present, at least [version][type], version == 1. Body is the remainder.</summary>
        internal static bool TryReadEnvelope(byte[] payload, out byte msgType, out byte[] body)
        {
            msgType = 0;
            body = null;
            if (payload == null || payload.Length < 2 || payload[0] != PayloadVersion) return false;
            msgType = payload[1];
            body = new byte[payload.Length - 2];
            Buffer.BlockCopy(payload, 2, body, 0, body.Length);
            return true;
        }

        // ── decode ──────────────────────────────────────────────────

        internal static bool TryReadSessionChallenge(byte[] body, out ulong token)
        {
            token = 0;
            if (body == null || body.Length != 8) return false;
            token = BitConverter.ToUInt64(body, 0);
            return true;
        }

        internal static bool TryReadTidyRequest(byte[] body, out ulong sessionToken, out uint requestId, out byte page, out TidyMode mode, out bool sortDescending, out List<HotkeySnapshot> hotkeys)
        {
            sessionToken = 0; requestId = 0; page = 0; mode = default(TidyMode); sortDescending = false; hotkeys = null;
            if (body == null || body.Length < 1 + 8 + 4 + 1 + 1 + 1 + 1) return false;
            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                var version = r.ReadByte();
                sessionToken = r.ReadUInt64();
                requestId = r.ReadUInt32();
                page = r.ReadByte();
                var modeByte = r.ReadByte();
                sortDescending = r.ReadBoolean();
                var hotkeyCount = r.ReadByte();
                if (version != ProtocolVersionV3) return false;
                if (sessionToken == 0UL) return false;
                if (modeByte > 2) return false;
                if (page != LitRuntime.AllPages && (page < HotkeySnapshotUtil.TIDYABLE_PAGE_MIN || page > HotkeySnapshotUtil.TIDYABLE_PAGE_MAX)) return false;
                if (hotkeyCount > HotkeyCountLimit) return false;
                if (r.BaseStream.Position + hotkeyCount * 6L != body.Length) return false;
                hotkeys = new List<HotkeySnapshot>(hotkeyCount);
                for (int i = 0; i < hotkeyCount; i++)
                {
                    var hi = r.ReadByte();
                    var itemId = r.ReadUInt16();
                    var p = r.ReadByte();
                    var x = r.ReadByte();
                    var y = r.ReadByte();
                    hotkeys.Add(new HotkeySnapshot(hi, itemId, p, x, y));
                }
                mode = (TidyMode)modeByte;
                return true;
            }
        }

        /// <summary>DEV-V5-03: decode one container tidy request — exact
        /// shape or the frame does not exist (fail-closed like every other
        /// kind). kind must be a SUPPORTED claim (1..2: 种类声明即身份面，
        /// 无页号、无哨兵); the fingerprint is the claimed content version.</summary>
        internal static bool TryReadContainerTidyRequest(byte[] body, out ulong sessionToken, out uint requestId, out byte kind, out ulong fingerprint, out bool sortDescending)
        {
            sessionToken = 0; requestId = 0; kind = 0; fingerprint = 0; sortDescending = false;
            if (body == null || body.Length != 1 + 8 + 4 + 1 + 8 + 1) return false;
            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                var version = r.ReadByte();
                sessionToken = r.ReadUInt64();
                requestId = r.ReadUInt32();
                kind = r.ReadByte();
                fingerprint = r.ReadUInt64();
                sortDescending = r.ReadBoolean();
                if (version != ProtocolVersionV3) return false;
                if (sessionToken == 0UL) return false;
                if (requestId == 0u) return false;
                if (kind != (byte)LitContainerTidyKind.WorldContainer && kind != (byte)LitContainerTidyKind.VehicleTrunk) return false;
                return true;
            }
        }

        /// <summary>DEV-V5-03: decode one container tidy result. result must
        /// be a frozen TidyCommitResult byte, the reason a frozen wire byte,
        /// and the pair must not contradict (Committed ⇔ reason None — 成功不
        /// 携带失败文案).</summary>
        internal static bool TryReadContainerTidyResult(byte[] body, out ulong sessionToken, out uint requestId, out TidyCommitResult result, out byte reasonCode)
        {
            sessionToken = 0; requestId = 0; result = TidyCommitResult.Rejected; reasonCode = 0;
            if (body == null || body.Length != 8 + 4 + 1 + 1) return false;
            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                sessionToken = r.ReadUInt64();
                requestId = r.ReadUInt32();
                var resultByte = r.ReadByte();
                reasonCode = r.ReadByte();
                if (sessionToken == 0UL) return false;
                if (requestId == 0u) return false;
                if (resultByte > 3) return false;
                if (!LitContainerTidyReasons.TryFromWire(reasonCode, out _)) return false;
                if (resultByte == (byte)TidyCommitResult.Committed && reasonCode != (byte)LitContainerTidyReason.None) return false;
                if (resultByte != (byte)TidyCommitResult.Committed && reasonCode == (byte)LitContainerTidyReason.None) return false;
                result = (TidyCommitResult)resultByte;
                return true;
            }
        }

        /// <summary>DEV-V5-05: decode one fast-transfer recover request — exact
        /// shape or the frame does not exist. sourcePage must be a fast-transfer
        /// source (2..6 player pages or the 7 mount page): 0/1 hands, 8 AREA and
        /// 255 never enter the frame — the wire itself enforces the ticket's
        /// 具名排除; kind rides the same {1,2} claim domain as 03.</summary>
        internal static bool TryReadFastTransferRequest(byte[] body,
            out ulong sessionToken, out uint requestId, out byte sourcePage, out byte sourceX, out byte sourceY,
            out byte kind, out ulong fingerprint, out bool sortDescending)
        {
            sessionToken = 0; requestId = 0; sourcePage = 0; sourceX = 0; sourceY = 0;
            kind = 0; fingerprint = 0; sortDescending = false;
            if (body == null || body.Length != 1 + 8 + 4 + 1 + 1 + 1 + 1 + 8 + 1) return false;
            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                var version = r.ReadByte();
                sessionToken = r.ReadUInt64();
                requestId = r.ReadUInt32();
                sourcePage = r.ReadByte();
                sourceX = r.ReadByte();
                sourceY = r.ReadByte();
                kind = r.ReadByte();
                fingerprint = r.ReadUInt64();
                sortDescending = r.ReadBoolean();
                if (version != ProtocolVersionV3) return false;
                if (sessionToken == 0UL) return false;
                if (requestId == 0u) return false;
                if (sourcePage < HotkeySnapshotUtil.TIDYABLE_PAGE_MIN
                    || sourcePage > LitContainerTidyExecution.MOUNT_PAGE) return false;
                if (kind != (byte)LitContainerTidyKind.WorldContainer && kind != (byte)LitContainerTidyKind.VehicleTrunk) return false;
                return true;
            }
        }

        /// <summary>DEV-V5-05: decode one fast-transfer recover result. Same
        /// frozen pair law as the container result (result byte ∈ 0..3, reason
        /// in the 8-cause domain, Committed ⇔ no reason).</summary>
        internal static bool TryReadFastTransferResult(byte[] body, out ulong sessionToken, out uint requestId, out TidyCommitResult result, out byte reasonCode)
        {
            sessionToken = 0; requestId = 0; result = TidyCommitResult.Rejected; reasonCode = 0;
            if (body == null || body.Length != 8 + 4 + 1 + 1) return false;
            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                sessionToken = r.ReadUInt64();
                requestId = r.ReadUInt32();
                var resultByte = r.ReadByte();
                reasonCode = r.ReadByte();
                if (sessionToken == 0UL) return false;
                if (requestId == 0u) return false;
                if (resultByte > 3) return false;
                if (!LitContainerTidyReasons.TryFromWire(reasonCode, out _)) return false;
                if (resultByte == (byte)TidyCommitResult.Committed && reasonCode != (byte)LitContainerTidyReason.None) return false;
                if (resultByte != (byte)TidyCommitResult.Committed && reasonCode == (byte)LitContainerTidyReason.None) return false;
                result = (TidyCommitResult)resultByte;
                return true;
            }
        }

        internal static bool TryReadTidyCommitted(byte[] body, out ulong sessionToken, out uint requestId, out TidyCommitResult result, out List<LitNewPositionMapping> mappings)
        {
            sessionToken = 0; requestId = 0; result = TidyCommitResult.Rejected; mappings = null;
            if (body == null || body.Length < 8 + 4 + 1 + 1) return false;
            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                sessionToken = r.ReadUInt64();
                requestId = r.ReadUInt32();
                var resultByte = r.ReadByte();
                var mappingCount = r.ReadByte();
                if (sessionToken == 0UL) return false;
                if (resultByte > 3) return false;
                if (r.BaseStream.Position + mappingCount * (long)MappingWireSize != body.Length) return false;
                mappings = new List<LitNewPositionMapping>(mappingCount);
                for (int i = 0; i < mappingCount; i++)
                {
                    if (!TryReadMapping(r, out var mapping)) return false;
                    mappings.Add(mapping);
                }
                result = (TidyCommitResult)resultByte;
                return true;
            }
        }

        internal static bool TryReadHotkeyFlowAck(byte[] body, out ulong sessionToken, out uint requestId)
        {
            sessionToken = 0; requestId = 0;
            if (body == null || body.Length != 12) return false;
            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                sessionToken = r.ReadUInt64();
                requestId = r.ReadUInt32();
                return sessionToken != 0UL;
            }
        }

        internal static bool TryReadTidyHotkeyResult(byte[] body, out ulong sessionToken, out uint requestId,
            out byte restoredCount, out byte clearedCount, out byte failedCount, out byte verifiedCount, out List<byte> failedIndices)
        {
            sessionToken = 0; requestId = 0; restoredCount = 0; clearedCount = 0; failedCount = 0; verifiedCount = 0; failedIndices = null;
            if (body == null || body.Length < 8 + 4 + 1 + 1 + 1 + 1) return false;
            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                sessionToken = r.ReadUInt64();
                requestId = r.ReadUInt32();
                restoredCount = r.ReadByte();
                clearedCount = r.ReadByte();
                failedCount = r.ReadByte();
                verifiedCount = r.ReadByte();
                if (sessionToken == 0UL) return false;
                if (clearedCount != failedCount) return false;
                if (verifiedCount > restoredCount) return false;
                if (r.BaseStream.Position + failedCount != body.Length) return false;
                failedIndices = new List<byte>(failedCount);
                for (int i = 0; i < failedCount; i++)
                {
                    var index = r.ReadByte();
                    if (index >= HotkeySnapshotUtil.HOTKEY_COUNT) return false;
                    failedIndices.Add(index);
                }
                return true;
            }
        }

        private static void WriteMapping(BinaryWriter w, LitNewPositionMapping m)
        {
            w.Write(m.HotkeyIndex);
            w.Write(m.NewPage);
            w.Write(m.NewX);
            w.Write(m.NewY);
            w.Write(m.ExpectedItemId);
            w.Write((byte)0); // reserved — must decode as zero (fail-closed)
        }

        private static bool TryReadMapping(BinaryReader r, out LitNewPositionMapping mapping)
        {
            var hi = r.ReadByte();
            var p = r.ReadByte();
            var x = r.ReadByte();
            var y = r.ReadByte();
            var id = r.ReadUInt16();
            var reserved = r.ReadByte();
            if (reserved != 0) { mapping = default(LitNewPositionMapping); return false; }
            mapping = new LitNewPositionMapping(hi, p, x, y, id);
            return true;
        }
    }
}
