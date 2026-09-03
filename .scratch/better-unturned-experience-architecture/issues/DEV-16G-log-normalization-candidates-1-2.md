# DEV-16G：日志规范化（工单 A：候选 1+2）

Type: task
Status: closed（2026-09-03 并入 DEV-16G 整体实机验收，随工单 D 关闭冻结）
Parent: 无（代码健康维护，来自 `/improve-codebase-architecture` 评审）
Blocked by: 无

## 背景

Better Item Interaction V1 功能闭环后（DEV-16D/E/F 关闭），日志面仍是 R5-R13 判别时代形态。真实日志 14,147 行中 **13,768 行（97.3%）** 是 `surface-not-ready`（每帧 × 4 页 × 3 站点）。用户要求：改为关键加载节点的一次性规范化报错（"xxx 已加载" / "xxx 加载失败，原因：yyy"），消除刷屏，出问题一眼可见。

走查证据：`.scratch/better-unturned-experience-architecture/research/logging-surface-walk-20260902.md`
评审报告：`%TEMP%/architecture-review-20260902-210435.html`

## 方案

### 切片 A：surface-not-ready 状态转换门控（候选 1）

- 现状三处每帧刷屏（同一 `BUE-INVENTORY-004`）：
  - `InventorySurfaceLifecycleAdapter.cs:1128`（Poll 主循环，无 reason）
  - `InventorySurfaceLifecycleAdapter.cs:1230`（reason=native-hierarchy-incomplete）
  - `InventorySurfaceLifecycleAdapter.cs:1255`（reason=scroll-viewport-not-laid-out）
- 改法：新增纯 C#「按页就绪状态跟踪器」，每页在**状态转换时**打一条：
  - 从未就绪 → 就绪：`page N 已就绪 grid=WxH`（复用 surface-context-dispatched 语义，但收敛）
  - 就绪 → 未就绪：`page N 加载失败，原因: <reason>`
  - 同状态持续：静默
- 三处 reason 汇入一个"未就绪原因"判定（Poll 循环层统一，因为那里知道上一帧此页状态）。

### 切片 B：运行时失败原因接入日志（候选 2）

- 现状：`LastPollDiagnostics` / `LastCleanupDiagnostics` / `DescribeTickGate` / `DescribeAdapterGate` / `DescribeNoActiveSession` 构建富文本 reason 但**生产从不输出**（只被测试用/存静态字段）。
- 改法：在真实失败/隔离出口加一次性 `LogWarning/LogError`，把已构造 reason 字符串发射：
  - `IsolateAndDetach` 失败分支
  - `InvokePollGuarded` catch
  - `CleanupIncomplete` 路径（`ReportCleanupIncomplete` / `ReportCleanupFailure`）
- 复用既有测试 seam（Describe* 已是最佳测试表面）。

## 红测锚点

- `--logging-gate-red`：就绪状态跟踪器——同页同状态不重复打；状态转换（not-ready→ready / ready→not-ready）各打一条且带正确 reason；页面间独立。
- `--logging-failure-red`：适配器隔离/失败出口确实发射一次性失败日志（含 reason 字符串）。

## 边界

- 不改变功能逻辑（纯日志面改造；状态跟踪器不改变 Poll 的 dispatch 决策）。
- 保留所有低频结果行：`placement-decision outcome=Submitted`、`surface-context-dispatched`（降为转换时）、`BootstrapReady/BootstrapFailed` 等一次性节点。
- 候选 3（双心跳去重 + GPT-WATERMARK 降级）为工单 B，不在本工单。
- 发布门禁不影响；本工单为代码健康维护，仍走 TDD + 双轴审查。

## 验收

- [x] 红测先红后绿（两个 `--logging-*-red` 锚点：先编译红 CS0426/CS0117，实现后 exit 0）
- [x] Release 构建 0/0；七项目测试全 PASS；UI token 扫描零命中；`git diff --check` 通过
- [x] 双轴独立审查 CLEAN（Standards 6 项可延后 + Spec 3 项可延后，均已列名）
- [ ] 真实日志对比：开背包不动 60 秒不再产生 ~14,000 行刷屏（实机验证项，待用户部署后确认）

## 双轴审查可延后项（已列名，非阻断）

**Standards**：S1 `EmitDiagnosticOnce` 实际"每失败阶段一行"（复合失败可 2-3 行，终端隔离守卫有界）——已改注释准确化，不强制去重；S2 生命周期失败经 drag adapter sink 以 `[BUE-DRAG]` 前缀发射（既有结构，工单 B 或后续处理）；S3 seam 两处复制且无 teardown reset（无害，可后续收敛）；S4 测试未恢复 LastCleanupDiagnostics 静态（当前顺序无害，潜伏）；S5 diagnosticId 复用削弱按 id 分流（建议 DEV-16G id 族）；S6 注释漂移已在本轮修正。

**Spec**：S1 同 EmitDiagnosticOnce 命名 vs 行为；S2 `InvokePollGuarded` catch 设 LastPollDiagnostics 但未发射（隔离失败本身已可见，用户可见问题已覆盖）；S3 gate ready 行与 surface-context-dispatched 在会话打开时双行（无害一次性），reason 为简短代码而非整句。

## 2026-09-02 实现记录

- 切片 A：`SurfaceReadinessGate`（纯 C# 每页就绪状态机，状态转换才发射）；Poll 三处每帧 `surface-not-ready` 全部改为 gate 喂入；`BuildSurfaceContext` 增 `out string notReadyReason`（10 条 null 路径各带原因）。
- 切片 B：`DiagnosticLogSink` + `EmitDiagnosticOnce` 静态 seam；`ReportCleanupIncomplete`/`ReportCleanupFailure` 接入发射；两个 adapter 的 Activate 绑定 sink。
- 红测：`--logging-gate-red` / `--logging-failure-red`，并入全套。
- 提交：`(待填)`
