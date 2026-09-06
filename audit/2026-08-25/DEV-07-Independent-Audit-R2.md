# GPT-DEV-07 独立审计报告 R2

## 一、审计元数据

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-07-candidate-build-three-environment-gate.md`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 审计轮次：Round 2；本报告只读审计，未修改生产源码、测试、LMN 或工单状态
- 审计对象：
  - `src/BetterUnturnedExperience.Release/CandidateBuild.cs`
  - `src/BetterUnturnedExperience.Release/Qualification.cs`
  - `tests/BetterUnturnedExperience.Release.Tests/Program.cs`
  - DEV-07 工单与 `Contribution-Build-Release-Gates-Spec.md`

## 二、最终判定

**FAIL（仍有 3 类阻断项；不得关闭 DEV-07，不得宣称 ReleaseReady、Stable、三环境运行 PASS 或发布授权）。**

R1 的 B-02、B-03 及 B-04 的主要集合匹配逻辑已修复：摘要/哈希已有 64 个 ASCII 十六进制字符校验，资格政策独立存在且默认要求 U3DS Headless、将 U3DS Client UI 标记为 `NotApplicable`，P2P 也已按当前 CandidateBuild/hash、同 `CaseId` 与时间窗口进行集合配对。可是以下问题仍阻断 DEV-07 的完整验收。

## 三、复测证据

### 3.1 Release 重建

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：**PASS，exit 0，0 errors，0 warnings**。

### 3.2 DEV-02～DEV-07 全套测试

| 测试 | 结果 |
|---|---|
| DEV-02 Definition Linker | PASS |
| DEV-03 SettingsRuntime | PASS |
| DEV-04 Placement Evaluator | PASS |
| DEV-05 ClientUi | PASS |
| DEV-06 Network | PASS |
| DEV-07 Release qualification | PASS |

实际输出均为对应的 `... tests: PASS`。

### 3.3 静态隔离

- `eng/Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Contracts`：PASS。
- `eng/Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Core`：PASS。
- Transport、Plugin、ClientUi、Release 及 Release.Tests 的语义引用扫描：未发现 Unity、Unturned native、Glazier、Sleek、Harmony、LMN、`Assembly.GetTypes()` 或 `PatchAll()` 的类型/调用引用。
- 对 Release 使用通用 `Verify-NoUiTokens.ps1` 会把证据字段 `BepInExVersion` 误报为 UI/native token；这不是类型引用，故以语义扫描结果为准，未将该脚本误报计为源码泄漏。

### 3.4 产物哈希

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Release/bin/Release/BetterUnturnedExperience.Release.dll` | `7AFAECF1C569AA57150E7CE113810E18271AE6DF9850D7C4682779DB1336ED7A` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `2A78538DD468A8FF358C227DE6B43241CD59C20C2E626247660DF4AD24D1197C` |
| `src/BetterUnturnedExperience.Transport/bin/Release/BetterUnturnedExperience.Transport.dll` | `595BFE6BFF1B07AF95B77215DB3C82B62B71E1CAEC09DF76ED6913ED1843B1E4` |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `068A6DD7D14C0FA004E36C38F4728ADC76A2701B40F11FE3CACE14F297E3F23F` |

这些哈希只证明本次构建产物身份，不证明任何运行环境通过。

## 四、R1 阻断项复核

| R1 项 | 本轮结论 | 依据 |
|---|---|---|
| B-01 Evidence metadata / UTC | **PARTIAL** | `EvidenceCase` 已包含 Candidate、CaseId、角色、环境、版本、部署来源、命令、UTC 时间、证据引用/摘要、采集者和诊断摘要；但仍缺少规格要求的显式 LMN、SteamP2PFriends/U3DS 版本/引用集字段和本地时间/时区表示，且零长度时间窗仍可创建。 |
| B-02 严格哈希与摘要 | **PASS（实现层）** | `CandidateBuildDescriptor.DigestValue` 对 Definition、Artifact、DLL 和 Evidence digest 统一执行 64 字符 ASCII 十六进制校验；小写输入被规范化为大写。非法长度/字符和空值不能进入构造。 |
| B-03 Qualification policy | **PASS（实现层）** | `QualificationPolicy` 为独立输入；构造时拒绝关闭 U3DS Headless；`Default` 令 U3DS Client UI 为 `NotApplicable`，且 `NotApplicable` 只能来自 policy。 |
| B-04 P2P matching | **PARTIAL** | 已改为遍历 Host/Client 集合、按同 CaseId 与时间窗判定，乱序多案例和无重叠窗口的基本行为正确；但时间窗的零长度/边界语义未被拒绝，导致零时长或仅端点相接的案例可能被视为有效配对。 |
| B-05 CandidateBuild / runtime evidence gate | **FAIL** | Release 项目仍只有 `CandidateBuild.cs` 与 `Qualification.cs`；没有证据包读取/验证、Build Artifact Binding、两次干净构建一致性比较、真实日志导入、SP/P2P Host+Client/U3DS Headless 运行编排或真实运行证据归档。 |

## 五、阻断项详情

### B-01-R2：EvidenceCase schema 仍未完全满足规格

位置：`src/BetterUnturnedExperience.Release/Qualification.cs:24-50`。

当前字段虽然明显比 R1 完整，但 `transportVersion` 是泛化字段，不能无歧义表达规格要求的 LMN、SteamP2PFriends 与 U3DS 版本/引用来源；同样没有显式本地时间及 timezone offset，只有 `StartedUtc`/`EndedUtc`。原始日志、截图/录像也只通过一个泛化的 `EvidenceReference + EvidenceDigest` 表示，无法区分各类原始证据。

这会让证据包无法被机械校验为“完整的环境来源与时间上下文”，因此 B-01 不能标记为完全关闭。应补齐冻结 schema 的显式字段或明确的结构化 `EnvironmentReferenceSet`、`LocalTimeOffset` 与原始证据引用集合，并继续保持 canonical serialization。

### B-02-R2：时间窗口允许零长度，且 P2P 采用闭区间边界

位置：`Qualification.cs:27`、`Qualification.cs:78`。

构造检查使用 `endedUtc < startedUtc`，因此 `start == end` 会被接受；错误消息却声称 “end after start”。随后 `Overlaps` 使用 `<=`，所以 Host `[t0,t1]` 与 Client `[t1,t2]` 仅在一个端点相接时会被评为重叠。针对当前 DLL 的独立反射复测得到：

```text
ZERO_WINDOW_ACCEPTED
TOUCHING_WINDOWS=Fulfilled/Fulfilled
```

这不满足“可关联运行时间窗口”的发布语义；没有共同的正时长运行区间时，P2P 证据不应 Fulfilled。应拒绝零长度窗口，并使用正长度交集判定（例如 `left.StartedUtc < right.EndedUtc && right.StartedUtc < left.EndedUtc`），同时补齐边界回归测试。

### B-03-R2：CandidateBuild 完整发布绑定与真实证据管线仍缺失

位置：`src/BetterUnturnedExperience.Release/` 全部实现范围及 DEV-07 工单验收条件。

当前对象模型能把 BuildIdentity、Definition/Artifact digest、工具链、双 reference-set 与 DLL hash 放入 `CandidateBuildDescriptor`，QualificationEvaluator 也能拒绝不同候选/hash 的证据；但这只是内存对象级比较，不是完整的 CandidateBuild gate。缺少：

- `AssemblyBuildBinding`/Artifact header 生成与验证；
- 两次干净 checkout/固定工具链构建的字节与 SHA-256 一致性证明；
- 证据包读取、原始日志/截图哈希核验及来源验证；
- SP、SteamP2PFriends Host/Client、U3DS Headless 的真实运行证据采集/导入；
- 与当前 DLL 哈希绑定的三环境 CaseId 证据归档。

因此本轮不能把单元测试 PASS 或当前 DLL 哈希升级为三环境资格 PASS。

## 六、独立边界复测

已独立确认以下行为成立：

1. Lowercase 64-hex digest 会被规范化为 uppercase 64-hex；非法长度/非十六进制字符无法构造。
2. U3DS Headless policy 禁用会抛出参数异常；默认 U3DS Client UI 为 `NotApplicable`。
3. P2P 多案例乱序输入可以找到后续有效同 CaseId/重叠窗口配对。
4. P2P 无重叠窗口返回 `Failed/Failed`。
5. 零长度证据窗口当前可创建，端点相接窗口当前返回 `Fulfilled/Fulfilled`（阻断）。

## 七、修复前置条件

1. 冻结完整 EvidenceCase schema，显式承载环境版本/引用集、本地时间上下文及原始证据引用集合；保持 UTC 规范化和 canonical serialization。
2. 将时间窗改为严格正长度，并把 P2P 配对改为正长度交集；增加 zero-length、touching-boundary、乱序多案例和不重叠回归测试。
3. 实现或明确独立的 CandidateBuild/Artifact Binding verifier 与证据包导入/验证 seam；取得当前 DLL 对应的 SP、P2P Host/Client、U3DS Headless 真实证据后再评估资格。
4. 修复后重新 Release Rebuild、运行 DEV-02～DEV-07 全套测试，并重新执行独立 Round 3 审计；不得复用本轮哈希或运行证据作为后续新源码的证据。

## 八、审计结论

**FAIL。** 本轮证实 R1 的哈希校验、独立 QualificationPolicy 和基本确定性 P2P 集合配对已具备；但完整 Evidence schema、严格有效时间窗、CandidateBuild/Artifact 绑定与真实三环境证据门禁仍未闭环。DEV-07 工单保持现状，不得标记 `resolved`，也不得声明插件整体已通过发布验收。

