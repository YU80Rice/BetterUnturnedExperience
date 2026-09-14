# V5-R7 限时超限（开火不扣匣、结束时折算后备）可行性

- **Ticket**: V5-R7
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: 07, 14
- **Map**: [map.md](../map.md)

## Question

换弹票把三级超限定义为临时供弹会话；R6 结论是「边打边从箱子扣、当前匣分开记账」在原版 `UseableGun` 射击权威下做不到。用户提出第三条产品：

> 双击 R 进入一段时间：开火不消耗当前匣、不需要换弹；时间结束（或换枪/死亡等）后再从匹配弹药箱 + 备用匣余弹一次性折算这段打出的发数。折算得上 → 默认技能冷却；折算不上（亏空）→ 惩罚冷却。当前装上的匣不当源。不是一次压弹，也不是边打边扣箱。

对照 U3-SDK `UseableGun` 开火/扣弹/连发门槛与 R6 报告，产出现状报告（`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R7-overlimit-burst-settle.md`）。

必须回答：

1. 要让空匣在一段时间内仍能连发，必须动哪些字段（`ammo`、`state[10]`、`tockShoot` 门槛）？owner 客户端预测是否仍会本地先减 `ammo`？
2. 「期间不扣匣、结束再扣箱」能否做到：激活时主机记下后备可覆盖的发数上限；期间伪造/冻结连发许可但不把假 `ammo` 写回背包匣；结束时只 `sendUpdateAmount` 弹药箱和备用匣、把当前匣恢复成激活前的 amount？
3. 失败模式：期间换匣/`ReceiveAttachMagazine`、切枪、死亡、整理把箱子挪走、客机多打超过主机上限、结束时漏结算。
4. 结论三选一并说明证据：可安全做（列出必须的主机封顶/恢复步骤）/ 只能做有缺陷的近似 / 与 R6 同因做不到，三级应移出第五阶段。

只查证不改码。产品语义是输入。R6 报告必须对照，不要重复抄完整开火路径，只写「与 R6 的差异：延后结算是否绕开逐发改背包」。

## Answer

2026-09-14：**只能做有缺陷的近似**。延后结算绕开 R6 的逐发背包写入（结束一次 `sendUpdateAmount`），不绕开 `tockShoot` 用同一个 `ammo` 当连发许可：空匣不换弹连发与 R6 同因，不能安全。owner 预测仍会本地先减，除非同时补 owner+host 的 `fire`/`tockShoot`。第五阶段最多做「有弹才激活、窗口内跳过 `state[10]` 写入、host 计发数封顶、一切出口在 Useable 销毁前扣箱」；空匣拒绝；漏结算 = 整窗免费伤。不要做成超容。

报告：[2026-09-14-V5-R7-overlimit-burst-settle.md](../research/2026-09-14-V5-R7-overlimit-burst-settle.md)
