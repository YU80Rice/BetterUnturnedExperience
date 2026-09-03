# 独立 LMN 共存接管机制

Type: wayfinder:research
Status: claimed（2026-09-03 本会话认领，research 子代理已触发）
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: V2-T1-itransportconnection-shape（已 resolved；先查证 LMN/原生传输内部）

## Question

当用户同时安装 BUE 与独立 LMN 时，BUE 如何检测独立 LMN 实例并停用其运行（不删除 DLL、不误伤不相关插件）？

## 查证要点

1. 独立 LMN 的入口身份（BepInEx GUID、程序集名、Harmony patch 目标）——如何静态识别。
2. 检测机制：程序集扫描禁区（BUE 不扫描 plugins 目录）之外，如何发现"已加载的 LMN 实例"——依赖 BepInEx 运行时事实？事件订阅？
3. 停用机制：阻止独立 LMN 重复运行的可行路径（如在其入口短路、patch 拦截、运行时标记），对照"不删除用户文件、不误伤其他插件"约束。
4. 诊断/恢复：接管状态如何可诊断、可恢复（面板显示"已由 BUE 接管"）。
5. 证据：LMN 源码/反编译事实 + 文件路径 + 行号；必要时用 browser-skill 查公开仓库（Forge 的模块接管/冲突处理实现可作对照）。

## 已预埋事实（T1 查证，research 直接引用不必重查）

- LMN 是 BepInEx 插件（`LaunchMultiplayerNet.dll`），其 Harmony Prefix 拦截目标 = `NetMessages.ReceiveMessageFromClient(ITransportConnection, byte[], int, int)`（U3-SDK `NetMessages.cs:123`）；帧识别靠魔数 + `ModRouter.BuildModPacket`（LMN `Routing\ModTransport.cs:714-726`）。
- LMN 程序集引用集：`mscorlib, BepInEx, 0Harmony, com.rlabrecque.steamworks.net, SDG.NetTransport, Assembly-CSharp, UnityEngine.CoreModule, System`（T1 反编译确认）。
- LMN V2 命名频道 = pluginGuid 路由；V1 = int virtualChannel——两种帧都在 `ITransportConnection.Send` 之上（T1 报告）。
- 停用需满足：不删除 DLL、不误伤不相关插件、可诊断可恢复（CONTEXT.md「网络能力接管」L69-71 +「独立 LMN 文件处理」L81-83）。

## 答案

（resolved 时记录机制选型 + 证据）
