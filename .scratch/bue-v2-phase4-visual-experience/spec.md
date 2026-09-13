# BUE Phase-4 规格：可视化体验定界与接线

Status: ready-for-agent
来源：`.scratch/bue-v2-phase4-visual-experience/map.md`（completed）+ V4-T1..T8 决策票 Answer + V4-R1/R2 研究报告 + `CONTEXT.md`；本规格只把已裁决内容成文，不重开已关闭争议。
当前对外部署基线：契约 2.1 / RELEASES 行 11 / 候选 `ce0d2191`。本阶段契约只消费 2.1，不扩 `IFeatureRegistration`。

## 事实基线（已建成事实与本阶段缺口）

1. Phase-3 已将八个平台缝接线并发布契约 2.1（DEV-V3-01..09）；`TryToggleFeature` 已接到 `SetFeatureEnabled`，原生面板未画；`SettingDescriptor` 已有 `DisplayNameKey` / `DescriptionKey` / `SettingKind.Choice` / `AllowedValues`，官方全填 SettingId，生产零条 Choice；
2. 管理面板改一项立刻写权威源，无未保存草稿、无「保存配置」；外部 `AcceptableValueList` 被标只读，不认 Cycle；
3. LIT 标题栏每页三按钮（模式/方向/整理）；模式与方向是每页内存字典，唯一设置是 `inventorytidy.enabled`，与生命周期两套开；STORAGE 不注入；
4. 游戏内表面继续用原版 Glazier（`Glazier.Get()` + `ISleek*`）；BueUi 不是本阶段模块，也不是 Glazier 的别名；
5. UPM 快照 v26.8.11.3 已有描述行（截断 120）、Cycle、ItemList/BlueprintList、路径；CreatureList/Category 只在技能包，不得写成快照已实现。

## Problem Statement

玩家打开 BUE 管理面板时，看到的是 SettingId 和立刻写入的开关，没有功能说明、没有循环切换、没有保存确认，也点不到功能级启停。背包整理在每页标题栏挤了三颗小按钮，模式和方向还是内存态，停用后按钮可能留着点了没反应。开发者对照 UPM 会发现描述行和 Cycle 都缺了；对照自己的生态功能，面板上甚至没有一句话说明。

## Solution

把三块产品表面按已冻结裁决接到现有权威源上，不新建 UI 平台：管理面板用未保存草稿和「保存配置」一次提交；配置行改为显示名、描述和合适控件（含循环切换）；详情页用目标启停意图走生命周期机；背包标题栏只留「整理」，模式和方向变成两条客户端偏好 Choice。官方文案进 chrome 对照表；外部插件同等升级描述和 Cycle，但不获得进程级启停。

从使用者视角：改完再保存，脏了会问要不要保存；功能能停能开且状态能读懂；整理按钮一眼能点，模式在设置里用循环切换改；生态样板在面板里能看到描述、Toggle 和 Choice。

## User Stories

1. As a 玩家, I want 在管理面板改设置后先看到未保存标记、点「保存配置」才写入, so that 我不会因为点错一下就立刻改掉运行中的功能
2. As a 玩家, I want 关闭面板或换条目时若有未保存修改被问「修改尚未保存，要保存吗？」, so that 我能选择保存、丢弃或继续编辑
3. As a 玩家, I want 把开关拨回去之后不再被当成未保存, so that 我不被无意义的确认框拦住
4. As a 玩家, I want 保存部分失败时看到哪些项成功、哪些仍未保存及原因, so that 我知道下一步该改什么而不是整页消失
5. As a 玩家, I want 不脏时仍能看见「保存配置」并得到「没有需要保存的修改。」, so that 我找得到按钮、也不会误以为写了盘
6. As a 玩家, I want 配置行显示人读名称和说明而不是 SettingId = 值, so that 我看得懂每一项在干什么
7. As a 玩家, I want 用左键下一档、右键上一档循环切换整理模式和方向, so that 我不必打字填内部枚举
8. As a 玩家, I want 功能详情里有一句话说明官方功能是干什么的, so that 我不用猜 FeatureId
9. As a 玩家, I want 用一颗「启用」开关表示保存后要开还是关、当前状态仍单独显示, so that 我能先改目标再保存，并且分得清「现在」和「保存后」
10. As a 玩家, I want 停用背包整理后标题栏不再留点了没反应的「整理」按钮, so that 我不会以为功能还在
11. As a 玩家, I want 在背包标题栏只看到一颗「整理」、用左键整理当前栏、Ctrl+左键按已保存的全局模式和方向整理全身（不含仓储栏）, so that 标题栏不再挤三颗看不清的按钮
12. As a 玩家, I want 在 LIT 设置里改整理模式（同类/空间/大件）和方向（降序/升序）, so that 所有栏共用一份规则且能持久化
13. As a 玩家, I want 面板里改了模式但没保存时，点「整理」仍用上次已保存的规则, so that 未保存草稿不会偷偷改背包行为
14. As a 玩家, I want 拖入时「自动旋转」有中文名和说明, so that BII 剩下的设置也读得懂
15. As a 玩家, I want 外部插件配置也有描述和循环切换（若插件声明了档位）, so that 我不必对照 UPM 才改得动
16. As a 玩家, I want 需要重启的配置在保存成功后才提示「需要重启」, so that 我不会把「这项本来要重启」当成「已经写进去了」
17. As a 玩家, I want 检测到 UPM 时仍能改它的受支持配置、同时看到 BUE 不会改它的加载状态, so that 两套管理器可以共存而不互卸
18. As a 玩家, I want 外部配置保存失败时看到「插件已卸载」「配置文件无法写入」或「值不合法」, so that 我知道是卸载、磁盘还是填错了
19. As a 生态功能作者, I want 我的功能在面板上与官方使用同一套草稿、同一套控件和同一套保存, so that 玩家管理我的功能时手感一致
20. As a 生态功能作者, I want NoOpFixture 在面板上有一句话、一个 Toggle 和一个 Choice, so that 我能对照样板接描述和循环切换
21. As a 生态功能作者, I want 没有对照表时不画功能级描述、也不出现空白占位, so that 我的条目不会看起来像缺了一块坏掉的文案
22. As a 官方功能维护者, I want LIT 的模式和方向成为真实 Choice 消费者, so that 循环切换不是只在样板里绿
23. As a 官方功能维护者, I want 旧的 enabled 总开关迁成生命周期权威且不再出现在设置页, so that 玩家不会看到两套总开关
24. As a 官方功能维护者, I want 生命周期机把「已隔离再提交停用」解释成空操作成功, so that 面板不必按 FeatureState 写 if
25. As a 仓库维护者, I want DEV-V4-01..08 不授候选、DEV-V4-09 出唯一对外候选且契约仍为 2.1, so that 台账身份唯一、实施中不得顺手扩契约
26. As a Headless 服务端, I want 不构造管理面板、不武装 LIT 整理按钮、不留下客户端草稿, so that U3DS 不会因为本阶段可视化而抛错或改 ServerAuthority

## Implementation Decisions

以下全部来自 V4-T1..T8 已裁决条目。标注冲突时以对应票 Answer 为准；本规格不发明新表面、不新增公开契约成员。

### 共享规则（V4-T1）

- **契约**：本阶段只消费 2.1 已有面（`SettingDescriptor` 的 Choice / DisplayNameKey / DescriptionKey / AllowedValues，以及既有启停缝）。`IFeatureRegistration` 保持四成员。描述行、Cycle、保存按钮、确认弹窗、未保存草稿均属 ClientUi，不进 SDK。`public` ≠ 契约。功能级长描述若将来入契约，须可选 facet 且 Minor 2.2，本阶段不偷渡。
- **同权**：官方与经注册桥的生态功能同一套控件、同一套草稿、同一套保存。外部 BepInEx 插件是第二目录，不是契约同权对象，无进程级启停、无热卸载。官方先行消费：①面板自身完成草稿、保存和功能级启停；②LIT 设置页真实消费模式与方向两条 Choice；③NoOpFixture 带一句话描述、Toggle 和至少一条 Choice。NoOp 不替代官方消费证明。
- **Headless**：可视化验收只针对客户端。U3DS 不构造管理面板、不构造草稿、不自动保存、不让客户端草稿改写 ServerAuthority、不武装 LIT Glazier 注入、不以异常当门禁。
- **逻辑面板会话**：草稿只活在当前条目详情的内存中；关面板、换条目或退出进程即结束。Glazier 重挂 ≠ 会话结束。收藏即时持久化，不进草稿。
- **设置提交原子性**：同一功能一次 SettingsRuntime 提交整单成败、revision 不部分推进。跨权威源（BUE 设置、SetFeatureEnabled、外部 ConfigEntry）允许部分成功。

### 未保存草稿与配置保存（V4-T2 → DEV-V4-01）

- 进草稿：ClientPreference 设置、功能级启停意图、外部允许编辑的配置（含 RequiresRestart 的值）。不进：只读行、状态投影、接管提示、收藏、ServerAuthority。
- 确认框原文：「修改尚未保存，要保存吗？」按钮：保存 / 不保存 / 取消。保存失败不导航。脏时刷新列表也走确认；重挂不确认。
- 脏 = 可编辑字段最终值 ≠ 权威快照。一脏即标「未保存」。不脏点击保存 = 空操作，文案「没有需要保存的修改。」全成功：「配置已保存。」
- 写入顺序：BUE 设置一次原子 Submit（进入详情或上次本源成功后的基准 revision）→ 外部 ConfigEntry 逐条 → 最后启停。启停不是事务尾。无后台自动保存。
- 外部世界变化不自动清草稿、不自动合并。revision 过期：该设置源拒绝，文案「未保存：设置已在别处变更。」

### 描述行与循环切换（V4-T3 → DEV-V4-02）

- 功能级描述来自 chrome 对照表，不进契约；无表则不画。DisplayNameKey / DescriptionKey 本阶段当字面中文。
- 配置行固定：显示名 → 描述（空不画，截断 120）→ 控件。不再画 SettingId = 值。
- Cycle：左键下一档、右键上一档、到头循环。覆盖 Choice+非空 AllowedValues，以及外部 Unturned.Cycle / AcceptableValueList。无档位 Choice 只读，不降级文本框。
- Toggle / Integer / Float / Text 维持形状，只补显示名和描述。KeyBinding 无专用捕获。
- 只读行：显示名 + 非空描述 + 只读当前值；不画灰掉的假控件；不进草稿。

### 生命周期目标提交（V4-T4 → DEV-V4-03）

- 面板只提交目标启用或停用。生命周期机按提交时权威状态解释：一致则空操作成功（含 Isolated 且目标停用：保持隔离，不走会失败的 disable）；目标停用且正在跑 → UserDisabled；目标启用且已停用或已隔离 → 启用/恢复并新代际；不允许则失败并留草稿。
- 现网 SetFeatureEnabled 对「已停再停 / 已跑再开 / Isolated+disable」返回失败——空操作成功是生命周期机语义修复，不是 ClientUi 分支。
- 目标差异投影不进 SDK。

### 官方 legacy enabled 迁移（V4-T4 → DEV-V4-04）

- 仅显式登记的 legacy lifecycle alias：LIT / LIR / LHT / Network / v1compat 的旧 `*.enabled`，以及 BII 的 `Enabled`。不登记 AutoRotate、NoOp probe-toggle、未声明 alias、生态功能。
- 旧值 false → 写入 UserDisabled 意图事实（再由生命周期机解释是否实际停用）；true 或不存在 → 不额外改生命周期；已有新权威则以新为准。幂等；成功前不丢旧值；成功后旧字段从 schema 与面板退役。不按字段名扫描。

### 功能级启停表面（V4-T4 → DEV-V4-05）

- 详情页「启用」开关 = 保存后目标状态，进草稿。当前 FeatureState 只读。有开关 = 拥有可停止生命周期 seam（官方、生态、NoOp、Network、v1compat）。不显示：外部插件、管理面板自身、核心 Host/Contracts。
- 状态投影映射：Running=运行中；Disabled/Stopped（UserDisabled）=已停用；Isolated=已隔离；Starting=启动中；Stopping=停用中；Isolating=隔离处理中；Discovered=待启动；Incompatible 与未映射=不可用（不显示开关）。表现状态独立一行。隔离原因有值才显示。`bue.network` 良性隔离文案本阶段不改。

### LIT 标题栏与全局模式/方向（V4-T5 → DEV-V4-06）

- 仅注入 headers[0..4]（Hands/Backpack/Vest/Shirt/Pants），不注入仓储栏。一颗 60×60「整理」，PositionOffset_X=-130。左键当前栏，Ctrl+左键全身（不含仓储栏）。不登记 ClientUi satellite。不留锁定空位。
- 九态决定注入/拆除：仅 Running 新开页注入；Disabled/Stopped/Isolated/Incompatible 拆除已有按钮；过渡态不新增，已有则点击安全回退、不报假成功。可用性由生命周期事实决定，不由 patch 私有布尔。
- 两条 ClientPreference Choice：`inventorytidy.mode`（同类/空间/大件，默认同类）、`inventorytidy.direction`（降序/升序，默认降序=大件优先）。旧每页内存丢弃。点击只读同一 revision 的已保存 ClientPreference 快照，不读面板草稿。
- 坐标与 tooltip 是 LIT 实现约束，不升格为全局 UI 契约。

### 官方文案与 NoOp（V4-T6 → DEV-V4-07）

功能级对照表（原文引用）：

| 条目 | 一句话 |
|---|---|
| 更好的物品交互 | 在支持的格子里增强拖入，失败时回到原版操作。 |
| 背包整理 | 整理背包与装备栏物品；模式和方向在本页设置，背包标题栏点「整理」。 |
| 更好的换弹体验 | 换弹尽量留在原位；背包整理完成后自动压缩弹药。 |
| 更好的尸潮播报 | 由主机追踪尸潮，并在客户端显示播报。 |
| BUE 网络模块 | 为 BUE 功能模块提供多人通信通道；可停用，停用不等于卸载。 |
| BUE V1 兼容层 | 为仍使用数字频道的旧插件提供兼容接收，不提供新的注册入口。 |
| NoOpFixture | 生态接入样板，用于展示功能描述、Toggle 和 Choice 在面板中的呈现。 |

设置文案：`inventorytidy.mode` 显示名「整理模式」，描述「同类：把相同物品聚在一起；空间：优先保留大块空位；大件：优先放置大件。对当前栏整理和全身整理都生效。」；`inventorytidy.direction` 显示名「整理方向」，描述「降序：大件优先；升序：小件优先。与整理模式共同决定整理顺序。」；BII `AutoRotate` 显示名「自动旋转」，描述「拖入时自动旋转物品以适配空位。」退役 enabled 不进 descriptor/草稿。网络接管文案本阶段不重写。NoOp 保留 `noop.probe-toggle`，另加 `noop.probe-choice`（甲/乙，默认甲），二者都不是生命周期代理。

### 外部配置同等升级（V4-T7 → DEV-V4-08）

- 不画插件级长描述。配置行描述来自 ConfigDescription，空不画，截断 120。
- 可编辑基础类型：bool、数字、字符串。Cycle 仅当值类型受支持、约束为 Unturned.Cycle 或 AcceptableValueList、候选非空且可写回。未识别离散约束不变成选择器。
- RequiresRestart：行级=固有属性；顶部徽章仅针对本次保存成功写入的项。
- 检测到 UPM：仍可编受支持 cfg；底栏「不修改其状态」指加载与运行状态。
- 失败三类短中文：插件已卸载 / 配置文件无法写入 / 值不合法。结构化 adapter 结果，不匹配异常文本。

### 实施与发布纪律（V4-T8）

九票号冻结：01 草稿 → 02 控件 / 03 生命周期空操作 / 08 外部配置；03→04 迁移、05 启停 UI；02→05/06/07/08；04→05；06→07；01..08→09 画面验收+唯一候选。01..08 不授候选、不加 RELEASES、不授 CaseId。09 唯一对外候选，契约仍 2.1。实施中若须新增公开成员：停候选链，另开 2.2，不得在实施票顺手扩面。

## Testing Decisions

- **只测外显行为**：经公开或面板 seam 断言结果（草稿脏/干净、保存成功/部分失败文案、Cycle 档位、启停目标与 FeatureState 投影、整理按钮有无、设置快照 revision、legacy 迁移后无 enabled 行）。不断言 Glazier 控件树内部结构、私有字典或锁。
- **红测先行**：每张实施票先立红测锚点再实现；红绿后双轴独立审查（standards-reviewer / Spec-Reviewer）→ CLEAN 才交付。
- **测试缝与既有先例**（不新建测试工程；优先复用既有七个测试工程直跑口径）：
  1. 草稿与保存：ManagementPanel 模型缝（现状 `TryEditBueSetting` 立刻 Apply 的反例改为草稿命令）；确认三选一；跨源部分成功；SettingsRuntime 单次提交原子与 ExpectedRevision。先例=ClientUi / Settings.Tests 既有组。
  2. 描述与 Cycle：SettingEntryView 须投影 DisplayNameKey / DescriptionKey / AllowedValues / Kind；Choice 无档位只读；外部 Cycle 识别。先例=Settings.Tests Choice 组 + Plugin.Tests 面板模型。
  3. 生命周期目标提交：空操作成功（已 Running 再启用、已 UserDisabled 再停用、Isolated 且目标停用）；启用已隔离 → 新代际。先例=Plugin.Tests SetFeatureEnabled 组（现网失败码须先红后改）。
  4. legacy alias：六项官方 false→UserDisabled 幂等；未声明 enabled 不迁；成功前旧值仍在。先例=Settings.Tests 持久化组。
  5. 启停表面：有开关 iff 可停止生命周期 seam；状态投影九态中文；草稿目标与只读状态分离。先例=ManagementPanel TryToggleFeature 组。
  6. LIT：仅 Running 注入；停用后拆除；mode/direction 两条 Choice；点击读同一 revision 快照。先例=LIT 模块测试 + InventoryTidyUiPatch 现状锚。
  7. 文案与 NoOp：对照表七句；LIT/BII 显示名；`noop.probe-choice` 甲/乙。先例=NoOpFixture 契约 probe。
  8. 外部配置：描述采集；Cycle 可写回才画 Cycle；失败三类短中文；UPM GUID 仍可编 cfg。先例=LoadedPluginCatalogAdapter 组。
  9. 画面验收：SP 完整面板+草稿+启停+整理按钮；P2P 跨端点（停网络不影响整理）；U3DS 不画不抛不留草稿。证据=落盘截图绑 CaseId/环境/SHA-256 + UMM LogOutput；聊天贴图不是唯一证据。
- **官方先行消费锚**：面板自身草稿+保存+启停；LIT 两条真实 Choice；NoOp 描述+Toggle+Choice（不替代 LIT）。
- **测试工程归属**：契约形状仍走 Contracts.Tests；运行时与面板模型走 Plugin/Settings 等既有工程，不新建测试工程。

## Out of Scope

**后续可视化（另开图）**：锁定页；ItemList / BlueprintList / CreatureList 选择器；Category 导航；程序集/配置路径；面板 i18n；`bue.network` 良性隔离文案；统一色板与跨表面尺寸语言；功能级描述可选 facet（Minor 2.2）。

**本阶段运行环境排除**：U3DS 不构造本阶段客户端表面；验收只要求不画、不抛、不留脏草稿。未来服务端管理表面须另开图。

**当前能力边界**：不给外部插件进程级启停；重做须另开图，不得在 DEV-V4 偷运。

**永久产品不变量**：不建 BueUi SDK 替代 Glazier；不把 O-LIT-1 排序规则混入本阶段视觉；不注入 STORAGE；不热卸载外部插件；不用草稿改写 ServerAuthority；不后台自动保存；不把外部配置编辑扩展成任意代码执行。

**其它**：尸潮 HUD、BII 拖入、主菜单/暂停入口按钮改版；SPF ResourceObs；跨机设置同步、诊断实时视图、SDK 模板、UMM 诊断包自动化。不重开 T1..T8 已关闭争议。

## Further Notes

- **DEV-V4 实施票计划（V4-T8 冻结）**：九张，票号不得临时重排——01 草稿与保存模型、02 描述行与 Cycle、03 生命周期目标提交与空操作、04 官方 legacy enabled 迁移、05 功能级启停详情页、06 LIT 标题栏与 mode/direction、07 官方文案与 NoOp Choice、08 外部配置同等升级、09 客户端画面验收与唯一对外候选。真实依赖见 Implementation Decisions「实施与发布纪律」。
- **候选策略**：01..08 不授候选、不加 RELEASES、不授 CaseId；09 生成唯一 Phase-4 对外候选，契约仍 2.1。中间诊断构建 ≠ 候选 ≠ 发布物。
- **实机**：agent 代部署、用户只做游戏内编号步骤；截图落盘绑 CaseId/环境/构建身份/步骤/阶段（before/dirty/saved/failure）；一次一张图不影响证据完整性。
- **下一步**：`/to-tickets` 已按九票与依赖边发布 DEV-V4-01..09（`issues/DEV-V4-01..09-*.md`，ready-for-agent），每票独立会话 `/implement`；前沿=DEV-V4-01（无阻塞）。
