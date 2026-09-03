# DEV-V2-02：BueNetworkApi 契约类型写入

Type: task
Status: resolved（2026-09-03 交付，双轴审查 CLEAN，提交）
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

- [x] 红测先行：`--bue-network-contract-red` 断言新类型存在且形状正确（先 CS0234 编译红 → 转绿；红测曾抓到自己一处断言参数位置错误并修正）。
- [x] Contracts 项目 0/0 构建；七项目测试全 PASS。
- [x] UI/native token 扫描零命中（Contracts 不引用引擎类型）。
- [x] 类型签名与 T3 Q1-Q12 逐项一致（Spec 轴逐项确认 12/12）。
- [x] **仓库修复**：`src/BetterUnturnedExperience.Contracts/` 此前从未被 git 跟踪（Plugin 靠 `<Compile Include>` 嵌入编译，源码未入库）——本票将其 csproj + ContractTypes.cs 纳入跟踪（bin/obj 排除），交付物正式入库。

## 交付记录（2026-09-03）

- 新增 `BetterUnturnedExperience.Contracts.BueNetwork` 命名空间 5 类型：`NetworkSendResult`（枚举）/ `ChannelRegistrationResult` / `ChannelVersionEntry` / `IConnectionSession` / `IBueNetworkApi`。
- 契约面：RegisterChannel(FeatureId, ContractVersion, ushort)（Q1/Q2）；Reason 复用 `FeatureRegistrationReason`（ContractIncompatible=202）；SendToServer/SendToClients/SendToClient(channel, session, payload, reliable)（Q9）；会话含 SessionId/PeerSteamId/PeerFeatureVersion/Channels（Q4/Q10）；纯 .NET 4.7.2 无引擎引用（Q6/Q12）。
- 红测锚点：`--bue-network-contract-red`（Plugin.Tests）+ 全套调用。
- 双轴审查：Standards CLEAN（3 smell 可延后）+ Spec CLEAN（5 smell 可延后），均已列名。
- 解锁：DEV-V2-03（BueNetworkApi 运行时）。

## 不做

- 不实现帧编解码、`IClientTransport` 反射、Steam 细节（留 Host 内部，DEV-V2-03）。
