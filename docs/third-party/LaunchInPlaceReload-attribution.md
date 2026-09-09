# LaunchInPlaceReload（更好的换弹体验）来源与署名声明

## 来源

- 项目：`LaunchInPlaceReload`（Launch 系列三插件之一）
- 开源仓库：https://github.com/YU80Rice/LaunchInPlaceReload
- 作者署名：`YU80Rice`
- 许可证：MIT License（与 BUE 本仓库 LICENSE 一致）

## 纳入方式

2026-09-08，DEV-V2-22（V2 第二阶段·LIR 官方纳入）将压弹领域源码迁入 BUE：`AmmoRepackService` 事务引擎原样迁移；宿主身份按本仓库纪律重写——原三处补丁改由 `ReloadContextGuard` 值缝与 `IReloadAction` adapter 缝承载（Harmony ID=FeatureId，Stop 只 UnpatchSelf），以 `TidyCompletedConsumer` 消费整理完成功能事件、`HostTick` 宿主时钟驱动双击检测与会话簿。运行时不依赖 `LaunchInPlaceReload.dll`，也不复用其 BepInEx 插件身份；功能身份为 `io.github.yu80rice.bue.in-place-reload`（面板条目「更好的换弹体验」）。旧命名频道随原插件退役，联机传输形态由 `IBueNetworkApi` 公开契约重建（`LirRepackWireCodec`，FeatureId 即频道）。

## 致谢

感谢 `YU80Rice` 编写并验收 LaunchInPlaceReload 的一键压弹事务实现（压弹事务引擎/双击检测/容量与幂等语义），BUE 官方纳入直接吸收该实现。
