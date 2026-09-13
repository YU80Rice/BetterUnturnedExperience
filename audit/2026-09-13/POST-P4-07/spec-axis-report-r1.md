# POST-P4-07 R1 Spec 轴报告

实例：全新 spawn `Spec-Reviewer`（未续用）。固定点 `85f8e6a`。冻结 diff=`review-freeze-r1.txt`。规格=票 07。

## 裁决：CLEAN

冻结 diff 与票面要求一致，未发现遗漏、范围蔓延或错误实现。

- **单一入口已完成**：规格「单一「提交后刷新目录再画当前详情」入口，三条路径调用它」。`RefreshCatalogThenPaintCurrentDetail`；`CommitDraftAndStatus` 与 `ResolveConfirm` 留页 / 关面板离开 / 换条离开统一调用。
- **F2 状态投影保持**：规格「保存启停后开关与功能状态显示权威值，不回跳草稿前」。helper 以 `forceRefresh || report.CommittedLifecycleIntent` 决定 `refreshModel()`；`RenderDraftReport` 最后执行。
- **确认行为未改变**：规格「不改变确认三选一、草稿范围、跨源部分成功」。diff 仅替换刷新/绘制调用。
- **明确排除项未触碰**：规格「插件草稿填充另两段同构」不在范围；diff 未改。
- 新增 `eng/Verify-RefreshModelDeduped.ps1` 属于验收门禁，验证单一入口、F2 回归锚点及调用点去重，不构成额外产品行为。
