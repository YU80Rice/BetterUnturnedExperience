# DEV-16D R5 Standards 审查阻断记录

审查基准：`03188edc81f931400f3df392c879e09574adf907`
对比基线：`42cdc9c220603c583365cc39c6e88fb9e2d3a045`
状态：修复中（不得交付）

## 阻断

1. `InventorySurfaceLifecycleAdapter` 失败隔离后，插件仍可能继续创建并激活 `InventoryDragPreviewAdapter`，其 Harmony hook 未纳入已完成的 cleanup 登记闭环。
2. native hierarchy 缺失仍以 `null`/重试返回，功能状态可能保持 `Running/Available`，未投影为 `Isolated/PresentationDegraded`。
3. poll guard 以 `Action` 调用 `IsolateAndDetach()`，丢失 `false` 清理结果，`CleanupIncomplete` 不能稳定穿过同一 FeatureId/ErrorCode/DiagnosticId 链。
4. `uiScale <= 0` 仍静默改为 `1`；无效 UI scale 必须拒绝并进入功能级隔离。

## 修复边界

- 只修改 DEV-16D 生命周期激活顺序、native hierarchy 失败投影、清理结果传播和 UI scale 几何门禁及其回归测试。
- 保持单 DLL、Headless 隔离、原生回退与现有 ABI；不修改 U3-SDK、原生游戏源码或 LMN。
- 修复后重新执行红测→绿测→Release/全套测试/静态门禁→全新 Standards→全新 Spec。
