# GPT-DEV-10 独立审计报告 R1

## 1. 审计对象与边界

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-10-bue-host-external-registration-tracer-bullet.md`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 共享变更：`SCR-GPT18-001`
- 重点文件：`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`、`src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs`、`tests/BetterUnturnedExperience.Contracts.Tests/Program.cs`
- 审计方式：只读源码审查、契约逐项对账、Release 重编译、测试程序执行、Contracts/Core token scan。

## 2. 最终判定

**FAIL（存在 1 个阻断项，暂不得将 DEV-10 标记为 `ready-for-human` 或关闭）**。

实现已覆盖大部分 tracer-bullet seam，且当前构建与测试通过；但注册运行时没有把外部 `IFeatureRegistration` 在成功登记时冻结为私有快照。该缺陷会破坏“不可变 Definition Artifact、Catalog 确定性及既有登记不被后续调用污染”的核心不变量。

## 3. 阻断项

### B-01：成功登记保存外部可变注册对象，Catalog 身份可在冻结前漂移

- **位置**：`src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs:43,58-70,76-82`；`FeatureRegistrationEntry` 构造函数 `13-18`。
- **事实**：`registrations` 保存的是外部传入的 `IFeatureRegistration` 引用。`Register` 中读取一次 `registration.Definition` 与其它属性完成校验后，仍将原对象直接 `registrations.Add(feature.Value, registration)`。`FreezeCatalog` 再次从该外部对象读取 `Definition`、`ModuleFactory`、`ClientUi`。
- **可复现风险**：实现一个合法但带可变属性的 `IFeatureRegistration`，登记成功后在 `FreezeCatalog` 前替换 `Definition`，可使 FeatureId、digest、payload 或 factory 与成功登记时不同；也可返回 null/抛异常，使冻结阶段 NRE/异常。此前登记的字典键仍是旧 FeatureId，因而可能绕过重复身份检查或生成与接受结果不一致的 Catalog。
- **根因**：把“外部注册接口”当作不可变值，而没有在 `Register` 边界建立内部不可变 registration snapshot。`FeatureDefinitionArtifact` 自身复制了 payload，但其宿主 registration 仍是可变外部对象。
- **修复要求**：在一次受控登记操作中读取并校验全部字段，建立内部不可变快照（至少复制 FeatureDefinitionArtifact 的所有值、ClientUi 的纯值元数据，并单独保存已验证的 factory 引用）；字典只保存该快照，不再保存外部 `IFeatureRegistration`。冻结阶段只消费快照。对 getter 异常、getter 前后不一致或 null 必须 fail-closed 为稳定 `InvalidDefinitionArtifact`/相应原因，且不污染既有登记。
- **必须新增测试**：登记后改变外部 registration 的 Definition/ClientUi/factory；登记对象 getter 在第二次读取时返回不同值或抛异常；确认 Catalog、CatalogRevision 与已接受事实不改变，且无异常穿透。

## 4. 逐项审计矩阵

| 审计维度 | 判定 | 证据与说明 |
| :--- | :---: | :--- |
| 需求符合性 | **FAIL** | 阶段机、显式注册、重复拒绝、展示投影已实现；但 B-01 违反不可变身份/Catalog 事实源边界。 |
| 阶段机 | PASS（静态） | `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady`；早注册、晚注册、重复冻结均有测试。`CoreSafeMode` 为额外 fail-closed 状态。 |
| 确定性 Catalog | **FAIL** | 对不同到达顺序的排序/Revision 测试通过；但外部 registration 可在冻结前改变，故确定性只对诚实且静态对象成立。 |
| Artifact digest 校验 | PASS（本轮静态/单元） | `IsValidDefinition` 调用 `DigestMatches`，按 payload SHA-256 对比 `ArtifactPayloadDigest`；测试 fixture 使用 payload 的正确 digest，非法 artifact 被拒绝。未证明 DefinitionSetDigest 的语义重算，属于后续 linker/format 责任。 |
| 数据不可变性 | **FAIL** | `FeatureDefinitionArtifact` 对 payload 做 defensive copy，Catalog entry 列表为 ReadOnlyCollection；但 registration、factory、satellite 外部引用未快照。 |
| 线程/重入 | **FAIL（与 B-01 同一根因，需同轮修复）** | `Dictionary` 与 `Phase/Catalog` 无锁、无线程归属断言；公开 `OpenRegistration/FreezeCatalog/MarkRuntimeReady/EnterCoreSafeMode` 可并发/重入调用。提案虽要求初始化线程，但实现没有把这一前置条件固化为防护。至少应加初始化线程/串行入口断言，或内部同步并保证单次线性化；外部 getter 不得在锁内执行。 |
| 错误隔离 | PARTIAL | 常规 null/坏 artifact/坏 UI 返回稳定结果；外部 getter 抛异常或冻结时对象漂移可异常穿透，随 B-01 修复。 |
| Contracts/Core 类型隔离 | PASS | 直接扫描 `Contracts` 与 `Core` 源码，未发现 Unity、Glazier、Sleek、LMN、BepInEx、Harmony、Unturned 具体 token；`Verify-NoUiTokens.ps1` 两个目录均 PASS。 |
| DEV-11+ 越界 | PASS | 未发现第三方 DLL、no-op fixture、ClientUi satellite、LoadSetIdentity、LMN/U3DS 修改或三环境运行实现。 |

## 5. 已执行验证

### Release 构建

命令：

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`BUILD_EXIT=0`，全仓项目构建完成；本轮输出未见 error/warning。

### 测试

执行：

```text
tests/BetterUnturnedExperience.Contracts.Tests/bin/Release/BetterUnturnedExperience.Contracts.Tests.exe
```

结果：`DEV-02 definition linker tests: PASS`，`TEST_EXIT=0`。注意测试程序输出标签仍为 DEV-02，未同步为 DEV-10；不影响进程退出码，但降低本轮验收证据的可追溯性。

### 静态 token 门禁

```text
eng/Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Contracts
→ UI/native token scan PASS: 2 C# files
eng/Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Core
→ UI/native token scan PASS: 10 C# files
```

### 产物指纹（本轮重编译后）

```text
BetterUnturnedExperience.Core.dll
SHA-256 2BDF12BAB85D100D970ABCC98CDEF150307F6FDF1EC44F46470E8A91A75C9C07
```

该哈希仅标识本轮静态构建产物，不是外部 DLL 安装成功、U3DS/SP/P2P 运行通过或发布授权证据。

## 6. 非阻断建议

1. 将 `CatalogRevision` 与 digest 的数值编码明确固定为跨架构端序；当前使用 `BitConverter.ToUInt64`（`FeatureRegistrationRuntime.cs:124,137`），在 Windows 目标上可工作，但规范化序列化最好显式声明端序。
2. 将测试程序输出从 `DEV-02 definition linker tests: PASS` 改为 `DEV-10 registration runtime tests: PASS`，并拆分出专用 DEV-10 测试入口，避免验收报告引用错误证据标签。
3. 增加 `null registration`、getter 异常、CoreSafeMode 后各控制方法、重复 `FreezeCatalog`/`MarkRuntimeReady` 的单元测试。
4. 保持 DEV-10 工单为 `ready-for-agent`，修复 B-01 后重新执行 Release 编译、测试、token scan 和独立审计；通过后才可转 `ready-for-human`。

## 7. 复审门槛

B-01 修复后，必须提供：

- registration snapshot 的源码位置与测试；
- 同一输入不同到达顺序的 Catalog/Revision 复测；
- 并发或线程归属防护证据；
- Release 0 errors/0 warnings；
- DEV-10 专用测试退出码 0；
- Contracts/Core token scan PASS。

