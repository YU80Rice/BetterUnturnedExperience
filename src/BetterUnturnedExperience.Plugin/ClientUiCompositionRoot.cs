using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Placement;

namespace BetterUnturnedExperience.Plugin
{
    /// <summary>
    /// BUE-owned composition boundary for the embedded official ClientUi.
    /// It performs the environment gate before invoking any component factory.
    /// </summary>
    internal sealed class BueClientUiCompositionRoot
    {
        private readonly ClientUiCompositionRoot composition;
        private readonly IClientUiRoot root = new BueClientUiRoot();
        private readonly bool clientUiAvailable;
        private int factoryInvocationCount;

        internal BueClientUiCompositionRoot()
        {
            var feature = BetterItemInteractionSettingsState.Feature;
            var registry = new GeneratedClientUiRegistry(new[]
            {
                new ClientUiRegistration(feature, CreateOfficialComponent)
            });
            composition = new ClientUiCompositionRoot(registry);
            clientUiAvailable = true;
        }

        internal bool IsReady { get { return composition.State == ClientUiCompositionState.Ready; } }
        internal ClientUiCompositionState State { get { return composition.State; } }
        internal int FactoryInvocationCount { get { return factoryInvocationCount; } }
        internal IReadOnlyList<FeatureId> IsolatedFeatureIds { get { return composition.IsolatedFeatureIds; } }

        internal bool Initialize(bool isBatchMode, bool headless, bool nativeUiAvailable)
        {
            var environment = new ClientUiEnvironment(nativeUiAvailable && clientUiAvailable, isBatchMode, headless);
            return composition.Initialize(environment, root);
        }

        internal void Destroy()
        {
            composition.Destroy();
        }

        internal void EnterSafeMode(Action<string> diagnostic)
        {
            composition.EnterSafeMode(diagnostic);
        }

        private IClientUiFeatureComponent CreateOfficialComponent()
        {
            factoryInvocationCount++;
            return new BetterItemInteractionUiComponent(
                new InventoryPreviewPresenter(new InventoryDragPresenter(new PlacementCandidateEvaluator())),
                new NativeInventoryInteractionAdapter(2, 8));
        }

        private sealed class BueClientUiRoot : IClientUiRoot { }
    }
}
