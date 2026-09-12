# DEV-V4-03：生命周期目标提交与空操作语义

Type: task
Status: resolved（2026-09-12 红测先行→GREEN；双轴 Round 1 全 CLEAN；构建 0/0 + 全套 7 绿；本票按 03 纪律不授候选/RELEASES/CaseId，契约仍 2.1。审计 `audit/2026-09-12/DEV-V4-03/audit-report.md`）
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: DEV-V4-01
Spec: `../spec.md`（「生命周期目标提交（V4-T4 → DEV-V4-03）」节）

## What to build

保存时面板只提交「目标启用或停用」。生命周期机按提交时的权威状态解释成一次空操作、停用、启用、恢复或拒绝。已在跑再提交启用、已用户停用再提交停用、已隔离再提交停用，均为空操作成功——现网对这些路径返回失败，本票修的是机，不是面板 if。

## Scope

- 提交面：目标状态 Enabled / Disabled，不是面板比较 FeatureState 再调用旧 SetFeatureEnabled 分支。
- 空操作成功：Running+目标启用；UserDisabled+目标停用；Isolated+目标停用（保持隔离，不走会失败的 disable，不新开代际）。
- 目标停用且正在跑 → UserDisabled。目标启用且已停用或已隔离 → 启用/恢复并新代际。
- 不允许的转换 → 失败，该意图留在草稿（配合 01 的跨源部分成功）。
- 目标差异投影不进 SDK。详情页开关外观归 05。
- 不做：扩 IFeatureRegistration；给外部插件进程级启停。

## 验收条件

- [x] 红测先行：上述三类空操作成功（相对现网失败码先红后绿）；隔离恢复产生新代际；不允许转换失败且草稿保留
- [x] ClientUi 不出现按 Isolated 写死的停用分支；全套测试 0 警告 0 错误
- [x] 官方先行消费锚：至少一条官方功能经目标提交停用再启用（新代际）
- [x] 双轴独立审查 CLEAN
- [x] **候选纪律**：不授候选、不加 RELEASES、不授 CaseId

## Comments

- 2026-09-12 关单（/implement 独立会话）：`BueFeatureStartRuntime.SetFeatureEnabled` 升格为目标状态提交——Running+启用 / 已停+停用 / Isolated+停用（保持隔离，不走会失败的 disable、不新开代际）三类空操作成功（现网 invalid-state / not-running 拒先红后绿）；停跑→UserDisabled、复停/复离→新代际不变；Starting 过渡期与未注册双向显式失败。ClientUi 零改动（无按 Isolated 的停用分支），拒绝语义经 01 草稿模型留意图并报「未保存：功能启停失败。」。审计链：R1 双轴全 CLEAN（无修复轮）。官方 LIT 锚扩展 Running 再提交启用空操作。契约仍 2.1，零公开成员入 diff。解锁 DEV-V4-04/05。
