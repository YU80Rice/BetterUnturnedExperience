# DEV-V2-24：三环境实机验收 + RELEASES 加行 + 真机手册（终票）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-20（LHT）、DEV-V2-21（LIT 联机）、DEV-V2-22（LIR）、DEV-V2-23（防双装+文档）
Spec: `../spec.md`（Solution「到达标准」、Testing Decisions「实机验收面」两处）

## What to build

目的地四条在实机闭环：玩家只装一个 `BetterUnturnedExperience.dll`（无独立 LMN、无三插件 DLL）就能在单人 / SteamP2PFriends / U3DS 获得四个官方功能；BUE 成为玩家侧唯一推荐交付入口；RELEASES 出候选行；真机手册落盘；证据包绑定身份后授予 CaseId。

## Scope

- 裸 BUE 单 DLL 三环境验收：四官方功能（更好的物品交互 / 背包整理 / 更好的换弹体验 / 更好的尸潮播报）全部可用；面板显示四件官方中文名；三插件 = BueNetworkApi 生产绑定第一批真实消费者实证。
- 共存承诺：BUE + 独立 LMN 部署下未知 V1 旧插件继续收发（裸 BUE 无独立 LMN 除外，为已承认边界，随证据记录）。
- T7 实机五项清单：改名实机对照；Mono `LoadFile` 二次探测；同版本程序集最终谁保留；**不同 GUID + 同程序集名（红测 + 实机双证，红测不能替代实机结论）**；Preloader `AssemblyResolve`。
- RELEASES：候选行（绑 LoadSetIdentity，延续未公开分发 + 每票候选节奏）。
- 真机手册：玩家安装/升级注意事项（含从「BUE+LMN+三插件」旧部署的升级要点与独立 LMN 保留语义——旧 V1 插件共存仍需独立 LMN 在场）。
- 证据门禁：三环境证据包 + T7 实机记录统一绑定 LoadSetIdentity；双轴审查 CLEAN 后授予 CaseId 并关闭本票（output-review-loop 第 4 步）。

## 验收条件

- [ ] 三环境证据包齐全且绑定同一 LoadSetIdentity（裸 BUE 单 DLL）
- [ ] T7 五项实机记录落盘，其中「不同 GUID + 同程序集名」红测 + 实机双证
- [ ] V1 共存证据（BUE + 独立 LMN 部署下旧插件收发）
- [ ] RELEASES 候选行 + 真机手册落盘
- [ ] 双轴独立审查 CLEAN；CaseId 授予；地图/规格同步终态
