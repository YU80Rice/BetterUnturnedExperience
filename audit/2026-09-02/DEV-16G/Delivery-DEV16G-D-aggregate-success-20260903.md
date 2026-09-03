# 交付报告 — DEV-16G 工单 D：日志"一句成功 + 错误才播报"

> 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-16G-D-aggregate-success-log-20260903.md`
> 阶段：实现交付（grill 拍板 D1-D13 → 红测→绿 → 双轴 CLEAN → 提交 `6c7066a`）
> 性质：代码健康维护（日志策略；非功能变更；非发布授权）

## 1. 背景（用户 2026-09-03 实机反馈）

> "打开箱子、车辆后备箱，日志内还是有刷新。已实现可用的功能，不需要再插入过多的探针去检验界面或者什么是否打开。只需要在插件加载成功后，打印一句'xxx加载成功，界面已注入'，只有出问题时再打印；游戏运行中成功不播报、错误才播报。"

诊断包 `UMM-诊断包_20260903_115902`：116 行 BUE 日志中 ~85 行是 `surface-context-dispatched`（每次开容器 → generation+1 → 页 2/3/4/6 各刷一条）；**无 Error/Warning**（功能正常）。

## 2. 决策来源

`/improve-codebase-architecture`（走查 + HTML 报告）+ `/grill-me`（Q1-Q13 拍板，用户确认全部推荐决策），落盘 D1-D13。

## 3. 变更内容

| 决策 | 变更 |
|---|---|
| D1/D7/D8 | `BueRuntimeLog.AnnounceReady`：RuntimeReady 后打一条 Info "Better Unturned Experience 加载成功，界面已注入"（Headless 变体"加载成功（无界面）"），once-guard |
| D2/D3/D6 | 全部子系统加载一次性降 Debug：surface-context-dispatched/surface-ready/surface-discarded/hooks-installed/polling-hook-installed/wiring/composition-ready/BootstrapReady/accepted/runtime-gate/start-entered/pump-created/pump-tick/host-destroyed |
| D4 | surface-not-ready 分级：`native-hierarchy-incomplete` → ErrorFriendly；`empty-grid`/`scroll-viewport-not-laid-out` 等良性 → Debug |
| D5 | Harmony 注入点（constructed/initialize-complete/patch-installed/host-ui-tick/first-tick + C 期 7 个）全走 IsRuntimeEvent → Debug |
| D9/D10 | `ErrorFriendly`：错误行加中文前缀"BUE 错误："，保留结构化 token；接入 BootstrapFailed/RuntimeCompletionIsolated/两 DiagnosticLogSink/关键 not-ready |
| D7 例外 | `assembly-identity`（sha256 证据锚点）保留 Info——每会话一条、非噪音，三环境验证依赖日志内嵌哈希 |

## 4. 红测 → 绿（TDD）

| 红测锚点 | 修复前 | 修复后 |
|---|---|---|
| `--logging-aggregate-red`（聚合行 exactly-once + 中文措辞 + Headless 变体 + 子系统全降 Debug + not-ready 分级） | 🔴 编译红 CS0117 | ✅ exit 0 |
| C 工单 `--logging-bue-runtime-red` | 期望翻转为 D6 语义（constructed 等现为 Runtime） | ✅ exit 0 |

## 5. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 PASS |
| 六个日志红测锚点 | 全 PASS |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 6. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | S1 `wiring-enabled` 分类器条目无生产发射点（adapter 直接走 Runtime，防御性）；S2 `BueRuntimeLog.Load` 生产不再调用（保留为契约锚点，注释已本轮修正） |
| **Spec** | **CLEAN** | 无 | S1 关键 not-ready 分支可达性低（ProbeNativeHierarchy 前置门先拦）；S2 assembly-identity 与聚合行并存 Info（证据锚点例外，明示取舍）；S3 工单 D12 措辞与分类翻转的文档细微差异 |

> Spec 轴逐点确认：D1-D13 全满足 ✓；assembly-identity 例外有依据且工单内文档化 ✓；无功能变更（AwaitingProjectionController/ItemInteractionUiComponent 未动）✓。

## 7. 预期效果（对照诊断包 116 行）

- 正常游戏：~85 行 surface 系 → 0；38 条加载 Info → **1 条聚合行**；面板循环事件 → 0
- 出错时：一条 `BUE 错误：...reason=xxx`（人话前缀 + 可检索 token）
- 排查时：`BepInEx.cfg` `[Logging] Levels = Debug` 恢复全量

## 8. 交付边界

- 本报告为 **DEV-16G 工单 D 实现交付**；日志规范化全部落地（A/B/C/D）。
- 可延后项全部列名（Standards S1-S2 + Spec S1-S3），不阻断。
- 实机验证项：正常游戏仅一条聚合行；开箱子/后备箱无刷新；出错一条"BUE 错误："；`Levels=Debug` 恢复全量。
