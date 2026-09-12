# DEV-V4-05：功能级启停详情页表面

Type: task
Status: resolved（2026-09-12 红测先行（编译红 CS1061/CS1729×2）→GREEN；双轴审查两轮闭环：Standards 两轮 CLEAN、Spec 轮2 CLEAN（轮1 F1「开关门禁按状态枚举而非 seam 所有权」修复=HasStoppableLifecycle 显式所有权事实+组合根 TryReadMachineLifecycleFacts 单点喂入）；构建 0/0 + 全套 7 绿；官方先行消费锚①=ClientUi 模型链+Plugin.Tests 组合级两组；本票按纪律不授候选/RELEASES/CaseId，契约仍 2.1。审计 `audit/2026-09-12/DEV-V4-05/audit-report.md`）
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: DEV-V4-02, DEV-V4-03, DEV-V4-04
Spec: `../spec.md`（「功能级启停表面（V4-T4 → DEV-V4-05）」节）

## What to build

玩家在 BUE 功能详情页看到一颗「启用」开关，表示保存后的目标状态；当前功能状态仍是只读中文。改开关进草稿，点「保存配置」才交给 03 的目标提交。外部插件和管理面板自身没有这颗开关。

## Scope

- 开关 = 保存后目标，进 01 草稿。与 FeatureState 不一致时 ClientUi 提示待生效，不承诺一定成功。
- 有开关 iff 可停止生命周期 seam：官方、生态、NoOp、Network、v1compat。不显示：外部 BepInEx 插件、管理面板自身、核心 Host/Contracts。
- 状态投影：Running=运行中；Disabled/Stopped（UserDisabled）=已停用；Isolated=已隔离；Starting=启动中；Stopping=停用中；Isolating=隔离处理中；Discovered=待启动；Incompatible 与未映射=不可用（无开关）。表现状态独立一行。隔离原因有值才显示。
- `bue.network` 良性隔离文案本票不改。
- 不做：立即启用按钮；目标差异进 SDK；外部插件进程级启停。

## 验收条件

- [ ] 红测先行：有开关 iff 可停止 seam；九态中文映射（待启动 ≠ 启动中）；草稿目标与只读状态分离；保存走 03 目标提交而非面板 if
- [ ] 外部插件详情无启停开关；全套测试 0 警告 0 错误
- [ ] 官方先行消费锚：面板自身完成草稿+保存+功能级启停（T1 检验点 ①）
- [ ] 双轴独立审查 CLEAN
- [ ] **候选纪律**：不授候选、不加 RELEASES、不授 CaseId

## Comments
