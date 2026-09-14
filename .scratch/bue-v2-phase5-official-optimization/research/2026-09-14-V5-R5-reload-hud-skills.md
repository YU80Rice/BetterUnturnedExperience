# V5-R5 换弹、原版经验与弹药 HUD 现状

- **Ticket**: V5-R5
- **日期**: 2026-09-14
- **方式**: 只读对照一手源码。未改生产代码、未构建、未提交。
- **范围**: T7 事实输入。不设计技能树 UI。不把 LHT 尸潮 HUD 当成弹药 HUD。

## 0. 一手来源

| 编号 | 来源 | 身份 |
|---|---|---|
| LIR-SVC | `src/BetterUnturnedExperience.Lir/Service/AmmoRepackService.cs` | 功能 A 合并 / 功能 B 压弹事务 |
| LIR-IN | `src/BetterUnturnedExperience.Lir/Reload/ReloadInputDriver.cs` | 双击 R 窗口 |
| LIR-POL | `src/BetterUnturnedExperience.Lir/ReloadRuntimePolicy.cs` | 窗口 / 冷却 / 页范围常量 |
| LIR-MOD | `src/BetterUnturnedExperience.Lir/InPlaceReloadModule.cs` | 模块生命周期、双击入口、设置 descriptor 残骸 |
| LIR-CON | `src/BetterUnturnedExperience.Lir/Reload/TidyCompletedConsumer.cs` | 整理后自动压弹触发门 |
| LIR-ACT | `src/BetterUnturnedExperience.Lir/Reload/AutoReloadAfterTidyAction.cs` | 功能 A adapter |
| LIR-GATE | `src/BetterUnturnedExperience.Lir/Service/LirRepackGate.cs` | 1.5s 冷却 / 120s 重放窗 |
| LIR-AUTH | `src/BetterUnturnedExperience.Lir/Service/LirProductionAuthority.cs` | 权威准入 + 事务调用 |
| LIR-NET | `src/BetterUnturnedExperience.Lir/Net/LirRepackNetwork.cs` | 功能 B toast / 联机请求 |
| LIR-P1 | `src/BetterUnturnedExperience.Lir/Patches/UseableGunReceiveAttachMagazinePatch.cs` | 仅 `ReceiveAttachMagazine` |
| LIR-P2 | `src/BetterUnturnedExperience.Lir/Patches/ForceAddItemPatch.cs` | 仅 `forceAddItem` |
| LIR-REG | `src/BetterUnturnedExperience.Plugin/InPlaceReloadFeatureRegistration.cs` | LIR 注册面（无 settings facet） |
| LHT-HUD | `src/BetterUnturnedExperience.Lht/Presentation/PlayerLifeUiHudPatches.cs` | 尸潮 HUD 注入 `PlayerLifeUI` |
| LHT-REG | `src/BetterUnturnedExperience.Plugin/HordeTrackerFeatureRegistration.cs` | LHT 注册面（同样无 settings facet） |
| LIT-REG | `src/BetterUnturnedExperience.Plugin/InventoryTidyFeatureRegistration.cs` | 对照：enabled 退役后仍有 Choice 面 |
| LIT-PUB | `src/BetterUnturnedExperience.Lit/InventoryTidyModule.cs` | `TidyCompleted` 发布页范围 |
| MIG | `src/BetterUnturnedExperience.Plugin/BueLegacyEnabledMigrationAdapter.cs` | `inplacereload.enabled` legacy 迁移 |
| SET | `src/BetterUnturnedExperience.Plugin/BueSettingsRuntime.cs` | 无 facet = Settings 视图诚实 null |
| CTR | `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` | `IFeatureSettingsRegistration` / `TidyCompleted` |
| PANEL | `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs` | 生命周期「启用」开关 vs 设置行 |
| HB | `docs/BetterUnturnedExperience-Player-Handbook.md` | 玩家可见 LIR 用法 |
| U3-GUN | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Useable\UseableGun.cs` | `updateInfo` / `ammoLabel` / 后备弹匣搜索 |
| U3-LOC | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Files\Local.cs` | `localization.format("Ammo", a, b)` |
| U3-LOCROOT | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Files\Localization.cs` | `PlayerUseableGun.dat` 读取路径 |
| U3-PINV | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInventory.cs` | 页常量 / `FindAttachmentsByCaliber` / `sendUpdateAmount` |
| U3-ITEMS | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Inventory\Items.cs` | `updateAmount` 无 MaxAmount 夹紧 |
| U3-ITEM | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Inventory\Item.cs` | 构造器可写任意 `amount` |
| U3-ASSET | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Bundles\ItemAsset.cs` | `MaxAmount` / `MaxAmountAsByte` |
| U3-SK | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerSkills.cs` | 经验 / `askSpend` / 技能树 |
| U3-SKILL | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\Skill.cs` | 单槽 `Skill` |
| U3-LIFE | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\UI\Player\PlayerLifeUI.cs` | HUD 容器 / 指南针几何 |
| U3-OFF | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\EPlayerOffense.cs` 等同目录枚举 | 技能树索引 |

U3-SDK 的 `PlayerUseableGun.dat` 不在本 SDK 树内（`Localization.read` 读运行时 `localizationRoot`，U3-LOCROOT `:54-72`）。`Ammo` 键的字面模板因此**不能从 SDK 仓库直接引用**；能钉死的是调用形态：两个参数 = 当前弹量 + 弹匣 `MaxAmount`。

---

## 1. 现网功能 A vs 功能 B

LIR 自己把两条路径钉成「功能 A = 整理后自动压弹；功能 B = 双击换弹键」（LIR-SVC `:22,32`）。玩家手册同口径：双击换弹键一键压弹 + 整理后自动压弹（HB `:14`）。

### 1.1 功能 A（整理后自动压弹）

**触发**

1. LIT 整理提交后发布 `TidyCompleted`（LIT-PUB `:338-350`）。单页发布该页，全身整理发布 2..6（LIT-PUB `:360-369`）。
2. LIR 在 `Start` 订阅该事件（LIR-MOD `:157`），`TidyCompletedConsumer` 消费（LIR-CON `:38-81`）。
3. 门：`Result == Succeeded`；`FirstPage <= LastPage`；范围必须落在 `MinRepackPage..MaxRepackPage`（2..6）；`TransactionId != 0`；同 publisher 事务 id 单调去重；目标 SteamId 可解析（LIR-CON `:54-80`）。
4. `AutoReloadAfterTidyAction` 再要求模块 `Enabled && Started && !ShuttingDown`（LIR-ACT `:28-31`）。
5. `ExecuteTidyMerge` → `Authority.ExecuteMerge` → `AmmoRepackService.TryMergeSameIdMagazinesTransactional`（LIR-MOD `:299-301`，LIR-AUTH `:56-63`）。

**算法（A 做什么）**

- 扫描页 `PlayerInventory.SLOTS..PANTS`（2..6）（LIR-SVC `:57-58`）。
- 只收 **含 `FillTargetItem` 蓝图** 的 `ItemMagazineAsset`（LIR-SVC `:790-798,926-936`）。无该蓝图直接跳过。
- **按同一弹匣 asset id 分组**。组内 ≥ 2 才合并（LIR-SVC `:824-844`）。
- 子弹总量按「最满优先填满」分配到 `maxAmount`；空且 `ShouldDeleteAtZero` 的弹匣删除（LIR-SVC `:856-890`）。
- **不读弹药箱**。`BuildMergePlan` 只处理弹匣条目，没有 ammo-box 分支。

**是否填弹药箱**：否。功能 A 是同 ID 弹匣互填，不是箱→匣。

**冷却**：与功能 B 共用 `LirRepackGate`。`Admit` 先解析玩家再 `TryAcquire`（LIR-AUTH `:91-96`）。命中冷却记 `RejectedCooldown`，A 只打诊断日志、无 toast（LIR-MOD `:310-311`）。冷却常量 1.5s（LIR-POL `:34`，LIR-GATE `:8-9,101`）。

### 1.2 功能 B（双击 R 一键压弹）

**触发**

1. `HostTick` 驱动 `ReloadInputDriver.Tick`（LIR-MOD `:245`）。生产键源 = `InputEx.GetKeyDown(ControlsSettings.reload)`（LIR-MOD `:348-352`）。
2. 双击窗口 `DoubleClickWindowSeconds = 0.3f`（LIR-POL `:14`，LIR-IN `:56`）。窗口内第二次按下触发一次并清基线；窗口外或首次只锚定基线。单击原生换弹不被拦截（LIR-IN `:50-68`）。
3. `OnDoubleTapReload`：模块必须 `Started && Enabled && !ShuttingDown`。服务器角色（单机 / 房主 / U3DS）本地 `ExecuteRepackFor`；纯客户端走可靠请求（LIR-MOD `:260-284`）。
4. 权威 `ExecuteRepack` → `TryRepackTransactional`（LIR-AUTH `:28-38`）。

**算法（B 做什么）**

- 同样只扫 page 2..6（LIR-SVC `:108-109,184-188`）。
- **目标**：未满弹匣（`ItemMagazineAsset` 且 `amount < MaxAmountAsByte` 且 `MaxAmountAsByte != 0`）（LIR-SVC `:215-218`）。
- **弹药源**：扫描时 **非弹匣** 且 `amount != 0` 且 `MaxAmountAsByte != 0` 的物品进 `ammoBoxMap`（LIR-SVC `:235-256`）。同类弹匣 **不会** 当弹药源。
- 主匹配：弹匣全部 `FillTargetItem` 蓝图的 supplies ID（LIR-SVC `:220-221,712-737`）。
- Fallback：主路径一个候选都没有、且弹匣有 calibers 时，才按 caliber 交集匹配 **`ItemCaliberAsset`**（注释写明「普通弹药箱不会触发此路径」）（LIR-SVC `:292-300,740-772`）。
- 每个未满弹匣按候选列表顺序从弹药源扣量，直到满或源尽（LIR-SVC `:273-351`）。

**是否填弹药箱**：是。功能 B 的弹药源就是弹药箱（及 caliber fallback 下的 caliber 物品），不是其它弹匣。

**成功反馈**：本地 toast `"一键压弹：成功压入 N 发子弹"`，时长 2.5s（LIR-NET `:203-205`，LIR-POL `:50`）。功能 A 无 toast。

### 1.3 范围（页 2..6）

| 常量 | 值 | 对应原生 |
|---|---|---|
| `ReloadRuntimePolicy.MinRepackPage` | 2 | `PlayerInventory.SLOTS`（Hands） |
| `ReloadRuntimePolicy.MaxRepackPage` | 6 | `PlayerInventory.PANTS` |

证据：LIR-POL `:43-47`；U3-PINV `:64-79`。页 0/1 装备槽、页 7 STORAGE、页 8 AREA **均不进 A/B**。事件范围越界直接丢（LIR-CON `:61-62`）。

### 1.4 冷却 / 窗口 / 其它常量

| 常量 | 值 | 作用 |
|---|---|---|
| `DoubleClickWindowSeconds` | 0.3s | 功能 B 双击窗 |
| `CooldownSeconds` | 1.5s | A/B 共用每玩家事务冷却 |
| `ReplayWindowSeconds` | 120s | 已观察 request id 重放窗 |
| `WorkTtlSeconds` | 3s | 入队未执行则丢 |
| `QueueLimit` | 64 | 待处理发送者队列 |
| `MaxPendingSuccesses` | 32 | 成功 toast 队列 |
| `MaxPerFrame` | 16 | 每帧 drain |
| `GateMaxEntries` | 128 | 闸门字典硬顶 |
| `ToastDurationSeconds` | 2.5s | 功能 B toast |
| `DiagnosticIntervalSeconds` | 5s | 诊断节流 |

全部在 LIR-POL。这些 **不是公开设置**（LIR-POL `:9` 仍写「only `enabled` persists」——该句相对 DEV-V4-04 已过时，见 §6）。

A 与 B 共用同一闸门：整理后立刻双击，或 1.5s 内两次双击，第二次会被 `RejectedCooldown` 吃掉。

---

## 2. `UseableGun.updateInfo` 与弹药 HUD

### 2.1 `ammoLabel` 怎么格式化

本地玩家装备枪时创建 `infoBox`，挂到 **`PlayerLifeUI.container`**（不是 `PlayerUI.container`）：

- `infoBox`：`PositionScale_X=0.7`，`PositionScale_Y=1`，`PositionOffset_Y=-70`，`SizeScale_X=0.3`，`SizeOffset_Y=70`（U3-GUN `:3649-3655`）。
- `ammoLabel` 是 `infoBox` 第一个子控件：`SizeScale_X=0.35`，`SizeScale_Y=1`，`FontSize=Large`（U3-GUN `:3657-3661`）。
- 同盒还有 `firemodeLabel`（右上）和 `attachLabel`（右下）（U3-GUN `:3663-3677`）。

`updateInfo()`（U3-GUN `:5344-5385`）：

```
ammoLabel.TextColor = ammo < equippedGunAsset.ammoPerShot ? ESleekTint.BAD : ESleekTint.FONT;
ammoLabel.Text = localization.format("Ammo", ammo, thirdAttachments.magazineAsset != null ? thirdAttachments.magazineAsset.MaxAmount : 0);
```

- 第一参数 `ammo`：当前膛内/匣内发数（`byte`，来自 `player.equipment.state[10]`，U3-GUN `:343,3513`）。
- 第二参数：有弹匣资产则 `magazineAsset.MaxAmount`（`int`，即资产 `Amount` 字段，U3-ASSET `:345-351`）；无弹匣则 `0`。
- 颜色：当前弹量小于 `ammoPerShot` 时 BAD（红），否则 FONT。
- 本地化：`Localization.read("/Player/Useable/PlayerUseableGun.dat")`（U3-GUN `:3542`），`Local.format(key, arg0, arg1)` = `string.Format(text, arg0, arg1)`（U3-LOC `:116-123`）。SDK 树没有该 `.dat`，常见英文本是 `{0}/{1}` 形态，但 **本报告不把未入库的 dat 当证据**。
- 枪模上可选 `Ammo_Counter` `UnityEngine.UI.Text` 只写 `ammo.ToString()`，不含容量、不含后备（U3-GUN `:221-223,3387-3394,5349-5356`）。

### 2.2 有没有「后备弹匣数 / 总备弹」现成 API

**没有给 HUD 用的现成总数/后备匣数字段。** `UseableGun` 没有 `reserveAmmo` / `spareMagazines` / `totalAmmo` 属性。

原版为 **换弹与挂匣 UI** 准备的是一次性搜索列表，不是累计 API：

- 静态缓冲 `magazineSearchResults`（U3-GUN `:350`）。
- 单击 R：`player.inventory.FindAttachmentsByCaliber(magazineSearchResults, EItemType.MAGAZINE, equippedGunAsset.magazineCalibers, allowZeroCaliber)`，再挑 **amount 最高** 的一条发 `SendAttachMagazine`（U3-GUN `:4001-4030`）。
- 打开挂件面板时同样搜索，结果画成 `SleekJars` 图标格，不显示「N 个后备匣」或「总备弹」数字（U3-GUN `:5290-5306`）。

`FindAttachmentsByCaliber` 参数（U3-PINV `:296-310`）：

- `IncludeEquipmentSlots = false` → 从 page `SLOTS`(=2) 起。
- `IncludeActiveStorageContainer = false` → 结束页 `STORAGE-1` = 6。
- `IncludeEmpty = false`。
- 只收 `EItemType.MAGAZINE` 且口径匹配。

因此原版能枚举「身上兼容非空弹匣」，但：

1. 不求和 `amount`；
2. 不含弹药箱（B 的压弹源）；
3. 不含装备槽里的枪自带弹匣（那是 `ammo` / `state[10]`）；
4. `updateInfo` **从不读** 这份列表。

若要「后备匣数 / 总备弹」，必须自扫库存（可复用 `FindAttachmentsByCaliber` 再 sum，或像 LIR-SVC 那样扫 2..6 含弹药箱）。没有可直接读的字段。

---

## 3. BUE 是否已 patch 弹药 HUD；LHT 占用哪块 UI

### 3.1 LIR / 弹药 HUD

BUE **没有** Harmony patch `UseableGun.updateInfo`、`ammoLabel`、`infoBox`。LIR 只装两个补丁（LIR-MOD `:368-369`）：

| 补丁 | 目标 | 作用 |
|---|---|---|
| `UseableGunReceiveAttachMagazinePatch` | `UseableGun.ReceiveAttachMagazine` | 记录新匣槽位，供旧匣原位放回（LIR-P1） |
| `ForceAddItemPatch` | `PlayerInventory.forceAddItem(Item, bool)` | 有换弹上下文时原位写入旧匣（LIR-P2） |

全仓库 `HarmonyPatch` 命中 `UseableGun` / `PlayerLifeUI` / `updateInfo` / `ammoLabel` 的只有上述 ReceiveAttachMagazine 与 LHT 的 `PlayerLifeUI` 构造函数。弹药数字 HUD 仍是原版 `updateInfo`。

功能 B 的反馈是底部 `PlayerUI.message(EPlayerMessage.NPC_CUSTOM, …)` toast（`LirToast.cs`），不是改 `ammoLabel`。

### 3.2 LHT 尸潮 HUD（不是弹药 HUD）

LHT 在 `PlayerLifeUI` **构造函数 postfix** 往 **`PlayerLifeUI.container`** 注入一个 `ISleekLabel`（LHT-HUD `:29-30,187-252`）：

| 项 | 值 |
|---|---|
| 尺寸 | 800×35 |
| 水平 | `PositionScale_X=0.5`，`PositionOffset_X=-400`（居中） |
| 垂直 | `PositionScale_Y=0`，`PositionOffset_Y=80` |
| 对齐 | `MiddleCenter` |
| 默认 | `IsVisible=false`，空文本 |
| 颜色 | 不设 TextColor，靠富文本 |

原版指南针 `compassBox`：宽 360、高 50，水平居中，`PositionOffset_Y` 默认 0，Arena 才改为 60（U3-LIFE `:1769-1776,802`）。LHT 注释写「指南针下方」（LHT-HUD `:20-21`）。800×35 @ Y=80 落在指南针条（高 50）之下。

原版弹药 `infoBox` 在容器 **右下**（`Scale_X=0.7, Scale_Y=1, Offset_Y=-70`）。LHT 在 **顶部水平居中**。二者同父容器、不同象限，现网几何不相交。

LHT 占用的是 `PlayerLifeUI.container` 顶栏指南针下方一条 800×35 标签，**不是** `ammoLabel` / `infoBox`。后续若在 `infoBox` 旁加后备弹数字，与尸潮条无现成重叠；若在屏幕顶部加第二行，才需要避让 LHT Y=80 带。

---

## 4. 原版 `PlayerSkills`：经验、技能树、`askSpend`、单人默认

### 4.1 经验怎么加减

公开字段 `uint experience`（U3-SK `:136-137`）。服务端改经验的主入口：

| API | 行为 | 条件 |
|---|---|---|
| `askSpend(uint cost)` | 减经验 | 本地玩家只发 `SendExperience(experience - cost)`；非本地先 `_experience -= cost` 再复制（U3-SK `:436-450`） |
| `askAward(uint award)` | 加经验 | 对称（`:453-467`） |
| `ServerSetExperience(uint)` | 差额转 `askAward`/`askSpend` | `:470-479` |
| `ServerModifyExperience(int delta)` | 正奖负花；负向夹到现有经验 | `:482-493` |
| `askPay(uint pay)` | 先乘 `Experience_Multiplier` 再加 | `:502-521` |
| `modXp` / `modXp2` | 本地字段 ±，发 `onExperienceUpdated`，**不**走 `SendExperience` | `:535-552` |

`askSpend` **不检查余额**。`ServerModifyExperience` 负向才 `Min(delta, _experience)`。`ReceiveUpgradeRequest` 用 `experience >= cost(...)` 才扣（`:584-589`）。插件直接 `askSpend` 可能把 `uint` 下溢（本地分支 `experience - cost` 无符号减）。

事件：`onExperienceUpdated`（实例）、`OnExperienceChanged_Global`（静态，加载不含）（`:108-117,231-236`）。

### 4.2 技能树结构

`Skill[][] skills`，`SPECIALITIES = 3`（U3-SK `:101,130-131,874`）。`InitializePlayer` 写死（`:872-905`）：

| 专精 | 槽数 | 枚举 | 构造 `Skill(level, max, baseCost, difficulty)` |
|---|---|---|---|
| OFFENSE 0 | 7 | OVERKILL, SHARPSHOOTER, DEXTERITY, CARDIO, EXERCISE, DIVING, PARKOUR | 0/7/10/1.0；0/7/10/1.0；其余 0/5/10/0.5，PARKOUR 0/5/20/0.5 |
| DEFENSE 1 | 7 | SNEAKYBEAKY … SURVIVAL | SNEAKYBEAKY 0/7/10/1.0；其余 0/5/10/0.5 |
| SUPPORT 2 | 8 | HEALING … ENGINEER | HEALING/AGRICULTURE 0/7/10/1.0；CRAFTING/COOKING/ENGINEER 0/3/20/1.5；其余 0/5/10/0.5 |

单槽 `Skill`：`level`、`max`、可选 `maxUnlockableLevel`、`cost = (baseCost + level * perLevelCostIncrease) * costMultiplier`（U3-SKILL `:11-82`）。升级走 `ReceiveUpgradeRequest` / `sendUpgrade`（U3-SK `:563-710`）。另有 Boost（`BOOST_COST = 25`）和 Horde 购买体积。

职业 `EPlayerSkillset`（NONE/FIRE/…/MEDIC）只对 **已有槽** 半价或免降级，不新增槽（U3-SK `:43-97,407-425`）。

地图 `LevelAsset.skillRules` 可改默认等级 / 最大可解锁 / 花费，不能加第 4 专精或第 8/9 进攻槽（`:907-933`）。`ServerSetSkillLevel` 只写已有索引（`:616-637`）。

**插件不能在不新建槽的前提下「加一条换弹技能」进原版树。** 原版树是固定 3×(7/7/8)。能做的是：花经验（`askSpend`）自己记账；或占用/覆盖某一原版槽（会与原版效果冲突）。`onApplyingDefaultSkills` 也只给已建好的 `Skill[][]`（`:14,1071`）。

### 4.3 插件能否 `askSpend` 而不新建技能槽

能。`askSpend` 是独立经验 API，不绑定技能槽。插件可：

- 服务端（含单机主机）对目标 `PlayerSkills` 调 `askSpend` / `ServerModifyExperience`；
- 自己持久化「换弹技能等级」（BUE 设置或自有存档）；
- **不**调用 `ReceiveUpgradeRequest`，因而 **不**改原版 22 槽。

约束：应在服务端调（单机 `Provider.isServer` 为真）。`askSpend` 无余额门，调用方必须自检。本地玩家分支只发包 `experience - cost`，非主机客户端直接调不会改权威字段。

### 4.4 单人默认有没有经验

`load()`（U3-SK `:951-998`）：

- Survival **且** 存在 `/Player/Skills.dat`（version > 4）→ 读存档经验。
- 否则（无存档、非 Survival、Arena/Horde 重置）→ `applyDefaultSkills()`，**该方法只改技能等级，从不写 `_experience`**（`:1031-1071`）。

因此 **新档 / 无存档时经验保持字段默认 `0`**。客户端非服务器角色会把 `_experience = uint.MaxValue` 当占位，等 `SendExperience`（`:942-946`）。单机是服务器角色，走 `load()`，新档为 0。

死亡：Survival 乘 `Lose_Experience_PvP/PvE`；Arena 置 0；其它非 Survival 乘 0.75（`:835-858`）。默认技能可能满级（`Spawn_With_Max_Skills`）或体力四项满级（`Spawn_With_Stamina_Skills`），与经验无关。

**结论**：原版单人新档默认 **0 经验、技能 0 级**（除非模式配置拉满技能）。要用原版经验做换弹技能，必须另有经验来源（击杀/任务/插件 `askAward`），或 BUE 自建进度。现网 LIR **完全不读** `PlayerSkills`。

---

## 5. 「超限弹匣」与功能 B 弹药箱优先级

### 5.1 原版有没有超过 `MaxAmount` 的路径

`ItemAsset.MaxAmount` / `MaxAmountAsByte` 就是 dat `Amount`，解析后若 `< 1` 则置 1（U3-ASSET `:345-351,910-913`）。

写入路径 **不夹紧到 MaxAmount**：

- `Item(ushort, byte amount, byte quality)` 和带 `state` 的重载直接赋值（U3-ITEM `:147-171`）。
- `Items.updateAmount` 只写 `items[index].item.amount = newAmount`（U3-ITEMS `:48-56`）。
- `PlayerInventory.sendUpdateAmount` → `updateAmount`，无 Max 检查（U3-PINV `:216-223,1335-1344`）。
- 换弹把 `jar.item.amount` 原样拷进 `state[10]` / `ammo`（U3-GUN `:2908-2911`）。

原版「填满」语义是 **写成 MaxAmount**，不是允许超过：

- 非 WORLD 起源构造：`amount = Max(MaxAmountAsByte, 1)`（U3-ITEM `:98-100`）。
- 卸载且 `shouldFillAfterDetach`：卸下数量改成 `MaxAmountAsByte`（U3-GUN `:2829-2832`）。这是卸下时补满到上限，不是超上限。

`byte amount` 硬顶 255。资产 `Amount` 也是 `uint8`。没有原版「容量+N」字段。

**存在超容的技术可能**（任意 `sendUpdateAmount` / 直接写 `item.amount`），**不存在原版玩法路径** 把弹匣填到 `> MaxAmount`。HUD 分母仍是 `MaxAmount`，超容会显示成 `当前 > 容量`。

### 5.2 LIR 有没有超容路径

没有。A/B 都把 `MaxAmountAsByte` 当天花板：

- A：目标数量 `min(bulletsLeft, maxAmount)`（LIR-SVC `:861-868`）。
- B：未满判定 `amount >= MaxAmountAsByte` 则跳过；转移 `Min(remaining, box.currentAmount)`，`remaining = maxAmount - currentAmount`（LIR-SVC `:218,276,319`）。

已 `amount > MaxAmount` 的匣（外部写入）在 B 里会被当成已满而跳过。LIR 不会制造超容，也不会把超容匣再压满。

### 5.3 功能 B 填弹药箱的优先级（先箱还是先其它弹匣）

**只填弹药箱，其它弹匣不是源。** 扫描把 `ItemMagazineAsset` 与「其它物品」分成两集；后者进 `ammoBoxMap`（LIR-SVC `:215-256`）。同 ID / 同口径弹匣不会给未满匣供弹。

箱内顺序：

1. 页 2→6，页内 `getItem` 下标升序收集弹药源（LIR-SVC `:188-257`）。Dictionary 按 asset id 分组，组内列表顺序 = 扫描顺序。
2. 未满匣同样 2→6 扫描序处理（`:273`）。
3. 每个匣：先按 `compatibleAmmoIds`（蓝图 supplies 顺序）取箱列表；有候选则 **不用** caliber fallback（`:279-300`）。
4. 同一弹药 ID 列表内按收集顺序扣（`:313-338`）。

没有「最满优先 / 最近优先 / 手持优先」。也没有「先其它弹匣、箱兜底」——匣间合并只属于功能 A。

**A vs B 产品边界**：A = 整理后同 ID 弹匣合并（不碰箱）。B = 双击 R 用弹药箱（及 caliber fallback 物品）填未满匣（不合并匣）。二者都受 1.5s 闸门。

---

## 6. LIR 设置面：`enabled` 退役后还剩什么

### 6.1 现网注册面

`InPlaceReloadFeatureRegistration.Registration` **只实现** `IFeatureRegistration`，**不实现** `IFeatureSettingsRegistration`（LIR-REG `:70-86`）。`ClientUi = null`。

对照：

- LIT：`IFeatureRegistration, IFeatureSettingsRegistration`，descriptor = `inventorytidy.mode` / `direction` 两条 Choice；legacy enabled **退役**（LIT-REG `:86-121`）。
- LHT：与 LIR 相同，无 settings facet（LHT-REG `:69-85`）。
- 宿主：无 facet → `SettingDescriptors = null` → `ComposeViewForStart` 返回 **诚实 null**，不造假页（SET `:80-89`，CTR `:73-74`）。

因此管理面板 LIR 详情 **没有设置行**。玩家仍看到的「启用」是 DEV-V4-04 **生命周期意图**开关（PANEL `:1016-1045`），不是 `SettingDescriptor`。

### 6.2 模块内残骸（未接线）

`InPlaceReloadModule` 仍保留：

- `EnabledSettingId = "inplacereload.enabled"`（LIR-MOD `:35`）。
- `CreateSettingsDescriptors` → 唯一 `ToggleDescriptor(...)`（`:55-57,419-426`）。
- `ReadToggle()`：`SettingsView == null` 时默认 **true**（`:410-416`）。
- `RefreshSwitches()` 读该 toggle 决定补丁装/卸（`:205-214`）。

生产注册 **从不调用** `CreateSettingsDescriptors`（全仓库唯一生产调用方是 LIT-REG `:118`）。因此：

- 生产 `SettingsView` 为 null，`ReadToggle()` 恒为 true。
- 模块 `Enabled` 在无 view 时一直为 true。
- 真正关功能走 `BueFeatureStartRuntime.SetFeatureEnabled` → `Stop` → `UninstallPatches`，不是写 `inplacereload.enabled`。

`ReloadRuntimePolicy` 注释「only `enabled` persists」（LIR-POL `:9`）是 DEV-V2-22 旧句，相对 DEV-V4-04 已过时。

### 6.3 legacy `inplacereload.enabled`

`BueLegacyEnabledMigrationAdapter` 仍把 `inplacereload.enabled` 列为六 alias 之一（MIG `:36-37,62-63`）。启动时从旧设置文档读 toggle；若为 false，经 `SetFeatureEnabled(feature, false)` 记 UserDisabled 意图，再从文档删该键。退役后 **schema 与面板都不再出现该键**。

### 6.4 还能给「技能等级」用什么 descriptor

**现网生产：零条 SettingDescriptor。** 技能等级若走 BUE 设置面，必须 **新加** facet（让 LIR 注册实现 `IFeatureSettingsRegistration`），不能复用已退役的 `inplacereload.enabled`。

可复用的平台能力（不是现成 LIR 行）：

- 生命周期「启用」= 功能总闸，不宜当技能等级。
- LIT 先例：facet 上挂 `SettingKind` Choice/Toggle 等，`ClientPreference` 权威，面板投影画行。
- `CreateSettingsDescriptors` 源码仍在，但生产未挂；若重新挂且仍只返回 enabled toggle，会与生命周期意图重复，且与「enabled 已退役」政策冲突。

常量（0.3s / 1.5s / 页 2..6）都不是设置。现网 LIR **无经验、无技能、无弹药 HUD**（HB `:14`；map.md 已建成事实）。

---

## 7. 对 T7 的直接含义（只陈述现状，不定案）

1. **两条现网能力不是同一条「压弹」**：A = 整理后同 ID 匣合并；B = 双击 R 用弹药箱填未满匣。二级「等待后自动压弹」若做成新触发，既不是 A 也不是 B。
2. **弹药 HUD 原版只有「当前/容量」**。后备匣数 / 总备弹需自扫；`FindAttachmentsByCaliber` 可列出兼容非空匣但不会求和、不含箱。BUE 未 patch `updateInfo`。LHT 占顶部指南针下 800×35，弹药盒在右下。
3. **原版经验可花、不可挂新槽**。`askSpend` 不必新建技能槽；单人新档经验为 0。用原版 XP 做换弹技能要解决经验来源，或改 BUE 自建进度。
4. **超限弹匣不是现网能力**。原版/LIR 都把 `MaxAmount` 当天花板；引擎写入可不夹紧，但没有玩法向超容。B 只从弹药箱（及 caliber fallback）填，不先掏其它匣。
5. **技能等级没有现成 LIR 设置行**。enabled 已迁到生命周期意图；生产 facet 为空。等级/开关要新 descriptor，不能复活 `inplacereload.enabled`。

---

## 8. 未在 SDK 树内核实的项

- `PlayerUseableGun.dat` 的 `Ammo` 字面（`{0}/{1}` 等）——运行时文件，不在 U3-SDK 仓库。调用是两参数 format，见 §2.1。
- 真机单击 R 与双击 R 的手感重叠：代码上单击仍走原版 `GetKeyDown(reload)` 换匣（U3-GUN `:4001`），双击另走 HostTick 检测；0.3s 内第二次会触发 B。本报告未做实机。
