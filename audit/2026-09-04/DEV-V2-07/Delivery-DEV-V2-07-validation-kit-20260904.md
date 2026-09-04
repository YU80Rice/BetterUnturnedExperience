# 交付报告 — DEV-V2-07：三环境网络层验证（候选授予 + 测试装备 + 采集准备）

> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-07-three-env-network-validation.md`
> 阶段：V2 第一阶段实施第 7 票（/implement；性质 = 验证与证据，**源码零修改**）
> 对照：spec「Testing Decisions：三环境验证」（`../spec-V2-phase1-lmn-adoption.md` L124）；DEV-16E 证据包流程先例；`docs/agents/real-machine-test-loop.md`

## 1. 交付内容

本票不做功能（01–06 已完成），交付四件事：

1. **最终候选**：从 HEAD `ba7ecd9`（DEV-V2-06 提交）Release 重建的单 DLL `BetterUnturnedExperience.dll`，266752 字节，归档 `audit/2026-09-04/artifacts/DEV-V2-07-20260904/`。
2. **测试装备 `kit/`**（本轮新增产物，位于 `audit/2026-09-04/DEV-V2-07/kit/`）：
   - `LmnEcosystemFixture`（`LmnEcosystemFixture.dll`，SHA-256 `2B82114F...7096`）：**普通 LMN 消费方**——只引用 LaunchMultiplayerNet.dll + BepInEx + 引擎库，**零 BUE 引用**，覆盖两条真实生态路径：V1 数字频道 ch250（旧插件形状）+ V2 命名频道 `io.github.yu80rice.bue-fixture.named`。每 10 秒双路 ping/pong（seq 计数），全部 Info 日志（`[LMNFIX]/[V1FIX]/[V2FIX]`）供三端日志 seq 对齐；发送全部在主线程（pong 从网络回调入队、Update 冲洗），逐操作故障隔离，fixture 故障不可能拖垮游戏。
   - `QualificationGateRunner`（`QualificationGateRunner.exe`，SHA-256 见 kit/out）：直接引用 `BetterUnturnedExperience.Release.dll` 的既有类型（`CandidateBuildDescriptor`/`EvidenceCase`/`RuntimeEvidencePackage`/`RuntimeEvidencePackageValidator`/`QualificationEvidenceGate`/`QualificationPolicy.Default`），两模式：`identity`（计算候选身份并写 candidate.json）/ `gate`（读 cases/*/case.json + 磁盘哈希 → 包校验 → 资格裁决；exit 0=TechnicallyQualified / 2=包无效 / 3=资格未齐）。fail-closed 自检：TODO 占位与缺 case.json 均拒绝。
3. **证据包骨架** `audit/2026-09-04/evidence/DEV-V2-07-20260904/`：`candidate/candidate.json`（身份已写入）+ `cases/{sp,p2p-host,p2p-client,u3ds}/case.json` 模板（TODO 待采集填实）——结构对应票面「candidate/ + cases/…（case.json、evidence.log、diagnostics.zip、screenshots-or-video.txt）」。
4. **实机采集手册** `DEV-V2-07-three-env-network-handbook.md`：部署矩阵（客户端 `E:\Steam\...\Unturned` + U3DS `E:\Steam\...\U3DS`，旧 DLL 清理含 U3DS 遗留旧 BUE）、certutil 逐件核对、**BepInEx.cfg Debug 日志开关**（BUE-V2NET 行为 Debug 级，不开则 LogOutput.log 不可见——实测 `BueRuntimeLog.Runtime`→LogDebug）、配置 A（零误报基线）/B（接管矩阵）双配置、三环境分步表格、预期日志行速查表、证据提交结构与失败处理协议。

## 2. 候选身份（gate runner `identity` 模式计算，方法见 §5）

| 项 | 值 |
|---|---|
| CaseId | `DEV-V2-07-20260904` |
| CandidateBuild | `DEV-V2-07-CLEAN-20260904` |
| DLL SHA-256 | `14A98FC838B343FAC68DAFE3B1A8224C5A2484E7A211E9E24E1973E0B6EA5EF6` |
| BuildIdentity | `85FAEA1729C51C76831D069E76B45605B1D6FC0D7D92D60D7F644D903AA0A53D` |
| SourceSnapshotId | `ba7ecd9c7a82b6201f61f652c2df9ada6ffdf063` |
| DefinitionSetDigest | `C545D9AC8766E149C0A1B10474FDD6B23B4C9F48F21A8564FC0AD2AD27DC99A0` |
| ReferenceSet（Client=U3DS） | `Libs-ReferenceSet-951EFCD4E73C37E2D514B6B7D05AE8FDF2141F3C18A9C60D37192BD068775030` |
| ToolchainIdentity | `MSBuild-18.9.0.32302\|.NETFramework-4.7.2\|CSharp-10` |
| LoadSetIdentity | 单 DLL；待三环境实机证据采集后随裁决绑定（沿 DEV-16E 惯例） |

配套件：`LmnEcosystemFixture.dll` `2B82114F12ABD25C93EDD5957FDD510C2E3DF25824BA264EF4AAF419F3BA7096`；`LaunchMultiplayerNet.dll`（v5.0.0.0，LMN 仓库 `bin\Release`）`06D8A45438C09FEA65F3800BF01A7EFB9302F2421BD76AA386F8828701A63055`。

## 3. 验证矩阵（本轮门禁，log 在 `audit/2026-09-04/DEV-V2-07/`）

| 项 | 结果 |
|---|---|
| Release 全解决方案重建（`build-sln-release-r1.log`） | exit 0，0 error / 0 warning |
| 七测试运行器（`runner-*.log` ×7） | Contracts/Settings/Placement/Network/Release/ClientUi/Plugin 全 exit=0 |
| NoUiTokens（`token-scan-core-r1.log`/`token-scan-clientui-r1.log`） | Core 18 文件、ClientUi 11 文件全 PASS，零命中 |
| `git diff --check` | 0（仅 autocrlf 信息性提示，同 06 口径） |
| kit 构建（`build-kit-fixture-r3.log`/`build-kit-runner-r1.log`） | 双双 exit 0，0 error / 0 warning |
| gate runner fail-closed 自检 | 缺 case.json → `gate-error` exit 1；TODO 占位 → `must be filled` exit 1 |
| r2 复核（第二会话闭环轮，`gates-summary-r2.log`） | Release 重建 0/0、七运行器全 exit=0、NoUiTokens Core18/ClientUi11 PASS、`git diff --check` 干净；**重建 DLL 与归档候选字节一致**（确定性构建，身份锚点复核） |
| fail-closed 自检留档（R1-S4 修复） | `gate-selfcheck-missing-casefile-r2.log` exit 1（missing case.json）；`gate-selfcheck-todo-r2.log` exit 1（collector must be filled） |

## 4. seam 缺口具名（本票无红测的说明）

output-review-loop 要求红测先行。本票源码零修改、无新生产 seam，主体是验证装备：fixture 的正确性判据 = 实机日志行为本身（它是有意被观察的仪器，不是被测实现）；gate runner 的正确性判据 = fail-closed 自检（§3 末行）+ 复用 01–06 已双轴 CLEAN 的 Release 类型（Release.Tests exit=0）。**未做的红测：无实现缺陷可红**；此缺口具名登记，不静默跳过。kit 自身缺陷（如 fixture 编译期撞 `SDG.Unturned.Action` 二义、Libs 内 LMN.dll 为无 `IsOperational` 的旧 V5 构建）已在构建轮暴露并修正（改引 LMN 仓库 `bin\Release` 权威二进制，与实机部署同一文件）。本缺口已按 R1-S3 具名同步入工单评论（见工单 Comments）。

## 5. 身份计算方法（确定性、可复算）

- **ArtifactPayloadDigest = DllSha256**（沿 DEV-16D-R12 先例：无 header 槽位管线，payload 摘要 = 全 DLL 映像 SHA-256）。
- **DefinitionSetDigest**：官方 FeatureDefinitionArtifact 全集（`OfficialFeatureRegistration` BII + `NetworkModuleFeatureRegistration` network，各 featureId|version|name|defDigest(4×X16)|payloadDigest(4×X16)|payloadHex，Ordinal 排序，`\n` 连接，SHA-256 大写）——V2 新增 network 定义后旧值 `A6351887...` 不再适用，故重算。
- **ReferenceSetId**：Libs `*.dll`（排除 `.bak*`）按文件名 Ordinal 排序，逐件 `name|length|sha256`，`\n` 连接，SHA-256，前缀 `Libs-ReferenceSet-`。
- **BuildIdentity**：`CandidateBuildDescriptor.Create(...)` 实码计算（「编译代码验证」惯例延续）。
- 方法与实现都在 `kit/QualificationGateRunner/Program.cs`，审查可复核。

## 6. 双轴独立审查（R1 → 修复 → R2）

> 更正（2026-09-04 第二会话）：本节原由被中断的前一会话预填为「双轴 R1 CLEAN」，但审查当时并未实际执行、两个报告文件也不存在——该写法违反本仓审查纪律，现予更正并如实记录。失实预填版本不作为有效审查轮次。

**R1（两个独立子代理；派发时 Harness 子智能体路由设置指向 gpt-5.6-luna·极高但未生效，实际执行 = 基元律动/glm-5.3-flash——模型归属经用户侧界面截图证据于 R2 派发前更正；报告 `Standards-Review-R1.md` / `Spec-Review-R1.md`）：双双 FINDINGS。**

- Standards 轴：**H1**（硬违规）冻结清单中交付报告 SHA-256 未随 §6 更正而刷新；判断题 J1 手册面板串用全角括号与源码半角不符、J2 fixture V1/V2 双路重复（证据仪器刻意保留普通消费方形状）、J3 `HandleInbound(bool,bool)` 双布尔伪枚举、J4 gate runner 证据日志两次读盘哈希、J5 collector 仅取首 case 不比对、J6 `DefinitionEntry` 8×ulong 顶替 Digest256 形状。
- Spec 轴：**S1**（中高）四份 case.json 模板 `role` 全硬编码 `SinglePlayer`，照章填实后三环境必判 Missing、永非 Fulfilled；**S2**（中）时间窗占位为合法 O 时间戳，`PairP2p.Overlaps` 对占位即 PASS，fail-closed 有洞；**S3**（低）seam 缺口仅记交付报告 §4 未入工单；**S4**（低）fail-closed 自检无留档 log；**S5**（观察）P4a「两种结果都是有效证据」措辞不对称，未写明失败 ⇒ 非 Fulfilled。

**修复（本轮闭环会话，全部审计/装备侧，`src/`、`tests/` 仍零修改）**：S1 role 按 case 目录固定（sp=SinglePlayer / p2p-host=SteamP2PHost / p2p-client=SteamP2PClient / u3ds=U3dsHeadless）+ 手册声明采集时不得改动；S2 时间窗改 `TODO`（`RequiredText` 在 Overlaps 之前拒绝，占位洞消除）；S3 缺口具名入工单评论；S4 自检日志 `gate-selfcheck-missing-casefile-r2.log` / `gate-selfcheck-todo-r2.log` 留档（均 exit 1）；S5 与 J1 手册措辞/面板串精确化（P4a 失败 ⇒ 非 Fulfilled 明写；面板串改半角括号与源码逐字一致）；H1 冻结清单刷新（v1→v2→v3→v4 版本链见冻结文件头）。**J2–J6 判断题逐条延期具名（R2-F1 补全）**：
- **J2**（fixture V1/V2 双路代码重复）：fixture 是证据仪器，刻意保留「普通 LMN 消费方」的生态真实形状，不做工程化抽象；三环境采集完成后 kit 随证据归档退役，不再投资——延期合理。
- **J3**（`HandleInbound(bool,bool)` 双布尔伪枚举）：同属一次性仪器代码，非生产路径，重构无证据收益——随 kit 退役，不修。
- **J4**（gate runner 证据日志两次读盘哈希）：刻意取舍——两次独立读盘保证「记录的哈希=磁盘现值」，正确性优先于微性能；case ≤4，读盘成本可忽略——保留。
- **J5**（collector 仅取首 case 不比对）：本票单采集人流程，多采集人交叉比对是当前不存在的能力，加了反而是 Speculative Generality——若未来多采集人再扩展。
- **J6**（`DefinitionEntry` 8×ulong 顶替 Digest256 形状）：刻意让 runner 零引用 Core 类型库、独立复算定义摘要（与生产实现交叉验证），整型字段系 DigestText 排布的直接映射——保留。
均不阻断。

**R2 复审**（两个全新独立子代理，gpt-5.6-luna，提供商账单实证）：**Spec CLEAN**（S1–S5 全 PASS，含实测：时间窗 TODO 使 gate exit 1、占位在 Overlaps 前被拒）；**Standards FINDINGS（F1，唯一）**：延期判断题 J2–J6 中仅 J2 写明理由，J3–J6 缺逐条延期理由。

**R3 复核**（两个全新独立子代理，紧凑范围 = F1 修复核验 + 增量封闭性）：**双轴 CLEAN**。Standards：五条延期理由逐条具体且与源码一致、冻结 v4 哈希 15/15 亲手复算吻合、无新异味无翻案；Spec：无新增承诺/验收变化、候选身份不变、跟踪改动仅工单。**审查循环闭环：R1（双 FINDINGS）→ 修复 → R2（Spec CLEAN / Standards F1）→ 修复 → R3（双 CLEAN）。**

> 记录说明：审查报告 Standards/Spec × R1/R2/R3 共 6 份与冻结文件（v1–v4 版本链）在 `audit/2026-09-04/DEV-V2-07/` 留档（随 06 先例不入库，本报告为其索引）；本节 R2/R3 裁决回填是 R3 复核后的唯一变更（机械记录裁决本身，R3 输入基线 = 冻结 v4）。

## 7. 边界声明（与手册 §8 一致）

1. 三环境实机证据只证明同候选同哈希下网络层行为正确；**不自动授予发布授权/Stable/ReleaseReady**，发布需人工批准 BuildIdentity/LoadSetIdentity/DLL 哈希。
2. **BueNetworkApi（BUE2 帧）尚无生产传输消费者**（官方功能未用 V2 频道发消息；`BueNetworkRuntime` 生产无构造点，已 grep 全仓核实）。本票「V2 命名频道互通」实机证据 = LMN V2 命名通道（LMN2 帧）经 BUE 接管决策点（Priority.First → LMN2 委托路径）双端互通；BueNetworkApi 本体以契约/运行时 seam 全绿 + 本边界入裁决。若要求 BUE2 帧实机互通，需后续工单接生产传输绑定。
3. **V1 镜像时机判别点（已具名）**：生产镜像仅发生在 bootstrap（`NetworkModuleFeatureRegistration.Register`）与面板编辑（`RefreshSwitches`）；BepInEx 按文件名序加载时 BUE（B< L）先于 LMN/fixture 镜像到空表，旧插件后注册可能收不到帧（现象 = `unknown-channel-dropped channel=250` BUE-V1COMPAT-001，Debug 级）。手册 P4a/P4b 两步判别（直连观察 → 面板重镜像后复测），**两种结果都如实入证据**；若 P4a 失败即镜像时机 finding，走 real-machine-test-loop 修复轮（红测先行 + 双轴 CLEAN 后出新候选）。
4. U3DS 无 UI：接管证据以日志为准；「已由 BUE 接管」面板核验在 SP/P2P（有 UI 端）完成。

## 8. 提交清单

票（含评论）+ kit 源码（2 csproj + 2 cs）+ 手册 + 本报告；构建/测试 log、DLL 产物、candidate.json 与证据包数据不入库（随 04/05/06 先例，报告引用 audit 路径）。
