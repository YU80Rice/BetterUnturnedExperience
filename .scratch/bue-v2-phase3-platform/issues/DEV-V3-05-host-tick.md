# DEV-V3-05：宿主时钟语义登记（零新增契约面+红测补齐+自节流登记）

Type: task
Status: resolved（2026-09-10，双轴 R2 双 CLEAN 闭环，commit 8e8e3b0；审计=audit/2026-09-10/DEV-V3-05/结单报告.md）
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

## Comments

### 本票定案（2026-09-10 /implement 开工定音，票面授权自定项）

**八条语义登记（SDK 附录 A「HostTick」节条目正文；落档=DEV-V3-08）与红测锚映射**
1. **宿主产生与身份（防伪造）**：Tick 只能由宿主经保留身份 `io.github.yu80rice.bue.host` 产生；`Publisher(hostId)`/`EventRegistry(hostId)` fail-fast 不可 mint。既有锚=DEV-V2-19「宿主身份保留」组 + DEV-V3-02 保留身份组，本票维持回归。
2. **每拍恰一 tick（去重归宿主）**：每泵拍最多一个；同帧多驱动源去重=宿主 implementation 责任（`BueRuntimeTickChain` 帧哨兵），订阅者无需自行去重。既有锚=DEV-V2-19「生产接线」+ F-D 组「同帧去重」，本票维持回归。
3. **Phase 冻结**：`Phase=Update`、数值=0；更多 Phase=契约登记显式加性扩展，不得隐式增加。既有锚=TickPhase 值冻结断言；本票 Contracts.Tests 新锚=枚举恰一成员（隐式扩展必红）。
4. **序号**：从 1 起严格单调 +1、时钟生命周期内不重置、功能不可修改、模块代际变化不改变序号语义。既有锚=DEV-V2-19「假时钟单调」；本票新锚=失败拍不消耗序号+跨暂停推进（生产真时钟侧）。
5. **DeltaTime**：相邻 tick 单调时差；首拍 0；回拨/负差钳零且基线停留高水位；**暂停期间无 Tick（无拍即无产生）；恢复后下一拍 DeltaTime=实际间隔、无追帧；是否忽略大间隔由功能自决**。既有锚=R1/R3 修复钉；本票新锚=「暂停恢复语义」组 + LHT 经真时钟大间隔只一帧。
6. **载荷范围**：只含 Sequence/DeltaTime/Phase 三字段——不得携带业务字段/网络消息/设置值/玩家状态/功能命令/诊断载荷。本票新锚=Contracts.Tests `HostTick` 恰三属性形状+值类型+身份串冻结（隐式加字段必红）。
7. **异常语义**：派发不把订阅者异常抛回泵调用方；时钟自身故障=**显式 false 返回 + 结构化诊断行（码族 BUE-CLOCK-001，本票定案：`event=host-tick result=failed errorType=... message=... diagnosticId=BUE-CLOCK-001`）+ 零派发 + 失败拍不消耗序号/不移动时间基线**。既有锚=订阅者异常隔离组；本票新锚=「Tick 失败不扩散与结构化诊断」组（失败行为红→补齐码族转绿）。BUE-CLOCK-001=宿主观察行（非拒绝码语义），不入附录 B 拒绝码表，登记口径同 BUE-MT-GEN/CREATED（04 先例）。
8. **主线程构造性保证**：时钟无内部线程/无隐藏队列——handler 在 `Tick()` 调用方线程上同步执行，单生产驱动=宿主 Update 链（`BueHostEventRuntime.TickOnce`←`BueRuntimeTickChain`）；生态可在回调内安全调用主线程限定 API。既有锚=F-D 链组；本票新锚=「主线程构造性保证」组（同线程+Tick 返回前派发完成）。

**自节流=官方推荐模式（SDK 登记条目正文，落档归 08）**：需要低频执行的功能订阅 HostTick 后在功能内部自节流——累计 DeltaTime 阈值模式或序号差值模式；先例=LHT 10Hz HUD（`HordePresentationAdapter.UpdateIntervalSeconds=0.1f` 实现常量，节奏时钟=宿主累计秒，无 Time.unscaledTime）；首拍 DeltaTime=0 需按「可能不干活」写码；回拨已钳零无需功能侧防御；模块停止清理自身节流状态；**禁止自建第二个 Unity Update 泵**（平台唯一泵）。派生低频时钟/per-feature 调度参数=需求信号雾区（Out of Scope，不预建）。

**同权三条（T6 裁决六）**：官方与生态同一 HostTick 订阅 seam（LHT 真订阅锚+NoOp 支线锚）；官方功能不得获得特殊 Tick 频率；生态功能不得被宿主静默过滤。

**红测补齐中揪出的接线缺陷（本票修复，具名）**：`BueFeatureStartRuntime.TrackEntry` 复用旧 entry 时不换 `Machine`/`Identity`/`Network`——对新 runtime 二次 `StartCatalog` 后，entry 仍指旧 machine，`StopEntry→BeginStop` 在旧 machine 记录上 `invalid-state` 被拒 → `CompleteStop/UnsubscribeAll` 全跳过 → **新功能代际的总线订阅泄漏过停止边界**（红=全套 Plugin.Tests 失败：停止后 probe 仍收拍；绿=TrackEntry 同步换绑当前代际装配三字段+停止边界自动注销锚钉死）。生产单次 StartCatalog 形态不可达该交错，属测试/热重载（同进程 reload 会二次组装）面的正确性缺陷。

**移交注记**：①八条语义+自节流条目正文 → DEV-V3-08 附录 A「HostTick」节；BUE-CLOCK-001 行格式 → 08 附录 B 宿主观察行口径；②NoOp probe 全链（Events 之后的 Network/Settings/Logger 顺序扩链）→ DEV-V3-08；本票=HostTick 支线最小形状；③可用性矩阵零行变化（本票零新增契约面）。
