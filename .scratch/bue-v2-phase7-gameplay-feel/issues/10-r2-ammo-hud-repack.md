# V7-R2 弹药 HUD 刷新缝与压弹匹配单源

- **Ticket**: V7-R2
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T3 与 T5 的事实输入。对照 LIR HUD 与压弹服务产出现状报告（`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R2-ammo-hud-repack.md`）。

必须回答（只查证不改码，结论带 file:line）：

1. HUD 观察 / 投影 / 呈现三层文件与入口；为何只在 `UseableGun.updateInfo` 之后刷新；开火扣弹、背包 amount 变化、弹药箱变化各会不会走到该 postfix。
2. `AmmoReserveProjection` 的 N/M 公式与「不含枪上那本」的类型保证；枪上匣当前发数今天能否从观察 DTO 读到。
3. 口径匹配与弹药箱 FillTargetItem 是否与 `AmmoRepackService` 共用单源（`CollectCompatibleAmmoIds` / `MagSuppliesMatch`）；有无第二套口径表。
4. 功能 A（整理后合匣）与功能 B（双击填箱）与技能 2 级延时一轮的调用链、页范围（2..6）、空匣是否被填、枪不在手是否可跑。
5. 现网 `ReloadAutoRoundScheduler` 的 8 秒窗从何时武装、到点重检哪些指纹、与技术闸 / 技能冷却的关系。
6. 哪些测试会在「总弹药含枪上匣」或「8 秒被动压所有匣」后必然红。

不要在本票设计新 HUD 或新调度器。

## Answer

报告：`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R2-ammo-hud-repack.md`

1. **三层**：观察 `AmmoReserveHudEngine`、投影 `AmmoReserveProjection`、呈现 `AmmoReserveHudSurface`；入口 `updateInfo` postfix → `AmmoReserveHudAdapter.OnGunInfoUpdated`。只挂该面：Hud 无库存/开火补丁。原版 `updateInfo` 仅三处（开火本地、装备建盒、state 刷新）。**开火会走 postfix**（M 通常不变）；**背包匣/箱 amount 默认不走**，直到再开火/换匣/再装备。
2. **N** = 页 2..6 非空口径匹配备用匣本数；**M** = 这些匣余弹 + 能给**枪上匣**供弹的箱。`AmmoReserveLoadedMagazine` 无弹量字段；观察不读 `state[10]`。枪上发数今天读不到。
3. **单源**：`CollectCompatibleAmmoIds` + `MagSuppliesMatch` 压弹与 HUD 箱侧共用。第二套是 N 对枪的 `CalibersMatchWeapon`（原版搜匣，有零跳过差异），不是第二份口径表。
4. **A** = TidyCompleted → 同 ID 合匣，2..6，不问枪在手，不填箱。**B** = 双击 → 未满匣（**含空匣**）吃箱，2..6，事务本身不问枪在手。**2 级** = B 成交后再 8s 跑同一套 B；枪在手只在指纹重检。
5. **8s 窗**：仅手动 Committed∧转移>0∧等级 2 武装。到点重检等级仍 2 + `equipment.state` 全字节 + 存活。技能窗 2 级不挡；**1.5s 技术闸仍吃**（hostInitiated 跳过回放比较）。
6. **必红**：HUD `DevV5AmmoReserveHudTests` 文案「备匣/备弹」、1f 无弹量字段、整组 M 精确值。技能 `DevV5ReloadSkillTests` 4a 手动才排且只一轮、4b 切枪取消、4g Stop 清表。B 已扫全部未满匣，与「所有匣从箱填」同向；冲突在武装点与枪在手指纹。
