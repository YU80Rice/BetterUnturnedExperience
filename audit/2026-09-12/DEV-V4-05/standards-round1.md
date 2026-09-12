# DEV-V4-05 Standards 轴审查·Round 1（全新实例）

## 阻断性违规
无。

## 过程/架构门（证据，非阻断）
- 红测先行：`audit/2026-09-12/DEV-V4-05/red-build.log` 观测到 `GetFeatureStatusProjection` CS1061 与 8 参构造 CS1729。最终 `DevV4FeatureToggleSurfaceTests.cs` 已无 `Seed` 调用，与红日志有漂移，但生产缝缺失被真实编译失败覆盖。
- 白名单 csproj：`BetterUnturnedExperience.ClientUi.Tests.csproj` 已显式 `Compile Include="DevV4FeatureToggleSurfaceTests.cs"`。
- 公开面：`PanelFeatureStatusView` / `GetFeatureStatusProjection` / 条目新字段均 `internal`（`ManagementPanel.cs`），本票零公开成员。
- 面板非第二事实源：投影读 `features`+草稿；`StopReason`/`StatusDiagnostic` 由 `ClientUiCompositionRoot.cs` 经 `BueFeatureStartRuntime.TryGetStatus` 喂入，无面板私账。
- 测试：只断言模型缝外显（开关/九态文案/草稿目标/保存 handler），不断言 Glazier 树或私有字典。
- 候选纪律：`changed-files.txt` 仅源码与测试，无 RELEASES / CaseId / 候选 DLL。

## Deferrable smells
1. Duplicated Code — `ClientUiCompositionRoot.cs` `OfficialManagementEntry` 与 `ToManagementEntry` 重复「TryGetStatus → stopReason/diagnostic」块。
2. Repeated Switches — `ManagementPanel.cs` `ProjectFeatureStateText` 与 `StateHasEnableToggle` 对同一 `FeatureState` 各维护一份映射；新态须改两处。
3. Data Clumps — `StopReason`+`StatusDiagnostic` 成对穿过条目构造；`EnableTogglePending`+`PendingEffectText` 双通道表达待生效（原生层只读后者）。

verdict: CLEAN（0 hard / 3 smells）
