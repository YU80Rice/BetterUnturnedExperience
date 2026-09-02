# 交付报告 — DEV-16F R2：拿起源解耦补全（AREA 地面 + 装备槽 0/1）

> 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-16F-source-decouple-page-expansion.md`（R2 记录已追加）
> 阶段：R2 修正（实机复测反馈驱动）—— 双轴 CLEAN，待实机复测确认后关闭
> Release DLL：`audit/2026-09-02/artifacts/DEV-16F-R2-20260902/BetterUnturnedExperience.dll` · SHA-256 `332C51A1D1A893A5732DB3F51FF7E7B45ADF8A31A7D88F8EB8CF00BC035C86E3`（234496 bytes）
> 性质：功能修复候选（非发布授权/Stable；实机三环境证据后另行裁决）

## 1. 实机复测反馈（用户 2026-09-02，UMM 诊断包 `UMM-诊断包_20260902_185213`）

- ✅ 成功：默认物品栏、上衣、背心、裤子、背包、容器、后备箱（网格页 2-7）拿起源均有强化预放置渲染。
- ❌ 失败：**从"附近的物品"（地面 AREA=8）以及手持物品（热键栏 1/2 栏位 = 装备槽页 0/1）拿起拖入网格时，不触发预放置渲染与自动旋转**。

## 2. 根因（日志证据 + 代码事实）

| 证据 | 内容 |
|---|---|
| 日志 generation 2/6/8 | `enhanced=False` + `placement-passthrough reason=enhanced-off` |
| 日志 generation 3/5/7/9 | `enhanced=True` + `state=Candidate` + `preview-visible` |

`OnDragStarted` 的 `dragSourcePassThrough = !IsSupportedEnhancedPage(source.Page)`（仅页 2-7）→ 源 8/0/1 时整次拖拽被置 Pass-Through、BUE 不接管；`NativeInventoryInteractionAdapter.IsEnhancedSourcePage` = `IsOrdinaryGrid`（仅 2-7）→ release 提交门同样排除。切片 A 的"任何页面拿起都进入 BUE 拖拽流"未贯彻到 AREA 与装备槽。

## 3. R2 修复内容

| 变更 | 位置 | 说明 |
|---|---|---|
| 源门解耦 | `ItemInteractionUiComponent.cs` `OnDragStarted` | `dragSourcePassThrough = !nativeAdapter.IsEnhancedSourcePage(source.Page)` |
| 源谓词 | `NativeInventoryInteractionAdapter.cs` `IsEnhancedSourcePage` | `page <= areaPage`（0-8 全部有效拿起源；越界页 >8 Pass-Through） |
| AREA 提交路由 | 同上 `HandleRelease` | `source.Page == areaPage` → `TakeGroundItem(target)`；其余源 → `SendDragItem` |
| 红测 | `Plugin.Tests/Program.cs` | 新增 `--dev16f-area-source-red`（AREA→BACKPACK candidate seam）+ `--dev16f-equip-source-red`（页 0 源→BACKPACK）；并入全套 |
| 矩阵测试更新 | ClientUi `Program.cs` + `Dev15DTests.cs` | AREA/装备源从"Pass-Through"改为"经正确原生端口提交（Submitted）"；首门 Pass-Through 用例改用越界页 9 |

## 4. 关键裁定（research，`research/DEV-16F-source-area-equipment-research.md`）

- **AREA 源必须走 `TakeGroundItem`，绝不能走 `sendDragItem`**：服务端 `ReceiveDragItem` 以 `page_0 >= PAGES-1` 拒绝（`PlayerInventory.cs:722`），否则物品凭空消失。BUE `NativeDragActions.TakeGroundItem`（`InventoryDragPreviewAdapter.cs:966-974`）与原生 `ItemManager.takeItem` 同形。
- **装备源 0/1 走 `sendDragItem`**：服务端接受源 0/1 并在 `page_0 < SLOTS` 时 `sendSlot` 卸装（`ReceiveDragItem:786-789`）；目标为网格时 `rot_1` 不被强制置 0（仅 `page_1 < SLOTS` 置零）。
- 两例目标均为受包装网格页 2-7，BUE 判 Pass-Through 时转发原生 handler，无 vanilla 回归风险。

## 5. 红测 → 绿（TDD）

| 红测锚点 | 修复前 | 修复后 |
|---|---|---|
| `--dev16f-area-source-red`（AREA=8 源 → BACKPACK 目标 candidate seam） | 🔴 exit 1（`dragSourcePassThrough=true`） | ✅ exit 0 |
| `--dev16f-equip-source-red`（装备槽页 0 源 → BACKPACK 目标） | 🔴 exit 1 | ✅ exit 0 |

## 6. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 PASS |
| 四个 DEV-16F 红测锚点 | 全 PASS（source-decouple / target-vest / area-source / equip-source） |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 7. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | S1 注释（AREA/装备"stay native pass-through"现为"as targets"——已在本轮修正）；S2 新红测未走真实 `RebuildForDrag(sourceContainer=default)` 路径（静态分析确认安全降级，`SameContainer` 在 SessionGeneration=0 时 fail-closed）；S3 页常量手维护（既有） |
| **Spec** | **CLEAN** | 无 | S1 `InventoryDragPreviewAdapter:827` 双重 stopDrag（既有；原生 `stopDrag` 幂等 `if(!isDragging) return`，已确认无害）；S2 装备源未禁用自动旋转（目标为网格时服务端接受任意 rot，`VO-16F-04` 待实机确认旋转语义）；S3 DEV-16D 要件 22 字面"AREA/装备 Pass-Through"指**目标**，R2 只改**源**，属工单内显式范围升级（符合 spec:96 独立工单机制）；S4 `TakeGroundItem` 对销毁的 interactable 抛异常依赖 guarded wrapper 兜底（既有，VO-16F-02 待实机） |

> Spec 轴逐点确认：切片 A 完成 ✓；AREA 源提交路由与 SDK `takeItem` 同形 ✓；装备源提交与 SDK `ReceiveDragItem` 语义一致 ✓；AREA/装备作目标仍 Pass-Through ✓；源门 0-8 / 目标门 2-7 拆分连贯 ✓；红测修复前确红 ✓；既有测试按新契约更新、无残留旧断言 ✓。

## 8. 交付边界

- 本报告与源码提交为 **DEV-16F R2 修正交付**；发布授权不在此列。
- 实机复测确认点：地面拿起→网格、热键栏拿起→网格、AREA 拖出不变、装备槽目标不变、装备源旋转语义（`VO-16F-04`）、TakeGroundItem 销毁兜底（`VO-16F-02`）。
- 可延后项已全部列名（Standards S1/S2/S3 + Spec S1/S2/S3/S4），不阻断。
