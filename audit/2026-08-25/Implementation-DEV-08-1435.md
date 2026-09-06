# DEV-08 实施报告：Runtime Evidence Package + Three-Environment Gate

## 一、需求执行概述

在 DEV-07 静态 CandidateBuild/Qualification 基础上，新增不可变运行证据包 seam。该模块只验证证据材料的 CandidateBuild、DLL SHA-256、CaseId、路径和 artifact 摘要绑定；不启动游戏、不修改运行时状态、不授予发布权。

基线：`BUE-V1-RT01-20260824`  
SourceSet：`BUE-SS-20260824-02`  
工单：`DEV-08-runtime-evidence-package-gate.md`  
状态：`ready-for-human`

## 二、源码溯源清单

| 需求点 | 落实位置 |
|---|---|
| 安全相对 artifact 路径 | `src/BetterUnturnedExperience.Release/RuntimeEvidencePackage.cs:RuntimeEvidenceArtifact` |
| Candidate/DLL/CaseId 绑定 | `RuntimeEvidencePackageValidator.Validate` |
| 64 位 ASCII SHA-256 与案例摘要绑定 | `CandidateBuildDescriptor.DigestValue`、`ArtifactDigestMismatch` |
| null/空包 fail-closed | `EmptyPackage`、`NullEvidenceCase`、`NullArtifact` |
| 稳定 canonical 序列化与摘要 | `RuntimeEvidencePackage.SerializeCanonical`、`CanonicalDigest` |
| TDD seam 测试 | `tests/BetterUnturnedExperience.Release.Tests/Program.cs` |

## 三、代码变更清单

- 新增 `src/BetterUnturnedExperience.Release/RuntimeEvidencePackage.cs`。
- 更新 `src/BetterUnturnedExperience.Release/BetterUnturnedExperience.Release.csproj` 纳入新源码。
- 更新 Release 测试：有效包、确定性、路径穿越、重复 CaseId、空包、artifact 摘要错配、null 元素。
- 新增工单 `DEV-08-runtime-evidence-package-gate.md`。
- 更新 `.scratch/better-unturned-experience-architecture/map.md` 生产 frontier。

## 四、编译与测试验证

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`0 errors / 0 warnings`。

全套测试：

- DEV-02 definition linker：PASS
- DEV-03 settings runtime：PASS
- DEV-04 placement evaluator：PASS
- DEV-05 ClientUi：PASS
- DEV-06 network：PASS
- DEV-08 runtime evidence package：PASS

确定性重建：Release DLL 与 Release.Tests EXE 两次重建哈希一致。

最终产物 SHA-256：

- `BetterUnturnedExperience.Release.dll`：`1F4451D0FBE238AE9D5EA4E6BE0CD5CA0880F5D10D1F519FE230D489FF3CE42A`
- `BetterUnturnedExperience.Release.Tests.exe`：`18E4A1D7D40E81E82A0221CA2E1EB416C61B89F9E6590B70D22C44AC56F32680`

源码 SHA-256：

- `RuntimeEvidencePackage.cs`：`442B98DC6D54EEF11247EAE99644CE1B7F8DEF09A71443ADAFA1513A177838E3`
- `Release.Tests/Program.cs`：`C1A8F0C0F28166D5D8139B5C8D306E129C1D99288ECDDBB1D0AE9E024A866166`。

## 五、静态隔离

Contracts、Core、Transport、Release 精确 `using`/反射/补丁扫描 PASS；未发现 Unity、Glazier、Sleek、Unturned、LMN、BepInEx、Harmony、`Assembly.GetTypes` 或 `PatchAll`。通用 token 脚本对 `BepInExVersion` 字段名的历史误报不作为类型泄漏证据。

## 六、独立审计记录

- R1：FAIL，发现 EvidenceDigest 未绑定 artifact、null 元素可能空引用。
- 修复：增加 `ArtifactDigestMismatch`、`NullEvidenceCase`、`NullArtifact` 结构化诊断及回归测试。
- R2：PASS，0 阻断项。报告：`audit/2026-08-25/DEV-08-Independent-Audit-R2.md`。

## 七、偏离与妥协

无架构偏离。RuntimeEvidencePackage 不读取 Git、日志内容或运行时环境；真实运行证据仍由人工/环境采集流程提供。未修改 LMN、U3DS、原生库存 RPC 或游戏客户端。

## 八、未完成发布门禁

本实施不证明真实单人、SteamP2PFriends Host/Client、U3DS Headless 运行通过，不证明原生库存玩家验收，不证明 ReleaseReady、Stable、1.0.0 或正式发布授权。DEV-08 只能交 Gemini 消费复核并等待人工门禁；DEV-07/08 不得在真实三环境同 Candidate/DLL 证据齐全前标记整体发布通过。

## 九、交给 Gemini 的复核范围

请复核：

1. 前端是否能消费 `EvidencePackageValidationResult` 而不将其误解为运行 PASS；
2. CandidateBuild/DLL/CaseId/路径/摘要绑定是否与前端状态中心边界一致；
3. `EmptyPackage`、`Stale/Mismatched` 与 `NotApplicable` 是否保持资格状态、FeatureState 和发布授权分离；
4. 是否无共享契约缺口。若发现缺口，仅提交 Shared Contract Change Request。

