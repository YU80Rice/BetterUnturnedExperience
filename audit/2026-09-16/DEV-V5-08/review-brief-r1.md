# DEV-V5-08 R1 审查简报（发布工件轮）

本轮性质=**发布工件轮**（非代码轮）：src 相对源基线 `52bafa9` 零改动；被审对象=候选身份工件+全套证据+缺口台账+验收方案。实机验收与 RELEASES/publish 变更在本轮 CLEAN 之后才发生；若实机发现缺陷，改二进制→重出候选→另起补轮（先例 V3-09 F1）。

## 被审工件清单（audit/2026-09-16/DEV-V5-08/）

| 工件 | 内容 |
|---|---|
| rebuild-1..3.txt | 3× `dotnet msbuild -t:Rebuild -p:Configuration=Release` 输出，exit 0、0 警 0 错 |
| fullsuite-{Contracts,Settings,Placement,ClientUi,Network,Release,Plugin}.txt | 7 套 exe 直跑输出（exit 0 已在会话记录） |
| gate-*.txt | 6 门禁脚本输出（NoUiTokens 双目标=Core PASS + Contracts 命中既有基线） |
| candidate-sha256.txt | 候选身份：BB33C2DD…4E16B / 725504B / CaseId DEV-V5-08-CANDIDATE-20260916（开发态） |
| contracts-diff.txt | `git diff a5ed3fb..HEAD -- src/BetterUnturnedExperience.Contracts` = 空（契约 2.1 零扩面） |
| gap-ledger.md | 02..07 累计 26 项具名缺口逐条销账方案（21 实机 + 5 定性，无静默跳过） |
| acceptance-plan.md | SP S1..S9 / P2P P1..P6 / U3DS U1..U3 判据 + 用户故事映射 |

## 判据（两轴共用事实）

- 工单：`.scratch/bue-v2-phase5-official-optimization/issues/DEV-V5-08-three-env-acceptance.md`；规格：`../spec.md`（User Stories 1..26 + Implementation Decisions + Testing Decisions）。
- 前置：01..07 票 Status=resolved；`git status src/ tests/`=clean。
- 纪律：01..07 不产候选不更 RELEASES（当前行 13=AF1F50C9…=POST-P4，无 Phase-5 行）；publish/ 无第五阶段目录。
- 门禁口径先例：NoUiTokens-Contracts 命中 `ContractTypes.cs:Glazier` 为 V2-02 既有注释（`git log -S` 实证 4a37d4b 引入），04..07 各票如实双记=基线，非本阶段违规。
- 手册同步对象：spec 补充说明「玩家手册须改」六条 ↔ `docs/BetterUnturnedExperience-Player-Handbook.md` LIT/LIR 行。

## Standards 轴重点

证据链形式质量：哈希/字节数/exit 码跨文件一致；candidate-sha256.txt 声明与日志逐字可对账；台账/方案的记录纪律（具名、无静默跳过、诚实双记）；简报所述与磁盘事实相符。

## Spec 轴重点

验收覆盖忠实度：acceptance-plan 对工单验收条件五格与 spec User Stories 1..26 是否全覆盖映射；gap-ledger 26 项与 02..07 票内原文是否逐条对应（可回票面核对）；U3DS 负面不变量+权威真实是否按 ticket「U3DS 不画但权威要真」落判据；P2P「不把 Isolated 当失败」口径；「本票唯一 CaseId/候选行」纪律是否成立。
