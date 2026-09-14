# DEV-V5-01：给人看的开发手册入口

Type: task
Status: claimed
Parent: spec.md（V2 第五阶段规格·现有官方功能优化定界与接线）
Blocked by: None (can start immediately)
Spec: `../spec.md`（「给人看的开发手册（V5-T2 → DEV-V5-01）」节 + 共享规则）

## What to build

第一次写生态插件的人打开仓库时，先读 `docs/developer/` 的一图和三短章，再被带到 SDK 与 NoOp，而不是掉进过期 `.scratch` 长文。玩家手册仍只管安装。契约唯一事实源仍是 SDK。

## Scope

- 新建 `docs/developer/`：入口页 + 手册。一张总图 + 三短章（模块结构、最小接入流程、NoOp 导读）。不进 `docs/sdk/`。
- README「给生态开发者」先链该入口，再链 SDK 与 NoOp。
- 范例只导读现有 NoOp，不复制第二套接入代码。与 SDK 冲突以 SDK 为准。
- 官方四件只点名「在主 DLL 内」。不写排版算法、锁定、换弹技能。
- 会抢权威的 `.scratch` 长文加 SUPERSEDED / 历史资料标，不删文件。
- 不做：扩契约；整本重写 SDK；把玩家手册和开发手册合并；本票改生产功能代码。

## 验收条件

- [ ] 红测先行：手册/入口存在；不复制 SDK 码表；链到 SDK 与 NoOp；点名的过期 `.scratch` 带历史标。先红后绿（文档门禁即可）
- [ ] 官方先行消费：README 开发者入口指向新目录
- [ ] 双轴独立审查（standards-reviewer / Spec-Reviewer，每轮全新实例）CLEAN
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId
- [ ] 用户目视可外发后，第三方评审由用户另请；评审不挡 02..08 开工
