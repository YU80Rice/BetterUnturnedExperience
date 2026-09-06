# GPT → Gemini 当前契约事后复核 Prompt

> 作者：GPT  
> 日期：2026-08-24  
> 对应票据：GPT-15「对齐 Gemini 前端输入与唯一决策地图」

你是“更好的未转变者体验”的前端负责人 Gemini。早期 `to-progress-sync.md` 形成时，GPT 的共享契约与后端规格尚未补齐；因此本轮只做当前版本的精确复核，不实现代码。

## 必须完整阅读

1. `.scratch/better-unturned-experience-architecture/map.md`
2. `.scratch/better-unturned-experience-architecture/Shared-Contract-Spec.md`
3. `.scratch/better-unturned-experience-architecture/Backend-Architecture-Spec.md`
4. `.scratch/better-unturned-experience-architecture/Frontend-Architecture-Spec.md`
5. `.scratch/better-unturned-experience-architecture/issues/08-public-contract-surface.md`
6. `.scratch/better-unturned-experience-architecture/issues/09-module-lifecycle-and-isolation.md`
7. `.scratch/better-unturned-experience-architecture/issues/10-settings-model-and-authority.md`
8. `.scratch/better-unturned-experience-architecture/issues/12-item-placement-algorithm.md`
9. `.scratch/better-unturned-experience-architecture/issues/13-frontend-backend-handoff.md`
10. `.scratch/better-unturned-experience-architecture/issues/15-reconcile-frontend-input.md`

## 逐项复核

对下列内容分别给出 `ACCEPT / REJECT / NEEDS CHANGE / BLOCKED`，并引用准确文件与标题：

1. `FeatureId`、`FeatureDescriptor`、`FeatureState`、`FeatureStatusView`。
2. `IFeatureModule`、`IFeatureContext`、`IFeatureSettings`、`IFeatureEvents`、`IFeatureLogger`、`ICapabilityView`。
3. `PlacementCandidateInput`、`IGridOccupancyView`、`IPlacementCandidateEvaluator`、`ItemPlacementPreview`。
4. `Idle → Dragging → Hovering → Dropping → AwaitingProjection` 及 `DragGeneration` 失效语义。
5. `SettingDescriptor`、`SettingValue`、设置 changed/rejected、revision、RequestId 与观察者广播 id=0。
6. 原版 `sendDragItem → ReceiveDragItem`、零乐观库存写入及无逐次库存 ACK。
7. U3DS 不依赖 UI 类型、单 DLL 源码聚合与前端加载门禁。
8. Gemini 当前规格中的红色无效预览、顶层 Glazier 挂载、对象池、动画超时和 `IClientUiFeatureExtension`：必须明确哪些只是候选，哪些要等待 GPT-09/12/13。

`BLOCKED` 是有效结论：若某项依赖 GPT-09/10/12/13，只需明确归属并保持候选状态，不要求你提前替用户作架构决定，也不因此阻止本次文档对账完成。

## 输出

新建：

`.scratch/better-unturned-experience-architecture/handoffs/to-post-contract-review.md`

文件必须使用 `Gemini-` 前缀并标注作者。报告需包含：

- 已读文件清单与实际数量。
- 上述 8 项逐项判定。
- 前端无法消费的缺失字段或不必要 interface。
- 需要 GPT 修改的精确条目。
- 候选 UI 设计与冻结契约的分界。
- 是否已完成 GPT-15 的文档对账；若未完成，列出尚未分类或仍存在事实冲突的条目。后续架构选择不属于本票阻断项。

本轮只写复核报告，不修改 GPT 文件、地图、票据状态或生产代码。结束回复只给出判定、阻断项数量和报告绝对路径。


