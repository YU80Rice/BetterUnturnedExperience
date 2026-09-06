# V2 第二阶段：三插件官方纳入与平台首公里（Wayfinder 地图）

Type: task
Status: **open（2026-09-06 建图，R1/R2 决策冻结，7 张子票就位）**
Label: wayfinder:map
Parent: 无（承接 [[bue-v2-phase1-progress]]：V2 第一阶段 01–13 全闭环）
Author: GPT（本会话 charting）

## Destination

**三插件官方纳入与平台首公里**：玩家只装 BUE 单 DLL（无 LMN、无三插件 DLL）时，四个官方功能（更好的物品交互/背包整理/更好的换弹体验/更好的尸潮播报）在单人/SteamP2PFriends/U3DS 三环境全部可用；面板显示官方中文名；BueNetworkApi 有生产传输绑定且三插件是第一批真实消费者；未知 V1 旧插件的共存承诺不破坏（裸 BUE 无 LMN 环境除外）。到达 = 可交 `/to-spec` 的完整规格。

## Notes

- 领域：Unturned（U3DS 2022.3.62）+ BepInEx 5.4.23.5；LIT/LIR/LHT 为 Launch 系列三插件，已实锤 `[BepInDependency(LMN,Hard)]` 且直调 LMN `ModTransport` 命名频道 API——纳入 = 源码改写（08 实锤，非迁移缺陷）。
- 词汇（已入典 `82c13df`）：产品语言用**数字频道/命名频道**，V1/V2 是内部/历史词汇；频道身份是 FeatureId 不是 GUID。
- 已拍板：源码迁入本仓库（单事实源，原 Archive 仓库停维护）；O-LIT-1（LIT 排列算法）=图内独立决策；维持未知 V1 旧插件承诺（不动 V1 兼容层、无新数字频道注册入口）；未公开分发，每票候选+RELEASES 节奏在实施期延续；官方显示名四件中文名（BII=更好的物品交互/LIT=背包整理/LIR=更好的换弹体验/LHT=更好的尸潮播报；管理条目身份仍 FeatureId）。
- 技能：grilling + domain-modeling（每决策票必调）；research（查证）；实验环境坑见 [[bue-build-env-pitfalls]]。
- 实施期硬规则（届时生效）：红测先行 + 双轴独立审查（standards-reviewer/Spec-Reviewer 专属智能体，勿用 general-purpose）→ CLEAN 才交付（docs/agents/output-review-loop.md）。
- 主源锚点：08 结单与 kit（`audit/2026-09-05/DEV-V2-08/`，三插件已重建成功）；BueNetworkApi 契约面 `src/BetterUnturnedExperience.Contracts/ContractTypes.cs`（BueNetwork 命名空间）；接管决策核 `src/BetterUnturnedExperience.Plugin/NetworkModuleAdapter.cs`（12 的 live/inert 两态契约）。

## Decisions so far

- [T1：BepInEx 解析机制实证与 Forge-like 可行性](issues/01-bepinex-resolution-mechanism.md)：前置按 GUID、IL 绑定按 AssemblyName,均与文件名无关——**Forge-like 承诺可行**(冻结 GUID+AssemblyName 即可);同 GUID 双装=BepInEx 留一跳一无双 Awake;**二次勘误:Awake 序=GUID 拓扑序,「按文件名序加载」系讹传**。报告 `research/2026-09-06-bepinex-resolution-mechanism.md`。
- [T2：三插件源码盘点与迁入形态](issues/02-three-plugin-source-inventory.md)：三插件全为 LMN 命名频道消费方（零 V1 API,频道 id 已录）;`IBueNetworkApi` 缺公开入站订阅面+寻址改 IConnectionSession;难度 LHT<LIR<LIT;推荐 EmbeddedOfficial 单 DLL+`IFeatureModule`,不碰 V1 兼容层。报告 `research/2026-09-06-three-plugin-source-inventory.md`。
- [T3：BueNetworkApi 生产传输绑定设计](issues/03-buenetworkapi-production-transport-binding.md)：六决全落——入站订阅升契约(单方法+ChannelDirection 方向枚举,双 handler 表);`IFeatureBootstrap.Network` 携带 API;BUE 帧消费 seam 与 LMN 接管 seam 拆分(patch 门=网络模块启用,探针 false 收窄为零 LMN 相关动作);发送=会话驱动组播(established-only 快照,新增 `PartialFailure`);网络模块自动握手(pending 会话内部可见,Ack 按 peer+代际匹配);**线帧魔数 BUE2→BUE1,产品语言一律「BUE 帧」,数字频道只留兼容不注册(重申)**。落地顺序 Q1→Q2→Q4→Q5→Q3;契约版本随冻结面变更升级。
- [T4：LIT(背包整理)纳入方式](issues/04-lit-adoption.md)：算法原样迁移但立即立 `ITidyStrategy` seam(内置 adapter `default-grid-v1`;**O-LIT-1 勘误:要改的是排序规则,非放置算法**);FeatureId `io.github.yu80rice.bue.inventory-tidy`(面板=频道一词一贯,显示名「背包整理」);本期只持久化 `enabled`(ClientLocal,关→原生回退);熔断 scope 改绑 BUE 连接代际(JSON 功能私有权威不变);TIDY_TEST_HARNESS 归档不进玩家 DLL(新 seam 红测重写);LIT 发布 TidyCompleted 功能事件供 LIR 订阅(TypeByName/跨功能 Harmony postfix 消除)。可交 /to-spec。

## Not yet specified

- **SDK 分发与版本策略**：第三方开发者从哪拿引用 DLL、BUE 版本与契约版本如何对应、示例插件形态——挂在 T3（生产绑定）与 T7（契约）结论上，未到可立票粒度。
- **玩家迁移指引**：从「BUE+LMN+三插件」旧部署到「只装 BUE」的迁移说明与 LMN DLL 保留语义（未知 V1 旧插件仍需 LMN 在场）——挂在纳入实施形态上。
- **原仓库退役动作**：Archive 三仓库停维护的公告/迁移说明——挂在 T2 盘点结论上。
- **实施期工程**：三环境验收票、RELEASES 加行、真机手册——随 `/to-spec`→`/to-tickets` 产生，不属于本决策图。

## Out of scope

- **整理排序规则变体**(compact/按类别分组/最少移动等 `ITidyStrategy` 变体、StrategyId 进设置):T4 已澄清用户诉求=排序规则而非放置算法;seam 已预留,立后续「设置与策略治理」/能力票,超出本图目的地。
- **DEV-V2-12 N-1**（LMN 出站首 ping 竞态，真实 target）：LMN 内部出站路径，BUE 未触碰，保持挂起；如需根治另立票。
- **DEV-V2-13 具名设计取舍 2-5**（面板 seam gap/J2 几何常量/PauseSpyColumnButtonX/Data Clump）：已有意不动，非本图议程。
- **其他能力迁移**（本地联机、性能优化、背包整理以外的 Launch 能力等）：旧地图明示的后续阶段，超出本目的地；目的地重绘时作为新 effort。
- **git 远程备份**：基础设施 chore，与图无关（基线已入库，待用户提供 URL）。
- **08 的截图/时间窗采集 gap**：流程改进项，非本图。
