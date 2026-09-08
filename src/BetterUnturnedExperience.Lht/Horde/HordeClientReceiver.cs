using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the client-side receive path (the old HordeStatusNetwork
    /// client half). The inbound handler only parses and stores — Unity state
    /// is committed on the main thread through the single-slot mailbox. The
    /// accept gate is the migrated 停止闸门: closed BEFORE the subscription is
    /// released, a late callback can never write state after the mailbox
    /// reset. Client handlers never send on this channel — the protocol is
    /// one-way server → clients.
    /// </summary>
    internal sealed class HordeClientReceiver
    {
        private bool acceptFrames;

        /// <summary>Whether stored frames may still arrive (the stop gate's observable state).</summary>
        internal bool AcceptFrames
        {
            get { return acceptFrames; }
        }

        /// <summary>Opens the receive gate (the subscription is live).</summary>
        internal void Open()
        {
            acceptFrames = true;
        }

        /// <summary>Closes the receive gate BEFORE unbinding — the stop order.</summary>
        internal void Close()
        {
            acceptFrames = false;
        }

        /// <summary>The FromServer subscription entry (parse → mailbox store, any thread).</summary>
        internal void HandleFrame(IConnectionSession session, byte[] payload)
        {
            if (!acceptFrames) return;

            if (HordeWireCodec.TryReadUpdate(payload, out HordeSnapshot update))
            {
                PendingHordeSnapshot.Store(update);
                LhtRuntime.LogInfo("[HordeNet] 收到 Update: epoch=" + update.Epoch + " seq=" + update.Sequence
                    + " remaining=" + update.Remaining + "/" + update.Total + " loc=" + update.Location);
                return;
            }

            if (HordeWireCodec.TryReadClear(payload, out uint epoch, out uint sequence))
            {
                // Clear 的单调序列号终结当前 epoch。
                PendingHordeSnapshot.Store(new HordeSnapshot(false, epoch, sequence, 0, 0, string.Empty, string.Empty));
                LhtRuntime.LogInfo("[HordeNet] 收到 Clear: epoch=" + epoch + " seq=" + sequence);
                return;
            }

            LhtRuntime.LogWarning("[HordeNet] 收到无法解析的负载，拒绝（版本/长度/边界校验未过）");
        }

        /// <summary>The main-thread mailbox drain (module host tick).</summary>
        internal void DrainToState()
        {
            PendingHordeSnapshot.DrainToState();
        }

        /// <summary>The generation-boundary mailbox reset.</summary>
        internal void Reset()
        {
            PendingHordeSnapshot.Reset();
        }
    }
}
