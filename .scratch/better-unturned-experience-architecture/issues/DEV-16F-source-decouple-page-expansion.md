# DEV-16F：增强拖入拿起源解耦 + 目标页扩展（VEST/SHIRT/PANTS）

Type: task
Status: resolved（已实现、双轴审查 CLEAN、提交；待实机三环境复测）
Parent: 04：DEV-16D 拖拽预览、真实图标、原生提交与投影收敛
Blocked by: DEV-16E（已解除 —— DEV-16E 已三环境资格关闭，本工单独立实现）

## 背景（用户 2026-09-02 实机反馈）

R7-ROTGRAB 修复后功能正常，但暴露体验缺口：

> "这个物品的强化渲染需要把物品放进有强化渲染的物品栏以后，在有强化渲染的物品格子内才有渲染。比如说现在我从上衣里拿一个物品，把它放进背包的过程是没有强化渲染的。我想的强化渲染方式是选中的物品移动到物品栏内就有这种预放置的渲染和功能。"

用户期望覆盖页面：**手中的物品（默认物品栏）+ 背心 + 上衣 + 裤子 + 背包 + 容器界面 + 后备箱**。

## 根因（已定位，代码事实）

`ItemInteractionUiComponent.OnDragStarted`（L576）：
```csharp
dragSourcePassThrough = !IsSupportedEnhancedPage(source.Page);
```
拿起源页面（如 SHIRT=5 / VEST=4 / PANTS=6 / SLOTS=2 / AREA=8）不在 `{3,7}` 时，整次拖拽被置为 Pass-Through，BUE 完全不接管 → 无预放置渲染。只有从 Backpack(3)/Storage(7) 拿起才被接管。

五道门控全部硬编码 `{3,7}`：
| 门控 | 位置 |
|---|---|
| 源页总闸（预览接管） | `ItemInteractionUiComponent.OnDragStarted` L576 |
| 源页提交 | `NativeInventoryInteractionAdapter.HandleRelease` L70 `IsEnhancedSourcePage` |
| 目标页提交 | 同上 L76 `IsOrdinaryGrid` |
| attach 门控 | `InventoryDragPreviewAdapter` L274 `IsSupportedPage` / `SupportedPages` |
| surface dispatch | `InventorySurfaceLifecycleAdapter` L805 `SupportedSurfacePages` |

## 方案（拆分两个垂直切片）

### 切片 A：拿起源解耦（解决核心体验）
- `dragSourcePassThrough` 不再由源页决定；**任何页面拿起都进入 BUE 拖拽流，渲染由目标网格是否受支持决定**。
- 语义：源页只决定"来源容器快照/来源 footprint 排除"，不再决定"是否接管"。
- 约束：AREA 作为源时保持原生拾取路径？还是也增强？→ 需 spec 裁决（用户清单含"手中/背心/上衣/裤子"但不含 AREA 拖出语义）。
- 风险：SLOTS(2) 是 `SleekSlot` 槽位而非网格，拿起/放下语义与网格不同，需单独处理。

### 切片 B：目标页扩展（VEST/SHIRT/PANTS）
- `SupportedPages`/`IsSupportedEnhancedPage`/`SupportedSurfacePages`/`IsOrdinaryGrid` 扩展到 `{3,4,5,6,7}`（背包+背心+上衣+裤子+容器/后备箱）。
- VEST/SHIRT/PANTS 与 BACKPACK 同为 `SleekItems` 网格、同一 50px 语义（U3-SDK 研究已确认），低风险；需按三环境验证手册同流程验证。

## 红测锚点（实现阶段新增）

- `--dev16f-source-decouple-red`：SHIRT(5) 源 + BACKPACK(3) 目标 → 必须到达 candidate seam（修复前红：dragSourcePassThrough=true → Hidden）。
- `--dev16f-target-vest-red`：VEST(4) 网格 → 增强预览生效（修复前红：未 attach）。
- D2/边缘感应带行为在 4/5/6 页保持与 3/7 一致（复用 spec §11）。

## 边界

- 不改变 AREA 拖出/丢弃语义（除非 spec 裁决）；
- 不改变原生库存权威、不新增 RPC、不写客户端库存；
- 本工单在 DEV-16E 三环境证据采集与人工批准前不启动实现。

## 2026-09-02 立项记录

用户拍板：新功能作为功能优化，放在 DEV-16E 三环境实现之后。DEV-16D 父工单已按当前范围（Backpack/Storage/Trunk 增强 + 其它页 Pass-Through）关闭；本工单为其明确的"扩大范围"需求变更（符合 spec-DEV-16D "若未来要增强装备页或 AREA，必须另立需求变更和独立工单" L96）。

## 2026-09-02 实现记录（DEV-16F 已交付）

### 范围裁定（Spec 轴确认）

- 目标/源页集合 = `{2,3,4,5,6,7}`（Hands/Backpack/Vest/Shirt/Pants/Storage+trunk）。
- **更正原风险注**：U3-SDK 证实 `SLOTS(2)=Hands` 是 `SleekItems items[0]` 普通网格（5×3），**不是** SleekSlot；`SleekSlot[] slots` 只覆盖 Primary/Secondary（页 0/1）。故 Hands(2) 按用户清单"手中的物品（默认物品栏）"纳入增强。
- AREA(8) 与装备槽(0/1) 保持原生 Pass-Through（边界不变）。

### 变更清单（红测 → 绿）

- [x] **切片 A 拿起源解耦**：`IsSupportedEnhancedPage` 从 `{3,7}` → `page>=2 && page<=7`，`OnDragStarted` 的 `dragSourcePassThrough` 对网格源页不再触发；渲染改由目标网格支持与否决定。源页只决定来源容器快照/footprint 排除。
- [x] **切片 B 目标页扩展**：五道门控全部 `{2,3,4,5,6,7}` —— `SupportedLiveSurfacePages`（ClientUi）、`IsSupportedEnhancedPage`（ClientUi）、`IsOrdinaryGrid`/`IsEnhancedSourcePage`（NativeAdapter）、`SupportedPages`/`IsSupportedPage`（DragPreviewAdapter）、`SupportedSurfacePages`（SurfaceLifecycleAdapter）。
- [x] **生命周期 kind 映射修正**：仅页 7（Storage/trunk）取会话 kind；页 2–6 恒为 `PlayerInventory`（原 `BACKPACK?` 判断已推广）。
- [x] **红测锚点**：`--dev16f-source-decouple-red`（SHIRT(5) 源 → BACKPACK(3) 目标到达 candidate seam；修复前红 exit 1）+ `--dev16f-target-vest-red`（VEST(4) 注册/attach/预览；修复前红 exit 1），已并入全套测试。
- [x] **既有矩阵测试更新**：`Dev15DTests.SourcePagePassThroughMatrixIsExplicit` 改为六网格页 + 排除装备槽/AREA。
- [x] **边界**：不改变 AREA 拖出/丢弃语义；不新增 RPC；不写客户端库存；原生权威不变；§11 边缘感应带/开阔区 D2 由 page-agnostic 的 `PlacementCandidateEvaluator` 保持（未改动该文件）。

### 验证

- [x] 红测先红（两个 `--dev16f-*-red` 修复前 exit 1）后绿（exit 0）。
- [x] Release 构建 0 errors / 0 warnings；七项目测试全 PASS；UI/native token 扫描零命中；`git diff --check` 通过。
- [x] 双轴独立审查（Standards + Spec，并行子代理）：两轴 **CLEAN**，无阻断项。可延后项已列名（Standards S1 门控六处手维护可共享常量、S2 `EvaluatePlacement` 沿用冻结公式、S3 注释三处重复；Spec S1 工单 slice-B 原文 `{3,4,5,6,7}` 与用户清单含 Hands 的措辞已在本记录对齐为 `{2..7}`、S2 纯 C# 红测不覆盖服装页格内几何、S3 提交门防御性不变式）。
- [x] 待办：实机三环境复测（单人 + SteamP2P + U3DS）后关闭；本工单实现阶段在双轴 CLEAN 后提交。

## 2026-09-02 实机复测 → R2 修正（拿起源解耦补全 AREA + 装备槽）

用户实机复测（UMM 诊断包 `UMM-诊断包_20260902_185213`）确认：网格页拿起源（默认物品栏/上衣/背心/裤子/背包/容器/后备箱）预放置渲染全部生效；但**从"附近的物品"（地面 AREA=8）以及手持物品（热键栏 1/2 栏位，页 0/1 装备槽）拿起拖入网格时不触发预放置渲染与自动旋转**。

日志证据：generation 2/6/8 `enhanced=False` + `placement-passthrough reason=enhanced-off`，网格源页 generation 3/5/7/9 全部 `enhanced=True` + `Candidate`。

### R2 根因

切片 A 的"任何页面拿起都进入 BUE 拖拽流"没有贯彻到 AREA 与装备槽源：
- `OnDragStarted` 的 `dragSourcePassThrough = !IsSupportedEnhancedPage(source.Page)`（`IsSupportedEnhancedPage` 仅页 2-7）→ 源 8/0/1 时整次拖拽 Pass-Through、BUE 不接管。
- `NativeInventoryInteractionAdapter.IsEnhancedSourcePage` = `IsOrdinaryGrid`（仅 2-7）→ release 提交门同样排除 AREA/装备槽源。

### R2 修复（research 裁定支撑，`research/DEV-16F-source-area-equipment-research.md`）

- [x] 源门解耦：`OnDragStarted` 改为 `dragSourcePassThrough = !nativeAdapter.IsEnhancedSourcePage(source.Page)`，`IsEnhancedSourcePage` 改为 `page <= areaPage`（0-8 全部为有效拿起源）；仅越界页（>AREA）保持 Pass-Through。渲染由目标网格决定（仅 2-7 为 live surface）。
- [x] AREA 源提交路由：`HandleRelease` 对 `source.Page == areaPage` 走 `TakeGroundItem`（原生 `ItemManager.takeItem` 同形），其余源走 `SendDragItem`。**关键**：AREA 源绝不可走 `sendDragItem`——服务端 `ReceiveDragItem` 以 `page_0 >= PAGES-1` 拒绝，物品会凭空消失（U3-SDK `PlayerInventory.cs:722`）。
- [x] 装备槽源提交：走通用 `sendDragItem(0/1 → grid)`，服务端接受源 0/1 并在 `page_0 < SLOTS` 时 `sendSlot` 卸装（U3-SDK `ReceiveDragItem:786-789`）。目标为网格（2-7）时服务端不强制 rot=0（`page_1 < SLOTS` 才置零）。
- [x] 红测锚点：`--dev16f-area-source-red`（AREA=8 源 → BACKPACK 目标到达 candidate seam；修复前红 exit 1）+ `--dev16f-equip-source-red`（装备槽页 0 源 → BACKPACK 目标；修复前红 exit 1），并入全套测试。
- [x] 既有矩阵测试更新：ClientUi `RunNativeDragAdapterTests`、`SourcePagePassThroughMatrixIsExplicit`、`UnsupportedSourceWithStalePreviewRemainsNativePassThrough` 与 Plugin `AssertDev16DR13UnsupportedSourcePassThrough` 由"AREA/装备槽源 Pass-Through"更新为"有效源、经正确原生端口提交；仅越界页 Pass-Through"。
- [x] 边界保持：AREA 作为**目标**（拖出/丢弃）仍 Pass-Through（`IsOrdinaryGrid(target.Page)` 只接受 2-7）；装备槽作为目标仍原生。

### R2 验证

- [x] 红测先红后绿：`--dev16f-area-source-red` / `--dev16f-equip-source-red` 修复前 exit 1 → 修复后 exit 0。
- [x] Release 构建 0/0；七项目测试全 PASS；UI token 扫描零命中；`git diff --check` 通过。
- [x] research 裁定：AREA 源与装备槽源均可安全增强；无 vanilla 回归风险（BUE 判 Pass-Through 时转发原生 handler）。
- [ ] 待实机复测确认（R2 交付后）：地面拿起→网格、热键栏拿起→网格、AREA 拖出不变、装备槽目标不变。
