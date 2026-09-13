using System;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;
using BetterUnturnedExperience.Core.Placement;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                Run();
                Dev15BTests.Run();
                Dev15CTests.Run();
                Dev15DTests.Run();
                Dev16BTests.Run();
                DevV4DraftTests.Run();
                DevV4PanelControlsTests.Run();
                DevV4BiiRetireTests.Run();
                DevV4FeatureToggleSurfaceTests.Run();
                DevV4OfficialCopyTests.Run();
                DevV4ExternalConfigParityTests.Run();
                DevTicket04UpmCategoryListTests.Run();
                Console.WriteLine("DEV-05/DEV-15A/DEV-15B/DEV-15C/DEV-15D/DEV-16B/DEV-V4-01/DEV-V4-02/DEV-V4-03/DEV-V4-04/DEV-V4-05/DEV-V4-07/DEV-V4-08/POST-P4-04 ClientUi tests: PASS");
                return 0;
            }
            catch (Exception error)
            {
                Console.WriteLine("DEV-05/DEV-15A/DEV-15B/DEV-15C/DEV-15D/DEV-16B/DEV-V4-01/DEV-V4-02/DEV-V4-03/DEV-V4-04/DEV-V4-05/DEV-V4-07/DEV-V4-08 ClientUi tests: FAIL");
                Console.WriteLine(error.GetType().FullName);
                Console.WriteLine(error.Message);
                return 1;
            }
        }

        private static void Run()
        {
            var root = new TestRoot();
            var good = new RecordingComponent("good");
            var failing = new RecordingComponent("failing") { ThrowOnInitialize = true };
            var failingOpen = new RecordingComponent("failing-open") { ThrowOnOpened = true };
            var failingClose = new RecordingComponent("failing-close") { ThrowOnClosed = true };
            var failingDestroy = new RecordingComponent("failing-destroy") { ThrowOnDestroyed = true };
            var registry = new GeneratedClientUiRegistry(new[]
            {
                new ClientUiRegistration(new FeatureId("io.example.good"), () => good),
                new ClientUiRegistration(new FeatureId("io.example.failing"), () => failing),
                new ClientUiRegistration(new FeatureId("io.example.failing-open"), () => failingOpen),
                new ClientUiRegistration(new FeatureId("io.example.failing-close"), () => failingClose),
                new ClientUiRegistration(new FeatureId("io.example.failing-destroy"), () => failingDestroy)
            });
            var composition = new ClientUiCompositionRoot(registry);

            Assert(!composition.Initialize(new ClientUiEnvironment(true, true, false), root), "batch mode blocks composition");
            Assert(good.InitializeCount == 0, "blocked gate does not invoke factories");
            var unavailable = new ClientUiCompositionRoot(registry);
            Assert(!unavailable.Initialize(new ClientUiEnvironment(false, false, false), root), "unavailable client blocks composition");
            var headless = new ClientUiCompositionRoot(registry);
            Assert(!headless.Initialize(new ClientUiEnvironment(true, false, true), root), "headless client blocks composition");
            Assert(composition.Initialize(new ClientUiEnvironment(true, false, false), root), "client gate composes");
            Assert(good.InitializeCount == 1, "registered component initialized once");
            Assert(failing.DestroyedCount == 1, "initialization failure triggers cleanup callback");
            Assert(composition.IsolatedFeatureIds.Count == 1 && composition.IsolatedFeatureIds[0].Value == "io.example.failing", "one component failure is isolated");

            composition.OpenInventory(new TestInventorySurface());
            composition.CloseInventory();
            composition.Destroy();
            Assert(good.InventoryOpenedCount == 1 && good.InventoryClosedCount == 1, "inventory lifecycle forwarded");
            Assert(good.DestroyedCount == 1, "destroy callback forwarded");
            Assert(failingOpen.DestroyedCount == 1 && failingClose.DestroyedCount == 1, "lifecycle failures trigger cleanup callbacks");
            Assert(failingDestroy.DestroyedCount == 1, "destroy failure invokes cleanup exactly once");

            var evaluator = new PlacementCandidateEvaluator();
            var presenter = new InventoryDragPresenter(evaluator);
            presenter.BeginDrag(7);
            var input = new PlacementCandidateInput(7, new ItemGridPosition(0, 0, 0, 0), new ContainerReference(ContainerKind.PlayerInventory, 0, 1), 1.2f, 1.2f, 1, 1, 0, true, new EmptyGrid(4, 4));
            Assert(presenter.Evaluate(input).State == PlacementPreviewState.Candidate, "current generation delegates to evaluator");
            var stale = new PlacementCandidateInput(6, input.Source, input.TargetContainer, input.CursorGridX, input.CursorGridY, input.ItemWidth, input.ItemHeight, input.CurrentRotation, input.AllowAutomaticRotation, input.Occupancy);
            Assert(presenter.Evaluate(stale).Reason == PlacementReason.StaleDrag, "stale generation is rejected");
            presenter.EndDrag();
            Assert(presenter.Evaluate(input).Reason == PlacementReason.StaleDrag, "ended drag rejects callbacks");

            var snapshot = new FeatureSettingsSnapshot(new FeatureId("io.example.good"), 1, SettingRevisionScope.ClientPreference, 4, SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent,
                new[] { new SettingEntryView("visible", SettingAuthority.ClientLocal, new SettingValueOption(false, default(SettingValue)), false, default(SettingPolicyView), SettingValue.Toggle(true), true, true),
                        new SettingEntryView("hidden", SettingAuthority.ClientLocal, new SettingValueOption(false, default(SettingValue)), false, default(SettingPolicyView), SettingValue.Toggle(false), false, true) });
            var settingsPresenter = new SettingsSnapshotPresenter();
            var rows = settingsPresenter.GetVisibleEntries(snapshot);
            Assert(rows.Count == 1 && rows[0].SettingId == "visible", "settings presenter consumes visible snapshot rows only");

            RunNativeDragAdapterTests();
        }

        private static void RunNativeDragAdapterTests()
        {
            var adapter = new NativeInventoryInteractionAdapter(2, 8);
            var native = new RecordingNativeDragActions();
            var source = new ItemGridPosition(7, 1, 2, 0);
            var target = new ItemGridPosition(7, 3, 4, 1);
            var candidate = new ItemPlacementPreview(12, PlacementPreviewState.Candidate, target, 2, 3, PlacementReason.None);
            var input = new NativeDragAdapterInput(true, 12, source, candidate);

            Assert(adapter.HandleRelease(input, native) == NativeDragAdapterOutcome.Submitted, "ordinary grid candidate submits through native port");
            Assert(native.StopCount == 1 && native.SendCount == 1, "ordinary candidate submits and then clears the native drag");
            Assert(native.OperationOrder == "send>stop", "ordinary candidate sends before stopping the native drag");
            Assert(native.LastSource.Page == 7 && native.LastTarget.Page == 7 && native.LastTarget.Rotation == 1, "native submission preserves source and candidate coordinates");

            native.Reset();
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 12, source, candidate, 3), native)
                == NativeDragAdapterOutcome.PassThrough,
                "callback page mismatch passes through instead of submitting a candidate from another page");
            Assert(native.StopCount == 0 && native.SendCount == 0,
                "callback page mismatch never invokes enhanced native actions");

            native.Reset();
            var groundSource = new ItemGridPosition(8, 0, 0, 0);
            var groundInput = new NativeDragAdapterInput(true, 13, groundSource,
                new ItemPlacementPreview(13, PlacementPreviewState.Candidate, new ItemGridPosition(7, 0, 0, 0), 1, 1, PlacementReason.None));
            Assert(adapter.HandleRelease(groundInput, native) == NativeDragAdapterOutcome.Submitted,
                "DEV-16F: AREA source targeting a grid submits through the ground port");
            Assert(native.GroundTakeCount == 1 && native.SendCount == 0 && native.StopCount == 1,
                "DEV-16F: AREA source commits via TakeGroundItem, never sendDragItem");

            native.Reset();
            var equipmentTarget = new ItemPlacementPreview(14, PlacementPreviewState.Candidate, new ItemGridPosition(1, 0, 0, 0), 1, 1, PlacementReason.None);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 14, source, equipmentTarget), native) == NativeDragAdapterOutcome.PassThrough, "equipment target is native pass-through");
            Assert(native.StopCount == 0 && native.SendCount == 0, "special target never invokes enhanced native port");

            native.Reset();
            var areaTarget = new ItemPlacementPreview(15, PlacementPreviewState.Candidate, new ItemGridPosition(8, 0, 0, 0), 1, 1, PlacementReason.None);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 15, source, areaTarget), native) == NativeDragAdapterOutcome.PassThrough, "area target is native pass-through");
            Assert(native.StopCount == 0 && native.SendCount == 0, "area target never invokes enhanced native port");

            native.Reset();
            var staleSpecialTarget = new ItemPlacementPreview(20, PlacementPreviewState.Candidate, new ItemGridPosition(1, 0, 0, 0), 1, 1, PlacementReason.None);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 21, source, staleSpecialTarget), native) == NativeDragAdapterOutcome.PassThrough, "stale special target passes through natively");
            Assert(native.StopCount == 0 && native.SendCount == 0, "stale special target never reaches native pass-through");

            native.Reset();
            var invalid = new ItemPlacementPreview(16, PlacementPreviewState.LocallyInvalid, new ItemGridPosition(7, 3, 4, 1), 2, 3, PlacementReason.Occupied);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 16, source, invalid), native) == NativeDragAdapterOutcome.Cancelled, "invalid ordinary candidate cancels enhanced drag");
            Assert(native.StopCount == 0 && native.SendCount == 0, "invalid ordinary candidate leaves native drag live for swap/pass-through decision");

            native.Reset();
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(false, 17, source, candidate), native) == NativeDragAdapterOutcome.PassThrough, "ended drag is native pass-through");
            Assert(native.StopCount == 0 && native.SendCount == 0, "ended drag does not invoke enhanced native port");

            native.Reset();
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 19, source, candidate), native) == NativeDragAdapterOutcome.Cancelled, "stale ordinary candidate fails closed");
            Assert(native.StopCount == 0 && native.SendCount == 0, "stale ordinary candidate is dropped without native action");

            native.Reset();
            var same = new ItemPlacementPreview(18, PlacementPreviewState.Candidate, source, 1, 1, PlacementReason.None);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 18, source, same), native) == NativeDragAdapterOutcome.PassThrough, "same placement is native cancellation path");
            Assert(native.StopCount == 0 && native.SendCount == 0, "same placement does not submit a duplicate native request");

            native.Reset();
            var ordinarySource = new ItemGridPosition(7, 3, 4, 0);
            var sameInvalid = new ItemPlacementPreview(22, PlacementPreviewState.LocallyInvalid, new ItemGridPosition(7, 3, 4, 0), 1, 1, PlacementReason.Occupied);
            Assert(adapter.HandleRelease(new NativeDragAdapterInput(true, 22, ordinarySource, sameInvalid), native) == NativeDragAdapterOutcome.Cancelled, "same placement with invalid preview cancels enhanced drag");
            Assert(native.StopCount == 0 && native.SendCount == 0, "same placement invalid preview keeps native drag live for vanilla swap handling");

        }

        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        internal sealed class TestRoot : IClientUiRoot { }
        internal sealed class TestInventorySurface : IClientUiInventorySurface { }

        internal sealed class RecordingComponent : IClientUiFeatureComponent
        {
            private readonly string name;
            public RecordingComponent(string name) { this.name = name; }
            public bool ThrowOnInitialize { get; set; }
            public bool ThrowOnOpened { get; set; }
            public bool ThrowOnClosed { get; set; }
            public bool ThrowOnDestroyed { get; set; }
            public int InitializeCount { get; private set; }
            public int InventoryOpenedCount { get; private set; }
            public int InventoryClosedCount { get; private set; }
            public int DestroyedCount { get; private set; }
            public void OnUiInitialized(IClientUiRoot root) { InitializeCount++; if (ThrowOnInitialize) throw new InvalidOperationException(name); }
            public void OnInventoryOpened(IClientUiInventorySurface inventory) { InventoryOpenedCount++; if (ThrowOnOpened) throw new InvalidOperationException(name); }
            public void OnInventoryClosed() { InventoryClosedCount++; if (ThrowOnClosed) throw new InvalidOperationException(name); }
            public void OnUiDestroyed() { DestroyedCount++; if (ThrowOnDestroyed) throw new InvalidOperationException(name); }
        }

        internal sealed class EmptyGrid : IGridOccupancyView
        {
            public EmptyGrid(byte width, byte height) { Width = width; Height = height; }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y) { return false; }
        }

        internal sealed class RecordingNativeDragActions : INativeInventoryDragActions
        {
            public int StopCount { get; private set; }
            public int SendCount { get; private set; }
            public int GroundTakeCount { get; private set; }
            public ItemGridPosition LastSource { get; private set; }
            public ItemGridPosition LastTarget { get; private set; }
            public string OperationOrder { get; private set; }

            public void StopDrag() { StopCount++; OperationOrder += "stop"; if (SendCount > 0) OperationOrder = "send>stop"; }

            public void SendDragItem(ItemGridPosition source, ItemGridPosition target)
            {
                SendCount++;
                LastSource = source;
                LastTarget = target;
                OperationOrder += "send";
            }

            public void TakeGroundItem(ItemGridPosition target)
            {
                GroundTakeCount++;
                LastTarget = target;
                OperationOrder += "ground";
            }

            public void Reset()
            {
                StopCount = 0;
                SendCount = 0;
                GroundTakeCount = 0;
                LastSource = default(ItemGridPosition);
                LastTarget = default(ItemGridPosition);
                OperationOrder = string.Empty;
            }
        }
    }
}
