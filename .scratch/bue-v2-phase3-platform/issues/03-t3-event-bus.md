# V3-T3 FeatureEventBus 事件身份与授权路由

- **Ticket**: V3-T3
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-09 七项裁决定音，方案 A）
- **Blocked By**: V3-T1, V3-R1
- **Map**: [map.md](../map.md)

## Question

功能事件是跨功能协作的唯一公开缝（CONTEXT 冻结词汇「功能事件」：官方与生态同权）。风险级 **Strong**。按六问结构：

1. **事件身份**：事件身份字符串由发布者 FeatureId 派生的现行规则（TypeByName）是否够稳——生态侧误订阅 / 伪造发布者身份的边界？是否需要授权路由（谁能发布哪些事件、谁能订阅）？
2. **订阅语义**：订阅句柄、注销、单订阅者异常不扩散——现行 OwnedEvents 面对生态是否够用，缺口在哪？
3. **实施深度**：本阶段做到哪层（现状契约化 vs 引入事件 envelope / 版本 vs 授权模型）？
4. **留到后续**：哪些明确不做？
5. **契约影响**：TidyCompleted / 宿主时钟已入 Contracts 冻结面（DEV-V2-19）；本票裁决是否扩面、如何升版本？
6. **同权影响**：官方 / 生态发布与消费的路由规则是否同一条？

事实输入：V3-R1。主源：CONTEXT「功能事件」条、第二阶段 spec.md 事件落位节、DEV-V2-19 票。

## Answer

2026-09-09 用户+PM 七项裁决全部定音，**方案 A（事件类型归属登记）通过**。deepening 目标=把事件总线从「按 CLR 类型广播」深化为「事件身份、类型归属和 owner 一致性均可验证的本地路由 module」。

### 1. 事件路由不变量 = 类型归属一致性（方案 A）

- 新增路由不变量：**`PublishedEventType.Owner == PublisherFeatureId`**。确定性归属登记：`TidyCompleted`→LIT FeatureId、`HostTick`→BUE Host（保留宿主身份）、生态事件→发布该事件的生态 FeatureId。
- 发布流程冻结：①校验 eventId 格式及发布者前缀 → ②查载荷类型登记归属 → ③校验发布者身份==类型归属 → ④不匹配=拒绝+新事件路由诊断 → ⑤通过才进现有按类型派发。
- 拒绝必须：显式失败结果、不调用任何订阅者、结构化诊断、不抛越过模块边界的异常。
- 定性：**事件类型真实性与归属一致性检查，不是 ACL**。不采用 B（订阅 API 带 eventId——不迁移生态消费者、不过滤责任下放订阅者）；不采用 C（自律挡不住「合法前缀+冒用载荷类型」）。
- 未登记事件类型按明确规则拒绝或限定为自有生态事件，**不得静默放行**。

### 2. 事件身份规则契约化

入 SDK 冻结面的是两个生态可见接口及其语义：`IFeatureEventSubscriber` / `IOwnedFeatureEventPublisher`。登记：eventId 由 `<owner>/<event-name>` 派生且发布者必须拥有前缀；公开事件类型唯一归属；宿主事件用保留身份；归属与发布者一致；拒绝=显式 false/结果+结构化诊断；订阅句柄幂等注销；`UnsubscribeAll(owner)` 只清对应 owner；回调锁外执行；单回调异常不扩散；停止后订阅清理。`FeatureEventBus` 类本体**保持内部自由不列契约**（索引/锁/dispatch 可演进，公开 interface 语义不变不触发契约升级）。

### 3. 载荷约定：不引入统一 envelope

官方事件维持既有冻结载荷（发布者 FeatureId/作用范围/结果/连接代际+领域字段），本票不重设计。生态事件只登记建议约定（含发布者身份与代际），不强制统一基类、不运行时逐字段验证生态 DTO、不把事件总线变成序列化协议、不用 envelope 取代归属检查。伪造由「eventId 前缀校验+类型归属校验」双层解决。

### 4. 跨环境语义：进程内本地总线（SDK 明确限制）

不跨机器、不经 BueNetworkApi、不持久化、不重放、不承诺断线补发、不承担网络可靠性与跨服同步。跨机必须走 BueNetworkApi。开发者文档给对照：同进程协作→Feature Events；跨 Host/Client→BueNetworkApi。

### 5. 实施深度 = 现状加固三件套

①归属映射检查（登记+owner 比对+伪造官方事件拒+官方冒发 HostTick 拒+正确 owner 通过+未登记类型明确规则不静默放行）；②既有语义红测（前缀不匹配拒/宿主保留/归属不匹配拒/句柄幂等只删自己/单回调异常不扩散进诊断/UnsubscribeAll 清理完整/锁外派发/停止后不再收到/同载荷类型不同身份不跨归属误投递）；③契约登记（两接口+身份规则+归属规则+宿主保留+本地限制+官方载荷+生态建议载荷+网络边界对照）。**不做**：ACL、统一 envelope、订阅 API 变更、跨网络事件、持久化/重放、全局权限系统。

### 6. 契约影响 = 加性 Minor

触及：归属映射与路由语义、`IFeatureEventSubscriber`、`IOwnedFeatureEventPublisher`、SDK 事件身份与跨环境说明。与 T2 同批实施则共用 **2.1**，晚于 T2 单独实施则顺延 **2.2**；本票只冻结「Minor 级、加性」，不自行决定发布批次。`FeatureEventBus` 类本体内部重构不单独触发契约版本；既有订阅调用无需改写。

### 7. 同权检验（三+NoOp 升级）

①订阅无 ACL：官方==生态同一订阅路径，不按官方/生态分叉权限；②官方同样受归属检查：红测「LIT 冒发 HostTick→拒绝」，官方 FeatureId 不豁免；③官方先行消费（dogfooding）：LIT 发布 TidyCompleted→LIR 消费、LHT 订阅 HostTick→HUD/追踪链——新增路由规则必须先由官方功能自己通过；④**NoOpFixture 升级为生态契约 probe**：注册→取订阅入口→订阅公开事件→触发→收到回调→停止自动注销→异常隔离可观察——不替代官方 dogfooding，但证明生态 DLL 的公开事件 interface 确实可用。

### 本票冻结 / 不做

冻结：类型唯一 owner 登记；发布者 owner==载荷类型 owner；两接口+完整语义入冻结面；无 envelope；进程内本地；归属+语义+清理全入红测；同权订阅路径；Minor（2.1 共用或 2.2 顺延）。不做：ACL、订阅 API 变更、跨网络事件、持久化/重放、升级成网络系统、FeatureEventBus 类本体冻结为公共 implementation。

## Comments
