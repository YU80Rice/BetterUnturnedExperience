# DEV-V7-04 审计：原版技能行与换图清零

- Ticket: `18-DEV-V7-04-skill-row-scope.md`
- Red group: `--bue-v7-04-skill-row-scope-red`
- Baseline: DEV-V7-03 resolved，最终提交 `8e9f780`
- Candidate discipline: 本票不授候选、不写 `audit/RELEASES.md`、不授 CaseId、不生成正式交付包。
- Contract: 2.1；无新 FeatureId、无 SDK 扩面。

## 退回问题与归属

2026-09-22 DEV-V7-07 实机复核将以下问题明确归属 DEV-V7-04：原实现是多行 Label + 独立半宽 Button 的自造底部文本区；缺少整行点击、三格锁条、80/90 原版几何；等级账没有按 `serverID × characterID × 地图名` 分家。该退回不归因 DEV-V7-03，被动压弹实现保持独立。

## 实施闭包

1. 生产等级键：`serverID × SteamID × characterID × mapName`。SteamID 保留用于多人同服同槽玩家隔离，角色维度使用原版 `characterID`，不使用显示名。
2. 旧 `better-inplace-reload.skill-levels.dat` 不读取、不迁移、不作为新世界初值；生产使用设置根下 `in-place-reload/skill-scopes/*.dat`，临时文件替换写入。
3. 原版行复刻使用 Glazier 原语，不构造 `SleekSkill`，不写 `Skill[][]`，不走 `sendUpgrade`。模型为单条 80 高 / 90 步进，含名称、当前/满级、三句描述、花费或 `Full`、三格锁条和整行经验门。
4. 战斗区和设置 fallback 共用 scope 账、projection 和主机升级入口；注册期与启动期 U3DS headless 均不画且不转设置页。设置页 descriptor 按当前确认等级/经验投影，只暴露当前下一等级；经验不足仅保留维持项。
5. 客机镜像绑定当前作用域；换图/换槽时清镜像和被动资格。升级/等级状态回执携带发送时作用域，最终消费时比对，旧作用域回执丢弃。
6. V7-03 被动压弹只继续消费等级账，不依赖技能 UI、调度器或事务服务；8 秒周期和总弹药 HUD 未改。

## 红→绿链

1. 新增 `DevV704SkillRowScopeTests`、工程登记和 `--bue-v7-04-skill-row-scope-red` 入口后，首轮 Release Rebuild 按预期编译红：缺少 `ReloadSkillScopeKey`、单行投影字段和 fallback seam。
2. 作用域账、按世界文件持久化、单行模型、Glazier 全幅按钮/锁条、引擎 scope 接线和网络回执 scope 捕获完成后，定向红测转绿。
3. 受影响 V5-07 旧 UI 阶梯断言迁移为 V7-04 单行断言；V7-03 回归保持通过。
4. 首次 FULLSUITE 唯一失败是 V5-08 IL 守卫仍锚定已退役 `CharacterKeyOfPlayer`；测试锚迁移到 `ScopeOfPlayer` 后 FULLSUITE 通过。
5. Spec 首轮发现两个 blocker：U3DS 注册期可能降级到设置页；设置页 fallback 静态列出 0/1/2，未按当前等级/经验投影。修复为注册期 headless 直接返回无 settings facet 的 Registration；Start/Stop 对称绑定/清理当前等级与经验 provider，descriptor 使用当前单行 projection，仅暴露当前下一等级且经验不足仅保留维持项。

## 当前验证

- `--bue-v7-04-skill-row-scope-red`：PASS（含 headless/fallback blocker 回归、注册后惰性 descriptor projection）
- `--bue-v5-07-reload-skill-red`：PASS
- `--bue-v7-03-passive-reload-red`：PASS
- Release Rebuild：PASS（`dotnet msbuild BetterUnturnedExperience.sln -t:Rebuild -p:Configuration=Release`）
- FULLSUITE：`steps=17 pass=16 failed=0 known-baseline=1`，`FULLSUITE: PASS`
- 已知基线：`Gates:NoUiTokens:Contracts` 命中 `ContractTypes.cs:Glazier`，为 V2-02 冻结基线，不计失败；防火墙 `violations=0`。

## 双轴审查链

### Round 1（全新实例）

- Standards：`FINDINGS`；硬 blocker 为工作树范围污染，另有兼容双轨、provider 取数、网络 scope 链和测试重复等 judgment smell。
- Spec：`FINDINGS`；blocker 为 U3DS 可能注册 settings fallback、设置页没有当前 projection/经验门。

处理：V7-04 文件精确 staged，排除已有 V7-02/V7-05/实机/第三方过程物；增加 headless 注册门禁、动态 settings projection/provider、经验门和新红测断言。

### Round 2（全新实例）

- Standards：`CLEAN`；无硬阻塞。兼容层重复、旧测试构造器、`productionRoot` 命名、provider Feature Envy、`FallbackDescription` Middle Man 和记录双形态均列为不阻塞 judgment smell。
- Spec：`CLEAN`；无 Missing / Scope Creep / Wrong Implementation blocker。确认 scope 切换清镜像/被动代际并由下一 HostTick 重请求，设置 descriptor 在注册后惰性读取 Start provider，U3DS 不转设置面，且未触及 BII/HUD/整理/V7-03 核心。

Round 2 关门条件满足：双轴均无 blocker，未授候选、未改 RELEASES、未授 CaseId。

## 视觉重开（2026-09-25）

- 实机退回事实：候选 `1A2A9A9782CBC52C6A070C1FDA3CA145D8CBF18B4D06DA2FCC157235DEB4CF15` 的技能行仍有原版结构偏差；诊断包仅有登记日志，没有视觉树，截图是发布门事实。
- 红测先行：在旧实现上新增布局 seam 红测，首次运行失败，明确报错“战斗区必须暴露可测试且由真实 Render 消费的布局 seam”。
- 最小修复：移除额外分区 Box 与 8px 顶隙；每条技能行直接挂到 `skillsScrollBox`，行根高 80、步进 90；按钮 `SizeScale_X/Y=1`；名称/描述/花费使用 `UpperLeft/LowerLeft/LowerRight`；文本和锁条全部挂在 80 高行根；滚动内容高恢复 `(vanillaRows + rowCount) * 90 - 10`，清除恢复 `vanillaRows * 90 - 10`。
- 生产与测试共用 `ReloadSkillDashboardLayout` seam；红测覆盖首行、第二行步进、按钮铺满、三种对齐、行根父级、锁条定位和追加/清除高度。
- 红转绿：视觉红测 PASS；V5-07、V7-03 回归 PASS；FULLSUITE `steps=17 pass=16 failed=0 known-baseline=1`，防火墙 `violations=0`。
- 本轮未改技能账、描述文案、升级权威、被动压弹、设置 fallback、BII、总弹药 HUD 或整理算法。

## 本轮双轴审查

- Round 3（全新实例）：Standards `CLEAN`；Spec `CLEAN`。
- Standards 未发现硬性标准违规；仅记录布局记录属性重复、裸像素值和测试断言字段命名等不阻塞 judgment smell。
- Spec 确认无 Missing / Scope Creep / Wrong Implementation blocker：原版对齐、行根父级、按钮铺满、锁条定位、90px 步进、追加/清除高度公式均与冻结规格一致。
- Round 3 关门条件满足；真实游戏内视觉、锁条纹理及 SP/P2P/U3DS 玩家人工门仍属于 DEV-V7-07，不由本轮机器审查代替。


真实 Glazier 可见几何、锁条纹理在游戏内的最终视觉和 SP/P2P/U3DS 主机人工验证仍属于 DEV-V7-07 实机门；本票不伪造实机通过，不修改 BII、总弹药 HUD、整理算法或 V7-03 被动周期。

## 结论

本票机器红测、受影响回归、FULLSUITE、双轴审查和 staged 边界已闭合；本票可提交，但不生成候选、正式交付包或 CaseId。