# DEV-V3-05：宿主时钟语义登记（零新增契约面+红测补齐+自节流登记）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-03（BueLifecycle）
Spec: `../spec.md`（「宿主时钟（V3-T6 → DEV-V3-05）」节）

## What to build

宿主时钟的八条语义从实现事实升格为登记契约：生态作者按官方推荐的自节流模式实现低频逻辑（按序号/DeltaTime 节流），不期待 Hz 承诺或调度协商；任何功能都无法伪造 HostTick（宿主保留身份不可 mint）；时钟异常不扩散、主线程构造性保证有红测钉住。本票零新增契约面、不触发版本变化。

## Scope

- 八条语义登记+红测补齐：保留身份防伪造（Publisher 对宿主保留身份 fail-fast）；每拍恰一 tick 去重归宿主；Phase=Update=0（冻结值，后续 phase 属契约登记扩展）；序号从 1 严格单调；DeltaTime 回拨钳零（基线停留高水位）；载荷只含时序三字段（Sequence/DeltaTime/Phase）；Tick 异常不扩散（结构化诊断+false 返回）；主线程构造性保证（单生产驱动=Update 链）。
- 自节流=官方推荐模式（LHT 10Hz 先例）在 SDK 文档登记；派生低频时钟挂需求信号雾区（Out of Scope）。
- NoOp probe 的 HostTick 支线（订阅→收 tick→序号推进断言）。
- 不做：Hz 承诺；调度协商；派发过滤；派生时钟实现；BueThreading。

## 验收条件

- [ ] 红测先行：fake-clock 组（序号单调/回拨钳零/暂停恢复语义/首拍 DeltaTime=0/异常不扩散/保留身份不可 mint），各先红后绿（先例=DEV-V2-19 fake-clock 钉死组）
- [ ] SDK 登记条目建立（八条语义+自节流模式，落档归 08 票总装）
- [ ] 官方先行消费锚：LHT 作为既有消费者回归绿
- [ ] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）
