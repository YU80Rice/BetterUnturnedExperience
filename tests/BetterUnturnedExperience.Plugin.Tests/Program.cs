using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.NoOpFixture;
using BetterUnturnedExperience.Plugin;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Plugin.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                AssertSingleDllAssemblyClosure();
                AssertExternalSdkAssemblyIdentity();
                Assert(BootstrapGuard.Decide(false, false, true) == BootstrapDecision.Client, "client decision");
                Assert(BootstrapGuard.Decide(true, true, true) == BootstrapDecision.Headless, "headless decision");
                Assert(BootstrapGuard.Decide(false, true, true) == BootstrapDecision.Headless, "explicit headless decision");
                Assert(BootstrapGuard.Decide(false, false, false) == BootstrapDecision.Unavailable, "unavailable decision");
                BueRuntimeHost.Clear();
                var unavailable = NoOpFeatureRegistration.Register();
                Assert(unavailable.Reason == FeatureRegistrationReason.HostUnavailable, "external fixture fails closed before host initialization");
                var runtime = new FeatureRegistrationRuntime();
                BueRuntimeHost.Bind(runtime);
                runtime.OpenRegistration();
                var official = BetterItemInteractionFeatureRegistration.Register();
                Assert(official.Accepted && official.Feature.Value == "io.github.yu80rice.bue.better-item-interaction", "official feature registers through the same public host bridge");
                var accepted = new NoOpFeatureBootstrap().Awake();
                Assert(accepted.Accepted && accepted.Feature.Value == "io.github.yu80rice.bue.noop", "independent fixture registers through public host bridge");
                Assert(runtime.CompleteRuntime() && runtime.Catalog.Entries.Count == 2, "official and fixture reach runtime ready through host barrier");
                Assert(officialRegistrationHasClientUi(official), "official feature exposes a ClientUi satellite descriptor");
                AssertClientUiCompositionGates();
                AssertRuntimePumpBridge();
                AssertRuntimeDriverDispatchesButtonInjectionSeam();
                AssertPanelDispatchReachesButtonInjectionSeam();
                AssertManagementPanelOpenHooks();
                Assert(RequiresParentRebindSemantics(), "management panel resets bindings when UI parent changes");
                Assert(runtime.Phase == FeatureRegistrationPhase.RuntimeReady, "runtime barrier enters RuntimeReady");
                Assert(runtime.Catalog.Entries[0].Definition.Feature.Value == "io.github.yu80rice.bue.better-item-interaction", "catalog order is deterministic by feature identity");
                var late = NoOpFeatureRegistration.Register();
                Assert(late.Reason == FeatureRegistrationReason.PhaseClosed, "fixture late registration is rejected");
                Console.WriteLine("DEV-14/DEV-16B plugin runtime tests: PASS"); return 0;
            }
            catch (Exception error) { Console.WriteLine("DEV-14 official registration parity tests: FAIL"); Console.WriteLine(error.ToString()); return 1; }
        }
        private static bool RequiresParentRebindSemantics()
        {
            var first = new object();
            var second = new object();
            return !BueNativeManagementPanel.RequiresParentRebind(first, first)
                && BueNativeManagementPanel.RequiresParentRebind(first, second)
                && !BueNativeManagementPanel.RequiresParentRebind(first, null);
        }

        private static void AssertRuntimePumpBridge()
        {
            var ticks = 0;
            var pump = new BueRuntimePump(() => ticks++);
            pump.Tick();
            Assert(ticks == 1, "runtime pump forwards one main-thread tick");
            pump.Clear();
            pump.Tick();
            Assert(ticks == 1, "cleared runtime pump does not call stale plugin state");

            var slot = new BueRuntimePumpSlot();
            var first = slot.GetOrCreate(() => { });
            var second = slot.GetOrCreate(() => { });
            Assert(object.ReferenceEquals(first, second), "runtime pump slot is idempotent");
            slot.Clear();
            var third = slot.GetOrCreate(() => { });
            Assert(!object.ReferenceEquals(first, third), "cleared runtime pump slot creates a fresh generation");
        }

        private static void AssertRuntimeDriverDispatchesButtonInjectionSeam()
        {
            var frame = 10;
            var ticks = 0;
            var driver = new BueRuntimeTickDispatcher(source => ticks++, () => frame);
            Assert(driver.Dispatch(BueNativeManagementPanel.TickSource.Update), "runtime driver dispatches the button injection seam");
            Assert(ticks == 1, "button injection seam receives one runtime tick");
            Assert(!driver.Dispatch(BueNativeManagementPanel.TickSource.Update), "runtime driver de-duplicates the same source within one frame");
            Assert(driver.Dispatch(BueNativeManagementPanel.TickSource.Harmony), "runtime driver still permits a distinct source within one frame");
            frame++;
            Assert(driver.Dispatch(BueNativeManagementPanel.TickSource.Update), "runtime driver resets source de-duplication on the next frame");
            driver.Isolate();
            Assert(!driver.Dispatch(BueNativeManagementPanel.TickSource.RuntimePump), "isolated runtime driver rejects stale tick");
            Assert(ticks == 3, "isolated runtime driver does not call button injection seam");

            var failures = 0;
            BueRuntimeTickDispatcher throwing = null;
            throwing = new BueRuntimeTickDispatcher(source =>
            {
                failures++;
                Assert(!throwing.Dispatch(BueNativeManagementPanel.TickSource.Harmony), "reentrant button injection is rejected");
                throw new InvalidOperationException("synthetic button injection failure");
            }, () => 20);
            Assert(!throwing.Dispatch(BueNativeManagementPanel.TickSource.Update), "runtime driver rejects the failing first tick");
            Assert(failures == 1, "failing button injection seam is called once");
            Assert(!throwing.Dispatch(BueNativeManagementPanel.TickSource.Harmony), "runtime driver opens fail-closed barrier after first error");
            Assert(failures == 1, "fail-closed barrier blocks later harmony callbacks");
        }

        private static void AssertPanelDispatchReachesButtonInjectionSeam()
        {
            var composition = new BueClientUiCompositionRoot();
            var recording = new RecordingButtonInjectionSeam();
            var panel = new BueNativeManagementPanel(composition.ManagementPanel, null, recording);
            Assert(panel.Dispatch(BueNativeManagementPanel.TickSource.Update), "panel dispatch reaches the button injection seam");
            Assert(recording.Sources.Count == 1, "panel performs one injection pass for the update source");
            Assert(recording.Sources[0] == "Update", "panel forwards the source identity to injection");
            Assert(!panel.Dispatch(BueNativeManagementPanel.TickSource.Update), "panel rejects duplicate same-frame injection");
            panel.Destroy();

            var failures = 0;
            var failing = new BueNativeManagementPanel(composition.ManagementPanel, null,
                new ThrowingButtonInjectionSeam(() => failures++));
            Assert(!failing.Dispatch(BueNativeManagementPanel.TickSource.RuntimePump), "button injection failure is rejected by panel dispatch");
            Assert(failures == 1, "button injection failure enters the seam exactly once");
            Assert(failing.TickIsolated, "button injection failure isolates the panel");
            Assert(!failing.Dispatch(BueNativeManagementPanel.TickSource.Harmony), "isolated panel rejects later Harmony injection");
            failing.Destroy();
        }

        private sealed class RecordingButtonInjectionSeam : IBueButtonInjectionSeam
        {
            internal readonly System.Collections.Generic.List<string> Sources = new System.Collections.Generic.List<string>();

            public void Inject(string source)
            {
                Sources.Add(source);
            }
        }

        private sealed class ThrowingButtonInjectionSeam : IBueButtonInjectionSeam
        {
            private readonly System.Action onAttempt;

            internal ThrowingButtonInjectionSeam(System.Action onAttempt)
            {
                this.onAttempt = onAttempt;
            }

            public void Inject(string source)
            {
                onAttempt();
                throw new InvalidOperationException("synthetic button injection failure");
            }
        }
        private static void AssertManagementPanelOpenHooks()
        {
            var composition = new BueClientUiCompositionRoot();
            var panel = new BueNativeManagementPanel(composition.ManagementPanel, null);
            try
            {
                panel.Initialize();
                var workshop = Harmony.GetPatchInfo(AccessTools.Method(typeof(MenuWorkshopUI), "open"));
                var pause = Harmony.GetPatchInfo(AccessTools.Method(typeof(PlayerPauseUI), "open"));
                var dashboard = Harmony.GetPatchInfo(AccessTools.Method(typeof(MenuDashboardUI), "open"));
                var menuUpdate = Harmony.GetPatchInfo(AccessTools.Method(typeof(MenuUI), "Update"));
                var playerUpdate = Harmony.GetPatchInfo(AccessTools.Method(typeof(PlayerUI), "Update"));
                Assert(HasOwner(workshop, "io.github.yu80rice.bue.management-panel"), "workshop open hook is installed");
                Assert(HasOwner(pause, "io.github.yu80rice.bue.management-panel"), "pause open hook is installed");
                Assert(HasOwner(dashboard, "io.github.yu80rice.bue.management-panel"), "dashboard open hook is installed");
                Assert(HasOwner(menuUpdate, "io.github.yu80rice.bue.management-panel"), "menu host update hook is installed");
                Assert(HasOwner(playerUpdate, "io.github.yu80rice.bue.management-panel"), "player host update hook is installed");
            }
            finally
            {
                panel.Destroy();
            }
        }
        private static bool HasOwner(HarmonyLib.Patches patches, string owner)
        {
            if (patches == null) return false;
            if (patches.Postfixes == null) return false;
            foreach (var patch in patches.Postfixes)
            {
                if (patch != null && patch.owner == owner) return true;
            }
            return false;
        }
        private static bool officialRegistrationHasClientUi(FeatureRegistrationResult result)
        {
            var runtime = BueRuntimeHost.CurrentRuntime;
            if (runtime == null || runtime.Catalog == null) return false;
            for (var index = 0; index < runtime.Catalog.Entries.Count; index++)
            {
                var entry = runtime.Catalog.Entries[index];
                if (entry.Definition.Feature.Value == result.Feature.Value) return entry.ClientUi != null;
            }
            return false;
        }
        private static void AssertClientUiCompositionGates()
        {
            var client = new BueClientUiCompositionRoot();
            Assert(client.Initialize(false, false, true), "client composition root initializes on client");
            Assert(client.IsReady, "client composition root reaches ready");
            var firstFactoryCount = client.FactoryInvocationCount;
            Assert(client.Initialize(false, false, true), "repeated client initialization is idempotent");
            Assert(client.FactoryInvocationCount == firstFactoryCount, "repeated initialization does not recreate UI components");
            client.Destroy();
            Assert(!client.Initialize(false, false, true), "destroyed composition root is not reinitialized");

            var headless = new BueClientUiCompositionRoot();
            Assert(!headless.Initialize(true, true, true), "batch/headless gate blocks composition");
            Assert(headless.FactoryInvocationCount == 0, "headless gate never invokes UI factory");

            var unavailable = new BueClientUiCompositionRoot();
            Assert(!unavailable.Initialize(false, false, false), "native UI unavailable blocks composition");
            Assert(unavailable.FactoryInvocationCount == 0, "native UI unavailable never invokes UI factory");
        }
        private static void AssertSingleDllAssemblyClosure()
        {
            Assert(typeof(FeatureId).Assembly == typeof(BueRuntimeHost).Assembly, "public contract types must be embedded in the BUE runtime assembly");
            Assert(typeof(IFeatureRegistration).Assembly == typeof(BueRuntimeHost).Assembly, "registration ABI must resolve from the BUE runtime assembly");
            var references = typeof(BetterUnturnedExperiencePlugin).Assembly.GetReferencedAssemblies();
            foreach (var reference in references)
            {
                Assert(reference.Name != "BetterUnturnedExperience.Core", "BUE main assembly must not require private Core runtime DLL");
                Assert(reference.Name != "BetterUnturnedExperience.Contracts", "BUE main assembly must not require private Contracts runtime DLL");
            }
        }
        private static void AssertExternalSdkAssemblyIdentity()
        {
            var fixtureAssembly = typeof(BetterUnturnedExperience.NoOpFixture.NoOpFeaturePlugin).Assembly;
            var references = fixtureAssembly.GetReferencedAssemblies();
            var bueReference = false;
            foreach (var reference in references)
            {
                Assert(reference.Name != "BetterUnturnedExperience.Contracts", "external SDK output must not bind to Contracts runtime assembly");
                Assert(reference.Name != "BetterUnturnedExperience.Core", "external SDK output must not bind to private Core runtime assembly");
                if (reference.Name == "BetterUnturnedExperience") bueReference = true;
            }
            Assert(bueReference, "external SDK output must bind to the public BUE runtime assembly");
            Assert(typeof(FeatureId).Assembly == typeof(BueRuntimeHost).Assembly, "public ABI identity is resolved by BUE runtime assembly");
        }
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    }
}
