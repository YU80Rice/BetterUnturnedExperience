> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-DEV-06-Network-Review：DEV-06 网络编解码与 Ready 门禁终审复核报告

> **作者**: Gemini（前端负责人 / 网络消费方）  
> **接收方**: GPT（后端负责人、架构与发布门禁） / 人工开发者  
> **复核方法论**: `codebase-design`（深度模块、线性化 Seam、容量隔离、Fail-Closed 防御） + `tdd`（并发切代、异常策略、白名单与双代际测试）  
> **复核对象**: DEV-06 交付物（`BueEnvelopeCodec`, `BueBootstrapCodec`, `BueFrameCodec`, `ReadyFrameFence`, `LocalLoopbackTransport`, `LmnTransportAdapter`, `BetterUnturnedExperience.Network.Tests`, `Implementation-DEV-06-1255.md`）  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **SourceSet 身份**: `BUE-SS-20260824-02`  
> **判定结论**: **ACCEPT（网络与编解码终审全量通过，无阻断异议，正式准予 DEV-06 关闭）**  

---

## 一、 核心复核结论与判定矩阵

| 审查维度 | 判定结果 | 前端/网络消费核查说明 |
| :--- | :---: | :--- |
| **1. 52-Byte Ready 门禁前置拒绝** | **`ACCEPT`** | `ReadyFrameFence` 严格以 generation、snapshot、双 16 字节 nonce 与 52 字节 prefix 作为前置拒绝边界；失配直接返回原因码且不进 DTO/Handler。 |
| **2. RequestId 语义定位** | **`ACCEPT`** | 前端 Presenter / 设置同步消费严格将 `RequestId` 仅视为代际内幂等关联键，绝不将其解释为身份、权限或网络认证。 |
| **3. 独立读写容量隔离** | **`ACCEPT`** | 写重放窗口（128 容量）与读 in-flight（16 容量）完全独立；写窗口满载时只读快照请求（`0x0104`）仍可正常收敛；切代原子清空旧状态。 |
| **4. 线性化 Seam 与双锁设计** | **`ACCEPT`** | `dispatchSync` 与 `sync` 分离；状态锁不跨越外部 Handler；读 Handler 异常释放 in-flight 槽位，写 Mutation 异常保留 replay 占位并 fail-closed。 |
| **5. 传输层解耦与线程安全入队** | **`ACCEPT`** | `LocalLoopbackTransport` 完美支撑单机/测试；`LmnTransportAdapter` 保持纯委托 Seam（零 LMN 具体类型），回调仅入队、由 `Pump()` 受控派发。 |
| **6. 严格白名单与零类型泄漏** | **`ACCEPT`** | 仅开放 `0x0001..0x0004`、`0x0101..0x0104`、`0x0201`；Contracts/Core/Transport 对 Unity/Sleek/LMN/Native 引用数**精确为 0**；未复制库存 RPC。 |

---

## 二、 深度架构审计细节（Codebase Design & Seam Analysis）

### 1. 深度模块实现（`ReadyFrameFence`）
* **小接口 + 深实现**：
  * 对外仅暴露 `TryAccept(frame, handler)`、`ReplaceBinding(next)`、`CompleteRead(requestId)` 3 个方法。
  * 内部完整封装代际校验、常数时间 Nonce 比较（`ByteEquality.FixedEquals`）、SHA-256 负载指纹、读写分域重放防护以及异常状态回退逻辑。
* **并发切代与死锁防御（Deadlock & Reentrancy Safety）**：
  ```csharp
  lock (dispatchSync)
  {
      lock (sync) { /* 状态校验与登记 */ }
      try { handler(frame); }
      catch { /* 读释放 / 写占位 */ throw; }
      return new FrameHandlingResult(FrameDecision.Applied);
  }
  ```
  状态锁 `sync` 在 Handler 执行前即时释放，外部业务逻辑或 Presenter 触发的状态读取绝不会与网络状态锁产生死锁。
  `ReplaceBinding` 同样获取 `dispatchSync`，保证切代时排队的旧 Handler 完全退出后再原子刷新绑定。

### 2. 编解码器与版本安全（`BueEnvelopeCodec` / `BueBootstrapCodec`）
* **固定 Magic 分流**：
  * 显式识别 ASCII `BUEB`（0x5542, 0x4245），碰撞时返回 `ReservedBootstrapCollision`。
  * 拒绝超出 16KB 的异常超长帧（`PayloadTooLarge`），杜绝内存放大攻击。
* **Prefix 规格严密性**：
  * 校验 `Version == 1`，`Flags == 0`，`Reserved == 0`，任何未知标记立即阻断（`UnsupportedFenceVersion` / `NonZeroFlags`）。

---

## 三、 前端 Presenter 消费与库存权威确认

1. **网络设置消费闭环**：
   * 前端 Presenter 在联机模式下提交 `UpdateModuleConfig`（`0x0101`）或拉取快照 `RequestModuleConfigSnapshot`（`0x0104`）时，直接通过 `BueFrameCodec` 封装 52-byte prefix 并由 Transport 发送。
   * 收到服务端的 `0x0102` / `0x0103` / `0x0201` 时，经 `ReadyFrameFence` 过滤后直接更新前端不可变快照 `FeatureSettingsSnapshot`。
2. **库存权威绝不越权**：
   * 前端确认：物品拖拽（Better Item Interaction）始终使用 Unturned 原生 `sendDragItem` 与服务端权威链，网络层**绝不建立任何平行的库存 RPC**。

---

## 四、 证据边界声明与后续推进

* **证据边界**：本复核确认 DEV-06 的协议编解码、ReadyFrameFence、并发线性化与传输委托 Seam 在 C# 层面达到生产就绪标准；本报告不代表已在真实 LMN、P2P 联机环境或 U3DS 运行通过。
* **工单状态**：同意正式将 [`DEV-06-network-codec-ready-fence.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/DEV-06-network-codec-ready-fence.md) 标记为 **`resolved`**。
* **开发推进**：**DEV-01 ～ DEV-06 全部开发与复核已 100% 闭环！** 同意正式开启最终交付门禁 **`DEV-07`**（CandidateBuild 构建与单人/P2P/U3DS 同哈希验收包）！

---

*报告完。作者: Gemini*


