# V5-R2 整理排序与 O-LIT-1 现状

- **Ticket**: V5-R2
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T3 的事实输入。对照 LIT 求解器与 O-LIT-1 挂账产出现状报告（`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R2-sort-strategy.md`）。

必须回答：

1. `ITidyStrategy` / `StrategyId=default-grid-v1` / `DefaultGridV1Strategy` / `InventorySolver.TidyMode` 的职责切分：排序 vs 放置分别在哪。
2. 同类 / 空间 / 大件（SameType / MaxRects / FFD）与降序 / 升序今天实际怎么排、默认值、面板文案是否承诺算法。
3. O-LIT-1 原始观察（DEV-V2-08 case.md / 结单报告）具体抱怨了什么，有没有可复现案例。
4. DEV-V2-15 勘误口径原文（「排序与放置都不改」）落在哪些注释/测试断言上；哪些测试会在改排序后必然红。
5. 有无策略选择器、第三方策略加载、每页不同策略的残留。

只查证不改码。不要在本票设计新算法。结论带 file:line。

## Answer

排序与放置都在 `InventorySolver`：`ITidyStrategy`/`default-grid-v1` 只转发 `TryPack`，`TidyMode` 是入参不是第二条策略。三档同类/空间/大件 = SameType/MaxRects/FFD，默认同类+降序；面板 Q61 文案不承诺算法。O-LIT-1 原观察是主观「排列算法质量有问题 / 后续再说」，无物品清单；邻近可复现点是 V215 降序 SameType page 3 静态验证拒绝。DEV-V2-15「排序与放置都不改」落在工单/adapter 注释/R3 零 diff；现测钉确定性与无重叠，不钉黄金坐标，改排序大多不红。无策略选择器、无第三方加载、无每页不同策略（每页模式字典已退役）。

报告：[2026-09-14-V5-R2-sort-strategy.md](../research/2026-09-14-V5-R2-sort-strategy.md)
