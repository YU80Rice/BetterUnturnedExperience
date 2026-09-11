# DEV-V4-02：描述行、基础控件与循环切换

Type: task
Status: resolved（2026-09-11 红测先行→GREEN；双轴 Round 1 全 CLEAN；构建 0/0 + 全套 7 绿；本票按 02 纪律不授候选/RELEASES/CaseId，契约仍 2.1。审计 `audit/2026-09-11/DEV-V4-02/audit-report.md`）
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: DEV-V4-01
Spec: `../spec.md`（「描述行与循环切换（V4-T3 → DEV-V4-02）」节）

## What to build

玩家看到的配置行是显示名、描述和控件，不再是 SettingId = 值。有明确档位的离散值用循环切换（左键下一档、右键上一档、到头循环），改的是草稿。没有档位的 Choice 只读。功能级一句话仍走对照表（文案归 07）；本票把投影和控件形状接到 01 的草稿上。

## Scope

- SettingEntryView（或等价面板投影）携带 DisplayNameKey / DescriptionKey / AllowedValues / Kind；键当字面展示文本。
- 配置行：显示名 → 描述（空不画，截断 120）→ 控件。
- Cycle：Choice+非空 AllowedValues，以及外部 Unturned.Cycle / AcceptableValueList（外部完整采集与失败分类归 08；本票至少打通 BUE Choice 形状，并为外部 Cycle 留同一控件缝）。
- Toggle / Integer / Float / Text 维持形状，只补显示名和描述。KeyBinding 无专用捕获。
- 只读行不画灰掉的假控件，不进草稿。
- 不做：功能级描述进契约；ItemList 等选择器；i18n 资源表。

## 验收条件

- [x] 红测先行：Choice 有档位则 Cycle 改草稿不立刻写盘；无档位只读不降级文本框；到头循环；描述空不占位；截断 120。先红后绿
- [x] 面板不再以 SettingId = 值作为可编辑行的主标签；全套测试 0 警告 0 错误
- [x] 官方先行消费锚可延至 06（LIT 两条 Choice）；本票至少用测试夹具 Choice 穿过控件缝
- [x] 双轴独立审查 CLEAN
- [x] **候选纪律**：不授候选、不加 RELEASES、不授 CaseId

## Comments

- 2026-09-11 关单（/implement 独立会话）：交付行投影（`PanelSettingRowView`/`PanelConfigRowView`：显示名→描述[空不画·截断120]→控件形状）、Cycle 命令缝（`DraftCycleBueSetting`/`DraftCyclePluginConfig`，左+1/右-1 到头循环、改草稿不写盘、回拨不脏）、schema 联表缝（internal `IBueSettingsEditor.GetDescriptors`，三实现+三假件）、外部 Cycle/描述字段缝（`Description`/`AllowedChoices`，采集归 08）、只读行规则（无档位 Choice/ServerAuthority/CanEdit=false/Unsupported→只读文本、不画假控件、不进草稿）；原生面板两处行渲染改消费投影、`SettingId = 值` 主标签退役（源码残留扫描=0）。红测 13 组（DevV4PanelControlsTests）CS0246 驱动缝→绿；绿中修正一处测试自身断言缺陷（首/末档循环方向）。双轴 Round 1 全 CLEAN（4 项 deferrable 已命名）。解锁 05/06/07/08 的控件/文案/外部面。
