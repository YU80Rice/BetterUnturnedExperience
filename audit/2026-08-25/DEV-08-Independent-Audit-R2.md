# GPT-DEV-08 独立审计报告 R2

## 1. 审计范围与基线

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-08-runtime-evidence-package-gate.md`
- 审计范围：`src/BetterUnturnedExperience.Release/RuntimeEvidencePackage.cs`、`CandidateBuild.cs`、`Qualification.cs`、`tests/BetterUnturnedExperience.Release.Tests/Program.cs`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 审计类型：针对 R1 两项阻断的独立复审；未修改源码。

## 2. 最终结论

**判定：PASS（0 阻断项）**

R1 的两项阻断均已闭合：

1. `EvidenceCase.EvidenceDigest` 现在与其 `EvidenceReference` 命中的 artifact `Sha256` 做精确比较，错配返回 `ArtifactDigestMismatch`。
2. `null` case/artifact 不再导致验证器空引用崩溃，而是分别返回 `NullEvidenceCase` / `NullArtifact` 结构化诊断并保持无效结果。

DEV-08 可进入 Gemini 前端消费复核和人工门禁流程，但本审计不等价于任何真实运行环境 PASS、ReleaseReady、Stable 或正式发布授权。

## 3. R1 阻断复核

### R1-B01：证据摘要绑定

- 位置：`src/BetterUnturnedExperience.Release/RuntimeEvidencePackage.cs:144-155`。
- 复核事实：先将 `EvidenceReference` 规范化为 `/` 并确认命中 artifact；命中后比较 `referenced.Sha256 != item.EvidenceDigest`，不一致追加 `ArtifactDigestMismatch`。
- 测试事实：`tests/BetterUnturnedExperience.Release.Tests/Program.cs` 新增 `mismatchedArtifact` 负向用例，断言验证结果无效。
- 判定：PASS。

### R1-B02：空元素 fail-closed

- 位置：`RuntimeEvidencePackage.cs:130-142`。
- 复核事实：`null` case/artifact 被分别映射到 `NullEvidenceCase` / `NullArtifact`，验证继续执行并返回结构化结果；不会访问其成员。
- 测试事实：新增 `nullCase` 与 `nullArtifact` 用例，验证“不抛异常且结果无效”。
- 判定：PASS。

## 4. 需求与 seam 核查

| 检查项 | 结果 | 证据 |
|---|---|---|
| CandidateBuild 绑定 | PASS | 包与案例均比较 BuildIdentity、DefinitionSetDigest、ArtifactPayloadDigest、ToolchainIdentity、ClientReferenceSetId、U3dsReferenceSetId 与 DLL SHA-256 |
| DLL SHA-256 绑定 | PASS | `SameCandidate` 和案例检查均包含 DLL digest；CandidateBuild 的 BuildIdentity 不包含 DLL digest |
| CaseId 唯一性 | PASS | Ordinal `HashSet` 检查重复 CaseId |
| 空包 | PASS | cases/artifacts 任一为空返回 `EmptyPackage` |
| artifact 引用 | PASS | 案例引用必须命中包内 canonical relative path |
| artifact 摘要 | PASS | 64 位 ASCII 十六进制格式校验，并与案例 EvidenceDigest 比对 |
| 路径安全 | PASS | 拒绝空路径、绝对路径、盘符/冒号、`.`、`..`、空段；统一 `\\` 为 `/`；允许 `v1..txt` 等合法文件名 |
| canonical 路径重复 | PASS | 构造时规范化斜杠，验证器以 Ordinal 集合检测重复 |
| 诊断排序 | PASS | `Result` 按稳定 byte 枚举值排序 |
| 运行证据边界 | PASS | 包验证只返回 `EvidencePackageValidationResult`，不改变 `QualificationVerdict`、FeatureState 或发布授权 |

## 5. 构建、测试与确定性证据

执行命令：

```text
dotnet msbuild D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\tests\BetterUnturnedExperience.Release.Tests\bin\Release\BetterUnturnedExperience.Release.Tests.exe
```

结果：

- Solution Release rebuild：0 errors / 0 warnings。
- `bin\\Release` 下 DEV-02～DEV-08 测试全部 PASS：Contracts、Settings、Placement、ClientUi、Network、Release qualification。
- Release test 输出文本仍写作 `DEV-07 release qualification tests: PASS`；这是测试标签陈旧，不影响当前断言结果，但不得将该文本当作 DEV-08 或真实运行 PASS。建议后续改成 `DEV-08 evidence package validation tests: PASS (static/unit only)`。
- 二次 Rebuild 确定性复核：前后哈希一致。

产物 SHA-256：

- `src/BetterUnturnedExperience.Release/bin/Release/BetterUnturnedExperience.Release.dll`
  - `1F4451D0FBE238AE9D5EA4E6BE0CD5CA0880F5D10D1F519FE230D489FF3CE42A`
- `tests/BetterUnturnedExperience.Release.Tests/bin/Release/BetterUnturnedExperience.Release.Tests.exe`
  - `947C2E794C1D56A617CF48E3AC3E4574084FDB352910048FC91A88D7C86110B0`

## 6. 依赖与隔离审计

- Release 项目目标 `.NET Framework 4.7.2`，`LangVersion 10.0`，`Deterministic=true`，`TreatWarningsAsErrors=true`。
- Release 生产项目仅引用 `System`、`System.Core`、`System.Security`。
- 对 Release 源码与 Release 测试执行 UI/native/LMN 类型模式扫描；未发现 Unity、Glazier、Sleek、Unturned、LMN、BepInEx、Harmony、`Assembly.GetTypes`、`PatchAll` 或反射入口。测试中的 `BepInExVersion` 是字段名，不是类型引用。
- DEV-08 未修改 LMN、U3DS、原生库存 RPC 或游戏客户端文件。

## 7. 非阻断建议

1. 将 Release 测试输出标签从 DEV-07 改为 DEV-08，并明确“静态/单元测试，不代表运行验收”，避免审计日志被误读。
2. `RuntimeEvidencePackage.SerializeCanonical()` 对包含 null 元素的无效包仍不能序列化；当前导入验证边界已 fail-closed，且有效包的 canonical 序列化通过。若未来要求在验证前生成损坏包诊断摘要，应增加显式 invalid-package 序列化路径。
3. `RuntimeEvidencePackageValidator` 对每个案例执行 artifact 线性查找；当前包规模很小且不在运行热路径，后续可用路径索引改善复杂度，但不构成 DEV-08 阻断。
4. `QualificationEvaluator` 本身仍假定输入 EvidenceCase 集合无 null 项；DEV-08 的正确调用顺序应是先验证包，再把验证通过的案例交给资格裁决。该边界符合工单窄连接，不构成本票阻断。

## 8. 后续门禁

本报告只证明 DEV-08 的代码、单元测试、构建、确定性与静态隔离。仍必须完成：

- Gemini 前端消费复核 ACCEPT；
- 工单状态保持 `ready-for-human`，直到人工批准；
- 真实 SP、SteamP2PFriends Host/Client、U3DS Headless 运行证据，且绑定同一 CandidateBuild、DLL SHA-256、CaseId 与证据包；
- 独立三环境资格审查与发布授权。

在上述证据完成前，不得宣称插件已在三环境运行通过、ReleaseReady、Stable 或正式发布。
