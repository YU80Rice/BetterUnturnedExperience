using System;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-05 (V5-T5): the client-side fact that a vanilla Ctrl+right-click
    /// quick transfer was ATTEMPTED on a fragmented destination and, per vanilla's
    /// own code, may therefore have sent nothing. One entry per onSelectedItem
    /// invocation: the Prefix records the branch facts (source slot + the
    /// container-session claim the client sees), the sendDragItem postfix marks
    /// that the vanilla packet DID go out, and the Finalizer answers at most one
    /// recovery request — sent → silence (成功转移不排), not sent → request.
    /// </summary>
    internal sealed class FastTransferIntent
    {
        /// <summary>Vanilla page of the item being quick-moved: 2..6 (player→
        /// container) or 7 = STORAGE (container→player). 0/1/8 never arrive (the
        /// engine-side probe gate + the wire codec + the authority all refuse).</summary>
        public byte SourcePage;
        public byte SourceX;
        public byte SourceY;
        /// <summary>The 03 container-session identity of the OTHER side (the
        /// receiving grid when SourcePage is a player page; the source grid when
        /// SourcePage is 7). Page 7 is never consulted as identity.</summary>
        public LitContainerTidyKind Kind;
        /// <summary>The 03 content fingerprint of the storage mirror at click
        /// time — the claimed version the authority re-verifies before ANY
        /// mutation (客机不能冒充版本).</summary>
        public ulong Fingerprint;
    }

    /// <summary>
    /// The verified-context window for one quick transfer (the 04 scope pattern
    /// mirrored): opened ONLY by the onSelectedItem Prefix when the intent probe
    /// judged the fast-transfer branch applicable, and closed by the Finalizer —
    /// which also consumes it. Pure managed state (main thread UI click flow,
    /// no locks needed — same shape as InsertRecoverScope), no Unity type, no
    /// feature bool: registration of the PATCH is the lifecycle fact, this type
    /// merely carries one click's facts.
    /// </summary>
    internal static class FastTransferIntentScope
    {
        private static int depth;
        private static FastTransferIntent intent;
        private static bool dragSent;

        internal static bool IsOpen { get { return depth > 0; } }

        /// <summary>Open one window with the click's facts (a null intent never
        /// opens — the probe refusing the branch is structural non-wiring).</summary>
        internal static void Enter(FastTransferIntent fastTransferIntent)
        {
            if (fastTransferIntent == null) return;
            depth++;
            intent = fastTransferIntent;
            dragSent = false; // a previous window's sent-mark never leaks
        }

        /// <summary>Mark that vanilla's sendDragItem ran inside the open window
        /// (no-op outside any window — real mouse drags, BII submissions, and the
        /// context-menu store button all fire OUTSIDE onSelectedItem and are
        /// therefore structurally never recovery triggers: 「不挂拖放预览」).</summary>
        internal static void NoteDragSent()
        {
            if (depth > 0 && intent != null) dragSent = true;
        }

        /// <summary>Close one window and answer its verdict: the pending
        /// FastTransferIntent when the transfer did NOT go out (碎洞未发包), else
        /// null. At most one request per Enter — a second close is inert, and an
        /// unbalanced close can only mask an already-closed window (the 04 rule:
        /// never open a spurious one).</summary>
        internal static FastTransferIntent ExitEvaluate()
        {
            if (depth <= 0) return null;
            depth--;
            var answer = intent != null && !dragSent ? intent : null;
            if (depth == 0)
            {
                intent = null;
                dragSent = false;
            }
            return answer;
        }

        /// <summary>Host-test seam: drop any stale window between groups.</summary>
        internal static void ResetForTests()
        {
            depth = 0;
            intent = null;
            dragSent = false;
        }
    }
}
