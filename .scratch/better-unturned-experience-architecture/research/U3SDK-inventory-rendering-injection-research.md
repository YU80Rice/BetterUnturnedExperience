# U3-SDK 原生库存 UI 渲染、输入与注入研究

> 调研日期：2026-09-01  
> 结论等级：`SOURCE_CONFIRMED` 为主；本文件不宣称真实客户端运行通过。  
> 原始来源：`D:\Agent-工作目录\U3-SDK`，HEAD `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`。  
> 研究对象：BUE Better Item Interaction 的原生 inventory surface、拖拽预览、绿色/红色占据框和浮动物品图标注入边界。

## 1. 结论摘要

U3-SDK 的原生 inventory renderer 不是“每个格子一个按钮”的树，而是：一个 `SleekItems` 持有一个可滚动的 `grid`，`grid` 下面有 `itemsPanel`，实际物品 `SleekItem` 只挂在 `itemsPanel`；空格命中由 `grid.OnClicked` 统一换算 `(x, y)` 后调用公开的 `SleekItems.onPlacedItem` delegate。

因此，兼容性最高的增强路径是：

1. 反射读取当前页面的真实 `SleekItems`、`horizontalScrollView`、`grid`、`itemsPanel`，并校验父子链；
2. 占据框作为 engine-native `ISleekBox` 挂到 `itemsPanel`，用 50 logical units/格定位，使其随原生内容滚动；
3. 浮动物品图标作为 `SleekItemIcon` 挂到 `PlayerUI.container`，用顶层归一化锚点和原生 `dragPivot` 跟随光标；
4. 只在普通网格分支包装公开 `SleekItems.onPlacedItem`，特殊装备槽、AREA、原生交换和未知状态交给原生处理；
5. 不替换 `Glazier.Root`，不把 overlay 挂到 `grid` 的 sprite 命中层，不把 `Items.items` 列表误当逐格 occupancy。

BUE 当前源码已经沿这条路线实现了生命周期轮询、反射 seam、预览 Presenter 和受控 delegate rebind。但 `UnturnedGridOccupancyView.IsOccupied()` 仍以 `index = y * width + x` 查 `Items.items`，与原生 top-left `ItemJar` 列表和 footprint 语义不一致（见第 9 节）；这属于静态差异/残余风险，不能被现有编译或纯 C# 测试解释为原生占据正确。

## 2. 固定证据来源与证据边界

| EvidenceSource | 绝对路径 | 固定身份 | 主要事实 | 证据等级 |
|---|---|---|---|---|
| `U3SRC-PDINV` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\UI\Player\PlayerDashboardInventoryUI.cs` | commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`; SHA-256 `593EDCB1AF5E19E548353BA3A2F97EA3351C1921DD348747AF99ABB95179566C` | dashboard、拖拽字段、UI 树、拖拽/旋转/放置 | `SOURCE_CONFIRMED` |
| `U3SRC-SITEMS` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Sleek\SleekItems.cs` | 同一 commit；SHA-256 `7DDD51D5250CB53F45AB02D8947F8C84620F1B9FE4CEEC312F9D8B0C39D2CC5C` | grid 命中、物品子树、公开 delegates、50px 尺寸 | `SOURCE_CONFIRMED` |
| `U3SRC-SITEM` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Sleek\SleekItem.cs` | 同一 commit | 单物品图标、左右键事件、旋转刷新 | `SOURCE_CONFIRMED` |
| `U3SRC-ICON` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Sleek\SleekItemIcon.cs` | 同一 commit | `Refresh`、异步句柄、`Clear`、旋转图标 | `SOURCE_CONFIRMED` |
| `U3SRC-ITEMS` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Inventory\Items.cs` | 同一 commit | `slots[,]`、footprint、space/drag/swap | `SOURCE_CONFIRMED` |
| `U3SRC-PINV` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInventory.cs` | 同一 commit；SHA-256 `8485CBF8D4EC75A35D43A20F4BE8D6B401A58A68017B7EBA0E111D663F890FAC` | page 常量、Storage/Trunk、事件转发 | `SOURCE_CONFIRMED` |
| `U3SRC-PUI` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerUI.cs` | 同一 commit | `Glazier.Root`、主线程 `Update`、Dashboard 输入 | `SOURCE_CONFIRMED` |
| `U3SRC-INTERACT` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInteract.cs` | 同一 commit | F 键射线、`Interactable.use()` | `SOURCE_CONFIRMED` |
| `U3SRC-STORAGE` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Interactable\InteractableStorage.cs` | 同一 commit | 普通容器客户端请求及服务端开启 | `SOURCE_CONFIRMED` |
| `U3SRC-VEHICLE` | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Interactable\InteractableVehicle.cs` | 同一 commit | 驾驶员 trunk 授权/撤销 | `SOURCE_CONFIRMED` |

`BUILD_CONFIRMED`、`RUNTIME_CONFIRMED`、`RELEASE_CONFIRMED` 不由上述静态源码自动推出。特别是 IMGUI/uGUI/UIToolkit 三个 Glazier backend 的实际尺寸、裁剪和 raycast 行为仍需在目标客户端分别验证。

## 3. 页面、快捷键与原生 UI 树

### 3.1 页面编号

`PlayerInventory.cs:64-79` 固定了 9 页：`SLOTS=2`、`PAGES=9`；页面含义为：

| Page | 常量 | 语义 | BUE 增强范围 |
|---:|---|---|---|
| 0 | — | Primary | 原生装备槽，pass-through |
| 1 | — | Secondary | 原生装备槽，pass-through |
| 2 | — | Hands | 普通 grid，但仍需按目标 spec 决定是否增强 |
| 3 | `BACKPACK` | Backpack | 普通 grid |
| 4 | `VEST` | Vest | 由服装尺寸驱动的普通 grid |
| 5 | `SHIRT` | Shirt | 由服装尺寸驱动的普通 grid |
| 6 | `PANTS` | Pants | 由服装尺寸驱动的普通 grid |
| 7 | `STORAGE` | Storage/vehicle trunk | 普通容器与后备箱共享视觉 surface |
| 8 | `AREA` | Nearby ground items | 特殊 Area 分支，不能按普通网格提交 |

`ControlsSettings.cs:393-465` 的默认绑定是 `F=INTERACT`、`Tab=DASHBOARD`、`G=INVENTORY`、`R=ROTATE`；实际运行配置可以改变按键，因此插件应读取 `ControlsSettings`，不能硬编码按键。

### 3.2 原生树

`PlayerDashboardInventoryUI.cs:2739-2808` 创建 `headers` 和长度为 7 的 `SleekItems[] items`。每个 `SleekItems` 的 page 为 `PlayerInventory.SLOTS + index`，并绑定：

```text
PlayerDashboardInventoryUI.container
├─ clothingBox / areaBox / headers / slots
└─ SleekItems(page)
   └─ horizontalScrollView
      └─ grid (Grid_Sprite, OnClicked)
         └─ itemsPanel (Frame; 必须是 grid 子节点)
            └─ SleekItem (每个物品，可能延迟创建)
```

`SleekItems.cs:229-255` 明确写明 `itemsPanel` 必须是 `grid` 的子节点，以支持 IMGUI 点击处理。`SleekItems.cs:115-124` 以 `width * 50`、`height * 50` 设置内容尺寸和网格高度；`SleekItems.cs:167-177` 每帧最多处理 5 个 `pendingItems`，所以事件到达和物品视觉子节点出现之间存在窗口。

`PlayerDashboardInventoryUI.updateBoxAreas()`（约 `1814-1932`）在屏幕宽度 `>=1350` 时把 Storage/Area 及其 header 移到 `areaBox`，其他页面留在 `clothingBox`。这是真实的 `AddChild` 父子重排，不是单纯坐标变化；任何 overlay 若挂到错误父节点都会出现裁剪、滚动或层级漂移。

## 4. 三个用户路径的完整调用链

### 4.1 场景 A：G 打开玩家库存

1. `PlayerUI.Update()`（`PlayerUI.cs:2173-2218`）在主线程先调用 `PlayerDashboardInventoryUI.updateDraggedItem()`、`updateNearbyDrops()`，再处理输入与光标可见性。
2. Dashboard 的 inventory 分支（`PlayerUI.cs:1914-1945`）在 `ControlsSettings.inventory`/G 的按键边沿调用 `PlayerDashboardInventoryUI.open()` 或 `close()`。
3. `open()`（`PlayerDashboardInventoryUI.cs:131-179`）设置 `active=true`，打开人物相机，按屏幕宽度选择 `clothingBox`/`areaBox` 布局，刷新 vehicle/nearby/hotkeys，并执行 `container.AnimateIntoView()`。
4. 原生构造时（`PlayerDashboardInventoryUI.cs:3099-3115`）创建顶层 `dragItem`、挂到 `PlayerUI.container`，并订阅 inventory resize/update/add/remove/stored 事件。
5. 左键点击 `SleekItem` 最终进入 `onGrabbedItem(page,x,y,item)`；网格空白点击不会创建新的 item button，而进入 `grid.OnClicked`。
6. `close()`（`PlayerDashboardInventoryUI.cs:181-198`）设置 `active=false`、停用人物相机、调用 `stopDrag()`、关闭 selection，再执行退场动画；BUE 必须同时卸载自己的 frame/icon 与 delegate。

### 4.2 场景 B：F 打开普通容器

1. `PlayerInteract.Update()`（`PlayerInteract.cs:170-310`）每约 0.1 秒从相机/自由视角发射 `Raycast`，将命中对象解析为 `Interactable`。
2. F 由 `ControlsSettings.interact` 驱动（`PlayerInteract.cs:409-516`）。无 cursor 且目标可用时调用 `interactable.use()`。
3. `InteractableStorage.use()`（`InteractableStorage.cs:445-458`）调用 `ClientInteract(quickGrab)`；客户端只发送请求。
4. 服务端 `ReceiveInteractRequest`（`InteractableStorage.cs:534-630`）校验死亡、距离、视线、已有 trunk 优先级及插件批准回调，普通存储最终调用 `player.inventory.openStorage(this)`。
5. `PlayerInventory.openStorage()`（`PlayerInventory.cs:1568-1588`）关闭旧存储，设置 `isStoring=true`、`isStorageTrunk=false`、保存 `storage`，把 `storage.items` 绑定为 page 7，再 `sendStorage()`。
6. `sendStorage()`（`PlayerInventory.cs:1258-1279`）在本地触发 `onInventoryResized(STORAGE,...)`、`onInventoryStored` 和逐项 `onItemAdded`；Dashboard 的 `onInventoryStored()`（`PlayerDashboardInventoryUI.cs:2101-...`）按 `shouldStorageOpenDashboard` 打开 dashboard。
7. 视觉上 BUE 仍读取 `PlayerDashboardInventoryUI.items[STORAGE-SLOTS]` 的同一 `SleekItems`；容器身份差异由 `isStorageTrunk` 和 session/container reference 表示。

### 4.3 场景 C：车辆驾驶员 G 打开后备箱

1. `InteractableVehicle.addPlayer()`（`InteractableVehicle.cs:2249-2283`）在 seat 0 加入驾驶员后调用 `grantTrunkAccess()`。
2. `grantTrunkAccess()`（`InteractableVehicle.cs:2181-2187`）服务端调用 `player.inventory.openTrunk(trunkItems)`。
3. `openTrunk()`（`PlayerInventory.cs:1590-1607`）关闭已有 storage，设置 `isStoring=true`、`isStorageTrunk=true`、`storage=null`，将 `trunkItems` 绑定到同一个 page 7，再 `sendStorage()`。
4. `PlayerDashboardInventoryUI.updateVehicle()` 把 Storage header 文本改成 `Storage_Trunk`（约 `1752-1759`）；网格类、命中和拖拽代码仍是同一 `SleekItems`。
5. 驾驶员离座时 `removePlayer()`（`InteractableVehicle.cs:2330-2371`）调用 `revokeTrunkAccess()` → `PlayerInventory.closeTrunk()`；普通存储的距离关闭逻辑不适用于 trunk（`PlayerInventory.cs:1539-1547` 明确直接返回）。

**关键差异**：普通容器以实体 `InteractableStorage` 为关闭/距离/权限对象；trunk 以 seat-0 access 和 `isStorageTrunk` 为生命周期对象。二者不能只凭“当前页面是 7”区分。

## 5. 原生输入、命中、拖拽、旋转与刷新

### 5.1 物品与空格命中

- `SleekItem` 左键按钮事件进入 `onDraggedItem`，右键进入 `onClickedItem`（`SleekItem.cs:231-239`）；`SleekItems.onDraggedItem()`（`SleekItems.cs:209-217`）把 item 的 `PositionOffset/50` 作为来源 top-left `(x,y)` 传给 `onGrabbedItem`。
- 空白网格点击进入 `SleekItems.onClickedGrid()`（`SleekItems.cs:219-227`）：读取 `grid.GetNormalizedCursorPosition()`，计算 `x=(normalized.x*width)`、`y=(normalized.y*height)`，直接调用 `onPlacedItem(page,x,y)`。
- 因此，overlay 不应添加一个覆盖整个网格的可点击控件；它会抢走或改变原生 `grid.OnClicked`/IMGUI 的命中顺序。

### 5.2 抓取状态与 dragPivot

`PlayerDashboardInventoryUI.onGrabbedItem()`（`PlayerDashboardInventoryUI.cs:1083-1151`）依次：

1. 处理 `ControlsSettings.other` 的丢弃/快速拾取特殊键；
2. 从 `inventory.getItem(page, getIndex(...))` 取 `dragJar`；
3. 保存 `dragSource`、`dragFromPage`、`dragFrom_x`、`dragFrom_y`、`dragFromRot`；
4. 用 item 自身 `GetNormalizedCursorPosition()` 和 `SizeOffset` 计算 `dragOffset`；
5. 按当前 `dragJar.rot` 调整抓取偏移；
6. 调 `updatePivot()`、`dragItem.updateItem(dragJar)`、`refreshDraggedVisualPosition()`，最后 `startDrag()`。

`updatePivot()`（`PlayerDashboardInventoryUI.cs:2381-2403`）是原生渲染 pivot：

```text
rot 0: pivot = ( offset.x,                         offset.y)
rot 1: pivot = (-(size_y*50 + offset.y),           offset.x)
rot 2: pivot = (-(size_x*50 + offset.x), -(size_y*50 + offset.y))
rot 3: pivot = ( offset.y,              -(size_x*50 + offset.x))
```

`refreshDraggedVisualPosition()`（`PlayerDashboardInventoryUI.cs:2405-2416`）把 `dragPivot` 写入 `dragItem.PositionOffset_X/Y`，再用 `PlayerUI.container.ViewportToNormalizedPosition(InputEx.NormalizedMousePosition)` 写入顶层 `PositionScale_X/Y`。

### 5.3 R 旋转与浮动物品刷新

`updateDraggedItem()`（`PlayerDashboardInventoryUI.cs:2418-2438`）在 `active && PlayerDashboardUI.active && isDragging` 时监听 `ControlsSettings.rotate`：

1. `dragJar.rot = (dragJar.rot + 1) % 4`；
2. `updatePivot()`；
3. `dragItem.updateItem(dragJar)`；
4. `PlayInventoryAudio(dragJar.GetAsset())`；
5. `refreshDraggedVisualPosition()`。

`SleekItem.updateItem()`（`SleekItem.cs:106-229`）在 item ID 变化时先 `icon.Clear()`，依据旋转交换自身宽高，把 `icon.rot=jar.rot`，再刷新图标、数量、质量和 hotkey。`SleekItemIcon.Refresh()`（`SleekItemIcon.cs:21-43`）通过 `ItemTool.getIcon()` 异步取图；回调以 `expectedHandle` 过滤陈旧结果（`SleekItemIcon.cs:91-97`）。

### 5.4 放置分支与原生提交

`onPlacedItem()`（`PlayerDashboardInventoryUI.cs:1153-1325`）包含不能丢失的分支：

- page < `SLOTS`：装备槽合法性与 equip；
- 普通网格：基于 `dragPivot/50` 修正 top-left，旋转后交换 footprint，调用 `checkSpaceDrag`；
- page == `AREA`：丢弃/地面分支；
- source == `AREA`：调用 `checkSpaceEmpty` 后 `ItemManager.takeItem`；
- 普通网格被占用：`findIndex` 找到目标 item，调用 `checkSpaceSwap`，必要时旋转被交换物，再 `sendSwapItem`；
- 合法普通移动：`sendDragItem(...)`、`stopDrag()`，装备槽还会关闭 dashboard 并打开生命界面。

成功普通移动的权威边界仍是 `PlayerInventory.sendDragItem(page_0,x_0,y_0,page_1,x_1,y_1,rot_1)`（`PlayerInventory.cs` 的公开方法，调用链见第 8 节）。BUE 不应在客户端直接改 `Items.items` 或自造 ACK。

## 6. 坐标系、滚动、裁剪与更新时序

| 坐标层 | 原生 API/字段 | 语义 | 注入要求 |
|---|---|---|---|
| 屏幕鼠标 | `InputEx.NormalizedMousePosition`、`Input.mousePosition` | 顶层输入 | 只用于顶层浮动图标锚点 |
| 顶层 Glazier | `PlayerUI.container.ViewportToNormalizedPosition` | 屏幕到 Root 的归一化锚点 | 不替换 Root，只读/挂子节点 |
| 网格内容 | `grid.GetNormalizedCursorPosition()` | 相对当前 grid/content 的比例；原生已经反映 content/scroll 关系 | frame 必须使用同一 content parent |
| 网格离散格 | `x=(normalized.x*width)`, `y=(normalized.y*height)` | 原生 `onPlacedItem` 命中格 | 不能用屏幕坐标直接代替 |
| 逻辑尺寸 | `width*50`, `height*50` | 一格 50 logical units | overlay 使用原生 50 单位，避免 double scale |
| 滚动裁剪 | `horizontalScrollView` 的 viewport/content | content 在 viewport 内移动并裁剪 | frame 跟 `itemsPanel`；icon 放 Root |
| UI backend | `GlazierFactory` 的 IMGUI/uGUI/UIToolkit | 父子、锚点、raycast、坐标实现不同 | 每个 backend 单独 runtime 验证 |

BUE 当前 adapter 选择 `GridContentLocal`：`UnturnedInventorySurfaceContext.TryGetLocalPointerPixels()`（`InventorySurfaceLifecycleAdapter.cs:466-505`）从同一 `scroll`/`grid` 层级读取 pointer，`InventoryPreviewWiring.cs:227-235` 在该模式下不再叠加 scroll。这个“只应用一次 scroll”原则是正确方向；若改回 screen/viewport 输入，必须显式加一次、且只能一次 `ScrollPixels`。

更新时序为：

```text
PlayerUI.Update (主线程)
  ├─ 原生 updateDraggedItem / updateNearbyDrops
  ├─ BUE PlayerUI.Update postfix: surface Poll
  │    ├─ 读 active/isStoring/isStorageTrunk/storage
  │    ├─ 读 dashboard items[page-SLOTS]
  │    ├─ 反射验证 scroll → grid → itemsPanel 父链
  │    └─ 新 generation 时 dispatch surface + mount overlay
  └─ BUE drag tick
       ├─ isDragging 边沿分配 DragGeneration
       ├─ 读 dragJar/source/dragPivot
       ├─ 读 grid-local pointer + scroll geometry
       ├─ Presenter evaluate candidate
       └─ 更新 frame/icon
```

原生 `SleekItems.OnUpdate()` 每帧只创建最多 5 个迟延 `SleekItem`。因而“数据事件已到达”不等于“物品视觉节点已存在”；占据算法应读 authority `Items` 数据，而不是依赖 `SleekItems.items` 是否已全部生成。

## 7. 可用注入 seam、兼容性与拒绝项

### 7.1 推荐 seam

1. **私有字段反射（受控、缓存）**：读取 `SleekItems.horizontalScrollView/grid/itemsPanel` 及 `PlayerDashboardInventoryUI.items`；每次 surface rebuild 重新检查引用和父链。
2. **`AddChild/RemoveChild`**：使用 `Glazier.Get().CreateBox()` 和 `new SleekItemIcon()` 创建 engine-native 图元，再挂到真实 native parent；所有操作留在游戏主线程。
3. **公开 `SleekItems.onPlacedItem`**：捕获原 delegate，按 session 重新包装；BUE 代码必须在 detach 时恢复原 delegate，并防止 wrapper 叠加。
4. **`PlayerUI.Update` postfix**：作为已知主线程 heartbeat 的快速路径/轮询入口；surface 缺失、重建或不兼容时进入 feature-local fail-closed。
5. **库存事件**：`PlayerInventory.onInventoryAdded/Removed/Updated/Resized` 用于观察原生投影；事件不是 BUE 自定义提交确认。

### 7.2 不推荐/不可行方式

- **替换 `Glazier.Get().Root`**：`PlayerUI.Player_OnGUI()`（`PlayerUI.cs:1284-1289`）每次 GUI 回调都会把 Root 设回原生 `window`；插件替换会与全局 UI 生命周期竞争。
- **把 frame 挂到 `PlayerUI.container`**：滚动/裁剪坐标不再和 grid 内容一致，必须自己重建 scroll mapping，极易产生漂移。
- **把浮动 icon 挂到 `itemsPanel`**：icon 会被 viewport clip，拖到网格外或换页面时不可见。
- **整段 Harmony patch `PlayerDashboardInventoryUI.onPlacedItem`**：该方法同时承担装备、AREA、原生 swap、drop 和 Dashboard 关闭；全量替换会扩大兼容面并容易漏分支。若必须介入，应在 `SleekItems.onPlacedItem` 公开 delegate seam 做受控 wrapper。
- **把 `Items.items[index]` 当逐格占据**：该列表只保存每个物品的 top-left `ItemJar`；真实格占据由私有 `bool[,] slots` 及每个 jar 的旋转 footprint 维护。
- **覆盖 grid 的 overlay 启用 raycast**：透明/半透明框不应抢占 `grid.OnClicked`；应验证 `IsRaycastTarget` backend 行为后保持非命中。
- **依赖 `ISleekSprite.IsRaycastTarget` setter**：UIToolkit 的 `GlazierSprite_UIToolkit.IsRaycastTarget` 存在未实现边界；命中应依赖原生 `OnClicked` 和真实父子关系，而非假设所有 backend 的 sprite raycast setter 可用。
- **后台线程读写 Glazier/Unity**：未见可证明安全的线程边界；所有反射、坐标读取、AddChild 和视觉更新必须回到主线程。

### 7.3 Harmony 与其他 UI patch 的兼容边界

Harmony 适合生命周期没有既有 event/interface 的位置，但不能保证与所有第三方 patch 顺序兼容。建议：

- 先探测字段/方法存在性，缺失则仅隔离 BUE feature，保留原生 inventory；
- postfix 只做观察/轮询，避免改变 `PlayerUI.Update` 的返回和原生顺序；
- delegate wrapper 必须保存 exact original delegate、幂等 attach、可逆 detach；
- 不使用全局 `PatchAll()` 去扫描未知类型，也不在服务端/U3DS 加载 UI 类型。

## 8. 渲染可行性裁定

### 8.1 绿色/红色占据框：可行，但挂载点必须是 content

证据：`SleekItems.cs:251-255` 的 `itemsPanel` 是 grid 子节点，`PlayerDashboardInventoryUI` 的 native item boxes 也在该 panel。BUE `InventoryPreviewVisualSink.ShowFrame()`（`ItemInteractionUiComponent.cs:129-140`）按 `Candidate.X/Y * CellPixelSize` 写位置，按 footprint 写宽高，并用 `PreviewFrameColor.ValidGreen/InvalidRed` 设置半透明颜色；BUE `UnturnedVisualElement.Color`（`InventorySurfaceLifecycleAdapter.cs:66-75`）映射到绿色/红色 `ISleekBox.BackgroundColor`。

该路径在静态结构上满足“随 grid 内容滚动、使用原生 50 单位、不会改变 grid 命中”。需要真实客户端确认：

- 当前 backend 的 z-order 是否总在原生 item icon 上方；
- `itemsPanel` 是否被某些重建/clear 操作移除 overlay；
- box 是否被 `grid`/scroll clip 正确裁剪；
- box 的 alpha、颜色和尺寸在不同 UI scale 下是否符合预期；
- frame 的 raycast 是否确实关闭。

### 8.2 浮动物品图标：可行，但必须独立挂到顶层

证据：原生 `dragItem` 本身在 `PlayerDashboardInventoryUI.cs:3099-3104` 挂到 `PlayerUI.container`；原生定位在 `refreshDraggedVisualPosition()` 使用顶层 anchor + `dragPivot`。BUE `UnturnedVisualContainer.CreateImage()`（`InventorySurfaceLifecycleAdapter.cs:133-135`）创建 `SleekItemIcon`，`BoundAsset` setter（`:78-115`）调用 `SleekItemIcon.Refresh` 或 `ItemTool.getIcon`，旋转 setter（`:43-61`）同步 `itemIcon.rot`。

BUE `InventoryPreviewWiring.TryGetNativeIconPlacement()`（`InventoryPreviewWiring.cs:278-306`）优先复用当前 rotation 的 native `dragPivot`；`InventoryPreviewVisualSink.ShowIcon()`（`ItemInteractionUiComponent.cs:142-158`）用顶层 scale/offset、尺寸和 rotation 更新 icon。该路径避免复制原生图标加载和异步陈旧句柄逻辑。

需要真实客户端确认：icon 的 z-order 是否覆盖原生 `dragItem`、换页/关闭时是否及时隐藏、异步 texture 是否在 asset 切换后仍只接受当前 handle，以及 IMGUI 的屏幕 Y 方向是否与 uGUI/UITK 一致。

## 9. BUE 当前实现对照与静态风险

### 9.1 已对齐部分

| BUE 文件/位置 | 当前事实 | 对照原生结论 |
|---|---|---|
| `src\BetterUnturnedExperience.Plugin\InventorySurfaceLifecycleAdapter.cs:242-245` | 反射缓存 `horizontalScrollView/grid/itemsPanel` 与 dashboard `items` | 对齐原生真实树 |
| `...InventorySurfaceLifecycleAdapter.cs:299-326` | 校验 owner、三层成员和 parent chain | 防止把 stale surface 当 live surface |
| `...InventorySurfaceLifecycleAdapter.cs:699-713` | Harmony `PlayerUI.Update` postfix | 对齐原生主线程 heartbeat |
| `...InventorySurfaceLifecycleAdapter.cs:870-951` | 读取 dashboard/storage/trunk，按 generation dispatch Backpack 或 Storage surface | 对齐三场景共享 Storage page 但区分 container kind |
| `...InventoryDragPreviewAdapter.cs:61-89` | 缓存 `dragJar/dragFrom*/dragPivot/dragItem` FieldInfo 并做 gate | 对齐原生拖拽字段 |
| `...InventoryDragPreviewAdapter.cs:147-202` | 捕获并恢复 `SleekItems.onPlacedItem` 原 delegate，幂等 attach/detach | 比整段 patch 原生 `onPlacedItem` 更窄 |
| `...InventoryDragPreviewAdapter.cs:482-557` | 每帧读 native pointer、pivot、drag source，形成 preview input | 对齐主线程/拖拽时序 |
| `...InventoryDragPreviewAdapter.cs:830-835` | `(-pivot.x/50,-pivot.y/50)` 转为 grab offset | 对齐 native pivot 语义，但仍需实机旋转校验 |
| `...ItemInteractionUiComponent.cs:85-170` | frame 挂 grid panel，icon 挂 top-level，复用 pooled native elements | 对齐双坐标/双挂载点裁定 |
| `...InventoryDragPreviewAdapter.cs:712-753` | swap guard 按每个 jar 的旋转 footprint 遍历 | 这里对齐 `Items.findIndex` 覆盖语义 |

### 9.2 仍存在的静态差异/风险

`UnturnedGridOccupancyView.IsOccupied()`（`InventorySurfaceLifecycleAdapter.cs:149-166`）目前为：

```csharp
var index = y * items.width + x;
return index < items.items.Count && items.items[index] != null;
```

这不等价于 U3-SDK `Items`：

- `Items.items`（`Items.cs:41-46`）是压缩的 `List<ItemJar>`，每项只保存 top-left `x/y`；
- `Items.findIndex()`（`Items.cs:146-180`）会按 `jar.rot` 交换 `size_x/size_y` 并判断整个 footprint；
- `Items.fillSlot()`（`Items.cs:661-680`）才维护真实 `slots[x,y]` occupancy；
- `removeItem()` 在 `RemoveAt` 前先触发 `onItemRemoved`（`Items.cs:363-377`），所以事件 `index` 也不能被当作固定网格坐标。

影响：BUE 的 placement preview 若消费该 `IGridOccupancyView`，可能把空格标为占用、把 footprint 内格子标为空，或在删除/重排后产生错误候选。`InventoryDragPreviewAdapter.IsSwapOntoOccupied()` 已经采用 jar footprint 遍历，说明修复方向明确；但两条 occupancy seam 尚未统一，因此本研究将其标记为 `UNRESOLVED`，不宣称预览正确。

### 9.3 BUE 提交与回退边界

`InventoryDragPreviewAdapter.EvaluatePlacement()`（约 `655-710`）只在普通 grid 介入：

- enhanced off、preview stale：pass-through；
- `LocallyInvalid + Occupied + IsSwapOntoOccupied`：回到 native swap；
- Candidate：调用 `NativeInventoryInteractionAdapter` 的 `SendDragItem`，再 `stopDrag()`；
- Cancelled：停止增强拖拽，不直接改 native inventory；
- slot、AREA、非普通 page：pass-through。

`NativeInventoryInteractionAdapter.cs:47-100` 将 ground source 走 `TakeGroundItem`，普通 source 走 `SendDragItem`，且最终仍由 native action 调用 `player.inventory.sendDragItem` 或 `ItemManager.takeItem`。`BetterItemInteractionUiComponent.OnDragReleased()`（`ItemInteractionUiComponent.cs:417-457`）将提交后等待状态限定为视觉投影等待，不把 timeout 解释为服务端拒绝。

## 10. UPM 对照：可复用的是生命周期模式，不是库存 renderer

归因文件：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\docs\third-party\UnturnedPluginManager-attribution.md`；来源提交 `9b75730`，作者 `35117+Deepseek-v4-falsh-0731`。BUE 运行时不依赖 UPM DLL。

UPM `PluginManagerMod.cs` 的可核对事实：

- `:13,36-43` 在 `Awake()` 创建 Harmony 并 `PatchAll()`；失败后保留 `Update()` 轮询兜底；
- `:47-50` `BaseUnityPlugin.Update()` 驱动 `PluginManagerUI.Tick()`；
- `:79-105` 保存自身 container、入口按钮、反射字段和附着父节点；
- `:163-185` 通过 `AddChild` 挂到给定 parent；
- `:187-213` 通过 `SleekWrapper.GetProxyImplementation()` 检测底层 uGUI/gameObject 是否仍有效；
- `:321-350` 比较 `MenuUI.container`/`PlayerUI.container`，发现 UI 重建就清空旧引用；
- `:420-545` 反射读取 `MenuWorkshopUI.container` 和 `PlayerPauseUI.container`；
- `:2214-2275` patch 菜单构造、ESC、`closeAll`，没有触碰 `SleekItems.grid/itemsPanel` 或 inventory `onPlacedItem`。

对 BUE 的直接启示：

1. `Harmony fast path + plugin-owned Update/poll fallback` 是已存在的本地 UI 生命周期模式；
2. `AddChild` 后必须检测底层对象和 parent 重建；
3. UPM 的根级全屏 overlay 适合插件管理窗口，不可直接推导为 inventory grid 的 content overlay；
4. UPM 没有证明 IMGUI/UITK 的 inventory hit-testing、footprint occupancy 或拖拽旋转；不能把 UPM 的 `IsElementAlive` 当作 BUE inventory runtime PASS。

## 11. 最终裁定与真实客户端验证义务

### 11.1 源码确认（可用于设计/实现）

- `SleekItems` 的三层 native hierarchy 和 `grid.OnClicked` 命中方式；
- `PlayerDashboardInventoryUI` 的 G inventory、F storage、vehicle trunk page 7 共享 surface；
- 原生 drag fields、50-unit cell、`dragPivot`、R rotation、`sendDragItem`/`sendSwapItem` 分支；
- `Items.items` 为 top-left list、`slots[,]` 为真实 occupancy；
- frame 应挂 content、icon 应挂 top-level；
- BUE 当前 reflection/AddChild/delegate/poll seam 及 occupancy 差异。

### 11.2 仍需真实客户端验证（不得以编译替代）

| 验证编号 | 场景 | 必须观察的证据 |
|---|---|---|
| `VO-INV-01` | G 打开/关闭背包，换服装尺寸 | surface generation、父链、frame/icon mount/unmount、原生 drag 恢复 |
| `VO-INV-02` | F 开普通箱子，换箱、离开距离 | `openStorage` → page 7、Storage session 失效、旧 overlay 不残留 |
| `VO-INV-03` | 驾驶员 G trunk，入座/离座 | `isStorageTrunk`、`Storage_Trunk`、page 7 grid、离座 cleanup |
| `VO-INV-04` | 1x1/2x1/1x2/2x2，R 连续 0→1→2→3→0 | pivot、icon rotation、frame footprint、抓取点不跳变 |
| `VO-INV-05` | 滚动到 content 上下边界 | frame 与 `itemsPanel` 同步滚动，icon 不被 clip，pointer 不双重加 scroll |
| `VO-INV-06` | 空格、越界、占用、多格 footprint、native swap | 红/绿态与 `Items.checkSpace*`/`findIndex` 一致；不存在 list-index occupancy 假阳性 |
| `VO-INV-07` | IMGUI、uGUI、UIToolkit（若目标发行版支持） | 坐标方向、alpha、z-order、raycast、异步图标刷新分别记录 |
| `VO-INV-08` | Host/Client 与 U3DS 进程 | 客户端 UI 类型不加载到 U3DS；权威提交仍为原生 RPC；两端日志和 DLL hash 绑定同一 Case ID |

**最终判断**：原生 UI 上的“绿色/红色 footprint frame + 顶层浮动物品图标 + 受控普通网格 delegate wrapper”在结构上可行；BUE 的挂载和拖拽 seam 已基本对齐，但 occupancy adapter 的线性索引仍是阻断级静态风险。只有修正/统一 footprint occupancy，并完成上述实机证据后，才能把该功能从 `SOURCE_CONFIRMED/UNRESOLVED` 推进到 `RUNTIME_CONFIRMED` 或发布资格。

