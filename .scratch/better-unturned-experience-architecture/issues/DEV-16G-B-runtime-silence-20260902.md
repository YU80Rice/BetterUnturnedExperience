# DEV-16G 工单 B：日志运行时静默（候选 3 扩展——用户新语义）

Type: task
Status: ready-for-agent
Parent: DEV-16G 日志规范化
Blocked by: 无（工单 A 已提交 `114977d`，实机已确认刷屏消除）

## 背景（用户 2026-09-02 实机反馈）

工单 A 消除刷屏后，用户注意到：**背包里移动物品时，日志内仍有大量播报（GPT-WATERMARK 判别读数 + 每次拖拽的 drag-started/placement-decision 等）**，对调试有用但对用户分析是负担。

用户明确的新日志语义：
> "启动游戏、注入插件的每个阶段和阶段正常，就播报正常加载，有错误就播报哪里出错了；然后进入游戏就不用再具体播报，只需要出错时再打印错误原因就行。"

## 方案

### 分类（research：`research/runtime-verbosity-walk-20260902.md`，92 处发射点）

| Bucket | 数量 | 策略 |
|---|---|---|
| LOAD_ONE_SHOT（加载/注入一次性：boot、wiring、hooks-installed、surface dispatch、patch） | 38 | **保留**（LogInfo，正常播报） |
| RUNTIME_RECURRING（游戏内运行时正常事件：drag-started/cancelled、placement-decision/passthrough×4、GPT-WATERMARK×6、native-inventory-snapshot、inventory-events-subscribed、placed-item-rebound、双心跳） | 18 | **降为 LogDebug**（正常游戏静默；BepInEx `[Logging] Levels` 默认排除 Debug，改配置一键恢复） |
| ERROR_ALWAYS（错误/隔离：ReportCleanup*、EmitDiagnosticOnce、BootstrapFailed、wiring-disabled、Isolate 路径） | 36 | **保留 + 补 reason** |

### 补充修正（research deep-dive 4）

- `projection-timed-out`（Warning，真异常）→ 保留 + 加 reason（generation/revision）。
- `host-destroyed state=preserved`（误标 Warning，正常）→ 降为 Info。
- `projection-submitted`（误标 Warning，正常流程）→ 降为 Debug。
- 错误路径 `LogError` 只带 errorType → 补 message（用户"哪里出错了"）。

## 红测锚点

- `--logging-verbose-red`：运行时事件发射点使用 Debug 级别（契约断言），加载用 Info、错误用 Warning/Error；且 ERROR_ALWAYS 不被静默门吞掉。
- 复用 `DiagnosticLogSink` / `ShouldEmitDiagnostic` 既有 seam。

## 边界

- 不改变功能逻辑（纯日志级别调整）。
- 保留：38 加载一次性 + 36 错误（+ reason）。
- 静默：18 运行时正常事件（LogDebug，配置可恢复）。
- 本工单完成后 DEV-16G 全部候选（1+2+3）落地；实机验收后关闭。

## 验收

- [x] 红测先红后绿（`--logging-verbose-red`：先编译红 CS0234 BueRuntimeLog 不存在，实现后 exit 0）
- [x] Release 构建 0/0；七项目全 PASS；UI token 零命中；`git diff --check` 通过
- [x] 双轴独立审查 CLEAN（Standards 4 项可延后 + Spec 3 项可延后，均已列名）
- [ ] 实机：正常游戏日志静默，仅加载/错误行；`Levels=Debug` 恢复运行时读数（待用户部署后确认）

## 双轴审查可延后项（已列名，非阻断）

**Standards**：S1 ProjectionSink 死字段 `log`（已本轮修正）；S2 message= 顺序（已本轮修正）；S3 LogTrace 无条件建 line（log 永非 null，可忽略）；S4 surface-discarded 仍 Info（超出 18 站范围，策略一致性可后续定）。

**Spec**：S1 `BueRuntimeLog.Error` 仅一个生产调用点（projection-timed-out）；S2 `BueRuntimeLog.Load` 仅测试使用（seam 兼作契约锚点）；S3 面板心跳经插件绑定 log（同 logger 无行为影响）。

## 2026-09-02 实现记录

- 新增 `BueRuntimeLog` 静态 seam：`Runtime()`→LogDebug（BepInEx 默认过滤）、`Load()`→LogInfo、`Error()`→LogError；`Bind(Logger)` 在 Awake；`Recorder` 测试注入。
- 18 处 RUNTIME_RECURRING 事件 LogInfo→`BueRuntimeLog.Runtime`（drag-started/cancelled、preview×6、inventory-events-subscribed、placed-item-rebound、placement-passthrough×3、placement-decision、projection-submitted、native-inventory-snapshot、双心跳）。
- 严重度修正：host-destroyed Warning→Info；projection-submitted Warning→Debug；projection-timed-out 保持 Error + 加 reason。
- 错误路径补 `message=`：BootstrapFailed、runtime-pump-create-failed、runtime-pump-failed、RuntimeCompletionIsolated。
- 提交：`(待填)`
