# GPT-DEV-07 独立审计报告 R1

## 一、审计范围

- 目标：DEV-07 CandidateBuild、证据案例与三环境 Qualification Gate。
- 审计对象：
  - `src/BetterUnturnedExperience.Release/CandidateBuild.cs`
  - `src/BetterUnturnedExperience.Release/Qualification.cs`
  - `tests/BetterUnturnedExperience.Release.Tests/Program.cs`
  - 对应 `.csproj`、DEV-07 工单及 `Contribution-Build-Release-Gates-Spec.md`。
- 原则：只读审计；未修改生产代码、测试、工单或 LMN。
- 基线：`BUE-V1-RT01-20260824`；SourceSet：`BUE-SS-20260824-02`。

## 二、独立复测证据

| 检查项 | 结果 |
|---|---|
| `dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal` | PASS；构建输出未报告 errors/warnings |
| `BetterUnturnedExperience.Release.Tests.exe` | PASS；`DEV-07 release qualification tests: PASS` |
| Release 项目与测试项目目标框架 | PASS；`.NET Framework 4.7.2`、`LangVersion 10.0` |
| `eng/Verify-NoUiTokens.ps1`（Release 及测试项目） | PASS |
| Release/测试源码 forbidden-token 与扫描 API 检索 | PASS；未发现 UI/native/LMN、`Assembly.GetTypes` 或 `PatchAll` |
| 产物哈希 | 已重新读取；本轮仅作为构建证据，不升级为三环境运行证据 |

## 三、逐项结论

### 通过项

1. `BuildIdentity` 在 `CandidateBuild.cs:18` 的 canonical 输入未包含 `DllSha256`，并在测试 `Program.cs:17-20` 用不同 DLL 字符串验证了这一点。
2. canonical 编码使用长度前缀、固定字段顺序和 UTF-8 SHA-256，当前实现对相同输入具有确定性。
3. P2P Host/Client 在 `Qualification.cs:49-52` 具备同 `CaseId` 的配对检查；不一致时两端均为 `Failed`。
4. 缺少某角色返回 `Missing`；候选 BuildIdentity 或 DLL hash 不匹配时不会返回 `Fulfilled`，当前返回 `Stale`。
5. 当前代码没有 `ReleaseReady`、`FeatureState` 或发布授权写入路径，因此没有发现其错误授予运行状态或发布权的证据。
6. Release 模块没有 UI、Unturned native、LMN 或适配器类型引用，项目基线符合 .NET Framework 4.7.2/C# 10。

## 四、阻断项（FAIL）

### B-01：EvidenceCase 未实现规格要求的完整证据字段与验证

- 位置：`Qualification.cs:13-31`、`Program.cs:22-36`。
- 事实：`EvidenceCase` 只有 `CaseId`、`BuildIdentity`、`DllSha256`、`Role`、`EnvironmentFingerprint`、`EvidenceReference`；缺少并未验证 `DefinitionSetDigest`、`ArtifactPayloadDigest`、版本/引用集、部署来源、命令步骤、UTC/本地时间窗口、原始日志/截图哈希、采集者和诊断摘要等 DEV-07/spec §9 必需字段。
- 影响：无法证明证据确实属于当前 CandidateBuild 的完整构建/环境上下文，也无法执行“时间窗口无法关联则 P2P 不满足”的门禁。
- 修复建议：扩展不可变证据 DTO 和 canonical schema；对所有必需字段、时间窗口、来源引用及摘要格式做拒绝式校验，并让 P2P 配对检查消费时间窗口和来源事实。

### B-02：DLL SHA-256 与摘要字段没有格式/规范化验证

- 位置：`CandidateBuild.cs:11-17,53-57`、`Qualification.cs:15-16,31`。
- 事实：`Required` 仅拒绝 `null` 和空字符串；测试使用的 `AA`、`BB`、`OLD` 均可作为 `DllSha256`。没有校验 64 个 ASCII 十六进制字符，也没有拒绝空白、大小写别名或非法字符。Definition/Artifact digest 同样只是任意非空字符串。
- 影响：非哈希占位值可以被判定为 `Fulfilled`，削弱“同一 DLL SHA-256”的发布门禁，且错误输入无法稳定归类为拒绝/诊断。
- 修复建议：冻结并复用 digest/hash parser；严格验证 ASCII 十六进制长度、大小写 canonical form 和空白规则。测试应使用真实 64 字符哈希并覆盖 null/空白/非法/长度错误。

### B-03：QualificationEvaluator 缺少 NotApplicable 政策输入，无法实现该语义

- 位置：`Qualification.cs:43-61`。
- 事实：`QualificationVerdict.NotApplicable` 虽定义在 enum 中，但 `Roles` 固定遍历四个环境，角色无证据只能返回 `Missing`；没有资格义务/政策输入，也不存在仅允许由政策产生 `NotApplicable` 的路径。
- 影响：不能表达“UI facet 在 U3DS 可 NotApplicable、核心仍必须做 U3DS Headless 验证”的规范语义，存在将适用性错误当作缺失或被调用方自行绕过的风险。
- 修复建议：引入独立、不可由证据作者声明的 obligation/policy 输入；仅当政策明确允许时返回 `NotApplicable`，并保证核心 U3DS 义务仍为必需项。

### B-04：P2P 配对检查不是完整的同 CaseId/时间窗口关系判定

- 位置：`Qualification.cs:49-63`。
- 事实：`Matching` 只取第一条匹配记录；当 Host/Client 各有多条当前 Build/hash 记录且有效配对不在首项时，会因首项 CaseId 不同而错误返回 `Failed`。同时没有时间窗口字段或关联检查。
- 影响：重复/重试证据会导致顺序相关的非确定性裁决，并无法满足规格要求的时间窗口关联门禁。
- 修复建议：先形成当前 CandidateBuild/hash 的 Host/Client 集合，再按 CaseId、时间窗口、证据来源和配对约束进行确定性匹配；无唯一合法配对时返回 `Failed` 或 `Missing`，并覆盖多案例、乱序、时间窗不重叠测试。

### B-05：DEV-07 验收所需的构建资格/产物绑定和真实环境证据管线尚未实现

- 位置：Release 模块仅含 `CandidateBuild.cs`、`Qualification.cs`；`DEV-07` 工单验收条件及 spec §7、§9、§10。
- 事实：实现没有两次干净构建比较、最终 DLL/Definition/Artifact/工具链/reference-set 绑定验证、证据包读取与哈希核验、SP/P2P/U3DS 运行编排或真实日志导入。现有测试只验证内存对象，且使用短占位字符串。
- 影响：当前只能证明最小对象级单元行为，不能证明 CandidateBuild gate、三环境同哈希运行资格或发布候选产物。
- 修复建议：先补齐构建/证据 schema 与绑定 verifier，再接入独立的运行证据采集；在缺少真实三环境证据时保持 `in-progress`/`ready-for-human`，不得宣称发布或整体运行通过。

## 五、非阻断建议

- `EvidenceCase.SerializeCanonical()`（`Qualification.cs:26-29`）目前只返回 canonical 文本，未提供 digest 或 schema version；建议加入显式版本字段和稳定的 UTF-8 digest。
- `QualificationResult.For()` 对未知 role 使用字典索引会抛异常；若未来扩展角色，应由版本化 policy/diagnostic 处理，而不是依赖运行时异常。
- 候选输入应拒绝首尾空白，而不是让不同调用方自行决定 trim/大小写策略；BuildIdentity 的 canonical 输入应使用冻结的 identity parser。

## 六、最终判定

**判定：FAIL（5 个阻断项）**。

本轮可以确认：BuildIdentity 排除 DLL hash、基础 deterministic canonical 编码、P2P 同 CaseId 的最小检查、基础 Stale/Missing 分类、框架隔离与编译测试均成立；但 DEV-07 的完整证据 schema、严格哈希验证、NotApplicable 政策、稳健 P2P 时间窗配对和真实 CandidateBuild/三环境门禁尚未满足。

在 B-01～B-05 修复并重新执行 Release 全量构建、全套测试及独立 Round 2 审计前，不得将 DEV-07 标记 `resolved`，不得宣称 ReleaseReady、Stable、三环境运行 PASS 或 GitHub 发布授权。

