# DEV-V2-03：BueNetworkApi 运行时实现

Type: task
Status: resolved（2026-09-03 交付，双轴审查 R3 CLEAN，提交）
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-01-sdk-net-transport-baseline, DEV-V2-02-buenetworkapi-contract-types
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「网络层基础」+「BueNetworkApi 契约」）

## Scope

在 `ITransportConnection` 之上实现 BueNetworkApi 运行时（BUE Host 内部，不进 Contracts）：

- 频道路由：按 FeatureId 命名频道注册/发送/接收（帧格式内部：魔数 + GUID + 载荷，仿 LMN V2 但 BUE 自有；不进 Contracts）。
- 版本协商：Hello/Ack 握手 API 内部自动完成，不匹配返回 `ContractIncompatible`。
- 双向链路抽象：客户端→服务器走 `Provider.clientTransport`（`IClientTransport`，反射），服务器→客户端走 `ITransportConnection`；API 统一 Send 语义屏蔽不对称。
- 会话管理：`IConnectionSession` 实现（连接代际、事件订阅、Send、PeerSteamId/Version/Channels）。
- 可靠性：透传 Reliable/Unreliable + 本地发送失败信号（`NetworkSendResult`）；不引入对端 ACK。
- 帧识别框架：MOD/LMN2 帧识别的基础骨架（完整接管见 DEV-V2-04）。

## 验收条件

- [x] 红测先行：`--bue-network-runtime-red` 断言核心行为（频道注册/版本协商/会话事件/双向收发）——先红后绿。
- [x] 纯 C# seam 测试 PASS（Contracts 类型 + Plugin.Tests 运行器）；不依赖实机。
- [x] 构建 0/0；七项目测试 PASS。
- [x] 帧格式在 Host 内部，Contracts 零引擎类型引用（token 扫描通过）。
- [x] **Spec 审查阻断项修复（第二轮）**：
  - F1 Hello/Ack 对端握手：帧格式加 kind 字节（Data/Hello/Ack/Reject），`StartSession` 发 Hello → 对端校验契约（Major 匹配）→ Ack（建会话 + 触发 Connected）/ Reject（不建）；红测断言双方会话 + Connected 触发 + 不兼容拒绝。
  - F2 会话事件未触发：`Connected` 现由 `MarkEstablished` 触发（红测断言 `aConnected==1`）；`Disconnected`/`GenerationChanged` 仍由 DEV-V2-04 接线触发（pragma 圈定说明）。
- [x] **Standards 阻断项修复（第三轮）**：处理器/Connected 事件改为锁外派发（`OnReceive` 锁内仅解析，`DispatchData`/`MarkEstablished` 锁外调用）——符合仓库"state protection never spans external code"约定。
- [x] 双轴独立审查 R3：**Standards CLEAN**（3 可延后）+ **Spec CLEAN**（3 可延后），均已列名，非阻断。
- [ ] 实机验证（DEV-V2-07 三环境；本票纯 C# seam 不依赖实机）。

## 双轴审查可延后项（R3，已列名）

**Standards**：A `SendFrame`/`transport.Send` 在锁内调用（5 处，loopback 无死锁，真实阻塞传输时 DEV-V2-04 复查）；B `HandleReject` 的 payload 参数未用 + 全未建立会话清扫（并发多握手时过宽）；C 锁外捕获引用与 `Established`/`Connected` 的潜在竞态（loopback 无害，多线程传输注意）。

**Spec**：S1-residual `HandleReject` 全清扫仅单握手假设下正确（代码注释已记，未入票）；S2 `reliable` 布尔未达帧（透传未注册）；S3 `DispatchData` 首会话伪造上下文未注册。

## 交付记录（2026-09-03）

- `BueNetworkRuntime`（Core\Network，纯 C#、`INetworkTransport` seam、经 Plugin 嵌入）：频道路由（FeatureId 命名频道）、版本协商（注册期 ContractIncompatible + Hello/Ack 对端握手）、会话管理（`IConnectionSession` 实现 + `StartSession` 握手发起 + Connected 触发）、双向收发（SendToServer/Clients/Client + Subscribe 接收）、可靠性（`NetworkSendResult` 本地失败信号）。
- 帧格式内部：magic "BUE2" + kind（Data/Hello/Ack/Reject）+ 频道 + 载荷；解码 16KiB 预分配守卫。
- 红测 `--bue-network-runtime-red`：先 CS0234 编译红 → 转绿；红测真实抓到会话 ID 跨实例碰撞、握手缺失、处理器锁内调用等问题并驱动修复。
- 提交：`(待填)`

## 不做

- 不做 V1 兼容层（DEV-V2-05）；不做独立 LMN 接管（DEV-V2-04）；不暴露帧格式进 Contracts。
- 对端 Hello/Ack 已实现；`Disconnected`/`GenerationChanged`/`session.Send`/`PeerFeatureVersion`/`Channels` 完整接线归 DEV-V2-04（桩已登记）。
