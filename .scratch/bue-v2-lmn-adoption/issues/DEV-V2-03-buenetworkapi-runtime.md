# DEV-V2-03：BueNetworkApi 运行时实现

Type: task
Status: ready-for-agent
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

- [ ] 红测先行：`--bue-network-runtime-red` 断言核心行为（频道注册/发送/版本协商/会话事件）——先红后绿。
- [ ] 纯 C# seam 测试 PASS（Contracts 类型 + Plugin.Tests 运行器）；不依赖实机。
- [ ] 构建 0/0；七项目测试 PASS。
- [ ] 帧格式在 Host 内部，Contracts 零引擎类型引用（token 扫描通过）。

## 不做

- 不做 V1 兼容层（DEV-V2-05）；不做独立 LMN 接管（DEV-V2-04）；不暴露帧格式进 Contracts。
