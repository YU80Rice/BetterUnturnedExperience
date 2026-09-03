# BueNetworkApi 契约设计

Type: wayfinder:grilling
Status: resolved（2026-09-03 两轮 grilling 拍板，契约形状冻结）
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: V2-T1-itransportconnection-shape（已 resolved；先查证原生传输能力）

## Question

`BueNetwork` / `BueNetworkApi` 的公开契约面是什么：命名频道如何注册、发送、接收、协商版本？

## 决策要点

1. 公开 API 形状：频道注册（名称/版本）、发送/接收、连接会话、能力协商（对照 LMN 双层握手与 `LaunchMultiplayerNet` 现状）。
2. 每模块自治原则：业务负载、身份授权、限流、幂等、事务由功能模块自管——BueNetworkApi 需要暴露哪些传输原语才够，哪些不该替模块做。
3. V1 兼容边界：旧数字频道只保留兼容接收/运行，V2 命名频道是新入口；API 是否同时承载两者。
4. 类型归属：哪些进 `ContractTypes.cs`（纯 .NET 4.7.2/C# 10 兼容），哪些留在 BUE Host 内部。
5. 输出：契约草案 + CONTEXT.md 词汇更新（BueNetwork/BueNetworkApi 的准确表述）。

## 答案

### 第一轮（API 形状）

- **Q1 频道粒度 = A**：一个功能模块 = 一个命名频道（`FeatureId` 即频道名），仿照 LMN V2 频道路由方式；多频道 YAGNI（模块可在载荷内分业务）。
- **Q2 版本协商归属 = A**：Hello/Ack 握手在 BueNetworkApi 内部自动完成；模块注册时声明 `MinimumBueContract` + 功能版本，不匹配返回 `ContractIncompatible`（复用 SCR-GPT18-001 reason 体系）。
- **Q3 可靠性边界 = A**：只透传 `Reliable/Unreliable` 两值 + **本地发送失败信号**（send 抛错/回调）；不引入对端应用层 ACK（幂等/事务属模块职责）。
- **Q4 连接生命周期 = A**：API 提供 `IConnectionSession` 事件订阅（Connected/Disconnected/GenerationChanged）；模块注册回调，不轮询。
- **Q5 客户端方向不对称 = A**：抽象进 API（统一 `Send` 语义），模块无感；双向链路差异（`IClientTransport` 反射 vs `ITransportConnection`）由 API 内部处理。
- **Q6 契约归属 = A**：公开 API 面（接口 + 只读值类型）进 Contracts；传输实现（`IClientTransport` 反射、帧编解码、Steam 细节）留 Host 内部——帧格式不引用 `SDG.NetTransport`。

### 第二轮（边界与实现细节）

- **Q7 接收面所有权 = C**：BUE 独占 `NetMessages.ReceiveMessageFromClient/FromServer` 拦截；识别自己的帧（魔数/GUID），非 BUE 帧交还 vanilla；**保留 V1 数字频道兼容接收路径**（兼容层落地于 T4）。补充：LMN 的 V1 数字频道注册方式只保留兼容，不再提供注册。
- **Q8 帧格式归属 = A**：BUE 内部实现细节，不进 Contracts；第三方只用高级原语，不接触帧字节。用户强调："我们负责把规范定好、方向探索好，其他开发者只要拿着规范就能实现功能。"
- **Q9 发送目标寻址 = A**：无"对端 FeatureId"语义；按连接上下文表达目标（`SendToServer` / `SendToClients` / `SendToClient(session)`），不关心对端装了什么。
- **Q10 会话契约 = B**：`IConnectionSession` = `SessionId`（连接代际）+ `Connected/Disconnected/GenerationChanged` 事件 + `Send` 能力 + `PeerSteamId` + `PeerFeatureVersion` + `Channels`（已协商频道版本表）——模块自管授权需要知道对端是谁与版本。
- **Q11 错误码 = A**：复用 `FeatureRegistrationReason` + 新增 `NetworkSendResult` 显式枚举（可本地化，不抛异常于热路径）。
- **Q12 命名空间 = B**：公开类型放独立 `BueNetwork` 命名空间（与 CONTEXT.md「BUE 网络 API」对外名称一致）。

### 实施依赖要求（从 T1 待确认项带入）

- SDK 基线锁定：`Libs\SDG.NetTransport.dll` 与游戏安装二进制哈希不同（接口一致）；实施前刷新 Libs 拷贝 + 记录 `ITransportConnection` 成员清单作为 BUE 依赖基线。
- `ITransportConnection` 接口演进属编译期断裂（非静默行为变化）；成员清单随依赖基线固化。

### 输出

- 契约形状冻结（上述 Q1-Q12）；具体类型签名另立 DEV 工单写入 `ContractTypes.cs`（与 T2 同一实施通道）。
- 解锁：T5（LMN 共存接管机制）。
