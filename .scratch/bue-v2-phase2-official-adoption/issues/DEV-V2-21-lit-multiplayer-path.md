# DEV-V2-21：LIT 迁入·联机路径 + TidyCompleted 发布

Type: task
Status: resolved
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-15（单人路径核心）、DEV-V2-18（BUE 帧绑定）、DEV-V2-19（TidyCompleted 契约件）
Spec: `../spec.md`（「LIT ↔ LIR：TidyCompleted 功能事件」「LIT：设置、熔断与夹具」及「三插件迁入形态」相关条目）

## What to build

P2P / U3DS 联机下的背包整理：客机发整理请求 → 主机权威执行事务 → 可靠回包，双方看到一致结果；主机整理完成后发布 `TidyCompleted` 功能事件（LIR 等消费方据此协作）；一次连接触发的熔断随断线结束但磁盘统计保留。

## Scope

- 网络：以新 FeatureId 注册命名频道；双端 handler 按 `Subscribe` 两方向重写（服务器 handler 以会话携带的 peer 身份替代旧 SteamId 参数；客户端 handler 无 sender）；五种私有消息与负载版本封套保留为功能私有（契约不定义业务协议）；请求/回包走可靠发送并处理 `NetworkSendResult` 显式结果。
- 保留业务一致性实现：session challenge、账本、玩家操作表、事务管理——全部绑功能代际，禁止跨功能泄漏；`BuildNamedMessage` 类纯组包 helper 改为功能内自组 `byte[]`。
- 熔断 scope 绑 BUE 连接代际：Connected / 新代际 → 开始本次会话 fault scope；Disconnected / 代际更替 → 关闭旧 scope + 清内存态 + **保留磁盘持久化统计**（功能私有 JSON 键结构不变，不复制进第二事实源）；旧 SteamP2PFriends `BeginScope("p2p")` 调用方随迁入消失。
- Provider 生命周期订阅（onEnemyConnected/onEnemyDisconnected）改绑 BUE 会话事件与代际（与 DEV-V2-17 的 Connected/Disconnected/GenerationChanged 对齐）。
- `TidyCompleted` 发布：经 OwnedPublisher，载荷符合契约件（DEV-V2-19）冻结字段；服务器权威事务完成处发布。

## 验收条件

- [ ] 红测先行：双端收发（假 transport 全链）/ session challenge 回归 / 代际切换 fault scope（清内存+留磁盘）/ `TidyCompleted` 载荷与发布语义 / 注册失败半注册回滚——先红后绿
- [ ] P2P 自验：客机整理 → 主机权威 → 回包一致；主机本地整理发布事件
- [ ] 联机路径行为与 08 kit 基线一致（MSG 语义、challenge、事务）
- [ ] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN

## Answer

**结论**：已实现并闭环（2026-09-07，双轴 R9 双 CLEAN，全套 7/7 PASS 0 警告）。候选 `BetterUnturnedExperience.dll` 417792 B，SHA-256 `78da57c2b2ff67b747ca002c67acb119bba334bd795c37b6959f27784a9b8385`（两轮 `-t:Rebuild` 字节一致），CaseId `DEV-V2-21-CANDIDATE-20260907`，RELEASES 换标随 DEV-V2-24 实机验收。结单：`audit/2026-09-07/DEV-V2-21/DEV-V2-21-closing-report.md`；红绿+审查链全记录：同目录 `red-evidence.md`。

**实现落位**：
- 网络：FeatureId 命名频道 + `Subscribe` 双方向（服务器 handler 身份=`IConnectionSession.PeerSteamId`，客户端 handler 无 sender）；五种私有消息（REQUEST_TIDY_V2/TIDY_COMMITTED/HOTKEY_FLOW_ACK/TIDY_HOTKEY_RESULT/SESSION_CHALLENGE）+ NamedPayloadVersion=1 + 协议 V3 封套功能私有（`LitTidyWireCodec`）；请求/回包 reliable 且显式处理 `NetworkSendResult`（客户端发送失败清 pending；服务器 Committed 失败留账本缓存等 Cached 重发命中）。
- 一致性件迁移重写（`LitTidyServerState`/`LitTidyClientState`，禁跨代泄漏）：session challenge（RNG fail-closed token-only+requestId 单调）、账本（Received 不被容量驱逐、TTL=防重放窗口、缓存完整 mappings）、per-peer 操作 lease（错配拒释放）、待恢复事务簿、准入复合门（模板 B 序）+ `CancelNew` 补偿——全部键绑功能代际+连接代际。
- 熔断：`LitTidyFaultScopeBook` 绑 BUE 连接代际（Connected/新代际开 peer scope；断线/换代关旧+临时态随代际清除+磁盘 JSON 权威保留回同步；代际竞态由 `ClosePeerScope(peer, dyingGen)` 匹配消除）；`SteamP2PFriends.BeginScope("p2p")`/`Provider.onEnemy*` 随迁入消失。
- TidyCompleted：`OwnedEvents.TryPublish` 唯一路径；本地（gen=0）与服务器权威终态统一经 `PublishTidyCompletedForPage`（页范围单源）映射（Committed→Succeeded / Rejected→Rejected / 其余→Failed），事务号=requestId/模块单调号，永非 0。
- 宿主首公里：`BueFeatureStartRuntime` Start/Stop+`UnsubscribeAll` 冻结交接；`DeferredBueNetworkApi` 保证 `IFeatureBootstrap.Network` 永非 null（登记/订阅先缓存、武装后回放）；`FeatureStartResult` 可构造（SDK 登记条目 ⑦，加性不升 Major）。

**红测**：`--bue-v2-lit-multiplayer-red` 五组收集式（双端假 transport 全链/challenge 回归/代际 fault scope 清内存留磁盘/TidyCompleted 载荷与发布/半注册回滚）+ 新断言（超会话临时态清除、MultiplayerReady）。红三轮实测（13→2→1）→ALL GREEN；编译红未独立锚点=具名流程偏差（案卷）。

**审查链**：R1 Spec(2G+1D+1S)→R2 Spec(1G)→R3 Standards(6B+3S)→R4 Spec(2)→R5(4S 具名延期 1 + 2 票面边界反驳)→R6(2S + 1 反驳 + SDK 条目⑦)→R7(1S + 1 设计裁定反驳)→R8 Spec **CLEAN**（反驳双采纳）→R9 双轴**双 CLEAN**。每轮全新实例。

**具名延期**：①键元组数据团（状态表锁域清晰优先）；②per-peer 限流器（随管理命令票）；③持久化首启 marker 仪式简化为 tmp→Replace→.bak；④可靠位与实机行为（P2P/U3DS 自验绑 DEV-V2-24）。
