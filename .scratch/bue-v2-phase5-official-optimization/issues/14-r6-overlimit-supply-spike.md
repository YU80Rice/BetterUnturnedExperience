# V5-R6 超限弹匣逐发供弹可行性

- **Ticket**: V5-R6
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: 07
- **Map**: [map.md](../map.md)

## Question

换弹技能票已把三级「超限弹匣」定义为临时供弹会话：先扣匹配弹药箱，再扣备用匣余弹；当前装上的匣不当源；主机按每次射击扣真实资源。这是否能在不破坏原版 `UseableGun` 射击权威、不造成客机无限/主机不同步的前提下实现？

对照 U3-SDK `UseableGun` 开火/扣弹路径与现网 LIR 补丁，产出现状报告（`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R6-overlimit-supply.md`）。

必须回答：

1. 主机何时把 `ammo` / `state[10]` / `jar.item.amount` 减一？客户端预测开火是否本地先减？
2. 有哪些 Harmony 缝可以在「这一发消耗备用池而不是当前匣」时仍让原版认为弹匣未空（不触发换弹动画、不卡连发）？
3. 失败模式：切枪、死亡、换匣、`ReceiveAttachMagazine`、弹药箱在射击中被整理走、P2P 客机伪造。
4. 结论三选一并说明证据：可安全做 / 只能做近似（例如激活时把备用打进当前匣再打完还原，这会改变容量语义）/ 本阶段应把超限降级为「仍是压弹、只改供弹顺序」或移出第五阶段。

只查证不改码。不设计技能 UI。换弹票 Answer 的产品语义是输入，不是本票可推翻的范围，除非结论是「该语义在原版射击权威下做不到」。

## Answer

2026-09-14：**must defer or downgrade**。原版用同一个 `byte ammo` 既当连发许可（`tockShoot`）又当当前匣账本（`fire()` 写 `state[10]`）；客机预测会本地先减这两份，背包 `jar.item.amount` 不在开火路径上。现网 LIR 只补 `ReceiveAttachMagazine`/`forceAddItem`，不补开火。任何让空匣继续连发的缝都要在 owner+host 伪造 `ammo>=ammoPerShot`，换匣会把伪造量写进真实库存，主机漏扣后备=免费弹。T7「分开记账 + 不写无限 + 不合并进当前匣 + 不破坏射击权威」在第五阶段做不到；不要改做成超容近似。本阶段把三级降为「仍是压弹、只改先箱后匣」；完整供弹会话移出第五阶段。

报告：[2026-09-14-V5-R6-overlimit-supply.md](../research/2026-09-14-V5-R6-overlimit-supply.md)
