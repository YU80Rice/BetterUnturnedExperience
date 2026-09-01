# DEV-16D 当前实现状态冻结快照

> FreezeId: `DEV-16D-FREEZE-20260901-1048`
> 记录性质：只读审计结果的仓库化冻结记录；不代表功能已完成或运行资格已通过。
> 责任变更：相关前端工作原由 Gemini 负责，现由 GPT 接手维护与后续修复。

## 1. 基线与身份

| 项目 | 固定值 |
|---|---|
| 分支 | `master` |
| 稳定审查基线 | `43d05ef91bf61f87dcba38774f78dcc9b4b60bc3` |
| 当前 HEAD | `c1919c4a705304227e68cc6f7decfbe1c839f8d4` |
| DEV-16D 最近实现提交 | `b698562f5d3fb4cff56b1eecd665b28a4548269f` |
| U3-SDK HEAD | `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb` |
| UnturnedPluginManager HEAD | `9b75730a6240c9e1c41d7ddb492b176380d5904c` |
| 官方 FeatureId | `io.github.yu80rice.bue.better-item-interaction` |

工作区存在大量历史 `.scratch`、源码、构建缓存和产物变更；本快照提交只允许包含本次列出的文档文件，不得吸收其它工作区修改。

## 2. 当前实现已对齐的原生 seam

- 反射读取 `SleekItems.horizontalScrollView`、`grid`、`itemsPanel`，并校验 `scroll → grid → itemsPanel` 父链。
- 通过 `PlayerUI.Update` postfix 进行主线程 surface/drag 轮询；Harmony 失败时保持功能级隔离和原生回退策略。
- 占据框挂到 `itemsPanel` 内容树；浮动物品图标挂到顶层 `PlayerUI.container`。
- 复用原生 `dragPivot`，并在受控范围包装公开 `SleekItems.onPlacedItem` delegate；支持幂等 attach、可逆 detach。
- `GridContentLocal` 指针坐标不重复应用 scroll；Screen/Viewport 路径只补偿一次。
- 普通网格合法放置沿原生 `sendDragItem`；快捷槽、装备、AREA、拖出、未知页面保留 Pass-Through。
- UI 关闭、重建、容器切换、代际失配和功能隔离具备清理路径；Headless 不创建 ClientUi。

## 3. 当前阻断项与覆盖记录

### B1：占据事实源错误且存在双真相

位置：`src/BetterUnturnedExperience.Plugin/InventorySurfaceLifecycleAdapter.cs:149-166`。

当前 `UnturnedGridOccupancyView.IsOccupied` 以 `y * width + x` 读取 `Items.items`。U3-SDK 的 `Items.items` 是压缩的 `List<ItemJar>`，只记录物品左上角；真实占据由 `ItemJar.x/y`、`rot`、资产尺寸和 footprint/`slots[,]` 决定。`InventoryDragPreviewAdapter.IsSwapOntoOccupied` 又自行遍历 footprint，形成两套占据算法。

影响：预览、自动旋转、同页移动和 native swap guard 可能对同一格产生不一致结果；当前不能证明与原生 `checkSpaceDrag/findIndex` 等价。

### B2：玩家网格覆盖事实（范围记录，非当前实现阻断）

位置：`InventorySurfaceLifecycleAdapter.cs:894-976,991-1052`、`BetterUnturnedExperiencePlugin.cs:79-87`。

当前 surface 选择固定为 `PlayerInventory.BACKPACK`，另覆盖共享的 Storage/Trunk page 7；Hands、Vest、Shirt、Pants page 2/4/5/6 尚未建立同等增强接线。U3-SDK 确实创建了这些页面，但 DEV-16D 规格已明确装备页保持原生 Pass-Through（`spec-DEV-16-runtime-clientui-management-panel.md:141`）。因此这是实现覆盖事实和未来需求入口，不是当前 DEV-16D 的实现阻断；若未来要增强装备页，应另立范围变更。

## 4. 证据等级

- `SOURCE_CONFIRMED`：U3-SDK 的 native hierarchy、`grid.OnClicked`、`onPlacedItem`、`dragPivot`、旋转、图标刷新、Storage/Trunk 生命周期和 `Items` footprint 语义。
- `STATIC_REVIEW_CONFIRMED`：R12 增量修复的 Standards/Spec 审查文件均记为 CLEAN；这只覆盖该增量，不消除 B1 既有差异。
- `BUILD/TEST_CONFIRMED`：R12 Release 0 errors/0 warnings、7 个测试运行器 PASS、静态 UI/native token 门禁 PASS，证据见 `audit/2026-09-01/`。
- `RUNTIME_UNVERIFIED`：真实客户端的颜色、z-order、viewport 裁剪、raycast、真实纹理刷新、所有页面接线、旋转稳定性及三环境资格。

正式 R12 候选 DLL 仅作为历史静态候选：`audit/2026-09-01/artifacts/BetterUnturnedExperience.dll`，SHA-256 `142AC39F769EF23EE75406F3DE84F21B59597B8F1BA72191650B128CF624C5E4`。不得把它当作 B1 修复后的产物，也不得把它解释为已扩大页面范围的产物。

## 5. 冻结的下一步开发方向

必须按以下顺序推进，且每步遵循红测 → 最小修复 → 绿测 → 全量验证 → Standards/Spec 双轴审查：

1. 提取唯一 `ItemJar footprint → occupancy snapshot` seam；preview 与 swap guard 共用，并排除当前拖拽物品自身来源 footprint。
2. 以现有规格冻结受支持页面矩阵：Backpack 与 Storage/Trunk 继续增强，Hands、Vest、Shirt、Pants、AREA 和其它未裁决页面保持原生回退；若产品要扩大范围，先创建独立需求变更，再补充页面映射与测试。
3. 补齐真实物品资产 quality/state 传递和 native icon 刷新验证（仅在需要时，避免扩大范围）。
4. 运行 Release 编译、全套测试、静态门禁；重新派发全新的 Standards + Spec 审查。
5. 双轴 CLEAN 后才生成新 DLL、SHA-256、CandidateBuild 和 CaseId；随后再安排真实客户端验证。

## 6. 禁止事项

- 不把本快照或 R12 静态 CLEAN 写成玩法通过。
- 不修改 U3-SDK、Unturned 原生库存权威或建立平行库存 RPC。
- 不把 `Glazier.Root` 替换为 BUE 根、不覆盖 grid 命中层、不把 frame 挂到顶层、不把 icon 挂到 `itemsPanel`。
- 不使用 `Items.items[index]` 作为逐格 occupancy。
- 不在未完成双轴审查前发布新的候选 DLL；不继承旧 CaseId/哈希运行证据。

## 7. 相关记录

- 原生调研：`research/U3SDK-inventory-rendering-injection-research.md`
- DEV-16D 工单：`issues/04-dev-16d-drag-preview-native-projection.md`
- DEV-16D 规格：`spec-DEV-16-runtime-clientui-management-panel.md`
- R12 审计：`audit/2026-09-01/RuntimeFix-DEV16D-0137.md`
