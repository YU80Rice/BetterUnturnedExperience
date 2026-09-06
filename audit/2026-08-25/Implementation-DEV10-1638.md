# DEV-10 实施报告：BUE Host 外部功能注册运行时 Tracer Bullet

## 一、需求执行概述

在不实现第三方完整功能、不修改 LMN、不修改 DEV-01～DEV-09 历史成果的前提下，落地 SCR-GPT18-001 的第一条公开注册运行时 seam：显式注册、阶段冻结、确定性 Catalog、稳定拒绝原因、presentation 纯值投影与 Headless 类型隔离。

## 二、源码溯源清单

| 需求点 | 落实位置 |
| --- | --- |
| 公开注册阶段与稳定 reason | `src/BetterUnturnedExperience.Contracts/ContractTypes.cs`：`FeatureRegistrationPhase`、`FeatureRegistrationReason`、`IBueFeatureRegistrationHost`、`FeatureRegistrationResult` |
| 不可变 Definition Artifact 与 factory 分离 | `ContractTypes.cs`：`FeatureDefinitionArtifact`、`IFeatureModuleFactory`；`FeatureRegistrationRuntime.cs`：`FeatureRegistrationSnapshot` |
| RegistrationOpen 接受、CatalogFrozen/RuntimeReady 拒绝晚注册 | `src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs`：`Register`、`OpenRegistration`、`FreezeCatalog`、`MarkRuntimeReady` |
| 重复 FeatureId 与非法 artifact fail-closed | `FeatureRegistrationRuntime.Register`、`IsValidDefinition`、`DigestMatches` |
| 到达顺序无关的 CatalogRevision | `FeatureRegistrationRuntime.FreezeCatalog`、`ComputeRevision`；Contracts.Tests 双顺序测试 |
| ClientUi 表现状态与核心状态分离 | `ContractTypes.cs`：`FeaturePresentationState`、`FeaturePresentationView` |
| UI/native 类型零泄漏 | `eng/Verify-NoUiTokens.ps1` 对 Contracts/Core 扫描 |

## 三、代码变更清单

- 新增：`src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs`
- 修改：`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`
- 修改：`src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj`
- 修改：`tests/BetterUnturnedExperience.Contracts.Tests/Program.cs`
- 新增：`.scratch/better-unturned-experience-architecture/issues/DEV-10-bue-host-external-registration-tracer-bullet.md`

## 四、TDD 与测试记录

1. Red：先在 Contracts.Tests 写入注册阶段/重复/冻结/确定性断言，未实现契约时出现 CS0246。
2. Green：加入最小共享类型与 `FeatureRegistrationRuntime`，测试转为 PASS。
3. 追加垂直切片：非法 artifact、satellite metadata、外部 registration 变更后不漂移、getter 异常 fail-closed；每次均重新构建验证。

## 五、编译验证记录

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：PASS，0 errors / 0 warnings。

## 六、测试结果

- `DEV-10 registration runtime tests: PASS`
- `DEV-03 settings runtime tests: PASS`
- `DEV-04 placement evaluator tests: PASS`
- `DEV-05 ClientUi tests: PASS`
- `DEV-06 network tests: PASS`
- `DEV-08 runtime evidence package tests: PASS`
- `DEV-09 runtime bootstrap tests: PASS`

## 七、静态门禁

- Contracts：`UI/native token scan PASS: 2 C# files`
- Core：`UI/native token scan PASS: 10 C# files`
- Release 中的 `BepInExVersion` 仅为证据字段名，不是类型引用；本票未新增 Release 类型泄漏。

## 八、独立审计记录

- R1：[DEV-10-Independent-Audit-R1.md](DEV-10-Independent-Audit-R1.md) 判定 FAIL，阻断 B-01：外部 registration 引用导致 Catalog 身份可漂移。
- 修复：登记边界建立 `FeatureRegistrationSnapshot`；ClientUi 复制为纯值 snapshot；状态与字典由同一 `sync` 锁线性化；新增 mutation/getter exception 测试。
- R2：[DEV-10-Independent-Audit-R2.md](DEV-10-Independent-Audit-R2.md) 判定 PASS。
- R3：[DEV-10-Independent-Audit-R3.md](DEV-10-Independent-Audit-R3.md) 判定 PASS，复核最终测试标签与最终产物。

## 九、产物哈希

| 产物 | SHA-256 |
| --- | --- |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `1FCB65E8620D39DF3B7F6A8DF65C642F15128348B2A5A87E55D82544339DE8CD` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `630B86C7B4713994677D431C14F52D8C9678949B91725CE7B81259273B9ABECF` |
| `tests/BetterUnturnedExperience.Contracts.Tests/bin/Release/BetterUnturnedExperience.Contracts.Tests.exe` | `21AB94008C8DD83BC02F490B18CB400F133C5B11D08A33F928500A771174ECB0` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `BC1312B996A726466CA4E867AB463F338C34163B74951D540AF6FAC2693D3AB7` |

## 十、偏离与证据边界

无需求偏离。当前仅证明纯 Contracts/Core 注册 seam 的静态与单元行为；尚未证明第三方 BepInEx DLL clean-install、ClientUi satellite 缺失降级、U3DS/SP/P2P 运行或发布授权。

## 十一、后续建议

下一票应实现 no-op 外部功能 fixture 与 BUE Host 物理 ABI/部署验证；继续绑定新的 SourceSet，并保留本票的 registration snapshot 与统一 UI 主权约束。

