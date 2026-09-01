# DEV-16D 后续 Agent 交接

> FreezeId: `DEV-16D-FREEZE-20260901-1048`
> 原由 Gemini 负责，现由 GPT 接手。
> 目的：让新 Agent 在不依赖当前对话的情况下继续 DEV-16D 修复。

## 先读这些文件

1. `../snapshots/DEV-16D-implementation-state-freeze-20260901.md`
2. `../research/U3SDK-inventory-rendering-injection-research.md`
3. `../spec-DEV-16-runtime-clientui-management-panel.md`
4. `../issues/04-dev-16d-drag-preview-native-projection.md`
5. `../../../audit/2026-09-01/RuntimeFix-DEV16D-0137.md`

## 固定基线

- BUE branch: `master`
- Stable baseline: `43d05ef91bf61f87dcba38774f78dcc9b4b60bc3`
- Current HEAD: `c1919c4a705304227e68cc6f7decfbe1c839f8d4`
- Latest DEV-16D implementation: `b698562f5d3fb4cff56b1eecd665b28a4548269f`
- U3-SDK: `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`
- UPM: `9b75730a6240c9e1c41d7ddb492b176380d5904c`

## 已确认的接线方向

原生 UI 树是 `SleekItems → horizontalScrollView → grid → itemsPanel`。BUE 当前已经采用：

- `itemsPanel` 内容层挂占据框；`PlayerUI.container` 顶层挂浮动图标；
- `PlayerUI.Update` 主线程 heartbeat；
- 受控包装 `SleekItems.onPlacedItem`，并支持 detach；
- 原生 `dragPivot`、`sendDragItem` 和特殊分支 Pass-Through；
- UI 重建、容器代际、关闭和 Headless 隔离。

不要替换 `Glazier.Root`，不要覆盖 grid 命中层。

## 必须先修复的阻断

### 1. Occupancy

`src/BetterUnturnedExperience.Plugin/InventorySurfaceLifecycleAdapter.cs:149-166` 使用 `y * width + x` 访问 `Items.items`。这是错误的逐格模型。U3-SDK 的真实占据来自 `ItemJar.x/y + rot + asset size` 的 footprint/`slots[,]`。`InventoryDragPreviewAdapter.IsSwapOntoOccupied` 又有另一套 footprint 遍历，必须统一为单一占据快照 seam，并排除当前拖拽物品自身来源 footprint。

### 2. 网格覆盖事实

`InventorySurfaceLifecycleAdapter.cs:894-976,991-1052` 当前固定增强 Backpack，并覆盖共享 Storage/Trunk page 7。U3-SDK Dashboard 创建 page 2～8：Hands、Backpack、Vest、Shirt、Pants、Storage、Area。Hands/Vest/Shirt/Pants 尚未接入，但 DEV-16D 规格已明确装备页保持原生 Pass-Through（`spec-DEV-16-runtime-clientui-management-panel.md:141`）；这是覆盖事实和未来范围入口，不是当前实现阻断。AREA 必须继续原生回退。

## 开发顺序

1. 先写 occupancy 红测，确认错误 list-index 行为失败。
2. 提取/实现唯一 footprint occupancy seam，跑绿测。
3. 按现有规格保持 Backpack 与 Storage/Trunk 增强、装备页和 AREA Pass-Through；若未来扩大页面范围，先走独立需求变更和新工单。
4. Release 编译、全套测试、静态门禁。
5. 派发全新的 Standards + Spec 双轴审查；FAIL 则记录并循环修复，CLEAN 前不得交付 DLL。
6. CLEAN 后生成新 DLL、SHA-256、CandidateBuild、CaseId，再安排实机测试。

## 当前证据边界

- R12 静态候选 DLL：`audit/2026-09-01/artifacts/BetterUnturnedExperience.dll`，SHA-256 `142AC39F769EF23EE75406F3DE84F21B59597B8F1BA72191650B128CF624C5E4`。
- R12 通过只代表该增量静态审查/构建通过，不代表本冻结阻断已修复。
- 真实颜色、z-order、裁剪、raycast、纹理刷新、全页面接线和三环境资格仍未确认。

## 禁止事项

- 不修改 U3-SDK/Unturned 原生库存权威，不建立平行库存 RPC。
- 不把旧 CaseId/哈希运行证据继承给新修复。
- 不把编译成功、静态 CLEAN 或无报错写成玩法通过。
- 不标记 DEV-16D 为 `resolved`，直到阻断修复和双轴 CLEAN 完成。

## Suggested skills

- `$diagnosing-bugs`：若实机日志或新的运行链症状出现。
- `$tdd`：每个阻断先写红测，再做最小修复。
- `$implement`：按本交接顺序落地代码并闭环验证。
- `$code-review`：以本冻结提交为 fixed point，重新执行 Standards + Spec 双轴审查。
- `$handoff`：若转移到新工作区或新 Agent，再生成便携交接。
