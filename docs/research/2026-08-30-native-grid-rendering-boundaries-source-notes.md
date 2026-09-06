# DEV-16D 调研素材汇总（会话交接包，2026-08-30）

> 目的：主会话上下文耗尽（smart zone 超限，三轮子代理同因死亡），将调研素材落盘供后续会话撰写正式报告 `2026-08-30-native-grid-rendering-boundaries.md`。
> 来源：本会话三轮侦察（f6a62346 拖拽回调链、939fb5b5 渲染流程、Glazier 源码验证）+ R34-R44 真机判读。

## Provenance

`D:\Agent-工作目录\U3-SDK` = **官方开源源码**（github.com/SmartlyDressedGames/U3-SDK，提交 ea7b4973，2026-07-07，Unity 2022.3.62f3 完整工程，可编辑器 play）。**非反编译产物**。Glazier 框架源码完整：`Assets/Runtime/SDG.Glazier/`。

## 一、完整调用链（源码确认，绝对路径:行号）

基路径 `U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\`：

1. **UI 构建**：`UI\Player\PlayerDashboardInventoryUI.cs` L2564 面板 container（SleekFullscreenBox）；L3099-3102 `dragItem = new SleekItem()` 挂 **PlayerUI.container**（全屏根）；L2581-2587/L2717-2731 dragOutside 全透明遮罩（挡点击）
2. **委托接线**：`Sleek\SleekItems.cs` L9-12 委托声明（onSelectedItem/onGrabbedItem/onPlacedItem）、L16-18 公开字段；PlayerDashboardInventoryUI 构造时接线到 L1154 onPlacedItem
3. **每帧更新**：`UI\Player\PlayerUI.cs` L2173 Update → L2181 `PlayerDashboardInventoryUI.updateDraggedItem()`（L2418-2438，仅 isDragging 时动作）；内部 refreshDraggedVisualPosition L2408-2416（`dragItem.PositionScale = PlayerUI.container.ViewportToNormalizedPosition(InputEx.NormalizedMousePosition)`——跟随原语）+ updatePivot L2381-2403（R 键旋转换 dragPivot）
4. **鼠标命中**：`Sleek\SleekItems.cs` L219-227 onClickedGrid → `grid.GetNormalizedCursorPosition()` 风格的归一化坐标 → onPlacedItem.Invoke(page, x, y)
5. **抓取**：`Sleek\SleekItem.cs` L231-234/250 左键 → onDraggedItem → PlayerDashboardInventoryUI.onGrabbedItem L1084（记 dragJar/dragFrom*）→ startDrag L200-221（isDragging=true + setItemsEnabled(false)）
6. **失焦**：setItemsEnabled L246-257 → `Sleek\SleekItem.cs` L53-65 `disable()`（BackgroundColor+icon.color 的 **alpha 0.5**）——"变浅"本体；原位"虚影"= 源格同被 disable，无专用字段
7. **放置**：onClickedGrid → onPlacedItem.Invoke → PlayerDashboardInventoryUI.onPlacedItem L1154-1325 分支：网格间 sendDragItem L1248-1254 / 槽位 equip L1256 / AREA sendDropItem L1215-1221 / swap sendSwapItem L1292-1314 / 网格外 sendDropItem L1407-1420（onClickedDuringDrag L1407）
8. **图标刷新**：`Sleek\SleekItem.cs` L106-229 updateItem → `jar.GetAsset()` L117 → `icon.Refresh(id, quality, state, asset)` L156 → `ItemTool.getIcon(...)` 异步回调（ItemIconReady(int, Texture2D)）设 internalImage.Texture
9. **提交**：`Player\PlayerInventory.cs` sendDragItem L967 / onInventoryAdded/Removed/Updated L141-143 / updateItems L1302

## 二、坐标系（Glazier 源码确认）

- `SDG.Glazier\GlazierElementBase.cs` L579-580：abstract `ViewportToNormalizedPosition(Vector2)` / `GetNormalizedCursorPosition()`
- uGUI 实现 `Assembly-CSharp\Glazier_uGUI\GlazierElementBase_uGUI.cs`：
  - **GetNormalizedCursorPosition L356-362**：`(mouse.x - rect.xMin)/rect.width, (Screen.height - mouse.y - rect.yMin)/rect.height` —— **输入 Input.mousePosition（屏幕底左 Y↑），内部已做 Y 翻转 + GetAbsoluteRect，输出相对网格左上（Y 向下）归一化 0-1**
  - **ViewportToNormalizedPosition L349-354**：输入屏幕归一化（底左 Y↑）→ 输出网格局部归一化（顶左 Y↓）——dragItem 跟随用
  - transform 属性 L506（internal，插件不可直接访问）
- `Framework\Extensions\RectTransformExtension.cs` L19-26 **GetAbsoluteRect**：`position.x, Screen.height - position.y`（Y 翻转）+ lossyScale 缩放 + pivot 修正 —— **屏幕底左系矩形**
- **结论**：**R44 的用法（GetNormalizedCursorPosition × grid 像素）就是官方语义的正确消费**——换算公式无需再修

## 三、BUE 现状 vs 原生（差异清单）

| BUE 当前 | 原生/建议 | 差异后果 |
|---|---|---|
| 预览 sink 挂 GridPanelContainer（itemsPanel 中间层） | 侦察建议：仿 dragItem 挂 **PlayerUI.container**（全屏根，绘制在面板之上） | 面板内挂载被 dragOutside 遮罩挡点击、随格子 disable 变暗 |
| 指针换算（R44）= GetNormalizedCursorPosition × grid 像素 | 同 | ✓ 语义正确（R44 后） |
| viewport clip = grid 像素（grid-local，R43） | — | ✓（R44 后） |
| onPlacedItem 委托重绑 | 原生委托字段公开 | ✓ 可行（R33 实证） |
| updateDraggedItem postfix 轮询 | PlayerUI.Update L2181 驱动 | ✓ 可 patch（实测） |
| onPlacedItem Harmony patch | **IL Compile Error（真机实证）** | ✗ 不可行——委托重绑替代 |
| PlayerUI.Update Harmony patch | **IL Compile Error（真机实证）** | ✗ 不可行——updateDraggedItem 替代 |

## 四、真机判读链（R34→R44 摘要）

- R34 fail-open 守卫 → 物品可移动恢复
- R35 幂等重绑 → 闪退消失（Storage/Trunk 共享 page-7 网格的包装链）
- R36 判别 → enhanced/canRun/sinkBound 全 True，preview-stale previewGen=0 → Contains 门（clip=SizeOffset_X=0）
- R37 条件 clip 门 → LastPreview 填充
- R41 降级 → dispatch 恢复（GPT fail-closed 层级快照真机恒失败）
- R42 AttachGrid 直取 NativeItems → placement-decision 首现（page=7 Submitted ×3）
- R44 grid-local 统一空间 → **用户实测仍失败**（预览不出现）——**失败模式待 R45 判别**（GetNormalizedCursorPosition 在真机的实际返回值需日志验证；预览元素挂载层需改 PlayerUI.container）

## 五、R13（GPT 工单）相关

- 冻结基线：`snapshots/DEV-16D-implementation-state-freeze-20260901.md`
- R13 工单：`.scratch/.../issues/DEV-16D-R13-native-conformance-remediation.md`（claimed by GPT）
- 工作树：GPT 的 staged 半成品（NativeItemGridOccupancySnapshot.cs 等）

## 六、待真机/Unity 编辑器验证清单

1. `GetNormalizedCursorPosition()` 在真实客户端网格上的返回值分布（网格内 vs 外、Y 语义）
2. 预览元素挂 PlayerUI.container 后的可见性（遮罩/变暗规避）
3. 黑色框的确切来源（box BackgroundColor vs icon 黑块）
4. 坐标换算的最终校准（OriginX/Y 若仍需，可用 placement-decision 格位 vs 实际对照）
5. Unity 编辑器 play（GameStartup.unity）作为中间验证档
