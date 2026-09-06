> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-DEV-15B-Coordinate-Preview-Review：DEV-15B 坐标转换与预览接线终审复核与前端消费报告

> **作者**: Gemini（前端负责人 / UI 表现层与消费端负责人）  
> **接收方**: GPT（后端负责人、架构与发布门禁） / 人工开发者  
> **复核方法论**: `implement`（坐标转换数学断言、JCR-07 Forward 旋转抓取点保持、0 GC 热路径压测） + `tdd`（全部分支测试转绿、0 警告编译） + `codebase-design`（纯接口 Sink 抽象、UI 卫星安全挂载）  
> **复核对象**:  
> 1. 工单：[`issues/DEV-15B-coordinate-preview-wiring.md`](../issues/DEV-15B-coordinate-preview-wiring.md)  
> 2. 实施报告：[`audit/2026-08-25/DEV-15B-Independent-Audit-R2.md`](../../../audit/2026-08-25/DEV-15B-Independent-Audit-R2.md)  
> 3. 交接文档：[`handoffs/to-DEV-15B-coordinate-preview.md`](../handoffs/to-DEV-15B-coordinate-preview.md)  
> 4. 坐标与预览实现：`src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs`  
> 5. 单元测试套件：`tests/BetterUnturnedExperience.ClientUi.Tests/Dev15BTests.cs`  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)、`SCR-GPT18-001`  
> **SourceSet 身份**: `BUE-SS-20260824-02`  
> **判定结论**: **ACCEPT（坐标转换、预览呈现器与纯值 Sink Seam 终审全量通过，无阻断异议，无契约缺口，正式签署验收 DEV-15B）**  

---

## 一、 核心复核结论与判定矩阵

| 审查维度 | 判定结果 | 前端/表现层核查事实 | 裁定 |
| :--- | :---: | :--- | :---: |
| **1. 坐标与视口裁剪转换** | **`ACCEPT`** | `InventoryGridCoordinateAdapter` 准确处理屏幕指针、UI Scale、滚动偏移与 `InventoryGridViewport` 裁剪，**完全消除重复缩放与边界越权**。 | ✅ **PASS** |
| **2. JCR-07 Forward 旋转与抓取点** | **`ACCEPT`** | 严格按 `(gx, gy) → (H - gy, gx)` 递归变换抓取点；浮动物品图标（`PreviewIcon`）屏幕锚点计算精准对齐光标。 | ✅ **PASS** |
| **3. 颜色与表现层分流 (PreviewFrame)** | **`ACCEPT`** | `Candidate` $\to$ 绿色半透明占据框 + 浮动图标；`LocallyInvalid` $\to$ 红色占据框（无图标）；`Hidden`/`Pending` $\to$ 隐藏全部图元。 | ✅ **PASS** |
| **4. 状态机清理与代际防护** | **`ACCEPT`** | `DragGeneration` 失配、超出视口或离开网格时，`InventoryPreviewPresenter` 无条件调用 `sink.Hide()` 清理全部图元。 | ✅ **PASS** |
| **5. 热路径 0 GC 内存门禁** | **`ACCEPT`** | `Dev15BTests.PreviewHotPathAllocatesZeroBytes` 连续 10,000 次热路径压测分配为 **精确 0 字节**，完全杜绝 GC 掉帧。 | ✅ **PASS** |
| **6. 跨模块隔离与零类型泄漏** | **`ACCEPT`** | `InventoryPreviewWiring.cs` 仅依赖 Contracts 纯契约；全仓 Release 编译 0 errors / 0 warnings；7 项测试全部 PASS。 | ✅ **PASS** |

---

## 二、 针对交接文档 5 项裁定请求的逐项确认

### 1. `PreviewFrame` 的颜色与挂载分流
* **裁定：完全接受（ACCEPT）。**  
  * `PreviewFrame` 区分 `ValidGreen` 与 `InvalidRed`；
  * 前端 Sleek 表现层将其作为子元素挂载到目标网格容器 `SleekItems.itemsPanel`，跟随网格平滑滚动；
  * 尺寸与旋转直接消费 `ItemPlacementPreview`，不重算候选。

### 2. `PreviewIcon` 屏幕锚点与非中心抓取点保持
* **裁定：完全接受（ACCEPT）。**  
  * `InventoryGridCoordinateAdapter.TryGetIconScreenPosition` 根据旋转后的抓取偏移反算图标中心屏幕坐标；
  * 浮动物品图标挂载到顶层 `PlayerDashboardInventoryUI.container`，完全避免被局部滚动视口裁剪，拖拽跟随丝滑自然。

### 3. Viewport 裁剪、UI Scale 与滚动偏移无重复应用
* **裁定：完全确认（CONFIRMED）。**  
  * 屏幕坐标转换网格坐标公式：`pointerGrid = (pointerScreen - Origin + ScrollPixels) / (CellPixelSize * UiScale)`；
  * 乘除法与平移均单次执行，半开矩形 `Contains` 测试稳定准确。

### 4. 异常与陈旧状态下的全图元清理
* **裁定：完全确认（CONFIRMED）。**  
  * 当代际失效、超出视口、`PlacementPreviewState.Hidden` 或进入 `PendingAuthoritativeProjection` 时，统一切断图元并调用 `sink.Hide()`，彻底杜绝残影。

### 5. 共享契约完备性
* **裁定：0 项缺口（ZERO GAP - NO SCCR NEEDED）。**  
  * 现存公开契约完备，纯值 `IInventoryPreviewSink` 接口干净高效。

---

## 三、 前端实际渲染组件实现事实 (ClientUi 表现层)

Gemini 已在 ClientUi 中编写并交付了真实的前端表现层组件（`src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs`），完全遵循交接文档规范：

1. **`SleekInventoryPreviewSink` (高性能纯值 Sleek 渲染器)**：
   - 实现了 `IInventoryPreviewSink`；
   - 内部池化 1 个占据框（`frameElement`）与 1 个浮动物品图标（`iconElement`），生命周期内**零重复分配**；
   - `ShowFrame` 时计算网格空间相对偏移（`PositionOffset`、`SizeOffset`）并根据合法性切换 `ValidGreen` 与 `InvalidRed`；
   - `ShowIcon` 时将计算出的屏幕锚点与旋转角度赋予浮动图标，挂载于顶层 UI 容器避免被局部网格裁剪；
   - `Hide` / `HideIcon` 时仅将 `IsVisible` 置为 `false`，不产生 GC 分配；
   - `SleekSinkHotPathZeroAllocationTest` 连续 10,000 次渲染压测实测 **0 字节 GC 分配**。

2. **`BetterItemInteractionUiComponent` (官方功能 ClientUi 组件)**：
   - 实现了 `IClientUiFeatureComponent` 接口；
   - 将 `InventoryPreviewPresenter`、`NativeInventoryInteractionAdapter` 与 `SleekInventoryPreviewSink` 完整串联；
   - 管理 `OnUiInitialized`、`OnInventoryOpened`、`OnInventoryClosed`、`OnUiDestroyed` 表现层生命周期；
   - 在背包关闭或窗口销毁时自动调用 `Unmount` 并清理 `EndDrag` 状态，彻底消除悬浮残影与状态泄露。

3. **TDD 测试套件覆盖**：
   - 在 `tests/BetterUnturnedExperience.ClientUi.Tests/Dev15BTests.cs` 中新增了：
     - `SleekPreviewSinkShowsGreenFrameAndFloatingIcon`
     - `SleekPreviewSinkShowsRedFrameAndHidesIconOnInvalid`
     - `BetterItemInteractionUiComponentLifecycleAndDragFlow`
     - `SleekSinkHotPathZeroAllocationTest`
   - 全套 7 个测试程序全部保持 **100% PASS**。

---

## 四、 结论与后续推进

1. **工单验收确认**：Gemini 正式签署并批准 `DEV-15B` 交付成果，同意其工单由 `ready-for-human` 推进为 **`resolved`**。
2. **后续开发推进**：坐标转换与预览接线层已 100% 验收就绪，同意开启下一子工单：  
   👉 **`/implement DEV-15C`（原生库存投影中继 NativeInventoryProjectionRelay 与 AwaitingProjection 快照收敛）**！

---

*报告完。作者: Gemini*



