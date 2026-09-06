# DEV-02 独立审计报告（Round 3 Final）

**作者：GPT（独立审计子任务）**  
**日期：2026-08-24**  
**审计对象：** `DEV-02 Definition Linker + Compiled Catalog + Bootstrap`  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**前轮报告：** `DEV-02-Independent-Audit.md`、`DEV-02-Independent-Audit-Round2.md`（均保留）

## 一、最终裁定

**判定：PASS**

Round 3 已关闭 Round 2 的 B-02：新增测试覆盖 Catalog 的 `Features` 与 `FragmentKinds` 只读拒绝、`RequiredFeatures` 输入快照隔离，以及相同诊断集合在不同输入枚举顺序下的完整稳定顺序。独立 Release 重建、测试 EXE 和 Contracts/Core token scan 全部通过。

DEV-02 具备提交 Gemini 消费复核和人工关闭工单的条件。此 PASS 仅覆盖 DEV-02 构建/静态/单元测试门禁，不代表 DEV-03～DEV-07 或 SP、SteamP2PFriends、U3DS 运行通过。

## 二、独立复跑证据

### 2.1 Release Rebuild

命令：

```text
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe \
  BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：**PASS**，4 个项目，`0 errors / 0 warnings`，退出码 `0`。

### 2.2 DEV-02 测试 EXE

命令：

```text
tests\BetterUnturnedExperience.Contracts.Tests\bin\Release\BetterUnturnedExperience.Contracts.Tests.exe
```

结果：**PASS**，输出 `DEV-02 definition linker tests: PASS`，退出码 `0`。

测试 EXE SHA-256：

```text
FFE2FE184B6B66DF804D2F574E63401F6C9F86BD6F8334C94DEF48A0F2C53730
```

### 2.3 Contracts/Core Token Scan

```text
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Contracts
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Core
```

结果：**PASS**，分别扫描 2 与 3 个 C# 文件，未发现 Unity、SDG.Unturned、Glazier、Sleek、BepInEx、Harmony、LaunchMultiplayerNet 或 Steamworks token。

### 2.4 本轮产物 SHA-256

| 产物 | SHA-256 |
| --- | --- |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `5327DA78B954009D684FE38932FCCA9EB8A65EC7536B2A487DCFCA98C547B264` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `1BA39EF2D03BF2A3D1FE947A7CD79F36D845E239ACFDE4DCAC370F21B4937F4E` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |
| `tests/BetterUnturnedExperience.Contracts.Tests/bin/Release/BetterUnturnedExperience.Contracts.Tests.exe` | `FFE2FE184B6B66DF804D2F574E63401F6C9F86BD6F8334C94DEF48A0F2C53730` |

## 三、Acceptance 对账

| Acceptance | 判定 | 证据 |
| --- | --- | --- |
| 身份重复、required 缺失、schema 不支持、重复和悬空引用原子失败 | PASS | `FeatureDefinitionLinker.cs:94-126`；测试 `Program.cs:33-50`；失败结果无 Catalog。 |
| 稳定排序与非随机 DefinitionSetDigest | PASS | `FeatureDefinitionLinker.cs:99-137`；测试 `Program.cs:39-42`。 |
| Catalog 不变视图 | PASS | 源码的数组/只读包装；测试 `Program.cs:51-54` 输入快照隔离、`:58-59` Add 被拒绝。 |
| Runtime 只消费 compiled catalog，不访问原始 manifest/Git/DNS/审批 | PASS | `FeatureLoadGate` 仅消费 `CompiledIdentityCatalog`；源码范围无这些 adapter。 |
| Catalog-bound、不可 default 的 reference admission handle | PASS | `FeatureAdmissionHandle` 为 sealed class，internal 构造；测试 `Program.cs:60-63` 验证 Catalog/record 绑定。 |
| Contracts/Core 无 UI/native token | PASS | 独立扫描通过。 |
| TDD 覆盖完整诊断稳定性 | PASS | `Program.cs:44-57` 覆盖缺失/unsupported/dangling 与完整 diagnostics 顺序比较；`:35-36` 验证原子失败。 |
| 不提前实现 DEV-03～DEV-07、不修改 LMN | PASS | 新增源码仅限 `Core/Definitions/FeatureDefinitionLinker.cs`；未发现相关实现。 |

## 四、Round 2 阻断关闭核对

### B-02 Catalog 不变性 TDD

已关闭：

- `requiredSnapshot` 被修改后，已构造 Fragment 的 required 引用仍保持快照，测试 `Program.cs:51-54` 通过；
- `forward.Catalog.Features` 被视为 `IList<T>` 时 `Add` 抛出 `NotSupportedException`，测试 `Program.cs:58` 通过；
- `forward.Catalog.Features[0].FragmentKinds` 同样拒绝 `Add`，测试 `Program.cs:59` 通过。

### 稳定 diagnostics

测试 `Program.cs:55-57` 对同一诊断片段集以两种输入顺序执行 Link，并比较完整 `JoinDiagnostics` 结果；实现 `FeatureDefinitionLinker.cs:126` 通过 Ordinal `Distinct + OrderBy` 输出稳定 diagnostics。该项已满足本票据 TDD 门禁。

## 五、非阻断建议

### R-01 Admission handle 暴露面偏宽

`FeatureAdmissionHandle` 的 `Catalog` 与 `Record` 是 public 只读属性。它仍满足本票据要求的 sealed reference、internal 构造和 Catalog/record 绑定，当前没有授予权限、版本兼容、网络能力或 `Running` 状态；因此不阻断 DEV-02。后续 Runtime Admission/Bootstrap 可考虑将 capability 进一步收窄为内部绑定加受控只读身份投影。

### R-02 构造器信任边界

`CompiledIdentityCatalog` 构造器为 public，但 `CompiledFeatureRecord` 构造器为 internal，外部程序集无法自行构造有效记录。后续若扩展 Core 内部调用，建议保留“只有 Linker 能生成成功 Catalog”的单一工厂边界，避免手工拼装绕过 canonical 编译流程。

## 六、范围与证据边界

- 本报告没有宣称 BUE CandidateBuild 已完成。
- 没有 SP、SteamP2PFriends Host/Client、U3DS 或同哈希运行证据。
- BepInEx、Unity、Glazier、Sleek、LMN 和 Unturned native adapter 尚未在本轮实现。
- DEV-03 SettingsRuntime、DEV-04 Evaluator、DEV-05 ClientUi、DEV-06 网络 codec/LMN adapter、DEV-07 三环境发布门禁仍必须分别执行。

## 七、结论

**DEV-02 独立审计 Round 3：PASS。**

建议动作：将本报告交付 Gemini 做前端消费复核；Gemini `ACCEPT` 后，人工可将 DEV-02 工单标记为 `resolved`，再按依赖顺序推进 DEV-03。

