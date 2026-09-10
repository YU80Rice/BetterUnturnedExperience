# DEV-V3-02：FeatureEventBus 事件归属路由（三元组索引+类型归属登记 seam）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-01（注册桥与 Bootstrap 基线）
Spec: `../spec.md`（「功能事件（V3-T3 → DEV-V3-02）」节）

## What to build

功能事件路由存在防伪造的归属事实：发布者 owner 必须等于载荷类型的归属 owner，一个载荷类型恰对应一个归属 EventId；官方事件类型（TidyCompleted→LIT、HostTick→宿主保留身份）由宿主登记，生态作者经公开登记入口登记自己的事件类型后才能发布/订阅；未登记类型发布与订阅都被显式拒绝并给出诊断，事件协作不再有身份冒用与跨事件互收路径。

## Scope

- 内部路由索引=（EventId, EventType, 载荷类型归属 owner）三元组；eventId 前缀校验保留（`<owner>/<event-name>`）。
- 事件类型归属登记 seam：**公开契约面（Minor 2.1 加性，形状由本票定）**；登记时机=模块注册之后、发布/订阅之前；登记失败=显式结果+诊断，不抛越界异常；重复登记同一类型=显式拒绝不覆盖。
- 未登记类型：发布=显式拒绝+诊断+不调用任何订阅者；订阅=同样拒绝（开发期错误，同 null handler fail-fast 纪律）。
- 公开订阅 API 形状不变：泛型 `Subscribe<TEvent>` 保留为便利入口，内部按登记的（EventId, Type）路由。
- 两接口（IFeatureEventSubscriber/IOwnedFeatureEventPublisher）语义入冻结面；FeatureEventBus 类本体保持内部自由；不引入统一 envelope；事件总线=进程内本地（跨机走 BueNetworkApi）。
- 既有语义保持：锁外快照派发、单 handler 异常隔离进诊断、句柄幂等、停止边界 UnsubscribeAll。

## 验收条件

- [ ] 红测先行：归属拒绝（他人载荷类型）/前缀拒绝/登记成功后可发布订阅/登记失败显式结果/重复登记拒绝/未登记发布与订阅均拒/同类型唯一 EventId 不互收/宿主保留身份不可 mint，各先红后绿
- [ ] 既有总线语义回归绿（句柄幂等/异常隔离/UnsubscribeAll；先例=DEV-V2-19 总线测试组）
- [ ] 官方先行消费锚：LIT 经登记路径发布 TidyCompleted 真实消费断言
- [ ] 登记 seam 登记入冻结面变更清单（SDK 条目由 08 票总装落档，本票登记条目）
- [ ] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）
