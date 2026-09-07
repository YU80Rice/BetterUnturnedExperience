# DEV-V2-19 R3 Standards 轴审查

## 总判词：CLEAN

审查对象：`audit/2026-09-07/DEV-V2-19/round3-increment.diff`（11 文件，含 3 新文件）及工作区当前全量。全新实例，无前轮上下文，不沿用 R1/R2 判词。对照工单（含 Status 编号裁定与 Comments F1–F4）、规格「TidyCompleted / 宿主时钟」、DEV-V2-14 Subscribe 冻结注释、`BueNetworkRuntime` 注入时钟先例。

坏味道四轴（Mysterious Name / Duplicated Code / Feature Envy / Data Clumps）：增量内无达到报告阈值的实例。`TryPublish` / `TryPublishHost` 的派生谓词同构是「同一规则、两条身份」的对称实现，诊断事件名不同（`feature-event` / `host-event`），不是逻辑分叉；载荷字段是冻结 struct 本身。

R1 SMELL-1/2/3 与 R2 SMELL-1（10MHz 样本封不死「忽略 Frequency」）均已落地。R1 SMELL-4 仍在，工单 Comments 具名延期，本轮不新开、不计入 SMELL。

---

## A. 冻结语义：FeatureEventBus

**身份派生校验（`<owner>/<event-name>`）——通过。**

`TryPublish`（`FeatureEventBus.cs` L97–108）拒绝：`owner.Value` null/空、`declaredEventId` null/空、不以 `owner.Value + "/"`（Ordinal）为前缀、长度恰为 `owner.Length+1`（空事件名）。

边界推演：

| 输入 | 结果 |
| --- | --- |
| owner=null/"" | 拒 |
| eventId=null/"" | 拒 |
| eventId == owner（无斜杠） | `StartsWith(owner+"/")` 失败 → 拒 |
| eventId == owner+"/" | 长度闸 → 拒 |
| owner=`io.foo`，id=`io.foo.bar/x` | 前缀恰等点号扩展 → 拒 |
| LIT 发 `TidyCompleted.EventId` | 过 |

功能发布者发 `HostTick.EventId` 被拒（组「发布者语义」钉死）。空 owner 可铸 publisher，落入 `TryPublish` 空闸，不误伤保留门。

**宿主标识保留门 + 内部路径同规则——通过。**

- `Publisher(owner)`：`owner.Value == HostTickClock.HostPublisherId` → `ArgumentException` fail-fast（L67–68）。字符串 `==` 即 Ordinal。空 owner 不是保留身份。
- `TryPublishHost`（L115–125）派生规则与功能路径同构：必须以 `HostPublisherId + "/"` 为前缀且事件名非空。`HostTick.EventId` 满足。内部路径 `internal`；组合根亦 `internal`。第三方拿不到宿主总线，也不能铸宿主 publisher。组「宿主身份保留」钉门与内部产针互不干扰。

**UnsubscribeAll / 句柄 / 快照 / 异常隔离——通过。**

- `UnsubscribeAll` 按 `Owner.Value` Ordinal 逆序摘除，只动本功能；空 owner 直接 false（L81–95）。组「停止注销」钉：本功能停收、其它功能不受影响、无订阅返回 false。
- `SubscriptionHandle.Dispose` 按引用 `List.Remove`；二次 Dispose / 注销后再 Dispose 为 no-op（L198–207）。两次 `Subscribe` 两条记录，独立派发。
- 锁内快照 `Action<object>[]`，锁外 foreach；handler 内再订阅/注销不持锁死锁，在途派发按快照完成。
- handler `catch` 后 `EmitDiagnostic`（`result=handler-error`），不回传发布者；sink 自身异常再吞。null handler → `ArgumentNullException` fail-fast，对齐 DEV-V2-14 Subscribe 注释块（`ContractTypes.cs` L348–356）。
- 派发按 `typeof(TEvent)`、授权按 EventId——接口冻结形如此；工单具名延期 SMELL-4。

---

## B. HostTickClock

**通过。3MHz 钉真正封死「忽略 Frequency」回归面。**

- `tickNumber++` 后发布，首帧 1，其后严格 +1；失败不回滚序号（后续仍单调）。寿命内不清零。
- 首帧 `started` 闩，`DeltaTime=0`；其后 `diff<0` 钳 0，再 `/1000f`（秒）。组「假时钟单调」钉正增量、零增量、回退钳 0 且序号仍 +1。
- 默认时钟：`MonotonicMilliseconds(GetTimestamp(), Frequency)` = `ts * 1000L / freq`；`freq<=0` fail-fast。纯函数。
- 换算钉组合：
  - `(10_000_000, 10_000_000)==1000` 与 `(5_000_000, 10_000_000)==500`：10MHz 下与旧式 `ts / TimeSpan.TicksPerMillisecond`（`ts/10000`）数值重合，单独封不死「忽略 Frequency」。
  - round3 新增 `(3_000_000, 3_000_000)==1000`：旧式 `ts/10000` → 300；硬编码 10MHz `ts*1000/10_000_000` → 300。两路「忽略实际 Frequency」在此必红。与 0.5s 钉合取后，常数 1000 / 先整除再乘 1000 亦被挡住。
- `Tick()` 全包 try/catch，诊断 `event=host-tick result=failed`，永不抛向泵。`monotonicMilliseconds()` 抛在 `started` 赋值之前，失败帧不脏状态。
- 自身不建线程；生产唯一驱动 = 插件 `Update` → `BueHostEventRuntime.TickOnce`（`BetterUnturnedExperiencePlugin.cs` L238–241，与 DEV-V2-18 `TickNetwork` 同链）。主线程声明与 DEV-V2-18 引擎绑定同构。
- 「统一产生」由保留门 + `TryPublishHost` 强制。载荷仅 TickNumber / DeltaTime / `TickPhase.Update=0`。

---

## C. BueHostEventRuntime

**通过。**

- `internal static class`（L17）。Plugin.Tests 已有 `InternalsVisibleTo`（`Plugin/Properties/AssemblyInfo.cs` L3）。第三方不可见组合根，保留身份从外部不可达。可见性与 `NetworkModuleAdapter` internal / `BueRuntimeHost.Clear` internal 对齐；`BueRuntimeHost` 本身 public 是因为第三方登记桥，本类型无此理由。
- `EnsureCreated`：`bus != null` 早退 false；首次组总线+时钟并绑实例 sink，true。一次成型，同构 `ArmBueRuntimeIfDecided` 的主线程闩（无额外锁）。
- `TickOnce`：clock null → false；`Tick()` 外包 catch + `BueRuntimeLog.Runtime`，双层永不向 Update 抛。
- `Clear` 注释写明仅测试缝、生产拆除不调用；实现丢 bus/clock 引用。诊断缝是每总线实例注入，Clear 后无静态 sink 泄漏。诚实。
- Awake：`BueRuntimeLog.Bind` 后 `EnsureCreated`；Update 在 `TickNetwork` 之后 `TickOnce`。headless 同样走 Update——U3DS 仍要帧驱动。

---

## D. 与先例一致性

**通过。诊断缝与换算差异均有据且注释诚实。**

| 项 | 先例 | 本增量 |
| --- | --- | --- |
| 注入时钟缺省 | `BueNetworkRuntime.DefaultMonotonicMilliseconds` = `GetTimestamp() / TimeSpan.TicksPerMillisecond` | **有意不同**：`MonotonicMilliseconds(ts, Frequency)`。工单 Comments F1 登记网络线同型换算为携带观察，本票不动 |
| 构造注入 | `Func<long> monotonicMilliseconds = null` → `?? Default…` | 同构 |
| 诊断缝 | `NetworkModuleAdapter.DiagnosticLogSink` = `internal static Action<string>` | **有意不同**：构造参数实例注入（F4）。无静态泄漏面 |
| 测试缝 Clear | `BueRuntimeHost.Clear` = internal | 同构 internal |
| 组合根可见性 | `NetworkModuleAdapter` = internal | 同构 internal |
| 句柄模型 | DEV-V2-14 Subscribe：独立幂等句柄、锁外、单 handler 不扩散、null fail-fast | 同构 |
| 注释/风格 | DEV-V2-14/16/18 英文冻结注释 + 票号 | 同构 |

F1 换算差异正当：HostTick 的 DeltaTime 是冻结载荷（秒），必须按真实 Frequency 标定；网络运行时毫秒时钟是既有 Windows 行为，本票不顺手改。Comments 完整。

---

## E. 测试质量与红绿链

**通过。**

八组：事件发布订阅 / 发布者语义 / 异常隔离 / 假时钟单调 / 停止注销 / 宿主身份保留 / 生态同权 / 生产接线。

- R1 红：`red-compile-build.log` 96 错，CS0246/CS0234/CS0103 均指向新面（TidyCompleted/HostTick/TickPhase/Core.Events）；`red-runtime-transcript.log` 收集式 19 条（桩级，七组均有失败）。绿七组：`green-event-clock-transcript.log`。
- 修复轮红：`fix-round-red-transcript.log` 恰 1 条——「宿主身份保留：宿主标识不可铸入公开发布者视图（fail-fast）」，保留门未实装时先红。绿：`fix-round-green-transcript.log` 八组 ALL GREEN；`fix-green-*.Tests.log` ×7 全 PASS。
- round3：`round3-build.log`「已成功生成。0 个警告 0 个错误」（TreatWarningsAsErrors）；`round3-transcript.log` 八组 ALL GREEN；`round3-*.Tests.log` ×7 全 PASS（Contracts/Plugin/Network/Placement/Settings/ClientUi/Release）。
- round3 3MHz 钉是对已绿纯函数的回归封闭，当前 HEAD 无法先红；静态推演旧公式必红（见 B）。不构成红绿链断裂——行为红已在 R1（换算函数存在）与修复轮（保留门）留证。
- 外部行为钉：TryPublish 真假、载荷保真、独立句柄、null handler fail-fast、诊断浮出、零订阅仍 true、异常不扩散、序号/Δt/Phase/负钳、UnsubscribeAll 隔离、生态同权+同 Owned 规则、EnsureCreated 幂等、TickOnce 不逃逸、保留门 fail-fast、内部路径不受门影响。
- Contracts.Tests 冻结值：`TidyCompleted.EventId`、`TidyCompletionResult` 1/2/3、`HostTick.EventId`、`TickPhase.Update=0`、值类型与属性类型——与实现/SDK 零漂移。
- `--bue-v2-event-clock-red` 收集入口与主套件调用均登记（`Plugin.Tests/Program.cs` L625–629 / L368）。

---

## F. 契约文档

**通过。SDK ⑤⑥ 与实现/工单验收行零漂移。**

- `TidyCompleted.EventId = "io.github.yu80rice.bue.inventory-tidy/tidy-completed"`；派生规则 `<发布者 FeatureId>/<事件名>`。
- 载荷 Publisher / FirstPage / LastPage / Result / ConnectionGeneration / TransactionId；枚举 Succeeded=1, Rejected=2, Failed=3；代际 0=不适用。
- `HostTick.EventId = "io.github.yu80rice.bue.host/host-tick"`；宿主标识 `io.github.yu80rice.bue.host` 为**保留身份**（`Publisher(宿主标识)` fail-fast，内部路径发布）。
- TickNumber 从 1 严格 +1；DeltaTime 首 tick 0；Phase=Update=0；每 Update 恰一 tick。
- 总线 Owned、锁外 handler、异常隔离+诊断、句柄幂等、`Tick()` 永不抛、禁自建 Update 泵。
- UnsubscribeAll 交接缝具名：接口=`FeatureEventBus.UnsubscribeAll`，调用点=`IFeatureModule.Stop` 返回后，归属 DEV-V2-21/22。工单 F3 与 SDK ⑤ 同步，非悬空。
- 编号：Status / Scope / 验收条件 / SDK 均为 ⑤ TidyCompleted、⑥ HostTick（round3 已把验收行「④⑤」改为「⑤⑥」）。

工单 Comments 具名延期完整：SMELL-4；`BueNetworkRuntime.DefaultMonotonicMilliseconds` 同型换算携带观察、本票不动。

---

## G. 常见坑

**通过。**

- 新文件无 `event` 成员，无 CS0067。
- Core IVT 仍仅 `Network.Tests`；Plugin.Tests 所用 `FeatureEventBus`/`HostTickClock` 为 public（同 `BueNetworkRuntime`）；`BueHostEventRuntime` / `TryPublishHost` / `Clear` 为 internal，靠 Plugin IVT。正当。
- 编译清单三处均登记：`Core.csproj` 两文件；`Plugin.csproj` EmbeddedCore 两链接 + `BueHostEventRuntime.cs`。Release/NoOpFixture 不嵌入，正确。生产列表无测试夹具类型。
- `round3-build.log`：0 警告 0 错误。

---

## 发现

### BLOCKING

无。

### SMELL

无（本轮新开）。R2 SMELL-1（10MHz 样本）已被 3MHz 钉关闭。

### 具名延期（不计入本轮 SMELL 计数）

**SMELL-4（R1，工单 Comments 延期）— 派发按类型、授权按 EventId。** 功能可用自有 EventId 发布 `HostTick`/`TidyCompleted` 实例，类型订阅者会收到。冻结接口即 `Subscribe<TEvent>` + `TryPublish<TEvent>(declaredEventId, …)`；防伪造按 EventId 所有权已满足。`TidyCompleted.Publisher` 可供消费方再验；`HostTick` 无发布者字段。后续票再绑 EventId↔TEvent。本轮仍存在，不新开。

### INFO

**INFO-1** `MonotonicMilliseconds` 的 `ts * 1000L / freq` 在 Frequency=10MHz 下约 29 年触及 `long` 乘积上界；非正 Frequency 已 fail-fast。游戏进程不可达，不单独立项。

**INFO-2** Contracts.Tests 未钉 get-only / `readonly struct` 的 `CanWrite=false`；实现确为 `readonly struct` + get-only。与 V2-14 形状钉粒度一致。

**INFO-3** 3MHz 钉钉在公开纯函数 `MonotonicMilliseconds`；`DefaultMonotonicMilliseconds` 是一行委托（`HostTickClock.cs` L82–85）。若 Default 被改回忽略 Frequency 而纯函数保持，钉仍绿。生产实现确委托纯函数，不阻断。

---

## 证据路径

- 工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-19-tidycompleted-hosttick-contract.md`（Status ⑤⑥；Comments F1–F4；验收行 round3 已同步）
- 规格：同目录 `spec.md` L164–169、L186–191
- 实现：`src/BetterUnturnedExperience.Core/Events/FeatureEventBus.cs`、`HostTickClock.cs`；`src/BetterUnturnedExperience.Plugin/BueHostEventRuntime.cs`
- 冻结面：`src/BetterUnturnedExperience.Contracts/ContractTypes.cs` L179–224；SDK `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` ⑤⑥
- 先例：`ContractTypes.cs` L348–356（Subscribe 注释）；`BueNetworkRuntime.cs` L125–140（注入时钟 / 旧换算）
- R1 红绿：`red-compile-build.log`（96 错）、`red-runtime-transcript.log`（桩级 19 条）、`green-event-clock-transcript.log`
- 修复轮：`fix-round-red-transcript.log`（保留门 1 条红）、`fix-round-green-transcript.log`（八组）、`fix-green-*.Tests.log` ×7
- round3：`round3-build.log`（0 警告）、`round3-transcript.log`（八组 ALL GREEN）、`round3-*.Tests.log` ×7
