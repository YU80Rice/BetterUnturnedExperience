# V5-R6 超限弹匣逐发供弹可行性

- **Ticket**: V5-R6
- **日期**: 2026-09-14
- **方式**: 只读对照一手源码。未改生产代码、未构建、未提交。
- **产品语义输入**: V5-T7 Answer Q5（超限 = 临时供弹会话，不是超容）。本票不推翻该语义，除非结论是「该语义在原版射击权威下做不到」。
- **范围**: U3-SDK `UseableGun` 开火/扣弹路径 + 现网 LIR 补丁。不设计技能 UI。不改码。

## 0. 一手来源

| 编号 | 来源 | 身份 |
|---|---|---|
| U3-GUN | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Useable\UseableGun.cs` | 开火、扣弹、换匣、HUD、弹道 |
| U3-IDX | 同上文件顶部 `GunStateIndices` | `AMMO = 10`、`MAGAZINE_ID = 8` |
| U3-EQ | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerEquipment.cs` | `simulate`/`tock`/`updateState`/`sendUpdateState`/`dequip` |
| U3-IN | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInput.cs` | 本地预测与主机重放 `tock` |
| U3-PINV | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInventory.cs` | `sendUpdateAmount` / 物品更新同步 / 卸枪 |
| U3-ITEMS | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Inventory\Items.cs` | `updateAmount` 写 `jar.item.amount` |
| U3-SRCH | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\InventorySearch.cs` | 主机逐量删除 `DeleteAmount` |
| U3-GAS | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Bundles\ItemGunAsset.cs` | `infiniteAmmo` / `ammoPerShot` / `shouldDeleteEmptyMagazines` |
| U3-NET | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\NetGen\NetInvokable\UseableGun_NetMethods.cs` | 换匣/开火 RPC 校验 |
| LIR-P1 | `src/BetterUnturnedExperience.Lir/Patches/UseableGunReceiveAttachMagazinePatch.cs` | 现网唯一 `UseableGun` 补丁 |
| LIR-P2 | `src/BetterUnturnedExperience.Lir/Patches/ForceAddItemPatch.cs` | 现网 `forceAddItem` 原位放回 |
| LIR-MOD | `src/BetterUnturnedExperience.Lir/InPlaceReloadModule.cs` | 只装上述两补丁；双击入口 |
| LIR-SVC | `src/BetterUnturnedExperience.Lir/Service/AmmoRepackService.cs` | 功能 B 箱→匣事务，逐槽 `sendUpdateAmount` |
| LIR-AUTH | `src/BetterUnturnedExperience.Lir/Service/LirProductionAuthority.cs` | 主机准入后才压弹 |
| LIR-NET | `src/BetterUnturnedExperience.Lir/Net/LirRepackNetwork.cs` | 客机只发请求，主机执行 |
| T7 | `.scratch/bue-v2-phase5-official-optimization/issues/07-t7-reload-skill-hud.md` | 超限产品语义冻结 |

行号以 2026-09-14 工作区文件为准。

---

## 1. 产品语义（输入，不是本票可改的范围）

V5-T7 Answer Q5 原话（T7 `:73-81`）：

> 三级双击 R 激活临时持续供弹：先消耗匹配弹药箱，再消耗备用匣余弹；**当前装上的匣不当备用源，激活瞬间不清空它**。不把 amount 写成无限，不把箱和匣合并进当前匣。内部应是供弹会话：主机按每次射击扣真实资源，客户端只显示状态，当前匣与备用资源分开记账，耗尽或退出则结束。

结束条件至少包括：匹配箱和备用余弹耗尽、换枪、卸下武器、死亡、不可操作、进入原版换弹、连接/会话代际变、最长持续时间、主机发现资源指纹不一致。优先资源耗尽 + 固定最大持续时间。

T7 同时把「逐发如何在不破坏原版射击权威下实现」标成 Speculative，并写明不能在实施票里直接写成「跳过当前匣扣弹」。本报告回答的就是这件事能不能做。

---

## 2. Q1 — 主机何时把 `ammo` / `state[10]` / `jar.item.amount` 减一？客户端预测开火是否本地先减？

### 2.1 三套数字不是一回事

| 字段 | 位置 | 含义 |
|---|---|---|
| `UseableGun.ammo` | 私有 `byte`（U3-GUN `:343`） | 膛内/当前匣发数的运行时镜像。装备时从 `state[10]` 拷入（`:3513`）；`updateState` 再从 `newState[10]` 覆写（`:4577-4579`）。 |
| `player.equipment.state[10]` | 枪物品 state 字节 10，常量 `GunStateIndices.AMMO`（U3-IDX `:33`） | 装备枪的持久弹药计数。开火时由 `fire()` 写入。 |
| 装备槽 `jar.item.amount` | 页 0/1 那把枪自己的 `Item.amount` | **开火不碰它。** 换匣时读的是**新匣**的 `jar.item.amount`（U3-GUN `:2908-2911`），不是枪物品的 amount。 |
| 备用匣 / 弹药箱 `jar.item.amount` | 页 2..6 | 开火路径完全不读。现网 LIR 功能 B 才用 `sendUpdateAmount` 改这些槽（LIR-SVC `:493`）。 |

原版射击权威只认 `UseableGun.ammo`（门槛）和 `state[10]`（持久化）。背包里的箱/备用匣不是射击记账单元。

### 2.2 开火时钟：谁跑 `tock` / `fire`

本地玩家每拍自己跑 `PlayerEquipment.simulate` 再 `tock`（U3-IN `:1636,1652`）。非本地、且本机是服务器时，主机按客机输入包重放同一对调用（U3-IN `:1810,1862-1864`）。`tock` 只在 `player.life.IsAlive` 时执行（`:1862-1864`）。

`UseableGun.tock` → 若仍在扣扳机/`bursts`/`fireDelay` 则 `tockShoot`（U3-GUN `:4547-4551`）。`tockShoot` 在射速窗过了之后：

```
if (ammo >= equippedGunAsset.ammoPerShot) { isFired = true; lastFire = clock; player.equipment.isBusy = true; fire(); }
else { /* 主机播空仓音效；bursts=0; isShooting=false */ }
```

（U3-GUN `:4506-4531`）

门槛是 **`ammo` 字段**，不是库存扫描。`ammoPerShot` 默认 1（U3-GAS `:136,1239`）。`infiniteAmmo` 为真时 `fire()` 不减弹，但 `tockShoot` 仍要求 `ammo >= ammoPerShot` 才能进 `fire()`（U3-GAS `:127-130`；U3-GUN `:884,4513`）。

`startPrimary` **不扣弹**，只置 `isShooting` / `wasTriggerJustPulled` / `fireDelayCounter`（U3-GUN `:3166-3210`）。真正消耗发生在随后的 `tock` 拍。

### 2.3 `fire()` 里减的是哪两份

`fire()` 注释写明「Called on server and owning client」（U3-GUN `:877-880`）。非无限弹药时：

1. 要求 `ammo >= ammoPerShot`，否则抛 `Insufficient ammo`（`:886-898`）。
2. `ammo -= ammoPerShot`（`:888`）。
3. 若 `action != EAction.String`：`player.equipment.state[10] = ammo`，然后 `player.equipment.updateState()`（`:890-893`）。

`updateState()`（无 send）只把当前 `state` 数组写回装备槽那件物品的 `item.state`（U3-EQ `:1874-1885` → U3-ITEMS `:76-87`）。**不发网。** 听服上本地玩家既是 owner 又是 server，这一步同时改了本机两份镜像。P2P 客机这一步只改客机自己的 `ammo` + 本地 `item.state` 拷贝；主机稍后在自己的 `fire()` 里改主机那份。

`state[10]` **没有** `sendUpdateState` 伴随每次射击。`sendUpdateState` 出现在：空匣删除（U3-GUN `:1282-1288,1307-1313`）、换匣（`:2921,2963`）、卡壳纠正走另一条 `SendPlayChamberJammed`（`:1357,3092-3131`）。常规逐发不同步 `state[10]` 给旁观者；旁观者只收不可靠 `SendPlayShoot` 特效（`:912-914`，U3-NET `ReceivePlayShoot` 为 `ONLY_FROM_SERVER`）。

### 2.4 客户端预测：是的，本地先减

纯客户端（`channel.IsLocalPlayer && !Provider.isServer`）：

1. 本地 `tockShoot` 见 `ammo >= ammoPerShot` 就进 `fire()`。
2. 本地立刻 `ammo -= ammoPerShot` 并 `state[10] = ammo` + 本地 `updateState()`。
3. 本地 `updateInfo()` 重画 HUD（`:1193`）。
4. 本地生成弹道并 `sendRaycast`（ballistics `:1909`）。
5. 主机收到输入包后 **独立** 再跑一遍 `tockShoot`/`fire()`，再减主机的 `ammo`/`state[10]`。
6. 命中权威在主机 `ballistics()`：`getInput(..., ERaycastInfoUsage.Gun)`（`:1916-1928`）。扣弹与命中是两条缝：扣弹在 `fire()`，伤害在后续 `tock` 的 `ballistics()`。

听服房主：`IsLocalPlayer && isServer`，`fire()` 只跑一次，不存在「先预测再主机校正」的第二趟扣弹。U3DS 上被控玩家不是 local，只有主机 `fire()` 扣弹；客机靠自己的预测副本先减，直到某次 `updateState`/`ReceivePlayChamberJammed` 把 `ammo` 拉回去。

卡壳是明确的「预测过冲再纠正」：`ReceivePlayChamberJammed(byte correctedAmmo)` 注释写 *「Since client can't predict chamber jams we fixup the predicted ammo count」*，然后 `ammo = correctedAmmo`（U3-GUN `:3093-3131`）。常规射击没有对等的逐发 ammo 校正 RPC。

### 2.5 `jar.item.amount` 何时变

- **当前枪物品 amount**：开火不减。换匣时主机用 `state[10]`（或 `shouldFillAfterDetach` 则满容）构造卸下的旧匣 `Item`（U3-GUN `:2827-2835`），再 `forceAddItem`。现网 LIR-P2 只改放回坐标，不改 amount。
- **新匣 amount**：`ReceiveAttachMagazine` 里 `ammo = jar.item.amount; state[10] = jar.item.amount`，然后 `removeItem` 把那本匣从背包拿走（`:2908-2914`）。
- **弹药箱 / 备用匣 amount**：原版射击不减。现网功能 B 在主机事务里 `inv.sendUpdateAmount(...)`（LIR-SVC `:493`）。`sendUpdateAmount` 先本地 `updateAmount`（写 `jar.item.amount`），再若非本地玩家则 `SendUpdateAmount` 给物主（U3-PINV `:1335-1344`；U3-ITEMS `:48-59`）。

**Q1 结论**：主机（以及拥有者客户端）在 `tockShoot` 通过弹药门槛后的 `fire()` 里把 `ammo` 和 `state[10]` 各减 `ammoPerShot`。客户端预测开火会本地先减这两份。背包 `jar.item.amount`（箱/备用匣）不在这条路径上；当前枪物品的 `amount` 也不在。要「按每次射击扣真实后备资源」，必须在 `fire()` 之外另开主机库存写入。

---

## 3. Q2 — 有哪些 Harmony 缝可以在「这一发消耗备用池而不是当前匣」时仍让原版认为弹匣未空？

目标约束来自 T7：不把 `amount` 写成无限、不把箱/匣合并进当前匣、当前匣不当源、激活瞬间不清空它；同时不触发换弹动画、不卡连发。

连发卡住的原版条件很硬：`tockShoot` 一旦 `ammo < ammoPerShot` 就 `isShooting = false; bursts = 0`（U3-GUN `:4522-4530`）。换弹动画由主机 `ReceiveAttachMagazine` 成功后 `SendPlayReload.InvokeAndLoopback` 触发（`:2924,2966` → `ReceivePlayReload` 置 `isReloading` 并 `play("Reload")`，`:3019-3057`）。单击 R 只是客机搜最高 amount 匣再发 `SendAttachMagazine`（`:4001-4030`）；动画权威在主机。

现网 LIR **没有**开火补丁。只装：

| 补丁 | 目标 | 与射击关系 |
|---|---|---|
| LIR-P1 | `UseableGun.ReceiveAttachMagazine` Prefix/Postfix | 只记新匣槽位；不碰 `fire`/`ammo`（LIR-P1；LIR-MOD `:368`） |
| LIR-P2 | `PlayerInventory.forceAddItem(Item, bool)` | 有换弹上下文才原位放回旧匣（LIR-P2；LIR-MOD `:369`） |

全仓库无 `UseableGun.fire` / `tockShoot` / `updateInfo` Harmony 命中（对照 LIR-MOD `:368-369` 与仓库 `HarmonyPatch` 检索）。下面是**可考虑的缝**，不是现成实现。

### 3.1 缝 A — Prefix `tockShoot` 或 `fire`：打前把 `ammo` 补回门槛

思路：会话激活且后备池仍有弹时，在原版读 `ammo >= ammoPerShot` **之前**把 `ammo`（及必要时 `state[10]`）抬到 ≥ `ammoPerShot`，让原版自己减当前匣那一发；主机再立刻从箱/备用匣扣等量，并把当前匣那一发「还回去」。

问题：

- `tockShoot` / `fire` 是 `private`（U3-GUN `:880,4431`）。Harmony 可补，但 BUE 现网刻意只补 `ReceiveAttachMagazine` 这一条公开 SteamCall。
- `fire()` 在 owner 客户端和主机各跑。Prefix 若两边都补弹，听服会双扣后备；只补主机则客机预测仍按真实当前匣递减，打空就停连发、弹 RELOAD，直到某次校正。T7 要求「客户端只显示状态」，与「客机必须自己维持 `ammo ≥ ammoPerShot` 才能连发」冲突。
- 原版减的仍是当前匣。要「不是当前匣」，必须在 `ammo -=` 之后把当前匣加回去。那是对射击权威账本的事后篡改，和「不跳过当前匣扣弹」的 T7 警告同一类。
- 空匣删除在 `fire()` 后半段看 **减完后的** `ammo == 0 && shouldDeleteEmptyMagazines`（`:1224-1288`）。若 Prefix 把空匣抬到 1 再让原版减回 0，仍会删匣。必须保证减完后 `ammo` 对原版仍 > 0，或再补一层删匣拦截。泵/轨/弦/火箭/霰默认 `shouldDeleteEmptyMagazines == true`（U3-GAS `:1013-1014`）。

### 3.2 缝 B — Prefix `fire`：会话中当作 `infiniteAmmo`，另账扣后备

`fire()` 开头 `if (!equippedGunAsset.infiniteAmmo)` 才减 `ammo`/`state[10]`（U3-GUN `:884-894`）。若会话中让这一枪走无限弹药分支，原版不减当前匣，连发门槛仍看 `tockShoot` 的 `ammo`——当前匣有弹时能打；当前匣打空后 `tockShoot` 仍停火。因此 **无限弹药分支救不了「当前匣空了还继续打」**。`tockShoot` 不读 `infiniteAmmo`。

要让空匣继续连发，必须同时抬 `tockShoot` 的 `ammo` 门槛。那就是把当前匣在权威账本里写成非空，直接违反「不把 amount 写成无限」。

### 3.3 缝 C — 激活时把后备打进当前匣（超容或反复填满）

激活时 `sendUpdateAmount` / 改 `state[10]` / 改 `ammo`，把箱和备用匣合并进当前匣，打完再还原。

这是 T7 **明确废止**的语义（T7 `:73-75`：「不把箱和匣合并进当前匣」；「废止 MaxAmount + 一半」）。即便 `Items.updateAmount` 不夹 `MaxAmount`（U3-ITEMS `:48-55`；V5-R5 已记录），HUD 第二参数仍是 `magazineAsset.MaxAmount`（U3-GUN `:5347`），旁观者/掉落/换匣会看到超容匣。且激活瞬间改变当前匣，违反「激活瞬间不清空它 / 分开记账」。

**此缝只能做近似，且改变容量语义。** 票面问题 4 把它列为「只能做近似」的例子。

### 3.4 缝 D — 拦截单击 R / `ReceiveAttachMagazine`，开火不补 `ammo`

即使后备供弹做成了，玩家单击 R 仍会走原版搜匣 + `SendAttachMagazine`（U3-GUN `:4001-4030`）。主机 `ReceiveAttachMagazine` 在 `isFired` 或换弹/拉机锤时拒绝（`:2800-2809`），但连发间隙 `isFired` 只维持约 0.15s（`:4351-4353`）。会话中一次成功换匣会：卸当前匣（按 `state[10]` 构造）、装上新匣、`SendPlayReload` 播动画、打断射击。T7 结束条件包含「进入原版换弹」。这不是供弹缝，是会话互斥缝：必须在主机拒绝换匣或客机吞掉 R。现网 LIR 双击 **不拦截** 单击原生换弹（LIR 输入驱动只认第二次按下）。

### 3.5 缝 E — 只在主机 `fire()` Postfix 扣后备，不改 `ammo`

这是「主机按每次射击扣真实资源」最干净的库存半截：`PlayerInventorySearchResultV2.DeleteAmount` 已是服务器逐量删除 API（U3-SRCH `:269-310`），或沿用 LIR-SVC 的 `sendUpdateAmount` + 指纹。

它 **不能** 单独满足「原版认为弹匣未空」。`tockShoot` 仍在当前匣打空时停火。客机 HUD 的 `ammo` 仍降到 0 并变 BAD 色（`:5346`）。结果是：后备被扣了，枪却打不出去——双重惩罚，不是超限。

### 3.6 为什么没有「安全且保语义」的单缝

原版把「能不能打下一发」和「当前匣还剩几发」绑死在同一个 `byte ammo` 上。T7 要把「能不能打」绑到后备池，同时当前匣分开记账且不能写成无限。这两件事在原版权威里是同一个变量。任何让空匣继续连发的补丁，都必须对 owner 客户端和主机同时伪造 `ammo >= ammoPerShot`。伪造一旦和真实 `state[10]` 漂移，换匣、空匣删除、卡壳校正、`updateState` 回放都会把会话打穿。

Harmony 技术上能补 `private void fire()` / `tockShoot`。现网 LIR 没有这条缝，也没有测试过与 `ReceiveAttachMagazine` 补丁的交织。这不是「找不到缝」，是「所有保连发的缝都要改射击权威账本」。

---

## 4. Q3 — 失败模式

下列均对照原版调用点。T7 已把其中多项列为会话结束条件；这里补「若仍试图继续供弹会怎样」。

### 4.1 切枪 / 卸下武器

主机 `PlayerEquipment.dequip` 发 `SendEquip` 卸枪（U3-EQ `:2018-2040`）。新枪 `equip()` 时 `ammo = state[10]`（U3-GUN `:3513`）。旧枪上伪造的 `ammo` 随 Useable 销毁；若会话仍扣新枪口径不匹配的箱，会错扣。T7 已要求换枪/卸武器结束会话。实施上必须挂在主机 `dequip`/`onLifeUpdated` 之前，不能靠客机 HUD。

### 4.2 死亡

`PlayerEquipment.onLifeUpdated(true)`：主机 `dequip()`，清 `isBusy`，装备坐标置 255（U3-EQ `:2922-2947`）。主机 `tock` 在 `!IsAlive` 时不跑（U3-IN `:1862-1864`）。`simulate` 开头 `player.life.isDead` 直接 return（U3-EQ `:2741-2744`）。死亡后不会再 `fire()`，但会话元数据若残留，复活切枪可能误激活。掉枪与否看 `Lose_Weapons_PvP/PvE`（`:2926-2934`）。

### 4.3 换匣 / `ReceiveAttachMagazine`

主机入口（U3-GUN `:2792-2968`）：

- `isBusy` / `isFired` / `isReloading|isHammering|isUnjamming|needsRechamber` → 拒绝。
- `ONLY_FROM_OWNER` + `ratelimitHz = 2`（`:2791`；U3-NET 读侧再 `IsOwnerOf`）。
- 成功路径改 `ammo` 与 `state[10]` 为**新匣** `jar.item.amount`，`removeItem` 新匣，`forceAddItem` 旧匣（按当时 `state[10]`），`sendUpdateState`，`SendPlayReload` 全客户端含 loopback。

失败交叉：

1. 会话中伪造的 `ammo`/`state[10]` 会被新匣真实 amount **覆盖**。若刚才为保连发把空匣写成非空，卸下的旧匣会带着伪造 amount 进背包 → 凭空造弹。
2. 现网 LIR-P1/P2 会在这次调用里记录槽位并原位放回。超限若再 Prefix 同一方法，必须与 Guard 生命周期兼容：P1 Prefix `Reset`+`BeginReload`，Postfix `Reset`（LIR-P1 `:22-47`）。
3. 单击 R 与双击 R 共用 `ControlsSettings.reload`。LIR 双击窗 0.3s 且不吞第一次（现网功能 B）。超限激活后玩家再点 R，原版换匣与会话结束条件「进入原版换弹」竞态。
4. `page == 255` 是卸匣不换（LIR-P1 不记录；U3-GUN `:2932-2966`）。会话中卸匣把 `state[10]=0`，下一拍 `tockShoot` 停火。

### 4.4 弹药箱在射击中被整理走 / 拖走 / 压弹

`fire()` 不锁库存。同一主机帧内 LIT 整理、LIR 功能 A/B、玩家拖拽都可以改页 2..6。

- 功能 B 的 `CommitRepack` 按槽位 preimage 校验，漂移则回滚/隔离（LIR-SVC `:470-529`；LIR-AUTH `:43-45`）。
- 逐发若用「记住的 (page,x,y)」而箱已被移走，`getIndex` 失败或 `RequireExactSlot` 失败。T7 要求「主机发现资源指纹不一致」则结束。
- 功能 A 合并同 ID 匣会删空匣、改 amount（LIR-SVC 合并路径）。超限若正把某备用匣当源，合并会让源槽消失。
- `onItemRemoved`：若移除的是当前装备格，主机会 `dequip`（U3-PINV `:1754-1766`）。后备箱被移走不会卸枪，只会让下一发无源。
- 射击是 50Hz `tock`（`BALLISTICS_DELTA_TIME = 0.02f`，U3-GUN `:1651`）。全自动每发都可能与整理事务交错。LIR 功能 B 是整包事务 + 1.5s 闸；逐发没有等价闸。

### 4.5 P2P 客机伪造

分层：

**原版射击**

- 开火输入随 `PlayerInput` 包走，主机重放 `tockShoot`。客机本地先减弹不能让主机多生成伤害：主机自己的 `ammo` 不够就不 `fire()`，`ballistics` 没有对应 `BulletInfo`。
- `SendPlayShoot` 仅服务器→客户端（U3-NET）。客机不能广播自己在开枪。
- `ReceiveAttachMagazine` 仅 owner，2 Hz。伪造换匣最多把主机上自己的枪换匣，不能改别人库存。
- 弹道 `sendRaycast` 主机校验距离（U3-GUN `:1941-1956`）。这是命中伪造，不是弹药伪造。

**若超限在客机预测侧维持 `ammo`**

- 客机可以把本地 `ammo` 保持非零并持续发开火输入。主机若**没有**同步维持非零，主机 `tockShoot` 停火，客机空放、主机不扣后备也不出伤 → 不同步，不是无限弹药。
- 主机若**同样**为会话伪造 `ammo`，则客机持续扣扳机 = 主机持续 `fire()`。此时无限与否完全取决于主机是否每发都成功 `DeleteAmount`。漏扣、错槽、整理走源、异常被吞 → 主机在打免费弹。这是「会话实现 bug = 刷弹」，不是原版 RPC 伪造。

**LIR 频道**

- 现网压弹：客机只 `SendToServer` 请求 id；主机 `Admit` 解析玩家 + 闸 + 事务（LIR-NET `:221-252`；LIR-AUTH `:28-38,91-96`）。回复按 session 定向。未知/重复 request id 拒绝（LIR-NET 文件头 `:31-32`）。
- 超限若复用「客机请求、主机执行」且每发打频道：全自动 ~10 发/秒会打满可靠通道；LIR 闸 1.5s、队列 64、每帧 16（V5-R5 常量表）不适合逐发。
- 超限若改「客机本地改库存」：P2P 客机对 `sendUpdateAmount` 的非物主分支不会发到主机（U3-PINV `:1341-1343` 条件是 `!IsLocalPlayer && ownerHasInventory`）。客机改自己看见的箱不会改主机库存 → 客机以为在耗后备，主机箱还在。必须主机写。

**听服房主**

房主 `IsLocalPlayer && isServer`，预测与权威同一趟 `fire()`。伪造面比纯客机小，但 Prefix 若按「每端各扣一次」会双扣。

### 4.6 其它会打穿会话的原版行为

| 行为 | 证据 | 风险 |
|---|---|---|
| 卡壳 | `SendPlayChamberJammed.InvokeAndLoopback(..., ammo)` 把客户端 `ammo` 拉回主机值（U3-GUN `:1345-1357,3098-3131`） | 会话中伪造的客机 `ammo` 被纠正；若纠正到 0 则连发停 |
| 空匣删除 | `ammo==0 && shouldDeleteEmptyMagazines` 清 MAGAZINE_ID 与 AMMO 并 `sendUpdateState`（`:1224-1288`） | 当前匣被删；T7 说激活不清空当前匣 |
| 弦/箭 `EAction.String` | `fire()` 不写 `state[10]`（`:890-894`）；另走 MAGAZINE_QUALITY / 每发删匣 | 供弹模型不是「匣内发数」 |
| `infiniteAmmo` 枪 | `fire()` 不减弹；`tockShoot` 仍要 `ammo>=ammoPerShot` | 超限无意义或双重无限 |
| 炮塔 `isTurret` | `updateState()` 直接 return，不写回物品（U3-EQ `:1876-1878`） | 状态账本不同 |
| `ReceivePlayReload` | 置 `isShooting=false`、`isBusy=true`、`isReloading=true`（U3-GUN `:3035-3043`） | 换弹动画期间 `tockShoot` 因 `isReloading` 取消射击（`:4434`） |

---

## 5. Q4 — 结论

**本阶段应把超限降级，或移出第五阶段。**

三选一裁决：**must defer or downgrade**（不是「可安全做」，也不是「本阶段做近似超容」）。

### 5.1 为什么不是「可安全做」

T7 冻结的语义要求同时成立：

1. 当前匣与后备分开记账，激活不清空当前匣，不把箱/匣合并进当前匣，不把 amount 写成无限；
2. 主机按每次射击扣真实后备；
3. 不破坏原版 `UseableGun` 射击权威；
4. 客机不能无限、主机不能不同步；
5. 当前匣空了仍能连发（否则「超限」相对原版没有射击侧效果——原版空匣本就会停火去换匣）。

原版把 (5) 的门槛和 (1) 的当前匣账本绑在同一个 `byte ammo` 上（U3-GUN `:4513-4520,884-893`）。要满足 (5) 必须在 owner+host 两侧让 `ammo >= ammoPerShot` 在后备耗尽前恒成立，这直接否定 (1) 的「不把 amount 写成无限」。要满足 (1) 则 (5) 做不到：当前匣一空，`tockShoot` 停火，后备扣了也打不出去。

现网 LIR 没有开火缝（LIR-MOD `:368-369`）。新补 `private fire`/`tockShoot` 会进入原版射击权威，与卡壳校正、空匣删除、换匣 `sendUpdateState`、听服单次 `fire()` 交织。这不是第五阶段「官方功能优化」能在不改射击权威的前提下切开的缝。

### 5.2 为什么不建议本阶段做「近似超容」

票面举例：「激活时把备用打进当前匣再打完还原」。这会：

- 改变容量语义（T7 已废止超容）；
- 换匣/死亡/整理会把超容匣写进真实库存；
- HUD 分母仍是 `MaxAmount`（U3-GUN `:5347`），分子超分母；
- 还原失败 = 刷弹或丢弹（LIR 对压弹已有 RestoreFailed 隔离先例，LIR-AUTH `:43-45`，逐发没有等价事务边界）。

近似超容不是「差一点的超限会话」，是另一种产品。T7 不许本票推翻 Q5，除非「该语义做不到」。做不到 → 降级或延期，不是改做成超容。

### 5.3 第五阶段可落地的降级（仍是压弹，只改供弹顺序）

与现网功能 B 同一条权威缝，不碰 `UseableGun.fire`：

- 三级双击仍走主机 `TryRepackTransactional`（LIR-AUTH `:28-38`；LIR-SVC）。
- 只改 `BuildRepackPlan` 的源优先级：先匹配弹药箱，再（若产品允许）拆备用匣余弹填当前口径未满匣。现网 B 明确「同类弹匣不会当弹药源」（LIR-SVC `:235-256`）；「再消耗备用匣」是新规则，但仍是一次性填匣，不是逐发。
- 当前匣继续是原版 `ammo`/`state[10]`。打空照常停火、单击 R 照常换匣。
- HUD「超限已激活」若仍要做，只能表示「这次压弹用了箱优先」，不能表示「空匣还能打」。

这保住 T7 的「先箱后匣」优先级，丢掉「临时供弹会话 / 当前匣空了继续打」。文案不要再叫「超限弹匣」若玩家会理解成无限匣。

### 5.4 若坚持完整会话语义，应移出第五阶段

完整语义需要至少：

- Harmony `tockShoot`+`fire`（owner 与 host 分流，听服防双扣）；
- 主机每发 `DeleteAmount`/`sendUpdateAmount` + 槽指纹（射击 50Hz 与 LIT/LIR 事务交错）；
- 拦截或结束于 `ReceiveAttachMagazine`、`dequip`、死亡、卡壳 RPC、空匣删除；
- 客机 HUD 与 `ammo` 解耦（否则预测空匣会停连发）；
- 伪造面审计：主机漏扣 = 免费弹。

这是新射击子系统，不是 LIR 压弹优化。应另开阶段/票，并准备把 T7 Q5 从「冻结语义」改成「需修订」——那是产品票，不是本研究票的权限。

### 5.5 对照表

| 方案 | 当前匣分开记账 | 空匣继续连发 | 不改射击权威 | 主机真实扣后备 | 第五阶段 |
|---|---|---|---|---|---|
| T7 字面会话 | 要 | 要 | 要 | 要 | 做不到 |
| Prefix 伪造 `ammo` + 扣后备 | 假（权威 `ammo` 非空） | 能 | 否 | 能 | 不安全 |
| 激活合并进当前匣 | 否 | 能直到打完 | 基本不补 fire | 一次扣 | 语义违规 |
| 只扣后备不抬 `ammo` | 是 | 否 | 是 | 能 | 无射击效果 |
| 降级：B 的源改为先箱后匣 | 是 | 否 | 是 | 一次压弹时扣 | 可做 |

---

## 6. 明确不在本报告范围

- 技能 UI、经验数值、超限冷却/最长持续秒数（T7 待定）。
- 实施设计（不写补丁代码）。
- 推翻 T7 Q5 的产品重裁（只判定该语义在原版射击权威下本阶段做不到）。
- 枪模 `Ammo_Counter`、U3DS HUD（T7 Q1/Q6：U3DS 不画，仍执行权威逻辑——降级后权威逻辑仍是压弹事务，U3DS 可跑）。

---

## 7. 一句话

原版用同一个 `ammo` 既当连发许可又当当前匣账本；T7 超限会话要把这两件事拆开。现网 LIR 只补换匣放回，不补开火。第五阶段不要做逐发供弹；把三级降为「仍是压弹、只改先箱后匣的顺序」，完整会话移出本阶段。




