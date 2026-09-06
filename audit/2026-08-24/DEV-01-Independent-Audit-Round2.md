# DEV-01 独立审计报告 — Round 2

**作者：GPT（独立审计角色）**  
**审计日期：2026-08-24**  
**审计对象：** `DEV-01 Repository / Solution Skeleton + Shared Contracts`  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**Round 1 结果：** FAIL（缺少三个 handshake/chunk token）  
**Round 2 结果：FAIL（1 项证据阻断）**

## 一、审计范围

本轮仅读取 Round 1 审计、DEV-01 工单、RT-01/Shared Contract/RT-06 基线及当前 solution、源码、测试和实施报告，并重新执行 Release Rebuild、测试程序和 Contracts/Core token scan。未修改生产源码、LMN 或任何上游报告。

## 二、Round 1 修复核对

Round 1 指出的三个缺失 token 已补入 `src/BetterUnturnedExperience.Contracts/ContractTypes.cs`：

- `HandshakeReject`：`ConnectionGeneration`、`ClientNonceHigh`、`ClientNonceLow`、`Error`、`SupportedContractMajor`
- `SnapshotKind : byte`：`CapabilityHello`、`CapabilitySnapshot`、`SettingsSnapshot`
- `SnapshotChunkEnvelope`：`ConnectionGeneration`、`SnapshotId`、`Kind`、`ChunkIndex`、`ChunkCount`、`TotalLength`、`ChunkLength`、`Sha256`、`ChunkBytes`

与 `Shared-Contract-Spec.md` §5.0 逐项对账：`PASS`。Public type 集合对账结果：

```text
Missing: (none)
Unexpected: (none)
```

测试程序新增了 DTO 存在性和 `SnapshotKind.SettingsSnapshot == 2` 断言。

## 三、重新执行门禁

### 3.1 Release Rebuild

命令：

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`PASS`，`BUILD_EXIT=0`，Contracts、Tests、Core、Plugin 四项目均完成构建，`0 errors / 0 warnings`。

### 3.2 合约测试

命令：

```text
tests\BetterUnturnedExperience.Contracts.Tests\bin\Release\BetterUnturnedExperience.Contracts.Tests.exe
```

结果：`PASS`，输出 `DEV-01 contract tests: PASS`，`TEST_EXIT=0`。

### 3.3 UI/native token scan

Contracts 和 Core 两次扫描均返回 `UI/native token scan PASS`，退出码均为 0。

## 四、阻断项

### B-DEV01-R2-01：实施报告中的测试产物 SHA-256 与当前 Rebuild 不一致

当前 `Implementation-DEV-01-2200.md` 记录：

```text
BetterUnturnedExperience.Contracts.Tests.exe
BB66858C240C3FF62D07EAAB47ED87FF16BFB6C6CC8B076D58996451A07F6739
```

本轮对 Rebuild 后实际产物复算：

```text
BetterUnturnedExperience.Contracts.Tests.exe
77A7B6183C6B962A4851DF5CC62B3EBF4DCEB04388D7C8E1498DA00F63895D01
```

Contracts/Core/Plugin 三个 DLL 的报告哈希与本轮复算一致；只有测试 EXE 的证据过期。由于 DEV-01 Acceptance 明确要求输出构建日志、测试结果和产物哈希，且工程审计必须保持源码/测试/产物可追溯，这一项必须修正后才能给出 PASS。

**修复要求：** 更新 GPT 前缀实施报告（或生成不可覆盖的 Round 2 实施报告）中的测试 EXE 哈希、字节数和对应 Rebuild 时间；确保报告中的四项产物哈希来自同一次 Rebuild。随后只需重跑独立审计的哈希核对和三项门禁即可。

## 五、其余维度结论

- 项目依赖方向 `Contracts <- Core <- Plugin`：`PASS`。
- .NET Framework 4.7.2 / C# 10 / Release 0 errors：`PASS`。
- Contracts/Core 未发现 Unity、Glazier、Sleek、BepInEx、Harmony、LaunchMultiplayerNet、Steamworks 或 LMN 源码 token：`PASS`。
- 当前 source solution 未提前实现 DEV-02～DEV-07：`PASS`；仍只有 contracts、marker 和测试骨架。
- 未发现 DEV-01 对 LMN 的项目引用或本项目侧 LMN 修改；LMN 工作树的既有 dirty 状态不归因于本轮：`PASS（范围判断）`。
- RT-01 冻结接口、状态、设置、placement、Core status、能力/握手以及 chunk DTO public type 集合：`PASS`。本轮未发现新的契约形状阻断。

## 六、最终裁定

Round 1 的契约缺失已经修复，代码和门禁本身通过；但 Round 2 发现实施报告仍引用旧测试 EXE 哈希，因此本轮为 `FAIL — 1 evidence blocker`。更新报告证据并重新核对后，才可将 DEV-01 标记为审计 PASS / `resolved`。本报告为新增 Round 2 记录，不覆盖 Round 1。

