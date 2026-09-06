轴: Spec｜轮: R2｜审查员: 全新独立子代理(gpt-5.6-luna，提供商账单实证)

# DEV-V2-07 Spec 轴 R2 独立复审

## 0. 审查基线与判定口径

基线为冻结清单 v3 `audit/2026-09-04/DEV-V2-07/dev-v2-07-review-freeze-r1.diff`，HEAD 为 `ba7ecd9c7a82b6201f61f652c2df9ada6ffdf063`，对照工单 `.scratch/bue-v2-lmn-adoption/issues/DEV-V2-07-three-env-network-validation.md` 与规格 `.scratch/bue-v2-lmn-adoption/spec-V2-phase1-lmn-adoption.md`「Testing Decisions：三环境验证」。本轮只审 Spec 轴，不把尚待人工实机采集误判为缺交付；“就绪”不等于三环境验收已完成，也不等于门禁当前应为 Fulfilled。

## 1. R1 修复逐项核验

### S1 修复：四环境 role 固定 — PASS

实测命令：

```text
python -c 读取 audit/2026-09-04/evidence/DEV-V2-07-20260904/cases/*/case.json 并打印 role
```

输出摘要：

```text
cases/p2p-client/case.json role=SteamP2PClient
cases/p2p-host/case.json role=SteamP2PHost
cases/sp/case.json role=SinglePlayer
cases/u3ds/case.json role=U3dsHeadless
```

逐一对应 `src/BetterUnturnedExperience.Release/Qualification.cs:8` 的枚举名 `SinglePlayer, SteamP2PHost, SteamP2PClient, U3dsHeadless`，无拼写或大小写偏差。手册 `audit/2026-09-04/DEV-V2-07/DEV-V2-07-three-env-network-handbook.md:126` 已明写 role 按 case 目录固定且“采集时不得改动”。R1 的“全硬编码 SinglePlayer 导致三环境 Missing”已消除。

### S2 修复：时间窗占位 fail-closed — PASS

四份 case.json 的 `startedUtc`、`endedUtc` 均为 `TODO`，没有可被 `PairP2p.Overlaps` 当作合法时间戳的默认值。

按要求用 Bash 在 `/tmp` 构造临时包：只建立 `cases/sp/case.json`，除 `startedUtc/endedUtc` 外字段填实，并提供 evidence.log、diagnostics.zip、screenshots-or-video.txt；运行：

```text
audit/2026-09-04/DEV-V2-07/kit/out/QualificationGateRunner.exe gate --package <临时包> --candidate <临时包>/candidate.json
```

实测输出摘要：

```text
EXIT=1
runner-fault errorType=ArgumentException message=case field 'startedUtc' must be filled before gating
```

临时包随后已删除。该消息发生在 `Program.cs:185` 的 `ParseUtc`/资格裁决之前；`RequiredText`（`Program.cs:281-285`）先拒绝 TODO，因此占位不能穿过时间解析和 `PairP2p.Overlaps`。S2 修复成立。

### S3 修复：具名 seam 缺口同步入 Comments — PASS

工单 Comments（`DEV-V2-07-three-env-network-validation.md:34`）已具名记录：本票无红测的 seam 缺口是“源码零修改无新生产 seam；fixture 判据=实机日志行为本身；gate runner 判据=fail-closed 自检 + 复用 01–06 已双轴 CLEAN 的 Release 类型”。

其内容与交付报告 `audit/2026-09-04/DEV-V2-07/Delivery-DEV-V2-07-validation-kit-20260904.md:47-49` §4 一致，且明确说明未做红测不是静默跳过，而是本票无实现缺陷可红。R1 S3 已关闭。

### S4 修复：fail-closed 自检留档 — PASS

文件均存在，且与交付报告 §3（`Delivery-DEV-V2-07-validation-kit-20260904.md:43-45`）引用一致：

- `audit/2026-09-04/DEV-V2-07/gate-selfcheck-missing-casefile-r2.log`：内容为 `gate-error missing case.json in ...`；报告记录 exit 1。
- `audit/2026-09-04/DEV-V2-07/gate-selfcheck-todo-r2.log`：内容为 `runner-fault errorType=ArgumentException message=case field 'collector' must be filled before gating`；报告记录 exit 1。

两项都证明缺失 case.json 或 TODO 占位 fail-closed；本轮另以 startedUtc TODO 临时包实测了 S2 所需的前置拒绝。

### S5 修复：P4a 失败的资格语义 — PASS

手册 `audit/2026-09-04/DEV-V2-07/DEV-V2-07-three-env-network-handbook.md:77` 已明确写明：

> P4a 失败 ⇒ V1 兼容开箱不成立 ⇒ 资格裁决非 Fulfilled，须走 real-machine-test-loop 修复轮出新候选后重采；P4b 通过只算修复验证证据。

这与工单 Scope“旧 V1 no-op 插件无改动收发”、规格 V1 验收（`.scratch/bue-v2-lmn-adoption/spec-V2-phase1-lmn-adoption.md:79`）及交付报告 §7.3 的失败处理一致。R1 S5 已关闭。

## 2. 全量重审：Scope 六要点

以下结论按“agent 交付到待人工实机采集即算就绪”的口径；实际证据采集、资格裁决和人工批准仍未宣称完成。

1. **同一 CandidateBuild / DLL SHA-256 / LoadSetIdentity 绑定** — PASS（就绪）。`candidate/candidate.json` 的 `candidateBuild=DEV-V2-07-CLEAN-20260904`、`caseId=DEV-V2-07-20260904`、`dllSha256=14A98FC8...E5EF6`、`buildIdentity=85FAEA17...A53D`、SourceSnapshotId 与报告一致；四份 case 的 caseId 均与 candidate 一致。`QualificationGateRunner` 以同一个 candidate 构造每个 `EvidenceCase`，不是用单一 DLL 哈希拼接证据。LoadSetIdentity 按交付报告 §2 约定在实机证据采集后随裁决绑定，手册已要求三环境使用同一候选及逐件哈希核对，故当前是正确的待采集状态。
2. **单人本地网络模块启动、频道注册、发送/接收** — PASS（就绪）。手册配置 A 覆盖无 LMN 的零误报启动，配置 B 的 B1/B2/B3 覆盖网络模块、fixture/频道注册及 V1/V2 ping-pong 回环，并要求保留日志。
3. **SteamP2PFriends Host/Client V2 + V1 互通、CaseId 与重叠时间窗** — PASS（就绪）。手册 §4 P1-P6 明确双端同候选、同 LMN/fixture、同 CaseId；P3 验 V2 seq 对齐，P4a/P4b 验旧 V1 no-op 收发及镜像时机，开始/结束 UTC 时间窗要求正交重叠。四模板 role 修复后可被资格门禁正确识别。
4. **U3DS Headless 无 UI、网络启动、BII 隔离** — PASS（就绪）。手册 §5 U-A1/U-A2/U-A3 与 U-B1-U-B5 覆盖无 UI/ClientUi/Sleek/Glazier、接管日志、客户端 BII 不受网络故障影响及 clean shutdown。
5. **证据包结构** — PASS（就绪）。已有 `candidate/candidate.json` 与 `cases/{sp,p2p-host,p2p-client,u3ds}/case.json`，手册 §7 明确每 case 的 evidence.log、diagnostics.zip（可选）、screenshots-or-video.txt、哈希记录及提交后 gate 命令。实机日志和附件尚未产生是手册所声明的下一阶段，不是本增量漏交。
6. **U3DS 独立 LMN 接管与无 LMN 零误报** — PASS（就绪）。配置 A/B 及 U-A1/U-B1 分别覆盖无 LMN 零 patch/零故障和有 LMN 的 takeover/fixture；U3DS 接管证据按规格和手册以日志为准，UI 接管卡在 SP/P2P 核验。

## 3. 全量重审：验收三条

1. **三环境证据齐全且同一 LoadSetIdentity** — 当前未完成实机采集，状态为“待人工实机采集”，不是 FAIL；手册、模板、身份和 fail-closed gate 已就绪。
2. **V2 第一阶段完成标准 8 条** — 当前票面不声称已完成；需依赖实机证据与既有 01-06 结果，由人工验收闭合。本增量已把所需采集矩阵、日志判据和失败回路准备到位，不构成 Spec 缺口。
3. **证据包通过资格裁决（Fulfilled）** — 当前不应为 Fulfilled，因为四模板仍是 TODO，且 gate 应 fail-closed；人工采集填实后再运行 gate。该状态符合工单验收条件及交付报告“待人工实机采集 → 门禁裁决 → 人工批准”。

## 4. 独立核验：状态、哈希、身份与边界

- `git status --porcelain | grep -v "^??"` 仅输出：`M .scratch/bue-v2-lmn-adoption/issues/DEV-V2-07-three-env-network-validation.md`；符合“不修改源码”。`git diff --check` exit 0，仅有 autocrlf 信息提示。
- 命令 `certutil -hashfile "audit/2026-09-04/artifacts/DEV-V2-07-20260904/BetterUnturnedExperience.dll" SHA256` 输出：`14a98fc838b343fac68dafe3b1a8224c5a2484e7a211e9e24e1973e0b6ea5ef6`，与要求的 `14a98fc838b343fac68dafe3b1a8224c5a2484e7a211e9e24e1973e0b6ea5ef6` 完全一致。
- 冻结清单 v3 中列出的 13 个输入文件逐一 SHA-256 复核全部 PASS；包括交付报告、手册、kit 源码/csproj、candidate.json、四份 case.json、两份 R1 报告。候选身份值与工单认领 Comments 的 CaseId、CandidateBuild、DLL SHA-256、SourceSnapshotId 一致。
- `gates-summary-r2.log` 记录 Release 重建 0 error/0 warning、七运行器全 exit=0、NoUiTokens 两侧 PASS、确定性重建 DLL 与归档候选字节一致；这些是当前就绪基线，不替代实机验收。
- 已知边界核验：`rg` 在生产 `src` 中只找到 `BueNetworkRuntime` 构造函数声明（`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:49`），没有生产 `new BueNetworkRuntime` 消费点。交付报告 §7.2 与手册 §8 已如实声明 BueNetworkApi/BUE2 尚无生产传输消费者；当前 P2P V2 实机项验证的是经 BUE takeover 决策点委托的 LMN2 命名通道，BueNetworkApi 本体由 Contracts/Core seam 测试覆盖。该边界足以支撑“待人工实机采集”的就绪状态，因为边界、替代证据和后续需要生产绑定的条件均已具名；它不支持把本票当前状态表述为“BUE2 真实传输已实机验证”。
- V1 镜像时机边界也已具名：手册 P4a/P4b 区分 bootstrap 镜像与旧插件 Awake 晚注册；P4a 失败明确导致非 Fulfilled并进入修复轮。因此该已知风险被正确转化为人工采集判别点，不构成当前 Spec 缺口。

## 5. 新发现

无。R1 的 S1-S5 均已按冻结清单 v3 修复并关闭；未发现工单 Scope、三条验收条件或规格 Testing Decisions 对当前“待人工实机采集”交付形态的新偏离。

VERDICT: CLEAN