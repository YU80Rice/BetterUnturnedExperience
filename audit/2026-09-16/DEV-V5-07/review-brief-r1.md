DEV-V5-07 双轴审查简报（R1）
========================================

## 票面与规格锚
- 票：`.scratch/bue-v2-phase5-official-optimization/issues/DEV-V5-07-reload-skill-0-to-2.md`
- 规格：`spec.md` 第 107 行「弹药 HUD 与换弹技能 0～2」+「Testing Decisions」+「实施纪律」
- 决策：`issues/07-t7-reload-skill-hud.md`（T7 Answer，三层分家/等级在 U 菜单/0 级不取消技术闸）
- 研究：`research/2026-09-14-V5-R5-reload-hud-skills.md`（现网 A/B 边界、askSpend 无余额门、原版无后备 API、LIR 无 settings facet）

## 用户当场裁定参数（票面 Comments 2026-09-16 落名，测试只引用这些常量）
- XP_COST_LEVEL_0_TO_1 = 125；XP_COST_LEVEL_1_TO_2 = 150；LEVEL0_EXTRA_COOLDOWN_SECONDS = 8
- AUTO_ROUND_DELAY_SECONDS = 8（沿用规格建议，本阶段不可设）；MAX_SKILL_LEVEL = 2
- 技术闸 CooldownSeconds = 1.5 沿用现网不删不改

## 本票实现的接缝（设计，非缺陷）
1. **三层分家**（T7 定音，不得混成一个设置字段）：
   - 进度层 = ReloadSkillStore（按玩家/角色 steamId×characterKey 的等级账，自有文件持久化，不写 *.bue-settings、不写原版 Skill[][]）
   - 运行状态层 = ReloadSkillRuntime（合并技能窗：0 级=技术闸1.5+额外8=9.5s 单窗；≥1 级技能层不武装）+ ReloadAutoRoundScheduler（2 级待压表）+ ReloadSkillLevelMirror（客户端确认镜像）——全部 Stop 即清，不过代际
   - 投影层 = ReloadSkillSectionModel（等级阶梯 0/1/2 + 至多一颗下一级升级按钮，无三级无占位）+ ReloadSkillSettingsSurface（表面 B 降级档位）+ LirRepackWireCodec kind 3-7
2. **官方先行消费**：真实双击 R（ReloadInputDriver→OnDoubleTapReload→NetService.ExecuteRepackFor）前置技能窗；等级读写全走 LIR 存储。
3. **两种 presentation adapter**（票面裁决 3）：表面 A=U 菜单战斗区下方分区（ReloadSkillDashboardBinder 显式解析恰一 updateSelection 宿主可证 + postfix + Glazier 注入薄壳）；表面 B=功能设置页（InPlaceReloadFeatureRegistration 注册期探测 ProbeSurfaceA 选路，接不上才挂 IFeatureSettingsRegistration facet）。
4. **引擎缝**：ILirSkillHooks + LirSkillEngineHooks（NoInlining，askSpend 扣原版经验/退款/characterName 读/枪械指纹 equipment.state），测试 SkillHooksForTests 全替换，生产路径宿主不进。
5. **网络**：功能私有频道加性扩 kind 3(升级请求)/4(回执)/5(等级状态)/6(冷却通知)/7(等级求取)；契约仍 2.1 零扩面。

## 已知具名接缝缺口（随 DEV-V5-08 实机验收，非本票缺陷）
- 真机 Glazier 注入几何（updateSelection postfix 每拍重建、滚动容器扩展、按钮点击回调实链）
- askSpend 各角色（SP/房主/客机）扣原版经验的复制行为、退款路径
- 引擎身份解析（channel.owner.playerID.characterName、equipment.state 指纹真值）
- 表面 A 探测真机是否命中（ProbeSurfaceA 在宿主可证=真，真机若漂移则自动降级表面 B）
- P2P/远端玩家升级回包定向、冷却通知毫秒主机口径

## 审查范围（diff）
见 `changed-files.txt`。生产改动集中在 `src/BetterUnturnedExperience.Lir/Skill/`（新增）+ InPlaceReloadModule/LirRepackNetwork/LirRepackWireCodec/LirProductionAuthority/InPlaceReloadFeatureRegistration；测试 `tests/BetterUnturnedExperience.Plugin.Tests/DevV5ReloadSkillTests.cs`（新增，6 组）+ Program.cs 注册两处。

## 判据自查（供 Spec 轴逐条核）
- [x] 票面先落具名常量，测试只引用常量（组1 是唯一字面出现处）
- [x] 红测先行：0 级双击仍可用；1 级额外冷却消失技术闸仍在；2 级等待后再压条件不满足取消；升级扣经验；无三级 UI——先红（CS0246）后绿，M1-M8 证红
- [x] 官方先行消费：真实双击 R 与等级读写走更好的换弹体验（组5）
- [x] 候选纪律：不产候选/RELEASES/CaseId（diff 无 publish/DLL/RELEASES）
