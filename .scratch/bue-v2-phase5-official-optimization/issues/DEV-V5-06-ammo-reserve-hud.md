# DEV-V5-06：弹药后备 HUD

Type: task
Status: resolved（2026-09-16 双轴评审链闭合：R1 Standards 0硬/6气味（1 驳回=git HEAD 证评审误读、2/4 具名递延随 08、3/6 修、5 免记）+ Spec CLEAN（8 判据零缺口）；R2 Standards 0硬/2 低权重具名 + Spec CLEAN（重启补派，采纳缺口补记第 6 条；阻断过程具名）。评审链 audit/2026-09-16/DEV-V5-06/review-loop.md）
Parent: spec.md（V2 第五阶段规格·现有官方功能优化定界与接线）
Blocked by: DEV-V5-01
Spec: `../spec.md`（「弹药 HUD 与换弹技能 0～2」HUD 段）

## What to build

更好的换弹体验启用时，原版右下角「当前/上限」旁边能看到有子弹的备用匣本数和后备总发数。停用该功能则不画。U3DS 不画。

## Scope

- 只扩展原版弹药信息区。不改枪模小字。不与尸潮条抢顶栏。
- N = 身上五页、口径匹配、amount>0 的备用匣本数，不含枪上那本、空匣、容器、地面。
- M = 这些匣余弹 + 能经 FillTargetItem 给当前匣供弹的箱，匹配同现网压弹。
- 进度/技能等级归 07。本票不画技能 UI，不改双击 R。
- 不扩契约。不新 FeatureId。

## 验收条件

- [x] 红测先行：N/M 定义（不含枪上匣、不含空匣、不含容器）；功能停不画。先红后绿（CS0246 编译红→五组 ALL GREEN；突变 M1–M8 各证红含构建退出码核验重建）
- [x] 官方先行消费：真实持枪时投影走更好的换弹体验（updateInfo postfix→LIR 登记闸→注入观察→纯投影→注入呈现；匹配与现网压弹机械同源=CollectCompatibleAmmoIds/单源谓词共用）
- [x] 双轴独立审查 CLEAN（R1+R2 全链，audit/2026-09-16/DEV-V5-06/review-loop.md）
- [x] **候选纪律**：不授候选 / RELEASES / CaseId（diff 核对无 publish/RELEASES/CaseId 痕迹）
