using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal readonly struct ClientUiEnvironment
    {
        public bool ClientUiAvailable { get; }
        public bool IsBatchMode { get; }
        public bool Headless { get; }

        public ClientUiEnvironment(bool clientUiAvailable, bool isBatchMode, bool headless)
        {
            ClientUiAvailable = clientUiAvailable;
            IsBatchMode = isBatchMode;
            Headless = headless;
        }

        public bool CanCompose
        {
            get { return ClientUiAvailable && !IsBatchMode && !Headless; }
        }
    }

    internal interface IClientUiRoot { }

    internal interface IClientUiInventorySurface { }

    internal interface IClientUiFeatureComponent
    {
        void OnUiInitialized(IClientUiRoot root);
        void OnInventoryOpened(IClientUiInventorySurface inventory);
        void OnInventoryClosed();
        void OnUiDestroyed();
    }

    internal readonly struct ClientUiRegistration
    {
        public FeatureId Feature { get; }
        public Func<IClientUiFeatureComponent> Factory { get; }

        public ClientUiRegistration(FeatureId feature, Func<IClientUiFeatureComponent> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            Feature = feature;
            Factory = factory;
        }
    }

    internal sealed class GeneratedClientUiRegistry
    {
        private readonly ClientUiRegistration[] registrations;

        public GeneratedClientUiRegistry(IReadOnlyList<ClientUiRegistration> registrations)
        {
            if (registrations == null) throw new ArgumentNullException(nameof(registrations));
            this.registrations = new ClientUiRegistration[registrations.Count];
            for (var index = 0; index < registrations.Count; index++)
            {
                this.registrations[index] = registrations[index];
            }
        }

        internal int Count { get { return registrations.Length; } }

        internal ClientUiRegistration Get(int index)
        {
            if ((uint)index >= (uint)registrations.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return registrations[index];
        }
    }

    internal enum ClientUiCompositionState : byte
    {
        NotInitialized,
        Unavailable,
        Ready,
        Destroyed
    }

    internal sealed class ClientUiCompositionRoot
    {
        private readonly GeneratedClientUiRegistry registry;
        private readonly List<ComponentSlot> activeComponents = new List<ComponentSlot>();
        private readonly List<FeatureId> isolatedFeatureIds = new List<FeatureId>();
        private ClientUiCompositionState state;
        private bool safeMode;
        private int safeModeDiagnosticCount;

        public ClientUiCompositionRoot(GeneratedClientUiRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            state = ClientUiCompositionState.NotInitialized;
        }

        internal ClientUiCompositionState State { get { return state; } }
        internal IReadOnlyList<FeatureId> IsolatedFeatureIds { get { return isolatedFeatureIds.AsReadOnly(); } }
        internal bool IsSafeMode { get { return safeMode; } }
        internal int SafeModeDiagnosticCount { get { return safeModeDiagnosticCount; } }

        internal bool Initialize(ClientUiEnvironment environment, IClientUiRoot root)
        {
            if (state == ClientUiCompositionState.Ready) return true;
            if (state == ClientUiCompositionState.Destroyed || safeMode) return false;
            if (!environment.CanCompose || root == null)
            {
                state = ClientUiCompositionState.Unavailable;
                return false;
            }

            for (var index = 0; index < registry.Count; index++)
            {
                var registration = registry.Get(index);
                IClientUiFeatureComponent component = null;
                try
                {
                    component = registration.Factory();
                    if (component == null) throw new InvalidOperationException("ClientUi factory returned null.");
                    component.OnUiInitialized(root);
                    activeComponents.Add(new ComponentSlot(registration.Feature, component));
                }
                catch (Exception)
                {
                    IsolateComponent(registration.Feature, component);
                }
            }

            state = ClientUiCompositionState.Ready;
            return true;
        }

        internal void OpenInventory(IClientUiInventorySurface inventory)
        {
            if (state != ClientUiCompositionState.Ready || inventory == null) return;
            for (var index = activeComponents.Count - 1; index >= 0; index--)
            {
                var slot = activeComponents[index];
                try { slot.Component.OnInventoryOpened(inventory); }
                catch (Exception) { IsolateAt(index); }
            }
        }

        internal void CloseInventory()
        {
            if (state != ClientUiCompositionState.Ready) return;
            for (var index = activeComponents.Count - 1; index >= 0; index--)
            {
                var slot = activeComponents[index];
                try { slot.Component.OnInventoryClosed(); }
                catch (Exception) { IsolateAt(index); }
            }
        }

        internal void Destroy()
        {
            if (state == ClientUiCompositionState.Destroyed) return;
            while (activeComponents.Count > 0)
            {
                var index = activeComponents.Count - 1;
                var slot = activeComponents[index];
                activeComponents.RemoveAt(index);
                try { slot.Component.OnUiDestroyed(); }
                catch (Exception) { isolatedFeatureIds.Add(slot.Feature); }
            }
            state = ClientUiCompositionState.Destroyed;
        }

        internal void EnterSafeMode(Action<string> diagnostic)
        {
            if (safeMode) return;
            safeMode = true;
            safeModeDiagnosticCount++;
            if (diagnostic != null)
            {
                try { diagnostic("BUE-CLIENTUI-SAFEMODE"); }
                catch (Exception) { }
            }
            while (activeComponents.Count > 0)
            {
                var index = activeComponents.Count - 1;
                var slot = activeComponents[index];
                activeComponents.RemoveAt(index);
                try { slot.Component.OnUiDestroyed(); }
                catch (Exception) { isolatedFeatureIds.Add(slot.Feature); }
            }
            state = ClientUiCompositionState.Unavailable;
        }

        private void IsolateAt(int index)
        {
            var slot = activeComponents[index];
            activeComponents.RemoveAt(index);
            IsolateComponent(slot.Feature, slot.Component);
        }

        private void IsolateComponent(FeatureId feature, IClientUiFeatureComponent component)
        {
            if (component != null)
            {
                try { component.OnUiDestroyed(); }
                catch (Exception) { }
            }
            isolatedFeatureIds.Add(feature);
        }

        private sealed class ComponentSlot
        {
            public FeatureId Feature { get; }
            public IClientUiFeatureComponent Component { get; }

            public ComponentSlot(FeatureId feature, IClientUiFeatureComponent component)
            {
                Feature = feature;
                Component = component;
            }
        }
    }
}
