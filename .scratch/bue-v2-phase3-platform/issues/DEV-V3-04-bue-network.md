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
