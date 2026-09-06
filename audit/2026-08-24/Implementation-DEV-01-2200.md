# DEV-01 生产实施报告

**作者：GPT**  
**日期：2026-08-24**  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`

## 一、需求执行概述

DEV-01 建立了 .NET Framework 4.7.2 / C# 10 solution 骨架、Contracts 公共 seam、Core 依赖边界、单一候选插件程序集输出骨架和 Core/Contracts UI/native token 静态扫描门禁。本轮没有实现 DEV-02～DEV-07，没有修改 LMN。

## 二、可追溯矩阵

| 需求点 | 落地 |
| --- | --- |
| 公共 Contracts 不依赖 native/UI | `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` 仅使用 System/System.Core，包含 RT-01 冻结的 module/settings/placement/status seams |
| Core 依赖方向 | `src/BetterUnturnedExperience.Core/BetterUnturnedExperience.Core.csproj` 仅 ProjectReference Contracts |
| 单一候选插件程序集 | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj` AssemblyName `BetterUnturnedExperience`，当前只含组合 marker，不宣称可运行 |
| UI Type Token 门禁 | `eng/Verify-NoUiTokens.ps1`，扫描 Contracts/Core C# 源码中 UnityEngine、SDG.Unturned、Glazier、Sleek、BepInEx、Harmony、LMN、Steamworks |
| TDD 公共 seam | `tests/BetterUnturnedExperience.Contracts.Tests/Program.cs`：枚举值、interface 形状和 DEV-01 无 runtime implementation |

## 三、代码变更

- 新建 `BetterUnturnedExperience.sln`。
- 新建 Contracts、Core、Plugin 三个 .NET Framework 4.7.2 项目。
- 新建 Contracts 公共类型、TDD 测试程序和静态扫描脚本。
- 未删除、未修改用户既有研究文档、LMN 工作树或稳定 DLL。

## 四、TDD / 构建验证

### RED

在 Contracts 源文件尚未存在时运行 MSBuild，失败于 `CS2001`（缺少 `ContractTypes.cs`）。这证明测试/solution 不是空洞绿灯。

### GREEN

命令：

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：4 个项目构建完成，`0 errors / 0 warnings`。

测试命令：

```text
tests\BetterUnturnedExperience.Contracts.Tests\bin\Release\BetterUnturnedExperience.Contracts.Tests.exe
```

结果：`DEV-01 contract tests: PASS`。

静态门禁：

```text
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Contracts
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Core
```

结果：两个目录均 `UI/native token scan PASS`。

## 五、产物哈希

| 产物 | SHA-256 | 字节 |
| --- | --- | ---: |
| `BetterUnturnedExperience.Contracts.dll` | `F3537868EEA22237A66260FC25BC6789F67C2431E62FC420CA3C05B57079A2CE` | 26624 |
| `BetterUnturnedExperience.Core.dll` | `A5E113C92FDEFDA72406DAFFCF1DF140F0D8013AF57A05911C532DC6A81DE92D` | 3584 |
| `BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` | 3584 |
| `BetterUnturnedExperience.Contracts.Tests.exe` | `77A7B6183C6B962A4851DF5CC62B3EBF4DCEB04388D7C8E1498DA00F63895D01` | 5120 |

## 六、边界与未决门禁

- 这是 DEV-01 构建/契约骨架，不是 BUE CandidateBuild 运行验收。
- 不代表 SP、SteamP2PFriends Host/Client 或 U3DS 加载通过。
- 不包含 BepInEx、Unity、Unturned、Glazier、LMN 引用；DEV-02 以后才按各自票据接入 adapter。
- `BetterUnturnedExperience.dll` 当前是 composition marker，尚未达到可装载插件的生产功能标准。

## 七、审计记录

- GPT 自测：RED/GREEN 完成；契约枚举与 DTO 完整形状对照后重建。
- 独立子智能体审计：Round 1 发现缺失三个 handshake/chunk 类型；Round 2 确认代码和契约已通过，仅发现测试 EXE 哈希过期；已更新为 `77A7B6183C6B962A4851DF5CC62B3EBF4DCEB04388D7C8E1498DA00F63895D01`，等待最终轻量复核。
