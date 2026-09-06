# GPT-DEV-15D 规格独立审查报告

日期：2026-08-26  
审查角色：GPT 独立规格审查  
审查范围：DEV-15D 工单、`spec-DEV-15-better-item-interaction.md`、`Module-Lifecycle-Isolation-Spec.md`，以及本票声明的 ClientUi/测试文件。

## 一、结论

**判定：FAIL（4 项阻断项）**

本轮 Release 编译和 ClientUi 测试均通过，但测试覆盖不足以证明 DEV-15D 已满足冻结规格。阻断项集中在生命周期非法转换/清理失败语义、设置快照代际防护，以及容器打开时的陈旧 UI 上下文清理。修复后必须重新编译、重跑全套测试并再次独立审查。

## 二、验证事实

```text
dotnet build BetterUnturnedExperience.sln --configuration Release --nologo
结果：0 errors / 0 warnings

tests/BetterUnturnedExperience.ClientUi.Tests/bin/Release/BetterUnturnedExperience.ClientUi.Tests.exe
结果：DEV-05/DEV-15A/DEV-15B/DEV-15C/DEV-15D ClientUi tests: PASS
```

以上仅证明当前代码可编译且现有测试通过，不等同于规格验收通过；本轮未宣称真实 Unity、单人、P2P、U3DS 或发布资格通过。

## 三、阻断项

### B1：生命周期没有拒绝非法转换，也没有 `InvalidStateTransition`

- **位置**：`src/BetterUnturnedExperience.ClientUi/BetterItemInteractionLifecycle.cs:75-143`；`tests/BetterUnturnedExperience.ClientUi.Tests/Dev15DTests.cs:56-75`
- **事实**：`Transition` 直接写入任意 `next`，没有按状态表校验，也没有记录 `InvalidStateTransition`。测试先执行 `Discovered → Incompatible`，随后又通过 `Start(true, false)` 执行 `Incompatible → Disabled`，再执行 `Disabled → Starting → Running`；这与生命周期规格规定的 `Incompatible` 当前进程不可重试相冲突。
- **根因**：状态机只是状态赋值器，未实现唯一状态写入 seam 的合法边约束和稳定错误诊断。
- **修复建议**：建立显式 `IsAllowedTransition(current,next)`；非法转换保持原状态、revision 不递增，并产生 `InvalidStateTransition`/DiagnosticId。按规格限制 `Incompatible`、`Isolated`、`Stopped` 的当前进程重试路径；测试必须覆盖每个非法边。

### B2：Stop 清理失败仍发布 `Stopped`

- **位置**：`src/BetterUnturnedExperience.ClientUi/BetterItemInteractionLifecycle.cs:220-225,228-236`
- **事实**：`RunCleanupOnce()` 将异常写入 `cleanupFailed`，但 `Stop()` 无条件调用 `lifecycle.CompleteStopped()`。因此 `Stopping → Stopped` 即使清理失败也会发生。
- **规格依据**：`Module-Lifecycle-Isolation-Spec.md:40-45` 明确规定清理/Dispose 失败时应转入 `Isolating`，保留诊断并且不得发布 `Stopped`；DEV-15D 工单验收项 44 要求清理异常继续清理但保留隔离结果。
- **根因**：清理结果没有反馈给生命周期裁决；正常停止和失败隔离共用同一完成路径。
- **修复建议**：`Stop()` 在清理完成后检查 `CleanupFailed`；失败时转 `Stopping → Isolating → Isolated`，保留同一 DiagnosticId，禁止 `Stopped`。新增故障注入测试验证所有 cleanup 仍执行、最终状态不是 `Stopped`。

### B3：设置快照没有 FeatureId/Revision 防护，旧快照可回写当前策略

- **位置**：`src/BetterUnturnedExperience.ClientUi/BetterItemInteractionLifecycle.cs:36-53`
- **事实**：`ApplySnapshot` 不校验 `snapshot.Feature` 是否为官方 FeatureId，也不比较 `snapshot.Revision` 与当前 `revision`；任意功能或迟到旧快照都会覆盖 `Enabled`、`AutoRotate` 并使 revision 倒退。当前测试只验证“中途变化不污染 active policy”，没有验证迟到快照/错误 FeatureId。
- **规格依据**：DEV-15D 要求消费不可变设置快照；前后端交接要求 revision/generation 单调消费。公开功能 FeatureId 也必须固定为 `io.github.yu80rice.bue.better-item-interaction`。
- **根因**：SettingsState 将快照解析与身份/版本接纳混在一起，缺少单调接收门。
- **修复建议**：只接受官方 FeatureId 且 `snapshot.Revision >= revision`（重复 revision 需同指纹/等价内容，旧 revision 静默丢弃或返回结构化拒绝）；拒绝后不得改变当前值。补充旧 revision、错误 FeatureId、缺失条目的测试。

### B4：打开不支持的 inventory surface 时保留旧上下文和图元

- **位置**：`src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs:204-213`
- **事实**：`OnInventoryOpened` 对普通 `IClientUiInventorySurface` 仅设置 `isInventoryOpen = true`，不会清除既有 `currentSurface`、`currentContainer`、`currentSessionGeneration` 或 `previewSink`。若先打开富上下文容器，再收到不支持页面/特殊区域的打开事件，后续拖拽仍可能沿用旧容器和旧 UI sink。
- **规格依据**：DEV-15 规定特殊页面必须原生放行；关闭、切换容器、UI Seam 冲突必须立即使当前增强拖拽失效并清理旧代际；DEV-15D 要求原生回退和陈旧回调隔离。
- **根因**：打开事件没有先执行对称关闭/失效，也没有明确的“非富上下文=原生 pass-through”状态。
- **修复建议**：每次打开先失效当前 drag、卸载旧 sink、清空 surface/container/generation；仅当传入 `IInventorySurfaceContext` 且上下文完整时绑定增强路径，否则保持打开标志但无增强上下文，所有后续操作走原生回退。补充“富上下文→特殊页面”和“换容器同代/跨代”测试。

## 四、已通过或基本符合的项目

- `Enabled`/`AutoRotate` 默认值为 `true`，且 drag begin 捕获不可变策略；当前测试证明拖拽中修改不会改变 active policy，下一次拖拽可读取新值。
- Disabled 时 `BetterItemInteractionUiComponent.OnDragReleased` 返回 `PassThrough` 且不调用增强 native port；重新启用在下一次 drag 生效。
- 局部隔离会结束增强拖拽并逆序执行全部 cleanup；单个 cleanup 异常不会阻断后续 cleanup。
- `ClientUiCompositionRoot.EnterSafeMode` 具备一次性诊断、倒序卸载全部已组合 UI、状态转为 `Unavailable` 的基础行为。
- 缺失卫星的纯值投影可保持功能 `Running` 并设置 `PresentationDegraded`；Headless 投影可设置为 `HeadlessOnly`。
- ClientUi 项目接入 `BetterItemInteractionLifecycle.cs` 和 `Dev15DTests.cs`；本轮构建为 0 警告/0 错误。

## 五、审查边界与后续门禁

本报告不修改源码。修复 B1-B4 后，必须：

1. 按 Red → Green 补齐非法状态、清理失败、旧快照/错误身份、特殊页面切换测试；
2. 重新执行 Release 全解决方案编译、7 个测试项目和 UI/Native token 静态扫描；
3. 重新执行 GPT 独立审计，并提交 Gemini 前端消费复核；
4. 在两项复核均通过前，不得关闭 DEV-15D，也不得宣称 DEV-15E 或真实环境资格通过。


