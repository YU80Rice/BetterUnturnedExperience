# DEV-16F：增强拖入拿起源解耦 + 目标页扩展（VEST/SHIRT/PANTS）

Type: task
Status: ready-for-agent（阻塞于 DEV-16E 三环境资格之后）
Parent: 04：DEV-16D 拖拽预览、真实图标、原生提交与投影收敛
Blocked by: DEV-16E（三环境证据采集完成前不启动；新功能作为独立需求变更，不混入 DEV-16D 关闭）

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
