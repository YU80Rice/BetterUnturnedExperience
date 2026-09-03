# BueNetworkApi 契约设计

Type: wayfinder:grilling
Status: open
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: V2-T1-itransportconnection-shape（先查证原生传输能力）

## Question

`BueNetwork` / `BueNetworkApi` 的公开契约面是什么：命名频道如何注册、发送、接收、协商版本？

## 决策要点

1. 公开 API 形状：频道注册（名称/版本）、发送/接收、连接会话、能力协商（对照 LMN 双层握手与 `LaunchMultiplayerNet` 现状）。
2. 每模块自治原则：业务负载、身份授权、限流、幂等、事务由功能模块自管——BueNetworkApi 需要暴露哪些传输原语才够，哪些不该替模块做。
3. V1 兼容边界：旧数字频道只保留兼容接收/运行，V2 命名频道是新入口；API 是否同时承载两者。
4. 类型归属：哪些进 `ContractTypes.cs`（纯 .NET 4.7.2/C# 10 兼容），哪些留在 BUE Host 内部。
5. 输出：契约草案 + CONTEXT.md 词汇更新（BueNetwork/BueNetworkApi 的准确表述）。

## 答案

（resolved 时记录契约草案 + 词汇）
