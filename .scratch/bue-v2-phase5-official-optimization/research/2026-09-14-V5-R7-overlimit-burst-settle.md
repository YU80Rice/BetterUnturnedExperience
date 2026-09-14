# V5-R7 限时超限（开火不扣匣、结束时折算后备）可行性

- **Ticket**: V5-R7
- **日期**: 2026-09-14
- **方式**: 只读对照一手源码。未改生产代码、未构建、未提交。
- **产品语义输入**: 双击 R 进入一段时间：开火不消耗当前匣、不需要换弹；窗口结束（或换枪/死亡等）后再从匹配弹药箱 + 备用匣余弹一次性折算打出的发数。折算得上 → 默认技能冷却；折算不上（亏空）→ 惩罚冷却。当前装上的匣不当源。不是一次压弹，也不是边打边扣箱。上限 = 激活时主机记下的后备可覆盖发数。
- **对照**: [V5-R6](2026-09-14-V5-R6-overlimit-supply.md)（逐发供弹）。本票不重抄完整开火路径。
- **范围**: U3-SDK `UseableGun` 开火/扣弹/连发门槛 + 现网 LIR 压弹权威缝。不设计技能 UI。不改码。

## 0. 一手来源

| 编号 | 来源 | 身份 |
|---|---|---|
| U3-GUN | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Useable\UseableGun.cs` | 开火、扣弹、换匣、HUD |
| U3-IDX | 同上 `GunStateIndices.AMMO = 10` | 枪物品 state 弹药字节 |
| U3-EQ | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerEquipment.cs` | `updateState` / `sendUpdateState` / `dequip` / `onLifeUpdated` |
| U3-IN | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInput.cs` | 本地预测与主机重放 `tock` |
| U3-PINV | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInventory.cs` | `sendUpdateAmount`；装备格移除则 `dequip` |
| U3-ITEMS | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Inventory\Items.cs` | `updateAmount` / `updateState` |
| U3-SRCH | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\InventorySearch.cs` | 主机 `DeleteAmount` |
| U3-GAS | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Bundles\ItemGunAsset.cs` | `infiniteAmmo` / `ammoPerShot` / `shouldDeleteEmptyMagazines` |
| LIR-MOD | `src/BetterUnturnedExperience.Lir/InPlaceReloadModule.cs` | 现网只装换匣/放回两补丁 |
| LIR-P1 | `src/BetterUnturnedExperience.Lir/Patches/UseableGunReceiveAttachMagazinePatch.cs` | 只记新匣槽位 |
| LIR-SVC | `src/BetterUnturnedExperience.Lir/Service/AmmoRepackService.cs` | 功能 B 整包 `sendUpdateAmount` |
| LIR-AUTH | `src/BetterUnturnedExperience.Lir/Service/LirProductionAuthority.cs` | 主机准入；`RestoreFailed` 隔离 |
| R6 | `.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R6-overlimit-supply.md` | 逐发供弹不可行 |
| T7 | `.scratch/bue-v2-phase5-official-optimization/issues/07-t7-reload-skill-hud.md` | 超限 = 临时供弹会话 |

行号以 2026-09-14 工作区文件为准。

---

## 1. 与 R6 的差异：延后结算是否绕开逐发改背包

R6 结论（R6 §5 / 票 14 Answer）：原版同一个 `byte ammo` 既是 `tockShoot` 连发许可，又是当前匣账本（`fire()` 写 `state[10]`）；客机预测本地先减这两份；背包 `jar.item.amount` **不在开火路径上**。要空匣继续连发，必须在 owner+host 伪造 `ammo >= ammoPerShot`；换匣会把伪造量写进真实库存；主机漏扣后备 = 免费弹。现网 LIR 不补开火。

R6 真正做不到的不是「逐发改弹药箱」——原版开火本来就不改箱。R6 做不到的是：**许可与当前匣账本拆不开**，再叠加「主机每发 `DeleteAmount`/`sendUpdateAmount` 与 LIT/LIR 50Hz 交错」。

本票产品把库存半截从「每发扣箱」改成「窗口结束一次性折算」。对照：

| 面 | R6 逐发供弹 | R7 限时爆发再折算 | 延后是否绕开 |
|---|---|---|---|
| 弹药箱 / 备用匣 `jar.item.amount` | 主机每发写库存 | 结束时一次 `sendUpdateAmount`（与 LIR-SVC 整包事务同类） | **绕开** R6 的 50Hz 背包写入 |
| `tockShoot` 门槛 `ammo >= ammoPerShot` | 空匣必须伪造 | 空匣仍然必须伪造；非空匣可「冻结不减」 | **不绕开** |
| `fire()` 写 `state[10]` | 每发改当前匣账本，再试图还回去 | 产品要求期间不扣匣 → 必须跳过这条写入，或写完再冻回 | 不绕开许可耦合；可避免把假量写进枪 `state` |
| 当前匣不当源 | 与许可冲突 | 同样冲突（空匣） | 同因 |
| 换匣把伪造量放回背包 | 有 | 若只伪造 RAM `ammo`、不写 `state[10]`，则枪物品账本可保持激活前；**仍必须在 `ReceiveAttachMagazine` 前结束/拒绝** | 减轻，不是消除 |
| 漏扣 = 免费弹 | 每发漏扣 | 窗口内伤害已生效，结束折算失败 = 已出伤未付款 | **更晚暴露**，不是更安全 |

**一句话**：延后结算绕开的是 R6 的「逐发改背包」，不是 R6 的「空匣连发必须伪造 `ammo`」。非空匣冻结不减 + 结束扣箱，是另一条近似产品；空匣不换弹连发与 R6 同因。

---

## 2. Q1 — 空匣限时连发必须动哪些字段？owner 是否仍本地先减 `ammo`？

### 2.1 门槛与账本仍是同一变量（R6 §2，不重抄路径）

连发许可只读运行时 `UseableGun.ammo`：`tockShoot` 在射速窗过了之后 `if (ammo >= equippedGunAsset.ammoPerShot) { isFired=true; fire(); } else { bursts=0; isShooting=false; }`（U3-GUN `:4513-4530`）。`ammoPerShot` 默认 1（U3-GAS `:136,1239`）。`infiniteAmmo` 只让 `fire()` 跳过减法（U3-GUN `:884`），**不**让 `tockShoot` 放过空匣（R6 §3.2）。`startPrimary` 不扣弹，只置 `isShooting`（`:3166-3210`）。

`fire()` 在 server **和** owning client 上跑（注释 `:877-880`）。非无限时：`ammo -= ammoPerShot`；非弦则 `state[10] = ammo` + `updateState()`（`:886-893`）。`updateState()` 把枪 `state` 写回装备槽 `item.state`，**不发网**（U3-EQ `:1874-1885`）。常规逐发没有 `sendUpdateState`。

因此要让空匣在窗口内仍连发，**最低必动字段**：

1. **`UseableGun.ammo`（owner + host 两侧）**：必须在每次 `tockShoot` 读门槛前保持 `>= ammoPerShot`。只动主机：客机预测打空即停连发并弹 RELOAD（`:902-905,5346`）。只动客机：主机停火，客机空放无伤。听服 `IsLocalPlayer && isServer` 只跑一趟 `fire()`（R6 §2.4）。
2. **`tockShoot` 门槛本身**：要么 Prefix 抬 `ammo`，要么改/跳过 `:4513` 判断。没有第三缝。
3. **`state[10]`**：原版 `fire()` 会把它写成减完后的 `ammo`。产品要求「不扣当前匣、结束把当前匣恢复成激活前」。两条实现：
   - 会话中让 `fire()` 不走减弹分支（伪 `infiniteAmmo` 或 Prefix 跳过 `:884-893`），则 `state[10]` 可保持激活前；**但** `tockShoot` 仍要非空 `ammo`，所以必须把 RAM `ammo` 与持久 `state[10]` **拆开**——原版每次 `updateState(byte[] newState)` / 换匣 / 卡壳会把它们再焊上（`:4577-4579,2908-2911,3131`）。
   - 让原版照常减，再每发把 `ammo`/`state[10]` 冻回激活前。那就是 R6 缝 A：权威账本在门槛读点上非空，换匣仍可能读到伪造量。

**不必**为了连发去动背包箱/备用匣 `jar.item.amount`——开火路径本来不读它们（R6 §2.1）。这是延后结算唯一真正省掉的写入。

空匣删除看减完后 `ammo == 0 && shouldDeleteEmptyMagazines`（`:1224-1288,1307-1313`）。泵/轨/弦/火箭/霰默认删空匣（U3-GAS `:1013-1014`）。若伪造后仍落到 0，当前匣被 `sendUpdateState` 清 MAGAZINE_ID。要保匣必须保证减完后对原版仍 > 0，或拦截删匣。

### 2.2 owner 客户端预测：是的，本地仍先减

纯客户端（`channel.IsLocalPlayer && !Provider.isServer`）：本地 `tock` 先跑（U3-IN `:1636,1652`），`tockShoot` 见 `ammo >= ammoPerShot` 就 `fire()`，立刻 `ammo -=` 并本地 `state[10]=ammo`。主机稍后按输入包独立再跑一遍（U3-IN `:1810,1862-1864`；`tock` 仅 `IsAlive`）。

因此：

- **不补 owner**：窗口开始后客机按真实匣递减；匣空则本地停火，即使主机伪造许可。产品「不需要换弹」在客机上失败。
- **补 owner 冻结 `ammo`、不补减法跳过**：客机每发仍先减，必须每发再加回，否则下一拍门槛失败。卡壳 RPC `ReceivePlayChamberJammed` 会把客机 `ammo` 拉回主机传入值（U3-GUN `:3093-3131`）。
- **补 owner 走无限分支**：本地不减 `ammo`。HUD 分子冻结（`:5347` 读的是 `ammo`）。主机必须同步不减，否则两侧账本在窗口内就分叉，结束恢复无单一真值。

**Q1 结论**：空匣限时连发必须在 owner+host 动 `ammo` 和 `tockShoot` 门槛；若不想把假量写入枪物品，还必须让 `fire()` 不写 `state[10]`，并挡住所有把 `ammo` 从 `state[10]` 拉回来的路径。owner **仍会**按原版本地先减，除非补丁同时改 owner 的 `fire()`/`tockShoot`。延后结算不改变这条预测事实。

---

## 3. Q2 — 「期间不扣匣、结束再扣箱」能否做到票面步骤？

票面步骤拆成四句，分别能不能：

### 3.1 激活时主机记下后备可覆盖发数上限 — 能（现网缝）

与功能 B 同一条主机库存读缝：扫描页 2..6 匹配弹药箱 + 备用匣余弹（LIR-SVC 已区分匣 vs 箱；现网 B **明确同类匣不当弹药源**，LIR-SVC `:235-256`，R7 要把备用匣当源是新规则但仍是读库存）。记下 `cap = min(扫描总和, 设计最长窗口×射速上界)`。当前装上匣不当源，激活不清空它——这只是**不写**当前匣，做得到。

指纹（槽 page/x/y、id、amount）应在激活瞬间冻结为 **cap 的依据**，不是结算时的源清单。结算时源可能已被动。

客机不能自己定 cap：P2P 客机对非物主 `sendUpdateAmount` 不发到主机（U3-PINV `:1341-1343`）。必须主机记、主机结。

### 3.2 期间伪造/冻结连发许可，但不把假 `ammo` 写回背包匣 — 只能部分

「不把假 `ammo` 写回**背包匣**」：开火路径本来不写备用匣/箱的 `jar.item.amount`，延后结算在这一点成立。

「不把假 `ammo` 写回**当前枪物品**」：要求会话中 `fire()` 不执行 `state[10]=ammo; updateState()`。技术上 = Prefix 让会话走 `infiniteAmmo` 分支，或跳过 `:884-893`，同时另账累计 `shotsFired`（只在 **host** 的 `fire()` 计数；听服不要双计）。

但许可仍要非空 `ammo`：

- **激活时当前匣 `ammo >= ammoPerShot`**：可冻结 RAM `ammo` 不减、不写 `state[10]`。枪物品保持激活前。这是「满匣/有弹匣的限时免费连发」，不是「空匣不换弹」。窗口内打出的发数与匣内数字脱钩，HUD 分子冻结（U3-GUN `:5347`），玩家看到的不是打出的发数。
- **激活时当前匣空（`ammo < ammoPerShot`）**：必须把 RAM `ammo` 抬过门槛。只要任何路径把这份 RAM 写进 `state[10]`（漏拦的 `fire()`、空匣删除、换匣构造旧匣用 `state[10]` 或当时 `ammo`），假量进背包。若坚持不写 `state[10]`，则 `ammo`（许可）与 `state[10]`（真匣）在整个窗口分叉；`updateState(byte[])` 装备同步会把 `ammo = newState[10]` 拉回 0，连发立刻停。

所以票面「伪造/冻结许可但不写回背包匣」：

- 对**后备匣/箱**：是，延后结算做到了。
- 对**当前枪 `state[10]`**：只有在「不抬持久账本、只冻 RAM、且挡住所有写回」时成立；空匣场景必须抬 RAM，写回风险与 R6 同类。

现网 LIR **没有** `fire`/`tockShoot` 补丁（LIR-MOD `:368-369`）。这不是现成缝。

### 3.3 结束时只 `sendUpdateAmount` 弹药箱和备用匣 — 能（库存半截），有指纹洞

主机按 `min(shotsFired, cap)` 从箱再备用匣扣，API 与 LIR-SVC 相同：`sendUpdateAmount`（U3-PINV `:1335-1344` → U3-ITEMS `:48-59`）或 `DeleteAmount`（U3-SRCH `:269-310`）。这是整包事务，**绕开** R6 每发写入。应走 LIR 式 preimage + 回滚/隔离（LIR-AUTH `:43-45`），不要无校验逐槽扣。

限制：

- 结算时箱可能已被整理/拖走/功能 A 合并。激活 cap 是**上限**不是保证能扣到。扣不够 = 产品说的「亏空 → 惩罚冷却」。伤害已经打出去，惩罚冷却不能把子弹从目标身上拿回来。
- `DeleteAmount` 对装备中物品会先 `DequipIfEquipped`（U3-SRCH `:276`）。结算源必须排除当前装备格；当前匣本就不当源。
- 客机不得本地改库存。

### 3.4 把当前匣恢复成激活前的 amount — 条件成立

若窗口内从未让 `fire()` 写 `state[10]`，且从未 `sendUpdateState` 清匣，则当前匣 **本来就还是** 激活前，无需恢复。这是延后方案相对 R6「每发改完再还」的优点。

若窗口内原版已经减了 `state[10]`，结束时把 `ammo`/`state[10]` 写回激活前快照 + `sendUpdateState`（U3-EQ `:1902-1916`）能改回去——**除非**中间换匣/卸匣/删空匣，那时「当前匣」已不是激活那本，写回 = 改错物品或凭空造弹。

### 3.5 Q2 结论

票面四步不能作为「可安全做」的完整产品同时成立：

- cap 快照 + 结束扣箱/备用匣：可做，且确实绕开 R6 逐发背包突变。
- 期间不扣当前匣：仅当补丁让 `fire()` 不写 `state[10]`，并在换匣/删匣/卡壳/`updateState` 前回滚或拒绝。
- 空匣仍连发、不换弹：仍要伪造 `ammo`，与 R6 同因，不能安全。
- 「结束恢复当前匣」在「从未改过」时是空操作；在「改过又换过匣」时不安全。

---

## 4. Q3 — 失败模式

R6 §4 已列切枪/死亡/换匣/整理/P2P。这里只写 **延后结算特有的交叉**（窗口内已出伤、付款在结束时）。

### 4.1 期间换匣 / `ReceiveAttachMagazine`

主机入口（U3-GUN `:2792-2968`）：`isBusy`/`isFired`/`isReloading|isHammering|isUnjamming|needsRechamber` 拒绝；成功则 `ammo` 与 `state[10]` **覆盖为新匣** `jar.item.amount`，按当时 `state[10]`（或 `shouldFillAfterDetach` 满容）构造旧匣 `forceAddItem`，`sendUpdateState`，`SendPlayReload`。

`isFired` 只维持约 0.15s（`:4351-4353`）。连发间隙可以换匣。单击 R 搜最高 amount 匣再 `SendAttachMagazine`（`:4001-4030`）；现网 LIR 双击不吞第一次。

延后方案特有：

1. 若 RAM `ammo` 被抬过、且漏进 `state[10]`：卸下的旧匣带着伪造 amount → 造弹（与 R6 相同，不因延后消失）。
2. 若坚持不写 `state[10]`、RAM `ammo` 非零：换匣仍用 `state[10]` 构造旧匣（真值），然后新匣覆盖 `ammo`。会话许可被真实新匣替换。必须 **先结束会话再允许换匣**，或 Prefix 拒绝 `ReceiveAttachMagazine`。T7 结束条件已含「进入原版换弹」。
3. 换匣成功播 `ReceivePlayReload`：`isShooting=false`、`isReloading=true`（`:3019-3057`），`tockShoot` 因 `isReloading` 取消射击（`:4434`）。产品「不需要换弹」被原版动画打断。
4. `page==255` 卸匣：`state[10]=0`（`:2959-2963`）。下一拍停火。LIR-P1 不记录卸匣。
5. 结束恢复若在换匣之后仍把激活前 amount 写回 **新**枪状态：改的是新匣账本。必须把会话绑死到 `(equippedPage,x,y, magazineID, 激活时 state[10])`，换匣即 settle 或 abort。

与 LIR-P1 交织：P1 Prefix `Reset`+`BeginReload`，Postfix `Reset`（LIR-P1 `:22-47`）。超限若再 Prefix 同一方法，必须兼容 Guard 生命周期。

### 4.2 切枪 / 卸下武器

主机 `dequip` → `SendEquip` 卸枪（U3-EQ `:2018-2040`）。新枪 `equip()` 时 `ammo = state[10]`（U3-GUN `:3513`）。旧枪上伪造的 RAM `ammo` 随 Useable 销毁，**不会**自动写进旧枪物品——这是延后方案比 R6 逐发改 `state[10]` 干净的一点。

但 `shotsFired` 若只活在 Useable 实例上，dequip 会丢计数 → **漏结算 = 窗口内免费伤**。必须在主机 `dequip`/`SendEquip` **之前** settle（T7 已要求换枪结束会话）。不能靠客机 HUD。

### 4.3 死亡

`onLifeUpdated(true)`：主机 `dequip()`，装备坐标 255（U3-EQ `:2922-2947`）。`tock` 在 `!IsAlive` 不跑（U3-IN `:1862-1864`）。死亡后不会再 `fire()`。掉枪与否看 `Lose_Weapons_PvP/PvE`。

延后特有：尸体/掉落物带走的是 **未改的** `state[10]`（若窗口内没写），后备箱还在活玩家或已掉落的库存里。settle 必须在 `dequip`/掉枪 **同一主机帧、更早**，否则：

- 漏结算 = 已出伤未扣箱；
- 死后对已掉落库存 `sendUpdateAmount` = 改错容器或找不到槽。

复活后会话元数据必须作废（R6 §4.2）。

### 4.4 整理把箱子挪走 / 功能 A 合匣 / 拖拽

`fire()` 不锁库存。窗口内 LIT 整理、LIR A/B、玩家拖拽都改页 2..6。后备箱被移走 **不会** `dequip`（U3-PINV `:1754-1766` 只在移除当前装备格时卸枪）。

延后相对 R6 的变化：窗口内射击不再跟整理抢同一槽的逐发写入，**交错从「每发」降为「激活快照 vs 结束实况」一次**。这是真正的减负。

仍失败：

- 结束时 `getIndex`/指纹对不上 → 扣不够 → 亏空。产品允许惩罚冷却，但伤已成。
- 功能 A 合并备用匣会删空匣、改 amount；激活时记下的备用匣槽可能消失。
- 结算事务与进行中的功能 B 抢同一玩家：必须互斥（LIR-AUTH 闸 1.5s）。窗口结束不能被闸挡掉后忘记补结。

### 4.5 客机多打超过主机 cap

分层（射击权威仍在主机重放，R6 §4.5）：

- 客机本地伪造非零 `ammo` **不能**让主机在 `ammo < ammoPerShot` 时 `fire()`。无对应 `BulletInfo` 则无伤。
- 若主机为会话维持许可直到 `shotsFired==cap`：必须在 **host `tockShoot`/`fire` 前** 用 `shotsFired >= cap` 停许可（恢复真 `ammo`，或取消 `isShooting`）。否则客机按住扳机 = 主机继续 `fire()` = 超 cap 免费伤。
- 客机预测可能比主机多跑几发（延迟）。owner 侧冻结 `ammo` 时，客机可以在主机已达 cap 后仍本地 `fire()` 出特效/弹道预测；主机应丢弃超 cap 输入。这是表现不同步，只要主机不出伤、不计入 settle，不是刷弹。
- 听服：预测与权威同一趟。cap 检查只应在这一趟加一次。

**没有**原版逐发 ammo 校正 RPC（卡壳除外）。达 cap 后要把客机 `ammo` 拉回真值，只能复用 `sendUpdateState` / 卡壳同类可靠 RPC / 自建频道。漏拉回 → 客机继续空放直到下一次装备同步。

### 4.6 结束时漏结算

这是延后方案 **相对 R6 更危险** 的面：R6 漏的是某一发的箱；R7 漏的是整窗伤害的付款。

会漏的出口（均须同一套 host settle，失败则亏空冷却 + 隔离，不能静默）：

| 出口 | 原版点 | 漏了结则 |
|---|---|---|
| 到时 | 自建计时 | 免费窗 |
| 换枪/卸枪 | `dequip` / `SendEquip` | 计数随 Useable 死 |
| 死亡 | `onLifeUpdated` | 库存/掉落已变 |
| 换匣 | `ReceiveAttachMagazine` | 当前匣身份变 |
| 卡壳 | `SendPlayChamberJammed(..., ammo)` | 客机 `ammo` 被纠正；若纠正到 0 停火，仍须 settle 已打出的 |
| 空匣删除 | `:1224-1288` | 当前匣没了 |
| 连接/代际 | T7 已列 | 玩家对象换代 |
| 主机异常/回滚失败 | LIR RestoreFailed 先例 | 半扣箱 |

惩罚冷却 **不能** 代替扣弹。产品「折算不上 → 惩罚冷却」是体验层；权威层仍应尽最大努力扣 `min(shotsFired, 结束时实有后备)`，余数记亏空。

### 4.7 其它仍打穿的原版行为（R6 §4.6，延后不免疫）

弦/箭 `EAction.String`：`fire()` 不写 `state[10]`，另走质量/每发删匣。`infiniteAmmo` 枪：超限无意义。炮塔 `isTurret`：`updateState()` 直接 return。`needsRechamber` 泵动在 `startPrimary` 挡射击（`:3168`）。这些枪型不应进会话。

---

## 5. Q4 — 结论

三选一：**只能做有缺陷的近似**（不是「可安全做」，也不是「与 R6 同因、整票做不到」）。

### 5.1 为什么不是「可安全做」

票面产品同时要：

1. 空匣（或打空后）一段时间仍能连发、不换弹；
2. 期间不消耗当前匣、假 `ammo` 不写回背包；
3. 结束才扣箱+备用匣，cap = 激活时主机后备；
4. 不破坏原版射击权威，客机不能超 cap 出伤。

(1) 仍要求 owner+host 伪造 `tockShoot` 的 `ammo`（U3-GUN `:4513-4520`）。这是 R6 判定「不可安全」的同一根绳子。延后结算不剪断它。

即便列出「必须的主机封顶/恢复步骤」，安全闭环仍缺：

- host `fire`/`tockShoot` 补丁（现网没有）；
- owner 同步冻结，否则预测停火；
- cap 前停许可，否则超 cap 免费伤；
- `dequip`/死亡/换匣 **之前** settle，否则漏付款；
- 拒绝或先结束 `ReceiveAttachMagazine`，否则真/假匣交叉；
- 挡住空匣删除与卡壳把 `ammo` 拉回 0 或把假量写进 `state[10]`。

任一步漏 = 免费伤或造弹。第五阶段官方优化不能把这套新射击子系统称为「安全」。

### 5.2 为什么不是「与 R6 同因整票做不到」

R6 做不到的是 **T7 字面逐发供弹**：每发改背包 + 空匣继续打 + 分开记账。延后结算 **确实绕开**「逐发改背包」：箱/备用匣只在结束时走 LIR 式整包 `sendUpdateAmount`，与 50Hz `tock` 解耦。

因此有一条 **有缺陷的近似**，语义已不是「空匣超限」，而是「有弹时限时不扣匣，结束从后备付款」：

1. **准入**：仅当激活时主机 `ammo >= ammoPerShot` 且后备 `cap > 0`。空匣拒绝，提示无可用弹或「空匣不能超限」（产品需改文案；本票不改 T7，只标明近似缺口）。
2. **激活（主机）**：快照 `gunSlot`、`magazineID`、`state[10]`、`cap`、后备指纹。不改当前匣 amount。
3. **窗口内（host+owner `fire` Prefix）**：跳过 `ammo -=` 与 `state[10]` 写入（等价会话期 `infiniteAmmo` 分支）；**只在 host** 累加 `shotsFired`，`shotsFired >= cap` 则恢复减法并停射。不扫描背包。
4. **HUD**：原版分子冻结在激活时 `ammo`（缺陷：不显示已打发数）。T7 要求的「超限已激活」仍可另标。
5. **结束（所有 Q3 出口，host，Useable 销毁前）**：`pay = min(shotsFired, cap)`；按激活优先级从现有箱再备用匣扣，能扣多少扣多少；扣满 → 默认技能冷却；否则亏空冷却。当前匣若未被换/删，无需恢复。
6. **互斥**：会话中拒绝 `ReceiveAttachMagazine`（或先 settle 再放行）；与 LIR B 事务互斥。

缺陷（必须写进规格，不能当边角）：

- **空匣不能打** — 与「不需要换弹」字面冲突；相对原版没有「空了还能打」的射击效果。
- 窗口内 HUD 不降；达 cap 后客机可能空放直到 `sendUpdateState`。
- 结束前伤害已生效；整理走箱 → 亏空冷却，不是撤销伤害。
- 新 `fire`/`tockShoot` Harmony，与卡壳、泵动、听服单次 `fire` 交织，测试面仍大。
- 漏 settle 仍 = 整窗免费伤（比 R6 单发漏扣更大）。

这不是 T7 Q5 字面会话，也不是 R6 否定的超容合并。它是「限时冻结当前匣 + 延期从后备付款」。第五阶段若要做三级的可玩切片，只能做这条，并改文案，不能叫「空匣超限」。

### 5.3 若坚持空匣不换弹 — 与 R6 同因，应移出第五阶段

空匣连发 = 伪造 `ammo`。延后扣箱只改变付款时间，不改变许可伪造。完整空匣会话需要 R6 §5.4 那套新射击子系统。应移出第五阶段；不要改做成超容（R6 §5.2 / T7 已废止）。

### 5.4 对照表

| 方案 | 当前匣不扣 | 空匣连发 | 不逐发改背包 | 结束真实扣后备 | 第五阶段 |
|---|---|---|---|---|---|
| T7 字面 + 本票空匣爆发 | 要 | 要 | 要 | 要 | 许可同 R6，做不到安全 |
| R6 逐发扣箱 | 要（失败） | 要（须伪造） | 否 | 每发 | 已否决 |
| 激活合并进当前匣 | 否 | 能直到打完 | 一次 | 一次 | 语义违规 |
| **近似：有弹才激活，冻 `fire` 减法，结束扣箱** | 窗口内是 | 否 | 是 | 结束一次 | 有缺陷可做 |
| R6 降级：B 先箱后匣压弹 | 是（仍是匣） | 否 | 是 | 压弹时 | 可做，无爆发 |

### 5.5 若有人把近似误标成「可安全做」

安全所需的主机步骤（即使近似也必须有，缺一则降为不安全）：

1. 主机算 cap，拒绝 cap=0 与空匣。
2. 只 host 计 `shotsFired`，达 cap 停许可。
3. 窗口内 `fire()` 不写 `state[10]`。
4. 一切出口在 Useable/库存变化前 settle。
5. 会话中拒绝换匣，或换匣前 settle。
6. 客机超 cap 不出伤；库存只主机写。

有这六步的近似仍缺陷（空匣、HUD、已出伤后亏空）。没有这六步则与 R6 一样不能做。

---

## 6. 明确不在本报告范围

- 技能 UI、经验、冷却/窗口秒数（T7 待定）。
- 实施补丁代码。
- 推翻 T7 Q5（只判定本票爆发语义：空匣安全做不到；有弹冻结+延后扣箱是另一产品）。
- 不改 `map.md`、生产源码、不提交。

---

## 7. 一句话

延后结算绕开了 R6 的逐发背包写入，没有绕开 `tockShoot` 用同一个 `ammo` 当连发许可。空匣不换弹连发与 R6 同因，不能安全做。第五阶段最多做「有弹才激活、窗口内不减匣、结束一次扣箱」的有缺陷近似；漏结算 = 整窗免费伤。
