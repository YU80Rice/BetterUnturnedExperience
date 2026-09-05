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
