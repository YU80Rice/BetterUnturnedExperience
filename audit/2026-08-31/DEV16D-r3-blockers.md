# DEV-16D R3 审查阻断记录

日期：2026-08-31
冻结基准：`79a7872...578db84`
状态：修复中（不得交付）

## Standards 轴阻断

1. 组件级隔离没有撤销 `InventoryDragPreviewAdapter` 的原生 delegate、库存事件和 Harmony 资源。
2. 清理异常没有稳定传播为 `CleanupIncomplete`，且缺少带 FeatureId/ErrorCode/DiagnosticId 的结构化诊断。
3. viewport/scroll 读取异常仍被 `0f`、缓存 viewport 或 `null` 静默降级，未统一进入功能隔离。
4. R3 缺少可追踪的全套测试运行日志。

## Spec 轴阻断

`GridPlacedItemWrapper` 的异常路径传入 `DetachAndDeactivate`，未将适配器置为隔离态；`AttachGrid` 也没有隔离守卫，因此隔离后新 surface 派发仍可能重新挂载 `onPlacedItem`。

## 修复边界

- 仅修改 DEV-16D 隔离、清理结果传播、native hierarchy/viewport/scroll 异常分类及其回归测试。
- 保留单 DLL、Headless 隔离、原生回退和现有 ABI。
- 修复后必须重新执行红测→绿测→Release/全套测试/静态门禁→全新 Standards+Spec 审查。
