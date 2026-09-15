# V2 第五阶段：现有官方功能优化定界与规格冻结（Wayfinder 地图）

Type: task
Status: **completed（2026-09-14：T1..T8 + R1..R8 resolved；spec.md ready-for-agent；DEV-V5-01..08 已发布；实施进展：DEV-V5-01 resolved（audit/2026-09-14/DEV-V5-01/）；DEV-V5-02 resolved（2026-09-15 双轴 R1-R7 CLEAN，统一排版模块+分类器+迁移落地，audit/2026-09-15/DEV-V5-02/）；DEV-V5-03 resolved（2026-09-15 双轴 R1-R4 CLEAN，容器会话深模块+两 adapter+线协议 7/8+权威重验零修改+标题栏一颗整理，audit/2026-09-15/DEV-V5-03/）；前沿=DEV-V5-04 / 05 / 06 / 07，依赖全满足）**
Label: wayfinder:map
Parent: 无（承接 POST-P4 收官：01–08 resolved，RELEASES 行 13 / 候选 `AF1F50C9…16D0`）
Author: GPT（本会话 charting，2026-09-14）

## Destination

BUE Phase 5：现有官方功能优化定界与规格冻结。

到达条件（闭包自检已过）：
1. 六块均有「本阶段做 / 后续另开图 / 本图范围外」裁决（锁定与完整超限为后续；手册/排版/容器/恢复/HUD/换弹 0～2 为本阶段）；
2. 不新增官方功能模块、不新增 FeatureId；
3. STORAGE 已按容器会话重开，不是把页上限改成 7；
4. 可交 `/to-spec`（八张实施票建议见规格闭包票）。

本图只产决策，不写生产代码、不授候选。

## Notes

- 领域：Unturned + BepInEx 5；当前对外部署 = 契约 2.1 / RELEASES 行 13 / 候选 `AF1F50C9…16D0`。本图 plan-only，生产实施随 `/to-spec` → `/to-tickets` 产生。
- **技能**：决策票必调 grilling + domain-modeling；research 票 AFK 子代理落报告，主会话回写本图指针。勿 `/triage`。实施期硬规则届时生效：红测先行 + 双轴 CLEAN（`docs/agents/output-review-loop.md`）；大写入分批。
- **开图轮已定、不立票（2026-09-14，用户当场裁定）**：
  1. 终点 = 可交 `/to-spec` 的规格，不是直接改 DLL；
  2. **本阶段不添加新的官方功能**；只优化已经在 BUE 里实现的官方功能；
  3. 本阶段六块 = 开发手册分层 + 整理排序优化 + 容器整理按键 + 被动整理 + 装备栏锁定 + 换弹技能/弹药 HUD；做完这六块本阶段结束；
  4. 第六阶段展望五项（工坊物品 ID、Xaero 式地图、Carry On、网格化放置、快捷栏手势）只记名，不在本图展开规格；
  5. 开发手册要写成给人看的「一图流」（模块生命周期、怎样接入 BUE 生态）+ 给 AI 看的执行规格；给人看的手册稳定后由用户请第三方评审——评审不是 FeatureId；
  6. 被动整理、容器按键、锁定、排序、换弹技能/HUD 都不开新功能模块：前四项属背包整理，后一项属更好的换弹体验。
- **票号**：决策票 V5-T1..T8，research 票 V5-R1..R8。实施票由 `/to-tickets` 产生 DEV-V5-01..08。
- **必须显式重开的旧不变量**：Phase-4 地图把「不注入 STORAGE」写成永久产品不变量。用户本阶段要求给容器加整理按键 → 由 T1 授权重开、T4 裁定新边界；未重开前不得当已批准。
- **已建成事实（各票对照，不重开）**：LIT 标题栏只留「整理」（60×60 @ -130，headers[0..4]）；模式/方向两条已保存 Choice；O-LIT-1 排列质量挂账未改算法；STORAGE 现网不注入；被动整理在原 LaunchInventoryTidy v1.4.1 以摘掉 `[HarmonyPatch]` 硬禁用，BUE 未移植；锁定页第四阶段不画、不留空位；LIR = 0.3s 双击 R 一键压弹 + 整理后同步自动压弹（无经验、无技能、无弹药 HUD）；契约 2.1。
- **主源锚点**：`InventoryTidyUiPatch.cs` / `DefaultGridV1Strategy.cs` / `InventorySolver.cs`；`ReloadInputDriver` / `AutoReloadAfterTidyAction` / `UseableGun.updateInfo`（U3-SDK）；玩家手册 `docs/BetterUnturnedExperience-Player-Handbook.md`；SDK `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`；Phase-4 地图 `.scratch/bue-v2-phase4-visual-experience/map.md`；原插件归档 `Archive/2-未闭环验证项目/LaunchInventoryTidy`。

## Decisions so far

- [开发文档读者分层现状盘点](issues/09-r1-docs-landscape.md) — 给人看的开发手册是空槽；生态作者按规定只读 SDK + NoOp；抢权威的是未退役 `.scratch` 规格；一图流不存在。报告 `research/2026-09-14-V5-R1-docs-landscape.md`。
- [整理排序与 O-LIT-1 现状](issues/10-r2-sort-strategy-status.md) — 排序与放置都在 `InventorySolver`，`default-grid-v1` 只转发；三档仍是同类/空间/大件；O-LIT-1 无物品清单；改排序大多不红测试。报告 `research/2026-09-14-V5-R2-sort-strategy.md`。
- [被动整理历史与现网缺席](issues/12-r4-passive-tidy-history.md) — 原插件只在 `tryFindSpace` 失败时整页重排；BUE 未移植；现网只有标题栏按钮；恢复必须挂服务端入包判定。报告 `research/2026-09-14-V5-R4-passive-tidy.md`。
- [容器页整理注入与权威现状](issues/11-r3-container-tidy-status.md) — 现网只注入 page 2..6；协议对 page=7 当畸形包拒；v1.0 画过 STORAGE 但执行不一致；要支持必须单独定义权限/生命周期/关箱并发。报告 `research/2026-09-14-V5-R3-container-tidy.md`。
- [换弹、原版经验与弹药 HUD 现状](issues/13-r5-reload-hud-skills.md) — 功能 A 整理后合匣、功能 B 双击 R 用弹药箱填匣；原版 HUD 只有当前/上限；`askSpend` 能花经验但不能加技能槽；LIR 设置面已空。报告 `research/2026-09-14-V5-R5-reload-hud-skills.md`。
- [现有官方功能优化跨表面共享裁决](issues/01-t1-shared-rulings.md) — 契约仍 2.1 零扩面；生态不同权到业务能力；授权重开 STORAGE 注入（边界归容器票）；画面/权威验收分工；手册先写再外请评审、不挡 `/to-spec`；被动整理不挂拖放。
- [给人看的开发手册与给 AI 的执行规格分层](issues/02-t2-developer-handbook.md) — 四层不增层；入口 `docs/developer/` 一张总图+三短章；SDK 仍唯一契约；范例只导读 NoOp；官方业务只点名；评审包=该目录+README 入口。
- [整理排序方式优化](issues/03-t3-sort-strategy.md) — 废止三模式与「只改排序」；唯一官方方法=标签分段行带排版；旧档位只迁移不决定算法；凡背包整理入口共用同一模块。
- [容器整理按键](issues/04-t4-container-tidy.md) — 第 7 页不是身份；本阶段=世界容器+已授权后备箱；会话/版本校验；按钮可见≠授权；全身整理永不含容器。
- [被动整理重新启用](issues/05-t5-passive-tidy.md) — 入包失败恢复玩家五页；Ctrl+右键失败只整理接收侧；主机权威；不发 TidyCompleted；不挂 tryAddItem 一条缝打天下。
- [装备栏锁定](issues/06-t6-page-lock.md) — 第五阶段不做；A 整理锁定与 B 防误触须未来拆开；不画、不留空、整理路径不读锁定。
- [换弹技能与弹药 HUD](issues/07-t7-reload-skill-hud.md) — 默认 HUD 追加有弹匣/后备；0 级仍能双击；U 菜单战斗区下方分区不占原版槽。三级完整超限由闭包移出本阶段。
- [第五阶段规格闭包与实施票拆分](issues/08-t8-spec-closure.md) — 地图关闭；八张 DEV-V5；01..07 不授候选；完整超限与锁定为后续；`/to-spec` 输入含 R1..R8。
- [超限弹匣逐发供弹可行性](issues/14-r6-overlimit-supply-spike.md) — **must defer or downgrade**：原版 `ammo` 同时是连发许可和当前匣账本，完整供弹会话本阶段做不到。报告 `research/2026-09-14-V5-R6-overlimit-supply.md`。
- [限时超限（开火不扣匣、结束折算）可行性](issues/15-r7-overlimit-burst-settle.md) — **只能做有缺陷的近似**：延后结算绕开逐发改背包，不绕开空匣连发；最多「有弹才激活、窗口内冻匣、结束扣箱」，空匣拒绝，漏结算=免费伤。报告 `research/2026-09-14-V5-R7-overlimit-burst-settle.md`。
- [空匣先压满再限时冻匣](issues/16-r8-overlimit-prime-then-burst.md) — **与 R7 同级不可做**：现网压弹只填背包未满匣，不写枪上匣/`ammo`/`state[10]`；空匣先压满接不上 R7 有弹准入；有弹+零后备开窗=冻匣免费伤。报告 `research/2026-09-14-V5-R8-overlimit-prime-then-burst.md`。

## Not yet specified

（空——闭包后无 Fog。经验/冷却秒是实施票必填参数，禁止猜测默认值；不另开决策票。）

## Out of scope

- **第六阶段展望（另开图）**：创意工坊物品 ID 冲突；Xaero 式地图与中键标点；Carry On 搬箱；红警/部落冲突式网格化放置；快捷栏滚轮手势与同类自动替换。
- **后续另开图（本阶段不做，不是永不）**：装备栏锁定 A/B；完整超限供弹（R6/R7/R8 为延期证据）；工坊虚拟容器。
- **新官方功能模块 / 新 FeatureId**：本图禁止。
- **第四阶段留给后续可视化、本图不偷运**：Item/Blueprint/Creature 可视化选择器、面板 i18n、统一色板、路径调试、BueUi。
- **永久产品不变量**：不建 BueUi SDK 替代 Glazier；不热卸载外部插件；不用草稿改写 ServerAuthority；不后台自动保存。STORAGE 注入已由跨表面共享裁决授权重开，边界见容器整理按键票（会话身份，不是把页上限改成 7）。
- **拖放路径自动整理**：更好的物品交互继续禁止在拖放时重排已占用格子；被动整理不挂到该路径。
- **尸潮 HUD、BII 拖入、主菜单入口按钮改版**。
- **SPF ResourceObs**：用户 SPF 域。
- **生产实施本身**：红测/候选/实机随 DEV-V5-* 届时产生。
