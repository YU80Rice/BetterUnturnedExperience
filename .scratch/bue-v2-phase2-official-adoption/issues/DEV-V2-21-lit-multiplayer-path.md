# DEV-V2-21：LIT 迁入·联机路径 + TidyCompleted 发布

Type: task
Status: claimed
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
