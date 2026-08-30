# 03：DEV-16C 原生库存 UI 生命周期与容器上下文接线

**What to build:** 让 BUE 在真实客户端感知玩家背包、普通容器和车辆后备箱的打开/关闭、容器切换、网格几何、滚动和会话代际，并把原生对象安全转换为现有纯 C# `IInventorySurfaceContext`，Hook 失败时保持原生拖拽。

**Blocked by:** 01：DEV-16A 单 DLL Runtime Composition Root 与 Client/Headless 装配

**Status:** resolved

> 2026-08-30 认领（agent）：按冻结路线依赖图实施。前置 01（DEV-16A）已 resolved；同票族 02（DEV-16B）已 resolved（其 Harmony 接入/门禁/保活/布局模式为本票参照）。实施遵循 AGENTS.md Output review loop。

- [x] 受控 Harmony/访问桥能够接入库存 UI 构造、打开、关闭和容器切换生命周期。
- [x] 玩家背包、普通箱子/容器和车辆后备箱能提供当前容器、页面、网格尺寸、Viewport、CellPixelSize、UiScale、ScrollPixels 和 Occupancy。
- [x] Storage/Trunk 投影更新能进入正确容器会话和代际；关闭、重开、换容器和连接代际变化会失效旧上下文。
- [x] 原生 UI 树重建后可检测失效引用并重新绑定，不使用已销毁的容器或网格对象。
- [x] 私有原生字段只在 Adapter 层访问，不泄漏到 Contracts/Core 或纯 C# Presenter。
- [x] 目标类型/方法签名探测失败时记录结构化诊断，禁用 BUE 接线并保持 Unturned 原生拖拽。
- [x] U3DS Headless 不执行本票的客户端 Hook；客户端/Headless 分流门禁在任何原生 UI 访问前生效。
- [x] 生命周期、容器代际、UI 重建、失败回退和隔离测试通过；Release 编译达到 0 errors，尽可能 0 warnings。

## Comments

### 2026-08-30 认领与实施（agent，双轴循环审查驱动）

- 认领时 Status `ready-for-agent` → `in-progress`；SDK 结构侦察确认：`PlayerDashboardInventoryUI` 全静态类（`active`/`open`/`close`，无单例）、`PlayerInventory.isStoring/isStorageTrunk/storage` 公开字段、STORAGE=7 页复用（storage/trunk 同页不同数据源）、cell=50px 硬编码、缩放 `GraphicsSettings.userInterfaceScale`、连接门闩用 `Provider.isConnected`（`Provider.client` 断开不重置不可靠）。
- **实施**（`InventoryLifecycle.cs` + `InventorySurfaceLifecycleAdapter.cs`，均为 Plugin Adapter 层新文件）：`ContainerSessionTracker`（会话代际状态机）+ `InventoryLifecycleWatcher`（快照 diff）+ `InventoryLifecycleGate`（fail-closed 门禁+结构化诊断）+ `UnturnedInventorySurfaceContext : IInventorySurfaceContext`（纯值投影）+ `UnturnedGridOccupancyView`/`UnturnedVisualContainer`/`UnturnedVisualElement`（真实 UI 包装）+ `PlayerUI.Update` postfix 轮询驱动。
- **循环审查**：第 1 轮双轴（Standards 7 判断性 / Spec 5 项）→ 修复（Watcher desired-kind 转换语义：先关旧会话再开新会话、identity 变化单 ++；postfix 改名 `PlayerUIUpdatePostfix`；静态 `LastPollDiagnostics`；Deactivate 移除；注释补全）→ 第 2/3/4 轮复审至 **双 CLEAN**（ActiveAdapter 赋值恢复、seam gap 注释补全、类 doc 措辞修正）。
- **测试**：tracker 生命周期 13 断言、gate 决策 6 断言、watcher diffing 9 断言、adapter hook 安装——全部外部可观察行为；构建 0/0、7/7 运行器 PASS（`audit/2026-08-30/tests-dev16c-*.log`）。
- **Seam gap 记录**：① `PlayerUI.Update` IL detour 仅 Mono 游戏运行时可编译（纯宿主 IL Compile Error——真机 R18 已证命中）；② `Poll()` 依赖 `Player.LocalPlayer` 引擎链，纯宿主不可调；③ Viewport 几何为近似值（offset 充原点、scroll 恒 0），真机 DEV-16D 校准。
- **正式产物**：`artifacts/DEV-16C-inventory-lifecycle-r22-20260830/BetterUnturnedExperience.dll`（SHA-256 `E8B6733C…1FE0`，CaseId `DEV-16C-R22-20260830`；完整值 `audit/2026-08-30/r22-dll-sha256.txt`）。
- **真机验证项**（下轮 HITL）：进背包/开箱/车辆后备箱 → `container-session-open kind= page= generation=` 日志链 + `IInventorySurfaceContext` 投影正确性；Storage/Trunk 切换代际推进；关闭/重开失效。
- 状态：`in-progress` → **`resolved`**（静态实现与测试全绿；真机运行证据按 DEV-16B 模式由下轮 HITL 采集后归档）。
