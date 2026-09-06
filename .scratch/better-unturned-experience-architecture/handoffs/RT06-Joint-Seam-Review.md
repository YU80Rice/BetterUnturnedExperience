> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-RT06-Joint-Seam-Review：RT-06 实施就绪包与联合 Seam 复核报告

> **作者**: Gemini（前端负责人）  
> **接收方**: GPT（后端负责人、架构与发布门禁） / 人工开发者  
> **复核对象**: [`RT-06-Joint-Seam-Implementation-Readiness.md`](../RT-06-Joint-Seam-Implementation-Readiness.md) 与 [`handoffs/to-RT-06-review.md`](../handoffs/to-RT-06-review.md)  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **SourceSet 身份**: `BUE-SS-20260824-02`  
> **判定结论**: **ACCEPT（全量通过，无阻断项，具备开启 DEV-01 生产开发条件）**  

---

## 一、 核心判定与总体结论

| 审查维度 | 判定结果 | 前端对齐与实施确认说明 |
| :--- | :---: | :--- |
| **Ready-Frame Fence (方案 A)** | **`ACCEPT`** | 确立 52-byte Ready-frame fence prefix 方案为 V1 必需基线；前端 Presenter 严格消费经 fence 过滤后的只读快照，不将 `RequestId` 解释为连接身份。 |
| **物品拖拽端到端时序** | **`ACCEPT`** | 与 `Gemini-RT-02` 100% 一致：抓取 Seam 换算 $\to$ Local-Fit Evaluator $\to$ Glazier 预览 $\to$ 普通网格 `sendDragItem` / 特殊分支放行 $\to$ 原生权威仲裁 $\to$ `AwaitingProjection` (2.0s 仅视觉等待，0 回滚) $\to$ 原生投影收敛。 |
| **设置、生命周期与 Headless** | **`ACCEPT`** | 与 `Gemini-RT-03` 100% 一致：静态 Facet + 动态 Snapshot 双层消费；150ms 防抖；9 态生命周期；Core SafeMode 卸载所有自定义 UI 并提供独立 fallback 提示；`ClientUiAvailable && !isBatchMode && !Headless` 强隔离。 |
| **生产票据依赖顺序 (DEV-01～07)** | **`ACCEPT`** | 依赖路径清晰，前后端解耦良好；前端将在 `DEV-01`～`DEV-04` 基线建立后无缝承接 `DEV-05`（ClientUi / Glazier 集成）。 |
| **共享契约与未决门禁** | **`ACCEPT`** | 维持 `BUE-V1-RT01-20260824` 契约不变，所有生产 IL 扫描与三环境运行测试严格保留为未决验证义务（`UNRESOLVED`）。 |

---

## 二、 对五项重点审查问题的逐项答复

### 1. 是否接受 `SCR-RT05-001` 方案 A 作为 BUE V1 必需基线，B 仅作 LMN 后续加固？
* **答复：完全接受（ACCEPT）。**  
  方案 A 在应用层消息（`0x0101`～`0x0104`, `0x0201`）头部增加 52-byte prefix，强制校验 `ConnectionGeneration + SnapshotId + HandshakeNonceBinding`，彻底杜绝了断线重连后旧排队 Action 污染新会话的安全隐患；且不需要对底层实验性 LMN 进行强制侵入修改，完全符合 BUE 传输解耦的架构原则。方案 B 作为未来的深度防御储备完全合理。

### 2. Ready 后 settings/status projection 是否可全部绑定 `ConnectionGeneration + SnapshotId + nonce binding`，且不把 `RequestId` 解释为连接身份？
* **答复：完全确认并遵守（CONFIRMED）。**  
  前端 Presenter 消费的所有状态快照与投影事件均严格受当前活跃的 `(ConnectionGeneration, SnapshotId)` 守卫。前端明确将 `RequestId` 仅视为当前连接代际内的客户端请求关联与防抖幂等键，绝不在 UI 文案或状态机中将其解释为连接身份或访问授权。

### 3. Better Item Interaction 的 pointer → preview → native submit → native projection 时序是否与 RT-02 一致？
* **答复：完全一致（CONFIRMED）。**  
  时序完全符合 `Gemini-RT-02` 第二轮修订版：
  1. 客户端从原生拖拽上下文读取 pointer、page、footprint、rot 及 `grabOffsetInFootprint`。
  2. Presenter 计算 $\text{intendedItemCenterGrid} = \text{pointerGrid} + (\text{currentFootprintCenter} - \text{grabOffsetInFootprint})$。
  3. 纯 C# Evaluator 执行 Local-Fit Priority 算法（局部当前 $\to$ 局部旋转 $\to$ 全局当前 $\to$ 全局旋转）。
  4. Glazier 分流渲染：悬浮图标挂载顶级全屏容器，占据框挂载目标网格内部（0 GC 对象池）。
  5. 释放时，仅普通网格合法候选调用原生 `sendDragItem`；快捷槽、AREA、同格取消等特殊分支一律放行原生处理（`return true`）。
  6. 服务端原生 `ReceiveDragItem` 执行权威校验，BUE 绝不自建平行库存 RPC。
  7. 前端进入 `AwaitingProjection`（2.0s 仅结束视觉等待，不推断拒绝，不伪造回滚，不弹错误提示）。
  8. 原生 `onInventoryAdded/Removed` 观察事件驱动最终事实刷新，在指纹与代际匹配时高置信收敛。

### 4. Settings Facet/Snapshot、9 态生命周期、SafeMode 和 Headless composition 是否与 RT-03 一致？
* **答复：完全一致（CONFIRMED）。**  
  1. **双层消费**：静态 Settings Facet（构建期生成，负责 Schema、类型、范围、本地化） + 运行时不可变 `FeatureSettingsSnapshot`（负责当前有效值、权限、Revision），150ms / PointerUp 防抖提交。
  2. **网络设置降级**：LMN 不可用时不影响本地设置与纯本地功能；联网权威设置置灰为只读/Unavailable，并展示最后确认快照。
  3. **9 态生命周期**：严格映射 9 种运行状态；单模块 `Isolated` 立即对称卸载其所有 HUD/UI 扩展。
  4. **Core SafeMode**：发生核心故障时卸载全部自定义功能 UI；若独立最小 fallback 提示器可用，显示一次温和提示（带 `DiagnosticId`）；不主动修改原版游戏与暂停菜单，尽可能恢复原版体验。
  5. **Headless 强隔离**：Core/Contracts 零 UI Token；装配前置 `ClientUiAvailable && !Application.isBatchMode && !Headless`；构建期显式生成注册表，杜绝反射扫描。

### 5. `DEV-01`～`DEV-07` 的前后端依赖顺序是否有阻断？
* **答复：无任何阻断（CONFIRMED）。**  
  依赖拓扑极度清晰且解耦严谨：
  * `DEV-01`（Repo / Contracts / CI UI-token gate） $\to$ `DEV-02`（Linker / Catalog / Bootstrap） $\to$ `DEV-03`（SettingsRuntime / Persistence / LocalLoopback） $\to$ `DEV-04`（Better Item Interaction Evaluator & Contract Tests）构建起稳固的领域与单机闭环。
  * 前端将在 `DEV-05` 集中接入 `ClientUi`（SleekPresenter, FootprintLayer, SettingsModal），直接消费前序已验证的纯 C# DTO 与契约接口。
  * 后续 `DEV-06`（Protocol Codec / ReadyFrameFence / LMN Adapter）与 `DEV-07`（CandidateBuild / 三环境同哈希验收）闭环联网与发布门禁。

---

## 三、 验证义务与门禁声明（Verification Obligations）

前端确认：本复核为**实施规格与架构联合 Seam 的静态就绪签署**，绝不代表生产编译、IL 审计或三环境运行通过。以下验证义务将在对应开发票据中逐一作为门禁执行：

1. **`VO-RT02-01`**：`ClientInventoryInteractionAdapter` 在快捷槽、AREA 及普通网格中的分支放行与落点替换单元测试（DEV-04/05）。
2. **`VO-RT02-02`**：连续拖拽下的 0 GC 内存分配性能分析（DEV-05）。
3. **`VO-RT02-03`**：单机、P2P Host/Client 及 U3DS 三环境下的拖拽与原生投影双机联调（DEV-07）。
4. **`VO-RT03-01`**：CI 单 DLL 的 IL 可达性扫描，断言 Core 命名空间对 UI Type-Token 引用为 0（DEV-01/07）。
5. **`VO-RT03-02`**：真实 U3DS 专用服务端实际加载 CandidateBuild DLL 并验证日志无异常（DEV-07）。
6. **`VO-RT03-03`**：Core SafeMode 故障注入下的 UI 卸载与原生菜单恢复验证（DEV-05/07）。

---

## 四、 结论与推进裁定

* **前端判定**：**`ACCEPT`**。
* **开发推进**：**RT-01～RT-06 全部研发调研与联合 Seam 对账已 100% 闭环。前端已完全准备就绪，同意正式开放 `DEV-01` 生产开发！**

---

*报告完。作者: Gemini*



