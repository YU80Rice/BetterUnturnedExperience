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
    /// DEV-V3-02 ownership routing: the bus routes by the registered
    /// (EventId, EventType, owner) triple with ONE owning EventId per
    /// payload type. The official types are host-registered at composition
    /// (TidyCompleted → LIT, HostTick → the reserved host identity); a
    /// feature registers its own payload types through its
    /// IFeatureEventRegistry view (bootstrap.EventRegistry) after module
    /// registration and before publishing/subscribing. A publish is rejected
    /// (explicit false + structured diagnostic + zero dispatch) when the
    /// payload type is unregistered, the declared id is not the type's
    /// owning EventId, or the publisher owner is not the type's registered
    /// owner — a valid own-prefix id carrying someone else's payload type is
    /// the forge the prefix rule alone cannot catch. A subscription to an
    /// unregistered type is a developer error and fails fast (the
    /// null-handler discipline).
    ///
    /// The platform host identity (HostTickClock.HostPublisherId) is a
    /// RESERVED identity: Publisher(owner) fail-fasts on it (R1-Spec fix —
    /// otherwise any code could mint a host publisher and forge host
    /// ticks), and the host clock publishes through the internal host path
    /// (TryPublishHost), still bound to the same ownership routing.
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
        // DEV-V3-02: the ownership routing index — the (EventId, EventType,
        // owner) triple with ONE owning EventId per payload type and one type
        // per EventId. Official types are seeded at composition; ecosystem
        // types register through the public IFeatureEventRegistry seam.
        private readonly Dictionary<Type, OwnedEventType> ownedTypes = new Dictionary<Type, OwnedEventType>();
        private readonly Dictionary<string, OwnedEventType> ownedEventIds = new Dictionary<string, OwnedEventType>(StringComparer.Ordinal);
        private readonly Action<string> diagnosticSink;

        private sealed class OwnedEventType
        {
            internal string EventId;
            internal string Owner;
        }

        /// <summary>
        /// The structured diagnostic sink (never throws into the bus). The
        /// composition root binds the runtime log; tests capture their own.
        /// Rejections and handler errors surface here instead of vanishing.
        /// The constructor also registers the OFFICIAL event types (the host
        /// composition-time registration of the frozen mapping:
        /// TidyCompleted → LIT, HostTick → the reserved host identity) — the
        /// owner is the EventId prefix, the same &lt;owner&gt;/&lt;event-name&gt;
        /// derivation every registration obeys. These mappings cannot be
        /// re-registered or re-purposed (duplicate rejections), so no feature
        /// can mint them.
        /// </summary>
        public FeatureEventBus(Action<string> diagnosticSink = null)
        {
            this.diagnosticSink = diagnosticSink;
            RegisterOfficial(typeof(TidyCompleted), TidyCompleted.EventId);
            RegisterOfficial(typeof(HostTick), HostTick.EventId);
        }

        private void RegisterOfficial(Type eventType, string eventId)
        {
            var owner = OwnerOf(eventId);
            if (owner == null) return;
            lock (gate)
            {
                if (ownedTypes.ContainsKey(eventType) || ownedEventIds.ContainsKey(eventId)) return;
                var record = new OwnedEventType { EventId = eventId, Owner = owner };
                ownedTypes.Add(eventType, record);
                ownedEventIds.Add(eventId, record);
            }
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
        /// DEV-V3-02: a registration view bound to one feature identity — the
        /// public seam (IFeatureEventRegistry) a module reaches through its
        /// bootstrap to register its own payload types after module
        /// registration and before publishing/subscribing. The platform host
        /// identity is reserved (the official types are host-registered at
        /// composition) and fail-fasts here, mirroring the publisher view.
        /// </summary>
        public IFeatureEventRegistry EventRegistry(FeatureId owner)
        {
            if (owner.Value == HostTickClock.HostPublisherId)
                throw new ArgumentException("the platform host identity is reserved for the host clock and cannot be minted into a registration view", nameof(owner));
            return new FeatureEventRegistryView(owner, this);
        }

        /// <summary>
        /// DEV-V3-02: the registration pipeline — format first (BUE-EVT-001),
        /// then the owner prefix (BUE-EVT-002), then the one-type-one-id
        /// gates (BUE-EVT-003 type duplicate / BUE-EVT-004 id collision).
        /// Every rejection is an explicit result with a structured diagnostic;
        /// nothing throws across the module boundary and no mapping is ever
        /// overwritten.
        /// </summary>
        internal FeatureEventRegistrationResult Register(FeatureId owner, Type eventType, string eventId)
        {
            var duplicate = 0;
            if (string.IsNullOrEmpty(owner.Value) || string.IsNullOrEmpty(eventId)
                || eventId.IndexOf('/') <= 0 || eventId.IndexOf('/') == eventId.Length - 1)
            {
                EmitDiagnostic("event=event-registry result=register-rejected owner=" + owner.Value + " eventId=" + eventId
                    + " reason=invalid-event-id diagnosticId=BUE-EVT-001");
                return new FeatureEventRegistrationResult(false, FeatureEventRegistrationReason.InvalidEventId, "BUE-EVT-001");
            }
            if (!string.Equals(OwnerOf(eventId), owner.Value, StringComparison.Ordinal))
            {
                EmitDiagnostic("event=event-registry result=register-rejected owner=" + owner.Value + " eventId=" + eventId
                    + " reason=event-id-not-derived-from-owner diagnosticId=BUE-EVT-002");
                return new FeatureEventRegistrationResult(false, FeatureEventRegistrationReason.EventIdNotDerivedFromOwner, "BUE-EVT-002");
            }
            lock (gate)
            {
                // The duplicate gates decide under the lock; the diagnostic
                // emits OUTSIDE it (the sink never runs under the state lock
                // — the frozen no-callback-under-lock discipline).
                if (ownedTypes.ContainsKey(eventType)) duplicate = 3;
                else if (ownedEventIds.ContainsKey(eventId)) duplicate = 4;
                else
                {
                    var record = new OwnedEventType { EventId = eventId, Owner = owner.Value };
                    ownedTypes.Add(eventType, record);
                    ownedEventIds.Add(eventId, record);
                }
            }
            if (duplicate == 3)
            {
                EmitDiagnostic("event=event-registry result=register-rejected owner=" + owner.Value + " eventId=" + eventId
                    + " reason=event-type-already-registered diagnosticId=BUE-EVT-003");
                return new FeatureEventRegistrationResult(false, FeatureEventRegistrationReason.EventTypeAlreadyRegistered, "BUE-EVT-003");
            }
            if (duplicate == 4)
            {
                EmitDiagnostic("event=event-registry result=register-rejected owner=" + owner.Value + " eventId=" + eventId
                    + " reason=event-id-already-registered diagnosticId=BUE-EVT-004");
                return new FeatureEventRegistrationResult(false, FeatureEventRegistrationReason.EventIdAlreadyRegistered, "BUE-EVT-004");
            }
            EmitDiagnostic("event=event-registry result=registered owner=" + owner.Value + " eventId=" + eventId + " diagnosticId=BUE-EVT-ACCEPT");
            return new FeatureEventRegistrationResult(true, FeatureEventRegistrationReason.None, "BUE-EVT-ACCEPT");
        }

        /// <summary>
        /// DEV-V3-02: the subscription gate — a payload type that never
        /// registered has no (EventId, Type) route, so subscribing to it is a
        /// developer error and fails fast (the null-handler discipline) with
        /// a structured diagnostic next to it.
        /// </summary>
        internal void EnsureTypeRegistered(Type eventType)
        {
            bool registered;
            lock (gate) registered = ownedTypes.ContainsKey(eventType);
            if (registered) return;
            EmitDiagnostic("event=feature-event result=subscribe-rejected owner-side=subscriber eventType=" + eventType.Name + " reason=type-not-registered");
            throw new ArgumentException("the payload type " + eventType.Name + " is not registered on the event bus; a feature registers its own payload types through its EventRegistry seam before subscribing", nameof(eventType));
        }

        private static string OwnerOf(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;
            var separator = eventId.IndexOf('/');
            if (separator <= 0 || separator == eventId.Length - 1) return null;
            return eventId.Substring(0, separator);
        }

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
            // DEV-V3-02 ownership routing (the T3 frozen publish flow:
            // ①format/prefix → ②the payload type's registered ownership →
            // ③publisher owner == registered owner → dispatch): the payload
            // type must be registered and owned by the publisher, and the
            // declared id must be its ONE owning EventId — a valid own-prefix
            // id carrying someone else's payload type is the forge the prefix
            // rule alone cannot catch. No rejection dispatches.
            OwnedEventType registration;
            lock (gate) ownedTypes.TryGetValue(typeof(TEvent), out registration);
            if (registration == null)
            {
                EmitDiagnostic("event=feature-event result=publish-rejected owner=" + owner.Value + " eventId=" + declaredEventId
                    + " eventType=" + typeof(TEvent).Name + " reason=type-not-registered");
                return false;
            }
            if (!string.Equals(registration.Owner, owner.Value, StringComparison.Ordinal))
            {
                EmitDiagnostic("event=feature-event result=publish-rejected owner=" + owner.Value + " eventId=" + declaredEventId
                    + " eventType=" + typeof(TEvent).Name + " reason=owner-mismatch");
                return false;
            }
            if (!string.Equals(registration.EventId, declaredEventId, StringComparison.Ordinal))
            {
                EmitDiagnostic("event=feature-event result=publish-rejected owner=" + owner.Value + " eventId=" + declaredEventId
                    + " eventType=" + typeof(TEvent).Name + " reason=event-id-mismatch");
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
            // DEV-V3-02: even the internal host path routes through the
            // ownership index — the type must be registered, the declared id
            // must be its owning EventId, and the registered owner must be
            // the reserved host identity.
            OwnedEventType registration;
            lock (gate) ownedTypes.TryGetValue(typeof(TEvent), out registration);
            if (registration == null
                || !string.Equals(registration.EventId, declaredEventId, StringComparison.Ordinal)
                || !string.Equals(registration.Owner, HostTickClock.HostPublisherId, StringComparison.Ordinal))
            {
                EmitDiagnostic("event=host-event result=publish-rejected eventId=" + declaredEventId
                    + " eventType=" + typeof(TEvent).Name + " reason=host-ownership-mismatch");
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
                // DEV-V3-02: no (EventId, Type) route without registration —
                // the unregistered-type subscription is a developer error and
                // fails fast (the null-handler discipline). One payload type
                // has exactly one owning EventId, so subscribing by type
                // subscribes to that type's registered route.
                bus.EnsureTypeRegistered(typeof(TEvent));
                var record = new Subscription { Owner = owner, EventType = typeof(TEvent), Handler = value => handler((TEvent)value) };
                lock (bus.gate) bus.subscriptions.Add(record);
                return new SubscriptionHandle(bus, record);
            }
        }

        private sealed class FeatureEventRegistryView : IFeatureEventRegistry
        {
            private readonly FeatureId owner;
            private readonly FeatureEventBus bus;
            internal FeatureEventRegistryView(FeatureId owner, FeatureEventBus bus)
            {
                this.owner = owner;
                this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            }
            public FeatureEventRegistrationResult Register<TEvent>(string eventId) { return bus.Register(owner, typeof(TEvent), eventId); }
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
