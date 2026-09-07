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
        private IBueNetworkApi active;

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
                records = new List<SubscriptionRecord>(subscriptions);
                registrations = new List<ChannelRegistration>(channels.Values);
            }
            DisposeAll(staleHandles); // R3-Standards B2: dispose OUTSIDE the facade lock

            for (var i = 0; i < registrations.Count; i++)
            {
                try { runtime.RegisterChannel(registrations[i].Channel, registrations[i].MinimumContract, registrations[i].FeatureVersion); }
                catch (Exception) { }
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
            }
            DisposeAll(handles);
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
                return new ChannelRegistrationResult(true, channel, FeatureRegistrationReason.None, "BUE-DEFERRED-ACCEPT");
            try
            {
                var result = runtime.RegisterChannel(channel, minimumBueContract, featureVersion);
                if (!result.Accepted)
                {
                    lock (sync) channels.Remove(channel.Value);
                }
                return result;
            }
            catch (Exception)
            {
                lock (sync) channels.Remove(channel.Value);
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
            try { runtime.UnregisterChannel(channel); } catch (Exception) { }
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
            if (runtime != null) AttachSubscription(runtime, record);
            return new SubscriptionHandle(this, record);
        }

        public IReadOnlyList<IConnectionSession> Sessions
        {
            get
            {
                IBueNetworkApi runtime;
                lock (sync) runtime = active;
                if (runtime == null) return new IConnectionSession[0];
                try { return runtime.Sessions ?? new IConnectionSession[0]; }
                catch (Exception) { return new IConnectionSession[0]; }
            }
        }

        public NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable)
        {
            IBueNetworkApi runtime;
            lock (sync) runtime = active;
            if (runtime == null) return NetworkSendResult.NoSession;
            try { return runtime.SendToServer(channel, payload, reliable); }
            catch (Exception) { return NetworkSendResult.LocalTransportUnavailable; }
        }

        public NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable)
        {
            IBueNetworkApi runtime;
            lock (sync) runtime = active;
            if (runtime == null) return NetworkSendResult.NoSession;
            try { return runtime.SendToClients(channel, payload, reliable); }
            catch (Exception) { return NetworkSendResult.LocalTransportUnavailable; }
        }

        public NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable)
        {
            IBueNetworkApi runtime;
            lock (sync) runtime = active;
            if (runtime == null) return NetworkSendResult.NoSession;
            try { return runtime.SendToClient(channel, session, payload, reliable); }
            catch (Exception) { return NetworkSendResult.LocalTransportUnavailable; }
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
                    try { stale.Dispose(); } catch (Exception) { } // outside the facade lock (R3-Standards B2)
                }
            }
            catch (Exception) { }
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
            try { handle?.Dispose(); } catch (Exception) { }
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

        private static void DisposeAll(List<IDisposable> handles)
        {
            if (handles == null) return;
            for (var i = 0; i < handles.Count; i++)
            {
                try { handles[i]?.Dispose(); } catch (Exception) { }
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
