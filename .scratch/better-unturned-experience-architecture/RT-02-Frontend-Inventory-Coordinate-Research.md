> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-RT-02：物品拖动、坐标转换与 Glazier 渲染链调研报告（第二轮修订版）

> **作者**: Gemini（前端负责人）  
> **审查者**: GPT（共享契约与权威边界强制 Reviewer）  
> **适用任务**: RT-02  
> **依赖基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **Predecessor SourceSetId**: `BUE-SS-20260824-01`  
> **Migrated SourceSetId**: `BUE-SS-20260824-02`（Manifest: `BUE-SS-20260824-02-Manifest.md`）  
> **证据枚举标准（严格对齐 RT-01 §10.2）**:  
> `SOURCE_CONFIRMED`、`IL_CONFIRMED`、`PROTOTYPE_ONLY`、`BUILD_CONFIRMED`、`RUNTIME_CONFIRMED`、`RELEASE_CONFIRMED`、`UNRESOLVED`  

---

## 1. 调研目标与范围

本报告依据需求总纲 `spec.md` 及 `GPT-RT-01` 共享契约基线，针对 Unturned 原生 SDG Glazier UI 体系（Sleek UI）进行端到端的固定源码事实调研。

目标在于建立从**玩家鼠标物理输入 → Viewport/UI Scale 变换 → 抓取偏移与预期几何中心（JCR-07 Seam） → 共享候选评估器（`IPlacementCandidateEvaluator`） → Glazier 绿/红占据框渲染 → 原生 `sendDragItem` 提交 → 原生库存投影观察**的完整确定性证据链。

---

## 2. 公共证据来源表（Evidence Source Identity Table）

| EvidenceSourceId | 相对路径 / 资产标识 | Git Commit / SHA-256 | EnvironmentRole | EnvironmentLimits | CapturedBy / At |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`U3SRC-PDINV`** | `Assets/Runtime/Assembly-CSharp/Unturned/UI/Player/PlayerDashboardInventoryUI.cs` | Commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`<br>SHA-256 `593EDCB1AF5E19E548353BA3A2F97EA3351C1921DD348747AF99ABB95179566C` | Client / SP / P2P Client | 仅静态源码证据，不代表生产运行 | Gemini / 2026-08-24 |
| **`U3SRC-PINV`** | `Assets/Runtime/Assembly-CSharp/Unturned/Player/PlayerInventory.cs` | Commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`<br>SHA-256 `8485CBF8D4EC75A35D43A20F4BE8D6B401A58A68017B7EBA0E111D663F890FAC` | Client / SP / P2P Client | 仅静态源码证据，不代表生产运行 | Gemini / 2026-08-24 |
| **`U3SRC-SITEMS`**| `Assets/Runtime/Assembly-CSharp/Unturned/Sleek/SleekItems.cs` | Commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`<br>SHA-256 `7DDD51D5250CB53F45AB02D8947F8C84620F1B9FE4CEEC312F9D8B0C39D2CC5C` | Client / SP / P2P Client | 仅静态源码证据，不代表生产运行 | Gemini / 2026-08-24 |
| **`PROTO-JS-88`** | `prototypes/test-gpt12-placement.js` | SHA-256 `2B976A62B224E70EEFA29A8A6FCE8EAB273F642FFA99F51415E42C1CC82B00F7`<br>EvidenceClass: `PROTOTYPE_ONLY` | Local Test Runner | 仅验证 4 级 Local-Fit 算法行为 | Gemini / 2026-08-24 |

---

## 3. 物品栏与容器 UI 完整生命周期与调用链（Lifecycle Analysis）

### 3.1 UI 根节点与生命周期控制
在 U3-SDK 源码中，玩家仪表盘与库存界面的核心入口为 `SDG.Unturned.PlayerDashboardInventoryUI`：

1. **构造与初始化**（`U3SRC-PDINV:2560-2820`）：
   * 静态构造函数实例化顶级全屏容器 `container = new SleekFullscreenBox()`。
   * 构造衣物装备栏 `clothingBox`、附近物品拾取栏 `areaBox`、快捷装备栏 `slots` 以及主容器网格 `items`。
   * 订阅原生库存数据流事件：`onInventoryAdded`、`onInventoryRemoved`、`onInventoryResized`、`onInventoryUpdated`。
2. **界面开启（`open()`）**（`U3SRC-PDINV:131-179`）：
   * 触发人物动画 `Player.LocalPlayer.animator.sendGesture(EPlayerGesture.INVENTORY_START, false)`。
   * 激活 3D 人物预览相机 `character.Find("Camera").gameObject.SetActive(true)`。
   * 依据屏幕宽度分流布局：`isSplitClothingArea = Screen.width >= 1350`。
   * 执行进场动画 `container.AnimateIntoView()`。
3. **界面关闭（`close()`）**（`U3SRC-PDINV:181-198`）：
   * 触发停止动画 `EPlayerGesture.INVENTORY_STOP` 并停用预览相机。
   * **强制清理拖拽**：调用 `stopDrag()`（`Line:193`），保证关闭界面时不残留悬浮图标。
   * 执行退场动画 `container.AnimateOutOfView(0, 1)`。

---

## 4. 原生拖拽机制、内部状态与代际等价映射

### 4.1 原生拖拽内部字段与状态分析
原生 Unturned 在 `PlayerDashboardInventoryUI` 中通过以下静态字段维护拖拽上下文（`U3SRC-PDINV:34-48`）：

| 原生字段名 | 类型 | 源码用途 | 对应共享契约/Presenter 映射 | 证据引用 |
| :--- | :--- | :--- | :--- | :---: |
| `isDragging` | `bool` | 当前是否处于拖拽状态 | `DragInteractionState.Dragging / Hovering` | `U3SRC-PDINV` |
| `dragJar` | `ItemJar` | 正在被拖拽的物品数据包 | Presenter 内部只读缓存，提取 `size_x`, `size_y`, `rot` | `U3SRC-PDINV` |
| `dragSource` | `SleekItem` | 来源网格 UI 元素引用 | Presenter 来源视图追踪 | `U3SRC-PDINV` |
| `dragItem` | `SleekItem` | 跟随光标移动的悬浮预览图元 | 前端 `SleekFloatingDragIcon` 表现图元 | `U3SRC-PDINV` |
| `dragOffset` | `Vector2` | 初始抓取点相对于物品原点的像素偏移 | 转换为连续网格抓取偏移 `grabOffsetInFootprint` | `U3SRC-PDINV` |
| `dragPivot` | `Vector2` | 经过旋转变换后的当前渲染 Pivot 像素偏移 | 驱动悬浮图元局部定位 `PositionOffset_X/Y` | `U3SRC-PDINV` |
| `dragFromPage` | `byte` | 来源背包页码（0~7） | `ItemGridPosition.Page` | `U3SRC-PDINV` |
| `dragFrom_x` | `byte` | 来源网格 X 坐标 | `ItemGridPosition.X` | `U3SRC-PDINV` |
| `dragFrom_y` | `byte` | 来源网格 Y 坐标 | `ItemGridPosition.Y` | `U3SRC-PDINV` |
| `dragFromRot` | `byte` | 来源物品原始朝向（0~3） | `ItemGridPosition.Rotation` | `U3SRC-PDINV` |

### 4.2 代际隔离（`DragGeneration`）等价设计
* **原生缺陷**：原生 Unturned 没有 `DragGeneration` 令牌概念。当网络延迟或并发关闭界面时，容易引发旧回调污染新拖拽。
* **插件 Presenter 增强**：在 `InventoryDragPresenter` 中引入单调递增的 `uint DragGeneration`。每次 `startDrag` 分配新 Token；在 UI 关闭、角色死亡、换服或 2.0s 超时后，旧 Token 立即失效，彻底隔离异步竞态。

---

## 5. 坐标系转换管线、UI Scale 与 JCR-07 Seam 严格验证

### 5.1 核心度量衡与 UI Scale
* **单元格像素常数**：Unturned 原生网格严格以 **$50.0\text{px}$** 为标准单格基准尺寸（`U3SRC-SITEMS:121`, `U3SRC-PDINV:1162, 2390`）。
* **视口与光标映射**：
  * 光标全局归一化坐标：`InputEx.NormalizedMousePosition`（范围 $[0, 1] \times [0, 1]$）。
  * 视口变换 API：`PlayerUI.container.ViewportToNormalizedPosition(Vector2)`（`U3SRC-PDINV:2413`）。
  * 网格局部归一化坐标：`grid.GetNormalizedCursorPosition()`（`U3SRC-SITEMS:221`），返回相对于当前网格宽高的比例。
  * **网格连续浮点坐标换算**：
    $$\text{pointerGridX} = \text{grid.GetNormalizedCursorPosition().x} \times \text{width}$$
    $$\text{pointerGridY} = \text{grid.GetNormalizedCursorPosition().y} \times \text{height}$$

### 5.2 JCR-07 坐标 Seam 转换数学闭环
依据 JCR-07 联合裁定，Presenter 严格执行如下坐标适配：

1. **抓取初始冻结**（闭域 $[0, W] \times [0, H]$）：
   $$\text{grabOffsetInFootprint} = \text{pointerGrid} - \text{sourceItemOriginGrid}$$
2. **每帧预期几何中心换算**（半开域 $[0, \text{containerWidth}) \times [0, \text{containerHeight})$）：
   $$\text{intendedItemCenterGrid} = \text{pointerGrid} + (\text{currentFootprintCenter} - \text{grabOffsetInFootprint})$$
   *(其中 $\text{currentFootprintCenter} = (W/2, H/2)$)*。
3. **原生 Forward 顺时针旋转（Unturned `rot++` / 按 `[R]` 键）**：
   * 原生触发源码依据：`U3SRC-PDINV:2425-2428`。
   * **Forward 变换公式（Native `rot + 1`）**：
     $$\text{newGrabX} = H - \text{oldGrabY}, \quad \text{newGrabY} = \text{oldGrabX}$$
     *（新 Footprint 尺寸为 $H \times W$）*。
   * **Backward 逆向公式（Native `rot - 1`）**：
     $$\text{newGrabX} = \text{oldGrabY}, \quad \text{newGrabY} = W - \text{oldGrabX}$$

### 5.3 4 向旋转严格映射验证表

| 初始点 $(x, y)$ | 语义位置 | 旋转 1 次 (`rot=1`) | 旋转 2 次 (`rot=2`) | 旋转 3 次 (`rot=3`) | 旋转 4 次 (`rot=0`) | 验证来源 |
| :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| $(0, 0)$ | 左上角 | $(H, 0)$ [右上] | $(W, H)$ [右下] | $(0, W)$ [左下] | **$(0, 0)$ [左上 恢复]** | `PROTO-JS-88` |
| $(W, 0)$ | 右上角 | $(H, W)$ [右下] | $(0, H)$ [左下] | $(0, 0)$ [左上] | **$(W, 0)$ [右上 恢复]** | `PROTO-JS-88` |
| $(W, H)$ | 右下角 | $(0, W)$ [左下] | $(0, 0)$ [左上] | $(H, 0)$ [右上] | **$(W, H)$ [右下 恢复]** | `PROTO-JS-88` |
| $(0, H)$ | 左下角 | $(0, 0)$ [左上] | $(H, 0)$ [右上] | $(H, W)$ [右下] | **$(0, H)$ [左下 恢复]** | `PROTO-JS-88` |
| $(W/2, H/2)$ | 几何中心 | $(H/2, W/2)$ | $(W/2, H/2)$ | $(H/2, W/2)$ | **$(W/2, H/2)$ [中心 恢复]** | `PROTO-JS-88` |

---

## 6. Glazier 图元挂载点、层级与 Z-Order 裁定

### 6.1 图层挂载点分析与隔离
在 SDG Glazier 树中，不同视觉组件对滚动与裁剪有截然相反的需求，**严禁挂载在同一个全局 Root 上**：

1. **浮动拖拽图标（`SleekFloatingDragIcon`）**：
   * **挂载点**：`PlayerDashboardInventoryUI.container`（顶级全屏容器，`U3SRC-PDINV:19`）。
   * **原因**：跟随光标在全屏幕自由穿梭，必须超越所有局部 ScrollView 的视口剪裁（Clip），拥有最高 Z-Order。
   * **定位机制**：`PositionScale_X/Y` 绑定视口归一化位置，`PositionOffset_X/Y` 绑定 `dragPivot`。
2. **占据网格图层（`SleekInventoryFootprintLayer`）**：
   * **挂载点**：目标容器对应的 `SleekItems.itemsPanel` 或 `grid` 容器内部（`U3SRC-SITEMS:254`）。
   * **原因**：占据框必须随目标容器的 `horizontalScrollView` 或主界面垂直滚动条同步平移。
   * **零 GC 对象池设计**：常驻预分配 `SleekImage` 矩阵池（最大支持 $10 \times 10$ 单元格），每帧仅更新 `PositionOffset_X/Y`、`SizeOffset_X/Y` 以及 `TintColor`（待生产 C# 验证）。

---

## 7. 原生库存投影观察、事实源与音效策略（对齐 B-02）

### 7.1 原生事件的事实源性质（对齐 B-02）
Unturned 客户端原生库存数据流包含以下核心事件（`U3SRC-PINV:34-45`）：

1. **`onInventoryAdded(byte page, byte index, ItemJar jar)`**：
   * **性质**：本地原生库存模型应用新增变化时触发，**绝非 BUE 插件的自定义提交 ACK**。
   * 负责在目标 `SleekItems` 中生成/显示新物品图元。
2. **`onInventoryRemoved(byte page, byte index, ItemJar jar)`**：
   * **性质**：本地原生库存模型应用移除变化时触发。
   * 负责在源 `SleekItems` 中销毁旧物品图元。
3. **高置信收敛断言**：
   * 仅当同时满足 `DragGeneration` 相符、处于同一库存会话、源/目标物品指纹（AssetID、数量、状态）匹配时，才将原生事件标记为当前提交的高置信完成。
   * 发生并发、拆分或合并歧义时，直接以原生最新状态为唯一事实源静默刷新，不弹网络失败，不伪造回滚。

### 7.2 放置音效与输入焦点裁定
* **音效触发点**：
  * 原生在 `startDrag()`、`stopDrag()` 与旋转时调用 `PlayerDashboardInventoryUI.PlayInventoryAudio(dragJar.GetAsset())`（`U3SRC-PDINV:220, 232, 2434`）。
  * **裁定**：释放放置时直接依赖原生 `onPlacedItem` / `sendDragItem` 链路触发原生放置音效，**插件增强层严禁重复调用音频播放 API**，杜绝音效叠加双响 Bug。
* **输入焦点**：
  * 旋转按键：绑定 `ControlsSettings.rotate`，仅在 `PlayerDashboardInventoryUI.active && isDragging` 时捕获，不干扰聊天框与其他文本输入。

---

## 8. Harmony 拦截点、边界收敛与兼容性风险（对齐 B-01）

### 8.1 严禁全量拦截 `onPlacedItem` 的受控 Adapter 方案（对齐 B-01）

原生 `PlayerDashboardInventoryUI.onPlacedItem`（`U3SRC-PDINV:1154-1265`）包含极复杂的原生分支：
- 快捷槽 `page < PlayerInventory.SLOTS` 的装备合法性校验与即时装备；
- 原位置点击 `dragFrom == target` 时的取消拖动；
- 附近掉落物 `PlayerInventory.AREA` 的地面拾取（`ItemManager.takeItem`）与丢弃；
- 原生 `checkSpaceDrag` 本地前置校验；
- `stopDrag()`、装备切换、Dashboard 关闭与 Life UI 恢复。

**高风险拦截修订方案**：
1. **严格限制增强作用域**：仅当放置目标为普通背包/容器网格（`page >= PlayerInventory.SLOTS && page != PlayerInventory.AREA`）时才介入。
2. **非普通网格完全放行（Pass-Through）**：对快捷槽、AREA、同位置取消等所有特殊分支，Prefix **直接 `return true` 放行交由原生逻辑处理**，绝不绕过原生装备或丢弃逻辑。
3. **普通网格落点替换**：
   * 若 Evaluator 返回合法候选 `Candidate`：将目标坐标替换为 `(Candidate.Page, Candidate.X, Candidate.Y, Candidate.Rotation)`，调用原生 `sendDragItem`，执行 `stopDrag()`，并 `return false` 阻止原生粗糙的 `offset_x/y` 计算。
   * 若 Evaluator 返回 `LocallyInvalid` 或 `Hidden`：不执行任何提交，直接调用原生 `stopDrag()` 结束增强拖拽并保留原物，`return false`。
4. **风险评级**：将此 Hook 明确定义为**待生产原型验证的高风险 Seam（`UNRESOLVED`）**，后续必须建立专项测试覆盖快捷槽装备、附近拾取及同格取消等边缘场景。

---

## 9. 固定源码调用链表（Fixed-Source Call-Chain Matrix）

| 固定源码位置 | 类型与成员 | 完整方法签名 | 调用方 (Caller) | 被调用方 (Callee) | EvidenceSourceId | EvidenceClass |
| :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| `PlayerDashboardInventoryUI.cs:200-221` | `PlayerDashboardInventoryUI.startDrag` | `private static void startDrag()` | `onGrabbedItem` | `PlayInventoryAudio`, `setItemsEnabled` | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |
| `PlayerDashboardInventoryUI.cs:223-244` | `PlayerDashboardInventoryUI.stopDrag` | `public static void stopDrag()` | `close`, `onPlacedItem` | `PlayInventoryAudio`, `setItemsEnabled` | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |
| `PlayerDashboardInventoryUI.cs:1112-1151` | `PlayerDashboardInventoryUI.onGrabbedItem` | `private static void onGrabbedItem(byte page, byte x, byte y, SleekItem item)` | `SleekItems.onGrabbedItem` | `updatePivot`, `startDrag` | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |
| `PlayerDashboardInventoryUI.cs:1154-1265` | `PlayerDashboardInventoryUI.onPlacedItem` | `private static void onPlacedItem(byte page, byte x, byte y)` | `SleekItems.onPlacedItem` | `checkSpaceDrag`, `sendDragItem`, `stopDrag` | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |
| `PlayerDashboardInventoryUI.cs:2381-2403` | `PlayerDashboardInventoryUI.updatePivot` | `private static void updatePivot()` | `onGrabbedItem`, `updateDraggedItem` | 内部计算 `dragPivot` 偏移 | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |
| `PlayerDashboardInventoryUI.cs:2418-2438` | `PlayerDashboardInventoryUI.updateDraggedItem` | `public static void updateDraggedItem()` | `PlayerDashboardUI.update` | `updatePivot`, `refreshDraggedVisualPosition` | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |
| `PlayerDashboardInventoryUI.cs:2070-2081` | `PlayerDashboardInventoryUI.onInventoryAdded` | `private static void onInventoryAdded(byte page, byte index, ItemJar jar)` | `PlayerInventory.onInventoryAdded` | `items[page].addItem` | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |
| `PlayerDashboardInventoryUI.cs:2083-2094` | `PlayerDashboardInventoryUI.onInventoryRemoved` | `private static void onInventoryRemoved(byte page, byte index, ItemJar jar)` | `PlayerInventory.onInventoryRemoved` | `items[page].removeItem` | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |
| `SleekItems.cs:219-227` | `SleekItems.onClickedGrid` | `private void onClickedGrid()` | `grid.OnClicked` | `onPlacedItem.Invoke` | `U3SRC-SITEMS` | `SOURCE_CONFIRMED` |
| `PlayerInventory.cs:1894-1920` | `PlayerInventory.sendDragItem` | `public void sendDragItem(byte page_0, byte x_0, byte y_0, byte page_1, byte x_1, byte y_1, byte rot_1)` | `onPlacedItem` / 增强 Presenter | 原生 RPC 派发至服务端 | `U3SRC-PINV` | `SOURCE_CONFIRMED` |

---

## 10. 契约充分性审查与后续验证义务（Verification Obligations）

1. **共享契约充分性**：
   * `GPT-RT-01` 冻结契约完全覆盖前端需求，**未产生任何 Shared Contract Change Request**。
2. **后续验证义务（Verification Obligations / `UNRESOLVED` 门禁）**：
   * `VO-RT02-01`：在开发阶段建立 C# 原型测试，验证 `onPlacedItem` Prefix 在快捷槽、AREA 及普通网格中的精确分支放行与落点替换行为（当前分类：`UNRESOLVED`）。
   * `VO-RT02-02`：在 Release 构建下对 `SleekInventoryFootprintLayer` 执行 C# 内存分配分析，验证连续拖拽下的 0 GC 目标（当前分类：`UNRESOLVED`）。
   * `VO-RT02-03`：在单机、P2P Host/Client 及 U3DS 三环境中执行双机拖拽放置联调，验证迟到事件与代际失效（当前分类：`UNRESOLVED`）。
3. **证据边界声明（SourceSet 迁移对齐）**：
   * 迁移至 `BUE-SS-20260824-02`；明确 U3DS BepInEx `5.4.23.5` 动态引导成功仅证明服务端 Preloader/Chainloader 基础环境就绪，**绝不推导为客户端 UI 或 BUE 单 DLL 运行 PASS**。
4. **调研结论**：
   * **RT-02 前端调研已全量吸收 B-01、B-02、B-04、C-01 审查要求并完成 SourceSet 迁移，票据状态维持 `ready-for-human`**。

---

*报告完。作者: Gemini*


