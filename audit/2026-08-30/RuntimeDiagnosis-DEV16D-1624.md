# DEV-16D 实机症状诊断与红色回归测试

日期：2026-08-30
范围：只读分析十次 UMM 诊断包、当前源码与 r36 DLL；未修改生产代码。

## 结论

共同失败边界位于“原生拖拽轮询已检测到开始，但预览输入没有进入 Presenter/Sink”。最后一次日志（`UMM-诊断包_20260830_162411`）证明：`hooks-installed`、`drag-started`、`enhanced=True`、`canRun=True`、`sinkBound=True` 均成立；随后直接出现 `placement-passthrough reason=preview-stale previewGen=0 dragGen=1..8`，没有 `preview-visible`、帧颜色或真实图标刷新事件。

这不是功能开关关闭、Satellite 缺失或单纯 UI Sink 颜色错误。当前证据优先指向 `updateDraggedItem` Postfix 的轮询/输入构造链：`Poll()` 没有成功完成 `TryCreatePreviewInput → OnDragUpdated → InventoryPreviewPresenter.Update`，或输入被某个门禁静默丢弃。

## 十次部署身份

| 诊断包 | 日志 DLL SHA-256 | 关键读数 |
|---|---|---|
| 110256 | 354C8A2E8AA356CCA397A8BF260C0C18073BA6DF762C039CCEDFD048F637EC5D | 管理面板链路有命中 |
| 114508 | 9CE92796C6571DD9B309A1D5C13570280E95869B04692CB73132EC4AF1E2A969 | 管理面板链路有命中 |
| 125228 | B7A0100F6952B2E96D5FDDD8420E4BFCA9F3EDC078E0E92D08A301C4693D0104 | `hooks-failed`，Harmony IL Compile Error |
| 140936 | D57423E4611EB1F40596459E6055EA8A8EC7FADE00D4C432518CD85AF87D696E | `hooks-failed`，拖拽未开始 |
| 142729 | D57423E4611EB1F40596459E6055EA8A8EC7FADE00D4C432518CD85AF87D696E | `hooks-failed` |
| 144940 | EEEB4E42C7E001C1E559C4D487487304C4122BFF5597B11429648B17862A035D | hooks installed，但未形成预览证据 |
| 150551 | 2B4098A67F02D294B16CBEEE39DD34BDE63BCC940AC410D11D147C132FE0C604 | `drag-started` 37 次，无可见预览证据 |
| 153746 | 3D24BB55040649626F74AEE1C9E98A0EB10B036A5993939A38326B299BEF5CE5 | `drag-started` 11 次 |
| 154625 | 406D76DB829DE0DB54C9AE38B1B301DF34B32355DAF7569B656FAF3CDADBF777 | `drag-started` 11 次 |
| 162411 | 31FD99A086E5A7A556EFAEBBD132A48BC610FAD0C6A59848B8291B31A3E84143 | `drag-started` 8 次；全部 `previewGen=0` 旁路 |

最后部署文件 `artifacts/DEV-16D-drag-preview-r36-20260830/BetterUnturnedExperience.dll` 的 SHA-256 与日志最后一次一致：`31FD99A086E5A7A556EFAEBBD132A48BC610FAD0C6A59848B8291B31A3E84143`。仓库源码 HEAD 已是 R37，因此“最后部署目录名 r36”与当前源码提交并非同一标签，后续修复必须重新产生新身份。

## 对照结论

- UPM 的成功路径是自身 `BaseUnityPlugin.Update()` 每帧直接调用 `PluginManagerUI.Tick()`，Harmony 仅快速路径；每帧读取当前原生容器并 `CreateButton → AddChild`。
- BUE 的 `InventoryDragPreviewAdapter` 依赖 `PlayerDashboardInventoryUI.updateDraggedItem` Postfix；`Poll()` 才会调用 `TryCreatePreviewInput` 和 `OnDragUpdated`。日志能证明 drag edge，但不能证明 Postfix 每帧完成了预览更新。
- `BetterItemInteractionUiComponent.OnDragUpdated` 在 `!isInventoryOpen`、`previewSink == null`、增强开关关闭、生命周期不可运行、会话/容器不匹配时直接隐藏；当前日志未记录这些分支，因此现有诊断粒度不足。
- 纯 C# Presenter/Sink 测试已有绿色帧、红色帧和资产绑定断言；缺口是从真实 `drag-started` 到 `OnDragUpdated` 的运行链证据。

## 排名假设（可证伪）

1. **H1：Postfix 没有持续命中。** 若将每次 `DashboardUpdatePostfix/Poll` 计数写入诊断，计数应停留在 0 或只出现一次；修复可靠 Update 驱动后预览事件出现。
2. **H2：`TryCreatePreviewInput` 返回 false。** 若记录返回值及 `isInventoryOpen/currentSurface`，应显示输入未创建；修复 surface 生命周期绑定后变绿。
3. **H3：输入创建后被 `OnDragUpdated` 门禁隐藏。** 若记录 gate 原因，应命中 stale session/container 或 lifecycle 分支；修复对应上下文绑定后变绿。
4. **H4：Presenter 产生 Hidden/Invalid，但 Sink 未刷新真实图元。** 若记录 `LastPreview.State` 和 Sink 调用，应能看到状态；此时问题在坐标/视口或真实图标刷新，而非驱动。

## 红色回归测试

新增夹具：`tests/BetterUnturnedExperience.Plugin.Tests/Fixtures/dev16d-r36-no-preview.log`

新增断言：`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs::AssertDev16DRealLogMustContainVisiblePreviewProjection`

运行命令：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' 'tests\BetterUnturnedExperience.Plugin.Tests\BetterUnturnedExperience.Plugin.Tests.csproj' /t:Build /p:Configuration=Release /v:minimal
& 'tests\BetterUnturnedExperience.Plugin.Tests\bin\Release\BetterUnturnedExperience.Plugin.Tests.exe'
```

结果：构建成功；测试按预期 **FAIL**：`drag start must reach a visible red/green preview projection`。该测试捕获的正是用户症状，而非泛化的“无异常”。

## 当前边界与下一步

本轮没有修改生产代码，也没有宣称 DEV-16D 修复。下一轮应在 H1→H2→H3 顺序补充最小结构化计数/原因诊断，并以本红测为入口实施修复；修复后再按红→绿、全套构建、双轴审查和新的 DLL SHA-256/CaseId 闭环。
