# DEV-V7-06：手册与官方功能文案落地

Type: task
Status: resolved
Parent: spec.md（V2 第七阶段规格·现有官方功能玩法手感定界与接线）
Blocked by: DEV-V7-01, DEV-V7-02, DEV-V7-03, DEV-V7-04, DEV-V7-05
Spec: `../spec.md`（「文案同步（V7-T7 → DEV-V7-06）」节）
Red: `--bue-v7-06-copy-handbook-red`

## What to build

玩家手册功能一览、管理面板三句说明、整理按钮悬停、整理成功句、换弹技能描述，与已经做完的行为说的是同一件事。面板不再画「整理方向」。

## Scope

- 按规格逐字落地手册三行、对照表三句、身上/容器 Tooltip、成功句、技能三句。
- 从面板描述符移除整理方向；旧值仍可读。
- 可用脚本门禁对齐冻结句。网络 / 尸潮 / NoOp 对照表不改。
- 手册可写「换弹技能等级按世界、角色槽位分别保存」。无游戏内弹窗。

## 隔离

- **不得**为了迁就旧文案去改运行逻辑。
- **不得**重新定义技能等级、HUD 公式、预览颜色或整理算法。
- **不得**为手册表达新增兼容层或第二套行为。

## 验收条件

- [x] 红测先行：手册三行、对照表三句、Tooltip、成功句、技能描述与规格冻结句逐字一致；direction 描述符不再作为有效 Choice。先红后绿。组名 `--bue-v7-06-copy-handbook-red`
- [x] 官方先行：生产路径文案（对照表/Tooltip/技能行）与手册同一语义
- [x] 双轴独立审查 CLEAN
- [x] **候选纪律**：不授候选 / RELEASES / CaseId

## Answer

已完成 V7-06。玩家手册三行、BII/LIT/LIR 对照表句、身上与容器 Tooltip、整理成功玩家提示、换弹技能三句及设置降级表面均按 V7-T7 同源同步；`direction` 旧值仍可读但不再登记为有效描述符。新增红测 `--bue-v7-06-copy-handbook-red` 先红后绿，并纳入 Plugin 完整回归。

审计：[`20-DEV-V7-06-copy-handbook.audit.md`](20-DEV-V7-06-copy-handbook.audit.md)。最终双轴：Standards CLEAN；Spec 最终轮无剩余阻塞（技能完整句争议经 spec.md:114–117 逐字核验关闭）。本票不授候选、不改 RELEASES、不授 CaseId。

