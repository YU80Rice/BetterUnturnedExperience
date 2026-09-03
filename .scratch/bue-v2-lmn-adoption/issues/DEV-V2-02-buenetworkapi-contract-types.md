# DEV-V2-02：BueNetworkApi 契约类型写入

Type: task
Status: ready-for-agent
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: 无（与 01 并行；纯 .NET 类型，不依赖 SDK 刷新）
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「BueNetworkApi 契约」）

## Scope

把 wayfinder T3（Q1-Q12 冻结）的 BueNetworkApi 公开契约类型写入 `ContractTypes.cs`（`BueNetwork` 命名空间），纯 .NET Framework 4.7.2 / C# 10 兼容，不引用 Unity/Glazier/Sleek/LMN/Unturned/BepInEx 具体类型：

- 频道注册面：一个功能模块 = 一个命名频道（FeatureId 即频道名）；注册声明 `MinimumBueContract` + 功能版本。
- 版本协商结果：不匹配返回 `ContractIncompatible`（复用 SCR-GPT18-001 `FeatureRegistrationReason`）。
- 会话契约 `IConnectionSession`：SessionId（连接代际）+ Connected/Disconnected/GenerationChanged 事件 + Send 能力 + PeerSteamId + PeerFeatureVersion + Channels（已协商频道版本表）。
- 发送目标语义：SendToServer / SendToClients / SendToClient(session)——无对端 FeatureId 寻址。
- 错误码：新增 `NetworkSendResult` 显式枚举（可本地化；复用 `FeatureRegistrationReason` 用于注册失败）。
- 可靠性：透传 Reliable/Unreliable + 本地发送失败信号（不引入对端 ACK）。
- `FeaturePresentationState`（Available/Degraded/HeadlessOnly/Failed）——SCR-GPT18-001 已批准，一并在本票写入。

## 验收条件

- [ ] 红测先行：`--bue-network-contract-red` 断言新类型存在且形状正确（编译红→绿）。
- [ ] Contracts 项目 0/0 构建；`BetterUnturnedExperience.Contracts.Tests` 全 PASS。
- [ ] UI/native token 扫描零命中（Contracts 不引用引擎类型）。
- [ ] 类型签名与 T3 Q1-Q12 逐项一致（对照 `issues/V2-T3-buenetworkapi-contract.md` 答案节）。

## 不做

- 不实现帧编解码、`IClientTransport` 反射、Steam 细节（留 Host 内部，DEV-V2-03）。
