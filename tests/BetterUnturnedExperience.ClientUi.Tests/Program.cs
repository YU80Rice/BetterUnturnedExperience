using System;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Tests
{
    /// <summary>
    /// DEV-V6-04：UI 套件只测界面工程（spec 166 行）。BII 组件/适配器语义测试
    /// （Dev15B/C/D/16B 与 BII 退役项、原生拖拽端口）随类型迁入插件套件（IVT
    /// 通道），本套件保留：组合状态机门禁、拖拽 presenter 代数语义、设置快照
    /// presenter 可见行投影、BII 设置态（界面单事实源留驻项）。
    /// 作废票：04（组合编排机制——registry/组件循环/Open/Close/IsolatedFeatureIds
    /// /FactoryInvocationCount——随唯一消费者迁 Bii 退役，机制本体已删，旧断言面
    /// 不再存在）。
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                Run();
                DevV4DraftTests.Run();
                DevV4PanelControlsTests.Run();
                DevV4FeatureToggleSurfaceTests.Run();
                DevV4OfficialCopyTests.Run();
                DevV4ExternalConfigParityTests.Run();
                DevTicket04UpmCategoryListTests.Run();
                Console.WriteLine("DEV-05/DEV-15A/DEV-V4-01/DEV-V4-02/DEV-V4-03/DEV-V4-05/DEV-V4-07/DEV-V4-08/POST-P4-04 ClientUi tests: PASS");
                return 0;
            }
            catch (Exception error)
            {
                Console.WriteLine("DEV-05/DEV-15A/DEV-V4-01/DEV-V4-02/DEV-V4-03/DEV-V4-05/DEV-V4-07/DEV-V4-08 ClientUi tests: FAIL");
                Console.WriteLine(error.GetType().FullName);
                Console.WriteLine(error.Message);
                return 1;
            }
        }

        private static void Run()
        {
            // DEV-V6-04：组合状态机门禁（组件编排成员退役后本面=环境门禁+safe mode+
            // 销毁序；作废票 04）。
            var blockBatch = new ClientUiCompositionRoot();
            Assert(!blockBatch.Initialize(new ClientUiEnvironment(true, true, false)), "batch mode blocks composition");
            var blockUnavailable = new ClientUiCompositionRoot();
            Assert(!blockUnavailable.Initialize(new ClientUiEnvironment(false, false, false)), "unavailable client blocks composition");
            var blockHeadless = new ClientUiCompositionRoot();
            Assert(!blockHeadless.Initialize(new ClientUiEnvironment(true, false, true)), "headless client blocks composition");
            Assert(blockHeadless.State == ClientUiCompositionState.Unavailable, "blocked gate leaves the composition unavailable");

            var composition = new ClientUiCompositionRoot();
            Assert(composition.Initialize(new ClientUiEnvironment(true, false, false)), "client gate composes");
            Assert(composition.State == ClientUiCompositionState.Ready, "composed gate reaches ready");
            Assert(composition.Initialize(new ClientUiEnvironment(true, false, false)), "repeated initialization is idempotent");
            composition.Destroy();
            Assert(composition.State == ClientUiCompositionState.Destroyed, "destroy moves to the destroyed state");
            Assert(!composition.Initialize(new ClientUiEnvironment(true, false, false)), "destroyed composition is not reinitialized");

            var safeMode = new ClientUiCompositionRoot();
            Assert(safeMode.Initialize(new ClientUiEnvironment(true, false, false)), "safe-mode fixture composes");
            safeMode.EnterSafeMode(line => { });
            Assert(safeMode.IsSafeMode && safeMode.State == ClientUiCompositionState.Unavailable,
                "safe mode leaves the composition unavailable");
            Assert(!safeMode.Initialize(new ClientUiEnvironment(true, false, false)), "safe mode blocks re-composition");

            // DEV-V6-04（作废票：04）：拖拽 presenter 代数语义测试随类型迁入插件套件
            // （InventoryDragPresenter 住 Bii 工程，UI 工程零引用；Dev15ANativeDragAdapterTests）。

            var snapshot = new FeatureSettingsSnapshot(new FeatureId("io.example.good"), 1, SettingRevisionScope.ClientPreference, 4, SettingSyncState.Ready, SettingSnapshotSource.LocalPersistent,
                new[] { new SettingEntryView("visible", SettingAuthority.ClientLocal, new SettingValueOption(false, default(SettingValue)), false, default(SettingPolicyView), SettingValue.Toggle(true), true, true),
                        new SettingEntryView("hidden", SettingAuthority.ClientLocal, new SettingValueOption(false, default(SettingValue)), false, default(SettingPolicyView), SettingValue.Toggle(false), false, true) });
            var settingsPresenter = new SettingsSnapshotPresenter();
            var rows = settingsPresenter.GetVisibleEntries(snapshot);
            Assert(rows.Count == 1 && rows[0].SettingId == "visible", "settings presenter consumes visible snapshot rows only");
        }

        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        internal sealed class EmptyGrid : IGridOccupancyView
        {
            public EmptyGrid(byte width, byte height) { Width = width; Height = height; }
            public byte Width { get; }
            public byte Height { get; }
            public bool IsOccupied(byte x, byte y) { return false; }
        }
    }
}
