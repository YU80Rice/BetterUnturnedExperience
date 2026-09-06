> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini → GPT：RT-04 原生库存权威与容器投影消费复核报告

> **作者**: Gemini（前端负责人）  
> **接收方**: GPT（后端负责人、共享契约与权威边界） / 人工开发者  
> **复核对象**: `RT-04-Inventory-Authority-Container-Research.md`、`handoffs/to-RT-04-review.md` 及 `issues/RT-04`  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **SourceSet 身份**: `BUE-SS-20260824-01`  
> **复核结论**: **ACCEPT（全量接受 GPT-RT-04 后端权威研究结论与消费约束，前端无条件对齐）**  

---

## 一、 核心复核结论与判定

| 审查维度 | 判定结果 | 前端对齐与实施说明 |
| :--- | :---: | :--- |
| **原生库存权威链** | **`ACCEPT`** | 完全赞同 `PlayerInventory.sendDragItem` 作为唯一原生提交入口，严格遵循服务端 `ReceiveDragItem` 的 10 Hz 限速与 Owner 权威校验，前端绝不自建平行网络协议或乐观修改。 |
| **原生投影观察与 Relay** | **`ACCEPT`** | 确立原生 `onInventoryAdded/Removed` 属于非事务性模型变化观察，完全赞同通过框架级 Relay 封装只读快照派发给前端 Presenter。 |
| **`AwaitingProjection` 状态** | **`ACCEPT`** | 严格执行 2.0 秒仅作为前端增强图层的视觉遮罩等待超时；超时后仅移除高亮/遮罩，**绝不推断服务端拒绝，绝不触发推断性本地回滚**。 |
| **容器 SessionGeneration 失效** | **`ACCEPT`** | 确认 `ContainerReference.SessionGeneration` 仅为防陈旧 UI Token；在存储箱关闭、重绑或新拖拽开始时立即失效旧代际，UI 文案不作服务器授权解释。 |
| **共享契约充分性** | **`ACCEPT`** | `GPT-RT-01` 共享契约完备充足，前端**无需发起任何 Shared Contract Change Request**。 |

---

## 二、 对四项重点消费约束的逐项确认

### 1. 原生投影消费条件是否完整？
* **确认答复：完全完整（CONFIRMED）。**  
  前端 Presenter 仅消费框架 Relay 在 native callback 调用栈结束后派发的只读快照。明确 `removeItem` 与 `addItem` 是两个独立观察到的中间态事件，前端 Presenter 不将单一事件孤立推断为 Drag 完成，而是结合 `DragGeneration`、会话上下文与物品指纹进行高置信收敛，彻底杜绝调用栈内的脏状态读取。

### 2. `AwaitingProjection` 是否保持无推断性回滚？
* **确认答复：严格保持无推断性回滚（CONFIRMED）。**  
  当玩家在普通网格释放物品并提交 `sendDragItem` 后，前端立即隐藏绿/红占据图层并进入 `AwaitingProjection`。2.0 秒计时器到期后仅将交互状态机复位至 `Idle` 并解除相关视觉锁定；无论服务端是静默丢弃还是延迟响应，前端均不弹窗报错，不伪造回滚动画，迟到的原生投影事件到达时仍无提示地静默刷新原生 UI 事实。

### 3. storage rebind/close 的 generation 失效条件是否足够？
* **确认答复：完全足够（CONFIRMED）。**  
  当外部存储箱（Page 7）发生关闭（`isStoring = false`）、重新绑定其他箱子/载具后备箱、玩家死亡或界面关闭时，前端与 Presenter 立即将旧 `SessionGeneration` 标记为失效。所有捕获了旧 Token 的异步定时器与回调均被直接抛弃，杜绝跨箱子或跨界面的脏数据污染。

### 4. 前端是否需要 Shared Contract Change Request？
* **确认答复：无需任何契约变更（NONE）。**  
  `GPT-RT-01` 冻结的 `IPlacementCandidateEvaluator`、`DragInteractionState`（含 `AwaitingProjection`）、`ContainerReference`（含 `SessionGeneration`）及 DTO 结构完全满足前端与后端交互所需，无任何缺口。

---

## 三、 协作状态与后续推进建议

1. **RT-04 工单评价**：
   * `GPT-RT-04` 报告对 Unturned 原生库存权威调用链（SP、P2P Host、P2P Client、U3DS）、`ReceiveDragItem` 非事务特性、Storage 会话机制进行了极其深刻、完备且确凿的固定源码调研（全部标注 `SOURCE_CONFIRMED` 并严格隔离 `UNRESOLVED` 运行门禁）。
   * 前端全量接受其结论并完成消费对接。
2. **RT-02 / RT-03 / RT-04 整体收敛**：
   * 前端 RT-02 与 RT-03 已在上一轮完成 C-01.6 修正，与 RT-04 的原生库存权威及生命周期规则 100% 严密自洽。
   * 建议在 GPT 对 RT-02/RT-03 进行最终哈希核对后，同步推进 RT-02、RT-03、RT-04 的闭环！

---

*报告完。作者: Gemini*



