# DEV-V7-04：原版技能行与换图清零

Type: task
Status: resolved
Parent: spec.md（V2 第七阶段规格·现有官方功能玩法手感定界与接线）
Blocked by: None (DEV-V7-03 行为语义已确认；本票不依赖 03 内部实现)
Spec: `../spec.md`（「技能行与换图清零（V7-T4 → DEV-V7-04）」节）
Red: `--bue-v7-04-skill-row-scope-red`

## What to build

U 菜单战斗区下方出现一行看起来像原版的换弹技能：名称、等级、描述、花费或 Full、整行可点、三格锁条。用原版经验升级。换一张地图，等级从 0 开始，和原版技能一样。旧的全局满级账丢掉。接不上战斗区时，才在本功能设置页显示同一套。

## Scope

- 本票拥有：技能等级账、原版行视觉结构、升级权限、地图×角色槽作用域、两表面互斥。
- 键 = 服务器身份 × 角色槽 × 地图名。不写原版技能数组，不占原版槽。旧全局账不迁移。
- 0/1/2 描述按规格完整三句。2 级必须写被动压弹语义，不得写「双击后再等一轮」。
- 花费仍 125/150。几何靠近原版行高，不用现网自造矮行。
- 探测失败才降级到设置页。两表面不得同时画，只共享同一账和同一主机升级入口。U3DS 不画。

## 隔离（语义等待 ≠ 代码依赖）

- **不得**调用被动压弹调度器、事务服务或 03 的内部类型来驱动技能页。
- 技能页**不是**被动压弹的启动入口。
- 被动压弹**不得**反过来读技能 UI 状态判断等级；等级事实在账本。
- 本票不实现 8 秒周期，不改总弹药 HUD。

## 验收条件

- [x] 红测先行：三元组作用域（换地图为 0、换角色槽不串账）；不写原版技能数组；描述三句逐字；两表面互斥；经验不足不可点。先红后绿。组名 `--bue-v7-04-skill-row-scope-red`
- [ ] 官方先行：更好的换弹体验战斗区或设置页降级面能升级（真机几何随 07）
- [x] 测试证明技能账可在无技能 UI、无被动调度器的情况下读写
- [x] 双轴独立审查 CLEAN
- [ ] **候选纪律**：不授候选 / RELEASES / CaseId

## Answer

- 实施完成：技能账改为 `serverID × SteamID × characterID × mapName` 作用域；生产使用 `in-place-reload/skill-scopes/` 五字段文件，旧全局技能账不读取、不迁移。角色槽使用原版 `characterID`，不使用显示名。
- 行面完成：战斗区使用 Glazier 原语复刻单条原版风格行，80 高、90 步进、整行按钮、三格锁条、0/1/2 三句冻结描述、125/150 花费与 2 级 `Full`；经验不足整行不可点。未写 `Skill[][]`，未构造 `SleekSkill`，未调用原版 `sendUpgrade`。
- 表面与生命周期：战斗区和设置 fallback 至多一个活动面；U3DS headless 注册与启动均不画且不转设置页。设置描述符延迟读取模块 Start 后等级/经验 provider，按当前 projection 只暴露下一等级，经验不足只保留维持。
- 换图/换槽：当前作用域变化清空等级镜像和被动代际资格；客机清除等级请求标记，下一 HostTick 按新作用域重请求；带旧作用域的升级/等级回执被拒绝。
- 隔离保持：技能 UI 不依赖 V7-03 调度器或事务服务；V7-03 被动周期、BII、总弹药 HUD、整理算法未改。
- 验证：`--bue-v7-04-skill-row-scope-red`、`--bue-v5-07-reload-skill-red`、`--bue-v7-03-passive-reload-red`、Release Rebuild 和 FULLSUITE 均通过；FULLSUITE 为 16 pass、1 个既有 Contracts Glazier known baseline、0 failed、firewall 0。
- 双轴：Round 2 使用全新 Standards 与 Spec 实例，最终均 `CLEAN`；仅遗留兼容双轨、provider 取数、命名和少量重复等不阻塞 judgment smell。
- 发布边界：本票不授候选、不改 `audit/RELEASES.md`、不授 CaseId、不生成正式交付包；真实 Glazier 几何与 SP/P2P/U3DS 玩家人工复核仍归 DEV-V7-07 发布门。