using System;
using System.Collections.Generic;

// DEV-V5-03 (V5-T4): the container-session tidy deep module — pure domain.
// The architecture frozen by T4:
//   容器表面适配器 → 当前容器会话事实 → 容器整理请求
//     → 统一标签分段行带排版（DEV-V5-02 出口） → 服务器权威校验与提交
// Page number is NOT identity: the seventh page is only where vanilla mounts
// the currently open container; identity = kind + session facts + requester +
// content fingerprint, verified by the authority before any commit. This file
// references no Unity/Unturned type; the engine-facing halves live in
// LitContainerSessionProbe / LitTidyProductionAuthority / the UI patch.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>What the REQUEST claims to tidy — only the two supported
    /// kinds exist on the wire; anything else has no adapter and fails
    /// closed (V5-T4 Q1: 本阶段两类).</summary>
    internal enum LitContainerTidyKind : byte
    {
        WorldContainer = 1,
        VehicleTrunk = 2,
    }

    /// <summary>What the CURRENTLY MOUNTED session actually is, as observed
    /// from the authoritative (or best-effort client) side facts. Ground
    /// drops, primary/secondary hands and equipment slots can never produce a
    /// container session at all (they are not mounted into the storage page by
    /// any supported adapter); a workshop virtual container is observed under
    /// its documented hook (T4 Q1: 能力投影 Unsupported，不画，留诊断原因).</summary>
    internal enum LitContainerSessionKind : byte
    {
        None = 0,
        WorldContainer = 1,
        VehicleTrunk = 2,
        VirtualContainer = 3,
        Unsupported = 4,
    }

    /// <summary>The structured failure identity (T4 Q4: 「中文原因至少能区分」
    /// — never a single vague "未改动"). 1..6 are the six causes the decision
    /// names; 7/8 are the honest extras the transaction/admission can answer
    /// with (internal state / another tidy holds the lease). Wire bytes are
    /// this domain; display text has ONE source (ChineseText).</summary>
    internal enum LitContainerTidyReason : byte
    {
        None = 0,
        ContainerClosed = 1,
        LostAccess = 2,
        ContentChanged = 3,
        UnsupportedKind = 4,
        LayoutFailed = 5,
        FeatureUnavailable = 6,
        InternalFailure = 7,
        Busy = 8,
    }

    /// <summary>Reason code ⇄ wire byte ⇄ player-facing Chinese text — the
    /// single source (toasts, logs and the audit all read these).</summary>
    internal static class LitContainerTidyReasons
    {
        internal const byte MaxWire = (byte)LitContainerTidyReason.Busy;

        internal static bool TryFromWire(byte value, out LitContainerTidyReason reason)
        {
            if (value > MaxWire) { reason = LitContainerTidyReason.None; return false; }
            reason = (LitContainerTidyReason)value;
            return true;
        }

        internal static string ChineseText(LitContainerTidyReason reason)
        {
            switch (reason)
            {
                case LitContainerTidyReason.ContainerClosed: return "容器已经关闭（或已更换），未整理任何物品。";
                case LitContainerTidyReason.LostAccess: return "已失去该容器的整理权限，未整理任何物品。";
                case LitContainerTidyReason.ContentChanged: return "容器内容已变化，请重新整理（物品未改动）。";
                case LitContainerTidyReason.UnsupportedKind: return "当前容器类型不支持整理（本阶段支持世界容器与已授权车辆后备箱）。";
                case LitContainerTidyReason.LayoutFailed: return "整理排版失败，未改动任何物品。";
                case LitContainerTidyReason.FeatureUnavailable: return "背包整理功能当前不可用。";
                case LitContainerTidyReason.InternalFailure: return "整理未能完成，物品保持原样（诊断日志有详情）。";
                case LitContainerTidyReason.Busy: return "上一次整理仍在进行，请稍后再试。";
                default: return string.Empty; // None = success has no reason text
            }
        }
    }

    /// <summary>The client-side snapshot of the container surface at click /
    /// refresh time. Every field is a FACT read from the engine (never a
    /// patch-private bool); FeatureRunning rides the lifecycle fact the module
    /// stamps, so the projection can answer the nine-state table honestly.</summary>
    internal struct LitContainerSessionObservation
    {
        public LitContainerSessionKind Kind;
        /// <summary>A container session is currently mounted (原版 isStoring 且网格非空挂载).</summary>
        public bool SessionActive;
        /// <summary>Single-opener / driver-seat access still stands (客户端可见口径；权威端提交前重验).</summary>
        public bool PermissionGranted;
        public bool FeatureRunning;
        public bool TitleBarPresent;
        public bool GridNonEmpty;
        /// <summary>Free-text diagnostic kept for the log when the projection
        /// refuses (e.g. virtual container's observed hook).</summary>
        public string UnsupportedDiagnostic;
    }

    /// <summary>Draw-button decision = capability projection「可用」ONLY
    /// (T4 Q4). Returns false for every not-draw case and names the reason;
    /// reason None on false means "silent absence" (no session / no title
    /// bar), never a failed click.</summary>
    internal static class LitContainerCapability
    {
        internal static bool TryProject(LitContainerSessionObservation obs, out LitContainerTidyReason reason)
        {
            reason = LitContainerTidyReason.None;
            if (!obs.TitleBarPresent) return false; // 标题栏不在场：没有表面可画，静默
            if (!obs.FeatureRunning) { reason = LitContainerTidyReason.FeatureUnavailable; return false; }
            switch (obs.Kind)
            {
                case LitContainerSessionKind.None:
                    return false; // 没有任何容器会话：静默
                case LitContainerSessionKind.VirtualContainer:
                    reason = LitContainerTidyReason.UnsupportedKind;
                    return false;
                case LitContainerSessionKind.Unsupported:
                    reason = LitContainerTidyReason.UnsupportedKind;
                    return false;
            }
            var adapter = LitContainerSessionAdapters.Resolve((LitContainerTidyKind)(byte)obs.Kind);
            if (adapter == null) { reason = LitContainerTidyReason.UnsupportedKind; return false; }
            if (!obs.SessionActive || !obs.GridNonEmpty) { reason = LitContainerTidyReason.ContainerClosed; return false; }
            if (!obs.PermissionGranted) { reason = LitContainerTidyReason.LostAccess; return false; }
            if (!adapter.Matches(obs)) { reason = LitContainerTidyReason.UnsupportedKind; return false; }
            return true;
        }
    }

    /// <summary>One jar as the fingerprint sees it (values, not references —
    /// the client mirror and the authority read the same content, never the
    /// same objects).</summary>
    internal struct LitContainerJarTuple
    {
        public byte X;
        public byte Y;
        public byte Rot;
        public ushort Id;
        public byte Amount;
        public byte Quality;
        public byte[] State;
    }

    /// <summary>Content fingerprint: FNV-1a 64 over the grid geometry plus the
    /// jar tuple MULTISET (each tuple canonical-encoded then sorted, so the
    /// Items enumeration order is never an input — same rule as the
    /// transaction's StateMatches normalization). The claim carries what the
    /// requester saw; the authority re-reads from the real jars and compares —
    /// a stale or forged claim cannot tidy someone else's state.</summary>
    internal static class LitContainerContentFingerprint
    {
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        internal static ulong Compute(byte width, byte height, List<LitContainerJarTuple> jars)
        {
            var encoded = new List<byte[]>((jars == null ? 0 : jars.Count) + 1);
            encoded.Add(new[] { width, height, (byte)0, (byte)(jars == null ? 0 : jars.Count) });
            if (jars != null)
                for (int i = 0; i < jars.Count; i++)
                    encoded.Add(Encode(jars[i]));
            encoded.Sort(CompareBytes);
            var hash = FnvOffset;
            for (int i = 0; i < encoded.Count; i++)
            {
                var bytes = encoded[i];
                for (int j = 0; j < bytes.Length; j++)
                {
                    hash ^= bytes[j];
                    hash *= FnvPrime;
                }
            }
            return hash;
        }

        /// <summary>Fingerprint of a real Items grid (host-safe: managed
        /// list/array reads only; a null jar/item is encoded as zeros so the
        /// hash never throws on anomalous data).</summary>
        internal static ulong FromItems(SDG.Unturned.Items items)
        {
            var tuples = new List<LitContainerJarTuple>();
            if (items != null)
            {
                byte count = items.getItemCount();
                for (byte i = 0; i < count; i++)
                {
                    var jar = items.getItem(i);
                    if (jar == null) continue;
                    var item = jar.item;
                    tuples.Add(new LitContainerJarTuple
                    {
                        X = jar.x, Y = jar.y, Rot = jar.rot,
                        Id = item == null ? (ushort)0 : item.id,
                        Amount = item == null ? (byte)0 : item.amount,
                        Quality = item == null ? (byte)0 : item.quality,
                        State = item == null ? null : item.state,
                    });
                }
                return Compute(items.width, items.height, tuples);
            }
            return Compute(0, 0, tuples);
        }

        private static byte[] Encode(LitContainerJarTuple t)
        {
            var state = t.State;
            var stateLen = state == null ? 0 : state.Length;
            var bytes = new byte[6 + stateLen];
            bytes[0] = t.X;
            bytes[1] = t.Y;
            bytes[2] = t.Rot;
            bytes[3] = (byte)(t.Id >> 8);
            bytes[4] = (byte)(t.Id & 0xFF);
            bytes[5] = (byte)stateLen;
            if (stateLen > 0) Buffer.BlockCopy(state, 0, bytes, 6, stateLen);
            // amount/quality ride a second block so the state variable length
            // cannot be confused with trailing scalars.
            var tail = new byte[2];
            tail[0] = t.Amount;
            tail[1] = t.Quality;
            var full = new byte[bytes.Length + tail.Length];
            Buffer.BlockCopy(bytes, 0, full, 0, bytes.Length);
            Buffer.BlockCopy(tail, 0, full, bytes.Length, tail.Length);
            return full;
        }

        private static int CompareBytes(byte[] a, byte[] b)
        {
            var n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++)
            {
                if (a[i] != b[i]) return a[i].CompareTo(b[i]);
            }
            return a.Length.CompareTo(b.Length);
        }
    }

    /// <summary>What the authority re-reads BEFORE committing: the live mount
    /// kind, whether a session stands, the KIND-SPECIFIC vanilla access
    /// dimensions (V5-T4 Q1: 箱子与后备箱各处理自己的生命周期 — 世界箱 = 单
    /// opener + 原版锁/组；后备箱 = 驾驶座授权仍在位；距离/关箱/离座/换座/车辆
    /// 销毁在原版里先把挂载会话拆掉，因而先于授权呈现为 ContainerClosed), and
    /// the live content fingerprint. Each adapter reads ONLY its own kind's
    /// dimensions — no shared "authorized" shortcut, no BUE-side lock.</summary>
    internal struct LitContainerLiveFacts
    {
        public LitContainerSessionKind KindObserved;
        public bool SessionActive;
        /// <summary>世界箱维度：InteractableStorage.opener 仍是请求者（单 opener）。</summary>
        public bool WorldOpenerIsRequester;
        /// <summary>世界箱维度：原版锁/组事实（checkRot）仍放行。</summary>
        public bool WorldAccessAllowed;
        /// <summary>后备箱维度：请求者仍坐在驾驶座且该车在位（离座/换座/换车即失效）。</summary>
        public bool TrunkDriverAuthorized;
        public ulong LiveFingerprint;
    }

    /// <summary>The request binding (T4 Q3): kind + claimed content version.
    /// The requester identity rides the SESSION (the channel's PeerSteamId —
    /// never a payload field), like the whole LIT protocol.</summary>
    internal struct LitContainerTidyClaim
    {
        public LitContainerTidyKind Kind;
        public ulong Fingerprint;
    }

    /// <summary>The per-kind adapter seam (V5-T4 Q1: 箱子与后备箱不是同一种
    /// 容器…必须各有适配器). Each answers the client-side Matches and the
    /// authority-side Verify for its own kind; the shared VerifyCore order
    /// encodes the Q3/Q4 rulings, and each adapter binds the expected kind so
    /// a trunk session is never tidied as a crate (or vice versa).</summary>
    internal interface ILitContainerSessionAdapter
    {
        LitContainerTidyKind Kind { get; }
        bool Matches(LitContainerSessionObservation obs);
        LitContainerTidyReason Verify(LitContainerLiveFacts live, LitContainerTidyClaim claim);
    }

    internal abstract class LitContainerSessionAdapterBase : ILitContainerSessionAdapter
    {
        public abstract LitContainerTidyKind Kind { get; }
        protected abstract LitContainerSessionKind ExpectedSessionKind { get; }

        /// <summary>本 adapter 自己的权限维度判定（T4 Q1 的「分别处理」落点：
        /// 世界箱读 opener+锁组，后备箱读驾驶座；彼此不越界）。</summary>
        protected abstract LitContainerTidyReason CheckAccess(LitContainerLiveFacts live);

        public bool Matches(LitContainerSessionObservation obs)
        {
            return (LitContainerTidyKind)(byte)obs.Kind == Kind
                && obs.SessionActive && obs.GridNonEmpty && obs.PermissionGranted;
        }

        public LitContainerTidyReason Verify(LitContainerLiveFacts live, LitContainerTidyClaim claim)
        {
            // Ordered exactly as T4 Q3/Q4: 会话 → 种类 → 权限（本种维度）→ 版本.
            if (!live.SessionActive) return LitContainerTidyReason.ContainerClosed;
            if (live.KindObserved == LitContainerSessionKind.VirtualContainer
                || live.KindObserved == LitContainerSessionKind.Unsupported)
                return LitContainerTidyReason.UnsupportedKind;
            if (live.KindObserved != ExpectedSessionKind) return LitContainerTidyReason.ContainerClosed;
            var access = CheckAccess(live);
            if (access != LitContainerTidyReason.None) return access;
            if (live.LiveFingerprint != claim.Fingerprint) return LitContainerTidyReason.ContentChanged;
            return LitContainerTidyReason.None;
        }
    }

    /// <summary>Ordinary world container (InteractableStorage mounted via the
    /// vanilla openStorage path). Its own access dimensions: 单 opener（opener
    /// 仍是请求者）+ 原版锁/组（checkRot）。距离过远与关箱由原版先把挂载会话
    /// 拆掉（closeMoveCheck / ManualOnDestroy），因而在「会话」一步就呈现为
    /// ContainerClosed——原版访问控制保持，BUE 不另造整理锁。</summary>
    internal sealed class LitWorldContainerSessionAdapter : LitContainerSessionAdapterBase
    {
        public override LitContainerTidyKind Kind { get { return LitContainerTidyKind.WorldContainer; } }
        protected override LitContainerSessionKind ExpectedSessionKind { get { return LitContainerSessionKind.WorldContainer; } }

        protected override LitContainerTidyReason CheckAccess(LitContainerLiveFacts live)
        {
            if (!live.WorldOpenerIsRequester) return LitContainerTidyReason.LostAccess;
            if (!live.WorldAccessAllowed) return LitContainerTidyReason.LostAccess;
            return LitContainerTidyReason.None;
        }
    }

    /// <summary>Vehicle trunk the CURRENT player was granted（驾驶座上车的原版
    /// grantTrunkAccess；离座/换座/车辆销毁由原版 revokeTrunkAccess→closeTrunk
    /// 先拆挂载会话，因而呈现为 ContainerClosed）。其自有权限维度 = 请求者仍
    /// 在驾驶座且该车在位——不读世界箱维度，也不读锁。</summary>
    internal sealed class LitVehicleTrunkSessionAdapter : LitContainerSessionAdapterBase
    {
        public override LitContainerTidyKind Kind { get { return LitContainerTidyKind.VehicleTrunk; } }
        protected override LitContainerSessionKind ExpectedSessionKind { get { return LitContainerSessionKind.VehicleTrunk; } }

        protected override LitContainerTidyReason CheckAccess(LitContainerLiveFacts live)
        {
            return live.TrunkDriverAuthorized ? LitContainerTidyReason.None : LitContainerTidyReason.LostAccess;
        }
    }

    /// <summary>DEV-V5-03: the container title-bar surface adapter — the ONE
    /// place the container button's layout and text are decided (V5-T4 Q2:
    /// 「布局由容器标题栏 adapter 提供，不复用服装页固定偏移」). The UI patch
    /// consumes this output and nothing else: geometry (left-anchored box,
    /// away from the right-side display-cabinet rot controls), the frozen
    /// 「整理」label and the tooltip single-source.</summary>
    internal static class LitContainerTitleBarAdapter
    {
        /// <summary>headers[STORAGE - SLOTS] = the sixth header row — a mount
        /// position, again not an identity.</summary>
        internal const int HeaderIndex = 5;
        internal const float ButtonSizeX = 60f;
        internal const float ButtonSizeY = 60f;
        /// <summary>Left anchor: the clothing-page right anchor (-130) is NOT
        /// reused; the storage header has no durability/quality badge, while
        /// display crates own the right 180px for rot_x/y/z.</summary>
        internal const float PositionScaleX = 0f;
        internal const float PositionOffsetX = 10f;
        internal const string ButtonText = "整理";
        internal const string TooltipText = "左键：整理当前打开的容器（只动这一只，不含身上与地面）";
    }

    internal static class LitContainerSessionAdapters
    {
        private static readonly LitWorldContainerSessionAdapter World = new LitWorldContainerSessionAdapter();
        private static readonly LitVehicleTrunkSessionAdapter Trunk = new LitVehicleTrunkSessionAdapter();
        private static readonly IReadOnlyList<ILitContainerSessionAdapter> AllAdapters =
            new ILitContainerSessionAdapter[] { World, Trunk };

        internal static IReadOnlyList<ILitContainerSessionAdapter> All { get { return AllAdapters; } }

        /// <summary>Null for any kind without an adapter (the fail-closed
        /// 「该类型有适配器」projection input).</summary>
        internal static ILitContainerSessionAdapter Resolve(LitContainerTidyKind kind)
        {
            switch (kind)
            {
                case LitContainerTidyKind.WorldContainer: return World;
                case LitContainerTidyKind.VehicleTrunk: return Trunk;
                default: return null;
            }
        }
    }

    /// <summary>The ONE authority-side entry the execution/protocol share:
    /// resolve the claim's adapter first (no adapter = unsupported kind,
    /// including the all-pages 0xFF sentinel), then run the adapter's ordered
    /// verify. Page numbers are never consulted (V5-T4: 第 7 页只是挂载位置).</summary>
    internal static class LitContainerTidyVerifier
    {
        internal static LitContainerTidyReason Verify(LitContainerLiveFacts live, LitContainerTidyClaim claim)
        {
            var adapter = LitContainerSessionAdapters.Resolve(claim.Kind);
            if (adapter == null) return LitContainerTidyReason.UnsupportedKind;
            return adapter.Verify(live, claim);
        }
    }
}
