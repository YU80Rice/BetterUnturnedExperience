# DEV-V2-24 审查轮次判词（D0-b 修复轮）

> 标的 = 面板 BII 条目显示名中文化（spec story 3）；每轮全新实例、显式 subagent_type（standards-reviewer / Spec-Reviewer）。
> 增量：round1-increment.diff（e70b7c4，源码 1 串 + 红测断言 1 条 + 审计换绑）/ round2-increment.diff（aae2c99，文档残留清理）。

## R1（2026-09-08，两个全新实例）

- **Standards R1: FINDINGS**（agent_16989f20-25fd-4a4a-9e90-b14381e841d2；blocking=0 smell=2 info=1）
  - 哈希换绑全链核过（identity-sha256.txt v2 / 手册 §1 / kit/out / artifacts / 六模板一致；旧哈希 7d5dd3b5 仅存于「前身作废」语境与追加史）；红测打在既有 HasManagementEntry 缝（Program.cs:3918-3920），redtest-run.log:2 FAIL 于新断言；REG-ACCEPT 英文日志标签具名裁定合理（spec 冻结的是面板显示名，LIT/LIR/LHT 注册日志同为英文标签）。无硬违规。
  - SMELL-1：手册 §3 S2 步骤残留「Better Item Interaction 条目 + D0 判别点」，与 D0-b 后源码冲突（采集员会误触「不符即停」）。
  - SMELL-2：手册 §10 与 sp / p2p-client 两模板残留「D0 判别 / 未拍板不采集」开放措辞，与票面「手册、六模板已换绑」不符——不可延期（会把错误期望写进证据）。
  - INFO：修复提交 hash 未回填票面 Comments。
- **Spec R1: CLEAN**（agent_251a95a9-3ab6-47c3-aafa-8996699506ae；gap=0 deviation=0 smell=0）
  - 修复恰落 story 3 / 显示名冻结面缺口，无其它英文投射点；FeatureId/注册身份未动；spec 未冻结启动日志文本（日志标签裁定不冲突）；红绿链证据支撑红测先行；票面记录与增量一致。

## F1-F3（提交 aae2c99）

S2 改四件中文名齐口径；§10 与 sp/p2p-client 模板清开放措辞；e70b7c4 回填票面 Comments 与手册 §1。

## R2（2026-09-08，两个全新实例）

- **Standards R2: CLEAN**（agent_0435752b-9e6d-41c5-bca1-05f9a0b73c50；0/0/0）：三条发现逐条核落地（S2/§10/模板/快照回填），无新口径或哈希冲突，round2 增量封闭（仅 4 个 md，无 src/tests）。
- **Spec R2: CLEAN**（agent_26ab1090-8287-40c6-af8d-e1dea96f83e8；0/0/0）：story 3 口径一致无残留矛盾；哈希与 CaseId 链一致；票面仍 claimed、未提前关票。

## 闭环

**双轴最终 CLEAN（Standards=R2 / Spec=R2）**。候选 `3cbd6268…9e4d`（DEV-V2-24-CANDIDATE-20260908）为已审增量产物；实机采集按手册（换绑后）进行。

---

# 修复轮审查（F-A + F-B2，2026-09-08）

> 标的 = round3-increment.diff（F-A `3db75c0` + F-B2 `f89194f`）与 round4-increment.diff（F1 `03b661f` + F1b `fe3e3cc`）。每轮全新实例。

## R2（两个全新实例）

- **Standards R2: CLEAN**（agent_a3473cf0-cf77-4083-b1d8-258b779c28a0；0B+2S+3I，均具名可延期）
  - F-A 回滚与 B3 同构且 token 面更严；OpenPeerScope 同代 no-op；包装器转发全 7 成员。
  - S1：ObservePollSuccess 不清 Latched 与测试语义不符（→F1 采纳修复）。
  - S2：宽限窗不对称（Poll 异常宽限内 dispatchedSurfaces 不清理）→具名延期：Poll 逐帧全捕获，≤60 帧残留 overlay 风险可容忍，genuine 失败仍到阈值隔离。
  - INFO：litfb 拆除彻底 ✓；无「单帧立即隔离」冻结承诺；F-A 事件重复 += 与 B3 同构且守卫幂等。
- **Spec R2: NOT CLEAN**（agent_e354a55a-8d1b-4739-9e51-ed720f5b8922；2D+0S）
  - D1：探针分支锁存行双发（隔离分支+通用分支各一次）。
  - D2：生产 sink 无条件追加 BUE-INVENTORY-003，污染 004 首失败/恢复行的诊断 id。

## F1/F1b（`03b661f` + `fe3e3cc`）

- D1：隔离后跳过通用发射，恰一次。
- D2：锁存行(003)走 EmitDiagnosticOnce(Error)；首失败/恢复行(004)改走 BueRuntimeLog.Runtime(Debug)，与既有 004 行同通道，id 不再被污染。
- S1 采纳：ObservePollSuccess 同时清 Latched。
- F1b：ObservePollSuccess 的 XML 注释同步（Standards R3 唯一 SMELL 处置）。

## R3（两个全新实例）

- **Standards R3: CLEAN**（agent_3cdcd991-09eb-4cff-b5e3-1e46f89c1f5e；0B+1S+1I——唯一 SMELL 即过期 XML 注释，已在 F1b 修复；S2 延期维持）。
- **Spec R3: CLEAN**（agent_10540155-ebea-461b-a636-9a840ca1b271；0/0/0；首派发因基础设施零输出作废留痕，本判词来自重派的全新实例）。

## 闭环

**双轴最终 CLEAN（Standards=R3 / Spec=R3）**。修复轮候选 `c9b6b6e4…eb86`（DEV-V2-24-CANDIDATE-20260908，535552B 三轮 Rebuild 一致，identity-sha256.txt v3）；前身 7d5dd3b5…c223 与 3cbd6268…9e4d 作废。F-B1（幽灵覆盖层）未在本轮修复——H2 数据复制已排除，待新候选上提复现率压测后定根因。
