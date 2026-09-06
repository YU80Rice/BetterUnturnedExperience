# DEV-V2-08：LIT/LIR/LHT 生态迁移验证

Type: task
Status: claimed（2026-09-05，agent）
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-07-three-env-network-validation
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「生态与前置」）

## Scope

完成 LIT/LIR/LHT 的 V2 迁移验证闭环（T8 修正：源码已迁 V2 但从未测试、归档未闭环区）：

- 对 LaunchInventoryTidy（v3.0.1）、LaunchInPlaceReload（v3.0.0）、LaunchHordeTracker（v3.0.0）三个已迁 V2 源码（`Archive\2-未闭环验证项目`）逐个：编译、部署到 BUE 环境、实机验证其网络功能（命名频道收发）在 BUE 网络模块下正常。
- 确认三个插件作为 BUE 官方功能（吃掉消化）工作：不再需要独立 LMN DLL，走 BUE 注册/生命周期/隔离路径。
- 验证记录：每个插件一个 CaseId，绑定 V2 网络层 DLL 的 LoadSetIdentity。
- 若发现 V2 迁移缺陷（如 API 面不匹配、会话语义差异），记录为修复票（属于各自官方纳入实施，不在本票内修）。

## 验收条件

- [ ] 三个插件在 BUE 网络模块下实机收发正常（或记录明确缺陷 + 修复票）。
- [ ] 迁移验证闭环：V1 兼容层"已知生态"维度的验证义务解除（T4 Q4）。
- [ ] 证据/日志归档到 `.scratch/bue-v2-lmn-adoption/` 或 `audit/`。

## 不做

- 不修改三个插件源码（发现缺陷另立修复票）；不重新实现它们的业务功能。

## Comments

> 2026-09-05 认领（agent）：前置调查结论——(1) 三插件归档源码均为**验收候选构建 0.0.0**（各自 CHANGELOG：正式版本号待三环境验收通过才授予；票面 v3.0.1/v3.0.0/v3.0.0 指已冻结的历史发布线身份）；(2) 三插件均 `[BepInDependency(LMN, HardDependency)]` 且直接调用 `ModTransport.RegisterNamed*/SendNamed*`（LMN V2 命名频道 API），**运行时必须部署 LaunchMultiplayerNet.dll**——票面 Scope「不再需要独立 LMN DLL，走 BUE 注册/生命周期/隔离路径」与「不做：不修改三个插件源码」**票内互斥、本票不可验证**，处置见下方 R1 评论（具名缺口，不作为本票验收项；官方纳入终态另立票承接）；本票验收以票面「验收条件」三条为准。(3) 证据绑定 RELEASES 行 6（`35670269…aef6`），BUE 源码零修改、无新候选。计划：编译三插件 → handoff + 实机手册（按 07 先例）→ 双轴审查（standards-reviewer/Spec-Reviewer）→ 带用户实机验证。（R2-Spec 指出本评论初版曾把该条款写成「shim 口径执行」的语义改写——已订正为上述缺口表述，初版表述作废。）

> 2026-09-05 交付 + R1（agent）：交付 kit 五件 + 手册 + 交付报告落盘 `audit/2026-09-05/DEV-V2-08/`（三插件 Release 重建 0 警 0 错、相邻两轮逐字节一致；BUE=行 6 复用，LMN=权威 v5 哈希与 07 记录一致）。**双轴 R1 双双 FINDINGS，全部文档侧已修**：Standards [M]×4+[L]×2（LIR 汇总窗口 5s 非 15s、session challenge 实际日志行、U3DS /horde admin 门、LHT 显示名更正、确定性留证口径收窄、Libs 件数）+ J1–J4 具名处置；**Spec [H]×1——票面 Scope「不再需要独立 LMN DLL，走 BUE 注册/生命周期/隔离路径」与「不做：不修改三个插件源码」票内互斥，本票不可验证**：处置 = 如实具名（交付报告 §6），本票闭环以票面「验收条件」三条为准（①接管态下实机收发正常：takeover installed + lmn2-delegate 委托锚；②T4 Q4 义务解除；③归档）；该条款属**官方纳入终态**（BueNetworkApi 重写 + 并入 BUE 单 DLL，User Story 18），建议本票闭环后另立「三插件官方纳入实施」票承接——不是迁移缺陷（T8 口径：已迁 V2 = LMN V2 命名频道，已完成）。Spec [M]×2（委托锚会话级 vs 每 CaseId 绑定 → 双端锚+组合判据+可选严格加采；U3DS/信标操作前提 → 手册补齐）已修。R2 待派发。

> 2026-09-05 R2（agent）：**Standards CLEAN**（六条修复核实、五件哈希复算 5/5 对、J1 处置属实）；**Spec FINDINGS 两条已修**：[H] 认领评论初版残留「shim 口径执行」语义改写 → 已订正为「票内互斥、本票不可验证、缺口表述」并注明初版作废；[M] P5 组合判据存在「首帧委托自 LIT、他件原生扛」反例空间 → 重构为**会话门**（takeover 在场 + 委托锚 ≥1，缺门则该会话三件全判不通过）+ 组合判定，并写明互斥原理（解析失败是会话级持续态，锚缺席 ⇔ 全部放行；锚在场 ⇔ 已注册频道帧均在 BUE 决策点消费）。R3 待派发。

> 2026-09-05 R3/R4（agent）：**Spec R3 CLEAN**（[H] 终判：工单三处定性一致无语义漂移、本票严格落在验收条件三条；[M] 终判：会话门消除反例空间、与 DEV-V2-11 锚定义相容、逐插件独立锚显式可选化）。**Standards R3 唯一残留**（交付报告 §8.6 未同步会话门口径）已修 → **Standards R4 CLEAN**。**双轴审查闭环：R1 双 FINDINGS → R2（S-CLEAN/Spec-2）→ R3（Spec-CLEAN/S-1）→ R4 双 CLEAN。** 交付物定稿：交付报告 + 手册 + identity（audit/2026-09-05/DEV-V2-08/）。下一步 = 部署 kit + 按手册三环境实机采集（agent 部署，用户实机操作，逐件带验）。

> 2026-09-05 R5 阶段（agent 自查）：R1–R4 机制叙事误用 **DEV-V2-11 委托口径**（`lmn2-delegate result=delegated`），而行 6 候选含 **DEV-V2-12 修复**——live 方向 BUE 放行、LMN 原生恰好一次派发，正向锚 = `lmn2-frame-release result=released decision=lmn-native-dispatch`（一次性），delegated 出现 = 回归信号。已源码实证（NetworkModuleAdapter.cs L455-468）并修正手册 P5/U5/速查表与交付报告 §6/§8.6（会话门三件套 = takeover installed + release 恰一条 + delegated 零出现 + 无重复派发）。缺口具名不受影响。R5 双轴重审待派发。

> 2026-09-05 R6–R8（agent）：R6 = Standards CLEAN（六处修正与源码锚一致、全文无委托/短路活口径残留、LHT epoch/seq 同构）+ Spec 一条（LHT 重复派发判据不可操作）。R7 = 首版改写被 Spec 源码纠正 + agent 亲证（`TryStoreReceivedSnapshot` 仅关闭态 false，`收到 Update/Clear` 行每帧必打——重复派发在客户端日志层**可检**；agent 此前「日志层观测不到」表述有误，如实更正）→ 按事实改写 LHT 子句（(epoch,seq) 重复键为零 + mailbox/PublishIfNewer 两层幂等辅证 + 会话门结构保证）。R8 = **双轴双 CLEAN**（Spec：幂等分层归因逐句对齐；Standards：LHT 子句/P6 与源码逐句对齐、kit 五件哈希未扰动）。速查表补客户端收到行。**审查循环最终闭环：R1→R2→R3→R4→（自查机制口径错误）R5→R6→R7→R8 双 CLEAN。** 交付物定稿并已部署双端（指纹=kit 五件全对）。下一步 = 用户按手册三环境实机采集（SP → P2P → U3DS），agent 复核日志与锚行。

> 2026-09-05 SP 轮采集完成（agent 复核）：用户提交 UMM 诊断包（单会话，LogOutput 3013 行，归档 `audit/2026-09-05/evidence/DEV-V2-08-20260905/env/sp/`）。**S1–S5 全过**：S1 接管 installed+三插件注册锚全齐；S2 LIT 完整整理链多轮（reqId=1/15/23，限频抑制按设计）；S3 LIR 用户确认三次双击均见成功 toast（SP 成功路径仅 UI；rejected=1=冷却闸门防护性静默拒，非故障）；S4 LHT 意外获得**完整尸潮生命周期**（爆发 Belfast Airport epoch=1 → 广播 Update seq=1..63 → 尸潮结束 → 广播 Clear，服务器侧广播链全落地）；S5 零误报（0 BUE 错误/0 uncaught/0 拒绝/0 RepackGate）。三份 case.md SP 段已写。**具名观察项 O-LIT-1（非本票 finding）**：用户认为排列算法质量有问题（拍板后续再说）→ 官方纳入票议程；与 V2 迁移无关。**截图 gap**：SP 轮未采集截图，以日志锚+口头确认代偿（P2P 轮起补齐截图要求）。下一步=P2P 轮（P1–P6，核心=跨端命名频道流量+`lmn2-frame-release` 会话门锚）。

> 2026-09-06 P2P 轮采集完成（agent 复核）：双端 UMM 包归档 `evidence/DEV-V2-08-20260905/env/p2p-{host,client}/`。**P1–P6 全过**：会话门双端全中（takeover installed 各 1 + `lmn2-frame-release result=released` 各 1 + `lmn2-delegate` 双端 0）；LIT 跨端请求-响应闭环（客机 RequestTidy ×3 → 主机提交 ×4 含主机自整理 loopback 1，reqId 对齐）；LIR 主机队列派发 ×2/客机回包 ×1（差额=NoChange 不回包，设计语义）；**LHT 恰好一次实锤——客机玩家放置信标（Charlottetown，发起人=易烨不会玩FPS），主机广播 Update ×10 + Clear seq=11，客机收到 Update ×10（seq=1..10 连续无缺无重复键）+ Clear 对齐，不可靠通道 1:1**。零误报（主机 6 处命中=LIT RateLimit 防护 ×5+退出 BeginQuiesce ×1，良性）。**截图/时间窗 gap 具名**：双端截图与 Get-Date 时间窗截图未采集，以包时间戳（08:51:30/08:52:02+0800，窗口重叠成立）+日志锚+口头确认代偿。剩=U3DS 轮。
