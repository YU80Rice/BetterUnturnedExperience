# 交付报告 — DEV-16G 工单 B：日志运行时静默（候选 3 扩展）

> 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-16G-B-runtime-silence-20260902.md`
> 阶段：实现交付（红测→绿 → 双轴 CLEAN → 提交）
> 性质：代码健康维护（日志策略；非功能变更；非发布授权）

## 1. 背景（用户 2026-09-02 实机反馈）

工单 A 消除刷屏后，用户指出：**背包里移动物品时日志仍有大量播报（GPT-WATERMARK 判别读数 + 每次拖拽事件），对调试有用但对用户分析是负担**。

用户明确语义：加载/注入阶段播报正常加载 + 错误播报原因；进入游戏后运行时静默，仅出错打印原因。

## 2. 变更内容

### BueRuntimeLog 静态 seam（新文件）

| 方法 | 级别 | 语义 |
|---|---|---|
| `Runtime(line)` | `LogDebug` | 游戏内运行时事件（BepInEx `[Logging] Levels` 默认过滤 → 正常静默，改配置一键恢复） |
| `Load(line)` | `LogInfo` | 加载/注入一次性（正常播报） |
| `Error(line)` | `LogError` | 错误/隔离（永远打印 + reason） |

`Bind(Logger)` 在 Awake；`Recorder` 静态测试注入。

### 18 处 RUNTIME_RECURRING → Debug

drag-started、drag-cancelled、preview-hidden×2、preview-evaluated、preview-visible、preview-input-rejected、preview-input-readout、inventory-events-subscribed、placed-item-delegate-rebound、placement-passthrough×3、placement-decision、projection-submitted、native-inventory-snapshot、plugin-update 心跳、panel 心跳。

### 严重度修正 + 错误 reason 补充

- host-destroyed Warning→Info（正常条件）
- projection-submitted Warning→Debug（正常流程）
- projection-timed-out 保持 Error + 加 `reason=native-convergence-timeout`
- BootstrapFailed / runtime-pump-create-failed / runtime-pump-failed / RuntimeCompletionIsolated 补 `message=`

## 3. 红测 → 绿（TDD）

| 红测锚点 | 修复前 | 修复后 |
|---|---|---|
| `--logging-verbose-red`（Runtime→Debug、Load→Info、Error→Error+reason、ERROR_ALWAYS 不被吞） | 🔴 编译红 CS0234（BueRuntimeLog 不存在） | ✅ exit 0 |

## 4. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 PASS |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 5. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | S1 ProjectionSink 死字段（已修）；S2 message= 顺序（已修）；S3 LogTrace 无条件建 line；S4 surface-discarded 仍 Info |
| **Spec** | **CLEAN** | 无 | S1 BueRuntimeLog.Error 单调用点；S2 BueRuntimeLog.Load 仅测试用；S3 面板心跳经插件 log |

> Spec 轴逐点确认：18/18 运行时事件精确转换无遗漏无过度 ✓；加载一次性全保留 Info ✓；错误全带 reason ✓；投影 timed-out 保持 Error + reason ✓；无功能逻辑变更 ✓；红测先编译红后转绿 ✓。

## 6. 交付边界

- 本报告为 **DEV-16G 工单 B 实现交付**；DEV-16G 全部候选（1+2+3）落地。
- 可延后项全部列名（Standards S1-S4 + Spec S1-S3），不阻断。
- 实机验证项：正常游戏日志静默（仅加载/错误行）；`BepInEx.cfg` `[Logging] Levels=Debug` 恢复运行时读数。
