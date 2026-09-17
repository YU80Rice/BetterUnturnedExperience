# DEV-V5-08：三环境验收与唯一对外候选

Type: task
Status: resolved（2026-09-17 用户原话「批准」关单：RELEASES 行 14 + publish 第五阶段交付包；候选 v4 F334657A…0D45；评审链 audit/2026-09-16/DEV-V5-08/review-loop.md + 结单报告.md）
Parent: spec.md（V2 第五阶段规格·现有官方功能优化定界与接线）
Blocked by: DEV-V5-01, DEV-V5-02, DEV-V5-03, DEV-V5-04, DEV-V5-05, DEV-V5-06, DEV-V5-07
Spec: `../spec.md`（实施纪律 + 测试决策 Headless/实机）

## What to build

01..07 全部关闭后，打出本阶段唯一对外候选：玩家能用行带整理、容器整理、两条恢复、后备 HUD、换弹 0～2。契约仍 2.1。单人 / P2P / U3DS 按既有三环境口径验收；U3DS 不画客户端表面，但权威行为要真。用户人工复核后才翻 RELEASES 并换发布包。

## Scope

- 三轮 Rebuild 身份稳定；全套测试绿。
- 实机：agent 代部署，用户只做编号步骤。身份锚绑定候选哈希。
- U3DS：不画整理按钮/HUD/U 菜单；容器请求、入包恢复、压弹、扣经验仍权威。
- P2P：跨端整理/恢复以会话与版本为准，不把网络模块 Isolated 当失败。
- 01..07 中间构建不得写入 RELEASES。本票是唯一 CaseId / 候选行。
- 不做：超限、锁定、扩契约、新 FeatureId。

## 验收条件

- [x] 01..07 均 resolved；工作树 src 干净（2026-09-16 核验：六票 Status=resolved，`git status src/ tests/` 空）
- [x] 全套 7/7 + Rebuild 0/0；三轮哈希一致（终态 v4 `F334657A…0D45`/726016B，`u3ds-fix-rebuild-1..3.txt`；v1/v2/v3 作废不得采集）
- [x] 双轴独立审查 CLEAN（发布工件轮）（链 R1 双 NC→修 4→R2 Spec C+Std NC（误读票面裁决驳回/歧义采纳）→修 1→R3 双 CLEAN，review-loop.md；首派空返回 1 次具名补派）
- [x] SP / P2P / U3DS 实机按规格用户故事抽检通过；U3DS 负面不变量成立（候选 v4 `F334657A…0D45`；SP 轮1–B + P2P v3/v4 + U3DS headless+权威 2026-09-17 用户确认「既有整理也有自动压弹」；过程作废 v1/v2/v3 与探针不得采集）
- [x] 用户批准后：RELEASES 新行当前发布物 + publish 第五阶段交付包；契约仍 2.1（2026-09-17 原话「批准」→ 行 14 `F334657A…0D45` + `publish/第五阶段-正式交付版本/`）

## Answer

第五阶段唯一对外候选 **v4** `F334657ABA2C6AD7CAC8E39E7A36C19FB38DCC19E60DCC575BF20D9777380D45`（726016B）。SP/P2P/U3DS 抽检通过；实机修复 F1–F11（身份链 `is null`、成交才开窗、自动轮 wireId=0、主机发起闸、容器按钮实现类型 IsVisible）。用户 2026-09-17 原话「批准」。RELEASES 行 14 当前发布物；交付包 `publish/第五阶段-正式交付版本/`。契约仍 2.1。结单 `audit/2026-09-16/DEV-V5-08/结单报告.md`。
