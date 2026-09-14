# V5-R4 被动整理历史与现网缺席

- **Ticket**: V5-R4
- **Date**: 2026-09-14
- **Type**: research（一手来源；只查证不改码、不设计新 patch）
- **Scope**: T5 事实输入。v1.4.0 `ItemsTryAddItemPatch`、v1.4.1 摘 `[HarmonyPatch]` 禁用、BUE DEV-V2-15 排除锚、现网整理入口、CONTEXT「自动整理」vs「被动整理」、`tryAddItem` 权威侧。
- **Method**: 对照 U3-SDK、Archive LIT git（`066e154` / `ce50bc6`）、BUE 生产源与红测。行号以 2026-09-14 工作树为准；v1.4.0 patch 行号取 `git show 066e154:Patches/ItemsTryAddItemPatch.cs`。

## 0. 一手来源

| 编号 | 来源 | 身份 |
|---|---|---|
| V140 | Archive LIT `066e154:Patches/ItemsTryAddItemPatch.cs` | v1.4.0 被动整理 Prefix 全文 |
| V141 | Archive LIT `Patches/ItemsTryAddItemPatch.cs`（工作树 = `ce50bc6` 后空类） | v1.4.1 摘特性、类留空 |
| V141C | Archive LIT commit `ce50bc6` | v1.4.1 紧急止损提交说明 |
| AUD-V2 | Archive LIT `ce50bc6:.audit/v1.4.0-bug-analysis-20260716/items-tryadditem-bug-analysis-v2.md` | 外部审计 v2：方案 A 禁用口径 |
| AUD-CL | Archive LIT `ce50bc6:AUDIT_CHECKLIST.md` | 禁止 Prefix 内开关的原文 |
| LIT-LOG | Archive LIT `LaunchInventoryTidyPlugin.cs:176` | 「仅支持玩家 page 2-6；容器整理未实现。被动整理保持禁用。」 |
| LIT-W | Archive LIT `ManualTidyWatcher.cs` | 旧 Plugin 0 手动整理入口（未迁入 BUE） |
| LIT-DEP | Archive LIT `DEPENDENCIES.md:26` | 发布边界：仅 page 2-6 手动整理 |
| CTX | `CONTEXT.md:161-163`、`:305-307` | 「被动整理」vs 更好的物品交互禁止「自动整理」 |
| TEST | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:4188-4471` | DEV-V2-15 排除锚（编译列表 + 产物类型） |
| CSPROJ | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj:73-94` | 生产 Compile Include；无 `ItemsTryAddItemPatch.cs` |
| V215-CL | `audit/2026-09-06/DEV-V2-15/DEV-V2-15-closing-report.md:15,65` | 结单：死代码空类不迁 |
| V215-ISS | `.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-15-lit-singleplayer-path.md:20` | 工单硬规则：夹具排除出生产编译 |
| LIT-UI | `src/BetterUnturnedExperience.Lit/Ui/InventoryTidyUiPatch.cs` | 现网唯一 Harmony 整理入口（标题栏按钮） |
| LIT-MOD | `src/BetterUnturnedExperience.Lit/InventoryTidyModule.cs` | `RequestTidyFromUiClick` / `RequestTidy` / 装补丁只装 UI |
| LIT-EXE | `src/BetterUnturnedExperience.Lit/Tidy/LocalTidyExecutor.cs:117-124` | 按钮路径才发 `TidyCompleted` |
| LIT-AUTH | `src/BetterUnturnedExperience.Lit/Tidy/LitTidyProductionAuthority.cs:31-38,60-72` | 联机整理权威在服务端角色 |
| BII | `src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs:13-19,989-994` | 拖放提交走 `sendDragItem`，不走 `tryAddItem` |
| U3-IT | U3-SDK `Items.cs:316-360` | `Items.tryAddItem(Item,bool)`：先 `tryFindSpace`，失败返 false |
| U3-PI | U3-SDK `PlayerInventory.cs` | 页常量、`tryAddItemAuto`、拖放 RPC、`ReceiveItemAdd` 仅服务端推客户端 |
| U3-IM | U3-SDK `ItemManager.cs:292-386` | 拾取 RPC `SERVERSIDE`，成功才销毁地面物 |
| U3-CR | U3-SDK `PlayerCrafting.cs:677-733,954-958` | 合成 RPC `ONLY_FROM_OWNER`，产物 `forceAddItem` |
| U3-AREA | U3-SDK `PlayerDashboardInventoryUI.cs:2440-2457` | 地面预览 AREA 页本地 `tryAddItem`（视觉，非库存权威） |

---

## 1. v1.4.0 `ItemsTryAddItemPatch`：触发、作用页、失败回退、事件

**结论：只在原版 `tryFindSpace` 失败后整页重排（含新物品）；页 `>= SLOTS`（即非装备槽 0/1）；`TryPack` 失败则放行原版返回 false；成功路径无整理完成事件，只把 `__result=true` 并跳过原方法。失败回退并不保证新物品守恒——`TryPack==true` 且新物品 `Placed=false` 时仍谎报成功。**

Harmony 目标是 `Items.tryAddItem(Item, bool)`，不是 `PlayerInventory.tryAddItem`（V140 `:26-29`）。vanilla 实现先 `tryFindSpace`，找不到空位即 `return false`（U3-IT `:321-337`）。

### 1.1 触发条件（仅碎洞失败，非每次放入）

Prefix 按序短路（V140）：

| 步 | 条件 | 行为 |
|---|---|---|
| 1 | `__instance.page < PlayerInventory.SLOTS`（SLOTS=2，页 0/1 装备槽） | `return true` 放行原版（`:31-35`） |
| 1b | 新物品 null / `GetAsset()` null | 放行（`:39-51`） |
| 2 | `__instance.tryFindSpace(sx, sy, …)` 成功 | 放行，**不重排**（`:56-60`） |
| 3 | `InventorySolver.TryPack(..., sortDescending: true, mode: TidyMode.MaxRects)` 返回 false | 放行，让原版走「返回 false」（`:84-91`） |
| 4 | TryPack 返回 true | 清空整页 + 按结果 `addItem`，`__result = true; return false` 跳过原方法（`:97-151`） |

类注释原话：「在背包已散乱导致 tryFindSpace 失败时……重组，让原本"装不下"的物品能塞进去」（V140 `:8-9`）。成功放入路径明确不触发。被动路径算法写死 MaxRects+降序，不读 UI 模式（`:84`）。

vanilla `Items.tryAddItem` 本身也是「无空位即 false」（U3-IT `:334-337`），所以 Prefix 的「先试 tryFindSpace」与原方法契约对齐。

### 1.2 作用页

- **跳过**：页 0/1（Primary/Secondary）。常量 `PlayerInventory.SLOTS = 2`（U3-PI `:64,72-73`）。
- **会拦截**：任何 `page >= 2` 的 `Items` 实例，因为 patch 挂在 `Items` 类型而非「仅玩家页」过滤器。玩家多格页是 2 Hands / 3 Backpack / 4 Vest / 5 Shirt / 6 Pants；7=STORAGE，8=AREA（U3-PI `:66-79`）。
- **产品声明（后继版本）**把手动整理收窄到 page 2-6，并写明被动整理保持禁用（LIT-LOG `:176`；LIT-DEP `:26`）。v1.4.0 Prefix **没有** `page > PANTS` 守卫，因此 STORAGE=7 与 AREA=8 只要走到 `Items.tryAddItem(Item,bool)` 也会被整页重排。AREA 地面预览正是这条签名（U3-AREA `:2444`）。

`tryAddItemAuto` 循环 `for (page = SLOTS; page < PAGES - 2; page++)`，即 2..6，不含 STORAGE/AREA（U3-PI `:576-578`）。拾取/合成自动入包因此只打玩家页；容器页不会被这条自动循环打到。

### 1.3 失败回退

- **TryPack false**（部分合法物品未放置）：Prefix `return true`，原方法再 `tryFindSpace` 失败后返回 false。网格未被清空（V140 `:14-15,86-91`）。vanilla 拾取据此发 `SPACE`、地面物留下（U3-IM `:383-386`）；`forceAddItem` 据此 `dropItem`（U3-PI `:607-612`）。
- **TryPack true 但新物品 Placed=false**：新物品 `Tag=null`，无原位可恢复；`tryFindSpace` 再失败则静默跳过，**仍** `__result=true`（V140 `:124-151`）。这是审计钉死的吞物路径（AUD-V2 根因 1；V141 `:10-14`）。**不是**「放不下就当原版失败」。
- TryPack 把超尺寸物品排除出 `validCount` 仍可对剩余合法物品返回 true——v1.4.0 求解器注释仍写「true = 所有合法物品均已成功放置（异常物品 Placed=false 但不影响整体）」（现网 `InventorySolver.cs:104-106` 同语义；AUD-V2 §2.6）。被动路径把这个 true 当成「新物品已入包」。

清空手段是 `while (getItemCount() > 0) removeItem(0)`（V140 `:97-101`），会打 `onItemRemoved`/`onItemAdded` 链；在 `PlayerInventory` 上下文会发 Reliable 同步包（V140 `:21-24`；U3-PI `:1742-1760`）。

### 1.4 是否发整理完成事件

**否。** v1.4.0 Prefix 只设 `__result` 并跳过原方法（V140 `:149-151`）。该 commit 的 diff 是 solver + UI 模式按钮 + 协议 `mode` 字段，无事件总线、无 `TidyCompleted`（V141C 对应的 v1.4.0 提交 `066e154` 说明）。BUE 的 `TidyCompleted` 是 DEV-V2-19/21 才立的功能事件，发布点在按钮/联机事务完成处（LIT-EXE `:117-124`；LIT-MOD `:330-350`）。被动路径若按 v1.4.0 原样恢复，不会自动接到 LIR「整理后自动压弹」。

---

## 2. v1.4.1 禁用方式与「禁止 Prefix 内加开关」原文

**结论：方案 A = 摘掉 `[HarmonyPatch]` + 清空类体，让 `PatchAll()` 发现不了该类。审计明文不采用 Prefix 内条件分支，因为会保留误触发面。**

工作树空类（V141 `:1-35`）：

> v1.4.1: 被动整理 Patch 已禁用。
> …禁用方式：移除 [HarmonyPatch] 特性，类留空。HarmonyInstance.PatchAll() 不会发现此类，Patch 不会被注册。
> …不要在此类内添加 Prefix/Postfix 方法或 [HarmonyPatch] 特性。

类体只剩 `internal static class ItemsTryAddItemPatch { }`（V141 `:32-34`）。

审计 v2 方案 A 原文（AUD-V2 `:274-288`）：

> **改动**：移除 `[HarmonyPatch(typeof(Items), "tryAddItem", ...)]` 特性，类留空。
> **实施方式**（按审计推荐）：
> - 移除 `[HarmonyPatch]` 特性
> - 类留空，加注释说明历史背景
> - **不采用**"在 Prefix 内增加条件分支"（会保留误触发面）
> - **不采用**"从 .csproj 移除文件"（保留历史参考）

`AUDIT_CHECKLIST.md` 把同一禁令写成「脏代码」清单（AUD-CL `:85-90`）：

> - 未使用 `[if condition] return false;` 之类的运行时开关绕过 Patch（审计明确指出此方式保留误触发面）
> - 未保留 Prefix 方法但加 `return true` 跳过（仍是 Patch 注册）
> - 未注释 `[HarmonyPatch]` 特性（注释会被编译器忽略，但显式删除更彻底）
> - 选择"彻底删除特性 + 清空类体"是最干净的方式：编译产物元数据层即证明无 Patch

提交说明同口径（V141C）：「未采用"在 Prefix 内增加条件分支"的方式（审计明确指出会保留误触发面）。」CONTEXT 把该纪律收成领域词避免项：「用 Prefix 内死开关冒充禁用」（CTX `:163`）。

v1.4.1 仍保留手动路径（[整理] 按钮 + Plugin 0），并声明残余风险（V141 `:19-21`；AUD-V2 `:281`）。方案 B（差分移动 + 失败回滚）被审计拒绝，理由是占位环、remove/add 立即发网、事后回滚撤不掉已发事件（AUD-V2 `:294-304`）。

---

## 3. DEV-V2-15 排除锚与生产 csproj

**结论：红测在 `AssertLitSingleplayerPath` 第四节同时钉编译列表文件名与产物类型名；`ItemsTryAddItemPatch` 两层都缺席。生产 Plugin csproj 的 EmbeddedLit 列表不含该文件，注释写明「dead passive-tidy patch are NOT in this list」。**

锚点方法头（TEST `:4188-4194`）：

> DEV-V2-15 red anchor: LIT (inventory tidy) adoption, single-player path. Freezes the four red surfaces the ticket names — strategy replacement … enabled=false native fallback, direct InventorySolver algorithm tests, and the harness exclusion from the production compile …

编译列表断言（TEST `:4416-4444`）上溯到 sln 根，读 `BetterUnturnedExperience.Plugin.csproj` 原文：

- 正向对照：必须含 `EmbeddedLit\Solver\InventorySolver.cs`（`:4426-4427`）
- `excludedSources` 含 `"ItemsTryAddItemPatch.cs"` 与 `"ManualTidyWatcher.cs"`（`:4428-4437`）
- `Assert(!pluginCsproj.Contains(excluded))`（`:4438-4441`）
- 同时禁止 `TIDY_TEST_HARNESS`（`:4443-4444`）

产物反射断言（TEST `:4446-4471`）：

- `excludedNames` 含 `"ItemsTryAddItemPatch"`、`"LaunchInventoryTidyPlugin"`、`"ManualTidyNetwork"`（`:4448-4455`）
- `Assert(!productionTypeNames.Contains(excluded))`（`:4467-4470`）
- 零 `LaunchInventoryTidy*` 命名空间（`:4462-4466`）

生产 csproj（CSPROJ `:73-75`）原文：

> DEV-V2-15: embed the migrated LIT (inventory tidy) domain into the single runtime assembly. The old plugin's BepInEx identity, LMN dependency, harness fixtures and dead passive-tidy patch are NOT in this list — the exclusion is a hard rule verified by the host red test.

随后 Compile Include 到 `InventoryTidyModule.cs` 为止（`:76-95`），无 `ItemsTryAddItemPatch.cs`。`src/BetterUnturnedExperience.Lit/` 目录也没有该文件。Lit 域没有独立 csproj，全部经 Plugin 工程嵌入。

结单把空类列为「不迁」死代码（V215-CL `:15`：「死代码（ItemsTryAddItemPatch 空类）排除出生产编译列表」；`:65` 不迁名单含 `ItemsTryAddItemPatch`）。工单硬规则（V215-ISS `:20`）：「TIDY_TEST_HARNESS 与全部实机夹具类型排除出生产编译列表」。

因此：恢复被动整理 = 新写并登记 patch（且须过红测，因为当前断言要求类型缺席），不是把 Archive 空类编进 csproj，也不是翻一个现存 bool。

---

## 4. 现网 LIT 除标题栏按钮外还有没有整理入口

**结论：没有。现网唯一产品入口是标题栏「整理」按钮（左键当前栏 / Ctrl+左键全身）。无 Plugin 0 热键、无拖放后整理、无拾取后整理、无 `Items.tryAddItem` Harmony patch。**

### 4.1 标题栏按钮（现网唯一入口）

`InventoryTidyUiPatch` 是 Lit 域唯一 `[HarmonyPatch]`，目标 `PlayerDashboardInventoryUI` 构造函数（LIT-UI `:37-38`）。注入 headers[0..4] = Hands/Backpack/Vest/Shirt/Pants；左键当前栏，Ctrl+左键全身、不含仓储栏（LIT-UI `:16-23,610-639`）。点击走 `RequestTidyFromUiClick`（LIT-MOD `:468-488`）→ `RequestTidy`（`:545-558`）：服务端角色本地整理，真客户端发可靠网请求，「a client never falls back to a local unauthoritative tidy」（`:548-550`）。

模块装补丁只 `CreateClassProcessor(typeof(InventoryTidyUiPatch)).Patch()`（LIT-MOD `:610-615`），不 PatchAll、不装 `Items` patch。

### 4.2 热键

Archive `ManualTidyWatcher` 监听 Unturned Plugin 0（`ControlsSettings.getPluginKeyCode(0)`）发全身整理（LIT-W `:7-38`）。该文件在 DEV-V2-15 排除名单（TEST `:4434`）。BUE `src/` 对 `getPluginKeyCode` / `ManualTidyWatcher` 零命中。`HotkeySnapshot` 是整理后重绑数字键快捷栏坐标，不是整理触发热键（`HotkeySnapshot.cs:17-20,55-58`）。

### 4.3 拖放后 / 拾取后

- BII 拖放提交 `player.inventory.sendDragItem(...)`（BII `:13-19,989-994`）。vanilla `ReceiveDragItem` 是 `ONLY_FROM_OWNER` 服务端 RPC，用 `checkSpaceDrag` + `removeItem` + `addItem`，**不调用** `Items.tryAddItem`（U3-PI `:699-784,967-969`）。因此即便恢复 v1.4.0 Prefix，拖放提交也不会打到它。
- 拾取权威在 `ItemManager.ReceiveTakeItemRequest`（`SERVERSIDE`）里 `player.inventory.tryAddItem(...)`（U3-IM `:292-368`）。现网无 Prefix，拾取失败即原版 `SPACE` + 地面物留下（`:383-386`）。
- 生产 `src/` 对 `tryAddItem` / `ItemsTryAddItem` 零命中。

页范围常量仍是 2..6（`HotkeySnapshot.cs:55-58`；权威页闸 LIT-AUTH `:67-71`）。STORAGE 现网不注入（LIT-UI `:23`；CONTEXT 整理按钮词条）。

---

## 5. CONTEXT「自动整理」禁止项 vs 被动整理

**结论：两套词，禁止混用。「被动整理」属背包整理，在原版入包失败时整页重排；「自动整理」是更好的物品交互的避免项，与自动交换、方向记忆并列。被动整理若做成拖放预览路径上的自动重排，会撞上 BII 禁令。**

背包整理词条（CTX `:161-163`）原文：

> **被动整理**：
> 背包整理在原版入包失败（典型是多格物品对不上碎裂空位）时触发的整页重排，让这次入包有机会成功；它属于背包整理，不是新官方功能。
> _避免_：自动整理、更好的物品交互的拖放增强、每次成功放入都重排、用 Prefix 内死开关冒充禁用

更好的物品交互词条（CTX `:305-307`）原文：

> **自动旋转**：
> 「更好的物品交互」在当前方向局部或全局无法放置时，是否允许尝试原生顺时针 90° 方向的玩家本地体验偏好。…朝向切换由**几何空间与边缘引力驱动**，而非物品历史方向记忆驱动…
> _避免_：自动交换、自动整理、全局跨生命周期方向记忆

冲突面（从原句直接推出，不另设计）：

1. **术语**：被动整理必须继续叫被动整理，不得写成「自动整理」。后者已被 BII 列为禁止项。
2. **功能归属**：被动整理「属于背包整理，不是新官方功能」；「避免：更好的物品交互的拖放增强」。挂到 BII 拖放预览/增强拖入即违宪。BII 现网提交本就不走 `tryAddItem`（§4.3）。
3. **触发密度**：避免「每次成功放入都重排」。v1.4.0 已是 tryFindSpace 失败才重排（§1.1），与该避免项同向；若改成成功放入也重排，同时撞 CONTEXT 被动整理避免项与 BII「自动整理」。
4. **禁用形态**：避免「Prefix 内死开关冒充禁用」——与 v1.4.1 审计同一句话。

---

## 6. `tryAddItem` 在哪一侧执行；被动整理权威必须挂哪侧

**结论：库存权威的 `Items.tryAddItem` / `PlayerInventory.tryAddItem*` 在服务端（含单机/听主机本地服务端）执行。远程客户端库存变更走 `ReceiveItemAdd`/`ReceiveItemRemove`（`ONLY_FROM_SERVER`），不重跑 tryAdd。被动整理若恢复，必须挂在服务端这次成功/失败判定上，与现网按钮整理同一权威；只在客户端改网格没有权威。**

### 6.1 vanilla 谁跑 tryAddItem

| 场景 | RPC / 入口 | 校验 | 实际 tryAdd 位置 |
|---|---|---|---|
| 拾取地面物 | `ItemManager.ReceiveTakeItemRequest` | `ESteamCallValidation.SERVERSIDE`（U3-IM `:293-294`） | 服务端 `player.inventory.tryAddItem(..., true)` 或定位四参重载（`:361-368`）。true → 删地面 + `SendDestroyItem`；false → `SPACE`（`:371-386`） |
| 合成产物 | `PlayerCrafting.ReceiveCraft` | `ONLY_FROM_OWNER`（U3-CR `:677-679`）→ `HandleCraftRequestInternal` | 服务端 `forceAddItem` → `tryAddItemAuto`；false 才 `dropItem`（U3-CR `:954-958`；U3-PI `:607-612`） |
| 拖放 | `ReceiveDragItem` | `ONLY_FROM_OWNER`（U3-PI `:699-701`） | 服务端 `checkSpaceDrag` + `removeItem` + `addItem`，**不**经 `tryAddItem`（`:761-784`） |
| 客户端看见入包 | `ReceiveItemAdd` | `ONLY_FROM_SERVER`（U3-PI `:1074-1084`） | 客户端 `items[page].addItem(...)` 按服务器坐标写入，不 tryFindSpace |
| 服务端同步发出 | `onItemAdded` | `Provider.isServer && !channel.IsLocalPlayer`（U3-PI `:1742-1746`） | 仅服务端非本地玩家连接发 `sendItemAdd` |
| AREA 地面预览 | `createElementForNearbyDrop` | 本地 UI | `areaItems.tryAddItem`（U3-AREA `:2444`）。`AREA=8` 是视觉页，不是玩家库存权威 |

`tryAddItemAuto` 只填空位，绝不替换手上枪（U3-PI `:508-527` 先 secondary/primary 空槽，再 2..6；AUD-V2 §2.1）。单机/听主机 `Provider.isServer==true`，同一进程既是客户端也是服务端，tryAdd 仍走服务端方法。

### 6.2 现网整理权威

按钮整理：`LitTidyProductionAuthority.IsServerRole()` 读 `Provider.isServer`（LIT-AUTH `:31-38`）。是服务端角色 → `RequestLocalTidy` 改本地（也是权威）库存（LIT-MOD `:552-555`）；真客户端只发网，服务端 `ExecuteServerTidy` 改 peer 的 `Player.inventory`（LIT-AUTH `:60-78`）。客户端「never falls back to a local unauthoritative tidy」（LIT-MOD `:548-550`）。页闸 2..6（LIT-AUTH `:67-71`）。

### 6.3 对被动整理恢复的事实约束（不设计 patch）

v1.4.0 Prefix 挂在 `Items.tryAddItem` 上。权威入包（拾取/合成/自动装备）在服务端调用该方法，所以 **Prefix 必须存在于执行 tryAdd 的那一侧进程**——U3DS 上是服务器 DLL；P2P 听主机是房主进程；纯客机上的同名方法主要用于 AREA 预览等非权威网格，改客机网格不会让服务器承认入包，也无法阻止服务器在 `__result=true` 时销毁地面物。

现网按钮整理已经把「改网格」收在服务端角色 + `ManualTidyService` 事务里，并在提交后发 `TidyCompleted`（LIT-EXE `:117-124`）。v1.4.0 被动路径没有该事件、没有事务回滚、且在 TryPack 误 true 时违反 tryAdd 成功契约（§1.3）。审计方案 B 拒绝「在 tryAddItem 内做复杂事务」（AUD-V2 `:304`）。这些是 T5 要裁的事实，不是本票方案。

---

## 7. 给 T5 的对照表（不裁决）

| T5 问 | 事实 |
|---|---|
| 触发 | v1.4.0 = 仅 `tryFindSpace` 失败才重排；成功放入放行。CONTEXT 避免「每次成功放入都重排」 |
| 失败回退 | TryPack false → 原版 false（物品留地上/掉落）。TryPack true 但新物品未放入 → 仍 `__result=true`（吞物）。恢复时若要「放不下就当原版失败」，不能原样搬 Prefix |
| 作用页 | Prefix 跳过 0/1，其余 `Items` 页都打得到。产品后来声明只 2-6；STORAGE/AREA 无产品承诺。现网按钮只 2-6 |
| 禁用形态 | 摘 `[HarmonyPatch]` + 类留空；禁止 Prefix 内开关。BUE 连空类都不编进 DLL，红测断言类型缺席 |
| 现网入口 | 仅标题栏按钮。Plugin 0 / 拖放后 / 拾取后均无 |
| 词汇 | 被动整理 ≠ 自动整理；后者是 BII 禁止项；被动整理禁止做成 BII 拖放增强 |
| 权威 | tryAdd 在服务端；拖放不经 tryAdd。被动整理要有权威必须挂服务端入包判定，不能只改客户端 |
| TidyCompleted | v1.4.0 不发。现网只有按钮/联机事务发。是否让被动成功触发 LIR 压弹 = T5 裁断，史实是「不会自动发」 |
