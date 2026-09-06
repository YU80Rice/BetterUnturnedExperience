# GPT-DEV-10 独立审计报告 R2

## 1. 审计范围与基线

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-10-bue-host-external-registration-tracer-bullet.md`
- 共享变更：`SCR-GPT18-001`
- 规格基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 审计对象：
  - `src/BetterUnturnedExperience.Contracts/ContractTypes.cs`
  - `src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs`
  - `tests/BetterUnturnedExperience.Contracts.Tests/Program.cs`
- 方法：源码审查、R1 阻断复测、全仓 Release 重编译、全部测试程序执行、Contracts/Core UI/native token scan、产物 SHA-256 记录。

## 2. 最终判定

**PASS（DEV-10 独立审计通过，可将工单推进为 `ready-for-human`；不代表第三方 DLL、U3DS/SP/P2P 或三环境运行通过）。**

R1 的 B-01 已闭环：运行时不再保存外部可变 `IFeatureRegistration`，而是在 Register 边界建立内部 `FeatureRegistrationSnapshot`；Catalog 冻结只消费内部快照。阶段转移和登记/冻结操作已由同一 `sync` 锁线性化，登记后 Definition 变更与 getter 异常均有测试覆盖。

## 3. R1 阻断复测

### B-01：外部 registration 变更导致 Catalog 身份漂移

**PASS。** `registrations` 当前类型为 `Dictionary<string, FeatureRegistrationSnapshot>`。Register 在成功进入字典前读取并验证 Definition、Contract、Factory、ClientUi，并构造内部快照；`FeatureRegistrationEntry` 与 `FreezeCatalog` 只接收/读取 `FeatureRegistrationSnapshot`。测试将已接受 registration 的 Definition 改为另一个 FeatureId/digest 后，冻结结果仍保持原始 FeatureId，证明 Catalog 不随外部对象漂移。

### B-01 异常穿透子项

**PASS。** registration getter 读取与快照构造位于 `try/catch` 中，异常返回稳定 `InvalidDefinitionArtifact` / `BUE-REG-009`，未观察到异常穿透；`ThrowingRegistration` 测试通过。

### B-01 线程/重入子项

**PASS（静态边界）。** `OpenRegistration`、`Register` 的阶段检查与二次提交、`FreezeCatalog`、`MarkRuntimeReady`、`EnterCoreSafeMode` 均使用同一 `sync` 锁；Register 在外部 getter/快照读取期间释放锁，避免把未知外部代码调用置于内部锁中；提交前再次检查阶段和重复 FeatureId，避免快照构造期间发生 Freeze/切换阶段后的错误提交。Registration 字典和 Catalog 写入均在锁内，阶段转换具有线性化点。

## 4. 审计矩阵

| 维度 | 判定 | 证据 |
| :--- | :---: | :--- |
| 需求符合性 | PASS | 阶段机、显式注册、拒绝原因、确定性排序、不可变 Catalog 和 presentation value projection 均已实现；明确未实现 DEV-11+。 |
| 阶段机与状态安全 | PASS | `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady`；早注册、晚注册、重复注册、重复冻结/Ready 的行为由阶段判断保护。`CoreSafeMode` 注册拒绝为 `CoreUnavailable`。 |
| Registration 快照 | PASS | 内部 `FeatureRegistrationSnapshot` 持有已验证 Definition、Contract、Factory；ClientUi 复制为 `ClientUiSatelliteSnapshot`，不再持有外部 registration 对象。 |
| Definition 不可变性 | PASS | `FeatureDefinitionArtifact` 只读属性，payload 构造时 defensive copy 并暴露 `ReadOnlyCollection<byte>`；Catalog Entries 也是只读集合。 |
| Artifact digest | PASS | `IsValidDefinition` 对非空 payload 计算 SHA-256，并将其 256-bit 值与 `ArtifactPayloadDigest` 比较；非法/空 artifact 不进入字典、不污染冻结 Catalog。此处仅验证 payload digest，不越界声称实现完整 artifact-container verifier。 |
| Catalog 确定性 | PASS | 以 FeatureId、DefinitionSetDigest、ArtifactPayloadDigest 的 Ordinal 稳定排序生成 Catalog；Revision 来自规范化条目集合，打乱登记顺序的双实例测试通过。 |
| 数据一致性 | PASS | Register 在提交锁内再次确认 Phase、重复 FeatureId；快照构造失败不写入字典；Freeze 只在 RegistrationOpen 且一次性转入 CatalogFrozen。 |
| 线程/重入 | PASS（静态） | 单一 `sync` 锁覆盖状态/字典/Catalog 线性化；外部 getter 不在锁内调用；二次检查防止构造窗口竞争。未宣称真实多线程压力运行证据。 |
| 错误模型 | PASS | HostUnavailable、CoreUnavailable、PhaseClosed、InvalidDefinitionArtifact、InvalidModuleFactory、ContractIncompatible、InvalidClientUiRegistration、DuplicateFeature 均有稳定 reason/DiagnosticId；getter 异常 fail-closed。 |
| Contracts/Core 类型隔离 | PASS | `Verify-NoUiTokens.ps1` 对 Contracts 与 Core 均 PASS；源码不引用 Unity、Glazier、Sleek、Unturned、LMN、BepInEx、Harmony 具体类型。 |
| DEV-11+ 边界 | PASS | 未实现外部 BepInEx DLL/no-op fixture、ClientUi satellite 装载、LoadSetIdentity、物理 Host 聚合、LMN/U3DS 修改或三环境运行。 |

## 5. 验证记录

### 5.1 Release 编译

命令：

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`BUILD_EXIT=0`。Contracts、Core、Plugin、Settings/Placement/ClientUi/Network/Release 及所有测试项目均完成 Release 构建；本次输出无 error/warning。

### 5.2 全部测试程序

结果：`FAILED_TEST_PROJECTS=0`。

```text
DEV-05 ClientUi tests: PASS
DEV-02 definition linker tests: PASS
DEV-06 network tests: PASS
DEV-04 placement evaluator tests: PASS
DEV-09 runtime bootstrap tests: PASS
DEV-08 runtime evidence package tests: PASS
DEV-03 settings runtime tests: PASS
```

DEV-10 新增 registration snapshot / getter exception / post-registration mutation 断言包含在 Contracts.Tests 中并通过；当前程序输出仍保留历史 `DEV-02 definition linker tests: PASS` 标签，属于证据命名问题，不影响退出码或断言执行。

### 5.3 UI/native token 门禁

```text
Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Contracts
→ UI/native token scan PASS: 2 C# files

Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Core
→ UI/native token scan PASS: 10 C# files
```

### 5.4 本轮产物摘要

```text
BetterUnturnedExperience.Contracts.dll
SHA-256 1FCB65E8620D39DF3B7F6A8DF65C642F15128348B2A5A87E55D82544339DE8CD

BetterUnturnedExperience.Core.dll
SHA-256 630B86C7B4713994677D431C14F52D8C9678949B91725CE7B81259273B9ABECF

BetterUnturnedExperience.Contracts.Tests.exe
SHA-256 8EE63F1F4A600D884EAB91059EA9216917BB924A90E21A354F50E9516FF29CD9
```

这些摘要只标识本轮静态构建产物，不代表插件安装成功、运行时兼容、U3DS/SP/P2P 通过或发布授权。

## 6. 非阻断建议与证据限制

1. 将测试输出标签改为 `DEV-10 registration runtime tests: PASS`，并在后续拆出独立 DEV-10 测试项目或专用日志，提升追溯性。
2. `ClientUiSatelliteSnapshot` 当前按对象属性复制纯值元数据；后续可将读取、校验和复制封装为单次快照函数，以进一步收窄恶意/竞态 getter 的语义窗口。本项不阻塞 DEV-10，因为注册入口按初始化线程调用且当前 getter 异常/登记后变更门禁已通过。
3. `BitConverter.ToUInt64` 用于 digest/revision 数值转换；后续跨架构格式冻结时应改为规范明确的固定端序编码。当前 Windows .NET Framework 4.7.2 构建目标下不构成 DEV-10 阻断。
4. 尚无真实 BepInEx clean-install、ClientUi satellite 缺失降级、U3DS/SP/P2P 运行证据；按工单边界保持未宣称状态。

## 7. 结论

DEV-10 的纯 Contracts/Core 注册运行时 tracer bullet 已通过独立静态与单元审计，可以交付人工复核并进入后续外部 Fixture/物理 Host 任务；不得将本报告解释为 GPT-18 全量完成或三环境发布资格。

