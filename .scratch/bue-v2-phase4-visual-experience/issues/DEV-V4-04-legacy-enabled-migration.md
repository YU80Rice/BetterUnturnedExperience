# DEV-V4-04：官方 legacy enabled 迁移 adapter

Type: task
Status: resolved（2026-09-12 红测先行（运行时红×2+编译红）→GREEN；双轴审查四轮闭环：Standards 轮1–4 CLEAN、Spec 轮4 CLEAN（前三轮 5 项 blocking 全修复）；构建 0/0 + 全套 7 绿；本票按纪律不授候选/RELEASES/CaseId，契约仍 2.1。审计 `audit/2026-09-12/DEV-V4-04/audit-report.md`）
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: DEV-V4-03
Spec: `../spec.md`（「官方 legacy enabled 迁移（V4-T4 → DEV-V4-04）」节）

## What to build

升级后，曾用设置里的 enabled 当总开关的官方功能，旧磁盘 `enabled=false` 变成生命周期上的用户停用，设置页不再出现这条总开关。玩家不会看到两套开，也不会因为丢掉旧值而在升级后功能又跑起来。

## Scope

- 仅显式登记的 legacy lifecycle alias：LIT / LIR / LHT / Network / v1compat 的旧 `*.enabled`，以及 BII 的 `Enabled`。
- 不登记：BII AutoRotate、NoOp `noop.probe-toggle`、未声明 alias、生态功能。
- 旧值 false → 写入 UserDisabled 意图事实，再由 03 的生命周期机解释是否实际停用（Isolated 上不盲调 disable）。true 或不存在 → 不额外改生命周期。已有新权威则以新为准。
- 幂等；成功写入新权威前不得丢旧值；成功后旧字段从 schema 与面板退役。不按字段名扫描。
- 不做：把任意名为 enabled 的设置当生命周期；生态强制迁移。

## 验收条件

- [ ] 红测先行：六项 alias 的 false→UserDisabled 幂等；重复加载不重复代际；未声明 enabled 不迁；成功前旧值仍在；成功后面板/descriptor 不再暴露该总开关
- [ ] AutoRotate 与 noop.probe-toggle 仍是普通设置；全套测试 0 警告 0 错误
- [ ] 官方先行消费锚：LIT 旧 enabled=false 升级后为 UserDisabled 且设置页无 enabled 行
- [ ] 双轴独立审查 CLEAN
- [ ] **候选纪律**：不授候选、不加 RELEASES、不授 CaseId

## Comments

- 2026-09-12 关单：红测先行（运行时红×2+编译红）→ 实现 → 全套 7 工程 0 警 0 错 7/7 绿 + static gates 全过。双轴四轮：Standards 轮1–4 CLEAN、Spec 轮4 CLEAN（前 3 轮 5 项 blocking 全修复：BII 读源改文档同构+e2e、六项 false 收口、意图写失败与退役写失败两条真实文档路径的旧值保全）。审计 `audit/2026-09-12/DEV-V4-04/audit-report.md`。
- 移交 DEV-V4-05：network/v1compat 良性隔离冻结 → 停用意图暂无机内清除路径，05 需落良性隔离功能的启用面（机外补清缝或重审 Module.Start 语义）。
