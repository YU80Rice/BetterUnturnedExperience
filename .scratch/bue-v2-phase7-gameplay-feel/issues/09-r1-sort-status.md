# V7-R1 行带整理求解器与分类器现状

- **Ticket**: V7-R1
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T2 的事实输入。对照 LIT 统一排版模块产出现状报告（`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R1-sort-status.md`）。

必须回答（只查证不改码，结论带 file:line）：

1. 现网唯一策略标识、入口（当前栏 / 全身 / 容器 / 入包恢复 / 快速转移恢复）是否都调用同一 `ITidyStrategy` / `TaggedRowBand*`；有无第二份放置循环。
2. `PlayerUseClassifier` / `PlayerUseLabel` 的 20 档顺序单源在哪；分类失败进「其他」的路径；弹药箱 vs SUPPLY 如何识别。
3. 行带放置的主体选择、横向并排、右下空位评分如何实现；哪些测试钉死行带几何（换方法后必然红）。
4. 管理面板 `inventorytidy.mode` / `direction` 现网是否仍可读、是否仍影响稳定收尾。
5. 第五阶段 T3 产品定义与现网实现的已知缺口（若有评审气味 / 具名接缝）。

不要在本票设计新算法。V5-R2 报告可作历史对照，但现网以 `TaggedRowBand*` 为准，不得把已退役的三模式写成仍有效。

## Answer

现网唯一内置策略是 `tagged-row-band-v1`（`TaggedRowBandV1Strategy` → `TaggedRowBandLayout.TryPlanLayout`）。当前栏、全身、容器、入包恢复、快速转移的整理侧都经模块 `ITidyStrategy`；快转源页是 `PreparePageLeave` 恒等离场，不是第二份排版。20 档顺序单源是 `PlayerUseLabel` 枚举序；分类失败进「其他」；弹药箱按 FillTargetItem 供弹蓝图认，不是 SUPPLY 本身。行带主体=高度优先+细长让位，并排可计算，没有独立右下评分函数——空位是左上推进的副作用，救济/精确层可拆行带结构。`inventorytidy.mode` 已从面板退役、不决定算法；`direction` 仍可读，只翻同几何稳定收尾。T3 视觉择优/右下评分未落地；换方法必红的是 `AssertDevV502TaggedRowBand` 几何钉。完整报告：[2026-09-21-V7-R1-sort-status.md](../research/2026-09-21-V7-R1-sort-status.md)。
