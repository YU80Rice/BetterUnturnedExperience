# V5-R4 被动整理历史与现网缺席

- **Ticket**: V5-R4
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T5 的事实输入。对照原 LaunchInventoryTidy 与 BUE 生产程序集产出现状报告（`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R4-passive-tidy.md`）。

必须回答：

1. v1.4.0 `ItemsTryAddItemPatch` 的触发条件、作用页、失败回退、是否发整理完成事件。
2. v1.4.1 禁用方式（摘 `[HarmonyPatch]`、类留空）与审计为何禁止 Prefix 内加开关——原文。
3. BUE DEV-V2-15 排除该文件的测试锚（`Program.cs` 断言缺席）及生产 csproj 是否仍引用。
4. 现网 LIT 除标题栏按钮外还有没有整理入口（热键、拖放后、拾取后）。
5. CONTEXT「自动整理」是更好的物品交互的禁止项——与被动整理可能冲突的原句。
6. `tryAddItem` 在客户端/服务端谁执行（U3-SDK / 现网整理权威），被动整理若恢复必须挂在哪一侧才有权威。

只查证不改码。不要设计新 patch。结论带 file:line。归档路径允许读 `Archive/2-未闭环验证项目/LaunchInventoryTidy`。

## Answer

v1.4.0 Prefix 挂 `Items.tryAddItem(Item,bool)`：页≥2 且 `tryFindSpace` 失败才整页 TryPack（写死 MaxRects+降序）；TryPack false 放行原版 false；TryPack true 清空+重添并 `__result=true`，即使新物品未放入——无 TidyCompleted。v1.4.1 摘 `[HarmonyPatch]`、类留空；审计原文禁止 Prefix 内条件分支（保留误触发面）。DEV-V2-15 红测 `AssertLitSingleplayerPath` 钉 csproj 与产物均无 `ItemsTryAddItemPatch`；生产 Compile Include 不含该文件。现网唯一入口是标题栏「整理」；无 Plugin 0 / 拖放后 / 拾取后。CONTEXT：被动整理属背包整理，避免「自动整理」和 BII 拖放增强。权威 tryAdd 在服务端（拾取 SERVERSIDE / 合成 ONLY_FROM_OWNER）；拖放走 sendDragItem 不经 tryAdd；恢复必须挂服务端入包判定。

报告：`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R4-passive-tidy.md`
