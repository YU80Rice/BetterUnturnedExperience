# DEV-V2-07：三环境网络层验证与证据包

Type: task
Status: claimed（2026-09-04，agent；双轴审查已闭环 CLEAN，候选/kit/证据骨架/手册齐备——待人工实机采集 → 资格裁决 → 人工批准）
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-03-buenetworkapi-runtime, DEV-V2-04-lmn-takeover, DEV-V2-05-v1-compat-layer, DEV-V2-06-config-migration-network-settings
Spec: `../spec-V2-phase1-lmn-adoption.md`（Testing Decisions「三环境验证」）

## Scope

对 V2 第一阶段最终 DLL 完成单人、SteamP2PFriends Host/Client、U3DS Headless 三环境网络层验证，绑定 LoadSetIdentity 证据包：

- 同一 CandidateBuild / DLL SHA-256 / LoadSetIdentity（含网络模块）绑定全部环境证据；不用单一 DLL 哈希拼接证据。
- 单人：本地网络模块启动、频道注册/发送/接收正常。
- SteamP2PFriends Host/Client：V2 命名频道互通；V1 兼容路径（旧 no-op 插件）两端收发；同一 CaseId、时间窗正交重叠。
- U3DS Headless：不实例化任何 UI/ClientUi；网络模块启动正常；本地功能（BII）不受网络故障影响。
- 证据包结构沿用 DEV-15E/16E：candidate/ + cases/{sp,p2p-host,p2p-client,u3ds}/（case.json、evidence.log、diagnostics.zip、screenshots-or-video.txt）。
- 若 U3DS 上独立 LMN 存在，验证接管生效（面板「已由 BUE 接管」）；无 LMN 时验证零误报。

## 验收条件

- [ ] 三环境证据齐全且绑定同一 LoadSetIdentity；人工验收（real-machine-test-loop）。
- [ ] V2 第一阶段完成标准 8 条全满足（CONTEXT「LMN 纳入完成标准」L93-95）。
- [ ] 证据包通过资格裁决（Fulfilled）；发布授权仍需人工批准。

## 不做

- 不实现功能（功能已在 DEV-V2-01~06 完成）；不修改源码。

## Comments

> 2026-09-04 认领（agent）：阻塞票 01–06 全部 resolved。静态门禁全绿：Release 重建 0 error/0 warning（`audit/2026-09-04/DEV-V2-07/build-sln-release-r1.log`）、七测试运行器 exit=0、NoUiTokens（Core 18 文件 + ClientUi 11 文件）PASS、`git diff --check` 0。候选授予：CaseId `DEV-V2-07-20260904`、CandidateBuild `DEV-V2-07-CLEAN-20260904`、DLL SHA-256 `14A98FC838B343FAC68DAFE3B1A8224C5A2484E7A211E9E24E1973E0B6EA5EF6`、BuildIdentity `85FAEA17...A53D`、SourceSnapshotId `ba7ecd9`（源码零修改，符合票面「不做」）。测试装备：`LmnEcosystemFixture`（普通 LMN 消费方，V1 ch250 + V2 named 双路，不引用 BUE）+ `QualificationGateRunner`（identity/gate 两模式，接 Release 既有 CandidateBuildDescriptor/RuntimeEvidencePackageValidator/QualificationEvidenceGate）。证据包骨架 `audit/2026-09-04/evidence/DEV-V2-07-20260904/`（candidate/candidate.json + cases/×4 模板）。实机采集手册 `audit/2026-09-04/DEV-V2-07/DEV-V2-07-three-env-network-handbook.md`。**待人工实机采集 → 门禁裁决 → 人工批准**。已具名边界：BueNetworkApi（BUE2 帧）尚无生产传输消费者，「V2 命名频道互通」实机证据 = LMN V2 named 通道经 BUE 接管决策点（LMN2 委托）；BueNetworkApi 本体以 seam 全绿 + 边界声明入裁决。已具名判别点：V1 镜像时机（bootstrap 镜像 vs 旧插件 Awake 晚注册），手册 P4a/P4b 两步判别，任一结果都是有效证据；失败走 real-machine-test-loop 修复轮。

> 2026-09-04 双轴 R1（第二闭环会话，审查子代理实际执行 = 基元律动/glm-5.3-flash；派发时路由设置指向 gpt-5.6-luna 未生效，模型归属已据用户截图证据更正）：双双 FINDINGS。Standards H1（冻结清单交付报告哈希未刷新，硬违规）+ J1–J6 判断题；Spec S1（case 模板 role 全硬编码 SinglePlayer，照章填实必判 Missing）/S2（时间窗合法占位可满足 PairP2p 重叠门禁）必修，S3/S4/S5 低危。**S3 修复即具名入票：本票无红测的 seam 缺口 = 交付报告 §4（源码零修改无新生产 seam；fixture 判据=实机日志行为本身；gate runner 判据=fail-closed 自检 + 复用 01–06 已双轴 CLEAN 的 Release 类型）**。全部必修已落：S1 role 按 case 固定、S2 时间窗改 TODO（RequiredText 前置拒绝）、S4 自检日志留档、S5/J1 手册精确化、H1 冻结清单刷新 v2；J2–J6 判断题延期具名（交付报告 §6）。R2 复审（两个全新独立子代理）裁决待回填。

> 2026-09-04 双轴闭环：R1（双 FINDINGS）→ 修复 → R2（Spec CLEAN / Standards F1：J3–J6 延期理由未逐条写明）→ 修复（五条逐条理由入交付报告 §6）→ **R3 双轴 CLEAN**（F1 理由与源码一致、冻结 v4 哈希 15/15、无新异味、无新增承诺、身份不变）。审查循环按 output-review-loop 闭环 CLEAN。审查执行：R1 = glm-5.3-flash（归属已更正），R2/R3 = gpt-5.6-luna（探针+提供商账单双实证）。agent 侧交付完成：候选 + kit + 证据骨架 + 手册 + 双轴闭环 + 静态门禁 r2 全绿（重建 DLL 与候选字节一致）。**下一站 = 人工实机采集**（手册 `audit/2026-09-04/DEV-V2-07/DEV-V2-07-three-env-network-handbook.md`）→ QualificationGateRunner 资格裁决 → 人工批准发布。候选身份不变：DLL SHA-256 `14A98FC8…E5EF6`、BuildIdentity `85FAEA17…A53D`。
