# DEV-V2-19 Spec 轴审查报告
## 总判词：NOT CLEAN

### BLOCKER
无。

### DEVIATION
1. **HostTick 生产 DeltaTime 计算错误。**
   - Spec 原文（`.scratch/bue-v2-phase2-official-adoption/spec.md:190`）："序号单调、携带时间增量"；登记冻结面（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:82`）要求 DeltaTime 为"相邻 tick 单调时差"。
   - 实现证据：`src/BetterUnturnedExperience.Core/Events/HostTickClock.cs:84-86` 用 `Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond`，未按 `Stopwatch.Frequency` 换算。该除数只在 Stopwatch 频率恰为 10,000,000 ticks/s 时成立，生产环境通常不是；因此发布的秒增量不符合冻结契约。建议使用 `timestampDelta / (double)Stopwatch.Frequency`，并将转换纳入运行时测试。

2. **宿主身份并未被总线保留，外部功能可伪造 HostTick。**
   - Spec 原文（`spec.md:189-190`）："时钟由宿主统一产生"，且"功能模块不得各自创建 Unity Update 泵"；登记条目（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:81`）明确"功能发布者无法以宿主身份发布（总线归属校验拒绝）"。
   - 实现证据：`src/BetterUnturnedExperience.Core/Events/FeatureEventBus.cs:48-52` 公开 `Publisher(FeatureId owner)`；`75-83` 仅校验事件串是否以传入 owner 开头。故调用 `Publisher(new FeatureId("io.github.yu80rice.bue.host"))` 即可通过 `HostTick.EventId` 校验并发布伪造时钟。应在总线拒绝保留宿主身份的功能 publisher，宿主时钟使用不可伪造的内部发布路径。

### GAP
1. **“停止自动注销”只有手工 seam，没有自动生命周期接线。**
   - Spec 原文（`spec.md:190`）："功能停止自动注销"；工单 Scope（`DEV-V2-19-tidycompleted-hosttick-contract.md:18`）同样冻结该不变性。
   - 实现证据：`FeatureEventBus.cs:59-72` 仅提供调用者主动执行的 `UnsubscribeAll`；`src/BetterUnturnedExperience.Plugin/BueHostEventRuntime.cs:55-64` 的 `Clear` 只清空总线/时钟引用，未注销功能订阅；`BetterUnturnedExperiencePlugin.cs:355-394` 也未把事件总线注销接入模块 Stop。现有测试（`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:715-733`）是手动调用 `bus.UnsubscribeAll(litFeature)`，不能证明“停止自动”。若按边界由 DEV-V2-21/22 接线，应在本票明确交接接口/调用点并补集成测试，避免冻结不变性悬空。

### SMELL
无。

### INFO
- 证据中的七组事件/时钟测试宣称 ALL GREEN（`audit/2026-09-07/DEV-V2-19/green-event-clock-transcript.log:1`），七个既有测试日志均 PASS；但这不能覆盖上述 Stopwatch 换算与宿主身份伪造边界。
