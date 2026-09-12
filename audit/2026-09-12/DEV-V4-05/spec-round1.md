# DEV-V4-05 Spec 轴审查·Round 1（全新实例）

verdict：NOT CLEAN（1 条错误实现；无另行确认的范围蔓延）

## 1. 错误实现：开关门禁按状态枚举，而非按「可停止生命周期 seam」

**Spec 出处**

- 工单第 15–16 行：「有开关 iff 可停止生命周期 seam：官方、生态、NoOp、Network、v1compat。不显示：外部 BepInEx 插件、管理面板自身、核心 Host/Contracts。」
- 裁决 Q45 第 31 行：「由条目是否拥有可停止的 BUE 功能生命周期 seam 决定」

**证据**

- `src/BetterUnturnedExperience.ClientUi/ManagementPanel.cs:145-152`：`StateHasEnableToggle` 仅依据 `FeatureState` 是否属于八个映射态返回 true，没有检查条目是否实际拥有可停止 seam。
- `src/BetterUnturnedExperience.Plugin/ClientUiCompositionRoot.cs:135-143`：无法从 `BueFeatureStartRuntime.TryGetStatus` 获取状态时，默认 `state = FeatureState.Running`，随后该条目会满足状态门禁并显示开关。

因此，已登记但没有生命周期状态记录/可停止 seam 的 BUE 条目会被默认视为 Running 并获得「启用」开关，违反「iff seam」。组合根注释声称「无机器记录→安全文案」，但实际仍产生可用开关。

**修复建议**

将「是否拥有可停止生命周期 seam」作为显式的内部条目能力/来源事实传入模型；无状态机记录或无 seam 的条目应投影为「不可用」且不显示开关。补测无 seam 的 BUE/生态条目，以及明确验证官方、NoOp、Network、v1compat 的正例。

## 相容但值得点名的判断

1. 表现状态中文化（可用/表现降级等）是未钉死文案下的 ClientUi 相容解释，不构成范围蔓延。
2. Discovered/Stopping/Isolating 的目标基线沿用既有 `IsFeatureCurrentlyEnabled`，可接受；但其机侧语义不应替代 seam 门禁。
3. Isolated+停用显示目标差异文案但不预演生命周期结果，符合 Q44/Q47。
4. 无开关时模型仍可能保留旧草稿、原生层不渲染提示，属于保留用户意图的相容处理。
