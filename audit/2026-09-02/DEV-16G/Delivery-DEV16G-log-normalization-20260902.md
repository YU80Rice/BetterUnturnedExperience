# 交付报告 — DEV-16G 日志规范化（工单 A：候选 1+2）

> 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-16G-log-normalization-candidates-1-2.md`
> 阶段：实现交付（红测→绿 → 双轴 CLEAN → 提交）
> 性质：代码健康维护（非功能变更；非发布授权）
> Release DLL SHA-256：`62AA2B2E57A0A180BE1A84A2456526EC6469AE4403F1FDA2382AD30A58F2120B`

## 1. 背景

BUE/BII V1 功能闭环后（DEV-16D/E/F 关闭），日志面仍是 R5-R13 判别时代形态。真实日志 14,147 行中 **13,768 行（97.3%）** 是 `surface-not-ready` 每帧刷屏（开着背包不动时 ~14,400 行/分）。用户要求：改为关键加载节点的一次性规范化报错（"xxx 已加载" / "xxx 加载失败，原因：yyy"），消除刷屏，出问题一眼可见。

走查证据：`research/logging-surface-walk-20260902.md`

## 2. 变更内容

### 切片 A：surface-not-ready 状态转换门控

| 变更 | 位置 |
|---|---|
| 新增 `SurfaceReadinessGate`（纯 C# 每页就绪状态机，状态转换才发射） | `InventorySurfaceLifecycleAdapter.cs` |
| Poll 三处每帧 `surface-not-ready` 全部改为 gate 喂入（静默稳态） | Poll 循环 + BuildSurfaceContext |
| `BuildSurfaceContext` 增 `out string notReadyReason`（10 条 null 路径各带原因） | `BuildSurfaceContext` |
| `surface-context-dispatched` 低频一次性行保留 | Poll |

### 切片 B：运行时失败原因接入日志

| 变更 | 位置 |
|---|---|
| 新增 `DiagnosticLogSink` + `EmitDiagnosticOnce` 静态 seam | 两个 adapter |
| `ReportCleanupIncomplete` / `ReportCleanupFailure` 接入发射 | `InventoryDragPreviewAdapter` |
| 两个 adapter 的 Activate 绑定 sink 到 BepInEx LogWarning | Activate |

## 3. 红测 → 绿（TDD）

| 红测锚点 | 修复前 | 修复后 |
|---|---|---|
| `--logging-gate-red`（状态转换门控：稳态静默、转换发射、页独立） | 🔴 编译红 CS0426（seam 不存在） | ✅ exit 0 |
| `--logging-failure-red`（失败原因一次性发射） | 🔴 编译红 CS0117 | ✅ exit 0 |

## 4. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 PASS |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |
| 预期日志降量 | 开背包不动从 ~14,400 行/分 → 稳态静默（实机待确认） |

## 5. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | S1 EmitDiagnosticOnce 每失败阶段一行（注释已改准确化）；S2 生命周期失败经 drag sink 带 `[BUE-DRAG]` 前缀（既有结构）；S3 seam 两处复制无 teardown reset（无害）；S4 测试未恢复 LastCleanupDiagnostics（潜伏）；S5 diagnosticId 复用；S6 注释漂移（本轮已修） |
| **Spec** | **CLEAN** | 无 | S1 同 EmitDiagnosticOnce 命名；S2 InvokePollGuarded catch 未发射（隔离失败已可见）；S3 gate ready 与 surface-context-dispatched 打开时双行（无害一次性）+ reason 简短代码 |

> Spec 轴逐点确认：三处每帧 surface-not-ready 全移除 ✓；gate 仅转换发射 ✓；`openDispatcher` 时机不变（log-only）✓；surface-context-dispatched 保留 ✓；ReportCleanup* 接入发射 ✓；两 adapter sink 绑定 ✓；GPT-WATERMARK/双心跳未动（属工单 B）✓；无 RPC/库存/权威代码触碰 ✓；红测可编译红后转绿 ✓。

## 6. 交付边界

- 本报告为 **DEV-16G 工单 A 实现交付**；工单 B（候选 3：双心跳去重 + GPT-WATERMARK 降级 + native-inventory-snapshot 门控）待后续。
- 可延后项全部列名（Standards S1-S6 + Spec S1-S3），不阻断。
- 实机验证项：开背包不动 60 秒日志量对比（~14,400 行 → 静默），待用户部署后确认。
