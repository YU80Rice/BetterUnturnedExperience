# DEV-V4-05 Standards 轴审查·Round 2（全新实例）

## 阻断性违规
无。

对照成文门：`HasStoppableLifecycle` / `PanelFeatureStatusView` / `GetFeatureStatusProjection` 均为 `internal`（`src/BetterUnturnedExperience.ClientUi/ManagementPanel.cs:23-60,235-256,941-964`），零公开面，不触 public≠契约。组合根 `TryReadMachineLifecycleFacts` 单点读 `BueFeatureStartRuntime.TryGetStatus`（`src/BetterUnturnedExperience.Plugin/ClientUiCompositionRoot.cs:174-188`），面板只投影喂入事实，非第二事实源。原生层只读 `status.ShowsEnableToggle`（`src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs:990-1012`）。csproj 已白名单 `DevV4FeatureToggleSurfaceTests.cs`；测试只断言模型缝外显（开关/文案/拒意图），不断言 Glazier 树。本票无 RELEASES/CaseId/候选 DLL。

R2 把门禁改为 `HasStoppableLifecycle && StateHasEnableToggle`（同文件 `:830-831,:961`），闭合 R1「按枚举推断 seam」；R1 组合根重复 `TryGetStatus` 已抽到单点，不再成立。

## Deferrable smells
1. Repeated Switches — `ProjectFeatureStateText`（`:969-985`）与 `StateHasEnableToggle`（`:989-994`）各维护一份 `FeatureState` 映射；新态仍须改两处。
2. Data Clumps — `StopReason`+`StatusDiagnostic`+`HasStoppableLifecycle` 仍以松散构造参数穿过条目（`:46-59,:164-165`）；`EnableTogglePending` 与 `PendingEffectText` 双通道表达待生效，原生只读后者（`:1009-1011`）。
3. Speculative Generality（fail-open 默认）— `hasStoppableLifecycle = true`（`:49`）。组合根虽显式传入，省略第 9 参的调用点仍被当成「有 seam」。与 F1「所有权不得从缺省推断」同向张力；生产路径已显式，故不升级。

verdict: CLEAN
