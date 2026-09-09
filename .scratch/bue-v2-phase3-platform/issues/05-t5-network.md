# V3-T5 BueNetwork 绑定、限流、主线程投递与失败语义

- **Ticket**: V3-T5
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-09 八项裁决定音，方案 B×2）
- **Blocked By**: V3-T1, V3-R1
- **Map**: [map.md](../map.md)

## Question

阶梯一级（收敛残差）。风险级 **Worth exploring**。除六问结构外带两个**具名子问题**（对账残差落位，2026-09-09 用户裁定）：

- **子问题 A：限流入平台（契约边界反悔）**——DEV-V2-25 把发送限频做成 LIT 功能私有（`LitSendFailureRateLimiter`）；愿景把限流列为 BueNetwork 发送路径平台职责（「检查频道→限流→编码→投递」）。裁决：限流是否上收平台（哪一级：全局/每频道/每会话）、功能私有保留多少、DEV-V2-25 产物如何迁移。
- **子问题 B：主线程投递契约化**——现行 spec.md 冻结面把 LIR 主线程 dispatcher 队列保留为功能私有；裁决要不要补注进公开契约：入站回调保证线程、dispatcher 队列上收为平台服务、还是维持功能私有只补文档。

六问：绑定状态公开面 / 失败结果语义（PartialFailure、lease 与失败可见性——PM 列 Worth exploring）/ 实施深度 / 留到后续 / 契约版本影响 / 同权影响。

事实输入：V3-R1。主源：DEV-V2-25 票与结单、CONTEXT「入站订阅」「会话驱动组播」「自动握手」、愿景阶段 1 节。

## Answer

2026-09-09 用户+PM 八项裁决全部定音（Q1/Q2 均方案 B）。deepening 目标=把 BueNetwork 从「能注册频道、能发送帧」的浅传输 facade 深化为拥有发送预算、线程交接、失败可见性与生命周期清理规则的深 module。

### 1. 限流入平台 = 切分上收（方案 B）

**平台负责**：每会话发送保底速率限制（发送预算）——超出预算发送不执行、返回新显式 `NetworkSendResult.Throttled`、记结构化诊断。冻结：按会话计算；官方/生态一视同仁；不静默丢弃；显式结果；不在锁内执行传输或回调；**预算状态随 ConnectionGeneration 隔离**。窗口/令牌数/容量留实施票。
**功能负责**：业务重试、业务退避、业务协议状态机——LIT 挑战重臂 `{0,1s,2s,4s,8s}` 与 `LitChallengeRearmBook` 仍由 LIT 持有，不上收。平台只回答「发送是否被接受」。
**LIT 告警限频**：平台健康诊断接管后可退役（逐帧 WARN 限频规则）；但 LIT 业务重试/退避不得删、领域状态机不得由平台替代；平台健康诊断只表达链路状态，不表达整理业务结果。

### 2. 主线程投递 = 线程语义冻结 + dispatcher 上收（方案 B）

**入站回调线程（SDK 明确登记）**：`BueNetworkApi` 入站 handler 在**传输泵线程**执行，不保证 Unity 主线程——生态不得在入站 handler 直接访问 Unity 对象/修改 Unturned 状态/操作 UI/调用必须游戏线程的方法。
**平台主线程 dispatcher**：传输泵线程 → BUE MainThreadDispatcher → Unity/Unturned 主线程。冻结语义：入站 handler 可投递工作；dispatcher 在宿主主线程执行；有容量上限、超限显式失败；模块停止时所属待处理工作失效；任务绑定模块 LifecycleGeneration；单任务异常只影响所属模块；**生态不得自建同职责 Update 泵或无限队列**。LIR 迁移自身 dispatcher=官方先行消费（dogfooding）。
不采纳 A（不改泵回调线程、不引入全局同步等待、不把接收路径改成主线程阻塞路径）；不采纳 C（只写文档会让主线程队列继续散落各功能，无法形成平台 leverage）。

### 3. 失败可见化 = 会话级链路健康 + 入站异常诊断补齐

**每会话健康状态**：`ConnectionGeneration + Session + ConsecutiveSendFailures + HealthState`（最低 Healthy/Degraded 两态）。连续失败达阈值→一次 degraded 诊断；成功发送恢复→一次 recovered 诊断+清零。默认阈值=DEV-V2-25 先例 **10 连续失败**，本票只冻默认行为，可配置化留给 Settings 票（不同时扩 Network 和 Settings 两面）。
**诊断纪律**：电平式状态变化不逐帧刷屏；每会话代际独立；degraded/recovered 各一次；诊断含 session/generation/last result/累计失败数；不带敏感 payload；不把链路 degraded 写成业务失败、不把 recovered 写成「功能已修复」。
**入站 handler 异常**：消除静默吞——记录模块/频道/方向/generation/异常类型，不扩散其他订阅者、不打穿传输泵线程；本票不新增公开 Logger 成员（诊断走现有内部 seam，T8 裁决诊断包与生态 Logger interface）。
**LIT 迁移**：保留业务重试/挑战重臂/整理结果处理；可移除私有平台告警限频与链路 degraded/recovered 重复诊断。原则：**平台报告链路健康，功能处理业务重试**。

### 4. 绑定状态公开面 = 不加查询 interface

继续用现行公开面（Sessions/RegisterChannel/Subscribe/三发送方法/NetworkSendResult）。不新增 IsNetworkReady()/GetBindingState()/GetTransportHealth()——SendResult 已表达单次操作可行性、查询易造「查询与发送时刻脱节」的浅 interface。文档明确：Sessions 不是底层传输连接全集、只含已建立 BUE 会话；SendResult 是单次操作结果；链路健康经结构化诊断可观察。

### 5. 实施深度 = 现状加固五件套

①平台保底限流（新 Throttled 结果+官方/生态统一+按代际隔离+红测覆盖首条/连续超限/恢复/跨代际清零）；②平台链路健康（LIT 平台级失败限频迁入 BueNetwork，degraded/recovered 电平式诊断，阈值 10 先例，业务状态不搬进 Network）；③入站异常诊断（消除静默吞、保持隔离、不重抛回泵线程、不加 Logger 公开面）；④主线程 dispatcher 服务（泵线程语义冻结+LIR 迁移+队列容量可测+停止清理+禁自建泵）；⑤SDK 契约登记（入站线程/dispatcher 用法/发送结果语义/Sessions established-only/限流与失败诊断/网络与事件总线职责边界）。

### 6. 留到后续

可靠通道分档；LHT 双可靠度统一平台策略（**Update/Clear 可靠度仍是功能语义，不偷换成平台可靠性等级**）；认证加密；跨服中继；入站回调改主线程；上游连接/断线检测根治；健康阈值用户可配置化；带宽预算与优先级调度；完整诊断包自动化归 T8。

### 7. 契约影响 = Minor 加性

新增/登记：`NetworkSendResult.Throttled`、主线程 dispatcher 公开 seam、入站回调线程语义、限流/失败可见化/恢复语义、Sessions 与单次 SendResult 边界说明。与 T2/T3/T4 同批共用 **2.1**，晚则顺延 **2.2**。不改变既有结果值语义/频道注册/订阅 interface/SendToClients established-only/BUE 帧与命名频道身份/BueNetwork 应用层定位。**Throttled 必须是旧模块可安全处理的加性结果；SDK 要求旧模块对未知结果保持安全降级，不得把未知枚举当成功**。

### 8. 同权检验（四条）

①官方同样受保底限流：红测「官方模块高频发送→同样 Throttled」（不只限生态）；②官方先行消费 dispatcher：LIR 迁移证明 泵线程→BUE dispatcher→LIR 主线程业务，生态用相同 interface；③NoOp 契约 probe 扩链：RegisterChannel→Subscribe→Send→结果分支→Throttled 可观察→入站 dispatcher 可用→handler 异常有诊断→停止后投递失效；④官方既有消费者证据保留（LIT 命名频道发送/LIR 请求响应主线程处理/LHT 会话驱动组播+HostTick）——BueNetwork 不是生态旁路，是官方自用公共 seam。

### 本票不做

不改入站回调线程；不做可靠等级平台化；不做认证加密；不做跨服中继；不根治 LMN/SPF 上游断线检测；不把业务退避上收；不新增网络状态查询 API。

## Comments
