# DEV-16D 审查阻断记录 R1

## 接管与冻结

本轮沿用提交 `fc3768d` 作为审查基准（R44 基线 `1d7749f`）。代码曾由 Gemini 负责 ClientUi，现由 GPT 接手。Standards 与 Spec 两轴审查均已返回完整结论；当前基准冻结，未依据结论修改生产代码。

## Standards 轴阻断

1. **动态滚动裁剪失效**：`UnturnedInventorySurfaceContext` 的 `Viewport` 在开面时固化，`ScrollPixelsY` 却每帧读取当前值；滚动后 pointer 与 clip 不在同一时间快照，合法位置可能变成 `Hidden`。涉及 `src/BetterUnturnedExperience.Plugin/InventorySurfaceLifecycleAdapter.cs` 的上下文字段/属性与 `src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs` 的裁剪消费。
2. **异常隔离未闭环**：`InventoryDragPreviewAdapter` 的原生层级/反射读取异常仅记录 `LastPollDiagnostics`，未统一调用组件隔离、清理预览并停止增强回调，可能重复异常或留下释放路径。涉及 `InventoryDragPreviewAdapter.cs` 的 `Poll`、`EnsureInventoryEventSubscription`、网格接线。
3. **TDD 门禁未闭合**：`--dev16d-r44-red` 仅由可选参数触发，默认插件测试没有直接执行该回归；R1 记录缺少本轮完整 red→green 与默认套件证据。

## Spec 轴阻断

1. **地面物品来源未实现**：工单要求地面物品拖入普通网格；当前 `AREA` 来源仍由原生 `sendDragItem` 路径接收，未覆盖原生 `ReceiveDragItem` 对 `AREA` 的特殊处理。
2. **交换 Pass-Through 时序风险**：增强释放先结束拖拽，再把交换交回原生；原生 `onPlacedItem` 要求 `isDragging`，可能导致交换无操作。
3. **浮动图标坐标/刷新风险**：预览输入使用 grid-local 坐标，图标挂在 `PlayerUI.container`，但没有明确的父级坐标转换；`ShowIcon` 每帧可能触发异步图标刷新，导致旧回调失效或刷新抖动。
4. **滚动/视口坐标域风险**：`ResolveViewport` 与每帧 pointer/scroll 的组合存在重复应用或固化 clip 的可能，未完全满足规格的真实 viewport/scroll 同步要求。

## 本轮行动门禁

- 先写并运行红色回归测试，确认上述最小症状可失败。
- 再解冻并进行最小修复；保持单 DLL、Headless 隔离、原生回退和现有 ABI。
- 重新执行 Release 构建、默认全套测试、静态门禁和 `git diff --check`。
- 以新的冻结提交派发全新的 Standards + Spec 双轴审查；两轴均 `CLEAN` 前不生成 CandidateBuild、CaseId、正式 DLL，也不要求实机复测。
