# V5-R5 换弹、原版经验与弹药 HUD 现状

- **Ticket**: V5-R5
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T7 的事实输入。对照 LIR、U3-SDK 经验/HUD 产出现状报告（`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R5-reload-hud-skills.md`）。

必须回答：

1. 现网功能 A（整理后自动压弹）与功能 B（双击 R 一键压弹）的触发、范围（页 2..6）、是否填弹药箱、冷却/窗口常量。
2. `UseableGun.updateInfo` 如何格式化 `ammoLabel`；有无现成「后备弹匣数 / 总备弹」API 或字段可直接读。
3. BUE 是否已 patch `updateInfo` / `ammoLabel` / `PlayerLifeUI`（LHT 尸潮 HUD 不要误当成弹药 HUD，但要标明它占用了哪块 UI）。
4. 原版 `PlayerSkills`：经验怎么加减、技能树结构、插件能否 `askSpend` 而不新建技能槽；单人默认是否有经验。
5. 「超限弹匣」若按超过 `magazineAsset.MaxAmount` 理解，原版/LIR 有没有已存在的超容路径；现网功能 B 填弹药箱的优先级（先箱还是先其它弹匣）。
6. LIR 设置面现有 descriptor（enabled 已退役为生命周期意图后还剩什么可给「技能等级」用）。

只查证不改码。U3-SDK 路径 `D:\Agent-工作目录\U3-SDK\` 允许读。不要设计技能树 UI。

## Answer

A = 整理成功后同 ID 匣合并（页 2..6，不填弹药箱）；B = 0.3s 双击 R 用弹药箱填未满匣（同样 2..6；其它匣不是源）。A/B 共用 1.5s 闸门。`updateInfo` 只格式化 `Ammo(当前, MaxAmount)`，没有后备匣/总备弹字段。BUE 未 patch `ammoLabel`；LHT 占 `PlayerLifeUI.container` 顶栏指南针下 800×35，与右下弹药盒不是同一块 UI。`askSpend` 可花原版经验且不必新建槽，但原版树是固定 3×(7/7/8)，单人新档经验为 0。原版/LIR 都没有玩法向超容（`MaxAmount` 当天花板）。LIR 生产 settings facet 为空，`inplacereload.enabled` 已迁到生命周期意图，技能等级没有现成 descriptor。

报告：[2026-09-14-V5-R5-reload-hud-skills.md](../research/2026-09-14-V5-R5-reload-hud-skills.md)
