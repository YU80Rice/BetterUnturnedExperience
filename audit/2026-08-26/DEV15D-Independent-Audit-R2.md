# GPT-DEV15D 独立审计报告 R2

## 一、审计范围

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-15D-settings-lifecycle-isolation.md`
- 规格：`.scratch/better-unturned-experience-architecture/spec-DEV-15-better-item-interaction.md`
- 审计对象：
  - `src/BetterUnturnedExperience.ClientUi/BetterItemInteractionLifecycle.cs`
  - `src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs`
  - `src/BetterUnturnedExperience.ClientUi/ClientUiTypes.cs`
  - `tests/BetterUnturnedExperience.ClientUi.Tests/Dev15DTests.cs`
- 审计时间：2026-08-26 00:49（Asia/Shanghai）
- 审计性质：GPT 独立只读复核；未修改生产代码。

## 二、最终判定

**FAIL（仍有 2 个前端生命周期阻断项）**

本轮修复已消除 R1 的编译阻断，并补齐设置身份/revision 与正常提交后的增强拖拽状态清理。Release 编译、ClientUi 测试、其余六个测试项目及 UI/native 静态扫描全部通过；但源码仍违反 Disabled/Isolated/SafeMode 的 UI 不实例化约束，并且 surface 重绑没有主动失效当前拖拽。

该审计仅覆盖静态/单元测试 Seam，不等价于真实 Unity/Glazier Hook、单人、SteamP2PFriends、U3DS 或发布资格通过。

## 三、审计矩阵

| 审计项 | 判定 | 证据 |
| :--- | :---: | :--- |
| 设置默认值 | PASS | `BetterItemInteractionLifecycle.cs:23-35` 默认 `Enabled=true`、`AutoRotate=true`；`Dev15DTests.cs:23-27` 通过 |
| 设置身份隔离 | PASS | `BetterItemInteractionLifecycle.cs:40` 以 Ordinal 比较官方 FeatureId `io.github.yu80rice.bue.better-item-interaction`；`Dev15DTests.cs:121-129` 覆盖错误 Feature 拒绝 |
| 设置 revision 单调消费 | PASS | `BetterItemInteractionLifecycle.cs:40-60` 拒绝旧值及重复 revision；`Dev15DTests.cs:124-129` 覆盖旧/重复快照 |
| 拖拽中设置不漂移 | PASS | `CaptureForDrag()` 捕获不可变 `BetterItemInteractionDragPolicy`；`Dev15DTests.cs:44-56` 覆盖下一次拖拽生效 |
| Disabled 原生回退与重新启用 | PASS | `BetterItemInteractionRuntime.BeginDrag()` 与组件释放分流；`Dev15DTests.cs:30-42,143-166` 通过 |
| 九态生命周期 | PASS | `Start/Disable/BeginIsolation/CompleteIsolation/BeginStopping/CompleteStopped` 实现九态路径；`Dev15DTests.cs:58-80` 覆盖 Incompatible、Disabled、Starting、Running、Isolating、Isolated、Stopping、Stopped |
| 非法状态转换 | PASS | `Start()` 限制为 Discovered/Disabled，非法请求写入 `BUE-DEV15D-INVALID-STATE-TRANSITION`；`Dev15DTests.cs:63-66` 验证 Incompatible 不可重启 |
| 状态 revision 单调 | PASS | `Transition()` 仅在状态变化时递增 `stateRevision`；生命周期测试验证单调递增 |
| 隔离与清理异常 | PASS | Runtime 先停止增强拖拽，再逆序执行全部 cleanup；单项异常不阻断后续清理，清理失败不发布 Stopped；`Dev15DTests.cs:83-97,131-140` 通过 |
| 正常提交后清理 | PASS | `ItemInteractionUiComponent.cs:306-310` 正常 `HandleRelease` 后调用 `runtime.EndDrag()`；`Dev15DTests.cs:157-166` 验证 `Submitted` 后 `EnhancedDragActive=false` |
| Surface 失配清理 | **FAIL** | 非 `IInventorySurfaceContext` 会清理，但 `OnInventoryOpened()` 对新的合法 surface 重绑前没有 `runtime.EndDrag()`/`previewPresenter.EndDrag()`；同一回调序列下旧 drag generation 可继续存活 |
| SafeMode | PASS | Runtime SafeMode 幂等清理、隔离并投影 `HeadlessOnly`；`Dev15DTests.cs:99-110` 通过；CompositionRoot 另有一次诊断和全量 UI 卸载实现 |
| UI 卫星缺失降级 | PASS | `OnUiInitialized(... satelliteAvailable, headless)` 与 `SetPresentationAvailable()` 投影 `PresentationDegraded/HeadlessOnly`，不改变 Core/Settings；`Dev15DTests.cs:112-119` 通过 |
| UI/Headless 边界 | PASS | `ClientUiEnvironment.CanCompose` 三门禁及 `ClientUiCompositionRoot` 工厂隔离逻辑；Contracts/Core/ClientUi token 扫描通过 |
| 线程/异常边界 | PASS（静态范围） | DEV-15D 未创建后台线程；UI callback 与释放路径均有异常边界。真实游戏主线程运行仍待 DEV-15E/实机证据 |

## 四、构建与测试证据

### Release 构建

命令：

```powershell
dotnet build D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\BetterUnturnedExperience.sln --configuration Release --nologo
```

结果：`0 errors / 0 warnings`，退出码 `0`。

### 测试结果

七个测试项目全部退出码 `0`：

- `BetterUnturnedExperience.ClientUi.Tests.exe`：DEV-05/DEV-15A/DEV-15B/DEV-15C/DEV-15D `PASS`
- `BetterUnturnedExperience.Contracts.Tests.exe`：DEV-10 registration runtime `PASS`
- `BetterUnturnedExperience.Network.Tests.exe`：DEV-06 network `PASS`
- `BetterUnturnedExperience.Placement.Tests.exe`：DEV-04 placement evaluator `PASS`
- `BetterUnturnedExperience.Plugin.Tests.exe`：DEV-14 official registration parity `PASS`
- `BetterUnturnedExperience.Release.Tests.exe`：DEV-08 runtime evidence package `PASS`
- `BetterUnturnedExperience.Settings.Tests.exe`：DEV-03 settings runtime `PASS`

### 静态门禁

`eng/Verify-NoUiTokens.ps1` 扫描结果：

- `src/BetterUnturnedExperience.ClientUi`：PASS，10 个 C# 文件
- `src/BetterUnturnedExperience.Contracts`：PASS，2 个 C# 文件
- `src/BetterUnturnedExperience.Core`：PASS，10 个 C# 文件

## 五、阻断项

### B1：Disabled/Isolated/SafeMode 仍可实例化自定义 UI

- 位置：`src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs:183-207,210-218`。
- `ApplySettingsSnapshot()` 在 Enabled=false 时只调用 `lifecycle.Disable()`，没有卸载已挂载的 `previewSink`。
- `OnInventoryOpened()` 无条件执行 `BindVisualSink()`，没有先检查 `lifecycle.CanRun`、SafeMode、Headless 或卫星可用状态。因此 Disabled、Isolated、SafeMode 或 `OnUiInitialized(..., satelliteAvailable:false, headless:true)` 场景仍可能创建/挂载自定义图元。
- 违反规格中 Disabled/Isolating/Isolated/Core SafeMode 必须卸载 UI，以及 UI 卫星缺失不得实例化表现层的约束。
- 修复要求：将 UI 组合/挂载门禁集中到 `OnInventoryOpened` 和 `ApplySettingsSnapshot`；关闭、隔离、SafeMode、卫星不可用时对称 `Unmount`、结束 presenter/runtime drag，并保持原生 Pass-Through。

### B2：surface 重绑未使当前 drag generation 失效

- 位置：`src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs:210-218`。
- 新的 `IInventorySurfaceContext` 到达时仅覆盖 `currentSurface/currentContainer/currentSessionGeneration` 并重绑 sink，没有先结束旧 runtime/presenter drag。
- 若原生调用序列在关闭回调之前发生重绑，旧 drag generation 会继续被 presenter 接受；仅依靠新的 target-container 比较不能替代“重绑立即失效旧拖拽”的规格要求。
- 修复要求：在接受新 surface 前原子执行旧 sink 隐藏/卸载、`runtime.EndDrag()`、`previewPresenter.EndDrag()`，再捕获新容器和新 SessionGeneration。

上述两项修复后必须重新运行 Release 构建、全套测试并再次独立审计。

## 六、残余风险与未覆盖项

以下不阻断本票纯 C# 关闭，但必须保留为后续证据义务：

1. 尚未证明真实 Unity/Glazier surface callback、真实原生拖拽 Hook 和真实 UI 卫星装配。
2. 尚未证明单人、SteamP2PFriends Host/Client、U3DS 三环境运行；同一候选 DLL 哈希证据仍属于 DEV-15E。
3. 当前清理计时、游戏主线程约束与真实引擎分配行为仍需实机验证。
4. 本报告不宣称库存权威、网络投影、服务器接受或发布授权通过。

## 七、结论

R2 审计确认 DEV-15D 的构建与自动化测试已恢复通过，但 B1/B2 仍阻断工单关闭。不得将 DEV-15D 置为 `resolved`，也不得进入 DEV-15E Qualification Evidence，直到两项前端生命周期门禁修复并完成新一轮构建与审计。
