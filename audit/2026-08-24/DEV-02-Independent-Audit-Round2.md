# DEV-02 独立审计报告（Round 2）

**作者：GPT（独立审计子任务）**  
**日期：2026-08-24**  
**审计对象：** `DEV-02 Definition Linker + Compiled Catalog + Bootstrap`  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**前轮报告：** `DEV-02-Independent-Audit.md`（Round 1，不覆盖）

## 一、最终裁定

**判定：FAIL（B-02 仍未关闭）**

本轮已确认 Round 1 主要缺口得到补充：失败无 Catalog、identity/binding 缺失、unsupported schema、dangling required feature、handle 的 Catalog/record 绑定均有测试。Release 构建、测试 EXE 和 Contracts/Core token scan 也全部通过。

但 DEV-02 Acceptance 明确要求 TDD 覆盖“不可变 catalog”；当前测试仍未验证 `CompiledIdentityCatalog.Features`、`CompiledFeatureRecord.FragmentKinds` 以及构造输入快照的不可变性。另有“稳定 diagnostics”仅验证了某条诊断存在，没有验证错误集合及其顺序在输入枚举顺序改变时稳定。因此本轮仍不能关闭 DEV-02。

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
BE9EEABFA067CB7DD35EC5EB20A2340AAAD2DB782B40B9BD47601ABC084F3993
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
| `tests/BetterUnturnedExperience.Contracts.Tests/bin/Release/BetterUnturnedExperience.Contracts.Tests.exe` | `BE9EEABFA067CB7DD35EC5EB20A2340AAAD2DB782B40B9BD47601ABC084F3993` |

## 三、Round 1 修复核对

| Round 1 缺口 | Round 2 状态 | 证据 |
| --- | --- | --- |
| 失败结果无 Catalog/无诊断断言 | 已补 | `Program.cs:33-35` |
| identity/binding 缺失诊断 | 已补 | `Program.cs:43-46` |
| unsupported schema | 已补 | `Program.cs:47` |
| dangling required feature | 已补 | `Program.cs:48-49` |
| handle 绑定当前 Catalog/record | 已补 | `Program.cs:50-53` |
| Catalog/record 不变性 | **未补** | 全文件无针对只读集合、输入快照或 mutation rejection 的断言 |
| diagnostics 稳定性 | **未完全补** | `HasDiagnostic` 只检查存在性；未比较不同输入顺序的完整、有序 diagnostics 集合 |

## 四、阻断项

### B-02：Catalog 不变性 TDD 仍缺失

**规范要求：** DEV-02 工单 Acceptance 要求“Compiled Catalog 是不变视图”并要求 TDD 覆盖不可变 catalog。

**源码事实：**

- `FeatureDefinitionLinker.cs:52-64` 对 Catalog 的 records 和 Features 使用数组快照及只读包装；
- `FeatureDefinitionLinker.cs:132` 由 Linker 生成 `ReadOnlyCollection<string>` 作为 `FragmentKinds`；
- 但 `CompiledIdentityCatalog` 的 public 构造函数仍接受 `CompiledFeatureRecord`，本身未验证每个 record 的 `FragmentKinds` 是不可变快照；
- `Program.cs` 没有尝试写入/替换 `Features` 或 `FragmentKinds`，也没有在创建原始 `requiredFeatures`/记录输入后修改它们并验证 Catalog 不变。

**影响：** 当前绿灯只证明实现“看起来”使用了只读包装，不能证明对外观察到的 Catalog 视图与构造输入隔离，也不能防止未来 Core 内部通过 public Catalog 构造路径传入可变的 record 子列表。该项直接对应工单 Acceptance/TDD 门禁，故为阻断。

**修复要求：** 增加只读 mutation 测试和输入快照测试；必要时收窄 `CompiledIdentityCatalog` 构造入口，保证任何成功 Catalog 的嵌套集合均为不可变快照。修复后重新构建、重跑测试、token scan 和独立审计。

## 五、稳定 diagnostics 复核结果

Linker 实现 `FeatureDefinitionLinker.cs:126` 使用 `Distinct(StringComparer.Ordinal).OrderBy(..., StringComparer.Ordinal)`，静态上具备稳定排序意图；Round 2 测试已覆盖具体缺失诊断存在性，但没有将同一错误片段集以不同输入顺序链接并比较完整 diagnostics 列表。因此此项暂列为建议随 B-02 一起补齐，若不补则仍不能完整声称“稳定 diagnostics TDD 已覆盖”。

## 六、非阻断风险

### R-01：Admission handle 暴露面偏宽

`FeatureAdmissionHandle` 在 `FeatureDefinitionLinker.cs:140-145` 是 sealed reference、internal 构造并绑定当前 Catalog/record，满足不可 default 和绑定的核心安全性质；但 `Catalog` 与 `Record` 为 public 属性。相对于“窄 opaque capability”建议后续收窄为内部绑定或受控只读投影。本轮未发现它直接赋予权限、版本兼容、网络能力或 Running 状态，因此不单独阻断。

## 七、结论与放行条件

DEV-02 当前状态：**构建/静态源码基本通过，独立审计 Round 2 仍 FAIL（B-02）**。

关闭前必须完成 Catalog 不变性测试（并建议补齐完整 diagnostics 顺序测试），然后重新执行：

1. Release Rebuild（0 errors / 0 warnings）；
2. DEV-02 测试 EXE；
3. Contracts/Core token scan；
4. 独立审计；
5. Gemini 前端消费复核。

在这些门禁完成前，不应将 DEV-02 标记为 `resolved`，也不应宣称 DEV-03～DEV-07 或 SP、SteamP2PFriends、U3DS 运行通过。

