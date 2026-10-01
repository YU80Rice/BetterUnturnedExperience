using System;
using System.Text;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V2-22: the LIR feature-private wire codec (the old AmmoRepackNetwork
    /// protocol layout, carried over the BUE named channel — FeatureId — instead
    /// of the retired LMN channel). The old ModTransport.BuildNamedMessage helper
    /// is gone per spec: the feature assembles its own byte[] (功能内自组,不占契约).
    /// Layout (fail-closed structural validation on read — truncation, trailing
    /// bytes, wrong version/type, requestId=0, total<=0 all refuse):
    ///   client → server: [protocolVersion:1][RequestRepackAmmo=1:1][requestId:8]
    ///   server → client: [protocolVersion:1][RepackSuccess=2:1][requestId:8][totalTransferred:4]
    /// DEV-V5-07 加性扩位（同频道功能私有协议，旧对端按未知 kind 拒收计数——与
    /// 03/05 kind 扩位同律）：
    ///   client → server: [1][RequestUpgrade=3:1][requestId:8][targetLevel:1]（1..2，其余畸形）
    ///   server → client: [1][UpgradeResult=4:1][requestId:8][accepted:1][newLevel:1][reasonCode:1]
    ///   server → client: [1][LevelState=5:1][level:1]（主机确认等级）
    ///   server → client: [1][SkillCooldownNotice=6:1][remainingMs:4]（红色剩余秒呈现输入）
    ///   client → server: [1][RequestLevelState=7:1]
    /// DEV-V7-04 客机缺陷修复②（scope 块加性扩位；kind 4/5 帧尾可选）：
    ///   [hasScope:1] + hasScope=1 时 [serverIdLen:1][serverId utf8][steamId:8]
    ///   [characterId:1][mapNameLen:1][mapName utf8]——服务端权威 scope 随回执
    ///   下行，客机镜像直接绑定（显示输入不依赖客户端自解析）；服务器解析不出
    ///   scope=诚实降级 hasScope=0；读侧截断/尾随/长度越界一律拒收。
    /// </summary>
    internal static class LirRepackWireCodec
    {
        internal const byte ProtocolVersion = 1;
        internal const byte MsgRequestRepackAmmo = 1;
        internal const byte MsgRepackSuccess = 2;
        internal const byte MsgRequestUpgrade = 3;
        internal const byte MsgUpgradeResult = 4;
        internal const byte MsgLevelState = 5;
        internal const byte MsgSkillCooldownNotice = 6;
        internal const byte MsgRequestLevelState = 7;

        internal static byte[] BuildRequest(ulong requestId)
        {
            var payload = new byte[10];
            payload[0] = ProtocolVersion;
            payload[1] = MsgRequestRepackAmmo;
            WriteUInt64(payload, 2, requestId);
            return payload;
        }

        internal static bool TryReadRequest(byte[] payload, out ulong requestId)
        {
            requestId = 0UL;
            if (payload == null || payload.Length != 10) return false;
            if (payload[0] != ProtocolVersion || payload[1] != MsgRequestRepackAmmo) return false;
            requestId = ReadUInt64(payload, 2);
            return requestId != 0UL;
        }

        internal static byte[] BuildSuccess(ulong requestId, int totalTransferred)
        {
            var payload = new byte[14];
            payload[0] = ProtocolVersion;
            payload[1] = MsgRepackSuccess;
            WriteUInt64(payload, 2, requestId);
            WriteInt32(payload, 10, totalTransferred);
            return payload;
        }

        internal static bool TryReadSuccess(byte[] payload, out ulong requestId, out int totalTransferred)
        {
            requestId = 0UL;
            totalTransferred = 0;
            if (payload == null || payload.Length != 14) return false;
            if (payload[0] != ProtocolVersion || payload[1] != MsgRepackSuccess) return false;
            requestId = ReadUInt64(payload, 2);
            totalTransferred = ReadInt32(payload, 10);
            // 成功回包必须绑定非零手动 requestId；被动主机事务不产生网络成功帧。
            return requestId != 0UL && totalTransferred > 0;
        }

        // ── DEV-V5-07 技能帧（同频道加性 kind；全部 fail-closed 结构校验：
        // 截断/尾随/版本/类型/requestId=0/目标级越界/毫秒非正 一律拒）──

        internal static byte[] BuildUpgradeRequest(ulong requestId, byte targetLevel)
        {
            var payload = new byte[11];
            payload[0] = ProtocolVersion;
            payload[1] = MsgRequestUpgrade;
            WriteUInt64(payload, 2, requestId);
            payload[10] = targetLevel;
            return payload;
        }

        internal static bool TryReadUpgradeRequest(byte[] payload, out ulong requestId, out byte targetLevel)
        {
            requestId = 0UL;
            targetLevel = 0;
            if (payload == null || payload.Length != 11) return false;
            if (payload[0] != ProtocolVersion || payload[1] != MsgRequestUpgrade) return false;
            requestId = ReadUInt64(payload, 2);
            targetLevel = payload[10];
            return requestId != 0UL && targetLevel >= 1 && targetLevel <= ReloadSkillPolicy.MaxSkillLevel;
        }

        internal static byte[] BuildUpgradeResult(ulong requestId, bool accepted, byte newLevel, byte reasonCode,
            bool withScope, ReloadSkillScopeKey scope)
        {
            var scopeSize = ScopeBlockSize(withScope, scope);
            var payload = new byte[13 + scopeSize];
            payload[0] = ProtocolVersion;
            payload[1] = MsgUpgradeResult;
            WriteUInt64(payload, 2, requestId);
            payload[10] = accepted ? (byte)1 : (byte)0;
            payload[11] = newLevel;
            payload[12] = reasonCode;
            if (scopeSize > 0) WriteScopeBlock(payload, 13, scope);
            return payload;
        }

        internal static bool TryReadUpgradeResult(byte[] payload, out ulong requestId, out bool accepted,
            out byte newLevel, out byte reasonCode, out bool hasScope, out ReloadSkillScopeKey scope)
        {
            requestId = 0UL;
            accepted = false;
            newLevel = 0;
            reasonCode = 0;
            hasScope = false;
            scope = default(ReloadSkillScopeKey);
            if (payload == null || payload.Length < 13) return false;
            if (payload[0] != ProtocolVersion || payload[1] != MsgUpgradeResult) return false;
            if (!TryReadScopeTrailer(payload, 13, ref hasScope, ref scope)) return false;
            requestId = ReadUInt64(payload, 2);
            accepted = payload[10] == 1;
            newLevel = payload[11];
            reasonCode = payload[12];
            return requestId != 0UL && payload[10] <= 1 && newLevel <= ReloadSkillPolicy.MaxSkillLevel;
        }

        internal static byte[] BuildLevelState(byte level, bool withScope, ReloadSkillScopeKey scope)
        {
            var scopeSize = ScopeBlockSize(withScope, scope);
            var payload = new byte[3 + scopeSize];
            payload[0] = ProtocolVersion;
            payload[1] = MsgLevelState;
            payload[2] = level;
            if (scopeSize > 0) WriteScopeBlock(payload, 3, scope);
            return payload;
        }

        internal static bool TryReadLevelState(byte[] payload, out byte level, out bool hasScope,
            out ReloadSkillScopeKey scope)
        {
            level = 0;
            hasScope = false;
            scope = default(ReloadSkillScopeKey);
            if (payload == null || payload.Length < 3) return false;
            if (payload[0] != ProtocolVersion || payload[1] != MsgLevelState) return false;
            if (!TryReadScopeTrailer(payload, 3, ref hasScope, ref scope)) return false;
            level = payload[2];
            return level <= ReloadSkillPolicy.MaxSkillLevel;
        }

        internal static byte[] BuildSkillCooldownNotice(int remainingMs)
        {
            var payload = new byte[6];
            payload[0] = ProtocolVersion;
            payload[1] = MsgSkillCooldownNotice;
            WriteInt32(payload, 2, remainingMs);
            return payload;
        }

        internal static bool TryReadSkillCooldownNotice(byte[] payload, out int remainingMs)
        {
            remainingMs = 0;
            if (payload == null || payload.Length != 6) return false;
            if (payload[0] != ProtocolVersion || payload[1] != MsgSkillCooldownNotice) return false;
            remainingMs = ReadInt32(payload, 2);
            return remainingMs > 0;
        }

        internal static byte[] BuildLevelStateRequest()
        {
            var payload = new byte[2];
            payload[0] = ProtocolVersion;
            payload[1] = MsgRequestLevelState;
            return payload;
        }

        internal static bool TryReadLevelStateRequest(byte[] payload)
        {
            return payload != null && payload.Length == 2
                && payload[0] == ProtocolVersion && payload[1] == MsgRequestLevelState;
        }

        // ── DEV-V7-04 scope 块（kind 4/5 帧尾可选；写侧超限/不可编码=诚实降级
        // 无 scope，读侧截断/尾随/越界/非法 UTF-8/身份字段非法一律整帧拒收）──

        // 严格 UTF-8：非法字节序列必须拒收而非替换成 U+FFFD（Round 2 blocker）。
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private static int ScopeBlockSize(bool withScope, ReloadSkillScopeKey scope)
        {
            if (!withScope || !scope.IsValid) return 0;
            byte[] serverId;
            byte[] mapName;
            try
            {
                serverId = StrictUtf8.GetBytes(scope.ServerId);
                mapName = StrictUtf8.GetBytes(scope.MapName);
            }
            catch (Exception)
            {
                return 0; // 不可编码身份=诚实降级为无 scope（不发明截断身份）
            }
            if (serverId.Length > 255 || mapName.Length > 255) return 0;
            return 1 + 1 + serverId.Length + 8 + 1 + 1 + mapName.Length;
        }

        private static void WriteScopeBlock(byte[] payload, int offset, ReloadSkillScopeKey scope)
        {
            var serverId = StrictUtf8.GetBytes(scope.ServerId); // ScopeBlockSize 已验可编码
            var mapName = StrictUtf8.GetBytes(scope.MapName);
            payload[offset] = 1;
            payload[offset + 1] = (byte)serverId.Length;
            Array.Copy(serverId, 0, payload, offset + 2, serverId.Length);
            var steamOffset = offset + 2 + serverId.Length;
            WriteUInt64(payload, steamOffset, scope.SteamId);
            payload[steamOffset + 8] = scope.CharacterId;
            var mapLenOffset = steamOffset + 9;
            payload[mapLenOffset] = (byte)mapName.Length;
            Array.Copy(mapName, 0, payload, mapLenOffset + 1, mapName.Length);
        }

        private static bool TryReadScopeTrailer(byte[] payload, int offset, ref bool hasScope,
            ref ReloadSkillScopeKey scope)
        {
            if (offset == payload.Length) return true; // 无 scope 尾块（降级帧）
            if (payload[offset] != 1) return false;    // 只认 flag=1 完整 scope 块；其余=畸形
            var cursor = offset + 1;
            if (cursor >= payload.Length) return false;
            var serverIdLen = payload[cursor];
            cursor += 1;
            if (cursor + serverIdLen + 10 > payload.Length) return false;
            string serverId;
            try { serverId = StrictUtf8.GetString(payload, cursor, serverIdLen); }
            catch (Exception) { return false; } // 非法 UTF-8=整帧拒收
            cursor += serverIdLen;
            var steamId = ReadUInt64(payload, cursor);
            cursor += 8;
            var characterId = payload[cursor];
            cursor += 1;
            var mapNameLen = payload[cursor];
            cursor += 1;
            if (cursor + mapNameLen != payload.Length) return false; // 尾随字节=畸形
            string mapName;
            try { mapName = StrictUtf8.GetString(payload, cursor, mapNameLen); }
            catch (Exception) { return false; }
            var candidate = new ReloadSkillScopeKey(serverId, steamId, characterId, mapName);
            if (!candidate.IsValid) return false; // 身份字段非法（空 id/零 steamId）=整帧拒收
            hasScope = true;
            scope = candidate;
            return true;
        }

        // Explicit little-endian readers/writers (no BinaryReader on the hot
        // path; the old EndOfStream handling collapses into length checks).
        private static void WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            for (var i = 0; i < 8; i++) buffer[offset + i] = (byte)(value >> (8 * i));
        }

        private static ulong ReadUInt64(byte[] buffer, int offset)
        {
            ulong value = 0;
            for (var i = 0; i < 8; i++) value |= (ulong)buffer[offset + i] << (8 * i);
            return value;
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            for (var i = 0; i < 4; i++) buffer[offset + i] = (byte)(value >> (8 * i));
        }

        private static int ReadInt32(byte[] buffer, int offset)
        {
            var value = 0;
            for (var i = 0; i < 4; i++) value |= buffer[offset + i] << (8 * i);
            return value;
        }
    }
}
