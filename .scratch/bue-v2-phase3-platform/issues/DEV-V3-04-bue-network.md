# DEV-V3-04：BueNetwork 传输规则与主线程投递（发送预算+MainThread dispatcher+链路健康+回放投影）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-01（注册桥与 Bootstrap 基线）、DEV-V3-03（BueLifecycle）
Spec: `../spec.md`（「网络（V3-T5 → DEV-V3-04）」节）

## What to build

发送被平台每会话预算保底限流时作者收到显式 `Throttled`（能区分「平台节流」与「传输失败」并安全降级），不再有静默丢弃；主线程工作投递给统一 dispatcher（`bootstrap.MainThread`）后不再自建线程泵；会话链路 degraded/recovered 以电平式诊断呈现，能定位「发送持续失败」是链路问题；入站 handler 异常与 Deferred 回放失败都有结构化诊断可查。

## Scope

- 发送预算：平台按会话保底限流，超出=新 `NetworkSendResult.Throttled`（加性枚举值，旧模块须对未知结果安全降级）；预算随 ConnectionGeneration 隔离；官方生态一视同仁；不静默丢弃；窗口/令牌数/容量数值本票定（可观察可测试）。
- 主线程 dispatcher：**`IFeatureBootstrap` 新增 `MainThread` 成员**（类型名与成员名本票定；接线前 null、本票接线，入可用性矩阵）。最小行为面冻结：恰一个投递方法单向 fire-and-forget（任务不返回结果、无等待句柄）；投递返回显式结果（成功/容量拒绝/已失效可区分，调用不抛越界异常）；模块停止/隔离/宿主停止后投递=显式失败+诊断；超限=显式失败+诊断不静默丢；任务绑定提交时模块的 LifecycleGeneration，代际失效后未执行任务不再执行；执行期单任务异常隔离进诊断；禁自建泵。
- 会话链路健康：连续失败达阈值（默认 10，先例）→一次 degraded 诊断；成功恢复→一次 recovered+清零；每会话代际独立；阈值可配置留 Settings 域不在本票扩面。
- 入站 handler 异常从静默吞改为结构化诊断（不扩散、不打穿泵线程）；入站回调线程=传输泵线程（SDK 冻结登记，不做入站改主线程）。
- 回放失败投影：`DeferredBueNetworkApi` 的 Attach/replay/detach 失败进统一诊断 sink，可与「未就绪/重放失败/模块停止/传输不可用」区分；实现不得以空 catch 无痕折叠；不新增业务协议、不新增网络状态查询 API。
- 业务重试与退避不上收（LIT 挑战重臂保留功能私有）；LIT 告警限频被链路健康接管后退役。
- LIR 迁移为 dispatcher 官方先行消费者（官方先行消费锚）。

## 验收条件

- [ ] 红测先行：LocalLoopbackTransport 假传输驱动（Throttled 首条/连续超限/恢复/跨代际清零；链路健康电平 degraded/recovered；入站异常诊断；回放四态区分），各先红后绿（先例=LocalLoopbackTransport+DEV-V2-16 发送结果测试组）
- [ ] 矩阵接线两侧红测：MainThread 接线前 null+接线后可用；未知 NetworkSendResult 旧模块安全降级红测
- [ ] 官方先行消费锚：LIR 迁移 dispatcher 真实消费断言；LIT 告警限频退役后链路健康覆盖回归
- [ ] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）

## Comments

### 本票定案（2026-09-10 /implement 开工定音，票面授权自定项）

**契约形状（Minor 2.1 加性，逐条入冻结面清单）**
1. `NetworkSendResult.Throttled = 205`（ushort，结果组邻位；旧模块安全降级纪律=未知/新结果值不得当成功，红测锚）。
2. `IFeatureBootstrap` 第 12 成员 **`IFeatureMainThread MainThread { get; }`**（类型名与成员名本票定；矩阵行=DEV-V3-04 后可用）。
3. `IFeatureMainThread`：恰一个投递方法 `MainThreadPostResult Post(Action task)`——单向 fire-and-forget（无返回句柄；需结果走事件回发/网络响应）；null task=开发期错误 fail-fast（同 null-handler 纪律，先浮出 BUE-MT-004 诊断行再抛）。
4. `MainThreadPostReason : byte { None=0, CapacityExceeded=1, GenerationInvalid=2 }`（成功/容量拒绝/已失效三态可区分）；`MainThreadPostResult` 显式三元组 `Posted/Reason/DiagnosticId`（FeatureEventRegistrationResult 先例）。
5. 诊断码族：**BUE-NET-001** 预算拒绝(Throttled)/002 链路 degraded/003 recovered/004 入站 handler 异常/005 回放失败投影（reason 四态=not-ready/detached/replay-failed/transport-unavailable）；**BUE-MT-ACCEPT** 投递成功/001 容量拒绝/002 代际或作用域失效/003 执行期单任务异常隔离/004 无效任务/005 非主线程泵拒绝/006 宿主泵组合带失败（R1-Standards deferrable 补具名）。另有宿主内部观察行 BUE-MT-GEN（开代际）/BUE-MT-CREATED（组合根）——非拒绝语义、不入附录 B 拒绝码表（R2-Spec 文档完整性条具名递延 08）。

**数值（本票定，可观察可测试）**
- 发送预算：**每会话（ConnectionGeneration）固定窗 2000ms 内 256 条数据发送**；超窗即重置；SendToServer 记账于已建立快照的会话（客户端拓扑单服务器对等）；控制帧/握手帧不过预算（平台内部流量，预算管作者数据发送）。
- dispatcher 队列：**全局容量 256 待处理任务；每拍至多执行 32 任务**（LIT 先例 200/10 量级按平台流量放大，判断题具名）。
- 链路健康：连续传输失败阈值 **10**（DEV-V2-25 先例冻结）；Throttled/参数门拒绝不计入失败（未执行≠传输失败）；PartialFailure 的失败目标计该会话失败、送达目标计恢复。
- 聚合规则扩展（既有冻结语义不动）：全送达→Sent；全传输失败→LocalTransportUnavailable；混合（含被节流未执行）→PartialFailure（不静默吞）；纯节流（无执行无失败）→Throttled。

**实现落位**
- 预算+健康+入站诊断在 `BueNetworkRuntime`（ctor 加性尾参 `Action<string> diagnosticSink = null`；判定在锁内、诊断行全在锁外——DEV-V3-02 F1 纪律）；账本新文件 `Core/Network/NetworkSendGuard.cs`（internal）。
- dispatcher 本体新文件 `Core/Dispatch/MainThreadDispatcherRuntime.cs`（public=宿主组合面，非 SDK 契约）；Plugin 组合根 `BueMainThreadRuntime`（BueHostEventRuntime 先例）；泵挂在 `BueRuntimeTickChain.Tick()` 的 HostTick 之后（同一宿主主线程泵链，禁自建泵的平台侧兑现）；构造线程=主线程守卫。
- 代际绑定=提交时视图携带 (owner, generation)；`OpenGeneration` 换代即撤旧代未执行任务（显式诊断）；停止/隔离/宿主停止=`InvalidateOwner/ShutdownHost`→投递显式失败+pending 不执行；BueFeatureStartRuntime 在 Start/enable/stop/isolate/StopAll 各边界接线。
- 回放投影在 `DeferredBueNetworkApi`（ctor 加性可选 sink；NetworkModuleAdapter 绑定既有 DiagnosticLogSink）；not-ready/detached 行为每 episode 一条（防用户路径逐帧刷）；全部空 catch 消除。
- LIT：`LitSendFailureRateLimiter` 退役（重臂退避 `LitChallengeRearmBook` 业务保留）；TrySendToSession 的逐帧 WARN/BUE-LIT-003 上下抛删除，链路事实=平台 BUE-NET-002/003；DEV-V2-25 测试组按此更新（退役回归锚）。
- LIR：`NetService.Drain()` 每拍直跑改为主线程节拍上 `bootstrap.MainThread.Post(drainOnce)`（业务队列/合并/TTL/摘要=功能私有保留）；`MainThread==null`（未接线宿主/既有夹具）回退直跑=旧语义（矩阵阶段基线侧的模块侧容忍）；官方先行消费锚=真模块+真 dispatcher：入站泵线程帧→执行只发生在 dispatcher 泵拍，Stop 后 pending 不执行。
- NoOp 生态对照最小延伸（ProbeState 记 MainThread 可得+投递受理），全链 probe 归 08。
