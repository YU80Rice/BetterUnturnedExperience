# V5-R3 容器页整理注入与权威现状

- **Ticket**: V5-R3
- **日期**: 2026-09-14
- **方式**: 只读对照一手源码与归档。未改生产代码、未构建、未提交。
- **范围**: T4 事实输入。不设计新按钮布局。

## 0. 一手来源

| 编号 | 来源 | 身份 |
|---|---|---|
| LIT-UI | `src/BetterUnturnedExperience.Lit/Ui/InventoryTidyUiPatch.cs` | 现网标题栏注入、STORAGE 排除注释 |
| LIT-HK | `src/BetterUnturnedExperience.Lit/Tidy/HotkeySnapshot.cs` | `TIDYABLE_PAGE_MIN/MAX` |
| LIT-WIRE | `src/BetterUnturnedExperience.Lit/Tidy/LitTidyWireCodec.cs` | `RequestTidy` 页范围解码 |
| LIT-NET | `src/BetterUnturnedExperience.Lit/Tidy/LitTidyNetService.cs` | 畸形包处理 |
| LIT-AUTH | `src/BetterUnturnedExperience.Lit/Tidy/LitTidyProductionAuthority.cs` | 权威端页门禁 |
| LIT-MOD | `src/BetterUnturnedExperience.Lit/InventoryTidyModule.cs` | `RequestTidy` / 本地路径 |
| LIT-LOCAL | `src/BetterUnturnedExperience.Lit/Tidy/LocalTidyExecutor.cs` | 主机本地执行 |
| LIT-SVC | `src/BetterUnturnedExperience.Lit/Tidy/ManualTidyService.cs` | `TidyPage` / `TidyAllPlayerPages` |
| LIT-RT | `src/BetterUnturnedExperience.Lit/LitRuntime.cs` | `AllPages = 0xFF` |
| U3-PINV | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInventory.cs` | 页常量、openStorage/openTrunk |
| U3-PDINV | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\UI\Player\PlayerDashboardInventoryUI.cs` | `headers[]`、STORAGE/AREA |
| U3-STOR | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Interactable\InteractableStorage.cs` | 开箱互斥、虚拟容器钩子 |
| U3-VEH | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Interactable\InteractableVehicle.cs` | 后备箱授权 |
| BII-SURF | `src/BetterUnturnedExperience.Plugin/InventorySurfaceLifecycleAdapter.cs` | BII 把 STORAGE 当活表面、AREA 原生 |
| BII-DRAG | `src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs` | AREA 透传 |
| LIT-V1 | `Archive/2-未闭环验证项目/LaunchInventoryTidy` git `22d828b` | v1.0.0 首次开源：注入 STORAGE |
| LIT-AUD20 | 同归档 `.audit/v2.0.0-static-audit-20260729/Codex-v2.0.0静态审计与指导报告-20260729.md` | V2 确定性拒 page 7 |
| LIT-HIST | `Archive/3-历史审计与开发日志/功能插件历史设计归档/LaunchInventoryTidy-一键整理.md` | v2.0.1 拆按钮交付 |
| P4-MAP | `.scratch/bue-v2-phase4-visual-experience/map.md` | 永久不变量「不注入 STORAGE」 |
| P4-SPEC | `.scratch/bue-v2-phase4-visual-experience/spec.md` | 同上 |
| P4-T8 | `.scratch/bue-v2-phase4-visual-experience/issues/08-t8-spec-closure.md` | Q71 永久不变量 |
| P4-T5 | `.scratch/bue-v2-phase4-visual-experience/issues/05-t5-lit-header-and-settings.md` | Q54 不含仓储栏 |
| CTX | `CONTEXT.md` | 整理按钮术语；未重开前不注入 |
| HB | `docs/BetterUnturnedExperience-Player-Handbook.md` | 玩家手册「不含仓储栏」 |

归档 git 现 `main` HEAD 已是验收候选（CHANGELOG 写明 v1.x–v3.x 冻结、仅 Git 可追溯）。v1.0 证据取自 `22d828b`（`feat: LaunchInventoryTidy v1.0.0 首次开源发布`），不是工作树 HEAD。

---

## 1. 现网注入哪些 headers / page；STORAGE 排除原文；旧常量已删

### 1.1 注入范围

`InventoryTidyUiPatch` Harmony postfix 挂在 `PlayerDashboardInventoryUI` 构造函数（LIT-UI `:37-38`）。循环上限 `HEADER_INJECT_COUNT = 5`（`:243`），`i=0..4`，`currentPage = (byte)(i + 2)`（`:514-516`）。每页 **1** 颗「整理」按钮，`Glazier.CreateButton()` 后 `AddChild` 到 `headers[i]`（`:531-577`）。

类头（`:16-23`）：「为 5 个标题栏（headers[0..4]，对应 Hands/Backpack/Vest/Shirt/Pants）各注入一颗『整理』按钮」；「Ctrl+左键按已保存的全局模式与方向整理全身（不含仓储栏）」。

| 常量 | 值 | 位置 |
|---|---|---|
| `HEADER_INJECT_COUNT` | `5` | LIT-UI `:243` |
| `TIDY_POS_OFFSET_X` | `-130f` | `:233` |
| `TIDY_SIZE_X` / `BTN_SIZE_Y` | `60f` | `:232,234` |
| `TOOLTIP_TIDY` | 「左键：整理当前栏；Ctrl+左键：按全局模式和方向整理全身（不含仓储栏）」 | `:247-248` |

原生页常量（U3-PINV `:64-71`）：`SLOTS=2` Hands、`BACKPACK=3`、`VEST=4`、`SHIRT=5`、`PANTS=6`、`STORAGE=7`、`AREA=8`。`headers` 长度 `PAGES - SLOTS + 3 = 10`（U3-PDINV `:2739`）。网格标题索引 = `page - SLOTS`。

| headers[i] | page | 物品栏 | 现网是否注入 |
|---|---|---|---|
| `[0]` | 2 | Hands | 是 |
| `[1]` | 3 | Backpack | 是 |
| `[2]` | 4 | Vest | 是 |
| `[3]` | 5 | Shirt | 是 |
| `[4]` | 6 | Pants | 是 |
| `[5]` | 7 | STORAGE（储物/后备箱） | **否** |
| `[6]` | 8 | AREA（地面拾取） | 否（循环不到） |
| `[7..9]` | 帽子/口罩/眼镜 | 否 | 否 |

原生 STORAGE 头：`headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS]`，车内改 `"Storage_Trunk"`，否则 `"Storage"`（U3-PDINV `:1757-1765`）。AREA 头始终可见（`:2753,2793`）。

页 0/1（主/副手槽）不在 `headers[0..4]` 循环内。帽子/口罩/眼镜是 `headers[7..9]`，同样不注入。

### 1.2 STORAGE 被排除的代码与注释原文

循环注释（LIT-UI `:510-511`）：

> 循环注入 5 颗按钮：headers[0..4] -> page 2..6（SLOTS..PANTS 服装页）
> v2.0.1：不再注入 STORAGE (headers[5])，因为 V2 协议不支持容器页整理。

块注释（LIT-UI `:236-240`）：

> 容器页（page=STORAGE=7，headers[5]）不注入（v2.0.1 起的既定裁决，DEV-V4-06 延续）：V2 协议仅支持 page 2..6 服装页；容器页涉及 InteractableStorage 生命周期、跨玩家并发、工坊虚拟容器等独立权限模型，不能与服装页共用同一套规则；注入会造成 UI 可点击但服务端确定性拒绝的误导性 UI。旧 STORAGE 布局常量已随本票退役删除。

类头「不含仓储栏」（`:23`）；点击处理「Ctrl 按下 -> …整理全身（不含仓储栏）」（`:612`）。

`HotkeySnapshotUtil`（LIT-HK `:55-58`）：「可整理的页范围（SLOTS=2 至 PANTS=6，不含 STORAGE=7 容器页）。PlayerInventory.SLOTS/PANTS 是 static readonly 不是 const，这里用硬编码值。」`TIDYABLE_PAGE_MIN = 2`，`TIDYABLE_PAGE_MAX = 6`。

### 1.3 旧 STORAGE 布局常量是否已删

**已删。** 现网 `InventoryTidyUiPatch` 只有服装页几何：`TIDY_POS_OFFSET_X=-130`、`TIDY_SIZE_X=60`、`BTN_SIZE_Y=60`（`:223-234`）。全文件零命中 `STORAGE_DIR_POS_OFFSET_X` / `STORAGE_TIDY_POS_OFFSET_X` / `[Obsolete]`。LIT 目录 `rg Obsolete|STORAGE_DIR_POS|STORAGE_TIDY_POS` 为空。

对照：v1.0 `22d828b` 的 `PlayerDashboardInventoryUIPatch.cs:74-88` 曾有 `STORAGE_DIR_POS_OFFSET_X = -285f`、`STORAGE_TIDY_POS_OFFSET_X = -240f`、`HEADER_INJECT_COUNT = 6`。V4-R2（2026-09-11）当时仍记「STORAGE 专用偏移常量标 `[Obsolete]`，仅历史参考」——那是 Phase-4 前快照；DEV-V4-06 后常量已退役删除，以现网 `:240` 为准。

---

## 2. 协议与执行：page 硬编码 2..6；服务端对 page=7 是拒

### 2.1 硬编码范围

`TIDYABLE_PAGE_MIN=2` / `TIDYABLE_PAGE_MAX=6` 是字面常量，注释写明因 `PlayerInventory.SLOTS/PANTS` 不是 C# `const` 才硬编码（LIT-HK `:55-58`）。`LitRuntime.AllPages = 0xFF`（LIT-RT `:26`）是全身哨兵，展开为 2..6（LIT-MOD `:360-368`；LIT-SVC `:321,358-359`：`for (byte page = PlayerInventory.SLOTS; page <= PlayerInventory.PANTS; page++)`）。

| 缝 | 对 page 的约束 | 位置 |
|---|---|---|
| 线编解码 | `page != AllPages && (page < 2 \|\| page > 6)` → `TryReadTidyRequest` 返回 `false` | LIT-WIRE `:181` |
| 权威执行 | 同上，返回 `TidyOperationOutcome.RejectedNoMutation` | LIT-AUTH `:63-71` |
| 快捷键快照 | 捕获/验证跳过不在 2..6 的页 | LIT-HK `:83,115` |
| 快捷键收敛 | `NewPage` 不在 2..6 → `VerifyClientConvergence` false | LIT-AUTH `:192` |
| 全身整理 | 只循环 SLOTS..PANTS | LIT-SVC `:358-359` |
| UI 注入 | `HEADER_INJECT_COUNT=5` → page 2..6 | LIT-UI `:243,514-516` |

`RequestTidy` 本身（LIT-MOD `:552-558`、LIT-NET `:525-570`）**不**在客户端发送前再验 page：客机把 UI 给的 `page` 原样写入 `BuildTidyRequest`（LIT-WIRE `:71`）。现网按钮只会产生 2..6 或 `AllPages`（LIT-UI `:639` + LIT-MOD `:488`）。page=7 只能来自畸形/手造包，或主机本地路径绕过线编解码。

### 2.2 服务端对 page=7：拒，不是忽略，也不是未定义

客机路径：

1. `HandleTidyRequest` 调 `TryReadTidyRequest`（LIT-NET `:604`）。page=7 使 `:181` 失败。
2. 失败分支（`:604-607`）：打 Warning「服务器收到畸形 RequestTidy，拒绝（peer=…）」然后 **`return`**。
3. 不入账本、不入队、**不** `SendCommitted`。客户端若已 `SetPending`（LIT-NET `:559`），会一直等到 pending TTL，而不是立刻收到 `TidyCommitResult.Rejected`。

这是 **fail-closed 拒收畸形包**（解码失败当包不存在），不是业务层「合法请求被 Rejected」。权威端还有第二道门（LIT-AUTH `:63-71`）：若有人绕过 codec 把 `Page=7` 送进 `ExecuteServerTidy`，打 Warning「权威端页范围校验失败（page=…），拒绝整理」并返回 `RejectedNoMutation`，**不会**索引 `items[7]`。注释原文：「the wire codec already rejects out-of-range pages, but the transaction must never index the inventory on an unvalidated page (the frozen tidyable range is 2..6 / AllPages).」

`TidyPage` 自身（LIT-SVC `:557-579`）**没有** page∈[2,6] 断言：只查 `items==null` 或 0×0 → `RejectedNoMutation`。范围门在 codec + 权威，不在装箱器。

### 2.3 主机本地路径缺口（T4 要用）

`RequestTidy` 在 `IsServerRole()`（单人 / listen host / U3DS）走 `RequestLocalTidy`，不经 codec（LIT-MOD `:552-555`）。`LocalTidyExecutor.Execute` 对非 `AllPages` 直接 `TidyPage(player.inventory.items[page], page, …)`（LIT-LOCAL `:65-67`），**没有** 2..6 门。因此：

- 现网 UI 不会把 page=7 送进这条缝（不注入 STORAGE）。
- 若测试或未来 UI 把 page=7 交给 `RequestTidy`，**主机本地会执行** `items[7]`（开着的箱子/后备箱网格）；客机仍被 codec 拒。
- `TidyAllPlayerPages` 仍只 2..6，Ctrl+全身不含 STORAGE（LIT-SVC `:358-359`）。

---

## 3. 原 LaunchInventoryTidy：v1.0 注入 STORAGE；执行分叉；v2.0.1 拆按钮

归档路径存在：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Archive\2-未闭环验证项目\LaunchInventoryTidy`。工作树 HEAD 是 2026-08-20 验收候选（CHANGELOG 冻结旧版本线）。v1.0 取 git `22d828b`。

### 3.1 v1.0 注入 STORAGE

`Patches/PlayerDashboardInventoryUIPatch.cs`（`22d828b`）：

- `:74-88`：容器页专用布局，`STORAGE_DIR_POS_OFFSET_X=-285`、`STORAGE_TIDY_POS_OFFSET_X=-240`，「headers 循环上限：i=0..5 -> page 2..7（含 STORAGE 容器页）」，`HEADER_INJECT_COUNT = 6`。
- `:291-303`：「循环注入 6 个标题栏：headers[0..5] -> page 2..7（含 STORAGE 容器页）」；`i=5` 用 STORAGE 偏移让出右侧 180px 避让 rot_x/y/z。
- 点击：房主 `ManualTidyService.TidyPage(page, desc)`；客机 `ManualTidyNetwork.SendTidyPageRequest(page, desc)`（同文件 HandleTidyClick）。

### 3.2 v1.0 执行不含 page 7？——只在房主便捷入口成立

**房主 / 单机便捷入口不含 page 7。** `ManualTidyService.cs`（`22d828b`）：

- 类头 `:8`：「对玩家 5 个多格页 (page 2..6: ITEMS/BACKPACK/VEST/SHIRT/PANTS) 执行 2D 装箱重排。」
- `TidyAllPlayerPages` `:22`：`for (byte page = PlayerInventory.SLOTS; page <= PlayerInventory.PANTS; page++)`。
- `TidyPage(byte page, …)` `:45-53`：「便捷入口：仅整理单个 page（page 2..6）。page 超出 [SLOTS, PANTS] 范围时**静默忽略**。」`if (page < SLOTS || page > PANTS) return;`

因此 v1.0 **房主点 STORAGE 按钮 = no-op**（可点、无整理、无错误回包）。Ctrl+全身也不含 page 7。

**客机网络路径会执行 page 7。** `ManualTidyNetwork.HandleRequestTidyPage`（`22d828b` `:98-112`）原文：

> page ∈ [SLOTS..STORAGE] = [2..7]，其中 page=7=STORAGE 是容器页（储物箱/展示柜/车辆后备箱）。服务端通过 player.inventory.items[STORAGE] 获取 Items 实例--该引用指向当前打开的 InteractableStorage.items，服务端权威修改后触发 onStateUpdated 自动同步给所有客机。

非法才是 `page < SLOTS || page > STORAGE`。合法 page=7 走 `TidyPage(Items, page, …)` 内部重载（`:64-68`），**没有** PANTS 上限。v1.0 客机点 STORAGE = 服务端真整理当前打开容器。

票面「v1.0 注入 STORAGE 但执行从不包含 page 7」对 **房主便捷入口** 为真，对 **客机网络** 为假。T4 不得把「从未执行 page 7」写成全路径事实。

### 3.3 v2.0.1 拆按钮的理由原文

V2 把协议白名单收到 2..6 后，UI 仍画 STORAGE → 可点但服务端确定性拒绝。外部审计（LIT-AUD20 `:23`）原文：

> UI 为 page 7 STORAGE 注入整理按钮，但 V2 服务端与快捷键逻辑只允许 page 2..6。点击容器整理按钮会被确定性拒绝。
>
> UI 可操作范围与协议白名单不一致。
>
> P0：若 v2 不支持 STORAGE，则不要向 header[5] 注入整理控件；若要支持，必须单独定义容器权限、生命周期和关闭容器时的并发行为，不能简单把上限改为 7。

同文件 `:38`：「P0：统一 UI 与服务端范围：v2.0.0 暂时移除 STORAGE 整理按钮。」`:88`：动态放行条件含「STORAGE 页面确认无不可用按钮，或按独立协议完成专项测试。」

v2.0.1 交付（LIT-HIST `:3507,3520,3589`）：

- 审计阻断：「STORAGE 页显示整理按钮，但服务端确定性拒绝 page 7」
- 「已解决: STORAGE 按钮移除 - HEADER_INJECT_COUNT=5，常量标记为 Obsolete」
- 对照表 P0-4：「`PlayerDashboardInventoryUIPatch`：`HEADER_INJECT_COUNT=5`，移除 STORAGE 按钮注入」

二次审计确认（LIT-HIST `:3641`）：「STORAGE 注入范围已降为 page 2..6。」归档 README `:23` / DEPENDENCIES `:26`：不支持箱子、车辆后备箱或其它世界容器；「这些对象具有独立的所有权、会话与并发模型，不能复用玩家背包协议。」

拆按钮的产品理由 = **消灭「可点但服务端拒」**，不是「容器整理永远不做」。审计明文：要支持必须单独定义权限/生命周期/关箱并发，不能把上限改成 7。

---

## 4. 容器对象模型：STORAGE vs InteractableStorage vs 后备箱 vs 工坊虚拟 vs AREA=8

**STORAGE 不是一种世界对象，是玩家库存的第 7 页槽。** 普通箱子、车辆后备箱、工坊虚拟容器打开时都把各自的 `Items` **挂进** `player.inventory.items[STORAGE]`。关则摘掉。谁有生命周期、谁跨玩家、谁被原版拒，取决于挂上去的那份 `Items` 的主人。

### 4.1 页槽本身

U3-PINV `:64-80,123-125`：`STORAGE=7`、`AREA=8`；运行时 `isStoring` / `isStorageTrunk` / `InteractableStorage storage`。`openStorage`（`:1571-1587`）服务器侧：已开则先关；`newStorage.isOpen=true`、`opener=player`；`isStoring=true`、`isStorageTrunk=false`、`storage=newStorage`；`updateItems(STORAGE, storage.items)`；`sendStorage()`。`openTrunk`（`:1593-1606`）同类，但 `isStorageTrunk=true`、`storage=null`（「storage is used to close crate」），挂 `trunkItems`。`closeStorage`（`:1623-1640`）清标志；若有 crate 则服务端 `isOpen=false`、`opener=null`；`updateItems(STORAGE, null)`。客户端 `ReceiveStoraging`（`:1120-1163`）重填 `items[STORAGE]`，`isStoring = height>0`，然后 `onInventoryStored`。

UI：箱子与后备箱**共用** `headers[5]` / STORAGE 网格；车内标题 `"Storage_Trunk"`，否则 `"Storage"`（U3-PDINV `:1757-1765`）。关箱 `newHeight==0` 时 `items[page].clear()`（`:1806-1809`）——会清掉挂在该 `itemsPanel` 上的注入子元素。

BII 已把 STORAGE 当活表面、AREA 当原生地面（BII-SURF `:260-263,799-811,1360-1364`；BII-DRAG `:271-273,816-821`）。LIT 整理协议没有跟过来。

### 4.2 InteractableStorage（世界箱子 / 展示柜）

U3-STOR：世界实体，自有 `Items items`（`:24-25`）、`isOpen` / `opener`（`:47-48`）、`owner` / `group`、锁。

| 问题 | 事实 |
|---|---|
| 生命周期 | 实体销毁 `ManualOnDestroy`：清物品（或掉落）、`items=null`；若仍开则 `opener.inventory.closeStorageAndNotifyClient()`（`:517-530`）。距离过远可关（`:57-63` + U3-PINV 距离检查 `:1560-1565`）。 |
| 跨玩家 | **互斥单 opener。** `checkStore`（`:326-333`）：锁/组通过且 `!isOpen`。已开则第二人 `ReceiveInteractRequest` 失败，玩家收到 `EPlayerMessage.BUSY`（`:626-628`）。 |
| 服务端拒 | 不能开玩家：`canPlayersOpen==false`（工坊可关，`:65,343-347,545-548`，「players are not allowed to open this type of storage」）；死、在后备箱中、逮捕、太远、视线挡、`BarricadeManager.onOpenStorageRequested` 插件拒（`:551-605`，「rejected by plugin」）。 |

整理若只重排 `player.inventory.items[7]`：开着时该引用就是 `storage.items`，变更走 `onStateUpdated` → `rebuildState`。关箱后 `items[7]` 被摘掉，整理窗口与箱子生命周期不同步。

### 4.3 车辆后备箱

不是 `InteractableStorage`。`grantTrunkAccess`（U3-VEH `:2181-2186`）仅服务端且 `trunkItems.height>0` 时 `openTrunk`。**驾驶座（seat 0）上车即授**（`:2279-2282`），与按 G 无关。离座/换座 `revokeTrunkAccess` → `closeTrunk`（`:2189-2194`）。`shouldStorageOpenDashboard => !isStorageTrunk`（U3-PINV `:138`）：后备箱**不会**像箱子那样自动弹仪表盘。多人：后备箱 `Items` 挂在车上，驾驶座玩家的 STORAGE 页指向同一份；不是箱子那种单 opener 互斥。LIT 协议仍把 page 7 当畸形。

### 4.4 工坊 / 插件虚拟容器

原版给插件的钩子，不是第三种页码：

1. `shouldCloseWhenOutsideRange` 默认 false，「Plugins needed to be able to set this to false for "virtual storage" plugins」（U3-STOR `:57-63`）。
2. `onStateRebuilt`：非 null 时不写 `BarricadeManager.updateState`，「Plugin implementation for virtual storage to hook state. Vanilla code does not use this.」（`:90-132`）。
3. `canPlayersOpen` / `ItemStorageAsset.CanPlayersOpen`（`:65,346,545-548`）：可做成不能被玩家打开的存储。
4. `BarricadeManager.onOpenStorageRequested`（`:599-605`）：插件可拒开箱。

虚拟容器仍占用 STORAGE=7 槽 + `InteractableStorage` 外壳，但状态可能不落原版 barricade、距离规则不同、开箱可被插件拒。LIT 注释把「工坊虚拟容器」列为不能与服装页共用规则的原因（LIT-UI `:236-238`）。现网协议没有识别虚拟容器的字段。

### 4.5 AREA=8

不是容器。U3-PDINV `resetNearbyDrops`（`:1771-1785`）：本地重建 8×3 `areaItems`，`replaceItems(AREA, areaItems)`，扫半径 16 的地面模拟物。标题 `"Area"`（`:2793`）。拖放里 AREA = 捡起/丢下（多处 `page == PlayerInventory.AREA`）。BII 明确 AREA 保持原生、不增强（BII-DRAG `:271-273,816-821`）。无 opener、无跨玩家共享网格、无服务端「整理 AREA」语义。LIT 循环与 `TIDYABLE` 都不含 8。装备槽 0/1 同样排除。

### 4.6 T4 对照

| 「容器」候选 | 页 | 生命周期 | 跨玩家 | 现网 LIT |
|---|---|---|---|---|
| 服装页 Hands..Pants | 2..6 | 玩家库存 | 仅本人 | 注入 + 协议允许 |
| InteractableStorage 箱子 | 7（挂载） | 世界实体 + 单 opener | 第二人 BUSY | 不注入；客机 codec 拒；主机本地若被调用会动 `items[7]` |
| 车辆后备箱 | 7（挂载） | 驾驶座授权/撤销 | 车载 Items，非单 opener | 同上 |
| 工坊虚拟容器 | 7（挂载） | 插件钩子，原版不落盘 | 插件可拒开 | 同上；无识别字段 |
| AREA 地面 | 8 | 本地半径扫描 | 无共享网格 | 不注入、协议拒 |
| 主副手 0/1 | 0/1 | 玩家槽 | 仅本人 | 不注入、协议拒 |

---

## 5. Phase-4 / CONTEXT / 手册把「不注入 STORAGE」写成永久不变量的原句

### 5.1 Phase-4 地图（永久产品不变量）

P4-MAP Out of scope `:64`：

> **永久产品不变量**：不建 BueUi SDK 替代 Glazier；不把 O-LIT-1 混入本阶段视觉；**不注入 STORAGE**；不热卸载外部插件；不用草稿改写 ServerAuthority；不后台自动保存；不把外部配置编辑扩展成任意代码执行。

开图 Notes `:34`：「STORAGE 页仍不注入」。V4-T1 摘要 `:45`：「STORAGE 注入均不做」。V4-T5 摘要 `:49`：「不含仓储栏」。

### 5.2 Phase-4 spec

P4-SPEC LIT 表面 `:100`：「仅注入 headers[0..4]（Hands/Backpack/Vest/Shirt/Pants），**不注入仓储栏**。」用户故事 `:37`：「Ctrl+左键…整理全身（**不含仓储栏**）」。Out of Scope `:163`：

> **永久产品不变量**：不建 BueUi SDK 替代 Glazier；不把 O-LIT-1 排序规则混入本阶段视觉；**不注入 STORAGE**；…

### 5.3 V4-T8 / V4-T5 票面

P4-T8 Q71 `:37-39`：

> **永久产品不变量（改目的地才能动）**
>
> 不建 BueUi SDK 替代 Glazier；不把 O-LIT-1 排序规则混入本阶段视觉工作；**不注入 STORAGE**；…

P4-T5 Q54 `:30`：「仍注入 `headers[0..4]`…**不注入 STORAGE**。」Tooltip：「Ctrl+左键：按全局模式和方向整理全身（**不含仓储栏**）」。同票「不重开 … STORAGE」（`:76`）。

### 5.4 CONTEXT.md（Phase-5 已改口径：可重开，未重开前仍不注入）

CTX 「整理按钮」`:157-159`：

> 背包标题栏上触发当前栏或全身整理的产品表面；仅在功能 `Running` 时注入 Hands/Backpack/Vest/Shirt/Pants。…**仓储栏是否注入由第五阶段重开裁决，未重开前现网仍不注入。**
>
> _避免_：…**把第四阶段「不注入 STORAGE」当成不可重开的产品宪法**

这是术语层对 Phase-4 永久句的显式降级：第五阶段可以重开，但重开前现网行为不变。

### 5.5 玩家手册

HB `:13`：

> 背包整理（LIT） | 背包页点「整理」按钮；Ctrl+点击 = 按已保存的全局模式与方向整理全身（**不含仓储栏**）；模式（同类/空间/大件）与方向在管理面板里改

手册是玩家可见契约，不是「永久不变量」标题，但产品文案与 Q54 tooltip / LIT-UI `:248` 一致。

### 5.6 Phase-5 地图对本不变量的态度

`.scratch/bue-v2-phase5-official-optimization/map.md` Destination `:16`：「第四阶段写过『不注入 STORAGE』的条目，若本阶段做容器整理，必须在本图显式重开后再定新边界。」Notes `:33`：「必须显式重开的旧不变量」。Out of scope `:55`：「STORAGE 注入是否仍永久禁止 = T1/T4 重开题，不在本节预先维持或废除。」本报告只供 T4 事实，不代替重开裁决。

---

## 6. 给 T4 的压缩结论

1. **现网只注入 headers[0..4] = page 2..6**，一颗 60×60 @ -130「整理」。STORAGE/AREA/0/1 不画。排除原文 LIT-UI `:236-240,510-511`。旧 STORAGE 偏移常量已删（`:240`）。
2. **协议硬编码 2..6 + AllPages=0xFF**。客机 page=7 = codec 畸形拒（无 Committed 回包）。权威第二道门 `RejectedNoMutation`。**主机本地 `LocalTidyExecutor` 无页门**——UI 目前到不了，T4 若开 STORAGE 必须先补这条缝。
3. **v1.0 画了 STORAGE**（`HEADER_INJECT_COUNT=6`）。房主 `TidyPage(byte)` 静默忽略 page 7；客机网络 **会** 整理 `items[STORAGE]`。v2.0.1 因审计「可点但服务端确定性拒绝 page 7」把注入降为 5；理由不是永久禁容器，而是 UI 与白名单必须一致，真做要独立权限/生命周期/关箱并发。
4. **STORAGE 是页槽**。箱子 = 单 opener 世界实体（第二人 BUSY）。后备箱 = 驾驶座授权的车载 `Items`。工坊虚拟 = 同槽 + 插件状态/开箱钩子。AREA=8 是本地地面，不是容器。
5. **Phase-4 把「不注入 STORAGE」写进永久产品不变量**（P4-MAP `:64`、P4-SPEC `:163`、P4-T8 `:39`）。CONTEXT 已写明由第五阶段重开；未重开前现网与手册仍是「不含仓储栏」。

本报告不设计按钮、不改码、不重开不变量。
