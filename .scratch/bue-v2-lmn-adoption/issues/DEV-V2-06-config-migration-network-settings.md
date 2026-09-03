# DEV-V2-06：配置迁移（空迁移）与网络模块设置

Type: task
Status: ready-for-agent
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-04-lmn-takeover
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「配置迁移」+「成熟度定级」）

## Scope

实现空迁移（no-op）记录 + 网络模块 Settings Facet：

- 空迁移：LMN V5 无配置系统（T6 查证），迁移映射为空表。记录一次"空迁移"结果：结构化日志（`BueRuntimeLog` diagnosticId 惯例）+ 面板一行状态「无独立配置可迁移（LMN 无配置文件）」。不持久化"已做过空迁移"标记（幂等空跑）。
- 网络模块 Settings Facet：网络模块作为官方功能（核心、默认启用、玩家可关）注册一个 Settings Facet，仅含"启用/禁用"开关；不承载任何 LMN 配置迁移条目。`SettingMigrationFailed=1309` 已预留，本票不触发（空迁移无失败路径）。

## 验收条件

- [ ] 红测先行：`--bue-config-migration-red` 断言空迁移记录（日志行 + 面板状态）——先红后绿。
- [ ] 网络模块开关通过 `SettingsRuntime` + `FileSettingsPersistence` 原子提交持久化。
- [ ] 面板显示「无独立配置可迁移」行（与「已由 BUE 接管」卡联动）。
- [ ] 构建 0/0；七项目测试 PASS。

## 不做

- 不迁移任何实际配置键（LMN 无配置）；不预留未来迁移适配器接口（YAGNI，T6 Q4）。
