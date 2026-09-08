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

### 2026-09-08 P2P 实机发现两起 LIT 真机缺陷（F-A/F-B）——采集停止，转修复轮

- **进展**：sp 单人 case 已通过并归档（8853cf8）；P2P 双端（本机 Host + 用户 VM Client）实测 **LIR/LHT 通过、LIT 两项 FAIL**（证据 `audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/p2p-{host,client}/`，含幽灵贴图截图）。
- **F-A（客机整理全程不可用）**：Host :2889 采纳客机会话（gen=2）即拍签发 challenge → :2890 定向发送 `result=LocalTransportUnavailable` 失败，**全程无重试**（恰此一条发送失败）；Client 侧 80 条「尚未收到有效服务端 session challenge」拒绝，RequestTidy 从未发出。反证：同会话 LIR 定向发送（Host :4610 `-> 客机 RepackSuccess`）与 LHT 组播（`广播 Update result=Sent`）均成功——失败是暂时性/状态性，一次重试即可恢复。候选 `3cbd6268…9e4d`。
- **F-B（主机本地整理幽灵贴图堆叠）**：Host :5134-:5137 本地路径提交成功（`placed=3 指纹守恒验证通过`），但 UI 多容器幽灵贴图堆叠（截图存档）；SP 同路径无此现象——本地提交后的界面刷新/预览清理路径 P2P 差异，根因待查（修复轮代码定位）。
- **处置（手册 §10 协议）**：停止采集、现场已保留（两端 LogOutput.log + 截图归档）；修复走 real-machine-test-loop：红测先行（LIT 挑战签发失败重臂缝 + F-B 根因）→ 双轴 CLEAN → 新候选 → **换绑后全量重采（含已通过的 sp/P2P LIR/LHT 项，不拼接）**。本票关票顺延。

### 2026-09-08 修复轮闭环（F-A + F-B2 双轴 R3 双 CLEAN）——候选 v3 重授，F-B1（幽灵）转压测定根因

- **F-A 已修**（`3db75c0`）：challenge 签发失败回滚采纳（DropSession 孤儿 token + liveSessions.Remove）→ 下一拍重发现重发，镜像异常路径 B3 语义；红测新组「挑战发送失败重臂」三断言（红=恰②③，`fixfa-red-run.log` → ALL GREEN 6 组）。
- **F-B2 已修**（`f89194f`+F1 `03b661f`+F1b `fe3e3cc`）：BII surface 隔离锁存去粘滞+可见化——根因=三条静默路径任一单帧瞬态失败即永久锁存 `isolated=true` 且零日志（第二次整理后强化渲染全灭）；`TransientIsolationGate` 去抖门（连续 60 帧持续失败才隔离，健康帧复位自愈）+ 首失败/恢复/锁存三态一次性诊断行（003 走 EmitDiagnosticOnce/Error，004 走 Runtime/Debug——F1 修正 D2 通道污染）；红测 `AssertTransientIsolationGate`（编译红→绿）。
- **F-B1（幽灵覆盖层）未在本轮修复**：诊断数据排除数据层复制（插桩计数整理前后每页一致），幽灵=BII 陈旧覆盖层，与触发时序相关（原始会话撞上关开过渡窗口+ESC 暂停）；转新候选压测定根因。
- **审查链**（判词存档 `audit/2026-09-08/DEV-V2-24/review-rounds.md`）：R2 Standards CLEAN（2S 均具名，S1 采纳/S2 具名延期）+ Spec NOT CLEAN（2D：锁存行双发/004 行被 003 sink 污染）→ F1+F1b → **R3 双轴全新实例双 CLEAN**（Standards 0B（唯一 SMELL 已在 F1b 修复）/ Spec 0/0/0；Spec R3 首派发基础设施零输出作废留痕，判词来自重派新实例）。
- **候选 v3 重授**：`c9b6b6e40684df04b6141bef1f220a84d49dd96dfa7fae228ff51696e526eb86`（535552B **三轮** Rebuild 一致，CaseId 仍 `DEV-V2-24-CANDIDATE-20260908`）；前身 7d5dd3b5…c223 与 3cbd6268…9e4d 作废；kit/out、归档、identity-sha256.txt v3、手册 §1、六模板换绑；历史 case（sp/p2p×2）加「已归档待重采」横幅。
- **下一步**：新候选部署（含 VM）→ F-B1 幽灵压测（提复现率：多次关开+整理+ESC 暂停时序）→ 全量重采（手册十节，绑 v3 哈希）。
