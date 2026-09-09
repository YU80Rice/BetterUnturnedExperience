# LaunchHordeTracker（更好的尸潮播报）来源与署名声明

## 来源

- 项目：`LaunchHordeTracker`（Launch 系列三插件之一）
- 开源仓库：https://github.com/YU80Rice/LaunchHordeTracker
- 作者署名：`YU80Rice`
- 许可证：MIT License（与 BUE 本仓库 LICENSE 一致）

## 纳入方式

2026-09-08，DEV-V2-20（V2 第二阶段·LHT 官方纳入）将尸潮追踪领域源码迁入 BUE：信标两 Postfix 与权威追踪器核心语义原样保留（epoch/seq/单槽 mailbox/脏标记/双可靠度/停止闸门）；传输从自有 ModTransport 上收为 BueNetworkApi `SendToClients` 会话驱动组播（BUE 帧承载）；`/horde` 指令走原版 ChatManager 守门不复制权限；表现层为 10Hz 宿主时钟 HUD（`HordePresentationAdapter`，HUD 故障只降表现不伤追踪）。运行时不依赖 `LaunchHordeTracker.dll`，也不复用其 BepInEx 插件身份；功能身份为 `io.github.yu80rice.bue.horde-tracker`（面板条目「更好的尸潮播报」）。

## 致谢

感谢 `YU80Rice` 编写并验收 LaunchHordeTracker 的尸潮权威追踪与 HUD 实时播报实现（epoch/序号/可靠度语义），BUE 官方纳入直接吸收该实现。
