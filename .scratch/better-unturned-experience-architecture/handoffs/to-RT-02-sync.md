> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini → GPT：RT-02 前端库存 UI 与坐标调研同步报告（修订版）

> **作者**: Gemini（前端负责人）  
> **接收方**: GPT（共享契约与权威边界强制 Reviewer）  
> **适用任务**: RT-02  
> **SourceSet 身份**: `BUE-SS-20260824-01`  
> **状态**: 已吸收 GPT 复核意见完成 B-01、B-02、B-04 修订，票据状态暂设为 `ready-for-human`  

---

## 1. 阻断项修订对账（B-01, B-02, B-04）

1. **B-01（`onPlacedItem` 拦截范围与 Adapter Seam 细化）**：
   - 彻底废除全量 `return false` 接管方案。
   - **严格限定拦截作用域**：仅在目标为普通库存/容器网格（`page >= PlayerInventory.SLOTS && page != PlayerInventory.AREA`）时介入。
   - **特殊分支完全放行（Pass-Through）**：对快捷槽装备（`page < SLOTS`）、地面丢弃与拾取（`AREA`）、同格点击取消等分支，Prefix 直接 `return true` 放行交由原生逻辑处理。
   - **候选落点替换**：仅对普通网格合法候选调用原生 `sendDragItem`，非法/越界候选调用 `stopDrag()` 取消并保留原物。明确将该 Hook 标记为待生产原型验证的高风险 Seam。
2. **B-02（原生库存事件的事实源性质）**：
   - 将“服务端确认后触发”修正为“本地原生库存模型应用变化时触发”。
   - 明确 `onInventoryAdded/Removed` 是原生模型变化的观察，绝非 BUE 自定义提交 ACK。只有在 `DragGeneration`、会话及物品指纹完全吻合时才作为高置信收敛，歧义时以原生最新状态静默刷新。
3. **B-04（证据边界与 SourceSet 勘误）**：
   - 降级“完全保证”措辞为“五重结构设计义务与待验证架构假设”。
   - 明确当前 SourceSet 下 U3DS 引用为 `CANDIDATE_UNFROZEN`，装配门禁采用 `ClientUiAvailable && !Application.isBatchMode && !Headless`。

---

## 2. 产物路径

- **完整调研报告（修订版）**: [`RT-02-Frontend-Inventory-Coordinate-Research.md`](../RT-02-Frontend-Inventory-Coordinate-Research.md)
- **已更新票据**: [`issues/RT-02-frontend-inventory-ui-coordinate-research.md`](../issues/RT-02-frontend-inventory-ui-coordinate-research.md)


