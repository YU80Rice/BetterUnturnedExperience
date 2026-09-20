using System;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// DEV-V2-05: the V1 compatibility layer — an official, player-visible
    /// feature whose on/off switch is independent from the network module
    /// switch (the panel binding itself is wired in DEV-V2-06). When enabled,
    /// a legacy "MOD" frame is consumed exactly once: routed to the channel's
    /// registered V1 handler, or dropped with a diagnostic when the channel
    /// has no handler. When disabled, every frame is handed back unconsumed
    /// so the vanilla path keeps it. Fault isolation is per-frame: a handler
    /// fault is caught, reported through <see cref="DiagnosticLogSink"/>, and
    /// the layer stays healthy — a V1 fault never propagates into V2 or local
    /// features. LMN2 (V2 namespaced) and non-LMN frames are never consumed
    /// here.
    /// </summary>
    public sealed class LmnV1CompatLayer
    {
        /// <summary>
        /// Host wiring binds this to the runtime log (the Core itself never
        /// touches host types); tests capture the lines. Every diagnostic
        /// line carries diagnosticId=BUE-V1COMPAT-001.
        /// DEV-V6-02E: public since the true-project split — Core compiles
        /// as its own assembly now, so the assembly root (and the harness)
        /// reach this seam across the assembly boundary instead of through
        /// the old same-assembly embed. Core is not a one-public-type
        /// feature project (V6-T2 追加名单只锁功能/界面工程).
        /// </summary>
        public static Action<string> DiagnosticLogSink = null;

        private const string DiagnosticId = "BUE-V1COMPAT-001";

        private readonly LmnV1CompatRegistry registry;
        private bool enabled = true;

        public LmnV1CompatLayer(LmnV1CompatRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public LmnV1CompatRegistry Registry { get { return registry; } }

        /// <summary>
        /// The official switch: true (default) keeps legacy V1 consumers
        /// working; false hands every frame back unconsumed.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        /// <summary>
        /// Server-side receive face: routes a frame received from a remote
        /// client. Returns true when this layer consumed the frame (the caller
        /// must not pass it further), false when the frame was not consumed
        /// and must be handed back (switch off, LMN2, or non-LMN frame).
        /// </summary>
        public bool RouteFromClient(byte[] frame, ulong senderSteamId)
        {
            if (!enabled) return false;
            int channel;
            byte[] payload;
            if (!LmnV1FrameCodec.TryParse(frame, out channel, out payload)) return false;
            return DispatchSafely(() => registry.DispatchServer(channel, senderSteamId, payload), channel);
        }

        /// <summary>Client-side receive face: routes a frame received from the server.</summary>
        public bool RouteFromServer(byte[] frame)
        {
            if (!enabled) return false;
            int channel;
            byte[] payload;
            if (!LmnV1FrameCodec.TryParse(frame, out channel, out payload)) return false;
            return DispatchSafely(() => registry.DispatchClient(channel, payload), channel);
        }

        /// <summary>
        /// Outgoing face for legacy sends: builds the V1 wire frame for an int
        /// channel. Returns false (and leaves frame null) when the switch is
        /// off or the channel is outside the 0..255 legacy range — the caller
        /// then falls back to the vanilla path.
        /// </summary>
        public bool TryBuildOutgoingFrame(int channel, byte[] payload, out byte[] frame)
        {
            frame = null;
            if (!enabled) return false;
            return LmnV1FrameCodec.TryBuild(channel, payload, out frame);
        }

        private bool DispatchSafely(Func<bool> dispatch, int channel)
        {
            try
            {
                if (dispatch()) return true;
                Emit("event=unknown-channel-dropped channel=" + channel
                    + " decision=drop diagnosticId=" + DiagnosticId);
                return true;
            }
            catch (Exception error)
            {
                Emit("event=handler-fault-isolated channel=" + channel
                    + " errorType=" + error.GetType().Name
                    + " decision=drop diagnosticId=" + DiagnosticId);
                return true;
            }
        }

        private static void Emit(string line)
        {
            var sink = DiagnosticLogSink;
            if (sink == null) return;
            try { sink(line); }
            catch (Exception) { }
        }
    }
}
