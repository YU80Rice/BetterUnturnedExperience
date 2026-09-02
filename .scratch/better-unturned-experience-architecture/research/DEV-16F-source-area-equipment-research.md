# DEV-16F：地面(AREA)/装备槽(0/1)源拖拽增强可行性研究

> 调研日期：2026-09-01
> 结论等级：`SOURCE_CONFIRMED`；本文件基于 U3-SDK 主源码与 BUE 当前源码做静态判定，不宣称真实客户端运行通过。
> 原始来源：`D:\Agent-工作目录\U3-SDK`（`PlayerInventory.cs`、`PlayerDashboardInventoryUI.cs`、`SleekItems.cs`），以及 BUE 仓库 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\src\...` 三处实现文件。
> 研究问题：当**源**为「附近的物品」AREA(8) 或「手持装备/快捷键 1、2 栏位」页 0/1 时，BUE 是否/如何能安全地提供增强版预放置预览与自动旋转；原生两例的提交 RPC 是什么。

## 1. 结论摘要

- **AREA(8) 源 → 普通网格(2-7)**：原生 vanilla 本身就支持，且在 `PlayerDashboardInventoryUI.onPlacedItem` 里已有一个专门的 `dragFromPage == AREA` 分支，通过 `ItemManager.takeItem(...)` 在客户端请求把地面物品放入目标网格。目标网格的 `onPlacedItem` 委托**无论源是什么页面都会触发**。BUE 的网格 wrapper 只包装被拖放到的那一格网格（页 2-7），因此 AREA 源拖放会照常进入原生 takeItem 分支。
  **可行**：BUE 只需在 release 时用 `INativeInventoryDragActions.TakeGroundItem(target)`（其内部调 `ItemManager.takeItem(interactable.transform.parent, x,y,rot,page)`），与原生 `onPlacedItem` AREA 分支语义一致。AREA 源不在任何网格里，因此无需做源 footprint 排除。

- **装备/热键槽页 0/1 源 → 普通网格(2-7)**：原生 vanilla 同样支持，且走的是**普通**提交路径：目标网格的 `onPlacedItem` 进入通用分支，调用 `PlayerInventory.sendDragItem(page_0=0/1, ..., 目标网格, ...)`。服务端 `ReceiveDragItem` 接受 `page_0 < PAGES-1` 的 0/1 源，并在 `page_0 < SLOTS` 时调用 `player.equipment.sendSlot(page_0)` 完成卸下装备。
  **可行**：BUE 在 release 时用 `INativeInventoryDragActions.SendDragItem(source, target)` 即可（其内部调 `player.inventory.sendDragItem(source.Page=0/1, ..., target.Page, ...)`），与原生通用分支一致。

- **两类源的共同阻塞点（BUE 当前不改也能安全兜底，但不是功能阻塞）**：BUE 的源页面判定 `IsEnhancedSourcePage`/`IsSupportedEnhancedPage` 目前把 AREA(8) 与页 0/1 判为非增强源 → `dragSourcePassThrough` 为真，在 `OnDragStarted` 立刻 `EndDrag`，所以预览从不出现。要支持这两类源，需放宽这三个门（见第 4 节），但**目标网格已是受 BUE 包装的页 2-7**，release 兜底路径（native onPlacedItem）始终可用，因此不存在「BUE 阻塞了 vanilla 提交」的风险。

## 2. 固定证据来源与证据边界

| EvidenceSource | 绝对路径 | 主要事实 | 证据等级 |
|---|---|---|---|
| `U3SRC-PINV` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInventory.cs` | page 常量；`sendDragItem`/`ReceiveDragItem`；装备槽校验与 `sendSlot` | `SOURCE_CONFIRMED` |
| `U3SRC-PDINV` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\UI\Player\PlayerDashboardInventoryUI.cs` | slots(0/1)、items 网格(2-8)、`onGrabbedItem`、`onPlacedItem` 的 AREA 分支与通用分支、`checkEquip`/`checkSlot` | `SOURCE_CONFIRMED` |
| `U3SRC-SITEMS` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Sleek\SleekItems.cs` | 各网格的 `grid.OnClicked` → `onPlacedItem(page,x,y)` 触发不依赖源页面 | `SOURCE_CONFIRMED` |
| `BUESRC-UI` | `...\src\BetterUnturnedExperience.ClientUi\ItemInteractionUiComponent.cs` | `IsSupportedEnhancedPage`、`SupportedLiveSurfacePages`、`OnDragStarted` 的 pass-through 门 | `SOURCE_CONFIRMED` |
| `BUESRC-ADAPTER` | `...\src\BetterUnturnedExperience.ClientUi\NativeInventoryInteractionAdapter.cs` | `IsEnhancedSourcePage`/`IsOrdinaryGrid`、release 主门 | `SOURCE_CONFIRMED` |
| `BUESRC-ADAPTER-PLUGIN` | `...\src\BetterUnturnedExperience.Plugin\InventoryDragPreviewAdapter.cs` | `IsSupportedPage`、`GridPlacedItemWrapper`、`EvaluatePlacement`、`ReadDragSource`、`NativeDragActions.TakeGroundItem/SendDragItem` | `SOURCE_CONFIRMED` |

`BUILD_CONFIRMED`、`RUNTIME_CONFIRMED`、`RELEASE_CONFIRMED` 不由静态源码推出；特别是 `ItemManager.takeItem` 在网络往返中的成功判定、以及不同 Glazier backend 的命中行为仍需实机验证。

## 3. 页面模型（问题 1）

`PlayerInventory.cs:64-79` 固定页面常量与注释：

```csharp
public static readonly byte SLOTS  = 2;   // :64
public static readonly byte PAGES  = 9;   // :65
public static readonly byte BACKPACK = 3; // :66
public static readonly byte VEST   = 4;   // :67
public static readonly byte SHIRT  = 5;   // :68
public static readonly byte PANTS  = 6;   // :69
public static readonly byte STORAGE = 7;  // :70
public static readonly byte AREA   = 8;   // :71
// 0 Primary     // :72
// 1 Secondary   // :73
// 2 Hands
// 3 Backpack
// 4 Vest
// 5 Shirt
// 6 Pants
// 7 Storage
```

页 0/1 即 Primary/Secondary 武器/装备槽；页 2-7 是玩家网格；页 8（AREA）不是玩家库存页，而是「附近地面物品」的临时 UI 容器（`resetNearbyDrops` 用 `areaItems` `replaceItems(AREA, areaItems)` 填充，`PlayerDashboardInventoryUI.cs:1777-1779`）。

装备槽属性由资产决定：`PlayerInventory.tryAddItem` 对 `page < SLOTS` 要求 `asset.slot.canEquipInPage(page)` 且强制 `rot = 0`（`PlayerInventory.cs:424-432`）——这是「装备槽不可旋转、只能正放」的原生约束，BUE 在做装备源预览时必须遵守（见第 5.2 节风险）。

## 4. BUE 当前门控位置（问题 4）

三处静态门把 AREA(8) 与页 0/1 排除出增强流程：

### 4.1 表面注册门（源/目标均为网格才进增强表面）

`ItemInteractionUiComponent.cs:221` `SupportedLiveSurfacePages = { 2, 3, 4, 5, 6, 7 }`；
`ItemInteractionUiComponent.cs:287-293`：

```csharp
internal static bool IsSupportedEnhancedPage(byte page)
{
    return page >= 2 && page <= 7;   // :292
}
```
`RegisterInventorySurface`（`:372-405`）在 `:378` 用该函数拒绝非网格页 → AREA(8) 与页 0/1 永不成为增强目标表面。

### 4.2 源页面 pass-through 门（当前阻塞 AREA/0/1 的根因）

`ItemInteractionUiComponent.cs:573-601` `OnDragStarted`：

```csharp
dragSourcePassThrough = !IsSupportedEnhancedPage(source.Page);   // :582
...
if (dragSourcePassThrough)                      // :586
{
    runtime.EndDrag();
    previewPresenter.EndDrag();
    HidePreview();
    return;
}
```
即 `source.Page ∈ {8, 0, 1}` 时 `dragSourcePassThrough = true`，`OnDragStarted` 直接 `EndDrag`+隐藏预览，后续 `OnDragUpdated` 在 `:607` 因 `dragSourcePassThrough` 直接 `HidePreview`。**这就是用户看到“AREA/装备源拿起时没有增强预览/自动旋转”的直接原因。**

### 4.3 release 提交门

`NativeInventoryInteractionAdapter.HandleRelease`（`NativeInventoryInteractionAdapter.cs:55-113`）：

```csharp
if (!IsEnhancedSourcePage(input.Source.Page))   // :66
    return NativeDragAdapterOutcome.PassThrough;
var target = input.Preview.Candidate;
if (!IsOrdinaryGrid(target.Page))               // :72
    return NativeDragAdapterOutcome.PassThrough;
...
native.SendDragItem(input.Source, target);      // :110 走 sendDragItem
native.StopDrag();
return NativeDragAdapterOutcome.Submitted;
```

其中：

```csharp
// :118-121
private bool IsOrdinaryGrid(byte page)
    => page >= slotsPageBoundary && page < areaPage;   // slotsPageBoundary=SLOTS=2, areaPage=AREA=8
// :126-129  IsEnhancedSourcePage 就是 IsOrdinaryGrid
```

因此 `source.Page == 8`（`8 < 8` 为假）与 `source.Page ∈ {0,1}`（`0/1 >= 2` 为假）都在 `:66` 直接 PassThrough。注意 `TakeGroundItem` 已定义但**从未被调用**（现有 `HandleRelease` 只走 `SendDragItem`）。

### 4.4 网格包装门（决定 release 兜底是否可达）

`InventoryDragPreviewAdapter.cs:279-282`：

```csharp
internal static bool IsSupportedPage(byte page)
    => page >= HandsPage && page <= StoragePage;   // Hands=2, Storage=7, :270-277
```
`AttachNativeGrid`（`:193-224`）只在页 2-7 包装 `SleekItems.onPlacedItem`（`:213` 换成 `GridPlacedItemWrapper`）。AREA 网格（页 8，`items[AREA-SLOTS]`）与装备槽（`SleekSlot`）的 `onPlacedItem` 保持原生。

`GridPlacedItemWrapper`（`:353-363`）对页 2-7 目标调用 `EvaluatePlacement(page,x,y)`，`EvaluatePlacement`（`:781-836`）在 `:789` 判定 `isOrdinaryGrid = page >= SLOTS && page != AREA`（目标页），然后走 `OnDragReleased`。**这里 `IsOrdinaryGrid` 检查的是目标页（2-7，恒为普通网格），不检查源页** —— 所以原始 AREA/0/1 源拖进 2-7 时，`EvaluatePlacement` 不会在 `:789` 拦截；真正把源挡回 pass-through 的是 4.3 的 `NativeInventoryInteractionAdapter.HandleRelease:66`（源页检查）。`HandleRelease` 返回 PassThrough 后 `EvaluatePlacement` 在 `:828` 令 `return true` → 转发原生 `node.NativeHandler(page,x,y)`，于是 vanilla 的 AREA 分支 / 装备通用分支照常执行。

## 5. 原生语义（问题 2 / 3）

### 5.1 抓取（onGrabbedItem `PlayerDashboardInventoryUI.cs:1084-1151`）

- AREA 源：`dragFromPage = page`（AREA=8）照常记录（`:1118`）。拿起的 `dragJar` 来自 `inventory.getItem(AREA, getIndex(AREA,x,y))`，其 `interactableItem` 指向真实地面物件（`areaItems` 的 jar 在 `createElementForNearbyDrop` 里 `jar.interactableItem = interactableItem`，`:2462`）。
- 装备/普通源：任何页面都用同一 `dragFromPage = page` 逻辑，页 0/1 亦如此。

`SleekSlot` 与 `SleekItems` 共用同一静态 `onGrabbedItem`（前者 `:2632`，后者 `:2805`），后者把 `onDraggedItem` 事件的 `page` 设为自身 `SleekItems` 的 page（`SleekItems.cs:214-217`）。因此 AREA/0/1 源拿起时 `dragFromPage` 分别为 8、0、1。

### 5.2 放置（onPlacedItem `PlayerDashboardInventoryUI.cs:1154-1325`）—— 关键分支（问题 2/3）

`ConsumeEvent()`（`:1156`）后若 `dragSource != null && isDragging`（`:1158`）：

1. `page >= SLOTS` 时按 `dragPivot/50` 修正 target top-left 并 clamp 到网格内（`:1160-1195`）——这是**目标**页修正，与源无关。
2. 目标为装备槽时校验 `asset.slot.canEquipInPage(page)`（`:1203-1206`）。
3. 拖回原位置则不动作（`:1208-1213`）。
4. **目标为 AREA**：`sendDropItem(dragFromPage,...)` 丢弃（`:1215-1225`）。
5. **源为 AREA（地面拿起 → 目标网格）**：`:1227-1246`：

```csharp
if (dragFromPage == PlayerInventory.AREA)
{
    byte rot = dragJar.rot;
    stopDrag();
    if (page != dragFromPage)
    {
        if (Player.LocalPlayer.inventory.checkSpaceEmpty(page, x, y, dragJar.size_x, dragJar.size_y, rot))
        {
            if (dragItem.jar != null && dragItem.jar.interactableItem != null)
            {
                ItemManager.takeItem(dragItem.jar.interactableItem.transform.parent, x, y, rot, page);  // :1240
            }
        }
    }
    return;
}
```
   地面拾取放置在客户端用 `ItemManager.takeItem(worldParent, x, y, rot, targetPage)`（目标网格页）。这就是问题 2 的答案：**原生 AREA→网格用的是 `ItemManager.takeItem`，不是 `sendDragItem`**。目标页的 `onPlacedItem` 在源为 AREA 时已经可达并特意处理（前置 `checkSpaceEmpty` 校验空格）。

6. **普通源通用分支**（`:1248-1263`，页 0/1 源走进这里）：

```csharp
if (Player.LocalPlayer.inventory.checkSpaceDrag(page, dragFrom_x, dragFrom_y, dragFromRot,
        x, y, dragJar.rot, dragJar.size_x, dragJar.size_y, page == dragFromPage))
{
    byte rot = dragJar.rot;
    stopDrag();
    Player.LocalPlayer.inventory.sendDragItem(dragFromPage, dragFrom_x, dragFrom_y, page, x, y, rot);  // :1254
    if (page < PlayerInventory.SLOTS)
    {
        Player.LocalPlayer.equipment.equip(page, 0, 0);
        PlayerDashboardUI.close();
        PlayerLifeUI.open();
    }
}
```
   当 `dragFromPage ∈ {0,1}`、`page`(目标) ∈ 2-7 时，命中此分支，调用 **`sendDragItem(source=0/1, ... → 目标网格)`**。这就是问题 3 的答案：**装备/热键槽源拖入普通网格的原生提交就是 `PlayerInventory.sendDragItem(page_0=0/1,...)`，没有独立的装备/卸装日志分支**；卸下装备由服务端在 `ReceiveDragItem` 内完成。

   一个细节：若源页 `< SLOTS` 且把物品拖进的是**另一个装备槽目标**（`page < SLOTS`），走 `checkSpaceDrag` 失败后的 `checkEquip`/`checkSlot` 装备交换分支（`:1264-1323`）。但 BUE 只增强「目标为普通网格 2-7」的场景，因此 BUE 走 :1254 的 `sendDragItem` 即可，不涉装备交换。

### 5.3 服务端 ReceiveDragItem 对 0/1 源的处理（问题 3 权威侧）

`PlayerInventory.ReceiveDragItem`（`PlayerInventory.cs:699-795`）：

- `page_0 < 0 || page_0 >= PAGES-1`（`>= 8`）即拒绝（`:722`）——页 0/1/2-7 均为合法源页，AREA(8) 不可作 `sendDragItem` 源（这印证了 AREA 必须走 `takeItem`，不能走 `sendDragItem`）。
- 目标装备槽校验：`page_1 < SLOTS && !canEquipInPage` 拒绝（`:773`）。
- `page_0 < SLOTS` → `player.equipment.sendSlot(page_0)`（`:786-789`）通知卸下源装备；`page_1 < SLOTS` 同理（`:791-794`）。
- 真正移动：`removeItem(page_0, index); items[page_1].addItem(x_1, y_1, rot_1, jar.item);`（`:783-784`）。

所以源 0/1 → 网格 2-7 是原生合法操作，用 `sendDragItem` 提交，服务端自动处理装备卸下。

### 5.4 目标网格的 onPlacedItem 是否“无论源都触发”（问题 5 关键确认）

`SleekItems.cs:219-227`：

```csharp
private void onClickedGrid()
{
    Vector2 cursorPosition = grid.GetNormalizedCursorPosition();
    byte x = (byte)(cursorPosition.x * width);
    byte y = (byte)(cursorPosition.y * height);
    onPlacedItem?.Invoke(page, x, y);   // :226
}
```
`grid.OnClicked += onClickedGrid`（`:247`）。每个 `SleekItems` 网格的 `onPlacedItem` 在被点击（落点）时触发，**事件负载只有目标 `page,x,y`，不携带源页信息**，因此无论当前源是 AREA/装备/网格，只要把物品拖放到该网格上，目标网格的 `onPlacedItem` 就触发。vanilla `onPlacedItem` 再据 `dragFromPage` 静态字段走对应分支。BUE 只是把页 2-7 的 `onPlacedItem` 换成 `GridPlacedItemWrapper`（转发给原生 handler），源为 AREA/0/1 时同样到达。

## 6. NativeDragActions 对照（问题 2 子项）

`InventoryDragPreviewAdapter.cs:954-975` `NativeDragActions`：

- `StopDrag()` → `PlayerDashboardInventoryUI.stopDrag()`（`:956`）。
- `SendDragItem(source, target)` → `player.inventory.sendDragItem(source.Page, source.X, source.Y, target.Page, target.X, target.Y, target.Rotation)`（`:958-964`）——与原生通用分支 :1254 一致，适合装备/网格源。
- `TakeGroundItem(target)`（`:966-974`）：

```csharp
public void TakeGroundItem(ItemGridPosition target)
{
    var adapter = ActiveAdapter;
    var dragItem = adapter == null ? null : adapter.ReadDragItem();
    var interactable = dragItem == null || dragItem.jar == null ? null : dragItem.jar.interactableItem;
    if (interactable == null || interactable.transform == null || interactable.transform.parent == null)
        throw new InvalidOperationException("native ground item is no longer available");
    ItemManager.takeItem(interactable.transform.parent, target.X, target.Y, target.Rotation, target.Page);
}
```
   与原生 `onPlacedItem` 的 AREA 分支 :1240 完全同形：都用 `dragItem.jar.interactableItem.transform.parent` 作为世界物件源、`ItemManager.takeItem(parent, x, y, rot, page)` 提交。**BUE 的 `TakeGroundItem` 已实现且语义正确，只是从未被调用**。

## 7. 可行性评估（问题 5）

### 7.1 (a) AREA(8) 源——可安全增强

| 要素 | 结论 | 证据 |
|---|---|---|
| 拖起时 `dragJar` 存在 | 是，`onGrabbedItem` 统一设置 `dragJar`/`dragFromPage=8` | `PlayerDashboardInventoryUI.cs:1112-1121` |
| 预览渲染前提满足 | `dragFromPage==8` 只是 read-only 状态；BUE 预览与源页无关，只依赖占位/目标网格 geometry | — |
| 源 footprint 是否需排除 | **否**。AREA 源不在任何玩家网格，occupancy 无需从目标里挖掉源占位 | — |
| release 提交调用 | `INativeInventoryDragActions.TakeGroundItem(target)` → `ItemManager.takeItem(parent, x,y,rot,page)` | `InventoryDragPreviewAdapter.cs:966-974`；`PlayerDashboardInventoryUI.cs:1240` |
| 目标网格 delegate 是否触发 | 是，`grid.OnClicked` 不携带源信息 | `SleekItems.cs:226` |
| 原生兜底 | wrapper 转发原生 handler，AREA 分支照常 | `EvaluatePlacement` 仅在 `HandleRelease:66` 判 PassThrough，兜底可达 |

**唯一前提**：要让预览出现，必须放宽 4.2 的 `IsSupportedEnhancedPage(8)`/4.3 的 `IsEnhancedSourcePage(8)` 门；并让 `HandleRelease` 对源 8 走 `TakeGroundItem` 而非 `SendDragItem`（`sendDragItem` 服务端拒绝 `page_0>=8`，`:722`）。放置到 AREA 目标页本身（把网格物品放到地面）不在本 issue 范围，保持原生 `sendDropItem`。

### 7.2 (b) 装备/热键槽页 0/1 源——可安全增强

| 要素 | 结论 | 证据 |
|---|---|---|
| 拖起时 `dragFromPage=0 或 1` | 是 | `onGrabbedItem:1118` |
| 预览渲染前提 | 与源页无关，满足 | — |
| 源 footprint 排除 | 目标网格的 occupancy 不含装备槽，但**同一物品在两页不可能同时存在**；装备槽源无需占用目标网格。若迁移到目标页后 BUE 的 `TryGetOccupancyForDrag` 把 `dragOriginContainer` 当作目标容器的一部分，需注意 origin==target 时勾除源 footprint 仅对「源也在目标网格」有意义——装到网格后源在 0/1，不在网格，故不勾除 | — |
| release 提交调用 | `INativeInventoryDragActions.SendDragItem(source, target)` → `sendDragItem(0/1, ... → grid)` | service `ReceiveDragItem:722` 接受源 0/1；`:786-789` 卸装备 |
| 不可旋转约束 | 源是装备槽时物品必须旋 0（服务端 `tryAddItem:431`、`ReceiveSwapItem` 强制 `rot=0` for `page<SLOTS`）；BUE 自动旋转若允许 R 会把 `dragJar.rot` 转到非 0，提交的 target.Rotation≠0，服务端在 `ReceiveDragItem` 没有强制源侧 rot=0，但为了让物品在网格里正放、且与下次拿起一致，应把 target 旋转按物品原始方向处理。**建议增强装备源时禁用自动旋转或忽略 R**。 | `PlayerInventory.cs:424-432, 780-781` |
| 目标网格 delegate 是否触发 | 是 | `SleekItems.cs:226` |

**唯一前提**：放宽 4.2 的 `IsSupportedEnhancedPage(0/1)`/4.3 的 `IsEnhancedSourcePage(0/1)` 门；装备源 release 走 `SendDragItem`（与 AREA 源区分开）。

### 7.3 统一风险清单

1. **两例都不存在「BUE 碰撞 vanilla」风险**：目标是受包装的页 2-7，BUE 对源判定 PassThrough 时会把提交交还原生 handler（AREA 分支 / 通用 `sendDragItem` 分支），vanilla 行为不丢失。
2. **`ItemManager.takeItem` 是客户端请求，服务端可能拒绝**（物品被另外的玩家抢走、距离/权限变化）：BUE 的部署投影等待（`AwaitingProjectionController`）已有处理——视觉等待超时不算服务端拒绝，保持 `SOURCE_CONFIRMED` 级别即可。
3. **AREA 源必须在 release 用 `TakeGroundItem`**；若错走 `SendDragItem`，服务端 `ReceiveDragItem:722` 会因 `page_0>=8` 直接 return，物品既不放网格也不掉地，体验错误。因此目标页选择与源分支必须成对区分。
4. **装备源旋转**：原生装备槽强制不可旋转（`rot=0`），增强时应同步约束，否则预览与提交 rotate 会与原生语义冲突。
5. `dragItem.jar.interactableItem` 在拖拽期间可能被销毁（原生 :1238 已做非空判断）；`TakeGroundItem:971` 在无 interactable 时抛异常，release 兜底必须捕获并回退原生（`EvaluatePlacement` 的 guarded wrapper 已捕获异常并转发原生 handler，`:373-426`）。

## 8. 裁定

- **BUE 可安全增强 AREA 源拾取**（拖拽中于网格上渲染预放置预览/自动旋转，release 用 `NativeDragActions.TakeGroundItem(target)` → `ItemManager.takeItem(parent,x,y,rot,targetPage)`）。阻塞项：放宽 `IsSupportedEnhancedPage(8)` 与 `IsEnhancedSourcePage(8)` 门，并让 `HandleRelease` 对源 8 路由到 `TakeGroundItem`。
- **BUE 可安全增强装备槽/热键槽 0/1 源拿起**（release 用 `NativeDragActions.SendDragItem(source,target)` → `sendDragItem(0/1,...,grid,...)`）。阻塞项：放宽 `IsSupportedEnhancedPage(0/1)` 与 `IsEnhancedSourcePage(0/1)` 门，`HandleRelease` 对源 0/1 走 `SendDragItem`；并保持装备源不可旋转（禁止自动旋转/R）。
- 两类源在原生 vanilla 里都是合法操作，目标网格 delegate 均会触发，BUE 失败时原生 handler 兜底不丢功能；因此增强不引入 vanilla 回归风险。

## 9. 仍需真实客户端验证（不得以编译替代）

| 验证编号 | 场景 | 必须观察的证据 |
|---|---|---|
| `VO-16F-01` | 地面拿起 → 拖入包含空格/占用的网格 | `TakeGroundItem` 命中、`ItemManager.takeItem` 走通、预览红/绿随源不在网格而正确（无勾除源占位） |
| `VO-16F-02` | 地面物品拖拽期间被他人拾取/销毁 | `interactableItem` 非空判断、服务端拒绝后不残留视觉、原生兜底不炸 |
| `VO-16F-03` | Primary(0)/Secondary(1) → 拖入网格 | `sendDragItem(0/1→grid)`、服务端 `sendSlot` 卸下当前装备、HUD 热键同步 |
| `VO-16F-04` | 装备源 + R 旋转 | 确认是否遵守原生「装备槽物品不可旋转」约束、提交 rot 正确 |
| `VO-16F-05` | 双页/重建时 AREA 与装备源拖放 | wrapper 幂等 attach/detach、兜底委托恢复、Preview 收起 |

## 10. 关键引用行速查

| 事实 | 文件:行 |
|---|---|
| 页面常量 SLOTS=2/PAGES=9/…/AREA=8 | `PlayerInventory.cs:64-79` |
| 装备槽不可旋转 & `canEquipInPage` | `PlayerInventory.cs:424-432` |
| `sendDragItem` | `PlayerInventory.cs:967-970` |
| `ReceiveDragItem`（0/1 源合法；源>=8 拒绝；卸下装备 `sendSlot`） | `PlayerInventory.cs:699-795`（`722`、`783-794`） |
| slots `SleekSlot[SLOTS]`，装备槽 `onPlacedItem=onPlacedItem` | `PlayerDashboardInventoryUI.cs:2627-2635` |
| 网格 `SleekItems[PAGES-SLOTS]`，`onPlacedItem=onPlacedItem` | `PlayerDashboardInventoryUI.cs:2800-2808` |
| AREA 源进店 `jar.interactableItem` 绑定 | `PlayerDashboardInventoryUI.cs:2461-2463` |
| `onGrabbedItem`（`dragFromPage=page`，AREA 特殊键分支） | `PlayerDashboardInventoryUI.cs:1084-1151`（`1095-1110`、`1118`） |
| `onPlacedItem` 源 AREA 分支 `ItemManager.takeItem` | `PlayerDashboardInventoryUI.cs:1227-1246`（`:1240`） |
| `onPlacedItem` 通用分支 `sendDragItem`（0/1 源入口） | `PlayerDashboardInventoryUI.cs:1248-1254` |
| `checkEquip`/`checkSlot`（装备交换） | `PlayerDashboardInventoryUI.cs:918-1032` |
| 网格 `grid.OnClicked` → `onPlacedItem(page,x,y)` 不依赖源 | `SleekItems.cs:219-227,247` |
| `IsSupportedEnhancedPage`=2-7；`SupportedLiveSurfacePages`={2..7} | `ItemInteractionUiComponent.cs:221,287-293` |
| `OnDragStarted` pass-through 门（源非网格→EndDrag） | `ItemInteractionUiComponent.cs:573-601`（`:582,586`） |
| `HandleRelease` 源/目标门（源 8/0/1 被拒） | `NativeInventoryInteractionAdapter.cs:55-113`（`:66,72`）；`IsEnhancedSourcePage/IsOrdinaryGrid :118-129` |
| 网格包装门（仅页 2-7） | `InventoryDragPreviewAdapter.cs:193-224,279-282` |
| `EvaluatePlacement` 目标页 `isOrdinaryGrid`（不查源页） | `InventoryDragPreviewAdapter.cs:781-836`（`:789`） |
| `ReadDragSource` | `InventoryDragPreviewAdapter.cs:869-876` |
| `NativeDragActions.StopDrag/SendDragItem/TakeGroundItem` | `InventoryDragPreviewAdapter.cs:956-974` |