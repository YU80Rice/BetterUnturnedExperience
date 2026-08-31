# DEV-16D 审查阻断记录 R3

## 冻结基准

- 上一轮修复提交：`79a787285d7a5c82981062465a863c7302d63ced`
- Spec 轴：`CLEAN`
- Standards 轴：`FAIL`
- 本轮由 GPT 接手；历史 ClientUi 代码原由 Gemini 负责。

## 阻断项

1. **原生回调资源未完整撤销**

   `GridPlacedItemWrapper` 异常后只隐藏/隔离，没有保证恢复 `SleekItems.onPlacedItem`、清除 `ActiveAdapter`、撤销继续接收回调的入口。违反 `Module-Lifecycle-Isolation-Spec.md §5.1–§5.2`。

2. **清理异常被静默吞掉**

   `FailClosedPreview` 对隔离与清理回调空捕获，导致 cleanup incomplete 没有结构化诊断。违反 `Module-Lifecycle-Isolation-Spec.md §5.2、§6`。

3. **动态 viewport/scroll 异常绕过隔离**

   `ReadScrollPixelsY` 与 `BuildSurfaceContext` 的 native 反射/几何异常被转换为 `0f`、`Vector2.zero` 或 `null`，外层无法区分异常与正常缺失，可能继续消费错误几何。违反功能级 fail-closed 边界。

## 本轮门禁

- 先增加“异常后 detach/deactivate”“cleanup incomplete 诊断”“surface poll 异常不降零”的红测并确认失败。
- 红测失败后才解冻生产代码；修复后重复 Release/7 项测试/静态门禁和双轴审查。

## R3 红测结果（GPT 水印）

已在未继续修改生产代码前加入回归断言。Plugin 测试项目编译退出码 `1`，准确暴露缺失的隔离闭环：

- `InvokePlacedItemGuarded` 尚无 detach 参数边界；
- `FailClosedPreview` 尚未返回 cleanup 完整性；
- `LastCleanupDiagnostics` 尚不存在。

命令：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' 'tests\BetterUnturnedExperience.Plugin.Tests\BetterUnturnedExperience.Plugin.Tests.csproj' /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`CS1501`、`CS0815`、`CS0117` 编译失败；红测有效，随后解冻实施最小修复。

## 修复与绿测

- `InventoryDragPreviewAdapter.IsolateAndDetach` 撤销 `SleekItems.onPlacedItem`、库存事件订阅、Harmony patch 和 `ActiveAdapter`；隔离后 `Tick` 不再进入增强路径。
- `FailClosedPreview` 现在返回清理完整性并记录 `LastCleanupDiagnostics`，不再静默吞掉异常。
- `ReadScrollPixelsY` 与 `BuildSurfaceContext` 的原生反射/几何异常冒泡到 `InvokePollGuarded`，由统一边界隔离、清理并重置派发代际。

验证结果：

- Release 全解决方案编译：`0 errors / 0 warnings`（`audit/2026-08-31/build-dev16d-r3.log`）。
- 7/7 测试程序：全部 `PASS`，总退出码 `0`。
- `Verify-NoUiTokens.ps1`：Contracts/Core/ClientUi 全部 `PASS`（`audit/2026-08-31/static-gates-dev16d-r3.log`）。
- `git diff --check`：`PASS`。
