# RT-01：冻结共享契约与对账基线

**Owner:** GPT（总维护者）  
**Required reviewer:** Gemini（前端消费复核）  
**Blocked by:** None (can start immediately)  
**Status:** resolved

## What to build

发布一份可由前端和后端共同执行的共享契约基线，使两个 Agent 能以完全相同的函数名、DTO、消息字段、坐标公式、状态机和证据边界开展 U3-SDK 调研，同时任何一方都不能静默改变公共 seam。

## Acceptance criteria

- [x] 对账并冻结需求规格中的全部共享函数精确签名及其 Required behavior、`IFeatureBootstrap` 属性、Command/Event Kind、方向、必需字段和关联语义。
- [x] 对账并冻结 FeatureState、拖动状态机、超时规则、设置重试规则、坐标域、旋转公式及 Local-Fit Priority 顺序。
- [x] 建立英文 Agent 执行版与中文人工版的契约 token/消息字段对账结果，所有差异均被修复或明确裁定。
- [x] 明确 Contracts 不得引用 UI、Unturned、BepInEx、Harmony、LMN 或 Steamworks 具体类型，并冻结“Contracts 不依赖 adapters；adapters 依赖 Contracts”的单向依赖。
- [x] 冻结运行时模块不得自行声明身份、依赖、设置 schema、事件所有权、能力或入口绑定；这些事实只能来自链接后的单一定义产物。
- [x] 明确 V1 不注册 `CommitItemPlacementCommand`、`RequestRotateItemCommand`、`ItemPlacementCommittedEvent`、`ItemPlacementRejectedEvent`、`InventoryStateUpdatedEvent` 五种库存插件网络消息。
- [x] 输出共享契约变更流程：GPT 维护、Gemini 必审、人工可追溯；研究 Agent 只能提出 change request。
- [x] 为 RT-02～RT-05 发布统一的冻结 U3-SDK 源码身份记录格式和证据分类词汇。
- [x] 交付物仅为契约/对账基线和测试义务，不包含生产功能实现。

## Verification

- [x] Gemini 逐项确认前端可消费性且无阻断异议。
- [x] 独立审计确认英中镜像、Wayfinder 决策和需求规格不存在实质漂移。

## Comments

- 2026-08-24：人工开发者授权 GPT 领取并撰写 RT-01；完成后交由 Gemini 进行前端消费复核。
- 2026-08-24：GPT 完成 `RT-01-Shared-Contract-Baseline.md` 与 Gemini 复核交接；独立审计历经三轮修正后 PASS。当前仅等待 Gemini `ACCEPT`，故状态保持 `claimed`。
- 2026-08-24：Gemini 按十项复核问题返回 `ACCEPT`，阻断项为零，并确认 RT-02/RT-03 发现不足时只提交 Shared Contract Change Request；RT-01 正式 resolved。

## Answer

RT-01 已发布 `BUE-V1-RT01-20260824` 共享契约基线：冻结 14 个 interface 成员及行为、8 个 bootstrap 属性、全部可达 DTO/interface/enum、消息表、状态/超时、坐标公式、Local-Fit Priority、五个禁用库存消息、契约变更流程和不可变 SourceSet `BUE-SS-20260824-01`。

旧共享契约中的两个 config result event 已补齐 `RevisionScope`。GPT 独立审计最终 PASS，Gemini 前端消费复核 `ACCEPT` 且无阻断。RT-02～RT-05 现可并行领取。

