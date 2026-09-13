# POST-P4-07 R1 Spec 轴 brief

固定点：HEAD `85f8e6a`。冻结 diff：`audit/2026-09-13/POST-P4-07/review-freeze-r1.txt`。

## 规格来源（权威=票 07 全文）

路径：`.scratch/bue-post-phase4-closure/issues/07-refresh-model-dedup.md`

What to build：保存成功留页、确认离开后保存、刷新/重挂三条路径里，对「先重建目录条目再渲染」的复制收成一处。行为与 F2（保存后开关不回跳）保持一致。

当前行为：F2 在保存钮 / 确认留页 / 确认离开三条路径先重建条目再渲染，避免目录缓存的旧生命周期。Standards 标 deferrable：三处复制。产品正确。

期望行为：
- 单一「提交后刷新目录再画当前详情」入口，三条路径调用它。
- 回归：保存启停后开关与功能状态显示权威值，不回跳草稿前。
- 不改变确认三选一、草稿范围、跨源部分成功。

关键接口：保存/确认导航后的目录重建；DraftSaveReport 已提交的生命周期意图仍用于刷新后投影。

验收：F2 回归组仍绿；三路径不再各写一份刷新序列（审查能指出单一入口）；全套测试绿；双轴 CLEAN；不授候选。

不在范围：插件草稿填充另两段同构（未升格）；改草稿语义。

## 实现者对「第三条路径」的读法（请核）

第三条=脏刷新走确认离开（`wasRefresh`），不是 Open / 干净 RequestRefresh。后者本来就是无条件 `refreshModel(); Render();`，不是 F2 生命周期门。Site C 原本就把 `wasRefresh || CommittedLifecycleIntent` 写在确认离开腿。关面板离开：先刷目录再 Close，不画一帧（`AfterCommitPaint.None`）。

红测缝=新 eng 门禁 `Verify-RefreshModelDeduped.ps1`（宿主不能构造 Glazier 面板；F2 行为仍由 ClientUi `SaveCommittedLifecycleIntentFlag` 钉）。红 7 违例→绿；M1 保存腿回内联 / M2 改 helper 名 / M3 丢掉 forceRefresh 门 / M4 改 F2 测试签名 各 exit=1。

## 任务

报告：(a) 规格要求但缺失或不完整的需求；(b) diff 中存在但规格未要求的行为（范围蔓延）；(c) 看起来已实现但实现可能错误的需求。每一项都引用规格原文。400 字以内。不要派子代理。只审冻结 diff 与票面。无问题则明确 CLEAN 并给依据。
