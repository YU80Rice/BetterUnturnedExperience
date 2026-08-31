# DEV-16D R2 红测记录（GPT 水印）

## 基准

- 起点提交：`21852c8511a161b0d92a78f00eb7522a4519051f`
- 目标：锁定 Standards R2 的异常边界与顶层图标坐标域阻断。
- 本阶段仅修改测试，不修改生产代码。

## 红测 1：顶层图标 fallback 坐标域

命令：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' 'tests\BetterUnturnedExperience.ClientUi.Tests\BetterUnturnedExperience.ClientUi.Tests.csproj' /t:Rebuild /p:Configuration=Release /v:minimal
& 'tests\BetterUnturnedExperience.ClientUi.Tests\bin\Release\BetterUnturnedExperience.ClientUi.Tests.exe'
```

结果：编译成功，测试退出码 `1`。

失败断言：

`grid-local pointer never falls back to a top-level icon coordinate`

说明：`TryGetNativeIconPlacement` 在顶层锚点为 `NaN` 时失败，当前 presenter 继续走 `TryGetIconScreenPosition`，把 grid-local 指针显示为顶层图标，因此红测准确复现阻断。

## 红测 2：原生回调/轮询异常边界

命令：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' 'tests\BetterUnturnedExperience.Plugin.Tests\BetterUnturnedExperience.Plugin.Tests.csproj' /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：测试项目编译退出码 `1`，缺失预期 guarded seam：

- `InventoryDragPreviewAdapter.InvokePlacedItemGuarded`
- `InventorySurfaceLifecycleAdapter.InvokePollGuarded`

说明：该编译红灯表示当前生产代码尚未提供可验证的回调隔离边界；测试先于实现锁定了本轮阻断。

## 修复与绿测

生产修复：

- `InventoryDragPreviewAdapter.InvokePlacedItemGuarded` 捕获增强评估和原生 fallback 回调异常；评估异常先隔离/清理再尝试原生回退，原生回调异常隔离并吞掉异常。
- `InventorySurfaceLifecycleAdapter.InvokePollGuarded` 统一包裹 `PlayerUI.Update` 轮询；失败时隔离、清理 surface 并重置派发代际。
- `InventoryPointerCoordinateSpace` 显式标记 `Screen` 与 `GridContentLocal`；grid-local 输入不再调用顶层 screen 图标 fallback。

绿测命令：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' 'tests\BetterUnturnedExperience.ClientUi.Tests\BetterUnturnedExperience.ClientUi.Tests.csproj' /t:Rebuild /p:Configuration=Release /v:minimal
& 'tests\BetterUnturnedExperience.ClientUi.Tests\bin\Release\BetterUnturnedExperience.ClientUi.Tests.exe'
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' 'tests\BetterUnturnedExperience.Plugin.Tests\BetterUnturnedExperience.Plugin.Tests.csproj' /t:Rebuild /p:Configuration=Release /v:minimal
& 'tests\BetterUnturnedExperience.Plugin.Tests\bin\Release\BetterUnturnedExperience.Plugin.Tests.exe'
```

结果：ClientUi 与 Plugin 回归测试均编译成功、退出码 `0`，输出 `PASS`。
