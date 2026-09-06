# GPT-DEV-08 独立审计报告 R1

## 1. 审计范围与基线

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-08-runtime-evidence-package-gate.md`
- 审计范围：`src/BetterUnturnedExperience.Release/RuntimeEvidencePackage.cs`、`CandidateBuild.cs`、`Qualification.cs`、`tests/BetterUnturnedExperience.Release.Tests/Program.cs`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 审计性质：只读独立审计；未修改生产源码。

## 2. 结论

**判定：FAIL**

阻断项：2 项。当前实现不能通过 DEV-08 独立审计，不能将工单标记为 `resolved`，也不能宣称真实单人、SteamP2PFriends Host/Client 或 U3DS 运行通过。

## 3. 阻断项

### DEV08-B01：EvidenceCase 摘要未绑定被引用工件摘要

- 位置：`src/BetterUnturnedExperience.Release/RuntimeEvidencePackage.cs:120-140`；相关字段为 `src/BetterUnturnedExperience.Release/Qualification.cs:41-42`。
- 事实：验证器只检查 `EvidenceReference` 能命中某个 artifact 路径，并检查 artifact 自身 SHA-256 字符串格式；没有比较 `EvidenceCase.EvidenceDigest` 与该路径对应的 `RuntimeEvidenceArtifact.Sha256`。
- 影响：攻击者或误配置的导入包可以用合法格式但错误的 `EvidenceDigest` 指向工件，仍返回 `Valid`。这违反工单要求的“路径引用/摘要一致性校验”，削弱 Candidate/DLL/CaseId 之外的证据完整性绑定。
- 修复要求：按 canonical relative path 建立 artifact 索引；每个 case 命中后必须同时满足 `case.EvidenceDigest == artifact.Sha256`。增加稳定的专用诊断码（例如 `ArtifactDigestMismatch`），并补充错误摘要测试。仍应保持只表示 `EvidencePackageValid`，不得触发或伪造 QualificationVerdict/ReleaseReady。

### DEV08-B02：验证器不是对公开可构造的空元素包 fail-closed

- 位置：`RuntimeEvidencePackage.cs:66-67`、`:115-127`、`:134-140`。
- 事实：`RuntimeEvidencePackage.Create` 复制集合但不拒绝集合中的 `null` 元素；`RuntimeEvidencePackageValidator.Validate` 直接访问 `item.Candidate`、`item.CaseId`、`artifact.RelativePath`，因此包含 null case/artifact 的公开输入会抛出 `NullReferenceException`，而不是返回结构化无效结果。
- 影响：包导入边界遇到损坏/恶意材料时可使验证流程异常退出，无法满足审计容器应有的 fail-closed 诊断语义；调用方不能可靠区分“包无效”和“验证器崩溃”。`SerializeCanonical` 对同类输入也会空引用崩溃。
- 修复要求：在包构造阶段拒绝 null 元素，或在验证器中将 null 元素映射为稳定的 invalid-package 诊断并继续收集其他诊断；canonical 序列化也必须不对可接受的无效输入产生未分类空引用异常。增加 null case/null artifact 测试。

## 4. 已通过检查

- Candidate 与 DLL：`RuntimeEvidencePackageValidator.SameCandidate` 比较 BuildIdentity、DLL SHA-256、Definition/Artifact/Toolchain/两个 reference-set；案例也重新绑定 Candidate。未发现跨 Candidate 或跨 DLL 的接受路径。
- 空包：cases 或 artifacts 任一为空时返回 `EmptyPackage`，现有测试通过。
- 重复 CaseId：Ordinal HashSet 检测，现有测试通过。
- 路径安全：工件构造拒绝空路径、绝对路径、盘符/冒号、`.`、`..` 和空段；反斜杠规范化为 `/`；连续点文件名（如 `logs/v1..txt`）被正确允许。现有路径测试通过。
- 重复 canonical artifact path：构造时统一斜杠，验证器使用 Ordinal 集合检测重复。
- 摘要格式：CandidateBuildDescriptor 对 64 位 ASCII 十六进制进行严格校验；工件工厂复用该规则。
- 诊断排序：`Result` 按 `EvidencePackageValidationCode` 的稳定 byte 值排序；当前源码已包含此修正。
- 资格边界：DEV-08 代码没有把包验证结果直接提升为运行资格、FeatureState 或发布授权；真实三环境证据仍未被代码或测试证明。
- 依赖/隔离：Release 工程仅引用 BCL（System、System.Core、System.Security），源码未发现 Unity、Glazier、Sleek、Unturned、LMN、BepInEx、Harmony 或反射扫描/`PatchAll` 类型泄漏。工程目标为 .NET Framework 4.7.2、C# 10，且 TreatWarningsAsErrors=true。

## 5. 构建与测试证据

命令：

```text
dotnet msbuild D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\tests\BetterUnturnedExperience.Release.Tests\bin\Release\BetterUnturnedExperience.Release.Tests.exe
```

结果：

- Release solution rebuild：0 errors / 0 warnings。
- 实际 `bin\\Release` Release tests：PASS，输出为 `DEV-07 release qualification tests: PASS`。
- 该输出标签仍沿用 DEV-07，不能作为 DEV-08 runtime evidence 或三环境运行 PASS；建议改成明确的 DEV-08 package-validation tests 标签，并写明“静态/单元测试，不代表运行验收”。
- 递归执行 `tests` 下全部 `*.Tests.exe` 时会误执行 `obj\\Release` 的旧副本并因缺少依赖失败；这不是 `bin\\Release` 构建失败，但测试发现脚本/收集方式应只枚举 `bin\\Release` 产物。

当前构建产物（本轮重建后）：

- `src/BetterUnturnedExperience.Release/bin/Release/BetterUnturnedExperience.Release.dll`
  - SHA-256：`F7D1EE590A0A2E9DBCA3737690FCBF61F69B091FB8A1FDD69D7DE7201C2FBD07`
- `tests/BetterUnturnedExperience.Release.Tests/bin/Release/BetterUnturnedExperience.Release.Tests.exe`
  - SHA-256：`ACACCE12CB72D6FCE0C5A915FC095A6803086D0A2E804D43C23481C45FB907C9`

## 6. 非阻断建议

1. `RuntimeEvidencePackage.SerializeCanonical` 对重复 CaseId/重复 artifact path 的同键项只按主键排序；无效包若也要求字节级确定性，应增加完整字段 tie-breaker。有效包因主键唯一不受影响。
2. `EvidenceReference` 当前通过与已验证 artifact 集合匹配而间接获得安全边界；可在 `EvidenceCase` 构造或独立 canonical-path helper 中显式复用同一相对路径校验，减少未来 seam 漂移。
3. 在 DEV-08 测试中增加：大小写/反斜杠 canonical path、错误 case digest、null 元素、包 Candidate mismatch、artifact 未引用、跨 DLL 同 BuildIdentity 等覆盖。
4. 测试和报告应明确：当前只证明编译、静态隔离和资格/包算法；尚无 SP、P2P Host/Client、U3DS 实际运行证据。

## 7. 复审门禁

修复 DEV08-B01 与 DEV08-B02 后，必须重新执行：

1. Release 全量 Rebuild（0 errors / 0 warnings）；
2. DEV-02～DEV-08 `bin\\Release` 测试；
3. Release/Transport/Core/Contracts 类型隔离扫描；
4. 重新生成 DLL/测试产物 SHA-256；
5. 独立审计 R2。

在 R2 PASS、Gemini 前端复核 ACCEPT、人工批准且真实三环境证据绑定同一 CandidateBuild/DLL/CaseId 之前，不得关闭 DEV-08 或宣称 ReleaseReady/Stable/正式发布。
