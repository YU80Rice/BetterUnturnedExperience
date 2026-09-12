# V2 第四阶段：可视化体验定界与规格冻结（Wayfinder 地图）

Type: task
Status: **completed（2026-09-11：8 决策票+2 research 票全部 resolved；spec.md ready-for-agent；DEV-V4-01..09 实施票已发布 ready-for-agent；前沿=DEV-V4-01）**
Label: wayfinder:map
Parent: 无（承接 Phase-3 收官：DEV-V3-01..09 resolved，契约 2.1 / RELEASES 行 11 / 候选 ce0d2191）
Author: GPT（本会话 charting，2026-09-11）

## Destination

BUE Phase 4：可视化体验定界与规格冻结。

到达条件：
1. 本阶段点名的三块表面——管理面板、背包整理游戏内「整理」按钮、面板功能级启停——各自有「本阶段做 / 后续做 / 永不做」的裁决；
2. 未保存草稿与配置保存、描述行、循环切换、两套目录（BUE 功能 / 外部 BepInEx 插件）的界面语义明确；
3. 每项明确本阶段实现、后续阶段或永久不做；
4. 形成无关键歧义、可交 `/to-spec` 的完整规格输入。

本图只产决策，不写生产代码。

## Notes

- 领域：Unturned + BepInEx 5；当前对外部署 = 契约 2.1 / RELEASES 行 11 / 候选 `ce0d2191`。本图 plan-only，生产实施随 `/to-spec` → `/to-tickets` 产生。
- **技能**：决策票必调 grilling + domain-modeling；research 票 AFK 子代理落报告，主会话回写本图指针。勿 `/triage`。实施期硬规则届时生效：红测先行 + 双轴 CLEAN（`docs/agents/output-review-loop.md`）；大写入分批。
- **词汇（开图轮已入典）**：可视化体验 / 未保存草稿 / 功能级启停 / 配置保存。BueUi ≠ Glazier ≠ 可视化体验。
- **开图轮已定、不立票（2026-09-11）**：
  1. 终点 = 可交 `/to-spec` 的规格，不是直接改界面；
  2. 开发者直观体验 = 同一套管理面板，不另做 GUI；
  3. 本阶段三块表面 = 管理面板 + 背包整理游戏内按钮 + 功能级启停；尸潮 HUD、物品拖入、菜单入口按钮改版不进本图；
  4. 不建 BueUi；游戏内表面继续用原版 Glazier（`Glazier.Get()` + `ISleek*`）；
  5. 整理排序规则（O-LIT-1）不算可视化，不进本图；
  6. 管理面板本阶段收：描述、配置行描述+合适控件、循环切换、功能级启停。物品/配方/生物选择器、分类导航、程序集/配置路径 = 后续可视化，不在本图实施范围；
  7. BUE 功能与外部 BepInEx 插件两套目录同等升级；
  8. 背包标题栏本阶段只留「整理」；模式/方向改为全局一份，进 LIT 插件设置（循环切换）；锁定/未锁定为后续表面，本阶段不画；STORAGE 页仍不注入；
  9. 未保存草稿覆盖详情页一切可改项（BUE 设置 + 启停意图 + 外部可编辑配置）；收藏仍即时；关面板或换条目脏时确认「修改尚未保存，要保存吗？」；
  10. 功能级启停进草稿，保存时才 `SetFeatureEnabled`；LIT 设置页不再保留 `enabled` 总开关；
  11. 外部插件配置进同一套草稿；`RequiresRestart` 保存后只提示重启、不热卸载；写失败留草稿并说明原因。
- **票号**：决策票 V4-T1..T8，research 票 V4-R1/R2。实施票届时 DEV-V4-*。
- **主源锚点**：`BueNativeManagementPanel.cs` / `ManagementPanel.cs`；`InventoryTidyUiPatch.cs`；`ContractTypes.cs` 的 `SettingDescriptor`；UPM 快照 `docs/third-party/UnturnedPluginManager-snapshot/`（v26.8.11.3）；UPM 技能包 `docs/third-party/unturned-plugin-dev/`（文档至 v26.8.13.3）；ADR-0002；Phase-3 spec 明确不做 BueUi。

## Decisions so far

- [V4-R1 管理面板与 UPM 控件差集盘点](issues/09-r1-panel-upm-gap.md)：UPM 快照 v26.8.11.3 已有描述行（截断 120）、bool/文本、Cycle、ItemList/BlueprintList、路径；CreatureList/Category 只在技能包、不得写成快照已实现。BUE 模型有 `TryToggleFeature` 未画；契约有 DisplayNameKey/DescriptionKey/Choice 但官方全填 SettingId、生产零条 Choice；外部 AcceptableValueList 被标只读。报告 `research/2026-09-11-V4-R1-panel-upm-gap.md`。
- [V4-R2 背包整理标题栏与设置现状](issues/10-r2-lit-header-settings.md)：每页三按钮注入 Hands..Pants，STORAGE 不注入；模式/方向是每页内存字典（默认同类+降序），Stop 清零；唯一设置 `inventorytidy.enabled` 与生命周期两套开；无策略选择器；Ctrl+左键仍绑在「整理」上。报告 `research/2026-09-11-V4-R2-lit-header-settings.md`。
- [V4-T1 可视化体验跨表面共享裁决](issues/01-t1-shared-visual-rulings.md)：Q15–Q20 已冻结；2.1 公开契约只消费不扩 `IFeatureRegistration`，功能描述若入契约须后续可选 facet/chrome 的 Minor 2.2 方案；官方/生态同控件同草稿同保存，外部插件无进程级启停；Headless 不组 ClientUi、不武装 LIT Glazier；草稿仅活于逻辑面板会话，重挂不丢、关闭/换条目先确认；单次 SettingsRuntime 提交原子，跨权威源才允许部分成功；选择器、路径、锁定、排序、BueUi、热卸载、STORAGE 注入均不做。
- [V4-T2 未保存草稿与配置保存](issues/02-t2-draft-save.md)：草稿只含 ClientPreference、启停意图、外部可编辑配置；ServerAuthority/只读/收藏不进。确认框「修改尚未保存，要保存吗？」保存/不保存/取消；脏时刷新也确认，重挂不确认。保存顺序=设置一次原子 Submit→逐条 ConfigEntry→最后启停（启停非事务尾）。脏=与权威快照最终值不同；行一脏即标未保存；全成功「配置已保存。」，空操作「没有需要保存的修改。」；无后台自动保存。T4 已解锁。
- [V4-T3 描述行、循环切换与配置控件](issues/03-t3-panel-controls.md)：功能级描述=面板 chrome 对照表（生态无表则不画），不进 2.1 契约；DisplayNameKey/DescriptionKey 本阶段当字面中文；配置行=显示名→描述（空不画，截断 120）→控件；Cycle=Choice+AllowedValues 与外部 Cycle/AcceptableValueList 同一控件，到头循环，无档位只读；KeyBinding 无专用捕获；外部只识别 Cycle，选择器标签静默忽略。T5/T7 已解锁。
- [V4-T4 功能级启停的面板表面](issues/04-t4-feature-toggle.md)：开关=保存后目标状态，进草稿；有开关=拥有可停止生命周期 seam（官方+生态+NoOp+Network+v1compat，不含外部插件与管理面板自身）；状态投影映射九态（待启动≠启动中）；保存提交目标，生命周期机解释空操作/停/启/恢复（Isolated+目标停用=空操作成功）；legacy alias 仅 LIT/LIR/LHT/Network/v1compat 的 `*.enabled` 与 BII `Enabled`；目标差异不进 SDK。
- [V4-T5 背包整理标题栏与全局模式/方向设置](issues/05-t5-lit-header-and-settings.md)：标题栏只留 60×60「整理」（headers[0..4]，-130，不含仓储栏）；Ctrl+左键全身整理保留；九态决定注入/拆除；两条 ClientPreference Choice=`inventorytidy.mode`/`direction`（同类/空间/大件，降序/升序）；旧每页内存丢弃；点击只读同一 revision 已保存快照，不读草稿；不登记 satellite、不留锁定空位。T6 已解锁。
- [V4-T6 官方功能描述与设置文案](issues/06-t6-official-copy.md)：功能级一句话=chrome 对照表（BII/LIT/LIR/LHT/Network/v1compat/NoOp）；LIT 模式/方向与 BII AutoRotate 填中文显示名+描述；退役 enabled 不进 descriptor/草稿；网络接管文案本阶段不重写；NoOp 保留 Toggle 另加 `noop.probe-choice`（甲/乙）。
- [V4-T7 外部 BepInEx 插件目录的同等升级](issues/07-t7-external-plugin-parity.md)：不画插件级长描述；可编辑=bool/数字/字符串，Cycle 须可写回；RequiresRestart 行级=固有属性、顶部徽章=本次成功写入；检测到 UPM 仍可编 cfg、不改加载状态；选择器/路径/热卸载不做；失败三类短中文。T8 已解锁。
- [V4-T8 第四阶段规格闭包与实施票拆分](issues/08-t8-spec-closure.md)：三层范围（本阶段做 / 后续可视化 / 永久不变量 + 本阶段运行环境排除）；实施九票 DEV-V4-01..09（生命周期空操作、legacy alias、启停 UI 分缝；01..08 不授候选，09 唯一对外，契约仍 2.1）；实机=agent 部署+用户编号步骤+落盘截图绑 CaseId/环境/SHA-256；雾区清空；出口=fresh `/to-spec`。
- [V4-R9 实时注入裁决（DEV-V4-09 实机二轮，2026-09-12）]：用户二次实机证伪「Start 尾部补注入」（Start 运行在机器 Starting 态，Q55 九态门必然拒绝=结构性死路，实机 194456 包 gen8 零注入行取证），用户原话裁定「**可不可以做成实时注入，打开背包时注入？**」→ 新裁决：**在 Q55 九态门不变（DecideTidyUiAction 表零改动）前提下，允许 Running 态 LIT 模块经既有 Tick 泵（非自建泵，16 拍节流首拍即试）对已构造且仍存活的仪表盘执行幂等补注入；该补注入视为「Running 注入」的生命周期补偿，非 Running 态不得新增按钮；在册去重（Q55 移除失败页引用保留→跳过→下次拆除重试）与 ctor 新仪表盘覆盖路径（v1 旧行为）并存**。落点=DEV-V4-09 修复轮提交；spec 冻结正文不动，本条为 spec 增补裁决（下次 fresh /to-spec 时并入正文）。

## Not yet specified

（空——闭包后无 Fog。后续可视化见 Out of scope。）

## Out of scope

- **后续可视化（另开图）**：锁定页；ItemList/BlueprintList/CreatureList；Category 导航；程序集/配置路径；面板 i18n；`bue.network` 良性隔离文案；统一色板与跨表面尺寸语言；功能级描述可选 facet（Minor 2.2）。整理按钮 60×60/-130 是 LIT 实现约束，不升格为全局 UI 契约。
- **本阶段运行环境排除**：U3DS 不构造本阶段客户端表面；验收只要求不画、不抛、不留脏草稿。
- **当前能力边界**：不给外部插件进程级启停（重做须另开图）。
- **永久产品不变量**：不建 BueUi SDK 替代 Glazier；不把 O-LIT-1 混入本阶段视觉；不注入 STORAGE；不热卸载外部插件；不用草稿改写 ServerAuthority；不后台自动保存；不把外部配置编辑扩展成任意代码执行。
- **尸潮 HUD、BII 拖入、主菜单/暂停「BUE 插件管理」入口按钮改版**。
- **SPF ResourceObs 刷屏**：用户 SPF 域。
- **平台续作**：跨机设置同步、诊断实时视图、SDK 模板、UMM 诊断包自动化、可靠通道。
- **生产实施本身**：红测/候选/实机随 DEV-V4-* 届时产生。
