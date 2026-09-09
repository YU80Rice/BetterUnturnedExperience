# DEV-V2-25 LIT 整理网络：定向发送失败退避 + 告警限频 + 传输持续失败面可见化

- **Status**: claimed
- **Type**: implementation
- **Priority**: P1
- **Blocked By**: —（与 DEV-V2-24 并行可开工；24 不被本票阻塞）
- **Origin**: DEV-V2-24 采集期实机发现 F-C（具名延期，票面 2026-09-08「F-B1 修复轮闭环」节）

## Background

DEV-V2-24 P2P 实机两轮捕获同案：主机 LitTidyNetService 对既成会话的定向发送持续失败，告警无限刷屏。

- 证据 A（诊断包 20260908_210433，主机）：`[TidyNet] 定向发送未送达（generation=2, result=LocalTransportUnavailable）` **1199 条**（:1301 起）。
- 证据 B（诊断包 20260908_225112，主机）：同案 **1057 条**；同轮客机包 225127:1111 显示 challenge（generation=2）**曾送达并应用**——链路中途劣化（出向帧持续被拒、断线事件不上抛），而非首拍失败。
- 传输语义：`BueNetworkRuntime.SendToClient` 对过期/异地会话返回 NoSession（可排除会话失效）；`LocalTransportUnavailable` 仅来自 `transport.Send` 返回 false（SendFrame :876）——即底层 LMN/SPF P2P 出向通道拒绝，且连接生命周期事件未触发。
- 同会话 LHT `SendToClients` 广播与 LIR 定向发送在证据 A 轮成功（证据 B 轮待查）——失败呈通道/时机选择性。

## Scope（拟修）

1. **重臂退避**：F-A 修复的「挑战发送失败→回滚采纳→下一拍重发现」环路在传输持续不可达时逐拍自旋（每循环 1 条 WARN）。沿 BueNetworkRuntime 握手重探退避先例（1s→8s）给重臂加退避。
2. **告警限频**：`LitTidyNetService.TrySendToSession`（:420）同会话代际同类失败限频（首条+每 N 条或时间窗一条），附累计计数。
3. **失败面可见化**：连续失败达到阈值时上抛一条结构性诊断（供面板/日志定位「链路劣化」状态），不静默、不炸帧。
4. 证据 B 轮 LHT/LIR 同期发送结果核对（扩大或收窄「通道选择性」结论）。

## Out of Scope

- LMN/SPF 传输层本身的断线检测缺陷（上游域，另案对 SPF 项目同步观察）。
- DEV-V2-24 验收阻塞项（F-A/F-B1/F-B2 已在 24 内闭环）。

## Acceptance

- [x] 红测先行：重臂退避缝 + 限频缝（收集式断言：持续失败时 WARN 条数有界、退避间隔单调、恢复后清零）。（观测红 red-build.log=CS0246×2+CS1729；旗标 `--bue-v2-lit-sendhealth-red` ALL GREEN 3 组；三断言对应测试=限频真值表 20 条/1000 失败+harness 2 条/55 尝试、重臂间隔 0,0,1000,2000,4000,8000,8000 单调、NoteSuccess 清零后新序列重开）
- [x] 双轴独立审查 CLEAN（R1: Standards CLEAN/Spec NOT CLEAN→F1→R2 双轴双 CLEAN，两全新实例并行零上下文）；候选重授随 DEV-V2-24 之后的发布节奏（本票不重授，下一次候选构建随入，见结单报告 §6）。


## Comments

- **2026-09-09 实证升级（DEV-V2-24 v6 P2P 采集轮,主机包 112239）**：F-C 家族在本轮**大爆发**——主机单轮 `定向发送未送达（generation=2, result=LocalTransportUnavailable）` **8333 条**（Error 级直出,generation=2 死代际无退避持续重试）;同期 generation 3/4 会话正常,客户端 3 次网络整理全链成功（reqId 1/2/3）=用户面无损,但日志噪声与无谓重试量化至此。票面证据链新增:SPF 链路中途劣化场景下死代际每帧重试直至会话更替（generation 2→3 事件拍才清）。优先级建议升 P1(待用户确认)。
- **2026-09-09 认领（ZCode）**：用户确认优先级按 P1 执行，Status=open→claimed、Priority=P2→P1。开工顺序：证据 B 轮 LHT/LIR 核对（scope 4）→ 红测先行（重臂退避缝+限频缝）→ 实现（退避/限频/失败面上抛）→ 全套测试 → 双轴独立审查 → 审计落档关单。

- **2026-09-09 Scope 4 核对完成（ZCode）**：证据 A/B 原始诊断包（210433/225112/225127，litfb7 轮机器侧采集）**未归档入库**（全库+git 全历史+游戏目录+TEMP 均无）=具名缺口，证据 B「曾送达后中途劣化」读法无法从原始数据重放。等效核对改用库内归档的 DEV-V2-24 v6/v7 P2P 主机+客机完整日志：v7 主机 gen=2 风暴（行 2492–4199 ×1650）紧贴引擎级 HOST_AUTHENTICATE_RECEIVED（行 4200，t=193.2s）终止，其后**同会话** TidyCommitted reqId 1/2/3（行 6173–6184）与 LHT 广播 epoch=2/3/4 result=Sent ×18（行 6580+）全部成功，challenge gen=2 客机应用（行 533）且会话未更替；v6 主机三个代际（2/3/4）各自诞生窗有 600/2808/3505 条风暴，challenge 最终全部送达应用（客机行 376/999/1627），TidyCommitted reqId 1/2/3 恰在 gen=2 风暴间隙成功（行 5576–5586）；v6 轮 LHT/LIR 未触发不构成同期样本（由 v7 LHT ×18 承担）。**结论：失败呈时间窗选择性（传输就绪窗/劣化窗，就绪窗边界=引擎级 P2P 认证完成拍），非按消息种类或频道的选择性**——票面「通道选择性」表述据此收窄。全量行号与引文：audit/2026-09-09/DEV-V2-25/scope4-evidence-check.md。

- **2026-09-09 关单（ZCode）**：Status=claimed→resolved，验收两条勾选。交付：LitTidySendHealth.cs（重臂退避 {0,1s,2s,4s,8s 封顶}——首重试立即位=DEV-V2-24 F-A 冻结契约；限频 首条+每累计50条 附累计=N；BUE-LIT-003 link-degraded/link-recovered 结构性诊断，episode 恰一条）+ LitTidyNetService 接入（clock 注入/退避门/DriveDueRearms 驱动/sessionEventsWired 一次性接线守卫——顺带消除 v6 日志「会话代际更替」双份行根因/丢弃与停止全清理/TrySendToSession 限频+上抛）。有效性：模拟 7 分钟持续失败窗 尝试 8333→55、WARN 8333→2、诊断恰 1 条；Scope 4 结论=时间窗选择性（证据 audit/2026-09-09/DEV-V2-25/scope4-evidence-check.md，原始包未归档缺口如实登记）。审查链：R1（Standards CLEAN 1NC+4SMELL/Spec NOT CLEAN 1GAP+1DEVIATION）→F1（V2-21 前缀恢复+Scope 4 证据交付物落档）→R2 双轴双 CLEAN（4 SMELL 同意具名延期，见结单报告 §4）。全套验证：Rebuild 0 错 0 警+7 exe 全 PASS（final-fullsuite-*.log）。候选重授随 DEV-V2-24 后发布节奏（结单报告 §6）。判词：票面 Scope 1/2/3 实现且红→绿证据链完整，Scope 4 以可复核交付物收窄结论并具名缺口，双轴 R2 终态 CLEAN，结单。

- **2026-09-09 候选 v8 重授+部署（用户拍板「构建 v8 并部署,实机验收作为 25 关单依据」,ZCode）**：上一条关单 Comment 的「不重授候选」被用户裁定推翻——理由成立：既定节奏=每票候选（14..23 先例），25 关单时按票面「随发布节奏」字面跳过该仪式，导致源码树领先于被验收发布物。**处置：Status resolved→claimed（重开），关单依据升级=实机验收轮**。仪式：三轮全方案 Rebuild 字节一致（candidate-v8-rebuild1/2/3.log，0 错 0 警）→ **候选 v8=f7b7513c569b8d2830ccdbf7bb4ab0c9e0b6abded88b2708e0a9f3b8303df569（548352B）**，CaseId=DEV-V2-25-CANDIDATE-20260909，身份链=identity-sha256.txt，取件副本=artifacts/BetterUnturnedExperience-v8.dll；源码基线=304dbd3。代部署本机 `E:\Steam\...\BepInEx\plugins\BetterUnturnedExperience.dll` 部署后哈希核验一致（deploy-fingerprint-v8.txt；部署前=v7 a1b339bf…，SPF 原位=配置 A 终态）。**v7 不作废——v8 实机验收通过且用户批准前，RELEASES 行 9 的当前发布物仍为 v7**。待办=用户 VM 客机复制 v8 覆盖（VM 现态配置 B 含 v7）→P2P 实机验收轮（主机+客机进世界、做一次整理、观察劣化窗日志形态、退出+UMM 诊断包）→agent 回收核验（预期形态：劣化窗内零星几条 WARN+至多一条 BUE-LIT-003 event=link-degraded，无逐帧风暴；无劣化窗则整轮零风暴记录）→终关单（勾票更新+RELEASES 指针随用户批准前移+结单报告 §6 落定）。

- **2026-09-09 傍晚 Scope 4 缺口补收关闭（ZCode）**：UMM 工作目录盘点（research 报告 `.scratch/bue-v2-phase2-official-adoption/research/2026-09-09-umm-diag-archive-inventory.md`）发现证据 A/B 原始包 210433/225112/225127 **完整在位**——原判「未归档」系检索遗漏 UMM 目录（当时只搜库内/git/游戏目录/TEMP），scope4-evidence-check.md §0 已如实修正。三包整包补归档至 `audit/2026-09-09/DEV-V2-25/evidence/scope4-raw/`（SHA 核对一致，含 225127:1111 缺口核心行），证据 B 读法现可从原始数据重放。同轮把 24 票引用的其余 13 份夜场/诊断轮日志（195933/195934/204042/204026/210425/000640/001648/081731/083303/083313/113740/120001/183015）按盘点建议落位补归档（哈希逐项核对）。本票 Scope 4 证据链自此完整：原始包（scope4-raw）+ 库内 v6/v7 等效核对（scope4-evidence-check.md §1-§3）双轨。
