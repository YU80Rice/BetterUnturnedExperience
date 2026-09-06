# DEV-01 独立审计报告

**作者：GPT（独立审计角色）**  
**审计日期：2026-08-24**  
**审计对象：** `DEV-01 Repository / Solution Skeleton + Shared Contracts`  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**结论：FAIL（1 项阻断）**

## 一、审计范围与只读边界

本轮仅读取 DEV-01 工单、`RT-01-Shared-Contract-Baseline.md`、`Shared-Contract-Spec.md`、`RT-06-Joint-Seam-Implementation-Readiness.md`、solution/csproj/源码/测试/实施报告，并重新执行构建、测试和 UI/native token 门禁。未修改任何生产源码、LMN 源码或现有研究材料。

目标证据文件：

- `src/BetterUnturnedExperience.Contracts/ContractTypes.cs`
- `src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj`
- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj`
- `eng/Verify-NoUiTokens.ps1`
- `tests/BetterUnturnedExperience.Contracts.Tests/Program.cs`
- `audit/2026-08-24/Implementation-DEV-01-2200.md`

## 二、门禁执行结果

### 2.1 Release Rebuild

命令：

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`PASS`，`BUILD_EXIT=0`，4 个项目完成构建，0 errors / 0 warnings。

### 2.2 合约测试

命令：

```text
tests\BetterUnturnedExperience.Contracts.Tests\bin\Release\BetterUnturnedExperience.Contracts.Tests.exe
```

结果：`PASS`，输出 `DEV-01 contract tests: PASS`，`TEST_EXIT=0`。

### 2.3 UI/native token 门禁

命令：

```text
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Contracts
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Core
```

结果：`PASS`；Contracts 扫描 2 个 C# 文件，Core 扫描 2 个 C# 文件，均未发现脚本当前禁止的 UI/native token。

## 三、阻断项

### B-DEV01-01：RT-01 冻结的 capability/chunk DTO 未完整进入 Contracts

**位置：** `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` 末段网络契约声明。  
**依据：** `RT-01-Shared-Contract-Baseline.md` §4 明确将 capability/negotiation/handshake/chunk DTO 与 enum 全部列为冻结 token；`Shared-Contract-Spec.md` §5.0 明确冻结以下类型。

源文件当前已包含 `CapabilityHello`、`CapabilitySnapshot`、`CapabilityAck`、`SessionReadyEvent`，但缺少：

- `HandshakeReject`
- `SnapshotKind`
- `SnapshotChunkEnvelope`

独立的 public type 集合对账结果：

```text
Expected RT-01/Shared Contract token set - Actual Contracts token set
Missing: HandshakeReject, SnapshotKind, SnapshotChunkEnvelope
Unexpected: (none)
```

这不是运行时实现要求；但它是共享 Contracts 的冻结形状缺失，会使后续 DEV-06 codec 无法只引用当前 DEV-01 Contracts 完成 `0x0001/0x0002` 的分片线路与 `BUEB` reject DTO 对账。DEV-01 不能在缺少这些已冻结 token 的情况下标记 `resolved`。

**修复要求：** 按 `Shared-Contract-Spec.md` §5.0 精确补入 3 个类型，保持字段、底层 enum 类型、顺序和值完全一致；补充最小反射/编译测试以防止再次遗漏；重新执行 Release Rebuild、测试、token scan、产物哈希并重新提交独立审计。

## 四、已通过检查

### 4.1 项目边界与依赖方向

- Contracts 仅引用 `System`、`System.Core`，无项目引用。
- Core 仅 ProjectReference Contracts。
- Plugin 仅 ProjectReference Core；未发现反向引用。
- 三个生产项目均声明 .NET Framework 4.7.2、C# 10、Release `TreatWarningsAsErrors=true`。

### 4.2 UI/headless 依赖隔离

逐源扫描未发现 `UnityEngine`、`SDG.Unturned`、`Glazier`、`Sleek`、`BepInEx`、`Harmony`、`LaunchMultiplayerNet`、`Steamworks` 或 `LMN`。Contracts 导出引用仅为 `mscorlib`；Core 导出类型仅为 composition marker，未引入 UI/native/LMN 程序集。

### 4.3 范围控制

当前 solution 中生产代码只有 Contracts 类型、Core marker 和 Plugin composition marker；未发现设置运行时、Definition Linker、库存候选算法、Glazier UI、网络 codec、ReadyFrameFence 或 LMN adapter 的生产实现。DEV-02～DEV-07 未被提前实现。

### 4.4 LMN 边界

DEV-01 项目目录未修改 LMN。LMN 工作树在审计时本身存在既有 dirty 状态；该状态不能被本轮 DEV-01 归因，也不能被解释为 DEV-01 修改证据。LMN 仍未纳入 DEV-01 solution 引用。

## 五、非阻断建议

1. `Verify-NoUiTokens.ps1` 当前禁止列表包含完整名称 `LaunchMultiplayerNet`，但没有单独的 `LMN` token；建议将 `LMN` 加入扫描清单，并把项目/程序集引用检查作为后续 CI 机械门禁。
2. 当前测试只断言少量 enum 值和两个 interface 形状；补齐 3 个缺失 token 后，建议增加完整 public type/field/enum 对账，特别是 `FeatureState`、`DragInteractionState`、`FrameworkErrorCode` 和消息 kind 相关 DTO。
3. `Implementation-DEV-01-2200.md` 中的当前产物哈希与本次 Rebuild 结果一致：Contracts `0588568AC490816F16916DC6A6B6F0D0D25A19E3EEBFF965A8BF8669CB23E5A8`、Core `A5E113C92FDEFDA72406DAFFCF1DF140F0D8013AF57A05911C532DC6A81DE92D`、Plugin `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC`、Tests `BB66858C240C3FF62D07EAAB47ED87FF16BFB6C6CC8B076D58996451A07F6739`；修复后应生成新的实施报告/哈希记录。

## 六、最终裁定

当前 DEV-01：`FAIL — 1 blocker`。  
构建、测试、UI/native 隔离和项目依赖方向均通过，但共享 Contracts 尚未完整承载 RT-01 冻结的 capability/chunk token。补齐 `HandshakeReject`、`SnapshotKind`、`SnapshotChunkEnvelope` 并重新审计 PASS 后，才可关闭 DEV-01；在此之前不得宣称 DEV-01 审计通过、后续生产功能完成或三环境运行通过。

