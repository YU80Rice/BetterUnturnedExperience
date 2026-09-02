# 交付报告 — DEV-16F 拿起源解耦 + 目标页扩展（VEST/SHIRT/PANTS/Hands）

> 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-16F-source-decouple-page-expansion.md`
> 阶段：实现交付（红测→绿 → 双轴 CLEAN → 提交）—— 实机三环境复测另行安排
> 源码提交：待提交（本报告随实现提交落盘）
> Release DLL：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` · SHA-256 `D96B96D2989820E8BE3EA6549F0D9276169C6C72B4D8AB49089F1AC34E4CEE8A`（234496 bytes）
> 性质：DEV-16F 功能实现（非发布授权/Stable；实机三环境证据后另行裁决）

## 1. 背景与目标

DEV-16D（Backpack/Storage 增强）实机确认后，用户 2026-09-02 提出体验缺口：**从上衣/背心/裤子/手中拿起物品拖入背包等网格时，没有强化预放置渲染**——因为拿起源页面不在 `{3,7}` 时整次拖拽被置为 Pass-Through，BUE 完全不接管。

用户期望覆盖页面：手中的物品（默认物品栏）+ 背心 + 上衣 + 裤子 + 背包 + 容器界面 + 后备箱 = U3-SDK 页 `{2,3,4,5,6,7}`。

## 2. 根因（代码事实）

五道门控全部硬编码 `{3,7}`：
| 门控 | 位置（修复前） |
|---|---|
| 源页总闸（预览接管） | `ItemInteractionUiComponent.OnDragStarted` L576 `dragSourcePassThrough = !IsSupportedEnhancedPage(source.Page)` |
| 源页提交 | `NativeInventoryInteractionAdapter.HandleRelease` L70 `IsEnhancedSourcePage` |
| 目标页提交 | 同上 L76 `IsOrdinaryGrid` |
| attach 门控 | `InventoryDragPreviewAdapter` L274 `IsSupportedPage` / `SupportedPages` |
| surface dispatch | `InventorySurfaceLifecycleAdapter` L805 `SupportedSurfacePages` |

## 3. 范围裁定（Spec 轴确认，含对原风险注的更正）

- 目标/源页集合 = `{2,3,4,5,6,7}`（Hands/Backpack/Vest/Shirt/Pants/Storage+trunk）。
- **更正原工单风险注**：U3-SDK 实证（`PlayerInventory.cs:SLOTS=2`；`PlayerDashboardInventoryUI.cs:2627 slots = new SleekSlot[PlayerInventory.SLOTS]` 只覆盖 Primary/Secondary 页 0/1；`2800 items = new SleekItems[PAGES - SLOTS]` 即页 2–8 每页一个 SleekItems）证明 **页 2 Hands 是 `items[0]` 普通 SleekItems 网格（5×3），不是 SleekSlot**。因此 Hands(2) 按用户清单"手中的物品（默认物品栏）"纳入增强集合。
- AREA(8) 与装备槽(0/1) 保持原生 Pass-Through（边界不变；用户清单不含 AREA 拖出语义）。

## 4. 变更清单

| 文件 | 变更 |
|---|---|
| `src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs` | `SupportedLiveSurfacePages` → `{2..7}`；`IsSupportedEnhancedPage` → `page>=2 && page<=7`；指针路由注释更新（Hands 最早页优先） |
| `src/BetterUnturnedExperience.ClientUi/NativeInventoryInteractionAdapter.cs` | 删除 `backpackPage/storagePage` 字段；`IsOrdinaryGrid` → `page>=slotsPageBoundary && page<areaPage`（构造参数 2,8 ⇒ `{2..7}`）；`IsEnhancedSourcePage` 同（源页解耦） |
| `src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs` | `SupportedPages`/`IsSupportedPage` → `{2..7}`（新增 HandsPage/VestPage/ShirtPage/PantsPage 常量） |
| `src/BetterUnturnedExperience.Plugin/InventorySurfaceLifecycleAdapter.cs` | `SupportedSurfacePages` → `{2..7}`；生命周期 kind 映射：仅页 7（STORAGE）取会话 kind，页 2–6 恒 `PlayerInventory`（原 `BACKPACK?` 推广） |
| `tests/BetterUnturnedExperience.ClientUi.Tests/Dev15DTests.cs` | 矩阵测试更新为六网格页增强 + 装备槽/AREA 排除 |
| `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` | 新增 `AssertDev16FSourceDecoupleReachesCandidateSeam` + `AssertDev16FTargetVestEnhancedPreview`，CLI 红测锚点 + 全套调用 |
| `CONTEXT.md` | 词条"玩家物品栏网格"/"普通容器网格"更新为页 2–6 / 页 7 语义 |

## 5. 红测 → 绿（TDD）

| 红测锚点 | 修复前 | 修复后 |
|---|---|---|
| `--dev16f-source-decouple-red`（SHIRT(5) 源 → BACKPACK(3) 目标到达 candidate seam） | 🔴 exit 1（`dragSourcePassThrough=true` → Hidden） | ✅ exit 0 |
| `--dev16f-target-vest-red`（VEST(4) 注册/attach/预览） | 🔴 exit 1（页 4 未注册/未 attach） | ✅ exit 0 |

两个红测均先于修复运行并失败（stash 语义符合 real-machine-test-loop.md 第 24-25 行"红测必须先于修复运行并失败"），修复后转绿且并入全套测试。

## 6. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建（Plugin 单 DLL） | 0 errors / 0 warnings，退出码 0 |
| 七项目测试运行器 | 全 PASS（Contracts / Settings / Placement / ClientUi / Network / Release / Plugin） |
| UI/native token 扫描（Core + ClientUi） | 零命中 |
| `git diff --check` | 退出码 0 |
| Placement 零分配测试 | PASS（`PlacementCandidateEvaluator` 未改动，§11 无回归） |

## 7. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | S1 六处门控手维护可共享单一常量来源；S2 `EvaluatePlacement` 沿用冻结公式（语义等价 `{2..7}`）；S3 页面表注释三处重复 |
| **Spec** | **CLEAN** | 无 | S1 工单 slice-B 原文 `{3,4,5,6,7}` 与用户清单含 Hands 的措辞已在本记录/工单对齐为 `{2..7}`；S2 纯 C# 红测不覆盖服装页格内几何（实机 VO 为准）；S3 提交门防御性不变式 |

> Spec 轴逐点确认：切片 A 源页解耦语义 ✓（`dragSourcePassThrough` 对网格源页不再触发，渲染由目标网格决定）；切片 B 五门控全部 `{2..7}` ✓；AREA/装备槽 Pass-Through 边界 ✓；`InventorySurfaceLifecycleAdapter` 页 2–7 dispatch + `BuildSurfaceContext` 对 0×0 页（未装备服装）返回 null ✓；`ReadDashboardSleekItems` 索引 `page-SLOTS` 落在 `items = new SleekItems[PAGES-SLOTS]`（7 项）内 ✓；无 RPC/无客户端库存写/原生权威不变 ✓；§11 边缘感应带/D2 由 page-agnostic 评估器保持 ✓。

## 8. 交付边界

- 本报告与源码提交为 **DEV-16F 实现阶段交付**；`TechnicallyQualified` 与发布授权不在此列。
- 实机复测（单人 + SteamP2P Host/Client + U3DS Headless）需在双轴 CLEAN 后进行，覆盖页 2–7 拿起/放下、AREA 拖出不变、§11 行为不回归；完成后关闭工单并冻结快照。
- 可延后项已全部列名，不阻断；Standards S1/S2/S3 与 Spec S1/S2/S3 如后续处理将单独记录。
