# DEV-V2-19：契约件——TidyCompleted 功能事件 + HostTick 宿主时钟入 Contracts

Type: task
Status: resolved（2026-09-07，/implement 会话；R4 双轴最终 CLEAN——Standards=R4 零阻断零 SMELL、Spec=R4 零发现；候选 688253d8…9241；结单报告 audit/2026-09-07/DEV-V2-19/。登记条目编号裁定：票面「④⑤」系出票时序号预期——③④已被 DEV-V2-16 占用，按登记序列连续性本票追加为 ⑤ TidyCompleted、⑥ HostTick）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-14（契约 Major 升级与登记流程须先建立；可与 DEV-V2-16/17/18 并行）
Spec: `../spec.md`（「LIT ↔ LIR：TidyCompleted 功能事件」「宿主时钟」两节）

## What to build

官方与生态功能同权地消费「整理完成」事件与宿主时钟：跨功能协作走唯一公开缝（功能事件），帧级驱动走宿主统一时钟（只带序号与时间）——不再有官方私有通道，也不再有功能模块自建 Unity Update 泵。

## Scope

- `TidyCompleted` 事件类型入 Contracts 冻结面（规格定稿）：只读 struct，载荷至少含发布者 FeatureId、整理对象范围、完成结果、连接代际（如适用）、事务/操作标识；事件身份串由发布者 FeatureId 派生（具体命名实施期按契约登记流程定稿）。
- `HostTick` 宿主时钟入 Contracts 冻结面（规格定稿）：序号 + 时间增量 + 阶段；由宿主统一产生；载荷不含功能逻辑。
- 发布/订阅经既有事件总线（OwnedPublisher / Subscriber）；两者随 DEV-V2-14 的契约 Major 升级登记（条目⑤⑥——编号裁定见 Status 行，出票时的「④⑤」已被 DEV-V2-16 的 ③④ 让位顺延）。
- 宿主时钟六条不变性：统一产生、频率与阶段固定、主线程回调、序号单调、停止自动注销、单订阅者异常不扩散；功能模块禁自建 Update 泵。

## 验收条件

- [x] 红测先行：事件发布/订阅/载荷断言/发布者异常隔离；假时钟驱动下 HostTick 单调、阶段、停止注销、异常不扩散——先红后绿（编译红 96 错→桩级红 19 条→七组绿；修复轮保留门行为红 1 条→八组绿；round3 非 malignant 10MHz 换算钉；R3 修复轮回退基线红 1 条→八组绿）
- [x] 生态侧消费者同权演示（非官方注册路径订阅同一事件与时钟）——「生态同权」组 io.example.thirdparty 经同一公开缝收 TidyCompleted+HostTick，且受同一 Owned 规则约束
- [x] 冻结面变更登记条目⑤⑥追加（编号裁定见 Status 行）
- [x] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN（R1→修复→R2→round3→R3 修复→R4，每轮全新实例；双轴最终 CLEAN=Standards R4/Spec R4）

## Comments

- **2026-09-07 实施会话（/implement）**：R1 双轴——Standards CLEAN（SMELL 4 条不阻断）；Spec NOT CLEAN（DEVIATION 2 + GAP 1）。修复轮 F1-F4 全部落地：F1 默认时钟换算改按 `Stopwatch.Frequency` 标定（纯函数 `MonotonicMilliseconds(timestamp, frequency)` 钉 1s=1000ms/0.5s=500ms/非正 Frequency fail-fast；`BueNetworkRuntime.DefaultMonotonicMilliseconds` 的同型换算为既有代码、Windows 上行为不变，登记为携带观察项随网络线票处理，本票不动）；F2 宿主标识 `io.github.yu80rice.bue.host` 升级为总线**保留身份**——`Publisher(宿主标识)` 参数异常 fail-fast，宿主时钟改经总线内部宿主路径（`TryPublishHost`）发布，伪造面封死（修复轮行为红 1 条先红留证 `fix-round-red-transcript.log`）；F3「功能停止自动注销」交接缝具名：接口=`FeatureEventBus.UnsubscribeAll(owner)`，调用点=宿主模块 Start/Stop 路径（`IFeatureModule.Stop` 返回后调用，最终 Stop 内发布仍可派发、边界后零存活），随 DEV-V2-21/22 落地——本票以总线级红测钉住语义，宿主路径接线为具名移交非悬空；F4（Standards SMELL1-3）组合根 `BueHostEventRuntime` 转 internal、诊断缝改实例注入（构造参数，静态字段撤除，测试泄漏面随之消失）、负时间回退钳 0 钉红测。Standards SMELL-4（派发按类型/授权按 EventId 的解耦）具名延期后续票。修复后旗标 ALL GREEN（八组，`fix-round-green-transcript.log`）。
- **2026-09-07 闭单**：round3（R2-SMELL 修复：非 10MHz 换算钉+验收行编号同步）后 R3 双轴——Standards CLEAN 零 SMELL、Spec NOT CLEAN（DEVIATION-1：时间源回退后基线被拉低，后续 tick 产生虚假正增量）→ 修复轮（高水位基线：仅正差推进 `lastMilliseconds`；回退后第二 tick 断言先红 `fix2-red-transcript.log` 后绿）→ R4 双轴全新实例：Standards **CLEAN**（零阻断零 SMELL，4 INFO）、Spec **CLEAN**（零发现）。双轴最终 CLEAN（Standards=R4，Spec=R4）。全套 7/7 PASS 0 警告（fix2-build2.log + fix2-*.Tests.log×7）；候选 `688253d8…9241`（370688B 两轮 Rebuild 一致）。具名延期两项：①「停止自动注销」宿主路径接线随 DEV-V2-21/22（交接缝=UnsubscribeAll 接口+Stop-返回后调用契约，工单 F3+SDK ⑤ 双记录）；② SMELL-4（派发按类型/授权按 EventId 解耦）后续票。携带观察项：BueNetworkRuntime.DefaultMonotonicMilliseconds 同型换算（既有代码，Windows 行为不变，随网络线票处理）。
