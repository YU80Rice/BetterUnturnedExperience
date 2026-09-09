# V3-R1 平台公开面现状盘点

- **Ticket**: V3-R1
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

所有平台决策票（T2..T7、T9）的共同事实输入。对 BUE 现行代码与文档做公开面盘点，产出分类清单报告（`.scratch/bue-v2-phase3-platform/research/2026-09-09-V3-R1-platform-surface-inventory.md`）。

盘点对象：

- `BueRuntimeHost.Register`（公开注册桥，SCR-GPT18-001）
- `BueNetworkApi` / `IFeatureBootstrap.Network`
- `IFeatureBootstrap` 全面
- 功能事件（OwnedEvents / TidyCompleted / 宿主时钟发布）
- 宿主时钟（HostTick）
- Settings（BUE 设置持久化与面板编辑面）
- Diagnostics（BueRuntimeLog / DiagnosticLogSink / BUE-PLATFORM-001 防双装诊断）
- Failure isolation（功能级隔离 / CoreSafeMode）
- NoOpFixture（`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs` 生态路径活样板）
- Contracts 程序集（`src/BetterUnturnedExperience.Contracts/`）冻结面清单

输出必须逐项区分六态（2026-09-09 用户定）：

```text
已公开 / 内部可见 / 官方功能私有 / 生态可调用 / 已实现 / 静态存在但运行未证实
```

附加事实基线（喂 V3-T9）：

- Contracts 拆分四条件（T7 决策 2：①第三方需脱离完整 BUE DLL 编译 ②多仓库需稳定纯契约包 ③runtime 与 SDK 发布节奏须独立 ④需公开桥接 adapter 而不暴露主程序集）逐条的现行事实证据；
- NoOpFixture 之外是否存在真实第三方需求信号（issue / 结单 / 用户表态里找）。

只查证不改码、不改契约；结论带 file:line 证据锚点。

## Answer

公开注册桥 `BueRuntimeHost.Register` 已实现且官方/NoOp 同桥，DEV-V2-24 改名实机 `accepted=True`。`IFeatureBootstrap` 生产只接线 Network/Events/OwnedEvents/LifecycleGeneration，Settings/Logger/Dependencies/Lifetime 传 null（Logger 无实现类）。TidyCompleted/HostTick 契约+总线+官方消费已实现，生态独立 DLL 实机消费未找到；NoOp 的 Start 返回 `Started=false` 不练这些缝。设置与诊断走官方私有 `SettingsRuntime`/`BueRuntimeLog`/多处内部 DiagnosticLogSink，面板路由写死官方 FeatureId；`EnterCoreSafeMode` 生产未调用。Contracts 嵌入主 DLL，四条件均无触发实例；NoOpFixture 之外未找到真实第三方需求信号。报告：[2026-09-09-V3-R1-platform-surface-inventory.md](../research/2026-09-09-V3-R1-platform-surface-inventory.md)。

## Comments
