# V7-R5 原版技能存档作用域与 LIR 全局账

- **Ticket**: V7-R5
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T4 的事实输入。对照原版技能存档钥匙与现网 LIR 等级账产出现状报告（`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R5-vanilla-skill-scope.md`）。

必须回答（只查证不改码，结论带 file:line）：

1. 原版 `PlayerSkills` 存在哪份文件、路径如何随单人地图 / P2P 主机 / U3DS 变化；换 PEI → Washington 时为何技能重置（世界目录？关卡文件？服务器 ID？）。
2. 原版是否按角色分家（同一世界多个角色各一份 Skills.dat 还是一份）。
3. 现网 `ReloadSkillFilePersistence` 路径谁注入、文件名、键（steamId × characterKey）如何归一；为何跨地图仍满级。
4. 客机等级镜像（`ReloadSkillLevelMirror`）是否跨世界残留。
5. 若要对齐原版作用域，本仓已有哪些「按世界分家」的持久化先例（LHT？设置根？玩家目录？），不要发明第三条根目录。

一手来源优先：U3-SDK / 游戏 `Players/` 目录实勘 / 本仓 LIR 持久化代码。不要设计新账本格式，只报告现网与原版各用什么钥匙。

## Answer

全文：`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R5-vanilla-skill-scope.md`

1. **原版档** = `<游戏根>/<Worlds|Servers>/<serverID>/Players/<steam>_<characterID>/<Level.info.name>/Player/Skills.dat`（`PlayerSkills.cs:955-957,1024` + `PlayerSavedata.cs:19,91` + `ServerSavedata.cs:24-41`）。单人 `serverID=Singleplayer_<Characters.selected>`；独服 `Servers/<命令行 id>`（无参 `Default`）。PEI→Washington 重置是因为路径含 **地图名**，不是关卡文件、也不是单人槽变了。实勘：同一 `Singleplayer_0` 角色下 PEI 与 Washington 各有一份 `Skills.dat`。
2. **按角色分家**：每 `characterID`（byte 槽）一份，不是显示名，不是全角色共用。
3. **现网 LIR**：宿主注入设置根 `BetterUnturnedExperience/`，文件 `better-inplace-reload.skill-levels.dat`，行键 `steamId|characterName(trim)|level`，**无地图/无 serverID**。跨图仍满级是设计（第五阶段按玩家×角色全局账），不是读档 bug。
4. **镜像**：静态连接态；生产只在补丁注销/`Stop` 时 `Clear`；无换世界清。跨图满级根因是主机全局文件账，不是镜像落盘。
5. **分家先例**：不要第三根。设置根（LIR 已用）+ LIT 熔断按 `Provider.map`×`Characters.selected` 分文件；LHT 无进度档；不要写进原版 `Skills.dat`。
