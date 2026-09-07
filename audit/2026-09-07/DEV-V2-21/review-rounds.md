# DEV-V2-21 双轴审查轮次记录（R1–R9 判词原文存档）

本文件由实施会话于结单后补落盘：每轮审查的**判词原文要点逐条照录**（含实例 agent-id、失败派发记录），此前判词只在会话流与 red-evidence.md 转述，未归档为独立文件——此为记录疏漏，本文件补齐。判词原文以会话流工具结果为准，本存档为照录非重写。

**派发纪律**：每轮两轴均为全新 spawn（Fresh-instance 规则 425c2aa），无 SendMessage 续用、无持久会话复用。

## 失败派发台账（网关/上游错误，均当场宣告并重试）

| 轮次 | 轴 | 结果 | 处置 |
|---|---|---|---|
| R1 | Standards | 首派 stream interrupted；二派 provider server error | 改后续轮重派（R3 起成功） |
| R1 | Spec | 首派 stream interrupted | 重派成功（agent_654afdc9） |
| R4 | Standards | stream interrupted（后有一实例 bf7dac35 起跑但未交付判词） | 缺席，R5 Standards 覆盖复核 |
| R9 | Spec | 两连派 "Model request failed" | 第三次（精简 prompt）成功（agent_142c98ed） |

## R1 Spec（agent_654afdc9）— NOT CLEAN（GAP 2 + DEVIATION 1 + SMELL 1）

1. GAP：Provider 生命周期未改绑 BUE 会话事件——Tick 轮询代替事件绑定（LitTidyNetService.cs:253-280）。
2. DEVIATION：服务器回包 `NetworkSendResult` 未参与状态处理（SendCommitted/SendHotkeyResult 丢弃结果）。
3. GAP：服务注册失败未向模块启动结果传播（module.Start 无条件 Started=true）。
4. SMELL：红测未证明先红后绿（无红态记录落盘）。
→ 修复 F1–F4（会话事件绑定/回包显式结果处理/MultiplayerReady/red-evidence.md 入档）。

## R2 Spec（agent_c2ecc8a6）— NOT CLEAN（GAP 1）

- GAP：断线/代际更替须关旧 scope 清内存态，但后继会话存在时直接 return，临时熔断可跨代际残留（LitTidyNetService.cs:337-360）。
→ 修复：代际精确清理（ClosePeerScope + 账本/会话簿/待恢复表按代际摘除）+ 超会话断言新增。

## R3 Standards（agent_377a43da）— NOT CLEAN（BLOCKING 6 + SMELL 3）

- B1 门面 RegisterChannel 锁内调运行时；B2 DetachHandlesLocked/AttachSubscription 锁内 Dispose；B3 GenerateTokenOrFail 异常后 challenge 永不补发；B4 FindSteamPlayer 返回 SteamPlayer 却 `as Player`（恒 null）；B5 账本 TTL 丢弃 Received 与注释不变量矛盾；B6 客户端 HotkeyFlowAck 误走 SendToClient。
- S1 RestoreVerified 命名反义；S2 客户端状态表结构重复；S3 红测为先实现后追绿（流程注记）。
→ 六 B 全修（其中 B4/B6 为两个真生产缺陷），S1/S2 修，S3 具名注记入 red-evidence.md。

## R4 Spec（agent_34224290）— NOT CLEAN（GAP 1 + DEVIATION 1）

1. DEVIATION：旧代际 drop 晚于新代际建立会误关新 scope（ClosePeerScope 仅按 peer）。
2. GAP：服务端未校验请求 Page 即索引 inventory.items（权威端缺纵深防御）。
→ 修复：ClosePeerScope(peer, dyingGeneration) 代际匹配 + FaultRecord.ScopeGeneration；权威页范围防御校验。

## R5 Standards（agent_6a438661）— NOT CLEAN（BLOCKING 0 + SMELL 4）

S1 PendingTable 死 Key/过期逻辑重复；S2 AllPages→first/last 双真源；S3 Drop 前缀扫描三处复制；S4 (peer,generation,token,requestId) 数据团。结论行明示「无独立 BLOCKING；R3 六 B 与 R4 代际匹配结构上仍成立」。
→ S1/S2/S3 修；S4 具名延期（案卷）。

## R5 Spec（agent_cb7f6934）— NOT CLEAN（GAP 2）【票面边界误读，反驳成立】

两 GAP 实为 LIR 消费侧与 LIR/LHT 迁入——属 DEV-V2-22/23 票面；本票 Scope 与 Blocked by（15/18/19）即边界证据。R6 起派发时显式声明票面边界。

## R6 Standards（agent_1132519f）— NOT CLEAN（BLOCKING 0 + SMELL 2）

PendingRestoreBook 未迁泛型集；Drop 扫描体未单源（LitStateKeys 只统一了前缀字符串）。
→ 修复：迁入 LitExpiringKeySet（+RemoveWhere）；LitStateKeys.RemoveMatching 单源化三处扫描体。

## R6 Spec（agent_b8f5661b）— NOT CLEAN（GAP 2）→ 1 反驳成立 + 1 修复

1. GAP Connected 未订阅 → **反驳**：冻结语义（established-only 快照 + Connected 恰一次且仅握手后触发）决定快照可发现会话的 Connected 必已发射，订阅=死代码；发现路径即等价投影（R8 裁定成立）。
2. GAP FeatureStartResult 构造器未登记 → 修复：SDK 契约演化**条目 ⑦**（加性不升 Major）。

## R7 Standards（agent_53756051）— NOT CLEAN（BLOCKING 0 + SMELL 1）

SessionKey/LedgerKey 手写键拼接未走 LitStateKeys → 修复（GenerationPrefix/TransactionKey 单源）。键元组数据团维持具名延期（不计 SMELL）。

## R7 Spec（agent_68b20954）— NOT CLEAN（GAP 1）【设计裁定反驳，R8 裁定成立】

联机注册失败仍 Started=true → 反驳：FeatureStartResult 描述模块本体（补丁/dispatcher/本地路径均生效）；返回 false 会让宿主停止交接漏掉该模块（更重违规）；联机子系统回滚完整且 MultiplayerReady + StartGateDiagnostics 显式暴露（本轮补记诊断）。

## R8 Spec（agent_ffb1e7ea）— **CLEAN**（GAP 0 + DEVIATION 0 + SMELL 0）

判词原文要点：「R6『Connected 未订阅』反驳成立」「R7『联机注册失败仍 Started=true』反驳成立」「其余票面要求均有实现与红测覆盖；LIR 消费及 LIR/LHT 迁入未越入本票边界」。

## R8 Standards（agent_1f60f72f）— NOT CLEAN（BLOCKING 0 + SMELL 2）

ReadCommittedToken 名称与实义不符；temporaryToken 死字段（构造期 RNG 无效）。
→ 修复：改名 ReadChallengeToken；死令牌连同无效 RNG 整体删除。

## R9（终审）Standards（agent_4357a8c4，11 次工具调用，约 3.3 分钟）— **CLEAN（BLOCKING 0 + SMELL 0）**

判词原文要点：「R8 两项已核：ReadChallengeToken 对齐实义；LitClientSessionToken 构造期临时令牌与无效 RNG 已删。键元组数据团维持案卷具名延期，本轮不重开。本轮增量未发现新的 Mysterious Name / Duplicated Code / Feature Envy / Data Clumps。」

## R9（终审）Spec（agent_142c98ed，13 次工具调用，约 4.0 分钟）— **CLEAN（GAP 0 + DEVIATION 0 + SMELL 0）**

判词原文要点：「round9-increment.diff 仅修复 ReadCommittedToken→ReadChallengeToken 命名，并删除无效构造期临时 token」「red-evidence.md 明确记录 R8 Spec 已 CLEAN，round9 未引入新的行为变更」「票面的 LIR/LHT 边界未被突破」「无逐条发现」。
（前两次派发 "Model request failed"，第三次成功——见失败派发台账。）

## 终局

**R9 双轴双 CLEAN**（Standards + Spec，全新实例）；R8 两项反驳均被独立裁定成立。轮次链：R1→R2→…→R9 共 9 轮，累计修复 6 BLOCKING + 1 DEVIATION + 8 SMELL，3 次技术/边界反驳被采纳，1 项具名延期（键元组数据团）。
