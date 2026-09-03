# DEV-16G 工单 C：BUE 面板日志静默 + projection-timed-out 误报修正

Type: task
Status: resolved（已实现、双轴 CLEAN、提交）
Parent: DEV-16G 日志规范化
Blocked by: 无（工单 A `114977d` + 工单 B `3bc651d` 已提交）

## 背景（用户 2026-09-03 实机反馈，`UMM-诊断包_20260903_112448`）

> "BII 的功能好像就不会刷日志了，这很好！但是 BUE 的功能好像还在刷日志，能不能也复用这个'加载阶段播报、错误带原因、游戏内静默'的具体日志播报功能？往期插桩现在已经成为累赘了，只需要用中文或者英文在日志里打一句关键信息就行。另外，测试时我注意到日志报错，但实际功能无异常（报错物品依然有强化渲染的功能，无异常），这是为什么？"

## 根因

1. **BUE 面板刷屏**：`BueNativeManagementPanel.LogTrace` 全部走 Info——未复用工单 B 的 `BueRuntimeLog` 静默策略。实机日志 ~40 行 `[BUE-UI-TRACE] event=surface-opened/constructor-postfix/create-button-*/add-child-success/container-state` 在每次开菜单/UI 重建时刷出。
2. **projection-timed-out 误报 Error**：工单 B 按 walk 建议标为 Error，但实机证明是**良性视觉预算到期**——`OnDragReleased` 先原生提交（`HandleRelease`→`sendDragItem`）再 `Begin(binding)`，放置服务端权威；`AwaitingProjectionController.Tick` 在 2000ms 预算到期时只停视觉等待（`ItemInteractionUiComponent` "No fake rollback"）。报 Error 是严重度误报。

## 修复

- `BueRuntimeLog.IsRuntimeEvent(eventName)` 纯分类器：7 个循环面板事件 → Runtime（Debug）；加载一次性保持 Info。
- `BueNativeManagementPanel.LogTrace` 路由经分类器（`isRuntime || IsRuntimeEvent`）。
- `OnProjectionTimedOut` Error→Runtime（Debug），reason 保留。

## 红测锚点

- `--logging-bue-runtime-red`：分类器双向断言（7 Runtime + 5 Load 排除；先 CS0117 编译红）。
- `--logging-timeout-red`：timeout 为 Debug 非 Error + reason 保留（先运行时红）。

## 验收

- [x] 红测先红后绿
- [x] Release 构建 0/0；七项目全 PASS；UI token 零命中；`git diff --check` 通过
- [x] 双轴独立审查 CLEAN（Standards S1 OR 冗余可延后、S2 事件名三处硬编码可延后；Spec S1 reason 断言已补、S2 heartbeat 冗余、S3 分类器硬编码可延后、S4 host-ui-tick Info 可延后）
- [ ] 待实机确认：BUE 面板正常游戏静默；无 Error 误报
