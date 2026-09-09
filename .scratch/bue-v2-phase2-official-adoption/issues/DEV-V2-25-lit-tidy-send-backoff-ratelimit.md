# DEV-V2-25 LIT 整理网络：定向发送失败退避 + 告警限频 + 传输持续失败面可见化

- **Status**: open
- **Type**: implementation
- **Priority**: P2
- **Blocked By**: —（与 DEV-V2-24 并行可开工；24 不被本票阻塞）
- **Origin**: DEV-V2-24 采集期实机发现 F-C（具名延期，票面 2026-09-08「F-B1 修复轮闭环」节）

## Background

DEV-V2-24 P2P 实机两轮捕获同案：主机 LitTidyNetService 对既成会话的定向发送持续失败，告警无限刷屏。

- 证据 A（诊断包 20260908_210433，主机）：`[TidyNet] 定向发送未送达（generation=2, result=LocalTransportUnavailable）` **1199 条**（:1301 起）。
- 证据 B（诊断包 20260908_225112，主机）：同案 **1057 条**；同轮客机包 225127:1111 显示 challenge（generation=2）**曾送达并应用**——链路中途劣化（出向帧持续被拒、断线事件不上抛），而非首拍失败。
- 传输语义：`BueNetworkRuntime.SendToClient` 对过期/异地会话返回 NoSession（可排除会话失效）；`LocalTransportUnavailable` 仅来自 `transport.Send` 返回 false（SendFrame :876）——即底层 LMN/SPF P2P 出向通道拒绝，且连接生命周期事件未触发。
- 同会话 LHT `SendToClients` 广播与 LIR 定向发送在证据 A 轮成功（证据 B 轮待查）——失败呈通道/时机选择性。

## Scope（拟修）

1. **重臂退避**：F-A 修复的「挑战发送失败→回滚采纳→下一拍重发现」环路在传输持续不可达时逐拍自旋（每循环 1 条 WARN）。沿 BueNetworkRuntime 握手重探退避先例（1s→8s）给重臂加退避。
2. **告警限频**：`LitTidyNetService.TrySendToSession`（:420）同会话代际同类失败限频（首条+每 N 条或时间窗一条），附累计计数。
3. **失败面可见化**：连续失败达到阈值时上抛一条结构性诊断（供面板/日志定位「链路劣化」状态），不静默、不炸帧。
4. 证据 B 轮 LHT/LIR 同期发送结果核对（扩大或收窄「通道选择性」结论）。

## Out of Scope

- LMN/SPF 传输层本身的断线检测缺陷（上游域，另案对 SPF 项目同步观察）。
- DEV-V2-24 验收阻塞项（F-A/F-B1/F-B2 已在 24 内闭环）。

## Acceptance

- 红测先行：重臂退避缝 + 限频缝（收集式断言：持续失败时 WARN 条数有界、退避间隔单调、恢复后清零）。
- 双轴独立审查 CLEAN；候选重授随 DEV-V2-24 之后的发布节奏。


## Comments

- **2026-09-09 实证升级（DEV-V2-24 v6 P2P 采集轮,主机包 112239）**：F-C 家族在本轮**大爆发**——主机单轮 `定向发送未送达（generation=2, result=LocalTransportUnavailable）` **8333 条**（Error 级直出,generation=2 死代际无退避持续重试）;同期 generation 3/4 会话正常,客户端 3 次网络整理全链成功（reqId 1/2/3）=用户面无损,但日志噪声与无谓重试量化至此。票面证据链新增:SPF 链路中途劣化场景下死代际每帧重试直至会话更替（generation 2→3 事件拍才清）。优先级建议升 P1(待用户确认)。