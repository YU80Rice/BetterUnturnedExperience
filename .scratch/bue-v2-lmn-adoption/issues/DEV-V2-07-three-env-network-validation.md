# DEV-V2-07：三环境网络层验证与证据包

Type: task
Status: resolved（2026-09-05，agent；新候选 DEV-V2-11-CLEAN-20260904 四角色资格门禁 TechnicallyQualified（gate exit 0），8 条完成标准全满足；发布授权按票面定义为独立人工批准，见 Comments 尾条）
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

- [x] 三环境证据齐全且绑定同一 LoadSetIdentity；人工验收（real-machine-test-loop）——配置 A/B + 10/11 两轮复测 + SP 复测均由人工实机采集，证据归档 `audit/2026-09-04/evidence/DEV-V2-{07,10,11}-20260904/`
- [x] V2 第一阶段完成标准 8 条全满足（CONTEXT「LMN 纳入完成标准」L93-95）——逐条对照见 Comments 尾条
- [x] 证据包通过资格裁决（Fulfilled）——gate exit=0 TechnicallyQualified（`audit/2026-09-04/DEV-V2-11/gate-final-r1.log`）；发布授权仍需人工批准（独立动作，见尾条④）

## 不做

- 不实现功能（功能已在 DEV-V2-01~06 完成）；不修改源码。

## Comments

> 2026-09-04 认领（agent）：阻塞票 01–06 全部 resolved。静态门禁全绿：Release 重建 0 error/0 warning（`audit/2026-09-04/DEV-V2-07/build-sln-release-r1.log`）、七测试运行器 exit=0、NoUiTokens（Core 18 文件 + ClientUi 11 文件）PASS、`git diff --check` 0。候选授予：CaseId `DEV-V2-07-20260904`、CandidateBuild `DEV-V2-07-CLEAN-20260904`、DLL SHA-256 `14A98FC838B343FAC68DAFE3B1A8224C5A2484E7A211E9E24E1973E0B6EA5EF6`、BuildIdentity `85FAEA17...A53D`、SourceSnapshotId `ba7ecd9`（源码零修改，符合票面「不做」）。测试装备：`LmnEcosystemFixture`（普通 LMN 消费方，V1 ch250 + V2 named 双路，不引用 BUE）+ `QualificationGateRunner`（identity/gate 两模式，接 Release 既有 CandidateBuildDescriptor/RuntimeEvidencePackageValidator/QualificationEvidenceGate）。证据包骨架 `audit/2026-09-04/evidence/DEV-V2-07-20260904/`（candidate/candidate.json + cases/×4 模板）。实机采集手册 `audit/2026-09-04/DEV-V2-07/DEV-V2-07-three-env-network-handbook.md`。**待人工实机采集 → 门禁裁决 → 人工批准**。已具名边界：BueNetworkApi（BUE2 帧）尚无生产传输消费者，「V2 命名频道互通」实机证据 = LMN V2 named 通道经 BUE 接管决策点（LMN2 委托）；BueNetworkApi 本体以 seam 全绿 + 边界声明入裁决。已具名判别点：V1 镜像时机（bootstrap 镜像 vs 旧插件 Awake 晚注册），手册 P4a/P4b 两步判别，任一结果都是有效证据；失败走 real-machine-test-loop 修复轮。

> 2026-09-04 双轴 R1（第二闭环会话，审查子代理实际执行 = 基元律动/glm-5.3-flash；派发时路由设置指向 gpt-5.6-luna 未生效，模型归属已据用户截图证据更正）：双双 FINDINGS。Standards H1（冻结清单交付报告哈希未刷新，硬违规）+ J1–J6 判断题；Spec S1（case 模板 role 全硬编码 SinglePlayer，照章填实必判 Missing）/S2（时间窗合法占位可满足 PairP2p 重叠门禁）必修，S3/S4/S5 低危。**S3 修复即具名入票：本票无红测的 seam 缺口 = 交付报告 §4（源码零修改无新生产 seam；fixture 判据=实机日志行为本身；gate runner 判据=fail-closed 自检 + 复用 01–06 已双轴 CLEAN 的 Release 类型）**。全部必修已落：S1 role 按 case 固定、S2 时间窗改 TODO（RequiredText 前置拒绝）、S4 自检日志留档、S5/J1 手册精确化、H1 冻结清单刷新 v2；J2–J6 判断题延期具名（交付报告 §6）。R2 复审（两个全新独立子代理）裁决待回填。

> 2026-09-04 双轴闭环：R1（双 FINDINGS）→ 修复 → R2（Spec CLEAN / Standards F1：J3–J6 延期理由未逐条写明）→ 修复（五条逐条理由入交付报告 §6）→ **R3 双轴 CLEAN**（F1 理由与源码一致、冻结 v4 哈希 15/15、无新异味、无新增承诺、身份不变）。审查循环按 output-review-loop 闭环 CLEAN。审查执行：R1 = glm-5.3-flash（归属已更正），R2/R3 = gpt-5.6-luna（探针+提供商账单双实证）。agent 侧交付完成：候选 + kit + 证据骨架 + 手册 + 双轴闭环 + 静态门禁 r2 全绿（重建 DLL 与候选字节一致）。**下一站 = 人工实机采集**（手册 `audit/2026-09-04/DEV-V2-07/DEV-V2-07-three-env-network-handbook.md`）→ QualificationGateRunner 资格裁决 → 人工批准发布。候选身份不变：DLL SHA-256 `14A98FC8…E5EF6`、BuildIdentity `85FAEA17…A53D`。

> 2026-09-05 转交复核（本会话）：**配置 A/B 实机复核完成，触发修复轮并已闭环**。配置 B 功能面全通但 P5 零误报未达成（F-A）+ 面板网络条目缺失（F-B）→ 修复票 DEV-V2-10（resolved，`e8c3a52`+`5c1cda5`）→ 实机复测揪出 F-C 类型名根因（`.Routing` 多写一级，镜像与 LMN2 委托此前从未生效；勘误：BepInEx 预载全部程序集，「先载/晚到」时机窗口不存在，07 复核的「时机」归因不成立）→ DEV-V2-11（resolved，`6907a1a`+`c468858`）→ **二轮实机复测全绿**（四端指纹一致、零「BUE 错误」行、`result=mirrored channels=2` 与 `result=delegated` 正向锚、`unknown-channel-dropped` 归零、B5/B6 `installed→removed→installed` 行为链、V1/V2 双向 seq 对齐；`audit/2026-09-04/DEV-V2-11/configB-retest-verification-r1.md`）。DEV-V2-12（V1 ping 双到达）立案延后非阻塞。本会话转交复核逐项通过：提交链 `e8c3a52→…→0bf15b2`、新候选身份逐字核对（`5B4E948E…`/`01BFF640…`/`38D66989…`，DLL 268800 字节）、kit 三定义重建、二轮全绿抽查（host/U3DS-client LogOutput 零错误零 dropped）。**本票候选就此切换：`DEV-V2-11-CLEAN-20260904`（`5B4E948E…`，SourceSnapshotId `6907a1a`）；旧候选 `14A98FC8…` 归档保留不再使用。证据骨架 `evidence/DEV-V2-11-20260904/` 已搭（candidate 绑定 + p2p-host/p2p-client/u3ds 三场 evidence.log 就位）。关闭前仅剩：① 新候选下单人（SP）冒烟采集（二轮复测未覆盖该角色，gate 四角色必需）；② 四份 case.json 填实；③ gate exit 0（Fulfilled）→ 本票方可转 resolved（发布授权仍为独立人工批准）。**

> 2026-09-05 关单（本会话）：**资格门禁 TechnicallyQualified，本票 resolved**。
>
> **① SP 复测验收**（用户三阶段操作：接管→恢复独立 LMN→重新接管）：部署指纹 `5B4E948E…`（assembly-identity 实锚）、`installed(L146)→mirrored deferred=true(L192)→removed hand-back(L1578)→mirrored channels=2(L1740)+installed(L1741)→退出清理(L1862-3)` 全链、BUE 错误=0、`unknown-channel-dropped`=0、fixture V1/V2 各 19 拍；证据入 `evidence/DEV-V2-11-20260904/cases/sp/`。
>
> **② 资格裁决**：四份 case.json 填实（UTC 窗口以 UMM 受管会话锚为准；诊断包系副本、起点为具名保守下界）。**gate exit=0**：`validation isValid=True codes=Valid`、`status=TechnicallyQualified`、四角色全 Fulfilled + U3dsClientUi NotApplicable、包 canonicalDigest `F05F7CC3…`、绑定 BuildIdentity `01BFF640…` / DLL `5B4E948E…`。log：`audit/2026-09-04/DEV-V2-11/gate-final-r1.log`。caseId 布局：p2p 对共用主 CaseId（PairP2p 要求），SP/U3DS 独立尾缀（校验器每 CaseId ≤2 场规则，修复窗口探针实证）。
>
> **③ 8 条完成标准逐条**：BueNetworkApi 内置✓（seam 全绿+无生产绑定边界已声明入裁决）；V2 命名频道可用✓（二轮复测双向 seq 对齐+delegated 锚）；V1 兼容可运行✓（四端双向+mirrored channels=2+v1compat 官方注册件）；共存接管生效✓（installed 锚+接管卡可达+B5/B6 可逆链）；配置迁移完成✓（no-op mapping=empty，LMN 无配置系统即空迁移完成）；单人✓（SP Fulfilled）；U3DS✓（Headless Fulfilled+完整日志）；SteamP2PFriends✓（Host/Client Fulfilled+正交重叠）；故障隔离下本地功能可用✓（隔离线全程未拖垮游戏、BII 三环境正常）。
>
> **④ 发布授权（独立人工批准，不挡关单）**：批准对象=BuildIdentity `01BFF64000C29FC1FEA8B2C13C8A4EC19EFD0A1A55D8743E512B8D6797FC86B5`、DLL sha256 `5B4E948E…C5BCD`（268800 字节）及其 LoadSetIdentity 绑定。
>
> **⑤ kit 工具修复（审计工具，非生产源码）**：老版 Newtonsoft `DateParseHandling.Auto` 在 JObject.Parse 时把 ISO 时间戳魔转为文化格式串（`'09/04/2026 16:25:00'`），`ParseExact("O")` 必炸——潜伏自本票建票以来（此前 case.json 均为 TODO 未触达该路径）。修复=`JsonConvert.DeserializeObject<JObject>(…, DateParseHandling.None)`+ParseUtc 值内嵌报错。红=本日观察到的 FormatException，绿=gate exit 0。同轮修复 case.json 转义（JSON 反斜杠）与 caseId 布局。
>
> **⑥ 遗留具名（均不阻塞）**：DEV-V2-12（sender=0 双到达，open-deferred）；DEV-V2-09（主菜单间距，needs-triage）；「BUE V1 兼容层」条目无单独截图（可选补）；V1 镜像「时机」归因已由 10/11 勘误（类型名为唯一根因）。

> 2026-09-05 人工验收授权（用户原话：「ok，我正式授权DEV-V2-07工单关闭，人工验收通过」）：用户正式确认人工验收通过并授权本票关闭。本票至此完全闭环：agent 侧装备/审查/证据链 + 人工实机采集 + 修复轮（10/11）+ 资格门禁 TechnicallyQualified + 人工验收授权，全链留档。**注意：本授权针对工单验收关闭；发布批准（BuildIdentity `01BFF640…` / DLL `5B4E948E…` 的对外发布）仍为独立人工动作，本评论不构成发布批准。**
