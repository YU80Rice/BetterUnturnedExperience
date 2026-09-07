# DEV-V2-19 实施结单报告——契约件：TidyCompleted 功能事件 + HostTick 宿主时钟入 Contracts（条目⑤⑥）

日期：2026-09-07（/implement 会话）。工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-19-tidycompleted-hosttick-contract.md`（Blocked by DEV-V2-14 已 resolved）。规约：`docs/agents/output-review-loop.md`（Fresh-instance 规则版）红测先行 + 双轴独立审查 CLEAN 才交付。规格：`.scratch/bue-v2-phase2-official-adoption/spec.md`「LIT ↔ LIR：TidyCompleted 功能事件」（164-169 行）与「宿主时钟」（186-191 行）；决策票口径 04-lit 决策6 / 05-lir 决策4 / 06-lht 决策7。

## 交付

- **契约⑤ `TidyCompleted`（`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`）**：只读 struct 入冻结面——跨功能协作唯一公开缝，官方/生态同权。载荷五要素冻结：`Publisher`（发布者 FeatureId）、`FirstPage`/`LastPage`（整理范围=连续页区间含端点）、`Result`（`TidyCompletionResult : byte { Succeeded=1, Rejected=2, Failed=3 }`）、`ConnectionGeneration`（0=不适用即本地路径；真实代际自 1 起，与运行时 SessionId 起点一致）、`TransactionId`（功能私有事务标识）。事件身份串 `TidyCompleted.EventId = "io.github.yu80rice.bue.inventory-tidy/tidy-completed"`，按派生规则 `<发布者 FeatureId>/<事件名>` 冻结。
- **契约⑥ `HostTick`（同文件）**：只读 struct 入冻结面——功能模块唯一帧级驱动缝。`TickNumber`（单调 ulong 从 1 起）/`DeltaTime`（float 秒，相邻 tick 单调时差）/`Phase`（`TickPhase : byte { Update = 0 }`）。`HostTick.EventId = "io.github.yu80rice.bue.host/host-tick"` 由平台宿主标识派生。载荷只含时间与序号，零功能逻辑。
- **宿主事件总线（`src/BetterUnturnedExperience.Core/Events/FeatureEventBus.cs` 新建）**：冻结契约缝 `IOwnedFeatureEventPublisher`/`IFeatureEventSubscriber` 的宿主组合。发布者视图按功能身份铸造，身份串必须由**本功能** FeatureId 派生（`<owner>/<event-name>` 校验，含 owner 空/前缀恰等/空事件名边界）；**宿主标识为总线保留身份**——`Publisher(宿主标识)` 参数异常 fail-fast，时钟经 `TryPublishHost` 内部路径发布（同派生规则），伪造面封死。订阅句柄独立幂等（同委托两订阅各一份派发、重复 Dispose 安全）、null handler fail-fast、锁内快照锁外派发、单 handler 异常不扩散且浮出结构化诊断（实例注入诊断缝，不静默吞）、`UnsubscribeAll(owner)` 只摘本功能订阅。
- **宿主时钟（`src/BetterUnturnedExperience.Core/Events/HostTickClock.cs` 新建）**：每泵拍恰一 `HostTick`（Phase=Update 固定）；序号严格单调 +1 永不重置；DeltaTime=单调时差（首帧 0；时间源回退钳 0 且**基线保持高水位**，R3 修复）；默认时钟按 `Stopwatch.Frequency` 标定（纯函数 `MonotonicMilliseconds(timestamp, frequency)` 可钉测）；`Tick()` 永不向泵抛出。主线程保证=唯一生产驱动为插件 Update 链（时钟自身零线程，main-thread-only 声明沿 DEV-V2-18 先例）。
- **插件组合根（`src/BetterUnturnedExperience.Plugin/BueHostEventRuntime.cs` 新建，internal）**：Awake 一次成型（总线+时钟+诊断缝绑 `BueRuntimeLog`），Update 泵驱动 `TickOnce()`（组合级 belt，永不逃逸 Update 链），`Clear` 仅测试缝。插件 `BetterUnturnedExperiencePlugin.cs` 两处接线（Awake `EnsureCreated`、Update 链 `TickOnce`）。
- **冻结面变更登记条目⑤⑥（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`）**：随 2.0 契约版本追加，与实现零漂移；条目编号裁定（票面「④⑤」→实际⑤⑥，③④已被 DEV-V2-16 占用，顺序追加制）在工单 Status/Scope/Comments 三处留痕。
- **显式编译清单三处登记**：Core.csproj（Events 两件）、Plugin.csproj（EmbeddedCore 链接两件 + BueHostEventRuntime）。

## 红绿链（证据留盘 audit/2026-09-07/DEV-V2-19/）

- **R1 轮**：红0 编译红——`--bue-v2-event-clock-red` 锚点引用未存在面 → 96 错（CS0246×50/CS0234×24/CS0103×22，`red-compile-build.log`/`red-compile-errors.log` 逐字）；红1 桩级运行时红——惰性桩（总线不派发/时钟不产针）后收集式 **19 条**（`red-runtime-transcript.log`，七组均有失败）；绿——实现落地后七组 ALL GREEN（`green-event-clock-transcript.log`）+全套 7/7 PASS（`green-*.Tests.log`×7）+0 警告（`green-stage-build.log`）。
- **R1 双轴**：Standards **CLEAN**（SMELL 4 条不阻断+INFO 3）；Spec **NOT CLEAN**（DEVIATION 2 + GAP 1）。
- **修复轮 F1-F4**（行为先红：`fix-round-red-transcript.log` 保留门 1 条）→ 八组 ALL GREEN（`fix-round-green-transcript.log`）+7/7 PASS+0 警告：
  - F1（Spec DEV-1）：默认时钟换算按 `Stopwatch.Frequency` 标定（原式仅在 10MHz 频率下凑对）。
  - F2（Spec DEV-2）：宿主标识升级为总线保留身份+内部宿主路径（原实现任何代码可 `Publisher(宿主标识)` 伪造时钟）。
  - F3（Spec GAP-1）：「停止自动注销」交接缝具名（接口=`UnsubscribeAll`、调用点=`IFeatureModule.Stop` 返回后、归属 DEV-V2-21/22），工单 F3+SDK ⑤ 双记录，非悬空。
  - F4（Standards SMELL1-3）：组合根转 internal（IVT 核实在案）、诊断缝改实例注入（静态字段撤除，测试泄漏面消失）、负时间回退钳 0 钉红测。
- **R2 双轴**（全新实例）：Spec **CLEAN 零发现**；Standards **CLEAN**（SMELL 1 条不阻断：换算钉仅 10MHz 样本，封不死「忽略 Frequency」回归）。
- **round3**：补非 10MHz 换算钉（`MonotonicMilliseconds(3_000_000, 3_000_000)==1000`——旧式固定除 10^7 在此必红）+工单验收行编号同步（INFO-3）→ 八组 ALL GREEN（`round3-transcript.log`）+7/7 PASS+0 警告。
- **R3 双轴**（全新实例）：Standards **CLEAN 零 SMELL**（INFO 3）；Spec **NOT CLEAN**（DEVIATION-1：时间源回退后基线被无条件拉低，后续 tick 产生虚假正增量——「相邻 tick 单调时差」语义被破坏）。
- **R3 修复轮**：高水位基线（仅正差推进 `lastMilliseconds`）+回退后第二 tick 断言——先红（`fix2-red-transcript.log` 1 条）后八组 ALL GREEN（`fix2-green-transcript.log`）+全套 7/7 PASS（`fix2-*.Tests.log`×7）+0 警告（`fix2-build2.log`）。
- **R4 双轴终审**（全新实例）：Standards **CLEAN**（零阻断零 SMELL，INFO 4）；Spec **CLEAN 零发现**。**双轴最终 CLEAN（Standards=R4，Spec=R4）**。

## 候选身份

`BetterUnturnedExperience.dll` SHA-256 `688253d8f30aaa3f9be185c2b85bc6454e0c810bb6d4782212b04e0bd9d19241`（370688 字节，R4 后两轮 `-t:Rebuild` 逐字节一致，`identity-rebuild1.txt`/`identity-rebuild2.txt`）。不继承 14-18 发布批准；RELEASES 换标随实机验收（DEV-V2-24 或用户实机验收）。

## 与既有测试的相容性核查

- DEV-V2-14 契约面（Subscribe/Network 注入形状钉）：零接触，Contracts.Tests 全绿。
- DEV-V2-15 LIT 单人路径：`InventoryTidyModule.LastLocalOutcome` 保留原状（TidyCompleted 真实发布接线属 DEV-V2-21），全绿。
- DEV-V2-16/17/18 网络面：零接触（本票不触网络运行时），全绿。
- Plugin.Tests 主链新增 `AssertBueV2EventBusAndHostTick()`（八组，默认套件内回归）。

## 具名延期 / 边界声明

1. **「停止自动注销」宿主路径接线随 DEV-V2-21/22**：本票交付总线级机制与语义红测（`UnsubscribeAll` 只摘本功能、注销后时钟照常），宿主模块 Start/Stop 路径尚不存在（DEV-V2-14 具名边界沿袭——FeatureBootstrap 生产构造与 Start 接线属 21/22）。交接契约已冻结并三处留痕（工单 Comments F3、`FeatureEventBus` 类注释、SDK 条目⑤）：宿主在 `IFeatureModule.Stop` 返回后调用 `UnsubscribeAll(feature)`——Stop 内最终发布仍可派发，边界后零存活。
2. **SMELL-4（派发按类型、授权按 EventId 的解耦）**：功能可用自有身份串发布 `HostTick`/`TidyCompleted` **实例**，类型订阅者会收到；防伪造按 EventId 所有权已满足（宿主标识不可铸入+派生校验），`TidyCompleted.Publisher` 供消费方再验。EventId↔TEvent 绑定若需要，归后续票（R1-R3 三轮独立具名一致）。
3. **`BueNetworkRuntime.DefaultMonotonicMilliseconds` 同型换算携带观察**：`GetTimestamp()/TicksPerMillisecond` 仅在 Stopwatch.Frequency=10^7 时精确——既有代码（DEV-V2-17 引入），Windows 10MHz 环境行为不变，握手退避容差（1s-8s）对此不敏感；本票不动既有审查面，随网络线票处理。
4. **LIT 真实发布 TidyCompleted 属 DEV-V2-21**（其 Scope 明列「TidyCompleted 发布：经 OwnedPublisher…服务器权威事务完成处发布」）；LIR 消费与自动压弹属 DEV-V2-22。本票交付契约件+总线+时钟+组合根，`LastLocalOutcome` 注释的「DEV-V2-19/21」预期中 19 侧职责以本结单为准收敛为 21。
5. **主线程模型声明**：时钟 main-thread-only（唯一生产驱动=插件 Update 链）；总线自身线程安全（锁内快照锁外派发）以支撑未来非主线程发布面，当前所有调用点均在主线程。

## 派发故障留痕

- 全部四轮（R1/R2/R3/R4）双轴共 8 次派发均为全新实例 spawn，无 SendMessage 续用、无持久会话复用；standards-reviewer 与 Spec-Reviewer 类型全程可用（DEV-V2-17 的兜底破例未触发）。
- R2-Standards SMELL-1 与 R3-Spec DEVIATION-1 的处置均按「先红后绿」补钉/修复（`fix-round-red-transcript.log`、`fix2-red-transcript.log` 行为红留证）。
