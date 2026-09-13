# V4-R2 背包整理标题栏与设置现状

- **Ticket**: V4-R2
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T5 的事实输入。对照 LIT 源码产出现状报告（`.scratch/bue-v2-phase4-visual-experience/research/2026-09-11-V4-R2-lit-header-settings.md`）。

必须回答：

1. `InventoryTidyUiPatch.cs` 每页画了哪些按钮、尺寸、注入位置、`headers[0..4]` 对应哪些物品栏、STORAGE 为何不注入。
2. 模式（同类/空间/大件）与方向（升/降）的数据存在哪：内存字典还是设置？Stop 时是否清零？默认值是什么？
3. `inventorytidy.enabled`（或等价 SettingId）如何声明、面板如何编辑、与功能生命周期的关系——是否存在「设置 enabled」和「功能状态」两套开。
4. `ITidyStrategy` / `StrategyId=default-grid-v1` 是否已有选择器 UI（预期：无）。
5. 全身整理（Ctrl+左键）是否绑在「整理」按钮上，去掉模式/方向按钮后这条手势还在不在。
6. 设置 facet：LIT 当前 `SettingDescriptors` 完整清单（Id、Kind、DisplayNameKey、DescriptionKey、AllowedValues）。

只查证不改码。结论带 file:line。Phase-2 O-LIT-1「改排序规则」与本票无关，不要扩成算法研究。

## Answer

每页三按钮（模式 60×60 / 方向 40×60 / 整理 60×60）注入 `headers[0..4]` = Hands..Pants；STORAGE 不注入。模式/方向是每页内存字典（默认同类+降序），Stop 清零，不是设置。唯一 SettingDescriptor 是 `inventorytidy.enabled`（Toggle，默认开）；它与 `SetFeatureEnabled` 生命周期是两套开。无 StrategyId 选择器。Ctrl+左键全身整理绑在「整理」按钮上，去掉模式/方向后仍在。

报告：[2026-09-11-V4-R2-lit-header-settings.md](../research/2026-09-11-V4-R2-lit-header-settings.md)

## Comments
