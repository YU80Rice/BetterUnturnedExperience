# DEV-V2-15 R2'（零上下文 fresh 验证轮）——Standards 轴报告

> 归档说明：本报告转录自 2026-09-06 implement 会话内全新 spawn 的 standards-reviewer 实例输出（Fresh-instance 规则下的第一轮 fresh 验证；派发提示词不含 R1 结论）。转录逐字保留判词与 findings，仅加本说明头。

---

CLEAN

R1 两项 BLOCKING 已闭合：`EnsureOpen` 在 `EnsureStarted` 代际边界重开队列（`MainThreadDispatcher.cs:262-268`，`InventoryTidyModule.cs:110`）；Stop 解绑 `LogSink/ErrorLogSink`（`InventoryTidyModule.cs:92-93`）；方向/模式点击有 `ActiveModule==null` 守卫（`InventoryTidyUiPatch.cs:536,561`）。署名：`docs/third-party/LaunchInventoryTidy-attribution.md` + 六份迁移文件 MIT 头。类型已全部 `internal`；半装回滚、`LogWarning→ErrorLogSink`、`ArmNewModule` 去重均落地。

## BLOCKING
无。

## SMELL（可具名延期）
- `TidyDiagnosticLog.Windows` 不随 Stop 清空（已具名：上限 64、非跨功能）。
- `enabled=false` 已注入按钮残留 + 关闭前已入队事务仍执行一次（已具名，实机自验）。
- `LastLocalOutcome` 预留 DEV-V2-19/21（已具名）。
- `LocalTidyFaultGate` 相对旧电路的最小化边界（代码头已声明 DEV-V2-21）。
- `MainThreadDispatcher.cs:41-43,53,112-113`：类注释仍写 `TidyFaultCircuit`/`/tidy_faults`/`Enqueue`「向后兼容」——本 DLL 无这些类型；R1「陈旧注释」未扫干净。
- `MainThreadDispatcher.cs:271-275`：`ResetForTests` 仍称「关停粘滞、同进程永不重开」，与生产 `EnsureOpen` 矛盾。
- `InventoryTidyModule.cs:92-94`：解绑 sink 之后的阶段 3 `LogInfo` 被吞。
- `TidyDiagnosticLog.cs`：迁入文件无 MIT 头（项目级署名已覆盖，文件头不齐）。
- `InventoryTidyModule.cs:122`：`RefreshSwitches` 文档写成「新功能代际」，实际只 `FaultGate.Reset`，不开/关 dispatcher。

（处置注：其中 5 项注释/顺序级 SMELL 已按评审者指名就地修复，见结单报告「具名延期与处置」第 6 项；修复后全套 7/7 PASS 复跑留证。）
