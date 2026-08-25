using System;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Registration;
using BetterUnturnedExperience.NoOpFixture;
using BetterUnturnedExperience.Plugin;

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
                var accepted = new NoOpFeatureBootstrap().Awake();
                Assert(accepted.Accepted && accepted.Feature.Value == "io.github.yu80rice.bue.noop", "independent fixture registers through public host bridge");
                Assert(runtime.CompleteRuntime() && runtime.Catalog.Entries.Count == 1, "fixture reaches runtime ready through host barrier");
                Assert(runtime.Phase == FeatureRegistrationPhase.RuntimeReady, "runtime barrier enters RuntimeReady");
                var late = NoOpFeatureRegistration.Register();
                Assert(late.Reason == FeatureRegistrationReason.PhaseClosed, "fixture late registration is rejected");
                Console.WriteLine("DEV-13 external registration barrier tests: PASS"); return 0;
            }
            catch (Exception error) { Console.WriteLine("DEV-13 external registration barrier tests: FAIL"); Console.WriteLine(error.GetType().FullName); Console.WriteLine(error.Message); return 1; }
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
