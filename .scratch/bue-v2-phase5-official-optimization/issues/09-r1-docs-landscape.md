# V5-R1 开发文档读者分层现状盘点

- **Ticket**: V5-R1
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T2 的事实输入。对照仓库现有文档产出现状报告（`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R1-docs-landscape.md`）。

必须回答：

1. 玩家手册、SDK、CONTEXT.md、AGENTS.md、`docs/adr/`、各阶段 `.scratch/*/spec.md`、架构目录 `.scratch/better-unturned-experience-architecture/` 各自的读者声明（文件头原话）与实际长度量级。
2. 生态作者今天要接入 BUE，按规定应读哪一份？是否存在第二份「开发规格」与 SDK 抢权威？
3. 有没有已经存在的「一图流 / one-pager / 模块生命周期图」？最接近的短文是哪几份。
4. 生命周期与接入步骤今天散落在哪些章节（SDK 附录 A、Phase-3 spec、NoOpFixture 注释、玩家手册）——列路径+标题，不要抄全文。
5. 被人类开发者抱怨的「雷霆长文」最可能指向哪几份（按行数/是否双语/是否把决策过程写进给人看的正文）。

只查证不改文档。结论带 file:line 或章节标题。

## Answer

给人看的开发手册今天是空槽（只有 `CONTEXT.md` L169–171 词条）；四层里实际有文件的是玩家手册 54 行、SDK 641 行、各阶段 `spec.md`（ready-for-agent）。生态作者按规定只读 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` + NoOpFixture——契约层无第二 `docs/sdk/`；抢权威的是 `.scratch` 未退役规格（V1 双语 spec、Shared-Contract 862 行、V1 生命周期 spec）。一图流/生命周期图未交付；最接近短入口是 README「给生态开发者」+ SDK §1 ASCII 链。接入步骤散落 SDK §1/§4/A.1/A.3/C.6、Phase-3 spec「生命周期」、NoOpFixture 注释、玩家手册仅部署。「雷霆长文」优先嫌疑：V1 `spec.md`/`spec.zh-CN.md`、SDK 账本、Shared-Contract、Phase-3 spec。

报告：`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R1-docs-landscape.md`
