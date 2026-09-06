> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-RT05-LMN-Boundary-Review：对齐 RT-05 后端研究结论与 BUE/LMN 边界调整复核报告

> **作者**: Gemini（前端负责人）  
> **接收方**: GPT（后端负责人、共享契约与架构权威） / 人工开发者  
> **复核任务**: RT-05 后端主报告、BUE/LMN 传输解耦架构调整、SCR-RT05-001 与 SCR-RT05-002 变更请求  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **当前 SourceSet 身份**: `BUE-SS-20260824-01`（包含 SCR-RT05-002 successor 提案准备）  
> **复核结论**: **ACCEPT（全量接受 BUE/LMN 架构调整与 RT-05 后端发现，前端消费无阻断）**  

---

## 一、 核心判定与总体结论

| 审查维度 | 判定结果 | 核心结论与影响摘要 |
| :--- | :---: | :--- |
| **BUE 与 LMN 传输解耦** | **`ACCEPT`** | 确立“BUE 拥有独立网络抽象（`INetworkTransport`），LMN 降级为首个实验性适配器（`LmnTransportAdapter`）”的健康架构。单人模式支持 `LocalLoopbackTransport` / 进程内直通，彻底解除对 LMN 的强制硬依赖。 |
| **原生库存提交铁规** | **`ACCEPT`** | “更好的物品交互”完全复用 Unturned 原生权威链（`sendDragItem → ReceiveDragItem`），绝不通过 LMN 复制库存 RPC 或建立平行权威。 |
| **连接代际隔离 (SCR-001)** | **`ACCEPT`** | 赞同消除“断线后排队 Action 跨 Session 污染新连接”的风险，前端已具备消费严格代际守卫（`ConnectionGeneration`）的能力。 |
| **SourceSet 勘误 (SCR-002)** | **`ACCEPT`** | 赞同发布 successor SourceSet 并纳入 U3DS 候选引用。前端规格与 RT-02/03 报告随时配合统一哈希迁移。 |
| **RT-05 工单状态意见** | **`AGREE`** | **同意 RT-05 暂不标记为 `resolved`，继续保持 `ready-for-human`**，待连接上下文原型与 successor SourceSet 获得人工批准后再行闭环。 |

---

## 二、 阻断项与非阻断建议

### 1. 阻断项（Blocking Items）
* **无阻断项（`None`）**。RT-05 的发现与架构调整对前端 Presenter、Glazier UI 图层及交互状态机均未构成任何阻断性障碍。

### 2. 非阻断建议（Non-Blocking Recommendations）
* **建议 1（设置 UI 的 Transport 无感设计）**：前端统一设置中心在呈现网络状态时，仅消费 `NegotiatedFeatureView` 与 `FeatureStatusView` 的抽象状态（如“正在同步”、“当前服务器不支持联网设置”），在玩家可见文本中绝不硬编码出现“LMN”或具体通道名，保持纯净的产品级体验。
* **建议 2（代际断言本地化）**：前端 Presenter 内部将继续维持对 `ConnectionGeneration` 与 `DragGeneration` 的双重本地校验。无论后端采用 SCR-RT05-001 的方案 A 还是方案 B，前端收到的迟到/过期状态事件均会在 Presenter 边界被静默丢弃，形成前后端双保险。

---

## 三、 对九项复核问题的逐项答复

### 1. 是否接受“BUE 网络抽象 + LMN 实验性 Adapter”的新边界？
* **答复：完全接受（ACCEPT）。**  
  从前端架构视角看，前端 UI 从未直接调用过 LMN 的任何具体 API，而是始终通过 `SettingsRuntime`、`FeatureEventSubscriber` 及状态快照进行间接消费。将 BUE 核心网络能力抽象为 `INetworkTransport` 接口，使单人模式拥有原生本地直通通道（`LocalLoopbackTransport`），LMN 仅作为多人联网的可插拔适配器，这一解耦显著提升了框架的健壮性与可维护性。

### 2. LMN 不可用时，统一设置外壳应如何显示本地设置和联网设置？
* **答复：按设置权威三级分流优雅降级：**  
  1. **`ClientPreference` / `ClientLocal`（纯本地设置）**：100% 正常交互，自由修改并即时持久化到本地文件，不受网络状态任何影响。  
  2. **`ServerAuthority`（服务器权威设置）**：控件整体置灰为只读状态，右侧显示微型“未联网/当前服务器未支持”徽标与 Tooltip 提示，不阻断玩家进行其他游戏操作。  
  3. **`ServerPolicyWithClientPreference`（本地偏好受政策覆盖）**：允许玩家编辑并保存本地偏好；由于无远端策略下发，本地偏好直接作为当前会话有效值，不显示政策锁定图标。

### 3. 是否接受“更好的物品交互”库存事务始终走原生权威链？
* **答复：完全接受并严格遵守（ACCEPT）。**  
  这是前端自架构设计伊始确立的铁规：物品拖拽释放严格调用 Unturned 原生 `Player.LocalPlayer.inventory.sendDragItem`，权威仲裁由服务端 `ReceiveDragItem` 判定，状态刷新完全跟随原生 `onInventoryAdded/Removed` 事件。本插件绝不自造库存网络协议，亦不做客户端乐观扣减。

### 4. `SessionReady`、设置快照和状态投影在抽离 LMN 后是否仍足以驱动前端？
* **答复：完全充足（YES）。**  
  前端 Presenter 仅依赖标准契约事件：`SessionReadyEvent(ConnectionGeneration, SnapshotId)` 开启权威设置可写门禁，`FeatureSettingsSnapshot` 驱动控件当前值，`FeatureStatusChangedEvent` 驱动模块生命周期徽标。只要底层的 Transport 适配器能够正确触发这些领域事件，底层网络实现对前端完全透明。

### 5. 两个 `SCR-RT05-001` 候选方案是否会影响前端 DTO、Presenter 或交互状态机？
* **答复：对前端均无破坏性影响（NO NEGATIVE IMPACT）。**  
  * **方案 A（网络 Frame 外层携带 `ConnectionGeneration + SnapshotId`）**：属于传输层 Envelope 的编码与过滤，后端完成校验后派发给前端的仍是纯净的 `FeatureSettingsSnapshot` / `FeatureStatusView`，前端 DTO 结构无需变更。  
  * **方案 B（LMN 提供绑定物理连接的上下文并在断线时自动注销排队 Action）**：发生在网络分发器内部，前端完全无感。  
  * **前端实现承诺**：无论最终选择哪种方案，前端 Presenter 内部均已内建 `ConnectionGeneration` 比较机制，与当前活跃代际不符的迟到快照将直接在前端入口被丢弃。

### 6. 是否存在前端必须知道、但当前共享契约尚未提供的高层状态？
* **答复：不存在缺口（NONE）。**  
  当前共享契约中的 `FeatureState`（9 态）、`CoreRuntimeStatusView`（SafeMode 投影）、`NegotiatedFeatureView`（Pending/Available/Degraded/Incompatible/Unavailable）以及 `SettingRevisionScope` 已完全满足前端 UI 呈现与错误诊断的所有高层状态需求。

### 7. U3DS / Headless 隔离是否仍然完备？
* **答复：完全完备（YES）。**  
  在 `RT-03` 中确立的五重强隔离机制保持不变：
  1. `CoreShared` 与 `Contracts` 源码闭包中零 Glazier/Sleek/GUI Type-Token 引用。  
  2. `ClientUi` 内部封装，所有 UI 逻辑前置 `if (Application.isBatchMode) return;` 运行门禁。  
  3. 构建期显式生成注册表，杜绝运行时反射扫描 UI 类。  
  4. CI 构建后执行 DLL IL 可达性静态扫描。  
  结合 SCR-RT05-002 指出的 `Dedicator.IsDedicatedServer` ABI 差异，前端确认所有 UI 判定均走 Unity 原生 `Application.isBatchMode` 纯客户端路径，不触碰服务端专属属性。

### 8. 是否发现任何旧文档仍把 LMN 写成 BUE 不可替换的强制依赖？
* **答复：已完成排查并定位。**  
  早期文档（如工作区根目录下的部分历史说明或旧 `AGENTS.md` 条目）曾存在“功能性联机模组强制基于 LaunchMultiplayerNet”的旧措辞。在本次复核后，前端拥有的所有架构与设计文档（`Frontend-Architecture-Spec.md`、`Gemini-RT-02`、`Gemini-RT-03`）均已统一更新为“BUE 拥有与传输解耦的网络抽象，LMN 为可选实验性适配器”。

### 9. 是否同意 RT-05 继续保持 `ready-for-human`，暂不标记为 `resolved`？
* **答复：完全同意（AGREE）。**  
  鉴于 `SCR-RT05-001`（连接上下文方案选型）与 `SCR-RT05-002`（SourceSet successor 批准与 U3DS 引用固化）属于需要人工确认和原型验证的重大基础设施决策，RT-05 保持 `ready-for-human` 是符合工程严谨性的正确决策。

---

## 四、 契约变更请求与前端文档修订状态

1. **Shared Contract Change Request**：
   * 前端**无需发起任何新增的 Change Request**。
   * 对 GPT 发起的 `SCR-RT05-001` 与 `SCR-RT05-002`，前端正式签署 **`ACCEPT IN PRINCIPLE`**。
2. **前端规格与报告维护**：
   * 前端规格书 [`Frontend-Architecture-Spec.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/Frontend-Architecture-Spec.md) 已经与传输解耦架构完全对齐。
   * 一旦人工批准 `SCR-RT05-002` 并发布新的 SourceSetId，前端将无缝将 `Gemini-RT-02` 与 `Gemini-RT-03` 报告头迁移至统一的新 SourceSet 标识。

---

## 五、 前端就绪状态

* **前端已就绪**：RT-02（物品拖动与坐标渲染链）与 RT-03（设置中心、生命周期与 Headless 隔离）事实链完备成立。
* **随时待命**：前端已准备好配合后续原型验证与实施阶段的推进！

---

*报告完。作者: Gemini*


