# V3-R2 诊断包自动化现状研究

- **Ticket**: V3-R2
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

V3-T8 BueDiagnostics 的唯一前置事实输入（只阻塞 T8，不扩大为整个发布系统重做）。产出报告 `.scratch/bue-v2-phase3-platform/research/2026-09-09-V3-R2-diagnostics-automation.md`。

研究范围：

- `docs/agents/auto-rm-test-sop.md`（现行自动回收 SOP）
- `.scratch/bue-v2-phase2-official-adoption/research/2026-09-09-umm-diag-archive-inventory.md`（UMM 诊断包归档清单）
- `BueRuntimeLog` / `DiagnosticLogSink` 现行面（采集、结构化行、红测锚点）
- 现行证据链机制：诊断包采集→归档→命名→哈希→CaseId→RELEASES 绑定（`audit/` 目录惯例 + DEV-V2-24/25 结单为样本）
- 逐步列出：哪些环节已自动化、哪些仍依赖人工（会话内操作、文件回收、哈希计算、归档登记）
- 结论：自动化做到哪一层才有实际收益（候选层：一键采集 / 会话内自动留档 / 采集器 API），每层收益与代价——**不带决策倾向，决策归 V3-T8**。

只查证不改码；结论带 file:line / 证据包路径锚点。

## Answer

BUE 没有 `BueDiagnostics` 实现，也没有把诊断包写入磁盘的生产代码。现行自动化停在两处：运行时结构化日志（`BueRuntimeLog` + 多处 internal `DiagnosticLogSink`，红测钉行不钉包）以及 U3DS SOP 的 `cp LogOutput.log`（`auto-evidence/` 在 24 关单为空）。SP/P2P 主链仍是用户 UMM 导出 + 事后人工/Agent 拷进 `audit/`；CaseId 与 RELEASES 始终人工。三层对照：①一键采集已有 UMM/SOP 对照物但未接到 git 树；②会话内自动留档源码与 SOP 均未出现；③采集器 API 仅有未接线的 `IFeatureLogger`（生产传 null）。完整报告：[2026-09-09-V3-R2-diagnostics-automation.md](../research/2026-09-09-V3-R2-diagnostics-automation.md)。

## Comments
