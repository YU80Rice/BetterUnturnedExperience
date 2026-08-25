# DEV-15A：Native Drag Adapter

Type: task  
Status: ready-for-human  
Owner: GPT（原生适配 Seam 与后端边界）  
Required reviewer: Gemini（前端消费与 UI 接线）  
Parent: DEV-15  
Baseline: BUE-V1-RT01-20260824  
SourceSet: BUE-SS-20260824-02  
Specification: `../spec-DEV-15-better-item-interaction.md`

## 目标

建立 ClientUi 与 Unturned 原生库存拖拽之间的最小受控 Adapter Seam：普通背包/容器网格的合法候选调用原生 `sendDragItem`；快捷槽、AREA、丢弃和其他特殊分支原生放行；非法候选停止增强拖拽并保留原物。

## 本票范围

- 定义纯值拖拽释放输入、页面分类策略和释放结果；
- 实现普通网格、特殊分支、非法候选和陈旧代际的 fail-closed 行为；
- 通过委托隔离原生 `sendDragItem`、`stopDrag` 和 pass-through 操作；
- 为 DEV-15B 提供稳定的前端消费接口；
- TDD 覆盖逐项行为和调用次数。

## 明确不做

- 不实现 Unity/Glazier/Sleek 具体坐标转换；
- 不实现占据预览、浮动图标或投影 Relay；
- 不直接引用 Unturned 原生类型；
- 不新增 Contracts 字段或库存 RPC；
- 不修改 LMN、BepInEx 或 Unturned 原版。

## 验收条件

- [x] TDD Red → Green 证据：普通网格合法候选只调用一次原生提交；
- [x] 合法候选动作顺序固定为 `sendDragItem → stopDrag`，非法候选仅 `stopDrag`；
- [x] 特殊分支只调用 pass-through，不调用 BUE 提交；
- [x] 非法/越界候选调用 stopDrag，保留原物；
- [x] 未拖拽或代际不匹配时 fail-closed，不触发任何原生动作；
- [x] Adapter 无 Unity/Glazier/Sleek/LMN/Unturned 类型引用；
- [x] ClientUi Release 构建 0 errors / 0 warnings；
- [x] 独立 GPT 审计 PASS；
- [ ] Gemini 前端消费复核 ACCEPT 后方可关闭本票。

## GPT 实施记录（2026-08-25）

- 先以 Red 测试捕获合法候选动作顺序与陈旧代际特殊页放行问题，再完成 Green 修复；
- 合法普通网格：`SendDragItem → StopDrag`；
- 非法普通网格：仅 `StopDrag`；
- 当前代际特殊页：`PassThrough`；陈旧代际（含特殊页）：零原生动作并返回 `Cancelled`；
- Release solution：0 errors / 0 warnings；7/7 测试 PASS；
- GPT 独立审计 R3：PASS，报告 `audit/2026-08-25/GPT-DEV-15A-Independent-Audit-R3.md`；
- 当前 ClientUi DLL SHA-256：`473F4ABB1A924B6B78DD9ABAEFABEBF670EB0D73A3CC5CA09139404C1D355B46`；
- 运行证据：无；真实 Unity/Unturned Hook、SP/P2P/U3DS 仍属于后续门禁。
