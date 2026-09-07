using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Events
{
    /// <summary>
    /// DEV-V2-19: the host-owned feature event bus — the composition behind
    /// the frozen IOwnedFeatureEventPublisher / IFeatureEventSubscriber
    /// contract seams. Official and ecosystem features get equal rights:
    /// every publish goes through a per-feature publisher view whose
    /// declared event id must be derived from the OWNER's FeatureId
    /// (&lt;owner&gt;/&lt;event-name&gt;), so a feature can never publish
    /// under another feature's identity.
    ///
    /// The platform host identity (HostTickClock.HostPublisherId) is a
    /// RESERVED identity: Publisher(owner) fail-fasts on it (R1-Spec fix —
    /// otherwise any code could mint a host publisher and forge host
    /// ticks), and the host clock publishes through the internal host path
    /// (TryPublishHost), still bound to the same derivation rule.
    ///
    /// Frozen semantics (mirrors the DEV-V2-14 handler model):
    ///   - handlers run outside the state lock (snapshot dispatch);
    ///   - one handler's exception never reaches its peers nor the publisher
    ///     (isolated, surfaced as a structured diagnostic — never silently
    ///     swallowed);
    ///   - every Subscribe returns its own idempotent handle whose Dispose
    ///     removes only its own delegate; a null handler is a developer
    ///     error and throws its argument exception (fail-fast);
    ///   - the host drops a feature's whole subscription set at the feature
    ///     stop boundary (UnsubscribeAll) — the frozen「停止自动注销」clock
    ///     invariant. Handoff contract for the host module path
    ///     (DEV-V2-21/22, where IFeatureModule.Stop is driven): AFTER
    ///     module.Stop returns, the host calls UnsubscribeAll(feature) —
    ///     the module's final in-Stop publishes still dispatch, and nothing
    ///     from the feature survives the stop boundary.
    /// </summary>
    public sealed class FeatureEventBus
    {
        private readonly object gate = new object();
        private readonly List<Subscription> subscriptions = new List<Subscription>();
        private readonly Action<string> diagnosticSink;

        /// <summary>
        /// The structured diagnostic sink (never throws into the bus). The
        /// composition root binds the runtime log; tests capture their own.
        /// Rejections and handler errors surface here instead of vanishing.
        /// </summary>
        public FeatureEventBus(Action<string> diagnosticSink = null)
        {
            this.diagnosticSink = diagnosticSink;
        }

        private sealed class Subscription
        {
            internal FeatureId Owner;
            internal Type EventType;
            internal Action<object> Handler;
        }

        /// <summary>
        /// A publisher view bound to one feature identity. The platform host
        /// identity is reserved (host clock only) and fail-fasts here.
        /// </summary>
        public IOwnedFeatureEventPublisher Publisher(FeatureId owner)
        {
            if (owner.Value == HostTickClock.HostPublisherId)
                throw new ArgumentException("the platform host identity is reserved for the host clock and cannot be minted into a public publisher view", nameof(owner));
            return new FeatureEventPublisher(owner, this);
        }

        /// <summary>A subscriber view bound to one feature identity.</summary>
        public IFeatureEventSubscriber Subscriber(FeatureId owner) { return new FeatureEventSubscriber(owner, this); }

        /// <summary>
        /// Host stop boundary: drops every subscription made through the
        /// owner's subscriber views. Returns true when at least one
        /// subscription was removed. See the class comment for the exact
        /// handoff contract with the host module stop path.
        /// </summary>
        public bool UnsubscribeAll(FeatureId owner)
        {
            if (string.IsNullOrEmpty(owner.Value)) return false;
            lock (gate)
            {
                var removed = false;
                for (var i = subscriptions.Count - 1; i >= 0; i--)
                {
                    if (!string.Equals(subscriptions[i].Owner.Value, owner.Value, StringComparison.Ordinal)) continue;
                    subscriptions.RemoveAt(i);
                    removed = true;
                }
                return removed;
            }
        }

        internal bool TryPublish<TEvent>(FeatureId owner, string declaredEventId, TEvent value)
        {
            if (string.IsNullOrEmpty(owner.Value) || string.IsNullOrEmpty(declaredEventId)
                || !declaredEventId.StartsWith(owner.Value + "/", StringComparison.Ordinal)
                || declaredEventId.Length == owner.Value.Length + 1)
            {
                EmitDiagnostic("event=feature-event result=publish-rejected owner=" + owner.Value
                    + " eventId=" + declaredEventId + " reason=identity-not-derived-from-owner");
                return false;
            }
            return Dispatch(declaredEventId, value);
        }

        /// <summary>
        /// The host clock's internal publish path — the ONLY route to the
        /// reserved host identity (R1-Spec fix). Same derivation rule as the
        /// feature path: the declared id must be host/&lt;event-name&gt;.
        /// </summary>
        internal bool TryPublishHost<TEvent>(string declaredEventId, TEvent value)
        {
            if (string.IsNullOrEmpty(declaredEventId)
                || !declaredEventId.StartsWith(HostTickClock.HostPublisherId + "/", StringComparison.Ordinal)
                || declaredEventId.Length == HostTickClock.HostPublisherId.Length + 1)
            {
                EmitDiagnostic("event=host-event result=publish-rejected eventId=" + declaredEventId + " reason=identity-not-derived-from-host");
                return false;
            }
            return Dispatch(declaredEventId, value);
        }

        private bool Dispatch<TEvent>(string declaredEventId, TEvent value)
        {
            Action<object>[] snapshot;
            lock (gate)
            {
                var matches = new List<Action<object>>();
                for (var i = 0; i < subscriptions.Count; i++)
                {
                    var record = subscriptions[i];
                    if (record.EventType != typeof(TEvent)) continue;
                    matches.Add(record.Handler);
                }
                snapshot = matches.ToArray();
            }
            // Dispatch outside the state lock; one handler's exception never
            // reaches its peers nor propagates back into the publisher.
            for (var i = 0; i < snapshot.Length; i++)
            {
                try { snapshot[i](value); }
                catch (Exception error)
                {
                    EmitDiagnostic("event=feature-event result=handler-error eventId=" + declaredEventId
                        + " errorType=" + error.GetType().Name + " message=" + error.Message);
                }
            }
            return true;
        }

        internal void EmitDiagnostic(string line)
        {
            var sink = diagnosticSink;
            if (sink == null) return;
            try { sink(line); }
            catch (Exception) { }
        }

        private sealed class FeatureEventPublisher : IOwnedFeatureEventPublisher
        {
            private readonly FeatureId owner;
            private readonly FeatureEventBus bus;
            internal FeatureEventPublisher(FeatureId owner, FeatureEventBus bus)
            {
                this.owner = owner;
                this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            }
            public bool TryPublish<TEvent>(string declaredEventId, TEvent value) { return bus.TryPublish(owner, declaredEventId, value); }
        }

        private sealed class FeatureEventSubscriber : IFeatureEventSubscriber
        {
            private readonly FeatureId owner;
            private readonly FeatureEventBus bus;
            internal FeatureEventSubscriber(FeatureId owner, FeatureEventBus bus)
            {
                this.owner = owner;
                this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            }
            public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
            {
                if (handler == null) throw new ArgumentNullException(nameof(handler));
                var record = new Subscription { Owner = owner, EventType = typeof(TEvent), Handler = value => handler((TEvent)value) };
                lock (bus.gate) bus.subscriptions.Add(record);
                return new SubscriptionHandle(bus, record);
            }
        }

        private sealed class SubscriptionHandle : IDisposable
        {
            private readonly FeatureEventBus bus;
            private readonly Subscription record;
            internal SubscriptionHandle(FeatureEventBus bus, Subscription record) { this.bus = bus; this.record = record; }
            public void Dispose()
            {
                lock (bus.gate)
                {
                    // Removes only its own record (reference identity); a
                    // second Dispose or a dispose after UnsubscribeAll is a
                    // safe no-op.
                    bus.subscriptions.Remove(record);
                }
            }
        }
    }
}
