# DEV-V4-05 Spec 轴审查·Round 2（全新实例）

审查范围：Round 2 增量及其与工单、规格、Q44–Q53 的行为匹配。未发现可确证的遗漏、范围蔓延或错误实现。

## F1 修复：CLEAN

- 规格要求：「有开关 = 拥有可停止生命周期 seam」，并明确「外部插件、管理面板自身、核心 Host/Contracts」不显示。
- `BueFeatureManagementEntry` 新增 `HasStoppableLifecycle`，并由组合根两个构造点分别传入真实机器事实：
  - `ClientUiCompositionRoot.cs:95-114`（官方条目）
  - `ClientUiCompositionRoot.cs:124-164`（目录条目）
  - 单点事实读取为 `TryGetStatus` 成功即拥有 seam：`ClientUiCompositionRoot.cs:167-187`
- 模型与 UI 均使用 `HasStoppableLifecycle && StateHasEnableToggle(...)`：
  - `ManagementPanel.cs:819-834`
  - `ManagementPanel.cs:941-963`
  - UI 开关渲染：`BueNativeManagementPanel.cs:989-1011`
- Round 2 新测覆盖「状态映射但机器未跟踪」条目，确认状态行仍为「运行中」、无开关且拒绝意图：`DevV4FeatureToggleSurfaceTests.cs:62-80`、`Plugin.Tests/Program.cs:402-435`。

## Q46 状态表与 Q45 seam 判据：裁定成立

规格分别规定状态文案映射和开关所有权；并未要求无 seam 条目改投影为「不可用」。当前实现无 seam 但状态为 Running 时仍显示「运行中」，仅移除开关，符合两条独立裁决。映射实现见 `ManagementPanel.cs:966-994`。

## 其余验收点

- 九态逐字映射、Discovered=待启动 与 Starting=启动中：`ManagementPanel.cs:969-984`。
- 非 UserDisabled 使用「已停止」：`ManagementPanel.cs:979-981`。
- 目标与只读状态分离、待生效提示及 Isolated 特殊文案：`ManagementPanel.cs:947-958`、`1009-1015`。
- 保存通过 `TryToggleFeature` 交给 03 seam，未增加面板分支：`ManagementPanel.cs:1158-1163`。
- `bue.network` 文案未改；外部插件分支仍无启停表面：`BueNativeManagementPanel.cs:1013-1027`。
- 不做项、候选纪律及 SDK 不扩面均未被 diff 破坏。

## 具名判断 1–4

均为实现层相容解释，没有越权改变规格、契约或生命周期语义；尤其判断 4 仅处理状态变化后的既有草稿，不新增外部行为。

verdict: CLEAN
