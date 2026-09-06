# Unturned 原生物品栏与容器网格的真实渲染、输入和可注入边界（只读取证调研报告）

> 调研日期：2026-09-01
> 调研方式：只读源码取证（U3-SDK 官方开源源码）+ BUE 现状只读对照
> 前置材料：`./2026-08-30-native-grid-rendering-boundaries-source-notes.md`（早前素材交接包；本报告对其每条将被引用的关键结论回到源码重新取证，凡未复核的素材内容不直接引用）
> 约束：本报告为只读调研产物，未修改 U3-SDK 与 BUE 的任何生产代码/配置/测试，未构建、未生成 DLL。
> 关键词澄清（用户问题中"Glazier.Root"）：源码确认 `Root` 是 **`IGlazier` 的实例属性**（`SDG.Glazier\Glazier.cs` L43 `SleekWindow Root { get; set; }`），**不是**静态 `Glazier.Root`；当前根窗口由 `LoadingUI.Update` 每帧仲裁（`LoadingUI.cs` L923-980），玩家窗口经 `PlayerUI.Player_OnGUI()` → `Glazier.Get().Root = window`（`PlayerUI.cs` L1284-1290）。
> 结论先行：**能**。原生层本身就用 `PlayerUI.container` 作为注入点（`dragItem`、`selectionFrame` 都直接 AddChild 到它上面），加上公开的 `Glazier.Get()` 工厂、公开的 `ISleekElement.AddChild`、公开的 `SleekItems.onPlacedItem/onGrabbedItem/onSelectedItem` 委托、公开的 `updateDraggedItem()/stopDrag()`、以及只读反射私有拖拽状态，绿色/红色占据框和浮动物品图标在原生渲染层上完全可行 —— BUE 当前实现已经以生产级代码走通此路径，本报告逐项核对了它的每个触点。

---

## 0. 元信息与结论速览（TL;DR）

| 问题 | 结论 | 依据 |
|---|---|---|
| 网格真实层级 | `SleekItems`(SleekWrapper) → `horizontalScrollView`(ISleekScrollView) → `grid`(ISleekSprite) → `itemsPanel`(ISleekElement, 由 `CreateFrame()` 创建) → `SleekItem`（每格 50px，`jar.x*50/jar.y*50`） | `Sleek\SleekItems.cs` L229-255、L196-207；SDK 无 `ISleekContainer`/`ISleekFrame`（`Glazier.cs` L16） |
| 全屏根 | `PlayerUI.window`(SleekWindow) → `PlayerUI.container`(public static ISleekElement)，任何 UI 的顶级父级 | `Player\PlayerUI.cs` L18-19、L2235-2262 |
| G/F/载具打开链路 | 见 §2 | `PlayerUI.cs` L1859-1944、`PlayerInventory.cs` L1120-1164、L1571-1607 |
| 拖拽跟手 | `PlayerUI.Update`(L2173) → `updateDraggedItem()`(L2418) → `refreshDraggedVisualPosition`(L2408)，`dragItem` 直接挂 `PlayerUI.container` | `PlayerUI.cs` L2181、`PlayerDashboardInventoryUI.cs` L3099-3100 |
| 放置命中 | `grid.GetNormalizedCursorPosition()`（grid-local 0..1）→ `onPlacedItem(page,x,y)` | `SleekItems.cs` L219-227 |
| 图标刷新 | `ItemTool.getIcon` 异步回调 → `internalImage.Texture` | `SleekItem.cs` L156、`SleekItemIcon.cs` L21-43、L91-97 |
| 可行注入 seam | ① `Glazier.Get().CreateXxx()` + `AddChild` 到 `PlayerUI.container`（仿原生 `dragItem`）；② 只读反射私有拖拽状态；③ 公开委托重绑 `onPlacedItem`；④ Harmony postfix 驱动；⑤ 公开 `Items/PlayerInventory` API 做占据判定 | §5 |
| 不可行/高风险 | ① Harmony IL patch 大方法体（真机 IL Compile Error）；② 注入非 Glazier 的 ISleekElement（后端拒绝）；③ 直接改私有 `Items.slots`；④ 挂 `itemsPanel` 内依赖 `clear()` 不被清；⑤ 写私有拖拽字段 | §6 |
| BUE 现状 | 用 Glazier 原生元素 AddChild 进 `itemsPanel`（框）和 `PlayerUI.container`（图标），Harmony postfix + 反射 + 委托重绑；无 IMGUI | §7 |

---

## 1. 对象、范围与方法

- **对象**：Unturned 客户端本地玩家的（1）按 G 打开的背包面板（六页：手中/背包/背心/上衣/裤子 + 储物/后备箱 + 附近地面）、（2）按 F 打开的普通容器、（3）载具上按 G 打开的车辆后备箱；以及承载它们的 `PlayerUI`、`PlayerDashboardUI`、`PlayerDashboardInventoryUI`、`SleekItems`、`itemsPanel`、`grid`、`scroll`、`Glazier`。
- **范围**：渲染层级（parentage）、输入（鼠标命中/拖拽）、更新时序、重建机制、坐标系，以及插件（Harmony/反射/AddChild/公开委托）可注入的边界。
- **方法**：直接读取 U3-SDK 源码并逐条给出行号；对 BUE 实现逐文件逐成员核对；对"源码确认"与"需真机验证"分开标注（见 §8）。
- **基路径约定**：以下 `A-C\` = `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\`；`SDG.Glazier\` = `D:\Agent-工作目录\U3-SDK\Assets\Runtime\SDG.Glazier\`；`Glazier_uGUI\` = `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Glazier_uGUI\`。

---

## 2. 完整调用链

### 2.1 三个入口的打开链路

**按键语义先行澄清（本 SDK 源码确认）**：`ControlsSettings.cs` 默认绑定 = `DASHBOARD=Tab`（L417）、`INVENTORY=G`（L418）、`INTERACT=F`（L407）、`ROTATE=R`（L465）。即：
- **Tab** = 打开/关闭整个玩家仪表盘（dashboard，最近一次激活的页签）；
- **G** = 打开仪表盘并切到"背包页"（inventory tab）；
- **F** = 交互键，打开普通容器；
- 载具内按 G = 打开背包页，此时 STORAGE 页显示后备箱。
（用户描述"按 G 打开背包、按 F 打开容器"在默认绑定下成立，但严格说是 G=背包页签、Tab=仪表盘。）

**按 G 打开背包面板（玩家六页）**
1. `PlayerUI.tickInput()`：`InputEx.ConsumeKeyDown(ControlsSettings.inventory)`（G，`ControlsSettings.cs` L418）→ `PlayerUI.cs` L1914-1943：若 dashboard 未开且 `canOpenMenus` → `PlayerDashboardInventoryUI.active=true; ... PlayerDashboardUI.open()`。
2. `PlayerDashboardUI.open()`（`UI\Player\PlayerDashboardUI.cs` L19-54）：`container.AnimateIntoView()`；若 inventory 页 active 则 `PlayerDashboardInventoryUI.open()`。
3. `PlayerDashboardInventoryUI.open()`（`UI\Player\PlayerDashboardInventoryUI.cs` L131-179）：`active=true`、手势 `INVENTORY_START`、`updateVehicle()`、`resetNearbyDrops()`、`updateHotkeys()`、重建 `characterPlayer`、`container.AnimateIntoView()`（L178）。
4. 页面数据装配：`resetNearbyDrops()`（L1771-1786）`areaItems.clear(); areaItems.resize(8,3); replaceItems(AREA,…)`；`updateHotkeys()`（L1935-1968）。

**按 F 打开普通容器（barricade/storage crate）**
1. `PlayerInteract.cs` L486/L511：F（`INTERACT`）→ `interactable.use()`。
2. `Interactable\InteractableStorage.cs` L455-458：`use()` → `ClientInteract(...)`。
3. 服务端授权后 `Player.LocalPlayer.inventory.openStorage(storage)`（`Player\PlayerInventory.cs` L1571-1588）→ `updateItems(STORAGE, storage.items)`（L1586）→ `sendStorage()`（L1587）。
4. 客户端 `ReceiveStoraging`（`PlayerInventory.cs` L1120-1164）：`items[STORAGE].resize/addItem`、`isStoring=true`、`onInventoryStored?.Invoke()`（L1162）。
5. `PlayerDashboardInventoryUI.onInventoryStored()`（`PlayerDashboardInventoryUI.cs` L2101-2131）：`shouldStorageOpenDashboard` → 开 dashboard（L2108-2124）。

**载具上按 G 打开车辆后备箱**
1. 按 G（同上 2.1 入口 1）；车内时 `updateVehicle()`（`PlayerDashboardInventoryUI.cs` L1523-1769）显示 `vehicleBox`，并把 STORAGE 页头改为 `"Storage_Trunk"`（L1757-1758）。
2. 数据链路（**后备箱权限在坐上驾驶座时就由服务端授予**，与按 G 无关）：`InteractableVehicle.addPlayer`（`InteractableVehicle.cs` L2249，`grantTrunkAccess` L2279-2282）→ `grantTrunkAccess`（L2181-2187）→ `PlayerInventory.openTrunk(trunkItems)`（L1593-1607）→ `updateItems(STORAGE, trunkItems)`（L1605）→ `sendStorage` → 客户端 `ReceiveStoraging`（`isStorageTrunk=true`）；离座/换座时 `revokeTrunkAccess`（L2189-2195）→ `closeTrunk`（`PlayerInventory.cs` L1612-1618）。
3. **注意（源码确认）**：`shouldStorageOpenDashboard => !isStorageTrunk`（`PlayerInventory.cs` L138），即后备箱**不会**像普通容器那样经由 `onInventoryStored` 自动打开 dashboard —— 因为玩家已经在车内按 G 打开着背包页，后备箱数据只是填充进已打开的 STORAGE 页。
4. **关键统一性**：普通容器与车辆后备箱**共用同一个 STORAGE 页网格**（`items[PlayerInventory.STORAGE - PlayerInventory.SLOTS] = items[5]`），仅头部文字/vehicleBox 面板不同；区分靠 `PlayerInventory.isStorageTrunk`（L124-125）。后备箱 `Items` 由 `VehicleManager` 以页 STORAGE 创建（`VehicleManager.cs` L2212-2215），尺寸来自 `VehicleAsset.trunkStorage_X/Y`（`VehicleAsset.cs` L1645-1651、L2431-2432）。

### 2.2 页面构建链（构造时序）

1. `PlayerUI.InitializePlayer()`（`PlayerUI.cs` L2228+）：`window = new SleekWindow()`（L2235）；`Glazier.Get().Root = window`（`Player_OnGUI`，L1284-1290）；`container = Glazier.Get().CreateFrame()`（L2259-2262）→ `window.AddChild(container)`；`dashboardUI = new PlayerDashboardUI()`（L2275）。
2. `PlayerDashboardUI` 构造（L187-292）：`container = new SleekFullscreenBox()`（L192）→ **`PlayerUI.container.AddChild(container)`**（L200）；四个页签按钮（L203-275）；`new PlayerDashboardInventoryUI()`（L288）。
3. `PlayerDashboardInventoryUI` 构造（L2544-3130）：
   - `container = new SleekFullscreenBox()`（L2556-2564）→ **`PlayerUI.container.AddChild(container)`**（L2564）；
   - `backdropBox`（L2567-2573）；`characterBox/characterImage`（L2594-2625）；
   - `slots = new SleekSlot[SLOTS]`（L2627-2635，槽位页 0/1），接线 `onSelectedItem/onGrabbedItem/onPlacedItem`；
   - `box`（L2689-2696）→ `clothingBox`(CreateScrollView, L2698-2703) + `areaBox`(CreateScrollView, L2705-2713)；
   - `headers = new ISleekButton[PAGES-SLOTS+3]`（L2739-2750），`clothingBox.AddChild(headers[i])`；
   - **`items = new SleekItems[PlayerInventory.PAGES - PlayerInventory.SLOTS]`（=7）**（L2800-2808），每个 `new SleekItems(SLOTS+index)`（页 2..8），接线 `onSelectedItem/onGrabbedItem/onPlacedItem`，`clothingBox.AddChild(items[index])`；
   - `areaItems = new Items(AREA)`（L2810）；
   - `selectionFrame`（L2820-2824）→ **`PlayerUI.container.AddChild(selectionFrame)`**；
   - `vehicleBox`（L2988-2990，初挂 `clothingBox`）；旋转按钮挂 STORAGE 头（L3070-3097）；
   - **`dragItem = new SleekItem(); PlayerUI.container.AddChild(dragItem);`**（L3099-3100）；
   - 事件订阅：inventory onInventoryResized/Updated/Added/Removed/Stored、equipment onHotkeysUpdated、ItemManager onItemDropAdded/Removed、movement onSeated、clothing onShirt/Pants/Hat/Backpack/Vest/Mask/GlassesUpdated（L3111-3129）。

### 2.3 SleekItems 内部层级构建（网格渲染本尊）

`Sleek\SleekItems.cs` 构造（L229-255）：
```
SleekItems(SleekWrapper)
 └─ horizontalScrollView = Glazier.CreateScrollView()      L237-241  AddChild
     └─ grid = Glazier.CreateSprite()  (Grid_Sprite)        L243-248  grid.OnClicked += onClickedGrid
         └─ itemsPanel = Glazier.CreateFrame()              L251-254  grid.AddChild(itemsPanel)
             └─ SleekItem（每个物品一个，PositionOffset = jar.x*50 / jar.y*50）  L196-207
```
每个 `SleekItem`（`Sleek\SleekItem.cs` L241-285）：
```
SleekItem(SleekWrapper)
 ├─ button = Glazier.CreateButton()（OnClicked→onDraggedItem, OnRightClicked→onClickedItem） L243-252
 ├─ icon = new SleekItemIcon()（isAngled=true）             L254-256
 ├─ amountLabel / qualityImage / hotkeyLabel               L258-282
 └─ updateItem(jar)（首次装配尺寸/图标/数量/稀有度/品质）    L284
```

### 2.4 每帧更新链（拖拽/附近物品）

1. `PlayerUI.Update()`（MonoBehaviour Update，`PlayerUI.cs` L2173）：
   - `PlayerDashboardInventoryUI.updateDraggedItem()`（L2181，见下）；
   - `PlayerDashboardInventoryUI.updateNearbyDrops()`（L2182，每帧至多 20 个附近掉落，L2479-2535，含视线遮挡检查 L2516-2526）；
   - `tickInput()`（L2212，G/F/Esc 等按键）。
2. `updateDraggedItem()`（`PlayerDashboardInventoryUI.cs` L2418-2438）：仅 `active && PlayerDashboardUI.active && isDragging` 时执行；按 R（`ControlsSettings.rotate`，`ControlsSettings.cs` L465）→ `dragJar.rot++ %=4`、`updatePivot()`、`dragItem.updateItem(dragJar)`；随后 `refreshDraggedVisualPosition()`。
3. `refreshDraggedVisualPosition()`（L2408-2416）：`dragItem.PositionOffset = dragPivot`；`dragItem.PositionScale = PlayerUI.container.ViewportToNormalizedPosition(InputEx.NormalizedMousePosition)` —— 图标跟随鼠标的官方语义。
4. 渲染阶段：`Glazier_uGUI.LateUpdate()`（`Glazier_uGUI\Glazier_uGUI.cs` L746-801）→ `rootImpl.Update()`（L773）→ 深度优先递归更新可见子元素（`GlazierElementBase_uGUI.cs` L298-307）；每个 `GlazierProxy` 先调 `owner.OnUpdate()`（`GlazierProxy_uGUI.cs` L27-37）→ `SleekItems.OnUpdate()` 每帧最多创建 5 个格子（`SleekItems.cs` L167-177）。即：游戏逻辑 Update → Glazier 元素 LateUpdate。

### 2.5 拖拽全链路（命中→抓取→跟手→放置/旋转→提交）

1. **鼠标命中**：`SleekItem.button` 左键 → `SleekItem.onDraggedItem`（`SleekItem.cs` L231-234）→ `SleekItems.onDraggedItem`（L214-217）→ `PlayerDashboardInventoryUI.onGrabbedItem`（L1084-1151）。
2. **抓取**：`onGrabbedItem`：Ctrl(其他键)时直接丢（L1093-1110）；否则 `dragJar = inventory.getItem(page, getIndex(page,x,y))`（L1112）、记 `dragSource/dragFromPage/dragFrom_x/dragFrom_y/dragFromRot`（L1116-1121）、算 `dragOffset`（L1123-1143）、`updatePivot()`（L1145）、`dragItem.updateItem(dragJar)`（L1147）、`refreshDraggedVisualPosition()`（L1148）、`startDrag()`（L1150）。
3. **拖拽开始**：`startDrag()`（L200-221）：`isDragging=true`、`setItemsEnabled(false)`（格子半透明 L246-257→`SleekItem.disable()` alpha 0.5，`SleekItem.cs` L53-65）、`dragItem.IsVisible=true`、dragOutside 遮罩可见（L213-218）。
4. **跟手/旋转**：每帧 `updateDraggedItem()`（见 2.4）；R 键旋转改 `dragJar.rot` → `updatePivot()`（L2381-2403，按 rot 计算负向像素 pivot）→ `dragItem.updateItem()`（图标换向）。
5. **放置**：网格点击 → `SleekItems.onClickedGrid()`（`SleekItems.cs` L219-227）：`grid.GetNormalizedCursorPosition()` → `x = (byte)(cursor.x*width), y=(byte)(cursor.y*height)` → `onPlacedItem?.Invoke(page,x,y)` → `PlayerDashboardInventoryUI.onPlacedItem`（L1154-1325）：先 `ConsumeEvent()`；`page>=SLOTS` 网格分支按 `dragPivot` 换算并夹取目标格（L1160-1195）；同格 `stopDrag()` 返回（L1208-1213）；AREA 页→丢（L1215-1225）；来自 AREA→捡（L1227-1246）；`checkSpaceDrag` 通过→`stopDrag()`+`inventory.sendDragItem(...)`（L1248-1263）；否则尝试 swap（L1292-1314）。
6. **右键"检查"**：本版本**没有 `onItemInspect`**。右键（`ISleekButton.OnRightClicked`，`SleekItem.cs` L236-239/L251）→ `SleekItem.onClickedItem` → `SleekItems.onSelectedItem`（L209-212）→ `PlayerDashboardInventoryUI.onSelectedItem`（L849-915，注释 "Called when right clicking on item"）：按住 `ControlsSettings.other` 时快速动作（`checkAction` L1034-1081 穿戴/装备），否则 `openSelection`（L518-823）弹 Equip/Context(枪支配件)/Drop/Store 菜单。
7. **拖拽结束**：`stopDrag()`（L223-244）：`isDragging=false`、`dragJar.rot = dragFromRot`（还原）、`setItemsEnabled(true)`、`dragItem.IsVisible=false`、遮罩隐藏。
8. **提交**：`PlayerInventory.sendDragItem(page_0,x_0,y_0,page_1,x_1,y_1,rot_1)`（`PlayerInventory.cs` L967-969）→ 服务端校验 → `ReceiveDragItem`（L701-793）→ `onInventoryAdded/Removed/Updated` → `PlayerDashboardInventoryUI.onInventoryAdded/Removed/Updated`（L2057-2099）→ `items[page].addItem/removeItem/updateItem` → `SleekItems.createElementForItem`（L196-207）重建格子。

### 2.6 图标刷新链

`SleekItem.updateItem(jar)`（`SleekItem.cs` L106-229）→ `icon.Refresh(jar.item.id, jar.item.quality, jar.item.state, asset)`（L156）→ `SleekItemIcon.Refresh`（`SleekItemIcon.cs` L21-43）→ `ItemTool.getIcon(...)` 异步请求（`ItemIconInfo`，`Tools\ItemIconInfo.cs` L9-31）→ 回调 `OnIconReady(handle, texture)`（`SleekItemIcon.cs` L91-97）设置 `internalImage.Texture`。旋转：`icon.rot = jar.rot`（L153）→ `internalImage.RotationAngle = rot*90`（`SleekItemIcon.cs` L45-52）；尺寸交换在 `SleekItem.updateItem` L126-151（rot%2==1 时 swap size_x/size_y 并对图标做偏移）。

---

## 3. 各类网格的统一点与差异

### 3.1 网格清单（页面枚举）

`Player\PlayerInventory.cs` L64-71：`SLOTS=2, PAGES=9, BACKPACK=3, VEST=4, SHIRT=5, PANTS=6, STORAGE=7, AREA=8`。

| 页 | 名称 | 网格实例 | 布局容器 | 特性 |
|---|---|---|---|---|
| 0/1 | 主/副手槽位 | `SleekSlot[2]`（L2627-2635） | `backdropBox` | 单独控件（非 SleekItems），250×150，物品居中 |
| 2 | 手中(Hands) | `items[0]`（页 2） | clothingBox/areaBox | 头 L2792 `"Hands"` |
| 3 | 背包 | `items[1]` | 同上 | 头 `onBackpackUpdated`→headers[1]（L2230-2250） |
| 4 | 背心 | `items[2]` | 同上 | 头 `onVestUpdated`→headers[2]（L2252-2272） |
| 5 | 上衣 | `items[3]` | 同上 | 头 `onShirtUpdated`→headers[3]（L2155-2175） |
| 6 | 裤子 | `items[4]` | 同上 | 头 `onPantsUpdated`→headers[4]（L2177-2200） |
| 7 | 储物/后备箱 | `items[5]` | 同上 | 头 STORAGE-SLOTS=5；车内改 "Storage_Trunk"（L1757-1758） |
| 8 | 附近地面(Area) | `items[6]` | 同上 | 动态高度（≤200）；来自 `resetNearbyDrops`/`updateNearbyDrops`（L1771-1786、L2479-2535） |

> 附注：`Sleek\SleekInventory.cs` 是**另一个控件**（经济/衣柜菜单网格：button + `SleekEconIcon` + 多个 label），与"世界内容器网格"无关，仅在菜单衣柜使用，勿混淆。

### 3.2 统一点（全部 SleekItems 网格）

- **同一控件类** `SleekItems`；同一内部层级（2.3）；同一 50px 格子（`resize` 设 `ContentSizeOffset = width*50/height*50`，`SleekItems.cs` L115-124）。
- 物品摆放统一 `PositionOffset = jar.x*50 / jar.y*50`（L199-200）；尺寸 `asset.size_x*50 / size_y*50`（`SleekItem.cs` L126-151）。
- 统一的三个公开委托 `onSelectedItem/onGrabbedItem/onPlacedItem`（`SleekItems.cs` L10-12、L16-18），全部被 `PlayerDashboardInventoryUI` 构造时接到私有静态处理器（L2804-2806）。
- 统一的拖拽状态机（§2.5），由 `PlayerUI.container` 上的 `dragItem` 幽灵图标承载视觉。
- 统一的物品→格子装配：`SleekItems.addItem(jar)` 入队 → `OnUpdate()` 每帧 ≤5 个 `createElementForItem`（L167-177）。
- 统一的可见性/重排：`updateBoxAreas()`（L1814-1932）在双栏（宽屏，`Screen.width>=1350`，L129）与单栏之间通过 **`AddChild` 重挂** 每个 `headers[i]/items[i]` 到 `clothingBox` 或 `areaBox`（L1847-1886）——注意 `ISleekElement.AddChild` 语义就是"换父"（`SDG.Glazier\Sleek.cs` L181-185 注释明说，且 dashboard 双栏模式正是官方用例）。

### 3.3 差异点

| 维度 | 普通容器(F) | 车辆后备箱(G) | 玩家背包六页 |
|---|---|---|---|
| 网格实例 | **复用 STORAGE 页** `items[5]` | **同左** `items[5]` | 各页独立实例 `items[0..4]` |
| 数据来源 | `openStorage`→`updateItems(STORAGE, storage.items)`（`PlayerInventory.cs` L1586） | `openTrunk`→`updateItems(STORAGE, trunkItems)`（L1605） | 玩家自身 `Items` 各页 |
| 区分标志 | `isStorageTrunk=false` | `isStorageTrunk=true`（L124-125） | — |
| 头部文字 | `"Storage"`（L1765） | `"Storage_Trunk"`+车辆名（L1757-1758） | 各衣物名/Hands |
| 附加面板 | — | `vehicleBox`（车名/操作/乘客，L1523-1769；`updateBoxAreas` 决定挂 areaBox 还是 clothingBox，L1823-1845） | `characterBox`（角色相机 L2594-2625） |
| 旋转按钮 | 仅 display 型 storage 显示（L1912-1929、L3070-3097） | 无 | 无 |
| 重建时机 | STORAGE 高度归 0 → `items[page].clear()`（L1806-1809） | 同左 | 高度变化 → `onInventoryResized`（L1788-1812） |
| 可滚动 | 是（clothingBox/areaBox 都是 ScrollView） | 是 | 是 |

**网格重建机制要点**：`onInventoryResized`（L1788-1812）对每页 `items[page-2].resize(newW,newH)` 并 `IsVisible = newH>0`；STORAGE 页 `newH==0` 时 `clear()`（`SleekItems.clear()` = `itemsPanel.RemoveAllChildren()`，`SleekItems.cs` L126-132）—— **这会把挂在该 itemsPanel 内的注入子元素一并清掉**（BUE 用每次 dispatch 重建来处理，见 §7）。

---

## 4. 坐标系与更新时序专节

### 4.1 坐标语义（源码确认）

- **itemsPanel/grid 局部坐标系**：左上原点，Y 向下；格子 = 50px 逻辑像素；物品位置 = `jar.x*50`。
- `grid.GetNormalizedCursorPosition()`（`SleekItems.cs` L221）：返回**相对 grid 左上**、Y 向下的归一化 0..1 光标位置；uGUI 实现 `GlazierElementBase_uGUI.cs` L356-362：`(mouse.x - rect.xMin)/width, (Screen.height - mouse.y - rect.yMin)/height`（内部完成屏幕底左 Y↑ → 网格顶左 Y↓ 翻转）。
- `PlayerUI.container.ViewportToNormalizedPosition(viewportPos)`（`SDG.Glazier\GlazierElementBase.cs` L579-580）：屏幕归一化（底左 Y↑）→ 元素局部归一化（顶左 Y↓），`dragItem` 跟随用（`PlayerDashboardInventoryUI.cs` L2413）。
- `GetAbsoluteSize()`（`Sleek.cs` L222）：元素在屏显示像素尺寸（BUE 用它求 grid 实际像素大小）。
- **双栏重排时坐标稳定**：因为 `AddChild` 换父不改变子元素局部坐标（`PositionOffset` 相对父），`updateBoxAreas` 只需改 `PositionOffset_Y` 堆叠（L1863-1864、L1880-1881）。

### 4.2 更新时序

1. **游戏逻辑 Update**（所有 MonoBehaviour `Update`，含 `PlayerUI.Update` L2173）：处理输入/拖拽状态/附近掉落；`LoadingUI.Update`（`LoadingUI.cs` L923-980）每帧仲裁 `Glazier.Get().Root`（加载中→加载窗；否则 `PlayerUI.instance.Player_OnGUI()` L968-971 把 Root 绑回玩家窗）。
2. **Glazier LateUpdate**（`Glazier_uGUI.LateUpdate` L746-801）：`rootImpl.Update()` 深度优先递归（**仅可见子元素**，`GlazierElementBase_uGUI.cs` L298-307），`SleekProxy` 先 `owner.OnUpdate()`（每帧建格等）再自身 Update。动画用 `Time.unscaledDeltaTime`（`GlazierElementBase.cs` L497-577，注释 "Game can be paused in singleplayer"）。
3. 因此：**注入元素的"每帧刷新"挂点**是 `SleekItems.OnUpdate`（原生）或插件的 Harmony postfix（`updateDraggedItem` / `PlayerUI.Update`，BUE 实证可行）；**渲染顺序**（谁盖谁）由 uGUI Canvas 内层级与兄弟顺序决定（`SupportsDepth=true`，`Glazier_uGUI.cs` L302；后 AddChild 的后绘制在上层）。
4. **可见性 gating（重要）**：元素只有在"可见且位于当前 Root 子树下"时才会收到每帧 `Update/OnUpdate`（`GlazierElementBase_uGUI.cs` L298-307）。注入到 `PlayerUI.container` 下的元素天然满足（`container` 常显），注入到被隐藏的 tab 容器下的元素在该 tab 关闭时**不 tick**。
5. **移除即销毁（重要）**：`RemoveChild/RemoveAllChildren` 会调用 `InternalDestroy()` 销毁子元素（`GlazierElementBase_uGUI.cs` L253-296），原生元素是池化的（`Glazier_uGUI.cs` L361-388）—— 持有已移除的原生元素引用是不安全的（池复用）。BUE 用 `Unmount/RemoveChild` 后丢弃引用，符合此语义。

### 4.3 重建与生命周期

- 打开/关闭：`SleekFullscreenBox.AnimateIntoView/OutOfView`（`Sleek\SleekFullscreenOverlay.cs` L15-42），隐藏用 `IsVisible=false`（不销毁元素）。
- 页面重排/换父：`updateBoxAreas`（AddChild 重挂）。
- 物品增删：`SleekItems.addItem/removeItem/updateItem` 增量更新；`clear()` 全清。
- 整面板销毁：静态类，`PlayerUI` 重建时 `window.InternalDestroy()`（`PlayerUI.cs` L2470-2472）；BUE 用"每次 dispatch 重建"来跟随。

---

## 5. 可行注入 seam 清单

按风险从低到高排列（均经源码确认；标注"实证"= BUE 已跑通或原生自身在用）。

### seam-1（推荐，实证）：`Glazier.Get().CreateXxx()` + `AddChild` 挂 `PlayerUI.container`
- 依据：`PlayerUI.container` 是 **public static ISleekElement**（`PlayerUI.cs` L19）；`Glazier.Get()` 与 `IGlazier` 全公开（`SDG.Glazier\Glazier.cs` L12-88）；`ISleekElement.AddChild` 公开（`Sleek.cs` L185）；`SleekWrapper.AddChild` 公开（`SleekWrapper.cs` L316）。
- 原生自身就在用这个注入点：`dragItem = new SleekItem(); PlayerUI.container.AddChild(dragItem);`（`PlayerDashboardInventoryUI.cs` L3099-3100）；`selectionFrame`（L2820-2824）；`PlayerDashboardUI.container`/`PlayerDashboardInventoryUI.container`（L200/L2564）。
- 用法：浮动物品图标 = `new SleekItemIcon()`（公开类，内部 `Glazier.CreateImage()`，`SleekItemIcon.cs` L83-89）或 `Glazier.Get().CreateImage()`；`PositionScale = PlayerUI.container.ViewportToNormalizedPosition(InputEx.NormalizedMousePosition)`、`PositionOffset = dragPivot`（完全复刻 `refreshDraggedVisualPosition` L2408-2416）。
- 优势：绘制在面板之上，不受 `setItemsEnabled(disable)` 变暗影响、不被 dragOutside 遮罩挡点击、不随网格重建销毁。
- **这是本报告建议的"浮动物品图标"与"绿色/红色占据框"首选挂载层**（框也可挂这里：把框按 `PlayerUI.container` 局部坐标折算）。

### seam-2（实证，BUE 当前用法）：只读反射私有拖拽状态
- 私有字段：`PlayerDashboardInventoryUI` L39-48 `dragJar/dragSource/dragItem/dragOffset/dragPivot/dragFromPage/dragFrom_x/dragFrom_y/dragFromRot`；L63 `SleekItems[] items`。`SleekItems` L20-22 `itemsPanel/grid/horizontalScrollView`。
- 只读 `GetValue` 完全安全；写这些字段（尤其 `dragJar.rot`、`dragPivot`）与原生 `updateDraggedItem` 冲突，**不要写**（§6）。
- 实证：BUE `InventoryDragPreviewAdapter.cs` L62-68 `AccessTools.Field` 缓存 7 个字段，L789-843 只读。

### seam-3（实证）：公开委托重绑 `SleekItems.onPlacedItem`（及 onGrabbedItem/onSelectedItem）
- `SleekItems.cs` L16-18 是公开字段；重绑前保存原生 handler，决定"接管 or 透传"（BUE `InventoryDragPreviewAdapter.cs` L183-184 存 `NativeHandler`、L306-316 wrapper 先判定后转发）。
- 原生在构造时把三个委托接到私有静态处理器（`PlayerDashboardInventoryUI.cs` L2804-2806），重绑即可劫持放置/抓取/选择；不 Harmony、不改 IL。
- 注意：重绑需在每次 dispatch（面板重建）后重新执行（BUE `AttachGrid` L162-193）。

### seam-4（实证）：Harmony postfix 驱动每帧
- 目标：`PlayerDashboardInventoryUI.updateDraggedItem()`（**public static**，L2418）或 `PlayerUI.Update`（private instance，L2173）。BUE 两者都 postfix（`InventoryDragPreviewAdapter.cs` L109-110、`InventorySurfaceLifecycleAdapter.cs` L859-860）。
- 用于：拖拽期间每帧计算候选格、更新预览框/图标位置；关闭/无拖拽时也可作为通用心跳。

### seam-5（推荐）：公开 `Items`/`PlayerInventory` API 做占据判定（不碰渲染层）
- `Items.checkSpaceEmpty`（L441-472）/`checkSpaceDrag`（L475-525）/`checkSpaceSwap`（L530-575）/`tryFindSpace`（L577-659）全公开；`Items.items` 公开列表（L42-46）、`ItemJar.x/y/rot/size_x/size_y` 公开（`ItemJar.cs` L9-14）。
- 插件可从 `Player.LocalPlayer.inventory.items[page]` 构建自己的占据快照（BUE `NativeItemGridOccupancySnapshot.cs` 正是如此，只读公开 API），从而算绿/红框而不动渲染层。
- 变更订阅：`onInventoryAdded/Removed/Updated/Stored`（`PlayerInventory.cs` L140-145）公开委托。

### seam-6（存在但更重）：独立插件 Canvas（官方容忍）
- 游戏明确为插件 UI 预留排序层：`CanvasSortOrders.cs` L37-47、`SleekWindow.hackSortOrder`（`SDG.Glazier\SleekWindow.cs` L55-59）。若不想进 Glazier 树，可建自己的 ScreenSpaceOverlay Canvas 叠加，但会丢失与网格的自动滚动/夹取/换父联动。

### seam-7（服务端，非渲染）：容器打开前插件 veto
- `InteractableStorage.ReceiveInteractRequest`（`InteractableStorage.cs` L541-630）在开箱前触发插件委托 `BarricadeManager.onOpenStorageRequested`（L597-606；声明 `BarricadeManager.cs` L28/L69，调用点 L1012）—— 服务端插件可拒绝开箱（影响"容器是否打开"），但**不能**直接向网格注入视觉元素。这与本文档的"渲染层注入"话题不同，仅作完整边界记录。

### 注入方式前提（重要约束，`Glazier_uGUI` 实证）
- **必须用 `Glazier.Get().CreateXxx()` 或继承 `SleekWrapper`（其构造自动拿到 proxy，`SleekWrapper.cs` L436-441）**。uGUI 后端会拒绝外来 `ISleekElement` 实现："cannot add non-uGUI element"（`GlazierElementBase_uGUI.cs` L343-346）。**不可** new 一个自定义 `ISleekElement` 对象 AddChild 进去。
- `CreateFrame()` 返回的只是**普通 `ISleekElement`**（`Glazier.cs` L16）——SDK 里**不存在 `ISleekContainer`/`ISleekFrame`**（全树搜索零命中）；最接近"容器"的是 `ISleekElement` frame 与 `SleekWrapper` 子类。
- **移除即销毁 + 池化**：`RemoveChild/RemoveAllChildren` 调用 `InternalDestroy()`（`GlazierElementBase_uGUI.cs` L253-296），原生元素进池复用（`Glazier_uGUI.cs` L361-388）——已移除引用不可再用。
- **移除/替换父级安全**：`AddChild` 可换父（`Sleek.cs` L181-185 注释 + 实现 `GlazierElementBase_uGUI.cs` L331-335），这正是 `updateBoxAreas` 双栏重挂的原理。
- **独立插件 Canvas 是被官方容纳的**：游戏显式预留排序层 —— `CanvasSortOrders.cs` L37-47 注释 "Plugins were spawning canvases with high sort orders that showed over the loading screen"（LoadingScreen=29000、Cursor=30000、Glazier=15）；`SleekWindow.hackSortOrder`（`SleekWindow.cs` L55-59）"Workaround to hide plugin UIs spawned while player is loading"；`GlazierBase.ShouldGameProcessKeyDown` 注释明说 "plugins can create uGUI text fields"、"plugins might not be using TMP"（`GlazierBase.cs` L17-32）。即：不进 Glazier 树的独立 Canvas 也是官方承认的注入路径。

---

## 6. 不可行或高风险的注入方式

| # | 方式 | 结论 | 依据 |
|---|---|---|---|
| 1 | Harmony IL patch `PlayerDashboardInventoryUI.onPlacedItem` 大方法体（L1154-1325） | **不可行（真机实证）**：170 行方法体导致 Harmony IL 重编译失败 | BUE 注释（`InventoryDragPreviewAdapter.cs` L103-108：*"its 170-line method body fails Harmony's IL recompile on this game build"*）；交接包 §三 同 |
| 2 | Harmony IL patch `PlayerUI.Update`（L2173） | **高风险/需真机复核**：交接包 §三 记"IL Compile Error 实证"，但当前 BUE 源码确实 postfix 了它（`InventorySurfaceLifecycleAdapter.cs` L859-860）且为生产路径 —— 结论：postfix 可用，transpiler/prefix 高风险；需在目标客户端版本复核 | 交接包 vs 当前源码差异见 §8 |
| 3 | new 自定义类实现 `ISleekElement` 再 AddChild 进原生树 | **不可行**：uGUI 后端拒绝外来实现 | `GlazierElementBase_uGUI.cs` L343-346 |
| 4 | 写私有字段（`dragJar.rot`、`dragPivot`、`Items.slots` 等） | **高风险**：与原生 `updateDraggedItem`/`Items.checkSpace*` 状态冲突；`Items.slots`（`Items.cs` L41）是占据判定唯一真源，写入会破坏 `checkSpaceEmpty/Drag/Swap` 正确性 | `Items.cs` L41、L441-575 |
| 5 | 把预览框/图标作为 `itemsPanel` 子元素并依赖其长期存活 | **高风险**：`SleekItems.clear()` = `itemsPanel.RemoveAllChildren()`（`SleekItems.cs` L126-132），STORAGE 关箱（`PlayerDashboardInventoryUI.cs` L1806-1809）、AREA 重置（L1782）都会清掉；且 `setItemsEnabled(false)` 会把它连同物品一起变暗（L246-257→`SleekItem.disable()` alpha 0.5），`dragOutside` 遮罩会挡其点击（L213-218）。BUE 当前即此风险点（§7 差异 1） | 见左 |
| 6 | 复用/隐藏原生 `dragItem` 幽灵图标 | **高风险**：原生每帧 `refreshDraggedVisualPosition`（L2408-2416）会覆盖其位置；隐藏它破坏原生拖拽视觉 | `PlayerDashboardInventoryUI.cs` L2408-2416 |
| 7 | 依赖 IMGUI 后端做层级叠加 | **不可行/别用**：IMGUI `SupportsDepth=false`，无深度、按绘制顺序命中；游戏默认 uGUI（`GlazierFactory.cs` L68），IMGUI 仅编辑器/`-Glazier IMGUI` 调试用 | `Glazier_IMGUI\GlazierSprite_IMGUI.cs` L71-91（OnGUI 绘制隐形 GUI.Button 做点击，SleekItems.cs L254 注释来源） |
| 8 | 在 dedicated server 用 Glazier | **不可行**：`GlazierFactory.Create` 直接 throw | `GlazierFactory.cs` L18-23 |

---

## 7. 对 BUE 当前实现的逐项差异表

BUE 路径：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\src\`。

| # | 维度 | BUE 当前实现（源码行号） | 原生层事实（行号） | 差异/风险与建议 |
|---|---|---|---|---|
| 1 | 占据框挂载层 | `InventoryPreviewVisualSink`：`frameElement = gridPanelContainer.CreateBox()`（`ClientUi\ItemInteractionUiComponent.cs` L108），`Mount()` → `gridPanelContainer.AddChild(frameElement)`（L134）；`gridPanelContainer` 包 **`SleekItems.itemsPanel`**（`Plugin\InventorySurfaceLifecycleAdapter.cs` L1188，经反射 L335 取私有 `itemsPanel`） | 原生物品也挂在 itemsPanel（`SleekItems.cs` L203/L254） | **同坐标系 ✓**（框 `PositionOffset=Candidate.X/Y*50`，L151-154，与物品 `jar.x*50` 同空间、随滚动/换父联动）。**但**：随 `clear()` 被清（§6-5）、随 `setItemsEnabled(false)` 变暗、被 dragOutside 遮罩挡点击（原生注释 L26-32 专为挡未处理拖拽点击而加）。**建议**：框改挂 `PlayerUI.container`（seam-1），用 ViewportToNormalized 折算，规避上述三风险 |
| 2 | 浮动物品图标 | `iconElement = topLevelContainer.CreateImage()`（`ItemInteractionUiComponent.cs` L109）= `new SleekItemIcon()`（`InventorySurfaceLifecycleAdapter.cs` L135）；`Mount()` → `topLevelContainer.AddChild(iconElement)`（L135）；`topLevelContainer` 包 **`PlayerUI.container`**（L1185） | 原生 `dragItem` 也挂 `PlayerUI.container`（L3099-3100）；`refreshDraggedVisualPosition` 用 `PlayerUI.container.ViewportToNormalizedPosition`（L2413） | **✓ 完全对齐原生语义**。`ShowIcon` 用 `PositionScale`=PlayerUI 归一化指针 + `PositionOffset`=原生 dragPivot（`InventoryPreviewWiring.cs` L283-312），与原生 `refreshDraggedVisualPosition` 一致。**差异点**：拖拽期间原生 `dragItem` 与 BUE 图标同时显示（BUE 不复用/不隐藏原生幽灵，只读它做地面拾取 `InventoryDragPreviewAdapter.cs` L891-899）——存在"双图标"观感，需真机确认是否可接受 |
| 3 | 驱动机制 | ① Harmony postfix `updateDraggedItem`（`InventoryDragPreviewAdapter.cs` L109-110）；② Harmony postfix `PlayerUI.Update`（`InventorySurfaceLifecycleAdapter.cs` L859-860）；③ BepInEx 插件 Update 兜底（`BetterUnturnedExperiencePlugin.cs` L222-224） | 原生 `PlayerUI.Update` L2181 调用 `updateDraggedItem`；`SleekItems.OnUpdate` L167-177 建格 | **✓ 三路并行的官方心跳**。② 与交接包"IL Compile Error"记录存在张力（§6-2），需在目标客户端版本复核 ② 是否仍稳 |
| 4 | 放置拦截 | 重绑公开委托 `sleek.onPlacedItem = GridPlacedItemWrapper`（`InventoryDragPreviewAdapter.cs` L183-184），存原生 handler 转发（L309-312） | `SleekItems.onPlacedItem` 公开字段（L16-18）；原生接私有静态 `onPlacedItem`（L2804-2806） | **✓ 正确选择**（不用 Harmony IL，规避 §6-1）；`EvaluatePlacement`（L706-761）先判定，透传或 `sendDragItem`+`stopDrag` 接管（L883-889、L752）。**注意**：每次面板重建需重新 `AttachGrid`（L162-193），BUE 已做 |
| 5 | 私有状态读取 | 只读反射 7 个拖拽字段 + `items[]` + `SleekItems` 三个私有子元素（`InventoryDragPreviewAdapter.cs` L62-68、`InventorySurfaceLifecycleAdapter.cs` L333-336） | 私有静态字段（`PlayerDashboardInventoryUI.cs` L39-48、L63）；`SleekItems.cs` L20-22 | **✓ 只读安全**；与原生写路径隔离 |
| 6 | 占据判定 | `NativeItemGridOccupancySnapshot`（`Plugin\NativeItemGridOccupancySnapshot.cs`）只读公开 `Items.items` 构建 bool 快照，支持同容器排除源 jar（`InventorySurfaceLifecycleAdapter.cs` L178-217） | `Items.slots`（私有）是原生唯一真源；`Items.items` 公开（L42-46） | **✓ 不碰私有 slots**（规避 §6-4），逻辑等价；拖拽中源格排除需与 `checkSpaceDrag` 的"允许重叠旧自身"语义对齐（`Items.cs` L475-525） |
| 7 | 页面范围 | 仅支持背包(3)与储物(7)两页（`InventoryDragPreviewAdapter.cs` L227-230、`ItemInteractionUiComponent.cs` L218、L284-287），设备/服装页、AREA 页透传 | 原生 7 个 SleekItems 网格 + 2 槽位（§3.1） | **差异**：BUE 刻意收敛到 3/7 两页（背包+储物/后备箱），其它页原生处理。若需扩展到 2(手中)/4(背心)/5(上衣)/6(裤子) 页，seam-1/3/5 同样适用（各页独立 SleekItems） |
| 8 | 生命周期跟随 | 每 dispatch 重建表面，页面重挂/换父/关箱时 Discard 重 dispatch（`InventorySurfaceLifecycleAdapter.cs` L1063-1095、`ItemInteractionUiComponent.cs` L490-539） | `updateBoxAreas` 换父、`onInventoryResized` 重建（§3.3） | **✓ 处理了重建机制**；但框挂 itemsPanel 意味着每次 `clear()` 后要重挂（seam-1 可免） |
| 9 | 关闭/隔离 | 异常→`IsolateAndDetach`：解绑委托、unhook、卸视觉（`InventoryDragPreviewAdapter.cs` L240-304、`InventorySurfaceLifecycleAdapter.cs` L923-938） | — | **✓ fail-closed**，比原生更稳 |
| 10 | 坐标读取 | `scroll.GetNormalizedCursorPosition()` + `grid.GetNormalizedCursorPosition()` + `grid.GetAbsoluteSize()`（`InventorySurfaceLifecycleAdapter.cs` L600-629），`CellPixelSize=50`（L550）；指针换算 `(pointer-origin+scroll)/(50*uiScale)`（`InventoryPreviewWiring.cs` L226-240） | 原生 `SleekItems.onClickedGrid` 用 `grid.GetNormalizedCursorPosition()`（L221-224） | **✓ 与官方语义一致**（交接包 §二 R44 结论复核成立）；需真机验证 uiScale 双倍换算是否引入偏移（见 §8） |

---

## 8. 源码确认结论 vs 待真机验证清单

### 8.1 源码确认（可直接采信）

1. 原生注入点 `PlayerUI.container` 与 `dragItem`/`selectionFrame` 挂载方式（`PlayerUI.cs` L19；`PlayerDashboardInventoryUI.cs` L2820-2824、L3099-3100）。
2. `SleekItems` 三层内部结构（scroll→grid→itemsPanel）与 50px 格子坐标（`SleekItems.cs` L229-255、L196-207）。
3. `ISleekElement.AddChild/RemoveChild/GetNormalizedCursorPosition/GetAbsoluteSize` 全部公开（`Sleek.cs` L185/204/216/222）；`Glazier.Get()`/`IGlazier` 全公开（`Glazier.cs` L12-88）。
4. uGUI 后端拒绝外来 ISleekElement、必须用工厂或 SleekWrapper（`GlazierElementBase_uGUI.cs` L343-346）。
5. `SleekItems.onPlacedItem/onGrabbedItem/onSelectedItem` 公开委托 + 原生接线（`SleekItems.cs` L16-18；`PlayerDashboardInventoryUI.cs` L2804-2806）。
6. `updateDraggedItem` 为 public static（L2418），`PlayerUI.Update` L2181 驱动 → Harmony postfix 目标成立。
7. 拖拽状态私有字段清单与 `refreshDraggedVisualPosition` 语义（L39-48、L2408-2416）。
8. 容器/后备箱共用 STORAGE 页网格；`onInventoryStored` 打开 dashboard（`PlayerInventory.cs` L1120-1164、L1571-1607；`PlayerDashboardInventoryUI.cs` L2101-2131）。
9. `Items`/`PlayerInventory` 公开占据 API（`Items.cs` L441-659；`PlayerInventory.cs` L633-679）可作为不碰渲染层的判定源。
10. BUE 机制逐项（§7）已对源码核对成立；其 seam 全部落在公开 API + 只读反射 + 委托重绑 + postfix。

### 8.2 需真机/Unity 编辑器验证（源码无法单独证实）

1. **`PlayerUI.Update` Harmony postfix 在当前目标客户端版本是否稳定**（交接包记 IL Compile Error，当前 BUE 生产代码在用 —— 版本相关，须实测）。
2. `GetNormalizedCursorPosition()` 在真实客户端上的实际返回值分布（网格内/外、Y 语义、uiScale 影响）——BUE `InventoryPreviewWiring.cs` L226-240 的 `(pointer−origin+scroll)/(50×uiScale)` 公式与原生 `onClickedGrid` 的 `cursor*width` 在缩放 ≠1 时是否一致（原生 `onClickedGrid` 直接乘网格宽高，未除 uiScale）。
3. 双图标观感：原生 `dragItem` 幽灵 + BUE 浮动物品图标同帧并存是否可接受（§7-2）。
4. 框挂 `itemsPanel` 的实际遮挡/变暗/点击表现（dragOutside 遮罩、disable alpha 0.5）——建议真机验证后迁移到 seam-1。
5. 背心/上衣/裤子/手中 页扩展（BUE 当前只做 3/7）在真实客户端的行为（这些页的 SleekItems 与 3/7 完全相同，理论上直接可用 seam）。
6. Unity 编辑器 play（`GameStartup.unity`）作为中间验证档（交接包 §六 建议）。

---

## 9. 附录：本次取证涉及的关键源码文件清单

U3-SDK（`D:\Agent-工作目录\U3-SDK`）：
- `Assets\Runtime\Assembly-CSharp\Unturned\Sleek\SleekItems.cs`、`SleekItem.cs`、`SleekItemIcon.cs`、`SleekSlot.cs`、`SleekInventory.cs`、`SleekFullscreenOverlay.cs`
- `Assets\Runtime\Assembly-CSharp\Unturned\UI\Player\PlayerDashboardInventoryUI.cs`、`PlayerDashboardUI.cs`
- `Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerUI.cs`、`PlayerInventory.cs`
- `Assets\Runtime\Assembly-CSharp\Unturned\Inventory\Items.cs`、`ItemJar.cs`
- `Assets\Runtime\Assembly-CSharp\Unturned\Tools\ItemIconInfo.cs`
- `Assets\Runtime\Assembly-CSharp\Unturned\Settings\ControlsSettings.cs`、`GraphicsSettings.cs`
- `Assets\Runtime\Assembly-CSharp\Unturned\Managers\ItemManager.cs`
- `Assets\Runtime\Assembly-CSharp\Unturned\Interactable\InteractableStorage.cs`、`InteractableVehicle.cs`
- `Assets\Runtime\SDG.Glazier\Glazier.cs`、`Sleek.cs`、`SleekWrapper.cs`、`SleekWindow.cs`、`SleekBox.cs`、`SleekImageTexture.cs`、`SleekScrollBox.cs`、`SleekSprite.cs`、`SleekButton.cs`、`SleekColor.cs`、`GlazierElementBase.cs`
- `Assets\Runtime\Assembly-CSharp\Glazier\GlazierFactory.cs`
- `Assets\Runtime\Assembly-CSharp\Glazier_uGUI\Glazier_uGUI.cs`、`GlazierElementBase_uGUI.cs`
- `Assets\Runtime\Assembly-CSharp\Glazier_IMGUI\GlazierSprite_IMGUI.cs`

BUE（`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\src\`）：
- `BetterUnturnedExperience.Plugin\InventoryDragPreviewAdapter.cs`、`InventorySurfaceLifecycleAdapter.cs`、`NativeItemGridOccupancySnapshot.cs`、`InventoryProjectionSink.cs`、`BetterUnturnedExperiencePlugin.cs`、`ClientUiCompositionRoot.cs`、`BueRuntimePump.cs`
- `BetterUnturnedExperience.ClientUi\ItemInteractionUiComponent.cs`、`InventoryPreviewWiring.cs`、`InventoryDragPresenter.cs`、`InventoryProjectionRelay.cs`、`NativeInventoryInteractionAdapter.cs`、`BetterItemInteractionLifecycle.cs`、`ClientUiTypes.cs`
