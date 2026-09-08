using System.Threading;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>The recorded NEW-magazine slot — one immutable value on the guard seam (no out-parameter data clumps).</summary>
    internal readonly struct ReloadSlotContext
    {
        public byte Page { get; }
        public byte X { get; }
        public byte Y { get; }
        public byte Rot { get; }
        public byte SizeX { get; }
        public byte SizeY { get; }

        public ReloadSlotContext(byte page, byte x, byte y, byte rot, byte sizeX, byte sizeY)
        {
            Page = page;
            X = x;
            Y = y;
            Rot = rot;
            SizeX = sizeX;
            SizeY = sizeY;
        }
    }

    /// <summary>
    /// DEV-V2-22 (spec T5 决策 2): the reload-context guard — the NEW seam the
    /// old P2PAmmoManager protocol becomes. The three Harmony adapters are its
    /// only production callers:
    ///   1. UseableGun.ReceiveAttachMagazine Prefix → BeginReload (records the
    ///      NEW magazine's full slot; the detach-only page==255 branch never
    ///      calls in — that decision lives in the adapter, one place);
    ///   2. PlayerInventory.forceAddItem Prefix → TryConsumeSlot (the overlay
    ///      point: a BII drag-in reaches this prefix with NO context, the
    ///      guard refuses, and no in-place reload logic executes — the native
    ///      tryFindSpace/dropItem path continues untouched);
    ///   3. ReceiveAttachMagazine Postfix → Reset (bottom-clean, an exception
    ///      path can never leak a slot into a later call).
    /// The single consumed-once slot is the frozen protocol: one reload
    /// produces exactly one old magazine. Deliberately engine-free — the
    /// overlay point is pinned by red tests at this seam's input/output, and
    /// the thread assertion moved to the adapters (production paths).
    /// </summary>
    internal sealed class ReloadContextGuard
    {
        private ReloadSlotContext pending;

        /// <summary>Observable pending state (diagnostics/tests; production decisions use TryConsumeSlot).</summary>
        internal bool HasPendingContext { get { return Thread.VolatileRead(ref _hasPending) == 1; } }

        // A separate int flag keeps the volatile read cheap and the struct
        // copy atomic under the write patterns below (game thread only).
        private int _hasPending;

        /// <summary>Called from the ReceiveAttachMagazine Prefix (attach branch only).</summary>
        internal void BeginReload(ReloadSlotContext context)
        {
            pending = context;
            Thread.VolatileWrite(ref _hasPending, 1);
        }

        /// <summary>
        /// Consumes the pending slot: true exactly once per BeginReload — the
        /// forceAddItem Prefix executes the in-place placement only on true
        /// and the slot is dead afterwards (a later forceAddItem can never
        /// misfire); false is the overlay point's passthrough (BII drag-in).
        /// </summary>
        internal bool TryConsumeSlot(out ReloadSlotContext context)
        {
            var valid = Thread.VolatileRead(ref _hasPending) == 1;
            context = pending;
            Reset();
            return valid;
        }

        /// <summary>The Postfix bottom-clean and the consume-side clear.</summary>
        internal void Reset()
        {
            pending = default(ReloadSlotContext);
            Thread.VolatileWrite(ref _hasPending, 0);
        }
    }
}
