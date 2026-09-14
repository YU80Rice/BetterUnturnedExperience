# DEV-V5-01：给人看的开发手册入口

Type: task
Status: resolved
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

- [x] 红测先行：手册/入口存在；不复制 SDK 码表；链到 SDK 与 NoOp；点名的过期 `.scratch` 带历史标。先红后绿（文档门禁即可）
- [x] 官方先行消费：README 开发者入口指向新目录
- [x] 双轴独立审查（standards-reviewer / Spec-Reviewer，每轮全新实例）CLEAN
- [x] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId
- [ ] 用户目视可外发后，第三方评审由用户另请；评审不挡 02..08 开工（→ 交用户：评审包=`docs/developer/`+README 开发者入口）

## Answer

2026-09-14 交付并关单（实施会话 /implement，基线 `e4894ac`）。

**交付**：`docs/developer/README.md`（入口页）+ `docs/developer/BetterUnturnedExperience-Developer-Handbook.md`（一张总图+三短章：模块结构/最小接入流程/NoOp 范例导读）；根 README「给生态开发者」开发手册条目置于 SDK 与 NoOp 之前；点名六份抢权威 `.scratch` 长文（V1 双语 spec 对、Shared-Contract、生命周期隔离、开放运行时双语对）加 SUPERSEDED/历史资料横幅并双指针（SDK+新手册），正文零删改。

**红绿链**：新文档门禁 `eng/Verify-DeveloperHandbook.ps1`（存在/尺寸帽/形态锚/SDK+NoOp 续链/码表·票号·版本账禁用/权威声明/README 顺序/横幅双指针）先红 34 项→实现后绿；四项突变各证红各还原（M1 顺序/M2 横幅/M3 码表 token/M4 链接断）；src/tests 零改动，终态 Rebuild 0/0 + 7 套件全绿。

**双轴**：R1 双全新实例即 CLEAN（Standards 无硬违反；Spec 无 gap/deviation）。具名递延两条判断性气味=门禁行数硬帽数值自设（一图三短章的机器化上界，数值可待第三方评审反馈再议）、双份「以 SDK 为准」声明（T2 Q1 对入口与手册分别要求，有意非漂移）。闭环链 `audit/2026-09-14/DEV-V5-01/review-loop.md`。

**提交**：685ca31（第五阶段开图工件补录入库）+ 81e2dfc（本票实施）+ 本关单提交。**候选纪律**：不产候选、不更 RELEASES、不授 CaseId（diff 实证无 publish//RELEASES/DLL）。

**下站**：01 已闭，前沿=DEV-V5-02（排版模块）/06/07 可开工；第三方评审由用户外请、不挡实施。
