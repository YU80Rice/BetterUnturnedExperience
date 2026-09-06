# DEV-02 独立审计报告

**作者：GPT（独立审计子任务）**  
**日期：2026-08-24**  
**审计对象：** `DEV-02 Definition Linker + Compiled Catalog + Bootstrap`  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**审计方式：** 只读源码审查、独立 Release Rebuild、测试可执行文件复跑、Contracts/Core UI-token scan。未修改实现文件。

## 一、最终裁定

**判定：FAIL（1 个阻断项，另有 1 个实现级风险建议）**

构建、现有测试和 token 扫描均通过；但当前 TDD 可执行测试没有覆盖 DEV-02 工单明确要求的完整失败诊断矩阵、Catalog 不变性和 handle 绑定不变量。因此不能将 DEV-02 标记为 `resolved`，也不能据此开放后续实现门禁。

## 二、执行证据

### 2.1 Release Rebuild

命令：

```text
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe \
  BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：**PASS，4 个项目，0 errors / 0 warnings**。

说明：系统 Framework 4.8 MSBuild（`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`）不支持项目要求的 `LangVersion=10.0`，独立复跑改用已安装的 VS 18 Insiders MSBuild；这属于工具选择问题，不是源码构建失败。

### 2.2 测试可执行文件

命令：

```text
tests\BetterUnturnedExperience.Contracts.Tests\bin\Release\BetterUnturnedExperience.Contracts.Tests.exe
```

结果：**PASS**，输出 `DEV-02 definition linker tests: PASS`，退出码 `0`。

### 2.3 Contracts/Core Token 门禁

命令：

```text
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Contracts
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Core
```

结果：**PASS**，分别扫描 2 与 3 个 C# 文件；未发现 Unity、SDG.Unturned、Glazier、Sleek、BepInEx、Harmony、LaunchMultiplayerNet 或 Steamworks token。

### 2.4 产物哈希

| 产物 | SHA-256 |
| --- | --- |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `5327DA78B954009D684FE38932FCCA9EB8A65EC7536B2A487DCFCA98C547B264` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `1BA39EF2D03BF2A3D1FE947A7CD79F36D845E239ACFDE4DCAC370F21B4937F4E` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |
| `tests/BetterUnturnedExperience.Contracts.Tests/bin/Release/BetterUnturnedExperience.Contracts.Tests.exe` | `6B7E0905B5AC1B4F6FEDD5AE9A09F9C14F72371950FD50FA6ECBB199D51593A9` |

## 三、逐项审计结果

| 审计维度 | 判定 | 证据与说明 |
| --- | --- | --- |
| 全仓原子失败路径 | PASS（静态） | `FeatureDefinitionLinker.cs:94-126` 在诊断非空时返回 `LinkFailed` 与 `Catalog=null`；没有把部分结果作为成功 Catalog 返回。 |
| 身份/绑定片段完整性 | PASS（静态） | `:113-119` 对每个 FeatureId 要求 `identity` 与 `binding`。 |
| schema / 重复 / 悬空 required feature | PASS（静态） | `:103-124` 检查 schema、`FeatureId + FragmentKind` 重复以及 required FeatureId 存在性。 |
| 稳定排序与 digest | PASS（静态及现有样例） | `:99-101`、`:128-137` 使用序位无关的排序与固定宽度 digest 文本；测试 `Program.cs:37-40` 复跑通过。 |
| Catalog 视图不变性 | **未证实（见 B-01）** | 代码使用 `ReadOnlyDictionary`/`ReadOnlyCollection`，但测试没有尝试修改 `Features`、`FragmentKinds` 或输入集合后的快照隔离。 |
| Runtime 只接受编译记录 | PASS（静态） | `FeatureLoadGate` 只消费 `CompiledIdentityCatalog`，未访问 manifest、Git、DNS、事件 ledger 或审批系统。 |
| admission handle 为不可 default 引用并绑定 Catalog/record | PASS（静态，需补测） | `FeatureAdmissionHandle` 为 `sealed class`，构造器 `internal`，由 `:150-156` 从当前 Catalog 记录创建。 |
| Contracts/Core 类型隔离 | PASS | 独立 token scan 通过。 |
| DEV-03～DEV-07 / LMN 越界 | PASS（源码范围） | 本轮可见新增实现仅为 `Core/Definitions/FeatureDefinitionLinker.cs`；未发现设置、库存、网络 codec、ClientUi 或 LMN 实现。 |
| TDD 接受矩阵完整度 | **FAIL（见 B-01）** | 当前测试只覆盖空集、成功链接、输入顺序 digest、重复状态、未知/已知准入；没有覆盖工单要求的完整诊断、不可变性和绑定约束。 |

## 四、阻断项

### B-01：DEV-02 TDD 验收矩阵不完整

**位置：** `tests/BetterUnturnedExperience.Contracts.Tests/Program.cs:32-50`。

**事实：** 当前可执行测试只断言：

- 空定义集失败；
- 两种输入顺序产生相同 digest；
- 重复片段返回失败；
- 未知 FeatureId 不准入；
- 已知 FeatureId 返回非空 handle。

**缺失断言：**

- schema version `0` 与大于 `SupportedFragmentSchemaVersion`；
- 缺少 identity fragment；
- 缺少 binding fragment；
- required feature 缺失/悬空引用；
- `LinkFailed` 的 `Catalog == null` 及稳定、可区分的 diagnostics；
- `Features` 与 `FragmentKinds` 的只读行为；
- 输入集合/required list 在构造后变更不会改变 Catalog；
- handle 的 `Catalog` 与 `Record` 正确绑定，且不同 Catalog 之间不能混淆；
- digest 随 canonical 输入变化而变化，并且不含枚举顺序噪声。

**影响：** 工单 Acceptance 明确要求“稳定排序、重复/缺失诊断、digest 稳定性、不可变 catalog 和无效运行时访问”的 TDD 覆盖。当前单元测试绿灯不能证明这些门禁，后续 DEV-03/Runtime Admission 可能在未发现边界回归的情况下依赖该实现。

**修复要求：** 扩充 DEV-02 测试矩阵并以独立重跑确认；修复应保持测试只验证已冻结 DEV-02 seam，不提前实现 DEV-03～DEV-07。

## 五、实现级风险（非本轮唯一阻断，但建议随 B-01 处理）

### R-01：Admission handle 暴露了 Catalog 与 Record

**位置：** `src/BetterUnturnedExperience.Core/Definitions/FeatureDefinitionLinker.cs:140-145`。

当前 handle 虽然是不可 default 的 sealed 引用对象、构造器为 internal，并绑定了 Catalog/Record；但其 `Catalog` 与 `Record` 为 public 属性。相对于 RT-06/Feature Definition Pipeline 所称的“窄 opaque capability”，调用方可以直接取得完整 Catalog 与记录视图。现阶段它没有赋予权限或 Running 状态，未观察到直接越权路径，故列为风险而非单独阻断。建议后续收窄为内部绑定、仅向受控 Bootstrap 暴露必要身份投影，或在 DEV-02 设计说明中明确这些只读属性属于诊断/消费视图而非权限能力。

## 六、未发现的问题

- 没有发现 Contracts/Core 的 Unity、Glazier、Sleek、LMN、BepInEx、Harmony 或 Unturned native 引用。
- 没有发现运行时重新解析原始 manifest、Git、DNS、审批系统的代码。
- 没有发现把 Admission handle 解释成代码安全、版本兼容、权限、网络能力或 `Running` 的逻辑。
- 没有发现本轮提前实现 DEV-03～DEV-07 或修改 LMN 的证据。
- 没有产生 SP、SteamP2PFriends Host/Client、U3DS 或同哈希运行 PASS；这与本票据的静态/构建范围一致。

## 七、结论与放行条件

DEV-02 当前为：**构建与静态实现基本通过，独立审计 FAIL（B-01）**。

在 B-01 修复并重新执行：

1. Release Rebuild（0 errors / 0 warnings）；
2. DEV-02 测试 EXE；
3. Contracts/Core token scan；
4. 独立审计；
5. Gemini 前端消费复核；

之前，不应将 DEV-02 标记为 `resolved`，也不应以本报告宣称后续模块或三环境运行通过。
