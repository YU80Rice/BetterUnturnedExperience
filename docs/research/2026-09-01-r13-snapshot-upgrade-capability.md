# DEV-16D-R13 快照 vs 三份渲染调研报告 + GPT 架构评审规划：理论实现能力核对

> 调研日期：2026-09-01（本核对紧随 R13 归档提交 `e187b24`）
> 调研方式：只读对照 —— 三份调研报告 + 一份架构评审规划 vs 当前仓库最新快照构建（HEAD `e187b24`，DEV-16D-R13 `resolved`，正式 DLL `BetterUnturnedExperience-1925.dll`，SHA-256 `45D509...`）
> 约束：只读，未修改任何生产代码/配置/测试，未构建，未生成 DLL。

## 0. 输入材料

| 编号 | 材料 | 路径 | 身份 |
|---|---|---|---|
| R1 | GPT 调研报告 | `.scratch/better-unturned-experience-architecture/research/U3SDK-inventory-rendering-injection-research.md` | U3-SDK HEAD `ea7b4973`；BUE 对照基于 R13 前 |
| R2 | Gemini 调研报告 | `D:\Agent-工作目录\docs\unturned-native-inventory-and-grid-injection-analysis.md` | 同 U3-SDK；附 `bue-decompile-r8` 对照 |
| R3 | DeepSeek 调研报告 | `docs/research/2026-09-01-native-grid-rendering-boundaries.md` | 本会话早前撰写，全部回源码取证 |
| A1 | GPT 架构评审规划（R13 前） | `C:\Users\The New Age\AppData\Local\Temp\architecture-review-20260901-1033.html` | 固定 BUE HEAD `c1919c4a` vs 基线 `43d05ef`；裁定"部分符合"，5 个深层化候选 |

## 1. 三份报告 + 架构评审的核心主张（摘要）

**一致认可（三报告 + A1）**：
1. 原生网格真实层级 = `SleekItems` → `horizontalScrollView` → `grid`(ISleekSprite, 空格命中) → `itemsPanel`(内容挂载) → `SleekItem`；50px/格、`jar.x*50`。
2. 最高兼容注入 seam = 反射读私有 `itemsPanel/grid/horizontalScrollView` + `Glazier.Get()` 建原生元素 + `AddChild` + 公开委托包装 `SleekItems.onPlacedItem` + `PlayerUI.Update` postfix 心跳；**不替换 Glazier.Root、不整段 Harmony patch onPlacedItem**。
3. frame（绿/红占据框）应挂 **content（itemsPanel）**；浮动 icon 应挂 **顶层（PlayerUI.container）**。
4. 容器(F)与后备箱(G)共用 Page 7 `items[5]`，仅 `isStorageTrunk`/header 不同。

**分歧/独家主张**：
- R2（Gemini）：占据框**禁用 `CreateBox()`，必须 `CreateImage(PixelTexture)`** —— 因 `GlazierBox_uGUI` 硬编码 `raycastTarget=true`（U3-SDK `GlazierBox_uGUI.cs:219`）会吞掉网格点击；`CreateImage` 默认 `raycastTarget=false`（`GlazierImage_uGUI.cs:201`）。另建议 `SetAsFirstSibling()` 下压层级。
- R1（GPT）/A1：**阻断级** = BUE `UnturnedGridOccupancyView.IsOccupied()` 用 `y*width+x` 线性索引查 `Items.items`，与原生 `slots[,]` + `ItemJar` footprint 语义冲突（A1 候选 1）。
- A1 候选 2-5：拆分两个过宽 Adapter（surface ~53KB / drag ~41KB）、单一坐标采样 seam（`CoordinateSample`）、统一运行时装配 seam（`InventoryFeatureRuntimeWiring`）、提取 Preview/Projection Session —— 均为重构深化项，非正确性阻断。
- R1/R2：建议把 BUE 页面范围从 `{3,7}` 扩展至全 9 页（尤其背心/上衣/裤子/手中）。

## 2. 当前快照（HEAD e187b24）逐项核对

### 2.1 A1 候选 1（阻断级：occupancy 线性索引 vs footprint）—— ✅ 已闭合（R13 落实）

**A1 依据**（R13 前）：`InventorySurfaceLifecycleAdapter.cs:149-166` 的 `UnturnedGridOccupancyView.IsOccupied` 直接 `y*width+x` 访问 `Items.items`。

**当前快照事实**（源码核对）：
- `NativeItemGridOccupancySnapshot.cs`（80 行，R13 新增，提交 `8d093f2 "DEV-16D-R13: unify native item footprint occupancy"`）：
  - `TryCreateFromItems(items, excludedJar, out snapshot)`（L33-78）**按 `ItemJar(x,y,rot)` 展开旋转 footprint**（`rot & 1` 交换宽高，L48-53），逐格写入 `bool[] occupied`；
  - **越界 jar 直接拒绝快照**（L66-70，fail-closed），不会发布残缺快照。
- `UnturnedGridOccupancyView.IsOccupied`（`InventorySurfaceLifecycleAdapter.cs:171-174`）**已委托** `snapshot.IsOccupied(x,y)`；`RebuildForDrag`（L178-217）在同容器拖拽时排除源 jar 重建快照。
- **preview 与 swap guard 共用同一 seam**：
  - preview：`PlacementCandidateEvaluator.Fits/Search`（`PlacementCandidateEvaluator.cs:73-99`）消费 `IGridOccupancyView`；
  - swap guard：`InventoryDragPreviewAdapter.FootprintOccupied`（L805-817）消费同一 `IGridOccupancyView`（注释明确 "operates on the canonical occupancy seam. It does not build or own a second item grid"）；
  - 二者经 `ItemInteractionUiComponent.TryGetOccupancyForDrag`（L741-784）→ `UnturnedInventorySurfaceContext.TryCreateOccupancyForDrag`（L556-570）→ `UnturnedGridOccupancyView.RebuildForDrag` 取同一快照。
- 契约：`IGridOccupancyView` 定义于 `Contracts/ContractTypes.cs:129`。

**判定**：A1 裁定为"当前唯一明确的阻断级静态差异"的候选 1，在当前快照**已被 R13 修复并统一** —— R1 §9.2 的 `UNRESOLVED` 标记针对的是 R13 前版本，不再适用于 HEAD `e187b24`。**理论实现能力：具备且已落地。**

### 2.2 R2 的 CreateBox raycast 隐患 —— ⚠️ 仍未处理（开放静态风险）

**当前快照事实**（源码核对）：
- 占据框元素：`ItemInteractionUiComponent.cs:108` `frameElement = gridPanelContainer.CreateBox()` → `UnturnedVisualContainer.CreateBox()`（`InventorySurfaceLifecycleAdapter.cs:134`）→ **`Glazier.Get().CreateBox()`**。
- 全仓 grep：`SetAsFirstSibling`、`IsRaycastTarget`、`raycastTarget` **零命中** —— 未按 R2 建议切换 `CreateImage`、未显式关闭 raycast、未下压层级。
- R2 引用的 U3-SDK 事实成立：`GlazierBox_uGUI.cs:219` 硬编码 `imageComponent.raycastTarget = true`；`GlazierImage_uGUI.cs:201` 默认 `false`。

**影响分析**：box 挂 itemsPanel（grid 的子节点、内容层在 sprite 之上），uGUI 射线检测按层级从最上层开始 —— 若 box `raycastTarget=true`，落在框区域内的点击可能被框截获，`grid.OnClicked` 收不到 → 空格放置失败或行为异常。这是**静态可证的风险**，但**未经验证**：
- 是否真实吞点击取决于 uGUI 命中顺序、box 是否 `IsVisible` 且带有效 Graphic（alpha>0）、以及 BUE 是否有隐含路径关闭（当前未见）。
- 需真机复测确认；若复现，修复方向即 R2 建议（`CreateImage(GlazierResources.PixelTexture)` 或反射置 `raycastTarget=false`）。

**判定**：**理论实现能力：具备**（原生提供 `CreateImage`/`ISleekImage` 的 raycast-passthrough 路径），但**当前实现未采用，风险开放**。

### 2.3 全页支持扩展（R1/R2 建议 8 页全覆盖）—— ⏳ 未实施（能力存在、范围未扩）

**当前快照事实**：
- `ItemInteractionUiComponent.cs:284-287`：`IsSupportedEnhancedPage(byte page) => page == 3 || page == 7`；
- `InventoryDragPreviewAdapter.cs`：`SupportedPages = { BackpackPage(3), StoragePage(7) }`（约 L269）；
- R13 工单确认维持"页面矩阵：Backpack、Storage、Trunk 进增强；Primary/Secondary、Hands、Vest、Shirt、Pants、AREA、拖出、未知页 Pass-Through"。

**判定**：三份报告一致确认所有页面（2..8）都是**同一 `SleekItems` 类、同一层级、同一 50px 语义**（R3 §3.2），扩展仅需把 `SupportedPages` 数组与 `IsSupportedEnhancedPage` 扩展，并复用同一 seam —— **理论实现能力：具备**；但属于**范围决策**（涉及 AREA 拖出/装备槽语义，需 spec 裁决），当前未做。

### 2.4 A1 候选 2-5（重构深化项）—— ⏳ 未实施（非阻断）

当前快照 grep：`CoordinateSample`、`InventoryFeatureRuntimeWiring`、`FootprintOccupancyAdapter`（作为独立类型）均不存在；`InventorySurfaceLifecycleAdapter.cs`（1225 行）与 `InventoryDragPreviewAdapter.cs`（942 行）仍为大型 adapter。A1 本身标注 2-4 为 "Worth exploring"、5 为 "Speculative"，**不影响正确性判定**。

**判定**：这些是**架构深化方向而非实现能力缺口**；当前 seam（反射 + AddChild + delegate + heartbeat）足以承载后续拆分。**理论实现能力：具备。**

### 2.5 已对齐且 R13 复核通过的 seam（R1/R2/R3/A1 共同推荐，当前快照全部在位）

| 推荐 seam | 当前快照位置 | 状态 |
|---|---|---|
| 反射读 `SleekItems.horizontalScrollView/grid/itemsPanel` + `PlayerDashboardInventoryUI.items` | `InventorySurfaceLifecycleAdapter.cs:333-336`（`NativeScrollField/NativeGridField/NativeItemsPanelField/DashboardItemsField`） | ✅ |
| 父链校验（owner→scroll→grid→itemsPanel） | `InventorySurfaceLifecycleAdapter.cs:394-418`（`ProbeNativeHierarchy`） | ✅ |
| frame 挂 content（itemsPanel） | `ItemInteractionUiComponent.cs:108,134` + `InventorySurfaceLifecycleAdapter.cs:1188` | ✅（含 R2 风险点） |
| icon 挂顶层（PlayerUI.container） | `ItemInteractionUiComponent.cs:109,135` + `InventorySurfaceLifecycleAdapter.cs:1185` | ✅ |
| 公开委托包装 `SleekItems.onPlacedItem`（exact original 保存 + 幂等 attach/detach） | `InventoryDragPreviewAdapter.cs:162-221,306-316`（R13-6 新增 `AttachNativeGrid`/`DetachGridAndDiscardSurface`） | ✅（R13-6 双轴 CLEAN） |
| `PlayerUI.Update` postfix 心跳 + 拖拽 tick | `InventorySurfaceLifecycleAdapter.cs:859-860`、`InventoryDragPreviewAdapter.cs:109-110` | ✅ |
| 原生提交只走 `sendDragItem`/`ItemManager.takeItem` | `InventoryDragPreviewAdapter.cs:879-900`（`NativeDragActions`） | ✅ |
| fail-closed：成员探测、异常隔离、unhook/unmount | `InventoryDragPreviewAdapter.cs:70-89,240-304`；`InventorySurfaceLifecycleAdapter.cs:923-938` | ✅ |

## 3. 理论实现能力裁定

| 升级方向 | 来源 | 当前快照是否具备理论能力 | 当前状态 |
|---|---|---|---|
| 统一占据事实源（footprint occupancy） | A1 候选 1（阻断级）/ R1 §9.2 | ✅ 具备 | **已实现**（`NativeItemGridOccupancySnapshot`，preview/swap 共用 seam） |
| 占据框挂 content + 顶层 icon | 三报告 + A1 | ✅ 具备 | 已实现 |
| CreateImage 替代 CreateBox / 关闭 frame raycast | R2 | ✅ 具备（原生提供 passthrough 路径） | **未实施**（仍 CreateBox，风险开放） |
| 全 9 页增强范围 | R1/R2 | ✅ 具备（同控件类同 seam） | 未实施（仍 {3,7}，范围决策待 spec） |
| 拆分 adapter / 坐标采样 / 装配 seam / Session | A1 候选 2-5 | ✅ 具备（重构方向） | 未实施（非阻断） |
| delegate 包装 + heartbeat + fail-closed | 三报告 + A1 | ✅ 具备 | 已实现并通过 R13 双轴 CLEAN |

## 4. 结论

**是。对照当前仓库最新快照（HEAD `e187b24`，DEV-16D-R13 `resolved`），GPT 架构评审规划中的所有升级方向均具备理论上的实现能力**，且：

1. **唯一被裁定为"阻断级"的候选 1（occupancy 线性索引 vs footprint 语义）已由 R13 实际闭合** —— 当前快照的 `NativeItemGridOccupancySnapshot` + `UnturnedGridOccupancyView` 委托 + preview/swap 共用同一 `IGridOccupancyView` seam，正是 A1 候选 1 的"After"形态。R1 的 `UNRESOLVED` 标记仅针对 R13 前版本。
2. **三份报告共同推荐的注入 seam（反射层级 + AddChild 双挂载点 + 公开委托包装 + PlayerUI.Update postfix + 原生提交路径 + fail-closed）在当前快照全部在位**，且 R13-6 双轴 CLEAN 复核通过。
3. **仍需处理/验证的开放项**：
   - **R2 的 `CreateBox` raycast 隐患仍未实施对应修复**（frame 仍 `CreateBox()`，无 `SetAsFirstSibling`/`IsRaycastTarget` 处理）——静态风险成立，需真机复测；如复现按 R2 方案（`CreateImage(PixelTexture)` 或关闭 raycast）修复。
   - **全页支持（2/4/5/6/8）未扩展** —— 理论可行、属范围决策，需 spec 裁决（涉及 AREA/装备槽语义）。
   - A1 候选 2-5 为重构深化方向，非能力缺口。

**结论边界**：以上为"理论实现能力 + 静态符合性"核对；真实玩法资格仍需按 R13 报告 §六/§七 的 DEV-16E 实机证据（单人、SteamP2PFriends Host/Client、U3DS Headless）验证，静态 CLEAN ≠ 运行通过。

## 5. 附录：本次核对引用文件

- 当前快照：`src/BetterUnturnedExperience.Plugin/InventorySurfaceLifecycleAdapter.cs`、`InventoryDragPreviewAdapter.cs`、`NativeItemGridOccupancySnapshot.cs`；`src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs`；`src/BetterUnturnedExperience.Core/Placement/PlacementCandidateEvaluator.cs`；`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`
- 报告：三份调研报告 + `architecture-review-20260901-1033.html`
- U3-SDK 佐证（R2 引用）：`Glazier_uGUI/GlazierBox_uGUI.cs:219`、`Glazier_uGUI/GlazierImage_uGUI.cs:201`（本会话早前已核对）
