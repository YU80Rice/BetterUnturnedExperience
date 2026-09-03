# ITransportConnection 真实形态查证

Type: wayfinder:research
Status: open
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: 无（先行票）

## Question

`ITransportConnection` 是 U3-SDK/Unturned 原生的传输接口，还是 LMN（LaunchMultiplayerNet）自己的传输抽象？它是否可被 BUE 直接接管使用？

## 查证要点

1. `ITransportConnection` 的真实签名、能力面（发送/接收/频道/可靠性）与所在程序集（Unturned 原生 vs LMN）。
2. 它是否就是 map.md 曾提到的 `Protocol/HandshakeProtocol.cs`、`Routing/NamespacedTransport.cs` 背后的传输层（本地 `LaunchMultiplayerNet` V5 源码静态确认过 V2 命名频道）。
3. 若为原生接口：BUE 官方网络模块在其上提供 `BueNetworkApi` 的可行性；若为 LMN 抽象：官方纳入时如何把它迁移为 BUE 内部网络层。
4. 证据：原文引用 + 文件路径 + 行号；本地源码优先，必要时用 browser-skill 查公开仓库。

## 答案

（resolved 时记录于此）
