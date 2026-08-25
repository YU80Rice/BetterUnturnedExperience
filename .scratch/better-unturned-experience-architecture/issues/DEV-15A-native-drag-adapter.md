# DEV-15A：Native Drag Adapter

Type: task  
Status: in-progress  
Owner: GPT（原生拖拽适配与后端权威边界）  
Required reviewer: Gemini（前端消费与 UI 接线）  
Parent: DEV-15  
Baseline: BUE-V1-RT01-20260824  
SourceSet: BUE-SS-20260824-02  
Specification: `../spec-DEV-15-better-item-interaction.md`

## 目标

建立 `ClientInventoryInteractionAdapter` 的纯测试 Seam：读取已归一化的原生拖拽上下文，识别普通网格与特殊分支，对合法候选调用原生 `sendDragItem`，对普通网格非法候选调用 `stopDrag`，对快捷槽、AREA、同格取消和其他特殊路径完全 pass-through。

## 本票范围

- 不实现真实 Glazier 预览；
- 不实现投影 Relay 或 `AwaitingProjection`；
- 不修改 Contracts；
- 不直接调用 `ReceiveDragItem`、`removeItem`、`addItem` 或 LMN；
- 不扩大 Harmony/原生 Hook 作用域。

## 已冻结测试 Seam

- 纯值输入：拖拽来源、目标页、候选和拖拽状态；
- 原生动作端口：`SendDragItem`、`StopDrag`；
- 输出：`PassThrough`、`Submitted`、`Cancelled`；
- 普通网格判定：`targetPage >= slotsPageBoundary && targetPage != areaPage`。

## 验收条件

- [ ] Red → Green 测试覆盖普通网格合法提交；
- [ ] 普通网格非法/越界候选停止拖拽并保留原物；
- [ ] 快捷槽、AREA、同格取消和非拖拽状态不调用 BUE 提交端口；
- [ ] 地面来源 → 普通网格可提交；
- [ ] 不调用平行库存权威；
- [ ] ClientUi Release 构建 0 errors / 0 warnings；
- [ ] 独立审计 PASS；
- [ ] Gemini 消费复核 ACCEPT。
