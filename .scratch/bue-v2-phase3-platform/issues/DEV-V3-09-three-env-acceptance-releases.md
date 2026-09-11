# DEV-V3-09：三环境验收与发布（唯一 2.1 候选+RELEASES 行+publish 换新）

Type: task
Status: resolved（2026-09-11，三环境实机验收全判据过 + 用户人工发布批准「批准发布！」；双轴多轮 CLEAN；当前发布物=RELEASES 行 11 候选 `ce0d2191…17e413`；审计=audit/2026-09-11/DEV-V3-09/结单报告.md；Phase-3 DEV-V3-01..09 全 resolved）
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-08（SDK 附录总装）
Spec: `../spec.md`（Further Notes 候选策略+「契约版本」节）

## What to build

Phase-3 全部平台缝以单一对外版本交付：唯一的 `2.1` 候选 DLL 经单机/P2P/U3DS 三环境实机验收全判据通过，SHA-256/CaseId 绑定候选身份，人工批准后加 RELEASES 行，publish 正式交付包同步换新（DLL+SDK 契约文档+交付说明）。这是 Phase-3 唯一产生候选身份与 RELEASES 行的票。

## Scope

- 契约版本合批生效：T2..T8 的 Minor 加性变更单一批次合入 **2.1**（若实施中实际分批则按 Minor 顺延 2.2——分批是允许的实施计划）；宿主门槛 `SupportedContractMajor=2` 不动；2.0 模块继续可注册回归。
- 三环境实机验收（判据按各平台缝冻结语义逐条核对：白名单拒绝/事件归属/生命周期启停与隔离/发送 Throttled/链路健康/HostTick/Settings 双 scope/诊断摘要/Logger 行）。
- 候选身份：`-t:Rebuild` 候选 DLL + assembly-identity SHA-256 绑定 + CaseId + 人工批准 → RELEASES 行 + publish 交付包换新（v8 2.0 基线包退役归档）。
- 全套测试 7 个测试工程 exe 直跑全 PASS、0 警告 0 错误。
- 实机部署/清目录/cfg/指纹/日志回收 agent 代办，用户只做游戏内操作（指引压缩成编号步骤）。

## 验收条件

- [x] 三环境验收全判据过（每环境记录结构化诊断与摘要证据，绑定候选 SHA-256）——**三环境全 PASS**：SP（`UMM-…125827`，§4b）／P2P 双端（`134947`/`135012`，跨端 TidyCommitted=13+RepackSuccess=1，`result=posted`=1，§4c）／U3DS 带 Steam+客机（`140158`，runtime `armed`、跨端 TidyCommitted=21+RepackSuccess=2、`result=posted`=2 真实负载证 F1、Throttled/link-degraded/BUE`[Error]`全 0，§4d）；三端双端 identity 均绑候选 `CE0D2191…`；bue.network 模块隔离经三环境+v7/v8 基线逐字比对=通用良性投影（功能经 runtime 在线），非回归
- [x] 全套测试 7 exe 直跑全 PASS；0 警告 0 错误
- [x] 唯一 `2.1` 候选身份确立：SHA-256 `ce0d2191…17e413`/CaseId `DEV-V3-09-CANDIDATE-20260911` 绑定（3× 确定性重建），**RELEASES 行 11 已加入**（注明 Phase-3 平台缝 Minor 批次 T2..T8），**人工批准记录在案**（2026-09-11「批准发布！」）
- [x] publish 正式交付包同步换新：`publish/第3阶段-正式交付版本/`（DLL+SDK 2.1 附录版契约文档+玩家手册+交付说明）；SDK 文档随主 DLL 版本走不独立发版；v8 2.0 基线包退役归档
- [x] 双轴独立审查（每轮全新实例）CLEAN；2.0 模块兼容回归绿——doc §7 翻转 + F1 日志风暴修复 两轮双轴 CLEAN；2.0 回归=五官方功能实机 `accepted=True` + 门槛 `SupportedContractMinor=1` 代码锚

## Comments

2026-09-11 /implement 会话（红先行→双轴 CLEAN→三环境实机·停等人工验收）：

- **契约版本合批生效**：SDK 正文 §7 版本活账登记 2.1（宿主支持契约版本，机器事实，代码门槛 `SupportedContractMinor=1` 自 01 起成立），新增 `### 2.1` 单一批次加性小节（T2..T8 按票构成 + §C.3 加性四类理由 + 发布绑定）；§C.1 记「翻转执行记录」并区分「宿主支持 2.1（今）」vs「对外发布 2.1（本票候选验收+批准后同提交随 RELEASES/publish 翻转）」。实现者侧 `IFeatureRegistration` 成员一个未加（恰 4 属性）；宿主侧 `IFeatureBootstrap`/`IFeatureLifetime` 加成员对 2.0 零破坏（读取方向+矩阵 null 容忍）。**破坏性变更为零，Major=2 不动。**
- **实机发现日志风暴并修复（F1）**：U3DS headless 实机首验即抓到 `LogOutput.log` ~63 行/秒风暴（`[BUE-MT] result=posted BUE-MT-ACCEPT` 全为 LIR）。根因=DEV-V3-04 dispatcher 迁移新引入：`InPlaceReloadModule.OnHostTick` 每帧无条件 `Drain()`→空队列仍 `seam.Post(DrainOnce)`→`MainThreadDispatcherRuntime` 每投递 emit 一行 Debug。F1 修复（LIR 内部，不触碰 2.1 dispatcher 冻结面）=加 `LirRepackDispatcher.HasPendingWork`（锁内读 requests/successes 计数），`Drain()` 仅在有实际待办时投递。红测 `NetworkV3GroupLirIdleDrainNoPost`（空闲多拍→0 投递／有工作→恰 1 投递+泵拍执行／排空→转静默）先红后绿。修复后 U3DS 重验：`result=posted=0`（对拍窗口 0 增长）、LIR `to=Running`+泵心跳在场（证 0=修复非停滞）、身份绑 `ce0d2191`、5/5 官方功能 accepted、0 `[Error`。
- **候选身份（开发态，待批准定身）**：`ce0d219179e036c3cacf6924e77df2ce0a93a0a08b4bb73526ab46565a17e413`（601600 B，3× `-t:Rebuild` 逐字节一致，0 警 0 错）。前序候选 `471eb245…658fb0` 因 F1 修复作废（见 auto-evidence/u3ds/FINDING-lir-mt-post-flood.md）。CaseId `DEV-V3-09-CANDIDATE-20260911`。双端 `certutil` 指纹=候选值（client 新增／U3DS 替换 v7 `a1b339bf`）。
- **双轴链（每轮全新实例，F1 首版 R1 因网关连接超时作废重派）**：doc §7 首轮 R1-Spec 发现「既有 interface 成员一个未加」措辞偏差（宿主侧确加了成员，仅实现者侧未加）→修→R2 双 CLEAN；F1 代码+doc 定时措辞 R1-Spec CLEAN on F1／1 gap（§7 勿把 2.1 当作已发布，RELEASES 仍 v8）→改「宿主支持 vs 对外发布」分离→R2 双 CLEAN。全套 7/7+全方案 Rebuild 0/0。
- **停等边界（auto-rm-test-sop 步 10，不 resolved/不关票/不动 RELEASES 批准态）**：① U3DS 网络缝完整验收（需 Steam 登录，当前 headless 无 Steam 网络功能正确隔离）；② SP 单人实机（客户端自动化为 SOP 已知缺口）；③ P2P 双端（需对端）；④ 三环境全判据 + 人工发布批准 → 同提交加 RELEASES 行 + publish 交付包换新（v8 归档）+ §7 发布态翻转闭环。用户编号验收指引见结单报告 §验收指引。
- **证据**：`audit/2026-09-11/DEV-V3-09/`（`candidate-sha256-final.txt` 3× 确定性／pre-fix 候选 `candidate-sha256.txt` 作废留档／`auto-evidence/u3ds/`：`FINDING-lir-mt-post-flood.md`+`FINDING-mt-post-flood-u3ds-20260911.log`（红证据）+`clean-boot-fixed-471eb245_to_ce0d2191-20260911.log`+`clean-boot-tally.txt`+`anchor-tally.txt`／`结单报告.md`）。log/diff 磁盘归档，报告+全套 txt 入库。
