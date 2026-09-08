# DEV-V2-20 红绿证据链

锚点：`--bue-v2-lht-red`（tests/BetterUnturnedExperience.Plugin.Tests/Program.cs，AssertBueV2LhtAdoption，六组收集式：信标守卫/组播不含本地/BUE 帧不可靠 1:1/enabled=false 完整停摆/U3DS 双状态/端到端全链）。

## 红链（先红）

1. **编译红（独立锚点）**：`compile-red.log` — 32 错（CS0234 Lht 命名空间不存在 + CS0246 六个 LHT 类型未定义 + CS0104 Action 二义性 2 条为测试代码自身修正）。六组红测先于实现落盘，实现在观测编译红之后写入。
2. **运行时红（环境缝，非行为红，如实留痕）**：实现接入后首次跑锚点，六组中组 1（信标守卫）/组 3（不可靠 1:1，直构 HordeTrackingModule）绿；组 2/4/5/6（经 HordeTrackerModule.Start 的组）UNEXPECTED `SecurityException: ECall 方法必须打包到系统模块中 @ HordeTrackerModule.get_CanUseClientUi`。
   - 根因：Mono 对含未解析 ECall（`Application.isBatchMode`）的方法在 **JIT 编译期**即抛 SecurityException，与运行分支无关——probe 分支已设仍炸。UNEXPECTED 首帧堆栈定位后确认。
   - 修复：引擎调用隔离进 `ReadBatchMode()`（`[MethodImpl(NoInlining)]`），测试路径永不 JIT 该方法（`HordeTrackerModule.CanUseClientUi` 缝）。该红为测试基建环境事实，不是行为缺陷；修复属缝实现非产品行为变更。
3. **CS0649 转正**：`FakeHudSurface.ThrowOnRender` 只读未写触 TreatWarningsAsErrors——把计划中的「HUD 失败只降表现」隔离断言补入组 5（throwing surface → adapter.Tick 不抛），行为钉死与警告归零一并完成。

## 绿链（后绿）

- `impl-green-run.log`：`DEV-V2-20 LHT adoption collection: ALL GREEN (0 failures) — 六组`（ECall 缝修复后首绿）。
- 解决方案 Rebuild：`sln-rebuild-fullsuite.log` — **0 错 0 警告**（TreatWarningsAsErrors=true）。
- 全套 7/7 PASS（直跑 exe）：`fullsuite-run.log` — Contracts/Network/Plugin/Placement/Settings/ClientUi/Release 全 PASS，0 警告。Plugin 全量套件含 `AssertBueV2LhtAdoption()`（LIR 与 LIT 单人路径之间）。

## F1 修复轮复验（R1 后）

- F1 内容：Standards R1 SMELL×3 修复（PlayerLifeUiHudPatches.DumpAvailableMembers/Cleanup 空 catch→LogWarning；OwnerResolver 两处空 catch→LogWarning）；Spec R1 GAP-1 以冻结交接缝反驳（未改代码，见 review-rounds.md）。
- `fix1-build.log`：0 错 0 警告；`fix1-green-run.log`：LHT 锚点六组 ALL GREEN；Plugin 全量 PASS。

## 说明

- LHT 未走桩级红阶段（LIT/LIR 曾用）：编译红已满足「先红」门，实现一次写入后仅余环境缝红；无「测试先于实现失败于真 bug」的行为红，如实记录不冒充。
- BUE 帧不可靠 1:1 复验（T2 移交点）：loopback 传输 32 帧事件窗口 1:1 到达 + sequence 严格连续无重复键（组 6）+ 探针网络 64 帧可靠度旗标注（组 3，Update=false/Clear=true）。08 基线（LMN 路径实机 1:1）的 BUE 帧路径重证在本锚点完成；真实 Steam 传输复验随 DEV-V2-24 实机验收。
