# DEV-16D R9 Spec 阻断记录（GPT）

## 审查结论

R9 Spec 轴返回 `FAIL`，当前冻结基准为 `5e5d12c`（生产实现 `74fc1ec`）。本轮只处理以下两个阻断，不扩展功能范围：

1. `InventoryDragPreviewAdapter.IsolateAndDetach(bool)` 在调用组件隔离前没有锁存 `detachSucceeded`。组件已登记的反向清理可能重入 `IsolateAndDetach(false)`，读取默认 `cleanupSucceeded=true`，导致 detach 失败被吞掉，生命周期错误发布普通 `Isolated`。
2. `GridPlacedItemWrapper` 仍把 `component.IsolatePreviewFailure`（`void`）传给 `FailClosedPreview(Action, ...)`，该包装恒返回成功，placed-item 异常路径不能传播清理失败结果。

## 修复门禁

- 先在 adapter/lifecycle seam 增加 R9 红色回归测试并确认失败。
- 仅修复 cleanup 结果锁存与 `Func<bool>` placed-item 隔离边界。
- 重新执行绿测、Release 全量构建、7 项测试、静态门禁和 `git diff --check`。
- 以新冻结基准派发全新的 Standards 与 Spec 审查；双轴 `CLEAN` 前不归档 DLL、不生成 CandidateBuild/CaseId、不要求实机测试。

代码曾由 Gemini 负责，现由 GPT 接手。
