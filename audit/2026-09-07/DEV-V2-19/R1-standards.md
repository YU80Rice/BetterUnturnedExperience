# DEV-V2-19 R1 Standards 轴审查

## 总判词：CLEAN

审查对象：`audit/2026-09-07/DEV-V2-19/round1-increment.diff`（10 文件，含 3 新文件）。全新实例，无前轮上下文。对照工单、规格「TidyCompleted / 宿主时钟」、05/06 决策票、DEV-V2-14 Subscribe 冻结注释、`BueNetworkRuntime` 注入时钟与 `NetworkModuleAdapter.DiagnosticLogSink` 先例。

坏味道四轴（Mysterious Name / Duplicated Code / Feature Envy / Data Clumps）：增量内无达到报告阈值的实例。命名与仓库一致；`EmitDiagnostic` / `DefaultMonotonicMilliseconds` 的短重复是先例同构而非逻辑分叉；载荷字段是冻结 struct 本身。

---

## A. 冻结语义：FeatureEventBus

**身份派生校验（`<owner>/<event-name>`）——通过。**

`TryPublish` 拒绝：`owner.Value` null/空、`declaredEventId` null/空、不以 `owner.Value + "/"`（Ordinal）为前缀、长度恰为 `owner.Length+1`（空事件名）。

边界推演：

| 输入 | 结果 |
| --- | --- |
| owner=null/"" | 拒 |
| eventId=null/"" | 拒 |
| eventId == owner（无斜杠） | `StartsWith(owner+"/")` 失败 → 拒 |
| eventId == owner+"/" | 长度闸 → 拒 |
| owner=`io.foo`，id=`io.foo.bar/x` | 前缀恰等点号扩展 → 拒 |
| owner=`io.foo`，id=`io.github.../host-tick` | 非本前缀 → 拒 |
| LIT 发 `TidyCompleted.EventId` | 过 |

功能发布者发 `HostTick.EventId` 被拒（红/绿组「发布者语义」钉死）。`Publisher()` 不把宿主标识加入保留黑名单——功能经冻结 `IOwnedFeatureEventPublisher`（绑定自身 FeatureId）无法宣称宿主身份；直接 `bus.Publisher(hostId)` 需碰到实现类型，见 SMELL-1。

**UnsubscribeAll / 句柄 / 快照 / 异常隔离——通过。**

- `UnsubscribeAll` 按 `Owner.Value` Ordinal 逆序摘除，只动本功能；空 owner 直接 false。
- `SubscriptionHandle.Dispose` 按引用 `List.Remove`；二次 Dispose / 注销后再 Dispose 为 no-op。
- 两次 `Subscribe` 两条记录，独立派发。
- 锁内快照 `Action<object>[]`，锁外 foreach；handler 内再订阅不持锁死锁。
- handler `catch` 后 `EmitDiagnostic`（`result=handler-error`），不回传发布者；sink 自身异常再吞。null handler → `ArgumentNullException` fail-fast，对齐 DEV-V2-14 Subscribe 注释。

派发按 `typeof(TEvent)` 不按 EventId。冻结接口本就 `Subscribe<TEvent>` + `TryPublish<TEvent>(declaredEventId, …)`。归属校验按工单/SDK 定义为 EventId 所有权，已强制。类型与 EventId 解耦见 SMELL-4（不阻断）。

---

## B. HostTickClock

**通过。**

- `tickNumber++` 后发布，首帧 1，其后严格 +1；失败返回 false 不回滚序号（后续仍单调）。
- 首帧 `started` 闩，`DeltaTime=0`；其后 `diff<0` 钳 0，再 `/1000f`。
- `Tick()` 全包 try/catch，诊断 `event=host-tick result=failed`，永不抛向泵。
- 自身不建线程；生产唯一驱动 = 插件 `Update` → `BueHostEventRuntime.TickOnce`（`BetterUnturnedExperiencePlugin.cs` 与 DEV-V2-18 `TickNetwork` 同链）。
- 以 `HostPublisherId` 取 publisher，发 `HostTick.EventId`；功能 publisher 发同一 EventId → 归属校验拒绝（「统一产生」按提示词定义成立）。
- 载荷仅 TickNumber / DeltaTime / `TickPhase.Update=0`。

负时间钳制实现有、测试未钉（SMELL-3）。`DefaultMonotonicMilliseconds` 与 `BueNetworkRuntime` 逐字同构（INFO-1）。

---

## C. BueHostEventRuntime

**通过（可见性见 SMELL-1）。**

- `EnsureCreated`：`bus != null` 早退 false；首次组总线+时钟并绑 sink，true。一次成型，同构 `ArmBueRuntimeIfDecided` 的 `networkRuntime != null` 闩（主线程泵，无额外锁）。
- `TickOnce`：clock null → false；`Tick()` 外包 catch + `BueRuntimeLog.Runtime`，双层永不向 Update 抛。同构 `TickNetwork` 分 stage catch。
- `Clear` 注释写明仅测试缝、生产拆除不调用；实现只丢 bus/clock 引用。诚实度：注释对，可访问性过宽（SMELL-1）；且不复位 `DiagnosticLogSink`（SMELL-2）。
- Awake：`BueRuntimeLog.Bind` 后 `EnsureCreated`；Update 在 `TickNetwork` 之后 `TickOnce`。headless 同样走 Update，时钟不停——符合 U3DS 仍要帧驱动、表现另降级。

---

## D. 与先例一致性

**通过，两处可见性不同构（SMELL-1/2）。**

| 项 | 先例 | 本增量 |
| --- | --- | --- |
| 注入时钟缺省 | `BueNetworkRuntime.DefaultMonotonicMilliseconds` = `Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond` | 逐字同构 |
| 构造注入 | `Func<long> monotonicMilliseconds = null` → `?? Default…` | 同构 |
| DiagnosticLogSink | `NetworkModuleAdapter` / `LmnV1CompatLayer`：`internal static Action<string>`，Emit 吞 sink 异常 | 形状同构；本票为 **public** 字段 |
| 测试缝 Clear | `BueRuntimeHost.Clear` = **internal** | `BueHostEventRuntime.Clear` = **public** |
| 组合根可见性 | `NetworkModuleAdapter` = internal | `BueHostEventRuntime` = public（`BueRuntimeHost` 因第三方登记桥才 public） |
| 注释/风格 | DEV-V2-14/16/18 英文冻结注释 + 票号 | 同构 |

---

## E. 测试质量

**通过。**

七组：事件发布订阅 / 发布者语义 / 异常隔离 / 假时钟单调 / 停止注销 / 生态同权 / 生产接线。

- 红：`red-runtime-transcript.log` 收集式 19 条，七组均有失败（含组内 UNEXPECTED `ArgumentOutOfRangeException`，桩态特征）。
- 绿：`green-event-clock-transcript.log`「ALL GREEN … 七组」；`green-*.Tests.log` ×7 PASS；`green-stage-errors.log` 空（0 警告，TreatWarningsAsErrors）。
- 断言钉外部行为：TryPublish 真假、载荷保真、独立句柄、fail-fast、诊断浮出、零订阅仍 true、异常不扩散、序号/Δt/Phase、UnsubscribeAll 隔离、生态同权、EnsureCreated 幂等、TickOnce 不逃逸。
- Contracts.Tests 冻结值：`TidyCompleted.EventId`、`TidyCompletionResult` 1/2/3、`HostTick.EventId`、`TickPhase.Update=0`、值类型与属性类型——与实现/SDK 零漂移。未钉 `CanWrite=false`（INFO-3，对齐 V2-14 枚举形状钉风格）。

编译红：`red-compile-errors.log` CS0246/CS0234/CS0103 均指向新面（TidyCompleted/HostTick/TickPhase/Core.Events）。

---

## F. 契约文档

**通过。**

SDK ⑤⑥ 与实现一致：

- `TidyCompleted.EventId = "io.github.yu80rice.bue.inventory-tidy/tidy-completed"`
- 载荷 Publisher / FirstPage / LastPage / Result / ConnectionGeneration / TransactionId；枚举 Succeeded=1, Rejected=2, Failed=3
- `HostTick.EventId = "io.github.yu80rice.bue.host/host-tick"`
- TickNumber / DeltaTime / Phase=Update=0；宿主标识 `io.github.yu80rice.bue.host`
- 总线 Owned 语义、锁外 handler、异常隔离+诊断、停止注销、禁止自建 Update 泵、Tick 永不抛

编号：票面 Status 已留痕——出票「④⑤」时 ③④ 已被 DEV-V2-16 占用，本票登记为 ⑤ TidyCompleted、⑥ HostTick。正当。工单 Scope 仍写「条目④⑤」（INFO-2），不与 SDK 冲突。

---

## G. 常见坑

**通过。**

- 新文件无 `event` 成员，无 CS0067。
- Core `InternalsVisibleTo` 仍仅 `Network.Tests`；Plugin.Tests 所用 Core 成员（`FeatureEventBus`/`HostTickClock`）均为 public。
- 编译清单三处均登记：`Core.csproj` 两文件；`Plugin.csproj` EmbeddedCore 两链接；`BueHostEventRuntime.cs`。无第四处遗漏。
- 生产列表无测试夹具类型；`Clear` 是测试缝方法挂在生产类型上，同构 `BueRuntimeHost.Clear`。

---

## 发现

### BLOCKING

无。

### SMELL（均不阻断）

**SMELL-1 — 组合根过宽 public。** `BueHostEventRuntime`（含 `Bus`/`EnsureCreated`/`TickOnce`/`Clear`）对第三方可见。先例：`NetworkModuleAdapter` internal，`BueRuntimeHost.Clear` internal；Plugin.Tests 已有 IVT，不必 public。后果：实现方可以 `Bus.Publisher(new FeatureId(HostPublisherId))` 取得宿主身份（绕过「功能发布者」路径）。冻结功能缝仍是绑定自身 FeatureId 的 `IOwnedFeatureEventPublisher`，DEV-V2-21/22 组合后功能模块拿不到 host publisher。

修复：类型及测试缝改为 `internal`（保留 IVT）。不阻断。

**SMELL-2 — DiagnosticLogSink 可见性 + Clear 泄漏。** 字段 public，先例为 internal。`Clear` 不置 `DiagnosticLogSink=null`，生产接线组结束后 sink 仍指向 `BueRuntimeLog.Runtime`。

修复：`internal`；`Clear` 复位 sink。不阻断。

**SMELL-3 — 负时间钳制无断言。** 实现 `if (diff < 0) diff = 0`；假时钟组只覆盖 Δt=0（时间戳不变）与正增量。

修复：`nowMs` 回退后断言 `DeltaTime==0` 且序号仍 +1。不阻断。

**SMELL-4 — 派发按类型、授权按 EventId。** 功能可用自有 EventId 发布 `HostTick`/`TidyCompleted` 实例，类型订阅者会收到。接口冻结形如此；「防伪造」按 EventId 所有权已满足。`TidyCompleted.Publisher` 可供消费方再验；`HostTick` 无发布者字段。

修复（后续票）：规范 EventId 与 `TEvent` 绑定，或 Subscribe 带 EventId。不阻断本票。

### INFO

**INFO-1** `DefaultMonotonicMilliseconds` 复制 V2-18 的 `GetTimestamp()/TimeSpan.TicksPerMillisecond`（QPC Frequency 非 10MHz 时速率不准）。本票要求同构，不单独立项。

**INFO-2** 工单 Scope 仍写登记「条目④⑤」；Status 已裁定 ⑤⑥。建议顺手改 Scope，非冻结面漂移。

**INFO-3** Contracts.Tests 未钉 get-only/`readonly struct`；实现确为 `readonly struct` + get-only。与 V2-14 形状钉粒度一致。

---

## 证据路径

- 工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-19-tidycompleted-hosttick-contract.md`
- 规格：同目录 `spec.md` L164-169、L186-191
- 实现：`src/BetterUnturnedExperience.Core/Events/FeatureEventBus.cs`、`HostTickClock.cs`；`src/BetterUnturnedExperience.Plugin/BueHostEventRuntime.cs`
- 冻结面：`src/BetterUnturnedExperience.Contracts/ContractTypes.cs` L179-224；SDK `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` ⑤⑥
- 红：`audit/2026-09-07/DEV-V2-19/red-compile-errors.log`、`red-runtime-transcript.log`
- 绿：`green-event-clock-transcript.log`、`green-*.Tests.log` ×7、`green-stage-errors.log`
