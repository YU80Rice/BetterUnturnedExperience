# DEV-V2-24：三环境实机验收 + RELEASES 加行 + 真机手册（终票）

Type: task
Status: claimed（2026-09-08，/implement 会话；前置 20/21/22/23 四票已核实 resolved；采集 kit + 手册就绪后用户就 D0 拍板 = **D0-b 修复轮**——BII 面板显示名改「更好的物品交互」，红测先行 + 全套 7/7 PASS 0 警告，新候选 `3cbd6268…9e4d`（前身 7d5dd3b5…c223 作废）；待实机采集 → 复核 → 双轴 → RELEASES/真机手册/关票）
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

## Comments

### 2026-09-08 认领 + 采集 kit/手册就绪（/implement 会话）

- **认领**：前置 20/21/22/23 四票核实 resolved；本票源码零修改（沿 DEV-V2-07 先例 = 验证与证据票）。候选 = DEV-V2-23 候选 `7d5dd3b5…c223`（533504B），本票审计轮两度 `-t:Rebuild` 字节一致（`audit/2026-09-08/DEV-V2-24/identity-rebuild1/2.log`）；采集 CaseId = `DEV-V2-24-20260908`，RELEASES 换标随实机验收。
- **采集 kit**：`audit/2026-09-08/DEV-V2-24/kit/out/` 六件——候选 BUE、独立 LMN、LmnEcosystemFixture（V1 旧插件替身）、NoOpFixture（T7-1 改名对照的第三方消费方）、**BueSameAsmProbe A/Z 双变体**（T7-4 双证仪器：AssemblyName=BetterUnturnedExperience、版本 0.0.0.0 与 BUE 相同，独立 GUID `io.github.yu80rice.aaprobe/zprobe.sameasm`——GUID 拓扑序分别排在 BUE 前/后，控制副本程序集进入 AppDomain 相对 BUE Awake 的时序；证据仪器非生产代码）。
- **证据包骨架**：`audit/2026-09-08/evidence/DEV-V2-24-20260908/`（candidate/cases×4/t7/coexistence，六份 case 模板 TODO 占位 fail-closed，沿 08 轻量链 + 07 结构）。
- **采集手册**：`audit/2026-09-08/DEV-V2-24/DEV-V2-24-three-env-acceptance-handbook.md`（十节：配置 A 裸 BUE 三环境四功能 + 防双装真机基线 / 配置 B V1 共存 / 配置 C T7 五项独立会话；全部锚行为源码现行串逐一核实）。
- **D0 采集前拍板点（待用户）**：BII 面板条目显示名为英文 `"Better Item Interaction"`（`ClientUiCompositionRoot.cs:126`），spec story 3 冻结四件中文名含「更好的物品交互」。处置二选一：(a) 接受现状→story 3/规格显示名措辞随结单修正登记；(b) 修复轮（改一处常量→红测+双轴→新候选→届时再采集）。未拍板不采集（中途换候选=全量重采）。
- **seam 缺口具名（沿 07 先例）**：本票无新生产 seam、无实现缺陷可红，红测面不变；kit 探针/fixture 的正确性判据 = 实机日志行为本身（证据仪器）。全套 7 测试运行器复跑归结单轮。

### 2026-09-08 D0 拍板 = D0-b 修复轮（用户拍板「B」）——BII 面板显示名中文化

- **判别内容**：spec story 3 与「功能身份与显示名」节冻结 BII 显示名 =「更好的物品交互」，面板实况为英文 `"Better Item Interaction"`（`ClientUiCompositionRoot.cs:126`，Legacy DEV-15B 期遗留）。
- **修复**：该一处显示名常量改「更好的物品交互」。REG-ACCEPT 启动日志标签 `Better Item Interaction featureId=…` 非面板显示名，不属显示名冻结面、维持原样（具名裁定，避免顺手扩大改动面）。
- **红测先行**：Plugin.Tests `AssertLitSingleplayerPath` 面板块新增断言（面板目录以官方中文名投射 BII 条目）→ **观测红**（FAIL 于新断言，`redtest-run.log`）→ 改串 → **绿**（`greentest-run.log` exit=0）。
- **全套门禁**：解决方案 Release 重建 0 错误 0 警告（`sln-rebuild-fullsuite.log`）+ 七运行器 7/7 exit=0（`fullsuite-run.log`）。
- **候选重授**：新候选 `3cbd62687bf765c618eaa5b6762c1172c7de022b6a50dc64ae1b0bdd0d399e4d`（533504B 两轮 Rebuild 字节一致，CaseId **`DEV-V2-24-CANDIDATE-20260908`**）；前身 `7d5dd3b5…c223`（DEV-V2-23-CANDIDATE-20260908）作废，kit/out 与归档、手册、六模板已换绑。采集 CaseId 不变 = `DEV-V2-24-20260908`。
- 增量 diff：`audit/2026-09-08/DEV-V2-24/round1-increment.diff`（双轴审查标的）。修复轮提交 = **`e70b7c4`**（候选来源快照 = e102935 + 本增量）。
- **双轴审查闭环（判词存档 `audit/2026-09-08/DEV-V2-24/review-rounds.md`）**：R1 Standards FINDINGS（2 SMELL——手册 S2/§10 与 sp/p2p-client 模板残留 D0 开放措辞；1 INFO——提交 hash 回填）/ Spec R1 CLEAN → F1-F3（aae2c99，仅文档）→ **R2 双轴全新实例双 CLEAN（Standards 0/0/0 / Spec 0/0/0）**。D0-b 修复轮审查面闭环；候选 `3cbd6268…9e4d` 为已审增量产物，可按手册开始实机采集。
