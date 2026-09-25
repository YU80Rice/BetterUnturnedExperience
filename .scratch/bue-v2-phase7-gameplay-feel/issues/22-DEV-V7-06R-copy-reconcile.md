# DEV-V7-06R：最终行为与玩家手册/界面文案重新对账

Type: task
Status: resolved（2026-09-25）
Parent: spec.md（V2 第七阶段规格·现有官方功能玩法手感定界与接线）
Blocked by: DEV-V7-02, DEV-V7-03, DEV-V7-04, DEV-V7-05
Spec: `../spec.md`（「文案同步（V7-T7 → DEV-V7-06）」节）
Previous ticket: [DEV-V7-06 手册与官方功能文案落地](20-DEV-V7-06-copy-handbook.md)
Red: `--bue-v7-06-copy-handbook-red`

## What to build

在 DEV-V7-02/03/04/05 修复并关单后，把玩家手册、ClientUi 对照表、Tooltip、技能行描述、设置降级表面与最终行为重新逐字对账。

本票只修文案与文案门禁，不修改 BII/LIR/LIT 的生产行为。

## Scope

必须核对：

- 总弹药 HUD 的最终文案与 V7-02 一致；
- 2 级被动压弹描述与 V7-03 一致：升到 2 级后每 8 秒扫描身上五页空/未满弹匣，不写成“双击成功后再等 8 秒”；
- 技能行名称、0/1/2 级描述、花费和满级文案与 V7-04 一致；
- 技能等级按世界/服务器、角色槽和地图分家；
- BII 绿色/红色预放置框、两个正向和文字可读性与 V7-05 一致；
- 移除所有旧的第五阶段承诺：`备匣 N · 备弹 M` 作为主 HUD；双击成功后再等待一轮自动压弹；旧的整理方向描述；旧的三种整理模式；自动旋转可选倒置方向；
- 手册、对照表、Tooltip、技能页和设置页不得各自写一套行为。

## Isolation

- 不修改整理算法；
- 不修改弹药观察或 HUD 计算；
- 不修改被动压弹调度器；
- 不修改技能账作用域或技能行布局；
- 不修改 BII 预览、ghost 生命周期、颜色或旋转；
- 不修改 SDK、公开契约、FeatureId、RELEASES 或正式交付包；
- 不在本票授候选或 CaseId。

## Acceptance

- [x] `--bue-v7-06-copy-handbook-red` 先红后绿
- [x] 手册、对照表、Tooltip、技能页和设置页逐字对账
- [x] 旧文案扫描为零
- [x] V7-02/03/04/05 的最终行为描述均有对应来源
- [x] FULLSUITE 通过
- [x] fresh Standards reviewer CLEAN
- [x] fresh Spec-Reviewer CLEAN
- [x] 本票不授候选、不改 RELEASES、不生成正式交付包

## Failure routing

如果发现的是行为不一致而不是文字错误：

- 总弹药/刷新问题 → 退回 DEV-V7-02
- 被动压弹语义问题 → 退回 DEV-V7-03
- 技能行或技能作用域问题 → 退回 DEV-V7-04
- BII ghost、绿红框或正向问题 → 退回 DEV-V7-05

不得在本票修改生产逻辑。

## Evidence

修复后追加：

- 红测首轮失败证据
- 修复后红测绿证据
- FULLSUITE 输出
- 双轴审查报告
- 文案逐字对账结果
- 最终未授候选的说明

## Answer

DEV-V7-06R 已完成最终文案对账：README、玩家手册、ClientUi 对照表、身上/容器 Tooltip、整理成功句、技能行与设置降级表面均与 V7-02/03/04/05 冻结行为一致；新增门禁覆盖总弹药、2 级每 8 秒扫描身上五页空/未满匣、技能行名称/0/1/2 描述/125/150/Full/三格锁条/主机确权及 BII 两个可读正向。红测首轮为断言红，修复后专测通过；受影响 ClientUi/Plugin 回归、V7-02/03/04/05 专测和 FULLSUITE 均通过（17 steps=16 pass、0 failed、1 known baseline，Firewall violations=0）。旧第五阶段主 HUD、双击后再等一轮、旧整理方向/三模式、自动倒置承诺在当前有效表面扫描为零；历史发布包、V5 兼容投影和研究票据保留。Standards 与 Spec 均由 fresh 实例审查为 CLEAN。本票只改文案、门禁和审计，不改 BII/LIR/LIT 生产行为；不授候选、不改 RELEASES、不授 CaseId、不生成正式交付包，候选仍仅由 DEV-V7-07 负责。详见 [`22-DEV-V7-06R-copy-reconcile.audit.md`](22-DEV-V7-06R-copy-reconcile.audit.md) 与 `audit/2026-09-22/DEV-V7-06R/`。
