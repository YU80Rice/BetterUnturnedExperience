using System;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    internal static class Dev15CTests
    {
        internal static void Run()
        {
            CallbackOnlyEnqueuesUntilPump();
            InterfaceCaptureUsesProjectionSourceSeam();
            FixedCapacityOverflowFailsClosed();
            StaleGenerationAndSessionAreDropped();
            ConsumerFailureInvalidatesBindingAndQueuedSnapshots();
            OlderNativeRevisionIsDropped();
            MatchingFingerprintConvergesWithoutRejection();
            AmbiguousFingerprintObservesLatestFactWithoutRejection();
            VisualBudgetOnlyClearsAwaitingState();
            ConsumerFailureDoesNotEscapePump();
        }

        private static void CallbackOnlyEnqueuesUntilPump()
        {
            var relay = new NativeInventoryProjectionRelay(4);
            var binding = Binding(7, 100, 363);
            relay.Bind(binding);
            var consumer = new RecordingConsumer();
            Assert(relay.TryEnqueue(Snapshot(binding, 1)), "callback enqueue succeeds");
            Assert(consumer.Count == 0, "callback enqueue does not invoke consumer");
            var report = relay.Pump(consumer);
            Assert(report.Applied == 1 && consumer.Count == 1, "pump applies queued snapshot on controlled path");
        }

        private static void FixedCapacityOverflowFailsClosed()
        {
            var relay = new NativeInventoryProjectionRelay(1);
            var binding = Binding(1, 10, 1);
            relay.Bind(binding);
            Assert(relay.TryEnqueue(Snapshot(binding, 1)), "first snapshot enters bounded queue");
            Assert(!relay.TryEnqueue(Snapshot(binding, 2)), "full queue rejects overflow");
            Assert(relay.Count == 1, "overflow does not corrupt queue count");
        }

        private static void InterfaceCaptureUsesProjectionSourceSeam()
        {
            var relay = new NativeInventoryProjectionRelay(4);
            var binding = Binding(2, 15, 5);
            relay.Bind(binding);
            INativeInventoryProjectionSource source = relay;
            Assert(source.TryCapture(Snapshot(binding, 1)), "projection source interface accepts callback snapshot");
            Assert(relay.Count == 1, "interface capture only enqueues and does not consume");
        }

        private static void StaleGenerationAndSessionAreDropped()
        {
            var relay = new NativeInventoryProjectionRelay(4);
            var binding = Binding(4, 20, 2);
            relay.Bind(binding);
            var consumer = new RecordingConsumer();
            Assert(relay.TryEnqueue(Snapshot(Binding(3, 20, 2), 1)), "stale generation can be queued for filtering");
            Assert(relay.TryEnqueue(Snapshot(Binding(4, 21, 2), 2)), "stale session can be queued for filtering");
            var report = relay.Pump(consumer);
            Assert(report.Applied == 0 && report.Dropped == 2 && consumer.Count == 0, "stale generation/session are silently dropped");
        }

        private static void MatchingFingerprintConvergesWithoutRejection()
        {
            var controller = new AwaitingProjectionController();
            var binding = Binding(8, 30, 7);
            controller.Begin(binding, 1000);
            var result = controller.Apply(Snapshot(binding, 1));
            Assert(result == ProjectionConvergence.Converged, "matching projection converges");
            Assert(controller.State == AwaitingProjectionState.Idle, "matching projection clears awaiting state");
        }

        private static void AmbiguousFingerprintObservesLatestFactWithoutRejection()
        {
            var controller = new AwaitingProjectionController();
            var binding = Binding(9, 31, 8);
            controller.Begin(binding, 1000);
            var different = new NativeInventorySnapshot(9, binding.Container,
                new InventoryItemFingerprint(ItemAssetIdentity.FromItemId(99), 1, 1, 0),
                new ItemGridPosition(7, 2, 2, 0), 2);
            var result = controller.Apply(different);
            Assert(result == ProjectionConvergence.ObservedLatestFact, "ambiguous projection is consumed as latest native fact");
            Assert(controller.State == AwaitingProjectionState.Idle, "ambiguous projection does not remain a rejection state");
        }

        private static void VisualBudgetOnlyClearsAwaitingState()
        {
            var controller = new AwaitingProjectionController();
            controller.Begin(Binding(10, 32, 9), 1000);
            Assert(!controller.Tick(2999), "budget remains active before two seconds");
            Assert(controller.Tick(3000), "two second budget clears visual waiting state");
            Assert(controller.State == AwaitingProjectionState.Idle, "budget timeout returns idle without rejection");
        }

        private static void ConsumerFailureDoesNotEscapePump()
        {
            var relay = new NativeInventoryProjectionRelay(2);
            var binding = Binding(11, 33, 10);
            relay.Bind(binding);
            relay.TryEnqueue(Snapshot(binding, 1));
            var report = relay.Pump(new ThrowingConsumer());
            Assert(report.ConsumerFailures == 1 && report.Applied == 0, "consumer failure is contained by relay pump");
        }

        private static void ConsumerFailureInvalidatesBindingAndQueuedSnapshots()
        {
            var relay = new NativeInventoryProjectionRelay(4);
            var binding = Binding(12, 34, 11);
            relay.Bind(binding);
            relay.TryEnqueue(Snapshot(binding, 1));
            relay.TryEnqueue(Snapshot(binding, 2));
            var report = relay.Pump(new ThrowingConsumer());
            Assert(report.ConsumerFailures == 1 && relay.Count == 0, "consumer failure invalidates binding and clears queued snapshots");
            var healthy = new RecordingConsumer();
            Assert(relay.Pump(healthy).Applied == 0 && healthy.Count == 0, "invalidated relay does not continue consuming stale work");
        }

        private static void OlderNativeRevisionIsDropped()
        {
            var relay = new NativeInventoryProjectionRelay(4);
            var binding = Binding(13, 35, 12);
            relay.Bind(binding);
            relay.TryEnqueue(Snapshot(binding, 3));
            relay.TryEnqueue(Snapshot(binding, 2));
            var consumer = new RecordingConsumer();
            var report = relay.Pump(consumer);
            Assert(report.Applied == 1 && report.Dropped == 1 && consumer.Count == 1, "older native revision is dropped without rewinding projection");
        }

        private static ProjectionBinding Binding(uint drag, uint session, ushort itemId)
        {
            return new ProjectionBinding(drag,
                new ContainerReference(ContainerKind.Storage, 4, session),
                new InventoryItemFingerprint(ItemAssetIdentity.FromItemId(itemId), 2, 1, 0));
        }

        private static NativeInventorySnapshot Snapshot(ProjectionBinding binding, uint revision)
        {
            return new NativeInventorySnapshot(binding.DragGeneration, binding.Container, binding.Fingerprint,
                new ItemGridPosition(binding.Container.Page, 1, 2, binding.Fingerprint.Rotation), revision);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class RecordingConsumer : INativeInventoryProjectionConsumer
        {
            internal int Count;
            public void Apply(NativeInventorySnapshot snapshot) { Count++; }
        }

        private sealed class ThrowingConsumer : INativeInventoryProjectionConsumer
        {
            public void Apply(NativeInventorySnapshot snapshot) { throw new InvalidOperationException("projection consumer failure"); }
        }
    }
}
