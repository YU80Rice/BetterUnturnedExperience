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

- [T2：三插件源码盘点与迁入形态](issues/02-three-plugin-source-inventory.md)：三插件全为 LMN 命名频道消费方（零 V1 API,频道 id 已录）;`IBueNetworkApi` 缺公开入站订阅面+寻址改 IConnectionSession;难度 LHT<LIR<LIT;推荐 EmbeddedOfficial 单 DLL+`IFeatureModule`,不碰 V1 兼容层。报告 `research/2026-09-06-three-plugin-source-inventory.md`。

## Not yet specified

- **SDK 分发与版本策略**：第三方开发者从哪拿引用 DLL、BUE 版本与契约版本如何对应、示例插件形态——挂在 T3（生产绑定）与 T7（契约）结论上，未到可立票粒度。
- **玩家迁移指引**：从「BUE+LMN+三插件」旧部署到「只装 BUE」的迁移说明与 LMN DLL 保留语义（未知 V1 旧插件仍需 LMN 在场）——挂在纳入实施形态上。
- **原仓库退役动作**：Archive 三仓库停维护的公告/迁移说明——挂在 T2 盘点结论上。
- **实施期工程**：三环境验收票、RELEASES 加行、真机手册——随 `/to-spec`→`/to-tickets` 产生，不属于本决策图。

## Out of scope

- **DEV-V2-12 N-1**（LMN 出站首 ping 竞态，真实 target）：LMN 内部出站路径，BUE 未触碰，保持挂起；如需根治另立票。
- **DEV-V2-13 具名设计取舍 2-5**（面板 seam gap/J2 几何常量/PauseSpyColumnButtonX/Data Clump）：已有意不动，非本图议程。
- **其他能力迁移**（本地联机、性能优化、背包整理以外的 Launch 能力等）：旧地图明示的后续阶段，超出本目的地；目的地重绘时作为新 effort。
- **git 远程备份**：基础设施 chore，与图无关（基线已入库，待用户提供 URL）。
- **08 的截图/时间窗采集 gap**：流程改进项，非本图。
