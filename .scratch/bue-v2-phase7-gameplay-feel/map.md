# V2 第七阶段：现有官方功能玩法手感定界与规格冻结（Wayfinder 地图）

Type: task
Status: **completed（2026-09-21：T1..T8 + R1..R6 全 resolved；交 `/to-spec`；实施票建议 DEV-V7-01..07）**
Label: wayfinder:map
Parent: 无（承接 Phase-6 收官：01–13 resolved，RELEASES 行 15 / 候选 `3E3B2DC0…4CE9`；6.5 平台语义图已挂起，不挡本图）
Author: GPT（本会话 charting，2026-09-21）

## Destination

BUE Phase 7：现有官方功能的三项玩法手感定界与规格冻结。

到达条件：
1. 三块均有「本阶段做 / 后续另开图 / 本图范围外」裁决——背包整理换成新的唯一方法；更好的换弹体验改总弹药实时 HUD、原版技能行模板、换图清零、2 级被动压弹；更好的物品交互改绿/红预放置框与自动旋转只走两个正向；
2. 不新增官方功能模块、不新增 FeatureId；
3. 契约仍 2.1 零扩面；生态接不到整理策略、换弹技能或 BII 预览；
4. 可交 `/to-spec`。

本图只产决策，不写生产代码、不授候选。规格：[spec.md](spec.md)（ready-for-agent）。实施票已发布：

- [DEV-V7-01 换成标签归拢、组内大件优先、左上紧凑](issues/15-DEV-V7-01-stable-label-compact.md)
- [DEV-V7-02 弹药观察单源与总弹药 HUD](issues/16-DEV-V7-02-total-ammo-source.md)
- [DEV-V7-03 换弹技能 2 级改为被动压弹](issues/17-DEV-V7-03-passive-reload.md)
- [DEV-V7-04 原版技能行与换图清零](issues/18-DEV-V7-04-skill-row-scope.md)
- [DEV-V7-05 绿/红预放置框与两个正向](issues/19-DEV-V7-05-preview-upright.md) — resolved（2026-09-22：红测/全套/双轴审查闭环；不授候选）
- [DEV-V7-06 手册与官方功能文案落地](issues/20-DEV-V7-06-copy-handbook.md) — resolved（2026-09-22：红测/ClientUi+Plugin 全套回归/三轮双轴审查闭环；成功句接入玩家反馈 sink；不授候选）
- [DEV-V7-06R 最终行为与玩家手册/界面文案重新对账](issues/22-DEV-V7-06R-copy-reconcile.md) — resolved（2026-09-25：README/BII 设置文案同步；技能行与设置降级表面门禁补齐；旧承诺扫描为零；ClientUi/Plugin、V7-02/03/04/05 与 FULLSUITE 通过；Standards/Spec fresh 双轴 CLEAN；不授候选）
- [DEV-V7-07 三环境实机与唯一对外候选](issues/21-DEV-V7-07-three-env-release.md)

前沿实施票（无阻塞）：01 / 02 / 03 / 04 / 05。06 阻塞于 01–05；07 阻塞于 01–06。

## Notes

- 领域：Unturned + BepInEx 5；当前对外部署 = 契约 2.1 / RELEASES 行 15 / 候选 `3E3B2DC0…4CE9` / `publish/第六阶段-正式交付版本/`。本图 plan-only，生产实施随 `/to-spec` → `/to-tickets` 产生。
- **技能**：决策票必调 grilling + domain-modeling；research 票 AFK 子代理落报告，主会话回写本图指针。勿 `/triage`。实施期硬规则届时生效：红测先行 + 双轴 CLEAN（`docs/agents/output-review-loop.md`）；大写入分批。
- **开图轮已定、不立票（2026-09-21，用户当场裁定）**：
  1. 终点 = 可交 `/to-spec` 的规格，不是直接改 DLL（Q1 A）；
  2. **本阶段不添加新的官方功能 / 新 FeatureId**；只优化已经在 BUE 里的三项官方功能（Q2 / Q3）；
  3. 本阶段三块 = 背包整理的排序方法 + 更好的换弹体验的弹药显示/技能页/被动压弹 + 更好的物品交互玩家看见的拖入反馈；做完这三块本阶段结束；
  4. 契约仍 2.1 零扩面（Q4 A）；排序、HUD、预览都不进 C.1；
  5. 6.5 平台语义图保持挂起：T1/T2 留下的内部约束（五页同一套、值观察四字段、三条权威提交分家、空集合不是成功）本图当官方功能内部纪律引用，不兑现为公开 seam、不拆 DEV-V65-*；
  6. 装备栏锁定 A/B、完整超限弹匣、工坊虚拟容器、面板选择器/i18n/色板、第五阶段展望五项，本图只记名不展开。
- **票号**：决策票 V7-T1..T8，research 票 V7-R1..R6。实施票由 `/to-tickets` 产生 DEV-V7-*。
- **必须显式重开的旧不变量**：
  1. 第五阶段 T3 唯一官方整理方法（标签分段行带排版 / `tagged-row-band-v1`）已由跨表面共享裁决授权重开，边界归整理方法票；
  2. 第五阶段 T7「技能 2 级 = 双击成功后再等 8 秒压一轮、枪须在手」已由跨表面共享裁决授权重开，边界归被动压弹票；
  3. 第五阶段 T7「换弹技能按玩家×角色全局持久化」已由跨表面共享裁决授权重开为「按世界/存档分家」，边界归技能页票；
  4. ADR 0003 / CONTEXT「自动旋转 = (当前朝向+1) mod 4」已由跨表面共享裁决授权收窄为只走两个正向，边界归预放置框与正向旋转票。未裁出新边界前，现网与玩家手册仍按第五/四阶段正文。授权 ≠ 批准新实现。
- **已建成事实（各票对照，不重开）**：LIT 标题栏只留「整理」；容器整理与被动整理已落地且凡背包整理共用同一模块；LIR HUD 文案「备匣 N · 备弹 M」、挂 `UseableGun.updateInfo`、枪上匣发数不进后备；换弹技能 0～2、花费 125/150、0 级额外冷却 8s、自造 U 菜单分区、账在 `better-inplace-reload.skill-levels.dat`；功能 A = 整理后同 ID 合匣；功能 B = 双击 R 一键压弹（填箱不拆匣）；BII 预览代码已有 ValidGreen/InvalidRed，自动旋转 `(rot+1)&3`；契约 2.1；ADR 0001 不增强拖出到地面。
- **主源锚点**：`TaggedRowBandV1Strategy.cs` / `TaggedRowBandLayout.cs` / `PlayerUseClassifier.cs`；`AmmoReserveProjection.cs` / `AmmoReserveHudAdapter.cs` / `AmmoRepackService.cs`；`ReloadSkillStore.cs` / `ReloadSkillDashboardAdapter.cs` / `ReloadAutoRoundScheduler.cs`；`InventoryPreviewWiring.cs` / `InventorySurfaceLifecycleAdapter.cs` / `PlacementCandidateEvaluator.cs`；ADR 0001 / 0003 / 0005；玩家手册 `docs/BetterUnturnedExperience-Player-Handbook.md`；Phase-5 地图 `.scratch/bue-v2-phase5-official-optimization/map.md`。

## Decisions so far

- [行带整理求解器与分类器现状](issues/09-r1-sort-status.md) — 唯一策略 `tagged-row-band-v1`；五入口共用 `ITidyStrategy`；20 档单源=`PlayerUseLabel` 枚举序；无独立右下评分；`mode` 退役、`direction` 只翻稳定收尾。报告 `research/2026-09-21-V7-R1-sort-status.md`。
- [预放置框着色口与深色来源](issues/13-r4-preview-frame-color.md) — 写入链已接通 ValidGreen/InvalidRed，代码绿 ≠ 玩家看见绿；BACKGROUND 回退只走 None；`box==null` 空操作；原版 `dragItem` 未隐藏。报告 `research/2026-09-21-V7-R4-preview-frame-color.md`。
- [自动旋转四向与图标朝向](issues/14-r6-upright-rotation.md) — 现网永远 `(rot+1)&3`，感应带从横会选出 `rot=2`（倒置竖）；原版拾取只搜 0 再 1；绿框与图标同一 Candidate.Rotation。报告 `research/2026-09-21-V7-R6-upright-rotation.md`。
- [原版技能页模板与现网分区](issues/11-r3-vanilla-skill-ui.md) — 原版一行=`SleekSkill` 全幅按钮+锁条+名称级+可选职业图标+描述/加成+花费（90/80）；现网 postfix `updateSelection` 自造 Box/Label/Button，不能在不写 `Skill[][]` 的前提下复用 `SleekSkill`；表面 B 仅探测失败。报告 `research/2026-09-21-V7-R3-vanilla-skill-ui.md`。
- [弹药 HUD 刷新缝与压弹匹配单源](issues/10-r2-ammo-hud-repack.md) — HUD 只挂 `updateInfo`，开火会刷新、背包匣/箱默认不刷新；N/M 不含枪上发数；箱侧与压弹共用 FillTargetItem 单源；2 级=B 成交后再 8s 同一套 B，吃 1.5s 技术闸。报告 `research/2026-09-21-V7-R2-ammo-hud-repack.md`。
- [原版技能存档作用域与 LIR 全局账](issues/12-r5-vanilla-skill-scope.md) — 原版 `Skills.dat` 按 `serverID × steam × characterID × 地图名` 分家，换 PEI→Washington 换目录；LIR 账在设置根、键只有 steam×角色名，故跨图仍满级。LIT 熔断是同根按 map×slot 分文件先例。报告 `research/2026-09-21-V7-R5-vanilla-skill-scope.md`。
- [现有官方功能玩法手感跨表面共享裁决](issues/01-t1-shared-rulings.md) — 契约仍 2.1 零扩面；官方先行、生态不同权到业务能力；四条旧不变量全部授权重开（≠ 批准新实现）；画面类 headless 不画、权威类主机必真跑；手册是交付切片但文案不得先于行为裁决；功能 A 保留不重开。
- [整理方法换成标签归拢、组内大件优先、左上紧凑](issues/02-t2-sort-method.md) — 唯一方法 `StableLabelCompact`；20 档标签沿用；不拆散=名单连续+四连通+关闭冻结；大件旁边未围住的边可补；整理只走 rot 0/1；五入口共用；放不下零提交；direction 退役。
- [总弹药实时 HUD](issues/03-t3-ammo-hud.md) — 留原版当前/上限，追加「总弹药 {0}」；0 也显示；枪上与 ammoLabel 同事实源、身上五页与压弹同匹配；不晚于下一帧；不得以开关背包刷新。
- [换弹技能 2 级改为被动压弹](issues/05-t5-passive-reload.md) — DEV-V7-03 实施票已恢复并 resolved（2026-09-24：独立 8 秒周期、主机权威事务、手动互斥、失败结构化日志；红测/Plugin 全套/三轮 fresh 双轴 CLEAN；真实 PlayerInventory 与 SP/P2P/U3DS 人工门仍归 DEV-V7-07，不授候选）
- [换弹技能原版行模板与换图清零](issues/04-t4-skill-page.md) — 复刻原版行（不复用 SleekSkill）；描述与被动压弹对齐；键=`serverID × characterID × 地图名`；旧全局账丢弃；战斗区优先、设置页降级互斥。
- [绿/红预放置框与自动旋转只走两个正向](issues/06-t6-preview-and-upright-rotation.md) — 半透明绿/红、无描边；框图标提交同一 rot；正向=拖入可读姿态；倒置触发时拉回；感应带与 D2 保留，停用 +1 mod 4。
- [玩家手册与官方功能文案同步](issues/07-t7-copy-and-handbook.md) — 手册三行、对照表三句、身上/容器 Tooltip、整理成功句冻结；技能描述单源；direction 设置行移除；展示层不得改写 T2–T6 行为。
- [第七阶段规格闭包与实施票拆分](issues/08-t8-spec-closure.md) — 到达四条件通过；DEV-V7-01..07；02 独占弹药观察/匹配单源，03 只消费；01 与 05 可并行；07 唯一授候选；Fog 清空；出口 fresh `/to-spec`。
- [DEV-V7-02 弹药观察单源与总弹药 HUD](issues/16-DEV-V7-02-total-ammo-source.md) — **claimed（2026-09-22 实机退回，R1 修复未关单）**：已补库存事件 dirty、HostTick 强制重观察、原版 `UseableGun.ammo` 优先读口、无枪隐藏和 infoBox slot 重建；V7/V5/Plugin/FULLSUITE 本地绿，M1/M2 修复突变证红。但 fresh Spec-Reviewer 仍阻断真实 SDG 观察/真实模块生命周期测试证据不足，三环境日志未回收；不得恢复 resolved 或授候选。

## Not yet specified

无。范围内决策已由 V7-T1..T7 及 V7-R1..R6 收口。

## Out of scope

- **6.5 平台语义图**：公开原版语义 seam、C.1 登记、DEV-V65-*、官方全迁。挂起条件未满足前本图不碰。
- **第五阶段地图列出的玩法展望（另开图）**：创意工坊物品 ID 冲突；Xaero 式地图与中键标点；Carry On 搬箱；网格化放置；快捷栏滚轮手势。
- **第五阶段三个延期项（另开图）**：装备栏锁定 A/B；完整超限供弹（R6/R7/R8 为延期证据）；工坊虚拟容器。
- **第四阶段留给后续可视化**：Item/Blueprint/Creature 选择器；Category 导航；面板 i18n；统一色板；路径调试；BueUi。
- **新官方功能模块 / 新 FeatureId**：本图禁止。
- **永久产品不变量**：不建 BueUi SDK 替代 Glazier；不热卸载外部插件；不用草稿改写 ServerAuthority；不后台自动保存；ADR 0001 不增强拖出到地面；三条原版权威提交不合成。
- **拖放路径自动整理**：更好的物品交互继续禁止在拖放时重排已占用格子；被动整理不挂到该路径。
- **尸潮 HUD、管理面板改版、主菜单入口按钮改版**。
- **整理后合匣（功能 A）**：跨表面共享裁决裁定本阶段保留、不重开、不改语义、不与 2 级被动压弹合并。
- **SPF ResourceObs**：用户 SPF 域。
- **生产实施本身**：红测/候选/实机随 DEV-V7-* 届时产生。
