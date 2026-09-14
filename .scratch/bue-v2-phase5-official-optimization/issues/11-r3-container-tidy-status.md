# V5-R3 容器页整理注入与权威现状

- **Ticket**: V5-R3
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T4 的事实输入。对照 LIT UI patch、整理协议与原插件归档产出现状报告（`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R3-container-tidy.md`）。

必须回答：

1. 现网 `InventoryTidyUiPatch` 注入哪些 headers/page、STORAGE 被排除的代码与注释原文、旧 STORAGE 布局常量是否已删。
2. 协议与执行：`RequestTidy` / `TidyPage` / `TIDYABLE_PAGE_MIN/MAX` 是否硬编码 2..6；服务端对 page=7 是拒、忽略还是未定义。
3. 原 LaunchInventoryTidy：v1.0 注入 STORAGE 但执行不含 page 7 的证据；v2.0.1 拆按钮的理由原文。
4. 容器对象模型：STORAGE 与 InteractableStorage、车辆后备箱、工坊虚拟容器、AREA=8 的差异（谁有生命周期、谁会跨玩家、谁会被服务端拒）。
5. Phase-4 地图/CONTEXT/玩家手册把「不注入 STORAGE」写成永久不变量的原句位置。

只查证不改码。不要设计新按钮布局。结论带 file:line。

## Answer

现网只注入 `headers[0..4]` = page 2..6（一颗 60×60 @ -130「整理」）；STORAGE/AREA/0/1 不画。排除原文在 `InventoryTidyUiPatch.cs:236-240,510-511`；旧 STORAGE 偏移常量已删。协议硬编码 `TIDYABLE_PAGE_MIN/MAX=2..6` + `AllPages=0xFF`：客机 page=7 被 codec 当畸形包拒（无 Committed 回包），权威第二道门 `RejectedNoMutation`；主机本地 `LocalTidyExecutor` 无页门，UI 目前到不了。v1.0（归档 git `22d828b`）`HEADER_INJECT_COUNT=6` 画了 STORAGE：房主 `TidyPage(byte)` 静默忽略 page 7，客机网络会整理 `items[STORAGE]`。v2.0.1 因审计「可点但服务端确定性拒绝 page 7」把注入降为 5——要支持必须单独定义权限/生命周期/关箱并发，不能把上限改成 7。STORAGE 是页槽：箱子=单 opener（第二人 BUSY），后备箱=驾驶座授权的车载 Items，工坊虚拟=同槽+插件钩子，AREA=8 是本地地面不是容器。Phase-4 把「不注入 STORAGE」写进永久不变量（map `:64` / spec `:163` / T8 `:39`）；CONTEXT 已改为第五阶段可重开，未重开前现网与手册仍是「不含仓储栏」。

报告：`.scratch/bue-v2-phase5-official-optimization/research/2026-09-14-V5-R3-container-tidy.md`
