轴: Spec｜轮: R1｜审查员: 独立子代理(实际执行=基元律动/glm-5.3-flash；原标 gpt-5.6-luna 系路由未生效误标，R2 派发前经用户截图证据更正)

# 一、Bash 独立核验（通过）

- `git rev-parse HEAD` = `ba7ecd9c7a82b6201f61f652c2df9ada6ffdf063` = 工单声称 SourceSnapshotId。
- `certutil` 候选 DLL = `14A98FC8…E5EF6`，与工单/candidate.json 一致；`git status --porcelain` 无任何 `src/`、`tests/` 修改行（仅 issue 文件 M 与既有 untracked）——「不做：不修改源码」成立。
- kit 哈希：`LmnEcosystemFixture.dll` `2B82114F…7096`、LMN `bin\Release` `06D8A454…3055` 与手册一致；fixture 仅引 LMN/BepInEx/引擎库，零 BUE 引用，覆盖 V1 ch250 + V2 named 双路——符合票面「V2 命名频道互通；V1 兼容路径（旧 no-op 插件）两端收发」。
- gate runner 确接既有 Release 类型：`CandidateBuildDescriptor.Create`/`EvidenceCase.Create`/`RuntimeEvidencePackage.Create`/`QualificationEvidenceGate.Evaluate`/`QualificationPolicy.Default` 签名逐一比对匹配（`src/BetterUnturnedExperience.Release/`）。硬编码 `DefinitionEntry` 常量与 `OfficialFeatureRegistration.cs`/`NetworkModuleFeatureRegistration.cs` 逐值一致（含 D=(1,0,0,14)/(1,0,0,16)、payload "BUE-BII-V1"/"BUE-NET-V1"）。
- 证据骨架 `candidate/candidate.json + cases/{sp,p2p-host,p2p-client,u3ds}/case.json` 对齐票面结构；手册覆盖三环境、配置 A/B、时间窗正交重叠、Debug 日志开关、U3DS 无 UI、失败协议。
- 独立复核边界声明：全仓 grep 证实 `BueNetworkRuntime` 无生产构造点（仅自身 ctor）——「BueNetworkApi 尚无生产传输消费者」属实；U3DS 以日志为准可接受（无 UI 属设计事实，面板核验移至 SP/P2P 端）。

# 二、发现

**S1（中高）证据骨架四份 case.json 的 `role` 全部硬编码 `SinglePlayer`**（四文件 SHA-256 相同 `087b19da…`）。票面 Scope 要求按环境绑定证据，验收条件 1「三环境证据齐全且绑定同一 LoadSetIdentity」经 `QualificationEvidenceGate.EvaluateRole` 裁决：`entries.Count == 0) return QualificationVerdict.Missing`——SteamP2PHost/SteamP2PClient/U3dsHeadless 三角色无对应 role 的 case 必判 Missing → exit 3 永非 Fulfilled。而手册 §7 仅要求「把 `TODO` 字段填实（环境指纹/版本/部署来源…采集人/诊断摘要）」，未列 `role`，人工照章填实后门禁仍失败（或迫使 agent 采后代改 role，污染证据来源）。需区分四模板 role（SteamP2PHost/SteamP2PClient/U3dsHeadless）并把 role 写入手册填实清单。

**S2（中）fail-closed 有洞，未覆盖票面核心绑定字段**。`RequiredText` 仅拒 `TODO`，但模板 `startedUtc/endedUtc` 占位 `2026-09-04T00:00:00.0000000Z/+1tick` 是合法 O 时间戳，`PairP2p.Overlaps`（`left.StartedUtc < right.EndedUtc && …`）对双份占位即 PASS——票面「同一 CaseId、时间窗正交重叠」的门禁可被未填模板满足；role 同理（S1）。交付报告 §3「TODO 占位与缺 case.json 均拒绝」的 fail-closed 声明只对 TODO 字段成立。

**S3（低）seam 缺口未记入工单**。output-review-loop：「When a pure host cannot construct a seam, record the seam gap in the ticket and the audit.」缺口仅在交付报告 §4 具名，工单 Comments 无对应记录。补一行即可关闭。

**S4（低，可延期判断）** gate runner fail-closed 自检（缺 case.json→exit 1、TODO→exit 1）无留档 log（其余门禁均有 r1/r2 log）。属证据链一致性 smell，可延期但需具名。

**S5（观察，非阻断）** 手册 P4a「两种结果都是有效证据」措辞不对称：P4a 失败即规格「干净环境装 BUE + 旧 V1 no-op 插件…插件不改代码能收发」（spec L79 / T4「兼容层兜底」）开箱不成立，按交付 §7.3 须走修复轮、本轮不可 Fulfilled。资格裁决报告必须明写「P4a 失败 ⇒ 非 Fulfilled」，不得因「有效证据」表述被读作双解均通过。

# 三、逐项核对结论（对照 Scope 六要点/验收三条）

单人（启动/注册/收发）、P2P 双端 V1+V2、U3DS 无 UI+BII 隔离、零误报配置 A、候选身份绑定、结构沿用——采集侧项均已按「待人工」交付到位，手册/装备齐备；验收 1/3 属实机采集+门禁+人工批准，本轮交付形态合规。唯 S1/S2 使「骨架→门禁」链路存在票面偏离，须修复后复检。

VERDICT: FINDINGS
