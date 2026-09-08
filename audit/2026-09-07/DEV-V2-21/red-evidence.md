# DEV-V2-21 红测先行证据（red-first evidence）

收集式红测入口：`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` 的 `--bue-v2-lit-multiplayer-red`（`AssertBueV2LitMultiplayerPath(collectAllFailures: true)`），五组：双端收发全链 / session challenge / 代际 fault scope / TidyCompleted 发布 / 半注册回滚。

## 红 1（实现后、编排修复前的首轮实跑，13 条失败）

执行时间：实现批次 C–G 完成并首次编译绿后、任何测试编排修复之前。

```
DEV-V2-21 red collection (13): 全链：challenge 就绪后客户端请求受理（Dispatched） ||
全链：主机权威恰好执行一次（reqId=1, page=3） ||
全链：权威事务终态发布 TidyCompleted（代际=会话代际, 事务=requestId, 范围=单页, Succeeded） ||
全链：客户端在收到 TidyCommitted 后执行收敛检查 ||
全链：客户端 HotkeyFlowAck 到达后服务器执行快捷键恢复 ||
全链：客户端收到 TidyCommitted 回包（功能私有消息 3） ||
全链：客户端收到 TidyHotkeyResult（功能私有消息 5） ||
全链：服务器收到 HotkeyFlowAck（功能私有消息 4） ||
重放：重复请求收到缓存的完整 Committed 重发 ||
challenge：challenge 就绪后请求受理（Dispatched） ||
[session challenge] UNEXPECTED ArgumentOutOfRangeException ||
fault：客户端仍可发送（熔断由服务器权威拒绝） ||
fault：临时熔断不写盘（restoreVerified=true）
```

根因（测试编排层）：harness 的 `EstablishChallenge` 只 Tick 服务端，客户端服务的会话发现集合为空 → 请求被判无会话；另有临时熔断断言口径错误（查路径非空应为查文件不存在）与 challenge 组缺 `Handshake()`。

## 红 2（编排修复后余留 2 条）

```
DEV-V2-21 red collection (2): challenge：challenge 就绪后请求受理（Dispatched） || [session challenge] UNEXPECTED ArgumentOutOfRangeException
```

根因：challenge 组漏调 `Handshake()`（无会话即无 challenge），补上。

## 红 3（会话真值修复后余留 1 条）

```
DEV-V2-21 red collection (1): challenge：旧 token 的伪造请求在服务器 token-only 准入失败（fail-closed）
```

根因（两个，先后修复）：
1. **生产缺陷**：`RequestTidy` 从服务的 Tick 滞后集合取会话，重连换代际后可能拿旧代际会话。修复：改用运行时 established 快照（真值）。
2. **测试断言基线**：伪造探针前权威已合法执行 1 次，断言改为「计数不再增长」；并把在途请求先排空（重连与 Hello 同批派发的换代际拒收行为另由全链组钉死）。

## 绿（最终）

```
DEV-V2-21 LIT multiplayer collection: ALL GREEN (0 failures) — groups: 双端收发全链/session challenge/代际 fault scope/TidyCompleted 发布/半注册回滚
```

随后全套默认套件 `DEV-14/DEV-16B plugin runtime tests: PASS`，全套 7/7 PASS、0 警告（TreatWarningsAsErrors=true 下构建通过即 0 警告）。

## 修复轮（R1 Spec 审查发现）后的回归

F1（生命周期绑会话事件）/ F2（服务器回包显式结果处理）/ F3（模块启动面暴露 MultiplayerReady）落地后复跑：

- `--bue-v2-lit-multiplayer-red`：ALL GREEN（0 failures），半注册组新增 `MultiplayerReady=false` 断言。
- 全套默认套件：PASS。

## 修复轮（R2 Spec 审查发现）后的回归

R2 GAP（代际更替残留旧 scope 临时熔断）修复：`OnSessionDropped` 改为代际精确清理（scope 一律关旧+磁盘持久重载；会话簿/账本/待恢复表按代际摘除；lease 保留以维持同玩家事务串行化）；`GenerationChanged` 事件驱动同代际清理。fault 组新增断言：临时熔断 + 重连换代际 → 旧 scope 关闭（临时态随代际清除，后继 scope 重开）。复跑：

- `--bue-v2-lit-multiplayer-red`：ALL GREEN（0 failures）。
- 全套默认套件：PASS。

## 流程注记（R3 Standards S3，具名）

本轮红测先行以**运行时红**为锚：收集式断言在实现编译绿后首轮实跑即红（13 条），经三轮收敛转绿，红/绿均有上文实跑记录。**编译红阶段（缺类型 CS0246）未作为独立锚点**——联机服务的 API 形状（LitTidyNetService/ILitTidyAuthority 等）在单会话内与实现同步定型，测试在 API 冻结后落入同一增量。此为具名流程偏差，不影响红/绿证据的真实性；后续票若在既有稳定缝上扩展，应恢复「先编译红→桩级红→绿」的完整链。

## 修复轮（R4 Spec 审查发现）后的回归

- DEVIATION（代际竞态）：旧代际 drop 晚于后继 scope 开启会误关新 scope。修复：`ClosePeerScope(peer, dyingGeneration)` 代际匹配（ownsScope 才整关+磁盘重载；后继持有期间只摘死代际的临时记录），`FaultRecord` 新增所属代际。
- GAP（权威页范围防御）：`ExecuteServerTidy` 增加 2..6/AllPages 防御校验（wire codec 已拒，纵深防御）。
- 复跑：`--bue-v2-lit-multiplayer-red` ALL GREEN；全套默认套件 PASS。

## 修复轮（R5 审查发现）后的回归

- Standards R5：0 BLOCKING + 4 SMELL。S1（客户端状态机 dedupe：泛型 LitExpiringKeySet + 共享 LitTidyClientKeys，死 Key 删除）、S2（页范围展开收进模块 PublishTidyCompletedForPage 单一真源）、S3（代际前缀扫描收进 LitStateKeys 单源）已修；S4（(peer, generation, token, requestId) 数据团）具名延期——状态表以显式键入参保持锁Scope 清晰，事务键收敛留后续治理。
- Spec R5 两 GAP 属票面边界误读（LIR 消费=TidyCompletedConsumer→ReloadAction 属 DEV-V2-22 票面；LIR/LHT 迁入属 DEV-V2-22/23）：本票（DEV-V2-21）票面 Scope 与 Blocked by（15/18/19，不含 22/23）即边界证据；发布侧（本票职责）已闭环。以票面文本反驳记录，R6 以显式边界声明复核。
- 复跑：`--bue-v2-lit-multiplayer-red` ALL GREEN；全套默认套件 PASS。

## 修复轮（R6 审查发现）后的回归

- Standards R6（0 BLOCKING + 2 SMELL）：PendingRestoreBook 迁入泛型 LitExpiringKeySet（含 RemoveWhere）；三处状态表的扫描体经 `LitStateKeys.RemoveMatching` 单源化。
- Spec R6 GAP-1（Connected 未订阅）**技术反驳**：冻结语义（DEV-V2-16 ④ established-only 快照 + DEV-V2-17 Connected 恰一次且仅握手完成后触发）决定快照可发现的会话其 Connected 必已发射——订阅只能得到死代码；发现路径即 Connected 的等价投影（发现未见过的 established 会话=开 scope+发 challenge），已在代码注释与本案卷记录反驳依据。
- Spec R6 GAP-2（冻结面登记）：`FeatureStartResult` 加性构造器入 SDK 契约演化登记**条目 ⑦**（加性不升 Major，理由随条目记录）。
- 复跑：`--bue-v2-lit-multiplayer-red` ALL GREEN；全套默认套件 PASS。

## 修复轮（R7 审查发现）后的回归

- Standards R7（0 BLOCKING + 1 SMELL）：SessionKey/LedgerKey 键拼接改走 LitStateKeys（GenerationPrefix/TransactionKey），键规则与 Drop 前缀彻底同源；键元组数据团维持具名延期。
- Spec R7 GAP（联机注册失败仍 Started=true）**设计裁定反驳**：`FeatureStartResult` 描述的是模块本体（补丁/dispatcher/本地权威路径均已生效）——返回 false 会让宿主停止交接漏掉该模块（跳过 UnsubscribeAll，更重违规）；失败的联机子系统已完整回滚并经 `MultiplayerReady=false` + StartGateDiagnostics 显式暴露（本修复轮补记 StartGateDiagnostics）。裁定依据随案卷记录。
- 复跑：`--bue-v2-lit-multiplayer-red` ALL GREEN；全套默认套件 PASS。

## 修复轮（R8 审查发现）后的回归

- **Spec R8：CLEAN**（GAP 0 / DEVIATION 0 / SMELL 0）——两项反驳（Connected 发现路径等价、联机失败模块 Started 语义）均被独立裁定成立；票面边界内全部要求有实现与红测覆盖。
- Standards R8（0 BLOCKING + 2 命名 SMELL）：`ReadCommittedToken`→`ReadChallengeToken`（名称对齐实义）；`LitClientSessionToken` 死构造期临时令牌连同无效 RNG 整体删除（发送门只认服务端签发 token，challenge 前拒绝即空 token 自然态）。
- 复跑：`--bue-v2-lit-multiplayer-red` ALL GREEN；全套默认套件 PASS。

## R10 修复轮（结单后全票重审发现）与红锚补录（2026-09-08）

- R10（结单后全票重审，全新实例，标的=round10-increment.diff 全票增量）发现与修复四项——**判词原文未随上一会话存档**，要点按修复增量 round11-increment.diff 重建，详录 `review-rounds.md`：①持久统计文件**解析失败**未 Degraded（R10-Standards BLOCKING，损坏文件被静默当空 scope=allow-all）→ 解析失败即 `Degraded=true` 全局降级；②后继连接代际接管有 Tick 延迟（R10-Spec GAP）→ GenerationChanged 事件拍 `AdoptSuccessor`，challenge 同拍签发；③`DropPeer(ulong)` 死代码（peer 前缀整扫）四处删除；④P2P/U3DS 实机自验**具名延期**绑 DEV-V2-24（R10-Spec GAP-2 裁定；调查证据 parse-u3ds-lnk*.ps1，本机 U3DS 无可用实例）。
- **修复轮红→绿锚点（2026-09-08 实测）**：修复源码暂存（四 src 文件 stash、保留新断言）→ `--bue-v2-lit-multiplayer-red` 恰 2 条新断言红：
  `DEV-V2-21 red collection (2): fault：代际更替由会话事件即时接管（后继 challenge 事件拍发出，无 Tick 延迟——R10-Spec GAP 修复） || fault：持久统计文件损坏 → 解析失败进入全局降级（fail-closed——R10-Standards BLOCKING 修复）`
  → 恢复修复后复跑 ALL GREEN（0 failures）。红测断言变更：challenge 组旧 token 窗口收窄为服务器侧（伪造旧 token 须被服务器 token-only 准入拒绝、权威零新增执行）+ rearm 免 Tick；fault 组新增上述两断言。
- 复跑（2026-09-08）：`--bue-v2-lit-multiplayer-red` ALL GREEN；全套默认套件 **7/7 PASS、0 警告**。
- 候选身份更新：两轮 `-t:Rebuild`（Release）字节一致，SHA-256 `0687d8f53303581c64e1fbbb2c64d1fa35cbe1573b8f3757bc7a8d7c9549a35d`（417792 B），CaseId **DEV-V2-21-CANDIDATE-20260908**（R10 修复经 Lit 源码变更传导进 Plugin.dll 确定性编译输入，身份重新授予而非沿用 78da57c2…；identity-rebuild1/2-r11.log）。
- **R11 双轴复审（全新实例）：Standards CLEAN（0 BLOCKING + 0 SMELL）/ Spec CLEAN（GAP 0 + DEVIATION 0 + SMELL 0）**——判词原文要点存档于 review-rounds.md。
