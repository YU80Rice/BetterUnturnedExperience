# 交付报告 — DEV-16G 工单 C：BUE 面板静默 + projection-timed-out 误报修正

> 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-16G-C-bue-panel-silence-20260903.md`
> 阶段：实现交付（红测→绿 → 双轴 CLEAN → 提交）
> 性质：代码健康维护（日志策略；非功能变更；非发布授权）

## 1. 背景（用户 2026-09-03 实机反馈）

用户复测 `UMM-诊断包_20260903_112448`：BII 不再刷日志 ✓，但 BUE 管理面板仍在刷（每次开菜单/UI 重建 ~40 行 `[BUE-UI-TRACE]`），且 `projection-timed-out` 报 Error 但功能正常。用户要求复用"加载阶段播报、错误带原因、游戏内静默"策略，并解释为何报错但功能正常。

## 2. 根因

| 问题 | 根因 |
|---|---|
| BUE 面板刷屏 | `BueNativeManagementPanel.LogTrace` 全部 Info，未走 `BueRuntimeLog` 静默 seam |
| projection-timed-out 报 Error | 良性视觉预算到期（2000ms）误标为 Error——放置已原生提交且服务端权威，超时只停视觉等待（No fake rollback） |

## 3. 变更内容

| 变更 | 位置 |
|---|---|
| `BueRuntimeLog.IsRuntimeEvent` 纯分类器 | `BueRuntimeLog.cs` |
| `LogTrace` 经分类器路由（`isRuntime || IsRuntimeEvent`） | `BueNativeManagementPanel.cs` |
| `OnProjectionTimedOut` Error→Runtime（Debug），reason 保留 | `InventoryProjectionSink.cs` |
| 红测 ×2 | `Plugin.Tests/Program.cs` |

## 4. 红测 → 绿（TDD）

| 红测锚点 | 修复前 | 修复后 |
|---|---|---|
| `--logging-bue-runtime-red`（分类器 7 Runtime + 5 Load 排除） | 🔴 编译红 CS0117 | ✅ exit 0 |
| `--logging-timeout-red`（timeout 为 Debug 非 Error + reason） | 🔴 运行时红（Error≠Debug） | ✅ exit 0 |

## 5. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 PASS |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 6. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | S1 `isRuntime ||` OR 左半冗余（heartbeat 已在分类器）；S2 事件名三处硬编码（分类器+面板+测试） |
| **Spec** | **CLEAN** | 无 | S1 timeout 测试未断言 reason 子串（已本轮补上）；S2 heartbeat 显式 isRuntime 冗余；S3 分类器硬编码开关（fail-open→Info 安全）；S4 host-ui-tick 保持 Info（单次守卫非刷屏） |

> Spec 轴逐点确认：7 个循环面板事件全分类 Runtime 且实测日志 ~40 行将静默 ✓；加载一次性全保持 Info ✓；projection-timed-out 降级正确且 reason 保留 ✓；错误路径全保持响亮 ✓；红测先红后绿 ✓；`AwaitingProjectionController`/`ItemInteractionUiComponent` 未动（无功能变更）✓。

## 7. 交付边界

- 本报告为 **DEV-16G 工单 C 实现交付**；DEV-16G 日志规范化（候选 1+2+3 + 面板 + 误报修正）全部落地。
- 可延后项全部列名（Standards S1-S2 + Spec S1-S4），不阻断。
- 实机验证项：正常游戏 BUE 面板静默；无 Error 误报；`Levels=Debug` 恢复全部运行时读数。
