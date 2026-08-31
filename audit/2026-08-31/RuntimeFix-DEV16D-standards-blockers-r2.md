# DEV-16D 审查阻断记录 R2

## 接管与冻结

本轮以提交 `21852c8511a161b0d92a78f00eb7522a4519051f` 为冻结基准。该基准已完成上一轮红测→绿测、Release 编译、全套测试和静态门禁，但新的 Standards 独立审查仍返回 `FAIL`。本轮代码由 GPT 接手；历史 ClientUi 代码原由 Gemini 负责。

## 新增 Standards 阻断

1. **原生回调边界未完全隔离**

   - 涉及：`src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs` 的 `GridPlacedItemWrapper`、原生 `nativePlacedHandler` 调用，以及 `InventorySurfaceLifecycleAdapter.PlayerUIUpdatePostfix` 对动态 viewport/scroll 读取的异常边界。
   - 根因：U3-SDK 的 `SleekItems.onPlacedItem` 直接调用委托；当前包装器在 `EvaluatePlacement` 或原生 handler 抛异常时可能越过 BUE 的功能级隔离边界。动态几何 getter 失败时外层仅记录 `LastPollDiagnostics`，没有保证组件进入隔离并清理预览。
   - 预期修复：新增明确的 placed-item guarded boundary；动态 surface poll 的异常统一走组件隔离/清理 seam，原生回退保持可用。

2. **顶层图标 fallback 可能混用坐标域**

   - 涉及：`src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs` 的 `InventoryPreviewPresenter.Update` 与 `TryGetIconScreenPosition` fallback。
   - 根因：`TryGetNativeIconPlacement` 失败时，grid-local `PointerScreenX/Y` 被包装为顶层图标坐标。顶层锚点不可用时继续绘制会产生错位，违反“不能跨坐标域 fallback”的 fail-closed 约束。
   - 预期修复：显式区分顶层锚点与 grid-local 坐标；无合法顶层转换时只隐藏图标，不把 grid-local 数值当作 screen/top-level 坐标。

## 行动门禁

- 先增加两个可观察 seam 的红色回归测试并实际确认失败。
- 仅在红测失败后解冻生产代码并实施最小修复。
- 修复后重新执行 Release 编译、7/7 测试、静态门禁和 `git diff --check`。
- 以新的冻结基准重新派发全新的 Standards 与 Spec 独立审查；两轴 `CLEAN` 前不生成正式 CandidateBuild/CaseId，不要求实机测试。
