# V7-R4 预放置框着色口与深色来源

- **Ticket**: V7-R4
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T6 的事实输入。对照 BII 预放置框着色产出现状报告（`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R4-preview-frame-color.md`）。

必须回答（只查证不改码，结论带 file:line）：

1. `PreviewFrameKind.ValidGreen/InvalidRed` → `PreviewFrameColor` → `InventorySurfaceLifecycleAdapter.Color` 的完整写入链；绿/红 `SleekColor` 的 RGBA 字面量。
2. 为何实机仍可能看见深色：`ESleekTint.BACKGROUND` 回退在哪些分支触发；`box == null` 是否导致 Color setter 空操作；Glazier 池化 / 重建是否丢掉 BackgroundColor；原版 drag 幽灵是否盖在 BUE 框上面。
3. `ShowFrame` 对 LocallyInvalid 是否真的走到 InvalidRed；Occupied / OutsideGrid / 互换占用各是什么 `PlacementPreviewState`。
4. 框挂在 `GridPanelContainer` 还是顶层；有无第二个预览框（原版 + BUE）。
5. 哪些测试断言了颜色，哪些只断言 Kind 枚举（换可见色后是否仍绿）。

不要在本票改颜色。不要把「代码里有 ValidGreen」写成「玩家已经看见绿色」。

## Answer

对照报告：`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R4-preview-frame-color.md`。未改 `src/`、未改颜色。

1. **写入链**：`InventoryPreviewPresenter.Update`（`InventoryPreviewWiring.cs:534,547-548`）把 `Candidate→ValidGreen`、其它已 Show 的状态（即 `LocallyInvalid`）`→InvalidRed`；`ApplyFrame`（`ItemInteractionUiComponent.cs:213-215`）写成 `PreviewFrameColor`；`UnturnedVisualElement.Color` setter（`InventorySurfaceLifecycleAdapter.cs:66-75`）写 `ISleekBox.BackgroundColor`。字面量：绿 `Color(0.2f, 1f, 0.3f, 0.85f)`，红 `Color(1f, 0.25f, 0.2f, 0.85f)`。这是写入意图，不是实机像素。

2. **深色候选（代码级，非观测判决）**：`ESleekTint.BACKGROUND, 0.6f` 只在非绿非红（`None`）且 `box != null` 时写（同 setter `74`）；`ApplyFrame` 从不写 `None`。`box == null` 则 setter 直接 return（`71`），框可显示但着色口空操作，留下 Glazier 默认底。池化可让 `set_BackgroundColor` NRE（注释 `ItemInteractionUiComponent.cs:102-106`）；FB1b 重建重试一次，仍失败则 Hide。原版 `dragItem` 未被隐藏，`updateDraggedItem` 仅 postfix（`InventoryDragPreviewAdapter.cs:137-138,983-1041`），幽灵可盖在格子框上。

3. **LocallyInvalid 逻辑上走 InvalidRed**（`Wiring.cs:534` + Dev15B `164-173`）。Occupied 满格 → `LocallyInvalid+Occupied`；物品大于网格 → `LocallyInvalid+OutsideGrid`；光标出网格 → `Hidden+OutsideGrid`（无框）。互换占用无独立 State，预览仍是 LocallyInvalid+Occupied，释放时 pass-through 原版 swap（`DragPreviewAdapter.cs:878-884`）。

4. **框挂 `SleekItems.itemsPanel`（GridPanel），不在顶层**；BUE icon 挂 `PlayerUI.container`。BUE 格子框一套；另有原版 `dragItem` 幽灵，Candidate 时还可与 BUE 顶层 icon 并存。

5. **测试只锁 Kind/枚举**（Dev15B `160,173,292,318`）。无测试读 RGBA。Mock `Color` 是自动属性；生产 getter 恒 `None`。改可见色现有测试仍绿。
