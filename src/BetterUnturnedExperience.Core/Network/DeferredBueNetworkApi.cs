using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Contracts.BueNetwork;

namespace BetterUnturnedExperience.Core.Network
{
    /// <summary>
    /// Stable feature-facing network identity. Registration and subscription
    /// are accepted before the engine role is known, then replayed onto the
    /// first live runtime. The facade itself is never replaced.
    ///
    /// DEV-V3-04 replay failure projection (the spec's 澄清修订): every
    /// Attach/replay/detach fault surfaces in the injected diagnostic sink
    /// instead of folding into an empty catch, and the four failure families
    /// stay distinguishable through the result token:
    ///   result=not-ready            never attached yet (stage=send/register)
    ///   result=detached             was attached, the module/host tore the
    ///                               binding down (module-stopped family)
    ///   result=replay-failed        a deferred registration/subscription was
    ///                               refused or threw while replaying onto the
    ///                               live runtime (stage=replay-register /
    ///                               replay-subscribe)
    ///   result=transport-unavailable a live-runtime call (send / sessions /
    ///                               unregister) threw under the hood.
    /// The not-ready / detached send projections latch ONE line per episode
    /// (user-paced send loops must not turn a standing state into a flood);
    /// replay and live-call faults are one-shot episodes and emit per fault.
    /// Diagnostics never run under the facade lock (the frozen discipline).
    /// </summary>
    public sealed class DeferredBueNetworkApi : IBueNetworkApi
    {
        private sealed class SubscriptionRecord
        {
            internal FeatureId Channel;
            internal ChannelDirection Direction;
            internal Action<IConnectionSession, byte[]> Handler;
            internal IDisposable ActiveHandle;
            internal bool Removed;
        }

        private readonly object sync = new object();
        private readonly Dictionary<string, ChannelRegistration> channels =
            new Dictionary<string, ChannelRegistration>(StringComparer.Ordinal);
        private readonly List<SubscriptionRecord> subscriptions = new List<SubscriptionRecord>();
        private readonly Action<string> diagnosticSink;
        private IBueNetworkApi active;
        // Episode latches for the standing-state projections (reset whenever
        // the state flips: attach clears both; detach arms the detached one).
        private bool everAttached;
        private bool notReadyLineEmitted;
        private bool detachedLineEmitted;

        public DeferredBueNetworkApi(Action<string> diagnosticSink = null)
        {
            this.diagnosticSink = diagnosticSink;
        }

        private void EmitDiagnostic(string line)
        {
            var sink = diagnosticSink;
            if (sink == null || line == null) return;
            try { sink(line); }
            catch (Exception) { }
        }

        private void EmitStanding(string line)
        {
            EmitDiagnostic(line + " diagnosticId=BUE-NET-005");
        }

        public void Attach(IBueNetworkApi runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            List<SubscriptionRecord> records;
            List<ChannelRegistration> registrations;
            List<IDisposable> staleHandles;
            lock (sync)
            {
                if (ReferenceEquals(active, runtime)) return;
                staleHandles = CollectActiveHandlesLocked();
                active = runtime;
                everAttached = true;
                notReadyLineEmitted = false;
                detachedLineEmitted = false;
                records = new List<SubscriptionRecord>(subscriptions);
                registrations = new List<ChannelRegistration>(channels.Values);
            }
            DisposeAll(staleHandles, "detach-dispose"); // R3-Standards B2: dispose OUTSIDE the facade lock

            for (var i = 0; i < registrations.Count; i++)
            {
                var channel = registrations[i].Channel;
                try
                {
                    var result = runtime.RegisterChannel(channel, registrations[i].MinimumContract, registrations[i].FeatureVersion);
                    if (!result.Accepted)
                    {
                        EmitStanding("event=network-deferred result=replay-failed stage=replay-register channel="
                            + channel.Value + " reason=refused-by-runtime accepted=false");
                    }
                }
                catch (Exception error)
                {
                    EmitStanding("event=network-deferred result=replay-failed stage=replay-register channel="
                        + channel.Value + " errorType=" + error.GetType().Name);
                }
            }
            for (var i = 0; i < records.Count; i++) AttachSubscription(runtime, records[i]);
        }

        public void Detach()
        {
            List<IDisposable> handles;
            lock (sync)
            {
                handles = CollectActiveHandlesLocked();
                active = null;
                detachedLineEmitted = false; // a fresh detached episode begins here
            }
            DisposeAll(handles, "detach-dispose");
        }

        public ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion)
        {
            // R3-Standards B1: the runtime call runs OUTSIDE the facade lock
            // (the lock never spans external code); a rejected/failed replay
            // removes the record again.
            lock (sync)
            {
                if (channels.ContainsKey(channel.Value))
                    return new ChannelRegistrationResult(false, channel, FeatureRegistrationReason.DuplicateFeature, "BUE-DEFERRED-DUP");
                channels.Add(channel.Value, new ChannelRegistration(channel, minimumBueContract, featureVersion));
            }
            IBueNetworkApi runtime;
            lock (sync) runtime = active;
            if (runtime == null)
            {
                EmitNotReady("register channel=" + channel.Value);
                return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "BUE-DEFERRED-ACCEPT");
            }
            try
            {
                var result = runtime.RegisterChannel(channel, minimumBueContract, featureVersion);
                if (!result.Accepted)
                {
                    lock (sync) channels.Remove(channel.Value);
                }
                return result;
            }
            catch (Exception error)
            {
                lock (sync) channels.Remove(channel.Value);
                EmitStanding("event=network-deferred result=transport-unavailable stage=register channel="
                    + channel.Value + " errorType=" + error.GetType().Name);
                return new ChannelRegistrationResult(false, channel, FeatureRegistrationReason.CoreUnavailable, "BUE-DEFERRED-REGISTER-FAILED");
            }
        }

        public bool UnregisterChannel(FeatureId channel)
        {
            IBueNetworkApi runtime;
            lock (sync)
            {
                if (!channels.Remove(channel.Value)) return false;
                runtime = active;
            }
            if (runtime == null) return true;
            try { runtime.UnregisterChannel(channel); }
            catch (Exception error)
            {
                EmitStanding("event=network-deferred result=transport-unavailable stage=unregister channel="
                    + channel.Value + " errorType=" + error.GetType().Name);
            }
            return true;
        }

        public IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (direction != ChannelDirection.FromClients && direction != ChannelDirection.FromServer)
                throw new ArgumentOutOfRangeException(nameof(direction));
            var record = new SubscriptionRecord { Channel = channel, Direction = direction, Handler = handler };
            IBueNetworkApi runtime;
            lock (sync)
            {
                subscriptions.Add(record);
                runtime = active;
            }
            if (runtime == null) EmitNotReady("subscribe channel=" + channel.Value);
            else AttachSubscription(runtime, record);
            return new SubscriptionHandle(this, record);
        }

        public IReadOnlyList<IConnectionSession> Sessions
        {
            get
            {
                IBueNetworkApi runtime;
                lock (sync) runtime = active;
                if (runtime == null)
                {
                    EmitStandingState(); // not-ready / detached family, latched
                    return new IConnectionSession[0];
                }
                try { return runtime.Sessions ?? new IConnectionSession[0]; }
                catch (Exception error)
                {
                    EmitStanding("event=network-deferred result=transport-unavailable stage=sessions errorType=" + error.GetType().Name);
                    return new IConnectionSession[0];
                }
            }
        }

        public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable)
        {
            IBueNetworkApi runtime;
            lock (sync) runtime = active;
            if (runtime == null)
            {
                EmitNotReady("send channel=" + channel.Value);
                return NetworkSendResult.NoSession;
            }
            try { return runtime.SendToServer(channel, payload, reliable); }
            catch (Exception error)
            {
                EmitStanding("event=network-deferred result=transport-unavailable stage=send-to-server channel="
                    + channel.Value + " errorType=" + error.GetType().Name);
                return NetworkSendResult.LocalTransportUnavailable;
            }
        }

        public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
        {
            IBueNetworkApi runtime;
            lock (sync) runtime = active;
            if (runtime == null)
            {
                EmitNotReady("send channel=" + channel.Value);
                return NetworkSendResult.NoSession;
            }
            try { return runtime.SendToClients(channel, payload, reliable); }
            catch (Exception error)
            {
                EmitStanding("event=network-deferred result=transport-unavailable stage=send-to-clients channel="
                    + channel.Value + " errorType=" + error.GetType().Name);
                return NetworkSendResult.LocalTransportUnavailable;
            }
        }

        public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable)
        {
            IBueNetworkApi runtime;
            lock (sync) runtime = active;
            if (runtime == null)
            {
                EmitNotReady("send channel=" + channel.Value);
                return NetworkSendResult.NoSession;
            }
            try { return runtime.SendToClient(channel, session, payload, reliable); }
            catch (Exception error)
            {
                EmitStanding("event=network-deferred result=transport-unavailable stage=send-to-client channel="
                    + channel.Value + " errorType=" + error.GetType().Name);
                return NetworkSendResult.LocalTransportUnavailable;
            }
        }

        /// <summary>The standing unbound-state projection: not-ready (never
        /// attached) vs detached (the binding was torn down = module-stopped
        /// family) are the two distinguishable result tokens; one line per
        /// episode keeps a user-paced send loop from flooding the log.</summary>
        private void EmitNotReady(string detail)
        {
            string line = null;
            lock (sync)
            {
                if (!everAttached)
                {
                    if (notReadyLineEmitted) return;
                    notReadyLineEmitted = true;
                    line = "event=network-deferred result=not-ready stage=" + detail;
                }
                else
                {
                    if (detachedLineEmitted) return;
                    detachedLineEmitted = true;
                    line = "event=network-deferred result=detached stage=" + detail;
                }
            }
            EmitStanding(line);
        }

        private void EmitStandingState()
        {
            bool attached;
            lock (sync) attached = active != null;
            if (attached) return;
            EmitNotReady("sessions");
        }

        private void AttachSubscription(IBueNetworkApi runtime, SubscriptionRecord record)
        {
            try
            {
                var handle = runtime.Subscribe(record.Channel, record.Direction, record.Handler);
                IDisposable stale = null;
                lock (sync)
                {
                    if (record.Removed || !ReferenceEquals(active, runtime)) stale = handle;
                    else record.ActiveHandle = handle;
                }
                if (stale != null)
                {
                    try { stale.Dispose(); }
                    catch (Exception error)
                    {
                        EmitStanding("event=network-deferred result=replay-failed stage=replay-dispose channel="
                            + record.Channel.Value + " errorType=" + error.GetType().Name);
                    } // outside the facade lock (R3-Standards B2)
                }
            }
            catch (Exception error)
            {
                EmitStanding("event=network-deferred result=replay-failed stage=replay-subscribe channel="
                    + record.Channel.Value + " errorType=" + error.GetType().Name);
            }
        }

        private void Remove(SubscriptionRecord record)
        {
            IDisposable handle;
            lock (sync)
            {
                if (record.Removed) return;
                record.Removed = true;
                subscriptions.Remove(record);
                handle = record.ActiveHandle;
                record.ActiveHandle = null;
            }
            try { handle?.Dispose(); }
            catch (Exception error)
            {
                EmitStanding("event=network-deferred result=transport-unavailable stage=unsubscribe channel="
                    + record.Channel.Value + " errorType=" + error.GetType().Name);
            }
        }

        /// <summary>COLLECTS active handles under the lock; disposal happens outside it (R3-Standards B2).</summary>
        private List<IDisposable> CollectActiveHandlesLocked()
        {
            var handles = new List<IDisposable>(subscriptions.Count);
            for (var i = 0; i < subscriptions.Count; i++)
            {
                var handle = subscriptions[i].ActiveHandle;
                subscriptions[i].ActiveHandle = null;
                if (handle != null) handles.Add(handle);
            }
            return handles;
        }

        private void DisposeAll(List<IDisposable> handles, string stage)
        {
            if (handles == null) return;
            for (var i = 0; i < handles.Count; i++)
            {
                try { handles[i]?.Dispose(); }
                catch (Exception error)
                {
                    EmitStanding("event=network-deferred result=transport-unavailable stage=" + stage
                        + " errorType=" + error.GetType().Name);
                }
            }
        }

        private sealed class SubscriptionHandle : IDisposable
        {
            private readonly DeferredBueNetworkApi owner;
            private readonly SubscriptionRecord record;
            internal SubscriptionHandle(DeferredBueNetworkApi owner, SubscriptionRecord record) { this.owner = owner; this.record = record; }
            public void Dispose() { owner.Remove(record); }
        }

        private sealed class ChannelRegistration
        {
            internal ChannelRegistration(FeatureId channel, ContractVersion minimumContract, ushort featureVersion)
            { Channel = channel; MinimumContract = minimumContract; FeatureVersion = featureVersion; }
            internal FeatureId Channel;
            internal ContractVersion MinimumContract;
            internal ushort FeatureVersion;
        }
    }
}
