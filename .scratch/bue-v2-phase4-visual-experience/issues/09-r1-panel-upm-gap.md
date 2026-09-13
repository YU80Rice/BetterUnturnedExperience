# V4-R1 管理面板与 UPM 控件差集盘点

- **Ticket**: V4-R1
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T2 / T3 / T7 的共同事实输入。对照一手源码与 UPM 快照，产出差集报告（`.scratch/bue-v2-phase4-visual-experience/research/2026-09-11-V4-R1-panel-upm-gap.md`）。

盘点对象：

- `src/BetterUnturnedExperience.ClientUi/ManagementPanel.cs`（模型：条目字段、设置行、`TryToggleFeature`、有无描述）
- `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs`（实际画出的控件）
- `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` 的 `SettingDescriptor` / `SettingKind` / `IFeatureSettingsRegistration`
- 官方功能如何填 `DisplayNameKey` / `DescriptionKey` / `AllowedValues`（LIT/LIR/LHT/BII/Network/NoOp）
- 外部插件路径：`LoadedPluginCatalogAdapter`、ConfigEntry 编辑范围、`RequiresRestart`、`HasDiscreteConstraint`
- UPM 本体快照 `docs/third-party/UnturnedPluginManager-snapshot/PluginManagerMod.cs`（采用提交 9b75730 / v26.8.11.3）
- UPM 技能包 `docs/third-party/unturned-plugin-dev/SKILL.md`（写到 v26.8.13.3 的标签：Cycle / ItemList / BlueprintList / CreatureList / Category）
- ADR-0002 允许编辑的类型边界

输出必须逐项区分：

```text
UPM 已有 / BUE 模型已有但未画 / BUE 契约已有但未填 / BUE 未有
本阶段开图已收（描述、配置行描述+控件、Cycle、功能级启停） / 开图已划后续
```

只查证不改码。每条结论带 file:line。不要把技能包里比快照新的控件写成「快照已实现」。

## Answer

差集已按六态标签落盘。要点：UPM 快照 v26.8.11.3（9b75730）已有描述行（截断 120）、bool 开关、文本框、Cycle 左右键、ItemList/BlueprintList、程序集/配置路径；**没有** CreatureList / Category（那是技能包 v26.8.12.1 / v26.8.13.3，不得写成快照已实现）。BUE 模型有 `TryToggleFeature` 但原生未画；契约有 `DisplayNameKey`/`DescriptionKey`/`Choice`+`AllowedValues` 但官方全填 SettingId 且生产零条 Choice；外部 `AcceptableValueList` 被标只读、不认 Cycle。本阶段开图已收：描述、配置行描述+控件、Cycle、功能级启停；选择器/分类/路径为后续。

报告：[2026-09-11-V4-R1-panel-upm-gap.md](../research/2026-09-11-V4-R1-panel-upm-gap.md)

## Comments
