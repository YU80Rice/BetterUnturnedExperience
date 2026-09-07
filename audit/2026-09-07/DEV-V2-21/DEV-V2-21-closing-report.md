# DEV-V2-21 结单报告：LIT 迁入·联机路径 + TidyCompleted 发布

日期：2026-09-07 ｜ 票据：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-21-lit-multiplayer-path.md` ｜ 状态：**resolved（双轴 R9 双 CLEAN）**

## 1. 交付内容

P2P / U3DS 联机背包整理全链 + TidyCompleted 发布 + 宿主模块生命周期首公里：

- **稳定功能网络入口**：`DeferredBueNetworkApi`（Core/Network）——`IFeatureBootstrap.Network` 永非 null 且实例不灭；角色未决时登记/订阅被缓存并回放到武装后的 `BueNetworkRuntime`；隔离只摘活绑定、门面不换。
- **宿主模块启动路径**：`BueFeatureStartRuntime`——注册屏障完成后按目录组合 `FeatureBootstrap`（稳定门面 + 宿主总线的 Subscriber/Publisher 本身份视图 + 宿主代际）并驱动 `IFeatureModule.Start`；停止沿冻结交接：`Stop` 返回后 `FeatureEventBus.UnsubscribeAll(feature)`。`FeatureStartResult` 获可构造结果（SDK 登记条目 ⑦）。
- **LIT 联机业务（功能私有协议）**：`LitTidyWireCodec`（五种消息 + V3 封套 + 结构校验 fail-closed）、`LitTidyServerState`（会话 token 簿/请求账本/操作 lease/复合准入门，键含 peer+连接代际+token+requestId）、`LitTidyClientState`（原子 token 门/pending/结果等待，泛型 `LitExpiringKeySet`）、`LitTidyFaultScopeBook`（代际 scope + 功能私有 JSON 磁盘权威）、`LitTidyNetService`（双方向订阅/challenge/请求→准入→dispatcher→权威事务→可靠回包→ACK→恢复→结果全链 + CancelNew 补偿 + BeginQuiesce/Stop 两阶段）、`LitTidyProductionAuthority`（玩家解析/可信指纹事务/ACK 热键恢复/收敛检查）。
- **连接代际熔断**：Connected/新代际开 peer scope；断线/换代关旧 scope（临时态随代际清除、磁盘持久统计保留并回同步）；`SteamP2PFriends.BeginScope("p2p")` 与 `Provider.onEnemy*` 业务订阅消失。
- **TidyCompleted**：本地（`LocalTidyExecutor`，代际 0）与服务器权威终态（`PublishCompleted`，代际=会话代际、事务号=requestId）统一经模块 `PublishTidyCompletedForPage`（页范围单一真源）→ `OwnedEvents.TryPublish`。
- **宿主泵**：LIT dispatcher 泵上收插件 Update 链（headless 含），不再挂客户端专用驱动。

## 2. 红绿链与审查链（摘要，全记录见 `red-evidence.md`）

- 红绿：运行时红三轮实测（13 条 → 2 条 → 1 条）→ ALL GREEN；修复中揪出两个真生产缺陷（SteamPlayer→Player 解析、客户端 ACK 误走 SendToClient）。编译红未作独立锚点=具名流程偏差（案卷注记）。
- 双轴独立审查（每轮全新实例，无续用）：
  - R1 Spec NOT CLEAN（2GAP+1DEV+1SMELL）→ 会话事件绑定/回包显式结果/MultiplayerReady/红证入档；
  - R2 Spec NOT CLEAN（1GAP）→ 代际精确清理（scope 关旧+账本代际摘除）；
  - R3 Standards NOT CLEAN（6 BLOCKING+3 SMELL）→ 门面锁纪律、challenge 异常摘除重试、SteamPlayer.player、账本 TTL 语义、客户端 ACK 发送路径、Temporary 更名、客户端状态去重、流程注记；
  - R4 Spec NOT CLEAN（2）→ 代际竞态 scope 误关（ClosePeerScope 代际匹配+FaultRecord.ScopeGeneration）、权威页范围防御；
  - R5 Standards 4 SMELL（3 修 1 具名延期：键元组数据团）+ Spec 2 GAP（票面边界误读，以票面文本反驳）；
  - R6 Standards 2 SMELL → PendingRestoreBook 迁泛型集+扫描体 `LitStateKeys.RemoveMatching` 单源；Spec 2 GAP（Connected 订阅=冻结语义反驳成立；FeatureStartResult 入 SDK 登记条目 ⑦）；
  - R7 Standards 1 SMELL → 键拼接走 LitStateKeys；Spec 1 GAP（联机失败模块 Started 语义）→ 设计裁定反驳（宿主停止交接优先）；
  - **R8 Spec CLEAN**（两项反驳均裁定成立）；R8 Standards 2 命名 SMELL → 修复；
  - **R9 双轴双 CLEAN**（Standards CLEAN + Spec CLEAN，R8 反驳维持）。

## 3. 验证与身份

- 全套测试 **7/7 PASS、0 警告**（TreatWarningsAsErrors=true）；`--bue-v2-lit-multiplayer-red` 收集组 ALL GREEN。
- 候选：`BetterUnturnedExperience.dll` **417792 B**，SHA-256 `78da57c2b2ff67b747ca002c67acb119bba334bd795c37b6959f27784a9b8385`，两轮 `-t:Rebuild` 字节一致（`identity-rebuild1/2.log`、`identity-sha256.txt`）；CaseId `DEV-V2-21-CANDIDATE-20260907`；**RELEASES 换标随 DEV-V2-24 实机验收**（沿 17/18/19 惯例，不继承既往批准）。

## 4. 具名延期 / 边界

1. **键元组数据团**（(peer,generation,token,requestId) 显式传参）——状态表锁域清晰优先，事务键收敛留后续治理（R5 具名，Standards R9 维持延期）。
2. **Per-peer 限流器**（最小间隔+滑动窗口）未迁——准入门已封重放/容量/串行化面，随管理命令票补齐。
3. **持久化首启 marker 仪式**简化为 tmp→Replace→.bak 两段原子写（JSON 键结构不变）。
4. **可靠位与实机行为**：loopback 不观测 reliability 位；P2P/U3DS 实机自验（challenge 拒发行、reqId 双端对齐、ACK 链）绑定 DEV-V2-24。
5. **LIR 消费侧与 LIR/LHT 迁入**属 DEV-V2-22/23（本票发布侧已闭环）。
